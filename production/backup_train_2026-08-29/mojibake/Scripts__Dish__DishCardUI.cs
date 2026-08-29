using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DishCardUI : MonoBehaviour
{
    [Header("UI")]

    [SerializeField] private Image imgDish;
    [SerializeField] private TMP_Text txtDishName;
    [SerializeField] private Button btnSelect;

    private DishData dishData;
    private Action<DishData> onSelected;

    public void Bind(DishData data, Action<DishData> selectedCallback)
    {
        dishData = data;
        onSelected = selectedCallback;

        if (imgDish != null)
            imgDish.sprite = dishData.dishSprite;

        if (txtDishName != null)
            txtDishName.text = dishData.dishName;

        if (btnSelect != null)
        {
            btnSelect.onClick.RemoveAllListeners();
            btnSelect.onClick.AddListener(OnClick);
        }
    }

    // Khóa/mở card theo level người chơi: khóa thì xám hình và không bấm được
    public void SetLocked(bool locked)
    {
        if (btnSelect != null)
            btnSelect.interactable = !locked;

        if (imgDish != null)
            imgDish.color = locked ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;
    }

    private void OnClick()
    {

        if (dishData == null)
        {
            Debug.LogWarning("DishCardUI chÆ°a cÃ³ DishData.");
            return;
        }

        onSelected?.Invoke(dishData);
    }
}
