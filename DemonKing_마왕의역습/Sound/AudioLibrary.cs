using UnityEngine;
using System.Collections.Generic;

// Unity 에디터의 Create 메뉴를 통해 이 애셋을 생성할 수 있게 해줍니다.
[CreateAssetMenu(fileName = "AudioLibrary", menuName = "AudioData/Audio Library", order = 1)]
public class AudioLibrary : ScriptableObject
{
    // SoundManager에서 쉽게 클립을 찾을 수 있도록 Dictionary를 사용합니다.
    private Dictionary<string, AudioClip> clipLookup;

    // Inspector에 표시될 사운드 목록입니다.
    [SerializeField]
    private List<SoundEffect> soundEffects;

    // SoundEffect: 사운드의 이름(ID)과 실제 오디오 클립을 묶는 구조체입니다.
    [System.Serializable]
    public struct SoundEffect
    {
        public string name;
        public AudioClip clip;
    }

    /// <summary>
    /// 이 객체가 활성화될 때 호출됩니다.
    /// 리스트를 기반으로 빠른 조회를 위한 Dictionary를 생성합니다.
    /// </summary>
    private void OnEnable()
    {
        clipLookup = new Dictionary<string, AudioClip>();
        foreach (var effect in soundEffects)
        {
            if (effect.clip != null && !clipLookup.ContainsKey(effect.name))
            {
                clipLookup.Add(effect.name, effect.clip);
            }
        }
    }

    /// <summary>
    /// 이름(ID)으로 오디오 클립을 찾아 반환합니다.
    /// </summary>
    /// <param name="name">찾고자 하는 사운드의 이름</param>
    /// <returns>찾은 오디오 클립, 없으면 null</returns>
    public AudioClip GetClipByName(string name)
    {
        if (clipLookup.TryGetValue(name, out AudioClip clip))
        {
            return clip;
        }

        Debug.LogWarning($"[AudioLibrary] '{name}' 이라는 이름의 사운드를 찾을 수 없습니다.");
        return null;
    }
}
