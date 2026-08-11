using UnityEditor;
using UnityEngine;

/// <summary>
/// CHƠI LẠI TỪ ĐẦU — đưa máy về đúng trạng thái của một người vừa cài game.
///
/// VÌ SAO PHẢI CÓ TOOL NÀY dù đã có "Force Level 1" và "Reset Player Save":
/// hai tool cũ chỉ xoá 4 key `PLAYER_LEVEL`, `PLAYER_EXP`, `FARM_ECONOMY_GOLD`,
/// `FARM_ECONOMY_GEMS`. Toàn bộ phần còn lại của save vẫn nguyên:
///
///   • `FARM_WAREHOUSE`      — kho hạt. Còn key này thì `StarterInventorySetup` thấy
///                             `DaCoSaveKho == true` và KHÔNG cấp 10 lúa + 10 hướng dương.
///                             Đây đúng là cảnh "về cấp 1 mà không có lúa", trong khi
///                             cà chua/bắp cải của lần chơi trước vẫn nằm đó.
///   • `TUTORIAL_MAIN_DONE`  — tutorial tưởng đã xong hoặc dở dang, chạy lệch pha với cấp.
///   • `MISSION_PROGRESS_V1` — nhiệm vụ vẫn 2/2 ngay từ giây đầu.
///   • `FARM_INVENTORY_SAVE`, `OrderBoard_Save`, `FARM_PLAYER_STALL`, `PenState_*`,
///     `FARM_PLACED_BUILDINGS`, `FARM_CONSTRUCTION_SITES`, `STARTER_ITEMS_GIVEN`…
///
/// Nên tool này dùng <c>PlayerPrefs.DeleteAll()</c> — quét sạch, kể cả những key sinh
/// động kiểu `PenState_<id>` mà không cách nào liệt kê hết bằng tay.
///
/// BẪY LỚN NHẤT và lý do tool tự thoát Play Mode: các manager là `DontDestroyOnLoad` và
/// giữ dữ liệu TRONG BỘ NHỚ. Xoá PlayerPrefs giữa lúc đang Play thì lần `Save()` kế tiếp
/// (thu hoạch, mua bán, đổi cấp) ghi nguyên si dữ liệu cũ trở lại — người dùng tưởng đã
/// reset mà thật ra chưa. Thoát Play trước rồi mới xoá là cách duy nhất chắc chắn.
/// </summary>
public static class ChoiLaiTuDauTool
{
    private const string Menu = "Tools/Farm/⚠ CHƠI LẠI TỪ ĐẦU (như người chơi mới)";

    /// <summary>Đặt cờ này trước khi thoát Play, để xoá sau khi Unity đã dừng hẳn.</summary>
    private const string CoCanXoa = "CHOILAI_XOA_SAU_KHI_STOP";

    [MenuItem(Menu, false, 1)]
    public static void ChoiLaiTuDau()
    {
        bool dongY = EditorUtility.DisplayDialog(
            "Chơi lại từ đầu",
            "Xoá TOÀN BỘ dữ liệu đã lưu, đưa game về đúng trạng thái người chơi mới:\n\n" +
            "  • Cấp 1, EXP 0, vàng/gem về mặc định\n" +
            "  • Kho trống → được cấp lại 10 hạt lúa + 10 hạt hướng dương\n" +
            "  • Tutorial chạy lại từ bước đầu\n" +
            "  • Nhiệm vụ, đơn hàng, quầy hàng, chuồng, công trình: xoá sạch\n\n" +
            "KHÔNG THỂ HOÀN TÁC.",
            "Xoá và chơi lại", "Huỷ");

        if (!dongY) return;

        if (Application.isPlaying)
        {
            // Hẹn xoá sau khi dừng hẳn. Xoá ngay bây giờ là vô nghĩa: manager còn sống
            // sẽ ghi đè lại trong vài giây tới.
            EditorPrefs.SetBool(CoCanXoa, true);
            EditorApplication.isPlaying = false;
            Debug.Log("[ChơiLại] Đang thoát Play Mode… dữ liệu sẽ được xoá ngay khi dừng hẳn.");
            return;
        }

        XoaThat();
    }

    /// <summary>
    /// Chờ Unity rời hẳn Play Mode rồi mới xoá. `EditorApplication.isPlaying = false`
    /// không dừng ngay lập tức — nó chỉ đặt lệnh, phải mất vài khung hình.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void DangKyTheoDoiPlayMode()
    {
        EditorApplication.playModeStateChanged -= OnDoiTrangThai;
        EditorApplication.playModeStateChanged += OnDoiTrangThai;
    }

    private static void OnDoiTrangThai(PlayModeStateChange trangThai)
    {
        if (trangThai != PlayModeStateChange.EnteredEditMode) return;
        if (!EditorPrefs.GetBool(CoCanXoa, false)) return;

        EditorPrefs.DeleteKey(CoCanXoa);
        XoaThat();
    }

    private static void XoaThat()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("[ChơiLại] ✅ Đã xoá sạch dữ liệu lưu.\n" +
                  "Bấm Play — game sẽ chạy đúng như lần đầu cài đặt:\n" +
                  "  · Cấp 1, kho có 10 hạt lúa + 10 hạt hướng dương\n" +
                  "  · Tutorial bắt đầu từ bước chào\n" +
                  "  · HUD nhiệm vụ ẩn cho tới khi xong tutorial và đạt cấp 3");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  KIỂM TRA NHANH: xem hiện đang lưu những gì
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Farm/Xem Dữ Liệu Đang Lưu", false, 2)]
    public static void XemDuLieuDangLuu()
    {
        // Không có API liệt kê PlayerPrefs, nên chỉ dò được các key đã biết tên.
        // Key sinh động (`PenState_<id>`) không hiện ở đây — `DeleteAll` vẫn quét được.
        string[] keys =
        {
            "PLAYER_LEVEL", "PLAYER_EXP", "FARM_ECONOMY_GOLD", "FARM_ECONOMY_GEMS",
            "FARM_WAREHOUSE", "FARM_INVENTORY_SAVE", "STARTER_ITEMS_GIVEN",
            "TUTORIAL_MAIN_DONE", "TUTORIAL_PREPLANT_DONE", "MISSION_PROGRESS_V1",
            "OrderBoard_Save", "FARM_PLAYER_STALL", "KITCHEN_TRANSFER_SAVE",
            "FARM_PLACED_BUILDINGS", "FARM_CONSTRUCTION_SITES",
            "ANIMAL_GUIDE_COOP_FEED_DONE", "GUIDE_COOKING_DONE",
            "GUIDE_DELIVER_DONE", "GUIDE_TRAIN_DONE",
        };

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══ DỮ LIỆU ĐANG LƯU ═══");

        int co = 0;
        foreach (string k in keys)
        {
            bool ton = PlayerPrefs.HasKey(k);
            if (ton) co++;
            sb.AppendLine($"  {(ton ? "CÓ  " : "  -  ")} {k}");
        }

        sb.AppendLine($"───────────────────────");
        sb.AppendLine(co == 0
            ? "  Sạch — lần Play tới là người chơi mới."
            : $"  {co} key còn dữ liệu. Muốn chơi lại từ đầu thì dùng menu ngay trên.");

        Debug.Log(sb.ToString());
    }
}
