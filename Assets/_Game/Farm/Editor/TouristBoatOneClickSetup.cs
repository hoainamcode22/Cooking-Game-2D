using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ★ TOOL MỘT NÚT — dựng TOÀN BỘ hệ Tàu Du Lịch V2 trong một lần bấm (Sếp yêu cầu 2026-08-29).
///
/// Sau khi chạy xong, Sếp KHÔNG phải điền config, KHÔNG phải kéo waypoint nữa —
/// mọi toạ độ đã được Lead trích THẲNG từ `SCN_Farm.unity` (world space, đã tính scale
/// cha) và ghi cứng trong bảng <see cref="QueueAnchorPos"/> / <see cref="DockWaypoints"/>
/// bên dưới.
///
/// THỨ TỰ CHẠY (mỗi bước có progress bar + đếm lỗi riêng):
///   1. Điền 13 field V2 vào TouristBoatConfig.asset
///   2. NPCAnimationSetupTool   — 132 sprite → clip → controller → 11 prefab khách
///   3. TouristVisitorSetupTool — TouristSystem / QueueAnchor / 3 path / 3 gangplank + wire
///   4. GHI ĐÈ TOẠ ĐỘ THẬT      — QueueAnchor + 3×3 waypoint (điểm mấu chốt)
///   5. TouristBoatUIPopupSetupTool (Dev C) — 2 popup + canvas riêng
///   6. Dịch bến sát bờ qua API Dev A (+0, +200) + đặt lại Gangplank theo Berth mới
///   7. TỰ KIỂM TRA — in bảng "đủ / thiếu" từng mục kèm cách khắc phục
///   8. MarkSceneDirty — KHÔNG tự lưu scene, chỉ nhắc Sếp Ctrl+S
///
/// GỌI TOOL KHÁC THẾ NÀO:
///   • 2 tool của Dev B: gọi TRỰC TIẾP hàm <c>RunSetup(quiet: true)</c> — chờ được,
///     bắt được lỗi, và không bật dialog riêng (chỉ có ĐÚNG 1 bảng tổng kết cuối cùng).
///   • Tool của Dev A (BoatShoreAdjustTool): Dev A ĐÃ MỞ API public static
///     (`ApplyShoreOffset` / `GuessShoreDirection` / `DefaultShoreOffset`) nên gọi
///     TRỰC TIẾP — không còn reflection, không còn bản sao logic dịch bến.
///   • Tool của Dev C (TouristBoatUIPopupSetupTool): vẫn gọi qua REFLECTION có chủ ý —
///     gói UI có thể chưa được copy vào project, tham chiếu thẳng sẽ làm CẢ TOOL NÀY
///     không biên dịch được; reflection thì chỉ bỏ qua bước popup và ghi rõ trong report.
///     Tra hàm ĐÍCH DANH theo chữ ký `SetupPopups(bool)` và MỌI lời gọi reflection đều
///     nằm trong try (lỗi Dev C bắt được 2026-08-29).
///
/// IDEMPOTENT: chạy lại 5 lần vẫn ra đúng 1 bộ — các tool con đều find-or-create, bước 4
/// tự tạo/xoá waypoint cho khớp đúng 3 cái, bước 6 chỉ dịch khi CHƯA dịch (đánh dấu bằng
/// khoảng cách Berth so với toạ độ gốc trong bảng).
/// </summary>
public static class TouristBoatOneClickSetup
{
    private const string MenuOneClick = "Tools/Farm Game/Tourist Boat/★ SETUP TẤT CẢ (1 nút)";
    private const string UndoLabel    = "Tourist Boat — Setup tất cả";
    private const string RootBoat     = "BoatSystem";
    private const string RootTourist  = "TouristSystem";
    private const string PrefabTouristRoot = "Assets/_Game/Farm/Prefabs/Tourists";

    /// <summary>Số bến của hệ — bằng BoatDockManager.DockCount, để riêng cho code Editor.</summary>
    private const int DockTotal = 3;

    // ─────────────────────────────────────────────────────────────────────
    //  TOẠ ĐỘ THẬT — Lead trích từ SCN_Farm.unity (world space, đã tính scale cha)
    // ─────────────────────────────────────────────────────────────────────
    //  Đường đi bám khu đất/cát (Tilemap_IsoDirt 332 ô + Tilemap_IsoSand), TRÁNH
    //  House_05 (-473,-2613) và House_02 (-1170,-2283); 3 bến hội tụ dần về nhà hàng
    //  CookingGate (494,-2367). WP cuối nối thẳng vào QueueAnchor nên không cần WP_04.
    //  Điểm đầu của đường đi bộ là Gangplank (đặt theo Berth) nên không cần WP_00.

    /// <summary>Chỗ khách ĐỨNG ĐẦU hàng — ngay trước cửa CookingGate.</summary>
    private static readonly Vector2 QueueAnchorPos = new Vector2(400f, -2700f);

    /// <summary>3 waypoint/bến, thứ tự WP_01 → WP_03 (từ bến đi vào nhà hàng).</summary>
    private static readonly Vector2[][] DockWaypoints =
    {
        new[] { new Vector2(-380f, -3980f), new Vector2( -40f, -3420f), new Vector2(260f, -2950f) }, // Dock_01
        new[] { new Vector2( 220f, -4080f), new Vector2( 340f, -3460f), new Vector2(400f, -2960f) }, // Dock_02
        new[] { new Vector2( 900f, -4260f), new Vector2( 700f, -3520f), new Vector2(500f, -2960f) }, // Dock_03
    };

    /// <summary>Toạ độ Berth GỐC trong scene (trước khi dịch sát bờ) — dùng để bước 6 idempotent.</summary>
    private static readonly Vector2[] BerthOriginal =
    {
        new Vector2(-531f, -4285f),
        new Vector2( 151f, -4573f),
        new Vector2( 948f, -4839f),
    };

    // Offset dịch bến sát bờ KHÔNG khai lại ở đây nữa: nguồn duy nhất là
    // BoatShoreAdjustTool.DefaultShoreOffset của Dev A (= (0, 200); +Y = vào bờ theo
    // layout scene hiện tại). Khai hai chỗ là sớm muộn lệch nhau.

    /// <summary>Dịch xong thì Berth cách toạ độ gốc đúng |ShoreOffset|; sai số cho phép khi so sánh.</summary>
    private const float ShoreEpsilon = 25f;

    // 13 field V2 của TouristBoatConfig (Sếp chốt 2026-08-29; maxDockMinutes = 35).
    private const float CfgGapOneDock    = 5f;
    private const float CfgGapMultiDock  = 10f;
    private const float CfgMinStagger    = 3f;
    private const float CfgMaxDock       = 35f;
    private const int   CfgVisitorsMin   = 3;
    private const int   CfgVisitorsMax   = 6;
    private const float CfgPatience      = 30f;
    private const int   CfgRewardMul     = 2;
    private const float CfgDisembark     = 0.8f;
    private const float CfgWalkSpeed     = 150f;
    private const float CfgQueueSpacing  = 120f;
    private const float CfgBubbleScaleIn = 0.25f;
    private const float CfgSmileyFly     = 1.2f;

    // ─────────────────────────────────────────────────────────────────────
    //  MENU
    // ─────────────────────────────────────────────────────────────────────

    // priority 0 = nằm TRÊN CÙNG nhánh Tourist Boat.
    [MenuItem(MenuOneClick, false, 0)]
    public static void RunAll()
    {
        if (!EditorUtility.DisplayDialog(
                "★ Setup tất cả hệ Tàu Du Lịch",
                "Tool sẽ chạy 8 bước liên tiếp trong scene ĐANG MỞ:\n\n" +
                "1. Điền 13 thông số vào TouristBoatConfig\n" +
                "2. Dựng animation + 11 prefab khách du lịch\n" +
                "3. Dựng TouristSystem (hàng chờ, đường đi, tấm gỗ)\n" +
                "4. Ghi ĐÚNG toạ độ đường đi đã đo từ scene (không phải kéo tay nữa)\n" +
                "5. Dựng 2 popup (báo tàu / mua bến)\n" +
                "6. Dịch 3 bến sát bờ\n" +
                "7. Tự kiểm tra và báo cáo\n\n" +
                "Chạy lại nhiều lần vẫn an toàn (không nhân đôi).\n" +
                "Scene sẽ KHÔNG tự lưu — bạn tự Ctrl+S sau khi xem kết quả.",
                "Chạy", "Hủy"))
            return;

        var report = new StringBuilder();
        var thieu  = new List<string>();
        int loi = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        try
        {
            Buoc(0, 8, "Điền thông số vào TouristBoatConfig…");
            if (!Buoc1_DienConfig(report)) loi++;

            Buoc(1, 8, "Dựng animation + prefab khách (132 ảnh, hơi lâu)…");
            if (!Buoc2_NPCAnimation(report)) loi++;

            Buoc(2, 8, "Dựng TouristSystem trong scene…");
            if (!Buoc3_TouristSystem(report)) loi++;

            Buoc(3, 8, "Ghi toạ độ đường đi thật…");
            if (!Buoc4_GhiToaDo(report)) loi++;

            Buoc(4, 8, "Dựng popup UI…");
            if (!Buoc5_Popup(report)) loi++;

            Buoc(5, 8, "Dịch bến sát bờ…");
            if (!Buoc6_DichBenSatBo(report)) loi++;

            Buoc(6, 8, "Tự kiểm tra…");
            Buoc7_TuKiemTra(report, thieu);

            Buoc(7, 8, "Đánh dấu scene đã đổi…");
            Buoc8_MarkDirty(report);
        }
        catch (Exception e)
        {
            loi++;
            report.AppendLine("✖ LỖI NGOÀI DỰ KIẾN: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        // ── Bảng tổng kết ───────────────────────────────────────────────
        var head = new StringBuilder();
        if (thieu.Count == 0 && loi == 0)
        {
            head.AppendLine("✔ XONG — hệ tàu khách đã sẵn sàng.");
            head.AppendLine();
            head.AppendLine("VIỆC DUY NHẤT CÒN LẠI: bấm Ctrl+S để lưu scene, rồi Play thử.");
        }
        else
        {
            head.AppendLine(loi > 0 ? $"⚠ XONG nhưng có {loi} bước lỗi." : "⚠ XONG nhưng CÒN THIẾU vài thứ.");
            head.AppendLine();
            if (thieu.Count > 0)
            {
                head.AppendLine("CÒN THIẾU:");
                for (int i = 0; i < thieu.Count; i++) head.AppendLine("• " + thieu[i]);
                head.AppendLine();
            }
            head.AppendLine("Đọc Console (lọc chữ TouristBoat) để xem chi tiết + cách khắc phục.");
            head.AppendLine("Đa số trường hợp: chạy lại chính menu này một lần nữa là hết.");
            head.AppendLine();
            head.AppendLine("Nhớ Ctrl+S nếu muốn giữ phần đã dựng được.");
        }

        Debug.Log("[TouristBoat] ★ SETUP TẤT CẢ — báo cáo đầy đủ:\n" + report);
        EditorUtility.DisplayDialog("★ Setup tất cả — Kết quả", head.ToString(), "OK");

        GameObject tourist = GameObject.Find(RootTourist);
        if (tourist != null)
        {
            Selection.activeGameObject = tourist;
            EditorGUIUtility.PingObject(tourist);
        }
    }

    private static void Buoc(int i, int tong, string mo)
    {
        EditorUtility.DisplayProgressBar("★ Setup tất cả hệ Tàu Du Lịch",
            $"Bước {i + 1}/{tong}: {mo}", (float)i / tong);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BƯỚC 1 — CONFIG
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Điền 13 field V2. Nhiều asset config trong project → ưu tiên ĐÚNG asset mà
    /// BoatDockManager trong scene đang tham chiếu (không thì điền nhầm cái không ai dùng),
    /// và log cảnh báo liệt kê các asset còn lại.
    /// </summary>
    private static bool Buoc1_DienConfig(StringBuilder report)
    {
        report.AppendLine("── BƯỚC 1: TouristBoatConfig ──");

        string[] guids = AssetDatabase.FindAssets("t:TouristBoatConfig");
        if (guids == null || guids.Length == 0)
        {
            report.AppendLine("✖ Không thấy asset TouristBoatConfig nào trong project.");
            report.AppendLine("   Khắc phục: chạy Tools/Farm Game/Tourist Boat/1. Setup All (Scene + Config) để tool V1 tạo asset.");
            Debug.LogError("[TouristBoat] Không có TouristBoatConfig — bước 1 thất bại.");
            return false;
        }

        // Asset mà scene đang thật sự dùng
        TouristBoatConfig chon = null;
        var mgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        if (mgr != null && mgr.Config != null) chon = mgr.Config;

        if (chon == null)
            chon = AssetDatabase.LoadAssetAtPath<TouristBoatConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));

        if (guids.Length > 1)
        {
            var ds = new StringBuilder();
            for (int i = 0; i < guids.Length; i++) ds.Append("\n     · " + AssetDatabase.GUIDToAssetPath(guids[i]));
            string canh = $"Có {guids.Length} asset TouristBoatConfig trong project — chỉ điền vào " +
                          $"'{AssetDatabase.GetAssetPath(chon)}' (asset BoatDockManager đang dùng). " +
                          "Các asset còn lại KHÔNG được đụng tới:" + ds;
            report.AppendLine("⚠ " + canh);
            Debug.LogWarning("[TouristBoat] " + canh);
        }

        if (chon == null)
        {
            report.AppendLine("✖ Không load được asset config.");
            return false;
        }

        Undo.RecordObject(chon, UndoLabel);

        chon.gapOneDockMinutes        = CfgGapOneDock;
        chon.gapMultiDockMinutes      = CfgGapMultiDock;
        chon.minStaggerMinutes        = CfgMinStagger;
        chon.maxDockMinutes           = CfgMaxDock;
        chon.visitorsMin              = CfgVisitorsMin;
        chon.visitorsMax              = CfgVisitorsMax;
        chon.patienceMinutes          = CfgPatience;
        chon.rewardIngredientMultiplier = CfgRewardMul;
        chon.disembarkInterval        = CfgDisembark;
        chon.visitorWalkSpeed         = CfgWalkSpeed;
        chon.queueSpacing             = CfgQueueSpacing;
        chon.bubbleScaleInTime        = CfgBubbleScaleIn;
        chon.smileyFlyTime            = CfgSmileyFly;

        EditorUtility.SetDirty(chon);
        AssetDatabase.SaveAssets();

        report.AppendLine("✔ Đã điền 13 thông số vào " + AssetDatabase.GetAssetPath(chon));
        report.AppendLine($"   gap 1 bến {CfgGapOneDock:0}' · nhiều bến {CfgGapMultiDock:0}' · so le {CfgMinStagger:0}' · " +
                          $"đậu tối đa {CfgMaxDock:0}' · khách {CfgVisitorsMin}-{CfgVisitorsMax} · " +
                          $"kiên nhẫn {CfgPatience:0}' · thưởng ×{CfgRewardMul}");

        // maxDockMinutes: default trong code Dev A vẫn là 30 — tool này set 35 vào ASSET.
        // (Đã nhờ Lead báo Dev A đổi default; KHÔNG tự sửa file của Dev A.)
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BƯỚC 2 & 3 — 2 tool của Dev B (gọi trực tiếp, chế độ quiet)
    // ─────────────────────────────────────────────────────────────────────

    private static bool Buoc2_NPCAnimation(StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine("── BƯỚC 2: animation + prefab khách ──");
        try
        {
            string kq = NPCAnimationSetupTool.RunSetup(quiet: true);
            report.AppendLine(kq);
            if (NPCAnimationSetupTool.LastCharacterCount <= 0)
            {
                report.AppendLine("✖ Không dựng được nhân vật nào.");
                return false;
            }
            report.AppendLine($"✔ {NPCAnimationSetupTool.LastCharacterCount}/11 nhân vật OK.");
            return true;
        }
        catch (Exception e)
        {
            report.AppendLine("✖ Lỗi khi dựng animation: " + e.Message);
            Debug.LogException(e);
            return false;
        }
    }

    private static bool Buoc3_TouristSystem(StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine("── BƯỚC 3: TouristSystem trong scene ──");
        try
        {
            // [QA M-8] Ghi nhận TRƯỚC khi dựng: mốc nào đã tồn tại (có thể do Sếp kéo tay),
            // mốc nào chưa. Sau khi dựng, chỉ ĐÓNG DẤU VẾT cho mốc VỪA ĐƯỢC TẠO — nhờ vậy
            // bước 4 phân biệt được "tool vừa sinh" (được phép ghi tiếp) với "Sếp đã kéo".
            bool[] pathDaCoTruoc = new bool[DockTotal];
            bool anchorDaCoTruoc = false;
            Transform tsCu = FindTouristRoot();
            if (tsCu != null)
            {
                anchorDaCoTruoc = tsCu.Find("QueueAnchor") != null;
                for (int d = 0; d < DockTotal; d++)
                {
                    Transform pt = tsCu.Find($"TouristPath_Dock{d + 1:00}");
                    pathDaCoTruoc[d] = pt != null && pt.childCount > 0;
                }
            }

            report.AppendLine(TouristVisitorSetupTool.RunSetup(quiet: true));
            DongDauVetChoMocMoi(pathDaCoTruoc, anchorDaCoTruoc);

            DonTenSortingLayerCu(report);
            WireHudGoldTarget(report);
            return GameObject.Find(RootTourist) != null;
        }
        catch (Exception e)
        {
            report.AppendLine("✖ Lỗi khi dựng TouristSystem: " + e.Message);
            Debug.LogException(e);
            return false;
        }
    }

    /// <summary>
    /// WIRE ĐÍCH BAY CỦA MẶT CƯỜI = ô VÀNG trên HUD.
    ///
    /// [Lead chốt 2026-08-29] Không wire thì FX phải dò tên lúc chạy, dò trượt là bay
    /// lung tung. Wire cứng một lần ở đây thì chắc chắn đúng.
    ///
    /// Cách dò (chỉ trong canvas HUD có sortingOrder cao nhất, bỏ qua canvas popup):
    ///   ① object có tên chứa "gold"/"vang"/"coin"/"tien" VÀ có Image hoặc TMP_Text
    ///   ② icon vàng: tên sprite của Image chứa "gold"/"vang"/"coin"
    ///   ③ không thấy → để trống, FX sẽ tự bay lên trời (vẫn đúng ý Sếp)
    /// Chỉ ghi khi field đang TRỐNG — không đè lựa chọn tay của Sếp.
    /// </summary>
    private static void WireHudGoldTarget(StringBuilder report)
    {
        var vm = UnityEngine.Object.FindFirstObjectByType<TouristVisitorManager>(FindObjectsInactive.Include);
        if (vm == null) return;

        var so = new SerializedObject(vm);
        SerializedProperty p = so.FindProperty("hudGoldTarget");
        if (p == null) return;

        if (p.objectReferenceValue != null)
        {
            report.AppendLine("✔ hudGoldTarget: đã gắn từ trước (" + p.objectReferenceValue.name + ") — giữ nguyên.");
            return;
        }

        Transform target = TimOVangHud(out string cachTim);
        if (target == null)
        {
            report.AppendLine("⚠ Không tìm được ô vàng HUD để mặt cười bay tới — " +
                              "FX sẽ cho mặt cười BAY THẲNG LÊN TRỜI (vẫn đúng ý đồ, không bay về giữa màn hình). " +
                              "Muốn bay về ví tiền: kéo tay object ô vàng vào field 'hudGoldTarget' của TouristVisitorManager.");
            return;
        }

        Undo.RecordObject(vm, UndoLabel);
        p.objectReferenceValue = target;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(vm);

        report.AppendLine($"✔ hudGoldTarget → \"{target.name}\" ({cachTim})");
        report.AppendLine("      đường dẫn: " + DuongDanHierarchy(target));
    }

    /// <summary>Dò ô vàng trong canvas HUD. Trả null nếu không có gì đáng tin.</summary>
    private static Transform TimOVangHud(out string cachTim)
    {
        cachTim = string.Empty;

        // Canvas HUD = canvas root có sortingOrder cao nhất trong số canvas ĐANG BẬT.
        Canvas hud = null;
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c == null || !c.isRootCanvas) continue;
            // Bỏ qua canvas popup của Dev C — mặt cười phải bay về HUD, không về popup.
            if (c.gameObject.name.ToLowerInvariant().Contains("popup")) continue;
            if (hud == null || c.sortingOrder > hud.sortingOrder) hud = c;
        }
        if (hud == null) return null;

        string[] goiY = { "gold", "vang", "coin", "tien" };
        RectTransform[] all = hud.GetComponentsInChildren<RectTransform>(true);

        // ① Tên khớp + có thành phần hiển thị (Image / TMP_Text)
        for (int g = 0; g < goiY.Length; g++)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (!all[i].name.ToLowerInvariant().Contains(goiY[g])) continue;

                bool coHinh = all[i].GetComponent<UnityEngine.UI.Image>() != null ||
                              all[i].GetComponent<TMPro.TMP_Text>() != null;
                if (!coHinh) continue;

                cachTim = "khớp tên \"" + goiY[g] + "\" trong canvas " + hud.gameObject.name;
                return all[i];
            }
        }

        // ② Icon vàng: tên SPRITE của Image khớp gợi ý
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            var img = all[i].GetComponent<UnityEngine.UI.Image>();
            if (img == null || img.sprite == null) continue;

            string ten = img.sprite.name.ToLowerInvariant();
            for (int g = 0; g < goiY.Length; g++)
            {
                if (!ten.Contains(goiY[g])) continue;
                cachTim = "khớp tên sprite \"" + img.sprite.name + "\"";
                return all[i];
            }
        }

        return null;
    }

    private static string DuongDanHierarchy(Transform t)
    {
        if (t == null) return "(null)";
        string s = t.name;
        Transform p = t.parent;
        while (p != null) { s = p.name + "/" + s; p = p.parent; }
        return s;
    }

    /// <summary>
    /// DỌN TÊN SORTING LAYER CŨ còn sót trong Inspector (scene + prefab khách).
    ///
    /// [BUG Sếp gặp 2026-08-29] Lưới an toàn TouristSortingLayers chạy đúng, nhưng giá
    /// trị "CongTrinh" vẫn nằm trong field serialize của TouristVisitorManager /
    /// TouristAgent / bubble / gangplank ⇒ MỖI LẦN spawn khách lại in một dòng cảnh báo
    /// "layer không tồn tại", làm ngập Console.
    /// Cách dọn: field nào đang giữ tên layer KHÔNG tồn tại thì ghi về RỖNG
    /// (= "tự chọn layer đúng"). Tên layer CÓ THẬT thì giữ nguyên — tôn trọng chỉnh tay.
    /// </summary>
    private static void DonTenSortingLayerCu(StringBuilder report)
    {
        string[] tenField = { "sortingLayerName", "fxSortingLayerName" };
        int daDon = 0;

        // 1 · Component trong scene
        foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string ten = mb.GetType().Name;
            if (ten != "TouristVisitorManager" && ten != "TouristAgent" &&
                ten != "TouristRequestBubble" && ten != "GangplankController") continue;

            if (DonFieldLayer(mb, tenField, UndoLabel)) daDon++;
        }

        // 2 · Prefab khách (giá trị nằm trong file .prefab, scene không chạm tới được)
        if (AssetDatabase.IsValidFolder(PrefabTouristRoot))
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab Tourist_NV", new[] { PrefabTouristRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (go == null) continue;

                foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
                {
                    if (mb == null) continue;
                    if (DonFieldLayer(mb, tenField, null)) daDon++;
                }
            }
            AssetDatabase.SaveAssets();
        }

        report.AppendLine(daDon > 0
            ? $"✔ Dọn {daDon} field sorting layer giữ tên không tồn tại (vd \"CongTrinh\") → để trống = tự chọn đúng. Hết spam cảnh báo."
            : "✔ Không field sorting layer nào giữ tên sai — Console sẽ không bị spam.");
    }

    /// <summary>Ghi rỗng cho các field layer đang giữ tên KHÔNG tồn tại. Trả true nếu có sửa.</summary>
    private static bool DonFieldLayer(MonoBehaviour mb, string[] tenField, string undoLabel)
    {
        var so = new SerializedObject(mb);
        bool doi = false;

        for (int i = 0; i < tenField.Length; i++)
        {
            SerializedProperty p = so.FindProperty(tenField[i]);
            if (p == null || p.propertyType != SerializedPropertyType.String) continue;
            if (string.IsNullOrEmpty(p.stringValue)) continue;
            if (TouristSortingLayers.Exists(p.stringValue)) continue; // tên có thật — tôn trọng

            if (!string.IsNullOrEmpty(undoLabel)) Undo.RecordObject(mb, undoLabel);
            p.stringValue = string.Empty;
            doi = true;
        }

        if (doi)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(mb);
        }
        return doi;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BƯỚC 4 — GHI ĐÈ TOẠ ĐỘ THẬT (điểm mấu chốt: Sếp hết phải kéo tay)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Đặt QueueAnchor + waypoint đường đi bộ.
    ///
    /// [QA M-8 · 2026-08-29] TÔN TRỌNG CHỈNH TAY CỦA SẾP. Bản trước ghi
    /// <c>wp.position = …</c> VÔ ĐIỀU KIỆN — cờ <c>bamDuongDat</c> chỉ đổi NGUỒN toạ độ
    /// (Dijkstra hay mốc thẳng) chứ không đổi việc CÓ ĐÈ hay không; tắt cờ còn tệ hơn
    /// (đè bằng 3 mốc thẳng hardcode). Sếp kéo waypoint bám đường đất rồi Ctrl+S, bấm lại
    /// nút ★ là mất sạch — mà báo cáo lại nói "không đè gì".
    ///
    /// Nay mỗi mốc chỉ bị ghi khi RƠI VÀO MỘT TRONG BA:
    ///   ① vừa được TẠO MỚI trong lần chạy này (chưa từng có);
    ///   ② còn nằm ĐÚNG chỗ tool sinh lần trước — đối chiếu bằng DẤU VẾT toạ độ
    ///      (<see cref="DauVetToaDo"/>) lưu trong EditorPrefs theo scene + bến;
    ///   ③ Sếp tự bật ô tick <see cref="GhiDeWaypointChinhTay"/> (mặc định TẮT).
    /// Ngoài ra → BỎ QUA + log rõ để Sếp biết vì sao không đổi.
    /// Áp dụng cho cả QueueAnchor (Sếp cũng được dặn kéo nó trong HANDOFF).
    /// </summary>
    private static bool Buoc4_GhiToaDo(StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine("── BƯỚC 4: ghi toạ độ đường đi thật (đo từ SCN_Farm) ──");

        GameObject touristRoot = GameObject.Find(RootTourist);
        if (touristRoot == null)
        {
            report.AppendLine("✖ Không thấy " + RootTourist + " — bước 3 chưa chạy được, bỏ qua ghi toạ độ.");
            return false;
        }

        // QueueAnchor
        Transform anchor = touristRoot.transform.Find("QueueAnchor");
        if (anchor == null)
        {
            report.AppendLine("✖ Thiếu QueueAnchor dưới " + RootTourist + ".");
            return false;
        }
        string khoaAnchor = KhoaDauVet("anchor");
        string vetAnchorHienTai = DauVetToaDo(new[] { anchor.position });
        bool anchorDaKeoTay = EditorPrefs.HasKey(khoaAnchor) &&
                              EditorPrefs.GetString(khoaAnchor, string.Empty) != vetAnchorHienTai;
        // Chưa có dấu vết = tool chưa từng ghi mốc này ⇒ coi như CHƯA đụng, được phép đặt lần đầu.

        if (anchorDaKeoTay && !GhiDeWaypointChinhTay)
        {
            report.AppendLine($"• QueueAnchor đang ở ({anchor.position.x:0}, {anchor.position.y:0}) — " +
                              "BẠN ĐÃ KÉO TAY nên GIỮ NGUYÊN. Muốn đưa về vị trí tool tính: " +
                              "tick menu \"⚙ Ghi đè waypoint đã chỉnh tay\" rồi chạy lại.");
        }
        else
        {
            Undo.RecordObject(anchor, UndoLabel);
            Vector3 cu = anchor.position;
            anchor.position = new Vector3(QueueAnchorPos.x, QueueAnchorPos.y, anchor.position.z);
            EditorUtility.SetDirty(anchor);
            EditorPrefs.SetString(khoaAnchor, DauVetToaDo(new[] { anchor.position }));
            report.AppendLine($"✔ QueueAnchor: ({cu.x:0}, {cu.y:0}) → ({QueueAnchorPos.x:0}, {QueueAnchorPos.y:0}) — trước cửa CookingGate." +
                              (anchorDaKeoTay ? "  (ghi đè theo yêu cầu)" : ""));
        }

        // ── 3 path × N WP ──
        // [Lead chốt 2026-08-29 — việc 4] Ưu tiên TÌM ĐƯỜNG BÁM TILEMAP ĐẤT thật
        // (Dijkstra 8 hướng, đất rẻ / cỏ đắt, rút gọn Douglas-Peucker). Bảng 3 mốc thẳng
        // bên dưới chỉ còn là ĐƯỜNG DỰ PHÒNG khi không đọc được tilemap.
        int daDat = 0, taoMoi = 0, xoaThua = 0, benBamDat = 0, benDuPhong = 0, benGiuNguyen = 0;
        bool batBamDat = TouristVisitorSetupTool.BamDuongDat;

        if (!batBamDat)
            report.AppendLine("• Cờ \"Bám đường đất khi setup\" đang TẮT — nguồn toạ độ là 3 mốc thẳng.");
        if (GhiDeWaypointChinhTay)
            report.AppendLine("• ⚠ Ô tick \"Ghi đè waypoint đã chỉnh tay\" đang BẬT — mọi mốc bạn kéo tay SẼ BỊ GHI LẠI.");

        for (int d = 0; d < DockWaypoints.Length; d++)
        {
            string pathName = $"TouristPath_Dock{d + 1:00}";
            Transform path = touristRoot.transform.Find(pathName);
            if (path == null)
            {
                report.AppendLine($"✖ Thiếu {pathName} — bến {d + 1} chưa có đường đi.");
                continue;
            }

            // Điểm bắt đầu = ĐẦU BỜ của tấm gỗ (khớp đúng cách manager dựng đường lúc chạy)
            Vector3 batDau = DiemDauDuongDiBo(d, anchor.position);

            List<Vector3> wps = null;
            if (batBamDat)
            {
                var kq = TouristVisitorSetupTool.TimDuongBamDat(batDau, anchor.position);
                if (kq.ThanhCong)
                {
                    wps = kq.Waypoints;
                    benBamDat++;
                    report.AppendLine($"✔ {pathName}: BÁM ĐƯỜNG ĐẤT — {kq.MoTaNgan()}");
                    if (kq.TiLeCo > 0.4f)
                        report.AppendLine($"   ⚠ {kq.TiLeCo * 100f:0}% quãng đường đi trên CỎ — " +
                                          "đường đất có thể không nối tới nhà hàng, bạn kiểm lại tilemap " +
                                          "Tilemap_IsoDirt (hoặc vẽ thêm đoạn nối).");
                }
                else
                {
                    report.AppendLine($"⚠ {pathName}: không bám được đường đất ({kq.LyDoThatBai}) — " +
                                      "dùng 3 mốc thẳng dự phòng.");
                    Debug.LogWarning($"[TouristBoat] {pathName}: {kq.LyDoThatBai} — rơi về 3 mốc thẳng.");
                }
            }

            if (wps == null)
            {
                wps = new List<Vector3>();
                for (int k = 0; k < DockWaypoints[d].Length; k++)
                    wps.Add(new Vector3(DockWaypoints[d][k].x, DockWaypoints[d][k].y, 0f));
                benDuPhong++;
                if (batBamDat == false)
                    report.AppendLine($"• {pathName}: 3 mốc thẳng (toạ độ đo từ scene).");
            }

            // ── [QA M-8] Có được phép ghi lên path này không? ──
            //   • path RỖNG            → tạo mới, đương nhiên được ghi
            //   • dấu vết KHỚP         → mốc vẫn đúng chỗ tool sinh lần trước, ghi tiếp được
            //   • dấu vết LỆCH/không có→ Sếp đã kéo tay ⇒ GIỮ NGUYÊN (trừ khi bật ô ghi đè)
            string khoa = KhoaDauVet("dock" + d);
            bool pathRong = path.childCount == 0;
            bool khopDauVet = pathRong ||
                              (EditorPrefs.HasKey(khoa) &&
                               EditorPrefs.GetString(khoa, string.Empty) == DauVetToaDo(LayToaDoCon(path)));

            if (!pathRong && !khopDauVet && !GhiDeWaypointChinhTay)
            {
                benGiuNguyen++;
                report.AppendLine($"• {pathName}: waypoint ĐÃ ĐƯỢC CHỈNH TAY ({path.childCount} WP) — GIỮ NGUYÊN, không ghi đè.");
                report.AppendLine("      Muốn dựng lại theo đường đất: tick menu " +
                                  "\"⚙ Ghi đè waypoint đã chỉnh tay\" rồi chạy lại nút ★.");
                continue;
            }

            // Xoá WP thừa (số WP thay đổi giữa các lần chạy / bản tool cũ sinh 4 cái)
            for (int c = path.childCount - 1; c >= wps.Count; c--)
            {
                Undo.DestroyObjectImmediate(path.GetChild(c).gameObject);
                xoaThua++;
            }

            for (int k = 0; k < wps.Count; k++)
            {
                Transform wp = k < path.childCount ? path.GetChild(k) : null;
                if (wp == null)
                {
                    var go = new GameObject($"WP_{k + 1:00}");
                    Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                    go.transform.SetParent(path, true);
                    wp = go.transform;
                    taoMoi++;
                }

                Undo.RecordObject(wp, UndoLabel);
                wp.name     = $"WP_{k + 1:00}"; // đổi tên luôn cho đúng thứ tự sắp xếp
                wp.position = new Vector3(wps[k].x, wps[k].y, wp.position.z);
                EditorUtility.SetDirty(wp);
                daDat++;
            }

            // Đóng dấu vết để lần chạy sau biết "mốc này do tool sinh, chưa ai kéo".
            EditorPrefs.SetString(khoa, DauVetToaDo(LayToaDoCon(path)));

            if (!pathRong && !khopDauVet && GhiDeWaypointChinhTay)
                report.AppendLine($"   ⚠ {pathName}: đã GHI ĐÈ waypoint bạn từng chỉnh tay (do ô tick đang bật).");
        }

        report.AppendLine($"   Tổng: {daDat} waypoint đã đặt" +
                          (taoMoi > 0 ? $", {taoMoi} tạo mới" : "") +
                          (xoaThua > 0 ? $", {xoaThua} WP thừa đã xoá" : "") +
                          $" · {benBamDat} bến bám tilemap đất, {benDuPhong} bến dùng mốc thẳng dự phòng" +
                          (benGiuNguyen > 0 ? $", {benGiuNguyen} bến GIỮ NGUYÊN vì bạn đã kéo tay" : "") + ".");
        if (benBamDat > 0)
            report.AppendLine("   Đường tự bám Tilemap_IsoDirt (đất 1 · cầu tàu 2 · cát 5 · cỏ 9), " +
                              "rút gọn Douglas-Peucker. Vẫn kéo tay tinh chỉnh được — " +
                              "muốn khỏi bị ghi đè thì tắt menu \"⚙ Bám đường đất khi setup\".");
        return true;
    }

    /// <summary>
    /// [QA M-8] Đóng dấu vết cho những mốc mà bước 3 VỪA TẠO (trước đó chưa có).
    /// Mốc đã tồn tại từ trước thì KHÔNG đóng dấu — giữ nguyên khả năng nhận ra
    /// "Sếp đã kéo tay" ở bước 4.
    /// </summary>
    private static void DongDauVetChoMocMoi(bool[] pathDaCoTruoc, bool anchorDaCoTruoc)
    {
        Transform ts = FindTouristRoot();
        if (ts == null) return;

        if (!anchorDaCoTruoc)
        {
            Transform a = ts.Find("QueueAnchor");
            if (a != null) EditorPrefs.SetString(KhoaDauVet("anchor"), DauVetToaDo(new[] { a.position }));
        }

        for (int d = 0; d < DockTotal && d < pathDaCoTruoc.Length; d++)
        {
            if (pathDaCoTruoc[d]) continue;
            Transform pt = ts.Find($"TouristPath_Dock{d + 1:00}");
            if (pt != null && pt.childCount > 0)
                EditorPrefs.SetString(KhoaDauVet("dock" + d), DauVetToaDo(LayToaDoCon(pt)));
        }
    }

    private static Transform FindTouristRoot()
    {
        GameObject go = GameObject.Find(RootTourist);
        return go != null ? go.transform : null;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  [QA M-8] DẤU VẾT TOẠ ĐỘ — phân biệt "mốc do tool sinh" với "mốc Sếp kéo tay"
    // ─────────────────────────────────────────────────────────────────────
    //
    // VÌ SAO DÙNG EditorPrefs CHỨ KHÔNG THÊM COMPONENT ĐÁNH DẤU VÀO SCENE:
    //   • không phải thêm file runtime mới chỉ để ghi sổ cho Editor;
    //   • không làm bẩn scene/prefab, không rủi ro serialize;
    //   • MẶC ĐỊNH AN TOÀN: máy khác (hoặc EditorPrefs bị xoá) thì KHÔNG có dấu vết ⇒
    //     tool coi như mốc đã bị chỉnh tay ⇒ GIỮ NGUYÊN. Thà bỏ qua còn hơn xoá công của Sếp.
    // Dấu vết là băm FNV-1a của toạ độ đã LÀM TRÒN 1 unit — kéo lệch ≥1 unit là phát hiện,
    // còn sai số float khi Unity serialize lại thì không tính là "đã kéo".

    private const string KhoaGhiDeWaypoint = "TouristBoat_GhiDeWaypointChinhTay";

    /// <summary>
    /// Ô tick "Ghi đè waypoint đã chỉnh tay" — MẶC ĐỊNH TẮT. Bật thì nút ★ được phép
    /// ghi lại cả mốc Sếp đã kéo (dùng khi muốn dựng lại từ đầu theo đường đất).
    /// </summary>
    public static bool GhiDeWaypointChinhTay
    {
        get { return EditorPrefs.GetBool(KhoaGhiDeWaypoint, false); }
        set { EditorPrefs.SetBool(KhoaGhiDeWaypoint, value); }
    }

    [MenuItem("Tools/Farm Game/Tourist Boat/⚙ Ghi đè waypoint đã chỉnh tay (nguy hiểm)", false, 6)]
    private static void ToggleGhiDeWaypoint()
    {
        GhiDeWaypointChinhTay = !GhiDeWaypointChinhTay;
        Debug.Log("[TouristBoat] Ghi đè waypoint đã chỉnh tay: " +
                  (GhiDeWaypointChinhTay ? "BẬT — nút ★ sẽ ghi lại cả mốc bạn đã kéo!"
                                         : "TẮT (an toàn) — nút ★ giữ nguyên mốc bạn đã kéo."));
    }

    [MenuItem("Tools/Farm Game/Tourist Boat/⚙ Ghi đè waypoint đã chỉnh tay (nguy hiểm)", true)]
    private static bool ToggleGhiDeWaypointValidate()
    {
        Menu.SetChecked("Tools/Farm Game/Tourist Boat/⚙ Ghi đè waypoint đã chỉnh tay (nguy hiểm)",
                        GhiDeWaypointChinhTay);
        return true;
    }

    /// <summary>Khoá EditorPrefs riêng cho từng SCENE + từng mốc (2 scene khác nhau không đè dấu vết nhau).</summary>
    private static string KhoaDauVet(string phanTu)
    {
        string scene = EditorSceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(scene)) scene = "(scene-chua-luu)";
        return "TouristBoat_DauVetWP_" + scene + "_" + phanTu;
    }

    /// <summary>Toạ độ các con của một node, theo thứ tự hiện có.</summary>
    private static Vector3[] LayToaDoCon(Transform node)
    {
        if (node == null) return new Vector3[0];
        var ds = new Vector3[node.childCount];
        for (int i = 0; i < node.childCount; i++)
            ds[i] = node.GetChild(i) != null ? node.GetChild(i).position : Vector3.zero;
        return ds;
    }

    /// <summary>
    /// Băm FNV-1a của danh sách toạ độ (làm tròn 1 unit). Tự cài thay vì
    /// <c>string.GetHashCode</c> vì hash chuỗi của .NET KHÔNG ổn định giữa các phiên bản
    /// runtime — dấu vết lưu trên đĩa thì phải tái lập được mãi.
    /// </summary>
    private static string DauVetToaDo(Vector3[] diem)
    {
        if (diem == null || diem.Length == 0) return "rong";

        unchecked
        {
            uint h = 2166136261u;
            for (int i = 0; i < diem.Length; i++)
            {
                long x = (long)Mathf.Round(diem[i].x);
                long y = (long)Mathf.Round(diem[i].y);
                h = BamSo(h, x);
                h = BamSo(h, y);
            }
            return diem.Length + ":" + h.ToString("x8");
        }
    }

    private static uint BamSo(uint h, long v)
    {
        unchecked
        {
            for (int b = 0; b < 8; b++)
            {
                h ^= (uint)((v >> (b * 8)) & 0xFF);
                h *= 16777619u;
            }
            return h;
        }
    }

    /// <summary>
    /// Điểm bắt đầu đường đi bộ của bến = ĐẦU BỜ của tấm gỗ (đo từ bounds sprite),
    /// khớp đúng cách <c>TouristVisitorManager.GetPathPoints</c> dựng đường lúc chạy.
    /// Không có gangplank → lấy Berth; không có cả Berth → lùi từ đích về phía bến.
    /// </summary>
    private static Vector3 DiemDauDuongDiBo(int dock, Vector3 dich)
    {
        GameObject boatRoot = TimBoatSystem();
        Transform gp = null, berth = null;
        if (boatRoot != null)
        {
            Transform d = boatRoot.transform.Find($"Dock_{dock + 1:00}");
            if (d != null) { gp = d.Find("Gangplank"); berth = d.Find("Berth"); }
        }

        if (gp != null)
        {
            Vector3 p = gp.position;
            var sr = gp.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) p += Vector3.up * (sr.bounds.size.y * 0.5f);
            return p;
        }

        if (berth != null)
            return berth.position + new Vector3(0f, TouristVisitorSetupTool.GangplankWorldLength, 0f);

        // Không có gì trong scene — dùng bảng toạ độ Berth gốc đã dịch sát bờ.
        return new Vector3(BerthOriginal[dock].x,
                           BerthOriginal[dock].y + BoatShoreAdjustTool.DefaultShoreOffset.y
                           + TouristVisitorSetupTool.GangplankWorldLength, 0f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BƯỚC 5 — POPUP (Dev C, gọi qua reflection)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi <c>TouristBoatUIPopupSetupTool.SetupPopups(bool quiet)</c> của Dev C.
    ///
    /// VẪN DÙNG REFLECTION (không tham chiếu thẳng) có chủ ý: gói Dev C chưa copy vào
    /// project thì tool này vẫn biên dịch và chỉ bỏ qua bước popup, thay vì làm hỏng
    /// cả nút "setup tất cả".
    ///
    /// [Dev C bắt lỗi 2026-08-29] Bản trước tra <c>GetMethod("SetupPopups", Public|Static)</c>
    /// — tra CHỈ THEO TÊN, và invoke lại nằm NGOÀI khối try:
    ///   • có 2 overload public cùng tên ⇒ AmbiguousMatchException;
    ///   • gọi không tham số vào hàm nhận bool ⇒ TargetParameterCountException;
    ///   • cả hai đều văng ra ngoài, giết luôn các bước 6-8 phía sau.
    /// Nay: tra ĐÍCH DANH theo chữ ký <c>new[] { typeof(bool) }</c>, truyền
    /// <c>quiet: true</c> (chỉ có ĐÚNG 1 bảng tổng kết cuối), và MỌI lời gọi reflection
    /// đều nằm trong try.
    /// </summary>
    private static bool Buoc5_Popup(StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine("── BƯỚC 5: popup UI (gói Dev C) ──");

        try
        {
            Type t = TimKieu("TouristBoatUIPopupSetupTool");
            if (t == null)
            {
                report.AppendLine("⚠ Chưa có TouristBoatUIPopupSetupTool trong project — BỎ QUA bước popup.");
                report.AppendLine("   Khắc phục: copy gói Dev C vào project rồi chạy lại menu này.");
                return true; // không tính là lỗi của hệ khách
            }

            // Tra ĐÍCH DANH theo chữ ký (bool) — không bao giờ nhập nhằng overload.
            MethodInfo m = t.GetMethod("SetupPopups", new[] { typeof(bool) });
            if (m == null)
            {
                report.AppendLine("⚠ TouristBoatUIPopupSetupTool không có hàm public static SetupPopups(bool) — bỏ qua.");
                report.AppendLine("   Khắc phục: cập nhật gói Dev C lên bản mới nhất.");
                return true;
            }

            string reportC = (string)m.Invoke(null, new object[] { true }); // quiet = true
            if (!string.IsNullOrEmpty(reportC)) report.AppendLine(reportC);
            report.AppendLine("✔ Đã dựng 2 popup (báo tàu + mua bến) — report của Dev C ở ngay trên.");
            return true;
        }
        catch (Exception e)
        {
            Exception that = e.InnerException ?? e; // reflection bọc lỗi thật trong TargetInvocationException
            report.AppendLine("✖ Lỗi khi dựng popup: " + that.Message);
            report.AppendLine("   Khắc phục: chạy tay Tools/Farm Game/Tourist Boat/Setup Popups (UI) để xem lỗi đầy đủ.");
            Debug.LogException(that);
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BƯỚC 6 — DỊCH BẾN SÁT BỜ + ĐẶT LẠI GANGPLANK
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dịch Berth 3 bến sát bờ rồi ĐẶT LẠI Gangplank theo Berth mới (gangplank là con
    /// của Dock chứ không phải của Berth nên không tự đi theo).
    ///
    /// [Dev A đã mở API 2026-08-29] Gọi THẲNG
    /// <c>BoatShoreAdjustTool.ApplyShoreOffset(DefaultShoreOffset, recordUndo: false)</c> —
    /// bỏ hẳn reflection và bỏ bản sao logic dịch bến của bản trước. Truyền
    /// <c>recordUndo: false</c> vì tool này tự gom TẤT CẢ 8 bước vào MỘT Undo group;
    /// đổi lại, ta phải tự <c>Undo.RecordObject</c> các transform Dev A sắp ghi
    /// (Berth + N waypoint đuôi) — làm trong <see cref="GhiUndoTruocKhiDich"/>.
    ///
    /// IDEMPOTENT: chỉ dịch khi TẤT CẢ Berth còn đang ở gần toạ độ GỐC (bảng
    /// <see cref="BerthOriginal"/>, sai số <see cref="ShoreEpsilon"/>). Đã dịch rồi —
    /// hoặc Sếp tự kéo — thì BỎ QUA, không cộng dồn +200 mỗi lần chạy.
    /// </summary>
    private static bool Buoc6_DichBenSatBo(StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine("── BƯỚC 6: dịch 3 bến sát bờ ──");

        GameObject boatRoot = TimBoatSystem();
        if (boatRoot == null)
        {
            report.AppendLine("✖ Không thấy " + RootBoat + " trong scene — bỏ qua.");
            report.AppendLine("   Khắc phục: chạy Tools/Farm Game/Tourist Boat/1. Setup All (Scene + Config).");
            return false;
        }

        // Đối chiếu hướng bờ Dev A suy ra với hướng ta ghi cứng — lệch nhiều là dấu hiệu
        // scene đã đổi layout, in ra để Sếp biết chứ không tự đổi hướng.
        try
        {
            Vector2 doan = BoatShoreAdjustTool.GuessShoreDirection();
            report.AppendLine($"   (Hướng bờ Dev A suy từ scene: ({doan.x:0.00}, {doan.y:0.00}) — " +
                              $"tool dùng offset ({BoatShoreAdjustTool.DefaultShoreOffset.x:0}, " +
                              $"{BoatShoreAdjustTool.DefaultShoreOffset.y:0}).");
            if (doan.y < 0.5f)
                report.AppendLine("   ⚠ Hướng bờ suy ra KHÔNG nghiêng hẳn về +Y — layout scene có thể đã đổi, " +
                                  "kiểm mắt sau khi chạy xong.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TouristBoat] GuessShoreDirection lỗi (bỏ qua, không chặn): " + e.Message);
        }

        // ── Kiểm idempotent trước khi đụng gì ──
        int coBerth = 0, conGoc = 0;
        for (int i = 0; i < BerthOriginal.Length; i++)
        {
            Transform berth = TimBerth(boatRoot, i);
            if (berth == null)
            {
                report.AppendLine($"⚠ Dock_{i + 1:00}: thiếu Berth — bỏ qua bến này.");
                continue;
            }
            coBerth++;

            float dx = berth.position.x - BerthOriginal[i].x;
            float dy = berth.position.y - BerthOriginal[i].y;
            if (Mathf.Sqrt(dx * dx + dy * dy) <= ShoreEpsilon) conGoc++;
        }

        if (coBerth == 0)
        {
            report.AppendLine("✖ Không bến nào có Berth — không dịch được gì.");
            return false;
        }

        if (conGoc == 0)
        {
            report.AppendLine($"• Cả {coBerth} bến đã KHÔNG còn ở toạ độ gốc (đã dịch lần trước, hoặc bạn tự kéo) " +
                              "— GIỮ NGUYÊN, không cộng dồn.");
            DatLaiTatCaGangplank(boatRoot, report);
            return true;
        }

        if (conGoc < coBerth)
        {
            report.AppendLine($"⚠ Chỉ {conGoc}/{coBerth} bến còn ở toạ độ gốc — BỎ QUA bước dịch để không làm 3 bến lệch nhau.");
            report.AppendLine("   Khắc phục: dùng menu Tools/Farm Game/Tourist Boat/Dịch bến sát bờ (Dev A) để canh tay từng bến.");
            DatLaiTatCaGangplank(boatRoot, report);
            return true;
        }

        try
        {
            // recordUndo: false ⇒ TA tự ghi Undo cho đúng những transform Dev A sắp đổi.
            GhiUndoTruocKhiDich(boatRoot);

            int soBen = BoatShoreAdjustTool.ApplyShoreOffset(
                BoatShoreAdjustTool.DefaultShoreOffset, recordUndo: false);

            if (soBen <= 0)
            {
                report.AppendLine("✖ BoatShoreAdjustTool.ApplyShoreOffset trả 0 bến — xem Console của Dev A.");
                return false;
            }

            report.AppendLine($"✔ Đã dịch {soBen} bến sát bờ " +
                              $"({BoatShoreAdjustTool.DefaultShoreOffset.x:0}, {BoatShoreAdjustTool.DefaultShoreOffset.y:0}) " +
                              $"+ {BoatShoreAdjustTool.DefaultTailWaypointCount} waypoint đuôi mỗi bến (API Dev A).");
            DatLaiTatCaGangplank(boatRoot, report);
            return true;
        }
        catch (Exception e)
        {
            report.AppendLine("✖ Lỗi khi dịch bến: " + e.Message);
            Debug.LogException(e);
            return false;
        }
    }

    /// <summary>
    /// Ghi Undo cho mọi transform mà <c>ApplyShoreOffset(recordUndo:false)</c> sắp ghi đè:
    /// Berth của từng bến + <c>DefaultTailWaypointCount</c> waypoint CUỐI của Path bến đó.
    /// Không có bước này thì Ctrl+Z không hoàn tác được phần dịch bến.
    /// </summary>
    private static void GhiUndoTruocKhiDich(GameObject boatRoot)
    {
        for (int i = 0; i < BerthOriginal.Length; i++)
        {
            Transform dock = boatRoot.transform.Find($"Dock_{i + 1:00}");
            if (dock == null) continue;

            Transform berth = dock.Find("Berth");
            if (berth != null) Undo.RecordObject(berth, UndoLabel);

            Transform path = dock.Find("Path");
            if (path == null || path.childCount == 0) continue;

            int take = Mathf.Clamp(BoatShoreAdjustTool.DefaultTailWaypointCount, 1, path.childCount);
            for (int k = path.childCount - take; k < path.childCount; k++)
            {
                Transform wp = path.GetChild(k);
                if (wp != null) Undo.RecordObject(wp, UndoLabel);
            }
        }
    }

    /// <summary>Đặt lại Gangplank của cả 3 bến theo Berth hiện tại.</summary>
    private static void DatLaiTatCaGangplank(GameObject boatRoot, StringBuilder report)
    {
        for (int i = 0; i < BerthOriginal.Length; i++)
        {
            Transform dock  = boatRoot.transform.Find($"Dock_{i + 1:00}");
            Transform berth = dock != null ? dock.Find("Berth") : null;
            DatLaiGangplank(dock, berth, report, i);
        }
    }

    private static Transform TimBerth(GameObject boatRoot, int index)
    {
        Transform dock = boatRoot.transform.Find($"Dock_{index + 1:00}");
        return dock != null ? dock.Find("Berth") : null;
    }

    /// <summary>
    /// Đặt Gangplank nằm giữa Berth và hướng vào bờ (+Y) — gọi lại sau khi Berth dịch
    /// để tấm gỗ không bị lệch khỏi mạn tàu.
    /// </summary>
    private static void DatLaiGangplank(Transform dock, Transform berth, StringBuilder report, int index)
    {
        if (dock == null || berth == null) return;

        Transform gp = dock.Find("Gangplank");
        if (gp == null)
        {
            report.AppendLine($"⚠ Dock_{index + 1:00}: chưa có Gangplank (bước 3 chưa dựng?).");
            return;
        }

        Undo.RecordObject(gp, UndoLabel);

        // Tâm tấm gỗ = Berth + nửa chiều dài ⇒ ván bắt đầu ĐÚNG tại mạn tàu và kéo trọn
        // GangplankWorldLength unit vào bờ. Hằng lấy từ TouristVisitorSetupTool để hai tool
        // không bao giờ lệch nhau (bản trước ghi cứng 110 ở đây → ván ngắn, lệch mạn tàu).
        gp.position = berth.position + new Vector3(0f, TouristVisitorSetupTool.GangplankDistance, 0f);
        gp.rotation = Quaternion.Euler(0f, 0f, 90f); // ván nằm dọc theo hướng vào bờ (+Y)

        // Áp lại CỠ THẬT: object có sẵn từ bản tool cũ đang mang scale bé xíu.
        var sr = gp.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Undo.RecordObject(sr, UndoLabel);
            sr.sortingLayerName = TouristSortingLayers.Resolve(TouristSortingLayers.Gangplank);

            if (sr.sprite.border != Vector4.zero)
            {
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size     = new Vector2(TouristVisitorSetupTool.GangplankWorldLength,
                                          TouristVisitorSetupTool.GangplankWorldThickness);
                gp.localScale = Vector3.one;
            }
            else
            {
                sr.drawMode = SpriteDrawMode.Simple;
                Vector2 native = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
                if (native.x > 0.0001f && native.y > 0.0001f)
                    gp.localScale = new Vector3(
                        TouristVisitorSetupTool.GangplankWorldLength / native.x,
                        TouristVisitorSetupTool.GangplankWorldThickness / native.y, 1f);
            }
            EditorUtility.SetDirty(sr);
        }

        EditorUtility.SetDirty(gp);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BƯỚC 7 — TỰ KIỂM TRA
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Soi lại từng mảnh và in bảng "đủ / thiếu". Mỗi dòng thiếu đều kèm CÁCH KHẮC PHỤC
    /// để Sếp không phải đoán.
    /// </summary>
    private static void Buoc7_TuKiemTra(StringBuilder report, List<string> thieu)
    {
        report.AppendLine();
        report.AppendLine("── BƯỚC 7: TỰ KIỂM TRA ──");

        // 1 · Config
        var mgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        TouristBoatConfig cfg = mgr != null ? mgr.Config : null;
        if (cfg == null)
        {
            Thieu(report, thieu, "BoatDockManager chưa gắn TouristBoatConfig",
                  "chạy Tools/Farm Game/Tourist Boat/1. Setup All (Scene + Config)");
        }
        else if (Mathf.Abs(cfg.patienceMinutes - CfgPatience) > 0.01f ||
                 Mathf.Abs(cfg.maxDockMinutes - CfgMaxDock) > 0.01f)
        {
            Thieu(report, thieu, "Config chưa mang đủ số V2 (patience/maxDock lệch)",
                  "chạy lại menu ★ SETUP TẤT CẢ");
        }
        else
        {
            report.AppendLine("✔ Config: đã điền đủ 13 thông số V2.");

            // Cùng luật với cảnh báo trong menu Chẩn Đoán của Dev A: lưới an toàn phải
            // RỘNG HƠN thời gian kiên nhẫn, không thì tàu bị ép rời bến trong khi khách
            // vẫn còn quyền chờ ⇒ khách tức giận oan, người chơi mất lượt phục vụ.
            if (cfg.maxDockMinutes <= cfg.patienceMinutes)
            {
                Thieu(report, thieu,
                      $"maxDockMinutes ({cfg.maxDockMinutes:0.#}') <= patienceMinutes ({cfg.patienceMinutes:0.#}') " +
                      "— lưới an toàn sẽ ép tàu rời bến TRƯỚC khi khách hết kiên nhẫn",
                      $"đặt maxDockMinutes > patienceMinutes (khuyến nghị {CfgMaxDock:0}' cho patience {CfgPatience:0}')");
            }
            else
            {
                report.AppendLine($"✔ Lưới an toàn: maxDock {cfg.maxDockMinutes:0.#}' > kiên nhẫn {cfg.patienceMinutes:0.#}' " +
                                  $"(dư {cfg.maxDockMinutes - cfg.patienceMinutes:0.#}' cho khách đi bộ về tàu).");
            }
        }

        // 2 · 11 prefab khách
        string[] prefabGuids = AssetDatabase.IsValidFolder(PrefabTouristRoot)
            ? AssetDatabase.FindAssets("t:Prefab Tourist_NV", new[] { PrefabTouristRoot })
            : new string[0];
        if (prefabGuids.Length < 11)
            Thieu(report, thieu, $"Prefab khách mới có {prefabGuids.Length}/11",
                  "kiểm tra Assets/NV_NPC/NVGAME/Processed/NV01..NV11 đủ 132 file chưa, rồi chạy lại");
        else
        {
            report.AppendLine($"✔ Prefab khách: {prefabGuids.Length}/11.");

            // Kiểm layer THẬT trong prefab — lỗi Sếp gặp: 11 prefab đều nằm layer Default
            // (id 0) vì bản đầu ghi tên layer "CongTrinh" không tồn tại.
            string layerMongDoi = TouristSortingLayers.Resolve(TouristSortingLayers.Visitor);
            int saiLayer = 0;
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGuids[i]));
                var sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
                if (sr != null && sr.sortingLayerName != layerMongDoi) saiLayer++;
            }
            if (saiLayer > 0)
                Thieu(report, thieu,
                      $"{saiLayer}/{prefabGuids.Length} prefab khách còn sai sorting layer (cần \"{layerMongDoi}\") — sẽ bị decor che",
                      "chạy lại menu ★ SETUP TẤT CẢ (bước 2 ghi lại layer)");
            else
                report.AppendLine($"✔ Sorting layer prefab khách: \"{layerMongDoi}\" (nổi trên decor).");

            // AnimatorController: file tồn tại VẪN có thể hỏng (thiếu statemachine) —
            // đây chính là lỗi làm nhân vật đứng đơ và Console spam lúc Sếp Play test.
            int ctrlHong = 0, ctrlThieu = 0;
            var tenHong = new List<string>();
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGuids[i]));
                var anim = go != null ? go.GetComponent<Animator>() : null;
                if (anim == null || anim.runtimeAnimatorController == null) { ctrlThieu++; continue; }

                string ctrlPath = AssetDatabase.GetAssetPath(anim.runtimeAnimatorController);
                string loiCtrl;
                if (!NPCAnimationSetupTool.ControllerHopLe(ctrlPath, out loiCtrl))
                {
                    ctrlHong++;
                    tenHong.Add(go.name + " (" + loiCtrl + ")");
                }
            }

            if (ctrlThieu > 0)
                Thieu(report, thieu, $"{ctrlThieu}/{prefabGuids.Length} prefab khách CHƯA gán AnimatorController",
                      "chạy lại menu ★ SETUP TẤT CẢ");

            if (ctrlHong > 0)
            {
                Thieu(report, thieu,
                      $"{ctrlHong}/{prefabGuids.Length} AnimatorController HỎNG (thiếu statemachine) — nhân vật sẽ đứng đơ, Console spam \"Animator has not been initialized\"",
                      "chạy lại menu ★ SETUP TẤT CẢ (bước 2 tự xoá và tạo lại controller hỏng)");
                for (int i = 0; i < tenHong.Count; i++) report.AppendLine("      · " + tenHong[i]);
            }
            else if (ctrlThieu == 0 && prefabGuids.Length > 0)
            {
                report.AppendLine($"✔ AnimatorController: {prefabGuids.Length}/{prefabGuids.Length} hợp lệ " +
                                  $"({NPCAnimationSetupTool.ExpectedStateCount} state, có statemachine).");
            }
        }

        // 3 · TouristVisitorManager wire đủ chưa
        var vm = UnityEngine.Object.FindFirstObjectByType<TouristVisitorManager>(FindObjectsInactive.Include);
        if (vm == null)
        {
            Thieu(report, thieu, "Scene chưa có TouristVisitorManager",
                  "chạy lại menu ★ SETUP TẤT CẢ (bước 3 hỏng)");
        }
        else
        {
            var so = new SerializedObject(vm);
            KiemRef(so, "config",  "TouristVisitorManager.config",  report, thieu);
            KiemRef(so, "queue",   "TouristVisitorManager.queue",   report, thieu);
            KiemMang(so, "touristPrefabs", 11, "roster prefab khách", report, thieu);
            KiemMang(so, "dishDatabase",   38, "database món DishData", report, thieu);
            KiemMangDayDu(so, "dockPathRoots", 3, "3 đường đi bộ (TouristPath_Dock0X)", report, thieu);
            KiemMangDayDu(so, "gangplanks",    3, "3 tấm gỗ (Gangplank)", report, thieu);
        }

        // 4 · 2 popup của Dev C
        int popup = 0;
        if (CoComponentTrongScene("BoatAnnouncePopupUI"))  popup++;
        if (CoComponentTrongScene("DockPurchasePopupUI")) popup++;
        if (popup < 2)
            Thieu(report, thieu, $"Popup UI mới có {popup}/2",
                  "copy gói Dev C rồi chạy Tools/Farm Game/Tourist Boat/Setup Popups (UI)");
        else report.AppendLine("✔ Popup UI: 2/2 (báo tàu + mua bến).");

        // 5 · 3 dock đủ Berth + Path + Boat
        GameObject boatRoot = TimBoatSystem();
        if (boatRoot == null)
        {
            Thieu(report, thieu, "Scene không có BoatSystem",
                  "chạy Tools/Farm Game/Tourist Boat/1. Setup All (Scene + Config)");
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                Transform dock = boatRoot.transform.Find($"Dock_{i + 1:00}");
                if (dock == null) { Thieu(report, thieu, $"Thiếu Dock_{i + 1:00}", "chạy tool V1 Setup All"); continue; }

                var mat = new List<string>();
                if (dock.Find("Berth") == null) mat.Add("Berth");
                if (dock.Find("Path")  == null || dock.Find("Path").childCount == 0) mat.Add("Path/WP");
                if (dock.GetComponentInChildren<TouristBoatController>(true) == null) mat.Add("Boat");

                if (mat.Count > 0)
                    Thieu(report, thieu, $"Dock_{i + 1:00} thiếu: {string.Join(", ", mat)}", "chạy tool V1 Setup All");
                else
                    report.AppendLine($"✔ Dock_{i + 1:00}: đủ Berth + Path + Boat.");

                // Tấm gỗ: kiểm CỠ THẬT trên màn hình, không chỉ kiểm có object hay không —
                // lỗi Sếp gặp là ván tồn tại nhưng bé 5 unit nên coi như vô hình.
                Transform gp = dock.Find("Gangplank");
                if (gp == null)
                {
                    Thieu(report, thieu, $"Dock_{i + 1:00} thiếu Gangplank", "chạy lại menu ★ SETUP TẤT CẢ");
                }
                else
                {
                    var gsr = gp.GetComponent<SpriteRenderer>();
                    if (gsr == null || gsr.sprite == null)
                    {
                        Thieu(report, thieu, $"Dock_{i + 1:00}/Gangplank chưa có sprite",
                              "gắn sprite gỗ vào SpriteRenderer, hoặc chạy lại menu ★");
                    }
                    else
                    {
                        Vector3 cỡ = gsr.bounds.size;
                        float dai = Mathf.Max(cỡ.x, cỡ.y);
                        if (dai < TouristVisitorSetupTool.GangplankWorldLength * 0.5f)
                            Thieu(report, thieu,
                                  $"Dock_{i + 1:00}/Gangplank chỉ {dai:0} unit (cần ~{TouristVisitorSetupTool.GangplankWorldLength:0}) — quá bé, gần như vô hình",
                                  "chạy lại menu ★ SETUP TẤT CẢ (bước 6 áp lại cỡ)");
                        else
                            report.AppendLine($"✔ Dock_{i + 1:00}/Gangplank: {cỡ.x:0}x{cỡ.y:0} unit, layer \"{gsr.sortingLayerName}\".");
                    }
                }
            }
        }
    }

    private static void Thieu(StringBuilder report, List<string> ds, string mo, string khacPhuc)
    {
        string dong = mo + " → " + khacPhuc;
        report.AppendLine("✖ " + dong);
        ds.Add(dong);
        Debug.LogError("[TouristBoat] THIẾU: " + dong);
    }

    private static void KiemRef(SerializedObject so, string prop, string ten,
                                StringBuilder report, List<string> thieu)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null || p.objectReferenceValue == null)
            Thieu(report, thieu, ten + " đang TRỐNG", "chạy lại menu ★ SETUP TẤT CẢ");
        else
            report.AppendLine($"✔ {ten}: đã nối.");
    }

    /// <summary>Kiểm mảng có ĐỦ SỐ LƯỢNG mong đợi (roster / database) — thiếu thì cảnh báo kèm số thật.</summary>
    private static void KiemMang(SerializedObject so, string prop, int mongDoi, string ten,
                                 StringBuilder report, List<string> thieu)
    {
        SerializedProperty p = so.FindProperty(prop);
        int n = p != null && p.isArray ? p.arraySize : 0;
        if (n < mongDoi)
            Thieu(report, thieu, $"{ten} mới có {n}/{mongDoi}",
                  n == 0 ? "chạy lại menu ★ SETUP TẤT CẢ" : "kiểm tra asset trong Project rồi chạy lại");
        else
            report.AppendLine($"✔ {ten}: {n}.");
    }

    /// <summary>Kiểm mảng cố định 3 phần tử — không phần tử nào được null.</summary>
    private static void KiemMangDayDu(SerializedObject so, string prop, int soLuong, string ten,
                                      StringBuilder report, List<string> thieu)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null || !p.isArray || p.arraySize < soLuong)
        {
            Thieu(report, thieu, ten + " chưa đủ ô", "chạy lại menu ★ SETUP TẤT CẢ");
            return;
        }

        int trong = 0;
        for (int i = 0; i < soLuong; i++)
            if (p.GetArrayElementAtIndex(i).objectReferenceValue == null) trong++;

        if (trong > 0)
            Thieu(report, thieu, $"{ten} còn {trong} ô trống", "chạy lại menu ★ SETUP TẤT CẢ");
        else
            report.AppendLine($"✔ {ten}: đủ {soLuong}.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BƯỚC 8 — ĐÁNH DẤU SCENE (KHÔNG tự lưu)
    // ─────────────────────────────────────────────────────────────────────

    private static void Buoc8_MarkDirty(StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine("── BƯỚC 8: đánh dấu scene ──");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        report.AppendLine("✔ Scene đã đánh dấu có thay đổi. KHÔNG tự lưu — bạn tự bấm Ctrl+S " +
                          "(an toàn hơn: xem kết quả trước, không ưng thì Ctrl+Z).");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scene có component tên <paramref name="tenNgan"/> không (kể cả object đang tắt).
    /// So sánh theo TÊN TYPE thay vì tham chiếu thẳng, để tool này không phụ thuộc biên dịch
    /// vào gói Dev C — chưa copy gói đó thì chỉ báo "thiếu popup", không gãy compile.
    /// </summary>
    private static bool CoComponentTrongScene(string tenNgan)
    {
        foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb != null && mb.GetType().Name == tenNgan) return true;
        }
        return false;
    }

    /// <summary>Tìm type theo TÊN NGẮN trong mọi assembly đang nạp (dùng cho gói Dev A/Dev C).</summary>
    private static Type TimKieu(string tenNgan)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; } // assembly nạp lỗi — bỏ qua, không làm gãy tool
            for (int i = 0; i < types.Length; i++)
                if (types[i] != null && types[i].Name == tenNgan) return types[i];
        }
        return null;
    }

    private static GameObject TimBoatSystem()
    {
        var mgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        if (mgr != null) return mgr.gameObject;

        foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.parent == null && t.name == RootBoat) return t.gameObject;
        }
        return null;
    }
}
