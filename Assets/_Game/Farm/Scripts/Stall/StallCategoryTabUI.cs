using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MỘT TAB DANH MỤC ở cột trái panel chọn vật phẩm (B4).
///
/// Tab đang chọn phải NỔI BẬT HẲN chứ không chỉ đổi màu nhạt — trong video, icon đang
/// chọn to hơn và sáng hẳn lên, nhờ vậy người chơi không bao giờ nhầm mình đang ở mục nào.
/// Ở đây tái hiện bằng ba tín hiệu chồng lên nhau (màu nền + độ phóng + chữ đậm) để
/// trạng thái vẫn đọc được cả khi người chơi không phân biệt được màu.
/// </summary>
public class StallCategoryTabUI : MonoBehaviour
{
    [Header("Danh mục tab này đại diện")]
    [SerializeField] private StallItemCategory category = StallItemCategory.TatCa;

    [Header("Thành phần")]
    [SerializeField] private Button        button;
    [SerializeField] private Image         imageArtTabBackground;
    [SerializeField] private Image         imageArtCategoryIcon;
    [SerializeField] private TMP_Text      label;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Màu")]
    [SerializeField] private Color colorSelected = new Color(0.18f, 0.75f, 0.66f, 1f);
    [SerializeField] private Color colorNormal   = new Color(0.28f, 0.18f, 0.40f, 1f);

    private StallPopupUI _owner;

    public StallItemCategory Category => category;

    public void Bind(StallPopupUI owner)
    {
        _owner = owner;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (_owner != null) _owner.OnSelectCategory(category);
    }

    public void SetSelected(bool selected)
    {
        if (imageArtTabBackground != null)
            imageArtTabBackground.color = selected ? colorSelected : colorNormal;

        if (imageArtCategoryIcon != null)
            imageArtCategoryIcon.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.55f);

        if (label != null)
        {
            label.color     = selected ? Color.white : new Color(1f, 1f, 1f, 0.65f);
            label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }

        if (scaleTarget != null)
            scaleTarget.localScale = selected ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
    }

#if UNITY_EDITOR
    /// <summary>Editor tool gán danh mục lúc dựng prefab tab.</summary>
    public void EditorSetCategory(StallItemCategory value) => category = value;
#endif
}
