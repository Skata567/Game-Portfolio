using UnityEngine;

public class AreaTerrainTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private TrapTerrainType terrainType;
    [SerializeField] private int radiusOverride = -1;

    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
        GameEvents.OnTrapTerrainRequested?.Invoke(new TrapTerrainRequest(
            context.Position,
            radius,
            context.Data.durationTurns,
            terrainType,
            context.Trap));
    }
}
