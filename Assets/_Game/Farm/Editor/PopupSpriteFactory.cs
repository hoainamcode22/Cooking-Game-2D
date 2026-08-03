using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sinh SPRITE THỦ TỤC cho popup lên cấp (phong cách Township).
/// Không cần art từ ngoài — mọi hình đều vẽ bằng SDF rồi xuất PNG.
///
/// Bảng màu lấy trực tiếp bằng cách lấy mẫu pixel từ video Township tham chiếu,
/// đã giảm bão hoà nhẹ vì video nén làm màu bị đẩy quá gắt.
///
/// Sprite được ghi ra: Assets/_Game/Farm/Art/UI_LevelUp/
/// </summary>
public static class PopupSpriteFactory
{
    public const string ArtFolder = "Assets/_Game/Farm/Art/UI_LevelUp";

    // ── BẢNG MÀU (lấy mẫu từ video Township) ────────────────────────────
    public static readonly Color BannerTop    = Hex("#5FB9FF"); // mép trên sáng
    public static readonly Color BannerMid    = Hex("#2E9BF5"); // thân
    public static readonly Color BannerBot    = Hex("#1C7FD8"); // mép dưới đậm
    public static readonly Color BannerTail   = Hex("#1668B0"); // đuôi cờ gập
    public static readonly Color BannerEdge   = Hex("#125B99"); // viền ngoài

    public static readonly Color StarWhite    = Hex("#FFFFFF");
    public static readonly Color StarRing     = Hex("#1E9CFC");
    public static readonly Color StarInnerTop = Hex("#7FDCFF");
    public static readonly Color StarInnerBot = Hex("#2CB6F2");

    public static readonly Color GlowWarm     = Hex("#FFD24A");

    public static readonly Color RingCream    = Hex("#F7EBD2");
    public static readonly Color RingCreamDk  = Hex("#D9C49C");
    public static readonly Color SlotFill     = Hex("#FFFFFF");

    public static readonly Color TagRedTop    = Hex("#F0463A");
    public static readonly Color TagRedBot    = Hex("#C41F18");

    public static readonly Color BtnGreenTop  = Hex("#6DD62A");
    public static readonly Color BtnGreenBot  = Hex("#3E9A14");
    public static readonly Color BtnGreenEdge = Hex("#2A6E0A");

    public static readonly Color BandDark     = new Color(0f, 0f, 0f, 0.55f);

    public static Color Hex(string hex)
        => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

    // ────────────────────────────────────────────────────────────────────
    // API CHÍNH
    // ────────────────────────────────────────────────────────────────────

    /// <summary>Sinh toàn bộ sprite. Trả về true nếu thành công.</summary>
    public static bool GenerateAll(bool force = false)
    {
        EnsureFolder();

        Gen("spr_banner_body",  160, 112, BannerBody,      new Vector4(46, 26, 46, 30), force);
        Gen("spr_banner_tail",   64,  96, BannerTailShape, Vector4.zero,                force);
        Gen("spr_star",         256, 256, StarShape,       Vector4.zero,                force);
        Gen("spr_glow_radial",  256, 256, RadialGlow,      Vector4.zero,                force);
        Gen("spr_ring_circle",  192, 192, RingCircle,      Vector4.zero,                force);
        Gen("spr_circle_fill",  160, 160, CircleFill,      Vector4.zero,                force);
        Gen("spr_new_tag",      112,  56, NewTag,          new Vector4(18, 16, 18, 16), force);
        Gen("spr_btn_green",    144,  80, ButtonGreen,     new Vector4(34, 26, 34, 30), force);
        Gen("spr_band_dark",     32,  32, SolidBand,       new Vector4(4, 4, 4, 4),     force);
        Gen("spr_soft_shadow",  128, 128, SoftShadow,      Vector4.zero,                force);
        Gen("spr_white_round",   48,  48, WhiteRounded,    new Vector4(16, 16, 16, 16), force);

        AssetDatabase.Refresh();
        return true;
    }

    public static Sprite Load(string name)
        => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");

    // ────────────────────────────────────────────────────────────────────
    // HẠ TẦNG
    // ────────────────────────────────────────────────────────────────────

    private static void EnsureFolder()
    {
        string abs = Abs(ArtFolder);
        if (!Directory.Exists(abs))
        {
            Directory.CreateDirectory(abs);
            AssetDatabase.Refresh();   // để AssetDatabase nhận biết thư mục mới
        }
    }

    /// <summary>Đổi đường dẫn kiểu "Assets/..." sang đường dẫn tuyệt đối trên đĩa.</summary>
    private static string Abs(string assetPath)
        => Path.Combine(Directory.GetCurrentDirectory(), assetPath);

    private delegate Color PixelFn(float u, float v, int w, int h);

    private static void Gen(string name, int w, int h, PixelFn fn, Vector4 border, bool force)
    {
        string path = $"{ArtFolder}/{name}.png";

        if (force || !File.Exists(Abs(path)))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px  = new Color[w * h];

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // u,v ∈ [-1,1], gốc ở tâm
                float u = (x + 0.5f) / w * 2f - 1f;
                float v = (y + 0.5f) / h * 2f - 1f;
                px[y * w + x] = fn(u, v, w, h);
            }

            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(Abs(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        // LUÔN áp lại import settings — kể cả khi file đã có sẵn.
        // Nếu lần import đầu lỡ hỏng (textureType = Default) thì Load<Sprite>()
        // sẽ trả null vĩnh viễn và popup ra toàn ô trắng.
        ApplyImportSettings(path, border);
    }

    private static void ApplyImportSettings(string path, Vector4 border)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        bool dirty = imp.textureType != TextureImporterType.Sprite
                  || imp.spriteBorder != border;

        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled       = false;
        imp.filterMode          = FilterMode.Bilinear;
        imp.wrapMode            = TextureWrapMode.Clamp;
        imp.spriteBorder        = border;

        if (dirty) imp.SaveAndReimport();
    }

    // ── HÀM SDF DÙNG CHUNG ───────────────────────────────────────────────

    /// <summary>SDF hình chữ nhật bo góc. p tính theo đơn vị pixel, b = nửa kích thước.</summary>
    private static float SdRoundBox(Vector2 p, Vector2 b, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    /// <summary>SDF đa giác (thuật toán Inigo Quilez) — dùng cho ngôi sao.</summary>
    private static float SdPolygon(Vector2[] poly, Vector2 p)
    {
        int n = poly.Length;
        float d = Vector2.Dot(p - poly[0], p - poly[0]);
        float s = 1f;

        for (int i = 0, j = n - 1; i < n; j = i, i++)
        {
            Vector2 e = poly[j] - poly[i];
            Vector2 w = p - poly[i];
            float   t = Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            Vector2 b = w - e * t;
            d = Mathf.Min(d, Vector2.Dot(b, b));

            bool c1 = p.y >= poly[i].y, c2 = p.y < poly[j].y, c3 = e.x * w.y > e.y * w.x;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
        }
        return s * Mathf.Sqrt(d);
    }

    /// <summary>Chuyển signed distance thành alpha có khử răng cưa.</summary>
    private static float Aa(float sd, float softness = 1.4f)
        => Mathf.Clamp01(0.5f - sd / softness);

    /// <summary>
    /// smoothstep kiểu GLSL: 0 khi x ở phía e0, 1 khi x tới e1, mượt ở giữa.
    /// CẢNH BÁO: Unity `Mathf.SmoothStep(from, to, t)` KHÔNG phải hàm này —
    /// nó nội suy giá trị từ `from` tới `to`, dùng nhầm sẽ ra cường độ sai hẳn.
    /// </summary>
    private static float SStep(float e0, float e1, float x)
    {
        float k = Mathf.Clamp01((x - e0) / (e1 - e0));
        return k * k * (3f - 2f * k);
    }

    private static Color Over(Color src, Color dst)
    {
        float a = src.a + dst.a * (1f - src.a);
        if (a <= 0.0001f) return Color.clear;
        Vector3 rgb = (new Vector3(src.r, src.g, src.b) * src.a
                     + new Vector3(dst.r, dst.g, dst.b) * dst.a * (1f - src.a)) / a;
        return new Color(rgb.x, rgb.y, rgb.z, a);
    }

    private static Color WithA(Color c, float a) { c.a = a; return c; }

    // ────────────────────────────────────────────────────────────────────
    // CÁC HÌNH
    // ────────────────────────────────────────────────────────────────────

    /// <summary>Thân băng rôn: bo góc, gradient dọc, dải sáng ở mép trên, viền ngoài.</summary>
    private static Color BannerBody(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd    = SdRoundBox(p, b, 26f);
        float aOut  = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn   = Aa(sd + 4f);          // phần lõi bên trong viền
        float t     = Mathf.InverseLerp(1f, -1f, v);   // 0 = trên, 1 = dưới

        Color body  = t < 0.5f
            ? Color.Lerp(BannerTop, BannerMid, t / 0.5f)
            : Color.Lerp(BannerMid, BannerBot, (t - 0.5f) / 0.5f);

        // Dải sáng bóng dồn vào 24% trên cùng
        float gloss = SStep(0.24f, 0f, t) * 0.30f;
        body = Color.Lerp(body, Color.white, gloss);

        Color edge = BannerEdge;
        Color final = Color.Lerp(edge, body, aIn);
        return WithA(final, aOut);
    }

    /// <summary>Đuôi cờ gập: hình thang có khấc chữ V, màu đậm hơn thân.</summary>
    private static Color BannerTailShape(float u, float v, int w, int h)
    {
        float x = (u * 0.5f + 0.5f) * w;   // 0..w  (0 = phía gắn thân)
        float y = (v * 0.5f + 0.5f) * h;

        // Cạnh trên/dưới xiên vào: càng ra xa thân càng hẹp
        float tt   = x / w;
        float half = Mathf.Lerp(h * 0.5f, h * 0.34f, tt);
        float dy   = Mathf.Abs(y - h * 0.5f);

        float aBody = Mathf.Clamp01((half - dy) / 1.5f);

        // Khấc chữ V ở đầu ngoài
        float notchDepth = w * 0.30f;
        float vx = w - notchDepth;
        if (x > vx)
        {
            float k = (x - vx) / notchDepth;      // 0..1
            float need = Mathf.Lerp(0f, half, k); // càng ra ngoài, khấc càng rộng
            aBody *= Mathf.Clamp01((dy - need) / 1.5f + 0.0f);
        }

        if (aBody <= 0.001f) return Color.clear;

        float shade = Mathf.InverseLerp(1f, -1f, v);
        Color c = Color.Lerp(BannerTail, BannerEdge, shade * 0.6f);
        return WithA(c, aBody);
    }

    /// <summary>Ngôi sao 5 cánh bo tròn, 3 lớp: trắng ngoài → xanh → lõi cyan gradient.</summary>
    private static Color StarShape(float u, float v, int w, int h)
    {
        const int POINTS = 5;
        // Phép "- 0.14f" bên dưới NỞ biên ra ngoài 0.14 đơn vị để bo góc.
        // Nên bán kính đa giác phải là 0.78 (0.78 + 0.14 = 0.92 < 1.0),
        // nếu để 0.92 thì đỉnh sao chạm 1.06 → bị texture cắt phẳng mất chóp.
        float R  = 0.78f;   // bán kính đỉnh
        float r  = 0.39f;   // bán kính lõm

        var poly = new Vector2[POINTS * 2];
        for (int i = 0; i < POINTS * 2; i++)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / POINTS;   // đỉnh hướng lên
            float rad = (i % 2 == 0) ? R : r;
            poly[i] = new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad);
        }

        Vector2 p = new Vector2(u, v);
        float sd  = SdPolygon(poly, p) - 0.14f;   // trừ đi = bo tròn góc

        float unit = 2f / w;            // 1 pixel theo đơn vị uv
        float soft = unit * 1.6f;

        float aOuter = Mathf.Clamp01(0.5f - sd / soft);
        if (aOuter <= 0.001f) return Color.clear;

        float bandWhite = unit * 15f;   // độ dày viền trắng (~7% đường kính, khớp Township)
        float bandRing  = unit * 12f;   // độ dày vòng xanh

        float aRing  = Mathf.Clamp01(0.5f - (sd + bandWhite) / soft);
        float aInner = Mathf.Clamp01(0.5f - (sd + bandWhite + bandRing) / soft);

        // Lõi: gradient dọc sáng trên → đậm dưới
        float t = Mathf.InverseLerp(0.8f, -0.8f, v);
        Color inner = Color.Lerp(StarInnerTop, StarInnerBot, t);

        Color c = StarWhite;
        c = Color.Lerp(c, StarRing, aRing);
        c = Color.Lerp(c, inner,    aInner);

        return WithA(c, aOuter);
    }

    /// <summary>Quầng sáng ấm toả tròn — đặt sau ngôi sao. Dùng blend Additive.</summary>
    private static Color RadialGlow(float u, float v, int w, int h)
    {
        float d = Mathf.Sqrt(u * u + v * v);
        if (d >= 1f) return Color.clear;

        // Lõi đặc + đuôi mờ dài
        float core = Mathf.Pow(Mathf.Clamp01(1f - d / 0.34f), 2.0f);
        float halo = Mathf.Pow(Mathf.Clamp01(1f - d),          3.2f);
        float a    = Mathf.Clamp01(core * 0.85f + halo * 0.55f);

        return WithA(Color.Lerp(GlowWarm, Color.white, core * 0.5f), a);
    }

    /// <summary>Vòng tròn viền kem (khung icon mở khoá), giữa rỗng.</summary>
    private static Color RingCircle(float u, float v, int w, int h)
    {
        float d = Mathf.Sqrt(u * u + v * v);
        float unit = 2f / w;

        float outerA = Mathf.Clamp01((0.985f - d) / (unit * 1.6f));
        if (outerA <= 0.001f) return Color.clear;

        float thickness = unit * 13f;
        float innerEdge = 0.985f - thickness;
        float innerA    = Mathf.Clamp01((innerEdge - d) / (unit * 1.6f));

        // Gradient nhẹ trên sáng dưới tối cho vòng có khối
        float t = Mathf.InverseLerp(0.9f, -0.9f, v);
        Color ring = Color.Lerp(RingCream, RingCreamDk, t * 0.75f);

        // Bên trong rỗng để lộ icon phía sau
        return WithA(ring, outerA * (1f - innerA));
    }

    /// <summary>Đĩa tròn đặc — nền phía sau icon trong khung.</summary>
    private static Color CircleFill(float u, float v, int w, int h)
    {
        float d = Mathf.Sqrt(u * u + v * v);
        float unit = 2f / w;
        float a = Mathf.Clamp01((0.99f - d) / (unit * 1.6f));
        return a <= 0.001f ? Color.clear : WithA(SlotFill, a);
    }

    /// <summary>Nhãn "NEW" đỏ — chữ nhật bo góc, gradient, viền trắng mảnh.</summary>
    private static Color NewTag(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd = SdRoundBox(p, b, 12f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Aa(sd + 3.5f);
        float t   = Mathf.InverseLerp(1f, -1f, v);
        Color red = Color.Lerp(TagRedTop, TagRedBot, t);

        Color c = Color.Lerp(Color.white, red, aIn);   // viền trắng
        return WithA(c, aOut);
    }

    /// <summary>Nút xanh lá bo góc: gradient, viền đậm, dải bóng trên.</summary>
    private static Color ButtonGreen(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd = SdRoundBox(p, b, 24f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Aa(sd + 5f);
        float t   = Mathf.InverseLerp(1f, -1f, v);
        Color g   = Color.Lerp(BtnGreenTop, BtnGreenBot, t);
        g = Color.Lerp(g, Color.white, SStep(0.26f, 0f, t) * 0.26f);

        Color c = Color.Lerp(BtnGreenEdge, g, aIn);
        return WithA(c, aOut);
    }

    /// <summary>Dải nền tối cho khu icon (9-slice, kéo giãn tuỳ ý).</summary>
    private static Color SolidBand(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        float sd = SdRoundBox(p, new Vector2(w * 0.5f - 1f, h * 0.5f - 1f), 3f);
        return WithA(BandDark, Aa(sd) * BandDark.a);
    }

    /// <summary>Bóng đổ mềm hình tròn.</summary>
    private static Color SoftShadow(float u, float v, int w, int h)
    {
        float d = Mathf.Sqrt(u * u + v * v);
        float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.6f) * 0.5f;
        return WithA(Color.black, a);
    }

    /// <summary>Chữ nhật trắng bo góc — dùng làm nền chung, tint bằng Image.color.</summary>
    private static Color WhiteRounded(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        float sd = SdRoundBox(p, new Vector2(w * 0.5f - 1f, h * 0.5f - 1f), 14f);
        return WithA(Color.white, Aa(sd));
    }
}
