using System.Collections.Generic;
using UnityEngine;

public static class TrapEffectUtility
{
    public static IEnumerable<Vector2Int> CellsInRadius(Vector2Int origin, int radius)
    {
        radius = Mathf.Max(0, radius);
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                var cell = new Vector2Int(origin.x + x, origin.y + y);
                if (GridSystem.Instance != null && GridSystem.Instance.IsWalkable(cell))
                    yield return cell;
            }
        }
    }

    public static IGridEntity GetEntityAt(Vector2Int cell, IGridEntity knownActivator = null)
    {
        // 함정/장판을 밟은 주체를 이미 알고 있다면 가장 먼저 사용합니다.
        // TurnManager/GridSystem 등록 타이밍 차이 때문에 같은 프레임에 못 찾는 일을 줄입니다.
        if (knownActivator != null && knownActivator.GridPosition == cell)
            return knownActivator;

        // 적과 대형 보스는 TurnManager에 등록되어 있습니다.
        // 특히 IMultiCellGridEntity 보스는 TurnManager.GetEntityAt이 OccupiedCells까지 검사합니다.
        IGridEntity turnEntity = TurnManager.Instance != null ? TurnManager.Instance.GetEntityAt(cell) : null;
        if (turnEntity != null)
            return turnEntity;

        // 플레이어는 TurnManager 목록이 아니라 GridSystem 점유 정보에 등록되어 있으므로 마지막에 여기서 찾습니다.
        // 이 fallback이 없으면 자기 발밑에 불/얼음 포션을 던졌을 때 즉시 상태이상이 걸리지 않습니다.
        return GridSystem.Instance != null ? GridSystem.Instance.GetEntityAt(cell) : null;
    }

    public static IDamageable AsDamageable(IGridEntity entity)
    {
        return entity as IDamageable;
    }

    public static void DealDamage(IGridEntity entity, TrapData data)
    {
        IDamageable damageable = AsDamageable(entity);
        if (damageable == null || damageable.IsDead || data == null) return;
        damageable.TakeDamage(data.CalculateDamage(damageable));
    }

    public static void RequestStatus(IGridEntity entity, Vector2Int targetPosition, StatusType statusType, int durationTurns, int power, GridTrap sourceTrap)
    {
        var request = new StatusRequest(entity, targetPosition, statusType, durationTurns, power, sourceTrap);
        GameEvents.OnStatusRequested?.Invoke(request);

        if (entity is Component component)
        {
            StatusController controller = component.GetComponent<StatusController>();
            if (controller == null)
                controller = component.gameObject.AddComponent<StatusController>();
            controller.ApplyStatus(request);
        }
    }

    public static bool TryMoveEntity(IGridEntity entity, Vector2Int destination)
    {
        if (entity == null || GridSystem.Instance == null || !GridSystem.Instance.IsWalkable(destination))
            return false;

        if (entity is EntityBase entityBase)
        {
            entityBase.SetGridPosition(destination);
            if (entity is GridPlayer && VisionSystem.Instance != null)
                VisionSystem.Instance.UpdateVision(destination);
            return true;
        }

        return false;
    }

    public static bool TryFindRandomWalkableCell(out Vector2Int cell)
    {
        cell = default;
        if (GridSystem.Instance == null) return false;

        var candidates = new List<Vector2Int>();
        GridPlayer player = Object.FindFirstObjectByType<GridPlayer>();
        foreach (Vector2Int candidate in GridSystem.Instance.GetAllWalkableCells())
        {
            if (TurnManager.Instance != null && TurnManager.Instance.GetEntityAt(candidate) != null)
                continue;
            if (player != null && player.GridPosition == candidate)
                continue;
            candidates.Add(candidate);
        }

        if (candidates.Count == 0) return false;
        cell = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    public static IGridEntity FindNearestEntity(Vector2Int origin, IGridEntity fallbackActivator, int maxRange)
    {
        IGridEntity best = fallbackActivator;
        int bestDistance = best != null ? SquaredDistance(origin, best.GridPosition) : int.MaxValue;
        int maxDistance = maxRange <= 0 ? int.MaxValue : maxRange * maxRange;

        GridPlayer player = Object.FindFirstObjectByType<GridPlayer>();
        if (player != null)
        {
            int distance = SquaredDistance(origin, player.GridPosition);
            if (distance < bestDistance && distance <= maxDistance)
            {
                best = player;
                bestDistance = distance;
            }
        }

        if (GridSystem.Instance == null) return best;

        foreach (Vector2Int cell in GridSystem.Instance.GetAllWalkableCells())
        {
            IGridEntity entity = TurnManager.Instance != null ? TurnManager.Instance.GetEntityAt(cell) : null;
            if (entity == null) continue;

            int distance = SquaredDistance(origin, entity.GridPosition);
            if (distance < bestDistance && distance <= maxDistance)
            {
                best = entity;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static int SquaredDistance(Vector2Int a, Vector2Int b)
    {
        int dx = a.x - b.x;
        int dy = a.y - b.y;
        return dx * dx + dy * dy;
    }
}
