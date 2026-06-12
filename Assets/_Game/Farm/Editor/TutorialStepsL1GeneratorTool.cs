using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup Tutorial Steps L1
///
/// Tạo thêm TutorialStepData assets cho Level 1 (sau 5 step hiện có).
/// Các step mới bao gồm: giao đơn hàng, nhận vàng/EXP, và kết thúc tutorial L1.
///
/// Assets được tạo trong: Assets/Resources/TutorialSteps/
/// TutorialManager sẽ đọc các bước này khi được kéo vào _steps list.
///
/// Lưu ý: Không xóa 5 step cũ (1_Welcome → 5_HarvestWheat).
///        Chỉ thêm step 6, 7, 8.
/// </summary>
public static class TutorialStepsL1GeneratorTool
{
    private const string MENU         = "Tools/Farm Game/Setup Tutorial Steps L1";
    private const string OUTPUT_FOLDER = "Assets/Resources/TutorialSteps";

    [System.Serializable]
    private struct StepSpec
    {
        public string fileName;
        public string npcText;
        public string targetID;
        public TutorialWaitAction waitAction;
        public bool showHandPointer;
        public Vector2 handOffset;
    }

    // Step 6-8: tiếp nối sau 5_HarvestWheat
    private static readonly StepSpec[] NewSteps = new StepSpec[]
    {
        new StepSpec
        {
            fileName       = "6_DeliverOrder",
            npcText        = "Tuyệt vời! Bạn đã có lúa rồi!\n" +
                             "Bây giờ hãy bấm vào căn nhà có bong bóng đơn hàng để giao hàng và nhận thưởng!",
            targetID       = "",   // User điền targetID của HouseOrderController sau
            waitAction     = TutorialWaitAction.WaitForDelivery,
            showHandPointer= false,
            handOffset     = new Vector2(40, -30),
        },
        new StepSpec
        {
            fileName       = "7_EarnedGold",
            npcText        = "Xuất sắc! Bạn vừa nhận được vàng và kinh nghiệm!\n" +
                             "Tiếp tục trồng trọt và giao đơn hàng để lên cấp nhé!",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            handOffset     = Vector2.zero,
        },
        new StepSpec
        {
            fileName       = "8_TutorialDone",
            npcText        = "Bạn đã hiểu cách chơi rồi đó!\n" +
                             "Hãy mở rộng nông trại, mua thêm hạt giống, và giao thật nhiều đơn hàng!\n" +
                             "Chúc bạn chơi vui! 🌾",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            handOffset     = Vector2.zero,
        },
    };

    [MenuItem(MENU)]
    public static void GenerateTutorialSteps()
    {
        // Đảm bảo thư mục tồn tại
        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/TutorialSteps"));
            AssetDatabase.Refresh();
        }

        int created = 0, skipped = 0;

        foreach (var spec in NewSteps)
        {
            string assetPath = $"{OUTPUT_FOLDER}/{spec.fileName}.asset";

            // Không ghi đè nếu đã tồn tại
            if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath)))
            {
                Debug.Log($"[TutorialGenerator] SKIP (đã tồn tại): {spec.fileName}");
                skipped++;
                continue;
            }

            var step = ScriptableObject.CreateInstance<TutorialStepData>();
            step.npcText         = spec.npcText;
            step.targetID        = spec.targetID;
            step.waitAction      = spec.waitAction;
            step.showHandPointer = spec.showHandPointer;
            step.handOffset      = spec.handOffset;
            step.typingSpeed     = 0.035f;

            AssetDatabase.CreateAsset(step, assetPath);
            Debug.Log($"[TutorialGenerator] ✓ Tạo: {assetPath}");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // In danh sách tất cả step hiện có để nhắc user gán vào TutorialManager
        PrintAllStepOrder();

        EditorUtility.DisplayDialog("Tutorial Steps Generator",
            $"✅ Hoàn thành!\n\n" +
            $"• {created} step mới được tạo\n" +
            $"• {skipped} step đã tồn tại (không bị ghi đè)\n\n" +
            "Tiếp theo:\n" +
            "1. Mở TutorialManager trong scene SCN_Farm\n" +
            "2. Kéo các step theo đúng thứ tự vào danh sách _steps:\n" +
            "   1_Welcome → 2_OpenShop → 3_BuyDirt → 4_PlantWheat\n" +
            "   → 5_HarvestWheat → 6_DeliverOrder → 7_EarnedGold → 8_TutorialDone\n\n" +
            "Xem Console để biết thứ tự đúng.",
            "OK");
    }

    private static void PrintAllStepOrder()
    {
        string[] guids = AssetDatabase.FindAssets("t:TutorialStepData", new[] { OUTPUT_FOLDER });

        Debug.Log("═══ TUTORIAL STEP ORDER (kéo theo thứ tự này vào TutorialManager._steps) ═══");
        Debug.Log("Index | Asset Name           | WaitAction          | TargetID");
        Debug.Log("─────────────────────────────────────────────────────────────────");

        int idx = 0;
        var sortedPaths = new System.Collections.Generic.List<string>();
        foreach (string g in guids)
            sortedPaths.Add(AssetDatabase.GUIDToAssetPath(g));
        sortedPaths.Sort(System.StringComparer.OrdinalIgnoreCase);

        foreach (string path in sortedPaths)
        {
            var s = AssetDatabase.LoadAssetAtPath<TutorialStepData>(path);
            if (s == null) continue;
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            Debug.Log($"  {idx,3}  | {name,-22} | {s.waitAction,-20} | '{s.targetID}'");
            idx++;
        }
        Debug.Log("═══════════════════════════════════════════════════════════════════");
    }

    [MenuItem(MENU, true)]
    private static bool Validate() => !EditorApplication.isPlaying;
}
