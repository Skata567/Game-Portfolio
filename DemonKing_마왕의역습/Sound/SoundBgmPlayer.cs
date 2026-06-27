using UnityEngine;

public class SceneBGMPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;

    void Start()
    {
        if (bgmClip != null)
        {
            SoundManager.Instance?.PlayBGM(bgmClip);
        }
    }
}