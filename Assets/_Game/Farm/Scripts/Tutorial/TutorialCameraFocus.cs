using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lia camera đến vùng trọng tâm tutorial (6 ô lúa / chậu hoa).
/// Gắn cùng GameObject với TutorialManager.
///
/// QUAN TRỌNG:
///  - Không tự ghi orthographicSize/position. Toàn bộ đi qua
///    CameraController.CinematicFocus() để camera có 1 chủ duy nhất → hết giật.
///  - Tâm focus tính theo TÂM NHÌN THẤY của ô (collider/renderer bounds center),
///    KHÔNG dùng transform.position vì gốc transform nằm dưới đáy tile → lệch.
///
/// TutorialManager gọi:
///   _cameraFocus.FocusOnRice(bridge)    — khi tới bước trồng lúa (L1L2_04)
///   _cameraFocus.FocusOnFlower(bridge)  — khi chuyển sang phase hoa
///   _cameraFocus.RestoreCamera()        — khi tutorial kết thúc
/// </summary>
public class TutorialCameraFocus : MonoBehaviour
{
    [Tooltip("Để trống → tự lấy CameraController trên Camera.main")]
    [SerializeField] private CameraController _camController;

    [Header("Zoom sizes (theo scale thật: default 750 / min 400 / max 1500)")]
    [Tooltip("OrthoSize khi focus vào 6 ô lúa (nhỏ hơn = zoom gần hơn)")]
    [SerializeField] private float _riceZoom   = 460f;
    [Tooltip("OrthoSize khi focus vào chậu hoa")]
    [SerializeField] private float _flowerZoom = 460f;

    // Giá trị scale cũ (vd 3.5) có thể còn sót trong Inspector → nếu < ngưỡng này thì bỏ qua.
    private const float MIN_VALID_ZOOM = 50f;
    private const float DEFAULT_ZOOM   = 460f;

    // Trạng thái gốc trước khi tutorial focus (để restore khi kết thúc)
    private Vector3 _originalPosition;
    private float   _originalZoom;
    private bool    _savedOriginal;

    // =========================================================================
    void Awake()
    {
        if (_camController == null && Camera.main != null)
            _camController = Camera.main.GetComponent<CameraController>();
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Lia camera vào trung tâm 6 ô lúa (theo tâm nhìn thấy) và zoom hợp lý.</summary>
    public void FocusOnRice(TutorialStepTriggerBridge bridge)
    {
        // Center theo TẤT CẢ ô Normal (8 ô) để khung hình + mask khớp với tay quét.
        Vector3 center = AverageVisualCenter(FindPlots(PlotCategory.Normal));

        if (center == Vector3.zero && bridge != null)
            center = bridge.GetRicePlotsWorldCenter();

        if (center == Vector3.zero)
        {
            Debug.LogWarning("[TutorialCameraFocus] Rice plots not found — skipping focus.");
            return;
        }

        SaveOriginal();
        float zoom = SanitizeZoom(_riceZoom);
        Focus(center, zoom);
        Debug.Log($"[TutorialCameraFocus] FocusOnRice center={center} zoom={zoom}");
    }

    /// <summary>Lia camera vào trung tâm các chậu hoa.</summary>
    public void FocusOnFlower(TutorialStepTriggerBridge bridge)
    {
        // Center theo TẤT CẢ chậu hoa (6 chậu) — không chỉ 1 chậu như trước.
        Vector3 center = AverageVisualCenter(FindPlots(PlotCategory.Flower));

        if (center == Vector3.zero && bridge != null)
            center = bridge.GetFlowerPotsWorldCenter();

        if (center == Vector3.zero)
        {
            Debug.LogWarning("[TutorialCameraFocus] Flower pots not found — skipping focus.");
            return;
        }
        SaveOriginal();
        float zoom = SanitizeZoom(_flowerZoom);
        Focus(center, zoom);
        Debug.Log($"[TutorialCameraFocus] Focus on flower pots center: {center} zoom={zoom}");
    }

    /// <summary>
    /// Lia camera vào 1 chuồng theo tâm collider/renderer.
    /// Mặc định là chuồng tutorial — khai ở <see cref="TutorialManager.TenChuongTutorial"/>.
    /// </summary>
    public void FocusOnPen(string penName = TutorialManager.TenChuongTutorial)
    {
        GameObject pen = TimChuong(penName);
        if (pen == null)
        {
            Debug.LogWarning($"[TutorialCameraFocus] Không tìm thấy '{penName}' trong scene " +
                             $"(đã dò cả bản '(Clone)' và object đang tắt).");
            return;
        }
        SaveOriginal();
        float zoom = SanitizeZoom(_flowerZoom);
        Focus(PlotVisualCenter(pen.transform), zoom);
        Debug.Log($"[TutorialCameraFocus] FocusOnPen '{penName}' zoom={zoom}");
    }

    /// <summary>
    /// Dò chuồng trong scene. `GameObject.Find` KHÔNG thấy object đang tắt, mà chuồng
    /// hoàn toàn có thể đang tắt lúc tutorial gọi tới (chưa mở khoá, hoặc đang ở Edit
    /// Mode) — lúc đó camera lặng lẽ không lia và tutorial treo ở bước này.
    /// Cũng thử cả tên có hậu tố `(Clone)` cho chuồng do người chơi mua.
    /// </summary>
    private static GameObject TimChuong(string penName)
    {
        if (string.IsNullOrEmpty(penName)) return null;

        GameObject g = GameObject.Find(penName);
        if (g != null) return g;

        g = GameObject.Find(penName + "(Clone)");
        if (g != null) return g;

        string clone = penName + "(Clone)";
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            if (t.name == penName || t.name == clone) return t.gameObject;
        }

        return null;
    }

    /// <summary>Trả camera về vị trí/zoom ban đầu và trả input cho người chơi.</summary>
    public void RestoreCamera()
    {
        if (_camController == null) return;

        // [V2] Director có thể đang chạy dở một cú lia (tới 1s). Nếu ta đặt đích rồi
        // EndCinematic() ngay, frame sau nó GHI ĐÈ đích VÀ bật lại _cinematicActive = true
        // — rồi không ai tắt nữa ⇒ camera đứng ở điểm tutorial và người chơi bị KHOÁ
        // pan/zoom VĨNH VIỄN. Phải cắt nó trước (QA vòng 2, mục D).
        if (_v2Director != null && _v2Director.isActiveAndEnabled)
        {
            _v2Director.HuyNgay();
        }

        if (_savedOriginal)
            _camController.CinematicFocus(_originalPosition, _originalZoom, false);

        _camController.EndCinematic();
        Debug.Log("[TutorialCameraFocus] Restoring camera + trả input cho người chơi.");
    }

    // =========================================================================
    // Internal
    // =========================================================================
    private void Focus(Vector3 worldPos, float zoom)
    {
        if (_camController == null)
        {
            Debug.LogWarning("[TutorialCameraFocus] Không tìm thấy CameraController trên Camera.main.");
            return;
        }

        // ── [V2 2026-09-04] Zoom mượt ────────────────────────────────────────
        // CinematicFocus() đặt ĐÍCH một phát rồi để SmoothDamp của CameraController tự bò
        // tới. SmoothDamp ease-out tự nhiên nhưng KHỞI ĐỘNG GIẬT: frame đầu đã lao đi với
        // vận tốc lớn nhất ⇒ cảm giác "trôi" chứ không phải máy quay có trọng lượng.
        //
        // Có TutorialCameraDirector trong scene thì giao cho nó: nó nuôi một ĐÍCH DI ĐỘNG
        // theo AnimationCurve ease-in-out + overshoot nhẹ 3%, nên camera khởi hành êm,
        // tăng tốc giữa chặng, hãm dần khi tới. CameraController vẫn là chủ duy nhất của
        // camera — không ai ghi thẳng transform/orthographicSize.
        //
        // KHÔNG có director ⇒ rơi về đúng đường cũ, không đổi hành vi một chút nào.
        // Sửa ở ĐÂY (1 chỗ) thay vì 5 chỗ gọi FocusOnRice/Flower/Pen trong TutorialManager.
        // Exclude (KHÔNG Include): director nằm trên GameObject đang TẮT thì StartCoroutine
        // trong FocusTo không chạy được ⇒ camera đứng im mà cũng đã return, không rơi về
        // đường cũ. Thà không thấy nó còn hơn thấy một cái chạy không được (QA vòng 2).
        if (_v2Director == null)
            _v2Director = FindAnyObjectByType<TutorialCameraDirector>(FindObjectsInactive.Exclude);

        // isActiveAndEnabled: bỏ tick _useV2Dialogue ⇒ TutorialManager tắt component này
        // ⇒ ở đây tự rơi về đường cũ. Đó là cách "về bản cũ 100%" thật sự có hiệu lực.
        if (_v2Director != null && _v2Director.isActiveAndEnabled)
        {
            _v2Director.FocusTo(worldPos, zoom);
            return;
        }

        _camController.CinematicFocus(worldPos, zoom, true);
    }

    /// <summary>[V2] Đạo diễn camera mới. Null ⇒ dùng đường cũ. Dò một lần rồi nhớ.</summary>
    private TutorialCameraDirector _v2Director;

    private static float SanitizeZoom(float z) => z < MIN_VALID_ZOOM ? DEFAULT_ZOOM : z;

    /// <summary>Trung bình tâm-nhìn-thấy của danh sách ô. Vector3.zero nếu rỗng.</summary>
    private static Vector3 AverageVisualCenter(List<Transform> plots)
    {
        if (plots == null || plots.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero; int n = 0;
        foreach (var t in plots)
        {
            if (t == null) continue;
            sum += PlotVisualCenter(t);
            n++;
        }
        return n > 0 ? sum / n : Vector3.zero;
    }

    /// <summary>Tìm mọi PlotController transform theo category.</summary>
    private static List<Transform> FindPlots(PlotCategory cat)
    {
        var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        var list = new List<Transform>();
        foreach (var p in all) if (p != null && p.Category == cat) list.Add(p.transform);
        return list;
    }

    /// <summary>Tâm nhìn thấy của 1 ô = tâm collider/renderer (đã gồm offset+scale).</summary>
    private static Vector3 PlotVisualCenter(Transform t)
    {
        if (t == null) return Vector3.zero;
        var col = t.GetComponent<Collider2D>();
        if (col != null) return col.bounds.center;
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.center;
        return t.position;
    }

    private void SaveOriginal()
    {
        if (_savedOriginal || _camController == null) return;
        _originalPosition = _camController.CurrentPosition;
        _originalZoom     = _camController.CurrentSize;
        _savedOriginal    = true;
    }
}
