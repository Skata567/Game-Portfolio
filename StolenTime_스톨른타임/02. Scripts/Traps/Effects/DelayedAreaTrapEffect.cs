using UnityEngine;

public class DelayedAreaTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private TrapTerrainType terrainType = TrapTerrainType.Pit;
    [SerializeField] private StatusType statusType = StatusType.Fall;

    public void Execute(TrapContext context)
    {
        if (TrapSystem.Instance == null)
        {
            GameEvents.OnTrapStubRequested?.Invoke(new TrapStubRequest(
                context.Position,
                "DelayedAreaTrapEffect requires TrapSystem in the scene.",
                context.Trap));
            return;
        }

        // 지연 예약은 TrapSystem이 들고 갑니다.
        // 덫 프리팹이 1회성이라 발동 직후 비활성화되어도 예약된 효과가 사라지지 않게 하기 위해서입니다.
        TrapSystem.Instance.ScheduleDelayedAreaEffect(context, terrainType, statusType);
    }
}
