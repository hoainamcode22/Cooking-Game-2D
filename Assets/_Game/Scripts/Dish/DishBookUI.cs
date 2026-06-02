using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DishBookUI : MonoBehaviour
{
    [SerializeField] private CookingSelectionManager cookingSelectionManager;
    [Header("UI Root")]
    [SerializeField] private GameObject leftPageRoot;
    [SerializeField] private GameObject rightPageRoot;
    [SerializeField] private GameObject detailPanelRoot;
    [Header("Selected Dish")]
    [SerializeField] private DishData selectedDish;

    [Header("Hint Box UI")]
    [SerializeField] private HintsBoxUI hintsBoxUI;

    [Header("Cooking")]
    [SerializeField] private CookingChallengeManager cookingManager;
    [Header("Data")]
    [SerializeField] private ListDishData listDishData;

    [Header("Page Settings")]
    [SerializeField] private int dishesPerSide = 3;

    [Header("Left Page")]
    [SerializeField] private Transform leftContent;

    [Header("Right Page")]
    [SerializeField] private Transform rightContent;

    [Header("Prefab")]
    [SerializeField] private DishCardUI dishCardPrefab;

    [Header("Buttons")]
    [SerializeField] private Button btnPreviousPage;
    [SerializeField] private Button btnNextPage;

    [Header("Page Text")]
    [SerializeField] private TMP_Text txtPageNumber;


    private int currentPageIndex = 0;

    private int DishesPerPage => dishesPerSide * 2;

    private void Start()
    {
        SetupButtons();
        ShowCurrentPage();
    }

    private void SetupButtons()
    {
        if (btnPreviousPage != null)
        {
            btnPreviousPage.onClick.RemoveAllListeners();
            btnPreviousPage.onClick.AddListener(PreviousPage);
        }

        if (btnNextPage != null)
        {
            btnNextPage.onClick.RemoveAllListeners();
            btnNextPage.onClick.AddListener(NextPage);
        }
    }

    private void ShowCurrentPage()
    {
        ClearContent(leftContent);
        ClearContent(rightContent);

        if (listDishData == null || listDishData.allDishes == null)
        {
            Debug.LogWarning("ChÆ°a gáº¯n ListDishData cho DishBookUI.");
            return;
        }

        List<DishData> dishes = listDishData.allDishes;

        int startIndex = currentPageIndex * DishesPerPage;

        int leftStartIndex = startIndex;
        int rightStartIndex = startIndex + dishesPerSide;

        SpawnDishesToSide(leftContent, dishes, leftStartIndex, dishesPerSide);
        SpawnDishesToSide(rightContent, dishes, rightStartIndex, dishesPerSide);

        UpdatePageUI();
    }
    public void ShowDishList()
    {
        // Hiá»‡n láº¡i 2 trang danh sÃ¡ch
        if (leftPageRoot != null)
            leftPageRoot.SetActive(true);

        if (rightPageRoot != null)
            rightPageRoot.SetActive(true);

        // Hiá»‡n láº¡i nÃºt chuyá»ƒn trang
        if (btnPreviousPage != null)
            btnPreviousPage.gameObject.SetActive(true);

        if (btnNextPage != null)
            btnNextPage.gameObject.SetActive(true);

        if (txtPageNumber != null)
            txtPageNumber.gameObject.SetActive(true);

        // áº¨n chi tiáº¿t mÃ³n
        if (detailPanelRoot != null)
            detailPanelRoot.SetActive(false);

        // Load láº¡i Ä‘Ãºng trang hiá»‡n táº¡i
        ShowCurrentPage();

    }

    private void SpawnDishesToSide(Transform parent, List<DishData> dishes, int startIndex, int count)
    {
        if (parent == null || dishCardPrefab == null)
            return;

        for (int i = 0; i < count; i++)
        {
            int dishIndex = startIndex + i;

            if (dishIndex >= dishes.Count)
                break;

            DishData dish = dishes[dishIndex];

            if (dish == null)
                continue;

            DishCardUI card = Instantiate(dishCardPrefab, parent);
            card.Bind(dish, OnDishSelected);
        }
    }

    private void OnDishSelected(DishData dish)
    {
        if (cookingSelectionManager != null)
            cookingSelectionManager.EnableIngredientSelection();
        if (dish == null)
            return;

        selectedDish = dish;


        // Gá»­i dá»¯ liá»‡u qua HintsBoxUI
        if (hintsBoxUI != null)
        {
            hintsBoxUI.BindDish(selectedDish);
        }
        else
        {
            Debug.LogWarning("ChÆ°a gáº¯n HintsBoxUI vÃ o DishBookUI.");
        }

        // Gá»­i mÃ³n Ä‘ang chá»n qua CookingManager
        if (cookingManager != null)
            cookingManager.SetCurrentDish(selectedDish);

        // áº¨n danh sÃ¡ch
        if (leftPageRoot != null)
            leftPageRoot.SetActive(false);

        if (rightPageRoot != null)
            rightPageRoot.SetActive(false);

        if (btnPreviousPage != null)
            btnPreviousPage.gameObject.SetActive(false);

        if (btnNextPage != null)
            btnNextPage.gameObject.SetActive(false);

        if (txtPageNumber != null)
            txtPageNumber.gameObject.SetActive(false);

        // Hiá»‡n chi tiáº¿t mÃ³n
        if (detailPanelRoot != null)
            detailPanelRoot.SetActive(true);
    }

    private void PreviousPage()
    {
        if (currentPageIndex <= 0)
            return;

        currentPageIndex--;
        ShowCurrentPage();
    }

    private void NextPage()
    {
        int maxPageIndex = GetMaxPageIndex();
        if (currentPageIndex >= maxPageIndex)
            return;

        currentPageIndex++;
        ShowCurrentPage();
    }

    private int GetMaxPageIndex()
    {
        if (listDishData == null || listDishData.allDishes == null || listDishData.allDishes.Count == 0)
            return 0;

        return Mathf.CeilToInt((float)listDishData.allDishes.Count / DishesPerPage) - 1;
    }

    private void UpdatePageUI()
    {
        int maxPageIndex = GetMaxPageIndex();

        if (btnPreviousPage != null)
            btnPreviousPage.interactable = currentPageIndex > 0;

        if (btnNextPage != null)
            btnNextPage.interactable = currentPageIndex < maxPageIndex;

        if (txtPageNumber != null)
            txtPageNumber.text = "Trang " + (currentPageIndex + 1) + " / " + (maxPageIndex + 1);
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null)
            return;

        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
    public DishData GetSelectedDish()
    {
        return selectedDish;
    }
}
