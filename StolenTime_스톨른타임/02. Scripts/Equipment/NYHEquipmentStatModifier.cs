using System;

/// <summary>
/// 장비가 플레이어에게 부여하는 고정 정수 스탯 보정치를 정의하는 구조체입니다.
/// </summary>
[Serializable]
public struct NYHEquipmentStatModifier
{
    /// <summary>증가시키거나 감소시킬 스탯의 종류 (예: 체력, 마력, 공격력 등)</summary>
    public NYHStatType statType;
    
    /// <summary>변화량 (양수면 증가, 음수면 감소)</summary>
    public int amount;
}

/// <summary>
/// 장비의 강화 수치에 비례해서 증가하는 스탯 보정치를 정의하는 구조체입니다.
/// </summary>
[Serializable]
public struct NYHEquipmentScalingStatModifier
{
    /// <summary>증가시키거나 감소시킬 스탯의 종류</summary>
    public NYHStatType statType;
    
    /// <summary>강화 레벨이 0일 때의 기본 스탯 변화량</summary>
    public int baseAmount;
    
    /// <summary>강화 레벨이 1 오를 때마다 추가로 증가하는 스탯 변화량</summary>
    public int amountPerEnhance;

    /// <summary>
    /// 현재 강화 레벨을 받아 최종적으로 적용할 스탯 변화량을 계산합니다.
    /// 강화 레벨이 음수(저주 등)일 경우, 최소 0으로 처리하여 기본 수치 이하로 떨어지지 않게 방어합니다.
    /// </summary>
    public int Evaluate(int enhanceLevel)
    {
        return baseAmount + amountPerEnhance * Math.Max(0, enhanceLevel);
    }
}
