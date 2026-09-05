using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialActionHandGuide : MonoBehaviour
{
    [SerializeField] private RectTransform _hand;
    [Tooltip("Vị trí ĐẦU NGÓN TAY trên ảnh hand, chuẩn hoá 0..1 (x: trái→phải, y: dưới→trên). " +
             "Ảnh tutorial_hand chỉ XUỐNG → đầu ngón ở đáy-hơi trái ≈ (0.36, 0.1).")]
    [SerializeField] private Vector2 _fingertipNormalized = new Vector2(0.36f, 0.1f);
    [Tooltip("Tinh chỉnh thêm vài px nếu vẫn lệch (thường để 0,0).")]
    [SerializeField] private Vector2 _nudge = Vector2.zero;
    [SerializeField] private float _pulseSpeed = 4f;
    [SerializeField] private float _pulseAmount = 0.12f;

    private Coroutine _routine;
    private Vector3 _baseScale = Vector3.one;
    private Vector3 _lastFingertipWorld;
    private bool _hasLastFingertipWorld;

    // [V6] Cờ chống dội: StopGuide được gọi TỪ BÊN TRONG StartGuide (dọn routine cũ) thì
    // không được nhả quyền / không được bảo TutorialManager bật lại tay tĩnh.
    private bool _dangDoiGuide;

    /// <summary>[V6] Guide có đang chạy không (chỉ-đọc, cho báo cáo F10).</summary>
    public bool DangChay => _routine != null;

    /// <summary>[V6] Object tay hành động đang dùng (chỉ-đọc, cho báo cáo F10).</summary>
    public RectTransform TayHanhDongRT => _hand;

    public void Configure(RectTransform hand)
    {
        _hand = hand;
        if (_hand == null) return;
        _baseScale = _hand.localScale == Vector3.zero ? Vector3.one : _hand.localScale;
        foreach (var graphic in _hand.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        _hand.gameObject.SetActive(false);
    }

    public void GuideSpeedUp(string plotTargetId) => StartGuide(SpeedUpRoutine(plotTargetId));
    public void GuideHarvest(string plotTargetId) => StartGuide(HarvestRoutine(plotTargetId));

    /// <summary>Tay pulse liên tục chỉ vào 1 target, bám theo target mỗi frame.</summary>
    public void GuidePoint(string targetId) => StartGuide(PointRoutine(targetId));

    /// <summary>Chỉ vào target ĐANG HIỆN đầu tiên trong danh sách (vd ["btn_store","btn_home"]):
    /// tay tự nhảy từ Home sang Store khi menu mở ra.</summary>
    public void GuidePointFirstActive(string[] targetIds) => StartGuide(PointFirstActiveRoutine(targetIds));

    /// <summary>Mua shop: tay chỉ nút ＋ tới khi số lượng item đạt requiredQty, rồi NHẢY sang nút Mua.
    /// itemId = target của ShopItemUI (để đọc số lượng đang chọn).</summary>
    public void GuideShopBuy(string plusId, string buyId, string itemId, int requiredQty, UnmaskRaycastFilter dim = null)
        => StartGuide(ShopBuyRoutine(plusId, buyId, itemId, requiredQty, dim));

    /// <summary>Tay quét qua các ô CÒN VIỆC theo thứ tự (bỏ qua ô user đã làm xong).
    /// needReady=false → chỉ ô trống (để trồng); needReady=true → chỉ ô chín (để thu hoạch).</summary>
    public void GuideSweepPlots(string[] targetIds, bool needReady = false)
        => StartGuide(SweepRoutine(targetIds, needReady));

    public void StopGuide()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        _hasLastFingertipWorld = false;
        if (_hand != null)
        {
            _hand.localScale = _baseScale;
            _hand.gameObject.SetActive(false);
        }

        if (_dangDoiGuide) return;   // [V6] đang đổi sang guide khác → chưa nhả quyền

        // [V6] Nhả quyền rồi báo TutorialManager xem bước hiện tại có còn cần tay tĩnh không.
        // Thiếu bước này thì sau khi tắt guide sẽ còn 0 bàn tay — người chơi mất chỉ dẫn.
        TutorialHandBus.Nha(LoaiTay.TayHanhDong);
        TutorialManager.Instance?.CapNhatLaiTayTinh();
    }

    private void StartGuide(IEnumerator routine)
    {
        // [V6] Dọn routine cũ mà KHÔNG nhả quyền (sẽ giành lại ngay bên dưới).
        _dangDoiGuide = true;
        try { StopGuide(); }
        finally { _dangDoiGuide = false; }

        // Không có object tay thì đừng giành quyền — giành mà không hiện được gì
        // sẽ chặn luôn tay tĩnh ⇒ bước đó KHÔNG CÒN BÀN TAY NÀO.
        if (_hand == null)
        {
            // [V6] StopGuide() phía trên đã bị cờ _dangDoiGuide chặn không cho nhả quyền.
            // Thoát ở đây mà không nhả thì trọng tài vẫn ghi TayHanhDong đang giữ ⇒ tay tĩnh
            // bị câm vĩnh viễn ⇒ 0 bàn tay. Nhả quyền rồi trả lượt lại cho tay tĩnh.
            TutorialHandBus.Nha(LoaiTay.TayHanhDong);
            TutorialManager.Instance?.CapNhatLaiTayTinh();
            return;
        }

        // [V6] MỘT TAY MỘT LÚC: giành quyền, ẩn tay tĩnh, tắt tay kéo.
        TutorialHandBus.Nhan(LoaiTay.TayHanhDong);
        var mgr = TutorialManager.Instance;
        if (mgr != null)
        {
            mgr.AnTayTinh();
            mgr.DungTayKeo();
        }

        _routine = StartCoroutine(routine);
    }

    private IEnumerator SpeedUpRoutine(string plotTargetId)
    {
        while (true)
        {
            RectTransform speedBtn = FindOpenSpeedButton();
            RectTransform target = speedBtn != null ? speedBtn : TutorialManager.GetTargetRect(plotTargetId);

            var dim = Object.FindAnyObjectByType<UnmaskRaycastFilter>();
            if (dim != null && dim.gameObject.activeInHierarchy)
            {
                if (speedBtn != null)
                    dim.SetTarget(speedBtn, false, 24f);
                else if (target != null)
                    dim.SetTarget(target, false, 80f);
            }

            PointAt(target);
            Pulse();
            yield return null;
        }
    }

    private IEnumerator HarvestRoutine(string plotTargetId)
    {
        while (true)
        {
            RectTransform plot = TutorialManager.GetTargetRect(plotTargetId);
            RectTransform sickle = null;
            if (FarmUIManager.Instance != null)
            {
                var tray = FarmUIManager.Instance.SickleTrayRect;
                if (tray != null && tray.gameObject.activeInHierarchy) sickle = tray;
            }

            if (sickle == null || plot == null)
            {
                PointAt(plot);
                Pulse();
                yield return null;
                continue;
            }

            _hand.gameObject.SetActive(true);
            Vector3 from = TargetCenter(sickle);
            Vector3 to = TargetCenter(plot);
            float elapsed = 0f;
            const float duration = 0.8f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                PlaceHandFingertipAt(Vector3.Lerp(from, to, t));
                Pulse();
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.45f);
        }
    }

    // Tay pulse cố định trên 1 target (bám theo target mỗi frame — chịu được camera pan).
    private IEnumerator PointRoutine(string targetId)
    {
        while (true)
        {
            PointAt(TutorialManager.GetTargetRect(targetId));
            Pulse();
            yield return null;
        }
    }

    // Mua shop: ＋ tới khi đủ số lượng → Mua. Đọc số lượng mỗi frame nên tay tự nhảy đúng lúc.
    private IEnumerator ShopBuyRoutine(string plusId, string buyId, string itemId, int requiredQty, UnmaskRaycastFilter dim)
    {
        while (true)
        {
            int qty = ReadShopItemQuantity(itemId);
            string id = qty >= requiredQty ? buyId : plusId;
            RectTransform target = TutorialManager.GetTargetRect(id);
            if (target == null) target = TutorialManager.GetTargetRect(itemId); // fallback: chỉ vào item
            if (dim != null)
            {
                dim.gameObject.SetActive(true);
                if (target != null) dim.SetTarget(target, false, 18f);
                else dim.ClearHole();
            }
            PointAt(target);
            Pulse();
            yield return null;
        }
    }

    private static int ReadShopItemQuantity(string itemId)
    {
        RectTransform rt = TutorialManager.GetTargetRect(itemId);
        if (rt == null) return 0;
        var item = rt.GetComponent<ShopItemUI>();
        return item != null ? item.CurrentQuantity : 0;
    }

    // Chỉ vào target đang hiện đầu tiên trong list (Home→Store: tay tự nhảy khi menu mở).
    private IEnumerator PointFirstActiveRoutine(string[] ids)
    {
        while (true)
        {
            RectTransform target = null;
            if (ids != null)
                foreach (var id in ids)
                {
                    var rt = TutorialManager.GetTargetRect(id);
                    if (rt != null && rt.gameObject.activeInHierarchy) { target = rt; break; }
                }
            PointAt(target);
            Pulse();
            yield return null;
        }
    }

    // Tay quét lần lượt qua các ô: tap ô hiện tại → lướt sang ô kế → lặp vòng.
    // Đọc lại RectTransform mỗi frame nên luôn đúng vị trí dù camera đang lia.
    private IEnumerator SweepRoutine(string[] ids, bool needReady)
    {
        if (ids == null || ids.Length == 0) yield break;

        int i = 0;
        while (true)
        {
            // Tìm ô CÒN VIỆC tiếp theo (bỏ qua ô user đã trồng/đã thu hoạch).
            int cur = NextPending(ids, i, needReady);
            if (cur < 0)
            {
                // Không còn ô nào cần làm → ẩn tay, chờ step tự chuyển (đã làm đủ).
                if (_hand != null) _hand.gameObject.SetActive(false);
                yield return null;
                continue;
            }

            RectTransform curRT = TutorialManager.GetTargetRect(ids[cur]);
            if (curRT == null) { i = (cur + 1) % ids.Length; yield return null; continue; }

            // 1. Tap nhấn trên ô đang cần làm — nếu user vừa làm xong thì nhảy ngay sang ô khác.
            float hold = 0f;
            const float holdDur = 0.45f;
            while (hold < holdDur)
            {
                hold += Time.unscaledDeltaTime;
                if (!TutorialRuntimeTargetResolver.IsPlotPending(ids[cur], needReady)) break;
                PointAt(curRT);
                Pulse();
                yield return null;
            }

            // 2. Lướt sang ô CÒN VIỆC kế tiếp.
            int nxt = NextPending(ids, cur + 1, needReady);
            RectTransform nxtRT = nxt >= 0 ? TutorialManager.GetTargetRect(ids[nxt]) : null;
            if (nxtRT != null && nxt != cur)
            {
                _hand.gameObject.SetActive(true);
                Vector3 fromC = TargetCenter(curRT);
                Vector3 toC   = TargetCenter(nxtRT);
                float elapsed = 0f;
                const float dur = 0.45f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
                    PlaceHandFingertipAt(Vector3.Lerp(fromC, toC, t));
                    Pulse();
                    yield return null;
                }
            }

            i = (cur + 1) % ids.Length;
        }
    }

    // Index ô tiếp theo (từ 'from', vòng tròn) còn việc + proxy đang hiển thị. -1 nếu hết.
    private static int NextPending(string[] ids, int from, bool needReady)
    {
        int start = ((from % ids.Length) + ids.Length) % ids.Length;
        for (int k = 0; k < ids.Length; k++)
        {
            int j = (start + k) % ids.Length;
            if (TutorialRuntimeTargetResolver.IsPlotPending(ids[j], needReady)
                && TutorialManager.GetTargetRect(ids[j]) != null)
                return j;
        }
        return -1;
    }

    private static RectTransform FindOpenSpeedButton()
    {
        var popups = Object.FindObjectsByType<CropProcessPopupUI>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var popup in popups)
            if (popup != null && popup.IsOpen && popup.SpeedUpButtonRect != null)
                return popup.SpeedUpButtonRect;
        return null;
    }

    private static readonly Vector3[] _cornerBuf = new Vector3[4];

    private void PointAt(RectTransform target)
    {
        bool show = target != null && target.gameObject.activeInHierarchy;
        _hand.gameObject.SetActive(show);
        if (!show) return;
        PlaceHandFingertipAt(TargetCenter(target));
    }

    /// <summary>Tâm hình học world-space của 1 RectTransform (đã gồm width/height) —
    /// tránh lệch do pivot nút không nằm giữa.</summary>
    private static Vector3 TargetCenter(RectTransform rt)
    {
        rt.GetWorldCorners(_cornerBuf);
        return (_cornerBuf[0] + _cornerBuf[2]) * 0.5f;
    }

    /// <summary>Đặt hand sao cho ĐẦU NGÓN TAY (tính theo width/height của ảnh hand) trùng worldCenter.</summary>
    private void PlaceHandFingertipAt(Vector3 worldCenter)
    {
        _lastFingertipWorld = worldCenter;
        _hasLastFingertipWorld = true;

        _hand.position = worldCenter;
        Rect r = _hand.rect;
        Vector2 fingerFromPivot = new Vector2(
            (_fingertipNormalized.x - _hand.pivot.x) * r.width,
            (_fingertipNormalized.y - _hand.pivot.y) * r.height);
        Vector2 scaledFingerFromPivot = new Vector2(
            fingerFromPivot.x * _hand.localScale.x,
            fingerFromPivot.y * _hand.localScale.y);
        _hand.anchoredPosition -= scaledFingerFromPivot;   // kéo đầu ngón tay về đúng tâm nút
        _hand.anchoredPosition += _nudge;            // tinh chỉnh nhỏ (Inspector)
    }

    private bool _hasRippledThisCycle;

    private void Pulse()
    {
        if (!_hand.gameObject.activeSelf) return;
        
        // Ép và Giãn (Squash & Stretch)
        float time = Time.unscaledTime * _pulseSpeed;
        float sin = Mathf.Sin(time);
        
        // Khi tay ấn xuống (sin > 0), chiều ngang phình ra (Squash), chiều dọc xẹp lại
        float squashX = 1f + sin * _pulseAmount;
        float squashY = 1f - sin * _pulseAmount * 0.5f; // Xẹp ít hơn phình
        
        _hand.localScale = new Vector3(_baseScale.x * squashX, _baseScale.y * squashY, _baseScale.z);
        
        // Sinh vòng sóng nước (Ripple) khi tay nhấn mạnh nhất
        if (sin > 0.95f && !_hasRippledThisCycle)
        {
            _hasRippledThisCycle = true;
            SpawnRipple();
        }
        else if (sin < 0f)
        {
            _hasRippledThisCycle = false;
        }

        if (_hasLastFingertipWorld)
            PlaceHandFingertipAt(_lastFingertipWorld);
    }

    private void SpawnRipple()
    {
        if (!_hasLastFingertipWorld) return;
        
        GameObject ripple = new GameObject("HandRipple");
        ripple.transform.SetParent(_hand.parent, true);
        ripple.transform.position = _lastFingertipWorld;
        ripple.transform.localScale = Vector3.zero;
        
        Image img = ripple.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.5f);
        img.raycastTarget = false;
        
        // Tao animation Ripple nho = Coroutine
        StartCoroutine(RippleAnim(ripple.transform, img));
    }

    private IEnumerator RippleAnim(Transform t, Image img)
    {
        float elapsed = 0f;
        float dur = 0.4f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = elapsed / dur;
            
            // To ra va mo dan
            t.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.5f, time);
            img.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.6f, 0f, time));
            
            yield return null;
        }
        Destroy(t.gameObject);
    }

    private void OnDisable() => StopGuide();
}
