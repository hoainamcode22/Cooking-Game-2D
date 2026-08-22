using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PHÁO HOA "BÙM BÙM" khi một mẻ xay hoàn tất.
///
/// ══════════════════════════════════════════════════════════════════════════
///  DÙNG ART PHÁO HOA CÓ SẴN TRONG PROJECT
/// ══════════════════════════════════════════════════════════════════════════
/// Sprite lấy từ `Assets/Lana Studio/Hyper Casual FX/Textures/` — ĐÚNG bộ art mà popup
/// Level Up đang dùng (`Confetti_blast_multicolor.prefab`, `Flash_magic_blue_pink.prefab`,
/// xem `LevelUpPopupUI.cs`). MillPopupBuilderTool wire sẵn:
///     anhGiay = confetti_large / Square01 / Plus01   ·   anhSao = Star01   ·   anhLoe = Flare01
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO KHÔNG Instantiate THẲNG PREFAB ParticleSystem CỦA LANA
/// ══════════════════════════════════════════════════════════════════════════
/// Ba lý do, cả ba đều chặn:
///
/// 1. **THỨ TỰ VẼ.** ParticleSystem là `Renderer` trong không gian world. Canvas gốc của
///    dự án (`Canvas_Popup`) là **ScreenSpaceOverlay** — loại canvas này vẽ SAU toàn bộ
///    world, không có cách nào chen hạt vào giữa. Popup máy xay lại có lớp `Dim` đen 55%
///    phủ kín màn hình. Nên pháo hoa world sẽ nằm sau lớp dim ⇒ người chơi thấy một màn
///    xám, không thấy hạt nào. (Muốn hạt world lên trên thì phải đổi canvas sang
///    ScreenSpaceCamera + planeDistance — sửa canvas dùng chung cho MỌI popup của dự án chỉ
///    vì một hiệu ứng là cái giá quá đắt.)
/// 2. **KHÔNG LOAD ĐƯỢC LÚC RUNTIME.** Prefab nằm ngoài `Resources/` nên không
///    `Resources.Load` được. `ConstructionManager.ResolveCompleteVfxPrefab()` phải lách
///    bằng **reflection vào field private của `LevelUpPopupUI`** — một sợi dây rất dễ đứt.
/// 3. **timeScale = 0.** Popup mở lúc game tạm dừng. Mọi emitter phải được ép
///    `main.useUnscaledTime = true` bằng tay sau khi Instantiate (`LevelUpPopupUI.cs:770`);
///    quên một cái là hạt đứng cứng.
///
/// ⇒ File này vẽ pháo hoa bằng **Image trong UI**, cùng art, cùng canvas với popup (order
/// 400) nên chắc chắn nằm trên, và dùng `Time.unscaledDeltaTime` nên chạy đúng khi tạm dừng.
///
/// ══════════════════════════════════════════════════════════════════════════
///  "BÙM BÙM" = HAI LOẠT
/// ══════════════════════════════════════════════════════════════════════════
/// <c>soLoat = 2</c>, cách nhau <c>treGiuaLoat</c>. Đây chính là nhịp mà
/// `LevelUpPopupUI.VfxBurstLoop()` tạo ra bằng cách gọi lại `ps.Play()` sau một
/// `WaitForSecondsRealtime`.
/// </summary>
[DisallowMultipleComponent]
public class MillCelebrationFX : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Node cha để gắn hạt. Nên là khu animation (AnimationBox). ĐỂ TRỐNG ⇒ dùng node này.")]
    [SerializeField] private RectTransform noiChua;

    [Tooltip("Các sprite mảnh giấy. Code chọn ngẫu nhiên trong danh sách cho mỗi mảnh.\n" +
             "Để TRỐNG MẢNG ⇒ chỉ có ngôi sao và vệt loé.")]
    [SerializeField] private Sprite[] anhGiay;

    [Tooltip("Sprite ngôi sao — vài cái to bung ra cùng lúc. Để trống ⇒ bỏ phần sao.")]
    [SerializeField] private Sprite anhSao;

    [Tooltip("Sprite vệt loé giữa tâm nổ. Để trống ⇒ bỏ phần loé.")]
    [SerializeField] private Sprite anhLoe;

    [Header("Nhịp nổ")]
    [Tooltip("Số loạt. 2 = \"bùm bùm\".")]
    [Range(1, 4)]
    [SerializeField] private int soLoat = 2;

    [Tooltip("Khoảng cách giữa hai loạt, giây.")]
    [SerializeField] private float treGiuaLoat = 0.26f;

    [Header("Mảnh giấy")]
    [Tooltip("Số mảnh giấy mỗi loạt.")]
    [Range(4, 60)]
    [SerializeField] private int soGiay = 22;

    [Tooltip("Cạnh một mảnh giấy, pixel.")]
    [SerializeField] private float kichCoGiay = 20f;

    [Tooltip("Vận tốc bắn ra ban đầu, pixel/giây. Mỗi mảnh lấy ngẫu nhiên 60–100% giá trị này.")]
    [SerializeField] private float tocDoBan = 340f;

    [Tooltip("Gia tốc rơi, pixel/giây². Cho mảnh giấy bay lên rồi rơi xuống như thật.")]
    [SerializeField] private float trongLuc = 620f;

    [Tooltip("Thời gian sống của một mảnh giấy, giây.")]
    [SerializeField] private float thoiGianSong = 1.05f;

    [Tooltip("Hệ số cản — mảnh giấy chậm dần thay vì bay thẳng mãi. 0 = không cản.")]
    [Range(0f, 6f)]
    [SerializeField] private float heSoCan = 2.2f;

    [Header("Ngôi sao")]
    [Tooltip("Số ngôi sao mỗi loạt.")]
    [Range(0, 10)]
    [SerializeField] private int soSao = 5;

    [Tooltip("Cạnh một ngôi sao, pixel.")]
    [SerializeField] private float kichCoSao = 46f;

    [Tooltip("Ngôi sao bung xa bao nhiêu pixel.")]
    [SerializeField] private float banKinhSao = 96f;

    [Tooltip("Thời gian sống của ngôi sao, giây.")]
    [SerializeField] private float thoiGianSao = 0.55f;

    [Header("Vệt loé")]
    [Tooltip("Cạnh vệt loé lúc to nhất, pixel.")]
    [SerializeField] private float kichCoLoe = 170f;

    [Tooltip("Thời gian vệt loé, giây. Nên rất ngắn — nó là cú \"bùm\".")]
    [SerializeField] private float thoiGianLoe = 0.22f;

    [Tooltip("Độ mờ tối đa của vệt loé.")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaLoe = 0.85f;

    private MillFxPool _pool;
    private Canvas     _canvas;

    private void Awake()
    {
        if (noiChua == null) noiChua = transform as RectTransform;

        _canvas = GetComponentInParent<Canvas>();
        _pool   = new MillFxPool(noiChua, "CelebrationBit");
    }

    private void OnDisable()
    {
        // Coroutine chết theo component ⇒ không ai trả hạt về pool. Thiếu bước này là vài
        // mảnh giấy đứng bất động giữa khung máy ở lần mở popup sau.
        DonSach();
    }

    // ─────────────────────────── API CÔNG KHAI ───────────────────────────

    /// <summary>
    /// Nổ pháo hoa tại TÂM của <paramref name="muc"/>.
    ///
    /// Dùng tâm, không dùng <c>muc.position</c>: node trong popup được neo bằng các helper
    /// TL/TR/BL/BR nên pivot nằm ở GÓC, `.position` sẽ cho ra mép chứ không phải giữa.
    /// Xem <see cref="MillRectUtil"/>.
    /// </summary>
    public void BumTai(RectTransform muc)
    {
        if (muc == null) return;
        Bum(MillRectUtil.QuyVeCucBo(noiChua, MillRectUtil.TamWorld(muc), _canvas));
    }

    /// <summary>Nổ pháo hoa tại một điểm trong hệ toạ độ cục bộ của <c>noiChua</c>.</summary>
    public void Bum(Vector2 tam)
    {
        if (!isActiveAndEnabled || noiChua == null) return;

        int n = Mathf.Clamp(soLoat, 1, 4);
        for (int i = 0; i < n; i++)
            StartCoroutine(CoMotLoat(tam, i * Mathf.Max(0f, treGiuaLoat), i));
    }

    /// <summary>Trả toàn bộ hạt về pool. Gọi khi đóng popup.</summary>
    public void DonSach()
    {
        StopAllCoroutines();
        if (_pool != null) _pool.TraHet();
    }

    // ─────────────────────────── COROUTINE ───────────────────────────

    private IEnumerator CoMotLoat(Vector2 tam, float tre, int chiSoLoat)
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

        // Loạt sau nhỏ hơn loạt đầu — nghe như tiếng vọng, không phải hai cú giống nhau.
        float coHep = (chiSoLoat == 0) ? 1f : 0.78f;

        if (anhLoe != null)
            StartCoroutine(CoLoe(tam, coHep));

        if (anhGiay != null && anhGiay.Length > 0)
        {
            int sg = Mathf.Max(1, Mathf.RoundToInt(soGiay * coHep));
            for (int i = 0; i < sg; i++)
            {
                Sprite s = anhGiay[Random.Range(0, anhGiay.Length)];
                if (s == null) continue;
                StartCoroutine(CoMotGiay(tam, s, coHep));
            }
        }

        if (anhSao != null)
        {
            int ss = Mathf.Max(0, Mathf.RoundToInt(soSao * coHep));
            for (int i = 0; i < ss; i++)
            {
                // Rải đều quanh vòng + lệch ngẫu nhiên, để sao không xếp thành hình sao đều đặn.
                float goc = (360f / Mathf.Max(1, ss)) * i + Random.Range(-16f, 16f);
                StartCoroutine(CoMotSao(tam, goc, coHep));
            }
        }
    }

    private IEnumerator CoMotGiay(Vector2 tam, Sprite s, float coHep)
    {
        Image img = _pool.Lay(s, kichCoGiay * Random.Range(0.75f, 1.25f) * coHep);
        if (img == null) yield break;

        RectTransform rt = img.rectTransform;

        // Bắn lên trên nhiều hơn xuống dưới: −20°..200° cho hình vòm, giống pháo giấy thật.
        float goc = Random.Range(-20f, 200f) * Mathf.Deg2Rad;
        float toc = tocDoBan * Random.Range(0.6f, 1f) * coHep;

        Vector2 v   = new Vector2(Mathf.Cos(goc), Mathf.Sin(goc)) * toc;
        Vector2 pos = tam;

        float xoay = Random.Range(-540f, 540f);
        float tong = Mathf.Max(0.1f, thoiGianSong * Random.Range(0.85f, 1.15f));
        float t    = 0f;

        rt.anchoredPosition = pos;

        while (t < tong)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;

            // Euler đơn giản: đủ tốt cho 1 giây và không cấp phát gì.
            v.y -= trongLuc * dt;
            v   -= v * (heSoCan * dt);        // cản tỉ lệ vận tốc
            pos += v * dt;

            rt.anchoredPosition = pos;
            rt.Rotate(0f, 0f, xoay * dt);

            // Mảnh giấy lật qua lật lại: bóp trục X theo sin ⇒ trông như tờ giấy mỏng xoay
            // trong không khí, chứ không phải viên bi.
            float lat = Mathf.Cos(t * 11f);
            rt.localScale = new Vector3(Mathf.Max(0.15f, Mathf.Abs(lat)), 1f, 1f);

            // Chỉ mờ ở 35% cuối — mờ ngay từ đầu thì cú nổ mất lực.
            float k = t / tong;
            Color c = img.color;
            c.a = 1f - Mathf.InverseLerp(0.65f, 1f, k);
            img.color = c;

            yield return null;
        }

        _pool.Tra(img);
    }

    private IEnumerator CoMotSao(Vector2 tam, float gocDeg, float coHep)
    {
        Image img = _pool.Lay(anhSao, kichCoSao * coHep);
        if (img == null) yield break;

        RectTransform rt = img.rectTransform;

        float goc = gocDeg * Mathf.Deg2Rad;
        Vector2 huong = new Vector2(Mathf.Cos(goc), Mathf.Sin(goc));
        float   xa    = banKinhSao * Random.Range(0.7f, 1f) * coHep;

        float tong = Mathf.Max(0.1f, thoiGianSao);
        float t    = 0f;
        float xoay = Random.Range(-160f, 160f);

        while (t < tong)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tong);

            // Bung nhanh rồi hãm — ease-out mạnh, đúng cảm giác nổ.
            float e = 1f - (1f - k) * (1f - k) * (1f - k);
            rt.anchoredPosition = tam + huong * (xa * e);

            // Phồng lên rồi teo về 0: nửa chu kỳ sin.
            float sc = Mathf.Sin(k * Mathf.PI);
            rt.localScale = new Vector3(sc, sc, 1f);
            rt.Rotate(0f, 0f, xoay * Time.unscaledDeltaTime);

            Color c = img.color;
            c.a = 1f - Mathf.InverseLerp(0.55f, 1f, k);
            img.color = c;

            yield return null;
        }

        _pool.Tra(img);
    }

    private IEnumerator CoLoe(Vector2 tam, float coHep)
    {
        Image img = _pool.Lay(anhLoe, kichCoLoe * coHep);
        if (img == null) yield break;

        RectTransform rt = img.rectTransform;
        rt.anchoredPosition = tam;

        // Vệt loé phải nằm DƯỚI mảnh giấy và ngôi sao, nếu không nó phủ trắng hết cú nổ.
        rt.SetAsFirstSibling();

        float tong = Mathf.Max(0.05f, thoiGianLoe);
        float t    = 0f;

        while (t < tong)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tong);

            float sc = Mathf.Lerp(0.35f, 1f, 1f - (1f - k) * (1f - k));
            rt.localScale = new Vector3(sc, sc, 1f);

            Color c = img.color;
            c.a = alphaLoe * (1f - k);
            img.color = c;

            yield return null;
        }

        _pool.Tra(img);
    }
}
