using UnityEngine;

/// <summary>
/// Gắn lên MapRoot object.
/// Theo dõi camera và tự động mở rộng bounds khi camera tiến gần mép.
/// Vẽ Gizmos màu vàng để nhìn thấy vùng giới hạn trong Editor.
/// </summary>
public class MapBoundary : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────
    public static MapBoundary Instance { get; private set; }

    // ── Tham số Inspector ────────────────────────────────────────────────
    [Header("Bounds khởi đầu")]
    [SerializeField] private float initialMinX = -5000f;
    [SerializeField] private float initialMaxX =  5000f;
    [SerializeField] private float initialMinY = -5000f;
    [SerializeField] private float initialMaxY =  5000f;

    [Header("Mở rộng tự động")]
    [SerializeField] private float expandThreshold = 500f;  // Ngưỡng khoảng cách tới mép để mở rộng
    [SerializeField] private float expandAmount     = 1000f; // Số units mở rộng mỗi lần

    // ── Biến nội bộ ──────────────────────────────────────────────────────
    private float currentMinX;
    private float currentMaxX;
    private float currentMinY;
    private float currentMaxY;

    private CameraController cameraController; // Tham chiếu đến CameraController trên Main Camera
    private Camera            mainCam;

    // ── [PERF P1] Bo qua LateUpdate khi camera KHONG nhuc nhich ──────────────
    //  LateUpdate cu chay 4 phep so sanh + (khi trung) dung Vector4 moi frame, ke ca luc camera
    //  dung yen (phan lon thoi gian choi). Nay nho lai vi tri + orthographicSize cua frame truoc;
    //  giong het thi return ngay — 0 cong viec.
    private Vector3 _viTriCamTruoc = new Vector3(float.NaN, float.NaN, float.NaN);
    private float   _orthoTruoc    = float.NaN;

    // ────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton: chỉ tồn tại 1 instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Khởi tạo bounds ban đầu
        currentMinX = initialMinX;
        currentMaxX = initialMaxX;
        currentMinY = initialMinY;
        currentMaxY = initialMaxY;
    }

    private void Start()
    {
        // Tìm CameraController trên Main Camera
        mainCam = Camera.main;
        if (mainCam != null)
            cameraController = mainCam.GetComponent<CameraController>();

        // BUG CŨ: `if (cameraController == null)` bị treo, thiếu {} nên nuốt luôn
        // dòng ApplyBounds() bên dưới → bounds CHỈ được áp dụng khi KHÔNG tìm thấy
        // CameraController, tức là gần như không bao giờ chạy. Trước đây không lộ ra
        // vì giá trị trong prefab tình cờ trùng với initialMinX/MaxX/MinY/MaxY.
        if (cameraController == null)
        {
            Debug.LogWarning("[MapBoundary] Không tìm thấy CameraController trên Camera.main — " +
                             "bounds sẽ không được áp dụng.");
            return;
        }

        // Áp dụng bounds khởi đầu cho camera
        ApplyBounds();
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

        Vector3 camPos = mainCam.transform.position;
        float   ortho  = mainCam.orthographicSize;

        // [PERF P1] Camera y nguyen so voi frame truoc => khong the vuot nguong moi => thoat luon.
        if (camPos == _viTriCamTruoc && ortho == _orthoTruoc) return;

        _viTriCamTruoc = camPos;
        _orthoTruoc    = ortho;

        bool    changed = false;

        // [FIX QA] Truoc day moi mep chi no THEM MOT LAN moi khung hinh, va vong lap dua vao
        // viec khung hinh sau camera lai lech di de no tiep. Cong them cua thoat nhanh
        // "camera y nguyen" o tren thi mot cu nhay xa (teleport / SetPosition) chi no duoc
        // DUNG MOT LAN roi thoat vinh vien => bien mai khong bao gio phu toi camera.
        // Doi sang while + tran lap: hoi tu ngay trong khung hinh nay, khong phu thuoc
        // khung hinh sau. Tran LAP_TOI_DA chan treo neu expandAmount <= 0.
        const int LAP_TOI_DA = 64;
        int lap;

        // Kiểm tra mép trái
        lap = 0;
        while (camPos.x - currentMinX < expandThreshold && lap++ < LAP_TOI_DA)
        {
            currentMinX -= expandAmount;
            changed = true;
            if (expandAmount <= 0f) break;
        }
        // Kiểm tra mép phải
        lap = 0;
        while (currentMaxX - camPos.x < expandThreshold && lap++ < LAP_TOI_DA)
        {
            currentMaxX += expandAmount;
            changed = true;
            if (expandAmount <= 0f) break;
        }
        // Kiểm tra mép dưới
        lap = 0;
        while (camPos.y - currentMinY < expandThreshold && lap++ < LAP_TOI_DA)
        {
            currentMinY -= expandAmount;
            changed = true;
            if (expandAmount <= 0f) break;
        }
        // Kiểm tra mép trên
        lap = 0;
        while (currentMaxY - camPos.y < expandThreshold && lap++ < LAP_TOI_DA)
        {
            currentMaxY += expandAmount;
            changed = true;
            if (expandAmount <= 0f) break;
        }

        // Chỉ gọi SetBounds khi có thay đổi thực sự
        if (changed)
            ApplyBounds();
    }

    // ── Gửi bounds mới sang CameraController ────────────────────────────

    private void ApplyBounds()
    {
        if (cameraController != null)
            cameraController.SetBounds(currentMinX, currentMaxX, currentMinY, currentMaxY);
    }

    // ── PUBLIC API ───────────────────────────────────────────────────────

    /// <summary>Lấy bounds hiện tại dưới dạng (minX, maxX, minY, maxY).</summary>
    public Vector4 GetCurrentBounds()
    {
        return new Vector4(currentMinX, currentMaxX, currentMinY, currentMaxY);
    }

    /// <summary>Đặt lại bounds về giá trị khởi đầu trong Inspector.</summary>
    public void ResetBounds()
    {
        currentMinX = initialMinX;
        currentMaxX = initialMaxX;
        currentMinY = initialMinY;
        currentMaxY = initialMaxY;
        ApplyBounds();
    }

    // ── GIZMOS (chỉ hiện trong Editor) ──────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Dùng giá trị khởi đầu nếu chưa chạy (edit mode), ngược lại dùng giá trị thực
        float minX = Application.isPlaying ? currentMinX : initialMinX;
        float maxX = Application.isPlaying ? currentMaxX : initialMaxX;
        float minY = Application.isPlaying ? currentMinY : initialMinY;
        float maxY = Application.isPlaying ? currentMaxY : initialMaxY;

        // Vẽ viền bounds màu vàng
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size   = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);

        // Vẽ vùng ngưỡng expandThreshold màu vàng nhạt
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Vector3 innerCenter = center;
        Vector3 innerSize   = new Vector3(
            Mathf.Max(0f, size.x - expandThreshold * 2f),
            Mathf.Max(0f, size.y - expandThreshold * 2f),
            0f);
        Gizmos.DrawWireCube(innerCenter, innerSize);
    }
#endif
}


