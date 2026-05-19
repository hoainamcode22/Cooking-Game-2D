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
        List<SelectableIngredientCard> selectedSeasonings
    )
    {
        CookingScoreResult result = new CookingScoreResult();

        if (dishData == null)
            return result;

        result.ingredientScore = ScoreRequiredIngredients(dishData, selectedIngredients);

        int flavorScore100 = ScoreFromVector(result.totalVector, dishData.targetFlavor);
        result.seasoningScore = Mathf.RoundToInt(flavorScore100 * 0.3f);

        result.baseScore = result.ingredientScore + result.seasoningScore;

        result.rareBonus = 0;
        result.techniqueBonus = 0;

        result.finalScore = Mathf.Clamp(result.baseScore, 0, 100);

        GetRewardByScore(
            result.finalScore,
            out result.goldReward,
            out result.gemReward,
            out result.rankPointReward
        );
        Debug.Log(
            $"[CookingScore] IngredientScore = {result.ingredientScore}, " +
            $"FlavorScore = {result.seasoningScore}, " +
            $"BaseScore = {result.baseScore}, " +
            $"FinalScore = {result.finalScore}"
        );
        return result;
    }
    public static int ScoreRequiredIngredients(
        DishData dishData,
        List<SelectableIngredientCard> selectedIngredients
    )
    {
        Debug.Log("===== SCORE REQUIRED INGREDIENTS START =====");

        if (dishData == null)
        {
            Debug.Log("DishData NULL");
            return 0;
        }

        if (dishData.requiredIngredients == null)
        {
            Debug.Log("requiredIngredients NULL");
            return 0;
        }

        if (selectedIngredients == null)
        {
            Debug.Log("selectedIngredients NULL");
            return 0;
        }

        Debug.Log($"Required Count = {dishData.requiredIngredients.Count}");
        Debug.Log($"Selected Count = {selectedIngredients.Count}");

        int requiredCount = dishData.requiredIngredients.Count;

        if (requiredCount == 0)
            return 0;

        int maxScore = 70;
        int penaltyPerMistake = 10;

        int matchedCount = 0;
        int wrongCount = 0;

        foreach (IngredientData required in dishData.requiredIngredients)
        {
            if (required == null)
                continue;

            bool found = false;

            foreach (SelectableIngredientCard card in selectedIngredients)
            {
                if (card == null)
                    continue;

                IngredientData selected = card.GetIngredientData();

                Debug.Log(
                    $"Check Required = {required.name} | Selected = {(selected != null ? selected.name : "NULL")}"
                );

                if (selected == null)
                    continue;

                if (selected == required || selected.name == required.name)
                {
                    found = true;
                    break;
                }
            }

            if (found)
                matchedCount++;
        }

        foreach (SelectableIngredientCard card in selectedIngredients)
        {
            if (card == null)
                continue;

            IngredientData selected = card.GetIngredientData();

            Debug.Log($"Wrong Check Selected = {(selected != null ? selected.name : "NULL")}");

            if (selected == null)
                continue;

            bool isRequired = false;

            foreach (IngredientData required in dishData.requiredIngredients)
            {
                if (required == null)
                    continue;

                if (selected == required || selected.name == required.name)
                {
                    isRequired = true;
                    break;
                }
            }

            if (!isRequired)
                wrongCount++;
        }

        Debug.Log($"Matched = {matchedCount}, Wrong = {wrongCount}");

        if (matchedCount == 0)
            return 0;

        int missingCount = requiredCount - matchedCount;
        int totalMistake = missingCount + wrongCount;

        int score = maxScore - totalMistake * penaltyPerMistake;

        return Mathf.Clamp(score, 0, maxScore);
    }
    private static bool IsSameIngredient(IngredientData selected, IngredientData required)
    {
        if (selected == null || required == null)
            return false;

        // Trường hợp cùng một asset IngredientData
        if (selected == required)
            return true;

        // Trường hợp khác reference nhưng cùng tên asset
        if (selected.name == required.name)
            return true;

        return false;
    }
}