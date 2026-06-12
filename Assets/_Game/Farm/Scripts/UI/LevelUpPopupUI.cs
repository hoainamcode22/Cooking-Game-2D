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

        ReleaseInputLock();
    }

    // =========================================================================
    // Event Handler
    // =========================================================================

    private void HandleLevelChanged(int newLevel)
    {
        // Bỏ qua lần gọi đầu tiên khi Start() đồng bộ UI (không phải lên cấp thật)
        if (newLevel <= _lastKnownLevel) return;

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
        StartCoroutine(AnimateOut(() =>
        {
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
        if (vfxConfettiPrefab != null)
        {
            Vector3 pos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            Destroy(Instantiate(vfxConfettiPrefab, pos, Quaternion.identity), 4f);
        }

        if (vfxSidePrefab != null)
        {
            if (vfxLeftPoint  != null) Destroy(Instantiate(vfxSidePrefab, vfxLeftPoint.position,  Quaternion.identity), 4f);
            if (vfxRightPoint != null) Destroy(Instantiate(vfxSidePrefab, vfxRightPoint.position, Quaternion.identity), 4f);
        }
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
