using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Setup 1-click Kitchen UI v2 (Sprint K1 — 2026-08-26).
/// Mở SampleScene rồi chạy: Tools → Farm Game → Kitchen → Setup Kitchen UI v2.
/// Tạo GO "Kitchen_UI_v2" + gán data (21 IngredientData, ListDishData) + DailySpecialManager.
/// KHÔNG xoá/tắt UI cũ — canvas v2 che phủ; minigame/popup cũ được UI v2 tự nâng lên trên.
/// </summary>
public static class KitchenV2SetupTool
{
    [MenuItem("Tools/Farm Game/Kitchen/Setup Kitchen UI v2")]
    public static void SetupKitchenV2()
    {
        var challenge = Object.FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
        if (challenge == null)
        {
            Debug.LogError("[KitchenV2] Không thấy CookingChallengeManager — mở đúng SampleScene (scene nấu ăn) rồi chạy lại.");
            return;
        }
        var selection = Object.FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);

        // 1. Root GO
        var ui = Object.FindFirstObjectByType<KitchenUIv2.KitchenSceneV2UI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            var go = new GameObject("Kitchen_UI_v2", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create Kitchen_UI_v2");
            ui = go.AddComponent<KitchenUIv2.KitchenSceneV2UI>();
        }

        var daily = ui.GetComponent<KitchenUIv2.DailySpecialManager>();
        if (daily == null) daily = Undo.AddComponent<KitchenUIv2.DailySpecialManager>(ui.gameObject);

        // 2. Data: 21 IngredientData + ListDishData
        var ingredients = new System.Collections.Generic.List<IngredientData>();
        foreach (var g in AssetDatabase.FindAssets("t:IngredientData"))
        {
            var a = AssetDatabase.LoadAssetAtPath<IngredientData>(AssetDatabase.GUIDToAssetPath(g));
            if (a != null) ingredients.Add(a);
        }
        ingredients.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        ListDishData book = null;
        foreach (var g in AssetDatabase.FindAssets("t:ListDishData"))
        {
            book = AssetDatabase.LoadAssetAtPath<ListDishData>(AssetDatabase.GUIDToAssetPath(g));
            if (book != null) break;
        }

        // 3. Gán qua SerializedObject (field private)
        var so = new SerializedObject(ui);
        var arr = so.FindProperty("allIngredients");
        arr.arraySize = ingredients.Count;
        for (int i = 0; i < ingredients.Count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = ingredients[i];
        so.FindProperty("dishBook").objectReferenceValue = book;
        so.FindProperty("challenge").objectReferenceValue = challenge;
        so.FindProperty("selection").objectReferenceValue = selection;
        so.ApplyModifiedProperties();

        // 4. SKIN K2 — nạp 36 sprite từ Export_Kitchen_UI_Package (serialize vào scene = an toàn khi build)
        _skinOk = 0; _skinFail = 0;
        AssignSkin(so);
        so.ApplyModifiedProperties();
        if (_skinFail > 0)
            Debug.LogWarning($"[KitchenV2] SKIN: nạp được {_skinOk}, THIẾU {_skinFail} — đọc các warning phía trên.");
        else
            Debug.Log($"[KitchenV2] SKIN: nạp đủ {_skinOk}/{_skinOk} sprite ✔");

        // 5. Sữa khoá cấp 14 (theo mockup) — sửa data QUA TOOL, có log
        foreach (var g in AssetDatabase.FindAssets("t:IngredientData"))
        {
            var a = AssetDatabase.LoadAssetAtPath<IngredientData>(AssetDatabase.GUIDToAssetPath(g));
            if (a != null && a.name == "SEA_Milk" && a.unlockLevel != 14)
            {
                Undo.RecordObject(a, "Set Milk unlockLevel");
                a.unlockLevel = 14;
                EditorUtility.SetDirty(a);
                Debug.Log("[KitchenV2] SEA_Milk.unlockLevel = 14 (thẻ khoá theo mockup).");
            }
        }
        AssetDatabase.SaveAssets();

        var soDaily = new SerializedObject(daily);
        soDaily.FindProperty("dishBook").objectReferenceValue = book;
        soDaily.ApplyModifiedProperties();

        // BAKE PREVIEW: dựng UI ngay trong Edit mode để quan sát không cần Play
        // (Play sẽ tự dọn preview và dựng bản tươi — không bao giờ nhân đôi).
        // UI sống trong Hierarchy (2026-08-26): đã có khung thì KHÔNG tự dựng lại —
        // dựng lại sẽ xóa mọi chỉnh tay của Sếp. Chỉ hỏi khi thật sự muốn reset.
        bool hasHierarchy = ui.transform.Find("Order_Banner") != null;
        if (!hasHierarchy)
        {
            ui.BuildEditorPreview();
        }
        else if (EditorUtility.DisplayDialog("Kitchen UI v2",
                     "UI đã nằm sẵn trong Hierarchy.\n\n• GIỮ KHUNG HIỆN TẠI: chỉ nạp lại data + skin (an toàn, khuyên dùng — art mới tự lên vì ghi đè cùng file).\n• DỰNG LẠI TỪ ĐẦU: xóa toàn bộ chỉnh tay của Sếp, dựng khung mới theo code.",
                     "Giữ khung hiện tại", "DỰNG LẠI từ đầu"))
        {
            Debug.Log("[KitchenV2] Giữ khung Hierarchy — chỉ nạp lại data/skin ✔");
        }
        else
        {
            ui.BuildEditorPreview();
            Debug.Log("[KitchenV2] Đã DỰNG LẠI khung từ code (chỉnh tay cũ đã bị thay).");
        }

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorGUIUtility.PingObject(ui);

        Debug.Log($"[KitchenV2] XONG ✔ nguyên liệu: {ingredients.Count} · sổ món: " +
                  $"{(book != null ? book.allDishes.Count.ToString() : "KHÔNG THẤY ListDishData!")} · " +
                  "NHỚ SAVE SCENE (Ctrl+S) rồi vào Play xem UI mới.");
    }

    // ── SKIN K2 ──────────────────────────────────────────────────────────────

    private const string KDir = "Assets/Export_Kitchen_UI_Package/Sprites";

    private static void AssignSkin(SerializedObject so)
    {
        S(so, "skin.wallTile",        KDir + "/kitchen_wall_tile.png");
        S(so, "skin.floorTile",       KDir + "/kitchen_floor_diamond_tile.png");
        S(so, "skin.shelfProps",      KDir + "/kitchen_shelf_props.png");
        S(so, "skin.plantPot",        KDir + "/plant_pot.png");
        S(so, "skin.sackFlour",       KDir + "/sack_flour.png");
        S(so, "skin.catSleeping",     KDir + "/cat_sleeping.png");
        S(so, "skin.ovenBody",        KDir + "/oven_body.png");
        S(so, "skin.ovenGlow",        KDir + "/oven_glow.png");
        S(so, "skin.smokePuff",       "Assets/Export_Train_UI_Package/Sprites/train_smoke_puff.png");
        S(so, "skin.prepTable",       KDir + "/prep_table.png");
        S(so, "skin.platingTable",    KDir + "/plating_table.png");
        S(so, "skin.warehouseHatch",  KDir + "/warehouse_hatch.png");
        S(so, "skin.chalkboard",      KDir + "/chalkboard_menu.png");
        S(so, "skin.panelBoard",      KDir + "/panel_board_wood.png");
        S(so, "skin.panelPaper",      KDir + "/panel_paper_cream.png");
        S(so, "skin.cardIngredient",  KDir + "/card_ingredient.png");
        S(so, "skin.cardSelectedGlow",KDir + "/card_selected_glow.png");
        S(so, "skin.cardLocked",      KDir + "/card_locked.png");
        S(so, "skin.iconLock",        KDir + "/icon_lock.png");
        S(so, "skin.tasteTrack",      KDir + "/taste_bar_track.png");
        S(so, "skin.tasteFill",       KDir + "/taste_bar_fill.png");
        S(so, "skin.tasteMarker",     KDir + "/taste_marker.png");
        S(so, "skin.btnGreen",        KDir + "/btn_big_green.png");
        S(so, "skin.btnGray",         KDir + "/btn_big_gray.png");
        S(so, "skin.btnRedSmall",     KDir + "/btn_red_small.png");
        S(so, "skin.tabOn",           KDir + "/tab_pill_on.png");
        S(so, "skin.tabOff",          KDir + "/tab_pill_off.png");
        S(so, "skin.chipTaste",       KDir + "/chip_taste.png");
        S(so, "skin.ribbon",          KDir + "/ribbon_header_orange.png");

        SArr(so, "skin.ovenFire",   KDir + "/oven_fire_0{0}.png",   4);
        SArr(so, "skin.manekiIdle", KDir + "/maneki_idle_0{0}.png", 4);

        // ── Polish R3: file nào art chưa giao thì chỉ nhắc nhẹ, KHÔNG tính lỗi ──
        SOpt(so, "skin.btnBackFarm",   KDir + "/btn_back_farm_sign.png", KDir + "/btn_back_to_farm.png");
        SOpt(so, "skin.btnPaperSmall", KDir + "/btn_paper_small.png",    null);
        SOpt(so, "skin.cookPot",       KDir + "/cook_pot.png",           null);
        SOpt(so, "skin.decorGarlic",   KDir + "/deco_garlic_string.png", null);
        SOpt(so, "skin.decorOnion",    KDir + "/deco_onion_string.png",  null);
        SOpt(so, "skin.decorHerbs",    KDir + "/deco_herb_bunch.png",    null);
        SOpt(so, "skin.decorLights",   KDir + "/deco_string_lights.png", null);
        SArrOpt(so, "skin.catChefWalk", KDir + "/cat_chef_walk_0{0}.png", 6);
        SOpt(so, "skin.iconGold",        KDir + "/icon_gold.png",         null);
        SOpt(so, "skin.plaqueOvenState", KDir + "/plaque_oven_state.png", null);
        SOpt(so, "skin.decoCrateStack",  KDir + "/deco_crate_stack.png",  null);
        SOpt(so, "skin.decoFirewood",    KDir + "/deco_firewood.png",     null);

        // Lửa lò prefab: TẠM TẮT (2026-08-26) — bật cần đổi canvas sang ScreenSpaceCamera,
        // làm canvas UI CŨ (Overlay) đè lên UI mới. Bật lại ở K3 sau khi xóa UI cũ:
        // gán Area_fire_red vào ovenFirePrefab + tick useCameraCanvasForFire.
        var fireProp = so.FindProperty("ovenFirePrefab");
        if (fireProp != null && fireProp.objectReferenceValue != null)
        {
            fireProp.objectReferenceValue = null;
            Debug.Log("[KitchenV2] Lửa lò: GỠ prefab (tạm dùng lửa frame — chờ K3 xóa UI cũ mới bật lại được)");
        }
        var camFlag = so.FindProperty("useCameraCanvasForFire");
        if (camFlag != null) camFlag.boolValue = false;
    }

    private static int _skinOk, _skinFail;

    /// <summary>
    /// Load sprite; nếu null (meta đội vẽ để sai textureType/spriteMode) thì TỰ SỬA IMPORTER
    /// về Sprite/Single + reimport rồi thử lại — hết cảnh skin trắng vì lỗi meta.
    /// </summary>
    private static Sprite LoadSpriteFixed(string path)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;

        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            Debug.LogWarning($"[KitchenV2] KHÔNG CÓ FILE: {path}");
            return null;
        }

        var tis = new TextureImporterSettings();
        imp.ReadTextureSettings(tis);
        tis.textureType = TextureImporterType.Sprite;
        if (tis.spriteMode != (int)SpriteImportMode.Single)
            tis.spriteMode = (int)SpriteImportMode.Single;
        tis.mipmapEnabled = false;
        imp.SetTextureSettings(tis);
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();

        sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null)
            Debug.Log($"[KitchenV2] Đã sửa importer → Sprite: {System.IO.Path.GetFileName(path)}");
        else
            Debug.LogWarning($"[KitchenV2] VẪN KHÔNG LOAD ĐƯỢC (file hỏng?): {path}");
        return sp;
    }

    private static void S(SerializedObject so, string field, string path)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[KitchenV2] Thiếu field {field}"); _skinFail++; return; }
        var sp = LoadSpriteFixed(path);
        if (sp == null) { _skinFail++; return; }
        prop.objectReferenceValue = sp;
        _skinOk++;
    }

    private static void SArr(SerializedObject so, string field, string pathFmt, int count)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { _skinFail += count; return; }
        prop.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            var sp = LoadSpriteFixed(string.Format(pathFmt, i + 1));
            if (sp == null) _skinFail++; else _skinOk++;
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sp;
        }
    }

    /// <summary>Sprite TUỲ CHỌN: chưa có file → log nhẹ "chờ art", không tính vào _skinFail. Có fallbackPath thì dùng tạm.</summary>
    private static void SOpt(SerializedObject so, string field, string path, string fallbackPath)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[KitchenV2] Thiếu field {field}"); return; }
        string use = null;
        if (System.IO.File.Exists(path)) use = path;
        else if (!string.IsNullOrEmpty(fallbackPath) && System.IO.File.Exists(fallbackPath)) use = fallbackPath;
        if (use == null)
        {
            Debug.Log($"[KitchenV2] (chờ art) {System.IO.Path.GetFileName(path)} chưa có — bỏ qua, không phải lỗi.");
            return;
        }
        var sp = LoadSpriteFixed(use);
        if (sp != null) { prop.objectReferenceValue = sp; _skinOk++; }
    }

    /// <summary>Mảng frame TUỲ CHỌN: nạp các frame đang có, thiếu hết → log nhẹ.</summary>
    private static void SArrOpt(SerializedObject so, string field, string pathFmt, int count)
    {
        var prop = so.FindProperty(field);
        if (prop == null) return;
        var found = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < count; i++)
        {
            var fp = string.Format(pathFmt, i + 1);
            if (!System.IO.File.Exists(fp)) continue;
            var sp = LoadSpriteFixed(fp);
            if (sp != null) found.Add(sp);
        }
        prop.arraySize = found.Count;
        for (int i = 0; i < found.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
        if (found.Count == 0) Debug.Log($"[KitchenV2] (chờ art) {field} chưa có frame nào — bỏ qua.");
        else _skinOk += found.Count;
    }
}
