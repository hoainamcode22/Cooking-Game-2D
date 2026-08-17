#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SINH SPRITE THỦ TỤC CHO BẢNG ĐƠN HÀNG — không cần art từ ngoài.
///
/// ⚠ TRANG TRÍ CỐ Ý KHÁC BẢN THAM CHIẾU (mục 7 file TEAM: "bố cục giống video, trang trí
/// khác đi để tránh đạo ý tưởng"). Bốn thứ được đổi có chủ đích:
///
///   • Bảng màu: video dùng CAM ĐẤT + bảng đen xanh than → ở đây dùng XANH RÊU + KEM
///     GIẤY, nhấn HỔ PHÁCH. Cũng khác luôn bộ Quầy Hàng (mận/tím + ngọc lam) để hai
///     công trình không lẫn vào nhau trên bản đồ.
///   • Góc panel: video BO TRÒN trơn → ở đây BO TRÒN + ĐINH TÁN bốn góc, ngôn ngữ
///     "bảng thông báo đóng đinh" thay vì "khay nhựa".
///   • Viền trong: video dùng NÉT ĐỨT quanh biển tên → ở đây VIỀN KÉP hai đường liền.
///     Nét đứt được giữ lại nhưng đẩy sang chỗ khác (ô trống + gạch chia cột phải), nơi
///     nó mang nghĩa "chưa có gì ở đây" chứ không phải trang trí.
///   • Phiếu: video có MÉP DƯỚI RĂNG CƯA → ở đây MÉP XÉ GIẤY lượn tự nhiên + góc trên
///     phải GẬP LẠI.
///
/// Bố cục (title pill đè lên đỉnh, nút X lồi ra mép, lưới 3x3 bên trái, chi tiết bên
/// phải) giữ nguyên theo video vì đó là phần CÔNG NĂNG, không phải phần trang trí.
///
/// Hạ tầng (Gen / ApplyImportSettings) mượn nguyên từ <c>StallSpriteFactory</c> đã chạy
/// ổn trong dự án — viết lại từ đầu chỉ để lặp lại đúng những cái bẫy import cũ.
/// </summary>
public static class OrderBoardSpriteFactory
{
    public const string ArtFolder = "Assets/_Game/Farm/Art/UI_OrderBoard";

    // ── BẢNG MÀU BẢNG ĐƠN HÀNG ĐỒNG BỘ 100% VỚI KHO & SHOP ───────────────────
    public static readonly Color BoardTop  = Hex("#A9743C"); // Nâu gỗ sáng
    public static readonly Color BoardMid  = Hex("#8A5A2E"); // Nâu gỗ
    public static readonly Color BoardBot  = Hex("#7C4E22"); // Nâu gỗ đậm
    public static readonly Color BoardEdge = Hex("#4A2508"); // Viền nâu đậm

    public static readonly Color InsetTop  = Hex("#FDF3DA"); // Giấy kem
    public static readonly Color InsetBot  = Hex("#FBECCB"); // Giấy kem đậm
    public static readonly Color InsetEdge = Hex("#6E4014"); // Viền nâu

    public static readonly Color Cream     = Hex("#FFFBE9");
    public static readonly Color Paper     = Hex("#FFFDF4");
    public static readonly Color Amber     = Hex("#FFD257");
    public static readonly Color AmberDark = Hex("#F0A32F");
    public static readonly Color Brick     = Hex("#E4574C");
    public static readonly Color Leaf      = Hex("#57A51F");
    public static readonly Color Ocean     = Hex("#57A51F"); // Giao hàng xanh lá giống nút mua

    public static Color Hex(string hex)
        => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

    // ─────────────────────────────────────────────────────────────────────────
    //  API
    // ─────────────────────────────────────────────────────────────────────────

    public static void GenerateAll(bool force = false)
    {
        EnsureFolder();

        Gen("ob_panel",      256, 256, PanelRivet,   new Vector4(48, 48, 48, 48), force);
        Gen("ob_inset",      128, 128, InsetBox,     new Vector4(28, 28, 28, 28), force);
        Gen("ob_btn",        160,  88, ButtonRound,  new Vector4(30, 28, 30, 28), force);
        Gen("ob_pill",       256,  96, TitlePill,    new Vector4(54, 26, 54, 26), force);
        Gen("ob_glow",       160, 160, GlowFrame,    new Vector4(46, 46, 46, 46), force);

        Gen("ob_ticket",     256, 216, TicketPaper,  Vector4.zero, force);
        Gen("ob_dashed",     160, 160, DashedFrame,  Vector4.zero, force);
        Gen("ob_dashline",    40,  10, DashSegment,  Vector4.zero, force);

        Gen("ob_circle",     128, 128, CircleFill,   Vector4.zero, force);
        Gen("ob_smoke",      128, 128, SmokePuff,    Vector4.zero, force);

        Gen("ob_check",      128, 128, IconCheck,    Vector4.zero, force);
        Gen("ob_star",       128, 128, IconStar,     Vector4.zero, force);
        Gen("ob_coin",       128, 128, IconCoin,     Vector4.zero, force);
        Gen("ob_trash",      128, 128, IconTrash,    Vector4.zero, force);
        Gen("ob_pin",        128, 128, IconPin,      Vector4.zero, force);
        Gen("ob_clipboard",  128, 128, IconClipboard, Vector4.zero, force);

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
        // toàn ô trắng — cái bẫy đã ghi trong PopupSpriteFactory và StallSpriteFactory.
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

        // meshType phải là FullRect. Mặc định Unity import là Tight, và Image ở chế độ
        // Sliced/Tiled (gạch nét đứt của cột phải dùng Tiled) TỪ CHỐI sprite Tight —
        // đường kẻ biến mất kèm một dòng warning rất khó lần ra.
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

    // ─────────────────────────────────────────────────────────────────────────
    //  HÀM SDF DÙNG CHUNG
    // ─────────────────────────────────────────────────────────────────────────

    private static float SdRoundBox(Vector2 p, Vector2 b, float r)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
        return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + Vector2.Max(d, Vector2.zero).magnitude - r;
    }

    private static float SdSegment(Vector2 p, Vector2 a, Vector2 b, float thickness)
    {
        Vector2 pa = p - a;
        Vector2 ba = b - a;
        float denom = Vector2.Dot(ba, ba);
        float t = denom <= 0.0000001f ? 0f : Mathf.Clamp01(Vector2.Dot(pa, ba) / denom);
        return (pa - ba * t).magnitude - thickness;
    }

    /// <summary>Khoảng cách có dấu tới đa giác kín — dùng vẽ ngôi sao 5 cánh.</summary>
    private static float SdPolygon(Vector2 p, Vector2[] verts)
    {
        int n = verts.Length;
        float d = Vector2.Dot(p - verts[0], p - verts[0]);
        float s = 1f;

        for (int i = 0, j = n - 1; i < n; j = i, i++)
        {
            Vector2 e = verts[j] - verts[i];
            Vector2 w = p - verts[i];
            float denom = Vector2.Dot(e, e);
            float t = denom <= 0.0000001f ? 0f : Mathf.Clamp01(Vector2.Dot(w, e) / denom);
            Vector2 b = w - e * t;
            d = Mathf.Min(d, Vector2.Dot(b, b));

            bool c1 = p.y >= verts[i].y;
            bool c2 = p.y < verts[j].y;
            bool c3 = e.x * w.y > e.y * w.x;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
        }

        return s * Mathf.Sqrt(d);
    }

    private static float Aa(float sd, float softness = 1.4f) => Mathf.Clamp01(0.5f - sd / softness);

    /// <summary>smoothstep kiểu GLSL. KHÔNG phải Mathf.SmoothStep — hàm Unity nội suy giá trị, không phải ngưỡng.</summary>
    private static float SStep(float e0, float e1, float x)
    {
        float k = Mathf.Clamp01((x - e0) / (e1 - e0));
        return k * k * (3f - 2f * k);
    }

    private static Color WithA(Color c, float a) { c.a = a; return c; }

    // ─────────────────────────────────────────────────────────────────────────
    //  KHUNG NỀN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Nền panel: bo tròn, gradient dọc, VIỀN KÉP hai đường, ĐINH TÁN bốn góc.</summary>
    private static Color PanelRivet(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd   = SdRoundBox(p, b, 36f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Aa(sd + 6f);
        float t   = Mathf.InverseLerp(1f, -1f, v);   // 0 = trên, 1 = dưới

        Color body = t < 0.5f
            ? Color.Lerp(BoardTop, BoardMid, t / 0.5f)
            : Color.Lerp(BoardMid, BoardBot, (t - 0.5f) / 0.5f);

        body = Color.Lerp(body, Color.white, SStep(0.20f, 0f, t) * 0.14f);

        Color c = Color.Lerp(BoardEdge, body, aIn);

        // VIỀN KÉP: hai đường mảnh màu hổ phách nhạt, cách mép 14 và 22.
        // Đây là chỗ cố ý khác video (video dùng một đường nét đứt).
        float line1 = Mathf.Abs(sd + 15f) - 1.6f;
        float line2 = Mathf.Abs(sd + 23f) - 1.0f;
        float lineA = Mathf.Max(Aa(line1) * 0.55f, Aa(line2) * 0.30f);
        c = Color.Lerp(c, Amber, lineA);

        // ĐINH TÁN bốn góc — nằm gọn trong 4 ô góc của 9-slice nên không bị kéo giãn.
        float rivet = 1f;
        float rx = w * 0.5f - 30f;
        float ry = h * 0.5f - 30f;
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sy = -1; sy <= 1; sy += 2)
            rivet = Mathf.Min(rivet, (p - new Vector2(sx * rx, sy * ry)).magnitude - 6.5f);

        float rivetA = Aa(rivet);
        if (rivetA > 0.001f)
        {
            Color head = Color.Lerp(Amber, AmberDark, Mathf.InverseLerp(-1f, 1f, v));
            c = Color.Lerp(c, head, rivetA);
        }

        return WithA(c, aOut);
    }

    /// <summary>Vùng lõm: tối hơn nền, bóng đổ trong ở mép trên cho cảm giác thụt vào.</summary>
    private static Color InsetBox(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd   = SdRoundBox(p, b, 22f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Aa(sd + 4f);
        float t   = Mathf.InverseLerp(1f, -1f, v);

        Color body = Color.Lerp(InsetTop, InsetBot, t);
        body = Color.Lerp(body, Color.black, SStep(0.18f, 0f, t) * 0.35f);   // lõm: tối ở TRÊN

        return WithA(Color.Lerp(InsetEdge, body, aIn), aOut);
    }

    /// <summary>Nút TRẮNG bo tròn — tô bằng Image.color nên một sprite dùng cho mọi màu nút.</summary>
    private static Color ButtonRound(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd   = SdRoundBox(p, b, 24f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Aa(sd + 5f);
        float t   = Mathf.InverseLerp(1f, -1f, v);

        Color body = Color.Lerp(Color.white, new Color(0.70f, 0.70f, 0.70f), t);
        body = Color.Lerp(body, Color.white, SStep(0.26f, 0f, t) * 0.38f);

        Color edge = new Color(0.30f, 0.32f, 0.30f);
        return WithA(Color.Lerp(edge, body, aIn), aOut);
    }

    /// <summary>Biển tên: viên thuốc bo tròn hết cỡ, viền hổ phách, ruột rêu đậm, viền kép bên trong.</summary>
    private static Color TitlePill(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 2f, h * 0.5f - 2f);

        float sd   = SdRoundBox(p, b, h * 0.48f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        float aGold = Aa(sd + 8f);
        float aBody = Aa(sd + 14f);

        float t = Mathf.InverseLerp(1f, -1f, v);
        Color gold = Color.Lerp(Amber, AmberDark, t);
        Color body = Color.Lerp(BoardBot, BoardEdge, t);

        Color c = Color.Lerp(BoardEdge, gold, aGold);
        c = Color.Lerp(c, body, aBody);

        // Đường viền mảnh thứ hai bên trong ruột.
        float inner = Mathf.Abs(sd + 22f) - 1.2f;
        c = Color.Lerp(c, Amber, Aa(inner) * 0.45f);

        return WithA(c, aOut);
    }

    /// <summary>
    /// KHUNG PHÁT SÁNG VÀNG cho phiếu đang chọn (B4 trạng thái 3).
    /// Ruột rỗng hoàn toàn để tờ giấy bên dưới vẫn nhìn thấy nguyên vẹn.
    /// </summary>
    private static Color GlowFrame(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 12f, h * 0.5f - 12f);

        float sd = SdRoundBox(p, b, 26f);

        // Quầng sáng lan ra NGOÀI khung
        float halo = SStep(14f, -2f, sd) * 0.55f;

        // Đường viền sắc nét ngay trên khung
        float line = Aa(Mathf.Abs(sd) - 3.2f);

        float a = Mathf.Clamp01(Mathf.Max(halo, line));

        // Ruột rỗng: mọi thứ thụt vào quá 5px đều trong suốt
        if (sd < -5f) a = 0f;

        if (a <= 0.003f) return Color.clear;

        Color c = Color.Lerp(Amber, Color.white, line * 0.45f);
        return WithA(c, a);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PHIẾU GIẤY
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TỜ PHIẾU — mép dưới XÉ GIẤY (khác video: video là răng cưa đều), góc trên phải GẬP.
    /// Vẽ bằng tông TRẮNG để `Image.color` nhuộm được cả hai màu: trắng ngà và xanh lá.
    /// </summary>
    private static Color TicketPaper(float u, float v, int w, int h)
    {
        float x = (u * 0.5f + 0.5f) * w;    // 0..w
        float y = (v * 0.5f + 0.5f) * h;    // 0..h, 0 = đáy

        // Mép xé: đường biên dưới gợn sóng không đều (ba tần số cộng lại cho khỏi máy móc)
        float tear = 12f
                   + 5.0f * Mathf.Sin(x * 0.085f)
                   + 3.0f * Mathf.Sin(x * 0.21f + 1.7f)
                   + 1.8f * Mathf.Sin(x * 0.47f + 0.4f);

        if (y < tear) return Color.clear;

        // Thân giấy bo góc nhẹ (chỉ ba góc trên + hai góc dưới đã bị xé cắt qua)
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 3f, h * 0.5f - 3f);
        float sd = SdRoundBox(p, b, 12f);
        float aOut = Aa(sd);
        if (aOut <= 0.001f) return Color.clear;

        // Góc trên phải GẬP LẠI: cắt một tam giác rồi tô đậm hơn ở mặt gập
        float fold = 34f;
        float foldDist = (x - (w - fold)) + (y - (h - fold));   // >0 nghĩa là nằm trong tam giác góc
        if (foldDist > 0f)
        {
            // Mặt gập: xám hơn thân giấy, có đường gấp sắc
            float edge = Aa(4f - foldDist);
            Color foldCol = Color.Lerp(new Color(0.80f, 0.78f, 0.72f), new Color(0.62f, 0.60f, 0.55f),
                                       Mathf.Clamp01(foldDist / fold));
            return WithA(Color.Lerp(foldCol, new Color(0.45f, 0.44f, 0.40f), edge * 0.5f), aOut);
        }

        float t = Mathf.InverseLerp(1f, -1f, v);
        Color body = Color.Lerp(Color.white, new Color(0.90f, 0.89f, 0.85f), t * 0.55f);

        // Hai dòng kẻ mờ — chỗ đặt hai dòng phần thưởng (sao EXP + đồng vàng)
        float rule1 = Mathf.Abs(y - h * 0.545f) - 1.0f;
        float rule2 = Mathf.Abs(y - h * 0.285f) - 1.0f;
        float ruleA = Mathf.Max(Aa(rule1), Aa(rule2)) * 0.28f;

        // Cắt hai dòng kẻ khỏi mép trái/phải cho giống giấy có lề
        if (x < w * 0.12f || x > w * 0.88f) ruleA = 0f;

        body = Color.Lerp(body, new Color(0.55f, 0.55f, 0.52f), ruleA);

        // Viền giấy mảnh cho tách khỏi nền
        body = Color.Lerp(body, new Color(0.62f, 0.61f, 0.57f), Aa(sd + 2.5f) < 0.5f ? 0.55f : 0f);

        return WithA(body, aOut);
    }

    /// <summary>Ô trống: khung bo góc VIỀN NÉT ĐỨT — trạng thái 4 của phiếu (B4).</summary>
    private static Color DashedFrame(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 6f, h * 0.5f - 6f);

        float sd = SdRoundBox(p, b, 22f);

        // Dải viền dày ~3px quanh đường bao
        float band = Aa(Mathf.Abs(sd) - 2.6f);
        if (band <= 0.003f) return Color.clear;

        // Cắt thành nét đứt: tham số chạy dọc chu vi xấp xỉ bằng |x|+|y| — đủ tốt cho
        // hình bo góc, và quan trọng là ĐỀU ở cả bốn cạnh.
        float s    = Mathf.Abs(p.x) + Mathf.Abs(p.y);
        float dash = Mathf.Repeat(s, 17f) < 10f ? 1f : 0f;

        float a = band * dash;
        return a <= 0.003f ? Color.clear : WithA(new Color(1f, 1f, 1f, 1f), a * 0.55f);
    }

    /// <summary>Một đốt gạch nét đứt — lát ngang (Image.Type.Tiled) thành đường chia cột phải.</summary>
    private static Color DashSegment(float u, float v, int w, int h)
    {
        float x = (u * 0.5f + 0.5f) * w;
        float y = (v * 0.5f + 0.5f) * h;

        // Đốt gạch chiếm 60% ô, còn lại là khoảng hở
        float dashLen = w * 0.62f;
        if (x > dashLen) return Color.clear;

        float half = h * 0.30f;
        float d = Mathf.Abs(y - h * 0.5f) - half;
        float a = Mathf.Clamp01(0.5f - d / 1.4f);

        // Bo hai đầu đốt gạch
        float capA = Mathf.Clamp01(Mathf.Min(x, dashLen - x) / 1.6f + 0.2f);

        a = Mathf.Min(a, capA);
        return a <= 0.003f ? Color.clear : WithA(Color.white, a);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HÌNH CƠ BẢN + ICON
    // ─────────────────────────────────────────────────────────────────────────

    private static Color CircleFill(float u, float v, int w, int h)
    {
        float d    = Mathf.Sqrt(u * u + v * v);
        float unit = 2f / w;
        float a    = Mathf.Clamp01((0.99f - d) / (unit * 1.6f));
        return a <= 0.001f ? Color.clear : WithA(Color.white, a);
    }

    /// <summary>Cụm khói: khối tròn mềm, mép lồi lõm — hiệu ứng 1 của B9.</summary>
    private static Color SmokePuff(float u, float v, int w, int h)
    {
        float ang = Mathf.Atan2(v, u);
        float d   = Mathf.Sqrt(u * u + v * v);

        // Bán kính gợn theo góc → cụm khói không phải hình tròn hoàn hảo
        float r = 0.78f
                + 0.10f * Mathf.Sin(ang * 3f)
                + 0.06f * Mathf.Sin(ang * 5f + 1.1f);

        // Rìa mềm: khói không có đường bao sắc
        float a = SStep(r, r - 0.34f, d);
        if (a <= 0.004f) return Color.clear;

        // Ruột sáng hơn rìa cho có khối
        Color c = Color.Lerp(new Color(0.86f, 0.88f, 0.86f), Color.white, SStep(0.7f, 0.05f, d));
        return WithA(c, a * 0.92f);
    }

    /// <summary>Dấu tích — hai đốt gạch nối nhau, bo đầu.</summary>
    private static Color IconCheck(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u, v);
        float d = Mathf.Min(
            SdSegment(p, new Vector2(-0.58f, 0.02f), new Vector2(-0.16f, -0.44f), 0.17f),
            SdSegment(p, new Vector2(-0.16f, -0.44f), new Vector2(0.60f, 0.46f), 0.17f));

        float a = Mathf.Clamp01(0.5f - d / (2f / w * 2.2f));
        return a <= 0.001f ? Color.clear : WithA(Color.white, a);
    }

    /// <summary>Ngôi sao 5 cánh — dấu hiệu EXP. Vẽ bằng đa giác thay vì ký tự ★ (font thiếu là ra ô vuông).</summary>
    private static Color IconStar(float u, float v, int w, int h)
    {
        var verts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float r  = (i % 2 == 0) ? 0.92f : 0.40f;
            float th = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
            verts[i] = new Vector2(Mathf.Cos(th) * r, Mathf.Sin(th) * r);
        }

        float sd = SdPolygon(new Vector2(u, v), verts);
        float a  = Mathf.Clamp01(0.5f - sd / (2f / w * 2.0f));
        if (a <= 0.001f) return Color.clear;

        // Sáng ở trên, đậm dần xuống dưới cho có khối
        Color c = Color.Lerp(Color.white, new Color(0.78f, 0.86f, 0.98f), Mathf.InverseLerp(1f, -1f, v));
        return WithA(c, a);
    }

    /// <summary>Đồng xu vàng: vành ngoài đậm, ruột sáng, vệt sáng chéo.</summary>
    private static Color IconCoin(float u, float v, int w, int h)
    {
        float d    = Mathf.Sqrt(u * u + v * v);
        float unit = 2f / w;

        float aOut = Mathf.Clamp01((0.94f - d) / (unit * 1.8f));
        if (aOut <= 0.001f) return Color.clear;

        float aIn = Mathf.Clamp01((0.72f - d) / (unit * 1.8f));

        float t = Mathf.InverseLerp(0.9f, -0.9f, v);
        Color ring  = Color.Lerp(AmberDark, Hex("#7E5A11"), t);
        Color inner = Color.Lerp(Amber, AmberDark, t);

        Color c = Color.Lerp(ring, inner, aIn);
        c = Color.Lerp(c, Color.white, SStep(0.15f, -0.55f, u + v) * 0.34f * aIn);

        return WithA(c, aOut);
    }

    /// <summary>Thùng rác — nút bỏ đơn (B8).</summary>
    private static Color IconTrash(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u, v);
        float unit = 2f / w;

        // Thân thùng
        float body = SdRoundBox(new Vector2(p.x, p.y + 0.18f), new Vector2(0.42f, 0.46f), 0.10f);
        // Nắp
        float lid  = SdRoundBox(new Vector2(p.x, p.y - 0.40f), new Vector2(0.60f, 0.09f), 0.06f);
        // Quai nắp
        float grip = SdRoundBox(new Vector2(p.x, p.y - 0.60f), new Vector2(0.22f, 0.08f), 0.05f);

        float sd = Mathf.Min(body, Mathf.Min(lid, grip));
        float a  = Mathf.Clamp01(0.5f - sd / (unit * 2.0f));
        if (a <= 0.001f) return Color.clear;

        // Ba rãnh dọc trên thân — khoét thủng để đọc ra "thùng rác" chứ không phải cái hộp
        float slot = 1f;
        for (int i = -1; i <= 1; i++)
            slot = Mathf.Min(slot,
                SdRoundBox(new Vector2(p.x - i * 0.21f, p.y + 0.18f), new Vector2(0.045f, 0.28f), 0.04f));

        float slotA = Mathf.Clamp01(0.5f - slot / (unit * 2.0f));

        return WithA(Color.white, a * (1f - slotA));
    }

    /// <summary>Đinh ghim: mũ tròn + thân nhọn. Ghim tờ phiếu lên bảng.</summary>
    private static Color IconPin(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u, v);
        float unit = 2f / w;

        float head   = new Vector2(p.x, p.y - 0.24f).magnitude - 0.52f;
        float needle = SdRoundBox(new Vector2(p.x, p.y + 0.52f), new Vector2(0.08f, 0.34f), 0.05f);

        float sd = Mathf.Min(head, needle);
        float a  = Mathf.Clamp01(0.5f - sd / (unit * 2.0f));
        if (a <= 0.001f) return Color.clear;

        // Vệt sáng góc trên trái cho mũ đinh có độ bóng
        Color c = Color.Lerp(Color.white, new Color(0.72f, 0.72f, 0.74f),
                             Mathf.InverseLerp(1f, -1f, v));
        c = Color.Lerp(c, Color.white, SStep(0.1f, -0.7f, u + v) * 0.5f);

        return WithA(c, a);
    }

    /// <summary>Kẹp giấy (clipboard) — icon cạnh biển tên popup, thay cho mặt cú của video.</summary>
    private static Color IconClipboard(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u, v);
        float unit = 2f / w;

        float board = SdRoundBox(new Vector2(p.x, p.y - 0.04f), new Vector2(0.60f, 0.76f), 0.14f);
        float aBoard = Mathf.Clamp01(0.5f - board / (unit * 2.0f));
        if (aBoard <= 0.001f) return Color.clear;

        Color c = Color.Lerp(Color.white, new Color(0.80f, 0.80f, 0.78f),
                             Mathf.InverseLerp(1f, -1f, v) * 0.5f);

        // Kẹp kim loại trên đỉnh
        float clip = SdRoundBox(new Vector2(p.x, p.y - 0.70f), new Vector2(0.26f, 0.14f), 0.07f);
        c = Color.Lerp(c, new Color(0.45f, 0.45f, 0.47f), Mathf.Clamp01(0.5f - clip / (unit * 2.0f)));

        // Ba dòng chữ giả
        float lines = 1f;
        for (int i = 0; i < 3; i++)
            lines = Mathf.Min(lines,
                SdRoundBox(new Vector2(p.x + 0.05f, p.y + 0.34f - i * 0.30f),
                           new Vector2(0.34f, 0.045f), 0.04f));

        c = Color.Lerp(c, new Color(0.42f, 0.44f, 0.42f), Mathf.Clamp01(0.5f - lines / (unit * 2.0f)));

        return WithA(c, aBoard);
    }
}
#endif
