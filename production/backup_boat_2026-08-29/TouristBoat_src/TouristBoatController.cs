using TMPro;
using UnityEngine;

/// <summary>
/// Visual controller của MỘT tàu du lịch (mỗi bến 1 tàu — GDD §3.2).
///
/// KHÔNG giữ logic thời gian nào: mỗi frame đọc pha từ BoatDockManager
/// (state + tiến độ 0-1) rồi ĐẶT vị trí tương ứng trên polyline
/// [BlindPoint → WP_01..WP_n → Berth]. Vì vị trí là hàm thuần của tiến độ,
/// vào game giữa chừng tàu tự snap đúng chỗ trên path — không cần code
/// "tua lại" riêng (GDD §5 edge 2).
///
///   Hidden    → SetActive(false) phần Visual, đứng chờ tại điểm mù (kỹ thuật ẩn của TrainState)
///   Arriving  → tiến theo path về berth, mũi hướng bến, flip theo hướng chạy
///   Docked    → đậu tại berth, hiện countdown world-space (TMPro)
///   Departing → đi NGƯỢC path: tàu LÙI, KHÔNG quay đầu, KHÔNG flip khi lùi
///
/// Bob dập dềnh + flip tái dùng cách làm của FerryController (bob trên child
/// "Visual" — root vẫn đi đúng path, chỉ sprite nhấp nhô).
/// </summary>
public class TouristBoatController : MonoBehaviour
{
    [Header("Bến")]
    [Tooltip("Index bến 0-2. Để -1 sẽ tự suy từ tên 'Dock_XX' của node cha (hierarchy tool sinh).")]
    [SerializeField] private int dockIndex = -1;

    [Header("Visual")]
    [Tooltip("SpriteRenderer của tàu (child 'Visual'). Bỏ trống sẽ tự tìm.")]
    [SerializeField] private SpriteRenderer visual;

    [Tooltip("Sprite gốc quay mặt sang TRÁI? (như FerryController.spriteFacesLeft)")]
    [SerializeField] private bool spriteFacesLeft = false;

    [Header("Countdown khi đậu bến (world-space TMPro)")]
    [Tooltip("Bỏ trống sẽ tìm child 'Countdown', không có thì tự tạo TextMeshPro placeholder.")]
    [SerializeField] private TMP_Text countdownText;

    [Tooltip("Vị trí chữ countdown so với tàu (unit world)")]
    [SerializeField] private Vector3 countdownOffset = new Vector3(0f, 60f, 0f);

    [Tooltip("Cỡ chữ countdown placeholder tự tạo")]
    [SerializeField] private float countdownFontSize = 72f;

    [Header("Canh vị trí (khi tàu đậu bị lệch khỏi ô)")]
    [Tooltip("Dịch tàu thêm so với waypoint/Berth, tính bằng unit world. " +
             "Dùng khi pivot của sprite tàu không nằm giữa thân, làm tàu đậu lệch khỏi ô. " +
             "Trong Play Mode không kéo tay được vì code ghi lại vị trí mỗi frame — chỉnh 2 số này thay vì kéo. " +
             "Tool: Tools/Farm Game/Tourist Boat/10.")]
    [SerializeField] private Vector3 berthOffset = Vector3.zero;

    // ─── Runtime ────────────────────────────────────────────────────────

    private int       _dockIndex = -1;     // index đã resolve (serialized hoặc suy từ tên cha)
    private Vector3[] _points;             // polyline cache: [điểm mù, WP..., berth]
    private float[]   _cumLengths;         // độ dài cộng dồn tới từng điểm
    private float     _totalLength;
    private bool      _pathReady;
    private bool      _warnedNoPath;
    // Cờ chống spam log: mỗi cảnh báo setup chỉ in đúng 1 lần cho mỗi tàu.
    private bool      _warnedNoSetup;
    private bool      _warnedNoVisual;

    private Vector3 _visualBaseLocalPos;   // localPosition gốc của Visual — bob cộng lên từ đây
    private float   _bobTime;
    private bool    _visualShown  = true;
    private bool    _countdownShown = true;
    private bool    _facingLeft;
    private int     _lastShownSeconds = -1;

    // ─── Unity lifecycle ────────────────────────────────────────────────

    private void Start()
    {
        _dockIndex = ResolveDockIndex();
        if (_dockIndex < 0)
            Debug.LogWarning($"[TouristBoat] {name}: không xác định được dockIndex " +
                             "(đặt trong Inspector hoặc đặt tàu dưới node 'Dock_XX'). Tàu sẽ đứng yên.");

        // Tự tìm Visual nếu chưa gán — ưu tiên child đúng tên theo hierarchy tool sinh.
        if (visual == null)
        {
            Transform v = transform.Find("Visual");
            visual = v != null ? v.GetComponent<SpriteRenderer>()
                               : GetComponentInChildren<SpriteRenderer>(true);
        }
        if (visual == null)
            Debug.LogWarning($"[TouristBoat] {name}: không tìm thấy SpriteRenderer 'Visual' — tàu chạy không hình (chờ Sếp gắn art).");
        else
            _visualBaseLocalPos = visual.transform.localPosition;

        SetupCountdown();
        ShowCountdown(false);
    }

    private void Update()
    {
        BoatDockManager mgr = BoatDockManager.Instance;
        if (mgr == null || mgr.Config == null || _dockIndex < 0)
        {
            // Trước đây nhánh này im lặng tuyệt đối: dockIndex = -1 (tool không wire
            // được, hoặc object cha bị đổi tên khác "Dock_XX") làm tàu tắt VĨNH VIỄN
            // mà không ai biết vì sao. Cảnh báo MỘT LẦN duy nhất — không spam mỗi frame.
            if (!_warnedNoSetup && mgr != null && mgr.Config != null && _dockIndex < 0)
            {
                _warnedNoSetup = true;
                Debug.LogWarning($"[TouristBoat] '{name}': dockIndex = -1 nen tau nay se KHONG BAO GIO hien. " +
                                 "Sua: gan dockIndex trong Inspector (0/1/2), hoac dat tau duoi object ten " +
                                 "'Dock_01'/'Dock_02'/'Dock_03' roi chay Tools/Farm Game/Tourist Boat/1. Setup All. " +
                                 "Chan doan day du: menu 6. Chan Doan.", this);
            }
            return;
        }

        if (!_warnedNoVisual && visual == null)
        {
            _warnedNoVisual = true;
            Debug.LogWarning($"[TouristBoat] '{name}': field Visual chua gan — logic thoi gian van chay " +
                             "nhung khong co gi hien tren man hinh. Keo SpriteRenderer cua con tau vao field Visual.", this);
        }

        // Path dựng lười: Start của manager có thể chạy SAU Start của tàu
        // (thứ tự script không đảm bảo) nên thử lại mỗi frame tới khi có.
        if (!_pathReady)
            TryBuildPath(mgr);

        BoatPhaseInfo info;
        if (!mgr.TryGetPhaseInfo(_dockIndex, out info))
        {
            // Locked / manager chưa sẵn sàng — tàu ẩn hoàn toàn.
            SetVisualShown(false);
            ShowCountdown(false);
            return;
        }

        switch (info.State)
        {
            case BoatState.Hidden:
                // Kỹ thuật ẩn của TrainState: tắt Visual, root đứng chờ ở điểm mù
                // sẵn tư thế cho pha Arriving kế tiếp.
                SetVisualShown(false);
                ShowCountdown(false);
                if (_pathReady)
                    transform.position = _points[0] + berthOffset;
                break;

            case BoatState.Arriving:
                SetVisualShown(true);
                ShowCountdown(false);
                // Tiến độ 0→1 = điểm mù → berth. Flip theo hướng chạy (mũi hướng bến).
                if (_pathReady)
                    PlaceAlongPath((float)info.Progress, true);
                break;

            case BoatState.Docked:
                SetVisualShown(true);
                if (_pathReady)
                    transform.position = _points[_points.Length - 1] + berthOffset; // đậu chính xác tại berth
                ShowCountdown(true);
                UpdateCountdownText(info.DockedRemainingSeconds);
                break;

            case BoatState.Departing:
                SetVisualShown(true);
                ShowCountdown(false);
                // Đi NGƯỢC path: tiến độ pha 0→1 ứng với quãng đường 1→0.
                // KHÔNG cập nhật flip — tàu LÙI thẳng ra, không quay đầu (GDD §3.2).
                if (_pathReady)
                    PlaceAlongPath(1f - (float)info.Progress, false);
                break;
        }

        if (_visualShown)
            Bob(mgr.Config);
    }

    // ─── Di chuyển theo polyline ────────────────────────────────────────

    /// <summary>
    /// Đặt tàu tại vị trí ứng với tỉ lệ quãng đường t (0 = điểm mù, 1 = berth).
    /// updateFacing = true chỉ trong pha Arriving — Departing giữ nguyên hướng cũ.
    /// Không alloc: đọc mảng cache, toàn phép toán struct.
    /// </summary>
    private void PlaceAlongPath(float t, bool updateFacing)
    {
        float distance = Mathf.Clamp01(t) * _totalLength;

        Vector3 position;
        Vector3 direction;
        SamplePath(distance, out position, out direction);

        transform.position = position + berthOffset;

        if (updateFacing && Mathf.Abs(direction.x) > 0.0001f)
            SetFacing(direction.x < 0f);
    }

    /// <summary>Nội suy vị trí + hướng đoạn tại quãng đường d dọc polyline.</summary>
    private void SamplePath(float distance, out Vector3 position, out Vector3 direction)
    {
        int last = _points.Length - 1;

        if (distance <= 0f)
        {
            position  = _points[0];
            direction = _points[1] - _points[0];
            direction.Normalize();
            return;
        }
        if (distance >= _totalLength)
        {
            position  = _points[last];
            direction = _points[last] - _points[last - 1];
            direction.Normalize();
            return;
        }

        for (int i = 1; i <= last; i++)
        {
            if (distance > _cumLengths[i]) continue;

            float segmentLength = _cumLengths[i] - _cumLengths[i - 1];
            float k = segmentLength > 0.0001f
                ? (distance - _cumLengths[i - 1]) / segmentLength
                : 1f;
            position  = Vector3.Lerp(_points[i - 1], _points[i], k);
            direction = _points[i] - _points[i - 1];
            direction.Normalize();
            return;
        }

        // Không tới được đây (đã chặn distance >= _totalLength) — trả berth cho chắc.
        position  = _points[last];
        direction = _points[last] - _points[last - 1];
        direction.Normalize();
    }

    /// <summary>
    /// Dựng cache polyline từ manager. Path thiếu → fallback 2 điểm
    /// (điểm mù + berth); thiếu nốt thì tàu đứng yên, chỉ warning 1 lần — không NRE
    /// (GDD §5 edge 8).
    ///
    /// [QA m-2] Hàm này được GỌI LẠI mỗi frame tới khi path hợp lệ. Buffer
    /// _points/_cumLengths được TÁI DÙNG qua EnsurePathBuffers — chỉ alloc khi
    /// kích thước đổi (thực tế: đúng 1 lần). Bản cũ null-out buffer khi path suy
    /// biến → new 2 mảng mỗi frame vĩnh viễn nếu waypoint trùng nhau.
    /// </summary>
    private void TryBuildPath(BoatDockManager mgr)
    {
        Transform[] pathTransforms = mgr.GetDockPathPoints(_dockIndex);
        int count;

        if (pathTransforms == null || pathTransforms.Length < 2)
        {
            // Fallback: đường thẳng điểm mù → berth.
            Transform blind = mgr.GetBlindPoint();
            Transform berth = mgr.GetDockBerth(_dockIndex);
            if (blind == null || berth == null)
            {
                if (!_warnedNoPath)
                {
                    _warnedNoPath = true;
                    Debug.LogWarning($"[TouristBoat] {name}: bến {_dockIndex + 1} chưa có path/berth hợp lệ — tàu đứng yên chờ tool sinh waypoint.");
                }
                return;
            }

            count = 2;
            EnsurePathBuffers(count);
            _points[0] = blind.position;
            _points[1] = berth.position;
        }
        else
        {
            count = pathTransforms.Length;
            EnsurePathBuffers(count);
            for (int i = 0; i < count; i++)
            {
                if (pathTransforms[i] == null) return; // waypoint bị xóa giữa chừng — thử lại frame sau
                _points[i] = pathTransforms[i].position;
            }
        }

        // Độ dài cộng dồn — ghi đè tại chỗ lên buffer tái dùng, không alloc.
        _cumLengths[0] = 0f;
        for (int i = 1; i < count; i++)
            _cumLengths[i] = _cumLengths[i - 1] + Vector3.Distance(_points[i - 1], _points[i]);
        _totalLength = _cumLengths[count - 1];

        // Path suy biến (mọi điểm trùng nhau) — coi như chưa có path, GIỮ buffer
        // để frame sau thử lại không tốn alloc nào (QA m-2).
        if (_totalLength <= 0.01f)
            return;

        _pathReady = true;
    }

    /// <summary>Cấp buffer polyline đúng kích thước — chỉ alloc khi count đổi (QA m-2).</summary>
    private void EnsurePathBuffers(int count)
    {
        if (_points == null || _points.Length != count)
        {
            _points     = new Vector3[count];
            _cumLengths = new float[count];
        }
    }

    // ─── Visual: bob + flip (tái dùng cách làm FerryController) ─────────

    /// <summary>
    /// Dập dềnh sprite theo sin — chỉ đụng localPosition.y của child Visual,
    /// root vẫn nằm đúng path. Biên độ/tần số từ config (không hardcode).
    /// </summary>
    private void Bob(TouristBoatConfig cfg)
    {
        if (visual == null) return;

        _bobTime += Time.deltaTime;
        float scaleY = Mathf.Max(0.0001f, transform.lossyScale.y);

        Vector3 lp = visual.transform.localPosition;
        lp.y = _visualBaseLocalPos.y +
               Mathf.Sin(_bobTime * cfg.bobFrequency * Mathf.PI * 2f) * cfg.bobAmplitude / scaleY;
        visual.transform.localPosition = lp;
    }

    /// <summary>Flip sprite theo hướng chạy ngang (flipX, không xoay). Chỉ gọi khi Arriving.</summary>
    private void SetFacing(bool movingLeft)
    {
        if (_facingLeft == movingLeft) return;
        _facingLeft = movingLeft;
        if (visual != null)
            visual.flipX = spriteFacesLeft ? !movingLeft : movingLeft;
    }

    /// <summary>Bật/tắt GameObject Visual — có guard tránh gọi SetActive lặp mỗi frame.</summary>
    private void SetVisualShown(bool shown)
    {
        if (_visualShown == shown) return;
        _visualShown = shown;
        if (visual != null)
            visual.gameObject.SetActive(shown);
    }

    // ─── Countdown world-space ──────────────────────────────────────────

    /// <summary>
    /// Chuẩn bị TMP countdown: ưu tiên ref Inspector → child "Countdown" →
    /// tự tạo TextMeshPro placeholder (game vẫn chạy khi tool chưa sinh đủ).
    /// Project dùng TMPro (xem FarmUIManager) nên placeholder cũng là TMPro.
    /// </summary>
    private void SetupCountdown()
    {
        if (countdownText == null)
        {
            Transform t = transform.Find("Countdown");
            if (t != null)
                countdownText = t.GetComponent<TMP_Text>();
        }

        if (countdownText == null)
        {
            var go = new GameObject("Countdown");
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize  = countdownFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(400f, 120f);
            tmp.text = string.Empty;

            // Nổi lên trên sprite tàu (tàu lửa dùng order 650 — xem TrainPathFollower).
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 700;

            countdownText = tmp;
        }

        // Con của Boat root (không phải Visual) → đi theo tàu nhưng không dập dềnh.
        countdownText.transform.localPosition = countdownOffset;
    }

    /// <summary>Bật/tắt countdown — có guard tránh SetActive lặp.</summary>
    private void ShowCountdown(bool shown)
    {
        if (_countdownShown == shown || countdownText == null)
        {
            _countdownShown = shown && countdownText != null;
            return;
        }
        _countdownShown = shown;
        countdownText.gameObject.SetActive(shown);
        if (!shown)
            _lastShownSeconds = -1; // lần đậu sau set text lại từ đầu
    }

    /// <summary>
    /// Cập nhật chữ "m:ss". Chỉ dựng string khi CON SỐ GIÂY đổi (1 lần/giây) —
    /// giữ luật "không alloc trong vòng frame": 59 frame còn lại không cấp phát gì.
    /// </summary>
    private void UpdateCountdownText(double remainingSeconds)
    {
        if (countdownText == null) return;

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)remainingSeconds));
        if (totalSeconds == _lastShownSeconds) return;
        _lastShownSeconds = totalSeconds;

        int m = totalSeconds / 60;
        int s = totalSeconds % 60;
        countdownText.text = m.ToString() + ":" + s.ToString("00");
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Suy dockIndex: ưu tiên giá trị Inspector (>= 0), không thì dò ngược cây cha
    /// tìm node tên "Dock_XX" (hierarchy tool của Dev B sinh) và parse XX - 1.
    /// </summary>
    private int ResolveDockIndex()
    {
        if (dockIndex >= 0) return dockIndex;

        Transform p = transform.parent;
        while (p != null)
        {
            string n = p.name;
            if (n.StartsWith("Dock_"))
            {
                int number;
                if (int.TryParse(n.Substring(5), out number) && number >= 1)
                    return number - 1;
            }
            p = p.parent;
        }
        return -1;
    }

#if UNITY_EDITOR
    /// <summary>
    /// (Editor) Toạ độ tàu SẼ đậu khi vào Play Mode = Berth + berthOffset.
    /// Tool menu 10 dùng hàm này để snap tàu trong Edit Mode, cho thấy trước
    /// đúng vị trí Play Mode — vì trong Play Mode kéo tay bị code ghi đè mỗi frame.
    /// </summary>
    public Vector3 EditorGetDockedPosition(Transform berth)
        => (berth != null ? berth.position : transform.position) + berthOffset;

    /// <summary>(Editor) Ghi offset từ vị trí tàu hiện tại so với berth — "kéo rồi chốt".</summary>
    public void EditorCaptureOffsetFrom(Transform berth)
    {
        if (berth == null) return;
        berthOffset = transform.position - berth.position;
    }

    /// <summary>(Editor) Đọc offset đang lưu.</summary>
    public Vector3 EditorBerthOffset => berthOffset;
#endif
}
