using System;
using System.Collections.Generic;
using UnityEngine;

// CookingScoreResult được gộp vào đây từ CookingScoreResult.cs (file đã xóa)
[Serializable]
public class CookingScoreResult
{
    public FlavorVector ingredientVector;
    public FlavorVector seasoningVector;
    public FlavorVector totalVector;

    public int ingredientScore;
    public int seasoningScore;

    public int baseScore;
    public int rareBonus;
    public int techniqueBonus;
    public int finalScore;

    public int goldReward;
    public int gemReward;
    public int rankPointReward;
}

public static class CookingScoreCalculator
{
    public static FlavorVector SumVectorsFromCards(List<SelectableIngredientCard> cards)
    {
        FlavorVector total = FlavorVector.Zero;
        if (cards == null) return total;

        foreach (var card in cards)
        {
            if (card == null) continue;
            IngredientData data = card.GetIngredientData();
            if (data != null) total += data.vector;
        }

        return total;
    }

    public static int ScoreFromVector(FlavorVector player, FlavorVector target)
    {
        int distance = player.ManhattanDistance(target);
        return Mathf.Clamp(100 - (distance * 5), 0, 100);
    }

    public static int CalculateRareBonusFromCards(List<SelectableIngredientCard> cards)
    {
        if (cards == null) return 0;

        int bonus = 0;
        foreach (var card in cards)
        {
            if (card == null) continue;
            IngredientData data = card.GetIngredientData();
            if (data != null && data.tier != IngredientTier.Basic)
                bonus += 5;
        }
        return bonus;
    }

    public static void GetRewardByScore(int finalScore, out int gold, out int gems, out int rankPoints)
    {
        if (finalScore >= 90)      { gold = 200; gems = 5; rankPoints = 50; }
        else if (finalScore >= 80) { gold = 150; gems = 2; rankPoints = 30; }
        else if (finalScore >= 70) { gold = 100; gems = 0; rankPoints = 15; }
        else                       { gold = 50;  gems = 0; rankPoints = 5;  }
    }

    public static CookingScoreResult Evaluate(
        DishData dishData,
        List<SelectableIngredientCard> selectedIngredients,
        List<SelectableIngredientCard> selectedSeasonings,
        bool correctTechnique)
    {
        var result = new CookingScoreResult();
        if (dishData == null) return result;

        result.ingredientVector = SumVectorsFromCards(selectedIngredients);
        result.seasoningVector  = SumVectorsFromCards(selectedSeasonings);
        result.totalVector      = result.ingredientVector + result.seasoningVector;

        result.ingredientScore = ScoreFromVector(result.ingredientVector, dishData.targetFlavor);
        result.seasoningScore  = ScoreFromVector(result.seasoningVector,  dishData.targetFlavor);
        result.baseScore       = ScoreFromVector(result.totalVector,      dishData.targetFlavor);

        result.rareBonus =
            CalculateRareBonusFromCards(selectedIngredients) +
            CalculateRareBonusFromCards(selectedSeasonings);

        result.techniqueBonus = correctTechnique ? 10 : 0;

        result.finalScore = Mathf.Clamp(
            result.baseScore + result.rareBonus + result.techniqueBonus, 0, 100);

        GetRewardByScore(result.finalScore,
            out result.goldReward, out result.gemReward, out result.rankPointReward);

        return result;
    }
}
