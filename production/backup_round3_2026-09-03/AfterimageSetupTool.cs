using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool hệ bóng mờ lưu ảnh (afterimage):
/// - ★ SETUP: tạo Assets/_Game/Resources/AfterimageConfig.asset (enabled=true) — đây là
///   "cú click đồng ý" bật feature gate §9; chưa có asset thì runtime tắt hoàn toàn.
/// - TẮT / BẬT lại: lật cờ enabled trên asset.
/// - Kiểm tra: in config hiện tại + đếm emitter đang chạy (ngoài Play Mode chỉ in config).
/// </summary>
public static class AfterimageSetupTool
{
    private const string AssetPath = "Assets/_Game/Resources/AfterimageConfig.asset";
    private const string MenuRoot  = "Tools/Farm Game/Afterimage/";

    [MenuItem(MenuRoot + "★ SETUP hiệu ứng bóng mờ (1 nút)", false, 1)]
    public static void Setup()
    {
        AfterimageConfig cfg = AssetDatabase.LoadAssetAtPath<AfterimageConfig>(AssetPath);
        bool created = false;

        if (cfg == null)
        {
            if (!EditorUtility.DisplayDialog(
                    "Afterimage — SETUP",
                    "Tạo AfterimageConfig.asset tại:\n" + AssetPath +
                    "\n\nvới enabled = TRUE — hiệu ứng bóng mờ sẽ BẬT ngay lần Play tới cho:\n" +
                    "  • FlowerGirlShipper (cô shipper giỏ hoa)\n" +
                    "  • BuilderWorker (thợ búa — chỉ khi di chuyển, đứng đập búa không nhả)\n" +
                    "  • TouristAgent (khách du lịch từ tàu)\n\nTiếp tục?",
                    "Tạo và BẬT", "Thôi"))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources"))
                AssetDatabase.CreateFolder("Assets/_Game", "Resources");

            cfg = ScriptableObject.CreateInstance<AfterimageConfig>();
            cfg.enabled = true;
            AssetDatabase.CreateAsset(cfg, AssetPath);
            created = true;
        }
        else if (!cfg.enabled)
        {
            cfg.enabled = true;
            EditorUtility.SetDirty(cfg);
        }

        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(cfg);
        Selection.activeObject = cfg;

        string targets = cfg.targetTypeNames != null ? string.Join(", ", cfg.targetTypeNames) : "(rỗng)";
        Debug.Log("[Afterimage] SETUP xong (" + (created ? "tạo mới" : "asset đã có, đảm bảo enabled=true") +
                  "). Target: " + targets + ". Bấm Play để thấy bóng mờ khi nhân vật đi bộ.");
    }

    [MenuItem(MenuRoot + "TẮT hiệu ứng (enabled=false)", false, 20)]
    public static void Disable() { SetEnabled(false); }

    [MenuItem(MenuRoot + "BẬT lại hiệu ứng", false, 21)]
    public static void Enable() { SetEnabled(true); }

    private static void SetEnabled(bool value)
    {
        AfterimageConfig cfg = AssetDatabase.LoadAssetAtPath<AfterimageConfig>(AssetPath);
        if (cfg == null)
        {
            EditorUtility.DisplayDialog("Afterimage",
                "Chưa có " + AssetPath + ".\nChạy menu ★ SETUP trước.", "OK");
            return;
        }
        cfg.enabled = value;
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        Debug.Log("[Afterimage] enabled = " + value +
                  (value ? " — có hiệu lực từ lần Play tới." : " — hệ sẽ không khởi động ở lần Play tới."));
    }

    [MenuItem(MenuRoot + "Kiểm tra", false, 40)]
    public static void Check()
    {
        AfterimageConfig cfg = AssetDatabase.LoadAssetAtPath<AfterimageConfig>(AssetPath);
        if (cfg == null)
        {
            Debug.Log("[Afterimage] CHƯA có config asset (" + AssetPath + ") ⇒ hệ đang TẮT hoàn toàn. Chạy ★ SETUP để bật.");
            return;
        }

        string targets = cfg.targetTypeNames != null ? string.Join(", ", cfg.targetTypeNames) : "(rỗng)";
        Debug.Log("[Afterimage] Config: enabled=" + cfg.enabled +
                  " | minSpeed=" + cfg.minSpeed + " u/s | spawnInterval=" + cfg.spawnInterval +
                  "s | ghostLife=" + cfg.ghostLife + "s | startAlpha=" + cfg.startAlpha +
                  " | shrink=" + cfg.shrink + " (endScaleMul=" + cfg.endScaleMul + ")" +
                  " | poolCap=" + cfg.poolCap + " | sortingOrderOffset=" + cfg.sortingOrderOffset +
                  " | rescanInterval=" + cfg.rescanInterval + "s | target: " + targets);

        if (Application.isPlaying)
        {
            SpriteAfterimageEmitter[] emitters =
                Object.FindObjectsByType<SpriteAfterimageEmitter>(FindObjectsSortMode.None);
            SpriteAfterimage[] ghosts =
                Object.FindObjectsByType<SpriteAfterimage>(FindObjectsSortMode.None);
            Debug.Log("[Afterimage] Đang Play: " + emitters.Length + " nhân vật có emitter, " +
                      ghosts.Length + " ghost đang hiện.");
        }
        else
        {
            Debug.Log("[Afterimage] (Ngoài Play Mode — chỉ in config. Vào Play rồi bấm Kiểm tra lại để đếm emitter.)");
        }
    }
}
