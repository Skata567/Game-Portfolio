using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리의 논리 칸 배열과 화면 좌표 변환을 전담합니다.
    /// InventoryController가 입력을 해석하면, 실제 배치 가능 여부와 슬롯 점유 처리를 이 클래스가 맡습니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ItemGrid : MonoBehaviour
    {
        // 인벤토리 한 칸의 UI 크기입니다. 아이템 크기나 그리드 전체 크기 계산에 공통으로 사용합니다.
        public const float TileWidth = 64f;
        public const float TileHeight = 64f;

        [Header("그리드 크기")]
        [Tooltip("인벤토리의 가로 칸 수입니다. 실제 UI 너비는 이 값에 칸 너비 64픽셀을 곱해 자동으로 정해집니다.")]
        [SerializeField, Min(1)] private int gridWidth = 20;

        [Tooltip("인벤토리의 세로 칸 수입니다. 실제 UI 높이는 이 값에 칸 높이 64픽셀을 곱해 자동으로 정해집니다.")]
        [SerializeField, Min(1)] private int gridHeight = 10;

        [Header("Placement Rules")]
        [Tooltip("켜면 Width 1, Height 1 아이템만 이 그리드에 배치할 수 있습니다. 퀵슬롯 10칸처럼 작은 아이템 전용 그리드에 사용합니다.")]
        [SerializeField] private bool acceptOnlyOneByOneItems;

        // 각 칸이 어떤 아이템에게 점유되어 있는지 저장합니다.
        // 2x2 아이템이면 4칸 모두 같은 InventoryItem 참조를 가집니다.
        private InventoryItem[,] _slots;

        private RectTransform _rectTransform;

        public event Action OnGridChanged;
        public event Action OnGridResized;

        public bool AcceptOnlyOneByOneItems => acceptOnlyOneByOneItems;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void InitGrid(int width, int height)
        {
            gridWidth = Mathf.Max(1, width);
            gridHeight = Mathf.Max(1, height);
            _slots = new InventoryItem[gridWidth, gridHeight];

            // 논리 그리드 크기와 실제 UI 판 크기를 맞춰 좌표 변환 오차를 막습니다.
            _rectTransform.sizeDelta = new Vector2(gridWidth * TileWidth, gridHeight * TileHeight);
            OnGridResized?.Invoke();
            RestoreExistingItemReferences();
        }

        /// <summary>
        /// 런타임에 인벤토리 크기를 바꿉니다.
        /// 기존 슬롯 배열을 새로 만들므로, 사용 중인 아이템이 있다면 호출 순서에 주의해야 합니다.
        /// </summary>
        public void ConfigureSize(int width, int height)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            InitGrid(width, height);
        }

        public InventoryItem GetItemAt(int x, int y)
        {
            EnsureInitialized();

            if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight) return null;
            return _slots[x, y];
        }

        public InventoryItem PickUpItem(int x, int y)
        {
            EnsureInitialized();

            InventoryItem item = GetItemAt(x, y);
            if (item == null) return null;

            // 아이템이 여러 칸을 차지하므로, 집을 때는 해당 아이템이 점유하던 모든 칸을 비웁니다.
            ClearItemReferences(item);
            RaiseGridChanged();
            return item;
        }

        /// <summary>
        /// 지정한 좌표에 아이템을 내려놓습니다.
        /// 빈 공간이면 바로 배치하고, 하나의 아이템하고만 겹치면 overlapItem으로 넘겨 교체할 수 있게 합니다.
        /// 서로 다른 아이템이 두 개 이상 겹치면 배치 실패합니다.
        /// </summary>
        public bool TryPlaceItem(InventoryItem item, int x, int y, ref InventoryItem overlapItem)
        {
            EnsureInitialized();

            if (item == null || item.ItemData == null) return false;
            if (!CanAcceptItem(item)) return false;
            if (!IsWithinBoundary(x, y, item.ItemData.Width, item.ItemData.Height)) return false;
            if (!CheckOverlap(x, y, item.ItemData.Width, item.ItemData.Height, ref overlapItem))
            {
                overlapItem = null;
                return false;
            }

            if (overlapItem != null) ClearItemReferences(overlapItem);
            PlaceItemInternal(item, x, y);
            return true;
        }

        public void PlaceItemInternal(InventoryItem item, int x, int y)
        {
            EnsureInitialized();

            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.SetParent(_rectTransform, false);

            // 아이템이 차지하는 모든 칸에 같은 참조를 넣어, 어느 칸을 눌러도 같은 아이템을 찾게 합니다.
            for (int ix = 0; ix < item.ItemData.Width; ix++)
            {
                for (int iy = 0; iy < item.ItemData.Height; iy++)
                    _slots[x + ix, y + iy] = item;
            }

            item.OnGridPositionX = x;
            item.OnGridPositionY = y;
            itemRect.anchoredPosition = GetLocalPositionForItem(item, x, y);
            RaiseGridChanged();
        }

        /// <summary>
        /// 화면 좌표를 인벤토리 칸 좌표로 변환합니다.
        /// 반환값은 좌상단이 (0, 0)이고, 오른쪽 아래쪽으로 증가합니다.
        /// </summary>
        public Vector2Int ScreenToGridPosition(Vector2 screenPos)
        {
            EnsureInitialized();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPos, null, out Vector2 local))
                return new Vector2Int(-1, -1);

            Vector2 topLeft = GetTopLeftLocalPosition();
            int x = Mathf.FloorToInt((local.x - topLeft.x) / TileWidth);
            int y = Mathf.FloorToInt((topLeft.y - local.y) / TileHeight);
            return new Vector2Int(x, y);
        }

        public bool ContainsScreenPoint(Vector2 screenPos)
        {
            EnsureInitialized();
            return RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, screenPos, null);
        }

        /// <summary>
        /// 아이템의 좌상단 그리드 좌표를 기준으로, RectTransform.anchoredPosition에 넣을 중심 좌표를 계산합니다.
        /// </summary>
        public Vector2 GetLocalPositionForItem(InventoryItem item, int x, int y)
        {
            EnsureInitialized();

            Vector2 topLeft = GetTopLeftLocalPosition();
            return new Vector2(
                topLeft.x + x * TileWidth + TileWidth * item.ItemData.Width / 2f,
                topLeft.y - (y * TileHeight + TileHeight * item.ItemData.Height / 2f));
        }

        public Vector2 GetTopLeftLocalPosition()
        {
            Rect rect = _rectTransform.rect;

            // RectTransform의 pivot이 어디에 있든 좌상단 로컬 좌표를 안정적으로 계산합니다.
            return new Vector2(
                -rect.width * _rectTransform.pivot.x,
                rect.height * (1f - _rectTransform.pivot.y));
        }

        public bool IsWithinBoundary(int x, int y, int width, int height)
        {
            EnsureInitialized();

            return x >= 0 && y >= 0 && x + width <= gridWidth && y + height <= gridHeight;
        }

        public bool CanAcceptItem(InventoryItem item)
        {
            if (item == null || item.ItemData == null) return false;
            if (!acceptOnlyOneByOneItems)
                return true;

            if (item.ItemData.Width != 1 || item.ItemData.Height != 1)
                return false;

            return CanAcceptQuickSlotItem(item);
        }

        private static bool CanAcceptQuickSlotItem(InventoryItem item)
        {
            ItemBase itemData = item.ItemData;
            if (itemData == null)
                return false;

            // 퀵슬롯은 소비/투척용 슬롯입니다. 1x1이어도 착용 장비가 들어가면 사용 키와 장착 흐름이 섞입니다.
            if (itemData is ThrowableWeapon && item.ItemInstance is ThrowableWeaponInstance)
                return true;

            return itemData.ItemType != ItemType.Equipment && itemData.Slot == EquipmentSlot.None;
        }

        public Vector2Int? FindSpaceForItem(InventoryItem item)
        {
            EnsureInitialized();

            if (!CanAcceptItem(item)) return null;

            // 자동 획득시 탐색합니다. 위쪽 줄부터 왼쪽에서 오른쪽으로 훑어 첫 빈 공간을 반환합니다.
            for (int y = 0; y <= gridHeight - item.ItemData.Height; y++)
            {
                for (int x = 0; x <= gridWidth - item.ItemData.Width; x++)
                {
                    if (IsAreaEmpty(x, y, item.ItemData.Width, item.ItemData.Height))
                        return new Vector2Int(x, y);
                }
            }

            return null;
        }

        public void RemoveItem(InventoryItem item)
        {
            EnsureInitialized();

            if (item == null) return;
            ClearItemReferences(item);
            Destroy(item.gameObject);
            RaiseGridChanged();
        }

        public void ClearAllItems(bool destroyItems)
        {
            EnsureInitialized();

            if (_slots == null) return;

            if (destroyItems)
            {
                // 큰 아이템이 여러 칸에 중복 저장되어 있으므로 HashSet으로 한 번만 Destroy합니다.
                HashSet<InventoryItem> uniqueItems = new();
                foreach (InventoryItem item in _slots)
                {
                    if (item != null)
                        uniqueItems.Add(item);
                }

                foreach (InventoryItem item in uniqueItems)
                {
                    if (item != null)
                        Destroy(item.gameObject);
                }
            }

            _slots = new InventoryItem[gridWidth, gridHeight];
            RaiseGridChanged();
        }

        private void RaiseGridChanged()
        {
            OnGridChanged?.Invoke();
        }

        private void EnsureInitialized()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_slots == null)
                InitGrid(gridWidth, gridHeight);
        }


        private bool CheckOverlap(int x, int y, int width, int height, ref InventoryItem overlap)
        {
            // 배치 영역 안에 이미 있는 아이템이 하나뿐이면 교체 가능, 둘 이상이면 실패 처리합니다.
            for (int ix = 0; ix < width; ix++)
            {
                for (int iy = 0; iy < height; iy++)
                {
                    InventoryItem current = _slots[x + ix, y + iy];
                    if (current == null) continue;
                    if (overlap == null) overlap = current;
                    else if (overlap != current) return false;
                }
            }

            return true;
        }

        private bool IsAreaEmpty(int x, int y, int width, int height)
        {
            for (int ix = 0; ix < width; ix++)
            {
                for (int iy = 0; iy < height; iy++)
                {
                    if (_slots[x + ix, y + iy] != null) return false;
                }
            }

            return true;
        }

        private void ClearItemReferences(InventoryItem item)
        {
            if (item.ItemData == null) return;

            // item에 기록된 좌상단 위치와 크기를 기준으로 슬롯 참조를 제거합니다.
            // 혹시 다른 아이템 참조가 들어간 칸은 건드리지 않아 방어적으로 동작합니다.
            for (int ix = 0; ix < item.ItemData.Width; ix++)
            {
                for (int iy = 0; iy < item.ItemData.Height; iy++)
                {
                    int x = item.OnGridPositionX + ix;
                    int y = item.OnGridPositionY + iy;
                    if (x >= 0 && y >= 0 && x < gridWidth && y < gridHeight && _slots[x, y] == item)
                        _slots[x, y] = null;
                }
            }
        }

        private void RestoreExistingItemReferences()
        {
            InventoryItem[] existingItems = GetComponentsInChildren<InventoryItem>(true);
            foreach (InventoryItem item in existingItems)
            {
                if (item == null || item.ItemData == null) continue;
                if (!IsWithinBoundary(item.OnGridPositionX, item.OnGridPositionY, item.ItemData.Width, item.ItemData.Height)) continue;

                for (int ix = 0; ix < item.ItemData.Width; ix++)
                {
                    for (int iy = 0; iy < item.ItemData.Height; iy++)
                        _slots[item.OnGridPositionX + ix, item.OnGridPositionY + iy] = item;
                }
            }
        }

        // (UI 시각 처리 로직은 모두 ItemGridView.cs로 이동되었습니다)
    }
}
