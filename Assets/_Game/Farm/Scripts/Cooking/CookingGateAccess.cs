using UnityEngine;

/// <summary>
/// Cổng kiểm quyền vào Bếp — MỘT chỗ duy nhất chứa con số cấp yêu cầu.
///
/// VÌ SAO phải tách ra một class riêng thay vì viết `if (level &lt; 5)` tại chỗ:
/// có HAI đường vào bếp, và trước đây cả hai đều không kiểm gì cả —
///   1. click cổng `CookingGate` ngoài world  → <see cref="BuildingInteractable"/>
///   2. nút HUD wired vào                    → <see cref="FarmUIManager.OnClick_GoCooking"/>
/// Khoá một đường mà quên đường kia thì người chơi cấp 1 vẫn vào được bằng đường còn lại.
/// Viết số 5 ở hai chỗ thì lần sau đổi cấp là chắc chắn lệch một chỗ.
///
/// VÌ SAO khoá tới cấp 5: món có `unlockLevel` thấp nhất trong 18 `DishData` là cấp 5
/// (`khoai_tay_chien`, `com_chien_trung`, `trung_chien_ca_chua`). Vào bếp ở cấp 1-4 là
/// mở ra một màn hình KHÔNG CÓ MÓN NÀO chọn được — người chơi tưởng game lỗi.
/// </summary>
public static class CookingGateAccess
{
    /// <summary>Cấp tối thiểu để vào Bếp. Bằng `unlockLevel` nhỏ nhất trong toàn bộ DishData.</summary>
    public const int RequiredLevel = 5;

    /// <summary>Thông báo hiện cho người chơi khi chưa đủ cấp. Ghép từ <see cref="RequiredLevel"/>
    /// chứ không gõ lại số — đổi cấp một chỗ là câu thông báo đổi theo.</summary>
    public static readonly string LockedMessage = $"Cần cấp {RequiredLevel} mới vào được Bếp.";

    /// <summary>
    /// Cấp người chơi hiện tại. Đọc `PlayerProgressManager` trước, `FarmLevelManager` sau —
    /// đúng thứ tự mà `PopupEwarManager.GetPlayerLevel()` đang dùng, để hai nơi không bao
    /// giờ đọc ra hai cấp khác nhau.
    /// </summary>
    public static int CurrentLevel
    {
        get
        {
            if (PlayerProgressManager.Instance != null) return PlayerProgressManager.Instance.Level;
            if (FarmLevelManager.Instance != null)      return FarmLevelManager.Instance.CurrentLevel;
            return 1;
        }
    }

    public static bool CanEnter => CurrentLevel >= RequiredLevel;

    /// <summary>
    /// Kiểm quyền và TỰ hiện thông báo khi bị chặn. Trả true = được phép vào.
    /// Gộp cả việc báo vào đây để không nơi nào chặn im lặng — chặn mà không nói gì
    /// thì người chơi bấm mãi vào cổng và tưởng game bị treo.
    /// </summary>
    public static bool CanEnterOrWarn()
    {
        if (CanEnter) return true;

        FarmUIManager.Instance?.ShowHint(LockedMessage);
        Debug.Log($"[CookingGate] Chặn vào Bếp: đang cấp {CurrentLevel}, cần cấp {RequiredLevel}.");
        return false;
    }
}
