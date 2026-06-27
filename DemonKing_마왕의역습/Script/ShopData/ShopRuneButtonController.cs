using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopRuneButton 프리팹에 붙는 스크립트
/// 룬 데이터를 받아서 UI 요소들을 업데이트하는 역할
/// 버튼 클릭 시 공유 상세 패널에 정보를 표시
/// </summary>
public class ShopRuneButtonController : MonoBehaviour
{
    #region UI 컴포넌트들

    [Header("기본 UI 요소")]
    [SerializeField] private Image runeImage;           // 룬 아이콘 이미지
    [SerializeField] private Text runeNameText;         // 룬 이름 텍스트
    [SerializeField] private Text priceText;            // 가격 텍스트 (골드)
    [SerializeField] private Text rarityText;           // 등급 텍스트
    [SerializeField] private Button detailButton;       // 상세 정보 버튼 (1번째 버튼)
    [SerializeField] private Button purchaseButton;     // 구매 버튼 (2번째 버튼)

    #endregion

    #region Private Fields

    private RuneSO runeData;                            // 이 버튼이 나타내는 룬 데이터
    private int runePrice;                              // 룬 가격
    private UIManager shopUIManager;                    // 상점 UI 매니저 참조
    private RuneDetailPanel detailPanelManager;         // 공유 상세 패널 매니저

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // 자식 오브젝트들에서 UI 요소들을 자동으로 찾기
        FindUIComponents();
    }

    #endregion

    #region UI Component Auto-Find

    /// <summary>
    /// 자동으로 자식 오브젝트들에서 UI 컴포넌트들을 찾는 메서드
    /// Inspector에서 직접 할당하지 않아도 자동으로 찾습니다
    ///
    /// 자동 찾기 규칙:
    /// - 이름에 "RuneName" 또는 "Name"이 포함된 Text → 룬 이름
    /// - 이름에 "Price"가 포함된 Text → 가격
    /// - 이름에 "Rarity"가 포함된 Text → 등급
    /// - 이름에 "Icon" 또는 "Image"가 포함된 Image → 룬 아이콘
    /// - Button 컴포넌트 → 버튼들
    /// </summary>
    private void FindUIComponents()
    {
        // Image 컴포넌트 찾기
        Image[] images = GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            string objName = img.gameObject.name.ToLower();

            if (objName.Contains("icon") || objName.Contains("rune"))
            {
                if (runeImage == null)
                    runeImage = img;
            }
        }

        // 모든 Text 컴포넌트 찾기
        Text[] textComponents = GetComponentsInChildren<Text>();
        foreach (Text textComp in textComponents)
        {
            string objName = textComp.gameObject.name.ToLower();

            // 오브젝트 이름으로 판단하여 자동 할당
            if (objName.Contains("runename") || objName.Contains("name"))
                runeNameText = textComp;
            else if (objName.Contains("price"))
                priceText = textComp;
            else if (objName.Contains("rarity"))
                rarityText = textComp;
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
    /// 룬 데이터를 설정하고 UI를 업데이트하는 메서드
    /// UIManager에서 호출합니다
    /// </summary>
    /// <param name="rune">표시할 룬 데이터</param>
    /// <param name="price">룬 가격 (골드)</param>
    /// <param name="uiManager">상점 UI 매니저</param>
    /// <param name="detailPanel">이 상점 전용 RuneDetailPanel</param>
    public void SetupRune(RuneSO rune, int price, UIManager uiManager, RuneDetailPanel detailPanel)
    {
        runeData = rune;
        runePrice = price;
        shopUIManager = uiManager;
        detailPanelManager = detailPanel;

        UpdateUI();
        SetupButton();
    }

    /// <summary>
    /// UI 요소들을 룬 데이터로 업데이트하는 메서드
    /// </summary>
    private void UpdateUI()
    {
        if (runeData == null) return;

        // 이미 보유한 룬인지 확인
        bool isOwned = RuneDatabase.Instance != null &&
                       RuneDatabase.Instance.GetOwnedRunes() != null &&
                       RuneDatabase.Instance.GetOwnedRunes().Contains(runeData);

        // 룬 아이콘 설정
        if (runeImage != null && runeData.icon != null)
        {
            runeImage.sprite = runeData.icon;
            // 이미 보유한 룬은 회색으로
            runeImage.color = isOwned ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
        }

        // 룬 이름 설정
        if (runeNameText != null)
        {
            runeNameText.text = isOwned ? $"{runeData.runeName} (보유)" : runeData.runeName;
            runeNameText.color = isOwned ? Color.gray : Color.white;
        }

        // 가격 설정
        if (priceText != null)
        {
            priceText.text = $"{runePrice}";
            priceText.color = isOwned ? Color.gray : Color.white;
        }

        // 등급 설정
        if (rarityText != null)
        {
            rarityText.text = GetRarityKoreanName(runeData.rarity);
            rarityText.color = isOwned ? Color.gray : GetRarityColor(runeData.rarity);
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
        if (runeData == null || shopUIManager == null) return;

        // 이미 보유한 룬인지 확인
        bool isOwned = RuneDatabase.Instance != null &&
                       RuneDatabase.Instance.GetOwnedRunes() != null &&
                       RuneDatabase.Instance.GetOwnedRunes().Contains(runeData);

        // 1번째 버튼: 상세 정보 표시
        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() =>
            {
                ShowDetailPanel();
            });
        }

        // 2번째 버튼: 바로 구매
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();

            // 이미 보유한 룬은 버튼 비활성화
            purchaseButton.interactable = !isOwned;

            purchaseButton.onClick.AddListener(() =>
            {
                OnPurchaseClicked();
            });
        }
    }

    /// <summary>
    /// 2번째 버튼 클릭 시 바로 구매 처리
    /// </summary>
    private void OnPurchaseClicked()
    {
        if (runeData == null)
        {
            Debug.LogWarning("[ShopRuneButtonController] 구매할 룬 데이터가 없습니다!");
            return;
        }

        if (shopUIManager == null)
        {
            Debug.LogWarning("[ShopRuneButtonController] UIManager를 찾을 수 없습니다!");
            return;
        }

        // RuneShopService null 체크
        if (RuneShopService.Instance == null)
        {
            Debug.LogError("[ShopRuneButtonController] RuneShopService.Instance가 null입니다!");
            return;
        }

        // RuneDatabase null 체크
        if (RuneDatabase.Instance == null)
        {
            Debug.LogError("[ShopRuneButtonController] RuneDatabase.Instance가 null입니다!");
            return;
        }

        Debug.Log($"[ShopRuneButtonController] {runeData.runeName} 구매 시도 중...");

        // RuneShopService를 통해 구매 처리
        bool success = RuneShopService.Instance.PurchaseRune(runeData, runePrice);

        if (success)
        {
            Debug.Log($"[ShopRuneButtonController] {runeData.runeName} 구매 성공!");

            // 이 버튼 UI 즉시 업데이트 (보유중 상태 반영)
            UpdateUI();
            SetupButton();

            // UI 매니저 업데이트 (다이아몬드 표시 갱신)
            if (shopUIManager != null)
            {
                shopUIManager.UpdateUI(refreshPanels: true);
            }
            //편성 화면 룬 표시
            var runeInventoryPanel = FindAnyObjectByType<RuneInventoryPanel>();
            if (runeInventoryPanel != null)
                runeInventoryPanel.Refresh();
        }
        else
        {
            Debug.Log($"[ShopRuneButtonController] {runeData.runeName} 구매 실패! (다이아 부족 또는 이미 보유)");
        }



        // 구매 성공/실패 관계없이 항상 상세 패널 표시
        ShowDetailPanel();
    }

    #endregion

    #region Detail Panel

    /// <summary>
    /// 공유 상세 패널에 이 룬 정보를 표시
    /// </summary>
    public void ShowDetailPanel()
    {
        if (detailPanelManager == null)
        {
            Debug.LogWarning("[ShopRuneButtonController] RuneDetailPanel을 찾을 수 없습니다!");
            return;
        }

        if (runeData == null)
        {
            Debug.LogWarning("[ShopRuneButtonController] 룬 데이터가 없습니다!");
            return;
        }

        // 공유 패널에 이 룬 정보 전달
        detailPanelManager.ShowRuneDetail(runeData, runePrice, shopUIManager);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 등급을 한글로 변환
    /// </summary>
    private string GetRarityKoreanName(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return "일반";
            case RuneRarity.Rare:
                return "희귀";
            case RuneRarity.Epic:
                return "영웅";
            case RuneRarity.Legendary:
                return "전설";
            default:
                return "알 수 없음";
        }
    }

    /// <summary>
    /// 등급별 텍스트 색상 반환
    /// </summary>
    private Color GetRarityColor(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return new Color(0.7f, 0.7f, 0.7f);  // 회색
            case RuneRarity.Rare:
                return new Color(0.3f, 0.6f, 1f);    // 파란색
            case RuneRarity.Epic:
                return new Color(0.7f, 0.3f, 1f);    // 보라색
            case RuneRarity.Legendary:
                return new Color(1f, 0.7f, 0.2f);    // 주황/금색
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// 현재 버튼의 룬 ID를 반환하는 메서드
    /// </summary>
    public string GetRuneID()
    {
        return runeData?.runeId;
    }

    /// <summary>
    /// 현재 버튼의 룬 데이터를 반환하는 메서드
    /// </summary>
    public RuneSO GetRuneData()
    {
        return runeData;
    }

    #endregion
}
