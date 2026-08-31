using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 🔍 CHẨN ĐOÁN & XUẤT BÁO CÁO RA FILE — Tools/Farm Game/Tourist Boat/🔍 Chẩn đoán &amp; Xuất báo cáo
///
/// VÌ SAO CẦN TOOL NÀY: Lead/QA không mở được Unity của Sếp, không đọc được Console.
/// Tool ghi TOÀN BỘ trạng thái scene + asset ra một file text UTF-8 để gửi đi đọc:
/// <code>production/session-state/DIAG_REPORT.txt</code>
/// (đường dẫn tính từ THƯ MỤC GỐC PROJECT — cạnh Assets/, không nằm trong Assets/ để
/// Unity khỏi import nó thành asset.)
///
/// NỘI DUNG (8 mục):
///   1. Renderer có bounds world &gt; 3000 unit — truy "object quá to che kín map"
///   2. Mọi Canvas (renderMode · sortingOrder · active · số con active)
///   3. Mọi UI.Image ĐANG ACTIVE mà sprite == null — thủ phạm điển hình của "khối trắng đặc"
///   4. Toàn hệ tourist: TouristSystem · 3 path + toạ độ từng WP · QueueAnchor ·
///      3 Gangplank (vị trí + CỠ THẬT tính từ bounds) · 3 Dock/Berth/Boat · wiring manager
///   5. TouristBoatConfig: 13 field + cảnh báo maxDockMinutes &lt;= patienceMinutes
///   6. 11 prefab khách: sorting layer/order · scale · chiều cao world tính từ sprite
///   7. Danh sách sorting layer THẬT của project
///   8. (chỉ Play Mode) state từng bến · số khách đang sống · pha từng khách
///
/// Chạy được cả Edit Mode lẫn Play Mode. Không sửa gì trong scene — CHỈ ĐỌC.
/// </summary>
public static class TouristBoatDiagnosticExport
{
    private const string MenuPath = "Tools/Farm Game/Tourist Boat/🔍 Chẩn đoán & Xuất báo cáo";

    /// <summary>Đường dẫn TƯƠNG ĐỐI so với thư mục gốc project (cạnh Assets/).</summary>
    private const string OutRelative = "production/session-state/DIAG_REPORT.txt";

    /// <summary>Ngưỡng coi renderer là "quá to, nghi phủ kín map" (unit world).</summary>
    private const float NguongBoundsLon = 3000f;

    private const string PrefabRoot = "Assets/_Game/Farm/Prefabs/Tourists";

    [MenuItem(MenuPath, false, 5)]
    public static void XuatBaoCao()
    {
        var sb = new StringBuilder(64 * 1024);

        try
        {
            EditorUtility.DisplayProgressBar("Chẩn đoán Tourist Boat", "Đang quét scene…", 0.1f);

            GhiTieuDe(sb);
            Muc1_RendererQuaTo(sb);
            Muc2_Canvas(sb);
            Muc3_ImageThieuSprite(sb);

            EditorUtility.DisplayProgressBar("Chẩn đoán Tourist Boat", "Đang soi hệ tourist…", 0.5f);
            Muc4_HeTourist(sb);
            Muc5_Config(sb);

            EditorUtility.DisplayProgressBar("Chẩn đoán Tourist Boat", "Đang soi prefab khách…", 0.75f);
            Muc6_PrefabKhach(sb);
            Muc7_SortingLayers(sb);
            Muc8_PlayMode(sb);

            GhiKetThuc(sb);
        }
        catch (Exception e)
        {
            sb.AppendLine();
            sb.AppendLine("!!! TOOL CHẨN ĐOÁN GẶP LỖI GIỮA CHỪNG: " + e);
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string duongDan = GhiFile(sb.ToString());
        if (string.IsNullOrEmpty(duongDan))
        {
            EditorUtility.DisplayDialog("Chẩn đoán Tourist Boat",
                "KHÔNG ghi được file báo cáo — xem Console để biết lý do.", "OK");
            return;
        }

        Debug.Log("[TouristBoat] 🔍 Đã ghi báo cáo chẩn đoán ra file:\n" + duongDan +
                  "\n(Gửi nguyên file này cho Lead/QA.)");
        EditorUtility.DisplayDialog("Chẩn đoán Tourist Boat — Đã xuất file",
            "Đã ghi báo cáo ra:\n\n" + duongDan +
            "\n\nGửi nguyên file này cho Lead/QA.\nĐường dẫn cũng đã in ở Console.", "OK");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TIÊU ĐỀ
    // ═════════════════════════════════════════════════════════════════════

    private static void GhiTieuDe(StringBuilder sb)
    {
        sb.AppendLine("================================================================");
        sb.AppendLine(" BAO CAO CHAN DOAN — TOURIST BOAT SYSTEM V2 (BOAT-002)");
        sb.AppendLine("================================================================");
        sb.AppendLine("Sinh boi : Tools/Farm Game/Tourist Boat/Chan doan & Xuat bao cao (Dev B)");
        sb.AppendLine("Thoi diem: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) +
                      " (local) · " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC");
        sb.AppendLine("Unity    : " + Application.unityVersion);
        sb.AppendLine("Che do   : " + (Application.isPlaying ? "PLAY MODE" : "EDIT MODE"));
        sb.AppendLine("Scene    : " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name +
                      "  (" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + ")");
        sb.AppendLine();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MỤC 1 — RENDERER QUÁ TO
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Liệt kê mọi Renderer có AABB world lớn hơn <see cref="NguongBoundsLon"/> —
    /// đây là cách truy nhanh "object nào đang phủ kín map" mà Sếp báo.
    /// Sắp giảm dần theo cạnh lớn nhất để thủ phạm nằm ngay đầu danh sách.
    /// </summary>
    private static void Muc1_RendererQuaTo(StringBuilder sb)
    {
        TieuDeMuc(sb, "1. RENDERER CO BOUNDS WORLD > " + NguongBoundsLon.ToString("0") + " UNIT (nghi phu kin map)");

        var ds = new List<Renderer>();
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r == null) continue;
            Vector3 co = r.bounds.size;
            if (Mathf.Max(co.x, co.y) > NguongBoundsLon) ds.Add(r);
        }

        if (ds.Count == 0)
        {
            sb.AppendLine("  (khong co renderer nao vuot nguong — khong phai nguyen nhan che map)");
            sb.AppendLine();
            return;
        }

        ds.Sort((a, b) =>
        {
            float ka = Mathf.Max(a.bounds.size.x, a.bounds.size.y);
            float kb = Mathf.Max(b.bounds.size.x, b.bounds.size.y);
            return kb.CompareTo(ka);
        });

        sb.AppendLine("  Tim thay " + ds.Count + " renderer (sap giam dan theo canh lon nhat):");
        sb.AppendLine();
        for (int i = 0; i < ds.Count; i++)
        {
            Renderer r = ds[i];
            Vector3 co = r.bounds.size;
            var sr = r as SpriteRenderer;

            sb.AppendLine("  [" + (i + 1) + "] " + r.gameObject.name);
            sb.AppendLine("      duong dan : " + DuongDan(r.transform));
            sb.AppendLine("      bounds    : " + F2(co.x) + " x " + F2(co.y) + " unit world");
            sb.AppendLine("      world pos : " + V(r.transform.position));
            sb.AppendLine("      world scl : " + V(r.transform.lossyScale));
            sb.AppendLine("      sprite    : " + (sr != null && sr.sprite != null ? sr.sprite.name : "(khong phai SpriteRenderer / khong co sprite)"));
            sb.AppendLine("      sorting   : layer \"" + r.sortingLayerName + "\" · order " + r.sortingOrder);
            sb.AppendLine("      active    : " + (r.gameObject.activeInHierarchy ? "CO" : "KHONG") +
                          " · renderer enabled: " + (r.enabled ? "CO" : "KHONG"));
            sb.AppendLine();
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MỤC 2 — CANVAS
    // ═════════════════════════════════════════════════════════════════════

    private static void Muc2_Canvas(StringBuilder sb)
    {
        TieuDeMuc(sb, "2. CANVAS TRONG SCENE");

        Canvas[] ds = UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (ds == null || ds.Length == 0)
        {
            sb.AppendLine("  (scene khong co Canvas nao)");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("  Tong: " + ds.Length + " canvas");
        sb.AppendLine();
        for (int i = 0; i < ds.Length; i++)
        {
            Canvas c = ds[i];
            if (c == null) continue;

            int conActive = 0;
            for (int k = 0; k < c.transform.childCount; k++)
                if (c.transform.GetChild(k) != null && c.transform.GetChild(k).gameObject.activeSelf) conActive++;

            sb.AppendLine("  - " + c.gameObject.name);
            sb.AppendLine("      duong dan   : " + DuongDan(c.transform));
            sb.AppendLine("      renderMode  : " + c.renderMode);
            sb.AppendLine("      sortingOrder: " + c.sortingOrder + " · sortingLayer \"" + c.sortingLayerName + "\"");
            sb.AppendLine("      isRootCanvas: " + (c.isRootCanvas ? "CO" : "KHONG"));
            sb.AppendLine("      active      : " + (c.gameObject.activeInHierarchy ? "CO" : "KHONG"));
            sb.AppendLine("      con active  : " + conActive + " / " + c.transform.childCount);
            sb.AppendLine();
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MỤC 3 — IMAGE THIẾU SPRITE (khối trắng đặc)
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// UI.Image không có sprite thì Unity vẽ HÌNH CHỮ NHẬT ĐẶC theo màu của nó —
    /// mặc định là TRẮNG. Đây là nghi phạm số 1 của "các khối trắng che màn hình".
    /// Chỉ liệt kê Image ĐANG ACTIVE (activeInHierarchy) vì chỉ chúng mới vẽ ra.
    /// </summary>
    private static void Muc3_ImageThieuSprite(StringBuilder sb)
    {
        TieuDeMuc(sb, "3. UI.Image DANG ACTIVE MA KHONG CO SPRITE (ve khoi dac theo mau)");

        var ds = new List<Image>();
        foreach (Image img in UnityEngine.Object.FindObjectsByType<Image>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img == null) continue;
            if (!img.gameObject.activeInHierarchy) continue;
            if (!img.enabled) continue;
            if (img.sprite != null) continue;
            ds.Add(img);
        }

        if (ds.Count == 0)
        {
            sb.AppendLine("  (khong co Image active nao thieu sprite — khong phai nguyen nhan khoi trang)");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("  Tim thay " + ds.Count + " Image (moi cai deu dang ve mot khoi dac):");
        sb.AppendLine();
        for (int i = 0; i < ds.Count; i++)
        {
            Image img = ds[i];
            var rt = img.rectTransform;
            Canvas cha = img.GetComponentInParent<Canvas>();

            sb.AppendLine("  [" + (i + 1) + "] " + img.gameObject.name);
            sb.AppendLine("      duong dan : " + DuongDan(img.transform));
            sb.AppendLine("      kich thuoc: " + (rt != null ? F2(rt.rect.width) + " x " + F2(rt.rect.height) : "?") +
                          " (sizeDelta " + (rt != null ? V2(rt.sizeDelta) : "?") + ")");
            sb.AppendLine("      mau       : " + Mau(img.color) +
                          (img.color.a > 0.95f && img.color.r > 0.95f && img.color.g > 0.95f && img.color.b > 0.95f
                              ? "   <-- TRANG DAC, KHA NGHI"
                              : ""));
            sb.AppendLine("      raycast   : " + (img.raycastTarget ? "CO (nuot click)" : "khong"));
            sb.AppendLine("      canvas cha: " + (cha != null ? cha.gameObject.name + " (order " + cha.sortingOrder + ")" : "(khong co)"));
            sb.AppendLine();
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MỤC 4 — HỆ TOURIST
    // ═════════════════════════════════════════════════════════════════════

    private static void Muc4_HeTourist(StringBuilder sb)
    {
        TieuDeMuc(sb, "4. HE KHACH DU LICH (TouristSystem + BoatSystem)");

        // ── TouristSystem ──
        GameObject ts = GameObject.Find("TouristSystem");
        if (ts == null)
        {
            sb.AppendLine("  [X] KHONG co 'TouristSystem' trong scene.");
            sb.AppendLine("      Khac phuc: chay Tools/Farm Game/Tourist Boat/SETUP TAT CA (1 nut).");
        }
        else
        {
            sb.AppendLine("  [OK] TouristSystem tai " + V(ts.transform.position) +
                          " · active " + (ts.activeInHierarchy ? "CO" : "KHONG"));
        }

        // ── QueueAnchor ──
        var queue = UnityEngine.Object.FindFirstObjectByType<TouristQueue>(FindObjectsInactive.Include);
        if (queue == null)
        {
            sb.AppendLine("  [X] KHONG co TouristQueue (QueueAnchor) — khach se dung chong nhau.");
        }
        else
        {
            sb.AppendLine("  [OK] QueueAnchor : " + V(queue.transform.position) +
                          " · duong dan " + DuongDan(queue.transform));
            sb.AppendLine("       slot 0..3   : " + V(queue.GetSlotPosition(0)) + " " + V(queue.GetSlotPosition(1)) +
                          " " + V(queue.GetSlotPosition(2)) + " " + V(queue.GetSlotPosition(3)));
        }
        sb.AppendLine();

        // ── 3 path + toạ độ WP ──
        sb.AppendLine("  -- Duong di bo (TouristPath_Dock0X) --");
        for (int i = 0; i < 3; i++)
        {
            string ten = "TouristPath_Dock" + (i + 1).ToString("00");
            GameObject path = GameObject.Find(ten);
            if (path == null)
            {
                sb.AppendLine("  [X] " + ten + ": KHONG CO");
                continue;
            }

            sb.AppendLine("  [OK] " + ten + " · " + path.transform.childCount + " waypoint:");
            for (int k = 0; k < path.transform.childCount; k++)
            {
                Transform wp = path.transform.GetChild(k);
                if (wp == null) continue;
                sb.AppendLine("        " + wp.name.PadRight(8) + V(wp.position));
            }
        }
        sb.AppendLine();

        // ── BoatSystem: Dock / Berth / Boat / Gangplank ──
        sb.AppendLine("  -- Ben tau (BoatSystem/Dock_0X) --");
        var mgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        GameObject boatRoot = mgr != null ? mgr.gameObject : GameObject.Find("BoatSystem");

        if (boatRoot == null)
        {
            sb.AppendLine("  [X] KHONG co BoatSystem trong scene.");
        }
        else
        {
            sb.AppendLine("  [OK] BoatSystem tai " + V(boatRoot.transform.position));
            Transform blind = boatRoot.transform.Find("BlindPoint");
            sb.AppendLine("       BlindPoint : " + (blind != null ? V(blind.position) : "(THIEU)"));

            for (int i = 0; i < 3; i++)
            {
                Transform dock = boatRoot.transform.Find("Dock_" + (i + 1).ToString("00"));
                if (dock == null) { sb.AppendLine("  [X] Dock_" + (i + 1).ToString("00") + ": KHONG CO"); continue; }

                Transform berth = dock.Find("Berth");
                Transform boat  = dock.Find("Boat");
                Transform gp    = dock.Find("Gangplank");
                Transform bpath = dock.Find("Path");

                sb.AppendLine("  Dock_" + (i + 1).ToString("00") + " tai " + V(dock.position));
                sb.AppendLine("       Berth     : " + (berth != null ? V(berth.position) : "(THIEU)"));
                sb.AppendLine("       Boat      : " + (boat != null
                    ? V(boat.position) + (boat.GetComponentInChildren<TouristBoatController>(true) != null
                        ? " · co TouristBoatController" : " · [X] THIEU TouristBoatController")
                    : "(THIEU)"));
                sb.AppendLine("       Path(tau) : " + (bpath != null ? bpath.childCount + " WP" : "(THIEU)"));
                GhiGangplank(sb, gp, berth);
            }
        }
        sb.AppendLine();

        // ── Wiring TouristVisitorManager ──
        sb.AppendLine("  -- Wiring TouristVisitorManager --");
        var vm = UnityEngine.Object.FindFirstObjectByType<TouristVisitorManager>(FindObjectsInactive.Include);
        if (vm == null)
        {
            sb.AppendLine("  [X] Scene KHONG co TouristVisitorManager.");
        }
        else
        {
            var so = new SerializedObject(vm);
            GhiRef(sb, so, "config",       "config (TouristBoatConfig)");
            GhiRef(sb, so, "queue",        "queue (TouristQueue)");
            GhiRef(sb, so, "visitorsRoot", "visitorsRoot");
            GhiMang(sb, so, "touristPrefabs", "touristPrefabs (roster khach)", 11);
            GhiMang(sb, so, "dishDatabase",   "dishDatabase (DishData)",       38);
            GhiMang(sb, so, "dockPathRoots",  "dockPathRoots",                 3);
            GhiMang(sb, so, "gangplanks",     "gangplanks",                    3);
        }
        sb.AppendLine();
    }

    /// <summary>Ghi thông tin tấm gỗ kèm CỠ THẬT tính từ bounds — lỗi "ván bé xíu" nhìn ở đây là ra.</summary>
    private static void GhiGangplank(StringBuilder sb, Transform gp, Transform berth)
    {
        if (gp == null)
        {
            sb.AppendLine("       Gangplank : (THIEU)");
            return;
        }

        var sr = gp.GetComponent<SpriteRenderer>();
        string co = "?";
        if (sr != null && sr.sprite != null)
        {
            Vector3 b = sr.bounds.size;
            co = F2(b.x) + " x " + F2(b.y) + " unit world";
        }

        sb.AppendLine("       Gangplank : " + V(gp.position) + " · co THAT " + co);
        sb.AppendLine("                   scale " + V(gp.lossyScale) +
                      " · sprite " + (sr != null && sr.sprite != null ? sr.sprite.name : "(KHONG CO)"));
        sb.AppendLine("                   sorting layer \"" + (sr != null ? sr.sortingLayerName : "?") +
                      "\" order " + (sr != null ? sr.sortingOrder.ToString() : "?") +
                      " · enabled " + (sr != null && sr.enabled ? "CO" : "KHONG"));
        if (berth != null)
        {
            Vector3 d = gp.position - berth.position;
            sb.AppendLine("                   lech so voi Berth: " + V2(new Vector2(d.x, d.y)) +
                          " (mong doi: 0, +" + F2(TouristVisitorSetupTool.GangplankDistance) + ")");
        }
        if (sr != null && sr.sprite != null && Mathf.Max(sr.bounds.size.x, sr.bounds.size.y)
            < TouristVisitorSetupTool.GangplankWorldLength * 0.5f)
        {
            sb.AppendLine("                   [X] QUA BE — can ~" +
                          F2(TouristVisitorSetupTool.GangplankWorldLength) +
                          " unit. Chay lai menu SETUP TAT CA.");
        }
    }

    private static void GhiRef(StringBuilder sb, SerializedObject so, string prop, string ten)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) { sb.AppendLine("       [?] " + ten + ": khong tim thay field"); return; }
        UnityEngine.Object v = p.objectReferenceValue;
        sb.AppendLine("       " + (v != null ? "[OK] " : "[X]  ") + ten + ": " + (v != null ? v.name : "TRONG"));
    }

    private static void GhiMang(StringBuilder sb, SerializedObject so, string prop, string ten, int mongDoi)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null || !p.isArray) { sb.AppendLine("       [?] " + ten + ": khong tim thay field"); return; }

        int trong = 0;
        for (int i = 0; i < p.arraySize; i++)
            if (p.GetArrayElementAtIndex(i).objectReferenceValue == null) trong++;

        bool du = p.arraySize >= mongDoi && trong == 0;
        sb.AppendLine("       " + (du ? "[OK] " : "[X]  ") + ten + ": " + p.arraySize + "/" + mongDoi +
                      (trong > 0 ? " (" + trong + " o TRONG)" : ""));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MỤC 5 — CONFIG
    // ═════════════════════════════════════════════════════════════════════

    private static void Muc5_Config(StringBuilder sb)
    {
        TieuDeMuc(sb, "5. TouristBoatConfig (13 field V2)");

        var mgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        TouristBoatConfig cfg = mgr != null ? mgr.Config : null;

        string[] guids = AssetDatabase.FindAssets("t:TouristBoatConfig");
        sb.AppendLine("  So asset TouristBoatConfig trong project: " + (guids != null ? guids.Length : 0));
        if (guids != null)
            for (int i = 0; i < guids.Length; i++)
                sb.AppendLine("    - " + AssetDatabase.GUIDToAssetPath(guids[i]));

        if (cfg == null)
        {
            sb.AppendLine("  [X] BoatDockManager KHONG gan config (hoac khong co BoatDockManager trong scene).");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("  Asset dang dung: " + AssetDatabase.GetAssetPath(cfg));
        sb.AppendLine();
        sb.AppendLine("    gapOneDockMinutes         = " + F2(cfg.gapOneDockMinutes));
        sb.AppendLine("    gapMultiDockMinutes       = " + F2(cfg.gapMultiDockMinutes));
        sb.AppendLine("    minStaggerMinutes         = " + F2(cfg.minStaggerMinutes));
        sb.AppendLine("    maxDockMinutes            = " + F2(cfg.maxDockMinutes));
        sb.AppendLine("    visitorsMin / visitorsMax = " + cfg.visitorsMin + " / " + cfg.visitorsMax);
        sb.AppendLine("    patienceMinutes           = " + F2(cfg.patienceMinutes));
        sb.AppendLine("    rewardIngredientMultiplier= " + cfg.rewardIngredientMultiplier);
        sb.AppendLine("    disembarkInterval         = " + F2(cfg.disembarkInterval));
        sb.AppendLine("    visitorWalkSpeed          = " + F2(cfg.visitorWalkSpeed));
        sb.AppendLine("    queueSpacing              = " + F2(cfg.queueSpacing));
        sb.AppendLine("    bubbleScaleInTime         = " + F2(cfg.bubbleScaleInTime));
        sb.AppendLine("    smileyFlyTime             = " + F2(cfg.smileyFlyTime));
        sb.AppendLine("    debugTimeScale            = " + F2(cfg.debugTimeScale) +
                      (cfg.debugTimeScale > 1.01f ? "   <-- DANG TUA NHANH THOI GIAN" : ""));
        sb.AppendLine();

        if (cfg.maxDockMinutes <= cfg.patienceMinutes)
        {
            sb.AppendLine("  [X] CANH BAO: maxDockMinutes (" + F2(cfg.maxDockMinutes) +
                          ") <= patienceMinutes (" + F2(cfg.patienceMinutes) + ")");
            sb.AppendLine("      => luoi an toan se ep tau roi ben TRUOC khi khach het kien nhan,");
            sb.AppendLine("         khach bi tuc gian oan. Dat maxDockMinutes > patienceMinutes (khuyen nghi 35 / 30).");
        }
        else
        {
            sb.AppendLine("  [OK] maxDock > patience (du " + F2(cfg.maxDockMinutes - cfg.patienceMinutes) +
                          " phut cho khach di bo ve tau).");
        }
        sb.AppendLine();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MỤC 6 — PREFAB KHÁCH
    // ═════════════════════════════════════════════════════════════════════

    private static void Muc6_PrefabKhach(StringBuilder sb)
    {
        TieuDeMuc(sb, "6. PREFAB KHACH DU LICH (" + PrefabRoot + ")");

        if (!AssetDatabase.IsValidFolder(PrefabRoot))
        {
            sb.AppendLine("  [X] Chua co thu muc " + PrefabRoot + " — chua chay Setup NPC Animations.");
            sb.AppendLine();
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab Tourist_NV", new[] { PrefabRoot });
        var paths = new List<string>(guids.Length);
        for (int i = 0; i < guids.Length; i++) paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
        paths.Sort(StringComparer.Ordinal);

        string layerMongDoi = TouristSortingLayers.Resolve(TouristSortingLayers.Visitor);
        sb.AppendLine("  Tong: " + paths.Count + "/11 prefab · sorting layer mong doi: \"" + layerMongDoi + "\"");
        sb.AppendLine();

        int saiLayer = 0;
        for (int i = 0; i < paths.Count; i++)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (go == null) { sb.AppendLine("  [X] " + paths[i] + ": khong load duoc"); continue; }

            var sr   = go.GetComponent<SpriteRenderer>();
            var anim = go.GetComponent<Animator>();
            var col  = go.GetComponent<Collider2D>();
            var ag   = go.GetComponent<TouristAgent>();
            var bub  = go.GetComponent<TouristRequestBubble>();

            // Prefab asset khong co bounds world → tinh tu sprite × localScale.
            float caoWorld = 0f;
            if (sr != null && sr.sprite != null)
            {
                float caoSprite = sr.sprite.rect.height / Mathf.Max(1f, sr.sprite.pixelsPerUnit);
                caoWorld = caoSprite * Mathf.Abs(go.transform.localScale.y);
            }

            bool layerOk = sr != null && sr.sortingLayerName == layerMongDoi;
            if (!layerOk) saiLayer++;

            sb.AppendLine("  " + (layerOk ? "[OK] " : "[X]  ") + go.name);
            sb.AppendLine("       sorting   : layer \"" + (sr != null ? sr.sortingLayerName : "(khong co SpriteRenderer)") +
                          "\" order " + (sr != null ? sr.sortingOrder.ToString() : "?") +
                          (layerOk ? "" : "   <-- SAI, se bi decor che"));
            sb.AppendLine("       scale     : " + V(go.transform.localScale) +
                          " · cao world ~" + F2(caoWorld) + " unit");
            sb.AppendLine("       sprite    : " + (sr != null && sr.sprite != null ? sr.sprite.name : "(TRONG)"));
            sb.AppendLine("       component : Animator " + (anim != null ? "CO" : "[X] THIEU") +
                          " · Collider2D " + (col != null ? "CO" : "[X] THIEU (khong tap duoc)") +
                          " · TouristAgent " + (ag != null ? "CO" : "[X] THIEU") +
                          " · Bubble " + (bub != null ? "CO" : "[X] THIEU"));
            // Controller ton tai VAN co the hong (thieu statemachine) — kiem that su.
            string ctrlMoTa;
            if (anim == null || anim.runtimeAnimatorController == null)
            {
                ctrlMoTa = "[X] TRONG";
            }
            else
            {
                string ctrlPath = AssetDatabase.GetAssetPath(anim.runtimeAnimatorController);
                string loiCtrl;
                bool ok = NPCAnimationSetupTool.ControllerHopLe(ctrlPath, out loiCtrl);
                ctrlMoTa = anim.runtimeAnimatorController.name +
                           (ok ? "  (OK, " + NPCAnimationSetupTool.ExpectedStateCount + " state)"
                               : "   <-- [X] HONG: " + loiCtrl + "  => nhan vat dung do, Console spam");
            }
            sb.AppendLine("       controller: " + ctrlMoTa);
            sb.AppendLine();
        }

        if (saiLayer > 0)
        {
            sb.AppendLine("  [X] " + saiLayer + "/" + paths.Count +
                          " prefab SAI sorting layer — chay lai menu SETUP TAT CA (buoc 2 ghi lai layer).");
            sb.AppendLine();
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MỤC 7 — SORTING LAYER CỦA PROJECT
    // ═════════════════════════════════════════════════════════════════════

    private static void Muc7_SortingLayers(StringBuilder sb)
    {
        TieuDeMuc(sb, "7. SORTING LAYER THAT CUA PROJECT (tu duoi len tren)");

        SortingLayer[] ds = SortingLayer.layers;
        if (ds == null || ds.Length == 0)
        {
            sb.AppendLine("  (khong doc duoc danh sach sorting layer)");
            sb.AppendLine();
            return;
        }

        for (int i = 0; i < ds.Length; i++)
            sb.AppendLine("  " + i + ". \"" + ds[i].name + "\"  (id " + ds[i].id + ", value " + ds[i].value + ")");

        sb.AppendLine();
        sb.AppendLine("  He tourist dung:");
        sb.AppendLine("    gangplank = \"" + TouristSortingLayers.Resolve(TouristSortingLayers.Gangplank) + "\"  (duoi chan khach)");
        sb.AppendLine("    khach     = \"" + TouristSortingLayers.Resolve(TouristSortingLayers.Visitor)   + "\"  (tren decor)");
        sb.AppendLine("    bubble/FX = \"" + TouristSortingLayers.Resolve(TouristSortingLayers.Overlay)   + "\"  (tren dau khach)");
        sb.AppendLine();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MỤC 8 — PLAY MODE
    // ═════════════════════════════════════════════════════════════════════

    private static void Muc8_PlayMode(StringBuilder sb)
    {
        TieuDeMuc(sb, "8. TRANG THAI RUNTIME (chi co trong PLAY MODE)");

        if (!Application.isPlaying)
        {
            sb.AppendLine("  (dang o EDIT MODE — bam Play roi chay lai menu nay de co muc nay)");
            sb.AppendLine();
            return;
        }

        var mgr = BoatDockManager.Instance;
        if (mgr == null)
        {
            sb.AppendLine("  [X] BoatDockManager.Instance = null luc Play.");
        }
        else
        {
            sb.AppendLine("  BoatDockManager: IsReady " + (mgr.IsReady ? "CO" : "KHONG") +
                          " · ben da mo " + mgr.UnlockedDockCount + "/3");
            for (int i = 0; i < BoatDockManager.DockCount; i++)
            {
                sb.AppendLine("    Ben " + (i + 1) + " (tau so " + mgr.BoatNumber(i).ToString("00") + "): " +
                              "unlocked " + (mgr.IsDockUnlocked(i) ? "CO " : "KHONG") +
                              " · state " + mgr.GetBoatState(i) +
                              " · IsDocked " + (mgr.IsDocked(i) ? "CO" : "KHONG"));
            }
        }
        sb.AppendLine();

        var vm = TouristVisitorManager.Instance;
        sb.AppendLine("  TouristVisitorManager.Instance: " + (vm != null ? "CO" : "[X] null") +
                      (vm != null ? " · timeScale hieu luc " + F2(vm.EffectiveTimeScale) : ""));

        TouristAgent[] khach = UnityEngine.Object.FindObjectsByType<TouristAgent>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        sb.AppendLine("  So khach dang song trong scene: " + (khach != null ? khach.Length : 0));
        sb.AppendLine();

        if (khach == null || khach.Length == 0) { sb.AppendLine(); return; }

        long now = DateTime.UtcNow.Ticks;
        for (int i = 0; i < khach.Length; i++)
        {
            TouristAgent a = khach[i];
            if (a == null) continue;

            string conLai = "(chua mo bubble)";
            if (a.PatienceEndUtcTicks > 0)
            {
                double giay = (a.PatienceEndUtcTicks - now) / (double)TimeSpan.TicksPerSecond;
                conLai = giay > 0 ? F2((float)giay) + "s nua het kien nhan" : "DA HET kien nhan " + F2((float)-giay) + "s";
            }

            sb.AppendLine("  - " + a.gameObject.name);
            sb.AppendLine("      ben " + (a.DockIndex + 1) + " · khach #" + a.VisitorIndex +
                          " · slot " + a.QueueSlot + (a.IsFrontOfQueue ? " (dau hang)" : ""));
            sb.AppendLine("      pha       : " + a.State +
                          " · served " + (a.WasServed ? "CO" : "khong") +
                          " · timedOut " + (a.WasTimedOut ? "CO" : "khong"));
            sb.AppendLine("      mon       : " + (string.IsNullOrEmpty(a.DishId) ? "(khong co)" : a.DishId));
            sb.AppendLine("      kien nhan : " + conLai);
            sb.AppendLine("      vi tri    : " + V(a.transform.position) +
                          " · sorting layer \"" + a.SortingLayerResolved + "\"");
            sb.AppendLine();
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GHI FILE + HELPERS
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Ghi ra production/session-state/DIAG_REPORT.txt (UTF-8). Trả đường dẫn tuyệt đối, rỗng nếu lỗi.</summary>
    private static string GhiFile(string noiDung)
    {
        try
        {
            // Application.dataPath = <project>/Assets → lùi 1 cấp ra gốc project.
            string goc = Directory.GetParent(Application.dataPath).FullName;
            string dich = Path.Combine(goc, OutRelative.Replace('/', Path.DirectorySeparatorChar));

            string thuMuc = Path.GetDirectoryName(dich);
            if (!string.IsNullOrEmpty(thuMuc) && !Directory.Exists(thuMuc))
                Directory.CreateDirectory(thuMuc);

            File.WriteAllText(dich, noiDung, new UTF8Encoding(false));
            return dich;
        }
        catch (Exception e)
        {
            Debug.LogError("[TouristBoat] Khong ghi duoc file chan doan: " + e.Message);
            return string.Empty;
        }
    }

    private static void GhiKetThuc(StringBuilder sb)
    {
        sb.AppendLine("================================================================");
        sb.AppendLine(" HET BAO CAO — " +
                      DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " (local)");
        sb.AppendLine("================================================================");
    }

    private static void TieuDeMuc(StringBuilder sb, string ten)
    {
        sb.AppendLine("----------------------------------------------------------------");
        sb.AppendLine(" " + ten);
        sb.AppendLine("----------------------------------------------------------------");
    }

    /// <summary>Đường dẫn hierarchy đầy đủ (Root/Con/Chau) — để Lead định vị object chính xác.</summary>
    private static string DuongDan(Transform t)
    {
        if (t == null) return "(null)";
        string s = t.name;
        Transform p = t.parent;
        while (p != null) { s = p.name + "/" + s; p = p.parent; }
        return s;
    }

    private static string V(Vector3 v)
        => "(" + F2(v.x) + ", " + F2(v.y) + ", " + F2(v.z) + ")";

    private static string V2(Vector2 v)
        => "(" + F2(v.x) + ", " + F2(v.y) + ")";

    private static string Mau(Color c)
        => "RGBA(" + F2(c.r) + ", " + F2(c.g) + ", " + F2(c.b) + ", " + F2(c.a) + ")";

    /// <summary>Format số bất biến văn hoá — file gửi đi đọc ở máy khác không bị dấu phẩy thập phân.</summary>
    private static string F2(float f)
        => f.ToString("0.##", CultureInfo.InvariantCulture);
}
