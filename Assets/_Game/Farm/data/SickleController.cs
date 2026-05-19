using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sickle harvest drag tool.
/// Di chuyển theo cursor mỗi frame, dùng Linecast thay vì OnTriggerEnter2D
/// để không miss ô khi kéo nhanh.
/// </summary>
public class SickleController : MonoBehaviour
{
    [Header("Layer")]
    [SerializeField] private LayerMask plotLayerMask = ~0;

    [Header("Feel")]
    [Tooltip("Tốc độ follow cursor. Cao = snappy, thấp = lag nhẹ như Hay Day.")]
    [SerializeField] private float followSpeed = 28f;

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

    private void Awake()
    {
        mainCam = Camera.main;

        // Nếu còn Rigidbody2D từ version cũ → tắt simulation để không conflict với transform.position
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;
    }

    private void OnEnable()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        isDragging   = true;
        enabledFrame = Time.frameCount;
        harvestedThisDrag.Clear();
        FarmInputLock.IsDraggingSickle = true;
        Debug.Log("[Sickle] OnEnable → drag start");
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

        startWorldPos.z = transform.position.z;
        transform.position = startWorldPos;
        prevCursorWorld    = startWorldPos;

        harvestedThisDrag.Clear();
        isDragging = true;
        FarmInputLock.IsDraggingSickle = true;
        gameObject.SetActive(true);

        // Thu hoạch ngay lập tức ô đang được click — không đợi Update đầu tiên
        Vector2 startPos2D = new Vector2(startWorldPos.x, startWorldPos.y);
        Collider2D startCol = Physics2D.OverlapPoint(startPos2D, plotLayerMask);
        if (startCol != null)
            TryHarvest(startCol);

        Debug.Log($"[Sickle] BeginHarvestMode at {startWorldPos}");
    }

    public void EndHarvestMode()
    {
        isDragging = false;
        FarmInputLock.IsDraggingSickle = false;

        // Ép giải phóng focus EventSystem — tránh pointer state bị kẹt sau harvest
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Debug.Log("[Sickle] EndHarvestMode");
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
            Debug.Log("[Sickle] Released → hide");
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
            Collider2D col = Physics2D.OverlapPoint(to, plotLayerMask);
            if (col != null)
                TryHarvest(col);
            return;
        }

        // Di chuyển — linecast để không bỏ sót ô ở giữa
        RaycastHit2D[] hits = Physics2D.LinecastAll(from, to, plotLayerMask);
        if (hits.Length == 0)
            return;

        foreach (RaycastHit2D h in hits)
            TryHarvest(h.collider);
    }

    private void TryHarvest(Collider2D col)
    {
        if (col == null)
            return;

        PlotController plot = col.GetComponent<PlotController>()
                           ?? col.GetComponentInParent<PlotController>();
        if (plot == null)
            return;

        // Đã harvest plot này trong drag hiện tại → skip
        if (harvestedThisDrag.Contains(plot))
            return;

        if (!plot.IsReadyToHarvest())
        {
            Debug.Log($"[Sickle] {plot.name} not ready — skip");
            return;
        }

        string cropName = plot.CurrentCrop?.displayName ?? "Nông sản";
        Debug.Log($"[Sickle] Harvest: {plot.name} ({cropName})");

        if (plot.Harvest())
        {
            harvestedThisDrag.Add(plot);
            FarmManager.Instance?.OnPlotHarvested(plot, cropName);
        }
        else
        {
            Debug.LogWarning($"[Sickle] plot.Harvest() failed: {plot.name}");
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
    /// Trả về true chỉ khi con trỏ đang nằm trên Canvas UI (panel, button…).
    /// KHÔNG trả về true khi hover lên world collider (plot, building) — tránh
    /// chặn nhầm khi lưỡi liềm di chuyển qua ô lúa có Physics2D Raycaster.
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
            // Chỉ tính kết quả từ GraphicRaycaster (Canvas UI), bỏ qua Physics2D/3D
            if (_uiRaycastResults[i].module is GraphicRaycaster)
                return true;
        }

        return false;
    }
}
