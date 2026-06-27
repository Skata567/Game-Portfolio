using UnityEngine;

/// <summary>
/// 트랩 위치를 중심으로 범위 피해를 적용하는 효과입니다.
/// 중심칸과 가장자리 피해량을 다르게 줄 수 있어 폭발/충격파형 트랩에 사용합니다.
/// </summary>
public class AreaDamageTrapEffect : MonoBehaviour, ITrapEffect
{
    [Header("범위 피해")]
    [Tooltip("피해 반경입니다. -1이면 TrapData.radius를 사용하고, 0 이상이면 이 값으로 덮어씁니다.")]
    [SerializeField] private int radiusOverride = -1;

    [Tooltip("중심칸이 아닌 칸에 적용할 피해 배율입니다. 0.5면 가장자리 피해가 절반으로 들어갑니다.")]
    [SerializeField, Range(0f, 1f)] private float edgeDamageMultiplier = 0.5f;

    [Tooltip("끄면 트랩 중심칸은 피해를 받지 않고 주변 칸만 피해를 받습니다.")]
    [SerializeField] private bool damageCenter = true;

    /// <summary>
    /// 반경 안의 각 칸을 순회하면서 피해 가능한 엔티티에게 데미지를 적용합니다.
    /// 이미 죽은 대상은 건너뜁니다.
    /// </summary>
    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
        {
            IGridEntity entity = TrapEffectUtility.GetEntityAt(cell, context.Activator);
            IDamageable damageable = TrapEffectUtility.AsDamageable(entity);
            if (damageable == null || damageable.IsDead) continue;

            bool isCenter = cell == context.Position;
            if (isCenter && !damageCenter) continue;

            int damage = context.Data.CalculateDamage(damageable);
            if (!isCenter)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * edgeDamageMultiplier));

            damageable.TakeDamage(damage);
        }
    }
}
