using UnityEngine;
using UnityEngine.EventSystems;

namespace PrototypeRT
{
    /// <summary>
    /// 마우스가 어떤 ItemGrid 위에 있는지 InventoryController에 알려주는 연결 컴포넌트입니다.
    /// 인벤토리 그리드가 여러 개가 되더라도, 컨트롤러는 현재 포인터가 올라간 그리드만 대상으로 입력을 처리합니다.
    /// </summary>
    [RequireComponent(typeof(ItemGrid))]
    public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("입력을 넘겨줄 인벤토리 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private InventoryController inventoryController;

        private ItemGrid _itemGrid;

        private void Awake()
        {
            _itemGrid = GetComponent<ItemGrid>();
            if (inventoryController == null)
                inventoryController = FindFirstObjectByType<InventoryController>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 포인터가 들어온 그리드를 현재 조작 대상으로 지정합니다.
            if (inventoryController != null) inventoryController.SelectedGrid = _itemGrid;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 그리드 밖으로 나가면 배치/하이라이트 계산을 멈춥니다.
            if (inventoryController != null) inventoryController.ClearSelectedGridIfCurrent(_itemGrid);
        }
    }
}
