using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MỘT Ô TRONG LƯỚI CHỌN VẬT PHẨM (cột giữa của panel trượt — B4).
///
/// Bố cục theo video: tên ở TRÊN, icon ở giữa, **badge số lượng nằm góc dưới phải**.
/// Hỗ trợ chuyển tiếp thao tác kéo vuốt trên PC & Mobile cảm ứng mượt mà.
/// </summary>
public class StallPickItemCellUI : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
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
    private ScrollRect   _parentScrollRect;

    public string ItemId => _itemId;

    private ScrollRect ParentScrollRect
    {
        get
        {
            if (_parentScrollRect == null)
                _parentScrollRect = GetComponentInParent<ScrollRect>();
            return _parentScrollRect;
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnEndDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnScroll(eventData);
    }

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
                ? new Color(1.0f, 0.95f, 0.8f, 1f)
                : Color.white;
        }
    }
}
