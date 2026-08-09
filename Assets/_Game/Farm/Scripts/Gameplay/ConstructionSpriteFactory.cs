using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SINH SPRITE THỦ TỤC **LÚC CHẠY** cho toàn bộ hệ "ĐANG XÂY".
///
/// VÌ SAO KHÔNG DÙNG PopupSpriteFactory: file đó nằm trong thư mục Editor/ (dùng
/// AssetDatabase + File.WriteAllBytes) nên KHÔNG tồn tại trong bản build. Hệ công trường
/// phải dựng được ở runtime trên máy người chơi → cần một bản chỉ dùng API runtime.
/// Kỹ thuật vẽ giữ nguyên: SDF + khử răng cưa, y như PopupSpriteFactory (xem §9.3 doc đội).
///
/// Mọi sprite được CACHE tĩnh: một texture chỉ sinh một lần cho cả phiên chơi.
/// hideFlags = HideAndDontSave để texture không bị kẹt vào scene khi Edric bấm Save.
///
/// LƯU Ý "Enter Play Mode không reload domain": biến static sống sót qua lần Play sau
/// nhưng Texture2D bên trong đã bị huỷ → cache trả về "fake null". Vì vậy mọi lần lấy
/// cache đều kiểm tra `cached != null` (toán tử == của UnityEngine.Object) rồi sinh lại.
/// </summary>
public static class ConstructionSpriteFactory
{
    // ── BẢNG MÀU ─────────────────────────────────────────────────────────────
    public static readonly Color WoodLight = Hex("#C99055");
    public static readonly Color WoodMid   = Hex("#A9713C");
    public static readonly Color WoodDark  = Hex("#7A4B1E");
    public static readonly Color WoodEdge  = Hex("#4A2C0E");

    public static readonly Color PanelDark = Hex("#1F1D1A");

    public static readonly Color GreenTop  = Hex("#6DD62A");
    public static readonly Color GreenMid  = Hex("#4FB61C");
    public static readonly Color GreenBot  = Hex("#3E9A14");
    public static readonly Color GreenEdge = Hex("#245F08");

    public static readonly Color CoinEdge  = Hex("#B7790C");
    public static readonly Color CoinMid   = Hex("#FFC531");
    public static readonly Color CoinCore  = Hex("#FFE9A8");

    public static readonly Color GemEdge   = Hex("#1E7FC2");
    public static readonly Color GemMid    = Hex("#4FC3F7");
    public static readonly Color GemCore   = Hex("#BDF0FF");

    public static readonly Color HatYellow = Hex("#FFC531");
    public static readonly Color HatEdge   = Hex("#A96F04");

    public static readonly Color ClockFace = Hex("#F3F3EF");
    public static readonly Color ClockRim  = Hex("#3B3A36");

    public static readonly Color WorkerBody = Hex("#3E6FA8");
    public static readonly Color WorkerSkin = Hex("#E8B98C");

    public static Color Hex(string hex)
        => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

    // ── HẠ TẦNG ──────────────────────────────────────────────────────────────

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    private delegate Color PixelFn(float u, float v, int w, int h);

    private static Sprite Make(string key, int w, int h, PixelFn fn, Vector4 border)
    {
        if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name       = "ConstructionTex_" + key,
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags  = HideFlags.HideAndDontSave
        };

        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // u,v ∈ [-1,1], gốc ở tâm ảnh (đồng bộ với PopupSpriteFactory)
                float u = (x + 0.5f) / w * 2f - 1f;
                float v = (y + 0.5f) / h * 2f - 1f;
                px[y * w + x] = fn(u, v, w, h);
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        Sprite sprite = Sprite.Create(
            tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, border);
        sprite.name      = key;
        sprite.hideFlags = HideFlags.HideAndDontSave;

        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>SDF chữ nhật bo góc. p theo pixel, b = nửa kích thước, r = bán kính bo.</summary>
    private static float SdRoundBox(Vector2 p, Vector2 b, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    /// <summary>SDF đoạn thẳng a→b, dùng vẽ kim đồng hồ và dấu tick.</summary>
    private static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a;
        Vector2 ba = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-5f, Vector2.Dot(ba, ba)));
        return (pa - ba * t).magnitude;
    }

    private static float Cross2(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

    /// <summary>
    /// SDF tam giác a-b-c (âm = trong, dương = ngoài).
    /// Ghép từ <see cref="SdSegment"/> + phép thử dấu tích có hướng — không cần biết trước
    /// tam giác quay chiều nào (nhận cả hai chiều), nên gọi được với toạ độ viết tay.
    /// Dùng cho mũi nhọn của nút XOAY.
    /// </summary>
    private static float SdTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d = Mathf.Min(SdSegment(p, a, b),
                  Mathf.Min(SdSegment(p, b, c), SdSegment(p, c, a)));

        float s1 = Cross2(b - a, p - a);
        float s2 = Cross2(c - b, p - b);
        float s3 = Cross2(a - c, p - c);
        bool inside = (s1 >= 0f && s2 >= 0f && s3 >= 0f) || (s1 <= 0f && s2 <= 0f && s3 <= 0f);
        return inside ? -d : d;
    }

    /// <summary>Signed distance → alpha có khử răng cưa.</summary>
    private static float Aa(float sd, float softness = 1.4f)
        => Mathf.Clamp01(0.5f - sd / softness);

    /// <summary>Chồng src LÊN TRÊN dst (alpha "over").</summary>
    private static Color Over(Color src, Color dst)
    {
        float a = src.a + dst.a * (1f - src.a);
        if (a <= 0.0001f) return Color.clear;
        Vector3 rgb = (new Vector3(src.r, src.g, src.b) * src.a
                     + new Vector3(dst.r, dst.g, dst.b) * dst.a * (1f - src.a)) / a;
        return new Color(rgb.x, rgb.y, rgb.z, a);
    }

    private static Color WithA(Color c, float a) { c.a = a; return c; }

    // ── SPRITE DÙNG CHUNG ────────────────────────────────────────────────────

    /// <summary>
    /// Chữ nhật TRẮNG bo góc, có border 9-slice → kéo giãn tuỳ ý không méo góc.
    /// Tô màu bằng Image.color / SpriteRenderer.color (nền tối, thảm đất, ruy băng…).
    /// </summary>
    public static Sprite Panel(int w = 96, int h = 96, int radius = 24)
    {
        radius = Mathf.Clamp(radius, 1, Mathf.Min(w, h) / 2 - 1);
        int r = radius;
        return Make($"panel_{w}_{h}_{r}", w, h,
            (u, v, ww, hh) =>
            {
                Vector2 p = new Vector2(u * ww * 0.5f, v * hh * 0.5f);
                float sd = SdRoundBox(p, new Vector2(ww * 0.5f - 1f, hh * 0.5f - 1f), r);
                float a  = Aa(sd);
                return a <= 0.001f ? Color.clear : WithA(Color.white, a);
            },
            new Vector4(r + 2, r + 2, r + 2, r + 2));
    }

    /// <summary>Đĩa tròn trắng đặc.</summary>
    public static Sprite Circle(int size = 64)
        => Make($"circle_{size}", size, size,
            (u, v, w, h) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                float unit = 2f / w;
                float a = Mathf.Clamp01((0.98f - d) / (unit * 1.6f));
                return a <= 0.001f ? Color.clear : WithA(Color.white, a);
            },
            Vector4.zero);

    /// <summary>Chấm mờ dần ra rìa — hạt khói bụi.</summary>
    public static Sprite SoftDot(int size = 64)
        => Make($"softdot_{size}", size, size,
            (u, v, w, h) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                return a <= 0.002f ? Color.clear : WithA(Color.white, a);
            },
            Vector4.zero);

    /// <summary>Nút xanh lá bo góc kiểu Township: gradient dọc + viền đậm + dải bóng trên.</summary>
    public static Sprite GreenButton(int w = 160, int h = 72, int radius = 26)
    {
        radius = Mathf.Clamp(radius, 1, Mathf.Min(w, h) / 2 - 1);
        int r = radius;
        return Make($"greenbtn_{w}_{h}_{r}", w, h,
            (u, v, ww, hh) =>
            {
                Vector2 p = new Vector2(u * ww * 0.5f, v * hh * 0.5f);
                float sd  = SdRoundBox(p, new Vector2(ww * 0.5f - 1f, hh * 0.5f - 1f), r);
                float aOut = Aa(sd);
                if (aOut <= 0.001f) return Color.clear;

                float aIn = Aa(sd + 5f);                       // lõi bên trong viền
                float t   = Mathf.InverseLerp(1f, -1f, v);     // 0 = trên, 1 = dưới
                Color g   = t < 0.5f
                    ? Color.Lerp(GreenTop, GreenMid, t / 0.5f)
                    : Color.Lerp(GreenMid, GreenBot, (t - 0.5f) / 0.5f);

                // dải bóng dồn vào 26 % trên cùng
                float gloss = Mathf.Clamp01(1f - t / 0.26f);
                g = Color.Lerp(g, Color.white, gloss * gloss * 0.28f);

                return WithA(Color.Lerp(GreenEdge, g, aIn), aOut);
            },
            new Vector4(r + 4, r + 4, r + 4, r + 4));
    }

    // ── GIÀN GIÁO ────────────────────────────────────────────────────────────

    /// <summary>
    /// Thanh gỗ nằm ngang: bo góc nhẹ, gradient sáng-tối, 2 vệt vân, viền nâu đậm.
    /// Cọc đứng dùng CHÍNH sprite này rồi xoay transform 90° — đỡ sinh thêm texture.
    /// </summary>
    public static Sprite Plank(int w = 192, int h = 40)
        => Make($"plank_{w}_{h}", w, h,
            (u, v, ww, hh) =>
            {
                Vector2 p = new Vector2(u * ww * 0.5f, v * hh * 0.5f);
                float sd  = SdRoundBox(p, new Vector2(ww * 0.5f - 1f, hh * 0.5f - 1f), hh * 0.22f);
                float aOut = Aa(sd);
                if (aOut <= 0.001f) return Color.clear;

                float aIn = Aa(sd + 2.5f);
                float t   = Mathf.InverseLerp(1f, -1f, v);
                Color body = t < 0.45f
                    ? Color.Lerp(WoodLight, WoodMid, t / 0.45f)
                    : Color.Lerp(WoodMid, WoodDark, (t - 0.45f) / 0.55f);

                // 2 vệt vân gỗ chạy dọc thân
                float grain = Mathf.Abs(Mathf.Sin((v * 2.3f + 0.6f) * Mathf.PI));
                body = Color.Lerp(body, WoodDark, (1f - grain) * 0.22f);

                return WithA(Color.Lerp(WoodEdge, body, aIn), aOut);
            },
            new Vector4(hh_(h), 0f, hh_(h), 0f));

    private static float hh_(int h) => Mathf.Max(2f, h * 0.30f);

    // ── ICON ─────────────────────────────────────────────────────────────────

    /// <summary>Đồng hồ tròn: vành đậm, mặt kem, 2 kim chỉ 10 h 10.</summary>
    public static Sprite ClockIcon(int size = 72)
        => Make($"clock_{size}", size, size,
            (u, v, w, h) =>
            {
                Vector2 p = new Vector2(u, v);
                float d = p.magnitude;
                float unit = 2f / w;
                float soft = unit * 1.6f;

                float aOuter = Mathf.Clamp01((0.94f - d) / soft);
                if (aOuter <= 0.001f) return Color.clear;

                float aFace = Mathf.Clamp01((0.76f - d) / soft);
                Color c = Color.Lerp(ClockRim, ClockFace, aFace);

                // kim giờ + kim phút (dày ~7 % đường kính)
                float hand = Mathf.Min(
                    SdSegment(p, Vector2.zero, new Vector2(0.00f, 0.50f)),
                    SdSegment(p, Vector2.zero, new Vector2(0.34f, 0.16f)));
                float aHand = Mathf.Clamp01((0.075f - hand) / soft) * aFace;
                c = Color.Lerp(c, ClockRim, aHand);

                return WithA(c, aOuter);
            },
            Vector4.zero);

    /// <summary>Đồng xu vàng — 3 vòng tròn đồng tâm cho ra khối.</summary>
    public static Sprite CoinIcon(int size = 72)
        => Make($"coin_{size}", size, size,
            (u, v, w, h) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                float unit = 2f / w;
                float soft = unit * 1.6f;

                float aOut = Mathf.Clamp01((0.95f - d) / soft);
                if (aOut <= 0.001f) return Color.clear;

                float aMid  = Mathf.Clamp01((0.80f - d) / soft);
                float aCore = Mathf.Clamp01((0.46f - d) / soft);

                Color c = CoinEdge;
                c = Color.Lerp(c, CoinMid, aMid);
                c = Color.Lerp(c, CoinCore, aCore * 0.85f);
                // bóng sáng lệch trên-trái
                float gloss = Mathf.Clamp01(1f - new Vector2(u + 0.28f, v - 0.30f).magnitude / 0.34f);
                c = Color.Lerp(c, Color.white, gloss * 0.45f);

                return WithA(c, aOut);
            },
            Vector4.zero);

    /// <summary>Kim cương — hình thoi có mặt cắt sáng ở đỉnh.</summary>
    public static Sprite GemIcon(int size = 72)
        => Make($"gem_{size}", size, size,
            (u, v, w, h) =>
            {
                // hình thoi: |u|/0.72 + |v|/0.92 <= 1
                float k = Mathf.Abs(u) / 0.72f + Mathf.Abs(v) / 0.92f;
                float unit = 2f / w;
                float a = Mathf.Clamp01((1f - k) / (unit * 2.4f));
                if (a <= 0.001f) return Color.clear;

                float aIn = Mathf.Clamp01((0.90f - k) / (unit * 2.4f));
                Color body = Color.Lerp(GemMid, GemCore, Mathf.Clamp01((v + 0.4f) / 1.2f));
                Color c = Color.Lerp(GemEdge, body, aIn);

                // mặt cắt ngang phía trên
                if (v > 0.30f && v < 0.40f) c = Color.Lerp(c, GemCore, 0.6f);
                return WithA(c, a);
            },
            Vector4.zero);

    /// <summary>Dấu tick — 2 đoạn thẳng bo đầu. Trắng, tô màu lại bằng color.</summary>
    public static Sprite CheckMark(int size = 72)
        => Make($"check_{size}", size, size,
            (u, v, w, h) =>
            {
                Vector2 p = new Vector2(u, v);
                float d = Mathf.Min(
                    SdSegment(p, new Vector2(-0.60f, 0.02f), new Vector2(-0.14f, -0.46f)),
                    SdSegment(p, new Vector2(-0.14f, -0.46f), new Vector2(0.64f, 0.52f)));
                float unit = 2f / w;
                float a = Mathf.Clamp01((0.17f - d) / (unit * 1.8f));
                return a <= 0.001f ? Color.clear : WithA(Color.white, a);
            },
            Vector4.zero);

    /// <summary>Mũ bảo hộ công trường: vòm + vành + gờ giữa.</summary>
    public static Sprite HardHat(int size = 96)
        => Make($"hardhat_{size}", size, size,
            (u, v, w, h) =>
            {
                Vector2 p = new Vector2(u, v);

                // Vòm = nửa TRÊN của đường tròn bán kính 0.62, tâm hạ xuống -0.10
                Vector2 domeP = new Vector2(u, v + 0.10f);
                float domeSd = domeP.magnitude - 0.62f;
                float unit = 2f / w;
                float soft = unit * 1.8f;
                float aDome = (v > -0.10f) ? Mathf.Clamp01(-domeSd / soft) : 0f;

                // Vành mũ
                float brimSd = SdRoundBox(new Vector2(u, v + 0.16f), new Vector2(0.88f, 0.10f), 0.09f);
                float aBrim = Mathf.Clamp01(-brimSd / soft);

                float a = Mathf.Max(aDome, aBrim);
                if (a <= 0.001f) return Color.clear;

                Color c = Color.Lerp(HatYellow, HatEdge, Mathf.InverseLerp(0.6f, -0.4f, v) * 0.55f);

                // gờ giữa nổi
                float ridge = SdRoundBox(new Vector2(u, v - 0.06f), new Vector2(0.10f, 0.42f), 0.09f);
                c = Color.Lerp(c, Color.Lerp(HatYellow, Color.white, 0.35f), Mathf.Clamp01(-ridge / soft) * 0.55f);

                return WithA(c, a);
            },
            Vector4.zero);

    /// <summary>Bóng bay: bầu tròn hơi nhọn dưới + núm thắt. Trắng, tô màu bằng color.</summary>
    public static Sprite Balloon(int w = 64, int h = 84)
        => Make($"balloon_{w}_{h}", w, h,
            (u, v, ww, hh) =>
            {
                // Bầu = ellipse hơi kéo dài, đáy vuốt nhọn
                float taper = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01((-v - 0.05f) / 0.95f));
                float k = Mathf.Sqrt((u / (0.72f * taper)) * (u / (0.72f * taper))
                                   + ((v - 0.10f) / 0.82f) * ((v - 0.10f) / 0.82f));
                float unit = 2f / ww;
                float a = Mathf.Clamp01((1f - k) / (unit * 2.2f));

                // núm thắt
                float knot = SdRoundBox(new Vector2(u, v + 0.86f), new Vector2(0.09f, 0.07f), 0.05f);
                a = Mathf.Max(a, Mathf.Clamp01(-knot / (unit * 2.2f)));
                if (a <= 0.001f) return Color.clear;

                // Thân để hơi tối (0.82) rồi đốm sáng kéo về trắng: khi tô màu bằng
                // SpriteRenderer.color (nhân) thì chỗ đốm sáng lên rõ hơn phần thân.
                float gloss = Mathf.Clamp01(1f - new Vector2(u + 0.26f, v - 0.38f).magnitude / 0.30f);
                Color c = Color.Lerp(new Color(0.82f, 0.82f, 0.82f, 1f), Color.white, gloss);
                return WithA(c, a);
            },
            Vector4.zero);

    // ── V6 — GLYPH CHO 3 NÚT TRÒN CỦA THANH XÁC NHẬN ─────────────────────────
    //
    // VÌ SAO VẼ BẰNG CODE THAY VÌ DÙNG KÝ TỰ TMP (✕ ↻ ✓):
    // prefab Placement_Ghost có sẵn 3 node "Label" chứa ký tự Unicode nhưng cả 3 đang
    // TẮT (m_IsActive: 0). Bật lên là đánh cược vào việc font TMP mặc định có đủ 3 glyph
    // đó — thiếu một cái là hiện ô vuông trống. Sprite thủ tục thì chắc chắn hiện, và
    // đằng nào cũng cùng đường với 19 ô art khác (Edric thay sprite thật sau).

    /// <summary>Dấu ✕ — hai đoạn thẳng bo đầu cắt nhau. Trắng, tô màu lại bằng color.</summary>
    public static Sprite CrossMark(int size = 72)
        => Make($"cross_{size}", size, size,
            (u, v, w, h) =>
            {
                Vector2 p = new Vector2(u, v);
                float d = Mathf.Min(
                    SdSegment(p, new Vector2(-0.50f, -0.50f), new Vector2(0.50f,  0.50f)),
                    SdSegment(p, new Vector2(-0.50f,  0.50f), new Vector2(0.50f, -0.50f)));
                float unit = 2f / w;
                float a = Mathf.Clamp01((0.16f - d) / (unit * 1.8f));
                return a <= 0.001f ? Color.clear : WithA(Color.white, a);
            },
            Vector4.zero);

    /// <summary>
    /// THÙNG RÁC 🗑 — dùng cho nút XOÁ công trình trong Edit Mode.
    ///
    /// Ghép 4 khối chữ nhật bo góc: nắp (thanh ngang trên), tay cầm (thanh nhỏ trên nắp),
    /// thân thùng (hơi thu hẹp xuống dưới cho ra dáng thùng), và 2 khe dọc khoét trong thân.
    /// Vẽ bằng SDF thay vì import icon để không phụ thuộc art — Edric thay sprite sau nếu muốn.
    /// </summary>
    public static Sprite TrashCan(int size = 72)
        => Make($"trash_{size}", size, size,
            (u, v, w, h) =>
            {
                Vector2 p    = new Vector2(u, v);
                float   unit = 2f / w;
                float   aa   = unit * 1.8f;

                // ── Nắp: thanh ngang ở y = +0.46 ──
                float dLid = SdRoundBox(p - new Vector2(0f, 0.46f),
                                        new Vector2(0.52f, 0.085f), 0.05f);

                // ── Tay cầm: thanh nhỏ nhô lên trên nắp ──
                float dGrip = SdRoundBox(p - new Vector2(0f, 0.60f),
                                         new Vector2(0.20f, 0.075f), 0.05f);

                // ── Thân thùng: thu hẹp dần xuống dưới ──
                // Nội suy nửa-chiều-rộng theo y: rộng 0.44 ở mép trên, 0.34 ở đáy.
                float tBody = Mathf.InverseLerp(0.34f, -0.62f, p.y);   // 0 ở trên → 1 ở đáy
                float halfW = Mathf.Lerp(0.44f, 0.34f, Mathf.Clamp01(tBody));
                float dBody = SdRoundBox(p - new Vector2(0f, -0.14f),
                                         new Vector2(halfW, 0.48f), 0.07f);

                // Gộp 3 khối
                float d = Mathf.Min(dLid, Mathf.Min(dGrip, dBody));
                float a = Mathf.Clamp01((0f - d) / aa);
                if (a <= 0.001f) return Color.clear;

                // ── Khoét 2 khe dọc trong thân (chỉ khoét phần thân, không đụng nắp) ──
                if (p.y < 0.30f)
                {
                    float slot = Mathf.Min(
                        SdRoundBox(p - new Vector2(-0.16f, -0.16f), new Vector2(0.045f, 0.30f), 0.04f),
                        SdRoundBox(p - new Vector2( 0.16f, -0.16f), new Vector2(0.045f, 0.30f), 0.04f));
                    float slotA = Mathf.Clamp01((0f - slot) / aa);
                    a *= 1f - slotA;
                    if (a <= 0.001f) return Color.clear;
                }

                return WithA(Color.white, a);
            },
            Vector4.zero);

    /// <summary>
    /// Mũi tên XOAY ↻ — vòng cung hở + mũi nhọn ở đầu cung.
    /// Khoảng hở (18°…96°) là thứ làm nó khác chữ "O": mắt phải thấy vòng cung CHƯA KHÉP
    /// mới đọc ra "quay". Mũi nhọn chỉ theo chiều ngược kim đồng hồ, khớp ký hiệu ↻ quen mắt.
    /// </summary>
    public static Sprite RotateArrow(int size = 72)
    {
        const float ringR   = 0.52f;                 // bán kính vòng cung
        const float ringHalf= 0.115f;                // nửa độ dày
        const float gapFrom = 18f;                   // độ — đầu cung có mũi nhọn
        const float gapTo   = 96f;                   // độ — đuôi cung

        float ca = Mathf.Cos(gapFrom * Mathf.Deg2Rad);
        float sa = Mathf.Sin(gapFrom * Mathf.Deg2Rad);
        Vector2 radial  = new Vector2(ca, sa);                 // hướng ra xa tâm tại 18°
        Vector2 tangent = new Vector2(-sa, ca);                // hướng ngược kim đồng hồ
        Vector2 baseIn  = radial * (ringR - 0.20f);
        Vector2 baseOut = radial * (ringR + 0.20f);
        Vector2 tip     = radial * ringR + tangent * 0.30f;

        return Make($"rotarrow_{size}", size, size,
            (u, v, w, h) =>
            {
                Vector2 p = new Vector2(u, v);
                float unit = 2f / w;
                float soft = unit * 1.8f;

                float ringSd = Mathf.Abs(p.magnitude - ringR) - ringHalf;
                float ang    = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;   // −180…180
                bool  inGap  = ang > gapFrom && ang < gapTo;
                float aRing  = inGap ? 0f : Mathf.Clamp01(-ringSd / soft);

                float aTip = Mathf.Clamp01(-SdTriangle(p, baseIn, baseOut, tip) / soft);

                float a = Mathf.Max(aRing, aTip);
                return a <= 0.001f ? Color.clear : WithA(Color.white, a);
            },
            Vector4.zero);
    }

    // ── V9 — ICON TRẠNG THÁI NỔI TRÊN ĐẦU CÔNG TRÌNH ─────────────────────────

    /// <summary>
    /// NGÔI SAO 5 CÁNH — "thưởng XP đang chờ".
    /// Cách vẽ: gập góc cực về MỘT nan quạt 1/5 rồi thử điểm nằm trong hay ngoài đường
    /// thẳng nối ĐỈNH cánh với HÕM giữa hai cánh. Rẻ hơn dựng đa giác 10 đỉnh, và có sẵn
    /// khử răng cưa vì làm việc bằng khoảng cách có dấu.
    /// </summary>
    public static Sprite Star(int size = 80)
    {
        const float rOut = 0.94f;                 // bán kính đỉnh cánh
        const float rIn  = 0.42f;                 // bán kính hõm
        float seg  = Mathf.PI * 2f / 5f;
        Vector2 tipP   = new Vector2(0f, rOut);
        Vector2 notchP = new Vector2(Mathf.Sin(seg * 0.5f) * rIn, Mathf.Cos(seg * 0.5f) * rIn);
        // Pháp tuyến HƯỚNG RA NGOÀI của cạnh tip→notch (suy tay: xem ghi chú trong FxEase
        // về dấu — ở đây tip.y > notch.y và notch.x > 0 nên (rOut−y, x) chỉ ra ngoài).
        Vector2 nOut = new Vector2(tipP.y - notchP.y, notchP.x - tipP.x).normalized;

        return Make($"star_{size}", size, size,
            (u, v, w, h) =>
            {
                float rr = Mathf.Sqrt(u * u + v * v);
                if (rr > rOut + 0.05f) return Color.clear;

                // Atan2(u, v) cho góc TÍNH TỪ TRỤC +Y → một cánh chỉ thẳng lên, dễ nhìn.
                float ang = Mathf.Atan2(u, v);
                ang = Mathf.Abs(Mathf.Repeat(ang + seg * 0.5f, seg) - seg * 0.5f);

                Vector2 p = new Vector2(Mathf.Sin(ang) * rr, Mathf.Cos(ang) * rr);
                float sd = Vector2.Dot(p - tipP, nOut);      // <0 = trong ngôi sao

                float a = Mathf.Clamp01(-sd / (2f / w * 1.8f));
                return a <= 0.001f ? Color.clear : WithA(Color.white, a);
            },
            Vector4.zero);
    }

    /// <summary>
    /// CHỮ "Z" — "máy đứng không, thiếu nguyên liệu".
    /// Tài liệu Township gọi đây là "chi tiết tinh tế nhất" (§3): nhà máy hết hàng thì
    /// "ngủ", người chơi thấy Z là biết phải nạp liệu, KHÔNG cần một dòng chữ nào.
    /// Ba đoạn thẳng — gạch trên, gạch chéo, gạch dưới.
    /// </summary>
    public static Sprite LetterZ(int size = 72)
        => Make($"letterz_{size}", size, size,
            (u, v, w, h) =>
            {
                Vector2 p = new Vector2(u, v);
                float d = Mathf.Min(Mathf.Min(
                    SdSegment(p, new Vector2(-0.44f,  0.54f), new Vector2( 0.44f,  0.54f)),
                    SdSegment(p, new Vector2( 0.44f,  0.54f), new Vector2(-0.44f, -0.54f))),
                    SdSegment(p, new Vector2(-0.44f, -0.54f), new Vector2( 0.44f, -0.54f)));
                float unit = 2f / w;
                float a = Mathf.Clamp01((0.145f - d) / (unit * 1.8f));
                return a <= 0.001f ? Color.clear : WithA(Color.white, a);
            },
            Vector4.zero);

    /// <summary>
    /// BÌNH SỮA — "sản phẩm đã xong, chạm để thu". Placeholder chung cho mọi sản phẩm;
    /// công trình nào có sprite riêng thì truyền vào BuildingStatusIcon.productSprite.
    /// Thân + cổ + nắp (nắp tô xám để mắt đọc ra 3 khối, không thành một cục trắng).
    /// </summary>
    public static Sprite MilkBottle(int w = 72, int h = 96)
        => Make($"milk_{w}_{h}", w, h,
            (u, v, ww, hh) =>
            {
                Color c = Color.clear;
                float unit = 2f / ww;
                float soft = unit * 2f;

                float body = SdRoundBox(new Vector2(u, v + 0.28f), new Vector2(0.44f, 0.50f), 0.16f);
                c = Over(WithA(Color.white, Mathf.Clamp01(-body / soft)), c);

                float neck = SdRoundBox(new Vector2(u, v - 0.50f), new Vector2(0.19f, 0.22f), 0.07f);
                c = Over(WithA(Color.white, Mathf.Clamp01(-neck / soft)), c);

                float cap = SdRoundBox(new Vector2(u, v - 0.74f), new Vector2(0.25f, 0.10f), 0.05f);
                c = Over(WithA(new Color(0.60f, 0.62f, 0.66f, 1f), Mathf.Clamp01(-cap / soft)), c);

                return c;
            },
            Vector4.zero);

    /// <summary>
    /// Bóng công nhân tạm thời (đội mũ bảo hộ). CHỈ LÀ PLACEHOLDER —
    /// Edric gán art thật vào ConstructionManager.workerSprite là sprite này biến mất.
    /// </summary>
    public static Sprite WorkerSilhouette(int w = 72, int h = 108)
        => Make($"worker_{w}_{h}", w, h,
            (u, v, ww, hh) =>
            {
                Color c = Color.clear;
                float unit = 2f / ww;
                float soft = unit * 2.0f;

                // Thân (áo)
                float body = SdRoundBox(new Vector2(u, v + 0.42f), new Vector2(0.34f, 0.40f), 0.16f);
                c = Over(WithA(WorkerBody, Mathf.Clamp01(-body / soft)), c);

                // Đầu
                float head = new Vector2(u, v - 0.30f).magnitude - 0.26f;
                c = Over(WithA(WorkerSkin, Mathf.Clamp01(-head / soft)), c);

                // Mũ bảo hộ: vòm + vành
                float dome = new Vector2(u, v - 0.34f).magnitude - 0.30f;
                float aDome = (v > -0.34f) ? Mathf.Clamp01(-dome / soft) : 0f;
                float brim = SdRoundBox(new Vector2(u, v - 0.36f), new Vector2(0.40f, 0.045f), 0.04f);
                float aHat = Mathf.Max(aDome, Mathf.Clamp01(-brim / soft));
                c = Over(WithA(HatYellow, aHat), c);

                return c;
            },
            Vector4.zero);
}
