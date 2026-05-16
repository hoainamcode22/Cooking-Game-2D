using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingChallengeManager : MonoBehaviour
{
    [Header("Current Dish")]
    [SerializeField] private DishData currentDishData;

    [Header("UI")]
    [SerializeField] private CookingUIManager uiManager;
    [SerializeField] private HintsBoxUI       hintsBoxUI;

    [Header("Selection")]
    [SerializeField] private CookingSelectionManager cookingSelectionManager;

    [Header("Technique")]
    [SerializeField] private bool correctTechniqueForNow = false;

    [Header("FX")]
    [SerializeField] private CookingFX cookingFX;
    [SerializeField] private float     cookSubmitDelay       = 0.8f;
    [SerializeField] private int       successScoreThreshold = 80;

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
            RefreshPreviewScore();
    }

    public void RefreshCenterUI()
    {
        if (uiManager == null) { Debug.LogWarning("[CookingChallengeManager] CookingUIManager is missing."); return; }

        if (currentDishData == null) { uiManager.ClearAll(); Debug.LogWarning("[CookingChallengeManager] DishData is null."); return; }

        uiManager.BindDish(currentDishData);
    }

    public void RefreshHintsUI()
    {
        if (hintsBoxUI == null) { Debug.LogWarning("[CookingChallengeManager] HintsBoxUI is missing."); return; }

        if (currentDishData == null) { hintsBoxUI.ClearUI(); return; }

        hintsBoxUI.BindDish(currentDishData);
    }

    public void RefreshPreviewScore()
    {
        if (uiManager == null || currentDishData == null || cookingSelectionManager == null) return;

        CookingScoreResult preview = CookingScoreCalculator.Evaluate(
            currentDishData,
            cookingSelectionManager.GetSelectedIngredientCards(),
            cookingSelectionManager.GetSelectedSeasoningCards(),
            correctTechniqueForNow
        );

        uiManager.SetPreviewScore(preview.finalScore);
    }

    public void OnClickCookSubmit()
    {
        if (isCooking)                  { Debug.Log("[CookingChallengeManager] Already cooking."); return; }
        if (currentDishData == null)    { Debug.LogWarning("[CookingChallengeManager] DishData missing."); return; }
        if (cookingSelectionManager == null) { Debug.LogWarning("[CookingChallengeManager] SelectionManager missing."); return; }
        if (uiManager == null)          { Debug.LogWarning("[CookingChallengeManager] CookingUIManager missing."); return; }

        StartCoroutine(CookSubmitRoutine());
    }

    public void OnClickClaimReward()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCoinReward();

        Debug.Log("[CookingChallengeManager] Claim Reward clicked.");
    }

    private IEnumerator CookSubmitRoutine()
    {
        isCooking = true;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCookStart();
        if (cookingFX != null)             cookingFX.PlayCookFX();

        yield return new WaitForSeconds(cookSubmitDelay);

        CookingScoreResult result = CookingScoreCalculator.Evaluate(
            currentDishData,
            cookingSelectionManager.GetSelectedIngredientCards(),
            cookingSelectionManager.GetSelectedSeasoningCards(),
            correctTechniqueForNow
        );

        uiManager.ShowResult(result);
        uiManager.SetPreviewScore(result.finalScore);

        if (cookingFX != null) cookingFX.PlayResultFX();

        if (AudioManager.Instance != null && result.finalScore >= successScoreThreshold)
            AudioManager.Instance.PlaySuccess();

        Debug.Log($"[Cook] Final={result.finalScore} | Ing={result.ingredientScore} Sea={result.seasoningScore} " +
                  $"Base={result.baseScore} Rare+{result.rareBonus} Tech+{result.techniqueBonus} | " +
                  $"Gold+{result.goldReward} Gem+{result.gemReward} Rank+{result.rankPointReward}");

        isCooking = false;
    }
}
