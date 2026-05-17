using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class SelectableIngredientCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private IngredientData ingredientData;

    [Header("UI Refs")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image mainIconImage;
    [SerializeField] private Image topIconImage;
    [SerializeField] private GameObject statusGO;
    private InventoryItemData inventoryItem;

    public bool isSeasoning;
    public bool IsSelected { get; private set; }

    private CookingSelectionManager manager;

    private string cachedName;
    private Sprite cachedMain;
    private Sprite cachedTop;

    public void Init(CookingSelectionManager mgr, bool seasoning)
    {
        manager = mgr;
        isSeasoning = seasoning;

        ResolveRefsIfMissing();
        CacheFromUI();
        SetSelected(false);
    }

    private void ResolveRefsIfMissing()
    {
        if (nameText == null)
        {
            Transform t = transform.Find("Txt_Name");
            if (t != null) nameText = t.GetComponent<TMP_Text>();
        }

        if (mainIconImage == null)
        {
            Transform t = transform.Find("Img_MainIcon");
            if (t != null) mainIconImage = t.GetComponent<Image>();
        }

        if (topIconImage == null)
        {
            Transform t = transform.Find("Img_TopIcon");
            if (t != null) topIconImage = t.GetComponent<Image>();
        }

        if (statusGO == null)
        {
            Transform t = transform.Find("Img_Status");
            if (t != null) statusGO = t.gameObject;
        }
    }

    public void SetIngredientData(IngredientData data)
    {
        ingredientData = data;
    }

    public IngredientData GetIngredientData()
    {
        return ingredientData;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager == null) return;

        if (IsSelected)
            manager.TryDeselect(this);
        else
            manager.TrySelect(this);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (statusGO != null) statusGO.SetActive(selected);
    }

    private void CacheFromUI()
    {
        cachedName = nameText != null ? nameText.text : "";
        cachedMain = mainIconImage != null ? mainIconImage.sprite : null;
        cachedTop = topIconImage != null ? topIconImage.sprite : null;

        if (cachedMain == null && ingredientData != null)
            cachedMain = ingredientData.icon;

        Debug.Log($"[CARD CACHE] {cachedName} | main={(cachedMain != null ? cachedMain.name : "NULL")} | top={(cachedTop != null ? cachedTop.name : "NULL")}");
    }

    public string GetItemName() => cachedName;
    public Sprite GetMainSprite() => cachedMain;
    public Sprite GetTopSprite() => cachedTop;
    public void SetInventoryItem(InventoryItemData item)
{
    inventoryItem = item;
    ingredientData = item != null ? item.cookingData : null;
}

    public InventoryItemData GetInventoryItem()
    {
        return inventoryItem;
    }

    public string GetItemId()
    {
        return inventoryItem != null ? inventoryItem.itemId : "";
    }
}