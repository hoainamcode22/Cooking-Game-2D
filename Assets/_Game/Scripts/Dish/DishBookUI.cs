using System.Collections;
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
        if (leftPageRoot != null && rightPageRoot != null)
        {
            if (btnPreviousPage != null) btnPreviousPage.interactable = false;
            if (btnNextPage != null) btnNextPage.interactable = false;

            StartCoroutine(AnimatePageFlip());
        }
        else
        {
            LoadPageData();
            UpdatePageUI();
        }
    }

    private IEnumerator AnimatePageFlip()
    {
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 0f, t);
            leftPageRoot.transform.localScale = new Vector3(scale, 1, 1);
            rightPageRoot.transform.localScale = new Vector3(scale, 1, 1);
            yield return null;
        }

        leftPageRoot.transform.localScale = new Vector3(0, 1, 1);
        rightPageRoot.transform.localScale = new Vector3(0, 1, 1);

        LoadPageData();

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1f, t) + Mathf.Sin(t * Mathf.PI) * 0.2f;
            leftPageRoot.transform.localScale = new Vector3(scale, 1, 1);
            rightPageRoot.transform.localScale = new Vector3(scale, 1, 1);
            yield return null;
        }

        leftPageRoot.transform.localScale = Vector3.one;
        rightPageRoot.transform.localScale = Vector3.one;

        UpdatePageUI();
    }

    private void LoadPageData()
    {
        ClearContent(leftContent);
        ClearContent(rightContent);

        if (listDishData == null || listDishData.allDishes == null)
        {
            return;
        }

        List<DishData> dishes = listDishData.allDishes;
        int startIndex = currentPageIndex * DishesPerPage;
        int leftStartIndex = startIndex;
        int rightStartIndex = startIndex + dishesPerSide;

        SpawnDishesToSide(leftContent, dishes, leftStartIndex, dishesPerSide);
        SpawnDishesToSide(rightContent, dishes, rightStartIndex, dishesPerSide);
    }
    public void ShowDishList()
    {
        if (leftPageRoot != null)
        {
            leftPageRoot.SetActive(true);
            CanvasGroup leftCG = leftPageRoot.GetComponent<CanvasGroup>();
            if (leftCG != null) leftCG.alpha = 1;
            leftPageRoot.transform.localScale = Vector3.one;
        }

        if (rightPageRoot != null)
        {
            rightPageRoot.SetActive(true);
            CanvasGroup rightCG = rightPageRoot.GetComponent<CanvasGroup>();
            if (rightCG != null) rightCG.alpha = 1;
            rightPageRoot.transform.localScale = Vector3.one;
        }

        if (btnPreviousPage != null) btnPreviousPage.gameObject.SetActive(true);
        if (btnNextPage != null) btnNextPage.gameObject.SetActive(true);
        if (txtPageNumber != null) txtPageNumber.gameObject.SetActive(true);
        if (detailPanelRoot != null) detailPanelRoot.SetActive(false);

        ShowCurrentPage();
    }

    private void SpawnDishesToSide(Transform parent, List<DishData> dishes, int startIndex, int count)
    {
        if (parent == null || dishCardPrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            int dishIndex = startIndex + i;
            if (dishIndex >= dishes.Count) break;

            DishData dish = dishes[dishIndex];
            if (dish == null) continue;

            DishCardUI card = Instantiate(dishCardPrefab, parent);
            card.Bind(dish, OnDishSelected);

            bool unlocked = PlayerProgressManager.Instance == null || PlayerProgressManager.Instance.Level >= dish.unlockLevel;
            card.SetLocked(!unlocked);
        }
    }

    private void OnDishSelected(DishData dish)
    {
        if (cookingSelectionManager != null) cookingSelectionManager.EnableIngredientSelection();
        if (dish == null) return;

        selectedDish = dish;

        if (hintsBoxUI != null) hintsBoxUI.BindDish(selectedDish);

        if (cookingManager != null) cookingManager.SetCurrentDish(selectedDish);

        StartCoroutine(FadeOutPages(0.3f));

        if (btnPreviousPage != null) btnPreviousPage.gameObject.SetActive(false);
        if (btnNextPage != null) btnNextPage.gameObject.SetActive(false);
        if (txtPageNumber != null) txtPageNumber.gameObject.SetActive(false);

        if (detailPanelRoot != null)
        {
            detailPanelRoot.SetActive(true);
            detailPanelRoot.transform.localScale = Vector3.zero;
            StartCoroutine(BounceInDetailPanel(0.4f));
        }
    }

    private IEnumerator FadeOutPages(float duration)
    {
        float elapsed = 0f;
        CanvasGroup leftCG = null;
        CanvasGroup rightCG = null;

        if (leftPageRoot != null)
        {
            leftCG = leftPageRoot.GetComponent<CanvasGroup>();
            if (leftCG == null) leftCG = leftPageRoot.AddComponent<CanvasGroup>();
        }

        if (rightPageRoot != null)
        {
            rightCG = rightPageRoot.GetComponent<CanvasGroup>();
            if (rightCG == null) rightCG = rightPageRoot.AddComponent<CanvasGroup>();
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(1f, 0f, t);
            float scale = Mathf.Lerp(1f, 0.9f, t);

            if (leftCG != null) { leftCG.alpha = alpha; leftPageRoot.transform.localScale = new Vector3(scale, scale, 1); }
            if (rightCG != null) { rightCG.alpha = alpha; rightPageRoot.transform.localScale = new Vector3(scale, scale, 1); }

            yield return null;
        }

        if (leftPageRoot != null) leftPageRoot.SetActive(false);
        if (rightPageRoot != null) rightPageRoot.SetActive(false);
    }

    private IEnumerator BounceInDetailPanel(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale;
            float n1 = 7.5625f;
            float d1 = 2.75f;
            if (t < 1f / d1) scale = n1 * t * t;
            else if (t < 2f / d1) scale = n1 * (t -= 1.5f / d1) * t + 0.75f;
            else if (t < 2.5f / d1) scale = n1 * (t -= 2.25f / d1) * t + 0.9375f;
            else scale = n1 * (t -= 2.625f / d1) * t + 0.984375f;

            detailPanelRoot.transform.localScale = new Vector3(scale, scale, 1);
            yield return null;
        }
        detailPanelRoot.transform.localScale = Vector3.one;
    }

    private void PreviousPage()
    {
        if (currentPageIndex <= 0) return;
        currentPageIndex--;
        ShowCurrentPage();
    }

    private void NextPage()
    {
        int maxPageIndex = GetMaxPageIndex();
        if (currentPageIndex >= maxPageIndex) return;
        currentPageIndex++;
        ShowCurrentPage();
    }

    private int GetMaxPageIndex()
    {
        if (listDishData == null || listDishData.allDishes == null || listDishData.allDishes.Count == 0) return 0;
        return Mathf.CeilToInt((float)listDishData.allDishes.Count / DishesPerPage) - 1;
    }

    private void UpdatePageUI()
    {
        int maxPageIndex = GetMaxPageIndex();
        if (btnPreviousPage != null) btnPreviousPage.interactable = currentPageIndex > 0;
        if (btnNextPage != null) btnNextPage.interactable = currentPageIndex < maxPageIndex;
        if (txtPageNumber != null) txtPageNumber.text = "Trang " + (currentPageIndex + 1) + " / " + (maxPageIndex + 1);
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        foreach (Transform child in parent) Destroy(child.gameObject);
    }
    public DishData GetSelectedDish() { return selectedDish; }
}
