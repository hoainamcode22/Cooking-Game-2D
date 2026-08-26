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


    private DishData cookedDishOnPlate;
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

        if (!FarmInventoryManager.Instance.AddItem(cookedDishOnPlate.dishId, 1))
        {
            // TESTER-F8 — LỖI MẤT ĐỒ NGƯỜI CHƠI.
            // F8 làm AddItem TỪ CHỐI loại mới khi kho hết slot. Bản cũ bỏ qua giá trị trả
            // về rồi vẫn `cookedDishOnPlate = null` + `HideCookedDish()` ở dưới ⇒ món ăn
            // BỐC HƠI dù nguyên liệu đã bị trừ (Phở bò tái = 310 vàng nguyên liệu).
            // DEV-B đã chặn đúng cách ở 3 chỗ khác (PlotController.Harvest,
            // PenMiniPanelUI.TryHarvest, TrainManager.CollectReward) nhưng bỏ sót chỗ này.
            // GIỮ món trên dĩa: người chơi dọn kho rồi bấm lại là nhận được.
            Debug.LogWarning($"[Cooking] Kho đầy — chưa đưa '{cookedDishOnPlate.dishId}' vào kho. " +
                             $"Món vẫn còn trên dĩa, bán bớt hoặc nâng cấp kho rồi bấm lại.");
            FarmUIManager.Instance?.ShowHint("Kho đầy — bán bớt hoặc nâng cấp kho rồi nhận món.");
            return;
        }

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
    /// <summary>
    /// Hệ số nhân thưởng theo điểm: đạt vừa đủ ngưỡng ăn 100%, nấu hoàn hảo ăn 150%.
    ///
    /// VÌ SAO nội suy thẳng thay vì chia bậc: chia bậc thì người chơi hơn 1 điểm mà nhảy
    /// hẳn một bậc thưởng — cảm giác như xổ số. Nội suy thì cố gắng thêm bao nhiêu được
    /// trả bấy nhiêu. Kẹp lại [1.0 , 1.5] để điểm dưới ngưỡng (không thể tới đây) hoặc
    /// điểm vượt 100 (không thể xảy ra) cũng không sinh số lạ.
    /// </summary>
    private float TinhHeSoThuongTheoDiem(int finalScore)
    {
        int nguong = successScoreThreshold;
        if (finalScore <= nguong) return 1f;

        // Khoảng cách từ ngưỡng tới điểm tối đa. Bảo vệ chia cho 0 nếu ai đặt ngưỡng = 100.
        int daiDiem = 100 - nguong;
        if (daiDiem <= 0) return 1f;

        float t = Mathf.Clamp01((finalScore - nguong) / (float)daiDiem);
        return Mathf.Lerp(1f, 1.5f, t);
    }

    private IEnumerator HandleCookingSuccess(CookingScoreResult result)
    {
        cookedDishOnPlate = currentDishData;

        // ── THƯỞNG THEO ĐỘ KHÓ × HỆ SỐ ĐIỂM (A5) ──
        // Trước đây cộng CỨNG 20 EXP và 0 vàng cho MỌI món: nấu "Phở bò tái" (5 nguyên
        // liệu, cấp 9, cần thịt bò từ chuồng cấp 7) ăn đúng bằng "Khoai tây chiên"
        // (1 nguyên liệu, cấp 5) ⇒ không ai có lý do nấu món khó.
        // Số gốc nằm trên từng `DishData` (rewardExp / rewardGold) để người cân bằng game
        // sửa được mà không phải mở code.
        float heSo = TinhHeSoThuongTheoDiem(result.finalScore);

        int expNhan = currentDishData != null
            ? Mathf.CeilToInt(currentDishData.rewardExp * heSo)
            : 0;
        int vangNhan = currentDishData != null
            ? Mathf.CeilToInt(currentDishData.rewardGold * heSo)
            : 0;

        if (expNhan > 0 && PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.AddExp(expNhan);

        if (vangNhan > 0 && FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.AddGold(vangNhan);

        Debug.Log($"[Cooking] '{(currentDishData != null ? currentDishData.dishId : "?")}' " +
                  $"{result.finalScore}đ × {heSo:0.00} → +{expNhan} EXP, +{vangNhan} vàng.");

        // Tiến độ nhiệm vụ nấu ăn
        MissionProgressTracker.ReportEvent(MissionEventType.CookDish,
            currentDishData != null ? currentDishData.dishId : "", 1);

        // Trước đây còn một lời gọi `QuestManager.Instance.OnItemCooked(...)` ở đây.
        // `QuestManager` là hệ nhiệm vụ THỨ HAI, chết hoàn toàn (không có instance trong
        // scene nào, `CheckQuestCompletion` còn ghi `// TODO: Give rewards`) nên đã xoá
        // sạch ở C8. `MissionProgressTracker.ReportEvent` ngay trên là hệ còn sống.

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
