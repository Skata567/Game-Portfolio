using UnityEngine;
using UnityEngine.UI;

public class AudioVolumeSliderBinder : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void OnEnable()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioVolumeSliderBinder: AudioManager가 없어 볼륨 슬라이더를 연결할 수 없습니다.");
            return;
        }

        // AudioManager는 씬을 넘어 유지되고, 슬라이더는 씬마다 새로 생기므로 켜질 때마다 다시 연결한다.
        AudioManager.Instance.BindVolumeSliders(masterVolumeSlider, bgmVolumeSlider, sfxVolumeSlider);
    }

    private void OnDisable()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.UnbindVolumeSliders(masterVolumeSlider, bgmVolumeSlider, sfxVolumeSlider);
    }
}
