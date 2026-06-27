using UnityEngine;

public class DragMap : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] float dragSpeed = 5f;
    [SerializeField] float wheelScrollSpeed = 3f;

    [Header("카메라 이동 제한")]
    [SerializeField] float minX = -10f;  // 왼쪽 끝
    [SerializeField] float maxX = 10f;   // 오른쪽 끝
    [SerializeField] float minY = -5f;   // 아래쪽 끝 
    [SerializeField] float maxY = 5f;    // 위쪽 끝  
    [SerializeField] bool limitY = false; // Y축 제한 여부

    private UIManager uimanager;
    private Camera cam;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private TutorialStartPanel tutorialStartPanel;

    void Start()
    {
        uimanager = FindAnyObjectByType<UIManager>();

        cam = Camera.main;
        if (cam == null)
            cam = FindAnyObjectByType<Camera>();

        // 튜토리얼 시작 패널 찾기 (비활성화된 것도 포함)
        tutorialStartPanel = FindAnyObjectByType<TutorialStartPanel>(FindObjectsInactive.Include);
    }

    void Update()
    {
        // 튜토리얼 모드 중에는 맵 드래그 비활성화
        if (TutorialManager.IsTutorialMode)
        {
            return;
        }

        // 튜토리얼 스킵 패널이 활성화되어 있으면 드래그 비활성화
        if (tutorialStartPanel != null && tutorialStartPanel.gameObject.activeInHierarchy)
        {
            return;
        }

        if(!uimanager.windowBg.activeInHierarchy)
        {
            HandleMouseDrag();
            HandleMouseWheel();
        }
    }

    void HandleMouseDrag()
    {
        // 마우스 드래그 시작
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            isDragging = true;
        }

        // 드래그 중
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 mouseDelta = currentMousePosition - lastMousePosition;

            // 마우스 이동량을 월드 좌표로 변환
            Vector3 worldDelta = cam.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, cam.nearClipPlane));
            worldDelta = worldDelta - cam.ScreenToWorldPoint(Vector3.zero);

            // 카메라를 반대 방향으로 이동 (드래그한 방향으로 맵이 따라가는 느낌)
            Vector3 newPosition = cam.transform.position - new Vector3(worldDelta.x * dragSpeed,
                                                                       limitY ? worldDelta.y * dragSpeed : 0,
                                                                       0);

            // 위치 제한 적용
            cam.transform.position = ClampCameraPosition(newPosition);

            lastMousePosition = currentMousePosition;
        }

        // 드래그 종료
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    void HandleMouseWheel()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 휠 위로: 오른쪽 이동 (양수)
            // 휠 아래로: 왼쪽 이동 (음수)
            float moveAmount = scroll * wheelScrollSpeed * Time.deltaTime;

            Vector3 newPosition = cam.transform.position + new Vector3(moveAmount, 0, 0);
            cam.transform.position = ClampCameraPosition(newPosition);
        }
    }

    Vector3 ClampCameraPosition(Vector3 position)
    {
        // X축 제한
        position.x = Mathf.Clamp(position.x, minX, maxX);

        // Y축 제한 (옵션)
        if (limitY)
        {
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        return position;
    }

    // 디버그용 - 카메라 이동 범위 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // X축 제한선
        Vector3 leftLimit = new Vector3(minX, transform.position.y, transform.position.z);
        Vector3 rightLimit = new Vector3(maxX, transform.position.y, transform.position.z);

        Gizmos.DrawLine(leftLimit + Vector3.up * 2, leftLimit + Vector3.down * 2);
        Gizmos.DrawLine(rightLimit + Vector3.up * 2, rightLimit + Vector3.down * 2);

        if (limitY)
        {
            // Y축 제한선
            Vector3 topLimit = new Vector3(transform.position.x, maxY, transform.position.z);
            Vector3 bottomLimit = new Vector3(transform.position.x, minY, transform.position.z);

            Gizmos.DrawLine(topLimit + Vector3.left * 2, topLimit + Vector3.right * 2);
            Gizmos.DrawLine(bottomLimit + Vector3.left * 2, bottomLimit + Vector3.right * 2);
        }

        // 이동 가능 영역 박스
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Vector3 center = new Vector3((minX + maxX) / 2, limitY ? (minY + maxY) / 2 : transform.position.y, transform.position.z);
        Vector3 size = new Vector3(maxX - minX, limitY ? maxY - minY : 0.1f, 0.1f);
        Gizmos.DrawCube(center, size);
    }
}