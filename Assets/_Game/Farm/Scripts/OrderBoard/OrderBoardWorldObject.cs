using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// OBJECT BẢNG ĐƠN HÀNG NGOÀI MAP (B1) + PHIẾU GHIM PHẢN CHIẾU TRẠNG THÁI (B2).
///
/// B1 — THÂN CÔNG TRÌNH: đúng MỘT <c>SpriteRenderer</c> nằm ngay trên gốc. Cố ý không
/// tách thành bảng + mái + chóp vòm như bản tham chiếu: chủ dự án tự vẽ cả công trình
/// thành một ảnh, một ô để gắn là dễ nhất. Đây cũng đúng quy ước sẵn có của dự án —
/// `Market` và `CookingGate` đều là một SpriteRenderer trên gốc.
///
/// B2 — PHIẾU GHIM: mấy tờ giấy nhỏ trên mặt bảng. Tờ XANH LÁ = đơn giao được ngay,
/// tờ TRẮNG NGÀ = chưa đủ hàng. Người chơi liếc qua bản đồ là biết có việc để làm hay
/// không, KHÔNG cần mở popup. Đây là thủ pháp mà quầy hàng cũng đang dùng (bày hàng lên
/// mặt quầy) — giữ cho hai công trình nói cùng một thứ ngôn ngữ.
///
/// Cách bắt chạm bám nguyên khuôn `StallWorldObject` / `MarketClickOpen`: New Input
/// System + <c>Collider2D.OverlapPoint</c>, KHÔNG dùng <c>OnMouseDown</c>. Lý do phải
/// theo: <c>OnMouseDown</c> không thấy chạm trên mobile khi scene có nhiều camera, và nó
/// bỏ qua hết các chốt chặn (edit mode, popup đang mở) mà mọi công trình khác đều tôn
/// trọng — lệch khuôn là bảng mở được xuyên qua popup đang che nó.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OrderBoardWorldObject : MonoBehaviour
{
    [Header("Liên kết")]
    [SerializeField] private OrderBoardPopupUI popupUI;
    [SerializeField] private Camera            mainCamera;
    [SerializeField] private Collider2D        targetCollider;

    [Header("B1 — Chỗ chờ art (chủ dự án gắn ảnh bảng vào đây)")]
    [Tooltip("SpriteRenderer duy nhất trên gốc. Thay sprite là thành công trình thật.")]
    [SerializeField] private SpriteRenderer spriteArtBoard;

    [Header("B2 — Phiếu ghim trên mặt bảng")]
    [Tooltip("Gốc chứa các tờ phiếu — EditModeManager tắt cả cụm này khi vào Edit Mode.")]
    [SerializeField] private GameObject       orderMarksRoot;
    [SerializeField] private SpriteRenderer[] orderMarks;

    [Tooltip("Phiếu của đơn ĐÃ ĐỦ HÀNG.")]
    [SerializeField] private Color colorMarkReady = new Color(0.44f, 0.80f, 0.40f, 1f);
    [Tooltip("Phiếu của đơn chưa đủ hàng.")]
    [SerializeField] private Color colorMarkNormal = new Color(0.96f, 0.94f, 0.86f, 1f);

    [Header("Neo cho con cú trỏ tay")]
    [Tooltip("Bỏ trống = tự lấy gốc công trình. Gán vào đây nếu muốn tay chỉ vào một " +
             "điểm cụ thể (đỉnh mái, mép bảng) thay vì tâm object.")]
    [SerializeField] private Transform neoTroTay;

    [Header("Nhịp cập nhật phiếu")]
    [Tooltip("Giây giữa hai lần kiểm lại kho. Kho đổi mà bảng không đổi (thu hoạch, nấu ăn) nên chỉ nghe sự kiện là chưa đủ.")]
    [SerializeField] private float markRefreshSeconds = 1.0f;

    [Header("Điều kiện mở")]
    [Tooltip("Cấp tối thiểu để dùng bảng. 0 = mở ngay từ đầu.")]
    [SerializeField] private int requiredLevel = 0;

    [Tooltip("Tên các Canvas popup — bấm trúng chúng thì KHÔNG tính là bấm vào bảng.")]
    [SerializeField]
    private string[] popupCanvasNames =
        { "Canvas_Popup", "Canvas_MarketPopup", "Canvas_StallPopup", "Canvas_OrderBoardPopup" };

    private static readonly List<RaycastResult> RaycastBuffer = new List<RaycastResult>();

    private float _nextMarkRefresh;

    /// <summary>
    /// Manager ĐANG nghe. Giữ tham chiếu thay vì cờ bool để scene nạp lại (manager là
    /// object mới) vẫn nối đúng — nếu không thì phiếu ngoài map đứng im vĩnh viễn.
    /// </summary>
    private OrderBoardManagerBase _subscribedTo;

    // ─────────────────────────────────────────────────────────────────────────
    //  VÒNG ĐỜI
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (mainCamera == null)     mainCamera = Camera.main;
        if (targetCollider == null) targetCollider = GetComponent<Collider2D>();
        if (spriteArtBoard == null) spriteArtBoard = GetComponent<SpriteRenderer>();

        if (popupUI == null)
            popupUI = FindAnyObjectByType<OrderBoardPopupUI>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        Subscribe();

        // Tự nghe Edit Mode thay vì chờ `EditModeManager` gọi hộ (chốt ở mục 8.3 file
        // TEAM). Bớt được một chiều phụ thuộc: `EditModeManager` không phải biết bảng
        // đơn hàng tồn tại, nên sau này thêm/bớt công trình cũng không phải sửa nó.
        EditModeManager.OnEditModeChanged -= OnEditModeChanged;
        EditModeManager.OnEditModeChanged += OnEditModeChanged;

        // Bật lại đúng trạng thái hiện tại: object có thể vừa được bật lên GIỮA lúc
        // người chơi đang ở trong Edit Mode, khi đó sự kiện đã bắn xong từ lâu.
        SetOrderMarksVisible(!EditModeManager.IsEditMode);

        RefreshMarks();
    }

    private void Start()
    {
        // Đăng ký lại: manager của DEV-A có thể Awake SAU công trình này.
        Subscribe();
        RefreshMarks();
    }

    private void OnDisable()
    {
        Unsubscribe();
        EditModeManager.OnEditModeChanged -= OnEditModeChanged;
    }

    /// <summary>Vào Edit Mode thì giấu phiếu — lúc kéo công trình, giấy nhấp nháy chỉ gây rối mắt.</summary>
    private void OnEditModeChanged(bool isEditMode) => SetOrderMarksVisible(!isEditMode);

    private void Subscribe()
    {
        OrderBoardManagerBase board = OrderBoardManagerBase.Instance;
        if (board == null || _subscribedTo == board) return;

        Unsubscribe();
        board.OnBoardChanged += RefreshMarks;
        _subscribedTo = board;

        // Tự khai neo cho con cú trỏ tay. Trước đây ô `Board World Anchor` trong
        // Inspector phải gán tay, mà tool dựng scene không gán nổi (lúc dựng manager
        // chưa chắc đã tồn tại) — nên nó luôn là None và người dựng scene không có
        // cách nào biết mình quên. Để chính công trình tự khai lúc chạy thì ô đó bỏ
        // trống vẫn đúng, còn ai muốn trỏ vào chỗ khác (đỉnh mái, mép bảng) thì gán
        // đè vào Inspector — dòng dưới tôn trọng giá trị đã gán, không ghi chồng.
        if (board is OrderBoardManager real && real.BoardWorldAnchor == null)
            real.RegisterBoardAnchor(neoTroTay != null ? neoTroTay : transform);
    }

    private void Unsubscribe()
    {
        if (_subscribedTo != null) _subscribedTo.OnBoardChanged -= RefreshMarks;
        _subscribedTo = null;
    }

    private void Update()
    {
        if (TryGetPointerScreenPosition(out Vector2 screenPos))
            TryOpenBoard(screenPos);

        // Kho thay đổi mà bảng đơn KHÔNG đổi (người chơi vừa thu hoạch, vừa nấu xong).
        // Chỉ nghe OnBoardChanged thì tờ phiếu vẫn trắng dù đơn đã giao được — mà đó
        // chính là thứ B2 hứa hẹn. Kiểm lại mỗi giây là đủ, rẻ hơn nhiều so với nghe
        // mọi sự kiện kho của cả hai hệ Farm và Cooking.
        if (Time.unscaledTime >= _nextMarkRefresh)
        {
            _nextMarkRefresh = Time.unscaledTime + Mathf.Max(0.2f, markRefreshSeconds);
            Subscribe();       // bắt kịp trường hợp manager sinh ra muộn
            RefreshMarks();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  B2 · PHIẾU GHIM
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Vẽ lại các tờ phiếu trên mặt bảng.
    ///
    /// Ưu tiên hiện các đơn GIAO ĐƯỢC trước: số phiếu trên bảng ít hơn số đơn (bảng có
    /// 9 đơn nhưng chỉ ghim vài tờ cho gọn). Nếu ghim theo thứ tự lưới thì rất dễ rơi
    /// vào cảnh có 3 đơn giao được mà ngoài map toàn phiếu trắng — đúng cái hiểu lầm
    /// mà B2 sinh ra để dập.
    /// </summary>
    public void RefreshMarks()
    {
        if (orderMarks == null || orderMarks.Length == 0) return;

        OrderBoardManagerBase board = OrderBoardManagerBase.Instance;
        IReadOnlyList<OrderBoardOrderView> orders = board != null ? board.GetOrders() : null;

        int totalCount = 0;
        if (orders != null)
        {
            for (int i = 0; i < orders.Count; i++)
                if (orders[i] != null) totalCount++;
        }

        // Tô TỪNG TỜ theo ĐÚNG đơn tương ứng, không tô theo số lượng.
        //
        // Bản cũ tô `readyCount` tờ đầu tiên màu xanh — nhìn thì cũng ra "có đơn giao được",
        // nhưng sai ngữ nghĩa: tờ thứ nhất xanh không có nghĩa đơn thứ nhất xong.
        // Video làm đúng theo từng tờ, và đó mới là thứ khiến người chơi liếc một cái là
        // đọc được tình hình.
        //
        // Bảng luôn giữ đủ 9 đơn nên `totalCount` luôn bằng 9 ⇒ điều kiện `i < totalCount`
        // của bản cũ luôn đúng, năm tờ luôn hiện hết. Giữ nguyên hành vi đó (bảng lúc nào
        // cũng có giấy) nhưng màu thì phải bám đúng đơn.
        for (int i = 0; i < orderMarks.Length; i++)
        {
            SpriteRenderer mark = orderMarks[i];
            if (mark == null) continue;

            bool visible = i < totalCount;
            if (mark.enabled != visible) mark.enabled = visible;
            if (!visible) continue;

            bool sanSang = orders != null && i < orders.Count
                           && orders[i] != null && orders[i].CanDeliverNow();
            mark.color = sanSang ? colorMarkReady : colorMarkNormal;
        }
    }

    /// <summary>
    /// Ẩn/hiện cụm phiếu — <c>EditModeManager</c> gọi khi vào/ra Edit Mode.
    /// Lúc người chơi đang kéo công trình, mấy tờ giấy nhấp nháy chỉ gây rối mắt.
    /// </summary>
    public void SetOrderMarksVisible(bool visible)
    {
        if (orderMarksRoot != null && orderMarksRoot.activeSelf != visible)
            orderMarksRoot.SetActive(visible);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BẤM ĐỂ MỞ POPUP
    // ─────────────────────────────────────────────────────────────────────────

    private static bool TryGetPointerScreenPosition(out Vector2 screenPos)
    {
        screenPos = default;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        return false;
    }

    private void TryOpenBoard(Vector2 screenPos)
    {
        // Minigame nấu ăn nạp chồng lên scene farm — lúc đó click thuộc về minigame.
        if (SceneManager.GetSceneByName("SampleScene").isLoaded) return;

        if (EditModeManager.IsEditMode) return;
        if (FarmInputLock.BlockMapPan) return;

        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) return;

        if (popupUI == null || mainCamera == null || targetCollider == null) return;
        if (popupUI.IsOpen) return;

        if (IsPointerOverPopupUI(screenPos)) return;

        Vector3 world3 = mainCamera.ScreenToWorldPoint(screenPos);
        if (!targetCollider.OverlapPoint(new Vector2(world3.x, world3.y))) return;

        if (requiredLevel > 0 && GetPlayerLevel() < requiredLevel)
        {
            Debug.Log($"[BảngĐơn] Cần đạt cấp {requiredLevel} mới dùng được bảng đơn hàng.");
            return;
        }

        popupUI.OpenPopup();
    }

    private static int GetPlayerLevel()
    {
        if (PlayerProgressManager.Instance != null) return PlayerProgressManager.Instance.Level;
        if (FarmLevelManager.Instance != null)      return FarmLevelManager.Instance.CurrentLevel;
        return 1;
    }

    private bool IsPointerOverPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

        RaycastBuffer.Clear();
        EventSystem.current.RaycastAll(eventData, RaycastBuffer);

        for (int i = 0; i < RaycastBuffer.Count; i++)
        {
            Canvas parentCanvas = RaycastBuffer[i].gameObject.GetComponentInParent<Canvas>();
            if (parentCanvas == null) continue;

            // Kiểm null: người dùng xoá mảng trong Inspector là ném NullReferenceException
            // mỗi lần chạm màn hình — lỗi rất khó truy vì nó nằm trong đường raycast.
            if (popupCanvasNames == null) continue;

            for (int n = 0; n < popupCanvasNames.Length; n++)
                if (parentCanvas.name == popupCanvasNames[n]) return true;
        }

        return false;
    }
}
