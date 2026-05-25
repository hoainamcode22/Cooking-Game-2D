using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttendanceManager : MonoBehaviour
{
    public static AttendanceManager Instance { get; private set; }

    [SerializeField] private RectTransform popupRect;

    public bool IsOpen => popupRect != null && popupRect.gameObject.activeSelf;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (popupRect != null) popupRect.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // Nếu click KHÔNG trúng UI nào trong Canvas_Popup → đóng popup
        if (!IsPointerOverPopupUI(Input.mousePosition))
            ClosePopup();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void OpenPopup()  => popupRect?.gameObject.SetActive(true);
    public void ClosePopup() => popupRect?.gameObject.SetActive(false);

    // ── UI Raycast (y hệt PigPenClickOpen) ───────────────────────────────────

    private bool IsPointerOverPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            Transform t = results[i].gameObject.transform;
            Canvas parentCanvas = t.GetComponentInParent<Canvas>();

            if (parentCanvas != null && parentCanvas.name == "Canvas_Popup")
                return true;
        }

        return false;
    }
}
