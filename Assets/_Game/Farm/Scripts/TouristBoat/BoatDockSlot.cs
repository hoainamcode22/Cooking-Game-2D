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
///  - Tap vào khóa (Collider2D + OnMouseDown — cùng pattern TrainWagonSlot):
///      đủ điều kiện → TryUnlockDock + hiệu ứng scale punch;
///      thiếu điều kiện → floating text hiện lý do từ CanUnlockDock.
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

    private BoatDockManager _manager;      // giữ ref để unsubscribe an toàn lúc teardown
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

    private void OnMouseDown()
    {
        var mgr = _manager != null ? _manager : BoatDockManager.Instance;
        if (mgr == null) return;
        if (mgr.IsDockUnlocked(dockIndex)) return; // đã mở — collider lẽ ra đã tắt, guard cho chắc

        // m-1 (QA): bến 1 mở MIỄN PHÍ qua hội thoại intro — chặn tap trước/trong intro
        // để không ai "mua trộm" bến 1 (CanUnlockDock(0) trả true từ L10), giữ trọn
        // khoảnh khắc chuyến tàu đầu tiên do TouristBoatUnlockFlow đạo diễn.
        if (dockIndex == 0 && !mgr.IsIntroDone) return;

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

    // =========================================================================
    //  Trạng thái khóa
    // =========================================================================

    private void HandleDockUnlocked(int unlockedIndex)
    {
        if (unlockedIndex != dockIndex) return;

        if (_unlockFxRoutine != null) StopCoroutine(_unlockFxRoutine);
        _unlockFxRoutine = StartCoroutine(UnlockFxRoutine());
    }

    /// <summary>Cập nhật UI khóa theo trạng thái hiện tại của manager (gọi lúc init/re-init scene).</summary>
    public void RefreshLockUI()
    {
        var mgr = _manager != null ? _manager : BoatDockManager.Instance;
        if (mgr == null) return;

        bool unlocked = mgr.IsDockUnlocked(dockIndex);

        if (lockRoot != null)    lockRoot.SetActive(!unlocked);
        if (tapCollider != null) tapCollider.enabled = !unlocked;
        if (!unlocked && teaserText != null)
            teaserText.text = BuildTeaserText(mgr.Config);
    }

    /// <summary>
    /// Teaser mở khóa — đọc toàn bộ số từ Config, không hardcode:
    ///   dock 0: "Mở ở Lv10" (miễn phí, mở qua intro)
    ///   dock 1: "Mở ở Lv12 · 2.000 vàng"
    ///   dock 2: "Mở ở Lv14 · 25 Kim Cương"
    /// KHÔNG dùng emoji trong text runtime (quyết định lead sau QA — font TMP mặc định
    /// thiếu glyph emoji). Sếp có thể thêm emoji lại nếu font TMP của dự án có glyph.
    /// </summary>
    private string BuildTeaserText(TouristBoatConfig config)
    {
        if (config == null) return string.Empty;

        switch (dockIndex)
        {
            case 0:  return $"Mở ở Lv{config.unlockLevel}";
            // Xuống dòng thay vì một dòng dài: chữ to hơn mà vẫn nằm gọn trong bảng.
            case 1:  return $"Mở ở Lv{config.dock2Level}\n{FormatVN(config.dock2GoldCost)} vàng";
            case 2:  return $"Mở ở Lv{config.dock3Level}\n{config.dock3GemCost} Kim Cương";
            default: return string.Empty;
        }
    }

    /// <summary>Định dạng số kiểu Việt Nam: 2000 → "2.000".</summary>
    private static string FormatVN(int amount)
        => amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

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
            _floatingText.overflowMode     = TextOverflowModes.Overflow;
            _floatingText.color            = new Color(1f, 0.95f, 0.75f); // trắng ấm, thân thiện
            var mr = _floatingText.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 200;
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
