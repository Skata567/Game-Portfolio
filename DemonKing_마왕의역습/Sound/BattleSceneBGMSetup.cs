using UnityEngine;

/// <summary>
/// 배틀씬 시작 시 노드 타입에 따라 적절한 BGM을 재생합니다.
/// - 일반 노드: normalBattleBGM
/// - 엘리트 노드: eliteBattleBGM
/// - 보스 노드: bossBattleBGM
///
/// 사용법:
/// 1. 배틀씬의 빈 GameObject에 이 스크립트 추가
/// 2. Inspector에서 3개의 BGM AudioClip 할당
/// 3. 씬 시작 시 자동으로 적절한 BGM 재생
/// </summary>
public class BattleSceneBGMSetup : MonoBehaviour
{
    #region Inspector 설정

    [Header("배틀 BGM 설정")]
    [SerializeField] private AudioClip normalBattleBGM;
    [Tooltip("일반 전투 노드에서 재생될 BGM")]

    [SerializeField] private AudioClip eliteBattleBGM;
    [Tooltip("엘리트 전투 노드에서 재생될 BGM")]

    [SerializeField] private AudioClip bossBattleBGM;
    [Tooltip("보스 전투 노드에서 재생될 BGM")]

    [Header("재생 옵션")]
    [SerializeField] private bool useCrossFade = true;
    [Tooltip("크로스페이드로 부드럽게 전환할지 여부")]

    [SerializeField] private float crossFadeDuration = 1.5f;
    [Tooltip("크로스페이드 지속 시간 (초)")]

    #endregion

    #region 초기화 및 BGM 재생

    private void Start()
    {
        PlayBattleBGM();
    }

    /// <summary>
    /// StaticInfoManager의 플래그를 확인하여 적절한 BGM 재생
    /// </summary>
    private void PlayBattleBGM()
    {
        AudioClip clipToPlay = null;
        string nodeType = "일반";

        // 노드 타입 확인 (우선순위: 보스 > 엘리트 > 일반)
        if (StaticInfoManager.floor == 15)
        {
            clipToPlay = bossBattleBGM;
            nodeType = "보스";
        }
        else if (StaticInfoManager.isElite)
        {
            clipToPlay = eliteBattleBGM;
            nodeType = "엘리트";
        }
        else
        {
            clipToPlay = normalBattleBGM;
            nodeType = "일반";
        }

        // BGM 재생
        if (clipToPlay != null)
        {
            if (useCrossFade && SoundManager.Instance != null)
            {
                SoundManager.Instance.CrossFadeBGM(clipToPlay, crossFadeDuration);
                Debug.Log($"[BattleSceneBGMSetup] {nodeType} 전투 BGM 크로스페이드 재생: {clipToPlay.name}");
            }
            else if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBGM(clipToPlay);
                Debug.Log($"[BattleSceneBGMSetup] {nodeType} 전투 BGM 즉시 재생: {clipToPlay.name}");
            }
            else
            {
                Debug.LogError("[BattleSceneBGMSetup] SoundManager.Instance를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"[BattleSceneBGMSetup] {nodeType} 전투 BGM이 할당되지 않았습니다. Inspector에서 AudioClip을 설정해주세요.");
        }
    }

    #endregion

    #region 디버그용 메서드 (Inspector에서 테스트 가능)

    /// <summary>
    /// Inspector 또는 디버그 모드에서 현재 노드 타입 확인용
    /// </summary>
    [ContextMenu("현재 노드 타입 확인")]
    private void DebugNodeType()
    {
        if (StaticInfoManager.isBoss)
        {
            Debug.Log("[BattleSceneBGMSetup] 현재 노드: 보스 전투");
        }
        else if (StaticInfoManager.isElite)
        {
            Debug.Log("[BattleSceneBGMSetup] 현재 노드: 엘리트 전투");
        }
        else
        {
            Debug.Log("[BattleSceneBGMSetup] 현재 노드: 일반 전투");
        }
    }

    #endregion
}
