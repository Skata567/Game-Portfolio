using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrototypeRT
{
    /// <summary>
    /// 게임 화면 HUD에 퀵슬롯 아이콘과 1~0 번호를 표시합니다.
    /// 실제 슬롯 데이터는 QuickSlotController가 들고, 이 클래스는 표시만 갱신합니다.
    /// </summary>
    public class QuickSlotHUDView : MonoBehaviour
    {
        // 키 입력은 1~9, 0 순서지만 배열 인덱스는 0~9라서 표시용 라벨을 따로 둡니다.
        private static readonly string[] SlotLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        [Header("데이터 참조")]
        [Tooltip("퀵슬롯 데이터와 변경 이벤트를 제공하는 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private QuickSlotController quickSlotController;

        [Header("HUD 표시")]
        [Tooltip("퀵슬롯 1~0번에 대응하는 아이콘 Image 배열입니다. 순서는 1,2,3,4,5,6,7,8,9,0 입니다.")]
        [SerializeField] private Image[] slotIcons = new Image[10];

        [Tooltip("선택 사항입니다. 연결하면 각 슬롯에 1~0 번호를 표시합니다.")]
        [SerializeField] private TMP_Text[] slotNumberTexts = new TMP_Text[10];

        [Tooltip("퀵슬롯 아이템 수량 표시 텍스트입니다. 비워두면 각 아이콘 아래에 CountText를 자동 생성합니다.")]
        // TODO(UI): 퀵슬롯 수량 숫자의 폰트/위치/색을 GUI에서 직접 꾸밀 때는
        // 각 슬롯 프리팹에 TMP_Text를 만들고 이 배열에 연결하면 됩니다.
        // 비워두면 GetOrCreateCountText()에서 임시 CountText를 자동 생성합니다.
        [SerializeField] private TMP_Text[] slotCountTexts = new TMP_Text[10];

        [Header("Selected Slot")]
        [Tooltip("Optional highlight images for slots. If empty, the icon parent Image is used.")]
        [SerializeField] private Image[] slotHighlightImages = new Image[10];

        [Tooltip("Color applied to the selected quick slot.")]
        [SerializeField] private Color selectedSlotColor = new Color(1f, 0.9f, 0.35f, 1f);

        [Tooltip("Scale multiplier for the selected quick slot icon.")]
        [SerializeField, Min(1f)] private float selectedSlotScale = 1.12f;

        private Vector3[] _baseIconScales;
        private Color[] _baseHighlightColors;
        private Color[] _baseNumberColors;

        /// <summary>
        /// HUD 표시를 위한 참조와 클릭 방해 방지 설정을 준비합니다.
        /// 퀵슬롯 아이콘은 보여주기 전용이므로 raycast를 꺼서 드래그/호버 입력을 막지 않게 합니다.
        /// </summary>
        private void Awake()
        {
            // 인스펙터 연결이 빠진 경우에도 HUD가 동작하도록 씬에서 QuickSlotController를 찾습니다.
            if (quickSlotController == null)
                quickSlotController = FindFirstObjectByType<QuickSlotController>();

            CacheSlotDefaults();
            DisableIconRaycasts();
        }

        /// <summary>
        /// 퀵슬롯 데이터 변경 이벤트를 구독하고, 현재 상태를 즉시 화면에 반영합니다.
        /// HUD가 꺼져 있는 동안 슬롯 내용이 바뀌었을 수 있으므로 Refresh를 바로 호출합니다.
        /// </summary>
        private void OnEnable()
        {
            if (quickSlotController != null)
            {
                quickSlotController.OnQuickSlotsChanged += Refresh;
                quickSlotController.OnSelectedSlotChanged += OnSelectedSlotChanged;
            }

            // HUD가 꺼져 있는 동안 아이템이 바뀌었을 수 있으므로 켜질 때 즉시 한 번 맞춥니다.
            Refresh();
        }

        /// <summary>
        /// HUD가 꺼질 때 이벤트 구독을 해제합니다.
        /// 비활성 UI가 계속 갱신되거나 같은 이벤트를 여러 번 받는 문제를 막습니다.
        /// </summary>
        private void OnDisable()
        {
            if (quickSlotController != null)
            {
                quickSlotController.OnQuickSlotsChanged -= Refresh;
                quickSlotController.OnSelectedSlotChanged -= OnSelectedSlotChanged;
            }
        }

        /// <summary>
        /// QuickSlotController의 현재 슬롯 상태를 읽어 HUD 아이콘과 번호 텍스트를 다시 씁니다.
        /// 아이템이 없는 슬롯은 Image를 꺼서 빈 슬롯처럼 보이게 합니다.
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < slotIcons.Length; i++)
            {
                // HUD는 실제 아이템을 소유하지 않고, 컨트롤러가 가진 슬롯 상태만 읽어 표시합니다.
                InventoryItem item = quickSlotController != null ? quickSlotController.GetItem(i) : null;
                Sprite icon = item != null && item.ItemData != null ? item.ItemData.Icon : null;

                if (slotIcons[i] != null)
                {
                    // sprite만 비우면 이전 아이콘 영역이 남을 수 있어, 아이콘이 없을 때 Image 자체를 끕니다.
                    slotIcons[i].sprite = icon;
                    slotIcons[i].enabled = icon != null;
                }

                // 퀵슬롯 수량 숫자 갱신 지점입니다.
                // InventoryItem.Count를 읽어서 2개 이상일 때만 작은 숫자를 표시합니다.
                RefreshCountText(i, item);
                RefreshSelectedVisual(i);
            }

            for (int i = 0; i < slotNumberTexts.Length && i < SlotLabels.Length; i++)
            {
                if (slotNumberTexts[i] != null)
                {
                    // 번호 텍스트는 슬롯 내용과 무관하게 항상 고정 라벨을 보여줍니다.
                    slotNumberTexts[i].text = SlotLabels[i];
                    RefreshSelectedVisual(i);
                }
            }
        }

        private void OnSelectedSlotChanged(int selectedIndex)
        {
            Refresh();
        }

        private void RefreshSelectedVisual(int slotIndex)
        {
            if (slotIndex < 0)
                return;

            bool selected = quickSlotController != null && quickSlotController.SelectedIndex == slotIndex;
            Image targetImage = GetSlotHighlightImage(slotIndex);
            if (targetImage != null)
                targetImage.color = selected ? selectedSlotColor : GetBaseHighlightColor(slotIndex);

            if (slotIcons != null && slotIndex < slotIcons.Length && slotIcons[slotIndex] != null)
            {
                Vector3 baseScale = GetBaseIconScale(slotIndex);
                slotIcons[slotIndex].transform.localScale = selected ? baseScale * selectedSlotScale : baseScale;
            }

            if (slotNumberTexts != null && slotIndex < slotNumberTexts.Length && slotNumberTexts[slotIndex] != null)
                slotNumberTexts[slotIndex].color = selected ? selectedSlotColor : GetBaseNumberColor(slotIndex);
        }

        private Image GetSlotHighlightImage(int slotIndex)
        {
            if (slotHighlightImages != null && slotIndex < slotHighlightImages.Length && slotHighlightImages[slotIndex] != null)
                return slotHighlightImages[slotIndex];

            if (slotIcons == null || slotIndex >= slotIcons.Length || slotIcons[slotIndex] == null)
                return null;

            Transform parent = slotIcons[slotIndex].transform.parent;
            Image parentImage = parent != null ? parent.GetComponent<Image>() : null;
            return parentImage != null ? parentImage : slotIcons[slotIndex];
        }

        private void CacheSlotDefaults()
        {
            if (slotIcons == null)
                return;

            _baseIconScales = new Vector3[slotIcons.Length];
            _baseHighlightColors = new Color[slotIcons.Length];
            for (int i = 0; i < slotIcons.Length; i++)
            {
                _baseIconScales[i] = slotIcons[i] != null ? slotIcons[i].transform.localScale : Vector3.one;
                Image targetImage = GetSlotHighlightImage(i);
                _baseHighlightColors[i] = targetImage != null ? targetImage.color : Color.white;
            }

            if (slotNumberTexts == null)
                return;

            _baseNumberColors = new Color[slotNumberTexts.Length];
            for (int i = 0; i < slotNumberTexts.Length; i++)
                _baseNumberColors[i] = slotNumberTexts[i] != null ? slotNumberTexts[i].color : Color.white;
        }

        private Vector3 GetBaseIconScale(int slotIndex)
        {
            if (_baseIconScales == null || slotIndex < 0 || slotIndex >= _baseIconScales.Length)
                return Vector3.one;

            return _baseIconScales[slotIndex];
        }

        private Color GetBaseHighlightColor(int slotIndex)
        {
            if (_baseHighlightColors == null || slotIndex < 0 || slotIndex >= _baseHighlightColors.Length)
                return Color.white;

            return _baseHighlightColors[slotIndex];
        }

        private Color GetBaseNumberColor(int slotIndex)
        {
            if (_baseNumberColors == null || slotIndex < 0 || slotIndex >= _baseNumberColors.Length)
                return Color.white;

            return _baseNumberColors[slotIndex];
        }

        /// <summary>
        /// HUD 아이콘 Image들이 마우스 입력을 가로채지 않도록 raycastTarget을 끕니다.
        /// 실제 드래그/호버 판정은 뒤에 있는 ItemGrid와 InventoryItem이 처리해야 합니다.
        /// </summary>
        private void DisableIconRaycasts()
        {
            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (slotIcons[i] != null)
                    // 아이콘이 포인터를 먹으면 ItemGrid의 hover/drag 판정이 흔들릴 수 있어 표시 전용으로 둡니다.
                    slotIcons[i].raycastTarget = false;
            }
        }

        private void RefreshCountText(int slotIndex, InventoryItem item)
        {
            // 퀵슬롯 숫자 표시의 시작점입니다.
            // 실제 텍스트 오브젝트는 인스펙터 연결값을 우선 쓰고, 없으면 자동 생성합니다.
            TMP_Text countText = GetOrCreateCountText(slotIndex);
            if (countText == null)
                return;

            // 인벤토리와 같은 규칙입니다. 1개는 숨기고 2개 이상만 표시합니다.
            int count = item != null ? item.Count : 1;
            bool showCount = item != null && count > 1;
            countText.gameObject.SetActive(showCount);
            countText.text = showCount ? count.ToString() : string.Empty;
        }

        private TMP_Text GetOrCreateCountText(int slotIndex)
        {
            // TODO(UI): 퀵슬롯 수량 숫자 자동 생성/배치 코드는 이 함수 안을 보면 됩니다.
            // 권장 방식은 자동 생성값 수정이 아니라, 프리팹에 직접 CountText를 만들고 slotCountTexts에 연결하는 것입니다.
            if (slotIndex < 0 || slotIndex >= slotIcons.Length)
                return null;

            if (slotCountTexts == null || slotCountTexts.Length != slotIcons.Length)
                slotCountTexts = new TMP_Text[slotIcons.Length];

            if (slotCountTexts[slotIndex] != null)
                return slotCountTexts[slotIndex];

            Image icon = slotIcons[slotIndex];
            if (icon == null)
                return null;

            // 아이콘 자식 중 이름이 "CountText"인 TMP_Text가 있으면 그걸 재사용합니다.
            // 슬롯 번호 텍스트와 섞이지 않도록 이름 규칙을 둔 겁니다.
            TextMeshProUGUI generatedText = FindExistingCountText(icon.transform);
            if (generatedText == null)
            {
                // HUD 프리팹을 당장 수정하지 않아도 수량 표시가 보이도록 기본 CountText를 만든다.
                // 나중에 디자이너가 직접 만든 텍스트를 slotCountTexts에 연결하면 이 경로는 쓰이지 않는다.
                // ===== 퀵슬롯 수량 숫자 자동 생성 구간 시작 =====
                // TODO(UI): 임시 표시용 기본값입니다.
                // 폰트 교체는 generatedText.font, 크기는 fontSize, 위치는 아래 RectTransform offset을 수정하세요.
                // 나중에 제대로 꾸밀 거면 슬롯 프리팹에 CountText를 직접 만들고 slotCountTexts에 연결하는 쪽이 좋습니다.
                GameObject textObject = new GameObject("CountText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.SetParent(icon.transform, false);

                // 아이콘 전체 영역을 기준으로 우하단에 붙입니다.
                // 숫자 위치를 옮기고 싶으면 anchor/offset 값을 조정하세요.
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(2f, 2f);
                textRect.offsetMax = new Vector2(-4f, -2f);

                generatedText = textObject.GetComponent<TextMeshProUGUI>();

                // TODO(UI): 퀵슬롯 수량 숫자 기본 스타일입니다.
                // 폰트, 크기, 정렬, 색상을 여기서 바꿀 수 있습니다.
                generatedText.fontSize = 14f;
                generatedText.alignment = TextAlignmentOptions.BottomRight;
                generatedText.color = Color.white;
                generatedText.raycastTarget = false;
                // ===== 퀵슬롯 수량 숫자 자동 생성 구간 끝 =====
            }

            slotCountTexts[slotIndex] = generatedText;
            return generatedText;
        }

        private static TextMeshProUGUI FindExistingCountText(Transform parent)
        {
            if (parent == null)
                return null;

            // 프리팹에 직접 만든 수량 텍스트를 찾는 부분입니다.
            // 이름이 CountText인 TMP_Text만 수량 숫자로 인정합니다.
            TextMeshProUGUI[] texts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                if (text != null && text.gameObject.name == "CountText")
                    return text;
            }

            return null;
        }
    }
}
