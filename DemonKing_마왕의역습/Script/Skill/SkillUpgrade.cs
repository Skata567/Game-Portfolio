using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// 종족별 스킬 업그레이드 시스템 (5개 종족 + 인구)
/// - Ain(아인족), Suin(수인족), UnDead(언데드), UnHoly(언홀리), Evil(이블) 종족별 스킬 3단계
/// - Population 스킬 5단계
///
/// 사용 방법:
/// 1. SkillDatabase를 생성하고 종족별 스킬 정보를 설정
/// 2. SkillUIManager가 붙은 UI 패널 생성
/// 3. 이 스크립트에서 참조들을 연결
/// 4. 스킬 버튼들을 배치하고 연결
/// </summary>
public class SkillUpgrade : MonoBehaviour
{
    #region 데이터 참조

    [Header("데이터 참조")]
    private MonsterCatalog catalogCache;
    private UIManager manager;
    private SkillUIManager skillInfoUI;

    #endregion

    #region 종족별 스킬 포인트 (현재 레벨 0부터 시작)

    [Header("종족별 스킬 포인트 - 현재 레벨")]
    [SerializeField] private int AinSkillPoint = 0;        // 아인족 스킬 현재 레벨
    [SerializeField] private int SuinSkillPoint = 0;       // 수인족 스킬 현재 레벨
    [SerializeField] private int UnDeadSkillPoint = 0;     // 언데드 스킬 현재 레벨
    [SerializeField] private int UnHolySkillPoint = 0;     // 언홀리 스킬 현재 레벨
    [SerializeField] private int EvilSkillPoint = 0;       // 이블 스킬 현재 레벨
    [SerializeField] private int SpecailSkillPoint = 0;    // 스페셜 유닛 스킬 현재 레벨
    [SerializeField] private int PopulationSkillPoint = 0; // 인구 스킬 현재 레벨
    [SerializeField] private int RuneSkillPoint = 0;       // 룬 스킬 현재 레벨 


    #endregion

    #region 스킬 비용 배열

    [Header("스킬 비용 배열 - 각 단계별 비용")]
    [SerializeField] private int[] ainSkillCosts = { 30, 60, 90 , 120 , 180 };
    [SerializeField] private int[] suinSkillCosts = { 30, 60, 90, 120, 180 };
    [SerializeField] private int[] undeadSkillCosts = { 30, 60, 90, 120, 180 };
    [SerializeField] private int[] unholySkillCosts = { 30, 60, 90, 120, 180 };
    [SerializeField] private int[] evilSkillCosts = { 30, 60, 90, 120, 180 };
    [SerializeField] private int[] specialSkillCosts = { 1, 1, 1 };
    [SerializeField] private int[] populationSkillCosts = { 60, 60, 60, 180, 180 };
    [SerializeField] private int[] runeSkillCosts = { 30, 30, 30, 30, 30};

    #endregion

    #region UI 버튼 참조

    [Header("종족별 스킬 버튼")]
    [SerializeField] private Button[] ainSkillButtons = new Button[5];
    [SerializeField] private Button[] suinSkillButtons = new Button[5];
    [SerializeField] private Button[] undeadSkillButtons = new Button[5];
    [SerializeField] private Button[] unholySkillButtons = new Button[5];
    [SerializeField] private Button[] evilSkillButtons = new Button[5];
    [SerializeField] private Button[] specialSkillButtons = new Button[3];

    [Header("코스트 스킬 버튼")]
    [SerializeField] private Button[] populationSkillButtons = new Button[5];
    [Header("룬 스킬 버튼")]
    [SerializeField] private Button[] runeSkillButtons = new Button[5];

    [Header("룬 잠금 표시용 이미지")]
    [SerializeField] private GameObject[] runeLockImage = new GameObject[5];
 
    #endregion

    #region UI 색상 설정

    [Header("UI 색상 설정")]
    [SerializeField] private Color availableColor = Color.white;      // 구매 가능
    [SerializeField] private Color unavailableColor = Color.gray;     // 구매 불가
    [SerializeField] private Color purchasedColor = Color.yellow;     // 구매 완료

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 초기화 메서드
    /// </summary>
    void Start()
    {
        // 필요한 컴포넌트들 찾기
        catalogCache = MonsterCatalog.Instance;
        manager = FindAnyObjectByType<UIManager>();
        skillInfoUI = FindAnyObjectByType<SkillUIManager>();

        // StaticInfoManager에서 스킬 레벨 불러오기
        LoadSkillLevelsFromStatic();

        // null 체크
        if (catalogCache == null)
            Debug.LogError("[SkillUpgrade] MonsterCatalog를 찾을 수 없습니다!");

        if (manager == null)
            Debug.LogError("[SkillUpgrade] UIManager를 찾을 수 없습니다!");

        if (skillInfoUI == null)
            Debug.LogWarning("[SkillUpgrade] SkillUIManager를 찾을 수 없습니다.");

        // 스킬 버튼 초기화
        InitializeSkillButtons();

        // 초기 UI 상태 업데이트
        UpdateAllSkillButtonStates();

        // 첫 번째 스킬 자동 선택 (상세 정보 표시)
        SelectFirstAvailableSkill();

    }

    #endregion

    #region StaticInfoManager 연동

    /// <summary>
    /// StaticInfoManager에서 스킬 레벨 불러오기
    /// 전투 후 메인씬 복귀 시 스킬 레벨 유지를 위해 필수
    /// </summary>
    private void LoadSkillLevelsFromStatic()
    {
        // StaticInfoManager에서 스킬 레벨 복원
        AinSkillPoint = StaticInfoManager.spMelee;
        SuinSkillPoint = StaticInfoManager.spRange;
        UnDeadSkillPoint = StaticInfoManager.spSpeical;
        UnHolySkillPoint = StaticInfoManager.spPor;
        EvilSkillPoint = StaticInfoManager.spEvil;
        SpecailSkillPoint = StaticInfoManager.spSpecial;
        PopulationSkillPoint = StaticInfoManager.spPopulation;
        RuneSkillPoint = StaticInfoManager.spRune;

        Debug.Log($"[SkillUpgrade] StaticInfoManager에서 스킬 레벨 불러옴 - Ain:{AinSkillPoint}, Suin:{SuinSkillPoint}, UnDead:{UnDeadSkillPoint}, UnHoly:{UnHolySkillPoint}, Evil:{EvilSkillPoint}, Special:{SpecailSkillPoint}, Population:{PopulationSkillPoint}, Rune:{RuneSkillPoint}");
    }

    /// <summary>
    /// StaticInfoManager에 스킬 레벨 저장
    /// 스킬 업그레이드 시 즉시 StaticInfoManager에 동기화하여 전투 후에도 유지
    /// </summary>
    private void SaveSkillLevelsToStatic()
    {
        // StaticInfoManager에 스킬 레벨 동기화
        StaticInfoManager.spMelee = AinSkillPoint;
        StaticInfoManager.spRange = SuinSkillPoint;
        StaticInfoManager.spSpeical = UnDeadSkillPoint;
        StaticInfoManager.spPor = UnHolySkillPoint;
        StaticInfoManager.spEvil = EvilSkillPoint;
        StaticInfoManager.spSpecial = SpecailSkillPoint;
        StaticInfoManager.spPopulation = PopulationSkillPoint;
        StaticInfoManager.spRune = RuneSkillPoint;

        Debug.Log($"[SkillUpgrade] StaticInfoManager에 스킬 레벨 저장 완료");
    }

    #endregion

    #region 스킬 버튼 초기화

    /// <summary>
    /// 스킬 버튼들 초기화 및 이벤트 연결
    /// </summary>
    private void InitializeSkillButtons()
    {
        // 아인족 스킬 버튼
        for (int i = 0; i < ainSkillButtons.Length; i++)
        {
            if (ainSkillButtons[i] != null)
            {
                int skillIndex = i;
                ainSkillButtons[i].onClick.RemoveAllListeners();
                ainSkillButtons[i].onClick.AddListener(() => ShowAinSkillInfo(skillIndex));
            }
        }

        // 수인족 스킬 버튼
        for (int i = 0; i < suinSkillButtons.Length; i++)
        {
            if (suinSkillButtons[i] != null)
            {
                int skillIndex = i;
                suinSkillButtons[i].onClick.RemoveAllListeners();
                suinSkillButtons[i].onClick.AddListener(() => ShowSuinSkillInfo(skillIndex));
            }
        }

        // 언데드 스킬 버튼
        for (int i = 0; i < undeadSkillButtons.Length; i++)
        {
            if (undeadSkillButtons[i] != null)
            {
                int skillIndex = i;
                undeadSkillButtons[i].onClick.RemoveAllListeners();
                undeadSkillButtons[i].onClick.AddListener(() => ShowUnDeadSkillInfo(skillIndex));
            }
        }

        // 언홀리 스킬 버튼
        for (int i = 0; i < unholySkillButtons.Length; i++)
        {
            if (unholySkillButtons[i] != null)
            {
                int skillIndex = i;
                unholySkillButtons[i].onClick.RemoveAllListeners();
                unholySkillButtons[i].onClick.AddListener(() => ShowUnHolySkillInfo(skillIndex));
            }
        }

        // 이블 스킬 버튼
        for (int i = 0; i < evilSkillButtons.Length; i++)
        {
            if (evilSkillButtons[i] != null)
            {
                int skillIndex = i;
                evilSkillButtons[i].onClick.RemoveAllListeners();
                evilSkillButtons[i].onClick.AddListener(() => ShowEvilSkillInfo(skillIndex));
            }
        }

        // 스페셜 스킬 버튼
        for (int i = 0; i < specialSkillButtons.Length; i++)
        {
            if (specialSkillButtons[i] != null)
            {
                int skillIndex = i;
                specialSkillButtons[i].onClick.RemoveAllListeners();
                specialSkillButtons[i].onClick.AddListener(() => ShowSpecialSkillInfo(skillIndex));
            }
        }

        // 인구 스킬 버튼
        for (int i = 0; i < populationSkillButtons.Length; i++)
        {
            if (populationSkillButtons[i] != null)
            {
                int skillIndex = i;
                populationSkillButtons[i].onClick.RemoveAllListeners();
                populationSkillButtons[i].onClick.AddListener(() => ShowPopulationSkillInfo(skillIndex));
            }
        }

        // 룬 스킬 버튼
        for(int i = 0; i < runeSkillButtons.Length; i++)
        {
            if (runeSkillButtons[i] != null)
            {
                int skillIndex = i;
                runeSkillButtons[i].onClick.RemoveAllListeners();
                runeSkillButtons[i].onClick.AddListener(() => ShowRuneSkillInfo(skillIndex));
            }
        }
    }

    /// <summary>
    /// 첫 번째 스킬 자동 선택 (패널 진입 시 상세 정보 표시)
    /// 아인족 스킬 첫 번째 버튼을 자동으로 클릭하여 상세 정보 표시
    /// </summary>
    private void SelectFirstAvailableSkill()
    {
        // 아인족 첫 번째 스킬 자동 선택
        if (ainSkillButtons != null && ainSkillButtons.Length > 0 && ainSkillButtons[0] != null)
        {
            ShowAinSkillInfo(0);
            Debug.Log("[SkillUpgrade] 첫 번째 스킬(Ain-0) 자동 선택됨");
        }
        else
        {
            Debug.LogWarning("[SkillUpgrade] 아인족 스킬 버튼을 찾을 수 없어 자동 선택 실패");
        }
    }

    #endregion

    #region 스킬 정보 창 표시 메서드들

    private void ShowAinSkillInfo(int skillIndex)
    {
        if (skillInfoUI == null)
        {
            TryPurchaseAinSkillDirect(skillIndex);
            return;
        }
        skillInfoUI.ShowSkillInfo(SkillType.Ain, skillIndex, OnSkillUpgradeRequested);
    }

    private void ShowSuinSkillInfo(int skillIndex)
    {
        if (skillInfoUI == null)
        {
            TryPurchaseSuinSkillDirect(skillIndex);
            return;
        }
        skillInfoUI.ShowSkillInfo(SkillType.Suin, skillIndex, OnSkillUpgradeRequested);
    }

    private void ShowUnDeadSkillInfo(int skillIndex)
    {
        if (skillInfoUI == null)
        {
            TryPurchaseUnDeadSkillDirect(skillIndex);
            return;
        }
        skillInfoUI.ShowSkillInfo(SkillType.UnDead, skillIndex, OnSkillUpgradeRequested);
    }

    private void ShowUnHolySkillInfo(int skillIndex)
    {
        if (skillInfoUI == null)
        {
            TryPurchaseUnHolySkillDirect(skillIndex);
            return;
        }
        skillInfoUI.ShowSkillInfo(SkillType.UnHoly, skillIndex, OnSkillUpgradeRequested);
    }

    private void ShowEvilSkillInfo(int skillIndex)
    {
        if (skillInfoUI == null)
        {
            TryPurchaseEvilSkillDirect(skillIndex);
            return;
        }
        skillInfoUI.ShowSkillInfo(SkillType.Evil, skillIndex, OnSkillUpgradeRequested);
    }

    private void ShowSpecialSkillInfo(int skillIndex)
    {
        if (skillInfoUI == null)
        {
            TryPurchaseSpecialSkillDirect(skillIndex);
            return;
        }
        skillInfoUI.ShowSkillInfo(SkillType.Special, skillIndex, OnSkillUpgradeRequested);
    }

    private void ShowPopulationSkillInfo(int skillIndex)
    {
        if (skillInfoUI == null)
        {
            TryPurchasePopulationSkillDirect(skillIndex);
            return;
        }
        skillInfoUI.ShowSkillInfo(SkillType.Population, skillIndex, OnSkillUpgradeRequested);
    }

    private void ShowRuneSkillInfo(int skillIndex)
    {
        if (skillInfoUI == null)
        {
            TryPurchaseRuneSkillDirect(skillIndex);
            return;
        }
        skillInfoUI.ShowSkillInfo(SkillType.Rune, skillIndex, OnSkillUpgradeRequested);
    }

    #endregion

    #region 스킬 
    
   

    /// <summary>
    /// 스킬 강화 요청을 받아 처리하는 콜백
    /// </summary>
    private void OnSkillUpgradeRequested(SkillType skillType, int skillIndex)
    {
        switch (skillType)
        {
            case SkillType.Ain:
                ExecuteAinSkillUpgrade(skillIndex);
                break;
            case SkillType.Suin:
                ExecuteSuinSkillUpgrade(skillIndex);
                break;
            case SkillType.UnDead:
                ExecuteUnDeadSkillUpgrade(skillIndex);
                break;
            case SkillType.UnHoly:
                ExecuteUnHolySkillUpgrade(skillIndex);
                break;
            case SkillType.Evil:
                ExecuteEvilSkillUpgrade(skillIndex);
                break;
            case SkillType.Special:
                ExecuteSpecialSkillUpgrade(skillIndex);
                break;
            case SkillType.Population:
                ExecutePopulationSkillUpgrade(skillIndex);
                break;
            case SkillType.Rune:
                ExecuteRuneSkillUpgrade(skillIndex);
                break;
            default:
                Debug.LogError($"[SkillUpgrade] 알 수 없는 스킬 타입: {skillType}");
                break;
        }
    }

    #endregion

    #region 실제 스킬 강화 실행 메서드들

    /// <summary>
    /// 아인족 스킬 강화 실행
    /// </summary>
    private void ExecuteAinSkillUpgrade(int skillIndex)
    {
        if (skillIndex != AinSkillPoint)
        {
            Debug.LogWarning($"[SkillUpgrade] 아인족 스킬 순차 해금 오류!");
            return;
        }

        int cost = ainSkillCosts[skillIndex];
        if (manager.playerDiamond < cost)
        {
            Debug.LogWarning($"[SkillUpgrade] 다이아몬드 부족! 필요: {cost}, 보유: {manager.playerDiamond}");
            return;
        }

        manager.playerDiamond -= cost;
        StaticInfoManager.dia = manager.playerDiamond;
        manager.UpdateUI();
        AinSkillPoint++;

        UpdateAllSkillButtonStates();
        ExecuteAinSkillEffect(skillIndex + 1);
        SaveSkillLevelsToStatic();
    }

    /// <summary>
    /// 수인족 스킬 강화 실행
    /// </summary>
    private void ExecuteSuinSkillUpgrade(int skillIndex)
    {
        if (skillIndex != SuinSkillPoint)
        {
            Debug.LogWarning($"[SkillUpgrade] 수인족 스킬 순차 해금 오류!");
            return;
        }

        int cost = suinSkillCosts[skillIndex];
        if (manager.playerDiamond < cost)
        {
            Debug.LogWarning($"[SkillUpgrade] 다이아몬드 부족!");
            return;
        }

        manager.playerDiamond -= cost;
        StaticInfoManager.dia = manager.playerDiamond;
        manager.UpdateUI();
        SuinSkillPoint++;

        UpdateAllSkillButtonStates();
        ExecuteSuinSkillEffect(skillIndex + 1);
        SaveSkillLevelsToStatic();
    }

    /// <summary>
    /// 언데드 스킬 강화 실행
    /// </summary>
    private void ExecuteUnDeadSkillUpgrade(int skillIndex)
    {
        if (skillIndex != UnDeadSkillPoint)
        {
            Debug.LogWarning($"[SkillUpgrade] 언데드 스킬 순차 해금 오류!");
            return;
        }

        int cost = undeadSkillCosts[skillIndex];
        if (manager.playerDiamond < cost)
        {
            Debug.LogWarning($"[SkillUpgrade] 다이아몬드 부족!");
            return;
        }

        manager.playerDiamond -= cost;
        StaticInfoManager.dia = manager.playerDiamond;
        manager.UpdateUI();
        UnDeadSkillPoint++;

        UpdateAllSkillButtonStates();
        ExecuteUnDeadSkillEffect(skillIndex + 1);
        SaveSkillLevelsToStatic();
    }

    /// <summary>
    /// 언홀리 스킬 강화 실행
    /// </summary>
    private void ExecuteUnHolySkillUpgrade(int skillIndex)
    {
        if (skillIndex != UnHolySkillPoint)
        {
            Debug.LogWarning($"[SkillUpgrade] 언홀리 스킬 순차 해금 오류!");
            return;
        }

        int cost = unholySkillCosts[skillIndex];
        if (manager.playerDiamond < cost)
        {
            Debug.LogWarning($"[SkillUpgrade] 다이아몬드 부족!");
            return;
        }

        manager.playerDiamond -= cost;
        StaticInfoManager.dia = manager.playerDiamond;
        manager.UpdateUI();
        UnHolySkillPoint++;

        UpdateAllSkillButtonStates();
        ExecuteUnHolySkillEffect(skillIndex + 1);
        SaveSkillLevelsToStatic();
    }

    /// <summary>
    /// 이블 스킬 강화 실행
    /// </summary>
    private void ExecuteEvilSkillUpgrade(int skillIndex)
    {
        if (skillIndex != EvilSkillPoint)
        {
            Debug.LogWarning($"[SkillUpgrade] 이블 스킬 순차 해금 오류!");
            return;
        }

        int cost = evilSkillCosts[skillIndex];
        if (manager.playerDiamond < cost)
        {
            Debug.LogWarning($"[SkillUpgrade] 다이아몬드 부족!");
            return;
        }

        manager.playerDiamond -= cost;
        StaticInfoManager.dia = manager.playerDiamond;
        manager.UpdateUI();
        EvilSkillPoint++;

        UpdateAllSkillButtonStates();
        ExecuteEvilSkillEffect(skillIndex + 1);
        SaveSkillLevelsToStatic();
    }

    /// <summary>
    /// 스페셜 스킬 강화 실행
    /// </summary>
    private void ExecuteSpecialSkillUpgrade(int skillIndex)
    {
        if (skillIndex != SpecailSkillPoint)
        {
            Debug.LogWarning($"[SkillUpgrade] 스페셜 스킬 순차 해금 오류!");
            return;
        }

        int cost = specialSkillCosts[skillIndex];
        if (manager.playerMithril < cost)
        {
            Debug.LogWarning($"[SkillUpgrade] 미스릴 부족!");
            return;
        }

        manager.playerMithril -= cost;
        StaticInfoManager.mis = manager.playerMithril;
        manager.UpdateUI();
        SpecailSkillPoint++;

        UpdateAllSkillButtonStates();
        ExecuteSpecialSkillEffect(skillIndex + 1);
        SaveSkillLevelsToStatic();
    }

    /// <summary>
    /// 인구 스킬 강화 실행
    /// </summary>
    private void ExecutePopulationSkillUpgrade(int skillIndex)
    {
        if (skillIndex != PopulationSkillPoint)
        {
            Debug.LogWarning($"[SkillUpgrade] 인구 스킬 순차 해금 오류!");
            return;
        }

        int cost = populationSkillCosts[skillIndex];
        if (manager.playerDiamond < cost)
        {
            Debug.LogWarning($"[SkillUpgrade] 다이아몬드 부족!");
            return;
        }

        manager.playerDiamond -= cost;
        StaticInfoManager.dia = manager.playerDiamond;
        manager.UpdateUI();
        PopulationSkillPoint++;

        try
        {
            ExecutePopulationSkillEffect(skillIndex + 1);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SkillUpgrade] PopulationSkillEffect 실행 중 에러 (무시됨): {e.Message}");
        }

        UpdateAllSkillButtonStates();
        SaveSkillLevelsToStatic();
    }

    /// <summary>
    /// 룬 스킬 강화 실행
    /// </summary>
    private void ExecuteRuneSkillUpgrade(int skillIndex)
    {
        if (skillIndex != RuneSkillPoint)
        {
            Debug.LogWarning($"[SkillUpgrade] 룬 스킬 순차 해금 오류!");
            return;
        }

        int cost = runeSkillCosts[skillIndex];
        if (manager.playerDiamond < cost)
        {
            Debug.LogWarning($"[SkillUpgrade] 다이아몬드 부족!");
            return;
        }

        manager.playerDiamond -= cost;
        StaticInfoManager.dia = manager.playerDiamond;
        manager.UpdateUI();
        RuneSkillPoint++;

        try
        {
            ExecuteRuneSkillEffect(skillIndex + 1);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SkillUpgrade] RuneSkillEffect 실행 중 에러 (무시됨): {e.Message}");
        }

        UpdateAllSkillButtonStates();
        SaveSkillLevelsToStatic();
    }

    #endregion

    #region 백업용 직접 구매 메서드들

    private void TryPurchaseAinSkillDirect(int skillIndex)
    {
        ExecuteAinSkillUpgrade(skillIndex);
    }

    private void TryPurchaseSuinSkillDirect(int skillIndex)
    {
        ExecuteSuinSkillUpgrade(skillIndex);
    }

    private void TryPurchaseUnDeadSkillDirect(int skillIndex)
    {
        ExecuteUnDeadSkillUpgrade(skillIndex);
    }

    private void TryPurchaseUnHolySkillDirect(int skillIndex)
    {
        ExecuteUnHolySkillUpgrade(skillIndex);
    }

    private void TryPurchaseEvilSkillDirect(int skillIndex)
    {
        ExecuteEvilSkillUpgrade(skillIndex);
    }

    private void TryPurchaseSpecialSkillDirect(int skillIndex)
    {
        ExecuteSpecialSkillUpgrade(skillIndex);
    }

    private void TryPurchasePopulationSkillDirect(int skillIndex)
    {
        ExecutePopulationSkillUpgrade(skillIndex);
    }

    private void TryPurchaseRuneSkillDirect(int skillIndex)
    {
        ExecuteRuneSkillUpgrade(skillIndex);
    }

    #endregion

    #region 공용 메서드들

    /// <summary>
    /// 특정 스킬 타입의 현재 레벨 반환
    /// </summary>
    public int GetSkillLevel(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Ain:
                return AinSkillPoint;
            case SkillType.Suin:
                return SuinSkillPoint;
            case SkillType.UnDead:
                return UnDeadSkillPoint;
            case SkillType.UnHoly:
                return UnHolySkillPoint;
            case SkillType.Evil:
                return EvilSkillPoint;
            case SkillType.Special:
                return SpecailSkillPoint;
            case SkillType.Population:
                return PopulationSkillPoint;
            case SkillType.Rune:
                return RuneSkillPoint;
            default:
                Debug.LogError($"[SkillUpgrade] 알 수 없는 스킬 타입: {skillType}");
                return 0;
        }
    }

    /// <summary>
    /// 다이아몬드 변경 시 호출
    /// </summary>
    public void OnDiamondChanged()
    {
        UpdateAllSkillButtonStates();

        if (skillInfoUI != null)
        {
            skillInfoUI.RefreshUI();
        }
    }

    #endregion

    #region UI 업데이트

    /// <summary>
    /// 모든 스킬 버튼 상태 업데이트
    /// </summary>
    private void UpdateAllSkillButtonStates()
    {
        UpdateSkillButtonStates(ainSkillButtons, AinSkillPoint, ainSkillCosts);
        UpdateSkillButtonStates(suinSkillButtons, SuinSkillPoint, suinSkillCosts);
        UpdateSkillButtonStates(undeadSkillButtons, UnDeadSkillPoint, undeadSkillCosts);
        UpdateSkillButtonStates(unholySkillButtons, UnHolySkillPoint, unholySkillCosts);
        UpdateSkillButtonStates(evilSkillButtons, EvilSkillPoint, evilSkillCosts);
        UpdateSkillButtonStates(specialSkillButtons, SpecailSkillPoint, specialSkillCosts);
        UpdateSkillButtonStates(populationSkillButtons, PopulationSkillPoint, populationSkillCosts);
        UpdateSkillButtonStates(runeSkillButtons, RuneSkillPoint, runeSkillCosts);
    }

    /// <summary>
    /// 특정 스킬 버튼들 상태 업데이트
    /// </summary>
    private void UpdateSkillButtonStates(Button[] buttons, int currentLevel, int[] costs)
    {
        if (buttons == null || costs == null || manager == null) return;

        for (int i = 0; i < buttons.Length && i < costs.Length; i++)
        {
            if (buttons[i] == null) continue;

            Image buttonImage = buttons[i].GetComponent<Image>();
            if (buttonImage == null) continue;

            if (i < currentLevel)
            {
                // 이미 구매한 스킬
                buttons[i].interactable = true;
                buttonImage.color = purchasedColor;
            }
            else if (i == currentLevel)
            {
                // 구매 가능한 다음 스킬
                buttons[i].interactable = true;
                buttonImage.color = (manager.playerDiamond >= costs[i]) ? availableColor : unavailableColor;
            }
            else
            {
                // 잠긴 스킬
                buttons[i].interactable = true;
                buttonImage.color = unavailableColor;
            }
        }
    }

    #endregion

    #region 스킬 효과 실행 메서드들 (비어있음 - 추후 구현)

    /// <summary>
    /// 아인족 스킬 효과 (추후 구현)
    /// </summary>
    private void ExecuteAinSkillEffect(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                AinSkill1();
                break;
            case 2:
                AinSkill2();
                break;
            case 3:
                AinSkill3();
                break;
            case 4:
                AinSkill4();
                break;
            case 5:
                AinSkill5();
                break;
        }
    }

    /// <summary>
    /// 수인족 스킬 효과 (추후 구현)
    /// </summary>
    private void ExecuteSuinSkillEffect(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                SuinSkill1();
                break;
            case 2:
                SuinSkill2();
                break;
            case 3:
                SuinSkill3();
                break;
            case 4:
                SuinSkill4();
                break;
            case 5:
                SuinSkill5();
                break;
        }
    }

    /// <summary>
    /// 언데드 스킬 효과 (추후 구현)
    /// </summary>
    private void ExecuteUnDeadSkillEffect(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                UnDeadSkill1();
                break;
            case 2:
                UnDeadSkill2();
                break;
            case 3:
                UnDeadSkill3();
                break;
            case 4:
                UnDeadSkill4();
                break;
            case 5:
                UnDeadSkill5();
                break;
        }
    }

    /// <summary>
    /// 언홀리 스킬 효과 (추후 구현)
    /// </summary>
    private void ExecuteUnHolySkillEffect(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                UnHolySkill1();
                break;
            case 2:
                UnHolySkill2();
                break;
            case 3:
                UnHolySkill3();
                break;
            case 4:
                UnHolySkill4();
                break;
            case 5:
                UnHolySkill5();
                break;
        }
    }

    /// <summary>
    /// 이블 스킬 효과 (추후 구현)
    /// </summary>
    private void ExecuteEvilSkillEffect(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                EvilSkill1();
                break;
            case 2:
                EvilSkill2();
                break;
            case 3:
                EvilSkill3();
                break;
            case 4:
                EvilSkill4();
                break;
            case 5:
                EvilSkill5();
                break;
        }
    }

    /// <summary>
    /// 스페셜 스킬 효과 (추후 구현)
    /// </summary>
    private void ExecuteSpecialSkillEffect(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                SpecialSkill1();
                break;
            case 2:
                SpecialSkill2();
                break;
            case 3:
                SpecialSkill3();
                break;
        }
    }

    /// <summary>
    /// 인구 스킬 효과 (코스트 증가)
    /// </summary>
    private void ExecutePopulationSkillEffect(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                PopulationSkill1();
                break;
            case 2:
                PopulationSkill2();
                break;
            case 3:
                PopulationSkill3();
                break;
            case 4:
                PopulationSkill4();
                break;
            case 5:
                PopulationSkill5();
                break;
        }
    }

    /// <summary>
    /// 룬 스킬 효과 (추후 구현)
    /// </summary>
    private void ExecuteRuneSkillEffect(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                RuneSkill1();
                break;
            case 2:
                RuneSkill2();
                break;
            case 3:
                RuneSkill3();
                break;
            case 4:
                RuneSkill4();
                break;
            case 5:
                RuneSkill5();
                break;
        }
    }

    /// <summary>
    /// 특정 종족의 쿨타임을 1턴 감소 (StaticInfoManager에 기록)
    /// 실제 유닛 생성 시 MonsterManager에서 적용
    /// </summary>
    /// <param name="raceCategory">종족 카테고리 ("Ain", "Suin", "UnDead", "UnHoly", "Evil", "Special")</param>
    private void ApplyRaceCooldownReduction(string raceCategory)
    {
        // StaticInfoManager에 쿨타임 감소 기록
        if (!StaticInfoManager.raceCooldownReduction.ContainsKey(raceCategory))
        {
            StaticInfoManager.raceCooldownReduction[raceCategory] = 0f;
        }

        // 쿨타임 1턴 감소 (누적)
        StaticInfoManager.raceCooldownReduction[raceCategory] += 1f;

        Debug.Log($"[SkillUpgrade] {raceCategory} 종족 쿨타임 -{StaticInfoManager.raceCooldownReduction[raceCategory]}턴 (2턴 이하로 감소 불가)");
    }

    /// <summary>
    /// 특정 종족의 패시브 업그레이드를 true로 변경
    /// </summary>
    /// <param name="raceCategory"></param>
    private void ApplyRacePassiveUpgrade(string raceCategory)
    {
        if (!StaticInfoManager.racePassiveUpgrade.ContainsKey(raceCategory))
        {
            StaticInfoManager.racePassiveUpgrade[raceCategory] = true;
        }

        Debug.Log($"[SkillUpgrade] {raceCategory} 종족 패시브 강화 완료!");
    }

    #endregion

    #region 개별 스킬 효과 메서드들

    // ===== 아인족 스킬 =====
    private void AinSkill1()
    {
        // 아인족 HP +10% (합연산)
        ApplyRaceHPBonus("아인", 0.1f);
        Debug.Log("[아인 Skill 1] 아인족 HP +10% 적용");
    }

    private void AinSkill2()
    {
        // 아인족 공격력 +10% (합연산)
        ApplyRaceDamageBonus("아인", 0.1f);
        Debug.Log("[Ain Skill 2] 아인족 공격력 +10% 적용");
    }

    private void AinSkill3()
    {
        // 쿨타임 감소(2턴 이하로 감소 불가)
        ApplyRaceCooldownReduction("아인");
        Debug.Log("[Ain Skill 3] 아인족 쿨타임 -1턴 적용");
    }

    private void AinSkill4()
    {
        // 아인족 HP +20% 및 공격력 +20% (합연산)
        ApplyRaceHPBonus("아인", 0.2f);
        ApplyRaceDamageBonus("아인", 0.2f);
    }

    private void AinSkill5()
    {
        ApplyRacePassiveUpgrade("아인");
    }

    // ===== 수인족 스킬 =====
    private void SuinSkill1()
    {
        // 수인족 공격력 +10% (합연산)
        ApplyRaceDamageBonus("수인", 0.1f);
        Debug.Log("[Suin Skill 1] 수인족 데미지 +10% 적용");
    }

    private void SuinSkill2()
    {
        // 수인족 속도 1 증가
        ApplyRaceSpeedBonus("수인", 1);
        Debug.Log("[Suin Skill 2] 수인족 속도 1 증가 적용");
    }

    private void SuinSkill3()
    {
        // 쿨타임 감소 (2턴 이하로 감소 불가)
        ApplyRaceCooldownReduction("수인");
        Debug.Log("[Suin Skill 3] 수인족 쿨타임 -1턴 적용");
    }

    private void SuinSkill4()
    {
        //수인족 공격력 체력 20% 증가 (합연산)
        ApplyRaceHPBonus("수인", 0.2f);
        ApplyRaceDamageBonus("수인", 0.2f);
    }

    private void SuinSkill5()
    {
        ApplyRacePassiveUpgrade("수인");
    }

    // ===== 언데드 스킬 =====
    private void UnDeadSkill1()
    {
        // 언데드 HP +10% (합연산)
        ApplyRaceHPBonus("언데드", 0.1f);
        Debug.Log("[UnDead Skill 1] 언데드 HP +10% 적용");
    }

    private void UnDeadSkill2()
    {
        // 언데드 체력 +15% (합연산)
        ApplyRaceHPBonus("언데드", 0.15f);
        Debug.Log("[UnDead Skill 2] 언데드 체력 +15% 적용");
    }

    private void UnDeadSkill3()
    {
        // 언데드족 모든 유닛 쿨타임 1 감소 (2턴 이하로 감소 불가)
        ApplyRaceCooldownReduction("언데드");
        Debug.Log("[UnDead Skill 3] 언데드족 쿨타임 -1턴 적용");
    }

    private void UnDeadSkill4()
    {
        // 언데드 체력 +30% (합연산)
        ApplyRaceHPBonus("언데드", 0.30f);
        Debug.Log("[UnDead Skill 4] 언데드 체력 +30% 적용");
    }

    private void UnDeadSkill5()
    {
        ApplyRacePassiveUpgrade("언데드");
    }

    // ===== 타락 스킬 =====
    private void UnHolySkill1()
    {
        // 타락 공격력 +10% (합연산)
        ApplyRaceDamageBonus("타락", 0.1f);
        Debug.Log("[UnHoly Skill 1] 타락 공격력 +10% 적용");
    }

    private void UnHolySkill2()
    {
        // 타락 체력 +10% (합연산)
        ApplyRaceHPBonus("타락", 0.1f);
        Debug.Log("[UnHoly Skill 2] 타락 체력 +10% 적용");
    }

    private void UnHolySkill3()
    {
        // 쿨타임 1턴 감소(2턴이하로 감소 불가)
        ApplyRaceCooldownReduction("타락");
        Debug.Log("[UnHoly Skill 3] 타락족 쿨타임 -1턴 적용");
    }

    private void UnHolySkill4()
    {
        ApplyRaceDamageBonus("타락", 0.3f);
        Debug.Log("[UnHoly Skill 4] 타락 공격력 +30% 적용");
    }

    private void UnHolySkill5()
    {
        ApplyRacePassiveUpgrade("타락");
    }

    // ===== 마족 스킬 =====
    private void EvilSkill1()
    {
        // 마족 공격력 +10% (합연산)
        ApplyRaceDamageBonus("마족", 0.1f);
        Debug.Log("[Evil Skill 1] 마족 공격력 +10% 적용");
    }

    private void EvilSkill2()
    {
        // 마족 최소 최대 이속 1 증가
        ApplyRaceSpeedBonus("마족", 1);
        Debug.Log("[Evil Skill 2] 마족 속도 +1 적용");
    }

    private void EvilSkill3()
    {
        // 마족 쿨타임 감소 1턴 (2턴 이하로 감소 불가)
        ApplyRaceCooldownReduction("마족");
        Debug.Log("[Evil Skill 3] 마족 쿨타임 -1턴 적용");
    }

    private void EvilSkill4()
    {
        // 마족 공격력 +30% (합연산)
        ApplyRaceDamageBonus("마족", 0.3f);
        Debug.Log("[Evil Skill 1] 마족 공격력 +30% 적용");
    }

    private void EvilSkill5()
    {
        ApplyRacePassiveUpgrade("마족");
    }

    // ===== 스페셜 스킬 =====
    private void SpecialSkill1()
    {
        // 스페셜 HP +20% & 공격력 +20% (합연산, 동시)
        ApplyRaceHPBonus("Special", 0.2f);
        ApplyRaceDamageBonus("Special", 0.2f);
        Debug.Log("[Special Skill 1] 스페셜 HP+공격력 +20% 적용");
    }

    private void SpecialSkill2()
    {
        // 스페셜 몬스터 스킬 쿨타임 감소 1턴 (2턴 이하로 감소 불가)
        ApplyRaceCooldownReduction("Special");
        Debug.Log("[Special Skill 2] 스페셜 유닛 쿨타임 -1턴 적용");
    }

    private void SpecialSkill3()
    {
        ApplyRacePassiveUpgrade("Special");
    }

    // ===== 인구 스킬 (배치 수 증가) =====
    private void PopulationSkill1()
    {
        StaticInfoManager.maxCostSquad1 += 15;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Population Skill 1] 1군단 최대 코스트 +15 (현재: {StaticInfoManager.maxCostSquad1})");
    }

    private void PopulationSkill2()
    {
        StaticInfoManager.maxCostSquad2 += 15;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Population Skill 2] 2군단 최대 코스트 +15 (현재: {StaticInfoManager.maxCostSquad2})");
    }

    private void PopulationSkill3()
    {
        StaticInfoManager.maxCostSquad3 += 15;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Population Skill 3] 3군단 최대 코스트 +15 (현재: {StaticInfoManager.maxCostSquad3})");
    }

    private void PopulationSkill4()
    {
        StaticInfoManager.maxCostSquad1 += 5;
        StaticInfoManager.maxCostSquad2 += 5;
        StaticInfoManager.maxCostSquad3 += 5;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Population Skill 4] 전군단 최대 코스트 +5");
    }

    private void PopulationSkill5()
    {
        StaticInfoManager.maxCostSquad1 += 5;
        StaticInfoManager.maxCostSquad2 += 5;
        StaticInfoManager.maxCostSquad3 += 5;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Population Skill 5] 전군단 코스트 +5");
    }

    // ===== 룬 스킬 =====
    private void RuneSkill1()
    {
        StaticInfoManager.squad2RuneSlots = 1;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Rune Skill 1] 제2부대 룬 슬롯 1개 해금");
    }

    private void RuneSkill2()
    {
        StaticInfoManager.squad3RuneSlots = 1;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Rune Skill 2] 제3부대 룬 슬롯 1개 해금");
    }

    private void RuneSkill3()
    {
        // 부대 1에 룬 2개 장착 가능
        StaticInfoManager.squad1RuneSlots = 2;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Rune Skill 3] 제1부대 룬 슬롯 2개로 확장");
    }

    private void RuneSkill4()
    {
        StaticInfoManager.squad2RuneSlots = 2;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Rune Skill 4] 제2부대 룬 슬롯 2개로 확장");
    }

    private void RuneSkill5()
    {
        StaticInfoManager.squad3RuneSlots = 2;
        if (SquadFormationManager.Instance != null)
        {
            SquadFormationManager.Instance.ReFreshUI();
        }
        Debug.Log($"[Rune Skill 5] 제3부대 룬 슬롯 2개로 확장");
    }

    #endregion

    #region 스킬 복원용 메서드

    /// <summary>
    /// 저장된 스킬 레벨 복원 (SaveSystem에서 호출)
    /// </summary>
    public void SetSkillLevels(int ain, int suin, int undead, int unholy, int evil, int specail, int population, int rune, bool refreshUI)
    {
        AinSkillPoint = ain;
        SuinSkillPoint = suin;
        UnDeadSkillPoint = undead;
        UnHolySkillPoint = unholy;
        EvilSkillPoint = evil;
        SpecailSkillPoint = specail;
        PopulationSkillPoint = population;
        RuneSkillPoint = rune;

        UpdateAllSkillButtonStates();
    }

    /// <summary>
    /// 특정 스킬 효과를 수동으로 실행 (치트/디버그용)
    /// </summary>
    public void ExecuteSkillEffect(SkillType type, int skillLevel)
    {
        switch (type)
        {
            case SkillType.Ain:
                ExecuteAinSkillEffect(skillLevel);
                break;
            case SkillType.Suin:
                ExecuteSuinSkillEffect(skillLevel);
                break;
            case SkillType.UnDead:
                ExecuteUnDeadSkillEffect(skillLevel);
                break;
            case SkillType.UnHoly:
                ExecuteUnHolySkillEffect(skillLevel);
                break;
            case SkillType.Evil:
                ExecuteEvilSkillEffect(skillLevel);
                break;
            case SkillType.Special:
                ExecuteSpecialSkillEffect(skillLevel);
                break;
            case SkillType.Population:
                ExecutePopulationSkillEffect(skillLevel);
                break;
            case SkillType.Rune:
                ExecuteRuneSkillEffect(skillLevel);
                break;
        }
    }

    #endregion

    #region 종족별 스탯 증가 헬퍼 메서드

    /// <summary>
    /// 특정 종족의 모든 유닛 HP를 합연산으로 증가
    /// </summary>
    /// <param name="raceCategory">종족 카테고리 ("Ain", "Suin", "UnDead", "UnHoly", "Evil", "Special")</param>
    /// <param name="bonusRate">증가율 (0.1 = 10%, 0.2 = 20%)</param>
    private void ApplyRaceHPBonus(string raceCategory, float bonusRate)
    {
        if (catalogCache == null || !catalogCache.IsReady)
        {
            Debug.LogWarning("[SkillUpgrade] MonsterCatalog이 준비되지 않았습니다.");
            return;
        }

        int affectedCount = 0;

        // 일반 유닛 적용
        foreach (var unit in catalogCache.normalUnits)
        {
            if (unit.category == raceCategory)
            {
                // 처음 강화 시 원본 HP 백업
                if (unit.baseHp == 0f)
                {
                    unit.baseHp = unit.hp;
                }

                // 합연산: 원본 HP + (원본 HP * 증가율)
                unit.hp = unit.baseHp + (unit.baseHp * bonusRate);
                affectedCount++;
            }
        }

        // 특수 유닛 적용 (raceCategory == "Special"인 경우)
        if (raceCategory == "Special")
        {
            foreach (var unit in catalogCache.specialUnits)
            {
                // group 필드가 "Special"인 유닛만 적용
                if (!string.IsNullOrEmpty(unit.group) && unit.group == "Special")
                {
                    if (unit.baseHp == 0f)
                    {
                        unit.baseHp = unit.hp;
                    }

                    unit.hp = unit.baseHp + (unit.baseHp * bonusRate);
                    affectedCount++;
                }
            }
        }

        Debug.Log($"[SkillUpgrade] {raceCategory} 종족 {affectedCount}개 유닛의 HP 증가 (+{bonusRate * 100}%)");
    }

    /// <summary>
    /// 특정 종족의 모든 유닛 공격력을 합연산으로 증가
    /// </summary>
    /// <param name="raceCategory">종족 카테고리 ("Ain", "Suin", "UnDead", "UnHoly", "Evil", "Special")</param>
    /// <param name="bonusRate">증가율 (0.1 = 10%, 0.2 = 20%)</param>
    private void ApplyRaceDamageBonus(string raceCategory, float bonusRate)
    {
        if (catalogCache == null || !catalogCache.IsReady)
        {
            Debug.LogWarning("[SkillUpgrade] MonsterCatalog이 준비되지 않았습니다.");
            return;
        }

        int affectedCount = 0;

        // 일반 유닛 적용
        foreach (var unit in catalogCache.normalUnits)
        {
            if (unit.category == raceCategory)
            {
                // 처음 강화 시 원본 공격력 백업
                if (unit.baseDamage == 0f)
                {
                    unit.baseDamage = unit.damage;
                }

                // 합연산: 원본 공격력 + (원본 공격력 * 증가율)
                unit.damage = unit.baseDamage + (unit.baseDamage * bonusRate);
                affectedCount++;
            }
        }

        // 특수 유닛 적용 (raceCategory == "Special"인 경우)
        if (raceCategory == "Special")
        {
            foreach (var unit in catalogCache.specialUnits)
            {
                // group 필드가 "Special"인 유닛만 적용
                if (!string.IsNullOrEmpty(unit.group) && unit.group == "Special")
                {
                    if (unit.baseDamage == 0f)
                    {
                        unit.baseDamage = unit.damage;
                    }

                    unit.damage = unit.baseDamage + (unit.baseDamage * bonusRate);
                    affectedCount++;
                }
            }
        }

        Debug.Log($"[SkillUpgrade] {raceCategory} 종족 {affectedCount}개 유닛의 공격력 증가 (+{bonusRate * 100}%)");
    }

    /// <summary>
    /// 특정 종족의 모든 유닛 이속 증가
    /// </summary>
    /// <param name="raceCategory">종족 카테고리 ("Ain", "Suin", "UnDead", "UnHoly", "Evil", "Special")</param>
    /// <param name="bonusRate">증가율 (0.1 = 10%, 0.2 = 20%)</param>
    private void ApplyRaceSpeedBonus(string raceCategory, float bonusAmount)
    {
        if (catalogCache == null || !catalogCache.IsReady)
        {
            Debug.LogWarning("[SkillUpgrade] MonsterCatalog이 준비되지 않았습니다.");
            return;
        }
        foreach (var unit in catalogCache.normalUnits)
        {
            if (unit.category == raceCategory)
            {
                    unit.minSpeed += bonusAmount;
                    unit.maxSpeed += bonusAmount;     
            }
        }
    }
}

#endregion