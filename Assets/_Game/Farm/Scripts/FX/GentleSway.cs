using UnityEngine;

/// <summary>
/// LAY NHẸ QUANH GỐC — cây cối, biển hiệu, cột đèn.
/// ═══════════════════════════════════════════════
///
/// Thông số ĐO TỪ VIDEO (PHAN_TICH_TOWNSHIP_ANIMATION.md §4.6):
///     rotation.z = sin(time + lệchPhaRiêng) · 2°,  PIVOT Ở GỐC CÂY
///
/// 🔴 PIVOT Ở GỐC LÀ ĐIỂM QUAN TRỌNG NHẤT. Xoay quanh tâm sprite thì gốc cây trượt qua
/// trượt lại trên mặt đất — mắt đọc ra "cây đang lắc lư trong không khí". Xoay quanh gốc
/// thì thân cây đứng yên và chỉ tán lá nghiêng — đúng cảm giác gió.
///
/// Unity KHÔNG cho đổi pivot của Transform, nên phải bù lại bằng vị trí: quay xong thì
/// dịch object đúng phần mà điểm pivot bị đẩy đi. Cùng thủ thuật `cloneRotationCompensation`
/// mà PlacementManager dùng để xoay Ghost quanh tâm visual.
///
/// ⚠ ĐÃ CÓ `EnvironmentSway` TRONG DỰ ÁN (34 object đang gắn) và nó làm NHIỀU hơn:
/// xoay + dịch ngang + phình scale. GentleSway là bản ĐÚNG-THÔNG-SỐ tối giản, dùng cho
/// vật mới và cho trường hợp cần pivot ở gốc (EnvironmentSway xoay quanh gốc transform,
/// không có phần bù pivot). KHÔNG sửa EnvironmentSway vì nó đang chạy đúng trên scene.
/// </summary>
[DisallowMultipleComponent]
public class GentleSway : MonoBehaviour
{
    [Header("◆ BIÊN ĐỘ & NHỊP (đo từ video)")]

    [Tooltip("Góc nghiêng tối đa, ĐỘ. Township ≈ 2°. Quá 5° là thành 'bị gió bão'.")]
    [SerializeField] private float swayDegrees = 2f;

    [Tooltip("Thời gian một nhịp nghiêng qua-về đầy đủ, giây. 3s cho cảm giác gió hiu hiu.")]
    [SerializeField] private float period = 3f;

    [Header("◆ PIVOT")]

    [Tooltip("Vị trí GỐC CÂY so với gốc transform, đơn vị LOCAL.\n" +
             "• Art pivot đã ở đáy sprite (chuẩn của dự án này) → để (0,0), khỏi bù gì.\n" +
             "• Art pivot ở giữa sprite → đặt (0, −nửaChiềuCao), vd (0, −1.2).\n" +
             "Xem ghi chú 🔴 đầu file để hiểu vì sao con số này quan trọng.")]
    [SerializeField] private Vector2 pivotOffset = Vector2.zero;

    [Header("◆ LỆCH PHA RIÊNG")]

    [Tooltip("BẬT = tự sinh lệch pha từ vị trí + InstanceID.\n" +
             "VÌ SAO CẦN: cả rừng cây nghiêng ĐỒNG LOẠT thì trông như một tấm ảnh bị xoay. " +
             "Dùng vị trí (không Random) để mỗi lần Play ra kết quả giống nhau.")]
    [SerializeField] private bool autoPhase = true;

    [Tooltip("Lệch pha tay theo VÒNG (0..1). Chỉ dùng khi tắt 'Auto Phase'.")]
    [SerializeField] private float manualPhase = 0f;

    [Header("◆ HIỆU NĂNG (F4.9 — 2026-09-17)")]

    [Tooltip("BẬT = bỏ qua Update khi Renderer của vật KHÔNG nằm trong khung hình.\n" +
             "Đo được: 34 instance GentleSway + 26 instance sway cây trồng ⇒ ~60 lượt Update/frame " +
             "ghi transform, phần lớn cho vật đang ở ngoài màn hình. Vật khuất thì đứng im ở góc " +
             "nghiêng hiện tại — không ai thấy, và khi hiện lại nó tính theo Time.time nên KHÔNG NHẢY.\n" +
             "TẮT nếu vật không có Renderer nào (script sẽ tự phát hiện và bỏ qua cổng này).")]
    [SerializeField] private bool chiChayKhiThayDuoc = true;

    [Tooltip("Chạy 1 lần mỗi N frame. 1 = mỗi frame (như cũ). 2 = 30Hz ở 60fps — mắt không đọc ra " +
             "vì đây là dao động sin chậm 3s/nhịp. Mỗi instance có offset riêng nên chúng KHÔNG " +
             "cùng tick vào một frame.")]
    [Range(1, 4)]
    [SerializeField] private int buocFrame = 2;

    private Vector3    _basePos;
    private Quaternion _baseRot;
    private float      _phase01;

    // ── [PERF F4.9] ──────────────────────────────────────────────────────────────
    // `isVisible` là một bool do hệ render ghi, đọc rất rẻ. Cache Renderer 1 lần thay vì
    // GetComponentInChildren mỗi frame. NULL = vật không có Renderer nào ⇒ không có bounds
    // để cull ⇒ bỏ hẳn cổng hiển thị (giống cách AnimatorCullingOptimizer bỏ qua Animator
    // không Renderer), chứ không giả vờ là có tác dụng.
    private Renderer _renderer;
    private int      _offsetFrame;   // 0..buocFrame-1, rải các instance ra nhiều frame khác nhau.

    private void OnEnable()
    {
        _basePos = transform.localPosition;
        _baseRot = transform.localRotation;
        _phase01 = autoPhase ? FxEase.StablePhase01(transform) : Mathf.Repeat(manualPhase, 1f);

        // [PERF F4.9] cache 1 lần. `true` = tính cả Renderer đang tắt, vì nó có thể được bật lên sau.
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>(true);

        // Rải instance ra các frame khác nhau: dùng InstanceID (ổn định trong 1 phiên chạy)
        // thay vì Random để hai lần Play cho kết quả giống nhau.
        int b = Mathf.Clamp(buocFrame, 1, 4);
        _offsetFrame = (int)((uint)GetInstanceID() % (uint)b);
    }

    private void OnDisable()
    {
        // Trả về mốc gốc, nếu không lần bật lại sẽ chụp một mốc đã nghiêng và cây lệch dần.
        transform.localPosition = _basePos;
        transform.localRotation = _baseRot;
    }

    // Update (không phải coroutine) vì đây là biến đổi THUẦN THEO Time.time, không có pha,
    // không có điểm bắt đầu/kết thúc — coroutine ở đây chỉ thêm một lớp không cần thiết.
    // (Đúng cách EnvironmentSway đang làm.)
    private void Update()
    {
        // [PERF F4.9] ① Ngoài khung hình ⇒ không ai nhìn thấy, bỏ hẳn lượt ghi transform.
        // Renderer == null ⇒ không có bounds ⇒ Unity coi luôn hiện ⇒ cổng này vô nghĩa, bỏ qua.
        if (chiChayKhiThayDuoc && _renderer != null && !_renderer.isVisible)
            return;

        // [PERF F4.9] ② Giảm nhịp. Phép sway là HÀM THUẦN của Time.time (không cộng dồn),
        // nên bỏ frame KHÔNG làm lệch pha hay trôi vị trí — chỉ thưa mẫu ra.
        int buoc = Mathf.Clamp(buocFrame, 1, 4);
        if (buoc > 1 && (Time.frameCount % buoc) != _offsetFrame)
            return;

        float p    = Mathf.Max(0.05f, period);
        float wave = Mathf.Sin((Time.time / p + _phase01) * Mathf.PI * 2f);

        Quaternion spin = Quaternion.Euler(0f, 0f, wave * swayDegrees);
        transform.localRotation = _baseRot * spin;

        // BÙ PIVOT: điểm pivotOffset bị phép xoay đẩy tới `spin * pivotOffset`.
        // Dịch object ngược lại đúng phần chênh đó → pivotOffset đứng yên tuyệt đối.
        Vector3 pv = new Vector3(pivotOffset.x, pivotOffset.y, 0f);
        transform.localPosition = _basePos + (pv - spin * pv);
    }

    private void OnValidate()
    {
        period      = Mathf.Max(0.05f, period);
        swayDegrees = Mathf.Clamp(swayDegrees, 0f, 45f);
        buocFrame   = Mathf.Clamp(buocFrame, 1, 4);
    }
}
