using TMPro;
using UnityEngine;

/// <summary>
/// Gắn lên một TMP_Text để nó TỰ ĐỔI NGÔN NGỮ khi người chơi bấm lá cờ.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// Dùng cho các text ĐẶT SẴN TRONG SCENE / PREFAB (111 text tiếng Việt trong SCN_Farm,
/// 197 trong prefab). Text dựng bằng code thì gọi thẳng `Loc.T(...)` thay vì dùng component này.
///
/// KHOÁ TRA = CHÍNH CHỮ TIẾNG VIỆT ĐANG CÓ. Awake tự chụp lại `text` lúc đó làm khoá gốc,
/// nên gắn component vào là chạy — KHÔNG phải điền key tay cho từng cái.
/// (Muốn ép khoá khác thì điền `khoaGhiDe`.)
///
/// AN TOÀN: câu chưa có bản dịch ⇒ giữ nguyên tiếng Việt, không bao giờ hiện chuỗi rỗng
/// hay "MISSING" trước mặt người chơi.
///
/// [Localization]
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Để TRỐNG (khuyến nghị) ⇒ tự lấy chữ tiếng Việt đang có trong TMP làm khoá tra. " +
             "Chỉ điền khi muốn ép tra theo một câu khác.")]
    [SerializeField] private string khoaGhiDe;

    [Tooltip("TRUE: chữ có tham số động (VD \"Còn {0} phút\") — component sẽ KHÔNG tự ghi đè, " +
             "code phải tự gọi Loc.TF(...). Bật khi thấy chữ bị mất số sau khi đổi ngôn ngữ.")]
    [SerializeField] private bool boQuaVìCoThamSo;

    private TMP_Text _txt;
    private string   _khoaGoc;
    private bool     _daChup;

    private void Awake()
    {
        _txt = GetComponent<TMP_Text>();
        ChupKhoaGoc();
    }

    private void OnEnable()
    {
        ChupKhoaGoc();          // phòng khi object được bật lần đầu sau khi text bị code đổi
        LocalizationManager.OnChanged += ApDung;
        ApDung(LocalizationManager.Current);
    }

    private void OnDisable()
    {
        LocalizationManager.OnChanged -= ApDung;
    }

    /// <summary>Chụp chữ gốc ĐÚNG MỘT LẦN — chụp lại sau khi đã dịch sẽ biến bản dịch thành khoá.</summary>
    private void ChupKhoaGoc()
    {
        if (_daChup) return;
        if (_txt == null) _txt = GetComponent<TMP_Text>();
        if (_txt == null) return;

        _khoaGoc = !string.IsNullOrEmpty(khoaGhiDe) ? khoaGhiDe : _txt.text;
        _daChup  = true;
    }

    private void ApDung(string lang)
    {
        if (boQuaVìCoThamSo) return;
        if (_txt == null || string.IsNullOrEmpty(_khoaGoc)) return;
        _txt.text = LocalizationManager.T(_khoaGoc);
    }

    /// <summary>
    /// Code đổi nội dung động thì gọi hàm này thay vì gán thẳng `.text`,
    /// để lần đổi ngôn ngữ sau vẫn dịch đúng câu mới.
    /// </summary>
    public void DatChu(string cauTiengVietMoi)
    {
        _khoaGoc = cauTiengVietMoi;
        _daChup  = true;
        if (_txt == null) _txt = GetComponent<TMP_Text>();
        if (_txt != null) _txt.text = LocalizationManager.T(_khoaGoc);
    }
}
