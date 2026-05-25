using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class FixWaterURPSettings
{
    private const string WaterObjectName = "Water_Surface_System";
    private const string UseCameraSortingTextureField = "m_UseCameraSortingLayersTexture";
    private const string CameraSortingTextureBoundField = "m_CameraSortingLayersTextureBound";
    private const string RendererDataListField = "m_RendererDataList";
    private const string DefaultRendererIndexField = "m_DefaultRendererIndex";
    private const string CameraRendererIndexField = "m_RendererIndex";

    [MenuItem("Tools/Map TopDown/Fix URP 2D Water Settings")]
    public static void FixUrp2DWaterSettings()
    {
        var urpAsset = GetCurrentUrpAsset();
        if (urpAsset == null)
        {
            Debug.LogError("FixWaterURPSettings: No active Universal Render Pipeline Asset was found.");
            return;
        }

        var renderer2DData = GetRenderer2DDataInUse(urpAsset);
        if (renderer2DData == null)
        {
            Debug.LogError($"FixWaterURPSettings: URP asset '{urpAsset.name}' is not using a Renderer2DData.");
            return;
        }

        var waterObject = FindSceneGameObject(WaterObjectName);
        var foremostLayerId = PickGroundSortingLayerId(waterObject);

        ConfigureRenderer2DData(renderer2DData, foremostLayerId);
        var configuredWaterRenderers = ConfigureWaterSurface(waterObject, foremostLayerId);
        EnableAnimatedMaterialsInSceneView();

        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();

        Debug.Log(
            "FixWaterURPSettings: URP 2D water settings fixed. " +
            $"Renderer2DData='{renderer2DData.name}', " +
            $"CameraSortingLayerTexture=On, " +
            $"ForemostSortingLayer='{SortingLayer.IDToName(foremostLayerId)}', " +
            $"WaterRenderersConfigured={configuredWaterRenderers}.");
    }

    [MenuItem("Tools/Map TopDown/Fix Water Shader (URP)")]
    public static void FixWaterShaderURP()
    {
        FixUrp2DWaterSettings();
    }

    private static UniversalRenderPipelineAsset GetCurrentUrpAsset()
    {
        RenderPipelineAsset pipelineAsset = GraphicsSettings.currentRenderPipeline;

        if (pipelineAsset == null)
        {
            pipelineAsset = QualitySettings.renderPipeline;
        }

        if (pipelineAsset == null)
        {
            pipelineAsset = GraphicsSettings.defaultRenderPipeline;
        }

        return pipelineAsset as UniversalRenderPipelineAsset;
    }

    private static Renderer2DData GetRenderer2DDataInUse(UniversalRenderPipelineAsset urpAsset)
    {
        var rendererDataList = GetRendererDataList(urpAsset);
        if (rendererDataList.Length == 0)
        {
            return null;
        }

        var index = GetRendererIndexFromMainCamera();
        if (index < 0)
        {
            index = GetDefaultRendererIndex(urpAsset);
        }

        if (index >= 0 && index < rendererDataList.Length && rendererDataList[index] is Renderer2DData cameraRenderer2DData)
        {
            return cameraRenderer2DData;
        }

        var defaultIndex = GetDefaultRendererIndex(urpAsset);
        if (defaultIndex >= 0 && defaultIndex < rendererDataList.Length && rendererDataList[defaultIndex] is Renderer2DData defaultRenderer2DData)
        {
            return defaultRenderer2DData;
        }

        var firstRenderer2DData = rendererDataList.OfType<Renderer2DData>().FirstOrDefault();
        if (firstRenderer2DData != null)
        {
            Debug.LogWarning(
                "FixWaterURPSettings: The camera/default renderer is not 2D, so the first Renderer2DData in the URP asset was used.");
        }

        return firstRenderer2DData;
    }

    private static ScriptableRendererData[] GetRendererDataList(UniversalRenderPipelineAsset urpAsset)
    {
        var serializedAsset = new SerializedObject(urpAsset);
        var rendererDataListProperty = serializedAsset.FindProperty(RendererDataListField);

        if (rendererDataListProperty == null || !rendererDataListProperty.isArray)
        {
            return Array.Empty<ScriptableRendererData>();
        }

        var rendererDataList = new ScriptableRendererData[rendererDataListProperty.arraySize];

        for (var i = 0; i < rendererDataListProperty.arraySize; i++)
        {
            rendererDataList[i] = rendererDataListProperty
                .GetArrayElementAtIndex(i)
                .objectReferenceValue as ScriptableRendererData;
        }

        return rendererDataList;
    }

    private static int GetDefaultRendererIndex(UniversalRenderPipelineAsset urpAsset)
    {
        var serializedAsset = new SerializedObject(urpAsset);
        var defaultIndexProperty = serializedAsset.FindProperty(DefaultRendererIndexField);
        return defaultIndexProperty != null ? defaultIndexProperty.intValue : 0;
    }

    private static int GetRendererIndexFromMainCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return -1;
        }

        var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            return -1;
        }

        var serializedCameraData = new SerializedObject(cameraData);
        var rendererIndexProperty = serializedCameraData.FindProperty(CameraRendererIndexField);
        return rendererIndexProperty != null ? rendererIndexProperty.intValue : -1;
    }

    private static int PickGroundSortingLayerId(GameObject waterObject)
    {
        var tilemapRenderers = FindSceneObjects<TilemapRenderer>()
            .Where(renderer => renderer != null)
            .Where(renderer => waterObject == null || !renderer.transform.IsChildOf(waterObject.transform))
            .ToArray();

        var defaultLayerId = SortingLayer.NameToID("Default");
        if (defaultLayerId != 0 || SortingLayer.IDToName(0) == "Default")
        {
            if (tilemapRenderers.Any(renderer => renderer.sortingLayerID == defaultLayerId))
            {
                return defaultLayerId;
            }
        }

        var lowestTilemapRenderer = tilemapRenderers
            .OrderBy(renderer => SortingLayer.GetLayerValueFromID(renderer.sortingLayerID))
            .ThenBy(renderer => renderer.sortingOrder)
            .FirstOrDefault();

        if (lowestTilemapRenderer != null)
        {
            return lowestTilemapRenderer.sortingLayerID;
        }

        if (defaultLayerId != 0 || SortingLayer.IDToName(0) == "Default")
        {
            return defaultLayerId;
        }

        var sortingLayers = SortingLayer.layers;
        return sortingLayers.Length > 0 ? sortingLayers[0].id : 0;
    }

    private static void ConfigureRenderer2DData(Renderer2DData renderer2DData, int foremostLayerId)
    {
        Undo.RecordObject(renderer2DData, "Fix URP 2D water renderer settings");

        var serializedRendererData = new SerializedObject(renderer2DData);
        serializedRendererData.Update();

        var useTextureProperty = serializedRendererData.FindProperty(UseCameraSortingTextureField);
        if (useTextureProperty == null || useTextureProperty.propertyType != SerializedPropertyType.Boolean)
        {
            Debug.LogError($"FixWaterURPSettings: Could not find '{UseCameraSortingTextureField}' on Renderer2DData.");
            return;
        }

        var boundProperty = serializedRendererData.FindProperty(CameraSortingTextureBoundField);
        if (boundProperty == null || boundProperty.propertyType != SerializedPropertyType.Integer)
        {
            Debug.LogError($"FixWaterURPSettings: Could not find '{CameraSortingTextureBoundField}' on Renderer2DData.");
            return;
        }

        useTextureProperty.boolValue = true;
        boundProperty.intValue = foremostLayerId;
        serializedRendererData.ApplyModifiedProperties();

        EditorUtility.SetDirty(renderer2DData);
    }

    private static int ConfigureWaterSurface(GameObject waterObject, int foremostLayerId)
    {
        if (waterObject == null)
        {
            Debug.LogWarning($"FixWaterURPSettings: No GameObject named '{WaterObjectName}' was found in the loaded scene.");
            return 0;
        }

        var renderers = waterObject.GetComponentsInChildren<UnityEngine.Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"FixWaterURPSettings: '{WaterObjectName}' has no SpriteRenderer, TilemapRenderer, or other Renderer component.");
            return 0;
        }

        var waterLayerId = GetSortingLayerAbove(foremostLayerId, out var hasHigherLayer);
        var targetOrder = hasHigherLayer
            ? 0
            : GetHighestSortingOrderOnLayer(foremostLayerId, renderers) + 1;

        foreach (var renderer in renderers)
        {
            Undo.RecordObject(renderer, "Fix water surface sorting");
            renderer.sortingLayerID = waterLayerId;
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, targetOrder);
            EditorUtility.SetDirty(renderer);
        }

        Undo.RecordObject(waterObject, "Fix water surface sorting");
        EditorUtility.SetDirty(waterObject);
        EditorSceneManager.MarkSceneDirty(waterObject.scene);

        return renderers.Length;
    }

    private static int GetSortingLayerAbove(int sortingLayerId, out bool hasHigherLayer)
    {
        var sortingLayers = SortingLayer.layers
            .OrderBy(layer => SortingLayer.GetLayerValueFromID(layer.id))
            .ToArray();

        var currentLayerIndex = Array.FindIndex(sortingLayers, layer => layer.id == sortingLayerId);
        if (currentLayerIndex >= 0 && currentLayerIndex + 1 < sortingLayers.Length)
        {
            hasHigherLayer = true;
            return sortingLayers[currentLayerIndex + 1].id;
        }

        hasHigherLayer = false;
        return sortingLayerId;
    }

    private static int GetHighestSortingOrderOnLayer(int sortingLayerId, UnityEngine.Renderer[] ignoredRenderers)
    {
        var ignored = ignoredRenderers.ToHashSet();

        return FindSceneObjects<UnityEngine.Renderer>()
            .Where(renderer => renderer != null)
            .Where(renderer => !ignored.Contains(renderer))
            .Where(renderer => renderer.sortingLayerID == sortingLayerId)
            .Select(renderer => renderer.sortingOrder)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static void EnableAnimatedMaterialsInSceneView()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            SetSceneViewAlwaysRefresh(SceneView.lastActiveSceneView);
        }

        foreach (SceneView sceneView in SceneView.sceneViews.OfType<SceneView>())
        {
            SetSceneViewAlwaysRefresh(sceneView);
        }
    }

    private static void SetSceneViewAlwaysRefresh(SceneView sceneView)
    {
        if (sceneView == null)
        {
            return;
        }

        Undo.RecordObject(sceneView, "Enable Scene View animated materials");

        sceneView.sceneViewState.alwaysRefresh = true;

        TrySetBoolMember(sceneView, "autoRepaintOnSceneChange", true);
        TrySetSceneViewStateBool(sceneView, "animatedMaterials", true);
        TrySetSceneViewStateBool(sceneView, "showAnimatedMaterials", true);

        sceneView.Repaint();
    }

    private static bool TrySetSceneViewStateBool(SceneView sceneView, string memberName, bool value)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var sceneViewStateProperty = typeof(SceneView).GetProperty("sceneViewState", Flags);
        if (sceneViewStateProperty == null)
        {
            return false;
        }

        var state = sceneViewStateProperty.GetValue(sceneView);
        if (state == null)
        {
            return false;
        }

        var changed = TrySetBoolMember(state, memberName, value);
        if (changed && sceneViewStateProperty.CanWrite)
        {
            sceneViewStateProperty.SetValue(sceneView, state);
        }

        return changed;
    }

    private static bool TrySetBoolMember(object target, string memberName, bool value)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var targetType = target.GetType();
        var property = targetType.GetProperty(memberName, Flags);
        if (property != null && property.PropertyType == typeof(bool) && property.CanWrite)
        {
            var currentValue = (bool)property.GetValue(target);
            if (currentValue != value)
            {
                property.SetValue(target, value);
                return true;
            }

            return false;
        }

        var field = targetType.GetField(memberName, Flags);
        if (field != null && field.FieldType == typeof(bool))
        {
            var currentValue = (bool)field.GetValue(target);
            if (currentValue != value)
            {
                field.SetValue(target, value);
                return true;
            }
        }

        return false;
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        return FindSceneObjects<GameObject>()
            .Where(gameObject => gameObject.name == objectName)
            .OrderByDescending(gameObject => gameObject.activeInHierarchy)
            .FirstOrDefault();
    }

    private static T[] FindSceneObjects<T>() where T : Object
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(obj => obj != null)
            .Where(obj => !EditorUtility.IsPersistent(obj))
            .ToArray();
    }
}
