using UnityEngine;
using UnityEngine.UI;

public class MarketShopItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image imageIcon;
    [SerializeField] private Text textName;
    [SerializeField] private Text textQuantity;
    [SerializeField] private Text textPrice;
    [SerializeField] private Button buttonBuy;
    [SerializeField] private GameObject panelSoldOut;

    private string itemID;
    private int quantity;
    private int totalPrice;
    private MarketManager owner;

    public void Setup(MarketManager manager, MarketItemDef itemDef, int rolledQuantity, Sprite icon, string displayName)
    {
        owner = manager;
        itemID = itemDef.ItemID;
        quantity = rolledQuantity;
        totalPrice = itemDef.BuyPrice * rolledQuantity;

        if (imageIcon != null)
        {
            imageIcon.sprite = icon;
            imageIcon.enabled = icon != null;
        }

        if (textName != null)
            textName.text = string.IsNullOrEmpty(displayName) ? itemID : displayName;

        if (textQuantity != null)
            textQuantity.text = "x" + quantity;

        if (textPrice != null)
            textPrice.text = totalPrice.ToString();

        if (panelSoldOut != null)
            panelSoldOut.SetActive(false);

        if (buttonBuy != null)
        {
            buttonBuy.onClick.RemoveAllListeners();
            buttonBuy.interactable = true;
            buttonBuy.onClick.AddListener(Buy);
        }
    }

    public void MarkSoldOut()
    {
        if (panelSoldOut != null)
            panelSoldOut.SetActive(true);

        if (buttonBuy != null)
            buttonBuy.interactable = false;
    }

    private void Buy()
    {
        if (owner != null)
            owner.TryBuy(this, itemID, quantity, totalPrice);
    }
}
