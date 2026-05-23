using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HintsBoxUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CookingSelectionManager cookingSelectionManager;
    [SerializeField] private TargetFlavorBoxUI targetFlavorBoxUI;

    [Header("Required Ingredients Display")]
    [SerializeField] private Transform groupNguyenLieu;
    [SerializeField] private GameObject itemNguyenLieuTemplate;

    private readonly List<GameObject> spawnedNguyenLieuItems = new List<GameObject>();

    [Header("Judge Button")]
    [SerializeField] private Button btnBack;

    [SerializeField] private DishBookUI dishBookUI;

    private void Start()
    {
        if (itemNguyenLieuTemplate != null)
            itemNguyenLieuTemplate.SetActive(false);

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
        ClearUI();

        if (dishData == null)
            return;

        gameObject.SetActive(true);

        Debug.Log("[HintsBoxUI] BindDish: " + dishData.dishName);

        BindRequiredIngredients(dishData.requiredIngredients);
    }

    public void ClearUI()
    {
        foreach (GameObject item in spawnedNguyenLieuItems)
        {
            if (item != null)
                Destroy(item);
        }

        spawnedNguyenLieuItems.Clear();

        if (itemNguyenLieuTemplate != null)
            itemNguyenLieuTemplate.SetActive(false);
    }

    private void BindRequiredIngredients(List<IngredientData> requiredIngredients)
    {
        if (groupNguyenLieu == null)
        {
            Debug.LogWarning("[HintsBoxUI] Chưa gắn Group_NguyenLieu.");
            return;
        }

        if (itemNguyenLieuTemplate == null)
        {
            Debug.LogWarning("[HintsBoxUI] Chưa gắn Item_NguyenLieu template.");
            return;
        }

        if (requiredIngredients == null || requiredIngredients.Count == 0)
        {
            Debug.LogWarning("[HintsBoxUI] Món này chưa có requiredIngredients.");
            return;
        }

        itemNguyenLieuTemplate.SetActive(false);

        float spacingY = 22f;
        int index = 0;

        foreach (IngredientData ingredient in requiredIngredients)
        {
            if (ingredient == null)
                continue;

            Debug.Log("[HintsBoxUI] Show required ingredient: " + ingredient.name);

            GameObject itemObj = Instantiate(itemNguyenLieuTemplate, groupNguyenLieu);
            itemObj.name = "Item_NguyenLieu_" + ingredient.name;
            itemObj.SetActive(true);

            spawnedNguyenLieuItems.Add(itemObj);

            RectTransform rect = itemObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -index * spacingY);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 20f);
            }

            TMP_Text txtName = itemObj.GetComponentInChildren<TMP_Text>(true);
            Image imgIcon = GetIconImage(itemObj);

            if (txtName != null)
            {
                txtName.text = !string.IsNullOrEmpty(ingredient.displayName)
                    ? ingredient.displayName
                    : ingredient.name;

                Debug.Log("[HintsBoxUI] Set name = " + txtName.text);
            }
            else
            {
                Debug.LogWarning("[HintsBoxUI] Không tìm thấy TMP_Text trong Item_NguyenLieu.");
            }

            if (imgIcon != null)
            {
                imgIcon.sprite = ingredient.icon;
                imgIcon.enabled = ingredient.icon != null;
                imgIcon.preserveAspect = true;

                Debug.Log("[HintsBoxUI] Set icon = " +
                    (ingredient.icon != null ? ingredient.icon.name : "NULL"));
            }
            else
            {
                Debug.LogWarning("[HintsBoxUI] Không tìm thấy Image icon trong Item_NguyenLieu.");
            }

            index++;
        }
    }

    private Image GetIconImage(GameObject itemObj)
    {
        Image[] images = itemObj.GetComponentsInChildren<Image>(true);

        foreach (Image img in images)
        {
            if (img.gameObject != itemObj)
                return img;
        }

        return null;
    }

    private void OnClickBack()
    {
        Debug.Log("Back button clicked in HintsBoxUI.");

        ClearUI();

        if (targetFlavorBoxUI != null)
            targetFlavorBoxUI.ClearUI();

        if (cookingSelectionManager != null)
            cookingSelectionManager.DisableIngredientSelection();

        if (dishBookUI != null)
            dishBookUI.ShowDishList();
        else
            Debug.LogWarning("[HintsBoxUI] Chưa gắn DishBookUI.");
    }
}