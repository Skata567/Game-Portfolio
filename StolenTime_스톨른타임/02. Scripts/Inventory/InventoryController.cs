using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 UI의 전체 입력 흐름을 관리하는 컨트롤러입니다.
    /// 아이템 생성/추가/삭제, 마우스 드래그, 배치, 하이라이트 갱신을 한 곳에서 조율합니다.
    /// 
    /// 플레이어 인벤토리 시스템을 총괄하는 핵심 클래스입니다.
    /// 아이템 선택 및 드래그 앤 드롭, 슬롯 간 이동, 신규 아이템 획득 시 빈칸 자동 배치 등 
    /// 인벤토리 내에서 발생하는 모든 상호작용과 데이터 흐름을 중앙에서 제어합니다.
    /// </summary>
    [RequireComponent(typeof(InventoryHighlight))]
    public class InventoryController : MonoBehaviour
    {
        [Header("UI 프리팹")]
        [Tooltip("인벤토리 칸에 생성할 아이템 UI 프리팹입니다. InventoryItem 컴포넌트와 Image가 붙어 있어야 합니다.")]
        [SerializeField] private InventoryItem itemPrefab;

        [Tooltip("아이템 UI를 생성할 Canvas Transform입니다. 비워두면 부모 Canvas를 자동으로 찾습니다.")]
        [SerializeField] private Transform canvasTransform;

        [Tooltip("아이템 자동 추가에 사용할 기본 인벤토리 그리드입니다. 퀵슬롯 그리드가 아닌 큰 가방 그리드를 연결하세요.")]
        [SerializeField] private ItemGrid defaultInventoryGrid;

        [Header("아이템 사용")]
        [Tooltip("아이템을 들고 다시 클릭했을 때 더블클릭으로 인정할 최대 시간입니다.")]
        [SerializeField, Min(0.05f)] private float doubleClickMaxDelay = 0.25f;

        [Tooltip("더블클릭 아이템 사용 분기를 담당하는 컨트롤러입니다. 비워두면 런타임에 자동으로 붙입니다.")]
        [SerializeField] private InventoryItemUseController itemUseController;

        [Tooltip("주문서 대상 지정과 투척 포션 조준을 담당하는 컨트롤러입니다. 비워두면 런타임에 자동으로 붙입니다.")]
        [SerializeField] private InventoryTargetingController targetingController;

        [Header("아이템 버리기")]
        [Tooltip("이 영역 밖에 들고 있는 아이템을 놓으면 플레이어 발밑에 버립니다. 인벤토리 전체 패널 RectTransform을 연결하세요.")]
        [SerializeField] private RectTransform inventoryPanelRect;

        [Tooltip("바닥에 버린 아이템과 줍기 순서를 관리하는 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private GroundItemController groundItemController;

        // 현재 마우스가 올라간 인벤토리 그리드입니다. 여러 그리드를 지원할 수 있도록 포인터 진입/이탈 때 갱신합니다.
        private ItemGrid _selectedGrid;

        // 마우스로 집어서 드래그 중인 아이템입니다. null이면 손에 들고 있는 아이템이 없는 상태입니다.
        private InventoryItem _selectedItem;

        // 배치할 위치에 이미 아이템이 하나만 겹쳐 있을 때, 교체 드래그를 위해 잠시 보관합니다.
        private InventoryItem _overlapItem;

        // 드래그 중인 아이템의 RectTransform 캐시입니다. 매 프레임 GetComponent를 피하기 위해 보관합니다.
        private RectTransform _dragRect;

        private InventoryHighlight _inventoryHighlight;

        // 플레이어가 실제로 소유한 인벤토리 아이템 UI 목록입니다. 판매/장착 UI 등에서 읽어갈 수 있습니다.
        private readonly List<InventoryItem> _items = new();

        // 상점/대화/다른 UI가 열렸을 때 인벤토리 클릭 처리를 잠깐 막기 위한 플래그입니다.
        private bool _isPointerInputBlocked;

        // 인벤토리 패널이 열려 있는지 기록합니다.
        // 패널이 닫힌 동안에는 클릭/하이라이트/드래그 입력을 처리하지 않기 위해 사용합니다.
        private bool _isInventoryOpen;

        // 드래그 시작 시점의 "원래 칸"을 저장합니다.
        // 화면 좌표를 저장하지 않고 ItemGrid + 그리드 X/Y를 저장하는 이유:
        // Canvas 해상도, 앵커, 스케일이 달라도 아이템이 실제로 돌아가야 할 위치는 "몇 번째 칸"이기 때문입니다.
        private ItemGrid _dragStartGrid;
        private int _dragStartX;
        private int _dragStartY;

        private InventoryItem _lastPickedItem;
        private float _lastPickTime;

        /// <summary>아이템 추가/삭제/재배치 등 인벤토리 구성이 바뀔 때 호출됩니다.</summary>
        public event Action OnInventoryChanged;

        /// <summary>새 아이템이 인벤토리에 성공적으로 들어왔을 때, 추가된 아이템 UI와 함께 호출됩니다.</summary>
        public event Action<InventoryItem> OnItemAdded;

        public IReadOnlyList<InventoryItem> Items => _items;
        public InventoryItem SelectedItem => _selectedItem;

        public ItemGrid SelectedGrid
        {
            get => _selectedGrid;
            set
            {
                if (_selectedGrid == value) return;

                _selectedGrid = value;
                _inventoryHighlight?.SetParent(value);
            }
        }

        public void ClearSelectedGridIfCurrent(ItemGrid grid)
        {
            if (_selectedGrid != grid) return;
            SelectedGrid = null;
        }

        /// <summary>
        /// 컴포넌트가 켜질 때 필요한 참조를 미리 찾아둡니다.
        /// 하이라이트, Canvas, 기본 ItemGrid를 준비하는 초기화 함수입니다.
        /// </summary>
        private void Awake()
        {
            _inventoryHighlight = GetComponent<InventoryHighlight>();

            // 드래그 중인 아이템은 그리드 밖에서도 마우스를 따라와야 하므로 Canvas 바로 아래에 둡니다.
            if (canvasTransform == null)
                canvasTransform = GetComponentInParent<Canvas>()?.transform;

            if (_selectedGrid == null)
                SelectedGrid = ResolveItemGrid();

            ResolveUseControllers();
            ResolveGroundItemController();
        }

        /// <summary>
        /// 매 프레임 인벤토리 입력과 드래그 표시를 처리합니다.
        /// 디버그 아이템 추가, 좌클릭 집기/배치, 하이라이트 갱신이 여기서 시작됩니다.
        /// </summary>
        private void Update()
        {
            // 인벤토리 패널이 닫혀 있으면 입력을 전부 무시합니다.
            // 이 상태에서 클릭이 들어오면 닫힌 UI의 아이템을 집는 문제가 생길 수 있습니다.
            if (!_isInventoryOpen)
            {
                _inventoryHighlight.Show(false);
                return;
            }

            // 들고 있는 아이템 이미지는 입력 가능 여부와 상관없이 마우스를 따라오게 둡니다.
            DragSelectedItemIcon();
            RefreshSelectedGridUnderMouse();

            // 주문서 타겟팅 중이거나 방금 타겟팅 클릭을 처리한 프레임이면,
            // 같은 클릭으로 인벤 아이템을 집는 일반 입력이 이어지지 않게 막습니다.
            if (targetingController != null && targetingController.BlocksPointerInput)
            {
                _inventoryHighlight.Show(false);
                return;
            }

            if (_isPointerInputBlocked)
            {
                _inventoryHighlight.Show(false);
                return;
            }

            if (Input.GetMouseButtonDown(0))
                HandleLeftClick();

            if (_selectedGrid == null)
            {
                _inventoryHighlight.Show(false);
                return;
            }

            UpdateHighlight();
        }

        public bool TryAddItem(ItemInstance itemInstance, int amount = 1)
        {
            if (_selectedGrid == null || _selectedGrid.AcceptOnlyOneByOneItems)
                SelectedGrid = ResolveItemGrid();

            if (_selectedGrid == null || itemPrefab == null || canvasTransform == null || itemInstance == null || itemInstance.data == null)
            {
                Debug.LogError("InventoryController: 인벤토리 그리드, 아이템 프리팹, Canvas, ItemData 연결을 확인해야 합니다.");
                return false;
            }

            // YJW 쪽은 "어떤 아이템인가"를 ItemInstance로 넘기고, NYH 인벤토리가 "몇 개로 쌓을 것인가"를 결정합니다.
            // 그래서 스택 수량은 ItemInstance가 아니라 InventoryItem.Count에 저장합니다.
            // amount를 열어둔 이유는 나중에 보상/상자처럼 한 번에 여러 개를 넣는 흐름도 같은 API로 받기 위해서입니다.
            amount = Mathf.Max(1, amount);

            // 포션/스크롤은 새 UI를 만들기 전에 기존 스택에 먼저 합칩니다.
            // 합쳐졌다면 새 InventoryItem GameObject를 만들 필요가 없으므로 여기서 끝냅니다.
            if (TryMergeStack(itemInstance, amount))
                return true;

            InventoryItem item = CreateInventoryItem(itemInstance);

            // 새 슬롯을 만들 때도 수량은 ItemInstance에 쓰지 않고 InventoryItem에만 기록합니다.
            // 이렇게 해야 같은 ItemInstance 구조를 장비/무기/스크롤/포션이 공유해도 수량 책임이 섞이지 않습니다.
            item.SetCount(amount);

            // 자동 획득 시에는 좌상단부터 훑어서 들어갈 수 있는 첫 빈 공간에 넣습니다.
            Vector2Int? pos = _selectedGrid.FindSpaceForItem(item);
            if (pos == null)
            {
                Destroy(item.gameObject);
                return false;
            }

            _selectedGrid.PlaceItemInternal(item, pos.Value.x, pos.Value.y);
            _items.Add(item);
            OnItemAdded?.Invoke(item);
            RaiseInventoryChanged();
            return true;
        }

        public bool TryRemoveItem(InventoryItem item)
        {
            if (item == null) return false;

            ItemGrid targetGrid = _selectedGrid != null ? _selectedGrid : ResolveItemGrid();
            if (targetGrid == null)
            {
                Debug.LogError("InventoryController: 아이템을 제거할 인벤토리 그리드를 찾을 수 없습니다.");
                return false;
            }

            // 지금 손에 들고 있던 아이템을 외부 시스템이 제거한 경우, 드래그 상태도 같이 정리합니다.
            if (_selectedItem == item)
            {
                _selectedItem = null;
                _dragRect = null;
                ClearDragStartPosition();
            }

            _items.Remove(item);
            targetGrid.RemoveItem(item);
            RaiseInventoryChanged();
            return true;
        }

        public void ForgetItem(InventoryItem item)
        {
            if (item == null) return;

            if (_items.Remove(item))
                RaiseInventoryChanged();
        }

        public bool TryConsumeSelectedItem(InventoryItem expected)
        {
            return TryConsumeHeldItem(expected, false);
        }

        public bool TryConsumeOneItem(InventoryItem item)
        {
            if (item == null || item.ItemInstance == null)
                return false;

            // 스택이 2개 이상이면 아이템 UI는 그대로 두고 수량만 1 감소시킵니다.
            // 마지막 1개를 쓸 때만 기존 삭제 흐름으로 넘겨 GameObject와 그리드 점유를 정리합니다.
            if (item.Count > 1)
            {
                item.TryDecreaseCount(1);

                // 더블클릭 사용 직후에는 아이템이 손에 들려 있을 수 있으므로,
                // 수량만 줄인 뒤에는 원래 칸으로 되돌려 드래그 상태를 끝냅니다.
                if (_selectedItem == item)
                    CancelDragAndReturnItem();

                RaiseInventoryChanged();
                return true;
            }

            if (_selectedItem == item)
                return TryConsumeHeldItem(item, true);

            return TryRemoveItem(item);
        }

        /// <summary>
        /// 현재 마우스에 들고 있는 아이템을 인벤토리 소유 목록과 드래그 상태에서 제거합니다.
        /// 장비 장착처럼 GameObject를 보관해야 하는 경우 destroyObject=false, 포션/스크롤 소비처럼 사라져야 하는 경우 true를 사용합니다.
        /// </summary>
        public bool TryConsumeHeldItem(InventoryItem expected, bool destroyObject)
        {
            // 손에 든 아이템과 요청 아이템이 다르면 다른 시스템이 잘못된 아이템을 소비하려는 것이므로 실패합니다.
            if (expected == null || _selectedItem != expected)
                return false;

            _items.Remove(expected);
            expected.SetRaycastTarget(false);
            _selectedItem = null;
            _overlapItem = null;
            _dragRect = null;
            _lastPickedItem = null;
            ClearDragStartPosition();
            _inventoryHighlight.Show(false);
            if (destroyObject)
                Destroy(expected.gameObject);

            RaiseInventoryChanged();
            return true;
        }

        public void HoldItem(InventoryItem item, ItemGrid returnGrid, int returnX, int returnY)
        {
            if (item == null) return;

            if (_selectedItem != null)
            {
                Debug.LogWarning("InventoryController: 이미 손에 든 아이템이 있어 새 아이템을 들 수 없습니다.");
                return;
            }

            if (!_items.Contains(item))
                _items.Add(item);

            item.gameObject.SetActive(true);
            item.SetRaycastTarget(false);
            _selectedItem = item;
            _overlapItem = null;
            _dragRect = item.GetComponent<RectTransform>();

            if (_dragRect != null)
            {
                _dragRect.SetParent(canvasTransform, true);
                _dragRect.position = Input.mousePosition;
                BringDraggedItemToFront();
            }

            _dragStartGrid = returnGrid;
            _dragStartX = returnX;
            _dragStartY = returnY;
            RaiseInventoryChanged();
        }

        public bool CanPlaceInDefaultGrid(InventoryItem item)
        {
            if (item == null) return false;

            // 자동 장착 교체처럼 "실제로 넣기 전"에 공간만 확인해야 하는 흐름에서 사용합니다.
            ItemGrid targetGrid = ResolveItemGrid();
            return targetGrid != null && targetGrid.FindSpaceForItem(item).HasValue;
        }

        /// <summary>
        /// 아이템을 기본 인벤토리 그리드의 첫 빈 공간에 자동 배치합니다.
        /// 장비 교체 후 기존 장비를 가방으로 돌려보낼 때 사용합니다.
        /// </summary>
        public bool TryPlaceInDefaultGrid(InventoryItem item)
        {
            if (item == null) return false;

            ItemGrid targetGrid = ResolveItemGrid();
            if (targetGrid == null) return false;

            // 실제 배치 전에 FindSpaceForItem으로 좌상단 빈칸을 찾습니다.
            Vector2Int? pos = targetGrid.FindSpaceForItem(item);
            if (pos == null) return false;

            // 장착 중 숨겨뒀던 InventoryItem도 다시 보이게 켠 뒤 그리드 아래로 배치합니다.
            item.gameObject.SetActive(true);
            targetGrid.PlaceItemInternal(item, pos.Value.x, pos.Value.y);
            item.SetRaycastTarget(true);
            if (!_items.Contains(item))
                _items.Add(item);

            RaiseInventoryChanged();
            return true;
        }

        public InventoryItem CreateItemView(ItemInstance itemInstance, Transform parent, bool playerOwned)
        {
            if (itemPrefab == null || itemInstance == null || itemInstance.data == null || parent == null)
            {
                Debug.LogError("InventoryController: 아이템 UI 뷰를 만들기 위한 Prefab, ItemData, Parent 연결을 확인해야 합니다.");
                return null;
            }   

            InventoryItem item = Instantiate(itemPrefab, parent);

            // playerOwned가 false인 뷰는 상점/보상 미리보기처럼 인벤토리 소유물이 아닌 표시용으로 쓸 수 있습니다.
            item.Set(itemInstance, playerOwned ? this : null);
            return item;
        }

        /// <summary>
        /// 인벤토리 패널의 열림 상태를 외부 토글 컴포넌트가 알려줄 때 사용합니다.
        /// 닫히는 순간에는 들고 있던 아이템을 원래 칸으로 되돌리고, 이후 클릭 입력을 차단합니다.
        /// </summary>
        public void SetInventoryOpen(bool isOpen)
        {
            _isInventoryOpen = isOpen;
            SetPointerInputBlocked(!isOpen);
        }

        /// <summary>
        /// 인벤토리 클릭 입력을 막거나 다시 허용합니다.
        /// 입력을 막을 때는 현재 드래그 중인 아이템이 사라지지 않도록 먼저 원위치로 되돌립니다.
        /// </summary>
        public void SetPointerInputBlocked(bool value)
        {
            _isPointerInputBlocked = value;
            if (!value) return;

            // 단순히 _selectedItem = null만 하면 그리드에서 빠져 있던 아이템이 돌아갈 곳을 잃습니다.
            // 그래서 입력을 막기 전에 반드시 드래그 취소 처리를 먼저 실행합니다.
            CancelDragAndReturnItem();

            _overlapItem = null;
            _inventoryHighlight.Show(false);
        }

        /// <summary>
        /// 플레이어 소유 인벤토리에 넣을 아이템 UI를 Canvas 아래에 생성합니다.
        /// </summary>
        private InventoryItem CreateInventoryItem(ItemInstance itemInstance)
        {
            return CreateItemView(itemInstance, canvasTransform, true);
        }

        private bool TryMergeStack(ItemInstance itemInstance, int amount)
        {
            // 이번 스택 시스템은 YJW ItemBase를 건드리지 않기 위해,
            // "스택 가능 여부"를 인벤토리 쪽 타입 검사로만 결정합니다.
            if (!IsStackableItem(itemInstance.data))
                return false;

            foreach (InventoryItem item in _items)
            {
                if (item == null || item.ItemInstance == null)
                    continue;

                // 같은 ScriptableObject 참조면 같은 포션/스크롤로 보고 한 칸에 합칩니다.
                if (item.ItemData != itemInstance.data)
                    continue;

                // 기존 슬롯에 합쳐질 때도 ItemInstance는 건드리지 않습니다.
                // 스택은 보관 방식의 문제라서 InventoryItem.Count만 증가시키는 것이 책임 분리에 맞습니다.
                item.AddCount(amount);
                RaiseInventoryChanged();
                return true;
            }

            return false;
        }

        private static bool IsStackableItem(ItemBase itemData)
        {
            if (itemData == null)
                return false;

            // 장비는 강화/저주/감정 같은 개별 상태가 있으므로 합치지 않습니다.
            // 소비형 기타 아이템은 개별 강화/저주 상태가 없으므로 포션처럼 같은 SO끼리 합칩니다.
            return itemData is PotionData
                || itemData.ItemType == ItemType.Scroll
                || itemData is TorchData
                || itemData is FragmentsOfLife;
        }

        private void ResolveUseControllers()
        {
            // 씬에 직접 붙여두지 않아도 더블클릭 사용 흐름이 동작하도록 같은 오브젝트에 자동으로 붙입니다.
            if (targetingController == null)
                targetingController = GetComponent<InventoryTargetingController>();

            if (targetingController == null)
                targetingController = gameObject.AddComponent<InventoryTargetingController>();

            targetingController.Configure(this);

            // 사용 컨트롤러는 대상 지정 컨트롤러를 필요로 하므로 targetingController 준비 뒤 초기화합니다.
            if (itemUseController == null)
                itemUseController = GetComponent<InventoryItemUseController>();

            if (itemUseController == null)
                itemUseController = gameObject.AddComponent<InventoryItemUseController>();

            itemUseController.Configure(this, targetingController);
        }

        /// <summary>
        /// 현재 씬에서 사용할 ItemGrid를 찾습니다.
        /// 활성 오브젝트에서 먼저 찾고, 없으면 비활성 UI 오브젝트까지 포함해서 다시 탐색합니다.
        /// </summary>
        private ItemGrid ResolveItemGrid()
        {
            if (defaultInventoryGrid != null)
                return defaultInventoryGrid;

            ItemGrid activeGrid = FindFirstObjectByType<ItemGrid>();
            if (activeGrid != null && !activeGrid.AcceptOnlyOneByOneItems) return activeGrid;

            // 인벤토리 패널이 비활성화된 상태에서도 드랍 아이템 획득은 가능해야 하므로,
            // 씬에 존재하지만 비활성화된 UI까지 포함해서 한 번 더 찾습니다.
            ItemGrid[] allGrids = Resources.FindObjectsOfTypeAll<ItemGrid>();
            foreach (ItemGrid grid in allGrids)
            {
                if (grid == null || !grid.gameObject.scene.IsValid() || grid.AcceptOnlyOneByOneItems) continue;
                return grid;
            }

            return null;
        }

        public void RefreshSelectedGridUnderMouse()
        {
            ItemGrid hoveredGrid = FindGridUnderMouse();
            if (hoveredGrid != null)
                SelectedGrid = hoveredGrid;
        }

        private ItemGrid FindGridUnderMouse()
        {
            ItemGrid[] grids = Resources.FindObjectsOfTypeAll<ItemGrid>();
            ItemGrid fallback = null;

            foreach (ItemGrid grid in grids)
            {
                if (grid == null || !grid.gameObject.scene.IsValid() || !grid.gameObject.activeInHierarchy) continue;
                if (!grid.ContainsScreenPoint(Input.mousePosition)) continue;

                if (grid.AcceptOnlyOneByOneItems)
                    return grid;

                fallback = grid;
            }

            return fallback;
        }

        /// <summary>
        /// 마우스 왼쪽 클릭 입력을 처리합니다.
        /// 손에 든 아이템이 없으면 집고, 이미 들고 있으면 현재 칸에 배치를 시도합니다.
        /// 실질적인 게임에서 더블클릭시 사용을 호출하는 시작점
        /// </summary>
        private void HandleLeftClick()
        {
            // 들고 있는 아이템을 인벤토리 패널 밖에 놓으면, 그리드 배치가 아니라 플레이어 발밑 드랍으로 해석합니다.
            if (_selectedItem != null && IsPointerOutsideInventoryPanel())
            {
                TryDropSelectedItemToGround();
                _lastPickedItem = null;
                return;
            }

            // 드래그 우선 방식: 첫 클릭은 기존처럼 집고, 같은 아이템을 짧은 시간 안에 다시 클릭하면 사용으로 전환합니다.
            if (_selectedItem != null && IsDoubleClickOnSelectedItem())
            {
                itemUseController?.TryUseDoubleClickedItem(_selectedItem);
                _lastPickedItem = null;
                return;
            }

            if (_selectedGrid == null) return;

            Vector2Int gridPos = GetMouseGridPosition();

            // 손이 비어 있으면 클릭한 칸의 아이템을 집고, 들고 있으면 현재 위치에 내려놓습니다.
            if (_selectedItem == null) TryPickUpItem(gridPos);
            else TryPlaceSelectedItem(gridPos);
        }

        /// <summary>
        /// 지정한 그리드 좌표에 있는 아이템을 집어 마우스 드래그 상태로 전환합니다.
        /// 클릭한 칸이 비어 있으면 아무 일도 하지 않습니다.
        /// </summary>
        private void TryPickUpItem(Vector2Int gridPos)
        {
            // PickUpItem()을 호출하면 해당 아이템이 그리드 슬롯 배열에서 빠집니다.
            // 그래서 호출 전에 GetItemAt()으로 아이템을 먼저 찾고, 원래 칸 정보를 저장해야 합니다.
            InventoryItem item = _selectedGrid.GetItemAt(gridPos.x, gridPos.y);
            if (item == null) return;

            SaveDragStartPosition(item);

            // 실제로 아이템을 그리드에서 빼서 손에 듭니다.
            // 이 호출은 한 번만 해야 합니다. 두 번 호출하면 첫 호출에서 이미 빠져서 두 번째는 null이 될 수 있습니다.
            _selectedItem = _selectedGrid.PickUpItem(gridPos.x, gridPos.y);
            if (_selectedItem == null) return;

            _selectedItem.SetRaycastTarget(false);
            // 더블클릭 판정을 위해 "방금 집은 아이템"과 집은 시간을 기록합니다.
            _lastPickedItem = _selectedItem;
            _lastPickTime = Time.unscaledTime;
            _dragRect = _selectedItem.GetComponent<RectTransform>();

            // 드래그 중인 아이템은 Canvas 최상단으로 올려 다른 슬롯/아이템 뒤에 가려지지 않게 합니다.
            _dragRect.SetParent(canvasTransform, true);
            BringDraggedItemToFront();
            RequestMoveAudio(_selectedItem);
        }

        /// <summary>
        /// 드래그를 시작한 아이템의 원래 위치를 저장합니다.
        /// 나중에 인벤토리가 닫히거나 드래그가 취소되면 이 정보로 아이템을 제자리로 돌려보냅니다.
        /// </summary>
        private void SaveDragStartPosition(InventoryItem item)
        {
            _dragStartGrid = _selectedGrid;
            _dragStartX = item.OnGridPositionX;
            _dragStartY = item.OnGridPositionY;
        }

        /// <summary>
        /// 저장해둔 드래그 시작 위치를 비웁니다.
        /// 아이템 배치가 끝났거나 드래그 취소 처리가 끝났을 때 이전 좌표가 남지 않게 합니다.
        /// </summary>
        private void ClearDragStartPosition()
        {
            _dragStartGrid = null;
            _dragStartX = 0;
            _dragStartY = 0;
        }

        /// <summary>
        /// 현재 들고 있는 아이템을 지정한 그리드 좌표에 내려놓으려고 시도합니다.
        /// 배치 위치에 아이템 하나만 겹치면 서로 교체하는 방식으로 처리합니다.
        /// </summary>
        private void TryPlaceSelectedItem(Vector2Int gridPos)
        {
            InventoryItem placedItem = _selectedItem;
            bool placed = _selectedGrid.TryPlaceItem(_selectedItem, gridPos.x, gridPos.y, ref _overlapItem);
            if (!placed)
            {
                if (_selectedGrid.AcceptOnlyOneByOneItems)
                    CancelDragAndReturnItem();

                return;
            }

            placedItem?.SetRaycastTarget(true);
            _selectedItem = null;
            if (_overlapItem != null)
            {
                // 배치 위치에 단 하나의 아이템만 겹쳤다면 서로 교체합니다.
                // 새 아이템은 그리드에 내려놓고, 겹쳐 있던 아이템을 손에 든 상태로 전환합니다.
                // 이때 _dragStartX/Y는 그대로 둡니다. 그래야 인벤토리를 닫으면 겹쳐 있던 아이템이
                // 방금 비워진 "처음 들었던 아이템의 자리"로 돌아갈 수 있습니다.
                _selectedItem = _overlapItem;
                _overlapItem = null;
                _selectedItem.SetRaycastTarget(false);
                _dragRect = _selectedItem.GetComponent<RectTransform>();
                _dragRect.SetParent(canvasTransform, true);
                BringDraggedItemToFront();
                RequestMoveAudio(_selectedItem);
            }
            else
            {
                // 완전히 빈 칸에 내려놓았다면 드래그가 끝난 것이므로 원래 위치 백업은 더 이상 필요 없습니다.
                _dragRect = null;
                ClearDragStartPosition();
            }

            RaiseInventoryChanged();
        }

        private static void RequestMoveAudio(InventoryItem item)
        {
            InventoryMoveAudio.RequestMoveAudio(item);
        }

        private void TryDropSelectedItemToGround()
        {
            if (_selectedItem == null || _selectedItem.ItemInstance == null)
                return;

            // 바닥 드랍은 KDU/YJW ItemPickup 프리팹을 사용하지만,
            // "어느 칸에 몇 개가 쌓였는가"는 GroundItemController가 관리합니다.
            ResolveGroundItemController();
            if (groundItemController == null)
            {
                Debug.LogWarning("InventoryController: GroundItemController를 찾지 못해 아이템을 버릴 수 없습니다.");
                return;
            }

            InventoryItem itemToDrop = _selectedItem;
            if (!groundItemController.DropItem(itemToDrop.ItemInstance, itemToDrop.Count))
            {
                Debug.LogWarning("InventoryController: 바닥 아이템 프리팹, 플레이어, GridSystem 연결을 확인해야 합니다.");
                return;
            }

            // 드랍 성공 후에만 인벤토리 UI와 소유 목록에서 제거합니다. 실패하면 계속 손에 든 상태를 유지합니다.
            // 장비/강화/저주 상태는 ItemInstance를 이미 바닥 픽업에 넘겼으므로 여기서는 UI만 정리합니다.
            TryConsumeHeldItem(itemToDrop, true);
        }

        /// <summary>
        /// 현재 마우스 화면 좌표를 선택된 인벤토리 그리드의 칸 좌표로 변환합니다.
        /// 큰 아이템을 들고 있을 때는 아이템의 좌상단 칸이 기준이 되도록 좌표를 보정합니다.
        /// </summary>
        private Vector2Int GetMouseGridPosition()
        {
            Vector2 mousePos = Input.mousePosition;
            if (_selectedItem != null)
            {
                // 아이템 크기가 2칸 이상일 때도 마우스가 아이템의 좌상단 기준 칸을 가리키도록 보정합니다.
                mousePos.x -= (_selectedItem.ItemData.Width - 1) * ItemGrid.TileWidth / 2f;
                mousePos.y += (_selectedItem.ItemData.Height - 1) * ItemGrid.TileHeight / 2f;
            }

            return _selectedGrid.ScreenToGridPosition(mousePos);
        }

        private bool IsPointerOutsideInventoryPanel()
        {
            RectTransform panelRect = ResolveInventoryPanelRect();
            if (panelRect == null)
                return false;

            return !IsPointerInsideRect(panelRect);
        }

        /// <summary>
        /// 드랍 판정 기준이 되는 인벤토리 전체 패널을 찾습니다.
        /// 인스펙터에 연결되어 있으면 그 값을 쓰고, 없으면 기본 인벤토리 그리드의 부모 RectTransform을 임시 기준으로 씁니다.
        /// </summary>
        public RectTransform ResolveInventoryPanelRect()
        {
            if (inventoryPanelRect != null)
                return inventoryPanelRect;

            ItemGrid grid = defaultInventoryGrid != null ? defaultInventoryGrid : ResolveItemGrid();
            if (grid != null)
                inventoryPanelRect = grid.GetComponentInParent<RectTransform>();

            return inventoryPanelRect;
        }

        /// <summary>
        /// Canvas 모드가 Overlay가 아니어도 같은 방식으로 마우스가 Rect 안에 있는지 검사합니다.
        /// 인벤토리 패널 밖 드랍 판정이 UI 카메라 설정에 따라 흔들리지 않게 하기 위한 보정입니다.
        /// </summary>
        private static bool IsPointerInsideRect(RectTransform rect)
        {
            if (rect == null)
                return false;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

            return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, eventCamera);
        }

        /// <summary>
        /// 드래그 중인 아이템 UI가 매 프레임 마우스 위치를 따라오도록 갱신합니다.
        /// </summary>
        private void DragSelectedItemIcon()
        {
            if (_selectedItem == null || _dragRect == null) return;

            _dragRect.position = Input.mousePosition;
            BringDraggedItemToFront();
        }

        private void BringDraggedItemToFront()
        {
            if (_dragRect == null) return;

            // 하이라이트/다른 UI가 sibling 순서를 바꿔도 손에 든 아이템은 항상 가장 위에 보이게 합니다.
            _dragRect.SetAsLastSibling();
        }

        /// <summary>
        /// 현재 손에 들고 있는 아이템을 드래그 시작 위치로 되돌리고, 드래그 상태를 정리합니다.
        /// 인벤토리 패널을 닫을 때 이 처리를 하지 않으면 아이템 UI가 Canvas 아래에 떠 있거나 슬롯에서 빠질 수 있습니다.
        /// </summary>
        public void CancelDragAndReturnItem()
        {
            if (_selectedItem != null)
            {
                bool returned = false;

                if (_dragStartGrid != null)
                {
                    _dragStartGrid.PlaceItemInternal(_selectedItem, _dragStartX, _dragStartY);
                    _selectedItem.SetRaycastTarget(true);
                    returned = true;
                }
                else
                {
                    returned = TryReturnSelectedItemToDefaultGrid();
                }

                if (!returned)
                {
                    Debug.LogWarning("InventoryController: 들고 있는 아이템을 되돌릴 빈 인벤토리 칸을 찾지 못했습니다.");
                    return;
                }

                RaiseInventoryChanged();
            }

            _selectedItem = null;
            _overlapItem = null;
            _dragRect = null;
            ClearDragStartPosition();
            _inventoryHighlight.Show(false);
        }

        private bool IsDoubleClickOnSelectedItem()
        {
            if (_selectedItem == null || _lastPickedItem != _selectedItem)
                return false;

            // Time.timeScale 영향을 받지 않도록 unscaledTime 기준으로 판정합니다.
            return Time.unscaledTime - _lastPickTime <= doubleClickMaxDelay;
        }

        private bool TryReturnSelectedItemToDefaultGrid()
        {
            if (_selectedItem == null) return true;

            return TryReturnSelectedItemToDefaultGrid(_selectedItem);
        }

        public bool TryReturnSelectedItemToDefaultGrid(InventoryItem item)
        {
            if (item == null) return true;

            ItemGrid targetGrid = ResolveItemGrid();
            if (targetGrid == null) return false;

            Vector2Int? pos = targetGrid.FindSpaceForItem(item);
            if (pos == null) return false;

            targetGrid.PlaceItemInternal(item, pos.Value.x, pos.Value.y);
            item.SetRaycastTarget(true);
            if (!_items.Contains(item))
                _items.Add(item);

            return true;
        }

        /// <summary>
        /// 이 컴포넌트가 꺼질 때 드래그 상태가 남아 있으면 안전하게 원위치로 되돌립니다.
        /// 패널 토글에서 SetInventoryOpen(false)를 호출하는 것이 주 경로이고, 이 함수는 예외 상황 대비용입니다.
        /// </summary>
        private void OnDisable()
        {
            CancelDragAndReturnItem();
        }

        /// <summary>
        /// 마우스 위치와 현재 드래그 상태에 맞춰 인벤토리 하이라이트의 표시 여부, 크기, 위치를 갱신합니다.
        /// 빈손일 때는 마우스 아래 아이템을, 아이템을 들고 있을 때는 배치될 영역을 보여줍니다.
        /// </summary>
        private void UpdateHighlight()
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (_selectedItem == null)
            {
                // 빈손일 때는 마우스 아래에 있는 아이템 전체 크기를 하이라이트합니다.
                InventoryItem target = _selectedGrid.GetItemAt(gridPos.x, gridPos.y);
                if (target == null)
                {
                    _inventoryHighlight.Show(false);
                    return;
                }

                _inventoryHighlight.Show(true);
                _inventoryHighlight.UpdateSize(target);
                _inventoryHighlight.UpdatePosition(_selectedGrid.GetLocalPositionForItem(target, target.OnGridPositionX, target.OnGridPositionY));
            }
            else
            {
                // 아이템을 들고 있을 때는 현재 위치에 배치 가능한 경우에만 하이라이트를 보여줍니다.
                bool valid = _selectedGrid.CanAcceptItem(_selectedItem)
                    && _selectedGrid.IsWithinBoundary(gridPos.x, gridPos.y, _selectedItem.ItemData.Width, _selectedItem.ItemData.Height);
                _inventoryHighlight.Show(valid);
                _inventoryHighlight.UpdateSize(_selectedItem);
                _inventoryHighlight.UpdatePosition(_selectedGrid.GetLocalPositionForItem(_selectedItem, gridPos.x, gridPos.y));
            }
        }

        /// <summary>
        /// 인벤토리 내용이 바뀌었음을 외부 구독자에게 알립니다.
        /// UI 갱신, 판매 목록 갱신, 장착 상태 갱신 같은 후처리가 이 이벤트를 구독할 수 있습니다.
        /// </summary>
        public void RaiseInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        private void ResolveGroundItemController()
        {
            if (groundItemController == null)
                groundItemController = FindFirstObjectByType<GroundItemController>();

            if (groundItemController == null)
                groundItemController = gameObject.AddComponent<GroundItemController>();

            // 자동 생성된 경우에도 인벤토리 참조가 반드시 들어가야,
            // 바닥 아이템을 주울 때 같은 TryAddItem 규칙과 스택 규칙을 사용할 수 있습니다.
            groundItemController.Configure(this);
        }
    }
}
