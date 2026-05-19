using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientItemUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameText;
    public Image mainIconImage;
    public Image statusIcon;

    public void Setup(string itemName, Sprite mainIcon, bool isSelected)
    {
        if (nameText != null)
            nameText.text = itemName;

        if (mainIconImage != null)
        {
            bool hasMain = mainIcon != null;
            mainIconImage.sprite = mainIcon;
            mainIconImage.enabled = hasMain;
            mainIconImage.color = Color.white;
            mainIconImage.gameObject.SetActive(hasMain);
        }

        if (statusIcon != null)
            statusIcon.gameObject.SetActive(isSelected);
    }
}