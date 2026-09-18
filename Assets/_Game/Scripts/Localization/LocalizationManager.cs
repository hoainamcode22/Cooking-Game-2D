using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HỆ ĐA NGÔN NGỮ VN / EN — bản nhẹ, tra theo CHÍNH CHUỖI TIẾNG VIỆT làm khoá.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO KHÔNG DÙNG Unity Localization package (quyết định Lead, vòng 13):
///   ① Phần lớn UI dự án này được dựng bằng CODE (`new GameObject` + `CreateText`), không phải
///      kéo thả trong scene. Unity Localization mạnh nhất khi gắn `LocalizeStringEvent` lên
///      component có sẵn — ở đây phần lớn không có component để gắn.
///   ② Package bắt buộc chạy trên Addressables; dự án chưa có Addressables ⇒ thêm rủi ro lớn.
///   ③ Dùng package thì VẪN phải sửa từng dòng code để thay chuỗi bằng key — công sức y hệt,
///      mà đội thêm một tầng hạ tầng nữa phải bảo trì.
///
/// THIẾT KẾ MẤU CHỐT — **khoá chính là câu tiếng Việt**:
///   Thay vì bắt Dev đặt key mới cho 3.178 chuỗi (dễ đặt trùng, dễ gõ sai, phải nhớ), ta tra
///   thẳng bằng câu tiếng Việt đang có trong code:
///
///       txt.text = "Cửa hàng";              →   txt.text = Loc.T("Cửa hàng");
///
///   Ưu điểm: đọc code vẫn hiểu ngay đang hiện chữ gì; sai key là không thể xảy ra; và
///   **chuỗi chưa dịch thì tự trả về nguyên tiếng Việt** — không bao giờ lòi ra "MISSING_KEY"
///   trước mặt người chơi. Nhược điểm: sửa câu tiếng Việt thì phải sửa cả bảng — chấp nhận được.
///
/// CÁCH DÙNG
///   • Chuỗi trong code:  `Loc.T("Cửa hàng")`
///   • Text trong scene:  gắn component <see cref="LocalizedText"/> lên TMP_Text (tự lấy chữ
///     đang có làm khoá, tự đổi khi người chơi bấm cờ).
///   • Nghe sự kiện:      `Loc.OnChanged += ...` để tự vẽ lại UI dựng bằng code.
///
/// [Localization]
/// </summary>
public static class LocalizationManager
{
    public const string PREF_KEY = "GAME_LANGUAGE";   // trùng key SettingsPopupUI đang dùng
    public const string VI = "vi";
    public const string EN = "en";

    private static string _lang = EN;
    private static bool   _daKhoiTao;

    /// <summary>Bắn sau khi ngôn ngữ đã đổi. UI dựng bằng code nên nghe cái này để vẽ lại.</summary>
    public static event Action<string> OnChanged;

    /// <summary>Mã ngôn ngữ hiện tại ("vi" / "en").</summary>
    public static string Current
    {
        get { KhoiTao(); return _lang; }
    }

    public static bool DangTiengAnh => Current == EN;

    // ═══════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Đọc lựa chọn đã lưu. Gọi tự động trước Scene đầu tiên nên mọi Awake/Start đều
    /// thấy đúng ngôn ngữ, không phụ thuộc thứ tự khởi tạo.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void KhoiTao()
    {
        if (_daKhoiTao) return;
        _daKhoiTao = true;
        _lang = PlayerPrefs.GetString(PREF_KEY, EN);
        if (_lang != VI && _lang != EN) _lang = EN;

        // [FIX 2026-09-06] Bat bo dich chay nen: dich MOI chu tren man hinh, khong phai boc
        // Loc.T() cho tung file UI. Dang tieng Viet thi no khong lam gi ca.
        LocRuntimeInterceptor.KhoiTao();
        if (_lang == EN) LocRuntimeInterceptor.QuetVaDich();
    }

    /// <summary>
    /// Đổi ngôn ngữ, lưu lại, rồi báo cho toàn bộ UI. Gọi lại cùng ngôn ngữ ⇒ không làm gì
    /// (tránh vẽ lại UI vô ích khi người chơi bấm liên tục vào lá cờ đang chọn).
    /// </summary>
    public static void SetLanguage(string lang)
    {
        KhoiTao();
        // [FIX 2026-09-16] Truoc day coerce ve VI trong khi boot coerce ve EN (:66) => hai duong
        // khoi tao cho ra hai ngon ngu khac nhau. Tieng Anh gio la ngon ngu chinh => ca hai ve EN.
        if (lang != VI && lang != EN) lang = EN;
        if (lang == _lang) return;

        _lang = lang;
        PlayerPrefs.SetString(PREF_KEY, lang);
        PlayerPrefs.Save();

        Debug.Log($"[Loc] Đổi ngôn ngữ → {lang}");
        OnChanged?.Invoke(lang);   // LocRuntimeInterceptor cung nghe su kien nay va quet lai ngay
    }

    /// <summary>
    /// Dịch một câu. Đang ở tiếng Việt, hoặc chưa có bản dịch ⇒ TRẢ VỀ NGUYÊN CÂU GỐC.
    /// Nhờ vậy bọc `T(...)` vào chỗ nào cũng an toàn, kể cả khi bảng dịch chưa có câu đó.
    /// </summary>
    public static string T(string cauTiengViet)
    {
        if (string.IsNullOrEmpty(cauTiengViet)) return cauTiengViet;
        KhoiTao();
        if (_lang == VI) return cauTiengViet;

        if (LocStringTable.EN.TryGetValue(cauTiengViet, out string en) && !string.IsNullOrEmpty(en))
            return en;

        // [FIX 06/09/2026] Bảng gõ Kiểu Tên Riêng ("Cà Rốt", "Phở Bò Tái") nhưng asset và code
        // lại viết thường ("Cà rốt", "Phở bò tái") ⇒ tra khớp từng ký tự là TRƯỢT, người chơi
        // bấm English vẫn thấy tiếng Việt. Tra lại lần hai, bỏ qua hoa/thường, rồi chỉnh kiểu
        // chữ của bản dịch cho khớp câu gốc (câu gốc IN HOA thì bản dịch cũng IN HOA).
        if (BangBoQuaHoaThuong.TryGetValue(cauTiengViet, out string en2) && !string.IsNullOrEmpty(en2))
            return KhopKieuChu(cauTiengViet, en2);

        // [FIX 2026-09-16] "Khu dat", "MO O CAP 40", "vang", "kim cuong" — tieng Viet ĐA BI BO DAU
        // (assets cu / code go voi). Hai vong tra o tren deu truot vi khoa trong bang CO dau.
        // Vong ba: bo dau ca hai ben roi tra lai. Bang phu dung mot lan, cache vinh vien.
        if (BangBoDau.TryGetValue(BoDau(cauTiengViet), out string en3) && !string.IsNullOrEmpty(en3))
            return KhopKieuChu(cauTiengViet, en3);

        GhiNhanThieu(cauTiengViet);
        return cauTiengViet;
    }

    /// <summary>
    /// Tra thu mot cau: CO ban dich thi tra true + dat <paramref name="en"/>.
    /// Dung cho cho code dung chuoi ghep (`$"..."`) — hoi truoc roi tu quyet dinh ghep the nao,
    /// thay vi ghep xong moi goi T() (luc do chuoi da khong con la khoa cua bang nua).
    /// KHONG ghi vao so cau thieu (day la phep hoi, khong phai luot dich hong).
    /// </summary>
    public static bool TryT(string cauTiengViet, out string en)
    {
        en = null;
        if (string.IsNullOrEmpty(cauTiengViet)) return false;
        KhoiTao();

        if (LocStringTable.EN.TryGetValue(cauTiengViet, out string e1) && !string.IsNullOrEmpty(e1))
        { en = e1; return true; }

        if (BangBoQuaHoaThuong.TryGetValue(cauTiengViet, out string e2) && !string.IsNullOrEmpty(e2))
        { en = KhopKieuChu(cauTiengViet, e2); return true; }

        if (BangBoDau.TryGetValue(BoDau(cauTiengViet), out string e3) && !string.IsNullOrEmpty(e3))
        { en = KhopKieuChu(cauTiengViet, e3); return true; }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BANG TRA KHONG DAU
    // ═══════════════════════════════════════════════════════════════════════
    // Khong dung string.Normalize(FormD): build IL2CPP bat Invariant Globalization, chuan hoa
    // Unicode khong bao dam co mat => tu map tay 134 ky tu, chac chan chay moi nen tang.
    private static readonly string[] _nhomCoDau =
    {
        "àáạảãâầấậẩẫăằắặẳẵ", "èéẹẻẽêềếệểễ", "ìíịỉĩ",
        "òóọỏõôồốộổỗơờớợởỡ", "ùúụủũưừứựửữ", "ỳýỵỷỹ", "đ",
        "ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ", "ÈÉẸẺẼÊỀẾỆỂỄ", "ÌÍỊỈĨ",
        "ÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠ", "ÙÚỤỦŨƯỪỨỰỬỮ", "ỲÝỴỶỸ", "Đ"
    };
    private static readonly char[] _chuGocTuongUng =
    { 'a', 'e', 'i', 'o', 'u', 'y', 'd', 'A', 'E', 'I', 'O', 'U', 'Y', 'D' };

    private static Dictionary<char, char> _mapBoDau;

    /// <summary>Bo toan bo dau tieng Viet khoi chuoi. Chuoi khong co dau tra ve chinh no (khong cap phat).</summary>
    public static string BoDau(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        if (_mapBoDau == null)
        {
            _mapBoDau = new Dictionary<char, char>(140);
            for (int n = 0; n < _nhomCoDau.Length; n++)
            {
                string nhom = _nhomCoDau[n];
                char goc = _chuGocTuongUng[n];
                for (int i = 0; i < nhom.Length; i++) _mapBoDau[nhom[i]] = goc;
            }
        }

        char[] dem = null;
        for (int i = 0; i < s.Length; i++)
        {
            if (!_mapBoDau.TryGetValue(s[i], out char thay)) continue;
            if (dem == null) dem = s.ToCharArray();
            dem[i] = thay;
        }
        return dem == null ? s : new string(dem);
    }

    private static Dictionary<string, string> _bangBoDau;

    /// <summary>Ban sao bang dich voi khoa DA BO DAU, tra khong phan biet hoa/thuong. Dung mot lan.</summary>
    private static Dictionary<string, string> BangBoDau
    {
        get
        {
            if (_bangBoDau == null)
            {
                _bangBoDau = new Dictionary<string, string>(
                    LocStringTable.EN.Count, StringComparer.OrdinalIgnoreCase);

                foreach (var cap in LocStringTable.EN)
                {
                    string khoa = BoDau(cap.Key);
                    if (string.IsNullOrEmpty(khoa)) continue;
                    if (!_bangBoDau.ContainsKey(khoa)) _bangBoDau[khoa] = cap.Value;   // gap truoc thi giu
                }
            }
            return _bangBoDau;
        }
    }

    private static Dictionary<string, string> _bangBoQuaHoaThuong;

    /// <summary>Bản sao của bảng dịch, tra KHÔNG phân biệt hoa/thường. Dựng một lần lúc cần.</summary>
    private static Dictionary<string, string> BangBoQuaHoaThuong
    {
        get
        {
            if (_bangBoQuaHoaThuong == null)
            {
                _bangBoQuaHoaThuong = new Dictionary<string, string>(
                    LocStringTable.EN.Count, StringComparer.OrdinalIgnoreCase);

                foreach (var cap in LocStringTable.EN)
                {
                    // Khoá chỉ khác nhau hoa/thường ⇒ giữ cái GẶP TRƯỚC, không ghi đè.
                    if (!_bangBoQuaHoaThuong.ContainsKey(cap.Key))
                        _bangBoQuaHoaThuong[cap.Key] = cap.Value;
                }
            }
            return _bangBoQuaHoaThuong;
        }
    }

    /// <summary>Câu gốc IN HOA HẾT ⇒ trả bản dịch IN HOA. Còn lại giữ nguyên bản dịch.</summary>
    private static string KhopKieuChu(string goc, string en)
    {
        bool coChuCai = false;
        for (int i = 0; i < goc.Length; i++)
        {
            if (char.IsLower(goc[i])) return en;
            if (char.IsUpper(goc[i])) coChuCai = true;
        }
        return coChuCai ? en.ToUpperInvariant() : en;
    }

    /// <summary>
    /// Dịch CHUỖI MẪU rồi mới ghép tham số: `Loc.TF("Mở ở cấp {0}", lv)` tra khoá `"Mở ở cấp {0}"`
    /// (nguyên văn, còn nguyên `{0}`) trong bảng, lấy `"Unlocks at level {0}"`, xong mới `Format`.
    /// Thứ tự này là bắt buộc — ghép trước rồi tra thì `"Mở ở cấp 40"` KHÔNG BAO GIỜ là khoá.
    /// (Đã rà 2026-09-16: đúng thứ tự, giữ nguyên.)
    /// `args` rỗng ⇒ bỏ qua Format luôn, tránh nuốt mất cặp `{}` có thật trong câu.
    /// </summary>
    public static string TF(string cauTiengViet, params object[] args)
    {
        string mau = T(cauTiengViet);
        if (args == null || args.Length == 0) return mau;
        try   { return string.Format(mau, args); }
        catch { return mau; }   // mẫu sai định dạng thì thà hiện thô còn hơn ném lỗi ra người chơi
    }

    /// <summary>
    /// Câu nào CHƯA có trong bảng dịch (dùng cho tool kiểm kê, không gọi lúc chơi).
    /// </summary>
    public static bool DaCoBanDich(string cauTiengViet)
    {
        return !string.IsNullOrEmpty(cauTiengViet)
            && (LocStringTable.EN.ContainsKey(cauTiengViet) || BangBoQuaHoaThuong.ContainsKey(cauTiengViet));
    }

    public static int SoCauDaDich => LocStringTable.EN.Count;

    // ═══════════════════════════════════════════════════════════════════════
    // SO CAU CHUA DICH — CHI TRONG EDITOR / DEVELOPMENT BUILD
    // ═══════════════════════════════════════════════════════════════════════
    // Ban Release: toan bo khoi nay bien mat khi bien dich, GhiNhanThieu() rong tuech,
    // khong HashSet, khong file, khong mot byte nao phat sinh trong T().
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const int THIEU_TOI_DA = 4000;
    private static readonly HashSet<string> _cauThieu = new HashSet<string>();
    private static bool _daNgheThoat;

    /// <summary>So cau tieng Viet da goi T() ma bang dich khong co (chi dem o Editor/Dev build).</summary>
    public static int SoCauThieu => _cauThieu.Count;

    private static void GhiNhanThieu(string vi)
    {
        if (_lang != EN) return;                       // dang tieng Viet thi khong tinh la thieu
        if (string.IsNullOrEmpty(vi)) return;
        if (_cauThieu.Count >= THIEU_TOI_DA) return;
        if (!_cauThieu.Add(vi)) return;

        if (!_daNgheThoat)
        {
            _daNgheThoat = true;
            Application.quitting += XuatFileCauThieu;   // dump mot lan luc thoat game
        }
    }

    /// <summary>
    /// Ghi danh sach cau CHUA DICH ra <c>Application.persistentDataPath/loc_missing.txt</c>.
    /// Boc try/catch toan bo: log thieu ma lam crash game thi phan tac dung.
    /// </summary>
    public static void XuatFileCauThieu()
    {
        try
        {
            if (_cauThieu.Count == 0) return;
            var dong = new List<string>(_cauThieu);
            dong.Sort(StringComparer.Ordinal);
            string f = System.IO.Path.Combine(Application.persistentDataPath, "loc_missing.txt");
            System.IO.File.WriteAllLines(f, dong, System.Text.Encoding.UTF8);
            Debug.Log($"[Loc] Da ghi {dong.Count} cau CHUA DICH -> {f}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Loc] Khong ghi duoc loc_missing.txt: {e.Message}");
        }
    }
#else
    /// <summary>Ban Release: khong lam gi (JIT/IL2CPP loai bo hoan toan loi goi nay).</summary>
    private static void GhiNhanThieu(string vi) { }
#endif
}

/// <summary>Bí danh ngắn cho <see cref="LocalizationManager"/> — gõ `Loc.T("...")` cho gọn.</summary>
public static class Loc
{
    public static string T(string vi) => LocalizationManager.T(vi);
    public static string TF(string vi, params object[] a) => LocalizationManager.TF(vi, a);

    /// <summary>Co ban dich cho cau nay khong? Dung truoc khi ghep chuoi bang `$"..."`.</summary>
    public static bool TryT(string vi, out string en) => LocalizationManager.TryT(vi, out en);

    /// <summary>Doi ngon ngu ("vi" / "en") — luu PlayerPrefs va ban OnChanged.</summary>
    public static void SetLanguage(string lang) => LocalizationManager.SetLanguage(lang);

    /// <summary>UI vua dung xong chu moi (mo popup, build list) ⇒ goi cai nay de dich ngay.</summary>
    public static void RequestRescan() => LocRuntimeInterceptor.RequestRescan();
    public static string Current => LocalizationManager.Current;
    public static bool DangTiengAnh => LocalizationManager.DangTiengAnh;

    public static event Action<string> OnChanged
    {
        add    { LocalizationManager.OnChanged += value; }
        remove { LocalizationManager.OnChanged -= value; }
    }
}
