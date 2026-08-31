using UnityEngine;

/// <summary>
/// Kéo RectTransform vào vùng an toàn của màn hình (<see cref="Screen.safeArea"/>) —
/// chống tai thỏ / lỗ camera / thanh gesture che HUD trên iPhone và Android khuyết.
///
/// Gắn vào RectTransform nào? — gắn vào GỐC của nhóm UI cần đẩy, không gắn vào từng
/// nút. HUD trên (avatar, thanh EXP, vàng, gem, nút cài đặt) là chỗ bắt buộc;
/// HUD dưới thường KHÔNG cần (thanh gesture của iOS chỉ chiếm ~20px và HUD dưới của
/// dự án đã cách đáy) — vì vậy có cờ chọn từng cạnh.
///
/// AN TOÀN VỚI LAYOUT ĐANG CÓ: khi safeArea trùng đúng toàn màn hình (đa số máy
/// Android tai thỏ ẩn / mọi máy PC + Editor), component KHÔNG sửa gì cả — anchor giữ
/// nguyên như Sếp đã canh. Chỉ khi hệ điều hành báo có vùng khuyết thì mới co lại.
///
/// Cách hoạt động: quy safeArea (pixel) → anchorMin/anchorMax (0-1) trên chính
/// RectTransform này, và ZERO hoá offset để anchor ăn thật. Vì thao tác qua anchor
/// nên mọi con bên trong vẫn giữ đúng tỉ lệ, không cần sửa gì thêm.
///
/// Chỉ tính lại khi CẦN: đổi safeArea / đổi kích thước màn hình / đổi orientation
/// (xoay máy). Không tính mỗi frame → không tốn CPU trong lúc chơi.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Áp vào cạnh nào")]
    [Tooltip("Đẩy xuống khỏi tai thỏ / lỗ camera. BẮT BUỘC cho HUD trên.")]
    [SerializeField] private bool apCanhTren = true;

    [Tooltip("Đẩy lên khỏi thanh gesture (iPhone không nút Home). HUD dưới của dự án thường đã cách đáy — mặc định TẮT để không phá layout.")]
    [SerializeField] private bool apCanhDuoi = false;

    [Tooltip("Đẩy khỏi khuyết bên trái (chỉ có tác dụng khi máy nằm ngang).")]
    [SerializeField] private bool apCanhTrai = true;

    [Tooltip("Đẩy khỏi khuyết bên phải (chỉ có tác dụng khi máy nằm ngang).")]
    [SerializeField] private bool apCanhPhai = true;

    [Header("Debug")]
    [Tooltip("In ra Console mỗi lần áp lại vùng an toàn (bật khi test trên máy thật).")]
    [SerializeField] private bool ghiLog = false;

    private RectTransform _rect;
    private Rect          _safeAreaCuoi;
    private Vector2Int    _manHinhCuoi;
    private ScreenOrientation _huongCuoi;

    // Anchor GỐC do Sếp canh trong Editor — mọi phép tính đều dựa trên đây, không
    // dựa trên anchor đã bị chính component này sửa (nếu không, chạy 2 lần là co dồn).
    private Vector2 _anchorMinGoc;
    private Vector2 _anchorMaxGoc;
    private bool    _daLuuAnchorGoc;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        LuuAnchorGoc();
    }

    private void OnEnable()
    {
        // Ép tính lại lần đầu (đặt mốc "không hợp lệ" để ApDung chắc chắn chạy)
        _manHinhCuoi = new Vector2Int(-1, -1);
        ApDung();
    }

    private void Update()
    {
        // Poll nhẹ: so 3 mốc, khác thì mới tính lại. Xoay máy / mở bàn phím ảo /
        // đổi kích thước cửa sổ Editor đều bị bắt ở đây.
        if (CungMotVung(Screen.safeArea, _safeAreaCuoi) &&
            Screen.width   == _manHinhCuoi.x &&
            Screen.height  == _manHinhCuoi.y &&
            Screen.orientation == _huongCuoi) return;

        ApDung();
    }

    /// <summary>So 2 vùng an toàn theo từng cạnh (1 pixel là ngưỡng đủ mịn).</summary>
    private static bool CungMotVung(Rect a, Rect b)
    {
        return Mathf.Abs(a.x - b.x) < 1f && Mathf.Abs(a.y - b.y) < 1f &&
               Mathf.Abs(a.width - b.width) < 1f && Mathf.Abs(a.height - b.height) < 1f;
    }

    private void LuuAnchorGoc()
    {
        if (_daLuuAnchorGoc || _rect == null) return;
        _anchorMinGoc  = _rect.anchorMin;
        _anchorMaxGoc  = _rect.anchorMax;
        _daLuuAnchorGoc = true;
    }

    /// <summary>Tính lại anchor theo Screen.safeArea hiện tại.</summary>
    public void ApDung()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        if (_rect == null) return;
        LuuAnchorGoc();

        Rect safe = Screen.safeArea;
        int w = Screen.width;
        int h = Screen.height;

        _safeAreaCuoi = safe;
        _manHinhCuoi  = new Vector2Int(w, h);
        _huongCuoi    = Screen.orientation;

        if (w <= 0 || h <= 0) return; // frame đầu trên vài thiết bị

        // Vùng an toàn = TOÀN màn hình → không có khuyết nào → GIỮ NGUYÊN layout gốc.
        // Đây là nhánh chạy trên Editor/PC và phần lớn Android, nên bản build hiện tại
        // của Sếp không đổi một pixel nào.
        bool khongKhuyet = Mathf.Approximately(safe.x, 0f) &&
                           Mathf.Approximately(safe.y, 0f) &&
                           Mathf.Approximately(safe.width,  w) &&
                           Mathf.Approximately(safe.height, h);
        if (khongKhuyet)
        {
            _rect.anchorMin = _anchorMinGoc;
            _rect.anchorMax = _anchorMaxGoc;
            ZeroOffset();
            if (ghiLog) Debug.Log("[SafeArea] Máy không có vùng khuyết — giữ nguyên layout gốc.");
            return;
        }

        // safeArea (pixel) → tỉ lệ 0-1
        Vector2 min = new Vector2(safe.x / w, safe.y / h);
        Vector2 max = new Vector2((safe.x + safe.width) / w, (safe.y + safe.height) / h);

        // Cạnh nào KHÔNG áp thì trả về anchor gốc của cạnh đó
        if (!apCanhTrai)  min.x = _anchorMinGoc.x;
        if (!apCanhDuoi)  min.y = _anchorMinGoc.y;
        if (!apCanhPhai)  max.x = _anchorMaxGoc.x;
        if (!apCanhTren)  max.y = _anchorMaxGoc.y;

        // Kẹp 0-1 và giữ min < max (thiết bị báo số lạ cũng không làm rect âm)
        min.x = Mathf.Clamp01(min.x); min.y = Mathf.Clamp01(min.y);
        max.x = Mathf.Clamp01(max.x); max.y = Mathf.Clamp01(max.y);
        if (max.x <= min.x) { min.x = _anchorMinGoc.x; max.x = _anchorMaxGoc.x; }
        if (max.y <= min.y) { min.y = _anchorMinGoc.y; max.y = _anchorMaxGoc.y; }

        _rect.anchorMin = min;
        _rect.anchorMax = max;
        ZeroOffset();

        if (ghiLog)
            Debug.Log($"[SafeArea] {name}: safeArea={safe} màn hình={w}x{h} → " +
                      $"anchorMin={min} anchorMax={max}");
    }

    /// <summary>
    /// Offset phải về 0 để anchor có hiệu lực thật — còn offset cũ thì rect vẫn bị
    /// kéo ra ngoài vùng an toàn đúng bằng số offset đó.
    /// </summary>
    private void ZeroOffset()
    {
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

#if UNITY_EDITOR
    /// <summary>Chỉnh cờ trong Inspector lúc đang Play → thấy ngay kết quả.</summary>
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        _manHinhCuoi = new Vector2Int(-1, -1);
    }
#endif
}
