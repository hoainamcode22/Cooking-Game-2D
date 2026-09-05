// =============================================================================
//  TutorialStepsRestoreTool
//  ---------------------------------------------------------------------------
//  MUC DICH: Khoi phuc 10 buoc Tutorial Level-2 (L2_01..L2_10) da bi roi khoi
//  danh sach _steps cua TutorialManager trong SCN_Farm.unity.
//
//  BANG CHUNG (khong doan mo):
//    - Ban git c05e3ebb (01.09.2026)  : _steps co 31 phan tu (21 L1L2 + 10 L2)
//    - Ban dang lam viec              : _steps chi con 21 phan tu
//    => 10 asset L2_01..L2_10 bi mat khoi list, KHONG phai bi xoa file.
//    (L1L2_04b_FirstHarvest CHUA BAO GIO nam trong list o bat ky commit nao
//     -> tool nay KHONG dong vao no.)
//
//  AN TOAN:
//    - Chi APPEND asset con thieu vao CUOI list, dung thu tu goc.
//    - KHONG xoa, KHONG sap xep lai 21 phan tu dang co.
//    - Dung SerializedObject + Undo.RecordObject (Ctrl+Z hoan tac duoc).
//    - KHONG tu luu scene: nguoi dung tu bam Ctrl+S sau khi doc log.
//    - KHONG [InitializeOnLoad], KHONG delayCall -> chi chay khi bam menu.
// =============================================================================

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TutorialStepsRestoreTool
{
    private const string THU_MUC = "Assets/Resources/TutorialSteps/L1_L2/";

    // Thu tu goc lay tu ban git c05e3ebb, index 21..30
    private static readonly string[] BUOC_L2 =
    {
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

    [MenuItem("Tools/Farm/Tutorial/Khoi phuc 10 buoc L2 - DRY RUN (chi bao cao)", false, 320)]
    public static void DryRun() { ChayTool(false); }

    [MenuItem("Tools/Farm/Tutorial/Khoi phuc 10 buoc L2 - APPLY (ghi vao scene)", false, 321)]
    public static void Apply() { ChayTool(true); }

    private static void ChayTool(bool ghiThat)
    {
        var mgr = Object.FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
        if (mgr == null)
        {
            Debug.LogError("[StepsRestore] Khong tim thay TutorialManager trong scene dang mo. " +
                           "Hay mo Assets/_Game/Scenes/SCN_Farm.unity roi chay lai.");
            return;
        }

        var so   = new SerializedObject(mgr);
        var list = so.FindProperty("_steps");
        if (list == null || !list.isArray)
        {
            Debug.LogError("[StepsRestore] Khong doc duoc field '_steps' tren TutorialManager.");
            return;
        }

        // --- 1. Doc danh sach hien tai -----------------------------------------
        var dangCo    = new HashSet<Object>();
        var tenDangCo = new List<string>();
        int soONull   = 0;
        for (int i = 0; i < list.arraySize; i++)
        {
            var obj = list.GetArrayElementAtIndex(i).objectReferenceValue;
            if (obj == null) { soONull++; tenDangCo.Add("<NULL>"); continue; }
            dangCo.Add(obj);
            tenDangCo.Add(obj.name);
        }

        // --- 2. Xac dinh buoc con thieu ----------------------------------------
        var canThem  = new List<Object>();
        var khongCo  = new List<string>();
        var daCoRoi  = new List<string>();

        foreach (var ten in BUOC_L2)
        {
            string duongDan = THU_MUC + ten + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<TutorialStepData>(duongDan);
            if (asset == null) { khongCo.Add(duongDan); continue; }
            if (dangCo.Contains(asset)) { daCoRoi.Add(ten); continue; }
            canThem.Add(asset);
        }

        // --- 3. Bao cao ---------------------------------------------------------
        var sb = new StringBuilder();
        sb.AppendLine("================ TUTORIAL STEPS RESTORE ================");
        sb.AppendLine($"Che do          : {(ghiThat ? "APPLY (ghi vao scene)" : "DRY RUN (khong ghi gi)")}");
        sb.AppendLine($"_steps hien tai : {list.arraySize} phan tu" + (soONull > 0 ? $"  (CANH BAO: {soONull} o NULL)" : ""));
        sb.AppendLine($"Chuan mong doi  : 31 phan tu (21 L1L2 + 10 L2)");
        sb.AppendLine("--------------------------------------------------------");
        sb.AppendLine($"Da co san       : {daCoRoi.Count}  {(daCoRoi.Count > 0 ? string.Join(", ", daCoRoi) : "-")}");
        sb.AppendLine($"Se them moi     : {canThem.Count}");
        foreach (var a in canThem) sb.AppendLine($"    + {a.name}");
        if (khongCo.Count > 0)
        {
            sb.AppendLine($"THIEU FILE ASSET: {khongCo.Count}  <-- KHONG the khoi phuc du");
            foreach (var p in khongCo) sb.AppendLine($"    ! {p}");
        }
        sb.AppendLine("--------------------------------------------------------");

        if (canThem.Count == 0)
        {
            sb.AppendLine("KET LUAN: khong co gi de them. _steps da du hoac asset bi thieu file.");
            sb.AppendLine("========================================================");
            Debug.Log(sb.ToString());
            return;
        }

        if (!ghiThat)
        {
            sb.AppendLine($"KET LUAN: sau khi APPLY, _steps se co {list.arraySize + canThem.Count} phan tu.");
            sb.AppendLine("Chay lai bang menu ... - APPLY de ghi that, roi Ctrl+S de luu scene.");
            sb.AppendLine("========================================================");
            Debug.Log(sb.ToString());
            return;
        }

        // --- 4. Ghi that --------------------------------------------------------
        Undo.RecordObject(mgr, "Khoi phuc 10 buoc Tutorial L2");

        foreach (var a in canThem)
        {
            int idx = list.arraySize;
            list.InsertArrayElementAtIndex(idx);
            list.GetArrayElementAtIndex(idx).objectReferenceValue = a;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mgr);
        EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);

        sb.AppendLine($"DA GHI: _steps bay gio co {list.arraySize} phan tu.");
        sb.AppendLine("THU TU CUOI CUNG:");
        for (int i = 0; i < list.arraySize; i++)
        {
            var obj = list.GetArrayElementAtIndex(i).objectReferenceValue;
            sb.AppendLine($"   [{i,2}] {(obj != null ? obj.name : "<NULL>")}");
        }
        sb.AppendLine("--------------------------------------------------------");
        sb.AppendLine(">>> BAY GIO BAM Ctrl+S DE LUU SCENE. Neu sai: Ctrl+Z hoac");
        sb.AppendLine(">>> khoi phuc production/backup_round16_2026-09-04/SCN_Farm.unity.bak");
        sb.AppendLine("========================================================");
        Debug.Log(sb.ToString());
    }
}
