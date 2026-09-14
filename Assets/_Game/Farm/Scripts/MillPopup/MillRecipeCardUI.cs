using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MỘT CARD CÔNG THỨC trong danh sách "CÔNG THỨC" bên trái popup.
///
/// ══ ĐỐI CHIẾU full_mill_ui.html ══
///     .recipe-card                → nền, đổi sprite theo 3 trạng thái
///     .recipe-card.active         → spriteActive   (card đang được chọn)
///     .recipe-card (thường)       → spriteInactive
///     .recipe-card.recipe-locked  → spriteLocked + phủ mờ + ổ khoá "Mở ở cấp 14"
///     .animal-tag                 → imgBadge + txtBadge      "Gà"
///     .recipe-icon-circle         → imgIcon
///     .recipe-name                → txtName                  "Cám cho gà"
///     .recipe-time                → txtBrewTime               "Ủ 2p00"
///     .cost-row .cost-chip ×2     → imgIng1/txtIng1, imgIng2/txtIng2   "x3" / "x2"
///
/// Card CHỈ trình bày, không tự quyết định gì. Bấm vào thì phát `OnClicked` cho
/// `MillPopupUI` xử lý. Card khoá thì nút bị tắt nên không phát sự kiện.
/// </summary>
[DisallowMultipleComponent]
public class MillRecipeCardUI : MonoBehaviour
{
    // ─────────────────────────── THAM CHIẾU (Dev B wire) ───────────────────────────

    [Header("Ảnh")]
    [Tooltip("Nền card. Code đổi sprite giữa spriteActive / spriteInactive / spriteLocked.")]
    [SerializeField] private Image imgBg;

    [Tooltip("Icon sản phẩm trong vòng tròn bên trái card.")]
    [SerializeField] private Image imgIcon;

    [Tooltip("Icon con vật cạnh nhãn góc phải.")]
    [SerializeField] private Image imgBadge;

    [Tooltip("Icon chip nguyên liệu thứ 1.")]
    [SerializeField] private Image imgIng1;

    [Tooltip("Icon chip nguyên liệu thứ 2. Công thức chỉ có 1 nguyên liệu ⇒ chip này tự ẩn.")]
    [SerializeField] private Image imgIng2;

    [Header("Chữ")]
    [Tooltip("Tên công thức: \"Cám cho gà\".")]
    [SerializeField] private TMP_Text txtName;

    [Tooltip("Thời gian ủ. Code ghi kèm tiền tố: \"Ủ 2p00\".")]
    [SerializeField] private TMP_Text txtBrewTime;

    [Tooltip("Nhãn con vật: \"Gà\".")]
    [SerializeField] private TMP_Text txtBadge;

    [Tooltip("Số lượng nguyên liệu 1: \"x3\".")]
    [SerializeField] private TMP_Text txtIng1;

    [Tooltip("Số lượng nguyên liệu 2: \"x2\".")]
    [SerializeField] private TMP_Text txtIng2;

    [Header("Nút")]
    [Tooltip("Nút phủ toàn card. Code tự tắt interactable khi card bị khoá.")]
    [SerializeField] private Button btnSelect;

    [Header("Sprite nền theo trạng thái")]
    [Tooltip("Nền khi card ĐANG ĐƯỢC CHỌN (HTML: .recipe-card.active).")]
    [SerializeField] private Sprite spriteActive;

    [Tooltip("Nền bình thường (HTML: .recipe-card).")]
    [SerializeField] private Sprite spriteInactive;

    [Tooltip("Nền khi chưa đủ cấp (HTML: .recipe-card.recipe-locked).")]
    [SerializeField] private Sprite spriteLocked;

    [Header("Khoá — TUỲ CHỌN (để trống vẫn chạy)")]
    [Tooltip("TUỲ CHỌN. Lớp phủ ổ khoá của card khoá (HTML: .recipe-lock-overlay).")]
    [SerializeField] private GameObject lockOverlay;

    [Tooltip("TUỲ CHỌN. Chữ trong lớp phủ: \"Mở ở cấp 14\" (HTML: .lock-text).")]
    [SerializeField] private TMP_Text txtLockText;

    [Tooltip("Độ mờ của card khoá. Bản thiết kế cho cảm giác ~0.55.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float alphaKhiKhoa = 0.55f;

    [Header("Anh thay the khi thieu icon — TUY CHON")]
    [Tooltip("TUY CHON. Sprite hien trong o icon khi cong thuc chua co icon (hoac icon " +
             "bi hong tham chieu). De trong ⇒ chi to xam, van thay o icon chu khong bien mat.")]
    [SerializeField] private Sprite spriteThieuIcon;

    [Tooltip("Mau to o icon khi dang dung anh thay the. Xam nhat de doc ra ngay la 'thieu art'.")]
    [SerializeField] private Color mauKhiThieuIcon = new Color(0.72f, 0.72f, 0.72f, 0.65f);

    [Header("Bo cuc")]
    [Tooltip("BAT = code ep lai fontSize / vi tri / kich thuoc cua ten + gio u moi lan Bind, " +
             "dung dung so cu (26pt, 156x30 tai 148,-24 / 20pt, 156x24 tai 148,-58).\n" +
             "TAT (mac dinh) = prefab thang: moi chinh sua trong prefab duoc giu nguyen.\n" +
             "Chi bat lai neu co card nao bi mot he thong khac keo lech bo cuc va can reset.")]
    [SerializeField] private bool epLayoutTheoCode = false;

    // Khoang co chu cho phep khi auto-size. Can tren giu dung co thiet ke, can duoi
    // la muc con doc duoc tren dien thoai.
    private const float CO_CHU_TEN_MIN = 20f;
    private const float CO_CHU_TEN_MAX = 26f;
    private const float CO_CHU_GIO_MIN = 16f;
    private const float CO_CHU_GIO_MAX = 20f;

    // ─────────────────────────── SỰ KIỆN ───────────────────────────

    /// <summary>Người chơi bấm chọn card này. Không phát khi card bị khoá.</summary>
    public System.Action<MillRecipeData> OnClicked;

    // ─────────────────────────── TRẠNG THÁI ───────────────────────────

    /// <summary>Công thức đang gắn với card này.</summary>
    public MillRecipeData Recipe => _recipe;

    /// <summary>Card có mở (đủ cấp) hay không.</summary>
    public bool IsUnlocked => _unlocked;

    /// <summary>
    /// Sprite đang hiện trong vòng tròn icon của card.
    ///
    /// Dùng bởi <see cref="MillRecipeDragSource"/> để vẽ bóng kéo GIỐNG HỆT cái người chơi
    /// đang nhìn. Đọc từ `imgIcon.sprite` chứ không từ `_recipe.icon`: nếu sau này icon
    /// đổi theo cấp/skin thì bóng kéo tự đúng, không phải sửa hai nơi.
    /// </summary>
    public Sprite IconSprite => (imgIcon != null) ? imgIcon.sprite : null;

    private MillRecipeData _recipe;
    private bool           _unlocked;
    private bool           _selected;
    private CanvasGroup    _canvasGroup;

    private void Awake()
    {
        // CanvasGroup là cách rẻ nhất để làm mờ CẢ card (nền + icon + chữ + chip) bằng
        // một giá trị. Nếu không có sẵn thì tự thêm.
        // ⚠ KHÔNG viết `GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>()`:
        //   component thiếu trả về "fake-null", `??` so tham chiếu nên coi như CÓ, không thêm
        //   gì cả, rồi dòng sau chạm `.alpha` là nổ NullReference/MissingComponent.
        //   Phải so tường minh `== null` (Unity đã nạp chồng toán tử này cho Object).
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (btnSelect != null)
        {
            btnSelect.onClick.RemoveAllListeners();
            btnSelect.onClick.AddListener(BamChon);
        }
    }

    // ─────────────────────────── API CÔNG KHAI ───────────────────────────

    /// <summary>
    /// Nạp dữ liệu công thức vào card.
    /// </summary>
    /// <param name="r">Công thức. null ⇒ card tự ẩn.</param>
    /// <param name="unlocked">Đã đủ cấp chưa. false ⇒ nền khoá, mờ, không bấm được.</param>
    public void Bind(MillRecipeData r, bool unlocked)
    {
        _recipe   = r;
        _unlocked = unlocked;

        if (r == null)
        {
            // Card dư (pool lớn hơn số công thức) thì tắt hẳn.
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (txtName != null)
        {
            txtName.text = r.displayName;

            // Chong tran chu: "Co tron cho bo" o 26pt khong vua o rong 156px cua card.
            // Prefab dang de overflowMode = Overflow va tat auto-size nen chu tran ra
            // ngoai nen card. Khong sua duoc prefab tu day nen ep bang code.
            ApChongTranChu(txtName, CO_CHU_TEN_MIN, CO_CHU_TEN_MAX);

            if (epLayoutTheoCode)
            {
                txtName.fontSize = 26f;
                var rtName = txtName.rectTransform;
                if (rtName != null)
                {
                    rtName.anchoredPosition = new Vector2(148f, -24f);
                    rtName.sizeDelta = new Vector2(156f, 30f);
                }
            }
        }

        if (txtBrewTime != null)
        {
            txtBrewTime.text = Loc.TF("Ủ {0}", r.BrewTimeLabel);   // "Ủ 2p00"

            // Nhan gio u ngan ("U 10p00") nhung ban dich khac co the dai hon nhieu,
            // vd tieng Anh "Brew 10m00". Cho no cung duoc co lai thay vi tran.
            ApChongTranChu(txtBrewTime, CO_CHU_GIO_MIN, CO_CHU_GIO_MAX);

            if (epLayoutTheoCode)
            {
                txtBrewTime.fontSize = 20f;
                var rtTime = txtBrewTime.rectTransform;
                if (rtTime != null)
                {
                    rtTime.anchoredPosition = new Vector2(148f, -58f);
                    rtTime.sizeDelta = new Vector2(156f, 24f);
                }
            }
        }

        if (txtBadge != null) txtBadge.text = r.animalTag;

        DatAnh(imgIcon,  r.icon);
        DatAnh(imgBadge, r.animalBadgeIcon);

        // Chip nguyên liệu: video vẽ tối đa 2–3, layout wire sẵn 2. Chip không có dữ liệu
        // thì ẩn cả icon lẫn chữ, không để lại ô trống.
        DatChipNguyenLieu(0, imgIng1, txtIng1, r.ingredients);
        DatChipNguyenLieu(1, imgIng2, txtIng2, r.ingredients);

        if (epLayoutTheoCode &&
            imgIng1 != null && imgIng1.transform.parent != null && imgIng1.transform.parent.parent != null)
        {
            var costRowRt = imgIng1.transform.parent.parent.GetComponent<RectTransform>();
            if (costRowRt != null)
            {
                costRowRt.anchoredPosition = new Vector2(148f, -94f);
            }
        }

        // Lớp phủ khoá + chữ "Mở ở cấp N"
        if (lockOverlay != null && lockOverlay.activeSelf != !unlocked)
            lockOverlay.SetActive(!unlocked);

        if (!unlocked && txtLockText != null)
            txtLockText.text = Loc.TF("Mở ở cấp {0}", r.unlockLevel);

        if (btnSelect != null)
            btnSelect.interactable = unlocked;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = unlocked ? 1f : alphaKhiKhoa;

            // ⚠ SỬA 21/08 — TRƯỚC ĐÂY: `blocksRaycasts = unlocked`.
            // Ý định ban đầu là "cho click xuyên xuống ScrollRect để vẫn kéo cuộn được".
            // Nhưng KHÔNG CÓ GÌ ĐỠ Ở DƯỚI: Viewport chỉ có RectMask2D (không Image),
            // RecipeList/InnerPanel đều raycastTarget = false ⇒ raycast xuyên thẳng tới
            // `Window`, và Window không phải ScrollRect. Kết quả thật: đặt ngón tay lên card
            // khoá (vd "Cám cho bò sữa", mở ở cấp 14) thì danh sách KHÔNG cuộn được chút nào.
            //
            // Nay luôn để true và để `MillRecipeDragSource` lo: card khoá không nhấc được
            // bao (nó tự kiểm `IsUnlocked`) nhưng vẫn FORWARD cú kéo cho ScrollRect ⇒ cuộn
            // được. Click vào card khoá vẫn vô hại: `btnSelect.interactable = false` và
            // `BamChon()` còn một hàng rào `!_unlocked` nữa.
            _canvasGroup.blocksRaycasts = true;
        }

        // Card khoá không bao giờ ở trạng thái được chọn.
        SetSelected(unlocked && _selected);
    }

    /// <summary>Bật/tắt viền "đang chọn". Card khoá luôn bị coi như không chọn.</summary>
    public void SetSelected(bool on)
    {
        _selected = on && _unlocked;
        ApNenTheoTrangThai();
    }

    // ─────────────────────────── NỘI BỘ ───────────────────────────

    private void ApNenTheoTrangThai()
    {
        if (imgBg == null) return;

        Sprite s;
        if (!_unlocked)     s = spriteLocked;
        else if (_selected) s = spriteActive;
        else                s = spriteInactive;

        // Bỏ qua nếu Dev B chưa gán sprite cho trạng thái đó — giữ nguyên còn hơn hoá trống.
        if (s != null && imgBg.sprite != s)
            imgBg.sprite = s;
    }

    /// <summary>
    /// Dat sprite cho mot o anh cua card.
    ///
    /// ⚠ SUA — TRUOC DAY: `img.enabled = (s != null)`, tuc thieu sprite thi TAT han o anh.
    /// Ket qua that: bon icon cam bi hong tham chieu (rect sprite trong file .meta tran ra
    /// ngoai texture 225x225) lam ca 20 o icon tra ve null, va vi o anh tu tat nen tren man
    /// hinh chi con cai vong tron nen mau kem — khong ai nhan ra la LOI, ai cung tuong
    /// "thiet ke no the". Loi song mot thoi gian dai chi vi cach an nay.
    ///
    /// Nay thieu sprite thi o anh VAN BAT: dung <see cref="spriteThieuIcon"/> neu Dev B co
    /// wire, khong thi giu nguyen sprite cu / o trong nhung to mau xam
    /// <see cref="mauKhiThieuIcon"/> ⇒ nhin la biet thieu art chu khong phai thiet ke.
    ///
    /// Duong co sprite PHAI tra mau ve trang duc: card duoc TAI DUNG (xem `_cards` trong
    /// MillPopupUI), khong tra mau thi mot card tung thieu icon se xam vinh vien du lan
    /// Bind sau da co icon that.
    /// </summary>
    private void DatAnh(Image img, Sprite s)
    {
        if (img == null) return;

        img.preserveAspect = true;

        if (s != null)
        {
            img.sprite  = s;
            img.color   = Color.white;   // tra ve trang duc — bat buoc vi card tai dung
            img.enabled = true;
            return;
        }

        // Thieu sprite: van hien o anh de loi nhin thay duoc.
        img.sprite  = spriteThieuIcon;   // co the null — Image van ve o mau tron
        img.color   = mauKhiThieuIcon;
        img.enabled = true;
    }

    /// <summary>
    /// Ep mot nhan chu khong tran ra ngoai khung: cat duoi bang dau ba cham va cho co chu
    /// tu co lai trong khoang cho phep.
    ///
    /// Prefab MillRecipeCard dang de `m_overflowMode: 0` (Overflow) va `m_enableAutoSizing: 0`
    /// cho moi nhan, nen ten dai nhu "Co tron cho bo" (14 ky tu, 27pt) tran qua khoi o rong
    /// 156px cua Txt_Name va de len nen card. Khong sua duoc prefab tu day nen ep bang code.
    /// Goi trong Bind, sau khi da gan .text.
    /// </summary>
    private void ApChongTranChu(TMP_Text txt, float coMin, float coMax)
    {
        if (txt == null) return;

        // Cat duoi bang "…" thay vi de chu tran ra ngoai nen card — dung trong CA HAI che do.
        txt.overflowMode = TextOverflowModes.Ellipsis;

        // Auto-size va fontSize la hai duong loai tru nhau trong TMP: bat auto-size thi
        // gan .fontSize khong con tac dung. Nen che do "ep layout theo code" (hanh vi cu)
        // phai TAT auto-size, de doan ma ngay duoi con set duoc 26pt/20pt nhu truoc.
        if (epLayoutTheoCode)
        {
            txt.enableAutoSizing = false;
            return;
        }

        txt.enableAutoSizing = true;
        txt.fontSizeMin      = coMin;
        txt.fontSizeMax      = coMax;
    }

    /// <summary>
    /// Điền một chip nguyên liệu (icon + "xN"), hoặc ẨN HẲN chip nếu công thức không dùng
    /// ô đó.
    ///
    /// ⚠ SỬA NGÀY 20/08 — trước đây chỉ tắt Image/Text BÊN TRONG chip, còn cái viên thuốc
    /// (GameObject "Chip_2" mang Image nền xanh) vẫn bật ⇒ công thức 1 nguyên liệu
    /// ("Cỏ trộn cho bò" chỉ có lúa) hiện một viên xanh RỖNG cạnh chip lúa, trông như lỗi
    /// thiếu icon. Cấu trúc do MillPopupBuilderTool dựng là:
    ///     Chip_1 / Chip_2   ← Image nền xanh + HorizontalLayoutGroup   (viên thuốc)
    ///        ├─ Img_Ing     ← `img`
    ///        └─ Txt_Ing     ← `txt`
    /// nên ẩn CHA của img/txt là ẩn đúng viên thuốc. LayoutGroup của Cost_Row tự co lại,
    /// chip còn lại không bị lệch.
    /// </summary>
    private static void DatChipNguyenLieu(int idx, Image img, TMP_Text txt, MillIngredient[] ds)
    {
        bool co = (ds != null && idx < ds.Length && ds[idx] != null && !string.IsNullOrEmpty(ds[idx].itemId));

        // Ẩn/hiện cả viên thuốc. Lấy cha từ img trước, không có img thì lấy từ txt.
        Transform vienThuoc = null;
        if (img != null)      vienThuoc = img.transform.parent;
        else if (txt != null) vienThuoc = txt.transform.parent;

        if (vienThuoc != null && vienThuoc.gameObject.activeSelf != co)
            vienThuoc.gameObject.SetActive(co);

        if (img != null)
        {
            img.sprite  = co ? ds[idx].icon : null;
            img.enabled = co && ds[idx].icon != null;
        }

        if (txt != null)
        {
            txt.text    = co ? ("x" + ds[idx].amount) : string.Empty;
            txt.enabled = co;
        }
    }

    private void BamChon()
    {
        // Hàng rào thứ hai sau interactable: nếu ai đó gọi onClick bằng code thì vẫn không
        // chọn được card khoá.
        if (!_unlocked || _recipe == null) return;

        if (OnClicked != null) OnClicked(_recipe);
    }
}
