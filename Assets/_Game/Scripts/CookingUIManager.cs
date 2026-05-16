using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gộp từ CenterCookingPanelUI + TargetFlavorBoxUI + ScoreResultBoxUI.
/// Đầu mối duy nhất để CookingChallengeManager cập nhật toàn bộ giao diện Cooking.
/// </summary>
public class CookingUIManager : MonoBehaviour
{
    // ── Dish Display (từ TargetFlavorBoxUI) ──────────────────────────────────
    [Header("Dish Info")]
    [SerializeField] private Image    uiDishImage;
    [SerializeField] private TMP_Text txtTodayDishTitle;
    [SerializeField] private TMP_Text txtTargetFlavorTitle;
    [SerializeField] private TMP_Text txtDishNameFull;

    [Header("Flavor Labels")]
    [SerializeField] private TMP_Text txtFlavorLabelSweet;
    [SerializeField] private TMP_Text txtFlavorLabelSpicy;
    [SerializeField] private TMP_Text txtFlavorLabelSour;
    [SerializeField] private TMP_Text txtFlavorLabelUmami;
    [SerializeField] private TMP_Text txtFlavorLabelTexture;

    [Header("Flavor Bars")]
    [SerializeField] private Image barFillSweet;
    [SerializeField] private Image barFillSpicy;
    [SerializeField] private Image barFillSour;
    [SerializeField] private Image barFillUmami;
    [SerializeField] private Image barFillTexture;

    [Header("Flavor Values")]
    [SerializeField] private TMP_Text txtFlavorValueSweet;
    [SerializeField] private TMP_Text txtFlavorValueSpicy;
    [SerializeField] private TMP_Text txtFlavorValueSour;
    [SerializeField] private TMP_Text txtFlavorValueUmami;
    [SerializeField] private TMP_Text txtFlavorValueTexture;

    [Header("Flavor Config")]
    [SerializeField] private int maxFlavorValue = 5;

    // ── Cook Submit Score (từ CenterCookingPanelUI) ───────────────────────────
    [Header("Cook Button")]
    [SerializeField] private TMP_Text txtCookSubmitScore;

    // ── Score Result (từ ScoreResultBoxUI) ────────────────────────────────────
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

    // ── Vòng đời ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (txtTodayDishTitle     != null) txtTodayDishTitle.text     = "Món ăn hôm nay";
        if (txtTargetFlavorTitle  != null) txtTargetFlavorTitle.text  = "Hương vị";
        if (txtFlavorLabelSweet   != null) txtFlavorLabelSweet.text   = "Ngọt";
        if (txtFlavorLabelSpicy   != null) txtFlavorLabelSpicy.text   = "Cay";
        if (txtFlavorLabelSour    != null) txtFlavorLabelSour.text    = "Chua";
        if (txtFlavorLabelUmami   != null) txtFlavorLabelUmami.text   = "Đậm đà";
        if (txtFlavorLabelTexture != null) txtFlavorLabelTexture.text = "Kết cấu";
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void BindDish(DishData dishData)
    {
        if (dishData == null) { ClearAll(); return; }

        // Tên đầy đủ: "Tên món (Phụ đề)"
        string sub      = dishData.dishSubTitle?.Trim() ?? string.Empty;
        bool wrapped    = sub.StartsWith("(") && sub.EndsWith(")");
        string fullName = string.IsNullOrEmpty(sub)
            ? dishData.dishName
            : (wrapped ? $"{dishData.dishName} {sub}" : $"{dishData.dishName} ({sub})");

        if (uiDishImage != null)
        {
            uiDishImage.sprite         = dishData.dishSprite;
            uiDishImage.preserveAspect = true;
        }
        if (txtDishNameFull != null) txtDishNameFull.text = fullName;

        SetFlavorBars(
            dishData.targetFlavor.sweet,
            dishData.targetFlavor.spicy,
            dishData.targetFlavor.sour,
            dishData.targetFlavor.umami,
            dishData.targetFlavor.texture);

        SetPreviewScore(0);
    }

    public void ClearAll()
    {
        if (uiDishImage    != null) uiDishImage.sprite    = null;
        if (txtDishNameFull != null) txtDishNameFull.text = string.Empty;
        SetFlavorBars(0, 0, 0, 0, 0);
        SetPreviewScore(0);
    }

    public void SetPreviewScore(int score)
    {
        if (txtCookSubmitScore != null)
            txtCookSubmitScore.text = score + " Điểm";
    }

    public void ShowResult(CookingScoreResult result)
    {
        if (result == null) return;

        if (txtIngredientPercent    != null) txtIngredientPercent.text    = result.ingredientScore + "%";
        if (txtIngredientScoreValue != null) txtIngredientScoreValue.text = result.ingredientScore + "/100";
        if (txtSeasoningPercent     != null) txtSeasoningPercent.text     = result.seasoningScore  + "%";
        if (txtSeasoningScoreValue  != null) txtSeasoningScoreValue.text  = result.seasoningScore  + "/100";
        if (txtFinalScoreValue      != null) txtFinalScoreValue.text      = result.finalScore      + "/100";
        if (txtFinalComment         != null) txtFinalComment.text         = GetScoreComment(result.finalScore);
        if (txtGoldReward           != null) txtGoldReward.text           = "+" + result.goldReward;
        if (txtGemReward            != null) txtGemReward.text            = "+" + result.gemReward;
        if (txtRankPointReward      != null) txtRankPointReward.text      = "+" + result.rankPointReward;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetFlavorBars(int sweet, int spicy, int sour, int umami, int texture)
    {
        SetOneFlavor(barFillSweet,   txtFlavorValueSweet,   sweet);
        SetOneFlavor(barFillSpicy,   txtFlavorValueSpicy,   spicy);
        SetOneFlavor(barFillSour,    txtFlavorValueSour,    sour);
        SetOneFlavor(barFillUmami,   txtFlavorValueUmami,   umami);
        SetOneFlavor(barFillTexture, txtFlavorValueTexture, texture);
    }

    private void SetOneFlavor(Image fillBar, TMP_Text valueText, int value)
    {
        value = Mathf.Clamp(value, 0, maxFlavorValue);
        if (valueText != null) valueText.text    = value.ToString();
        if (fillBar   != null) fillBar.fillAmount = (float)value / maxFlavorValue;
    }

    private static string GetScoreComment(int score)
    {
        if (score >= 90) return "Tuyệt vời! Gần như hoàn hảo!";
        if (score >= 80) return "Rất tốt! Gần hoàn hảo!";
        if (score >= 70) return "Khá tốt! Cố gắng thêm nhé!";
        return "Chưa ổn lắm! Thử lại nào!";
    }
}
