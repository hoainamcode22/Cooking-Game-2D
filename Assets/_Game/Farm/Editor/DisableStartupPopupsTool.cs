using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup/Disable Startup Popups
///
/// Set inactive tất cả popup lớn (chợ/kho/shop) trong scene để chúng
/// không tự mở khi Play Mode. Icon/nút mở popup vẫn hoạt động bình thường.
///
/// Chạy nhiều lần an toàn.
/// </summary>
public static class DisableStartupPopupsTool
{
    private const string MENU = "Tools/Farm Game/Setup/Disable Startup Popups";

    // Tên popup root objects cần set inactive
    private static readonly string[] POPUP_ROOT_NAMES =
    {
        "Panel_Background",   // Market popup root
        "Frame",              // Warehouse popup root (tên chung)
    };

    // Tên object cha (canvas/parent) chứa popup — tìm theo tên này trước
    private static readonly string[] POPUP_PARENT_NAMES =
    {
        "Canvas_MarketPopup",
        "Canvas_Popup",
        "MarketPopup",
        "WarehousePopup",
        "ShopPopup",
    };

    // Component types để identify popup objects
    private static readonly System.Type[] POPUP_COMPONENT_TYPES =
    {
        typeof(MarketPopupUI),
        typeof(WarehousePopupUI),
    };

    [MenuItem(MENU)]
    public static void DisablePopups()
    {
        int disabled = 0;
        int skipped  = 0;

        Debug.Log("═══ DISABLE STARTUP POPUPS ═══");

        // Strategy 1: Tìm qua component types — chắc chắn nhất
        foreach (var type in POPUP_COMPONENT_TYPES)
        {
            var components = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Component comp in components)
            {
                var so   = new SerializedObject(comp);
                var root = so.FindProperty("popupRoot")?.objectReferenceValue as GameObject;
                if (root == null)
                {
                    Debug.Log($"  [SKIP] {comp.GetType().Name} on '{comp.gameObject.name}' — popupRoot field null");
                    skipped++;
                    continue;
                }

                if (root.activeSelf)
                {
                    root.SetActive(false);
                    EditorUtility.SetDirty(root);
                    Debug.Log($"  [SET INACTIVE] {type.Name}.popupRoot → '{root.name}' (parent: {root.transform.parent?.name})");
                    disabled++;
                }
                else
                {
                    Debug.Log($"  [OK] {type.Name}.popupRoot '{root.name}' da inactive");
                    skipped++;
                }
            }
        }

        // Strategy 2: Tìm theo tên parent canvas
        foreach (var parentName in POPUP_PARENT_NAMES)
        {
            var go = GameObject.Find(parentName);
            if (go == null) continue;

            // Không tắt Canvas cha — chỉ tắt popup content bên trong
            foreach (Transform child in go.transform)
            {
                bool isPopupContent = false;
                foreach (var rootName in POPUP_ROOT_NAMES)
                {
                    if (child.name == rootName) { isPopupContent = true; break; }
                }
                if (!isPopupContent) continue;

                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    EditorUtility.SetDirty(child.gameObject);
                    Debug.Log($"  [SET INACTIVE] '{parentName}/{child.name}'");
                    disabled++;
                }
                else
                {
                    skipped++;
                }
            }
        }

        // Lưu scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"═══ DONE: {disabled} popup(s) set inactive | {skipped} bỏ qua ═══");
        Debug.Log("Lưu scene (Ctrl+S) để lưu thay đổi!");

        EditorUtility.DisplayDialog("Disable Startup Popups",
            $"Hoan thanh!\n\n" +
            $"Popup da tat: {disabled}\n" +
            $"Bo qua (da inactive): {skipped}\n\n" +
            "Nho save scene (Ctrl+S)!\n" +
            "Icon nut mo cho/kho van hoat dong binh thuong.",
            "OK");
    }

    [MenuItem(MENU, true)]
    private static bool Validate() => !EditorApplication.isPlaying;

    // =========================================================================
    // Dùng bởi CheckTutorialL1L2SetupTool
    // =========================================================================
    public static bool IsMarketPopupActiveAtStart()
    {
        var all = Object.FindObjectsByType<MarketPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var comp in all)
        {
            var so   = new SerializedObject(comp);
            var root = so.FindProperty("popupRoot")?.objectReferenceValue as GameObject;
            if (root != null && root.activeSelf) return true;
        }
        return false;
    }

    public static bool IsWarehousePopupActiveAtStart()
    {
        var all = Object.FindObjectsByType<WarehousePopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Component comp in all)
        {
            var so   = new SerializedObject(comp);
            var root = so.FindProperty("popupRoot")?.objectReferenceValue as GameObject;
            if (root != null && root.activeSelf) return true;
        }
        return false;
    }
}
