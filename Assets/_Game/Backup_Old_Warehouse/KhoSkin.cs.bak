using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// KHO VẬT PHẨM — bố cục theo render `KhoVatPham_B.html`:
/// ván gỗ + ruy băng tiêu đề · lưới ô TRẮNG bo góc gọn (không còn ô khổng lồ) ·
/// số lượng trong huy hiệu nâu (tấm `khung` sẵn trong prefab item_1) ·
/// thanh "x/25 Slot" thẻ kem góc trái dưới · Nâng cấp vàng góc phải dưới ·
/// 3 modal con (chuyển bếp / nâng cấp / thiếu đồ) cùng ngôn ngữ giấy-kem-nút-màu.
///
/// Tham chiếu lấy thẳng từ [SerializeField] của WarehousePopupUI (SkinVi.Lay) —
/// không dò tên. Chỉ DI CHUYỂN / TÔ / THÊM lớp trang trí, logic nguyên vẹn.
/// </summary>
[DisallowMultipleComponent]
public class KhoSkin : MonoBehaviour
{
    [Header("Bật/tắt mặc áo Kho (tắt khi dùng hệ UI Kho mới 100% Mockup)")]
    [SerializeField] private bool batAo = false;

    private WarehousePopupUI _popup;
    private GameObject _root;
    private RectTransform _luoi;
    private bool _daAp;
    private float _lanQuet;
    private readonly HashSet<WarehouseSlotUI> _daMacSlot = new HashSet<WarehouseSlotUI>();
    private readonly List<(TMP_Text chu, Image tui, Image than, Image vien)> _oTheoDoi =
        new List<(TMP_Text, Image, Image, Image)>();

    // Ô trống phải ĐẬM HƠN nền giấy rõ rệt — bản trước #f3ead2 trên nền #fdf3da
    // gần như tàng hình (ảnh 13/08: "chỉ thấy nền, không thấy gì").
    private static readonly Color ThanCoDo   = TaskPopupDesign.Hex("#fffcf3");
    private static readonly Color ThanTrong  = TaskPopupDesign.Hex("#eadfc0");
    private static readonly Color VienCoDo   = TaskPopupDesign.HangVien;
    private static readonly Color VienTrong  = TaskPopupDesign.Hex("#d2bf9a");

    private void Awake() => _popup = GetComponent<WarehousePopupUI>();

    private void Update()
    {
        if (!batAo || !Application.isPlaying || _popup == null) return;

        if (_root == null)
        {
            _root = SkinVi.Lay<GameObject>(_popup, "popupRoot");
            if (_root == null) return;
        }
        if (!_root.activeInHierarchy) return;

        if (!_daAp) { _daAp = true; MacKhung(); }

        if (Time.unscaledTime < _lanQuet) return;
        _lanQuet = Time.unscaledTime + 0.3f;

        // ÉP layout tính xong TRƯỚC khi mặc ô — mặc ở khung hình ô vừa đổi cỡ là
        // viền Skin_Border đo theo cỡ CŨ 380px, 25 cái viền khổng lồ đè nhau thành
        // mớ đan rối (ảnh 13/08). Cùng lớp bug đã gặp ở thẻ Shop.
        if (_luoi != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_luoi);

        // Ô kho: quét theo COMPONENT từ popupRoot (bản 1 quét từ GO của manager —
        // popup nằm nhánh khác nên không thấy ô nào, ảnh chụp toàn ô beige cũ).
        foreach (var slot in _root.GetComponentsInChildren<WarehouseSlotUI>(false))
        {
            if (_daMacSlot.Contains(slot)) continue;
            MacAoSlot(slot);
            _daMacSlot.Add(slot);
        }

        // Ô CÓ ĐỒ: thân trắng + huy hiệu nâu. Ô TRỐNG: thân kem mờ, không huy hiệu
        // (mock để ô trống chìm hẳn xuống). Kho đổi nội dung liên tục nên soát mỗi nhịp.
        for (int i = _oTheoDoi.Count - 1; i >= 0; i--)
        {
            var (chu, tui, than, vien) = _oTheoDoi[i];
            if (chu == null || than == null) { _oTheoDoi.RemoveAt(i); continue; }
            bool coDo = !string.IsNullOrEmpty(chu.text);
            if (tui != null) tui.enabled = coDo;
            than.color = coDo ? ThanCoDo : ThanTrong;
            if (vien != null) vien.color = coDo ? VienCoDo : VienTrong;
        }
    }

    private void MacKhung()
    {
        Transform goc = _root.transform;

        // Ván gỗ = Image to nhất của popup. KHÔNG gradient — tấm Kho cao, dải
        // gradient loang thành mảng sáng lệch góc (ảnh 13/08).
        SkinKit.MacAoVanGo(SkinVi.TimVanGo(goc), 40f, themGradient: false);

        // Ruy băng quanh tiêu đề — tôn trọng chữ có sẵn, chỉ lót vàng phía dưới.
        var tieuDe = goc.GetComponentInChildren<TMP_Text>(true);
        Transform tim = TimTheoTen(goc, "TxtTitle");
        if (tim != null) tieuDe = tim.GetComponent<TMP_Text>();
        if (tieuDe != null && tieuDe.transform.parent != null)
        {
            var trt = (RectTransform)tieuDe.transform;
            if (trt.parent.Find("Skin_Ribbon") == null && trt.anchorMin == trt.anchorMax)
            {
                var ktRb = new Vector2(Mathf.Clamp(trt.rect.width + 120f, 460f, 720f),
                                       Mathf.Max(trt.rect.height + 36f, 96f));
                var rb = SkinKit.LamRuyBang(trt.parent, "", trt.anchorMin,
                                            trt.anchoredPosition, ktRb);
                rb.pivot = trt.pivot;
                rb.SetSiblingIndex(trt.GetSiblingIndex());
                tieuDe.color = Color.white;
                if (tieuDe.GetComponent<Shadow>() == null)
                {
                    var sh = tieuDe.gameObject.AddComponent<Shadow>();
                    sh.effectColor = new Color(0f, 0f, 0f, 0.3f);
                    sh.effectDistance = new Vector2(0f, -3f);
                }
            }
        }

        // Tìm kiếm: ô lõm kem như mock.
        var timKiem = SkinVi.Lay<TMP_InputField>(_popup, "inputSearch");
        var tkImg = timKiem != null ? timKiem.GetComponent<Image>() : null;
        if (tkImg != null)
        {
            SkinKit.MacAoThe(tkImg, 18f);
            tkImg.color = TaskPopupDesign.Hex("#f3e2bb");
            foreach (var tmp in timKiem.GetComponentsInChildren<TMP_Text>(true))
                tmp.color = TaskPopupDesign.Hex("#8d7550");
        }

        SkinVi.NutDong(SkinVi.Lay<Button>(_popup, "btnClose"));

        // LƯỚI Ô — mock xếp 4 CỘT. Cỡ ô tính từ BỀ NGANG THẬT của container
        // (170px cứng lần trước dựa trên bề ngang đoán — container thật hẹp hơn
        // nhiều nên ô tràn đè nhau). GridLayout là chỗ chỉnh hợp lệ duy nhất.
        var luoi = SkinVi.Lay<Transform>(_popup, "itemGridContainer");
        if (luoi != null)
        {
            _luoi = luoi as RectTransform;
            var grid = luoi.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = luoi.gameObject.AddComponent<GridLayoutGroup>();

            // Đo VIEWPORT (khung nhìn cố định), KHÔNG đo Content — Content phình
            // theo nội dung (~2400px) nên "chia 4 cột" ra ô 580px: bắp to như cái
            // gối, lưới tràn ra ngoài khung (ảnh 13/08). Số cột suy từ cỡ ô đích
            // ~170px thay vì ghim cứng 4.
            var vung = _luoi != null ? _luoi.parent as RectTransform : null;
            float rong = vung != null ? vung.rect.width
                       : (_luoi != null ? _luoi.rect.width : 0f);
            if (rong < 200f) rong = 680f;
            int cot = Mathf.Clamp(Mathf.RoundToInt(rong / 170f), 4, 8);
            float o = Mathf.Floor((rong - 36f - 16f * (cot - 1)) / cot);
            grid.cellSize = new Vector2(o, Mathf.Round(o * 0.82f));
            grid.spacing = new Vector2(16f, 14f);
            grid.padding = new RectOffset(18, 18, 18, 18);
            grid.childAlignment = TextAnchor.UpperLeft;

            // Mở popup là thấy hàng ĐẦU lưới, không phải đoạn đang trôi lưng chừng.
            var thanhCuon = _luoi != null ? _luoi.GetComponentInParent<ScrollRect>(true) : null;
            if (thanhCuon != null) thanhCuon.verticalNormalizedPosition = 1f;

            // Tấm kem sau lưới — mock đặt cả lưới trên một panel giấy sáng.
            var nenLuoi = luoi.GetComponent<Image>();
            if (nenLuoi == null) nenLuoi = luoi.gameObject.AddComponent<Image>();
            nenLuoi.sprite = SkinKit.BoGoc(18f);
            nenLuoi.type = Image.Type.Sliced;
            nenLuoi.color = TaskPopupDesign.GiayTren;
            nenLuoi.raycastTarget = false;
        }

        // Thanh sức chứa (thẻ kem, góc TRÁI DƯỚI) — script game tự sinh khung này
        // với nền nâu tối + ô cam placeholder, giờ đưa về ngôn ngữ chung.
        var chuSlot = SkinVi.Lay<TMP_Text>(_popup, "txtSlotUsage");
        var khungSlot = chuSlot != null ? chuSlot.transform.parent as RectTransform : null;
        if (khungSlot != null && khungSlot != _root.transform)
        {
            SkinVi.Neo(khungSlot, new Vector2(0f, 0f), new Vector2(160f, 72f));
            var img = khungSlot.GetComponent<Image>();
            if (img != null) SkinKit.MacAoThe(img, 16f);
            foreach (var tmp in khungSlot.GetComponentsInChildren<TMP_Text>(true))
                tmp.color = TaskPopupDesign.TenBinhThuong;
            var cham = khungSlot.Find("Img_WarehouseSlotIcon");
            var chamImg = cham != null ? cham.GetComponent<Image>() : null;
            if (chamImg != null)
            {
                chamImg.sprite = SkinKit.HinhTron();          // tròn thật, không Sliced
                chamImg.type = Image.Type.Simple;
                chamImg.color = TaskPopupDesign.TdRuotDuoi;   // chấm xanh sức chứa
            }
        }

        // Nâng cấp (vàng, góc PHẢI DƯỚI) + Đưa vào bếp (xanh, GIỮA DƯỚI — logic tự
        // bật khi có món chờ chuyển) + icon xem trước bên trái nút bếp.
        var nutNang = SkinVi.Lay<Button>(_popup, "btnOpenUpgrade");
        if (nutNang != null)
        {
            SkinVi.Neo((RectTransform)nutNang.transform, new Vector2(1f, 0f), new Vector2(-160f, 72f));
            SkinKit.MacAoNut(nutNang, TaskPopupDesign.NutDiLam, 18f);
        }
        var nutBep = SkinVi.Lay<Button>(_popup, "btnSendToKitchen");
        if (nutBep != null)
        {
            var rt = (RectTransform)nutBep.transform;
            SkinVi.Neo(rt, new Vector2(0.5f, 0f), new Vector2(0f, 72f));
            SkinKit.MacAoNut(nutBep, TaskPopupDesign.NutNhan, 20f);
        }
        var xemIcon = SkinVi.Lay<Image>(_popup, "selectedPreviewIcon");
        if (xemIcon != null)
            SkinVi.Neo((RectTransform)xemIcon.transform, new Vector2(0.5f, 0f), new Vector2(-230f, 72f));
        var xemSo = SkinVi.Lay<TMP_Text>(_popup, "selectedPreviewAmount");
        if (xemSo != null)
        {
            SkinVi.Neo((RectTransform)xemSo.transform, new Vector2(0.5f, 0f), new Vector2(-170f, 72f));
            xemSo.color = TaskPopupDesign.TenBinhThuong;
        }

        // 3 modal con của Kho — cùng bảng giấy-kem, nút phân màu theo vai.
        SkinVi.MacModal(SkinVi.Lay<GameObject>(_popup, "transferPopupRoot"));
        SkinVi.MacModal(SkinVi.Lay<GameObject>(_popup, "upgradePopupRoot"));
        SkinVi.MacModal(SkinVi.Lay<GameObject>(_popup, "missingPopupRoot"));

        // Font tròn của mock cho toàn popup (không có font vỏ thì đây là no-op).
        SkinKit.ApFont(_root.transform);
    }

    /// <summary>
    /// Ô vật phẩm — prefab `item_1` đã có sẵn đúng cấu trúc mock: icon + số +
    /// tấm `khung` sau số. Chỉ tô: thân trắng, `khung` thành huy hiệu nâu bo góc,
    /// số trắng đậm. `khung` nằm SAU chữ trong prefab (che mất số) → đẩy xuống dưới.
    /// </summary>
    private void MacAoSlot(WarehouseSlotUI slot)
    {
        var rt = (RectTransform)slot.transform;
        Vector2 oCell = rt.rect.size;                        // đã ép layout trước khi vào đây
        if (oCell.x < 40f) oCell = new Vector2(150f, 123f);

        // Ô trống bị Button disabled NHÂN MÀU XÁM đè lên màu vỏ (Unity mặc định
        // disabledColor ≈ 0.78) — ảnh 13/08 cả lưới xám xịt vì thế. Độ mờ ô trống
        // do vỏ quản bằng ThanTrong, tint của Button đưa về trắng.
        var nutO = slot.GetComponent<Button>();
        if (nutO != null)
        {
            var mau = nutO.colors;
            mau.disabledColor = Color.white;
            nutO.colors = mau;
        }

        Image img = slot.GetComponent<Image>();
        Image vienImg = null;
        if (img != null)
        {
            SkinKit.MacAoThe(img, 16f, bongDo: true);
            img.color = ThanCoDo;
            // Viền do MacAoThe sinh dùng cỡ lúc gọi — chốt lại theo Ô THẬT.
            var vien = slot.transform.Find("Skin_Border") as RectTransform;
            if (vien != null)
            {
                vien.sizeDelta = oCell + new Vector2(6f, 6f);
                vienImg = vien.GetComponent<Image>();
            }
        }

        var chu = slot.GetComponentInChildren<TMP_Text>(true);
        if (chu != null)
        {
            chu.color = Color.white;
            chu.fontStyle = FontStyles.Bold;
            chu.enableAutoSizing = true;
            chu.fontSizeMin = 12f;
            chu.fontSizeMax = 26f;
        }

        // icon + số + huy hiệu: toạ độ prefab viết cho ô cỡ cũ — neo lại theo ô thật.
        var icon = slot.transform.Find("icon") as RectTransform;
        if (icon != null)
        {
            SkinVi.Neo(icon, new Vector2(0.5f, 0.5f), new Vector2(0f, oCell.y * 0.07f));
            icon.sizeDelta = Vector2.one * Mathf.Min(oCell.x, oCell.y) * 0.54f;
            var iconImg = icon.GetComponent<Image>();
            if (iconImg != null) iconImg.preserveAspect = true;
        }
        Vector2 gocSo = new Vector2(oCell.x * 0.5f - 34f, -oCell.y * 0.5f + 20f);
        var soRt = chu != null ? chu.transform as RectTransform : null;
        if (soRt != null)
        {
            SkinVi.Neo(soRt, new Vector2(0.5f, 0.5f), gocSo);
            soRt.sizeDelta = new Vector2(64f, 30f);
            chu.alignment = TextAlignmentOptions.Center;
        }

        var khung = slot.transform.Find("khung");
        var khungImg = khung != null ? khung.GetComponent<Image>() : null;
        if (khungImg != null && chu != null)
        {
            khungImg.sprite = SkinKit.BoGoc(13f);
            khungImg.type = Image.Type.Sliced;
            khungImg.color = new Color(0.243f, 0.157f, 0.063f, 0.8f);
            var krt = (RectTransform)khung;
            SkinVi.Neo(krt, new Vector2(0.5f, 0.5f), gocSo);
            krt.sizeDelta = new Vector2(58f, 28f);
            if (khung.GetSiblingIndex() > chu.transform.GetSiblingIndex())
                khung.SetSiblingIndex(chu.transform.GetSiblingIndex());
        }

        _oTheoDoi.Add((chu, khungImg, img, vienImg));
        SkinKit.ApFont(slot);
    }

    private static Transform TimTheoTen(Transform goc, string ten)
    {
        if (goc.name == ten) return goc;
        for (int i = 0; i < goc.childCount; i++)
        {
            var kq = TimTheoTen(goc.GetChild(i), ten);
            if (kq != null) return kq;
        }
        return null;
    }
}
