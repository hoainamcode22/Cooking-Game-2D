using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor tool hệ bóng mờ lưu ảnh (afterimage):
/// - ★ SETUP: tạo Assets/_Game/Resources/AfterimageConfig.asset (enabled=true) — "cú click
///   đồng ý" bật feature gate §9. IDEMPOTENT: asset đã có thì chỉ THÊM entry còn thiếu
///   vào targetEntries, KHÔNG ghi đè chỉnh tay của Sếp.
/// - Gắn tag cho NPC cảnh: AddComponent AfterimageTag cho từng con có SpriteRenderer
///   của object NPC_Villagers trong scene đang mở (bà lão hàng rông/quân nhân/nhân viên tàu).
/// - TẮT / BẬT lại: lật cờ enabled trên asset.
/// - Kiểm tra: in config + đếm emitter/pulse đang chạy (ngoài Play Mode chỉ in config).
/// </summary>
public static class AfterimageSetupTool
{
    private const string AssetPath = "Assets/_Game/Resources/AfterimageConfig.asset";
    private const string MenuRoot  = "Tools/Farm Game/Afterimage/";

    /// <summary>Bộ entry mặc định — trùng với default của AfterimageConfig.targetEntries.</summary>
    private static AfterimageConfig.Entry[] DefaultEntries()
    {
        Color vehicleTint = new Color(0.88f, 0.94f, 1f, 1f);
        return new AfterimageConfig.Entry[]
        {
            new AfterimageConfig.Entry { typeName = "FlowerGirlShipper" },
            new AfterimageConfig.Entry { typeName = "BuilderWorker" },
            new AfterimageConfig.Entry { typeName = "TouristAgent" },
            new AfterimageConfig.Entry { typeName = "DeliveryCharacterMover" },
            new AfterimageConfig.Entry { typeName = "TrainPathFollower",     includeChildRenderers = true, useTintOverride = true, tintOverride = vehicleTint },
            new AfterimageConfig.Entry { typeName = "TouristBoatController", includeChildRenderers = true, useTintOverride = true, tintOverride = vehicleTint },
            new AfterimageConfig.Entry { typeName = "FerryController",       includeChildRenderers = true, useTintOverride = true, tintOverride = vehicleTint },
        };
    }

    [MenuItem("Tools/Farm Game/★ BẬT AFTERIMAGE CHO TOÀN BỘ NHÂN VẬT & TÀU & THUYỀN", false, 0)]
    public static void SetupAllAndSlice()
    {
        // 1. Cắt lại 3 spritesheet sát biên
        CharacterSheetSliceTool.Apply();

        // 2. Setup Afterimage Config
        Setup(false);

        // 3. Gắn tag cho toàn bộ NPC cảnh & Thuyền & Tàu trong SCN_Farm
        TagSceneNpcs(false);

        EditorUtility.DisplayDialog("Thành công",
            "Đã thực hiện xong toàn bộ:\n" +
            "1. Cắt sạch 100% frame cho Shipper & Worker (không còn dính mép/layer khác)\n" +
            "2. BẬT Afterimage (hiệu ứng bóng ma lưu ảnh) cho Thuyền, Tàu lửa, Shipper, Thợ xây, Du khách, Bà lão, Quân nhân, Nhân viên tàu\n" +
            "3. Cập nhật Y-Sorting để Shipper đi sau nhà sẽ được nhà che tự nhiên!", "Tuyệt vời!");
    }

    [MenuItem(MenuRoot + "★ SETUP hiệu ứng bóng mờ (1 nút)", false, 1)]
    public static void SetupMenu()
    {
        Setup(true);
    }

    public static void Setup(bool showDialog = true)
    {
        AfterimageConfig cfg = AssetDatabase.LoadAssetAtPath<AfterimageConfig>(AssetPath);
        bool created = false;

        if (cfg == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources"))
                AssetDatabase.CreateFolder("Assets/_Game", "Resources");

            cfg = ScriptableObject.CreateInstance<AfterimageConfig>();
            cfg.enabled = true;
            cfg.spawnInterval = 0.07f;
            cfg.ghostLife = 0.38f;
            cfg.startAlpha = 0.55f;
            AssetDatabase.CreateAsset(cfg, AssetPath);
            created = true;
        }

        // IDEMPOTENT: chỉ THÊM entry còn thiếu (so cả targetEntries lẫn legacy targetTypeNames),
        // không đụng entry/chỉnh tay đã có.
        HashSet<string> have = new HashSet<string>();
        if (cfg.targetEntries != null)
            foreach (AfterimageConfig.Entry e in cfg.targetEntries)
                if (e != null && !string.IsNullOrEmpty(e.typeName)) have.Add(e.typeName);
        if (cfg.targetTypeNames != null)
            foreach (string n in cfg.targetTypeNames)
                if (!string.IsNullOrEmpty(n)) have.Add(n);

        List<AfterimageConfig.Entry> merged = new List<AfterimageConfig.Entry>(
            cfg.targetEntries ?? new AfterimageConfig.Entry[0]);
        List<string> added = new List<string>();
        foreach (AfterimageConfig.Entry def in DefaultEntries())
        {
            if (have.Contains(def.typeName)) continue;
            merged.Add(def);
            added.Add(def.typeName);
        }

        bool dirty = created;
        if (added.Count > 0) { cfg.targetEntries = merged.ToArray(); dirty = true; }
        if (!cfg.enabled)    { cfg.enabled = true; dirty = true; }

        if (dirty)
        {
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }
        EditorGUIUtility.PingObject(cfg);
        Selection.activeObject = cfg;

        List<string> all = new List<string>();
        foreach (AfterimageConfig.Entry e in cfg.targetEntries)
            if (e != null && !string.IsNullOrEmpty(e.typeName))
                all.Add(e.typeName + (e.includeChildRenderers ? " (multi-SR)" : ""));
        Debug.Log("[Afterimage] SETUP xong (" +
                  (created ? "tạo mới" : (added.Count > 0 ? "thêm entry thiếu: " + string.Join(", ", added) : "asset đã đủ, không đổi gì")) +
                  "). Target: " + string.Join(", ", all) +
                  " | building-pulse: " + string.Join(", ", cfg.buildingTypeNames ?? new string[0]) +
                  " | NPC cảnh: chạy thêm menu 'Gắn tag cho NPC cảnh (NPC_Villagers)'. Bấm Play để xem.");
    }

    [MenuItem(MenuRoot + "Gắn tag cho NPC cảnh (NPC_Villagers)", false, 10)]
    public static void TagSceneNpcsMenu()
    {
        TagSceneNpcs(true);
    }

    public static void TagSceneNpcs(bool showDialog = true)
    {
        Transform root = null;
        Transform[] allTf = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTf.Length; i++)
            if (allTf[i] != null && allTf[i].name == "NPC_Villagers") { root = allTf[i]; break; }

        if (root == null)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Afterimage",
                    "Không tìm thấy object 'NPC_Villagers' trong scene đang mở.\n" +
                    "Mở SCN_Farm rồi chạy lại menu này.", "OK");
            }
            return;
        }

        List<string> tagged = new List<string>();
        List<string> skipped = new List<string>();
        for (int i = 0; i < root.childCount; i++)
        {
            GameObject child = root.GetChild(i).gameObject;
            if (child.GetComponentInChildren<SpriteRenderer>(true) == null) continue;
            if (child.GetComponent<AfterimageTag>() != null) { skipped.Add(child.name); continue; }
            Undo.AddComponent<AfterimageTag>(child);
            tagged.Add(child.name);
        }

        if (tagged.Count > 0)
        {
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            EditorSceneManager.SaveScene(root.gameObject.scene);
        }

        Debug.Log("[Afterimage] NPC_Villagers: gắn AfterimageTag cho " + tagged.Count + " NPC cảnh" +
                  (tagged.Count > 0 ? ": " + string.Join(", ", tagged) : "") +
                  (skipped.Count > 0 ? " | đã có sẵn (bỏ qua): " + string.Join(", ", skipped) : ""));
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

        List<string> targets = new List<string>();
        if (cfg.targetEntries != null)
            foreach (AfterimageConfig.Entry e in cfg.targetEntries)
                if (e != null && !string.IsNullOrEmpty(e.typeName))
                    targets.Add(e.typeName + (e.includeChildRenderers ? "(multi)" : ""));
        if (cfg.targetTypeNames != null)
            foreach (string n in cfg.targetTypeNames)
                if (!string.IsNullOrEmpty(n)) targets.Add(n + "(legacy)");

        Debug.Log("[Afterimage] Config: enabled=" + cfg.enabled +
                  " | minSpeed=" + cfg.minSpeed + " u/s | spawnInterval=" + cfg.spawnInterval +
                  "s | ghostLife=" + cfg.ghostLife + "s | startAlpha=" + cfg.startAlpha +
                  " | shrink=" + cfg.shrink + " (endScaleMul=" + cfg.endScaleMul + ")" +
                  " | poolCap=" + cfg.poolCap + " | sortingOrderOffset=" + cfg.sortingOrderOffset +
                  " | rescanInterval=" + cfg.rescanInterval + "s" +
                  " | buildingPulse=" + cfg.buildingPulse + " (scaleMul=" + cfg.pulseScaleMul +
                  ", life=" + cfg.pulseLife + "s, alpha=" + cfg.pulseAlpha + "; " +
                  string.Join(", ", cfg.buildingTypeNames ?? new string[0]) + ")" +
                  " | target: " + string.Join(", ", targets));

        if (Application.isPlaying)
        {
            SpriteAfterimageEmitter[] emitters =
                Object.FindObjectsByType<SpriteAfterimageEmitter>(FindObjectsSortMode.None);
            BuildingGhostPulse[] pulses =
                Object.FindObjectsByType<BuildingGhostPulse>(FindObjectsSortMode.None);
            SpriteAfterimage[] ghosts =
                Object.FindObjectsByType<SpriteAfterimage>(FindObjectsSortMode.None);
            AfterimageTag[] tags =
                Object.FindObjectsByType<AfterimageTag>(FindObjectsSortMode.None);
            Debug.Log("[Afterimage] Đang Play: " + emitters.Length + " emitter, " +
                      pulses.Length + " building-pulse, " + tags.Length + " AfterimageTag, " +
                      ghosts.Length + " ghost đang hiện.");
        }
        else
        {
            Debug.Log("[Afterimage] (Ngoài Play Mode — chỉ in config. Vào Play rồi bấm Kiểm tra lại để đếm emitter/pulse.)");
        }
    }
}
