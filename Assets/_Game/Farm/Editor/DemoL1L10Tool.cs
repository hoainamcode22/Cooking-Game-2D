using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool tổng cho Demo Level 1 → Level 10.
/// Menu: Tools → Farm Game → Demo L1-L10
///   • Check All            — kiểm tra PASS/FAIL toàn bộ data/scene theo bảng kinh tế đã duyệt
///   • Setup All            — gọi chuỗi tool setup sẵn có (tutorial, level-up popup, startup popups)
///   • Reset Demo Save      — xoá PlayerPrefs để chơi lại từ New Game
///   • Print Playtest Checklist — in checklist 18 tiêu chí hoàn thành demo
/// LƯU Ý: KHÔNG chạy "Setup Village Orders L1-L6/Apply Phase 1 Data" — tool đó chứa
/// số liệu kinh tế CŨ và sẽ ghi đè data đã duyệt (xem L1_L10_ECONOMY_TABLE.md).
/// </summary>
public static class DemoL1L10Tool
{
    // ── Bảng giá trị kỳ vọng (theo L1_L10_ECONOMY_TABLE.md đã duyệt) ──────────
    private static readonly Dictionary<string, (int unlock, int sell)> ExpectedCrops =
        new Dictionary<string, (int, int)>
        {
            { "rice", (1, 7) },          { "bapcai", (1, 15) },      { "ngo", (2, 13) },
            { "cachua", (3, 20) },       { "carot", (3, 16) },       { "khoaitay", (5, 25) },
            { "nam", (6, 30) },          { "sugarcane", (7, 36) },   { "lemon", (8, 38) },
            { "chili", (9, 48) },        { "pepper", (10, 55) },
            { "huong_duong", (1, 12) },  { "hoa_hong", (4, 24) },    { "hoa_oai_huong", (4, 30) },
            { "hoa_lan", (7, 22) },      { "hoa_cuc_trang", (7, 24) }, { "tulip", (9, 20) },
            { "hoa_cuc_van_tho", (9, 26) }, { "hoa_mau_don", (10, 28) },
            { "hoa_cam_tu_cau", (10, 30) }, { "hoa_anh_thao", (10, 32) },
        };

    private static readonly Dictionary<string, (int unlock, int gold)> ExpectedRawOrders =
        new Dictionary<string, (int, int)>
        {
            { "rice", (1, 15) },   { "bapcai", (1, 26) },  { "ngo", (2, 22) },
            { "cachua", (3, 34) }, { "egg", (3, 24) },     { "chicken_meat", (3, 36) },
            { "huong_duong", (3, 20) }, { "hoa_oai_huong", (4, 48) }, { "hoa_hong", (4, 40) },
            { "pork", (5, 55) },   { "mushroom", (6, 50) }, { "sugarcane", (7, 60) },
            { "beef", (8, 80) },   { "milk", (8, 45) },    { "tulip", (9, 34) },
        };

    // itemID (string) → (giá vàng, unlockLevel) cho 4 chuồng
    private static readonly Dictionary<string, (int gold, int unlock, string label)> ExpectedPens =
        new Dictionary<string, (int, int, string)>
        {
            { "107", (100, 2, "Chuong Ga") },
            { "108", (600, 4, "Chuong Heo") },
            { "106", (1500, 6, "Chuong Bo") },
            { "113", (2000, 8, "Chuong Bo Sua") },
        };

    private static int _pass, _fail, _warn;
    private static StringBuilder _log;

    // ──────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Farm Game/Demo L1-L10/Check All", false, 1)]
    public static void CheckAll()
    {
        _pass = _fail = _warn = 0;
        _log = new StringBuilder();
        _log.AppendLine("===== DEMO L1-L10 — CHECK ALL =====");

        CheckCrops();
        CheckOrders();
        CheckDishes();
        CheckLevelRewards();
        CheckSceneManagers();
        CheckPens();
        CheckStartupPopups();
        CheckMissingScripts();

        _log.AppendLine("-----------------------------------");
        _log.AppendLine($"KẾT QUẢ: {_pass} PASS · {_fail} FAIL · {_warn} WARN");
        _log.AppendLine("Lưu ý: Console đỏ phải tự kiểm tra bằng mắt sau khi bấm Play (tool không đọc được Console).");

        if (_fail > 0) Debug.LogError(_log.ToString());
        else if (_warn > 0) Debug.LogWarning(_log.ToString());
        else Debug.Log(_log.ToString());
    }

    // ── 1. Crops ──────────────────────────────────────────────────────────────
    private static void CheckCrops()
    {
        _log.AppendLine("— CROPS —");
        var found = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:CropData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
            string id = so.FindProperty("cropId")?.stringValue;
            if (string.IsNullOrEmpty(id) || !ExpectedCrops.ContainsKey(id)) continue;

            found.Add(id);
            int unlock = so.FindProperty("unlockLevel")?.intValue ?? -1;
            int sell   = so.FindProperty("sellGold")?.intValue ?? -1;
            var exp    = ExpectedCrops[id];

            if (unlock == exp.unlock && sell == exp.sell) Pass($"crop '{id}' unlock L{unlock}, sell {sell}");
            else Fail($"crop '{id}' unlock L{unlock} (kỳ vọng {exp.unlock}), sell {sell} (kỳ vọng {exp.sell}) — {path}");
        }
        foreach (var missing in ExpectedCrops.Keys.Where(k => !found.Contains(k)))
            Fail($"crop '{missing}' KHÔNG tìm thấy CropData asset");
    }

    // ── 2. Village orders ─────────────────────────────────────────────────────
    private static void CheckOrders()
    {
        _log.AppendLine("— VILLAGE ORDERS —");
        var dishUnlock = new Dictionary<string, int>();
        foreach (string guid in AssetDatabase.FindAssets("t:DishData"))
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
            string id = so.FindProperty("dishId")?.stringValue;
            if (!string.IsNullOrEmpty(id)) dishUnlock[id] = so.FindProperty("unlockLevel")?.intValue ?? 5;
        }

        int orderCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:OrderItemDefinition"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
            string id   = so.FindProperty("itemId")?.stringValue;
            int unlock  = so.FindProperty("unlockLevel")?.intValue ?? -1;
            int gold    = so.FindProperty("goldPerUnit")?.intValue ?? -1;
            int catEnum = so.FindProperty("category")?.enumValueIndex ?? 0;
            orderCount++;

            if (id == "nam") { Fail($"order '{path}' vẫn dùng itemId 'nam' (phải là 'mushroom')"); continue; }

            if (ExpectedRawOrders.TryGetValue(id, out var exp))
            {
                if (unlock == exp.unlock && gold == exp.gold) Pass($"order '{id}' L{unlock}, {gold}g/đv");
                else Fail($"order '{id}' L{unlock}/{gold}g (kỳ vọng L{exp.unlock}/{exp.gold}g) — {path}");
            }
            else if (catEnum == 3) // OrderCategory.Cooking
            {
                if (!dishUnlock.TryGetValue(id, out int dUnlock))
                    Fail($"order món '{id}' KHÔNG có DishData tương ứng — không thể nấu — {path}");
                else if (unlock < dUnlock)
                    Fail($"order món '{id}' mở L{unlock} nhưng món chỉ nấu được từ L{dUnlock} — kẹt đơn — {path}");
                else
                    Pass($"order món '{id}' L{unlock} (món nấu được từ L{dUnlock})");
            }
            if (gold <= 0) Fail($"order '{id}' goldPerUnit={gold} — {path}");
        }
        if (orderCount < 30) Warn($"chỉ thấy {orderCount} OrderItemDefinition (kỳ vọng ~33)");
    }

    // ── 3. Dishes ─────────────────────────────────────────────────────────────
    private static void CheckDishes()
    {
        _log.AppendLine("— COOKING DISHES —");
        int count = 0, fish = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:DishData"))
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
            string id  = so.FindProperty("dishId")?.stringValue;
            var prop   = so.FindProperty("unlockLevel");
            count++;
            if (prop == null) { Fail($"DishData '{id}' THIẾU field unlockLevel (script chưa compile lại?)"); continue; }
            if (id == "ca_nuong_tieu" || id == "canh_chua_ca")
            {
                fish++;
                if (prop.intValue >= 99) Pass($"món cá '{id}' đã khoá (L{prop.intValue})");
                else Fail($"món cá '{id}' unlock L{prop.intValue} — phải ≥99 vì chưa có hệ cá");
            }
            else if (prop.intValue < 5 || prop.intValue > 10)
                Warn($"món '{id}' unlock L{prop.intValue} — ngoài dải L5-L10");
            else Pass($"món '{id}' unlock L{prop.intValue}");
        }
        if (count != 20) Warn($"thấy {count}/20 DishData");
        if (fish != 2) Warn($"thấy {fish}/2 món cá");
    }

    // ── 4. Level rewards ──────────────────────────────────────────────────────
    private static void CheckLevelRewards()
    {
        _log.AppendLine("— LEVEL-UP REWARDS —");
        var levels = new HashSet<int>();
        foreach (string guid in AssetDatabase.FindAssets("t:LevelRewardConfig"))
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
            int lv = so.FindProperty("levelReached")?.intValue ?? -1;
            if (lv >= 2 && lv <= 10) levels.Add(lv);
        }
        for (int lv = 2; lv <= 10; lv++)
        {
            if (levels.Contains(lv)) Pass($"LevelReward L{lv} tồn tại");
            else Fail($"THIẾU LevelRewardConfig cho L{lv}");
        }

        // L11-L30: ngoài phạm vi demo — chỉ thông tin (tạo bằng Setup Reward Data L2-L30)
        int extCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:LevelRewardConfig"))
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
            int lv = so.FindProperty("levelReached")?.intValue ?? -1;
            if (lv >= 11 && lv <= 30) extCount++;
        }
        if (extCount >= 20) Pass($"LevelReward L11-L30: {extCount}/20");
        else Warn($"LevelReward L11-L30: {extCount}/20 — chạy Setup Reward Data (L2-L30) để tạo đủ");

        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup == null) Warn("LevelUpPopupUI không có trong scene đang mở (mở SCN_Farm rồi check lại)");
        else
        {
            var so = new SerializedObject(popup);
            int n = so.FindProperty("levelRewardConfigs")?.arraySize ?? 0;
            if (n >= 9) Pass($"LevelUpPopupUI đã gán {n} config");
            else Fail($"LevelUpPopupUI mới gán {n}/9 config — chạy Tools → Farm Game → Setup Level Up Popup → Setup Reward Data (L2-L10)");
        }
    }

    // ── 5. Scene managers ─────────────────────────────────────────────────────
    private static void CheckSceneManagers()
    {
        _log.AppendLine("— SCENE MANAGERS —");
        var econ = Object.FindFirstObjectByType<FarmEconomyManager>(FindObjectsInactive.Include);
        if (econ == null) Warn("FarmEconomyManager không có trong scene đang mở");
        else
        {
            var so = new SerializedObject(econ);
            int g = so.FindProperty("startGold")?.intValue ?? -1;
            int d = so.FindProperty("startGems")?.intValue ?? -1;
            if (g == 400 && d == 15) Pass("starter 400 vàng / 15 gem");
            else Fail($"starter {g} vàng / {d} gem (kỳ vọng 400/15)");
        }

        var vom = Object.FindFirstObjectByType<Village.VillageOrderManager>(FindObjectsInactive.Include);
        if (vom == null) Warn("VillageOrderManager không có trong scene đang mở");
        else
        {
            var so = new SerializedObject(vom);
            int steps = so.FindProperty("houseUnlockSteps")?.arraySize ?? 0;
            if (steps > 0) Pass($"houseUnlockSteps: {steps} mốc (L1=4 nhà → L9=8 nhà)");
            else Fail("houseUnlockSteps trống — mọi nhà sẽ nhận đơn từ L1");

            float l1 = so.FindProperty("twoItemChanceL1")?.floatValue ?? -1f;
            if (Mathf.Approximately(l1, 0f)) Pass("đơn L1 luôn 1-item");
            else Warn($"twoItemChanceL1 = {l1} (đề xuất 0)");
        }
    }

    // ── 6. Pens ───────────────────────────────────────────────────────────────
    private static void CheckPens()
    {
        _log.AppendLine("— ANIMAL PENS (SHOP) —");
        var found = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:BuildingData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
            string id = so.FindProperty("itemID")?.stringValue;
            if (string.IsNullOrEmpty(id) || !ExpectedPens.ContainsKey(id)) continue;

            found.Add(id);
            int gold   = so.FindProperty("goldPrice")?.intValue ?? -1;
            var unlockProp = so.FindProperty("unlockLevel");
            int unlock = unlockProp?.intValue ?? -1;
            var exp = ExpectedPens[id];

            if (unlockProp == null)
                Fail($"{exp.label} (itemID {id}) THIẾU field unlockLevel — PlaceableItemData chưa compile?");
            else if (gold == exp.gold && unlock == exp.unlock)
                Pass($"{exp.label}: {gold} vàng, mở L{unlock}");
            else
                Fail($"{exp.label}: {gold} vàng/L{unlock} (kỳ vọng {exp.gold}/L{exp.unlock}) — {path}");
        }
        foreach (var missing in ExpectedPens.Where(kv => !found.Contains(kv.Key)))
            Fail($"không tìm thấy BuildingData itemID {missing.Key} ({missing.Value.label})");

        // — Máy chế biến L11-L15 (Sprint 6 — ngoài phạm vi demo L1-L10, chỉ INFO/WARN) —
        var machineIds = new HashSet<string> { "120", "121", "122" };
        var machineFound = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:BuildingData"))
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
            string id = so.FindProperty("itemID")?.stringValue;
            if (!string.IsNullOrEmpty(id) && machineIds.Contains(id)) machineFound.Add(id);
        }
        if (machineFound.Count >= 3)
            Pass($"Máy chế biến L11-L15: {machineFound.Count}/3 (itemID 120/121/122)");
        else
            Warn($"Máy chế biến L11-L15: {machineFound.Count}/3 — chạy Tools → Farm Game → Setup Production Machines L11-L15 nếu thiếu");
    }

    // ── 7. Startup popups ─────────────────────────────────────────────────────
    private static void CheckStartupPopups()
    {
        _log.AppendLine("— STARTUP POPUPS —");
        CheckPopupInactive<MarketPopupUI>("Market");
        CheckPopupInactive<WarehousePopupUI>("Warehouse");
    }

    private static void CheckPopupInactive<T>(string label) where T : Component
    {
        var popup = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (popup == null) { Warn($"{label}PopupUI không thấy trong scene đang mở"); return; }

        // Chuẩn đúng là kiểm tra popupRoot (panel con) — script popup luôn active, chỉ panel tắt.
        var so = new SerializedObject(popup);
        var rootProp = so.FindProperty("popupRoot");
        var rootGo = rootProp != null ? rootProp.objectReferenceValue as GameObject : null;

        if (rootGo != null)
        {
            if (rootGo.activeSelf) Fail($"{label} popupRoot '{rootGo.name}' đang ACTIVE — sẽ tự mở khi Play. Chạy Tools → Farm Game → Setup → Disable Startup Popups");
            else Pass($"{label} popupRoot '{rootGo.name}' tắt khi start");
        }
        else
        {
            // Không có field popupRoot → fallback check cả GameObject (ít chính xác hơn)
            if (popup.gameObject.activeSelf) Warn($"{label}: không tìm thấy field popupRoot, GameObject đang active — tự kiểm tra bằng mắt khi Play");
            else Pass($"{label} popup tắt khi start");
        }
    }

    // ── 8. Missing scripts ────────────────────────────────────────────────────
    private static void CheckMissingScripts()
    {
        _log.AppendLine("— MISSING SCRIPTS (scene đang mở) —");
        int missing = 0;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                missing += CountMissingRecursive(root.transform);
        }
        if (missing == 0) Pass("0 missing script");
        else Fail($"{missing} component missing script trong scene");
    }

    private static int CountMissingRecursive(Transform t)
    {
        int n = t.GetComponents<Component>().Count(c => c == null);
        for (int i = 0; i < t.childCount; i++) n += CountMissingRecursive(t.GetChild(i));
        return n;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void Pass(string msg) { _pass++; _log.AppendLine($"  ✔ PASS  {msg}"); }
    private static void Fail(string msg) { _fail++; _log.AppendLine($"  ✘ FAIL  {msg}"); }
    private static void Warn(string msg) { _warn++; _log.AppendLine($"  ⚠ WARN  {msg}"); }

    // ──────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Farm Game/Demo L1-L10/Setup All", false, 2)]
    public static void SetupAll()
    {
        var sb = new StringBuilder("===== DEMO L1-L10 — SETUP ALL =====\n");

        // Gọi trực tiếp (menu "Setup Level Up Popup" là submenu cha nên ExecuteMenuItem không chạy được)
        LevelUpPopupSetupTool.EnsureExists();
        sb.AppendLine("  ✔ chạy: LevelUpPopupSetupTool.EnsureExists()");

        string[] menus =
        {
            "Tools/Farm Game/Setup Level Up Popup/Setup Reward Data (L2-L30)",
            "Tools/Farm Game/Setup Tutorial L1-L2",
            "Tools/Farm Game/Setup/Disable Startup Popups",
        };
        foreach (string menu in menus)
        {
            bool ok = EditorApplication.ExecuteMenuItem(menu);
            sb.AppendLine((ok ? "  ✔ chạy: " : "  ⚠ không tìm thấy menu: ") + menu);
        }

        // ── Animal Guide — tip ngữ cảnh chuồng trại L2-L8 (Sprint 3) ──────────
        // Gắn AnimalGuideController lên cùng GameObject với TutorialManager.
        // KHÔNG đụng vào TutorialManager — guide chỉ nghe OnLevelChanged.
        var tutorialMgr = Object.FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
        if (tutorialMgr == null)
        {
            sb.AppendLine("  ⚠ [SetupAll] Không thấy TutorialManager trong scene — bỏ qua AnimalGuideController (mở SCN_Farm rồi chạy lại)");
        }
        else
        {
            if (tutorialMgr.GetComponent<AnimalGuideController>() == null)
            {
                tutorialMgr.gameObject.AddComponent<AnimalGuideController>();
                EditorUtility.SetDirty(tutorialMgr.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tutorialMgr.gameObject.scene);
            }
            sb.AppendLine("  ✔ [SetupAll] AnimalGuideController OK");
        }

        // ── Đồng bộ VillageOrderManager.availableItems với TOÀN BỘ OrderItemDefinition ──
        // availableItems là list serialize trong scene → item đơn hàng mới
        // (vd OrderItem_Milk) chỉ cần tạo asset rồi chạy Setup All là vào pool.
        var vomSync = Object.FindFirstObjectByType<Village.VillageOrderManager>(FindObjectsInactive.Include);
        if (vomSync == null)
        {
            sb.AppendLine("  ⚠ [SetupAll] Không thấy VillageOrderManager trong scene — bỏ qua đồng bộ availableItems");
        }
        else
        {
            var orderDefs = AssetDatabase.FindAssets("t:OrderItemDefinition")
                .Select(g => AssetDatabase.LoadAssetAtPath<Village.OrderItemDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(d => d != null)
                .OrderBy(d => d.unlockLevel).ThenBy(d => d.itemId)
                .ToList();

            var soVom = new SerializedObject(vomSync);
            var itemsProp = soVom.FindProperty("availableItems");
            itemsProp.ClearArray();
            for (int i = 0; i < orderDefs.Count; i++)
            {
                itemsProp.InsertArrayElementAtIndex(i);
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = orderDefs[i];
            }
            soVom.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(vomSync);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(vomSync.gameObject.scene);
            sb.AppendLine($"  ✔ [SetupAll] VillageOrderManager.availableItems ← {orderDefs.Count} OrderItemDefinition (quét toàn bộ asset)");
        }

        sb.AppendLine("KHÔNG chạy 'Setup Village Orders L1-L6/Apply Phase 1 Data' — tool cũ, sẽ ghi đè kinh tế đã duyệt.");
        sb.AppendLine("Tiếp theo: chạy Check All rồi lưu scene.");
        Debug.Log(sb.ToString());

        // ── CoinFlyFX — hiệu ứng "coin bay về ví" khi nhận vàng (auto-wire) ──────
        SetupCoinFlyFX();

        // Ping các khung đã tạo cho user thấy ngay trong Hierarchy
        var popupCreated = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popupCreated != null)
        {
            Selection.activeGameObject = popupCreated.gameObject;
            EditorGUIUtility.PingObject(popupCreated.gameObject);
            Debug.Log($"[SetupAll] KHUNG POPUP Ở ĐÂY → {GetPath(popupCreated.transform)}\n" +
                      "Muốn NHÌN THẤY khung để gắn ảnh: Tools → Farm Game → Demo L1-L10 → Preview → Bật khung Level-Up Popup / Guide Board");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // COIN FLY FX — đảm bảo GameObject "CoinFlyFX" nằm dưới HUD canvas và
    // best-effort tự gắn canvas + targetGoldIcon (icon vàng) + coinSprite.
    // Không tìm thấy icon → để NULL: runtime tự bay về góc phải-trên màn hình.
    // ──────────────────────────────────────────────────────────────────────────
    private static void SetupCoinFlyFX()
    {
        // 1. Tìm HUD canvas: ưu tiên đúng tên "Canvas_HUD", fallback canvas đầu tiên
        Canvas canvas = null;
        var hudGo = GameObject.Find("Canvas_HUD");
        if (hudGo != null) canvas = hudGo.GetComponent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogWarning("[SetupAll] CoinFlyFX: không tìm thấy Canvas nào trong scene — bỏ qua.");
            return;
        }

        // 2. Đảm bảo GameObject "CoinFlyFX" tồn tại dưới canvas
        var fx = Object.FindFirstObjectByType<CoinFlyFX>(FindObjectsInactive.Include);
        if (fx == null)
        {
            var go = new GameObject("CoinFlyFX", typeof(RectTransform));
            go.layer = canvas.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            fx = go.AddComponent<CoinFlyFX>();
            Undo.RegisterCreatedObjectUndo(go, "Create CoinFlyFX");
        }

        // 3. Best-effort wiring qua SerializedObject (không ghi đè field đã gắn tay)
        var so = new SerializedObject(fx);
        var canvasProp = so.FindProperty("canvas");
        var targetProp = so.FindProperty("targetGoldIcon");
        var spriteProp = so.FindProperty("coinSprite");

        if (canvasProp != null && canvasProp.objectReferenceValue == null)
            canvasProp.objectReferenceValue = canvas;

        if (targetProp != null && targetProp.objectReferenceValue == null)
        {
            var icon = FindGoldIcon(canvas);
            if (icon != null)
            {
                targetProp.objectReferenceValue = icon.rectTransform;
                if (spriteProp != null && spriteProp.objectReferenceValue == null && icon.sprite != null)
                    spriteProp.objectReferenceValue = icon.sprite;
                Debug.Log($"[SetupAll] CoinFlyFX: đã gắn icon vàng HUD → {GetPath(icon.transform)}");
            }
            else
            {
                Debug.LogWarning("[SetupAll] CoinFlyFX: gắn tay targetGoldIcon (icon vàng HUD) vào component CoinFlyFX — tạm thời xu vẫn bay về góc phải-trên màn hình.");
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fx);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fx.gameObject.scene);
    }

    /// <summary>
    /// Tìm Image icon vàng dưới canvas: ưu tiên đúng tên "Vangicon" (HUD hiện tại),
    /// sau đó tên chứa gold/coin/vang — chỉ nhận khi duy nhất 1 kết quả (tránh gắn nhầm).
    /// </summary>
    private static UnityEngine.UI.Image FindGoldIcon(Canvas canvas)
    {
        var images = canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true);

        foreach (var img in images)
            if (img.sprite != null && string.Equals(img.name, "Vangicon", System.StringComparison.OrdinalIgnoreCase))
                return img;

        var matches = images.Where(i =>
        {
            string n = i.name.ToLowerInvariant();
            return n.Contains("gold") || n.Contains("coin") || n.Contains("vang");
        }).ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PREVIEW — bật khung popup ngay trong Editor để gắn ảnh, không cần Play
    // ──────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Farm Game/Demo L1-L10/Preview/Bật khung Level-Up Popup", false, 20)]
    public static void PreviewLevelUpPopup()
    {
        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            EditorUtility.DisplayDialog("Preview", "Chưa có LevelUpPopup trong scene.\nChạy Demo L1-L10 → Setup All trước nhé.", "OK");
            return;
        }

        var go = popup.gameObject;
        go.SetActive(true);
        foreach (Transform child in go.transform) child.gameObject.SetActive(true);
        foreach (var cg in go.GetComponentsInChildren<CanvasGroup>(true)) cg.alpha = 1f;

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("===== PREVIEW LEVEL-UP POPUP — ĐANG BẬT =====\n" +
                  $"Hierarchy: {GetPath(go.transform)}\n" +
                  "GẮN ẢNH/CHỈNH KHUNG Ở ĐÂY:\n" +
                  "  • Nền khung gỗ: Image trên các panel con (chọn trong Inspector)\n" +
                  "  • Icon quà: tự tra từ CropData khi chạy Setup Reward Data (L2-L30) — thiếu thì gắn tay vào LevelReward_L*.asset\n" +
                  "  • VFX 2 bên: field vfxSidePrefab / vfxLeftPoint / vfxRightPoint trên LevelUpPopupUI\n" +
                  "XONG thì bấm: Demo L1-L10 → Preview → Tắt hết Preview, RỒI MỚI Save scene!");
    }

    [MenuItem("Tools/Farm Game/Demo L1-L10/Preview/Bật khung Guide Board (4 ảnh tutorial)", false, 21)]
    public static void PreviewGuideBoard()
    {
        var board = Object.FindFirstObjectByType<TutorialGuideBoardUI>(FindObjectsInactive.Include);
        if (board == null)
        {
            EditorUtility.DisplayDialog("Preview", "Chưa có Tutorial_GuideBoard trong scene.\nChạy Demo L1-L10 → Setup All trước nhé.", "OK");
            return;
        }

        var go = board.gameObject;
        go.SetActive(true);
        foreach (Transform child in go.transform) child.gameObject.SetActive(true);
        foreach (var cg in go.GetComponentsInChildren<CanvasGroup>(true)) cg.alpha = 1f;

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("===== PREVIEW GUIDE BOARD — ĐANG BẬT =====\n" +
                  $"Hierarchy: {GetPath(go.transform)}\n" +
                  "GẮN 4 ẢNH MINH HOẠ VÀO:\n" +
                  "  ContentPanel/StepCards/StepCard_1/IllustrationImage  (gieo hạt)\n" +
                  "  ContentPanel/StepCards/StepCard_2/IllustrationImage  (tăng tốc)\n" +
                  "  ContentPanel/StepCards/StepCard_3/IllustrationImage  (thu hoạch)\n" +
                  "  ContentPanel/StepCards/StepCard_4/IllustrationImage  (nhận thưởng)\n" +
                  "NPC portrait: gắn sprite vào TutorialManager._npcPortrait (cùng object Tutorial Manager).\n" +
                  "XONG thì bấm: Demo L1-L10 → Preview → Tắt hết Preview, RỒI MỚI Save scene!");
    }

    [MenuItem("Tools/Farm Game/Demo L1-L10/Preview/Tắt hết Preview", false, 22)]
    public static void HideAllPreviews()
    {
        int hidden = 0;

        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            foreach (Transform child in popup.transform) child.gameObject.SetActive(false);
            foreach (var cg in popup.GetComponentsInChildren<CanvasGroup>(true)) cg.alpha = 0f;
            hidden++;
        }

        var board = Object.FindFirstObjectByType<TutorialGuideBoardUI>(FindObjectsInactive.Include);
        if (board != null) { board.gameObject.SetActive(false); hidden++; }

        Debug.Log($"[Preview] Đã tắt {hidden} khung preview — giờ Save scene (Ctrl+S) được rồi. " +
                  "Khi chơi thật, code tự bật popup đúng lúc.");
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

    [MenuItem("Tools/Farm Game/Demo L1-L10/List Missing Scripts", false, 5)]
    public static void ListMissingScripts()
    {
        var sb = new StringBuilder("===== MISSING SCRIPTS — VỊ TRÍ CỤ THỂ =====\n");
        int total = 0;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                ListMissingRecursive(root.transform, root.name, sb, ref total);
        }
        sb.AppendLine($"Tổng: {total} component missing script.");
        sb.AppendLine("KHÔNG tự xoá — gửi danh sách này cho team để duyệt trước khi dọn (Phase 13).");
        Debug.LogWarning(sb.ToString());
    }

    private static void ListMissingRecursive(Transform t, string path, StringBuilder sb, ref int total)
    {
        int n = t.GetComponents<Component>().Count(c => c == null);
        if (n > 0) { total += n; sb.AppendLine($"  • {path}  ({n} missing)"); }
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            ListMissingRecursive(child, path + "/" + child.name, sb, ref total);
        }
    }

    [MenuItem("Tools/Farm Game/Demo L1-L10/Reset Demo Save", false, 3)]
    public static void ResetDemoSave()
    {
        if (!EditorUtility.DisplayDialog("Reset Demo Save",
                "Xoá TOÀN BỘ PlayerPrefs (level, EXP, vàng, gem, kho, ô đất)?\nNew Game sẽ bắt đầu: Level 1, 400 vàng, 15 gem.",
                "Xoá và chơi lại", "Huỷ"))
            return;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[DemoL1L10] Đã xoá PlayerPrefs — vào Play Mode để bắt đầu New Game L1.");
    }

    [MenuItem("Tools/Farm Game/Demo L1-L10/Print Playtest Checklist", false, 4)]
    public static void PrintChecklist()
    {
        Debug.Log(@"===== PLAYTEST CHECKLIST DEMO L1-L10 =====
[ ] New Game chạy sạch (Reset Demo Save → Play)
[ ] Không popup chợ/kho/shop nào tự mở khi Play
[ ] Tutorial L1 chạy mượt (NPC chào → guide board → kéo hạt lúa)
[ ] Camera zoom đúng 6 ô đất, rồi tới chậu hoa
[ ] Hand pointer kéo từ seed panel đến đúng ô
[ ] Trồng 6 lúa + 2 hoa → đủ 40 EXP → lên Level 2
[ ] Popup level-up hiện (quà + unlock list + pháo hoa)
[ ] EXP dư giữ lại sau khi lên cấp
[ ] Shop: hạt ngô khoá tới L2, cà chua L3… overlay 'Mở ở cấp X'
[ ] Chuồng gà 100g mở L2, heo 600g L4, bò 1500g L6
[ ] L1 chỉ 4 nhà có bubble đơn hàng; L3 thêm nhà thứ 5
[ ] Đơn L1 chỉ 1 item (lúa/bắp cải), giao có lời
[ ] L5 mở bếp; danh sách món: 3 món sáng, món khoá xám
[ ] Nấu thành công +8 EXP; món vào kho farm
[ ] Đơn món ăn chỉ xuất hiện từ L5, không đòi món chưa nấu được
[ ] Không đơn nào đòi món cá
[ ] Chơi liền L1→L10 không kẹt tiền (theo dõi số dư)
[ ] Unity Console 0 error đỏ");
    }
}
