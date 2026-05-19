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





    public void ShowResult(CookingScoreResult result)
    {
        Debug.Log("SHOW RESULT UI CALLED");

        if (result == null)
        {
            Debug.LogWarning("ShowResult called but result is NULL");
            return;
        }

        gameObject.SetActive(true);



        if (txtIngredientScoreValue != null)
            txtIngredientScoreValue.text = result.ingredientScore + "/100";



        if (txtSeasoningScoreValue != null)
            txtSeasoningScoreValue.text = result.seasoningScore + "/100";

        if (txtFinalScoreValue != null)
            txtFinalScoreValue.text = result.finalScore + "/100";

        if (txtFinalComment != null)
            txtFinalComment.text = GetComment(result.finalScore);

    }
    public void ResetUI()
    {
        gameObject.SetActive(false);

        if (txtIngredientScoreValue != null)
            txtIngredientScoreValue.text = "0/100";


        if (txtSeasoningScoreValue != null)
            txtSeasoningScoreValue.text = "0/100";

        if (txtFinalScoreValue != null)
            txtFinalScoreValue.text = "0/100";

        if (txtFinalComment != null)
            txtFinalComment.text = "";


    }


    private string GetComment(int score)
    {
        if (score >= 90) return "Tuyệt vời! Gần như hoàn hảo!";
        if (score >= 80) return "Rất tốt! Gần hoàn hảo!";
        if (score >= 70) return "Khá tốt! Cố gắng thêm nhé!";
        return "Chưa ổn lắm! Thử lại nào!";
    }
}