using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MỘT TAB DANH MỤC ở cột trái panel chọn vật phẩm (B4).
///
/// Tab đang chọn phải NỔI BẬT HẲN chứ không chỉ đổi màu nhạt — trong video, icon đang
/// chọn to hơn và sáng hẳn lên, nhờ vậy người chơi không bao giờ nhầm mình đang ở mục nào.
/// Ở đây tái hiện bằng ba tín hiệu chồng lên nhau (màu nền + độ phóng + chữ đậm) để
/// trạng thái vẫn đọc được cả khi người chơi không phân biệt được màu.
/// </summary>
public class StallCategoryTabUI : MonoBehaviour
{
    [Header("Danh mục tab này đại diện")]
    [SerializeField] private StallItemCategory category = StallItemCategory.TatCa;

    [Header("Thành phần")]
    [SerializeField] private Button        button;
    [SerializeField] private Image         imageArtTabBackground;
    [SerializeField] private Image         imageArtCategoryIcon;
    [SerializeField] private TMP_Text      label;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Màu")]
    [SerializeField] private Color colorSelected = new Color(1.0f, 0.82f, 0.34f, 1f); // #FFD257 Vàng rực nổi bật
    [SerializeField] private Color colorNormal   = new Color(0.49f, 0.31f, 0.13f, 1f); // #7C4E22 Nâu gỗ ấm

    private StallPopupUI _owner;

    public StallItemCategory Category => category;

    private void Awake()
    {
        ApplyCategoryIcon();
    }

    public void Bind(StallPopupUI owner)
    {
        _owner = owner;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        ApplyCategoryIcon();
    }

    private void OnClick()
    {
        if (_owner != null) _owner.OnSelectCategory(category);
    }

    /// <summary>
    /// [VÒNG 2026-09-11] Gắn icon tương ứng từ Shop/Kho vào tab danh mục (Nông sản, Hoa, Hạt giống, Chế biến, Tất cả)
    /// </summary>
    public void ApplyCategoryIcon()
    {
        if (imageArtCategoryIcon == null) return;

        // Nếu sprite hiện tại là vòng tròn trắng placeholder hoặc chưa có sprite -> nạp sprite thật
        if (imageArtCategoryIcon.sprite == null || imageArtCategoryIcon.sprite.name.Contains("stall_circle") || imageArtCategoryIcon.sprite.name.Contains("circle"))
        {
            Sprite spr = GetCategoryIcon(category);
            if (spr != null)
            {
                imageArtCategoryIcon.sprite = spr;
                imageArtCategoryIcon.preserveAspect = true;
            }
        }

        imageArtCategoryIcon.color = Color.white;
        imageArtCategoryIcon.preserveAspect = true;
    }

    /// <summary>
    /// [SỬA 2026-09-22] NGUYÊN NHÂN LỖI "MỌI TAB ĐỀU LÀ TÔ PHỞ" TRÊN APK:
    /// bản cũ đặt TOÀN BỘ switch trong #if UNITY_EDITOR và dùng AssetDatabase. Khi build
    /// Android, cả khối đó bị cắt bỏ, hàm rơi thẳng xuống dòng cuối
    /// `Resources.Load("UI_ChuyenCanh/MonAn_ChuyenCanh")` — đúng là cái tô phở — nên MỌI
    /// tab đều nhận cùng một ảnh. Trong Editor thì AssetDatabase chạy được nên nhìn vẫn đúng.
    /// CÁCH SỬA: 7 icon đã được copy vào Assets/_Game/Resources/UI_Stall/ (đã cắt viền và
    /// thu về 192 px: 17 MB -> 156 KB) và nạp bằng Resources.Load — chạy giống hệt nhau ở
    /// Editor lẫn build. KHÔNG còn ảnh dự phòng sai: thà thiếu icon còn hơn icon sai.
    /// </summary>
    private static Sprite GetCategoryIcon(StallItemCategory cat)
    {
        string ten = null;
        switch (cat)
        {
            case StallItemCategory.TatCa:        ten = "stall_tab_tatca";        break;
            case StallItemCategory.NongSan:      ten = "stall_tab_nongsan";      break;
            case StallItemCategory.Hoa:          ten = "stall_tab_hoa";          break;
            case StallItemCategory.HatGiong:     ten = "stall_tab_hatgiong";     break;
            case StallItemCategory.CheBien:      ten = "stall_tab_chebien";      break;
            case StallItemCategory.VatLieu:      ten = "stall_tab_vatlieu";      break;
            case StallItemCategory.ThucAnGiaSuc: ten = "stall_tab_thucangiasuc"; break;
        }
        if (string.IsNullOrEmpty(ten)) return null;

        Sprite spr = Resources.Load<Sprite>("UI_Stall/" + ten);
        if (spr == null)
            Debug.LogWarning("[StallTab] Thieu icon Resources/UI_Stall/" + ten + " — tab se khong co anh.");
        return spr;
    }

    public void SetSelected(bool selected)
    {
        if (imageArtTabBackground != null)
            imageArtTabBackground.color = selected ? colorSelected : colorNormal;

        if (imageArtCategoryIcon != null)
        {
            imageArtCategoryIcon.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.75f);
            imageArtCategoryIcon.preserveAspect = true;
        }

        if (label != null)
        {
            label.color     = selected ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }

        if (scaleTarget != null)
            scaleTarget.localScale = selected ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
    }

#if UNITY_EDITOR
    /// <summary>Editor tool gán danh mục lúc dựng prefab tab.</summary>
    public void EditorSetCategory(StallItemCategory value) => category = value;
#endif
}
