using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintsBoxUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text txtHintTitle;
    [SerializeField] private TMP_Text txtRequiredLabel;
    [SerializeField] private TMP_Text txtOptionalLabel;
    [SerializeField] private TMP_Text txtSeasoningTipsTitle;
    [SerializeField] private TMP_Text txtBonusComboLabel;

    [Header("Required")]
    [SerializeField] private GameObject hintRequiredItemBeef;
    [SerializeField] private Image imgRequiredBeefIcon;
    [SerializeField] private TMP_Text txtRequiredBeefName;

    [SerializeField] private GameObject hintRequiredItemNoodle;
    [SerializeField] private Image imgRequiredNoodleIcon;
    [SerializeField] private TMP_Text txtRequiredNoodleName;

    [Header("Optional")]
    [SerializeField] private GameObject hintOptionalItemMushroom;
    [SerializeField] private Image imgOptionalMushroomIcon;
    [SerializeField] private TMP_Text txtOptionalMushroomName;

    [SerializeField] private GameObject hintOptionalItemEgg;
    [SerializeField] private Image imgOptionalEggIcon;
    [SerializeField] private TMP_Text txtOptionalEggName;

    [SerializeField] private GameObject hintOptionalItemHerbs;
    [SerializeField] private Image imgOptionalHerbsIcon;
    [SerializeField] private TMP_Text txtOptionalHerbsName;

    [Header("Seasoning Tips")]
    [SerializeField] private GameObject hintSeasoningTipFishSauce;
    [SerializeField] private Image imgTipFishSauceIcon;
    [SerializeField] private TMP_Text txtTipFishSauceItemName;
    [SerializeField] private TMP_Text txtTipFishSauceStatValue;
    [SerializeField] private TMP_Text txtTipFishSauceStatName;

    [SerializeField] private GameObject hintSeasoningTipChili;
    [SerializeField] private Image imgTipChiliIcon;
    [SerializeField] private TMP_Text txtTipChiliItemName;
    [SerializeField] private TMP_Text txtTipChiliStatValue;
    [SerializeField] private TMP_Text txtTipChiliStatName;

    [SerializeField] private GameObject hintSeasoningTipLemon;
    [SerializeField] private Image imgTipLemonIcon;
    [SerializeField] private TMP_Text txtTipLemonItemName;
    [SerializeField] private TMP_Text txtTipLemonStatValue;
    [SerializeField] private TMP_Text txtTipLemonStatName;

    [SerializeField] private GameObject hintSeasoningTipSalt;
    [SerializeField] private Image imgTipSaltIcon;
    [SerializeField] private TMP_Text txtTipSaltItemName;
    [SerializeField] private TMP_Text txtTipSaltStatValue;
    [SerializeField] private TMP_Text txtTipSaltStatName;

    [Header("Bonus Combo")]
    [SerializeField] private TMP_Text txtBonusItem1;
    [SerializeField] private TMP_Text txtBonusPlus1;
    [SerializeField] private TMP_Text txtBonusItem2;
    [SerializeField] private TMP_Text txtBonusPlus2;
    [SerializeField] private TMP_Text txtBonusItem3;
    [SerializeField] private TMP_Text txtBonusEquals;
    [SerializeField] private TMP_Text txtBonusValue;
    [SerializeField] private TMP_Text txtBonusScore;

    [Header("Judge Button")]
    [SerializeField] private TMP_Text txtButtonPrefix;
    [SerializeField] private TMP_Text txtButtonLabel;

    private void Awake()
    {
        SetStaticTexts();
    }

    private void SetStaticTexts()
    {
        if (txtHintTitle != null) txtHintTitle.text = "Hint";
        if (txtRequiredLabel != null) txtRequiredLabel.text = "Required:";
        if (txtOptionalLabel != null) txtOptionalLabel.text = "Optional:";
        if (txtSeasoningTipsTitle != null) txtSeasoningTipsTitle.text = "Seasoning Tips:";
        if (txtBonusComboLabel != null) txtBonusComboLabel.text = "Bonus Combo:";

        if (txtBonusPlus1 != null) txtBonusPlus1.text = "+";
        if (txtBonusPlus2 != null) txtBonusPlus2.text = "+";
        if (txtBonusEquals != null) txtBonusEquals.text = "=";
        if (txtBonusScore != null) txtBonusScore.text = "Score";

        if (txtButtonPrefix != null) txtButtonPrefix.text = "?";
        if (txtButtonLabel != null) txtButtonLabel.text = "What Judge Likes";
    }

    public void BindDish(DishData dishData)
    {
        if (dishData == null)
        {
            ClearUI();
            return;
        }

        BindHintItem(dishData.required1, hintRequiredItemBeef, imgRequiredBeefIcon, txtRequiredBeefName);
        BindHintItem(dishData.required2, hintRequiredItemNoodle, imgRequiredNoodleIcon, txtRequiredNoodleName);

        BindHintItem(dishData.optional1, hintOptionalItemMushroom, imgOptionalMushroomIcon, txtOptionalMushroomName);
        BindHintItem(dishData.optional2, hintOptionalItemEgg, imgOptionalEggIcon, txtOptionalEggName);
        BindHintItem(dishData.optional3, hintOptionalItemHerbs, imgOptionalHerbsIcon, txtOptionalHerbsName);

        BindSeasoningTip(dishData.tip1, hintSeasoningTipFishSauce, imgTipFishSauceIcon, txtTipFishSauceItemName, txtTipFishSauceStatValue, txtTipFishSauceStatName);
        BindSeasoningTip(dishData.tip2, hintSeasoningTipChili, imgTipChiliIcon, txtTipChiliItemName, txtTipChiliStatValue, txtTipChiliStatName);
        BindSeasoningTip(dishData.tip3, hintSeasoningTipLemon, imgTipLemonIcon, txtTipLemonItemName, txtTipLemonStatValue, txtTipLemonStatName);
        BindSeasoningTip(dishData.tip4, hintSeasoningTipSalt, imgTipSaltIcon, txtTipSaltItemName, txtTipSaltStatValue, txtTipSaltStatName);

        ApplyBonusComboText(dishData.bonusComboText);

        if (txtButtonLabel != null && !string.IsNullOrEmpty(dishData.whatJudgeLikesText))
            txtButtonLabel.text = dishData.whatJudgeLikesText;
    }

    public void ClearUI()
    {
        BindHintItem(null, hintRequiredItemBeef, imgRequiredBeefIcon, txtRequiredBeefName);
        BindHintItem(null, hintRequiredItemNoodle, imgRequiredNoodleIcon, txtRequiredNoodleName);

        BindHintItem(null, hintOptionalItemMushroom, imgOptionalMushroomIcon, txtOptionalMushroomName);
        BindHintItem(null, hintOptionalItemEgg, imgOptionalEggIcon, txtOptionalEggName);
        BindHintItem(null, hintOptionalItemHerbs, imgOptionalHerbsIcon, txtOptionalHerbsName);

        BindSeasoningTip(null, hintSeasoningTipFishSauce, imgTipFishSauceIcon, txtTipFishSauceItemName, txtTipFishSauceStatValue, txtTipFishSauceStatName);
        BindSeasoningTip(null, hintSeasoningTipChili, imgTipChiliIcon, txtTipChiliItemName, txtTipChiliStatValue, txtTipChiliStatName);
        BindSeasoningTip(null, hintSeasoningTipLemon, imgTipLemonIcon, txtTipLemonItemName, txtTipLemonStatValue, txtTipLemonStatName);
        BindSeasoningTip(null, hintSeasoningTipSalt, imgTipSaltIcon, txtTipSaltItemName, txtTipSaltStatValue, txtTipSaltStatName);

        ApplyBonusComboText("");
        if (txtButtonLabel != null) txtButtonLabel.text = "What Judge Likes";
    }

    private void BindHintItem(HintIngredientSlotData data, GameObject root, Image icon, TMP_Text nameText)
    {
        bool hasData = data != null && (!string.IsNullOrEmpty(data.displayName) || data.icon != null);

        if (root != null)
            root.SetActive(hasData);

        if (!hasData) return;

        if (icon != null)
            icon.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.displayName;
    }

    private void BindSeasoningTip(
        SeasoningTipData data,
        GameObject root,
        Image icon,
        TMP_Text itemName,
        TMP_Text statValue,
        TMP_Text statName)
    {
        bool hasData = data != null &&
                       (!string.IsNullOrEmpty(data.displayName) ||
                        !string.IsNullOrEmpty(data.effectText) ||
                        data.icon != null);

        if (root != null)
            root.SetActive(hasData);

        if (!hasData) return;

        if (icon != null)
            icon.sprite = data.icon;

        if (itemName != null)
            itemName.text = data.displayName;

        if (statValue != null)
            statValue.text = data.effectText;
    }
    private void ParseEffectText(string effectText, out string value, out string stat)
    {
        value = "";
        stat = "";

        if (string.IsNullOrEmpty(effectText))
            return;

        string[] parts = effectText.Split(' ');
        if (parts.Length > 0)
        {
            value = parts[0];
            if (parts.Length > 1)
                stat = string.Join(" ", parts, 1, parts.Length - 1);
        }
        else
        {
            value = effectText;
        }
    }
    public void OnClickWhatJudgeLikes()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();

        Debug.Log("What Judge Likes clicked.");
    }
    private void ApplyBonusComboText(string comboText)
    {
        if (txtBonusItem1 == null || txtBonusItem2 == null || txtBonusItem3 == null || txtBonusValue == null)
            return;

        txtBonusItem1.text = "";
        txtBonusItem2.text = "";
        txtBonusItem3.text = "";
        txtBonusValue.text = "";

        if (string.IsNullOrEmpty(comboText))
            return;

        // ví dụ: Beef + Herbs + Fish Sauce = +20 Score
        string[] sides = comboText.Split('=');
        if (sides.Length >= 1)
        {
            string left = sides[0].Trim();
            string[] items = left.Split('+');

            if (items.Length > 0) txtBonusItem1.text = items[0].Trim();
            if (items.Length > 1) txtBonusItem2.text = items[1].Trim();
            if (items.Length > 2) txtBonusItem3.text = items[2].Trim();
        }

        if (sides.Length > 1)
        {
            string right = sides[1].Trim();
            right = right.Replace("Score", "").Trim();
            txtBonusValue.text = right;
        }
    }
}