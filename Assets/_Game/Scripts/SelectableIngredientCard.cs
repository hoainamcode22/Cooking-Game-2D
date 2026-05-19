using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Runtime.Serialization;

public class SelectableIngredientCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private IngredientData ingredientData;

    private void Awake()
    {
        // DraggableItem là thành phần bắt buộc để kéo thả hoạt động.
        // Tự thêm vào nếu spawn dynamic từ prefab không có sẵn.
        if (GetComponent<DraggableItem>() == null)
            gameObject.AddComponent<DraggableItem>();
    }

    [Header("UI Refs")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image mainIconImage;
    [SerializeField] private GameObject statusGO;


    public bool isSeasoning;
    public bool IsSelected { get; private set; }

    private CookingSelectionManager manager;

    private string cachedName;
    private Sprite cachedMain;
    private string idItem;

    [SerializeField] private TMP_Text txtQuantity;

    private int currentKitchenQuantity;

    public void SetQuantityFromKitchen(int quantity)
    {
        currentKitchenQuantity = quantity;

        if (txtQuantity != null)
            txtQuantity.text = "x" + currentKitchenQuantity;
    }
    public void setIdItem(string id)
    {
        idItem = id;
    }

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

        if (statusGO == null)
        {
            Transform t = transform.Find("Img_Status");
            if (t != null) statusGO = t.gameObject;
        }
    }

    public void SetIngredientData(IngredientData data)
    {
        ingredientData = data;

        // Đồng bộ sang DraggableItem để cả hai component luôn trỏ đến cùng data.
        DraggableItem drag = GetComponent<DraggableItem>();
        if (drag != null)
            drag.ingredientData = data;
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

        if (cachedMain == null && ingredientData != null)
            cachedMain = ingredientData.icon;

        Debug.Log($"[CARD CACHE] {cachedName} | main={(cachedMain != null ? cachedMain.name : "NULL")}");
    }

    public string GetItemName() => cachedName;
    public int GetQuantity() => currentKitchenQuantity;
    public Sprite GetMainSprite() => cachedMain;
    public string GetItemId() => idItem;
}