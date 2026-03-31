using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarehouseSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text txtSoLuong;
    [SerializeField] private Button button;

    private string currentItemId;
    private Action<string> onClickCallback;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    public void SetEmpty()
    {
        currentItemId = null;
        onClickCallback = null;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (txtSoLuong != null)
            txtSoLuong.text = "";

        if (button != null)
            button.interactable = false;

        gameObject.SetActive(true);
    }

    public void SetData(Sprite itemIcon, int amount)
    {
        if (icon != null)
        {
            icon.sprite = itemIcon;
            icon.enabled = itemIcon != null;
        }

        if (txtSoLuong != null)
            txtSoLuong.text = "x" + amount;

        if (button != null)
            button.interactable = amount > 0;

        gameObject.SetActive(true);
    }

    public void SetData(string itemId, Sprite itemIcon, int amount, Action<string> clickCallback)
    {
        currentItemId = itemId;
        onClickCallback = clickCallback;

        SetData(itemIcon, amount);
    }

    private void HandleClick()
    {
        if (string.IsNullOrEmpty(currentItemId))
            return;

        onClickCallback?.Invoke(currentItemId);
    }
}