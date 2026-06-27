using System;
using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 10칸 퀵슬롯 그리드를 관리하고 1~0 키 입력으로 아이템을 사용합니다.
    /// 퀵슬롯에 들어간 아이템은 일반 인벤토리에서 빠져 실제로 이 그리드에 배치된 InventoryItem입니다.
    /// 
    /// 화면 하단의 빠른 사용 슬롯(Quick Slot) 시스템을 전담하는 클래스입니다.
    /// 키보드 숫자키 입력(1~0)을 감지하여 인벤토리 창을 열지 않고도 등록된 소모품 등을 즉시 사용할 수 있도록 지원하며,
    /// 메인 인벤토리와 분리된 별도의 10x1 크기 ItemGrid를 통해 데이터를 동기화합니다.
    /// </summary>
    public class QuickSlotController : MonoBehaviour
    {
        // 키보드 1~0에 대응하는 고정 슬롯 수입니다. UI 배열과 ItemGrid 크기가 이 값을 기준으로 맞춰집니다.
        private const int SlotCount = 10;

        [Header("퀵슬롯 그리드")]
        [Tooltip("10x1 크기의 퀵슬롯 전용 ItemGrid입니다. ItemGrid의 Accept Only One By One Items 옵션을 켜야 합니다.")]
        [SerializeField] private ItemGrid quickSlotGrid;

        [Tooltip("켜면 시작 시 quickSlotGrid를 10x1 크기로 맞춥니다.")]
        [SerializeField] private bool configureGridSize = true;

        [Header("사용 딜레이")]
        [Tooltip("Safe Room 밖에서 퀵슬롯 아이템을 사용한 뒤 다음 행동까지 막는 시간입니다.")]
        [SerializeField, Min(0f)] private float useActionDelay = 1f;

        [Header("참조")]
        [Tooltip("플레이어가 소유한 InventoryItem 목록을 관리하는 인벤토리 컨트롤러입니다. 사용한 아이템을 목록에서도 제거합니다.")]
        [SerializeField] private InventoryController inventoryController;

        private GridPlayer _player;
        private bool _inputSubscribed;

        /// <summary>
        /// 퀵슬롯 배치/사용/삭제 등 표시가 바뀌는 순간 HUD가 갱신할 수 있도록 알립니다.
        /// </summary>
        public event Action OnQuickSlotsChanged;
        public event Action<int> OnSelectedSlotChanged;

        /// <summary>
        /// 외부에서 퀵슬롯 전용 그리드가 필요할 때 읽는 접근자입니다.
        /// 예: 인벤토리 드래그 시스템이 이 그리드에 1x1 아이템만 놓게 할 때 사용합니다.
        /// </summary>
        public ItemGrid QuickSlotGrid => quickSlotGrid;
        public int SelectedIndex { get; private set; }

        /// <summary>
        /// 씬에 배치된 참조를 준비하고, 퀵슬롯 그리드의 기본 크기를 보정합니다.
        /// 이 단계에서 그리드/인벤토리/플레이어를 찾아두면 Update에서 매번 찾지 않아도 됩니다.
        /// </summary>
        private void Awake()
        {
            // 인스펙터 연결이 빠졌을 때를 대비해 1x1 전용 그리드를 퀵슬롯으로 자동 탐색합니다.
            if (quickSlotGrid == null)
                quickSlotGrid = FindQuickSlotGrid();

            // 씬에서 그리드 크기를 잘못 잡아도 퀵슬롯은 항상 1줄 10칸 규칙을 유지합니다.
            if (configureGridSize && quickSlotGrid != null)
                quickSlotGrid.ConfigureSize(SlotCount, 1);

            // 사용한 아이템은 ItemGrid뿐 아니라 인벤토리 소유 목록에서도 빼야 하므로 컨트롤러 참조가 필요합니다.
            if (inventoryController == null)
                inventoryController = FindFirstObjectByType<InventoryController>();

            // 행동 딜레이, 사망 여부, Safe Room 상태를 확인하기 위해 플레이어 참조를 캐시합니다.
            _player = FindFirstObjectByType<GridPlayer>();
        }

        private void Start()
        {
            SubscribePlayerInput();
            OnSelectedSlotChanged?.Invoke(SelectedIndex);
        }

        /// <summary>
        /// 퀵슬롯 그리드의 변경 이벤트를 구독합니다.
        /// 아이템을 드래그로 넣거나 빼면 ItemGrid.OnGridChanged가 발생하고, 그 신호를 HUD 갱신 이벤트로 변환합니다.
        /// </summary>
        private void OnEnable()
        {
            ResolveInventoryController();

            if (quickSlotGrid != null)
                quickSlotGrid.OnGridChanged += RaiseQuickSlotsChanged;

            // Count만 바뀌는 스택 병합은 ItemGrid 배치 이벤트가 안 뜨므로 인벤토리 변경도 같이 듣습니다.
            if (inventoryController != null)
                inventoryController.OnInventoryChanged += RaiseQuickSlotsChanged;

            // 시작 시점에 HUD가 빈 상태로 남지 않게 현재 그리드 상태를 바로 발행합니다.
            RaiseQuickSlotsChanged();

            // 입력 시스템과 연동합니다.
            SubscribePlayerInput();

            OnSelectedSlotChanged?.Invoke(SelectedIndex);
        }

        /// <summary>
        /// 비활성화될 때 이벤트 구독을 해제합니다.
        /// UI/씬 전환 뒤에도 이전 컨트롤러가 이벤트를 받는 중복 호출을 막기 위한 정리입니다.
        /// </summary>
        private void OnDisable()
        {
            if (quickSlotGrid != null)
                quickSlotGrid.OnGridChanged -= RaiseQuickSlotsChanged;

            // 비활성화된 컨트롤러가 계속 이벤트를 받지 않도록 해제합니다.
            if (inventoryController != null)
                inventoryController.OnInventoryChanged -= RaiseQuickSlotsChanged;

            UnsubscribePlayerInput();
        }

        /// <summary>
        /// 숫자키 입력 이벤트가 발생했을 때 호출됩니다.
        /// 실제 사용 조건/소비 처리는 TryUseSlot에 맡깁니다.
        /// </summary>
        public void SelectSlot(int index)
        {
            int clampedIndex = Mathf.Clamp(index, 0, SlotCount - 1);
            if (SelectedIndex == clampedIndex)
                return;

            SelectedIndex = clampedIndex;
            OnSelectedSlotChanged?.Invoke(SelectedIndex);
        }

        public void MoveSelectedSlot(int delta)
        {
            if (delta == 0)
                return;

            SelectSlot(SelectedIndex + delta);
        }

        private void SubscribePlayerInput()
        {
            if (_inputSubscribed)
                return;

            if (_player == null)
                _player = FindFirstObjectByType<GridPlayer>();

            if (_player == null || _player.Input == null)
                return;

            _player.Input.OnQuickSlotNumberPressed += HandleQuickSlotNumberPressed;
            _player.Input.OnQuickSlotScroll += MoveSelectedSlot;
            _inputSubscribed = true;
        }

        private void UnsubscribePlayerInput()
        {
            if (!_inputSubscribed)
                return;

            if (_player != null && _player.Input != null)
            {
                _player.Input.OnQuickSlotNumberPressed -= HandleQuickSlotNumberPressed;
                _player.Input.OnQuickSlotScroll -= MoveSelectedSlot;
            }

            _inputSubscribed = false;
        }

        private void HandleQuickSlotNumberPressed(int index)
        {
            // 숫자키는 여전히 "선택 슬롯 변경" 역할을 먼저 수행합니다.
            // 단, 힐/힘 포션처럼 먹는 포션은 버튼만 눌러도 즉시 사용되도록 아래에서 한 번 더 검사합니다.
            SelectSlot(index);
            TryUseInstantConsumableSlot(index);
        }

        /// <summary>
        /// 지정한 퀵슬롯 번호에 들어 있는 InventoryItem을 반환합니다.
        /// 범위를 벗어나거나 그리드가 없으면 null을 반환해서 호출자가 안전하게 실패 처리할 수 있게 합니다.
        /// </summary>
        public InventoryItem GetItem(int slotIndex)
        {
            // 배열/그리드 접근 전에 범위를 먼저 막아 잘못된 키 입력이나 외부 호출을 방어합니다.
            if (quickSlotGrid == null || slotIndex < 0 || slotIndex >= SlotCount)
                return null;

            // 퀵슬롯은 10x1 그리드라서 x가 슬롯 번호, y는 항상 0입니다.
            return quickSlotGrid.GetItemAt(slotIndex, 0);
        }

        public bool TryUseSelectedSlotAt(Vector2Int targetCell)
        {
            return TryUseSelectedSlotAt(SelectedIndex, targetCell);
        }

        private bool TryUseInstantConsumableSlot(int slotIndex)
        {
            InventoryItem item = GetItem(slotIndex);
            if (item == null || item.ItemData == null)
                return false;

            if (!CanUseQuickSlot())
                return false;

            // 먹는 포션/횃불/생명파편처럼 타겟 지정이 필요 없는 소비 아이템만 숫자키로 즉시 사용합니다.
            // FirePotion/IcePotion 같은 투척 아이템은 선택만 하고, 기존처럼 좌클릭 타일 지정으로 던집니다.
            if (!IsInstantQuickSlotConsumable(item.ItemData))
                return false;

            item.ItemData.Use();
            return ConsumeUsedQuickSlotItem(item);
        }

        private bool TryUseSelectedSlotAt(int slotIndex, Vector2Int targetCell)
        {
            InventoryItem item = GetItem(slotIndex);
            if (item == null || item.ItemData == null)
                return false;

            if (!CanUseQuickSlot())
                return false;

            if (item.ItemData.ItemType == ItemType.Scroll)
                return false;

            if (item.ItemInstance is ThrowableWeaponInstance throwableWeapon)
                return TryThrowWeaponItem(item, throwableWeapon, targetCell);

            if (item.ItemData is IThrowable throwableData)
            {
                throwableData.Throw(targetCell);
                return ConsumeUsedQuickSlotItem(item);
            }

            if (IsEquipment(item.ItemData))
                return false;

            if (IsInstantQuickSlotConsumable(item.ItemData))
            {
                item.ItemData.Use();
                return ConsumeUsedQuickSlotItem(item);
            }

            return false;
        }

        private bool TryThrowWeaponItem(InventoryItem item, ThrowableWeaponInstance throwableWeapon, Vector2Int targetCell)
        {
            if (throwableWeapon.currentDurabilities == null || throwableWeapon.currentDurabilities.Count == 0)
                return false;

            int beforeCount = throwableWeapon.currentDurabilities.Count;
            throwableWeapon.Throw(targetCell);

            if (throwableWeapon.currentDurabilities.Count >= beforeCount)
                return false;

            if (throwableWeapon.currentDurabilities.Count == 0)
                return ConsumeUsedQuickSlotItem(item);

            ApplyUseDelay();
            RaiseQuickSlotsChanged();
            return true;
        }

        private bool ConsumeUsedQuickSlotItem(InventoryItem item)
        {
            if (inventoryController == null)
                ResolveInventoryController();

            if (inventoryController == null || item == null || item.ItemInstance == null)
                return false;

            if (item.Count > 1)
            {
                // 퀵슬롯 아이템은 실제로 quickSlotGrid에 배치되어 있으므로, 수량만 줄어드는 경우에는
                // 일반 인벤토리 그리드 제거 흐름을 타지 않고 현재 슬롯을 유지한 채 HUD만 즉시 갱신합니다.
                if (!item.TryDecreaseCount(1))
                    return false;

                inventoryController.RaiseInventoryChanged();
                ApplyUseDelay();
                RaiseQuickSlotsChanged();
                return true;
            }

            // 마지막 1개를 사용한 경우에는 퀵슬롯 그리드에서 직접 제거해야 합니다.
            // InventoryController.TryConsumeOneItem은 일반 인벤토리 그리드를 기준으로 제거할 수 있어서,
            // 퀵슬롯 HUD가 다음 이동/갱신 전까지 이전 아이콘을 들고 있는 문제가 생길 수 있습니다.
            inventoryController.ForgetItem(item);
            if (quickSlotGrid != null)
                quickSlotGrid.RemoveItem(item);

            ApplyUseDelay();
            RaiseQuickSlotsChanged();
            return true;
        }

        private static bool IsEquipment(ItemBase itemData)
        {
            return itemData != null && (itemData.ItemType == ItemType.Equipment || itemData.Slot != EquipmentSlot.None);
        }

        private static bool IsInstantQuickSlotConsumable(ItemBase itemData)
        {
            if (itemData == null || itemData is IThrowable)
                return false;

            return itemData is PotionData || itemData is TorchData || itemData is FragmentsOfLife;
        }

        /// <summary>
        /// 지정한 퀵슬롯 아이템 사용을 시도합니다.
        /// 성공하면 ItemBase.Use()를 호출하고, 사용한 InventoryItem을 인벤토리 목록과 퀵슬롯 그리드에서 제거합니다.
        /// </summary>
        public bool TryUseSlot(int slotIndex)
        {
            // 빈 슬롯이거나 아이템 데이터가 깨진 슬롯이면 사용할 대상이 없으므로 실패합니다.
            InventoryItem item = GetItem(slotIndex);
            if (item == null || item.ItemData == null) return false;

            // 플레이어 사망/행동 딜레이 중에는 아이템 효과와 턴 진행이 꼬일 수 있어 사용을 막습니다.
            if (!CanUseQuickSlot())
                return false;

            // 현재 ItemBase.Use()는 성공/실패 결과를 돌려주지 않으므로, 호출 후 소비까지 바로 진행합니다.
            // 나중에 UseResult가 생기면 성공한 경우에만 제거하도록 이 지점에서 분기하면 됩니다.
            // 퀵슬롯에는 대상 선택 UI가 없어서 스크롤은 여기서 즉시 사용하지 않습니다.
            if (item.ItemData.ItemType == ItemType.Scroll)
            {
                // 대상 지정 UI가 없는 퀵슬롯에서는 스크롤을 즉시 사용하지 않습니다.
                // 실수로 Use()만 호출되어 효과 없이 소비되는 일을 막기 위해 아무 반응 없이 실패 처리합니다.
                return false;
            }

            item.ItemData.Use();

            // 퀵슬롯 안의 스택 아이템은 마지막 1개가 되기 전까지 슬롯에서 제거하지 않습니다.
            // 수량만 줄이고 HUD 갱신 이벤트를 보내 기존 슬롯 배치는 유지합니다.
            if (item.ItemInstance != null && item.Count > 1)
            {
                // 퀵슬롯도 실제로는 InventoryItem을 공유하므로, 수량 감소는 ItemInstance가 아니라 Count에만 반영합니다.
                // 마지막 1개는 아래 제거 흐름으로 보내야 인벤토리 목록과 퀵슬롯 그리드가 같이 정리됩니다.
                item.TryDecreaseCount(1);
                ApplyUseDelay();
                OnQuickSlotsChanged?.Invoke();
                return true;
            }

            // InventoryController는 "플레이어가 소유한 아이템 목록"을 들고 있으므로, 사용한 아이템을 목록에서 잊게 합니다.
            inventoryController?.ForgetItem(item);

            // ItemGrid.RemoveItem은 슬롯 점유 정보 정리와 InventoryItem GameObject 파괴를 같이 처리합니다.
            quickSlotGrid.RemoveItem(item);

            // Safe Room 밖이라면 사용 후 바로 다음 행동하지 못하게 짧은 딜레이를 줍니다.
            ApplyUseDelay();

            // HUD는 ItemGrid 변경 이벤트를 받을 수도 있지만, 사용 직후 즉시 갱신되게 한 번 더 알립니다.
            RaiseQuickSlotsChanged();
            return true;
        }

        /// <summary>
        /// 현재 플레이어가 퀵슬롯 아이템을 사용할 수 있는 상태인지 검사합니다.
        /// 이 함수는 아이템 종류를 보지 않고, 플레이어 생존/행동 가능 여부만 판단합니다.
        /// </summary>
        private bool CanUseQuickSlot()
        {
            if (KeyBindingManager.ShouldBlockGameplayInput)
                return false;

            // 씬 로딩 순서 때문에 Awake에서 못 찾았거나 플레이어가 교체됐을 가능성을 대비합니다.
            if (_player == null)
                _player = FindFirstObjectByType<GridPlayer>();

            // 사망 상태거나 행동 딜레이 중이면 아이템 사용으로 턴/상태가 꼬이지 않게 막습니다.
            if (_player == null || _player.Health == null || _player.Health.IsDead)
                return false;

            // ActionDelay가 없으면 아직 딜레이 시스템이 연결되지 않은 상태로 보고 사용 자체는 허용합니다.
            return _player.ActionDelay == null || _player.ActionDelay.CanAct;
        }

        /// <summary>
        /// 퀵슬롯 사용 후 플레이어 행동 딜레이를 적용합니다.
        /// Safe Room처럼 TimeSystem이 멈춘 상태에서는 준비/정비 단계로 보고 딜레이를 주지 않습니다.
        /// </summary>
        private void ApplyUseDelay()
        {
            // 플레이어/딜레이 컴포넌트가 없거나 설정값이 0이면 딜레이 적용 대상이 아닙니다.
            if (_player == null || _player.ActionDelay == null || useActionDelay <= 0f)
                return;

            // Safe Room에서는 타이머가 멈춰 있으므로 퀵슬롯 사용도 행동 딜레이를 주지 않습니다.
            if (TimeSystem.Instance != null && !TimeSystem.Instance.IsRunning)
                return;

            _player.ActionDelay.StartDelay(useActionDelay);
        }

        /// <summary>
        /// 씬 안의 ItemGrid 중 퀵슬롯 후보를 찾습니다.
        /// 현재 규칙은 AcceptOnlyOneByOneItems가 켜진 그리드를 퀵슬롯으로 보는 방식입니다.
        /// </summary>
        private static ItemGrid FindQuickSlotGrid()
        {
            // 퀵슬롯 UI가 비활성 상태일 수도 있으므로 Resources.FindObjectsOfTypeAll로 비활성 오브젝트까지 찾습니다.
            ItemGrid[] grids = Resources.FindObjectsOfTypeAll<ItemGrid>();
            foreach (ItemGrid grid in grids)
            {
                // Project asset/prefab 원본까지 잡힐 수 있어, 실제 씬에 존재하는 오브젝트만 통과시킵니다.
                if (grid == null || !grid.gameObject.scene.IsValid()) continue;
                // 현재 프로젝트에서는 1x1 전용 ItemGrid가 퀵슬롯이라는 규칙을 씁니다.
                if (grid.AcceptOnlyOneByOneItems) return grid;
            }

            return null;
        }

        /// <summary>
        /// 퀵슬롯 상태 변경 이벤트를 발행합니다.
        /// 직접 HUD를 참조하지 않고 이벤트만 쏘는 이유는 UI 표시와 데이터 처리를 분리하기 위해서입니다.
        /// </summary>
        private void RaiseQuickSlotsChanged()
        {
            OnQuickSlotsChanged?.Invoke();
        }

        private void ResolveInventoryController()
        {
            // 인스펙터 연결이 빠진 씬에서도 퀵슬롯 HUD 수량 갱신 이벤트를 받을 수 있게 보정합니다.
            if (inventoryController == null)
                inventoryController = FindFirstObjectByType<InventoryController>();
        }
    }
}
