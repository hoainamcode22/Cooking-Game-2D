using UnityEngine;

public class LeftPanelTest : MonoBehaviour
{
    public LeftPanelRefs leftPanel;

    private void Start()
    {
        if (leftPanel == null)
        {
            Debug.LogError("LeftPanelRefs chưa được gán.");
            return;
        }

        leftPanel.ingredientsTitleText.text = "Ingredients";
        leftPanel.ingredientsCountText.text = "Select 1/4";

        leftPanel.seasoningsTitleText.text = "Seasonings";
        leftPanel.seasoningsCountText.text = "Select 1/3";

        if (leftPanel.ingredientCardSample != null)
        {
            Sprite mainSprite = leftPanel.ingredientCardSample.mainIconImage != null
                ? leftPanel.ingredientCardSample.mainIconImage.sprite
                : null;

            Sprite topSprite = leftPanel.ingredientCardSample.topIconImage != null
                ? leftPanel.ingredientCardSample.topIconImage.sprite
                : null;

            leftPanel.ingredientCardSample.Setup("Beef", mainSprite, topSprite, 3, true);
        }

        if (leftPanel.seasoningCardSample != null)
        {
            Sprite mainSprite = leftPanel.seasoningCardSample.mainIconImage != null
                ? leftPanel.seasoningCardSample.mainIconImage.sprite
                : null;

            Sprite topSprite = leftPanel.seasoningCardSample.topIconImage != null
                ? leftPanel.seasoningCardSample.topIconImage.sprite
                : null;

            leftPanel.seasoningCardSample.Setup("Fish Sauce", mainSprite, topSprite, 3, false);
        }

        Debug.Log("LeftPanelTest đã chạy.");
    }
}