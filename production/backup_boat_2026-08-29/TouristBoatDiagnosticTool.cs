#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool CHẨN ĐOÁN hệ Tourist Boat — trả lời đúng một câu hỏi:
/// "vì sao vào game không thấy tàu?".
///
/// Lý do tool này tồn tại: khi bến chưa mở khóa, tàu bị
/// <c>SetVisualShown(false)</c> nên biến mất HOÀN TOÀN và trước đây không có
/// một dòng log nào giải thích. Người chơi/dev nhìn vào chỉ thấy biển trống và
/// tưởng code hỏng, trong khi phần lớn trường hợp là gate cấp độ (mở ở cấp 10)
/// hoặc tàu đang trong pha Hidden (núp ở điểm mù 15 phút).
///
/// Menu:
///   6. Chẩn Đoán            — in mọi nguyên nhân khả dĩ + kết luận, chạy được cả Edit/Play Mode
///   7. Test Ngay            — ép tàu cập bến để soi art/vị trí/sorting (Play Mode)
///   8. Xóa Save Tàu         — xóa PlayerPrefs TouristBoat_* để diễn lại intro
/// </summary>
public static class TouristBoatDiagnosticTool
{
    private const string Menu6 = "Tools/Farm Game/Tourist Boat/6. Chẩn Đoán — Vì Sao Không Thấy Tàu";
    private const string Menu7 = "Tools/Farm Game/Tourist Boat/7. Test Ngay — Cho Tàu Cập Bến";
    private const string Menu8 = "Tools/Farm Game/Tourist Boat/8. Xóa Save Tàu (chơi lại intro)";

    private const string KeyUnlockedFormat = "TouristBoat_Unlocked_{0}";
    private const string KeyAnchorFormat   = "TouristBoat_AnchorUtc_{0}";
    private const string KeyIntroDone      = "TouristBoat_IntroDone";

    // Hai dock cách nhau dưới ngưỡng này coi như bị đặt chồng ("1 cục").
    private const float NguongTrungViTri = 50f;

    // ───────────────────────── 6. CHẨN ĐOÁN ─────────────────────────

    [MenuItem(Menu6, false, 60)]
    public static void ChanDoan()
    {
        var log = new StringBuilder();
        // nguyenNhan = danh sách kết luận theo thứ tự ưu tiên, hiện trên dialog.
        var nguyenNhan = new List<string>();

        log.AppendLine("===== CHẨN ĐOÁN TOURIST BOAT =====");
        log.AppendLine(Application.isPlaying
            ? "Chế độ: PLAY MODE (số liệu là trạng thái đang chạy thật)."
            : "Chế độ: EDIT MODE (manager chưa Awake — trạng thái suy ra từ PlayerPrefs + scene).");
        log.AppendLine();

        // ── 1. Scene: BoatSystem & manager
        var mgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        if (mgr == null)
        {
            log.AppendLine("[1] BoatSystem/BoatDockManager: KHÔNG TÌM THẤY trong scene đang mở.");
            nguyenNhan.Add("Scene chưa có BoatSystem — chạy menu 1. Setup All trước.");
            KetThuc(log, nguyenNhan);
            return;
        }

        log.AppendLine($"[1] Manager: '{mgr.name}' (GameObject '{mgr.gameObject.name}')");
        string chaTat = ChuoiChaBiTat(mgr.transform);
        if (chaTat != null)
        {
            log.AppendLine($"    LỖI: cả cụm bị tắt vì object '{chaTat}' đang SetActive(false).");
            nguyenNhan.Add($"Object '{chaTat}' đang bị tắt (dấu tick ở đầu Inspector) — bật lại.");
        }
        if (!mgr.enabled)
        {
            log.AppendLine("    LỖI: component BoatDockManager bị disable (bỏ tick).");
            nguyenNhan.Add("Component BoatDockManager đang bị bỏ tick — bật lại.");
        }
        if (Application.isPlaying)
            log.AppendLine($"    IsReady = {mgr.IsReady}  |  IsIntroDone = {mgr.IsIntroDone}");

        // ── 2. Config
        TouristBoatConfig cfg = mgr.Config;
        if (cfg == null)
        {
            log.AppendLine("[2] Config: CHƯA GÁN.");
            nguyenNhan.Add("Field Config trên BoatDockManager đang trống — kéo TouristBoatConfig.asset vào, hoặc chạy lại menu 1.");
            KetThuc(log, nguyenNhan);
            return;
        }
        log.AppendLine($"[2] Config: '{cfg.name}'  |  mở ở cấp {cfg.unlockLevel}"
                       + $"  |  đậu {cfg.dockMinutes} phút, núp {cfg.hideMinutes} phút"
                       + $"  |  tốc độ {cfg.boatSpeed}  |  debugTimeScale = {cfg.debugTimeScale}");
        log.AppendLine($"    Bến 2: cấp {cfg.dock2Level} / {cfg.dock2GoldCost} vàng"
                       + $"   |   Bến 3: cấp {cfg.dock3Level} / {cfg.dock3GemCost} kim cương");

        // ── 3. Cấp người chơi — nghi vấn số 1
        int capHienTai = -1;
        var lvl = UnityEngine.Object.FindFirstObjectByType<FarmLevelManager>(FindObjectsInactive.Include);
        if (lvl != null && Application.isPlaying) capHienTai = lvl.CurrentLevel;

        log.AppendLine();
        if (capHienTai < 0)
        {
            log.AppendLine($"[3] Cấp người chơi: chỉ đọc được trong Play Mode. Tàu mở ở cấp {cfg.unlockLevel}.");
            if (lvl == null)
                nguyenNhan.Add("Scene không có FarmLevelManager — intro mở bến sẽ không bao giờ chạy.");
        }
        else if (capHienTai < cfg.unlockLevel)
        {
            log.AppendLine($"[3] Cấp người chơi: {capHienTai}  <  cấp mở khóa {cfg.unlockLevel}");
            log.AppendLine("    ==> ĐÂY LÀ LÝ DO KHÔNG THẤY TÀU. Bến chưa mở nên cả 3 tàu bị ẩn hoàn toàn.");
            nguyenNhan.Add($"Bạn đang ở cấp {capHienTai}, tàu chỉ mở từ cấp {cfg.unlockLevel} — ĐÂY là nguyên nhân. "
                           + "Bấm menu 7. Test Ngay để xem tàu lập tức, không cần lên cấp.");
        }
        else
        {
            log.AppendLine($"[3] Cấp người chơi: {capHienTai}  >=  {cfg.unlockLevel} — điều kiện cấp ĐẠT.");
        }

        // ── 4. Save (PlayerPrefs)
        log.AppendLine();
        log.AppendLine($"[4] Save: {KeyIntroDone} = {PlayerPrefs.GetInt(KeyIntroDone, 0)}"
                       + "  (1 = đã xem intro; xóa bằng menu 8 nếu muốn diễn lại)");
        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            string kU = string.Format(CultureInfo.InvariantCulture, KeyUnlockedFormat, i);
            string kA = string.Format(CultureInfo.InvariantCulture, KeyAnchorFormat, i);
            log.AppendLine($"    Bến {i + 1}: unlocked={PlayerPrefs.GetInt(kU, 0)}"
                           + $"  anchor='{PlayerPrefs.GetString(kA, "(chưa có)")}'");
        }

        // ── 5. Điểm mù
        log.AppendLine();
        Transform blind = Application.isPlaying ? mgr.GetBlindPoint() : mgr.transform.Find("BlindPoint");
        if (blind == null)
        {
            log.AppendLine("[5] BlindPoint: KHÔNG TÌM THẤY (tàu sẽ lấy waypoint đầu làm điểm mù).");
            nguyenNhan.Add("Thiếu object 'BlindPoint' dưới BoatSystem — chạy lại menu 1.");
        }
        else
        {
            log.AppendLine($"[5] BlindPoint tại {V(blind.position)}");
        }

        // ── 6. Từng bến: wiring + trạng thái + vị trí
        var viTriBerth = new Vector3?[BoatDockManager.DockCount];
        bool coTauNaoHien = false;

        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            log.AppendLine();
            log.AppendLine($"───── BẾN {i + 1} ─────");

            Transform dock = mgr.transform.Find($"Dock_{i + 1:00}");
            if (dock == null)
            {
                log.AppendLine($"  Không có object 'Dock_{i + 1:00}'.");
                nguyenNhan.Add($"Thiếu Dock_{i + 1:00} — chạy lại menu 1. Setup All (không phá vị trí đã kéo).");
                continue;
            }

            string dockTat = ChuoiChaBiTat(dock);
            if (dockTat != null)
                log.AppendLine($"  LƯU Ý: bị tắt bởi object '{dockTat}'.");

            // Berth
            Transform berth = Application.isPlaying ? mgr.GetDockBerth(i) : dock.Find("Berth");
            if (berth == null)
            {
                log.AppendLine("  Berth: THIẾU — tàu không biết đậu ở đâu.");
                nguyenNhan.Add($"Bến {i + 1} thiếu object 'Berth'.");
            }
            else
            {
                viTriBerth[i] = berth.position;
                log.AppendLine($"  Berth tại {V(berth.position)}");
            }

            // Path / waypoint
            Transform path = dock.Find("Path");
            int soWP = 0;
            if (path != null) soWP = path.childCount;
            log.AppendLine($"  Path: {soWP} waypoint");
            if (soWP < 1)
                nguyenNhan.Add($"Bến {i + 1} chưa có waypoint — bấm menu 4. Tự Sinh Lại Waypoints.");

            if (blind != null && berth != null)
            {
                float dai = Vector3.Distance(blind.position, berth.position);
                float giay = cfg.boatSpeed > 0.01f ? dai / cfg.boatSpeed : -1f;
                log.AppendLine($"  Khoảng điểm mù -> bến: {dai:0} unit  ~  {giay:0.0} giây chạy"
                               + $" (tốc độ {cfg.boatSpeed})");
                if (dai < 200f)
                    log.AppendLine("  LƯU Ý: quá gần — tàu sẽ hiện ngay trong khung hình, mất cảm giác từ xa vào bến.");
            }

            // Controller + dockIndex + visual  (đọc private [SerializeField] qua SerializedObject)
            var boat = dock.GetComponentInChildren<TouristBoatController>(true);
            if (boat == null)
            {
                log.AppendLine("  TouristBoatController: THIẾU — bến này không có tàu.");
                nguyenNhan.Add($"Bến {i + 1} thiếu Boat/TouristBoatController — chạy lại menu 1.");
                continue;
            }

            var so = new SerializedObject(boat);
            int dockIdx = so.FindProperty("dockIndex") != null ? so.FindProperty("dockIndex").intValue : -999;
            var pVisual = so.FindProperty("visual");
            var visual  = pVisual != null ? pVisual.objectReferenceValue as SpriteRenderer : null;

            log.AppendLine($"  Controller: '{boat.name}'  |  dockIndex = {dockIdx}  |  enabled = {boat.enabled}");
            if (dockIdx < 0)
            {
                log.AppendLine("  LỖI NẶNG: dockIndex = -1 -> tàu tắt VĨNH VIỄN và im lặng.");
                nguyenNhan.Add($"Tàu bến {i + 1} có dockIndex = -1 (chưa wire). Sửa: gán dockIndex = {i} "
                               + "trong Inspector, hoặc đổi tên object cha về đúng 'Dock_" + (i + 1).ToString("00") + "' rồi chạy lại menu 1.");
            }
            else if (dockIdx != i)
            {
                log.AppendLine($"  LỖI: dockIndex = {dockIdx} nhưng đang nằm dưới Dock_{i + 1:00}.");
                nguyenNhan.Add($"Tàu dưới Dock_{i + 1:00} có dockIndex = {dockIdx} (lệch) — sửa thành {i}.");
            }

            // Visual: null / thiếu sprite / bị tắt / sorting
            if (visual == null)
            {
                log.AppendLine("  Visual: CHƯA GÁN -> không có gì để hiện.");
                nguyenNhan.Add($"Tàu bến {i + 1}: field Visual đang trống. Kéo SpriteRenderer của con tàu vào field Visual "
                               + "trên TouristBoatController.");
            }
            else
            {
                bool coSprite = visual.sprite != null;
                log.AppendLine($"  Visual: '{visual.name}'  |  sprite = "
                               + (coSprite ? visual.sprite.name : "(TRỐNG)")
                               + $"  |  enabled = {visual.enabled}"
                               + $"  |  layer '{visual.sortingLayerName}' order {visual.sortingOrder}"
                               + $"  |  alpha = {visual.color.a:0.00}"
                               + $"  |  scale = {V(visual.transform.lossyScale)}");
                if (!coSprite)
                    nguyenNhan.Add($"Tàu bến {i + 1}: SpriteRenderer chưa có sprite — kéo ảnh tàu vào.");
                if (!visual.enabled)
                    nguyenNhan.Add($"Tàu bến {i + 1}: SpriteRenderer bị bỏ tick.");
                if (visual.color.a < 0.05f)
                    nguyenNhan.Add($"Tàu bến {i + 1}: màu Visual gần như trong suốt (alpha {visual.color.a:0.00}).");
                if (visual.drawMode != SpriteDrawMode.Simple)
                    log.AppendLine($"  LƯU Ý: Draw Mode = {visual.drawMode} — nên để Simple, Sliced làm sprite tàu bị méo.");
            }

            // Trạng thái pha hiện tại
            string trangThai = MoTaTrangThai(mgr, cfg, i, out bool dangHien);
            log.AppendLine($"  TRẠNG THÁI: {trangThai}");
            if (dangHien) coTauNaoHien = true;
        }

        // ── 7. Hai dock đặt chồng nhau
        log.AppendLine();
        for (int a = 0; a < BoatDockManager.DockCount; a++)
        {
            for (int b = a + 1; b < BoatDockManager.DockCount; b++)
            {
                if (!viTriBerth[a].HasValue || !viTriBerth[b].HasValue) continue;
                float d = Vector3.Distance(viTriBerth[a].Value, viTriBerth[b].Value);
                if (d < NguongTrungViTri)
                {
                    log.AppendLine($"[7] Bến {a + 1} và bến {b + 1} gần như TRÙNG vị trí (cách {d:0} unit).");
                    nguyenNhan.Add($"Bến {a + 1} và {b + 1} đặt chồng nhau — kéo tách ra 3 ô đậu khác nhau.");
                }
            }
        }

        // ── 8. Sorting so với nền nước (best effort)
        CanhBaoSorting(log, nguyenNhan, mgr);

        // ── 9. Kết luận chung khi không tìm ra lỗi cấu hình nào
        if (nguyenNhan.Count == 0 && !coTauNaoHien)
        {
            nguyenNhan.Add("Cấu hình ĐÚNG hết — tàu đang trong pha Hidden (núp ở điểm mù) nên chưa hiện. "
                           + "Xem mục TRẠNG THÁI ở trên để biết còn bao lâu, hoặc bấm menu 7 để thấy ngay.");
        }
        if (nguyenNhan.Count == 0 && coTauNaoHien)
        {
            nguyenNhan.Add("Có tàu đang ở trạng thái HIỆN mà bạn vẫn không thấy trên màn hình: "
                           + "khả năng cao là camera không nhìn tới vị trí đó, hoặc tàu bị nền/lớp khác che. "
                           + "So sánh toạ độ Berth ở trên với vùng camera đang quay.");
        }

        KetThuc(log, nguyenNhan);
    }

    // ───────────────────────── 7. TEST NGAY ─────────────────────────

    [MenuItem(Menu7, false, 61)]
    public static void TestNgay()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                "Chức năng này cần đang chạy game.\n\nBấm Play trước, rồi gọi lại menu 7.", "OK");
            return;
        }

        var mgr = UnityEngine.Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        if (mgr == null || mgr.Config == null)
        {
            EditorUtility.DisplayDialog("Tourist Boat",
                "Không tìm thấy BoatDockManager hoặc chưa gán Config.\nChạy menu 6. Chẩn Đoán để biết chi tiết.", "OK");
            return;
        }

        var cfg = mgr.Config;
        var bc  = new StringBuilder();

        // 1) Nâng cấp người chơi nếu đang thấp hơn mốc mở khóa.
        var lvl = UnityEngine.Object.FindFirstObjectByType<FarmLevelManager>(FindObjectsInactive.Include);
        if (lvl != null && lvl.CurrentLevel < cfg.unlockLevel)
        {
            int cu = lvl.CurrentLevel;
            lvl.SetLevel(cfg.unlockLevel);
            bc.AppendLine($"- Đặt cấp người chơi: {cu} -> {cfg.unlockLevel}");
        }

        // 2) Mở bến 1 nếu chưa mở. UnlockDockFree đặt anchor để tàu Arriving ngay.
        if (!mgr.IsDockUnlocked(0))
        {
            mgr.UnlockDockFree(0);
            mgr.MarkIntroDone();
            bc.AppendLine("- Mở bến 1 (miễn phí) — tàu bắt đầu chạy vào ngay.");
        }

        // 3) Ép pha sang Docked để thấy tàu đậu tại bến trong 1 giây.
        //    _anchorTicks là private readonly long[] (không serialize) nên chỉ tới được
        //    bằng reflection — chấp nhận vì đây là tool debug chỉ sống trong Editor.
        if (EpSangDocked(mgr, cfg, 0, out string loiEp))
            bc.AppendLine("- Ép tàu bến 1 sang trạng thái ĐANG ĐẬU để soi art/vị trí.");
        else
            bc.AppendLine($"- Không ép được pha ({loiEp}). Tàu vẫn sẽ tự chạy vào bến sau vài giây.");

        Transform berth = mgr.GetDockBerth(0);
        if (berth != null)
        {
            bc.AppendLine();
            bc.AppendLine($"Tàu sẽ đậu tại toạ độ {V(berth.position)} — kéo camera tới đó nếu chưa thấy.");
        }

        Debug.Log("[TouristBoat] TEST NGAY:\n" + bc);
        EditorUtility.DisplayDialog("Tourist Boat — Test Ngay",
            "Đã làm:\n\n" + bc + "\nNhìn vào Game view. Nếu vẫn không thấy tàu, chạy menu 6. Chẩn Đoán.", "OK");
    }

    // ───────────────────────── 8. XÓA SAVE ─────────────────────────

    [MenuItem(Menu8, false, 62)]
    public static void XoaSave()
    {
        if (!EditorUtility.DisplayDialog("Xóa save bến tàu?",
            "Sẽ xóa toàn bộ PlayerPrefs TouristBoat_* :\n"
            + "- trạng thái mở khóa 3 bến\n- mốc thời gian (anchor) của tàu\n- cờ đã xem intro\n\n"
            + "Tiến trình khác của game KHÔNG bị ảnh hưởng.",
            "Xóa", "Hủy"))
            return;

        PlayerPrefs.DeleteKey(KeyIntroDone);
        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            PlayerPrefs.DeleteKey(string.Format(CultureInfo.InvariantCulture, KeyUnlockedFormat, i));
            PlayerPrefs.DeleteKey(string.Format(CultureInfo.InvariantCulture, KeyAnchorFormat, i));
        }
        PlayerPrefs.Save();

        Debug.Log("[TouristBoat] Đã xóa save bến tàu — lần Play tới sẽ diễn lại intro khi đạt cấp mở khóa.");
        EditorUtility.DisplayDialog("Tourist Boat",
            "Đã xóa. Lần Play tới, khi đạt cấp mở khóa thì hội thoại intro sẽ chạy lại từ đầu.\n\n"
            + "Đang trong Play Mode thì cần Stop rồi Play lại.", "OK");
    }

    // ───────────────────────── Helper ─────────────────────────

    /// <summary>Mô tả pha hiện tại của 1 bến, kèm thời gian còn lại tới mốc kế.</summary>
    private static string MoTaTrangThai(BoatDockManager mgr, TouristBoatConfig cfg, int dockIndex, out bool dangHien)
    {
        dangHien = false;

        bool moKhoa = Application.isPlaying
            ? mgr.IsDockUnlocked(dockIndex)
            : PlayerPrefs.GetInt(string.Format(CultureInfo.InvariantCulture, KeyUnlockedFormat, dockIndex), 0) == 1;

        if (!moKhoa)
            return $"CHƯA MỞ KHÓA -> tàu bị ẩn hoàn toàn (bình thường; mở ở cấp "
                   + (dockIndex == 0 ? cfg.unlockLevel : (dockIndex == 1 ? cfg.dock2Level : cfg.dock3Level)) + ").";

        // Anchor: Play Mode lấy qua TryGetPhaseInfo (chính xác nhất); Edit Mode đọc prefs.
        if (Application.isPlaying && mgr.TryGetPhaseInfo(dockIndex, out BoatPhaseInfo info))
        {
            dangHien = info.State != BoatState.Hidden && info.State != BoatState.Locked;
            double conLai = ThoiGianConLai(info, cfg, mgr);
            return $"{TenTrangThai(info.State)}  |  còn {DinhDangGiay(conLai)} tới mốc kế"
                   + (info.State == BoatState.Docked
                        ? $"  (đang đậu, còn {DinhDangGiay(info.DockedRemainingSeconds)})"
                        : string.Empty);
        }

        string sAnchor = PlayerPrefs.GetString(
            string.Format(CultureInfo.InvariantCulture, KeyAnchorFormat, dockIndex), string.Empty);
        if (!long.TryParse(sAnchor, NumberStyles.Integer, CultureInfo.InvariantCulture, out long anchor))
            return "ĐÃ MỞ KHÓA nhưng chưa có anchor — sẽ tính lại khi vào Play Mode.";

        float travel = cfg.fallbackTravelSeconds;
        var phase = BoatScheduleCore.ComputePhase(
            DateTime.UtcNow.Ticks, anchor,
            cfg.DockSeconds, cfg.HideSeconds, travel, 1.0);
        dangHien = phase.State != BoatState.Hidden && phase.State != BoatState.Locked;
        return $"{TenTrangThai(phase.State)} (ước tính ở Edit Mode, travel giả định {travel:0}s)";
    }

    private static double ThoiGianConLai(BoatPhaseInfo info, TouristBoatConfig cfg, BoatDockManager mgr)
    {
        double hide   = cfg.HideSeconds;
        double dock   = cfg.DockSeconds;
        double travel = Application.isPlaying ? mgr.GetScheduleTravelSeconds() : cfg.fallbackTravelSeconds;
        double p = info.PhaseSeconds;

        double mocArriving = hide;
        double mocDocked   = hide + travel;
        double mocDepart   = hide + travel + dock;

        if (p < mocArriving) return mocArriving - p;
        if (p < mocDocked)   return mocDocked - p;
        if (p < mocDepart)   return mocDepart - p;
        return info.CycleSeconds - p;
    }

    /// <summary>
    /// Đẩy anchor để pha rơi đúng đầu đoạn Docked. Dùng reflection vì _anchorTicks
    /// là private readonly array, không có đường công khai nào ghi vào.
    /// </summary>
    private static bool EpSangDocked(BoatDockManager mgr, TouristBoatConfig cfg, int dockIndex, out string loi)
    {
        loi = string.Empty;
        try
        {
            var f = typeof(BoatDockManager).GetField("_anchorTicks",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) { loi = "không thấy field _anchorTicks"; return false; }

            var arr = f.GetValue(mgr) as long[];
            if (arr == null || dockIndex >= arr.Length) { loi = "mảng anchor không hợp lệ"; return false; }

            double scale  = Math.Max(0.0001f, cfg.debugTimeScale);
            double travel = mgr.GetScheduleTravelSeconds();
            // phase = elapsed * scale; muốn phase = hide + travel + 1 giây đệm
            double elapsed = (cfg.HideSeconds + travel + 1.0) / scale;
            arr[dockIndex] = DateTime.UtcNow.Ticks - (long)(elapsed * TimeSpan.TicksPerSecond);
            return true;
        }
        catch (Exception e)
        {
            loi = e.Message;
            return false;
        }
    }

    /// <summary>Cảnh báo nếu tàu có sortingOrder thấp hơn sprite nền nước/biển tìm được.</summary>
    private static void CanhBaoSorting(StringBuilder log, List<string> nguyenNhan, BoatDockManager mgr)
    {
        string[] tuKhoaNuoc = { "nuoc", "water", "sea", "bien", "ocean", "wave", "song" };
        SpriteRenderer nen = null;

        foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            string ten = sr.name.ToLowerInvariant();
            foreach (var k in tuKhoaNuoc)
            {
                if (!ten.Contains(k)) continue;
                if (nen == null || sr.sortingOrder > nen.sortingOrder) nen = sr;
                break;
            }
        }

        if (nen == null) return;

        log.AppendLine($"[8] Nền nước tìm được: '{nen.name}' layer '{nen.sortingLayerName}' order {nen.sortingOrder}");
        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            Transform dock = mgr.transform.Find($"Dock_{i + 1:00}");
            if (dock == null) continue;
            var boat = dock.GetComponentInChildren<TouristBoatController>(true);
            if (boat == null) continue;
            var so = new SerializedObject(boat);
            var p = so.FindProperty("visual");
            var v = p != null ? p.objectReferenceValue as SpriteRenderer : null;
            if (v == null) continue;

            if (v.sortingLayerName == nen.sortingLayerName && v.sortingOrder <= nen.sortingOrder)
            {
                log.AppendLine($"    Bến {i + 1}: tàu order {v.sortingOrder} <= nền {nen.sortingOrder} -> có thể bị nền che.");
                nguyenNhan.Add($"Tàu bến {i + 1} có Order in Layer ({v.sortingOrder}) không cao hơn nền nước "
                               + $"({nen.sortingOrder}) — tăng Order in Layer của tàu lên.");
            }
        }
    }

    /// <summary>Trả về tên object đầu tiên (tính từ chính nó lên gốc) đang bị SetActive(false).</summary>
    private static string ChuoiChaBiTat(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
            if (!p.gameObject.activeSelf) return p.name;
        return null;
    }

    private static string TenTrangThai(BoatState s)
    {
        switch (s)
        {
            case BoatState.Locked:    return "CHƯA MỞ KHÓA (ẩn)";
            case BoatState.Hidden:    return "ĐANG NÚP Ở ĐIỂM MÙ (ẩn — đúng thiết kế)";
            case BoatState.Arriving:  return "ĐANG CHẠY VÀO BẾN (hiện)";
            case BoatState.Docked:    return "ĐANG ĐẬU Ở BẾN (hiện)";
            case BoatState.Departing: return "ĐANG LÙI RA KHỎI BẾN (hiện)";
            default:                  return s.ToString();
        }
    }

    private static string DinhDangGiay(double giay)
    {
        if (giay < 0) giay = 0;
        int tong = (int)Math.Round(giay);
        return tong >= 60 ? $"{tong / 60} phút {tong % 60} giây" : $"{tong} giây";
    }

    private static string V(Vector3 v) => $"({v.x:0}, {v.y:0})";

    private static void KetThuc(StringBuilder log, List<string> nguyenNhan)
    {
        log.AppendLine();
        log.AppendLine("===== KẾT LUẬN =====");
        if (nguyenNhan.Count == 0)
        {
            log.AppendLine("Không phát hiện vấn đề nào.");
        }
        else
        {
            for (int i = 0; i < nguyenNhan.Count; i++)
                log.AppendLine($"{i + 1}. {nguyenNhan[i]}");
        }

        Debug.Log(log.ToString());

        var tomTat = new StringBuilder();
        if (nguyenNhan.Count == 0)
        {
            tomTat.AppendLine("Không phát hiện vấn đề cấu hình nào.");
        }
        else
        {
            tomTat.AppendLine($"Tìm thấy {nguyenNhan.Count} điểm cần xử lý:");
            tomTat.AppendLine();
            for (int i = 0; i < nguyenNhan.Count && i < 6; i++)
                tomTat.AppendLine($"{i + 1}. {nguyenNhan[i]}");
            if (nguyenNhan.Count > 6)
                tomTat.AppendLine($"... và {nguyenNhan.Count - 6} mục nữa.");
        }
        tomTat.AppendLine();
        tomTat.Append("Báo cáo đầy đủ đã in ra Console.");

        EditorUtility.DisplayDialog("Tourist Boat — Chẩn Đoán", tomTat.ToString(), "OK");
    }
}
#endif
