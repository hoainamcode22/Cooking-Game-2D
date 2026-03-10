using UnityEngine;
using TMPro;

public class LeftPanelRefs : MonoBehaviour
{
    [Header("Containers")]
    public Transform ingredientsContent;
    public Transform seasoningsContent;

    [Header("Headers")]
    public TMP_Text ingredientsTitleText;
    public TMP_Text ingredientsCountText;
    public TMP_Text seasoningsTitleText;
    public TMP_Text seasoningsCountText;

    [Header("Samples")]
    public IngredientItemUI ingredientCardSample;
    public IngredientItemUI seasoningCardSample;
}