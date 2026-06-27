using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 자원 교환 시스템 (InputField 기반)
/// - 12가지 교환 조합 지원
/// - 입력값 자동 제한 및 확인 패널
/// </summary>
public class ResourceExchangeManager : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameObject changePanelUI;        // 교환 입력 패널
    [SerializeField] private GameObject changeCheckPanelUI;   // 확인 패널
    [SerializeField] private InputField exchangeInputField;   // 교환 수량 입력 필드
    [SerializeField] private Text maxExchangeText;            // 최대 교환 가능 개수 표시
    [SerializeField] private Text confirmMessageText;         // 확인 메시지 텍스트
    [SerializeField] private Text complteMessageText;         // 구매 실패 메시지 텍스트
    

    // 현재 교환 정보
    private ExchangeType currentExchangeType;
    private int currentMaxExchangeable;

    /// <summary>
    /// 교환 타입 열거형 (12가지 조합)
    /// </summary>
    public enum ExchangeType
    {
        GoldToIron,      // 골드 → 철
        GoldToDiamond,   // 골드 → 다이아
        GoldToMithril,   // 골드 → 미스릴
        IronToGold,      // 철 → 골드
        IronToDiamond,   // 철 → 다이아
        IronToMithril,   // 철 → 미스릴
        DiamondToGold,   // 다이아 → 골드
        DiamondToIron,   // 다이아 → 철
        DiamondToMithril,// 다이아 → 미스릴
        MithrilToGold,   // 미스릴 → 골드
        MithrilToIron,   // 미스릴 → 철
        MithrilToDiamond // 미스릴 → 다이아
    }

    void Start()
    {
        // UIManager 자동 찾기
        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
        }

        // InputField 이벤트 연결
        if (exchangeInputField != null)
        {
            exchangeInputField.onValueChanged.AddListener(OnInputValueChanged);
        }

        // 초기에는 패널 숨김
        HideAllPanels();
    }

    #region 12가지 교환 버튼 메서드

    // Gold → Iron (골드 100 → 철 1)
    public void OpenExchange_GoldToIron()
    {
        OpenExchangePanel(ExchangeType.GoldToIron);
    }

    // Gold → Diamond (골드 500 → 다이아 1)
    public void OpenExchange_GoldToDiamond()
    {
        OpenExchangePanel(ExchangeType.GoldToDiamond);
    }

    // Gold → Mithril (골드 50000 → 미스릴 1)
    public void OpenExchange_GoldToMithril()
    {
        OpenExchangePanel(ExchangeType.GoldToMithril);
    }

    // Iron → Gold (철 1 → 골드 100)
    public void OpenExchange_IronToGold()
    {
        OpenExchangePanel(ExchangeType.IronToGold);
    }

    // Iron → Diamond (철 5 → 다이아 1)
    public void OpenExchange_IronToDiamond()
    {
        OpenExchangePanel(ExchangeType.IronToDiamond);
    }

    // Iron → Mithril (철 500 → 미스릴 1)
    public void OpenExchange_IronToMithril()
    {
        OpenExchangePanel(ExchangeType.IronToMithril);
    }

    // Diamond → Gold (다이아 1 → 골드 500)
    public void OpenExchange_DiamondToGold()
    {
        OpenExchangePanel(ExchangeType.DiamondToGold);
    }

    // Diamond → Iron (다이아 1 → 철 5)
    public void OpenExchange_DiamondToIron()
    {
        OpenExchangePanel(ExchangeType.DiamondToIron);
    }

    // Diamond → Mithril (다이아 100 → 미스릴 1)
    public void OpenExchange_DiamondToMithril()
    {
        OpenExchangePanel(ExchangeType.DiamondToMithril);
    }

    // Mithril → Gold (미스릴 1 → 골드 50000)
    public void OpenExchange_MithrilToGold()
    {
        OpenExchangePanel(ExchangeType.MithrilToGold);
    }

    // Mithril → Iron (미스릴 1 → 철 500)
    public void OpenExchange_MithrilToIron()
    {
        OpenExchangePanel(ExchangeType.MithrilToIron);
    }

    // Mithril → Diamond (미스릴 1 → 다이아 100)
    public void OpenExchange_MithrilToDiamond()
    {
        OpenExchangePanel(ExchangeType.MithrilToDiamond);
    }

    #endregion

    #region 교환 패널 열기/닫기

    /// <summary>
    /// 교환 패널 열기 (InputField 입력 창)
    /// </summary>
    private void OpenExchangePanel(ExchangeType exchangeType)
    {
        if (uiManager == null)
        {
            Debug.LogError("[ResourceExchangeManager] UIManager를 찾을 수 없습니다!");
            return;
        }

        currentExchangeType = exchangeType;
        currentMaxExchangeable = CalculateMaxExchangeable(exchangeType);

        // 교환 불가능한 경우
        if (currentMaxExchangeable <= 0)
        {
            Debug.Log($"[ResourceExchangeManager] {GetResourceName(exchangeType, true)} 부족으로 교환 불가능");

            StartCoroutine(FadeIn(1f));

            return;

        }

        // InputField 초기화
        if (exchangeInputField != null)
        {
            exchangeInputField.text = "1";
        }

        // 최대 교환 가능 개수 표시
        if (maxExchangeText != null)
        {
            maxExchangeText.text = $"최대 교환 가능: {currentMaxExchangeable}";
        }

        // 패널 표시
        if (changePanelUI != null)
        {
            changePanelUI.SetActive(true);
        }
    }

    /// <summary>
    /// 교환 패널 닫기
    /// </summary>
    public void CloseExchangePanel()
    {
        if (changePanelUI != null)
        {
            changePanelUI.SetActive(false);
        }
    }

    /// <summary>
    /// 확인 패널 닫기
    /// </summary>
    public void CloseCheckPanel()
    {
        if (changeCheckPanelUI != null)
        {
            changeCheckPanelUI.SetActive(false);
        }
    }

    /// <summary>
    /// 모든 패널 숨김
    /// </summary>
    private void HideAllPanels()
    {
        if (changePanelUI != null)
            changePanelUI.SetActive(false);
        if (changeCheckPanelUI != null)
            changeCheckPanelUI.SetActive(false);
    }

    #endregion

    #region InputField 처리

    /// <summary>
    /// InputField 값 변경 시 호출
    /// - 최대치 초과 시 자동으로 최대치로 조정
    /// </summary>
    private void OnInputValueChanged(string input)
    {
        if (string.IsNullOrEmpty(input))
            return;

        // 숫자 파싱
        if (int.TryParse(input, out int value))
        {
            // 최대치 초과 시 자동 조정
            if (value > currentMaxExchangeable)
            {
                exchangeInputField.text = currentMaxExchangeable.ToString();
            }
            // 0 이하 입력 방지
            else if (value <= 0)
            {
                exchangeInputField.text = "1";
            }
        }
        else
        {
            // 숫자가 아닌 경우 1로 초기화
            exchangeInputField.text = "1";
        }
    }

    #endregion

    #region 교환 확인 및 실행

    /// <summary>
    /// 교환하기 버튼 클릭 (ChangePanel → ChangeCheckPanel)
    /// </summary>
    public void OnExchangeButtonClicked()
    {
        if (exchangeInputField == null || string.IsNullOrEmpty(exchangeInputField.text))
        {
            Debug.LogError("[ResourceExchangeManager] 교환 수량이 입력되지 않았습니다!");
            return;
        }

        int exchangeAmount = int.Parse(exchangeInputField.text);

        // 확인 메시지 생성
        string message = GenerateConfirmMessage(currentExchangeType, exchangeAmount);
        if (confirmMessageText != null)
        {
            confirmMessageText.text = message;
        }

        // 패널 전환
        CloseExchangePanel();
        if (changeCheckPanelUI != null)
        {
            changeCheckPanelUI.SetActive(true);
        }
    }

    /// <summary>
    /// 교환 확정 버튼 클릭 (실제 교환 실행)
    /// </summary>
    public void OnConfirmExchangeClicked()
    {
        if (exchangeInputField == null || string.IsNullOrEmpty(exchangeInputField.text))
        {
            Debug.LogError("[ResourceExchangeManager] 교환 수량이 입력되지 않았습니다!");
            return;
        }

        int exchangeAmount = int.Parse(exchangeInputField.text);

        // 실제 교환 실행
        ExecuteExchange(currentExchangeType, exchangeAmount);

        // 패널 닫기
        CloseCheckPanel();
    }

    /// <summary>
    /// 실제 교환 실행
    /// </summary>
    private void ExecuteExchange(ExchangeType exchangeType, int amount)
    {
        if (uiManager == null) return;

        // StaticInfoManager를 먼저 수정 (단일 진실 공급원)
        switch (exchangeType)
        {
            case ExchangeType.GoldToIron:
                StaticInfoManager.gold -= 100 * amount;
                StaticInfoManager.iron += 1 * amount;
                break;
            case ExchangeType.GoldToDiamond:
                StaticInfoManager.gold -= 500 * amount;
                StaticInfoManager.dia += 1 * amount;
                break;
            case ExchangeType.GoldToMithril:
                StaticInfoManager.gold -= 50000 * amount;
                StaticInfoManager.mis += 1 * amount;
                break;
            case ExchangeType.IronToGold:
                StaticInfoManager.iron -= 1 * amount;
                StaticInfoManager.gold += 100 * amount;
                break;
            case ExchangeType.IronToDiamond:
                StaticInfoManager.iron -= 5 * amount;
                StaticInfoManager.dia += 1 * amount;
                break;
            case ExchangeType.IronToMithril:
                StaticInfoManager.iron -= 500 * amount;
                StaticInfoManager.mis += 1 * amount;
                break;
            case ExchangeType.DiamondToGold:
                StaticInfoManager.dia -= 1 * amount;
                StaticInfoManager.gold += 500 * amount;
                break;
            case ExchangeType.DiamondToIron:
                StaticInfoManager.dia -= 1 * amount;
                StaticInfoManager.iron += 5 * amount;
                break;
            case ExchangeType.DiamondToMithril:
                StaticInfoManager.dia -= 100 * amount;
                StaticInfoManager.mis += 1 * amount;
                break;
            case ExchangeType.MithrilToGold:
                StaticInfoManager.mis -= 1 * amount;
                StaticInfoManager.gold += 50000 * amount;
                break;
            case ExchangeType.MithrilToIron:
                StaticInfoManager.mis -= 1 * amount;
                StaticInfoManager.iron += 500 * amount;
                break;
            case ExchangeType.MithrilToDiamond:
                StaticInfoManager.mis -= 1 * amount;
                StaticInfoManager.dia += 100 * amount;
                break;
        }

        // UI 업데이트 (자원 표시만, 상점 재생성 안함)
        uiManager.UpdateUI(refreshPanels: false);

        Debug.Log($"[ResourceExchangeManager] 교환 완료: {exchangeType}, 수량: {amount}");
    }

    #endregion

    #region 유틸리티 메서드

    /// <summary>
    /// 최대 교환 가능한 개수 계산 (StaticInfoManager 기준)
    /// </summary>
    private int CalculateMaxExchangeable(ExchangeType exchangeType)
    {
        switch (exchangeType)
        {
            case ExchangeType.GoldToIron:
                return StaticInfoManager.gold / 100;
            case ExchangeType.GoldToDiamond:
                return StaticInfoManager.gold / 500;
            case ExchangeType.GoldToMithril:
                return StaticInfoManager.gold / 50000;
            case ExchangeType.IronToGold:
                return StaticInfoManager.iron / 1;
            case ExchangeType.IronToDiamond:
                return StaticInfoManager.iron / 5;
            case ExchangeType.IronToMithril:
                return StaticInfoManager.iron / 500;
            case ExchangeType.DiamondToGold:
                return StaticInfoManager.dia / 1;
            case ExchangeType.DiamondToIron:
                return StaticInfoManager.dia / 1;
            case ExchangeType.DiamondToMithril:
                return StaticInfoManager.dia / 100;
            case ExchangeType.MithrilToGold:
                return StaticInfoManager.mis / 1;
            case ExchangeType.MithrilToIron:
                return StaticInfoManager.mis / 1;
            case ExchangeType.MithrilToDiamond:
                return StaticInfoManager.mis / 1;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 확인 메시지 생성
    /// </summary>
    private string GenerateConfirmMessage(ExchangeType exchangeType, int amount)
    {
        string fromResource = GetResourceName(exchangeType, true);
        string toResource = GetResourceName(exchangeType, false);
        int fromAmount = GetResourceAmount(exchangeType, amount, true);
        int toAmount = GetResourceAmount(exchangeType, amount, false);

        return $"{fromResource} {fromAmount}개를\n{toResource} {toAmount}개로 바꾸시겠습니까?";
    }

    /// <summary>
    /// 자원 이름 가져오기
    /// </summary>
    private string GetResourceName(ExchangeType exchangeType, bool isFrom)
    {
        switch (exchangeType)
        {
            case ExchangeType.GoldToIron:
                return isFrom ? "골드" : "철";
            case ExchangeType.GoldToDiamond:
                return isFrom ? "골드" : "다이아";
            case ExchangeType.GoldToMithril:
                return isFrom ? "골드" : "미스릴";
            case ExchangeType.IronToGold:
                return isFrom ? "철" : "골드";
            case ExchangeType.IronToDiamond:
                return isFrom ? "철" : "다이아";
            case ExchangeType.IronToMithril:
                return isFrom ? "철" : "미스릴";
            case ExchangeType.DiamondToGold:
                return isFrom ? "다이아" : "골드";
            case ExchangeType.DiamondToIron:
                return isFrom ? "다이아" : "철";
            case ExchangeType.DiamondToMithril:
                return isFrom ? "다이아" : "미스릴";
            case ExchangeType.MithrilToGold:
                return isFrom ? "미스릴" : "골드";
            case ExchangeType.MithrilToIron:
                return isFrom ? "미스릴" : "철";
            case ExchangeType.MithrilToDiamond:
                return isFrom ? "미스릴" : "다이아";
            default:
                return "";
        }
    }

    /// <summary>
    /// 자원 수량 계산
    /// </summary>
    private int GetResourceAmount(ExchangeType exchangeType, int exchangeCount, bool isFrom)
    {
        switch (exchangeType)
        {
            case ExchangeType.GoldToIron:
                return isFrom ? 100 * exchangeCount : 1 * exchangeCount;
            case ExchangeType.GoldToDiamond:
                return isFrom ? 500 * exchangeCount : 1 * exchangeCount;
            case ExchangeType.GoldToMithril:
                return isFrom ? 50000 * exchangeCount : 1 * exchangeCount;
            case ExchangeType.IronToGold:
                return isFrom ? 1 * exchangeCount : 100 * exchangeCount;
            case ExchangeType.IronToDiamond:
                return isFrom ? 5 * exchangeCount : 1 * exchangeCount;
            case ExchangeType.IronToMithril:
                return isFrom ? 500 * exchangeCount : 1 * exchangeCount;
            case ExchangeType.DiamondToGold:
                return isFrom ? 1 * exchangeCount : 500 * exchangeCount;
            case ExchangeType.DiamondToIron:
                return isFrom ? 1 * exchangeCount : 5 * exchangeCount;
            case ExchangeType.DiamondToMithril:
                return isFrom ? 100 * exchangeCount : 1 * exchangeCount;
            case ExchangeType.MithrilToGold:
                return isFrom ? 1 * exchangeCount : 50000 * exchangeCount;
            case ExchangeType.MithrilToIron:
                return isFrom ? 1 * exchangeCount : 500 * exchangeCount;
            case ExchangeType.MithrilToDiamond:
                return isFrom ? 1 * exchangeCount : 100 * exchangeCount;
            default:
                return 0;
        }
    }

    #endregion



    /// <summary>
    /// 페이드 인 효과
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        complteMessageText.gameObject.SetActive(true);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // SmoothStep으로 부드러운 페이드 적용
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            complteMessageText.color = new Color(complteMessageText.color.r, complteMessageText.color.g, complteMessageText.color.b, smoothT);
            yield return null;
        }
        if(elapsed > duration)
        {
            StartCoroutine(FadeOut(1f));
        }
    }

    /// <summary>
    /// 페이드 아웃 효과
    /// </summary>
    private IEnumerator FadeOut(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // SmoothStep으로 부드러운 페이드 적용
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            complteMessageText.color = new Color(complteMessageText.color.r, complteMessageText.color.g, complteMessageText.color.b, 1 - smoothT);
            yield return null;
        }
        complteMessageText.gameObject.SetActive(false);
    }

}
