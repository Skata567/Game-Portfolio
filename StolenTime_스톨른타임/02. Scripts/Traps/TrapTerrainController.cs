using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 불/얼음/독가스처럼 일정 범위에 남는 "장판 판정"을 한 곳에서 관리합니다.
/// 포션이나 함정은 시각 이펙트를 직접 띄우더라도, 실제 상태이상 판정은
/// GameEvents.OnTrapTerrainRequested 이벤트를 통해 이 컨트롤러에 등록해야 합니다.
/// </summary>
public class TrapTerrainController : MonoBehaviour
{
    public static TrapTerrainController Instance { get; private set; }

    [SerializeField] private TrapTerrainVisual visualPrefab;

    // 현재 살아 있는 장판 판정 목록입니다.
    // visualPrefab이 비어 있어도 이 목록만 있으면 상태이상 판정은 정상 동작합니다.
    private readonly List<ActiveTerrainEffect> _activeEffects = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static TrapTerrainController EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        // 씬에 직접 배치해 둔 매니저가 있으면 그 오브젝트를 사용합니다.
        // Inspector에서 visualPrefab을 연결하고 싶을 때는 씬 배치 방식이 가장 편합니다.
        TrapTerrainController existing = FindFirstObjectByType<TrapTerrainController>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        // 씬에 없으면 런타임에 자동 생성합니다.
        // 이렇게 해두면 FirePotion/IcePotion이 이벤트를 보냈는데 받을 매니저가 없어 무시되는 일을 막을 수 있습니다.
        GameObject root = new GameObject("TrapTerrainController");
        Instance = root.AddComponent<TrapTerrainController>();
        return Instance;
    }

    /// <summary>
    /// 특정 엔티티가 특정 칸에 들어갔을 때, 그 칸에 활성 장판이 있으면 상태이상을 적용합니다.
    /// 플레이어/몬스터 이동 완료 지점에서 호출합니다.
    /// </summary>
    public static bool TryApplyTerrainAt(IGridEntity entity, Vector2Int cell)
    {
        TrapTerrainController controller = EnsureInstance();
        return controller != null && controller.TryApplyTerrainAtInternal(entity, cell);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        GameEvents.OnTrapTerrainRequested += OnTrapTerrainRequested;
        GameEvents.OnPlayerActionCompleted += OnPlayerActionCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnTrapTerrainRequested -= OnTrapTerrainRequested;
        GameEvents.OnPlayerActionCompleted -= OnPlayerActionCompleted;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnTrapTerrainRequested(TrapTerrainRequest request)
    {
        // 시각용 장판 프리팹이 연결되어 있으면 범위 전체에 표시합니다.
        // 포션 자체 이펙트가 따로 있는 경우 visualPrefab은 비워도 됩니다.
        var visuals = new List<TrapTerrainVisual>();
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(request.Origin, request.Radius))
        {
            if (visualPrefab == null) continue;
            TrapTerrainVisual visual = Instantiate(visualPrefab);
            visual.Show(cell, request.TerrainType);
            visuals.Add(visual);
        }

        ActiveTerrainEffect effect = new ActiveTerrainEffect(
            request.Origin,
            request.Radius,
            request.DurationTurns > 0 ? request.DurationTurns : 1,
            request.TerrainType,
            visuals);

        _activeEffects.Add(effect);

        // 장판이 생긴 순간 이미 그 위에 서 있던 대상도 즉시 상태이상에 걸려야 합니다.
        // 예: 플레이어 발밑에 불 포션을 던지고 가만히 있어도 바로 화상 적용.
        ApplyTerrainTick(effect);
    }

    private void OnPlayerActionCompleted(float timeCost)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveTerrainEffect effect = _activeEffects[i];

            // 장판 위에 계속 서 있는 대상은 매 플레이어 행동 완료마다 상태가 갱신됩니다.
            // StatusController가 같은 상태의 남은 시간을 max로 유지하므로 중복 적용도 안전합니다.
            ApplyTerrainTick(effect);

            effect.remainingTurns--;
            if (effect.remainingTurns <= 0)
            {
                DestroyVisuals(effect.visuals);
                _activeEffects.RemoveAt(i);
            }
            else
            {
                _activeEffects[i] = effect;
            }
        }
    }

    private void ApplyTerrainTick(ActiveTerrainEffect effect)
    {
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(effect.origin, effect.radius))
        {
            // TurnManager에 등록된 적/보스뿐 아니라 GridSystem에 등록된 플레이어도 찾습니다.
            IGridEntity entity = TrapEffectUtility.GetEntityAt(cell);
            if (entity == null) continue;

            ApplyStatusForTerrain(entity, cell, effect.terrainType);
        }
    }

    private bool TryApplyTerrainAtInternal(IGridEntity entity, Vector2Int cell)
    {
        // 호출자가 엔티티를 넘기지 않은 경우, 현재 칸의 엔티티를 직접 찾아서 처리합니다.
        if (entity == null)
            entity = TrapEffectUtility.GetEntityAt(cell);

        if (entity == null)
            return false;

        bool applied = false;
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveTerrainEffect effect = _activeEffects[i];
            if (!effect.Contains(cell))
                continue;

            applied |= ApplyStatusForTerrain(entity, cell, effect.terrainType);
        }

        return applied;
    }

    private static bool ApplyStatusForTerrain(IGridEntity entity, Vector2Int cell, TrapTerrainType terrainType)
    {
        if (!TryGetStatusType(terrainType, out StatusType statusType))
            return false;

        // v1 밸런스: 장판에 닿을 때마다 1초/1파워 상태를 부여합니다.
        // 장판 유지 시간과 상태 지속 시간은 분리되어 있습니다.
        // 화상 지속시간 
        TrapEffectUtility.RequestStatus(entity, cell, statusType, 5, 1, null);
        return true;
    }

    private static bool TryGetStatusType(TrapTerrainType terrainType, out StatusType statusType)
    {
        // 장판 종류와 실제 상태이상 종류의 대응표입니다.
        // 새 장판을 추가하면 몬스터/플레이어 코드가 아니라 이 매핑만 확장하면 됩니다.
        switch (terrainType)
        {
            case TrapTerrainType.Fire:
                statusType = StatusType.Burn;
                return true;
            case TrapTerrainType.Chill:
                statusType = StatusType.Chill;
                return true;
            case TrapTerrainType.Shock:
                statusType = StatusType.Shock;
                return true;
            case TrapTerrainType.Gas:
                statusType = StatusType.Poison;
                return true;
            case TrapTerrainType.Ooze:
                statusType = StatusType.Ooze;
                return true;
            default:
                statusType = default;
                return false;
        }
    }

    private void DestroyVisuals(List<TrapTerrainVisual> visuals)
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
                Destroy(visuals[i].gameObject);
        }
    }

    private struct ActiveTerrainEffect
    {
        public Vector2Int origin;
        public int radius;
        public int remainingTurns;
        public TrapTerrainType terrainType;
        public List<TrapTerrainVisual> visuals;

        public ActiveTerrainEffect(Vector2Int origin, int radius, int remainingTurns, TrapTerrainType terrainType, List<TrapTerrainVisual> visuals)
        {
            this.origin = origin;
            this.radius = radius;
            this.remainingTurns = remainingTurns;
            this.terrainType = terrainType;
            this.visuals = visuals;
        }

        public bool Contains(Vector2Int cell)
        {
            // 현재 장판은 사각형 범위 판정입니다. CellsInRadius도 같은 방식으로 순회하므로 판정 모양을 맞춥니다.
            return Mathf.Abs(cell.x - origin.x) <= radius
                && Mathf.Abs(cell.y - origin.y) <= radius
                && GridSystem.Instance != null
                && GridSystem.Instance.IsWalkable(cell);
        }
    }
}
