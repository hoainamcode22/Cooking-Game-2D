using UnityEngine;

/// <summary>
/// PLAYER ANIMATION BẰNG <c>Sprite[]</c> — KHÔNG cần Animator / .anim / .controller.
/// ═══════════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO KHÔNG DÙNG ANIMATOR:
///   • Project này đã đi theo lối "đè sprite bằng code" ở <see cref="NpcBreathingIdle"/> —
///     lý do: Animator giữ frame theo state machine của nó, script gán sprite ở Update
///     sẽ bị Animator ghi đè lại ở cuối frame. Cách thắng duy nhất mà không phải dựng
///     AnimatorController là gán ở <b>LateUpdate</b> (chạy SAU Animator).
///   • Thợ búa được tool đổ 12 frame vào mảng, không có ai dựng .controller bằng tay.
///     Một player 200 dòng rẻ hơn 2 file asset nhị phân mà DEV nào cũng conflict.
///
/// ĐẶC ĐIỂM QUAN TRỌNG:
///   • Chỉ gán <c>target.sprite</c> khi CHỈ SỐ FRAME THẬT SỰ ĐỔI (không gán 60 lần/giây —
///     đúng bài học bug <c>HouseGrowthController.UpdateVisuals()</c>, §7 CONTRACT).
///   • <see cref="SetPhaseOffset01"/> để 3 con thợ đứng cạnh nhau KHÔNG đập cùng nhịp.
///   • <c>frames</c> rỗng/null ⇒ tự <c>enabled = false</c> + cảnh báo MỘT LẦN, không spam.
///   • <see cref="pingPong"/> cho kiểu walk 1-2-3-2: chuỗi thật là 0,1,2,1,0,1,2,1…
///     (độ dài chu kỳ = 2N-2, không phải N).
///
/// [Worker]
/// </summary>
[DisallowMultipleComponent]
public class SpriteSequencePlayer : MonoBehaviour
{
    [Header("◆ ĐÍCH VẼ")]
    [Tooltip("Bỏ trống = tự GetComponent<SpriteRenderer>() trên chính GameObject này.")]
    public SpriteRenderer target;

    [Header("◆ CHUỖI FRAME")]
    [Tooltip("Các frame theo đúng thứ tự phát. Tool CharacterSheetSliceTool sẽ đổ vào đây.")]
    public Sprite[] frames;

    [Tooltip("Số frame mỗi giây. Thợ búa = 10 (1.2s/nhát), ăn mừng = 12.")]
    public float fps = 10f;

    [Tooltip("Lặp vô hạn. Tắt = dừng ở frame cuối rồi bắn OnLoopCompleted một lần.")]
    public bool loop = true;

    [Tooltip("Đi-về kiểu walk (0,1,2,1,0,1,2,1…) thay vì vòng tròn (0,1,2,0,1,2…).")]
    public bool pingPong = false;

    [Tooltip("Tự Play() ở OnEnable.")]
    public bool playOnEnable = true;

    [Tooltip("Dùng Time.unscaledDeltaTime — animation vẫn chạy khi timeScale = 0 (popup mở).")]
    public bool useUnscaledTime = false;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private float _time;                     // giây đã trôi kể từ Play(), đã nhân speed
    private float _phase01;                  // lệch pha 0..1 trong 1 chu kỳ
    private float _speedMul = 1f;
    private int   _currentFrame = -1;        // -1 = chưa gán frame nào
    private int   _lastStep = int.MinValue;  // step tuyệt đối lần đánh giá trước
    private bool  _playing;
    private bool  _daCanhBaoRong;            // cảnh báo "frames rỗng" chỉ 1 lần

    /// <summary>Chống sai số float khi phase là phân số tuần hoàn (4f/12f = 0.33333334f).</summary>
    private const float STEP_EPSILON = 0.0001f;

    // ── Event ────────────────────────────────────────────────────────────────

    /// <summary>Bắn khi VÀO một frame mới (không bắn lại nếu frame không đổi).</summary>
    public event System.Action<SpriteSequencePlayer, int> OnFrameEntered;

    /// <summary>Bắn khi hết một vòng (loop) hoặc khi chuỗi non-loop chạy xong.</summary>
    public event System.Action<SpriteSequencePlayer> OnLoopCompleted;

    // ── Property ─────────────────────────────────────────────────────────────

    /// <summary>Số frame trong mảng (0 nếu null).</summary>
    public int FrameCount => frames != null ? frames.Length : 0;

    /// <summary>Chỉ số frame đang hiển thị (-1 nếu chưa gán gì).</summary>
    public int CurrentFrame => _currentFrame;

    /// <summary>Đang phát hay không (PauseAtFrame/Stop làm false).</summary>
    public bool IsPlaying => _playing;

    /// <summary>Vị trí 0..1 trong MỘT chu kỳ (đã tính cả lệch pha).</summary>
    public float NormalizedTime
    {
        get
        {
            int seqLen = SequenceLength;
            if (seqLen <= 0) return 0f;
            return Mathf.Repeat(_time * Mathf.Max(0.01f, fps) / seqLen + _phase01, 1f);
        }
    }

    /// <summary>
    /// Số BƯỚC trong một chu kỳ: bình thường = số frame; pingPong = 2N-2
    /// (N=3 ⇒ 4 bước: 0,1,2,1). Luôn ≥ 1 để không chia cho 0.
    /// </summary>
    public int SequenceLength
    {
        get
        {
            int n = FrameCount;
            if (n <= 1) return Mathf.Max(1, n);
            return pingPong ? (2 * n - 2) : n;
        }
    }

    // ── Vòng đời ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (playOnEnable) Play();
    }

    private void LateUpdate()   // LateUpdate: gán SAU Animator (xem ghi chú đầu file)
    {
        if (frames == null || frames.Length == 0)
        {
            CanhBaoRong();
            enabled = false;
            return;
        }

        if (!_playing) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        _time += dt * _speedMul;
        Evaluate();
    }

    // ── API công khai ────────────────────────────────────────────────────────

    /// <summary>
    /// Đổi cả bộ frame + thông số rồi PHÁT LẠI TỪ ĐẦU. Dùng khi thợ chuyển
    /// sheet đập búa ⇄ sheet ăn mừng. Giữ nguyên lệch pha và speed multiplier.
    /// </summary>
    public void SetFrames(Sprite[] newFrames, float newFps, bool newLoop, bool newPingPong)
    {
        frames   = newFrames;
        fps      = Mathf.Max(0.01f, newFps);
        loop     = newLoop;
        pingPong = newPingPong;

        _daCanhBaoRong = false;
        if (frames != null && frames.Length > 0 && !enabled) enabled = true;

        _currentFrame = -1;
        _lastStep     = int.MinValue;
        _time         = 0f;
    }

    /// <summary>Phát từ đầu chu kỳ (có tính lệch pha) và gán frame đầu NGAY LẬP TỨC.</summary>
    public void Play()
    {
        if (frames == null || frames.Length == 0)
        {
            CanhBaoRong();
            enabled = false;
            _playing = false;
            return;
        }

        if (!enabled) enabled = true;

        _time         = 0f;
        _currentFrame = -1;
        _lastStep     = int.MinValue;
        _playing      = true;

        Evaluate();   // để CurrentFrame có nghĩa ngay ở t=0 (test lệch pha dựa vào đây)
    }

    /// <summary>Dừng tại chỗ, GIỮ frame hiện tại (không reset về 0).</summary>
    public void Stop()
    {
        _playing = false;
    }

    /// <summary>
    /// ĐỨNG IM ở một frame cụ thể (thợ ở giai đoạn hộp quà: celebrate frame 0 — §5.3).
    /// Chỉ số bị clamp vào [0, FrameCount-1]; mảng rỗng thì bỏ qua êm.
    /// </summary>
    public void PauseAtFrame(int index)
    {
        _playing = false;

        int n = FrameCount;
        if (n == 0)
        {
            CanhBaoRong();
            return;
        }

        int idx = Mathf.Clamp(index, 0, n - 1);
        _currentFrame = idx;
        _lastStep     = int.MinValue;

        if (target != null && frames[idx] != null) target.sprite = frames[idx];
        OnFrameEntered?.Invoke(this, idx);
    }

    /// <summary>Nhân tốc độ phát (1 = gốc). Giá trị ≤ 0 bị kẹp về 0 (đứng im).</summary>
    public void SetSpeedMultiplier(float mul)
    {
        _speedMul = Mathf.Max(0f, mul);
    }

    /// <summary>
    /// Lệch pha theo VÒNG (0..1) — để 3 con thợ cạnh nhau không đập trùng nhịp.
    /// Gọi được cả trước và trong lúc Play; đang phát thì frame cập nhật ngay.
    /// </summary>
    public void SetPhaseOffset01(float t)
    {
        _phase01 = Mathf.Repeat(t, 1f);
        if (_playing && frames != null && frames.Length > 0) Evaluate();
    }

    // ── Lõi tính frame ───────────────────────────────────────────────────────

    private void Evaluate()
    {
        int n = FrameCount;
        if (n == 0) return;

        int seqLen = SequenceLength;

        // step = số bước tuyệt đối kể từ mốc Play (đã cộng lệch pha)
        float rawSteps = _time * Mathf.Max(0.01f, fps) + _phase01 * seqLen + STEP_EPSILON;
        int step = Mathf.FloorToInt(rawSteps);
        if (step < 0) step = 0;

        bool finishedOnce = false;
        if (!loop && step > seqLen - 1)
        {
            step = seqLen - 1;
            finishedOnce = _playing;
        }

        // Đếm số vòng vừa hoàn thành (dt lớn có thể nhảy nhiều vòng một lúc)
        int loopsDone = 0;
        if (loop && _lastStep != int.MinValue && step > _lastStep)
            loopsDone = (step / seqLen) - (_lastStep / seqLen);

        int seqIdx = step % seqLen;
        int frame  = MapSeqToFrame(seqIdx, n);

        _lastStep = step;

        // THỨ TỰ EVENT QUAN TRỌNG: "hết vòng cũ" phải bắn TRƯỚC "vào frame đầu vòng mới".
        // Nếu bắn ngược lại thì bên nghe đếm frame theo vòng sẽ thấy vòng 1 có 13 frame
        // và vòng 2 có 11 — đúng con số nhưng sai quy về vòng nào (đã bắt được ở sandbox).
        for (int i = 0; i < loopsDone; i++) OnLoopCompleted?.Invoke(this);

        if (frame != _currentFrame)
        {
            _currentFrame = frame;
            if (target != null && frames[frame] != null) target.sprite = frames[frame];
            OnFrameEntered?.Invoke(this, frame);
        }

        if (finishedOnce)
        {
            _playing = false;
            OnLoopCompleted?.Invoke(this);
        }
    }

    /// <summary>Bước trong chu kỳ → chỉ số frame thật (xử lý ping-pong).</summary>
    private int MapSeqToFrame(int seqIdx, int n)
    {
        if (!pingPong || n <= 2) return seqIdx % n;
        return seqIdx < n ? seqIdx : (2 * n - 2 - seqIdx);
    }

    private void CanhBaoRong()
    {
        if (_daCanhBaoRong) return;
        _daCanhBaoRong = true;
        Debug.LogWarning($"[Worker] SpriteSequencePlayer trên '{name}' không có frame nào — " +
                         "tự tắt component. Chạy Tools > Farm Game > Cắt spritesheet nhân vật để đổ frame.");
    }

    private void OnValidate()
    {
        fps = Mathf.Max(0.01f, fps);
    }
}
