using System.Collections.Generic;
using UnityEngine;

public static class CookingScoreCalculator
{
    public static FlavorVector SumVectorsFromCards(List<SelectableIngredientCard> cards)
    {
        FlavorVector total = FlavorVector.Zero;

        if (cards == null)
            return total;

        foreach (SelectableIngredientCard card in cards)
        {
            if (card == null) continue;

            IngredientData data = card.GetIngredientData();
            if (data == null) continue;

            total += data.vector;
        }

        return total;
    }
    // Công thức tính điểm
    public static int ScoreFromVector(FlavorVector player, FlavorVector target)
    {
        int distance = player.ManhattanDistance(target);
        int score = 100 - (distance * 5);
        return Mathf.Clamp(score, 0, 100);
    }

    public static int CalculateRareBonusFromCards(List<SelectableIngredientCard> cards)
    {
        if (cards == null) return 0;

        int bonus = 0;

        foreach (SelectableIngredientCard card in cards)
        {
            if (card == null) continue;

            IngredientData data = card.GetIngredientData();
            if (data == null) continue;

            if (data.tier != IngredientTier.Basic)
                bonus += 5;
        }

        return bonus;
    }
    // Hàm này sẽ xác định phần thưởng dựa trên điểm số cuối cùng, nó sẽ trả về số vàng, ngọc và điểm xếp hạng tương ứng với từng mức điểm, đảm bảo rằng phần thưởng được phân chia hợp lý để khuyến khích người chơi cải thiện kỹ năng nấu ăn của mình
    public static void GetRewardByScore(int finalScore, out int gold, out int gems, out int rankPoints)
    {
        if (finalScore >= 90)
        {
            gold = 200;
            gems = 5;
            rankPoints = 50;
        }
        else if (finalScore >= 80)
        {
            gold = 150;
            gems = 2;
            rankPoints = 30;
        }
        else if (finalScore >= 70)
        {
            gold = 100;
            gems = 0;
            rankPoints = 15;
        }
        else
        {
            gold = 50;
            gems = 0;
            rankPoints = 5;
        }
    }

    public static CookingScoreResult Evaluate(
        DishData dishData,
        List<SelectableIngredientCard> selectedIngredients,
        List<SelectableIngredientCard> selectedSeasonings,
        bool correctTechnique
    )
    {
        CookingScoreResult result = new CookingScoreResult();

        if (dishData == null)
            return result;

        result.ingredientVector = SumVectorsFromCards(selectedIngredients);
        result.seasoningVector = SumVectorsFromCards(selectedSeasonings);
        result.totalVector = result.ingredientVector + result.seasoningVector;

        result.ingredientScore = ScoreFromVector(result.ingredientVector, dishData.targetFlavor);
        result.seasoningScore = ScoreFromVector(result.seasoningVector, dishData.targetFlavor);
        result.baseScore = ScoreFromVector(result.totalVector, dishData.targetFlavor);

        result.rareBonus =
            CalculateRareBonusFromCards(selectedIngredients) +
            CalculateRareBonusFromCards(selectedSeasonings);

        result.techniqueBonus = correctTechnique ? 10 : 0;

        result.finalScore = Mathf.Clamp(
            result.baseScore + result.rareBonus + result.techniqueBonus,
            0,
            100
        );

        GetRewardByScore(result.finalScore, out result.goldReward, out result.gemReward, out result.rankPointReward);

        return result;
    }
}