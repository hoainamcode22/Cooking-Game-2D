using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Tool: Tools/Farm Game/Test/...
///
/// Bộ công cụ test nhanh flow Level 1-6 mà không cần chơi thủ công từ đầu.
///
/// TẤT CẢ tool ở đây chỉ READ hoặc mô phỏng nhẹ (ForceSetLevel).
/// Không xóa file, không phá prefab, không push data.
///
/// Dùng trong Play Mode hoặc Edit Mode (tuỳ tool).
/// </summary>
public static class Phase1TestTool
{
    private const string BASE = "Tools/Farm Game/Test/";

    // ── Simulate Level Up ─────────────────────────────────────────────────────

    // Tên cũ là "Force Level 1 (Reset)" — chữ "(Reset)" gây hiểu nhầm nặng: nó CHỈ đổi
    // con số cấp độ, kho/tutorial/nhiệm vụ giữ nguyên. Người dùng bấm nó tưởng đã về
    // trạng thái người chơi mới, vào game thấy cấp 1 mà không có hạt lúa (kho cũ vẫn
    // còn nên StarterInventorySetup không cấp lại) và nhiệm vụ đã 2/2 sẵn.
    // Muốn chơi lại thật thì dùng: Tools ▸ Farm ▸ ⚠ CHƠI LẠI TỪ ĐẦU.
    [MenuItem(BASE + "Force Level 1 (chỉ đổi cấp, KHÔNG xoá save)")]
    private static void ForceLevel1()  => ForceLevel(1);

    [MenuItem(BASE + "Force Level 2")]
    private static void ForceLevel2()  => ForceLevel(2);

    [MenuItem(BASE + "Force Level 3")]
    private static void ForceLevel3()  => ForceLevel(3);

    [MenuItem(BASE + "Force Level 4")]
    private static void ForceLevel4()  => ForceLevel(4);

    [MenuItem(BASE + "Force Level 5 (Cooking Unlock)")]
    private static void ForceLevel5()  => ForceLevel(5);

    [MenuItem(BASE + "Force Level 6")]
    private static void ForceLevel6()  => ForceLevel(6);

    private static void ForceLevel(int level)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Phase 1 Test",
                "Cần ở trong Play Mode để Force Level!\n\nNhấn Play rồi chạy lại.", "OK");
            return;
        }

        var pm = PlayerProgressManager.Instance;
        if (pm == null) { Debug.LogError("[Phase1Test] PlayerProgressManager.Instance = null!"); return; }

        int prevLevel = pm.Level;
        pm.ForceSetLevelExp(level, 0);
        Debug.Log($"[Phase1Test] Level {prevLevel} → {level}  (EXP reset to 0)");
    }

    // ── Test Currency (đang dựng game) ────────────────────────────────────────

    /// <summary>
    /// Giao cho <see cref="NapTienTestTool"/>.
    ///
    /// Bản cũ ở đây gọi `SetCurrency(1000, 1000)` — ĐẶT BẰNG chứ không CỘNG THÊM. Đang
    /// có 25.000 vàng mà bấm "Give Test Currency" là tụt còn 1.000: tên tool nói nạp
    /// tiền, hành vi thật là xoá tiền.
    /// </summary>
    [MenuItem(BASE + "Nạp +1000 vàng / +1000 gem")]
    private static void GiveTestCurrency() => NapTienTestTool.Nap(1000, 1000);

    // ── Status Report ─────────────────────────────────────────────────────────

    [MenuItem(BASE + "Print Player Status")]
    private static void PrintPlayerStatus()
    {
        if (!Application.isPlaying)
        {
            // Read từ PlayerPrefs khi không ở Play Mode
            int level = PlayerPrefs.GetInt("PLAYER_LEVEL", 1);
            int exp   = PlayerPrefs.GetInt("PLAYER_EXP",   0);
            int gold  = PlayerPrefs.GetInt("FARM_ECONOMY_GOLD", 0);
            int gems  = PlayerPrefs.GetInt("FARM_ECONOMY_GEMS", 0);

            Debug.Log("═══ PLAYER STATUS (from PlayerPrefs — not in Play Mode) ═══");
            Debug.Log($"  Level:     {level}");
            Debug.Log($"  EXP:       {exp}");
            Debug.Log($"  Gold:      {gold}");
            Debug.Log($"  Gems:      {gems}");
            Debug.Log("═══════════════════════════════════════════════════════════");
            return;
        }

        var pm  = PlayerProgressManager.Instance;
        var eco = FarmEconomyManager.Instance;

        Debug.Log("═══ PLAYER STATUS (Play Mode) ═══");
        if (pm  != null) Debug.Log($"  Level={pm.Level}  EXP={pm.CurrentExp}/{pm.RequiredExpCurrentLevel}");
        else             Debug.LogWarning("  PlayerProgressManager: NULL");

        if (eco != null) Debug.Log($"  Gold={eco.Gold}  Gems={eco.Gems}");
        else             Debug.LogWarning("  FarmEconomyManager: NULL");
        Debug.Log("═════════════════════════════════");
    }

    // ── Village Orders Report ─────────────────────────────────────────────────

    [MenuItem(BASE + "Print Order Board Status")]
    private static void PrintOrderBoard()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[Phase1Test] Cần Play Mode để in trạng thái Bảng Đơn Hàng.");
            return;
        }

        var board = OrderBoardManagerBase.Instance as OrderBoardManager;
        if (board == null) { Debug.LogWarning("[Phase1Test] OrderBoardManager.Instance = null!"); return; }

        var orders = board.GetOrders();
        Debug.Log($"[Phase1Test] Bảng đơn: {orders.Count}/{OrderBoardManagerBase.SlotCount} ô — " +
                  $"{board.CountDeliverableOrders()} đơn giao được ngay.");

        for (int i = 0; i < orders.Count; i++)
        {
            var o = orders[i];
            if (o == null) { Debug.Log($"  ô {i}: (trống)"); continue; }

            string items = string.Empty;
            for (int j = 0; j < o.requirements.Count; j++)
            {
                var r = o.requirements[j];
                if (r == null) continue;
                if (items.Length > 0) items += " + ";
                items += $"{board.GetOwnedAmount(r.itemId)}/{r.needAmount} {r.itemId}";
            }

            Debug.Log($"  ô {i}: \"{o.title}\" [{items}] → {o.rewardGold}v {o.rewardExp}exp" +
                      (o.CanDeliverNow() ? "  ✓" : string.Empty));
        }
    }

    // ── Check Phase 1 Setup ───────────────────────────────────────────────────

    [MenuItem(BASE + "Check Phase 1 Setup Status")]
    private static void CheckPhase1Setup()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("═══ PHASE 1 SETUP CHECK ═══");

        // 1. LevelUpPopupUI
        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>();
        report.AppendLine(popup != null
            ? "  ✅ LevelUpPopupUI: FOUND in scene"
            : "  ❌ LevelUpPopupUI: NOT FOUND — chạy Tools/Farm Game/Setup Level Up Popup");

        // 2. ShopLevelLockUI (trên prefab instanced)
        var lockUIs = Object.FindObjectsByType<ShopLevelLockUI>(FindObjectsSortMode.None);
        report.AppendLine(lockUIs.Length > 0
            ? $"  ✅ ShopLevelLockUI: {lockUIs.Length} instance(s) active"
            : "  ⚠  ShopLevelLockUI: 0 instances — chạy Tools/Farm Game/Setup Shop Locks");

        // 3. BaseItemData.unlockLevel
        var shopMgr = Object.FindFirstObjectByType<ShopManager>();
        if (shopMgr != null)
        {
            var so = new SerializedObject(shopMgr);
            var seedProp = so.FindProperty("seedList");
            int lockedCount = 0;
            if (seedProp != null)
            {
                for (int i = 0; i < seedProp.arraySize; i++)
                {
                    var data = seedProp.GetArrayElementAtIndex(i).objectReferenceValue as BaseItemData;
                    if (data != null && GetUnlockLevel(data) > 1) lockedCount++;
                }
            }
            report.AppendLine($"  {(lockedCount > 0 ? "✅" : "⚠")} Shop items with unlockLevel>1: {lockedCount}" +
                              (lockedCount == 0 ? " — đặt unlockLevel trong asset BaseItemData" : ""));
        }
        else
        {
            report.AppendLine("  ⚠  ShopManager: NOT FOUND in scene");
        }

        // 4. Tutorial steps count
        string[] guids = AssetDatabase.FindAssets("t:TutorialStepData", new[] { "Assets/Resources/TutorialSteps" });
        report.AppendLine(guids.Length >= 8
            ? $"  ✅ TutorialStepData assets: {guids.Length} (đủ cho L1 tutorial)"
            : $"  ⚠  TutorialStepData assets: {guids.Length} (cần ít nhất 8 — chạy Tools/Farm Game/Setup Tutorial Steps L1)");

        // 5. Món ăn trong pool đơn hàng phải khoá từ L5 trở lên
        //    (không còn asset OrderItemDefinition — pool sinh thẳng từ MarketPriceTable)
        int dishesInPool = 0, dishesTooEarly = 0;
        foreach (var info in MarketPriceTable.AllItems)
        {
            if (!info.MarketEnabled || info.Category != MarketCategory.MonAn) continue;
            dishesInPool++;
            if (info.UnlockLevel < 5) dishesTooEarly++;
        }
        report.AppendLine(dishesTooEarly == 0
            ? $"  ✅ Món ăn trong pool đơn: {dishesInPool}, tất cả khoá từ L5+"
            : $"  ⚠  {dishesTooEarly}/{dishesInPool} món ăn mở trước L5 — người chơi chưa có bếp");

        // 6. Bảng đơn hàng trong scene
        var boards = Object.FindObjectsByType<OrderBoardManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        report.AppendLine(boards.Length == 1
            ? "  ✅ OrderBoardManager: đúng 1 trong scene"
            : $"  ⚠  OrderBoardManager: {boards.Length} trong scene (phải đúng 1)");

        report.AppendLine("═══════════════════════════════════════");
        Debug.Log(report.ToString());

        EditorUtility.DisplayDialog("Phase 1 Setup Check",
            "Kết quả đã được in trong Console.\n\nMở Console (Window > General > Console) để xem chi tiết.",
            "OK");
    }

    // ── Reset Player Save (cẩn thận!) ────────────────────────────────────────

    /// <summary>
    /// Chỉ xoá 4 con số cấp/tiền. GIỮ NGUYÊN kho, tutorial, nhiệm vụ, đơn hàng.
    ///
    /// Tool này từng mang tên "Reset Player Save" và mô tả là "xóa toàn bộ dữ liệu lưu
    /// của người chơi" — sai, và cái sai đó tốn nhiều giờ mò lỗi: về cấp 1 nhưng kho vẫn
    /// giữ hàng của lần chơi trước, mà còn key kho thì `StarterInventorySetup` bỏ qua
    /// bước cấp hạt khởi đầu ⇒ cấp 1 mà không có lúa để trồng.
    ///
    /// Giữ lại vì đôi khi vẫn muốn đúng thế: thử lại cân bằng tiền ở cấp thấp mà không
    /// mất công gây dựng lại kho.
    /// </summary>
    [MenuItem(BASE + "Xoá cấp + tiền (GIỮ kho, tutorial, nhiệm vụ)")]
    private static void ResetPlayerSave()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Xoá cấp độ và tiền",
            "Xoá 4 mục: Level, EXP, Gold, Gems.\n\n" +
            "GIỮ NGUYÊN: kho hạt, tutorial, nhiệm vụ, đơn hàng, quầy, chuồng, công trình.\n\n" +
            "→ Vào game sẽ là cấp 1 nhưng KHÔNG được cấp lại hạt khởi đầu (kho cũ vẫn còn).\n\n" +
            "Muốn chơi lại thật sự như người mới, dùng:\n" +
            "Tools ▸ Farm ▸ ⚠ CHƠI LẠI TỪ ĐẦU",
            "Xoá cấp + tiền", "Huỷ bỏ");

        if (!confirm) return;

        PlayerPrefs.DeleteKey("PLAYER_LEVEL");
        PlayerPrefs.DeleteKey("PLAYER_EXP");
        PlayerPrefs.DeleteKey("FARM_ECONOMY_GOLD");
        PlayerPrefs.DeleteKey("FARM_ECONOMY_GEMS");
        PlayerPrefs.Save();

        Debug.Log("[Phase1Test] Đã xoá cấp + tiền. Kho/tutorial/nhiệm vụ GIỮ NGUYÊN — " +
                  "muốn sạch hoàn toàn thì dùng Tools ▸ Farm ▸ ⚠ CHƠI LẠI TỪ ĐẦU.");

        if (Application.isPlaying)
            Debug.LogWarning("[Phase1Test] Đang Play Mode — manager còn sống sẽ ghi đè lại " +
                             "ngay lần Save kế tiếp. Phải Stop rồi chạy lại tool này.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int GetUnlockLevel(BaseItemData item)
    {
        if (item == null) return 1;
        var field = item.GetType().GetField("unlockLevel",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(int))
            return Mathf.Max(1, (int)field.GetValue(item));
        return 1;
    }

    // Validate — một số tool chỉ có nghĩa trong Play Mode
    [MenuItem(BASE + "Force Level 1 (chỉ đổi cấp, KHÔNG xoá save)", true)]
    [MenuItem(BASE + "Force Level 2", true)]
    [MenuItem(BASE + "Force Level 3", true)]
    [MenuItem(BASE + "Force Level 4", true)]
    [MenuItem(BASE + "Force Level 5 (Cooking Unlock)", true)]
    [MenuItem(BASE + "Force Level 6", true)]
    private static bool ValidatePlayOnly() => Application.isPlaying;
}
