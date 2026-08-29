using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Tourist Boat/Setup Tourist Visitors (Scene) — BOAT-002 §3.3.
/// Pattern bắt chước TouristBoatSetupTool: find-or-create từng mảnh, IDEMPOTENT
/// (không phá vị trí Sếp đã kéo), Undo được toàn bộ, kết thúc bằng report + ping.
///
/// DỰNG GÌ TRONG SCENE:
///   TouristSystem                         ← root mới (không đụng BoatSystem của Dev A)
///   ├─ TouristVisitorManager (component)  ← wire sẵn config · roster prefab · database món
///   ├─ QueueAnchor (TouristQueue)         ← hàng chờ trước nhà hàng cooking
///   ├─ TouristPath_Dock01/02/03           ← mỗi cái 4 WP mặc định nối Berth → hướng đất liền
///   └─ Visitors                           ← node cha cho khách spawn lúc chạy
///   BoatSystem/Dock_0X/Gangplank          ← tấm gỗ (SpriteRenderer + GangplankController)
///
/// NHỮNG THỨ TOOL KHÔNG ĐOÁN ĐƯỢC (đều LOG RÕ "CẦN SẾP…"):
///   • Đường đất thật: WP sinh mặc định trên đường THẲNG từ bến vào bờ — Sếp kéo lại
///     cho khớp con đường đã vẽ (đánh dấu REVIEW).
///   • Vị trí nhà hàng cooking: tool dò object tên chứa "Cooking"/"NhaHang"/"Restaurant";
///     không thấy thì đặt QueueAnchor ở cuối path + ghi cảnh báo cần kéo tay.
///   • Art tấm gỗ: dò sprite tên chứa "wood"/"plank"/"go_"; không có thì dùng
///     placeholder nâu (logic gangplank vẫn chạy đủ).
/// </summary>
public static class TouristVisitorSetupTool
{
    private const string MenuRoot   = "Tools/Farm Game/Tourist Boat/";
    private const string MenuSetup  = MenuRoot + "Setup Tourist Visitors (Scene)";
    private const string MenuDelete = MenuRoot + "Xóa Tourist Visitors (Undo)";

    private const string RootName   = "TouristSystem";
    private const string UndoLabel  = "Tourist Visitor Setup";

    private const string BoatRootName = "BoatSystem";
    private const string ConfigPath   = "Assets/_Game/ScriptableObjects/TouristBoatConfig.asset";
    private const string PrefabRoot   = "Assets/_Game/Farm/Prefabs/Tourists";

    private const int DockTotal        = 3;   // = BoatDockManager.DockCount
    // 3 WP mỗi bến — khớp bảng toạ độ thật mà Lead trích từ SCN_Farm (tool 1 nút ghi đè
    // đúng 3 điểm này). Bản đầu để 4 khi chưa có toạ độ thật.
    private const int DefaultWaypoints = 3;

    // Khoảng cách MẶC ĐỊNH giữa các mốc (unit world). Map dùng toạ độ rất lớn
    // (3 bến cách nhau ~740 unit) nên số nhỏ kiểu 1-2 unit là vô nghĩa ở đây.
    private const float GangplankDistance = 110f;  // từ Berth vào bờ
    private const float WaypointSpacing   = 260f;  // giữa 2 WP
    private const float QueueExtra        = 240f;  // từ WP cuối tới hàng chờ (khi không thấy nhà hàng)

    private const float GangplankWidth  = 190f;
    private const float GangplankHeight = 46f;

    // ─────────────────────────────────────────────────────────────────────
    //  SETUP
    // ─────────────────────────────────────────────────────────────────────

    [MenuItem(MenuSetup, false, 21)]
    public static void SetupAll()
    {
        RunSetup(false);
    }

    /// <summary>
    /// Lõi chạy được từ ngoài (TouristBoatOneClickSetup gọi trực tiếp — KHÔNG dùng
    /// ExecuteMenuItem vì nó không chờ và không bắt được lỗi).
    /// <paramref name="quiet"/> = true: không bật dialog riêng, không đổi Selection.
    /// Trả về report dạng text.
    /// </summary>
    public static string RunSetup(bool quiet)
    {
        GameObject boatRoot = FindBoatSystem();
        if (boatRoot == null)
        {
            string loi = "Không tìm thấy \"" + BoatRootName + "\" trong scene đang mở. " +
                         "Chạy Tools/Farm Game/Tourist Boat/1. Setup All (Scene + Config) trước.";
            Debug.LogError("[TouristVisitor] " + loi);
            if (!quiet) EditorUtility.DisplayDialog("Tourist Visitors", loi, "OK");
            return loi;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        var added   = new StringBuilder();
        var canLam  = new StringBuilder();
        int addedCount = 0;

        // ── Root TouristSystem ──────────────────────────────────────────
        GameObject root = GameObject.Find(RootName);
        bool rootExisted = root != null;
        if (root == null)
        {
            root = CreateGO(RootName, null, Vector3.zero);
            Note(added, ref addedCount, RootName + " (gốc hệ khách du lịch)");
        }

        var manager = root.GetComponent<TouristVisitorManager>();
        if (manager == null)
        {
            manager = Undo.AddComponent<TouristVisitorManager>(root);
            Note(added, ref addedCount, "component TouristVisitorManager");
        }

        Transform visitorsRoot = root.transform.Find("Visitors");
        if (visitorsRoot == null)
        {
            visitorsRoot = CreateGO("Visitors", root.transform, root.transform.position).transform;
            Note(added, ref addedCount, "Visitors (node cha khách spawn lúc chạy)");
        }

        // ── Gangplank + path từng bến ───────────────────────────────────
        Transform blind = boatRoot.transform.Find("BlindPoint");
        var gangplanks  = new Transform[DockTotal];
        var pathRoots   = new Transform[DockTotal];
        Vector3 huongBo = Vector3.up;   // hướng "vào đất liền" trung bình, để đặt QueueAnchor
        int huongDem    = 0;

        Sprite woodSprite = FindWoodSprite(out string woodInfo);

        for (int i = 0; i < DockTotal; i++)
        {
            Transform dock = boatRoot.transform.Find($"Dock_{i + 1:00}");
            if (dock == null)
            {
                canLam.AppendLine($"• Thiếu Dock_{i + 1:00} trong BoatSystem — bến {i + 1} chưa có gangplank/đường đi.");
                continue;
            }

            Transform berth = dock.Find("Berth");
            Vector3 berthPos = berth != null ? berth.position : dock.position;

            // Hướng vào bờ = NGƯỢC hướng ra điểm mù (điểm mù nằm ngoài khơi).
            Vector3 dir = blind != null ? (berthPos - blind.position) : Vector3.up;
            dir.z = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;
            huongBo += dir; huongDem++;

            // Gangplank — con của Dock (bật/tắt theo tàu, xoay theo hướng vào bờ)
            Transform gp = dock.Find("Gangplank");
            if (gp == null)
            {
                GameObject go = CreateGO("Gangplank", dock, berthPos + dir * GangplankDistance);
                var sr = Undo.AddComponent<SpriteRenderer>(go);
                sr.sprite = woodSprite;
                sr.color  = woodSprite != null ? Color.white : new Color(0.55f, 0.38f, 0.20f, 1f);
                sr.sortingLayerName = "CongTrinh";
                sr.sortingOrder     = 900; // trên mặt nước, dưới khách
                ApplySpriteSize(sr, new Vector2(GangplankWidth, GangplankHeight));

                // Quay tấm gỗ theo hướng bến → bờ cho hợp mắt.
                go.transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

                Undo.AddComponent<GangplankController>(go);
                gp = go.transform;
                Note(added, ref addedCount, $"Dock_{i + 1:00}/Gangplank ({woodInfo})");
            }
            else if (gp.GetComponent<GangplankController>() == null)
            {
                Undo.AddComponent<GangplankController>(gp.gameObject);
                Note(added, ref addedCount, $"Dock_{i + 1:00}/Gangplank: component GangplankController");
            }
            gangplanks[i] = gp;

            // Đường đi bộ TouristPath_Dock0X (con của TouristSystem để dễ kéo)
            string pathName = $"TouristPath_Dock{i + 1:00}";
            Transform path = root.transform.Find(pathName);
            if (path == null)
            {
                path = CreateGO(pathName, root.transform, gp.position).transform;
                for (int k = 1; k <= DefaultWaypoints; k++)
                    CreateGO($"WP_{k:00}", path, gp.position + dir * (WaypointSpacing * k));

                Note(added, ref addedCount, $"{pathName} ({DefaultWaypoints} WP mặc định — CẦN SẾP KÉO theo đường đất)");
                canLam.AppendLine($"• Kéo WP_01..WP_{DefaultWaypoints:00} của {pathName} bám theo con đường đất đã vẽ (REVIEW).");
            }
            pathRoots[i] = path;
        }

        if (huongDem > 0) huongBo = (huongBo / huongDem).normalized;

        // ── QueueAnchor cạnh nhà hàng cooking ───────────────────────────
        Transform queueAnchor = root.transform.Find("QueueAnchor");
        if (queueAnchor == null)
        {
            Vector3 anchorPos;
            GameObject cooking = FindCookingBuilding(out string cookingName);

            if (cooking != null)
            {
                // Đứng NGAY TRƯỚC nhà hàng (thấp hơn tâm một chút để không đè lên mái).
                anchorPos = cooking.transform.position + new Vector3(0f, -160f, 0f);
                canLam.AppendLine($"• QueueAnchor đặt cạnh \"{cookingName}\" — nhìn scene chỉnh lại cho khách đứng đúng trước cửa.");
            }
            else if (pathRoots[0] != null && pathRoots[0].childCount > 0)
            {
                Transform lastWp = pathRoots[0].GetChild(pathRoots[0].childCount - 1);
                anchorPos = lastWp.position + huongBo * QueueExtra;
                canLam.AppendLine("• CẦN SẾP KÉO: không tìm thấy object nhà hàng cooking (tên chứa 'Cooking'/'NhaHang'/'Restaurant') — " +
                                  "QueueAnchor tạm đặt ở cuối đường đi bộ bến 1.");
            }
            else
            {
                anchorPos = Vector3.zero;
                canLam.AppendLine("• CẦN SẾP KÉO: không tìm được cả nhà hàng lẫn đường đi — QueueAnchor đang ở (0,0).");
            }

            GameObject go = CreateGO("QueueAnchor", root.transform, anchorPos);
            Undo.AddComponent<TouristQueue>(go);
            queueAnchor = go.transform;
            Note(added, ref addedCount, "QueueAnchor (TouristQueue — chỗ khách đầu hàng đứng)");
        }
        else if (queueAnchor.GetComponent<TouristQueue>() == null)
        {
            Undo.AddComponent<TouristQueue>(queueAnchor.gameObject);
            Note(added, ref addedCount, "QueueAnchor: component TouristQueue");
        }

        // ── Wire field cho manager (chỉ điền chỗ TRỐNG) ─────────────────
        string wireReport = WireManager(manager, queueAnchor, visitorsRoot, pathRoots, gangplanks);

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        if (!quiet)
        {
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        // ── REPORT ──────────────────────────────────────────────────────
        var report = new StringBuilder();
        report.AppendLine(rootExisted
            ? "TouristSystem đã có — GIỮ NGUYÊN vị trí bạn đã kéo, chỉ bổ sung phần thiếu."
            : "ĐÃ TẠO MỚI hệ khách du lịch.");
        if (addedCount > 0) { report.AppendLine("Bổ sung:"); report.Append(added); }
        else                  report.AppendLine("Không thiếu gì — scene không đổi.");
        report.AppendLine();
        report.AppendLine("WIRE TỰ ĐỘNG:");
        report.Append(wireReport);
        report.AppendLine();
        report.AppendLine("CẦN SẾP LÀM TRONG UNITY:");
        if (canLam.Length == 0) report.AppendLine("• (không có việc nào bắt buộc)");
        else                    report.Append(canLam);
        report.AppendLine("• Chạy trước menu \"Setup NPC Animations\" nếu chưa có prefab khách.");
        report.AppendLine("• Ctrl+S lưu scene sau khi chỉnh xong.");
        report.AppendLine();
        report.AppendLine("(Ctrl+Z hoàn tác toàn bộ phần scene vừa tạo.)");

        Debug.Log("[TouristVisitor] Setup scene:\n" + report);
        if (!quiet)
            EditorUtility.DisplayDialog("Tourist Visitors — Setup xong", report.ToString(), "OK");

        return report.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  XÓA
    // ─────────────────────────────────────────────────────────────────────

    [MenuItem(MenuDelete, false, 22)]
    public static void DeleteSetup()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Tourist Visitors",
                "Không thấy " + RootName + " trong scene — không có gì để xóa.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Tourist Visitors — Xóa",
                "Xóa \"" + RootName + "\" khỏi scene?\n\n" +
                "• Gangplank nằm dưới BoatSystem/Dock_XX KHÔNG bị xóa (xóa tay nếu cần).\n" +
                "• Ctrl+Z hoàn tác được.", "Xóa", "Hủy"))
            return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tourist Visitor Delete");
        Undo.DestroyObjectImmediate(root);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        Debug.Log("[TouristVisitor] Đã xóa " + RootName + " (Ctrl+Z để hoàn tác).");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  WIRE MANAGER
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gán các field serialize của TouristVisitorManager — CHỈ điền field đang trống,
    /// không đè lựa chọn tay của Sếp (cùng luật WireSlot của TouristBoatSetupTool).
    /// </summary>
    private static string WireManager(TouristVisitorManager manager, Transform queueAnchor,
                                      Transform visitorsRoot, Transform[] pathRoots, Transform[] gangplanks)
    {
        var sb = new SerializedObject(manager);
        var report = new StringBuilder();

        // Config: ưu tiên asset chuẩn, fallback config đang gắn trên BoatDockManager
        var cfgProp = sb.FindProperty("config");
        if (cfgProp != null && cfgProp.objectReferenceValue == null)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<TouristBoatConfig>(ConfigPath);
            if (cfg == null)
            {
                var boatMgr = Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
                if (boatMgr != null) cfg = boatMgr.Config;
            }
            cfgProp.objectReferenceValue = cfg;
            report.AppendLine(cfg != null
                ? "• Config: " + AssetDatabase.GetAssetPath(cfg)
                : "• Config: KHÔNG THẤY — kéo TouristBoatConfig vào Inspector!");
        }
        else report.AppendLine("• Config: đã gắn từ trước — giữ nguyên.");

        // Roster prefab khách
        var rosterProp = sb.FindProperty("touristPrefabs");
        if (rosterProp != null && rosterProp.arraySize == 0)
        {
            List<GameObject> prefabs = LoadTouristPrefabs();
            rosterProp.arraySize = prefabs.Count;
            for (int i = 0; i < prefabs.Count; i++)
                rosterProp.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];

            report.AppendLine(prefabs.Count > 0
                ? $"• Roster khách: {prefabs.Count} prefab từ {PrefabRoot}"
                : "• Roster khách: TRỐNG — chạy menu \"Setup NPC Animations\" rồi chạy lại menu này!");
        }
        else report.AppendLine("• Roster khách: đã có — giữ nguyên.");

        // Database món ăn
        var dishProp = sb.FindProperty("dishDatabase");
        if (dishProp != null && dishProp.arraySize == 0)
        {
            List<DishData> dishes = LoadAllDishes();
            dishProp.arraySize = dishes.Count;
            for (int i = 0; i < dishes.Count; i++)
                dishProp.GetArrayElementAtIndex(i).objectReferenceValue = dishes[i];

            report.AppendLine(dishes.Count > 0
                ? $"• Database món: {dishes.Count} DishData (quét toàn Project)"
                : "• Database món: KHÔNG THẤY DishData nào — kiểm tra lại Project!");
        }
        else report.AppendLine("• Database món: đã có — giữ nguyên.");

        SetRefIfEmpty(sb, "queue",        queueAnchor != null ? queueAnchor.GetComponent<TouristQueue>() : null);
        SetRefIfEmpty(sb, "visitorsRoot", visitorsRoot);

        SetArrayIfEmpty(sb, "dockPathRoots", pathRoots);
        SetArrayIfEmpty(sb, "gangplanks",    gangplanks);
        report.AppendLine("• QueueAnchor / Visitors / TouristPath_* / Gangplank: đã nối vào manager.");

        sb.ApplyModifiedProperties();
        return report.ToString();
    }

    private static void SetRefIfEmpty(SerializedObject so, string propName, Object value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p == null) return;
        if (p.objectReferenceValue == null) p.objectReferenceValue = value;
    }

    /// <summary>Điền mảng Transform theo index — chỉ ghi vào ô đang trống.</summary>
    private static void SetArrayIfEmpty(SerializedObject so, string propName, Transform[] values)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p == null || !p.isArray) return;

        if (p.arraySize < values.Length) p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty el = p.GetArrayElementAtIndex(i);
            if (el.objectReferenceValue == null) el.objectReferenceValue = values[i];
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Dò asset / object trong project
    // ─────────────────────────────────────────────────────────────────────

    private static List<GameObject> LoadTouristPrefabs()
    {
        var list = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder(PrefabRoot)) return list;

        string[] guids = AssetDatabase.FindAssets("t:Prefab Tourist_NV", new[] { PrefabRoot });
        var paths = new List<string>(guids.Length);
        foreach (string g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
        paths.Sort(string.CompareOrdinal); // Tourist_NV01, NV02, ... đúng thứ tự

        foreach (string p in paths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null && go.GetComponent<TouristAgent>() != null) list.Add(go);
        }
        return list;
    }

    private static List<DishData> LoadAllDishes()
    {
        var list = new List<DishData>();
        string[] guids = AssetDatabase.FindAssets("t:DishData");
        var paths = new List<string>(guids.Length);
        foreach (string g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
        paths.Sort(string.CompareOrdinal);

        foreach (string p in paths)
        {
            var d = AssetDatabase.LoadAssetAtPath<DishData>(p);
            if (d != null && !string.IsNullOrEmpty(d.dishId)) list.Add(d);
        }
        return list;
    }

    /// <summary>
    /// Dò sprite gỗ cho gangplank. Cố ý KHÔNG khớp mù chuỗi "go" (dính vô số tên khác)
    /// mà dùng các mẫu rõ nghĩa. Không thấy → trả null, bên gọi dùng placeholder nâu.
    /// </summary>
    private static Sprite FindWoodSprite(out string info)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite");
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string ten  = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            bool khop = ten.Contains("wood") || ten.Contains("plank") || ten.Contains("khunggo")
                        || ten == "go" || ten.StartsWith("go_") || ten.Contains("_go_")
                        || ten.Contains("tamgo") || ten.Contains("cauvan");
            if (!khop) continue;

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null)
            {
                info = "sprite gỗ: " + System.IO.Path.GetFileName(path);
                return sp;
            }
        }

        info = "CHƯA CÓ ART — dùng placeholder nâu, thay sprite 4 frame khi art về";
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    /// <summary>Dò object nhà hàng cooking trong scene theo tên (không phân biệt hoa thường).</summary>
    private static GameObject FindCookingBuilding(out string foundName)
    {
        string[] hints = { "cooking", "nhahang", "nha_hang", "restaurant", "bep", "kitchen" };

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            string ten = t.name.ToLowerInvariant();
            for (int i = 0; i < hints.Length; i++)
            {
                if (!ten.Contains(hints[i])) continue;
                // Bỏ qua object UI (canvas) — ta cần công trình trong world.
                if (t.GetComponentInParent<Canvas>() != null) continue;
                foundName = t.name;
                return t.gameObject;
            }
        }

        foundName = string.Empty;
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers (copy pattern TouristBoatSetupTool)
    // ─────────────────────────────────────────────────────────────────────

    private static GameObject FindBoatSystem()
    {
        var mgr = Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        if (mgr != null) return mgr.gameObject;

        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.parent == null && t.name == BoatRootName) return t.gameObject;
        }
        return null;
    }

    private static GameObject CreateGO(string name, Transform parent, Vector3 worldPos)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, UndoLabel);
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        return go;
    }

    private static void Note(StringBuilder sb, ref int count, string line)
    {
        sb.AppendLine("   + " + line);
        count++;
    }

    /// <summary>
    /// Đặt kích thước hiển thị theo UNIT WORLD — copy nguyên bài học của
    /// TouristBoatSetupTool.ApplySpriteSize (sprite có border thì set size, không border
    /// thì phóng bằng localScale; luôn reset scale trước để chạy lại không dồn hệ số).
    /// </summary>
    private static void ApplySpriteSize(SpriteRenderer sr, Vector2 size)
    {
        if (sr == null || sr.sprite == null) return;

        Undo.RecordObject(sr, "Sprite size");
        Undo.RecordObject(sr.transform, "Sprite scale");
        sr.transform.localScale = Vector3.one;

        if (sr.sprite.border != Vector4.zero)
        {
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size     = size;
        }
        else
        {
            sr.drawMode = SpriteDrawMode.Simple;
            Vector2 native = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            sr.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        }
    }
}
