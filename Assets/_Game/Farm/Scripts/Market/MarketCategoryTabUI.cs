using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một viên tab trên dải lọc danh mục dọc bên trái bảng tin.
///
/// Hierarchy đến từ prefab do Editor tool sinh — script chỉ đổ dữ liệu và đổi trạng thái.
///
/// Tab ĐANG CHỌN phải nổi bật RẤT MẠNH (đổi nền sang vàng + phóng to + hiện tên đầy đủ).
/// Nếu chỉ đổi màu nhạt thì trên màn hình điện thoại người chơi không biết mình
/// đang đứng ở mục nào, phải bấm thử từng cái.
/// </summary>
public class MarketCategoryTabUI : MonoBehaviour
{
    [SerializeField] private Image    imageBackground;
    [SerializeField] private Image    imageAccent;     // ô màu đại diện danh mục — CHỖ CHỜ ART (icon)
    [SerializeField] private TMP_Text textShort;       // viết tắt 2 ký tự khi chưa có icon
    [SerializeField] private TMP_Text textLabel;       // tên đầy đủ, chỉ hiện khi được chọn
    [SerializeField] private Button   button;
    [SerializeField] private RectTransform scaleTarget;

    private MarketCategory          category;
    private Action<MarketCategory>  onSelected;

    public MarketCategory Category => category;

    public void Bind(MarketCategory value, Action<MarketCategory> selectCallback)
    {
        category   = value;
        onSelected = selectCallback;

        if (imageAccent != null)
            imageAccent.color = MarketCategoryUtil.GetAccentColor(value);

        if (textShort != null)
            textShort.text = MarketCategoryUtil.GetShortLabel(value);

        if (textLabel != null)
            textLabel.text = MarketCategoryUtil.GetDisplayName(value);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
        }

        SetSelected(false);
    }

    private void HandleClicked()
    {
        onSelected?.Invoke(category);
    }

    public void SetSelected(bool selected)
    {
        if (imageBackground != null)
            imageBackground.color = selected ? MarketBoardPalette.TabSelected : MarketBoardPalette.TabIdle;

        if (textShort != null)
            textShort.color = selected ? MarketBoardPalette.TextOnCard : MarketBoardPalette.TextOnPanel;

        if (textLabel != null)
        {
            textLabel.gameObject.SetActive(selected);
            textLabel.color = MarketBoardPalette.TextOnCard;
        }

        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        if (scaleTarget != null)
            scaleTarget.localScale = selected ? Vector3.one * 1.12f : Vector3.one;
    }
}
