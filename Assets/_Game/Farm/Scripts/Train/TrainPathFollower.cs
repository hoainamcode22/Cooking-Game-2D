using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TrainPathFollower : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Ga tàu — tàu đứng đây chờ chất hàng & thu reward")]
    public Transform point00;
    [Tooltip("Điểm quay đầu — gần ga, trên đường ray")]
    public Transform point01;
    [Tooltip("Cửa hầm / điểm đích cuối")]
    public Transform point02;

    [Header("Đầu tàu")]
    public Transform engineTransform;

    [Header("Toa tàu (theo thứ tự từ đầu tàu)")]
    public Transform[] carriages;

    [Tooltip("Khoảng cách giữa các toa (world units). " +
             "Chỉnh bằng khoảng cách thực tế giữa wagons trong Scene, thường 100–250.")]
    public float carriageSpacing = 150f;

    [Header("Tốc độ")]
    public float moveSpeed = 300f;

    [Header("Flip sprite khi đổi chiều")]
    public bool autoFlip = true;

    // ─── Callbacks cho TrainManager ──────────────────────────────
    /// Gọi khi tàu tới Point_02 (cửa hầm, sau khi Depart xong)
    public Action onArrivedAtPoint00;
    /// Gọi khi tàu về lại Point_00 (ga, sau Return)
    public Action onArrivedAtPoint01AfterReturn;
    /// Gọi khi tàu hoàn thành ResetMove (Point_00 → Point_01 → Point_00)
    public Action onResetMoveDone;

    // ─── Internal ────────────────────────────────────────────────
    private readonly List<Vector3> _pathHistory = new List<Vector3>();
    private bool _initialized = false;

    // Engine (Locomotive có Animator) → dùng SpriteRenderer.flipX
    // → không đụng localScale, không bị pivot offset khi flip
    private SpriteRenderer _engineSR;
    private bool           _engineBaseFlipX; // flipX gốc trong Scene

    // Wagon (không có Animator) → dùng localScale.x
    // → child objects (cargo icon) cũng được flip theo
    private float[] _carriageBaseScaleX;


    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        if (engineTransform == null)
        {
            Debug.LogError("[TrainPathFollower] Chưa gán engineTransform!");
            return;
        }

        int numWagons = carriages != null ? carriages.Length : 0;

        // ── Engine: lấy SpriteRenderer để dùng flipX (không đụng localScale) ──
        _engineSR = engineTransform.GetComponentInChildren<SpriteRenderer>();
        if (_engineSR == null)
            Debug.LogWarning("[Train] Không tìm thấy SpriteRenderer trên engineTransform — flip sẽ không hoạt động.");
        _engineBaseFlipX = _engineSR != null && _engineSR.flipX;

        // ── Wagon: lưu base localScale.x để flip (không có Animator) ──
        _carriageBaseScaleX = new float[numWagons];
        if (carriages != null)
            for (int i = 0; i < numWagons; i++)
                _carriageBaseScaleX[i] = carriages[i] != null ? carriages[i].localScale.x : 1f;

        Debug.Log($"[Train] SETUP" +
                  $"\n  point00 (ga)      = {point00?.position}" +
                  $"\n  point01 (quay đầu)= {point01?.position}" +
                  $"\n  point02 (hầm)     = {point02?.position}" +
                  $"\n  engineBaseFlipX   = {_engineBaseFlipX}");

        // ── Pre-fill history bằng đường vật lý thực tế ────────────
        Vector3 engineStart = engineTransform.position;

        var initPath = new List<Vector3>();
        for (int w = numWagons - 1; w >= 0; w--)
            if (carriages[w] != null) initPath.Add(carriages[w].position);
        initPath.Add(engineStart);

        if (initPath.Count > 1)
        {
            _pathHistory.Add(initPath[0]);
            for (int i = 1; i < initPath.Count; i++)
            {
                Vector3 from  = initPath[i - 1];
                Vector3 to    = initPath[i];
                float   dist  = Vector3.Distance(from, to);
                int     steps = Mathf.Max(1, Mathf.CeilToInt(dist));
                for (int s = 1; s <= steps; s++)
                    _pathHistory.Add(Vector3.Lerp(from, to, (float)s / steps));
            }
        }
        else
        {
            _pathHistory.Add(engineStart);
        }

        _initialized = true;
    }

    void LateUpdate()
    {
        if (_initialized) UpdateCarriages();
    }

    // ─── Public API (gọi từ TrainManager) ────────────────────────

    /// Khởi hành: Point_00 (ga) → Point_01 (quay đầu) → Point_02 (hầm)
    public void DepartToProcess()
    {
        StopAllCoroutines();
        StartCoroutine(DepartRoutine());
    }

    /// Về: Point_02 (hầm) → Point_00 (ga)
    public void ReturnToWait()
    {
        StopAllCoroutines();
        StartCoroutine(ReturnRoutine());
    }

    /// Reset: Point_00 (ga) → Point_01 (quay đầu) → Point_00 (ga) — quay đầu tại ga
    public void ResetMove()
    {
        StopAllCoroutines();
        StartCoroutine(ResetRoutine());
    }

    // ─── Coroutines ──────────────────────────────────────────────

    /// Ga → Quay đầu → Hầm
    private IEnumerator DepartRoutine()
    {
        yield return StartCoroutine(MoveToPoint(point01.position)); // quay đầu
        yield return StartCoroutine(MoveToPoint(point02.position)); // vào hầm
        onArrivedAtPoint00?.Invoke(); // "đã đến đích"
    }

    /// Hầm → Ga
    private IEnumerator ReturnRoutine()
    {
        yield return StartCoroutine(MoveToPoint(point00.position)); // về ga
        onArrivedAtPoint01AfterReturn?.Invoke(); // "đã về ga"
    }

    /// Ga → Quay đầu → Ga (chuẩn bị chuyến mới)
    private IEnumerator ResetRoutine()
    {
        yield return StartCoroutine(MoveToPoint(point01.position)); // ra điểm quay đầu
        yield return StartCoroutine(MoveToPoint(point00.position)); // trở về ga
        onResetMoveDone?.Invoke(); // "đã quay đầu xong"
    }

    private IEnumerator MoveToPoint(Vector3 target)
    {
        if (engineTransform == null) yield break;

        Vector3 dir = target - engineTransform.position;

        // Flip ngay khi bắt đầu đoạn mới — truyền dir.x trực tiếp
        if (autoFlip) FlipTrain(dir.x);

        // Reset rotation
        if (engineTransform != null) engineTransform.rotation = Quaternion.identity;
        if (carriages != null)
            foreach (var c in carriages)
                if (c != null) c.rotation = Quaternion.identity;

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

    // ─── Flip toàn bộ đoàn tàu ───────────────────────────────────
    // goingRight=true  → đi về hầm → flipX=true  cho tất cả
    // goingRight=false → đi về ga  → flipX=false cho tất cả
    private void FlipTrain(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;

        bool goingRight = dirX > 0;

        // Engine
        if (_engineSR != null)
            _engineSR.flipX = goingRight;

        // Wagon — lấy SR trực tiếp trên wagon, nếu không có thì lấy child đầu tiên.
        // KHÔNG dùng GetComponentInChildren vì sẽ lấy nhầm cargo icon child.
        if (carriages != null)
        {
            foreach (var wagon in carriages)
            {
                if (wagon == null) continue;
                var sr = wagon.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    foreach (Transform child in wagon)
                    {
                        sr = child.GetComponent<SpriteRenderer>();
                        if (sr != null) break;
                    }
                }
                if (sr != null)
                    sr.flipX = goingRight;
            }
        }
    }

    // ─── Toa bám vết đầu tàu (distance-based) ────────────────────
    private void UpdateCarriages()
    {
        if (carriages == null || _pathHistory.Count < 2) return;

        for (int i = 0; i < carriages.Length; i++)
        {
            if (carriages[i] == null) continue;
            float targetDist      = carriageSpacing * (i + 1);
            carriages[i].position = GetPositionAtDistance(targetDist);
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
                return Vector3.Lerp(_pathHistory[i], _pathHistory[i - 1], t);
            }
        }
        return _pathHistory[0];
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Vẽ đường ray: ga → quay đầu → hầm
        Gizmos.color = Color.cyan;
        if (point00 != null && point01 != null)
            Gizmos.DrawLine(point00.position, point01.position);
        if (point01 != null && point02 != null)
            Gizmos.DrawLine(point01.position, point02.position);

        // Vẽ waypoints
        if (point00 != null) { Gizmos.color = Color.green; Gizmos.DrawSphere(point00.position, 20f); } // ga = xanh lá
        if (point01 != null) { Gizmos.color = Color.yellow; Gizmos.DrawSphere(point01.position, 20f); } // quay đầu = vàng
        if (point02 != null) { Gizmos.color = Color.red;   Gizmos.DrawSphere(point02.position, 20f); } // hầm = đỏ
    }
#endif
}
