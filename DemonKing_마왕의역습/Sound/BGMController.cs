using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM 전용 재생 컨트롤러 (Singleton)
/// - 크로스페이드를 지원하는 부드러운 BGM 전환
/// - 2개의 AudioSource를 번갈아 사용하여 끊김 없는 재생
/// - AudioMixer를 통한 볼륨 조절 적용
/// </summary>
public class BGMController : MonoBehaviour
{
    #region Singleton

    public static BGMController Instance { get; private set; }

    #endregion

    #region AudioSource 관리

    private AudioSource currentSource;  // 현재 재생 중인 AudioSource
    private AudioSource nextSource;     // 다음에 재생할 AudioSource (크로스페이드용)

    private AudioClip currentClip;      // 현재 재생 중인 클립 추적
    private Coroutine fadeCoroutine;    // 페이드 코루틴 참조 (중복 방지)

    #endregion

    #region AudioMixerGroup 설정

    [Header("AudioMixer 그룹 설정")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    #endregion

    #region 초기화

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 2개의 AudioSource를 생성하고 초기 설정
    /// </summary>
    private void InitializeAudioSources()
    {
        // AudioSource 2개 생성
        currentSource = gameObject.AddComponent<AudioSource>();
        nextSource = gameObject.AddComponent<AudioSource>();

        // 공통 설정
        SetupAudioSource(currentSource);
        SetupAudioSource(nextSource);

        Debug.Log("[BGMController] 초기화 완료 - 크로스페이드 준비됨");
    }

    /// <summary>
    /// AudioSource의 기본 설정 적용
    /// </summary>
    private void SetupAudioSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 1f;  // 기본 볼륨 (AudioMixer가 실제 볼륨 조절)

        // AudioMixerGroup 연결
        if (bgmMixerGroup != null)
        {
            source.outputAudioMixerGroup = bgmMixerGroup;
        }
        else
        {
            Debug.LogWarning("[BGMController] BGM MixerGroup이 할당되지 않았습니다. Inspector에서 설정해주세요.");
        }
    }

    #endregion

    #region BGM 재생

    /// <summary>
    /// BGM을 즉시 재생 (크로스페이드 없이 바로 전환)
    /// </summary>
    /// <param name="clip">재생할 BGM 클립</param>
    /// <param name="loop">루프 재생 여부 (기본: true)</param>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("[BGMController] 재생할 BGM 클립이 null입니다.");
            return;
        }

        // 이미 같은 클립이 재생 중이면 무시
        if (currentClip == clip && currentSource.isPlaying)
        {
            Debug.Log($"[BGMController] '{clip.name}'은(는) 이미 재생 중입니다.");
            return;
        }

        // 진행 중인 페이드 중단
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // 기존 재생 중단
        currentSource.Stop();
        nextSource.Stop();

        // 새 BGM 재생
        currentSource.clip = clip;
        currentSource.loop = loop;
        currentSource.volume = 1f;
        currentSource.Play();

        currentClip = clip;
        Debug.Log($"[BGMController] '{clip.name}' 재생 시작 (즉시 전환)");
    }

    /// <summary>
    /// BGM을 크로스페이드로 부드럽게 전환
    /// </summary>
    /// <param name="clip">재생할 BGM 클립</param>
    /// <param name="duration">페이드 지속 시간 (초)</param>
    public void CrossFade(AudioClip clip, float duration = 1.5f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[BGMController] 크로스페이드할 BGM 클립이 null입니다.");
            return;
        }

        // 이미 같은 클립이 재생 중이면 무시
        if (currentClip == clip && currentSource.isPlaying)
        {
            Debug.Log($"[BGMController] '{clip.name}'은(는) 이미 재생 중입니다. (크로스페이드 생략)");
            return;
        }

        // 아무것도 재생 중이 아니면 즉시 재생
        if (!currentSource.isPlaying)
        {
            PlayBGM(clip);
            return;
        }

        // 진행 중인 페이드가 있으면 중단
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 크로스페이드 시작
        fadeCoroutine = StartCoroutine(CrossFadeCoroutine(clip, duration));
    }

    /// <summary>
    /// 크로스페이드 코루틴 - 두 AudioSource의 볼륨을 동시에 조절
    /// </summary>
    private IEnumerator CrossFadeCoroutine(AudioClip newClip, float duration)
    {
        Debug.Log($"[BGMController] 크로스페이드 시작: '{currentClip?.name}' → '{newClip.name}' ({duration}초)");

        // nextSource에 새 클립 설정
        nextSource.clip = newClip;
        nextSource.volume = 0f;
        nextSource.Play();

        float elapsed = 0f;

        // duration 동안 볼륨 교차
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 볼륨 커브 적용 (부드러운 전환)
            currentSource.volume = Mathf.Lerp(1f, 0f, t);
            nextSource.volume = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // 최종 볼륨 설정
        currentSource.volume = 0f;
        nextSource.volume = 1f;

        // 이전 소스 정지 및 AudioSource 역할 스왑
        currentSource.Stop();
        SwapAudioSources();

        currentClip = newClip;
        fadeCoroutine = null;

        Debug.Log($"[BGMController] 크로스페이드 완료: '{newClip.name}' 재생 중");
    }

    /// <summary>
    /// currentSource와 nextSource의 역할을 바꿈
    /// </summary>
    private void SwapAudioSources()
    {
        AudioSource temp = currentSource;
        currentSource = nextSource;
        nextSource = temp;
    }

    #endregion

    #region 재생 제어

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void Stop()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        currentSource.Stop();
        nextSource.Stop();
        currentClip = null;

        Debug.Log("[BGMController] BGM 정지");
    }

    /// <summary>
    /// BGM 일시정지
    /// </summary>
    public void Pause()
    {
        if (currentSource.isPlaying)
        {
            currentSource.Pause();
            Debug.Log("[BGMController] BGM 일시정지");
        }
    }

    /// <summary>
    /// 일시정지된 BGM 재개
    /// </summary>
    public void Resume()
    {
        if (!currentSource.isPlaying && currentSource.clip != null)
        {
            currentSource.UnPause();
            Debug.Log("[BGMController] BGM 재개");
        }
    }

    #endregion

    #region 상태 확인

    /// <summary>
    /// 현재 재생 중인 BGM 클립 반환
    /// </summary>
    public AudioClip GetCurrentBGM()
    {
        return currentClip;
    }

    /// <summary>
    /// BGM이 재생 중인지 확인
    /// </summary>
    public bool IsPlaying()
    {
        return currentSource.isPlaying;
    }

    #endregion
}
