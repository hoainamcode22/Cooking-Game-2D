using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một ô icon "vừa mở khoá" trong popup lên cấp (phong cách Township):
/// khung tròn viền kem + icon bên trong + nhãn NEW đỏ nghiêng ở góc dưới-trái.
///
/// Prefab do <c>LevelUpPopupTownshipTool</c> sinh ra. Dùng lại được cho bất kỳ
/// danh sách phần thưởng / vật phẩm mở khoá nào.
/// </summary>
public class UnlockSlotUI : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private Image           iconImage;
    [SerializeField] private Image           ringImage;
    [SerializeField] private GameObject      newTagRoot;
    [SerializeField] private TextMeshProUGUI captionText;

    [Header("Animation xuất hiện")]
    [Tooltip("Độ trễ giữa các ô khi hiện lần lượt (giây).")]
    public float staggerDelay = 0.06f;
    [Tooltip("Thời gian phóng to vào chỗ (giây).")]
    public float popDuration  = 0.32f;

    private RectTransform _rt;
    private Coroutine     _popRoutine;

    // [R2 GỘP] Scale "nền" do khu phần thưởng gộp quyết định (co ô khi đông cell).
    // Animation PopRoutine NHÂN với scale này thay vì ghi đè cứng Vector3.one —
    // nếu không, ô mở khoá sẽ phình về 190px giữa lúc cả dải đã co còn 0.8x.
    private Vector3 _baseScale = Vector3.one;

    private void Awake() => _rt = transform as RectTransform;

    /// <summary>
    /// [R2 GỘP] LevelUpPopupUI gọi khi xếp khu phần thưởng gộp: đặt scale chuẩn của ô.
    /// Đang KHÔNG chạy animation thì áp ngay; đang pop thì PopRoutine tự nhân theo.
    /// </summary>
    public void SetBaseScale(float k)
    {
        _baseScale = new Vector3(k, k, 1f);
        if (_rt == null) _rt = transform as RectTransform;
        if (_popRoutine == null && _rt != null) _rt.localScale = _baseScale;
    }

    /// <summary>Sprite icon đang gắn (null = ô chưa có icon). Dùng cho tool kiểm tra / báo cáo QA.</summary>
    public Sprite CurrentIcon => iconImage != null ? iconImage.sprite : null;

    /// <summary>TRUE khi ô thật sự đang vẽ được một icon (có sprite VÀ Image đang bật).</summary>
    public bool HasIcon => iconImage != null && iconImage.sprite != null && iconImage.enabled;

    /// <summary>Gán nội dung cho ô.</summary>
    /// <param name="icon">Sprite hiển thị. Null → ẩn icon, chỉ còn khung.</param>
    /// <param name="showNewTag">Hiện nhãn NEW đỏ.</param>
    /// <param name="caption">Chữ dưới ô. Để trống thì ẩn.</param>
    public void Setup(Sprite icon, bool showNewTag = true, string caption = "")
    {
        // ── CHỐT: trả kích thước về 1 trước mọi thứ ──────────────────────────
        // Nếu ô bị SetActive(false) ĐANG GIỮA animation PopRoutine, Unity huỷ coroutine
        // ngay tại chỗ → localScale kẹt ở ~0. Lần popup sau bật lại ô đó, nếu PlayPop
        // không chạy được thì ô VÔ HÌNH dù đã bật. Reset ở đây bảo đảm ô luôn nhìn thấy.
        if (_rt == null) _rt = transform as RectTransform;
        if (_popRoutine != null) { StopCoroutine(_popRoutine); _popRoutine = null; }
        if (_rt != null) _rt.localScale = _baseScale;   // [R2 GỘP] tôn trọng scale khu gộp

        if (ringImage != null)
            ringImage.enabled = true;   // khung luôn hiện, kể cả khi chưa có icon

        if (iconImage != null)
        {
            iconImage.sprite  = icon;
            iconImage.enabled = icon != null;
            // preserveAspect tránh icon chữ nhật bị bóp méo trong khung tròn
            iconImage.preserveAspect = true;
            // Trả màu về trắng: nếu ai đó tint ô này ở lần dùng trước (hoặc trong prefab)
            // thì icon thật sẽ bị nhuộm sai màu / trong suốt.
            iconImage.color = Color.white;
        }

        if (newTagRoot != null)
            newTagRoot.SetActive(showNewTag);

        if (captionText != null)
        {
            bool has = !string.IsNullOrWhiteSpace(caption);
            captionText.gameObject.SetActive(has);
            if (has) captionText.text = caption;
        }
    }

    /// <summary>Cho ô "bật" ra với hiệu ứng nảy, trễ theo thứ tự index.</summary>
    public void PlayPop(int index)
    {
        if (_rt == null) _rt = transform as RectTransform;

        // Object đang tắt → Unity KHÔNG cho StartCoroutine. Bỏ hiệu ứng nhưng
        // vẫn phải để scale = 1, nếu không ô sẽ vô hình khi được bật.
        if (!isActiveAndEnabled)
        {
            if (_rt != null) _rt.localScale = _baseScale;   // [R2 GỘP]
            return;
        }

        if (_popRoutine != null) StopCoroutine(_popRoutine);
        _popRoutine = StartCoroutine(PopRoutine(index * staggerDelay));
    }

    private System.Collections.IEnumerator PopRoutine(float delay)
    {
        _rt.localScale = Vector3.zero;

        float t = 0f;
        while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }

        t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popDuration);
            _rt.localScale = _baseScale * EaseOutBack(k);   // [R2 GỘP] nhân với scale khu gộp
            yield return null;
        }
        _rt.localScale = _baseScale;                        // [R2 GỘP]
        _popRoutine    = null;
    }

    /// <summary>Nảy quá đà rồi ổn định — tạo cảm giác "bung ra" đã tay.</summary>
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float m = x - 1f;
        return 1f + c3 * m * m * m + c1 * m * m;
    }

#if UNITY_EDITOR
    /// <summary>Tool dựng hierarchy gọi để gán tham chiếu lúc sinh prefab.</summary>
    public void EditorBind(Image icon, Image ring, GameObject tag, TextMeshProUGUI caption)
    {
        iconImage   = icon;
        ringImage   = ring;
        newTagRoot  = tag;
        captionText = caption;
    }
#endif
}
