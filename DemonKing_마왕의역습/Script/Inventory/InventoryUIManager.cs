using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 인벤토리 UI 매니저 (MonsterCatalog 호환 버전)
/// 인벤토리 표시 및 상세정보 창 관리
/// </summary>
public class InventoryUIManager : MonoBehaviour
{
    #region UI Components

    [Header("인벤토리 UI")]
    [SerializeField] private Transform inventoryGridParent;
    [SerializeField] private GameObject monsterIconPrefab;
    [SerializeField] private GameObject specialMonsterIconPrefab;

    [Header("상세 정보 패널")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailMonsterImage;
    [SerializeField] private Text detailNameText;
    [SerializeField] private Text detailHPText;
    [SerializeField] private Text detailDamageText;
    [SerializeField] private Text detailSpeedText;
    [SerializeField] private Text detailRaceText;
    [SerializeField] private Text detailCostText;
    [SerializeField] private Text detailOwnedText;
    [SerializeField] private Text detailDescriptionText;

    #endregion

    #region Private Fields

    private MonsterPurchaseService purchaseService;
    private MonsterCatalog catalogCache;

    private UnitStock currentDetailMonster;
    private MonsterCatalog.SpecialMeta currentSpecialMeta;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        InitializeServices();
        InitializeDetailPanel();
        UpdateFullInventoryUI();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 필요한 서비스들 초기화
    /// </summary>
    private void InitializeServices()
    {
        purchaseService = FindAnyObjectByType<MonsterPurchaseService>();
        catalogCache = MonsterCatalog.Instance;

        if (catalogCache == null)
        {
            Debug.LogError("[InventoryUIManager] MonsterCatalog 인스턴스를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 상세정보 창 초기 설정
    /// </summary>
    private void InitializeDetailPanel()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    #endregion

    #region Public Methods - Inventory Update

    /// <summary>
    /// 전체 인벤토리 UI 업데이트 (일반 + 특수 몬스터)
    /// </summary>
    public void UpdateFullInventoryUI()
    {
        if (inventoryGridParent == null) return;

        Debug.Log("=== 전체 인벤토리 업데이트 시작 ===");
        ClearInventory();

        // 일반 몬스터 추가
        AddNormalMonstersToInventory();

        // 특수 몬스터 추가
        AddSpecialMonstersToInventory();

        Debug.Log("=== 전체 인벤토리 업데이트 완료 ===");
    }

    /// <summary>
    /// 호환성용 예전 메서드 (이름 바뀐 경고)
    /// </summary>
    [System.Obsolete("UpdateFullInventoryUI 메서드를 사용하세요")]
    public void UpdateInventoryUI()
    {
        Debug.LogWarning("UpdateInventoryUI 대신 UpdateFullInventoryUI 메서드를 사용하세요");
        UpdateFullInventoryUI();
    }

    /// <summary>
    /// 호환성용 예전 메서드 (이름 바뀐 경고)
    /// </summary>
    [System.Obsolete("UpdateFullInventoryUI 메서드를 사용하세요")]
    public void UpdateSInventoryUI()
    {
        Debug.LogWarning("UpdateSInventoryUI 대신 UpdateFullInventoryUI 메서드를 사용하세요");
        UpdateFullInventoryUI();
    }

    #endregion

    #region Private Methods - Monster Creation

    /// <summary>
    /// 일반 몬스터를 인벤토리에 추가 (
    /// 
    /// 표시, 미획득 유닛 포함)
    /// 튜토리얼 모드일 경우 튜토리얼 몬스터 카운트를 표시
    /// </summary>
    private void AddNormalMonstersToInventory()
    {
        if (catalogCache == null || monsterIconPrefab == null) return;

        // MonsterCatalog의 모든 일반 유닛 가져오기
        var allNormalMonsters = catalogCache.normalUnits;
        Debug.Log($"전체 일반 몬스터 개수: {allNormalMonsters.Count}");

        UnitStock firstMonster = null;

        foreach (var monster in allNormalMonsters)
        {
            // 첫 번째 몬스터 저장
            if (firstMonster == null)
            {
                firstMonster = monster;
            }

            // 튜토리얼 모드일 경우 가상 소유 수를 임시로 덮어씌움
            if (TutorialManager.IsTutorialMode)
            {
                int tutorialCount = TutorialManager.Instance.GetTutorialMonsterCount(monster.enemyId);
                if (tutorialCount > 0)
                {
                    // 원본 데이터 보존을 위해 임시 복사본 생성
                    var tempMonster = new UnitStock
                    {
                        enemyId = monster.enemyId,
                        displayName = monster.displayName,
                        icon = monster.icon,
                        hp = monster.hp,
                        damage = monster.damage,
                        minSpeed = monster.minSpeed,
                        maxSpeed = monster.maxSpeed,
                        category = monster.category,
                        cost = monster.cost,
                        owned = tutorialCount,  // 튜토리얼 카운트 사용
                        select = monster.select,
                        stage = monster.stage,
                        price_Gold = monster.price_Gold,
                        price_Iron = monster.price_Iron,
                        ex = monster.ex,
                        group = monster.group,
                        attackCount = monster.attackCount,
                        encountered = monster.encountered,
                        hasBeenOwned = monster.hasBeenOwned  //
                    };
                    CreateMonsterIcon(tempMonster);
                }
                else
                {
                    CreateMonsterIcon(monster);
                }
            }
            else
            {
                CreateMonsterIcon(monster);
            }
        }

        // 첫 번째 몬스터 자동 선택 (상세 정보 표시)
        if (firstMonster != null)
        {
            ShowMonsterDetail(firstMonster);
            Debug.Log($"[InventoryUIManager] 첫 번째 일반 몬스터 자동 선택: {firstMonster.displayName}");
        }
    }

    /// <summary>
    /// 특수 몬스터를 인벤토리에 추가 (모든 유닛 표시, 미획득 유닛 포함)
    /// </summary>
    private void AddSpecialMonstersToInventory()
    {
        if (catalogCache == null || specialMonsterIconPrefab == null) return;

        // MonsterCatalog의 모든 특수 유닛 가져오기
        var allSpecialMonsters = catalogCache.specialUnits;
        Debug.Log($"전체 특수 몬스터 개수: {allSpecialMonsters.Count}");

        foreach (var specialMonster in allSpecialMonsters)
        {
            CreateSpecialMonsterIcon(specialMonster);
        }
    }

    /// <summary>
    /// 일반 몬스터 아이콘 생성
    /// </summary>
    private void CreateMonsterIcon(UnitStock monster)
    {
        GameObject iconObj = Instantiate(monsterIconPrefab, inventoryGridParent);
        var iconController = iconObj.GetComponent<InventoryMonsterIcon>();

        if (iconController != null)
        {
            iconController.SetupMonster(monster, this);
        }
    }

    /// <summary>
    /// 특수 몬스터 아이콘 생성
    /// </summary>
    private void CreateSpecialMonsterIcon(UnitStock specialMonster)
    {
        GameObject iconObj = Instantiate(specialMonsterIconPrefab, inventoryGridParent);
        var iconController = iconObj.GetComponent<InventoryMonsterIcon>();

        if (iconController != null)
        {
            iconController.SetupSpecialMonster(specialMonster, this);
        }
    }

    #endregion

    #region Public Methods - Detail Display

    /// <summary>
    /// 일반 몬스터 상세정보 창 표시
    /// </summary>
    public void ShowMonsterDetail(UnitStock monster)
    {
        if (detailPanel == null || monster == null) return;

        currentDetailMonster = monster;
        currentSpecialMeta = null;

        UpdateDetailUI(monster, null);
        detailPanel.SetActive(true);
    }

    /// <summary>
    /// 특수 몬스터 상세정보 창 표시
    /// </summary>
    public void ShowSpecialMonsterDetail(UnitStock monster, MonsterCatalog.SpecialMeta meta)
    {
        if (detailPanel == null || monster == null) return;

        currentDetailMonster = monster;
        currentSpecialMeta = meta;

        UpdateDetailUI(monster, meta);
        detailPanel.SetActive(true);
    }

    /// <summary>
    /// 상세정보 창 닫기
    /// </summary>
    public void HideMonsterDetail()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);

        currentDetailMonster = null;
        currentSpecialMeta = null;
    }

    public void OnSellMonster()
    {
        OnSellButtonClicked();
    }

    #endregion

    #region Private Methods - Detail UI Update

    /// <summary>
    /// 상세정보 UI 업데이트 (일반/특수 몬스터 공통)
    /// </summary>
    private void UpdateDetailUI(UnitStock monster, MonsterCatalog.SpecialMeta specialMeta)
    {
        // 기본 정보 업데이트
        UpdateBasicMonsterInfo(monster);

        // 특수 몬스터 추가 정보 업데이트
        if (specialMeta != null)
        {
            UpdateSpecialMonsterInfo(specialMeta);
        }
    }

    /// <summary>
    /// 몬스터 기본 정보 업데이트 (미획득 유닛 정보 숨김 처리)
    /// 한 번이라도 소유했던 유닛은 도감에 영구 등록 (hasBeenOwned 체크)
    /// </summary>
    private void UpdateBasicMonsterInfo(UnitStock monster)
    {
        // 도감 표시 조건 변경: owned > 0 → hasBeenOwned (한 번이라도 소유했으면 도감에 표시)
        bool isOwned = monster.hasBeenOwned;

        if (detailMonsterImage != null && monster.icon != null)
        {
            detailMonsterImage.sprite = monster.icon;
            // 미획득시 검정색, 획득시 흰색
            detailMonsterImage.color = isOwned ? Color.white : Color.black;
        }

        // 미획득 유닛은 이름을 ???로 표시
        if (detailNameText != null)
            detailNameText.text = isOwned ? monster.displayName : "???";

        // 미획득 유닛은 스탯을 ???로 표시
        if (detailHPText != null)
            detailHPText.text = isOwned ? $"체력: {monster.hp}" : "체력: ???";

        if (detailDamageText != null)
            detailDamageText.text = isOwned ? $"공격력: {monster.damage}" : "공격력: ???";

        if (detailSpeedText != null)
            detailSpeedText.text = isOwned ? $"속도: {monster.minSpeed} ~ {monster.maxSpeed}" : "속도: ??? ~ ???";

        if (detailRaceText != null)
        {
            Debug.Log($"[InventoryUIManager] 몬스터 카테고리: '{monster.category}' (이름: {monster.displayName})");

            // 스페셜 그룹 확인 (늑대의 왕, 트롤의 왕, 아인의 왕, 하늘의 왕, 대지의 왕)
            bool isSpecialGroup = monster.category == "Werewolf" || monster.category == "Troll" ||
                                  monster.category == "Org" || monster.category == "Dragon" ||
                                  monster.category == "Tyranno";

            switch(monster.category)
            {
                case "아인":
                    detailRaceText.text = isOwned ? "종족: 아인" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(0.2f, 0.8f, 0.3f) : Color.gray; ; 
                    break;
                case "수인":
                    detailRaceText.text = isOwned ? "종족: 수인" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(0.7f, 0.5f, 0.3f) : Color.gray; // 갈색
                    break;
                case "언데드":
                    detailRaceText.text = isOwned ? "종족: 언데드" : "종족: ???";
                    detailRaceText.color = isOwned ?new Color(0.6f, 0.4f, 0.8f) : Color.gray; // 보라색
                    break;
                case "타락":
                    detailRaceText.text = isOwned ? "종족: 타락" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(0.8f, 0.2f, 0.2f) : Color.gray; // 붉은색
                    break;
                case "마족":
                    detailRaceText.text = isOwned ? "종족: 마족" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(0.9f, 0.1f, 0.3f) : Color.gray; // 진홍색
                    break;
                case "늑대의 왕":
                    detailRaceText.text = isOwned ? "종족: 늑대의 왕" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(1f, 0.7f, 0.2f) : Color.gray; // 레전더리 금색
                    break;
                case "트롤의 왕":
                    detailRaceText.text = isOwned ? "종족: 트롤의 왕" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(1f, 0.7f, 0.2f) : Color.gray; // 레전더리 금색
                    break;
                case "아인의 왕":
                    detailRaceText.text = isOwned ? "종족: 아인의 왕" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(1f, 0.7f, 0.2f) : Color.gray; // 레전더리 금색
                    break;
                case "하늘의 왕":
                    detailRaceText.text = isOwned ? "종족: 하늘의 왕" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(1f, 0.7f, 0.2f) : Color.gray; // 레전더리 금색
                    break;
                case "대지의 왕":
                    detailRaceText.text = isOwned ? "종족: 대지의 왕" : "종족: ???";
                    detailRaceText.color = isOwned ? new Color(1f, 0.7f, 0.2f) : Color.gray; // 레전더리 금색
                    break;
                default :
                    detailRaceText.text = isOwned ? $"종족: {monster.category}" : "???";
                    detailRaceText.color = isOwned ? Color.white : Color.gray; // 미획득 시 회색
                    break;
            }
        }

        if (detailCostText != null)
        {
            if (isOwned)
            {
                // 일반 몬스터는 코스트, 특수 몬스터는 가격 표시
                bool isSpecial = currentSpecialMeta != null;
                if (isSpecial && currentSpecialMeta.price > 0)
                {
                    detailCostText.text = $"가격: {currentSpecialMeta.price} 미스릴";
                }
                else
                {
                    detailCostText.text = $"코스트: {monster.cost}";
                }
            }
            else
            {
                detailCostText.text = "코스트: ???";
            }
        }

        if (detailOwnedText != null)
            detailOwnedText.text = $"보유수: {monster.owned}";

        // 미획득 유닛은 설명을 숨김
        if (detailDescriptionText != null)
            detailDescriptionText.text = isOwned ?
                (string.IsNullOrEmpty(monster.ex) ? "설명이 없습니다." : monster.ex) :
                "아직 획득하지 못한 유닛입니다.";
    }

    /// <summary>
    /// 특수 몬스터 추가 정보 업데이트
    /// </summary>
    private void UpdateSpecialMonsterInfo(MonsterCatalog.SpecialMeta specialMeta)
    {
        // 특수 몬스터만의 추가 UI 업데이트 가능
        // 예: 상점 타입 표시, 특별한 강조 효과 등
        if (detailMonsterImage != null && currentDetailMonster != null)
        {
            bool isOwned = currentDetailMonster.hasBeenOwned;

            // 미획득시 검정색 유지, 획득시에만 특수 색상 적용
            if (isOwned)
            {
                // 특수 몬스터 시각적 구분 (예: 일반 상점은 흰색, 비밀 상점은 노랑)
                detailMonsterImage.color = specialMeta.shop ? Color.white : Color.yellow;
            }
            else
            {
                // 미획득시 검정 실루엣 유지
                detailMonsterImage.color = Color.black;
            }
        }
    }

    #endregion

    #region Public Methods - External Updates

    /// <summary>
    /// 인벤토리에서 골드 변경 시 호출
    /// </summary>
    public void UpdateGoldFromInventory(int goldEarned)
    {
        var shopUI = FindAnyObjectByType<UIManager>();
        if (shopUI != null)
        {
            shopUI.UpdateGoldFromInventory(goldEarned);
        }
    }

    /// <summary>
    /// 인벤토리에서 철 변경 시 호출
    /// </summary>
    public void UpdateIronFromInventory(int ironEarned)
    {
        var shopUI = FindAnyObjectByType<UIManager>();
        if (shopUI != null)
        {
            shopUI.UpdateIronFromInventory(ironEarned);
        }
    }

    #endregion

    #region Private Methods - Utilities

    /// <summary>
    /// 기존 인벤토리 아이콘들 제거
    /// </summary>
    private void ClearInventory()
    {
        if (inventoryGridParent == null) return;

        foreach (Transform child in inventoryGridParent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 판매 버튼 클릭 시 호출 (내부 확정)
    /// </summary>
    private void OnSellButtonClicked()
    {
        if (currentDetailMonster == null || purchaseService == null) return;

        var result = purchaseService.SellMonster(currentDetailMonster.enemyId, 1);

        if (result.success)
        {
            Debug.Log($"판매 성공: {result.message}");

            UpdateDetailUI(currentDetailMonster, currentSpecialMeta);
            UpdateFullInventoryUI();
            UpdateGoldFromInventory(result.goldEarned);
            UpdateIronFromInventory(result.ironEarned);

            if (currentDetailMonster.owned <= 0)
            {
                HideMonsterDetail();
            }
        }
        else
        {
            Debug.LogWarning($"판매 실패: {result.message}");
        }
    }

    #endregion
}

/// <summary>
/// UIManager 확장 예시
/// </summary>
public static class ShopUIManagerExtensions
{
    public static void UpdateGoldFromInventory_AddThisToCompleteShopUIManager(int goldEarned)
    {

        /*
        public void UpdateGoldFromInventory(int goldEarned)
        {
            playerGold += goldEarned;
            UpdateGoldUI();
        }
        */
    }
}
