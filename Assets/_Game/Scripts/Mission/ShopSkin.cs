using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VỎ MỚI CHO SHOP — theo spec "thẻ mẫu 3a" trong `Export_Popups_Chon/ShopPopup/README.md`.
///
/// Làm việc trên BẢN ĐỒ HIERARCHY THẬT (dump 13/08): card `KhungHatGiong` 600×400 gồm
/// `img_HatGiong` · `TxT_NameHatGiong` · `GiamSL` · `TăngSL` · `Mua` · `GameObject`(hộp
/// giá: img_Vang + TxT_Gia + TXT_soluong) · `LockOverlay`. Mọi thao tác chỉ là DI CHUYỂN
/// và TÔ LẠI các object đó — không xoá, không tạo thay thế — nên [SerializeField] của
/// `ShopItemUI` và mọi onClick còn nguyên.
///
/// Bố cục thẻ theo mock (card giữ 600×400, spec 280 nhân ~2):
///     tên (2 dòng, trên) → đĩa tròn kem + icon → hàng − SỐ + → nút MUA = nút giá
/// Nút giá: vàng → xanh lá · kim cương → xanh dương · khoá → xám + phủ nâu.
/// </summary>
[DisallowMultipleComponent]
public class ShopSkin : MonoBehaviour
{
    [Tooltip("Bỏ tick để về vỏ cũ mà không cần gỡ component.")]
    public bool batAo = false;

    private Transform _content;
    private readonly HashSet<Transform> _daMac = new HashSet<Transform>();
    private float _lanQuet;
    private bool _daChinhLuoi;

    private void OnEnable()
    {
        // Tắt hoàn toàn can thiệp runtime để giữ 100% UI gốc do designer thiết kế
        return;
    }

    private void Update()
    {
        return;
    }

    private void QuetVaMac()
    {
        if (_content == null)
        {
            // Đường dẫn từ dump: popup_Menu/popup_Menu_Trong/Scroll View/Viewport/Content
            var sv = transform.Find("popup_Menu_Trong/Scroll View/Viewport/Content");
            if (sv == null)
            {
                var scroll = GetComponentInChildren<ScrollRect>(true);
                if (scroll != null) _content = scroll.content;
            }
            else _content = sv;
            if (_content == null) return;
        }

        // BỐ CỤC MỚI THEO MOCK: thẻ đứng (cao hơn rộng, tỉ lệ ~1.12) xếp nhiều cột
        // gọn — layout cũ giữ cellSize nguyên bản nên thẻ to bè, "vẫn layout cũ".
        // Cỡ ô tính từ bề ngang THẬT của Content; mọi chi tiết trong thẻ tự co
        // theo (MacAoCard nhân hệ số ts từ cellSize).
        if (!_daChinhLuoi)
        {
            var grid = _content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid != null)
            {
                _daChinhLuoi = true;
                float rong = ((RectTransform)_content).rect.width;
                if (rong < 300f) rong = 1700f;
                int cot = Mathf.Max(4, Mathf.FloorToInt(rong / 340f));
                float o = Mathf.Floor((rong - 40f - 20f * (cot - 1)) / cot);
                grid.cellSize = new Vector2(o, Mathf.Round(o * 1.12f));
                grid.spacing = new Vector2(20f, 20f);
            }
        }

        // Ép layout tính xong TRƯỚC khi đo — mặc áo ở khung hình card vừa sinh là đo
        // phải kích thước template 600×400, trong khi GridLayout sẽ ép nó về ~240px.
        // Chính lệch này làm icon văng ra mép và đĩa kem to hơn cả thẻ ở lần chạy đầu.
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_content);

        for (int i = 0; i < _content.childCount; i++)
        {
            Transform c = _content.GetChild(i);
            if (c == null || _daMac.Contains(c)) continue;
            if (!c.name.StartsWith("KhungHatGiong")) continue;
            if (!c.gameObject.activeSelf) continue;          // template gốc đang tắt — bỏ qua
            MacAoCard(c);
            _daMac.Add(c);
        }

        // Font tròn của mock cho toàn popup (không có font vỏ thì đây là no-op).
        SkinKit.ApFont(this);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  KHUNG NGOÀI: ván gỗ + 3 tab + nút đóng
    // ═════════════════════════════════════════════════════════════════════════

    private void MacKhungNgoai()
    {
        // popup_Menu (root, 1759×685) → ván gỗ. Ảnh parchment cũ thay bằng gỗ nâu token.
        var goc = GetComponent<Image>();
        if (goc != null) SkinKit.MacAoVanGo(goc, 40f);

        // popup_Menu_Trong (tấm lót 1914×682) → giấy kem.
        var trong = transform.Find("popup_Menu_Trong");
        if (trong != null)
        {
            var img = trong.GetComponent<Image>();
            if (img != null) SkinKit.MacAoGiay(img, 24f);
        }

        // Viewport mang component MASK — mask cắt con theo ALPHA của chính Image này.
        // Bản đầu tôi hạ alpha về 0.001 cho "tàng hình" ảnh nền tối → stencil của mask
        // rỗng → TOÀN BỘ card bên trong bị cắt mất, shop trống trơn dù card vẫn sinh ra.
        // Cách đúng: tô ĐỤC màu giấy sáng — mask đầy trở lại, và nền trong khớp mock
        // (lưới card của thiết kế cũng nằm trên nền kem).
        foreach (string dd in new[] { "popup_Menu_Trong/Scroll View", "popup_Menu_Trong/Scroll View/Viewport" })
        {
            var t = transform.Find(dd);
            var img = t != null ? t.GetComponent<Image>() : null;
            if (img == null) continue;
            img.sprite = SkinKit.BoGoc(18f);
            img.type = Image.Type.Sliced;
            img.color = TaskPopupDesign.GiayTren;   // ĐỤC — tuyệt đối không alpha thấp ở đây
        }

        // 3 tab: HẠT GIỐNG đang chọn (sáng nối giấy), 2 tab kia lún màu đất.
        MacTab("btn_Hatgiong", true);
        MacTab("btn_Congtrinh", false);
        MacTab("btn_trangtri", false);

        // Bấm tab nào thì tab đó sáng — CHỈ AddListener thêm, không đụng listener cũ
        // của ShopManager (đổi tab vẫn là việc của nó).
        HookTab("btn_Hatgiong");
        HookTab("btn_Congtrinh");
        HookTab("btn_trangtri");

        // Ruy băng "CỬA HÀNG" giữa mép trên — popup không có tiêu đề sẵn (đã soát
        // scene: không TMP nào mang chữ này) nên đây là lớp trang trí thêm thuần tuý,
        // raycast tắt, không che nút nào.
        SkinKit.LamRuyBang(transform, "CỬA HÀNG", new Vector2(0.5f, 1f),
                           new Vector2(0f, 26f), new Vector2(560f, 104f));

        // Nút X: đỏ theo bảng nút chung.
        var dong = transform.Find("BtnClose");
        var nutDong = dong != null ? dong.GetComponent<Button>() : null;
        if (nutDong != null) SkinVi.NutDong(nutDong);   // WP-D2b: sprite đóng chuẩn (Sliced, trắng) + chữ X dự phòng; null → áo đỏ cũ

        // Thanh tìm kiếm: mock là Ô LÕM KEM #f3e2bb viền #d9b478 — ảnh gỗ tối cũ không
        // hợp với giấy sáng mới. Tô lại nền + chữ nâu nhạt, không đụng InputField logic.
        var timKiem = transform.Find("SearchBar");
        var tkImg = timKiem != null ? timKiem.GetComponent<Image>() : null;
        if (tkImg != null)
        {
            SkinKit.MacAoThe(tkImg, 18f);
            tkImg.color = TaskPopupDesign.Hex("#f3e2bb");
            var vien = timKiem.Find("Skin_Border");
            var vienImg = vien != null ? vien.GetComponent<Image>() : null;
            if (vienImg != null) vienImg.color = TaskPopupDesign.Hex("#d9b478");

            foreach (var tmp in timKiem.GetComponentsInChildren<TMP_Text>(true))
                tmp.color = TaskPopupDesign.Hex("#8d7550");
        }
    }

    private void MacTab(string ten, bool chon)
    {
        var t = transform.Find(ten);
        var img = t != null ? t.GetComponent<Image>() : null;
        if (img == null) return;

        img.sprite = SkinKit.BoGoc(20f);
        img.type = Image.Type.Sliced;
        img.color = chon ? TaskPopupDesign.TabChonDuoi : TaskPopupDesign.TabThuongDuoi;

        var chu = t.GetComponentInChildren<TMP_Text>(true);
        if (chu != null) chu.color = chon ? TaskPopupDesign.TabChuChon : TaskPopupDesign.TabChuThuong;
    }

    private void HookTab(string ten)
    {
        var t = transform.Find(ten);
        var nut = t != null ? t.GetComponent<Button>() : null;
        if (nut == null) return;
        nut.onClick.AddListener(() =>
        {
            MacTab("btn_Hatgiong", ten == "btn_Hatgiong");
            MacTab("btn_Congtrinh", ten == "btn_Congtrinh");
            MacTab("btn_trangtri", ten == "btn_trangtri");
            _daMac.Clear();     // tab mới sinh card mới

            // Mặc áo NGAY trong khung hình bấm tab: listener của ShopManager (kéo sẵn
            // trong scene) chạy TRƯỚC listener thêm lúc runtime, nên đến lượt mình
            // card mới đã tồn tại — mặc luôn, không chờ nhịp quét → hết "delay UI".
            QuetVaMac();
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  THẺ VẬT PHẨM — mẫu 3a
    // ═════════════════════════════════════════════════════════════════════════

    private void MacAoCard(Transform card)
    {
        var rt = (RectTransform)card;

        // Kích thước THẬT: GridLayout là nguồn chân lý (nó ghi đè rect của con).
        Vector2 kt = rt.rect.size;
        var grid = _content != null ? _content.GetComponent<UnityEngine.UI.GridLayoutGroup>() : null;
        if (grid != null) kt = grid.cellSize;
        if (kt.x < 50f || kt.y < 50f) kt = new Vector2(600f, 400f);

        // MỌI số đo trong hàm này viết cho thẻ 600×400 (dump). Thẻ thật có thể chỉ
        // ~240px — nhân hệ số để đĩa/icon/nút co theo, không tràn mép như lần đầu.
        float ts = Mathf.Clamp(Mathf.Min(kt.x / 600f, kt.y / 400f), 0.3f, 1.2f);

        // ── khung gỗ ngoài + lõi giấy ────────────────────────────────────────
        // Bản render mock: viền thẻ MỎNG màu nâu nhạt (#c9a06a), thân gần trắng —
        // khung dày #a96f36 lần trước nhìn nặng hơn mẫu rõ rệt.
        var nen = card.GetComponent<Image>();
        if (nen != null)
        {
            nen.sprite = SkinKit.BoGoc(26f);
            nen.type = Image.Type.Sliced;
            nen.color = TaskPopupDesign.Hex("#c9a06a");
        }
        if (card.Find("Skin_Paper") == null)
        {
            var giay = new GameObject("Skin_Paper", typeof(RectTransform), typeof(Image));
            var grt = (RectTransform)giay.transform;
            grt.SetParent(card, false);
            grt.sizeDelta = kt - new Vector2(10f, 10f);      // viền lộ ~5px như mock
            grt.SetSiblingIndex(0);                          // DƯỚI mọi nội dung có sẵn
            var gi = giay.GetComponent<Image>();
            gi.sprite = SkinKit.BoGoc(22f);
            gi.type = Image.Type.Sliced;
            gi.color = TaskPopupDesign.Hex("#fffcf3");
            gi.raycastTarget = false;
            var bong = giay.AddComponent<Shadow>();          // thẻ nổi khỏi giấy như mock
            bong.effectColor = new Color(0.35f, 0.22f, 0.05f, 0.18f);
            bong.effectDistance = new Vector2(0f, -6f);
        }

        // ── tên: 2 dòng cố định trên đầu ─────────────────────────────────────
        var ten = card.Find("TxT_NameHatGiong") as RectTransform;
        if (ten != null)
        {
            ten.anchorMin = ten.anchorMax = new Vector2(0.5f, 0.5f);
            ten.pivot = new Vector2(0.5f, 0.5f);
            ten.anchoredPosition = new Vector2(0f, kt.y * 0.5f - 52f * ts);
            ten.sizeDelta = new Vector2(kt.x - 60f * ts, 88f * ts);
            var tmp = ten.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = TaskPopupDesign.TenBinhThuong;
                tmp.enableAutoSizing = true;                 // tên dài không tràn
                tmp.fontSizeMax = 42f; tmp.fontSizeMin = 24f;
            }
        }

        // ── đĩa tròn kem + icon ──────────────────────────────────────────────
        var icon = card.Find("img_HatGiong") as RectTransform;
        if (icon != null)
        {
            if (card.Find("Skin_Disc") == null)
            {
                var dia = new GameObject("Skin_Disc", typeof(RectTransform), typeof(Image));
                var drt = (RectTransform)dia.transform;
                drt.SetParent(card, false);
                drt.anchoredPosition = new Vector2(0f, 26f * ts);
                drt.sizeDelta = new Vector2(224f * ts, 224f * ts);
                drt.SetSiblingIndex(1);                      // trên giấy, dưới icon
                var di = dia.GetComponent<Image>();
                di.sprite = SkinKit.HinhTron();              // tròn thật — BoGoc Sliced vỡ khi thẻ co
                di.type = Image.Type.Simple;
                di.color = TaskPopupDesign.Hex("#f1dfb4");
                di.raycastTarget = false;
            }

            // Neo TÂM tường minh — icon của prefab gốc neo kiểu khác, chỉ đặt
            // anchoredPosition mà không đặt anchor là nó trôi ra mép thẻ (ảnh chụp:
            // cà rốt, hoa hồng nằm ngoài card chính vì thế này).
            icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = new Vector2(0f, 26f * ts);
            icon.sizeDelta = new Vector2(168f * ts, 168f * ts);
            var ii = icon.GetComponent<Image>();
            if (ii != null) ii.preserveAspect = true;
        }

        // ── hàng stepper:  −   SỐ   +  ───────────────────────────────────────
        float yStepper = -kt.y * 0.5f + 152f * ts;
        DatNut(card, "GiamSL", new Vector2(-96f * ts, yStepper), new Vector2(72f * ts, 72f * ts),
               SkinKit.NutCam);                              // mock: − CAM, + xanh lá
        DatNut(card, "TăngSL", new Vector2(96f * ts, yStepper), new Vector2(72f * ts, 72f * ts),
               TaskPopupDesign.NutNhan);

        // TXT_soluong đang nằm TRONG hộp giá (dump) — kéo ra giữa hai nút.
        var hopGia = card.Find("GameObject") as RectTransform;
        var soLuong = hopGia != null ? hopGia.Find("TXT_soluong") as RectTransform : null;
        if (soLuong == null) soLuong = card.Find("TXT_soluong") as RectTransform; // đã kéo lần trước
        if (soLuong != null)
        {
            soLuong.SetParent(card, true);                   // ref của ShopItemUI vẫn sống
            soLuong.anchorMin = soLuong.anchorMax = new Vector2(0.5f, 0.5f);
            soLuong.pivot = new Vector2(0.5f, 0.5f);
            soLuong.anchoredPosition = new Vector2(0f, yStepper);
            soLuong.sizeDelta = new Vector2(110f * ts, 60f * ts);
            var st = soLuong.GetComponent<TMP_Text>();
            if (st != null) { st.alignment = TextAlignmentOptions.Center; st.color = TaskPopupDesign.TenBinhThuong; }
        }

        // ── NÚT GIÁ = NÚT MUA (spec) ─────────────────────────────────────────
        var mua = card.Find("Mua") as RectTransform;
        if (mua != null && hopGia != null)
        {
            mua.anchorMin = mua.anchorMax = new Vector2(0.5f, 0.5f);
            mua.pivot = new Vector2(0.5f, 0.5f);
            // Render mock: nút giá chiếm GẦN TRỌN bề ngang thẻ, sát mép dưới.
            mua.anchoredPosition = new Vector2(0f, -kt.y * 0.5f + 56f * ts);
            mua.sizeDelta = new Vector2(kt.x - 48f * ts, 84f * ts);

            // Màu theo tiền tệ: img_Vang mang sprite vàng → xanh lá; kim cương → xanh dương.
            var anhTien = hopGia.Find("img_Vang");
            var sprTien = anhTien != null ? anhTien.GetComponent<Image>() : null;
            bool laGem = sprTien != null && sprTien.sprite != null &&
                         sprTien.sprite.name.ToLowerInvariant().Contains("kim");
            var nutMua = mua.GetComponent<Button>();
            if (nutMua != null)
                SkinKit.MacAoNut(nutMua, laGem ? SkinKit.NutKimCuong : TaskPopupDesign.NutNhan, 20f);

            // Chữ "MUA" tắt đi — spec: trên nút chỉ còn icon tiền + giá. Không xoá,
            // logic có ghi text vào cũng vô hại.
            var chuMua = mua.Find("Text (TMP)");
            if (chuMua != null) chuMua.gameObject.SetActive(false);

            // Kéo hộp giá VÀO trong nút (reparent — mọi ref TxT_Gia/img_Vang còn nguyên),
            // tắt ảnh nền parchment của hộp, canh giữa.
            hopGia.SetParent(mua, false);
            hopGia.anchorMin = hopGia.anchorMax = new Vector2(0.5f, 0.5f);
            hopGia.anchoredPosition = Vector2.zero;
            var hgImg = hopGia.GetComponent<Image>();
            if (hgImg != null) hgImg.enabled = false;

            if (anhTien != null)
            {
                var art = (RectTransform)anhTien;
                art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
                art.pivot = new Vector2(0.5f, 0.5f);
                art.anchoredPosition = new Vector2(-52f * ts, 0f);
                art.sizeDelta = new Vector2(52f * ts, 52f * ts);
            }
            var gia = hopGia.Find("TxT_Gia") as RectTransform;
            if (gia != null)
            {
                gia.anchorMin = gia.anchorMax = new Vector2(0.5f, 0.5f);
                gia.pivot = new Vector2(0.5f, 0.5f);
                gia.anchoredPosition = new Vector2(30f * ts, 0f);
                gia.sizeDelta = new Vector2(150f * ts, 60f * ts);
                var gt = gia.GetComponent<TMP_Text>();
                if (gt != null) { gt.alignment = TextAlignmentOptions.Center; gt.color = Color.white; }
            }
        }

        // ── khoá cấp: phủ nâu + chữ trắng (spec) ─────────────────────────────
        var khoa = card.Find("LockOverlay");
        if (khoa != null)
        {
            var ki = khoa.GetComponent<Image>();
            if (ki != null)
            {
                ki.sprite = SkinKit.BoGoc(26f);
                ki.type = Image.Type.Sliced;
                ki.color = new Color(0.243f, 0.157f, 0.063f, 0.62f);   // rgba(62,40,16)
            }
            var kt2 = khoa.GetComponentInChildren<TMP_Text>(true);
            if (kt2 != null) kt2.color = Color.white;
            khoa.SetAsLastSibling();   // khoá phải phủ TRÊN mọi thứ của thẻ
        }
    }

    private void DatNut(Transform card, string ten, Vector2 viTri, Vector2 kt,
                        TaskPopupDesign.KieuNut kieu)
    {
        var t = card.Find(ten) as RectTransform;
        if (t == null) return;
        t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
        t.anchoredPosition = viTri;
        t.sizeDelta = kt;
        var nut = t.GetComponent<Button>();
        if (nut != null) SkinKit.MacAoNut(nut, kieu, kt.x * 0.5f);   // bo = nửa cạnh → tròn
    }
}
