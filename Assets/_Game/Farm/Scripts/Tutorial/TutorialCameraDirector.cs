using System.Collections;
using UnityEngine;

/// <summary>
/// ĐẠO DIỄN CAMERA TUTORIAL V2 — zoom có easing thay vì trôi phẳng.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// CHỦ FILE: DEV-ANIM. Không Dev nào khác sửa file này.
///
/// VẤN ĐỀ ĐO ĐƯỢC 04/09:
///   ① `CameraController` lia camera bằng `Vector3.SmoothDamp` + `Mathf.SmoothDamp`
///      (dòng 517-526) với `cinematicSmoothTime = 0.45`. SmoothDamp cho ease-out
///      TỰ NHIÊN nhưng KHỞI ĐỘNG GIẬT: nó lao đi ngay từ frame đầu với vận tốc lớn nhất.
///      Cảm giác "trôi" chứ không phải "máy quay có trọng lượng".
///   ② `TutorialCameraZoom.cs` ghi THẲNG `orthographicSize` từ 8 → 4, trong khi thang
///      zoom thật của dự án là ~460 (`TutorialCameraFocus.DEFAULT_ZOOM = 460f`).
///      ⇒ script đó vừa SAI THANG vừa TRANH CHẤP với CameraController — vốn tự nhận là
///      "chủ duy nhất điều khiển camera". Đây là một phần lý do camera tutorial khựng.
///
/// CÁCH SỬA (KHÔNG viết lại CameraController — chỉ lái nó):
///   Thay vì gọi `CinematicFocus(đích)` một phát rồi để SmoothDamp tự lo, director này
///   NUÔI một cái đích DI ĐỘNG: mỗi frame nó nội suy đích theo `AnimationCurve` ease-in-out
///   rồi mới đưa cho `CinematicFocus`. SmoothDamp bám theo đích mượt ⇒ camera khởi hành nhẹ,
///   tăng tốc giữa chặng, hãm dần khi tới nơi, có overshoot rất nhẹ rồi lùi về.
///   CameraController vẫn là chủ duy nhất — không ai ghi thẳng transform/orthographicSize.
///
/// [TutorialV2]
/// </summary>
[DisallowMultipleComponent]
public class TutorialCameraDirector : MonoBehaviour
{
    [Header("◆ Nhịp lia")]
    [Tooltip("Thời lượng một cú lia + zoom (giây, unscaled). 0.9-1.2 là 'điện ảnh'; " +
             "dưới 0.6 thành giật, trên 1.6 thành lê thê.")]
    [SerializeField] private float focusDuration = 1.0f;

    [Tooltip("Đường cong nội suy. Mặc định EaseInOut — khởi hành nhẹ, hãm dần khi tới.")]
    [SerializeField] private AnimationCurve focusCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Vọt qua đích bao nhiêu % rồi lùi về (0 = không vọt). 3% cho cảm giác máy quay " +
             "có trọng lượng, giống người quay thật hãm hơi trễ.")]
    [Range(0f, 0.12f)]
    [SerializeField] private float overshootRatio = 0.03f;

    [Tooltip("Phần cuối thời lượng dành cho việc lùi về từ điểm vọt (tỉ lệ 0-1).")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float overshootSettleRatio = 0.22f;

    [Header("◆ Trả camera về chỗ cũ")]
    [Tooltip("Thời lượng trả camera về vị trí/zoom trước tutorial.")]
    [SerializeField] private float restoreDuration = 0.8f;

    [Header("◆ Dọn xung đột")]
    [Tooltip("TRUE: tự TẮT component TutorialCameraZoom nếu gặp (nó ghi thẳng orthographicSize " +
             "8→4, sai thang ~460 của dự án và tranh chấp CameraController). " +
             "KHÔNG xoá file, chỉ tắt — bỏ tick là về hành vi cũ.")]
    [SerializeField] private bool tatTutorialCameraZoomCu = true;

    // ═══════════════════════════════════════════════════════════════════════
    private CameraController _cam;
    private Coroutine _routine;

    private Vector3 _viTriGoc;
    private float   _zoomGoc;
    private bool    _daLuuGoc;

    /// <summary>Đang lia camera hay không (TutorialManager hỏi để đừng chồng lệnh).</summary>
    public bool DangLia => _routine != null;

    // ═══════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        LayCameraController();
        DonXungDot();
    }

    private void LayCameraController()
    {
        if (_cam != null) return;
        if (Camera.main != null) _cam = Camera.main.GetComponent<CameraController>();
        if (_cam == null) _cam = FindAnyObjectByType<CameraController>();
    }

    /// <summary>
    /// Tắt <c>TutorialCameraZoom</c> — script cũ ghi thẳng orthographicSize 8→4.
    /// Dùng GetComponent theo TÊN qua reflection-free cách: tìm component cùng scene.
    /// KHÔNG xoá file, chỉ `enabled = false`, để Sếp bật lại được nếu muốn so sánh.
    /// </summary>
    private void DonXungDot()
    {
        if (!tatTutorialCameraZoomCu) return;

        var cu = FindObjectsByType<TutorialCameraZoom>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int tat = 0;
        foreach (var c in cu)
        {
            if (c != null && c.enabled) { c.enabled = false; tat++; }
        }

        if (tat > 0)
            Debug.Log($"[TutorialCameraDirector] Đã TẮT {tat} component TutorialCameraZoom " +
                      "(ghi thẳng orthographicSize 8→4, sai thang ~460 và tranh chấp CameraController). " +
                      "File không bị xoá; bỏ tick 'Tat Tutorial Camera Zoom Cu' là về như cũ.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // API công khai
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Ghi nhớ vị trí/zoom trước khi tutorial đụng vào camera. Gọi 1 lần lúc bắt đầu.</summary>
    public void LuuTrangThaiGoc()
    {
        LayCameraController();
        if (_cam == null || _daLuuGoc) return;

        _viTriGoc = _cam.CurrentPosition;
        _zoomGoc  = _cam.CurrentSize;
        _daLuuGoc = true;
    }

    /// <summary>
    /// Lia + zoom camera tới một điểm world, có easing. <paramref name="orthoSize"/> theo
    /// thang thật của dự án (~460 là mặc định, KHÔNG phải 3-5).
    /// </summary>
    public void FocusTo(Vector3 diemWorld, float orthoSize, float thoiLuong = -1f)
    {
        LayCameraController();
        if (_cam == null)
        {
            Debug.LogWarning("[TutorialCameraDirector] Không tìm thấy CameraController → bỏ qua lệnh lia, " +
                             "tutorial vẫn chạy tiếp bình thường.");
            return;
        }

        LuuTrangThaiGoc();

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ChayLia(diemWorld, orthoSize,
                                          thoiLuong > 0f ? thoiLuong : focusDuration, khoaInput: true));
    }

    /// <summary>Trả camera về đúng vị trí/zoom trước tutorial rồi trả quyền cho người chơi.</summary>
    public void RestoreCamera()
    {
        LayCameraController();
        if (_cam == null) return;

        if (!_daLuuGoc)
        {
            _cam.EndCinematic();
            return;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ChayTraVe());
    }

    /// <summary>Cắt ngang mọi cú lia đang chạy và trả quyền điều khiển ngay lập tức.</summary>
    public void HuyNgay()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        if (_cam != null) _cam.EndCinematic();
    }

    // ═══════════════════════════════════════════════════════════════════════
    private IEnumerator ChayLia(Vector3 dich, float zoomDich, float thoiLuong, bool khoaInput)
    {
        Vector3 batDau     = _cam.CurrentPosition;
        float   zoomBatDau = _cam.CurrentSize;

        float dur = Mathf.Max(0.05f, thoiLuong);

        // Điểm vọt: đi quá đích một chút theo đúng hướng di chuyển, rồi lùi về.
        Vector3 huong  = dich - batDau;
        Vector3 diemVot = dich + huong * overshootRatio;
        float   zoomVot = zoomDich + (zoomDich - zoomBatDau) * overshootRatio;

        float mocLui = Mathf.Clamp01(1f - overshootSettleRatio);
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float r = Mathf.Clamp01(t / dur);
            float e = focusCurve.Evaluate(r);

            Vector3 viTri;
            float   zoom;

            if (overshootRatio <= 0.0001f || r >= mocLui)
            {
                if (overshootRatio <= 0.0001f)
                {
                    viTri = Vector3.LerpUnclamped(batDau, dich, e);
                    zoom  = Mathf.LerpUnclamped(zoomBatDau, zoomDich, e);
                }
                else
                {
                    // Chặng cuối: từ điểm vọt lùi về đúng đích.
                    float r2 = Mathf.InverseLerp(mocLui, 1f, r);
                    float e2 = focusCurve.Evaluate(r2);
                    viTri = Vector3.LerpUnclamped(diemVot, dich, e2);
                    zoom  = Mathf.LerpUnclamped(zoomVot, zoomDich, e2);
                }
            }
            else
            {
                // Chặng đầu: từ chỗ đứng lao tới điểm vọt.
                float r1 = Mathf.InverseLerp(0f, mocLui, r);
                float e1 = focusCurve.Evaluate(r1);
                viTri = Vector3.LerpUnclamped(batDau, diemVot, e1);
                zoom  = Mathf.LerpUnclamped(zoomBatDau, zoomVot, e1);
            }

            // Đưa ĐÍCH DI ĐỘNG cho CameraController — nó vẫn là chủ duy nhất của camera,
            // vẫn SmoothDamp bám theo. Ta chỉ quyết định đích đi đường nào.
            _cam.CinematicFocus(viTri, zoom, khoaInput);
            yield return null;
        }

        _cam.CinematicFocus(dich, zoomDich, khoaInput);
        _routine = null;
    }

    private IEnumerator ChayTraVe()
    {
        Vector3 batDau     = _cam.CurrentPosition;
        float   zoomBatDau = _cam.CurrentSize;
        float   dur        = Mathf.Max(0.05f, restoreDuration);

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float e = focusCurve.Evaluate(Mathf.Clamp01(t / dur));
            _cam.CinematicFocus(Vector3.LerpUnclamped(batDau, _viTriGoc, e),
                                Mathf.LerpUnclamped(zoomBatDau, _zoomGoc, e), true);
            yield return null;
        }

        _cam.CinematicFocus(_viTriGoc, _zoomGoc, false);
        _cam.EndCinematic();   // trả quyền pan/zoom cho người chơi
        _routine = null;
    }
}
