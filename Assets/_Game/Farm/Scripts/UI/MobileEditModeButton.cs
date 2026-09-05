using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nút HUD bật/tắt CHẾ ĐỘ SỬA (Edit Mode) — vá lỗ CHẶN GAMEPLAY trên mobile.
///
/// ── LÝ DO TỒN TẠI (số liệu đo trên project thật) ────────────────────────────
/// `EditModeManager.Update()` chỉ có MỘT đường vào Edit Mode:
///     if (Keyboard.current.eKey.wasPressedThisFrame) ToggleEditMode();
/// và trong `SCN_Farm.unity`: `grep Btn_EditMode` = 0, `grep ToggleEditMode` = 0
/// ⇒ KHÔNG có nút nào trong scene gọi hàm đó. Trên điện thoại không có bàn phím
/// ⇒ người chơi <b>không bao giờ vào được Edit Mode</b> ⇒ không di chuyển / xoay /
/// xoá được công trình đã đặt.
///
/// (Ghi chú quan trọng: hàng nút ✓ ✕ ↻ 🗑 của Ghost thì ĐÃ CÓ và ĐÃ chạy bằng ngón
/// tay — `PlacementManager.BindGhostButtons` + nhánh `IsMouseOverRect` trong Update.
/// Chỗ thiếu chỉ là cái CỬA vào Edit Mode, nên file này không thêm nút xoay/xoá nào
/// nữa: thêm nút UI mới mà không khai báo rect của nó trong danh sách ở
/// PlacementManager.Update sẽ làm Ghost nhảy tới ngón tay và mang nút chạy theo —
/// đúng cái lỗi mà comment trong file đó cảnh báo.)
///
/// ── CÁCH DÙNG ───────────────────────────────────────────────────────────────
/// Gắn component này vào bất kỳ object nào trong scene farm (ví dụ ngay trên HUD).
///  • Có `nutCoSan` → dùng đúng nút đó, chỉ nối onClick + đổi nhãn.
///  • Để trống  → tự dựng một nút ở góc dưới-phải canvas HUD lúc chạy.
/// Nhãn tự đổi theo trạng thái: "Sửa" ⇄ "Xong". Không đụng logic Edit Mode, chỉ gọi
/// đúng API public đang có (`ToggleEditMode`).
/// </summary>
[DisallowMultipleComponent]
public class MobileEditModeButton : MonoBehaviour
{
    [Header("Nút")]
    [Tooltip("Nút có sẵn trong scene (tuỳ chọn). Để TRỐNG thì script tự dựng nút lúc chạy.")]
    [SerializeField] private Button nutCoSan;

    [Tooltip("Chỉ hiện nút khi máy có màn hình cảm ứng. TẮT (mặc định) = luôn hiện, để Sếp test bằng chuột.")]
    [SerializeField] private bool chiHienTrenMobile = false;

    [Header("Nút tự dựng — vị trí & cỡ")]
    [Tooltip("Cỡ nút theo pixel THẬT của máy (90 = mức tối thiểu Apple/Google cho vùng chạm).")]
    [SerializeField] private float coNutPixel = 110f;

    [Tooltip("Lề so với góc dưới-phải (canvas unit).")]
    [SerializeField] private Vector2 leGocDuoiPhai = new Vector2(40f, 220f);

    [Header("Nhãn")]
    [SerializeField] private string nhanKhiTat = "";
    [SerializeField] private string nhanKhiBat = "";

    private Button    _nut;
    private TMP_Text  _nhan;
    private bool      _daDung;

    private void Start()
    {
        if (chiHienTrenMobile && !TouchInput.HasTouchscreen)
        {
            // PC/Editor mà Sếp chọn "chỉ mobile" → không dựng gì cả
            if (nutCoSan != null) nutCoSan.gameObject.SetActive(false);
            return;
        }

        DungNut();
        CapNhatNhan(EditModeManager.IsEditMode);
    }

    private void OnEnable()
    {
        EditModeManager.OnEditModeChanged -= CapNhatNhan;
        EditModeManager.OnEditModeChanged += CapNhatNhan;
    }

    private void OnDisable()
    {
        EditModeManager.OnEditModeChanged -= CapNhatNhan;
    }

    private void OnDestroy()
    {
        if (_nut != null) _nut.onClick.RemoveListener(OnBamNut);
    }

    /// <summary>Bấm nút = gọi đúng API public đang có của EditModeManager.</summary>
    public void OnBamNut()
    {
        var mgr = EditModeManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[MobileEditMode] Không thấy EditModeManager trong scene — nút Sửa không có tác dụng.");
            return;
        }

        mgr.ToggleEditMode();
        CapNhatNhan(EditModeManager.IsEditMode); // đổi nhãn ngay, không đợi event
    }

    private void CapNhatNhan(bool dangSua)
    {
        if (_nhan == null) return;
        _nhan.text = dangSua ? nhanKhiBat : nhanKhiTat;
    }

    private void DungNut()
    {
        if (_daDung) return;
        _daDung = true;

        if (nutCoSan != null)
        {
            _nut  = nutCoSan;
            _nhan = _nut.GetComponentInChildren<TMP_Text>();
            _nut.onClick.AddListener(OnBamNut);
            _nut.gameObject.SetActive(true);
            return;
        }

        Canvas canvas = LayCanvasHUD();
        if (canvas == null)
        {
            Debug.LogWarning("[MobileEditMode] Không tìm được Canvas nào trong scene — " +
                             "không dựng được nút Sửa. Kéo một nút có sẵn vào field 'Nut Co San'.");
            return;
        }

        float scale = canvas.scaleFactor > 0.0001f ? canvas.scaleFactor : 1f;
        float co    = Mathf.Max(1f, coNutPixel) / scale; // pixel thật → canvas unit

        var go = new GameObject("Btn_EditMode_Mobile", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.sizeDelta        = new Vector2(co, co);
        rt.anchoredPosition = new Vector2(-leGocDuoiPhai.x, leGocDuoiPhai.y);

        var img = go.AddComponent<Image>();
        img.color         = new Color(0.42f, 0.72f, 0.30f, 0.95f); // xanh lá HUD
        img.raycastTarget = true;

        _nut = go.AddComponent<Button>();
        _nut.targetGraphic = img;
        _nut.onClick.AddListener(OnBamNut);

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontSize      = co * 0.32f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        _nhan = tmp;

        Debug.Log("[MobileEditMode] Đã dựng nút Sửa (Edit Mode) ở góc dưới-phải — " +
                  "scene chưa có nút nào gọi EditModeManager.ToggleEditMode().");
    }

    /// <summary>Canvas HUD: ưu tiên canvas của chính object này, fallback canvas sortingOrder cao nhất.</summary>
    private Canvas LayCanvasHUD()
    {
        var mine = GetComponentInParent<Canvas>();
        if (mine != null) return mine.rootCanvas != null ? mine.rootCanvas : mine;

        Canvas best = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c == null || !c.isRootCanvas) continue;
            if (best == null || c.sortingOrder > best.sortingOrder) best = c;
        }
        return best;
    }
}
