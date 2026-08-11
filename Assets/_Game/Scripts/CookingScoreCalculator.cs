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

    HashSet<string> requiredIds = new HashSet<string>();
    HashSet<string> selectedIds = new HashSet<string>();

    // Chỉ lấy nguyên liệu yêu cầu, bỏ qua gia vị
    foreach (IngredientData required in dishData.requiredIngredients)
    {
        if (required == null)
            continue;

        if (required.kind != IngredientKind.Ingredient)
            continue;

        requiredIds.Add(required.id);
    }

    // Chỉ lấy nguyên liệu người chơi đã chọn
    foreach (SelectableIngredientCard card in selectedIngredients)
    {
        if (card == null)
            continue;

        IngredientData selected = card.GetIngredientData();

        if (selected == null)
            continue;

        if (selected.kind != IngredientKind.Ingredient)
            continue;

        selectedIds.Add(selected.id);
    }

    Debug.Log("=== Required Ingredients Only ===");
    foreach (string id in requiredIds)
        Debug.Log(id);

    Debug.Log("=== Selected Ingredients Only ===");
    foreach (string id in selectedIds)
        Debug.Log(id);

    if (requiredIds.Count == 0)
        return 0;

    bool hasMistake = false;
    bool hasMatched = false;

    // Kiểm tra nguyên liệu thừa
    foreach (string selectedId in selectedIds)
    {
        if (requiredIds.Contains(selectedId))
        {
            hasMatched = true;
        }
        else
        {
            hasMistake = true;
            Debug.Log("Nguyên liệu thừa: " + selectedId);
        }
    }

    // Kiểm tra nguyên liệu thiếu
    foreach (string requiredId in requiredIds)
    {
        if (!selectedIds.Contains(requiredId))
        {
            hasMistake = true;
            Debug.Log("Nguyên liệu thiếu: " + requiredId);
        }
    }

    if (!hasMatched)
        return 0;

    if (hasMistake)
        return maxScore / 2;

    return maxScore;
}
    // C9 — đã xoá `IsSameIngredient(selected, required)`: không nơi nào gọi.
    // Và ĐỪNG dựng lại nó: nó so bằng `selected.name == required.name` (TÊN ASSET).
    // Cách so đó chính là thứ đã che lỗi trùng asset ở A7 — hai file `SEA_Pepper.asset`
    // cùng tên nhưng một bản `kind: 0`, một bản `kind: 1`, mà `IsSameIngredient` vẫn coi
    // là một. Hàm `ScoreRequiredIngredients` bên trên so bằng `IngredientData.id` — đúng,
    // vì `id` là khoá dữ liệu, còn tên asset chỉ là tên file, ai đổi cũng được.
}