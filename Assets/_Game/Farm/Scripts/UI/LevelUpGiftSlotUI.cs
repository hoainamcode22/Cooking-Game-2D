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

    /// <summary>
    /// Tự DỰNG NỀN ô quà bằng code (dùng khi chưa có prefab GiftItemSlot).
    /// Tạo: Image nền + Image icon + Text số lượng + Text tên, rồi đổ dữ liệu.
    /// Bạn chỉ cần thay sprite nền/icon sau trong runtime hierarchy hoặc gán prefab thật.
    /// </summary>
    public void BuildProcedural(LevelRewardConfig.ItemGift gift)
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(108f, 120f);

        // Nền ô quà (placeholder — thay sprite sau)
        var bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color32(255, 236, 195, 255);
        bg.raycastTarget = false;

        // Icon vật phẩm
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        var iconRT = (RectTransform)iconGO.transform;
        iconRT.SetParent(rt, false);
        iconRT.anchoredPosition = new Vector2(0f, 16f);
        iconRT.sizeDelta = new Vector2(66f, 66f);
        iconImage = iconGO.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        // Tên + số lượng
        nameText   = CreateLabel(rt, "Name",   new Vector2(0f, -32f), new Vector2(102f, 30f), 15, new Color32(108, 64, 34, 255));
        amountText = CreateLabel(rt, "Amount", new Vector2(0f, -52f), new Vector2(102f, 24f), 18, new Color32(85, 49, 25, 255));

        Setup(gift);
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string label, Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        var go = new GameObject(label, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }
}
