using UnityEngine;

/// <summary>
/// 효과음 하나의 재생 규칙을 담는 데이터입니다.
/// 같은 사건이라도 여러 클립/피치를 섞어 반복 재생의 단조로움을 줄일 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "AudioCue", menuName = "60s Dungeon/Audio/Audio Cue")]
public class AudioCue : ScriptableObject
{
    [Header("클립")]
    [Tooltip("이 효과음에서 랜덤으로 고를 클립 목록입니다. 한 개만 넣으면 항상 같은 소리가 납니다.")]
    [SerializeField] private AudioClip[] clips;

    [Header("재생 보정")]
    [Tooltip("이 효과음 자체의 볼륨 배율입니다. 최종 볼륨은 SFX 슬라이더와 Mixer 설정도 함께 적용됩니다.")]
    [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;

    [Tooltip("랜덤 피치 최소값입니다. 1이면 원래 속도/높이로 재생됩니다.")]
    [SerializeField, Range(0.1f, 3f)] private float pitchMin = 1f;

    [Tooltip("랜덤 피치 최대값입니다. 최소값과 같으면 피치가 고정됩니다.")]
    [SerializeField, Range(0.1f, 3f)] private float pitchMax = 1f;

    public float VolumeScale => volumeScale;

    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    public float PickPitch()
    {
        float min = Mathf.Min(pitchMin, pitchMax);
        float max = Mathf.Max(pitchMin, pitchMax);
        return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
    }

    private void OnValidate()
    {
        volumeScale = Mathf.Clamp01(volumeScale);
        pitchMin = Mathf.Clamp(pitchMin, 0.1f, 3f);
        pitchMax = Mathf.Clamp(pitchMax, 0.1f, 3f);
    }
}
