using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Intro mở khóa bến tàu du lịch khi đạt level (BOAT-001 §3.1) — gắn trên BoatSystem.
///
/// Luồng: đạt config.unlockLevel (nghe FarmLevelManager.OnLevelChanged + check lúc Start
/// để bắt save đã qua level) && !BoatDockManager.IsIntroDone →
///   1. Dựng overlay chặn UI + hiện 4 câu hội thoại từ config.introDialogue
///      (typewriter, tap để qua câu — tap lúc đang gõ thì hiện hết câu; câu cuối tự đóng).
///   2. Camera zoom mượt tới GetDockBerth(0) — ưu tiên public API
///      CameraController.CinematicFocus (một chủ camera duy nhất, giống TutorialCameraFocus);
///      không có CameraController thì tự lerp Camera.main và trả đúng giá trị cũ.
///   3. UnlockDockFree(0) → tàu 1 xuất phát từ điểm mù.
///   4. Đợi vài giây cho người chơi thấy tàu chạy vào → trả camera → MarkIntroDone().
///
/// KHÔNG tái dùng TutorialGuideBoardUI: component đó coupled cứng với TutorialManager
/// (nút confirm gọi thẳng TutorialManager.Instance.ConfirmGuidePopup()) và text từng
/// trang phải wire sẵn trong Inspector — không có API truyền hội thoại động. Bảng
/// hội thoại ở đây tự dựng runtime, cùng style (bounce-in + typewriter).
///
/// GHI CHÚ KHÓA INPUT: pan/zoom người chơi bị khóa qua CinematicFocus(lockInput:true);
/// UI bị chặn bởi Image blocker (raycastTarget). FarmInputLock chỉ thấy chỗ ĐỌC
/// BlockMapPan/BlockMapZoom trong codebase, không chắc có setter public → KHÔNG gọi,
/// nghĩa là object world nhận OnMouseDown vẫn tap được trong vài giây intro (vô hại,
/// đã ghi vào mục cần lead review).
/// </summary>
[DisallowMultipleComponent]
public class TouristBoatUnlockFlow : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("OrthoSize khi zoom vào bến (theo scale thật của map: default 750, tutorial dùng 460).")]
    [SerializeField] private float introZoomOrthoSize = 460f;
    [Tooltip("Thời gian lia camera tới bến / trả về (giây).")]
    [SerializeField] private float cameraMoveSeconds = 1.2f;
    [Tooltip("Thời gian đứng ngắm tàu chạy vào bến trước khi trả camera (giây).")]
    [SerializeField] private float boatWatchSeconds = 4f;

    [Header("Hội thoại")]
    [Tooltip("Tốc độ typewriter (giây/ký tự) — cùng nhịp TutorialGuideBoardUI.")]
    [SerializeField] private float charSeconds = 0.03f;
    [Tooltip("Câu cuối tự đóng sau bấy nhiêu giây (vẫn tap được để đóng sớm).")]
    [SerializeField] private float lastLineAutoCloseSeconds = 2.2f;

    private bool _running; // intro đang chạy
    private bool _done;    // intro đã xong trong session này (hoặc IsIntroDone từ save)
    private bool _subscribed;

    // Overlay UI (tự dựng runtime, hủy khi intro xong)
    private GameObject      _overlayRoot;
    private Image           _blocker;
    private Image           _panel;
    private TextMeshProUGUI _dialogueText;
    private TextMeshProUGUI _hintText;

    // Camera save/restore
    private CameraController _cc;
    private Vector3          _savedCamPos;
    private float            _savedCamSize;

    // =========================================================================
    //  Vòng đời + trigger
    // =========================================================================

    private void Start()
    {
        StartCoroutine(BootRoutine());
    }

    /// <summary>
    /// Đợi FarmLevelManager + BoatDockManager SẴN SÀNG THẬT SỰ (IsReady = đã
    /// LoadFromPrefs xong) rồi mới subscribe và check — M-1 (QA): thứ tự Start
    /// không bảo đảm, đọc IsIntroDone trước khi manager load prefs có thể thấy
    /// false giả và replay intro với save đã IntroDone. Đồng thời bắt cả trường
    /// hợp load save đã ≥ unlockLevel (FarmLevelManager.Start đã bắn
    /// OnLevelChanged trước khi mình kịp subscribe).
    /// </summary>
    private IEnumerator BootRoutine()
    {
        float waited = 0f;
        while ((FarmLevelManager.Instance == null ||
                BoatDockManager.Instance == null ||
                !BoatDockManager.Instance.IsReady) && waited < 8f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (FarmLevelManager.Instance == null || BoatDockManager.Instance == null ||
            !BoatDockManager.Instance.IsReady)
        {
            Debug.LogWarning("[TouristBoat] UnlockFlow: FarmLevelManager/BoatDockManager chưa sẵn sàng (IsReady=false — thiếu config?) — intro không chạy.");
            yield break;
        }

        FarmLevelManager.Instance.OnLevelChanged += HandleLevelChanged;
        _subscribed = true;

        TryStartIntro();
    }

    private void OnDestroy()
    {
        if (_subscribed && FarmLevelManager.Instance != null)
            FarmLevelManager.Instance.OnLevelChanged -= HandleLevelChanged;
        if (_overlayRoot != null)
            Destroy(_overlayRoot);
    }

    private void HandleLevelChanged(int _) => TryStartIntro();

    /// <summary>Check điều kiện và khởi động intro (idempotent — gọi bao nhiêu lần cũng chỉ chạy 1 lần).</summary>
    private void TryStartIntro()
    {
        if (_running || _done) return;

        var mgr = BoatDockManager.Instance;
        var lvl = FarmLevelManager.Instance;
        if (mgr == null || lvl == null) return;

        if (mgr.IsIntroDone) { _done = true; return; } // persist flag — chỉ chạy 1 lần

        var cfg = mgr.Config;
        if (cfg == null)
        {
            Debug.LogWarning("[TouristBoat] UnlockFlow: BoatDockManager.Config chưa gán — chạy TouristBoatSetupTool hoặc kéo config vào Inspector.");
            return;
        }

        // HasReached bắt cả trường hợp nhảy cóc nhiều level 1 lúc (GDD edge #1)
        if (!lvl.HasReached(cfg.unlockLevel)) return;

        StartCoroutine(IntroRoutine());
    }

    // =========================================================================
    //  Intro chính
    // =========================================================================

    private IEnumerator IntroRoutine()
    {
        _running = true;
        var mgr = BoatDockManager.Instance;
        var cfg = mgr.Config;
        Debug.Log($"[TouristBoat] Intro mở khóa bến tàu bắt đầu (unlockLevel={cfg.unlockLevel}).");

        BuildOverlay();

        // ── 1. Hội thoại 4 câu từ config ────────────────────────────────────
        string[] lines = cfg.introDialogue;
        if (lines != null && lines.Length > 0)
        {
            yield return ShowPanelRoutine();
            for (int i = 0; i < lines.Length; i++)
                yield return ShowLineRoutine(lines[i], isLast: i == lines.Length - 1);
            yield return HidePanelRoutine();
        }
        else
        {
            Debug.LogWarning("[TouristBoat] config.introDialogue trống — bỏ qua phần hội thoại.");
        }

        // Sau hội thoại: bỏ lớp tối nhưng GIỮ raycast chặn UI tới hết intro
        if (_blocker != null) _blocker.color = new Color(0f, 0f, 0f, 0f);

        // ── 2. Camera zoom tới bến 1 ────────────────────────────────────────
        Transform berth = mgr.GetDockBerth(0);
        bool hasCamera = berth != null && Camera.main != null;
        if (berth == null)
            Debug.LogWarning("[TouristBoat] GetDockBerth(0) == null — bỏ qua đoạn camera, vẫn mở bến.");

        if (hasCamera)
            yield return FocusCameraRoutine(berth.position);

        // ── 3. Mở bến 1 miễn phí → tàu xuất phát từ điểm mù ─────────────────
        mgr.UnlockDockFree(0);

        // ── 4. Ngắm tàu chạy vào rồi trả camera ─────────────────────────────
        if (hasCamera)
        {
            yield return new WaitForSeconds(boatWatchSeconds);
            yield return RestoreCameraRoutine();
        }

        // ── 5. Chốt: intro chỉ chạy 1 lần ───────────────────────────────────
        mgr.MarkIntroDone();

        if (_overlayRoot != null) Destroy(_overlayRoot);
        _overlayRoot = null;

        if (_subscribed && FarmLevelManager.Instance != null)
        {
            FarmLevelManager.Instance.OnLevelChanged -= HandleLevelChanged;
            _subscribed = false;
        }

        _done    = true;
        _running = false;
        Debug.Log("[TouristBoat] Intro mở khóa bến tàu hoàn tất — MarkIntroDone().");
    }

    // =========================================================================
    //  Camera — ưu tiên CameraController.CinematicFocus, fallback tự lerp
    // =========================================================================

    private IEnumerator FocusCameraRoutine(Vector3 target)
    {
        Camera cam = Camera.main;
        _cc = cam != null ? cam.GetComponent<CameraController>() : null;

        if (_cc != null)
        {
            // Một chủ camera duy nhất (nguyên tắc của TutorialCameraFocus) — lưu vị trí
            // gốc qua public API rồi để CameraController tự SmoothDamp tới target.
            _savedCamPos  = _cc.CurrentPosition;
            _savedCamSize = _cc.CurrentSize;
            _cc.CinematicFocus(target, introZoomOrthoSize, lockInput: true); // khóa luôn pan/zoom
            yield return new WaitForSeconds(cameraMoveSeconds);              // SmoothDamp 0.45s → 1.2s là tới nơi
        }
        else if (cam != null)
        {
            // Fallback: KHÔNG đụng CameraController — tự lerp Camera.main, nhớ giá trị gốc
            _savedCamPos  = cam.transform.position;
            _savedCamSize = cam.orthographicSize;
            yield return LerpCameraRoutine(cam,
                new Vector3(target.x, target.y, _savedCamPos.z), introZoomOrthoSize);
        }
    }

    private IEnumerator RestoreCameraRoutine()
    {
        if (_cc != null)
        {
            // Giống TutorialCameraFocus.RestoreCamera: lia về giá trị gốc, trả input ngay
            _cc.CinematicFocus(_savedCamPos, _savedCamSize, lockInput: false);
            _cc.EndCinematic();
            yield return new WaitForSeconds(0.6f); // cho camera lướt gần về chỗ cũ rồi mới gỡ overlay
        }
        else if (Camera.main != null)
        {
            Camera cam = Camera.main;
            yield return LerpCameraRoutine(cam, _savedCamPos, _savedCamSize);
            cam.transform.position = _savedCamPos;   // trả ĐÚNG giá trị cũ, không lệch tích lũy
            cam.orthographicSize   = _savedCamSize;
        }
    }

    /// <summary>Lerp mượt position + orthographicSize của camera trong cameraMoveSeconds (ease SmoothStep).</summary>
    private IEnumerator LerpCameraRoutine(Camera cam, Vector3 toPos, float toSize)
    {
        Vector3 fromPos  = cam.transform.position;
        float   fromSize = cam.orthographicSize;
        float   t        = 0f;

        while (t < cameraMoveSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / cameraMoveSeconds));
            cam.transform.position = Vector3.Lerp(fromPos, toPos, p);
            cam.orthographicSize   = Mathf.Lerp(fromSize, toSize, p);
            yield return null;
        }

        cam.transform.position = toPos;
        cam.orthographicSize   = toSize;
    }

    // =========================================================================
    //  Hội thoại — bảng tự dựng cùng style TutorialGuideBoardUI
    // =========================================================================

    private IEnumerator ShowLineRoutine(string line, bool isLast)
    {
        _dialogueText.text = line;
        _dialogueText.maxVisibleCharacters = 0;
        _hintText.gameObject.SetActive(false);

        yield return null; // nuốt tap của câu trước / lúc panel vừa mở

        // Typewriter — tap lúc đang gõ thì hiện hết câu ngay
        int   total   = line.Length;
        int   visible = 0;
        float t       = 0f;
        while (visible < total)
        {
            if (TapDownThisFrame()) break;
            t += Time.unscaledDeltaTime;
            int target = Mathf.Min(total, Mathf.FloorToInt(t / Mathf.Max(0.001f, charSeconds)));
            if (target != visible)
            {
                visible = target;
                _dialogueText.maxVisibleCharacters = visible;
            }
            yield return null;
        }
        _dialogueText.maxVisibleCharacters = total;

        // Chờ tap để qua câu; câu cuối tự đóng sau lastLineAutoCloseSeconds.
        // Không dùng glyph đặc biệt/emoji (▸) — font TMP mặc định thiếu glyph
        // (quyết định lead sau QA). Sếp có thể thêm lại nếu font dự án có glyph.
        _hintText.text = isLast ? "Nhìn ra biển nào!" : "Chạm để tiếp tục";
        _hintText.gameObject.SetActive(true);

        float autoClose = isLast ? lastLineAutoCloseSeconds : float.PositiveInfinity;
        float waited    = 0f;
        yield return null; // tránh tap vừa skip typewriter lại advance luôn câu
        while (waited < autoClose)
        {
            if (TapDownThisFrame()) break;
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        _hintText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Tap/click down frame này? Cùng chiến lược 2 tầng input của CameraController:
    /// New Input System trước, fallback old Input (Unity Simulator).
    /// </summary>
    private static bool TapDownThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

        if (Input.GetMouseButtonDown(0)) return true; // legacy fallback

        return false;
    }

    // =========================================================================
    //  Overlay UI dựng runtime
    // =========================================================================

    /// <summary>Canvas overlay: blocker tối + bảng hội thoại đáy màn hình (style guide board).</summary>
    private void BuildOverlay()
    {
        _overlayRoot = new GameObject("TouristBoatIntroOverlay");
        var canvas = _overlayRoot.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // nổi trên HUD farm

        var scaler = _overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        _overlayRoot.AddComponent<GraphicRaycaster>(); // để blocker thật sự chặn UI bên dưới

        // Blocker: tối nhẹ lúc hội thoại, alpha 0 (vẫn chặn raycast) lúc camera chạy
        _blocker = CreateImage("Blocker", _overlayRoot.transform, new Color(0f, 0f, 0f, 0.35f));
        StretchFull(_blocker.rectTransform);

        // Bảng hội thoại — nền kem ấm, đáy màn hình (placeholder phẳng, thay art sau)
        _panel = CreateImage("DialoguePanel", _overlayRoot.transform, new Color(1f, 0.96f, 0.86f, 0.97f));
        RectTransform prt = _panel.rectTransform;
        prt.anchorMin        = new Vector2(0.5f, 0f);
        prt.anchorMax        = new Vector2(0.5f, 0f);
        prt.pivot            = new Vector2(0.5f, 0f);
        prt.sizeDelta        = new Vector2(1500f, 280f);
        prt.anchoredPosition = new Vector2(0f, 90f);

        _dialogueText = CreateText("Text", _panel.transform, 46f,
            new Color(0.35f, 0.22f, 0.10f), TextAlignmentOptions.MidlineLeft);
        RectTransform trt = _dialogueText.rectTransform;
        StretchFull(trt);
        trt.offsetMin = new Vector2(60f, 55f);   // chừa đáy cho hint
        trt.offsetMax = new Vector2(-60f, -35f);

        _hintText = CreateText("Hint", _panel.transform, 30f,
            new Color(0.69f, 0.53f, 0.31f), TextAlignmentOptions.MidlineRight);
        RectTransform hrt = _hintText.rectTransform;
        hrt.anchorMin        = new Vector2(1f, 0f);
        hrt.anchorMax        = new Vector2(1f, 0f);
        hrt.pivot            = new Vector2(1f, 0f);
        hrt.sizeDelta        = new Vector2(600f, 44f);
        hrt.anchoredPosition = new Vector2(-40f, 12f);
        _hintText.gameObject.SetActive(false);

        _panel.gameObject.SetActive(false);
    }

    private IEnumerator ShowPanelRoutine()
    {
        _panel.gameObject.SetActive(true);
        Transform t = _panel.transform;

        // Bounce-in 2 nhịp: 0 → 1.1 (easeOut) rồi 1.1 → 1 — cùng nhịp TutorialGuideBoardUI
        float d1 = 0.3f, elapsed = 0f;
        Vector3 overshoot = new Vector3(1.1f, 1.1f, 1f);
        while (elapsed < d1)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / d1);
            t.localScale = Vector3.Lerp(Vector3.zero, overshoot, p * (2f - p));
            yield return null;
        }

        float d2 = 0.2f; elapsed = 0f;
        while (elapsed < d2)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / d2);
            float ease = p < 0.5f ? 2f * p * p : -1f + (4f - 2f * p) * p;
            t.localScale = Vector3.Lerp(overshoot, Vector3.one, ease);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator HidePanelRoutine()
    {
        Transform t = _panel.transform;
        float d = 0.25f, elapsed = 0f;
        Vector3 from = t.localScale;
        while (elapsed < d)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(from, Vector3.zero, Mathf.Clamp01(elapsed / d));
            yield return null;
        }
        _panel.gameObject.SetActive(false);
        t.localScale = Vector3.one;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;            // sprite null → hình chữ nhật phẳng (placeholder)
        img.raycastTarget = true;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size,
                                              Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize      = size;
        txt.color         = color;
        txt.alignment     = align;
        txt.raycastTarget = false;
        return txt;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
