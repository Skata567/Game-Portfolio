namespace PrototypeRT
{
    public static class ItemPickupAudio
    {
        public static void RequestPickupAudio(ItemInstance item)
        {
            RequestPickupAudio(item?.data);
        }

        public static void RequestPickupAudio(ItemBase item)
        {
            AudioEventId eventId = GetPickupEventId(item);
            if (eventId == AudioEventId.None)
                return;

            GameEvents.OnAudioEventRequested?.Invoke(eventId);
        }

        public static AudioEventId GetPickupEventId(ItemBase item)
        {
            if (item == null)
                return AudioEventId.None;

            if (item is FragmentsOfLife)
                return AudioEventId.FragmentOfLifePicked;
            
            if(IsPotion(item))
                return AudioEventId.PotionPicked;

            if (IsScroll(item))
                return AudioEventId.ScrollPicked;

            if (IsEquipment(item))
                return AudioEventId.EquipmentPicked;

            return AudioEventId.ItemPicked;
        }

        private static bool IsScroll(ItemBase item)
        {
            return item.ItemType == ItemType.Scroll || ItemCategories.Of(item) == ItemCategory.Scroll;
        }

        private static bool IsEquipment(ItemBase item)
        {
            if (item.ItemType == ItemType.Equipment || item.Slot != EquipmentSlot.None || item is ThrowableWeapon)
                return true;

            ItemCategory category = ItemCategories.Of(item);
            return category == ItemCategory.Armor
                || category == ItemCategory.MeleeWeapon
                || category == ItemCategory.ThrowWeapon
                || category == ItemCategory.Ring;
        }

        private static bool IsPotion(ItemBase item)
        {
            return item is PotionData || ItemCategories.Of(item) == ItemCategory.Potion;
        }
    }
}
