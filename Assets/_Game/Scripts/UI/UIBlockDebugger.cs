using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIBlockDebugger : MonoBehaviour
{
    [SerializeField] private bool logOnlyTopHit;

    private readonly List<RaycastResult> results = new List<RaycastResult>(32);

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current == null)
        {
            Debug.LogWarning("[UIBlockDebugger] No EventSystem.current found.");
            return;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        results.Clear();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0)
        {
            Debug.Log("[UIBlockDebugger] Click hit no UI.");
            return;
        }

        if (logOnlyTopHit)
        {
            Debug.Log("[UIBlockDebugger] Top UI hit: " + BuildHitLine(results[0]));
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[UIBlockDebugger] UI hits under mouse: {results.Count}");

        for (int i = 0; i < results.Count; i++)
            sb.AppendLine($"{i + 1}. {BuildHitLine(results[i])}");

        Debug.Log(sb.ToString());
    }

    private static string BuildHitLine(RaycastResult hit)
    {
        GameObject go = hit.gameObject;
        string path = go != null ? GetHierarchyPath(go.transform) : "<null>";
        string module = hit.module != null ? hit.module.GetType().Name : "no module";
        return $"{path} | module={module} | sortingLayer={hit.sortingLayer} | sortingOrder={hit.sortingOrder} | depth={hit.depth}";
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return "<null>";

        StringBuilder sb = new StringBuilder(transform.name);
        Transform parent = transform.parent;

        while (parent != null)
        {
            sb.Insert(0, parent.name + "/");
            parent = parent.parent;
        }

        return sb.ToString();
    }
}
