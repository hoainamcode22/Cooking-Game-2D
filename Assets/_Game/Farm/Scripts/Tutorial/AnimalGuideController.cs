using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hướng dẫn ngữ cảnh NHẸ cho farm (Demo L1-L10) — phong cách Hay Day:
/// toast nhỏ đáy màn hình + bàn tay tap-hint (GuideTapHintFX), KHÔNG chặn thao tác,
/// mỗi tip chỉ hiện đúng 1 lần (PlayerPrefs).
///   • L2: chuồng gà mở bán   • L4: chuồng heo   • L6: chuồng bò   • L8: chuồng bò sữa
///   • Chuồng gà (Pen_03) lần đầu xuất hiện trong scene → toast + tay chỉ vào chuồng
///   • Đơn làng đầu tiên ĐỦ HÀNG giao → toast + tay chỉ vào nhà có bubble (GUIDE_DELIVER_DONE)
///   • L3+: tàu chở hàng chờ ở ga → toast + tay chỉ vào toa tàu (GUIDE_TRAIN_DONE)
///   • L5: NHÀ BẾP mở → toast + tay chỉ nút/cổng cooking (GUIDE_COOKING_DONE)
/// KHÔNG dính dáng TutorialManager (tutorial L1 đã khoá) — chỉ nghe
/// PlayerProgressManager.OnLevelChanged và vài poll nhẹ (5s/lần, dừng hẳn khi xong).
/// Toast + tap hint tự dựng runtime dưới Canvas_HUD — không cần prefab, không cần asset.
/// Cài đặt: Tools → Farm Game → Demo L1-L10 → Setup All (tự gắn lên object TutorialManager).
/// </summary>
public class AnimalGuideController : MonoBehaviour
{
    public static AnimalGuideController Instance { get; private set; }

    // ── Guide steps theo level ───────────────────────────────────────────────
    private struct LevelStep
    {
        public int level;
        public string prefKey;
        public string message;

        public LevelStep(int level, string prefKey, string message)
        {
            this.level = level;
            this.prefKey = prefKey;
            this.message = message;
        }
    }

    private static readonly LevelStep[] LevelSteps =
    {
        new LevelStep(2, "ANIMAL_GUIDE_L2_DONE",
            "Chuồng gà đã mở bán! Vào Shop mua chuồng gà (100 vàng) rồi cho gà ăn nhé!"),
        new LevelStep(4, "ANIMAL_GUIDE_L4_DONE",
            "Chuồng heo đã mở bán — heo ăn bắp cải/cà rốt cho thịt heo đó!"),
        new LevelStep(6, "ANIMAL_GUIDE_L6_DONE",
            "Chuồng bò đã mở bán — cho bò ăn lúa/ngô để lấy thịt bò nhé!"),
        new LevelStep(8, "ANIMAL_GUIDE_L8_DONE",
            "Chuồng bò sữa đã mở bán — bò sữa ăn lúa/ngô cho sữa giao đơn làng đó!"),
    };

    // ── Tip cho gà ăn khi chuồng gà đầu tiên xuất hiện trong scene ───────────
    private const string FeedPrefKey = "ANIMAL_GUIDE_COOP_FEED_DONE";
    private const string FeedMessage = "Chạm vào chuồng để cho gà ăn (1 lúa hoặc ngô)!";
    // PlacementManager Instantiate prefab Pen_03 → clone tên "Pen_03(Clone)"
    private static readonly string[] CoopNames = { "Pen_03(Clone)", "Pen_03" };
    private const float CoopPollInterval = 5f;

    // ── Tap-hint config (GuideTapHintFX) ─────────────────────────────────────
    private const float TapHintDuration = 8f;

    // Giao hàng dân làng — đơn đầu tiên đủ hàng (poll nhẹ, dừng hẳn sau khi hiện)
    private const string DeliverPrefKey = "GUIDE_DELIVER_DONE";
    private const string DeliverMessage = "Bạn có đủ hàng rồi! Chạm vào nhà có bong bóng để giao đơn nhé!";
    private const float DeliverPollInterval = 5f;
    private const int DeliverMinLevel = 1;

    // Tàu chở hàng — lần đầu tàu đứng ga chờ nạp hàng (từ L3)
    private const string TrainPrefKey = "GUIDE_TRAIN_DONE";
    private const string TrainMessage = "Tàu chở hàng đã đến! Chất nông sản lên tàu để nhận thưởng lớn!";
    private const float TrainPollInterval = 5f;
    private const int TrainMinLevel = 3;

    // Vào bếp nấu ăn — mở khoá ở L5
    private const string CookingPrefKey = "GUIDE_COOKING_DONE";
    private const string CookingMessage = "NHÀ BẾP đã mở! Vào nấu món đầu tiên nào!";
    private const int CookingMinLevel = 5;
    private bool _loggedCookingTargetMissing;

    // ── Toast config ─────────────────────────────────────────────────────────
    private const float ToastDuration = 6f;
    private const float FadeInTime = 0.25f;
    private const float FadeOutTime = 0.3f;
    private const float TextPadX = 28f;
    private const float TextPadY = 18f;

    private readonly Queue<string> _pendingToasts = new Queue<string>();
    private bool _toastShowing;
    private bool _dismissRequested;
    private bool _subscribed;

    private RectTransform _toastRoot;
    private CanvasGroup _toastGroup;
    private TextMeshProUGUI _toastText;
    private static Sprite _roundedSprite;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(SubscribeWhenReady());

        if (!IsDone(FeedPrefKey))
            StartCoroutine(PollForChickenCoop());

        if (!IsDone(DeliverPrefKey))
            StartCoroutine(PollForDeliverableOrder());

        if (!IsDone(TrainPrefKey))
            StartCoroutine(PollForTrainReady());
    }

    private void OnDestroy()
    {
        if (_subscribed && PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;

        if (Instance == this)
            Instance = null;
    }

    // ── Level events ─────────────────────────────────────────────────────────

    private IEnumerator SubscribeWhenReady()
    {
        // PlayerProgressManager là DontDestroyOnLoad — thường có sẵn, nhưng chờ cho chắc
        while (PlayerProgressManager.Instance == null)
            yield return null;

        PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
        _subscribed = true;

        // RaiseAll của manager có thể đã bắn TRƯỚC khi mình subscribe → tự kiểm tra ngay
        HandleLevelChanged(PlayerProgressManager.Instance.Level);
    }

    private void HandleLevelChanged(int level)
    {
        HandleAnimalLevelSteps(level);
        TryShowCookingHint(level);
    }

    private void HandleAnimalLevelSteps(int level)
    {
        // Chỉ toast step CAO NHẤT đã đạt mà chưa xem; các step thấp hơn đánh dấu
        // xong luôn — save cũ vào thẳng L8 chỉ thấy tip bò sữa, không bị spam 4 toast.
        int newest = -1;
        for (int i = 0; i < LevelSteps.Length; i++)
        {
            if (level >= LevelSteps[i].level && !IsDone(LevelSteps[i].prefKey))
                newest = i;
        }

        if (newest < 0)
            return;

        for (int i = 0; i <= newest; i++)
        {
            if (level >= LevelSteps[i].level)
                MarkDone(LevelSteps[i].prefKey);
        }

        EnqueueToast(LevelSteps[newest].message);
    }

    // ── Chicken coop poll (không có event đặt chuồng → poll nhẹ 5s/lần) ──────

    private IEnumerator PollForChickenCoop()
    {
        // Chờ scene load xong (PlacementManager.LoadBuildings chạy ở Start)
        yield return new WaitForSeconds(3f);

        var wait = new WaitForSeconds(CoopPollInterval);
        while (!IsDone(FeedPrefKey))
        {
            for (int i = 0; i < CoopNames.Length; i++)
            {
                GameObject coop = GameObject.Find(CoopNames[i]);
                if (coop == null)
                    continue;

                yield return new WaitForSeconds(0.75f); // để thao tác đặt chuồng kết thúc hẳn
                MarkDone(FeedPrefKey);
                EnqueueToast(FeedMessage);

                // Tay chỉ vào chuồng (coop có thể đã bị huỷ trong lúc chờ → check lại)
                if (coop != null)
                    GuideTapHintFX.ShowAtWorld(coop.transform.position, TapHintDuration);
                yield break;
            }
            yield return wait;
        }
    }

    // ── Giao hàng dân làng — đơn đầu tiên ĐỦ HÀNG (poll 5s, one-shot) ────────

    private IEnumerator PollForDeliverableOrder()
    {
        // Chờ managers (VillageOrderManager gán order ở Start + replenish 5s/lần)
        yield return new WaitForSeconds(4f);

        var wait = new WaitForSeconds(DeliverPollInterval);
        while (!IsDone(DeliverPrefKey))
        {
            yield return wait;

            if (PlayerProgressManager.Instance == null ||
                PlayerProgressManager.Instance.Level < DeliverMinLevel)
                continue;

            var manager = Village.VillageOrderManager.Instance;
            if (manager == null)
                continue;

            Village.HouseOrderController found = null;
            foreach (var house in FindObjectsByType<Village.HouseOrderController>(FindObjectsSortMode.None))
            {
                if (house == null || house.CurrentState != Village.OrderState.Active ||
                    house.CurrentOrder == null)
                    continue;
                if (!manager.HasEnoughForOrder(house.CurrentOrder))
                    continue;

                found = house;
                break;
            }

            if (found == null)
                continue;

            MarkDone(DeliverPrefKey);
            EnqueueToast(DeliverMessage);
            GuideTapHintFX.ShowAtWorld(GetHouseHintPosition(found), TapHintDuration);
            yield break;
        }
    }

    /// <summary>Vị trí tay chỉ: ưu tiên bubble đơn hàng đang hiện, fallback nóc nhà.</summary>
    private static Vector3 GetHouseHintPosition(Village.HouseOrderController house)
    {
        var bubble = house.GetComponentInChildren<Village.HouseOrderBubble>(false);
        if (bubble != null)
            return bubble.transform.position;

        var sr = house.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            return sr.bounds.center + new Vector3(0f, sr.bounds.extents.y * 0.8f, 0f);

        return house.transform.position + Vector3.up;
    }

    // ── Tàu chở hàng — lần đầu tàu đứng ga chờ nạp (L3+, poll 5s, one-shot) ──

    private IEnumerator PollForTrainReady()
    {
        yield return new WaitForSeconds(4f); // chờ TrainManager init xong chuyến đầu

        var wait = new WaitForSeconds(TrainPollInterval);
        while (!IsDone(TrainPrefKey))
        {
            yield return wait;

            if (PlayerProgressManager.Instance == null ||
                PlayerProgressManager.Instance.Level < TrainMinLevel)
                continue;

            if (TrainManager.Instance == null ||
                TrainManager.Instance.State != TrainState.WaitingForLoad)
                continue;

            MarkDone(TrainPrefKey);
            EnqueueToast(TrainMessage);

            if (TryGetTrainHintPosition(out Vector3 pos))
                GuideTapHintFX.ShowAtWorld(pos, TapHintDuration);
            else
                Debug.Log("[AnimalGuide] Không tìm thấy toa tàu/nhà ga để chỉ tay — chỉ hiện toast tàu.");
            yield break;
        }
    }

    /// <summary>Toa tàu đang chờ nạp hàng (collider bật = còn click được), fallback nhà ga.</summary>
    private static bool TryGetTrainHintPosition(out Vector3 pos)
    {
        TrainWagonSlot best = null;
        foreach (var slot in FindObjectsByType<TrainWagonSlot>(FindObjectsSortMode.None))
        {
            if (slot == null || !slot.isActiveAndEnabled)
                continue;

            var col = slot.GetComponent<Collider2D>();
            if (col == null || !col.enabled)
                continue; // toa trống/đã đầy — không click được

            if (best == null || slot.slotIndex < best.slotIndex)
                best = slot; // ưu tiên toa đầu tiên cho ổn định
        }

        if (best != null)
        {
            pos = best.GetWorldPosition();
            return true;
        }

        var station = FindFirstObjectByType<TrainStationBuilding>();
        if (station != null)
        {
            pos = station.transform.position;
            return true;
        }

        pos = default;
        return false;
    }

    // ── Vào bếp nấu ăn — L5 (one-shot, gọi từ HandleLevelChanged) ────────────

    private void TryShowCookingHint(int level)
    {
        if (level < CookingMinLevel || IsDone(CookingPrefKey))
            return;

        MarkDone(CookingPrefKey);
        EnqueueToast(CookingMessage);

        // 1) Nút UI có onClick wired tới FarmUIManager.OnClick_GoCooking
        RectTransform wiredButton = FindCookingButtonRect(requireWiredOnClick: true);
        if (wiredButton != null)
        {
            GuideTapHintFX.ShowAtRect(wiredButton, TapHintDuration);
            return;
        }

        // 2) Cổng bếp ngoài world (CookingGate — BuildingInteractable.CookingGate
        //    gọi OnClick_GoCooking khi click) — entry THẬT của Demo hiện tại.
        Transform gate = FindCookingGateTransform();
        if (gate != null)
        {
            GuideTapHintFX.ShowAtWorld(gate.position, TapHintDuration);
            return;
        }

        // 3) Fallback cuối: nút tên "cook"/"bep" (Btn_cooking trong HUD hiện
        //    KHÔNG có listener — chỉ dùng khi không còn target nào tốt hơn).
        RectTransform namedButton = FindCookingButtonRect(requireWiredOnClick: false);
        if (namedButton != null)
        {
            GuideTapHintFX.ShowAtRect(namedButton, TapHintDuration);
            return;
        }

        if (!_loggedCookingTargetMissing)
        {
            _loggedCookingTargetMissing = true;
            Debug.Log("[AnimalGuide] Không tìm thấy nút/cổng NHÀ BẾP để chỉ tay — chỉ hiện toast.");
        }
    }

    private static RectTransform FindCookingButtonRect(bool requireWiredOnClick)
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (button == null)
                continue;

            if (requireWiredOnClick)
            {
                // Nút wired thẳng vào OnClick_GoCooking (persistent listener)
                int count = button.onClick.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    if (button.onClick.GetPersistentMethodName(i) == "OnClick_GoCooking")
                        return button.transform as RectTransform;
                }
            }
            else
            {
                string n = button.name.ToLowerInvariant();
                if (n.Contains("cook") || n.Contains("bep"))
                    return button.transform as RectTransform;
            }
        }
        return null;
    }

    private static Transform FindCookingGateTransform()
    {
        var kitchen = FindFirstObjectByType<KitchenClickOpen>();
        if (kitchen != null)
            return kitchen.transform;

        GameObject gate = GameObject.Find("CookingGate");
        return gate != null ? gate.transform : null;
    }

    // ── PlayerPrefs flags ────────────────────────────────────────────────────

    private static bool IsDone(string key) => PlayerPrefs.GetInt(key, 0) == 1;

    private static void MarkDone(string key)
    {
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    // ── Toast queue ──────────────────────────────────────────────────────────

    private void EnqueueToast(string message)
    {
        _pendingToasts.Enqueue(message);
        if (!_toastShowing)
            StartCoroutine(DrainToastQueue());
    }

    private IEnumerator DrainToastQueue()
    {
        _toastShowing = true;
        while (_pendingToasts.Count > 0)
        {
            string message = _pendingToasts.Dequeue();
            if (!EnsureToastUI())
            {
                Debug.LogWarning("[AnimalGuide] Không tìm thấy Canvas để hiện toast: " + message);
                continue;
            }

            yield return ShowToastRoutine(message);
            yield return new WaitForSecondsRealtime(0.35f); // nghỉ ngắn giữa 2 toast
        }
        _toastShowing = false;
    }

    private IEnumerator ShowToastRoutine(string message)
    {
        _toastText.text = message;

        // Panel rộng cố định, chữ wrap → tính chiều cao theo nội dung
        float innerWidth = _toastRoot.sizeDelta.x - TextPadX * 2f;
        Vector2 pref = _toastText.GetPreferredValues(message, innerWidth, 0f);
        _toastRoot.sizeDelta = new Vector2(_toastRoot.sizeDelta.x, Mathf.Max(64f, pref.y + TextPadY * 2f));

        _toastRoot.SetAsLastSibling();
        _toastRoot.gameObject.SetActive(true);
        _dismissRequested = false;

        yield return Fade(0f, 1f, FadeInTime);

        float t = 0f;
        while (t < ToastDuration && !_dismissRequested && _toastRoot != null)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_toastRoot == null)
            yield break; // canvas bị huỷ (đổi scene) — toast tự rebuild ở lần sau

        yield return Fade(1f, 0f, FadeOutTime);
        _toastRoot.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (_toastGroup == null)
                yield break;
            t += Time.unscaledDeltaTime;
            _toastGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        if (_toastGroup != null)
            _toastGroup.alpha = to;
    }

    // ── Toast UI (tự dựng runtime, không prefab) ─────────────────────────────

    private bool EnsureToastUI()
    {
        if (_toastRoot != null)
            return true;

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
            return false;

        // Panel nền — bo góc tối, đáy giữa màn hình, KHÔNG chặn thao tác ngoài toast
        var go = new GameObject("AnimalGuideToast", typeof(RectTransform));
        go.layer = canvas.gameObject.layer;
        _toastRoot = go.GetComponent<RectTransform>();
        _toastRoot.SetParent(canvas.transform, false);
        _toastRoot.anchorMin = new Vector2(0.5f, 0f);
        _toastRoot.anchorMax = new Vector2(0.5f, 0f);
        _toastRoot.pivot = new Vector2(0.5f, 0f);

        Rect canvasRect = canvas.GetComponent<RectTransform>().rect;
        float width = Mathf.Min(720f, canvasRect.width * 0.92f);
        if (width <= 0f) width = 720f; // canvas chưa layout xong
        _toastRoot.sizeDelta = new Vector2(width, 96f);
        _toastRoot.anchoredPosition = new Vector2(0f, 140f);

        var bg = go.AddComponent<Image>();
        bg.sprite = GetRoundedSprite();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.07f, 0.09f, 0.14f, 0.92f);
        bg.raycastTarget = true; // chỉ chặn click NGAY TRÊN toast (bấm = tắt sớm)

        var button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => _dismissRequested = true);

        _toastGroup = go.AddComponent<CanvasGroup>();
        _toastGroup.alpha = 0f;
        _toastGroup.blocksRaycasts = true;
        _toastGroup.interactable = true;

        // Text — không gán font: TMP tự dùng default trong TMP Settings
        // (giống các text runtime khác: HarvestAmountTextVFX/SeedCostTextVFX)
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = go.layer;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(_toastRoot, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(TextPadX, TextPadY);
        textRect.offsetMax = new Vector2(-TextPadX, -TextPadY);

        _toastText = textGo.AddComponent<TextMeshProUGUI>();
        _toastText.fontSize = 30f;
        _toastText.color = new Color(1f, 0.97f, 0.9f, 1f);
        _toastText.alignment = TextAlignmentOptions.Center;
        _toastText.textWrappingMode = TextWrappingModes.Normal;
        _toastText.raycastTarget = false;

        go.SetActive(false);
        return true;
    }

    private static Canvas FindHudCanvas()
    {
        Canvas named = null, rootCanvas = null, any = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isActiveAndEnabled || c.renderMode == RenderMode.WorldSpace)
                continue;
            if (named == null && c.name == "Canvas_HUD") named = c;
            if (rootCanvas == null && c.transform.parent == null) rootCanvas = c;
            if (any == null) any = c;
        }
        if (named != null) return named;
        if (rootCanvas != null) return rootCanvas;
        return any;
    }

    // Sprite bo góc tạo bằng code (texture 64×64, bo 20px, viền mềm ~1px) — Sliced
    private static Sprite GetRoundedSprite()
    {
        if (_roundedSprite != null)
            return _roundedSprite;

        const int size = 64;
        const float radius = 20f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "AnimalGuideToastBG",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius - dist + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        _roundedSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(24f, 24f, 24f, 24f));
        _roundedSprite.name = "AnimalGuideToastBG";
        return _roundedSprite;
    }
}
