using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MỘT Ô TRONG LƯỚI CHỌN VẬT PHẨM (cột giữa của panel trượt — B4).
///
/// Bố cục theo video: tên ở TRÊN, icon ở giữa, **badge số lượng nằm góc dưới phải**.
/// Badge phải luôn thấy được: nó là thứ duy nhất cho biết còn bao nhiêu để bán, và
/// cũng là cách người chơi nhận ra vật phẩm sắp biến mất khỏi lưới (B6).
/// </summary>
public class StallPickItemCellUI : MonoBehaviour
{
    [Header("Nội dung")]
    [SerializeField] private Image    imageIcon;
    [SerializeField] private TMP_Text textName;
    [SerializeField] private TMP_Text textAmount;
    [SerializeField] private Button   button;

    [Header("Chỗ chờ art")]
    [SerializeField] private Image      imageArtCellBackground;
    [SerializeField] private GameObject selectedFrame;

    private StallPopupUI _owner;
    private string       _itemId;

    public string ItemId => _itemId;

    public void Bind(StallPopupUI owner, string itemId, int amount)
    {
        _owner  = owner;
        _itemId = itemId;

        StallItemCatalog catalog = StallItemCatalog.Instance;

        if (imageIcon != null)
        {
            Sprite icon = catalog != null ? catalog.GetIcon(itemId) : null;
            imageIcon.sprite  = icon;
            imageIcon.enabled = icon != null;
        }

        if (textName != null)
            textName.text = catalog != null ? catalog.GetDisplayName(itemId) : itemId;

        if (textAmount != null)
            textAmount.text = amount.ToString();

        if (button != null)
        {
            // Dọn listener cũ: ô được TÁI SỬ DỤNG giữa các lần lọc danh mục, nên cùng
            // một ô sẽ lần lượt đại diện cho nhiều vật phẩm khác nhau. Không dọn thì
            // bấm một cái sẽ chọn luôn cả những vật phẩm nó từng đại diện trước đó.
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        SetSelected(false);
    }

    private void OnClick()
    {
        if (_owner != null) _owner.OnPickItem(_itemId);
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null) selectedFrame.SetActive(selected);

        if (imageArtCellBackground != null)
        {
            imageArtCellBackground.color = selected
                ? new Color(0.18f, 0.75f, 0.66f, 1f)   // ngọc lam — màu nhấn của bộ quầy hàng
                : new Color(0.24f, 0.15f, 0.35f, 1f);
        }
    }
}
