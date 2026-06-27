using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 전투 씬 클릭 문제 디버깅 도구
/// 클릭 시 모든 Raycast 정보를 출력하여 어디서 막히는지 확인
/// </summary>
public class ClickDebugger : MonoBehaviour
{
    [Header("디버그 설정")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private bool showOnScreenLog = true;
    [SerializeField] private KeyCode debugKey = KeyCode.F1; // F1 누르면 씬 정보 출력
    [SerializeField] private KeyCode clearKey = KeyCode.F2; // F2 누르면 로그 클리어

    [Header("UI 표시 설정")]
    [SerializeField] private int maxLogLines = 20;
    [SerializeField] private float logFontSize = 14f;

    private List<string> onScreenLogs = new List<string>();
    private StringBuilder logBuilder = new StringBuilder();

    #region Unity Lifecycle

    void Update()
    {
        if (!enableDebug) return;

        // 마우스 클릭 시 디버그 정보 출력
        if (Input.GetMouseButtonDown(0))
        {
            DebugClick();
        }

        // F1: 씬 전체 분석
        if (Input.GetKeyDown(debugKey))
        {
            AnalyzeScene();
        }

        // F2: 로그 클리어
        if (Input.GetKeyDown(clearKey))
        {
            ClearLogs();
        }
    }

    void OnGUI()
    {
        if (!enableDebug || !showOnScreenLog) return;

        // 화면 왼쪽 위에 로그 표시
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = (int)logFontSize;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;

        // 배경 박스
        GUI.Box(new Rect(10, 10, 600, maxLogLines * (logFontSize + 2) + 40), "");

        // 헤더
        GUI.Label(new Rect(20, 15, 580, 30),
            "<b>클릭 디버거</b> | F1: 씬 분석 | F2: 클리어", style);

        // 로그 출력
        float yPos = 45;
        int startIndex = Mathf.Max(0, onScreenLogs.Count - maxLogLines);
        for (int i = startIndex; i < onScreenLogs.Count; i++)
        {
            GUI.Label(new Rect(20, yPos, 580, logFontSize + 2), onScreenLogs[i], style);
            yPos += logFontSize + 2;
        }
    }

    #endregion

    #region 클릭 디버깅

    /// <summary>
    /// 클릭 위치에서 모든 Raycast 결과 출력
    /// </summary>
    void DebugClick()
    {
        Vector3 mousePos = Input.mousePosition;

        AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        AddLog($"<color=yellow>클릭: ({mousePos.x:F0}, {mousePos.y:F0})</color>");

        // 1. Physics Raycast (3D)
        DebugPhysicsRaycast(mousePos);

        // 2. UI Raycast (EventSystem)
        DebugUIRaycast(mousePos);

        // 3. Canvas 계층 확인
        DebugCanvasHierarchy(mousePos);

        AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }

    /// <summary>
    /// Physics Raycast 결과 (3D Collider)
    /// </summary>
    void DebugPhysicsRaycast(Vector3 mousePos)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            AddLog("<color=red>❌ Camera.main 없음</color>");
            return;
        }

        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

        if (hits.Length > 0)
        {
            AddLog($"<color=cyan>🎯 Physics Hit: {hits.Length}개</color>");
            foreach (var hit in hits)
            {
                string unitInfo = "";
                var unit = hit.collider.GetComponent<Unit_1001Upgrade>();
                if (unit != null)
                {
                    unitInfo = $" (유닛: {unit.unitName})";
                }

                AddLog($"  └─ {hit.collider.gameObject.name}{unitInfo}");
                AddLog($"     Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, 거리: {hit.distance:F2}");
            }
        }
        else
        {
            AddLog("<color=gray>✓ Physics Hit 없음</color>");
        }
    }

    /// <summary>
    /// UI Raycast 결과 (EventSystem)
    /// </summary>
    void DebugUIRaycast(Vector3 mousePos)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            AddLog("<color=red>❌ EventSystem 없음</color>");
            return;
        }

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = mousePos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            AddLog($"<color=lime>🖱️ UI Hit: {results.Count}개</color>");

            for (int i = 0; i < Mathf.Min(5, results.Count); i++) // 최대 5개만
            {
                var result = results[i];
                Canvas canvas = result.gameObject.GetComponentInParent<Canvas>();
                int sortingOrder = canvas != null ? canvas.sortingOrder : 0;

                string componentInfo = "";
                if (result.gameObject.GetComponent<Button>() != null)
                    componentInfo = " [Button]";
                else if (result.gameObject.GetComponent<Image>() != null)
                    componentInfo = " [Image]";
                else if (result.gameObject.GetComponent<Text>() != null)
                    componentInfo = " [Text]";

                AddLog($"  {i + 1}. {result.gameObject.name}{componentInfo}");
                AddLog($"     sortingOrder: {sortingOrder}, 거리: {result.distance:F2}");
            }

            if (results.Count > 5)
            {
                AddLog($"  ... 외 {results.Count - 5}개");
            }
        }
        else
        {
            AddLog("<color=gray>✓ UI Hit 없음</color>");
        }
    }

    /// <summary>
    /// 클릭 위치의 Canvas 계층 확인
    /// </summary>
    void DebugCanvasHierarchy(Vector3 mousePos)
    {
        // 클릭 위치를 덮고 있는 Canvas들 찾기
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<Canvas> coveringCanvases = new List<Canvas>();

        foreach (var canvas in allCanvases)
        {
            if (!canvas.gameObject.activeInHierarchy) continue;

            RectTransform rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos, canvas.worldCamera))
            {
                coveringCanvases.Add(canvas);
            }
        }

        if (coveringCanvases.Count > 0)
        {
            // sortingOrder 순서로 정렬 (높은 순)
            coveringCanvases.Sort((a, b) => b.sortingOrder.CompareTo(a.sortingOrder));

            AddLog($"<color=orange>📋 덮고 있는 Canvas: {coveringCanvases.Count}개</color>");
            foreach (var canvas in coveringCanvases)
            {
                string raycasterInfo = canvas.GetComponent<GraphicRaycaster>() != null ? " [Raycaster ✓]" : " [Raycaster ✗]";
                AddLog($"  └─ {canvas.gameObject.name} (sort: {canvas.sortingOrder}){raycasterInfo}");
            }
        }
    }

    #endregion

    #region 씬 전체 분석

    /// <summary>
    /// F1 키: 씬 전체 구조 분석
    /// </summary>
    void AnalyzeScene()
    {
        ClearLogs();
        AddLog("━━━━━━━━ 씬 분석 시작 ━━━━━━━━");

        // 1. Canvas 목록
        AnalyzeCanvases();

        // 2. GraphicRaycaster 목록
        AnalyzeGraphicRaycasters();

        // 3. Physics Raycaster 확인
        AnalyzePhysicsRaycasters();

        // 4. BoxCollider 목록 (유닛)
        AnalyzeUnitColliders();

        // 5. 튜토리얼 매니저 상태
        AnalyzeTutorialState();

        AddLog("━━━━━━━━ 씬 분석 완료 ━━━━━━━━");
    }

    void AnalyzeCanvases()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var sortedCanvases = new List<Canvas>(canvases);
        sortedCanvases.Sort((a, b) => b.sortingOrder.CompareTo(a.sortingOrder)); // 높은 순

        AddLog($"<color=cyan>🖼️ Canvas 목록: {canvases.Length}개</color>");
        foreach (var canvas in sortedCanvases)
        {
            if (!canvas.gameObject.activeInHierarchy) continue;

            string renderMode = canvas.renderMode.ToString();
            string overrideSorting = canvas.overrideSorting ? "override" : "inherit";
            AddLog($"  └─ {canvas.gameObject.name}");
            AddLog($"     sort: {canvas.sortingOrder} ({overrideSorting}), mode: {renderMode}");
        }
    }

    void AnalyzeGraphicRaycasters()
    {
        GraphicRaycaster[] raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        AddLog($"<color=lime>🖱️ GraphicRaycaster: {raycasters.Length}개</color>");
        foreach (var raycaster in raycasters)
        {
            if (!raycaster.gameObject.activeInHierarchy) continue;

            Canvas canvas = raycaster.GetComponent<Canvas>();
            int sortingOrder = canvas != null ? canvas.sortingOrder : 0;
            AddLog($"  └─ {raycaster.gameObject.name} (sort: {sortingOrder})");
        }
    }

    void AnalyzePhysicsRaycasters()
    {
        var raycasters = FindObjectsByType<PhysicsRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        AddLog($"<color=yellow>🎯 PhysicsRaycaster: {raycasters.Length}개</color>");
        foreach (var raycaster in raycasters)
        {
            AddLog($"  └─ {raycaster.gameObject.name} (Camera: {raycaster.GetComponent<Camera>()?.name ?? "없음"})");
        }
    }

    void AnalyzeUnitColliders()
    {
        Unit_1001Upgrade[] units = FindObjectsByType<Unit_1001Upgrade>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int enabledCount = 0;

        foreach (var unit in units)
        {
            var collider = unit.GetComponent<BoxCollider>();
            if (collider != null && collider.enabled)
            {
                enabledCount++;
            }
        }

        AddLog($"<color=orange>🎮 유닛 Collider: {enabledCount}/{units.Length}개 활성화</color>");
    }

    void AnalyzeTutorialState()
    {
        if (TutorialManager.Instance != null)
        {
            AddLog($"<color=magenta>📚 튜토리얼: {TutorialManager.Instance.CurrentStep}</color>");
            AddLog($"  모드: {(TutorialManager.IsTutorialMode ? "활성화" : "비활성화")}");
        }
        else
        {
            AddLog("<color=gray>📚 튜토리얼: 없음</color>");
        }
    }

    #endregion

    #region 로그 관리

    void AddLog(string message)
    {
        onScreenLogs.Add(message);
        Debug.Log($"[ClickDebugger] {message}");

        // 최대 라인 수 제한
        if (onScreenLogs.Count > maxLogLines * 3) // 버퍼 3배
        {
            onScreenLogs.RemoveAt(0);
        }
    }

    void ClearLogs()
    {
        onScreenLogs.Clear();
        Debug.Log("[ClickDebugger] 로그 클리어");
    }

    #endregion
}
