using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// InventoryController?먯꽌 異붿텧??留덉슦???낅젰 諛??쒕옒洹????쒕∼ ?대떦 ?몃뱾?ъ엯?덈떎.
    /// ?몃깽?좊━ ?곗씠??紐⑤뜽)? UI ?곗텧(酉??낅젰) 梨낆엫??遺꾨━?섍린 ?꾪빐 ?앹꽦?섏뿀?듬땲??
    /// </summary>
    public class InventoryDragDropHandler : MonoBehaviour
    {
        private InventoryController _controller;
        private InventoryHighlight _highlight;
        private Transform _canvasTransform;
        private InventoryItemUseController _useController;
        private InventoryTargetingController _targetingController;
        private GroundItemController _groundController;
        private float _doubleClickMaxDelay;
        private RectTransform _inventoryPanelRect;

        // 마우스로 집어 들어 드래그 중인 아이템입니다.
        private InventoryItem _selectedItem;
        // 諛곗튂???꾩튂???대? ?꾩씠?쒖씠 ?섎굹留?寃뱀퀜 ?덉쓣 ???꾩떆 蹂닿?
        private InventoryItem _overlapItem;
        private RectTransform _dragRect;

        private ItemGrid _dragStartGrid;
        private int _dragStartX;
        private int _dragStartY;

        private InventoryItem _lastPickedItem;
        private float _lastPickTime;

        /// <summary>
        /// 留덉슦?ㅻ줈 ?ㅺ퀬 ?덈뒗 ?꾩씠?? (?꾨Т寃껊룄 ???ㅺ퀬 ?덉쑝硫?null)
        /// </summary>
        public InventoryItem SelectedItem => _selectedItem;

        /// <summary>
        /// 寃뚯엫 ?쒖옉 ??InventoryController媛 ??而댄룷?뚰듃瑜??명똿?????몄텧?⑸땲??
        /// ?꾩슂??紐⑤뱺 ?쒖뒪?쒓낵 UI 李몄“瑜?諛쏆븘????섏〈?깆쓣 二쇱엯?댁쨳?덈떎.
        /// </summary>
        public void Initialize(InventoryController controller, InventoryHighlight highlight, Transform canvas, InventoryItemUseController useCtrl, InventoryTargetingController targetCtrl, GroundItemController groundCtrl, float doubleClickDelay, RectTransform panelRect)
        {
            _controller = controller;
            _highlight = highlight;
            _canvasTransform = canvas;
            _useController = useCtrl;
            _targetingController = targetCtrl;
            _groundController = groundCtrl;
            _doubleClickMaxDelay = doubleClickDelay;
            _inventoryPanelRect = panelRect;
        }

        /// <summary>
        /// ?ㅺ퀬 ?덈뒗 ?꾩씠???곹깭瑜??꾩쟾??珥덇린??鍮꾩슦湲??⑸땲??
        /// ?몃깽?좊━媛 媛뺤젣濡??ロ엳嫄곕굹, ?꾩씠?쒖씠 利앸컻?댁빞 ???곹솴??遺덈┰?덈떎.
        /// </summary>
        public void ClearSelectedState()
        {
            _selectedItem = null;
            _overlapItem = null;
            _dragRect = null;
            _lastPickedItem = null;
            ClearDragStartPosition();
            _highlight?.Show(false);
        }

        /// <summary>
        /// ?쒕옒洹몃? ?쒖옉?덈뜕 '?먮옒 ?꾩튂' ?뺣낫瑜?吏?곷땲??
        /// 諛곗튂媛 ?꾨즺?섏뿀嫄곕굹 痍⑥냼 泥섎━媛 ?앸굹?????댁긽 ?쏅궇 ?꾩튂瑜?湲곗뼲???꾩슂媛 ?놁쓣 ???몄텧?⑸땲??
        /// </summary>
        private void ClearDragStartPosition()
        {
            _dragStartGrid = null;
            _dragStartX = 0;
            _dragStartY = 0;
        }

        /// <summary>
        /// 留??꾨젅?꾨쭏??留덉슦???낅젰??諛쏆븘 ?쒕옒洹?UI瑜??대룞?쒗궎怨?
        /// ?대┃ ?낅젰??泥댄겕?섏뿬 ?꾩씠?쒖쓣 吏묎굅???대젮?볥뒗 ?꾨컲?곸씤 怨쇱젙???듭젣?⑸땲??
        /// InventoryController??Update?먯꽌 留??꾨젅?????⑥닔瑜??몄텧?⑸땲??
        /// </summary>
        public void HandleUpdate(bool isInventoryOpen, bool isPointerInputBlocked)
        {
            if (!isInventoryOpen)
            {
                _highlight?.Show(false);
                return;
            }

            DragSelectedItemIcon();
            _controller.RefreshSelectedGridUnderMouse();

            if (_targetingController != null && _targetingController.BlocksPointerInput)
            {
                _highlight?.Show(false);
                return;
            }

            if (isPointerInputBlocked)
            {
                _highlight?.Show(false);
                return;
            }

            if (Input.GetMouseButtonDown(0))
                HandleLeftClick();

            if (_controller.SelectedGrid == null)
            {
                _highlight?.Show(false);
                return;
            }

            UpdateHighlight();
        }

        /// <summary>
        /// ?붾㈃??留덉슦??醫뚰겢由?씠 ?ㅼ뼱?붿쓣 ???몄텧?⑸땲??
        /// ?몃깽?좊━ 諛붽묑???뚮??붿?(?꾩씠??踰꾨━湲?, ?붾툝?대┃?몄?(?꾩씠???ъ슜),
        /// 洹몃━???대?瑜??뚮??붿?(吏묎린/?대젮?볤린)瑜?遺꾧린?섏뿬 ?곸젅???됰룞??痍⑦빀?덈떎.
        /// </summary>
        private void HandleLeftClick()
        {
            if (_selectedItem != null && IsPointerOutsideInventoryPanel())
            {
                TryDropSelectedItemToGround();
                _lastPickedItem = null;
                return;
            }

            if (_selectedItem != null && IsDoubleClickOnSelectedItem())
            {
                _useController?.TryUseDoubleClickedItem(_selectedItem);
                _lastPickedItem = null;
                return;
            }

            if (_controller.SelectedGrid == null) return;

            Vector2Int gridPos = GetMouseGridPosition();

            if (_selectedItem == null) TryPickUpItem(gridPos);
            else TryPlaceSelectedItem(gridPos);
        }

        /// <summary>
        /// ?몃깽?좊━ 移몄뿉 ?덈뒗 ?꾩씠?쒖쓣 留덉슦?ㅻ줈 吏묒뼱 ?щ┰?덈떎.
        /// ???⑥닔媛 ?ㅽ뻾?섎㈃ ?꾩씠?쒖? ?몃깽?좊━ 洹몃━?쒖뿉??鍮좎졇?섏? 留덉슦?ㅻ? ?곕씪?ㅻ땲寃??⑸땲??
        /// </summary>
        /// <param name="gridPos">留덉슦?ㅺ? 媛由ы궎怨??덈뒗 洹몃━??移몄쓽 醫뚰몴</param>
        private void TryPickUpItem(Vector2Int gridPos)
        {
            InventoryItem item = _controller.SelectedGrid.GetItemAt(gridPos.x, gridPos.y);
            if (item == null) return;

            SaveDragStartPosition(item);

            _selectedItem = _controller.SelectedGrid.PickUpItem(gridPos.x, gridPos.y);
            if (_selectedItem == null) return;

            _selectedItem.SetRaycastTarget(false);
            _lastPickedItem = _selectedItem;
            _lastPickTime = Time.unscaledTime;
            _dragRect = _selectedItem.GetComponent<RectTransform>();

            _dragRect.SetParent(_canvasTransform, true);
            BringDraggedItemToFront();
            RequestMoveAudio(_selectedItem);
        }

        /// <summary>
        /// 留덉슦?ㅼ뿉 ?ㅺ퀬 ?덈뜕 ?꾩씠?쒖쓣 ?꾩옱 留덉슦?ㅺ? 媛由ы궎???몃깽?좊━ 移몄뿉 ?대젮?볦뒿?덈떎.
        /// 留뚯빟 洹?移몄뿉 ?ㅻⅨ ?꾩씠?쒖씠 1媛?寃뱀퀜?덈떎硫??쒕줈???꾩튂瑜?留욌컮轅됰땲??(?ㅼ솑).
        /// </summary>
        /// <param name="gridPos">?꾩씠?쒖쓣 ?대젮?볦쓣 ?몃깽?좊━ 洹몃━??醫뚰몴</param>
        private void TryPlaceSelectedItem(Vector2Int gridPos)
        {
            InventoryItem placedItem = _selectedItem;
            bool placed = _controller.SelectedGrid.TryPlaceItem(_selectedItem, gridPos.x, gridPos.y, ref _overlapItem);
            if (!placed)
            {
                if (_controller.SelectedGrid.AcceptOnlyOneByOneItems)
                    CancelDragAndReturnItem();
                return;
            }

            placedItem?.SetRaycastTarget(true);
            _selectedItem = null;
            if (_overlapItem != null)
            {
                _selectedItem = _overlapItem;
                _overlapItem = null;
                _selectedItem.SetRaycastTarget(false);
                _dragRect = _selectedItem.GetComponent<RectTransform>();
                _dragRect.SetParent(_canvasTransform, true);
                BringDraggedItemToFront();
                RequestMoveAudio(_selectedItem);
            }
            else
            {
                _dragRect = null;
                ClearDragStartPosition();
            }

            _controller.RaiseInventoryChanged();
        }

        private static void RequestMoveAudio(InventoryItem item)
        {
            InventoryMoveAudio.RequestMoveAudio(item);
        }

        /// <summary>
        /// 留덉슦?ㅻ줈 吏묎퀬 ?덈뜕 ?꾩씠?쒖쓣 ?몃깽?좊━ 李?諛뽰쑝濡??쒕옒洹명빐???볦븯?????몄텧?⑸땲??
        /// ?꾩씠?쒖쓣 ?ъ쓽 諛붾떏(?뚮젅?댁뼱 諛쒕컩)?쇰줈 ?ㅼ젣濡?踰꾨━???쒕∼) 泥섎━瑜??섑뻾?⑸땲??
        /// </summary>
        private void TryDropSelectedItemToGround()
        {
            if (_selectedItem == null || _selectedItem.ItemInstance == null) return;

            if (_groundController == null)
            {
                Debug.LogWarning("InventoryDragDropHandler: GroundItemController瑜?李얠? 紐삵빐 ?꾩씠?쒖쓣 踰꾨┫ ???놁뒿?덈떎.");
                return;
            }

            InventoryItem itemToDrop = _selectedItem;
            if (!_groundController.DropItem(itemToDrop.ItemInstance, itemToDrop.Count)) return;

            _controller.TryConsumeHeldItem(itemToDrop, true);
        }

        /// <summary>
        /// ?꾩씠?쒖쓣 吏묒뼱?щ┫ ?? ?먮옒 ?대뒓 移몄뿉 ?덉뿀?붿? 諛깆뾽?대몼?덈떎.
        /// ?섏쨷??諛곗튂媛 遺덇??ν빐??痍⑥냼??寃쎌슦 ???꾩튂濡??덉쟾?섍쾶 ?섎룎?꾧?湲??꾪븿?낅땲??
        /// </summary>
        private void SaveDragStartPosition(InventoryItem item)
        {
            _dragStartGrid = _controller.SelectedGrid;
            _dragStartX = item.OnGridPositionX;
            _dragStartY = item.OnGridPositionY;
        }

        /// <summary>
        /// ?ㅺ퀬 ?덈뒗 ?꾩씠??UI ?꾩씠肄섏씠 留??꾨젅??留덉슦??而ㅼ꽌 ?꾩튂瑜??곕씪?ㅻ땲?꾨줉 醫뚰몴瑜?媛깆떊?⑸땲??
        /// </summary>
        private void DragSelectedItemIcon()
        {
            if (_selectedItem == null || _dragRect == null) return;

            _dragRect.position = Input.mousePosition;
            BringDraggedItemToFront();
        }

        /// <summary>
        /// ?쒕옒洹?以묒씤 ?꾩씠?쒖씠 ?ㅻⅨ UI ?⑤꼸?대굹 ?꾩씠???꾩씠肄?諛묒뿉 源붾━吏 ?딅룄濡?        /// 罹붾쾭???뚮뜑留??쒖꽌??留??욎쑝濡??밴꺼?듬땲??
        /// </summary>
        private void BringDraggedItemToFront()
        {
            if (_dragRect == null) return;
            _dragRect.SetAsLastSibling();
        }

        /// <summary>
        /// ?쒕옒洹?以묒뿉 ?몃깽?좊━ 李쎌씠 ?ロ엳嫄곕굹 ?좏슚?섏? ?딆? 怨녹뿉 ?볦븘???쒕옒洹멸? 痍⑥냼????遺由낅땲??
        /// 吏묒뿀???꾩씠?쒖쓣 ?먮옒 ?덈뜕 ?몃깽?좊━ 移몄쑝濡??ㅻⅤ瑜??섎룎?ㅻ넃?듬땲??
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
                    returned = _controller.TryReturnSelectedItemToDefaultGrid(_selectedItem);
                }

                if (!returned)
                {
                    Debug.LogWarning("InventoryDragDropHandler: ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖쓣 ?섎룎由?鍮??몃깽?좊━ 移몄쓣 李얠? 紐삵뻽?듬땲??");
                    return;
                }

                _controller.RaiseInventoryChanged();
            }

            ClearSelectedState();
        }

        /// <summary>
        /// 諛⑷툑 留덉슦???대┃??"?붾툝?대┃"?댁뿀?붿? ?먮퀎?⑸땲??
        /// 吏㏃? ?쒓컙(_doubleClickMaxDelay) ?덉뿉 媛숈? ?꾩씠?쒖쓣 ??踰??꾨Ⅴ硫??붾툝?대┃?쇰줈 ?몄젙?⑸땲??
        /// </summary>
        private bool IsDoubleClickOnSelectedItem()
        {
            if (_selectedItem == null || _lastPickedItem != _selectedItem)
                return false;
            return Time.unscaledTime - _lastPickTime <= _doubleClickMaxDelay;
        }

        /// <summary>
        /// ?꾩옱 留덉슦?ㅼ쓽 ?ㅽ겕由??붾㈃) ?쎌? 醫뚰몴瑜? ?몃깽?좊━ 移몄쓽 2D 寃⑹옄 醫뚰몴(Grid Position)濡?蹂?섑빀?덈떎.
        /// ?꾩씠?쒖쓽 媛濡??몃줈 ?ш린(Width/Height)媛 ??寃쎌슦, 以묒떖?먯씠 ?뉖굹媛吏 ?딄쾶 ?꾩튂瑜?蹂댁젙?댁쨳?덈떎.
        /// </summary>
        private Vector2Int GetMouseGridPosition()
        {
            Vector2 mousePos = Input.mousePosition;
            if (_selectedItem != null)
            {
                mousePos.x -= (_selectedItem.ItemData.Width - 1) * ItemGrid.TileWidth / 2f;
                mousePos.y += (_selectedItem.ItemData.Height - 1) * ItemGrid.TileHeight / 2f;
            }
            return _controller.SelectedGrid.ScreenToGridPosition(mousePos);
        }

        /// <summary>
        /// ?꾩옱 留덉슦??而ㅼ꽌媛 ?몃깽?좊━ 李?UI) 諛붽묑???덈뒗吏 寃?ы빀?덈떎.
        /// 諛붽묑???덈떎硫??꾩씠?쒖쓣 ?낆뿉 踰꾨━湲??꾪븳 ?먯젙?쇰줈 ?ъ슜?⑸땲??
        /// </summary>
        private bool IsPointerOutsideInventoryPanel()
        {
            RectTransform panelRect = _inventoryPanelRect != null ? _inventoryPanelRect : _controller.ResolveInventoryPanelRect();
            if (panelRect == null) return false;

            Canvas canvas = panelRect.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

            return !RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition, eventCamera);
        }

        /// <summary>
        /// 留덉슦??而ㅼ꽌 ?꾨옒???덈뒗 移몄뿉 ?몃????뚮몢由??섏씠?쇱씠??瑜?爾먯꽌 
        /// ?닿? 吏湲??대뼡 移몄쓣 ?좏깮?섍퀬 ?덈뒗吏, ?뱀? ?ㅺ퀬 ?덈뒗 ?꾩씠?쒖씠 ?ㅼ뼱媛????덈뒗吏 ?쒓컖?곸쑝濡?蹂댁뿬以띾땲??
        /// </summary>
        private void UpdateHighlight()
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (_selectedItem == null)
            {
                InventoryItem target = _controller.SelectedGrid.GetItemAt(gridPos.x, gridPos.y);
                if (target == null)
                {
                    _highlight?.Show(false);
                    return;
                }
                _highlight?.Show(true);
                _highlight?.UpdateSize(target);
                _highlight?.UpdatePosition(_controller.SelectedGrid.GetLocalPositionForItem(target, target.OnGridPositionX, target.OnGridPositionY));
            }
            else
            {
                bool valid = _controller.SelectedGrid.CanAcceptItem(_selectedItem)
                    && _controller.SelectedGrid.IsWithinBoundary(gridPos.x, gridPos.y, _selectedItem.ItemData.Width, _selectedItem.ItemData.Height);
                _highlight?.Show(valid);
                _highlight?.UpdateSize(_selectedItem);
                _highlight?.UpdatePosition(_controller.SelectedGrid.GetLocalPositionForItem(_selectedItem, gridPos.x, gridPos.y));
            }
        }

        /// <summary>
        /// 媛뺤젣濡??뱀젙 ?꾩씠?쒖쓣 '留덉슦?ㅻ줈 吏묒뼱???곹깭'濡?留뚮벊?덈떎.
        /// ?λ퉬 李쎌뿉???λ퉬瑜?踰쀬뼱 ?몃깽?좊━濡?媛?몄삤嫄곕굹 ?????ъ슜?⑸땲??
        /// </summary>
        public void HoldItem(InventoryItem item, ItemGrid returnGrid, int returnX, int returnY)
        {
            if (item == null) return;
            if (_selectedItem != null) return;

            item.gameObject.SetActive(true);
            item.SetRaycastTarget(false);
            _selectedItem = item;
            _overlapItem = null;
            _dragRect = item.GetComponent<RectTransform>();

            if (_dragRect != null)
            {
                _dragRect.SetParent(_canvasTransform, true);
                _dragRect.position = Input.mousePosition;
                BringDraggedItemToFront();
            }

            _dragStartGrid = returnGrid;
            _dragStartX = returnX;
            _dragStartY = returnY;
            _controller.RaiseInventoryChanged();
        }
    }
}
