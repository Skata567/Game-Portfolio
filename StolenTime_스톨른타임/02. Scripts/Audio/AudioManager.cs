using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 전체 오디오의 진입점입니다.
/// 볼륨 저장/복원, 구역/보스 BGM 전환, 공통 SFX 재생을 한 곳에서 관리합니다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const string MasterPrefsKey = "Audio.MasterVolume";
    private const string BgmPrefsKey = "Audio.BGMVolume";
    private const string SfxPrefsKey = "Audio.SFXVolume";

    private const string MasterMixerParameter = "MasterVolume";
    private const string BgmMixerParameter = "BGMVolume";
    private const string SfxMixerParameter = "SFXVolume";

    private const float DefaultVolume = 1f;
    private const float MinSliderVolume = 0.0001f;
    private const float MutedDecibel = -80f;

    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("MasterVolume/BGMVolume/SFXVolume 파라미터가 노출된 AudioMixer입니다.")]
    [SerializeField] private AudioMixer audioMixer;

    [Tooltip("BGM AudioSource들이 출력될 Mixer Group입니다. BGM 슬라이더가 배경음만 줄이려면 반드시 BGM 그룹을 연결해야 합니다.")]
    [SerializeField] private AudioMixerGroup bgmOutputGroup;

    [Tooltip("SFX AudioSource들이 출력될 Mixer Group입니다. SFX 슬라이더가 효과음만 줄이려면 반드시 SFX 그룹을 연결해야 합니다.")]
    [SerializeField] private AudioMixerGroup sfxOutputGroup;

    [Header("Volume Sliders")]
    [Tooltip("전체 볼륨을 조절하는 슬라이더입니다. UI의 MasterVolum 오브젝트를 연결합니다.")]
    [SerializeField] private Slider masterVolumeSlider;

    [Tooltip("배경음 볼륨을 조절하는 슬라이더입니다. UI의 BGMVolum 오브젝트를 연결합니다.")]
    [SerializeField] private Slider bgmVolumeSlider;

    [Tooltip("효과음 볼륨을 조절하는 슬라이더입니다. UI의 SFXVolum 오브젝트를 연결합니다.")]
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("BGM Playback")]
    [Tooltip("층 번호와 보스 여부에 따라 어떤 BGM을 재생할지 정하는 테이블입니다.")]
    [SerializeField] private MusicTable musicTable;

    [Tooltip("기존 인스펙터 연결을 유지하기 위한 첫 번째 BGM AudioSource입니다. 비워두면 자동으로 찾거나 추가합니다.")]
    [SerializeField] private AudioSource bgmSource;

    [Tooltip("페이드 전환 때 다음 곡을 미리 재생할 두 번째 BGM AudioSource입니다. 비워두면 자동으로 추가합니다.")]
    [SerializeField] private AudioSource secondaryBgmSource;

    [Tooltip("게임 시작 시 자동으로 재생할 기본 BGM입니다. 층 전환 시스템이 곧 BGM을 지정한다면 비워두거나 자동 재생을 끄면 됩니다.")]
    [SerializeField] private AudioClip startBgmClip;

    [Tooltip("게임 시작 때 startBgmClip을 자동 재생할지 정합니다.")]
    [SerializeField] private bool playBgmOnStart = true;

    [Tooltip("시작 BGM을 반복 재생할지 정합니다.")]
    [SerializeField] private bool loopStartBgm = true;

    [Tooltip("BGM이 바뀔 때 서로 교차 페이드되는 시간입니다.")]
    [SerializeField, Min(0f)] private float defaultBgmFadeSeconds = 0.75f;

    [Header("Scene BGM")]
    [Tooltip("이 씬들로 진입하면 MusicTable의 MainMenuBgm을 재생합니다.")]
    [SerializeField] private string[] mainMenuSceneNames = { "NYH_MainScene", "MainMeun" };

    [Tooltip("메인 메뉴 씬 로드 완료 시 메뉴 BGM으로 자동 전환할지 정합니다.")]
    [SerializeField] private bool playMainMenuBgmOnSceneLoaded = true;

    [Header("SFX Playback")]
    [Tooltip("동시에 재생 가능한 기본 SFX Source 개수입니다. 짧은 효과음이 겹쳐도 앞 소리가 끊기지 않도록 풀을 둡니다.")]
    [SerializeField, Min(1)] private int sfxPoolSize = 8;

    [Tooltip("SFX가 동시에 너무 많이 나면 Source를 임시로 늘릴지 정합니다. 꺼두면 가장 오래된 Source를 재사용합니다.")]
    [SerializeField] private bool expandSfxPoolWhenFull = true;

    [Tooltip("AudioEventId별 효과음 Cue 테이블입니다. 새 사운드는 이 테이블에 추가합니다.")]
    [SerializeField] private AudioCueTable audioCueTable;

    [Tooltip("레이저처럼 타이밍이 중요한 SFX가 다른 효과음에 밀리지 않도록 사용할 AudioSource 우선순위입니다. Unity는 숫자가 낮을수록 우선 재생합니다.")]
    [SerializeField, Range(0, 256)] private int criticalSfxPriority = 32;

    [Tooltip("중요 SFX 전용 Source 개수입니다. 연속 레이저처럼 짧은 간격으로 같은 소리가 반복될 때 앞 소리의 꼬리와 다음 소리를 분리합니다.")]
    [SerializeField, Min(1)] private int criticalSfxPoolSize = 3;

    [Header("Game Event SFX")]
    [Tooltip("플레이어가 피해를 입었을 때 재생할 효과음입니다.")]
    [SerializeField] private AudioCue playerDamagedCue;

    [Tooltip("적이 처치되었을 때 재생할 효과음입니다.")]
    [SerializeField] private AudioCue enemyKilledCue;

    [Tooltip("함정이 발동했을 때 재생할 효과음입니다.")]
    [SerializeField] private AudioCue trapTriggeredCue;

    [Tooltip("상자가 열렸을 때 재생할 효과음입니다.")]
    [SerializeField] private AudioCue chestOpenedCue;

    [Tooltip("열쇠를 획득했을 때 재생할 효과음입니다.")]
    [SerializeField] private AudioCue keyAcquiredCue;

    [Tooltip("상점에서 아이템 구매에 성공했을 때 재생할 효과음입니다.")]
    [SerializeField] private AudioCue itemPurchasedCue;

    [Tooltip("숨겨진 문/함정 같은 요소를 발견했을 때 재생할 효과음입니다.")]
    [SerializeField] private AudioCue secretDiscoveredCue;

    [Tooltip("보스가 처치되었을 때 재생할 효과음입니다.")]
    [SerializeField] private AudioCue bossDefeatedCue;

    private readonly List<AudioSource> _sfxSources = new List<AudioSource>();

    private readonly List<AudioSource> _criticalSfxSources = new List<AudioSource>();
    private int _nextCriticalSfxSourceIndex;
    private AudioSource _activeBgmSource;
    private AudioSource _inactiveBgmSource;
    private Coroutine _bgmFadeCoroutine;
    private bool _isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("AudioManager: 씬에 AudioManager가 둘 이상 있어 새 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 씬 전환 중에도 BGM과 볼륨 설정이 끊기지 않도록 최초 인스턴스를 유지한다.
        DontDestroyOnLoad(gameObject);

        ResolveBgmSources();
        ResolveSfxPool();

        ApplySavedMixerVolumes();
        InitializeSlider(masterVolumeSlider, MasterPrefsKey);
        InitializeSlider(bgmVolumeSlider, BgmPrefsKey);
        InitializeSlider(sfxVolumeSlider, SfxPrefsKey);

        _isInitialized = true;
    }

    private void Start()
    {
        if (playBgmOnStart && startBgmClip != null)
            PlayBgm(startBgmClip, loopStartBgm, 0f);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeSliders();
        SubscribeGameEvents();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeSliders();
        UnsubscribeGameEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetMasterVolume(float volume)
    {
        SetVolume(MasterPrefsKey, MasterMixerParameter, volume);
    }

    public void SetBgmVolume(float volume)
    {
        SetVolume(BgmPrefsKey, BgmMixerParameter, volume);
    }

    public void SetSfxVolume(float volume)
    {
        SetVolume(SfxPrefsKey, SfxMixerParameter, volume);
    }

    public void BindVolumeSliders(Slider masterSlider, Slider bgmSlider, Slider sfxSlider)
    {
        UnsubscribeSliders();

        masterVolumeSlider = masterSlider;
        bgmVolumeSlider = bgmSlider;
        sfxVolumeSlider = sfxSlider;

        ApplySavedMixerVolumes();
        InitializeSlider(masterVolumeSlider, MasterPrefsKey);
        InitializeSlider(bgmVolumeSlider, BgmPrefsKey);
        InitializeSlider(sfxVolumeSlider, SfxPrefsKey);

        SubscribeSliders();
    }

    public void UnbindVolumeSliders(Slider masterSlider, Slider bgmSlider, Slider sfxSlider)
    {
        if (masterVolumeSlider != masterSlider && bgmVolumeSlider != bgmSlider && sfxVolumeSlider != sfxSlider)
            return;

        UnsubscribeSliders();

        // 씬 UI는 씬 전환 때 파괴되므로 유지되는 AudioManager가 오래된 참조를 잡지 않게 비운다.
        masterVolumeSlider = null;
        bgmVolumeSlider = null;
        sfxVolumeSlider = null;
    }

    public void PlayBgm(AudioClip clip)
    {
        PlayBgm(clip, true, defaultBgmFadeSeconds);
    }

    public void PlayBgm(AudioClip clip, bool loop)
    {
        PlayBgm(clip, loop, defaultBgmFadeSeconds);
    }

    public void PlayBgm(AudioClip clip, bool loop = true, float fadeSeconds = -1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: 재생할 BGM AudioClip이 비어 있습니다.");
            return;
        }

        ResolveBgmSources();
        if (_activeBgmSource == null || _inactiveBgmSource == null)
            return;

        // 같은 곡을 다시 요청할 때 처음부터 재생하면 층 UI 갱신이나 설정창 조작 때 음악이 튈 수 있습니다.
        if (_activeBgmSource.clip == clip && _activeBgmSource.isPlaying)
        {
            _activeBgmSource.loop = loop;
            return;
        }

        float duration = fadeSeconds >= 0f ? fadeSeconds : defaultBgmFadeSeconds;
        if (_bgmFadeCoroutine != null)
            StopCoroutine(_bgmFadeCoroutine);

        if (duration <= 0f || !_activeBgmSource.isPlaying)
            PlayBgmImmediately(clip, loop);
        else
            _bgmFadeCoroutine = StartCoroutine(CrossFadeBgmCoroutine(clip, loop, duration));
    }

    public void PlayBgmForDepth(int depth, bool isBoss)
    {
        if (musicTable == null)
        {
            Debug.LogWarning("AudioManager: MusicTable이 연결되지 않아 층별 BGM을 재생할 수 없습니다.");
            return;
        }

        AudioClip clip = musicTable.GetBgmForDepth(depth, isBoss);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: {depth}층에 사용할 BGM이 MusicTable에 없습니다.");
            return;
        }

        PlayBgm(clip, true, defaultBgmFadeSeconds);
    }

    public void PlayBossPreludeBgmForDepth(int depth)
    {
        if (musicTable == null)
        {
            Debug.LogWarning("AudioManager: MusicTable이 없어 보스 전투 전 BGM을 재생할 수 없습니다.");
            return;
        }

        AudioClip clip = musicTable.GetBossPreludeBgm(depth);
        if (clip == null)
        {
            StopBgm();
            return;
        }

        PlayBgm(clip, true, defaultBgmFadeSeconds);
    }

    public void PlayMainMenuBgm() => PlayOptionalTableBgm(musicTable != null ? musicTable.MainMenuBgm : null);
    public void PlaySafeRoomBgm() => PlayOptionalTableBgm(musicTable != null ? musicTable.SafeRoomBgm : null);
    public void PlayShopOrRestBgm() => PlayOptionalTableBgm(musicTable != null ? musicTable.ShopOrRestBgm : null);
    public void PlayPlayerDeathBgm() => PlayOptionalTableBgm(musicTable != null ? musicTable.PlayerDeathBgm : null);
    public void PlayGameClearBgm() => PlayOptionalTableBgm(musicTable != null ? musicTable.GameClearBgm : null);

    public void StopBgm()
    {
        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = null;
        }

        if (bgmSource != null)
            bgmSource.Stop();

        if (secondaryBgmSource != null)
            secondaryBgmSource.Stop();
    }

    public void PlaySfx(AudioClip clip)
    {
        PlaySfx(clip, 1f);
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        PlaySfxInternal(clip, Mathf.Clamp01(volumeScale), 1f);
    }

    public void PlaySfx(AudioCue cue)
    {
        if (cue == null)
            return;

        PlaySfxInternal(cue.PickClip(), cue.VolumeScale, cue.PickPitch());
    }

    public void PlaySfx(AudioEventId eventId)
    {
        if (eventId == AudioEventId.None)
            return;

        if (audioCueTable != null && audioCueTable.TryGetCue(eventId, out AudioCue cue))
        {
            if (IsCriticalSfxEvent(eventId))
                PlayCriticalSfx(cue);
            else
                PlaySfx(cue);

            return;
        }

        PlaySfx(GetLegacyCue(eventId));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!playMainMenuBgmOnSceneLoaded || !IsMainMenuScene(scene.name))
            return;

        PlayMainMenuBgm();
    }

    private bool IsMainMenuScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || mainMenuSceneNames == null)
            return false;

        for (int i = 0; i < mainMenuSceneNames.Length; i++)
        {
            if (string.Equals(mainMenuSceneNames[i], sceneName, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private void PlayBgmImmediately(AudioClip clip, bool loop)
    {
        _activeBgmSource.Stop();
        _activeBgmSource.clip = clip;
        _activeBgmSource.loop = loop;
        _activeBgmSource.volume = 1f;
        _activeBgmSource.Play();

        _inactiveBgmSource.Stop();
        _inactiveBgmSource.volume = 0f;
    }

    private void PlayOptionalTableBgm(AudioClip clip)
    {
        if (clip == null)
            return;

        PlayBgm(clip, true, defaultBgmFadeSeconds);
    }

    private IEnumerator CrossFadeBgmCoroutine(AudioClip nextClip, bool loop, float duration)
    {
        AudioSource from = _activeBgmSource;
        AudioSource to = _inactiveBgmSource;

        to.Stop();
        to.clip = nextClip;
        to.loop = loop;
        to.volume = 0f;
        to.Play();

        float fromStartVolume = from.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            from.volume = Mathf.Lerp(fromStartVolume, 0f, t);
            to.volume = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        from.Stop();
        from.volume = 0f;
        to.volume = 1f;

        _activeBgmSource = to;
        _inactiveBgmSource = from;
        _bgmFadeCoroutine = null;
    }

    private void PlaySfxInternal(AudioClip clip, float volumeScale, float pitch)
    {
        if (clip == null)
            return;

        ResolveSfxPool();

        AudioSource source = GetAvailableSfxSource();
        if (source == null)
            return;

        // AudioSource의 pitch는 PlayOneShot에도 영향을 주므로, Cue마다 살짝 다른 소리를 만들 수 있습니다.
        source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private void PlayCriticalSfx(AudioCue cue)
    {
        if (cue == null)
            return;

        AudioClip clip = cue.PickClip();
        if (clip == null)
            return;

        AudioSource source = GetCriticalSfxSource();
        if (source == null)
            return;

        // 중요한 발사음은 일반 SFX 풀과 분리하고, 전용 풀을 순환해 연속 발사음의 꼬리가 다음 소리를 덮지 않게 한다.
        source.Stop();
        source.pitch = Mathf.Clamp(cue.PickPitch(), 0.1f, 3f);
        source.PlayOneShot(clip, Mathf.Clamp01(cue.VolumeScale));
    }

    private static bool IsCriticalSfxEvent(AudioEventId eventId)
    {
        return eventId == AudioEventId.BossA_LaserFire;
    }

    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < _sfxSources.Count; i++)
        {
            if (_sfxSources[i] != null && !_sfxSources[i].isPlaying)
                return _sfxSources[i];
        }

        if (expandSfxPoolWhenFull)
            return CreateSfxSource();

        return _sfxSources.Count > 0 ? _sfxSources[0] : null;
    }

    private AudioSource GetCriticalSfxSource()
    {
        ResolveCriticalSfxPool();

        if (_criticalSfxSources.Count == 0)
            return null;

        _nextCriticalSfxSourceIndex %= _criticalSfxSources.Count;
        AudioSource source = _criticalSfxSources[_nextCriticalSfxSourceIndex];
        _nextCriticalSfxSourceIndex = (_nextCriticalSfxSourceIndex + 1) % _criticalSfxSources.Count;

        ConfigureCriticalSfxSource(source);
        return source;
    }

    private void ResolveBgmSources()
    {
        if (bgmSource == null)
            bgmSource = GetComponent<AudioSource>();

        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        if (secondaryBgmSource == null || secondaryBgmSource == bgmSource)
            secondaryBgmSource = gameObject.AddComponent<AudioSource>();

        ConfigureBgmSource(bgmSource);
        ConfigureBgmSource(secondaryBgmSource);

        if (_activeBgmSource == null)
            _activeBgmSource = bgmSource;

        if (_inactiveBgmSource == null || _inactiveBgmSource == _activeBgmSource)
            _inactiveBgmSource = secondaryBgmSource;
    }

    private void ConfigureBgmSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.spatialBlend = 0f;

        // BGM은 SFX와 다른 Mixer Group으로 보내야 BGM 슬라이더만으로 따로 줄일 수 있습니다.
        if (bgmOutputGroup != null)
            source.outputAudioMixerGroup = bgmOutputGroup;
    }

    private void ResolveSfxPool()
    {
        if (_sfxSources.Count >= sfxPoolSize)
            return;

        while (_sfxSources.Count < sfxPoolSize)
            CreateSfxSource();
    }

    private void ResolveCriticalSfxPool()
    {
        int targetCount = Mathf.Max(1, criticalSfxPoolSize);
        while (_criticalSfxSources.Count < targetCount)
        {
            AudioSource source = CreateSfxSource(addToPool: false);
            ConfigureCriticalSfxSource(source);
            _criticalSfxSources.Add(source);
        }

        if (_nextCriticalSfxSourceIndex >= _criticalSfxSources.Count)
            _nextCriticalSfxSourceIndex = 0;
    }

    private AudioSource CreateSfxSource(bool addToPool = true)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;

        // SFX는 별도 Mixer Group으로 보내야 효과음 슬라이더가 배경음에 영향을 주지 않습니다.
        if (sfxOutputGroup != null)
            source.outputAudioMixerGroup = sfxOutputGroup;

        if (addToPool)
            _sfxSources.Add(source);

        return source;
    }

    private void ConfigureCriticalSfxSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.priority = criticalSfxPriority;

        if (sfxOutputGroup != null)
            source.outputAudioMixerGroup = sfxOutputGroup;
    }

    private void SubscribeSliders()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(SetBgmVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void UnsubscribeSliders()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.RemoveListener(SetBgmVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
    }

    private void SubscribeGameEvents()
    {
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;
        GameEvents.OnEnemyKilled += OnEnemyKilled;
        GameEvents.OnTrapTriggered += OnTrapTriggered;
        GameEvents.OnChestOpened += OnChestOpened;
        GameEvents.OnKeyAcquired += OnKeyAcquired;
        GameEvents.OnItemPurchased += OnItemPurchased;
        GameEvents.OnSecretDiscovered += OnSecretDiscovered;
        GameEvents.OnBossDefeated += OnBossDefeated;
        GameEvents.OnPlayerDied += OnPlayerDied;
        GameEvents.OnAudioEventRequested += PlaySfx;
    }

    private void UnsubscribeGameEvents()
    {
        GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
        GameEvents.OnEnemyKilled -= OnEnemyKilled;
        GameEvents.OnTrapTriggered -= OnTrapTriggered;
        GameEvents.OnChestOpened -= OnChestOpened;
        GameEvents.OnKeyAcquired -= OnKeyAcquired;
        GameEvents.OnItemPurchased -= OnItemPurchased;
        GameEvents.OnSecretDiscovered -= OnSecretDiscovered;
        GameEvents.OnBossDefeated -= OnBossDefeated;
        GameEvents.OnPlayerDied -= OnPlayerDied;
        GameEvents.OnAudioEventRequested -= PlaySfx;
    }

    private void OnPlayerDamaged(int damage) => PlaySfx(AudioEventId.PlayerDamaged);
    private void OnEnemyKilled(Vector2Int position, float timeBonus) => PlaySfx(AudioEventId.MonsterKilled);
    private void OnTrapTriggered(Vector2Int position) => PlaySfx(trapTriggeredCue);
    private void OnChestOpened(Vector2Int position) => PlaySfx(AudioEventId.ChestOpened);
    private void OnKeyAcquired(KeyType keyType) => PlaySfx(AudioEventId.KeyPicked);
    private void OnItemPurchased(ItemBase item) => PlaySfx(itemPurchasedCue);
    private void OnSecretDiscovered(Vector2Int position) => PlaySfx(secretDiscoveredCue);
    private void OnBossDefeated()
    {
        PlaySfx(AudioEventId.BossDefeated);
        StopBgm();
    }
    private void OnPlayerDied()
    {
        PlaySfx(AudioEventId.PlayerDied);
        PlayPlayerDeathBgm();
    }

    private AudioCue GetLegacyCue(AudioEventId eventId)
    {
        return eventId switch
        {
            AudioEventId.PlayerDamaged => playerDamagedCue,
            AudioEventId.MonsterKilled => enemyKilledCue,
            AudioEventId.ChestOpened => chestOpenedCue,
            AudioEventId.KeyPicked => keyAcquiredCue,
            AudioEventId.BossDefeated => bossDefeatedCue,
            _ => null
        };
    }

    private void ApplySavedMixerVolumes()
    {
        ApplySavedMixerVolume(MasterPrefsKey, MasterMixerParameter);
        ApplySavedMixerVolume(BgmPrefsKey, BgmMixerParameter);
        ApplySavedMixerVolume(SfxPrefsKey, SfxMixerParameter);
    }

    private void ApplySavedMixerVolume(string prefsKey, string mixerParameter)
    {
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(prefsKey, DefaultVolume));
        ApplyMixerVolume(mixerParameter, savedVolume);
    }

    private void InitializeSlider(Slider slider, string prefsKey)
    {
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(prefsKey, DefaultVolume));

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // 초기화 중 onValueChanged가 다시 저장을 때리면 Inspector 기본값과 저장값 적용 순서가 꼬일 수 있습니다.
            slider.SetValueWithoutNotify(savedVolume);
        }
    }

    private void SetVolume(string prefsKey, string mixerParameter, float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);

        ApplyMixerVolume(mixerParameter, clampedVolume);

        // 설정창을 닫거나 씬이 바뀌어도 사용자가 마지막으로 맞춘 음량을 유지합니다.
        PlayerPrefs.SetFloat(prefsKey, clampedVolume);
        PlayerPrefs.Save();
    }

    private void ApplyMixerVolume(string mixerParameter, float volume)
    {
        if (audioMixer == null)
        {
            if (_isInitialized)
                Debug.LogWarning("AudioManager: AudioMixer가 연결되지 않아 볼륨을 적용할 수 없습니다.");
            return;
        }

        float decibel = ConvertSliderValueToDecibel(volume);
        if (!audioMixer.SetFloat(mixerParameter, decibel))
            Debug.LogWarning($"AudioManager: AudioMixer에 '{mixerParameter}' 노출 파라미터가 없습니다.");
    }

    private static float ConvertSliderValueToDecibel(float volume)
    {
        // AudioMixer는 선형 0~1 값이 아니라 dB 값을 사용합니다.
        // 사람 귀는 볼륨 변화를 로그에 가깝게 느끼므로 Log10 변환을 거쳐야 슬라이더 감각이 자연스럽습니다.
        if (volume <= 0f)
            return MutedDecibel;

        // Mathf.Log10(0)은 음의 무한대가 되므로, 0은 Unity Mixer에서 사실상 음소거로 쓰는 -80dB에 고정합니다.
        float safeVolume = Mathf.Max(volume, MinSliderVolume);
        return Mathf.Log10(safeVolume) * 20f;
    }
}
