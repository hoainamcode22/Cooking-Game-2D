#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool 1-click dựng cụm Tutorial L2 (Shop → mua Ngô → trồng), nối tiếp sau L1 (lúa+hoa).
/// Menu: Tools/Farm Game/Setup Tutorial L2 (Shop + Corn)
///
/// Việc nó làm:
///   1. Tạo 5 step asset L2_01..L2_05 trong Resources/TutorialSteps/L1_L2.
///   2. APPEND 5 step đó vào cuối TutorialManager._steps (không xoá step L1).
///   3. Gắn TutorialTarget cho Btn_Home / Btn_Store / Btn_Close (kể cả khi đang inactive).
///
/// Logic hành vi của từng step nằm trong TutorialManager (nhánh L2_01/03/04/05) + resolver
/// (đăng ký item Ngô runtime). Chạy tool này rồi bấm Play để test.
/// </summary>
public static class SetupTutorialL2Tool
{
    private const string MENU   = "Tools/Farm Game/Setup Tutorial L2 (Shop + Corn)";
    private const string FOLDER = "Assets/Resources/TutorialSteps/L1_L2";

    private struct Spec
    {
        public string file;
        public string npc;
        public TutorialWaitAction wait;
    }

    private static readonly Spec[] Steps =
    {
        new Spec { file = "L2_01_GotoShop",   npc = "",                                          wait = TutorialWaitAction.WaitForOpenShop },
        new Spec { file = "L2_02_UnlockCorn", npc = "Tuyệt vời! Bạn vừa mở khoá hạt Ngô. Mở Shop mua ít hạt nhé!", wait = TutorialWaitAction.WaitForClick },
        new Spec { file = "L2_03_BuyCorn",    npc = "",                                          wait = TutorialWaitAction.WaitForBuyItem },
        new Spec { file = "L2_04_CloseShop",  npc = "",                                          wait = TutorialWaitAction.WaitForCloseShop },
        new Spec { file = "L2_05_PlantCorn",  npc = "",                                          wait = TutorialWaitAction.WaitForAllPlotsPlanted },

        // ── B8–B13: chăn nuôi (chuồng gà Pen_03) ──
        new Spec { file = "L2_06_AnimalIntro",npc = "Bạn đã làm tốt lắm! Trồng trọt xong rồi — giờ mình tập chăn nuôi gia súc nhé!", wait = TutorialWaitAction.WaitForClick },
        new Spec { file = "L2_07_FocusPen",   npc = "",                                          wait = TutorialWaitAction.WaitForOpenPen },
        new Spec { file = "L2_08_FeedPen",    npc = "",                                          wait = TutorialWaitAction.WaitForFeed },
        new Spec { file = "L2_09_PenSpeedUp", npc = "",                                          wait = TutorialWaitAction.WaitForPenSpeedUp },
        new Spec { file = "L2_10_HarvestPen", npc = "",                                          wait = TutorialWaitAction.WaitForPenHarvest },
    };

    [MenuItem(MENU)]
    public static void Setup()
    {
        EnsureFolders();
        int created = CreateAssets();
        int added   = AppendToManager();
        TagButtons();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Setup Tutorial L2",
            $"Xong!\n• Tạo/cập nhật {created} step L2\n• Thêm {added} step vào _steps\n• Gắn target Btn_Home/Btn_Store/Btn_Close\n\n" +
            "Kiểm tra Console nếu có cảnh báo. Bấm Play để test cụm Shop→Ngô.",
            "OK");
    }

    [MenuItem(MENU, true)]
    private static bool Validate() => !EditorApplication.isPlaying;

    // ── Tạo step asset ─────────────────────────────────────────────────────────
    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/TutorialSteps"))
            AssetDatabase.CreateFolder("Assets/Resources", "TutorialSteps");
        if (!AssetDatabase.IsValidFolder(FOLDER))
            AssetDatabase.CreateFolder("Assets/Resources/TutorialSteps", "L1_L2");
    }

    private static int CreateAssets()
    {
        int count = 0;
        foreach (var s in Steps)
        {
            string path = $"{FOLDER}/{s.file}.asset";
            var step = AssetDatabase.LoadAssetAtPath<TutorialStepData>(path);
            bool isNew = step == null;
            if (isNew) step = ScriptableObject.CreateInstance<TutorialStepData>();

            step.npcText         = s.npc;
            step.waitAction      = s.wait;
            step.showHandPointer = false;  // nhánh code trong TutorialManager tự điều khiển tay
            step.showGuideBoard  = false;
            step.targetID        = "";
            step.dragToTargetId  = "";
            step.typingSpeed     = 0.02f;

            if (isNew) AssetDatabase.CreateAsset(step, path);
            else       EditorUtility.SetDirty(step);
            count++;
        }
        return count;
    }

    // ── Append vào TutorialManager._steps (không xoá L1) ─────────────────────────
    private static int AppendToManager()
    {
        var mgr = Object.FindFirstObjectByType<TutorialManager>();
        if (mgr == null) { Debug.LogError("[L2Setup] Không tìm thấy TutorialManager trong scene."); return 0; }

        var so   = new SerializedObject(mgr);
        var prop = so.FindProperty("_steps");
        if (prop == null) { Debug.LogError("[L2Setup] Không tìm thấy field '_steps'."); return 0; }

        int added = 0;
        foreach (var s in Steps)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TutorialStepData>($"{FOLDER}/{s.file}.asset");
            if (asset == null) { Debug.LogError($"[L2Setup] Thiếu asset {s.file}"); continue; }

            bool exists = false;
            for (int i = 0; i < prop.arraySize; i++)
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == asset) { exists = true; break; }
            if (exists) continue;

            int idx = prop.arraySize;
            prop.InsertArrayElementAtIndex(idx);
            prop.GetArrayElementAtIndex(idx).objectReferenceValue = asset;
            added++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);
        Debug.Log($"[L2Setup] Append {added} step L2 vào _steps (tổng {prop.arraySize}).");
        return added;
    }

    // ── Gắn TutorialTarget cho các nút (kể cả inactive) ──────────────────────────
    private static void TagButtons()
    {
        TagByName("Btn_Home",  "btn_home");
        TagByName("Btn_Store", "btn_store");
        TagByName("Btn_Close", "btn_close");
    }

    private static void TagByName(string goName, string id)
    {
        var matches = FindSceneObjectsByName(goName);
        if (matches.Count == 0)
        {
            Debug.LogWarning($"[L2Setup] KHÔNG thấy '{goName}' trong scene — gắn TutorialTarget id='{id}' thủ công nhé.");
            return;
        }
        if (matches.Count > 1)
            Debug.LogWarning($"[L2Setup] Có {matches.Count} object tên '{goName}'. Đã gắn vào cái đầu tiên: " +
                             $"'{Path(matches[0].transform)}'. Nếu sai, gắn TutorialTarget id='{id}' thủ công.");

        var go = matches[0];
        var tt = go.GetComponent<TutorialTarget>();
        if (tt == null) tt = go.AddComponent<TutorialTarget>();
        tt.targetID = id;
        EditorUtility.SetDirty(go);
        Debug.Log($"[L2Setup] Tagged '{Path(go.transform)}' → '{id}'.");
    }

    private static System.Collections.Generic.List<GameObject> FindSceneObjectsByName(string n)
    {
        var result = new System.Collections.Generic.List<GameObject>();
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.name != n) continue;
            if (!rt.gameObject.scene.IsValid()) continue;        // bỏ prefab asset
            if (rt.hideFlags != HideFlags.None) continue;        // bỏ object ẩn của editor
            result.Add(rt.gameObject);
        }
        return result;
    }

    private static string Path(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
#endif
