using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    [Header("Target Scene")]
    [Tooltip("로딩이 끝난 뒤 최종적으로 활성화할 씬 이름입니다. 비워두면 MoveScene.PendingTargetSceneName 값을 사용합니다.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("MoveScene에서 넘어온 목표 씬도 없고 targetSceneName도 비어 있을 때 사용할 기본 목표 씬입니다.")]
    [SerializeField] private string fallbackTargetSceneName = "NYH_Grid";

    [Header("Loading Timing")]
    [Tooltip("로딩 화면을 최소 몇 초 동안 보여줄지 정합니다. 너무 짧으면 로딩 씬이 순간적으로 깜빡여 보일 수 있습니다.")]
    [SerializeField] private float minimumLoadingTime = 0.5f;

    // 중복 호출로 같은 씬 로딩 코루틴이 여러 개 실행되는 것을 막는다.
    // 버튼 연타, Start와 다른 스크립트의 BeginLoad 동시 호출 같은 상황을 방지하기 위한 안전장치다.
    private bool isLoading;

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(MoveScene.PendingTargetSceneName))
        {
            // 메뉴 씬에서 넘어온 목표 씬이 있으면 Inspector 값보다 우선 사용한다.
            // 이렇게 하면 로딩 씬 하나를 새 게임/이어하기/챌린지 이동에 재사용할 수 있다.
            targetSceneName = MoveScene.PendingTargetSceneName;
        }
        else if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            // 로딩 씬을 직접 실행했거나 버튼이 MoveScene을 거치지 않은 경우에도 기본 게임 씬으로 이동하게 한다.
            targetSceneName = fallbackTargetSceneName;
        }

        // 로딩 씬에 들어오면 목표 씬 로드를 바로 시작한다.
        // 나중에 "아무 키나 누르면 시작" 같은 연출을 넣고 싶다면 여기서 바로 호출하지 않으면 된다.
        BeginLoad();
    }

    public void SetTargetSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Target scene name is empty.");
            return;
        }

        // 외부 스크립트가 로딩 씬 안에서 목표 씬을 직접 지정해야 할 때 사용하는 진입점이다.
        // 일반적인 타이틀 버튼 흐름에서는 MoveScene.PendingTargetSceneName을 쓰므로 직접 호출하지 않아도 된다.
        targetSceneName = sceneName;
    }

    public void BeginLoad()
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            // 목표 씬 이름이 없으면 LoadSceneAsync가 실패하므로, 코루틴 시작 전에 명확히 중단한다.
            Debug.LogError("Target scene name is not assigned.");
            return;
        }

        // 실제 씬 로딩은 프레임을 넘겨야 하므로 코루틴으로 처리한다.
        Debug.Log("Loading scene started. Target scene: " + targetSceneName);
        StartCoroutine(LoadTargetSceneRoutine());
    }

    private IEnumerator LoadTargetSceneRoutine()
    {
        isLoading = true;

        // LoadSceneAsync는 씬 파일을 백그라운드로 읽는다.
        // 로딩바/페이드/팁 문구 같은 연출은 이 코루틴 안에서 함께 갱신하면 된다.
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(targetSceneName);
        if (asyncOperation == null)
        {
            // 씬 이름 오타 또는 Build Settings 미등록 같은 경우 여기로 올 수 있다.
            Debug.LogError("Failed to start scene loading: " + targetSceneName);
            isLoading = false;
            yield break;
        }

        // 로딩 완료 직후 바로 전환되지 않게 막아, 나중에 로딩 UI나 페이드 연출을 붙일 수 있게 한다.
        asyncOperation.allowSceneActivation = false;

        float elapsedTime = 0f;

        // allowSceneActivation이 false면 Unity는 progress를 0.9f에서 멈춘다.
        // 최소 로딩 시간을 같이 보장해 화면이 너무 빠르게 깜빡이지 않게 한다.
        while (asyncOperation.progress < 0.9f || elapsedTime < minimumLoadingTime)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);

            // TODO: 로딩바 UI를 만들면 여기서 progress 값을 Slider나 Image.fillAmount에 넣는다.
            // 예시: loadingSlider.value = progress;
            // 예시: loadingFillImage.fillAmount = progress;

            yield return null;
        }

        // 현재는 별도 입력 대기나 페이드가 없으므로 로딩 완료 즉시 목표 씬을 활성화한다.
        // 페이드 아웃을 넣는다면 이 줄 바로 전에 페이드 코루틴을 yield return 하면 된다.
        asyncOperation.allowSceneActivation = true;
    }
}
