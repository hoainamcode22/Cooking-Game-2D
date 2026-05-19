using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một slot trong Panel_Dish.
/// Hiển thị icon + tên + số lượng của một món ăn đã nấu trong kho.
/// Click → gọi FarmInventoryManager.AddItem(dishId, 1) nếu cấu hình addOnClick = true.
/// </summary>
public class DishResultSlotUI : MonoBehaviour
{
    [SerializeField] private Image     iconImage;
    [SerializeField] private TMP_Text  nameText;
    [SerializeField] private TMP_Text  amountText;
    [SerializeField] private Button    slotButton;

    private string dishId;

    private void Awake()
    {
        if (slotButton != null)
            slotButton.onClick.AddListener(OnClick);
    }

    public void SetData(DishData dish, int amount)
    {
        if (dish == null) { gameObject.SetActive(false); return; }

        dishId = dish.dishId;
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite  = dish.dishSprite;
            iconImage.enabled = dish.dishSprite != null;
        }

        if (nameText != null)
            nameText.text = dish.dishName;

        if (amountText != null)
            amountText.text = amount > 0 ? "x" + amount : "";
    }

    public void SetEmpty()
    {
        dishId = null;
        gameObject.SetActive(false);
    }

    private void OnClick()
    {
        if (string.IsNullOrEmpty(dishId)) return;
        if (FarmInventoryManager.Instance == null) return;

        FarmInventoryManager.Instance.AddItem(dishId, 1);
        Debug.Log($"[DishResultSlotUI] Thêm vào kho: {dishId}");
    }
}
