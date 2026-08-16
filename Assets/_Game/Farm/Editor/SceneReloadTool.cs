using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneReloadTool
{
    [MenuItem("Tools/Farm/Tải Lại Scene Gốc (Discard & Reload SCN_Farm)", false, 0)]
    public static void ReloadScene()
    {
        string scenePath = "Assets/_Game/Scenes/SCN_Farm.unity";
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log("[SceneReload] ✅ Đã tải lại toàn bộ scene SCN_Farm từ file gốc!");
        }
    }
}
