using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 코나미 커맨드 치트 시스템
/// ` 키를 누르고 ↑↑↓↓←→←→BA 입력 시 자원 최대치 + 모든 유닛 정보 해금
///
/// 사용 방법:
/// 1. 빈 GameObject 생성 → "KonamiCodeCheat" 이름 지정
/// 2. 이 스크립트 AddComponent
/// 3. 게임 실행 중 ` 키 누르고 코나미 커맨드 입력
///
/// 코나미 커맨드: ↑ ↑ ↓ ↓ ← → ← → B A
/// (화살표 키 + B키 + A키)
/// </summary>
public class KonamiCodeCheat : MonoBehaviour
{
    #region 코나미 커맨드 설정

    private static readonly KeyCode[] konamiCode = new KeyCode[]
    {
        KeyCode.UpArrow,
        KeyCode.UpArrow,
        KeyCode.DownArrow,
        KeyCode.DownArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.B,
        KeyCode.A
    };

    #endregion

    #region Private Fields

    private int currentIndex = 0;
    private bool isCheatMode = false;
    private bool cheatActivated = false;
    private float lastInputTime = 0f;
    private const float inputTimeout = 2f; // 2초 이내에 입력해야 함

    private MonsterCatalog catalogCache;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        catalogCache = MonsterCatalog.Instance;

        if (catalogCache == null)
        {
            Debug.LogWarning("[KonamiCodeCheat] MonsterCatalog를 찾을 수 없습니다. 나중에 재시도합니다.");
        }
    }

    void Update()
    {
        // ` 키로 치트 모드 활성화/비활성화
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            isCheatMode = !isCheatMode;

            if (isCheatMode)
            {
                Debug.Log("[KonamiCodeCheat] 치트 모드 활성화. 코나미 커맨드를 입력하세요: ↑↑↓↓←→←→BA");
                currentIndex = 0;
                lastInputTime = Time.time;
            }
            else
            {
                Debug.Log("[KonamiCodeCheat] 치트 모드 비활성화");
                currentIndex = 0;
            }
        }

        // 치트 모드일 때만 코나미 커맨드 감지
        if (isCheatMode)
        {
            DetectKonamiCode();
        }
    }

    #endregion

    #region 코나미 커맨드 감지

    /// <summary>
    /// 코나미 커맨드 입력 감지
    /// </summary>
    private void DetectKonamiCode()
    {
        // 타임아웃 체크 (2초 이상 입력 없으면 초기화)
        if (Time.time - lastInputTime > inputTimeout)
        {
            if (currentIndex > 0)
            {
                Debug.Log("[KonamiCodeCheat] 입력 시간 초과. 처음부터 다시 입력하세요.");
                currentIndex = 0;
            }
        }

        // 현재 입력해야 할 키 확인
        if (Input.GetKeyDown(konamiCode[currentIndex]))
        {
            currentIndex++;
            lastInputTime = Time.time;

            Debug.Log($"[KonamiCodeCheat] 코나미 커맨드 진행: {currentIndex}/{konamiCode.Length}");

            // 전체 커맨드 완성 시
            if (currentIndex >= konamiCode.Length)
            {
                ActivateCheat();
                currentIndex = 0;
                isCheatMode = false; // 치트 활성화 후 모드 종료
            }
        }
        // 잘못된 키 입력 시
        else if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.BackQuote))
        {
            // 코나미 커맨드 키가 아닌 다른 키 입력 시 초기화
            bool isWrongKey = false;

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key) && !IsKonamiKey(key))
                {
                    isWrongKey = true;
                    break;
                }
            }

            if (isWrongKey && currentIndex > 0)
            {
                Debug.Log("[KonamiCodeCheat] 잘못된 입력. 처음부터 다시 입력하세요.");
                currentIndex = 0;
            }
        }
    }

    /// <summary>
    /// 코나미 커맨드에 포함된 키인지 확인
    /// </summary>
    private bool IsKonamiKey(KeyCode key)
    {
        foreach (KeyCode konamiKey in konamiCode)
        {
            if (key == konamiKey) return true;
        }
        return false;
    }

    #endregion

    #region 치트 활성화

    /// <summary>
    /// 코나미 커맨드 성공 시 치트 활성화
    /// </summary>
    private void ActivateCheat()
    {
        if (cheatActivated)
        {
            Debug.Log("[KonamiCodeCheat] 치트가 이미 활성화되어 있습니다!");
            return;
        }

        Debug.Log("=== [KonamiCodeCheat] 코나미 커맨드 성공! 치트 활성화 ===");

        // MonsterCatalog 캐시 재확인
        if (catalogCache == null)
        {
            catalogCache = MonsterCatalog.Instance;
        }

        // 1. 자원 최대치
        MaxResources();

        // 2. 모든 유닛 정보 해금
        UnlockAllUnitsInfo();

        // 3. 모든 스킬 최대치
       /* MaxAllSkills();*/

        cheatActivated = true;

        Debug.Log("=== [KonamiCodeCheat] 치트 활성화 완료! ===");
        Debug.Log("[KonamiCodeCheat] 골드: 999999, 철: 999999, 다이아: 999999, 미스릴: 999");
    }

    #endregion

    #region 치트 기능들

    /// <summary>
    /// 모든 자원 최대치로 설정
    /// </summary>
    private void MaxResources()
    {
        StaticInfoManager.gold = 999999;
        StaticInfoManager.iron = 999999;
        StaticInfoManager.dia = 999999;
        StaticInfoManager.mis = 999;

        Debug.Log("[KonamiCodeCheat] 자원 최대치 설정 완료");

        // UIManager가 있다면 UI 갱신
        var uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateUI(refreshPanels: false);
        }
    }

    /// <summary>
    /// 모든 유닛 정보 해금 (아군 + 특수 + 적군 전체)
    /// </summary>
    private void UnlockAllUnitsInfo()
    {
        if (catalogCache == null || !catalogCache.IsReady)
        {
            Debug.LogError("[KonamiCodeCheat] MonsterCatalog이 준비되지 않았습니다!");
            return;
        }

        Debug.Log("[KonamiCodeCheat] 모든 유닛 정보 해금 시작");

        // 1. 모든 아군 몬스터 정보 해금
        UnlockAllNormalMonsters();

        // 2. 모든 특수 몬스터 정보 해금
        UnlockAllSpecialMonsters();

        // 3. 모든 적군 조우 처리
       /* UnlockAllEnemies();*/

        Debug.Log("[KonamiCodeCheat] 모든 유닛 정보 해금 완료");
        Debug.Log($"[KonamiCodeCheat] 총 해금: 일반 {catalogCache.normalUnits.Count}개, 특수 {catalogCache.specialUnits.Count}개, 적군 {catalogCache.enemyUnits.Count}개");
    }

    /// <summary>
    /// 모든 아군 몬스터 정보 해금
    /// </summary>
    private void UnlockAllNormalMonsters()
    {
        int count = 0;
        foreach (var unit in catalogCache.normalUnits)
        {
            if (unit != null)
            {
                Debug.Log($"[KonamiCodeCheat] 아군 해금: {unit.displayName} (ID: {unit.enemyId})");
                count++;
            }
        }

        Debug.Log($"[KonamiCodeCheat] 아군 몬스터 {count}개 정보 해금 완료");
    }

    /// <summary>
    /// 모든 특수 몬스터 정보 해금
    /// </summary>
    private void UnlockAllSpecialMonsters()
    {
        int count = 0;
        foreach (var unit in catalogCache.specialUnits)
        {
            if (unit != null)
            {
                Debug.Log($"[KonamiCodeCheat] 특수 해금: {unit.displayName} (ID: {unit.enemyId})");
                count++;
            }
        }

        Debug.Log($"[KonamiCodeCheat] 특수 몬스터 {count}개 정보 해금 완료");
    }

    /// <summary>
    /// 모든 적군 조우 처리
    /// </summary>
/*    private void UnlockAllEnemies()
    {
        int count = 0;
        foreach (var unit in catalogCache.enemyUnits)
        {
            if (unit != null)
            {
                unit.encountered = true;
                Debug.Log($"[KonamiCodeCheat] 적군 조우: {unit.displayName} (ID: {unit.enemyId})");
                count++;
            }
        }

        Debug.Log($"[KonamiCodeCheat] 적군 {count}개 조우 처리 완료");
    }*/

    /// <summary>
    /// 모든 스킬 최대치
    /// </summary>
/*    private void MaxAllSkills()
    {
        var su = FindAnyObjectByType<SkillUpgrade>();
        if (su != null)
        {
            su.SetSkillLevels(5, 5, 5, 5, 5, 3, 5, 5, refreshUI: true);
            Debug.Log("[KonamiCodeCheat] 모든 스킬 최대치 설정 완료");
        }
        else
        {
            Debug.LogWarning("[KonamiCodeCheat] SkillUpgrade를 찾을 수 없습니다.");
        }
    }*/

    /// <summary>
    /// 치트 초기화 (디버그용)
    /// </summary>
    [ContextMenu("치트 초기화")]
    public void ResetCheat()
    {
        cheatActivated = false;
        currentIndex = 0;
        isCheatMode = false;
        Debug.Log("[KonamiCodeCheat] 치트 초기화 완료. 다시 코나미 커맨드를 입력할 수 있습니다.");
    }

    #endregion

    #region 디버그 정보

    void OnGUI()
    {
        if (isCheatMode)
        {
            // 화면 왼쪽 상단에 진행 상황 표시
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 10, 500, 30), $"코나미 커맨드 입력 중... ({currentIndex}/{konamiCode.Length})");
            GUI.Label(new Rect(10, 40, 500, 30), "↑↑↓↓←→←→BA");
            GUI.color = Color.white;
        }

        if (cheatActivated)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 70, 300, 30), "치트 활성화됨!");
            GUI.color = Color.white;
        }
    }

    #endregion
}
