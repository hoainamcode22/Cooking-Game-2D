using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PigSlotUI : MonoBehaviour
{
    public enum PigState { Idle, Feeding, Ready }

    [Header("Tham chiếu")]
    [SerializeField] private Animator pigAnimator;
    [SerializeField] private GameObject vatPhamThit;
    // Thời gian chờ sau khi heonam3 (nằm xuống) chạy xong rồi mới hiện thịt
    [SerializeField] private float heonam3Duration = 1.2f;

    public PigState CurrentState { get; private set; } = PigState.Idle;
    public Action OnHarvested;

    // Hash tránh lỗi typo string và nhanh hơn lookup mỗi frame
    private static readonly int IsFeedingHash = Animator.StringToHash("IsFeeding");

    private Canvas rootCanvas;
    private Transform pigBody;
    private Image pigImage;
    private Vector3 pigOriginalScale;
    private Coroutine highlightCoroutine;

    // ─── Lifecycle ───────────────────────────────────────────────

    void Awake()
    {
        if (vatPhamThit != null) vatPhamThit.SetActive(false);
        if (pigAnimator != null)
        {
            pigAnimator.SetBool(IsFeedingHash, false);
            pigBody = pigAnimator.transform;
            pigOriginalScale = pigBody.localScale;
            Debug.Log($"[PigSlotUI] Awake {gameObject.name} | animator={pigAnimator.gameObject.name}" +
                      $" | controller={pigAnimator.runtimeAnimatorController?.name ?? "NULL"}");
        }
        else
        {
            Debug.LogError($"[PigSlotUI] Awake {gameObject.name} ANIMATOR IS NULL!");
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
        if (pigBody != null)
            pigImage = pigBody.GetComponent<Image>();
    }

    // ─── Public API ──────────────────────────────────────────────

    public bool CanFeed() => CurrentState == PigState.Idle;
    public GameObject GetVatPhamThit() => vatPhamThit;

    /// <summary>DraggablePigFeedItem gọi khi drag hover vào / ra khỏi slot này.</summary>
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
            ResetPigVisual();
    }

    public void StartFeeding(float duration)
    {
        Debug.Log($"[PigSlotUI] StartFeeding called: {gameObject.name} " +
                  $"CanFeed={CanFeed()} animator={pigAnimator != null}");

        if (!CanFeed()) return;
        if (pigAnimator == null)
        {
            Debug.LogError($"[PigSlotUI] pigAnimator NULL on {gameObject.name}!");
            return;
        }

        // Tắt highlight drag trước
        SetDragHighlight(false);

        StopAllCoroutines();
        CurrentState = PigState.Feeding;
        vatPhamThit.SetActive(false);
        pigAnimator.SetBool(IsFeedingHash, true);
        Debug.Log($"[PigSlotUI] SetBool IsFeeding=TRUE | obj={gameObject.name}" +
                  $" | animatorObj={pigAnimator.gameObject.name}" +
                  $" | controller={pigAnimator.runtimeAnimatorController?.name ?? "NULL"}" +
                  $" | hash={IsFeedingHash}");

        // Khởi động hiệu ứng + logic song song
        StartCoroutine(PunchScaleEffect());
        StartCoroutine(SpawnFeedParticles(6));
        StartCoroutine(FeedingCoroutine(duration));
    }

    public void OnHarvestClick()
    {
        if (CurrentState != PigState.Ready) return;

        Vector3 harvestWorldPos = vatPhamThit.transform.position;
        vatPhamThit.SetActive(false);
        CurrentState = PigState.Idle;
        // IsFeeding đã false từ coroutine, đặt lại cho chắc chắn
        pigAnimator.SetBool(IsFeedingHash, false);

        StartCoroutine(HarvestFlash(harvestWorldPos));
        OnHarvested?.Invoke();
    }

    // ─── Core Coroutine ──────────────────────────────────────────

    IEnumerator FeedingCoroutine(float duration)
    {
        // heonam2 (đứng dậy + nhai) và tự nối sang heonam3 chạy trong khoảng này
        Debug.Log($"[PigSlotUI] Coroutine start, waiting {duration}s");
        yield return new WaitForSeconds(duration);

        Debug.Log($"[PigSlotUI] Duration done, setting IsFeeding=false on {gameObject.name}");
        if (pigAnimator == null)
        {
            Debug.LogError($"[PigSlotUI] pigAnimator NULL mid-coroutine on {gameObject.name}!");
            yield break;
        }
        // Heo đã tự chuyển heonam2 → heonam3, set false để dọn state
        pigAnimator.SetBool(IsFeedingHash, false);
        CurrentState = PigState.Ready;

        // Chờ heonam3 (nằm xuống, 1 lần) chạy xong rồi mới hiện thịt
        Debug.Log($"[PigSlotUI] Waiting heonam3: {heonam3Duration}s");
        yield return new WaitForSeconds(heonam3Duration);

        Debug.Log($"[PigSlotUI] Show pork!");
        vatPhamThit.SetActive(true);
        StartCoroutine(PopInEffect(vatPhamThit.transform));
    }

    // ─── Effects ─────────────────────────────────────────────────

    // Hiệu ứng 1: Pulse xanh lá nhẹ khi drag hover vào slot
    IEnumerator PulseHighlight()
    {
        if (pigImage == null) yield break;
        Color targetTint = new Color(0.65f, 1f, 0.65f, 1f);
        float speed = 4.5f;
        while (true)
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            pigImage.color = Color.Lerp(Color.white, targetTint, t * 0.55f);
            if (pigBody != null)
            {
                float s = 1f + Mathf.Sin(Time.time * speed) * 0.025f;
                pigBody.localScale = pigOriginalScale * s;
            }
            yield return null;
        }
    }

    void ResetPigVisual()
    {
        if (pigBody != null) pigBody.localScale = pigOriginalScale;
        if (pigImage != null) pigImage.color = Color.white;
    }

    // Hiệu ứng 2: Punch scale khi nhận thức ăn (1 → 1.2 → 1)
    IEnumerator PunchScaleEffect()
    {
        if (pigBody == null) yield break;
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
            pigBody.localScale = pigOriginalScale * s;
            yield return null;
        }
        pigBody.localScale = pigOriginalScale;
    }

    // Hiệu ứng 3: Các chấm vàng/hồng bay lên quanh heo khi ăn
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
        if (rootCanvas == null || pigBody == null) return;

        GameObject p = new GameObject("FeedParticle");
        p.transform.SetParent(rootCanvas.transform, false);
        p.transform.SetAsLastSibling();

        Image img = p.AddComponent<Image>();
        img.color = UnityEngine.Random.value > 0.5f
            ? new Color(1f, 0.88f, 0.28f, 1f)     // vàng = mảnh cám
            : new Color(1f, 0.75f, 0.80f, 0.9f);  // hồng = đặc trưng heo
        img.raycastTarget = false;

        RectTransform rt = p.GetComponent<RectTransform>();
        float size = UnityEngine.Random.Range(7f, 15f);
        rt.sizeDelta = new Vector2(size, size);

        // Tính vị trí spawn gần miệng heo (trên + lệch trái/phải)
        Camera cam = rootCanvas.worldCamera;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, pigBody.position);
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
