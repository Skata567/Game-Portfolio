using UnityEngine;

/// <summary>
/// 트랩 위치 주변에 상태이상을 퍼뜨리는 범위 상태 트랩 효과입니다.
/// 대상이 없는 빈 칸에도 상태 요청을 남길 수 있어, 독 장판/화상 지대 같은 지형성 효과에 사용합니다.
/// </summary>
public class AreaStatusTrapEffect : MonoBehaviour, ITrapEffect
{
    [Header("상태이상")]
    [Tooltip("범위 안 대상 또는 칸에 적용할 상태이상 종류입니다.")]
    [SerializeField] private StatusType statusType;

    [Header("적용 범위")]
    [Tooltip("켜면 엔티티가 없는 빈 칸에도 상태이상 요청을 보냅니다. 장판형 효과를 만들 때 사용합니다.")]
    [SerializeField] private bool affectEmptyCells = true;

    [Tooltip("효과 반경입니다. -1이면 TrapData.radius를 사용하고, 0 이상이면 이 값으로 덮어씁니다.")]
    [SerializeField] private int radiusOverride = -1;

    /// <summary>
    /// 반경 안의 칸을 순회하며 엔티티 또는 빈 칸에 상태이상 요청을 전달합니다.
    /// </summary>
    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
        {
            IGridEntity entity = TrapEffectUtility.GetEntityAt(cell, context.Activator);
            if (entity != null || affectEmptyCells)
            {
                TrapEffectUtility.RequestStatus(
                    entity,
                    cell,
                    statusType,
                    context.Data.durationTurns,
                    context.Data.statusPower,
                    context.Trap);
            }
        }
    }
}
