using UnityEngine;


public class TrapDebugEventLogger : MonoBehaviour
{
    [SerializeField] private bool logStatusRequests = true;
    [SerializeField] private bool logTerrainRequests = true;
    [SerializeField] private bool logSpawnRequests = true;
    [SerializeField] private bool logStubRequests = true;

    private void OnEnable()
    {
        GameEvents.OnStatusRequested += OnStatusRequested;
        GameEvents.OnTrapTerrainRequested += OnTrapTerrainRequested;
        GameEvents.OnTrapSpawnRequested += OnTrapSpawnRequested;
        GameEvents.OnTrapStubRequested += OnTrapStubRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnStatusRequested -= OnStatusRequested;
        GameEvents.OnTrapTerrainRequested -= OnTrapTerrainRequested;
        GameEvents.OnTrapSpawnRequested -= OnTrapSpawnRequested;
        GameEvents.OnTrapStubRequested -= OnTrapStubRequested;
    }

    private void OnStatusRequested(StatusRequest request)
    {
        if (!logStatusRequests) return;
        Debug.Log($"[Trap] Status requested: {request.StatusType} at {request.TargetPosition}, duration={request.DurationTurns}, power={request.Power}");
    }

    private void OnTrapTerrainRequested(TrapTerrainRequest request)
    {
        if (!logTerrainRequests) return;
        Debug.Log($"[Trap] Terrain requested: {request.TerrainType} at {request.Origin}, radius={request.Radius}, duration={request.DurationTurns}");
    }

    private void OnTrapSpawnRequested(TrapSpawnRequest request)
    {
        if (!logSpawnRequests) return;
        Debug.Log($"[Trap] Spawn requested: {request.SpawnType} at {request.Origin}, count={request.Count}, radius={request.Radius}");
    }

    private void OnTrapStubRequested(TrapStubRequest request)
    {
        if (!logStubRequests) return;
        Debug.Log($"[Trap] Stub requested at {request.Origin}: {request.Message}");
    }
}
