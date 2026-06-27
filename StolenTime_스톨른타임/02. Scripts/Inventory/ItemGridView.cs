using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PrototypeRT
{
    /// <summary>
    /// ItemGrid의 논리적 데이터 변화를 감지하여 실제 인벤토리 화면에 슬롯 이미지를 그려주는 전담 View 클래스입니다.
    /// 단일 책임 원칙(SRP)을 준수하기 위해 로직과 시각 요소를 분리했습니다.
    /// </summary>
    [RequireComponent(typeof(ItemGrid))]
    public class ItemGridView : MonoBehaviour
    {
        [Header("Slot Visuals")]
        [Tooltip("그리드 한 칸을 표시할 UI 프리팹입니다. 배경 장식용입니다.")]
        [SerializeField] private RectTransform slotPrefab;

        [Tooltip("생성된 슬롯을 담을 부모입니다. 비워두면 이 오브젝트 아래에 바로 생성합니다.")]
        [SerializeField] private RectTransform slotContainer;

        [Tooltip("시작 시 슬롯 프리팹을 자동으로 생성할지 결정합니다.")]
        [SerializeField] private bool generateVisualSlots = true;

        private ItemGrid _itemGrid;
        private RectTransform _rectTransform;

        // 생성된 시각 슬롯들을 관리하기 위한 캐시입니다.
        private readonly List<RectTransform> _visualSlots = new();
        private RectTransform[,] _visualSlotGrid;

        private void Awake()
        {
            _itemGrid = GetComponent<ItemGrid>();
            _rectTransform = GetComponent<RectTransform>();
            
            // 데이터가 변경될 때마다 화면도 다시 그리도록 이벤트를 구독합니다.
            _itemGrid.OnGridChanged += RefreshVisualSlots;
            _itemGrid.OnGridResized += RebuildVisualSlots;
        }

        private void OnDestroy()
        {
            if (_itemGrid != null)
            {
                _itemGrid.OnGridChanged -= RefreshVisualSlots;
                _itemGrid.OnGridResized -= RebuildVisualSlots;
            }
        }

        private void Start()
        {
            // 초기화 시점에 슬롯을 구축합니다.
            RebuildVisualSlots();
        }

        /// <summary>
        /// 그리드 크기가 바뀔 때 완전히 새로운 슬롯들을 생성합니다.
        /// </summary>
        private void RebuildVisualSlots()
        {
            ClearVisualSlots();

            if (!generateVisualSlots || slotPrefab == null || _itemGrid == null) return;

            int width = _itemGrid.GridWidth;
            int height = _itemGrid.GridHeight;

            RectTransform parent = slotContainer != null ? slotContainer : _rectTransform;
            Vector2 topLeft = _itemGrid.GetTopLeftLocalPosition();
            _visualSlotGrid = new RectTransform[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    RectTransform slot = Instantiate(slotPrefab, parent);
                    slot.name = $"Slot_{x}_{y}";

                    // 슬롯은 배경 장식이므로 중앙 앵커/피벗 기준으로 직접 위치를 찍습니다.
                    slot.anchorMin = new Vector2(0.5f, 0.5f);
                    slot.anchorMax = new Vector2(0.5f, 0.5f);
                    slot.pivot = new Vector2(0.5f, 0.5f);
                    slot.sizeDelta = new Vector2(ItemGrid.TileWidth, ItemGrid.TileHeight);
                    slot.anchoredPosition = new Vector2(
                        topLeft.x + x * ItemGrid.TileWidth + ItemGrid.TileWidth / 2f,
                        topLeft.y - (y * ItemGrid.TileHeight + ItemGrid.TileHeight / 2f));

                    Image slotImage = slot.GetComponent<Image>();
                    if (slotImage != null)
                        slotImage.raycastTarget = false; // 입력 가로채기 방지

                    _visualSlots.Add(slot);
                    _visualSlotGrid[x, y] = slot;
                }
            }

            RefreshVisualSlots();
        }

        private void ClearVisualSlots()
        {
            for (int i = _visualSlots.Count - 1; i >= 0; i--)
            {
                RectTransform slot = _visualSlots[i];
                if (slot == null) continue;

                if (Application.isPlaying)
                    Destroy(slot.gameObject);
                else
                    DestroyImmediate(slot.gameObject);
            }

            _visualSlots.Clear();
            _visualSlotGrid = null;
        }

        /// <summary>
        /// 2x2 같은 큰 아이템이 들어왔을 때, 가려지는 슬롯들을 숨기고 하나를 병합된 것처럼 크게 만듭니다.
        /// </summary>
        private void RefreshVisualSlots()
        {
            if (_visualSlotGrid == null || _itemGrid == null)
                return;

            RestoreDefaultVisualSlots();

            int width = _itemGrid.GridWidth;
            int height = _itemGrid.GridHeight;
            HashSet<InventoryItem> mergedItems = new();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    InventoryItem item = _itemGrid.GetItemAt(x, y);
                    if (item == null || item.ItemData == null || mergedItems.Contains(item))
                        continue;

                    mergedItems.Add(item);
                    MergeVisualSlotsForItem(item);
                }
            }
        }

        private void RestoreDefaultVisualSlots()
        {
            int width = _itemGrid.GridWidth;
            int height = _itemGrid.GridHeight;
            Vector2 topLeft = _itemGrid.GetTopLeftLocalPosition();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    RectTransform slot = _visualSlotGrid[x, y];
                    if (slot == null) continue;

                    slot.gameObject.SetActive(true);
                    slot.name = $"Slot_{x}_{y}";
                    slot.sizeDelta = new Vector2(ItemGrid.TileWidth, ItemGrid.TileHeight);
                    slot.anchoredPosition = new Vector2(
                        topLeft.x + x * ItemGrid.TileWidth + ItemGrid.TileWidth / 2f,
                        topLeft.y - (y * ItemGrid.TileHeight + ItemGrid.TileHeight / 2f));
                }
            }
        }

        private void MergeVisualSlotsForItem(InventoryItem item)
        {
            int startX = item.OnGridPositionX;
            int startY = item.OnGridPositionY;
            int width = item.ItemData.Width;
            int height = item.ItemData.Height;

            if (!_itemGrid.IsWithinBoundary(startX, startY, width, height))
                return;

            RectTransform mergedSlot = _visualSlotGrid[startX, startY];
            if (mergedSlot == null)
                return;

            Vector2 topLeft = _itemGrid.GetTopLeftLocalPosition();
            mergedSlot.name = $"MergedSlot_{startX}_{startY}_{width}x{height}";
            mergedSlot.gameObject.SetActive(true);
            mergedSlot.sizeDelta = new Vector2(width * ItemGrid.TileWidth, height * ItemGrid.TileHeight);
            mergedSlot.anchoredPosition = new Vector2(
                topLeft.x + startX * ItemGrid.TileWidth + width * ItemGrid.TileWidth / 2f,
                topLeft.y - (startY * ItemGrid.TileHeight + height * ItemGrid.TileHeight / 2f));

            for (int iy = 0; iy < height; iy++)
            {
                for (int ix = 0; ix < width; ix++)
                {
                    if (ix == 0 && iy == 0) continue;

                    RectTransform coveredSlot = _visualSlotGrid[startX + ix, startY + iy];
                    if (coveredSlot != null)
                        coveredSlot.gameObject.SetActive(false);
                }
            }
        }
    }
}
