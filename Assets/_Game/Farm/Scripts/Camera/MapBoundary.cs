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

        if (cameraController == null)
            Debug.LogWarning("[MapBoundary] Không tìm thấy CameraController trên Main Camera!");

        // Áp dụng bounds khởi đầu cho camera
        ApplyBounds();
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

        Vector3 camPos = mainCam.transform.position;
        bool    changed = false;

        // Kiểm tra mép trái
        if (camPos.x - currentMinX < expandThreshold)
        {
            currentMinX -= expandAmount;
            changed = true;
        }
        // Kiểm tra mép phải
        if (currentMaxX - camPos.x < expandThreshold)
        {
            currentMaxX += expandAmount;
            changed = true;
        }
        // Kiểm tra mép dưới
        if (camPos.y - currentMinY < expandThreshold)
        {
            currentMinY -= expandAmount;
            changed = true;
        }
        // Kiểm tra mép trên
        if (currentMaxY - camPos.y < expandThreshold)
        {
            currentMaxY += expandAmount;
            changed = true;
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


