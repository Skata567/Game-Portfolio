using System;
using System.Collections.Generic;
using PrototypeRT;
using System.Text;
using UnityEngine;

/// <summary>
/// 플레이어의 장비 장착 및 해제, 그리고 이로 인한 스탯 변동을 관리하는 코어 시스템입니다.
/// 인벤토리에서 전달받은 아이템 데이터를 분석해 올바른 슬롯(EquipmentSlot)에 장착하고,
/// 기존 장비가 있다면 스왑 처리하며, 장착 조건(요구 레벨/힘)을 검사합니다.
/// </summary>
public class NYHEquipmentController : MonoBehaviour
{
    private const float SlowCurseMoveActionDelayPenalty = 0.2f;

    [Header("참조")]
    [Tooltip("스탯을 적용할 플레이어 스탯입니다. 비워두면 씬의 NYHPlayerStats.Instance를 사용합니다.")]
    [SerializeField] private NYHPlayerStats playerStats;

    [Tooltip("레벨 조건을 확인할 플레이어 레벨 시스템입니다. 비워두면 씬의 PlayerLevelSystem.Instance를 사용합니다.")]
    [SerializeField] private PlayerLevelSystem playerLevelSystem;

    [Tooltip("YJW ItemBase에 NYH 장비 스탯을 연결하는 프로필 목록입니다.")]
    [SerializeField] private List<NYHEquipmentProfile> equipmentProfiles = new();

    [Header("시작 장비")]
    [Tooltip("게임 시작 시 자동으로 장착할 기본 무기 데이터입니다. 비워두면 무기 없이 시작합니다.")]
    [SerializeField] private ItemBase startingWeapon;

    [Tooltip("게임 시작 시 자동으로 장착할 기본 방어구 데이터입니다. 비워두면 방어구 없이 시작합니다.")]
    [SerializeField] private ItemBase startingArmor;

    [Tooltip("시작 장비를 나중에 해제할 수 있도록 숨겨진 InventoryItem 뷰를 만들 인벤토리 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private InventoryController inventoryController;

    [Tooltip("시작 장비 InventoryItem을 임시로 보관할 부모 Transform입니다. 비워두면 InventoryController Transform 아래에 비활성 상태로 둡니다.")]
    [SerializeField] private Transform startingEquipmentViewParent;

    [Tooltip("반지/마법부여/저주가 최종 이동/공격 지연 시간에 개입하는 공용 통로입니다. 비워도 플레이어에서 자동으로 찾아 붙입니다.")]
    [SerializeField] private NYHFinalStatModifierController finalStatModifierController;

    [Header("Debug Log")]
    [Tooltip("장착/해제/강화/정화로 장비 스탯이나 최종 쿨다운 보정이 바뀔 때 Console에 변화량을 출력합니다.")]
    [SerializeField] private bool logEquipmentStatChanges = true;

    // 실제 장착된 장비 데이터(인스턴스)들을 슬롯 키를 기준으로 관리하는 딕셔너리
    private readonly Dictionary<NYHEquipmentSlotKey, NYHEquipmentItemInstance> _equippedItems = new();
    
    // 장착된 장비에 의해 현재 적용 중인 스탯 프로필들
    private readonly Dictionary<NYHEquipmentSlotKey, NYHEquipmentProfile> _appliedProfiles = new();
    
    // 장비와 연결된 인벤토리 내 UI 시각적 객체(InventoryItem)의 참조 보관용
    private readonly Dictionary<NYHEquipmentSlotKey, InventoryItem> _equippedViews = new();

    // 장착 장비가 현재 플레이어에게 적용 중인 정수 스탯 총합입니다.
    // 강화 중인 반지처럼 수치가 바뀌는 장비는 전체 총합을 다시 계산하고 차이만 반영해야 예전 수치가 남지 않습니다.
    private readonly Dictionary<NYHStatType, int> _appliedEquipmentStatTotals = new();

    // 장착 장비가 현재 적용 중인 최종 행동 지연 보정 총합입니다.
    // 로그용으로 이전 총합과 새 총합의 차이를 비교합니다.
    private readonly Dictionary<NYHFinalStatTarget, float> _appliedEquipmentFinalStatTotals = new();

    // 씬 오브젝트가 꺼졌다 켜져도 시작 장비가 중복 장착되지 않도록 한 번만 처리한다.
    private bool _startingEquipmentApplied;

    /// <summary>장비 장착 완료 시 발동하는 이벤트</summary>
    public event Action<EquipmentSlot, NYHEquipmentItemInstance> OnEquipped;
    /// <summary>장비 해제 완료 시 발동하는 이벤트</summary>
    public event Action<EquipmentSlot, NYHEquipmentItemInstance> OnUnequipped;
    /// <summary>인벤토리 아이템 뷰가 연결된 상태로 장착되었을 때 발동하는 UI 연동 이벤트</summary>
    public event Action<EquipmentSlot, int, InventoryItem> OnInventoryItemEquipped;
    /// <summary>인벤토리 아이템 뷰가 장착 해제되었을 때 발동하는 UI 연동 이벤트</summary>
    public event Action<EquipmentSlot, int, InventoryItem> OnInventoryItemUnequipped;
    /// <summary>장착 중인 장비의 강화/감정/정화 같은 내부 상태가 바뀌었을 때 발동하는 이벤트</summary>
    public event Action<EquipmentSlot, int, InventoryItem> OnInventoryItemStateChanged;

    private void Start()
    {
        ApplyStartingEquipmentOnce();
    }

    /// <summary>
    /// 플레이어 스탯 시스템에 접근하는 지연 초기화 프로퍼티입니다.
    /// 명시적으로 할당되지 않았다면 싱글톤 인스턴스를 찾습니다.
    /// </summary>
    private NYHPlayerStats Stats
    {
        get
        {
            if (playerStats == null)
                playerStats = NYHPlayerStats.Instance;

            return playerStats;
        }
    }

    /// <summary>
    /// 플레이어 레벨 시스템에 접근하는 지연 초기화 프로퍼티입니다.
    /// 명시적으로 할당되지 않았다면 싱글톤 인스턴스를 찾습니다.
    /// </summary>
    private PlayerLevelSystem LevelSystem
    {
        get
        {
            if (playerLevelSystem == null)
                playerLevelSystem = PlayerLevelSystem.Instance;

            return playerLevelSystem;
        }
    }

    /// <summary>
    /// 최종 행동 지연 보정 컨트롤러를 가져오는 지연 초기화 프로퍼티입니다.
    /// 장비/이동/공격 컴포넌트가 같은 보정 컨트롤러를 보도록 이 오브젝트(Player)에 붙은 컴포넌트를 우선 사용합니다.
    /// 다른 오브젝트의 컨트롤러를 쓰면 장착 로그에는 보정이 찍히지만 실제 이동 계산에는 적용되지 않을 수 있습니다.
    /// </summary>
    private NYHFinalStatModifierController FinalStatModifiers
    {
        get
        {
            NYHFinalStatModifierController localController = GetComponent<NYHFinalStatModifierController>();
            if (localController == null)
            {
                // 인스펙터 연결이 빠져도 장착 보정이 유실되지 않도록 Player 오브젝트에 런타임으로 붙인다.
                localController = gameObject.AddComponent<NYHFinalStatModifierController>();
            }

            if (finalStatModifierController != null && finalStatModifierController != localController)
            {
                Debug.LogWarning("NYHEquipmentController: 다른 오브젝트의 NYHFinalStatModifierController가 연결되어 있어 Player 로컬 컨트롤러로 교체합니다.");
            }

            finalStatModifierController = localController;
            return finalStatModifierController;
        }
    }

    /// <summary>
    /// 원본 아이템 데이터를 받아 새로운 장비 인스턴스로 변환 후 장착을 시도합니다.
    /// </summary>
    public bool TryEquip(ItemBase itemData)
    {
        if (itemData == null)
            return false;

        return TryEquip(new NYHEquipmentItemInstance(itemData));
    }

    private void ApplyStartingEquipmentOnce()
    {
        if (_startingEquipmentApplied)
            return;

        _startingEquipmentApplied = true;

        TryEquipStartingItem(startingWeapon, EquipmentSlot.Weapon);
        TryEquipStartingItem(startingArmor, EquipmentSlot.Armor);
    }

    private void TryEquipStartingItem(ItemBase itemData, EquipmentSlot expectedSlot)
    {
        if (itemData == null)
            return;

        if (!TryGetTargetSlotFor(itemData, out EquipmentSlot resolvedSlot, out _) || resolvedSlot != expectedSlot)
        {
            Debug.LogWarning($"NYHEquipmentController: 시작 장비 {itemData.Name}의 슬롯이 {expectedSlot}이 아니라 {resolvedSlot}입니다.");
            return;
        }

        ItemInstance sourceInstance = itemData.CreateInstance();
        if (sourceInstance == null)
        {
            Debug.LogWarning($"NYHEquipmentController: 시작 장비 {itemData.Name}의 인스턴스를 만들지 못해 장착하지 않습니다.");
            return;
        }

        MarkStartingEquipmentIdentified(sourceInstance);

        InventoryItem itemView = CreateStartingEquipmentView(sourceInstance);
        if (itemView == null)
        {
            Debug.LogWarning($"NYHEquipmentController: 시작 장비 {itemData.Name}의 UI 아이템을 만들지 못해 장착하지 않습니다.");
            return;
        }

        NYHEquipmentSlotKey key = new NYHEquipmentSlotKey(expectedSlot, 0);

        // 방어구/무기 계산은 강화 단계와 저주 같은 런타임 값을 SourceInstance에서 읽는다.
        // 또한 장비 해제는 InventoryItem 뷰를 손에 드는 방식이라, 시작 장비도 숨겨진 뷰를 함께 등록해야 한다.
        if (!TryEquipInternal(new NYHEquipmentItemInstance(itemData, sourceInstance, isIdentified: true), itemView, key, out InventoryItem replacedItem))
        {
            if (itemView != null)
                Destroy(itemView.gameObject);
            Debug.LogWarning($"NYHEquipmentController: 시작 장비 {itemData.Name} 장착에 실패했습니다.");
            return;
        }

        itemView.gameObject.SetActive(false);
        itemView.SetRaycastTarget(false);

        if (replacedItem != null)
            replacedItem.gameObject.SetActive(false);
    }

    private static void MarkStartingEquipmentIdentified(ItemInstance sourceInstance)
    {
        if (sourceInstance is not EquipmentInstance equipmentInstance)
            return;

        // 시작 장비는 플레이어가 이미 알고 있는 기본 장비라 미감정 표시 없이 장착한다.
        equipmentInstance.isAnalyzed = true;
    }

    private InventoryItem CreateStartingEquipmentView(ItemInstance sourceInstance)
    {
        InventoryController targetInventory = ResolveInventoryController();
        if (targetInventory == null)
        {
            Debug.LogWarning("NYHEquipmentController: InventoryController를 찾지 못해 시작 장비는 해제 가능한 UI 아이템 없이 장착됩니다.");
            return null;
        }

        Transform parent = startingEquipmentViewParent != null ? startingEquipmentViewParent : targetInventory.transform;
        InventoryItem itemView = targetInventory.CreateItemView(sourceInstance, parent, true);
        if (itemView == null)
            return null;

        // 시작 장비는 인벤토리 칸을 차지하지 않고 장비 슬롯에만 들어간 상태여야 한다.
        // 해제할 때 InventoryController.HoldItem이 다시 활성화하고 Canvas 아래로 옮긴다.
        itemView.gameObject.SetActive(false);
        itemView.SetRaycastTarget(false);
        return itemView;
    }

    private InventoryController ResolveInventoryController()
    {
        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryController>();

        return inventoryController;
    }

    /// <summary>
    /// 이미 생성된 장비 인스턴스의 장착을 시도합니다. 
    /// UI 참조(InventoryItem) 없이 순수 데이터 레벨의 장착 시 사용됩니다.
    /// </summary>
    public bool TryEquip(NYHEquipmentItemInstance item)
    {
        if (item == null || item.Data == null)
            return false;

        NYHEquipmentSlotKey key = GetDefaultKey(item.Data);
        return TryEquipInternal(item, null, key, out _);
    }

    /// <summary>
    /// 지정한 아이템을 현재 플레이어가 장착할 수 있는 조건(요구 레벨, 힘)인지 사전에 검증합니다.
    /// </summary>
    public bool CanEquip(ItemBase itemData)
    {
        if (itemData == null) return false;

        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);
        if (slot == EquipmentSlot.None) return false;

        return MeetsEquipRequirements(itemData, profile);
    }

    /// <summary>
    /// 지정한 아이템을 특정 슬롯에 장착할 수 있는지 사전에 검증합니다.
    /// </summary>
    public bool CanEquip(ItemBase itemData, EquipmentSlot expectedSlot, int slotIndex)
    {
        if (itemData == null) return false;

        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);
        if (slot == EquipmentSlot.None || slot != expectedSlot) return false;

        return MeetsEquipRequirements(itemData, profile);
    }

    /// <summary>
    /// 대상 아이템 데이터가 들어갈 기본 슬롯과 인덱스를 판별해 반환합니다.
    /// </summary>
    public bool TryGetTargetSlotFor(ItemBase itemData, out EquipmentSlot slot, out int slotIndex)
    {
        slot = EquipmentSlot.None;
        slotIndex = 0;

        // 자동 장착 전에 슬롯과 조건을 모두 확인합니다.
        // 실패하면 장착/교체 시도를 하지 않아 인벤토리 상태가 바뀌지 않습니다.
        if (itemData == null || !CanEquip(itemData))
            return false;

        // 반지는 0번 칸이 차 있으면 1번 칸을 우선 선택하는 기존 GetDefaultKey 규칙을 그대로 따릅니다.
        NYHEquipmentSlotKey key = GetDefaultKey(itemData);
        slot = key.Slot;
        slotIndex = key.Index;
        return slot != EquipmentSlot.None;
    }

    /// <summary>
    /// 인벤토리 UI 상의 아이템을 장착 시도합니다.
    /// 장착 성공 시 기존에 착용 중이던 아이템 뷰가 있다면 replacedItem으로 반환하여 인벤토리로 되돌릴 수 있게 합니다.
    /// </summary>
    public bool TryEquipFromInventory(InventoryItem item, out InventoryItem replacedItem)
    {
        replacedItem = null;
        if (item == null || item.ItemData == null)
            return false;

        NYHEquipmentSlotKey key = GetDefaultKey(item.ItemData);
        return TryEquipInternal(new NYHEquipmentItemInstance(item.ItemData, item.ItemInstance), item, key, out replacedItem);
    }

    /// <summary>
    /// 인벤토리 UI 상의 아이템을 명시된 슬롯에 강제 지정하여 장착 시도합니다.
    /// </summary>
    public bool TryEquipFromInventory(InventoryItem item, EquipmentSlot slot, int slotIndex, out InventoryItem replacedItem)
    {
        replacedItem = null;
        if (item == null || item.ItemData == null)
            return false;

        NYHEquipmentProfile profile = FindProfile(item.ItemData);
        EquipmentSlot resolvedSlot = ResolveSlot(item.ItemData, profile);
        if (resolvedSlot != slot)
        {
            Debug.LogWarning($"NYHEquipmentController: {item.ItemData.Name} 아이템은 {slot} 슬롯에 장착할 수 없습니다.");
            return false;
        }

        NYHEquipmentSlotKey key = new NYHEquipmentSlotKey(slot, slotIndex);
        return TryEquipInternal(new NYHEquipmentItemInstance(item.ItemData, item.ItemInstance), item, key, out replacedItem);
    }

    /// <summary>
    /// 지정된 슬롯의 인덱스 0번 장비를 해제합니다.
    /// </summary>
    public bool Unequip(EquipmentSlot slot)
    {
        return Unequip(slot, 0, out _);
    }

    /// <summary>
    /// 특정 슬롯과 인덱스에 있는 장비를 해제하고 적용되던 스탯 보너스를 롤백합니다.
    /// 반환되는 itemView는 인벤토리로 다시 들어갈 때 사용됩니다.
    /// </summary>
    public bool Unequip(EquipmentSlot slot, int slotIndex, out InventoryItem itemView)
    {
        NYHEquipmentSlotKey key = new NYHEquipmentSlotKey(slot, slotIndex);
        itemView = null;

        if (!_equippedItems.TryGetValue(key, out NYHEquipmentItemInstance item))
            return false;

        // 장착 해제 시 해당 장비가 올려주던 스탯을 다시 빼줌 (-1)
        if (_appliedProfiles.TryGetValue(key, out NYHEquipmentProfile profile))
        {
            _appliedProfiles.Remove(key);
        }

        _equippedItems.Remove(key);
        if (_equippedViews.TryGetValue(key, out itemView))
            _equippedViews.Remove(key);

        NotifyRingUnequip(item);

        // 장비가 빠지면 그 장비가 주던 최종 초 단위 보정도 즉시 빠져야 한다.
        // 기존 정수 스탯과 finalStatModifiers 모두 장착 중인 전체 장비를 다시 훑어서 새 목록으로 교체한다.
        RebuildEquipmentStats("[해제]", item);
        RebuildFinalStatModifiers("[해제]", item);

        OnUnequipped?.Invoke(slot, item);
        OnInventoryItemUnequipped?.Invoke(slot, slotIndex, itemView);
        return true;
    }

    /// <summary>
    /// 지정된 슬롯(인덱스 0번)에 장착된 장비 인스턴스를 가져옵니다.
    /// </summary>
    public bool TryGetEquipped(EquipmentSlot slot, out NYHEquipmentItemInstance item)
    {
        return TryGetEquipped(slot, 0, out item);
    }

    /// <summary>
    /// 특정 슬롯과 인덱스에 장착된 장비 인스턴스를 가져옵니다.
    /// </summary>
    public bool TryGetEquipped(EquipmentSlot slot, int slotIndex, out NYHEquipmentItemInstance item)
    {
        return _equippedItems.TryGetValue(new NYHEquipmentSlotKey(slot, slotIndex), out item);
    }

    /// <summary>
    /// 특정 슬롯과 인덱스에 장착된 장비의 인벤토리 UI 객체를 가져옵니다.
    /// </summary>
    public bool TryGetEquippedInventoryItem(EquipmentSlot slot, int slotIndex, out InventoryItem item)
    {
        return _equippedViews.TryGetValue(new NYHEquipmentSlotKey(slot, slotIndex), out item);
    }

    /// <summary>
    /// 장착 중인 InventoryItem의 내부 상태가 바뀌었음을 알립니다.
    /// 강화/감정/정화는 아이템 오브젝트를 교체하지 않고 ItemInstance 값만 바꾸므로,
    /// 장착/해제 이벤트 대신 이 이벤트로 스탯 UI와 장비 슬롯 UI를 다시 계산하게 합니다.
    /// </summary>
    public bool NotifyEquippedInventoryItemStateChanged(InventoryItem changedItem)
    {
        if (changedItem == null)
            return false;

        foreach (KeyValuePair<NYHEquipmentSlotKey, InventoryItem> pair in _equippedViews)
        {
            if (pair.Value != changedItem)
                continue;

            // 강화 주문서, 감정, 정화, 마법부여처럼 장착은 그대로인데 ItemInstance 내부 값만 바뀐 경우가 있다.
            // 이때 해제/재장착 이벤트가 없으므로 여기서 최종 보정을 다시 계산해 강화 수치를 반영한다.
            RebuildEquipmentStats("[강화/상태변경]", _equippedItems.TryGetValue(pair.Key, out NYHEquipmentItemInstance changedEquipped) ? changedEquipped : null);
            RebuildFinalStatModifiers("[강화/상태변경]", _equippedItems.TryGetValue(pair.Key, out changedEquipped) ? changedEquipped : null);
            // 반지는 Profile 시스템이 아닌 OnEquip()으로 스탯을 관리하므로 별도로 델타 갱신
            if (changedEquipped?.SourceInstance is RingInstance ring)
                ring.RefreshEquippedStat();
            OnInventoryItemStateChanged?.Invoke(pair.Key.Slot, pair.Key.Index, changedItem);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 장착의 실제 내부 로직을 처리하는 코어 메서드입니다.
    /// 기존 장비 해제, 새 장비 등록, 스탯 부여, 이벤트를 일괄 호출합니다.
    /// </summary>
    private bool TryEquipInternal(NYHEquipmentItemInstance item, InventoryItem inventoryView, NYHEquipmentSlotKey key, out InventoryItem replacedItem)
    {
        replacedItem = null;
        if (item == null || item.Data == null)
            return false;

        NYHEquipmentProfile profile = FindProfile(item.Data);
        EquipmentSlot slot = ResolveSlot(item.Data, profile);
        if (slot == EquipmentSlot.None)
        {
            Debug.LogWarning($"NYHEquipmentController: {item.Data.Name} 아이템은 장착 슬롯이 없어 장착할 수 없습니다.");
            return false;
        }

        if (slot != key.Slot)
        {
            Debug.LogWarning($"NYHEquipmentController: {item.Data.Name} 아이템은 {key.Slot} 슬롯에 장착할 수 없습니다.");
            return false;
        }

        if (!MeetsEquipRequirements(item.Data, profile, GetEnhanceLevel(item)))
            return false;

        // 타겟 슬롯에 이미 착용 중인 장비가 있을 경우 스왑을 위해 기존 장비 해제
        if (_equippedItems.TryGetValue(key, out NYHEquipmentItemInstance oldItem))
        {
            if (_appliedProfiles.TryGetValue(key, out NYHEquipmentProfile oldProfile))
            {
                _appliedProfiles.Remove(key);
            }

            if (_equippedViews.TryGetValue(key, out replacedItem))
                _equippedViews.Remove(key);

            NotifyRingUnequip(oldItem);
            OnUnequipped?.Invoke(key.Slot, oldItem);
            OnInventoryItemUnequipped?.Invoke(key.Slot, key.Index, replacedItem);
        }

        // 새 장비 등록
        _equippedItems[key] = item;

        if (inventoryView != null)
            _equippedViews[key] = inventoryView;

        // 새 장비 스탯 보너스 부여 (+1)
        if (profile != null)
        {
            _appliedProfiles[key] = profile;
        }

        // 새 장비가 들어온 뒤 최종 초 단위 보정도 갱신한다.
        // 같은 source(this)로 SetModifiers를 호출하기 때문에 이전 장비 보정과 중복 누적되지 않는다.
        RebuildEquipmentStats("[장착]", item);
        RebuildFinalStatModifiers("[장착]", item);
        NotifyRingEquip(item);

        OnEquipped?.Invoke(key.Slot, item);
        OnInventoryItemEquipped?.Invoke(key.Slot, key.Index, inventoryView);
        return true;
    }

    /// <summary>
    /// 원본 아이템 데이터와 매칭되는 장비 스탯 프로필(NYHEquipmentProfile)을 목록에서 찾습니다.
    /// </summary>
    private NYHEquipmentProfile FindProfile(ItemBase itemData)
    {
        for (int i = 0; i < equipmentProfiles.Count; i++)
        {
            NYHEquipmentProfile profile = equipmentProfiles[i];
            if (profile != null && profile.Matches(itemData))
                return profile;
        }

        return null;
    }

    /// <summary>
    /// 프로필이나 원본 데이터에서 이 장비가 들어갈 슬롯 부위(머리, 몸, 무기 등)를 추출합니다.
    /// </summary>
    private static EquipmentSlot ResolveSlot(ItemBase itemData, NYHEquipmentProfile profile)
    {
        if (profile != null)
            return profile.GetSlot();

        return itemData != null ? itemData.Slot : EquipmentSlot.None;
    }

    /// <summary>
    /// 장착하려는 장비의 기본 슬롯 키를 결정합니다.
    /// 반지(Ring)의 경우 0번이 차 있으면 자동으로 1번(보조) 슬롯을 반환합니다.
    /// </summary>
    private NYHEquipmentSlotKey GetDefaultKey(ItemBase itemData)
    {
        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);

        if (slot == EquipmentSlot.Ring && _equippedItems.ContainsKey(new NYHEquipmentSlotKey(slot, 0)))
            return new NYHEquipmentSlotKey(slot, 1);

        return new NYHEquipmentSlotKey(slot, 0);
    }

    /// <summary>
    /// 장착 시 플레이어의 힘(Strength)이나 레벨(Level)이 장비의 요구 조건을 충족하는지 검증합니다.
    /// </summary>
    private bool MeetsEquipRequirements(ItemBase itemData, NYHEquipmentProfile profile, int enhanceLevel = 0)
    {
        NYHPlayerStats stats = Stats;
        if (stats == null)
        {
            Debug.LogWarning("NYHEquipmentController: NYHPlayerStats를 찾지 못해 장비 스탯을 적용할 수 없습니다.");
            return false;
        }

        int requiredStrength = GetRequiredStrength(itemData, profile, enhanceLevel);
        if (stats.Strength < requiredStrength)
        {
            Debug.LogWarning($"NYHEquipmentController: 힘이 부족합니다. 필요 힘 {requiredStrength}, 현재 힘 {stats.Strength}");
            return false;
        }

        int requiredLevel = profile != null ? profile.RequiredLevel : 0;
        if (requiredLevel > 0)
        {
            PlayerLevelSystem levelSystem = LevelSystem;
            if (levelSystem == null)
            {
                Debug.LogWarning("NYHEquipmentController: PlayerLevelSystem을 찾지 못해 레벨 조건을 확인할 수 없습니다.");
                return false;
            }

            if (levelSystem.Level < requiredLevel)
            {
                Debug.LogWarning($"NYHEquipmentController: 레벨이 부족합니다. 필요 레벨 {requiredLevel}, 현재 레벨 {levelSystem.Level}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 프로필 또는 아이템 원본 데이터에서 요구 힘 수치를 가져옵니다.
    /// 강화 단계마다 요구 힘이 1씩 줄어들며, 0 밑으로는 내려가지 않습니다.
    /// </summary>
    private static int GetRequiredStrength(ItemBase itemData, NYHEquipmentProfile profile, int enhanceLevel = 0)
    {
        int baseRequiredStrength;
        if (profile != null && profile.RequiredStrength > 0)
            baseRequiredStrength = profile.RequiredStrength;
        else if (itemData is ArmorData armor)
            baseRequiredStrength = armor.CanUseStrength;
        else if (itemData is CloseWeapon closeWeapon)
            baseRequiredStrength = closeWeapon.CanUseStrength;
        else if (itemData is ThrowableWeapon throwableWeapon)
            baseRequiredStrength = throwableWeapon.CanUseStrength;
        else
            return 0;

        return Mathf.Max(0, baseRequiredStrength - enhanceLevel);
    }

    private void RebuildEquipmentStats(string logPrefix = null, NYHEquipmentItemInstance changedItem = null)
    {
        NYHPlayerStats stats = Stats;
        if (stats == null)
            return;

        Dictionary<NYHStatType, int> desiredTotals = new();

        foreach (KeyValuePair<NYHEquipmentSlotKey, NYHEquipmentItemInstance> pair in _equippedItems)
        {
            NYHEquipmentItemInstance item = pair.Value;
            if (item == null || item.Data == null)
                continue;

            NYHEquipmentProfile profile = FindProfile(item.Data);
            if (profile == null)
                continue;

            AddStatModifiers(desiredTotals, profile.StatModifiers);
            AddScalingStatModifiers(desiredTotals, profile.ScalingStatModifiers, GetEnhanceLevel(item));
        }

        ApplyEquipmentStatDiff(stats, desiredTotals, logPrefix, changedItem);
    }

    private static void AddStatModifiers(Dictionary<NYHStatType, int> totals, NYHEquipmentStatModifier[] modifiers)
    {
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Length; i++)
        {
            NYHEquipmentStatModifier modifier = modifiers[i];
            AddStatTotal(totals, modifier.statType, modifier.amount);
        }
    }

    private static void AddScalingStatModifiers(Dictionary<NYHStatType, int> totals, NYHEquipmentScalingStatModifier[] modifiers, int enhanceLevel)
    {
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Length; i++)
        {
            NYHEquipmentScalingStatModifier modifier = modifiers[i];
            AddStatTotal(totals, modifier.statType, modifier.Evaluate(enhanceLevel));
        }
    }

    private static void AddStatTotal(Dictionary<NYHStatType, int> totals, NYHStatType statType, int amount)
    {
        if (amount == 0)
            return;

        totals.TryGetValue(statType, out int current);
        totals[statType] = current + amount;
    }

    private void ApplyEquipmentStatDiff(NYHPlayerStats stats, Dictionary<NYHStatType, int> desiredTotals, string logPrefix = null, NYHEquipmentItemInstance changedItem = null)
    {
        Dictionary<NYHStatType, int> deltas = new();

        foreach (KeyValuePair<NYHStatType, int> pair in _appliedEquipmentStatTotals)
        {
            desiredTotals.TryGetValue(pair.Key, out int desiredAmount);
            int delta = desiredAmount - pair.Value;
            if (delta != 0)
            {
                stats.AddStat(pair.Key, delta);
                deltas[pair.Key] = delta;
            }
        }

        foreach (KeyValuePair<NYHStatType, int> pair in desiredTotals)
        {
            if (_appliedEquipmentStatTotals.ContainsKey(pair.Key))
                continue;

            if (pair.Value != 0)
            {
                stats.AddStat(pair.Key, pair.Value);
                deltas[pair.Key] = pair.Value;
            }
        }

        _appliedEquipmentStatTotals.Clear();
        foreach (KeyValuePair<NYHStatType, int> pair in desiredTotals)
        {
            if (pair.Value != 0)
                _appliedEquipmentStatTotals[pair.Key] = pair.Value;
        }

        LogStatChanges(logPrefix, changedItem, deltas);
    }

    /// <summary>
    /// 현재 장착 중인 모든 장비를 다시 읽어서 최종 행동 지연 보정 목록을 새로 만듭니다.
    ///
    /// 왜 "변경된 장비 하나만 더하고 빼는 방식"을 쓰지 않느냐면,
    /// 강화 주문서나 정화/마법부여처럼 장착 슬롯은 그대로인데 내부 ItemInstance 값만 바뀌는 경우가 있기 때문입니다.
    /// 매번 전체 장착 장비를 다시 읽으면 계산은 조금 더 하지만,
    /// 예전 강화 수치가 남거나 같은 반지 효과가 두 번 쌓이는 버그를 피할 수 있습니다.
    ///
    /// 흐름:
    /// 1. _equippedItems에서 현재 장착 중인 장비들을 가져옵니다.
    /// 2. 각 장비의 ItemBase와 매칭되는 NYHEquipmentProfile을 찾습니다.
    /// 3. 프로필의 FinalStatModifiers 배열을 읽습니다.
    /// 4. YJW 쪽 ItemInstance가 EquipmentInstance라면 enhanceLevel을 읽어서 강화당 보정까지 계산합니다.
    /// 5. 계산이 끝난 값을 NYHFinalStatModifierController에 source=this로 한 번에 등록합니다.
    /// </summary>
    private void RebuildFinalStatModifiers(string logPrefix = null, NYHEquipmentItemInstance changedItem = null)
    {
        NYHFinalStatModifierController modifierController = FinalStatModifiers;
        if (modifierController == null)
            return;

        List<NYHFinalStatModifierRuntime> runtimeModifiers = new();

        foreach (KeyValuePair<NYHEquipmentSlotKey, NYHEquipmentItemInstance> pair in _equippedItems)
        {
            NYHEquipmentItemInstance item = pair.Value;
            if (item == null || item.Data == null)
                continue;

            // YJW의 원본 ItemBase와 NYH의 장비 프로필을 매칭한다.
            // 실제 수치 튜닝은 RingData가 아니라 NYHEquipmentProfile 쪽 finalStatModifiers에 들어 있다.
            NYHEquipmentProfile profile = FindProfile(item.Data);
            int enhanceLevel = GetEnhanceLevel(item);
            NYHFinalStatModifier[] finalModifiers = profile != null ? profile.FinalStatModifiers : null;
            if (finalModifiers != null)
            {
                for (int i = 0; i < finalModifiers.Length; i++)
                {
                    NYHFinalStatModifier modifier = finalModifiers[i];
                    float flatSeconds = modifier.Evaluate(enhanceLevel);
                    if (Mathf.Approximately(flatSeconds, 0f))
                        continue;

                    runtimeModifiers.Add(new NYHFinalStatModifierRuntime(modifier.target, flatSeconds));
                }
            }

            AddCurseFinalStatModifiers(item, runtimeModifiers);
        }

        // 장착 장비 전체를 다시 계산해서 강화/저주/마법부여 변경 후에도 누적 오차가 남지 않게 한다.
        modifierController.SetModifiers(this, runtimeModifiers);
        LogFinalStatChanges(logPrefix, changedItem, runtimeModifiers);
    }

    /// <summary>
    /// 장착된 NYHEquipmentItemInstance 안에서 YJW ItemInstance를 꺼내 강화 수치를 읽습니다.
    ///
    /// YJW 쪽에서 반지를 만들 때 RingData.CreateInstance()가 EquipmentInstance를 반환하면,
    /// 인벤토리 아이템은 그 EquipmentInstance를 들고 있게 됩니다.
    /// NYH 장비 시스템은 InventoryItem.ItemInstance를 SourceInstance로 보관하고,
    /// 여기서 SourceInstance가 EquipmentInstance인지 확인한 뒤 enhanceLevel을 읽습니다.
    ///
    /// SourceInstance가 없거나 EquipmentInstance가 아니면 강화 수치를 알 수 없으므로 0강으로 취급합니다.
    /// </summary>
    private static int GetEnhanceLevel(NYHEquipmentItemInstance item)
    {
        if (item != null && item.SourceInstance is EquipmentInstance equipmentInstance)
            return equipmentInstance.enhanceLevel;

        return 0;
    }

    private void LogStatChanges(string logPrefix, NYHEquipmentItemInstance changedItem, Dictionary<NYHStatType, int> deltas)
    {
        if (!logEquipmentStatChanges || string.IsNullOrEmpty(logPrefix) || deltas == null || deltas.Count == 0)
            return;

        StringBuilder builder = new();
        builder.Append(logPrefix)
            .Append(' ')
            .Append(GetEquipmentLogName(changedItem))
            .Append(": ");

        bool appended = false;
        foreach (KeyValuePair<NYHStatType, int> pair in deltas)
        {
            if (appended)
                builder.Append(", ");

            builder.Append(GetStatDisplayName(pair.Key))
                .Append(' ')
                .Append(FormatSignedInt(pair.Value));
            appended = true;
        }

        Debug.Log(builder.ToString());
    }

    private void LogFinalStatChanges(string logPrefix, NYHEquipmentItemInstance changedItem, List<NYHFinalStatModifierRuntime> runtimeModifiers)
    {
        Dictionary<NYHFinalStatTarget, float> desiredTotals = BuildFinalStatTotals(runtimeModifiers);
        Dictionary<NYHFinalStatTarget, float> deltas = new();

        foreach (KeyValuePair<NYHFinalStatTarget, float> pair in _appliedEquipmentFinalStatTotals)
        {
            desiredTotals.TryGetValue(pair.Key, out float desiredAmount);
            float delta = desiredAmount - pair.Value;
            if (!Mathf.Approximately(delta, 0f))
                deltas[pair.Key] = delta;
        }

        foreach (KeyValuePair<NYHFinalStatTarget, float> pair in desiredTotals)
        {
            if (_appliedEquipmentFinalStatTotals.ContainsKey(pair.Key))
                continue;

            if (!Mathf.Approximately(pair.Value, 0f))
                deltas[pair.Key] = pair.Value;
        }

        _appliedEquipmentFinalStatTotals.Clear();
        foreach (KeyValuePair<NYHFinalStatTarget, float> pair in desiredTotals)
        {
            if (!Mathf.Approximately(pair.Value, 0f))
                _appliedEquipmentFinalStatTotals[pair.Key] = pair.Value;
        }

        if (!logEquipmentStatChanges || string.IsNullOrEmpty(logPrefix) || deltas.Count == 0)
            return;

        StringBuilder builder = new();
        builder.Append(logPrefix)
            .Append(' ')
            .Append(GetEquipmentLogName(changedItem))
            .Append(": ");

        bool appended = false;
        foreach (KeyValuePair<NYHFinalStatTarget, float> pair in deltas)
        {
            if (appended)
                builder.Append(", ");

            builder.Append(GetFinalStatDisplayName(pair.Key))
                .Append(' ')
                .Append(FormatSignedSeconds(pair.Value));
            appended = true;
        }

        Debug.Log(builder.ToString());
    }

    private static Dictionary<NYHFinalStatTarget, float> BuildFinalStatTotals(List<NYHFinalStatModifierRuntime> runtimeModifiers)
    {
        Dictionary<NYHFinalStatTarget, float> totals = new();
        if (runtimeModifiers == null)
            return totals;

        for (int i = 0; i < runtimeModifiers.Count; i++)
        {
            NYHFinalStatModifierRuntime modifier = runtimeModifiers[i];
            totals.TryGetValue(modifier.Target, out float current);
            totals[modifier.Target] = current + modifier.FlatSeconds;
        }

        return totals;
    }

    private static string GetEquipmentLogName(NYHEquipmentItemInstance item)
    {
        if (item == null || item.Data == null)
            return "장비";

        return $"+{GetEnhanceLevel(item)}강 {item.Data.Name}";
    }

    private static string GetStatDisplayName(NYHStatType statType)
    {
        return statType switch
        {
            NYHStatType.Health => "최대 체력",
            NYHStatType.Strength => "힘",
            NYHStatType.MoveSpeed => "이동속도",
            NYHStatType.AttackSpeed => "공격속도",
            NYHStatType.Defense => "방어력",
            NYHStatType.Evasion => "회피",
            NYHStatType.Accuracy => "정확도",
            NYHStatType.Hunger => "배고픔",
            NYHStatType.Level => "레벨",
            _ => statType.ToString()
        };
    }

    private static string GetFinalStatDisplayName(NYHFinalStatTarget target)
    {
        return target switch
        {
            NYHFinalStatTarget.MoveActionDelay => "이동 딜레이",
            NYHFinalStatTarget.AttackActionDelay => "공격 딜레이",
            _ => target.ToString()
        };
    }

    private static string FormatSignedInt(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static string FormatSignedSeconds(float value)
    {
        return value >= 0f ? $"+{value:0.###}초" : $"{value:0.###}초";
    }

    private static void AddCurseFinalStatModifiers(NYHEquipmentItemInstance item, List<NYHFinalStatModifierRuntime> runtimeModifiers)
    {
        if (item == null || runtimeModifiers == null)
            return;

        if (item.SourceInstance is not EquipmentInstance equipmentInstance || equipmentInstance.curse == null)
            return;

        // 이번 2차 범위에서는 YJW CurseData 실행 함수는 호출하지 않고 Type만 읽는다.
        // 둔화 저주는 장비가 켜져 있는 동안 최종 이동 행동 지연에 +0.2초를 더한다.
        if (equipmentInstance.curse.Type == CurseType.Slow)
        {
            runtimeModifiers.Add(new NYHFinalStatModifierRuntime(
                NYHFinalStatTarget.MoveActionDelay,
                SlowCurseMoveActionDelayPenalty));
        }
    }

    /// <summary>
    /// 장비 컨트롤러가 꺼질 때 자신이 등록했던 최종 보정을 제거합니다.
    /// 씬 전환이나 플레이어 비활성화 중에 이전 보정값이 컨트롤러 안에 남는 것을 막기 위한 정리 작업입니다.
    /// </summary>
    private void OnDisable()
    {
        NotifyAllRingUnequip();

        if (Stats != null)
            ApplyEquipmentStatDiff(Stats, new Dictionary<NYHStatType, int>());

        if (finalStatModifierController != null)
            finalStatModifierController.ClearModifiers(this);

        _appliedEquipmentFinalStatTotals.Clear();
    }

    /// <summary>
    /// 장비 컨트롤러가 다시 켜졌을 때 현재 장착 상태를 기준으로 최종 보정을 다시 등록합니다.
    /// OnDisable에서 지운 값을 복구하기 위한 짝 함수입니다.
    /// </summary>
    private void OnEnable()
    {
        RebuildEquipmentStats();
        RebuildFinalStatModifiers();
        NotifyAllRingEquip();
    }

    // 반지 장착 시 RingInstance.OnEquip() 호출
    private static void NotifyRingEquip(NYHEquipmentItemInstance item)
    {
        if (item?.SourceInstance is RingInstance ring)
            ring.OnEquip();
    }

    // 반지 해제 시 RingInstance.OnUnequip() 호출
    private static void NotifyRingUnequip(NYHEquipmentItemInstance item)
    {
        if (item?.SourceInstance is RingInstance ring)
            ring.OnUnequip();
    }

    private void NotifyAllRingEquip()
    {
        foreach (KeyValuePair<NYHEquipmentSlotKey, NYHEquipmentItemInstance> pair in _equippedItems)
            NotifyRingEquip(pair.Value);
    }

    private void NotifyAllRingUnequip()
    {
        foreach (KeyValuePair<NYHEquipmentSlotKey, NYHEquipmentItemInstance> pair in _equippedItems)
            NotifyRingUnequip(pair.Value);
    }
}

