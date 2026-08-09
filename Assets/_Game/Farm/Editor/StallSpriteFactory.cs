#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SINH SPRITE THỦ TỤC CHO QUẦY HÀNG — không cần art từ ngoài.
///
/// ⚠ TRANG TRÍ CỐ Ý KHÁC VIDEO THAM CHIẾU (yêu cầu mục 0.6 file TEAM: "bố cục giống
/// video, trang trí khác đi để tránh đạo ý tưởng"). Ba thứ được đổi có chủ đích:
///
///   • Bảng màu: video dùng CAM ĐẤT → ở đây dùng MẬN/TÍM THAN + nhấn NGỌC LAM.
///   • Góc: video BO TRÒN → ở đây VÁT GÓC (bát giác), cho cảm giác gỗ đóng thay vì nhựa.
///   • Mái hiên: video SỌC XANH-TRẮNG → ở đây VIỀN RĂNG SÒ (scallop) ngọc lam viền vàng.
///
/// Bố cục (mái vắt ngang đỉnh, title pill đè lên mái, nút X lồi ra mép, lưới ô lõm)
/// thì giữ nguyên theo video vì đó là phần CÔNG NĂNG, không phải phần trang trí.
///
/// Hạ tầng (Gen / ApplyImportSettings) mượn nguyên từ `PopupSpriteFactory` đã chạy ổn
/// trong dự án — viết lại từ đầu chỉ để lặp lại đúng những cái bẫy import cũ.
/// </summary>
public static class StallSpriteFactory
{
    public const string ArtFolder = "Assets/_Game/Farm/Art/UI_Stall";

    // ── BẢNG MÀU QUẦY HÀNG ───────────────────────────────────────────────────
    public static readonly Color PanelTop   = Hex("#6B4A90");
    public static readonly Color PanelMid   = Hex("#553873");
    public static readonly Color PanelBot   = Hex("#3E2857");
    public static readonly Color PanelEdge  = Hex("#2A1A3C");

    public static readonly Color SlotTop    = Hex("#2E1D42");
    public static readonly Color SlotBot    = Hex("#3B2653");
    public static readonly Color SlotEdge   = Hex("#20122F");

    public static readonly Color Teal       = Hex("#2FBFA8");
    public static readonly Color TealDark   = Hex("#1E8C7B");
    public static readonly Color Gold       = Hex("#F2C14E");
    public static readonly Color GoldDark   = Hex("#C4922B");
    public static readonly Color Cream      = Hex("#F6EFE4");

    public static Color Hex(string hex)
        => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

    // ─────────────────────────────────────────────────────────────────────────
    //  API
    // ─────────────────────────────────────────────────────────────────────────

    public static void GenerateAll(bool force = false)
    {
        EnsureFolder();

        Gen("stall_panel",        192, 192, PanelChamfer,  new Vector4(40, 40, 40, 40), force);
        Gen("stall_slot",         128, 128, SlotChamfer,   new Vector4(26, 26, 26, 26), force);
        Gen("stall_btn",          160,  88, ButtonChamfer, new Vector4(30, 26, 30, 30), force);
        Gen("stall_pill",         224,  88, TitlePill,     new Vector4(48, 24, 48, 24), force);
        Gen("stall_valance",      128,  80, ValanceScallop, Vector4.zero,               force);
        Gen("stall_circle",       128, 128, CircleFill,    Vector4.zero,                force);
        Gen("stall_icon_lock",    128, 128, IconLock,      Vector4.zero,                force);
        Gen("stall_icon_plus",    128, 128, IconPlus,      Vector4.zero,                force);
        Gen("stall_icon_speaker", 128, 128, IconSpeaker,   Vector4.zero,                force);
        Gen("stall_icon_coin",    128, 128, IconCoin,      Vector4.zero,                force);

        AssetDatabase.Refresh();
    }

    public static Sprite Load(string spriteName)
        => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{spriteName}.png");

    // ─────────────────────────────────────────────────────────────────────────
    //  HẠ TẦNG
    // ─────────────────────────────────────────────────────────────────────────

    private static void EnsureFolder()
    {
        string abs = Abs(ArtFolder);
        if (!Directory.Exists(abs))
        {
            Directory.CreateDirectory(abs);
            AssetDatabase.Refresh();
        }
    }

    private static string Abs(string assetPath)
        => Path.Combine(Directory.GetCurrentDirectory(), assetPath);

    private delegate Color PixelFn(float u, float v, int w, int h);

    private static void Gen(string spriteName, int w, int h, PixelFn fn, Vector4 border, bool force)
    {
        string path = $"{ArtFolder}/{spriteName}.png";

        if (force || !File.Exists(Abs(path)))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px  = new Color[w * h];

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w * 2f - 1f;   // u,v ∈ [-1,1], gốc ở tâm
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

        // LUÔN áp lại import settings kể cả khi file đã có. Nếu lần import đầu lỡ hỏng
        // (textureType = Default) thì Load<Sprite>() trả null VĨNH VIỄN và cả popup ra
        // toàn ô trắng — đúng cái bẫy đã ghi trong PopupSpriteFactory.
        ApplyImportSettings(path, border);
    }

    private static void ApplyImportSettings(string path, Vector4 border)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        bool dirty = imp.textureType != TextureImporterType.Sprite || imp.spriteBorder != border;

        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled       = false;
        imp.filterMode          = FilterMode.Bilinear;
        imp.wrapMode            = TextureWrapMode.Clamp;
        imp.spriteBorder        = border;

        // meshType phải là FullRect. Mặc định Unity import là Tight, và SpriteRenderer
        // ở chế độ Sliced/Tiled (mặt quầy ngoài map dùng cả hai) TỪ CHỐI sprite Tight —
        // hàng bày trên quầy sẽ biến mất kèm một dòng warning khó lần ra.
        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        if (settings.spriteMeshType != SpriteMeshType.FullRect)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            dirty = true;
        }

        if (dirty) imp.SaveAndReimport();
    }

    // ── HÀM SDF DÙNG CHUNG ───────────────────────────────────────────────────

    /// <summary>
    /// SDF hình chữ nhật VÁT GÓC (bát giác) — thay cho bo tròn để bộ quầy hàng khác
    /// hẳn bộ popup lên cấp đang có và khác video tham chiếu.
    /// </summary>
    private static float SdChamferBox(Vector2 p, Vector2 b, float chamfer)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
        Vector2 d = new Vector2(q.x - b.x, q.y - b.y);
        float box  = Mathf.Min(Mathf.Max(d.x, d.y), 0f) + Vector2.Max(d, Vector2.zero).magnitude;
        float diag = (q.x + q.y - (b.x + b.y - chamfer)) * 0.70710678f;
        return Mathf.Max(box, diag);
    }

    private static float Aa(float sd, float softness = 1.4f) => Mathf.Clamp01(0.5f - sd / softness);

    /// <summary>smoothstep kiểu GLSL. KHÔNG phải Mathf.SmoothStep — hàm của Unity nội suy giá trị, không phải ngưỡng.</summary>
    private static float SStep(float e0, float e1, float x)
    {
        float k = Mathf.Clamp01((x - e0) / (e1 - e0));
        return k * k * (3f - 2f * k);
    }

    private static Color WithA(Color c, float a) { c.a = a; return c; }

    // ── CÁC HÌNH ─────────────────────────────────────────────────────────────

    /// <summary>Nền panel: vát góc, gradient dọc, viền ngoài đậm, dải bóng ở mép trên.</summary>
    private static Color PanelChamfer(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd   = SdChamferBox(p, b, 34f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Aa(sd + 5f);
        float t   = Mathf.InverseLerp(1f, -1f, v);   // 0 = trên, 1 = dưới

        Color body = t < 0.5f
            ? Color.Lerp(PanelTop, PanelMid, t / 0.5f)
            : Color.Lerp(PanelMid, PanelBot, (t - 0.5f) / 0.5f);

        body = Color.Lerp(body, Color.white, SStep(0.20f, 0f, t) * 0.18f);

        return WithA(Color.Lerp(PanelEdge, body, aIn), aOut);
    }

    /// <summary>Ô lõm: vát góc, tối hơn nền, bóng đổ trong ở mép trên cho cảm giác thụt vào.</summary>
    private static Color SlotChamfer(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd   = SdChamferBox(p, b, 22f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Aa(sd + 4f);
        float t   = Mathf.InverseLerp(1f, -1f, v);

        Color body = Color.Lerp(SlotTop, SlotBot, t);
        body = Color.Lerp(body, Color.black, SStep(0.18f, 0f, t) * 0.35f);   // lõm: tối ở TRÊN

        return WithA(Color.Lerp(SlotEdge, body, aIn), aOut);
    }

    /// <summary>Nút trắng vát góc — tô màu bằng Image.color nên dùng lại được cho mọi màu nút.</summary>
    private static Color ButtonChamfer(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd   = SdChamferBox(p, b, 24f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Aa(sd + 5f);
        float t   = Mathf.InverseLerp(1f, -1f, v);

        // Trắng ở giữa, tối dần xuống đáy + viền đen mờ: nhân với Image.color ra nút
        // có khối ở BẤT KỲ màu nào, không phải sinh riêng một sprite cho mỗi màu.
        Color body = Color.Lerp(Color.white, new Color(0.72f, 0.72f, 0.72f), t);
        body = Color.Lerp(body, Color.white, SStep(0.24f, 0f, t) * 0.35f);

        Color edge = new Color(0.35f, 0.33f, 0.38f);
        return WithA(Color.Lerp(edge, body, aIn), aOut);
    }

    /// <summary>Biển tên popup: lục giác dẹt viền vàng, ruột mận đậm.</summary>
    private static Color TitlePill(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd   = SdChamferBox(p, b, h * 0.48f);   // vát sâu → hai đầu thành mũi nhọn
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aGold = Aa(sd + 7f);    // dải viền vàng
        float aBody = Aa(sd + 13f);   // ruột

        float t = Mathf.InverseLerp(1f, -1f, v);
        Color gold = Color.Lerp(Gold, GoldDark, t);
        Color body = Color.Lerp(PanelBot, PanelEdge, t);

        Color c = Color.Lerp(PanelEdge, gold, aGold);
        c = Color.Lerp(c, body, aBody);
        return WithA(c, aOut);
    }

    /// <summary>
    /// MÁI HIÊN RĂNG SÒ — thay cho mái sọc xanh-trắng của video.
    /// Lát ngang được: hai vòng cung đặt ở x = w/4 và 3w/4, tiếp tuyến đúng tại hai mép
    /// nên nối tiếp nhau không thấy đường ghép.
    /// </summary>
    private static Color ValanceScallop(float u, float v, int w, int h)
    {
        float x = (u * 0.5f + 0.5f) * w;
        float y = (v * 0.5f + 0.5f) * h;   // 0 = đáy

        float bandBottom = h * 0.45f;
        float r          = w * 0.25f;
        float cy         = bandBottom;

        bool inside = y >= bandBottom;

        if (!inside)
        {
            float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.25f, cy));
            float d2 = Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.75f, cy));
            inside = Mathf.Min(d1, d2) <= r;
        }

        if (!inside) return Color.clear;

        // Khử răng cưa ở rìa cung
        float alpha = 1f;
        if (y < bandBottom)
        {
            float d = Mathf.Min(
                Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.25f, cy)),
                Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.75f, cy)));
            alpha = Mathf.Clamp01((r - d) / 1.5f);
        }

        float t = 1f - y / h;
        Color c = Color.Lerp(Teal, TealDark, t);

        // Viền vàng mảnh chạy dọc mép dưới của từng răng sò
        if (y < bandBottom)
        {
            float d = Mathf.Min(
                Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.25f, cy)),
                Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.75f, cy)));
            c = Color.Lerp(c, Gold, SStep(r - 7f, r - 1f, d));
        }

        return WithA(c, alpha);
    }

    private static Color CircleFill(float u, float v, int w, int h)
    {
        float d    = Mathf.Sqrt(u * u + v * v);
        float unit = 2f / w;
        float a    = Mathf.Clamp01((0.99f - d) / (unit * 1.6f));
        return a <= 0.001f ? Color.clear : WithA(Color.white, a);
    }

    /// <summary>Ổ khoá: thân vát góc + quai hình vòng cung. Vẽ bằng hình thay vì dùng emoji 🔒.</summary>
    private static Color IconLock(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u, v);

        // Thân khoá
        float body = SdChamferBox(new Vector2(p.x, p.y + 0.22f), new Vector2(0.46f, 0.36f), 0.14f);

        // Quai: nửa vành khuyên phía trên
        float ringD  = Mathf.Abs(new Vector2(p.x, p.y - 0.30f).magnitude - 0.30f) - 0.09f;
        float shackle = p.y >= 0.30f ? ringD : Mathf.Max(ringD, 0.30f - p.y);

        float sd = Mathf.Min(body, shackle);
        float a  = Mathf.Clamp01(0.5f - sd / (2f / w * 1.8f));
        if (a <= 0.001f) return Color.clear;

        // Lỗ chìa trên thân khoá
        float hole = new Vector2(p.x, p.y + 0.20f).magnitude - 0.10f;
        float aHole = Mathf.Clamp01(0.5f - hole / (2f / w * 1.8f));

        Color c = Color.Lerp(Cream, GoldDark, Mathf.InverseLerp(0.8f, -0.8f, v) * 0.5f);
        return WithA(c, a * (1f - aHole));
    }

    private static Color IconPlus(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u, v);
        float bar1 = SdChamferBox(p, new Vector2(0.62f, 0.17f), 0.06f);
        float bar2 = SdChamferBox(p, new Vector2(0.17f, 0.62f), 0.06f);

        float sd = Mathf.Min(bar1, bar2);
        float a  = Mathf.Clamp01(0.5f - sd / (2f / w * 1.8f));
        return a <= 0.001f ? Color.clear : WithA(Cream, a);
    }

    /// <summary>Loa phát thanh: thân hình thang mở sang phải + hai vòng sóng âm.</summary>
    private static Color IconSpeaker(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u, v);
        float unit = 2f / w;

        // Thân: nửa trái là hộp nhỏ, mở rộng dần sang phải
        float halfH = Mathf.Lerp(0.16f, 0.46f, Mathf.InverseLerp(-0.75f, -0.05f, p.x));
        float bodyA = 0f;
        if (p.x >= -0.78f && p.x <= -0.02f)
            bodyA = Mathf.Clamp01((halfH - Mathf.Abs(p.y)) / (unit * 1.8f));

        // Hai vòng sóng bên phải
        float wave = 1f;
        for (int i = 0; i < 2; i++)
        {
            float radius = 0.28f + i * 0.26f;
            float ring = Mathf.Abs(new Vector2(p.x + 0.05f, p.y).magnitude - radius) - 0.06f;
            // Chỉ giữ cung bên phải (~±55°)
            if (p.x + 0.05f <= 0f || Mathf.Abs(p.y) > (p.x + 0.05f) * 1.4f) ring = 1f;
            wave = Mathf.Min(wave, ring);
        }

        float waveA = Mathf.Clamp01(0.5f - wave / (unit * 1.8f));
        float a = Mathf.Max(bodyA, waveA);
        return a <= 0.001f ? Color.clear : WithA(Cream, a);
    }

    /// <summary>Đồng xu vàng: vành ngoài đậm, ruột sáng, một khấc chéo cho khỏi phẳng.</summary>
    private static Color IconCoin(float u, float v, int w, int h)
    {
        float d    = Mathf.Sqrt(u * u + v * v);
        float unit = 2f / w;

        float aOut = Mathf.Clamp01((0.94f - d) / (unit * 1.8f));
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Mathf.Clamp01((0.72f - d) / (unit * 1.8f));

        float t = Mathf.InverseLerp(0.9f, -0.9f, v);
        Color ring  = Color.Lerp(GoldDark, Hex("#8F6512"), t);
        Color inner = Color.Lerp(Gold, GoldDark, t);

        Color c = Color.Lerp(ring, inner, aIn);

        // Vệt sáng chéo phía trên-trái
        float gloss = SStep(0.15f, -0.55f, u + v) * 0.35f * aIn;
        c = Color.Lerp(c, Color.white, gloss);

        return WithA(c, aOut);
    }
}
#endif
