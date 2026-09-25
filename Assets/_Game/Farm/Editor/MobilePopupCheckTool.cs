// ============================================================================
//  Tools > Farm Game > Mobile  (2026-09-24)
//   1. Kiem tra popup tren dien thoai (chi doc)   -> bang ket qua trong Console + docs/KIEM_TRA_MOBILE.md
//   2. Gan tu co cho popup bi tran                 -> gan PopupTuCo cho bang TRAN ma chua co tu co (Undo, Ctrl+S)
//   3. Go het PopupTuCo khoi scene                 -> hoan tac muc 2
//  Cach tinh: moi Canvas goc co CanvasScaler -> quy doi ra kich thuoc canvas tren tung may
//  (16:9 PC, Android 20:9, iPhone tai tho, iPhone SE, iPad 4:3, tablet 16:10), tru vung an toan
//  (tai tho + le 12dp nhu SafeAreaFitter) roi so voi DAU CHAN THAT cua tung bang popup
//  (ke ca ruy-bang tieu de / nut X tho ra ngoai; khong tinh noi dung ScrollRect bi xen).
//  Popup sinh tu prefab luc chay (vd popup tau): bam Play, MO popup do roi chay muc 1.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MobilePopupCheckTool
{
    private const string Goc = "Tools/Farm Game/Mobile/";

    private struct May
    {
        public string ten; public float w, h, trai, phai, tren, duoi, dpi;
        public May(string t, float w, float h, float tr, float ph, float tn, float du, float dpi)
        { ten = t; this.w = w; this.h = h; trai = tr; phai = ph; tren = tn; duoi = du; this.dpi = dpi; }
    }

    // man ngang (landscape), don vi pixel; tai tho / thanh cu chi theo safeArea that cua may
    private static readonly May[] DanhSachMay =
    {
        new May("PC 16:9 (1920x1080)",          1920, 1080,   0,   0, 0,  0,  96),
        new May("Android 20:9 (2400x1080)",     2400, 1080,  90,   0, 0,  0, 400),
        new May("iPhone tai tho (2532x1170)",   2532, 1170, 141, 141, 0, 63, 460),
        new May("iPhone SE 16:9 (1334x750)",    1334,  750,   0,   0, 0,  0, 326),
        new May("iPad 4:3 (2048x1536)",         2048, 1536,   0,   0, 0, 40, 264),
        new May("Tablet 16:10 (2560x1600)",     2560, 1600,   0,   0, 0,  0, 320),
    };

    private static readonly string[] TuKhoaPopup = { "popup", "board", "window", "dialog" };
    private static readonly HashSet<string> CoTuCo = new HashSet<string>
    { "MarketBoardUI", "MillPopupUI", "StallPopupUI", "OrderBoardPopupUI", "PopupTuCo", "ShopManager" };

    private class KetQua
    {
        public RectTransform bang; public string duong; public bool coTuCo, ngoaiManHinh; public float tranMax;
        public List<string> loi = new List<string>();
    }

    // =====================================================================
    [MenuItem(Goc + "1. Kiem tra popup tren dien thoai (chi doc)", false, 10)]
    private static void KiemTra()
    {
        var sb = new StringBuilder();
        var ds = Quet(sb);
        int tran = 0, tranChuaCo = 0;
        foreach (var k in ds) if (k.loi.Count > 0 && !k.ngoaiManHinh) { tran++; if (!k.coTuCo) tranChuaCo++; }

        var bao = new StringBuilder();
        bao.AppendLine("# Kiem tra popup tren dien thoai (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")");
        bao.AppendLine(Application.isPlaying ? "Che do: PLAY (tinh ca popup sinh luc chay dang mo)." : "Che do: EDIT (popup sinh tu prefab luc chay chua co trong danh sach).");
        bao.AppendLine();
        bao.Append(sb);
        bao.AppendLine();
        bao.AppendLine($"Tong: {ds.Count} bang popup, {tran} bang TRAN tren it nhat 1 may, trong do {tranChuaCo} bang CHUA co tu co.");
        bao.AppendLine();
        bao.AppendLine("| Popup | Tu co | Tran lon nhat (dv canvas) | May bi tran |");
        bao.AppendLine("|---|---|---|---|");
        ds.Sort((a, b) => b.tranMax.CompareTo(a.tranMax));
        foreach (var k in ds)
        {
            if (k.ngoaiManHinh) continue;
            bao.AppendLine($"| {k.duong} | {(k.coTuCo ? "co" : "KHONG")} | {(k.loi.Count > 0 ? k.tranMax.ToString("0") : "-")} | {(k.loi.Count > 0 ? string.Join("; ", k.loi) : "vua het")} |");
        }
        bao.AppendLine();
        bao.AppendLine("Nam NGOAI man hinh ngay ca tren PC (panel truot vao / dang an) -> khong danh gia, bam Play MO popup do roi chay lai muc 1:");
        foreach (var k in ds) if (k.ngoaiManHinh) bao.AppendLine("- " + k.duong);
        bao.AppendLine();
        bao.AppendLine("Bang 'Tu co = co' se tu dich / co lai luc mo -> khong can lam gi. Bang KHONG + tran: chay muc 2 (gan PopupTuCo) roi Ctrl+S.");

        string goc = Path.GetDirectoryName(Application.dataPath);
        string thuMuc = Path.Combine(goc, "docs");
        Directory.CreateDirectory(thuMuc);
        string file = Path.Combine(thuMuc, "KIEM_TRA_MOBILE.md");
        File.WriteAllText(file, bao.ToString(), new UTF8Encoding(false));
        Debug.Log("[Mobile] " + bao);
        EditorUtility.DisplayDialog("Kiem tra mobile",
            $"{ds.Count} bang popup.\n{tran} bang tran tren it nhat 1 may ({tranChuaCo} bang chua co tu co).\n\nChi tiet: docs/KIEM_TRA_MOBILE.md va Console.", "OK");
    }

    // =====================================================================
    [MenuItem(Goc + "2. Gan tu co cho popup bi tran", false, 11)]
    private static void GanTuCo()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Mobile", "Thoat Play mode truoc (gan trong Play se mat khi dung).", "OK"); return; }
        var ds = Quet(new StringBuilder());
        var can = new List<KetQua>();
        foreach (var k in ds) if (k.loi.Count > 0 && !k.coTuCo && !k.ngoaiManHinh) can.Add(k);
        if (can.Count == 0) { EditorUtility.DisplayDialog("Mobile", "Khong co bang nao tran ma chua co tu co.", "OK"); return; }
        var sb = new StringBuilder("Se gan PopupTuCo (chi dich / co khi man khong du cho, PC 16:9 giu nguyen) cho:\n\n");
        foreach (var k in can) sb.AppendLine("- " + k.duong);
        if (!EditorUtility.DisplayDialog("Mobile", sb.ToString(), "Gan", "Huy")) return;
        foreach (var k in can)
        {
            var c = Undo.AddComponent<PopupTuCo>(k.bang.gameObject);
            c.LuuGoc();
            EditorUtility.SetDirty(c);
            EditorSceneManager.MarkSceneDirty(k.bang.gameObject.scene);
        }
        Debug.Log($"[Mobile] Da gan PopupTuCo cho {can.Count} bang. Ctrl+S de luu. Hoan tac: Ctrl+Z hoac muc 3.");
    }

    [MenuItem(Goc + "3. Go het PopupTuCo khoi scene", false, 30)]
    private static void GoTuCo()
    {
        int n = 0;
        foreach (var c in Object.FindObjectsByType<PopupTuCo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
            Undo.DestroyObjectImmediate(c);
            n++;
        }
        Debug.Log($"[Mobile] Da go {n} PopupTuCo. Ctrl+S de luu.");
    }

    // =====================================================================
    //  Quet
    // =====================================================================
    private static List<KetQua> Quet(StringBuilder sb)
    {
        var kq = new List<KetQua>();
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cv == null || !cv.isRootCanvas || cv.renderMode == RenderMode.WorldSpace) continue;
            if (EditorUtility.IsPersistent(cv)) continue;
            var rtCv = cv.transform as RectTransform;
            var sc = cv.GetComponent<CanvasScaler>();
            if (cv.GetComponent("UIPopBounce") != null)
                sb.AppendLine($"- CANH BAO: Canvas goc '{cv.name}' co UIPopBounce (da chan trong code, nen go: Tools > VFX > 4).");
            if (sc == null || sc.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                if (sc != null) sb.AppendLine($"- Canvas '{cv.name}': CanvasScaler khong phai Scale With Screen Size -> co chu / nut se khac nhau theo may.");
                continue;
            }
            var bangs = new List<RectTransform>();
            for (int i = 0; i < rtCv.childCount; i++) TimBang(rtCv.GetChild(i) as RectTransform, cv.name.ToLower().Contains("popup"), 0, bangs);

            foreach (var b in bangs)
            {
                // Khung tham chieu = cha an toan (~SafeArea, co khi dang Play) hoac canvas.
                // Luc chay popup nam TRONG lop ~SafeArea -> neo tinh theo vung an toan cua tung may.
                var saf = b.GetComponentInParent<SafeAreaFitter>(true);
                RectTransform F = saf != null ? (RectTransform)saf.transform : rtCv;
                Vector2 C = F.rect.size;
                if (C.x < 1f || C.y < 1f) continue;
                Vector2 mn, mx;
                if (!DauChan(b, F, out mn, out mx)) continue;
                mn -= F.rect.min; mx -= F.rect.min;                           // toa do tinh tu goc duoi-trai khung
                if (LaPopupCu(b)) { sb.AppendLine("- Bo qua popup cu khong dung: " + cv.name + "/" + DuongDan(b, rtCv)); continue; }
                var k = new KetQua { bang = b, duong = cv.name + "/" + DuongDan(b, rtCv), coTuCo = CoScriptTuCo(b, rtCv) };

                // Ngoai khung ngay trong layout hien tai (> 300 dv) -> panel truot / dang an, khong danh gia.
                if (mn.x < -300f || mn.y < -300f || mx.x > C.x + 300f || mx.y > C.y + 300f)
                {
                    k.ngoaiManHinh = true;
                    kq.Add(k);
                    continue;
                }

                foreach (var m in DanhSachMay)
                {
                    Vector2 W = KichThuocCanvas(sc, m.w, m.h);
                    float s = m.w / W.x;                                         // px / don vi canvas
                    float le = 12f * Mathf.Clamp(m.dpi / 160f, 1f, 3f);          // giong SafeAreaFitter
                    float l = Mathf.Max(m.trai, le) / s, r = Mathf.Max(m.phai, le) / s;
                    float t = Mathf.Max(m.tren, le) / s, d = Mathf.Max(m.duoi, le) / s;
                    Vector2 S = new Vector2(W.x - l - r, W.y - t - d);          // vung an toan
                    const float LE = 16f;

                    // Moi mep bam theo neo cua no (anchorMin cho mep trai/duoi, anchorMax cho mep phai/tren)
                    float xMin = l + b.anchorMin.x * S.x + (mn.x - b.anchorMin.x * C.x);
                    float xMax = l + b.anchorMax.x * S.x + (mx.x - b.anchorMax.x * C.x);
                    float yMin = d + b.anchorMin.y * S.y + (mn.y - b.anchorMin.y * C.y);
                    float yMax = d + b.anchorMax.y * S.y + (mx.y - b.anchorMax.y * C.y);

                    float tT = Mathf.Max(0f, (l + LE) - xMin), tP = Mathf.Max(0f, xMax - (W.x - r - LE));
                    float tD = Mathf.Max(0f, (d + LE) - yMin), tTr = Mathf.Max(0f, yMax - (W.y - t - LE));
                    float tMax = Mathf.Max(Mathf.Max(tT, tP), Mathf.Max(tD, tTr));
                    if (tMax > 2f)
                    {
                        var cho = new List<string>();
                        if (tTr > 2f) cho.Add("tren " + tTr.ToString("0"));
                        if (tD > 2f) cho.Add("duoi " + tD.ToString("0"));
                        if (tT > 2f) cho.Add("trai " + tT.ToString("0"));
                        if (tP > 2f) cho.Add("phai " + tP.ToString("0"));
                        k.loi.Add(m.ten + ": " + string.Join(", ", cho));
                        if (tMax > k.tranMax) k.tranMax = tMax;
                    }
                }
                kq.Add(k);
            }
        }
        return kq;
    }

    private static readonly string[] TuKhoaBoQua = { "dim", "overlay", "blocker", "shadow" };

    /// <summary>Bang popup = node dau tien tren nhanh (trong vung popup) KHONG phu kin khung, du lon, co Graphic.
    /// Bo qua nen mo (Dim/Overlay, du co fix cung 3840x2160), hieu ung (FX_, Glow, QuangSang...).</summary>
    private static void TimBang(RectTransform n, bool trongPopup, int sau, List<RectTransform> ra)
    {
        if (n == null || sau > 5) return;
        string ten = n.name.ToLower();
        if (ten == "~safearea") { for (int i = 0; i < n.childCount; i++) TimBang(n.GetChild(i) as RectTransform, trongPopup, sau, ra); return; }
        bool popup = trongPopup;
        foreach (var t in TuKhoaPopup) if (ten.Contains(t)) { popup = true; break; }
        bool boQua = false;
        foreach (var t in TuKhoaBoQua) if (ten.Contains(t)) { boQua = true; break; }
        bool phuKin = n.anchorMin.x <= 0.001f && n.anchorMin.y <= 0.001f && n.anchorMax.x >= 0.999f && n.anchorMax.y >= 0.999f;
        Vector2 kt = n.rect.size;
        bool quaKhung = kt.x > 2000f || kt.y > 1500f;
        if (ten.StartsWith("fx") || ten.Contains("glow") || ten.Contains("quangsang") || ten.Contains("sunray") || ten.Contains("toast") || ten.Contains("tooltip")) return;   // hieu ung / thong bao: bo ca nhanh
        // nen mo (Dim/Overlay/Blocker) va tam qua khung: KHONG phai bang, nhung van tim bang ben trong
        if (popup && !boQua && !phuKin && !quaKhung && kt.x >= 360f && kt.y >= 240f && n.GetComponent<Graphic>() != null)
        {
            ra.Add(n);
            return;
        }
        for (int i = 0; i < n.childCount; i++) TimBang(n.GetChild(i) as RectTransform, popup, sau + 1, ra);
    }

    private static readonly Vector3[] _g = new Vector3[4];

    private static bool DauChan(RectTransform bang, RectTransform rtCv, out Vector2 mn, out Vector2 mx)
    {
        Vector3 a = new Vector3(float.MaxValue, float.MaxValue), b = new Vector3(float.MinValue, float.MinValue);
        Gom(bang, rtCv, true, ref a, ref b);
        mn = a; mx = b;
        return a.x <= b.x && a.y <= b.y;
    }

    private static void Gom(RectTransform n, RectTransform rtCv, bool laGoc, ref Vector3 a, ref Vector3 b)
    {
        if (n == null) return;
        if (!laGoc && !n.gameObject.activeSelf) return;
        string ten = n.name;
        if (!laGoc && (ten.IndexOf("Dim", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       ten.IndexOf("Overlay", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       ten.IndexOf("Blocker", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       ten.IndexOf("Toast", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       ten.StartsWith("FX", System.StringComparison.OrdinalIgnoreCase) ||
                       ten.IndexOf("Glow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       ten.IndexOf("QuangSang", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       ten.IndexOf("SunRay", System.StringComparison.OrdinalIgnoreCase) >= 0)) return;   // hieu ung tran ra ngoai la co y
        if (!laGoc && (n.rect.width > 2000f || n.rect.height > 1500f)) return;
        n.GetWorldCorners(_g);
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = rtCv.InverseTransformPoint(_g[i]);
            a = Vector3.Min(a, p); b = Vector3.Max(b, p);
        }
        if (n.GetComponent<RectMask2D>() != null || n.GetComponent<Mask>() != null) return;
        for (int i = 0; i < n.childCount; i++) Gom(n.GetChild(i) as RectTransform, rtCv, false, ref a, ref b);
    }

    private static Vector2 KichThuocCanvas(CanvasScaler sc, float w, float h)
    {
        Vector2 r = sc.referenceResolution;
        float s;
        switch (sc.screenMatchMode)
        {
            case CanvasScaler.ScreenMatchMode.Expand: s = Mathf.Min(w / r.x, h / r.y); break;
            case CanvasScaler.ScreenMatchMode.Shrink: s = Mathf.Max(w / r.x, h / r.y); break;
            default:
                float lw = Mathf.Log(w / r.x, 2f), lh = Mathf.Log(h / r.y, 2f);
                s = Mathf.Pow(2f, Mathf.Lerp(lw, lh, sc.matchWidthOrHeight));
                break;
        }
        return new Vector2(w / s, h / s);
    }

    // Popup co code tu co nam o object KHAC (khong phai cha cua bang) -> nhan theo ten goc popup.
    //   WarehousePopup: WarehousePopupUI.VuaManHinh()  ·  popup_Menu: ShopManager + PopupFitClamp
    private static readonly HashSet<string> GocTuCo = new HashSet<string>
    { "WarehousePopup", "popup_Menu", "Canvas_MarketPopup", "MillPopup_Root", "Canvas_StallPopup", "Canvas_OrderBoardPopup" };

    /// <summary>Popup cu khong con dung (LevelUpPopup ban dau, da thay bang Popup_LevelUp_Township).</summary>
    private static bool LaPopupCu(RectTransform b)
    {
        for (Transform t = b; t != null; t = t.parent)
            if (t.name == "LevelUpPopup" && !t.gameObject.activeSelf) return true;
        return false;
    }

    private static bool CoScriptTuCo(RectTransform b, RectTransform rtCv)
    {
        for (Transform t = b; t != null; t = t.parent)
            if (GocTuCo.Contains(t.name)) return true;
        for (Transform t = b; t != null; t = t.parent)
        {
            foreach (var mb in t.GetComponents<MonoBehaviour>())
                if (mb != null && CoTuCo.Contains(mb.GetType().Name)) return true;
            if (t == rtCv) break;
        }
        return false;
    }

    private static string DuongDan(Transform t, Transform dung)
    {
        var ds = new List<string>();
        for (; t != null && t != dung; t = t.parent) if (t.name != "~SafeArea") ds.Add(t.name);
        ds.Reverse();
        return string.Join("/", ds);
    }
}
