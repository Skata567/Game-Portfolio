using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사운드 UI를 관리하고 SoundManager와 연결하는 역할.
/// 각 씬의 사운드 설정 패널에 이 스크립트를 추가하세요.
/// </summary>
public class SoundView : MonoBehaviour
{
    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider MasterAudioSlider;
    [SerializeField] private Slider BGMAudioSlider;
    [SerializeField] private Slider SFXAudioSlider;

    [Header("음소거 버튼")]
    [SerializeField] private Button OffSoundButton;
    [SerializeField] private Button OnSoundButton;
    
    [Header("초기화 버튼")]
    [SerializeField] private Button ResetButton;


    private SoundManager soundManager;

    private void Start()
    {
        // 싱글톤 SoundManager 인스턴스 찾기
        soundManager = SoundManager.Instance;
        if (soundManager == null)
        {
            Debug.LogError("[SoundView] SoundManager.Instance를 찾을 수 없습니다!");
            return;
        }

        // UI 초기화 및 이벤트 리스너 등록
        InitializeUI();
        SetupListeners();
    }

    /// <summary>
    /// SoundManager의 현재 설정값으로 UI를 초기화합니다.
    /// </summary>
    private void InitializeUI()
    {
        if (MasterAudioSlider != null)
            MasterAudioSlider.value = soundManager.MasterVolume;

        if (BGMAudioSlider != null)
            BGMAudioSlider.value = soundManager.BGMVolume;

        if (SFXAudioSlider != null)
            SFXAudioSlider.value = soundManager.SFXVolume;
        
        UpdateButtonStates(soundManager.IsMuted);
    }

    /// <summary>
    /// UI 컴포넌트의 이벤트 리스너를 등록합니다.
    /// </summary>
    private void SetupListeners()
    {
        if (MasterAudioSlider != null)
            MasterAudioSlider.onValueChanged.AddListener(soundManager.SetMasterVolume);

        if (BGMAudioSlider != null)
            BGMAudioSlider.onValueChanged.AddListener(soundManager.SetBGMVolume);

        if (SFXAudioSlider != null)
            SFXAudioSlider.onValueChanged.AddListener(soundManager.SetSFXVolume);

        if (OffSoundButton != null)
            OffSoundButton.onClick.AddListener(Mute);

        if (OnSoundButton != null)
            OnSoundButton.onClick.AddListener(Unmute);
            
        if (ResetButton != null)
            ResetButton.onClick.AddListener(ResetSettings);
    }

    private void Mute()
    {
        soundManager.MuteAudio();
        UpdateButtonStates(true);
    }

    private void Unmute()
    {
        soundManager.UnmuteAudio();
        UpdateButtonStates(false);
    }
    
    private void ResetSettings()
    {
        soundManager.ResetToDefault();
        // ResetToDefault가 UI를 직접 업데이트하지 않으므로, 여기서 다시 초기화
        InitializeUI();
    }

    /// <summary>
    /// 음소거 상태에 따라 버튼 표시/숨김
    /// - 음소거 OFF 상태: OffSoundButton 보임 (클릭 시 음소거 ON)
    /// - 음소거 ON 상태: OnSoundButton 보임 (클릭 시 음소거 OFF)
    /// </summary>
    private void UpdateButtonStates(bool isMuted)
    {
        // Off 버튼: 음소거 아닐 때만 보임 (클릭하면 음소거 됨)
        if (OffSoundButton != null)
            OffSoundButton.gameObject.SetActive(!isMuted);

        // On 버튼: 음소거일 때만 보임 (클릭하면 음소거 해제)
        if (OnSoundButton != null)
            OnSoundButton.gameObject.SetActive(isMuted);
    }

    private void OnDestroy()
    {
        // 씬 전환 시 리스너 정리 (메모리 누수 방지)
        if (MasterAudioSlider != null) MasterAudioSlider.onValueChanged.RemoveAllListeners();
        if (BGMAudioSlider != null) BGMAudioSlider.onValueChanged.RemoveAllListeners();
        if (SFXAudioSlider != null) SFXAudioSlider.onValueChanged.RemoveAllListeners();
        if (OffSoundButton != null) OffSoundButton.onClick.RemoveAllListeners();
        if (OnSoundButton != null) OnSoundButton.onClick.RemoveAllListeners();
        if (ResetButton != null) ResetButton.onClick.RemoveAllListeners();
    }
}
