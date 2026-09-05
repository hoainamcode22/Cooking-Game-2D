using System;
using System.Collections;
using System.Collections.Generic;
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
        public TextMeshProUGUI tutorialText;
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
    [SerializeField] private ParticleSystem confirmBurstParticles;

    [Header("Stepper Indicator")]
    [SerializeField] private Image[] stepDots = Array.Empty<Image>();
    [SerializeField] private Sprite dotOnSprite;
    [SerializeField] private Sprite dotOffSprite;

    [Header("Four Popup Pages")]
    [SerializeField] private PopupPage[] popupPages = Array.Empty<PopupPage>();

    private Coroutine _handRoutine;
    private Coroutine _typewriterRoutine;
    private Coroutine _transitionRoutine;
    private Coroutine _idleFloatRoutine;
    private PopupPage _currentPage;
    private bool _isClosing = false;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    public void Show() => ShowForStep(string.Empty);

    public void ShowForStep(string stepName)
    {
        var target = rootPanel != null ? rootPanel : gameObject;
        _isClosing = false;

        if (!target.activeSelf)
        {
            target.SetActive(true);
            StartCoroutine(BounceInRoutine(target.transform));
        }

        PopupPage nextSelected = null;
        foreach (var page in popupPages)
        {
            if (page?.root == null) continue;
            if (nextSelected == null && (string.IsNullOrEmpty(page.stepName) || page.stepName == stepName))
            {
                nextSelected = page;
            }
        }

        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        foreach (var page in popupPages)
        {
            if (page?.root == null) continue;
            if (page != nextSelected)
            {
                page.root.SetActive(false);
                var cg = page.root.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }

        if (_currentPage != nextSelected && _currentPage != null && nextSelected != null)
        {
            _currentPage.root.SetActive(true);
            _transitionRoutine = StartCoroutine(TransitionPagesRoutine(_currentPage, nextSelected));
            _currentPage = nextSelected;
        }
        else
        {
            _currentPage = nextSelected;
            if (nextSelected != null)
            {
                nextSelected.root.SetActive(true);
                var cg = nextSelected.root.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
                nextSelected.root.transform.localPosition = Vector3.zero;
            }
        }

        StopPageAnimation();
        
        if (nextSelected != null)
        {
            UpdateStepperDots(nextSelected);

            if (nextSelected.animatedHand != null && nextSelected.handFrom != null)
                _handRoutine = StartCoroutine(AnimatePageHand(nextSelected));
                
            if (nextSelected.tutorialText != null)
                _typewriterRoutine = StartCoroutine(TypewriterEffect(nextSelected.tutorialText));

            // Start 3D Floating animation for icons
            _idleFloatRoutine = StartCoroutine(AnimateIconsFloat(nextSelected));
        }

        transform.SetAsLastSibling();
    }

    private void UpdateStepperDots(PopupPage activePage)
    {
        if (stepDots == null || stepDots.Length == 0) return;
        int activeIndex = -1;
        for (int i = 0; i < popupPages.Length; i++)
        {
            if (popupPages[i] == activePage)
            {
                activeIndex = i;
                break;
            }
        }

        for (int i = 0; i < stepDots.Length; i++)
        {
            if (stepDots[i] == null) continue;
            bool isActive = (i == activeIndex);
            if (isActive && dotOnSprite != null)
            {
                stepDots[i].sprite = dotOnSprite;
                stepDots[i].transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            }
            else if (!isActive && dotOffSprite != null)
            {
                stepDots[i].sprite = dotOffSprite;
                stepDots[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void Hide()
    {
        if (_isClosing) return;
        _isClosing = true;
        StopPageAnimation();
        _currentPage = null;
        var target = rootPanel != null ? rootPanel : gameObject;

        // VONG 16 — FIX: neu chinh GameObject nay dang TAT thi Unity khong cho
        // StartCoroutine (loi "Coroutine couldn't be started because the game
        // object 'Tutorial_GuideBoard' is inactive!"). Truong hop nay bang da
        // an san roi, chi can dong ngay khong can animation.
        if (!isActiveAndEnabled || target == null)
        {
            if (target != null) target.SetActive(false);
            _isClosing = false;
            return;
        }

        StartCoroutine(BounceOutRoutine(target.transform, () => {
            target.SetActive(false);
            _isClosing = false;
        }));
    }

    private void OnConfirmClicked()
    {
        if (confirmBurstParticles != null) confirmBurstParticles.Play();
        
        if (confirmButton != null)
        {
            StartCoroutine(PunchScaleRoutine(confirmButton.transform, new Vector3(1.15f, 1.15f, 1f), 0.2f));
        }

        StartCoroutine(DelayedConfirm(0.3f));
    }

    private IEnumerator DelayedConfirm(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        TutorialManager.Instance?.ConfirmGuidePopup();
    }

    private IEnumerator BounceInRoutine(Transform t)
    {
        float duration1 = 0.3f;
        float elapsed = 0f;
        Vector3 targetScale = new Vector3(1.1f, 1.1f, 1.1f);
        
        while (elapsed < duration1)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration1;
            float ease = progress * (2 - progress);
            t.localScale = Vector3.Lerp(Vector3.zero, targetScale, ease);
            yield return null;
        }

        float duration2 = 0.2f;
        elapsed = 0f;
        Vector3 startScale = targetScale;
        targetScale = Vector3.one;
        while (elapsed < duration2)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration2;
            float ease = progress < 0.5f ? 2 * progress * progress : -1 + (4 - 2 * progress) * progress;
            t.localScale = Vector3.Lerp(startScale, targetScale, ease);
            yield return null;
        }
        t.localScale = targetScale;
    }

    private IEnumerator BounceOutRoutine(Transform t, Action onComplete)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = t.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float s = 1.70158f;
            float ease = progress * progress * ((s + 1) * progress - s);
            t.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, ease);
            yield return null;
        }
        t.localScale = Vector3.zero;
        onComplete?.Invoke();
    }

    private IEnumerator PunchScaleRoutine(Transform t, Vector3 punchScale, float duration)
    {
        Vector3 originalScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float scaleMulti = Mathf.Sin(progress * Mathf.PI);
            t.localScale = Vector3.Lerp(originalScale, punchScale, scaleMulti);
            yield return null;
        }
        t.localScale = originalScale;
    }

    private IEnumerator TransitionPagesRoutine(PopupPage oldPage, PopupPage newPage)
    {
        float duration = 0.4f;
        float elapsed = 0f;

        CanvasGroup oldCg = null;
        Vector3 oldStartPos = Vector3.zero;
        if (oldPage != null && oldPage.root != null)
        {
            oldCg = oldPage.root.GetComponent<CanvasGroup>();
            if (oldCg == null) oldCg = oldPage.root.AddComponent<CanvasGroup>();
            oldStartPos = oldPage.root.transform.localPosition;
        }

        CanvasGroup newCg = null;
        Vector3 newTargetPos = Vector3.zero;
        Vector3 newStartPos = Vector3.zero;
        if (newPage != null && newPage.root != null)
        {
            newPage.root.SetActive(true);
            newCg = newPage.root.GetComponent<CanvasGroup>();
            if (newCg == null) newCg = newPage.root.AddComponent<CanvasGroup>();
            
            newTargetPos = Vector3.zero;
            newStartPos = newTargetPos + new Vector3(200f, 0, 0);
            newPage.root.transform.localPosition = newStartPos;
            newCg.alpha = 0f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            float s = 1.70158f;
            float p = progress - 1;
            float easeOutBack = (p * p * ((s + 1) * p + s) + 1);
            float easeOutQuad = progress * (2 - progress);

            if (oldPage != null && oldPage.root != null && oldCg != null)
            {
                oldPage.root.transform.localPosition = Vector3.Lerp(oldStartPos, oldStartPos + new Vector3(-200f, 0, 0), easeOutQuad);
                oldCg.alpha = Mathf.Lerp(1f, 0f, easeOutQuad);
            }

            if (newPage != null && newPage.root != null && newCg != null)
            {
                newPage.root.transform.localPosition = Vector3.LerpUnclamped(newStartPos, newTargetPos, easeOutBack);
                newCg.alpha = Mathf.Lerp(0f, 1f, easeOutQuad);
            }

            yield return null;
        }

        if (oldPage != null && oldPage.root != null)
        {
            oldPage.root.SetActive(false);
            oldPage.root.transform.localPosition = oldStartPos;
            if (oldCg != null) oldCg.alpha = 1f;
        }

        if (newPage != null && newPage.root != null)
        {
            newPage.root.transform.localPosition = newTargetPos;
            if (newCg != null) newCg.alpha = 1f;
        }
    }

    private IEnumerator TypewriterEffect(TextMeshProUGUI textComp)
    {
        string fullText = textComp.text;
        textComp.maxVisibleCharacters = 0;
        
        float charDelay = 0.03f;
        int totalChars = fullText.Length;
        int visibleChars = 0;
        
        while (visibleChars < totalChars)
        {
            visibleChars++;
            textComp.maxVisibleCharacters = visibleChars;
            yield return new WaitForSecondsRealtime(charDelay);
        }
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

    private IEnumerator AnimateIconsFloat(PopupPage page)
    {
        // Lấy tất cả các hình ảnh icon bên trong page này
        Image[] images = page.root.GetComponentsInChildren<Image>(true);
        List<Transform> icons = new List<Transform>();
        
        foreach (var img in images)
        {
            // ⚠️ [VÒNG 14] TRƯỚC ĐÂY LỌC THEO TÊN ("panel"/"bg") — SÓT.
            // `Template_Process_Diamond` là TẤM NỀN VÀNG cỡ 560×300, tên không chứa từ nào
            // trong danh sách nên bị coi là "icon" ⇒ bị xoay ±5° quanh Z và trôi ±10px
            // ⇒ nền nghiêng đè lên dòng chữ hướng dẫn (đúng lỗi Sếp chụp ở trang BƯỚC 2).
            // Nay dùng DANH SÁCH TRẮNG: chỉ thứ ĐÚNG LÀ icon nhỏ mới được float.
            string ten = img.gameObject.name;
            bool laIconThat = ten.StartsWith("Icon") || ten.StartsWith("Image")
                              || ten.StartsWith("Diamond_") || ten.StartsWith("Badge");

            if (!laIconThat
                || img.gameObject == page.root
                || ten.StartsWith("Template_")            // tấm nền — TUYỆT ĐỐI không xoay
                || img.GetComponent<Button>() != null
                || (page.animatedHand != null && img.transform.IsChildOf(page.animatedHand)))
            {
                continue;
            }
            icons.Add(img.transform);
        }

        Vector3[] startLocalPos = new Vector3[icons.Count];
        for (int i = 0; i < icons.Count; i++) startLocalPos[i] = icons[i].localPosition;

        float time = 0f;
        while (true)
        {
            time += Time.unscaledDeltaTime * 2f;
            for (int i = 0; i < icons.Count; i++)
            {
                Transform icon = icons[i];
                if (icon == null) continue;
                
                // Hiệu ứng "Bay bay" lơ lửng
                float floatOffset = Mathf.Sin(time + i) * 10f; // Nhấp nhô 10 pixel
                
                // Hiệu ứng "3D" xoay lắc nhẹ
                float rotY = Mathf.Sin(time * 0.7f + i) * 15f; // Lắc trái phải 15 độ
                float rotZ = Mathf.Cos(time * 0.5f + i) * 5f;  // Nghiêng nhẹ 5 độ

                icon.localPosition = startLocalPos[i] + new Vector3(0, floatOffset, 0);
                icon.localRotation = Quaternion.Euler(0, rotY, rotZ);
            }
            yield return null;
        }
    }

    private void StopPageAnimation()
    {
        if (_handRoutine != null) StopCoroutine(_handRoutine);
        _handRoutine = null;
        if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
        _typewriterRoutine = null;
        if (_idleFloatRoutine != null) StopCoroutine(_idleFloatRoutine);
        _idleFloatRoutine = null;

        // Reset icon positions just in case
        if (_currentPage != null && _currentPage.root != null)
        {
            Image[] images = _currentPage.root.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                img.transform.localRotation = Quaternion.identity;
                // Vị trí pos thì khó reset chính xác vì bị offset bởi layout, nhưng tắt trang đi là ẩn rồi.
            }
        }
    }

    private void OnDisable() => StopPageAnimation();
}
