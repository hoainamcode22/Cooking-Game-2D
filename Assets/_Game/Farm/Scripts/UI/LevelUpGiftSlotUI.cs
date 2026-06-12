using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI slot hiển thị một vật phẩm quà tặng trong Level-Up Popup.
/// Gắn vào prefab "GiftItemSlot" — một Image icon + TextMeshPro amount.
/// </summary>
public class LevelUpGiftSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image         iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI nameText;

    /// <summary>Thiết lập slot với icon, tên và số lượng.</summary>
    public void Setup(Sprite icon, string displayName, int amount)
    {
        if (iconImage   != null) { iconImage.sprite  = icon; iconImage.enabled = icon != null; }
        if (amountText  != null) amountText.text  = amount > 1 ? $"x{amount}" : "";
        if (nameText    != null) nameText.text    = displayName;
    }

    /// <summary>Thiết lập slot từ LevelRewardConfig.ItemGift.</summary>
    public void Setup(LevelRewardConfig.ItemGift gift)
    {
        Setup(gift.icon, gift.displayName, gift.amount);
    }
}
