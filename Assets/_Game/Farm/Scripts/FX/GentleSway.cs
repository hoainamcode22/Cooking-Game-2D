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

    private Vector3    _basePos;
    private Quaternion _baseRot;
    private float      _phase01;

    private void OnEnable()
    {
        _basePos = transform.localPosition;
        _baseRot = transform.localRotation;
        _phase01 = autoPhase ? FxEase.StablePhase01(transform) : Mathf.Repeat(manualPhase, 1f);
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
    }
}
