using System.Collections;
using UnityEngine;

/// <summary>
/// ĐẦU BẾP TỰ DIỄN — vòng lặp xào nấu, KHÔNG input, KHÔNG di chuyển, KHÔNG Rigidbody.
///
/// Vòng diễn (BẢN RÚT GỌN — chỉ còn 2 động tác):
///     Idle (nghỉ, thời gian ngẫu nhiên)  ->  Stir (đảo, n vòng)  -> về Idle
///
/// LỊCH SỬ: trước đây có thêm Flip (xào lắc) và Finish (tắt lửa). Cả hai ĐÃ BỊ BỎ theo yêu cầu.
/// Sheet PNG vẫn còn 2 hàng art chưa dùng — muốn bật lại thì xem README_CHEF.md mục 13.
///
/// VÌ SAO Stir -> Idle giờ cần trigger ToIdle: trước kia Finish là clip KHÔNG loop nên
/// transition Finish->Idle dùng hasExitTime=1.0 (tự về). Stir là clip LOOP, không bao giờ
/// "hết" để tự thoát, nên vòng lặp phải chủ động bắn ToIdle sau khi chờ đủ số vòng.
///
/// VÌ SAO chờ theo AnimatorStateInfo.length chứ không hardcode giây:
///   nếu Edric thay PNG (số frame khác) hoặc đổi frameRate của clip, thời lượng thật đổi theo.
///   Hardcode giây sẽ khiến động tác bị cắt giữa vòng hoặc đứng chờ thừa.
///
/// VÌ SAO dùng Trigger chứ không Bool:
///   Bool phải nhớ tự tắt, quên là kẹt state. Trigger được transition "tiêu thụ" ngay khi dùng.
///   Đổi lại Trigger có bẫy: SetTrigger mà KHÔNG transition nào nhận thì nó ĐỌNG lại và
///   nổ sai lúc sau -> nên trước mỗi lần set, script RESET toàn bộ trigger khác (xem SetOnly).
/// </summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class ChefCookLoop : MonoBehaviour
{
    // Tên phải khớp Animator do ChefSetupTool sinh ra.
    public const string StateIdle = "Idle";
    public const string StateStir = "Stir";

    public const string TrigIdle = "ToIdle";
    public const string TrigStir = "ToStir";

    [Header("NGHỈ (Idle)")]
    [Tooltip("Thời gian nghỉ TỐI THIỂU giữa 2 lượt nấu (giây).")]
    [Min(0f)] public float idleMinSeconds = 2f;

    [Tooltip("Thời gian nghỉ TỐI ĐA giữa 2 lượt nấu (giây). Mỗi lượt lấy ngẫu nhiên trong khoảng min..max " +
             "để nhiều đầu bếp đặt cạnh nhau không diễn trùng khớp như robot.")]
    [Min(0f)] public float idleMaxSeconds = 4f;

    [Tooltip("BẬT = làm tròn thời gian nghỉ thành SỐ VÒNG NGUYÊN của clip Idle. " +
             "Nhờ vậy Idle luôn kết thúc đúng frame cuối, không bị cắt giữa vòng rồi nhảy sang Stir.")]
    public bool lamTronTheoVongClip = true;

    [Header("SỐ VÒNG MỖI ĐỘNG TÁC")]
    [Tooltip("Số lần lặp clip Stir (đảo bằng sạn) mỗi lượt nấu.")]
    [Min(1)] public int soVongStir = 3;

    [Header("KHỞI ĐỘNG")]
    [Tooltip("BẬT = tự diễn ngay khi vào scene. TẮT = đứng Idle chờ hệ thống khác gọi BatDauDien().")]
    public bool tuDongDien = true;

    [Tooltip("Trễ ngẫu nhiên 0..giá trị này (giây) trước lượt đầu. " +
             "Đặt > 0 khi rải nhiều đầu bếp trong map để họ lệch pha nhau.")]
    [Min(0f)] public float treKhoiDongNgauNhien = 0f;

    [Header("AN TOÀN")]
    [Tooltip("Thời gian chờ tối đa để Animator vào đúng state sau khi bắn trigger (giây). " +
             "Quá hạn = Animator thiếu state/transition -> script log cảnh báo thay vì treo im.")]
    [Min(0.1f)] public float hetHanVaoState = 2f;

    [Tooltip("Dùng khi không đọc được độ dài clip thật (clip rỗng). Chỉ là phao cứu sinh.")]
    [Min(0.05f)] public float thoiLuongDuPhong = 0.7f;

    private Animator _animator;
    private Coroutine _loop;

    private static readonly int HashIdle = Animator.StringToHash(StateIdle);
    private static readonly int HashStir = Animator.StringToHash(StateStir);

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        // Nhân vật đứng yên -> không cần root motion, tắt cho khỏi tốn và khỏi bị trôi vị trí.
        _animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        if (tuDongDien) BatDauDien();
    }

    private void OnDisable()
    {
        DungDien();
    }

    private void OnValidate()
    {
        // Giữ min <= max để Random.Range không cho kết quả lạ.
        if (idleMaxSeconds < idleMinSeconds) idleMaxSeconds = idleMinSeconds;
    }

    /// <summary>Bắt đầu (hoặc khởi động lại) vòng diễn.</summary>
    public void BatDauDien()
    {
        DungDien();
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[ChefCookLoop] '{name}' chưa gán Animator Controller (Chef.controller). Không diễn.", this);
            return;
        }
        _loop = StartCoroutine(VongDien());
    }

    /// <summary>Dừng vòng diễn, giữ nguyên frame hiện tại.</summary>
    public void DungDien()
    {
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
    }

    private IEnumerator VongDien()
    {
        // Chờ 1 frame để Animator kịp khởi tạo layer/state.
        // Nếu đọc GetCurrentAnimatorStateInfo ngay frame đầu, Animator có thể chưa tick lần nào.
        yield return null;

        if (treKhoiDongNgauNhien > 0f)
            yield return new WaitForSeconds(Random.Range(0f, treKhoiDongNgauNhien));

        while (true)
        {
            // Idle: chờ theo GIÂY do designer đặt (đây là thông số thiết kế, không phải hardcode),
            // nhưng vẫn quy về số vòng nguyên của clip thật nếu lamTronTheoVongClip = true.
            yield return DienState(TrigIdle, HashIdle, StateIdle, 0, Random.Range(idleMinSeconds, idleMaxSeconds));
            yield return DienState(TrigStir, HashStir, StateStir, Mathf.Max(1, soVongStir), -1f);
            // Hết vòng lặp -> quay lên đầu, bắn ToIdle -> transition Stir->Idle nhận trigger đó.
            // KHÔNG có state nào tự thoát bằng hasExitTime nữa (Finish đã bị bỏ), nên vòng
            // Stir -> Idle hoàn toàn do coroutine điều khiển.
        }
    }

    /// <summary>
    /// Bắn trigger, ĐỢI Animator thật sự vào state, rồi chờ đúng thời lượng clip thật.
    /// </summary>
    /// <param name="soVong">Số lần lặp clip. Bỏ qua nếu overrideSeconds >= 0.</param>
    /// <param name="overrideSeconds">&gt;= 0 = chờ theo giây này thay vì theo số vòng.</param>
    private IEnumerator DienState(string trigger, int stateHash, string stateName, int soVong, float overrideSeconds)
    {
        SetOnly(trigger);

        // Transition mất vài frame -> phải đợi, nếu đọc length ngay sẽ ra length của state CŨ.
        float doi = 0f;
        while (doi < hetHanVaoState)
        {
            if (!_animator.IsInTransition(0) &&
                _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash) break;
            doi += Time.deltaTime;
            yield return null;
        }
        if (doi >= hetHanVaoState)
        {
            Debug.LogWarning($"[ChefCookLoop] '{name}': Animator không vào được state '{stateName}' " +
                             $"sau {hetHanVaoState}s. Thiếu state hoặc thiếu transition '{trigger}'?", this);
            yield break;
        }

        float doDaiClip = _animator.GetCurrentAnimatorStateInfo(0).length;
        if (doDaiClip <= 0.01f || float.IsInfinity(doDaiClip)) doDaiClip = thoiLuongDuPhong;

        float tong;
        if (overrideSeconds >= 0f)
        {
            tong = overrideSeconds;
            if (lamTronTheoVongClip)
                tong = Mathf.Max(1f, Mathf.Round(overrideSeconds / doDaiClip)) * doDaiClip;
        }
        else
        {
            tong = doDaiClip * soVong;
        }

        // Animator.speed nhân vào tốc độ phát -> phải chia ra, nếu không chờ sai khi ai đó tăng/giảm speed.
        float speed = Mathf.Max(0.01f, _animator.speed);
        yield return new WaitForSeconds(tong / speed);
    }

    /// <summary>
    /// Reset MỌI trigger rồi chỉ set 1 cái.
    /// VÌ SAO: trigger không được transition nào tiêu thụ sẽ đọng lại và cắt ngang động tác sau.
    /// Ví dụ ToIdle đọng lại từ lượt trước sẽ giết clip Stir ngay frame đầu.
    /// </summary>
    private void SetOnly(string trigger)
    {
        _animator.ResetTrigger(TrigIdle);
        _animator.ResetTrigger(TrigStir);
        _animator.SetTrigger(trigger);
    }
}
