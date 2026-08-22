using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HIỆU ỨNG "NGUYÊN LIỆU CHẢY VÀO MÁY" — phát MỘT LOẠT khi một mẻ xay bắt đầu.
///
/// ══════════════════════════════════════════════════════════════════════════
///  KHÁC GÌ VỚI ConveyorItem SẴN CÓ
/// ══════════════════════════════════════════════════════════════════════════
/// <see cref="ConveyorItem"/> là hoạt cảnh TRANG TRÍ: 2 bó cỏ chạy vòng lặp vô tận suốt
/// thời gian popup mở, kể cả khi máy rảnh (đúng bản HTML gốc, xem
/// `MillPopupUI.DatChayAnimation`). File này là PHẢN HỒI HÀNH ĐỘNG: người chơi vừa thả bao
/// vào slot ⇒ một loạt hạt bắn từ bong bóng nguyên liệu bay vào phễu máy rồi máy nhún một
/// nhịp. Hai thứ không thay thế nhau và không được gộp — gộp vào thì hoạt cảnh nền sẽ
/// đứng im lúc máy rảnh.
///
/// ══════════════════════════════════════════════════════════════════════════
///  THỜI GIAN KHÔNG SCALE
/// ══════════════════════════════════════════════════════════════════════════
/// Popup thường mở lúc <c>Time.timeScale = 0</c> (cùng quy ước với toast trong
/// `MillPopupUI.ChayToast`). Mọi phép cộng thời gian ở đây dùng
/// <c>Time.unscaledDeltaTime</c>; dùng deltaTime thì hạt đứng cứng giữa đường.
///
/// ══════════════════════════════════════════════════════════════════════════
///  TÁI DÙNG HẠT
/// ══════════════════════════════════════════════════════════════════════════
/// 5 slot có thể bắt đầu gần nhau ⇒ nhiều loạt chồng nhau. Hạt được lấy từ pool và trả về
/// pool, không Instantiate/Destroy từng hạt.
/// </summary>
[DisallowMultipleComponent]
public class MillIntakeFX : MonoBehaviour
{
    [Header("Điểm mốc")]
    [Tooltip("Nơi hạt bắn ra — bong bóng nguyên liệu bên trái máy (Bubble_Input).")]
    [SerializeField] private RectTransform diemXuatPhat;

    [Tooltip("Nơi hạt bay tới — miệng phễu / thân máy (Machine).")]
    [SerializeField] private RectTransform diemDich;

    [Tooltip("Node cha để gắn hạt. Nên là khu animation (AnimationBox) để hạt bị cắt gọn " +
             "trong khung máy. ĐỂ TRỐNG ⇒ dùng chính node này.")]
    [SerializeField] private RectTransform noiChuaHat;

    [Tooltip("TUỲ CHỌN. Thân máy, để nhún một nhịp khi nhận nguyên liệu. Để trống ⇒ không nhún.")]
    [SerializeField] private RectTransform thanMay;

    [Header("Số lượng & nhịp")]
    [Tooltip("Số hạt mỗi loạt.")]
    [Range(1, 12)]
    [SerializeField] private int soHat = 6;

    [Tooltip("Thời gian một hạt bay từ bong bóng vào máy, giây.")]
    [SerializeField] private float thoiGianBay = 0.5f;

    [Tooltip("Độ trễ giữa hai hạt liên tiếp, giây. Cho ra cảm giác 'chảy' thay vì 'nổ'.")]
    [SerializeField] private float treGiuaHat = 0.06f;

    [Tooltip("Cạnh của một hạt, pixel.")]
    [SerializeField] private float kichCoHat = 24f;

    [Tooltip("Độ vồng của đường bay, pixel. 0 = bay thẳng.\n" +
             "⚠ Giữ ≤ 32: bong bóng nguyên liệu nằm cách mép trên khung máy ~70px, vồng " +
             "cao hơn là hạt nhô lên khỏi khung.")]
    [SerializeField] private float doVong = 30f;

    [Tooltip("Lệch ngẫu nhiên vị trí xuất phát, pixel — để các hạt không xếp thành một hàng.")]
    [SerializeField] private float lechNgauNhien = 10f;

    [Header("Nhún máy")]
    [Tooltip("Biên độ nhún của thân máy (tỉ lệ). 0.08 = phình/bẹp 8%.")]
    [SerializeField] private float bienNhun = 0.07f;

    [Tooltip("Thời gian một nhịp nhún, giây.")]
    [SerializeField] private float thoiGianNhun = 0.26f;

    /// <summary>Pool hạt. Phần tử có thể là fake-null nếu ai đó xoá tay trong Editor.</summary>
    private readonly List<RectTransform> _pool    = new List<RectTransform>();
    private readonly List<RectTransform> _dangDung = new List<RectTransform>();

    private Canvas    _canvas;
    private Coroutine _coNhun;
    private Vector3   _scaleMayGoc = Vector3.one;
    private bool      _daLuuScaleMay;

    private void Awake()
    {
        if (noiChuaHat == null) noiChuaHat = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();

        if (thanMay != null && !_daLuuScaleMay)
        {
            // Máy được neo góc dưới-phải ⇒ pivot ở góc ⇒ nhún bằng localScale sẽ làm máy
            // LAO CHÉO thay vì bẹp tại chỗ. Đưa pivot về giữa (không xê dịch hình, cũng
            // không xê dịch bánh răng con) — xem MillRectUtil.
            MillRectUtil.DoiPivotVeGiua(thanMay);

            _scaleMayGoc   = thanMay.localScale;
            _daLuuScaleMay = true;
        }
    }

    private void OnDisable()
    {
        // Coroutine chết theo component ⇒ trả hết hạt về pool bằng tay, nếu không lần mở
        // popup sau sẽ thấy vài hạt đứng bất động giữa khung máy.
        for (int i = 0; i < _dangDung.Count; i++)
        {
            RectTransform h = _dangDung[i];
            if (h == null) continue;
            if (h.gameObject.activeSelf) h.gameObject.SetActive(false);
            _pool.Add(h);
        }
        _dangDung.Clear();

        _coNhun = null;
        if (thanMay != null && _daLuuScaleMay) thanMay.localScale = _scaleMayGoc;
    }

    // ─────────────────────────── API CÔNG KHAI ───────────────────────────

    /// <summary>
    /// Phát một loạt hạt <paramref name="icon"/> bay vào máy và nhún máy một nhịp.
    /// Gọi được nhiều lần liên tiếp — các loạt chồng nhau vô hại.
    /// </summary>
    /// <param name="icon">Sprite nguyên liệu. null ⇒ bỏ qua toàn bộ hiệu ứng (không vẽ ô trắng).</param>
    public void Chay(Sprite icon)
    {
        if (!isActiveAndEnabled) return;
        if (icon == null) return;
        if (noiChuaHat == null || diemXuatPhat == null || diemDich == null) return;

        Vector2 tu  = QuyVeCucBo(diemXuatPhat);
        Vector2 den = QuyVeCucBo(diemDich);

        int n = Mathf.Clamp(soHat, 1, 12);
        for (int i = 0; i < n; i++)
            StartCoroutine(BayMotHat(icon, tu, den, i * Mathf.Max(0f, treGiuaHat)));

        NhunMay();
    }

    /// <summary>Nhún thân máy một nhịp. Tách riêng để nơi khác dùng lại được.</summary>
    public void NhunMay()
    {
        if (thanMay == null || !isActiveAndEnabled) return;

        if (_coNhun != null) StopCoroutine(_coNhun);
        _coNhun = StartCoroutine(CoNhun());
    }

    // ─────────────────────────── COROUTINE ───────────────────────────

    private IEnumerator BayMotHat(Sprite icon, Vector2 tu, Vector2 den, float tre)
    {
        if (tre > 0f)
        {
            float doi = 0f;
            while (doi < tre)
            {
                doi += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        RectTransform hat = LayHat(icon);
        if (hat == null) yield break;

        Vector2 batDau = tu + Random.insideUnitCircle * Mathf.Max(0f, lechNgauNhien);

        // Điểm điều khiển Bezier bậc 2: giữa đường, đẩy LÊN doVong pixel ⇒ đường bay vồng
        // như hạt được hắt lên rồi rơi vào phễu.
        Vector2 giua = (batDau + den) * 0.5f + Vector2.up * doVong;

        float tong = Mathf.Max(0.05f, thoiGianBay);
        float t    = 0f;

        hat.anchoredPosition = batDau;
        hat.localScale       = Vector3.one;

        Image img = hat.GetComponent<Image>();
        float xoay = Random.Range(-220f, 220f);

        while (t < tong)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tong);

            hat.anchoredPosition = Bezier(batDau, giua, den, k);
            hat.Rotate(0f, 0f, xoay * Time.unscaledDeltaTime);

            // Thu nhỏ + mờ dần ở 35% cuối: hạt "lọt vào" máy chứ không biến mất đột ngột.
            float cuoi = Mathf.InverseLerp(0.65f, 1f, k);
            float s    = Mathf.Lerp(1f, 0.45f, cuoi);
            hat.localScale = new Vector3(s, s, 1f);

            if (img != null)
            {
                Color c = img.color;
                c.a = 1f - cuoi;
                img.color = c;
            }

            yield return null;
        }

        TraHat(hat);
    }

    private IEnumerator CoNhun()
    {
        float tong = Mathf.Max(0.05f, thoiGianNhun);
        float t    = 0f;

        while (t < tong)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tong);

            // Một nửa chu kỳ sin: 0 → 1 → 0. Bẹp theo Y, phình theo X (squash & stretch).
            float song = Mathf.Sin(k * Mathf.PI);
            float b    = bienNhun * song;

            if (thanMay != null)
                thanMay.localScale = new Vector3(_scaleMayGoc.x * (1f + b),
                                                 _scaleMayGoc.y * (1f - b),
                                                 _scaleMayGoc.z);
            yield return null;
        }

        if (thanMay != null) thanMay.localScale = _scaleMayGoc;
        _coNhun = null;
    }

    // ─────────────────────────── POOL ───────────────────────────

    private RectTransform LayHat(Sprite icon)
    {
        RectTransform hat = null;

        while (_pool.Count > 0 && hat == null)
        {
            int cuoi = _pool.Count - 1;
            hat = _pool[cuoi];
            _pool.RemoveAt(cuoi);
            // hat có thể là fake-null (bị Destroy ngoài ý muốn) ⇒ vòng while thử tiếp.
        }

        if (hat == null)
        {
            var go = new GameObject("IntakeGrain", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = noiChuaHat.gameObject.layer;

            hat = (RectTransform)go.transform;
            hat.SetParent(noiChuaHat, false);

            // Neo TRÙNG PIVOT của cha, không cứng (0.5, 0.5): AnimationBox neo góc trên-trái
            // nên neo giữa sẽ lệch nửa khung (314, −125). Xem MillRectUtil.
            MillRectUtil.DatNeoTheoPivotCha(hat, noiChuaHat);

            Image im = go.GetComponent<Image>();
            im.raycastTarget  = false;   // hạt bay ngang con trỏ, không được ăn click
            im.preserveAspect = true;
        }

        float c = (kichCoHat > 1f) ? kichCoHat : 24f;
        hat.sizeDelta      = new Vector2(c, c);
        hat.localRotation  = Quaternion.identity;
        hat.SetAsLastSibling();

        Image img = hat.GetComponent<Image>();
        if (img != null)
        {
            img.sprite  = icon;
            img.enabled = true;
            img.color   = Color.white;
        }

        if (!hat.gameObject.activeSelf) hat.gameObject.SetActive(true);
        _dangDung.Add(hat);
        return hat;
    }

    private void TraHat(RectTransform hat)
    {
        if (hat == null) return;

        _dangDung.Remove(hat);

        if (hat.gameObject.activeSelf) hat.gameObject.SetActive(false);
        _pool.Add(hat);
    }

    // ─────────────────────────── TOÁN ───────────────────────────

    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return (u * u) * a + (2f * u * t) * b + (t * t) * c;
    }

    /// <summary>
    /// Đổi vị trí của một RectTransform bất kỳ sang toạ độ cục bộ của <c>noiChuaHat</c>.
    /// Hai node có thể ở hai nhánh khác nhau nên không thể trừ anchoredPosition trực tiếp.
    ///
    /// Dùng TÂM hình, không dùng `muc.position`: bong bóng nguyên liệu neo góc trên-trái và
    /// máy neo góc dưới-phải, `.position` của chúng là vị trí PIVOT (một góc) nên hạt sẽ bắn
    /// từ mép chứ không từ giữa. Xem MillRectUtil.
    /// </summary>
    private Vector2 QuyVeCucBo(RectTransform muc)
    {
        return MillRectUtil.QuyVeCucBo(noiChuaHat, MillRectUtil.TamWorld(muc), _canvas);
    }
}
