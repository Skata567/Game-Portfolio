using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 최종 보정이 어느 행동 지연값에 적용될지 구분하는 표식입니다.
/// 예를 들어 신속의 반지는 MoveActionDelay를 줄이고, 타격의 반지는 AttackActionDelay를 줄입니다.
/// </summary>
public enum NYHFinalStatTarget
{
    /// <summary>
    /// 실제 이동 행동이 끝난 뒤 다음 행동까지 기다리는 시간입니다.
    /// 이동 애니메이션 보간 시간은 건드리지 않고, 턴/쿨타임 계산에 쓰이는 시간만 바꿉니다.
    /// </summary>
    MoveActionDelay,

    /// <summary>
    /// 근접 공격 쿨다운과 공격 행동 시간 비용에 같이 적용되는 시간입니다.
    /// 타격의 반지처럼 "공격을 더 자주 하게 만드는 효과"가 여기로 들어옵니다.
    /// </summary>
    AttackActionDelay
}

/// <summary>
/// 인스펙터에서 장비 프로필에 직접 넣는 최종 보정 설정값입니다.
/// 이 구조체는 "반지 효과가 몇 초를 더하거나 빼는지"만 들고 있고,
/// 실제 플레이어에게 적용하는 일은 NYHFinalStatModifierController가 담당합니다.
/// </summary>
[Serializable]
public struct NYHFinalStatModifier
{
    /// <summary>
    /// 이 보정이 이동 지연에 들어갈지, 공격 지연에 들어갈지 정합니다.
    /// </summary>
    public NYHFinalStatTarget target;

    /// <summary>
    /// 강화 수치와 상관없이 항상 적용되는 초 단위 보정값입니다.
    /// 예: 둔화 저주 갑옷이 이동을 0.2초 느리게 만들면 +0.2를 넣습니다.
    /// 예: 기본 0강 반지가 이미 0.1초 빠르게 만들면 -0.1을 넣습니다.
    /// </summary>
    public float flatSeconds;

    /// <summary>
    /// 강화 1단계마다 추가로 적용되는 초 단위 보정값입니다.
    /// 예: 신속의 반지가 강화 1마다 이동 지연을 0.05초 줄이면 -0.05를 넣습니다.
    /// 2강이면 flatSeconds + (-0.05 * 2)처럼 계산됩니다.
    /// </summary>
    public float flatSecondsPerEnhance;

    /// <summary>
    /// 현재 강화 수치를 받아서 실제 적용할 최종 초 값을 계산합니다.
    /// enhanceLevel이 음수로 들어오면 이상한 계산이 되므로 0 이상으로 막습니다.
    /// </summary>
    public float Evaluate(int enhanceLevel)
    {
        return flatSeconds + flatSecondsPerEnhance * Mathf.Max(0, enhanceLevel);
    }
}

/// <summary>
/// 게임 실행 중 실제로 적용되는 최종 보정값입니다.
/// NYHFinalStatModifier는 인스펙터용 원본 설정이고,
/// 이 Runtime 구조체는 "강화 수치까지 계산이 끝난 결과"를 들고 있습니다.
/// </summary>
public readonly struct NYHFinalStatModifierRuntime
{
    /// <summary>
    /// 계산 완료된 보정이 어느 행동 지연값에 적용될지 나타냅니다.
    /// </summary>
    public readonly NYHFinalStatTarget Target;

    /// <summary>
    /// 강화 수치까지 반영된 최종 초 단위 보정값입니다.
    /// -0.05면 0.05초 빨라지고, +0.2면 0.2초 느려집니다.
    /// </summary>
    public readonly float FlatSeconds;

    /// <summary>
    /// 이미 계산이 끝난 보정값을 런타임용 데이터로 포장합니다.
    /// 장비 컨트롤러, 특성 컨트롤러, 상태이상 컨트롤러 등이 이 생성자를 써서 등록할 수 있습니다.
    /// </summary>
    public NYHFinalStatModifierRuntime(NYHFinalStatTarget target, float flatSeconds)
    {
        Target = target;
        FlatSeconds = flatSeconds;
    }
}

/// <summary>
/// 플레이어의 "최종 행동 지연 시간"을 한곳에서 관리하는 컴포넌트입니다.
///
/// NYHPlayerStats는 Strength, AttackSpeed, MoveSpeed처럼 정수 스탯만 관리합니다.
/// 그런데 기획서에는 "쿨다운 -0.2초", "강화당 -0.05초"처럼 소수 초 단위 보정도 있습니다.
/// 그런 값까지 NYHPlayerStats에 억지로 넣으면 정수 스탯과 최종 시간 보정이 섞여서 나중에 헷갈립니다.
///
/// 그래서 이 컴포넌트는 반지, 마법부여, 특성, 포션/상태가 같은 방식으로
/// "최종 계산이 끝난 시간에 몇 초를 더하거나 뺄지"만 등록하게 해주는 통로 역할을 합니다.
/// </summary>
public class NYHFinalStatModifierController : MonoBehaviour
{
    /// <summary>
    /// 모든 보정이 끝난 뒤에도 행동 지연 시간이 이 값 아래로 내려가지 않게 막습니다.
    /// 반지 여러 개, 특성, 마법부여가 겹쳐도 0초 행동이나 음수 쿨타임이 되면 게임 흐름이 깨지기 때문입니다.
    /// </summary>
    private const float MinFinalActionDelay = 0.01f;

    /// <summary>
    /// source별로 등록된 보정 목록입니다.
    /// source는 "누가 이 보정을 등록했는가"를 구분하는 열쇠입니다.
    /// 예: 장비 컨트롤러가 등록한 보정, 특성 컨트롤러가 등록한 보정, 상태이상이 등록한 보정을 서로 따로 보관합니다.
    /// 같은 source가 다시 SetModifiers를 호출하면 기존 보정을 교체해서 같은 효과가 두 번 누적되는 사고를 막습니다.
    /// </summary>
    private readonly Dictionary<object, List<NYHFinalStatModifierRuntime>> _modifiersBySource = new();

    /// <summary>
    /// 특정 source가 가진 최종 보정 목록을 새 값으로 등록합니다.
    /// 장비 쪽에서는 장착/해제/강화가 바뀔 때마다 장착 장비 전체를 다시 계산한 뒤 이 함수에 넣습니다.
    /// 같은 source로 다시 호출하면 이전 값이 덮어써지므로, 강화 주문서를 여러 번 써도 예전 수치가 남지 않습니다.
    /// </summary>
    /// <param name="source">
    /// 보정을 등록하는 주체입니다.
    /// 장비 시스템은 NYHEquipmentController 자신을 source로 쓰고,
    /// 나중에 특성 시스템을 만들면 TraitController 자신을 source로 쓰면 됩니다.
    /// </param>
    /// <param name="modifiers">
    /// 실제 적용할 보정 목록입니다.
    /// null이 들어오거나, 전부 0초 보정이면 해당 source의 보정을 삭제합니다.
    /// </param>
    public void SetModifiers(object source, IEnumerable<NYHFinalStatModifierRuntime> modifiers)
    {
        if (source == null)
        {
            Debug.LogWarning("NYHFinalStatModifierController.SetModifiers source is null.");
            return;
        }

        if (modifiers == null)
        {
            ClearModifiers(source);
            return;
        }

        List<NYHFinalStatModifierRuntime> copiedModifiers = new();
        foreach (NYHFinalStatModifierRuntime modifier in modifiers)
        {
            if (Mathf.Approximately(modifier.FlatSeconds, 0f))
            {
                continue;
            }

            copiedModifiers.Add(modifier);
        }

        if (copiedModifiers.Count == 0)
        {
            ClearModifiers(source);
            return;
        }

        // 같은 장비/특성/상태 source가 다시 등록되면 이전 값을 교체해서 중복 적용을 막는다.
        _modifiersBySource[source] = copiedModifiers;
    }

    /// <summary>
    /// 특정 source가 등록했던 모든 보정을 제거합니다.
    /// 예를 들어 장비 컨트롤러가 꺼지거나, 상태이상이 끝났거나, 특성이 비활성화되면 이 함수를 호출하면 됩니다.
    /// </summary>
    public void ClearModifiers(object source)
    {
        if (source == null)
        {
            return;
        }

        _modifiersBySource.Remove(source);
    }

    /// <summary>
    /// 이동 행동 지연값에 등록된 모든 MoveActionDelay 보정을 적용합니다.
    /// GridPlayerMovement에서 기본 이동 시간과 상태이상 보정까지 계산한 뒤 마지막에 이 함수를 호출합니다.
    /// </summary>
    /// <param name="value">이미 계산된 이동 행동 지연 시간입니다.</param>
    /// <returns>신속의 반지, 둔화 저주 같은 최종 보정까지 적용된 이동 행동 지연 시간입니다.</returns>
    public float ModifyMoveActionDelay(float value)
    {
        return ModifyActionDelay(NYHFinalStatTarget.MoveActionDelay, value);
    }

    /// <summary>
    /// 공격 행동 지연값에 등록된 모든 AttackActionDelay 보정을 적용합니다.
    /// GridPlayerCombat에서 공격 속도와 상태이상 보정까지 계산한 뒤 마지막에 이 함수를 호출합니다.
    /// 공격 쿨다운과 공격 행동 시간 비용 양쪽 모두 이 함수를 사용합니다.
    /// </summary>
    /// <param name="value">이미 계산된 공격 쿨다운 또는 공격 행동 시간 비용입니다.</param>
    /// <returns>타격의 반지 같은 최종 보정까지 적용된 공격 지연 시간입니다.</returns>
    public float ModifyAttackActionDelay(float value)
    {
        return ModifyActionDelay(NYHFinalStatTarget.AttackActionDelay, value);
    }

    /// <summary>
    /// target과 일치하는 보정만 골라서 value에 더합니다.
    /// 예를 들어 이동 시간을 계산 중이면 MoveActionDelay만 더하고 AttackActionDelay는 무시합니다.
    /// 모든 보정을 더한 뒤 마지막에 최소값 clamp를 걸어 0초 이하 행동을 막습니다.
    /// </summary>
    private float ModifyActionDelay(NYHFinalStatTarget target, float value)
    {
        float result = value;

        foreach (List<NYHFinalStatModifierRuntime> modifiers in _modifiersBySource.Values)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                NYHFinalStatModifierRuntime modifier = modifiers[i];
                if (modifier.Target == target)
                {
                    result += modifier.FlatSeconds;
                }
            }
        }

        // 최종 보정이 과하게 겹쳐도 0초 이하 행동이 되지 않도록 마지막 단계에서만 막는다.
        return Mathf.Max(MinFinalActionDelay, result);
    }
}
