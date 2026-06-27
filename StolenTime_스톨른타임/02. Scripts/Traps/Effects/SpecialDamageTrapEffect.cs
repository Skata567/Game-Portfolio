using UnityEngine;

public class SpecialDamageTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private bool targetNearest = true;
    [SerializeField] private int maxRange;
    [SerializeField] private bool emitItemDestructionStub;

    public void Execute(TrapContext context)
    {
        IGridEntity target = targetNearest
            ? TrapEffectUtility.FindNearestEntity(context.Position, context.Activator, maxRange)
            : context.Activator;

        IDamageable damageable = TrapEffectUtility.AsDamageable(target);
        if (damageable == null || damageable.IsDead) return;

        damageable.TakeDamage(context.Data.CalculateDamage(damageable));

        if (emitItemDestructionStub && target is GridPlayer)
        {
            GameEvents.OnTrapStubRequested?.Invoke(new TrapStubRequest(
                context.Position,
                "Item destruction requested by trap, but equipment/inventory destruction is not connected yet.",
                context.Trap));
        }
    }
}
