using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 안에서 보이는 아이템 UI 한 개를 표현합니다.
    /// 실제 아이템 수치/크기/아이콘은 ItemData가 가지고, 이 클래스는 화면 표시와 그리드 위치만 들고 있습니다.
    /// 
    /// 인벤토리 슬롯에 그려질 단일 아이템의 시각적 요소를 담당합니다.
    /// 공격력 등의 원본 데이터는 ItemData 인스턴스가 보관하며, 본 스크립트는 
    /// 해당 아이템이 인벤토리의 어느 좌표에 어떤 크기의 이미지로 출력될지만 집중적으로 관리합니다.
    /// </summary>
    [RequireComponent(typeof(Image))] // 이 스크립트가 제대로 작동하려면 화면에 그림을 그리기 위한 Image 컴포넌트가 반드시 필요하다는 뜻입니다.
    public class InventoryItem : MonoBehaviour
    {
        /// <summary>
        /// 이 UI가 표시하는 아이템의 개별 상태입니다. 강화/저주/감정 여부는 여기에 저장됩니다.
        /// 
        /// 동일한 아이템이라도 개별적으로 부여된 강화 수치나 저주 상태 등 
        /// 해당 객체만의 고유한 런타임 상태 데이터를 보관하는 속성입니다.
        /// </summary>
        public ItemInstance ItemInstance { get; private set; }

        /// <summary>
        /// 이 InventoryItem UI 슬롯에 겹쳐진 수량입니다.
        /// ItemInstance에 수량을 넣으면 장비/무기처럼 겹치지 않는 아이템까지 count를 갖게 되어 책임이 흐려지므로,
        /// "인벤토리 안에서 몇 개로 보이는가"는 인벤토리 표시 단위인 이 클래스가 관리합니다.
        /// </summary>
        public int Count { get; private set; } = 1;

        /// <summary>
        /// 이 UI가 표시하는 아이템 데이터입니다.
        /// 
        /// ItemInstance에 포함된 아이템의 원본(Base) 데이터를 즉시 반환하는 편의용 프로퍼티입니다.
        /// </summary>
        public ItemBase ItemData => ItemInstance != null ? ItemInstance.data : null;

        /// <summary>
        /// 아이템이 배치된 좌상단 칸의 X 좌표입니다.
        /// 
        /// 인벤토리 그리드 상에서 이 아이템이 차지하는 영역의 가로축 시작 인덱스(0부터 시작)를 나타냅니다.
        /// </summary>
        public int OnGridPositionX { get; set; }

        /// <summary>
        /// 아이템이 배치된 좌상단 칸의 Y 좌표입니다.
        /// 
        /// 인벤토리 그리드 상에서 이 아이템이 차지하는 영역의 세로축 시작 인덱스(0부터 시작)를 나타냅니다.
        /// </summary>
        public int OnGridPositionY { get; set; }

        // 유니티에서 이미지를 화면에 띄워주기 위해 사용하는 도구입니다.
        private Image _image;
        // 스택 수량 표시용 텍스트입니다. 프리팹에 없으면 런타임에 CountText를 자동 생성합니다.
        [SerializeField] private TMP_Text countText;

        // 주문서 타겟팅 중 반투명 표시를 했다가 원래 색으로 정확히 되돌리기 위해 최초 Image 색을 저장합니다.
        private Color _defaultImageColor = Color.white;
        private bool _hasDefaultImageColor;

        // 게임이 실행되고 이 아이템이 처음 만들어질 때 딱 한 번 자동으로 실행되는 함수입니다.
        private void Awake()
        {
            // 자신에게 붙어있는 Image 컴포넌트를 찾아서 _image 변수에 저장해둡니다. 나중에 써먹기 위해서입니다.
            _image = GetComponent<Image>();
            CacheDefaultImageColor();
        }

        /// <summary>
        /// 이미 생성된 ItemInstance를 UI에 연결합니다.
        /// 드랍 아이템처럼 강화/저주/감정 상태가 있는 아이템은 이 경로로 들어와야 상태가 보존됩니다.
        /// 
        /// 획득한 아이템의 데이터를 기반으로 UI 아이콘과 크기를 초기화하는 역할을 수행합니다.
        /// </summary>
        public void Set(ItemInstance itemInstance, InventoryController inventoryController)
        {
            // 받아온 아이템 상태 정보를 내 것으로 저장합니다.
            ItemInstance = itemInstance;

            // 같은 UI 오브젝트가 재사용되더라도 이전 스택 수량이 새 아이템에 섞이지 않도록 초기화합니다.
            // 실제 추가 수량은 InventoryController.TryAddItem에서 SetCount로 다시 지정합니다.
            Count = 1;
            
            // 그 상태 정보에서 원본 아이템 데이터가 뭔지 빼옵니다.
            ItemBase itemData = ItemData;
            
            // 만약 원본 데이터가 비어있다면 화면에 그릴 게 없으므로 에러 메시지를 띄우고 함수를 끝냅니다.
            if (itemData == null)
            {
                Debug.LogWarning("InventoryItem: ItemInstance에 ItemBase 데이터가 없어 표시할 수 없습니다.");
                return;
            }

            // 만약 아까 저장해둔 _image가 어쩌다 보니 비어있다면 다시 한번 찾아줍니다.
            if (_image == null) _image = GetComponent<Image>();
            CacheDefaultImageColor();

            // ItemData의 아이콘을 그대로 UI Image에 연결합니다.
            // 아이콘이 아직 없는 테스트 데이터라면 이미지를 비활성화해서 빈 사각형 노출을 피합니다.
            _image.sprite = itemData.Icon; // 원본 데이터에 있는 아이콘 그림을 내 이미지로 설정합니다.
            _image.enabled = itemData.Icon != null; // 그림이 있으면 화면에 보이고, 없으면 숨깁니다.
            _image.raycastTarget = true; // 마우스 클릭을 감지할 수 있도록 켜줍니다.
            _image.preserveAspect = true; // 그림이 찌그러지지 않고 원본 비율을 유지하도록 합니다.

            // ItemData의 칸 크기와 UI 크기를 맞춰 배치 계산이 어긋나지 않게 합니다.
            // 
            // 아이템의 타일 크기(예: 1x1, 2x2)에 맞춰 실제 RectTransform의 화면상 픽셀 크기를 동기화합니다.
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(itemData.Width * ItemGrid.TileWidth, itemData.Height * ItemGrid.TileHeight);
            RefreshCountText();
        }

        public void SetCount(int count)
        {
            // 외부에서 0 이하 값이 들어와도 인벤토리 UI에서는 최소 1개로 보정합니다.
            Count = Mathf.Max(1, count);
            RefreshCountText();
        }

        public void AddCount(int amount)
        {
            // 잘못된 수량 증가 요청은 무시해서 스택 수량이 음수/0 방향으로 흔들리지 않게 합니다.
            if (amount <= 0) return;

            Count = Mathf.Max(1, Count) + amount;
            RefreshCountText();
        }

        public bool TryDecreaseCount(int amount)
        {
            // 마지막 1개를 여기서 0으로 만들지 않습니다.
            // 수량이 1개 남은 아이템은 InventoryController/QuickSlotController의 기존 제거 흐름으로 처리해야
            // 그리드 점유, 목록, GameObject 제거가 한 번에 정리됩니다.
            if (amount <= 0 || Count <= amount)
                return false;

            Count -= amount;
            RefreshCountText();
            return true;
        }

        public void RefreshCountText()
        {
            EnsureCountText();

            if (countText == null || ItemInstance == null)
                return;

            // 1개일 때는 숫자를 숨겨 기존 단일 아이템 UI와 똑같이 보이게 합니다.
            // 2개 이상부터만 우하단에 수량을 표시합니다.
            int count = Mathf.Max(1, Count);
            bool showCount = count > 1;
            countText.gameObject.SetActive(showCount);
            countText.text = showCount ? count.ToString() : string.Empty;
        }

        /// <summary>
        /// 이 아이템 UI가 마우스 클릭을 막을지(true) 통과시킬지(false) 결정하는 함수입니다.
        /// </summary>
        public void SetRaycastTarget(bool value)
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;

            // 손에 든 아이템이 마우스 클릭을 먹으면(가로채면), 그 밑에 있는 장비 슬롯이나 퀵슬롯이 클릭을 받지 못합니다.
            // 그래서 마우스로 아이템을 집어 들었을 때는 클릭을 통과시키게 끄고(false), 다시 내려놨을 때는 켜줍니다(true).
            _image.raycastTarget = value;
        }

        public void SetUsePendingVisual(bool active)
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;

            CacheDefaultImageColor();

            // 주문서를 "사용 대기 중"으로만 표시합니다. 실제 삭제는 대상 선택 성공 후 InventoryTargetingController가 처리합니다.
            Color color = active ? _defaultImageColor : _image.color;
            color.a = active ? 0.2f : _defaultImageColor.a;
            _image.color = color;
        }

        private void CacheDefaultImageColor()
        {
            if (_image == null || _hasDefaultImageColor)
                return;

            // 이후 반투명 해제 시 아이템별 원래 알파값까지 복구해야 하므로 한 번만 캐싱합니다.
            _defaultImageColor = _image.color;
            _hasDefaultImageColor = true;
        }

        private void EnsureCountText()
        {
            if (countText != null)
                return;

            // 디자이너가 프리팹에 TMP_Text를 직접 연결한 경우 그 텍스트를 우선 사용합니다.
            TextMeshProUGUI generatedText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (generatedText == null)
            {
                // 현재 InventoryItem_RT 프리팹을 직접 수정하지 않아도 동작하도록 런타임 기본 텍스트를 생성합니다.
                // 나중에 프리팹에 예쁘게 만든 countText를 연결하면 이 자동 생성 경로는 타지 않습니다.
                GameObject textObject = new GameObject("CountText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.SetParent(transform, false);
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(2f, 2f);
                textRect.offsetMax = new Vector2(-4f, -2f);

                generatedText = textObject.GetComponent<TextMeshProUGUI>();
                generatedText.fontSize = 18f;
                generatedText.alignment = TextAlignmentOptions.BottomRight;
                generatedText.color = Color.white;
                generatedText.raycastTarget = false;
            }

            countText = generatedText;
        }

    }
}
