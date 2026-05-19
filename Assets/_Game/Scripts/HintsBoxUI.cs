using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime;
using System.Diagnostics.CodeAnalysis;
using System.Security;

public class HintsBoxUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CookingSelectionManager cookingSelectionManager;
    [SerializeField] private  TargetFlavorBoxUI targetFlavorBoxUI;

    [Header("Required")]
    [SerializeField] private GameObject hintRequiredItemBeef;
    [SerializeField] private Image imgRequiredBeefIcon;
    [SerializeField] private TMP_Text txtRequiredBeefName;

    [SerializeField] private GameObject hintRequiredItemNoodle;
    [SerializeField] private Image imgRequiredNoodleIcon;
    [SerializeField] private TMP_Text txtRequiredNoodleName;

    [Header("Judge Button")]
    [SerializeField] private Button btnBack;

    [SerializeField] private DishBookUI dishBookUI;

    
    private void Start()
    {
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(OnClickBack);
        }
        else
        {
            Debug.LogWarning("[HintsBoxUI] Chưa gắn btnBack.");
        }
    }
    public void BindDish(DishData dishData)
    {
    if (dishData == null)
    {
        ClearUI();
        return;
    }

    gameObject.SetActive(true);

    Debug.Log("[HintsBoxUI] BindDish: " + dishData.dishName);



        BindHintItem(dishData.required1, hintRequiredItemBeef, imgRequiredBeefIcon, txtRequiredBeefName);
        BindHintItem(dishData.required2, hintRequiredItemNoodle, imgRequiredNoodleIcon, txtRequiredNoodleName);


    }

    public void ClearUI()
    {
        BindHintItem(null, hintRequiredItemBeef, imgRequiredBeefIcon, txtRequiredBeefName);
        BindHintItem(null, hintRequiredItemNoodle, imgRequiredNoodleIcon, txtRequiredNoodleName);
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

        string effect = LocalizeEffectText(data.effectText);
        if (statValue != null)
            statValue.text = effect;

        if (statName != null)
            statName.text = string.Empty;
    }

    private string LocalizeEffectText(string effectText)
    {
        if (string.IsNullOrEmpty(effectText))
            return string.Empty;

        return effectText
            .Replace("Umami", "Đậm đà")
            .Replace("Spicy", "Cay")
            .Replace("Sour", "Chua")
            .Replace("Sweet", "Ngọt")
            .Replace("Texture", "Kết cấu");
    }

    public void OnClickWhatJudgeLikes()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();

        Debug.Log("Giám khảo thích gì? clicked.");
    }

    private void ApplyBonusComboText(string comboText)
    {
        if (string.IsNullOrEmpty(comboText))
            return;

        // ví dụ: Beef + Herbs + Fish Sauce = +20 Score
        string[] sides = comboText.Split('=');

    }

    private string LocalizeCommonCookingWords(string s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        return s
            .Replace("Score", "Điểm")
            .Replace("Beef", "Thịt bò")
            .Replace("Noodle", "Bánh phở")
            .Replace("Egg", "Trứng")
            .Replace("Herbs", "Rau thơm")
            .Replace("Fish Sauce", "Nước mắm")
            .Replace("Chili", "Ớt")
            .Replace("Lemon", "Chanh")
            .Replace("Salt", "Muối");
    }
    private void OnClickBack()
    {
        Debug.Log("Back button clicked in HintsBoxUI.");

        ClearUI();
      
        targetFlavorBoxUI.ClearUI();
        cookingSelectionManager.DisableIngredientSelection();
        if (dishBookUI != null)
            dishBookUI.ShowDishList();
        else
            Debug.LogWarning("[HintsBoxUI] Chưa gắn DishBookUI.");
    }
}