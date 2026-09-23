using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sickle harvest drag tool.
/// Di chuyá»ƒn theo cursor má»—i frame, dÃ¹ng Linecast thay vÃ¬ OnTriggerEnter2D
/// Ä‘á»ƒ khÃ´ng miss Ã´ khi kÃ©o nhanh.
/// </summary>
public class SickleController : MonoBehaviour
{
    [Header("Layer")]
    [SerializeField] private LayerMask plotLayerMask = ~0;

    [Header("Feel")]
    [Tooltip("Tốc độ follow cursor. Cao = snappy, thấp = lag nhẹ như Hay Day.")]
    [SerializeField] private float followSpeed = 28f;

    [Header("Visual & Sorting")]
    [SerializeField] private float sickleScale = 12.5f;
    [SerializeField] private string targetSortingLayer = "Foreground";
    [SerializeField] private int targetSortingOrder = 30000;

    [Header("Scale theo zoom")]
    [Tooltip("Ortho camera mà tại đó liềm đúng bằng Sickle Scale. Zoom in (ortho nhỏ) liềm " +
             "nhỏ lại theo tỉ lệ ortho/thamChieu, zoom out to lên — kích thước TRÊN MÀN HÌNH " +
             "giữ ổn định. Kẹp [0.5x, 2x]. Mặc định 750 = CameraController.defaultSize.")]
    [SerializeField] private float orthoThamChieu = ZoomScaleHelper.ORTHO_THAM_CHIEU;

    // Ortho lần áp scale gần nhất — LateUpdate chỉ tính lại khi lệch > 0.5.
    private float _orthoDaApDung = float.NegativeInfinity;

    private Camera mainCam;
    private bool isDragging;
    private int enabledFrame; // guard: không nhận release ngay frame enable

    // Cursor world position frame trước — dùng linecast để không miss khi kéo nhanh
    private Vector3 prevCursorWorld;

    // HashSet chống harvest trùng trong cùng 1 lần kéo
    private readonly HashSet<PlotController> harvestedThisDrag = new HashSet<PlotController>();

    // Cache để tránh allocate mỗi frame khi kiểm tra Canvas UI
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
    private PointerEventData _uiPointerEventData;

    // Static buffers để tránh GC Alloc khi raycast/linecast
    private static readonly RaycastHit2D[] _linecastHits = new RaycastHit2D[16];
    private static readonly Collider2D[] _overlapHits = new Collider2D[8];

    public void EnforceVisualAndSorting()
    {
        ApDungScaleTheoZoom();
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = targetSortingLayer;
            sr.sortingOrder = targetSortingOrder;
        }
    }

    /// <summary>
    /// localScale = sickleScale × (ortho / orthoThamChieu), hệ số kẹp [0.5, 2]. Không alloc.
    /// </summary>
    private void ApDungScaleTheoZoom()
    {
        float heSo  = ZoomScaleHelper.HeSo(mainCam, orthoThamChieu);
        float baseScale = Mathf.Max(sickleScale, 12f);
        float scale = Mathf.Clamp(baseScale, 0.1f, 30f) * heSo;
        transform.localScale = new Vector3(scale, scale, 1f);
        _orthoDaApDung = (mainCam != null) ? mainCam.orthographicSize : float.NegativeInfinity;
    }

    // Chỉ chạy khi liềm đang bật (đang gặt). Rẻ: 1 phép so; chỉ set scale khi ortho đổi > 0.5.
    private void LateUpdate()
    {
        if (mainCam == null) return;
        if (Mathf.Abs(mainCam.orthographicSize - _orthoDaApDung) <= 0.5f) return;
        ApDungScaleTheoZoom();
    }

    private void Awake()
    {
        mainCam = Camera.main;
        EnforceVisualAndSorting();

        // Nếu còn Rigidbody2D từ version cũ → tắt simulation để không conflict với transform.position
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;
    }

    private void OnEnable()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        EnforceVisualAndSorting();

        // isDragging=false khi tray hiện — chỉ set true khi user thật sự kéo liềm (BeginHarvestMode)
        isDragging   = false;
        enabledFrame = Time.frameCount;
        harvestedThisDrag.Clear();
    }

    private void OnDisable()
    {
        FarmInputLock.IsDraggingSickle = false;
        harvestedThisDrag.Clear();
    }

    // FarmUIManager gọi khi plot Ready được click
    public void BeginHarvestMode(Vector3 startWorldPos)
    {
        if (mainCam == null)
            mainCam = Camera.main;

        EnforceVisualAndSorting();

        startWorldPos.z    = transform.position.z;
        transform.position = startWorldPos;
        prevCursorWorld    = startWorldPos;
        harvestedThisDrag.Clear();
        gameObject.SetActive(true);  // kích hoạt object (OnEnable set isDragging=false)
        isDragging                     = true;  // override OnEnable ngay sau
        enabledFrame                   = Time.frameCount;
        FarmInputLock.IsDraggingSickle = true;
    }

    public void EndHarvestMode()
    {
        isDragging = false;
        FarmInputLock.IsDraggingSickle = false;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        gameObject.SetActive(false); // ẩn SickleTool world object sau khi gặt xong
    }

    private void Update()
    {
        if (!isDragging || mainCam == null)
            return;

        Vector3 cursorWorld = GetCursorWorldPos();

        // Visual: sickle follow cursor mượt — không snap cứng
        transform.position = Vector3.Lerp(transform.position, cursorWorld, followSpeed * Time.deltaTime);

        // Detection: trace cursor path mỗi frame để không miss ô khi kéo nhanh
        // Không check khi cursor đang trên UI, nhưng KHÔNG dừng drag state
        if (!IsPointerOverUI())
            CheckHarvestPath(prevCursorWorld, cursorWorld);

        prevCursorWorld = cursorWorld;

        // Mouse release → kết thúc drag
        if (IsPointerReleased() && Time.frameCount != enabledFrame)
        {
            FarmUIManager.Instance?.HideSickleTool();
        }
    }

    // Linecast từ vị trí frame trước → frame hiện tại.
    // Khi kéo nhanh, cursor nhảy xa → linecast bắt hết plot ở giữa.
    private void CheckHarvestPath(Vector3 from, Vector3 to)
    {
        float distSq = (to - from).sqrMagnitude;

        if (distSq < 0.0001f)
        {
            // Đứng yên — check overlap tại điểm hiện tại
            int overlapCount = Physics2D.OverlapPointNonAlloc(to, _overlapHits, plotLayerMask);
            for (int i = 0; i < overlapCount; i++)
            {
                if (_overlapHits[i] != null)
                    TryHarvest(_overlapHits[i]);
            }
            return;
        }

        // Di chuyển — linecast để không bỏ sót ô ở giữa
        int hitCount = Physics2D.LinecastNonAlloc(from, to, _linecastHits, plotLayerMask);
        for (int i = 0; i < hitCount; i++)
        {
            if (_linecastHits[i].collider != null)
                TryHarvest(_linecastHits[i].collider);
        }
    }

    private void TryHarvest(Collider2D col)
    {
        if (col == null)
            return;

        PlotController plot = col.GetComponent<PlotController>()
                           ?? col.GetComponentInParent<PlotController>();
        if (plot == null)
            return;

        // ÄÃ£ harvest plot nÃ y trong drag hiá»‡n táº¡i â†’ skip
        if (harvestedThisDrag.Contains(plot))
            return;

        if (!plot.IsReadyToHarvest())
        {
            return;
        }

        string cropName = plot.CurrentCrop?.displayName ?? "Nông sản";

        if (plot.Harvest())
        {
            harvestedThisDrag.Add(plot);
            FarmManager.Instance?.OnPlotHarvested(plot, cropName);

            // C8 — chỗ này trước đây gọi `QuestManager.Instance.OnItemHarvested(...)`.
            // `QuestManager` đã xoá sạch (hệ nhiệm vụ thứ hai, 0 instance trong mọi scene,
            // `CheckQuestCompletion` còn ghi `// TODO: Give rewards` ⇒ thưởng rơi vào hư không).
            //
            // KHÔNG cần thay bằng `MissionProgressTracker.ReportEvent(HarvestItem, ...)` ở
            // đây: `PlotController.Harvest()` đã tự báo rồi. Thêm lời gọi thứ hai là mỗi lần
            // quét liềm cộng tiến độ HAI lần, nhiệm vụ "thu hoạch 20 lúa" xong khi mới 10.
            // Ngoài ra id cũ dùng `CurrentCrop.name` (TÊN ASSET, ví dụ "Crop_Rice") chứ không
            // phải `harvestItemId` ("rice") — có gọi cũng không khớp khoá nhiệm vụ nào.
        }
        else
        {
        }
    }

    private Vector3 GetCursorWorldPos()
    {
        Vector2 screenPos;

        if (Pointer.current != null)
            screenPos = Pointer.current.position.ReadValue();
        else
            screenPos = Input.mousePosition;

        float depth = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        Vector3 world = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        world.z = transform.position.z;
        return world;
    }

    private bool IsPointerReleased()
    {
        if (Pointer.current != null)
            return !Pointer.current.press.isPressed;

        return !Input.GetMouseButton(0);
    }

    /// <summary>
    /// Tráº£ vá» true chá»‰ khi con trá» Ä‘ang náº±m trÃªn Canvas UI (panel, buttonâ€¦).
    /// KHÃ”NG tráº£ vá» true khi hover lÃªn world collider (plot, building) â€” trÃ¡nh
    /// cháº·n nháº§m khi lÆ°á»¡i liá»m di chuyá»ƒn qua Ã´ lÃºa cÃ³ Physics2D Raycaster.
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        Vector2 screenPos = Pointer.current != null
            ? Pointer.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        if (_uiPointerEventData == null)
            _uiPointerEventData = new PointerEventData(EventSystem.current);
        _uiPointerEventData.position = screenPos;

        _uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(_uiPointerEventData, _uiRaycastResults);

        for (int i = 0; i < _uiRaycastResults.Count; i++)
        {
            // Chá»‰ tÃ­nh káº¿t quáº£ tá»« GraphicRaycaster (Canvas UI), bá» qua Physics2D/3D
            if (_uiRaycastResults[i].module is GraphicRaycaster)
                return true;
        }

        return false;
    }
}
