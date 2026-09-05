#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// KIỂM TRA LỚP UI — CÔNG CỤ CHỈ ĐỌC.
///
/// Quét mọi Canvas trong (các) scene đang mở, kể cả object đang tắt, rồi in ra
/// Console một bảng dễ đọc + phần cảnh báo. Tool này TUYỆT ĐỐI KHÔNG sửa gì:
/// không ghi vào Canvas, không đánh dấu scene bẩn, không lưu scene.
///
/// Muốn SỬA thì dùng UILayerApplyTool (Tools/Farm/UI/Sap xep lai lop UI ...).
///
/// QUY ĐỊNH CỨNG CỦA DỰ ÁN (đã từng có 7 tool tự chạy làm hỏng scene):
///   - KHÔNG dùng thuộc tính tự chạy lúc Unity nạp assembly, KHÔNG hoãn gọi qua
///     EditorApplication, KHÔNG tự lưu scene. Tool chỉ chạy khi người dùng tự bấm menu.
/// </summary>
public static class UILayerAuditTool
{
    private const string Menu = "Tools/Farm/UI/Kiem tra lop UI (chi bao cao)";

    /// <summary>Một dòng trong bảng báo cáo.</summary>
    private struct DongCanvas
    {
        public Canvas Canvas;
        public string Ten;
        public string Scene;
        public int Order;
        public string RenderMode;
        public bool OverrideSorting;
        public bool CanvasEnabled;      // component Canvas có đang bật không
        public bool GameObjectActive;   // GameObject có đang active trong hierarchy không
        public bool LaCanvasGoc;        // là root canvas hay canvas lồng
        public bool OrderCoHieuLuc;     // sortingOrder có thật sự được dùng không
    }

    [MenuItem(Menu, false, 1)]
    public static void KiemTra()
    {
        var tatCa = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (tatCa == null || tatCa.Length == 0)
        {
            Debug.LogWarning("[UILayerAudit] Không tìm thấy Canvas nào trong scene đang mở.");
            return;
        }

        var ds = new List<DongCanvas>(tatCa.Length);
        foreach (var c in tatCa)
        {
            if (c == null) continue;

            bool laGoc = c.isRootCanvas;

            ds.Add(new DongCanvas
            {
                Canvas           = c,
                Ten              = c.name,
                Scene            = c.gameObject.scene.name,
                Order            = c.sortingOrder,
                RenderMode       = c.renderMode.ToString(),
                OverrideSorting  = c.overrideSorting,
                CanvasEnabled    = c.enabled,
                GameObjectActive = c.gameObject.activeInHierarchy,
                LaCanvasGoc      = laGoc,
                // Canvas lồng mà KHÔNG bật overrideSorting thì sortingOrder bị bỏ qua,
                // nó vẽ theo thứ tự của canvas cha.
                OrderCoHieuLuc   = laGoc || c.overrideSorting,
            });
        }

        ds = ds.OrderBy(d => d.Order).ThenBy(d => d.Ten).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("===== KIEM TRA LOP UI (chi doc, khong sua gi) =====");
        sb.AppendLine($"Tong so Canvas tim thay: {ds.Count}");
        sb.AppendLine();
        sb.AppendLine(string.Format("{0,6}  {1,-28} {2,-18} {3,-8} {4,-8} {5,-9} {6,-8} {7}",
            "ORDER", "TEN CANVAS", "RENDER MODE", "OVERRID", "CANVAS", "OBJECT", "LOAI", "LOP THEO UILayers"));
        sb.AppendLine(new string('-', 118));

        foreach (var d in ds)
        {
            sb.AppendLine(string.Format("{0,6}  {1,-28} {2,-18} {3,-8} {4,-8} {5,-9} {6,-8} {7}",
                d.Order,
                Cat(d.Ten, 28),
                d.RenderMode,
                d.OverrideSorting ? "co" : "-",
                d.CanvasEnabled ? "bat" : "TAT",
                d.GameObjectActive ? "active" : "INACTIVE",
                d.LaCanvasGoc ? "goc" : "long",
                UILayers.MoTa(d.Order) + (d.OrderCoHieuLuc ? "" : "   (order BI BO QUA)")));
        }

        sb.AppendLine();
        sb.AppendLine("Ghi chu cot: OVERRID = overrideSorting | CANVAS = component Canvas bat/tat");
        sb.AppendLine("             OBJECT  = GameObject active trong hierarchy | LOAI = canvas goc hay canvas long");

        // ── Cảnh báo 1: trùng sortingOrder ───────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("----- CANH BAO 1: CAC CANVAS TRUNG sortingOrder -----");

        var nhomTrung = ds.GroupBy(d => d.Order).Where(g => g.Count() > 1).OrderBy(g => g.Key).ToList();
        if (nhomTrung.Count == 0)
        {
            sb.AppendLine("  (khong co) — moi Canvas deu co mot muc order rieng.");
        }
        else
        {
            foreach (var g in nhomTrung)
            {
                sb.AppendLine($"  order {g.Key}: {g.Count()} Canvas -> {string.Join(", ", g.Select(x => x.Ten))}");
                sb.AppendLine("     => Thu tu ve khi trung order la KHONG XAC DINH (phu thuoc thu tu hierarchy/Unity),");
                sb.AppendLine("        chay may nay dung, may khac sai. Phai gian gia tri ra.");
            }
        }

        // ── Cảnh báo 2: order không khớp bảng UILayers ───────────────────────
        sb.AppendLine();
        sb.AppendLine("----- CANH BAO 2: ORDER KHONG KHOP BANG UILayers -----");

        var lech = ds.Where(d => !KhopBangChuan(d.Order)).ToList();
        if (lech.Count == 0)
        {
            sb.AppendLine("  (khong co) — moi Canvas deu nam dung moc lop hoac moc + boi so cua BuocTrongLop.");
        }
        else
        {
            foreach (var d in lech)
                sb.AppendLine($"  {d.Ten,-28} order={d.Order,-6} -> gan nhat: {UILayers.MoTa(d.Order)}");

            sb.AppendLine("     => Order hop le phai la mot moc lop (0/100/200/250/300/400/9999)");
            sb.AppendLine($"        hoac moc + boi so cua {UILayers.BuocTrongLop} van con trong dai cua lop do.");
        }

        // ── Cảnh báo 3: Canvas tắt nhưng vẫn chiếm một mức order ─────────────
        sb.AppendLine();
        sb.AppendLine("----- CANH BAO 3: CANVAS DANG TAT NHUNG VAN CHIEM MOT MUC ORDER -----");

        var canvasTat = ds.Where(d => !d.CanvasEnabled || !d.GameObjectActive).ToList();
        if (canvasTat.Count == 0)
        {
            sb.AppendLine("  (khong co).");
        }
        else
        {
            foreach (var d in canvasTat)
            {
                bool dungChungOrder = ds.Any(x => x.Order == d.Order && x.Canvas != d.Canvas);
                sb.AppendLine($"  {d.Ten,-28} order={d.Order,-6} "
                    + $"canvas={(d.CanvasEnabled ? "bat" : "TAT")} object={(d.GameObjectActive ? "active" : "INACTIVE")}"
                    + (dungChungOrder ? "   <-- lai con DUNG CHUNG order voi Canvas khac" : ""));
            }

            sb.AppendLine("     => Canvas dang tat khong ve gi ca, nhung van giu cho mot con so trong bang lop.");
            sb.AppendLine("        Ai doc scene sau nay se tuong muc order do da bi chiem. Nen dua ve dung lop cua no");
            sb.AppendLine("        (hoac xoa han neu khong con dung), thay vi de lo lung.");
        }

        // ── Cảnh báo 4 (bổ sung): canvas lồng không bật overrideSorting ──────
        var longKhongOverride = ds.Where(d => !d.LaCanvasGoc && !d.OverrideSorting).ToList();
        if (longKhongOverride.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("----- CANH BAO 4: CANVAS LONG KHONG BAT overrideSorting -----");
            foreach (var d in longKhongOverride)
                sb.AppendLine($"  {d.Ten,-28} order={d.Order,-6} -> con so nay VO NGHIA, canvas ve theo canvas cha.");
        }

        sb.AppendLine();
        sb.AppendLine("===== HET BAO CAO — TOOL NAY KHONG SUA GI =====");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Order được coi là "khớp bảng" khi nó là một mốc lớp chuẩn, hoặc là mốc lớp
    /// cộng thêm bội số của <see cref="UILayers.BuocTrongLop"/> mà vẫn còn nằm trong
    /// dải của chính lớp đó (phần lệch nhỏ hơn 100).
    /// </summary>
    private static bool KhopBangChuan(int order)
    {
        if (UILayers.LaMocChuan(order)) return true;

        string mo = UILayers.MoTa(order);
        int viTri = mo.IndexOf(" +");
        if (viTri < 0) return false;

        if (!int.TryParse(mo.Substring(viTri + 2), out int lech)) return false;

        return lech > 0 && lech < 100 && lech % UILayers.BuocTrongLop == 0;
    }

    /// <summary>Cắt bớt chuỗi cho vừa cột bảng.</summary>
    private static string Cat(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max - 1) + "~";
    }
}
#endif
