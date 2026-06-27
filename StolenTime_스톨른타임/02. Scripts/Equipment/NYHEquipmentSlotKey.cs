using System;

[Serializable]
public readonly struct NYHEquipmentSlotKey : IEquatable<NYHEquipmentSlotKey>
{
    public readonly EquipmentSlot Slot;
    public readonly int Index;

    public NYHEquipmentSlotKey(EquipmentSlot slot, int index)
    {
        Slot = slot;
        Index = Math.Max(0, index);
    }

    public bool Equals(NYHEquipmentSlotKey other)
    {
        return Slot == other.Slot && Index == other.Index;
    }

    public override bool Equals(object obj)
    {
        return obj is NYHEquipmentSlotKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Slot * 397) ^ Index;
        }
    }
}
