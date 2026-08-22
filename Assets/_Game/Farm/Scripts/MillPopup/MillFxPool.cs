using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// POOL ẢNH UI cho các hiệu ứng của popup máy xay — dùng chung bởi
/// <see cref="MillCelebrationFX"/>, <see cref="MillSmokeFX"/>.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO CẦN POOL
/// ══════════════════════════════════════════════════════════════════════════
/// Một loạt pháo hoa là ~30 mảnh giấy + 6 ngôi sao + 1 vệt loé, và khói thì phun liên tục
/// suốt thời gian máy chạy. `Instantiate`/`Destroy` từng cái là rác GC đều đặn — đúng thứ
/// gây giật khung hình trên máy tầm trung, và popup này có Definition of Done ≥60fps.
///
/// Pool giữ object đã tạo (ẩn đi), lần sau lấy lại dùng. Không bao giờ `Destroy` trong lúc
/// chơi.
///
/// ══════════════════════════════════════════════════════════════════════════
///  KHÔNG PHẢI MonoBehaviour
/// ══════════════════════════════════════════════════════════════════════════
/// Đây là class thuần, do component FX sở hữu như một field. Object con vẫn nằm dưới
/// <c>cha</c> nên vào Editor xem hierarchy vẫn thấy, và khi <c>cha</c> bị huỷ thì chúng
/// chết theo — lúc đó phần tử trong pool thành "fake-null", nên <see cref="Lay"/> luôn
/// kiểm `== null` tường minh và bỏ qua (`?.`/`??` KHÔNG hiểu fake-null của Unity).
/// </summary>
public sealed class MillFxPool
{
    private readonly RectTransform _cha;
    private readonly string        _ten;

    private readonly List<Image> _ranh    = new List<Image>();
    private readonly List<Image> _dangDung = new List<Image>();

    /// <param name="cha">Node cha để gắn ảnh. Toạ độ trả về sẽ tính theo pivot của node này.</param>
    /// <param name="ten">Tên node, để dễ nhìn trong Hierarchy khi debug.</param>
    public MillFxPool(RectTransform cha, string ten)
    {
        _cha = cha;
        _ten = string.IsNullOrEmpty(ten) ? "MillFx" : ten;
    }

    /// <summary>Số ảnh đang được dùng.</summary>
    public int DangDung => _dangDung.Count;

    /// <summary>
    /// Lấy một ảnh sẵn sàng dùng: đã bật, đã đặt sprite, kích cỡ, màu trắng đục, xoay 0,
    /// scale 1, và nằm cuối danh sách con (vẽ trên cùng).
    /// </summary>
    /// <param name="s">Sprite. null vẫn trả về ảnh (đã tắt renderer) để nơi gọi tự xử.</param>
    /// <param name="canh">Cạnh ô vuông, pixel.</param>
    /// <returns>null nếu chưa gán node cha.</returns>
    public Image Lay(Sprite s, float canh)
    {
        if (_cha == null) return null;

        Image img = null;

        // Bỏ qua phần tử đã bị huỷ (đổi scene, hoặc ai đó xoá tay trong Editor).
        while (_ranh.Count > 0 && img == null)
        {
            int cuoi = _ranh.Count - 1;
            img = _ranh[cuoi];
            _ranh.RemoveAt(cuoi);
        }

        if (img == null)
        {
            var go = new GameObject(_ten, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = _cha.gameObject.layer;

            var rt = (RectTransform)go.transform;
            rt.SetParent(_cha, false);

            // Neo TRÙNG PIVOT của cha: ScreenPointToLocalPointInRectangle trả toạ độ tính từ
            // pivot cha, còn anchoredPosition tính từ điểm neo. Cha là AnimationBox (pivot
            // góc trên-trái) nên neo giữa sẽ lệch nửa khung. Xem MillRectUtil.
            MillRectUtil.DatNeoTheoPivotCha(rt, _cha);

            img = go.GetComponent<Image>();
            img.raycastTarget  = false;   // hạt bay ngang con trỏ, KHÔNG được ăn click
            img.preserveAspect = true;
        }

        var r = img.rectTransform;
        float c = (canh > 1f) ? canh : 24f;
        r.sizeDelta     = new Vector2(c, c);
        r.localScale    = Vector3.one;
        r.localRotation = Quaternion.identity;
        r.SetAsLastSibling();

        img.sprite  = s;
        img.enabled = (s != null);
        img.color   = Color.white;

        if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);

        _dangDung.Add(img);
        return img;
    }

    /// <summary>Trả một ảnh về pool (ẩn đi, giữ lại để tái dùng).</summary>
    public void Tra(Image img)
    {
        if (img == null) return;

        _dangDung.Remove(img);

        if (img.gameObject.activeSelf) img.gameObject.SetActive(false);
        _ranh.Add(img);
    }

    /// <summary>
    /// Trả TẤT CẢ về pool. Gọi trong <c>OnDisable</c> và khi đóng popup: coroutine chết theo
    /// component nên không có ai gọi <see cref="Tra"/> hộ, thiếu bước này là vài mảnh giấy
    /// đứng bất động giữa khung máy ở lần mở sau.
    /// </summary>
    public void TraHet()
    {
        for (int i = 0; i < _dangDung.Count; i++)
        {
            Image img = _dangDung[i];
            if (img == null) continue;

            if (img.gameObject.activeSelf) img.gameObject.SetActive(false);
            _ranh.Add(img);
        }

        _dangDung.Clear();
    }
}
