using UnityEngine;

/// <summary>
/// 피해를 준 뒤 상태이상을 함께 적용하는 복합 트랩 효과입니다.
/// 상태이상의 power는 TrapData.statusPower가 있으면 그 값을 쓰고, 없으면 실제로 준 피해량을 사용합니다.
/// </summary>
public class DamageAndStatusTrapEffect : MonoBehaviour, ITrapEffect
{
    [Header("상태이상")]
    [Tooltip("피해 적용 후 대상에게 부여할 상태이상 목록입니다. 여러 개를 넣으면 순서대로 모두 요청합니다.")]
    [SerializeField] private StatusType[] statuses;

    [Header("적용 범위")]
    [Tooltip("켜면 트랩 위치 기준 반경 안의 대상들에게 적용하고, 끄면 발동 대상에게만 적용합니다.")]
    [SerializeField] private bool affectArea;

    [Tooltip("효과 반경입니다. -1이면 TrapData.radius를 사용하고, 0 이상이면 이 값으로 덮어씁니다.")]
    [SerializeField] private int radiusOverride = -1;

    /// <summary>
    /// 단일 대상 또는 범위 대상에게 피해와 상태이상을 적용합니다.
    /// </summary>
    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
        if (!affectArea || radius <= 0)
        {
            Apply(context.Activator, context);
            return;
        }

        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
            Apply(TrapEffectUtility.GetEntityAt(cell, context.Activator), context);
    }

    /// <summary>
    /// 한 대상에게 피해를 먼저 적용한 뒤 상태이상 요청을 보냅니다.
    /// 피해량 기반 상태 강도를 쓰기 위해 피해 전후 HP 차이를 계산합니다.
    /// </summary>
    private void Apply(IGridEntity entity, TrapContext context)
    {
        if (entity == null) return;

        IDamageable damageable = TrapEffectUtility.AsDamageable(entity);
        int beforeHp = damageable != null ? damageable.CurrentHp : 0;
        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(context.Data.CalculateDamage(damageable));

        int dealtDamage = damageable != null ? Mathf.Max(0, beforeHp - damageable.CurrentHp) : 0;
        int power = context.Data.statusPower > 0 ? context.Data.statusPower : dealtDamage;
        for (int i = 0; i < statuses.Length; i++)
        {
            TrapEffectUtility.RequestStatus(
                entity,
                entity.GridPosition,
                statuses[i],
                context.Data.durationTurns,
                power,
                context.Trap);
        }
    }
}
