using System.Collections.Generic;

using TMPro;
using UnityEngine;

public class CookingSelectionManager : MonoBehaviour
{
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

    // [Header("New Left Panels")]
    // public Transform leftIngredientsContent;
    // public Transform leftSeasoningsContent;

    // [Header("New Pot Panels")]
    // public Transform newPotIngredientsContent;
    // public Transform newPotSeasoningsContent;

    // [Header("New Slot Prefab")]
    // public CookingStackSlotUI stackSlotPrefab;

    // [Header("Cooking Item Database")]
    // public List<InventoryItemData> cookingInventoryItems = new List<InventoryItemData>();

    private readonly List<SelectableIngredientCard> selectedIngredients = new();
    private readonly List<SelectableIngredientCard> selectedSeasonings = new();

    // private readonly Dictionary<string, InventoryItemData> inventoryLookup = new();
    private readonly Dictionary<string, int> leftIngredientAmounts = new();
    private readonly Dictionary<string, int> leftSeasoningAmounts = new();
    private readonly Dictionary<string, int> potIngredientAmounts = new();
    private readonly Dictionary<string, int> potSeasoningAmounts = new();

    public void RegisterAllLeftCards(Transform ingredientsContent, Transform seasoningsContent)// đăng ký tất cả card ở panel bên trái, để có thể tương tác được. Sẽ tìm tất cả card con của container nguyên liệu và gia vị, rồi gọi hàm Init của từng card để đăng ký với selection manager
    {
        selectedIngredients.Clear();
        selectedSeasonings.Clear();
        Debug.Log("RegisterAllLeftCards DA DUOC GOI");

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

    public void TrySelect(SelectableIngredientCard card)// sự kiện khi người chơi nhấn vào một card nguyên liệu hoặc gia vị để chọn, sẽ kiểm tra xem đã đạt giới hạn tối đa chưa, nếu chưa thì thêm vào danh sách đã chọn, và cập nhật lại UI
    {
        if (card == null) return;

        IngredientAmountUI amountUI = card.GetComponent<IngredientAmountUI>();
        if (amountUI == null)
        {
            Debug.LogWarning("Card chưa có IngredientAmountUI: " + card.name);
            return;
        }

        if (amountUI.Amount <= 0)
        {
            Debug.Log("Hết số lượng để chọn.");
            return;
        }

        if (card.isSeasoning)
        {
            if (selectedSeasonings.Count >= maxSeasonings)
            {
                Debug.Log("Đã đạt tối đa gia vị.");
                return;
            }

            amountUI.DecreaseOne();
            selectedSeasonings.Add(card);
            Debug.Log("Đã thêm gia vị: " + card.GetItemName());
        }
        else
        {
            if (selectedIngredients.Count >= maxIngredients)
            {
                Debug.Log("Đã đạt tối đa nguyên liệu.");
                return;
            }

            amountUI.DecreaseOne();
            selectedIngredients.Add(card);
            Debug.Log("Đã thêm nguyên liệu: " + card.GetItemName());
        }
        RebuildPot();
        UpdateCounts();
    }

    public void TryDeselect(SelectableIngredientCard card)// sự kiện khi người chơi nhấn vào một card nguyên liệu hoặc gia vị đã chọn để bỏ chọn, sẽ xóa khỏi danh sách đã chọn, và cập nhật lại UI
    {
        if (card == null) return;

        IngredientAmountUI amountUI = card.GetComponent<IngredientAmountUI>();
        if (amountUI == null)
        {
            Debug.LogWarning("Card chưa có IngredientAmountUI: " + card.name);
            return;
        }

        if (card.isSeasoning)
        {
            if (selectedSeasonings.Contains(card))
            {
                selectedSeasonings.Remove(card);
                amountUI.IncreaseOne();
                Debug.Log("Đã bỏ gia vị: " + card.GetItemName());
            }
        }
        else
        {
            if (selectedIngredients.Contains(card))
            {
                selectedIngredients.Remove(card);
                Debug.Log("So luong hien tai tren UI truoc khi tra ve: " + amountUI.Amount);

                amountUI.IncreaseOne();
                Debug.Log("Đã bỏ nguyên liệu: " + card.GetItemName());
            }
        }
        RebuildPot();
        UpdateCounts();
    }

    private void UpdateCounts()// cập nhật lại số lượng đã chọn hiển thị trên UI, dựa trên số lượng card nguyên liệu và gia vị đã chọn, so với giới hạn tối đa. Nếu có text để hiển thị thì sẽ cập nhật, nếu không có thì sẽ bỏ qua
    {
        int ingredientCount = selectedIngredients.Count;
        int seasoningCount = selectedSeasonings.Count;

        if (ingredientsCountText != null)
            ingredientsCountText.text = $"Chọn {ingredientCount}/{maxIngredients}";

        if (seasoningsCountText != null)
            seasoningsCountText.text = $"Chọn {seasoningCount}/{maxSeasonings}";
    }

    private void RebuildPot()// làm mới lại UI của nồi, dựa trên những card nguyên liệu và gia vị đã chọn. Sẽ xóa hết card con của container nồi, rồi tạo lại card mới dựa trên những card đã chọn, để hiển thị trong nồi
    {
        ClearChildren(potIngredientsContent);
        ClearChildren(potSeasoningsContent);

        foreach (var c in selectedIngredients)
            SpawnPotCard(potIngredientsContent, c);

        foreach (var c in selectedSeasonings)
            SpawnPotCard(potSeasoningsContent, c);
    }

    private void SpawnPotCard(Transform parent, SelectableIngredientCard fromCard)// tạo một card mới trong nồi dựa trên một card đã chọn, để hiển thị trong nồi. Sẽ lấy dữ liệu từ card đã chọn, rồi điền vào card mới, và đặt làm con của container nồi
    {
        if (potCardPrefab == null || parent == null || fromCard == null) return;

        var newUi = Instantiate(potCardPrefab, parent, false);
        newUi.gameObject.SetActive(true);

        RectTransform rt = newUi.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        newUi.Setup(
            fromCard.GetItemName(),
            fromCard.GetMainSprite(),
            fromCard.GetTopSprite(),
            3,
            true
        );
        //thêm component để có thể nhận sự kiện nhấn vào card trong nồi, để bỏ chọn. Sẽ gán manager và card gốc vào component, để khi nhấn vào sẽ gọi hàm TryDeselect của manager với card gốc
        var clickBack = newUi.GetComponent<PotCardClickBack>();
        if (clickBack == null)
            clickBack = newUi.gameObject.AddComponent<PotCardClickBack>();

        clickBack.Init(this, fromCard);
    }

    private void ClearChildren(Transform t)// xóa hết các gameobject con của một transform, để làm mới UI. Sẽ duyệt qua tất cả con của transform, và hủy chúng đi
    {
        if (t == null) return;

        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    public void ResetSelection()// sự kiện khi người chơi nhấn nút reset để xóa hết lựa chọn đã chọn, sẽ bỏ chọn tất cả card đã chọn, xóa hết card trong nồi, và cập nhật lại số lượng đã chọn về 0
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

        Debug.Log("Đã reset toàn bộ lựa chọn.");
    }

    public void Cook()// sự kiện khi người chơi nhấn nút nấu ăn, sẽ kiểm tra xem đã chọn nguyên liệu nào chưa, nếu chưa thì sẽ hiển thị log và không làm gì, nếu đã chọn thì sẽ bắt đầu quy trình nấu ăn, bao gồm phát âm thanh, hiệu ứng, đợi một khoảng thời gian, tính điểm và hiển thị kết quả
    {
        int ingredientCount = GetTotalAmount(potIngredientAmounts);
        int seasoningCount = GetTotalAmount(potSeasoningAmounts);

        if (ingredientCount == 0)
        {
            Debug.Log("Chưa chọn nguyên liệu nào.");
            return;
        }

        Debug.Log("===== COOK START =====");
        Debug.Log("Số nguyên liệu: " + ingredientCount);
        Debug.Log("Số gia vị: " + seasoningCount);

        foreach (var kv in potIngredientAmounts)
        {
            //KitchenTransferManager.Instance.DecreaseItem(kv.Key, kv.Value);
            Debug.Log("Nguyên liệu: " + kv.Key + " x" + kv.Value);

        }
            
        foreach (var kv in potSeasoningAmounts)
        {
            //KitchenTransferManager.Instance.DecreaseItem(kv.Key, kv.Value);
            Debug.Log("Gia vị: " + kv.Key + " x" + kv.Value);
        }

        //Cập nhật số lượng của các phần tử trong kitchenTransferManager khi người chơi nhấn Cook

            

        Debug.Log("Nấu xong! (tạm thời chưa tính điểm)");
    }

    public List<SelectableIngredientCard> GetSelectedIngredientCards()// trả về danh sách card nguyên liệu đã chọn, để có thể sử dụng trong việc tính điểm hoặc hiển thị kết quả. Sẽ trả về một list mới chứa các card đã chọn, để tránh bị thay đổi từ bên ngoài
    {
        return new List<SelectableIngredientCard>(selectedIngredients);
    }

    public List<SelectableIngredientCard> GetSelectedSeasoningCards()// trả về danh sách card gia vị đã chọn, để có thể sử dụng trong việc tính điểm hoặc hiển thị kết quả. Sẽ trả về một list mới chứa các card đã chọn, để tránh bị thay đổi từ bên ngoài
    {
        return new List<SelectableIngredientCard>(selectedSeasonings);
    }

    public Dictionary<string, int> GetSelectedIngredientCounts()
{
    Dictionary<string, int> result = new Dictionary<string, int>();

    foreach (var card in selectedIngredients)
    {
        if (card == null) continue;

        string itemId = card.GetItemId();
        if (string.IsNullOrEmpty(itemId)) continue;

        if (!result.ContainsKey(itemId))
            result[itemId] = 0;

        result[itemId]++;
    }

    return result;
}
    // =========================
    // FLOW MỚI
    // =========================

    // public void LoadTransferredItemsToLeftPanel()// sự kiện khi scene nấu ăn được load lên, sẽ lấy dữ liệu những item đã được chuyển từ scene trước (nếu có), rồi điền vào panel bên trái, để người chơi có thể chọn. Nếu không có dữ liệu nào được chuyển, thì sẽ để trống panel bên trái
    // {
    //     BuildInventoryLookup();

    //     leftIngredientAmounts.Clear();
    //     leftSeasoningAmounts.Clear();
    //     potIngredientAmounts.Clear();
    //     potSeasoningAmounts.Clear();

    //     if (KitchenTransferManager.Instance == null)
    //     {
    //         Debug.LogWarning("Chưa có KitchenTransferManager.");
    //         RebuildNewUI();
    //         return;
    //     }

    //     List<KeyValuePair<string, int>> items = KitchenTransferManager.Instance.GetTransferredItems();

    //     foreach (var kv in items)
    //     {
    //         if (!inventoryLookup.TryGetValue(kv.Key, out InventoryItemData inventoryItem))
    //             continue;

    //         if (inventoryItem == null || inventoryItem.cookingData == null)
    //             continue;

    //         if (inventoryItem.cookingData.kind == IngredientKind.Seasoning)
    //             leftSeasoningAmounts[kv.Key] = kv.Value;
    //         else
    //             leftIngredientAmounts[kv.Key] = kv.Value;
    //     }

    //     RebuildNewUI();
    // }

    // private void BuildInventoryLookup()// xây dựng lại lookup từ list, để dễ tìm kiếm khi cần thiết. Sẽ duyệt qua list item dữ liệu, và thêm vào dictionary lookup với key là itemId, và value là itemData, để có thể tìm kiếm nhanh khi cần thiết
    // {
    //     inventoryLookup.Clear();

    //     for (int i = 0; i < cookingInventoryItems.Count; i++)
    //     {
    //         InventoryItemData item = cookingInventoryItems[i];
    //         if (item == null || string.IsNullOrEmpty(item.itemId))
    //             continue;

    //         if (!inventoryLookup.ContainsKey(item.itemId))
    //             inventoryLookup.Add(item.itemId, item);
    //     }
    // }

    // private void RebuildNewUI()// làm mới lại UI của cả panel bên trái và nồi, dựa trên những item đã chọn và số lượng của chúng. Sẽ xóa hết card con của cả hai panel, rồi tạo lại card mới dựa trên dữ liệu đã chọn, để hiển thị trong UI. Sau đó cập nhật lại số lượng đã chọn hiển thị trên UI
    // {
    //     RebuildAmountPanel(leftIngredientsContent, leftIngredientAmounts, OnLeftIngredientClicked);
    //     RebuildAmountPanel(leftSeasoningsContent, leftSeasoningAmounts, OnLeftSeasoningClicked);
    //     RebuildAmountPanel(newPotIngredientsContent, potIngredientAmounts, OnPotIngredientClicked);
    //     RebuildAmountPanel(newPotSeasoningsContent, potSeasoningAmounts, OnPotSeasoningClicked);

    //     UpdateCounts();
    // }

    // private void RebuildAmountPanel(Transform parent, Dictionary<string, int> source, System.Action<string> clickAction)// làm mới lại một panel hiển thị những item đã chọn và số lượng của chúng, dựa trên một dictionary chứa itemId và amount. Sẽ xóa hết card con của panel, rồi tạo lại card mới dựa trên dữ liệu trong dictionary, để hiển thị trong UI. Mỗi card sẽ có một nút để tương tác, khi nhấn vào sẽ gọi clickAction với itemId tương ứng
    // {
    //     if (parent == null)
    //         return;

    //     ClearChildren(parent);

    //     foreach (var kv in source)
    //     {
    //         if (kv.Value <= 0)
    //             continue;

    //         if (!inventoryLookup.TryGetValue(kv.Key, out InventoryItemData itemData))
    //             continue;

    //         if (stackSlotPrefab == null)
    //             continue;

    //         Sprite iconSprite = itemData.icon;
    //         if (iconSprite == null && itemData.cookingData != null)
    //             iconSprite = itemData.cookingData.icon;

    //         CookingStackSlotUI slot = Instantiate(stackSlotPrefab, parent, false);
    //         slot.gameObject.SetActive(true);
    //         slot.Setup(kv.Key, iconSprite, kv.Value, clickAction);
    //     }
    // }

    // private void OnLeftIngredientClicked(string itemId)// sự kiện khi người chơi nhấn vào một card nguyên liệu trong panel bên trái, sẽ kiểm tra xem đã đạt giới hạn tối đa chưa, nếu chưa thì chuyển một đơn vị của item đó từ panel bên trái vào nồi, và cập nhật lại UI
    // {
    //     if (GetTotalAmount(potIngredientAmounts) >= maxIngredients)
    //     {
    //         Debug.Log("Đã đạt tối đa nguyên liệu.");
    //         return;
    //     }

    //     if (!TryMoveOne(leftIngredientAmounts, potIngredientAmounts, itemId))
    //         return;

    //     RebuildNewUI();
    // }

    // private void OnLeftSeasoningClicked(string itemId)// sự kiện khi người chơi nhấn vào một card gia vị trong panel bên trái, sẽ kiểm tra xem đã đạt giới hạn tối đa chưa, nếu chưa thì chuyển một đơn vị của item đó từ panel bên trái vào nồi, và cập nhật lại UI
    // {
    //     if (GetTotalAmount(potSeasoningAmounts) >= maxSeasonings)
    //     {
    //         Debug.Log("Đã đạt tối đa gia vị.");
    //         return;
    //     }

    //     if (!TryMoveOne(leftSeasoningAmounts, potSeasoningAmounts, itemId))
    //         return;

    //     RebuildNewUI();
    // }

    // private void OnPotIngredientClicked(string itemId)// sự kiện khi người chơi nhấn vào một card nguyên liệu trong nồi, sẽ chuyển một đơn vị của item đó từ nồi về panel bên trái, và cập nhật lại UI
    // {
    //     if (!TryMoveOne(potIngredientAmounts, leftIngredientAmounts, itemId))
    //         return;

    //     RebuildNewUI();
    // }

    // private void OnPotSeasoningClicked(string itemId)// sự kiện khi người chơi nhấn vào một card gia vị trong nồi, sẽ chuyển một đơn vị của item đó từ nồi về panel bên trái, và cập nhật lại UI
    // {
    //     if (!TryMoveOne(potSeasoningAmounts, leftSeasoningAmounts, itemId))
    //         return;

    //     RebuildNewUI();
    // }

    // private bool TryMoveOne(Dictionary<string, int> from, Dictionary<string, int> to, string itemId)// cố gắng chuyển một đơn vị của itemId từ dictionary from sang dictionary to. Sẽ kiểm tra xem itemId có tồn tại trong from và có số lượng lớn hơn 0 không, nếu có thì giảm số lượng trong from đi 1, tăng số lượng trong to lên 1, và trả về true. Nếu không thì trả về false
    // {
    //     if (string.IsNullOrEmpty(itemId))
    //         return false;

    //     if (!from.TryGetValue(itemId, out int value))
    //         return false;

    //     if (value <= 0)
    //         return false;

    //     from[itemId] = value - 1;
    //     if (from[itemId] <= 0)
    //         from.Remove(itemId);

    //     if (!to.ContainsKey(itemId))
    //         to[itemId] = 0;

    //     to[itemId] += 1;
    //     return true;
    // }

    // private void ReturnAllPotItemsToLeft()// sự kiện khi người chơi nhấn nút reset để xóa hết lựa chọn đã chọn, sẽ chuyển tất cả item trong nồi về panel bên trái, và cập nhật lại UI
    // {
    //     MoveAll(potIngredientAmounts, leftIngredientAmounts);
    //     MoveAll(potSeasoningAmounts, leftSeasoningAmounts);

    //     potIngredientAmounts.Clear();
    //     potSeasoningAmounts.Clear();

    //     RebuildNewUI();
    // }

    // private void MoveAll(Dictionary<string, int> from, Dictionary<string, int> to)// chuyển tất cả item từ dictionary from sang dictionary to. Sẽ duyệt qua tất cả cặp key-value trong from, và cộng dồn số lượng vào to, để chuyển hết item từ from sang to
    // {
    //     foreach (var kv in from)
    //     {
    //         if (!to.ContainsKey(kv.Key))
    //             to[kv.Key] = 0;

    //         to[kv.Key] += kv.Value;
    //     }
    // }

    private int GetTotalAmount(Dictionary<string, int> dict)// tính tổng số lượng của tất cả item trong một dictionary, để biết tổng cộng có bao nhiêu item đã chọn. Sẽ duyệt qua tất cả cặp key-value trong dictionary, và cộng dồn số lượng lại để trả về tổng
    {
        int total = 0;

        foreach (var kv in dict)
            total += kv.Value;

        return total;
    }

    public void EnableIngredientSelection()
    {
        // Cho phép người chơi bắt đầu chọn nguyên liệu (sau khi đã chọn món)
        ResetSelection();
    }
}