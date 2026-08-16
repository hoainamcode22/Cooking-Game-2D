using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarehouseSlotUI : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Image bgCard;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject badgeRoot;
    [SerializeField] private TMP_Text txtSoLuong;
    [SerializeField] private Button button;

    [Header("Sprite States")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite emptySprite;

    private string currentItemId;
    private Action<string> onClickCallback;
    private bool isCurrentSelected;

    public string CurrentItemId => currentItemId;
    public bool IsEmpty => string.IsNullOrEmpty(currentItemId);

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);

        EnsureComponents();
    }

    private void EnsureComponents()
    {
        if (badgeRoot == null)
        {
            Transform b = transform.Find("Badge_Count");
            if (b != null) badgeRoot = b.gameObject;
        }

        if (txtSoLuong == null)
        {
            if (badgeRoot != null)
                txtSoLuong = badgeRoot.GetComponentInChildren<TMP_Text>(true);
            if (txtSoLuong == null)
                txtSoLuong = GetComponentInChildren<TMP_Text>(true);
        }

        if (icon == null)
        {
            Transform ic = transform.Find("Img_Icon");
            if (ic != null) icon = ic.GetComponent<Image>();
        }

        if (bgCard == null)
            bgCard = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();
    }

    public void SetSprites(Sprite normal, Sprite selected, Sprite empty)
    {
        normalSprite = normal;
        selectedSprite = selected;
        emptySprite = empty;
    }

    public void SetEmpty()
    {
        currentItemId = null;
        onClickCallback = null;
        isCurrentSelected = false;

        EnsureComponents();

        if (bgCard != null)
        {
            if (emptySprite != null)
                bgCard.sprite = emptySprite;
            bgCard.color = Color.white;
        }

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (badgeRoot != null)
            badgeRoot.SetActive(false);

        if (txtSoLuong != null)
            txtSoLuong.text = "";

        if (button != null)
            button.interactable = false;

        gameObject.SetActive(true);
    }

    public void SetData(string itemId, Sprite itemIcon, int amount, bool isSelected, Action<string> clickCallback)
    {
        currentItemId = itemId;
        onClickCallback = clickCallback;
        isCurrentSelected = isSelected;

        EnsureComponents();
        UpdateVisualState();

        if (icon != null)
        {
            icon.sprite = itemIcon;
            icon.enabled = itemIcon != null;
        }

        if (badgeRoot != null)
        {
            badgeRoot.SetActive(amount > 0);
            badgeRoot.transform.SetAsLastSibling(); // Luôn hiển thị trên cùng
        }

        if (txtSoLuong != null)
        {
            txtSoLuong.text = amount.ToString();
            txtSoLuong.color = new Color(1.0f, 0.93f, 0.77f, 1f); // #FFE9BD vàng kem sáng
            txtSoLuong.fontStyle = FontStyles.Bold;
            txtSoLuong.alignment = TextAlignmentOptions.Center;
            txtSoLuong.gameObject.SetActive(true);
            txtSoLuong.transform.SetAsLastSibling();
        }

        if (button != null)
            button.interactable = true;

        gameObject.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        isCurrentSelected = selected;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (bgCard == null) return;

        if (string.IsNullOrEmpty(currentItemId))
        {
            if (emptySprite != null) bgCard.sprite = emptySprite;
        }
        else if (isCurrentSelected)
        {
            if (selectedSprite != null) bgCard.sprite = selectedSprite;
        }
        else
        {
            if (normalSprite != null) bgCard.sprite = normalSprite;
        }
    }

    private void HandleClick()
    {
        if (string.IsNullOrEmpty(currentItemId))
            return;

        onClickCallback?.Invoke(currentItemId);
    }
}