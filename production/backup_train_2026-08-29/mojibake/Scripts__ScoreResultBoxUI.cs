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

    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite successBackground;
    [SerializeField] private Sprite failBackground;

    private void Awake()
    {
        Hide();
    }

    public void ShowResult(CookingScoreResult result)
    {
        ShowResult(result, true);
    }

    public void ShowResult(CookingScoreResult result, bool isSuccess)
    {

        if (result == null)
        {
            Debug.LogWarning("ShowResult called but result is NULL");
            return;
        }

        if (root == null)
        {
            Debug.LogWarning("Root chÆ°a Ä‘Æ°á»£c gÃ¡n trong ScoreResultBoxUI.");
            return;
        }

       root.SetActive(true);

        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;

            Color c = backgroundImage.color;
            c.a = 1f;
            backgroundImage.color = c;
            
            if (isSuccess && successBackground != null)
            {
            
                backgroundImage.sprite = successBackground;
            }
            else if (!isSuccess && failBackground != null)
            {
                backgroundImage.sprite = failBackground;
            }
        }

        if (txtIngredientPercent != null)
            txtIngredientPercent.text =  "70%";

        if (txtIngredientScoreValue != null)
            txtIngredientScoreValue.text = result.ingredientScore + "";

        if (txtSeasoningPercent != null)
            txtSeasoningPercent.text =  "30%";

        if (txtSeasoningScoreValue != null)
            txtSeasoningScoreValue.text = result.seasoningScore + "";

        if (txtFinalScoreValue != null)
            txtFinalScoreValue.text = result.finalScore + "/100";

        if (txtFinalComment != null)
            txtFinalComment.text = GetComment(result.finalScore, isSuccess);
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private string GetComment(int score, bool isSuccess)
    {
        if (!isSuccess)
            return "ChÆ°a Ä‘á»§ Ä‘iá»ƒm! HÃ£y thá»­ láº¡i nhÃ©!";

        if (score >= 90) return "Tuyá»‡t vá»i! Gáº§n nhÆ° hoÃ n háº£o!";
        if (score >= 80) return "Ráº¥t tá»‘t! Gáº§n hoÃ n háº£o!";
        if (score >= 70) return "KhÃ¡ tá»‘t! Cá»‘ gáº¯ng thÃªm nhÃ©!";
        return "ChÆ°a á»•n láº¯m! Thá»­ láº¡i nÃ o!";
    }
}
