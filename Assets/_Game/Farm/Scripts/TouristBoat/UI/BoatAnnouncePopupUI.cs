using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Popup screen-space "Tàu số 0X sắp cập bến!" (BOAT-002 §3.5) — gắn trên gốc
/// TouristBoatPopups (object ACTIVE dưới canvas popup; phần visual popupRoot
/// mặc định inactive, tool TouristBoatUIPopupSetupTool dựng + wire).
///
/// Luồng:
///   • Nghe BoatDockManager.OnNextTripScheduled(dockIndex, arrivalUtc, phút) —
///     API contract V2 của Dev A (bắn khi chuyến kế được lên lịch: tàu trước rời
///     bến / vừa mở bến / vào game thấy chuyến kế trong tương lai).
///   • Mỗi arrivalUtc chỉ báo ĐÚNG 1 LẦN: persist PlayerPrefs theo dock
///     (key = TouristBoat_DaBaoChuyen_{dock}, value = arrival ticks — 1 key/bến,
///     không phình prefs theo số chuyến).
///   • Nhiều event dồn lúc mở 3 bến → vào hàng đợi, hiện LẦN LƯỢT, không chồng.
///   • KHÔNG hiện khi: tutorial đang chạy · popup khác đang mở (FarmInputLock /
///     PopupManager) · đang ở scene bếp (GDD §5 edge 6 — hoãn tới khi về farm).
///   • Hiệu ứng: dim đen 60% fade-in + card scale-pop 0.9→1 (ease-out-back
///     ~0.25s) + text fade-in; nút "Đã rõ" đóng.
///
/// Input lock: FarmInputLock.RegisterPopupOpen/RegisterPopupClose (đúng pattern —
/// RegisterPopupClose tự SuppressWorldClickForCurrentFrame chống tap xuyên).
/// Mọi tween là coroutine ease tự viết (dự án không dùng tween library ngoài),
/// chạy theo unscaled time để không phụ thuộc Time.timeScale.
/// </summary>
[DisallowMultipleComponent]
public class BoatAnnouncePopupUI : MonoBehaviour
{
    // ─── Tham chiếu UI (tool wire — dựng tay thì kéo vào Inspector) ─────────

    [Header("Tham chiếu UI (tool tự wire)")]
    [Tooltip("Gốc visual của popup (dim + card) — mặc định INACTIVE, script bật khi cần.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("Dim đen full-screen (alpha đích 0.6) — raycastTarget bật để chặn UI dưới.")]
    [SerializeField] private Image dimImage;

    [Tooltip("Card khung gỗ bo góc — scale-pop khi mở. Sprite do tool wire từ asset khung có sẵn.")]
    [SerializeField] private RectTransform cardRect;

    [Tooltip("CanvasGroup bọc phần chữ + nút (con 'Content' của card) — fade-in.")]
    [SerializeField] private CanvasGroup contentGroup;

    [Tooltip("Tiêu đề: 'Tàu số 0X sắp cập bến!'")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Nội dung: 'Tàu số 0X sẽ cập bến sau X phút! ...'")]
    [SerializeField] private TextMeshProUGUI bodyText;

    [Tooltip("Nút 'Đã rõ' — đóng popup.")]
    [SerializeField] private Button confirmButton;

    // ─── Tuning ─────────────────────────────────────────────────────────────

    [Header("Tuning")]
    [Tooltip("Thời gian scale-pop của card (giây).")]
    [SerializeField] private float popSeconds = 0.25f;

    [Tooltip("Thời gian fade chữ (giây) — chạy sau khi card pop xong một nửa.")]
    [SerializeField] private float textFadeSeconds = 0.30f;

    [Tooltip("Alpha đích của dim nền (0.6 = đen 60% theo GDD §3.5).")]
    [SerializeField] private float dimAlpha = 0.6f;

    [Tooltip("Tên scene bếp (cùng default với FarmUIManager.cookingSceneName) — đang ở bếp thì hoãn popup tới khi về farm (GDD §5 edge 6).")]
    [SerializeField] private string cookingSceneName = "SampleScene";

    // ─── PlayerPrefs (mỗi arrivalUtc chỉ báo 1 lần) ─────────────────────────

    // 1 key/bến, value = ticks của arrival ĐÃ báo gần nhất. Lịch tàu per-dock
    // đơn điệu tăng nên "ticks đã báo == ticks chuyến này" ⇔ chuyến này đã báo.
    private const string KeyDaBaoFormat = "TouristBoat_DaBaoChuyen_{0}";

    // ─── Runtime ────────────────────────────────────────────────────────────

    private struct ChuyenChoBao
    {
        public int      DockIndex;
        public DateTime ArrivalUtc;
        public int      PhutCho; // từ event — chỉ dùng làm fallback, lúc hiện tính lại từ ArrivalUtc
    }

    private readonly List<ChuyenChoBao> _hangDoi = new List<ChuyenChoBao>();

    private BoatDockManager _manager;    // giữ ref để unsubscribe an toàn lúc teardown
    private bool            _subscribed;
    private bool            _dangHien;   // 1 popup tại 1 thời điểm — hàng đợi lo phần còn lại
    private Coroutine       _drainRoutine;
    private Coroutine       _animRoutine;
    private float           _nhipKiemTraBep; // đếm ngược tới lần kiểm tra scene bếp kế

    // =========================================================================
    //  Vòng đời
    // =========================================================================

    private void Awake()
    {
        // Wire nút runtime (không dựa persistent listener trong scene — tool chỉ tạo object)
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickDaRo);

        if (popupRoot != null)
            popupRoot.SetActive(false); // đảm bảo trạng thái mặc định dù scene lưu sai
    }

    private void Start()
    {
        StartCoroutine(BootRoutine());
    }

    /// <summary>
    /// Subscribe SỚM NHẤT có thể (ngay khi Instance xuất hiện, không đợi IsReady)
    /// để không lỡ event Dev A bắn trong Start của manager; hàng đợi + persist
    /// tự lo chuyện hiện đúng lúc. Cùng pattern chờ 8s của BoatDockSlot/UnlockFlow.
    /// </summary>
    private IEnumerator BootRoutine()
    {
        float waited = 0f;
        while (BoatDockManager.Instance == null && waited < 8f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        _manager = BoatDockManager.Instance;
        if (_manager == null)
        {
            Debug.LogWarning("[TouristBoat] BoatAnnouncePopupUI: không thấy BoatDockManager — popup báo tàu tắt.");
            yield break;
        }

        _manager.OnNextTripScheduled += HandleNextTripScheduled;
        _subscribed = true;

        // [QA m-3] Không phụ thuộc vào việc event có bắn kịp hay không: Dev A có thể
        // đã announce ngay trong Start của họ (migrate V1→V2 / đồng hồ lùi) TRƯỚC khi
        // mình kịp subscribe, và họ không bắn lại chuyến đã đánh dấu. Quét thẳng lịch
        // qua API đọc-chỉ để tự dựng lại hàng đợi — đây cũng chính là luật §3.5
        // "vào game thấy chuyến kế còn ≥1 phút thì báo 1 lần nếu chưa từng báo".
        QuetChuyenChuaBao();
    }

    /// <summary>
    /// Quét cả 3 bến: bến nào có chuyến kế trong tương lai mà PlayerPrefs chưa ghi
    /// "đã báo" thì đưa vào hàng đợi. Idempotent — chuyến đã báo bị lọc ở
    /// HandleNextTripScheduled, chuyến &lt;1 phút bị lọc ở DrainRoutine.
    /// </summary>
    private void QuetChuyenChuaBao()
    {
        if (_manager == null) return;

        for (int dock = 0; dock < BoatDockManager.DockCount; dock++)
        {
            DateTime arrival;
            if (!_manager.TryGetNextArrivalUtc(dock, out arrival)) continue;

            int phut = _manager.GetMinutesToNextArrival(dock);
            HandleNextTripScheduled(dock, arrival, phut > 0 ? phut : 1);
        }
    }

    /// <summary>
    /// [QA B-4] Unity GIẾT mọi coroutine khi GameObject bị SetActive(false) và KHÔNG
    /// chạy lại khi bật lên — nên không được coi _drainRoutine là còn sống qua một
    /// lần tắt/bật. Bật lại: hàng đợi còn hàng thì khởi động lại vòng rút ngay.
    /// </summary>
    private void OnEnable()
    {
        if (_drainRoutine == null && _hangDoi.Count > 0)
            _drainRoutine = StartCoroutine(DrainRoutine());
    }

    /// <summary>
    /// [QA B-4 + M-5] Bị tắt (vào bếp, canvas bị SetActive(false), ai đó tắt object):
    /// coroutine đã chết → xoá tay mọi dấu vết để OnEnable khởi động lại sạch sẽ,
    /// TRẢ input lock (không được trông chờ FarmInputLock.ResetAll của sceneLoaded),
    /// và trả visual về nguyên trạng (tween bị cắt giữa chừng làm card méo/mờ).
    /// Hàng đợi GIỮ NGUYÊN — GDD §5 edge 6: thông báo phải HOÃN rồi hiện lại,
    /// không phải mất luôn.
    /// </summary>
    private void OnDisable()
    {
        _drainRoutine = null;
        _animRoutine  = null;
        TraTrangThaiVePhongThu();
    }

    /// <summary>
    /// Đóng popup NGAY (không anim) + trả input lock + reset visual. Dùng cho
    /// OnDisable/OnDestroy và khi người chơi vào bếp lúc popup đang mở.
    /// An toàn gọi nhiều lần (cờ _dangHien tự chặn trả lock 2 lần).
    /// </summary>
    private void TraTrangThaiVePhongThu()
    {
        if (_dangHien)
        {
            FarmInputLock.RegisterPopupClose();
            _dangHien = false;
        }

        if (popupRoot != null && popupRoot.activeSelf) popupRoot.SetActive(false);
        if (cardRect != null)     cardRect.localScale = Vector3.one;
        if (contentGroup != null) contentGroup.alpha  = 1f;
        if (dimImage != null)     SetAlpha(dimImage, dimAlpha);
    }

    /// <summary>
    /// Vào bếp lúc popup ĐANG mở: canvas riêng của popup boat không bị
    /// FarmUIManager tắt (xem HANDOFF §3 — cố ý, để component sống mà nghe event),
    /// nên tự đóng ở đây, nếu không popup sẽ nằm đè lên scene bếp.
    /// Chuyến đã được đánh dấu "đã báo" nên không hiện lại — người chơi đã đọc rồi.
    /// Poll thưa 0.5s: tra scene theo tên mỗi frame là phí.
    /// </summary>
    private void Update()
    {
        if (!_dangHien) return;

        _nhipKiemTraBep -= Time.unscaledDeltaTime;
        if (_nhipKiemTraBep > 0f) return;
        _nhipKiemTraBep = 0.5f;

        if (DangTrongSceneBep())
            TraTrangThaiVePhongThu();
    }

    private void OnDestroy()
    {
        if (_subscribed && _manager != null)
            _manager.OnNextTripScheduled -= HandleNextTripScheduled;

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnClickDaRo);

        // Destroy giữa lúc popup đang mở (đổi scene...) — trả input lock, không để
        // popupLockCount lệch (FarmInputLock tự reset khi load scene, nhưng destroy
        // đơn lẻ trong cùng scene thì không).
        TraTrangThaiVePhongThu();
    }

    // =========================================================================
    //  Nhận event + hàng đợi
    // =========================================================================

    /// <summary>
    /// API contract Dev A: (dockIndex, arrivalUtc, phút chờ làm tròn).
    /// Chỉ enqueue — mọi điều kiện hiện (tutorial, popup khác, scene bếp)
    /// kiểm ở DrainRoutine ngay trước khi hiện.
    /// </summary>
    private void HandleNextTripScheduled(int dockIndex, DateTime arrivalUtc, int phutCho)
    {
        if (DaBaoChuyenNay(dockIndex, arrivalUtc)) return; // đã báo đúng arrival này rồi

        // Chống duplicate trong hàng đợi (manager có thể re-fire cùng chuyến)
        for (int i = 0; i < _hangDoi.Count; i++)
        {
            if (_hangDoi[i].DockIndex == dockIndex && _hangDoi[i].ArrivalUtc == arrivalUtc)
                return;
        }

        _hangDoi.Add(new ChuyenChoBao
        {
            DockIndex  = dockIndex,
            ArrivalUtc = arrivalUtc,
            PhutCho    = phutCho,
        });

        if (_drainRoutine == null)
            _drainRoutine = StartCoroutine(DrainRoutine());
    }

    /// <summary>
    /// Rút hàng đợi: đợi tới lúc ĐƯỢC PHÉP hiện (không tutorial, không popup khác,
    /// không ở scene bếp) rồi hiện từng popup một. Chuyến còn &lt; 1 phút lúc tới
    /// lượt (chờ lâu trong hàng / vào game sát giờ) → bỏ, coi như đã báo (GDD §3.5:
    /// chỉ báo khi chuyến kế còn ≥ 1 phút).
    /// </summary>
    private IEnumerator DrainRoutine()
    {
        while (_hangDoi.Count > 0)
        {
            // Đợi điều kiện hiện — poll thưa 0.25s cho nhẹ
            while (_dangHien || !DuocPhepHien())
                yield return new WaitForSecondsRealtime(0.25f);

            if (_hangDoi.Count == 0) break;

            ChuyenChoBao chuyen = _hangDoi[0];
            _hangDoi.RemoveAt(0);

            // Số phút hiển thị = SỐ THẬT của lịch (5 phút khi 1 bến, 10 phút khi
            // nhiều bến — GDD §3.2). Ưu tiên hỏi lại manager: popup có thể đã xếp
            // hàng khá lâu sau tutorial/popup khác, và GetMinutesToNextArrival tính
            // theo đúng thang thời gian game (tôn trọng debugTimeScale của Dev A),
            // khác với phép trừ UTC thô. Chỉ khi manager không còn giữ đúng chuyến
            // này mới dùng số phút kèm theo event.
            int phutHienThi = chuyen.PhutCho;
            DateTime arrivalHienTai;
            if (_manager != null &&
                _manager.TryGetNextArrivalUtc(chuyen.DockIndex, out arrivalHienTai) &&
                arrivalHienTai == chuyen.ArrivalUtc)
            {
                int phutLich = _manager.GetMinutesToNextArrival(chuyen.DockIndex);
                if (phutLich >= 0) phutHienThi = phutLich;
            }

            // Sát giờ/quá giờ — báo cũng vô nghĩa; đánh dấu đã tiêu thụ để không báo lại
            if (phutHienThi < 1 || chuyen.ArrivalUtc <= DateTime.UtcNow)
            {
                GhiDaBao(chuyen.DockIndex, chuyen.ArrivalUtc);
                continue;
            }

            yield return HienPopupRoutine(chuyen.DockIndex, chuyen.ArrivalUtc, phutHienThi);

            // Nghỉ 1 nhịp ngắn giữa 2 popup liên tiếp cho đỡ dội
            yield return new WaitForSecondsRealtime(0.15f);
        }

        _drainRoutine = null;
    }

    /// <summary>Điều kiện được phép hiện popup báo tàu ngay bây giờ.</summary>
    private bool DuocPhepHien()
    {
        // Tutorial đang chạy → không đè (GDD §5 edge 7)
        if (TutorialDangChay()) return false;

        // Popup khác đang mở (seed/market/train/tab mua bến...) → chờ
        if (FarmInputLock.IsPopupOpen) return false;
        if (FarmInputLock.IsSeedPopupOpen || FarmInputLock.IsMarketPopupOpen) return false;
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) return false;

        // Đang trong scene bếp (additive) → hoãn tới khi về farm (GDD §5 edge 6).
        // [QA B-4] Điều kiện này chỉ có tác dụng THẬT khi component còn sống lúc ở
        // bếp — nên tool đặt popup dưới canvas RIÊNG, không phải canvasPopupRoot
        // (thứ bị EnterCookingMode tắt). Xem HANDOFF §3.
        if (DangTrongSceneBep()) return false;

        // Thiếu tham chiếu UI thì đừng cố (defensive — tool chưa chạy)
        if (popupRoot == null || cardRect == null) return false;

        return true;
    }

    /// <summary>Scene bếp (additive) đang mở? Tên scene đọc từ field cho khớp FarmUIManager.</summary>
    private bool DangTrongSceneBep()
    {
        return !string.IsNullOrEmpty(cookingSceneName) &&
               SceneManager.GetSceneByName(cookingSceneName).isLoaded;
    }

    // =========================================================================
    //  Hiện / đóng popup
    // =========================================================================

    private IEnumerator HienPopupRoutine(int dockIndex, DateTime arrivalUtc, int phut)
    {
        _dangHien = true;

        // Đánh dấu đã báo NGAY LÚC HIỆN (không đợi bấm nút) — lỡ crash/thoát giữa
        // chừng cũng không báo lại chuyến này, đúng luật "mỗi chuyến 1 lần".
        GhiDaBao(dockIndex, arrivalUtc);

        // Số hiệu tàu = số bến (GDD §3.1) — đọc qua API contract của Dev A
        int soHieu = _manager != null ? _manager.BoatNumber(dockIndex) : dockIndex + 1;

        if (titleText != null)
            titleText.text = $"Tàu số {soHieu:00} sắp cập bến!";
        if (bodyText != null)
            bodyText.text = $"Tàu số {soHieu:00} sẽ cập bến sau {phut} phút! " +
                            "Chuẩn bị nguyên liệu, nấu món ngon tiếp đãi khách nhé!";

        FarmInputLock.RegisterPopupOpen();
        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);
        popupRoot.SetActive(true);

        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(MoAnimRoutine());

        // Đợi người chơi bấm "Đã rõ" (OnClickDaRo hạ cờ _dangHien)
        while (_dangHien)
            yield return null;
    }

    /// <summary>Nút "Đã rõ" — đóng popup với anim thu nhỏ + fade ngắn.</summary>
    private void OnClickDaRo()
    {
        if (!_dangHien || popupRoot == null || !popupRoot.activeSelf) return;

        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(DongAnimRoutine());
    }

    /// <summary>Mở: dim fade 0→dimAlpha + card 0.9→1 ease-out-back + chữ fade-in.</summary>
    private IEnumerator MoAnimRoutine()
    {
        if (dimImage != null)     SetAlpha(dimImage, 0f);
        if (contentGroup != null) contentGroup.alpha = 0f;
        if (cardRect != null)     cardRect.localScale = Vector3.one * 0.9f;

        float t = 0f;
        float tong = Mathf.Max(popSeconds, textFadeSeconds) + 0.05f;
        while (t < tong)
        {
            t += Time.unscaledDeltaTime;

            if (cardRect != null)
            {
                float p = Mathf.Clamp01(t / Mathf.Max(0.01f, popSeconds));
                cardRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, EaseOutBack(p));
            }
            if (dimImage != null)
            {
                float p = Mathf.Clamp01(t / Mathf.Max(0.01f, popSeconds * 0.8f));
                SetAlpha(dimImage, dimAlpha * p);
            }
            if (contentGroup != null)
            {
                // Chữ vào trễ nửa nhịp pop cho có tầng lớp
                float p = Mathf.Clamp01((t - popSeconds * 0.4f) / Mathf.Max(0.01f, textFadeSeconds));
                contentGroup.alpha = p;
            }
            yield return null;
        }

        if (cardRect != null)     cardRect.localScale = Vector3.one;
        if (dimImage != null)     SetAlpha(dimImage, dimAlpha);
        if (contentGroup != null) contentGroup.alpha = 1f;
        _animRoutine = null;
    }

    /// <summary>Đóng: thu 1→0.92 + fade toàn bộ trong ~0.15s rồi tắt + trả input lock.</summary>
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

        popupRoot.SetActive(false);
        if (cardRect != null)     cardRect.localScale = Vector3.one; // trả scale cho lần mở sau
        if (contentGroup != null) contentGroup.alpha = 1f;

        FarmInputLock.RegisterPopupClose(); // tự suppress world-click frame này
        _dangHien    = false;               // DrainRoutine tiếp tục chuyến kế (nếu có)
        _animRoutine = null;
    }

    // =========================================================================
    //  Persist "đã báo" — PlayerPrefs ticks dạng string (pattern BoatDockManager)
    // =========================================================================

    private static bool DaBaoChuyenNay(int dockIndex, DateTime arrivalUtc)
    {
        string raw = PlayerPrefs.GetString(string.Format(KeyDaBaoFormat, dockIndex), string.Empty);
        long ticks;
        if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks))
            return false;
        return ticks == arrivalUtc.Ticks;
    }

    private static void GhiDaBao(int dockIndex, DateTime arrivalUtc)
    {
        PlayerPrefs.SetString(string.Format(KeyDaBaoFormat, dockIndex),
            arrivalUtc.Ticks.ToString(CultureInfo.InvariantCulture));
        LuuGopPrefs.Hen(); // lưu gộp có trễ — pattern chung của dự án
    }

    // =========================================================================
    //  Check tutorial — dùng chung cho các popup boat
    // =========================================================================

    /// <summary>
    /// Tutorial đang chạy? — API THẬT của TutorialManager (quy ước chung của dự án,
    /// giống MissionHudButtonUI): có Instance trong scene VÀ cờ persist IsTutorialDone
    /// chưa bật. Dùng chung cho cả popup báo tàu lẫn popup mua bến, và cho
    /// TouristBoatUnlockFlow — một chỗ sửa duy nhất nếu luật đổi.
    /// </summary>
    public static bool TutorialDangChay()
    {
        return TutorialManager.Instance != null && !TutorialManager.IsTutorialDone;
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    /// <summary>Ease-out-back chuẩn (overshoot ~10%) — cùng "gu" bounce của TutorialGuideBoardUI.</summary>
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
