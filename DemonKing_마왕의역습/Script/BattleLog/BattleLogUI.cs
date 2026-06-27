using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 로그 UI 관리 스크립트
///
/// 역할:
/// - 전투 중 발생하는 이벤트(공격, 피해, 상태이상, 턴 시작 등)를 텍스트로 표시
/// - ScrollView에 로그 메시지를 동적으로 추가
/// - Object Pooling 방식으로 Text 프리팹 재사용 (GC 부담 감소)
/// - BattleLog.FormatK() 메서드를 활용하여 한국어 조사 자동 처리
///
/// 사용 예시:
/// - battleLogUI.AddLog("{0}.이(가) {1}.를 공격했다!".FormatK("고블린", "기사"));
/// - battleLogUI.AddTurnLog(3); // "3턴 시작"
/// - battleLogUI.AddDamageLog("고블린", "기사", 150);
/// - battleLogUI.AddStatusLog("기사", "기절");
/// </summary>
public class BattleLogUI : MonoBehaviour
{
    public static BattleLogUI instance;

    #region UI 컴포넌트들
    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject battleLogBg; //배틀 로그 띄우는 창
    [SerializeField] private Transform logContent; // ScrollView의 Content (로그가 추가될 부모 Transform)
    [SerializeField] private GameObject logTextPrefab; // 로그 텍스트 프리팹 (Text 컴포넌트 포함)
    [SerializeField] private ScrollRect scrollRect; // 스크롤뷰 (자동 스크롤용)
    [SerializeField] private int maxLogCount = 50; // 최대 로그 개수
    #endregion

    #region 오브젝트 풀링
    private Queue<GameObject> logPool = new Queue<GameObject>(); // 비활성화된 로그 오브젝트 풀
    private List<GameObject> activeLogObjects = new List<GameObject>(); // 현재 활성화된 로그들
    #endregion

    #region 색상 테마 상수
    // 유닛 이름 색상
    private const string COLOR_UNIT = "#66CCFF";        // 하늘색 - 아군 유닛
    private const string COLOR_ENEMY = "#FF9999";       // 연한 빨강 - 적군 유닛

    // 능력 타입별 색상
    private const string COLOR_BUFF = "#00FF88";        // 초록색 - 버프 (공격력, 신속, 회피)
    private const string COLOR_HEAL = "#00FFFF";        // 청록색 - 회복
    private const string COLOR_DEBUFF = "#FF6666";      // 빨강 - 디버프 (기절, 중독 등)
    private const string COLOR_SHIELD = "#FFD700";      // 금색 - 방어/데미지 감소
    private const string COLOR_SKILL = "#FF88FF";       // 분홍색 - 스킬 사용

    // 시스템 메시지 색상
    private const string COLOR_TURN = "#FFFF00";        // 노란색 - 턴 시작
    private const string COLOR_DEATH = "#EE0000";       // 밝은 빨강 - 사망
    private const string COLOR_DAMAGE = "#FF8800";      // 주황색 - 데미지 수치
    #endregion

    #region 초기화 메서드들
    private void Awake()
    {
        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // UI 컴포넌트 자동 찾기 (Inspector 미할당 시)
        if (logContent == null)
        {
            var scrollView = GetComponentInChildren<ScrollRect>();
            if (scrollView != null)
            {
                logContent = scrollView.content;
                scrollRect = scrollView;
            }
        }

        // 오브젝트 풀 미리 생성 (10개 정도)
        InitializePool(10);

        // 시작 메시지
        AddLog("전투 시작!");
    }

    /// <summary>
    /// 오브젝트 풀 초기화 (미리 일정 개수 생성)
    /// </summary>
    private void InitializePool(int count)
    {
        if (logTextPrefab == null || logContent == null)
        {
            Debug.LogError("[BattleLogUI] logTextPrefab 또는 logContent가 null입니다!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject logObj = Instantiate(logTextPrefab, logContent);
            logObj.SetActive(false);
            logPool.Enqueue(logObj);
        }
    }
    #endregion

    #region 로그 추가 메서드들
    /// <summary>
    /// 일반 로그 메시지 추가
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    public void AddLog(string message)
    {
        // 로그 오브젝트 받아오기
        GameObject logObj = GetLogFromPool();
        // 가져온 오브젝트의 컴포넌트에 message 설정
        Text textComponent = logObj.GetComponent<Text>();
        if(textComponent == null)
        {
            Debug.Log("[BattleLogUI] Text 컴포넌트를 찾지 못하였습니다.");
            return;
        }
        textComponent.text = message;

        // 오브젝트 활성화 및 Hierarchy 맨 앞으로 이동 (최신 로그가 아래에 표시)
        logObj.SetActive(true);
        logObj.transform.SetAsLastSibling();

        // activeLogObjects 리스트에 추가
        activeLogObjects.Add(logObj);

        // 최대 개수 초과 시 가장 오래된 로그 제거
        while(activeLogObjects.Count > maxLogCount)
        {
            GameObject oldestLog = activeLogObjects[0];
            ReturnLogToPool(oldestLog);
            activeLogObjects.RemoveAt(0);
        }

        // 스크롤을 맨 아래로 이동
        StartCoroutine(ScrollToBottom());

        Debug.Log($"[BattleLog] {message}");
    }

    /// <summary>
    /// 풀에서 로그 오브젝트 가져오기
    /// </summary>
    private GameObject GetLogFromPool()
    {
        GameObject logObj;

        // 풀에 사용 가능한 오브젝트가 있으면 가져오기
        if (logPool.Count > 0)
        {
            logObj = logPool.Dequeue();
        }
        else
        {
            // 풀이 비었으면 새로 생성
            logObj = Instantiate(logTextPrefab, logContent);
        }

        return logObj;
    }

    /// <summary>
    /// 로그 오브젝트를 풀로 반환
    /// </summary>
    private void ReturnLogToPool(GameObject logObj)
    {
        if (logObj == null) return;

        logObj.SetActive(false);
        // Hierarchy 순서를 맨 뒤로 보내서 정리 (선택사항)
        logObj.transform.SetAsLastSibling();
        logPool.Enqueue(logObj);
    }

    /// <summary>
    /// 턴 시작 로그
    /// </summary>
    /// <param name="turnNumber">턴 번호</param>
    public void AddTurnLog(int turnNumber)
    {
        string message = $"<color={COLOR_TURN}>========== {turnNumber}턴 시작 ==========</color>";
        AddLog(message);
    }

    #region ID 기반 색상 판별 헬퍼
    /// <summary>
    /// 유닛 ID로 아군/적군 판별 (아군 = 파란색, 적군 = 빨간색)
    /// </summary>
    private bool IsPlayerUnit(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return true; // 기본값 아군
        int id = int.Parse(unitId);
        return id < 300; // 101-117, 201-205 = 아군 / 301-315 = 적군
    }

    /// <summary>
    /// 유닛 ID로 색상 결정
    /// </summary>
    private string GetUnitColor(string unitId)
    {
        return IsPlayerUnit(unitId) ? COLOR_UNIT : COLOR_ENEMY;
    }
    #endregion

    /// <summary>
    /// 데미지 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddDamageLog(string attackerName, string targetName, float damage)
    {
        AddDamageLog(attackerName, targetName, damage, null, null);
    }

    /// <summary>
    /// 데미지 로그 (아군/적군 ID로 색상 판별)
    /// </summary>
    /// <param name="attackerName">공격자 이름</param>
    /// <param name="targetName">피격자 이름</param>
    /// <param name="damage">피해량</param>
    /// <param name="attackerId">공격자 ID (101-117, 201-205 = 아군 / 301-315 = 적군)</param>
    /// <param name="targetId">대상 ID</param>
    public void AddDamageLog(string attackerName, string targetName, float damage, string attackerId, string targetId)
    {
        int damageInt = Mathf.FloorToInt(damage);  // 내림 처리

        // 조사가 붙은 이름들 먼저 생성 (FormatK로 조사 처리)
        string attackerWithJosa = "{0}.이".FormatK(attackerName);  // "고블린이" 또는 "드래곤이"
        string josaOnlyUnit = attackerWithJosa.Substring(attackerName.Length);

        // 공격자/대상 색상 결정
        string attackerColor = GetUnitColor(attackerId);
        string targetColor = GetUnitColor(targetId);

        // 색상 적용된 최종 메시지
        string message = $"<color={attackerColor}>{attackerName}</color>{josaOnlyUnit} <color={targetColor}>{targetName}</color>" +
            $"에게 <color={COLOR_DAMAGE}>{damageInt}</color>의 <color=Red>피해</color>를 입혔다.";

        AddLog(message);
    }

    /// <summary>
    /// 상태이상 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddStatusLog(string unitName, string statusName)
    {
        AddStatusLog(unitName, statusName, null);
    }

    /// <summary>
    /// 상태이상 로그 (ID 기반 색상)
    /// </summary>
    /// <param name="unitName">유닛 이름</param>
    /// <param name="statusName">상태이상 이름 (기절, 화상, 넘어짐 등)</param>
    /// <param name="unitId">유닛 ID</param>
    public void AddStatusLog(string unitName, string statusName, string unitId)
    {
        string unitWithJosa = "{0}.은".FormatK(unitName);  // "고블린은" 또는 "드래곤은"
        string josaOnlyUnit = unitWithJosa.Substring(unitName.Length); // 은
        string unitColor = GetUnitColor(unitId);

        string message;
        if ((unitName == "좀비" || unitName == "스켈레톤") && statusName == "기절")
        {
            message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} " +
                $"<color={COLOR_BUFF}>{statusName}</color> 상태에 <color=Yellow>면역</color> 입니다!";
        }
        else
        {
            message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} " +
                $"<color={COLOR_BUFF}>{statusName}</color> 상태가 되었다!";
        }
        AddLog(message);
    }

    /// <summary>
    /// 버프 상태 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddPlusStatusLog(string unitName, string statusName, float Hp)
    {
        AddPlusStatusLog(unitName, statusName, Hp, null);
    }

    /// <summary>
    /// 버프 상태 로그 (ID 기반 색상)
    /// </summary>
    public void AddPlusStatusLog(string unitName, string statusName, float Hp, string unitId)
    {
        string unitWithJosa = "{0}.은".FormatK(unitName);
        string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);
        string unitColor = GetUnitColor(unitId);

        if (statusName == "회피")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>" +
                $"{Hp}%</color>만큼 <color={COLOR_BUFF}>{statusName}력 증가</color> 상태가 되었다.";
            AddLog(message);
        }
        else if (statusName == "공격")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>" +
                $"{Hp}%</color>만큼 <color={COLOR_BUFF}>{statusName}력 증가</color> 상태가 되었다.";
            AddLog(message);
        }
        else if (statusName == "회복")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} 체력이 <color={COLOR_HEAL}>" +
                $"{Hp}</color>만큼 <color={COLOR_HEAL}>{statusName}</color> 되었다.";
            AddLog(message);
        }
        else if (statusName == "데미지")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_SHIELD}>" +
                $"{Hp}%</color>만큼 <color={COLOR_SHIELD}>받는 {statusName} 감소</color> 상태가 되었다.";
            AddLog(message);
        }
    }

    /// <summary>
    /// 특수 버프 상태 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddSpStatusLog(string unitName, string statusName, float Hp)
    {
        AddSpStatusLog(unitName, statusName, Hp, null);
    }

    /// <summary>
    /// 특수 버프 상태 로그 (ID 기반 색상)
    /// </summary>
    public void AddSpStatusLog(string unitName, string statusName, float Hp, string unitId)
    {
        string unitColor = GetUnitColor(unitId);

        if (statusName == "신속")
        {
            string unitWithJosa = "{0}.은".FormatK(unitName);
            string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);

            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>" +
                $"{Hp}</color>만큼 <color={COLOR_BUFF}>{statusName} 증가</color> 상태가 되었다.";
            AddLog(message);
        }
        else if (statusName == "공격력")
        {
            string unitWithJosa = "{0}.은".FormatK(unitName);
            string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);

            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit}" +
                $"<color={COLOR_BUFF}>{Hp}%</color>만큼 <color={COLOR_BUFF}>{statusName} 증가</color> 상태가 되었다.";
            AddLog(message);
        }
        else if (statusName == "강화")
        {
            string unitWithJosa = "{0}.이".FormatK(unitName);
            string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);

            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>" +
                $"{Hp}턴</color> 동안 <color={COLOR_BUFF}>{statusName}</color>되었다.";
            AddLog(message);
        }
        else if (statusName == "회복")
        {
            string unitWithJosa = "{0}.은".FormatK(unitName);
            string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);

            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} 체력이" +
                $" <color={COLOR_HEAL}>{Hp}</color>만큼 <color={COLOR_HEAL}>{statusName}</color> 되었다.";
            AddLog(message);
        }
    }

    /// <summary>
    /// 기타 특수 버프 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddOtherStatusLog(string unitName, float Hp)
    {
        AddOtherStatusLog(unitName, Hp, null);
    }

    /// <summary>
    /// 기타 특수 버프 로그 (ID 기반 색상)
    /// </summary>
    public void AddOtherStatusLog(string unitName, float Hp, string unitId)
    {
        string unitColor = GetUnitColor(unitId);

        if (unitName == "오즈 소서러")
        {
            string message = $"<color={unitColor}>{unitName}</color>의 <color={COLOR_BUFF}>속도</color>가" +
                $" 2턴 동안 <color={COLOR_BUFF}>{Hp}</color>으로 증가 되었다.";
            AddLog(message);
        }
        else if(unitName == "샤프 슈터")
        {
            string message = $"<color={unitColor}>{unitName}</color>가 <color={COLOR_BUFF}>{Hp}</color>턴" +
                $" 동안 <color={COLOR_BUFF}>공격 대상 증가</color> 상태가 되었다.";
            AddLog(message);
        }
    }

    /// <summary>
    /// 상태이상 회복 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddStatusRecoverLog(string unitName, string statusName)
    {
        AddStatusRecoverLog(unitName, statusName, null);
    }

    /// <summary>
    /// 상태이상 회복 로그 (ID 기반 색상)
    /// </summary>
    /// <param name="unitName">유닛 이름</param>
    /// <param name="statusName">상태이상 이름</param>
    /// <param name="unitId">유닛 ID</param>
    public void AddStatusRecoverLog(string unitName, string statusName, string unitId)
    {
        string unitWithJosa = "{0}.은".FormatK(unitName);
        string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);
        string unitColor = GetUnitColor(unitId);

        if(statusName != "신속" && statusName != "회피" && statusName != "공격" && statusName != "데미지" && statusName != "전체신" && statusName != "강화")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_DEBUFF}>{statusName}</color> 상태에서 회복했다.";
            AddLog(message);
        }
        else if(statusName == "회피")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>{statusName} 증가</color> 상태가 해제되었다.";
            AddLog(message);
        }
        else if(statusName == "공격")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>{statusName}력 증가</color> 상태가 해제되었다.";
            AddLog(message);
        }
        else if(statusName == "데미지")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_SHIELD}>받는 {statusName} 감소</color> 상태가 해제되었다.";
            AddLog(message);
        }
        else if (statusName == "전체신")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>{statusName}속 증가</color> 상태가 해제되었다.";
            AddLog(message);
        }
        else if (statusName == "강화")
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>{statusName}</color> 상태가 해제되었다.";
            AddLog(message);
        }
        else
        {
            string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_BUFF}>{statusName}</color> 상태가 해제되었다.";
            AddLog(message);
        }
    }

    /// <summary>
    /// 유닛 사망 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddDeathLog(string unitName)
    {
        AddDeathLog(unitName, null);
    }

    /// <summary>
    /// 유닛 사망 로그 (ID 기반 색상)
    /// </summary>
    /// <param name="unitName">유닛 이름</param>
    /// <param name="unitId">유닛 ID</param>
    public void AddDeathLog(string unitName, string unitId)
    {
        string unitWithJosa = "{0}.이".FormatK(unitName);
        string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);
        string unitColor = GetUnitColor(unitId);

        /*string message = $"<b><color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_DEATH}>쓰러졌다!</color></b>";*/ // 
        string message = $"<i><b><color={COLOR_DEATH}>{unitWithJosa} 쓰러졌다!</color></b></i>";
        AddLog(message);
    }

    /// <summary>
    /// 스킬 사용 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddSkillLog(string unitName, string skillName)
    {
        AddSkillLog(unitName, skillName, null);
    }

    /// <summary>
    /// 스킬 사용 로그 (ID 기반 색상)
    /// </summary>
    /// <param name="unitName">유닛 이름</param>
    /// <param name="skillName">스킬 이름</param>
    /// <param name="unitId">유닛 ID</param>
    public void AddSkillLog(string unitName, string skillName, string unitId)
    {
        // 유닛 이름 + 조사 처리
        string unitWithJosa = "{0}.이".FormatK(unitName);
        string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);  // "이"

        // 스킬 이름 + 조사 처리
        string skillWithJosa = "{0}.을".FormatK(skillName);  // "달려들기를"
        string josaOnly = skillWithJosa.Substring(skillName.Length);  // "를"

        // ID 기반 유닛 색상 결정
        string unitColor = GetUnitColor(unitId);

        // 스킬 이름만 색칠, 조사는 흰색 유지
        string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} <color={COLOR_SKILL}>{skillName}</color>{josaOnly} 사용했다!";
        AddLog(message);
    }

    /// <summary>
    /// 회복 로그 (오버로드 - 기존 호환성)
    /// </summary>
    public void AddHealLog(string unitName, float healAmount)
    {
        AddHealLog(unitName, healAmount, null);
    }

    /// <summary>
    /// 회복 로그 (ID 기반 색상)
    /// </summary>
    /// <param name="unitName">유닛 이름</param>
    /// <param name="healAmount">회복량</param>
    /// <param name="unitId">유닛 ID</param>
    public void AddHealLog(string unitName, float healAmount, string unitId)
    {
        int healInt = Mathf.FloorToInt(healAmount);  // 내림 처리
        string unitWithJosa = "{0}.은".FormatK(unitName);
        string josaOnlyUnit = unitWithJosa.Substring(unitName.Length);
        string unitColor = GetUnitColor(unitId);
        string message = $"<color={unitColor}>{unitName}</color>{josaOnlyUnit} 체력이 <color={COLOR_HEAL}>{healInt}</color>만큼 <color={COLOR_HEAL}>회복</color>되었다.";
        AddLog(message);
    }
    #endregion

    #region 유틸리티 메서드들
    /// <summary>
    /// 모든 로그 삭제 (풀로 반환)
    /// </summary>
    public void ClearLogs()
    {
        foreach (var log in activeLogObjects)
        {
            ReturnLogToPool(log);
        }
        activeLogObjects.Clear();
    }

    /// <summary>
    /// 스크롤을 맨 아래로 이동 (최신 로그 보이도록)
    /// </summary>
    private IEnumerator ScrollToBottom()
    {
        // 1프레임 대기 (Layout Rebuild 완료 대기)
        yield return null;

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    #endregion

    public void OnOffButton()
    {
        if(battleLogBg != null)
        {
            if(battleLogBg.activeInHierarchy)
                battleLogBg.SetActive(false);
            else
                battleLogBg.SetActive(true);
        }
    }
}
