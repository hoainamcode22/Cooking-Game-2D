using TMPro;
using UnityEngine;
using UnityEngine.UI;
// TargetFlavorBoxUI là một component UI chuyên biệt để hiển thị thông tin về món ăn hiện tại và các giá trị vị cần đạt được, nó sẽ được gắn vào một phần của giao diện chính ở trên cùng, nơi người chơi có thể dễ dàng nhìn thấy khi chọn món ăn
public class TargetFlavorBoxUI : MonoBehaviour
{
    [Header("Dish Main UI")]
    [SerializeField] private Image uiDishImage;
    [SerializeField] private TMP_Text txtTodayDishTitle;
    [SerializeField] private TMP_Text txtTargetFlavorTitle;
    [SerializeField] private TMP_Text txtDishNameFull;

    [Header("Flavor Labels")]
    [SerializeField] private TMP_Text txtFlavorLabelSweet;
    [SerializeField] private TMP_Text txtFlavorLabelSpicy;
    [SerializeField] private TMP_Text txtFlavorLabelSour;
    [SerializeField] private TMP_Text txtFlavorLabelUmami;
    [SerializeField] private TMP_Text txtFlavorLabelTexture;

    [Header("Flavor Bars")]
    [SerializeField] private Image barFillSweet;
    [SerializeField] private Image barFillSpicy;
    [SerializeField] private Image barFillSour;
    [SerializeField] private Image barFillUmami;
    [SerializeField] private Image barFillTexture;

    [Header("Flavor Values")]
    [SerializeField] private TMP_Text txtFlavorValueSweet;
    [SerializeField] private TMP_Text txtFlavorValueSpicy;
    [SerializeField] private TMP_Text txtFlavorValueSour;
    [SerializeField] private TMP_Text txtFlavorValueUmami;
    [SerializeField] private TMP_Text txtFlavorValueTexture;

    [Header("Config")]
    [SerializeField] private int maxFlavorValue = 5;

    private void Awake()
    {
        SetStaticTexts();
    }
    // hàm này sẽ thiết lập tất cả text cố định trong UI, nó sẽ được gọi một lần duy nhất trong Awake để đảm bảo rằng các text như tiêu đề và nhãn vị luôn hiển thị đúng, bất kể món ăn nào được chọn
    private void SetStaticTexts()
    {
        if (txtTodayDishTitle != null) txtTodayDishTitle.text = "Today's Dish";
        if (txtTargetFlavorTitle != null) txtTargetFlavorTitle.text = "Target Flavor";

        if (txtFlavorLabelSweet != null) txtFlavorLabelSweet.text = "Sweet";
        if (txtFlavorLabelSpicy != null) txtFlavorLabelSpicy.text = "Spicy";
        if (txtFlavorLabelSour != null) txtFlavorLabelSour.text = "Sour";
        if (txtFlavorLabelUmami != null) txtFlavorLabelUmami.text = "Umami";
        if (txtFlavorLabelTexture != null) txtFlavorLabelTexture.text = "Texture";
    }
    // hàm này sẽ được gọi từ CenterCookingPanelUI mỗi khi có món mới được chọn, nó sẽ cập nhật toàn bộ UI dựa trên thông tin của món ăn đó, bao gồm tên đầy đủ (có thể có phụ đề), hình ảnh và các giá trị vị
    public void BindDish(DishData dishData)
    {
        if (dishData == null)
        {
            ClearUI();
            return;
        }

        string fullDishName = string.IsNullOrEmpty(dishData.dishSubTitle)
            ? dishData.dishName
            : $"{dishData.dishName} ({dishData.dishSubTitle})";

        if (uiDishImage != null)
        {
            uiDishImage.sprite = dishData.dishSprite;
            uiDishImage.preserveAspect = true;
        }

        if (txtDishNameFull != null)
            txtDishNameFull.text = fullDishName;

        SetTargetFlavor(
            dishData.targetFlavor.sweet,
            dishData.targetFlavor.spicy,
            dishData.targetFlavor.sour,
            dishData.targetFlavor.umami,
            dishData.targetFlavor.texture
        );
    }
    // hàm này dùng để thiết lập giá trị của từng loại vị, nó sẽ gọi hàm SetOneFlavor để cập nhật từng loại một, đảm bảo rằng giá trị được giới hạn trong khoảng từ 0 đến maxFlavorValue và cập nhật cả text hiển thị giá trị lẫn fill amount của thanh bar
    public void SetTargetFlavor(int sweet, int spicy, int sour, int umami, int texture)
    {
        SetOneFlavor(barFillSweet, txtFlavorValueSweet, sweet);
        SetOneFlavor(barFillSpicy, txtFlavorValueSpicy, spicy);
        SetOneFlavor(barFillSour, txtFlavorValueSour, sour);
        SetOneFlavor(barFillUmami, txtFlavorValueUmami, umami);
        SetOneFlavor(barFillTexture, txtFlavorValueTexture, texture);
    }
    // hàm này sẽ cập nhật một loại vị cụ thể, nó sẽ đảm bảo rằng giá trị được giới hạn trong khoảng hợp lệ, sau đó cập nhật text hiển thị giá trị và fill amount của thanh bar tương ứng, nếu các tham chiếu không null
    private void SetOneFlavor(Image fillBar, TMP_Text valueText, int value)
    {
        value = Mathf.Clamp(value, 0, maxFlavorValue);

        if (valueText != null)
            valueText.text = value.ToString();

        if (fillBar != null)
            fillBar.fillAmount = (float)value / maxFlavorValue;
    }
    // hàm này sẽ xóa toàn bộ thông tin hiển thị trên UI, nó sẽ được gọi khi không có món nào được chọn hoặc khi cần reset lại UI, nó sẽ đảm bảo rằng hình ảnh được xóa, tên món ăn được làm trống và tất cả giá trị vị được đặt về 0
    public void ClearUI()
    {
        if (uiDishImage != null)
            uiDishImage.sprite = null;

        if (txtDishNameFull != null)
            txtDishNameFull.text = string.Empty;

        SetTargetFlavor(0, 0, 0, 0, 0);
    }
}