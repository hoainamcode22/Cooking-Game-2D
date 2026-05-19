#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools > Farm Tools > Setup Warehouse ScrollView
///
/// Tái cấu trúc UI Hierarchy của Warehouse Popup thành:
///   Frame
///     └── Scroll_View_Warehouse  [ScrollRect]
///           └── Viewport         [RectMask2D]
///                 └── ItemGrid   [GridLayoutGroup + ContentSizeFitter]  ← giữ nguyên
///
/// TUYỆT ĐỐI không sửa logic code WarehousePopupUI.
/// Reference itemGridContainer trong Inspector vẫn hợp lệ sau khi reparent.
/// </summary>
public static class SetupWarehouseScrollViewTool
{
    [MenuItem("Tools/Farm Tools/Setup Warehouse ScrollView")]
    public static void Run()
    {
        // ── 1. Tìm WarehousePopupUI trong scene ────────────────────────────
#if UNITY_2023_1_OR_NEWER
        var warehouseUI = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
#else
        var warehouseUI = Object.FindObjectOfType<WarehousePopupUI>(true);
#endif
        if (warehouseUI == null)
        {
            EditorUtility.DisplayDialog("Setup Warehouse ScrollView",
                "Không tìm thấy WarehousePopupUI trong scene.\n" +
                "Mở scene chứa Warehouse Popup và thử lại.", "OK");
            return;
        }

        // ── 2. Tìm ItemGrid qua serialized field itemGridContainer ──────────
        SerializedObject soUI = new SerializedObject(warehouseUI);
        Transform itemGrid = soUI.FindProperty("itemGridContainer")?.objectReferenceValue as Transform;

        // Fallback: tìm theo tên trong hierarchy
        if (itemGrid == null)
            itemGrid = FindChildRecursive(warehouseUI.transform, "ItemGrid");

        if (itemGrid == null)
        {
            EditorUtility.DisplayDialog("Setup Warehouse ScrollView",
                "Không tìm thấy ItemGrid.\n" +
                "Đảm bảo field 'itemGridContainer' trong WarehousePopupUI đã được gán hoặc\n" +
                "có GameObject tên 'ItemGrid' trong hierarchy.", "OK");
            return;
        }

        // ── 3. Xác định Frame (parent hiện tại của ItemGrid) ────────────────
        // Nếu ItemGrid đang nằm trong scroll cũ → lấy parent của scroll làm Frame
        Transform frame = itemGrid.parent;
        if (frame != null && frame.name == "Viewport")
            frame = frame.parent?.parent; // ItemGrid → Viewport → Scroll → Frame

        if (frame == null)
        {
            EditorUtility.DisplayDialog("Setup Warehouse ScrollView",
                "Không xác định được Frame parent của ItemGrid.\nKiểm tra hierarchy.", "OK");
            return;
        }

        // ── 4. Xử lý trường hợp Scroll_View_Warehouse đã tồn tại ───────────
        Transform existingScroll = frame.Find("Scroll_View_Warehouse");
        if (existingScroll != null)
        {
            bool rebuild = EditorUtility.DisplayDialog("Setup Warehouse ScrollView",
                $"'Scroll_View_Warehouse' đã tồn tại trong '{frame.name}'.\n" +
                "Bạn có muốn xóa và rebuild lại không?", "Rebuild", "Hủy");
            if (!rebuild) return;

            // Trả ItemGrid về Frame trước khi xóa scroll cũ
            if (itemGrid.IsChildOf(existingScroll))
            {
                Undo.SetTransformParent(itemGrid, frame, "Restore ItemGrid before rebuild");
                itemGrid.SetAsLastSibling();
            }
            Undo.DestroyObjectImmediate(existingScroll.gameObject);
        }

        // ── 5. Tạo Scroll_View_Warehouse ────────────────────────────────────
        GameObject scrollGO = new GameObject("Scroll_View_Warehouse");
        Undo.RegisterCreatedObjectUndo(scrollGO, "Setup Warehouse ScrollView");
        scrollGO.transform.SetParent(frame, false);
        scrollGO.layer = frame.gameObject.layer;

        RectTransform scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin        = Vector2.zero;
        scrollRT.anchorMax        = Vector2.one;
        scrollRT.offsetMin        = new Vector2(10f, 10f);   // inset 10px từ Frame
        scrollRT.offsetMax        = new Vector2(-10f, -10f);

        // Image trong suốt để ScrollRect nhận input chuột/touch
        Image scrollBg        = scrollGO.AddComponent<Image>();
        scrollBg.color        = Color.clear;
        scrollBg.raycastTarget = true;

        // ── 6. Tạo Viewport (RectMask2D cắt item tràn) ──────────────────────
        GameObject viewportGO = new GameObject("Viewport");
        Undo.RegisterCreatedObjectUndo(viewportGO, "Setup Warehouse ScrollView");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        viewportGO.layer = frame.gameObject.layer;

        RectTransform viewportRT = viewportGO.AddComponent<RectTransform>();
        viewportRT.anchorMin     = Vector2.zero;
        viewportRT.anchorMax     = Vector2.one;
        viewportRT.offsetMin     = Vector2.zero;
        viewportRT.offsetMax     = Vector2.zero;

        viewportGO.AddComponent<RectMask2D>();

        // ── 7. Đưa ItemGrid vào Viewport ────────────────────────────────────
        Undo.SetTransformParent(itemGrid, viewportGO.transform, "Reparent ItemGrid → Viewport");

        RectTransform itemGridRT = itemGrid.GetComponent<RectTransform>();
        if (itemGridRT == null)
            itemGridRT = itemGrid.gameObject.AddComponent<RectTransform>();

        // Content của ScrollRect: pivot + anchor góc trên-trái
        itemGridRT.anchorMin        = new Vector2(0f, 1f);
        itemGridRT.anchorMax        = new Vector2(0f, 1f);
        itemGridRT.pivot            = new Vector2(0f, 1f);
        itemGridRT.anchoredPosition = Vector2.zero;

        // ── 8. Cấu hình ScrollRect ──────────────────────────────────────────
        ScrollRect scrollRect        = scrollGO.AddComponent<ScrollRect>();
        scrollRect.content           = itemGridRT;
        scrollRect.viewport          = viewportRT;
        scrollRect.horizontal        = true;
        scrollRect.vertical          = true;
        scrollRect.movementType      = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar   = null;

        // ── 9. Fix GridLayoutGroup padding ──────────────────────────────────
        GridLayoutGroup grid = itemGrid.GetComponent<GridLayoutGroup>();
        bool gridFixed = false;
        if (grid != null)
        {
            Undo.RecordObject(grid, "Fix GridLayoutGroup Padding");
            grid.padding = new RectOffset(20, 20, 20, 20);
            EditorUtility.SetDirty(grid);
            gridFixed = true;
        }

        // ── 10. Fix ContentSizeFitter ───────────────────────────────────────
        ContentSizeFitter fitter = itemGrid.GetComponent<ContentSizeFitter>();
        bool fitterAdded = false;
        if (fitter == null)
        {
            fitter = itemGrid.gameObject.AddComponent<ContentSizeFitter>();
            fitterAdded = true;
        }
        else
        {
            Undo.RecordObject(fitter, "Fix ContentSizeFitter");
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        EditorUtility.SetDirty(fitter);

        // ── 11. Lưu scene ───────────────────────────────────────────────────
        EditorUtility.SetDirty(warehouseUI);
        EditorUtility.SetDirty(scrollGO);
        EditorSceneManager.MarkSceneDirty(warehouseUI.gameObject.scene);

        Selection.activeGameObject = scrollGO;

        // ── Báo kết quả ─────────────────────────────────────────────────────
        string gridMsg   = gridFixed  ? "✓ GridLayoutGroup padding → Left/Top/Right/Bottom = 20px"
                                      : "⚠ GridLayoutGroup không tìm thấy trên ItemGrid";
        string fitterMsg = fitter != null
            ? (fitterAdded ? "✓ ContentSizeFitter mới tạo → PreferredSize cả 2 trục"
                           : "✓ ContentSizeFitter → PreferredSize cả 2 trục")
            : "⚠ ContentSizeFitter lỗi";

        EditorUtility.DisplayDialog("Setup Warehouse ScrollView — Hoàn tất",
            $"✓ Scroll_View_Warehouse (ScrollRect) tạo trong '{frame.name}'.\n" +
            $"✓ Viewport (RectMask2D) bao ItemGrid.\n" +
            $"✓ ItemGrid reparented: Frame → Scroll → Viewport → ItemGrid.\n" +
            $"{gridMsg}\n{fitterMsg}\n\n" +
            "Lưu ý:\n" +
            "  • Offset của Scroll_View_Warehouse đang là 10px — điều chỉnh qua Inspector\n" +
            "    để khớp với viền bảng gỗ.\n" +
            "  • Reference 'itemGridContainer' trong WarehousePopupUI vẫn nguyên vẹn.\n" +
            "  • Ctrl+S để lưu scene.", "OK");
    }

    // ── Helper: tìm đệ quy theo tên ─────────────────────────────────────────
    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
                return child;

            Transform found = FindChildRecursive(child, targetName);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
