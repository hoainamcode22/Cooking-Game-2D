using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Đảm bảo toàn bộ Light2D (đặc biệt là Global Light 2D và Day/Night lights)
/// luôn chiếu sáng MỌI Sorting Layer (kể cả CongTrinh, Water, Objects, Foreground...)
/// để tránh hiện tượng công trình và sprite dùng Sprite-Lit-Default bị đen thui.
/// </summary>
public static class Light2DSortingLayerFixer
{
    private static readonly FieldInfo FieldApplyToSortingLayers =
        typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitRuntime()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FixAllLightsInActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FixAllLightsInActiveScene();
    }

    /// <summary>Quét và sửa toàn bộ Light2D trong scene hiện tại.</summary>
    public static void FixAllLightsInActiveScene()
    {
        var layers = SortingLayer.layers;
        if (layers == null || layers.Length == 0) return;

        int[] layerIds = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            layerIds[i] = layers[i].id;

        var allLights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool hasGlobalLight = false;

        foreach (var light in allLights)
        {
            if (light == null) continue;
            if (light.lightType == Light2D.LightType.Global && light.gameObject.activeInHierarchy && light.enabled)
                hasGlobalLight = true;

            ApplySortingLayersRuntime(light, layerIds);
        }

        // Nếu scene hoàn toàn không có Global Light 2D nào hoạt động, tạo 1 Global Light dự phòng
        if (!hasGlobalLight && allLights.Length == 0)
        {
            var fallbackGo = new GameObject("Fallback_Global_Light_2D");
            var globalLight = fallbackGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = Color.white;
            globalLight.intensity = 1f;
            ApplySortingLayersRuntime(globalLight, layerIds);
            Debug.Log("[Light2DSortingLayerFixer] Scene chưa có Global Light 2D — đã tự tạo Fallback_Global_Light_2D.");
        }
    }

    private static void ApplySortingLayersRuntime(Light2D light, int[] layerIds)
    {
        if (light == null || layerIds == null) return;
        try
        {
            if (FieldApplyToSortingLayers != null)
            {
                FieldApplyToSortingLayers.SetValue(light, layerIds);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Light2DSortingLayerFixer] Không thể gán sorting layers cho '{light.name}': {ex.Message}");
        }
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void InitEditor()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                FixAllLightsInEditor(false);
            }
        };
    }

    [MenuItem("Tools/Farm Game/Setup/Fix 2D Lighting Layers (CongTrinh, Water)")]
    public static void FixAllLightsMenu()
    {
        FixAllLightsInEditor(true);
    }

    public static void FixAllLightsInEditor(bool showDialog)
    {
        var layers = SortingLayer.layers;
        if (layers == null || layers.Length == 0) return;

        int fixedCount = 0;

        // 1. Quét trong scene đang mở
        var sceneLights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var light in sceneLights)
        {
            if (light == null) continue;
            if (FixLightSerializedObject(light, layers))
            {
                fixedCount++;
                EditorUtility.SetDirty(light);
                EditorUtility.SetDirty(light.gameObject);
            }
        }

        if (sceneLights.Length > 0 && !EditorApplication.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // 2. Quét các Prefab ngày đêm trong project
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab DayNight");
        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var pLights = prefab.GetComponentsInChildren<Light2D>(true);
            bool modified = false;
            foreach (var l in pLights)
            {
                if (FixLightSerializedObject(l, layers))
                {
                    modified = true;
                    fixedCount++;
                }
            }

            if (modified)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[Light2DSortingLayerFixer] Đã cập nhật Light2D trong Prefab '{path}'");
            }
        }

        Debug.Log($"[Light2DSortingLayerFixer] Hoàn tất đồng bộ sorting layers cho {fixedCount} Light2D!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Fix 2D Lighting Layers",
                $"Đã đồng bộ toàn bộ {layers.Length} Sorting Layer (bao gồm CongTrinh, Water, Objects...)\n" +
                $"cho {fixedCount} Light2D trong Scene và Prefab.\n\n" +
                "Các công trình dùng Sprite-Lit-Default sẽ sáng rõ ràng!", "OK");
        }
    }

    private static bool FixLightSerializedObject(Light2D light, SortingLayer[] layers)
    {
        try
        {
            var so = new SerializedObject(light);
            var pLayers = so.FindProperty("m_ApplyToSortingLayers");
            if (pLayers != null && pLayers.isArray)
            {
                bool needsUpdate = pLayers.arraySize != layers.Length;
                if (!needsUpdate)
                {
                    for (int i = 0; i < layers.Length; i++)
                    {
                        if (pLayers.GetArrayElementAtIndex(i).intValue != layers[i].id)
                        {
                            needsUpdate = true;
                            break;
                        }
                    }
                }

                if (needsUpdate)
                {
                    pLayers.arraySize = layers.Length;
                    for (int i = 0; i < layers.Length; i++)
                    {
                        pLayers.GetArrayElementAtIndex(i).intValue = layers[i].id;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Light2DSortingLayerFixer] Lỗi cập nhật SerializedObject '{light.name}': {ex.Message}");
        }
        return false;
    }
#endif
}
