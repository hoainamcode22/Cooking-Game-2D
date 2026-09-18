using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FarmPlotInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask plotMask = ~0;

    // ── [PERF F4.1 2026-09-17] KHONG CAP PHAT TRONG LUC QUET ──────────────────────
    // Ban cu: Physics2D.OverlapCircleAll(...) => Unity cap phat MOT Collider2D[] MOI
    // moi lan quet, roi GetComponent<>() + GetComponentInParent<>() qua toan tu `??`
    // (toan tu `??` tren UnityEngine.Object con lam hong phep so sanh null cua Unity).
    // Ban moi: mot List<Collider2D> dung lai + ContactFilter2D + overload khong cap phat.
    // LUU Y Unity 6: OverlapCircleNonAlloc DA BI DANH DAU OBSOLETE; overload dung la
    // Physics2D.OverlapCircle(point, radius, ContactFilter2D, List<Collider2D>).
    private readonly List<Collider2D> _hits = new List<Collider2D>(16);
    private ContactFilter2D           _filter;

    /// <summary>Ban kinh quet quanh diem bam, giu nguyen gia tri cu (25 world unit).</summary>
    private const float BAN_KINH_QUET = 25f;

    // Cache camera chính nếu chưa gán tay trong Inspector.
    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Dung ContactFilter2D thay layerMask tho: OverlapCircleAll cu dung
        // Physics2D.queriesHitTriggers, nen phai chep lai dung co do, khong doi hanh vi.
        _filter = new ContactFilter2D
        {
            useTriggers   = Physics2D.queriesHitTriggers,
            useLayerMask  = true,
            layerMask     = plotMask,
            useDepth      = false,
            useOutsideDepth = false,
            useNormalAngle  = false,
            useOutsideNormalAngle = false
        };
    }

    // Bắt click/tap ngoài world rồi forward vào PlotController tương ứng.
    private void Update()
    {
        if (!IsPointerDownThisFrame())
            return;

        if (FarmInputLock.IsCookingMode)
            return;

        // Tuyệt đối không mở Seed Popup / logic trồng trọt khi Edit Mode đang bật
        // hoặc đang kéo/đặt công trình — hai hệ thống này không được dẫm chân nhau.
        if (EditModeManager.IsEditMode || PlacementManager.IsPlacingNewObject)
        {
            return;
        }

        // Không xử lý trong khi đang kéo hạt giống hoặc liềm
        if (FarmInputLock.IsDraggingSeed || FarmInputLock.IsDraggingSickle)
        {
            return;
        }

        // Không xử lý plot khi đang có popup mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
        {
            return;
        }

        if (OrderBoardPopupUI.AnyOpen)
        {
            return;
        }

        // Nếu đang bấm UI thì không xử lý world plot.
        if (IsPointerOverUI())
        {
            return;
        }

        if (mainCamera == null || FarmManager.Instance == null)
            return;

        Vector2 screenPos = GetPointerScreenPosition();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        // Quét tất cả collider tại điểm bấm để tìm PlotController,
        // tránh bị các collider khác (nhà, nhân vật, cây cối) đè lên nuốt mất click
        PlotController plot = null;

        // [PERF F4.1] Overload khong cap phat: ket qua do vao _hits dung lai, khong sinh mang moi.
        // (_filter.layerMask duoc dong bo lai phong khi plotMask bi doi luc chay.)
        _filter.layerMask = plotMask;
        _filter.useTriggers = Physics2D.queriesHitTriggers;
        Physics2D.OverlapCircle(worldPos, BAN_KINH_QUET, _filter, _hits);

        for (int i = 0; i < _hits.Count; i++)
        {
            Collider2D c = _hits[i];
            if (c == null) continue;

            // TryGetComponent: khong sinh rac khi KHONG tim thay (khac GetComponent<>()).
            if (!c.TryGetComponent(out PlotController p))
                p = c.GetComponentInParent<PlotController>();

            if (p != null)
            {
                plot = p;
                break;
            }
        }

        if (plot == null)
        {
            return;
        }

        plot.HandlePlotClick();
    }

    // Kiểm tra frame hiện tại có vừa tap/click hay không.
    private bool IsPointerDownThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Input.GetMouseButtonDown(0))
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    // Lấy tọa độ con trỏ hiện tại theo touch hoặc mouse.
    private Vector2 GetPointerScreenPosition()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return Mouse.current.position.ReadValue();

        if (Input.GetMouseButtonDown(0))
            return Input.mousePosition;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Input.mousePosition;
    }

    // Check pointer hiện tại có đang nằm trên UI thật (GraphicRaycaster) không.
    // TUYỆT ĐỐI KHÔNG dùng EventSystem.IsPointerOverGameObject() vì Physics2DRaycaster
    // trên Main Camera bắt luôn Collider2D của chính ô đất => tự chặn mình.
    private bool IsPointerOverUI()
    {
        return FarmInputLock.ConTroTrenUiThat();
    }
}
