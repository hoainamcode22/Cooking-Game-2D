using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một viên tab trên dải lọc danh mục dọc bên trái bảng tin chợ.
/// Thiết kế mở rộng hiển thị đầy đủ cả Icon và Tên chữ to rõ ràng mọi lúc.
/// </summary>
public class MarketCategoryTabUI : MonoBehaviour
{
    private const string PerfectSvgDir  = "Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites";
    private const string MarketArtDir   = "Assets/_Game/Farm/Art/UI_MarketBoard";

    [SerializeField] private Image    imageBackground;
    [SerializeField] private Image    imageAccent;     // Icon hình vẽ danh mục
    [SerializeField] private TMP_Text textShort;       // viết tắt dự phòng
    [SerializeField] private TMP_Text textLabel;       // tên đầy đủ danh mục (luôn hiện)
    [SerializeField] private Button   button;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Sprite Tabs")]
    [SerializeField] private Sprite tabActiveSprite;
    [SerializeField] private Sprite tabInactiveSprite;

    private MarketCategory          category;
    private Action<MarketCategory>  onSelected;

    public MarketCategory Category => category;

    public void Bind(MarketCategory value, Action<MarketCategory> selectCallback)
    {
        category   = value;
        onSelected = selectCallback;

        if (tabActiveSprite == null)
            tabActiveSprite = LoadResourceSprite("UI_MarketBoard/tab_active");
        if (tabInactiveSprite == null)
            tabInactiveSprite = LoadResourceSprite("UI_MarketBoard/tab_inactive");

        // Gắn icon vẽ tay cho từng danh mục
        Sprite catIcon = GetCategoryIcon(value);
        if (imageAccent != null)
        {
            if (catIcon != null)
            {
                imageAccent.sprite = catIcon;
                imageAccent.color  = Color.white;
                imageAccent.preserveAspect = true;
                if (textShort != null) textShort.gameObject.SetActive(false);
            }
            else
            {
                imageAccent.color = MarketCategoryUtil.GetAccentColor(value);
                if (textShort != null)
                {
                    textShort.gameObject.SetActive(true);
                    textShort.text = MarketCategoryUtil.GetShortLabel(value);
                }
            }
        }

        if (textLabel != null)
        {
            textLabel.text = MarketCategoryUtil.GetDisplayName(value);
            textLabel.gameObject.SetActive(true);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
        }

        SetSelected(false);
    }

    private void HandleClicked()
    {
        onSelected?.Invoke(category);
    }

    public void SetSelected(bool selected)
    {
        if (imageBackground != null)
        {
            if (selected && tabActiveSprite != null)
            {
                imageBackground.sprite = tabActiveSprite;
                imageBackground.type   = Image.Type.Sliced;
                imageBackground.color  = Color.white;
            }
            else if (!selected && tabInactiveSprite != null)
            {
                imageBackground.sprite = tabInactiveSprite;
                imageBackground.type   = Image.Type.Sliced;
                imageBackground.color  = Color.white;
            }
            else
            {
                imageBackground.color = selected ? MarketBoardPalette.TabSelected : MarketBoardPalette.TabIdle;
            }
        }

        if (textLabel != null)
        {
            textLabel.gameObject.SetActive(true);
            // Tab đang chọn: chữ nâu gỗ đậm nổi trên nền vàng | Tab thường: chữ kem sáng nổi trên nền gỗ
            textLabel.color = selected ? new Color(0.28f, 0.14f, 0.03f) : new Color(0.98f, 0.94f, 0.86f);
        }

        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        if (scaleTarget != null)
            scaleTarget.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
    }

    public static Sprite GetCategoryIcon(MarketCategory cat)
    {
        int idx = 0;
        switch (cat)
        {
            case MarketCategory.All:      idx = 0; break;
            case MarketCategory.NongSan:  idx = 1; break;
            case MarketCategory.HatGiong: idx = 2; break;
            case MarketCategory.Hoa:      idx = 3; break;
            case MarketCategory.ChanNuoi: idx = 4; break;
            case MarketCategory.MonAn:    idx = 5; break;
            case MarketCategory.GiaVi:    idx = 6; break;
            case MarketCategory.VatLieu:  idx = 7; break;
            default:                      idx = 0; break;
        }

        return LoadResourceSprite($"UI_MarketBoard/tab_icon_{idx}");
    }

    public static Sprite LoadResourceSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // 1. Thử load trực tiếp dạng Sprite
        Sprite spr = Resources.Load<Sprite>(path);
        if (spr != null) return spr;

        // 2. Thử LoadAll nếu texture bị set spriteMode Multiple
        Sprite[] all = Resources.LoadAll<Sprite>(path);
        if (all != null && all.Length > 0) return all[0];

        // 3. Thử load Texture2D và tự đóng gói Sprite nếu importer chưa set Sprite
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return null;
    }
}
