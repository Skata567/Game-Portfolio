using System;
using UnityEngine;

/// <summary>
/// 스킬의 기본 정보를 담는 데이터 구조체
/// - 각 스킬의 이름, 설명, 비용 등의 정보를 저장
/// - 스킬 타입별로 구분하여 관리
///
/// 사용 예시:
/// SkillInfo ainSkill1 = new SkillInfo("아인족 강화 1단계", "아인족의 공격력을 5% 증가시킵니다", 30);
/// </summary>
[System.Serializable]
public struct SkillInfo
{
    [Header("기본 정보")]
    public string skillName;        // 스킬 이름
    public string description;      // 스킬 설명
    public int cost;               // 강화 비용
    public string resourceType;    // 필요한 자원 타입 ("다이아몬드", "미스릴" 등)

    [Header("스킬 속성")]
    public int skillLevel;         // 스킬 단계 (1~5)
    public SkillType type;         // 스킬 타입

    /// <summary>
    /// SkillInfo 생성자
    /// </summary>
    public SkillInfo(string name, string desc, int skillCost, int level, SkillType skillType, string resource = "다이아몬드")
    {
        skillName = name;
        description = desc;
        cost = skillCost;
        skillLevel = level;
        type = skillType;
        resourceType = resource;
    }
}

/// <summary>
/// 스킬 타입을 정의하는 열거형
/// - JSON의 category 필드와 일치하도록 설정
/// - Ain(고블린/오크), Suin(수인), UnDead(언데드), UnHoly(언홀리), Evil(이블)
/// </summary>
public enum SkillType
{
    Ain,        // 아인족 (고블린, 오크)
    Suin,       // 수인족 (리저드맨, 머맨, 수인)
    UnDead,     // 언데드 (좀비, 스켈레톤)
    UnHoly,     // 언홀리 (악마, 타락자)
    Evil,       // 이블 (웨어울프, 다크엘프, 뱀파이어)
    Special,    // 스페셜 몬스터
    Population, // 인구 (배치 수)
    Rune        // 룬 스킬
}

/// <summary>
/// 스킬 정보를 관리하는 데이터 클래스
/// - 모든 스킬의 정보를 중앙에서 관리
/// - 스킬 타입별로 데이터 분류하여 저장
/// - ScriptableObject로 에디터에서 쉽게 수정 가능
///
/// 사용 방법:
/// 1. Project 창에서 우클릭 후 Create > Game Data > Skill Database
/// 2. Inspector에서 스킬 정보들을 입력
/// 3. SkillUpgrade 스크립트에서 참조하여 사용
/// </summary>
[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Game Data/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    [Header("아인족 스킬 (고블린, 오크)")]
    public SkillInfo[] ainSkills = new SkillInfo[5]
    {
        new SkillInfo("아인족 강화 1단계", "아인족 유닛 HP +10%", 30, 1, SkillType.Ain),
        new SkillInfo("아인족 강화 2단계", "아인족 유닛 공격력 +10%", 60, 2, SkillType.Ain),
        new SkillInfo("아인족 강화 3단계", "아인족 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.Ain),
        new SkillInfo("아인족 강화 4단계", "아인족 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.Ain),
        new SkillInfo("아인족 강화 5단계", "아인족 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.Ain)
    };

    [Header("수인족 스킬 (리저드맨, 머맨, 수인)")]
    public SkillInfo[] suinSkills = new SkillInfo[5]
    {
        new SkillInfo("수인족 강화 1단계", "수인족 유닛 HP +10%", 30, 1, SkillType.Suin),
        new SkillInfo("수인족 강화 2단계", "수인족 유닛 공격력 +10%", 60, 2, SkillType.Suin),
        new SkillInfo("수인족 강화 3단계", "수인족 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.Suin),
        new SkillInfo("수인족 강화 4단계", "수인족 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.Suin),
        new SkillInfo("수인족 강화 5단계", "수인족 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.Suin)
    };

    [Header("언데드 스킬 (좀비, 스켈레톤)")]
    public SkillInfo[] undeadSkills = new SkillInfo[5]
    {
        new SkillInfo("언데드 강화 1단계", "언데드 유닛 HP +10%", 30, 1, SkillType.UnDead),
        new SkillInfo("언데드 강화 2단계", "언데드 유닛 공격력 +10%", 60, 2, SkillType.UnDead),
        new SkillInfo("언데드 강화 3단계", "언데드 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.UnDead),
        new SkillInfo("언데드 강화 4단계", "언데드 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.UnDead),
        new SkillInfo("언데드 강화 5단계", "언데드 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.UnDead)
    };

    [Header("타락 스킬 (악마, 타락자)")]
    public SkillInfo[] unholySkills = new SkillInfo[5]
    {
        new SkillInfo("타락 강화 1단계", "타락 유닛 HP +10%", 30, 1, SkillType.UnHoly),
        new SkillInfo("타락 강화 2단계", "타락 유닛 공격력 +10%", 60, 2, SkillType.UnHoly),
        new SkillInfo("타락 강화 3단계", "타락 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.UnHoly),
        new SkillInfo("타락 강화 4단계", "타락 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.UnHoly),
        new SkillInfo("타락 강화 5단계", "타락 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.UnHoly)
    };

    [Header("마족 스킬 (웨어울프, 다크엘프, 뱀파이어)")]
    public SkillInfo[] evilSkills = new SkillInfo[5]
    {
        new SkillInfo("마족 강화 1단계", "마족 유닛 HP +10%", 30, 1, SkillType.Evil),
        new SkillInfo("마족 강화 2단계", "마족 유닛 공격력 +10%", 60, 2, SkillType.Evil),
        new SkillInfo("마족 강화 3단계", "마족 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.Evil),
        new SkillInfo("마족 강화 4단계", "마족 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.Evil),
        new SkillInfo("마족 강화 5단계", "마족 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.Evil)
    };

    [Header("스페셜 스킬")]
    public SkillInfo[] specialSkills = new SkillInfo[3]
    {
        new SkillInfo("스페셜 강화 1단계", "스페셜 유닛 HP +20%, 공격력 +20%", 30, 1, SkillType.Special, "미스릴"),
        new SkillInfo("스페셜 강화 2단계", "스페셜 유닛 강화 효과 (추후 구현)", 60, 2, SkillType.Special, "미스릴"),
        new SkillInfo("스페셜 강화 3단계", "스페셜 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.Special, "미스릴")
    };

    [Header("인구 스킬")]
    public SkillInfo[] populationSkills = new SkillInfo[5]
    {
        new SkillInfo("코스트 증가 1단계", "최대 배치 수 +20", 30, 1, SkillType.Population),
        new SkillInfo("코스트 증가 2단계", "최대 배치 수 +20", 60, 2, SkillType.Population),
        new SkillInfo("코스트 증가 3단계", "최대 배치 수 +20", 90, 3, SkillType.Population),
        new SkillInfo("코스트 증가 4단계", "최대 배치 수 +20", 120, 4, SkillType.Population),
        new SkillInfo("코스트 증가 5단계", "최대 배치 수 +20", 250, 5, SkillType.Population)
    };

    [Header("룬 스킬")]
    public SkillInfo[] runeSkills = new SkillInfo[5]
    {
        new SkillInfo("룬 스킬 1단계", "룬 효과 (추후 구현)", 30, 1, SkillType.Rune),
        new SkillInfo("룬 스킬 2단계", "룬 효과 (추후 구현)", 60, 2, SkillType.Rune),
        new SkillInfo("룬 스킬 3단계", "룬 효과 (추후 구현)", 90, 3, SkillType.Rune),
        new SkillInfo("룬 스킬 4단계", "룬 효과 (추후 구현)", 120, 4, SkillType.Rune),
        new SkillInfo("룬 스킬 5단계", "룬 효과 (추후 구현)", 150, 5, SkillType.Rune)
    };

    /// <summary>
    /// 특정 스킬 타입과 레벨에 해당하는 스킬 정보를 반환
    /// </summary>
    /// <param name="type">스킬 타입</param>
    /// <param name="level">스킬 단계 (0-based 인덱스)</param>
    /// <returns>해당하는 스킬 정보, 없으면 기본값</returns>
    public SkillInfo GetSkillInfo(SkillType type, int level)
    {
        try
        {
            switch (type)
            {
                case SkillType.Ain:
                    return level < ainSkills.Length ? ainSkills[level] : default;
                case SkillType.Suin:
                    return level < suinSkills.Length ? suinSkills[level] : default;
                case SkillType.UnDead:
                    return level < undeadSkills.Length ? undeadSkills[level] : default;
                case SkillType.UnHoly:
                    return level < unholySkills.Length ? unholySkills[level] : default;
                case SkillType.Evil:
                    return level < evilSkills.Length ? evilSkills[level] : default;
                case SkillType.Special:
                    return level < specialSkills.Length ? specialSkills[level] : default;
                case SkillType.Population:
                    return level < populationSkills.Length ? populationSkills[level] : default;
                case SkillType.Rune:
                    return level < runeSkills.Length ? runeSkills[level] : default;
                default:
                    Debug.LogError($"[SkillDatabase] 알 수 없는 스킬 타입: {type}");
                    return default;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SkillDatabase] 스킬 정보 조회 중 오류: {e.Message}");
            return default;
        }
    }

    /// <summary>
    /// 특정 스킬 타입의 모든 스킬 정보를 반환
    /// </summary>
    /// <param name="type">스킬 타입</param>
    /// <returns>해당 타입의 모든 스킬 정보 배열</returns>
    public SkillInfo[] GetAllSkillsOfType(SkillType type)
    {
        switch (type)
        {
            case SkillType.Ain:
                return ainSkills;
            case SkillType.Suin:
                return suinSkills;
            case SkillType.UnDead:
                return undeadSkills;
            case SkillType.UnHoly:
                return unholySkills;
            case SkillType.Evil:
                return evilSkills;
            case SkillType.Special:
                return specialSkills;
            case SkillType.Population:
                return populationSkills;
            case SkillType.Rune:
                return runeSkills;
            default:
                Debug.LogError($"[SkillDatabase] 알 수 없는 스킬 타입: {type}");
                return new SkillInfo[0];
        }
    }

    /// <summary>
    /// 스킬 데이터를 최신 코드 값으로 강제 초기화 (디버그용)
    /// Unity Inspector에서 우클릭 → "스킬 데이터 강제 초기화" 실행
    /// </summary>
    [ContextMenu("스킬 데이터 강제 초기화")]
    public void ForceResetSkillData()
    {
        // 아인족 스킬 초기화
        ainSkills = new SkillInfo[5]
        {
            new SkillInfo("아인족 강화 1단계", "아인족 유닛 HP +10%", 30, 1, SkillType.Ain),
            new SkillInfo("아인족 강화 2단계", "아인족 유닛 공격력 +10%", 60, 2, SkillType.Ain),
            new SkillInfo("아인족 강화 3단계", "아인족 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.Ain),
            new SkillInfo("아인족 강화 4단계", "아인족 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.Ain),
            new SkillInfo("아인족 강화 5단계", "아인족 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.Ain)
        };

        // 수인족 스킬 초기화
        suinSkills = new SkillInfo[5]
        {
            new SkillInfo("수인족 강화 1단계", "수인족 유닛 HP +10%", 30, 1, SkillType.Suin),
            new SkillInfo("수인족 강화 2단계", "수인족 유닛 공격력 +10%", 60, 2, SkillType.Suin),
            new SkillInfo("수인족 강화 3단계", "수인족 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.Suin),
            new SkillInfo("수인족 강화 4단계", "수인족 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.Suin),
            new SkillInfo("수인족 강화 5단계", "수인족 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.Suin)
        };

        // 언데드 스킬 초기화
        undeadSkills = new SkillInfo[5]
        {
            new SkillInfo("언데드 강화 1단계", "언데드 유닛 HP +10%", 30, 1, SkillType.UnDead),
            new SkillInfo("언데드 강화 2단계", "언데드 유닛 공격력 +10%", 60, 2, SkillType.UnDead),
            new SkillInfo("언데드 강화 3단계", "언데드 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.UnDead),
            new SkillInfo("언데드 강화 4단계", "언데드 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.UnDead),
            new SkillInfo("언데드 강화 5단계", "언데드 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.UnDead)
        };

        // 언홀리 스킬 초기화
        unholySkills = new SkillInfo[5]
        {
            new SkillInfo("언홀리 강화 1단계", "언홀리 유닛 HP +10%", 30, 1, SkillType.UnHoly),
            new SkillInfo("언홀리 강화 2단계", "언홀리 유닛 공격력 +10%", 60, 2, SkillType.UnHoly),
            new SkillInfo("언홀리 강화 3단계", "언홀리 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.UnHoly),
            new SkillInfo("언홀리 강화 4단계", "언홀리 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.UnHoly),
            new SkillInfo("언홀리 강화 5단계", "언홀리 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.UnHoly)
        };

        // 이블 스킬 초기화
        evilSkills = new SkillInfo[5]
        {
            new SkillInfo("이블 강화 1단계", "이블 유닛 HP +10%", 30, 1, SkillType.Evil),
            new SkillInfo("이블 강화 2단계", "이블 유닛 공격력 +10%", 60, 2, SkillType.Evil),
            new SkillInfo("이블 강화 3단계", "이블 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.Evil),
            new SkillInfo("이블 강화 4단계", "이블 유닛 강화 효과 (추후 구현)", 120, 4, SkillType.Evil),
            new SkillInfo("이블 강화 5단계", "이블 유닛 강화 효과 (추후 구현)", 150, 5, SkillType.Evil)
        };

        // 스페셜 스킬 초기화
        specialSkills = new SkillInfo[3]
        {
            new SkillInfo("스페셜 강화 1단계", "스페셜 유닛 HP +20%, 공격력 +20%", 30, 1, SkillType.Special, "미스릴"),
            new SkillInfo("스페셜 강화 2단계", "스페셜 유닛 강화 효과 (추후 구현)", 60, 2, SkillType.Special, "미스릴"),
            new SkillInfo("스페셜 강화 3단계", "스페셜 유닛 강화 효과 (추후 구현)", 90, 3, SkillType.Special, "미스릴")
        };

        Debug.Log("[SkillDatabase] 스킬 데이터 강제 초기화 완료!");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}
