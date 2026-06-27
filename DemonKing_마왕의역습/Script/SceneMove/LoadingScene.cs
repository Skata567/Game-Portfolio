using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


// 씬을 이동할때 로딩 씬을 통해서 가게 해주는 스크립트
// 씬을 부를때 LoadingSceneManager.LoadScene("씬 이름");
// GameSceneManager에 Scene이동 관련 함수 넣어두면 됨
public class LoadingSceneManager : MonoBehaviour
{
    public static string nextScene;

    [SerializeField]
    Text loadingText;

    [Header("로딩 텍스트 애니메이션 설정")]
    [SerializeField] private float dotAnimationSpeed = 0.5f;  // 점 애니메이션 속도
    [SerializeField] private string baseLoadingText = "로딩중";  // 기본 텍스트
    [SerializeField] private float minimumLoadingTime = 3.0f;  // 최소 로딩 시간 (초)

    [Header("로딩 팁 메시지 설정")]
    [SerializeField] private Text tipText;  // 팁 메시지를 표시할 텍스트
    [SerializeField] private float tipChangeInterval = 1.5f;  // 팁 변경 간격 (초)
    [SerializeField] private bool showTips = true;  // 팁 표시 여부

    [Header("페이드 인 아웃 시간")]
    [SerializeField] private float fadInTime = 2f;

    private int currentDotCount = 0;  // 현재 점의 개수
    private float loadingStartTime;   // 로딩 시작 시간
    private static AudioClip targetSecaneBGM;

    // 게임 팁 메시지 배열
    private string[] gameTips = {
        "다이아몬드로 종족별 스킬을 강화하여 유닛들을 강화 할 수 있습니다.",
        "특수 유닛은 강력하지만 한 종류당 하나만 소유할 수 있습니다.",
        "교환소에서는 미스릴로 특수 유닛을 구매할 수 있습니다.",
        "유닛마다 스킬이 다르니 다양한 스킬로 다양한 전략을 구사할 수 있습니다.",
        "도감에서 몬스터의 종족과 스킬 정보를 확인할 수 있습니다.",
        "일반 몬스터는 판매시 구매 재화의 절반을 돌려 받을 수 있습니다.",
        "이벤트 노드에는 좋은 일도, 나쁜 일도 일어날 수 있습니다.",
        "엘리트 스테이지를 클리어하면 미스릴을 획득할 수 있습니다.",
        "비밀 교환소는 한 번 닫으면 다시 열 수 없습니다.",
        "한 군단에 여러 종족이 섞일 수 없습니다.",
        "스페셜 유닛은 한명의 유닛이 한 군단 전체를 사용 합니다.",
        "다양한 룬을 군단에 장착하여 여러 효과를 받을 수 있습니다.",
        "도감에는 게임 플레이중 도움이 될 만한 정보들이 있습니다."
    };

    private void Start()
    {
        StartCoroutine(LoadScene());
        StartCoroutine(AnimateLoadingDots());

        // 팁 메시지 기능이 활성화되어 있으면 시작
        if (showTips && tipText != null)
        {
            StartCoroutine(AnimateLoadingTips());
        }

        // 로딩 시작 시 사운드는 그대로 재생 (FadeOut은 로딩 진행률에 맞춰 진행)
    }

    public static void LoadScene(string sceneName, AudioClip Bgm = null)
    {
        nextScene = sceneName;                                        // 목표 씬의 이름 설정
        targetSecaneBGM = Bgm;
        SceneManager.LoadScene("LoadingScene");                       // 로딩 씬으로 우선 이동
    }

    IEnumerator LoadScene()
    {
        loadingStartTime = Time.time;
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            yield return null;
        }

        float elapsedTime = Time.time - loadingStartTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            yield return null;
        }

        // 타겟 BGM이 있으면 크로스페이드로 전환
        if(targetSecaneBGM != null)
        {
            SoundManager.Instance.CrossFadeBGM(targetSecaneBGM, fadInTime);
        }
    }

    /// <summary>
    /// 로딩 텍스트의 점을 애니메이션하는 코루틴
    /// "로딩중" -> "로딩중." -> "로딩중.." -> "로딩중..." -> "로딩중" 순으로 반복
    /// </summary>
    private IEnumerator AnimateLoadingDots()
    {
        while (true)
        {
            yield return new WaitForSeconds(dotAnimationSpeed);

            // 점의 개수를 0, 1, 2, 3 순으로 순환
            currentDotCount = (currentDotCount + 1) % 4;

            // 점 생성
            string dots = "";
            for (int i = 0; i < currentDotCount; i++)
            {
                dots += ".";
            }

            // 텍스트 업데이트
            if (loadingText != null)
            {
                loadingText.text = baseLoadingText + dots;
            }
        }
    }

    /// <summary>
    /// 로딩 팁 메시지를 랜덤으로 표시하는 코루틴
    /// 설정된 간격마다 새로운 팁 메시지를 표시
    /// </summary>
    private IEnumerator AnimateLoadingTips()
    {
        if (gameTips == null || gameTips.Length == 0)
        {
            Debug.LogWarning("[LoadingSceneManager] 팁 메시지가 설정되지 않았습니다!");
            yield break;
        }

        // 첫 번째 팁을 즉시 표시
        ShowRandomTip();

        while (true)
        {
            yield return new WaitForSeconds(tipChangeInterval);
            ShowRandomTip();
        }
    }

    /// <summary>
    /// 랜덤한 팁 메시지를 선택해서 표시하는 메서드
    /// </summary>
    private void ShowRandomTip()
    {
        if (tipText == null || gameTips == null || gameTips.Length == 0)
            return;

        // 랜덤 인덱스 선택
        int randomIndex = Random.Range(0, gameTips.Length);

        // 팁 텍스트 업데이트
        tipText.text = gameTips[randomIndex];

       /* Debug.Log($"[LoadingTip] {gameTips[randomIndex]}");*/
    }
}