using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.UI
{
    /// <summary>
    /// Điều khiển toàn bộ HUD chính (Township HUD) theo thiết kế mới:
    /// - Cụm Top-Left: Khung Avatar bo góc độc lập + Thanh EXP Bar (Nền tối + Fill xanh + Ngôi sao cấp độ).
    /// - Cụm Top-Right: 2 Capsule Vàng & Kim Cương + Nút Cài Đặt.
    /// - Cụm Bottom-Left: 4 Tab Điều Hướng (CỬA HÀNG, KHO, NHIỆM VỤ, BẢN ĐỒ).
    /// </summary>
    public class TownshipHUDController : MonoBehaviour
    {
        public static TownshipHUDController Instance { get; private set; }

        [Header("── Top-Left: Avatar ──")]
        public Button btnAvatar;
        public Image imgAvatar;

        [Header("── Top-Left: EXP & Level ──")]
        public Image imgExpFill;
        public TMP_Text txtExp;
        public TMP_Text txtLevel;

        [Header("── Top-Right: Currencies ──")]
        public TMP_Text txtGold;
        public TMP_Text txtDiamond;
        public Button btnAddGold;
        public Button btnAddDiamond;

        [Header("── Top-Right: Settings ──")]
        public Button btnSettings;

        [Header("── Left Side: Mission Button & Quick Widget (Chấm đỏ) ──")]
        public Button btnMission;
        public GameObject goMissionBadge;
        public GameObject goMissionWidget;
        public Image imgMissionItem;
        public TMP_Text txtMissionTitle;
        public TMP_Text txtMissionDesc;
        public Image imgMissionProgressFill;
        public TMP_Text txtMissionProgress;
        public Button btnMissionGo;

        [Header("── Bottom-Left: Navigation Tabs ──")]
        public Button btnTabShop;
        public Button btnTabWarehouse;
        public Button btnTabMarket;
        public Button btnTabCooking;
        [HideInInspector] public Button btnTabMission;
        [HideInInspector] public Button btnTabMap;

        private static readonly CultureInfo VnCulture = new CultureInfo("vi-VN");

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
            if (goMissionWidget != null)
                goMissionWidget.SetActive(false);

            if (goMissionBadge != null)
                goMissionBadge.SetActive(true);

            SetupButtonListeners();
            SubscribeEvents();
            RefreshAllUI();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        // ── Gán sự kiện Click ──────────────────────────────────────────────────

        private void SetupButtonListeners()
        {
            if (btnAvatar != null)
            {
                btnAvatar.onClick.RemoveAllListeners();
                btnAvatar.onClick.AddListener(OnAvatarClicked);
            }

            if (btnMission != null)
            {
                btnMission.onClick.RemoveAllListeners();
                btnMission.onClick.AddListener(OnMissionButtonClicked);
            }

            if (btnMissionGo != null)
            {
                btnMissionGo.onClick.RemoveAllListeners();
                btnMissionGo.onClick.AddListener(OnMissionGoClicked);
            }

            if (btnTabShop != null)
            {
                btnTabShop.onClick.RemoveAllListeners();
                btnTabShop.onClick.AddListener(OnShopClicked);
            }

            if (btnTabWarehouse != null)
            {
                btnTabWarehouse.onClick.RemoveAllListeners();
                btnTabWarehouse.onClick.AddListener(OnWarehouseClicked);
            }

            if (btnTabMarket != null)
            {
                btnTabMarket.onClick.RemoveAllListeners();
                btnTabMarket.onClick.AddListener(OnMarketClicked);
            }

            if (btnTabCooking != null)
            {
                btnTabCooking.onClick.RemoveAllListeners();
                btnTabCooking.onClick.AddListener(OnCookingClicked);
            }

            if (btnTabMission != null)
            {
                btnTabMission.onClick.RemoveAllListeners();
                btnTabMission.onClick.AddListener(OnMissionButtonClicked);
            }

            if (btnTabMap != null)
            {
                btnTabMap.onClick.RemoveAllListeners();
                btnTabMap.onClick.AddListener(OnCookingClicked);
            }

            if (btnAddGold != null)
            {
                btnAddGold.onClick.RemoveAllListeners();
                btnAddGold.onClick.AddListener(OnShopClicked);
            }

            if (btnAddDiamond != null)
            {
                btnAddDiamond.onClick.RemoveAllListeners();
                btnAddDiamond.onClick.AddListener(OnShopClicked);
            }

            if (btnSettings != null)
            {
                btnSettings.onClick.RemoveAllListeners();
                btnSettings.onClick.AddListener(OnSettingsClicked);
            }
        }

        // ── Lắng nghe sự kiện dữ liệu ──────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (FarmEconomyManager.Instance != null)
            {
                FarmEconomyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
            }

            if (PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.OnExpChanged += OnExpChanged;
                PlayerProgressManager.Instance.OnLevelChanged += OnLevelChanged;
            }

            if (FarmLevelManager.Instance != null)
            {
                FarmLevelManager.Instance.OnLevelChanged += OnLevelChanged;
            }

            AvatarProfilePopupUI.OnAvatarSelected += HandleAvatarChanged;
        }

        private void UnsubscribeEvents()
        {
            if (FarmEconomyManager.Instance != null)
            {
                FarmEconomyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
            }

            if (PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.OnExpChanged -= OnExpChanged;
                PlayerProgressManager.Instance.OnLevelChanged -= OnLevelChanged;
            }

            if (FarmLevelManager.Instance != null)
            {
                FarmLevelManager.Instance.OnLevelChanged -= OnLevelChanged;
            }

            AvatarProfilePopupUI.OnAvatarSelected -= HandleAvatarChanged;
        }

        // ── Xử lý Cập nhật UI ──────────────────────────────────────────────────

        public void RefreshAllUI()
        {
            // 1. Tiền tệ
            if (FarmEconomyManager.Instance != null)
            {
                UpdateCurrency(FarmEconomyManager.Instance.Gold, FarmEconomyManager.Instance.Gems);
            }

            // 2. Cấp độ & EXP
            int currentLevel = 1;
            int currentExp = 0;
            int requiredExp = 100;

            if (PlayerProgressManager.Instance != null)
            {
                currentLevel = PlayerProgressManager.Instance.Level;
                currentExp = PlayerProgressManager.Instance.CurrentExp;
                requiredExp = PlayerProgressManager.Instance.RequiredExpForLevel(currentLevel);
            }
            else if (FarmLevelManager.Instance != null)
            {
                currentLevel = FarmLevelManager.Instance.CurrentLevel;
            }

            UpdateLevel(currentLevel);
            UpdateExp(currentExp, requiredExp);

            // 3. Avatar người chơi
            RefreshAvatar();
        }

        public void RefreshAvatar(int index = -1)
        {
            if (imgAvatar == null) return;
            if (index < 0) index = PlayerPrefs.GetInt("PLAYER_PROFILE_AVATAR_INDEX", 0);
            Sprite spr = Resources.Load<Sprite>($"Avatars/avatar_npc_{index}");
#if UNITY_EDITOR
            if (spr == null)
                spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Avatars/avatar_npc_{index}.png");
#endif
            if (spr != null) imgAvatar.sprite = spr;
        }

        private void HandleAvatarChanged(int index)
        {
            RefreshAvatar(index);
        }

        private void OnCurrencyChanged(int gold, int gems)
        {
            UpdateCurrency(gold, gems);
        }

        private void UpdateCurrency(int gold, int gems)
        {
            if (txtGold != null)
                txtGold.text = gold.ToString("N0", VnCulture).Replace(",", " ");

            if (txtDiamond != null)
                txtDiamond.text = gems.ToString("N0", VnCulture).Replace(",", " ");
        }

        private void OnExpChanged(int cur, int req)
        {
            UpdateExp(cur, req);
        }

        private void UpdateExp(int cur, int req)
        {
            if (req <= 0) req = 1;
            float ratio = Mathf.Clamp01((float)cur / req);

            if (imgExpFill != null)
                imgExpFill.fillAmount = ratio;

            if (txtExp != null)
                txtExp.text = $"{cur:N0} / {req:N0}".Replace(",", " ");
        }

        private void OnLevelChanged(int level)
        {
            UpdateLevel(level);
        }

        private void UpdateLevel(int level)
        {
            if (txtLevel != null)
                txtLevel.text = level.ToString();
        }

        // ── Xử lý Click Nút ────────────────────────────────────────────────────

        private void OnAvatarClicked()
        {
            var avatarPopup = Object.FindFirstObjectByType<AvatarProfilePopupUI>(FindObjectsInactive.Include);
            if (avatarPopup != null)
            {
                avatarPopup.OpenPopup();
            }
            else
            {
                Debug.Log("[TownshipHUD] Mở Popup Hồ Sơ / Avatar!");
            }
        }

        private void OnShopClicked()
        {
            if (goMissionWidget != null) goMissionWidget.SetActive(false);

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OpenShop();
            }
            else
            {
                var shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
                if (shop != null) shop.OpenShop();
            }
        }

        private void OnWarehouseClicked()
        {
            if (goMissionWidget != null) goMissionWidget.SetActive(false);

            var wh = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
            if (wh != null)
            {
                wh.OpenPopup();
            }
            else
            {
                Debug.LogWarning("[TownshipHUD] Không tìm thấy WarehousePopupUI trong Scene!");
            }
        }

        private void OnMarketClicked()
        {
            if (goMissionWidget != null) goMissionWidget.SetActive(false);

            // Mở Bảng Tin Chợ (OrderBoard / Stall)
            var orderBoard = Object.FindFirstObjectByType<OrderBoardPopupUI>(FindObjectsInactive.Include);
            if (orderBoard != null)
            {
                orderBoard.OpenPopup();
                return;
            }

            var stall = Object.FindFirstObjectByType<StallPopupUI>(FindObjectsInactive.Include);
            if (stall != null)
            {
                stall.OpenPopup();
                return;
            }

            Debug.Log("[TownshipHUD] Mở Bảng Tin Chợ...");
        }

        private void OnMissionButtonClicked()
        {
            if (goMissionWidget != null)
            {
                goMissionWidget.SetActive(!goMissionWidget.activeSelf);
            }
            else
            {
                UnifiedTaskPopupUI.OpenMission();
            }
        }

        private void OnMissionGoClicked()
        {
            if (goMissionWidget != null)
                goMissionWidget.SetActive(false);

            UnifiedTaskPopupUI.OpenMission();
        }

        private void OnMissionClicked()
        {
            OnMissionButtonClicked();
        }

        private void OnCookingClicked()
        {
            if (goMissionWidget != null) goMissionWidget.SetActive(false);

            // Chuyển sang Cooking Scene
            Debug.Log("[TownshipHUD] Chuyển sang Nấu Ăn (Cooking Scene)...");
            if (FarmUIManager.Instance != null)
            {
                FarmUIManager.Instance.OnClick_GoCooking();
            }
            else if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadScene("SampleScene", SceneTransitionManager.TransitionType.CloudWipe, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
            else if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene("SampleScene");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
        }

        private void OnMapClicked()
        {
            OnCookingClicked();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[TownshipHUD] Mở Cài Đặt...");
        }
    }
}
