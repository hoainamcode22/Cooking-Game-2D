using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreResultBoxUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

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

    private void Awake()
    {
        Hide();
    }

    public void ShowResult(CookingScoreResult result)
    {
        Debug.Log("SHOW RESULT UI CALLED");

        if (result == null)
        {
            Debug.LogWarning("ShowResult called but result is NULL");
            return;
        }

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

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

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private string GetComment(int score)
    {
        if (score >= 90) return "Tuyệt vời! Gần như hoàn hảo!";
        if (score >= 80) return "Rất tốt! Gần hoàn hảo!";
        if (score >= 70) return "Khá tốt! Cố gắng thêm nhé!";
        return "Chưa ổn lắm! Thử lại nào!";
    }
}