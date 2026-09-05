using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MŨI TÊN WORLD-SPACE chỉ xuống ngôi nhà đích ("có 1 mũi tên đi tới đứng trước nhà"
/// — yêu cầu Sếp). Tự dựng 100% runtime theo pattern <see cref="GuideTapHintFX"/>:
/// 1 GameObject + 1 <see cref="SpriteRenderer"/>, không cần prefab/asset/scene setup.
///
///   <see cref="ShowAbove"/> — bật mũi tên trên đầu 1 công trình (1 nhà = 1 mũi tên)
///   <see cref="Hide"/>      — tắt mũi tên này
///   <see cref="HideAll"/>   — dọn sạch mọi mũi tên
///
/// <c>arrowSprite</c> trong config để trống ⇒ sinh sprite mũi tên CHỈ XUỐNG bằng
/// <see cref="Texture2D"/> vẽ bằng code: thân chữ nhật + đầu tam giác, viền nâu đậm
/// <c>#4A2B14</c> theo style studio. Khử răng cưa bằng khoảng cách CÓ DẤU tới cạnh
/// (signed distance) chứ không phải vẽ 4× rồi thu nhỏ — rẻ hơn và mép sạch hơn.
/// Sprite được CACHE STATIC, cả phiên chỉ tạo MỘT texture.
///
/// ⚠ REGISTRY STATIC: mỗi <c>host</c> chỉ có ĐÚNG 1 mũi tên. Gọi
/// <see cref="ShowAbove"/> hai lần trên cùng ngôi nhà thì trả lại mũi tên cũ chứ
/// không xếp chồng 2 cái lên nhau.
/// ⚠ <c>host</c> bị Destroy (người chơi xoá nhà) ⇒ mũi tên tự <c>Destroy</c> chính nó.
/// </summary>
public class DeliveryArrowFX : MonoBehaviour
{
    /// <summary>Sorting order — phải nằm trên mọi thứ (nhà, cây, NPC).</summary>
    private const int ArrowSortingOrder = 31000;

    /// <summary>Màu viền nâu đậm theo style studio.</summary>
    private static readonly Color32 OutlineColor = new Color32(0x4A, 0x2B, 0x14, 0xFF);

    /// <summary>Màu thân mũi tên (vàng ấm, dễ thấy trên nền cỏ/mái nhà).</summary>
    private static readonly Color32 BodyColor = new Color32(0xFF, 0xC1, 0x3B, 0xFF);

    /// <summary>Cạnh texture sinh bằng code.</summary>
    private const int TexSize = 96;

    // ─── Static ─────────────────────────────────────────────────────────

    private static readonly List<DeliveryArrowFX> _active = new List<DeliveryArrowFX>(4);
    private static Sprite _generatedArrow;

    /// <summary>
    /// Bật mũi tên bồng bềnh trên đầu <paramref name="house"/>.
    /// Đã có mũi tên trên nhà đó ⇒ trả lại cái cũ (không xếp chồng).
    /// <paramref name="house"/> hoặc <paramref name="cfg"/> null ⇒ trả null, không lỗi.
    /// </summary>
    public static DeliveryArrowFX ShowAbove(Transform house, ShipperConfig cfg)
    {
        if (house == null || cfg == null) return null;

        // dọn các entry đã chết + tìm mũi tên sẵn có của nhà này
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            DeliveryArrowFX fx = _active[i];
            if (fx == null) { _active.RemoveAt(i); continue; }
            if (fx.Host == house) return fx;
        }

        var go = new GameObject("DeliveryArrowFX");
        var arrow = go.AddComponent<DeliveryArrowFX>();
        arrow.Init(house, cfg);
        _active.Add(arrow);
        return arrow;
    }

    /// <summary>Dọn sạch MỌI mũi tên đang bật (dùng khi reload scene / tắt hệ shipper).</summary>
    public static void HideAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            DeliveryArrowFX fx = _active[i];
            if (fx != null) Destroy(fx.gameObject);
        }
        _active.Clear();
    }

    /// <summary>Tắt mũi tên của đúng một công trình. Không có thì bỏ qua êm.</summary>
    public static void HideFor(Transform house)
    {
        if (house == null) return;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            DeliveryArrowFX fx = _active[i];
            if (fx == null) { _active.RemoveAt(i); continue; }
            if (fx.Host != house) continue;
            _active.RemoveAt(i);
            Destroy(fx.gameObject);
        }
    }

    // ─── Runtime ────────────────────────────────────────────────────────

    private ShipperConfig  _cfg;
    private SpriteRenderer _sr;
    private float          _t0;
    private float          _baseScale = 1f;

    /// <summary>Công trình mà mũi tên đang chỉ vào. Null nghĩa là đã bị Destroy.</summary>
    public Transform Host { get; private set; }

    /// <summary>Tắt mũi tên này ngay.</summary>
    public void Hide()
    {
        _active.Remove(this);
        if (this != null) Destroy(gameObject);
    }

    private void Init(Transform host, ShipperConfig cfg)
    {
        Host = host;
        _cfg = cfg;
        _t0  = Time.time;

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite           = cfg.arrowSprite != null ? cfg.arrowSprite : GeneratedArrow();
        _sr.sortingLayerName = TouristSortingLayers.Resolve(TouristSortingLayers.Overlay);
        _sr.sortingOrder     = ArrowSortingOrder;

        // scale để chiều cao mũi tên = arrowWorldSize
        if (_sr.sprite != null && _sr.sprite.bounds.size.y > 0.0001f)
            _baseScale = cfg.SafeArrowWorldSize / _sr.sprite.bounds.size.y;
        transform.localScale = new Vector3(_baseScale, _baseScale, 1f);

        Reposition(0f, 1f);
    }

    private void LateUpdate()
    {
        // host biến mất (người chơi xoá/di chuyển nhà) ⇒ tự dọn, KHÔNG NullReference
        if (Host == null)
        {
            _active.Remove(this);
            Destroy(gameObject);
            return;
        }

        if (_cfg == null) return;

        // FX dùng giây THỰC (CONTRACT §0.6) — vẫn bồng bềnh khi game pause bởi popup
        float t      = Time.unscaledTime - _t0;
        float period = _cfg.SafeArrowBobPeriod;
        float phase  = t * Mathf.PI * 2f / period;

        float bob = Mathf.Sin(phase) * _cfg.arrowBobPixels;
        // scale lệch pha 1/4 vòng — giống FloatingIconBob
        float pulse = 1f + 0.06f * Mathf.Sin(phase - Mathf.PI * 0.5f);

        Reposition(bob, pulse);
    }

    /// <summary>
    /// Bám theo ĐỈNH nhà mỗi frame — nhà đổi sprite/stage (Building → hộp quà →
    /// hoàn thiện) là bounds đổi theo, mũi tên phải nhảy lên chứ không được nằm trong mái.
    /// </summary>
    private void Reposition(float bobY, float pulse)
    {
        if (Host == null) return;

        Vector3 p = Host.position;
        float topY;

        Bounds b;
        if (VillageRoadRing.TryGetVisualBounds(Host, out b)) topY = b.max.y;
        else                                                 topY = p.y;

        transform.position = new Vector3(p.x,
                                         topY + _cfg.arrowHeightAboveHouse + bobY,
                                         p.z);
        transform.localScale = new Vector3(_baseScale * pulse, _baseScale * pulse, 1f);
    }

    private void OnDestroy()
    {
        _active.Remove(this);
    }

    // ─── Sprite mũi tên vẽ bằng code (cache static, tạo 1 lần) ──────────

    /// <summary>
    /// Mũi tên CHỈ XUỐNG: thân chữ nhật ở trên + đầu tam giác ở dưới, viền nâu đậm.
    /// Khử răng cưa bằng signed distance tới biên hình (mép mượt ở mọi kích thước scale).
    /// </summary>
    private static Sprite GeneratedArrow()
    {
        if (_generatedArrow != null) return _generatedArrow;

        const int S = TexSize;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            name       = "ShipperDeliveryArrow",
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };

        // hệ toạ độ chuẩn hoá: u,v ∈ [-1, 1], v = +1 ở TRÊN
        // thân: |u| <= 0.26, v ∈ [0.02, 0.92]
        // đầu tam giác: đỉnh (0, -0.94), hai vai (±0.62, 0.04)
        Vector2 tip = new Vector2(0f, -0.94f);
        Vector2 shL = new Vector2(-0.62f, 0.04f);
        Vector2 shR = new Vector2(0.62f, 0.04f);

        float unit    = 2f / S;
        float soft    = unit * 1.6f;      // độ mượt mép
        float outline = unit * 5.0f;      // dày viền

        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            float v = (y + 0.5f) * 2f / S - 1f;
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) * 2f / S - 1f;
                var p = new Vector2(u, v);

                float sdBody = SdBox(new Vector2(u, v - 0.47f), new Vector2(0.26f, 0.45f));
                float sdHead = SdTriangle(p, tip, shL, shR);
                float sd     = Mathf.Min(sdBody, sdHead);   // hợp 2 hình

                float aOuter = Mathf.Clamp01(-sd / soft);                 // cả hình + viền
                float aInner = Mathf.Clamp01(-(sd + outline) / soft);     // phần lõi

                if (aOuter <= 0.003f) { px[y * S + x] = new Color32(0, 0, 0, 0); continue; }

                // lõi vàng đè lên nền viền nâu
                Color32 c = Lerp32(OutlineColor, BodyColor, aInner);
                c.a = (byte)Mathf.RoundToInt(aOuter * 255f);
                px[y * S + x] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);

        // PPU = S ⇒ bounds đúng 1×1 world unit, scale sau này là kích thước thật
        _generatedArrow = Sprite.Create(tex, new Rect(0f, 0f, S, S), new Vector2(0.5f, 0.5f), S);
        _generatedArrow.name = "ShipperDeliveryArrow";
        return _generatedArrow;
    }

    /// <summary>Khoảng cách CÓ DẤU tới hình chữ nhật nửa-kích-thước <paramref name="b"/>.</summary>
    private static float SdBox(Vector2 p, Vector2 b)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y);
        float outside = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude;
        float inside  = Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        return outside + inside;
    }

    /// <summary>Khoảng cách CÓ DẤU tới tam giác a-b-c (âm = bên trong).</summary>
    private static float SdTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d = Mathf.Min(Mathf.Min(SdSegment(p, a, b), SdSegment(p, b, c)), SdSegment(p, c, a));

        float s1 = Cross2(b - a, p - a);
        float s2 = Cross2(c - b, p - b);
        float s3 = Cross2(a - c, p - c);
        bool inside = (s1 >= 0f && s2 >= 0f && s3 >= 0f) || (s1 <= 0f && s2 <= 0f && s3 <= 0f);

        return inside ? -d : d;
    }

    private static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float l2 = ab.sqrMagnitude;
        float t = l2 < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
        return (p - (a + ab * t)).magnitude;
    }

    private static float Cross2(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

    private static Color32 Lerp32(Color32 from, Color32 to, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32(
            (byte)Mathf.RoundToInt(from.r + (to.r - from.r) * t),
            (byte)Mathf.RoundToInt(from.g + (to.g - from.g) * t),
            (byte)Mathf.RoundToInt(from.b + (to.b - from.b) * t),
            255);
    }
}
