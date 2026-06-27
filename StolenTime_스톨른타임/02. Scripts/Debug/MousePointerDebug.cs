using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MousePointerDebug : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private bool verboseRaycastList = true;
    [SerializeField] private Vector2 overlayOffset = new Vector2(16f, 16f);

    [Header("Raycast")]
    [SerializeField] private LayerMask world3DMask = ~0;
    [SerializeField] private LayerMask world2DMask = ~0;
    [SerializeField] private float world3DDistance = 1000f;

    private readonly List<RaycastResult> uiResults = new();
    private readonly List<GraphicRaycaster> graphicRaycasters = new();
    private readonly StringBuilder builder = new();
    private PointerEventData pointerEventData;
    private GUIStyle labelStyle;
    private EventSystem cachedEventSystem;

    private Camera cachedCamera;
    private string overlayText = string.Empty;

    private void Awake()
    {
        cachedCamera = Camera.main;
    }

    private void Update()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;

        CollectPointerInfo();

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            Debug.Log($"[MousePointerDebug] Click {GetMouseButtonName()} \n{overlayText}");
        }
    }

    private void CollectPointerInfo()
    {
        builder.Clear();

        Vector3 mouse = Input.mousePosition;
        builder.AppendLine($"Mouse Screen: {mouse}");

        if (cachedCamera != null)
        {
            Vector3 world = cachedCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cachedCamera.transform.position.z));
            builder.AppendLine($"Mouse World : {world}");
        }
        else
        {
            builder.AppendLine("Mouse World : <no main camera>");
        }

        AppendUIInfo(mouse);
        AppendWorld2DInfo(mouse);
        AppendWorld3DInfo(mouse);

        overlayText = builder.ToString();
    }

    private void AppendUIInfo(Vector3 mouse)
    {
        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        builder.AppendLine($"UI Pointer  : {pointerOverUi}");

        uiResults.Clear();

        if (EventSystem.current == null)
        {
            builder.AppendLine("UI Target   : <no EventSystem>");
            return;
        }

        if (pointerEventData == null || cachedEventSystem != EventSystem.current)
        {
            cachedEventSystem = EventSystem.current;
            pointerEventData = new PointerEventData(cachedEventSystem);
        }

        pointerEventData.position = mouse;

        graphicRaycasters.Clear();
        graphicRaycasters.AddRange(FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None));
        foreach (var raycaster in graphicRaycasters)
        {
            if (raycaster == null || !raycaster.isActiveAndEnabled) continue;
            raycaster.Raycast(pointerEventData, uiResults);
        }

        if (uiResults.Count == 0)
        {
            builder.AppendLine("UI Target   : <none>");
            return;
        }

        RaycastResult top = uiResults[0];
        builder.AppendLine($"UI Target   : {GetObjectPath(top.gameObject)}");

        if (verboseRaycastList)
        {
            int count = Mathf.Min(uiResults.Count, 8);
            for (int i = 0; i < count; i++)
            {
                builder.AppendLine($"UI Hit {i + 1}    : {GetObjectPath(uiResults[i].gameObject)}");
            }
        }
    }

    private void AppendWorld2DInfo(Vector3 mouse)
    {
        if (cachedCamera == null)
        {
            builder.AppendLine("World2D Hit : <no main camera>");
            return;
        }

        Vector2 worldPoint = cachedCamera.ScreenToWorldPoint(mouse);
        Collider2D hit2D = Physics2D.OverlapPoint(worldPoint, world2DMask);
        if (hit2D == null)
        {
            builder.AppendLine("World2D Hit : <none>");
            return;
        }

        builder.AppendLine($"World2D Hit : {GetObjectPath(hit2D.gameObject)}");

    }

    private void AppendWorld3DInfo(Vector3 mouse)
    {
        if (cachedCamera == null)
        {
            builder.AppendLine("World3D Hit : <no main camera>");
            return;
        }

        Ray ray = cachedCamera.ScreenPointToRay(mouse);
        if (!Physics.Raycast(ray, out RaycastHit hit3D, world3DDistance, world3DMask))
        {
            builder.AppendLine("World3D Hit : <none>");
            return;
        }

        builder.AppendLine($"World3D Hit : {GetObjectPath(hit3D.collider.gameObject)}");
    }

    private void OnGUI()
    {
        if (!showOverlay) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                richText = false,
                wordWrap = false
            };
            labelStyle.normal.textColor = Color.white;
        }

        Vector2 size = labelStyle.CalcSize(new GUIContent(overlayText));
        Rect rect = new Rect(
            overlayOffset.x,
            overlayOffset.y,
            Mathf.Min(size.x + 20f, Screen.width - overlayOffset.x * 2f),
            Mathf.Min(size.y + 20f, Screen.height - overlayOffset.y * 2f));

        GUI.Box(rect, overlayText, labelStyle);
    }

    private static string GetMouseButtonName()
    {
        if (Input.GetMouseButtonDown(0)) return "Left";
        if (Input.GetMouseButtonDown(1)) return "Right";
        if (Input.GetMouseButtonDown(2)) return "Middle";
        return "Unknown";
    }

    private static string GetObjectPath(GameObject target)
    {
        if (target == null) return "<null>";

        Transform current = target.transform;
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = $"{current.name}/{path}";
        }

        return path;
    }
}
