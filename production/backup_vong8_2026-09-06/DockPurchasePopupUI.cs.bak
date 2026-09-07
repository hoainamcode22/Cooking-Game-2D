using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Popup mua slot bến tàu (BOAT-002 §3.6) — thay hành vi bấm-mua-trực-tiếp của
/// bảng khóa V1. TouristBoatUnlockFlow (rework V2) gọi <see cref="MoChoBen"/>
/// khi người chơi tap bảng khóa bến 2/3.
///
/// Hiển thị: dim đen 60% + card khung gỗ · tiêu đề "Mở bến số X" · dòng yêu cầu
/// level · hàng giá (icon vàng/gem + số giá TMP màu vàng #FFD34D) · nút MUA ·
/// nút X đóng. Nút MUA disable + dòng lý do ĐỎ khi thiếu level/tiền, cập nhật
/// LIVE qua FarmEconomyManager.OnCurrencyChanged + FarmLevelManager.OnLevelChanged.
///
/// Mua: BoatDockManager.TryUnlockDock (API V1 giữ nguyên — manager tự kiểm +
/// trừ tiền qua SpendGold/SpendGems, tự từ chối nếu thiếu). Thành công → đóng
/// popup; hiệu ứng mở slot (DockUnlockCelebrationFX) do TouristBoatUnlockFlow
/// bắn qua event OnDockUnlocked — MỘT đường FX duy nhất, không double.
///
/// Toàn bộ số (level/giá) đọc từ Config.GetDockRequirement — KHÔNG hardcode.
/// Input lock: FarmInputLock.RegisterPopupOpen/Close đúng pattern. Tween =
/// coroutine ease tự viết, unscaled time.
/// </summary>
[DisallowMultipleComponent]
public class DockPurchasePopupUI : MonoBehaviour
{
    /// <summary>Màu vàng của số giá — khớp tông vàng HUD (#FFD34D, chốt trong GDD §3.6).</summary>
    public static readonly Color MauVangGia = new Color(1f, 0.827f, 0.302f); // #FFD34D

    /// <summary>Màu đỏ của dòng lý do từ chối.</summary>
    private static readonly Color MauDoLyDo = new Color(0.898f, 0.290f, 0.290f); // #E54A4A

    // ─── Tham chiếu UI (tool wire — dựng tay thì kéo vào Inspector) ─────────

    [Header("Tham chiếu UI (tool tự wire)")]
    [Tooltip("Gốc visual của popup (dim + card) — mặc định INACTIVE.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("Dim đen full-screen (alpha 0.6) — chặn raycast xuống dưới.")]
    [SerializeField] private Image dimImage;

    [Tooltip("Card khung gỗ bo góc — scale-pop khi mở.")]
    [SerializeField] private RectTransform cardRect;

    [Tooltip("CanvasGroup bọc nội dung card — fade-in.")]
    [SerializeField] private CanvasGroup contentGroup;

    [Tooltip("Tiêu đề: 'Mở bến số X'.")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Dòng yêu cầu level: 'Yêu cầu: đạt Lv12 (bạn đang Lv10)'.")]
    [SerializeField] private TextMeshProUGUI levelReqText;

    [Tooltip("Icon tiền trên hàng giá — script tự swap sprite vàng/gem theo bến.")]
    [SerializeField] private Image costIcon;

    [Tooltip("Số giá — TMP màu vàng #FFD34D.")]
    [SerializeField] private TextMeshProUGUI costText;

    [Tooltip("Dòng lý do từ chối (đỏ) — chỉ hiện khi nút MUA bị disable.")]
    [SerializeField] private TextMeshProUGUI reasonText;

    [Tooltip("Nút MUA.")]
    [SerializeField] private Button buyButton;

    [Tooltip("Nhãn chữ trên nút MUA (để làm mờ khi disable).")]
    [SerializeField] private TextMeshProUGUI buyLabel;

    [Tooltip("Nút X đóng popup.")]
    [SerializeField] private Button closeButton;

    [Header("Icon tiền (tool wire từ sprite HUD có sẵn)")]
    [Tooltip("Sprite icon VÀNG — dùng cho bến trả vàng (bến 2).")]
    [SerializeField] private Sprite goldIconSprite;

    [Tooltip("Sprite icon GEM/Kim Cương — dùng cho bến trả gem (bến 3).")]
    [SerializeField] private Sprite gemIconSprite;

    [Header("Tuning")]
    [Tooltip("Thời gian scale-pop của card (giây).")]
    [SerializeField] private float popSeconds = 0.25f;

    [Tooltip("Alpha đích của dim nền.")]
    [SerializeField] private float dimAlpha = 0.6f;

    [Tooltip("Tên scene bếp (khớp FarmUIManager.cookingSceneName) — vào bếp lúc popup mở thì tự đóng.")]
    [SerializeField] private string cookingSceneName = "SampleScene";

    // ─── Runtime ────────────────────────────────────────────────────────────

    private int       _dockIndex = -1;
    private bool      _dangMo;
    private bool      _subscribed;
    private Coroutine _animRoutine;
    private float     _nhipKiemTraBep; // đếm ngược tới lần kiểm tra scene bếp kế

    /// <summary>Popup đang mở? (TouristBoatUnlockFlow đọc để chống mở chồng).</summary>
    public bool DangMo => _dangMo;

    // =========================================================================
    //  Vòng đời
    // =========================================================================

    private void Awake()
    {
        if (buyButton != null)   buyButton.onClick.AddListener(OnClickMua);
        if (closeButton != null) closeButton.onClick.AddListener(Dong);

        // Ép màu theo spec dù tool/Inspector chỉnh lệch — số giá luôn vàng, lý do luôn đỏ
        if (costText != null)   costText.color   = MauVangGia;
        if (reasonText != null) reasonText.color = MauDoLyDo;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    /// <summary>
    /// [QA M-5] Bị tắt giữa chừng (vào bếp, canvas SetActive(false), code khác tắt
    /// object): coroutine đóng đã bị Unity giết nên KHÔNG có ai trả input lock —
    /// phải tự trả ở đây, không được trông chờ FarmInputLock.ResetAll của sceneLoaded
    /// (đổi cách vào bếp là input khoá cứng ngay). Đồng thời trả visual về nguyên
    /// trạng vì tween bị cắt để lại card méo/mờ.
    /// </summary>
    private void OnDisable()
    {
        _animRoutine = null;
        HuyDangKyLive();
        TraTrangThaiVePhongThu();
    }

    private void OnDestroy()
    {
        if (buyButton != null)   buyButton.onClick.RemoveListener(OnClickMua);
        if (closeButton != null) closeButton.onClick.RemoveListener(Dong);

        HuyDangKyLive();
        TraTrangThaiVePhongThu();
    }

    /// <summary>
    /// Đóng NGAY (không anim): trả input lock, hạ cờ, ẩn popup, reset scale/alpha.
    /// An toàn gọi nhiều lần — cờ _dangMo tự chặn trả lock 2 lần.
    /// </summary>
    private void TraTrangThaiVePhongThu()
    {
        if (_dangMo)
        {
            FarmInputLock.RegisterPopupClose();
            _dangMo    = false;
            _dockIndex = -1;
        }

        if (popupRoot != null && popupRoot.activeSelf) popupRoot.SetActive(false);
        if (cardRect != null)     cardRect.localScale = Vector3.one;
        if (contentGroup != null) contentGroup.alpha  = 1f;
        if (dimImage != null)     SetAlpha(dimImage, dimAlpha);
    }

    /// <summary>
    /// Vào bếp lúc popup mua đang mở → tự đóng (canvas popup boat cố ý KHÔNG bị
    /// FarmUIManager tắt, xem HANDOFF §3). Poll thưa 0.5s cho rẻ.
    /// </summary>
    private void Update()
    {
        if (!_dangMo) return;

        _nhipKiemTraBep -= Time.unscaledDeltaTime;
        if (_nhipKiemTraBep > 0f) return;
        _nhipKiemTraBep = 0.5f;

        if (!string.IsNullOrEmpty(cookingSceneName) &&
            SceneManager.GetSceneByName(cookingSceneName).isLoaded)
            TraTrangThaiVePhongThu();
    }

    // =========================================================================
    //  API — TouristBoatUnlockFlow gọi khi tap bảng khóa
    // =========================================================================

    /// <summary>
    /// Mở popup mua cho bến dockIndex (0-2; thực tế chỉ 1/2 vì bến 0 mở free qua
    /// intro). Idempotent: đang mở thì chỉ refresh sang bến mới.
    /// </summary>
    public void MoChoBen(int dockIndex)
    {
        var mgr = BoatDockManager.Instance;
        if (mgr == null || mgr.Config == null)
        {
            Debug.LogWarning("[TouristBoat] DockPurchasePopupUI: BoatDockManager/Config chưa sẵn sàng — không mở popup mua.");
            return;
        }
        if (popupRoot == null || cardRect == null)
        {
            Debug.LogWarning("[TouristBoat] DockPurchasePopupUI: thiếu tham chiếu UI — chạy tool Setup Popups (UI) trước.");
            return;
        }
        if (mgr.IsDockUnlocked(dockIndex)) return;          // đã mở rồi — không có gì để bán
        if (BoatAnnouncePopupUI.TutorialDangChay()) return; // không đè tutorial

        _dockIndex = dockIndex;
        DienNoiDung(mgr);
        RefreshTrangThai();

        if (!_dangMo)
        {
            _dangMo = true;
            FarmInputLock.RegisterPopupOpen();
            FarmInputLock.SetPopupRaycastBlock(popupRoot, true);
            popupRoot.SetActive(true);

            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(MoAnimRoutine());
        }

        DangKyLive();
    }

    /// <summary>Đóng popup (nút X, hoặc sau khi mua thành công).</summary>
    public void Dong()
    {
        if (!_dangMo) return;

        HuyDangKyLive();

        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(DongAnimRoutine());
    }

    // =========================================================================
    //  Nội dung + trạng thái nút MUA
    // =========================================================================

    /// <summary>Điền tiêu đề/level/giá — toàn bộ số từ Config, không hardcode.</summary>
    private void DienNoiDung(BoatDockManager mgr)
    {
        DockUnlockRequirement req = mgr.Config.GetDockRequirement(_dockIndex);

        if (titleText != null)
            titleText.text = $"Mở bến số {_dockIndex + 1}";

        if (levelReqText != null)
        {
            int levelHienTai = FarmLevelManager.Instance != null
                ? FarmLevelManager.Instance.CurrentLevel : 0;
            levelReqText.text = $"Yêu cầu: đạt Lv{req.RequiredLevel} (bạn đang Lv{levelHienTai})";
        }

        // Hàng giá: bến trả vàng → icon vàng; bến trả gem → icon gem.
        // Config hiện tại không có bến tốn cả 2 loại (GetDockRequirement) — nếu
        // tương lai có, ưu tiên hiện vàng và ghi chú gem vào dòng level.
        if (req.GoldCost > 0)
        {
            if (costIcon != null) { costIcon.sprite = goldIconSprite; costIcon.enabled = goldIconSprite != null; }
            if (costText != null) costText.text = FormatVN(req.GoldCost);
        }
        else if (req.GemCost > 0)
        {
            if (costIcon != null) { costIcon.sprite = gemIconSprite; costIcon.enabled = gemIconSprite != null; }
            if (costText != null) costText.text = FormatVN(req.GemCost);
        }
        else
        {
            // Không tốn gì (bến 0 — lý thuyết không đi qua popup này, guard cho chắc)
            if (costIcon != null) costIcon.enabled = false;
            if (costText != null) costText.text = "Miễn phí";
        }
    }

    /// <summary>
    /// Bật/tắt nút MUA + dòng lý do đỏ theo CanUnlockDock (GDD §5 edge 5 —
    /// manager trả sẵn lý do tiếng Việt). Gọi lúc mở + mỗi lần tiền/level đổi.
    /// </summary>
    private void RefreshTrangThai()
    {
        var mgr = BoatDockManager.Instance;
        if (mgr == null || _dockIndex < 0) return;

        // Bến vừa được mở từ đường khác (hiếm) → popup hết việc, tự đóng
        if (mgr.IsDockUnlocked(_dockIndex))
        {
            if (_dangMo) Dong();
            return;
        }

        bool duDieuKien = mgr.CanUnlockDock(_dockIndex, out string lyDo);

        if (buyButton != null) buyButton.interactable = duDieuKien;
        if (buyLabel != null)
        {
            Color c = buyLabel.color;
            c.a = duDieuKien ? 1f : 0.55f;
            buyLabel.color = c;
        }
        if (reasonText != null)
        {
            reasonText.text = duDieuKien ? string.Empty : lyDo;
            reasonText.gameObject.SetActive(!duDieuKien);
        }

        // Dòng level cũng cập nhật live (lên level trong lúc popup mở)
        if (levelReqText != null && mgr.Config != null)
        {
            DockUnlockRequirement req = mgr.Config.GetDockRequirement(_dockIndex);
            int levelHienTai = FarmLevelManager.Instance != null
                ? FarmLevelManager.Instance.CurrentLevel : 0;
            levelReqText.text = $"Yêu cầu: đạt Lv{req.RequiredLevel} (bạn đang Lv{levelHienTai})";
        }
    }

    private void OnClickMua()
    {
        var mgr = BoatDockManager.Instance;
        if (mgr == null || _dockIndex < 0) return;

        // TryUnlockDock tự kiểm lần cuối + trừ tiền (SpendGold/SpendGems tự từ chối
        // nếu thiếu — API V1 của Dev A giữ nguyên). Thành công → manager bắn
        // OnDockUnlocked → TouristBoatUnlockFlow chạy DockUnlockCelebrationFX +
        // Dev A cho tàu xuất phát ngay. Popup chỉ việc đóng.
        if (mgr.TryUnlockDock(_dockIndex))
        {
            Dong();
        }
        else
        {
            // Từ chối phút chót (tiền vừa bị tiêu chỗ khác...) — cập nhật lý do ngay
            RefreshTrangThai();
        }
    }

    // ─── Live update: tiền + level đổi trong lúc popup mở ───────────────────

    private void DangKyLive()
    {
        if (_subscribed) return;
        if (FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        if (FarmLevelManager.Instance != null)
            FarmLevelManager.Instance.OnLevelChanged += HandleLevelChanged;
        _subscribed = true;
    }

    private void HuyDangKyLive()
    {
        if (!_subscribed) return;
        if (FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        if (FarmLevelManager.Instance != null)
            FarmLevelManager.Instance.OnLevelChanged -= HandleLevelChanged;
        _subscribed = false;
    }

    private void HandleCurrencyChanged(int gold, int gems) => RefreshTrangThai();
    private void HandleLevelChanged(int level)             => RefreshTrangThai();

    // =========================================================================
    //  Anim mở/đóng — cùng nhịp BoatAnnouncePopupUI
    // =========================================================================

    private IEnumerator MoAnimRoutine()
    {
        if (dimImage != null)     SetAlpha(dimImage, 0f);
        if (contentGroup != null) contentGroup.alpha = 0f;
        if (cardRect != null)     cardRect.localScale = Vector3.one * 0.9f;

        float t = 0f;
        float tong = popSeconds + 0.15f;
        while (t < tong)
        {
            t += Time.unscaledDeltaTime;

            if (cardRect != null)
            {
                float p = Mathf.Clamp01(t / Mathf.Max(0.01f, popSeconds));
                cardRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, EaseOutBack(p));
            }
            if (dimImage != null)
                SetAlpha(dimImage, dimAlpha * Mathf.Clamp01(t / Mathf.Max(0.01f, popSeconds * 0.8f)));
            if (contentGroup != null)
                contentGroup.alpha = Mathf.Clamp01((t - popSeconds * 0.3f) / 0.25f);
            yield return null;
        }

        if (cardRect != null)     cardRect.localScale = Vector3.one;
        if (dimImage != null)     SetAlpha(dimImage, dimAlpha);
        if (contentGroup != null) contentGroup.alpha = 1f;
        _animRoutine = null;
    }

    private IEnumerator DongAnimRoutine()
    {
        const float dur = 0.15f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);

            if (cardRect != null)     cardRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, p);
            if (dimImage != null)     SetAlpha(dimImage, dimAlpha * (1f - p));
            if (contentGroup != null) contentGroup.alpha = 1f - p;
            yield return null;
        }

        if (popupRoot != null) popupRoot.SetActive(false);
        if (cardRect != null)     cardRect.localScale = Vector3.one;
        if (contentGroup != null) contentGroup.alpha = 1f;

        FarmInputLock.RegisterPopupClose(); // tự chặn tap xuyên xuống world frame này
        _dangMo      = false;
        _dockIndex   = -1;
        _animRoutine = null;
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    // [QA m-6] KHÔNG dùng CultureInfo.GetCultureInfo("vi-VN"): Player Settings bật
    // Invariant Globalization (hay gặp khi build IL2CPP mobile cho nhẹ) sẽ ném
    // CultureNotFoundException. Tự dựng NumberFormatInfo — chạy mọi cấu hình build.
    private static readonly NumberFormatInfo DinhDangSoVN = new NumberFormatInfo
    {
        NumberGroupSeparator  = ".",
        NumberDecimalSeparator = ",",
        NumberGroupSizes      = new[] { 3 },
    };

    /// <summary>Định dạng số kiểu Việt Nam: 2000 → "2.000".</summary>
    private static string FormatVN(int amount)
        => amount.ToString("N0", DinhDangSoVN);

    private static float EaseOutBack(float p)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float t = p - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }
}
