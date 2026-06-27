using UnityEngine;

public class SpawnTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private TrapSpawnType spawnType;
    [SerializeField, Min(0)] private int countOverride;

    public void Execute(TrapContext context)
    {
        int count = countOverride > 0 ? countOverride : Mathf.Max(1, context.Data.statusPower);
        GameEvents.OnTrapSpawnRequested?.Invoke(new TrapSpawnRequest(
            context.Position,
            spawnType,
            count,
            context.Data.radius,
            context.Trap));
    }
}
