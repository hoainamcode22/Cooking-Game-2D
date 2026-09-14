using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Di chuyển tàu theo path.
/// trainVisualRoot.transform là điểm gốc di chuyển.
/// Carriages theo sau trainVisualRoot qua path history.
///
/// Public API duy nhất TrainManager được gọi:
///   SnapToPosition(pos, backwardDir)  — đặt tàu tại điểm, trải wagons
///   ShowTrain()                       — hiện visual
///   HideTrain()                       — ẩn visual
///   MoveTo(target, onDone)            — di chuyển, callback khi đến
/// </summary>
public class TrainPathFollower : MonoBehaviour
{
    private const string TrainSortingLayerName = "ObjectsFront";
    private const int TrainSortingOrder = 650;

    [Header("Visual Root — ROOT của toàn bộ tàu (engine + wagons phải là con của GO này)")]
    [Tooltip("Kéo TrainVisualRoot hoặc TrainVisualRoot2 vào đây. TOÀN BỘ tàu phải là con của GO này.")]
    [SerializeField] private Transform trainRoot;

    [Header("Toa tàu (theo thứ tự từ đầu tàu trở ra, là con của trainRoot)")]
    public Transform[] carriages;

    [Tooltip("Khoảng cách giữa các toa (world units).")]
    public float carriageSpacing = 150f;

    [Tooltip("Khoảng cách từ trainRoot đến Locomotive (carriages[0]). Thường nhỏ hơn carriageSpacing.")]
    public float locomotiveSpacing = 50f;

    [Header("Tốc độ di chuyển")]
    public float moveSpeed = 300f;

    [Header("Layout tay xếp (fix 2026-08-26: Play mode toa tách khỏi ray)")]
    [Tooltip("BẬT: dùng ĐÚNG vị trí/khoảng cách toa như Sếp đã xếp trong Scene lúc Edit — code chụp lại ở frame đầu, không trải lại bằng spacing cứng nữa. TẮT: hành vi cũ (locomotiveSpacing + carriageSpacing*i).")]
    public bool useAuthoredSpacing = true;

    [Header("Căn tàu theo ĐẦU TÀU (fix 2026-09-10: tàu dừng lệch xa ga)")]
    [Tooltip("BẬT (mặc định): điểm dừng (PointStationShip / PointStationReward…) có nghĩa là \"NƠI ĐẦU TÀU ĐỖ\", " +
             "không phải nơi cái root vô hình (TrainVisualRoot / TrainVisualRoot2) tới. " +
             "Vì các toa được Sếp kéo xếp tay LỆCH khỏi root, nếu không bù thì code đặt root lên marker rồi " +
             "cộng thêm độ lệch đó lần nữa — đầu tàu bay lên xa ga (TrainVisualRoot2 lệch ~ +2056, +1052). " +
             "TẮT: quay lại 100% hành vi cũ (đặt thẳng root lên marker), không đụng gì khác.")]
    public bool canhChinhTheoDauTau = true;

    [Header("Bù lệch ray Play Mode (fix 2026-09-11: tàu bị lệch khỏi đường ray)")]
    [Tooltip("BẬT (khuyên dùng): Giữ đúng vị trí trainRoot lúc Edit Mode khi đỗ tại ga, đảm bảo tàu khớp 100% đường ray không bị lệch trong Play Mode.")]
    public bool snapToAuthoredStationPosition = true;

    [Tooltip("Offset tuỳ biến (X, Y, Z) để dịch chuyển tàu thủ công trong Inspector lúc Play Mode nếu muốn tinh chỉnh.")]
    public Vector3 playModeOffset = Vector3.zero;

    private Vector3 _authoredRootPosition;
    private bool _hasAuthoredRootPosition;

    private Vector3[] _authoredLocalPositions;

    // Độ lệch world từ trainRoot đến ĐẦU TÀU (carriages[0]) — tính LẠI HOÀN TOÀN từ layout tay xếp
    // mỗi lần CaptureAuthoredLayout() chạy, nên KHÔNG BAO GIỜ cộng dồn. Vector3.zero = không bù.
    private Vector3 _rootToHeadOffset = Vector3.zero;
#if UNITY_EDITOR
    private bool _loggedHeadOffset;
#endif

    void Awake()
    {
        CaptureAuthoredLayout();
        // [KHÓA CẤP 1-4] Nếu chưa đạt Cấp 5, ẩn visual tàu ngay từ Awake
        if (!TrainGateAccess.DuCap)
        {
            HideTrain();
        }
    }

    void Start()
    {
        if (trainRoot == null)
        {
            Debug.LogError($"[TrainPathFollower] {gameObject.name}: trainRoot chưa gán! " +
                           "Kéo TrainVisualRoot (hoặc TrainVisualRoot2) vào field trainRoot.");
            return;
        }

        if (!TrainGateAccess.DuCap)
        {
            HideTrain();
            return;
        }

        ConfigureTrainSorting();
        if (_authoredLocalPositions == null || _authoredLocalPositions.Length == 0)
            CaptureAuthoredLayout();
    }

    void LateUpdate()
    {
        // Giữ nguyên vị trí local đã xếp tay chuẩn xác từng pixel — không can thiệp làm lệch toa
    }

    // ─── Public API ───────────────────────────────────────────────

    /// Hiện toàn bộ visual tàu.
    public void ShowTrain()
    {
        if (trainRoot != null)
        {
            ConfigureTrainSorting();
            trainRoot.gameObject.SetActive(true);
        }
        else
            Debug.LogWarning($"[TrainPathFollower] {gameObject.name}: trainRoot chưa gán!");
    }

    /// Ẩn toàn bộ visual tàu.
    public void HideTrain()
    {
        if (trainRoot != null)
            trainRoot.gameObject.SetActive(false);
        else
            Debug.LogWarning($"[TrainPathFollower] {gameObject.name}: trainRoot chưa gán!");
    }

    /// <summary>
    /// Chụp lại 100% tọa độ local chính xác mà Sếp đã kéo xếp tay trong Scene.
    /// Giữ nguyên vị trí này suốt quá trình chạy game.
    /// </summary>
    public void CaptureAuthoredLayout()
    {
        if (trainRoot == null) return;

        if (!_hasAuthoredRootPosition)
        {
            _authoredRootPosition = trainRoot.position;
            _hasAuthoredRootPosition = true;
        }

        if (carriages == null || carriages.Length == 0)
        {
            var list = new List<Transform>();
            foreach (Transform child in trainRoot)
            {
                if (child != null && child.GetComponent<SpriteRenderer>() != null)
                    list.Add(child);
            }
            if (list.Count > 0) carriages = list.ToArray();
        }

        if (carriages != null && carriages.Length > 0)
        {
            _authoredLocalPositions = new Vector3[carriages.Length];
            for (int i = 0; i < carriages.Length; i++)
            {
                if (carriages[i] != null)
                    _authoredLocalPositions[i] = carriages[i].localPosition;
            }
        }

        RecomputeHeadOffset();
    }

    /// <summary>
    /// Tính lại độ lệch root → đầu tàu, CHỈ từ localPosition tay xếp (_authoredLocalPositions[0]),
    /// không đọc world position hiện thời của toa. Nhờ vậy dù tàu đang đứng ở đâu, dù
    /// CaptureAuthoredLayout() bị gọi lại nhiều lần, kết quả vẫn y hệt — không cộng dồn.
    /// Mọi trường hợp thiếu dữ liệu → Vector3.zero → chạy đúng như code cũ.
    /// </summary>
    private void RecomputeHeadOffset()
    {
        _rootToHeadOffset = Vector3.zero;

        if (trainRoot == null) return;
        if (carriages == null || carriages.Length == 0) return;

        Transform head = carriages[0];
        if (head == null) return;
        if (_authoredLocalPositions == null || _authoredLocalPositions.Length == 0) return;

        // Không gian local mà _authoredLocalPositions[0] thuộc về là parent của đầu tàu.
        Transform space = head.parent != null ? head.parent : trainRoot;
        Vector3 headWorldIfRootHere = space.TransformPoint(_authoredLocalPositions[0]);
        _rootToHeadOffset = headWorldIfRootHere - trainRoot.position;

#if UNITY_EDITOR
        if (!_loggedHeadOffset)
        {
            _loggedHeadOffset = true;
            Debug.Log($"[TrainPathFollower] {gameObject.name} → root '{trainRoot.name}': " +
                      $"độ lệch root→đầu tàu ('{head.name}') = {_rootToHeadOffset:F1} " +
                      $"| canhChinhTheoDauTau = {canhChinhTheoDauTau}", this);
        }
#endif
    }

    /// <summary>
    /// Marker (điểm dừng) → vị trí thật cần đặt cho trainRoot.
    /// [FIX 2026-09-11] Nếu bật snapToAuthoredStationPosition và điểm dừng là ga (gần vị trí gốc lúc Edit Mode),
    /// tàu sẽ giữ ĐÚNG 100% toạ độ ray mà Sếp đã kéo xếp tay, không bao giờ bị lệch sang một bên khi vào Play Mode.
    /// </summary>
    private Vector3 ResolveRootTarget(Vector3 marker)
    {
        Vector3 baseTarget = canhChinhTheoDauTau ? marker - _rootToHeadOffset : marker;

        if (snapToAuthoredStationPosition && _hasAuthoredRootPosition)
        {
            // Điểm đỗ ga (PointStationShip / PointStationReward) nằm gần vị trí authored lúc Edit Mode (< 600 unit)
            if (Vector3.Distance(baseTarget, _authoredRootPosition) < 600f)
            {
                baseTarget = _authoredRootPosition;
            }
        }

        return baseTarget + playModeOffset;
    }

    /// Snap tàu đến pos, giữ nguyên 100% layout tay xếp của đoàn toa.
    public void SnapToPosition(Vector3 pos, Vector3 backwardDir)
    {
        if (trainRoot == null) return;

        trainRoot.position = ResolveRootTarget(pos);
        ConfigureTrainSorting();

        if (carriages != null && _authoredLocalPositions != null)
        {
            for (int i = 0; i < carriages.Length; i++)
            {
                if (carriages[i] != null && i < _authoredLocalPositions.Length)
                {
                    carriages[i].localPosition = _authoredLocalPositions[i];
                }
            }
        }
    }

    /// Di chuyển tới target, gọi onDone khi đến nơi.
    /// Dừng mọi coroutine đang chạy trước khi bắt đầu.
    public void MoveTo(Vector3 target, Action onDone)
    {
        if (trainRoot == null)
        {
            Debug.LogError($"[TrainPathFollower] {gameObject.name}: MoveTo gọi nhưng trainRoot == null!");
            onDone?.Invoke(); // không block flow
            return;
        }

        StopAllCoroutines();
        StartCoroutine(MoveCoroutine(target, onDone));
    }

    // ─── Coroutine ────────────────────────────────────────────────

    private IEnumerator MoveCoroutine(Vector3 target, Action onDone)
    {
        Vector3 rootTarget = ResolveRootTarget(target);

        while (Vector3.Distance(trainRoot.position, rootTarget) > 1f)
        {
            trainRoot.position = Vector3.MoveTowards(
                trainRoot.position, rootTarget, moveSpeed * Time.deltaTime);
            yield return null;
        }

        trainRoot.position = rootTarget;
        onDone?.Invoke();
    }

    private void ConfigureTrainSorting()
    {
        if (trainRoot == null) return;

        SpriteRenderer[] renderers = trainRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            sr.sortingLayerName = TrainSortingLayerName;
            sr.sortingOrder = Mathf.Max(sr.sortingOrder, TrainSortingOrder);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (trainRoot != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(trainRoot.position, 15f);
        }
    }
#endif
}
