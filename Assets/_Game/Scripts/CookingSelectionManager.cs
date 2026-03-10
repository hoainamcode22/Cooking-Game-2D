using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CookingSelectionManager : MonoBehaviour
{
    [Header("Limits")]
    public int maxIngredients = 4;
    public int maxSeasonings = 3;

    [Header("Left UI (counts)")]
    public TMP_Text ingredientsCountText;
    public TMP_Text seasoningsCountText;

    [Header("Pot Containers")]
    public Transform potIngredientsContent;
    public Transform potSeasoningsContent;

    [Header("Pot Card Prefab (mini)")]
    public IngredientItemUI potCardPrefab;

    private readonly List<SelectableIngredientCard> selectedIngredients = new();
    private readonly List<SelectableIngredientCard> selectedSeasonings = new();


    // hàm này sẽ được gọi từ CookingBoot sau khi đã spawn xong tất cả card ở bên trái, nó sẽ tìm tất cả card và gọi Init để thiết lập liên kết với manager, đồng thời đảm bảo nồi hiển thị đúng những gì đã chọn (lúc đầu là chưa chọn gì nên sẽ trống)
    public void RegisterAllLeftCards(Transform ingredientsContent, Transform seasoningsContent)
    {
        selectedIngredients.Clear();
        selectedSeasonings.Clear();

        foreach (Transform t in ingredientsContent)
        {
            var card = t.GetComponent<SelectableIngredientCard>();
            if (card != null)
                card.Init(this, false);
        }

        foreach (Transform t in seasoningsContent)
        {
            var card = t.GetComponent<SelectableIngredientCard>();
            if (card != null)
                card.Init(this, true);
        }

        RebuildPot();
        UpdateCounts();
    }
    // hàm này sẽ thử chọn một card, nó sẽ kiểm tra xem đã chọn tối đa chưa, nếu chưa thì thêm vào danh sách đã chọn và cập nhật lại nồi và số lượng hiển thị
    public void TrySelect(SelectableIngredientCard card)
    {
        if (card == null) return;

        if (card.isSeasoning)
        {
            if (selectedSeasonings.Contains(card)) return;
            if (selectedSeasonings.Count >= maxSeasonings)
            {
                Debug.Log("Đã đạt tối đa gia vị.");
                return;
            }

            selectedSeasonings.Add(card);
            Debug.Log("Đã thêm gia vị: " + card.GetItemName());
        }
        else
        {
            if (selectedIngredients.Contains(card)) return;
            if (selectedIngredients.Count >= maxIngredients)
            {
                Debug.Log("Đã đạt tối đa nguyên liệu.");
                return;
            }

            selectedIngredients.Add(card);
            Debug.Log("Đã thêm nguyên liệu: " + card.GetItemName());
        }

        card.SetSelected(true);
        RebuildPot();
        UpdateCounts();
    }
    // hàm này sẽ bỏ chọn một card đã chọn, nó sẽ xóa khỏi danh sách đã chọn và cập nhật lại nồi và số lượng hiển thị
    public void TryDeselect(SelectableIngredientCard card)
    {
        if (card == null) return;

        if (card.isSeasoning)
        {
            selectedSeasonings.Remove(card);
            Debug.Log("Đã bỏ gia vị: " + card.GetItemName());
        }
        else
        {
            selectedIngredients.Remove(card);
            Debug.Log("Đã bỏ nguyên liệu: " + card.GetItemName());
        }

        card.SetSelected(false);
        RebuildPot();
        UpdateCounts();
    }
    // hàm này sẽ cập nhật lại số lượng đã chọn và hiển thị ở bên trái, nó sẽ hiển thị dạng "Select X/Y" để người chơi biết còn có thể chọn thêm hay không
    private void UpdateCounts()
    {
        if (ingredientsCountText != null)
            ingredientsCountText.text = $"Select {selectedIngredients.Count}/{maxIngredients}";

        if (seasoningsCountText != null)
            seasoningsCountText.text = $"Select {selectedSeasonings.Count}/{maxSeasonings}";
    }
    // hàm này sẽ xóa hết các card nhỏ trong nồi rồi tạo lại dựa trên danh sách đã chọn, đảm bảo nồi luôn hiển thị đúng những gì người chơi đã chọn
    private void RebuildPot()
    {
        ClearChildren(potIngredientsContent);
        ClearChildren(potSeasoningsContent);

        foreach (var c in selectedIngredients)
            SpawnPotCard(potIngredientsContent, c);

        foreach (var c in selectedSeasonings)
            SpawnPotCard(potSeasoningsContent, c);
    }
    // hàm này sẽ tạo một card nhỏ trong nồi dựa trên card đã chọn ở bên trái, nó sẽ lấy tên và hình ảnh từ card gốc để hiển thị
    private void SpawnPotCard(Transform parent, SelectableIngredientCard fromCard)
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
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;

        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
    //hàm này sẽ bỏ chọn tất cả nguyên liệu và gia vị đã chọn, đồng thời cập nhật lại nồi và số lượng hiển thị
    public void ResetSelection()
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
    // hàm này sẽ được gọi khi người chơi nhấn nút Cook, nó sẽ kiểm tra xem đã chọn nguyên liệu nào chưa, nếu có thì in ra danh sách nguyên liệu và gia vị đã chọn, sau đó hiển thị thông báo nấu xong (tạm thời chưa tính điểm)
    public void Cook()
    {
        if (selectedIngredients.Count == 0)
        {
            Debug.Log("Chưa chọn nguyên liệu nào.");
            return;
        }

        Debug.Log("===== COOK START =====");
        Debug.Log("Số nguyên liệu: " + selectedIngredients.Count);
        Debug.Log("Số gia vị: " + selectedSeasonings.Count);

        foreach (var item in selectedIngredients)
        {
            Debug.Log("Nguyên liệu: " + item.GetItemName());
        }

        foreach (var item in selectedSeasonings)
        {
            Debug.Log("Gia vị: " + item.GetItemName());
        }

        Debug.Log("Nấu xong! (tạm thời chưa tính điểm)");
    }
    // hàm này sẽ trả về danh sách các card nguyên liệu đã chọn, nó sẽ tạo một bản sao mới để tránh bị thay đổi từ bên ngoài, đảm bảo tính toàn vẹn của dữ liệu
    public List<SelectableIngredientCard> GetSelectedIngredientCards()
    {
        return new List<SelectableIngredientCard>(selectedIngredients);
    }

    public List<SelectableIngredientCard> GetSelectedSeasoningCards()
    {
        return new List<SelectableIngredientCard>(selectedSeasonings);
    }
}