using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialActionHandGuide : MonoBehaviour
{
    [SerializeField] private RectTransform _hand;
    [SerializeField] private Vector2 _offset = new Vector2(42f, -34f);
    [SerializeField] private float _pulseSpeed = 4f;
    [SerializeField] private float _pulseAmount = 0.12f;

    private Coroutine _routine;
    private Vector3 _baseScale = Vector3.one;

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

    /// <summary>Tay quét qua các ô CÒN VIỆC theo thứ tự (bỏ qua ô user đã làm xong).
    /// needReady=false → chỉ ô trống (để trồng); needReady=true → chỉ ô chín (để thu hoạch).</summary>
    public void GuideSweepPlots(string[] targetIds, bool needReady = false)
        => StartGuide(SweepRoutine(targetIds, needReady));

    public void StopGuide()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        if (_hand == null) return;
        _hand.localScale = _baseScale;
        _hand.gameObject.SetActive(false);
    }

    private void StartGuide(IEnumerator routine)
    {
        StopGuide();
        if (_hand != null) _routine = StartCoroutine(routine);
    }

    private IEnumerator SpeedUpRoutine(string plotTargetId)
    {
        while (true)
        {
            RectTransform target = FindOpenSpeedButton();
            if (target == null) target = TutorialManager.GetTargetRect(plotTargetId);
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
            Vector3 from = sickle.position;
            Vector3 to = plot.position;
            float elapsed = 0f;
            const float duration = 0.8f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _hand.position = Vector3.Lerp(from, to, t);
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
                float elapsed = 0f;
                const float dur = 0.45f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
                    _hand.position          = Vector3.Lerp(curRT.position, nxtRT.position, t);
                    _hand.anchoredPosition += _offset;
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

    private void PointAt(RectTransform target)
    {
        bool show = target != null && target.gameObject.activeInHierarchy;
        _hand.gameObject.SetActive(show);
        if (!show) return;
        _hand.position = target.position;
        _hand.anchoredPosition += _offset;
    }

    private void Pulse()
    {
        if (!_hand.gameObject.activeSelf) return;
        float scale = 1f + Mathf.Sin(Time.unscaledTime * _pulseSpeed) * _pulseAmount;
        _hand.localScale = _baseScale * scale;
    }

    private void OnDisable() => StopGuide();
}
