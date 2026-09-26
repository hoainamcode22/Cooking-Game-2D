// ============================================================================
//  Edric Tools > Art > Lam sang anh dang chon (giong quay hang)   (2026-09-25)
//  Sep chot: moi asset phai SANG nhu quay hang (FarmStand). Tool nay chinh do sang / bao hoa cua anh
//  dang chon trong Project cho KHOP quay hang (khop phan vi do sang, giu nguyen mau, giu net vien toi hon).
//  - Chon 1 hoac nhieu PNG cung 1 cong trinh (base + cac lop dong): dung CHUNG 1 duong cong lay tu anh
//    TO NHAT -> cac lop khong lech mau nhau.
//  - Backup anh goc vao <project>/_Backup_LamSang_<gio>/ truoc khi ghi. Menu "Tra lai" chep nguoc lai.
//  - Chay lai tren anh da sang: gan nhu khong doi (vi da khop mau roi).
//  Khong doi .meta (PPU, pivot, cat sprite giu nguyen) -> scene khong mat tham chieu.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LamSangArtTool
{
    private const string GOC = "Edric Tools/Art/";
    private static readonly string[] MAU = { "Assets/Art/Buildings/FarmStand/_Opt/stand_base_opt.png", "Assets/Art/Buildings/FarmStand/stand_base.png" };
    private static readonly float[] Q = { 5, 25, 50, 75, 95 };
    private static readonly float[] W = { 0.55f, 0.8f, 1f, 1f };      // vung toi keo it hon -> net vien van dam

    [MenuItem(GOC + "1. Lam sang anh dang chon (giong quay hang)", false, 1)]
    public static void LamSang()
    {
        string mau = MAU.FirstOrDefault(File.Exists);
        if (mau == null) { Bao("Khong thay anh mau quay hang (FarmStand)."); return; }
        var ds = Selection.objects.Select(AssetDatabase.GetAssetPath)
                                  .Where(p => !string.IsNullOrEmpty(p) && p.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                                  .Distinct().ToList();
        if (ds.Count == 0) { Bao("Chon 1 hoac nhieu anh PNG trong Project truoc (vd market_base_opt + cac lop cua cho)."); return; }
        if (ds.Contains(mau)) ds.Remove(mau);
        if (ds.Count == 0) { Bao("Dang chon chinh anh mau quay hang."); return; }

        var texMau = Doc(mau);
        float[] dich; float satDich;
        ThongKe(texMau, out dich, out satDich);
        Object.DestroyImmediate(texMau);

        // Duong cong lay tu anh TO NHAT trong nhom
        var texs = ds.ToDictionary(p => p, Doc);
        string lon = texs.OrderByDescending(kv => kv.Value.width * kv.Value.height).First().Key;
        float[] nguon; float satNguon;
        ThongKe(texs[lon], out nguon, out satNguon);
        float[] den = new float[5];
        for (int i = 0; i < 4; i++) den[i] = nguon[i] + (dich[i] - nguon[i]) * W[i];
        den[4] = Mathf.Min(0.97f, nguon[4] + (den[3] - nguon[3]));
        float satX = Mathf.Clamp(satDich / Mathf.Max(0.001f, satNguon), 0.85f, 1.1f);

        string tomTat = $"Do sang giua: {nguon[2]:0.00} -> {den[2]:0.00} (quay hang {dich[2]:0.00})\nBao hoa x{satX:0.00}\nDuong cong lay tu: {Path.GetFileName(lon)}";
        if (!EditorUtility.DisplayDialog("Lam sang art", $"Lam sang {ds.Count} anh cho giong quay hang:\n{string.Join("\n", ds.Select(Path.GetFileName))}\n\n{tomTat}\n\nBackup anh goc truoc. Tra lai: menu 2.", "Lam", "Huy"))
        { foreach (var t in texs.Values) Object.DestroyImmediate(t); return; }

        string goc = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(goc, "_Backup_LamSang_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        var manifest = new List<string>();
        int i2 = 0;
        foreach (var kv in texs)
        {
            string ten = (i2++).ToString("00") + "_" + Path.GetFileName(kv.Key);
            File.Copy(Path.Combine(goc, kv.Key), Path.Combine(bk, ten), true);
            manifest.Add(ten + "|" + kv.Key);
            var px = kv.Value.GetPixels32();
            ApDung(px, nguon, den, satX);
            kv.Value.SetPixels32(px);
            File.WriteAllBytes(Path.Combine(goc, kv.Key), kv.Value.EncodeToPNG());
            Object.DestroyImmediate(kv.Value);
        }
        File.WriteAllLines(Path.Combine(bk, "manifest.txt"), manifest);
        foreach (var p in ds) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[LamSang] Da lam sang {ds.Count} anh. {tomTat.Replace('\n', ' ')}. Backup: {Path.GetFileName(bk)}");
        Bao($"Xong {ds.Count} anh. Backup: {Path.GetFileName(bk)}\nKhong ung: menu 2 tra lai.");
    }

    [MenuItem(GOC + "2. Tra lai lan lam sang gan nhat", false, 2)]
    public static void TraLai()
    {
        string goc = Path.GetDirectoryName(Application.dataPath);
        var bk = Directory.GetDirectories(goc, "_Backup_LamSang_*").Where(d => File.Exists(Path.Combine(d, "manifest.txt")))
                          .OrderByDescending(d => d).FirstOrDefault();
        if (bk == null) { Bao("Khong co lan lam sang nao de tra lai."); return; }
        var dong = File.ReadAllLines(Path.Combine(bk, "manifest.txt")).Where(s => s.Contains("|")).ToList();
        if (!EditorUtility.DisplayDialog("Lam sang art", $"Tra lai {dong.Count} anh tu {Path.GetFileName(bk)}?", "Tra lai", "Huy")) return;
        foreach (var d in dong)
        {
            var t = d.Split('|');
            File.Copy(Path.Combine(bk, t[0]), Path.Combine(goc, t[1]), true);
            AssetDatabase.ImportAsset(t[1], ImportAssetOptions.ForceUpdate);
        }
        Directory.Move(bk, bk + "_DaTraLai");
        Bao($"Da tra lai {dong.Count} anh.");
    }

    // =====================================================================
    //  3. (2026-09-26) LAM SANG + TO NET cho CONG TRINH dang chon trong Hierarchy / Scene (chon nhieu bang Ctrl).
    //  Moi cong trinh: gom moi PNG cua cac SpriteRenderer (ca con) -> lam sang chung 1 duong cong nhu muc 1,
    //  roi ve NET VIEN nau dam quanh hinh (giong net quay hang) cho anh TO NHAT (anh than nha).
    //  Anh cat nhieu sprite (Multiple) chi lam sang, khong to net (net se lan sang sprite ben canh).
    //  Backup chung dinh dang voi muc 1 -> muc 2 "Tra lai" dung duoc.
    // =====================================================================
    [MenuItem(GOC + "3. Lam sang + to net CONG TRINH dang chon (Hierarchy)", false, 3)]
    public static void LamSangToNetCongTrinh()
    {
        string mau = MAU.FirstOrDefault(File.Exists);
        if (mau == null) { Bao("Khong thay anh mau quay hang (FarmStand)."); return; }
        var nhom = new List<List<string>>();
        var moTa = new List<string>();
        var daCo = new HashSet<string>();
        foreach (var go in Selection.gameObjects)
        {
            var ds = new List<string>();
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || sr.sprite == null || sr.sprite.texture == null) continue;
                string p = AssetDatabase.GetAssetPath(sr.sprite.texture);
                if (string.IsNullOrEmpty(p) || !p.StartsWith("Assets/") || !p.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (MAU.Contains(p) || !daCo.Add(p)) continue;
                ds.Add(p);
            }
            if (ds.Count > 0) { nhom.Add(ds); moTa.Add("- " + go.name + ": " + string.Join(", ", ds.Select(Path.GetFileName))); }
        }
        if (nhom.Count == 0) { Bao("Chon cong trinh trong Hierarchy / Scene truoc (giu Ctrl de chon nhieu).\nVd: 5 ngoi nha, nha ga tau, bang don hang, nha kho."); return; }

        int chon = EditorUtility.DisplayDialogComplex("Lam sang + to net",
            $"{nhom.Count} cong trinh:\n{string.Join("\n", moTa)}\n\nLam sang cho giong quay hang, va to net vien nau dam quanh anh than nha?\nBackup truoc. Khong ung: muc 2 tra lai.",
            "Sang + to net", "Huy", "Chi lam sang");
        if (chon == 1) return;
        bool toNet = chon == 0;

        var texMau = Doc(mau);
        float[] dich; float satDich;
        ThongKe(texMau, out dich, out satDich);
        Object.DestroyImmediate(texMau);

        string goc = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(goc, "_Backup_LamSang_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        var manifest = new List<string>();
        var baoCao = new List<string>();
        int stt = 0;
        foreach (var ds in nhom)
        {
            var texs = ds.ToDictionary(p => p, Doc);
            string lon = texs.OrderByDescending(kv => kv.Value.width * kv.Value.height).First().Key;
            float[] nguon; float satNguon;
            ThongKe(texs[lon], out nguon, out satNguon);
            float[] den = new float[5];
            for (int i = 0; i < 4; i++) den[i] = nguon[i] + (dich[i] - nguon[i]) * W[i];
            den[4] = Mathf.Min(0.97f, nguon[4] + (den[3] - nguon[3]));
            float satX = Mathf.Clamp(satDich / Mathf.Max(0.001f, satNguon), 0.85f, 1.1f);
            foreach (var kv in texs)
            {
                string ten = (stt++).ToString("00") + "_" + Path.GetFileName(kv.Key);
                File.Copy(Path.Combine(goc, kv.Key), Path.Combine(bk, ten), true);
                manifest.Add(ten + "|" + kv.Key);
                var px = kv.Value.GetPixels32();
                ApDung(px, nguon, den, satX);
                string net = "";
                if (toNet && kv.Key == lon)
                {
                    var ti = AssetImporter.GetAtPath(kv.Key) as TextureImporter;
                    if (ti != null && ti.spriteImportMode == SpriteImportMode.Multiple) net = " (anh nhieu sprite: khong to net)";
                    else { ToNet(px, kv.Value.width, kv.Value.height); net = " + net"; }
                }
                kv.Value.SetPixels32(px);
                File.WriteAllBytes(Path.Combine(goc, kv.Key), kv.Value.EncodeToPNG());
                baoCao.Add(Path.GetFileName(kv.Key) + net);
                Object.DestroyImmediate(kv.Value);
            }
        }
        File.WriteAllLines(Path.Combine(bk, "manifest.txt"), manifest);
        foreach (var ds in nhom) foreach (var p in ds) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        Debug.Log("[LamSang] " + string.Join(" | ", baoCao) + " | Backup: " + Path.GetFileName(bk));
        Bao($"Xong {manifest.Count} anh:\n{string.Join("\n", baoCao)}\n\nBackup: {Path.GetFileName(bk)}\nKhong ung: muc 2 tra lai.");
    }

    // Net vien nau dam quanh phan dac cua anh (khoang cach chamfer 3-4, mep mem 1px). Day ~ 1/340 canh lon (2..6 px).
    private static readonly Color32 MAU_NET = new Color32(62, 36, 22, 255);
    private static void ToNet(Color32[] px, int w, int h)
    {
        int T = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(w, h) / 340f), 2, 6);
        const int INF = 1 << 28;
        var d = new int[w * h];
        for (int i = 0; i < d.Length; i++) d[i] = px[i].a >= 128 ? 0 : INF;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x, v = d[i];
                if (v == 0) continue;
                if (x > 0) v = Mathf.Min(v, d[i - 1] + 3);
                if (y > 0)
                {
                    v = Mathf.Min(v, d[i - w] + 3);
                    if (x > 0) v = Mathf.Min(v, d[i - w - 1] + 4);
                    if (x < w - 1) v = Mathf.Min(v, d[i - w + 1] + 4);
                }
                d[i] = v;
            }
        for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x, v = d[i];
                if (v == 0) continue;
                if (x < w - 1) v = Mathf.Min(v, d[i + 1] + 3);
                if (y < h - 1)
                {
                    v = Mathf.Min(v, d[i + w] + 3);
                    if (x < w - 1) v = Mathf.Min(v, d[i + w + 1] + 4);
                    if (x > 0) v = Mathf.Min(v, d[i + w - 1] + 4);
                }
                d[i] = v;
            }
        for (int i = 0; i < px.Length; i++)
        {
            if (d[i] == 0 || d[i] >= INF) continue;
            float kc = d[i] / 3f;
            float an = Mathf.Clamp01(T + 0.5f - kc);                 // do dam net tai diem nay
            if (an <= 0f) continue;
            var c = px[i];
            float pa = c.a / 255f, oa = an;
            float ra = pa + oa * (1f - pa);
            if (ra <= 0f) continue;
            float k1 = pa / ra, k2 = oa * (1f - pa) / ra;           // anh cu DE LEN net (giu mep mem cua anh)
            px[i] = new Color32((byte)Mathf.RoundToInt(c.r * k1 + MAU_NET.r * k2),
                                (byte)Mathf.RoundToInt(c.g * k1 + MAU_NET.g * k2),
                                (byte)Mathf.RoundToInt(c.b * k1 + MAU_NET.b * k2),
                                (byte)Mathf.RoundToInt(ra * 255f));
        }
    }

    // =====================================================================
    private static Texture2D Doc(string path)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(File.ReadAllBytes(path), false);
        return t;
    }

    private static float Sang(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;

    private static void ThongKe(Texture2D t, out float[] pv, out float sat)
    {
        var px = t.GetPixels32();
        var hist = new int[256];
        int n = 0; double s = 0;
        foreach (var c in px)
        {
            if (c.a < 204) continue;
            hist[Mathf.Clamp(Mathf.RoundToInt(Sang(c) * 255f), 0, 255)]++;
            int mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            s += mx > 0 ? (mx - mn) / (double)mx : 0;
            n++;
        }
        pv = new float[5];
        sat = n > 0 ? (float)(s / n) : 0.6f;
        if (n == 0) { for (int i = 0; i < 5; i++) pv[i] = Q[i] / 100f; return; }
        for (int i = 0; i < 5; i++)
        {
            int can = Mathf.CeilToInt(n * Q[i] / 100f), cong = 0, k = 0;
            for (; k < 256; k++) { cong += hist[k]; if (cong >= can) break; }
            pv[i] = Mathf.Clamp(k, 0, 255) / 255f;
        }
    }

    private static float Noi(float x, float[] a, float[] b)
    {
        // duong gap khuc qua (0,0), (a_i,b_i), (1,1)
        float x0 = 0f, y0 = 0f;
        for (int i = 0; i <= a.Length; i++)
        {
            float x1 = i < a.Length ? a[i] : 1f, y1 = i < a.Length ? b[i] : 1f;
            if (x <= x1) return x1 - x0 < 1e-5f ? y1 : Mathf.Lerp(y0, y1, (x - x0) / (x1 - x0));
            x0 = x1; y0 = y1;
        }
        return x;
    }

    private static void ApDung(Color32[] px, float[] nguon, float[] den, float satX)
    {
        for (int i = 0; i < px.Length; i++)
        {
            var c = px[i];
            if (c.a == 0) continue;
            float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
            float L = 0.299f * r + 0.587f * g + 0.114f * b;
            float k = L > 1e-4f ? Noi(L, nguon, den) / L : 1f;
            r *= k; g *= k; b *= k;
            float L2 = 0.299f * r + 0.587f * g + 0.114f * b;
            r = L2 + (r - L2) * satX; g = L2 + (g - L2) * satX; b = L2 + (b - L2) * satX;
            float mx = Mathf.Max(1f, Mathf.Max(r, Mathf.Max(g, b)));
            px[i] = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(r / mx * 255f), 0, 255),
                                (byte)Mathf.Clamp(Mathf.RoundToInt(g / mx * 255f), 0, 255),
                                (byte)Mathf.Clamp(Mathf.RoundToInt(b / mx * 255f), 0, 255), c.a);
        }
    }

    private static void Bao(string s) => EditorUtility.DisplayDialog("Lam sang art", s, "OK");
}
