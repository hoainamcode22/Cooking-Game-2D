using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// ĂN MỪNG KHÁNH THÀNH V2 — chuỗi Township "nhiều đợt, SO LE, TRƯỚC công trình".
/// ══════════════════════════════════════════════════════════════════════════
///
/// GỌI MỘT DÒNG:  <c>ConstructionCelebrationFX.Play(buildingTransform, expReward);</c>
/// (expReward = 0 thì chỉ hiện ngôi sao, không hiện số "+N".)
///
/// TIMELINE (tổng ≤ 3.5 giây, tự Destroy root, không leak):
///     0.00s  Đợt 1 — 4–6 cụm khói poof quanh CHÂN công trình (tròn mềm, nở ra, tan)
///     0.10s  Đợt 2 — sao EXP (+ số "+N" nếu expReward &gt; 0) nảy ra từ ĐỈNH rồi bay lên
///     0.20s  Đợt 3 — 4 đợt confetti burst SO LE (0.2 / 0.65 / 1.10 / 1.50s) quanh
///                     NỬA TRÊN công trình: sao 4 cánh + chấm tròn + vuông xoay,
///                     văng parabol trọng lực nhẹ, twinkle rồi tan
///     0.35s  Đợt 4 — 3–5 bóng bay từ NÓC bay cao lên trời (TÁI DÙNG RisingBalloon)
///     3.50s  Destroy(root) — chốt an toàn, kể cả khi một coroutine chết giữa chừng
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO V2 LUÔN VẼ TRƯỚC CÔNG TRÌNH (bài học từ pháo hoa cũ — xem CHAN_DOAN.md)
/// ══════════════════════════════════════════════════════════════════════════
/// Hiệu ứng cũ Instantiate prefab confetti của Lana (mượn từ LevelUpPopupUI) mà không
/// ghi đè sorting của ParticleSystemRenderer → hạt giữ layer "Default" serialize trong
/// prefab, vẽ SAU layer "Objects" của công trình. V2 KHÔNG dùng ParticleSystem prefab:
/// mọi mảnh là SpriteRenderer tự vẽ runtime, và sorting được đọc từ CHÍNH công trình:
///
///     layer = sortingLayerName của SpriteRenderer con có sortingOrder LỚN NHẤT
///     order = maxSortingOrder + 100          (fallback: layer "Default", order 5000)
///
/// → dù công trình nằm layer nào, order bao nhiêu (kể cả Y-sort đổi order lúc chạy),
/// hiệu ứng vẫn nằm TRƯỚC nó mà không đụng tới TagManager.
///
/// KHÔNG CẦN PREFAB / ART MỚI: mọi sprite (khói, sao, chấm, vuông, bóng bay, dây)
/// vẽ bằng code lúc chạy — cùng triết lý ConstructionSpriteFactory. Texture cache tĩnh,
/// HideAndDontSave, tự sinh lại nếu bị huỷ (an toàn khi tắt Domain Reload).
///
/// TÁI SỬ DỤNG đồ sprint trước:
///   • <see cref="FxEase"/>      — OutCubic / OutQuad / InCubic / OutBackRaw(c1=3) / SetAlpha
///   • <see cref="RisingBalloon"/> — Đợt 4 gọi đúng API public: Configure() trước, bật
///     GameObject sau (autoPlay bắn Play() trong OnEnable), component tự Destroy khi xong.
///   • FloatingNumber KHÔNG tái dùng được cho cảnh world này: `pixelToUnit` là field
///     private mặc định 1 (bay chỉ 90 unit ≈ 1 ô — quá thấp so với nhà cao 400+ unit)
///     và nó không đặt sorting cho MeshRenderer của TMP (chữ sẽ chìm sau công trình,
///     đúng cái bệnh đang chữa). Số "+N" ở đây tự chạy coroutine cùng công thức
///     (+90px ease-out, pop 1.25) nhưng với đơn vị world và sorting đúng.
///
/// NULL-SAFE TOÀN TẬP: building null → cảnh báo rồi thôi; công trình không có
/// SpriteRenderer → dùng hộp 4×4 ô quanh transform; TMP không có font → bỏ chữ,
/// giữ sao; mọi coroutine tự thoát khi renderer đã chết.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")] // chỉ dựng qua Play(), không cho kéo tay vào Inspector
public class ConstructionCelebrationFX : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════
    // ENTRY TĨNH — chỗ duy nhất bên ngoài cần biết
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Nổ chuỗi ăn mừng quanh <paramref name="building"/> (root của công trình vừa xây).
    /// An toàn gọi với bất kỳ Transform nào — kể cả object không có SpriteRenderer.
    /// </summary>
    /// <param name="building">Root công trình. null = bỏ qua (chỉ log warning).</param>
    /// <param name="expReward">EXP hiển thị "+N". ≤ 0 = chỉ hiện ngôi sao.</param>
    public static void Play(Transform building, int expReward = 0)
    {
        if (building == null)
        {
            Debug.LogWarning("[CelebrationV2] Play() nhận building = null — bỏ qua hiệu ứng.");
            return;
        }

        var root = new GameObject("Celebration_V2");
        root.transform.position = new Vector3(building.position.x, building.position.y, 0f);

        var fx = root.AddComponent<ConstructionCelebrationFX>();
        fx.Begin(building, expReward);
    }

    // ══════════════════════════════════════════════════════════════════════
    // HẰNG SỐ NHỊP & CỠ (world unit — map này 1 ô = 100 unit)
    // ══════════════════════════════════════════════════════════════════════

    private const float TotalLife = 3.5f;   // chốt tự huỷ tuyệt đối
    private const float Cell      = 100f;   // = PlacementManager.CELL, chép hằng để file FX đứng độc lập

    // Sorting fallback khi công trình không có SpriteRenderer nào (spec V2).
    private const string FallbackLayer = "Default";
    private const int    FallbackOrder = 5000;

    // Bảng màu confetti = palette game (vàng đồng, burgundy, xanh lá, trắng kem).
    private static readonly Color ColVang     = Hex("#D9A441");
    private static readonly Color ColBurgundy = Hex("#8E1F3B");
    private static readonly Color ColXanhLa   = Hex("#5FA845");
    private static readonly Color ColTrang    = Hex("#F5F1E6");

    // Màu phụ.
    private static readonly Color ColKhoi     = new Color(0.93f, 0.89f, 0.80f, 0.9f); // khói bụi ấm
    private static readonly Color ColSaoExp   = Hex("#FFD447");                        // sao EXP vàng tươi

    // ══════════════════════════════════════════════════════════════════════
    // TRẠNG THÁI RUNTIME
    // ══════════════════════════════════════════════════════════════════════

    private Bounds _bounds;                    // hộp bao world của toàn bộ sprite con
    private string _layerName = FallbackLayer; // layer của công trình
    private int    _order     = FallbackOrder; // maxOrder công trình + 100

    // ══════════════════════════════════════════════════════════════════════
    // KHỞI ĐỘNG
    // ══════════════════════════════════════════════════════════════════════

    private void Begin(Transform building, int expReward)
    {
        ResolveBoundsAndSorting(building);

        // Chốt an toàn: root LUÔN biến mất sau TotalLife giây, kể cả khi có coroutine
        // nào đó chết giữa chừng (object con bị bên ngoài Destroy chẳng hạn).
        Destroy(gameObject, TotalLife);

        StartCoroutine(CoTimeline(expReward));
    }

    /// <summary>
    /// Đo hộp bao + lấy sorting từ CHÍNH công trình.
    /// Lấy layer của renderer có order LỚN NHẤT (không lấy renderer đầu tiên: một prefab
    /// có thể trộn nhiều layer, mảnh vẽ trên cùng mới là mốc phải vượt qua).
    /// </summary>
    private void ResolveBoundsAndSorting(Transform building)
    {
        SpriteRenderer[] srs = building != null
            ? building.GetComponentsInChildren<SpriteRenderer>(false)
            : null;

        bool   any      = false;
        Bounds b        = default;
        int    maxOrder = int.MinValue;
        string layer    = null;

        if (srs != null)
        {
            for (int i = 0; i < srs.Length; i++)
            {
                SpriteRenderer sr = srs[i];
                if (sr == null || sr.sprite == null) continue;

                if (!any) { b = sr.bounds; any = true; }
                else        b.Encapsulate(sr.bounds);

                if (sr.sortingOrder > maxOrder)
                {
                    maxOrder = sr.sortingOrder;
                    layer    = sr.sortingLayerName;
                }
            }
        }

        if (!any)
        {
            // Không có sprite nào (prefab lồng particle? object trống?) → hộp 4×4 ô
            // quanh vị trí, sorting fallback theo spec.
            Vector3 c = building != null ? building.position : transform.position;
            _bounds    = new Bounds(new Vector3(c.x, c.y, 0f), new Vector3(4f * Cell, 4f * Cell, 0f));
            _layerName = FallbackLayer;
            _order     = FallbackOrder;
            return;
        }

        _bounds    = b;
        _layerName = string.IsNullOrEmpty(layer) ? FallbackLayer : layer;

        // +100 để đứng TRƯỚC mọi sprite con; kẹp lại cho không tràn dải sorting hợp lệ.
        _order = Mathf.Clamp(maxOrder, -30000, 30000) + 100;
    }

    // ══════════════════════════════════════════════════════════════════════
    // TIMELINE TỔNG
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator CoTimeline(int expReward)
    {
        // ── ĐỢT 1 (0.00s): khói poof quanh chân ─────────────────────────────
        SpawnPoofs();

        yield return Wait(0.10f);

        // ── ĐỢT 2 (0.10s): sao EXP + "+N" từ đỉnh ───────────────────────────
        SpawnExpStar(expReward);

        yield return Wait(0.10f);

        // ── ĐỢT 3 (0.20 → 1.50s): 4 đợt confetti SO LE quanh nửa trên ───────
        // Khoảng cách không đều (0.45/0.45/0.40) để nghe như "bùm… bùm-bùm" chứ
        // không phải máy đếm nhịp.
        SpawnConfettiBurst(1.00f);
        StartCoroutine(CoDelayed(0.45f, () => SpawnConfettiBurst(0.92f)));
        StartCoroutine(CoDelayed(0.90f, () => SpawnConfettiBurst(0.84f)));
        StartCoroutine(CoDelayed(1.30f, () => SpawnConfettiBurst(0.78f)));

        yield return Wait(0.15f);

        // ── ĐỢT 4 (0.35s): bóng bay từ nóc bay cao ──────────────────────────
        SpawnBalloons();
    }

    private IEnumerator CoDelayed(float delay, System.Action action)
    {
        yield return Wait(delay);
        action?.Invoke();
    }

    /// <summary>Đợi bằng deltaTime cộng dồn — cùng cách RisingBalloon/GiftBoxReveal đếm giờ.</summary>
    private static IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ĐỢT 1 — KHÓI POOF QUANH CHÂN
    // ══════════════════════════════════════════════════════════════════════

    private void SpawnPoofs()
    {
        int count = Random.Range(4, 7); // 4–6 cụm

        float xMin = _bounds.min.x + _bounds.size.x * 0.12f;
        float xMax = _bounds.max.x - _bounds.size.x * 0.12f;
        float yFoot = _bounds.min.y;

        for (int i = 0; i < count; i++)
        {
            // Rải đều theo bề ngang + jitter, để không dồn cục một góc.
            float k = count > 1 ? (float)i / (count - 1) : 0.5f;
            var pos = new Vector3(
                Mathf.Lerp(xMin, xMax, k) + Random.Range(-18f, 18f),
                yFoot + Random.Range(-6f, 22f),
                0f);

            SpriteRenderer sr = NewSprite("Poof_" + i, SoftCircleSprite(), pos, ColKhoi, _order + i);
            if (sr == null) continue;

            StartCoroutine(CoPoof(sr, Random.Range(0.55f, 0.8f), Random.Range(38f, 62f)));
        }
    }

    private IEnumerator CoPoof(SpriteRenderer sr, float life, float startSize)
    {
        float endSize = startSize * Random.Range(2.2f, 2.8f);  // nở ra ~2.5 lần
        float drift   = Random.Range(-14f, 14f);                // trôi ngang nhẹ
        float baseA   = ColKhoi.a;

        Vector3 basePos = sr != null ? sr.transform.position : Vector3.zero;

        float t = 0f;
        while (t < life)
        {
            if (sr == null) yield break; // ai đó huỷ giữa chừng — thoát êm

            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);

            float size = Mathf.Lerp(startSize, endSize, FxEase.OutQuad(k));
            sr.transform.localScale = new Vector3(size, size, 1f);

            // Khói bốc nhẹ lên + dạt ngang.
            sr.transform.position = basePos + new Vector3(drift * k, 26f * FxEase.OutCubic(k), 0f);

            // Tan bằng InCubic — phần mờ dồn về cuối, cụm khói "đứng" được một nhịp.
            Color c = sr.color;
            c.a = baseA * (1f - FxEase.InCubic(k));
            sr.color = c;

            yield return null;
        }

        if (sr != null) Destroy(sr.gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ĐỢT 2 — SAO EXP + SỐ "+N" TỪ ĐỈNH
    // ══════════════════════════════════════════════════════════════════════

    private void SpawnExpStar(int expReward)
    {
        var top = new Vector3(_bounds.center.x, _bounds.max.y + 14f, 0f);

        // Ngôi sao — luôn có, kể cả expReward = 0.
        SpriteRenderer star = NewSprite("Exp_Star", Star4Sprite(), top, ColSaoExp, _order + 8);
        if (star != null)
            StartCoroutine(CoRisePop(star.transform, star, null, 150f, 1.15f, 66f));

        // Số "+N" — chỉ khi có thưởng và TMP có font. Không tái dùng FloatingNumber:
        // xem ghi chú đầu file (pixelToUnit private = 1 → bay quá thấp, và không set
        // sorting cho MeshRenderer → chữ chìm sau công trình).
        if (expReward > 0)
        {
            TMP_Text label = NewWorldText("Exp_Text", "+" + expReward,
                top + new Vector3(52f, 26f, 0f), ColSaoExp, _order + 9);
            if (label != null)
                StartCoroutine(CoRisePop(label.transform, null, label, 150f, 1.15f, 0f));
        }
    }

    /// <summary>
    /// Chuyển động "Township số thưởng" (PHAN_TICH §4.3): bay +rise ease-out trong 1.15s,
    /// scale pop 0 → 1.25 → 1 bằng OutBack c1 = 3 (nghiệm CHÍNH XÁC cho đỉnh 1.25 —
    /// FxEase.BackC1Peak125), alpha giữ 60% đầu rồi tắt.
    /// Nhận SpriteRenderer HOẶC TMP_Text — cái nào khác null thì fade cái đó.
    /// </summary>
    private IEnumerator CoRisePop(Transform tf, SpriteRenderer sr, TMP_Text text,
                                  float rise, float duration, float worldSize)
    {
        if (tf == null) yield break;

        Vector3 basePos   = tf.position;
        Vector3 baseScale = worldSize > 0f ? new Vector3(worldSize, worldSize, 1f) : tf.localScale;
        const float popDur = 0.4f;

        float t = 0f;
        while (t < duration)
        {
            if (tf == null) yield break;

            t += Time.deltaTime;
            float raw = Mathf.Clamp01(t / duration);

            tf.position = basePos + new Vector3(0f, FxEase.OutCubic(raw) * rise, 0f);

            float sT = Mathf.Clamp01(t / popDur);
            tf.localScale = baseScale * FxEase.OutBackRaw(sT, FxEase.BackC1Peak125);

            if (raw > 0.6f)
            {
                float a = 1f - FxEase.InCubic((raw - 0.6f) / 0.4f);
                if (sr   != null) { Color c = sr.color; c.a = a; sr.color = c; }
                if (text != null) text.alpha = a;
            }

            yield return null;
        }

        if (tf != null) Destroy(tf.gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ĐỢT 3 — CONFETTI BURST SO LE QUANH NỬA TRÊN
    // ══════════════════════════════════════════════════════════════════════

    /// <param name="power">1 = đợt đầu; các đợt sau nhỏ dần (nghe như tiếng vọng).</param>
    private void SpawnConfettiBurst(float power)
    {
        if (this == null) return;

        // Tâm nổ: ngẫu nhiên quanh NỬA TRÊN công trình, có thể nhô hơn nóc một chút —
        // đúng vị trí Township đặt pháo sáng (trước mặt + quanh thân trên).
        var center = new Vector3(
            _bounds.center.x + Random.Range(-0.42f, 0.42f) * _bounds.size.x,
            Mathf.Lerp(_bounds.center.y, _bounds.max.y + 40f, Random.value),
            0f);

        int pieces = Mathf.RoundToInt(Random.Range(12, 19) * power); // 12–18 mảnh
        for (int i = 0; i < pieces; i++)
        {
            // Trộn 3 hình: sao 4 cánh / chấm tròn / vuông xoay.
            float pick = Random.value;
            Sprite spr; float size;
            if (pick < 0.30f)      { spr = Star4Sprite();  size = Random.Range(26f, 42f); }
            else if (pick < 0.65f) { spr = DotSprite();    size = Random.Range(12f, 20f); }
            else                   { spr = SquareSprite(); size = Random.Range(14f, 24f); }

            Color col = PickConfettiColor();

            SpriteRenderer sr = NewSprite("Confetti", spr,
                center + (Vector3)(Random.insideUnitCircle * 12f), col, _order + 4 + (i & 3));
            if (sr == null) continue;

            StartCoroutine(CoConfettiPiece(sr, size * power, power));
        }
    }

    private static Color PickConfettiColor()
    {
        switch (Random.Range(0, 4))
        {
            case 0:  return ColVang;
            case 1:  return ColBurgundy;
            case 2:  return ColXanhLa;
            default: return ColTrang;
        }
    }

    private IEnumerator CoConfettiPiece(SpriteRenderer sr, float size, float power)
    {
        if (sr == null) yield break;

        // Bắn hình vòm: −25°..205° (lên trên nhiều hơn xuống) — cùng dải MillCelebrationFX
        // dùng cho pháo giấy popup, đã kiểm chứng "trông như pháo thật".
        float ang   = Random.Range(-25f, 205f) * Mathf.Deg2Rad;
        var   vel   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang))
                      * Random.Range(220f, 420f) * power;
        const float gravity = 520f;   // trọng lực nhẹ (unit/s²) — mảnh bay vòm rồi rơi
        const float drag    = 2.0f;   // cản tỉ lệ vận tốc — chậm dần, không bay thẳng mãi

        float life    = Random.Range(0.8f, 1.2f);
        float spinDeg = Random.Range(-540f, 540f);
        float twPhase = Random.value * Mathf.PI * 2f; // pha twinkle riêng từng mảnh
        float baseA   = sr.color.a;

        Vector3 pos = sr.transform.position;

        float t = 0f;
        while (t < life)
        {
            if (sr == null) yield break;

            float dt = Time.deltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / life);

            // Euler đơn giản — đủ chuẩn cho ~1 giây, không cấp phát gì mỗi frame.
            vel.y -= gravity * dt;
            vel   -= vel * (drag * dt);
            pos   += (Vector3)(vel * dt);

            sr.transform.position = pos;
            sr.transform.Rotate(0f, 0f, spinDeg * dt);
            sr.transform.localScale = new Vector3(size, size, 1f);

            // TWINKLE: alpha lấp lánh quanh 1 rồi nhân với fade 30% cuối —
            // "twinkle rồi tan" đúng đề bài.
            float twinkle = 0.78f + 0.22f * Mathf.Sin(t * 19f + twPhase);
            float fade    = 1f - Mathf.InverseLerp(0.70f, 1f, k);
            Color c = sr.color;
            c.a = baseA * twinkle * fade;
            sr.color = c;

            yield return null;
        }

        if (sr != null) Destroy(sr.gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ĐỢT 4 — BÓNG BAY TỪ NÓC (tái dùng RisingBalloon)
    // ══════════════════════════════════════════════════════════════════════

    private void SpawnBalloons()
    {
        int count = Random.Range(3, 6); // 3–5 quả

        Color[] palette = { Hex("#F24D4D"), ColVang, ColBurgundy, ColXanhLa };

        for (int i = 0; i < count; i++)
        {
            float k = count > 1 ? (float)i / (count - 1) : 0.5f;

            var root = new GameObject("Balloon_" + i);
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(
                Mathf.Lerp(_bounds.min.x + 30f, _bounds.max.x - 30f, k) + Random.Range(-14f, 14f),
                _bounds.max.y - Random.Range(0f, 24f),
                0f);

            // ⚠ TẮT trước khi AddComponent<RisingBalloon>: autoPlay của nó bắn Play()
            // ngay trong OnEnable — phải Configure() XONG rồi mới bật, nếu không quả
            // bóng bay bằng thông số mặc định (pixelToUnit = 1 → chỉ cao 250 unit).
            root.SetActive(false);

            Color col = palette[Random.Range(0, palette.Length)];
            float h   = Random.Range(56f, 76f); // chiều cao quả bóng (world unit)

            // Thân bóng (oval + highlight vẽ sẵn trong texture, tint bằng color).
            SpriteRenderer body = ChildSprite(root.transform, "Body", BalloonSprite(),
                Vector3.zero, col, _order + 3);
            if (body != null) body.transform.localScale = new Vector3(h * 0.78f, h, 1f);

            // Dây — treo dưới thân, hơi lệch cho tự nhiên.
            SpriteRenderer str = ChildSprite(root.transform, "String", StringSprite(),
                new Vector3(Random.Range(-3f, 3f), -h * 0.92f, 0f),
                new Color(1f, 1f, 1f, 0.85f), _order + 2);
            if (str != null) str.transform.localScale = new Vector3(3f, h * 0.85f, 1f);

            // TÁI DÙNG RisingBalloon: rise 250"px" × pixelToUnit 2.2 ≈ 550 unit ≈ 5.5 ô —
            // BAY CAO thật sự (chuỗi cũ bóng chỉ nhích ~250 unit vì pixelToUnit mặc định 1).
            // duration lệch nhau chút để cả chùm không đáp đỉnh cùng lúc; sway/scale/fade
            // + lệch pha StablePhase01 đã nằm sẵn trong component.
            var rb = root.AddComponent<RisingBalloon>();
            rb.Configure(Random.Range(240f, 300f), Random.Range(2.1f, 2.6f), 2.2f);

            root.SetActive(true); // OnEnable → autoPlay Play(); destroyOnFinish tự dọn
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // DỰNG SPRITE / TEXT RUNTIME
    // ══════════════════════════════════════════════════════════════════════

    private SpriteRenderer NewSprite(string name, Sprite sprite, Vector3 worldPos,
                                     Color color, int order)
    {
        if (sprite == null) return null;

        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = worldPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = sprite;
        sr.color            = color;
        sr.sortingLayerName = _layerName; // layer đọc từ CHÍNH công trình
        sr.sortingOrder     = order;      // luôn ≥ maxOrder + 100 → TRƯỚC mọi sprite của nó
        return sr;
    }

    private SpriteRenderer ChildSprite(Transform parent, string name, Sprite sprite,
                                       Vector3 localPos, Color color, int order)
    {
        if (sprite == null || parent == null) return null;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = sprite;
        sr.color            = color;
        sr.sortingLayerName = _layerName;
        sr.sortingOrder     = order;
        return sr;
    }

    /// <summary>
    /// TMP world ("+N"). Trả null nếu TMP không có font mặc định — khi đó Đợt 2 chỉ có
    /// ngôi sao, không crash. Sorting đặt qua MeshRenderer (TMP 3D vẽ bằng MeshRenderer,
    /// mặc định layer "Default" order 0 → không đặt là chữ chìm sau công trình).
    /// </summary>
    private TMP_Text NewWorldText(string name, string content, Vector3 worldPos,
                                  Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = worldPos;

        var tmp = go.AddComponent<TextMeshPro>();
        if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        if (tmp.font == null)
        {
            Destroy(go); // không có font thì thà bỏ chữ còn hơn hiện ô vuông hồng
            return null;
        }

        tmp.text         = content;
        tmp.fontSize     = 64f;    // TMP 3D: ~6.4 unit → nhân localScale 9 ≈ 58 unit cao
        tmp.color        = color;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.rectTransform.sizeDelta = new Vector2(40f, 12f);
        go.transform.localScale     = Vector3.one * 9f;

        // Viền đậm kiểu Township — cùng cách AddOutline của ConstructionSiteUI
        // (bật keyword trên material INSTANCE rồi nới mesh padding).
        Material mat = tmp.fontMaterial;
        if (mat != null) mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        tmp.outlineColor = new Color(0.20f, 0.10f, 0.02f, 1f);
        tmp.outlineWidth = 0.26f;
        tmp.UpdateMeshPadding();

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = _layerName;
            mr.sortingOrder     = order;
        }
        return tmp;
    }

    // ══════════════════════════════════════════════════════════════════════
    // SPRITE VẼ RUNTIME — cache tĩnh, tự sinh lại nếu texture bị huỷ
    // (cùng thủ thuật ConstructionSpriteFactory dùng cho chế độ tắt Domain Reload)
    // ══════════════════════════════════════════════════════════════════════

    private static Sprite _sprSoft, _sprDot, _sprStar, _sprSquare, _sprBalloon, _sprString;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _sprSoft = _sprDot = _sprStar = _sprSquare = _sprBalloon = _sprString = null;
    }

    private static bool Dead(Sprite s) => s == null || s.texture == null;

    /// <summary>Chấm tròn MỀM (alpha smoothstep từ tâm ra) — cụm khói poof.</summary>
    private static Sprite SoftCircleSprite()
    {
        if (!Dead(_sprSoft)) return _sprSoft;
        _sprSoft = Bake(64, 64, (x, y) =>
        {
            float dx = (x + 0.5f - 32f) / 30f;
            float dy = (y + 0.5f - 32f) / 30f;
            float r  = Mathf.Sqrt(dx * dx + dy * dy);
            float a  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - r) / 0.55f));
            return new Color(1f, 1f, 1f, a);
        });
        return _sprSoft;
    }

    /// <summary>Chấm tròn đặc, mép khử răng cưa — mảnh confetti tròn.</summary>
    private static Sprite DotSprite()
    {
        if (!Dead(_sprDot)) return _sprDot;
        _sprDot = Bake(32, 32, (x, y) =>
        {
            float dx = (x + 0.5f - 16f) / 14f;
            float dy = (y + 0.5f - 16f) / 14f;
            float r  = Mathf.Sqrt(dx * dx + dy * dy);
            return new Color(1f, 1f, 1f, Mathf.Clamp01((1f - r) * 7f));
        });
        return _sprDot;
    }

    /// <summary>
    /// Sao 4 cánh — đường astroid |x|^(2/3) + |y|^(2/3) ≤ 1 (đúng hình lấp lánh
    /// 4 mũi nhọn, không phải vẽ đa giác tay).
    /// </summary>
    private static Sprite Star4Sprite()
    {
        if (!Dead(_sprStar)) return _sprStar;
        _sprStar = Bake(64, 64, (x, y) =>
        {
            float px = Mathf.Abs((x + 0.5f - 32f) / 30f);
            float py = Mathf.Abs((y + 0.5f - 32f) / 30f);
            float v  = Mathf.Pow(px, 2f / 3f) + Mathf.Pow(py, 2f / 3f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01((1f - v) * 6f));
        });
        return _sprStar;
    }

    /// <summary>Vuông trắng đặc — mảnh giấy vuông (xoay + twinkle lúc bay).</summary>
    private static Sprite SquareSprite()
    {
        if (!Dead(_sprSquare)) return _sprSquare;
        _sprSquare = Bake(8, 8, (x, y) => Color.white);
        return _sprSquare;
    }

    /// <summary>
    /// Bóng bay: thân oval xám-sáng 0.92 + highlight TRẮNG lệch trên-trái + nút thắt dưới.
    /// Thân cố ý 0.92 (không trắng tinh) để khi tint màu, vùng highlight vẫn SÁNG HƠN
    /// thân — SpriteRenderer.color là phép nhân.
    /// </summary>
    private static Sprite BalloonSprite()
    {
        if (!Dead(_sprBalloon)) return _sprBalloon;
        _sprBalloon = Bake(48, 64, (x, y) =>
        {
            float fx = x + 0.5f, fy = y + 0.5f;

            // Thân: ellipse tâm (24, 38), bán trục (19, 23).
            float bx = (fx - 24f) / 19f;
            float by = (fy - 38f) / 23f;
            float d  = bx * bx + by * by;
            float bodyA = Mathf.Clamp01((1f - d) * 6f);

            // Nút thắt: tam giác nhỏ dưới đáy (y 9..15).
            float knotA = 0f;
            if (fy >= 9f && fy <= 15f && Mathf.Abs(fx - 24f) <= (fy - 8f) * 0.55f)
                knotA = 1f;

            float a = Mathf.Max(bodyA, knotA);
            if (a <= 0f) return Color.clear;

            // Highlight: ellipse nhỏ lệch trên-trái, sáng dần vào tâm highlight.
            float hx = (fx - 17f) / 7.5f;
            float hy = (fy - 46f) / 9.5f;
            float hl = Mathf.Clamp01(1f - (hx * hx + hy * hy));
            float v  = Mathf.Lerp(0.92f, 1f, hl);

            return new Color(v, v, v, a);
        });
        return _sprBalloon;
    }

    /// <summary>Dây bóng bay — cột dọc mảnh, mờ nhẹ hai đầu.</summary>
    private static Sprite StringSprite()
    {
        if (!Dead(_sprString)) return _sprString;
        _sprString = Bake(4, 32, (x, y) =>
        {
            float a = (x == 1 || x == 2) ? 0.9f : 0f;
            return new Color(0.95f, 0.95f, 0.95f, a);
        });
        return _sprString;
    }

    /// <summary>
    /// Nướng texture theo hàm màu từng pixel → Sprite chuẩn hoá CAO = 1 world unit
    /// (pixelsPerUnit = chiều cao) — nhờ vậy localScale đọc thẳng ra world unit.
    /// </summary>
    private static Sprite Bake(int w, int h, System.Func<int, int, Color> pixel)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
            hideFlags  = HideFlags.HideAndDontSave
        };

        var cols = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                cols[y * w + x] = pixel(x, y);

        tex.SetPixels(cols);
        // GIỮ readable (không truyền makeNoLongerReadable): một số đường Sprite.Create
        // cần đọc texture — vài KB cho 6 texture nhỏ, đổi lấy an toàn tuyệt đối.
        tex.Apply(false, false);

        var spr = Sprite.Create(tex, new Rect(0, 0, w, h),
                                new Vector2(0.5f, 0.5f), h, 0, SpriteMeshType.FullRect);
        spr.hideFlags = HideFlags.HideAndDontSave;
        return spr;
    }

    private static Color Hex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}
