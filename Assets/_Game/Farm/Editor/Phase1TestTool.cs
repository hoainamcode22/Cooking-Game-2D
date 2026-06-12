using UnityEngine;
using UnityEditor;
using Village;

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

    [MenuItem(BASE + "Force Level 1 (Reset)")]
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

    [MenuItem(BASE + "Print Village Orders Status")]
    private static void PrintVillageOrders()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[Phase1Test] Cần Play Mode để in trạng thái Village Orders.");
            return;
        }

        var vom = VillageOrderManager.Instance;
        if (vom == null) { Debug.LogWarning("[Phase1Test] VillageOrderManager.Instance = null!"); return; }

        Debug.Log("[Phase1Test] Inventory print skipped — DebugPrintInventoryNow is private in VillageOrderManager. Use [ContextMenu] on the component directly in Play Mode.");
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
                              (lockedCount == 0 ? " — chạy Tools/Farm Game/Setup Village Orders L1-L6" : ""));
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

        // 5. OrderItemDefinition unlock levels
        string[] orderGuids = AssetDatabase.FindAssets("t:OrderItemDefinition",
            new[] { "Assets/_Game/Farm/data/Village_data" });
        int lockedOrders = 0;
        foreach (string g in orderGuids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<OrderItemDefinition>(AssetDatabase.GUIDToAssetPath(g));
            if (asset != null && asset.unlockLevel >= 5 && ContainsCookingKeyword(asset.name.ToLower()))
                lockedOrders++;
        }
        report.AppendLine(lockedOrders > 0
            ? $"  ✅ Cooking orders locked at L5+: {lockedOrders} assets"
            : $"  ⚠  Cooking orders may not be level-locked — chạy Tools/Farm Game/Setup Village Orders L1-L6");

        // 6. HouseOrderBubbleAnimator
        var animators = Object.FindObjectsByType<Village.HouseOrderBubbleAnimator>(FindObjectsSortMode.None);
        report.AppendLine(animators.Length > 0
            ? $"  ✅ HouseOrderBubbleAnimator: {animators.Length} instance(s)"
            : "  ⚠  HouseOrderBubbleAnimator: 0 — thêm component vào HouseOrderBubble GameObjects");

        report.AppendLine("═══════════════════════════════════════");
        Debug.Log(report.ToString());

        EditorUtility.DisplayDialog("Phase 1 Setup Check",
            "Kết quả đã được in trong Console.\n\nMở Console (Window > General > Console) để xem chi tiết.",
            "OK");
    }

    // ── Reset Player Save (cẩn thận!) ────────────────────────────────────────

    [MenuItem(BASE + "⚠ Reset Player Save (PlayerPrefs)")]
    private static void ResetPlayerSave()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "⚠ Reset Player Save",
            "Thao tác này sẽ xóa toàn bộ dữ liệu lưu của người chơi (PlayerPrefs)!\n\n" +
            "Bao gồm: Level, EXP, Gold, Gems.\n\n" +
            "Chỉ dùng để test lại từ đầu. Không thể hoàn tác!",
            "XÓA DỮ LIỆU", "Huỷ bỏ");

        if (!confirm) return;

        PlayerPrefs.DeleteKey("PLAYER_LEVEL");
        PlayerPrefs.DeleteKey("PLAYER_EXP");
        PlayerPrefs.DeleteKey("FARM_ECONOMY_GOLD");
        PlayerPrefs.DeleteKey("FARM_ECONOMY_GEMS");
        PlayerPrefs.Save();

        Debug.Log("[Phase1Test] ✅ PlayerPrefs đã được xóa. Lần chơi tiếp sẽ bắt đầu từ Level 1.");

        if (Application.isPlaying)
            Debug.LogWarning("[Phase1Test] Đang Play Mode — cần Stop và Play lại để reset có hiệu lực.");
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

    private static bool ContainsCookingKeyword(string name) =>
        name.Contains("_xao_") || name.Contains("_nuong_") || name.Contains("_chien") ||
        name.Contains("_ham_") || name.Contains("pho_") || name.Contains("trung_op_") ||
        name.Contains("com_chien") || name.Contains("suon_") || name.Contains("salad_") ||
        name.Contains("order_item_");

    // Validate — một số tool chỉ có nghĩa trong Play Mode
    [MenuItem(BASE + "Force Level 1 (Reset)", true)]
    [MenuItem(BASE + "Force Level 2", true)]
    [MenuItem(BASE + "Force Level 3", true)]
    [MenuItem(BASE + "Force Level 4", true)]
    [MenuItem(BASE + "Force Level 5 (Cooking Unlock)", true)]
    [MenuItem(BASE + "Force Level 6", true)]
    private static bool ValidatePlayOnly() => Application.isPlaying;
}
