using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// KHÓI + BONG BÓNG của máy xay — dấu hiệu "máy đang chạy".
///
/// ══════════════════════════════════════════════════════════════════════════
///  BA THỨ KHÁC NHAU, ĐỪNG GỘP
/// ══════════════════════════════════════════════════════════════════════════
/// Popup máy xay có ba lớp chuyển động, mỗi lớp trả lời một câu khác nhau:
///
///   • <see cref="ConveyorItem"/> + `RotatingGear` + `UIScrollingTexture`
///       → "popup này còn sống" — chạy vòng lặp VÔ TẬN suốt lúc popup mở, kể cả máy rảnh
///         (đúng bản HTML gốc, xem `MillPopupUI.DatChayAnimation`).
///   • <see cref="MillIntakeFX"/>
///       → "vừa nhận nguyên liệu" — một loạt hạt, phát MỘT LẦN lúc thả bao vào slot.
///   • FILE NÀY
///       → "cái máy này là máy thật, và nó đang làm gì" — khói bay khỏi phễu SUỐT thời gian
///         popup mở, nhưng ĐẶC KHÁC NHAU:
///           máy rảnh    → sợi khói mỏng, thưa (chu kỳ ~1.1s, nhỏ hơn 30%, mờ hơn)
///           đang xay    → dòng khói dày, liên tục (chu kỳ ~0.4s, đầy cỡ, đậm)
///         Nhờ vậy người chơi vẫn phân biệt được rảnh / đang chạy mà không cần đọc chữ,
///         đồng thời popup KHÔNG BAO GIỜ đứng im — đúng yêu cầu "thêm tí animation".
///
/// Gộp lớp thứ ba vào lớp thứ nhất là mất luôn thông tin đó.
///
/// ══════════════════════════════════════════════════════════════════════════
///  KHÓI PHẢI NẰM GỌN TRONG KHUNG GỖ
/// ══════════════════════════════════════════════════════════════════════════
/// Khu animation KHÔNG có mask (node `Conveyor` đã mang một stencil `Mask`, chồng mask lên
/// nhau là rủi ro không cần thiết). Vì vậy khói được giới hạn bằng HÌNH HỌC:
/// `MillDesign` cố ý đặt máy cao 280 với lề dưới 24 để còn **74px trời** phía trên đỉnh
/// phễu. `caoBay` mặc định 66 < 74 nên cụm khói tan trước khi ra khỏi khung.
///
/// ⚠ Nâng `caoBay` quá 58 là khói lòi ra nền kem của panel, nhìn như lỗi render.
/// Phép tính đầy đủ ở tooltip của `caoBay` — nhớ trừ cả `khoiCoCuoi/2`, vì cụm khói LOÃNG
/// RA khi bay lên nên mép trên của nó cao hơn TÂM một nửa chiều rộng. Bản trước quên nửa
/// này. Nếu cần khói cao hơn thì phải hạ `MillDesign.MachineSize` trước.
///
/// ══════════════════════════════════════════════════════════════════════════
///  ART CÓ SẴN, KHÔNG VẼ MỚI
/// ══════════════════════════════════════════════════════════════════════════
/// <c>anhKhoi</c> = `Assets/_Game/Farm/Art/UI_OrderBoard/ob_smoke.png` (128², do
/// `OrderBoardSpriteFactory.SmokePuff` sinh ra) — một cụm khói có biên gợn thật, không phải
/// hình tròn. `OrderDeliverFxUI` đã dùng nó cho hiệu ứng giao đơn.
/// <c>anhBongBong</c> = `Lana Studio/Hyper Casual FX/Textures/Circle01.png` (128²).
///
/// ══════════════════════════════════════════════════════════════════════════
///  THỜI GIAN KHÔNG SCALE
/// ══════════════════════════════════════════════════════════════════════════
/// Popup mở lúc <c>Time.timeScale = 0</c> ⇒ toàn bộ dùng <c>Time.unscaledDeltaTime</c>.
/// Dùng `deltaTime` thì khói treo lơ lửng bất động — nhìn tệ hơn là không có khói.
/// </summary>
[DisallowMultipleComponent]
public class MillSmokeFX : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Node cha để gắn khói. Nên là khu animation (AnimationBox). ĐỂ TRỐNG ⇒ dùng node này.")]
    [SerializeField] private RectTransform noiChua;

    [Tooltip("Miệng phun — phễu / thân máy. Khói bay lên từ ĐỈNH node này.")]
    [SerializeField] private RectTransform mieng;

    [Tooltip("Sprite cụm khói. Để trống ⇒ không có khói (bong bóng vẫn chạy nếu có sprite).")]
    [SerializeField] private Sprite anhKhoi;

    [Tooltip("Sprite bong bóng tròn. Để trống ⇒ bỏ phần bong bóng.")]
    [SerializeField] private Sprite anhBongBong;

    [Header("Khói")]
    [Tooltip("Khoảng cách giữa hai cụm khói KHI ĐANG XAY, giây.")]
    [SerializeField] private float chuKyPhun = 0.28f;

    [Tooltip("Khoảng cách giữa hai cụm khói KHI MÁY RẢNH, giây. Lớn hơn ⇒ khói thưa hơn.\n" +
             "Đặt 0 để TẮT hẳn khói lúc rảnh.")]
    [SerializeField] private float chuKyPhunRanh = 0.75f;

    [Tooltip("Cụm khói lúc máy rảnh nhỏ và mờ đi bao nhiêu lần. 0.82 = còn 82%.\n" +
             "⚠ ĐỪNG hạ xuống dưới 0.7: nền phía sau là TRỜI SÁNG, khói mờ quá là vô hình.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float heSoRanh = 0.85f;

    [Tooltip("Cạnh cụm khói lúc mới ra, pixel.")]
    [SerializeField] private float khoiCoDau = 36f;

    [Tooltip("Cạnh cụm khói lúc tan, pixel. Khói LOÃNG RA khi bay lên nên phải lớn hơn.")]
    [SerializeField] private float khoiCoCuoi = 120f;

    [Tooltip("Khói bay lên cao bao nhiêu pixel trước khi TÂM cụm khói đi hết đường.")]
    [SerializeField] private float caoBay = 160f;

    [Tooltip("Độ dạt ngang tối đa, pixel. Cho khói uốn nhẹ thay vì đi thẳng đứng.")]
    [SerializeField] private float dat = 32f;

    [Tooltip("Thời gian sống của một cụm khói, giây.")]
    [SerializeField] private float thoiGianKhoi = 2.0f;

    [Tooltip("Độ mờ tối đa của khói.")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaKhoi = 0.92f;

    [Tooltip("Màu khói.")]
    [SerializeField] private Color mauKhoi = Color.white;

    [Tooltip("Lệch ngang điểm phun so với đỉnh phễu, pixel. Âm = lệch trái.")]
    [SerializeField] private float lechMieng = 0f;

    [Tooltip("Nhấc điểm phun lên khỏi đỉnh phễu, pixel. 0 = phun ngay tại miệng phễu.")]
    [SerializeField] private float nangMieng = 2f;

    [Header("Bong bóng")]
    [Tooltip("Cứ bao nhiêu cụm khói thì kèm một bong bóng. 3 = mỗi 3 cụm. 0 = tắt.")]
    [Range(0, 8)]
    [SerializeField] private int cuMayNhipMotBong = 3;

    [Tooltip("Cạnh bong bóng, pixel.")]
    [SerializeField] private float bongCo = 16f;

    [Tooltip("Thời gian sống của bong bóng, giây.")]
    [SerializeField] private float thoiGianBong = 1.3f;

    [Tooltip("Biên độ lắc ngang của bong bóng, pixel.")]
    [SerializeField] private float bongLac = 13f;

    [Tooltip("Độ mờ của bong bóng.")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaBong = 0.7f;

    private MillFxPool _pool;
    private Canvas     _canvas;
    private Coroutine  _coPhun;
    private bool       _dangXay;
    private int        _demNhip;

    private void Awake()
    {
        if (noiChua == null) noiChua = transform as RectTransform;

        _canvas = GetComponentInParent<Canvas>();
        _pool   = new MillFxPool(noiChua, "SmokePuff");
    }

    private void OnEnable()
    {
        // Dòng khói chạy SUỐT thời gian popup mở (rảnh thì thưa, đang xay thì dày) —
        // không đợi ai gọi DatChay. Nhờ vậy mở popup ra là đã thấy máy "sống".
        if (_coPhun == null) _coPhun = StartCoroutine(CoDongKhoi());
    }

    private void OnDisable()
    {
        _coPhun  = null;
        _dangXay = false;
        DonSach();
    }

    // ─────────────────────────── API CÔNG KHAI ───────────────────────────

    /// <summary>
    /// Báo máy có đang xay hay không. KHÔNG bật/tắt dòng khói — dòng khói luôn chạy, hàm này
    /// chỉ đổi MẬT ĐỘ: rảnh ⇒ sợi mỏng thưa, đang xay ⇒ dòng dày liên tục.
    ///
    /// An toàn để gọi MỖI FRAME (chỉ gán một bool).
    /// </summary>
    public void DatChay(bool dangXay)
    {
        _dangXay = dangXay;

        // Dòng có thể đã chết vì DonSach() gọi StopAllCoroutines — dựng lại.
        if (isActiveAndEnabled && _coPhun == null)
            _coPhun = StartCoroutine(CoDongKhoi());
    }

    /// <summary>Phun một cụm khói ngay, không cần bật dòng. Dùng lúc máy vừa nhận nguyên liệu.</summary>
    public void PhunMotNhip()
    {
        if (!isActiveAndEnabled || noiChua == null) return;

        Vector2 goc = LayDiemPhun();
        if (anhKhoi != null) StartCoroutine(CoMotCumKhoi(goc, 1f));

        if (anhBongBong != null) StartCoroutine(CoMotBong(goc, 1f));
    }

    /// <summary>Trả toàn bộ hạt về pool và dừng phun. Gọi khi đóng popup.</summary>
    public void DonSach()
    {
        StopAllCoroutines();
        _coPhun = null;
        if (_pool != null) _pool.TraHet();
    }

    // ─────────────────────────── COROUTINE ───────────────────────────

    private IEnumerator CoDongKhoi()
    {
        // Cụm đầu ra NGAY, không chờ một chu kỳ: người chơi vừa thả bao xong, chờ 0.4s mới
        // thấy khói là mất liên hệ nhân-quả.
        while (true)
        {
            Vector2 goc = LayDiemPhun();
            float he = _dangXay ? 1f : Mathf.Clamp(heSoRanh, 0.2f, 1f);

            if (anhKhoi != null)
                StartCoroutine(CoMotCumKhoi(goc, he));

            _demNhip++;
            // Lúc rảnh thì bong bóng thưa gấp đôi — nếu không, máy rảnh mà sủi bong bóng
            // liên tục nhìn như đang chạy.
            int nhipBong = cuMayNhipMotBong * (_dangXay ? 1 : 2);
            if (anhBongBong != null && nhipBong > 0 && (_demNhip % nhipBong) == 0)
                StartCoroutine(CoMotBong(goc, he));

            // Rảnh: chu kỳ dài (khói thưa). chuKyPhunRanh = 0 ⇒ tắt hẳn khói lúc rảnh.
            float chuKy = _dangXay ? chuKyPhun : chuKyPhunRanh;
            if (!_dangXay && chuKyPhunRanh <= 0.01f)
            {
                // Không phun gì, nhưng vẫn phải nhường frame để không treo Unity.
                yield return null;
                continue;
            }

            float cho = Mathf.Max(0.08f, chuKy) * Random.Range(0.85f, 1.15f);
            float t   = 0f;
            while (t < cho)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator CoMotCumKhoi(Vector2 goc, float he)
    {
        Image img = _pool.Lay(anhKhoi, khoiCoDau * he);
        if (img == null) yield break;

        RectTransform rt = img.rectTransform;

        // Khói nổi lên trên mặt trước máy và nền trời
        rt.SetAsLastSibling();

        float huongDat = Random.Range(-1f, 1f);
        float xoay     = Random.Range(-40f, 40f);
        float tong     = Mathf.Max(0.15f, thoiGianKhoi * Random.Range(0.85f, 1.15f));
        float t        = 0f;

        rt.anchoredPosition = goc;

        while (t < tong)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tong);

            // Lên nhanh rồi chậm dần — khói nóng mất động lượng khi loãng.
            float len = 1f - (1f - k) * (1f - k);

            // Dạt ngang theo sin để đường khói uốn, cộng thêm hướng ngẫu nhiên của cụm.
            float ngang = Mathf.Sin(k * 3.1f) * dat * huongDat;

            rt.anchoredPosition = goc + new Vector2(ngang, caoBay * he * len);

            float co = Mathf.Lerp(khoiCoDau, khoiCoCuoi, k) * he;
            rt.sizeDelta = new Vector2(co, co);
            rt.Rotate(0f, 0f, xoay * Time.unscaledDeltaTime);

            // Hiện nhanh trong 15% đầu rồi tan dần — không bật ra ở alpha tối đa.
            float a = (k < 0.15f) ? (k / 0.15f) : (1f - Mathf.InverseLerp(0.15f, 1f, k));
            Color c = mauKhoi;
            c.a = alphaKhoi * a * he;
            img.color = c;

            yield return null;
        }

        _pool.Tra(img);
    }

    private IEnumerator CoMotBong(Vector2 goc, float he)
    {
        Image img = _pool.Lay(anhBongBong, bongCo * he);
        if (img == null) yield break;

        RectTransform rt = img.rectTransform;
        rt.SetAsLastSibling();

        float pha  = Random.Range(0f, 6.28f);
        float tong = Mathf.Max(0.2f, thoiGianBong * Random.Range(0.85f, 1.15f));
        float t    = 0f;

        // Bong bóng bay cao hơn khói một chút và không loãng ra — nó vỡ chứ không tan.
        // Hệ số 1.05 (không phải 1.25): 66 × 1.05 = 69 < 74px trời chừa trên đỉnh phễu.
        float cao = caoBay * 1.05f * he;

        while (t < tong)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tong);

            float ngang = Mathf.Sin(pha + k * 7.5f) * bongLac;
            rt.anchoredPosition = goc + new Vector2(ngang, cao * k);

            // Phồng nhẹ suốt đường lên, rồi VỠ ở 12% cuối: phóng nhanh + mờ hẳn.
            float co = 1f + 0.25f * k;
            if (k > 0.88f)
            {
                float v = (k - 0.88f) / 0.12f;
                co += v * 0.75f;
            }
            rt.localScale = new Vector3(co, co, 1f);

            float a = (k < 0.12f) ? (k / 0.12f) : (k > 0.88f ? (1f - (k - 0.88f) / 0.12f) : 1f);
            Color c = img.color;
            c.a = alphaBong * a * he;
            img.color = c;

            yield return null;
        }

        _pool.Tra(img);
    }

    // ─────────────────────────── NỘI BỘ ───────────────────────────

    /// <summary>
    /// Điểm phun: ĐỈNH của node <c>mieng</c>, quy về hệ toạ độ cục bộ của <c>noiChua</c>.
    ///
    /// Không dùng <c>mieng.position</c> (là pivot, mà pivot của Machine nằm ở góc dưới-phải)
    /// và không dùng tâm — khói phải ra từ miệng phễu ở TRÊN. Lấy tâm rồi cộng nửa chiều cao.
    /// </summary>
    private Vector2 LayDiemPhun()
    {
        if (mieng == null) return Vector2.zero;

        Vector2 tam = MillRectUtil.QuyVeCucBo(noiChua, MillRectUtil.TamWorld(mieng), _canvas);
        float nuaCao = mieng.rect.height * 0.5f;

        float offsetSpreadX = Random.Range(-20f, 25f);
        return tam + new Vector2(lechMieng + offsetSpreadX, nuaCao + nangMieng);
    }
}
