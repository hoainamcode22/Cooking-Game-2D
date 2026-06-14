using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialGuideBoardUI : MonoBehaviour
{
    [Serializable]
    public class PopupPage
    {
        public string stepName;
        public GameObject root;
        public RectTransform animatedHand;
        public RectTransform handFrom;
        public RectTransform handTo;
        public float travelDuration = 0.65f;
        public float pauseDuration = 0.45f;
    }

    [Header("Root")]
    [SerializeField] private GameObject rootPanel;

    [Header("Legacy Image Slots")]
    [SerializeField] private Image step1Icon;
    [SerializeField] private Image step2Icon;
    [SerializeField] private Image step3Icon;
    [SerializeField] private Image step4Icon;

    [Header("Button")]
    [SerializeField] private Button confirmButton;

    [Header("Four Popup Pages")]
    [SerializeField] private PopupPage[] popupPages = Array.Empty<PopupPage>();

    private Coroutine _handRoutine;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    public void Show() => ShowForStep(string.Empty);

    public void ShowForStep(string stepName)
    {
        var target = rootPanel != null ? rootPanel : gameObject;
        target.SetActive(true);

        PopupPage selected = null;
        foreach (var page in popupPages)
        {
            if (page?.root == null) continue;
            bool active = selected == null
                && (string.IsNullOrEmpty(page.stepName) || page.stepName == stepName);
            page.root.SetActive(active);
            if (active) selected = page;
        }

        StopPageAnimation();
        if (selected?.animatedHand != null && selected.handFrom != null)
            _handRoutine = StartCoroutine(AnimatePageHand(selected));

        transform.SetAsLastSibling();
        Debug.Log($"[TutorialGuideBoardUI] ShowForStep('{stepName}')");
    }

    public void Hide()
    {
        StopPageAnimation();
        var target = rootPanel != null ? rootPanel : gameObject;
        target.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        TutorialManager.Instance?.ConfirmGuidePopup();
    }

    private IEnumerator AnimatePageHand(PopupPage page)
    {
        RectTransform hand = page.animatedHand;
        hand.gameObject.SetActive(true);
        Vector3 baseScale = hand.localScale == Vector3.zero ? Vector3.one : hand.localScale;
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.1f, page.pauseDuration));

        while (true)
        {
            RectTransform destination = page.handTo != null ? page.handTo : page.handFrom;
            Vector3 start = page.handFrom.position;
            Vector3 end = destination.position;
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, page.travelDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                hand.position = Vector3.Lerp(start, end, t);
                hand.localScale = baseScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.12f);
                yield return null;
            }

            hand.position = end;
            hand.localScale = baseScale;
            yield return wait;
        }
    }

    private void StopPageAnimation()
    {
        if (_handRoutine != null) StopCoroutine(_handRoutine);
        _handRoutine = null;
    }

    private void OnDisable() => StopPageAnimation();
}
