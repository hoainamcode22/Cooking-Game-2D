#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;

public class RainScaleAdjuster : EditorWindow
{
    private float scaleFactor = 2f;

    [MenuItem("Tools/Farm/Adjust Rain VFX Scale")]
    public static void ShowWindow()
    {
        GetWindow<RainScaleAdjuster>("Adjust Rain Size");
    }

    private void OnGUI()
    {
        GUILayout.Label("Adjust Rain & Splash Size", EditorStyles.boldLabel);
        
        scaleFactor = EditorGUILayout.Slider("Scale Factor", scaleFactor, 1f, 5f);

        if (GUILayout.Button("Apply Scale to Weather Prefab"))
        {
            ApplyScaleToPrefab();
        }
    }

    private void ApplyScaleToPrefab()
    {
        string[] guids = AssetDatabase.FindAssets("DayNightWeatherSetup t:Prefab");
        if (guids.Length == 0)
        {
            Debug.LogError("Could not find DayNightWeatherSetup prefab!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab != null)
        {
            VisualEffect[] vfxs = prefab.GetComponentsInChildren<VisualEffect>(true);
            int count = 0;
            foreach (var vfx in vfxs)
            {
                if (vfx.gameObject.name.Contains("Rain"))
                {
                    vfx.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                    count++;
                }
            }
            
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log($"Successfully scaled {count} Rain VFX objects to {scaleFactor}x in the prefab.");
        }
    }
}
#endif
