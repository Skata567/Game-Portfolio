using System;

[Serializable]
public class NYHEquipmentItemInstance
{
    public ItemBase Data { get; }
    public ItemInstance SourceInstance { get; }
    public int Durability { get; private set; }
    public bool IsIdentified { get; private set; }
    public bool IsCursed { get; private set; }

    public NYHEquipmentItemInstance(ItemBase data, ItemInstance sourceInstance = null, int durability = 0, bool isIdentified = false, bool isCursed = false)
    {
        Data = data;
        SourceInstance = sourceInstance;
        Durability = durability;
        IsIdentified = isIdentified;
        IsCursed = isCursed;
    }

    public void SetDurability(int durability)
    {
        Durability = durability;
    }

    public void SetIdentified(bool isIdentified)
    {
        IsIdentified = isIdentified;
    }

    public void SetCursed(bool isCursed)
    {
        IsCursed = isCursed;
    }
}
