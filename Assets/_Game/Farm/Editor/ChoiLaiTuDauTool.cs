using UnityEditor;
using UnityEngine;


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
        Debug.Log("[ChơiLại] ✅ ĐÃ XOÁ SẠCH TOÀN BỘ SAVE (PlayerPrefs.DeleteAll). Lần Play tới game sẽ ở trạng thái người chơi mới hoàn toàn!");
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
