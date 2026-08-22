using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HIỆU ỨNG BAO THÀNH PHẨM — vòng tròn đầu ra bên phải máy (node `Output_Bubble`).
///
/// ══════════════════════════════════════════════════════════════════════════
///  HAI NHỊP RIÊNG BIỆT
/// ══════════════════════════════════════════════════════════════════════════
///   1. <see cref="PhatRoi"/>  — MỘT LẦN, đúng lúc một mẻ vừa xay xong: bao nảy ra khỏi
///      máy (nhảy lên rồi rơi xuống, bẹp một nhịp khi chạm) + loé sáng. Đây là tín hiệu
///      "có hàng mới", người chơi đang nhìn chỗ khác vẫn bắt được bằng đuôi mắt.
///   2. <see cref="DatSanSang"/> — TRẠNG THÁI, bật khi còn hàng chưa thu: vòng sáng thở
///      nhè nhẹ + bao phồng/xẹp theo cùng nhịp. Tắt khi đã thu hết.
///
/// Nhịp 1 là sự kiện, nhịp 2 là trạng thái — nhập hai cái vào một hàm thì mở lại popup
/// (đã có hàng chờ từ phiên trước) sẽ nảy bao lần nữa như thể vừa xay xong.
///
/// ══════════════════════════════════════════════════════════════════════════
///  KHÔNG DI CHUYỂN BAO ĐI ĐÂU
/// ══════════════════════════════════════════════════════════════════════════
/// Theo chốt thiết kế: bao thành phẩm NẰM LẠI ở vòng tròn đầu ra sẵn có, không rơi xuống
/// nền đất. Vì vậy mọi phép biến đổi ở đây đều CỘNG THÊM lên
/// <c>anchoredPosition</c>/<c>localScale</c> GỐC đã lưu lúc Awake và luôn được đặt lại về
/// gốc khi kết thúc. Không bao giờ ghi đè vị trí tuyệt đối — MillPopupBuilderTool có thể
/// dựng lại node ở toạ độ khác và hiệu ứng vẫn phải đúng.
///
/// Toàn bộ dùng <c>Time.unscaledDeltaTime</c> vì popup mở lúc <c>timeScale = 0</c>.
/// </summary>
[DisallowMultipleComponent]
public class MillOutputBagFX : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Vòng tròn đầu ra (Output_Bubble). ĐỂ TRỐNG ⇒ dùng chính node này.")]
    [SerializeField] private RectTransform bao;

    [Tooltip("TUỲ CHỌN. Ảnh vòng sáng phía sau bao (sprite tròn toả sáng). " +
             "Để trống ⇒ chỉ phồng/xẹp, không có glow.")]
    [SerializeField] private Image imgGlow;

    [Header("Nhịp nảy khi vừa xay xong")]
    [Tooltip("Thời gian bao nảy ra, giây.")]
    [SerializeField] private float thoiGianNay = 0.5f;

    [Tooltip("Bao nhảy lên cao bao nhiêu pixel rồi rơi lại.")]
    [SerializeField] private float caoNhay = 26f;

    [Tooltip("Bao phồng tới bao nhiêu lần lúc đỉnh nhảy. 1.18 = to thêm 18%.")]
    [SerializeField] private float scaleDinh = 1.18f;

    [Tooltip("Bao bẹp còn bao nhiêu lúc chạm đáy. 0.9 = bẹp 10%.")]
    [SerializeField] private float scaleBep = 0.9f;

    [Header("Vòng sáng chờ thu")]
    [Tooltip("Một chu kỳ thở của vòng sáng, giây.")]
    [SerializeField] private float chuKyTho = 1.15f;

    [Tooltip("Độ mờ tối đa của vòng sáng.")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaGlowMax = 0.8f;

    [Tooltip("Biên độ phồng/xẹp của bao lúc chờ thu (tỉ lệ). 0.06 = ±6%.")]
    [SerializeField] private float bienTho = 0.06f;

    [Tooltip("Vòng sáng to hơn bao bao nhiêu lần lúc sáng nhất.\n" +
             "⚠ Giữ ≤ 1.2: khung máy (AnimationBox) rộng có hạn, phình quá thì vệt sáng " +
             "tràn ra nền kem của panel và nhìn như lỗi render.")]
    [SerializeField] private float scaleGlowMax = 1.18f;

    private Vector2 _viTriGoc;
    private Vector3 _scaleGoc  = Vector3.one;
    private Vector3 _scaleGlowGoc = Vector3.one;
    private bool    _daLuuGoc;

    private bool      _sanSang;
    private Coroutine _coTho;
    private Coroutine _coNay;

    private void Awake()
    {
        if (bao == null) bao = transform as RectTransform;

        // Vòng tròn đầu ra neo góc dưới-phải ⇒ pivot ở góc ⇒ phồng/xẹp bằng localScale sẽ
        // làm bao LAO CHÉO LÊN TRÁI 14px ở nhịp nảy (scale 1.18 × 80px), nhìn như bao bị
        // kéo đi chứ không phải thở. Đưa pivot về giữa TRƯỚC khi lưu gốc.
        MillRectUtil.DoiPivotVeGiua(bao);

        LuuGoc();

        // Glow luôn tắt lúc khởi động: prefab có thể được lưu ở trạng thái đang sáng.
        DatAlphaGlow(0f);
        if (imgGlow != null && imgGlow.gameObject.activeSelf)
            imgGlow.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        // Coroutine chết theo component ⇒ tự trả bao về đúng vị trí/kích cỡ gốc, nếu không
        // lần mở popup sau bao sẽ nằm lệch lên 26px hoặc phồng vĩnh viễn.
        _coTho = null;
        _coNay = null;
        _sanSang = false;

        VeGoc();
        DatAlphaGlow(0f);
        if (imgGlow != null && imgGlow.gameObject.activeSelf)
            imgGlow.gameObject.SetActive(false);
    }

    // ─────────────────────────── API CÔNG KHAI ───────────────────────────

    /// <summary>
    /// Bật/tắt trạng thái "còn hàng chờ thu" — vòng sáng thở nhè nhẹ.
    /// An toàn để gọi MỖI FRAME: có hàng rào, gọi lại cùng giá trị thì không làm gì.
    /// </summary>
    public void DatSanSang(bool on)
    {
        if (_sanSang == on) return;
        _sanSang = on;

        if (!isActiveAndEnabled) return;

        if (on)
        {
            if (imgGlow != null && !imgGlow.gameObject.activeSelf)
                imgGlow.gameObject.SetActive(true);

            if (_coTho == null) _coTho = StartCoroutine(CoTho());
        }
        else
        {
            if (_coTho != null) { StopCoroutine(_coTho); _coTho = null; }

            DatAlphaGlow(0f);
            if (imgGlow != null && imgGlow.gameObject.activeSelf)
                imgGlow.gameObject.SetActive(false);

            // Chỉ trả về gốc khi KHÔNG có nhịp nảy đang chạy — nếu không hai coroutine
            // giành nhau ghi localScale và bao giật.
            if (_coNay == null) VeGoc();
        }
    }

    /// <summary>
    /// Phát nhịp "bao vừa rơi ra khỏi máy". Gọi ĐÚNG MỘT LẦN mỗi mẻ xong.
    /// </summary>
    public void PhatRoi()
    {
        if (!isActiveAndEnabled) return;

        if (_coNay != null) StopCoroutine(_coNay);
        _coNay = StartCoroutine(CoNay());
    }

    // ─────────────────────────── COROUTINE ───────────────────────────

    private IEnumerator CoNay()
    {
        LuuGoc();

        float tong = Mathf.Max(0.1f, thoiGianNay);
        float t    = 0f;

        // Loé sáng ngay từ frame đầu: mắt bắt được ánh sáng trước khi bắt được chuyển động.
        if (imgGlow != null && !imgGlow.gameObject.activeSelf)
            imgGlow.gameObject.SetActive(true);

        while (t < tong)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tong);

            // Nửa chu kỳ sin cho độ cao: 0 → 1 → 0 (nhảy lên rồi rơi lại đúng chỗ cũ).
            float cao = Mathf.Sin(k * Mathf.PI);

            // Scale: phồng lúc bay lên (k<0.5), bẹp lúc chạm đáy (k≈0.85), rồi về 1.
            float s;
            if (k < 0.5f)
            {
                s = Mathf.Lerp(1f, scaleDinh, k / 0.5f);
            }
            else if (k < 0.85f)
            {
                s = Mathf.Lerp(scaleDinh, scaleBep, (k - 0.5f) / 0.35f);
            }
            else
            {
                s = Mathf.Lerp(scaleBep, 1f, (k - 0.85f) / 0.15f);
            }

            if (bao != null)
            {
                bao.anchoredPosition = _viTriGoc + Vector2.up * (caoNhay * cao);
                // Bẹp thì bè ngang ra — squash & stretch giữ "thể tích" nhìn có trọng lượng.
                bao.localScale = new Vector3(_scaleGoc.x * (2f - s), _scaleGoc.y * s, _scaleGoc.z);
            }

            // Glow loé mạnh rồi tắt dần trong nửa sau.
            DatAlphaGlow(alphaGlowMax * (1f - k));
            DatScaleGlow(Mathf.Lerp(scaleGlowMax, 1f, k));

            yield return null;
        }

        VeGoc();
        _coNay = null;

        // Còn hàng chờ thu thì chuyển ngay sang nhịp thở, đừng để tắt hẳn một khoảnh khắc.
        if (_sanSang)
        {
            if (imgGlow != null && !imgGlow.gameObject.activeSelf)
                imgGlow.gameObject.SetActive(true);
            if (_coTho == null) _coTho = StartCoroutine(CoTho());
        }
        else
        {
            DatAlphaGlow(0f);
            if (imgGlow != null && imgGlow.gameObject.activeSelf)
                imgGlow.gameObject.SetActive(false);
        }
    }

    private IEnumerator CoTho()
    {
        LuuGoc();

        float chuKy = Mathf.Max(0.2f, chuKyTho);
        float pha   = 0f;

        while (_sanSang)
        {
            pha += Time.unscaledDeltaTime / chuKy;
            if (pha >= 1f) pha -= 1f;

            // 0 → 1 → 0 mượt, không giật ở điểm nối vòng.
            float song = 0.5f - 0.5f * Mathf.Cos(pha * 2f * Mathf.PI);

            DatAlphaGlow(alphaGlowMax * song);
            DatScaleGlow(Mathf.Lerp(1f, scaleGlowMax, song));

            // Nhịp nảy đang chạy thì NHƯỜNG quyền ghi localScale cho nó.
            if (bao != null && _coNay == null)
            {
                float s = 1f + bienTho * song;
                bao.localScale = new Vector3(_scaleGoc.x * s, _scaleGoc.y * s, _scaleGoc.z);
            }

            yield return null;
        }

        _coTho = null;
    }

    // ─────────────────────────── NỘI BỘ ───────────────────────────

    private void LuuGoc()
    {
        if (_daLuuGoc) return;

        if (bao != null)
        {
            _viTriGoc = bao.anchoredPosition;
            _scaleGoc = bao.localScale;
        }

        if (imgGlow != null)
            _scaleGlowGoc = imgGlow.rectTransform.localScale;

        _daLuuGoc = true;
    }

    private void VeGoc()
    {
        if (!_daLuuGoc || bao == null) return;

        bao.anchoredPosition = _viTriGoc;
        bao.localScale       = _scaleGoc;
        DatScaleGlow(1f);
    }

    private void DatAlphaGlow(float a)
    {
        if (imgGlow == null) return;

        Color c = imgGlow.color;
        c.a = Mathf.Clamp01(a);
        imgGlow.color = c;
    }

    private void DatScaleGlow(float heSo)
    {
        if (imgGlow == null || !_daLuuGoc) return;

        imgGlow.rectTransform.localScale = _scaleGlowGoc * heSo;
    }
}
