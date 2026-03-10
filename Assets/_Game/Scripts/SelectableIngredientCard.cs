using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class SelectableIngredientCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private IngredientData ingredientData;

    public bool isSeasoning;
    public bool IsSelected { get; private set; }

    private CookingSelectionManager manager;

    private GameObject statusGO;
    private string cachedName;
    private Sprite cachedMain;
    private Sprite cachedTop;

    public void Init(CookingSelectionManager mgr, bool seasoning)
    {
        manager = mgr;
        isSeasoning = seasoning;

        CacheFromUI();
        SetSelected(false);
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
        Transform tName = transform.Find("Txt_Name");
        if (tName != null)
        {
            TMP_Text tmp = tName.GetComponent<TMP_Text>();
            if (tmp != null) cachedName = tmp.text;
        }

        Transform tTop = transform.Find("Img_TopIcon");
        if (tTop != null)
        {
            Image img = tTop.GetComponent<Image>();
            if (img != null) cachedTop = img.sprite;
        }

        Transform tMain = transform.Find("Img_MainIcon");
        if (tMain != null)
        {
            Image img = tMain.GetComponent<Image>();
            if (img != null) cachedMain = img.sprite;
        }

        Transform tStatus = transform.Find("Img_Status");
        if (tStatus != null)
            statusGO = tStatus.gameObject;
    }

    public string GetItemName() => cachedName;
    public Sprite GetMainSprite() => cachedMain;
    public Sprite GetTopSprite() => cachedTop;
}