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
    private static Sprite _glow, _circle, _sparkle;

    public static Sprite GlowSprite    => _glow    != null ? _glow    : (_glow    = TaoSprite(VeGlow(128), "SoftFx_Glow"));
    public static Sprite CircleSprite  => _circle  != null ? _circle  : (_circle  = TaoSprite(VeCircle(64), "SoftFx_Circle"));
    public static Sprite SparkleSprite => _sparkle != null ? _sparkle : (_sparkle = TaoSprite(VeSparkle(64), "SoftFx_Sparkle"));

    private static Sprite TaoSprite(Texture2D tex, string ten)
    {
        var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        s.name = ten;
        return s;
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
