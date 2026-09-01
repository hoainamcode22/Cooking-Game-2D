using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Loại thưởng mà RewardFlyFX biết bay: vàng, kim cương, EXP (sao xanh).</summary>
public enum RewardKind
{
    Gold,
    Gem,
    Exp
}

/// <summary>
/// HIỆU ỨNG "TIỀN TỆ BAY VỀ HUD" KIỂU TOWNSHIP — HỆ HỢP NHẤT V2
/// ═══════════════════════════════════════════════════════════════
/// Thay thế cặp CoinFlyFX + GemFlyFX (hai file gần-trùng-nhau) bằng MỘT hệ cho cả
/// VÀNG, KIM CƯƠNG và EXP:
///
///   Pha 1 — BUNG: icon nở ra thành chùm radial (~0.18s) với ease-out-back
///            (FxEase.OutBackRaw, overshoot nhẹ — đúng "vị nảy" tài liệu Township §4.3).
///   KHỰNG  — đứng yên một nhịp ngắn (~0.07s) cho mắt kịp thấy chùm icon.
///   Pha 2 — BAY: từng icon RỜI SO LE (stagger 0.05s), bay theo đường cong bezier
///            (control point lệch VUÔNG GÓC ngẫu nhiên 30–80px) về icon HUD tương ứng,
///            thu nhỏ 1 → 0.45; riêng VÀNG xoay nhẹ như đồng xu.
///   CHẠM   — mỗi icon chạm đích gọi JuicyPulseFX.Play(target) cho icon HUD nảy mẩy mẩy.
///
/// NGUỒN SỰ KIỆN (tự nghe, không cần ai gọi tay):
///   FarmEconomyManager.OnGoldAddedFx / OnGemAddedFx · PlayerProgressManager.OnExpAddedFx
///
/// SPRITE: lấy từ RewardIconLibrary (Resources/RewardIconLibrary.asset — CẢ GAME dùng
/// chung 1 bộ icon); thiếu library thì mượn sprite icon HUD đích; vẫn thiếu thì vẽ
/// fallback runtime (xu vàng tròn / lục giác tím / ngôi sao 5 cánh xanh lá).
///
/// THỜI GIAN: dùng Time.unscaledDeltaTime — thưởng có thể được cộng từ popup đang
/// pause game (bài học từ MillCollectFlyFX: CoinFlyFX dùng deltaTime nên đứng hình khi
/// timeScale = 0).
///
/// LEGACY: mặc định Awake/Start sẽ TẮT (enabled = false, KHÔNG destroy) CoinFlyFX và
/// GemFlyFX trong scene để không bị FX nhân đôi. Tắt cả ở Start vì GemFlyFX có
/// [RuntimeInitializeOnLoadMethod(AfterSceneLoad)] Bootstrap tự AddComponent SAU Awake
/// của scene — chỉ tắt trong Awake là lọt lưới. Muốn quay về hệ cũ: bỏ tick
/// disableLegacyFx rồi tắt/xoá component này.
/// </summary>
[DisallowMultipleComponent]
public class RewardFlyFX : MonoBehaviour
{
    public static RewardFlyFX Instance { get; private set; }

    [Header("Wiring (để trống = tự tìm)")]
    [SerializeField] private Canvas canvas;                 // HUD canvas (nơi gắn icon bay)
    [SerializeField] private RectTransform targetGold;     // đích vàng — Gold_Container/Icon_Gold
    [SerializeField] private RectTransform targetGem;      // đích kim cương — Diamond_Container/Icon_Diamond
    [SerializeField] private RectTransform targetExp;      // đích EXP — cụm level/EXP bar top-left

    [Header("Legacy")]
    [Tooltip("TRUE (mặc định): tự tắt CoinFlyFX/GemFlyFX trong scene (enabled = false, KHÔNG destroy) để tránh FX nhân đôi. Revert: bỏ tick rồi bật lại 2 component cũ.")]
    [SerializeField] private bool disableLegacyFx = true;

    [Header("Tuning — Pha 1: bung")]
    [SerializeField] private float burstTime = 0.18f;      // 0.15–0.25s theo spec Township
    [SerializeField] private float burstRadiusMin = 40f;
    [SerializeField] private float burstRadiusMax = 75f;
    [SerializeField] private float burstOvershoot = 0.18f; // độ vượt của ease-out-back
    [SerializeField] private float holdTime = 0.07f;       // khựng nhẹ trước khi bay

    [Header("Tuning — Pha 2: bay")]
    [SerializeField] private float flyDuration = 0.62f;
    [SerializeField] private float staggerDelay = 0.05f;   // mỗi icon rời chùm cách nhau
    [SerializeField] private float bendMin = 30f;          // control point lệch vuông góc 30–80px
    [SerializeField] private float bendMax = 80f;
    [SerializeField] private float endScale = 0.45f;

    private const float IconSizeGold = 54f;   // [V2] Sếp yêu cầu icon bay to rõ như Township
    private const float IconSizeGem  = 48f;   // [V2]
    private const float IconSizeExp  = 46f;   // [V2]

    // Fallback sprite vẽ runtime, cache static dùng chung (pattern CoinFlyFX.GetFallbackSprite)
    private static Sprite fallbackGold;
    private static Sprite fallbackGem;
    private static Sprite fallbackExp;

    // c1 của ease-out-back giải MỘT LẦN từ overshoot (FxEase dặn: đừng gọi Newton mỗi frame)
    private float backC1 = -1f;

    private readonly List<GameObject> liveIcons = new List<GameObject>(16);
    private bool warnedMissingCanvas;

    // ─────────────────────────── VÒNG ĐỜI ───────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Không phá singleton cũ — component thừa tự tắt cho êm.
            enabled = false;
            return;
        }

        Instance = this;
        DisableLegacyFxIfAny("Awake");
    }

    private void Start()
    {
        // Lưới thứ hai: GemFlyFX.Bootstrap ([RuntimeInitializeOnLoadMethod AfterSceneLoad])
        // có thể vừa AddComponent GemFlyFX SAU Awake của scene. Start chạy sau đó nên quét lại.
        DisableLegacyFxIfAny("Start");
    }

    private void OnEnable()
    {
        FarmEconomyManager.OnGoldAddedFx += HandleGoldAdded;
        FarmEconomyManager.OnGemAddedFx += HandleGemAdded;
        PlayerProgressManager.OnExpAddedFx += HandleExpAdded;
    }

    private void OnDisable()
    {
        FarmEconomyManager.OnGoldAddedFx -= HandleGoldAdded;
        FarmEconomyManager.OnGemAddedFx -= HandleGemAdded;
        PlayerProgressManager.OnExpAddedFx -= HandleExpAdded;

        for (int i = 0; i < liveIcons.Count; i++)
        {
            if (liveIcons[i] != null)
                Destroy(liveIcons[i]);
        }
        liveIcons.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void DisableLegacyFxIfAny(string giaiDoan)
    {
        if (!disableLegacyFx) return;

        int soTat = 0;

        var coins = FindObjectsByType<CoinFlyFX>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < coins.Length; i++)
        {
            if (coins[i] != null && coins[i].enabled)
            {
                coins[i].enabled = false;
                soTat++;
            }
        }

        var gems = FindObjectsByType<GemFlyFX>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < gems.Length; i++)
        {
            if (gems[i] != null && gems[i].enabled)
            {
                gems[i].enabled = false;
                soTat++;
            }
        }

        if (soTat > 0)
        {
            Debug.Log($"[RewardFlyFX/{giaiDoan}] Đã tắt {soTat} component CoinFlyFX/GemFlyFX cũ " +
                      "(enabled = false, KHÔNG destroy) — RewardFlyFX tiếp quản hiệu ứng bay. " +
                      "Revert: bỏ tick 'disableLegacyFx' trên RewardFlyFX rồi bật lại component cũ.", this);
        }
    }

    // ─────────────────────────── API TĨNH ───────────────────────────

    /// <summary>Bay từ vị trí con trỏ (Input System); không có con trỏ → giữa-dưới màn hình.</summary>
    public static void Fly(RewardKind kind, int amount)
    {
        var inst = Instance;
        if (inst == null) return;

        Vector2 startScreen;
        var pointer = Pointer.current;
        if (pointer != null)
            startScreen = pointer.position.ReadValue();
        else
            startScreen = new Vector2(Screen.width * 0.5f, Screen.height / 3f);

        inst.SpawnFromScreen(kind, amount, startScreen);
    }

    /// <summary>Bay từ một điểm WORLD (vd: chuồng gà, cánh đồng). worldCam null → Camera.main.</summary>
    public static void Fly(RewardKind kind, int amount, Vector3 worldPos, Camera worldCam = null)
    {
        var inst = Instance;
        if (inst == null) return;

        Camera cam = worldCam != null ? worldCam : Camera.main;
        Vector2 startScreen = cam != null
            ? RectTransformUtility.WorldToScreenPoint(cam, worldPos)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        inst.SpawnFromScreen(kind, amount, startScreen);
    }

    /// <summary>Bay từ một điểm SCREEN (pixel) — cho popup UI tự chọn điểm xuất phát.</summary>
    public static void FlyFromScreen(RewardKind kind, int amount, Vector2 screenPos)
    {
        var inst = Instance;
        if (inst == null) return;

        inst.SpawnFromScreen(kind, amount, screenPos);
    }

    // ─────────────────────────── NGHE SỰ KIỆN ───────────────────────────

    private void HandleGoldAdded(int amount) => SpawnFromPointer(RewardKind.Gold, amount);
    private void HandleGemAdded(int amount)  => SpawnFromPointer(RewardKind.Gem, amount);
    private void HandleExpAdded(int amount)  => SpawnFromPointer(RewardKind.Exp, amount);

    private void SpawnFromPointer(RewardKind kind, int amount)
    {
        Vector2 startScreen;
        var pointer = Pointer.current;
        if (pointer != null)
            startScreen = pointer.position.ReadValue();
        else
            startScreen = new Vector2(Screen.width * 0.5f, Screen.height / 3f);

        SpawnFromScreen(kind, amount, startScreen);
    }

    // ─────────────────────────── LÕI SPAWN ───────────────────────────

    private void SpawnFromScreen(RewardKind kind, int amount, Vector2 startScreen)
    {
        if (!isActiveAndEnabled || amount <= 0)
            return;

        if (!ResolveCanvas())
            return;

        RectTransform target = ResolveTarget(kind);

        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 endScreen = target != null
            ? RectTransformUtility.WorldToScreenPoint(uiCam, target.position)
            : FallbackEndScreen(kind);

        var canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, uiCam, out Vector2 startLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreen, uiCam, out Vector2 endLocal);

        int count = IconCountFor(kind, amount);
        Sprite sprite = ResolveSprite(kind, target);
        if (sprite == null) return; // thà không có hiệu ứng còn hơn bay ô vuông trắng

        if (backC1 < 0f)
            backC1 = FxEase.BackConstantFor(Mathf.Max(0f, burstOvershoot));

        for (int i = 0; i < count; i++)
            StartCoroutine(FlyOneIcon(kind, sprite, target, startLocal, endLocal, i));
    }

    /// <summary>Số icon theo loại thưởng: vàng amount/15+1 (1–8), gem amount (1–5), exp amount/10+1 (1–6).</summary>
    private static int IconCountFor(RewardKind kind, int amount)
    {
        switch (kind)
        {
            case RewardKind.Gold: return Mathf.Clamp(amount / 15 + 1, 1, 8);
            case RewardKind.Gem:  return Mathf.Clamp(amount, 1, 5);
            default:              return Mathf.Clamp(amount / 10 + 1, 1, 6);
        }
    }

    private static Vector2 FallbackEndScreen(RewardKind kind)
    {
        // Không tìm được icon HUD → bay về đúng GÓC màn hình nơi cụm đó thường nằm
        // (Top-Right: vàng rồi kim cương · Top-Left: EXP/level).
        switch (kind)
        {
            case RewardKind.Gold: return new Vector2(Screen.width * 0.72f, Screen.height * 0.94f);
            case RewardKind.Gem:  return new Vector2(Screen.width * 0.88f, Screen.height * 0.94f);
            default:              return new Vector2(Screen.width * 0.12f, Screen.height * 0.94f);
        }
    }

    // ─────────────────────────── COROUTINE BAY ───────────────────────────

    private IEnumerator FlyOneIcon(RewardKind kind, Sprite sprite, RectTransform target,
                                   Vector2 startLocal, Vector2 endLocal, int index)
    {
        RectTransform icon = CreateIcon(kind, sprite);
        if (icon == null) yield break;

        // Hướng bung chia đều vòng tròn + nhiễu nhẹ để chùm không đều tăm tắp
        float angle = (index * 360f / 8f) + Random.Range(-22f, 22f);
        float radius = Random.Range(burstRadiusMin, burstRadiusMax);
        Vector2 burstPos = startLocal + (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector3.right) * radius;

        float spin = kind == RewardKind.Gold ? Random.Range(-200f, 200f) : 0f;

        icon.anchoredPosition = startLocal;
        icon.localScale = Vector3.zero;

        // ── Pha 1: bung radial, ease-out-back (overshoot nhẹ) + scale nở ra ──
        float dur = Mathf.Max(0.02f, burstTime);
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float raw = Mathf.Clamp01(t / dur);
            float k = FxEase.OutBackRaw(raw, backC1);
            icon.anchoredPosition = Vector2.LerpUnclamped(startLocal, burstPos, k);

            float s = Mathf.Lerp(0.55f, 1f, FxEase.OutQuad(raw));
            icon.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        icon.localScale = Vector3.one;

        // ── Khựng nhẹ + so le: icon thứ i rời chùm sau i × stagger ──
        float wait = Mathf.Max(0f, holdTime) + index * Mathf.Max(0f, staggerDelay);
        t = 0f;
        while (t < wait)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // ── Pha 2: bezier về HUD — control point lệch VUÔNG GÓC 30–80px, ngẫu nhiên trái/phải ──
        Vector2 dir = endLocal - burstPos;
        Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
        float bend = Random.Range(bendMin, bendMax) * (Random.value < 0.5f ? -1f : 1f);
        Vector2 control = (burstPos + endLocal) * 0.5f + perp * bend;

        dur = Mathf.Max(0.05f, flyDuration);
        t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float raw = Mathf.Clamp01(t / dur);
            float k = raw * raw * (3f - 2f * raw); // smoothstep — cùng vị GemFlyFX/MillCollectFlyFX

            icon.anchoredPosition = Bezier(burstPos, control, endLocal, k);

            float s = Mathf.Lerp(1f, endScale, raw);
            icon.localScale = new Vector3(s, s, 1f);

            if (spin != 0f)
                icon.Rotate(0f, 0f, spin * Time.unscaledDeltaTime);

            yield return null;
        }

        // ── Chạm đích: icon HUD nảy mẩy mẩy ──
        if (target != null)
            JuicyPulseFX.Play(target, 1.20f, 0.22f);

        liveIcons.Remove(icon.gameObject);
        Destroy(icon.gameObject);
    }

    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return (u * u) * a + (2f * u * t) * b + (t * t) * c;
    }

    private RectTransform CreateIcon(RewardKind kind, Sprite sprite)
    {
        if (canvas == null) return null;

        var go = new GameObject("RewardFx_" + kind, typeof(RectTransform), typeof(Image));
        go.layer = canvas.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        float size = kind == RewardKind.Gold ? IconSizeGold : kind == RewardKind.Gem ? IconSizeGem : IconSizeExp;
        rt.sizeDelta = new Vector2(size, size);
        rt.SetAsLastSibling();

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false; // icon bay qua nút, không được ăn click
        img.preserveAspect = true;

        liveIcons.Add(go);
        return rt;
    }

    // ─────────────────────────── TÌM CANVAS / ĐÍCH ───────────────────────────

    private bool ResolveCanvas()
    {
        if (canvas != null) return true;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var goldContainer = GameObject.Find("Gold_Container");
            if (goldContainer != null)
                canvas = goldContainer.GetComponentInParent<Canvas>();
        }
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

        if (canvas != null)
        {
            canvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            return true;
        }

        if (!warnedMissingCanvas)
        {
            warnedMissingCanvas = true;
            Debug.LogWarning("[RewardFlyFX] Thiếu Canvas — bỏ qua hiệu ứng bay thưởng.", this);
        }
        return false;
    }

    /// <summary>
    /// Đích bay theo loại. KHÔNG cache vĩnh viễn: HUD có thể bị builder tool dựng lại
    /// giữa chừng (bài học từ MillCollectFlyFX) — target đã chết (Unity-null) thì tìm lại.
    /// </summary>
    private RectTransform ResolveTarget(RewardKind kind)
    {
        switch (kind)
        {
            case RewardKind.Gold:
                if (targetGold == null)
                    targetGold = FindByNames("Gold_Container", "Icon_Gold", "GoldContainer", "Gold_Capsule");
                return targetGold;

            case RewardKind.Gem:
                if (targetGem == null)
                    targetGem = FindByNames("Diamond_Container", "Icon_Diamond", "Icon_Gem", "Gem_Container", "Diamond_Capsule");
                return targetGem;

            default:
                if (targetExp == null)
                    targetExp = FindExpTarget();
                return targetExp;
        }
    }

    private static RectTransform FindByNames(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            var go = GameObject.Find(names[i]);
            if (go != null)
            {
                var rt = go.transform as RectTransform;
                if (rt != null) return rt;
            }
        }
        return null;
    }

    /// <summary>
    /// Đích EXP: ưu tiên hỏi thẳng TownshipHUDController (ngôi sao cấp độ txtLevel nằm trong
    /// cụm EXP top-left; imgExpFill là thanh fill) rồi mới dò theo tên object.
    /// </summary>
    private RectTransform FindExpTarget()
    {
        var hud = FarmGame.UI.TownshipHUDController.Instance;
        if (hud != null)
        {
            if (hud.txtLevel != null && hud.txtLevel.transform.parent is RectTransform saoCapDo)
                return saoCapDo; // node cha của số level = ngôi sao cấp độ — đích đẹp nhất

            if (hud.imgExpFill != null)
                return hud.imgExpFill.rectTransform;
        }

        return FindByNames("EXP_Bar_Container", "Exp_Bar_Container", "EXPBar_Container",
                           "Level_Star", "Icon_Level_Star", "Exp_Container");
    }

    // ─────────────────────────── SPRITE ───────────────────────────

    private static Sprite ResolveSprite(RewardKind kind, RectTransform target)
    {
        // 1) Bộ icon dùng chung của cả game
        var lib = RewardIconLibrary.Instance;
        if (lib != null)
        {
            Sprite s = kind == RewardKind.Gold ? lib.goldSprite
                     : kind == RewardKind.Gem  ? lib.gemSprite
                     : lib.expSprite;
            if (s != null) return s;
        }

        // 2) Mượn sprite của chính icon HUD đích (pattern GemFlyFX)
        if (target != null)
        {
            var img = target.GetComponentInChildren<Image>();
            if (img != null && img.sprite != null) return img.sprite;
        }

        // 3) Fallback vẽ runtime
        switch (kind)
        {
            case RewardKind.Gold: return GetFallbackGold();
            case RewardKind.Gem:  return GetFallbackGem();
            default:              return GetFallbackExp();
        }
    }

    private static Sprite GetFallbackGold()
    {
        if (fallbackGold != null) return fallbackGold;

        // Xu tròn vàng viền cam — copy đúng pattern CoinFlyFX.GetFallbackSprite
        const int size = 16;
        var tex = NewFxTexture(size);
        var gold = new Color(1f, 0.84f, 0.2f, 1f);
        var rim  = new Color(0.85f, 0.58f, 0.05f, 1f);
        var clear = new Color(0f, 0f, 0f, 0f);
        float c = (size - 1) * 0.5f;
        float r = size * 0.5f - 0.5f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                tex.SetPixel(x, y, d <= r - 1.5f ? gold : d <= r ? rim : clear);
            }
        tex.Apply();

        fallbackGold = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        return fallbackGold;
    }

    private static Sprite GetFallbackGem()
    {
        if (fallbackGem != null) return fallbackGem;

        // Lục giác tím #B44CE0, viền tím đậm
        var fill = new Color(0.706f, 0.298f, 0.878f, 1f); // #B44CE0
        var rim  = new Color(0.44f, 0.13f, 0.60f, 1f);
        fallbackGem = MakePolygonSprite(BuildRegularPolygon(6, 90f), fill, rim);
        return fallbackGem;
    }

    private static Sprite GetFallbackExp()
    {
        if (fallbackExp != null) return fallbackExp;

        // Ngôi sao 5 cánh xanh lá #7FD64F, viền xanh đậm
        var fill = new Color(0.498f, 0.839f, 0.310f, 1f); // #7FD64F
        var rim  = new Color(0.24f, 0.55f, 0.13f, 1f);
        fallbackExp = MakePolygonSprite(BuildStar(5, 0.5f, 90f), fill, rim);
        return fallbackExp;
    }

    private static Texture2D NewFxTexture(int size)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear
        };
    }

    /// <summary>Đa giác đều n đỉnh, bán kính 1, đỉnh đầu ở góc startDeg.</summary>
    private static Vector2[] BuildRegularPolygon(int n, float startDeg)
    {
        var pts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = (startDeg + i * 360f / n) * Mathf.Deg2Rad;
            pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }
        return pts;
    }

    /// <summary>Ngôi sao points cánh: xen kẽ đỉnh ngoài (r=1) và đỉnh trong (r=innerRatio).</summary>
    private static Vector2[] BuildStar(int points, float innerRatio, float startDeg)
    {
        var pts = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float r = (i % 2 == 0) ? 1f : innerRatio;
            float a = (startDeg + i * 180f / points) * Mathf.Deg2Rad;
            pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        return pts;
    }

    /// <summary>Rasterize đa giác (đơn vị, tâm 0) thành sprite 32px: lõi = fill, mép = rim.</summary>
    private static Sprite MakePolygonSprite(Vector2[] shape, Color fill, Color rim)
    {
        const int size = 32;
        var tex = NewFxTexture(size);
        var clear = new Color(0f, 0f, 0f, 0f);
        float c = (size - 1) * 0.5f;
        float scale = size * 0.5f - 1f;

        // Bản thu nhỏ 0.78 làm ranh giới lõi/viền
        var inner = new Vector2[shape.Length];
        for (int i = 0; i < shape.Length; i++) inner[i] = shape[i] * 0.78f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x - c) / scale, (y - c) / scale);
                if (PointInPolygon(p, inner))       tex.SetPixel(x, y, fill);
                else if (PointInPolygon(p, shape))  tex.SetPixel(x, y, rim);
                else                                tex.SetPixel(x, y, clear);
            }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }
}
