using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 시작 여부를 선택하는 패널
/// 게임 시작 시 표시되며, 이미 완료한 경우 자동으로 숨김
/// </summary>
public class TutorialStartPanel : MonoBehaviour
{
    #region Fields

    [Header("UI 요소")]
    [SerializeField] private GameObject panel;              // 패널 루트 오브젝트
    [SerializeField] private Button startButton;            // "튜토리얼 시작" 버튼
    [SerializeField] private Button skipButton;             // "건너뛰기" 버튼

    [Header("옵션")]
    [SerializeField] private Text titleText;                // 제목 텍스트 (옵션)
    [SerializeField] private Text descriptionText;          // 설명 텍스트 (옵션)

    [Header("개발자 옵션")]
    [Tooltip("Unity Editor에서 매번 튜토리얼을 다시 표시할지 여부 (테스트용)")]
    [SerializeField] private bool resetOnPlayInEditor = false;   // Editor에서 매번 리셋 (기본: false)

    #endregion

    #region Unity Lifecycle

    void Start()
    {
#if UNITY_EDITOR
        // Unity Editor에서 플레이할 때는 매번 튜토리얼 초기화 (테스트용)
        if (resetOnPlayInEditor)
        {
            PlayerPrefs.DeleteKey("TutorialCompleted");
            PlayerPrefs.Save();
            Debug.Log("[TutorialStartPanel] Unity Editor - 튜토리얼 상태 초기화됨");
        }
#endif

        // 이미 튜토리얼을 완료했는지 확인 (PlayerPrefs 직접 체크)
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        Debug.Log($"[TutorialStartPanel] TutorialCompleted 값: {tutorialCompleted}");

        if (tutorialCompleted == 1)
        {
            // 완료했으면 패널 숨기고 종료
            HidePanel();
            Debug.Log("[TutorialStartPanel] ✅ 튜토리얼 이미 완료됨 - 패널 숨김");
            return;
        }

        Debug.Log("[TutorialStartPanel] 튜토리얼 미완료 - 시작 패널 표시");

        // 완료하지 않았으면 패널 표시
        ShowPanel();

        // 버튼 이벤트 연결
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 패널 표시
    /// </summary>
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        // 다른 모든 버튼 비활성화 (튜토리얼 시작/스킵 버튼 제외)
        DisableOtherButtons();
    }

    /// <summary>
    /// 패널 숨김
    /// </summary>
    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // 다른 모든 버튼 활성화
        EnableOtherButtons();
    }

    /// <summary>
    /// 튜토리얼 패널 외의 모든 버튼 비활성화
    /// </summary>
    private void DisableOtherButtons()
    {
        // 씬의 모든 버튼 찾기
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            // 튜토리얼 시작/스킵 버튼은 제외
            if (button == startButton || button == skipButton)
            {
                continue;
            }

            // 튜토리얼 패널 내부의 버튼은 제외
            if (panel != null && button.transform.IsChildOf(panel.transform))
            {
                continue;
            }

            // 나머지 버튼 비활성화
            button.interactable = false;
            Debug.Log($"[TutorialStartPanel] 버튼 비활성화: {button.name}");
        }

        Debug.Log("[TutorialStartPanel] 튜토리얼 패널 표시 - 다른 버튼들 비활성화됨");
    }

    /// <summary>
    /// 비활성화했던 버튼들 다시 활성화
    /// </summary>
    private void EnableOtherButtons()
    {
        // 씬의 모든 버튼 찾기
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            button.interactable = true;
        }

        Debug.Log("[TutorialStartPanel] 튜토리얼 패널 숨김 - 모든 버튼 활성화됨");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// "튜토리얼 시작" 버튼 클릭
    /// </summary>
    private void OnStartButtonClicked()
    {
        Debug.Log("[TutorialStartPanel] 튜토리얼 시작 버튼 클릭");

        var rm =  FindAnyObjectByType<RuneCodexManager>();
        rm.PclearCodex();

        StaticInfoManager.squad1RuneSlots = 1;
        StaticInfoManager.squad2RuneSlots = 0;
        StaticInfoManager.squad3RuneSlots = 0;

        RuneDatabase.Instance.ClearDiscoveredRunes();
        // 패널 숨김
        HidePanel();

        // 튜토리얼 시작
        TutorialManager.Instance.StartTutorial();
    }

    /// <summary>
    /// "건너뛰기" 버튼 클릭
    /// </summary>
    private void OnSkipButtonClicked()
    {
        Debug.Log("[TutorialStartPanel] 건너뛰기 버튼 클릭");

        var rm = FindAnyObjectByType<RuneCodexManager>();
        rm.PclearCodex();

        StaticInfoManager.squad1RuneSlots = 1;
        StaticInfoManager.squad2RuneSlots = 0;
        StaticInfoManager.squad3RuneSlots = 0;

        RuneDatabase.Instance.ClearDiscoveredRunes();
        // 패널 숨김
        HidePanel();

        // 튜토리얼 건너뛰기 (완료 플래그 저장)
        TutorialManager.Instance.SkipTutorial();
    }

    #endregion

    #region Auto-Find (옵션)

    void Awake()
    {
        // ✅ 튜토리얼 완료 여부 체크 (Start 전에 먼저 체크)
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        if (tutorialCompleted == 1)
        {
            Debug.Log("[TutorialStartPanel] 튜토리얼 이미 완료됨 - 패널 숨김 및 비활성화");
            if (panel != null)
            {
                panel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

        // panel이 없으면 자기 자신을 패널로 설정
        if (panel == null)
        {
            panel = gameObject;
        }

        // startButton이 없으면 자동으로 찾기
        if (startButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name.ToLower().Contains("start") || button.name.ToLower().Contains("시작"))
                {
                    startButton = button;
                    break;
                }
            }
        }

        // skipButton이 없으면 자동으로 찾기
        if (skipButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name.ToLower().Contains("skip") || button.name.ToLower().Contains("건너"))
                {
                    skipButton = button;
                    break;
                }
            }
        }
    }

    #endregion
}
