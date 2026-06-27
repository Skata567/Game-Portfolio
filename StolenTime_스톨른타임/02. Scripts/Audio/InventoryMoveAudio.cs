namespace PrototypeRT
{
    public static class InventoryMoveAudio
    {
        public static void RequestMoveAudio(InventoryItem item)
        {
            RequestMoveAudio(item?.ItemData);
        }

        public static void RequestMoveAudio(ItemBase item)
        {
            AudioEventId eventId = GetMoveEventId(item);
            if (eventId == AudioEventId.None)
                return;

            GameEvents.OnAudioEventRequested?.Invoke(eventId);
        }

        public static AudioEventId GetMoveEventId(ItemBase item)
        {
            if (item == null)
                return AudioEventId.None;

            if (item is FragmentsOfLife)
                return AudioEventId.FragmentOfLifeMove;

            if (item is PotionData || ItemCategories.Of(item) == ItemCategory.Potion)
                return AudioEventId.MovePotion;

            if (item.ItemType == ItemType.Scroll || ItemCategories.Of(item) == ItemCategory.Scroll)
                return AudioEventId.MoveScroll;

            if (IsEquipment(item))
                return AudioEventId.MoveEquipment;

            return AudioEventId.None;
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
    }
}
