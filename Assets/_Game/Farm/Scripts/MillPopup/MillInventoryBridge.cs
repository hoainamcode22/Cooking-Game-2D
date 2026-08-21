using UnityEngine;

/// <summary>
/// CẦU NỐI KINH TẾ / KHO cho popup máy xay — một chỗ duy nhất chạm vào hệ thống của dự án.
///
/// ══ TẤT CẢ HÀM DƯỚI ĐÂY GỌI API THẬT, KHÔNG CÓ HÀM BỊA ══
///   • Assets/_Game/Farm/Scripts/Managers/FarmInventoryManager.cs   ← TÚI NÔNG SẢN
///       FarmInventoryManager.Instance
///       .CanAddItem(string itemId) → bool
///       .AddItem(string itemId, int amount) → bool
///       .GetAmount(string itemId) → int
///       .HasItem(string itemId, int amount = 1) → bool
///       .RemoveItem(string itemId, int amount) → bool
///   • Assets/_Game/Farm/Scripts/Managers/FarmEconomyManager.cs
///       FarmEconomyManager.Instance.Gems (int, get) · .SpendGems(int) → bool
///   • Assets/_Game/Farm/Scripts/Managers/FarmLevelManager.cs
///       FarmLevelManager.Instance.CurrentLevel (int, get) · .HasReached(int) → bool
///
/// ══ VÌ SAO CÓ LỚP NÀY DÙ API ĐÃ CÓ THẬT ══
/// 1) Mọi manager của dự án đều là singleton có thể null (scene test, scene Editor, lúc
///    boot). Gói null-check vào đây để `MillPopupUI` không rải 20 lần `if (Instance != null)`.
/// 2) Nếu sau này dự án đổi túi đồ, chỉ sửa ĐÚNG file này, không phải mò trong logic popup.
/// 3) Khi manager vắng mặt, popup vẫn mở được để wire UI — chỉ log cảnh báo, KHÔNG ném
///    exception làm chết cả canvas.
///
/// ══ LỊCH SỬ ══
/// v1 đọc `WarehouseManager` (kho vật phẩm) → SAI TÚI: nông sản thu hoạch nằm ở
/// `FarmInventoryManager` (PlotController.cs:660). Hậu quả: kho luôn hiện 0 nguyên liệu,
/// nút "XAY NGAY" vĩnh viễn bị disable, popup trông như ảnh tĩnh. v2 (20/08) đã đổi đúng túi.
/// </summary>
public static class MillInventoryBridge
{
    private const string LOG = "[MILL] ";

    // Chỉ cảnh báo MỘT LẦN cho mỗi hệ thống thiếu — nếu không thì Update sẽ spam console.
    private static bool _daCanhBaoKho;
    private static bool _daCanhBaoTien;
    private static bool _daCanhBaoCap;

    // ═══════════════════════════════ KHO ═══════════════════════════════
    //
    //  ⚠ SỬA NGÀY 20/08 — ĐÂY LÀ NGUYÊN NHÂN GỐC CỦA LỖI "NÚT XAY NGAY KHÔNG BẤM ĐƯỢC".
    //
    //  Dự án có HAI túi đồ:
    //    • WarehouseManager     — kho vật phẩm mua/chế biến (popup Kho)
    //    • FarmInventoryManager — TÚI NÔNG SẢN thu hoạch từ ruộng
    //  Nông sản (lúa, ngô, cà rốt, bắp cải) đi vào FarmInventoryManager, xem
    //  Assets/_Game/Farm/Scripts/Gameplay/PlotController.cs:660
    //      FarmInventoryManager.Instance.AddItem(harvestItemId, amount);
    //  Bản đầu tiên của file này đọc WarehouseManager ⇒ luôn thấy 0 lúa ⇒ DuNguyenLieu()
    //  luôn false ⇒ nút lớn vĩnh viễn "THIẾU NGUYÊN LIỆU" (interactable = false) ⇒ popup
    //  trông như một tấm ảnh chết. Nay đọc/ghi đúng túi nông sản.
    //
    //  API đã đối chiếu trực tiếp trong FarmInventoryManager.cs:
    //      .CanAddItem(string itemId) → bool          (dòng 119)
    //      .AddItem(string itemId, int amount) → bool (dòng 136 — CHỈ 2 tham số, không có
    //                                                  displayName/icon như WarehouseManager)
    //      .GetAmount(string itemId) → int            (dòng 164)
    //      .HasItem(string itemId, int amount = 1)    (dòng 171)
    //      .RemoveItem(string itemId, int amount)     (dòng 178)
    //      .SlotCapacity / .UsedSlots / .IsFull

    /// <summary>Số lượng nông sản đang có trong túi. Không có manager ⇒ trả 0.</summary>
    public static int SoLuongTrongKho(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;

        if (FarmInventoryManager.Instance == null)
        {
            CanhBaoKho();
            return 0;
        }

        return FarmInventoryManager.Instance.GetAmount(itemId);
    }

    /// <summary>Có đủ nguyên liệu cho toàn bộ công thức hay không.</summary>
    public static bool DuNguyenLieu(MillRecipeData r)
    {
        if (r == null || r.ingredients == null) return false;

        bool coItNhatMotNguyenLieu = false;

        for (int i = 0; i < r.ingredients.Length; i++)
        {
            MillIngredient ing = r.ingredients[i];
            if (ing == null || string.IsNullOrEmpty(ing.itemId)) continue;

            coItNhatMotNguyenLieu = true;

            if (SoLuongTrongKho(ing.itemId) < ing.amount)
                return false;
        }

        // Công thức chưa điền itemId nào (data còn trống) ⇒ coi như CHƯA đủ, để không cho
        // xay ra sản phẩm từ hư không.
        return coItNhatMotNguyenLieu;
    }

    /// <summary>
    /// Trừ toàn bộ nguyên liệu của công thức.
    /// TRẢ VỀ false và KHÔNG TRỪ GÌ nếu thiếu — kiểm tra đủ trước rồi mới trừ, để không
    /// bao giờ xảy ra cảnh trừ được nửa công thức rồi hết hàng giữa đường.
    /// </summary>
    public static bool TruNguyenLieu(MillRecipeData r)
    {
        if (!DuNguyenLieu(r)) return false;

        if (FarmInventoryManager.Instance == null)
        {
            CanhBaoKho();
            return false;
        }

        for (int i = 0; i < r.ingredients.Length; i++)
        {
            MillIngredient ing = r.ingredients[i];
            if (ing == null || string.IsNullOrEmpty(ing.itemId)) continue;

            FarmInventoryManager.Instance.RemoveItem(ing.itemId, ing.amount);
        }

        return true;
    }

    /// <summary>
    /// Túi còn nhận được sản phẩm của công thức này hay không (kiểm TRƯỚC khi bấm THU).
    /// Loại đã có trong túi thì luôn nhận thêm được; chỉ LOẠI MỚI mới cần slot trống.
    /// </summary>
    public static bool CoCho(MillRecipeData r)
    {
        if (r == null || string.IsNullOrEmpty(r.outputItemId)) return false;

        if (FarmInventoryManager.Instance == null)
        {
            CanhBaoKho();
            return false;
        }

        return FarmInventoryManager.Instance.CanAddItem(r.outputItemId);
    }

    /// <summary>
    /// Cộng sản phẩm của công thức vào túi (bấm THU).
    /// TRẢ VỀ false khi túi đầy — nơi gọi PHẢI giữ nguyên slot, không được xoá, nếu không
    /// người chơi mất trắng mẻ hàng.
    /// </summary>
    public static bool CongSanPham(MillRecipeData r)
    {
        if (r == null || string.IsNullOrEmpty(r.outputItemId)) return false;

        if (FarmInventoryManager.Instance == null)
        {
            CanhBaoKho();
            return false;
        }

        return FarmInventoryManager.Instance.AddItem(r.outputItemId, r.outputAmount);
    }

    // ═══════════════════════════════ KIM CƯƠNG ═══════════════════════════════

    /// <summary>Số kim cương hiện có. Không có manager ⇒ 0.</summary>
    public static int SoKimCuong()
    {
        if (FarmEconomyManager.Instance == null)
        {
            CanhBaoTien();
            return 0;
        }

        return FarmEconomyManager.Instance.Gems;
    }

    /// <summary>
    /// Trừ kim cương. Trả về false nếu không đủ (hoặc không có manager) — KHÔNG tự cộng bù,
    /// KHÔNG tự mở popup mua thêm. Nơi gọi quyết định làm gì tiếp.
    /// </summary>
    public static bool TruKimCuong(int soLuong)
    {
        if (soLuong <= 0) return true;

        if (FarmEconomyManager.Instance == null)
        {
            CanhBaoTien();
            return false;
        }

        return FarmEconomyManager.Instance.SpendGems(soLuong);
    }

    // ═══════════════════════════════ CẤP ═══════════════════════════════

    /// <summary>Cấp nông trại hiện tại. Không có manager ⇒ 1 (mở những gì mở từ đầu).</summary>
    public static int CapHienTai()
    {
        if (FarmLevelManager.Instance == null)
        {
            CanhBaoCap();
            return 1;
        }

        return FarmLevelManager.Instance.CurrentLevel;
    }

    /// <summary>Đã đạt cấp yêu cầu chưa.</summary>
    public static bool DatCap(int capYeuCau)
    {
        if (capYeuCau <= 1) return true;

        if (FarmLevelManager.Instance == null)
        {
            CanhBaoCap();
            return false;
        }

        return FarmLevelManager.Instance.HasReached(capYeuCau);
    }

    // ═══════════════════════════════ CẢNH BÁO ═══════════════════════════════

    private static void CanhBaoKho()
    {
        if (_daCanhBaoKho) return;
        _daCanhBaoKho = true;
        Debug.LogWarning(LOG + "Không tìm thấy FarmInventoryManager.Instance trong scene. " +
                         "Popup máy xay sẽ coi túi nông sản là RỖNG và không cộng được sản phẩm. " +
                         "Kiểm tra xem scene farm có object mang FarmInventoryManager không.");
    }

    private static void CanhBaoTien()
    {
        if (_daCanhBaoTien) return;
        _daCanhBaoTien = true;
        Debug.LogWarning(LOG + "Không tìm thấy FarmEconomyManager.Instance. " +
                         "Số dư kim cương hiện 0 và mọi giao dịch kim cương sẽ bị từ chối.");
    }

    private static void CanhBaoCap()
    {
        if (_daCanhBaoCap) return;
        _daCanhBaoCap = true;
        Debug.LogWarning(LOG + "Không tìm thấy FarmLevelManager.Instance. " +
                         "Coi như cấp 1: công thức và slot yêu cầu cấp cao sẽ ở trạng thái khoá.");
    }
}
