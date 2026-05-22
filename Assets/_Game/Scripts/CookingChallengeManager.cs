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

    [Header("Selection")]
    [SerializeField] private CookingSelectionManager cookingSelectionManager;

    [Header("Technique")]
    [SerializeField] private bool correctTechniqueForNow = false;

    [Header("FX")]
    [SerializeField] private CookingFX cookingFX;
    [SerializeField] private float cookSubmitDelay = 0.8f;
    [SerializeField] private int successScoreThreshold = 70;


    [Header("Dish Display After Cooking")]
    [SerializeField] private Image cookedDishDisplayImage;
    private DishData cookedDishOnPlate;// Biến này để lưu trữ món ăn đã nấu được hiển thị trên đĩa
    private DishBookUI dishBookUI;// Tham chiếu đến DishBookUI để cập nhật kho sau khi nấu xong
    private DishData currentDishData;

    private bool isCooking = false;

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
        CookingScoreResult previewResult = CookingScoreCalculator.Evaluate(
            currentDishData,
            selectedIngredients,
            selectedSeasonings
        );

        centerCookingPanelUI.SetCookSubmitScore(previewResult.finalScore);
    }

    public void OnClickCookSubmit()
    {
        if (isCooking)
        {
            Debug.Log("Already cooking. Please wait.");
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
    public void OnClickClaimReward()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCoinReward();

        Debug.Log("Claim Reward clicked.");
    }

    private IEnumerator CookSubmitRoutine()
    {
        isCooking = true;
        Debug.Log("Cook submit started.");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCookStart();

        if (cookingFX != null)
            cookingFX.PlayCookFX();

        yield return new WaitForSeconds(cookSubmitDelay);

        List<SelectableIngredientCard> selectedIngredients = cookingSelectionManager.GetSelectedIngredientCards();
        List<SelectableIngredientCard> selectedSeasonings = cookingSelectionManager.GetSelectedSeasoningCards();


        CookingScoreResult result = CookingScoreCalculator.Evaluate(
            currentDishData,
            selectedIngredients,
            selectedSeasonings
        );

        // TRỪ NGUYÊN LIỆU ĐÃ CHỌN SAU KHI NẤU
        List<string> cookedItemIds = new List<string>();

        foreach (var card in selectedIngredients)
        {
            if (card != null)
                cookedItemIds.Add(card.GetItemId());
        }

        foreach (var card in selectedSeasonings)
        {
            if (card != null)
                cookedItemIds.Add(card.GetItemId());
        }

        if (KitchenTransferManager.Instance != null)
        {
            KitchenTransferManager.Instance.SetAfterCooking(cookedItemIds);
        }
        // scoreResultBoxUI.ShowResult(result);
        cookedDishOnPlate = currentDishData;// Lưu trữ món ăn đã nấu được hiển thị trên đĩa
        ShowCookedDishOnPlate();// Hiển thị món ăn đã nấu lên đĩa

        if (centerCookingPanelUI != null)
            centerCookingPanelUI.SetCookSubmitScore(result.finalScore);

        if (cookingFX != null)
            cookingFX.PlayResultFX();

        if (AudioManager.Instance != null)
        {
            if (result.finalScore >= successScoreThreshold)
                AudioManager.Instance.PlaySuccess();
        }

        Debug.Log("=== COOK SUBMIT RESULT ===");
        Debug.Log("Ingredient Vector: " + result.ingredientVector);
        Debug.Log("Seasoning Vector: " + result.seasoningVector);
        Debug.Log("Total Vector: " + result.totalVector);
        Debug.Log("Ingredient Score: " + result.ingredientScore);
        Debug.Log("Seasoning Score: " + result.seasoningScore);
        Debug.Log("Base Score: " + result.baseScore);
        Debug.Log("Rare Bonus: " + result.rareBonus);
        Debug.Log("Technique Bonus: " + result.techniqueBonus);
        Debug.Log("Final Score: " + result.finalScore);
        Debug.Log("Reward: Gold +" + result.goldReward + ", Gems +" + result.gemReward + ", Rank +" + result.rankPointReward);
      

       isCooking = false;
       cookingSelectionManager.ResetUIAfterCooking();
       if (result.finalScore >= successScoreThreshold)
        {
            Debug.Log("Đạt điểm! Qua món mới.");

            if (cookingSelectionManager != null)
            {
                cookingSelectionManager.ResetSelection();
            }


            if (centerCookingPanelUI != null)
            {
                centerCookingPanelUI.SetCookSubmitScore(0);
            }
        }
        else
        {
            Debug.Log("Chưa đủ điểm, làm lại."+successScoreThreshold);
            RefreshCenterUI();
        }
       
        // yield return new WaitForSeconds(1.2f);
        // NextDish();
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

}