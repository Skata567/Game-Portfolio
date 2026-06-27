using UnityEngine;

public static class HazardTerrainEffect
{
    private const float HazardMoveCostMultiplier = 2f;
    private const int HazardSlowDurationTurns = 2;
    private const int HazardDamageDurationTurns = 2;
    private const int HazardDamagePower = 1;

    private static readonly StatusType HazardDamageStatusType = StatusType.Poison;

    public static bool IsHazardCell(Vector2Int cell)
    {
        return MapGenerator.Instance != null
            && MapGenerator.Instance.HazardCells != null
            && MapGenerator.Instance.HazardCells.Contains(cell);
    }

    public static float ModifyMoveCost(IGridEntity entity, Vector2Int targetCell, float baseCost)
    {
        if (!ShouldApplyHazard(entity, targetCell))
            return baseCost;

        if (GetArmorMagic(entity)?.IgnoresHazardSlow == true)
            return baseCost;

        return Mathf.Max(0.01f, baseCost * HazardMoveCostMultiplier);
    }

    public static void ApplyOnEnter(IGridEntity entity, Vector2Int cell)
    {
        if (!ShouldApplyHazard(entity, cell))
            return;

        MagicData magic = GetArmorMagic(entity);
        bool emptyStep = magic?.HalvesHazardDamage == true;

        if (!emptyStep)
            TrapEffectUtility.RequestStatus(entity, cell, StatusType.Slow, HazardSlowDurationTurns, 1, null);

        int power = emptyStep ? Mathf.Max(1, HazardDamagePower / 2) : HazardDamagePower;
        TrapEffectUtility.RequestStatus(entity, cell, HazardDamageStatusType, HazardDamageDurationTurns, power, null);
    }

    private static bool ShouldApplyHazard(IGridEntity entity, Vector2Int cell)
    {
        if (entity == null || !IsHazardCell(cell))
            return false;

        if (entity is Component component)
        {
            StatusController status = component.GetComponent<StatusController>();
            if (status != null && status.ShouldIgnoreHazard())
                return false;
        }

        return true;
    }

    // 엔티티의 장착 갑옷 MagicData를 가져옴
    private static MagicData GetArmorMagic(IGridEntity entity)
    {
        if (entity is not Component component)
            return null;
        NYHEquipmentController eq = component.GetComponent<NYHEquipmentController>();
        if (eq == null || !eq.TryGetEquipped(EquipmentSlot.Armor, out NYHEquipmentItemInstance item))
            return null;
        return item.SourceInstance is EquipmentInstance inst ? inst.magic : null;
    }
}
