using UnityEngine;

/// <summary>
/// HIỆU ỨNG MẶT CƯỜI BAY LÊN HUD khi khách được phục vụ (GDD BOAT-002 §3.3, §8.2).
///
/// Spawn tại đầu khách → bay về phía panel HUD (ô vàng trên thanh trạng thái) →
/// scale nhỏ→to (0.4 → 1.4) + fade out ở cuối. Thời gian = config.smileyFlyTime.
///
/// VÌ SAO LÀM BẰNG WORLD-SPACE SPRITE CHỨ KHÔNG PHẢI UI IMAGE:
///   FarmUIManager giữ canvasHudRoot ở field PRIVATE, không có API công khai để lấy
///   RectTransform ô vàng, và hệ khách vốn sống hoàn toàn trong world (SpriteRenderer).
///   Nên: tìm RectTransform đích theo TÊN trong canvas HUD, đổi vị trí nó ra SCREEN
///   rồi ScreenToWorldPoint về world → bay bằng SpriteRenderer thường. Cách này không
///   phụ thuộc render mode của canvas và không đụng một dòng nào của UI hiện có.
///   Không tìm được đích → fallback bay THẲNG LÊN TRỜI (vẫn juicy, không lỗi).
///
/// Toàn bộ FX tự huỷ sau khi bay xong — không cần pool (≤18 khách đồng thời, AC §8.7).
/// </summary>
public class TouristSmileyFlyFX : MonoBehaviour
{
    // Tên object HUD hay gặp trong SCN_Farm — dò theo thứ tự ưu tiên.
    // Sếp đổi tên object thì thêm chuỗi vào đây, KHÔNG cần sửa logic.
    private static readonly string[] HudTargetNameHints =
    {
        "txtgold", "textgold", "goldtext", "gold", "vang",
        "topbar", "hudtop", "canvashud", "hud"
    };

    /// <summary>Mốc thời gian (0-1) bắt đầu mờ dần. Lead chốt 0.45 — thấy rõ hiệu ứng tan biến.</summary>
    private const float FadeStart = 0.45f;

    /// <summary>[QA m-5] Chống spam Console: chỉ cảnh báo thiếu sprite đúng 1 lần.</summary>
    private static bool _warnedNoSprite;

    /// <summary>Chống spam khi không dò được đích HUD — cảnh báo đúng 1 lần/phiên.</summary>
    private static bool _warnedNoHud;

    private SpriteRenderer _renderer;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private float   _duration = 1.2f;
    private float   _elapsed;

    /// <summary>
    /// Tạo và chạy 1 hiệu ứng mặt cười.
    /// </summary>
    /// <param name="worldStart">Vị trí đầu khách (world).</param>
    /// <param name="smiley">Sprite mặt cười (bubble trả về — art hoặc placeholder).</param>
    /// <param name="flyTime">config.smileyFlyTime.</param>
    /// <param name="sortingLayerName">Layer sorting để FX nổi trên mọi thứ.</param>
    /// <param name="sortingOrder">Order sorting (đặt cao hơn bubble).</param>
    /// <param name="worldSize">Cỡ mặt cười ở scale 1.0, tính bằng unit world.</param>
    /// <param name="hudTarget">
    /// Đích bay wire cứng (ô vàng HUD). Có thì dùng THẲNG, không dò tên.
    /// Null thì dò theo tên; dò không ra thì BAY THẲNG LÊN TRỜI.
    /// </param>
    public static TouristSmileyFlyFX Spawn(Vector3 worldStart, Sprite smiley, float flyTime,
                                           string sortingLayerName, int sortingOrder, float worldSize,
                                           Transform hudTarget)
    {
        if (smiley == null)
        {
            // [QA m-5] 18 khách/chu kỳ ⇒ log mỗi lần sẽ ngập Console và che lỗi thật.
            if (!_warnedNoSprite)
            {
                _warnedNoSprite = true;
                Debug.LogWarning("[TouristVisitor] SmileyFlyFX: không có sprite mặt cười — bỏ qua hiệu ứng. " +
                                 "(Cảnh báo này chỉ in 1 lần cho cả phiên.)");
            }
            return null;
        }

        var go = new GameObject("SmileyFlyFX");
        go.transform.position = worldStart;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = smiley;
        if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        // Quy đổi cỡ hiển thị ra unit world (sprite 256px/PPU100 = 2.56 unit — map này rất lớn).
        float native = Mathf.Max(smiley.rect.width, smiley.rect.height) / Mathf.Max(1f, smiley.pixelsPerUnit);
        if (native > 0.0001f)
            go.transform.localScale = Vector3.one * (worldSize / native);

        var fx = go.AddComponent<TouristSmileyFlyFX>();
        fx.Init(worldStart, flyTime, sr, hudTarget);
        return fx;
    }

    private void Init(Vector3 start, float flyTime, SpriteRenderer sr, Transform hudTarget)
    {
        _renderer  = sr;
        _startPos  = start;
        _duration  = Mathf.Max(0.15f, flyTime);
        _endPos    = ResolveHudWorldPosition(start, hudTarget);
        _baseScale = transform.localScale;
    }

    private Vector3 _baseScale = Vector3.one;

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // Bay theo đường cong nhẹ (ease-out) cho mềm, không đi thẳng đơ.
        float ease = 1f - Mathf.Pow(1f - t, 2f);
        transform.position = Vector3.Lerp(_startPos, _endPos, ease);

        // Nhỏ → to. [Lead chốt 2026-08-29] 0.4 → 1.5 (to hơn bản 1.4 cho rõ "to dần").
        transform.localScale = _baseScale * Mathf.Lerp(0.4f, 1.5f, t);

        // Fade bắt đầu từ t = 0.45 (bản trước 0.65 nên mờ chỉ loé ở cuối, gần như không thấy).
        if (_renderer != null)
        {
            Color c = _renderer.color;
            c.a = t < FadeStart ? 1f : Mathf.Clamp01(1f - (t - FadeStart) / (1f - FadeStart));
            _renderer.color = c;
        }

        if (t >= 1f) Destroy(gameObject);
    }

    /// <summary>
    /// Chọn đích bay theo 3 nhánh, ưu tiên giảm dần:
    ///   ① <paramref name="hudTarget"/> wire cứng (tool ★ gán ô vàng HUD) — dùng thẳng;
    ///   ② dò RectTransform theo TÊN trong canvas HUD;
    ///   ③ dò không ra → **BAY THẲNG LÊN TRỜI** (đúng ý Sếp: "mặt cười bay lên").
    ///
    /// [Lead chốt 2026-08-29] Bản trước, khi không khớp tên nào thì
    /// <c>return best.GetComponent&lt;RectTransform&gt;()</c> = rect GỐC của canvas =
    /// **TÂM MÀN HÌNH** ⇒ mặt cười bay vào giữa màn hình, trông như lỗi. Nhánh đó đã XOÁ HẲN.
    /// Nhánh ③ giờ là đường chạy thật, không phải code chết.
    /// </summary>
    private static Vector3 ResolveHudWorldPosition(Vector3 batDau, Transform hudTarget)
    {
        Camera cam = Camera.main;

        // ① Wire cứng — có thể là RectTransform (UI) hoặc Transform world thường.
        if (hudTarget != null)
        {
            var rtWire = hudTarget as RectTransform;
            if (rtWire != null) return UiSangWorld(rtWire, batDau, cam);

            Vector3 w = hudTarget.position;
            w.z = batDau.z;
            return w;
        }

        // ② Dò theo tên
        RectTransform target = FindHudTarget();
        if (target != null) return UiSangWorld(target, batDau, cam);

        // ③ Bay thẳng lên trời — TUYỆT ĐỐI không rơi về tâm màn hình
        if (!_warnedNoHud)
        {
            _warnedNoHud = true;
            Debug.LogWarning("[TouristVisitor] Không tìm được ô vàng HUD để mặt cười bay tới — " +
                             "cho bay THẲNG LÊN TRỜI. Muốn bay về ví tiền: chạy " +
                             "Tools/Farm Game/Tourist Boat/★ SETUP TẤT CẢ (tool tự wire hudGoldTarget), " +
                             "hoặc kéo tay object đó vào field 'hudGoldTarget' của TouristVisitorManager. " +
                             "(Cảnh báo này chỉ in 1 lần.)");
        }
        return BayLenTroi(batDau, cam);
    }

    /// <summary>Đổi vị trí một RectTransform UI sang toạ độ world trên mặt phẳng của khách.</summary>
    private static Vector3 UiSangWorld(RectTransform rt, Vector3 batDau, Camera cam)
    {
        if (cam == null) return BayLenTroi(batDau, null);

        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3 screen = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
        float depth = Mathf.Abs(cam.transform.position.z - batDau.z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = batDau.z;
        return world;
    }

    /// <summary>Đích dự phòng: thẳng lên trên, cao 1.5× nửa chiều cao camera.</summary>
    private static Vector3 BayLenTroi(Vector3 batDau, Camera cam)
    {
        float cao = cam != null && cam.orthographic ? cam.orthographicSize * 1.5f : 300f;
        return batDau + new Vector3(0f, cao, 0f);
    }

    /// <summary>
    /// Dò RectTransform HUD theo tên (không đụng field private của FarmUIManager).
    /// Ưu tiên canvas có sortingOrder cao nhất (HUD nằm trên cùng).
    /// KHÔNG khớp tên nào → trả NULL để bên gọi bay lên trời (bản cũ trả rect gốc canvas
    /// = tâm màn hình, đó là bug).
    /// </summary>
    private static RectTransform FindHudTarget()
    {
        Canvas best = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c == null || !c.isRootCanvas) continue;
            if (best == null || c.sortingOrder > best.sortingOrder) best = c;
        }
        if (best == null) return null;

        RectTransform[] all = best.GetComponentsInChildren<RectTransform>(false);
        for (int hint = 0; hint < HudTargetNameHints.Length; hint++)
        {
            string needle = HudTargetNameHints[hint];
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].name.ToLowerInvariant().Contains(needle))
                    return all[i];
            }
        }
        return null;   // không có đích hợp lệ — KHÔNG trả canvas gốc
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  [QA B-6 · 2026-08-29] TouristRewardCalculator KHÔNG còn nằm trong file này.
//
//  Trước đây class đó ở đây (Dev B viết bản V2.0), rồi Dev A viết lại công thức
//  thưởng V2.1 — thành ra HAI CHỦ trong CÙNG MỘT FILE: copy gói A đè gói B thì
//  player build vẫn compile sạch nhưng chạy công thức thưởng CŨ, im lặng hoàn toàn.
//
//  Nay class thuộc về Dev A ở file riêng:
//      Assets/_Game/Farm/Scripts/TouristBoat/Visitors/TouristRewardCalculator.cs
//
//  File này CHỈ còn hiệu ứng mặt cười của Dev B. TouristVisitorManager gọi calculator
//  qua chữ ký nhận config (ComputeGold(dish, config, out fallback) /
//  ComputeExp(dish, config)) — Dev A vẫn giữ cả 2 chữ ký cũ để tương thích.
// ─────────────────────────────────────────────────────────────────────────────
