using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵에 배치된 함정(GridTrap)들을 좌표(Vector2Int) 기반으로 캐싱하고 플레이어 이동 시 발동 여부를 검사하는 코어 시스템입니다.
/// 함정의 등록/해제, 밟았을 때의 단일 발동, 범위 폭발 발동, 그리고 
/// 일정 턴 이후에 터지는 특수 함정(DelayedAreaEffect)의 턴 카운트다운을 전담하여 관리합니다.
/// </summary>
public class TrapSystem : MonoBehaviour
{
    public static TrapSystem Instance { get; private set; }

    [SerializeField, Min(1)] private int currentFloor = 1;

    private readonly Dictionary<Vector2Int, GridTrap> _traps = new();
    private readonly List<PendingDelayedAreaEffect> _pendingDelayedAreaEffects = new();

    public int CurrentFloor => currentFloor;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("TrapSystem이 씬에 두 개 이상 있습니다.");
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerActionCompleted += OnPlayerActionCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerActionCompleted -= OnPlayerActionCompleted;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetCurrentFloor(int floor)
    {
        currentFloor = Mathf.Max(1, floor);
    }

    /// <summary>
    /// 생성된 함정 객체를 TrapSystem의 관리 목록에 좌표 기준으로 등록합니다.
    /// 절차적 맵 생성 시 스폰된 함정들이 초기화되면서 호출합니다.
    /// </summary>
    public void Register(GridTrap trap)
    {
        if (trap == null) return;
        _traps[trap.GridPosition] = trap;
    }

    /// <summary>
    /// 발동 완료되거나 파괴된 함정을 관리 목록에서 제거합니다.
    /// </summary>
    public void Unregister(GridTrap trap)
    {
        if (trap == null) return;
        if (_traps.TryGetValue(trap.GridPosition, out GridTrap current) && current == trap)
            _traps.Remove(trap.GridPosition);
    }

    /// <summary>
    /// 특정 격자 좌표(cell)에 함정이 존재하는지 확인하고, 존재할 경우 발동을 시도합니다.
    /// 주로 플레이어가 이동을 완료한 직후 해당 타일에 이 함수를 호출하여 함정을 밟았는지 판정합니다.
    /// </summary>
    public bool TryTriggerAt(Vector2Int cell, IGridEntity activator)
    {
        if (!_traps.TryGetValue(cell, out GridTrap trap) || trap == null)
            return false;

        return trap.TryTrigger(activator);
    }

    public bool TryGetTrapAt(Vector2Int cell, out GridTrap trap)
    {
        return _traps.TryGetValue(cell, out trap) && trap != null;
    }

    public bool RevealAt(Vector2Int cell)
    {
        if (!TryGetTrapAt(cell, out GridTrap trap)) return false;
        trap.Reveal();
        return true;
    }

    /// <summary>
    /// 특정 원점(origin)을 기준으로 주어진 반경(radius) 내의 모든 함정들을 연쇄 발동시킵니다.
    /// </summary>
    public int TriggerArea(Vector2Int origin, int radius, IGridEntity activator)
    {
        int triggeredCount = 0;
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(origin, radius))
        {
            if (TryTriggerAt(cell, activator))
                triggeredCount++;
        }

        return triggeredCount;
    }

    /// <summary>
    /// 밟은 즉시 터지지 않고 지정된 턴(delayTurns)이 경과한 뒤에 광역 피해/상태이상을 뿌리는 지연형 함정을 스케줄링합니다.
    /// OnPlayerActionCompleted 이벤트와 연동되어 매 행동마다 턴 카운트가 차감됩니다.
    /// </summary>
    public void ScheduleDelayedAreaEffect(TrapContext context, TrapTerrainType terrainType, StatusType statusType)
    {
        // 덫은 플레이어 이동 처리 중에 예약되고, 같은 이동의 OnPlayerActionCompleted가 곧바로 발생합니다.
        // delayTurns=1이 "밟은 즉시"가 아니라 "다음 행동 완료 후" 발동되도록 1턴을 보정합니다.
        _pendingDelayedAreaEffects.Add(new PendingDelayedAreaEffect(
            context,
            terrainType,
            statusType,
            Mathf.Max(1, context.Data.delayTurns) + 1));
    }

    private void OnPlayerActionCompleted(float timeCost)
    {
        for (int i = _pendingDelayedAreaEffects.Count - 1; i >= 0; i--)
        {
            PendingDelayedAreaEffect pending = _pendingDelayedAreaEffects[i];
            pending.remainingTurns--;
            if (pending.remainingTurns > 0)
            {
                _pendingDelayedAreaEffects[i] = pending;
                continue;
            }

            ExecuteDelayedAreaEffect(pending);
            _pendingDelayedAreaEffects.RemoveAt(i);
        }
    }

    private void ExecuteDelayedAreaEffect(PendingDelayedAreaEffect pending)
    {
        TrapContext context = pending.context;
        GameEvents.OnTrapTerrainRequested?.Invoke(new TrapTerrainRequest(
            context.Position,
            context.Data.radius,
            context.Data.durationTurns,
            pending.terrainType,
            context.Trap));

        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, context.Data.radius))
        {
            IGridEntity entity = TrapEffectUtility.GetEntityAt(cell, context.Activator);
            if (entity == null) continue;

            TrapEffectUtility.RequestStatus(
                entity,
                cell,
                pending.statusType,
                context.Data.durationTurns,
                context.Data.statusPower,
                context.Trap);
        }
    }

    private struct PendingDelayedAreaEffect
    {
        public TrapContext context;
        public TrapTerrainType terrainType;
        public StatusType statusType;
        public int remainingTurns;

        public PendingDelayedAreaEffect(TrapContext context, TrapTerrainType terrainType, StatusType statusType, int remainingTurns)
        {
            this.context = context;
            this.terrainType = terrainType;
            this.statusType = statusType;
            this.remainingTurns = remainingTurns;
        }
    }
}
