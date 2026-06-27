using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특수 몬스터 상점 버튼 컨트롤러 (MonsterCatalog 호환)
/// 특수 몬스터 데이터를 받아서 UI 업데이트 및 구매 처리
///
/// 2버튼 시스템:
/// - 1번째 버튼 (detailButton): 상세 정보 표시
/// - 2번째 버튼 (purchaseButton): 바로 구매
/// </summary>
public class ShopSMonsterButtonController : MonoBehaviour
{
    #region UI Components

    [Header("UI 요소들")]
    [SerializeField] private Image monsterImage;
    [SerializeField] private Text monsterNameText;
    [SerializeField] private Text priceMithrilText;
    [SerializeField] private Button detailButton;      // 1번째 버튼: 상세 정보
    [SerializeField] private Button purchaseButton;    // 2번째 버튼: 바로 구매

    #endregion

    #region Private Fields

    private UnitStock monsterData;
    private MonsterCatalog.SpecialMeta specialMeta;
    private UIManager shopUIManager;
    private MonsterCatalog catalogCache;
    private MonsterDetailPanel detailPanelManager;     // 공유 상세 패널

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        FindUIComponents();
        InitializeCatalog();
        // DetailPanel은 SetupMonster에서 받아옴 (각 상점마다 다름)
    }

    #endregion

    #region Initialization

    /// <summary>
    /// UI 컴포넌트 자동 검색
    /// </summary>
    private void FindUIComponents()
    {
        if (monsterImage == null)
            monsterImage = GetComponentInChildren<Image>();

        // Text 컴포넌트 자동 찾기
        Text[] textComponents = GetComponentsInChildren<Text>();
        foreach (Text textComp in textComponents)
        {
            string objName = textComp.gameObject.name.ToLower();

            if (objName.Contains("smonstername") || objName.Contains("name"))
                monsterNameText = textComp;
            else if (objName.Contains("price_mithril") || objName.Contains("price"))
                priceMithrilText = textComp;
        }

        // 버튼 자동 찾기 (2개)
        Button[] buttons = GetComponentsInChildren<Button>();
        if (buttons.Length >= 2)
        {
            detailButton = buttons[0];      // 1번째 버튼
            purchaseButton = buttons[1];    // 2번째 버튼
        }
        else if (buttons.Length == 1)
        {
            purchaseButton = buttons[0];    // 구매 버튼만 있는 경우
        }
    }

    /// <summary>
    /// MonsterCatalog 참조 초기화
    /// </summary>
    private void InitializeCatalog()
    {
        catalogCache = MonsterCatalog.Instance;

        if (catalogCache == null)
        {
            Debug.LogError("[ShopSMonsterButtonController] MonsterCatalog 인스턴스를 찾을 수 없습니다!");
        }
    }

    #endregion

    #region Public Setup Methods

    /// <summary>
    /// 특수 몬스터 데이터 설정 및 UI 업데이트
    /// </summary>
    /// <param name="monster">특수 몬스터 데이터</param>
    /// <param name="uiManager">UI 매니저 참조</param>
    /// <param name="detailPanel">이 상점 전용 MonsterDetailPanel</param>
    public void SetupMonster(UnitStock monster, UIManager uiManager, MonsterDetailPanel detailPanel)
    {
        monsterData = monster;
        shopUIManager = uiManager;
        detailPanelManager = detailPanel; // 각 상점마다 다른 DetailPanel 설정

        // MonsterCatalog에서 특수 메타 정보 조회
        if (catalogCache != null)
        {
            specialMeta = catalogCache.GetSpecialMeta(monster.enemyId);
        }
        else
        {
            Debug.LogWarning($"[ShopSMonsterButtonController] MonsterCatalog가 없어 기본값 사용: {monster.enemyId}");
            specialMeta = new MonsterCatalog.SpecialMeta { price = 0, shop = true };
        }

        UpdateUI();
        SetupButtons();
    }

    #endregion

    #region Private UI Methods

    /// <summary>
    /// UI 요소들 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (monsterData == null) return;

        UpdateBasicInfo();
        UpdatePurchaseVisuals();
    }

    /// <summary>
    /// 기본 정보 업데이트 (이미지, 이름, 가격)
    /// </summary>
    private void UpdateBasicInfo()
    {
        // 몬스터 이미지
        if (monsterImage != null && monsterData.icon != null)
        {
            monsterImage.sprite = monsterData.icon;
        }

        // 몬스터 이름
        if (monsterNameText != null)
        {
            monsterNameText.text = monsterData.displayName;
        }

        // 미스릴 가격
        if (priceMithrilText != null && specialMeta != null)
        {
            priceMithrilText.text = $"{specialMeta.price}";
        }
    }

    /// <summary>
    /// 구매 상태에 따른 시각적 효과 적용
    /// </summary>
    private void UpdatePurchaseVisuals()
    {
        bool isPurchaseable = monsterData.owned == 0;
        Debug.Log($"[ShopSMonster] {monsterData.displayName} - owned: {monsterData.owned}, isPurchaseable: {isPurchaseable}");
        SetMonsterImageAndColor(isPurchaseable);

        // 구매 버튼 비활성화
        if (purchaseButton != null)
        {
            purchaseButton.interactable = isPurchaseable;
        }
    }

    /// <summary>
    /// 몬스터 이미지 색상 설정
    /// </summary>
    private void SetMonsterImageAndColor(bool isPurchaseable)
    {
        if (monsterImage == null || monsterData.icon == null) return;

        monsterImage.sprite = monsterData.icon;

        // 구매 상태에 따라 색상 설정
        if (isPurchaseable)
        {
            monsterImage.color = Color.white; // 구매 가능
        }
        else
        {
            monsterImage.color = Color.gray;  // 이미 구매함 (회색)
        }
    }

    /// <summary>
    /// 2개 버튼 이벤트 설정
    /// </summary>
    private void SetupButtons()
    {
        if (monsterData == null || shopUIManager == null)
            return;

        // 1번째 버튼: 상세 정보 표시
        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(ShowDetailPanel);
        }

        // 2번째 버튼: 바로 구매
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
    }

    /// <summary>
    /// 상세 정보 패널 표시 (1번째 버튼)
    /// </summary>
    private void ShowDetailPanel()
    {
        if (detailPanelManager != null && monsterData != null)
        {
            detailPanelManager.ShowMonsterDetail(monsterData, shopUIManager);
        }
        else
        {
            Debug.LogWarning("[ShopSMonsterButtonController] DetailPanel을 표시할 수 없습니다.");
        }
    }

    /// <summary>
    /// 구매 버튼 클릭 (2번째 버튼)
    /// </summary>
    private void OnPurchaseClicked()
    {
        if (monsterData == null || shopUIManager == null) return;

        // UIManager의 구매 메서드 호출
        shopUIManager.OnSpecialMonsterPurchaseClicked(monsterData.enemyId);

        // 구매 후 UI 업데이트
        UpdateUI();

        // 상세 패널 표시 (구매 후 바로 정보 보여주기)
        ShowDetailPanel();
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// 현재 몬스터 ID 반환
    /// </summary>
    public string GetMonsterID()
    {
        return monsterData?.enemyId;
    }

    /// <summary>
    /// 현재 몬스터 데이터 반환
    /// </summary>
    public UnitStock GetMonsterData()
    {
        return monsterData;
    }

    /// <summary>
    /// 특수 메타 정보 반환
    /// </summary>
    public MonsterCatalog.SpecialMeta GetSpecialMeta()
    {
        return specialMeta;
    }

    /// <summary>
    /// 구매 가능 여부 확인
    /// </summary>
    public bool IsPurchaseable()
    {
        return monsterData != null && monsterData.owned == 0;
    }

    /// <summary>
    /// 상점 타입 확인 (일반/비밀 교환소)
    /// </summary>
    public bool IsNormalShop()
    {
        return specialMeta?.shop ?? true;
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// 디버그용 상태 출력
    /// </summary>
    [ContextMenu("Debug Button Status")]
    public void DebugButtonStatus()
    {
        Debug.Log("=== ShopSMonsterButtonController 상태 ===");
        Debug.Log($"몬스터 이름: {monsterData?.displayName ?? "없음"}");
        Debug.Log($"몬스터 ID: {GetMonsterID()}");
        Debug.Log($"구매 가능: {IsPurchaseable()}");
        Debug.Log($"보유량: {monsterData?.owned ?? 0}");

        if (specialMeta != null)
        {
            Debug.Log($"미스릴 가격: {specialMeta.price}");
            Debug.Log($"상점 타입: {(specialMeta.shop ? "일반" : "비밀")} 교환소");
        }

        Debug.Log($"UI 컴포넌트 - 이미지: {monsterImage != null}, 이름: {monsterNameText != null}, 가격: {priceMithrilText != null}");
        Debug.Log($"버튼 - 상세: {detailButton != null}, 구매: {purchaseButton != null}");
        Debug.Log($"MonsterCatalog: {catalogCache != null}");
        Debug.Log($"UIManager: {shopUIManager != null}");
        Debug.Log($"DetailPanel: {detailPanelManager != null}");
    }
    #endregion
}
