using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CookingSelectionManager : MonoBehaviour
{
    [Header("Current Flavor UI")]
    [SerializeField] private CurrentFlavorBoxUI currentFlavor;

    [Header("Current Total Flavor")]
    [SerializeField] private FlavorVector currentFlavorVector = FlavorVector.Zero;
    [Header("Limits")]
    public int maxIngredients = 4;
    public int maxSeasonings = 3;

    [Header("Left UI (counts)")]
    public TMP_Text ingredientsCountText;
    public TMP_Text seasoningsCountText;

    [Header("Old Pot Containers")]
    public Transform potIngredientsContent;
    public Transform potSeasoningsContent;

    [Header("Old Pot Card Prefab (mini)")]
    public IngredientItemUI potCardPrefab;

    // ─────────────────────────────────────────────────────────────────────────
    //  C11 — ĐÃ XOÁ 6 FIELD KHÔNG MỘT DÒNG CODE NÀO ĐỌC
    // ─────────────────────────────────────────────────────────────────────────
    //   leftIngredientsContent · leftSeasoningsContent
    //   newPotIngredientsContent · newPotSeasoningsContent
    //   stackSlotPrefab (kiểu `CookingStackSlotUI` — class cũng đã xoá ở C9)
    //   cookingInventoryItems
    //
    // VÌ SAO nguy hiểm chứ không chỉ vô dụng: trong `SampleScene` chúng ĐÃ ĐƯỢC GÁN, và
    // gán SAI. `leftIngredientsContent` trỏ vào chính THẺ `Item_Ingredient_Beef` thay vì
    // trỏ vào `Content_Ingredients`. Ai tin vào Inspector rồi viết code dùng field đó sẽ
    // sinh thẻ vào bên trong một cái thẻ khác. Cột thẻ bên trái do `CookingBoot` +
    // `leftRefs` (LeftPanelRefs) quản, và `RegisterAllLeftCards(...)` nhận container qua
    // THAM SỐ — không đọc field nào của class này.
    //
    // `cookingInventoryItems` cũng bị xoá: bản dùng thật nằm ở `CookingBoot`. Hai danh sách
    // song song là chắc chắn lệch — A3 (thêm `Item_Milk`) chỉ cần sửa MỘT chỗ.

    private readonly List<SelectableIngredientCard> selectedIngredients = new();
    private readonly List<SelectableIngredientCard> selectedSeasonings = new();

    private bool canSelectIngredient = false;

    public void RegisterAllLeftCards(Transform ingredientsContent, Transform seasoningsContent)
    {
        selectedIngredients.Clear();
        selectedSeasonings.Clear();

        foreach (Transform t in ingredientsContent)
        {
            if (!t.gameObject.activeSelf) continue;

            var card = t.GetComponent<SelectableIngredientCard>();
            if (card != null)
                card.Init(this, false);
        }

        foreach (Transform t in seasoningsContent)
        {
            if (!t.gameObject.activeSelf) continue;

            var card = t.GetComponent<SelectableIngredientCard>();
            if (card != null)
                card.Init(this, true);
        }

        RebuildPot();
        UpdateCounts();
    }
    public void EnableIngredientSelection()
    {
        canSelectIngredient = true;
    }

    public void DisableIngredientSelection()
    {
        canSelectIngredient = false;
    }

    public void TrySelect(SelectableIngredientCard card)
    {
        if(!canSelectIngredient)
            return;
        if (card == null) return;

        int quantity = card.GetQuantity();

        if (quantity <= 0)
        {
            return;
        }


        if (card.isSeasoning)
        {
            if (selectedSeasonings.Contains(card)) return;

            if (selectedSeasonings.Count >= maxSeasonings)
            {
                return;
            }

            selectedSeasonings.Add(card);

            AddFlavor(card.GetIngredientData());
        }
        else
        {
            if (selectedIngredients.Contains(card)) return;

            if (selectedIngredients.Count >= maxIngredients)
            {
                return;
            }

            selectedIngredients.Add(card);

            // NguyÃªn liá»‡u cÅ©ng cÃ³ hÆ°Æ¡ng vá»‹ nÃªn cÅ©ng pháº£i cá»™ng vector
            AddFlavor(card.GetIngredientData());
        }

        card.SetQuantityFromKitchen(quantity - 1);
        card.SetSelected(true);

        AnimateThrow(card);

        RebuildPot();
        UpdateCounts();
    }

    private void AnimateThrow(SelectableIngredientCard card)
    {
        GameObject flyingObj = new GameObject("FlyingIngredient");
        flyingObj.transform.SetParent(this.transform, false);
        Image img = flyingObj.AddComponent<Image>();
        img.sprite = card.GetMainSprite();
        flyingObj.transform.position = card.transform.position;
        Vector3 targetPos = (card.isSeasoning ? potSeasoningsContent.position : potIngredientsContent.position);
        StartCoroutine(ParabolaRoutine(flyingObj, flyingObj.transform.position, targetPos));
    }

    private System.Collections.IEnumerator ParabolaRoutine(GameObject obj, Vector3 start, Vector3 end)
    {
        float t = 0;
        float duration = 0.4f;
        while(t < duration)
        {
            t += Time.deltaTime;
            float normalizedT = t / duration;
            Vector3 lerpPos = Vector3.Lerp(start, end, normalizedT);
            lerpPos.y += Mathf.Sin(normalizedT * Mathf.PI) * 200f; // Parabola height
            if(obj != null)
                obj.transform.position = lerpPos;
            yield return null;
        }
        if (obj != null) Destroy(obj);
        CreateWaterSplash(end);
    }

    private void CreateWaterSplash(Vector3 pos)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject splash = new GameObject("Splash");
            splash.transform.SetParent(this.transform, false);
            splash.transform.position = pos;
            Image img = splash.AddComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            splash.transform.localScale = Vector3.one * 0.4f;
            StartCoroutine(SplashRoutine(splash));
        }
    }

    private System.Collections.IEnumerator SplashRoutine(GameObject obj)
    {
        Vector3 start = obj.transform.position;
        Vector3 randomOffset = new Vector3(Random.Range(-80f, 80f), Random.Range(50f, 150f), 0);
        Vector3 end = start + randomOffset;
        float t = 0;
        float duration = 0.4f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalizedT = t / duration;
            Vector3 lerpPos = Vector3.Lerp(start, end, normalizedT);
            lerpPos.y -= Mathf.Pow(normalizedT, 2) * 150f; // Gravity approximation
            if(obj != null) {
                obj.transform.position = lerpPos;
                Image img = obj.GetComponent<Image>();
                Color c = img.color;
                c.a = 1f - normalizedT;
                img.color = c;
            }
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    public void TryDeselect(SelectableIngredientCard card)
    {
        if(!canSelectIngredient)
            return;
        if (card == null) return;

        int quantity = card.GetQuantity();

        if (card.isSeasoning)
        {
            if (!selectedSeasonings.Contains(card)) return;

            selectedSeasonings.Remove(card);

            RemoveFlavor(card.GetIngredientData());
        }
        else
        {
            if (!selectedIngredients.Contains(card)) return;

            selectedIngredients.Remove(card);

            // Bá» nguyÃªn liá»‡u thÃ¬ pháº£i trá»« vector hÆ°Æ¡ng vá»‹
        RemoveFlavor(card.GetIngredientData());        }

        card.SetQuantityFromKitchen(quantity + 1);
        card.SetSelected(false);

        RebuildPot();
        UpdateCounts();
    }

    private void UpdateCounts()
    {
        int ingredientCount = selectedIngredients.Count;
        int seasoningCount = selectedSeasonings.Count;

        if (ingredientsCountText != null)
            ingredientsCountText.text = $"Chọn {ingredientCount}/{maxIngredients}";

        if (seasoningsCountText != null)
            seasoningsCountText.text = $"Chọn {seasoningCount}/{maxSeasonings}";
    }

    private void RebuildPot()
    {
        ClearChildren(potIngredientsContent);
        ClearChildren(potSeasoningsContent);

        foreach (var c in selectedIngredients)
            SpawnPotCard(potIngredientsContent, c);

        foreach (var c in selectedSeasonings)
            SpawnPotCard(potSeasoningsContent, c);
    }

    private void SpawnPotCard(Transform parent, SelectableIngredientCard fromCard)// Táº¡o má»™t card nhá» trong ná»“i dá»±a trÃªn card Ä‘Ã£ chá»n á»Ÿ bÃªn trÃ¡i
    {
        if (potCardPrefab == null || parent == null || fromCard == null) return;

        var newUi = Instantiate(potCardPrefab, parent, false);// Táº¡o má»™t card nhá» trong ná»“i dá»±a trÃªn card Ä‘Ã£ chá»n á»Ÿ bÃªn trÃ¡i
        newUi.gameObject.SetActive(true);

        RectTransform rt = newUi.GetComponent<RectTransform>();// Äáº£m báº£o scale vÃ  position Ä‘Ãºng
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        newUi.Setup(
            fromCard.GetItemName(),
            fromCard.GetMainSprite(),
            fromCard.GetTopSprite(),
            true
        );
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;

        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    public void ResetSelection()
    {

        foreach (var card in selectedIngredients)
        {
            if (card != null)
                card.SetSelected(false);
            card.SetQuantityFromKitchen(card.GetQuantity() + 1);
        }

        foreach (var card in selectedSeasonings)
        {
            if (card != null)
                card.SetSelected(false);
            card.SetQuantityFromKitchen(card.GetQuantity() + 1);
        }
        
        selectedIngredients.Clear();
        selectedSeasonings.Clear();
        RebuildPot();
        UpdateCounts();
        ResetFlavor();

    }
    public void ResetUIAfterCooking()
    {

        foreach (var card in selectedIngredients)
        {
            if (card != null)
                card.SetSelected(false);
        }

        foreach (var card in selectedSeasonings)
        {
            if (card != null)
                card.SetSelected(false);
        }
        
        selectedIngredients.Clear();
        selectedSeasonings.Clear();
        RebuildPot();
        UpdateCounts();
        ResetFlavor();
    }


    public List<SelectableIngredientCard> GetSelectedIngredientCards()
    {
        return new List<SelectableIngredientCard>(selectedIngredients);
    }

    public List<SelectableIngredientCard> GetSelectedSeasoningCards()
    {
        return new List<SelectableIngredientCard>(selectedSeasonings);
    }


    // C11 — đã xoá `GetTotalAmount(dict)` cùng hai dictionary `potIngredientAmounts` /
    // `potSeasoningAmounts` mà nó cộng: cả ba đều không có nơi nào gọi/ghi. Nồi được dựng
    // lại từ `selectedIngredients` / `selectedSeasonings` trong `RebuildPot()`.

    private void AddFlavor(IngredientData data)
    {
        if (data == null) return;

        currentFlavorVector += data.vector;
        UpdateCurrentFlavorUI();
    }

    private void RemoveFlavor(IngredientData data)
    {
        if (data == null) return;

        currentFlavorVector -= data.vector;
        UpdateCurrentFlavorUI();
    }

    private void UpdateCurrentFlavorUI()
    {
        if (currentFlavor != null)
            currentFlavor.SetFlavor(currentFlavorVector);
    }

    public void ResetFlavor()
    {
        currentFlavorVector = FlavorVector.Zero;
        UpdateCurrentFlavorUI();
    }
}
