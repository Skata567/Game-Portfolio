using PrototypeRT;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NYHEquipmentSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("슬롯 설정")]
    [Tooltip("이 UI 슬롯이 받을 장비 슬롯 종류입니다.")]
    [SerializeField] private EquipmentSlot slot = EquipmentSlot.None;

    [Tooltip("같은 슬롯이 여러 개일 때 구분하는 번호입니다. 반지 2칸은 0, 1로 설정합니다.")]
    [SerializeField, Min(0)] private int slotIndex;

    [Header("참조")]
    [Tooltip("장비 장착/해제와 스탯 적용을 담당하는 컨트롤러입니다.")]
    [SerializeField] private NYHEquipmentController controller;

    [Tooltip("현재 마우스로 들고 있는 인벤토리 아이템을 넘겨받기 위한 인벤토리 컨트롤러입니다.")]
    [SerializeField] private InventoryController inventoryController;

    // 주문서 타겟팅 클릭이 장비 해제/장착 클릭으로도 처리되는 것을 막기 위해 확인합니다.
    [SerializeField] private InventoryTargetingController targetingController;

    [Tooltip("장착된 아이템 아이콘을 표시할 Image입니다. 슬롯 배경이 아닌 자식 Image를 연결하는 것을 권장합니다.")]
    [SerializeField] private Image iconImage;

    public EquipmentSlot Slot => slot;
    public int SlotIndex => slotIndex;

    private bool _isPointerOver;

    private void Awake()
    {
        ResolveReferences();
        ConfigureIconImage();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (controller != null)
        {
            controller.OnInventoryItemEquipped += OnEquipmentChanged;
            controller.OnInventoryItemUnequipped += OnEquipmentChanged;
            controller.OnInventoryItemStateChanged += OnEquipmentChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        HideTooltip();

        if (controller != null)
        {
            controller.OnInventoryItemEquipped -= OnEquipmentChanged;
            controller.OnInventoryItemUnequipped -= OnEquipmentChanged;
            controller.OnInventoryItemStateChanged -= OnEquipmentChanged;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        ResolveReferences();

        // 감정/강화/정화 대상 선택 중에는 이 클릭을 주문서 타겟팅 전용으로 둡니다.
        // 타겟팅 성공 직후 같은 프레임에도 장비가 손에 들리지 않게 BlocksPointerInput을 봅니다.
        if (targetingController != null && targetingController.BlocksPointerInput)
            return;

        if (inventoryController != null && inventoryController.SelectedItem == null)
        {
            TryUnequipToHand();
            return;
        }

        TryEquipHeldItem();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        HideTooltip();
    }

    private void TryEquipHeldItem()
    {
        ResolveReferences();

        if (controller == null || inventoryController == null)
            return;

        InventoryItem selectedItem = inventoryController.SelectedItem;
        if (selectedItem == null || selectedItem.ItemData == null)
            return;

        if (!controller.TryEquipFromInventory(selectedItem, slot, slotIndex, out InventoryItem replacedItem))
            return;

        if (!inventoryController.TryConsumeSelectedItem(selectedItem))
        {
            // 장착 컨트롤러만 바뀌고 인벤토리 손 상태 정리에 실패한 예외 상황을 되돌립니다.
            controller.Unequip(slot, slotIndex, out _);
            return;
        }

        // 장착 중인 아이템 UI는 슬롯 아이콘으로만 보여주고, 원본 InventoryItem은 교체/해제용으로 보관합니다.
        selectedItem.gameObject.SetActive(false);
        Refresh();

        if (replacedItem != null)
            inventoryController.HoldItem(replacedItem, null, 0, 0);
    }

    private void TryUnequipToHand()
    {
        if (controller == null || inventoryController == null)
            return;

        if (!controller.TryGetEquippedInventoryItem(slot, slotIndex, out InventoryItem equippedItem) || equippedItem == null)
            return;

        if (!controller.Unequip(slot, slotIndex, out InventoryItem itemView) || itemView == null)
            return;

        // 장착 해제한 아이템은 원래 그리드 좌표가 없으므로 빈 인벤토리 칸을 찾을 때까지 손에 들게 둡니다.
        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.EquipmentPicked);
        inventoryController.HoldItem(itemView, null, 0, 0);
        Refresh();
    }

    private void Refresh()
    {
        if (iconImage == null)
            return;

        Sprite icon = null;
        if (controller != null && controller.TryGetEquipped(slot, slotIndex, out NYHEquipmentItemInstance equipped))
            icon = equipped.Data != null ? equipped.Data.Icon : null;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.preserveAspect = true;
    }

    private void OnEquipmentChanged(EquipmentSlot changedSlot, int changedIndex, InventoryItem item)
    {
        if (changedSlot != slot || changedIndex != slotIndex)
            return;

        Refresh();

        if (_isPointerOver)
            ShowTooltip();
    }

    private void ShowTooltip()
    {
        if (ToolTipController.Instance == null)
            return;

        string tooltipText = GetEquippedTooltipText();
        if (string.IsNullOrWhiteSpace(tooltipText))
        {
            ToolTipController.Instance.Hide();
            return;
        }

        ToolTipController.Instance.Show(tooltipText);
    }

    private void HideTooltip()
    {
        if (ToolTipController.Instance == null)
            return;

        ToolTipController.Instance.Hide();
    }

    private string GetEquippedTooltipText()
    {
        ResolveReferences();

        if (controller == null)
            return null;

        // 인벤토리에서 장착된 장비는 원본 ItemInstance가 상태(강화/저주/감정)를 가장 정확히 들고 있다.
        if (controller.TryGetEquippedInventoryItem(slot, slotIndex, out InventoryItem itemView)
            && itemView != null
            && itemView.ItemInstance != null)
        {
            return itemView.ItemInstance.GetToolTipStats();
        }

        if (!controller.TryGetEquipped(slot, slotIndex, out NYHEquipmentItemInstance equipped) || equipped == null)
            return null;

        if (equipped.SourceInstance != null)
            return equipped.SourceInstance.GetToolTipStats();

        return equipped.Data != null ? equipped.Data.GetToolTipStats() : null;
    }

    private void ResolveReferences()
    {
        if (controller == null)
            controller = FindFirstObjectByType<NYHEquipmentController>();

        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryController>();

        if (targetingController == null)
            targetingController = FindFirstObjectByType<InventoryTargetingController>();

        if (iconImage == null)
            iconImage = FindIconImage();
    }

    private Image FindIconImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].transform != transform)
                return images[i];
        }

        return GetComponent<Image>();
    }

    private void ConfigureIconImage()
    {
        if (iconImage == null)
            return;

        // 표시용 자식 아이콘이 클릭 판정을 가로채면 슬롯 OnPointerClick이 흔들리므로 꺼둡니다.
        if (iconImage.transform != transform)
            iconImage.raycastTarget = false;

        iconImage.preserveAspect = true;
    }
}
