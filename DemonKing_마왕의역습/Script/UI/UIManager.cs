using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System;
using UnityEngine.Rendering;

/// <summary>
/// 게임 UI 매니저 (MonsterCatalog 호환 버전)
/// 상점, 인벤토리, 설정 등 모든 UI 패널 관리
/// </summary>
public partial class UIManager : MonoBehaviour
{
    #region UI 패널들

    [Header("UI 패널들")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject specialShopPanel;
    [SerializeField] private GameObject superSpecailShopPanel;
    [SerializeField] private GameObject inventoryPanel;        // 인벤토리 패널 (부모)
    [SerializeField] private GameObject settingPanel;
    [SerializeField] public GameObject windowBg;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject exitPanel;
    [SerializeField] private GameObject minePanel;
    [SerializeField] private GameObject badEventPanel;
    [SerializeField] private GameObject saveCompletePanel;
    [SerializeField] private GameObject mineInfoPanel;
    [SerializeField] private GameObject combatRewardPanel; // 전투 보상 패널

    [Header("상점 추가 패널")]
    [SerializeField] private GameObject shopRunePanel;           // 일반 상점 - 룬 구매 패널

    [Header("교환소 추가 패널 (일반 교환소)")]
    [SerializeField] private GameObject exchangeResourcePanel;   // 교환소 - 재화 교환 패널
    [SerializeField] private GameObject exchangeRunePanel;       // 교환소 - 룬 구매 패널

    [Header("특수교환소 추가 패널 (비밀 교환소)")]
    [SerializeField] private GameObject secretResourcePanel;     // 특수교환소 - 재화 교환 패널
    [SerializeField] private GameObject secretRunePanel;         // 특수교환소 - 룬 구매 패널

    #endregion

    #region 상점 UI 구성요소들

    [Header("상점 UI 구성요소")]
    [SerializeField] private Transform shopItemParent;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private MonsterDetailPanel shopDetailPanel;        // 상점 전용 DetailPanel
    [SerializeField] private Transform shopRuneItemParent;              // 상점 룬 아이템 부모
    [SerializeField] private GameObject shopRuneItemPrefab;             // 상점 룬 아이템 프리팹
    [SerializeField] private RuneDetailPanel shopRuneDetailPanel;       // 상점 룬 상세 패널

    [Header("교환소 UI 구성요소")]
    [SerializeField] private Transform specialShopItemParent;
    [SerializeField] private GameObject specialShopItemPrefab;
    [SerializeField] private MonsterDetailPanel exchangeDetailPanel;    // 교환소 전용 DetailPanel
    [SerializeField] private Transform exchangeRuneItemParent;          // 교환소 룬 아이템 부모
    [SerializeField] private GameObject exchangeRuneItemPrefab;         // 교환소 룬 아이템 프리팹
    [SerializeField] private RuneDetailPanel exchangeRuneDetailPanel;   // 교환소 룬 상세 패널

    [Header("특수교환소 UI 구성요소")]
    [SerializeField] private Transform superSpecialShopItemParent;
    [SerializeField] private GameObject superSpecialShopItemPrefab;
    [SerializeField] private MonsterDetailPanel secretDetailPanel;      // 특수교환소 전용 DetailPanel
    [SerializeField] private Transform secretRuneItemParent;            // 특수교환소 룬 아이템 부모
    [SerializeField] private GameObject secretRuneItemPrefab;           // 특수교환소 룬 아이템 프리팹
    [SerializeField] private RuneDetailPanel secretRuneDetailPanel;     // 특수교환소 룬 상세 패널

    [Header("인벤토리 UI (스크롤뷰 토글)")]
    [SerializeField] private GameObject allyScrollView;        // 아군 스크롤뷰
    [SerializeField] private GameObject enemyScrollView;       // 적군 스크롤뷰
    [SerializeField] private GameObject runeScrollView;        // 룬 도감 스크롤뷰
    [SerializeField] private GameObject guidBookPanel;         // 가이드 판넬
    [SerializeField] private Transform inventoryItemParent;    // 아군 Grid Parent
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private GameObject inventorySpecialItemPrefab;

    #endregion

    #region 플레이어 자원 관리

    [Header("플레이어 자원 UI")]
    [SerializeField] private Text goldText;
    [SerializeField] private Text ironText;
    [SerializeField] private Text MithrilText;
    [SerializeField] private Text DiamondText;

    [SerializeField] public int playerGold = 0;
    [SerializeField] public int playerIron = 0;
    [SerializeField] public int playerMithril = 0;
    [SerializeField] public int playerDiamond = 0;

    #endregion

    #region 의존성 및 외부 참조

    [Header("인벤토리 및 도감 관리자")]
    [SerializeField] private InventoryUIManager inventoryUIManager;
    [SerializeField] private EnemyBestiaryManager enemyBestiaryManager;
    [SerializeField] private RuneCodexManager runeCodexManager;

    private MonsterPurchaseService purchaseService;
    private SMonsterPurchaseService specialPurchaseService;
    private SkillUpgrade skillUpgrade;
    private MonsterCatalog catalogCache;

    #endregion

    #region 유니티 라이프사이클

    void Start()
    {
        InitializeServices();
        InitializeUI();
    }

    void Update()
    {
        HandleEscapeKey();
    }

    #endregion

    #region 초기화 메서드들

    /// <summary>
    /// 필요한 서비스들 초기화
    /// </summary>
    private void InitializeServices()
    {
        purchaseService = FindAnyObjectByType<MonsterPurchaseService>();
        specialPurchaseService = FindAnyObjectByType<SMonsterPurchaseService>();
        skillUpgrade = FindAnyObjectByType<SkillUpgrade>();
        catalogCache = MonsterCatalog.Instance;

        if (inventoryUIManager == null)
            inventoryUIManager = FindAnyObjectByType<InventoryUIManager>();

        if (runeCodexManager == null)
            runeCodexManager = FindAnyObjectByType<RuneCodexManager>();

        playerGold = StaticInfoManager.gold;
        playerIron = StaticInfoManager.iron;
        playerDiamond = StaticInfoManager.dia;
        playerMithril = StaticInfoManager.mis;

        ValidateServices();
    }

    /// <summary>
    /// 서비스 유효성 검사
    /// </summary>
    private void ValidateServices()
    {
        if (purchaseService == null)
            Debug.LogError("MonsterPurchaseService를 찾을 수 없습니다!");

        if (specialPurchaseService == null)
            Debug.LogError("SMonsterPurchaseService를 찾을 수 없습니다!");

        if (skillUpgrade == null)
            Debug.LogError("SkillUpgrade를 찾을 수 없습니다");

        if (catalogCache == null)
            Debug.LogError("MonsterCatalog를 찾을 수 없습니다!");
    }

    /// <summary>
    /// UI 초기 설정
    /// </summary>
    private void InitializeUI()
    {
        UpdateUI();
        /*GameObject sitemObj = Instantiate(superSpecialShopItemPrefab, superSpecialShopItemParent);*/
    }

    /// <summary>
    /// ESC 키 처리
    /// </summary>
    private void HandleEscapeKey()
    {
        // 튜토리얼 시작 패널이 활성화되어 있는지 체크
        TutorialStartPanel startPanel = FindAnyObjectByType<TutorialStartPanel>();
        bool isTutorialStartPanelVisible = (startPanel != null && startPanel.gameObject.activeSelf);

        // 튜토리얼 모드거나 시작 패널이 보이면 ESC 무시
        if (Input.GetKeyDown(KeyCode.Escape) && !TutorialManager.IsTutorialMode && !isTutorialStartPanelVisible)
        {
                if (settingPanel.activeInHierarchy)
                    OffSetting();
                else
                    ShowSettingPanel();

        }
    }

    #endregion

    #region 패널 관리 메서드들

    /// <summary>
    /// 패널 표시
    /// </summary>
    public void OnShopTabClicked()
    {
        ShowShopPanel();
    }

    public void OnSpecialShopTabClicked()
    {
        ShowSpecailShopPanel();
    }

    public void OnSuperSpecialShopTabClicked()
    {
        ShowSuperSpecailShopPanel();
    }

    public void OnInventoryTabClicked()
    {
        ShowInventoryPanel();
    }

    public void OnSettingTabClicked()
    {
        ShowSettingPanel();
    }

    public void OnExitTabClicked()
    {
        ShowExitPanel();
    }

    #region 상점별 탭 전환 메서드들

    // ===== 상점 (shopPanel) 탭들 =====

    /// <summary>
    /// 상점 - 몬스터 탭 (일반 몬스터 구매)
    /// </summary>
    public void OnShopMonsterTabClicked()
    {
        // 룬 패널 닫고 몬스터 리스트 Parent 켜기
        if (shopRunePanel != null) shopRunePanel.SetActive(false);
        if (shopItemParent != null) shopItemParent.gameObject.SetActive(true);
        ShowShopPanel(false); // 구매 모드
    }

    /// <summary>
    /// 상점 - 되팔기 탭 (보유 몬스터 판매)
    /// </summary>
    public void OnShopSellTabClicked()
    {
        // 룬 패널 닫고 몬스터 리스트 Parent 켜기
        if (shopRunePanel != null) shopRunePanel.SetActive(false);
        if (shopItemParent != null) shopItemParent.gameObject.SetActive(true);
        ShowShopPanel(true); // 판매 모드
    }

    /// <summary>
    /// 상점 - 룬 페이지 탭
    /// </summary>
    public void OnShopRuneTabClicked()
    {
        if (shopRunePanel != null)
        {
            // 몬스터 리스트 Parent 끄고 룬 패널 열기
            if (shopItemParent != null) shopItemParent.gameObject.SetActive(false);
            shopRunePanel.SetActive(true);

            // 룬 상점 UI 업데이트
            UpdateShopRuneUI();

            Debug.Log("[UIManager] 상점 - 룬 페이지 열림");
        }
        else
        {
            Debug.LogWarning("[UIManager] shopRunePanel이 연결되지 않았습니다!");
        }
    }

    /// <summary>
    /// 상점 - 재화 교환 탭 (일반 상점에는 재화 교환 없음)
    /// </summary>
    public void OnShopResourceTabClicked()
    {
        // 일반 상점에는 재화 교환 없음 (교환소/특수교환소만 가능)
        Debug.Log("[UIManager] 상점 - 재화 교환은 교환소에서만 이용 가능합니다.");
    }

    // ===== 교환소 (specialShopPanel) 탭들 =====

    /// <summary>
    /// 교환소 - 몬스터 탭 (특수 몬스터, shop=true)
    /// </summary>
    public void OnExchangeMonsterTabClicked()
    {
        // 룬/재화 패널 닫고 몬스터 리스트 Parent 켜기
        if (exchangeRunePanel != null) exchangeRunePanel.SetActive(false);
        if (exchangeResourcePanel != null) exchangeResourcePanel.SetActive(false);
        if (specialShopItemParent != null) specialShopItemParent.gameObject.SetActive(true);
        ShowSpecailShopPanel();
    }

    /// <summary>
    /// 교환소 - 룬 페이지 탭
    /// </summary>
    public void OnExchangeRuneTabClicked()
    {
        if (exchangeRunePanel != null)
        {
            // 몬스터 리스트 Parent + 재화 패널 끄고 룬 패널 열기
            if (specialShopItemParent != null) specialShopItemParent.gameObject.SetActive(false);
            if (exchangeResourcePanel != null) exchangeResourcePanel.SetActive(false);
            exchangeRunePanel.SetActive(true);

            // 교환소 룬 UI 업데이트
            UpdateExchangeRuneUI();

            Debug.Log("[UIManager] 교환소 - 룬 페이지 열림");
        }
        else
        {
            Debug.LogWarning("[UIManager] exchangeRunePanel이 연결되지 않았습니다!");
        }
    }

    /// <summary>
    /// 교환소 - 재화 교환 탭
    /// </summary>
    public void OnExchangeResourceTabClicked()
    {
        if (exchangeResourcePanel != null)
        {
            // 몬스터 리스트 Parent + 룬 패널 끄고 재화 교환 패널 열기
            if (specialShopItemParent != null) specialShopItemParent.gameObject.SetActive(false);
            if (exchangeRunePanel != null) exchangeRunePanel.SetActive(false);
            exchangeResourcePanel.SetActive(true);
            Debug.Log("[UIManager] 교환소 - 재화 교환 패널 열림");
        }
        else
        {
            Debug.LogWarning("[UIManager] exchangeResourcePanel이 연결되지 않았습니다!");
        }
    }

    // ===== 특수교환소 (superSpecailShopPanel) 탭들 =====

    /// <summary>
    /// 특수교환소 - 몬스터 탭 (특수 몬스터, shop=false)
    /// </summary>
    public void OnSecretExchangeMonsterTabClicked()
    {
        // 룬/재화 패널 닫고 몬스터 리스트 Parent 켜기
        if (secretRunePanel != null) secretRunePanel.SetActive(false);
        if (secretResourcePanel != null) secretResourcePanel.SetActive(false);
        if (superSpecialShopItemParent != null) superSpecialShopItemParent.gameObject.SetActive(true);
        ShowSuperSpecailShopPanel();
    }

    /// <summary>
    /// 특수교환소 - 룬 페이지 탭
    /// </summary>
    public void OnSecretExchangeRuneTabClicked()
    {
        if (secretRunePanel != null)
        {
            // 몬스터 리스트 Parent + 재화 패널 끄고 룬 패널 열기
            if (superSpecialShopItemParent != null) superSpecialShopItemParent.gameObject.SetActive(false);
            if (secretResourcePanel != null) secretResourcePanel.SetActive(false);
            secretRunePanel.SetActive(true);

            // 특수교환소 룬 UI 업데이트 (새로고침은 층수 변경 시에만)
            UpdateSecretRuneUI();

            Debug.Log("[UIManager] 특수교환소 - 룬 페이지 열림");
        }
        else
        {
            Debug.LogWarning("[UIManager] secretRunePanel이 연결되지 않았습니다!");
        }
    }

    /// <summary>
    /// 특수교환소 - 재화 교환 탭
    /// </summary>
    public void OnSecretExchangeResourceTabClicked()
    {
        if (secretResourcePanel != null)
        {
            // 몬스터 리스트 Parent + 룬 패널 끄고 재화 교환 패널 열기
            if (superSpecialShopItemParent != null) superSpecialShopItemParent.gameObject.SetActive(false);
            if (secretRunePanel != null) secretRunePanel.SetActive(false);
            secretResourcePanel.SetActive(true);
            Debug.Log("[UIManager] 특수교환소 - 재화 교환 패널 열림");
        }
        else
        {
            Debug.LogWarning("[UIManager] secretResourcePanel이 연결되지 않았습니다!");
        }
    }

    #endregion

    public void OnSkillTabClicked()
    {
        ShowSkillPanel();
    }
    public void OnCompleteSave()
    {
        ShowcCmpleteSavePanel();
    }

    /// <summary>
    /// 패널 닫기 
    /// </summary>
    public void OffShopTabClicked() => OffShop();
    public void OffSpecialShopTabClicked() => OffSpecialShop();
    public void OffSuperSpecialShopTabClicked() => OffSuperSpecialShop();
    public void OffInventoryTabClicked() => OffInventory();
    public void OffSettingTabClicked() => OffSetting();
    public void OffExitTabClicked() => OffExit();
    public void OffRootTabClicked() => OffRootPanel();
    public void OffSkillTabClicked() => OffSkill();
    public void OffMineTabClicked() => OffMine();
    public void OffEventTabClicked() => OffBadEvent();
    public void OffSaveClicked() => OffSave();

    /// <summary>
    /// 스테이지 진행
    /// </summary>
    public void OnStage()
    {
        StaticInfoManager.floor++;
        Debug.Log($"현재 스테이지: {StaticInfoManager.floor}");
    }

    #endregion

    #region 패널 표시 메서드들



    private void ShowSettingPanel()
    {
        SetActivePanel(settingPanel);
    }

    private void ShowShopPanel(bool sellMode = false)
    {
        SetActivePanel(shopPanel);
        UpdateShopUI(sellMode);
    }

    private void ShowSpecailShopPanel()
    {
        SetActivePanel(specialShopPanel);
        UpdateSpecialShopUI();
    }

    private void ShowSuperSpecailShopPanel()
    {
        SetActivePanel(superSpecailShopPanel);
        UpdateSuperSpecialShopUI();
    }

    /// <summary>
    /// 교환소 열기 (외부에서 호출 가능)
    /// StageInfoPanel에서 교환소 노드 클릭 시 사용
    /// 기본적으로 몬스터 탭을 표시
    /// </summary>
    public void OpenExchangeShop()
    {
        ShowSpecailShopPanel(); // 기본: 몬스터 탭 표시
        Debug.Log("[UIManager] 교환소 열림 (몬스터 탭)");
    }

    /// <summary>
    /// 특수교환소 열기 (외부에서 호출 가능)
    /// StageInfoPanel에서 비밀 교환소 이벤트 발생 시 사용
    /// 기본적으로 몬스터 탭을 표시
    /// </summary>
    public void OpenSecretExchangeShop()
    {
        ShowSuperSpecailShopPanel(); // 기본: 몬스터 탭 표시
        Debug.Log("[UIManager] 특수교환소 열림 (몬스터 탭)");
    }

    private void ShowInventoryPanel()
    {
        SetActivePanel(inventoryPanel);
        Debug.Log("=== 인벤토리 패널 표시 ===");

        if (inventoryUIManager != null)
        {
            if (enemyScrollView.activeSelf)
            {
                enemyBestiaryManager.UpdateBestiaryUI();
            }
            else if(runeScrollView.activeSelf)
            {
                runeCodexManager.UpdateFullCodexUI();
            }
            else if(allyScrollView.activeSelf)
            inventoryUIManager.UpdateFullInventoryUI();
        }
        else
            UpdateInventoryUI();
    }

    /// <summary>
    /// 아군 도감 버튼 클릭 (아군 스크롤뷰 ON, 적군/룬 스크롤뷰 OFF)
    /// </summary>
    public void ShowAllyInventory()
    {
        // 인벤토리 패널은 유지, 스크롤뷰만 토글
        if (allyScrollView != null)
            allyScrollView.SetActive(true);

        if (enemyScrollView != null)
            enemyScrollView.SetActive(false);

        if (runeScrollView != null)
            runeScrollView.SetActive(false);

        if(guidBookPanel != null)
            guidBookPanel.SetActive(false);

        // 인벤토리 UI 갱신
        if (inventoryUIManager != null)
            inventoryUIManager.UpdateFullInventoryUI();
    }

    /// <summary>
    /// 적군 도감 버튼 클릭 (적군 스크롤뷰 ON, 아군 스크롤뷰 OFF)
    /// </summary>
    public void ShowEnemyBestiary()
    {
        // 인벤토리 패널은 유지, 스크롤뷰만 토글
        if (allyScrollView != null)
            allyScrollView.SetActive(false);

        if (enemyScrollView != null)
            enemyScrollView.SetActive(true);

        if (runeScrollView != null)
            runeScrollView.SetActive(false);

        if (guidBookPanel != null)
            guidBookPanel.SetActive(false);

        // 적군 도감 UI 갱신
        if (enemyBestiaryManager != null)
            enemyBestiaryManager.UpdateBestiaryUI();

    }

    /// <summary>
    /// 룬 도감 버튼 클릭 (룬 스크롤뷰 ON, 아군/적군 스크롤뷰 OFF)
    /// </summary>
    public void ShowRuneCodex()
    {
        // 인벤토리 패널은 유지, 스크롤뷰만 토글
        if (allyScrollView != null)
            allyScrollView.SetActive(false);

        if (enemyScrollView != null)
            enemyScrollView.SetActive(false);

        if (runeScrollView != null)
            runeScrollView.SetActive(true);

        if (guidBookPanel != null)
            guidBookPanel.SetActive(false);

        // 룬 도감 UI 갱신
        if (runeCodexManager != null)
            runeCodexManager.UpdateFullCodexUI();
        else
            Debug.LogWarning("[UIManager] RuneCodexManager를 찾을 수 없습니다!");

    }

    public void ShowGuideBoock()
    {
        if (allyScrollView != null)
            allyScrollView.SetActive(false);

        if (enemyScrollView != null)
            enemyScrollView.SetActive(false);

        if (runeScrollView != null)
            runeScrollView.SetActive(false);

        if (guidBookPanel != null)
            guidBookPanel.SetActive(true);
    }

    private void ShowExitPanel()
    {
        exitPanel.SetActive(true);
    }

    private void ShowSkillPanel()
    {
        SetActivePanel(skillPanel);
    }

    private void ShowcCmpleteSavePanel()
    {
        SetActiveSavePanel(saveCompletePanel);
    }

    /// <summary>
    /// 공통 패널 활성화 로직
    /// </summary>
    private void SetActivePanel(GameObject targetPanel)
    {
        bool wasShopOpen = specialShopPanel.activeSelf || superSpecailShopPanel.activeSelf;

        // 모든 패널 비활성화
        shopPanel.SetActive(false);
        specialShopPanel.SetActive(false);
        superSpecailShopPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        settingPanel.SetActive(false);
        skillPanel.SetActive(false);

        // 타겟 패널과 배경 활성화
        targetPanel.SetActive(true);
        windowBg.SetActive(true);

        // 상점을 닫았다면 노드 이동
        if (wasShopOpen && MapSelectionController.Instance != null)
        {
            Debug.Log("[UIManager] 상점 닫힘 - 노드 이동 트리거");
            MapSelectionController.Instance.OnNonCombatStageComplete();
        }
    }

    private void SetActiveSavePanel(GameObject targetPanel)
    {
        // 셋팅 창 제외 비활성화
        shopPanel.SetActive(false);
        specialShopPanel.SetActive(false);
        superSpecailShopPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        skillPanel.SetActive(false);

        // 타겟 패널과 배경 활성화
        targetPanel.SetActive(true);
        windowBg.SetActive(true);
    }

    #endregion

    #region 패널 숨기기 메서드들

    private void OffRootPanel() => rootPanel.SetActive(false);
    private void OffShop() => HidePanel();
    private void OffInventory() => HidePanel();
    private void OffSetting() => HidePanel();
    private void OffSkill() => HidePanel();
    private void OffExit() => exitPanel.SetActive(false);
    private void OffSave() => HideSavePanel();

    private void OffSpecialShop()
    {
        HidePanel();
        MapSelectionController.Instance.OnNonCombatStageComplete();
    }

    private void OffSuperSpecialShop()
    {
        HidePanel();
        MapSelectionController.Instance.OnNonCombatStageComplete();
    }

    private void OffMine()
    {
        minePanel.SetActive(false);
        MapSelectionController.Instance.OnNonCombatStageComplete();
    }

    private void OffBadEvent()
    {
        badEventPanel.SetActive(false);
        MapSelectionController.Instance.OnNonCombatStageComplete();
    }



    /// <summary>
    /// 공통 패널 숨기기
    /// </summary>
    private void HidePanel()
    {
        bool wasShopOpen = specialShopPanel.activeSelf || superSpecailShopPanel.activeSelf;

        shopPanel.SetActive(false);
        specialShopPanel.SetActive(false);
        superSpecailShopPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        settingPanel.SetActive(false);
        skillPanel.SetActive(false);
        windowBg.SetActive(false);
        saveCompletePanel.SetActive(false);

        // 상점을 닫았다면 노드 이동
        if (wasShopOpen && MapSelectionController.Instance != null)
        {
            Debug.Log("[UIManager] 상점 닫힘 - 노드 이동 트리거");
            MapSelectionController.Instance.OnNonCombatStageComplete();
        }
    }

    private void HideSavePanel()
    {
        shopPanel.SetActive(false);
        specialShopPanel.SetActive(false);
        superSpecailShopPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        skillPanel.SetActive(false);
        saveCompletePanel.SetActive(false);
    }

    #endregion

    #region UI 업데이트 메서드들

    /// <summary>
    /// 전체 UI 업데이트
    /// </summary>
    /// <param name="refreshPanels">패널 내용을 재생성할지 여부 (기본값: false)</param>
    public void UpdateUI(bool refreshPanels = false)
    {
        // StaticInfoManager에서 최신 자원 값 동기화
        SyncFromStaticInfoManager();

        UpdateGoldUI();
        UpdateIronUI();
        UpdateMithrilUI();
        UpdateDiamondUI();

        // refreshPanels이 true일 때만 패널 재생성
        if (refreshPanels)
        {
            // 활성 패널에 따른 업데이트
            if (shopPanel.activeInHierarchy)
                UpdateShopUI();
            else if (specialShopPanel.activeInHierarchy)
                UpdateSpecialShopUI();
            else if (superSpecailShopPanel.activeInHierarchy)
                UpdateSuperSpecialShopUI();
            else if (inventoryPanel.activeInHierarchy)
                UpdateInventoryUI();
        }
    }

    /// <summary>
    /// StaticInfoManager에서 자원 값 동기화
    /// 튜토리얼 모드일 경우 튜토리얼 재화를 사용
    /// </summary>
    private void SyncFromStaticInfoManager()
    {
        if (TutorialManager.IsTutorialMode)
        {
            // 튜토리얼 모드: 가상 재화 사용
            playerGold = TutorialManager.Instance.tutorialGold;
            playerIron = TutorialManager.Instance.tutorialIron;
            playerDiamond = StaticInfoManager.dia;  // 다이아와 미스릴은 튜토리얼에서 사용 안 함
            playerMithril = StaticInfoManager.mis;
        }
        else
        {
            // 일반 모드: StaticInfoManager 값 사용
            playerGold = StaticInfoManager.gold;
            playerIron = StaticInfoManager.iron;
            playerDiamond = StaticInfoManager.dia;
            playerMithril = StaticInfoManager.mis;
        }
    }

    /// <summary>
    /// 자원별 UI 업데이트
    /// </summary>
    private void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"{playerGold}";
    }

    private void UpdateIronUI()
    {
        if (ironText != null)
            ironText.text = $"{playerIron}";
    }

    private void UpdateMithrilUI()
    {
        if (MithrilText != null)
            MithrilText.text = $"{playerMithril}";

        NotifySkillUpgrade();
    }

    private void UpdateDiamondUI()
    {
        if (DiamondText != null)
            DiamondText.text = $"{playerDiamond}";

        NotifySkillUpgrade();
    }

    /// <summary>
    /// 스킬 업그레이드에 자원 변경 알림
    /// </summary>
    private void NotifySkillUpgrade()
    {
        if (skillUpgrade != null)
            skillUpgrade.OnDiamondChanged();
    }

    #endregion

    #region 상점 UI 업데이트 메서드들

    /// <summary>
    /// 일반 상점 UI 업데이트
    /// </summary>
    /// <param name="sellMode">판매 모드 여부 (true = 판매, false = 구매)</param>
    private void UpdateShopUI(bool sellMode = false)
    {
        if (purchaseService == null || shopItemParent == null || shopItemPrefab == null)
            return;

        ClearChildren(shopItemParent);

        var shopItems = sellMode ? purchaseService.GetInventory() : purchaseService.GetShopItems();
        Debug.Log($"[UpdateShopUI] 상점 아이템 개수: {shopItems?.Count ?? 0}, 판매 모드: {sellMode}");

        bool isFirstItem = true;
        if (shopItems != null && shopItems.Count > 0)
        {
            foreach (var item in shopItems)
            {
                CreateShopItem(item, sellMode);

                // 첫 번째 아이템 자동 선택
                if (isFirstItem && shopDetailPanel != null)
                {
                    shopDetailPanel.ShowMonsterDetail(item, this);
                    isFirstItem = false;
                }
            }
        }

        // 구매 모드일 때만 잠긴 몬스터 미리보기 추가
        if (!sellMode)
        {
            var lockedItems = purchaseService.GetLockedMonstersPreview();
            foreach (var item in lockedItems)
            {
                CreateShopItem(item, sellMode);
            }
        }
    }

    /// <summary>
    /// 일반 교환소 UI 업데이트 (shop = true)
    /// </summary>
    private void UpdateSpecialShopUI()
    {
        if (specialPurchaseService == null || specialShopItemParent == null || specialShopItemPrefab == null)
            return;

        ClearChildren(specialShopItemParent);

        var normalShopItems = specialPurchaseService.GetNormalShopItems();
        bool isFirstItem = true;
        foreach (var item in normalShopItems)
        {
            CreateSpecialShopItem(item);

            // 첫 번째 아이템 자동 선택
            if (isFirstItem && exchangeDetailPanel != null)
            {
                exchangeDetailPanel.ShowMonsterDetail(item, this);
                isFirstItem = false;
            }
        }
    }

    /// <summary>
    /// 비밀 교환소 UI 업데이트 (shop = false)
    /// </summary>
    private void UpdateSuperSpecialShopUI()
    {
        if (specialPurchaseService == null || superSpecialShopItemParent == null || superSpecialShopItemPrefab == null)
            return;

        ClearChildren(superSpecialShopItemParent);

        var secretShopItems = specialPurchaseService.GetSecretShopItems();
        bool isFirstItem = true;
        foreach (var item in secretShopItems)
        {
            CreateSuperSpecialShopItem(item);

            // 첫 번째 아이템 자동 선택
            if (isFirstItem && secretDetailPanel != null)
            {
                secretDetailPanel.ShowMonsterDetail(item, this);
                isFirstItem = false;
            }
        }
    }

    /// <summary>
    /// 인벤토리 UI 업데이트 (호환성용)
    /// </summary>
    private void UpdateInventoryUI()
    {
        if (purchaseService == null || inventoryItemParent == null) return;

        ClearChildren(inventoryItemParent);

        // 일반 몬스터
        var inventory = purchaseService.GetInventory();
        foreach (var item in inventory)
        {
            CreateInventoryItem(item);
        }

        // 특수 몬스터
        var specialInventory = specialPurchaseService.GetInventory();
        foreach (var specialItem in specialInventory)
        {
            CreateSpecialInventoryItem(specialItem);
        }
    }

    #endregion

    #region 상점 아이템 생성 메서드들

    /// <summary>
    /// 일반 상점 아이템 생성
    /// </summary>
    private void CreateShopItem(UnitStock monster, bool sellMode = false)
    {
        GameObject itemObj = Instantiate(shopItemPrefab, shopItemParent);
        var buttonController = itemObj.GetComponent<ShopMonsterButtonController>();

        if (buttonController != null)
        {
            buttonController.SetupMonster(monster, this, shopDetailPanel, sellMode);
        }
        else
        {
            SetupShopItemManually(itemObj, monster);
        }
    }

    /// <summary>
    /// 특수 상점 아이템 생성 (일반 교환소)
    /// </summary>
    private void CreateSpecialShopItem(UnitStock specialMonster)
    {
        GameObject itemObj = Instantiate(specialShopItemPrefab, specialShopItemParent);
        var buttonController = itemObj.GetComponent<ShopSMonsterButtonController>();

        if (buttonController != null)
        {
            buttonController.SetupMonster(specialMonster, this, exchangeDetailPanel);
        }
        else
        {
            SetupSpecialShopItemManually(itemObj, specialMonster);
        }
    }

    /// <summary>
    /// 특수 상점 아이템 생성 (비밀 교환소)
    /// </summary>
    private void CreateSuperSpecialShopItem(UnitStock specialMonster)
    {
        GameObject itemObj = Instantiate(superSpecialShopItemPrefab, superSpecialShopItemParent);
        var buttonController = itemObj.GetComponent<ShopSMonsterButtonController>();

        if (buttonController != null)
        {
            buttonController.SetupMonster(specialMonster, this, secretDetailPanel);
        }
        else
        {
            SetupSpecialShopItemManually(itemObj, specialMonster);
        }
    }

    /// <summary>
    /// 인벤토리 아이템 생성
    /// </summary>
    private void CreateInventoryItem(UnitStock monster)
    {
        GameObject itemObj = Instantiate(inventoryItemPrefab, inventoryItemParent);
        SetupInventoryItemManually(itemObj, monster, false);
    }

    private void CreateSpecialInventoryItem(UnitStock specialMonster)
    {
        GameObject itemObj = Instantiate(inventorySpecialItemPrefab, inventoryItemParent);
        SetupInventoryItemManually(itemObj, specialMonster, true);
    }

    #endregion

    #region 수동 설정 메서드들

    /// <summary>
    /// 수동 상점 아이템 설정 (호환성용)
    /// </summary>
    private void SetupShopItemManually(GameObject itemObj, UnitStock monster)
    {
        var nameText = itemObj.transform.Find("MonsterName")?.GetComponent<Text>();
        var priceText = itemObj.transform.Find("Price")?.GetComponent<Text>();
        var iconImage = itemObj.transform.Find("MonsterIcon")?.GetComponent<Image>();
        var buyButton = itemObj.transform.Find("BuyButton")?.GetComponent<Button>();

        if (nameText != null) nameText.text = monster.displayName;
        if (priceText != null) priceText.text = $"{monster.price_Gold} 골드";
        if (iconImage != null && monster.icon != null) iconImage.sprite = monster.icon;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => OnBuyButtonClicked(monster.enemyId));
        }
    }

    private void SetupSpecialShopItemManually(GameObject itemObj, UnitStock specialMonster)
    {
        var nameText = itemObj.transform.Find("MonsterName")?.GetComponent<Text>();
        var priceText = itemObj.transform.Find("Price")?.GetComponent<Text>();
        var iconImage = itemObj.transform.Find("MonsterIcon")?.GetComponent<Image>();
        var buyButton = itemObj.transform.Find("BuyButton")?.GetComponent<Button>();

        var meta = specialPurchaseService.GetSpecialMeta(specialMonster.enemyId);

        if (nameText != null) nameText.text = specialMonster.displayName;
        if (priceText != null) priceText.text = $"{meta.price} 미스릴";
        if (iconImage != null && specialMonster.icon != null) iconImage.sprite = specialMonster.icon;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => OnSpecialBuyButtonClicked(specialMonster.enemyId));
        }
    }

    private void SetupInventoryItemManually(GameObject itemObj, UnitStock monster, bool isSpecial)
    {
        var nameText = itemObj.transform.Find("MonsterName")?.GetComponent<Text>();
        var countText = itemObj.transform.Find("OwnedCount")?.GetComponent<Text>();
        var iconImage = itemObj.transform.Find("MonsterIcon")?.GetComponent<Image>();
        var sellButton = itemObj.transform.Find("SellButton")?.GetComponent<Button>();

        if (nameText != null) nameText.text = monster.displayName;
        if (countText != null) countText.text = $"{monster.owned}";
        if (iconImage != null && monster.icon != null) iconImage.sprite = monster.icon;

        if (sellButton != null && !isSpecial) // 일반 몬스터만 판매 가능
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => OnSellButtonClicked(monster.enemyId));
        }
    }

    #endregion

    #region 구매 이벤트 처리 메서드들

    /// <summary>
    /// 일반 몬스터 구매 버튼 클릭 (외부 호출용)
    /// </summary>
    public void OnMonsterPurchaseClicked(string enemyId)
    {
        OnBuyButtonClicked(enemyId);
    }

    /// <summary>
    /// 특수 몬스터 구매 버튼 클릭 (외부 호출용)
    /// </summary>
    public void OnSpecialMonsterPurchaseClicked(string enemyId)
    {
        OnSpecialBuyButtonClicked(enemyId);
    }

    /// <summary>
    /// 일반 몬스터 구매 처리
    /// </summary>
    private void OnBuyButtonClicked(string enemyId)
    {
        var result = purchaseService.PurchaseMonster(enemyId, 1, playerGold, playerIron);

        if (result.success)
        {
            playerGold = result.newGoldAmount;
            playerIron = result.newIronAmount;

            // 튜토리얼 모드에서는 상점 패널도 새로고침해야 보유량이 표시됨
            bool refreshShop = TutorialManager.IsTutorialMode;
            UpdateUI(refreshPanels: refreshShop);

            if (inventoryUIManager != null)
                inventoryUIManager.UpdateFullInventoryUI();

            // 튜토리얼 모드에서는 MonsterCatalog 이벤트 발생
            if (TutorialManager.IsTutorialMode)
            {
                MonsterCatalog.Instance?.NotifyUnitsChanged();
            }
        }
        else
        {
            Debug.LogWarning($"구매 실패: {result.message}");
        }
    }

    /// <summary>
    /// 특수 몬스터 구매 처리
    /// </summary>
    private void OnSpecialBuyButtonClicked(string enemyId)
    {
        var result = specialPurchaseService.PurchaseMonster(enemyId, 1, playerMithril);

        if (result.success)
        {
            playerMithril = result.newMithrilAmount;

            UpdateUI(refreshPanels: true);  // 상점 갱신

            if (inventoryUIManager != null)
                inventoryUIManager.UpdateFullInventoryUI();
        }
        else
        {
            Debug.LogWarning($"구매 실패: {result.message}");
        }
    }

    /// <summary>
    /// 몬스터 판매 처리
    /// </summary>
    private void OnSellButtonClicked(string enemyId)
    {
        var result = purchaseService.SellMonster(enemyId, 1);

        if (result.success)
        {
            playerGold += result.goldEarned;
            Debug.Log($"판매 성공: {result.message}");
            UpdateUI(refreshPanels: false);  // 자원 표시만 업데이트
        }
        else
        {
            Debug.LogWarning($"판매 실패: {result.message}");
        }
    }

    #endregion

    #region 외부 호출 업데이트 메서드들

    /// <summary>
    /// 인벤토리에서 골드 변경 시 호출
    /// </summary>
    public void UpdateGoldFromInventory(int goldEarned)
    {
        playerGold += goldEarned;
        UpdateGoldUI();
    }

    /// <summary>
    /// 인벤토리에서 철 변경 시 호출
    /// </summary>
    public void UpdateIronFromInventory(int ironEarned)
    {
        playerIron += ironEarned;
        UpdateIronUI();
    }

    #endregion

    #region 유틸리티 메서드들

    /// <summary>
    /// 자식 오브젝트 정리
    /// </summary>
    private void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    #endregion

    #region 이벤트 처리 메서드들

    /// <summary>
    /// 광산 이벤트
    /// </summary>
    public void Mining()
    {

        StaticInfoManager.isMine = true;
        
        int plusIron = 180 + 15 * StaticInfoManager.floor;
        int plusDiamond = 108 + 9 * StaticInfoManager.floor;
        int plusMithril = 1;

        
        playerIron += plusIron;
        playerDiamond += plusDiamond;
        playerMithril += plusMithril;

        StaticInfoManager.iron = playerIron;
        StaticInfoManager.dia = playerDiamond;
        StaticInfoManager.mis = playerMithril;

        UpdateUI(refreshPanels: false);  // 자원 표시만 업데이트
        
        // MineInfoPanel에 보상 정보 전달
        var mineInfo = mineInfoPanel.GetComponent<MineInfoPanel>();
        if (mineInfo != null)
        {
            mineInfo.ShowRewards(plusIron, plusDiamond, plusMithril);
        }
        
        mineInfoPanel.SetActive(true);
        StaticInfoManager.isMining = false;
    }

    public void MineInfoPanelOff()
    {
        mineInfoPanel.SetActive(false);
        OffMineTabClicked();
    }

    /// <summary>
    /// 전투 보상 패널 표시
    /// </summary>
    public void ShowCombatRewardPanel()
    {
        Debug.Log($"[UIManager] ShowCombatRewardPanel 호출됨 - hasCombatReward={StaticInfoManager.hasCombatReward}");

        if (!StaticInfoManager.hasCombatReward)
        {
            Debug.LogWarning("[UIManager] 전투 보상 없음 - 패널 표시 안 함");
            return;
        }

        Debug.Log($"[UIManager] 보상 데이터: 골드={StaticInfoManager.actualRewardGold}, 철={StaticInfoManager.actualRewardIron}, 다이아={StaticInfoManager.actualRewardDiamond}, 미스릴={StaticInfoManager.actualRewardMithril}, 룬={StaticInfoManager.actualRewardRuneName}");

        if (combatRewardPanel == null)
        {
            Debug.LogError("[UIManager] combatRewardPanel이 null입니다! Unity Inspector에서 연결하세요.");
            return;
        }

        var combatReward = combatRewardPanel.GetComponent<CombatRewardPanel>();
        if (combatReward != null)
        {
            Debug.Log("[UIManager] CombatRewardPanel 컴포넌트 찾음 - ShowRewards 호출");
            combatReward.ShowRewards(
                StaticInfoManager.actualRewardGold,
                StaticInfoManager.actualRewardIron,
                StaticInfoManager.actualRewardDiamond,
                StaticInfoManager.actualRewardMithril,
                StaticInfoManager.actualRewardRuneName
            );
        }
        else
        {
            Debug.LogError("[UIManager] CombatRewardPanel 컴포넌트를 찾을 수 없습니다!");
        }

        combatRewardPanel.SetActive(true);
        Debug.Log("[UIManager] combatRewardPanel 활성화 완료");

        // 보상 표시 후 플래그 초기화
        StaticInfoManager.hasCombatReward = false;
        StaticInfoManager.actualRewardGold = 0;
        StaticInfoManager.actualRewardIron = 0;
        StaticInfoManager.actualRewardDiamond = 0;
        StaticInfoManager.actualRewardMithril = 0;
        StaticInfoManager.actualRewardRuneName = "";
    }

    /// <summary>
    /// 전투 보상 패널 닫기
    /// </summary>
    public void CombatRewardPanelOff()
    {
        combatRewardPanel.SetActive(false);
    }

    /// <summary>
    /// 악운 이벤트
    /// </summary>
    public void BadEvent()
    {
        StaticInfoManager.isBadEvent = true;

        int minusIron = 100 + 15 * StaticInfoManager.floor;
        int minusDiamond = 36 + 3 * StaticInfoManager.floor;
        int minusMithril = 1;

        playerIron = Mathf.Max(0, playerIron - minusIron);
        playerDiamond = Mathf.Max(0, playerDiamond - minusDiamond);
        playerMithril = Mathf.Max(0, playerMithril - minusMithril);

        UpdateUI(refreshPanels: false);  // 자원 표시만 업데이트
        OffEventTabClicked();  // 즉시 창 닫기 (버튼 중복 클릭 방지)
    }

    #endregion

    #region 카메라 컨트롤 메서드들

    /// <summary>
    /// 게임 저장
    /// </summary>
    public void OnClick_SaveGame()
    {
        SaveSystem.Save();
        Debug.Log("저장 완료");
    }

    #endregion

    #region 스태틱인포 매니저 자원
    public void BackupToStatic()
    {
        StaticInfoManager.gold = playerGold;
        StaticInfoManager.dia = playerDiamond;
        StaticInfoManager.mis = playerMithril;
        StaticInfoManager.iron = playerIron;
    }

    public void RestoreToStatic()
    {
       playerGold = StaticInfoManager.gold;
       playerDiamond =StaticInfoManager.dia;
       playerMithril =StaticInfoManager.mis;
       playerIron =StaticInfoManager.iron;

        UpdateUI();

    }


    #endregion

    #region 상점에서 판매 기능

    /// <summary>
    /// 상점에서 몬스터 판매 처리
    /// MonsterDetailPanel의 판매 버튼에서 호출됨
    /// </summary>
    /// <param name="monster">판매할 몬스터</param>
    public void SellMonsterFromShop(UnitStock monster)
    {
        if (purchaseService == null || monster == null)
        {
            Debug.LogWarning("[UIManager] 판매 서비스 또는 몬스터 정보가 없습니다!");
            return;
        }

        var result = purchaseService.SellMonster(monster.enemyId, 1);

        if (result.success)
        {
            Debug.Log($"[UIManager] 판매 성공: {result.message}");

            // 골드/철 업데이트
            playerGold += result.goldEarned;
            playerIron += result.ironEarned;
            StaticInfoManager.gold = playerGold;
            StaticInfoManager.iron = playerIron;
            UpdateUI();

            // 상점 UI 갱신 (판매 모드 유지)
            UpdateShopUI(true);

            Debug.Log($"[UIManager] 판매 완료 - 골드 +{result.goldEarned}, 철 +{result.ironEarned}");
        }
        else
        {
            Debug.LogWarning($"[UIManager] 판매 실패: {result.message}");
        }
    }

    #endregion

    #region 룬 상점 UI 업데이트

    /// <summary>
    /// 일반 상점 룬 UI 업데이트 (커먼 등급)
    /// </summary>
    private void UpdateShopRuneUI()
    {
        if (shopRuneItemParent == null || shopRuneItemPrefab == null)
        {
            Debug.LogWarning("[UIManager] shopRuneItemParent 또는 shopRuneItemPrefab이 연결되지 않았습니다!");
            return;
        }

        ClearChildren(shopRuneItemParent);

        var runes = RuneShopService.Instance.GetNormalShopRunes();
        Debug.Log($"[UpdateShopRuneUI] 커먼 룬 개수: {runes?.Count ?? 0}");

        bool isFirstItem = true;
        if (runes != null && runes.Count > 0)
        {
            foreach (var rune in runes)
            {
                int price = RuneShopService.Instance.GetRunePrice(rune);
                CreateShopRuneItem(rune, price);

                // 첫 번째 아이템 자동 선택
                if (isFirstItem && shopRuneDetailPanel != null)
                {
                    shopRuneDetailPanel.ShowRuneDetail(rune, price, this);
                    isFirstItem = false;
                }
            }
        }
    }

    /// <summary>
    /// 교환소 룬 UI 업데이트 (레어 등급)
    /// </summary>
    private void UpdateExchangeRuneUI()
    {
        if (exchangeRuneItemParent == null || exchangeRuneItemPrefab == null)
        {
            Debug.LogWarning("[UIManager] exchangeRuneItemParent 또는 exchangeRuneItemPrefab이 연결되지 않았습니다!");
            return;
        }

        ClearChildren(exchangeRuneItemParent);

        var runes = RuneShopService.Instance.GetExchangeShopRunes();
        Debug.Log($"[UpdateExchangeRuneUI] 레어 룬 개수: {runes?.Count ?? 0}");

        bool isFirstItem = true;
        if (runes != null && runes.Count > 0)
        {
            foreach (var rune in runes)
            {
                int price = RuneShopService.Instance.GetRunePrice(rune);
                CreateExchangeRuneItem(rune, price);

                // 첫 번째 아이템 자동 선택
                if (isFirstItem && exchangeRuneDetailPanel != null)
                {
                    exchangeRuneDetailPanel.ShowRuneDetail(rune, price, this);
                    isFirstItem = false;
                }
            }
        }
    }

    /// <summary>
    /// 특수 교환소 룬 UI 업데이트 (에픽 등급)
    /// </summary>
    private void UpdateSecretRuneUI()
    {
        if (secretRuneItemParent == null || secretRuneItemPrefab == null)
        {
            Debug.LogWarning("[UIManager] secretRuneItemParent 또는 secretRuneItemPrefab이 연결되지 않았습니다!");
            return;
        }

        ClearChildren(secretRuneItemParent);

        var runes = RuneShopService.Instance.GetSecretShopRunes();
        Debug.Log($"[UpdateSecretRuneUI] 에픽 룬 개수: {runes?.Count ?? 0}");

        bool isFirstItem = true;
        if (runes != null && runes.Count > 0)
        {
            foreach (var rune in runes)
            {
                int price = RuneShopService.Instance.GetRunePrice(rune);
                CreateSecretRuneItem(rune, price);

                // 첫 번째 아이템 자동 선택
                if (isFirstItem && secretRuneDetailPanel != null)
                {
                    secretRuneDetailPanel.ShowRuneDetail(rune, price, this);
                    isFirstItem = false;
                }
            }
        }
    }

    /// <summary>
    /// 일반 상점 룬 아이템 생성
    /// </summary>
    private void CreateShopRuneItem(RuneSO rune, int price)
    {
        GameObject itemObj = Instantiate(shopRuneItemPrefab, shopRuneItemParent);
        var buttonController = itemObj.GetComponent<ShopRuneButtonController>();

        if (buttonController != null)
        {
            buttonController.SetupRune(rune, price, this, shopRuneDetailPanel);
        }
    }

    /// <summary>
    /// 교환소 룬 아이템 생성
    /// </summary>
    private void CreateExchangeRuneItem(RuneSO rune, int price)
    {
        GameObject itemObj = Instantiate(exchangeRuneItemPrefab, exchangeRuneItemParent);
        var buttonController = itemObj.GetComponent<ShopRuneButtonController>();

        if (buttonController != null)
        {
            buttonController.SetupRune(rune, price, this, exchangeRuneDetailPanel);
        }
    }

    /// <summary>
    /// 특수 교환소 룬 아이템 생성
    /// </summary>
    private void CreateSecretRuneItem(RuneSO rune, int price)
    {
        GameObject itemObj = Instantiate(secretRuneItemPrefab, secretRuneItemParent);
        var buttonController = itemObj.GetComponent<ShopRuneButtonController>();

        if (buttonController != null)
        {
            buttonController.SetupRune(rune, price, this, secretRuneDetailPanel);
        }
    }

    #endregion

}