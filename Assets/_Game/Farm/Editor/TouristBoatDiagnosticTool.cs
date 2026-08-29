#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
/// hoặc tàu đang chờ chuyến kế ngoài điểm mù.
///
/// ── VÁ CHO V2 (BOAT-002) ────────────────────────────────────────────────
/// V1 tính trạng thái theo CHU KỲ CỐ ĐỊNH (núp 15' → vào → đậu 40' → lùi) nên
/// tool cũ in "còn bao lâu tới mốc kế" bằng phép modulo, và menu 7 ép pha bằng
/// reflection vào field private <c>_anchorTicks</c>. V2 bỏ cả hai thứ đó:
///   • Trạng thái giờ là MÁY TRẠNG THÁI persist (WaitingNext → Arriving →
///     Docked VÔ HẠN → Departing), tàu đậu tới khi khách xong.
///   • Tool đọc trạng thái qua API chính thức Dev A mở sẵn:
///     <c>EditorDescribeState</c> / <c>EditorForceDockNow</c> /
///     <c>EditorForceDepartNow</c> — KHÔNG còn reflection.
///   • Menu 8 xóa thêm save V2 của Dev A, save khách của Dev B
///     (<c>TouristTrip_{dock}</c>) và cờ popup của Dev C
///     (<c>TouristBoat_DaBaoChuyen_{dock}</c>) — xóa nửa vời sẽ để lại khách mồ côi.
///
/// Menu (GIỮ NGUYÊN TÊN — Sếp không phải học lại):
///   6. Chẩn Đoán            — in mọi nguyên nhân khả dĩ + kết luận, chạy được cả Edit/Play Mode
///   7. Test Ngay            — ép tàu cập bến để soi art/vị trí/sorting (Play Mode)
///   8. Xóa Save Tàu         — xóa PlayerPrefs của CẢ 3 dev để diễn lại intro sạch sẽ
/// </summary>
public static class TouristBoatDiagnosticTool
{
    private const string Menu6 = "Tools/Farm Game/Tourist Boat/6. Chẩn Đoán — Vì Sao Không Thấy Tàu";
    private const string Menu7 = "Tools/Farm Game/Tourist Boat/7. Test Ngay — Cho Tàu Cập Bến";
    private const string Menu8 = "Tools/Farm Game/Tourist Boat/8. Xóa Save Tàu (chơi lại intro)";

    // ─── PlayerPrefs keys ────────────────────────────────────────────────
    // V1 (Dev A — vẫn còn trên máy người chơi cũ):
    private const string KeyUnlockedFormat = "TouristBoat_Unlocked_{0}";
    private const string KeyAnchorV1Format = "TouristBoat_AnchorUtc_{0}";
    private const string KeyIntroDone      = "TouristBoat_IntroDone";
    // V2 (Dev A — máy trạng thái mới):
    private const string KeyStateFormat        = "TouristBoat_V2_State_{0}";
    private const string KeyStateAnchorFormat  = "TouristBoat_V2_Anchor_{0}";
    private const string KeyNextArrivalFormat  = "TouristBoat_V2_NextArrival_{0}";
    private const string KeySchemaVersion      = "TouristBoat_ScheduleVersion";
    // Dev B — save chuyến khách (TouristVisitorManager.KeyTripFormat):
    private const string KeyTripFormat         = "TouristTrip_{0}";
    // Dev C — cờ "đã báo popup chuyến này" (BoatAnnouncePopupUI.KeyDaBaoFormat):
    private const string KeyDaBaoChuyenFormat  = "TouristBoat_DaBaoChuyen_{0}";

    // Hai dock cách nhau dưới ngưỡng này coi như bị đặt chồng ("1 cục").
    private const float NguongTrungViTri = 50f;

    // ───────────────────────── 6. CHẨN ĐOÁN ─────────────────────────

    [MenuItem(Menu6, false, 60)]
    public static void ChanDoan()
    {
        var log = new StringBuilder();
        // nguyenNhan = danh sách kết luận theo thứ tự ưu tiên, hiện trên dialog.
        var nguyenNhan = new List<string>();

        log.AppendLine("===== CHẨN ĐOÁN TOURIST BOAT (V2 event-driven) =====");
        log.AppendLine(Application.isPlaying
            ? "Chế độ: PLAY MODE (số liệu là trạng thái đang chạy thật)."
            : "Chế độ: EDIT MODE (manager chưa Awake — trạng thái đọc từ PlayerPrefs + scene).");
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
            log.AppendLine($"    IsReady = {mgr.IsReady}  |  IsIntroDone = {mgr.IsIntroDone}"
                           + $"  |  số bến đã mở = {mgr.UnlockedDockCount}");

        // ── 2. Config (số V2, không còn dockMinutes/hideMinutes)
        TouristBoatConfig cfg = mgr.Config;
        if (cfg == null)
        {
            log.AppendLine("[2] Config: CHƯA GÁN.");
            nguyenNhan.Add("Field Config trên BoatDockManager đang trống — kéo TouristBoatConfig.asset vào, hoặc chạy lại menu 1.");
            KetThuc(log, nguyenNhan);
            return;
        }
        log.AppendLine($"[2] Config: '{cfg.name}'  |  mở ở cấp {cfg.unlockLevel}"
                       + $"  |  tốc độ tàu {cfg.boatSpeed}  |  debugTimeScale = {cfg.debugTimeScale}");
        log.AppendLine($"    LỊCH V2: gap 1 bến {cfg.gapOneDockMinutes} phút"
                       + $"  |  gap nhiều bến {cfg.gapMultiDockMinutes} phút"
                       + $"  |  so le tối thiểu {cfg.minStaggerMinutes} phút");
        log.AppendLine($"    LƯỚI AN TOÀN: maxDockMinutes = {cfg.maxDockMinutes} phút"
                       + (cfg.maxDockMinutes <= 0f ? "  (ĐANG TẮT — tàu có thể đậu vô hạn)" : string.Empty)
                       + $"  |  kiên nhẫn khách {cfg.patienceMinutes} phút");
        // [QA M-7] BẰNG NHAU cũng là lỗi: lưới an toàn đếm từ lúc tàu CHẠM BẾN, còn kiên
        // nhẫn khách chỉ chạy từ lúc bubble mở (sau khi xuống tàu + đi bộ + tới lượt) nên
        // maxDock == patience ⇒ tàu luôn bị ép rời trước, nhánh "khách giận tự về" thành code chết.
        if (cfg.maxDockMinutes > 0f && cfg.maxDockMinutes <= cfg.patienceMinutes)
            nguyenNhan.Add($"Config lệch (QA M-7): maxDockMinutes ({cfg.maxDockMinutes}) <= patienceMinutes ({cfg.patienceMinutes}) "
                           + "— lưới an toàn cắt chuyến TRƯỚC khi khách kịp hết kiên nhẫn ⇒ nhánh 'khách giận tự về tàu' "
                           + "không bao giờ chạy. Quy ước: maxDockMinutes PHẢI LỚN HƠN patienceMinutes (mặc định 35 vs 30).");
        log.AppendLine($"    Bến 2: cấp {cfg.dock2Level} / {cfg.dock2GoldCost} vàng"
                       + $"   |   Bến 3: cấp {cfg.dock3Level} / {cfg.dock3GemCost} kim cương");
        log.AppendLine("    (dockMinutes/hideMinutes/staggerMinutes là field V1 — V2 KHÔNG dùng nữa.)");

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

        // ── 4. Save (PlayerPrefs) — V2
        log.AppendLine();
        log.AppendLine($"[4] Save: {KeyIntroDone} = {PlayerPrefs.GetInt(KeyIntroDone, 0)}"
                       + "  (1 = đã xem intro; xóa bằng menu 8 nếu muốn diễn lại)");
        log.AppendLine($"    Schema lịch: {KeySchemaVersion} = {PlayerPrefs.GetInt(KeySchemaVersion, 1)}"
                       + "  (1 = save V1 chưa migrate, 2 = đã V2)");
        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            log.AppendLine($"    Bến {i + 1}: unlocked={PlayerPrefs.GetInt(K(KeyUnlockedFormat, i), 0)}"
                           + $"  state={TenTrangThai(DocStatePrefs(i))}"
                           + $"  mốc='{PlayerPrefs.GetString(K(KeyStateAnchorFormat, i), "(chưa có)")}'"
                           + $"  chuyếnKế='{PlayerPrefs.GetString(K(KeyNextArrivalFormat, i), "-")}'");
            string trip = PlayerPrefs.GetString(K(KeyTripFormat, i), string.Empty);
            log.AppendLine($"       khách (Dev B) {K(KeyTripFormat, i)}: "
                           + (string.IsNullOrEmpty(trip) ? "(không có chuyến lưu)" : $"{trip.Length} ký tự JSON")
                           + $"  |  popup (Dev C) đã báo chuyến='{PlayerPrefs.GetString(K(KeyDaBaoChuyenFormat, i), "-")}'");
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

            // Trạng thái V2 thật (pha · giờ đậu đã trôi · chuyến kế · đã bị ép rời chưa)
            string trangThai = MoTaTrangThai(mgr, cfg, i, out bool dangHien, nguyenNhan);
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
            nguyenNhan.Add("Cấu hình ĐÚNG hết — tàu đang CHỜ CHUYẾN KẾ ngoài điểm mù nên chưa hiện. "
                           + "Xem mục TRẠNG THÁI ở trên để biết còn bao lâu tới giờ cập bến, hoặc bấm menu 7 để thấy ngay.");
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

    /// <summary>
    /// Ép tàu bến 1 cập bến NGAY để soi art/vị trí/sorting.
    /// V2: dùng API chính thức <c>BoatDockManager.EditorForceDockNow</c> (Dev A mở
    /// riêng cho tool này) — KHÔNG còn reflection vào field private như bản V1.
    /// Cập bến qua đường này bắn <c>OnBoatDocked</c> thật ⇒ Dev B spawn khách luôn,
    /// nên đây cũng là cách test nhanh cả luồng khách.
    /// </summary>
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

        // 2) Mở bến 1 nếu chưa mở (tàu bắt đầu chạy vào ngay).
        if (!mgr.IsDockUnlocked(0))
        {
            mgr.UnlockDockFree(0);
            mgr.MarkIntroDone();
            bc.AppendLine("- Mở bến 1 (miễn phí) — tàu bắt đầu chạy vào ngay.");
        }

        // 3) Ép cập bến ngay (V2: API chính thức, không reflection).
        if (mgr.IsDocked(0))
        {
            bc.AppendLine("- Tàu bến 1 ĐANG ĐẬU sẵn — không cần ép.");
        }
        else
        {
            mgr.EditorForceDockNow(0);
            bc.AppendLine("- Ép tàu bến 1 CẬP BẾN NGAY (bắn OnBoatDocked thật → Dev B spawn khách).");
        }

        bc.AppendLine($"- Trạng thái sau khi ép: {mgr.EditorDescribeState(0)}");
        bc.AppendLine();
        bc.AppendLine("Muốn xem tàu RỜI BẾN ngay: gọi BoatDockManager.EditorForceDepartNow(0)");
        bc.AppendLine("(hoặc để Dev B báo khách cuối lên tàu như luồng thật).");

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

    /// <summary>
    /// Xóa TOÀN BỘ save của hệ bến tàu — cả 3 dev.
    /// V2 bắt buộc xóa đủ 3 nhóm: nếu chỉ xóa key V1 như bản cũ thì state V2 và
    /// chuyến khách của Dev B còn nguyên ⇒ vào game lại thấy khách "mồ côi" của
    /// chuyến đã bị xóa, hoặc popup Dev C không báo lại vì tưởng đã báo rồi.
    /// </summary>
    [MenuItem(Menu8, false, 62)]
    public static void XoaSave()
    {
        if (!EditorUtility.DisplayDialog("Xóa save bến tàu?",
            "Sẽ xóa toàn bộ PlayerPrefs của hệ bến tàu:\n"
            + "- trạng thái mở khóa 3 bến + cờ đã xem intro\n"
            + "- lịch tàu V2 (state / mốc UTC / chuyến kế) + anchor V1 cũ\n"
            + "- chuyến khách đang lưu của 3 bến (TouristTrip_x — Dev B)\n"
            + "- cờ 'đã báo popup chuyến' của 3 bến (Dev C)\n\n"
            + "Tiến trình khác của game (vàng, cấp, kho) KHÔNG bị ảnh hưởng.",
            "Xóa", "Hủy"))
            return;

        int daXoa = 0;

        daXoa += Xoa(KeyIntroDone);
        daXoa += Xoa(KeySchemaVersion);

        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            daXoa += Xoa(K(KeyUnlockedFormat, i));
            daXoa += Xoa(K(KeyAnchorV1Format, i));       // V1 cũ (save đời trước)
            daXoa += Xoa(K(KeyStateFormat, i));          // V2 — Dev A
            daXoa += Xoa(K(KeyStateAnchorFormat, i));    // V2 — Dev A
            daXoa += Xoa(K(KeyNextArrivalFormat, i));    // V2 — Dev A
            daXoa += Xoa(K(KeyTripFormat, i));           // chuyến khách — Dev B
            daXoa += Xoa(K(KeyDaBaoChuyenFormat, i));    // cờ popup — Dev C
        }
        PlayerPrefs.Save();

        Debug.Log($"[TouristBoat] Đã xóa save bến tàu ({daXoa} key: lịch V2 của Dev A, chuyến khách của Dev B, "
                  + "cờ popup của Dev C) — lần Play tới sẽ diễn lại intro khi đạt cấp mở khóa.");
        EditorUtility.DisplayDialog("Tourist Boat",
            $"Đã xóa {daXoa} key (Dev A + Dev B + Dev C).\n\n"
            + "Lần Play tới, khi đạt cấp mở khóa thì hội thoại intro sẽ chạy lại từ đầu.\n\n"
            + "Đang trong Play Mode thì cần Stop rồi Play lại "
            + "(manager đang giữ state trong RAM, sẽ ghi đè lại khi lưu).", "OK");
    }

    /// <summary>Xóa 1 key nếu có — trả 1 nếu thực sự xóa, 0 nếu vốn không tồn tại.</summary>
    private static int Xoa(string key)
    {
        if (!PlayerPrefs.HasKey(key)) return 0;
        PlayerPrefs.DeleteKey(key);
        return 1;
    }

    // ───────────────────────── Helper ─────────────────────────

    private static string K(string format, int dockIndex)
        => string.Format(CultureInfo.InvariantCulture, format, dockIndex);

    /// <summary>
    /// Mô tả trạng thái V2 của 1 bến.
    /// Play Mode: hỏi thẳng manager qua <c>EditorDescribeState</c> (pha · giờ đậu đã
    /// trôi so với lưới an toàn · chuyến kế lúc nào · chuyến vừa rồi có bị ép rời không).
    /// Edit Mode: đọc PlayerPrefs V2 và mô tả tương đương (không tính tiến độ path).
    /// </summary>
    private static string MoTaTrangThai(BoatDockManager mgr, TouristBoatConfig cfg, int dockIndex,
                                        out bool dangHien, List<string> nguyenNhan)
    {
        dangHien = false;

        bool moKhoa = Application.isPlaying
            ? mgr.IsDockUnlocked(dockIndex)
            : PlayerPrefs.GetInt(K(KeyUnlockedFormat, dockIndex), 0) == 1;

        if (!moKhoa)
            return "CHƯA MỞ KHÓA -> tàu bị ẩn hoàn toàn (bình thường; mở ở cấp "
                   + (dockIndex == 0 ? cfg.unlockLevel : (dockIndex == 1 ? cfg.dock2Level : cfg.dock3Level)) + ").";

        if (Application.isPlaying && mgr.TryGetPhaseInfo(dockIndex, out BoatPhaseInfo info))
        {
            dangHien = info.State != BoatState.WaitingNext && info.State != BoatState.Locked;

            var s = new StringBuilder();
            s.Append(TenTrangThai(info.State));

            switch (info.State)
            {
                case BoatState.WaitingNext:
                    s.Append($"  |  còn {DinhDangGiay(info.PhaseSeconds)} tới giờ cập bến");
                    break;
                case BoatState.Arriving:
                    s.Append($"  |  đã đi {info.Progress * 100.0:0}% quãng đường vào bến");
                    break;
                case BoatState.Docked:
                    s.Append($"  |  đã đậu {DinhDangGiay(mgr.EditorDockedElapsedSeconds(dockIndex))}");
                    double maxDock = mgr.EditorMaxDockSeconds();
                    s.Append(maxDock > 0.0
                        ? $" / tối đa {DinhDangGiay(maxDock)} (lưới an toàn)"
                        : "  (lưới an toàn ĐANG TẮT — maxDockMinutes = 0)");
                    s.Append("  — V2 đậu tới khi khách xong, KHÔNG có countdown cố định");
                    break;
                case BoatState.Departing:
                    s.Append($"  |  đã lùi ra {info.Progress * 100.0:0}%");
                    break;
            }

            if (mgr.EditorIsDepartForcedByTimeout(dockIndex))
            {
                s.Append("  |  CHUYẾN VỪA RỒI BỊ ÉP RỜI do quá giờ đậu");
                nguyenNhan.Add($"Bến {dockIndex + 1}: chuyến vừa rồi bị LƯỚI AN TOÀN ép rời (khách chưa xong trong "
                               + $"{cfg.maxDockMinutes} phút). Kiểm tra TouristVisitorManager có gọi ReportVisitorsAllAboard không.");
            }

            s.Append("  ||  chi tiết: ").Append(mgr.EditorDescribeState(dockIndex));
            return s.ToString();
        }

        // ── Edit Mode: đọc thẳng prefs V2 ───────────────────────────────
        BoatState st = DocStatePrefs(dockIndex);
        dangHien = st != BoatState.WaitingNext && st != BoatState.Locked;

        string sAnchor = PlayerPrefs.GetString(K(KeyStateAnchorFormat, dockIndex), string.Empty);
        if (!long.TryParse(sAnchor, NumberStyles.Integer, CultureInfo.InvariantCulture, out long anchor) || anchor <= 0L)
            return "ĐÃ MỞ KHÓA nhưng chưa có lịch V2 — sẽ tạo chuyến mới khi vào Play Mode (tàu vào sau ~30 giây).";

        var moc = new DateTime(anchor, DateTimeKind.Utc);
        double lech = (DateTime.UtcNow - moc).TotalSeconds;

        switch (st)
        {
            case BoatState.WaitingNext:
                return $"{TenTrangThai(st)} (Edit Mode) | giờ cập bến hẹn lúc {moc:HH:mm:ss} UTC — "
                       + (lech < 0 ? $"còn {DinhDangGiay(-lech)}" : $"đã tới hạn {DinhDangGiay(lech)} trước, vào Play là tàu cập bến ngay");
            case BoatState.Arriving:
                return $"{TenTrangThai(st)} (Edit Mode) | cập bến lúc {moc:HH:mm:ss} UTC";
            case BoatState.Docked:
                return $"{TenTrangThai(st)} (Edit Mode) | chạm bến lúc {moc:HH:mm:ss} UTC — đã đậu {DinhDangGiay(lech)}"
                       + (cfg.maxDockMinutes > 0f ? $" / tối đa {cfg.maxDockMinutes} phút" : " (lưới an toàn tắt)");
            case BoatState.Departing:
                string next = PlayerPrefs.GetString(K(KeyNextArrivalFormat, dockIndex), string.Empty);
                string nextMoTa = long.TryParse(next, NumberStyles.Integer, CultureInfo.InvariantCulture, out long nt) && nt > 0
                    ? new DateTime(nt, DateTimeKind.Utc).ToString("HH:mm:ss") + " UTC"
                    : "(chưa có)";
                return $"{TenTrangThai(st)} (Edit Mode) | rời bến lúc {moc:HH:mm:ss} UTC — chuyến kế {nextMoTa}";
            default:
                return $"{TenTrangThai(st)} (Edit Mode)";
        }
    }

    /// <summary>Đọc state V2 của 1 bến từ PlayerPrefs (Edit Mode / phần in save).</summary>
    private static BoatState DocStatePrefs(int dockIndex)
    {
        if (PlayerPrefs.GetInt(K(KeyUnlockedFormat, dockIndex), 0) != 1)
            return BoatState.Locked;

        int raw = PlayerPrefs.GetInt(K(KeyStateFormat, dockIndex), (int)BoatState.WaitingNext);
        switch (raw)
        {
            case (int)BoatState.WaitingNext: return BoatState.WaitingNext;
            case (int)BoatState.Arriving:    return BoatState.Arriving;
            case (int)BoatState.Docked:      return BoatState.Docked;
            case (int)BoatState.Departing:   return BoatState.Departing;
            case (int)BoatState.Locked:      return BoatState.Locked;
            default:                         return BoatState.WaitingNext;
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

    /// <summary>
    /// Tên tiếng Việt của pha. Lưu ý: <c>BoatState.WaitingNext</c> và
    /// <c>BoatState.Hidden</c> là CÙNG một giá trị (alias V1) nên chỉ có 1 nhánh.
    /// </summary>
    private static string TenTrangThai(BoatState s)
    {
        switch (s)
        {
            case BoatState.Locked:      return "CHƯA MỞ KHÓA (ẩn)";
            case BoatState.WaitingNext: return "ĐANG CHỜ CHUYẾN KẾ ở điểm mù (ẩn — đúng thiết kế)";
            case BoatState.Arriving:    return "ĐANG CHẠY VÀO BẾN (hiện)";
            case BoatState.Docked:      return "ĐANG ĐẬU ĐÓN KHÁCH (hiện)";
            case BoatState.Departing:   return "ĐANG LÙI RA KHỎI BẾN (hiện)";
            default:                    return s.ToString();
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
