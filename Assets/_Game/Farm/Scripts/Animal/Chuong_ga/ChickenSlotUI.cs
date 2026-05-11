using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChickenSlotUI : MonoBehaviour
{
    public enum ChickenState { Idle, Feeding, Ready }

    [Header("Tham chiếu")]
    [SerializeField] private Animator chickenAnimator;
    [SerializeField] private GameObject vatPhamThit;   // thịt gà
    [SerializeField] private GameObject vatPhamTrung;  // trứng
    // Thời gian chờ sau khi ganam4 (nằm xuống, 1 lần) rồi mới hiện vật phẩm
    [SerializeField] private float ganam4Duration = 1.2f;

    public ChickenState CurrentState { get; private set; } = ChickenState.Idle;
    // Truyền this để Popup biết slot nào vừa hoàn tất thu hoạch cả hai vật phẩm
    public Action<ChickenSlotUI> OnHarvested;

    // Hash tránh lỗi typo string và nhanh hơn lookup mỗi frame
    private static readonly int IsFeedingHash = Animator.StringToHash("IsFeeding");

    private Canvas rootCanvas;
    private Transform chickenBody;
    private Image chickenImage;
    private Vector3 chickenOriginalScale;
    private Coroutine highlightCoroutine;

    // Theo dõi từng loại vật phẩm đã được user click chưa
    private bool meatHarvested = false;
    private bool eggHarvested = false;

    // ─── Lifecycle ───────────────────────────────────────────────

    void Awake()
    {
        if (vatPhamThit != null) vatPhamThit.SetActive(false);
        if (vatPhamTrung != null) vatPhamTrung.SetActive(false);
        if (chickenAnimator != null)
        {
            chickenAnimator.SetBool(IsFeedingHash, false);
            chickenBody = chickenAnimator.transform;
            chickenOriginalScale = chickenBody.localScale;
            Debug.Log($"[ChickenSlotUI] Awake {gameObject.name} | animator={chickenAnimator.gameObject.name}" +
                      $" | controller={chickenAnimator.runtimeAnimatorController?.name ?? "NULL"}");
        }
        else
        {
            Debug.LogError($"[ChickenSlotUI] Awake {gameObject.name} ANIMATOR IS NULL!");
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
        if (chickenBody != null)
            chickenImage = chickenBody.GetComponent<Image>();
    }

    // ─── Public API ──────────────────────────────────────────────

    public bool CanFeed() => CurrentState == ChickenState.Idle;
    public GameObject GetVatPhamThit() => vatPhamThit;
    public GameObject GetVatPhamTrung() => vatPhamTrung;

    /// <summary>DraggableChickenFeedItem gọi khi drag hover vào / ra khỏi slot này.</summary>
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
            ResetChickenVisual();
    }

    public void StartFeeding(float duration)
    {
        Debug.Log($"[ChickenSlotUI] StartFeeding called: {gameObject.name} " +
                  $"CanFeed={CanFeed()} animator={chickenAnimator != null}");

        if (!CanFeed()) return;
        if (chickenAnimator == null)
        {
            Debug.LogError($"[ChickenSlotUI] chickenAnimator NULL on {gameObject.name}!");
            return;
        }

        // Tắt highlight drag trước
        SetDragHighlight(false);

        StopAllCoroutines();
        CurrentState = ChickenState.Feeding;
        vatPhamThit.SetActive(false);
        vatPhamTrung.SetActive(false);
        chickenAnimator.SetBool(IsFeedingHash, true);
        Debug.Log($"[ChickenSlotUI] SetBool IsFeeding=TRUE | obj={gameObject.name}" +
                  $" | animatorObj={chickenAnimator.gameObject.name}" +
                  $" | controller={chickenAnimator.runtimeAnimatorController?.name ?? "NULL"}" +
                  $" | hash={IsFeedingHash}");

        // Khởi động hiệu ứng + logic song song
        StartCoroutine(PunchScaleEffect());
        StartCoroutine(SpawnFeedParticles(6));
        StartCoroutine(FeedingCoroutine(duration));
    }

    public void OnHarvestMeat()
    {
        // Chỉ thu hoạch khi đang ở trạng thái Ready và thịt chưa bị ẩn
        if (CurrentState != ChickenState.Ready) return;
        if (!vatPhamThit.activeSelf) return;

        Vector3 harvestWorldPos = vatPhamThit.transform.position;
        vatPhamThit.SetActive(false);
        meatHarvested = true;

        StartCoroutine(HarvestFlash(harvestWorldPos));
        CheckAllHarvested();
    }

    public void OnHarvestEgg()
    {
        // Chỉ thu hoạch khi đang ở trạng thái Ready và trứng chưa bị ẩn
        if (CurrentState != ChickenState.Ready) return;
        if (!vatPhamTrung.activeSelf) return;

        Vector3 harvestWorldPos = vatPhamTrung.transform.position;
        vatPhamTrung.SetActive(false);
        eggHarvested = true;

        StartCoroutine(HarvestFlash(harvestWorldPos));
        CheckAllHarvested();
    }

    // ─── Private Helpers ─────────────────────────────────────────

    // Reset về Idle chỉ khi user đã click CẢ HAI thịt và trứng
    private void CheckAllHarvested()
    {
        if (!meatHarvested || !eggHarvested) return;

        CurrentState = ChickenState.Idle;
        meatHarvested = false;
        eggHarvested = false;
        OnHarvested?.Invoke(this);
    }

    // ─── Core Coroutine ──────────────────────────────────────────

    IEnumerator FeedingCoroutine(float duration)
    {
        // ganam2 (đứng dậy, 1 lần) → ganam3 (nhai loop) chạy trong khoảng này
        Debug.Log($"[ChickenSlotUI] Coroutine start, waiting {duration}s");
        yield return new WaitForSeconds(duration);

        Debug.Log($"[ChickenSlotUI] Duration done, setting IsFeeding=false on {gameObject.name}");
        if (chickenAnimator == null)
        {
            Debug.LogError($"[ChickenSlotUI] chickenAnimator NULL mid-coroutine on {gameObject.name}!");
            yield break;
        }
        // IsFeeding=false → ganam4 (nằm xuống, 1 lần) → ganam1 (nằm im, loop)
        chickenAnimator.SetBool(IsFeedingHash, false);
        CurrentState = ChickenState.Ready;

        // Chờ ganam4 (nằm xuống) chạy xong rồi mới hiện cả hai vật phẩm
        Debug.Log($"[ChickenSlotUI] Waiting ganam4: {ganam4Duration}s");
        yield return new WaitForSeconds(ganam4Duration);

        Debug.Log($"[ChickenSlotUI] Show chicken meat + egg!");
        meatHarvested = false;
        eggHarvested = false;
        vatPhamThit.SetActive(true);
        vatPhamTrung.SetActive(true);
        StartCoroutine(PopInEffect(vatPhamThit.transform));
        StartCoroutine(PopInEffect(vatPhamTrung.transform));
    }

    // ─── Effects ─────────────────────────────────────────────────

    // Hiệu ứng 1: Pulse xanh lá nhẹ khi drag hover vào slot
    IEnumerator PulseHighlight()
    {
        if (chickenImage == null) yield break;
        Color targetTint = new Color(0.65f, 1f, 0.65f, 1f);
        float speed = 4.5f;
        while (true)
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            chickenImage.color = Color.Lerp(Color.white, targetTint, t * 0.55f);
            if (chickenBody != null)
            {
                float s = 1f + Mathf.Sin(Time.time * speed) * 0.025f;
                chickenBody.localScale = chickenOriginalScale * s;
            }
            yield return null;
        }
    }

    void ResetChickenVisual()
    {
        if (chickenBody != null) chickenBody.localScale = chickenOriginalScale;
        if (chickenImage != null) chickenImage.color = Color.white;
    }

    // Hiệu ứng 2: Punch scale khi nhận thức ăn (1 → 1.2 → 1)
    IEnumerator PunchScaleEffect()
    {
        if (chickenBody == null) yield break;
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
            chickenBody.localScale = chickenOriginalScale * s;
            yield return null;
        }
        chickenBody.localScale = chickenOriginalScale;
    }

    // Hiệu ứng 3: Các chấm vàng/nâu bay lên quanh gà khi ăn
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
        if (rootCanvas == null || chickenBody == null) return;

        GameObject p = new GameObject("FeedParticle");
        p.transform.SetParent(rootCanvas.transform, false);
        p.transform.SetAsLastSibling();

        Image img = p.AddComponent<Image>();
        img.color = UnityEngine.Random.value > 0.5f
            ? new Color(1f, 0.88f, 0.28f, 1f)      // vàng = mảnh thóc
            : new Color(0.95f, 0.80f, 0.55f, 0.9f); // nâu nhạt = cám gà
        img.raycastTarget = false;

        RectTransform rt = p.GetComponent<RectTransform>();
        float size = UnityEngine.Random.Range(7f, 15f);
        rt.sizeDelta = new Vector2(size, size);

        // Tính vị trí spawn gần miệng gà
        Camera cam = rootCanvas.worldCamera;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, chickenBody.position);
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

    // Hiệu ứng 4: vatPham bật ra với overshoot (0 → 1.3 → 1)
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

    // Hiệu ứng 5: Vòng sáng vàng lan rộng và mờ dần khi thu vật phẩm
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
