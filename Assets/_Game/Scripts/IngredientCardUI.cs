using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientItemUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameText;
    public Image topIconImage;
    public Image mainIconImage;
    public Image statusIcon;


    public void Setup(string itemName, Sprite mainIcon, Sprite topIcon, bool isSelected)
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

        if (topIconImage != null)
        {
            bool hasTop = topIcon != null;
            topIconImage.sprite = topIcon;
            topIconImage.enabled = hasTop;
            topIconImage.color = Color.white;
            topIconImage.gameObject.SetActive(hasTop);
        }
    }
}