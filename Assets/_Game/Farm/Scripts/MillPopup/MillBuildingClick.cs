using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// CLICK CÔNG TRÌNH MÁY XAY TRONG WORLD → mở popup. Gắn lên `MayThucAn_Anim`.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO KHÔNG MỞ NGAY Ở OnMouseDown
/// ══════════════════════════════════════════════════════════════════════════
/// Yêu cầu là "chặn click khi đang kéo camera". Ở thời điểm `OnMouseDown` chạy thì
/// KHÔNG THỂ BIẾT cú nhấn này sẽ thành cú click hay thành cú kéo map — người chơi chưa
/// nhấc ngón lên. Mở popup ngay tại đó ⇒ mỗi lần kéo map mà điểm bắt đầu rơi trên máy xay
/// là popup nhảy ra, cực khó chịu trên điện thoại.
///
/// Nên:
///   • `OnMouseDown`         → chỉ GHI LẠI vị trí con trỏ (và kiểm tra các điều kiện chặn).
///   • `OnMouseUpAsButton`   → Unity chỉ gọi khi nhấn VÀ nhả ĐỀU trên collider này. Tới lúc
///                             này mới so quãng di chuyển với `nguongKeoPixel`: vượt ngưỡng
///                             ⇒ đó là cú kéo map, BỎ QUA.
///
/// ⚠ TÔI KHÔNG hỏi `CameraController` xem nó có đang kéo không: trong
/// `Assets/_Game/Farm/Scripts/Camera/CameraController.cs` biến `isDragging` là **private**
/// và không có property công khai nào phơi nó ra. Tôi không được sửa file có sẵn nên dùng
/// cách đo quãng di chuyển ở trên — cho kết quả tương đương và không phụ thuộc file khác.
/// Nếu sau này lead thêm `public bool IsDragging => isDragging;` vào CameraController thì
/// có thể thay bằng một dòng kiểm tra ở `BiChan()`.
///
/// ══════════════════════════════════════════════════════════════════════════
///  BA ĐIỀU KIỆN CHẶN
/// ══════════════════════════════════════════════════════════════════════════
/// 1. Popup máy xay ĐANG MỞ (`MillPopupUI.Instance.IsOpen`) — bấm lại không mở đè.
/// 2. Popup KHÁC đang mở (`PopupManager.Instance.IsAnyPopupOpen()`).
/// 3. Con trỏ đang trên UI (`EventSystem.current.IsPointerOverGameObject()`) — nếu không,
///    click vào một nút Canvas nằm đè lên công trình sẽ kích hoạt CẢ HAI.
///
/// ═ GHI CHÚ CHO LEAD ═
/// `PopupManager.IsAnyPopupOpen()` hiện KHÔNG biết tới popup máy xay. Tôi không sửa file
/// có sẵn, nên `MillPopupUI` phơi ra cờ static `MillPopupUI.AnyOpen` theo đúng quy ước dự
/// án đang dùng cho `CropProcessPopupUI.AnyOpen` / `OrderBoardPopupUI.AnyOpen`.
/// Thêm một dòng `|| MillPopupUI.AnyOpen` vào cuối `IsAnyPopupOpen()` là xong.
/// </summary>
[DisallowMultipleComponent]
public class MillBuildingClick : MonoBehaviour
{
    [Tooltip("Tự tạo BoxCollider2D khớp bounds của SpriteRenderer nếu công trình chưa có " +
             "collider. TẮT nếu bạn muốn tự vẽ vùng bấm bằng tay.")]
    [SerializeField] private bool tuTaoCollider = true;

    [Tooltip("Nhân với kích thước sprite để ra vùng bấm. 1 = khít sprite. " +
             "Máy xay có phần ống khói mảnh nên 0.9–1.0 là dễ bấm nhất.")]
    [SerializeField] private float heSoCoVungBam = 1f;

    [Tooltip("Con trỏ di chuyển quá bao nhiêu PIXEL giữa lúc nhấn và lúc nhả thì coi là " +
             "KÉO MAP chứ không phải click. 12px là mức quen dùng cho cảm ứng.")]
    [SerializeField] private float nguongKeoPixel = 12f;

    [Tooltip("BẬT để in log mỗi lần cú bấm bị chặn, kèm lý do. Dùng khi đi tìm " +
             "\"sao bấm máy mà không mở popup\".")]
    [SerializeField] private bool logGoLoi = false;

    private const string LOG = "[MILL] ";

    // Vector2 (KHÔNG phải Vector3): Input System trả về Vector2 cho vị trí con trỏ.
    // Bản đầu để Vector3 vì dùng Input.mousePosition cũ, gây lỗi CS0034
    // "Operator '-' is ambiguous on operands of type 'Vector2' and 'Vector3'".
    private Vector2 _viTriNhan;
    private bool    _dangNhan;

    private void Awake()
    {
        if (tuTaoCollider) BaoDamCoCollider();
    }

    /// <summary>
    /// Bảo đảm công trình có Collider2D — không có collider thì `OnMouseDown` của Unity
    /// KHÔNG BAO GIỜ được gọi (Unity bắn tia vào collider, không vào SpriteRenderer).
    /// </summary>
    private void BaoDamCoCollider()
    {
        // ⚠ CẠM BẪY ĐÃ GÂY LỖI THẬT TRONG DỰ ÁN NÀY:
        //   KHÔNG viết  GetComponent<T>() ?? gameObject.AddComponent<T>()
        //   Component thiếu, Unity trả về một tham chiếu "fake-null": nó KHÁC null theo
        //   phép so của C# (mà `??` dùng đúng phép so đó) nhưng lại == null theo toán tử
        //   Unity đã nạp chồng. Kết quả: `??` tưởng đã có component nên không AddComponent,
        //   dòng sau chạm vào nó là MissingComponentException/NullReference.
        //   Bắt buộc so tường minh bằng `== null`.
        Collider2D coSan = GetComponent<Collider2D>();
        if (coSan != null) return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogWarning(LOG + "'" + name + "' không có SpriteRenderer (hoặc chưa có sprite) " +
                             "nên không suy ra được vùng bấm. Hãy tự thêm Collider2D, " +
                             "nếu không click vào công trình sẽ không mở được popup.", this);
            return;
        }

        BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();

        // `sprite.bounds` là bounds trong KHÔNG GIAN LOCAL của sprite (đã tính pivot và
        // pixelsPerUnit). Đúng thứ BoxCollider2D.size/offset cần.
        // ĐỪNG dùng `sr.bounds` — đó là bounds WORLD, đã nhân scale + cộng vị trí; gán vào
        // size/offset (là local) sẽ ra hộp lệch và to sai tỉ lệ nếu công trình có scale ≠ 1.
        Bounds b = sr.sprite.bounds;

        box.size   = new Vector2(b.size.x * heSoCoVungBam, b.size.y * heSoCoVungBam);
        box.offset = new Vector2(b.center.x, b.center.y);

        if (logGoLoi)
            Debug.Log(LOG + "Đã tự thêm BoxCollider2D cho '" + name + "' size=" + box.size, this);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  NHẬN CLICK — VIẾT LẠI THEO ĐÚNG PATTERN ĐANG CHẠY ĐƯỢC CỦA DỰ ÁN
    // ═════════════════════════════════════════════════════════════════════════
    //
    // BẢN ĐẦU DÙNG OnMouseDown/OnMouseUpAsButton + IsPointerOverGameObject() VÀ KHÔNG
    // BAO GIỜ MỞ ĐƯỢC POPUP. Hai lý do, cả hai đều IM LẶNG (không hề có log/lỗi):
    //
    //  1) OnMouseDown / OnMouseUpAsButton / Input.mousePosition là API INPUT CŨ.
    //     Dự án này dùng Input System mới (WarehouseClickOpen, CameraController đều
    //     `using UnityEngine.InputSystem`). Nếu Project Settings > Active Input Handling
    //     đặt "Input System Package (New)" thì Unity KHÔNG bắn các sự kiện OnMouse* nữa —
    //     hàm không bao giờ được gọi, và Unity cũng không cảnh báo gì.
    //
    //  2) EventSystem.IsPointerOverGameObject() trả TRUE khi con trỏ nằm trên BẤT KỲ
    //     graphic nào có raycastTarget — kể cả HUD full-screen và blockingOverlay của
    //     PopupManager (Image alpha 0). Trên map này HUD phủ gần hết màn hình nên nó
    //     chặn gần như mọi cú bấm.
    //     WarehouseClickOpen CỐ Ý không dùng hàm đó: nó chỉ chặn khi tia UI trúng Canvas
    //     tên "Canvas_Popup". Ở đây làm y hệt để hành vi giống các công trình khác.
    //
    // Cách hiện tại = bản sao cách của WarehouseClickOpen: tự đọc chuột/cảm ứng bằng
    // Input System, rồi tự kiểm tra điểm bấm có nằm trong collider hay không bằng
    // Collider2D.OverlapPoint. Giữ thêm ngưỡng kéo để phân biệt click với kéo map.

    private void Update()
    {
        // ⚠ SỬA 20/08 — VÌ SAO KHÔNG CÒN `return` SAU NHÁNH NHẤN
        //
        // Bản trước viết:  if (DocViTriNhan(...)) { ...; return; }
        // Ở FPS thấp (build này đo được ~18 FPS ⇒ 55ms/frame) một cú click bình thường
        // (~50-80ms) NẰM GỌN TRONG MỘT FRAME. Input System khi đó báo CẢ HAI
        // `wasPressedThisFrame` VÀ `wasReleasedThisFrame` cùng true ở frame đó.
        // `return` sớm ⇒ nhánh NHẢ không bao giờ được xét ⇒ cú click bị ăn mất hoàn toàn,
        // và vì mọi nhánh thoát đều im lặng nên console không có một dòng nào để lần ra.
        // Nay đọc CẢ nhấn và nhả trong cùng một lượt Update rồi mới xử lý.
        bool coNhan = DocViTriNhan(out Vector2 viTriNhan);
        bool coNha  = DocViTriNha(out Vector2 viTriNha);

        if (coNhan)
        {
            _dangNhan = false;

            bool trung = TrungCongTrinh(viTriNhan);

            if (trung && BiChan())
            {
                // Chỉ log khi bấm ĐÚNG vào máy mà vẫn bị chặn — đó mới là tình huống cần
                // biết lý do. Bấm ra ngoài máy thì im lặng, không spam console.
                return;
            }

            if (!trung)
            {
                if (logGoLoi) NhatKyTruot(viTriNhan);
                return;
            }

            _dangNhan  = true;
            _viTriNhan = viTriNhan;
        }

        if (!coNha || !_dangNhan) return;

        _dangNhan = false;

        float diChuyen = (viTriNha - _viTriNhan).magnitude;
        if (diChuyen > nguongKeoPixel)
        {
            if (logGoLoi)
                Debug.Log(LOG + "Bỏ qua: con trỏ đi " + diChuyen.ToString("0") +
                          "px > ngưỡng " + nguongKeoPixel + "px ⇒ đây là cú KÉO MAP.", this);
            return;
        }

        // Kiểm tra lại: giữa lúc nhấn và nhả có thể popup khác đã mở.
        if (BiChan()) return;

        // KHÔNG kiểm TrungCongTrinh lần hai khi nhấn và nhả cùng frame: cùng một toạ độ,
        // kiểm lại chỉ tốn một lượt raycast. Khác frame thì vẫn kiểm để chặn cú kéo.
        if (!coNhan && !TrungCongTrinh(viTriNha))
        {
            if (logGoLoi)
                Debug.Log(LOG + "Bỏ qua: nhả tay NGOÀI vùng máy xay.", this);
            return;
        }

        MoPopup();
    }

    /// <summary>
    /// Log chi tiết khi cú bấm KHÔNG trúng công trình. Đây là nhánh im lặng nhất và cũng
    /// là nhánh hay sai nhất (camera sai, collider lệch, collider bị scale sai), nên khi
    /// bật logGoLoi thì phải in đủ số để so bằng mắt.
    /// </summary>
    private void NhatKyTruot(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning(LOG + "Camera.main = NULL ⇒ không đổi được toạ độ màn hình sang " +
                             "world, mọi cú bấm vào máy xay đều trượt. Gắn tag 'MainCamera' cho " +
                             "camera của farm.", this);
            return;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning(LOG + "'" + name + "' KHÔNG có Collider2D ⇒ không có vùng bấm. " +
                             "Chạy Tools/Farm/Popup May Xay/0. LAM TAT CA.", this);
            return;
        }

        Vector3 w = cam.ScreenToWorldPoint(screenPos);
        Bounds  b = col.bounds;

        Debug.Log(LOG + "Bấm TRƯỢT máy xay. Điểm bấm world = (" + w.x.ToString("0") + ", " +
                  w.y.ToString("0") + "). Vùng bấm của '" + name + "' = x[" +
                  b.min.x.ToString("0") + " … " + b.max.x.ToString("0") + "]  y[" +
                  b.min.y.ToString("0") + " … " + b.max.y.ToString("0") + "]. " +
                  "Nếu điểm bấm nằm TRONG khoảng trên mà vẫn báo trượt thì collider bị " +
                  "scale sai; nếu nằm ngoài thì bạn đang bấm sang công trình khác.", this);
    }

    /// <summary>Đọc vị trí lúc BẤM XUỐNG (chuột trái hoặc ngón tay). Input System mới.</summary>
    private static bool DocViTriNhan(out Vector2 viTri)
    {
        viTri = default;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            viTri = Mouse.current.position.ReadValue();
            return true;
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            viTri = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        return false;
    }

    /// <summary>Đọc vị trí lúc NHẢ.</summary>
    private static bool DocViTriNha(out Vector2 viTri)
    {
        viTri = default;
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            viTri = Mouse.current.position.ReadValue();
            return true;
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            viTri = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        return false;
    }

    /// <summary>Điểm bấm trên màn hình có nằm trong collider của công trình không.</summary>
    private bool TrungCongTrinh(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        Vector3 world3 = cam.ScreenToWorldPoint(screenPos);
        return col.OverlapPoint(new Vector2(world3.x, world3.y));
    }

    /// <summary>Mở popup máy xay. Public để tutorial / nút test gọi được mà không cần click thật.</summary>
    public void MoPopup()
    {
        var targetUI = MillPopupUI.Instance;
        if (targetUI == null)
            targetUI = Object.FindFirstObjectByType<MillPopupUI>(FindObjectsInactive.Include);

        if (targetUI == null)
        {
            Debug.LogWarning(LOG + "Không có MillPopupUI trong scene ⇒ không mở được popup máy xay. " +
                             "Chạy Tools/Farm/Popup May Xay/1. Dung Popup để dựng lại popup.", this);
            return;
        }

        targetUI.Open();
    }

    private bool BiChan()
    {
        // 1. Popup máy xay đang mở.
        if (MillPopupUI.Instance != null && MillPopupUI.Instance.IsOpen)
        {
            if (logGoLoi) Debug.Log(LOG + "Bỏ qua: popup máy xay đang mở.", this);
            return true;
        }

        // 2. Đang ở Edit Mode (kéo/đặt công trình)
        if (EditModeManager.IsEditMode)
        {
            if (logGoLoi) Debug.Log(LOG + "Bỏ qua: đang Edit Mode.", this);
            return true;
        }

        // 3. Popup khác đang mở.
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
        {
            if (logGoLoi) Debug.Log(LOG + "Bỏ qua: có popup khác đang mở.", this);
            return true;
        }

        return false;
    }

    /// <summary>
    /// CHỈ chặn khi tia UI trúng Canvas tên "Canvas_Popup" — copy nguyên tắc của
    /// WarehouseClickOpen. KHÔNG dùng IsPointerOverGameObject() vì HUD full-screen sẽ
    /// chặn mọi cú bấm (đó chính là lỗi của bản đầu).
    /// </summary>
    private bool ConTroTrenPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData data = new PointerEventData(EventSystem.current) { position = screenPos };
        List<RaycastResult> ketQua = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, ketQua);

        for (int i = 0; i < ketQua.Count; i++)
        {
            Canvas c = ketQua[i].gameObject.GetComponentInParent<Canvas>();
            if (c != null && c.name == "Canvas_Popup") return true;
        }
        return false;
    }
}
