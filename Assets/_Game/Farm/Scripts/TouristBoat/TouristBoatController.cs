using TMPro;
using UnityEngine;

/// <summary>
/// Visual controller của MỘT tàu du lịch (mỗi bến 1 tàu — GDD §3.1).
///
/// KHÔNG giữ logic thời gian nào: mỗi frame đọc pha từ BoatDockManager
/// (state + tiến độ 0-1) rồi ĐẶT vị trí tương ứng trên polyline
/// [BlindPoint → WP_01..WP_n → Berth]. Vì vị trí là hàm thuần của tiến độ,
/// vào game giữa chừng tàu tự snap đúng chỗ trên path — không cần code
/// "tua lại" riêng (GDD V2 §5 edge 1).
///
///   WaitingNext → SetActive(false) phần Visual, đứng chờ tại điểm mù
///   Arriving    → tiến theo path về berth, mũi hướng bến, flip theo hướng chạy
///   Docked      → đậu tại berth; V2 KHÔNG còn countdown (đậu tới khi khách xong)
///   Departing   → đi NGƯỢC path: tàu LÙI, KHÔNG quay đầu, KHÔNG flip khi lùi
///
/// ── Đổi ở V2 (BOAT-002) ──────────────────────────────────────────────────
/// Pha Docked giờ VÔ HẠN (chờ Dev B báo khách lên tàu hết) nên mốc thời gian cố
/// định không còn nghĩa: chữ world-space khi đậu chuyển thành nhãn tĩnh
/// "Đang đón khách..." (hoặc ẨN hẳn nếu bật showDockedLabel = false).
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

    [Header("12 Directional Sprites (360° Clockwise: 0=12h, 3=3h, 6=6h, 9=9h)")]
    [Tooltip("Mảng 12 sprite hướng quay tròn 360 độ (mỗi góc 30 độ). Nếu để trống sẽ tự động tải từ Assets/Assetsgame/TouristBoat/12_Directions/.")]
    [SerializeField] private Sprite[] directionalSprites = new Sprite[12];

    [Header("Visual Scale & Motion")]
    [Tooltip("Cỡ mong muốn của tàu trong thế giới (unit world).")]
    [SerializeField] private float boatWorldWidth = 680f;

    [Tooltip("Góc nghiêng mạn thuyền tối đa khi dập dềnh sóng (độ).")]
    [SerializeField] private float waveRollAngle = 2.4f;

    [Tooltip("Góc nghiêng thân thuyền khi bẻ lái cua (độ).")]
    [SerializeField] private float turnBankingAngle = 4.5f;

    [Header("Nhãn khi đậu bến (world-space TMPro)")]
    [Tooltip("Bỏ trống sẽ tìm child 'Countdown', không có thì tự tạo TextMeshPro placeholder. " +
             "V2: không còn countdown — chỉ hiện nhãn tĩnh khi tàu đang đón khách.")]
    [SerializeField] private TMP_Text countdownText;

    [Tooltip("V2: BẬT để hiện nhãn 'Đang đón khách...' khi tàu đậu; TẮT để ẩn hẳn chữ trên tàu " +
             "(dùng khi Dev C đã có UI riêng cho trạng thái chuyến).")]
    [SerializeField] private bool showDockedLabel = true;

    [Tooltip("Nội dung nhãn khi tàu đang đậu đón khách (V2 — không còn mốc thời gian cố định).")]
    [SerializeField] private string dockedLabel = "Đang đón khách...";

    [Tooltip("Vị trí chữ so với tàu (unit world)")]
    [SerializeField] private Vector3 countdownOffset = new Vector3(0f, 60f, 0f);

    [Tooltip("Cỡ chữ placeholder tự tạo")]
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
    private bool      _warnedNoSetup;
    private bool      _warnedNoVisual;

    private Vector3   _visualBaseLocalPos;
    private float     _bobTime;
    private bool      _visualShown    = true;
    private bool      _countdownShown = true;
    private bool      _facingLeft;
    private bool      _dockedLabelSet;

    private float     _currentBankZ;
    private Vector3   _lastDirection = Vector3.right;
    private bool      _isInitializedSprites;
    private BoatState _lastKnownState = BoatState.WaitingNext;

    // ─── Unity lifecycle ────────────────────────────────────────────────

    private void Start()
    {
        _dockIndex = ResolveDockIndex();
        if (_dockIndex < 0)
            Debug.LogWarning($"[TouristBoat] {name}: không xác định được dockIndex. Tàu sẽ đứng yên.");

        if (visual == null)
        {
            Transform v = transform.Find("Visual");
            visual = v != null ? v.GetComponent<SpriteRenderer>()
                               : GetComponentInChildren<SpriteRenderer>(true);
        }

        if (visual != null)
        {
            _visualBaseLocalPos = visual.transform.localPosition;
            visual.sortingLayerName = "ObjectsFront";
            visual.sortingOrder = 200;
        }

        EnsureDirectionalSprites();
        SetupCountdown();
        ShowCountdown(false);
    }

    private void EnsureDirectionalSprites()
    {
        if (_isInitializedSprites) return;
        _isInitializedSprites = true;

        bool hasMissing = false;
        if (directionalSprites == null || directionalSprites.Length != 12)
        {
            directionalSprites = new Sprite[12];
            hasMissing = true;
        }
        else
        {
            for (int i = 0; i < 12; i++)
            {
                if (directionalSprites[i] == null) { hasMissing = true; break; }
            }
        }

        if (!hasMissing) return;

        // Tự động load 12 sprite tương ứng (Tàu đỏ cho dock 1, Tàu xanh cho dock 0 & 2)
        string prefix = (_dockIndex == 1) ? "boat_red_12_dir_" : "boat_blue_12_dir_";

        for (int i = 0; i < 12; i++)
        {
            if (directionalSprites[i] != null) continue;

            string fileName = $"{prefix}{i}";
            Sprite s = Resources.Load<Sprite>($"TouristBoat/{fileName}");
#if UNITY_EDITOR
            if (s == null)
            {
                string path = $"Assets/Assetsgame/TouristBoat/12_Directions/{fileName}.png";
                s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
#endif
            directionalSprites[i] = s;
        }
    }

    private void Update()
    {
        BoatDockManager mgr = BoatDockManager.Instance;
        if (mgr == null || mgr.Config == null || _dockIndex < 0)
        {
            if (!_warnedNoSetup && mgr != null && mgr.Config != null && _dockIndex < 0)
            {
                _warnedNoSetup = true;
                Debug.LogWarning($"[TouristBoat] '{name}': dockIndex = -1 nên tàu sẽ không hiện.", this);
            }
            return;
        }

        if (!_pathReady)
            TryBuildPath(mgr);

        EnsureDirectionalSprites();

        if (!mgr.IsDockUnlocked(_dockIndex))
        {
            SetVisualShown(false);
            ShowCountdown(false);
            return;
        }

        BoatPhaseInfo info;
        if (!mgr.TryGetPhaseInfo(_dockIndex, out info))
        {
            SetVisualShown(false);
            ShowCountdown(false);
            return;
        }

        // [FIX COMPILE 2026-09-03] Khoi am thanh duoc chen o day da lam mat 'return;' + '}'
        // dong khoi guard ben tren => vo class (16 loi CS0106/CS1513). Da tra lai guard,
        // giu nguyen logic coi tau, dat SAU guard nen 'info' chac chan hop le.
        if (_lastKnownState != info.State)
        {
            if (info.State == BoatState.Arriving || info.State == BoatState.Docked)
            {
                AudioManager.Instance?.PlayBoatHorn();
            }
            _lastKnownState = info.State;
        }

        switch (info.State)
        {
            case BoatState.WaitingNext:
                SetVisualShown(false);
                ShowCountdown(false);
                if (_pathReady)
                    transform.position = _points[0] + berthOffset;
                break;

            case BoatState.Arriving:
                SetVisualShown(true);
                ShowCountdown(false);
                if (_pathReady)
                    PlaceAlongPathArriving((float)info.Progress);
                break;

            case BoatState.Docked:
                SetVisualShown(true);
                if (_pathReady)
                {
                    Vector3 berthPos = _points[_points.Length - 1] + berthOffset;
                    transform.position = berthPos;
                    // Hướng đậu bến: quay mặt vào bến
                    ApplyDirectionSprite(_lastDirection);
                }
                ShowCountdown(showDockedLabel);
                if (showDockedLabel)
                    ApplyDockedLabel();
                break;

            case BoatState.Departing:
                SetVisualShown(true);
                ShowCountdown(false);
                if (_pathReady)
                    PlaceAlongPathDeparting((float)info.Progress);
                break;
        }

        if (_visualShown)
            Bob(mgr.Config, info.State == BoatState.Docked);
    }

    // ─── Di chuyển & Quay đầu 360° ──────────────────────────────────────

    /// <summary>
    /// Tiến vào bến (Arriving): đi từ Điểm Mù → Berth, cập bến mượt mà với hướng mũi tàu chuẩn.
    /// </summary>
    private void PlaceAlongPathArriving(float progress)
    {
        // Smooth deceleration khi gần tới bến
        float t = progress;
        float distance = Mathf.Clamp01(t) * _totalLength;

        Vector3 position;
        Vector3 direction;
        SamplePath(distance, out position, out direction);

        transform.position = position + berthOffset;
        _lastDirection = direction;

        ApplyDirectionSprite(direction);
    }

    /// <summary>
    /// Rời bến (Departing): Bẻ lái quay đầu 180° mượt mà rồi rẽ sóng thẳng tiến ra khơi!
    /// </summary>
    private void PlaceAlongPathDeparting(float progress)
    {
        // Quãng đường thực tế đi từ 1 -> 0 (từ Berth về BlindPoint)
        float t = Mathf.Clamp01(progress);

        // Giai đoạn 1: Quay đầu 180° (Turn Arc) trong 28% tiến độ đầu tiên khi rời bến
        const float turnPhaseDuration = 0.28f;

        Vector3 berthPos = _points[_points.Length - 1];
        Vector3 arrivalDir = (_points[_points.Length - 1] - _points[_points.Length - 2]).normalized;
        Vector3 departDir  = -arrivalDir;

        if (t < turnPhaseDuration)
        {
            float turnT = t / turnPhaseDuration;
            float smoothTurn = Mathf.SmoothStep(0f, 1f, turnT);

            // Nội suy góc quay 180 độ từ Hướng Cập Bến -> Hướng Rời Bến
            float arrivalAngle = Mathf.Atan2(arrivalDir.y, arrivalDir.x) * Mathf.Rad2Deg;
            float departAngle  = Mathf.Atan2(departDir.y, departDir.x)   * Mathf.Rad2Deg;
            float curAngle     = Mathf.LerpAngle(arrivalAngle, departAngle, smoothTurn);

            float rad = curAngle * Mathf.Deg2Rad;
            Vector3 headingDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            // Vòng cua nhẹ ra ngoài mạn bến để tàu không bị va vào cầu cảng
            Vector3 lateralNorm = new Vector3(-arrivalDir.y, arrivalDir.x, 0f);
            float arcOffset = Mathf.Sin(turnT * Mathf.PI) * 48f;

            // Di chuyển nhích dần ra khỏi bến
            float backDist = smoothTurn * 70f;
            Vector3 pos = berthPos + (departDir * backDist) + (lateralNorm * arcOffset);

            transform.position = pos + berthOffset;
            ApplyDirectionSprite(headingDir);
            _currentBankZ = Mathf.Sin(turnT * Mathf.PI) * -turnBankingAngle;
        }
        else
        {
            // Giai đoạn 2: Đã quay đầu xong -> rẽ sóng chạy thẳng về Điểm Mù ngoài khơi
            float cruiseProgress = (t - turnPhaseDuration) / (1f - turnPhaseDuration);
            // Đi từ quãng đường (totalLength - 70) về 0
            float remainDist = _totalLength - 70f;
            float distance = (_totalLength - 70f) * (1f - cruiseProgress);

            Vector3 position;
            Vector3 pathTangent;
            SamplePath(distance, out position, out pathTangent);

            transform.position = position + berthOffset;
            Vector3 outDir = -pathTangent; // Mũi tàu hướng về phía trước theo hướng chạy
            _lastDirection = outDir;

            ApplyDirectionSprite(outDir);
            _currentBankZ = 0f;
        }
    }

    /// <summary>
    /// Nhận diện góc vector và áp dụng đúng 1 trong 12 sprite hướng 360° (chuẩn mặt đồng hồ).
    /// </summary>
    private void ApplyDirectionSprite(Vector3 dir)
    {
        if (visual == null) return;
        if (dir.sqrMagnitude < 0.0001f) return;

        // Góc toán học (-180..180) từ trục +X
        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Đổi sang góc đồng hồ: 12h (lên) = 0°, 3h (phải) = 90°, 6h (xuống) = 180°, 9h (trái) = 270°
        float clockDeg = (450f - angleDeg) % 360f;
        if (clockDeg < 0f) clockDeg += 360f;

        // Mỗi nấc 30 độ (12 hướng: 0..11)
        int dirIndex = Mathf.RoundToInt(clockDeg / 30f) % 12;

        if (directionalSprites != null && dirIndex < directionalSprites.Length && directionalSprites[dirIndex] != null)
        {
            visual.sprite = directionalSprites[dirIndex];
            visual.flipX = false; // Không cần flip vì 12 hướng đã vẽ chuẩn 360 độ
        }

        // Đảm bảo kích thước tàu hiển thị đúng tỷ lệ bản đồ
        if (visual.sprite != null)
        {
            float nativeWidth = visual.sprite.rect.width / visual.sprite.pixelsPerUnit;
            if (nativeWidth > 0.001f)
            {
                float desiredWidth = (BoatDockManager.Instance != null && BoatDockManager.Instance.Config != null && BoatDockManager.Instance.Config.boatVisualWidth > 0.01f)
                    ? BoatDockManager.Instance.Config.boatVisualWidth
                    : boatWorldWidth;

                float targetScale = desiredWidth / nativeWidth;
                visual.transform.localScale = Vector3.one * targetScale;
            }
        }
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

        position  = _points[last];
        direction = _points[last] - _points[last - 1];
        direction.Normalize();
    }

    /// <summary>
    /// Dựng cache polyline từ manager. Path thiếu → fallback 2 điểm
    /// (điểm mù + berth); thiếu nốt thì tàu đứng yên, chỉ warning 1 lần — không NRE.
    ///
    /// [QA m-2] Hàm này được GỌI LẠI mỗi frame tới khi path hợp lệ. Buffer
    /// _points/_cumLengths được TÁI DÙNG qua EnsurePathBuffers — chỉ alloc khi
    /// kích thước đổi (thực tế: đúng 1 lần).
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
    /// Dập dềnh thân tàu theo sóng nước (Heave trục Y + Roll lắc lư mạn + Banking khi bẻ lái).
    /// </summary>
    private void Bob(TouristBoatConfig cfg, bool isDocked)
    {
        if (visual == null) return;

        float freq = cfg != null ? cfg.bobFrequency : 1.2f;
        float amp  = cfg != null ? cfg.bobAmplitude : 3.5f;

        _bobTime += Time.deltaTime;
        float scaleY = Mathf.Max(0.0001f, transform.lossyScale.y);

        // 1. Nhấp nhô Heave (trục Y)
        float waveSpeed = isDocked ? freq * 0.8f : freq;
        float waveAmp   = isDocked ? amp * 0.7f : amp;
        float heaveY    = Mathf.Sin(_bobTime * waveSpeed * Mathf.PI * 2f) * waveAmp / scaleY;

        Vector3 lp = visual.transform.localPosition;
        lp.y = _visualBaseLocalPos.y + heaveY;
        visual.transform.localPosition = lp;

        // 2. Lắc lư Roll (góc Z) + Nghiêng thân khi bẻ lái (Banking)
        float rollAngle = Mathf.Sin(_bobTime * (waveSpeed * 0.9f) * Mathf.PI * 2f) * (isDocked ? waveRollAngle * 0.6f : waveRollAngle);
        float totalRotZ = rollAngle + _currentBankZ;

        visual.transform.localRotation = Quaternion.Euler(0f, 0f, totalRotZ);
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

    // ─── Nhãn world-space khi đậu bến ───────────────────────────────────

    /// <summary>
    /// Chuẩn bị TMP: ưu tiên ref Inspector → child "Countdown" → tự tạo
    /// TextMeshPro placeholder (game vẫn chạy khi tool chưa sinh đủ).
    /// Tên child giữ "Countdown" như V1 để scene/prefab cũ không phải sửa.
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
            if (mr != null)
            {
                // [FIX 2026-09-03] Thiếu sortingLayerName ⇒ rơi về layer "Default" (thấp hơn "Objects" của khách) ⇒ khách đè lên chữ. Ép về cùng layer với thân tàu.
                mr.sortingLayerName = "ObjectsFront";
                mr.sortingOrder = 700;
            }

            countdownText = tmp;
        }

        // Con của Boat root (không phải Visual) → đi theo tàu nhưng không dập dềnh.
        countdownText.transform.localPosition = countdownOffset;
    }

    /// <summary>Bật/tắt nhãn — có guard tránh SetActive lặp mỗi frame.</summary>
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
            _dockedLabelSet = false; // lần đậu sau set text lại từ đầu
    }

    /// <summary>
    /// V2: ghi nhãn tĩnh "Đang đón khách..." đúng MỘT LẦN cho mỗi lần đậu —
    /// giữ luật "không alloc trong vòng frame" (V1 dựng string countdown mỗi giây).
    /// </summary>
    private void ApplyDockedLabel()
    {
        if (countdownText == null || _dockedLabelSet) return;
        _dockedLabelSet    = true;
        countdownText.text = dockedLabel;
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Suy dockIndex: ưu tiên giá trị Inspector (>= 0), không thì dò ngược cây cha
    /// tìm node tên "Dock_XX" (hierarchy tool sinh) và parse XX - 1.
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
    /// Tool menu 10 và BoatShoreAdjustTool dùng hàm này để snap/xem trước vị trí
    /// đậu trong Edit Mode — vì trong Play Mode kéo tay bị code ghi đè mỗi frame.
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
