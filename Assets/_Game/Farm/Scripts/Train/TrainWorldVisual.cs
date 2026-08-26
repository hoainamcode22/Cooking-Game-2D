using UnityEngine;

/// <summary>
/// Visual runtime cho tàu WORLD — đồng bộ style với tàu popup:
///  - Đổi frame theo chuyển động (bánh lăn + thân nhún) từ bộ frame đội vẽ giao.
///  - Tự chọn bộ frame theo hướng chạy (trái-xuống / phải-lên) dựa trên delta vị trí.
///  - Phun khói từ miệng ống khói bằng sprite train_smoke_puff.png có sẵn (chỉ đầu tàu).
/// Gắn tự động bằng menu: Tools → Farm Game → Train → Setup World Train Frames.
/// Không đụng TrainPathFollower — script này chỉ đọc vị trí, không di chuyển tàu.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TrainWorldVisual : MonoBehaviour
{
    [Header("Frames — gán qua tool Setup World Train Frames")]
    [Tooltip("Frame khi tàu chạy hướng chéo TRÁI-XUỐNG (world_..._frontleft_01→06)")]
    [SerializeField] private Sprite[] framesFrontLeft;
    [Tooltip("Frame khi tàu chạy hướng chéo PHẢI-LÊN (world_..._upright_01→06)")]
    [SerializeField] private Sprite[] framesUpRight;

    [Header("Animation")]
    [Tooltip("Số frame/giây khi tàu đang chạy")]
    [SerializeField] private float runFps = 10f;
    [Tooltip("Quãng đường tối thiểu trong 1 frame (world unit) để tính là 'đang chạy'")]
    [SerializeField] private float moveThreshold = 0.1f;

    [Tooltip("Thiếu bộ frame của 1 hướng → dùng bộ hướng kia và LẬT GƯƠNG ngang (đội vẽ mới giao hướng upright)")]
    [SerializeField] private bool flipXWhenFallback = true;

    [Tooltip("Sorting CỐ ĐỊNH theo thứ tự đoàn tàu (Sếp chốt 2026-08-26): đầu tàu cao nhất, toa giảm dần. -1 = không đụng sorting. Tool tự gán: Locomotive=660, Wagon_01=659 ... Wagon_04=656.")]
    [SerializeField] private int sortingOrder = -1;

    // Scale gốc của object TRƯỚC khi tool fit theo art mới — tool ghi 1 lần để fit idempotent
    [HideInInspector] [SerializeField] private Vector3 fitBaseScale = Vector3.zero;

    [Tooltip("Hướng mặc định khi tàu ĐỨNG YÊN: 0 = trái-xuống (tàu thưởng quay về ga), 1 = phải-lên (tàu giao chờ khởi hành). Tool tự đặt theo TrainVisualRoot / TrainVisualRoot2.")]
    [SerializeField] private int initialDir = 1;

    [Header("Khói ống khói — chỉ bật cho đầu tàu")]
    [SerializeField] private bool   emitSmoke;
    [SerializeField] private Sprite smokePuffSprite;
    [Tooltip("Vị trí miệng ống khói so với pivot (world unit). Để (0,0) = tự tính từ bounds sprite. X tự lật theo hướng chạy.")]
    [SerializeField] private Vector2 chimneyOffset = Vector2.zero;
    [Tooltip("Nhịp phun khói khi tàu đang chạy (giây/cụm)")]
    [SerializeField] private float smokeIntervalMoving = 0.30f;
    [Tooltip("Nhịp phun khói khi tàu đứng ở ga (giây/cụm)")]
    [SerializeField] private float smokeIntervalIdle   = 1.40f;

    private SpriteRenderer _sr;
    private Vector3 _lastPos;
    private bool    _moving;
    private int     _dir;        // 0 = FrontLeft, 1 = UpRight
    private float   _frameTimer;
    private int     _frame;
    private float   _smokeTimer;

    void Awake()
    {
        _sr      = GetComponent<SpriteRenderer>();
        _lastPos = transform.position;
    }

    void OnEnable()
    {
        _lastPos    = transform.position;
        _frame      = 0;
        _frameTimer = 0f;
        _smokeTimer = 0.4f; // phun cụm đầu sớm cho có sức sống
        _dir        = Mathf.Clamp(initialDir, 0, 1); // đứng yên = quay đúng hướng ga/hầm của tàu này
        if (sortingOrder >= 0 && _sr != null) _sr.sortingOrder = sortingOrder;
        ApplyFrame();
    }

    void LateUpdate()
    {
        // 1. Phát hiện chuyển động + hướng (chạy SAU TrainPathFollower đặt vị trí)
        Vector3 delta = transform.position - _lastPos;
        _lastPos = transform.position;

        _moving = delta.magnitude > moveThreshold;
        if (_moving && Mathf.Abs(delta.x) > 0.0001f)
            _dir = delta.x < 0f ? 0 : 1;

        // 2. Frame animation: chạy thì lăn bánh, đứng thì về frame nghỉ (frame 01)
        if (_moving)
        {
            _frameTimer += Time.deltaTime;
            float step = 1f / Mathf.Max(1f, runFps);
            while (_frameTimer >= step)
            {
                _frameTimer -= step;
                _frame++;
            }
        }
        else
        {
            _frame = 0;
            _frameTimer = 0f;
        }
        ApplyFrame();

        // Giữ sorting cố định (ConfigureTrainSorting của PathFollower có thể chạy lại khi ShowTrain)
        if (sortingOrder >= 0 && _sr != null && _sr.sortingOrder != sortingOrder)
            _sr.sortingOrder = sortingOrder;

        // 3. Khói bốc từ ống khói — nhanh khi chạy, chậm rãi khi đậu ở ga
        if (emitSmoke && smokePuffSprite != null && gameObject.activeInHierarchy)
        {
            _smokeTimer -= Time.deltaTime;
            if (_smokeTimer <= 0f)
            {
                _smokeTimer = _moving ? smokeIntervalMoving : smokeIntervalIdle;
                SpawnPuff();
            }
        }
    }

    private void ApplyFrame()
    {
        Sprite[] set = _dir == 0 ? framesFrontLeft : framesUpRight;
        bool usedFallback = false;
        if (set == null || set.Length == 0)
        {
            set = _dir == 0 ? framesUpRight : framesFrontLeft; // fallback nếu thiếu 1 hướng
            usedFallback = true;
        }
        if (set == null || set.Length == 0 || _sr == null) return;

        var sprite = set[_frame % set.Length];
        if (sprite != null && _sr.sprite != sprite)
            _sr.sprite = sprite;

        _sr.flipX = usedFallback && flipXWhenFallback;
    }

    private Vector3 ChimneyWorldPos()
    {
        float xSign = _dir == 0 ? -1f : 1f;

        if (chimneyOffset != Vector2.zero)
            return transform.position + new Vector3(chimneyOffset.x * xSign, chimneyOffset.y, 0f);

        // Auto: mép trên sprite, lệch về phía mũi tàu ~45% bề ngang
        var b = _sr.bounds;
        return new Vector3(b.center.x + xSign * b.extents.x * 0.45f, b.max.y - b.size.y * 0.06f, 0f);
    }

    private void SpawnPuff()
    {
        var go = new GameObject("TrainSmokePuff");
        go.transform.position = ChimneyWorldPos();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite         = smokePuffSprite;
        sr.sortingLayerID = _sr.sortingLayerID;
        sr.sortingOrder   = _sr.sortingOrder + 2;

        // Scale khói theo cỡ tàu — không phụ thuộc PPU/resolution ảnh
        float locoH = Mathf.Max(0.01f, _sr.bounds.size.y);
        float puffH = Mathf.Max(0.01f, smokePuffSprite.bounds.size.y);
        float baseScale = (locoH * 0.5f) / puffH;

        go.AddComponent<TrainWorldSmokePuff>().Init(_moving, baseScale, locoH);
    }
}

/// <summary>
/// 1 cụm khói: bay lên từ ống khói, nở to, lượn nhẹ theo gió rồi tan — tự hủy.
/// Sinh runtime bởi TrainWorldVisual, không cần prefab.
/// </summary>
public class TrainWorldSmokePuff : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Vector3 _origin;
    private float _t;
    private float _dur   = 1.4f;
    private float _rise;
    private float _drift;
    private float _scale0;
    private float _scale1;

    public void Init(bool strong, float baseScale, float locoHeight)
    {
        _sr     = GetComponent<SpriteRenderer>();
        _origin = transform.position;
        _rise   = locoHeight * (strong ? 1.3f : 1.0f);
        _drift  = Random.Range(-0.2f, 0.06f) * locoHeight;
        _scale0 = baseScale * 0.35f;
        _scale1 = baseScale * (strong ? 1.25f : 0.85f);
        if (!strong) _dur = 1.8f;

        transform.localScale = Vector3.one * _scale0;
        if (_sr != null) _sr.color = new Color(1f, 1f, 1f, 0.9f);
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = _t / _dur;
        if (k >= 1f) { Destroy(gameObject); return; }

        float sway = Mathf.Sin(k * Mathf.PI * 2f) * _rise * 0.06f;
        transform.position   = _origin + new Vector3(sway + _drift * k, _rise * k, 0f);
        transform.localScale = Vector3.one * Mathf.Lerp(_scale0, _scale1, k);
        if (_sr != null) _sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0f, k * k));
    }
}
