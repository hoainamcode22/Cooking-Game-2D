using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingChallengeManager : MonoBehaviour
{
    [Header("Current Dish")]
    [SerializeField] private DishData currentDishData;

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
    [SerializeField] private int successScoreThreshold = 80;

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

        CookingScoreResult previewResult = CookingScoreCalculator.Evaluate(
            currentDishData,
            selectedIngredients,
            selectedSeasonings,
            correctTechniqueForNow
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
            selectedSeasonings,
            correctTechniqueForNow
        );

        scoreResultBoxUI.ShowResult(result);

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
    }
}