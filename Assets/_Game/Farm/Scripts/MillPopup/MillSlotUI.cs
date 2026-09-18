using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 5 TRẠNG THÁI của một ô trong khu "SLOT XAY" — khớp 1–1 với 5 kiểu slot trong video.
/// </summary>
public enum MillSlotMode
{
    /// <summary>Đang xay: icon + tên + thanh tiến độ + đồng hồ "1p56" + nút kim cương tăng tốc.</summary>
    Running,

    /// <summary>Xay xong, chờ thu: icon + tên + nút THU xanh + chấm đỏ góc phải.</summary>
    ReadyToCollect,

    /// <summary>Đã mở nhưng chưa có gì trong đó.</summary>
    Empty,

    /// <summary>Chưa mở, MUA ĐƯỢC bằng kim cương.</summary>
    UnlockGem,

    /// <summary>Chưa mở, KHÔNG mua được — phải lên cấp. Video slot #5: "Chưa đủ cấp / Cấp 18".</summary>
    LockedLevel
}

/// <summary>
/// MỘT SLOT XAY — thuần trình bày, KHÔNG giữ logic thời gian.
///
/// ══ AI QUYẾT ĐỊNH GÌ ══
/// `MillPopupUI` là nơi duy nhất giữ trạng thái (công thức nào, còn bao lâu, đã mở chưa) và
/// nó GỌI `BindRunning()` MỖI FRAME cho slot đang chạy. Slot chỉ vẽ ra những gì được đưa.
/// Tách như vậy để save/load và bù thời gian offline chỉ phải làm ở đúng một chỗ.
///
/// ══ CHỐNG RÁC MỖI FRAME (cạm bẫy #3) ══
/// `BindRunning` chạy 60 lần/giây × 5 slot. Nó chỉ dựng LẠI chuỗi khi:
///   • số giây NGUYÊN đổi          → đồng hồ "1p56"
///   • giá kim cương đổi           → nút "x6"
///   • công thức đổi               → tên + icon
/// Còn lại chỉ gán `fillAmount` (float, không cấp phát). ⇒ ~1 chuỗi nhỏ mỗi giây mỗi slot,
/// thay vì 60. ĐỪNG bỏ các hàng rào `_giayDangHien` / `_giaGemDangHien` này.
///
/// ══ YÊU CẦU SETUP CHO DEV B ══
/// `imgProgressFill` phải là Image có **Image Type = Filled, Fill Method = Horizontal,
/// Fill Origin = Left**. Code điều khiển qua `fillAmount`; nếu để Type = Simple thì thanh
/// tiến độ đứng yên mà không báo lỗi gì — đây là lỗi wire khó thấy nhất của file này.
///
/// ══ NƠI NHẬN CÚ THẢ (kéo-thả) ══
/// Slot là ĐÍCH của thao tác kéo bao nguyên liệu từ danh sách công thức (xem
/// <see cref="MillRecipeDragSource"/>). Nó hiện thực `IDropHandler` nên EventSystem tự gọi
/// <see cref="OnDrop"/> khi người chơi nhả ngón tay trên slot này.
///
/// Hai điều kiện BẮT BUỘC để OnDrop nổ, cả hai đều được `Awake` tự bảo đảm:
///   • node slot phải có một Graphic ăn raycast — `imgBg.raycastTarget = true`;
///   • bóng kéo phải KHÔNG ăn raycast (đã xử trong `MillDragSession`).
/// Unity phát OnDrop bằng `ExecuteEvents.ExecuteHierarchy` nên thả vào nút THU / nút tăng
/// tốc (là node con) cũng nổi lên tới đây — không cần vùng thả riêng.
///
/// ══ VIỀN SÁNG KHI KÉO — KÊNH VẼ RIÊNG ══
/// `MillPopupUI.Update` là nơi DUY NHẤT được vẽ 5 root trạng thái, thông qua hàng rào
/// `_modeDaVe`. Viền sáng lúc kéo KHÔNG đi qua <see cref="SetMode"/> mà bật/tắt riêng node
/// `dropHighlight` (<see cref="SetDropHighlight"/>). Nếu nhồi nó thành một MillSlotMode mới
/// thì hàng rào sẽ coi "đang sáng" là một trạng thái và xoá mất trạng thái thật của slot.
/// </summary>
[DisallowMultipleComponent]
public class MillSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ─────────────────────────── THAM CHIẾU (Dev B wire) ───────────────────────────

    [Header("Chữ")]
    [Tooltip("Số thứ tự góc trên trái: \"#1\".")]
    [SerializeField] private TMP_Text txtIndex;

    [Tooltip("Tên sản phẩm đang xay: \"Cám gà\".")]
    [SerializeField] private TMP_Text txtName;

    [Tooltip("Đồng hồ đếm ngược nằm trên thanh tiến độ: \"1p56\".")]
    [SerializeField] private TMP_Text txtTimer;

    [Tooltip("Số kim cương. Dùng cho nút MỞ SLOT (mode UnlockGem).\n" +
             "Nếu bạn KHÔNG wire txtSpeedUpCost thì field này cũng được dùng cho nút tăng tốc.")]
    [SerializeField] private TMP_Text txtGemCost;

    [Tooltip("Chữ khoá: \"Chưa đủ cấp\".")]
    [SerializeField] private TMP_Text txtLockLabel;

    [Header("Chữ — TUỲ CHỌN (Dev A thêm, để trống vẫn chạy)")]
    [Tooltip("TUỲ CHỌN. Nhãn cấp yêu cầu, viên thuốc dưới cùng slot khoá: \"Cấp 18\".\n" +
             "ĐỂ TRỐNG ⇒ code dồn cả hai dòng vào txtLockLabel dạng \"Chưa đủ cấp\\nCấp 18\".")]
    [SerializeField] private TMP_Text txtLockLevelValue;

    [Tooltip("TUỲ CHỌN. Giá kim cương trên nút TĂNG TỐC của slot đang xay: \"x6\".\n" +
             "ĐỂ TRỐNG ⇒ code dùng txtGemCost. Chỉ cần wire riêng nếu hai nút nằm ở hai " +
             "root khác nhau (rootRunning vs rootUnlockGem) — mà thực tế là vậy.")]
    [SerializeField] private TMP_Text txtSpeedUpCost;

    [Header("Ảnh")]
    [Tooltip("Nền thẻ slot — đổi màu/sprite giữa slot thường và slot khoá.")]
    [SerializeField] private Image imgBg;

    [Tooltip("Icon sản phẩm trong vòng tròn giữa slot.")]
    [SerializeField] private Image imgIcon;

    [Tooltip("Phần XANH của thanh tiến độ.\n" +
             "⚠ BẮT BUỘC: Image Type = Filled · Fill Method = Horizontal · Fill Origin = Left.")]
    [SerializeField] private Image imgProgressFill;

    [Tooltip("Ổ khoá tròn của slot chưa mở.")]
    [SerializeField] private Image imgLockIcon;

    [Header("Nhóm theo trạng thái — bật đúng MỘT cái")]
    [SerializeField] private GameObject rootRunning;
    [SerializeField] private GameObject rootReady;
    [SerializeField] private GameObject rootEmpty;
    [SerializeField] private GameObject rootUnlockGem;
    [SerializeField] private GameObject rootLockedLevel;

    [Header("Nút")]
    [Tooltip("Nút THU xanh (mode ReadyToCollect).")]
    [SerializeField] private Button btnCollect;

    [Tooltip("Nút kim cương xanh dương tăng tốc (mode Running).")]
    [SerializeField] private Button btnSpeedUp;

    [Tooltip("Nút mở slot bằng kim cương (mode UnlockGem).")]
    [SerializeField] private Button btnUnlockGem;

    [Header("Khác")]
    [Tooltip("Chấm đỏ nhắc \"có hàng chờ thu\", góc dưới phải slot.")]
    [SerializeField] private GameObject redDot;

    [Tooltip("TUỲ CHỌN. Viền sáng phủ slot, hiện khi người chơi đang KÉO một công thức và " +
             "slot này nhận được. Để trống ⇒ kéo-thả vẫn chạy, chỉ là không có gợi ý thị giác.")]
    [SerializeField] private GameObject dropHighlight;

    [Tooltip("Độ mờ viền sáng khi slot CHỈ đang sẵn sàng nhận (con trỏ ở nơi khác).")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaVienSanSang = 0.45f;

    [Tooltip("Độ mờ viền sáng khi con trỏ ĐANG Ở TRÊN slot này.")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaVienHover = 1f;

    // ─────────────────────────── SỰ KIỆN ───────────────────────────

    /// <summary>Người chơi bấm THU.</summary>
    public System.Action OnCollect;

    /// <summary>Người chơi bấm nút kim cương tăng tốc.</summary>
    public System.Action OnSpeedUp;

    /// <summary>Người chơi bấm mở slot bằng kim cương.</summary>
    public System.Action OnUnlock;

    /// <summary>
    /// Người chơi THẢ một công thức vào slot này. Tham số là công thức đang được kéo.
    /// `MillPopupUI` gán handler trong `GanSuKienSlot()` (gán thẳng, không dùng +=).
    /// </summary>
    public System.Action<MillRecipeData> OnDropRecipe;

    /// <summary>
    /// Hỏi `MillPopupUI` xem slot này có nhận được cú thả không (đã mở? còn trống?).
    /// Chỉ dùng để quyết định VIỀN SÁNG — việc chặn thật vẫn nằm ở
    /// `MillPopupUI.ThaVaoSlot`, không tin vào UI.
    /// null ⇒ coi như nhận được (chỉ mất phần gợi ý, không sai logic).
    /// </summary>
    public System.Func<bool> CoTheNhanTha;

    // ─────────────────────────── TRẠNG THÁI TRÌNH BÀY ───────────────────────────

    /// <summary>Trạng thái đang hiển thị.</summary>
    public MillSlotMode Mode => _mode;

    private MillSlotMode   _mode = MillSlotMode.Empty;
    private MillRecipeData _congThucDangHien;
    private int            _giayDangHien   = int.MinValue;   // hàng rào chống dựng chuỗi mỗi frame
    private int            _giaGemDangHien = int.MinValue;
    private int            _chiSoDangHien  = int.MinValue;
    private Image          _imgVienSang;

    private void Awake()
    {
        // RemoveAllListeners chỉ xoá listener gắn bằng CODE, không xoá listener kéo trong
        // Inspector ⇒ an toàn, và chặn được việc đăng ký trùng nếu Awake chạy lại.
        if (btnCollect != null)
        {
            btnCollect.onClick.RemoveAllListeners();
            btnCollect.onClick.AddListener(BamThu);
        }

        if (btnSpeedUp != null)
        {
            btnSpeedUp.onClick.RemoveAllListeners();
            btnSpeedUp.onClick.AddListener(BamTangToc);
        }

        if (btnUnlockGem != null)
        {
            btnUnlockGem.onClick.RemoveAllListeners();
            btnUnlockGem.onClick.AddListener(BamMoSlot);
        }

        // BẮT BUỘC cho kéo-thả: EventSystem chỉ gửi OnDrop tới node nằm DƯỚI con trỏ, và
        // chỉ "thấy" node có Graphic ăn raycast. MillPopupBuilderTool dựng ảnh nền slot với
        // raycastTarget = false (mặc định của MillUI.Img) ⇒ không bật ở đây thì thả vào
        // vùng trống của slot rơi vào hư không mà không có lỗi nào để lần ra.
        if (imgBg != null && !imgBg.raycastTarget)
            imgBg.raycastTarget = true;

        // Prefab có thể được lưu lúc viền sáng đang bật.
        BatRoot(dropHighlight, false);

        TatOKhoaTrung();
        ApChongTranChuSlot();
    }

    /// <summary>
    /// ⚠ SỬA 17/09 — "SLOT #4 VÀ #5 HIỆN HAI Ổ KHOÁ ĐÈ LÊN NHAU, LỆCH NHAU MỘT CHÚT".
    ///
    /// CẢ HAI ổ khoá đều do prefab/scene dựng sẵn, KHÔNG có cái nào do code Instantiate:
    ///
    ///   Slot_N
    ///    └─ Img_LockIcon   84×84, top-center @ y −44   sprite = shop_lock_badge.png
    ///        └─ Glyph_Lock 46×46, căn giữa cha          sprite = mill_glyph_lock
    ///
    /// `MillPopupBuilderTool` (dòng ~2527 và ~3706) vẽ `Glyph_Lock` là vì — nguyên văn —
    /// "shop_lock_badge.png 64×64: đĩa nâu đậm có vành sáng, BÊN TRONG KHÔNG có hình ổ khoá
    /// (đã soi ảnh thật)". Nhận định đó NAY ĐÃ SAI: file art đã bị thay
    /// (Assets/Assetsgame/popup/ui_shop_svg/generated_sprites/shop_lock_badge.png) và bản
    /// hiện tại LÀ một đĩa nâu CÓ ổ khoá vàng vẽ sẵn bên trong. Ổ khoá trắng 46px vẽ đè lên
    /// thành cái thứ hai; hai hình lệch nhau vì quai khoá của art nằm cao hơn tâm đĩa còn
    /// glyph thì căn đúng tâm.
    ///
    /// Giữ lại ĐĨA (nó là ô [SerializeField] `imgLockIcon` mà SetMode bật/tắt theo trạng
    /// thái) và TẮT cái glyph thừa. Chỉ SetActive(false) — KHÔNG Destroy: đảo lại art thành
    /// đĩa trơn thì chỉ cần bật node này lên trong Inspector là xong.
    /// </summary>
    private void TatOKhoaTrung()
    {
        if (imgLockIcon == null) return;

        Transform goc = imgLockIcon.transform;
        for (int i = 0; i < goc.childCount; i++)
        {
            Transform con = goc.GetChild(i);
            if (con == null) continue;

            // Tìm theo TÊN để không đụng nhầm node khác ai đó thêm sau này.
            if (con.name.IndexOf("Glyph", System.StringComparison.OrdinalIgnoreCase) < 0) continue;

            if (con.gameObject.activeSelf)
                con.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Chống tràn chữ cho các nhãn khung CỨNG của slot. Bản tiếng Anh dài hơn hẳn:
    /// Txt_Name rộng 146.4px @21pt mà "Cattle Feed Mix" là 15 ký tự, Txt_LockLabel rộng
    /// 152px @20pt mà "Level too low" là 13 ký tự. Auto-size CHỈ-CO (trần = cỡ đang có,
    /// sàn = 0.75×) + cắt đuôi "…" — bản tiếng Việt ngắn không đổi một pixel nào.
    /// </summary>
    private void ApChongTranChuSlot()
    {
        MillPopupUI.ApChongTranChu(txtName);
        MillPopupUI.ApChongTranChu(txtLockLabel);
        MillPopupUI.ApChongTranChu(txtLockLevelValue);
        MillPopupUI.ApChongTranChu(txtTimer);
        MillPopupUI.ApChongTranChu(txtGemCost);
        MillPopupUI.ApChongTranChu(txtSpeedUpCost);
    }

    // ─────────────────────────── API CÔNG KHAI ───────────────────────────

    /// <summary>
    /// Bật đúng một nhóm trạng thái, tắt hết còn lại. Đây là hàm DUY NHẤT được đổi
    /// active của 5 root — đừng bật/tắt chúng từ ngoài, sẽ có lúc hai root cùng bật.
    /// </summary>
    public void SetMode(MillSlotMode m)
    {
        _mode = m;

        BatRoot(rootRunning,     m == MillSlotMode.Running);
        BatRoot(rootReady,       m == MillSlotMode.ReadyToCollect);
        BatRoot(rootEmpty,       m == MillSlotMode.Empty);
        BatRoot(rootUnlockGem,   m == MillSlotMode.UnlockGem);
        BatRoot(rootLockedLevel, m == MillSlotMode.LockedLevel);

        // Chấm đỏ CHỈ có nghĩa ở trạng thái chờ thu.
        BatRoot(redDot, m == MillSlotMode.ReadyToCollect);

        // Ổ khoá dùng chung cho hai kiểu chưa mở.
        //
        // ⚠ SỬA 21/08 — "MỞ SLOT RỒI MÀ Ổ KHOÁ VẪN CÒN".
        // Bản trước chỉ đặt `imgLockIcon.enabled = false`. Nhưng `.enabled` của một Image
        // CHỈ ẩn Image của ĐÚNG node đó — mọi node CON vẫn tiếp tục render bình thường.
        // `MillPopupBuilderTool` dựng `Glyph_Lock` (hình ổ khoá TRẮNG) làm CON của node ổ
        // khoá, nên nó nằm lại và đè lên bao thức ăn ở slot đã mở.
        // Phải tắt cả GameObject mới ẩn được nhánh con.
        if (imgLockIcon != null)
        {
            bool khoa = (m == MillSlotMode.UnlockGem || m == MillSlotMode.LockedLevel);
            imgLockIcon.enabled = khoa;

            if (imgLockIcon.gameObject.activeSelf != khoa)
                imgLockIcon.gameObject.SetActive(khoa);
        }

        // Đổi mode ⇒ xoá hàng rào để lần Bind kế tiếp chắc chắn vẽ lại chữ.
        _giayDangHien   = int.MinValue;
        _giaGemDangHien = int.MinValue;
    }

    /// <summary>
    /// Cập nhật slot ĐANG XAY. An toàn để gọi MỖI FRAME — bên trong tự chặn việc dựng
    /// chuỗi khi số giây chưa đổi.
    /// </summary>
    /// <param name="r">Công thức đang xay.</param>
    /// <param name="remainSec">Giây còn lại (đã trừ cả thời gian offline).</param>
    /// <param name="totalSec">Tổng giây của lượt xay, để tính tỉ lệ thanh tiến độ.</param>
    /// <param name="gemCost">Giá kim cương để hoàn thành ngay, hiện trên nút xanh dương.</param>
    public void BindRunning(MillRecipeData r, float remainSec, float totalSec, int gemCost)
    {
        if (_mode != MillSlotMode.Running)
            SetMode(MillSlotMode.Running);

        DatCongThuc(r);

        // ── Thanh tiến độ: float, không cấp phát, cập nhật mượt mỗi frame ──
        if (imgProgressFill != null)
        {
            float tienDo = 0f;
            if (totalSec > 0f)
                tienDo = Mathf.Clamp01(1f - (remainSec / totalSec));   // ĐÃ XAY được bao nhiêu
            imgProgressFill.fillAmount = tienDo;
        }

        // ── Đồng hồ: chỉ dựng chuỗi khi số giây nguyên đổi ──
        int giay = Mathf.CeilToInt(Mathf.Max(0f, remainSec));
        if (giay != _giayDangHien)
        {
            _giayDangHien = giay;
            if (txtTimer != null)
                txtTimer.text = MillTimeFormat.PhutGiay(giay);
        }

        // ── Giá tăng tốc: chỉ dựng chuỗi khi giá đổi ──
        if (gemCost != _giaGemDangHien)
        {
            _giaGemDangHien = gemCost;
            TMP_Text oGia = txtSpeedUpCost != null ? txtSpeedUpCost : txtGemCost;
            if (oGia != null)
                oGia.text = "x" + gemCost;
        }
    }

    /// <summary>Chuyển slot sang trạng thái XONG, chờ người chơi bấm THU.</summary>
    public void BindReady(MillRecipeData r)
    {
        SetMode(MillSlotMode.ReadyToCollect);
        DatCongThuc(r);

        // Đầy thanh cho khớp cảm giác "đã xong" nếu thanh vẫn còn hiện ở layout của Dev B.
        if (imgProgressFill != null)
            imgProgressFill.fillAmount = 1f;

        if (txtTimer != null)
            txtTimer.text = string.Empty;
    }

    /// <summary>Slot đã mở nhưng trống.</summary>
    public void BindEmpty()
    {
        SetMode(MillSlotMode.Empty);
        DatCongThuc(null);

        if (imgProgressFill != null)
            imgProgressFill.fillAmount = 0f;

        if (txtTimer != null)
            txtTimer.text = string.Empty;
    }

    /// <summary>Slot chưa mở, mua được bằng kim cương.</summary>
    /// <param name="gemCost">Giá mở, video: 15.</param>
    /// <param name="duGem">Người chơi có đủ kim cương không — quyết định nút bấm được hay không.</param>
    public void BindUnlockGem(int gemCost, bool duGem)
    {
        SetMode(MillSlotMode.UnlockGem);
        DatCongThuc(null);

        // ⚠ AN TOÀN ĐỂ GỌI MỖI FRAME (MillPopupUI.VeSlotChuaMo gọi như vậy từ 17/09).
        // Hàng rào `_giaGemDangHien` chặn việc dựng chuỗi khi CON SỐ chưa đổi ⇒ không rác GC.
        // `SetMode` đặt lại hàng rào về int.MinValue mỗi lần ĐỔI mode nên lần Bind đầu sau
        // khi đổi trạng thái luôn ghi thật.
        if (txtGemCost != null)
        {
            // Điều kiện thứ hai cứu đúng ca lỗi trong ảnh chụp: ô giá đang RỖNG (chuỗi gốc
            // của prefab chưa bao giờ bị ghi đè) thì phải ghi, kể cả khi con số không đổi.
            if (gemCost != _giaGemDangHien || string.IsNullOrEmpty(txtGemCost.text))
            {
                _giaGemDangHien = gemCost;
                txtGemCost.text = gemCost.ToString();
            }
        }

        // KHÔNG CÓ GIÁ ĐỂ HIỆN ⇒ ẩn hẳn thanh giá thay vì để một thanh xanh rỗng. Thanh rỗng
        // trông y như lỗi thiếu dữ liệu, mà đây là trạng thái hợp lệ (config đặt giá 0).
        if (btnUnlockGem != null)
        {
            bool coGia = (gemCost > 0);
            if (btnUnlockGem.gameObject.activeSelf != coGia)
                btnUnlockGem.gameObject.SetActive(coGia);

            btnUnlockGem.interactable = duGem;
        }
    }

    /// <summary>Slot chưa mở và KHÔNG mua được — chờ lên cấp. Video: "Chưa đủ cấp" + "Cấp 18".</summary>
    public void BindLockedLevel(int capYeuCau)
    {
        SetMode(MillSlotMode.LockedLevel);
        DatCongThuc(null);

        Color colLabel = new Color(0.35f, 0.25f, 0.16f, 1f); // #594029 Nâu đậm rõ nét
        Color colLevel = new Color(0.65f, 0.18f, 0.12f, 1f); // #A62E1F Đỏ gạch nổi bật

        if (rootLockedLevel != null)
        {
            var imgs = rootLockedLevel.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < imgs.Length; i++)
            {
                if (imgs[i] != null && imgs[i].gameObject.name.Contains("Pill"))
                {
                    imgs[i].color = new Color(0.92f, 0.86f, 0.79f, 1f); // #EADBCA Nền kem ấm bo góc
                }
            }
        }

        if (txtLockLevelValue != null)
        {
            // Hai ô riêng — đúng như video (chữ nâu + viên thuốc xám).
            if (txtLockLabel != null)
            {
                // Khoá "Chưa đủ cấp" ĐÃ CÓ trong LocStringTable (dòng 131 → "Level too low");
                // bản trước gán thẳng chuỗi tiếng Việt nên bản tiếng Anh phải chờ lượt quét
                // của LocRuntimeInterceptor mới đổi được — nhấp nháy tiếng Việt một nhịp.
                txtLockLabel.text = Loc.T("Chưa đủ cấp");
                txtLockLabel.color = colLabel;
            }
            txtLockLevelValue.text = Loc.TF("Cấp {0}", capYeuCau);
            txtLockLevelValue.color = colLevel;
        }
        else if (txtLockLabel != null)
        {
            // Dev B chưa wire ô thứ hai ⇒ dồn hai dòng vào một ô để không mất thông tin cấp.
            txtLockLabel.text = Loc.TF("Chưa đủ cấp\nCấp {0}", capYeuCau);
            txtLockLabel.color = colLabel;
        }
    }

    /// <summary>Đặt nhãn số thứ tự: 1 → "#1". Gọi một lần lúc dựng popup.</summary>
    public void SetIndexLabel(int i)
    {
        if (i == _chiSoDangHien) return;
        _chiSoDangHien = i;

        if (txtIndex != null)
            txtIndex.text = "#" + i;
    }

    /// <summary>Đổi sprite nền slot (Dev B có thể dùng để phân biệt slot khoá).</summary>
    public void SetBackground(Sprite s)
    {
        if (imgBg != null && s != null)
            imgBg.sprite = s;
    }

    /// <summary>
    /// Bật/tắt viền sáng "thả được vào đây".
    ///
    /// KÊNH VẼ RIÊNG — không đi qua <see cref="SetMode"/>, xem khối ghi chú đầu file.
    /// </summary>
    /// <param name="sanSang">Đang có phiên kéo và slot này nhận được.</param>
    /// <param name="dangHover">Con trỏ đang ở trên slot này (sáng đậm hơn).</param>
    public void SetDropHighlight(bool sanSang, bool dangHover)
    {
        BatRoot(dropHighlight, sanSang);
        if (!sanSang || dropHighlight == null) return;

        Image v = VienSang;
        if (v == null) return;

        Color c = v.color;
        c.a = dangHover ? alphaVienHover : alphaVienSanSang;
        v.color = c;
    }

    // ─────────────────────────── KÉO-THẢ ───────────────────────────

    /// <summary>
    /// Người chơi nhả ngón tay trên slot này. Unity gọi hàm này TRƯỚC
    /// `MillRecipeDragSource.OnEndDrag`, nên `MillDragSession.Recipe` còn nguyên giá trị.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        SetDropHighlight(false, false);

        if (!MillDragSession.IsDragging) return;

        // Cú thả này phải đến từ ĐÚNG ngón tay đang giữ phiên kéo. Hai ngón cùng lúc: ngón
        // A nhấc "Cám gà", ngón B nhấc "Cám heo" (bị Bat() từ chối), ngón B nhả tay trên
        // slot #2 ⇒ không kiểm thì slot #2 xay "Cám gà" và trừ nguyên liệu của ngón A.
        if (eventData != null && !MillDragSession.ThuocVe(eventData.pointerId)) return;

        MillRecipeData r = MillDragSession.Recipe;

        // Ghi nhận TRƯỚC khi gọi handler: handler có thể hiện toast / mở popup khác, và
        // bên gửi cần biết "đã có người nhận" để không báo huỷ kéo.
        MillDragSession.GhiNhanTha();

        if (OnDropRecipe != null) OnDropRecipe(r);
    }

    /// <summary>Con trỏ vào slot trong lúc đang kéo ⇒ viền sáng đậm lên.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!MillDragSession.IsDragging) return;
        if (!NhanDuoc()) return;

        SetDropHighlight(true, true);
    }

    /// <summary>
    /// Con trỏ rời slot. Nếu vẫn đang kéo thì hạ về mức "sẵn sàng" chứ không tắt hẳn —
    /// tắt hẳn sẽ làm cả hàng slot nhấp nháy khi người chơi lướt ngón tay qua.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (MillDragSession.IsDragging && NhanDuoc())
        {
            SetDropHighlight(true, false);
            return;
        }

        SetDropHighlight(false, false);
    }

    private bool NhanDuoc()
    {
        return (CoTheNhanTha == null) || CoTheNhanTha();
    }

    private Image VienSang
    {
        get
        {
            if (dropHighlight == null) return null;

            if (_imgVienSang == null)
                _imgVienSang = dropHighlight.GetComponent<Image>();

            return _imgVienSang;
        }
    }

    // ─────────────────────────── NỘI BỘ ───────────────────────────

    private void DatCongThuc(MillRecipeData r)
    {
        // So tham chiếu: chỉ vẽ lại tên + icon khi ĐỔI công thức, không phải mỗi frame.
        if (_congThucDangHien == r) return;
        _congThucDangHien = r;

        if (txtName != null)
            txtName.text = (r != null) ? r.displayName : string.Empty;

        if (imgIcon != null)
        {
            Sprite s = (r != null) ? r.GetIcon() : null;
            imgIcon.sprite  = s;
            // Ẩn hẳn ô icon khi không có sprite, tránh hiện ô vuông trắng mặc định của UGUI.
            imgIcon.enabled = (s != null);
        }
    }

    private static void BatRoot(GameObject go, bool on)
    {
        // KHÔNG dùng `go?.SetActive` cho Unity Object: `?.` dùng phép so null của C#, còn
        // object Unity đã Destroy là "fake-null" — phép so của C# cho là KHÁC null rồi gọi
        // vào object chết. Luôn so tường minh với `== null` (toán tử này Unity đã nạp chồng).
        if (go == null) return;

        // Chỉ gọi khi thực sự đổi: SetActive kéo theo dựng lại layout của cả nhánh con.
        if (go.activeSelf != on)
            go.SetActive(on);
    }

    private void BamThu()
    {
        if (OnCollect != null) OnCollect();
    }

    private void BamTangToc()
    {
        if (OnSpeedUp != null) OnSpeedUp();
    }

    private void BamMoSlot()
    {
        if (OnUnlock != null) OnUnlock();
    }
}
