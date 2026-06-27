using System.Collections.Generic;
using UnityEngine;

public class TrapSpawnController : MonoBehaviour
{
    [SerializeField] private bool logOnly = true;

    private void OnEnable()
    {
        GameEvents.OnTrapSpawnRequested += OnTrapSpawnRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnTrapSpawnRequested -= OnTrapSpawnRequested;
    }

    private void OnTrapSpawnRequested(TrapSpawnRequest request)
    {
        List<Vector2Int> positions = PickSpawnPositions(request.Origin, request.Radius, request.Count);
        if (logOnly)
            Debug.Log($"[Trap] Spawn request {request.SpawnType}: {positions.Count}/{request.Count} positions reserved near {request.Origin}");
    }

    private List<Vector2Int> PickSpawnPositions(Vector2Int origin, int radius, int count)
    {
        var candidates = new List<Vector2Int>();
        int searchRadius = Mathf.Max(1, radius);

        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(origin, searchRadius))
        {
            if (TurnManager.Instance != null && TurnManager.Instance.GetEntityAt(cell) != null)
                continue;
            candidates.Add(cell);
        }

        var result = new List<Vector2Int>();
        while (result.Count < count && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return result;
    }
}
