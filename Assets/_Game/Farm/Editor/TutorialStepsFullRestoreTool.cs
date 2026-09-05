#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// KHÔI PHỤC ĐẦY ĐỦ DANH SÁCH BƯỚC TUTORIAL cho <c>TutorialManager._steps</c> trong SCN_Farm.unity.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// KHÁC VỚI <see cref="TutorialStepsRestoreTool"/> (tool cũ):
///   • Tool cũ chỉ APPEND 10 bước L2 vào cuối, không đụng 21 phần tử đang có.
///   • Tool này GHI LẠI TOÀN BỘ <c>_steps</c> theo THỨ TỰ CHUẨN trong mảng <see cref="THU_TU"/>
///     ⇒ sửa được cả trường hợp sai thứ tự / trùng / có ô NULL.
///
/// ⚠ CON SỐ 31 (đã chốt 2026-09-05):
///   Thư mục có 32 asset, nhưng mảng THU_TU chỉ liệt 31 — CỐ Ý bỏ <c>L1L2_04b_FirstHarvest</c>
///   vì nội dung bước đó là "Chạm vào ô lúa chín để thu hoạch" (WaitForHarvest) trong khi vị trí
///   của nó nằm NGAY SAU 04_FocusPlots, tức TRƯỚC khi người chơi gieo hạt ⇒ không ô nào chín
///   ⇒ kẹt cứng. Bản git c05e3ebb (chạy được tới phần chăn nuôi) cũng đúng 31 bước như vậy.
///   Muốn dùng lại 04b: sửa nội dung asset cho hợp ngữ cảnh rồi chèn tên nó SAU "L1L2_08b_GuideHarvest".
///
/// AN TOÀN:
///   • DRY RUN in bảng đầy đủ, KHÔNG ghi gì.
///   • APPLY: SerializedObject + Undo (Ctrl+Z), gom 1 nhóm Undo duy nhất, KHÔNG tự lưu scene.
///   • Asset thiếu file → log 🔴 và BỎ QUA (không nhét ô NULL vào list).
///   • Asset có trong thư mục L1_L2 mà không có tên trong THU_TU → xếp CUỐI + cảnh báo.
///   • Không tìm thấy TutorialManager → log lỗi rõ ràng, KHÔNG throw.
///   • KHÔNG [InitializeOnLoad], KHÔNG delayCall → chỉ chạy khi bấm menu.
/// </summary>
public static class TutorialStepsFullRestoreTool
{
    private const string MENU_DRY   = "Tools/Farm/Tutorial/Khoi phuc DU 31 buoc - DRY RUN";
    private const string MENU_APPLY = "Tools/Farm/Tutorial/Khoi phuc DU 31 buoc - APPLY";

    /// <summary>Thư mục gốc để quét asset (theo yêu cầu) — trong đó chỉ nhánh L1_L2 mới thuộc phạm vi tool.</summary>
    private const string THU_MUC_QUET = "Assets/Resources/TutorialSteps";
    private const string THU_MUC_L1L2 = "Assets/Resources/TutorialSteps/L1_L2/";

    /// <summary>Field serialize trên TutorialManager.cs:82 — <c>[SerializeField] private List&lt;TutorialStepData&gt; _steps</c>.</summary>
    private const string F_STEPS = "_steps";

    /// <summary>THỨ TỰ CHUẨN của kịch bản tutorial (tên asset, không có đuôi .asset).</summary>
    private static readonly string[] THU_TU =
    {
        // ── Level 1: lúa ──
        "L1L2_01_Welcome",
        "L1L2_02_ReadyQuestion",
        "L1L2_04_FocusPlots",
        // ⛔ L1L2_04b_FirstHarvest CỐ Ý BỊ LOẠI (quyết định Lead 2026-09-05):
        //    nội dung bước là "Chạm vào ô lúa chín để thu hoạch nào!" (waitAction = WaitForHarvest)
        //    nhưng vị trí của nó lại nằm NGAY SAU 04_FocusPlots — tức TRƯỚC khi người chơi gieo hạt.
        //    Lúc đó không ô nào chín ⇒ cổng không bao giờ đạt ⇒ tutorial kẹt cứng tại bước 5/31.
        //    Bản git c05e3ebb (chạy được tới chăn nuôi) cũng không có bước này.
        //    Muốn dùng lại: đổi nội dung asset cho hợp ngữ cảnh rồi chèn SAU 08b_GuideHarvest.
        "L1L2_05_DragFirstRice",
        "L1L2_06_PlantAllRice",
        "L1L2_06b_GuideSpeedUp",
        "L1L2_07_OpenCropProgress",
        "L1L2_08_SpeedUpTip",
        "L1L2_08b_GuideHarvest",
        "L1L2_09_HarvestFirstRice",
        "L1L2_09b_HarvestResult",
        "L1L2_10_HarvestAllRice",
        // ── Level 1: hoa ──
        "L1L2_11_TransitionFlower",
        "L1L2_12_FocusFlowerPots",
        "L1L2_13_DragFirstFlower",
        "L1L2_14_PlantAllFlowers",
        "L1L2_15_FlowerSpeedUp",
        "L1L2_16_HarvestFirstFlower",
        "L1L2_17_HarvestAllFlowers",
        "L1L2_18_LevelUpCelebration",
        // ── Level 2: shop + ngô + chuồng ──
        "L2_01_GotoShop",
        "L2_02_UnlockCorn",
        "L2_03_BuyCorn",
        "L2_04_CloseShop",
        "L2_05_PlantCorn",
        "L2_06_AnimalIntro",
        "L2_07_FocusPen",
        "L2_08_FeedPen",
        "L2_09_PenSpeedUp",
        "L2_10_HarvestPen",
    };

    [MenuItem(MENU_DRY, false, 330)]
    public static void DryRun() => ChayTool(ghiThat: false);

    [MenuItem(MENU_APPLY, false, 331)]
    public static void Apply()
    {
        if (!EditorUtility.DisplayDialog(
                "Khôi phục danh sách bước Tutorial (APPLY)",
                "Tool sẽ GHI ĐÈ TOÀN BỘ TutorialManager._steps theo thứ tự chuẩn trong mảng THU_TU.\n\n" +
                "Nên bấm DRY RUN xem bảng trước.\n" +
                "Có Undo (Ctrl+Z). Tool KHÔNG tự lưu scene — Sếp bấm Ctrl+S.",
                "Ghi vào scene", "Thôi"))
            return;

        ChayTool(ghiThat: true);
    }

    // ═══════════════════════════════════════════════════════════════════════════

    private static void ChayTool(bool ghiThat)
    {
        // ── 1. TutorialManager ──────────────────────────────────────────────
        var mgr = UnityEngine.Object.FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
        if (mgr == null)
        {
            Debug.LogError("[StepsFullRestore] Không tìm thấy TutorialManager trong scene đang mở (đã tìm cả object inactive).\n" +
                           "Hãy mở Assets/_Game/Scenes/SCN_Farm.unity rồi chạy lại. (Tool không làm gì cả.)");
            return;
        }

        var so = new SerializedObject(mgr);
        SerializedProperty list = so.FindProperty(F_STEPS);
        if (list == null || !list.isArray)
        {
            Debug.LogError($"[StepsFullRestore] Không đọc được field '{F_STEPS}' trên TutorialManager " +
                           "(tên field đã đổi? xem TutorialManager.cs:82). Tool dừng, không ghi gì.");
            return;
        }

        // ── 2. Danh sách _steps hiện tại ────────────────────────────────────
        var dangCo   = new HashSet<UnityEngine.Object>();
        var tenDangCo = new List<string>();
        int soONull  = 0;
        for (int i = 0; i < list.arraySize; i++)
        {
            UnityEngine.Object obj = list.GetArrayElementAtIndex(i).objectReferenceValue;
            if (obj == null) { soONull++; tenDangCo.Add("<NULL>"); continue; }
            dangCo.Add(obj);
            tenDangCo.Add(obj.name);
        }

        // ── 3. Nạp toàn bộ asset TutorialStepData ───────────────────────────
        var theoTen  = new Dictionary<string, TutorialStepData>();
        var trungTen = new List<string>();
        var ngoaiPhamVi = new List<string>();

        string[] guids = AssetDatabase.FindAssets("t:TutorialStepData", new[] { THU_MUC_QUET });
        foreach (string guid in guids)
        {
            string duongDan = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<TutorialStepData>(duongDan);
            if (asset == null) continue;

            if (!duongDan.StartsWith(THU_MUC_L1L2))
            {
                ngoaiPhamVi.Add(duongDan);
                continue;
            }
            if (theoTen.ContainsKey(asset.name))
            {
                trungTen.Add($"{asset.name}  ({duongDan})");
                continue;
            }
            theoTen[asset.name] = asset;
        }

        // ── 4. Xếp theo THỨ TỰ CHUẨN ────────────────────────────────────────
        var thuTuCuoi = new List<TutorialStepData>();
        var thieuFile = new List<string>();
        var daDung    = new HashSet<string>();

        foreach (string ten in THU_TU)
        {
            if (!theoTen.TryGetValue(ten, out TutorialStepData asset))
            {
                thieuFile.Add(ten);
                continue;
            }
            thuTuCuoi.Add(asset);
            daDung.Add(ten);
        }

        // Asset lạ trong L1_L2 (không có tên trong THU_TU) → xếp cuối + cảnh báo.
        var lam = new List<TutorialStepData>();
        foreach (KeyValuePair<string, TutorialStepData> kv in theoTen)
        {
            if (daDung.Contains(kv.Key)) continue;
            lam.Add(kv.Value);
        }
        lam.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        thuTuCuoi.AddRange(lam);

        // ── 5. Báo cáo ──────────────────────────────────────────────────────
        var sb = new StringBuilder();
        sb.AppendLine("═══════ KHÔI PHỤC DANH SÁCH BƯỚC TUTORIAL (_steps) ═══════");
        sb.AppendLine($"Chế độ            : {(ghiThat ? "APPLY (ghi vào scene)" : "DRY RUN (không ghi gì)")}");
        sb.AppendLine($"Scene             : {mgr.gameObject.scene.name}");
        sb.AppendLine($"Thư mục quét      : {THU_MUC_QUET}  →  {theoTen.Count} asset trong {THU_MUC_L1L2}");
        sb.AppendLine($"_steps hiện tại   : {list.arraySize} bước" + (soONull > 0 ? $"   🔴 ({soONull} ô NULL)" : ""));
        sb.AppendLine($"Danh sách chuẩn   : {THU_TU.Length} tên trong mảng THU_TU");
        sb.AppendLine("──────────────────────────────────────────────────────────");
        sb.AppendLine(" [i] tên bước                     · waitAction                     · targetID          · có trong _steps?");
        sb.AppendLine("──────────────────────────────────────────────────────────");

        int soThem = 0;
        var tenSeThem = new List<string>();
        for (int i = 0; i < thuTuCuoi.Count; i++)
        {
            TutorialStepData a = thuTuCuoi[i];
            bool daNam = dangCo.Contains(a);
            if (!daNam) { soThem++; tenSeThem.Add(a.name); }

            string tid = string.IsNullOrEmpty(a.targetID) ? "-" : a.targetID;
            sb.AppendLine($" [{i,2}] {a.name,-30} · {a.waitAction,-30} · {tid,-18} · {(daNam ? "✓" : "✗ THÊM MỚI")}");
        }

        sb.AppendLine("──────────────────────────────────────────────────────────");
        if (thieuFile.Count > 0)
        {
            sb.AppendLine($"🔴 THIẾU FILE ASSET ({thieuFile.Count}) — có tên trong THU_TU nhưng không có file, ĐÃ BỎ QUA:");
            foreach (string t in thieuFile) sb.AppendLine($"     🔴 {THU_MUC_L1L2}{t}.asset");
        }
        if (lam.Count > 0)
        {
            sb.AppendLine($"⚠ ASSET LẠ ({lam.Count}) — có file trong L1_L2 nhưng KHÔNG có tên trong THU_TU, đã XẾP CUỐI:");
            foreach (TutorialStepData a in lam) sb.AppendLine($"     ⚠ {a.name}");
        }
        if (trungTen.Count > 0)
        {
            sb.AppendLine($"⚠ TRÙNG TÊN ({trungTen.Count}) — chỉ dùng file gặp đầu tiên:");
            foreach (string t in trungTen) sb.AppendLine($"     ⚠ {t}");
        }
        if (ngoaiPhamVi.Count > 0)
        {
            sb.AppendLine($"· Ngoài thư mục L1_L2 ({ngoaiPhamVi.Count}) — tool KHÔNG đụng tới:");
            foreach (string p in ngoaiPhamVi) sb.AppendLine($"     · {p}");
        }

        // Bước hiện có trong _steps nhưng sẽ KHÔNG còn sau khi ghi đè.
        var seMat = new List<string>();
        var setMoi = new HashSet<UnityEngine.Object>(thuTuCuoi);
        for (int i = 0; i < list.arraySize; i++)
        {
            UnityEngine.Object obj = list.GetArrayElementAtIndex(i).objectReferenceValue;
            if (obj == null) continue;
            if (!setMoi.Contains(obj)) seMat.Add(obj.name);
        }
        if (seMat.Count > 0)
        {
            sb.AppendLine($"⚠ SẼ BỊ LOẠI KHỎI _steps ({seMat.Count}) — đang có trong scene nhưng không nằm trong danh sách chuẩn:");
            foreach (string t in seMat) sb.AppendLine($"     ⚠ {t}");
        }

        sb.AppendLine("──────────────────────────────────────────────────────────");
        sb.AppendLine($"TỔNG KẾT: scene đang có {list.arraySize} bước; sau khi APPLY sẽ có {thuTuCuoi.Count} bước.");
        sb.AppendLine(soThem > 0
            ? $"Các bước sẽ được THÊM ({soThem}): {string.Join(", ", tenSeThem)}"
            : "Các bước sẽ được THÊM: (không có — chỉ sắp xếp lại thứ tự)");

        if (thuTuCuoi.Count == 0)
        {
            sb.AppendLine("🔴 KHÔNG có asset nào để ghi → tool DỪNG, không đụng vào _steps.");
            sb.AppendLine("══════════════════════════════════════════════════════════");
            Debug.LogError(sb.ToString());
            return;
        }

        if (!ghiThat)
        {
            sb.AppendLine("DRY RUN: chưa ghi bất cứ thứ gì. Chạy menu '... - APPLY' để ghi vào scene.");
            sb.AppendLine("══════════════════════════════════════════════════════════");
            Debug.Log(sb.ToString());
            return;
        }

        // ── 6. Ghi thật ─────────────────────────────────────────────────────
        int nhom = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Khoi phuc danh sach buoc Tutorial");
        Undo.RecordObject(mgr, "Khoi phuc danh sach buoc Tutorial");

        list.arraySize = thuTuCuoi.Count;
        for (int i = 0; i < thuTuCuoi.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = thuTuCuoi[i];

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);
        EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);
        Undo.CollapseUndoOperations(nhom);

        sb.AppendLine($"ĐÃ GHI: _steps bây giờ có {list.arraySize} bước.");
        sb.AppendLine("THỨ TỰ CUỐI CÙNG:");
        for (int i = 0; i < list.arraySize; i++)
        {
            UnityEngine.Object obj = list.GetArrayElementAtIndex(i).objectReferenceValue;
            sb.AppendLine($"   [{i,2}] {(obj != null ? obj.name : "<NULL>")}");
        }
        sb.AppendLine("──────────────────────────────────────────────────────────");
        sb.AppendLine(">>> Nhớ Ctrl+S để lưu scene. Sai thì Ctrl+Z (1 lần là hoàn tác cả nhóm).");
        sb.AppendLine("══════════════════════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }
}
#endif
