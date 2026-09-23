using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace KitchenCozyV3.Editor
{
    [InitializeOnLoad]
    public static class CozyKitchenV3AutoRunner
    {
        static CozyKitchenV3AutoRunner()
        {
            EditorApplication.delayCall += ExecuteOnce;
        }

        private static void ExecuteOnce()
        {
            // Đảm bảo chỉ chạy 1 lần
            string markerFile = "Temp/CozyKitchenV3AutoRunnerDone.txt";
            if (File.Exists(markerFile)) return;

            try
            {
                Debug.Log("<color=cyan>[CozyKitchen] Đang tự động chạy sửa bố cục số lượng và thanh Order Tomato Pasta...</color>");

                // 1. Sửa bố cục số lượng
                CozyKitchenV3LayoutSuite.FixQuantityLayoutInPrefabAndScene(true);

                // 2. Thiết kế thanh Order mẫu Tomato Pasta
                CozyKitchenV3LayoutSuite.SetupTomatoPastaOrderBanner(true);

                // 3. Gắn 5 icon gia vị vào Flavor
                CozyFlavorIconsSetupTool.ApplyFlavorIconsToScene(true);

                Debug.Log("<color=green>[CozyKitchen] ✔ HOÀN TẤT TỰ ĐỘNG CẢ 3 PHẦN!</color>");

                File.WriteAllText(markerFile, "done");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CozyKitchen] Lỗi khi tự động chạy: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
