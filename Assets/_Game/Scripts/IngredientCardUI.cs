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

    [Header("Stars")]
    public Image star1;
    public Image star2;
    public Image star3;

    public void Setup(string itemName, Sprite mainIcon, Sprite topIcon, int starCount, bool isSelected)
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

        if (statusIcon != null)
            statusIcon.gameObject.SetActive(isSelected);

        if (star1 != null)
            star1.enabled = starCount >= 1;

        if (star2 != null)
            star2.enabled = starCount >= 2;

        if (star3 != null)
            star3.enabled = starCount >= 3;
    }
}