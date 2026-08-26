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

    private Vector3[] _authoredLocalPositions;

    void Awake()
    {
        CaptureAuthoredLayout();
    }

    void Start()
    {
        if (trainRoot == null)
        {
            Debug.LogError($"[TrainPathFollower] {gameObject.name}: trainRoot chưa gán! " +
                           "Kéo TrainVisualRoot (hoặc TrainVisualRoot2) vào field trainRoot.");
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
    }

    /// Snap tàu đến pos, giữ nguyên 100% layout tay xếp của đoàn toa.
    public void SnapToPosition(Vector3 pos, Vector3 backwardDir)
    {
        if (trainRoot == null) return;

        trainRoot.position = pos;
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
        while (Vector3.Distance(trainRoot.position, target) > 1f)
        {
            trainRoot.position = Vector3.MoveTowards(
                trainRoot.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        trainRoot.position = target;
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
