using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    // Hàm này sẽ xác định phần thưởng dựa trên điểm số cuối cùng, nó sẽ trả về số vàng, ngọc và điểm xếp hạng tương ứng với từng mức điểm, đảm bảo rằng phần thưởng được phân chia hợp lý để khuyến khích người chơi cải thiện kỹ năng nấu ăn của mình

    public static CookingScoreResult Evaluate(
        DishData dishData,
        List<SelectableIngredientCard> selectedIngredients,
        List<SelectableIngredientCard> selectedSeasonings
    )
    {
        CookingScoreResult result = new CookingScoreResult();

        if (dishData == null)
            return result;

        result.ingredientVector = SumVectorsFromCards(selectedIngredients);
        result.seasoningVector = SumVectorsFromCards(selectedSeasonings);
        result.totalVector = result.ingredientVector + result.seasoningVector;

        result.ingredientScore = ScoreRequiredIngredients(dishData, selectedIngredients);

        int flavorScore100 = ScoreFromVector(result.totalVector, dishData.targetFlavor);
        result.seasoningScore = Mathf.RoundToInt(flavorScore100 * 0.3f);

        result.baseScore = result.ingredientScore + result.seasoningScore;

        result.rareBonus = 0;
        result.techniqueBonus = 0;

        result.finalScore = Mathf.Clamp(result.baseScore, 0, 100);

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
        if (dishData == null || dishData.requiredIngredients == null || selectedIngredients == null)
            return 0;

        int maxScore = 70;

        HashSet<string> requiredNames = new HashSet<string>();
        HashSet<string> selectedNames = new HashSet<string>();

        // Tập hợp nguyên liệu yêu cầu
        foreach (IngredientData required in dishData.requiredIngredients)
        {
            if (required == null)
                continue;

            requiredNames.Add(required.name);
        }

        // Tập hợp nguyên liệu người chơi đã chọn
        foreach (SelectableIngredientCard card in selectedIngredients)
        {
            if (card == null)
                continue;

            IngredientData selected = card.GetIngredientData();

            if (selected == null)
                continue;

            selectedNames.Add(selected.name);
        }

        bool hasMistake = false;
        bool hasMatched = false;

        // Kiểm tra thừa và xem có chọn đúng nguyên liệu nào không
        foreach (string selectedName in selectedNames)
        {
            if (requiredNames.Contains(selectedName))
                hasMatched = true; // chọn trúng ít nhất 1 nguyên liệu
            else
                hasMistake = true; // chọn sai → thừa
        }

        // Kiểm tra thiếu nếu chưa có lỗi
        if (!hasMistake)
        {
            foreach (string requiredName in requiredNames)
            {
                if (!selectedNames.Contains(requiredName))
                {
                    hasMistake = true; // thiếu nguyên liệu
                    break;
                }
            }
        }

        if (!hasMatched)
            return 0; // không trúng nguyên liệu nào → 0 điểm

        if (hasMistake)
            return maxScore / 2; // trúng nhưng có thiếu hoặc thừa → 50%

        return maxScore; // đúng hoàn toàn → 100%
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