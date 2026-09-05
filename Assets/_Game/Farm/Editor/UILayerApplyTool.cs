#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// SẮP XẾP LẠI LỚP UI — áp bảng <see cref="UILayers"/> vào các Canvas trong scene đang mở.
///
/// Có hai menu:
///   - DRY RUN : chỉ in "tu X -> Y" cho từng Canvas, KHÔNG ghi gì cả. Luôn chạy cái này trước.
///   - APPLY   : ghi thật, có Undo (Ctrl+Z hoàn tác được) và đánh dấu scene bẩn.
///
/// QUY ĐỊNH CỨNG CỦA DỰ ÁN (đã từng có 7 tool tự chạy làm hỏng scene):
///   - KHÔNG dùng thuộc tính tự chạy lúc Unity nạp assembly, KHÔNG hoãn gọi qua
///     EditorApplication, KHÔNG tự lưu scene.
///   - Tool chỉ chạy khi người dùng tự bấm menu; lưu scene là việc của người dùng (Ctrl+S).
///
/// ─────────────────────────────────────────────────────────────────────────────
/// VÌ SAO PHẢI SẮP LẠI — hiện trạng SCN_Farm.unity trước khi chạy tool:
///
///   order 100 : World (canvas ĐANG TẮT, World Space)   ]
///   order 100 : Canvas_HUD                             ] TRÙNG
///   order 120 : popup_Menu                             ]
///   order 120 : WarehousePopup                         ] TRÙNG
///   order 125 : Canvas_MarketPopup
///   order 300 : Canvas_Popup (chứa 13 popup không có canvas riêng)  ]
///   order 300 : Popup_LevelUp_Township                              ] TRÙNG
///   order 400 : Canvas_TouristBoatPopup                ]
///   order 400 : MillPopup_Root                         ] TRÙNG
///   order 999 : Tutorial_Canvas   <-- ĐÈ LÊN MỌI POPUP, đây là lỗi nặng nhất
///
/// 4 nhóm trùng order (100 · 120 · 300 · 400) khiến thứ tự vẽ không xác định.
/// Tutorial_Canvas ở 999 khiến lớp phủ hướng dẫn che mọi popup hệ thống.
/// </summary>
public static class UILayerApplyTool
{
    private const string MenuDryRun = "Tools/Farm/UI/Sap xep lai lop UI - DRY RUN";
    private const string MenuApply  = "Tools/Farm/UI/Sap xep lai lop UI - APPLY";

    /// <summary>Một dòng ánh xạ: tên Canvas trong scene → order mới + lý do.</summary>
    private struct AnhXa
    {
        public string TenCanvas;
        public int    OrderMoi;
        public string Lop;
        public string LyDo;

        public AnhXa(string ten, int order, string lop, string lyDo)
        {
            TenCanvas = ten; OrderMoi = order; Lop = lop; LyDo = lyDo;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BẢNG ÁNH XẠ — khai báo TƯỜNG MINH, không đoán.
    // Canvas nào KHÔNG có tên trong bảng này thì tool BỎ QUA và báo lại,
    // tuyệt đối không tự suy diễn ra một con số nào.
    //
    // Nguyên tắc giãn số: cùng một lớp thì cộng thêm bội số của UILayers.BuocTrongLop
    // (10), giữ nguyên THỨ TỰ TƯƠNG ĐỐI cũ giữa các Canvas để không đổi cảm giác chơi.
    // ─────────────────────────────────────────────────────────────────────────
    private static readonly AnhXa[] Bang =
    {
        // ── Lớp World (0) ────────────────────────────────────────────────────
        new AnhXa("World", UILayers.World, "World",
            "Canvas World Space gan vao canh vat. Dang TAT nhung van chiem order 100 " +
            "trung voi Canvas_HUD, nen keo han ve dai World de tra lai muc 100 cho HUD."),

        // ── Lớp HUD (100) ────────────────────────────────────────────────────
        new AnhXa("Canvas_HUD", UILayers.HUD, "HUD",
            "HUD thuong truc (tien, kim cuong, EXP, thanh tab day). Giu nguyen 100, " +
            "nay khong con bi Canvas 'World' dung chung nua."),

        // ── Lớp Panel (200 · 210 · 220) — go nhom trung 120/120/125 ──────────
        new AnhXa("popup_Menu", UILayers.Panel, "Panel",
            "Canvas long trong Canvas_Popup, co overrideSorting. Cu o 120, trung dung " +
            "voi WarehousePopup. Dua ve moc Panel = 200."),

        new AnhXa("WarehousePopup", UILayers.Panel + UILayers.BuocTrongLop, "Panel +10",
            "Cung lop Panel voi popup_Menu, truoc day trung y het 120. Gian ra 210. " +
            "Hai canvas nay truoc gio trung nhau nen khong co thu tu tuong doi cu de giu; " +
            "chon Warehouse tren popup_Menu vi kho thuong mo de tren menu."),

        new AnhXa("Canvas_MarketPopup", UILayers.Panel + 2 * UILayers.BuocTrongLop, "Panel +20",
            "Bang tin cho, truoc o 125 tuc CAO NHAT trong nhom Panel cu (120/120/125). " +
            "Giu nguyen vi tri cao nhat cua nhom bang 220."),

        // ── Lớp Tutorial (250) — ĐIỂM MẤU CHỐT CỦA TOÀN BỘ ĐỢT DỌN NÀY ───────
        new AnhXa("Tutorial_Canvas", UILayers.Tutorial, "Tutorial",
            "TU 999 XUONG 250. Day la thay doi quan trong nhat: 999 khien lop phu huong dan " +
            "de len MOI popup he thong, che nut bam va lam nguoi choi ket. 250 nam TREN HUD (100) " +
            "va TREN Panel (200-220) de highlight/mui ten van hien ro, nhung DUOI Canvas_Popup (300) " +
            "de popup he thong luon cat ngang duoc tutorial."),

        // ── Lớp Popup (300 · 310) — go nhom trung 300/300 ────────────────────
        new AnhXa("Canvas_Popup", UILayers.Popup, "Popup",
            "Canvas chua 13 popup khong co canvas rieng. Giu moc chuan 300."),

        new AnhXa("Popup_LevelUp_Township", UILayers.Popup + UILayers.BuocTrongLop, "Popup +10",
            "Canvas long trong Canvas_Popup, co overrideSorting, truoc day trung dung 300 voi cha. " +
            "Nang len 310 de popup len cap luon noi tren cac popup thuong cua Canvas_Popup."),

        // ── Lớp PopupCaoCap (400 · 410) — go nhom trung 400/400 ──────────────
        new AnhXa("Canvas_TouristBoatPopup", UILayers.PopupCaoCap, "PopupCaoCap",
            "Popup uu tien cao, canvas goc rieng. Giu moc chuan 400."),

        new AnhXa("MillPopup_Root", UILayers.PopupCaoCap + UILayers.BuocTrongLop, "PopupCaoCap +10",
            "Canvas long trong Canvas_Popup, co overrideSorting, truoc day trung dung 400 " +
            "voi Canvas_TouristBoatPopup. Gian ra 410."),
    };

    // ─────────────────────────────────────────────────────────────────────────
    // MENU
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(MenuDryRun, false, 20)]
    public static void DryRun() => Chay(false);

    [MenuItem(MenuApply, false, 21)]
    public static void Apply()
    {
        bool dongY = EditorUtility.DisplayDialog(
            "Sap xep lai lop UI - APPLY",
            "Tool se GHI sortingOrder moi vao cac Canvas trong scene dang mo.\n\n"
            + "• Co Undo: bam Ctrl+Z de hoan tac.\n"
            + "• Tool KHONG tu luu scene — ban tu bam Ctrl+S neu ung y.\n\n"
            + "Da chay DRY RUN va doc ky bang thay doi chua?",
            "Ghi that", "Huy");

        if (!dongY)
        {
            Debug.Log("[UILayerApply] Nguoi dung huy. Khong ghi gi ca.");
            return;
        }

        Chay(true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // THÂN CHÍNH
    // ─────────────────────────────────────────────────────────────────────────

    private static void Chay(bool ghiThat)
    {
        // Bước 0: tự kiểm tra bảng ánh xạ trước khi động vào scene.
        if (!KiemTraBangAnhXa(out string loiBang))
        {
            Debug.LogError("[UILayerApply] BANG ANH XA BI SAI, dung lai, khong dong vao scene:\n" + loiBang);
            return;
        }

        var tatCa = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                          .Where(c => c != null)
                          .ToList();

        if (tatCa.Count == 0)
        {
            Debug.LogWarning("[UILayerApply] Khong tim thay Canvas nao trong scene dang mo.");
            return;
        }

        // Gom mọi thay đổi của lần chạy này vào MỘT nhóm Undo duy nhất,
        // để một lần Ctrl+Z hoàn tác được toàn bộ bảng.
        if (ghiThat)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Sap xep lai lop UI");
        }

        var sb = new StringBuilder();
        sb.AppendLine(ghiThat
            ? "===== SAP XEP LAI LOP UI — APPLY (ghi that, co Undo) ====="
            : "===== SAP XEP LAI LOP UI — DRY RUN (khong ghi gi) =====");
        sb.AppendLine($"Tim thay {tatCa.Count} Canvas trong scene dang mo.");
        sb.AppendLine();
        sb.AppendLine(string.Format("{0,-28} {1,-10} {2,-16} {3}", "TEN CANVAS", "TU -> DEN", "LOP MOI", "TRANG THAI"));
        sb.AppendLine(new string('-', 100));

        int soDoi = 0, soGiuNguyen = 0;
        var boQua = new List<Canvas>();
        var scenesBan = new HashSet<UnityEngine.SceneManagement.Scene>();

        // Duyệt theo THỨ TỰ BẢNG ÁNH XẠ để log đọc từ dưới lên trên cho dễ hình dung.
        foreach (var ax in Bang)
        {
            var khop = tatCa.Where(c => c.name == ax.TenCanvas).ToList();

            if (khop.Count == 0)
            {
                sb.AppendLine(string.Format("{0,-28} {1,-10} {2,-16} {3}",
                    Cat(ax.TenCanvas, 28), "-", ax.Lop, "KHONG TIM THAY trong scene — bo qua"));
                continue;
            }

            if (khop.Count > 1)
            {
                sb.AppendLine(string.Format("{0,-28} {1,-10} {2,-16} {3}",
                    Cat(ax.TenCanvas, 28), "-", ax.Lop,
                    $"CO {khop.Count} CANVAS TRUNG TEN — bo qua het, khong doan"));
                continue;
            }

            var canvas = khop[0];
            int cu = canvas.sortingOrder;

            if (cu == ax.OrderMoi)
            {
                soGiuNguyen++;
                sb.AppendLine(string.Format("{0,-28} {1,-10} {2,-16} {3}",
                    Cat(canvas.name, 28), $"{cu} -> {ax.OrderMoi}", ax.Lop, "da dung, giu nguyen"));
                continue;
            }

            soDoi++;
            string trangThai = ghiThat ? "DA GHI" : "se doi (dry-run)";

            sb.AppendLine(string.Format("{0,-28} {1,-10} {2,-16} {3}",
                Cat(canvas.name, 28), $"{cu} -> {ax.OrderMoi}", ax.Lop, trangThai));
            sb.AppendLine($"        ly do: {ax.LyDo}");

            if (ghiThat)
            {
                // Undo.RecordObject phải gọi TRƯỚC khi thay đổi giá trị.
                Undo.RecordObject(canvas, "Sap xep lai lop UI");

                // Ghi qua SerializedObject để Unity đánh dấu prefab-override/dirty đúng chuẩn.
                var so = new SerializedObject(canvas);
                var prop = so.FindProperty("m_SortingOrder");
                if (prop == null)
                {
                    sb.AppendLine($"        LOI: khong tim thay property m_SortingOrder tren '{canvas.name}'.");
                    continue;
                }

                prop.intValue = ax.OrderMoi;

                // Dùng bản ...WithoutUndo vì Undo.RecordObject ở trên đã ghi undo rồi.
                // Nếu gọi ApplyModifiedProperties() thường, Unity tạo THÊM một mốc undo nữa,
                // người dùng sẽ phải bấm Ctrl+Z hai lần cho mỗi Canvas.
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(canvas);
                scenesBan.Add(canvas.gameObject.scene);
            }
        }

        // ── Canvas có trong scene nhưng KHÔNG có trong bảng ánh xạ ───────────
        var tenTrongBang = new HashSet<string>(Bang.Select(a => a.TenCanvas));
        foreach (var c in tatCa)
            if (!tenTrongBang.Contains(c.name)) boQua.Add(c);

        sb.AppendLine();
        sb.AppendLine("----- CANVAS KHONG CO TRONG BANG ANH XA -----");
        if (boQua.Count == 0)
        {
            sb.AppendLine("  (khong co) — moi Canvas trong scene deu duoc khai bao tuong minh.");
        }
        else
        {
            foreach (var c in boQua)
                sb.AppendLine($"  {c.name,-28} order hien tai = {c.sortingOrder}  -> BO QUA, khong doan.");

            sb.AppendLine("     => Muon xu ly chung thi them dong vao mang 'Bang' trong UILayerApplyTool.cs.");
        }

        // ── Bảng kết quả cuối, sắp theo order mới ────────────────────────────
        sb.AppendLine();
        sb.AppendLine(ghiThat ? "----- BANG LOP SAU KHI GHI -----" : "----- BANG LOP DU KIEN SAU KHI GHI -----");
        foreach (var ax in Bang.OrderBy(a => a.OrderMoi))
            sb.AppendLine($"  {ax.OrderMoi,6}  {ax.TenCanvas,-28} {ax.Lop}");

        // ── Xác nhận điều kiện then chốt: Tutorial phải DƯỚI Canvas_Popup ────
        int orderTutorial = LayOrderTrongBang("Tutorial_Canvas");
        int orderPopup    = LayOrderTrongBang("Canvas_Popup");
        sb.AppendLine();
        sb.AppendLine(orderTutorial >= 0 && orderPopup >= 0 && orderTutorial < orderPopup
            ? $"  KIEM TRA OK: Tutorial_Canvas ({orderTutorial}) < Canvas_Popup ({orderPopup}) — tutorial nam DUOI popup, dung yeu cau."
            : "  CANH BAO: khong xac nhan duoc dieu kien Tutorial_Canvas < Canvas_Popup, kiem tra lai bang anh xa!");

        sb.AppendLine();
        sb.AppendLine($"Tong ket: {soDoi} Canvas doi order, {soGiuNguyen} giu nguyen, {boQua.Count} bo qua.");

        if (ghiThat)
        {
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            foreach (var sc in scenesBan)
                if (sc.IsValid()) EditorSceneManager.MarkSceneDirty(sc);

            sb.AppendLine();
            sb.AppendLine(">>> DA GHI XONG. TOOL KHONG TU LUU SCENE.");
            sb.AppendLine(">>> Hay tu bam Ctrl+S de luu scene neu ban ung y ket qua.");
            sb.AppendLine(">>> Neu khong ung y: bam Ctrl+Z de hoan tac, hoac dong scene ma khong luu.");
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine(">>> DAY LA DRY RUN — CHUA GHI GI CA.");
            sb.AppendLine(">>> Ung y thi chay: " + MenuApply);
        }

        sb.AppendLine("=====================================================");
        Debug.Log(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TIỆN ÍCH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Bảng ánh xạ phải không trùng tên và không trùng order — kiểm trước khi chạy.</summary>
    private static bool KiemTraBangAnhXa(out string loi)
    {
        var sb = new StringBuilder();

        foreach (var g in Bang.GroupBy(a => a.TenCanvas).Where(g => g.Count() > 1))
            sb.AppendLine($"  - Ten Canvas '{g.Key}' xuat hien {g.Count()} lan trong bang.");

        foreach (var g in Bang.GroupBy(a => a.OrderMoi).Where(g => g.Count() > 1))
            sb.AppendLine($"  - Order {g.Key} bi gan cho {g.Count()} Canvas: {string.Join(", ", g.Select(a => a.TenCanvas))}.");

        loi = sb.ToString();
        return loi.Length == 0;
    }

    private static int LayOrderTrongBang(string ten)
    {
        foreach (var a in Bang)
            if (a.TenCanvas == ten) return a.OrderMoi;

        return -1;
    }

    private static string Cat(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max - 1) + "~";
    }
}
#endif
