using System.Collections;
using UnityEngine;

/// <summary>
/// TẤM GỖ (gangplank) nối tàu ↔ bờ của MỘT bến (GDD BOAT-002 §3.7).
///
/// Object con "Gangplank" của Dock_0X (tool TouristVisitorSetupTool sinh + gán
/// sprite placeholder). Nghe event của BoatDockManager (contract Dev A):
///   • OnBoatDocked(dock)    → bắc tấm gỗ (extend)
///   • OnBoatDeparting(dock) → rút tấm gỗ (retract)
/// Vào scene giữa chừng: đọc thẳng IsDocked(dock) để đặt đúng trạng thái ngay
/// (không chờ event — tàu có thể đã đậu sẵn từ trước khi mình Start).
///
/// [QA M-1] IsDocked chỉ trả đúng SAU KHI BoatDockManager.IsReady = true, mà cờ đó bật
/// trong Start của Dev A — thứ tự Start giữa 2 MonoBehaviour là KHÔNG XÁC ĐỊNH. Bản cũ
/// đọc một lần trong Start rồi thôi ⇒ chạy trước Dev A là tấm gỗ biến mất VĨNH VIỄN
/// (lỗi nhấp nháy theo thứ tự script). Nay: chờ IsReady rồi mới chốt trạng thái, và
/// Update TỰ RE-SYNC mỗi khi phát hiện lệch với IsDocked — không bao giờ kẹt sai pha.
///
/// Animation 2 chế độ:
///   • CÓ art frame (mảng <see cref="frames"/> ≥ 2, art request 4 frame): play
///     tuần tự frame khi bắc, đảo ngược khi rút.
///   • CHƯA có art: placeholder scale-X 0→1 trong <see cref="extendSeconds"/>
///     (0.4s) — logic vẫn chạy đủ, gắn art sau không sửa code.
/// </summary>
public class GangplankController : MonoBehaviour
{
    [Header("Bến")]
    [Tooltip("Index bến 0-2. Để -1 sẽ tự suy từ tên 'Dock_XX' của node cha " +
             "(cùng cách ResolveDockIndex của TouristBoatController).")]
    [SerializeField] private int dockIndex = -1;

    [Header("Visual")]
    [Tooltip("SpriteRenderer tấm gỗ. Bỏ trống sẽ tự tìm trên chính object này.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Mảng frame animation 'bắc tấm gỗ' (art request 4 frame). " +
             "Để trống → placeholder scale-X 0→1.")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("Giây chạy hết animation bắc/rút (placeholder lẫn frame).")]
    [SerializeField] private float extendSeconds = 0.4f;

    [Header("Sorting")]
    // Tấm gỗ nằm DƯỚI chân khách (khách đi đè lên) ⇒ layer thấp hơn khách một bậc:
    // gangplank = "Objects", khách = "ObjectsFront". Để trống = tự giải + cảnh báo nếu
    // layer không tồn tại (xem TouristSortingLayers — bug "CongTrinh" của bản đầu).
    [Tooltip("ĐỂ TRỐNG = tự chọn 'Objects' (dưới khách, trên mặt nước).")]
    [SerializeField] private string sortingLayerName = "";

    [Tooltip("Order trong layer. Đặt dưới order của khách để khách đi đè lên tấm gỗ.")]
    [SerializeField] private int sortingOrder = 900;

    // ─── Runtime ────────────────────────────────────────────────────────

    private int       _dockIndex = -1;
    private bool      _extended;          // trạng thái hiện tại (đã bắc xong / đang bắc)
    private bool      _subscribed;
    private bool      _daChotSauReady;    // QA M-1: đã chốt trạng thái sau khi manager IsReady chưa
    // QA m-10: giữ ĐÚNG instance đã subscribe để OnDestroy gỡ đúng cái đó
    // (reload scene / có 2 BoatSystem thì Instance hiện tại có thể là manager khác).
    private BoatDockManager _mgr;
    private Vector3   _fullScale;         // scale gốc = trạng thái bắc hết
    private Coroutine _animRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        _fullScale = transform.localScale;

        // Ép lại sorting layer lúc chạy: object dựng bằng tool đời cũ còn lưu layer
        // Default trong scene, để nguyên là tấm gỗ chìm dưới nền/decor.
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName =
                TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Gangplank);
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private void Start()
    {
        _dockIndex = ResolveDockIndex();
        if (_dockIndex < 0)
            Debug.LogWarning($"[TouristVisitor] Gangplank '{name}': không suy được dockIndex " +
                             "(đặt trong Inspector hoặc đặt dưới node 'Dock_XX'). Tấm gỗ sẽ luôn ẩn.");

        // Manager có thể Start SAU mình (thứ tự script không đảm bảo) — subscribe được
        // ngay (Instance có từ Awake) nhưng CHƯA chốt trạng thái: IsDocked cần IsReady.
        // Việc chốt để Update lo (QA M-1).
        TrySubscribe();
    }

    /// <summary>
    /// [QA M-1] Đồng bộ định kỳ thay vì chỉ subscribe một lần:
    ///   • chưa subscribe được → thử lại;
    ///   • manager vừa IsReady → CHỐT trạng thái lần đầu (không animation);
    ///   • sau đó, phát hiện lệch giữa trạng thái tấm gỗ và IsDocked → tự sửa (có animation).
    /// Nhờ vậy load save đang Docked luôn thấy tấm gỗ đã bắc, dù Dev A cố ý không
    /// bắn lại OnBoatDocked cho chuyến cũ.
    /// </summary>
    private void Update()
    {
        if (!_subscribed) { TrySubscribe(); return; }

        var mgr = _mgr != null ? _mgr : BoatDockManager.Instance;
        if (mgr == null || !mgr.IsReady || _dockIndex < 0) return;

        bool dangDau = mgr.IsDocked(_dockIndex);

        if (!_daChotSauReady)
        {
            _daChotSauReady = true;
            ApplyStateInstant(dangDau);
            return;
        }

        // Lệch trạng thái (lỡ event vì bị SetActive(false), lịch bị reset…) → sửa êm.
        if (_extended != dangDau) SetExtended(dangDau);
    }

    private void OnDestroy()
    {
        // QA m-10: gỡ khỏi ĐÚNG instance đã subscribe, không phải Instance hiện tại.
        if (_subscribed && _mgr != null)
        {
            _mgr.OnBoatDocked    -= HandleBoatDocked;
            _mgr.OnBoatDeparting -= HandleBoatDeparting;
        }
    }

    // ─── Event handlers ─────────────────────────────────────────────────

    private void TrySubscribe()
    {
        var mgr = BoatDockManager.Instance;
        if (mgr == null) return;

        _mgr = mgr;                       // QA m-10: nhớ đúng instance đã gắn
        mgr.OnBoatDocked    += HandleBoatDocked;
        mgr.OnBoatDeparting += HandleBoatDeparting;
        _subscribed = true;
        // KHÔNG chốt trạng thái ở đây (QA M-1): IsDocked chưa đáng tin tới khi IsReady.
    }

    private void HandleBoatDocked(int dock)
    {
        if (dock != _dockIndex) return;
        SetExtended(true);
    }

    private void HandleBoatDeparting(int dock)
    {
        if (dock != _dockIndex) return;
        SetExtended(false);
    }

    // ─── Animation ──────────────────────────────────────────────────────

    /// <summary>Bắc/rút có animation (event runtime).</summary>
    private void SetExtended(bool extended)
    {
        if (_extended == extended) return;
        _extended = extended;

        if (_animRoutine != null) StopCoroutine(_animRoutine);
        if (!isActiveAndEnabled) { ApplyStateInstant(extended); return; }
        _animRoutine = StartCoroutine(AnimRoutine(extended));
    }

    /// <summary>Đặt trạng thái NGAY không animation (lúc load/subscribe muộn — GDD §5.1).</summary>
    private void ApplyStateInstant(bool extended)
    {
        _extended = extended;
        if (_animRoutine != null) { StopCoroutine(_animRoutine); _animRoutine = null; }

        if (HasFrames())
        {
            SetRendererShown(extended);
            if (extended && spriteRenderer != null)
                spriteRenderer.sprite = frames[frames.Length - 1];
            transform.localScale = _fullScale;
        }
        else
        {
            SetRendererShown(extended);
            transform.localScale = extended ? _fullScale : ScaleX(0f);
        }
    }

    private IEnumerator AnimRoutine(bool extend)
    {
        float duration = Mathf.Max(0.05f, extendSeconds);
        SetRendererShown(true);

        if (HasFrames())
        {
            // Art frame: play tuần tự (bắc) / đảo ngược (rút).
            int n = frames.Length;
            float perFrame = duration / n;
            for (int step = 0; step < n; step++)
            {
                int i = extend ? step : (n - 1 - step);
                if (spriteRenderer != null && frames[i] != null)
                    spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(perFrame);
            }
        }
        else
        {
            // Placeholder: scale-X 0→1 (bắc) / 1→0 (rút) với ease mượt.
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                transform.localScale = ScaleX(extend ? k : 1f - k);
                yield return null;
            }
            transform.localScale = extend ? _fullScale : ScaleX(0f);
        }

        if (!extend) SetRendererShown(false);
        _animRoutine = null;
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private bool HasFrames()
    {
        return frames != null && frames.Length >= 2 && frames[0] != null;
    }

    private Vector3 ScaleX(float k)
    {
        return new Vector3(_fullScale.x * k, _fullScale.y, _fullScale.z);
    }

    private void SetRendererShown(bool shown)
    {
        if (spriteRenderer != null && spriteRenderer.enabled != shown)
            spriteRenderer.enabled = shown;
    }

    private bool IsBoatDocked()
    {
        var mgr = _mgr != null ? _mgr : BoatDockManager.Instance;
        return mgr != null && mgr.IsReady && _dockIndex >= 0 && mgr.IsDocked(_dockIndex);
    }

    /// <summary>Suy dockIndex từ tên node cha 'Dock_XX' (pattern TouristBoatController).</summary>
    private int ResolveDockIndex()
    {
        if (dockIndex >= 0) return dockIndex;

        Transform p = transform.parent;
        while (p != null)
        {
            string n = p.name;
            if (n.StartsWith("Dock_"))
            {
                int number;
                if (int.TryParse(n.Substring(5), out number) && number >= 1)
                    return number - 1;
            }
            p = p.parent;
        }
        return -1;
    }
}
