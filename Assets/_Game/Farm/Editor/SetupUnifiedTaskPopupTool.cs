#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dựng sẵn POPUP GỘP (Nhiệm vụ + Hằng ngày + Thành tựu) vào scene.
///
/// Tạo GameObject "UnifiedTaskPopupRoot" dưới Canvas_Popup, gắn component UnifiedTaskPopupUI,
/// và tự gán database mission/daily từ PopupEwarManager có sẵn → bạn KHÔNG phải gán tay.
///
/// Popup tự dựng toàn bộ layout + slot (khung gỗ, giấy, ribbon, 3 tab, các hàng nhiệm vụ,
/// 7 ô điểm danh, hàng thành tựu, reward slot, fill bar, button trạng thái) LÚC CHẠY GAME.
/// Việc của bạn: chọn root → gán sprite vào mục "Sprites" trong Inspector để thay hình mẫu.
///
/// 3 nút đã được redirect sẵn trong code (không cần gắn lại Inspector):
///   • Btn_GoMission ("Đi" trên bong bóng nhiệm vụ) → tab Nhiệm vụ
///   • Btn_Lich  → tab Hằng ngày
///   • Btn_Ewar  → tab Thành tựu
///
/// Menu: Tools/Farm Game/Setup Unified Task Popup
/// </summary>
public static class SetupUnifiedTaskPopupTool
{
    private const string MENU = "Tools/Farm Game/Setup Unified Task Popup";
    private const string RootName = "UnifiedTaskPopupRoot";

    [MenuItem(MENU)]
    public static void Setup()
    {
        // 1) Tìm Canvas_Popup (hoặc Canvas bất kỳ làm fallback)
        GameObject canvasGO = GameObject.Find("Canvas_Popup");
        if (canvasGO == null)
        {
            var anyCanvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            canvasGO = anyCanvas != null ? anyCanvas.gameObject : null;
        }
        if (canvasGO == null)
        {
            EditorUtility.DisplayDialog("Unified Task Popup",
                "Không tìm thấy Canvas nào trong scene.\nHãy mở SCN_Farm rồi chạy lại.", "OK");
            return;
        }

        // 2) Tìm/tạo UnifiedTaskPopupRoot
        var existing = Object.FindFirstObjectByType<UnifiedTaskPopupUI>(FindObjectsInactive.Include);
        GameObject rootGO;
        UnifiedTaskPopupUI popup;

        if (existing != null)
        {
            popup = existing;
            rootGO = existing.gameObject;
        }
        else
        {
            rootGO = new GameObject(RootName, typeof(RectTransform));
            rootGO.transform.SetParent(canvasGO.transform, false);
            popup = rootGO.AddComponent<UnifiedTaskPopupUI>();
        }

        // Full-stretch trùm màn hình
        var rt = rootGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        // 3) Auto-gán database từ PopupEwarManager nếu trống (mission + daily + achievement)
        var ewar = Object.FindFirstObjectByType<PopupEwarManager>(FindObjectsInactive.Include);
        var so = new SerializedObject(popup);
        var mdb = so.FindProperty("missionDatabase");
        var ddb = so.FindProperty("dailyMissionDatabase");
        var adb = so.FindProperty("achievementDatabase");
        if (ewar != null)
        {
            if (mdb != null && mdb.objectReferenceValue == null)
                mdb.objectReferenceValue = ewar.MissionDatabaseRef;
            if (ddb != null && ddb.objectReferenceValue == null)
                ddb.objectReferenceValue = ewar.DailyMissionDatabaseRef;
            if (adb != null && adb.objectReferenceValue == null)
                adb.objectReferenceValue = ewar.AchievementMissionDatabaseRef;
        }

        // 4) Auto-tìm & gán sprite phần thưởng (chỉ gán khi đang trống — giữ hình designer đã chỉnh).
        //    Sprite vàng/kim cương/EXP/rương/khóa có sẵn trong project → hết cảnh ô vuông □ (★ placeholder).
        int iconsWired = 0;
        iconsWired += AssignSprite(so, "sprites.coinIcon",    FindSprite("Sprite_coin_icon", "Icon_vang", "vang", "coin", "gold"));
        iconsWired += AssignSprite(so, "sprites.diamondIcon", FindSprite("kimcuong", "diamond", "gem"));
        iconsWired += AssignSprite(so, "sprites.expIcon",     FindSprite("exp"));
        iconsWired += AssignSprite(so, "sprites.chestIcon",   FindSprite("chest", "ruong", "rương", "box"));
        iconsWired += AssignSprite(so, "sprites.lockIcon",    FindSprite("lock", "okhoa", "khoa", "ổ khóa"));

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[UnifiedTaskPopup] Auto-gán {iconsWired} icon thưởng (coin/diamond/exp/chest/lock) — " +
                  "ô nào vẫn trống thì gán tay trong mục 'Sprites'.");

        EditorUtility.SetDirty(popup);
        EditorSceneManager.MarkSceneDirty(rootGO.scene);
        Selection.activeGameObject = rootGO;

        EditorUtility.DisplayDialog("Unified Task Popup",
            (existing != null ? "Đã CẬP NHẬT " : "Đã TẠO ") + RootName + " dưới " + canvasGO.name + ".\n\n" +
            (ewar != null
                ? "Database Nhiệm vụ + Hằng ngày đã tự gán từ PopupEwarManager.\n\n"
                : "(Không thấy PopupEwarManager — bạn tự gán Database trong Inspector.)\n\n") +
            "VIỆC CỦA BẠN:\n" +
            "• Chọn " + RootName + " → mục 'Sprites' → gán hình: khung gỗ, giấy, ribbon,\n" +
            "  nút đóng, nút tab + nút chọn, 3 icon tab, icon vàng/kim cương/EXP, rương, ổ khóa,\n" +
            "  7 icon phần thưởng điểm danh, mascot, lá/hoa trang trí.\n" +
            "• 3 nút Btn_GoMission / Btn_Lich / Btn_Ewar đã tự mở đúng tab (không cần gắn lại).\n" +
            "• Popup tự dựng layout + slot lúc chạy game.",
            "OK");
    }

    /// <summary>Gán sprite vào 1 property của UnifiedTaskPopupSprites NẾU đang trống. Trả 1 nếu có gán.</summary>
    private static int AssignSprite(SerializedObject so, string propPath, Sprite sprite)
    {
        if (sprite == null) return 0;
        var prop = so.FindProperty(propPath);
        if (prop == null || prop.objectReferenceValue != null) return 0;
        prop.objectReferenceValue = sprite;
        return 1;
    }

    /// <summary>Tìm Sprite đầu tiên khớp 1 trong các từ khoá (ưu tiên theo thứ tự). Hỗ trợ cả
    /// asset Sprite lẫn Texture2D có sprite con (ảnh PNG import kiểu Sprite).</summary>
    private static Sprite FindSprite(params string[] keywords)
    {
        foreach (string kw in keywords)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{kw} t:Sprite"))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sp != null) return sp;
            }
            foreach (string guid in AssetDatabase.FindAssets($"{kw} t:Texture2D"))
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (asset is Sprite sp2) return sp2;
            }
        }
        return null;
    }

    [MenuItem(MENU, true)]
    private static bool Validate() => !EditorApplication.isPlaying;
}
#endif
