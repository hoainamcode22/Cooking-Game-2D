// ═══════════════════════════════════════════════════════════════════════════
//  TUA NHANH TUTORIAL — CHỈ DÀNH CHO DEV
// ═══════════════════════════════════════════════════════════════════════════
//
//  Toàn bộ file nằm trong #if UNITY_EDITOR || DEVELOPMENT_BUILD nên KHÔNG có
//  một dòng nào lọt vào bản release: script biến mất hoàn toàn khỏi bản build
//  chính thức, kể cả component đã gắn sẵn trong scene.
//
//  Vì sao cần: muốn thử bước 27 thì phải chơi lại 26 bước trước đó, mất ~10 phút
//  mỗi lần sửa một dòng text. Bảng này nhảy thẳng tới bước cần thử trong 10 giây.
//
#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Bảng tua nhanh tutorial. Bấm F8 để mở/đóng. Khi chưa bấm F8 thì component này
/// KHÔNG làm gì cả — Update() chỉ đọc đúng một phím, không tìm object, không cấp phát.
/// WP-A2 (2026-09-05): đổi F9 → F8 vì PopupGateDebugF9 đã chiếm F9; tự sinh khi vào Play
/// (không cần gắn tay vào scene) — cùng kiểu với PopupCaptureReporter.AutoSpawn.
/// </summary>
public class TutorialDebugJump : MonoBehaviour
{
    /// <summary>Tên phím tắt hiển thị trong bảng / log. Phím thật xem <see cref="DaBamPhimBat"/>.</summary>
    private const string TEN_PHIM_TAT = "F8";

    [Header("Phím tắt")]
    [Tooltip("Tắt đi nếu F8 đụng với debug khác. (Đã đổi từ F9 sang F8 vì PopupGateDebugF9 đang nghe F9.)")]
    [SerializeField] private bool _batPhimTat = true;

    // Trạng thái bảng. Mặc định ĐÓNG — không tự chạy gì khi chưa bấm F8.
    private bool _hienBang;

    // Instance tự sinh — chỉ để không sinh trùng khi scene đã có sẵn component này.
    private static TutorialDebugJump _instance;

    /// <summary>
    /// Tự tạo bảng debug khi vào Play — không cần gắn tay vào scene.
    /// Cả file đã nằm trong #if UNITY_EDITOR || DEVELOPMENT_BUILD; guard lặp lại bên trong
    /// để ai đọc riêng hàm này vẫn thấy rõ nó không bao giờ lọt vào bản release.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TuSinh()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_instance != null) return;
        // Scene có thể đã gắn sẵn component (kể cả đang tắt) — tôn trọng, không sinh thêm.
        _instance = FindAnyObjectByType<TutorialDebugJump>(FindObjectsInactive.Include);
        if (_instance != null) return;

        var go = new GameObject("~TutorialDebugJump");
        go.hideFlags = HideFlags.HideInHierarchy; // ẩn cho khỏi rối Hierarchy khi debug scene
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<TutorialDebugJump>();
#endif
    }

    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // Cache TutorialManager MỘT LẦN. Tuyệt đối không FindObjectsByType mỗi frame.
    private TutorialManager _quanLy;
    private bool _daTimQuanLy;

    // Ô nhập số bước muốn nhảy tới.
    private string _oNhapChiSo = "0";

    // Vùng vẽ ở góc trên bên trái màn hình. BeginArea CẮT theo đúng rect này,
    // nên chiều cao phải đủ chứa 5 hàng — để 0 là bảng trắng trơn.
    private static readonly Rect KHUNG_BANG = new Rect(12f, 12f, 320f, 165f);

    // GUIStyle phải cache: OnGUI chạy nhiều lần mỗi frame, tạo mới mỗi lần là rác.
    private static GUIStyle _kieuNhan;

    // ═══════════════════════ VÒNG ĐỜI ═══════════════════════

    private void Update()
    {
        // CHỈ đọc phím — không làm gì khác ở đây.
        if (!_batPhimTat) return;
        if (DaBamPhimBat()) _hienBang = !_hienBang;
    }

    private static bool DaBamPhimBat()
    {
#if ENABLE_INPUT_SYSTEM
        var banPhim = Keyboard.current;
        return banPhim != null && banPhim.f8Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F8);
#endif
    }

    // ═══════════════════════ BẢNG ĐIỀU KHIỂN ═══════════════════════

    private void OnGUI()
    {
        if (!_hienBang) return;

        // Tìm TutorialManager lần đầu tiên bảng được mở, rồi giữ lại mãi.
        if (!_daTimQuanLy) TimQuanLy();

        GUILayout.BeginArea(KHUNG_BANG, GUI.skin.box);

        GUILayout.Label("<b>TUA NHANH TUTORIAL</b> (" + TEN_PHIM_TAT + " để đóng)", KieuNhan());

        if (_quanLy == null)
        {
            GUILayout.Label("Không tìm thấy TutorialManager trong scene.");
            if (GUILayout.Button("Tìm lại")) TimQuanLy();
            GUILayout.EndArea();
            return;
        }

        int tongSo   = _quanLy.TongSoBuoc;
        int chiSoNay = _quanLy.ChiSoBuocHienTai;
        string tenNay = _quanLy.TenBuocHienTai;

        GUILayout.Label($"Bước hiện tại: [{chiSoNay}/{tongSo}] {tenNay}");

        // ── Hàng nhập số + nút Nhảy tới ──────────────────────────────────
        GUILayout.BeginHorizontal();
        GUILayout.Label("Nhảy tới bước:", GUILayout.Width(95f));
        _oNhapChiSo = GUILayout.TextField(_oNhapChiSo, 4, GUILayout.Width(50f));
        if (GUILayout.Button("Nhảy tới"))
        {
            if (int.TryParse(_oNhapChiSo, out int chiSoMuon)) NhayToi(chiSoMuon, tongSo);
            else Debug.LogWarning($"[TutorialDebugJump] '{_oNhapChiSo}' không phải số bước hợp lệ.");
        }
        GUILayout.EndHorizontal();

        // ── Hàng lùi / tiến một bước ─────────────────────────────────────
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<< Bước trước")) NhayToi(chiSoNay - 1, tongSo);
        if (GUILayout.Button("Bước sau >>"))   NhayToi(chiSoNay + 1, tongSo);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("In danh sách bước ra Console")) InDanhSachBuoc();

        GUILayout.EndArea();
    }

    private static GUIStyle KieuNhan()
    {
        if (_kieuNhan == null) _kieuNhan = new GUIStyle(GUI.skin.label) { richText = true };
        return _kieuNhan;
    }

    // ═══════════════════════ HÀNH ĐỘNG ═══════════════════════

    private void TimQuanLy()
    {
        // Gọi ĐÚNG MỘT LẦN (hoặc khi dev bấm "Tìm lại"), không gọi trong Update.
        _quanLy = TutorialManager.Instance != null
            ? TutorialManager.Instance
            : FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
        _daTimQuanLy = true;
    }

    private void NhayToi(int chiSo, int tongSo)
    {
        if (_quanLy == null) return;

        if (tongSo <= 0)
        {
            Debug.LogWarning("[TutorialDebugJump] Tutorial chưa có bước nào để nhảy tới.");
            return;
        }

        if (chiSo < 0 || chiSo >= tongSo)
        {
            Debug.LogWarning($"[TutorialDebugJump] Bước {chiSo} nằm ngoài khoảng hợp lệ 0..{tongSo - 1}.");
            return;
        }

        _oNhapChiSo = chiSo.ToString();
        _quanLy.DebugNhayToiBuoc(chiSo);
    }

    /// <summary>
    /// In toàn bộ tên bước kèm chỉ số ra Console để biết cần nhảy tới số mấy.
    /// [VÒNG 17] Dùng API công khai LayTenBuoc(i) mà Lead đã thêm vào TutorialManager —
    /// bỏ hẳn reflection, đổi tên field private không còn làm gãy công cụ này nữa.
    /// </summary>
    private void InDanhSachBuoc()
    {
        if (_quanLy == null) return;

        int tongSo   = _quanLy.TongSoBuoc;
        int chiSoNay = _quanLy.ChiSoBuocHienTai;

        var sb = new StringBuilder();
        sb.AppendLine($"═══ DANH SÁCH BƯỚC TUTORIAL ({tongSo} bước) ═══");
        for (int i = 0; i < tongSo; i++)
        {
            string ten = _quanLy.LayTenBuoc(i);
            sb.AppendLine(i == chiSoNay ? $">> [{i:00}] {ten}" : $"   [{i:00}] {ten}");
        }
        Debug.Log(sb.ToString());
    }
}

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
