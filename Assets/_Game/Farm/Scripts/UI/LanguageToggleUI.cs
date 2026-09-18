using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NÚT ĐỔI NGÔN NGỮ trong màn Cài đặt — Tiếng Việt ⇄ English.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// ── LÝ DO TỒN TẠI (số liệu rà trên project thật, 16/09/2026) ─────────────────
/// `grep -rn "SetLanguage" Assets/**/*.cs` = **0 kết quả** ngoài chính file định nghĩa.
/// Nghĩa là toàn bộ hệ đa ngôn ngữ (<see cref="LocalizationManager"/>,
/// <see cref="LocRuntimeInterceptor"/>, bảng 3.000 câu) ĐÃ CHẠY ĐƯỢC nhưng
/// **không có một cái nút nào gọi nó** — người chơi không có cách gì đổi ngôn ngữ,
/// game khoá cứng ở giá trị PlayerPrefs đang lưu. File này là cái cửa còn thiếu.
///
/// ── CÁCH DÙNG ───────────────────────────────────────────────────────────────
/// Gắn component này lên object CHA của hai con chip ngôn ngữ trong màn Cài đặt
/// (ví dụ hàng chip cạnh nhãn "Ngôn ngữ"), rồi:
///   • Kéo hai Button vào <c>nutVI</c> / <c>nutEN</c>, HOẶC
///   • Chuột phải component → <b>"Auto-find buttons in children"</b> để tự dò theo tên, HOẶC
///   • Không làm gì cả — lúc chạy nó TỰ dò con nào có tên chứa "VI"/"Viet" và "EN"/"Eng".
/// Mọi ô kéo-thả đều là TUỲ CHỌN và được kiểm tra null; điền thiếu thì phần đó bị bỏ qua,
/// không ném lỗi.
///
/// ── AN TOÀN ─────────────────────────────────────────────────────────────────
///   • Không tự dựng UI, không đụng layout — chỉ nối onClick và tô màu trạng thái.
///   • Gọi đúng API public đang có: <see cref="LocalizationManager.SetLanguage"/>
///     (hàm đó tự lưu PlayerPrefs "GAME_LANGUAGE" và tự bắn <c>OnChanged</c>).
///   • Nghe <c>Loc.OnChanged</c> để đồng bộ highlight kể cả khi ngôn ngữ bị đổi từ chỗ khác;
///     huỷ đăng ký ở OnDisable VÀ OnDestroy (đăng ký trùng là không thể vì có cờ chặn).
///   • MẸO ĐẶT TÊN: đặt tên object chữ trên chip là "Txt_VI [NoLoc]" / "Txt_EN [NoLoc]"
///     để bộ dịch chạy nền không dịch mất chính nhãn của nút chọn ngôn ngữ.
///
/// [Localization]
/// </summary>
[DisallowMultipleComponent]
public class LanguageToggleUI : MonoBehaviour
{
    [Header("Nút (tuỳ chọn — để trống thì tự dò trong con)")]
    [Tooltip("Nút chọn TIẾNG VIỆT.")]
    [SerializeField] private Button nutVI;

    [Tooltip("Nút chọn ENGLISH.")]
    [SerializeField] private Button nutEN;

    [Header("Nhãn chữ trên nút (tuỳ chọn — chỉ dùng để tô màu)")]
    [SerializeField] private TMP_Text nhanVI;
    [SerializeField] private TMP_Text nhanEN;

    [Header("Ảnh highlight (tuỳ chọn — bật/tắt theo nút đang chọn)")]
    [SerializeField] private Image highlightVI;
    [SerializeField] private Image highlightEN;

    [Header("Màu trạng thái")]
    [Tooltip("Màu chữ của ngôn ngữ ĐANG CHỌN.")]
    [SerializeField] private Color mauDangChon = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Màu chữ của ngôn ngữ KHÔNG chọn.")]
    [SerializeField] private Color mauKhongChon = new Color(1f, 1f, 1f, 0.45f);

    [Tooltip("BẬT: nút của ngôn ngữ đang chọn bị khoá bấm (phản hồi rõ hơn). " +
             "TẮT: cả hai nút luôn bấm được.")]
    [SerializeField] private bool khoaNutDangChon = false;

    private bool _daNghe;

    // ═══════════════════════════════════════════════════════════════════════
    // VÒNG ĐỜI
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        TuDoNutNeuThieu();
        NoiSuKienNut();
    }

    private void OnEnable()
    {
        TuDoNutNeuThieu();     // phòng khi object được bật lần đầu sau khi UI vừa dựng xong
        NoiSuKienNut();
        DangKyNghe();
        VeTrangThai(LocalizationManager.Current);
    }

    private void Start()
    {
        // Awake/OnEnable có thể chạy TRƯỚC khi layout kịp tính; vẽ lại một lần cho chắc.
        VeTrangThai(LocalizationManager.Current);
    }

    private void OnDisable()
    {
        HuyNghe();
    }

    private void OnDestroy()
    {
        HuyNghe();
        if (nutVI != null) nutVI.onClick.RemoveListener(ChonTiengViet);
        if (nutEN != null) nutEN.onClick.RemoveListener(ChonTiengAnh);
    }

    private void DangKyNghe()
    {
        if (_daNghe) return;
        _daNghe = true;
        LocalizationManager.OnChanged += VeTrangThai;
    }

    private void HuyNghe()
    {
        if (!_daNghe) return;
        _daNghe = false;
        LocalizationManager.OnChanged -= VeTrangThai;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NỐI NÚT
    // ═══════════════════════════════════════════════════════════════════════

    private void NoiSuKienNut()
    {
        if (nutVI != null)
        {
            nutVI.onClick.RemoveListener(ChonTiengViet);   // chống nối trùng khi bật/tắt object
            nutVI.onClick.AddListener(ChonTiengViet);
        }
        if (nutEN != null)
        {
            nutEN.onClick.RemoveListener(ChonTiengAnh);
            nutEN.onClick.AddListener(ChonTiengAnh);
        }
    }

    public void ChonTiengViet() => Chon(LocalizationManager.VI);
    public void ChonTiengAnh()  => Chon(LocalizationManager.EN);

    /// <summary>Đổi ngôn ngữ. Bấm lại đúng ngôn ngữ đang chọn ⇒ SetLanguage tự bỏ qua.</summary>
    public void Chon(string lang)
    {
        LocalizationManager.SetLanguage(lang);
        // SetLanguage bắn OnChanged ⇒ VeTrangThai chạy theo. Nhưng nếu bấm lại đúng ngôn ngữ
        // đang chọn thì nó KHÔNG bắn ⇒ tự vẽ lại ở đây cho chắc (rẻ, không gây nháy).
        VeTrangThai(LocalizationManager.Current);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VẼ TRẠNG THÁI
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Tô lại highlight/màu chữ theo ngôn ngữ hiện tại. Ô nào trống thì bỏ qua ô đó.</summary>
    private void VeTrangThai(string lang)
    {
        bool laVI = (lang == LocalizationManager.VI);

        if (highlightVI != null) highlightVI.enabled = laVI;
        if (highlightEN != null) highlightEN.enabled = !laVI;

        if (nhanVI != null) nhanVI.color = laVI ? mauDangChon : mauKhongChon;
        if (nhanEN != null) nhanEN.color = laVI ? mauKhongChon : mauDangChon;

        if (khoaNutDangChon)
        {
            if (nutVI != null) nutVI.interactable = !laVI;
            if (nutEN != null) nutEN.interactable = laVI;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TỰ DÒ NÚT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dò hai nút trong các object con theo TÊN (không phân biệt hoa/thường):
    /// chứa "viet"/"vi" ⇒ nút Tiếng Việt; chứa "eng"/"en" ⇒ nút English.
    /// Chỉ điền vào ô còn TRỐNG — đã kéo tay thì không bao giờ bị ghi đè.
    /// </summary>
    private void TuDoNutNeuThieu()
    {
        if (nutVI != null && nutEN != null) return;

        var nut = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < nut.Length; i++)
        {
            var b = nut[i];
            if (b == null) continue;

            string ten = b.gameObject.name.ToLowerInvariant();

            // Xét VI TRƯỚC: "Btn_Vietnamese" phải rơi vào nhánh VI, không phải nhánh EN.
            if (nutVI == null && b != nutEN && (Chua(ten, "viet") || LaTu(ten, "vi") || LaTu(ten, "vn")))
            {
                nutVI = b;
                continue;
            }
            if (nutEN == null && b != nutVI && (Chua(ten, "eng") || LaTu(ten, "en")))
            {
                nutEN = b;
                continue;
            }
        }

        // Nhãn chữ: lấy TMP_Text đầu tiên trong mỗi nút nếu Sếp chưa kéo.
        if (nhanVI == null && nutVI != null) nhanVI = nutVI.GetComponentInChildren<TMP_Text>(true);
        if (nhanEN == null && nutEN != null) nhanEN = nutEN.GetComponentInChildren<TMP_Text>(true);
    }

    /// <summary>Có chứa chuỗi con không (tên đã hạ về chữ thường).</summary>
    private static bool Chua(string ten, string mau)
    {
        return ten.IndexOf(mau, System.StringComparison.Ordinal) >= 0;
    }

    /// <summary>
    /// Mã 2 chữ cái ("vi", "en") chỉ khớp khi đứng RIÊNG thành một từ — "Btn_VI", "Lang VI",
    /// "vi_flag" thì khớp; "Preview", "Divider" thì KHÔNG. Phân tách bằng ký tự không phải chữ cái.
    /// </summary>
    private static bool LaTu(string ten, string ma)
    {
        // "BtnVI", "ChipEN" — mã dính liền ở CUỐI tên vẫn tính là khớp (cách đặt tên rất hay gặp).
        if (ten.EndsWith(ma, System.StringComparison.Ordinal)) return true;

        int i = 0;
        while (true)
        {
            i = ten.IndexOf(ma, i, System.StringComparison.Ordinal);
            if (i < 0) return false;

            bool truocOk = (i == 0) || !char.IsLetter(ten[i - 1]);
            int sau = i + ma.Length;
            bool sauOk = (sau >= ten.Length) || !char.IsLetter(ten[sau]);
            if (truocOk && sauOk) return true;

            i = sau;
        }
    }

    /// <summary>
    /// Bấm chuột phải lên component trong Inspector → chạy dò tự động và ghi kết quả ra Console.
    /// Dùng lúc dựng scene cho nhanh, không ảnh hưởng gì lúc chơi.
    /// </summary>
    [ContextMenu("Auto-find buttons in children")]
    private void TuDoNutTuMenu()
    {
        nutVI = null;
        nutEN = null;
        TuDoNutNeuThieu();

        Debug.Log($"[LanguageToggleUI] Da do: VI = {(nutVI != null ? nutVI.name : "KHONG THAY")}, " +
                  $"EN = {(nutEN != null ? nutEN.name : "KHONG THAY")}", this);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
