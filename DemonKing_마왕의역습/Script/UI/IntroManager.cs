using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 인트로 시퀀스 관리자
/// 배경 이미지와 대사를 순차적으로 표시하며 페이드 인/아웃 효과 적용
///
/// 사용 예시:
/// 1. Unity Inspector에서 introScenes 배열 크기 설정 (예: 5)
/// 2. 각 장면의 배경 이미지(backgroundImage)와 대사(dialogue) 입력
/// 3. displayDuration: 0 = 사용자 클릭 대기, 0보다 크면 자동 진행
/// 4. 인트로 씬 시작 시 자동으로 재생됨
/// </summary>
public class IntroManager : MonoBehaviour
{
    #region 데이터 구조

    /// <summary>
    /// 인트로 장면 데이터
    /// </summary>
    [System.Serializable]
    public class IntroScene
    {
        [Header("장면 설정")]
        public Sprite backgroundImage;              // 배경 이미지
        [TextArea(3, 5)]
        public string dialogue;                     // 대사 (빈 문자열이면 이미지만 표시)

        [Header("타이밍")]
        public float displayDuration = 0f;          // 표시 시간 (0이면 클릭 대기)
        public float fadeInDuration = 1f;           // 페이드 인 시간
        public float fadeOutDuration = 1f;          // 페이드 아웃 시간
    }

    #endregion

    #region UI 레퍼런스

    [Header("UI 요소")]
    [SerializeField] private Image backgroundImage;         // 배경 이미지 표시용
    [SerializeField] private Text dialogueText;             // 대사 텍스트 표시용
    [SerializeField] private GameObject escText;                  // 스킵 텍스트 표시
    [SerializeField] private CanvasGroup canvasGroup;       // 페이드 효과용
    [SerializeField] private GameObject skipButton;         // 스킵 버튼 (옵션)

    #endregion

    #region 인트로 데이터

    [Header("인트로 장면들")]
    [SerializeField] private IntroScene[] introScenes;      // 인트로 장면 배열

    [Header("설정")]
    [SerializeField] private float typingSpeed = 0.1f;     // 타이핑 속도 (초/글자)
    [SerializeField] private string nextSceneName = "MainScene"; // 인트로 후 이동할 씬

    #endregion

    #region 상태 변수

    private int currentSceneIndex = 0;                      // 현재 장면 인덱스
    private bool isTyping = false;                          // 타이핑 중 여부
    private bool skipTyping = false;                        // 타이핑 스킵 플래그
    private bool canProceed = false;                        // 다음 장면 진행 가능 여부
   /* private bool isSkipConfirmationMode = false;            // ESC스킵 표시 여부*/
    private Coroutine currentCoroutine;                     // 현재 실행 중인 코루틴

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        // CanvasGroup 자동 찾기
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 대사 텍스트 초기화
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        // 인트로 시작
        StartIntro();
    }

    void Update()
    {
        // 클릭이나 스페이스바로 진행
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isTyping)
            {
                // 타이핑 중이면 스킵
                skipTyping = true;
            }
            else if (canProceed)
            {
                // 타이핑 완료 후 다음 장면으로
                ProceedToNextScene();
            }
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            // ESC로 전체 스킵
            SkipIntro();

        }
    }

    #endregion

    #region 인트로 제어

    /// <summary>
    /// 인트로 시작
    /// </summary>
    private void StartIntro()
    {
        if (introScenes == null || introScenes.Length == 0)
        {
            Debug.LogWarning("[IntroManager] 인트로 장면이 설정되지 않았습니다. 다음 씬으로 이동합니다.");
            LoadNextScene();
            return;
        }

        currentSceneIndex = 0;
        currentCoroutine = StartCoroutine(PlaySceneSequence());
    }

    /// <summary>
    /// 장면 시퀀스 재생
    /// </summary>
    private IEnumerator PlaySceneSequence()
    {
        while (currentSceneIndex < introScenes.Length)
        {
            IntroScene scene = introScenes[currentSceneIndex];

            // 배경 이미지 설정
            if (backgroundImage != null && scene.backgroundImage != null)
            {
                backgroundImage.sprite = scene.backgroundImage;
            }

            // 대사 텍스트 초기화 (새 장면 시작 시 즉시 지우기)
            if (dialogueText != null)
            {
                dialogueText.text = "";
            }

            // 페이드 인
            yield return StartCoroutine(FadeIn(scene.fadeInDuration));

            // 대사 타이핑 (대사가 있는 경우)
            if (!string.IsNullOrEmpty(scene.dialogue) && dialogueText != null)
            {
                yield return StartCoroutine(TypeDialogue(scene.dialogue));
            }

            // 표시 시간 대기 (0이면 사용자 입력 대기)
            if (scene.displayDuration > 0f)
            {
                canProceed = false;
                yield return new WaitForSeconds(scene.displayDuration);
            }
            else
            {
                // 사용자 입력 대기
                canProceed = true;
                yield return new WaitUntil(() => !canProceed); // ProceedToNextScene()에서 false로 변경됨
            }

            // 페이드 아웃
            yield return StartCoroutine(FadeOut(scene.fadeOutDuration));

            currentSceneIndex++;
        }

        // 모든 장면 완료 - 다음 씬으로 이동
        LoadNextScene();
    }

    /// <summary>
    /// 다음 장면으로 진행
    /// </summary>
    private void ProceedToNextScene()
    {
        canProceed = false; // WaitUntil 조건 해제
    }

    /// <summary>
    /// 인트로 전체 스킵
    /// </summary>
    public void SkipIntro()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        LoadNextScene();
    }

    /// <summary>
    /// 다음 씬 로드
    /// </summary>
    private void LoadNextScene()
    {
        Debug.Log($"[IntroManager] 인트로 종료 - {nextSceneName} 씬으로 이동");
        LoadingSceneManager.LoadScene(nextSceneName);
    }

    #endregion

    #region 페이드 효과

    /// <summary>
    /// 페이드 인 효과
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // SmoothStep으로 부드러운 페이드 적용
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            backgroundImage.color = new Color(1, 1, 1, smoothT);
            dialogueText.color = new Color(1, 1,1, smoothT);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 페이드 아웃 효과
    /// </summary>
    private IEnumerator FadeOut(float duration)
    {
        // TODO(human): 이미지만 서서히 사라져서 검정색이 되게 하기
        // 힌트: backgroundImage.color를 사용하여 alpha 값을 1에서 0으로 변경
      

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // SmoothStep으로 부드러운 페이드 적용
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            backgroundImage.color = new Color(1, 1, 1, 1 - smoothT);
            dialogueText.color = new Color(1, 1,1, 1 - smoothT);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    #endregion

    #region 타이핑 효과

    /// <summary>
    /// 대사 타이핑 효과
    /// TutorialDialoguePanel과 동일한 방식
    /// </summary>
    private IEnumerator TypeDialogue(string fullText)
    {
        isTyping = true;
        skipTyping = false;
        dialogueText.text = "";

        foreach (char c in fullText)
        {
            if (skipTyping)
            {
                dialogueText.text = fullText;
                break;
            }

            dialogueText.text += c;

            // 공백과 줄바꿈은 사운드 재생 안 함
            if (c != ' ' && c != '\n')
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX("타이핑");
                }
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        skipTyping = false;
    }

    #endregion
}
