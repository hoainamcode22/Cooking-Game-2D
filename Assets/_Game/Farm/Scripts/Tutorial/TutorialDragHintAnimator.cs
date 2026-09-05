using System.Collections;
using UnityEngine;

/// <summary>
/// Animate bàn tay theo chuyển động "kéo" từ target này sang target khác, lặp vòng.
///
/// TutorialManager gọi StartDragHint / StopDragHint tại mỗi bước tutorial.
/// Tự lấy RectTransform từ TutorialManager.HandPointerRT nếu không được gán.
///
/// Không phá drag logic thật — chỉ di chuyển UI hand pointer.
/// </summary>
public class TutorialDragHintAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float _travelDuration = 0.55f;
    [SerializeField] private float _loopInterval   = 0.6f;
    [SerializeField] private AnimationCurve _ease  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _tapScale        = 0.82f;

    [Tooltip("Vị trí ĐẦU NGÓN TAY trên ảnh hand (0-1). tutorial_hand trỏ XUỐNG → đầu ngón ~ (0.36, 0.1).")]
    [SerializeField] private Vector2 _fingertipNormalized = new Vector2(0.36f, 0.1f);

    [SerializeField] private RectTransform _hand;
    private Coroutine     _loop;
    private string        _fromId;
    private string        _toId;
    private static readonly Vector3[] _cornerBuf = new Vector3[4];

    public bool IsRunning => _loop != null;

    /// <summary>[V6] Object tay kéo đang dùng (chỉ-đọc) — để nơi khác biết nó có
    /// trùng với tay tĩnh của TutorialManager hay không, và để F10 in tên ra báo cáo.</summary>
    public RectTransform TayKeoRT => _hand;

    // [V6] Cờ chống dội: StopDragHint được gọi TỪ BÊN TRONG StartDragHint (dọn vòng cũ)
    // thì không được nhả quyền / không được gọi ngược TutorialManager bật lại tay tĩnh.
    private bool _dangKhoiDongLai;

    // =========================================================================
    void Start()
    {
        // Lazy-resolve hand pointer from TutorialManager on same GO
        if (_hand == null)
        {
            var mgr = GetComponent<TutorialManager>();
            if (mgr != null) _hand = mgr.HandPointerRT;
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void StartDragHint(string fromTargetId, string toTargetId)
    {
        // Resolve hand pointer lazily
        if (_hand == null)
        {
            var compMgr = GetComponent<TutorialManager>();
            if (compMgr != null) _hand = compMgr.HandPointerRT;
        }

        if (_hand == null)
        {
            Debug.LogWarning("[TutorialDragHint] Hand pointer not found on TutorialManager — cannot animate.");
            return;
        }

        // Already running same hint? No-op.
        if (_fromId == fromTargetId && _toId == toTargetId && _loop != null) return;

        // [V6] MỘT TAY MỘT LÚC — giành quyền TRƯỚC, rồi mới dọn vòng cũ.
        // Đặt cờ để StopDragHint bên dưới không nhả lại quyền vừa giành.
        _dangKhoiDongLai = true;
        try
        {
            TutorialHandBus.Nhan(LoaiTay.TayKeo);
            StopDragHint();
        }
        finally
        {
            _dangKhoiDongLai = false;
        }

        // Ẩn tay tĩnh + tắt tay hành động để chỉ còn ĐÚNG MỘT bàn tay trên màn hình.
        // TutorialManager tự bỏ qua nếu tay tĩnh CHÍNH LÀ object này (Inspector bỏ trống).
        var tutMgr = TutorialManager.Instance;
        if (tutMgr != null)
        {
            tutMgr.AnTayTinh();
            tutMgr.DungTayHanhDong();
        }

        _fromId = fromTargetId;
        _toId   = toTargetId;
        _loop   = StartCoroutine(DragLoop());
        Debug.Log($"[TutorialDragHint] Start: '{fromTargetId}' → '{toTargetId}'");
    }

    public void StopDragHint()
    {
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
        if (_hand != null) _hand.gameObject.SetActive(false);

        if (_dangKhoiDongLai) return;   // [V6] đang khởi động lại chính mình → chưa nhả

        // [V6] Nhả quyền rồi báo TutorialManager xem bước hiện tại có còn cần tay tĩnh không.
        // Không có bước này thì bước "kéo xong" sẽ còn 0 bàn tay — người chơi mất chỉ dẫn.
        TutorialHandBus.Nha(LoaiTay.TayKeo);
        TutorialManager.Instance?.CapNhatLaiTayTinh();
    }

    // =========================================================================
    // Animation
    // =========================================================================

    private IEnumerator DragLoop()
    {
        var waitRetry = new WaitForSeconds(_loopInterval);
        bool logged = false;

        while (true)
        {
            RectTransform fromRT = TutorialManager.GetTargetRect(_fromId);
            RectTransform toRT   = TutorialManager.GetTargetRect(_toId);

            if (fromRT == null)
            {
                Debug.LogWarning($"[TutorialDragHint] '{_fromId}' not registered yet — retrying...");
                yield return waitRetry;
                continue;
            }
            if (toRT == null)
            {
                Debug.LogWarning($"[TutorialDragHint] '{_toId}' not registered yet — retrying...");
                yield return waitRetry;
                continue;
            }

            _hand.gameObject.SetActive(true);

            // Tâm 2 item quy về TOẠ ĐỘ MÀN HÌNH (chuẩn cho mọi loại canvas).
            Vector2 fromS = TargetScreen(fromRT);
            Vector2 toS   = TargetScreen(toRT);

            if (!logged)
            {
                Debug.Log($"[TutorialDragHint] from '{_fromId}'=({fromRT.name}) screen={fromS}  →  to '{_toId}'=({toRT.name}) screen={toS}");
                logged = true;
            }

            // 1. Đặt ĐẦU NGÓN TAY lên item nguồn (ô lúa)
            _hand.localScale = Vector3.one;
            PlaceHandFingertipAtScreen(fromS);

            // 2. Press tap
            yield return ScaleHand(Vector3.one, new Vector3(_tapScale, _tapScale, 1f), 0.08f);

            // 3. Drag to TO (đầu ngón tay bám theo)
            yield return MoveHand(fromS, toS, _travelDuration);

            // 4. Release
            yield return ScaleHand(new Vector3(_tapScale, _tapScale, 1f), Vector3.one, 0.08f);

            // 5. Snap về nguồn, GIỮ HIỆN (không chớp tắt), chờ rồi lặp
            _hand.localScale = Vector3.one;
            PlaceHandFingertipAtScreen(fromS);
            yield return waitRetry;
        }
    }

    private IEnumerator MoveHand(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = _ease.Evaluate(Mathf.Clamp01(elapsed / duration));
            PlaceHandFingertipAtScreen(Vector2.Lerp(from, to, t));
            yield return null;
        }
        PlaceHandFingertipAtScreen(to);
    }

    /// <summary>Tâm hình học của 1 RectTransform quy về TOẠ ĐỘ MÀN HÌNH (px), dùng camera đúng canvas.</summary>
    private static Vector2 TargetScreen(RectTransform rt)
    {
        rt.GetWorldCorners(_cornerBuf);
        Vector3 worldCenter = (_cornerBuf[0] + _cornerBuf[2]) * 0.5f;
        Canvas c = rt.GetComponentInParent<Canvas>();
        Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;
        return RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
    }

    /// <summary>Đặt hand sao cho ĐẦU NGÓN TAY trùng 1 điểm trên MÀN HÌNH — convert sang local của
    /// parent hand bằng camera của canvas hand (đúng cho overlay/camera/world).</summary>
    private void PlaceHandFingertipAtScreen(Vector2 screen)
    {
        RectTransform parent = _hand.parent as RectTransform;
        if (parent == null) return;

        Canvas hc = _hand.GetComponentInParent<Canvas>();
        Camera hcam = (hc != null && hc.renderMode != RenderMode.ScreenSpaceOverlay) ? hc.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, hcam, out Vector2 local))
            return;

        Rect r = _hand.rect;
        Vector2 fingerFromPivot = new Vector2(
            (_fingertipNormalized.x - _hand.pivot.x) * r.width,
            (_fingertipNormalized.y - _hand.pivot.y) * r.height);
        Vector2 scaled = new Vector2(
            fingerFromPivot.x * _hand.localScale.x,
            fingerFromPivot.y * _hand.localScale.y);
        _hand.anchoredPosition = local - scaled;   // đầu ngón tay về đúng tâm item
    }

    private IEnumerator ScaleHand(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _hand.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _hand.localScale = to;
    }
}
