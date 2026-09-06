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

    private static string _lang = VI;
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
        _lang = PlayerPrefs.GetString(PREF_KEY, VI);
        if (_lang != VI && _lang != EN) _lang = VI;

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
        if (lang != VI && lang != EN) lang = VI;
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

        return LocStringTable.EN.TryGetValue(cauTiengViet, out string en) && !string.IsNullOrEmpty(en)
            ? en
            : cauTiengViet;
    }

    /// <summary>Dịch rồi ghép tham số, ví dụ: `Loc.TF("Còn {0} phút", 5)`.</summary>
    public static string TF(string cauTiengViet, params object[] args)
    {
        string mau = T(cauTiengViet);
        try   { return string.Format(mau, args); }
        catch { return mau; }   // mẫu sai định dạng thì thà hiện thô còn hơn ném lỗi ra người chơi
    }

    /// <summary>
    /// Câu nào CHƯA có trong bảng dịch (dùng cho tool kiểm kê, không gọi lúc chơi).
    /// </summary>
    public static bool DaCoBanDich(string cauTiengViet)
    {
        return !string.IsNullOrEmpty(cauTiengViet) && LocStringTable.EN.ContainsKey(cauTiengViet);
    }

    public static int SoCauDaDich => LocStringTable.EN.Count;
}

/// <summary>Bí danh ngắn cho <see cref="LocalizationManager"/> — gõ `Loc.T("...")` cho gọn.</summary>
public static class Loc
{
    public static string T(string vi) => LocalizationManager.T(vi);
    public static string TF(string vi, params object[] a) => LocalizationManager.TF(vi, a);
    public static string Current => LocalizationManager.Current;
    public static bool DangTiengAnh => LocalizationManager.DangTiengAnh;

    public static event Action<string> OnChanged
    {
        add    { LocalizationManager.OnChanged += value; }
        remove { LocalizationManager.OnChanged -= value; }
    }
}
