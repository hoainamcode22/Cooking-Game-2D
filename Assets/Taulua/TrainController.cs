using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều khiển tàu hỏa isometric 2D.
/// - KHÔNG thay đổi rotation/scale, chỉ thay đổi position
/// - Toa bám vết đầu tàu qua pathHistory (index offset)
/// - flipX dựa theo flipX gốc trong Scene, không tự ý đổi chiều khi start
/// </summary>
public class TrainController : MonoBehaviour
{
    [Header("Waypoints - đặt theo đường ray")]
    public Transform[] waypoints;

    [Header("Đầu tàu")]
    public Transform engineTransform;

    [Header("Toa tàu")]
    public Transform[] carriages;
    [Tooltip("Số frame lùi trong pathHistory cho mỗi toa. Tăng = toa cách xa hơn.")]
    public float carriageSpacing = 200f;

    [Header("Tốc độ")]
    public float moveSpeed = 300f;

    [Header("Dừng cuối đường")]
    public float waitTimeAtEnd = 600f;

    [Header("Flip hướng")]
    [Tooltip("Tự flip sprite khi đổi chiều X.")]
    public bool autoFlip = true;

    // Lịch sử position đầu tàu mỗi frame — toa lấy index lùi lại
    private readonly List<Vector3> _pathHistory = new();

    private bool _started = false;

    // SpriteRenderer để flipX
    private SpriteRenderer   _engineSR;
    private SpriteRenderer[] _carriageSR;

    // flipX gốc đọc từ Scene (chiều chuẩn khi tàu đi theo hướng _initDirX)
    private bool  _baseFlip;
    // Dấu X của segment đầu tiên: +1 = đi phải, -1 = đi trái
    private float _initDirX;

    // ─────────────────────────────────────────────────────────
    void Start()
    {
        if (!Validate()) return;

        // Lấy SpriteRenderer — KHÔNG thay đổi rotation/scale/position
        _engineSR = engineTransform.GetComponentInChildren<SpriteRenderer>();
        _carriageSR = new SpriteRenderer[carriages.Length];
        for (int i = 0; i < carriages.Length; i++)
            if (carriages[i] != null)
                _carriageSR[i] = carriages[i].GetComponentInChildren<SpriteRenderer>();

        // Đọc flipX gốc từ Scene — đây là trạng thái "đúng" mà user đã đặt
        _baseFlip = _engineSR != null && _engineSR.flipX;

        // Tìm waypoint gần nhất — KHÔNG di chuyển tàu
        int nearest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector3.Distance(engineTransform.position, waypoints[i].position);
            if (d < minDist) { minDist = d; nearest = i; }
        }

        // Ghi nhớ hướng X của segment đầu tiên để dùng làm baseline flip
        int nextIdx = Mathf.Min(nearest + 1, waypoints.Length - 1);
        float dx = waypoints[nextIdx].position.x - waypoints[nearest].position.x;
        _initDirX = dx >= 0f ? 1f : -1f;

        // Pre-fill history để toa không nhảy ngay lúc start
        for (int i = 0; i < 500; i++)
            _pathHistory.Add(engineTransform.position);

        _started = true;
        StartCoroutine(RunLoop(nearest));
    }

    void LateUpdate()
    {
        // Chỉ cập nhật position toa — không làm gì khác
        if (_started) UpdateCarriages();
    }

    // ── Kiểm tra dữ liệu bắt buộc ────────────────────────────
    private bool Validate()
    {
        if (engineTransform == null)
        { Debug.LogError("[Train] Chưa gán Engine Transform!"); return false; }
        if (waypoints == null || waypoints.Length < 2)
        { Debug.LogError("[Train] Cần ít nhất 2 Waypoints!"); return false; }
        for (int i = 0; i < waypoints.Length; i++)
            if (waypoints[i] == null)
            { Debug.LogError($"[Train] Waypoints[{i}] = null!"); return false; }
        return true;
    }

    // ── Vòng lặp: xuôi từ nearest → cuối → đợi → ngược → đợi → lặp ──
    private IEnumerator RunLoop(int startIdx)
    {
        yield return StartCoroutine(MovePath(startIdx, waypoints.Length - 1, 1));

        while (true)
        {
            yield return new WaitForSeconds(waitTimeAtEnd);
            yield return StartCoroutine(MovePath(waypoints.Length - 1, 0, -1));
            yield return new WaitForSeconds(waitTimeAtEnd);
            yield return StartCoroutine(MovePath(0, waypoints.Length - 1, 1));
        }
    }

    // Duyệt waypoints từ from → to theo step (+1 xuôi / -1 ngược)
    private IEnumerator MovePath(int from, int to, int step)
    {
        for (int i = from; i != to + step; i += step)
            yield return StartCoroutine(MoveToWaypoint(waypoints[i].position));
    }

    // Di chuyển đầu tàu đến target — CHỈ thay đổi position, không rotation/scale
    private IEnumerator MoveToWaypoint(Vector3 target)
    {
        Vector3 dir = target - engineTransform.position;

        // Flip 1 lần khi vào segment mới — dựa theo flipX gốc trong Scene
        if (autoFlip && dir.sqrMagnitude > 1f && Mathf.Abs(dir.x) > 0.5f)
        {
            // Nếu đi cùng chiều X ban đầu → giữ flipX gốc, ngược lại → đảo
            float curDirSign = dir.x >= 0f ? 1f : -1f;
            bool flip = Mathf.Approximately(curDirSign, _initDirX) ? _baseFlip : !_baseFlip;
            if (_engineSR != null) _engineSR.flipX = flip;
            foreach (var sr in _carriageSR)
                if (sr != null) sr.flipX = flip;
        }

        // Di chuyển thẳng bằng MoveTowards — không thay đổi gì ngoài position
        while (Vector3.Distance(engineTransform.position, target) > 1f)
        {
            engineTransform.position = Vector3.MoveTowards(
                engineTransform.position, target, moveSpeed * Time.deltaTime);

            _pathHistory.Add(engineTransform.position);
            yield return null;
        }

        engineTransform.position = target;
        _pathHistory.Add(target);
    }

    // Toa lấy position từ history, lùi lại theo carriageSpacing — không rotation, không Lerp
    private void UpdateCarriages()
    {
        if (carriages == null || _pathHistory.Count == 0) return;

        for (int i = 0; i < carriages.Length; i++)
        {
            if (carriages[i] == null) continue;

            int offset = (int)carriageSpacing * (i + 1);
            int idx = Mathf.Max(0, _pathHistory.Count - 1 - offset);
            carriages[i].position = _pathHistory[idx];
        }

        // Dọn history cũ, giữ đủ cho toa xa nhất
        int maxKeep = (int)carriageSpacing * (carriages.Length + 1) + 500;
        while (_pathHistory.Count > maxKeep)
            _pathHistory.RemoveAt(0);
    }

#if UNITY_EDITOR
    // Vẽ đường ray (xanh lá) và waypoints (đỏ) trong Scene view
    void OnDrawGizmos()
    {
        if (waypoints == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Length - 1; i++)
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);

        Gizmos.color = Color.red;
        foreach (var wp in waypoints)
            if (wp != null) Gizmos.DrawSphere(wp.position, 20f);
    }
#endif
}
