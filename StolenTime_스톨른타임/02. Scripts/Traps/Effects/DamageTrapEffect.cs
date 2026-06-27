using UnityEngine;

/// <summary>
/// 트랩 발동 시 대상에게 피해만 적용하는 기본 피해 효과입니다.
/// affectArea가 꺼져 있으면 발동 대상 1명, 켜져 있으면 트랩 위치 기준 반경 안의 대상들에게 피해를 줍니다.
/// </summary>
public class DamageTrapEffect : MonoBehaviour, ITrapEffect
{
    [Header("피해 범위")]
    [Tooltip("켜면 트랩 위치를 중심으로 반경 안의 모든 대상에게 피해를 줍니다. 끄면 트랩을 발동한 대상에게만 피해를 줍니다.")]
    [SerializeField] private bool affectArea;

    [Tooltip("효과 범위 지정값입니다. -1은 TrapData.Radius 사용, 0은 중심칸만, 1 이상은 해당 칸 반경을 뜻합니다.")]
    [SerializeField] private int radiusOverride = -1;

    /// <summary>
    /// 트랩 컨텍스트의 위치/데이터를 기준으로 피해를 줄 대상을 결정합니다.
    /// 실제 피해량 계산은 TrapData와 TrapEffectUtility가 처리합니다.
    /// </summary>
    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;

        if (!affectArea || radius <= 0)
        {
            DealDamage(context.Activator, context);
            return;
        }

        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
            DealDamage(TrapEffectUtility.GetEntityAt(cell, context.Activator), context);
    }

    private static void DealDamage(IGridEntity entity, TrapContext context)
    {
        TrapEffectUtility.DealDamage(entity, context.Data);
    }
}
