using UnityEngine;
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
                return cam;
            }
        }
        return null;
    }

    private void Start()
    {
    }

    private void Update()
    {
        if (!TryGetPointerScreenPos(out Vector2 screenPos)) return;
        TryOpenPanel(screenPos);
    }

    private static bool TryGetPointerScreenPos(out Vector2 screenPos)
    {
        screenPos = default;

        bool newApi = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool oldApi = Input.GetMouseButtonDown(0);

        // Diagnostic: log khi BẤT KỲ api nào thấy click
        if (newApi || oldApi)

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
            return;
        }

        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
        {
            return;
        }

        if (FarmInputLock.BlockMapPan)
        {
            return;
        }

        if (mainCamera == null || targetCollider == null || miniPanel == null)
        {
            return;
        }

        Vector3 worldPt = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2  = new Vector2(worldPt.x, worldPt.y);
        bool hit = targetCollider.OverlapPoint(world2);

        if (!hit) return;

        if (miniPanel.IsPanelOpen())
        {
            miniPanel.ClosePanel();
            return;
        }

        miniPanel.OpenPanel();
    }
}
