using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CÔNG CỤ XÁC MINH POPUP — chụp Game view ra PNG + xuất báo cáo trạng thái ra TXT.
///
/// Mục đích: cho phép kiểm tra popup có thật sự hiện trên màn hình hay không,
/// bằng dữ liệu thay vì phỏng đoán. File xuất ra:
///
///   Assets/_Debug_Capture/game_view.png     ← ảnh Game view
///   Assets/_Debug_Capture/popup_report.txt  ← trạng thái runtime đầy đủ
///
/// Cách dùng: bấm F10 trong Play Mode, hoặc gọi <see cref="CaptureNow"/>.
/// Component tự huỷ ở bản phát hành.
/// </summary>
public class PopupCaptureReporter : MonoBehaviour
{
    public const string OutFolder  = "Assets/_Debug_Capture";
    public const string PngName    = "game_view.png";
    public const string ReportName = "popup_report.txt";

    [Tooltip("Tên GameObject gốc của popup cần kiểm tra.")]
    public string popupRootName = "Popup_LevelUp_Township";

    [Tooltip("Nhân độ phân giải ảnh chụp (1 = nguyên bản).")]
    [Range(1, 2)] public int superSize = 1;

    private static PopupCaptureReporter _instance;

    /// <summary>Tự tạo instance khi vào Play — không cần gắn tay vào scene.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_instance != null) return;
        var go = new GameObject("~PopupCaptureReporter");
        Object.DontDestroyOnLoad(go);
        _instance = go.AddComponent<PopupCaptureReporter>();
#endif
    }

    private void Awake()
    {
        // Gán vô điều kiện — nếu để trong #else thì bản release báo CS0649
        // ("field never assigned") vì CaptureNow() vẫn đọc _instance.
        _instance = this;
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        Destroy(this);
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Dùng legacy Input để không phụ thuộc cấu hình Input System
        if (Input.GetKeyDown(KeyCode.F10)) CaptureNow();
#endif
    }

    /// <summary>Chụp ảnh + xuất báo cáo. Gọi được từ Editor tool.</summary>
    public static void CaptureNow()
    {
        // StartCoroutine không chạy ngoài Play Mode → chốt trước cho khỏi throw
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PopupCapture] Cần bấm Play trước khi chụp.");
            return;
        }
        if (_instance == null) AutoSpawn();
        if (_instance != null) _instance.StartCoroutine(_instance.CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        Directory.CreateDirectory(OutFolder);

        // Mỗi lần F10 lưu THÊM một bản có timestamp (capture_yyyyMMdd_HHmmss.*) để chụp nhiều bước
        // liên tiếp không mất ảnh cũ; game_view.png / popup_report.txt vẫn là "bản mới nhất".
        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Báo cáo viết TRƯỚC (không cần chờ frame)
        // [V6] Nối thêm mục TRẠNG THÁI TUTORIAL ở NGOÀI BuildReport(): hàm đó có đường
        // `return` sớm khi không tìm thấy popup, nối bên trong sẽ mất mục này đúng lúc
        // cần nhất (đứng im mà không có popup nào).
        string report = BuildReport() + BuildTutorialStateSection();
        File.WriteAllText(Path.Combine(OutFolder, ReportName), report, Encoding.UTF8);
        File.WriteAllText(Path.Combine(OutFolder, $"capture_{stamp}_report.txt"), report, Encoding.UTF8);

        // Ảnh: phải chờ hết frame mới có nội dung đã vẽ
        yield return new WaitForEndOfFrame();

        string png = Path.Combine(OutFolder, PngName);
        if (File.Exists(png)) File.Delete(png);

        // ScreenCapture ghi bất đồng bộ → chờ file xuất hiện
        ScreenCapture.CaptureScreenshot(png, Mathf.Max(1, superSize));

        float waited = 0f;
        while (!File.Exists(png) && waited < 5f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        string pngStamped = Path.Combine(OutFolder, $"capture_{stamp}.png");
        if (File.Exists(png))
        {
            try { File.Copy(png, pngStamped, true); }
            catch (System.Exception e) { Debug.LogWarning($"[PopupCapture] Không sao chép được bản timestamp: {e.Message}"); }
        }

        Debug.Log($"[PopupCapture] Xong.\n" +
                  $"   • Ảnh    : {png} {(File.Exists(png) ? "✔" : "✘ (chưa ghi được)")}\n" +
                  $"   • Bản lưu: {pngStamped} {(File.Exists(pngStamped) ? "✔" : "✘")}\n" +
                  $"   • Báo cáo: {Path.Combine(OutFolder, ReportName)} ✔");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    // ════════════════════════════════════════════════════════════════════
    // BÁO CÁO
    // ════════════════════════════════════════════════════════════════════

    private string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════");
        sb.AppendLine("  BÁO CÁO TRẠNG THÁI POPUP LÊN CẤP");
        sb.AppendLine($"  Thời điểm : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  Màn hình  : {Screen.width} x {Screen.height}");
        sb.AppendLine($"  isPlaying : {Application.isPlaying}");
        sb.AppendLine("═══════════════════════════════════════════════════");
        sb.AppendLine();

        // ── 1. Tìm popup ─────────────────────────────────────────────────
        GameObject popup = FindByName(popupRootName);

        if (popup == null)
        {
            sb.AppendLine($"✘✘ KHÔNG TÌM THẤY '{popupRootName}' trong scene.");
            sb.AppendLine("   → Chưa dựng popup, hoặc scene chưa được lưu trước khi Play.");
            AppendAllCanvases(sb);
            return sb.ToString();
        }

        sb.AppendLine($"✔ Tìm thấy '{popup.name}'");
        sb.AppendLine($"   Đường dẫn: {Path_(popup.transform)}");
        sb.AppendLine();

        // ── 2. Chuỗi tổ tiên — chỉ ra CHÍNH XÁC cấp nào tắt ──────────────
        sb.AppendLine("── CHUỖI TỔ TIÊN (từ gốc xuống popup) ──");
        var chain = new System.Collections.Generic.List<Transform>();
        for (var t = popup.transform; t != null; t = t.parent) chain.Add(t);
        chain.Reverse();

        bool anyOff = false;
        foreach (var t in chain)
        {
            bool on = t.gameObject.activeSelf;
            if (!on) anyOff = true;
            sb.AppendLine($"   {(on ? "✔" : "✘ TẮT")}  {t.name}" +
                          $"   [scale={Fmt(t.localScale)}]");
        }
        sb.AppendLine($"   → activeInHierarchy của popup: " +
                      $"{(popup.activeInHierarchy ? "TRUE ✔" : "FALSE ✘")}");
        if (anyOff)
            sb.AppendLine("   ✘✘ CÓ TỔ TIÊN ĐANG TẮT → popup không thể hiện dù SetActive(true).");
        sb.AppendLine();

        // ── 3. Scale tích luỹ ────────────────────────────────────────────
        var ls = popup.transform.lossyScale;
        sb.AppendLine("── SCALE ──");
        sb.AppendLine($"   lossyScale = {Fmt(ls)}   " +
                      (Mathf.Abs(ls.x - 1f) < 0.05f ? "✔" : "✘ PHẢI ≈ 1 — popup đang bị co/giãn!"));
        sb.AppendLine();

        // ── 4. Canvas ────────────────────────────────────────────────────
        sb.AppendLine("── CANVAS ──");
        var canvases = popup.GetComponentsInParent<Canvas>(true);
        if (canvases.Length == 0)
            sb.AppendLine("   ✘✘ KHÔNG có Canvas nào phía trên → UI không thể vẽ.");
        foreach (var c in canvases)
        {
            sb.AppendLine($"   • '{c.name}'  renderMode={c.renderMode}  order={c.sortingOrder}" +
                          $"  overrideSorting={c.overrideSorting}  enabled={c.enabled}" +
                          $"  isRoot={(c.rootCanvas == c)}");
            var scaler = c.GetComponent<CanvasScaler>();
            sb.AppendLine($"       CanvasScaler: " +
                          (scaler != null
                            ? $"mode={scaler.uiScaleMode}, refRes={scaler.referenceResolution}"
                            : "KHÔNG CÓ" + (c.rootCanvas == c ? " ✘ (canvas gốc nên có)" : "")));
        }
        if (canvases.Length > 0 && canvases[canvases.Length - 1].renderMode == RenderMode.WorldSpace)
            sb.AppendLine("   ✘✘ CANVAS GỐC LÀ WORLD SPACE → popup nằm trong thế giới game, " +
                          "KHÔNG hiện trên màn hình!");
        sb.AppendLine();

        // ── 5. CanvasGroup ───────────────────────────────────────────────
        sb.AppendLine("── CANVAS GROUP ──");
        var cgs = popup.GetComponentsInChildren<CanvasGroup>(true);
        if (cgs.Length == 0) sb.AppendLine("   (không có)");
        foreach (var cg in cgs)
            sb.AppendLine($"   • '{cg.name}'  alpha={cg.alpha:F2}" +
                          (cg.alpha < 0.99f ? "  ✘ ALPHA < 1 → popup mờ/vô hình!" : "  ✔") +
                          $"  blocksRaycasts={cg.blocksRaycasts}");
        sb.AppendLine();

        // ── 6. Thành phần chính ──────────────────────────────────────────
        sb.AppendLine("── THÀNH PHẦN CHÍNH ──");
        foreach (var n in new[] { "Root_HienThi", "Bg_NenToi", "Content", "BangRon",
                                  "Than_BangRon", "Text_TieuDe", "NgoiSao", "Hinh_Sao",
                                  "Text_SoCap", "Hang_PhanThuong", "Dai_MoKhoa",
                                  "Btn_TiepTuc", "Text_Nut" })
        {
            var t = FindChildDeep(popup.transform, n);
            if (t == null) { sb.AppendLine($"   ✘ THIẾU: {n}"); continue; }

            var rt  = t as RectTransform;
            string size = rt != null ? $"{rt.rect.width:F0}x{rt.rect.height:F0}" : "-";
            string extra = "";

            var img = t.GetComponent<Image>();
            if (img != null)
                extra += $"  sprite={(img.sprite != null ? img.sprite.name : "NULL ✘")}" +
                         $"  alpha={img.color.a:F2}  enabled={img.enabled}";

            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                extra += $"  text=\"{tmp.text}\"  size={tmp.fontSize:F0}  alpha={tmp.color.a:F2}";

            sb.AppendLine($"   {(t.gameObject.activeInHierarchy ? "✔" : "✘tắt")} {n,-18} " +
                          $"rect={size,-12} pos={Fmt2(t.localPosition)}{extra}");
        }
        sb.AppendLine();

        // ── 7. Ô mở khoá ─────────────────────────────────────────────────
        var slots = popup.GetComponentsInChildren<UnlockSlotUI>(true);
        sb.AppendLine($"── Ô MỞ KHOÁ: {slots.Length} cái ──");

        int activeSlots = 0, withIcon = 0, activeNoIcon = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            var s      = slots[i];
            bool on    = s.gameObject.activeInHierarchy;
            bool hasIc = s.HasIcon;
            var  spr   = s.CurrentIcon;

            if (on) activeSlots++;
            if (hasIc) withIcon++;
            if (on && !hasIc) activeNoIcon++;

            // Chỉ liệt kê chi tiết ô đang BẬT — ô ẩn không quan trọng
            if (on)
                sb.AppendLine($"   [{i + 1}] BẬT   icon={(spr != null ? spr.name : "NULL ✘")}" +
                              $"   scale={Fmt(s.transform.localScale)}" +
                              (hasIc ? "  ✔" : "  ✘ Ô TRẮNG!"));
        }

        sb.AppendLine($"   → Đang bật: {activeSlots}/{slots.Length}   Có icon: {withIcon}");
        if (slots.Length > 0 && activeSlots == 0)
            sb.AppendLine("   ✘ TẤT CẢ BỊ TẮT!");
        if (activeNoIcon > 0)
            sb.AppendLine($"   ✘✘ CÓ {activeNoIcon} Ô ĐANG BẬT MÀ KHÔNG CÓ ICON → vẫn còn ô trắng!");
        if (activeSlots > 0 && activeNoIcon == 0)
            sb.AppendLine("   ✔ Mọi ô đang bật đều có icon.");

        // Ô kẹt scale 0 thì vô hình dù có icon
        int zeroScale = 0;
        foreach (var s in slots)
            if (s.gameObject.activeInHierarchy && s.transform.localScale.x < 0.01f) zeroScale++;
        if (zeroScale > 0)
            sb.AppendLine($"   ✘ {zeroScale} ô có localScale ≈ 0 → vô hình dù đã bật (coroutine pop bị huỷ).");
        sb.AppendLine();

        // ── 7b. Icon vàng/ngọc hàng Phần thưởng ──────────────────────────
        sb.AppendLine("── ICON TIỀN TỆ (hàng Phần thưởng) ──");
        foreach (var rowName in new[] { "Hang_Vang", "Hang_Ngoc" })
        {
            var row = FindChildDeep(popup.transform, rowName);
            if (row == null) { sb.AppendLine($"   ✘ THIẾU {rowName}"); continue; }

            var ic = FindChildDeep(row, "Icon");
            var im = ic != null ? ic.GetComponent<Image>() : null;
            if (im == null) { sb.AppendLine($"   ✘ {rowName}: không có Image 'Icon'"); continue; }

            string sn = im.sprite != null ? im.sprite.name : "NULL";
            bool placeholder = sn.StartsWith("spr_circle_fill");
            sb.AppendLine($"   {rowName,-12} sprite={sn,-34} " +
                          (placeholder ? "✘ VẪN LÀ ĐĨA TRÒN GIẢ — cần dựng lại popup"
                                       : "✔ sprite thật"));
        }
        sb.AppendLine();

        // ── 8. Trạng thái script ─────────────────────────────────────────
        sb.AppendLine("── LevelUpPopupUI ──");
        var ui = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (ui == null) sb.AppendLine("   ✘✘ KHÔNG có LevelUpPopupUI trong scene!");
        else
        {
            sb.AppendLine($"   Nằm trên  : '{ui.gameObject.name}'  " +
                          $"activeInHierarchy={ui.gameObject.activeInHierarchy}");
            sb.AppendLine($"   Đường dẫn : {Path_(ui.transform)}");
            sb.AppendLine($"   IsActive (static) = {LevelUpPopupUI.IsActive}");

            foreach (var fn in new[] { "_isShowing", "_lastKnownLevel" })
            {
                var f = typeof(LevelUpPopupUI).GetField(fn,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (f != null) sb.AppendLine($"   {fn} = {f.GetValue(ui)}");
            }

            var cf = typeof(LevelUpPopupUI).GetField("levelRewardConfigs",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (cf != null)
            {
                var list = cf.GetValue(ui) as System.Collections.ICollection;
                int n = list != null ? list.Count : 0;
                sb.AppendLine($"   levelRewardConfigs = {n}" +
                              (n == 0 ? "  ✘ RỖNG → không có vàng/ngọc/quà nào" : "  ✔"));
            }
        }
        sb.AppendLine();

        AppendAllCanvases(sb);

        // ── 9. Kết luận tự động ──────────────────────────────────────────
        sb.AppendLine("── KẾT LUẬN ──");
        bool ok = popup.activeInHierarchy
               && Mathf.Abs(ls.x - 1f) < 0.05f
               && canvases.Length > 0
               && canvases[canvases.Length - 1].renderMode != RenderMode.WorldSpace;
        sb.AppendLine(ok
            ? "✔ Popup ĐANG hiện đúng cấu hình. Hãy đối chiếu với ảnh game_view.png."
            : "✘ Popup CHƯA hiện được. Xem các dòng có dấu ✘ ở trên.");

        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════════════
    // [V6] TRẠNG THÁI TUTORIAL — để Lead chẩn đoán chỗ tutorial đứng im
    // ════════════════════════════════════════════════════════════════════
    //
    // TOÀN BỘ mục này nằm trong MỘT try/catch: báo cáo popup cũ đã dùng nhiều vòng,
    // không được phép hỏng vì một ref tutorial rơi. Hỏng thì in đúng một dòng lỗi.

    /// <summary>Số bước tutorial ĐẦY ĐỦ theo thiết kế (L1+L2). Thiếu ⇒ cảnh báo.</summary>
    private const int SO_BUOC_DAY_DU = 31;

    private static string BuildTutorialStateSection()
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("── TRẠNG THÁI TUTORIAL ──");

        try
        {
            var tm = TutorialManager.Instance;
            if (tm == null)
            {
                sb.AppendLine("   ✘ Không có TutorialManager trong scene (Instance = null).");
                string prefXongRoi = TenVaGiaTriPref("TUTORIAL_MAIN_DONE");
                string prefBuocRoi = TenVaGiaTriPref("TUTORIAL_STEP_INDEX");
                sb.AppendLine($"   PlayerPrefs      : {prefXongRoi} · {prefBuocRoi}");
                return sb.ToString();
            }

            // ── Tổng quan bước ──
            int tongBuoc = tm.TongSoBuoc;
            string canhBaoBuoc = tongBuoc < SO_BUOC_DAY_DU
                ? $" ⚠ (thiếu {SO_BUOC_DAY_DU - tongBuoc} bước so với thiết kế {SO_BUOC_DAY_DU})"
                : " ✔";
            sb.AppendLine($"   DangChayTutorial : {tm.DangChayTutorial}" +
                          $"      _steps: {tongBuoc}/{SO_BUOC_DAY_DU}{canhBaoBuoc}");

            int idx = tm.ChiSoBuocHienTai;
            var step = tm.LayStepData(idx);
            string waitCuaBuoc = step != null ? step.waitAction.ToString() : "(không có step data)";
            sb.AppendLine($"   Bước hiện tại    : [{idx}] {tm.TenBuocHienTai}   waitAction={waitCuaBuoc}");

            // ── Máy trạng thái + hàng đợi ──
            string hangDoi = "(rỗng)";
            var dsHangDoi = tm.HangDoiActionHienTai;
            if (dsHangDoi != null && dsHangDoi.Length > 0)
                hangDoi = "[" + string.Join(", ", dsHangDoi) + "]";
            sb.AppendLine($"   Trạng thái       : {tm.TenTrangThai}   |  Đang chờ: {tm.TenActionDangCho}" +
                          $"   |  Hàng đợi action: {hangDoi}");

            // ── Cổng popup — nghi phạm số 1 khi tutorial đứng im ──
            bool coPopup = TutorialGate.CoPopupDangMo();
            sb.AppendLine($"   Cổng popup       : CoPopupDangMo={coPopup}" +
                          (coPopup
                            ? $" ({TutorialGate.TenPopupDangMo()})     ← NGHI PHẠM nếu đứng im"
                            : "     ✔ không popup nào chặn"));

            // ── Bàn tay: mong đợi ĐÚNG MỘT cái đang bật ──
            bool tayTinh = tm.CoTayTinhDangBat;
            bool tayKeo  = tm.CoTayKeoDangBat;
            bool tayHD   = tm.CoTayHanhDongDangBat;
            bool aoAnh   = tm.CoAoAnhDangChay;
            sb.AppendLine($"   Tay đang bật     : {tm.TenTayTinh} {Dau(tayTinh)} | " +
                          $"{tm.TenTayKeo} {Dau(tayKeo)} | " +
                          $"{tm.TenTayHanhDong} {Dau(tayHD)} | " +
                          $"Ảo ảnh: {(aoAnh ? "đang chạy" : "tắt")}");
            int soTay = (tayTinh ? 1 : 0) + (tayKeo ? 1 : 0) + (tayHD ? 1 : 0) + (aoAnh ? 1 : 0);
            sb.AppendLine($"   Chủ bàn tay      : {tm.TenChuTayHienTai}   (đang bật {soTay} tay)" +
                          (soTay > 1 ? "  ✘✘ NHIỀU HƠN 1 BÀN TAY!" : "  ✔"));

            // ── Hộp thoại NPC cũ: mong đợi đã bị xoá khỏi scene ──
            sb.AppendLine($"   Hộp thoại NPC cũ : {(tm.CoHopThoaiNpcCu ? "VẪN CÒN trong scene ⚠ (nên xoá hẳn)" : "đã xoá ✔")}");

            // ── Ô đất / chậu hoa ──
            sb.AppendLine("   " + MoTaVungO("Ô lúa   ", TutorialStepTriggerBridge.LayODatLua()));
            sb.AppendLine("   " + MoTaVungO("Chậu hoa", TutorialStepTriggerBridge.LayChauHoa()));

            // ── Kho hạt ──
            string hatLua = MoTaHat("seed_rice");
            string hatHoa = MoTaHat("seed_huong_duong");
            string hatNgo = MoTaHat("seed_ngo");
            sb.AppendLine($"   Kho hạt          : {hatLua}  {hatHoa}  {hatNgo}");

            // ── PlayerPrefs ──
            sb.AppendLine($"   PlayerPrefs      : {TenVaGiaTriPref(TutorialManager.KhoaPrefDaXong)} · " +
                          $"{TenVaGiaTriPref(TutorialManager.KhoaPrefBuoc)}");
        }
        catch (System.Exception e)
        {
            sb.AppendLine($"   ✘ Lỗi khi đọc trạng thái tutorial: {e.GetType().Name}: {e.Message}");
        }

        return sb.ToString();
    }

    private static string Dau(bool bat) => bat ? "✔" : "✘";

    /// <summary>
    /// Đếm ô theo trạng thái + liệt kê tên ô CÒN VIỆC (trống = chờ trồng, chín = chờ thu hoạch).
    /// Danh sách vào đây ĐÃ được LayODatLua()/LayChauHoa() lọc bỏ ô khoá, nên "tổng" = số ô đã mở.
    /// </summary>
    private static string MoTaVungO(string nhan, System.Collections.Generic.List<PlotController> ds)
    {
        if (ds == null || ds.Count == 0)
            return $"{nhan}         : (không tìm thấy ô nào — scene chưa sẵn sàng?)";

        int trong = 0, dangLon = 0, chin = 0;
        var conViec = new System.Collections.Generic.List<string>();

        foreach (var o in ds)
        {
            if (o == null) continue;

            if (o.IsReady)        { chin++;    conViec.Add(TenO(o)); }
            else if (o.IsGrowing) { dangLon++; }
            else                  { trong++;   conViec.Add(TenO(o)); }
        }

        string ten = conViec.Count == 0
            ? "(không ô nào còn việc)"
            : string.Join(", ", conViec.ToArray());

        return $"{nhan}         : tổng {ds.Count} (ô đã mở) · trống {trong} · đang lớn {dangLon} · " +
               $"chín {chin}        (ô còn việc: {ten})";
    }

    private static string TenO(PlotController o)
    {
        return o != null ? $"{o.gameObject.name}#{o.PlotId}" : "(null)";
    }

    /// <summary>"seed_rice=0 ⚠" — cảnh báo khi hết hạt (bước trồng sẽ bế tắc).</summary>
    private static string MoTaHat(string seedId)
    {
        var kho = WarehouseManager.Instance;
        if (kho == null) return $"{seedId}=? (không có WarehouseManager)";
        int n = kho.GetAmount(seedId);
        return $"{seedId}={n}{(n <= 0 ? " ⚠" : "")}";
    }

    private static string TenVaGiaTriPref(string khoa)
    {
        if (string.IsNullOrEmpty(khoa)) return "(khoá rỗng)";
        return PlayerPrefs.HasKey(khoa)
            ? $"{khoa}={PlayerPrefs.GetInt(khoa, 0)}"
            : $"{khoa}=(chưa có)";
    }

    // ── Tiện ích ─────────────────────────────────────────────────────────

    private static void AppendAllCanvases(StringBuilder sb)
    {
        sb.AppendLine("── TẤT CẢ CANVAS TRONG SCENE ──");
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                   FindObjectsSortMode.None);
        int worldSpace = 0;
        foreach (var c in all)
        {
            if (c.renderMode == RenderMode.WorldSpace) { worldSpace++; continue; }
            sb.AppendLine($"   • '{c.name}'  order={c.sortingOrder}  mode={c.renderMode}" +
                          $"  active={c.gameObject.activeInHierarchy}");
        }
        sb.AppendLine($"   (+ {worldSpace} canvas World Space bị lược — bong bóng đơn hàng của nhà)");
        sb.AppendLine();
    }

    private static GameObject FindByName(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                             FindObjectsSortMode.None))
            if (t != null && t.name == name) return t.gameObject;
        return null;
    }

    private static Transform FindChildDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindChildDeep(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    private static string Path_(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + " / " + p; }
        return p;
    }

    private static string Fmt(Vector3 v)  => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    private static string Fmt2(Vector3 v) => $"({v.x:F0}, {v.y:F0})";
}
