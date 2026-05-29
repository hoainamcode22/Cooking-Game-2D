using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PenClickDetector : MonoBehaviour
{
    [SerializeField] private PenMiniPanelUI miniPanel;
    [SerializeField] private Camera         mainCamera;
    [SerializeField] private Collider2D     targetCollider;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = FindCameraFromSiblings() ?? Camera.main;

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        Debug.Log("[PenClickDetector] mainCamera=" + (mainCamera != null
            ? mainCamera.name + " pos=" + mainCamera.transform.position + " orthoSize=" + mainCamera.orthographicSize
            : "NULL") + " | Camera.main=" + (Camera.main != null ? Camera.main.name : "NULL"));
    }

    // Lấy camera từ sibling component (CowPenClickOpen/PigPenClickOpen…) đã được gán sẵn trong Inspector
    private Camera FindCameraFromSiblings()
    {
        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (mb == this) continue;
            var field = mb.GetType().GetField("mainCamera",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) continue;
            var cam = field.GetValue(mb) as Camera;
            if (cam != null)
            {
                Debug.Log("[PenClickDetector] Borrowed camera '" + cam.name + "' from " + mb.GetType().Name);
                return cam;
            }
        }
        return null;
    }

    private void Start()
    {
        if (miniPanel == null)
            Debug.LogError("[PenClickDetector] miniPanel chưa được gán!");
    }

    private void Update()
    {
        // Log mỗi giây — nếu bạn thấy wasPressedThisFrame=True khi click → input OK
        if (Time.frameCount % 60 == 0)
            Debug.Log("[PenClickDetector] ALIVE on " + gameObject.name
                + " | Mouse.current=" + Mouse.current
                + " | wasPressedThisFrame=" + (Mouse.current != null ? Mouse.current.leftButton.wasPressedThisFrame.ToString() : "N/A")
                + " | OldAPI=" + Input.GetMouseButtonDown(0));

        if (!TryGetPointerScreenPos(out Vector2 screenPos)) return;
        Debug.Log("[PenClickDetector] Click detected, screenPos=" + screenPos);
        TryOpenPanel(screenPos);
    }

    private static bool TryGetPointerScreenPos(out Vector2 screenPos)
    {
        screenPos = default;

        bool newApi = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool oldApi = Input.GetMouseButtonDown(0);

        // Diagnostic: log khi BẤT KỲ api nào thấy click
        if (newApi || oldApi)
            Debug.Log("[PenClickDetector] InputCheck — NewAPI=" + newApi + " OldAPI=" + oldApi);

        if (newApi)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        if (oldApi)
        {
            screenPos = Input.mousePosition;
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        return false;
    }

    private void TryOpenPanel(Vector2 screenPos)
    {
        if (EditModeManager.IsEditMode)
        {
            Debug.Log("[PenClickDetector] BLOCKED: EditMode is active");
            return;
        }

        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
        {
            Debug.Log("[PenClickDetector] BLOCKED: popup đang mở");
            return;
        }

        if (FarmInputLock.BlockMapPan)
        {
            Debug.Log("[PenClickDetector] BLOCKED: FarmInputLock.BlockMapPan");
            return;
        }

        if (mainCamera == null || targetCollider == null || miniPanel == null)
        {
            Debug.Log("[PenClickDetector] BLOCKED: null ref — camera=" + mainCamera + " collider=" + targetCollider + " miniPanel=" + miniPanel);
            return;
        }

        Vector3 worldPt = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2  = new Vector2(worldPt.x, worldPt.y);
        bool hit = targetCollider.OverlapPoint(world2);
        Debug.Log("[PenClickDetector] cam=" + mainCamera.name
            + " camPos=" + mainCamera.transform.position
            + " | screen=" + screenPos
            + " → world=" + world2
            + " | colliderCenter=" + targetCollider.bounds.center
            + " colliderSize=" + targetCollider.bounds.size
            + " | Hit=" + hit + " on " + gameObject.name);

        if (!hit) return;

        if (miniPanel.IsPanelOpen())
        {
            miniPanel.ClosePanel();
            return;
        }

        miniPanel.OpenPanel();
    }

    private static bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData data = new PointerEventData(EventSystem.current) { position = screenPos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);

        foreach (RaycastResult r in results)
        {
            Canvas c = r.gameObject.GetComponentInParent<Canvas>();
            if (c == null) continue;

            // Canvas_Popup KHÔNG check ở đây — PopupManager.IsAnyPopupOpen() đã chặn trước đó.
            // Một element luôn-active trong Canvas_Popup đang phủ toàn màn hình và block mọi click.
            // Chỉ block khi click trúng UI của mini panel thức ăn đang mở (tránh kéo food slot làm toggle chuồng).
            if (c.name == "PF_PenMiniPanel")
                return true;
        }
        return false;
    }
}
