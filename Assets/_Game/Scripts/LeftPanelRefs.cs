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

    // ─────────────────────────────────────────────────────────────────────────
    //  KHUÔN THẺ — thứ mà A1 cần
    // ─────────────────────────────────────────────────────────────────────────
    // VÌ SAO phải có khuôn thẻ ở đây chứ không nhân tay thẻ trong scene:
    // bếp phải hiện ĐÚNG số loại người chơi gửi vào (có thể 1, có thể 15). Nhân tay
    // trong scene là cố định số ô — lần sau thêm món/thêm nguyên liệu là thiếu ô lại,
    // và người chơi lại gặp đúng lỗi "16/20 món không đạt 70 điểm".
    //
    // VÌ SAO kiểu là SelectableIngredientCard chứ không IngredientItemUI:
    // thẻ phải BẤM ĐƯỢC. `IngredientItemUI` chỉ vẽ chữ + icon, còn phần nghe click
    // và giữ `IngredientData` nằm ở `SelectableIngredientCard`. Lấy sai kiểu thì thẻ
    // hiện ra đầy đủ nhưng bấm không có gì xảy ra.
    //
    // ĐỂ TRỐNG VẪN CHẠY: `CookingBoot` tự lấy thẻ con đầu tiên đang có trong container
    // làm khuôn (đúng `Item_Ingredient_Beef` / `Item_Seasoning_FishSauce` như thiết kế).
    // Field này để chủ dự án gắn prefab riêng sau này mà không phải sửa code.
    [Header("Card Prefabs (để trống = dùng thẻ đầu tiên trong container làm khuôn)")]
    public SelectableIngredientCard ingredientCardPrefab;
    public SelectableIngredientCard seasoningCardPrefab;
}
