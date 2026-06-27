using UnityEngine;

/// <summary>
/// 전투 숫자가 어떤 의미인지 구분합니다.
/// 색상과 크기 선택은 CombatFloatingTextSpawner가 이 값을 보고 결정합니다.
/// </summary>
public enum CombatTextType
{
    Damage,
    CriticalDamage,
    Miss,
    Heal,
    Gold
}

/// <summary>
/// 전투 로직에서 UI 쪽으로 넘기는 표시 요청 데이터입니다.
/// GameEvents.OnCombatTextRequested를 통해 전달되며, UI는 이 데이터만 보고 숫자를 생성합니다.
/// </summary>
public readonly struct CombatTextRequest
{
    public readonly Vector3 WorldPosition;
    public readonly string Text;
    public readonly int Damage;
    public readonly CombatTextType Type;

    public CombatTextRequest(Vector3 worldPosition, string text, int damage, CombatTextType type)
    {
        WorldPosition = worldPosition;
        Text = text;
        Damage = damage;
        Type = type;
    }
}

/// <summary>
/// 보스 피격 표시에서 치명타 기준을 잠깐 전달하기 위한 임시 컨텍스트입니다.
/// 보스 TakeDamage 쪽은 플레이어의 공격 범위를 직접 모르기 때문에,
/// 플레이어 공격 코드가 TakeDamage를 호출하는 짧은 순간에만 최대 피해값을 넣어 둡니다.
/// </summary>
public static class CombatTextCriticalContext
{
    public static int CurrentMaxDamage { get; private set; }

    public static void SetCurrentMaxDamage(int maxDamage)
    {
        CurrentMaxDamage = Mathf.Max(0, maxDamage);
    }

    public static void Clear()
    {
        CurrentMaxDamage = 0;
    }
}
