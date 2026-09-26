// ============================================================================
//  Edric Tools > Chuong (Pen) > 1. Lap chuong moi v3 (ngoai map + shop)   (2026-09-25)
//                             > 2. Tra lai chuong cu
//  Lam gi (co backup file truoc, tra lai = chep nguoc):
//   - 4 prefab Pen_01 (bo) / Pen_02 (heo) / Pen_03 (ga) / Pen_04 (bo sua): doi hinh BarnSprite sang art v3
//     (AnimalPens_v3/_Opt, da xoa nen trang giua song rao). GIU scale, collider, script, dan con vat.
//     Moi chuong trong scene + chuong mua moi o shop deu dung prefab nay -> doi het cung luc.
//   - Co chuong: CUNG BE NGANG hang rao voi chuong cu (PPU tinh tu anh) -> chiem dung o luoi nhu cu, vua phai.
//   - Them PenWalkArea (vung san that, do tu hinh): con vat chi di trong san, khong de len nha chuong / may an,
//     khong ra ngoai rao (LivestockAI tu doc).
//   - Them chong chong ga (PV_Weathervane) quay tren mai.
//   - Shop: doi icon 4 mon chuong sang hinh chuong moi.
//  So lieu do tu anh 1024x1024 (px, goc tren-trai):
//    bo : rao trai (27,558) phai (997,557) day (511,903) -> PPU 970/4.13 = 234.9
//    heo: rao trai (17,568) phai (1006,568) day (512,949) -> PPU 989/4.13 = 239.5
//    ga : rao trai (12,567) phai (1011,563) day (512,921) -> PPU 999/4.13 = 241.9
//    (4.13 = be ngang sprite chuong cu chuongmoigiasuc 413 px / PPU 100)
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ChuongV3SetupTool
{
    private const string GOC = "Edric Tools/Chuong (Pen)/";
    private const string OPT = "Assets/Art/Buildings/AnimalPens_v3/_Opt/";
    private const string PEN = "Assets/_Game/Farm/CÔNG TRÌNH/";
    private const float PPU_VANE = 238f;
    private static readonly int[] THU_TU_VANE = { 0, 1, 2, 4, 6, 7 };   // truoc -> phai -> sau -> trai

    private class Loai
    {
        public string anh, icon;
        public float ppu; public Vector2 day;        // px
        public Vector2[] san;                         // px, da giac loi
        public Vector2 vane; public float coVane;     // px, ti le
        public bool coChongChong = true;
    }

    private static readonly Loai BO = new Loai
    {
        anh = "cow_barn_base_opt", icon = "icon_cow_barn", ppu = 234.9f, day = new Vector2(511, 903),
        san = new[] { new Vector2(243, 500), new Vector2(781, 500), new Vector2(850, 560), new Vector2(512, 780), new Vector2(175, 560) },
        vane = new Vector2(560, 132), coVane = 1f, coChongChong = false,   // Sep 26/09: chuong bo cung bo ga tren mai
    };
    private static readonly Loai HEO = new Loai
    {
        anh = "pig_pen_base_opt", icon = "icon_pig_pen", ppu = 239.5f, day = new Vector2(512, 949),
        san = new[] { new Vector2(345, 500), new Vector2(765, 500), new Vector2(855, 570), new Vector2(512, 780), new Vector2(265, 635) },
        vane = new Vector2(497, 111), coVane = 1f, coChongChong = false,   // Sep 25/09: chuong heo khong co ga tren mai
    };
    private static readonly Loai GA = new Loai
    {
        anh = "chicken_coop_base_opt", icon = "icon_chicken_coop", ppu = 241.9f, day = new Vector2(512, 921),
        san = new[] { new Vector2(300, 470), new Vector2(690, 470), new Vector2(785, 600), new Vector2(512, 790), new Vector2(175, 565) },
        vane = new Vector2(515, 106), coVane = 0.8f,
    };

    private static readonly (string prefab, Loai loai)[] DS =
    {
        ("Pen_01.prefab", BO), ("Pen_02.prefab", HEO), ("Pen_03.prefab", GA), ("Pen_04.prefab", BO),
    };

    [MenuItem(GOC + "1. Lap chuong moi v3 (ngoai map + shop)", false, 1)]
    public static void Lap()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var thieu = new List<string>();
        foreach (var l in new[] { BO, HEO, GA })
        {
            if (!File.Exists(OPT + l.anh + ".png")) thieu.Add(l.anh);
            if (!File.Exists(OPT + l.icon + ".png")) thieu.Add(l.icon);
        }
        for (int i = 0; i < 8; i++) if (!File.Exists(OPT + $"pen_vane_{i}.png")) thieu.Add($"pen_vane_{i}");
        foreach (var d in DS) if (!File.Exists(PEN + d.prefab)) thieu.Add(d.prefab);
        if (thieu.Count > 0) { Bao("Thieu file:\n" + string.Join("\n", thieu)); return; }

        // Mon shop tro toi 4 prefab
        var prefabs = DS.Select(d => AssetDatabase.LoadAssetAtPath<GameObject>(PEN + d.prefab)).ToArray();
        var shop = new Dictionary<int, List<PlaceableItemData>>();
        foreach (var g in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            var it = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(g));
            if (it == null || it.prefabToBuild == null) continue;
            for (int i = 0; i < prefabs.Length; i++)
                if (prefabs[i] != null && it.prefabToBuild == prefabs[i])
                {
                    if (!shop.ContainsKey(i)) shop[i] = new List<PlaceableItemData>();
                    shop[i].Add(it);
                }
        }
        if (!EditorUtility.DisplayDialog("Chuong v3", $"Doi 4 chuong (bo, heo, ga, bo sua) sang art v3 + vung di cho con vat + chong chong.\nDoi icon {shop.Values.Sum(v => v.Count)} mon chuong trong Shop.\n\nBackup file truoc. Tra lai: muc 2.", "Lap", "Huy")) return;

        // ---- Backup
        string goc = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(goc, "_Backup_ChuongV3_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        var manifest = new List<string>();
        int stt = 0;
        void Luu(string asset)
        {
            string ten = (stt++).ToString("00") + "_" + Path.GetFileName(asset);
            File.Copy(Path.Combine(goc, asset), Path.Combine(bk, ten), true);
            manifest.Add(ten + "|" + asset);
        }
        foreach (var d in DS) Luu(PEN + d.prefab);
        foreach (var ds in shop.Values) foreach (var it in ds) Luu(AssetDatabase.GetAssetPath(it));
        File.WriteAllLines(Path.Combine(bk, "manifest.txt"), manifest);

        // ---- Import anh
        foreach (var l in new[] { BO, HEO, GA })
        {
            Nhap(OPT + l.anh + ".png", l.ppu, new Vector2(l.day.x / 1024f, (1024f - l.day.y) / 1024f), 1024);
            Nhap(OPT + l.icon + ".png", 100f, new Vector2(0.5f, 0.5f), 512);
        }
        for (int i = 0; i < 8; i++) Nhap(OPT + $"pen_vane_{i}.png", PPU_VANE, new Vector2(0.5f, 0f), 256);
        var vane = THU_TU_VANE.Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(OPT + $"pen_vane_{i}.png")).Where(s => s != null).ToArray();

        // ---- Prefab
        var baoCao = new List<string>();
        foreach (var d in DS)
        {
            string path = PEN + d.prefab;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var barnT = root.transform.Find("BarnSprite");
                var barn = barnT != null ? barnT.GetComponent<SpriteRenderer>() : null;
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(OPT + d.loai.anh + ".png");
                if (barn == null || sp == null) { baoCao.Add(d.prefab + ": KHONG thay BarnSprite / sprite, bo qua"); continue; }
                barn.sprite = sp;

                // Vung san: goc = object co spawner con vat (cha cua con vat), mac dinh root
                Transform chuSan = root.transform;
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && mb.GetType().Name == "HappyHarvestAnimalVisualSpawner") { chuSan = mb.transform; break; }
                var vung = chuSan.GetComponent<PenWalkArea>();
                if (vung == null) vung = chuSan.gameObject.AddComponent<PenWalkArea>();
                vung.diem = d.loai.san.Select(px =>
                {
                    Vector3 w = barnT.TransformPoint(PxSangLocal(px, d.loai));
                    Vector3 l = chuSan.InverseTransformPoint(w);
                    return new Vector2(l.x, l.y);
                }).ToArray();

                // Chong chong
                var cu = barnT.Find("PV_Weathervane");
                if (!d.loai.coChongChong)
                {
                    if (cu != null) Object.DestroyImmediate(cu.gameObject, true);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    baoCao.Add($"{d.prefab}: {d.loai.anh}, san {vung.diem.Length} diem, khong chong chong");
                    continue;
                }
                GameObject vgo = cu != null ? cu.gameObject : new GameObject("PV_Weathervane");
                vgo.transform.SetParent(barnT, false);
                vgo.transform.localPosition = PxSangLocal(d.loai.vane, d.loai);
                vgo.transform.localScale = Vector3.one * d.loai.coVane;
                var vsr = vgo.GetComponent<SpriteRenderer>(); if (vsr == null) vsr = vgo.AddComponent<SpriteRenderer>();
                vsr.sprite = vane.Length > 0 ? vane[0] : null;
                vsr.sortingLayerID = barn.sortingLayerID;
                vsr.sortingOrder = barn.sortingOrder + 1;
                vsr.sharedMaterial = barn.sharedMaterial;
                var loop = vgo.GetComponent<SpriteLoop2D>(); if (loop == null) loop = vgo.AddComponent<SpriteLoop2D>();
                loop.frames = vane; loop.fps = 1.2f;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                baoCao.Add($"{d.prefab}: {d.loai.anh}, san {vung.diem.Length} diem");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ---- Shop icon
        int soIcon = 0;
        for (int i = 0; i < DS.Length; i++)
        {
            if (!shop.ContainsKey(i)) continue;
            var ic = AssetDatabase.LoadAssetAtPath<Sprite>(OPT + DS[i].loai.icon + ".png");
            if (ic == null) continue;
            foreach (var it in shop[i]) { it.itemIcon = ic; EditorUtility.SetDirty(it); soIcon++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[ChuongV3] " + string.Join(" | ", baoCao) + $" | icon shop: {soIcon}. Backup: {Path.GetFileName(bk)}");
        Bao($"Xong.\n{string.Join("\n", baoCao)}\nIcon shop doi: {soIcon}\n\nBam Ctrl+S (scene) roi Play xem.\nChinh vung di: chon chuong > PenWalkArea (khung xanh).\nHong: muc 2. Backup: {Path.GetFileName(bk)}");
    }

    [MenuItem(GOC + "3. Bo chong chong ga tren chuong heo", false, 3)]
    public static void BoChongChongHeo()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        string path = PEN + "Pen_02.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        bool co = false;
        try
        {
            var v = root.transform.Find("BarnSprite/PV_Weathervane");
            if (v != null) { Object.DestroyImmediate(v.gameObject, true); co = true; PrefabUtility.SaveAsPrefabAsset(root, path); }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Bao(co ? "Da bo chong chong tren chuong heo (Pen_02). Moi chuong heo tren map + mua moi deu het." : "Chuong heo khong co chong chong.");
    }

    [MenuItem(GOC + "2. Tra lai chuong cu", false, 2)]
    public static void TraLai()
    {
        string goc = Path.GetDirectoryName(Application.dataPath);
        var bk = Directory.GetDirectories(goc, "_Backup_ChuongV3_*").Where(d => File.Exists(Path.Combine(d, "manifest.txt")))
                          .OrderByDescending(d => d).FirstOrDefault();
        if (bk == null) { Bao("Khong co ban backup chuong nao."); return; }
        var dong = File.ReadAllLines(Path.Combine(bk, "manifest.txt")).Where(s => s.Contains("|")).ToList();
        if (!EditorUtility.DisplayDialog("Chuong v3", $"Tra lai {dong.Count} file tu {Path.GetFileName(bk)}?", "Tra lai", "Huy")) return;
        foreach (var d in dong)
        {
            var t = d.Split('|');
            File.Copy(Path.Combine(bk, t[0]), Path.Combine(goc, t[1]), true);
            AssetDatabase.ImportAsset(t[1], ImportAssetOptions.ForceUpdate);
        }
        Directory.Move(bk, bk + "_DaTraLai");
        Bao($"Da tra lai {dong.Count} file (4 chuong + icon shop).");
    }

    // =====================================================================
    //  4. CHUONG V4 (2026-09-26): art ve lai dung goc iso 2:1 + animation
    //   - Pen_01 bo / Pen_04 bo sua (hinh rieng) / Pen_03 ga: doi hinh BarnSprite sang AnimalPens_v4/_Opt
    //     (da xoa nen trang giua song rao), vung di moi, doi icon shop.
    //   - Lop dong (con cua BarnSprite, ten PV4_*): co kho lay theo gio, nuoc mang gon song, trung lac trong o.
    //   - Chuong heo: art v4 ve doc 30.8 do -> nen doc x0.84 cho dung 26.6 do. Them rau lac trong mang +
    //     BUN BAN TUNG khi heo buoc vao vung bun (SpriteLoop2D che do "phat khi con vat di qua").
    //   - Chuong ga: doi vi tri chong chong len dinh mai moi.
    //   Moi frame xuat san dung khung + pivot = diem day hang rao -> dat localPosition 0 la khop hinh.
    // =====================================================================
    private const string OPT4 = "Assets/Art/Buildings/AnimalPens_v4/_Opt/";

    private class Lop4
    {
        public string key; public int n; public Vector2 pivot; public float fps;
        public bool phat; public Vector2 tamPx, banKinhPx;
    }
    private class Loai4
    {
        public string prefab, anh, icon; public float ppu; public Vector2 day;
        public Vector2[] san; public Lop4[] lop; public bool doiHinh = true;
        public bool coVane; public Vector2 vanePx;
    }

    private static readonly Loai4[] DS4 =
    {
        new Loai4 { prefab = "Pen_01.prefab", anh = "cow_barn_base_opt", icon = "icon_cow_barn", ppu = 990f / 4.13f, day = new Vector2(512, 937),
            san = new[] { new Vector2(430, 570), new Vector2(690, 565), new Vector2(840, 680), new Vector2(512, 820), new Vector2(200, 690) },
            lop = new[] {
                new Lop4 { key = "cow_hay",   n = 6, pivot = new Vector2(1.4842f, -2.2765f), fps = 5f },
                new Lop4 { key = "cow_water", n = 8, pivot = new Vector2(2.6063f, -3.5529f), fps = 6f } } },
        new Loai4 { prefab = "Pen_04.prefab", anh = "dairy_barn_base_opt", icon = "icon_dairy_barn", ppu = 948f / 4.13f, day = new Vector2(512, 937),
            san = new[] { new Vector2(410, 615), new Vector2(680, 600), new Vector2(840, 690), new Vector2(512, 825), new Vector2(210, 700) },
            lop = new[] {
                new Lop4 { key = "dairy_hay",   n = 6, pivot = new Vector2(1.6375f, -2.2313f), fps = 5f },
                new Lop4 { key = "dairy_water", n = 8, pivot = new Vector2(2.48f, -3.1889f), fps = 6f } } },
        new Loai4 { prefab = "Pen_03.prefab", anh = "chicken_coop_base_opt", icon = "icon_chicken_coop", ppu = 949f / 4.13f, day = new Vector2(512, 938),
            san = new[] { new Vector2(340, 610), new Vector2(640, 660), new Vector2(800, 725), new Vector2(512, 830), new Vector2(200, 700) },
            lop = new[] { new Lop4 { key = "chicken_egg", n = 6, pivot = new Vector2(-0.65f, -4.8667f), fps = 4f } },
            coVane = true, vanePx = new Vector2(619, 206) },
        // Heo: art v4 ve doc 30.8 do -> da NEN DOC x0.84 quanh day hang rao cho dung 26.6 do (khop luoi)
        new Loai4 { prefab = "Pen_02.prefab", anh = "pig_pen_base_opt", icon = "icon_pig_pen", ppu = 987f / 4.13f, day = new Vector2(512, 935),
            san = new[] { new Vector2(380, 600), new Vector2(690, 580), new Vector2(850, 690), new Vector2(512, 825), new Vector2(210, 700) },
            lop = new[] {
                new Lop4 { key = "pig_food", n = 4, pivot = new Vector2(1.7043f, -1.7353f), fps = 3f },
                new Lop4 { key = "pig_splash", n = 8, pivot = new Vector2(-0.412f, -1.0208f), fps = 12f,
                    phat = true, tamPx = new Vector2(740, 660), banKinhPx = new Vector2(95, 45) } } },
    };

    [MenuItem(GOC + "4. Lap chuong v4 (goc 2:1 + animation)", false, 4)]
    public static void LapV4()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var thieu = new List<string>();
        foreach (var l in DS4)
        {
            if (!File.Exists(PEN + l.prefab)) thieu.Add(l.prefab);
            if (l.doiHinh) { if (!File.Exists(OPT4 + l.anh + ".png")) thieu.Add(l.anh); if (!File.Exists(OPT4 + l.icon + ".png")) thieu.Add(l.icon); }
            foreach (var p in l.lop) for (int i = 0; i < p.n; i++) if (!File.Exists(OPT4 + p.key + "_" + i + ".png")) thieu.Add(p.key + "_" + i);
        }
        if (thieu.Count > 0) { Bao("Thieu file:\n" + string.Join("\n", thieu.Take(20))); return; }

        var shop = new Dictionary<string, List<PlaceableItemData>>();
        var prefabs = DS4.ToDictionary(d => d.prefab, d => AssetDatabase.LoadAssetAtPath<GameObject>(PEN + d.prefab));
        foreach (var g in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            var it = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(g));
            if (it == null || it.prefabToBuild == null) continue;
            foreach (var kv in prefabs)
                if (kv.Value != null && it.prefabToBuild == kv.Value)
                {
                    if (!shop.ContainsKey(kv.Key)) shop[kv.Key] = new List<PlaceableItemData>();
                    shop[kv.Key].Add(it);
                }
        }
        if (!EditorUtility.DisplayDialog("Chuong v4",
                "Lap chuong v4 (goc iso 2:1 khop luoi):\n- Bo, Bo sua (hinh rieng), Ga, Heo: hinh moi + vung di + icon shop\n" +
                "- Animation: co kho, nuoc mang, trung trong o, rau trong mang heo\n- Heo: hinh moi (da nen cho dung goc), bun ban tung khi heo di qua\n\n" +
                "Backup truoc. Tra lai: muc 2.", "Lap", "Huy")) return;

        // ---- Backup (cung dinh dang v3 -> muc 2 tra lai duoc)
        string goc = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(goc, "_Backup_ChuongV3_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_v4");
        Directory.CreateDirectory(bk);
        var manifest = new List<string>();
        int stt = 0;
        void Luu(string asset)
        {
            string ten = (stt++).ToString("00") + "_" + Path.GetFileName(asset);
            File.Copy(Path.Combine(goc, asset), Path.Combine(bk, ten), true);
            manifest.Add(ten + "|" + asset);
        }
        foreach (var d in DS4) Luu(PEN + d.prefab);
        foreach (var ds in shop.Values) foreach (var it in ds) Luu(AssetDatabase.GetAssetPath(it));
        File.WriteAllLines(Path.Combine(bk, "manifest.txt"), manifest);

        // ---- Import
        foreach (var l in DS4)
        {
            if (l.doiHinh)
            {
                Nhap(OPT4 + l.anh + ".png", l.ppu, new Vector2(l.day.x / 1024f, (1024f - l.day.y) / 1024f), 1024);
                Nhap(OPT4 + l.icon + ".png", 100f, new Vector2(0.5f, 0.5f), 512);
            }
            foreach (var p in l.lop) for (int i = 0; i < p.n; i++) Nhap(OPT4 + p.key + "_" + i + ".png", l.ppu, p.pivot, 256);
        }

        var baoCao = new List<string>();
        foreach (var d in DS4)
        {
            string path = PEN + d.prefab;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var barnT = root.transform.Find("BarnSprite");
                var barn = barnT != null ? barnT.GetComponent<SpriteRenderer>() : null;
                if (barn == null) { baoCao.Add(d.prefab + ": KHONG thay BarnSprite, bo qua"); continue; }
                string ghi = d.prefab + ":";
                if (d.doiHinh)
                {
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(OPT4 + d.anh + ".png");
                    if (sp == null) { baoCao.Add(d.prefab + ": khong nap duoc " + d.anh); continue; }
                    barn.sprite = sp;
                    ghi += " hinh v4";

                    Transform chuSan = root.transform;
                    foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                        if (mb != null && mb.GetType().Name == "HappyHarvestAnimalVisualSpawner") { chuSan = mb.transform; break; }
                    var vung = chuSan.GetComponent<PenWalkArea>();
                    if (vung == null) vung = chuSan.gameObject.AddComponent<PenWalkArea>();
                    vung.diem = d.san.Select(px =>
                    {
                        Vector3 w = barnT.TransformPoint(Px4(px, d));
                        Vector3 lc = chuSan.InverseTransformPoint(w);
                        return new Vector2(lc.x, lc.y);
                    }).ToArray();
                    ghi += ", san " + vung.diem.Length + " diem";
                }

                // chong chong: bo tren moi chuong tru ga; ga doi len dinh mai moi
                var vane = barnT.Find("PV_Weathervane");
                if (vane != null)
                {
                    if (d.coVane) { vane.localPosition = Px4(d.vanePx, d); ghi += ", chong chong len dinh mai"; }
                    else { Object.DestroyImmediate(vane.gameObject, true); ghi += ", bo chong chong"; }
                }

                // lop dong: xoa ban cu (chay lai khong nhan doi)
                for (int c = barnT.childCount - 1; c >= 0; c--)
                    if (barnT.GetChild(c).name.StartsWith("PV4_")) Object.DestroyImmediate(barnT.GetChild(c).gameObject, true);
                foreach (var p in d.lop)
                {
                    var frames = new Sprite[p.n];
                    for (int i = 0; i < p.n; i++) frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(OPT4 + p.key + "_" + i + ".png");
                    var go = new GameObject("PV4_" + p.key);
                    go.transform.SetParent(barnT, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = Vector3.one;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = frames[0];
                    sr.sortingLayerID = barn.sortingLayerID;
                    sr.sortingOrder = barn.sortingOrder + 1;
                    sr.sharedMaterial = barn.sharedMaterial;
                    var loop = go.AddComponent<SpriteLoop2D>();
                    loop.frames = frames; loop.fps = p.fps;
                    if (p.phat)
                    {
                        loop.chiPhatKhiConVatDiQua = true;
                        loop.batDauNgauNhien = false;
                        loop.tamVung = Px4(p.tamPx, d);
                        loop.banKinhVung = new Vector2(p.banKinhPx.x / d.ppu, p.banKinhPx.y / d.ppu);
                        sr.sprite = null;                          // an san, chi hien khi phat
                    }
                    ghi += ", " + p.key;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                baoCao.Add(ghi);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        int soIcon = 0;
        foreach (var d in DS4)
        {
            if (!d.doiHinh || !shop.ContainsKey(d.prefab)) continue;
            var ic = AssetDatabase.LoadAssetAtPath<Sprite>(OPT4 + d.icon + ".png");
            if (ic == null) continue;
            foreach (var it in shop[d.prefab]) { it.itemIcon = ic; EditorUtility.SetDirty(it); soIcon++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[ChuongV4] " + string.Join(" | ", baoCao) + $" | icon shop: {soIcon}. Backup: {Path.GetFileName(bk)}");
        Bao($"Xong chuong v4.\n{string.Join("\n", baoCao)}\nIcon shop doi: {soIcon}\n\nCtrl+S roi Play xem.\nHong: muc 2 (tra lai). Backup: {Path.GetFileName(bk)}");
    }

    private static Vector3 Px4(Vector2 px, Loai4 l) => new Vector3((px.x - l.day.x) / l.ppu, (l.day.y - px.y) / l.ppu, 0f);

    // =====================================================================
    /// <summary>px anh (goc tren-trai) -> local cua BarnSprite (don vi sprite, pivot = day hang rao).</summary>
    private static Vector3 PxSangLocal(Vector2 px, Loai l) => new Vector3((px.x - l.day.x) / l.ppu, (l.day.y - px.y) / l.ppu, 0f);

    private static void Nhap(string path, float ppu, Vector2 pivot, int maxSize)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { AssetDatabase.ImportAsset(path); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (ti == null) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.isReadable = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.maxTextureSize = maxSize;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.crunchedCompression = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        st.spriteMeshType = SpriteMeshType.Tight;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    private static void Bao(string s) => EditorUtility.DisplayDialog("Chuong v3", s, "OK");
}
