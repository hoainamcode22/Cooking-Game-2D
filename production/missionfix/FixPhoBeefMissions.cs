#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Missions/Fix Pho Beef Missions (Dry-Run | APPLY)
///
/// VÌ SAO CÓ TOOL NÀY: 12 mission proc_c_* trong Main_L1_L10 đang trỏ vào dish id
/// "pho_beef" — id này KHÔNG TỒN TẠI (dish thật là "pho_bo_tai", xem
/// Farm_Cooking/Dish_pho_bo_tai.asset). MissionProgressTracker đếm tiến độ theo key
/// "CookDish:pho_beef" nên không sự kiện nấu ăn nào khớp → 12 mission "chết",
/// người chơi không bao giờ hoàn thành được.
///
/// Bug bắt nguồn từ mảng dishes trong MissionSetupTool.SetupMissions() (sinh mission
/// tự động) có phần tử "pho_beef". File MissionSetupTool.PATCHED.cs sửa tận gốc để
/// chạy lại Setup Missions không tái sinh bug; tool này sửa 12 ASSET đang nằm trên đĩa.
///
/// CÁCH DÙNG:
///   1. Đặt file này vào Assets/_Game/Farm/Editor/ (bắt buộc folder Editor).
///   2. Chạy menu "Tools/Farm Game/Missions/Fix Pho Beef Missions (Dry-Run)"
///      — chỉ IN log từng thay đổi dự kiến, KHÔNG ghi gì vào asset.
///   3. Xem log Console ưng ý rồi chạy ".../Fix Pho Beef Missions (APPLY)".
///
/// AN TOÀN:
///   - Idempotent: asset nào targetItemId đã KHÔNG còn là "pho_beef" thì skip + log,
///     chạy APPLY lần 2 vô hại.
///   - Chỉ đổi missionName / targetItemId / targetAmount theo bảng đã duyệt 2026-08-19;
///     requiredLevel, reward, eventType, icon... giữ nguyên.
///   - Có Undo.RecordObject → Ctrl+Z hoàn tác được ngay trong Editor.
/// </summary>
public static class FixPhoBeefMissions
{
    private const string MENU_DRYRUN = "Tools/Farm Game/Missions/Fix Pho Beef Missions (Dry-Run)";
    private const string MENU_APPLY  = "Tools/Farm Game/Missions/Fix Pho Beef Missions (APPLY)";

    private const string MainFolder     = "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10";
    private const string BrokenTargetId = "pho_beef";

    private struct Fix
    {
        public string newTarget;
        public int    newAmount;
        public string newName;

        public Fix(string newTarget, int newAmount, string newName)
        {
            this.newTarget = newTarget; this.newAmount = newAmount; this.newName = newName;
        }
    }

    // Thứ tự xử lý/log ổn định (Dictionary không đảm bảo thứ tự duyệt).
    private static readonly string[] FIX_ORDER =
    {
        "proc_c_4_1",  "proc_c_6_3",  "proc_c_8_5",
        "proc_c_15_2", "proc_c_17_4", "proc_c_19_6", "proc_c_21_8", "proc_c_23_10",
        "proc_c_24_1", "proc_c_26_3", "proc_c_28_5", "proc_c_30_7",
    };

    // ─── Bảng sửa ĐÃ DUYỆT 2026-08-19: missionId → (targetItemId mới, targetAmount, tên mới) ───
    //   3 mission cấp thấp (L4/L6/L8) đổi sang món cấp thấp cùng độ khó;
    //   9 mission còn lại giữ ý đồ thiết kế "phở bò" → dish id thật "pho_bo_tai".
    private static readonly Dictionary<string, Fix> FIXES = new Dictionary<string, Fix>
    {
        { "proc_c_4_1",   new Fix("com_chien_trung",  3, "Nấu 3 món Cơm chiên trứng") },
        { "proc_c_6_3",   new Fix("bap_cai_xao_nam",  4, "Nấu 4 món Bắp cải xào nấm") },
        { "proc_c_8_5",   new Fix("nam_xao_thit_bo",  5, "Nấu 5 món Nấm xào thịt bò") },
        { "proc_c_15_2",  new Fix("pho_bo_tai",       8, "Nấu 8 món Phở bò tái") },
        { "proc_c_17_4",  new Fix("pho_bo_tai",       9, "Nấu 9 món Phở bò tái") },
        { "proc_c_19_6",  new Fix("pho_bo_tai",      10, "Nấu 10 món Phở bò tái") },
        { "proc_c_21_8",  new Fix("pho_bo_tai",      11, "Nấu 11 món Phở bò tái") },
        { "proc_c_23_10", new Fix("pho_bo_tai",      12, "Nấu 12 món Phở bò tái") },
        { "proc_c_24_1",  new Fix("pho_bo_tai",      13, "Nấu 13 món Phở bò tái") },
        { "proc_c_26_3",  new Fix("pho_bo_tai",      14, "Nấu 14 món Phở bò tái") },
        { "proc_c_28_5",  new Fix("pho_bo_tai",      15, "Nấu 15 món Phở bò tái") },
        { "proc_c_30_7",  new Fix("pho_bo_tai",      16, "Nấu 16 món Phở bò tái") },
    };

    [MenuItem(MENU_DRYRUN)]
    public static void DryRun() => Run(apply: false);

    [MenuItem(MENU_APPLY)]
    public static void Apply() => Run(apply: true);

    private static void Run(bool apply)
    {
        string tag = apply ? "[Apply]" : "[DryRun]";
        int fixedCount = 0, skipped = 0, missing = 0;

        foreach (string missionId in FIX_ORDER)
        {
            Fix    fix   = FIXES[missionId];
            string path  = $"{MainFolder}/Mission_{missionId}.asset";
            var    asset = AssetDatabase.LoadAssetAtPath<MissionData>(path);

            if (asset == null)
            {
                Debug.LogWarning($"{tag} MISSING — không tìm thấy asset: {path} (bỏ qua, không throw).");
                missing++;
                continue;
            }

            // Idempotent: chỉ sửa asset đang thật sự trỏ vào pho_beef.
            if (asset.targetItemId != BrokenTargetId)
            {
                Debug.Log($"{tag} SKIP — {missionId}: targetItemId hiện là '{asset.targetItemId}' " +
                          $"(không phải '{BrokenTargetId}') — đã sửa trước đó hoặc designer đã đổi tay.");
                skipped++;
                continue;
            }

            if (!apply)
            {
                Debug.Log($"[DryRun] {missionId}: '{BrokenTargetId}' → '{fix.newTarget}' | " +
                          $"{asset.missionName} → {fix.newName}");
                fixedCount++;
                continue;
            }

            Undo.RecordObject(asset, "Fix pho_beef mission");
            asset.targetItemId = fix.newTarget;
            asset.missionName  = fix.newName;
            asset.targetAmount = fix.newAmount; // theo bảng duyệt (trùng giá trị hiện tại — ghi lại cho chắc)
            EditorUtility.SetDirty(asset);
            fixedCount++;

            Debug.Log($"[Apply] {missionId}: '{BrokenTargetId}' → '{fix.newTarget}' | " +
                      $"targetAmount={fix.newAmount} | tên mới: {fix.newName}");
        }

        if (apply)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[Apply] Tổng kết: đã sửa {fixedCount}, skip {skipped}, missing {missing} " +
                      $"(trên {FIX_ORDER.Length} mission trong bảng duyệt).");
            if (fixedCount == FIX_ORDER.Length)
                Debug.Log("✅ FIX HOÀN TẤT");
        }
        else
        {
            Debug.Log($"[DryRun] Tổng kết: sẽ-sửa {fixedCount}, bỏ-qua {skipped}, missing {missing} " +
                      $"(trên {FIX_ORDER.Length} mission trong bảng duyệt). KHÔNG có gì được ghi.");
        }
    }
}
#endif
