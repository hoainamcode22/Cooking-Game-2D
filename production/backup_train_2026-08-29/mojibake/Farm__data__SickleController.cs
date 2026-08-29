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
    [Tooltip("Tá»‘c Ä‘á»™ follow cursor. Cao = snappy, tháº¥p = lag nháº¹ nhÆ° Hay Day.")]
    [SerializeField] private float followSpeed = 28f;

    private Camera mainCam;
    private bool isDragging;
    private int enabledFrame; // guard: khÃ´ng nháº­n release ngay frame enable

    // Cursor world position frame trÆ°á»›c â€” dÃ¹ng linecast Ä‘á»ƒ khÃ´ng miss khi kÃ©o nhanh
    private Vector3 prevCursorWorld;

    // HashSet chá»‘ng harvest trÃ¹ng trong cÃ¹ng 1 láº§n kÃ©o
    private readonly HashSet<PlotController> harvestedThisDrag = new HashSet<PlotController>();

    // Cache Ä‘á»ƒ trÃ¡nh allocate má»—i frame khi kiá»ƒm tra Canvas UI
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
    private PointerEventData _uiPointerEventData;

    private void Awake()
    {
        mainCam = Camera.main;

        // Náº¿u cÃ²n Rigidbody2D tá»« version cÅ© â†’ táº¯t simulation Ä‘á»ƒ khÃ´ng conflict vá»›i transform.position
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;
    }

    private void OnEnable()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        // isDragging=false khi tray hiá»‡n â€” chá»‰ set true khi user tháº­t sá»± kÃ©o liá»m (BeginHarvestMode)
        isDragging   = false;
        enabledFrame = Time.frameCount;
        harvestedThisDrag.Clear();
    }

    private void OnDisable()
    {
        FarmInputLock.IsDraggingSickle = false;
        harvestedThisDrag.Clear();
    }

    // FarmUIManager gá»i khi plot Ready Ä‘Æ°á»£c click
    public void BeginHarvestMode(Vector3 startWorldPos)
    {
        if (mainCam == null)
            mainCam = Camera.main;

        startWorldPos.z    = transform.position.z;
        transform.position = startWorldPos;
        prevCursorWorld    = startWorldPos;
        harvestedThisDrag.Clear();
        gameObject.SetActive(true);  // kÃ­ch hoáº¡t object (OnEnable set isDragging=false)
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

        gameObject.SetActive(false); // áº©n SickleTool world object sau khi gáº·t xong

    }

    private void Update()
    {
        if (!isDragging || mainCam == null)
            return;

        Vector3 cursorWorld = GetCursorWorldPos();

        // Visual: sickle follow cursor mÆ°á»£t â€” khÃ´ng snap cá»©ng
        transform.position = Vector3.Lerp(transform.position, cursorWorld, followSpeed * Time.deltaTime);

        // Detection: trace cursor path má»—i frame Ä‘á»ƒ khÃ´ng miss Ã´ khi kÃ©o nhanh
        // KhÃ´ng check khi cursor Ä‘ang trÃªn UI, nhÆ°ng KHÃ”NG dá»«ng drag state
        if (!IsPointerOverUI())
            CheckHarvestPath(prevCursorWorld, cursorWorld);

        prevCursorWorld = cursorWorld;

        // Mouse release â†’ káº¿t thÃºc drag
        if (IsPointerReleased() && Time.frameCount != enabledFrame)
        {
            FarmUIManager.Instance?.HideSickleTool();
        }
    }

    // Linecast tá»« vá»‹ trÃ­ frame trÆ°á»›c â†’ frame hiá»‡n táº¡i.
    // Khi kÃ©o nhanh, cursor nháº£y xa â†’ linecast báº¯t háº¿t plot á»Ÿ giá»¯a.
    private void CheckHarvestPath(Vector3 from, Vector3 to)
    {
        float distSq = (to - from).sqrMagnitude;

        if (distSq < 0.0001f)
        {
            // Äá»©ng yÃªn â€” check overlap táº¡i Ä‘iá»ƒm hiá»‡n táº¡i
            Collider2D col = Physics2D.OverlapPoint(to, plotLayerMask);
            if (col != null)
                TryHarvest(col);
            return;
        }

        // Di chuyá»ƒn â€” linecast Ä‘á»ƒ khÃ´ng bá» sÃ³t Ã´ á»Ÿ giá»¯a
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

        // ÄÃ£ harvest plot nÃ y trong drag hiá»‡n táº¡i â†’ skip
        if (harvestedThisDrag.Contains(plot))
            return;

        if (!plot.IsReadyToHarvest())
        {
            return;
        }

        string cropName = plot.CurrentCrop?.displayName ?? "NÃ´ng sáº£n";

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
