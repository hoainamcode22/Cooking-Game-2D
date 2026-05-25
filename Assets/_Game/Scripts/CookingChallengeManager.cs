using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;//mới

public class CookingChallengeManager : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private CenterCookingPanelUI centerCookingPanelUI;
    [SerializeField] private ScoreResultBoxUI scoreResultBoxUI;
    [SerializeField] private HintsBoxUI hintsBoxUI;
    [SerializeField] private CurrentFlavorBoxUI currentFlavorBoxUI;

    [Header("Selection")]
    [SerializeField] private CookingSelectionManager cookingSelectionManager;

    [Header("Technique")]
    [SerializeField] private bool correctTechniqueForNow = false;

    [SerializeField] private float cookSubmitDelay = 0.8f;
    [SerializeField] private int successScoreThreshold = 70;

    [Header("Mini Game")]
    [SerializeField] private CookingTimingMiniGameUI timingMiniGame;


    [Header("Dish Display After Cooking")]
    [SerializeField] private Image cookedDishDisplayImage;
    private DishData cookedDishOnPlate;// Biến này để lưu trữ món ăn đã nấu được hiển thị trên đĩa
    private DishBookUI dishBookUI;// Tham chiếu đến DishBookUI để cập nhật kho sau khi nấu xong
    private DishData currentDishData;

    private bool isCooking = false;
    [SerializeField] private GameObject failMessageText;
    [SerializeField] private float failMessageDuration = 3f;

    [Header("Check Selection Popup")]
    [SerializeField] private GameObject checkSelectionPopup;
    [SerializeField] private float checkSelectionPopupDuration = 2f;

    private bool isShowingCheckSelectionPopup;

    private bool isShowingFailMessage;
    private void Start()
    {
        RefreshCenterUI();
        RefreshHintsUI();
        RefreshPreviewScore();
    }

    private void Update()
    {
        if (!isCooking)
        {
            RefreshPreviewScore();
        }
    }

    public void RefreshCenterUI()
    {
        if (centerCookingPanelUI == null)
        {
            Debug.LogWarning("CenterCookingPanelUI is missing.");
            return;
        }

        if (currentDishData == null)
        {
            centerCookingPanelUI.ClearCenter();
            Debug.LogWarning("Current Dish Data is null.");
            return;
        }

        centerCookingPanelUI.BindDish(currentDishData);
    }

    public void RefreshHintsUI()
    {
        if (hintsBoxUI == null)
        {
            Debug.LogWarning("HintsBoxUI is missing.");
            return;
        }

        if (currentDishData == null)
        {
            hintsBoxUI.ClearUI();
            return;
        }

        hintsBoxUI.BindDish(currentDishData);
    }

    public void RefreshPreviewScore()
    {

        if (centerCookingPanelUI == null) return;
        if (currentDishData == null) return;
        if (cookingSelectionManager == null) return;

        List<SelectableIngredientCard> selectedIngredients = cookingSelectionManager.GetSelectedIngredientCards();
        List<SelectableIngredientCard> selectedSeasonings = cookingSelectionManager.GetSelectedSeasoningCards();
        if ((selectedIngredients == null || selectedIngredients.Count == 0) &&
            (selectedSeasonings == null || selectedSeasonings.Count == 0))
        {
            centerCookingPanelUI.SetCookSubmitScore(0);
            return;
        }

    }

    private void OnTimingMiniGameFinished(bool isSuccess)
    {


        if (isCooking)
        {
            Debug.Log("Already cooking. Please wait.");
            return;
        }

        if (!isSuccess)
        {
            Debug.Log("Mini game thất bại.");

            StartCoroutine(ShowFailMessageRoutine());
            List<SelectableIngredientCard> selectedIngredients = cookingSelectionManager.GetSelectedIngredientCards();
            List<SelectableIngredientCard> selectedSeasonings = cookingSelectionManager.GetSelectedSeasoningCards();

            ConsumeSelectedCookingItems(selectedIngredients, selectedSeasonings);
            cookingSelectionManager.ResetUIAfterCooking();
            cookingSelectionManager.EnableIngredientSelection();
            if (cookingSelectionManager != null)
            {
                cookingSelectionManager.ResetFlavor();
            }
             if (cookingSelectionManager != null)
            {
                cookingSelectionManager.ResetSelection();
            }
            RefreshCenterUI();

            return;
        }

        if (currentDishData == null)
        {
            Debug.LogWarning("Current dish data is missing.");
            return;
        }

        if (cookingSelectionManager == null)
        {
            Debug.LogWarning("CookingSelectionManager is missing.");
            return;
        }

        if (scoreResultBoxUI == null)
        {
            Debug.LogWarning("ScoreResultBoxUI is missing.");
            return;
        }

        StartCoroutine(CookSubmitRoutine());
    }

    public void OnClickCookSubmit()
    {
        if (isCooking)
        {
            Debug.Log("Already cooking. Please wait.");
            return;
        }

        if (isShowingFailMessage)
        {
            Debug.Log("Fail message is showing. Please wait.");
            return;
        }

        if (isShowingCheckSelectionPopup)
        {
            Debug.Log("Check selection popup is showing. Please wait.");
            return;
        }

        if (currentDishData == null)
        {
            Debug.LogWarning("Chưa chọn món ăn.");
            return;
        }

        if (!HasSelectedCookingItem())
        {
            Debug.Log("Chưa chọn nguyên liệu hoặc gia vị nào.");

            StartCoroutine(ShowCheckSelectionPopupRoutine());

            return;
        }

        if (timingMiniGame == null)
        {
            Debug.LogWarning("Timing mini game is missing.");
            return;
        }

        timingMiniGame.StartMiniGame(currentDishData.difficulty, OnTimingMiniGameFinished);
    }
    private IEnumerator CookSubmitRoutine()
    {
        isCooking = true;
        Debug.Log("Cook submit started.");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCookStart();


        yield return new WaitForSeconds(cookSubmitDelay);

        List<SelectableIngredientCard> selectedIngredients = cookingSelectionManager.GetSelectedIngredientCards();
        List<SelectableIngredientCard> selectedSeasonings = cookingSelectionManager.GetSelectedSeasoningCards();


        CookingScoreResult result = CookingScoreCalculator.Evaluate(
            currentDishData,
            selectedIngredients,
            selectedSeasonings
        );

        // TRỪ NGUYÊN LIỆU ĐÃ CHỌN SAU KHI NẤU
        ConsumeSelectedCookingItems(selectedIngredients, selectedSeasonings);
        cookedDishOnPlate = currentDishData;// Lưu trữ món ăn đã nấu được hiển thị trên đĩa

        if (centerCookingPanelUI != null)
            centerCookingPanelUI.SetCookSubmitScore(result.finalScore);


        if (AudioManager.Instance != null)
        {
            if (result.finalScore >= successScoreThreshold)
                AudioManager.Instance.PlaySuccess();
        }

        isCooking = false;
        cookingSelectionManager.DisableIngredientSelection();

        if (result.finalScore >= successScoreThreshold)
        {
            Debug.Log("Đạt điểm! Hiện popup kết quả trước.");

            if (cookingSelectionManager != null)
            {
                cookingSelectionManager.ResetFlavor();
            }

            RefreshCenterUI();

            StartCoroutine(ShowScoreResultPopupRoutine(result, true));
        }
        else
        {
            Debug.Log("Chưa đủ điểm, làm lại. " + successScoreThreshold);

            cookingSelectionManager.EnableIngredientSelection();

            if (cookingSelectionManager != null)
            {
                cookingSelectionManager.ResetFlavor();
                cookingSelectionManager.ResetSelection();
            }

            RefreshCenterUI();

            StartCoroutine(ShowScoreResultPopupRoutine(result, false));
        }

    }


    //Hàm mới do Nguyên thêm 

    private void ShowCookedDishOnPlate()//Hàm này sẽ hiển thị món ăn đã nấu lên đĩa sau khi người chơi nhấn nút Cook Submit
    {
        if (cookedDishDisplayImage == null)
        {
            Debug.LogWarning("Cooked Dish Display Image chưa được gán!");
            return;
        }

        if (currentDishData == null || currentDishData.dishSprite == null)
        {
            Debug.LogWarning("Món hiện tại chưa có sprite!");
            return;
        }

        cookedDishDisplayImage.sprite = currentDishData.dishSprite;
        cookedDishDisplayImage.gameObject.SetActive(true);
    }

    public void CollectCookedDishToWarehouse()//Hàm này sẽ được gọi khi người chơi nhấn nút "Collect" để đưa món ăn đã nấu vào kho sau khi xem điểm số và thưởng
    {
        if (cookingSelectionManager != null)
        {
            cookingSelectionManager.ResetSelection();
            cookingSelectionManager.ResetFlavor();
        }
        cookingSelectionManager.EnableIngredientSelection();

        if (cookedDishOnPlate == null)
        {
            Debug.LogWarning("[Cooking] Không có món ăn trên dĩa để đưa vào kho.");
            return;
        }

        if (FarmInventoryManager.Instance == null)
        {
            Debug.LogError("[Cooking] Không tìm thấy FarmInventoryManager.");
            return;
        }

        if (string.IsNullOrEmpty(cookedDishOnPlate.dishId))
        {
            Debug.LogError("[Cooking] dishId của món ăn đang bị trống.");
            return;
        }

        FarmInventoryManager.Instance.AddItem(cookedDishOnPlate.dishId, 1);
        int amount = FarmInventoryManager.Instance.GetAmount(cookedDishOnPlate.dishId);
         Debug.Log("[Cooking] Số lượng trong FarmInventoryManager sau khi thêm: "
              + cookedDishOnPlate.dishId + " = " + amount);

        Debug.Log("[Cooking] Đã đưa món vào kho: " + cookedDishOnPlate.dishId);

        cookedDishOnPlate = null;

        if (cookedDishDisplayImage != null)
        {
            cookedDishDisplayImage.sprite = null;
            cookedDishDisplayImage.gameObject.SetActive(false);
        }
    }
    public void SetCurrentDish(DishData dish)
    {
        if (dish == null)
        {
            Debug.LogWarning("[CookingChallengeManager] Dish truyền vào bị null.");
            return;
        }

        currentDishData = dish;

        Debug.Log("[CookingChallengeManager] Đã nhận món: " + currentDishData.dishName);

        RefreshCenterUI();
        RefreshHintsUI();
        RefreshPreviewScore();
    }
    private IEnumerator ShowFailMessageRoutine()
    {
        isShowingFailMessage = true;

        if (failMessageText != null)
        {
            failMessageText.SetActive(true);
        }

        yield return new WaitForSeconds(failMessageDuration);

        if (failMessageText != null)
        {
            failMessageText.SetActive(false);
        }

        isShowingFailMessage = false;
    }
    // Hàm mới do Nguyên thêm để trừ nguyên liệu đã chọn sau khi nấu ăn xong, bất kể thành công hay thất bại
    private void ConsumeSelectedCookingItems(
    List<SelectableIngredientCard> selectedIngredients,
    List<SelectableIngredientCard> selectedSeasonings
    )
    {
        List<string> cookedItemIds = new List<string>();

        foreach (var card in selectedIngredients)
        {
            if (card == null) continue;

            string itemId = card.GetItemId();

            if (!string.IsNullOrEmpty(itemId))
            {
                cookedItemIds.Add(itemId);
            }
        }

        foreach (var card in selectedSeasonings)
        {
            if (card == null) continue;

            string itemId = card.GetItemId();

            if (!string.IsNullOrEmpty(itemId))
            {
                cookedItemIds.Add(itemId);
            }
        }

        if (KitchenTransferManager.Instance != null)
        {
            KitchenTransferManager.Instance.SetAfterCooking(cookedItemIds);
        }
        else
        {
            Debug.LogWarning("KitchenTransferManager.Instance is missing.");
        }
    }
    private IEnumerator ShowScoreResultPopupRoutine(CookingScoreResult result, bool isSuccess)
    {
        Debug.Log("CALL SHOW SCORE RESULT POPUP | isSuccess = " + isSuccess);

        if (scoreResultBoxUI != null)
        {
            scoreResultBoxUI.ShowResult(result, isSuccess);
        }
        else
        {
            Debug.LogWarning("ScoreResultBoxUI is missing.");
        }

        yield return new WaitForSeconds(3f);

        if (scoreResultBoxUI != null)
        {
            scoreResultBoxUI.Hide();
        }

        if (isSuccess)
        {
            ShowCookedDishOnPlate();
        }
    }
    private bool HasSelectedCookingItem()
    {
        if (cookingSelectionManager == null)
        {
            Debug.LogWarning("CookingSelectionManager is missing.");
            return false;
        }

        List<SelectableIngredientCard> selectedIngredients =
            cookingSelectionManager.GetSelectedIngredientCards();

        List<SelectableIngredientCard> selectedSeasonings =
            cookingSelectionManager.GetSelectedSeasoningCards();

        if (selectedIngredients != null)
        {
            foreach (var card in selectedIngredients)
            {
                if (card != null)
                    return true;
            }
        }

        if (selectedSeasonings != null)
        {
            foreach (var card in selectedSeasonings)
            {
                if (card != null)
                    return true;
            }
        }

        return false;
    }
    private IEnumerator ShowCheckSelectionPopupRoutine()
    {
        isShowingCheckSelectionPopup = true;

        if (checkSelectionPopup != null)
        {
            checkSelectionPopup.SetActive(true);
        }

        yield return new WaitForSeconds(checkSelectionPopupDuration);

        if (checkSelectionPopup != null)
        {
            checkSelectionPopup.SetActive(false);
        }

        isShowingCheckSelectionPopup = false;
    }
}