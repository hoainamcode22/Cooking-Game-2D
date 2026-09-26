// ============================================================================
//  WorldClearFX — VFX lam bang code cho CHAT CAY / CAT BUI / DAP DA / DON LO DAT (2026-09-25)
//  - 1 runner dung chung, pool 180 hat SpriteRenderer (khong ParticleSystem, khong prefab, khong art moi).
//  - Sprite ve bang code: vun go, la, cong co, manh da, bui, khoi, tia lua.
//  - Kieu bay: VANG (roi theo trong luc, xoay), LA (roi cham + dung dua), BAY LEN (bui/khoi phong to, mo dan).
//  - Ngoai camera -> khong phat (tiet kiem cho WebGL mobile).
//  k = he so kich thuoc: 1 = vat cao ~100 unit world (1 o dat = 300 x 150).
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

public class WorldClearFX : MonoBehaviour
{
    public enum Hat { VunGo, La, Co, ManhDa, Bui, Khoi, TiaLua }
    private enum Kieu { Vang, La, BayLen }

    private const int POOL = 180;

    private class P
    {
        public SpriteRenderer sr;
        public Vector3 v;
        public float g, quay, song, t, co0, co1, pha;
        public Kieu kieu;
        public Color mau;
        public bool dung;
    }

    private static WorldClearFX _inst;
    private readonly List<P> _ds = new List<P>(POOL);
    private int _ke;
    private static readonly Dictionary<Hat, Sprite> _sp = new Dictionary<Hat, Sprite>();
    private static Material _mat;

    private static WorldClearFX I
    {
        get
        {
            if (_inst != null) return _inst;
            var go = new GameObject("[WorldClearFX]");
            _inst = go.AddComponent<WorldClearFX>();
            return _inst;
        }
    }

    // =====================================================================
    //  API
    // =====================================================================

    /// <summary>1 nhat cuoc / riu / bua trung vat: vun theo loai + bui nho.</summary>
    public static void TrungDon(WorldClearKind kind, Vector3 p, float k, Vector2 huong, int layer, int order)
    {
        if (!TrongCamera(p, 200f * k)) return;
        switch (kind)
        {
            case WorldClearKind.Riu:
                Phun(Hat.VunGo, p, k, 5, huong, 55f, layer, order, new Color(0.78f, 0.55f, 0.30f));
                Phun(Hat.La, p + Vector3.up * 40f * k, k, 2, Vector2.up, 90f, layer, order, new Color(0.40f, 0.68f, 0.24f));
                Phun(Hat.Bui, p, k, 1, Vector2.up, 30f, layer, order - 1, new Color(0.86f, 0.78f, 0.64f));
                break;
            case WorldClearKind.Keo:
                Phun(Hat.La, p, k, 4, huong, 70f, layer, order, new Color(0.36f, 0.66f, 0.22f));
                Phun(Hat.Co, p, k, 3, huong, 60f, layer, order, new Color(0.52f, 0.78f, 0.30f));
                break;
            case WorldClearKind.Bua:
                Phun(Hat.ManhDa, p, k, 5, huong, 65f, layer, order, new Color(0.62f, 0.60f, 0.58f));
                Phun(Hat.TiaLua, p, k, 4, huong, 80f, layer, order + 1, new Color(1f, 0.88f, 0.40f));
                Phun(Hat.Bui, p, k, 2, Vector2.up, 40f, layer, order - 1, new Color(0.80f, 0.78f, 0.74f));
                break;
        }
    }

    /// <summary>Vat bien mat: bung lon (bui day + vun nhieu).</summary>
    public static void BungKetThuc(WorldClearKind kind, Vector3 chan, float k, int layer, int order)
    {
        if (!TrongCamera(chan, 300f * k)) return;
        Phun(Hat.Bui, chan, k * 1.4f, 6, Vector2.up, 80f, layer, order, new Color(0.86f, 0.78f, 0.64f));
        switch (kind)
        {
            case WorldClearKind.Riu:
                Phun(Hat.VunGo, chan + Vector3.up * 20f * k, k, 10, Vector2.up, 70f, layer, order + 1, new Color(0.74f, 0.50f, 0.27f));
                Phun(Hat.La, chan + Vector3.up * 90f * k, k, 10, Vector2.up, 120f, layer, order + 1, new Color(0.38f, 0.66f, 0.22f));
                break;
            case WorldClearKind.Keo:
                Phun(Hat.La, chan + Vector3.up * 30f * k, k, 12, Vector2.up, 110f, layer, order + 1, new Color(0.36f, 0.66f, 0.22f));
                Phun(Hat.Co, chan, k, 8, Vector2.up, 90f, layer, order + 1, new Color(0.52f, 0.78f, 0.30f));
                break;
            case WorldClearKind.Bua:
                Phun(Hat.ManhDa, chan + Vector3.up * 20f * k, k * 1.2f, 12, Vector2.up, 100f, layer, order + 1, new Color(0.60f, 0.58f, 0.56f));
                Phun(Hat.Bui, chan, k * 1.6f, 4, Vector2.up, 60f, layer, order, new Color(0.78f, 0.76f, 0.72f));
                break;
        }
    }

    /// <summary>Don lo dat: tron bui + khoi + da + la + go bay tung.</summary>
    public static void DonDat(Vector3 p, float k, int layer, int order)
    {
        if (!TrongCamera(p, 250f * k)) return;
        Phun(Hat.Bui, p, k, 2, Vector2.up, 70f, layer, order, new Color(0.86f, 0.77f, 0.62f));
        Phun(Hat.ManhDa, p, k, 2, Vector2.up, 110f, layer, order + 1, new Color(0.62f, 0.60f, 0.57f));
        if (Random.value < 0.8f) Phun(Hat.La, p, k, 2, Vector2.up, 120f, layer, order + 1, new Color(0.40f, 0.68f, 0.24f));
        if (Random.value < 0.7f) Phun(Hat.VunGo, p, k, 2, Vector2.up, 110f, layer, order + 1, new Color(0.74f, 0.52f, 0.28f));
    }

    public static void Khoi(Vector3 p, float k, int layer, int order)
    {
        if (!TrongCamera(p, 250f * k)) return;
        Phun(Hat.Khoi, p, k, 1, Vector2.up, 20f, layer, order, new Color(0.80f, 0.78f, 0.76f));
    }

    // =====================================================================
    //  Loi
    // =====================================================================
    private static void Phun(Hat hat, Vector3 p, float k, int n, Vector2 huong, float xoe, int layer, int order, Color mau)
    {
        var me = I;
        var sp = LaySprite(hat);
        Vector2 h = huong.sqrMagnitude < 0.001f ? Vector2.up : huong.normalized;
        float goc0 = Mathf.Atan2(h.y, h.x) * Mathf.Rad2Deg;
        for (int i = 0; i < n; i++)
        {
            var q = me.LayHat();
            float goc = (goc0 + Random.Range(-xoe, xoe)) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(goc), Mathf.Sin(goc), 0f);
            float tocDo, co;
            switch (hat)
            {
                case Hat.Bui:
                    q.kieu = Kieu.BayLen; tocDo = Random.Range(25f, 55f) * k; q.g = 0f; q.song = Random.Range(0.7f, 1.1f);
                    co = Random.Range(38f, 60f) * k; q.co0 = co * 0.5f; q.co1 = co * 1.5f; break;
                case Hat.Khoi:
                    q.kieu = Kieu.BayLen; tocDo = Random.Range(18f, 30f) * k; q.g = 0f; q.song = Random.Range(1.4f, 2.0f);
                    co = Random.Range(60f, 85f) * k; q.co0 = co * 0.5f; q.co1 = co * 1.8f; dir = new Vector3(Random.Range(-0.3f, 0.3f), 1f, 0f); break;
                case Hat.La:
                    q.kieu = Kieu.La; tocDo = Random.Range(90f, 170f) * k; q.g = 160f * k; q.song = Random.Range(1.2f, 1.8f);
                    co = Random.Range(12f, 18f) * k; q.co0 = co; q.co1 = co * 0.8f; break;
                case Hat.TiaLua:
                    q.kieu = Kieu.Vang; tocDo = Random.Range(160f, 260f) * k; q.g = 380f * k; q.song = Random.Range(0.25f, 0.4f);
                    co = Random.Range(8f, 12f) * k; q.co0 = co; q.co1 = co * 0.3f; break;
                default: // VunGo, Co, ManhDa
                    q.kieu = Kieu.Vang; tocDo = Random.Range(140f, 240f) * k; q.g = 520f * k; q.song = Random.Range(0.6f, 0.9f);
                    co = (hat == Hat.ManhDa ? Random.Range(10f, 16f) : Random.Range(10f, 15f)) * k; q.co0 = co; q.co1 = co * 0.85f; break;
            }
            q.v = dir * tocDo;
            q.quay = hat == Hat.Bui || hat == Hat.Khoi ? Random.Range(-30f, 30f) : Random.Range(-540f, 540f);
            q.t = 0f;
            q.pha = Random.value * 6.28f;
            float sang = Random.Range(0.88f, 1.08f);
            q.mau = new Color(Mathf.Clamp01(mau.r * sang), Mathf.Clamp01(mau.g * sang), Mathf.Clamp01(mau.b * sang), 1f);
            q.dung = true;
            var sr = q.sr;
            sr.sprite = sp;
            sr.sortingLayerID = layer;
            sr.sortingOrder = Mathf.Clamp(order, -32000, 32000);
            sr.color = q.mau;
            sr.transform.position = new Vector3(p.x + Random.Range(-6f, 6f) * k, p.y + Random.Range(-4f, 4f) * k, p.z);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            sr.transform.localScale = Vector3.one * q.co0;
            sr.enabled = true;
        }
    }

    private P LayHat()
    {
        if (_ds.Count < POOL)
        {
            var go = new GameObject("hat");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (_mat == null) { var sh = Shader.Find("Sprites/Default"); if (sh != null) _mat = new Material(sh); }
            if (_mat != null) sr.sharedMaterial = _mat;
            var q = new P { sr = sr };
            _ds.Add(q);
            return q;
        }
        // Het pool: tai dung hat cu nhat (vong tron)
        _ke = (_ke + 1) % _ds.Count;
        return _ds[_ke];
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        for (int i = 0; i < _ds.Count; i++)
        {
            var q = _ds[i];
            if (!q.dung) continue;
            q.t += dt;
            float u = q.t / Mathf.Max(0.05f, q.song);
            if (u >= 1f) { q.dung = false; q.sr.enabled = false; continue; }
            var tr = q.sr.transform;
            Vector3 pos = tr.position;
            switch (q.kieu)
            {
                case Kieu.Vang:
                    q.v.y -= q.g * dt;
                    q.v *= 1f - 0.6f * dt;
                    break;
                case Kieu.La:
                    q.v.y -= q.g * dt;
                    q.v *= 1f - 2.4f * dt;                                        // can gio manh -> roi cham
                    pos.x += Mathf.Sin(q.t * 7f + q.pha) * 40f * dt * (q.co0 / 14f); // dung dua
                    break;
                case Kieu.BayLen:
                    q.v *= 1f - 1.6f * dt;
                    break;
            }
            pos += q.v * dt;
            tr.position = pos;
            tr.rotation = Quaternion.Euler(0f, 0f, tr.eulerAngles.z + q.quay * dt);
            float co = Mathf.Lerp(q.co0, q.co1, q.kieu == Kieu.BayLen ? 1f - (1f - u) * (1f - u) : u);
            if (q.kieu == Kieu.La) tr.localScale = new Vector3(co * (0.35f + 0.65f * Mathf.Abs(Mathf.Cos(q.t * 6f + q.pha))), co, 1f);   // la lat
            else tr.localScale = new Vector3(co, co, 1f);
            float a;
            if (q.kieu == Kieu.BayLen) a = (u < 0.2f ? u / 0.2f : 1f - (u - 0.2f) / 0.8f) * 0.6f;
            else a = u < 0.7f ? 1f : 1f - (u - 0.7f) / 0.3f;
            var c = q.mau; c.a = a; q.sr.color = c;
        }
    }

    public static bool TrongCamera(Vector3 p, float le)
    {
        var cam = Camera.main;
        if (cam == null) return true;
        if (cam.orthographic)
        {
            float h = cam.orthographicSize + le, w = cam.orthographicSize * cam.aspect + le;
            Vector3 c = cam.transform.position;
            return Mathf.Abs(p.x - c.x) <= w && Mathf.Abs(p.y - c.y) <= h;
        }
        Vector3 v = cam.WorldToViewportPoint(p);
        return v.z > 0f && v.x > -0.2f && v.x < 1.2f && v.y > -0.2f && v.y < 1.2f;
    }

    // =====================================================================
    //  Sprite ve bang code (PPU = kich thuoc -> 1 sprite = 1 unit, scale = co that)
    // =====================================================================
    private static Sprite LaySprite(Hat h)
    {
        Sprite s;
        if (_sp.TryGetValue(h, out s) && s != null) return s;
        switch (h)
        {
            case Hat.VunGo: s = Ve(24, 12, (x, y) => HopBoGoc(x, y, 24, 12, 4f) * (0.85f + 0.15f * Mathf.Sin(y * 1.6f))); break;
            case Hat.La:    s = Ve(24, 14, (x, y) => La(x, y)); break;
            case Hat.Co:    s = Ve(8, 22, (x, y) => Co(x, y)); break;
            case Hat.ManhDa: s = Ve(20, 20, (x, y) => ManhDa(x, y)); break;
            case Hat.TiaLua: s = Ve(16, 16, (x, y) => Sao(x, y)); break;
            default:        s = Ve(48, 48, (x, y) => Tron(x, y, 48)); break;   // Bui, Khoi
        }
        _sp[h] = s;
        return s;
    }

    private delegate float HamAlpha(int x, int y);

    private static Sprite Ve(int w, int h, HamAlpha f)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = Mathf.Clamp01(f(x, y));
                // vien toi nhe o mep (cho hat noi tren nen dat sang)
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Mathf.Max(w, h));
    }

    private static float HopBoGoc(int x, int y, int w, int h, float r)
    {
        float cx = Mathf.Clamp(x + 0.5f, r, w - r), cy = Mathf.Clamp(y + 0.5f, r, h - r);
        float d = r - Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
        return d + 0.5f;
    }

    private static float La(int x, int y)
    {
        float u = (x + 0.5f) / 24f * 2f - 1f, v = (y + 0.5f) / 14f * 2f - 1f;
        float r = u * u + (v * v) / (0.35f + 0.65f * (1f - u * u));
        float a = (1f - r) * 6f;
        if (Mathf.Abs(v) < 0.08f && u > -0.8f) a *= 0.7f;          // gan la
        return a;
    }

    private static float Co(int x, int y)
    {
        float t = (y + 0.5f) / 22f;
        float giua = 4f + Mathf.Sin(t * 2.2f) * 1.6f;
        float rong = 2.4f * (1f - t) + 0.4f;
        return (rong - Mathf.Abs(x + 0.5f - giua)) * 1.5f;
    }

    private static float ManhDa(int x, int y)
    {
        // da giac 6 canh meo
        float u = x + 0.5f - 10f, v = y + 0.5f - 10f;
        float goc = Mathf.Atan2(v, u);
        float r = 8.5f + 1.6f * Mathf.Sin(goc * 3f + 0.7f) + 0.9f * Mathf.Sin(goc * 5f);
        return (r - Mathf.Sqrt(u * u + v * v)) * 1.2f;
    }

    private static float Sao(int x, int y)
    {
        float u = Mathf.Abs(x + 0.5f - 8f), v = Mathf.Abs(y + 0.5f - 8f);
        float a = 1f - (u * v) / 5f - Mathf.Sqrt(u * u + v * v) / 8f;
        return a * 2f;
    }

    private static float Tron(int x, int y, int n)
    {
        float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
        float d = Mathf.Sqrt(u * u + v * v);
        float a = 1f - d;
        return a * a * 1.4f;
    }

    // =====================================================================
    //  Sprite bo goc cho thanh tien trinh / nut (dung chung)
    // =====================================================================
    private static readonly Dictionary<long, Sprite> _boGoc = new Dictionary<long, Sprite>();

    /// <summary>Sprite bo goc trang (to mau bang SpriteRenderer.color / Image.color). 9-slice theo ban kinh.
    /// ppu: world -> ban kinh goc that = r / ppu unit (SpriteRenderer Sliced); UI Image thi ppu 100.</summary>
    public static Sprite BoGocTrang(int n, int r, float ppu = 100f)
    {
        long key = n * 1000000L + r * 1000L + Mathf.RoundToInt(ppu * 10f);
        Sprite s;
        if (_boGoc.TryGetValue(key, out s) && s != null) return s;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(HopBoGoc(x, y, n, n, r)));
        tex.SetPixels(px);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), Mathf.Max(0.01f, ppu), 0, SpriteMeshType.FullRect,
            new Vector4(r + 1, r + 1, r + 1, r + 1));
        _boGoc[key] = s;
        return s;
    }
}
