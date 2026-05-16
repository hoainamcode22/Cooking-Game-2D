using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct HintSlotRefs
{
    public GameObject root;
    public Image      icon;
    public TMP_Text   label;
}

[System.Serializable]
public struct SeasoningTipSlotRefs
{
    public GameObject root;
    public Image      icon;
    public TMP_Text   itemName;
    public TMP_Text   statValue;
    public TMP_Text   statName;
}

public class HintsBoxUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text txtHintTitle;
    [SerializeField] private TMP_Text txtRequiredLabel;
    [SerializeField] private TMP_Text txtOptionalLabel;
    [SerializeField] private TMP_Text txtSeasoningTipsTitle;
    [SerializeField] private TMP_Text txtBonusComboLabel;

    [Header("Required Slots (2)")]
    [SerializeField] private HintSlotRefs[] requiredSlots = new HintSlotRefs[2];

    [Header("Optional Slots (3)")]
    [SerializeField] private HintSlotRefs[] optionalSlots = new HintSlotRefs[3];

    [Header("Seasoning Tip Slots (4)")]
    [SerializeField] private SeasoningTipSlotRefs[] tipSlots = new SeasoningTipSlotRefs[4];

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

    public void BindDish(DishData dishData)
    {
        if (dishData == null) { ClearUI(); return; }

        // Bind required/optional slots theo index — không phụ thuộc tên nguyên liệu
        var required = new[] { dishData.required1, dishData.required2 };
        for (int i = 0; i < requiredSlots.Length; i++)
            BindHintSlot(i < required.Length ? required[i] : null, requiredSlots[i]);

        var optional = new[] { dishData.optional1, dishData.optional2, dishData.optional3 };
        for (int i = 0; i < optionalSlots.Length; i++)
            BindHintSlot(i < optional.Length ? optional[i] : null, optionalSlots[i]);

        var tips = new[] { dishData.tip1, dishData.tip2, dishData.tip3, dishData.tip4 };
        for (int i = 0; i < tipSlots.Length; i++)
            BindSeasoningTipSlot(i < tips.Length ? tips[i] : null, tipSlots[i]);

        ApplyBonusComboText(dishData.bonusComboText);

        if (txtButtonLabel != null && !string.IsNullOrEmpty(dishData.whatJudgeLikesText))
            txtButtonLabel.text = dishData.whatJudgeLikesText;
    }

    public void ClearUI()
    {
        for (int i = 0; i < requiredSlots.Length; i++)  BindHintSlot(null, requiredSlots[i]);
        for (int i = 0; i < optionalSlots.Length; i++)  BindHintSlot(null, optionalSlots[i]);
        for (int i = 0; i < tipSlots.Length; i++)       BindSeasoningTipSlot(null, tipSlots[i]);
        ApplyBonusComboText(string.Empty);
    }

    private void BindHintSlot(HintIngredientSlotData data, HintSlotRefs slot)
    {
        bool hasData = data != null && (!string.IsNullOrEmpty(data.displayName) || data.icon != null);

        if (slot.root != null) slot.root.SetActive(hasData);
        if (!hasData) return;

        if (slot.icon  != null) slot.icon.sprite = data.icon;
        if (slot.label != null) slot.label.text  = data.displayName;
    }

    private void BindSeasoningTipSlot(SeasoningTipData data, SeasoningTipSlotRefs slot)
    {
        bool hasData = data != null &&
                       (!string.IsNullOrEmpty(data.displayName) ||
                        !string.IsNullOrEmpty(data.effectText)  ||
                        data.icon != null);

        if (slot.root != null) slot.root.SetActive(hasData);
        if (!hasData) return;

        if (slot.icon     != null) slot.icon.sprite    = data.icon;
        if (slot.itemName != null) slot.itemName.text  = data.displayName;
        if (slot.statValue != null) slot.statValue.text = LocalizeEffectText(data.effectText);
        if (slot.statName  != null) slot.statName.text  = string.Empty;
    }

    // Chuyển tên stat từ tiếng Anh sang tiếng Việt (flavor vector labels từ data cũ)
    private string LocalizeEffectText(string effectText)
    {
        if (string.IsNullOrEmpty(effectText)) return string.Empty;

        return effectText
            .Replace("Umami",   "Đậm đà")
            .Replace("Spicy",   "Cay")
            .Replace("Sour",    "Chua")
            .Replace("Sweet",   "Ngọt")
            .Replace("Texture", "Kết cấu");
    }

    public void OnClickWhatJudgeLikes()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();

        Debug.Log("[HintsBoxUI] Giám khảo thích gì? clicked.");
    }

    // bonusComboText trong DishData phải là tiếng Việt, ví dụ: "Thịt bò + Rau thơm + Nước mắm = +20"
    private void ApplyBonusComboText(string comboText)
    {
        if (txtBonusItem1 == null || txtBonusItem2 == null || txtBonusItem3 == null || txtBonusValue == null)
            return;

        txtBonusItem1.text = "";
        txtBonusItem2.text = "";
        txtBonusItem3.text = "";
        txtBonusValue.text = "";

        if (string.IsNullOrEmpty(comboText)) return;

        string[] sides = comboText.Split('=');

        if (sides.Length >= 1)
        {
            string[] items = sides[0].Trim().Split('+');
            if (items.Length > 0) txtBonusItem1.text = items[0].Trim();
            if (items.Length > 1) txtBonusItem2.text = items[1].Trim();
            if (items.Length > 2) txtBonusItem3.text = items[2].Trim();
        }

        if (sides.Length > 1)
            txtBonusValue.text = sides[1].Trim().Replace("Điểm", "").Trim();

        if (txtBonusPlus1  != null) txtBonusPlus1.text  = "+";
        if (txtBonusPlus2  != null) txtBonusPlus2.text  = "+";
        if (txtBonusEquals != null) txtBonusEquals.text = "=";
        if (txtBonusScore  != null) txtBonusScore.text  = "Điểm";
    }
}
