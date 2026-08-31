using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

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
    private const float WaypointSpacing = 260f;  // giữa 2 WP
    private const float QueueExtra      = 240f;  // từ WP cuối tới hàng chờ (khi không thấy nhà hàng)

    // ── CỠ TẤM GỖ (unit world) ──────────────────────────────────────────
    // [BUG Sếp gặp lúc Play test 2026-08-29] Bản đầu để 190x46 và gán sprite
    // WoodBoard_Frame (512px, PPU 100 = 5.12 unit) — nhưng ApplySpriteSize chỉ chạy khi
    // sprite có border; sprite không border thì phóng bằng scale, và với cỡ mục tiêu quá
    // nhỏ so với map (3 bến cách nhau ~740 unit, khách cao 170 unit) thì tấm ván chỉ là
    // một chấm gần như vô hình. Nay canh theo world size THẬT như cách làm nhân vật.
    /// <summary>Chiều DÀI tấm gỗ (unit world) — đủ nối từ mạn tàu vào bờ.</summary>
    public const float GangplankWorldLength = 420f;

    /// <summary>Chiều DÀY tấm gỗ (unit world) — đủ rộng cho 1 khách cao 170 unit đi qua.</summary>
    public const float GangplankWorldThickness = 90f;

    /// <summary>
    /// Khoảng cách từ Berth tới TÂM tấm gỗ = nửa chiều dài ⇒ tấm gỗ bắt đầu ĐÚNG tại
    /// mạn tàu và kéo dài trọn 420 unit vào bờ (trả lời câu hỏi "lệch 110 unit" của Lead:
    /// 110 là số cũ, quá ngắn nên ván không chạm được cả hai đầu).
    /// </summary>
    public const float GangplankDistance = GangplankWorldLength * 0.5f;

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
        var duDungMocThang = new bool[DockTotal]; // path nào còn đang là mốc thẳng dự phòng

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
                // Layer THẬT của project (Objects) — dưới khách (ObjectsFront), trên mặt nước.
                sr.sortingLayerName = TouristSortingLayers.Resolve(TouristSortingLayers.Gangplank);
                sr.sortingOrder     = 900;
                ApplySpriteSize(sr, new Vector2(GangplankWorldLength, GangplankWorldThickness));

                // Quay tấm gỗ theo hướng bến → bờ cho hợp mắt.
                go.transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

                Undo.AddComponent<GangplankController>(go);
                gp = go.transform;
                Note(added, ref addedCount,
                     $"Dock_{i + 1:00}/Gangplank {GangplankWorldLength:0}x{GangplankWorldThickness:0} unit ({woodInfo})");
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

                // Ưu tiên BÁM ĐƯỜNG ĐẤT thật; đích tạm là QueueAnchor nếu đã có, không thì
                // hướng đất liền. (Tool ★ chạy bước 4 sau bước này nên sẽ ghi lại lần nữa
                // với QueueAnchor chính xác — ở đây chỉ để tool con dùng độc lập cũng đúng.)
                Transform anchorCo = root.transform.Find("QueueAnchor");
                Vector3 dauVan = gp.position;
                var gpSr = gp.GetComponent<SpriteRenderer>();
                if (gpSr != null && gpSr.sprite != null)
                    dauVan += Vector3.up * (gpSr.bounds.size.y * 0.5f);

                KetQuaTimDuong kqDuong = null;
                if (BamDuongDat && anchorCo != null)
                    kqDuong = TimDuongBamDat(dauVan, anchorCo.position);

                if (kqDuong != null && kqDuong.ThanhCong)
                {
                    for (int k = 0; k < kqDuong.Waypoints.Count; k++)
                        CreateGO($"WP_{k + 1:00}", path, kqDuong.Waypoints[k]);

                    Note(added, ref addedCount, $"{pathName} — BÁM ĐƯỜNG ĐẤT: {kqDuong.MoTaNgan()}");
                    if (kqDuong.TiLeCo > 0.4f)
                        canLam.AppendLine($"• {pathName}: {kqDuong.TiLeCo * 100f:0}% quãng đường đi trên CỎ — " +
                                          "kiểm lại Tilemap_IsoDirt xem đường đất có nối tới nhà hàng chưa.");
                }
                else
                {
                    for (int k = 1; k <= DefaultWaypoints; k++)
                        CreateGO($"WP_{k:00}", path, gp.position + dir * (WaypointSpacing * k));

                    string lyDo = kqDuong != null ? kqDuong.LyDoThatBai
                                : !BamDuongDat ? "cờ bám đường đất đang TẮT"
                                : "chưa có QueueAnchor để làm đích";
                    duDungMocThang[i] = true;
                    Note(added, ref addedCount, $"{pathName} ({DefaultWaypoints} WP thẳng dự phòng — {lyDo})");
                }
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

        // ── Lượt 2: path nào vừa phải dùng mốc thẳng vì CHƯA có QueueAnchor thì bám lại ──
        // (QueueAnchor được tạo SAU vòng lặp bến, nên lần đầu chạy tool chưa có đích để tìm đường.)
        if (BamDuongDat && queueAnchor != null)
        {
            for (int i = 0; i < DockTotal; i++)
            {
                if (!duDungMocThang[i] || pathRoots[i] == null || gangplanks[i] == null) continue;

                Vector3 dauVan = gangplanks[i].position;
                var gsr = gangplanks[i].GetComponent<SpriteRenderer>();
                if (gsr != null && gsr.sprite != null)
                    dauVan += Vector3.up * (gsr.bounds.size.y * 0.5f);

                KetQuaTimDuong kq = TimDuongBamDat(dauVan, queueAnchor.position);
                if (!kq.ThanhCong)
                {
                    canLam.AppendLine($"• Kéo WP của TouristPath_Dock{i + 1:00} bám theo đường đất đã vẽ (REVIEW) " +
                                      $"— tool chưa bám tự động được: {kq.LyDoThatBai}.");
                    continue;
                }

                Transform path = pathRoots[i];
                for (int c = path.childCount - 1; c >= 0; c--)
                    Undo.DestroyObjectImmediate(path.GetChild(c).gameObject);

                for (int k = 0; k < kq.Waypoints.Count; k++)
                    CreateGO($"WP_{k + 1:00}", path, kq.Waypoints[k]);

                Note(added, ref addedCount, $"TouristPath_Dock{i + 1:00} — BÁM ĐƯỜNG ĐẤT (lượt 2): {kq.MoTaNgan()}");
                if (kq.TiLeCo > 0.4f)
                    canLam.AppendLine($"• TouristPath_Dock{i + 1:00}: {kq.TiLeCo * 100f:0}% quãng đường đi trên CỎ — " +
                                      "kiểm lại Tilemap_IsoDirt xem đường đất có nối tới nhà hàng chưa.");
            }
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
                var boatMgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
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

    private static void SetRefIfEmpty(SerializedObject so, string propName, UnityEngine.Object value)
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

        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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

    // ═════════════════════════════════════════════════════════════════════
    //  TÌM ĐƯỜNG BÁM ĐƯỜNG ĐẤT THẬT (Sếp: "đi theo cái đường đất tôi đã vẽ")
    // ═════════════════════════════════════════════════════════════════════
    //
    // Bản trước đặt 3 mốc trên ĐƯỜNG THẲNG bến → nhà hàng nên khách đi xuyên qua cỏ.
    // Nay: Dijkstra 8 hướng trên chính lưới ô của tilemap, chi phí theo loại mặt đất
    // (đất rẻ nhất) ⇒ đường tự bám con đường đất Sếp vẽ; rồi rút gọn bằng
    // Douglas-Peucker xuống 4-7 waypoint cho Sếp còn kéo tinh chỉnh được.
    //
    // Cỏ KHÔNG bị chặn, chỉ đắt — nhờ vậy không bao giờ "không tìm được đường" khiến
    // khách đứng im; nếu đường đất chưa nối tới nhà hàng thì đường đi sẽ nhiều ô cỏ và
    // tool CẢNH BÁO để Sếp kiểm lại tilemap.

    /// <summary>Chi phí đi vào 1 ô theo loại mặt đất (Lead chốt 2026-08-29).</summary>
    private const int ChiPhiDat    = 1;
    private const int ChiPhiCauTau = 2;
    private const int ChiPhiCat    = 5;
    private const int ChiPhiCo     = 9;   // không chặn, chỉ đắt

    /// <summary>Dung sai rút gọn Douglas-Peucker (unit world) — điểm khởi đầu, tool tự nới nếu ra quá nhiều WP.</summary>
    private const float DungSaiRutGon = 200f;

    private const int SoWpToiThieu = 4;
    private const int SoWpToiDa    = 7;

    /// <summary>Trần số ô quét — chặn trường hợp tilemap khổng lồ làm Editor treo.</summary>
    private const int TranSoO = 250000;

    /// <summary>Khoá EditorPrefs cho cờ bật/tắt tính năng bám đường đất.</summary>
    private const string KhoaBamDuongDat = "TouristBoat_BamDuongDat";

    /// <summary>
    /// Cờ Sếp bật/tắt: TRUE = tool tự tìm đường bám tilemap đất; FALSE = giữ 3 mốc thẳng
    /// (dùng khi Sếp đã kéo tay waypoint và không muốn bị ghi đè).
    /// Đổi bằng menu "Bám đường đất khi setup (bật/tắt)".
    /// </summary>
    public static bool BamDuongDat
    {
        get { return EditorPrefs.GetBool(KhoaBamDuongDat, true); }
        set { EditorPrefs.SetBool(KhoaBamDuongDat, value); }
    }

    [MenuItem(MenuRoot + "⚙ Bám đường đất khi setup (bật/tắt)", false, 23)]
    private static void ToggleBamDuongDat()
    {
        BamDuongDat = !BamDuongDat;
        Debug.Log("[TouristVisitor] Bám đường đất khi setup: " + (BamDuongDat ? "BẬT" : "TẮT (dùng 3 mốc thẳng)"));
    }

    [MenuItem(MenuRoot + "⚙ Bám đường đất khi setup (bật/tắt)", true)]
    private static bool ToggleBamDuongDatValidate()
    {
        Menu.SetChecked(MenuRoot + "⚙ Bám đường đất khi setup (bật/tắt)", BamDuongDat);
        return true;
    }

    /// <summary>Kết quả một lần tìm đường — dùng cho cả việc ghi waypoint lẫn in report.</summary>
    public class KetQuaTimDuong
    {
        /// <summary>Waypoint đã rút gọn (KHÔNG gồm điểm đầu và điểm đích).</summary>
        public List<Vector3> Waypoints = new List<Vector3>();

        public int   ODat, OCauTau, OCat, OCo;
        public float TongDaiUnit;

        /// <summary>null/rỗng = thành công. Có nội dung = lý do phải rơi về 3 mốc thẳng.</summary>
        public string LyDoThatBai;

        public bool ThanhCong => string.IsNullOrEmpty(LyDoThatBai) && Waypoints.Count > 0;
        public int  TongO     => ODat + OCauTau + OCat + OCo;
        public float TiLeCo   => TongO > 0 ? (float)OCo / TongO : 0f;

        public string MoTaNgan()
        {
            if (!ThanhCong) return "thất bại: " + LyDoThatBai;
            return $"{Waypoints.Count} WP · {TongO} ô (đất {ODat} · cầu tàu {OCauTau} · cát {OCat} · cỏ {OCo}" +
                   $" = {TiLeCo * 100f:0}% cỏ) · dài {TongDaiUnit:0} unit";
        }
    }

    /// <summary>
    /// Tìm đường bám đường đất từ <paramref name="batDau"/> tới <paramref name="dich"/>.
    /// Không bao giờ ném exception — thất bại thì trả kết quả có <c>LyDoThatBai</c>.
    /// </summary>
    public static KetQuaTimDuong TimDuongBamDat(Vector3 batDau, Vector3 dich)
    {
        var kq = new KetQuaTimDuong();

        try
        {
            Tilemap dat = null, cauTau = null, cat = null;
            foreach (Tilemap tm in UnityEngine.Object.FindObjectsByType<Tilemap>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tm == null) continue;
                string ten = tm.gameObject.name.ToLowerInvariant();
                if (dat    == null && ten.Contains("isodirt")) dat    = tm;
                if (cauTau == null && ten.Contains("isodock")) cauTau = tm;
                if (cat    == null && ten.Contains("isosand")) cat    = tm;
            }

            if (dat == null)
            {
                kq.LyDoThatBai = "không thấy tilemap tên chứa 'IsoDirt' trong scene";
                return kq;
            }

            // Ô của điểm đầu/đích — quy chiếu theo lưới của tilemap đất.
            Vector3Int oDau  = dat.WorldToCell(batDau);
            Vector3Int oDich = dat.WorldToCell(dich);

            // Vùng quét = bao của các tilemap + điểm đầu/đích, nới 8 ô cho có đường lách.
            BoundsInt vung = GopVung(dat, cauTau, cat, oDau, oDich, 8);
            long soO = (long)vung.size.x * vung.size.y;
            if (soO <= 0 || soO > TranSoO)
            {
                kq.LyDoThatBai = $"vùng quét không hợp lệ ({vung.size.x}x{vung.size.y} ô)";
                return kq;
            }

            // ── Bảng chi phí ──
            int w = vung.size.x, h = vung.size.y;
            var chiPhi = new int[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var o = new Vector3Int(vung.xMin + x, vung.yMin + y, 0);
                    int c = ChiPhiCo;                                    // mặc định: cỏ / trống
                    if (cat    != null && cat.HasTile(o))    c = ChiPhiCat;
                    if (cauTau != null && cauTau.HasTile(o)) c = ChiPhiCauTau;
                    if (dat.HasTile(o))                      c = ChiPhiDat; // đất thắng tất cả
                    chiPhi[y * w + x] = c;
                }
            }

            List<Vector3Int> duong = Dijkstra(chiPhi, w, h, vung, oDau, oDich);
            if (duong == null || duong.Count < 2)
            {
                kq.LyDoThatBai = "không nối được ô đầu tới ô đích trong vùng quét";
                return kq;
            }

            // ── Đếm loại ô + đổi sang world ──
            var diem = new List<Vector3>(duong.Count);
            for (int i = 0; i < duong.Count; i++)
            {
                Vector3Int o = duong[i];
                int c = chiPhi[(o.y - vung.yMin) * w + (o.x - vung.xMin)];
                if      (c == ChiPhiDat)    kq.ODat++;
                else if (c == ChiPhiCauTau) kq.OCauTau++;
                else if (c == ChiPhiCat)    kq.OCat++;
                else                        kq.OCo++;

                // GetCellCenterWorld tự lo layout Isometric + cellSize + scale cha —
                // cho ra đúng công thức ((cx-cy)*0.5*S, (cx+cy)*0.25*S) mà Lead đã kiểm.
                Vector3 wpos = dat.GetCellCenterWorld(o);
                wpos.z = 0f;
                diem.Add(wpos);
            }

            for (int i = 1; i < diem.Count; i++)
                kq.TongDaiUnit += Vector3.Distance(diem[i - 1], diem[i]);

            // ── Rút gọn về 4..7 waypoint sẽ ghi ──
            List<int> giu = RutGonVeKhoang(diem, SoWpToiThieu, SoWpToiDa);

            // Bỏ điểm ĐẦU (manager tự thêm đầu tấm gỗ) và điểm CUỐI (khách tự đi vào slot hàng chờ).
            for (int i = 1; i < giu.Count - 1; i++)
                kq.Waypoints.Add(diem[giu[i]]);

            if (kq.Waypoints.Count == 0)
            {
                kq.LyDoThatBai = "đường quá ngắn, rút gọn xong không còn waypoint trung gian nào";
                return kq;
            }

            return kq;
        }
        catch (Exception e)
        {
            kq.LyDoThatBai = "lỗi khi tìm đường: " + e.Message;
            Debug.LogWarning("[TouristVisitor] TimDuongBamDat: " + e);
            return kq;
        }
    }

    /// <summary>Bao chung của các tilemap + 2 ô mốc, nới thêm <paramref name="noi"/> ô mỗi phía.</summary>
    private static BoundsInt GopVung(Tilemap dat, Tilemap cauTau, Tilemap cat,
                                     Vector3Int oDau, Vector3Int oDich, int noi)
    {
        BoundsInt b = dat.cellBounds;
        int xMin = b.xMin, xMax = b.xMax, yMin = b.yMin, yMax = b.yMax;

        if (cauTau != null)
        {
            BoundsInt c = cauTau.cellBounds;
            xMin = Mathf.Min(xMin, c.xMin); xMax = Mathf.Max(xMax, c.xMax);
            yMin = Mathf.Min(yMin, c.yMin); yMax = Mathf.Max(yMax, c.yMax);
        }
        if (cat != null)
        {
            BoundsInt c = cat.cellBounds;
            xMin = Mathf.Min(xMin, c.xMin); xMax = Mathf.Max(xMax, c.xMax);
            yMin = Mathf.Min(yMin, c.yMin); yMax = Mathf.Max(yMax, c.yMax);
        }

        xMin = Mathf.Min(xMin, Mathf.Min(oDau.x, oDich.x));
        xMax = Mathf.Max(xMax, Mathf.Max(oDau.x, oDich.x) + 1);
        yMin = Mathf.Min(yMin, Mathf.Min(oDau.y, oDich.y));
        yMax = Mathf.Max(yMax, Mathf.Max(oDau.y, oDich.y) + 1);

        xMin -= noi; yMin -= noi; xMax += noi; yMax += noi;
        return new BoundsInt(xMin, yMin, 0, xMax - xMin, yMax - yMin, 1);
    }

    /// <summary>
    /// Dijkstra 8 hướng trên lưới ô (mọi ô đều đi được, chỉ khác giá) — đường đi rẻ nhất
    /// tự bám dải ô đất. Đường chéo nhân 1.41 để không "ăn gian" khoảng cách.
    /// Trả danh sách ô từ đầu tới đích, null nếu không tới được.
    /// </summary>
    private static List<Vector3Int> Dijkstra(int[] chiPhi, int w, int h, BoundsInt vung,
                                             Vector3Int oDau, Vector3Int oDich)
    {
        int iDau  = ChiSo(oDau,  vung, w, h);
        int iDich = ChiSo(oDich, vung, w, h);
        if (iDau < 0 || iDich < 0) return null;

        int n = w * h;
        var dist = new float[n];
        var truoc = new int[n];
        var xong  = new bool[n];
        for (int i = 0; i < n; i++) { dist[i] = float.MaxValue; truoc[i] = -1; }
        dist[iDau] = 0f;

        // Heap nhị phân tối giản (không dùng SortedSet để tránh cấp phát nhiều).
        var heap = new List<int>(256) { iDau };
        var uuTien = new List<float>(256) { 0f };

        int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        while (heap.Count > 0)
        {
            // Lấy phần tử nhỏ nhất (quét tuyến tính — vùng quét nhỏ, đủ nhanh cho Editor).
            int best = 0;
            for (int i = 1; i < heap.Count; i++) if (uuTien[i] < uuTien[best]) best = i;
            int cur = heap[best];
            heap.RemoveAt(best); uuTien.RemoveAt(best);

            if (xong[cur]) continue;
            xong[cur] = true;
            if (cur == iDich) break;

            int cx = cur % w, cy = cur / w;
            for (int k = 0; k < 8; k++)
            {
                int nx = cx + dx[k], ny = cy + dy[k];
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

                int next = ny * w + nx;
                if (xong[next]) continue;

                float buoc = chiPhi[next] * (k >= 4 ? 1.41f : 1f);
                float mo = dist[cur] + buoc;
                if (mo >= dist[next]) continue;

                dist[next] = mo;
                truoc[next] = cur;
                heap.Add(next); uuTien.Add(mo);
            }
        }

        if (truoc[iDich] < 0 && iDich != iDau) return null;

        var ds = new List<Vector3Int>();
        for (int i = iDich; i >= 0; i = truoc[i])
        {
            ds.Add(new Vector3Int(vung.xMin + (i % w), vung.yMin + (i / w), 0));
            if (i == iDau) break;
        }
        ds.Reverse();
        return ds;
    }

    private static int ChiSo(Vector3Int o, BoundsInt vung, int w, int h)
    {
        int x = o.x - vung.xMin, y = o.y - vung.yMin;
        if (x < 0 || y < 0 || x >= w || y >= h) return -1;
        return y * w + x;
    }

    /// <summary>
    /// Rút gọn polyline rồi bảo đảm SỐ WAYPOINT SẼ GHI nằm trong [min..max].
    /// (Số waypoint ghi = số điểm giữ − 2, vì điểm đầu và điểm cuối bị bỏ: điểm đầu do
    /// manager tự thêm là đầu tấm gỗ, điểm cuối là hàng chờ khách tự đi tới.)
    ///
    /// Ba bước:
    ///   ① Douglas-Peucker với dung sai khởi điểm;
    ///   ② quá NHIỀU điểm → nới dung sai (×1.6); quá ÍT → siết (÷1.6);
    ///   ③ vẫn quá ít (đường đất gần như thẳng / hình L thì DP đúng ra chỉ còn 3-4 điểm)
    ///      → CHÈN THÊM điểm lấy từ CHÍNH đường đã truy, chia đôi đoạn dài nhất.
    ///      Điểm chèn vẫn nằm trên ô đất nên khách không rời đường; chỉ là Sếp có thêm
    ///      mốc để kéo tinh chỉnh.
    /// Trả về danh sách CHỈ SỐ trong <paramref name="diem"/> (đã sắp tăng).
    /// </summary>
    private static List<int> RutGonVeKhoang(List<Vector3> diem, int minGhi, int maxGhi)
    {
        int muonToiThieu = minGhi + 2;
        int muonToiDa    = maxGhi + 2;

        float dungSai = DungSaiRutGon;
        List<int> giu = DouglasPeucker(diem, dungSai);

        for (int lan = 0; lan < 12 && giu.Count > muonToiDa; lan++)
        {
            dungSai *= 1.6f;
            giu = DouglasPeucker(diem, dungSai);
        }
        for (int lan = 0; lan < 12 && giu.Count < muonToiThieu && dungSai > 10f; lan++)
        {
            dungSai /= 1.6f;
            giu = DouglasPeucker(diem, dungSai);
        }

        ChenThemDiemTrenDuong(diem, giu, muonToiThieu);
        return giu;
    }

    /// <summary>
    /// Chèn thêm mốc cho tới khi đủ <paramref name="soDiemMuon"/>: mỗi lần tìm khoảng
    /// TRỐNG DÀI NHẤT giữa 2 mốc liền kề (đo bằng số ô của đường gốc) rồi lấy ô giữa.
    /// Không còn ô nào chèn được thì dừng — đường quá ngắn thì ít mốc là ĐÚNG, không bịa thêm.
    /// </summary>
    private static void ChenThemDiemTrenDuong(List<Vector3> diem, List<int> giu, int soDiemMuon)
    {
        while (giu.Count < soDiemMuon)
        {
            int viTriChen = -1, khoangRong = 1;
            for (int i = 1; i < giu.Count; i++)
            {
                int rong = giu[i] - giu[i - 1];
                if (rong > khoangRong) { khoangRong = rong; viTriChen = i; }
            }
            if (viTriChen < 0 || khoangRong < 2) return; // hết chỗ chèn

            int giua = giu[viTriChen - 1] + khoangRong / 2;
            giu.Insert(viTriChen, giua);
        }
    }

    /// <summary>
    /// Douglas-Peucker kinh điển (đệ quy) — trả về CHỈ SỐ các điểm được giữ, luôn gồm 2 đầu.
    /// </summary>
    private static List<int> DouglasPeucker(List<Vector3> diem, float dungSai)
    {
        var kq = new List<int>();
        if (diem == null || diem.Count == 0) return kq;
        if (diem.Count < 3)
        {
            for (int i = 0; i < diem.Count; i++) kq.Add(i);
            return kq;
        }

        var giu = new bool[diem.Count];
        giu[0] = giu[diem.Count - 1] = true;
        DPDeQuy(diem, 0, diem.Count - 1, dungSai, giu);

        for (int i = 0; i < diem.Count; i++) if (giu[i]) kq.Add(i);
        return kq;
    }

    private static void DPDeQuy(List<Vector3> d, int dau, int cuoi, float dungSai, bool[] giu)
    {
        if (cuoi <= dau + 1) return;

        float xaNhat = -1f;
        int iXa = -1;
        for (int i = dau + 1; i < cuoi; i++)
        {
            float kc = KhoangCachToiDoanThang(d[i], d[dau], d[cuoi]);
            if (kc > xaNhat) { xaNhat = kc; iXa = i; }
        }

        if (xaNhat <= dungSai || iXa < 0) return;

        giu[iXa] = true;
        DPDeQuy(d, dau, iXa, dungSai, giu);
        DPDeQuy(d, iXa, cuoi, dungSai, giu);
    }

    private static float KhoangCachToiDoanThang(Vector3 p, Vector3 a, Vector3 b)
    {
        float abx = b.x - a.x, aby = b.y - a.y;
        float len2 = abx * abx + aby * aby;
        if (len2 < 0.0001f)
        {
            float ddx = p.x - a.x, ddy = p.y - a.y;
            return Mathf.Sqrt(ddx * ddx + ddy * ddy);
        }
        float t = Mathf.Clamp01(((p.x - a.x) * abx + (p.y - a.y) * aby) / len2);
        float qx = a.x + abx * t, qy = a.y + aby * t;
        float dx = p.x - qx, dy = p.y - qy;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers (copy pattern TouristBoatSetupTool)
    // ─────────────────────────────────────────────────────────────────────

    private static GameObject FindBoatSystem()
    {
        var mgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        if (mgr != null) return mgr.gameObject;

        foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(
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
            // Sprite 9-slice: set size trực tiếp, scale giữ 1.
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size     = size;
        }
        else
        {
            // Sprite KHÔNG border: Sliced/size vô nghĩa → phóng bằng localScale.
            // Đây chính là đường mà WoodBoard_Frame đi qua (512px, PPU 100 = 5.12 unit):
            // phải scale ~82 lần mới ra 420 unit. Bản đầu đặt size mục tiêu quá nhỏ nên
            // tấm ván gần như vô hình trên map toạ độ lớn.
            sr.drawMode = SpriteDrawMode.Simple;
            Vector2 native = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            sr.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        }
    }
}
