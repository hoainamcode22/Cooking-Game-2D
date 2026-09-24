#pragma warning disable CS0414
using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// UI trạng thái KHÓA của một bến tàu du lịch — gắn trên Dock_01..03 (BOAT-001).
///
/// Nhiệm vụ:
///  - Hiển thị/ẩn UI khóa (sprite mờ placeholder + teaser giá) theo BoatDockManager.
///  - Teaser đọc số từ BoatDockManager.Config (KHÔNG hardcode giá/level).
///  - Tap vào khóa (Collider2D, cùng pattern TrainWagonSlot):
///      • V2 (BOAT-002 §3.6): mở DockPurchasePopupUI — popup có giá, icon tiền,
///        nút MUA disable kèm lý do. Việc trừ tiền vẫn do BoatDockManager lo.
///      • Không có popup trong scene (chưa chạy tool UI) → GIỮ NGUYÊN hành vi V1:
///        đủ điều kiện → TryUnlockDock; thiếu → floating text hiện lý do.
///      • [QA M-6] Bắt ở NHẢ chuột (OnMouseUpAsButton) + ngưỡng di chuyển, và bỏ qua
///        khi đang kéo bản đồ / kéo hạt / kéo liềm / có popup khác — trước đây bắt ở
///        NHẤN XUỐNG nên chỉ cần chạm tay vào bảng rồi kéo map là mua/mở popup nhầm.
///  - OnDrawGizmos vẽ đường waypoint (line xanh) + Berth + BlindPoint để Sếp
///    chỉnh path bằng mắt trong Scene view (giống gizmo của FerryController).
///
/// Tham chiếu scene do TouristBoatSetupTool tự gán; nếu dựng tay thì kéo vào Inspector
/// (để trống sẽ tự dò theo tên con: "Berth", "Path", sibling "BlindPoint").
/// </summary>
[DisallowMultipleComponent]
public class BoatDockSlot : MonoBehaviour
{
    [Header("Bến số mấy (0 = bến 1 miễn phí mở qua intro)")]
    public int dockIndex;

    [Header("Tham chiếu scene (tool tự gán)")]
    [Tooltip("Điểm cập bến — con \"Berth\" của Dock.")]
    [SerializeField] private Transform berth;
    [Tooltip("Gốc chứa các waypoint WP_01..WP_03 — con \"Path\" của Dock. Thứ tự con = thứ tự tàu chạy (điểm mù → bến).")]
    [SerializeField] private Transform pathRoot;
    [Tooltip("Điểm mù ngoài khơi — con \"BlindPoint\" của BoatSystem (dùng chung cho các bến).")]
    [SerializeField] private Transform blindPoint;

    [Header("UI khóa")]
    [Tooltip("Gốc UI khóa (sprite mờ + teaser). Ẩn khi bến đã mở.")]
    [SerializeField] private GameObject lockRoot;
    [Tooltip("Text teaser \"Mở ở Lv12 · 2.000 vàng\" — nội dung set runtime từ Config.")]
    [SerializeField] private TextMeshPro teaserText;
    [Tooltip("Collider bắt tap vào nút khóa. Tắt khi bến đã mở.")]
    [SerializeField] private Collider2D tapCollider;

    [Header("Floating text (lý do từ chối)")]
    [SerializeField] private float floatingTextRise    = 80f;   // đơn vị world (map hệ tọa độ lớn)
    [SerializeField] private float floatingTextSeconds = 1.6f;

    // [FIX 2026-09-19] Hệ số phóng to biển cọc gỗ khóa bến (sprite 240x180 px, PPU 1) để chữ teaser không tràn mép.
    private const float BangGoScale = 1.9f;   // [2026-09-23] 1.35 -> 1.9: bang go to hon, chu nam gon trong mat bang

    // [QA M-6] Ngưỡng coi là "chạm" chứ không phải "kéo" (pixel màn hình).
    private const float NguongKeoPixel = 24f;

    private BoatDockManager _manager;      // giữ ref để unsubscribe an toàn lúc teardown
    private Vector2         _viTriNhan;    // vị trí màn hình lúc nhấn xuống
    private bool            _dangNhan;     // có nhịp nhấn hợp lệ đang chờ nhả không
    private DockPurchasePopupUI _popupMua; // cache popup mua (V2) — tìm 1 lần
    private TextMeshPro     _floatingText; // tái dùng 1 instance, không spam GameObject
    private Coroutine       _floatingRoutine;
    private Coroutine       _unlockFxRoutine;

    // Màu gizmo — "line xanh" theo spec, phân biệt với đường cyan của Ferry.
    private static readonly Color GizmoPathColor  = new Color(0.20f, 0.90f, 0.40f); // xanh lá
    private static readonly Color GizmoBerthColor = Color.yellow;                   // cầu tàu
    private static readonly Color GizmoBlindColor = Color.magenta;                  // điểm mù

    // =========================================================================
    //  Vòng đời
    // =========================================================================

    private void Start()
    {
        StartCoroutine(InitRoutine());
    }

    /// <summary>
    /// Đợi BoatDockManager sẵn sàng rồi mới subscribe + refresh — script execution
    /// order không đảm bảo manager Awake trước slot.
    /// </summary>
    private IEnumerator InitRoutine()
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
            Debug.LogWarning($"[TouristBoat] BoatDockSlot dock {dockIndex}: không tìm thấy BoatDockManager trong scene — UI khóa giữ nguyên trạng thái mặc định.");
            yield break;
        }

        _manager.OnDockUnlocked += HandleDockUnlocked;
        RefreshLockUI();
    }

    private void OnDestroy()
    {
        if (_manager != null)
            _manager.OnDockUnlocked -= HandleDockUnlocked;
    }

    // =========================================================================
    //  Input — tap vào nút khóa (Collider2D + OnMouseDown như TrainWagonSlot)
    // =========================================================================

    /// <summary>
    /// Chỉ GHI NHẬN nhịp nhấn (không hành động) — hành động dời sang lúc nhả để phân
    /// biệt "chạm để mở bảng" với "đặt tay lên bảng rồi kéo bản đồ" (QA M-6).
    /// </summary>
    private void OnMouseDown()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.DangChayTutorial) return;
        // [FIX 2026-09-04] Chặn click xuyên khi đang ở Bếp (scene phụ load additive) / đang mở popup.
        if (FarmInputLock.BlockWorldClickBySceneOrPopup) return;
        _dangNhan  = false;
        _viTriNhan = Vector2.zero;

        var mgr = _manager != null ? _manager : BoatDockManager.Instance;
        if (mgr == null) return;
        if (mgr.IsDockUnlocked(dockIndex)) return; // đã mở — collider lẽ ra đã tắt, guard cho chắc

        // m-1 (QA V1 — GIỮ NGUYÊN): bến 1 mở MIỄN PHÍ qua hội thoại intro — chặn tap
        // trước/trong intro để không ai "mua trộm" bến 1 (CanUnlockDock(0) trả true từ
        // L10), giữ trọn khoảnh khắc chuyến tàu đầu tiên do TouristBoatUnlockFlow đạo diễn.
        if (dockIndex == 0 && !mgr.IsIntroDone) return;

        // [QA M-6] Đang kéo bản đồ / kéo hạt giống / kéo liềm / có popup khác đang mở
        // → nhịp chạm này không phải ý định mở bảng khóa.
        if (FarmInputLock.BlockMapPan || FarmInputLock.IsDraggingSeed ||
            FarmInputLock.IsDraggingSickle || FarmInputLock.IsPopupOpen) return;

        _viTriNhan = ViTriConTro();
        _dangNhan  = true;
    }

    /// <summary>
    /// Nhả chuột/ngón tay trên collider hoặc tap trực tiếp để mở bảng mua slot bến tàu.
    /// </summary>
    private void OnMouseUpAsButton()
    {
        // [FIX 2026-09-04] Chặn click xuyên khi đang ở Bếp (scene phụ load additive) / đang mở popup.
        if (FarmInputLock.BlockWorldClickBySceneOrPopup) return;
        _dangNhan = false;

        var mgr = _manager != null ? _manager : BoatDockManager.Instance;
        if (mgr == null) return;
        if (mgr.IsDockUnlocked(dockIndex)) return;
        if (dockIndex == 0 && !mgr.IsIntroDone) return;

        // [VÒNG 2026-09-11] Âm thanh click + hiệu ứng nảy đàn hồi (juicy bounce punch) cực mượt mà
        AudioManager.Instance?.PlayUIClick();
        if (lockRoot != null && lockRoot.activeSelf)
        {
            if (_tapPunchRoutine != null) StopCoroutine(_tapPunchRoutine);
            _tapPunchRoutine = StartCoroutine(TapPunchRoutine());
        }

        // [2026-09-24] Chua du cap: dong chu "This dock unlocks at Level N" hien len roi mo dan
        if (mgr.Config != null && LockedHintFX.ChanTheoCap("Bến tàu này", CapMo(mgr.Config))) return;

        // ── V2: tap bảng khóa → MỞ POPUP MUA ──
        if (_popupMua == null)
            _popupMua = FindFirstObjectByType<DockPurchasePopupUI>(FindObjectsInactive.Include);

        if (_popupMua != null)
        {
            _popupMua.MoChoBen(dockIndex);
            return;
        }

        // ── Không có popup trong scene (chưa chạy tool UI) → GIỮ NGUYÊN đường V1 ──
        // Không ai bị mất đường mua bến chỉ vì quên chạy tool.
        if (mgr.CanUnlockDock(dockIndex, out string reason))
        {
            // Đủ điều kiện → nhờ manager mở (manager tự trừ tiền qua FarmEconomyManager).
            // Hiệu ứng punch chạy ở HandleDockUnlocked khi event OnDockUnlocked bắn về.
            if (!mgr.TryUnlockDock(dockIndex))
            {
                // Manager từ chối phút chót (vd tiền vừa bị trừ nơi khác) — báo nhẹ nhàng.
                ShowFloatingText("Chưa mở được, thử lại nhé!");
                Debug.Log($"[TouristBoat] TryUnlockDock({dockIndex}) trả false dù CanUnlockDock true.");
            }
        }
        else
        {
            // Thiếu level/tiền — hiện đúng lý do manager đưa (text tiếng Việt từ Dev A).
            ShowFloatingText(reason);
        }
    }

    private Coroutine _tapPunchRoutine;
    private IEnumerator TapPunchRoutine()
    {
        if (lockRoot == null) yield break;
        Transform tr = lockRoot.transform;
        Vector3 baseScale = Vector3.one;

        float duration = 0.18f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            // Co nhẹ xuống 0.92 rồi nảy lên 1.07 rồi về 1.0
            float scale = 1f - Mathf.Sin(p * Mathf.PI) * 0.08f + Mathf.Sin(p * Mathf.PI * 2f) * 0.05f;
            tr.localScale = baseScale * scale;
            yield return null;
        }
        tr.localScale = baseScale;
        _tapPunchRoutine = null;
    }

    private Vector3 _baseLockLocalPos = Vector3.zero;
    private bool _savedBasePos = false;

    private void Update()
    {
        // Hiệu ứng lơ lửng nhấp nhô nhẹ nhàng trên sóng nước (subtle water bobbing)
        if (lockRoot != null && lockRoot.activeSelf)
        {
            if (!_savedBasePos)
            {
                _baseLockLocalPos = lockRoot.transform.localPosition;
                _savedBasePos = true;
            }

            float bob = Mathf.Sin(Time.time * 2.2f + dockIndex * 1.4f) * 2.5f;
            lockRoot.transform.localPosition = _baseLockLocalPos + new Vector3(0f, bob, 0f);
        }
    }

    /// <summary>
    /// Vị trí con trỏ/ngón tay trên MÀN HÌNH (pixel). Dùng Input cũ cho gọn — đường
    /// vào đây vốn đã là OnMouseDown/OnMouseUpAsButton (API input cũ của Unity) nên
    /// không thêm phụ thuộc mới.
    /// </summary>
    private static Vector2 ViTriConTro()
    {
        return Input.mousePosition;
    }

    // =========================================================================
    //  Trạng thái khóa
    // =========================================================================

    private void HandleDockUnlocked(int unlockedIndex)
    {
        if (unlockedIndex != dockIndex) return;

        _dangNhan = false; // bến vừa mở — huỷ nhịp nhấn đang chờ (nếu có)

        if (_unlockFxRoutine != null) StopCoroutine(_unlockFxRoutine);
        _unlockFxRoutine = StartCoroutine(UnlockFxRoutine());
    }

    /// <summary>Cập nhật UI khóa theo trạng thái hiện tại của manager (gọi lúc init/re-init scene).</summary>
    public void RefreshLockUI()
    {
        var mgr = _manager != null ? _manager : BoatDockManager.Instance;
        if (mgr == null) return;

        bool unlocked = mgr.IsDockUnlocked(dockIndex);

        if (lockRoot != null)
        {
            lockRoot.SetActive(!unlocked);

            if (!unlocked)
            {
                // [VÒNG 2026-09-11] Áp dụng Biển Cọc Gỗ Cắm Cầu Tàu (Wooden Signpost) đẹp mắt
                var sr = lockRoot.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Sprite plaque = FarmGame.UI.TouristBoatArtRuntime.GetDockPlaqueSprite();
                    if (plaque != null)
                    {
                        sr.sprite = plaque;
                        sr.color = Color.white;
                        sr.drawMode = SpriteDrawMode.Simple;
                    }
                    sr.sortingOrder = 55; // Nổi bật trên mặt nước
                }

                // [FIX 2026-09-19] Bảng gỗ to lên 1.35x để chữ nằm gọn trong khung (trước: Vector3.one).
                // Scale quanh gốc lockRoot = pivot chân cọc (0.5, 0.05) nên cọc vẫn cắm đúng chỗ cũ.
                lockRoot.transform.localScale = new Vector3(BangGoScale, BangGoScale, 1f);

                // Tắt hoàn toàn LockIcon placeholder nếu có
                Transform icon = lockRoot.transform.Find("LockIcon");
                if (icon != null) icon.gameObject.SetActive(false);

                // Căn chỉnh chữ teaser to, rõ nét, font Baloo2 tiếng Việt chuẩn, nằm gọn trên mặt bảng gỗ
                if (teaserText != null)
                {
                    var font = Resources.Load<TMP_FontAsset>("Fonts/Baloo2 SDF");
                    if (font == null) font = TMP_Settings.defaultFontAsset;
                    if (font != null) teaserText.font = font;

                    // [2026-09-23] Dong 1 = "UNLOCKS AT LV 12" (1 dong, gon, can giua).
                    // Dong 2 = [icon vang/kim cuong] + so tien — la 2 object rieng Cost_Icon + Cost_Text.
                    teaserText.text = Loc.TF("MỞ Ở CẤP {0}", CapMo(mgr.Config));
                    teaserText.isOrthographic = true;
                    teaserText.fontSize = 20f;
                    teaserText.enableAutoSizing = true;
                    teaserText.fontSizeMax = 20f;
                    teaserText.fontSizeMin = 12f;
                    teaserText.fontStyle = FontStyles.Bold;
                    teaserText.alignment = TextAlignmentOptions.Center;
                    teaserText.textWrappingMode = TextWrappingModes.NoWrap;
                    teaserText.overflowMode = TextOverflowModes.Overflow;
                    teaserText.color = new Color(1f, 0.98f, 0.88f, 1f); // Màu kem vàng sáng
                    teaserText.outlineWidth = 0.22f;
                    teaserText.outlineColor = new Color(0.24f, 0.12f, 0.04f, 1f); // Viền nâu gỗ đậm

                    var mr = teaserText.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sortingOrder = 58;

                    var rt = teaserText.rectTransform;
                    if (rt != null)
                    {
                        // [FIX 2026-09-19] Rect chữ = ~85% bề rộng mặt bảng (224px) x chiều cao ván trong (~98px).
                        // Trước: 210x90 (sát mép, 3 dòng tràn dọc). Đơn vị local của lockRoot, tự to theo scale 1.35.
                        rt.sizeDelta = new Vector2(196f, 30f);
                    }
                    teaserText.transform.localPosition = new Vector3(0f, 140f, -0.5f);   // nua tren mat bang
                    DungDongGia(mgr.Config, teaserText.font, mr != null ? mr.sortingOrder : 58);
                }

                // Đảm bảo BoxCollider2D phủ vừa khít kích thước toàn bộ biển cọc gỗ để chạm là ăn 100%
                var col = lockRoot.GetComponent<BoxCollider2D>();
                if (col == null) col = GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    // Collider nằm trên lockRoot thì tự to theo scale; nếu fallback nằm trên slot (không scale) thì nhân tay.
                    float k = (col.gameObject == lockRoot) ? 1f : BangGoScale;
                    col.size = new Vector2(240f * k, 180f * k);
                    col.offset = new Vector2(0f, 90f * k);
                }
            }
        }

        if (tapCollider != null) tapCollider.enabled = !unlocked;
    }

    /// <summary>
    /// [VÒNG 2026-09-11] Teaser mở khóa thiết kế mới — chữ to, sáng rõ, in đậm trên bảng gỗ cắm cầu tàu:
    ///   dock 0: "MỞ Ở CẤP 10 \n ★ MIỄN PHÍ ★"
    ///   dock 1: "MỞ Ở CẤP 12 \n 2.000 VÀNG"
    ///   dock 2: "MỞ Ở CẤP 14 \n 25 KIM CƯƠNG"
    /// </summary>
    private int CapMo(TouristBoatConfig c)
    {
        if (c == null) return 0;
        return dockIndex == 0 ? c.unlockLevel : dockIndex == 1 ? c.dock2Level : c.dock3Level;
    }

    /// <summary>
    /// [2026-09-23] Dong gia o nua duoi mat bang: [icon] + so. Icon vang (ben 2) / kim cuong (ben 3);
    /// ben 1 mien phi thi chi chu "FREE" (truoc dung ky tu ★ ma font khong co => hien o vuong).
    /// Cap icon + chu duoc CAN GIUA theo tong be ngang. Object Cost_Icon / Cost_Text nam trong lockRoot.
    /// </summary>
    private void DungDongGia(TouristBoatConfig c, TMP_FontAsset font, int thuTu)
    {
        if (lockRoot == null || c == null) return;
        string chu; Color mau; Sprite icon = null;
        switch (dockIndex)
        {
            case 0:  chu = Loc.T("MIỄN PHÍ"); mau = new Color(0.51f, 0.93f, 0.93f); break;
            case 1:  chu = FormatVN(c.dock2GoldCost); mau = new Color(1f, 0.84f, 0f); icon = Resources.Load<Sprite>("UI/Standard/icon_gold"); break;
            default: chu = c.dock3GemCost.ToString(); mau = new Color(0.45f, 0.73f, 1f); icon = Resources.Load<Sprite>("UI/Standard/kimcuong-removebg-preview"); break;
        }

        // Chu
        var tTr = lockRoot.transform.Find("Cost_Text");
        TextMeshPro t = tTr != null ? tTr.GetComponent<TextMeshPro>() : null;
        if (t == null)
        {
            var go = new GameObject("Cost_Text", typeof(RectTransform));
            go.transform.SetParent(lockRoot.transform, false);
            t = go.AddComponent<TextMeshPro>();
        }
        if (font != null) t.font = font;
        t.text = chu;
        t.isOrthographic = true;
        t.enableAutoSizing = false;
        t.fontSize = 24f;
        t.fontStyle = FontStyles.Bold;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.color = mau;
        t.outlineWidth = 0.22f;
        t.outlineColor = new Color(0.24f, 0.12f, 0.04f, 1f);
        t.rectTransform.sizeDelta = new Vector2(160f, 30f);
        t.rectTransform.pivot = new Vector2(0f, 0.5f);
        var tmr = t.GetComponent<MeshRenderer>(); if (tmr != null) tmr.sortingOrder = thuTu;

        // Icon
        var iTr = lockRoot.transform.Find("Cost_Icon");
        SpriteRenderer sr = iTr != null ? iTr.GetComponent<SpriteRenderer>() : null;
        if (sr == null && icon != null)
        {
            var go = new GameObject("Cost_Icon");
            go.transform.SetParent(lockRoot.transform, false);
            sr = go.AddComponent<SpriteRenderer>();
        }
        const float ICON = 32f, KHE = 6f, Y = 102f;
        float iconW = 0f;
        if (sr != null)
        {
            sr.gameObject.SetActive(icon != null);
            if (icon != null)
            {
                sr.sprite = icon;
                sr.sortingOrder = thuTu;
                float cao = Mathf.Max(0.0001f, icon.bounds.size.y), rong = Mathf.Max(0.0001f, icon.bounds.size.x);
                float k = ICON / Mathf.Max(cao, rong);
                sr.transform.localScale = new Vector3(k, k, 1f);
                iconW = rong * k;
            }
        }

        // Can giua ca cum [icon + khe + chu]
        float chuW = t.GetPreferredValues(chu).x;
        float tong = iconW + (iconW > 0f ? KHE : 0f) + chuW;
        float x0 = -tong * 0.5f;
        if (sr != null && iconW > 0f) sr.transform.localPosition = new Vector3(x0 + iconW * 0.5f, Y, -0.5f);
        t.alignment = TextAlignmentOptions.Left;
        t.transform.localPosition = new Vector3(x0 + (iconW > 0f ? iconW + KHE : 0f), Y, -0.5f);
    }

    private string BuildTeaserText(TouristBoatConfig config)
    {
        if (config == null) return string.Empty;

        switch (dockIndex)
        {
            case 0:
                return "<b>" + Loc.TF("MỞ Ở CẤP {0}", config.unlockLevel) + "</b>\n<size=75%><color=#81ECEC>★ " + Loc.T("MIỄN PHÍ") + " ★</color></size>";
            case 1:
                return "<b>" + Loc.TF("MỞ Ở CẤP {0}", config.dock2Level) + "</b>\n<size=80%><color=#FFD700>" + Loc.TF("{0} VÀNG", FormatVN(config.dock2GoldCost)) + "</color></size>";
            case 2:
                return "<b>" + Loc.TF("MỞ Ở CẤP {0}", config.dock3Level) + "</b>\n<size=80%><color=#74B9FF>" + Loc.TF("{0} KIM CƯƠNG", config.dock3GemCost) + "</color></size>";
            default:
                return string.Empty;
        }
    }

    // [QA m-6] KHÔNG dùng CultureInfo.GetCultureInfo("vi-VN"): build bật Invariant
    // Globalization (hay gặp với IL2CPP mobile) sẽ ném CultureNotFoundException.
    private static readonly NumberFormatInfo DinhDangSoVN = new NumberFormatInfo
    {
        NumberGroupSeparator   = ".",
        NumberDecimalSeparator = ",",
        NumberGroupSizes       = new[] { 3 },
    };

    /// <summary>Định dạng số kiểu Việt Nam: 2000 → "2.000".</summary>
    private static string FormatVN(int amount)
        => amount.ToString("N0", DinhDangSoVN);

    /// <summary>Punch scale nhỏ rồi thu về 0 và ẩn UI khóa — dopamine lúc mở bến.</summary>
    private IEnumerator UnlockFxRoutine()
    {
        if (tapCollider != null) tapCollider.enabled = false;

        if (lockRoot != null && lockRoot.activeSelf)
        {
            Transform t = lockRoot.transform;
            Vector3 baseScale = t.localScale;

            // Punch lên 1.18 rồi về (sin nửa chu kỳ — giống PunchScaleRoutine của TutorialGuideBoardUI)
            float punchDur = 0.22f, elapsed = 0f;
            while (elapsed < punchDur)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / punchDur);
                t.localScale = Vector3.Lerp(baseScale, baseScale * 1.18f, Mathf.Sin(p * Mathf.PI));
                yield return null;
            }

            // Thu về 0 rồi ẩn
            float shrinkDur = 0.18f; elapsed = 0f;
            while (elapsed < shrinkDur)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(baseScale, Vector3.zero, Mathf.Clamp01(elapsed / shrinkDur));
                yield return null;
            }

            lockRoot.SetActive(false);
            t.localScale = baseScale; // trả scale để lần bật lại (nếu có) không bị méo
        }

        _unlockFxRoutine = null;
    }

    // =========================================================================
    //  Floating text — lý do từ chối mở khóa
    // =========================================================================

    /// <summary>Hiện text nổi bay lên + mờ dần tại vị trí nút khóa (tái dùng 1 instance).</summary>
    private void ShowFloatingText(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (_floatingText == null)
        {
            var go = new GameObject("FloatingText_Dock");
            go.transform.SetParent(transform, false);
            // TMP world-space: fontSize 56 * scale 10 ≈ chữ cao ~56 unit (viewport mặc định ~1500 unit)
            go.transform.localScale = new Vector3(10f, 10f, 1f);
            _floatingText = go.AddComponent<TextMeshPro>();
            _floatingText.fontSize         = 56;
            _floatingText.alignment        = TextAlignmentOptions.Center;
            _floatingText.textWrappingMode = TextWrappingModes.NoWrap;
            _floatingText.overflowMode     = TextOverflowModes.Overflow;   // [FIX QA] Text noi KHONG co sizeDelta (rect mac dinh ti hon) => Ellipsis cat cut chu. Giu Overflow.
            _floatingText.color            = new Color(1f, 0.95f, 0.75f); // trắng ấm, thân thiện
            var mr = _floatingText.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // [FIX 2026-09-03] Thiếu sortingLayerName ⇒ rơi về layer "Default" (thấp hơn "Objects" của khách) ⇒ khách đè lên chữ. Ép về cùng layer với thân tàu.
                mr.sortingLayerName = "ObjectsFront";
                mr.sortingOrder = 210;
            }
        }

        Vector3 basePos = (lockRoot != null ? lockRoot.transform.position : transform.position)
                          + Vector3.up * 130f;
        _floatingText.transform.position = basePos;
        _floatingText.text = message;
        _floatingText.gameObject.SetActive(true);

        if (_floatingRoutine != null) StopCoroutine(_floatingRoutine);
        _floatingRoutine = StartCoroutine(FloatingTextRoutine(basePos));
    }

    private IEnumerator FloatingTextRoutine(Vector3 from)
    {
        float t = 0f;
        Color baseColor = _floatingText.color; baseColor.a = 1f;

        while (t < floatingTextSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / floatingTextSeconds);
            _floatingText.transform.position = from + Vector3.up * (floatingTextRise * Mathf.SmoothStep(0f, 1f, p));
            baseColor.a = 1f - p * p; // giữ rõ lúc đầu, mờ nhanh về cuối
            _floatingText.color = baseColor;
            yield return null;
        }

        _floatingText.gameObject.SetActive(false);
        baseColor.a = 1f;
        _floatingText.color = baseColor;
        _floatingRoutine = null;
    }

    // =========================================================================
    //  Gizmos — Sếp chỉnh path bằng mắt trong Scene view
    // =========================================================================

    private void OnDrawGizmos()
    {
        ResolveSceneRefsIfMissing();

        // Chuỗi điểm tàu chạy: BlindPoint → WP theo thứ tự con của Path → Berth
        Vector3? prev = null;

        if (blindPoint != null)
        {
            Gizmos.color = GizmoBlindColor;
            Gizmos.DrawSphere(blindPoint.position, 30f);
            prev = blindPoint.position;
        }

        Gizmos.color = GizmoPathColor;
        if (pathRoot != null)
        {
            for (int i = 0; i < pathRoot.childCount; i++)
            {
                Transform wp = pathRoot.GetChild(i);
                if (wp == null) continue;

                Gizmos.color = GizmoPathColor;
                Gizmos.DrawSphere(wp.position, 18f);
                if (prev.HasValue) Gizmos.DrawLine(prev.Value, wp.position);
                prev = wp.position;
            }
        }

        if (berth != null)
        {
            if (prev.HasValue)
            {
                Gizmos.color = GizmoPathColor;
                Gizmos.DrawLine(prev.Value, berth.position);
            }
            Gizmos.color = GizmoBerthColor;
            Gizmos.DrawSphere(berth.position, 26f);
            // Vành ngoài đánh dấu "cầu tàu" cho dễ nhận ra giữa đám sphere
            Gizmos.DrawWireSphere(berth.position, 44f);
        }
    }

    /// <summary>Tự dò tham chiếu theo tên khi chưa gán (dựng tay không qua tool vẫn thấy gizmo).</summary>
    private void ResolveSceneRefsIfMissing()
    {
        if (berth == null)    berth    = transform.Find("Berth");
        if (pathRoot == null) pathRoot = transform.Find("Path");
        if (blindPoint == null && transform.parent != null)
            blindPoint = transform.parent.Find("BlindPoint");
    }
}
