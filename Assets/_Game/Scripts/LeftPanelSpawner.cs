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
    }

    [Header("Refs")]
    public LeftPanelRefs leftPanel;
    public CookingSelectionManager selectionManager;

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
        // isSeasoning = false cho nguyên liệu, true cho gia vị
        SpawnList(ingredients, leftPanel.ingredientsContent, leftPanel.ingredientCardSample, isSeasoning: false);
        SpawnList(seasonings,  leftPanel.seasoningsContent,  leftPanel.seasoningCardSample,  isSeasoning: true);
    }

    private void SpawnList(List<CardData> dataList, Transform parent, IngredientItemUI samplePrefab, bool isSeasoning)
    {
        if (parent == null || samplePrefab == null) return;

        // Xoá card cũ, giữ lại sample
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == samplePrefab.transform) continue;
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
            Sprite mainSprite  = data.mainIcon;

            if (data.ingredientData != null)
            {
                if (!string.IsNullOrEmpty(data.ingredientData.displayName))
                    displayName = data.ingredientData.displayName;

                if (data.ingredientData.icon != null)
                    mainSprite = data.ingredientData.icon;
            }

            newCard.Setup(displayName, mainSprite, false);

            SelectableIngredientCard selectableCard = newCard.GetComponent<SelectableIngredientCard>();
            if (selectableCard == null)
            {
                Debug.LogWarning("[LeftPanelSpawner] Card thiếu SelectableIngredientCard: " + displayName);
                continue;
            }

            // 1. Gán data nguyên liệu (cũng đồng bộ DraggableItem.ingredientData bên trong)
            selectableCard.SetIngredientData(data.ingredientData);

            // 2. Gán Item ID để KitchenTransferManager trừ đúng slot sau khi nấu
            string itemId = data.ingredientData != null ? data.ingredientData.id : "";
            selectableCard.setIdItem(itemId);

            // 3. Đăng ký manager + đánh dấu loại (nguyên liệu / gia vị)
            //    Nếu selectionManager chưa gán trong Inspector, card vẫn hiển thị
            //    nhưng sẽ cảnh báo khi người dùng tương tác.
            if (selectionManager != null)
                selectableCard.Init(selectionManager, isSeasoning);
            else
                Debug.LogWarning("[LeftPanelSpawner] selectionManager chưa được gán — card '" + displayName + "' sẽ không phản hồi khi kéo/thả.");
        }
    }
}