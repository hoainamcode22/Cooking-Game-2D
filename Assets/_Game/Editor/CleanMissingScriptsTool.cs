#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool tự động phát hiện và xóa sạch tất cả Missing (MonoBehaviour) scripts
/// trong Prefabs và Scene, giúp Unity không bị lỗi 'Error while saving Prefab: You are trying to save a Prefab with a missing script'.
/// </summary>
// ⛔ [VÒNG 13 — 04/09/2026] ĐÃ TẮT TỰ CHẠY THEO LỆNH LEAD.
// Trước đây attribute [InitializeOnLoad] khiến static constructor chạy MỖI LẦN Unity biên dịch
// lại, kéo theo EditorApplication.delayCall → tool tự sửa scene rồi TỰ LƯU. Hậu quả: mọi thứ
// Sếp kéo tay trong scene (vị trí prefab tàu, nút HUD, reference nhân vật popup) đều bị ghi đè
// âm thầm sau mỗi lần compile — đây chính là nguyên nhân của chuỗi lỗi "tự nhiên hỏng".
// Menu trong Tools/... VẪN CÒN — muốn chạy thì bấm tay, chủ động và kiểm soát được.
// Muốn bật lại: bỏ dấu // ở dòng dưới.
// [InitializeOnLoad]
public static class CleanMissingScriptsTool
{
    static CleanMissingScriptsTool()
    {
        // ⛔ [VÒNG 14] ĐÃ TẮT — dòng dưới từng khiến tool tự chạy + tự lưu scene mỗi lần compile.
        // Comment [InitializeOnLoad] ở vòng 13 là CHƯA ĐỦ: chỉ cần code khác chạm vào bất kỳ
        // member nào của class là static constructor vẫn chạy, và dòng này vẫn đăng ký.
        // Muốn chạy: bấm menu trong Tools/... (chủ động, kiểm soát được).
        // EditorApplication.delayCall += CleanAllTrainPrefabs;
    }

    [MenuItem("Tools/Farm Game/Clean Missing Scripts In Prefabs & Scene", false, 50)]
    public static void CleanAllTrainPrefabs()
    {
        int totalRemoved = 0;

        // 1. Dọn dẹp trong các Prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Export_Train_UI_Package", "Assets/_Game" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot != null)
            {
                int count = CleanRecursive(prefabRoot);
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    totalRemoved += count;
                    Debug.Log($"[CleanMissingScripts] Đã xóa {count} missing scripts trong Prefab: {path}");
                }
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        // 2. Dọn dẹp trong Scene hiện tại
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            int count = CleanRecursive(root);
            if (count > 0)
            {
                totalRemoved += count;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
                Debug.Log($"[CleanMissingScripts] Đã xóa {count} missing scripts trong Scene object: {root.name}");
            }
        }

        if (totalRemoved > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CleanMissingScripts] ĐÃ DỌN DẸP HOÀN TẤT: Tổng cộng {totalRemoved} Missing Scripts đã được loại bỏ!");
        }
    }

    private static int CleanRecursive(GameObject go)
    {
        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
        {
            count += CleanRecursive(child.gameObject);
        }
        return count;
    }
}
#endif
