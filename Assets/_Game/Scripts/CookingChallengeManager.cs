using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;//má»›i

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


    private DishData cookedDishOnPlate;// Biáº¿n nÃ y Ä‘á»ƒ lÆ°u trá»¯ mÃ³n Äƒn Ä‘Ã£ náº¥u Ä‘Æ°á»£c hiá»ƒn thá»‹ trÃªn Ä‘Ä©a
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
// CÃ¡c hÃ m liÃªn quan Ä‘áº¿n mini game
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
            return;
        }
        if (!isSuccess)
        {
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

        StartRandomMiniGame();
    }
    private IEnumerator CookSubmitRoutine()
    {
        isCooking = true;
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

        // TRá»ª NGUYÃŠN LIá»†U ÄÃƒ CHá»ŒN SAU KHI Náº¤U
        if (cookingItemConsumer != null)
        {
            cookingItemConsumer.ConsumeSelectedCookingItems();
        }

        if (AudioManager.Instance != null)
        {
            if (result.finalScore >= successScoreThreshold)
                AudioManager.Instance.PlaySuccess();
        }


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
            Debug.LogWarning("[Cooking] KhÃ´ng cÃ³ mÃ³n Äƒn trÃªn dÄ©a Ä‘á»ƒ Ä‘Æ°a vÃ o kho.");
            return;
        }

        if (string.IsNullOrEmpty(cookedDishOnPlate.dishId))
        {
            Debug.LogError("[Cooking] dishId cá»§a mÃ³n Äƒn Ä‘ang bá»‹ trá»‘ng.");
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
            Debug.LogWarning("[CookingChallengeManager] Dish truyá»n vÃ o bá»‹ null.");
            return;
        }
 
        currentDishData = dish;


        RefreshCenterUI();
        RefreshHintsUI();

    }

    private bool HasSelectedCookingItem()// HÃ m nÃ y kiá»ƒm tra xem ngÆ°á»i chÆ¡i Ä‘Ã£ chá»n nguyÃªn liá»‡u hoáº·c gia vá»‹ nÃ o chÆ°a trÆ°á»›c khi náº¥u
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



///CÃ¡c hÃ m con
/// HÃ m nÃ y kiá»ƒm tra xem cÃ³ thá»ƒ báº¯t Ä‘áº§u náº¥u Äƒn hay khÃ´ng dá»±a trÃªn cÃ¡c Ä‘iá»u kiá»‡n nhÆ° Ä‘ang náº¥u Äƒn, Ä‘ang hiá»ƒn thá»‹ thÃ´ng bÃ¡o tháº¥t báº¡i, Ä‘ang hiá»ƒn thá»‹ popup kiá»ƒm tra lá»±a chá»n, Ä‘Ã£ chá»n mÃ³n Äƒn chÆ°a, Ä‘Ã£ chá»n nguyÃªn liá»‡u hoáº·c gia vá»‹ chÆ°a, vÃ  mini game Ä‘Ã£ sáºµn sÃ ng chÆ°a.
    private bool CanStartCooking()
    {
        if (isCooking)
        {
            return false;
        }

        if (cookingPopupController != null && cookingPopupController.IsShowingFailMessage)
        {
            return false;
        }

        if (cookingPopupController != null && cookingPopupController.IsShowingCheckSelectionPopup)
        {
            return false;
        }

        if (currentDishData == null)
        {
            Debug.LogWarning("ChÆ°a chá»n mÃ³n Äƒn.");
            return false;
        }

        if (!HasSelectedCookingItem())
        {
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

        // Cộng EXP khi nấu thành công (chạy đúng 1 lần cho mỗi lần nấu thành công).
        // 8 → 20: nấu ăn nhiều bước/lâu hơn trồng cây nên EXP cao hơn (cân bằng cấp 1-100).
        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.AddExp(20);

        // Tiến độ nhiệm vụ nấu ăn
        MissionProgressTracker.ReportEvent(MissionEventType.CookDish,
            currentDishData != null ? currentDishData.dishId : "", 1);

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
