using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MoveScene : MonoBehaviour
{
    // 로딩 씬으로 넘어가면 타이틀 씬 오브젝트가 파괴되므로 static 값으로 목표 씬 이름을 보관한다.
    // LoadingSceneController가 로딩 씬에서 이 값을 읽어 실제 게임 씬을 비동기로 불러온다.
    public static string PendingTargetSceneName { get; private set; }

    // 외부(엔딩 등)에서 로딩 씬 경유 목표 씬을 지정할 때 사용
    public static void SetPendingTarget(string sceneName)
    {
        PendingTargetSceneName = sceneName;
    }

    [Header("Scene Flow")]
    [Tooltip("버튼을 누른 뒤 먼저 이동할 로딩 씬 이름입니다. Project Settings > Build Profiles/Build Settings에 등록된 씬 이름과 같아야 합니다.")]
    [SerializeField] private string loadingSceneName = "Loading";

    [Tooltip("게임 시작 버튼을 눌렀을 때 로딩 씬 이후 최종적으로 이동할 새 게임 씬 이름입니다.")]
    [SerializeField] private string newGameSceneName = "NYH_Grid";

    [Tooltip("이어하기 버튼을 눌렀을 때 로딩 씬 이후 최종적으로 이동할 씬 이름입니다. 저장 시스템이 생기면 저장 데이터 기준으로 바꿀 수 있습니다.")]
    [SerializeField] private string continueSceneName = "NYH_Grid";

    [Tooltip("챌린지 버튼을 눌렀을 때 로딩 씬 이후 최종적으로 이동할 씬 이름입니다. 챌린지 전용 씬이 생기면 이 값을 교체하세요.")]
    [SerializeField] private string challengeSceneName = "NYH_Grid";
    [Tooltip("메인 메뉴 버튼을 눌렀을 때 로딩 씬 이후 최종적으로 이동할 씬 이름입니다.")]
    [SerializeField] private string mainMenuSceneName = "MainMeun";
    [Tooltip("튜토리얼 버튼을 눌렀을 때 로딩 씬 이후 최종적으로 이동할 씬 이름입니다.")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    

    [Header("Continue Button State")]
    [Tooltip("이어하기 버튼입니다. 저장 데이터가 없으면 interactable=false가 되어 회색 비활성 상태가 됩니다.")]
    [SerializeField] private Button continueButton;

    [Tooltip("임시 저장 데이터 확인용 PlayerPrefs 키입니다. 실제 저장 시스템이 완성되면 HasContinueSave() 내부 구현만 교체하세요.")]
    [SerializeField] private string continueSaveKey = "HasContinueSave";

    private void Awake()
    {
        // 타이틀 씬이 켜질 때마다 이어하기 버튼 상태를 저장 데이터 여부에 맞춰 갱신한다.
        RefreshContinueButton();
    }

    public void StartNewGame()
    {
        // 새 게임은 기존 저장을 폐기해 새 시드 1층부터 시작하게 한다.
        SaveManager.Delete();

        // 새 게임은 로딩 씬을 거쳐 게임 메인 씬으로 이동한다.
        Debug.Log("StartNewGame button clicked.");
        LoadThroughLoadingScene(newGameSceneName);
    }

    public void ContinueGame()
    {
        if (!HasContinueSave())
        {
            Debug.LogWarning("이어하기 저장 데이터가 없어 이동하지 않습니다.");
            RefreshContinueButton();
            return;
        }

        // 현재는 새 게임과 같은 씬으로 이동하지만, 나중에 저장 데이터를 읽어 위치/상태를 복원하면 된다.
        LoadThroughLoadingScene(continueSceneName);
    }

    public void StartChallenge()
    {
        // 챌린지 전용 씬 또는 챌린지 모드 초기화 씬이 생기면 challengeSceneName만 바꾸면 된다.
        LoadThroughLoadingScene(challengeSceneName);
    }

    public void StartMainMenu()
    {
        // 타이틀 씬으로 이동
        LoadThroughLoadingScene(mainMenuSceneName);
    }
    // 튜토리얼 씬으로 이동
    public void StartTutorial()
    {
        LoadThroughLoadingScene(tutorialSceneName);
    }

    public void RefreshContinueButton()
    {
        if (continueButton == null)
        {
            return;
        }

        // 저장 시스템이 완성되기 전까지는 PlayerPrefs 키 하나로 버튼 활성 상태만 판단한다.
        // Button.interactable을 끄면 Unity UI가 자동으로 비활성 색상을 적용한다.
        continueButton.interactable = HasContinueSave();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // 에디터에서는 Application.Quit이 동작하지 않아 플레이 모드만 종료한다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadThroughLoadingScene(string targetSceneName)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            // 잘못된 씬 이름으로 이동하면 Unity가 런타임 에러를 내므로 버튼 입력 단계에서 먼저 막는다.
            Debug.LogError("목표 씬 이름이 비어 있습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(loadingSceneName))
        {
            // 로딩 씬 이름이 비어 있으면 목표 씬까지 도달할 수 없으므로 즉시 중단한다.
            Debug.LogError("로딩 씬 이름이 비어 있습니다.");
            return;
        }

        // 로딩 씬으로 넘어가면 현재 오브젝트는 사라지므로 static 값으로 목표 씬 이름을 전달한다.
        PendingTargetSceneName = targetSceneName;
        Debug.Log("Move to loading scene. Loading scene: " + loadingSceneName + ", target scene: " + targetSceneName);
        SceneManager.LoadScene(loadingSceneName);
    }

    private bool HasContinueSave()
    {
        return SaveManager.HasSave();
    }
}
