using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Tourist Boat/... (BOAT-001, pattern MissionSetupTool)
///
///   1. Setup All (Scene + Config)
///        - Tạo TouristBoatConfig.asset tại Assets/_Game/ScriptableObjects/ (default GDD §7;
///          introDialogue lấy từ field initializer của TouristBoatConfig.cs — single source,
///          tool không bơm/đè). Đã có asset → GIỮ NGUYÊN giá trị designer chỉnh.
///        - Dựng hierarchy BoatSystem trong scene đang mở đúng contract với Dev A:
///            BoatSystem (BoatDockManager + TouristBoatUnlockFlow)
///            ├─ BlindPoint (offset -2600,-1800 — hướng biển dưới-trái theo map)
///            ├─ Dock_01 (0,0) · Dock_02 (+520,-260) · Dock_03 (+1040,-520) — BẬC THANG
///            │  theo hướng cầu cảng isometric (feedback Sếp: không spawn chồng 1 cục)
///            └─ mỗi Dock: BoatDockSlot · Berth · Path(WP_01..03) · Boat(Controller+Visual) · LockUI
///        - IDEMPOTENT + AN TOÀN VỊ TRÍ: BoatSystem đã có → KHÔNG reset vị trí user đã
///          kéo, chỉ bổ sung đúng phần thiếu (object/component/ref nào chưa có mới tạo).
///        - Mọi object tạo mới đều Undo.RegisterCreatedObjectUndo → Ctrl+Z gỡ sạch.
///        - Kết thúc: dialog REPORT tạo gì / giữ gì + checklist "CẦN BẠN LÀM".
///   2. Xóa Setup (Undo)   — xóa BoatSystem khỏi scene (undo được); config asset giữ lại.
///   3. Chọn Config        — ping + select TouristBoatConfig.asset trong Project.
///   4. Tự Sinh Lại Waypoints — sau khi Sếp kéo BlindPoint/Dock vào vị trí thật: xóa WP
///        cũ (undo được), sinh lại N WP (N = số WP hiện có, mặc định 3) đặt ĐỀU trên
///        đường thẳng BlindPoint → Berth của từng bến; report độ dài path + ước tính
///        travel giây theo boatSpeed của config đang gắn.
///   5. Hướng Dẫn Nhanh    — dialog 5 bước cho Sếp.
/// </summary>
public static class TouristBoatSetupTool
{
    private const string MenuRoot   = "Tools/Farm Game/Tourist Boat/";
    private const string MenuRebuild = MenuRoot + "0. DUNG LAI TU DAU (xoa cu + tao moi + noi waypoint)";
    private const string MenuSetup  = MenuRoot + "1. Setup All (Scene + Config)";
    private const string MenuDelete = MenuRoot + "2. Xóa Setup (Undo)";
    private const string MenuSelect = MenuRoot + "3. Chọn Config";
    private const string MenuRegen  = MenuRoot + "4. Tự Sinh Lại Waypoints (BlindPoint → Berth)";
    private const string MenuGuide  = MenuRoot + "5. Hướng Dẫn Nhanh";
    private const string MenuLockSize = MenuRoot + "9. Ap Co LockUI (chu to, tu canh theo map)";
    private const string MenuBoatFit  = MenuRoot + "10. Canh Tau Vao O Dau (co + xem truoc vi tri)";
    private const string MenuBoatGrab = MenuRoot + "11. Chot Vi Tri Tau Dang Keo (luu offset)";

    private const string ConfigFolder = "Assets/_Game/ScriptableObjects";
    private const string ConfigPath   = ConfigFolder + "/TouristBoatConfig.asset";

    private const string RootName  = "BoatSystem";
    private const string UndoLabel = "Tourist Boat Setup";

    private const int DockTotal            = 3; // = BoatDockManager.DockCount (const bên Dev A)
    private const int DefaultWaypointCount = 3;

    // Hội thoại intro: SINGLE SOURCE là field initializer của TouristBoatConfig.cs
    // (Dev A quản, bản không emoji) — tool KHÔNG bơm/đè introDialogue (quyết định lead
    // sau QA: tránh 2 nguồn text lệch nhau).

    // Offset mặc định khi tạo MỚI (hệ tọa độ world lớn — camera size 400..1500):
    // bậc thang xuống-phải theo hướng cầu cảng isometric, BlindPoint ngoài khơi dưới-trái.
    // Chạy lại Setup All trên hệ đã có sẽ KHÔNG đụng tới vị trí user đã kéo.
    private static readonly Vector3[] DockLocalOffsets =
    {
        new Vector3(0f,     0f,    0f),
        new Vector3(520f,  -260f,  0f),
        new Vector3(1040f, -520f,  0f),
    };
    private static readonly Vector3 BlindLocalOffset = new Vector3(-2600f, -1800f, 0f);

    // ─────────────────────────────────────────────────────────────────────────
    //  1. SETUP ALL — find-or-create từng mảnh, không phá vị trí có sẵn
    // ─────────────────────────────────────────────────────────────────────────

    // Khi RebuildAll gọi lại SetupAll/RegenerateAllWaypoints, hai hàm đó vẫn ghi log
    // Console nhưng KHÔNG bật dialog riêng — người dùng chỉ thấy đúng 1 bảng tổng kết.
    private static bool _quietDialog;

    /// <summary>DisplayDialog có thể bị tắt khi đang chạy trong RebuildAll.</summary>
    private static void Dialog(string title, string msg)
    {
        if (_quietDialog) return;
        EditorUtility.DisplayDialog(title, msg, "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  0. DỰNG LẠI TỪ ĐẦU — một nút, mọi thứ hiện ra lại
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Xóa sạch BoatSystem cũ rồi tạo lại toàn bộ + tự nối waypoint trong MỘT lần bấm.
    /// Khác menu 1 (Setup All) ở chỗ: menu 1 cố ý KHÔNG phá vị trí đã kéo nên nếu
    /// hierarchy đang dở dang/sai cấu trúc thì nó không sửa được; menu 0 dựng lại mới hẳn.
    /// </summary>
    [MenuItem(MenuRebuild, false, 0)]
    public static void RebuildAll()
    {
        GameObject cu = FindBoatSystem();

        bool ok = EditorUtility.DisplayDialog(
            "Tourist Boat — Dựng Lại Từ Đầu",
            (cu != null
                ? "Sẽ XÓA \"" + GetScenePath(cu.transform) + "\" đang có rồi tạo lại mới hoàn toàn.\n\n"
                  + "Vị trí bến/điểm mù bạn đã kéo trước đó SẼ MẤT (về vị trí mặc định).\n"
                  + "Sprite tàu bạn đã gắn cũng mất — phải gắn lại.\n\n"
                : "Chưa có BoatSystem trong scene — sẽ tạo mới toàn bộ.\n\n")
            + "Sau khi dựng: tự nối waypoint từ BlindPoint tới từng Berth.\n"
            + "Hoàn tác được bằng Ctrl+Z.",
            cu != null ? "Xóa và dựng lại" : "Dựng mới", "Hủy");
        if (!ok) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tourist Boat Rebuild");

        if (cu != null)
            Undo.DestroyObjectImmediate(cu);

        _quietDialog = true;
        try
        {
            SetupAll();
            RegenerateAllWaypoints();
        }
        finally
        {
            _quietDialog = false;
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        GameObject moi = FindBoatSystem();
        if (moi != null)
        {
            Selection.activeGameObject = moi;
            EditorGUIUtility.PingObject(moi);
        }

        string tomTat =
            "Đã dựng lại xong.\n\n"
            + "Trong Hierarchy giờ có: BoatSystem > BlindPoint + Dock_01/02/03\n"
            + "(mỗi Dock: Berth · Path(WP_01-03) · Boat/Visual · LockUI)\n\n"
            + "CẦN BẠN LÀM:\n"
            + "1) Kéo BlindPoint ra ngoài khơi (chỗ tàu núp, ngoài tầm camera).\n"
            + "2) Kéo Dock_01/02/03 vào 3 ô đậu trên cầu cảng.\n"
            + "3) Bấm lại menu \"4. Tự Sinh Lại Waypoints\" cho path khớp vị trí mới.\n"
            + "4) Gắn sprite tàu vào Dock_XX/Boat/Visual (Draw Mode = Simple).\n"
            + "5) Bấm Play rồi chạy menu \"7. Test Ngay\" để thấy tàu đậu ngay,\n"
            + "   hoặc menu \"6. Chẩn Đoán\" nếu vẫn không thấy tàu.\n\n"
            + "Chi tiết đầy đủ đã in ra Console. (Ctrl+Z hoàn tác toàn bộ.)";

        Debug.Log("[TouristBoat] RebuildAll hoàn tất — đã xóa cũ, tạo mới, nối waypoint.");
        EditorUtility.DisplayDialog("Tourist Boat — Dựng Lại Xong", tomTat, "OK");
    }

    [MenuItem(MenuSetup, false, 1)]
    public static void SetupAll()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        TouristBoatConfig config = LoadOrCreateConfig(out bool configCreated);

        var added = new StringBuilder(); // liệt kê phần BỔ SUNG trong lần chạy này
        int addedCount = 0;

        // Root: đã có → giữ nguyên (kể cả vị trí); chưa có → tạo tại pivot Scene view
        GameObject root = FindBoatSystem();
        bool rootExisted = root != null;
        Vector3 origin;
        if (root == null)
        {
            origin = Vector3.zero;
            if (SceneView.lastActiveSceneView != null)
            {
                origin   = SceneView.lastActiveSceneView.pivot;
                origin.z = 0f;
            }
            root = CreateGO(RootName, null, origin);
            Note(added, ref addedCount, RootName + " (gốc hệ thống, tại pivot Scene view)");
        }
        else
        {
            origin = root.transform.position; // offset mặc định tính từ vị trí user đã kéo
        }

        var manager = root.GetComponent<BoatDockManager>();
        if (manager == null)
        {
            manager = Undo.AddComponent<BoatDockManager>(root);
            Note(added, ref addedCount, "component BoatDockManager");
        }
        if (root.GetComponent<TouristBoatUnlockFlow>() == null)
        {
            Undo.AddComponent<TouristBoatUnlockFlow>(root);
            Note(added, ref addedCount, "component TouristBoatUnlockFlow");
        }

        Transform blind = root.transform.Find("BlindPoint");
        if (blind == null)
        {
            blind = CreateGO("BlindPoint", root.transform, origin + BlindLocalOffset).transform;
            Note(added, ref addedCount, "BlindPoint (offset -2600,-1800 — kéo ra ngoài khơi)");
        }

        for (int i = 0; i < DockTotal; i++)
            EnsureDock(root.transform, blind, i, origin + DockLocalOffsets[i], added, ref addedCount);

        // Config: chỉ gán khi field đang TRỐNG — không đè config designer đã chọn tay
        string configStatus;
        if (manager.Config != null)
            configStatus = "đã gắn từ trước — giữ nguyên";
        else if (WireConfigIfEmpty(manager, config))
            configStatus = "ĐÃ gán " + ConfigPath;
        else
            configStatus = "CHƯA gán được (không thấy field TouristBoatConfig) — kéo tay vào Inspector!";

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Selection.activeGameObject = root;
        if (!rootExisted && SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        // ── REPORT ──────────────────────────────────────────────────────────
        var report = new StringBuilder();
        if (rootExisted)
        {
            report.AppendLine("BoatSystem đã có sẵn — GIỮ NGUYÊN mọi vị trí bạn đã kéo.");
            report.AppendLine(addedCount > 0 ? "Bổ sung phần thiếu:" : "Không thiếu gì — scene không đổi.");
            if (addedCount > 0) report.Append(added);
        }
        else
        {
            report.AppendLine("ĐÃ TẠO MỚI (bậc thang isometric, không chồng 1 cục):");
            report.AppendLine("• " + RootName + " (BoatDockManager + TouristBoatUnlockFlow)");
            report.AppendLine("• BlindPoint (-2600,-1800) — hướng biển dưới-trái");
            report.AppendLine("• Dock_01 (0,0) · Dock_02 (+520,-260) · Dock_03 (+1040,-520)");
            report.AppendLine("  mỗi bến: Berth · Path/WP_01..03 (đều trên đường BlindPoint→Berth)");
            report.AppendLine("  · Boat/Visual (placeholder trắng) · LockUI (teaser đọc config runtime)");
        }
        report.AppendLine("• Config: " + ConfigPath + (configCreated
            ? "  (MỚI — default GDD §7; hội thoại intro từ initializer của TouristBoatConfig)"
            : "  (đã có sẵn — giữ nguyên giá trị)"));
        report.AppendLine("• Gán config vào BoatDockManager: " + configStatus);
        report.AppendLine();
        report.AppendLine("CẦN BẠN LÀM:");
        report.AppendLine("1) Kéo BlindPoint ra ngoài khơi (điểm tàu núp, ngoài tầm camera).");
        report.AppendLine("2) Kéo Dock_01..03 vào 3 ô đậu trên cầu cảng (con tự theo).");
        report.AppendLine("3) Bấm menu \"4. Tự Sinh Lại Waypoints\" — path tự nối lại theo vị trí mới.");
        report.AppendLine("4) Gắn sprite tàu thật vào Dock_XX/Boat/Visual rồi Play test.");
        report.AppendLine("   (Menu \"5. Hướng Dẫn Nhanh\" có đủ 5 bước chi tiết.)");
        report.AppendLine();
        report.AppendLine("(Ctrl+Z hoàn tác phần scene vừa tạo. Config asset không bị Ctrl+Z xóa.)");

        Dialog("Tourist Boat — Setup xong ✅", report.ToString());
        Debug.Log("[TouristBoat] Setup tool hoàn tất.\n" + report);
    }

    /// <summary>
    /// Đảm bảo 1 bến đầy đủ: Dock_0X + BoatDockSlot + Berth + Path(WP) + Boat + LockUI
    /// + collider. Mảnh nào ĐÃ CÓ thì giữ nguyên (kể cả vị trí), chỉ tạo mảnh thiếu.
    /// </summary>
    private static void EnsureDock(Transform root, Transform blind, int index, Vector3 defaultPos,
                                   StringBuilder added, ref int addedCount)
    {
        string dockName = $"Dock_{index + 1:00}";
        Transform dock = root.Find(dockName);
        bool dockIsNew = dock == null;
        if (dockIsNew)
        {
            dock = CreateGO(dockName, root, defaultPos).transform;
            Note(added, ref addedCount, dockName + " (offset bậc thang mặc định)");
        }
        Vector3 dockPos = dock.position; // bến có sẵn → dùng vị trí user đã kéo

        var slot = dock.GetComponent<BoatDockSlot>();
        bool slotIsNew = slot == null;
        if (slotIsNew)
        {
            slot = Undo.AddComponent<BoatDockSlot>(dock.gameObject);
            Note(added, ref addedCount, dockName + ": component BoatDockSlot");
        }

        Transform berth = dock.Find("Berth");
        if (berth == null)
        {
            berth = CreateGO("Berth", dock, dockPos).transform;
            Note(added, ref addedCount, dockName + "/Berth");
        }

        Transform path = dock.Find("Path");
        if (path == null)
        {
            path = CreateGO("Path", dock, dockPos).transform;
            Note(added, ref addedCount, dockName + "/Path");
        }
        if (path.childCount == 0)
        {
            RegenerateWaypoints(path, blind.position, berth.position, DefaultWaypointCount);
            Note(added, ref addedCount, dockName + $"/Path: {DefaultWaypointCount} WP (đều trên đường BlindPoint→Berth)");
        }

        Transform boat = dock.Find("Boat");
        if (boat == null)
        {
            boat = CreateGO("Boat", dock, blind.position).transform;
            var controller = Undo.AddComponent<TouristBoatController>(boat.gameObject);
            SetControllerDockIndex(controller, index);

            GameObject visual = CreateGO("Visual", boat, boat.position);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            AssignDirectionalBoatSprites(controller, index);
            Note(added, ref addedCount, dockName + "/Boat (TouristBoatController 12-Direction 360° + Visual)");
        }
        else
        {
            var controller = boat.GetComponent<TouristBoatController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<TouristBoatController>(boat.gameObject);
                SetControllerDockIndex(controller, index);
                Note(added, ref addedCount, dockName + "/Boat: component TouristBoatController");
            }
            if (boat.Find("Visual") == null)
            {
                GameObject visual = CreateGO("Visual", boat, boat.position);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 10;
                Note(added, ref addedCount, dockName + "/Boat/Visual (12-Direction 360°)");
            }
            AssignDirectionalBoatSprites(controller, index);
        }

        Transform lockUi = dock.Find("LockUI");
        TextMeshPro teaser;
        if (lockUi == null)
        {
            lockUi = BuildLockUi(dock, dockPos, out teaser).transform;
            Note(added, ref addedCount, dockName + "/LockUI (bảng khóa + teaser)");
        }
        else
        {
            Transform tt = lockUi.Find("TeaserText");
            teaser = tt != null ? tt.GetComponent<TextMeshPro>()
                                : lockUi.GetComponentInChildren<TextMeshPro>(true);
            if (teaser == null)
            {
                teaser = BuildTeaserText(lockUi, lockUi.position + new Vector3(0f, -48f, 0f));
                Note(added, ref addedCount, dockName + "/LockUI/TeaserText");
            }
        }

        var col = dock.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = Undo.AddComponent<BoxCollider2D>(dock.gameObject);
            col.offset = new Vector2(0f, 170f);
            col.size   = new Vector2(360f, 180f);
            Note(added, ref addedCount, dockName + ": BoxCollider2D (vùng tap nút khóa)");
        }

        // Wire field cho slot: slot MỚI → set tất cả; slot có sẵn → chỉ điền chỗ trống,
        // không đè ref/dockIndex user đã chỉnh tay.
        WireSlot(slot, index, berth, path, blind, lockUi.gameObject, teaser, col, force: slotIsNew);
    }

    /// <summary>LockUI đầy đủ: bảng khóa mờ + icon tròn + teaser text (nội dung set runtime từ config).</summary>
    private static GameObject BuildLockUi(Transform dock, Vector3 dockPos, out TextMeshPro teaser)
    {
        Vector3 lockPos = dockPos + new Vector3(0f, 170f, 0f);
        TouristBoatConfig sizeCfg = LoadConfigForSize();
        Vector2 panelSize = sizeCfg != null
            ? new Vector2(sizeCfg.lockPanelWidth, sizeCfg.lockPanelHeight)
            : new Vector2(620f, 300f);

        GameObject lockUi = CreateGO("LockUI", dock, lockPos);
        AddPlaceholderSprite(lockUi, panelSize, new Color(0.18f, 0.18f, 0.22f, 0.82f), sortingOrder: 50);

        GameObject lockIcon = CreateGO("LockIcon", lockUi.transform, lockPos + new Vector3(0f, 22f, 0f));
        AddPlaceholderSprite(lockIcon, new Vector2(60f, 60f), new Color(0.85f, 0.8f, 0.55f, 0.95f), sortingOrder: 51, round: true);

        teaser = BuildTeaserText(lockUi.transform, lockPos + new Vector3(0f, -48f, 0f));
        return lockUi;
    }

    /// <summary>TMP world-space cho teaser giá — BoatDockSlot điền text từ config lúc runtime.</summary>
    private static TextMeshPro BuildTeaserText(Transform parent, Vector3 worldPos)
    {
        GameObject teaserGo = CreateGO("TeaserText", parent, worldPos);
        // localScale = 1 và fontSize tính THẲNG bằng unit world: dễ suy luận hơn
        // cách cũ (scale 10 x fontSize 42 = chữ cao 420 unit trong khi bảng chỉ cao 160
        // — chữ tràn gấp 2,6 lần bảng, đó là lý do trông sai cỡ).
        teaserGo.transform.localScale = Vector3.one;
        var teaser = Undo.AddComponent<TextMeshPro>(teaserGo);
        TouristBoatConfig tcfg = LoadConfigForSize();
        teaser.fontSize         = tcfg != null ? tcfg.lockTeaserFontSize : 96f;
        teaser.alignment        = TextAlignmentOptions.Center;
        teaser.textWrappingMode = TextWrappingModes.Normal; // teaser xuống 2 dòng cho chữ to mà vẫn gọn
        teaser.overflowMode     = TextOverflowModes.Overflow;
        teaser.color            = Color.white;
        teaser.text             = "…";
        var mr = teaserGo.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 52;
        return teaser;
    }

    /// <summary>Set dockIndex serialize của TouristBoatController (mặc định -1) — không bắt runtime parse tên.</summary>
    private static void SetControllerDockIndex(TouristBoatController controller, int index)
    {
        var so = new SerializedObject(controller);
        SerializedProperty idx = so.FindProperty("dockIndex");
        if (idx != null)
        {
            idx.intValue = index;
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Wire các field serialize của BoatDockSlot. force=true (slot mới) set tất cả;
    /// force=false chỉ điền field object đang null — giữ chỉnh tay của user.
    /// </summary>
    private static void WireSlot(BoatDockSlot slot, int index, Transform berth, Transform path,
                                 Transform blind, GameObject lockUi, TextMeshPro teaser,
                                 Collider2D col, bool force)
    {
        var so = new SerializedObject(slot);
        if (force)
        {
            SerializedProperty idx = so.FindProperty("dockIndex");
            if (idx != null) idx.intValue = index;
        }
        SetRefIfEmpty(so, "berth",       berth,  force);
        SetRefIfEmpty(so, "pathRoot",    path,   force);
        SetRefIfEmpty(so, "blindPoint",  blind,  force);
        SetRefIfEmpty(so, "lockRoot",    lockUi, force);
        SetRefIfEmpty(so, "teaserText",  teaser, force);
        SetRefIfEmpty(so, "tapCollider", col,    force);
        so.ApplyModifiedProperties();
    }

    private static void SetRefIfEmpty(SerializedObject so, string propName, Object value, bool force)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p == null) return;
        if (force || p.objectReferenceValue == null)
            p.objectReferenceValue = value;
    }

    private static void AssignDirectionalBoatSprites(TouristBoatController controller, int dockIndex)
    {
        if (controller == null) return;
        var so = new SerializedObject(controller);
        var propArray = so.FindProperty("directionalSprites");
        if (propArray == null) return;

        propArray.arraySize = 12;
        string prefix = (dockIndex == 1) ? "boat_red_12_dir_" : "boat_blue_12_dir_";

        for (int i = 0; i < 12; i++)
        {
            string path = $"Assets/Assetsgame/TouristBoat/12_Directions/{prefix}{i}.png";
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var element = propArray.GetArrayElementAtIndex(i);
            if (element != null) element.objectReferenceValue = s;
        }

        so.ApplyModifiedProperties();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  2. XÓA SETUP
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(MenuDelete, false, 2)]
    public static void DeleteSetup()
    {
        GameObject root = FindBoatSystem();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                "Không tìm thấy " + RootName + " trong scene đang mở — không có gì để xóa.", "OK");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Tourist Boat — Xóa Setup",
            "Xóa \"" + GetScenePath(root.transform) + "\" khỏi scene?\n\n" +
            "• Hoàn tác được bằng Ctrl+Z.\n" +
            "• Config asset (" + ConfigPath + ") được GIỮ LẠI — muốn bỏ thì xóa tay trong Project.",
            "Xóa", "Hủy");
        if (!ok) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tourist Boat Delete");
        Undo.DestroyObjectImmediate(root);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        Debug.Log("[TouristBoat] Đã xóa " + RootName + " khỏi scene (Ctrl+Z để hoàn tác). Config asset giữ nguyên: " + ConfigPath);
        EditorUtility.DisplayDialog("Tourist Boat",
            "Đã xóa " + RootName + " khỏi scene (Ctrl+Z để hoàn tác).\nConfig asset giữ nguyên: " + ConfigPath, "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  3. CHỌN CONFIG
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(MenuSelect, false, 3)]
    public static void SelectConfig()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<TouristBoatConfig>(ConfigPath);
        if (cfg == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                "Chưa có config tại:\n" + ConfigPath + "\n\nChạy \"" + MenuSetup + "\" để tạo.", "OK");
            return;
        }

        Selection.activeObject = cfg;
        EditorGUIUtility.PingObject(cfg);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  4. TỰ SINH LẠI WAYPOINTS — workflow chính của Sếp sau khi kéo Dock/BlindPoint
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(MenuRegen, false, 4)]
    public static void RegenerateAllWaypoints()
    {
        GameObject root = FindBoatSystem();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                "Không tìm thấy " + RootName + " trong scene — chạy \"" + MenuSetup + "\" trước.", "OK");
            return;
        }

        Transform blind = root.transform.Find("BlindPoint");
        if (blind == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                RootName + " thiếu con \"BlindPoint\" — chạy lại \"" + MenuSetup + "\" để bổ sung.", "OK");
            return;
        }

        // Config để ước tính travel: ưu tiên config đang gắn trên manager, fallback asset chuẩn
        TouristBoatConfig config = null;
        var manager = root.GetComponent<BoatDockManager>();
        if (manager != null) config = manager.Config;
        if (config == null) config = AssetDatabase.LoadAssetAtPath<TouristBoatConfig>(ConfigPath);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tourist Boat Regen Waypoints");

        var slots = root.GetComponentsInChildren<BoatDockSlot>(true);
        System.Array.Sort(slots, (a, b) => a.dockIndex.CompareTo(b.dockIndex));

        var report = new StringBuilder();
        int done = 0;

        foreach (var slot in slots)
        {
            Transform dock  = slot.transform;
            Transform berth = dock.Find("Berth");
            if (berth == null)
            {
                report.AppendLine(dock.name + ": THIẾU con \"Berth\" — bỏ qua.");
                continue;
            }

            Transform path = dock.Find("Path");
            if (path == null)
                path = CreateGO("Path", dock, dock.position).transform;

            // N giữ theo số WP hiện có (user thêm/bớt WP thì tôn trọng), mặc định 3
            int n = path.childCount > 0 ? path.childCount : DefaultWaypointCount;

            // Xóa WP cũ (undo được) — duyệt ngược vì DestroyImmediate rút ngắn childCount
            for (int c = path.childCount - 1; c >= 0; c--)
                Undo.DestroyObjectImmediate(path.GetChild(c).gameObject);

            RegenerateWaypoints(path, blind.position, berth.position, n);
            done++;

            float length = ComputePathLength(blind, path, berth);
            string travel = config != null
                ? $" ≈ {length / Mathf.Max(1f, config.boatSpeed):0.0}s travel (speed {config.boatSpeed:0})"
                : " (chưa gắn config — không ước tính travel được)";
            report.AppendLine($"{dock.name}: {n} WP · path {length:0} unit{travel}");
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        if (done == 0)
        {
            EditorUtility.DisplayDialog("Tourist Boat — Waypoints",
                "Không sinh lại được bến nào:\n" + report, "OK");
            return;
        }

        report.AppendLine();
        report.AppendLine("WP đặt ĐỀU trên đường thẳng BlindPoint → Berth");
        report.AppendLine("(WP cuối cách Berth đúng 1 khoảng chia — không trùng Berth).");
        report.AppendLine("Vẫn tinh chỉnh từng WP bằng tay được — gizmo line xanh trong Scene view.");
        report.AppendLine("(Ctrl+Z để hoàn tác.)");

        Dialog("Tourist Boat — Waypoints ✅", report.ToString());
        Debug.Log("[TouristBoat] Regen waypoints cho " + done + " bến.\n" + report);
    }

    /// <summary>
    /// Sinh count WP đặt ĐỀU trên đường thẳng blindPos → berthPos:
    /// t = k/(count+1), k = 1..count → WP đầu gần BlindPoint, WP cuối cách Berth
    /// đúng 1 khoảng chia (KHÔNG trùng Berth — berth là điểm kết riêng của polyline).
    /// </summary>
    private static void RegenerateWaypoints(Transform pathRoot, Vector3 blindPos, Vector3 berthPos, int count)
    {
        for (int k = 1; k <= count; k++)
        {
            float t = (float)k / (count + 1);
            CreateGO($"WP_{k:00}", pathRoot, Vector3.Lerp(blindPos, berthPos, t));
        }
    }

    /// <summary>Độ dài polyline BlindPoint → WP theo thứ tự con → Berth (unit world).</summary>
    private static float ComputePathLength(Transform blind, Transform path, Transform berth)
    {
        float length = 0f;
        Vector3 prev = blind.position;
        for (int c = 0; c < path.childCount; c++)
        {
            Vector3 p = path.GetChild(c).position;
            length += Vector3.Distance(prev, p);
            prev = p;
        }
        length += Vector3.Distance(prev, berth.position);
        return length;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  5. HƯỚNG DẪN NHANH
    // ─────────────────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────────
    //  9. ÁP CỠ LOCKUI — chữ to, tự canh theo khoảng cách bến thật
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Áp lại cỡ bảng khóa / icon / chữ teaser cho 3 bến ĐANG CÓ trong scene, lấy số
    /// từ TouristBoatConfig. Đo luôn khoảng cách thật giữa các bến rồi cảnh báo nếu
    /// bảng rộng quá sẽ chạm nhau — map này dùng toạ độ rất lớn nên số pixel UI
    /// thông thường (kiểu 340x160) trông bé xíu, phải canh theo unit world.
    /// Chạy lại được nhiều lần: sửa số trong Config rồi bấm menu này là thấy ngay.
    /// </summary>
    [MenuItem(MenuLockSize, false, 9)]
    public static void ApplyLockUiSize()
    {
        GameObject root = FindBoatSystem();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                "Không tìm thấy " + RootName + " trong scene.\nChạy menu 0. DỰNG LẠI TỪ ĐẦU trước.", "OK");
            return;
        }

        TouristBoatConfig cfg = LoadConfigForSize();
        if (cfg == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                "Không đọc được TouristBoatConfig tại:\n" + ConfigPath, "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tourist Boat LockUI Size");

        var report = new StringBuilder();
        var berths = new List<Vector3>();
        int applied = 0;

        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            Transform dock = root.transform.Find(string.Format("Dock_{0:00}", i + 1));
            if (dock == null) continue;

            Transform berth = dock.Find("Berth");
            if (berth != null) berths.Add(berth.position);

            Transform lockUi = dock.Find("LockUI");
            if (lockUi == null)
            {
                report.AppendLine(string.Format("- Dock_{0:00}: không có LockUI (bến này bỏ qua).", i + 1));
                continue;
            }

            // Bảng khóa
            var panel = lockUi.GetComponent<SpriteRenderer>();
            if (panel != null)
                ApplySpriteSize(panel, new Vector2(cfg.lockPanelWidth, cfg.lockPanelHeight));

            // Icon ổ khóa — đặt ở nửa trên bảng
            Transform icon = lockUi.Find("LockIcon");
            if (icon != null)
            {
                var ir = icon.GetComponent<SpriteRenderer>();
                if (ir != null)
                    ApplySpriteSize(ir, new Vector2(cfg.lockIconSize, cfg.lockIconSize));
                Undo.RecordObject(icon, "LockIcon pos");
                icon.localPosition = new Vector3(0f, cfg.lockPanelHeight * 0.22f, 0f);
            }

            // Chữ teaser — fontSize tính thẳng bằng unit world, scale giữ 1
            Transform tt = lockUi.Find("TeaserText");
            if (tt != null)
            {
                var tmp = tt.GetComponent<TextMeshPro>();
                if (tmp != null)
                {
                    Undo.RecordObject(tmp, "Teaser font");
                    tmp.fontSize         = cfg.lockTeaserFontSize;
                    tmp.alignment        = TextAlignmentOptions.Center;
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                    tmp.overflowMode     = TextOverflowModes.Overflow;
                    // Vùng chữ rộng gần bằng bảng để wrap đúng chỗ
                    var rt = tmp.rectTransform;
                    if (rt != null)
                    {
                        Undo.RecordObject(rt, "Teaser rect");
                        rt.sizeDelta = new Vector2(cfg.lockPanelWidth * 0.92f, cfg.lockPanelHeight * 0.55f);
                    }
                }
                Undo.RecordObject(tt, "Teaser pos");
                tt.localPosition = new Vector3(0f, -cfg.lockPanelHeight * 0.20f, 0f);
                tt.localScale    = Vector3.one;
            }

            applied++;
            report.AppendLine(string.Format("- Dock_{0:00}: bảng {1:0}x{2:0} · icon {3:0} · chữ {4:0} unit",
                i + 1, cfg.lockPanelWidth, cfg.lockPanelHeight, cfg.lockIconSize, cfg.lockTeaserFontSize));
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        // Đo khoảng cách bến thật để cảnh báo bảng chạm nhau
        float minGap = float.MaxValue;
        for (int a = 0; a < berths.Count; a++)
            for (int b = a + 1; b < berths.Count; b++)
                minGap = Mathf.Min(minGap, Vector3.Distance(berths[a], berths[b]));

        var head = new StringBuilder();
        head.AppendLine("Đã áp cỡ cho " + applied + " bến.");
        head.AppendLine();
        if (berths.Count >= 2 && minGap < float.MaxValue)
        {
            head.AppendLine("Khoảng cách gần nhất giữa 2 bến: " + minGap.ToString("0") + " unit.");
            if (cfg.lockPanelWidth > minGap * 0.95f)
                head.AppendLine("CẢNH BÁO: bảng rộng " + cfg.lockPanelWidth.ToString("0")
                                + " unit -> 2 bảng cạnh nhau sẽ chạm/đè. Nên đặt lockPanelWidth <= "
                                + (minGap * 0.85f).ToString("0") + ".");
            else
                head.AppendLine("Cỡ bảng hiện tại KHÔNG bị chạm nhau — an toàn.");
            head.AppendLine();
        }
        head.AppendLine("Muốn to/nhỏ hơn: mở " + ConfigPath);
        head.AppendLine("sửa lockPanelWidth / lockPanelHeight / lockIconSize / lockTeaserFontSize");
        head.AppendLine("rồi bấm lại menu 9. (Ctrl+Z hoàn tác.)");
        head.AppendLine();
        head.Append("Chi tiết từng bến đã in ra Console.");

        Debug.Log("[TouristBoat] Áp cỡ LockUI:\n" + report);
        EditorUtility.DisplayDialog("Tourist Boat — Cỡ LockUI", head.ToString(), "OK");
    }

    /// <summary>Đọc TouristBoatConfig ở đường dẫn chuẩn (dùng cho các hàm tính cỡ).</summary>
    private static TouristBoatConfig LoadConfigForSize()
        => AssetDatabase.LoadAssetAtPath<TouristBoatConfig>(ConfigPath);

    // ─────────────────────────────────────────────────────────────────────────
    //  10 & 11. CANH TÀU VÀO Ô ĐẬU
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Áp cỡ sprite tàu theo Config và SNAP tàu về đúng chỗ nó sẽ đậu khi Play
    /// (= Berth + berthOffset). Mục đích: trong Play Mode code ghi lại vị trí tàu
    /// mỗi frame nên kéo tay vô ích ("bị khóa cứng"); thay vào đó canh trong Edit
    /// Mode bằng menu này rồi bấm Play là khớp.
    /// </summary>
    [MenuItem(MenuBoatFit, false, 10)]
    public static void FitBoatsToDock()
    {
        GameObject root = FindBoatSystem();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                "Không tìm thấy " + RootName + " trong scene.", "OK");
            return;
        }

        TouristBoatConfig cfg = LoadConfigForSize();
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tourist Boat Fit");

        var report = new StringBuilder();
        int done = 0;

        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            Transform dock = root.transform.Find(string.Format("Dock_{0:00}", i + 1));
            if (dock == null) continue;

            Transform berth = dock.Find("Berth");
            var boat = dock.GetComponentInChildren<TouristBoatController>(true);
            if (boat == null || berth == null)
            {
                report.AppendLine(string.Format("- Dock_{0:00}: thiếu Boat hoặc Berth — bỏ qua.", i + 1));
                continue;
            }

            // Cỡ tàu (bỏ qua nếu Config để 0 — nghĩa là bạn tự chỉnh tay)
            Transform vis = boat.transform.Find("Visual");
            var sr = vis != null ? vis.GetComponent<SpriteRenderer>() : null;
            if (sr != null && cfg != null && cfg.boatVisualWidth > 0.01f)
            {
                if (sr.sprite != null)
                {
                    Vector2 native = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
                    float h = cfg.boatVisualHeight > 0.01f
                        ? cfg.boatVisualHeight
                        : (native.x > 0.0001f ? cfg.boatVisualWidth * native.y / native.x : cfg.boatVisualWidth);
                    ApplySpriteSize(sr, new Vector2(cfg.boatVisualWidth, h));
                    report.AppendLine(string.Format("- Dock_{0:00}: cỡ tàu {1:0} x {2:0} unit", i + 1, cfg.boatVisualWidth, h));
                }
                else
                {
                    report.AppendLine(string.Format("- Dock_{0:00}: Visual chưa có sprite — chưa canh cỡ được.", i + 1));
                }
            }

            // Snap tàu về đúng chỗ đậu của Play Mode
            Undo.RecordObject(boat.transform, "Boat snap");
            Vector3 target = boat.EditorGetDockedPosition(berth);
            boat.transform.position = target;
            Vector3 off = boat.EditorBerthOffset;
            report.AppendLine(string.Format("  vị trí đậu ({0:0}, {1:0}) — offset đang lưu ({2:0}, {3:0})",
                target.x, target.y, off.x, off.y));
            done++;
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        var head = new StringBuilder();
        head.AppendLine("Đã canh " + done + " tàu.");
        head.AppendLine();
        head.AppendLine("Tàu hiện đang đứng ĐÚNG chỗ nó sẽ đậu khi bấm Play.");
        head.AppendLine();
        head.AppendLine("CÒN LỆCH THÌ LÀM THEO 1 TRONG 2 CÁCH:");
        head.AppendLine();
        head.AppendLine("A) Kéo cả object Berth tới đúng ô đậu, rồi bấm lại menu 10.");
        head.AppendLine("   (Berth = chỗ tàu đậu. Đây là cách nên dùng.)");
        head.AppendLine();
        head.AppendLine("B) Kéo trực tiếp object Boat cho khớp mắt, rồi bấm menu 11");
        head.AppendLine("   để lưu độ lệch đó vào berthOffset. Play Mode sẽ giữ đúng chỗ đó.");
        head.AppendLine();
        head.AppendLine("Cỡ tàu: sửa boatVisualWidth trong Config rồi bấm lại menu 10.");
        head.AppendLine("(Để 0 = tool không đụng cỡ, bạn tự chỉnh tay.)");
        head.AppendLine();
        head.Append("Chi tiết đã in ra Console. Ctrl+Z hoàn tác.");

        Debug.Log("[TouristBoat] Canh tàu vào ô đậu:\n" + report);
        EditorUtility.DisplayDialog("Tourist Boat — Canh Tàu", head.ToString(), "OK");
    }

    /// <summary>
    /// Lưu vị trí tàu bạn vừa kéo tay thành berthOffset (độ lệch so với Berth).
    /// Nhờ vậy Play Mode đặt tàu đúng chỗ bạn canh bằng mắt, thay vì đúng tâm Berth.
    /// </summary>
    [MenuItem(MenuBoatGrab, false, 11)]
    public static void CaptureBoatOffsets()
    {
        GameObject root = FindBoatSystem();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat", "Không tìm thấy " + RootName + ".", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tourist Boat Capture Offset");

        var report = new StringBuilder();
        int done = 0;

        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            Transform dock = root.transform.Find(string.Format("Dock_{0:00}", i + 1));
            if (dock == null) continue;
            Transform berth = dock.Find("Berth");
            var boat = dock.GetComponentInChildren<TouristBoatController>(true);
            if (boat == null || berth == null) continue;

            Undo.RecordObject(boat, "Capture berth offset");
            boat.EditorCaptureOffsetFrom(berth);
            EditorUtility.SetDirty(boat);
            Vector3 off = boat.EditorBerthOffset;
            report.AppendLine(string.Format("- Dock_{0:00}: berthOffset = ({1:0}, {2:0})", i + 1, off.x, off.y));
            done++;
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Debug.Log("[TouristBoat] Chốt offset tàu:\n" + report);
        EditorUtility.DisplayDialog("Tourist Boat — Chốt Vị Trí",
            "Đã lưu offset cho " + done + " tàu.\n\n"
            + "Bấm Play: tàu sẽ đậu đúng chỗ bạn vừa kéo.\n\n"
            + "Nhớ Ctrl+S lưu scene.\n\nChi tiết in ở Console.", "OK");
    }

    [MenuItem(MenuGuide, false, 5)]
    public static void ShowQuickGuide()
    {
        EditorUtility.DisplayDialog(
            "Tourist Boat — Hướng Dẫn Nhanh (5 bước)",
            "1) Kéo BlindPoint ra ngoài khơi — điểm tàu núp giữa 2 chuyến, để ngoài tầm camera người chơi.\n\n" +
            "2) Kéo Dock_01..Dock_03 vào 3 ô đậu trên cầu cảng (Berth/Boat/LockUI là con — tự theo).\n\n" +
            "3) Bấm menu \"4. Tự Sinh Lại Waypoints\" — path tự nối thẳng BlindPoint → từng Berth, sau đó vẫn tinh chỉnh từng WP bằng tay (gizmo line xanh trong Scene view).\n\n" +
            "4) Gắn sprite tàu thật vào Dock_XX/Boat/Visual. NHỚ: ẩn/xóa tàu ART TĨNH đang trang trí ở cầu cảng nếu trùng chỗ đậu — không thì thấy 2 tàu chồng nhau.\n\n" +
            "5) Play Mode test: đặt debugTimeScale = 60 trong TouristBoatConfig để tua nhanh chu kỳ (chỉ ăn trong Editor/Development build), lên Lv10 xem intro + tàu vào bến.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Load config; chưa có thì tạo mới với default GDD §7. Asset có sẵn → giữ nguyên
    /// giá trị designer đã chỉnh. introDialogue KHÔNG được tool đụng tới — nguồn duy
    /// nhất là field initializer trong TouristBoatConfig.cs (tự có khi CreateInstance).
    /// </summary>
    private static TouristBoatConfig LoadOrCreateConfig(out bool created)
    {
        created = false;
        EnsureFolder(ConfigFolder);

        var cfg = AssetDatabase.LoadAssetAtPath<TouristBoatConfig>(ConfigPath);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<TouristBoatConfig>();
            ApplyGddDefaults(cfg);
            AssetDatabase.CreateAsset(cfg, ConfigPath);
            created = true;
        }

        AssetDatabase.SaveAssets();
        return cfg;
    }

    /// <summary>
    /// Default theo GDD tourist-boat-system.md §4 + §7 (tất cả là tuning knobs).
    /// introDialogue KHÔNG set ở đây — giữ nguyên từ initializer của TouristBoatConfig.
    /// Ghi chú lệch contract: spec ban đầu ghi config có "dockCount" nhưng bản thật
    /// của Dev A dùng const BoatDockManager.DockCount = 3 — không có field để set.
    /// </summary>
    private static void ApplyGddDefaults(TouristBoatConfig cfg)
    {
        cfg.unlockLevel    = 10;
        cfg.dockMinutes    = 40;
        cfg.hideMinutes    = 15;
        cfg.staggerMinutes = 12;
        cfg.boatSpeed      = 300;
        cfg.bobAmplitude   = 8;
        cfg.bobFrequency   = 0.8f;
        cfg.dock2Level     = 12;
        cfg.dock2GoldCost  = 2000;
        cfg.dock3Level     = 14;
        cfg.dock3GemCost   = 25;
        cfg.debugTimeScale = 1;
    }

    /// <summary>
    /// Gán config vào field TouristBoatConfig đầu tiên của BoatDockManager NẾU đang
    /// trống — dò theo type qua SerializedObject (tên field private do Dev A đặt).
    /// Trả true nếu đã gán trong lần gọi này.
    /// </summary>
    private static bool WireConfigIfEmpty(BoatDockManager manager, TouristBoatConfig cfg)
    {
        var so = new SerializedObject(manager);
        SerializedProperty it = so.GetIterator();
        while (it.NextVisible(true))
        {
            if (it.propertyType == SerializedPropertyType.ObjectReference &&
                it.type.Contains("TouristBoatConfig"))
            {
                if (it.objectReferenceValue != null) return false; // designer đã gắn — không đè
                it.objectReferenceValue = cfg;
                so.ApplyModifiedProperties();
                return true;
            }
        }
        return false;
    }

    /// <summary>Ghi 1 dòng "phần bổ sung" vào report + tăng đếm.</summary>
    private static void Note(StringBuilder sb, ref int count, string line)
    {
        sb.AppendLine("   + " + line);
        count++;
    }

    /// <summary>Tạo GameObject + RegisterCreatedObjectUndo (spec: undo cho MỌI object).</summary>
    private static GameObject CreateGO(string name, Transform parent, Vector3 worldPos)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, UndoLabel);
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        return go;
    }

    /// <summary>
    /// SpriteRenderer placeholder trắng từ sprite built-in (UISprite 9-slice / Knob tròn).
    /// UISprite có border → dùng drawMode Sliced đặt size chuẩn theo unit; sprite không
    /// border thì scale transform theo kích thước gốc. Sprite null cũng không NRE (GDD edge #8).
    /// </summary>
    private static void AddPlaceholderSprite(GameObject go, Vector2 size, Color color,
                                             int sortingOrder, bool round = false)
    {
        var sr = Undo.AddComponent<SpriteRenderer>(go);
        sr.sprite = round
            ? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
            : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color        = color;
        sr.sortingOrder = sortingOrder;

        if (sr.sprite == null)
        {
            Debug.LogWarning("[TouristBoat] Không load được sprite built-in — SpriteRenderer để trống (không lỗi, gắn art sau).");
            return;
        }

        ApplySpriteSize(sr, size);
    }

    /// <summary>
    /// Đặt kích thước hiển thị của 1 SpriteRenderer theo UNIT WORLD, an toàn cho cả 2 loại sprite.
    ///
    /// Vì sao phải có hàm riêng: sprite CÓ border (UISprite 9-slice) thì set được sr.size trực tiếp,
    /// nhưng sprite KHÔNG border (Knob tròn) phải phóng bằng localScale. Nếu nhầm hai đường này
    /// thì hai hệ số NHÂN với nhau: icon từng bị scale 183 rồi set thêm size 150 -> 27.000 unit,
    /// che kín cả map. Luôn RESET localScale về 1 trước khi tính để chạy lại nhiều lần không dồn.
    /// </summary>
    private static void ApplySpriteSize(SpriteRenderer sr, Vector2 size)
    {
        if (sr == null || sr.sprite == null) return;

        Undo.RecordObject(sr, "Sprite size");
        Undo.RecordObject(sr.transform, "Sprite scale");
        sr.transform.localScale = Vector3.one; // chống dồn hệ số giữa các lần chạy

        if (sr.sprite.border != Vector4.zero)
        {
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size     = size;
        }
        else
        {
            // Sprite không có border: Sliced/size vô nghĩa -> giữ Simple và phóng bằng scale.
            sr.drawMode = SpriteDrawMode.Simple;
            Vector2 native = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            sr.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        }
    }

    /// <summary>Tìm BoatSystem trong scene đang mở — ưu tiên theo component, fallback theo tên (kể cả đang tắt).</summary>
    private static GameObject FindBoatSystem()
    {
        var mgr = Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        if (mgr != null) return mgr.gameObject;

        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.parent == null && t.name == RootName)
                return t.gameObject;
        }
        return null;
    }

    /// <summary>Đường dẫn hierarchy để in trong dialog (Scene/Cha/Con).</summary>
    private static string GetScenePath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

    /// <summary>Tạo folder lồng nhau trong Assets nếu chưa có (copy pattern MissionSetupTool).</summary>
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf   = System.IO.Path.GetFileName(folder);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
