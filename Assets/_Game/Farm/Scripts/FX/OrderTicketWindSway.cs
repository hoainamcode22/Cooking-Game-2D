using UnityEngine;

/// <summary>
/// PHIẾU ĐƠN HÀNG ĐUNG ĐƯA TRONG GIÓ — dành riêng cho bảng đơn (OrderBoardPopupUI).
/// ═══════════════════════════════════════════════════════════════════════════════
///
/// Cùng tinh thần với <see cref="GentleSway"/> (xoay quanh một điểm ghim, lệch pha riêng
/// từng vật, biên độ rất nhỏ) nhưng viết lại cho UGUI vì ba khác biệt bắt buộc:
///
///   1. UI dùng <c>anchoredPosition</c> của RectTransform, không phải localPosition.
///   2. Bảng đơn chạy khi game đang tạm dừng ⇒ phải bám <c>Time.unscaledTime</c>,
///      không phải Time.time (Time.time đứng yên lúc timeScale = 0, sway sẽ chết cứng).
///   3. Phiếu bị NGƯỜI KHÁC dời chỗ: OrderBoardPopupUI.RefreshAll() kéo phiếu về
///      HomePosition, còn ReflowRoutine() trượt phiếu giữa hai ô khi có đơn biến mất.
///      Xem mục 🔴 bên dưới.
///
/// 🔴 CƠ CHẾ "NHƯỜNG QUYỀN" — phần quan trọng nhất của file này.
/// Nếu cứ nhớ một mốc lúc OnEnable rồi mãi mãi cộng dao động vào đó thì mỗi lần popup dời
/// phiếu sang ô mới, sway sẽ lập tức kéo phiếu ngược về ô cũ — nhìn ra là "phiếu giật".
/// Cách xử lý: mỗi khung hình ghi nhớ CHÍNH XÁC giá trị mình vừa ghi xuống. Sang khung sau,
/// nếu giá trị đang có KHÁC cái mình ghi ⇒ có người khác vừa can thiệp ⇒ lấy luôn giá trị
/// đó làm mốc mới. Popup luôn thắng, sway chỉ đắp thêm dao động lên trên.
///
/// 🔴 GHIM Ở MÉP TRÊN. Phiếu treo trên bảng như tờ giấy đóng đinh phía trên, nên tâm xoay
/// phải nằm ở giữa MÉP TRÊN chứ không phải tâm phiếu. Transform không cho đổi pivot nên
/// phải bù bằng vị trí — đúng thủ thuật bù pivot của GentleSway, chỉ khác là điểm ghim
/// được suy ra từ <c>rect</c> + <c>pivot</c> của RectTransform thay vì gõ tay.
///
/// LƯU Ý: chạy ở LateUpdate để luôn ghi SAU khi popup (Update / coroutine) đã ghi xong
/// trong cùng khung hình — nếu chạy ở Update thì hai bên tranh nhau và phiếu rung.
/// Không đụng tới localScale vì popup đang dùng scale cho hiệu ứng dồn lưới.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class OrderTicketWindSway : MonoBehaviour
{
    [Header("◆ BIÊN ĐỘ (rất nhẹ — đây là polish, không phải hiệu ứng chính)")]

    [Tooltip("Góc nghiêng tối đa quanh trục Z, ĐỘ. 1.5° là vừa đủ thấy. Quá 4° thì " +
             "chữ trong phiếu bắt đầu khó đọc và bảng trông như đang rung lắc.")]
    [SerializeField] private float swayDegrees = 1.5f;

    [Tooltip("Trôi ngang tối đa, đơn vị px của Canvas. Rất nhỏ để không phá cảm giác lưới.")]
    [SerializeField] private float driftPixels = 2f;

    [Tooltip("Nhấp nhô lên xuống tối đa, px. Cố ý CHẬM hơn nhịp xoay (xem 'Bob Period').")]
    [SerializeField] private float bobPixels = 1.5f;

    [Header("◆ NHỊP")]

    [Tooltip("Thời gian một nhịp nghiêng qua-về đầy đủ, giây.")]
    [SerializeField] private float swayPeriod = 3.2f;

    [Tooltip("Thời gian một nhịp trôi ngang đầy đủ, giây.")]
    [SerializeField] private float driftPeriod = 4.1f;

    [Tooltip("Thời gian một nhịp lên-xuống đầy đủ, giây. Để LỆCH và CHẬM hơn nhịp xoay: " +
             "ba nhịp không chia hết cho nhau thì quỹ đạo không bao giờ lặp lại y hệt, " +
             "mắt không bắt được chu kỳ và chuyển động trông như gió thật.")]
    [SerializeField] private float bobPeriod = 5.3f;

    [Header("◆ LỆCH PHA RIÊNG TỪNG PHIẾU")]

    [Tooltip("BẬT = tự sinh lệch pha ổn định từ vị trí + InstanceID (FxEase.StablePhase01).\n" +
             "VÌ SAO CẦN: 9 phiếu đung đưa ĐỒNG LOẠT thì trông như cả tấm bảng bị xoay, " +
             "không ra gió. Dùng hàm ổn định (không Random) để mỗi lần mở bảng ra như nhau.")]
    [SerializeField] private bool autoPhase = true;

    [Tooltip("Lệch pha tay theo VÒNG (0..1). Chỉ dùng khi tắt 'Auto Phase'.")]
    [SerializeField] private float manualPhase = 0f;

    private RectTransform _rect;

    private Vector2    _basePos;        // mốc vị trí do popup sở hữu
    private Quaternion _baseRot;        // mốc góc xoay lúc bật
    private Vector2    _lastWritten;    // đúng giá trị sway ghi xuống khung trước
    private bool       _hasWritten;
    private float      _phase01;

    private void Awake()
    {
        _rect = (RectTransform)transform;
    }

    private void OnEnable()
    {
        if (_rect == null) _rect = (RectTransform)transform;

        _basePos     = _rect.anchoredPosition;
        _baseRot     = _rect.localRotation;
        _lastWritten = _basePos;
        _hasWritten  = false;
        _phase01     = autoPhase ? FxEase.StablePhase01(transform) : Mathf.Repeat(manualPhase, 1f);
    }

    private void OnDisable()
    {
        // Trả phiếu về đúng mốc gốc. Không làm việc này thì lần bật lại sẽ chụp một mốc
        // ĐANG NGHIÊNG làm mốc mới và phiếu trôi xa dần sau mỗi lần đóng/mở bảng.
        if (_rect == null) return;
        _rect.anchoredPosition = _basePos;
        _rect.localRotation    = _baseRot;
        _hasWritten            = false;
    }

    private void LateUpdate()
    {
        if (_rect == null) return;

        // ── NHƯỜNG QUYỀN: popup vừa dời phiếu thì lấy chỗ mới làm mốc (xem 🔴 đầu file).
        Vector2 current = _rect.anchoredPosition;
        if (!_hasWritten || (current - _lastWritten).sqrMagnitude > 0.0001f)
            _basePos = current;

        // Bảng đơn mở lúc game tạm dừng ⇒ bắt buộc dùng unscaledTime.
        float t = Time.unscaledTime;

        float wSway  = Mathf.Sin((t / Mathf.Max(0.05f, swayPeriod)  + _phase01)          * Mathf.PI * 2f);
        float wDrift = Mathf.Sin((t / Mathf.Max(0.05f, driftPeriod) + _phase01 + 0.25f)  * Mathf.PI * 2f);
        float wBob   = Mathf.Sin((t / Mathf.Max(0.05f, bobPeriod)   + _phase01 + 0.5f)   * Mathf.PI * 2f);

        Quaternion spin = Quaternion.Euler(0f, 0f, wSway * swayDegrees);
        _rect.localRotation = _baseRot * spin;

        // ── BÙ PIVOT VỀ MÉP TRÊN: điểm ghim (0.5, 1) trong toạ độ chuẩn hoá của rect,
        // quy ra offset LOCAL so với pivot hiện tại của phiếu.
        Rect    r  = _rect.rect;
        Vector2 pv = new Vector2((0.5f - _rect.pivot.x) * r.width,
                                 (1f   - _rect.pivot.y) * r.height);

        // Phép xoay đẩy điểm ghim tới spin * pv → dịch ngược lại đúng phần chênh đó thì
        // mép trên đứng yên tuyệt đối và chỉ phần đuôi phiếu đưa qua đưa lại.
        Vector3 pv3   = new Vector3(pv.x, pv.y, 0f);
        Vector3 fixup = pv3 - spin * pv3;

        Vector2 offset = new Vector2(fixup.x + wDrift * driftPixels,
                                     fixup.y + wBob   * bobPixels);

        _lastWritten           = _basePos + offset;
        _rect.anchoredPosition = _lastWritten;
        _hasWritten            = true;
    }

    private void OnValidate()
    {
        swayDegrees = Mathf.Clamp(swayDegrees, 0f, 15f);
        driftPixels = Mathf.Max(0f, driftPixels);
        bobPixels   = Mathf.Max(0f, bobPixels);
        swayPeriod  = Mathf.Max(0.05f, swayPeriod);
        driftPeriod = Mathf.Max(0.05f, driftPeriod);
        bobPeriod   = Mathf.Max(0.05f, bobPeriod);
    }
}
