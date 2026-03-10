using System.Collections.Generic;
using UnityEngine;

public class LeftPanelSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CardData
    {
        public IngredientData ingredientData;

        public string itemName;
        public Sprite mainIcon;
        public Sprite topIcon;

        [Range(1, 3)] public int starCount = 3;
    }

    [Header("Refs")]
    public LeftPanelRefs leftPanel;

    [Header("Ingredients")]
    public List<CardData> ingredients = new List<CardData>();

    [Header("Seasonings")]
    public List<CardData> seasonings = new List<CardData>();

    private void Start()
    {
        if (leftPanel == null)
        {
            Debug.LogError("LeftPanelRefs chưa được gán.");
            return;
        }

        SpawnAll();
    }

    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        SpawnList(ingredients, leftPanel.ingredientsContent, leftPanel.ingredientCardSample);
        SpawnList(seasonings, leftPanel.seasoningsContent, leftPanel.seasoningCardSample);
    }

    private void SpawnList(List<CardData> dataList, Transform parent, IngredientItemUI samplePrefab)
    {
        if (parent == null || samplePrefab == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child == samplePrefab.transform)
                continue;

            Destroy(child.gameObject);
        }

        samplePrefab.gameObject.SetActive(false);

        foreach (var data in dataList)
        {
            IngredientItemUI newCard = Instantiate(samplePrefab, parent, false);
            newCard.gameObject.SetActive(true);

            RectTransform rt = newCard.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
            }

            string displayName = data.itemName;
            Sprite mainSprite = data.mainIcon;
            Sprite topSprite = data.topIcon;

            if (data.ingredientData != null)
            {
                if (!string.IsNullOrEmpty(data.ingredientData.displayName))
                    displayName = data.ingredientData.displayName;

                if (data.ingredientData.icon != null)
                    mainSprite = data.ingredientData.icon;
            }

            newCard.Setup(
                displayName,
                mainSprite,
                topSprite,
                data.starCount,
                false
            );

            SelectableIngredientCard selectableCard = newCard.GetComponent<SelectableIngredientCard>();
            if (selectableCard != null)
            {
                selectableCard.SetIngredientData(data.ingredientData);
            }
            else
            {
                Debug.LogWarning("Card spawn ra chưa có SelectableIngredientCard: " + displayName);
            }
        }
    }
}