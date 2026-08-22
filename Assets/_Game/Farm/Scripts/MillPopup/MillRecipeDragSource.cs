using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// NGUỒN KÉO của một card công thức — cho phép người chơi KÉO bao nguyên liệu từ danh sách
/// "CÔNG THỨC" và THẢ vào một slot xay.
///
/// Gắn cùng node với <see cref="MillRecipeCardUI"/> (MillPopupBuilderTool tự gắn vào prefab
/// `MillRecipeCard`).
///
/// ══════════════════════════════════════════════════════════════════════════
///  BÀI TOÁN KHÓ NHẤT: KÉO ĐỂ CUỘN vs KÉO ĐỂ LẤY BAO
/// ══════════════════════════════════════════════════════════════════════════
/// Card nằm trong một ScrollRect DỌC. Nếu card cứ nhận drag là bắt đầu kéo bao thì danh
/// sách công thức KHÔNG CUỘN ĐƯỢC NỮA — người chơi mobile không tới được công thức thứ 4.
/// Ngược lại nếu forward hết cho ScrollRect thì không kéo bao được.
///
/// Cách xử: PHÂN XỬ THEO TRỤC ở đúng frame OnBeginDrag.
///   • |Δy| &gt; |Δx| × <c>nguongTruc</c>  ⇒ người chơi muốn CUỘN  → forward toàn bộ cho ScrollRect
///   • còn lại (kéo ngang / chéo sang phải) ⇒ muốn LẤY BAO        → mở phiên MillDragSession
/// Khu slot nằm BÊN PHẢI danh sách nên "kéo ngang" là cử chỉ tự nhiên để mang bao qua đó.
/// Nếu danh sách ngắn hơn viewport (không có gì để cuộn) thì bỏ luôn phép phân xử — mọi
/// hướng kéo đều là lấy bao.
///
/// Quyết định được CHỐT một lần ở OnBeginDrag và giữ nguyên tới OnEndDrag. Không đổi ý
/// giữa đường: đổi giữa đường làm ScrollRect nhận OnDrag mà chưa nhận OnBeginDrag (kẹt
/// inertia) hoặc bóng kéo hiện ra giữa lúc danh sách đang trôi.
///
/// ══════════════════════════════════════════════════════════════════════════
///  KHÔNG PHÁ CLICK CHỌN CARD
/// ══════════════════════════════════════════════════════════════════════════
/// File này KHÔNG chạm tới `Button btnSelect` của card. Bấm (không kéo) vẫn chọn card như
/// cũ để xem trước sản phẩm ở bong bóng đầu ra. Unity phân biệt click và drag bằng
/// `EventSystem.pixelDragThreshold` nên hai cử chỉ không tranh nhau.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MillRecipeCardUI))]
public class MillRecipeDragSource : MonoBehaviour,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Phân xử trục kéo")]
    [Tooltip("Kéo được coi là CUỘN khi |Δy| > |Δx| × giá trị này.\n" +
             "1 = chia đôi 45°. Lớn hơn 1 ⇒ ưu tiên kéo bao (dễ lấy bao, khó cuộn).\n" +
             "Nhỏ hơn 1 ⇒ ưu tiên cuộn.")]
    [Range(0.4f, 3f)]
    [SerializeField] private float nguongTruc = 1f;

    [Header("Bóng kéo")]
    [Tooltip("Cạnh của bóng bao chạy theo ngón tay, pixel.")]
    [SerializeField] private float kichCoBong = 64f;

    [Tooltip("TUỲ CHỌN. Canvas để gắn bóng kéo. ĐỂ TRỐNG ⇒ code tự tìm canvas gần nhất " +
             "phía trên (chính là canvas của popup, sortingOrder 400).")]
    [SerializeField] private Canvas canvasBong;

    private MillRecipeCardUI _card;
    private ScrollRect       _scroll;
    private Canvas           _canvasDaTim;

    /// <summary>true = phiên kéo này dành cho ScrollRect, không phải kéo bao.</summary>
    private bool _dangCuon;

    /// <summary>true = phiên kéo này đang cầm bao (đã mở MillDragSession).</summary>
    private bool _dangCamBao;

    private void Awake()
    {
        _card = GetComponent<MillRecipeCardUI>();
    }

    private void OnDisable()
    {
        // Card bị tắt giữa lúc kéo (đóng popup, hoặc Bind(null) vì config bớt công thức)
        // ⇒ Unity không gửi OnEndDrag nữa. Tự dọn để bóng không nằm lại trên màn hình.
        if (_dangCamBao)
        {
            MillDragSession.HuyNgay();
            _dangCamBao = false;
        }

        // Đang forward cho ScrollRect mà card tắt ⇒ ScrollRect KHÔNG nhận OnEndDrag và cờ
        // `m_Dragging` của nó treo ở true VĨNH VIỄN: lần mở popup sau danh sách công thức
        // không cuộn được nữa và không có lỗi nào chỉ ra nguyên nhân.
        // Phải tự đóng phiên hộ nó. PointerEventData rỗng là đủ — OnEndDrag của ScrollRect
        // chỉ dùng eventData.button, và chỉ cần nó hạ cờ.
        if (_dangCuon)
        {
            ScrollRect sr = Cuon;
            if (sr != null && UnityEngine.EventSystems.EventSystem.current != null)
                sr.OnEndDrag(new PointerEventData(UnityEngine.EventSystems.EventSystem.current));
        }

        _dangCuon = false;
    }

    /// <summary>
    /// Chỉ chuột TRÁI / ngón tay mới kéo được. Đây đúng là cách `ScrollRect` tự lọc, nên bỏ
    /// qua ở đây là khớp hành vi: chuột phải kéo thì KHÔNG cuộn và KHÔNG nhấc bao, thay vì
    /// nhấc bao rồi treo bóng vì không có OnDrop nào nổ.
    /// </summary>
    private static bool NutHopLe(PointerEventData e)
    {
        return e != null && e.button == PointerEventData.InputButton.Left;
    }

    // ─────────────────────────── EVENT SYSTEM ───────────────────────────

    /// <summary>
    /// Luôn forward cho ScrollRect. Ở frame này CHƯA biết người chơi muốn cuộn hay lấy bao
    /// (delta còn bằng 0), mà ScrollRect BẮT BUỘC phải nhận event này để dừng inertia đang
    /// trôi — thiếu nó danh sách vẫn trượt trong lúc người chơi đã đặt ngón tay xuống.
    /// Không forward cho ai khác nên vô hại với nhánh kéo bao.
    /// </summary>
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        ScrollRect sr = Cuon;
        if (sr != null) sr.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dangCuon   = false;
        _dangCamBao = false;

        if (!NutHopLe(eventData)) return;

        // Card khoá (chưa đủ cấp) thì không kéo được — nhưng vẫn phải cho CUỘN, nếu không
        // người chơi đặt ngón tay vào card khoá là danh sách chết cứng.
        bool keoDuoc = _card != null && _card.IsUnlocked && _card.Recipe != null;

        if (!keoDuoc || LaCuCHiCuon(eventData))
        {
            _dangCuon = true;
            ScrollRect sr = Cuon;
            if (sr != null) sr.OnBeginDrag(eventData);
            return;
        }

        Canvas c = CanvasBong;
        MillDragSession.Bat(_card.Recipe, _card.IconSprite, c, kichCoBong,
                            eventData.position, eventData.pointerId);

        // Bat() có thể TỪ CHỐI nếu một ngón khác đang giữ phiên ⇒ phải hỏi lại chứ không
        // được tự nhận là đang cầm bao, nếu không OnEndDrag của ngón này sẽ Tat() phiên của
        // ngón kia.
        _dangCamBao = MillDragSession.ThuocVe(eventData.pointerId);
        if (!_dangCamBao) return;

        // Kéo bao cũng CHỌN luôn card đó: bong bóng đầu ra / bó cỏ trên băng tải đi theo
        // công thức đang chọn, không đồng bộ thì người chơi kéo "Cám heo" mà máy vẫn hiện
        // "Cám gà".
        MillPopupUI popup = MillPopupUI.Instance;
        if (popup != null) popup.BatDauKeo(_card.Recipe);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!NutHopLe(eventData)) return;

        if (_dangCuon)
        {
            ScrollRect sr = Cuon;
            if (sr != null) sr.OnDrag(eventData);
            return;
        }

        if (_dangCamBao)
            MillDragSession.Theo(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!NutHopLe(eventData)) return;

        if (_dangCuon)
        {
            ScrollRect sr = Cuon;
            if (sr != null) sr.OnEndDrag(eventData);
            _dangCuon = false;
            return;
        }

        if (!_dangCamBao) return;

        // Unity phát OnDrop (trên slot) TRƯỚC OnEndDrag (trên đây) ⇒ đọc DaTha ở đây là
        // đã biết cú thả có ai nhận hay không.
        bool aiNhan = MillDragSession.DaTha;

        MillDragSession.Tat();
        _dangCamBao = false;

        MillPopupUI popup = MillPopupUI.Instance;
        if (popup != null) popup.KetThucKeo(aiNhan);
    }

    // ─────────────────────────── NỘI BỘ ───────────────────────────

    /// <summary>
    /// Cử chỉ này là CUỘN danh sách hay LẤY BAO. Xem khối ghi chú đầu file.
    /// </summary>
    private bool LaCuCHiCuon(PointerEventData eventData)
    {
        ScrollRect sr = Cuon;
        if (sr == null) return false;

        // Danh sách không có gì để cuộn (nội dung ngắn hơn viewport) ⇒ mọi hướng đều là
        // lấy bao. Nếu vẫn phân xử thì kéo dọc sẽ "cuộn" một danh sách bất động, người chơi
        // tưởng game treo.
        if (!CoTheCuon(sr)) return false;

        Vector2 d = eventData.position - eventData.pressPosition;

        // Chưa vượt ngưỡng kéo thì delta gần 0 — dùng eventData.delta của frame này thay vì
        // chia cho 0.
        if (d.sqrMagnitude < 1f) d = eventData.delta;

        float nguong = (nguongTruc > 0.01f) ? nguongTruc : 1f;
        return Mathf.Abs(d.y) > Mathf.Abs(d.x) * nguong;
    }

    private static bool CoTheCuon(ScrollRect sr)
    {
        RectTransform content  = sr.content;
        RectTransform viewport = (sr.viewport != null) ? sr.viewport : sr.transform as RectTransform;

        if (content == null || viewport == null) return false;

        // +1px dung sai: layout tính ra 379.0001 vs 379 là chuyện thường, không nên vì thế
        // mà bật chế độ cuộn.
        return content.rect.height > viewport.rect.height + 1f;
    }

    private ScrollRect Cuon
    {
        get
        {
            if (_scroll == null) _scroll = GetComponentInParent<ScrollRect>();
            return _scroll;
        }
    }

    private Canvas CanvasBong
    {
        get
        {
            if (canvasBong != null) return canvasBong;

            if (_canvasDaTim == null) _canvasDaTim = GetComponentInParent<Canvas>();
            return _canvasDaTim;
        }
    }
}
