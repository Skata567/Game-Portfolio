using UnityEngine;

/// <summary>
/// 층 번호와 상황에 맞는 BGM 클립을 데이터로 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "MusicTable", menuName = "60s Dungeon/Audio/Music Table")]
public class MusicTable : ScriptableObject
{
    [Header("Common BGM")]
    [Tooltip("메인 메뉴에서 사용할 BGM입니다.")]
    [SerializeField] private AudioClip mainMenuBgm;

    [Tooltip("Safe Room에서 사용할 BGM입니다. 비워두면 현재 BGM을 유지합니다.")]
    [SerializeField] private AudioClip safeRoomBgm;

    [Tooltip("상점/휴식 공간에서 사용할 BGM입니다. 비워두면 현재 BGM을 유지합니다.")]
    [SerializeField] private AudioClip shopOrRestBgm;

    [Tooltip("플레이어 사망 화면에서 사용할 BGM입니다.")]
    [SerializeField] private AudioClip playerDeathBgm;

    [Tooltip("게임 클리어 화면에서 사용할 BGM입니다.")]
    [SerializeField] private AudioClip gameClearBgm;

    [Header("Area BGM")]
    [Tooltip("1~5층 일반 구간 BGM입니다.")]
    [SerializeField] private AudioClip area1Bgm;

    [Tooltip("6~10층 일반 구간 BGM입니다.")]
    [SerializeField] private AudioClip area2Bgm;

    [Tooltip("11~15층 일반 구간 BGM입니다.")]
    [SerializeField] private AudioClip area3Bgm;

    [Header("Boss Prelude BGM")]
    [Tooltip("5층 보스 이름 연출 전까지 사용할 대기 BGM입니다. 비워두면 BGM을 정지합니다.")]
    [SerializeField] private AudioClip bossPreludeDepth5Bgm;

    [Tooltip("10층 보스 이름 연출 전까지 사용할 대기 BGM입니다. 비워두면 BGM을 정지합니다.")]
    [SerializeField] private AudioClip bossPreludeDepth10Bgm;

    [Tooltip("15층 보스 이름 연출 전까지 사용할 대기 BGM입니다. 비워두면 BGM을 정지합니다.")]
    [SerializeField] private AudioClip bossPreludeDepth15Bgm;

    [Header("Boss BGM")]
    [Tooltip("5층 보스 전투 BGM입니다. 현재 전투 BGM은 DialogData.BossBgm을 우선 사용합니다.")]
    [SerializeField] private AudioClip bossDepth5Bgm;

    [Tooltip("10층 보스 전투 BGM입니다. 현재 전투 BGM은 DialogData.BossBgm을 우선 사용합니다.")]
    [SerializeField] private AudioClip bossDepth10Bgm;

    [Tooltip("15층 보스 전투 BGM입니다. 현재 전투 BGM은 DialogData.BossBgm을 우선 사용합니다.")]
    [SerializeField] private AudioClip bossDepth15Bgm;

    public AudioClip MainMenuBgm => mainMenuBgm;
    public AudioClip SafeRoomBgm => safeRoomBgm;
    public AudioClip ShopOrRestBgm => shopOrRestBgm;
    public AudioClip PlayerDeathBgm => playerDeathBgm;
    public AudioClip GameClearBgm => gameClearBgm;

    public AudioClip GetBgmForDepth(int depth, bool isBoss)
    {
        if (isBoss)
        {
            AudioClip bossClip = GetBossBgm(depth);
            if (bossClip != null)
                return bossClip;
        }

        return GetAreaBgm(depth);
    }

    public AudioClip GetBossPreludeBgm(int depth)
    {
        if (depth == 5)
            return bossPreludeDepth5Bgm;

        if (depth == 10)
            return bossPreludeDepth10Bgm;

        if (depth == 15)
            return bossPreludeDepth15Bgm;

        return null;
    }

    private AudioClip GetAreaBgm(int depth)
    {
        if (depth <= 5)
            return area1Bgm;

        if (depth <= 10)
            return area2Bgm;

        return area3Bgm;
    }

    private AudioClip GetBossBgm(int depth)
    {
        if (depth == 5)
            return bossDepth5Bgm;

        if (depth == 10)
            return bossDepth10Bgm;

        if (depth == 15)
            return bossDepth15Bgm;

        return null;
    }
}
