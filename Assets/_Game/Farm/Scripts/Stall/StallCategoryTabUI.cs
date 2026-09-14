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

    private static Sprite GetCategoryIcon(StallItemCategory cat)
    {
#if UNITY_EDITOR
        switch (cat)
        {
            case StallItemCategory.TatCa:
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/QuayHang/stall_tab_tatca.png")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/Design_Assets/baothoc.png");

            case StallItemCategory.NongSan:
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/QuayHang/stall_tab_nongsan.png")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/Design_Assets/iconlua.png");

            case StallItemCategory.Hoa:
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/QuayHang/stall_tab_hoa.png")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Hoa/hoahong-removebg-preview.png");

            case StallItemCategory.HatGiong:
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/QuayHang/stall_tab_hatgiong.png")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/tab_seeds.png");

            case StallItemCategory.CheBien:
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/QuayHang/stall_tab_chebien.png")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/Design_Assets/monan1.png");
        }
#endif
        return Resources.Load<Sprite>("UI_ChuyenCanh/MonAn_ChuyenCanh");
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
