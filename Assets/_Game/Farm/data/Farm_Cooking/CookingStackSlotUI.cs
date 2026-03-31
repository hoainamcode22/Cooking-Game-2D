using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CookingStackSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text txtAmount;

    private string itemId;
    private Action<string> onClick;

    public void Setup(string newItemId, Sprite newIcon, int amount, Action<string> clickCallback)
    {
        itemId = newItemId;
        onClick = clickCallback;

        if (icon != null)
        {
            icon.sprite = newIcon;
            icon.enabled = newIcon != null;
            icon.color = Color.white;
        }

        if (txtAmount != null)
            txtAmount.text = "x" + Mathf.Max(1, amount);

        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        onClick?.Invoke(itemId);
    }
}