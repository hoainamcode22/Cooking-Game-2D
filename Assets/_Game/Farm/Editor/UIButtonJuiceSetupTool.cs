using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Farm.UI;

namespace Farm.EditorScripts
{
    public class UIButtonJuiceSetupTool : EditorWindow
    {
        [MenuItem("Tools/Farm/Setup UI Button Juice")]
        public static void ShowWindow()
        {
            GetWindow<UIButtonJuiceSetupTool>("Button Juice Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("UI Button Auto-Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup All Buttons in Project (Prefabs & Current Scene)"))
            {
                SetupAll();
            }
        }

        private void SetupAll()
        {
            int modifiedPrefabs = SetupPrefabs();
            int modifiedSceneRoots = SetupCurrentScene();

            Debug.Log($"[UIButtonJuiceSetupTool] Setup complete! Modified {modifiedPrefabs} prefabs and {modifiedSceneRoots} root objects in the scene.");
        }

        private int SetupPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int modifiedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Load prefab contents safely
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    GameObject prefabRoot = editingScope.prefabContentsRoot;
                    if (prefabRoot == null) continue;

                    if (ProcessGameObject(prefabRoot))
                    {
                        modifiedCount++;
                    }
                }
            }

            return modifiedCount;
        }

        private int SetupCurrentScene()
        {
            int modifiedCount = 0;
            Scene currentScene = EditorSceneManager.GetActiveScene();
            GameObject[] rootObjects = currentScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                if (ProcessGameObject(root))
                {
                    modifiedCount++;
                }
            }

            if (modifiedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(currentScene);
            }

            return modifiedCount;
        }

        private bool ProcessGameObject(GameObject root)
        {
            bool modified = false;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);

            foreach (Button btn in buttons)
            {
                bool buttonModified = false;

                // Add UIJuiceFeedback
                Component juice = btn.GetComponent("UIJuiceFeedback");
                if (juice == null)
                {
                    System.Type juiceType = GetTypeByName("UIJuiceFeedback");
                    if (juiceType != null)
                    {
                        btn.gameObject.AddComponent(juiceType);
                        buttonModified = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[UIButtonJuiceSetupTool] UIJuiceFeedback type not found. Skipping adding to {btn.name}.");
                    }
                }

                // Check for Reward keywords
                string nameLower = btn.gameObject.name.ToLower();
                if (nameLower.Contains("claim") || nameLower.Contains("reward") || 
                    nameLower.Contains("receive") || nameLower.Contains("gift"))
                {
                    // Add UIRewardButtonFX
                    UIRewardButtonFX fx = btn.GetComponent<UIRewardButtonFX>();
                    if (fx == null)
                    {
                        btn.gameObject.AddComponent<UIRewardButtonFX>();
                        buttonModified = true;
                    }
                }

                if (buttonModified)
                {
                    modified = true;
                }
            }

            return modified;
        }

        private System.Type GetTypeByName(string className)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == className)
                    {
                        return type;
                    }
                }
            }
            return null;
        }
    }
}
