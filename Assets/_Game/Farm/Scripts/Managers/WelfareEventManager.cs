using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WelfareEventManager : MonoBehaviour
{
    public static WelfareEventManager Instance { get; private set; }

/*  */    [SerializeField] private RectTransform popupRect;

    public bool IsOpen => popupRect != null && popupRect.gameObject.activeSelf;
    private bool popupInputLockHeld;
    private bool _startOpen;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _startOpen = popupRect != null && popupRect.gameObject.activeSelf;
    }

    private void Start()
    {
        if (!_startOpen && popupRect != null) popupRect.gameObject.SetActive(false);
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

    public void OpenPopup()
    {
        if (popupRect != null)
        {
            popupRect.gameObject.SetActive(true);
            AcquirePopupInputBlock();
        }
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();

        if (popupRect != null)
            popupRect.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        GameObject root = popupRect != null ? popupRect.gameObject : null;
        FarmInputLock.SetPopupRaycastBlock(root, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        GameObject root = popupRect != null ? popupRect.gameObject : null;
        FarmInputLock.SetPopupRaycastBlock(root, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }

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
