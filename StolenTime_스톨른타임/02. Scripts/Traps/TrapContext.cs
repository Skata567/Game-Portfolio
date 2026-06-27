using UnityEngine;

public readonly struct TrapContext
{
    public readonly Vector2Int Position;
    public readonly IGridEntity Activator;
    public readonly int CurrentFloor;
    public readonly TrapData Data;
    public readonly GridTrap Trap;

    public TrapContext(Vector2Int position, IGridEntity activator, int currentFloor, TrapData data, GridTrap trap)
    {
        Position = position;
        Activator = activator;
        CurrentFloor = currentFloor;
        Data = data;
        Trap = trap;
    }
}
