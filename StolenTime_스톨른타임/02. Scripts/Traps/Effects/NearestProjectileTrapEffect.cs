using UnityEngine;

public class NearestProjectileTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private int maxRange;
    [SerializeField] private StatusType[] statuses;

    public void Execute(TrapContext context)
    {
        IGridEntity target = TrapEffectUtility.FindNearestEntity(context.Position, context.Activator, maxRange);
        IDamageable damageable = TrapEffectUtility.AsDamageable(target);
        if (damageable == null || damageable.IsDead) return;

        damageable.TakeDamage(context.Data.CalculateDamage(damageable));

        for (int i = 0; i < statuses.Length; i++)
        {
            TrapEffectUtility.RequestStatus(
                target,
                target.GridPosition,
                statuses[i],
                context.Data.durationTurns,
                context.Data.statusPower,
                context.Trap);
        }
    }
}
