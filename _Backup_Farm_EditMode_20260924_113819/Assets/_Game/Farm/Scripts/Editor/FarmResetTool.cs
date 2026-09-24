#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// HARD RESET EVERYTHING — "ĐƯỜNG B" (menu Editor) của việc xoá tiến độ.
/// Menu: Tools ▸ SCN Farm ▸ Hard Reset Everything
///
/// ══ NGUỒN SỰ THẬT DUY NHẤT ══
/// Đường A — nút "Chơi lại từ đầu" trong Cài đặt — là bản đầy đủ, đã rà kỹ:
///     SettingsPopupUI.OnResetProgressClicked()  →  SettingsPopupUI.cs dòng 322-395.
/// File này KHÔNG tự bịa ra danh sách bước xoá riêng nữa:
///   • Play Mode, tìm thấy SettingsPopupUI → GỌI THẲNG đường A. Uỷ quyền thật, không chép.
///   • Play Mode, không tìm thấy           → bản SOI GƯƠNG, mỗi bước chú thích số dòng đường A.
///   • Edit Mode                           → chỉ phần XOÁ TRÊN ĐĨA của đường A. Destroy manager
///     và LoadScene là vô nghĩa ngoài Play Mode, và UnityEngine.Object.Destroy còn ném lỗi.
///
/// ══ 3 LỖ HỔNG CỦA BẢN CŨ (đã vá ở đây) ══
///  1. Thiếu SaveSystem.DeleteSave()     ⇒ save.json trong persistentDataPath còn nguyên;
///     lần Play sau SaveSystem nạp lại và đè lên PlayerPrefs vừa xoá.
///  2. Thiếu SaveVersionGuard.ClearAll() ⇒ dấu phiên bản của mọi họ save còn sót lại.
///     Chính SaveVersionGuard.cs dòng 86-88 đã ghi rõ FarmResetTool phải duyệt AllFamilies.
///  3. Chỉ đụng 4 manager và chỉ reset RAM (không Destroy), bỏ sót 5 cái mà đường A có xử lý:
///     KitchenTransferManager, MissionProgressTracker, AnimalGuideController,
///     TutorialManager, TownshipHUDController.
/// </summary>
public static class FarmResetTool
{
    private const string MenuPath = "Tools/SCN Farm/Hard Reset Everything";

    /// <summary>
    /// Cờ hẹn xoá SAU KHI Unity đã dừng hẳn Play Mode. Cùng thủ thuật với
    /// ChoiLaiTuDauTool.cs dòng 10, nhưng dùng key riêng để hai tool không kích nhầm nhau.
    /// </summary>
    private const string CoHenXoaSauKhiStop = "SCNFARM_HARDRESET_XOA_SAU_KHI_STOP";

    // ═════════════════════════════════════════════════════════════════════════
    //  CỬA VÀO
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(MenuPath)]
    private static void HardResetEverything()
    {
        if (Application.isPlaying)
        {
            XuLyKhiDangPlay();
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Hard Reset Everything",
                "Xoá TOÀN BỘ tiến độ, đưa game về đúng trạng thái người chơi mới:\n\n" +
                "  • save.json (+ .bak/.tmp) trong persistentDataPath\n" +
                "  • Toàn bộ PlayerPrefs: ô đất, nhà, tiền, kho, nhiệm vụ, chuồng\n" +
                "  • Cờ tutorial và dấu phiên bản của mọi họ save\n\n" +
                "Đang ở Edit Mode nên không manager nào cần dọn.\n" +
                "KHÔNG THỂ HOÀN TÁC.",
                "Xoá hết", "Huỷ"))
            return;

        var bc = new System.Text.StringBuilder();
        bc.AppendLine("═══ [HardReset] ĐÃ XOÁ — Edit Mode ═══");
        XoaTrenDia(bc);
        bc.AppendLine("  • Manager: không cái nào đang sống ở Edit Mode → không cần Destroy.");
        bc.AppendLine("  • Lần Play tới sẽ là người chơi mới hoàn toàn.");
        { Debug.Log(bc.ToString()); }
    }

    /// <summary>
    /// Đang Play thì có hai lối đúng, và chúng khác nhau thật sự — nên hỏi thay vì tự quyết:
    ///   ① Thoát Play rồi xoá: Unity huỷ MỌI object khi rời Play Mode, kể cả SaveBootstrap
    ///     (Save/SaveBootstrap.cs dòng 65) vốn tự lưu định kỳ ở dòng 271 và lưu lúc thoát ở
    ///     dòng 279. Không cái nào kịp ghi đè dữ liệu cũ trở lại. Đây là lối chắc chắn nhất.
    ///   ② Reset ngay trong Play: chạy đúng thứ người chơi thật chạy, để kiểm thử đường A.
    /// </summary>
    private static void XuLyKhiDangPlay()
    {
        int chon = EditorUtility.DisplayDialogComplex(
            "Hard Reset Everything — đang ở Play Mode",
            "Xoá TOÀN BỘ tiến độ. KHÔNG THỂ HOÀN TÁC.\n\n" +
            "① THOÁT PLAY RỒI XOÁ  (khuyên dùng)\n" +
            "     Unity dừng hẳn → mọi manager tự chết → mới xoá đĩa.\n" +
            "     Không manager nào kịp ghi dữ liệu cũ trở lại.\n\n" +
            "② RESET NGAY TRONG PLAY  (uỷ quyền sang đường A)\n" +
            "     Gọi thẳng SettingsPopupUI.OnResetProgressClicked() — đúng y hệt\n" +
            "     nút \"Chơi lại từ đầu\" người chơi bấm. Scene sẽ tự nạp lại.",
            "① Thoát Play rồi xoá",
            "Huỷ",
            "② Reset ngay trong Play");

        if (chon == 1) return;

        if (chon == 0)
        {
            EditorPrefs.SetBool(CoHenXoaSauKhiStop, true);
            EditorApplication.isPlaying = false;
            { Debug.Log("[HardReset] Đang thoát Play Mode… dữ liệu sẽ được xoá ngay khi Unity dừng hẳn. Không cần bấm gì thêm."); }
            return;
        }

        ResetNgayTrongPlay();
    }

    /// <summary>
    /// Chờ Unity rời hẳn Play Mode rồi mới xoá. `EditorApplication.isPlaying = false`
    /// không dừng ngay — nó chỉ đặt lệnh, phải mất vài khung hình (ChoiLaiTuDauTool.cs dòng 40-58).
    /// </summary>
    [InitializeOnLoadMethod]
    private static void DangKyTheoDoiPlayMode()
    {
        EditorApplication.playModeStateChanged -= OnDoiTrangThaiPlay;
        EditorApplication.playModeStateChanged += OnDoiTrangThaiPlay;
    }

    private static void OnDoiTrangThaiPlay(PlayModeStateChange trangThai)
    {
        if (trangThai != PlayModeStateChange.EnteredEditMode) return;
        if (!EditorPrefs.GetBool(CoHenXoaSauKhiStop, false)) return;

        EditorPrefs.DeleteKey(CoHenXoaSauKhiStop);

        var bc = new System.Text.StringBuilder();
        bc.AppendLine("═══ [HardReset] ĐÃ XOÁ — ngay sau khi thoát Play Mode ═══");
        XoaTrenDia(bc);
        bc.AppendLine("  • Manager: Unity đã tự huỷ toàn bộ khi rời Play Mode → không sót RAM cũ.");
        bc.AppendLine("  • Lần Play tới sẽ là người chơi mới hoàn toàn.");
        { Debug.Log(bc.ToString()); }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  UỶ QUYỀN SANG ĐƯỜNG A
    // ═════════════════════════════════════════════════════════════════════════

    private static void ResetNgayTrongPlay()
    {
        // Uỷ quyền THẬT: gọi đúng hàm mà nút trong game gọi, không chép lại dòng nào.
        // GameProgressionStudioOverlay.cs dòng 191-192 cũng vào bằng đúng lối này.
        // FindObjectsInactive.Include là bắt buộc — popup Cài đặt gần như luôn tắt sẵn,
        // bản mặc định của FindFirstObjectByType sẽ bỏ qua nó và ta rơi nhầm sang soi gương.
        var settings = UnityEngine.Object.FindFirstObjectByType<SettingsPopupUI>(FindObjectsInactive.Include);

        if (settings != null)
        {
            { Debug.Log("[HardReset] UỶ QUYỀN sang ĐƯỜNG A — SettingsPopupUI.OnResetProgressClicked() (SettingsPopupUI.cs:322). Chi tiết từng bước xem log [Settings] và [Save] ngay bên dưới."); }
            settings.OnResetProgressClicked();
            return;
        }

        { Debug.LogWarning("[HardReset] Không thấy SettingsPopupUI nào trong scene (kể cả object đang tắt) → chạy BẢN SOI GƯƠNG của đường A ngay tại đây."); }

        var bc = new System.Text.StringBuilder();
        bc.AppendLine("═══ [HardReset] ĐÃ XOÁ — Play Mode, bản soi gương đường A ═══");
        SoiGuongDuongA(bc);
        { Debug.Log(bc.ToString()); }
    }

    /// <summary>
    /// Bản SOI GƯƠNG từng bước của SettingsPopupUI.OnResetProgressClicked().
    /// Chỉ dùng khi không tìm được SettingsPopupUI để uỷ quyền.
    /// Sửa đường A thì phải sửa cả đây — mỗi bước đã ghi rõ số dòng gốc để đối chiếu.
    /// </summary>
    private static void SoiGuongDuongA(System.Text.StringBuilder bc)
    {
        XoaTrenDia(bc);

        // [Đường A dòng 336-384] Dọn dữ liệu trong RAM rồi HUỶ LUÔN GameObject.
        // Vì sao phải Destroy chứ không chỉ reset số liệu: manager giữ Instance tĩnh và
        // vẫn sống qua scene load, nên lần AddItem/SetGold kế tiếp sẽ ghi lại y nguyên
        // trạng thái cũ — đúng cái bẫy bản cũ của file này đã dính.
        int soManager = 0;

        // [Đường A dòng 337-341]
        if (FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.ResetCurrency();
            UnityEngine.Object.Destroy(FarmEconomyManager.Instance.gameObject);
            bc.AppendLine("      - FarmEconomyManager: ResetCurrency() + Destroy");
            soManager++;
        }

        // [Đường A dòng 343-347]
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.ForceSetLevelExp(1, 0);
            UnityEngine.Object.Destroy(PlayerProgressManager.Instance.gameObject);
            bc.AppendLine("      - PlayerProgressManager: ForceSetLevelExp(1, 0) + Destroy");
            soManager++;
        }

        // [Đường A dòng 349-353]
        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.XoaSaveVaLamTrongKho();
            UnityEngine.Object.Destroy(WarehouseManager.Instance.gameObject);
            bc.AppendLine("      - WarehouseManager: XoaSaveVaLamTrongKho() + Destroy");
            soManager++;
        }

        // [Đường A dòng 355-359]
        if (FarmInventoryManager.Instance != null)
        {
            FarmInventoryManager.Instance.ClearAll();
            UnityEngine.Object.Destroy(FarmInventoryManager.Instance.gameObject);
            bc.AppendLine("      - FarmInventoryManager: ClearAll() + Destroy");
            soManager++;
        }

        // [Đường A dòng 361-364] — bản cũ của file này BỎ SÓT từ đây trở xuống.
        if (KitchenTransferManager.Instance != null)
        {
            UnityEngine.Object.Destroy(KitchenTransferManager.Instance.gameObject);
            bc.AppendLine("      - KitchenTransferManager: Destroy   (bản cũ bỏ sót)");
            soManager++;
        }

        // [Đường A dòng 366-369]
        if (MissionProgressTracker.Instance != null)
        {
            UnityEngine.Object.Destroy(MissionProgressTracker.Instance.gameObject);
            bc.AppendLine("      - MissionProgressTracker: Destroy   (bản cũ bỏ sót)");
            soManager++;
        }

        // [Đường A dòng 371-374]
        if (AnimalGuideController.Instance != null)
        {
            UnityEngine.Object.Destroy(AnimalGuideController.Instance.gameObject);
            bc.AppendLine("      - AnimalGuideController: Destroy    (bản cũ bỏ sót)");
            soManager++;
        }

        // [Đường A dòng 376-379]
        if (TutorialManager.Instance != null)
        {
            UnityEngine.Object.Destroy(TutorialManager.Instance.gameObject);
            bc.AppendLine("      - TutorialManager: Destroy          (bản cũ bỏ sót)");
            soManager++;
        }

        // [Đường A dòng 381-384]
        if (FarmGame.UI.TownshipHUDController.Instance != null)
        {
            UnityEngine.Object.Destroy(FarmGame.UI.TownshipHUDController.Instance.gameObject);
            bc.AppendLine("      - TownshipHUDController: Destroy    (bản cũ bỏ sót)");
            soManager++;
        }

        bc.AppendLine("  • Manager đã dọn: " + soManager + "/9 (cái nào không có Instance thì không cần dọn)");

        // [Đường A dòng 392-394] Nạp lại scene hiện tại cho sạch.
        int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
        bc.AppendLine("  • Nạp lại scene buildIndex=" + sceneIndex + " (đường A dòng 392-394)");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PHẦN XOÁ TRÊN ĐĨA — dùng chung cho mọi lối vào
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Soi gương SettingsPopupUI.cs dòng 327-334 và 386-390 (phần không đụng tới manager).
    /// Ghi kết quả vào <paramref name="bc"/> để nơi gọi in ra Console MỘT lần.
    /// </summary>
    private static void XoaTrenDia(System.Text.StringBuilder bc)
    {
        int khoaTruoc  = DemKhoaCoTen();
        bool coFileSave = SaveSystem.HasSaveFile();

        // [Đường A dòng 327-328] Xoá save.json (+ .bak/.tmp) trên ổ đĩa.
        // Đây là lỗ hổng số 1 của bản cũ: PlayerPrefs sạch nhưng file save vẫn nạp lại.
        SaveSystem.DeleteSave();

        // [Đường A dòng 330-334]
        PlayerPrefs.DeleteAll();
        TutorialManager.ClearTutorialDoneFlag();
        SaveVersionGuard.ClearAll();          // lỗ hổng số 2 của bản cũ
        PlayerPrefs.Save();

        // [Đường A dòng 386-390] Quét lại 3 cờ có thể mọc lại trong lúc ClearAll chạy.
        PlayerPrefs.DeleteKey("FARM_INVENTORY_SAVE");
        PlayerPrefs.DeleteKey("STARTER_ITEMS_GIVEN");
        PlayerPrefs.DeleteKey("TUTORIAL_MAIN_DONE");
        PlayerPrefs.Save();

        int khoaSau = DemKhoaCoTen();

        bc.AppendLine("  • save.json: " + (coFileSave ? "CÓ → đã xoá (kèm .bak/.tmp)" : "vốn không có") + "   [" + SaveSystem.SavePath + "]");
        bc.AppendLine("  • PlayerPrefs.DeleteAll(): xong — " + khoaTruoc + " khoá có tên cố định trước khi xoá, còn lại " + khoaSau + " sau khi xoá");
        bc.AppendLine("  • SaveVersionGuard.ClearAll(): xoá dấu phiên bản của " + SaveVersionGuard.AllFamilies.Length + " họ save");
        bc.AppendLine("  • TutorialManager.ClearTutorialDoneFlag(): xong");
        bc.AppendLine("  • Xoá lại 3 cờ hay mọc lại: FARM_INVENTORY_SAVE, STARTER_ITEMS_GIVEN, TUTORIAL_MAIN_DONE");
    }

    /// <summary>
    /// Đếm số khoá CÓ TÊN CỐ ĐỊNH còn trong PlayerPrefs.
    /// PlayerPrefs không có API liệt kê, nên đây là cách duy nhất để báo con số THẬT thay vì
    /// đoán. Khoá sinh động (PenState_*, PLOT_NORMAL_*, MISSION_CLAIMED_*) không đếm được ở
    /// đây — DeleteAll() vẫn quét sạch chúng, chỉ là không hiện trong con số này.
    /// Danh sách tên lấy từ ChoiLaiTuDauTool.cs dòng 87-96.
    /// </summary>
    private static int DemKhoaCoTen()
    {
        int n = 0;

        for (int i = 0; i < KhoaCoTenCoDinh.Length; i++)
        {
            if (PlayerPrefs.HasKey(KhoaCoTenCoDinh[i])) n++;
        }

        // Cả dấu phiên bản của từng họ save (SaveVersionGuard.cs dòng 91-105).
        for (int i = 0; i < SaveVersionGuard.AllFamilies.Length; i++)
        {
            if (PlayerPrefs.HasKey(SaveVersionGuard.KeyFor(SaveVersionGuard.AllFamilies[i]))) n++;
        }

        return n;
    }

    private static readonly string[] KhoaCoTenCoDinh =
    {
        "PLAYER_LEVEL", "PLAYER_EXP", "FARM_ECONOMY_GOLD", "FARM_ECONOMY_GEMS",
        "FARM_WAREHOUSE", "FARM_INVENTORY_SAVE", "STARTER_ITEMS_GIVEN",
        "TUTORIAL_MAIN_DONE", "TUTORIAL_PREPLANT_DONE", "MISSION_PROGRESS_V1",
        "OrderBoard_Save", "FARM_PLAYER_STALL", "KITCHEN_TRANSFER_SAVE",
        "FARM_PLACED_BUILDINGS", "FARM_CONSTRUCTION_SITES",
        "ANIMAL_GUIDE_COOP_FEED_DONE", "GUIDE_COOKING_DONE",
        "GUIDE_DELIVER_DONE", "GUIDE_TRAIN_DONE",
    };
}
#endif
