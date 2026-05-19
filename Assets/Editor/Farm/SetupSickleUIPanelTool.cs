#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools > Farm Tools > Setup Sickle UI Panel
///
/// Tự động tạo Hierarchy:
///   [Target Canvas]
///     └── Sickle_Bottom_Tray   (Panel đen mờ, inactive by default)
///           ├── BG_Image        (Image nền bo góc — gán sprite 9-slice qua Inspector)
///           └── Sickle_Icon     (Image + SickleTrayIcon — điểm nhấn/kéo liềm)
///
/// Sau khi tool chạy xong:
///   1. Gán sprite bo góc cho BG_Image nếu muốn
///   2. Gán sprite liềm cho Sickle_Icon nếu chưa tự tìm được
///   3. Ctrl+S lưu scene
/// </summary>
public static class SetupSickleUIPanelTool
{
    [MenuItem("Tools/Farm Tools/Setup Sickle UI Panel")]
    public static void Run()
    {
        // ── 1. Tìm FarmUIManager ────────────────────────────────────────────
#if UNITY_2023_1_OR_NEWER
        var farmUI = Object.FindFirstObjectByType<FarmUIManager>();
#else
        var farmUI = Object.FindObjectOfType<FarmUIManager>();
#endif
        if (farmUI == null)
        {
            EditorUtility.DisplayDialog("Setup Sickle UI Panel",
                "Không tìm thấy FarmUIManager trong scene.\n" +
                "Hãy mở scene SCN_Farm và thử lại.", "OK");
            return;
        }

        // ── 2. Tìm Canvas phù hợp ──────────────────────────────────────────
        Canvas targetCanvas = FindBestCanvas(farmUI);
        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Setup Sickle UI Panel",
                "Không tìm thấy Canvas trong scene.\n" +
                "Đảm bảo scene có ít nhất một Canvas.", "OK");
            return;
        }

        // ── 3. Kiểm tra tray đã tồn tại chưa ──────────────────────────────
        Transform existing = targetCanvas.transform.Find("Sickle_Bottom_Tray");
        if (existing != null)
        {
            bool recreate = EditorUtility.DisplayDialog("Setup Sickle UI Panel",
                $"'Sickle_Bottom_Tray' đã tồn tại trong '{targetCanvas.name}'.\n" +
                "Bạn có muốn xóa và tạo lại không?", "Tạo lại", "Hủy");
            if (!recreate) return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // ── 4. Tìm SickleController hiện có để lấy sprite ──────────────────
#if UNITY_2023_1_OR_NEWER
        SickleController existingSickle = Object.FindFirstObjectByType<SickleController>(
            FindObjectsInactive.Include);
#else
        SickleController existingSickle = Object.FindObjectOfType<SickleController>(true);
#endif

        // ── 5. Tạo Sickle_Bottom_Tray ──────────────────────────────────────
        GameObject trayGO = CreateTrayPanel(targetCanvas.transform);
        Undo.RegisterCreatedObjectUndo(trayGO, "Create Sickle_Bottom_Tray");

        // ── 6. Tạo BG_Image (nền đen mờ) ───────────────────────────────────
        CreateBackground(trayGO.transform);

        // ── 7. Tạo Sickle_Icon (điểm kéo liềm) ─────────────────────────────
        bool spriteFound = CreateSickleIcon(trayGO.transform, existingSickle);

        // ── 8. Wire FarmUIManager.sickleBottomTray ─────────────────────────
        bool wired = false;
        SerializedObject so   = new SerializedObject(farmUI);
        SerializedProperty sp = so.FindProperty("sickleBottomTray");
        if (sp != null)
        {
            sp.objectReferenceValue = trayGO;
            so.ApplyModifiedProperties();
            wired = true;
        }

        // ── 9. Đánh dấu dirty và lưu ───────────────────────────────────────
        EditorUtility.SetDirty(farmUI);
        EditorSceneManager.MarkSceneDirty(farmUI.gameObject.scene);

        // ── 10. Chọn tray trong Hierarchy để dễ inspect ─────────────────────
        Selection.activeGameObject = trayGO;

        // ── Báo kết quả ────────────────────────────────────────────────────
        string warnings = "";
        if (!wired)
            warnings += "\n⚠ Không tìm thấy field 'sickleBottomTray' trong FarmUIManager.\n" +
                        "  Gán thủ công trong Inspector sau khi recompile.";
        if (!spriteFound)
            warnings += "\n⚠ Không tìm thấy sprite liềm — gán thủ công cho Sickle_Icon.Image.";

        EditorUtility.DisplayDialog("Setup Sickle UI Panel — Hoàn tất",
            $"✓ Đã tạo 'Sickle_Bottom_Tray' trong Canvas '{targetCanvas.name}'.\n" +
            $"✓ Đã wire FarmUIManager.sickleBottomTray: {(wired ? "OK" : "FAIL")}.\n\n" +
            "Các bước tiếp theo:\n" +
            "  • Gán sprite 9-slice bo góc vào BG_Image (Inspector)\n" +
            "  • Điều chỉnh vị trí / kích thước tray theo ý muốn\n" +
            "  • Ctrl+S lưu scene\n" +
            $"{warnings}", "OK");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Canvas FindBestCanvas(FarmUIManager farmUI)
    {
        // Ưu tiên 1: canvasPopupRoot hoặc canvasHudRoot của FarmUIManager → đi lên tìm Canvas root
        SerializedObject so = new SerializedObject(farmUI);

        Canvas c = TryGetCanvasFromProp(so, "canvasPopupRoot");
        if (c != null) return c;

        c = TryGetCanvasFromProp(so, "canvasHudRoot");
        if (c != null) return c;

        // Ưu tiên 2: Canvas Screen Space Overlay gốc trong scene
#if UNITY_2023_1_OR_NEWER
        Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
        Canvas[] all = Object.FindObjectsOfType<Canvas>();
#endif
        foreach (Canvas cv in all)
        {
            if (!cv.gameObject.activeInHierarchy) continue;
            if (cv.isRootCanvas && cv.renderMode == RenderMode.ScreenSpaceOverlay)
                return cv;
        }

        // Fallback: bất kỳ Canvas đang active
        foreach (Canvas cv in all)
            if (cv.gameObject.activeInHierarchy) return cv;

        return null;
    }

    private static Canvas TryGetCanvasFromProp(SerializedObject so, string propName)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null) return null;
        GameObject go = prop.objectReferenceValue as GameObject;
        if (go == null) return null;
        return go.GetComponentInParent<Canvas>(true) ?? go.GetComponent<Canvas>();
    }

    private static GameObject CreateTrayPanel(Transform canvasTransform)
    {
        GameObject tray = new GameObject("Sickle_Bottom_Tray");
        tray.transform.SetParent(canvasTransform, false);
        tray.layer = canvasTransform.gameObject.layer;

        RectTransform rt     = tray.AddComponent<RectTransform>();
        rt.anchorMin         = new Vector2(0.5f, 0f);
        rt.anchorMax         = new Vector2(0.5f, 0f);
        rt.pivot             = new Vector2(0.5f, 0f);
        rt.anchoredPosition  = new Vector2(0f, 20f);   // 20 px từ đáy màn hình
        rt.sizeDelta         = new Vector2(220f, 130f); // rộng 220, cao 130

        tray.SetActive(false); // Ẩn mặc định — FarmManager bật khi cần
        return tray;
    }

    private static void CreateBackground(Transform trayRoot)
    {
        GameObject bg = new GameObject("BG_Image");
        bg.transform.SetParent(trayRoot, false);
        bg.layer = trayRoot.gameObject.layer;

        RectTransform rt = bg.AddComponent<RectTransform>();
        rt.anchorMin     = Vector2.zero;
        rt.anchorMax     = Vector2.one;
        rt.offsetMin     = Vector2.zero;
        rt.offsetMax     = Vector2.zero;

        Image img         = bg.AddComponent<Image>();
        img.color         = new Color(0f, 0f, 0f, 150f / 255f); // đen mờ
        img.raycastTarget = true;
        // ← Gán sprite 9-slice bo góc vào đây qua Inspector để có rounded corners
    }

    private static bool CreateSickleIcon(Transform trayRoot, SickleController sickle)
    {
        GameObject icon = new GameObject("Sickle_Icon");
        icon.transform.SetParent(trayRoot, false);
        icon.layer = trayRoot.gameObject.layer;

        RectTransform rt     = icon.AddComponent<RectTransform>();
        rt.anchorMin         = new Vector2(0.5f, 0.5f);
        rt.anchorMax         = new Vector2(0.5f, 0.5f);
        rt.pivot             = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition  = Vector2.zero;
        rt.sizeDelta         = new Vector2(90f, 90f);

        Image img         = icon.AddComponent<Image>();
        img.raycastTarget = true;

        // Cố tìm sprite liềm từ SickleController hiện có
        bool found = false;
        if (sickle != null)
        {
            // Thử SpriteRenderer (world-space sprite)
            SpriteRenderer sr = sickle.GetComponent<SpriteRenderer>()
                             ?? sickle.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null)
            {
                img.sprite = sr.sprite;
                found = true;
            }

            // Thử Image component (nếu sickle đã là UI element)
            if (!found)
            {
                Image si = sickle.GetComponent<Image>()
                        ?? sickle.GetComponentInChildren<Image>(true);
                if (si != null && si.sprite != null)
                {
                    img.sprite = si.sprite;
                    found = true;
                }
            }
        }

        icon.AddComponent<SickleTrayIcon>();
        return found;
    }
}
#endif
