using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 사운드 설정 관리자 (PersistentSingleton)
/// - UI와 분리되어 데이터와 핵심 로직만 처리
/// - 볼륨/음소거 상태 저장 및 불러오기 (PlayerPrefs)
/// - 씬 전환 및 게임 재시작 시에도 설정 유지
/// </summary>
public class SoundManager : PersistentSingleton<SoundManager>
{
    #region 상수 정의

    // PlayerPrefs 키
    private const string KEY_MASTER_VOLUME = "Master";
    private const string KEY_BGM_VOLUME = "BGM";
    private const string KEY_SFX_VOLUME = "SFX";
    private const string KEY_IS_MUTED = "IsMuted";

    // 기본값 (dB)
    private const float DEFAULT_MASTER_VOLUME = 0f;
    private const float DEFAULT_BGM_VOLUME = 0f;
    private const float DEFAULT_SFX_VOLUME = 0f;
    private const bool DEFAULT_IS_MUTED = false;

    // 최소 볼륨 임계값 (이 값 이하면 완전 음소거)
    private const float MUTE_THRESHOLD = -20f;
    private const float MUTE_VOLUME = -80f;

    #endregion

    [Header("오디오 믹서")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("컨트롤러 참조")]
    [SerializeField] private BGMController bgmController;
    [SerializeField] private SFXController sfxController;

    [Header("AudioMixerGroup 공개 (컨트롤러 연결용)")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    public AudioMixerGroup BGMMixerGroup => bgmMixerGroup;
    public AudioMixerGroup SFXMixerGroup => sfxMixerGroup;

    #region 공개 프로퍼티 (SoundView에서 사용)

    public float MasterVolume { get; private set; }
    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public bool IsMuted { get; private set; }

    #endregion

    private float previousMasterVolume; // 음소거 이전 볼륨 저장용

    #region 초기화

    protected override void Awake()
    {
        base.Awake();
        InitializeControllers();
        LoadSettings(); // 게임 시작 시 설정 불러오기
    }

    /// <summary>
    /// BGMController와 SFXController 초기화
    /// </summary>
    private void InitializeControllers()
    {
        // 같은 GameObject에 컨트롤러가 없으면 자동 생성
        if (bgmController == null)
        {
            bgmController = GetComponent<BGMController>();
            if (bgmController == null)
            {
                bgmController = gameObject.AddComponent<BGMController>();
            }
        }

        if (sfxController == null)
        {
            sfxController = GetComponent<SFXController>();
            if (sfxController == null)
            {
                sfxController = gameObject.AddComponent<SFXController>();
            }
        }
    }

    #endregion

    #region 볼륨 조절 메서드 (SoundView에서 호출)

    /// <summary>
    /// 전체 볼륨 조절
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        MasterVolume = volume;
        ApplyVolume("Master", volume);
        SaveSettings();
    }

    /// <summary>
    /// 배경음악 볼륨 조절
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        BGMVolume = volume;
        ApplyVolume("BGM", volume);
        SaveSettings();
    }

    /// <summary>
    /// 효과음 볼륨 조절
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SFXVolume = volume;
        ApplyVolume("SFX", volume);
        SaveSettings();
    }

    /// <summary>
    /// AudioMixer에 실제 볼륨 적용
    /// </summary>
    private void ApplyVolume(string parameter, float volume)
    {
        if (audioMixer == null) return;

        // 음소거 상태가 아닐 때만 적용
        if (!IsMuted)
        {
            float mixerVolume = (volume <= MUTE_THRESHOLD) ? MUTE_VOLUME : volume;
            audioMixer.SetFloat(parameter, mixerVolume);
        }
    }

    #endregion

    #region 음소거 (SoundView에서 호출)

    /// <summary>
    /// 음소거 활성화
    /// </summary>
    public void MuteAudio()
    {
        if (IsMuted) return;

        IsMuted = true;
        previousMasterVolume = MasterVolume; // 현재 MasterVolume 프로퍼티 백업 (슬라이더 값)
        audioMixer.SetFloat("Master", MUTE_VOLUME);
        AudioListener.volume = 0f;
        SaveSettings();
        Debug.Log("[SoundManager] 음소거 활성화");
    }

    /// <summary>
    /// 음소거 해제
    /// </summary>
    public void UnmuteAudio()
    {
        if (!IsMuted) return;

        IsMuted = false;
        // 음소거 중에 슬라이더가 변경되었을 수 있으므로, 현재 MasterVolume 값 사용
        float mixerVolume = (MasterVolume <= MUTE_THRESHOLD) ? MUTE_VOLUME : MasterVolume;
        audioMixer.SetFloat("Master", mixerVolume);
        AudioListener.volume = 1f;
        SaveSettings();
        Debug.Log($"[SoundManager] 음소거 해제 - 복원 볼륨: {MasterVolume}dB");
    }

    #endregion

    #region SFX 재생 (컨트롤러로 위임)

    /// <summary>
    /// 지정된 오디오 클립을 SFX로 재생 (SFXController로 위임)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (SFXController.Instance != null)
        {
            SFXController.Instance.PlaySFX(clip, volume);
        }
        else
        {
            Debug.LogWarning("[SoundManager] SFXController.Instance를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 라이브러리에서 이름으로 사운드를 찾아 재생 (SFXController로 위임)
    /// </summary>
    public void PlaySFX(string name, float volume = 1.0f)
    {
        if (SFXController.Instance != null)
        {
            SFXController.Instance.PlaySFX(name, volume);
        }
        else
        {
            Debug.LogWarning("[SoundManager] SFXController.Instance를 찾을 수 없습니다.");
        }
    }

    #endregion

    #region 저장 및 불러오기

    /// <summary>
    /// 현재 사운드 설정을 PlayerPrefs에 저장
    /// </summary>
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, MasterVolume);
        PlayerPrefs.SetFloat(KEY_BGM_VOLUME, BGMVolume);
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, SFXVolume);
        PlayerPrefs.SetInt(KEY_IS_MUTED, IsMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// PlayerPrefs에서 저장된 사운드 설정 불러오기
    /// </summary>
    private void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, DEFAULT_MASTER_VOLUME);
        BGMVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, DEFAULT_BGM_VOLUME);
        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, DEFAULT_SFX_VOLUME);
        IsMuted = PlayerPrefs.GetInt(KEY_IS_MUTED, DEFAULT_IS_MUTED ? 1 : 0) == 1;

        // AudioMixer에 볼륨 적용
        ApplyVolume("Master", MasterVolume);
        ApplyVolume("BGM", BGMVolume);
        ApplyVolume("SFX", SFXVolume);

        // 음소거 상태 적용
        if (IsMuted)
        {
            previousMasterVolume = MasterVolume;
            audioMixer.SetFloat("Master", MUTE_VOLUME);
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = 1f;
        }

        Debug.Log($"[SoundManager] 설정 불러오기 완료 - Master: {MasterVolume}dB, BGM: {BGMVolume}dB, SFX: {SFXVolume}dB, Muted: {IsMuted}");
    }

    #endregion

    #region 초기화 (SoundView에서 호출)

    /// <summary>
    /// 사운드 설정을 기본값으로 초기화
    /// </summary>
    public void ResetToDefault()
    {
        SetMasterVolume(DEFAULT_MASTER_VOLUME);
        SetBGMVolume(DEFAULT_BGM_VOLUME);
        SetSFXVolume(DEFAULT_SFX_VOLUME);

        if (IsMuted)
        {
            UnmuteAudio();
        }
        
        SaveSettings();
        Debug.Log("[SoundManager] 설정이 기본값으로 초기화되었습니다.");
    }

    #endregion

    #region BGM 재생 (컨트롤러로 위임)

    /// <summary>
    /// BGM 재생 (BGMController로 위임)
    /// </summary>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (BGMController.Instance != null)
        {
            BGMController.Instance.PlayBGM(clip, loop);
        }
        else
        {
            Debug.LogWarning("[SoundManager] BGMController.Instance를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// BGM 크로스페이드 (BGMController로 위임)
    /// </summary>
    public void CrossFadeBGM(AudioClip clip, float duration = 1.5f)
    {
        if (BGMController.Instance != null)
        {
            BGMController.Instance.CrossFade(clip, duration);
        }
        else
        {
            Debug.LogWarning("[SoundManager] BGMController.Instance를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 현재 재생 중인 BGM 가져오기 (BGMController로 위임)
    /// </summary>
    public AudioClip GetCurrentBGM()
    {
        return BGMController.Instance?.GetCurrentBGM();
    }

    #endregion
}
