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

        // [FIX 2026-09-06 vong3] Cho nay truoc kia la "if (newApi || oldApi)" KHONG CO THAN
        // (Debug.Log bi script don log xoa mat), nen no nuot luon cau "if (newApi)" ngay duoi
        // lam than cua no. Ket qua chay tinh co van dung, nhung day la cai bay: them bat ky
        // dong nao vao giua se doi logic. Da bo han cai if rong do.
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
        if (mainCamera == null || targetCollider == null || miniPanel == null)
        {
            return;
        }

        // [FIX 2026-09-06 vong3] Kiem TRUNG CHUONG truoc, roi moi kiem cong khoa input.
        // OverlapPoint khong co tac dung phu nen doi cho la an toan; doi de chi ghi log khi
        // nguoi choi THUC SU bam trung chuong nay (khong spam Console moi cu click man hinh).
        Vector3 worldPt = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2  = new Vector2(worldPt.x, worldPt.y);
        bool hit = targetCollider.OverlapPoint(world2);

        if (!hit) return;

        if (FarmInputLock.BlockWorldInteraction)
        {
            Debug.Log("[PenClick] '" + name + "': trung chuong nhung BI CHAN. cooking=" + FarmInputLock.IsCookingMode + " popupLock=" + FarmInputLock.IsPopupOpen + " keoHat=" + FarmInputLock.IsDraggingSeed + " keoLiem=" + FarmInputLock.IsDraggingSickle + " seedPopup=" + FarmInputLock.IsSeedPopupOpen + " market=" + FarmInputLock.IsMarketPopupOpen + " editMode=" + EditModeManager.IsEditMode + " conTroTrenUI=" + FarmInputLock.ConTroTrenUiThat());
            return;
        }

        Debug.Log("[PenClick] '" + name + "': TRUNG chuong. state=" + miniPanel.CurrentState + " panelDangMo=" + miniPanel.IsPanelOpen());

        if (miniPanel.CurrentState == PenMiniPanelUI.PenState.Processing)
        {
            var popup = PenProcessPopupUI.Instance ?? FindFirstObjectByType<PenProcessPopupUI>(FindObjectsInactive.Include);
            if (popup == null)
            {
                var go = new GameObject("PenProcessPopupUI_Host", typeof(PenProcessPopupUI));
                popup = go.GetComponent<PenProcessPopupUI>();
            }
            if (popup != null)
            {
                popup.Open(miniPanel);
                return;
            }
        }

        if (miniPanel.IsPanelOpen())
        {
            Debug.Log("[PenClick] '" + name + "': panel CUA CHINH chuong nay dang mo => dong lai (toggle).");
            miniPanel.ClosePanel();
            return;
        }

        Debug.Log("[PenClick] '" + name + "': goi OpenPanel().");
        miniPanel.OpenPanel();
    }
}
