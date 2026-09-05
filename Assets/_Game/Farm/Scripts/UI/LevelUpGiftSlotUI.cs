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

    /// <summary>
    /// Tự DỰNG NỀN ô quà tròn chuẩn 190x190 đồng bộ với ô Mở Khóa NEW.
    /// </summary>
    public void BuildProcedural(LevelRewardConfig.ItemGift gift)
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        const float size = 190f;
        rt.sizeDelta = new Vector2(size, size);

        // 1. Nền tròn bên trong
        var fillGO = new GameObject("Nen_Tron", typeof(RectTransform));
        var fillRT = (RectTransform)fillGO.transform;
        fillRT.SetParent(rt, false);
        fillRT.anchorMin = fillRT.anchorMax = new Vector2(0.5f, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        fillRT.sizeDelta = new Vector2(size - 22f, size - 22f);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = LoadSpriteSafe("spr_circle_fill");
        fillImg.color = new Color32(252, 246, 235, 255);
        fillImg.raycastTarget = false;

        // 2. Icon vật phẩm ở giữa
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        var iconRT = (RectTransform)iconGO.transform;
        iconRT.SetParent(rt, false);
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0f, 6f);
        iconRT.sizeDelta = new Vector2(size - 56f, size - 56f);
        iconImage = iconGO.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        // 3. Vòng viền tròn ngoài (đồng bộ 100% với ô Mở Khóa NEW)
        var ringGO = new GameObject("Vong_Vien", typeof(RectTransform));
        var ringRT = (RectTransform)ringGO.transform;
        ringRT.SetParent(rt, false);
        ringRT.anchorMin = Vector2.zero;
        ringRT.anchorMax = Vector2.one;
        ringRT.offsetMin = ringRT.offsetMax = Vector2.zero;
        var ringImg = ringGO.AddComponent<Image>();
        ringImg.sprite = LoadSpriteSafe("spr_ring_circle");
        ringImg.raycastTarget = false;

        // 4. Badge Số lượng hình pill ở dưới (Ví dụ: +350, +3, x2...)
        var badgeGO = new GameObject("Nhan_SoLuong", typeof(RectTransform));
        var badgeRT = (RectTransform)badgeGO.transform;
        badgeRT.SetParent(rt, false);
        badgeRT.anchorMin = badgeRT.anchorMax = new Vector2(0.5f, 0f);
        badgeRT.anchoredPosition = new Vector2(0f, 18f);
        badgeRT.sizeDelta = new Vector2(106f, 38f);
        var badgeBg = badgeGO.AddComponent<Image>();
        badgeBg.sprite = LoadSpriteSafe("spr_white_round");
        badgeBg.type = Image.Type.Sliced;
        badgeBg.color = new Color32(230, 95, 25, 255); // Màu cam nâu nổi bật
        badgeBg.raycastTarget = false;

        amountText = CreateLabel(badgeRT, "Amount", Vector2.zero, new Vector2(100f, 32f), 22, Color.white);

        // [B4 — 2026-09-05] Đường legacy (chưa gộp) trước đây KHÔNG tạo nameText → Setup() gán tên
        // vào null, ô quà câm không có chữ. Tạo caption ngay dưới vòng viền (neo tâm, y âm).
        nameText = CreateLabel(rt, "Ten_VatPham", new Vector2(0f, -(size * 0.5f) - 18f),
                               new Vector2(size + 24f, 30f), 20, new Color32(70, 45, 20, 255));

        Setup(gift);
    }

    private static Sprite LoadSpriteSafe(string name)
    {
#if UNITY_EDITOR
        string path = $"Assets/_Game/Farm/Art/UI_LevelUp/{name}.png";
        Sprite spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr != null) return spr;
#endif
        return Resources.Load<Sprite>($"UI_LevelUp/{name}") ?? Resources.Load<Sprite>(name);
    }

    public void Setup(Sprite icon, string displayName, int amount)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        if (amountText != null)
        {
            amountText.text = amount > 1 ? $"+{amount}" : "+1";
        }
        if (nameText != null)
        {
            nameText.text = displayName;
        }
    }

    public void Setup(LevelRewardConfig.ItemGift gift)
    {
        if (gift == null) return;
        Setup(gift.icon, gift.displayName, gift.amount);
    }

    public void ShowPlaceholderTint(Color color)
    {
        if (iconImage == null) return;
        iconImage.sprite = null;
        iconImage.color = color;
        iconImage.enabled = true;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string label, Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        var go = new GameObject(label, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        // [B4 — 2026-09-05] Neo TÂM rõ ràng: RectTransform mới mặc định anchor (0,0)-(1,1) stretch
        // → anchoredPosition/sizeDelta bị hiểu thành offset, chữ lệch khỏi badge/ô.
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
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
