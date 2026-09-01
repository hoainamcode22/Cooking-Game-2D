using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [V3] CHẠM Ô QUÀ trong popup Lên Cấp → ô "nhún nhún, mẩy mẩy" (squash &amp; stretch
/// kiểu casual) + hiện TOOLTIP thông tin món quà ngay phía trên ô (giống Family Farm).
///
/// CƠ CHẾ:
///   • LevelUpPopupUI AddComponent lúc dựng ô quà (cả đường prefab lẫn procedural)
///     rồi gọi <see cref="Init"/> — KHÔNG cần sửa prefab/scene, không cần art mới.
///   • Ô quà được bật raycastTarget → tap rơi vào ô này và DỪNG tại đây,
///     KHÔNG lọt xuống lớp LevelUpTapToClose (sibling thấp hơn) → chạm quà xem
///     thông tin thoải mái, popup không bị đóng oan.
///   • Tooltip là 1 panel dùng chung (static) dựng runtime, parent theo cha của ô quà
///     → popup đóng là tooltip chết theo, không leak sang scene.
///   • Toàn bộ chạy unscaled time — popup mở lúc game pause vẫn nhún mượt.
/// </summary>
public class GiftSlotBounceTooltip : MonoBehaviour, IPointerClickHandler
{
    private const float TooltipWidth    = 268f;
    private const float TooltipHeight   = 78f;
    private const float TooltipGapY     = 14f;   // hở giữa mép trên ô quà và tooltip
    private const float TooltipLifetime = 2.6f;  // tự ẩn sau chừng này giây
    private const float BounceTime      = 0.55f;

    private string _tenQua   = "";
    private string _moTa     = "";
    private int    _soLuong  = 1;

    private Coroutine _bounceCo;
    private Vector3   _baseScale = Vector3.one;
    private bool      _baseScaleCaptured;

    // ── Tooltip dùng chung cho mọi ô quà (mỗi lần chỉ hiện 1 cái) ──
    private static RectTransform   _tipRoot;
    private static TextMeshProUGUI _tipTitle;
    private static TextMeshProUGUI _tipDesc;
    private static Coroutine       _tipHideCo;
    private static MonoBehaviour   _tipHideHost;

    // =========================================================================
    //  KHỞI TẠO
    // =========================================================================

    /// <summary>Đổ dữ liệu món quà cho tooltip. Gọi ngay sau AddComponent.</summary>
    public void Init(LevelRewardConfig.ItemGift gift)
    {
        if (gift == null) return;
        _tenQua  = string.IsNullOrEmpty(gift.displayName) ? gift.itemId : gift.displayName;
        _soLuong = Mathf.Max(1, gift.amount);
        _moTa    = MoTaTheoItemId(gift.itemId);
    }

    /// <summary>Bản Init cho hàng vàng/gem nếu sau này muốn gắn thêm.</summary>
    public void Init(string ten, int soLuong, string moTa)
    {
        _tenQua  = ten ?? "";
        _soLuong = Mathf.Max(1, soLuong);
        _moTa    = moTa ?? "";
    }

    private void Awake()
    {
        // Ô quà phải NHẬN raycast thì mới chạm được (BuildProcedural đang để
        // raycastTarget = false toàn bộ). Không có Graphic nào → thêm Image tàng hình.
        var g = GetComponent<Graphic>();
        if (g == null)
        {
            var img = gameObject.AddComponent<Image>();
            img.color = Color.clear;
            g = img;
        }
        g.raycastTarget = true;
    }

    // =========================================================================
    //  CHẠM
    // =========================================================================

    public void OnPointerClick(PointerEventData eventData)
    {
        // Nhún mẩy
        if (_bounceCo == null)
        {
            // Chốt scale gốc LÚC ĐẦU LẦN NHÚN ĐẦU TIÊN (sau khi FitGiftRowV2 đã co dải quà),
            // để nhún xong trả về đúng kích thước dàn hàng, không phình to dần.
            if (!_baseScaleCaptured)
            {
                _baseScale = transform.localScale;
                _baseScaleCaptured = true;
            }
            _bounceCo = StartCoroutine(CoBounce());
        }

        // Tooltip
        HienTooltip();
    }

    private void OnDisable()
    {
        if (_bounceCo != null)
        {
            StopCoroutine(_bounceCo);
            _bounceCo = null;
            if (_baseScaleCaptured) transform.localScale = _baseScale;
        }
        // Tooltip đang trỏ vào ô này mà ô tắt (popup đóng) → ẩn luôn cho sạch.
        if (_tipHideHost == this) AnTooltipNgay();
    }

    // =========================================================================
    //  NHÚN MẨY — squash & stretch 2 nhịp, unscaled
    // =========================================================================

    private IEnumerator CoBounce()
    {
        Vector3 b = _baseScale;
        float t = 0f;
        while (t < BounceTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / BounceTime);

            float sx, sy;
            if (k < 0.18f)
            {
                // Nhịp 1: bẹp xuống lấy đà
                float p = k / 0.18f;
                sx = Mathf.Lerp(1f, 1.14f, p);
                sy = Mathf.Lerp(1f, 0.84f, p);
            }
            else if (k < 0.40f)
            {
                // Nhịp 2: bật vọt lên
                float p = (k - 0.18f) / 0.22f;
                sx = Mathf.Lerp(1.14f, 0.90f, p);
                sy = Mathf.Lerp(0.84f, 1.18f, p);
            }
            else
            {
                // Đuôi: dao động tắt dần về 1 (mẩy mẩy)
                float p = (k - 0.40f) / 0.60f;
                float wob = Mathf.Sin(p * Mathf.PI * 3f) * (1f - p) * 0.10f;
                sx = 1f - wob;
                sy = 1f + wob;
            }
            transform.localScale = new Vector3(b.x * sx, b.y * sy, b.z);
            yield return null;
        }
        transform.localScale = b;
        _bounceCo = null;
    }

    // =========================================================================
    //  TOOLTIP
    // =========================================================================

    private void HienTooltip()
    {
        var myRt = transform as RectTransform;
        if (myRt == null || myRt.parent == null) return;

        // Parent tooltip = CHA CỦA DẢI QUÀ (thường là ContentPanel) → nằm trên các ô,
        // chết theo popup. Fallback: cha trực tiếp của ô.
        RectTransform host = myRt.parent as RectTransform;
        if (host != null && host.parent is RectTransform hostCha) host = hostCha;
        if (host == null) return;

        if (_tipRoot == null) DungTooltip(host);
        else if (_tipRoot.parent != host) _tipRoot.SetParent(host, false);

        // Nội dung
        string tieuDe = _soLuong > 1 ? $"{_tenQua}  ×{_soLuong}" : _tenQua;
        if (_tipTitle != null) _tipTitle.text = tieuDe;
        if (_tipDesc  != null) _tipDesc.text  = _moTa;

        // Vị trí: ngay TRÊN ô quà, kẹp trong bề ngang host để không tràn màn hình
        Vector3 dinhO = myRt.TransformPoint(new Vector3(0f, myRt.rect.yMax * Mathf.Abs(myRt.localScale.y), 0f));
        Vector3 local = host.InverseTransformPoint(dinhO);
        float nuaRong = TooltipWidth * 0.5f;
        float xMin = host.rect.xMin + nuaRong + 8f;
        float xMax = host.rect.xMax - nuaRong - 8f;
        if (xMin < xMax) local.x = Mathf.Clamp(local.x, xMin, xMax);
        local.y += TooltipGapY + TooltipHeight * 0.5f;

        _tipRoot.anchoredPosition = new Vector2(local.x, local.y);
        _tipRoot.SetAsLastSibling();
        _tipRoot.gameObject.SetActive(true);

        // Pop nhẹ khi hiện
        _tipRoot.localScale = new Vector3(0.85f, 0.85f, 1f);
        StartCoroutine(CoTipPop());

        // Reset đồng hồ tự ẩn
        if (_tipHideCo != null && _tipHideHost != null) _tipHideHost.StopCoroutine(_tipHideCo);
        _tipHideHost = this;
        _tipHideCo   = StartCoroutine(CoTuAnTooltip());
    }

    private IEnumerator CoTipPop()
    {
        float t = 0f;
        const float dur = 0.14f;
        while (t < dur && _tipRoot != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            k = 1f + (1.7f + 1f) * Mathf.Pow(k - 1f, 3f) + 1.7f * Mathf.Pow(k - 1f, 2f); // easeOutBack
            float s = Mathf.LerpUnclamped(0.85f, 1f, k);
            _tipRoot.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (_tipRoot != null) _tipRoot.localScale = Vector3.one;
    }

    private IEnumerator CoTuAnTooltip()
    {
        yield return new WaitForSecondsRealtime(TooltipLifetime);
        AnTooltipNgay();
    }

    private static void AnTooltipNgay()
    {
        if (_tipHideCo != null && _tipHideHost != null) _tipHideHost.StopCoroutine(_tipHideCo);
        _tipHideCo   = null;
        _tipHideHost = null;
        if (_tipRoot != null) _tipRoot.gameObject.SetActive(false);
    }

    /// <summary>Dựng panel tooltip runtime: nền kem + viền nâu + 2 dòng chữ.</summary>
    private void DungTooltip(RectTransform host)
    {
        var go = new GameObject("GiftTooltip_V3", typeof(RectTransform));
        _tipRoot = (RectTransform)go.transform;
        _tipRoot.SetParent(host, false);
        _tipRoot.anchorMin = _tipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _tipRoot.pivot     = new Vector2(0.5f, 0.5f);
        _tipRoot.sizeDelta = new Vector2(TooltipWidth, TooltipHeight);

        // Viền nâu (rect ngoài) + nền kem (rect trong) — không cần sprite
        var vien = go.AddComponent<Image>();
        vien.color = new Color32(101, 65, 41, 235);        // nâu ấm #654129
        vien.raycastTarget = false;

        var nenGO = new GameObject("Nen", typeof(RectTransform));
        var nenRT = (RectTransform)nenGO.transform;
        nenRT.SetParent(_tipRoot, false);
        nenRT.anchorMin = Vector2.zero; nenRT.anchorMax = Vector2.one;
        nenRT.offsetMin = new Vector2(3f, 3f); nenRT.offsetMax = new Vector2(-3f, -3f);
        var nen = nenGO.AddComponent<Image>();
        nen.color = new Color32(255, 243, 220, 250);       // kem
        nen.raycastTarget = false;

        _tipTitle = TaoChu(nenRT, "TieuDe", new Vector2(0f, 15f), 21f, new Color32(108, 64, 34, 255), FontStyles.Bold);
        _tipDesc  = TaoChu(nenRT, "MoTa",   new Vector2(0f, -15f), 15.5f, new Color32(122, 88, 55, 255), FontStyles.Normal);
    }

    private static TextMeshProUGUI TaoChu(RectTransform cha, string ten, Vector2 pos, float size, Color mau, FontStyles style)
    {
        var go = new GameObject(ten, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(cha, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(TooltipWidth - 22f, 32f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize  = size;
        t.color     = mau;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode     = TextOverflowModes.Ellipsis;
        return t;
    }

    // =========================================================================
    //  MÔ TẢ THEO LOẠI ITEM (ItemGift chưa có field mô tả — suy từ itemId)
    // =========================================================================

    private static string MoTaTheoItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return "Vật phẩm quà tặng";
        if (itemId == "__gold") return "Vàng — cộng thẳng vào ví của bạn";
        if (itemId == "__gem")  return "Kim cương — tiền tệ cao cấp";
        if (itemId.StartsWith("seed_") || itemId == "khoai_tay" || itemId == "ca_rot")
            return "Hạt giống — gieo xuống ô đất để trồng";
        if (itemId == "mushroom")
            return "Nông sản — nguyên liệu nấu ăn, bán được ở chợ";
        return "Vật phẩm quà tặng — cộng thẳng vào kho";
    }
}
