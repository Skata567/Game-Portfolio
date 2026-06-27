using UnityEngine;

/// <summary>
/// 씬별 BGM 자동 설정 스크립트
/// - SceneBGMPlayer, SceneBGMConnector, BattleBgmPlayer를 대체하는 통합 솔루션
/// - 씬 시작 시 지정된 BGM을 자동으로 재생
/// - 크로스페이드 옵션 지원으로 부드러운 전환
///
/// 사용 방법:
/// 1. 씬에 빈 GameObject 생성 (예: "SoundSetup")
/// 2. 이 스크립트를 추가
/// 3. Inspector에서 BGM 클립 할당
/// 4. 크로스페이드 옵션 설정
/// </summary>
public class SceneSoundSetup : MonoBehaviour
{
    #region Inspector 설정

    [Header("BGM 설정")]
    [Tooltip("이 씬에서 재생할 BGM 클립")]
    [SerializeField] private AudioClip bgmClip;

    [Header("재생 옵션")]
    [Tooltip("크로스페이드를 사용하여 부드럽게 전환할지 여부")]
    [SerializeField] private bool useCrossFade = true;

    [Tooltip("크로스페이드 지속 시간 (초)")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Tooltip("씬 시작 시 자동으로 BGM 재생할지 여부")]
    [SerializeField] private bool playOnStart = true;

    [Header("고급 옵션")]
    [Tooltip("이미 같은 BGM이 재생 중이면 건너뛸지 여부")]
    [SerializeField] private bool skipIfSameBGM = true;

    #endregion

    #region 초기화

    void Start()
    {
        if (playOnStart)
        {
            PlaySceneBGM();
        }
    }

    #endregion

    #region BGM 재생

    /// <summary>
    /// 설정된 BGM을 재생
    /// </summary>
    public void PlaySceneBGM()
    {
        // BGMController 인스턴스 확인
        if (BGMController.Instance == null)
        {
            Debug.LogError("[SceneSoundSetup] BGMController.Instance를 찾을 수 없습니다! " +
                           "씬에 BGMController가 있는지 확인하세요.");
            return;
        }

        // BGM 클립이 할당되지 않았을 때
        if (bgmClip == null)
        {
            Debug.LogWarning($"[SceneSoundSetup] BGM 클립이 할당되지 않았습니다. (GameObject: {gameObject.name})");
            return;
        }

        // 이미 같은 BGM이 재생 중인지 확인
        if (skipIfSameBGM)
        {
            AudioClip currentBGM = BGMController.Instance.GetCurrentBGM();
            if (currentBGM == bgmClip)
            {
                Debug.Log($"[SceneSoundSetup] '{bgmClip.name}'은(는) 이미 재생 중입니다. 재생을 건너뜁니다.");
                return;
            }
        }

        // 크로스페이드 또는 즉시 재생
        if (useCrossFade)
        {
            BGMController.Instance.CrossFade(bgmClip, fadeDuration);
            Debug.Log($"[SceneSoundSetup] '{bgmClip.name}' 크로스페이드 재생 ({fadeDuration}초)");
        }
        else
        {
            BGMController.Instance.PlayBGM(bgmClip);
            Debug.Log($"[SceneSoundSetup] '{bgmClip.name}' 즉시 재생");
        }
    }

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM()
    {
        if (BGMController.Instance != null)
        {
            BGMController.Instance.Stop();
            Debug.Log("[SceneSoundSetup] BGM 정지");
        }
    }

    #endregion

    #region 런타임에서 BGM 변경

    /// <summary>
    /// 런타임에서 BGM을 변경하고 재생
    /// (스크립트나 이벤트에서 호출 가능)
    /// </summary>
    /// <param name="newClip">새로 재생할 BGM 클립</param>
    /// <param name="crossFade">크로스페이드 사용 여부</param>
    public void ChangeBGM(AudioClip newClip, bool crossFade = true)
    {
        if (newClip == null)
        {
            Debug.LogWarning("[SceneSoundSetup] ChangeBGM: 새 BGM 클립이 null입니다.");
            return;
        }

        bgmClip = newClip;
        useCrossFade = crossFade;
        PlaySceneBGM();
    }

    #endregion

    #region 에디터 디버깅

#if UNITY_EDITOR
    /// <summary>
    /// Inspector에서 'Play BGM' 버튼으로 테스트 가능
    /// </summary>
    [ContextMenu("Play BGM (Test)")]
    private void TestPlayBGM()
    {
        if (Application.isPlaying)
        {
            PlaySceneBGM();
        }
        else
        {
            Debug.LogWarning("[SceneSoundSetup] Play 모드에서만 테스트할 수 있습니다.");
        }
    }

    /// <summary>
    /// Inspector에서 'Stop BGM' 버튼으로 테스트 가능
    /// </summary>
    [ContextMenu("Stop BGM (Test)")]
    private void TestStopBGM()
    {
        if (Application.isPlaying)
        {
            StopBGM();
        }
        else
        {
            Debug.LogWarning("[SceneSoundSetup] Play 모드에서만 테스트할 수 있습니다.");
        }
    }

    /// <summary>
    /// Inspector에서 현재 설정 정보 출력
    /// </summary>
    [ContextMenu("Print Setup Info")]
    private void PrintSetupInfo()
    {
        Debug.Log($"[SceneSoundSetup] 설정 정보:\n" +
                  $"  - BGM: {(bgmClip != null ? bgmClip.name : "없음")}\n" +
                  $"  - 크로스페이드: {useCrossFade}\n" +
                  $"  - 페이드 시간: {fadeDuration}초\n" +
                  $"  - 자동 재생: {playOnStart}\n" +
                  $"  - 중복 건너뛰기: {skipIfSameBGM}");
    }
#endif

    #endregion
}
