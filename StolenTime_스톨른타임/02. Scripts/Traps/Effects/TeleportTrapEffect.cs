using UnityEngine;

public class TeleportTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private bool useFixedDestination;
    [SerializeField] private Vector2Int fixedDestination;

    public void Execute(TrapContext context)
    {
        Vector2Int destination = fixedDestination;
        if (!useFixedDestination && !TrapEffectUtility.TryFindRandomWalkableCell(out destination))
            return;

        if (!TrapEffectUtility.TryMoveEntity(context.Activator, destination))
        {
            GameEvents.OnTrapStubRequested?.Invoke(new TrapStubRequest(
                context.Position,
                "TeleportTrapEffect: activator cannot be moved yet.",
                context.Trap));
        }
    }
}
