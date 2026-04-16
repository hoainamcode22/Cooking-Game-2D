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

    // ─── Internal ────────────────────────────────────────────────
    private readonly List<Vector3> _pathHistory = new List<Vector3>();

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        if (trainRoot == null)
        {
            Debug.LogError($"[TrainPathFollower] {gameObject.name}: trainRoot chưa gán! " +
                           "Kéo TrainVisualRoot (hoặc TrainVisualRoot2) vào field trainRoot.");
            return;
        }

        // Pre-fill history từ vị trí vật lý hiện tại của trainRoot.
        // Sẽ bị ghi đè bởi SnapToPosition trong InitAfterFrame.
        _pathHistory.Add(trainRoot.position);
    }

    void LateUpdate()
    {
        if (trainRoot == null) return;
        UpdateCarriages();
    }

    // ─── Public API ───────────────────────────────────────────────

    /// Hiện toàn bộ visual tàu.
    public void ShowTrain()
    {
        if (trainRoot != null)
            trainRoot.gameObject.SetActive(true);
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

    /// Snap tàu đến pos, trải wagons theo backwardDir.
    /// backwardDir = chiều NGƯỢC với hướng tàu sẽ chạy tiếp.
    /// Gọi trước ShowTrain + MoveTo.
    public void SnapToPosition(Vector3 pos, Vector3 backwardDir)
    {
        if (trainRoot == null) return;

        float mag = backwardDir.magnitude;
        backwardDir = mag > 0.001f ? backwardDir / mag : Vector3.left;

        // Di chuyển trainRoot đến pos
        trainRoot.position = pos;

        // Trải wagons theo backwardDir
        int numWagons = carriages != null ? carriages.Length : 0;
        if (carriages != null)
            for (int i = 0; i < numWagons; i++)
                if (carriages[i] != null)
                {
                    float dist = locomotiveSpacing + carriageSpacing * i;
                    carriages[i].position = pos + backwardDir * dist;
                }

        // Rebuild path history: đường thẳng từ xa nhất → pos
        _pathHistory.Clear();
        float totalLen = locomotiveSpacing + carriageSpacing * numWagons;
        int   steps    = Mathf.Max(1, Mathf.CeilToInt(totalLen));
        for (int i = steps; i >= 0; i--)
            _pathHistory.Add(pos + backwardDir * i);
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
            _pathHistory.Add(trainRoot.position);
            yield return null;
        }

        trainRoot.position = target;
        _pathHistory.Add(target);

        onDone?.Invoke();
    }

    // ─── Wagons bám theo path ─────────────────────────────────────

    private void UpdateCarriages()
    {
        if (carriages == null || _pathHistory.Count < 2) return;

        for (int i = 0; i < carriages.Length; i++)
        {
            if (carriages[i] == null) continue;
            // carriages[0] (Locomotive) cách trainRoot = locomotiveSpacing
            // carriages[1+] (Wagons) cách trainRoot = locomotiveSpacing + carriageSpacing * i
            float dist = locomotiveSpacing + carriageSpacing * i;
            carriages[i].position = GetPositionAtDistance(dist);
        }

        while (_pathHistory.Count > 2000)
            _pathHistory.RemoveAt(0);
    }

    private Vector3 GetPositionAtDistance(float targetDist)
    {
        float accumulated = 0f;
        for (int i = _pathHistory.Count - 1; i > 0; i--)
        {
            float d = Vector3.Distance(_pathHistory[i], _pathHistory[i - 1]);
            accumulated += d;
            if (accumulated >= targetDist)
            {
                float t = (accumulated - targetDist) / d;
                return Vector3.Lerp(_pathHistory[i - 1], _pathHistory[i], t);
            }
        }
        return _pathHistory[0];
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
