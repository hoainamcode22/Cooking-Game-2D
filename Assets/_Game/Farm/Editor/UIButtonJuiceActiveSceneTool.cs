#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class UIButtonJuiceActiveSceneTool : EditorWindow
{
    [MenuItem("Tools/Farm/Setup UI Juice (All Popups & Bars)")]
    public static void ShowWindow()
    {
        GetWindow<UIButtonJuiceActiveSceneTool>("UI Juice Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto-inject Juice Scripts", EditorStyles.boldLabel);

        if (GUILayout.Button("Inject Button Juice to Active Scene"))
        {
            InjectButtonJuiceToActiveScene();
        }
    }

    private void InjectButtonJuiceToActiveScene()
    {
        UnityEngine.UI.Button[] allButtons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (var btn in allButtons)
        {
            if (btn.GetComponent<UIJuiceFeedback>() == null)
            {
                btn.gameObject.AddComponent<UIJuiceFeedback>();
                EditorUtility.SetDirty(btn.gameObject);
                count++;
            }
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            
        Debug.Log($"Hoan tat! Da them UIJuiceFeedback vao {count} Buttons trong Active Scene.");
    }
}
#endif
