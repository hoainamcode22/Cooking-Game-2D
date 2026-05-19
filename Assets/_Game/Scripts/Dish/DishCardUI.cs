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

    private void OnClick()
    {

        if (dishData == null)
        {
            Debug.LogWarning("DishCardUI chưa có DishData.");
            return;
        }

        Debug.Log("Click Dish Card: " + dishData.dishName);
        onSelected?.Invoke(dishData);
    }
}