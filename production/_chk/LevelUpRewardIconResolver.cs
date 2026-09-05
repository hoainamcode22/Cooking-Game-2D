using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [R2 ICON] TRA ICON PHẦN THƯỞNG cho popup lên cấp — một chuỗi ưu tiên duy nhất.
/// ═══════════════════════════════════════════════════════════════════════════
/// VÌ SAO CÓ FILE NÀY: bảng quà V3 (LevelRewardV2FillTool) ghi lại 29 asset
/// LevelReward_L2..L30 nhưng CHỈ tái dùng icon của entry cũ trùng id — mọi id mới
/// (ngo, sugarcane, beef, kinh, da, go, dinh, son, milk...) đều mang icon = null.
/// Popup vẽ ô quà chỉ-có-chữ → ảnh chụp bị Sếp chê. Icon của các id đó THẬT RA
/// ĐỀU TỒN TẠI trong asset của game (CropData / InventoryItemData) — chỉ là chưa
/// ai tra. File này tra hộ, theo đúng thứ tự:
///
///   1. Sprite đã gán sẵn trong LevelRewardConfig (gift.icon) — nguồn do designer
///      duyệt tay, luôn thắng.
///   2. RewardIconLibrary — bộ icon TIỀN TỆ dùng chung (__gold / __gem / exp).
///      Thư viện này chỉ có 3 sprite tiền tệ, không có icon vật phẩm.
///   3. StallItemCatalog.Instance.GetIcon(id) — sổ tra id → icon dựng từ CropData
///      (BaseItemData.itemIcon / harvestIcon) + InventoryItemData.icon. Đây chính
///      là "icon của chính data item qua id". KHÔNG viết bảng tra thứ hai — hai
///      bảng icon song song là con đường ngắn nhất tới cảnh cùng một củ cà rốt
///      hiện hai hình khác nhau ở hai màn hình.
///   4. Vẫn miss → trả null; nơi gọi tự vẽ placeholder (đĩa màu theo id), còn ở
///      đây log warning "[LevelUp]" MỘT LẦN DUY NHẤT cho mỗi id để Sếp thấy trong
///      Console mà không bị spam mỗi lần lên cấp.
///
/// NULL-SAFE: thiếu RewardIconLibrary.asset hay chưa có StallItemCatalog trong
/// scene đều KHÔNG ném lỗi — chỉ rơi xuống bậc kế tiếp.
/// </summary>
public static class LevelUpRewardIconResolver
{
    /// <summary>id ảo của ô quà VÀNG (chỉ để vẽ, không vào kho) — khớp [V4] trong LevelUpPopupUI.</summary>
    public const string GoldId = "__gold";

    /// <summary>id ảo của ô quà KIM CƯƠNG (chỉ để vẽ, không vào kho).</summary>
    public const string GemId = "__gem";

    // Mỗi id chỉ được cảnh báo 1 lần / phiên chạy (kể cả lên cấp 5 lần liên tiếp).
    private static readonly HashSet<string> _daCanhBao = new HashSet<string>();

    /// <summary>
    /// Tra icon theo chuỗi ưu tiên ở đầu file. Trả null khi mọi nguồn đều miss —
    /// khi đó ĐÃ log warning [LevelUp] (một lần cho mỗi id), nơi gọi chỉ việc vẽ placeholder.
    /// </summary>
    /// <param name="itemId">id vật phẩm ("ngo", "kinh", "__gold"...). Không phân biệt hoa thường.</param>
    /// <param name="assetIcon">Sprite đã gán sẵn trong LevelRewardConfig — ưu tiên 1, null thì tra tiếp.</param>
    /// <param name="displayName">Tên hiển thị, chỉ dùng cho câu warning dễ đọc.</param>
    public static Sprite Resolve(string itemId, Sprite assetIcon, string displayName = "")
    {
        // ── 1 · icon designer gán tay trong asset ───────────────────────────
        if (assetIcon != null) return assetIcon;

        string key = string.IsNullOrEmpty(itemId) ? "" : itemId.Trim().ToLowerInvariant();

        // ── 2 · thư viện tiền tệ dùng chung ─────────────────────────────────
        var lib = RewardIconLibrary.Instance;   // null êm khi chưa có asset
        if (lib != null)
        {
            if ((key == GoldId || key == "gold") && lib.goldSprite != null) return lib.goldSprite;
            if ((key == GemId || key == "gem" || key == "diamond") && lib.gemSprite != null) return lib.gemSprite;
            if ((key == "exp" || key == "__exp") && lib.expSprite != null) return lib.expSprite;
        }

        // ── 3 · icon của chính data item, tra theo id ────────────────────────
        // StallItemCatalog là singleton scene (Awake tự Build) — popup lên cấp chỉ
        // chạy trong SCN_Farm nơi catalog đã được tool quét-và-gán 72 InventoryItemData
        // + 23 CropData. Vẫn null-check theo luật chung.
        if (key.Length > 0 && StallItemCatalog.Instance != null)
        {
            Sprite s = StallItemCatalog.Instance.GetIcon(key);
            if (s != null) return s;
        }

        // ── 4 · chịu thua → cảnh báo 1 lần rồi trả null ──────────────────────
        if (_daCanhBao.Add(key.Length > 0 ? key : "(id rỗng)"))
        {
            string nguon = StallItemCatalog.Instance == null
                ? "StallItemCatalog CHƯA có trong scene (chạy tool Quầy Hàng để dựng)"
                : "không nguồn nào có icon cho id này";
            Debug.LogWarning($"[LevelUp] Thiếu icon cho quà '{displayName}' [{itemId}] — {nguon}. " +
                             "Đang vẽ placeholder màu. Bổ sung: gán gift.icon trong LevelReward_L*.asset " +
                             "hoặc tạo InventoryItemData có itemId này kèm icon rồi chạy lại tool Quầy Hàng.");
        }
        return null;
    }

    /// <summary>Xoá bộ nhớ "đã cảnh báo" — chỉ dùng cho test/tool, runtime không cần gọi.</summary>
    public static void ResetWarnings() => _daCanhBao.Clear();
}
