using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  CỔNG PHIÊN BẢN CHUNG CHO MỌI KHOÁ PLAYERPREFS (B4)
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO CẦN: dự án có 6 hệ save đã tự quản `saveVersion` bên trong JSON
/// (`WarehouseManager`, `PlacementManager`, `ConstructionManager`, `OrderBoardManager`,
/// `PlayerStallManager`, `MarketRefreshTimer`) nhưng còn hơn 20 khoá **ghi thẳng số/chuỗi**
/// chứ không qua JSON — `PLAYER_LEVEL`, `FARM_ECONOMY_GOLD`, `WAREHOUSE_LEVEL`, `PenState_*`,
/// mọi cờ tutorial… Những khoá đó KHÔNG có chỗ nào để nhét `saveVersion` vào.
///
/// Không có version thì lần sau đổi ý nghĩa dữ liệu là **không phân biệt được** save cũ với
/// save mới. Ví dụ thật vừa xảy ra trong đợt này: `growSeconds` đổi từ "giây × 0.3" sang
/// "giây thật", và `plotId` được cấp lại (F1) làm ĐỔI KHOÁ LƯU của 8 ô đất. Save cũ đọc bằng
/// code mới cho ra kết quả sai mà không ai biết.
///
/// CÁCH DÙNG: mỗi hệ gọi <see cref="Ensure"/> đúng MỘT LẦN, ngay trước khi đọc save.
/// Hàm trả về phiên bản CŨ tìm thấy (0 = save đời chưa có version, hoặc chưa từng có save),
/// và tự đóng dấu phiên bản hiện tại để lần sau không chuyển đổi lần thứ hai.
///
/// VÌ SAO tách riêng một khoá `SAVE_VER_&lt;họ&gt;` thay vì gói version vào từng khoá:
/// gói vào là ĐỔI ĐỊNH DẠNG của chính khoá đang có dữ liệu — người chơi hiện tại mất sạch
/// ngay lần cập nhật này. Khoá phụ thì dữ liệu cũ vẫn đọc được nguyên vẹn.
/// </summary>
public static class SaveVersionGuard
{
    private const string Prefix = "SAVE_VER_";

    /// <summary>Tên khoá phụ giữ số phiên bản của một họ save.</summary>
    public static string KeyFor(string family) => Prefix + family;

    /// <summary>
    /// Đọc phiên bản save đang có của một họ khoá, gọi <paramref name="migrate"/> nếu cũ hơn,
    /// rồi đóng dấu <paramref name="currentVersion"/>.
    /// </summary>
    /// <param name="family">Tên họ, ví dụ "PLAYER_PROGRESS". Chỉ dùng chữ IN + gạch dưới.</param>
    /// <param name="currentVersion">Phiên bản mà code đang chạy hiểu được. Luôn ≥ 1.</param>
    /// <param name="migrate">
    /// Nhánh chuyển đổi: (phiênBảnCũ, phiênBảnMới). Chỉ được gọi khi cũ &lt; mới VÀ máy này
    /// thật sự đã có save của họ đó (<paramref name="hasExistingSave"/> = true).
    /// Bỏ trống = "định dạng không đổi, chỉ cần đóng dấu".
    /// </param>
    /// <param name="hasExistingSave">
    /// Máy này đã có dữ liệu của họ save đó chưa. BẮT BUỘC người gọi truyền vào:
    /// người chơi MỚI cũng đọc ra version 0, mà gọi migrate cho máy trắng là vô nghĩa và
    /// dễ sinh log báo động giả.
    /// </param>
    /// <returns>Phiên bản tìm thấy trước khi đóng dấu (0 = chưa có version).</returns>
    public static int Ensure(string family, int currentVersion,
                             System.Action<int, int> migrate = null,
                             bool hasExistingSave = true)
    {
        if (string.IsNullOrEmpty(family) || currentVersion < 1)
        {
            Debug.LogError($"[SaveVersion] Tham số sai: family='{family}', version={currentVersion}.");
            return currentVersion;
        }

        string key = KeyFor(family);
        int found = PlayerPrefs.GetInt(key, 0);

        if (found == currentVersion)
            return found;

        if (found > currentVersion)
        {
            // Save mới hơn code = người chơi vừa hạ cấp bản game. KHÔNG được ghi đè xuống:
            // ghi đè là lần sau lên bản mới lại chạy migrate một lần nữa trên dữ liệu đã mới.
            Debug.LogWarning(
                $"[SaveVersion] '{family}': save v{found} MỚI HƠN code v{currentVersion} " +
                $"(hạ cấp bản game?). Giữ nguyên dấu phiên bản, đọc save bằng code cũ có thể sai.");
            return found;
        }

        if (hasExistingSave && migrate != null)
        {
            Debug.Log($"[SaveVersion] '{family}': chuyển save v{found} → v{currentVersion}.");
            migrate(found, currentVersion);
        }
        else if (hasExistingSave && found == 0)
        {
            Debug.Log($"[SaveVersion] '{family}': save đời cũ (chưa có version) → đóng dấu v{currentVersion}, " +
                      $"định dạng không đổi nên không phải chuyển gì.");
        }

        PlayerPrefs.SetInt(key, currentVersion);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        return found;
    }

    /// <summary>Xoá dấu phiên bản của một họ. Dùng trong các tool reset save.</summary>
    public static void Clear(string family)
    {
        if (string.IsNullOrEmpty(family)) return;
        PlayerPrefs.DeleteKey(KeyFor(family));
    }

    /// <summary>
    /// Danh sách MỌI họ save trong dự án — nguồn sự thật duy nhất.
    /// Tool reset (`FarmSaveCleanupTool`, `FarmResetTool`) duyệt danh sách này để xoá sạch dấu
    /// phiên bản; thiếu một họ là save cũ "lai" save mới, lỗi cực khó truy.
    /// </summary>
    public static readonly string[] AllFamilies =
    {
        "PLAYER_PROGRESS",     // PLAYER_LEVEL, PLAYER_EXP
        "FARM_ECONOMY",        // FARM_ECONOMY_GOLD, FARM_ECONOMY_GEMS
        "FARM_INVENTORY",      // FARM_INVENTORY_SAVE
        "KITCHEN_TRANSFER",    // KITCHEN_TRANSFER_SAVE
        "WAREHOUSE_LEVEL",     // WAREHOUSE_LEVEL
        "PLAYER_PROFILE",      // PLAYER_PROFILE_NAME / _AVATAR_INDEX / _WAREHOUSE_LEVEL / _ACHIEVEMENT_COUNT
        "PEN_STATE",           // PenState_* , PenFood_* , PenStartTime_*
        "TUTORIAL",            // TUTORIAL_MAIN_DONE, TUTORIAL_PREPLANT_DONE, STARTER_ITEMS_GIVEN,
                               // ANIMAL_GUIDE_COOP_FEED_DONE, GUIDE_DELIVER_DONE, GUIDE_TRAIN_DONE,
                               // GUIDE_COOKING_DONE
        "MISSION",             // MISSION_PROGRESS_V1, MISSION_CLAIMED_*, ACHIEVEMENT_CLAIMED_*,
                               // UNIFIED_TASK_DAILY_*
    };
}
