using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PHIÊN KÉO-THẢ CỦA POPUP MÁY XAY — trạng thái "đang cầm bao nguyên liệu nào".
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO LÀ static
/// ══════════════════════════════════════════════════════════════════════════
/// Kéo-thả có ĐÚNG MỘT phiên tại một thời điểm (một ngón tay / một con chuột). Bên gửi
/// (<see cref="MillRecipeDragSource"/> trên card) và bên nhận (<see cref="MillSlotUI"/>)
/// nằm ở hai nhánh hierarchy khác nhau, không có tham chiếu tới nhau. Nhồi thêm reference
/// hai chiều chỉ để truyền một <see cref="MillRecipeData"/> là thừa và dễ đứt khi
/// MillPopupBuilderTool dựng lại cây node.
///
/// ══════════════════════════════════════════════════════════════════════════
///  BÓNG KÉO (ghost)
/// ══════════════════════════════════════════════════════════════════════════
/// Card nằm trong ScrollRect có RectMask2D ⇒ KHÔNG được kéo chính card đi (nó bị cắt ngay
/// khi ra khỏi viewport). Thay vào đó ta dựng một Image rời, gắn làm CON CUỐI của canvas
/// popup nên luôn vẽ trên mọi thứ, và cho nó chạy theo con trỏ.
///
/// `raycastTarget = false` là BẮT BUỘC: bóng nằm ngay dưới con trỏ, nếu nó ăn raycast thì
/// EventSystem không bao giờ thấy slot bên dưới ⇒ OnDrop không bao giờ nổ.
///
/// ══════════════════════════════════════════════════════════════════════════
///  TÁI DÙNG, KHÔNG DESTROY MỖI LẦN
/// ══════════════════════════════════════════════════════════════════════════
/// Bóng được tạo một lần rồi ẩn/hiện. Kéo-thả là hành động người chơi làm liên tục
/// (5 slot × nhiều lượt), Instantiate/Destroy mỗi lần là rác GC vô ích.
///
/// Object có thể bị huỷ khi đổi scene ⇒ mọi chỗ đều so `== null` tường minh (Unity nạp
/// chồng toán tử này cho Object; `?.` và `??` KHÔNG hiểu "fake-null" nên tuyệt đối không dùng).
/// </summary>
public static class MillDragSession
{
    private const string TEN_BONG = "MillDragGhost";

    private static MillRecipeData _congThuc;
    private static bool           _daTha;

    /// <summary>
    /// Con trỏ / ngón tay đang giữ phiên này. `PointerEventData.pointerId` là −1..−3 cho
    /// chuột và 0..n cho từng ngón tay chạm.
    ///
    /// ⚠ VÌ SAO CẦN: hai ngón tay có thể nhấc hai card khác nhau cùng lúc. Không kiểm chủ
    /// sở hữu thì ngón thứ hai ghi đè `_congThuc`, rồi ngón thứ nhất nhả tay trên một slot
    /// và slot đó bắt đầu xay CÔNG THỨC CỦA NGÓN KIA — trừ nguyên liệu sai, và người chơi
    /// không hiểu vì sao.
    /// </summary>
    private static int _conTro = KHONG_CO;

    private const int KHONG_CO = int.MinValue;

    private static GameObject    _bongGo;
    private static RectTransform _bongRt;
    private static Image         _bongImg;
    private static Canvas        _canvas;
    private static RectTransform _canvasRt;

    /// <summary>Công thức người chơi đang cầm. null = không có phiên kéo nào.</summary>
    public static MillRecipeData Recipe => _congThuc;

    /// <summary>Có đang kéo một công thức hay không.</summary>
    public static bool IsDragging => _congThuc != null;

    /// <summary>
    /// Phiên hiện tại có thuộc về con trỏ <paramref name="conTro"/> không.
    /// Slot phải hỏi câu này trước khi nhận cú thả — xem ghi chú ở `_conTro`.
    /// </summary>
    public static bool ThuocVe(int conTro) => (_congThuc != null) && (_conTro == conTro);

    /// <summary>Phiên kéo này đã được một slot nhận (OnDrop đã nổ) hay chưa.</summary>
    public static bool DaTha => _daTha;

    /// <summary>
    /// Bắt đầu một phiên kéo và hiện bóng tại vị trí con trỏ.
    /// </summary>
    /// <param name="r">Công thức đang kéo. null ⇒ không làm gì.</param>
    /// <param name="icon">Sprite vẽ trong bóng. null ⇒ vẫn kéo được, chỉ là bóng trống.</param>
    /// <param name="canvas">Canvas để gắn bóng. Nên là canvas của popup (sortingOrder cao nhất).</param>
    /// <param name="kichCo">Cạnh của bóng, pixel.</param>
    /// <param name="viTriManHinh">Vị trí con trỏ lúc bắt đầu, toạ độ màn hình.</param>
    /// <param name="conTro">`PointerEventData.pointerId` của ngón tay/chuột mở phiên này.</param>
    public static void Bat(MillRecipeData r, Sprite icon, Canvas canvas, float kichCo,
                           Vector2 viTriManHinh, int conTro)
    {
        if (r == null) return;

        // Đã có một ngón khác đang giữ phiên ⇒ BỎ QUA ngón mới. Ngón đầu tiên thắng: người
        // chơi nhìn thấy bóng của ngón đó, đổi giữa đường là giật hình.
        if (_congThuc != null && _conTro != KHONG_CO && _conTro != conTro) return;

        _congThuc = r;
        _daTha    = false;
        _conTro   = conTro;

        if (canvas == null) return;   // vẫn giữ phiên logic, chỉ là không có bóng để vẽ

        BaoDamCoBong(canvas, kichCo);

        if (_bongImg != null)
        {
            _bongImg.sprite         = icon;
            _bongImg.enabled        = (icon != null);
            _bongImg.preserveAspect = true;
        }

        if (_bongGo != null && !_bongGo.activeSelf)
            _bongGo.SetActive(true);

        // Luôn đẩy xuống cuối: popup có thể mở toast/khung khác SAU khi bóng được tạo,
        // thứ tự sibling quyết định thứ tự vẽ trong cùng canvas.
        if (_bongRt != null) _bongRt.SetAsLastSibling();

        Theo(viTriManHinh);
    }

    /// <summary>Cho bóng chạy theo con trỏ. Gọi mỗi frame trong OnDrag.</summary>
    public static void Theo(Vector2 viTriManHinh)
    {
        if (_bongRt == null || _canvasRt == null || _canvas == null) return;

        // ScreenSpaceOverlay ⇒ camera phải là null, truyền camera vào sẽ lệch vị trí.
        // Canvas lồng nhau trả về renderMode của canvas GỐC nên phép so này vẫn đúng.
        Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;

        Vector2 cucBo;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, viTriManHinh, cam, out cucBo))
            _bongRt.anchoredPosition = cucBo;
    }

    /// <summary>Slot đã nhận cú thả này. Gọi từ <see cref="MillSlotUI.OnDrop"/>.</summary>
    public static void GhiNhanTha()
    {
        _daTha = true;
    }

    /// <summary>
    /// Kết thúc phiên kéo: xoá công thức đang cầm và ẩn bóng.
    /// Gọi từ OnEndDrag — Unity phát OnDrop TRƯỚC OnEndDrag nên lúc này
    /// <see cref="DaTha"/> đã đúng.
    /// </summary>
    public static void Tat()
    {
        _congThuc = null;
        _daTha    = false;
        _conTro   = KHONG_CO;

        if (_bongGo != null && _bongGo.activeSelf)
            _bongGo.SetActive(false);
    }

    /// <summary>
    /// Dọn sạch phiên kéo đang treo — gọi khi ĐÓNG popup.
    ///
    /// ⚠ BẮT BUỘC: người chơi có thể bấm nút X trong lúc đang kéo (nút đóng nằm ngoài
    /// ScrollRect nên vẫn nhận click). Khi đó OnEndDrag của card vẫn nổ, nhưng nếu popup
    /// bị tắt trước thì card đã disable ⇒ Unity KHÔNG gửi OnEndDrag nữa và bóng nằm lại
    /// trên màn hình mãi mãi.
    /// </summary>
    public static void HuyNgay()
    {
        _congThuc = null;
        _daTha    = false;
        _conTro   = KHONG_CO;

        if (_bongGo != null)
        {
            if (_bongGo.activeSelf) _bongGo.SetActive(false);
            if (_bongImg != null)   _bongImg.sprite = null;
        }
    }

    // ─────────────────────────── NỘI BỘ ───────────────────────────

    private static void BaoDamCoBong(Canvas canvas, float kichCo)
    {
        // Đổi canvas (mở popup ở scene khác) ⇒ dựng lại bóng ở canvas mới.
        if (_bongGo != null && _canvas == canvas)
        {
            ApKichCo(kichCo);
            return;
        }

        if (_bongGo != null)
            Object.Destroy(_bongGo);

        _canvas   = canvas;
        _canvasRt = canvas.transform as RectTransform;

        _bongGo = new GameObject(TEN_BONG, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _bongGo.layer = canvas.gameObject.layer;

        _bongRt = (RectTransform)_bongGo.transform;
        _bongRt.SetParent(canvas.transform, false);

        // Neo TRÙNG PIVOT của canvas, không cứng (0.5, 0.5): ScreenPointToLocalPointInRectangle
        // trả về toạ độ tính từ PIVOT của cha, còn anchoredPosition tính từ ĐIỂM NEO. Canvas
        // mặc định pivot giữa nên hai cách cho cùng kết quả, nhưng ai đó đổi pivot canvas là
        // bóng lệch nửa màn hình mà không có lỗi nào để lần ra.
        MillRectUtil.DatNeoTheoPivotCha(_bongRt, _canvasRt);
        _bongRt.localScale = Vector3.one;

        _bongImg = _bongGo.GetComponent<Image>();
        _bongImg.raycastTarget = false;   // xem khối ghi chú đầu file — KHÔNG được bỏ dòng này
        _bongImg.preserveAspect = true;

        ApKichCo(kichCo);
        _bongGo.SetActive(false);
    }

    private static void ApKichCo(float kichCo)
    {
        if (_bongRt == null) return;

        float c = (kichCo > 1f) ? kichCo : 64f;
        _bongRt.sizeDelta = new Vector2(c, c);
    }
}
