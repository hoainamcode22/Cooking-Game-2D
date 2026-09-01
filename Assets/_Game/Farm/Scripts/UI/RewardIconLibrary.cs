using UnityEngine;

/// <summary>
/// BỘ ICON TIỀN TỆ DÙNG CHUNG CHO CẢ GAME — một nguồn sự thật duy nhất.
/// ═══════════════════════════════════════════════════════════════════
/// VÌ SAO CÓ FILE NÀY: trước đây mỗi màn hình tự kéo một sprite vàng riêng nên trong game
/// tồn tại 3–4 kiểu đồng xu khác nhau. Đội art sẽ bàn giao bộ icon chính thức vào
/// `Assets/Art/UI/Currency/` (icon_gold.png / icon_gem.png / icon_exp_star.png);
/// từ đó mọi hiệu ứng (RewardFlyFX) và tool đồng bộ icon đều lấy sprite Ở ĐÂY.
///
/// VỊ TRÍ ASSET: `Assets/_Game/Resources/RewardIconLibrary.asset` — PHẢI nằm trong một
/// folder `Resources` và giữ đúng tên file để <see cref="Instance"/> load được.
/// Tạo bằng menu: Tools/Farm Game/Reward FX/★ Setup Reward Fly FX (1 nút).
///
/// NULL-SAFE: chưa có asset thì Instance trả null — người gọi tự lo fallback
/// (RewardFlyFX có sprite vẽ runtime), KHÔNG được ném lỗi đỏ.
/// </summary>
[CreateAssetMenu(fileName = "RewardIconLibrary", menuName = "Farm Game/Reward Icon Library")]
public class RewardIconLibrary : ScriptableObject
{
    [Tooltip("Icon VÀNG chính thức — cả game dùng chung 1 icon này (Assets/Art/UI/Currency/icon_gold.png).")]
    public Sprite goldSprite;

    [Tooltip("Icon KIM CƯƠNG chính thức (Assets/Art/UI/Currency/icon_gem.png).")]
    public Sprite gemSprite;

    [Tooltip("Icon EXP — ngôi sao xanh lá (Assets/Art/UI/Currency/icon_exp_star.png).")]
    public Sprite expSprite;

    private static RewardIconLibrary cached;

    /// <summary>
    /// Load từ Resources ("RewardIconLibrary"), cache sau lần tìm thấy đầu tiên.
    /// Chưa có asset → trả null ÊM (không log lỗi) và lần gọi sau sẽ thử load lại —
    /// để asset được tạo giữa chừng (chạy Setup tool trong lúc Play) vẫn được nhặt lên.
    /// </summary>
    public static RewardIconLibrary Instance
    {
        get
        {
            if (cached == null)
                cached = Resources.Load<RewardIconLibrary>("RewardIconLibrary");
            return cached;
        }
    }
}
