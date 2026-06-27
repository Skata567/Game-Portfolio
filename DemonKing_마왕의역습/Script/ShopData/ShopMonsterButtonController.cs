using Assets.PixelFantasy.PixelMonsters.Common.Scripts;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopMonsterButton 프리팹에 붙는 스크립트
/// 몬스터 데이터를 받아서 UI 요소들을 업데이트하는 역할
/// 버튼 클릭 시 공유 상세 패널에 정보를 표시
/// </summary>
public class ShopMonsterButtonController : MonoBehaviour
{
    #region UI 컴포넌트들

    [Header("기본 UI 요소")]
    [SerializeField] private Image monsterImage;        // 몬스터 이미지
    [SerializeField] private Text monsterNameText;      // 몬스터 이름 텍스트
    [SerializeField] private Text priceGoldText;        // 골드 가격 텍스트
    [SerializeField] private Text priceIronText;        // 철 가격 텍스트
    [SerializeField] private Text ownedCountText;       // 보유량 텍스트
    [SerializeField] private Text stageText;            // 해금단계 표시 텍스트
    [SerializeField] private Button detailButton;       // 상세 정보 버튼 (1번째 버튼)
    [SerializeField] private Button purchaseButton;     // 구매 버튼 (2번째 버튼)

    #endregion

    #region Private Fields

    private UnitStock monsterData;                      // 이 버튼이 나타내는 몬스터 데이터
    private UIManager shopUIManager;                    // 상점 UI 매니저 참조
    private MonsterPurchaseService purchaseService;
    private MonsterDetailPanel detailPanelManager;      // 공유 상세 패널 매니저
    private bool isLocked = false; // 잠금 상태
    private bool isSellMode = false; // 판매 모드 여부

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // 자식 오브젝트들에서 UI 요소들을 자동으로 찾기
        FindUIComponents();
        purchaseService = FindAnyObjectByType<MonsterPurchaseService>();

        // DetailPanel은 SetupMonster에서 받아옴 (각 상점마다 다름)
    }

    #endregion

    #region UI Component Auto-Find

    /// <summary>
    /// 자동으로 자식 오브젝트들에서 UI 컴포넌트들을 자동으로 찾는 메서드
    /// Inspector에서 직접적으로 할당하지 않아도 자동으로 찾습니다
    ///
    /// 자동 찾기 규칙:
    /// - 이름에 "MonsterName" 또는 "title"이 포함된 Text → 몬스터 이름
    /// - 이름에 "price" 또는 "cost"가 포함된 Text → 가격
    /// - 이름에 "owned" 또는 "count"가 포함된 Text → 보유량
    /// - Image 컴포넌트 → 몬스터 이미지
    /// - Button 컴포넌트 → 구매 버튼
    /// </summary>

    private void FindUIComponents()
    {
        // Image 컴포넌트 찾기 (몬스터 이미지)
        if (monsterImage == null)
            monsterImage = GetComponentInChildren<Image>();

        // 모든 Text 컴포넌트를 찾기
        Text[] textComponents = GetComponentsInChildren<Text>();
        foreach (Text textComp in textComponents)
        {
            string objName = textComp.gameObject.name.ToLower();

            // 오브젝트 이름으로 판단하여 자동 할당
            if (objName.Contains("monstername") || objName.Contains("title"))
                monsterNameText = textComp;
            else if (objName.Contains("price_gold"))
                priceGoldText = textComp;
            else if (objName.Contains("price_iron"))
                priceIronText = textComp;
            else if (objName.Contains("owned"))
                ownedCountText = textComp;
            else if (objName.Contains("stage_text"))
                stageText = textComp;
        }

        // 버튼 찾기 (2개)
        Button[] buttons = GetComponentsInChildren<Button>();
        if (buttons.Length >= 1 && detailButton == null)
            detailButton = buttons[0];  // 1번째 버튼: 상세 정보
        if (buttons.Length >= 2 && purchaseButton == null)
            purchaseButton = buttons[1];  // 2번째 버튼: 구매
    }

    #endregion

    #region Setup and Update

    /// <summary>
    /// 몬스터 데이터를 설정하고 UI를 업데이트하는 메서드
    /// CompleteShopUIManager에서 호출합니다
    /// </summary>
    /// <param name="monster">표시할 몬스터 데이터</param>
    /// <param name="uiManager">상점 UI 매니저</param>
    /// <param name="detailPanel">이 상점 전용 MonsterDetailPanel</param>
    /// <param name="sellMode">판매 모드 여부 (true = 판매, false = 구매)</param>
    ///
    /// 사용 예시:
    /// buttonController.SetupMonster(goblinData, shopUIManager, shopDetailPanel);
    public void SetupMonster(UnitStock monster, UIManager uiManager, MonsterDetailPanel detailPanel, bool sellMode = false)
    {
        monsterData = monster;
        shopUIManager = uiManager;
        detailPanelManager = detailPanel; // 각 상점마다 다른 DetailPanel 설정
        isSellMode = sellMode;

        isLocked = purchaseService != null && purchaseService.IsMonsterLocked(monster.enemyId);

        UpdateUI();
        SetupButton();
    }

    /// <summary>
    /// UI 요소들을 몬스터 데이터로 업데이트하는 메서드
    /// 몬스터 이미지, 이름, 가격, 해금상태 표시
    /// </summary>
    private void UpdateUI()
    {
        if (monsterData == null) return;

        // 잠금 상태 재확인 (스테이지가 변경되거나 구매 후 UI 업데이트 시)
        isLocked = purchaseService != null && purchaseService.IsMonsterLocked(monsterData.enemyId);

        // 몬스터 이미지 설정
        if (monsterImage != null && monsterData.icon != null)
        {
            monsterImage.sprite = monsterData.icon;

            if (isLocked)
            {
                monsterImage.color = new Color(0f, 0f, 0f, 0.8f); // 회색
                stageText.text = $"{monsterData.stage - StaticInfoManager.floor}단계 후 해금";
            }
            else
            {
                stageText.text = "";
                monsterImage.color = Color.white;
            }
        }

        // 몬스터 이름 설정 (판매 모드일 때 "(판매)" 추가)
        if (monsterNameText != null)
        {
            monsterNameText.text = isSellMode ? $"{monsterData.displayName} (판매)" : monsterData.displayName;
        }

        // 가격 설정 (판매 모드일 때 판매가로 표시)
        if (isSellMode)
        {
            // 판매 가격 = 구매 가격의 절반
            int sellGoldPrice = monsterData.price_Gold / 2;
            int sellIronPrice = monsterData.price_Iron / 2;

            if (priceGoldText != null)
            {
                priceGoldText.text = $"{sellGoldPrice} ";
            }

            if (priceIronText != null)
            {
                priceIronText.text = $"{sellIronPrice}";
            }
        }
        else
        {
            // 구매 가격
            if (priceGoldText != null)
            {
                priceGoldText.text = $"{monsterData.price_Gold} ";
            }

            if (priceIronText != null)
            {
                priceIronText.text = $"{monsterData.price_Iron}";
            }
        }

        // 보유량 표시
        if (ownedCountText != null)
        {
            int ownedCount;
            if (TutorialManager.IsTutorialMode)
            {
                // 튜토리얼 모드: 튜토리얼 전용 데이터 사용
                ownedCount = TutorialManager.Instance.GetTutorialMonsterCount(monsterData.enemyId);
            }
            else
            {
                // 일반 모드: 실제 데이터 사용
                ownedCount = monsterData.owned;
            }

            ownedCountText.text = $"x{ownedCount}";
        }
    }

    #endregion

    #region Button Setup

    /// <summary>
    /// 버튼 이벤트를 설정하는 메서드
    /// 1번째 버튼: 상세 정보 표시
    /// 2번째 버튼: 바로 구매
    /// </summary>
    private void SetupButton()
    {
        if (monsterData == null || shopUIManager == null) return;

        // 1번째 버튼: 상세 정보 표시
        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();

            if (isLocked)
            {
                detailButton.interactable = false;
                detailButton.onClick.AddListener(() =>
                {
                    Debug.Log($"{monsterData.displayName}은(는) 스테이지 {monsterData.stage}에서 해금됩니다!");
                });
            }
            else
            {
                detailButton.interactable = true;
                detailButton.onClick.AddListener(() =>
                {
                    ShowDetailPanel();
                });
            }
        }

        // 2번째 버튼: 바로 구매 또는 판매
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();

            if (isLocked)
            {
                purchaseButton.interactable = false;
            }
            else
            {
                purchaseButton.interactable = true;

                // 판매 모드일 때는 OnSellClicked, 구매 모드일 때는 OnPurchaseClicked 호출
                if (isSellMode)
                {
                    // 판매 모드: 보유량이 0이면 버튼 비활성화
                    purchaseButton.interactable = monsterData.owned > 0;
                    purchaseButton.onClick.AddListener(() =>
                    {
                        OnSellClicked();
                    });
                }
                else
                {
                    // 구매 모드
                    purchaseButton.onClick.AddListener(() =>
                    {
                        OnPurchaseClicked();
                    });
                }
            }
        }
    }

    /// <summary>
    /// 2번째 버튼 클릭 시 바로 구매 처리
    /// </summary>
    private void OnPurchaseClicked()
    {
        if (monsterData == null)
        {
            Debug.LogWarning("[ShopMonsterButtonController] 구매할 몬스터 데이터가 없습니다!");
            return;
        }

        if (purchaseService == null)
        {
            Debug.LogWarning("[ShopMonsterButtonController] MonsterPurchaseService를 찾을 수 없습니다!");
            return;
        }

        // 현재 플레이어 자원 가져오기
        int currentGold = StaticInfoManager.gold;
        int currentIron = StaticInfoManager.iron;

        // 구매 처리 (수량 1개)
        var result = purchaseService.PurchaseMonster(
            monsterData.enemyId,
            1,  // 수량
            currentGold,
            currentIron
        );

        if (result.success)
        {
            Debug.Log($"[ShopMonsterButtonController] {monsterData.displayName} 구매 성공! {result.message}");

            // 버튼 UI 업데이트 (보유량 변경 반영)
            UpdateUI();

            // 자원 표시만 업데이트 (상점 재생성 안함 - 깜빡임 방지)
            if (shopUIManager != null)
            {
                shopUIManager.UpdateUI(refreshPanels: false);
            }

            // 상세 패널 표시 (구매 후 바로 정보 보여주기)
            ShowDetailPanel();
        }
        else
        {
            Debug.Log($"[ShopMonsterButtonController] {monsterData.displayName} 구매 실패: {result.message}");
        }
    }

    /// <summary>
    /// 2번째 버튼 클릭 시 바로 판매 처리 (판매 모드일 때)
    /// </summary>
    private void OnSellClicked()
    {
        if (monsterData == null)
        {
            Debug.LogWarning("[ShopMonsterButtonController] 판매할 몬스터 데이터가 없습니다!");
            return;
        }

        if (shopUIManager == null)
        {
            Debug.LogWarning("[ShopMonsterButtonController] UIManager를 찾을 수 없습니다!");
            return;
        }

        if (monsterData.owned <= 0)
        {
            Debug.LogWarning($"[ShopMonsterButtonController] {monsterData.displayName}을(를) 보유하고 있지 않아 판매할 수 없습니다!");
            return;
        }

        // UIManager를 통해 판매 처리
        shopUIManager.SellMonsterFromShop(monsterData);

        // 버튼 UI 업데이트 (보유량 변경 반영)
        UpdateUI();

        // 상세 패널 표시 (판매 후 정보 업데이트)
        ShowDetailPanel();
    }

    #endregion

    #region Detail Panel

    /// <summary>
    /// 공유 상세 패널에 이 몬스터 정보를 표시
    /// </summary>
    public void ShowDetailPanel()
    {
        if (detailPanelManager == null)
        {
            Debug.LogWarning("[ShopMonsterButtonController] MonsterDetailPanel을 찾을 수 없습니다! 씬에 MonsterDetailPanel이 있는지 확인하세요.");
            return;
        }

        if (monsterData == null)
        {
            Debug.LogWarning("[ShopMonsterButtonController] 몬스터 데이터가 없습니다!");
            return;
        }

        // 공유 패널에 이 몬스터 정보 전달
        detailPanelManager.ShowMonsterDetail(monsterData, shopUIManager);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 보유량을 업데이트하는 메서드 (구매 후 호출됨)
    /// 구매가 완료된 후 보유량 숫자를 새로 갱신할 때 사용
    /// </summary>
    public void UpdateOwnedCount()
    {
        if (ownedCountText != null && monsterData != null)
        {
            ownedCountText.text = $"보유: {monsterData.owned}";
        }
    }

    /// <summary>
    /// 현재 버튼의 ID를 반환하는 메서드
    /// 디버깅이나 다른 시스템에서 이 버튼이 어떤 몬스터를 나타내는지 확인할 때 사용
    /// </summary>
    /// <returns>몬스터 ID 문자열</returns>
    public string GetMonsterID()
    {
        return monsterData?.enemyId;
    }

    /// <summary>
    /// 현재 버튼의 몬스터 데이터를 반환하는 메서드
    /// </summary>
    /// <returns>몬스터 데이터</returns>
    public UnitStock GetMonsterData()
    {
        return monsterData;
    }

    /// <summary>
    /// 잠금 상태 확인
    /// </summary>
    public bool IsLocked() => isLocked;

    #endregion
}
