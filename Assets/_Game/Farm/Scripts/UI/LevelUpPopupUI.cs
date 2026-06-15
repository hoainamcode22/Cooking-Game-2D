using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup lên cấp — hiển thị khi PlayerProgressManager.OnLevelChanged fires.
///
/// Cách setup trong Unity:
///   1. Chạy menu Tools/Farm Game/Setup Level Up Popup để tạo hierarchy.
///   2. Kéo các LevelRewardConfig asset (mỗi asset cho 1 level) vào levelRewardConfigs.
///   3. Kéo VFX prefab (Confetti_blast_multicolor từ Lana Studio) vào vfxConfettiPrefab.
///   4. Đặt component này trên root GameObject của popup.
///
/// Flow:
///   PlayerProgressManager.OnLevelChanged → HandleLevelChanged() → queue → ShowNextPopup()
///   Nhấn "Nhận Quà" → ClaimAndClose() → grant rewards → ShowNextPopup() hoặc đóng
/// </summary>
public class LevelUpPopupUI : MonoBehaviour
{
    // =========================================================================
    // Inspector
    // =========================================================================

    [Header("Level Reward Configs (1 asset mỗi level)")]
    [SerializeField] private List<LevelRewardConfig> levelRewardConfigs = new List<LevelRewardConfig>();

    [Header("Root & Fade")]
    [SerializeField] private GameObject   popupRoot;
    [SerializeField] private CanvasGroup  canvasGroup;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Gold / Gems Display")]
    [SerializeField] private GameObject      goldRewardRow;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private GameObject      gemRewardRow;
    [SerializeField] private TextMeshProUGUI gemRewardText;

    [Header("Gift Items Container")]
    [SerializeField] private Transform  giftItemsContainer;
    [SerializeField] private GameObject giftItemSlotPrefab;

    [Header("Unlock Descriptions")]
    [SerializeField] private TextMeshProUGUI unlockDescText;

    [Header("Buttons")]
    [SerializeField] private Button claimButton;

    [Header("VFX")]
    [Tooltip("LanaDemo02 – confetti bắn từ trên (Confetti_blast_multicolor)")]
    [SerializeField] private GameObject vfxConfettiPrefab;
    [SerializeField] private Transform  vfxSpawnPoint;

    [Tooltip("LanaDemo03 – flash 2 bên (Flash_magic_blue_pink hoặc tương đương)")]
    [SerializeField] private GameObject vfxSidePrefab;
    [SerializeField] private Transform  vfxLeftPoint;
    [SerializeField] private Transform  vfxRightPoint;

    [Header("VFX Screen Composition")]
    [SerializeField] private float vfxTopPanelGap = 70f;
    [SerializeField] private float vfxSidePanelGap = 130f;
    [SerializeField] private float vfxSideVerticalOffset = 70f;
    [SerializeField] private float vfxTopDemoScale = 0.5f;
    [SerializeField] private float vfxSideDemoScale = 0.38f;
    [SerializeField] private float vfxLifetime = 4f;

    [Header("VFX Intensity — bùm bùm rầm rộ tới khi nhận quà")]
    [Tooltip("Phóng to pháo hoa (confetti) phía trên")]
    [SerializeField] private float vfxScaleBoost        = 2.0f;
    [Tooltip("Phóng to Lana03 hai bên (to hơn confetti)")]
    [SerializeField] private float vfxSideScaleBoost    = 2.8f;
    [Tooltip("Nhân số lượng particle (nhiều hơn)")]
    [SerializeField] private float vfxEmissionMultiplier = 2.5f;
    [Tooltip("Khoảng cách giữa các lần bùm (giây). Lặp tới khi user bấm Nhận Quà.")]
    [SerializeField] private float vfxBurstInterval      = 0.6f;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float scaleInDuration = 0.3f;
    [SerializeField] private RectTransform contentPanel;

    // =========================================================================
    // Runtime
    // =========================================================================

    private readonly Queue<int> _levelUpQueue = new Queue<int>();
    private bool                _isShowing    = false;
    private int                 _lastKnownLevel;
    private LevelRewardConfig   _currentConfig;
    private bool                _inputLockHeld;
    private GameObject          _activeVfxRoot;
    private Coroutine           _vfxLoop;

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Start()
    {
        if (popupRoot != null) popupRoot.SetActive(false);

        if (PlayerProgressManager.Instance != null)
        {
            _lastKnownLevel = PlayerProgressManager.Instance.Level;
            PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
        }
        else
        {
            Debug.LogWarning("[LevelUpPopupUI] PlayerProgressManager.Instance không tìm thấy tại Start(). " +
                             "Đặt PlayerProgressManager vào scene trước LevelUpPopupUI.");
        }

        if (claimButton != null)
            claimButton.onClick.AddListener(ClaimAndClose);
    }

    private void OnDestroy()
    {
        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;

        StopVFX();
        ReleaseInputLock();
    }

    private void OnDisable()
    {
        StopVFX();
    }

    // =========================================================================
    // Event Handler
    // =========================================================================

    private void HandleLevelChanged(int newLevel)
    {
        // Bỏ qua lần gọi đầu tiên khi Start() đồng bộ UI; và khi reset xuống (vd về L1)
        // → đồng bộ lại mốc để lần lên cấp sau vẫn hiện popup + pháo hoa.
        if (newLevel <= _lastKnownLevel) { _lastKnownLevel = newLevel; return; }

        _lastKnownLevel = newLevel;
        _levelUpQueue.Enqueue(newLevel);

        if (!_isShowing)
            ShowNextPopup();
    }

    // =========================================================================
    // Show Logic
    // =========================================================================

    private void ShowNextPopup()
    {
        if (_levelUpQueue.Count == 0)
        {
            _isShowing = false;
            return;
        }

        int level = _levelUpQueue.Dequeue();
        _isShowing    = true;
        _currentConfig = levelRewardConfigs.Find(c => c != null && c.levelReached == level);

        PopulateUI(level, _currentConfig);

        if (popupRoot != null) popupRoot.SetActive(true);

        AcquireInputLock();
        SpawnVFX();
        StartCoroutine(AnimateIn());
    }

    private void PopulateUI(int level, LevelRewardConfig cfg)
    {
        // Title
        if (titleText != null)
            titleText.text = $"Lên cấp {level}!";

        // Clear gift slots
        if (giftItemsContainer != null)
            foreach (Transform child in giftItemsContainer)
                Destroy(child.gameObject);

        if (cfg != null)
        {
            // Gold row
            bool hasGold = cfg.giftGold > 0;
            if (goldRewardRow  != null) goldRewardRow.SetActive(hasGold);
            if (goldRewardText != null) goldRewardText.text = $"+{cfg.giftGold}";

            // Gem row
            bool hasGem = cfg.giftGems > 0;
            if (gemRewardRow  != null) gemRewardRow.SetActive(hasGem);
            if (gemRewardText != null) gemRewardText.text = $"+{cfg.giftGems}";

            // Gift item slots
            if (giftItemsContainer != null && giftItemSlotPrefab != null)
            {
                foreach (var gift in cfg.giftItems)
                {
                    var go   = Instantiate(giftItemSlotPrefab, giftItemsContainer);
                    var slot = go.GetComponent<LevelUpGiftSlotUI>();
                    if (slot != null) slot.Setup(gift);
                }
            }

            // Unlock descriptions
            if (unlockDescText != null)
            {
                if (cfg.unlockDescriptions != null && cfg.unlockDescriptions.Count > 0)
                {
                    unlockDescText.text = "Mở khóa: " + string.Join(", ", cfg.unlockDescriptions);
                    unlockDescText.gameObject.SetActive(true);
                }
                else
                {
                    unlockDescText.gameObject.SetActive(false);
                }
            }

            // Hint text
            if (hintText != null)
            {
                bool hasHint = !string.IsNullOrEmpty(cfg.hintText);
                hintText.text = hasHint ? cfg.hintText : "";
                hintText.gameObject.SetActive(hasHint);
            }
        }
        else
        {
            // Không có config → hiển thị minimal
            if (goldRewardRow != null) goldRewardRow.SetActive(false);
            if (gemRewardRow  != null) gemRewardRow.SetActive(false);
            if (unlockDescText != null) unlockDescText.gameObject.SetActive(false);
            if (hintText      != null) hintText.gameObject.SetActive(false);

            Debug.Log($"[LevelUpPopupUI] Không tìm thấy LevelRewardConfig cho level {level}. " +
                      "Tạo asset và kéo vào levelRewardConfigs list.");
        }
    }

    // =========================================================================
    // Claim & Close
    // =========================================================================

    private void ClaimAndClose()
    {
        GrantRewards(_currentConfig);
        StopVFX(); // bấm Nhận Quà → tắt pháo hoa NGAY rồi mới đóng popup
        StartCoroutine(AnimateOut(() =>
        {
            StopVFX();
            if (popupRoot != null) popupRoot.SetActive(false);
            ReleaseInputLock();
            ShowNextPopup();
        }));
    }

    private void GrantRewards(LevelRewardConfig cfg)
    {
        if (cfg == null) return;

        if (cfg.giftGold > 0 && FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.AddGold(cfg.giftGold);
            Debug.Log($"[LevelUpPopup] +{cfg.giftGold} vàng");
        }

        if (cfg.giftGems > 0 && FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.AddGems(cfg.giftGems);
            Debug.Log($"[LevelUpPopup] +{cfg.giftGems} kim cương");
        }

        if (WarehouseManager.Instance != null)
        {
            foreach (var gift in cfg.giftItems)
            {
                WarehouseManager.Instance.AddItem(
                    gift.itemId, gift.displayName, gift.icon, gift.amount);
                Debug.Log($"[LevelUpPopup] +{gift.amount}x {gift.displayName}");
            }
        }
    }

    // =========================================================================
    // Animations
    // =========================================================================

    private IEnumerator AnimateIn()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / fadeInDuration;
                canvasGroup.alpha = Mathf.Clamp01(t);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        if (contentPanel != null)
        {
            contentPanel.localScale = Vector3.one * 0.6f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / scaleInDuration;
                float s = EaseOutBack(Mathf.Clamp01(t));
                contentPanel.localScale = Vector3.one * s;
                yield return null;
            }
            contentPanel.localScale = Vector3.one;
        }
    }

    private IEnumerator AnimateOut(System.Action onDone)
    {
        if (canvasGroup != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.18f;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
        onDone?.Invoke();
    }

    // Easing: overshoot spring feel khi bật popup
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // =========================================================================
    // VFX
    // =========================================================================

    private void SpawnVFX()
    {
        StopVFX();

        Camera renderCamera = Camera.main;
        if (renderCamera == null)
        {
            Debug.LogWarning("[LevelUpPopupUI] Main Camera not found. Lana VFX cannot be placed from UI screen space.");
            return;
        }

        _activeVfxRoot = new GameObject("LevelUpPopup_VFX_Runtime");

        if (vfxConfettiPrefab != null)
        {
            // Pháo hoa: bắn 3 điểm phía trên (giữa + 2 góc) cho rầm rộ
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_Top",
                GetVfxScreenPoint(VfxPlacement.Top),      renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_TopLeft",
                GetVfxScreenPoint(VfxPlacement.TopLeft),  renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_TopRight",
                GetVfxScreenPoint(VfxPlacement.TopRight), renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
        }

        if (vfxSidePrefab != null)
        {
            // Lana03 hai bên — to hơn confetti
            SpawnWorldVfx(vfxSidePrefab, "LevelUp_Flash_Lana03_Left",
                GetVfxScreenPoint(VfxPlacement.Left),  renderCamera, 20f, vfxSideDemoScale * vfxSideScaleBoost);
            SpawnWorldVfx(vfxSidePrefab, "LevelUp_Flash_Lana03_Right",
                GetVfxScreenPoint(VfxPlacement.Right), renderCamera, 20f, vfxSideDemoScale * vfxSideScaleBoost);
        }

        // KHÔNG tự huỷ sau vài giây nữa — lặp "bùm bùm bùm" tới khi user bấm Nhận Quà (StopVFX dừng).
        if (_vfxLoop != null) StopCoroutine(_vfxLoop);
        _vfxLoop = StartCoroutine(VfxBurstLoop());
    }

    private enum VfxPlacement
    {
        Top,
        TopLeft,
        TopRight,
        Left,
        Right
    }

    private Vector2 GetVfxScreenPoint(VfxPlacement placement)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;

        if (contentPanel != null)
        {
            Vector3[] corners = new Vector3[4];
            contentPanel.GetWorldCorners(corners);

            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(null, corners[3]);

            if (placement == VfxPlacement.Top)
                return Vector2.Lerp(topLeft, topRight, 0.5f)
                    + Vector2.up * (vfxTopPanelGap * scaleFactor);

            if (placement == VfxPlacement.TopLeft)
                return topLeft
                    + Vector2.up   * (vfxTopPanelGap * scaleFactor)
                    + Vector2.left * (vfxSidePanelGap * 0.5f * scaleFactor);

            if (placement == VfxPlacement.TopRight)
                return topRight
                    + Vector2.up    * (vfxTopPanelGap * scaleFactor)
                    + Vector2.right * (vfxSidePanelGap * 0.5f * scaleFactor);

            Vector2 sideCenter = placement == VfxPlacement.Left
                ? Vector2.Lerp(bottomLeft, topLeft, 0.5f)
                : Vector2.Lerp(bottomRight, topRight, 0.5f);
            Vector2 horizontalOffset =
                (placement == VfxPlacement.Left ? Vector2.left : Vector2.right)
                * (vfxSidePanelGap * scaleFactor);

            return sideCenter
                + horizontalOffset
                + Vector2.up * (vfxSideVerticalOffset * scaleFactor);
        }

        Transform fallback =
            (placement == VfxPlacement.Top || placement == VfxPlacement.TopLeft || placement == VfxPlacement.TopRight)
            ? vfxSpawnPoint
            : placement == VfxPlacement.Left ? vfxLeftPoint : vfxRightPoint;
        return fallback != null
            ? RectTransformUtility.WorldToScreenPoint(null, fallback.position)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void SpawnWorldVfx(
        GameObject prefab,
        string instanceName,
        Vector2 screenPoint,
        Camera renderCamera,
        float demoOrthoSize,
        float demoScale)
    {
        float cameraDistance = renderCamera.nearClipPlane + 1f;
        Vector3 worldPosition = renderCamera.ScreenToWorldPoint(
            new Vector3(screenPoint.x, screenPoint.y, cameraDistance));

        GameObject instance = Instantiate(
            prefab,
            worldPosition,
            Quaternion.identity,
            _activeVfxRoot.transform);
        instance.name = instanceName;

        float worldScale = renderCamera.orthographic
            ? (renderCamera.orthographicSize / demoOrthoSize) * demoScale
            : demoScale;
        instance.transform.localScale = Vector3.one * worldScale;

        foreach (ParticleSystem particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            main.useUnscaledTime = true;

            // Nhiều particle hơn: nhân rate + số lượng burst
            var emission = particleSystem.emission;
            emission.rateOverTimeMultiplier *= vfxEmissionMultiplier;
            int burstCount = emission.burstCount;
            if (burstCount > 0)
            {
                var bursts = new ParticleSystem.Burst[burstCount];
                emission.GetBursts(bursts);
                for (int b = 0; b < bursts.Length; b++)
                {
                    var c = bursts[b].count;
                    c.constantMin *= vfxEmissionMultiplier;
                    c.constantMax *= vfxEmissionMultiplier;
                    bursts[b].count = c;
                }
                emission.SetBursts(bursts);
            }

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }

        int sortingOffset = 0;
        foreach (ParticleSystemRenderer particleRenderer in
                 instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            particleRenderer.sortingLayerName = "Foreground";
            particleRenderer.sortingOrder = 1000 + sortingOffset++;
        }
    }

    // Lặp bùm tới khi user bấm Nhận Quà.
    private IEnumerator VfxBurstLoop()
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.15f, vfxBurstInterval));
        while (_activeVfxRoot != null)
        {
            yield return wait;
            if (_activeVfxRoot == null) yield break;

            // Bùm lại tất cả emitter → cảm giác "bùm bùm bùm" liên tục
            foreach (ParticleSystem ps in _activeVfxRoot.GetComponentsInChildren<ParticleSystem>(true))
                ps.Play(false);
        }
    }

    private void StopVFX()
    {
        if (_vfxLoop != null) { StopCoroutine(_vfxLoop); _vfxLoop = null; }
        if (_activeVfxRoot == null) return;

        foreach (ParticleSystem particleSystem in
                 _activeVfxRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        Destroy(_activeVfxRoot);
        _activeVfxRoot = null;
    }

    // =========================================================================
    // Input Lock
    // =========================================================================

    private void AcquireInputLock()
    {
        if (!_inputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            _inputLockHeld = true;
        }
    }

    private void ReleaseInputLock()
    {
        if (_inputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            _inputLockHeld = false;
        }
    }

    // =========================================================================
    // Debug
    // =========================================================================

#if UNITY_EDITOR
    [ContextMenu("Debug: Preview Level 2 Popup")]
    private void DebugPreviewL2()
    {
        _lastKnownLevel = 1;
        HandleLevelChanged(2);
    }

    [ContextMenu("Debug: Preview Level 5 Popup (Cooking Unlock)")]
    private void DebugPreviewL5()
    {
        _lastKnownLevel = 4;
        HandleLevelChanged(5);
    }
#endif
}
