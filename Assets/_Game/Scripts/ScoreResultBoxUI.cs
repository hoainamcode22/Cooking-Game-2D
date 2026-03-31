using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreResultBoxUI : MonoBehaviour
{
    [Header("Ingredient Score")]
    [SerializeField] private TMP_Text txtIngredientPercent;
    [SerializeField] private TMP_Text txtIngredientScoreValue;

    [Header("Seasoning Score")]
    [SerializeField] private TMP_Text txtSeasoningPercent;
    [SerializeField] private TMP_Text txtSeasoningScoreValue;

    [Header("Final Score")]
    [SerializeField] private TMP_Text txtFinalScoreValue;
    [SerializeField] private TMP_Text txtFinalComment;

    [Header("Reward Preview")]
    [SerializeField] private TMP_Text txtGoldReward;
    [SerializeField] private TMP_Text txtGemReward;
    [SerializeField] private TMP_Text txtRankPointReward;

    public void ShowResult(CookingScoreResult result)
    {
        if (result == null) return;

        if (txtIngredientPercent != null)
            txtIngredientPercent.text = result.ingredientScore + "%";

        if (txtIngredientScoreValue != null)
            txtIngredientScoreValue.text = result.ingredientScore + "/100";

        if (txtSeasoningPercent != null)
            txtSeasoningPercent.text = result.seasoningScore + "%";

        if (txtSeasoningScoreValue != null)
            txtSeasoningScoreValue.text = result.seasoningScore + "/100";

        if (txtFinalScoreValue != null)
            txtFinalScoreValue.text = result.finalScore + "/100";

        if (txtFinalComment != null)
            txtFinalComment.text = GetComment(result.finalScore);

        if (txtGoldReward != null)
            txtGoldReward.text = "+" + result.goldReward;

        if (txtGemReward != null)
            txtGemReward.text = "+" + result.gemReward;

        if (txtRankPointReward != null)
            txtRankPointReward.text = "+" + result.rankPointReward;
    }

    private string GetComment(int score)
    {
        if (score >= 90) return "Tuy?t v?i! G?n nh? hoàn h?o!";
        if (score >= 80) return "R?t t?t! G?n hoàn h?o!";
        if (score >= 70) return "Khá t?t! C? g?ng thêm nhé!";
        return "Ch?a ?n l?m! Th? l?i nào!";
    }
}