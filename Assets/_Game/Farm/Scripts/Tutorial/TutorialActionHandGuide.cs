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
