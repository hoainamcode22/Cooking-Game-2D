using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;//mới

public class CookingChallengeManager : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private CenterCookingPanelUI centerCookingPanelUI;
    [SerializeField] private HintsBoxUI hintsBoxUI;

    [Header("Selection")]
    [SerializeField] private CookingSelectionManager cookingSelectionManager;


    [SerializeField] private float cookSubmitDelay = 0.8f;
    [SerializeField] private int successScoreThreshold = 70;

    [Header("Mini Game")]
    [SerializeField] private CookingTimingMiniGameUI timingMiniGame;


    private DishData cookedDishOnPlate;// Biến này để lưu trữ món ăn đã nấu được hiển thị trên đĩa
    private DishData currentDishData;

    private bool isCooking = false;
    [Header("Letter Mini Game")]
    [SerializeField] private LetterMiniGame letterMiniGame;
    [SerializeField] private CookingBoot cookingBoot;

    [SerializeField] private CookingEffectController cookingEffectController;
    [SerializeField] private CookingPopupController cookingPopupController;
    [SerializeField] private CookingItemConsumer cookingItemConsumer;
    [SerializeField] private DeliveryCharacterMover deliveryCharacterMover;


    private void Start()
    {
        RefreshCenterUI();
        RefreshHintsUI();
        if (centerCookingPanelUI != null)
                centerCookingPanelUI.SetCookSubmitScore(0);
    }
    public void RefreshCenterUI()
    {
        Debug.Log("[CookingChallengeManager] RefreshCenterUI được gọi.");
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
        Debug.Log("[CookingChallengeManager] RefreshCenterUI với món: " + currentDishData.dishName);

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
// Các hàm liên quan đến mini game
    private void OnTimingMiniGameFinished(bool isSuccess)
    {
        OnCookingMiniGameFinished(isSuccess);
    }
    private void OnLetterMiniGameFinished(bool isSuccess)
    {
        OnCookingMiniGameFinished(isSuccess);
    }

    private void OnCookingMiniGameFinished(bool isSuccess)
    {
        if (isCooking)
        {
            Debug.Log("Already cooking. Please wait.");
            return;
        }
        if (!isSuccess)
        {
            Debug.Log("Mini game thất bại.");
            cookingPopupController.ShowFailMessage();
            if (cookingItemConsumer != null)
            {
                cookingItemConsumer.ConsumeSelectedCookingItems();
            }
            ResetCookingSelectionState();
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
        if (cookingPopupController == null)
        {
            Debug.LogWarning("CookingPopupController is missing.");
            return;
        }

        StartCoroutine(CookSubmitRoutine());
    }

    public void OnClickCookSubmit()
    {
        if (!CanStartCooking())
            return;

        Debug.Log("OnClickCookSubmit không chạy được vì điều kiện chưa được đáp ứng.");
        StartRandomMiniGame();
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
        if (cookingItemConsumer != null)
        {
            cookingItemConsumer.ConsumeSelectedCookingItems();
        }

        if (AudioManager.Instance != null)
        {
            if (result.finalScore >= successScoreThreshold)
                AudioManager.Instance.PlaySuccess();
        }

        Debug.Log("Final Score = " + result.finalScore);

        isCooking = false;
        cookingSelectionManager.DisableIngredientSelection();

        if (result.finalScore >= successScoreThreshold)
        {
            yield return StartCoroutine(HandleCookingSuccess(result));
        }
        else
        {
            yield return StartCoroutine(HandleCookingFailed(result));
        }

        ResetCookingSelectionState();

    }

    public void CollectCookedDishToWarehouse()
    {
        if (cookedDishOnPlate == null)
        {
            Debug.LogWarning("[Cooking] Không có món ăn trên dĩa để đưa vào kho.");
            return;
        }

        if (string.IsNullOrEmpty(cookedDishOnPlate.dishId))
        {
            Debug.LogError("[Cooking] dishId của món ăn đang bị trống.");
            return;
        }
        if (deliveryCharacterMover != null)
        {
            deliveryCharacterMover.ShowDeliveryOnly();
        }

        FarmInventoryManager.Instance.AddItem(cookedDishOnPlate.dishId, 1);
        if (deliveryCharacterMover != null)
        {
            deliveryCharacterMover.MoveFromCookingToWarehouse();
        }

        Debug.Log("[Cooking] Đã đưa món vào kho: " + cookedDishOnPlate.dishId);


        cookedDishOnPlate = null;
        if (centerCookingPanelUI != null)
            centerCookingPanelUI.SetCookSubmitScore(0);

        if (cookingEffectController != null)
        {
            cookingEffectController.HideCookedDish();
        }
        ResetCookingSelectionState();
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

    }

    private bool HasSelectedCookingItem()// Hàm này kiểm tra xem người chơi đã chọn nguyên liệu hoặc gia vị nào chưa trước khi nấu
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



///Các hàm con
/// Hàm này kiểm tra xem có thể bắt đầu nấu ăn hay không dựa trên các điều kiện như đang nấu ăn, đang hiển thị thông báo thất bại, đang hiển thị popup kiểm tra lựa chọn, đã chọn món ăn chưa, đã chọn nguyên liệu hoặc gia vị chưa, và mini game đã sẵn sàng chưa.
    private bool CanStartCooking()
    {
        if (isCooking)
        {
            Debug.Log("Already cooking. Please wait.");
            return false;
        }

        if (cookingPopupController != null && cookingPopupController.IsShowingFailMessage)
        {
            Debug.Log("Fail message is showing. Please wait.");
            return false;
        }

        if (cookingPopupController != null && cookingPopupController.IsShowingCheckSelectionPopup)
        {
            Debug.Log("Check selection popup is showing. Please wait.");
            return false;
        }

        if (currentDishData == null)
        {
            Debug.LogWarning("Chưa chọn món ăn.");
            return false;
        }

        if (!HasSelectedCookingItem())
        {
            Debug.Log("Chưa chọn nguyên liệu hoặc gia vị nào.");
            if (cookingPopupController != null)
            {
                cookingPopupController.ShowCheckSelectionPopup();
            }
            return false;
        }

        if (timingMiniGame == null || letterMiniGame == null)
        {
            Debug.LogWarning("Mini game is missing.");
            return false;
        }

        return true;
    }
    private void StartRandomMiniGame()
    {
        int randomMiniGame = UnityEngine.Random.Range(0, 2);

        if (randomMiniGame == 0)
        {
            timingMiniGame.StartMiniGame(currentDishData.difficulty, OnTimingMiniGameFinished);
        }
        else
        {
            letterMiniGame.StartMiniGame(currentDishData.difficulty, OnLetterMiniGameFinished);
        }
    }

    //
    private IEnumerator HandleCookingSuccess(CookingScoreResult result)
    {
        cookedDishOnPlate = currentDishData;
        Debug.Log("Đạt điểm! Hiện popup kết quả trước.");

        if (cookingPopupController != null)
        {
            cookingPopupController.ShowScoreResultPopup(result, true, currentDishData);
        }

        if (centerCookingPanelUI != null)
        {
            centerCookingPanelUI.SetCookSubmitScore(result.finalScore);
            yield return new WaitForSeconds(5f);
            centerCookingPanelUI.SetCookSubmitScore(0);
        }
    }

    private IEnumerator HandleCookingFailed(CookingScoreResult result)
    {
        Debug.Log("Chưa đủ điểm, làm lại. " + successScoreThreshold);

        cookingSelectionManager.EnableIngredientSelection();

        cookingPopupController.ShowScoreResultPopup(result, false, currentDishData);

        if (centerCookingPanelUI != null)
        {
            centerCookingPanelUI.SetCookSubmitScore(result.finalScore);
            yield return new WaitForSeconds(5f);
            centerCookingPanelUI.SetCookSubmitScore(0);
        }
    }

    private void ResetCookingSelectionState()
    {
        if (cookingBoot != null)
        {
            cookingBoot.RefreshTransferredItemCards();
        }

        if (cookingSelectionManager != null)
        {
            cookingSelectionManager.ResetSelection();
            cookingSelectionManager.ResetFlavor();
            cookingSelectionManager.EnableIngredientSelection();
        }
    }
}