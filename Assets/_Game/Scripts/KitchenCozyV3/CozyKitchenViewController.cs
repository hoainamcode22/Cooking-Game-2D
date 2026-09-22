using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenCozyV3
{
    public enum DishCategory
    {
        All = -1,
        Main = 0,
        Side = 1,
        Soup = 2,
        Dessert = 3
    }

    /// <summary>
    /// Lightweight sprite frame animation player for UI / 2D elements (Cat Chef, Fire, Steam).
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float fps = 10f;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnAwake = true;

        private int _currentFrame = 0;
        private float _timer = 0f;
        private bool _isPlaying = false;

        public Sprite[] Frames { get => frames; set => frames = value; }
        public float Fps { get => fps; set => fps = value; }

        private void Awake()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            if (playOnAwake && frames != null && frames.Length > 0)
            {
                Play();
            }
        }

        public void Play()
        {
            _isPlaying = true;
            _timer = 0f;
            _currentFrame = 0;
            ApplyFrame();
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        private void Update()
        {
            if (!_isPlaying || frames == null || frames.Length <= 1) return;

            _timer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, fps);
            if (_timer >= interval)
            {
                _timer -= interval;
                _currentFrame++;
                if (_currentFrame >= frames.Length)
                {
                    if (loop) _currentFrame = 0;
                    else
                    {
                        _currentFrame = frames.Length - 1;
                        _isPlaying = false;
                    }
                }
                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            if (frames == null || _currentFrame < 0 || _currentFrame >= frames.Length) return;
            var sprite = frames[_currentFrame];
            if (targetImage != null) targetImage.sprite = sprite;
            if (targetRenderer != null) targetRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// UI Controller for the Cozy Kitchen V3 system.
    /// Manages direct Inspector hierarchy references with full drag-and-drop support.
    /// </summary>
    public class CozyKitchenViewController : MonoBehaviour
    {
        public static CozyKitchenViewController Instance { get; private set; }

        [Header("--- Data Sources ---")]
        [SerializeField] private ListDishData dishBookData;
        [SerializeField] private IngredientData[] allIngredientsData;

        [Header("--- Backend Managers (Auto-found if null) ---")]
        [SerializeField] private CookingChallengeManager challengeManager;
        [SerializeField] private CookingSelectionManager selectionManager;
        [SerializeField] private KitchenUIv2.DailySpecialManager dailySpecialManager;

        [Header("--- Top Bar ---")]
        [SerializeField] private Button btnBackToFarm;
        [SerializeField] private Button btnSettings;

        [Header("--- Left Panel: Recipe Book ---")]
        [SerializeField] private Button[] tabCategoryButtons; // [All], [Main], [Side], [Soup], [Dessert]
        [SerializeField] private Transform dishListContent;
        [SerializeField] private GameObject dishCardPrefab;

        [Header("--- Center Top: Selected Dish Overview ---")]
        [SerializeField] private Image imgSelectedDishPreview;
        [SerializeField] private TMP_Text txtSelectedDishName;
        [SerializeField] private TMP_Text txtSelectedDishDesc;
        [SerializeField] private Transform requiredIngredientsContainer;
        [SerializeField] private TMP_Text txtRewardGold;
        [SerializeField] private TMP_Text txtRewardExp;
        [SerializeField] private TMP_Text txtCookingTimeTimer;

        [Header("--- Right Top: Today's Special ---")]
        [SerializeField] private TMP_Text txtTodaySpecialList;

        [Header("--- Center Kitchen: Station & Appliances ---")]
        [SerializeField] private GameObject cuttingBoardObj;
        [SerializeField] private GameObject stoveGasBurnerObj;
        [SerializeField] private Image imgFoodInPan;
        [SerializeField] private SpriteFrameAnimator stoveFireAnimator;
        [SerializeField] private Button btnAutoCook;
        [SerializeField] private SpriteFrameAnimator catChefAnimator;
        [SerializeField] private TMP_Text txtCatChefTip;
        [SerializeField] private GameObject speechBubbleObj;
        [SerializeField] private GameObject stoneOvenObj;
        [SerializeField] private SpriteFrameAnimator ovenFireAnimator;
        [SerializeField] private TMP_Text txtStorageCount;

        [Header("--- Bottom Panel: Ingredients Tray ---")]
        [SerializeField] private Transform ingredientsTrayContainer;
        [SerializeField] private GameObject ingredientSlotPrefab;

        [Header("--- Bottom Right: Action Button ---")]
        [SerializeField] private Button btnCookAction;
        [SerializeField] private TMP_Text txtCookButtonLabel;

        // Runtime State
        private DishData _currentSelectedDish;
        private DishCategory _currentCategory = DishCategory.All;
        private float _cookingCountdown = 45f;
        private bool _isCookingInProgress = false;

        private void Awake()
        {
            Instance = this;
            FindManagers();
        }

        private void Start()
        {
            SetupButtonListeners();
            RefreshDailySpecials();
            PopulateRecipeBook();
            PopulateIngredientsTray();

            if (_currentSelectedDish == null && dishBookData != null && dishBookData.allDishes != null && dishBookData.allDishes.Count > 0)
            {
                SelectDish(dishBookData.allDishes[0]);
            }
        }

        private void Update()
        {
            if (_isCookingInProgress)
            {
                _cookingCountdown -= Time.deltaTime;
                if (_cookingCountdown <= 0f)
                {
                    _cookingCountdown = 0f;
                    _isCookingInProgress = false;
                    OnCookingCompleted();
                }
                UpdateCookingTimerUI();
            }
        }

        public void FindManagers()
        {
            if (challengeManager == null) challengeManager = FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
            if (selectionManager == null) selectionManager = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);
            if (dailySpecialManager == null) dailySpecialManager = FindFirstObjectByType<KitchenUIv2.DailySpecialManager>(FindObjectsInactive.Include);
        }

        private void SetupButtonListeners()
        {
            if (btnBackToFarm != null)
            {
                btnBackToFarm.onClick.RemoveAllListeners();
                btnBackToFarm.onClick.AddListener(OnClickBackToFarm);
            }

            if (btnCookAction != null)
            {
                btnCookAction.onClick.RemoveAllListeners();
                btnCookAction.onClick.AddListener(OnClickCook);
            }

            if (btnAutoCook != null)
            {
                btnAutoCook.onClick.RemoveAllListeners();
                btnAutoCook.onClick.AddListener(OnClickAutoCook);
            }

            if (tabCategoryButtons != null)
            {
                for (int i = 0; i < tabCategoryButtons.Length; i++)
                {
                    int index = i - 1; // 0 = All (-1), 1 = Main (0), etc.
                    var btn = tabCategoryButtons[i];
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnSelectCategory((DishCategory)index));
                    }
                }
            }
        }

        public void OnSelectCategory(DishCategory category)
        {
            _currentCategory = category;
            PopulateRecipeBook();
        }

        public void SelectDish(DishData dish)
        {
            _currentSelectedDish = dish;
            if (dish == null) return;

            if (txtSelectedDishName != null) txtSelectedDishName.text = dish.dishName;
            if (txtSelectedDishDesc != null) txtSelectedDishDesc.text = string.IsNullOrEmpty(dish.dishSubTitle) ? "Simple ingredients, rich flavor. A classic home-cooked dish!" : dish.dishSubTitle;
            if (imgSelectedDishPreview != null && dish.dishSprite != null) imgSelectedDishPreview.sprite = dish.dishSprite;

            if (txtRewardGold != null) txtRewardGold.text = $"+{dish.rewardGold}";
            if (txtRewardExp != null) txtRewardExp.text = $"+{dish.rewardExp}";

            _cookingCountdown = 45f;
            UpdateCookingTimerUI();
            UpdateRequiredIngredientsUI(dish);

            if (challengeManager != null)
            {
                challengeManager.SetCurrentDish(dish);
            }
        }

        private void UpdateRequiredIngredientsUI(DishData dish)
        {
            if (requiredIngredientsContainer == null || dish == null) return;

            // Clear old items or update slots
            for (int i = 0; i < requiredIngredientsContainer.childCount; i++)
            {
                var child = requiredIngredientsContainer.GetChild(i);
                if (i < dish.requiredIngredients.Count)
                {
                    child.gameObject.SetActive(true);
                    var ing = dish.requiredIngredients[i];
                    var img = child.Find("Img_Icon")?.GetComponent<Image>();
                    var txt = child.Find("Txt_Count")?.GetComponent<TMP_Text>();
                    if (img != null && ing != null) img.sprite = ing.icon;
                    if (txt != null) txt.text = "1/1";
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateCookingTimerUI()
        {
            if (txtCookingTimeTimer != null)
            {
                int mins = Mathf.FloorToInt(_cookingCountdown / 60f);
                int secs = Mathf.FloorToInt(_cookingCountdown % 60f);
                txtCookingTimeTimer.text = string.Format("{0:00}:{1:00}", mins, secs);
            }
        }

        public void PopulateRecipeBook()
        {
            if (dishBookData == null || dishListContent == null) return;
            // Recipe items population logic (demo cards or runtime data)
        }

        public void PopulateIngredientsTray()
        {
            if (ingredientsTrayContainer == null) return;
            // Ingredients tray items logic
        }

        public void RefreshDailySpecials()
        {
            if (txtTodaySpecialList == null) return;
            txtTodaySpecialList.text = "• Cabbage Salad\n• Chicken & Cabbage\n• Corn Egg Soup";
        }

        public void OnClickCook()
        {
            Debug.Log("[CozyKitchen] Start Cooking: " + (_currentSelectedDish != null ? _currentSelectedDish.dishName : "None"));
            _isCookingInProgress = true;
            _cookingCountdown = 45f;
            if (stoveFireAnimator != null) stoveFireAnimator.Play();
        }

        public void OnClickAutoCook()
        {
            Debug.Log("[CozyKitchen] Auto Cook triggered!");
        }

        public void OnClickBackToFarm()
        {
            Debug.Log("[CozyKitchen] Returning to Farm scene...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("SCN_Farm");
        }

        private void OnCookingCompleted()
        {
            Debug.Log("[CozyKitchen] Cooking completed successfully!");
            if (txtCatChefTip != null) txtCatChefTip.text = "Delicious! Dish is served! ✨";
        }
    }
}
