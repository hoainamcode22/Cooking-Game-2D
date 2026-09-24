// ============================================================================
//  SoftFxSprites — bo sprite mem (ve bang code, cache static) dung chung cho VFX dieu nhe:
//    GlowSprite    : quang sang tron mo dan ra mep (hao quang, suong mu, anh sang nen)
//    CircleSprite  : hat tron mem vien mo nhe (bui lap lanh, bong bong nho)
//    SparkleSprite : ngoi sao 4 canh mem (lap lanh)
//  Khong can file anh. Tool Editor co the ghi ra PNG (SoftFxSprites.VePNG) de xem truoc o Edit mode.
// ============================================================================
using UnityEngine;

public static class SoftFxSprites
{
    private static Sprite _glow, _circle, _sparkle, _ring, _band, _rays, _buom, _chim;

    public static Sprite GlowSprite    => _glow    != null ? _glow    : (_glow    = TaoSprite(VeGlow(128), "SoftFx_Glow"));
    public static Sprite CircleSprite  => _circle  != null ? _circle  : (_circle  = TaoSprite(VeCircle(64), "SoftFx_Circle"));
    public static Sprite SparkleSprite => _sparkle != null ? _sparkle : (_sparkle = TaoSprite(VeSparkle(64), "SoftFx_Sparkle"));
    /// <summary>Vong tron mong (gon song nuoc).</summary>
    public static Sprite RingSprite    => _ring    != null ? _ring    : (_ring    = TaoSprite(VeRing(96), "SoftFx_Ring"));
    /// <summary>Dai sang doc mo 2 mep (anh sang luot qua nut). Pivot giua.</summary>
    public static Sprite BandSprite    => _band    != null ? _band    : (_band    = TaoSpriteRect(VeBand(64, 16), "SoftFx_Band"));
    /// <summary>Tia nang toa tron (sau huy hieu len cap).</summary>
    public static Sprite RaysSprite    => _rays    != null ? _rays    : (_rays    = TaoSprite(VeRays(256, 12), "SoftFx_Rays"));
    /// <summary>Con buom trang (to mau bang SpriteRenderer.color). Canh vo bang scale X.</summary>
    public static Sprite ButterflySprite => _buom  != null ? _buom    : (_buom    = TaoSprite(VeButterfly(64), "SoftFx_Butterfly"));
    /// <summary>Chim bay bong den (hinh chu "m" mem). Vo canh bang scale Y.</summary>
    public static Sprite BirdSprite    => _chim    != null ? _chim    : (_chim    = TaoSprite(VeBird(64), "SoftFx_Bird"));

    private static Sprite TaoSprite(Texture2D tex, string ten)
    {
        var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        s.name = ten;
        return s;
    }

    private static Sprite TaoSpriteRect(Texture2D tex, string ten)
    {
        var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        s.name = ten;
        return s;
    }

    private static Texture2D TaoTex(int w, int h, string ten)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.name = ten;
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.hideFlags = HideFlags.DontSave;
        return t;
    }

    private static float Muot(float a) { a = Mathf.Clamp01(a); return a * a * (3f - 2f * a); }

    public static Texture2D VeRing(int n)
    {
        var t = TaoTex(n, "SoftFx_Ring");
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;   // 0..1
                float a = Muot(1f - Mathf.Abs(d - 0.86f) / 0.1f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px); t.Apply(false, false);
        return t;
    }

    public static Texture2D VeBand(int w, int h)
    {
        var t = TaoTex(w, h, "SoftFx_Band");
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs(x / (float)(w - 1) - 0.5f) * 2f;   // 0 giua .. 1 mep
                float a = Muot(1f - u);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        t.SetPixels32(px); t.Apply(false, false);
        return t;
    }

    public static Texture2D VeRays(int n, int soTia)
    {
        var t = TaoTex(n, "SoftFx_Rays");
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / c;
                float g = Mathf.Atan2(dy, dx);
                float tia = Mathf.Pow(Mathf.Abs(Mathf.Cos(g * soTia * 0.5f)), 3f);
                float xa = Muot(1f - d) * Muot(d / 0.12f);           // mo o tam va mep
                float a = tia * xa;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
        t.SetPixels32(px); t.Apply(false, false);
        return t;
    }

    public static Texture2D VeButterfly(int n)
    {
        var t = TaoTex(n, "SoftFx_Butterfly");
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x - c) / c, v = (y - c) / c;
                float au = Mathf.Abs(u);
                // canh tren (to) + canh duoi (nho), hai ben than
                float e1 = ((au - 0.48f) * (au - 0.48f)) / 0.2f + ((v - 0.22f) * (v - 0.22f)) / 0.2f;
                float e2 = ((au - 0.38f) * (au - 0.38f)) / 0.09f + ((v + 0.38f) * (v + 0.38f)) / 0.07f;
                float canh = Mathf.Max(Muot((1f - e1) / 0.15f), Muot((1f - e2) / 0.15f));
                float than = Muot((0.07f - au) / 0.03f) * Muot((0.75f - Mathf.Abs(v)) / 0.1f);
                float a = Mathf.Max(canh, than);
                byte sang = (byte)(than > canh ? 90 : 255);                   // than toi hon
                px[y * n + x] = new Color32(sang, sang, sang, (byte)(a * 255f));
            }
        t.SetPixels32(px); t.Apply(false, false);
        return t;
    }

    public static Texture2D VeBird(int n)
    {
        var t = TaoTex(n, "SoftFx_Bird");
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x - c) / c, v = (y - c) / c;
                float au = Mathf.Abs(u);
                // duong cong canh: v = 0.35*sin(pi*au) - 0.15 (hinh chu m), day giam dan ra dau canh
                float duong = 0.32f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, au)) - 0.12f;
                float day = Mathf.Lerp(0.14f, 0.03f, au);
                float a = Muot((day - Mathf.Abs(v - duong)) / 0.04f) * Muot((0.95f - au) / 0.08f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px); t.Apply(false, false);
        return t;
    }

    private static Texture2D TaoTex(int n, string ten)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.name = ten;
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.hideFlags = HideFlags.DontSave;
        return t;
    }

    /// <summary>Quang sang: trang, alpha giam muot tu tam ra mep (smoothstep binh phuong).</summary>
    public static Texture2D VeGlow(int n)
    {
        var t = TaoTex(n, "SoftFx_Glow");
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                a *= a;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px);
        t.Apply(false, false);
        return t;
    }

    /// <summary>Hat tron: dac o giua, mep mo 30%.</summary>
    public static Texture2D VeCircle(int n)
    {
        var t = TaoTex(n, "SoftFx_Circle");
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01((1f - d) / 0.3f);
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px);
        t.Apply(false, false);
        return t;
    }

    /// <summary>Sao 4 canh mem + loi sang tron.</summary>
    public static Texture2D VeSparkle(int n)
    {
        var t = TaoTex(n, "SoftFx_Sparkle");
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x - c) / c, dy = Mathf.Abs(y - c) / c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float canh = Mathf.Clamp01(1f - (dx * dy) * 18f - r * 0.9f);   // chu thap thon nhon
                float loi  = Mathf.Clamp01(1f - r / 0.35f);
                float a = Mathf.Clamp01(Mathf.Max(canh, loi * loi));
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px);
        t.Apply(false, false);
        return t;
    }
}
