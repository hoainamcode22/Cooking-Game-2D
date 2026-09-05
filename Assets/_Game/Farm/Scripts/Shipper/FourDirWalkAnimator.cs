using UnityEngine;

/// <summary>
/// ANIMATION ĐI BỘ 4 HƯỚNG bằng <c>Sprite[]</c> — dùng cho cô gái giỏ hoa (Task 1).
///
/// Component này CHỈ LÀM MỘT VIỆC: <b>chọn bộ frame theo hướng</b> rồi đưa cho
/// <see cref="SpriteSequencePlayer"/> (DEV-B) phát. Không tự đếm thời gian, không tự
/// gán sprite — mọi việc phát hình là của player, tránh 2 hệ cùng ghi
/// <c>SpriteRenderer.sprite</c> rồi đá nhau.
///
/// LAYOUT SHEET (CONTRACT §5.1) — <c>flowergirl_walk_spritesheet.png</c> 848×1264,
/// grid 3 cột × 4 hàng, index frame = <c>row*3 + col</c>:
/// <code>
///   hàng 0 → frame 0,1,2   = DOWN  (mặt trước)
///   hàng 1 → frame 3,4,5   = LEFT
///   hàng 2 → frame 6,7,8   = RIGHT
///   hàng 3 → frame 9,10,11 = UP    (lưng)
/// </code>
/// Đi bộ = ping-pong <c>1-2-3-2</c> @ 8 fps, loop. Đứng im = giữ frame GIỮA (index 1).
/// Ping-pong của player với 3 frame cho ra chuỗi 0-1-2-1 (SequenceLength = 2*3-2 = 4),
/// đúng nhịp "1-2-3-2" mà §5.1 mô tả (đánh số từ 1).
///
/// ⚠ BẪY ĐÃ PHÒNG: <see cref="SpriteSequencePlayer.SetFrames"/> RESET <c>_time</c> về 0.
/// Gọi nó mỗi frame ⇒ nhân vật đứng mãi ở frame 0 = "giật chân". Vì vậy lớp này giữ
/// <c>_hasApplied</c> + hướng/trạng thái cũ và CHỈ áp lại khi hướng hoặc
/// trạng thái đi/đứng THẬT SỰ đổi.
///
/// ⚠ <c>playOnEnable</c> của player bị TẮT chủ động: nếu để bật, mỗi lần object enable
/// player tự <c>Play()</c> làm cô gái "chạy chân tại chỗ" trong lúc đang đứng chờ.
/// </summary>
[DisallowMultipleComponent]
public class FourDirWalkAnimator : MonoBehaviour
{
    /// <summary>4 hướng chính. Giá trị số = chỉ số hàng trên sheet (§5.1).</summary>
    public enum Facing
    {
        Down  = 0,
        Left  = 1,
        Right = 2,
        Up    = 3,
    }

    /// <summary>Số frame mỗi hướng theo §5.1.</summary>
    public const int FramesPerDirection = 3;

    /// <summary>Tổng số frame của sheet phẳng mà <see cref="SetupFromFlat"/> mong đợi.</summary>
    public const int FlatFrameCount = 12;

    /// <summary>Frame ĐỨNG IM (frame giữa của hàng) — §5.1.</summary>
    private const int IdleFrameIndex = 1;

    [Header("Đích vẽ (để trống = tự tìm SpriteRenderer trên chính object)")]
    [SerializeField] private SpriteRenderer spriteTarget;

    [Header("Tốc độ đi bộ (§5.1 chốt 8 fps)")]
    [SerializeField] private float walkFps = 8f;

    // ─── Runtime ────────────────────────────────────────────────────────

    // _sets[(int)Facing] = bộ frame của hướng đó.
    private readonly Sprite[][] _sets = new Sprite[4][];

    private SpriteSequencePlayer _player;

    private Facing _facing = Facing.Down;
    private bool   _walking;

    private bool _hasApplied;

    private bool _ready;
    private bool _warnedMissingFrames;

    /// <summary>Hướng đang quay mặt.</summary>
    public Facing CurrentFacing => _facing;

    /// <summary>Đang phát animation đi bộ hay đứng im.</summary>
    public bool IsWalking => _walking;

    /// <summary>Đã nhận đủ frame và có player dùng được chưa.</summary>
    public bool IsReady => _ready;

    /// <summary>SpriteRenderer đang được vẽ vào (có thể null nếu chưa Setup).</summary>
    public SpriteRenderer Target => spriteTarget;

    /// <summary>Sprite của frame ĐỨNG IM hướng hiện tại — dùng để đo bounds tính scale.</summary>
    public Sprite RepresentativeSprite
    {
        get
        {
            Sprite[] set = _sets[(int)_facing];
            if (set != null && set.Length > 0)
            {
                int idx = Mathf.Clamp(IdleFrameIndex, 0, set.Length - 1);
                if (set[idx] != null) return set[idx];
                for (int i = 0; i < set.Length; i++)
                    if (set[i] != null) return set[i];
            }
            for (int d = 0; d < _sets.Length; d++)
            {
                Sprite[] s = _sets[d];
                if (s == null) continue;
                for (int i = 0; i < s.Length; i++)
                    if (s[i] != null) return s[i];
            }
            return null;
        }
    }

    // ─── Setup ──────────────────────────────────────────────────────────

    /// <summary>
    /// Nhận 4 bộ frame rời. Bộ nào null/rỗng ⇒ cảnh báo ĐÚNG 1 LẦN rồi
    /// <c>enabled = false</c> (không spam Console mỗi frame như bug hệ cũ).
    /// </summary>
    public void Setup(Sprite[] down, Sprite[] left, Sprite[] right, Sprite[] up,
                      float newWalkFps, SpriteRenderer sr)
    {
        if (sr != null) spriteTarget = sr;
        if (spriteTarget == null) spriteTarget = GetComponent<SpriteRenderer>();
        if (newWalkFps > 0.01f) walkFps = newWalkFps;

        _sets[(int)Facing.Down]  = down;
        _sets[(int)Facing.Left]  = left;
        _sets[(int)Facing.Right] = right;
        _sets[(int)Facing.Up]    = up;

        if (!ValidateSets() || spriteTarget == null)
        {
            WarnMissingOnce(spriteTarget == null
                ? "không tìm được SpriteRenderer để vẽ"
                : "thiếu frame ở ít nhất 1 trong 4 hướng");
            _ready = false;
            enabled = false;
            return;
        }

        EnsurePlayer();
        _ready = _player != null;
        if (!_ready)
        {
            WarnMissingOnce("không tạo được SpriteSequencePlayer");
            enabled = false;
            return;
        }

        enabled = true;
        _hasApplied = false;
        _walking    = false;
        Apply();
    }

    /// <summary>
    /// Nhận sheet PHẲNG 12 frame theo đúng thứ tự §5.1 (0-2 down · 3-5 left ·
    /// 6-8 right · 9-11 up). Đây là dạng DEV-D nhồi vào <see cref="ShipperConfig"/>.
    /// </summary>
    public void SetupFromFlat(Sprite[] twelveFrames, float newWalkFps, SpriteRenderer sr)
    {
        if (twelveFrames == null || twelveFrames.Length < FlatFrameCount)
        {
            if (sr != null) spriteTarget = sr;
            WarnMissingOnce($"sheet phẳng cần đúng {FlatFrameCount} frame, nhận được " +
                            (twelveFrames == null ? "null" : twelveFrames.Length.ToString()));
            _ready  = false;
            enabled = false;
            return;
        }

        var down  = new Sprite[FramesPerDirection];
        var left  = new Sprite[FramesPerDirection];
        var right = new Sprite[FramesPerDirection];
        var up    = new Sprite[FramesPerDirection];

        for (int i = 0; i < FramesPerDirection; i++)
        {
            down[i]  = twelveFrames[i];
            left[i]  = twelveFrames[FramesPerDirection + i];
            right[i] = twelveFrames[FramesPerDirection * 2 + i];
            up[i]    = twelveFrames[FramesPerDirection * 3 + i];
        }

        Setup(down, left, right, up, newWalkFps, sr);
    }

    // ─── Điều khiển hướng ───────────────────────────────────────────────

    /// <summary>
    /// SNAP vector hướng về 4 hướng chính — TRỤC NÀO LỚN HƠN THẮNG, bằng nhau
    /// (chéo 45°) thì trục X thắng (Left/Right). Vector gần 0 ⇒ GIỮ hướng cũ,
    /// nhờ vậy lúc dừng lại nhân vật không tự quay về Down.
    /// Pattern copy từ <c>TouristAgent.FaceCardinal()</c>.
    /// </summary>
    public void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        Facing f;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            f = dir.x >= 0f ? Facing.Right : Facing.Left;
        else
            f = dir.y >= 0f ? Facing.Up : Facing.Down;

        FaceFacing(f);
    }

    /// <summary>Quay mặt về một hướng cụ thể. Trùng hướng cũ ⇒ KHÔNG áp lại (chống giật chân).</summary>
    public void FaceFacing(Facing f)
    {
        if (_hasApplied && f == _facing) return;
        _facing = f;
        Apply();
    }

    /// <summary>
    /// Bật/tắt animation đi bộ. <c>false</c> ⇒ đứng im ở frame GIỮA (index 1).
    /// Trùng trạng thái cũ ⇒ KHÔNG áp lại.
    /// </summary>
    public void SetWalking(bool walking)
    {
        if (_hasApplied && walking == _walking) return;
        _walking = walking;
        Apply();
    }

    // ─── Nội bộ ─────────────────────────────────────────────────────────

    private void Apply()
    {
        if (!_ready || _player == null) return;

        Sprite[] set = _sets[(int)_facing];
        if (set == null || set.Length == 0)
        {
            WarnMissingOnce($"hướng {_facing} không có frame");
            enabled = false;
            return;
        }

        if (_player.target == null) _player.target = spriteTarget;

        // pingPong + loop: 3 frame -> chuỗi 0-1-2-1 = "1-2-3-2" của §5.1
        _player.SetFrames(set, walkFps, true, true);

        if (_walking) _player.Play();
        else          _player.PauseAtFrame(Mathf.Clamp(IdleFrameIndex, 0, set.Length - 1));

        _hasApplied = true;
    }

    private void EnsurePlayer()
    {
        if (_player == null) _player = GetComponent<SpriteSequencePlayer>();
        if (_player == null) _player = gameObject.AddComponent<SpriteSequencePlayer>();
        if (_player == null) return;

        _player.target          = spriteTarget;
        _player.useUnscaledTime = false;   // di chuyển world dùng Time.deltaTime (§0.6)
        _player.playOnEnable    = false;   // đứng chờ thì KHÔNG tự chạy chân tại chỗ
    }

    private bool ValidateSets()
    {
        for (int d = 0; d < _sets.Length; d++)
        {
            Sprite[] s = _sets[d];
            if (s == null || s.Length == 0) return false;

            bool coFrame = false;
            for (int i = 0; i < s.Length; i++)
                if (s[i] != null) { coFrame = true; break; }
            if (!coFrame) return false;
        }
        return true;
    }

    private void WarnMissingOnce(string lyDo)
    {
        if (_warnedMissingFrames) return;
        _warnedMissingFrames = true;
        Debug.LogWarning($"[Shipper] FourDirWalkAnimator trên '{name}': {lyDo} — " +
                         "nhân vật vẫn di chuyển đúng logic nhưng KHÔNG có animation. " +
                         "Khắc phục: slice sheet flowergirl_walk_spritesheet.png (3 cột × 4 hàng) " +
                         "rồi gán 12 sprite vào ShipperConfig.walkFrames theo thứ tự " +
                         "down/left/right/up. (Cảnh báo này chỉ in 1 lần.)", this);
    }
}
