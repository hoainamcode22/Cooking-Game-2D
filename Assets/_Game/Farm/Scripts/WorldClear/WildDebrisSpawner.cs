// ============================================================================
//  WildDebrisSpawner — RAI DA NHO + BUI RAM tren dat nguoi choi DA SO HUU (2026-09-25)
//  Goi 1 lan tu WorldClearManager.Start (truoc khi quet) -> vat rai ra tu cham Bua / Keo duoc (ten wildrock_ / wildbush_).
//
//  CHON CHO:
//    - Chi o thuoc dat DA MO (khu da mua + dat khoi dau quanh do), nen CO, khong duong dat / cat / nuoc / ben / da nen.
//    - Uu tien CHO TRONG RONG: quanh o (5x5) phai con >= 'oTrongToiThieu' o trong + da mo -> khong chen sat nha, ruong, chuong.
//    - Khong collider (khong trigger), khong de len chan cay / hoa / bui / da co san, cac vat rai cach nhau.
//    - Vi tri co dinh theo o (hash) -> vao lai game van dung cho cu; vat da don (luu theo ten + vi tri) khong moc lai.
//    - Mua them dat -> lan vao game sau dat moi cung co vat hoang.
//  SPRITE: lay tu Prefab_RocksSmall / Prefab_RockMedium / Prefab_Bush / Prefab_Grass_Clump dang co trong scene
//          (hoac mang sprite trong WorldClearConfig neu Sep keo tay) -> build khong can them asset.
//  TOI UU MOBILE: toi da toiDaDa + toiDaBui vat; moi vat CHI 1 SpriteRenderer (khong collider, particle, audio, script lac).
//          WorldClearable tat Update khi nghi -> ~0 chi phi / frame. Quet 1 lan luc vao scene.
//  Tat: bo tick 'raiVatHoang' trong Resources/WorldClearConfig.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class WildDebrisSpawner
{
    private static readonly string[] TILE_CAM = { "tilemap_isodirt", "tilemap_isosand", "water_tilemap", "tilemap_isodock", "tilemap_isostone", "tilemap_isorock" };
    private static readonly string[] MAU_DA = { "prefab_rockssmall", "prefab_rockmedium" };
    private static readonly string[] MAU_BUI = { "prefab_bush", "prefab_grass_clump" };

    private struct Mau { public Sprite sp; public Material mat; public int layer; public float scale; }

    public static int Rai(WorldClearConfig cfg)
    {
        if (cfg == null || !cfg.raiVatHoang) return 0;
        var lm = LandExpansionManager.Instance;
        if (lm == null || lm.regions == null) return 0;

        // ---- 1. Quet scene 1 lan: tilemap nen + sprite mau + luoi bam decor co san
        Tilemap co = null; var cam = new List<Tilemap>();
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm == null) continue;
            string n = tm.name.ToLowerInvariant();
            if (n == "tilemap_isograss") co = tm;
            else for (int i = 0; i < TILE_CAM.Length; i++) if (n == TILE_CAM[i]) { cam.Add(tm); break; }
        }
        var phu = lm.lockedOverlayTilemap;

        const float O = 200f;
        var bam = new Dictionary<long, List<Bounds>>(512);
        var mauDa = new List<Mau>(); var mauBui = new List<Mau>();
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr == null || sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
            var b = sr.bounds;
            if (b.size.x > 1600f || b.size.y > 1600f) continue;            // nen / anh lon
            ChoVao(bam, b, O);
            string goc = TenGocMau(sr.transform);
            if (goc == null) continue;
            bool laDa = goc.StartsWith("prefab_rock");
            var ds = laDa ? mauDa : mauBui;
            if (ds.Count >= 6) continue;
            bool trung = false;
            for (int i = 0; i < ds.Count; i++) if (ds[i].sp == sr.sprite) { trung = true; break; }
            if (trung) continue;
            float sc = Mathf.Abs(sr.transform.lossyScale.y);
            if (goc.StartsWith("prefab_rockmedium")) sc *= 0.6f;          // da vua -> thu nho thanh da nho
            ds.Add(new Mau { sp = sr.sprite, mat = sr.sharedMaterial, layer = sr.sortingLayerID, scale = sc });
        }
        ThemTuConfig(cfg.spDaNho, mauDa, mauBui.Count > 0 ? mauBui[0] : (Mau?)null);
        ThemTuConfig(cfg.spBui, mauBui, mauDa.Count > 0 ? mauDa[0] : (Mau?)null);
        if (mauDa.Count == 0 && mauBui.Count == 0) return 0;

        // ---- 2. Vung xet: hop bao cac khu DA MO + le 'leDatKhoiDau' o (dat khoi dau ve tay khong thuoc khu nao)
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
        foreach (var r in lm.regions)
        {
            if (r == null || !lm.IsRegionUnlocked(r) || r.cellRects == null) continue;
            foreach (var rc in r.cellRects)
            {
                x0 = Mathf.Min(x0, rc.xMin); y0 = Mathf.Min(y0, rc.yMin);
                x1 = Mathf.Max(x1, rc.xMax); y1 = Mathf.Max(y1, rc.yMax);
            }
        }
        if (x0 > x1) return 0;
        int le = Mathf.Max(0, cfg.leDatKhoiDau);
        x0 -= le; y0 -= le; x1 += le; y1 += le;

        var pm = PlacementManager.Instance;
        var ok = new Dictionary<Vector2Int, bool>(1024);
        System.Func<Vector2Int, bool> oDung = c =>
        {
            bool v;
            if (ok.TryGetValue(c, out v)) return v;
            v = lm.IsCellUnlocked(c) && (pm == null || pm.IsAreaFree(new RectInt(c.x, c.y, 1, 1))) && NenCo(c, co, cam, phu);
            ok[c] = v;
            return v;
        };

        // ---- 3. Ung vien: o dung + du trong xung quanh, xep theo hash (phan bo ngau nhien nhung co dinh)
        float matDo = Mathf.Clamp01(cfg.matDoVatHoang);
        int can = Mathf.Clamp(cfg.oTrongToiThieu, 0, 25);
        var ungVien = new List<KeyValuePair<uint, Vector2Int>>();
        for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
            {
                uint h = Hash(x, y);
                if ((h % 10000u) / 10000f >= matDo) continue;
                var c = new Vector2Int(x, y);
                if (!oDung(c)) continue;
                int trong = 0;
                for (int dx = -2; dx <= 2; dx++)
                    for (int dy = -2; dy <= 2; dy++)
                        if (oDung(new Vector2Int(x + dx, y + dy))) trong++;
                if (trong < can) continue;
                ungVien.Add(new KeyValuePair<uint, Vector2Int>(h, c));
            }
        ungVien.Sort((a, b) => a.Key.CompareTo(b.Key));

        // ---- 4. Dat vat
        var loc = new ContactFilter2D();
        loc.NoFilter(); loc.useTriggers = false;
        var dem = new Collider2D[1];
        var daRai = new List<Vector3>();
        Transform chua = null;
        int soDa = 0, soBui = 0;
        float cach2 = cfg.cachNhau * cfg.cachNhau;

        foreach (var kv in ungVien)
        {
            if (soDa >= cfg.toiDaDa && soBui >= cfg.toiDaBui) break;
            uint h = kv.Key; var c = kv.Value;
            bool laDa = ((h >> 8) % 100u) < (uint)Mathf.Clamp(cfg.phanTramDa, 0, 100);
            if (laDa && (soDa >= cfg.toiDaDa || mauDa.Count == 0)) laDa = false;
            if (!laDa && (soBui >= cfg.toiDaBui || mauBui.Count == 0))
            {
                if (soDa >= cfg.toiDaDa || mauDa.Count == 0) continue;
                laDa = true;
            }

            float jx = ((h >> 12) & 255u) / 255f - 0.5f, jy = ((h >> 20) & 255u) / 255f - 0.5f;
            Vector3 p = IsoGrid.CellFloatToWorld(new Vector2(c.x + jx * 0.45f, c.y + jy * 0.45f));
            p.z = 0f;

            float banKinh = laDa ? 60f : 80f;
            if (Physics2D.OverlapCircle(p, banKinh, loc, dem) > 0) continue;
            if (Cham(bam, p, banKinh, O)) continue;
            bool gan = false;
            for (int i = 0; i < daRai.Count && !gan; i++) if ((daRai[i] - p).sqrMagnitude < cach2) gan = true;
            if (gan) continue;

            var ds = laDa ? mauDa : mauBui;
            var m = ds[(int)((h >> 4) % (uint)ds.Count)];
            if (chua == null) chua = new GameObject("[WildDebris]").transform;
            var go = new GameObject((laDa ? "wildrock_" : "wildbush_") + c.x + "_" + c.y);
            go.transform.SetParent(chua, false);
            go.transform.position = p;
            Vector2 tl = laDa ? cfg.tiLeDa : cfg.tiLeBui;
            float s = m.scale * Mathf.Lerp(tl.x, tl.y, ((h >> 28) & 15u) / 15f);
            go.transform.localScale = new Vector3(((h >> 3) & 1u) == 0 ? s : -s, s, 1f);   // lat trai/phai cho do lap
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = m.sp;
            if (m.mat != null) sr.sharedMaterial = m.mat;
            sr.sortingLayerID = m.layer;
            sr.sortingOrder = 0;
            daRai.Add(p);
            ChoVao(bam, sr.bounds, O);
            if (laDa) soDa++; else soBui++;
        }
        return soDa + soBui;
    }

    // Ten goc mau (prefab_rockssmall...) neu renderer nam trong 1 object mau; null neu khong phai
    private static string TenGocMau(Transform t)
    {
        for (int d = 0; d < 3 && t != null; d++, t = t.parent)
        {
            string n = t.name.ToLowerInvariant();
            for (int i = 0; i < MAU_DA.Length; i++) if (n.StartsWith(MAU_DA[i])) return n;
            for (int i = 0; i < MAU_BUI.Length; i++) if (n.StartsWith(MAU_BUI[i])) return n;
        }
        return null;
    }

    private static void ThemTuConfig(Sprite[] arr, List<Mau> ds, Mau? du)
    {
        if (arr == null || arr.Length == 0) return;
        Mau goc = ds.Count > 0 ? ds[0] : (du ?? new Mau { layer = SortingLayer.NameToID("Objects"), scale = 1f });
        ds.Clear();
        foreach (var sp in arr) if (sp != null) ds.Add(new Mau { sp = sp, mat = goc.mat, layer = goc.layer, scale = goc.scale });
    }

    private static bool NenCo(Vector2Int c, Tilemap co, List<Tilemap> cam, Tilemap phu)
    {
        Vector3 p = IsoGrid.CellCenterToWorld(c);
        if (co != null && !co.HasTile(co.WorldToCell(p))) return false;
        if (phu != null && phu.HasTile(phu.WorldToCell(p))) return false;
        for (int i = 0; i < cam.Count; i++) if (cam[i].HasTile(cam[i].WorldToCell(p))) return false;
        return true;
    }

    private static uint Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ 0x9E3779B9u;
            h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
            return h;
        }
    }

    private static long Khoa(int i, int j) => ((long)i << 32) ^ (uint)j;

    private static void ChoVao(Dictionary<long, List<Bounds>> bam, Bounds b, float O)
    {
        int i0 = Mathf.FloorToInt(b.min.x / O), i1 = Mathf.FloorToInt(b.max.x / O);
        int j0 = Mathf.FloorToInt(b.min.y / O), j1 = Mathf.FloorToInt(b.max.y / O);
        for (int i = i0; i <= i1; i++)
            for (int j = j0; j <= j1; j++)
            {
                long k = Khoa(i, j);
                List<Bounds> ds;
                if (!bam.TryGetValue(k, out ds)) { ds = new List<Bounds>(4); bam[k] = ds; }
                ds.Add(b);
            }
    }

    // Cham phan CHAN (45% duoi) cua decor co san? -> duoc dung truoc tan cay nhung khong de len goc cay / hoa
    private static bool Cham(Dictionary<long, List<Bounds>> bam, Vector3 p, float r, float O)
    {
        int i0 = Mathf.FloorToInt((p.x - r) / O), i1 = Mathf.FloorToInt((p.x + r) / O);
        int j0 = Mathf.FloorToInt((p.y - r) / O), j1 = Mathf.FloorToInt((p.y + r) / O);
        for (int i = i0; i <= i1; i++)
            for (int j = j0; j <= j1; j++)
            {
                List<Bounds> ds;
                if (!bam.TryGetValue(Khoa(i, j), out ds)) continue;
                for (int k = 0; k < ds.Count; k++)
                {
                    var b = ds[k];
                    float yTren = b.min.y + b.size.y * 0.45f;
                    if (p.x > b.min.x - r && p.x < b.max.x + r && p.y > b.min.y - r * 0.6f && p.y < yTren + r * 0.6f) return true;
                }
            }
        return false;
    }
}
