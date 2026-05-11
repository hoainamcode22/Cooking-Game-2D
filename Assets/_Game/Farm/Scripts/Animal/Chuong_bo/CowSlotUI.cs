using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CowSlotUI : MonoBehaviour
{
    public enum CowState { Idle, Feeding, Ready }

    [Header("Tham chiếu")]
    [SerializeField] private Animator cowAnimator;
    [SerializeField] private GameObject vatPhamThit;
    [SerializeField] private float bonam4Duration = 1.2f;

    public CowState CurrentState { get; private set; } = CowState.Idle;
    public Action OnHarvested;

    // Hash tránh lỗi typo string và nhanh hơn lookup mỗi frame
    private static readonly int IsFeedingHash = Animator.StringToHash("IsFeeding");

    private Canvas rootCanvas;
    private Transform cowBody;
    private Image cowImage;
    private Vector3 cowOriginalScale;
    private Coroutine highlightCoroutine;

    // ─── Lifecycle ───────────────────────────────────────────────

    void Awake()
    {
        if (vatPhamThit != null) vatPhamThit.SetActive(false);
        if (cowAnimator != null)
        {
            cowAnimator.SetBool(IsFeedingHash, false);
            cowBody = cowAnimator.transform;
            cowOriginalScale = cowBody.localScale;
            Debug.Log($"[CowSlotUI] Awake {gameObject.name} | animator={cowAnimator.gameObject.name}" +
                      $" | controller={cowAnimator.runtimeAnimatorController?.name ?? "NULL"}");
        }
        else
        {
            Debug.LogError($"[CowSlotUI] Awake {gameObject.name} ANIMATOR IS NULL!");
        }
    }

    void Start()
    {
        // Tìm root canvas để spawn UI particles
        rootCanvas = GetComponentInParent<Canvas>();
        while (rootCanvas != null && !rootCanvas.isRootCanvas)
        {
            Canvas parent = rootCanvas.transform.parent?.GetComponentInParent<Canvas>();
            if (parent == null) break;
            rootCanvas = parent;
        }

        // Image để tint khi highlight (drag hover)
        if (cowBody != null)
            cowImage = cowBody.GetComponent<Image>();
    }

    // ─── Public API ──────────────────────────────────────────────

    public bool CanFeed() => CurrentState == CowState.Idle;
    public GameObject GetVatPhamThit() => vatPhamThit;

    /// <summary>DraggableFeedItem gọi khi drag hover vào / ra khỏi slot này.</summary>
    public void SetDragHighlight(bool active)
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        if (active)
            highlightCoroutine = StartCoroutine(PulseHighlight());
        else
            ResetCowVisual();
    }

    public void StartFeeding(float duration)
    {
        Debug.Log($"[CowSlotUI] StartFeeding called: {gameObject.name} " +
                  $"CanFeed={CanFeed()} animator={cowAnimator != null}");

        if (!CanFeed()) return;
        if (cowAnimator == null)
        {
            Debug.LogError($"[CowSlotUI] cowAnimator NULL on {gameObject.name}!");
            return;
        }

        // Tắt highlight drag trước
        SetDragHighlight(false);

        StopAllCoroutines();
        CurrentState = CowState.Feeding;
        vatPhamThit.SetActive(false);
        cowAnimator.SetBool(IsFeedingHash, true);
        Debug.Log($"[CowSlotUI] SetBool IsFeeding=TRUE | obj={gameObject.name}" +
                  $" | animatorObj={cowAnimator.gameObject.name}" +
                  $" | controller={cowAnimator.runtimeAnimatorController?.name ?? "NULL"}" +
                  $" | hash={IsFeedingHash}");

        // Khởi động hiệu ứng + logic song song
        StartCoroutine(PunchScaleEffect());
        StartCoroutine(SpawnFeedParticles(6));
        StartCoroutine(FeedingCoroutine(duration));
    }

    public void OnHarvestClick()
    {
        if (CurrentState != CowState.Ready) return;

        Vector3 harvestWorldPos = vatPhamThit.transform.position;
        vatPhamThit.SetActive(false);
        CurrentState = CowState.Idle;
        cowAnimator.SetBool(IsFeedingHash, false);

        StartCoroutine(HarvestFlash(harvestWorldPos));
        OnHarvested?.Invoke();
    }

    // ─── Core Coroutine ──────────────────────────────────────────

    IEnumerator FeedingCoroutine(float duration)
    {
        Debug.Log($"[CowSlotUI] Coroutine start, waiting {duration}s");
        yield return new WaitForSeconds(duration);

        Debug.Log($"[CowSlotUI] Duration done, setting IsFeeding=false on {gameObject.name}");
        if (cowAnimator == null)
        {
            Debug.LogError($"[CowSlotUI] cowAnimator NULL mid-coroutine on {gameObject.name}!");
            yield break;
        }
        cowAnimator.SetBool(IsFeedingHash, false);
        CurrentState = CowState.Ready;

        Debug.Log($"[CowSlotUI] Waiting bonam4: {bonam4Duration}s");
        yield return new WaitForSeconds(bonam4Duration);

        Debug.Log($"[CowSlotUI] Show meat!");
        vatPhamThit.SetActive(true);
        StartCoroutine(PopInEffect(vatPhamThit.transform));
    }

    // ─── Effects ─────────────────────────────────────────────────

    // Hiệu ứng 1: Pulse xanh lá nhẹ khi drag hover vào slot
    IEnumerator PulseHighlight()
    {
        if (cowImage == null) yield break;
        Color targetTint = new Color(0.65f, 1f, 0.65f, 1f);
        float speed = 4.5f;
        while (true)
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            cowImage.color = Color.Lerp(Color.white, targetTint, t * 0.55f);
            if (cowBody != null)
            {
                float s = 1f + Mathf.Sin(Time.time * speed) * 0.025f;
                cowBody.localScale = cowOriginalScale * s;
            }
            yield return null;
        }
    }

    void ResetCowVisual()
    {
        if (cowBody != null) cowBody.localScale = cowOriginalScale;
        if (cowImage != null) cowImage.color = Color.white;
    }

    // Hiệu ứng 2: Punch scale khi nhận thức ăn (1 → 1.2 → 1)
    IEnumerator PunchScaleEffect()
    {
        if (cowBody == null) yield break;
        float dur = 0.28f;
        float punch = 1.22f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ratio = t / dur;
            float s = ratio < 0.5f
                ? Mathf.Lerp(1f, punch, ratio * 2f)
                : Mathf.Lerp(punch, 1f, (ratio - 0.5f) * 2f);
            cowBody.localScale = cowOriginalScale * s;
            yield return null;
        }
        cowBody.localScale = cowOriginalScale;
    }

    // Hiệu ứng 3: Các chấm vàng/trắng bay lên quanh bò khi ăn
    IEnumerator SpawnFeedParticles(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnOneParticle();
            yield return new WaitForSeconds(0.07f);
        }
    }

    void SpawnOneParticle()
    {
        if (rootCanvas == null || cowBody == null) return;

        GameObject p = new GameObject("FeedParticle");
        p.transform.SetParent(rootCanvas.transform, false);
        p.transform.SetAsLastSibling();

        Image img = p.AddComponent<Image>();
        img.color = UnityEngine.Random.value > 0.5f
            ? new Color(1f, 0.88f, 0.28f, 1f)   // vàng = mảnh cỏ
            : new Color(0.95f, 0.95f, 0.95f, 0.9f); // trắng = nước bọt
        img.raycastTarget = false;

        RectTransform rt = p.GetComponent<RectTransform>();
        float size = UnityEngine.Random.Range(7f, 15f);
        rt.sizeDelta = new Vector2(size, size);

        // Tính vị trí spawn gần miệng bò (trên + lệch trái/phải)
        Camera cam = rootCanvas.worldCamera;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, cowBody.position);
        screenPt += new Vector2(
            UnityEngine.Random.Range(-55f, 55f),
            UnityEngine.Random.Range(-25f, 35f));

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(), screenPt, cam, out Vector2 local);
        rt.anchoredPosition = local;

        StartCoroutine(FloatFadeParticle(p, rt, img));
    }

    IEnumerator FloatFadeParticle(GameObject p, RectTransform rt, Image img)
    {
        if (p == null) yield break;
        Vector2 startPos = rt.anchoredPosition;
        float floatAmt = UnityEngine.Random.Range(55f, 95f);
        float dur = UnityEngine.Random.Range(0.5f, 0.85f);
        float t = 0f;
        while (t < dur)
        {
            if (p == null) yield break;
            t += Time.deltaTime;
            float ratio = t / dur;
            rt.anchoredPosition = startPos + Vector2.up * floatAmt * ratio;
            Color c = img.color;
            c.a = Mathf.Lerp(1f, 0f, ratio);
            img.color = c;
            yield return null;
        }
        if (p != null) Destroy(p);
    }

    // Hiệu ứng 4: vatPhamThit bật ra với overshoot (0 → 1.3 → 1)
    IEnumerator PopInEffect(Transform target)
    {
        if (target == null) yield break;
        target.localScale = Vector3.zero;
        float dur = 0.38f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ratio = t / dur;
            float s = ratio < 0.62f
                ? Mathf.Lerp(0f, 1.3f, ratio / 0.62f)
                : Mathf.Lerp(1.3f, 1f, (ratio - 0.62f) / 0.38f);
            target.localScale = Vector3.one * s;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    // Hiệu ứng 5: Vòng sáng vàng lan rộng và mờ dần khi thu thịt
    IEnumerator HarvestFlash(Vector3 worldPos)
    {
        if (rootCanvas == null) yield break;

        GameObject flash = new GameObject("HarvestFlash");
        flash.transform.SetParent(rootCanvas.transform, false);
        flash.transform.SetAsLastSibling();

        Image img = flash.AddComponent<Image>();
        img.color = new Color(1f, 0.92f, 0.35f, 0.85f);
        img.raycastTarget = false;

        RectTransform rt = flash.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(55f, 55f);

        Camera cam = rootCanvas.worldCamera;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(), screenPt, cam, out Vector2 local);
        rt.anchoredPosition = local;

        float dur = 0.38f;
        float t = 0f;
        while (t < dur)
        {
            if (flash == null) yield break;
            t += Time.deltaTime;
            float ratio = t / dur;
            flash.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 2.2f, ratio);
            Color c = img.color;
            c.a = Mathf.Lerp(0.85f, 0f, ratio);
            img.color = c;
            yield return null;
        }
        if (flash != null) Destroy(flash);
    }
}
