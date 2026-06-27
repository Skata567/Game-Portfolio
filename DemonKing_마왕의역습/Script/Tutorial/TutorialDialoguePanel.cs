using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 대화 패널
/// 대사를 표시하고 사용자 입력을 받아 다음 단계로 진행
/// </summary>
public class TutorialDialoguePanel : MonoBehaviour
{
    #region Fields

    [Header("UI 요소")]
    [SerializeField] private GameObject panel;              // 패널 루트 오브젝트
    [SerializeField] private Text dialogueText;             // 대사 텍스트
    [SerializeField] private Button nextButton;             // "다음" 버튼
    [SerializeField] private Text nextButtonText;           // 다음 버튼 텍스트

    [Header("옵션")]
    [SerializeField] private Image characterImage;          // 캐릭터 이미지 (옵션)

    // 현재 대화 완료 시 호출될 콜백
    private Action onDialogueComplete;

    // 다음 버튼 클릭 후 자동으로 숨길지 여부
    private bool shouldAutoHide = true;

    // CanvasGroup for visibility control
    private CanvasGroup canvasGroup;

    // 메인 UI 버튼들 (대화 중 비활성화용)
    private Button[] mainUIButtons;
    private bool[] originalButtonStates;  // 원래 활성화 상태 저장

    private bool skipTyping = false;
    private Coroutine typingCoroutine;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        Debug.Log("[TutorialDialoguePanel] Awake() 호출됨");

        // 튜토리얼 완료 여부 체크
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        if (tutorialCompleted == 1)
        {
            Debug.Log("[TutorialDialoguePanel] 튜토리얼 이미 완료됨 - 자동 파괴");
            Destroy(gameObject);
            return;
        }

        // 씬 전환 시에도 유지 (배틀씬에서도 대화창 사용)
        DontDestroyOnLoad(gameObject);

        // panel이 없으면 자기 자신을 패널로 설정
        if (panel == null)
        {
            panel = gameObject;
            Debug.Log($"[TutorialDialoguePanel] Panel 자동 설정: {panel.name}");
        }
        else
        {
            Debug.Log($"[TutorialDialoguePanel] Panel 이미 연결됨: {panel.name}");
        }

        // CanvasGroup 가져오기 또는 추가 (Awake에서 초기화!)
        if (panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
                Debug.Log("[TutorialDialoguePanel] CanvasGroup 자동 추가됨");
            }
        }

        // dialogueText가 없으면 자동으로 찾기
        if (dialogueText == null)
        {
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text.name.ToLower().Contains("dialogue") || text.name.ToLower().Contains("content"))
                {
                    dialogueText = text;
                    Debug.Log($"[TutorialDialoguePanel] DialogueText 자동 찾음: {dialogueText.name}");
                    break;
                }
            }
        }

        // nextButton이 없으면 자동으로 찾기
        if (nextButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name.ToLower().Contains("next"))
                {
                    nextButton = button;
                    // 버튼의 Text 컴포넌트도 찾기
                    nextButtonText = button.GetComponentInChildren<Text>();
                    Debug.Log($"[TutorialDialoguePanel] NextButton 자동 찾음: {nextButton.name}");
                    break;
                }
            }
        }
    }

    void Start()
    {
        Debug.Log("[TutorialDialoguePanel] Start() 호출됨");

        // 시작 시 패널 숨김 (CanvasGroup만 사용, GameObject는 활성화 유지)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Debug.Log("[TutorialDialoguePanel] Panel 숨김 완료 (CanvasGroup alpha=0)");
        }

        // "다음" 버튼 이벤트 연결
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
            Debug.Log("[TutorialDialoguePanel] NextButton 이벤트 연결 완료");
        }
        else
        {
            Debug.LogWarning("[TutorialDialoguePanel]  NextButton이 null입니다!");
        }

        if(panel != null)
        {
            Button panelButton = panel.GetComponent<Button>();
            if(panelButton == null)
            {
               panelButton = panel.AddComponent<Button>();
            }
            panelButton.onClick.AddListener(OnPanelClicked);

        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 대화 표시
    /// </summary>
    /// <param name="dialogue">표시할 대사</param>
    /// <param name="onComplete">대화 완료 시 호출될 콜백</param>
    /// <param name="autoHide">다음 버튼 클릭 후 자동으로 숨길지 여부 (기본값: true)</param>
    public void ShowDialogue(string dialogue, Action onComplete, bool autoHide = true)
    {
        Debug.Log($"[TutorialDialoguePanel] ShowDialogue 호출됨!");

        if(typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

        }

        // CanvasGroup이 없으면 즉시 초기화 (Awake가 안 불렸을 경우 대비)
        EnsureCanvasGroupInitialized();

        // CanvasGroup으로 패널 표시 (GameObject는 항상 활성화 유지)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log($"[TutorialDialoguePanel] Panel 표시 (alpha=1, interactable=true)");
        }
        else
        {
            Debug.LogError("[TutorialDialoguePanel] CanvasGroup이 null입니다!");
        }

        // 대사 텍스트 설정
        if (dialogueText != null)
        {
            typingCoroutine = StartCoroutine(TypeDialogue(dialogue));
            Debug.Log($"[TutorialDialoguePanel] 대사 설정: {dialogue.Substring(0, Mathf.Min(30, dialogue.Length))}...");
        }
        else
        {
            Debug.LogError("[TutorialDialoguePanel]  DialogueText가 null입니다!");
        }

        // 콜백 저장
        onDialogueComplete = onComplete;

        // 자동 숨김 옵션 저장
        shouldAutoHide = autoHide;

        // 대화 표시 시 메인 UI 버튼들 비활성화
        DisableMainUIButtons();
    }

    /// <summary>
    /// CanvasGroup 초기화 보장 (Awake가 실행되지 않았을 경우 대비)
    /// </summary>
    private void EnsureCanvasGroupInitialized()
    {
        if (canvasGroup != null) return;

        // panel 확인
        if (panel == null)
        {
            panel = gameObject;
            Debug.Log($"[TutorialDialoguePanel] Panel 늦은 초기화: {panel.name}");
        }

        // CanvasGroup 추가
        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
            Debug.Log("[TutorialDialoguePanel] CanvasGroup 늦은 추가됨 (Awake가 실행되지 않았음)");
        }
    }


    /// <summary>
    /// 대화 표시 (캐릭터 이미지 포함)
    /// </summary>
    public void ShowDialogue(string dialogue, Sprite characterSprite, Action onComplete)
    {
        // 캐릭터 이미지 설정
        if (characterImage != null && characterSprite != null)
        {
            characterImage.sprite = characterSprite;
            characterImage.gameObject.SetActive(true);
        }

        ShowDialogue(dialogue, onComplete);
       
    }


    /// <summary>
    /// 대화 패널 숨기기
    /// </summary>
    public void HideDialogue()
    {
        // CanvasGroup 초기화 보장
        EnsureCanvasGroupInitialized();

        // CanvasGroup으로 패널 숨김 (GameObject는 활성화 유지)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Debug.Log("[TutorialDialoguePanel] Panel 숨김 (alpha=0, interactable=false)");
        }
        
    }

    /// <summary>
    /// 다음 버튼 텍스트 변경
    /// </summary>
    public void SetNextButtonText(string text)
    {
        if (nextButtonText != null)
        {
            nextButtonText.text = text;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// "다음" 버튼 클릭 이벤트
    /// </summary>
    private void OnNextButtonClicked()
    {


        if (typingCoroutine != null)
        {
            Debug.Log("[TutorialDialoguePanel] 타이핑 중 - 스킵 처리");
            skipTyping = true;
            return;
        }

        // 1. 콜백을 먼저 저장 (HideDialogue에서 초기화되기 전에)
        Action callback = onDialogueComplete;

        // 2. 자동 숨김이 활성화되어 있으면 패널 숨김
        if (shouldAutoHide)
        {
            HideDialogue();
            Debug.Log("[TutorialDialoguePanel] 자동 숨김 - 패널 숨김");
        }
        else
        {
            Debug.Log("[TutorialDialoguePanel] 자동 숨김 비활성화 - 패널 유지");
        }

        // 3. 저장된 콜백 실행 (숨긴 후에 실행)
        Debug.Log($"[TutorialDialoguePanel] 콜백 실행: {callback != null}");
        callback?.Invoke();

        // 4. 메인 UI 버튼들 다시 활성화
        EnableMainUIButtons();
    }

    /// <summary>
    /// 메인 UI 버튼들 비활성화 (대화 중 다른 패널로 이동 방지)
    /// </summary>
    private void DisableMainUIButtons()
    {
        // 모든 버튼 찾기
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 메인 UI 버튼들 필터링 (상점, 모집, 강화, 도감 등)
        System.Collections.Generic.List<Button> tempList = new System.Collections.Generic.List<Button>();
        System.Collections.Generic.List<bool> tempStates = new System.Collections.Generic.List<bool>();

        foreach (var button in allButtons)
        {
            string name = button.name.ToLower();

            // 메인 UI 버튼 판별 (상점, 모집, 강화, 도감, 편성, 설정 등)
            if (name.Contains("shop")      || name.Contains("상점") ||
                name.Contains("recruit")   || name.Contains("모집") ||
                name.Contains("enhance")   || name.Contains("강화") ||
                name.Contains("codex")     || name.Contains("도감") ||
                name.Contains("formation") || name.Contains("편성") ||
                name.Contains("setting")   || name.Contains("설정") ||
                name.Contains("inventory") || name.Contains("인벤토리"))
            {
                // Next 버튼은 제외
                if (button == nextButton) continue;

                tempList.Add(button);
                tempStates.Add(button.interactable);
                button.interactable = false;

                /*Debug.Log($"[TutorialDialoguePanel] 메인 버튼 비활성화: {button.name}");*/
            }
        }

        mainUIButtons = tempList.ToArray();
        originalButtonStates = tempStates.ToArray();
    }

    /// <summary>
    /// 메인 UI 버튼들 다시 활성화
    /// </summary>
    private void EnableMainUIButtons()
    {
        if (mainUIButtons == null || originalButtonStates == null) return;

        for (int i = 0; i < mainUIButtons.Length; i++)
        {
            if (mainUIButtons[i] != null)
            {
                mainUIButtons[i].interactable = originalButtonStates[i];
               /* Debug.Log($"[TutorialDialoguePanel] 메인 버튼 활성화: {mainUIButtons[i].name}");*/
            }
        }

        mainUIButtons = null;
        originalButtonStates = null;
    }

    /// <summary>
    /// 타이핑 모션 구현
    /// </summary>
    /// <param name="fullText"></param>
    /// <returns></returns>
    private IEnumerator TypeDialogue(string fullText)
    {
        dialogueText.text = ""; //텍스트 초기화
        skipTyping = false;

        foreach(char c in fullText)
        {
            if(skipTyping)
            {
                dialogueText.text = fullText;
                typingCoroutine = null;
                yield break;
            }
            dialogueText.text += c;

            if(c != ' ' && c != '\n')
            {
                if(SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX("타이핑");
                }
            }
            yield return new WaitForSeconds(0.05f);
        }
        typingCoroutine = null;
    }

    private void OnPanelClicked()
    {
        if(!skipTyping)
        {
            skipTyping = true;
        }
    }    



    #endregion
}
