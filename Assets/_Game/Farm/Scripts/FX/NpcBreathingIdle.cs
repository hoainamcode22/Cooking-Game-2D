using UnityEngine;

/// <summary>
/// [V3.2] "IDLE SỐNG ĐỘNG" CHO NPC WORLD-SPACE (khách du lịch, dân làng…).
///
/// Bản V3.1 chỉ phóng-thu scale ("thở") — với sprite TOÀN THÂN nhỏ trên map, mắt người
/// đọc thành "tấm ảnh phập phồng" (Sếp chê đúng). Người thật đứng chờ còn CỰA QUẬY:
/// đổi chân trụ, ngoái nhìn quanh, thi thoảng nhún vai. NPC của studio có sẵn 12 frame
/// (4 hướng × 3) → V3.2 dùng CHÍNH FRAME THẬT để diễn:
///
///   • Nền: thở rất nhẹ (scale ~2%) + nghiêng vi tế — chỉ là lớp lót.
///   • CỨ 2.2–4.5s làm MỘT HÀNH ĐỘNG (chọn ngẫu nhiên có trọng số):
///       40% ĐỔI CHÂN TRỤ  — nháy sang frame down_2/down_3 ~0.25s rồi về (tư thế đổi thật)
///       30% NGOÁI NHÌN    — sang frame left_1/right_1 0.6–1.0s rồi quay lại (như đang ngắm cảnh)
///       20% NHÚN VAI      — cú squash-stretch chậm 1.1s (bản V3.1)
///       10% GIẬM CHÂN ĐÔI — down_2 → down_3 → về (0.5s)
///   • Đè sprite ở LateUpdate → thắng Animator đang giữ frame đứng.
///   • CHỈ diễn khi NPC ĐỨNG YÊN; đang đi bộ → trả nguyên trạng, animation walk lo.
///
/// Frames do tool đổ sẵn vào prefab: Tools/Farm Game/Tourist Boat/Thêm hiệu ứng THỞ cho khách.
/// Không có frame → tự rơi về chế độ thở thuần (vẫn chạy, không lỗi).
/// </summary>
[DisallowMultipleComponent]
public class NpcBreathingIdle : MonoBehaviour
{
    [Header("Frame idle (tool tự đổ — down_1 là tư thế nghỉ)")]
    [Tooltip("3 frame hướng xuống: [0]=nghỉ, [1..2]=đổi chân trụ.")]
    [SerializeField] private Sprite[] downFrames;

    [Tooltip("Frame nhìn sang trái (left_1).")]
    [SerializeField] private Sprite lookLeftFrame;

    [Tooltip("Frame nhìn sang phải (right_1).")]
    [SerializeField] private Sprite lookRightFrame;

    [Header("Nhịp thở nền (rất nhẹ)")]
    [SerializeField] private float breatheCycle = 3.0f;
    [SerializeField] private float breatheAmount = 0.018f;
    [SerializeField] private float idleTiltDegrees = 0.7f;

    [Header("Hành động cựa quậy")]
    [Tooltip("Khoảng cách ngẫu nhiên giữa 2 hành động (giây).")]
    [SerializeField] private float actionEveryMin = 2.2f;
    [SerializeField] private float actionEveryMax = 4.5f;

    [Tooltip("Biên độ cú nhún vai (0.05 = ~5%).")]
    [SerializeField] private float bounceSquash = 0.05f;
    [SerializeField] private float bounceDuration = 1.1f;

    [Header("Điều kiện chạy")]
    [Tooltip("Chỉ diễn khi NPC đứng yên (dịch chuyển < ngưỡng).")]
    [SerializeField] private bool onlyWhenStanding = true;

    [Tooltip("Ngưỡng 'đứng yên' (unit world/giây; map 1 ô = 100 unit).")]
    [SerializeField] private float standingSpeedThreshold = 6f;

    [SerializeField] private bool useUnscaledTime = false;

    private enum HanhDong { Khong, DoiChan, NgoaiNhin, NhunVai, GiamChanDoi }

    // ── Runtime ──────────────────────────────────────────────────────────────
    private SpriteRenderer _sr;
    private Vector3    _baseScale;
    private Quaternion _baseRot;
    private float      _t;
    private float      _nextActionAt;
    private HanhDong   _action = HanhDong.Khong;
    private float      _actionStart;
    private float      _actionDur;
    private Sprite     _actionSprite;      // frame đang đè (null = không đè)
    private Sprite     _restSprite;        // tư thế nghỉ để trả về
    private Vector3    _lastPos;
    private float      _calmTimer;
    private bool       _wasIdling;

    private void OnEnable()
    {
        _sr        = GetComponentInChildren<SpriteRenderer>();
        _baseScale = transform.localScale;
        _baseRot   = transform.localRotation;
        _t         = Random.value * breatheCycle;              // mỗi NPC một nhịp
        _nextActionAt = _t + Random.Range(0.8f, actionEveryMax);
        _lastPos   = transform.position;
        _calmTimer = 0f;
    }

    private void OnDisable() => TraNguyenTrang();

    private void TraNguyenTrang()
    {
        transform.localScale    = _baseScale;
        transform.localRotation = _baseRot;
        _action = HanhDong.Khong;
        _actionSprite = null;
        _wasIdling = false;
    }

    private void LateUpdate()   // LateUpdate: đè SAU Animator + logic di chuyển
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        // ── Đang di chuyển? → nhường sân cho walk animation ──
        if (onlyWhenStanding)
        {
            float speed = (transform.position - _lastPos).magnitude / dt;
            _lastPos = transform.position;
            if (speed > standingSpeedThreshold)
            {
                if (_wasIdling) TraNguyenTrang();
                _calmTimer = 0f;
                return;
            }
            _calmTimer += dt;
            if (_calmTimer < 0.3f) return;   // vừa dừng — chờ êm rồi mới diễn
        }

        if (!_wasIdling)
        {
            // Vào trạng thái idle: chốt tư thế nghỉ (ưu tiên down_1, không có thì frame hiện tại)
            _restSprite = (downFrames != null && downFrames.Length > 0 && downFrames[0] != null)
                          ? downFrames[0]
                          : (_sr != null ? _sr.sprite : null);
            _wasIdling = true;
        }

        _t += dt;

        // ── Lớp lót: thở rất nhẹ + nghiêng vi tế ──
        float u = (_t / Mathf.Max(0.5f, breatheCycle)) * Mathf.PI * 2f;
        float br = Mathf.Sin(u);
        float sy = 1f + breatheAmount * br;
        float sx = 1f - breatheAmount * 0.6f * br;
        float tilt = idleTiltDegrees * Mathf.Sin(u * 0.5f + 0.8f);

        // ── Lên lịch hành động mới ──
        if (_action == HanhDong.Khong && _t >= _nextActionAt)
            BatDauHanhDong();

        // ── Diễn hành động đang chạy ──
        if (_action != HanhDong.Khong)
        {
            float p = (_t - _actionStart) / _actionDur;
            if (p >= 1f)
            {
                _action = HanhDong.Khong;
                _actionSprite = null;
                _nextActionAt = _t + Random.Range(actionEveryMin, actionEveryMax);
            }
            else if (_action == HanhDong.NhunVai)
            {
                float k = BounceScaleOffset(p);
                sy += bounceSquash * k;
                sx -= bounceSquash * 0.75f * k;
            }
            else if (_action == HanhDong.GiamChanDoi)
            {
                // nửa đầu down_2, nửa sau down_3
                _actionSprite = LayFrameDown(p < 0.5f ? 1 : 2) ?? _actionSprite;
            }
            // DoiChan / NgoaiNhin: _actionSprite đã chọn sẵn lúc bắt đầu
        }

        // ── Áp kết quả ──
        transform.localScale    = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);
        transform.localRotation = _baseRot * Quaternion.Euler(0f, 0f, tilt);

        if (_sr != null)
        {
            Sprite want = _actionSprite != null ? _actionSprite : _restSprite;
            if (want != null && _sr.sprite != want) _sr.sprite = want;
        }
    }

    private void BatDauHanhDong()
    {
        // Chọn ngẫu nhiên có trọng số; hành động cần frame mà thiếu frame → rơi về NhunVai
        float r = Random.value;
        if (r < 0.40f && LayFrameDown(Random.Range(1, 3)) != null)
        {
            _action = HanhDong.DoiChan;
            _actionDur = 0.25f;
            _actionSprite = LayFrameDown(Random.value < 0.5f ? 1 : 2);
        }
        else if (r < 0.70f && (lookLeftFrame != null || lookRightFrame != null))
        {
            _action = HanhDong.NgoaiNhin;
            _actionDur = Random.Range(0.6f, 1.0f);
            _actionSprite = (lookLeftFrame != null && (lookRightFrame == null || Random.value < 0.5f))
                            ? lookLeftFrame : lookRightFrame;
        }
        else if (r < 0.90f || LayFrameDown(1) == null)
        {
            _action = HanhDong.NhunVai;
            _actionDur = Mathf.Max(0.4f, bounceDuration);
            _actionSprite = null;   // giữ tư thế nghỉ, chỉ nhún
        }
        else
        {
            _action = HanhDong.GiamChanDoi;
            _actionDur = 0.5f;
            _actionSprite = LayFrameDown(1);
        }
        _actionStart = _t;
    }

    private Sprite LayFrameDown(int i)
    {
        return (downFrames != null && i >= 0 && i < downFrames.Length) ? downFrames[i] : null;
    }

    /// <summary>Đường cong nhún vai: lún → vươn overshoot → dư chấn tắt dần (-1..+1).</summary>
    private static float BounceScaleOffset(float p)
    {
        if (p < 0.30f)
        {
            float q = p / 0.30f;
            return -(q * q * (3f - 2f * q));
        }
        if (p < 0.60f)
        {
            float q = (p - 0.30f) / 0.30f;
            return -1f + 2.05f * (q * q * (3f - 2f * q));
        }
        float r = (p - 0.60f) / 0.40f;
        return 1.05f * (1f - r) * Mathf.Cos(r * Mathf.PI * 1.5f);
    }
}
