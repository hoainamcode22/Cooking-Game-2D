using UnityEngine;

/// <summary>
/// Registry SPRITE CHUẨN dùng chung toàn game (Sếp duyệt 2026-09-05).
/// Mọi popup/tool lấy nút đóng, nút gem, khung, card… từ đây thay vì tự tìm đường dẫn riêng
/// → chấm dứt tình trạng 4 sprite đóng + 2 nút vẽ code rải rác 18 chỗ.
///
/// Thứ tự tìm sprite: Resources/UI/Standard/&lt;tên&gt; (chạy được trong build) → đường dẫn asset gốc (Editor,
/// qua <see cref="SettingsPopupUI.LoadSprite"/>). Tool "Đồng bộ nút đóng" sẽ copy các file gốc vào
/// Resources/UI/Standard/ để build thật không bị null.
/// </summary>
public static class UIStandardSprites
{
    /// <summary>Thư mục Resources chứa bản copy runtime của bộ sprite chuẩn.</summary>
    public const string ResourcesFolder = "UI/Standard";

    // ── Đường dẫn asset gốc (nguồn sự thật, dùng khi ở Editor) ──────────────────────────────
    public const string PathClose      = "Assets/Export_Kitchen_UI_Package/Sprites/btn_red_small.png";      // nút đóng đỏ (popup Cài đặt)
    public const string PathBtnGreen   = "Assets/Export_Kitchen_UI_Package/Sprites/btn_big_green.png";      // nút hành động chính (ĐÃ RÕ, Bắt đầu nào)
    public const string PathBtnGray    = "Assets/Export_Kitchen_UI_Package/Sprites/btn_big_gray.png";
    public const string PathBtnPaper   = "Assets/Export_Kitchen_UI_Package/Sprites/btn_paper_small.png";
    public const string PathBtnGem     = "Assets/Assetsgame/popup/ui_building_svg/generated_sprites/proc_btn_blue.png"; // nền nút kim cương
    public const string PathIconGem    = "Assets/Assetsgame/kimcuong-removebg-preview.png";                  // icon kim cương
    public const string PathIconGold   = "Assets/Export_Kitchen_UI_Package/Sprites/icon_gold.png";
    public const string PathFrameWood  = "Assets/Export_Train_UI_Package/Sprites/popup_frame_wood.png";      // khung gỗ ngoài popup
    public const string PathPanelPaper = "Assets/Export_Train_UI_Package/Sprites/popup_panel_paper.png";     // giấy kem bên trong
    public const string PathRibbon     = "Assets/Export_Train_UI_Package/Sprites/ribbon_banner_gold.png";    // ribbon tiêu đề
    public const string PathRowDark    = "Assets/Export_Train_UI_Package/Sprites/timer_box_dark.png";        // hàng lõm tối
    public const string PathBtnGreen3D = "Assets/Export_Train_UI_Package/Sprites/btn_green_3d.png";
    public const string PathBtnYellow3D= "Assets/Export_Train_UI_Package/Sprites/btn_yellow_3d.png";
    public const string PathBarTrack   = "Assets/Export_Train_UI_Package/Sprites/progress_track_bar.png";
    public const string PathBarFill    = "Assets/Export_Train_UI_Package/Sprites/progress_fill_green.png";
    public const string PathCheckBadge = "Assets/Export_Train_UI_Package/Sprites/check_badge_green.png";
    public const string PathCardOuter  = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites/shop_card_outer.png"; // card bo góc
    public const string PathCardInner  = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites/shop_card_inner.png";
    public const string PathSlotNormal = "Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/slot_normal.png";   // ô chọn
    public const string PathSlotSelected="Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/slot_selected.png";
    public const string PathAvatarBase = "Assets/Assetsgame/popup/ui_township_exact_bases/generated_sprites/hud_avatar_base.png";
    public const string PathTutFrame   = "Assets/Art/UI/TutorialV2/board/tut_board_frame.png";
    public const string PathTutRibbon  = "Assets/Art/UI/TutorialV2/board/tut_board_ribbon.png";
    public const string PathTutDotOn   = "Assets/Art/UI/TutorialV2/board/tut_step_dot_on.png";
    public const string PathTutDotOff  = "Assets/Art/UI/TutorialV2/board/tut_step_dot_off.png";

    /// <summary>Kích thước chuẩn nút đóng (vuông, 9-slice từ btn_red_small 256×96).</summary>
    public static readonly Vector2 CloseSize = new Vector2(64f, 64f);
    /// <summary>Cỡ chữ "X" trên nút đóng (TMP, trắng, bold).</summary>
    public const float CloseGlyphSize = 26f;

    // ── Truy cập ────────────────────────────────────────────────────────────────────────────
    public static Sprite Close       => Load(PathClose);
    public static Sprite BtnGreen    => Load(PathBtnGreen);
    public static Sprite BtnGray     => Load(PathBtnGray);
    public static Sprite BtnPaper    => Load(PathBtnPaper);
    public static Sprite BtnGem      => Load(PathBtnGem);
    public static Sprite IconGem     => Load(PathIconGem);
    public static Sprite IconGold    => Load(PathIconGold);
    public static Sprite FrameWood   => Load(PathFrameWood);
    public static Sprite PanelPaper  => Load(PathPanelPaper);
    public static Sprite Ribbon      => Load(PathRibbon);
    public static Sprite RowDark     => Load(PathRowDark);
    public static Sprite BtnGreen3D  => Load(PathBtnGreen3D);
    public static Sprite BtnYellow3D => Load(PathBtnYellow3D);
    public static Sprite BarTrack    => Load(PathBarTrack);
    public static Sprite BarFill     => Load(PathBarFill);
    public static Sprite CheckBadge  => Load(PathCheckBadge);
    public static Sprite CardOuter   => Load(PathCardOuter);
    public static Sprite CardInner   => Load(PathCardInner);
    public static Sprite SlotNormal  => Load(PathSlotNormal);
    public static Sprite SlotSelected=> Load(PathSlotSelected);
    public static Sprite AvatarBase  => Load(PathAvatarBase);
    public static Sprite TutFrame    => Load(PathTutFrame);
    public static Sprite TutRibbon   => Load(PathTutRibbon);
    public static Sprite TutDotOn    => Load(PathTutDotOn);
    public static Sprite TutDotOff   => Load(PathTutDotOff);

    /// <summary>Toàn bộ đường dẫn gốc — tool đồng bộ dùng để copy vào Resources/UI/Standard/.</summary>
    public static readonly string[] AllPaths =
    {
        PathClose, PathBtnGreen, PathBtnGray, PathBtnPaper, PathBtnGem, PathIconGem, PathIconGold,
        PathFrameWood, PathPanelPaper, PathRibbon, PathRowDark, PathBtnGreen3D, PathBtnYellow3D,
        PathBarTrack, PathBarFill, PathCheckBadge, PathCardOuter, PathCardInner, PathSlotNormal,
        PathSlotSelected, PathAvatarBase, PathTutFrame, PathTutRibbon, PathTutDotOn, PathTutDotOff,
    };

    /// <summary>
    /// Tải sprite theo đường dẫn gốc: ưu tiên bản copy trong Resources/UI/Standard (build thật),
    /// sau đó tới AssetDatabase/Resources theo tên file (logic sẵn có của SettingsPopupUI).
    /// Trả null nếu không có — caller tự fallback màu phẳng, KHÔNG throw.
    /// </summary>
    public static Sprite Load(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        Sprite s = Resources.Load<Sprite>(ResourcesFolder + "/" + file);
        if (s != null) return s;

        return SettingsPopupUI.LoadSprite(assetPath);
    }
}
