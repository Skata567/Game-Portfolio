using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 아이템을 더블클릭했을 때 어떤 방식으로 사용할지 결정합니다.
    /// 아이템 종류별 처리를 한 메서드의 if-else에 몰아넣지 않기 위해 간단한 전략 목록으로 분리했습니다.
    /// </summary>
    public class InventoryItemUseController : MonoBehaviour
    {
        [Tooltip("아이템을 들고 있는 상태와 인벤토리 배치를 관리하는 컨트롤러입니다.")]
        [SerializeField] private InventoryController inventoryController;

        [Tooltip("투척 포션과 대상 지정 주문서의 후속 클릭 처리를 맡는 컨트롤러입니다.")]
        [SerializeField] private InventoryTargetingController targetingController;

        [Tooltip("더블클릭 장비 자동 착용에 사용하는 장비 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private NYHEquipmentController equipmentController;

        private struct ItemUseStrategy
        {
            public Func<ItemBase, bool> Predicate;
            public Func<InventoryItem, bool> Handler;
        }

        private readonly List<ItemUseStrategy> _useStrategies = new();

        private void Awake()
        {
            RegisterDefaultStrategies();
        }

        private void RegisterDefaultStrategies()
        {
            // 부모 타입인 PotionData보다 ThrowablePotion을 먼저 검사해야 투척 포션이 즉시 사용으로 빠지지 않습니다.
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = data => data is ThrowablePotion,
                Handler = item => targetingController != null && targetingController.BeginThrowPotion(item)
            });
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = data => data is PotionData,
                Handler = UseImmediateItem
            });
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = IsEquipment,
                Handler = TryAutoEquip
            });
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = data => data is AnalyzeScroll,
                Handler = item => targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Analyze)
            });
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = data => data is ReinforceScroll,
                Handler = item => targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Reinforce)
            });
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = data => data is PurifyScroll,
                Handler = item => targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Purify)
            });
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = data => data is MapScroll,
                Handler = UseImmediateItem
            });
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = data => data is TorchData,
                Handler = UseImmediateItem
            });
            _useStrategies.Add(new ItemUseStrategy {
                Predicate = data => data is FragmentsOfLife,
                Handler = UseImmediateItem
            });
        }

        /// <summary>
        /// 새 아이템 타입이 추가될 때 기존 분기문을 건드리지 않고 사용 규칙만 추가하기 위한 연결점입니다.
        /// </summary>
        public void AddUseStrategy(Func<ItemBase, bool> predicate, Func<InventoryItem, bool> handler)
        {
            if (predicate == null || handler == null)
                return;

            _useStrategies.Add(new ItemUseStrategy { Predicate = predicate, Handler = handler });
        }

        public void Configure(InventoryController inventory, InventoryTargetingController targeting)
        {
            inventoryController = inventory;
            targetingController = targeting;
            ResolveReferences();
        }

        public bool TryUseDoubleClickedItem(InventoryItem item)
        {
            ResolveReferences();

            if (item == null || item.ItemData == null || inventoryController == null)
                return false;

            if (targetingController != null && targetingController.IsTargeting)
                return false;

            ItemBase itemData = item.ItemData;

            for (int i = 0; i < _useStrategies.Count; i++)
            {
                ItemUseStrategy strategy = _useStrategies[i];
                if (strategy.Predicate(itemData))
                    return strategy.Handler(item);
            }

            // 대상 지정 방식이 연결되지 않은 일반 스크롤은 인벤토리에서 눌러도 아무 행동을 하지 않습니다.
            // 즉시 Use()를 호출하면 지도 스크롤이 아닌 스크롤까지 의도치 않게 소비될 수 있어 여기서 막습니다.
            if (itemData.ItemType == ItemType.Scroll)
                return false;

            return false;
        }

        private bool UseImmediateItem(InventoryItem item)
        {
            item.ItemData.Use();
            return inventoryController.TryConsumeOneItem(item);
        }

        private bool TryAutoEquip(InventoryItem item)
        {
            if (equipmentController == null)
                return false;

            if (!equipmentController.TryGetTargetSlotFor(item.ItemData, out EquipmentSlot slot, out int slotIndex))
                return false;

            // 교체 장비가 생기는 경우, 먼저 인벤토리에 되돌릴 공간을 확인해서 장비가 사라지는 상황을 막습니다.
            if (equipmentController.TryGetEquippedInventoryItem(slot, slotIndex, out InventoryItem replacedItem)
                && replacedItem != null
                && !inventoryController.CanPlaceInDefaultGrid(replacedItem))
            {
                Debug.LogWarning("InventoryItemUseController: 교체할 장비를 넣을 인벤토리 공간이 없어 착용을 취소합니다.");
                return false;
            }

            if (!equipmentController.TryEquipFromInventory(item, slot, slotIndex, out replacedItem))
                return false;

            if (!inventoryController.TryConsumeHeldItem(item, false))
            {
                equipmentController.Unequip(slot, slotIndex, out _);
                return false;
            }

            item.gameObject.SetActive(false);

            if (replacedItem != null && !inventoryController.TryPlaceInDefaultGrid(replacedItem))
                inventoryController.HoldItem(replacedItem, null, 0, 0);

            return true;
        }

        private static bool IsEquipment(ItemBase itemData)
        {
            if (itemData == null)
                return false;

            return itemData.ItemType == ItemType.Equipment || itemData.Slot != EquipmentSlot.None;
        }

        private void ResolveReferences()
        {
            if (inventoryController == null)
                inventoryController = GetComponent<InventoryController>();

            if (targetingController == null)
                targetingController = GetComponent<InventoryTargetingController>();

            if (equipmentController == null)
                equipmentController = FindFirstObjectByType<NYHEquipmentController>();
        }
    }
}
