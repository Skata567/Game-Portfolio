using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// SFX 전용 재생 컨트롤러 (Singleton)
/// - AudioSource Pool을 사용하여 여러 SFX 동시 재생 지원
/// - AudioLibrary와 연동하여 이름으로 사운드 재생 가능
/// - AudioMixer를 통한 볼륨 조절 자동 적용
/// </summary>
public class SFXController : MonoBehaviour
{
    #region Singleton

    public static SFXController Instance { get; private set; }

    #endregion

    #region AudioSource Pool 설정

    [Header("AudioSource Pool 설정")]
    [SerializeField] private int poolSize = 5;  // Pool에 생성할 AudioSource 개수
    [Tooltip("Pool 크기가 부족할 때 자동으로 확장할지 여부")]
    [SerializeField] private bool autoExpand = true;

    private List<AudioSource> audioSourcePool;  // AudioSource Pool
    private int currentPoolIndex = 0;           // 다음 사용할 AudioSource 인덱스

    #endregion

    #region AudioMixerGroup 및 AudioLibrary

    [Header("AudioMixer 그룹 설정")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("AudioLibrary 참조")]
    [SerializeField] private AudioLibrary audioLibrary;

    #endregion

    #region 초기화

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// AudioSource Pool 생성 및 초기화
    /// </summary>
    private void InitializePool()
    {
        audioSourcePool = new List<AudioSource>();

        for (int i = 0; i < poolSize; i++)
        {
            CreateNewAudioSource();
        }

        Debug.Log($"[SFXController] AudioSource Pool 초기화 완료 (크기: {poolSize})");
    }

    /// <summary>
    /// 새로운 AudioSource를 생성하여 Pool에 추가
    /// </summary>
    private AudioSource CreateNewAudioSource()
    {
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.loop = false;  // SFX는 기본적으로 루프하지 않음

        // AudioMixerGroup 연결
        if (sfxMixerGroup != null)
        {
            newSource.outputAudioMixerGroup = sfxMixerGroup;
        }
        else
        {
            Debug.LogWarning("[SFXController] SFX MixerGroup이 할당되지 않았습니다. Inspector에서 설정해주세요.");
        }

        audioSourcePool.Add(newSource);
        return newSource;
    }

    #endregion

    #region SFX 재생

    /// <summary>
    /// AudioClip을 직접 전달하여 SFX 재생
    /// </summary>
    /// <param name="clip">재생할 AudioClip</param>
    /// <param name="volume">재생 볼륨 (0.0 ~ 1.0, 기본: 1.0)</param>
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SFXController] 재생할 AudioClip이 null입니다.");
            return;
        }

        AudioSource source = GetAvailableAudioSource();
        if (source != null)
        {
            source.volume = Mathf.Clamp01(volume);  // 0~1 범위로 제한
            source.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// AudioLibrary에서 이름으로 사운드를 찾아 재생
    /// </summary>
    /// <param name="soundName">AudioLibrary에 등록된 사운드 이름</param>
    /// <param name="volume">재생 볼륨 (0.0 ~ 1.0, 기본: 1.0)</param>
    public void PlaySFX(string soundName, float volume = 1.0f)
    {
        if (audioLibrary == null)
        {
            Debug.LogWarning("[SFXController] AudioLibrary가 할당되지 않았습니다. Inspector에서 설정해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(soundName))
        {
            Debug.LogWarning("[SFXController] 사운드 이름이 비어있습니다.");
            return;
        }

        AudioClip clip = audioLibrary.GetClipByName(soundName);
        if (clip != null)
        {
            PlaySFX(clip, volume);
        }
        // AudioLibrary에서 이미 경고 메시지 출력됨
    }

    /// <summary>
    /// 사용 가능한 AudioSource를 Pool에서 가져오기
    /// </summary>
    /// <returns>사용 가능한 AudioSource</returns>
    private AudioSource GetAvailableAudioSource()
    {
        // 1차 시도: 재생 중이지 않은 AudioSource 찾기
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            currentPoolIndex = (currentPoolIndex + 1) % audioSourcePool.Count;
            AudioSource source = audioSourcePool[currentPoolIndex];

            if (!source.isPlaying)
            {
                return source;
            }
        }

        // 2차 시도: 모든 AudioSource가 재생 중일 때
        if (autoExpand)
        {
            // Pool 확장
            Debug.LogWarning($"[SFXController] Pool이 가득 찼습니다. 자동으로 AudioSource 추가 (현재: {audioSourcePool.Count})");
            return CreateNewAudioSource();
        }
        else
        {
            // 가장 오래된 AudioSource 덮어쓰기 (Round-Robin)
            Debug.LogWarning($"[SFXController] Pool이 가득 찼습니다. 가장 오래된 사운드를 중단하고 재사용합니다.");
            currentPoolIndex = (currentPoolIndex + 1) % audioSourcePool.Count;
            return audioSourcePool[currentPoolIndex];
        }
    }

    #endregion

    #region 모든 SFX 제어

    /// <summary>
    /// 현재 재생 중인 모든 SFX 정지
    /// </summary>
    public void StopAllSFX()
    {
        int stoppedCount = 0;
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                source.Stop();
                stoppedCount++;
            }
        }

        Debug.Log($"[SFXController] {stoppedCount}개의 SFX 정지");
    }

    /// <summary>
    /// 현재 재생 중인 모든 SFX 일시정지
    /// </summary>
    public void PauseAllSFX()
    {
        int pausedCount = 0;
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                source.Pause();
                pausedCount++;
            }
        }

        Debug.Log($"[SFXController] {pausedCount}개의 SFX 일시정지");
    }

    /// <summary>
    /// 일시정지된 모든 SFX 재개
    /// </summary>
    public void ResumeAllSFX()
    {
        int resumedCount = 0;
        foreach (AudioSource source in audioSourcePool)
        {
            // isPlaying이 false이고 clip이 있으면 일시정지 상태로 간주
            if (!source.isPlaying && source.clip != null)
            {
                source.UnPause();
                resumedCount++;
            }
        }

        Debug.Log($"[SFXController] {resumedCount}개의 SFX 재개");
    }

    #endregion

    #region 상태 확인

    /// <summary>
    /// 현재 재생 중인 SFX 개수 반환
    /// </summary>
    public int GetPlayingSFXCount()
    {
        int count = 0;
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Pool의 현재 크기 반환
    /// </summary>
    public int GetPoolSize()
    {
        return audioSourcePool.Count;
    }

    #endregion

    #region 디버그 정보

    /// <summary>
    /// Pool 상태를 콘솔에 출력 (디버깅용)
    /// </summary>
    public void PrintPoolStatus()
    {
        int playingCount = GetPlayingSFXCount();
        int totalCount = audioSourcePool.Count;

        Debug.Log($"[SFXController Pool Status] 재생 중: {playingCount} / 전체: {totalCount}");

        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            AudioSource source = audioSourcePool[i];
            string status = source.isPlaying ? $"재생 중 ({source.clip?.name})" : "대기";
            Debug.Log($"  [{i}] {status}");
        }
    }

    #endregion
}
