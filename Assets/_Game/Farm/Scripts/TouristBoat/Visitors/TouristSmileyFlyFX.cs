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

    /// <summary>[QA m-5] Chống spam Console: chỉ cảnh báo thiếu sprite đúng 1 lần.</summary>
    private static bool _warnedNoSprite;

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
    public static TouristSmileyFlyFX Spawn(Vector3 worldStart, Sprite smiley, float flyTime,
                                           string sortingLayerName, int sortingOrder, float worldSize)
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
        fx.Init(worldStart, flyTime, sr);
        return fx;
    }

    private void Init(Vector3 start, float flyTime, SpriteRenderer sr)
    {
        _renderer  = sr;
        _startPos  = start;
        _duration  = Mathf.Max(0.15f, flyTime);
        _endPos    = ResolveHudWorldPosition(start);
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

        // Nhỏ → to (0.4 → 1.4) đúng GDD.
        transform.localScale = _baseScale * Mathf.Lerp(0.4f, 1.4f, t);

        // Fade ở 35% cuối.
        if (_renderer != null)
        {
            Color c = _renderer.color;
            c.a = t < 0.65f ? 1f : Mathf.Clamp01(1f - (t - 0.65f) / 0.35f);
            _renderer.color = c;
        }

        if (t >= 1f) Destroy(gameObject);
    }

    /// <summary>
    /// Tìm đích bay: RectTransform trong canvas HUD có tên khớp gợi ý → đổi sang world.
    /// Không thấy → bay thẳng lên trời (1.5 lần nửa chiều cao camera).
    /// </summary>
    private static Vector3 ResolveHudWorldPosition(Vector3 fallbackFrom)
    {
        Camera cam = Camera.main;

        RectTransform target = FindHudTarget();
        if (target != null && cam != null)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector3 screen = RectTransformUtility.WorldToScreenPoint(uiCam, target.position);
            // z = khoảng cách từ camera tới mặt phẳng world của khách (camera 2D orthographic).
            float depth = Mathf.Abs(cam.transform.position.z - fallbackFrom.z);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            world.z = fallbackFrom.z;
            return world;
        }

        float rise = cam != null && cam.orthographic ? cam.orthographicSize * 1.5f : 300f;
        return fallbackFrom + new Vector3(0f, rise, 0f);
    }

    /// <summary>
    /// Dò RectTransform HUD theo tên (không đụng field private của FarmUIManager).
    /// Ưu tiên canvas có sortingOrder cao nhất (HUD nằm trên cùng).
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
        return best.GetComponent<RectTransform>();
    }
}

/// <summary>
/// TÍNH THƯỞNG KHI GIAO MÓN CHO KHÁCH (GDD BOAT-002 §3.4).
///
///   goldReward = Σ BasePriceBook giá nguyên liệu của món × rewardIngredientMultiplier
///   expReward  = dish.rewardExp
///
/// Nguồn giá: <see cref="BasePriceBook.TryGetBasePrice"/> — ĐÚNG sổ giá duy nhất của dự
/// án (provider của Dev A → MarketPriceTable → StallItemCatalog asset thật → bảng dự
/// phòng). GDD §3.4 cấm bịa bảng giá mới, nên ở đây KHÔNG có một con số giá nào.
///
/// Map id: dùng thẳng <c>IngredientData.id</c> làm khoá tra giá — cùng khoá mà kho
/// (FarmInventoryManager tự normalize lowercase) và BasePriceBook (cũng normalize
/// lowercase) đang dùng; bảng dự phòng của BasePriceBook liệt kê đúng các id kiểu
/// "rice"/"khoaitay"/"beef" nên hai bên khớp nhau.
///
/// [QA M-4] CHỈ cộng <c>IngredientKind.Ingredient</c> — GIA VỊ (Seasoning) bị loại,
/// đúng chữ GDD "Σ giá NGUYÊN LIỆU CHÍNH của món". Cộng cả gia vị làm thưởng phồng 30-60%.
///
/// FALLBACK AN TOÀN: chỉ cần MỘT nguyên liệu không tra được giá thì CẢ MÓN rơi về
/// <c>dish.sellPrice × multiplier</c> + log warning; [QA B-3] nếu sellPrice = 0 thì rơi
/// tiếp xuống BasePriceBook.DefaultBasePrice — KHÔNG BAO GIỜ trả 0, vì thưởng 0 sẽ làm
/// DeliverTo huỷ giao dịch và người chơi không giao được món.
/// </summary>
public static class TouristRewardCalculator
{
    /// <summary>
    /// Tính vàng thưởng. <paramref name="usedFallback"/> = true khi phải rơi về sellPrice.
    /// Món null / không có nguyên liệu → 0 (bên gọi tự quyết định có thưởng không).
    /// </summary>
    public static int ComputeGold(DishData dish, float multiplier, out bool usedFallback)
    {
        usedFallback = false;
        if (dish == null) return 0;

        float mul = multiplier > 0.01f ? multiplier : 1f;

        var list = dish.requiredIngredients;
        if (list == null || list.Count == 0)
        {
            usedFallback = true;
            return GiaFallback(dish, mul, "món không khai nguyên liệu nào");
        }

        int tong = 0;
        int soNguyenLieuChinh = 0;

        for (int i = 0; i < list.Count; i++)
        {
            IngredientData ing = list[i];
            if (ing == null || string.IsNullOrEmpty(ing.id))
            {
                usedFallback = true;
                break;
            }

            // [QA M-4] GDD §3.4 ghi rõ "Σ giá NGUYÊN LIỆU CHÍNH của món".
            // IngredientData đã phân biệt sẵn Ingredient / Seasoning — cộng cả gia vị
            // (muối, nước mắm, tiêu) làm thưởng phồng 30-60% so với số Sếp đã cân bằng.
            if (ing.kind == IngredientKind.Seasoning) continue;

            int gia;
            if (!BasePriceBook.TryGetBasePrice(ing.id, out gia))
            {
                usedFallback = true;
                Debug.LogWarning($"[TouristVisitor] Không tra được giá nguyên liệu '{ing.id}' " +
                                 $"của món '{dish.dishId}' — cả món rơi về sellPrice (GDD §3.4). " +
                                 "Bổ sung giá vào MarketPriceTable/StallItemCatalog để tuning đúng.");
                break;
            }
            tong += gia;
            soNguyenLieuChinh++;
        }

        if (usedFallback)
            return GiaFallback(dish, mul, "thiếu giá nguyên liệu");

        // Món TOÀN GIA VỊ (không có nguyên liệu chính nào) — không được trả 0 (QA B-3).
        if (soNguyenLieuChinh == 0 || tong <= 0)
        {
            usedFallback = true;
            return GiaFallback(dish, mul, "món không có nguyên liệu chính nào có giá");
        }

        // Sàn 1 vàng: thưởng bằng 0 sẽ làm DeliverTo HỦY giao dịch (QA B-3) — người chơi
        // nấu xong mà không giao được món thì cũng là hỏng, nên luôn giữ > 0.
        return Mathf.Max(1, Mathf.RoundToInt(tong * mul));
    }

    /// <summary>
    /// [QA B-3] Đường thưởng dự phòng — KHÔNG BAO GIỜ trả 0.
    /// GDD §3.4 nói fallback dùng <c>dish.sellPrice</c>, nhưng <c>sellPrice</c> mặc định
    /// của DishData là 0; món nào chưa điền số sẽ khiến thưởng = 0 và giao dịch bị huỷ.
    /// Nên khi sellPrice ≤ 0 thì rơi tiếp xuống <see cref="BasePriceBook.DefaultBasePrice"/>
    /// (giá của vật phẩm "không biết" — cố tình thấp, không gây lạm phát) + cảnh báo để tuning.
    /// </summary>
    private static int GiaFallback(DishData dish, float mul, string lyDo)
    {
        int goc = dish.sellPrice;
        if (goc <= 0)
        {
            goc = BasePriceBook.DefaultBasePrice;
            Debug.LogWarning($"[TouristVisitor] Món '{dish.dishId}': {lyDo} VÀ sellPrice = 0 — " +
                             $"tạm thưởng theo giá mặc định {BasePriceBook.DefaultBasePrice}. " +
                             "Điền sellPrice/requiredIngredients cho asset món này để số thưởng đúng thiết kế.");
        }
        else
        {
            Debug.LogWarning($"[TouristVisitor] Món '{dish.dishId}': {lyDo} — " +
                             $"dùng sellPrice ({goc}) × {mul:0.##} làm thưởng (GDD §3.4).");
        }
        return Mathf.Max(1, Mathf.RoundToInt(goc * mul));
    }

    /// <summary>EXP thưởng = dish.rewardExp (số nằm sẵn trên asset món — GDD §3.4).</summary>
    public static int ComputeExp(DishData dish)
    {
        return dish != null ? Mathf.Max(0, dish.rewardExp) : 0;
    }
}
