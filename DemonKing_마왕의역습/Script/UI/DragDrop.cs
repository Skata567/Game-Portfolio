using UnityEngine;
using UnityEngine.EventSystems;
//claude-monitor --view realtime


public class UIDragDrop : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 offset;

    [SerializeField] private float minX = 0f;
    [SerializeField] private float maxX = 0f;
    [SerializeField] private float minY = 0f;
    [SerializeField] private float maxY = 0f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 마우스 클릭 위치와 오브젝트 중심점 사이의 오프셋 계산
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out offset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            Vector2 localPointerPosition;

            // 마우스 위치를 캔버스 좌표계로 변환
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition))
            {
                // 오프셋을 적용한 새로운 위치 계산
                Vector2 newPosition = localPointerPosition - offset;

                // 화면 경계 제한 적용
                newPosition = ClampToCanvas(newPosition);

                rectTransform.localPosition = newPosition;
            }
        }
    }

    private Vector2 ClampToCanvas(Vector2 position)
    {
        //RectTransform canvasRect = canvas.transform as RectTransform;

        //// 캔버스 크기의 절반 값들
        //float canvasHalfWidth = canvasRect.rect.width * 0.75f;
        //float canvasHalfHeight = canvasRect.rect.height * 0.75f;

        //// UI 오브젝트 크기의 절반 값들
        //float objHalfWidth = rectTransform.rect.width * 0.25f;
        //float objHalfHeight = rectTransform.rect.height * 0.25f;

        //// 경계값 계산 (UI 오브젝트가 완전히 화면 안에 있도록)
        //float minX = -canvasHalfWidth + objHalfWidth;
        //float maxX = canvasHalfWidth - objHalfWidth;
        //float minY = -canvasHalfHeight + objHalfHeight;
        //float maxY = canvasHalfHeight - objHalfHeight;

        // 위치를 경계 내로 제한
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}