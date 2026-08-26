using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một người bán trên Bảng Tin Chợ.
/// Dùng chung cho cả NPC lẫn chính người chơi — UI chỉ cần một kiểu dữ liệu để vẽ.
/// </summary>
public struct MarketSeller
{
    /// <summary>Id ổn định. "local" = chính người chơi, "npc_xx" = NPC, sau này là id server.</summary>
    public string SellerId;
    public string DisplayName;
    /// <summary>Chỉ số bộ avatar. Chưa có art nên tạm quy ra màu, xem GetAvatarColor.</summary>
    public int    AvatarIndex;
    public int    Level;
    public bool   IsLocalPlayer;

    public bool IsValid => !string.IsNullOrEmpty(SellerId);
}

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  DANH BẠ NGƯỜI BÁN NPC (A4)
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO danh sách cứng trong code chứ không random tên lúc chạy:
/// "Ngọc Hằng hôm nay bán lúa, mai vẫn phải là Ngọc Hằng cấp 23 với đúng avatar đó".
/// Nếu sinh ngẫu nhiên mỗi phiên thì bảng tin trông như bot — mà tên người bán chính
/// là thứ duy nhất làm người chơi tin đây là chợ có người thật.
///
/// Mảng cố định ⇒ SellerId = "npc_" + chỉ số ⇒ ổn định vĩnh viễn qua mọi phiên,
/// không cần lưu PlayerPrefs, không có đường nào lệch.
///
/// Có trộn vài tên dạng "guest.13xxxxxxx" cho giống chợ thật — chợ nào cũng có
/// tài khoản khách chưa đặt tên.
/// </summary>
public static class MarketSellerDirectory
{
    public const string LocalSellerId = "local";

    // Tên · avatar · cấp. Cấp rải từ 6 đến 58 để bảng tin không đều tăm tắp.
    private static readonly MarketSeller[] Sellers = BuildSellers();

    public static int Count => Sellers.Length;

    /// <summary>Lấy người bán theo chỉ số bất kỳ — tự cuộn vòng nên gọi kiểu gì cũng an toàn.</summary>
    public static MarketSeller GetByIndex(int index)
    {
        if (Sellers.Length == 0)
            return default;

        int safe = ((index % Sellers.Length) + Sellers.Length) % Sellers.Length;
        return Sellers[safe];
    }

    /// <summary>Tra theo id. Trả về false nếu id lạ (ví dụ dữ liệu save cũ).</summary>
    public static bool TryGetById(string sellerId, out MarketSeller seller)
    {
        seller = default;
        if (string.IsNullOrEmpty(sellerId))
            return false;

        for (int i = 0; i < Sellers.Length; i++)
        {
            if (Sellers[i].SellerId == sellerId)
            {
                seller = Sellers[i];
                return true;
            }
        }
        return false;
    }

    /// <summary>Người bán đại diện cho chính người chơi (dùng cho hàng từ Quầy Hàng).</summary>
    public static MarketSeller GetLocalPlayerSeller(string playerName, int playerLevel)
    {
        return new MarketSeller
        {
            SellerId      = LocalSellerId,
            DisplayName   = string.IsNullOrWhiteSpace(playerName) ? "Bạn" : playerName,
            AvatarIndex   = 0,
            Level         = Mathf.Max(1, playerLevel),
            IsLocalPlayer = true
        };
    }

    /// <summary>
    /// Bảng màu avatar tạm — CHỖ CHỜ ART.
    /// Chủ dự án gắn 10 sprite avatar vào là thay được, AvatarIndex giữ nguyên ý nghĩa.
    /// </summary>
    private static readonly Color[] AvatarColors =
    {
        new Color(0.95f, 0.76f, 0.42f),
        new Color(0.53f, 0.78f, 0.66f),
        new Color(0.78f, 0.60f, 0.86f),
        new Color(0.92f, 0.58f, 0.55f),
        new Color(0.55f, 0.72f, 0.92f),
        new Color(0.86f, 0.84f, 0.52f),
        new Color(0.64f, 0.86f, 0.56f),
        new Color(0.90f, 0.66f, 0.80f),
        new Color(0.70f, 0.68f, 0.62f),
        new Color(0.48f, 0.66f, 0.78f)
    };

    public static Sprite GetAvatarSprite(int avatarIndex)
    {
        int safe = ((avatarIndex % 6) + 6) % 6;
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Game/Farm/Art/UI_MarketBoard/avatar_npc_{safe}.png");
#else
        return null;
#endif
    }

    public static Color GetAvatarColor(int avatarIndex)
    {
        if (AvatarColors.Length == 0)
            return Color.gray;

        int safe = ((avatarIndex % AvatarColors.Length) + AvatarColors.Length) % AvatarColors.Length;
        return AvatarColors[safe];
    }

    /// <summary>
    /// Chữ cái đầu để đắp lên ô avatar khi chưa có art — nhìn vào phân biệt được người bán,
    /// còn hơn 12 ô màu na ná nhau.
    /// </summary>
    public static string GetAvatarInitial(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "?";

        string trimmed = displayName.Trim();
        return trimmed.Substring(0, 1).ToUpperInvariant();
    }

    private static MarketSeller[] BuildSellers()
    {
        // (tên, avatarIndex, cấp)
        (string name, int avatar, int level)[] raw =
        {
            ("Ngọc Hằng",       1, 24), ("Tạ Trân",          2, 31), ("Hiệp Trần Thị",   3, 18),
            ("Hạnh Bùi Thị",    4, 27), ("Mơ Mơ",            5, 12), ("Thái Trần",       6, 35),
            ("Tăng Thu Hậu",    7, 22), ("Hoài Thương",      8, 16), ("Su Hào Bany",     9, 41),
            ("Hằng Nguyễn",     0, 29), ("Lê Minh Tuấn",     1,  9), ("Phạm Thu Trang",  2, 38),
            ("Vũ Đình Khoa",    3, 14), ("Đặng Bảo Ngọc",    4, 46), ("Bùi Quang Huy",   5, 20),
            ("Trịnh Mai Anh",   6, 33), ("Đỗ Hải Yến",       7, 11), ("Lý Gia Bảo",      8, 25),
            ("Ngô Thanh Vân",   9, 52), ("Hoàng Văn Sơn",    0, 17), ("Cao Thị Lụa",     1, 30),
            ("Dương Tiểu Mi",   2,  8), ("Chu Văn Đức",      3, 44), ("Tô Ngọc Diệp",    4, 21),
            ("Mai Hữu Phước",   5, 36), ("Lâm Khánh Chi",    6, 13), ("Kiều Anh Thư",    7, 28),
            ("Hà Bảo Lâm",      8, 19), ("Uông Mỹ Duyên",    9, 40), ("Tống Gia Hân",    0, 15),
            ("Bảy Ròm",         1,  7), ("Út Mập",           2, 10), ("Chị Ba Rau Sạch", 3, 26),
            ("Cô Tư Miệt Vườn", 4, 34), ("Anh Năm Lúa",      5, 23), ("Dì Sáu Bánh Bèo", 6, 32),
            ("Thím Chín Chợ",   7, 39), ("Ông Mười Vườn",    8, 48), ("Hai Lúa Miền Tây",9, 45),
            ("Nhóc Tí",         0,  6), ("Nguyễn Thảo My",   1, 37), ("Trương Bảo Trân", 2, 42),
            ("Phan Đức Trọng",  3, 50), ("Võ Kim Ngân",      4, 43), ("Đinh Hồng Nhung", 5, 49),
            ("Lưu Gia Huy",     6, 54), ("Tạ Bích Phượng",   7, 58), ("Huỳnh Nhật Nam",  8, 47),
            ("Bành Tiểu Yến",   9, 51), ("Quách Thành Đạt",  0, 56),
            // Tài khoản khách — chợ thật luôn có vài cái, thiếu là trông "sạch" quá hoá giả
            ("guest.1379345808", 1, 5),  ("guest.2048117364", 3, 8),
            ("guest.9931276450", 5, 11), ("guest.4471982003", 7, 14),
            ("guest.6620358117", 9, 19), ("guest.8815440276", 2, 26)
        };

        List<MarketSeller> list = new List<MarketSeller>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            list.Add(new MarketSeller
            {
                SellerId      = "npc_" + i.ToString("00"),
                DisplayName   = raw[i].name,
                AvatarIndex   = raw[i].avatar,
                Level         = raw[i].level,
                IsLocalPlayer = false
            });
        }

        return list.ToArray();
    }
}
