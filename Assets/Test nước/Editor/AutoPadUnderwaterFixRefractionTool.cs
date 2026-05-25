using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class AutoPadUnderwaterFixRefractionTool
{
    private const string MenuPath = "Tools/Map TopDown/Auto Pad Underwater & Fix Refraction";
    private const string WaterTilemapName = "Water_Tilemap";
    private const string UnderwaterTilemapName = "Underwater_Tilemap";
    private const string RendererDataListField = "m_RendererDataList";
    private const string DefaultRendererIndexField = "m_DefaultRendererIndex";
    private const string CameraRendererIndexField = "m_RendererIndex";
    private const string UseCameraSortingTextureField = "m_UseCameraSortingLayersTexture";
    private const string CameraSortingTextureBoundField = "m_CameraSortingLayersTextureBound";
    private const int UnderwaterSortingOrder = -200;

    private static readonly string[] BottomTileKeywords =
    {
        "stone",
        "dirt",
        "ground",
        "walkway",
        "soil",
        "elevation"
    };

    [MenuItem(MenuPath)]
    public static void AutoPadUnderwaterAndFixRefraction()
    {
        var configuredRendererDataCount = ForceConfigureRenderer2DData();
        if (configuredRendererDataCount == 0)
        {
            Debug.LogError("AutoPadUnderwaterFixRefractionTool: Could not find any active Renderer2DData to configure.");
            return;
        }

        var grid = FindMainGrid();
        if (grid == null)
        {
            Debug.LogError("AutoPadUnderwaterFixRefractionTool: No Grid found in the loaded scene.");
            return;
        }

        var underwaterTilemap = CreateOrResetUnderwaterTilemap(grid);
        var waterTilemap = FindNamedSceneTilemap(WaterTilemapName);
        if (waterTilemap == null)
        {
            Debug.LogError($"AutoPadUnderwaterFixRefractionTool: Could not find '{WaterTilemapName}'. Create/draw the water layer first.");
            Selection.activeGameObject = underwaterTilemap.gameObject;
            return;
        }

        var bottomTile = FindBottomTile();
        if (bottomTile == null)
        {
            Debug.LogError("AutoPadUnderwaterFixRefractionTool: Could not find a stone/dirt/ground/walkway TileBase asset for the lake bottom.");
            Selection.activeGameObject = underwaterTilemap.gameObject;
            return;
        }

        var filledTileCount = PadUnderwaterTiles(waterTilemap, underwaterTilemap, bottomTile);

        EditorUtility.SetDirty(underwaterTilemap);
        EditorUtility.SetDirty(underwaterTilemap.GetComponent<TilemapRenderer>());
        EditorSceneManager.MarkSceneDirty(underwaterTilemap.gameObject.scene);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();

        Selection.activeGameObject = underwaterTilemap.gameObject;
        EditorGUIUtility.PingObject(underwaterTilemap.gameObject);

        Debug.Log(
            "AutoPadUnderwaterFixRefractionTool: Refraction setup complete. " +
            $"Renderer2DDataConfigured={configuredRendererDataCount}, " +
            $"BottomTile='{bottomTile.name}', " +
            $"FilledUnderwaterCells={filledTileCount}.");
    }

    private static int ForceConfigureRenderer2DData()
    {
        var renderer2DDataAssets = GetRenderer2DDataInUse().Distinct().ToArray();
        var defaultLayerId = SortingLayer.NameToID("Default");
        var configuredCount = 0;

        foreach (var renderer2DData in renderer2DDataAssets)
        {
            if (renderer2DData == null)
            {
                continue;
            }

            Undo.RecordObject(renderer2DData, "Enable URP 2D camera sorting layer texture");

            var serializedRendererData = new SerializedObject(renderer2DData);
            serializedRendererData.Update();

            var useTextureProperty = serializedRendererData.FindProperty(UseCameraSortingTextureField);
            var boundProperty = serializedRendererData.FindProperty(CameraSortingTextureBoundField);

            if (useTextureProperty == null || useTextureProperty.propertyType != SerializedPropertyType.Boolean
                || boundProperty == null || boundProperty.propertyType != SerializedPropertyType.Integer)
            {
                Debug.LogWarning($"AutoPadUnderwaterFixRefractionTool: Renderer2DData '{renderer2DData.name}' does not expose expected camera sorting texture fields.");
                continue;
            }

            useTextureProperty.boolValue = true;
            boundProperty.intValue = defaultLayerId;
            serializedRendererData.ApplyModifiedProperties();

            EditorUtility.SetDirty(renderer2DData);
            configuredCount++;
        }

        return configuredCount;
    }

    private static IEnumerable<Renderer2DData> GetRenderer2DDataInUse()
    {
        var urpAsset = GetCurrentUrpAsset();
        if (urpAsset != null)
        {
            var rendererDataList = GetRendererDataList(urpAsset);
            var cameraRendererIndex = GetRendererIndexFromMainCamera();
            var defaultRendererIndex = GetDefaultRendererIndex(urpAsset);

            if (cameraRendererIndex >= 0
                && cameraRendererIndex < rendererDataList.Length
                && rendererDataList[cameraRendererIndex] is Renderer2DData cameraRenderer2DData)
            {
                yield return cameraRenderer2DData;
            }

            if (defaultRendererIndex >= 0
                && defaultRendererIndex < rendererDataList.Length
                && rendererDataList[defaultRendererIndex] is Renderer2DData defaultRenderer2DData)
            {
                yield return defaultRenderer2DData;
            }

            foreach (var renderer2DData in rendererDataList.OfType<Renderer2DData>())
            {
                yield return renderer2DData;
            }
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Renderer2DData", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var renderer2DData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(path);
            if (renderer2DData != null)
            {
                yield return renderer2DData;
            }
        }
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

    private static Grid FindMainGrid()
    {
        var grids = FindSceneObjects<Grid>();

        return grids.FirstOrDefault(grid => grid.name.Equals("Grid_Map_45", StringComparison.OrdinalIgnoreCase))
            ?? grids.FirstOrDefault(grid => grid.name.Equals("Grid", StringComparison.OrdinalIgnoreCase))
            ?? grids.FirstOrDefault(grid => grid.gameObject.activeInHierarchy)
            ?? grids.FirstOrDefault();
    }

    private static Tilemap CreateOrResetUnderwaterTilemap(Grid grid)
    {
        var underwaterObject = FindDirectChild(grid.transform, UnderwaterTilemapName);

        if (underwaterObject == null)
        {
            underwaterObject = new GameObject(UnderwaterTilemapName);
            Undo.RegisterCreatedObjectUndo(underwaterObject, "Create underwater tilemap");
            underwaterObject.transform.SetParent(grid.transform, false);
        }
        else
        {
            Undo.RecordObject(underwaterObject, "Reset underwater tilemap");
        }

        underwaterObject.layer = grid.gameObject.layer;
        underwaterObject.transform.localPosition = Vector3.zero;
        underwaterObject.transform.localRotation = Quaternion.identity;
        underwaterObject.transform.localScale = Vector3.one;

        var tilemap = underwaterObject.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            tilemap = Undo.AddComponent<Tilemap>(underwaterObject);
        }
        else
        {
            Undo.RecordObject(tilemap, "Clear underwater tilemap");
            tilemap.ClearAllTiles();
        }

        var renderer = underwaterObject.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(underwaterObject);
        }

        Undo.RecordObject(renderer, "Configure underwater renderer");
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = UnderwaterSortingOrder;
        renderer.sharedMaterial = GetSpritesDefaultMaterial();
        renderer.mode = TilemapRenderer.Mode.Chunk;

        EditorUtility.SetDirty(underwaterObject);
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);

        return tilemap;
    }

    private static GameObject FindDirectChild(Transform parent, string childName)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static Material GetSpritesDefaultMaterial()
    {
        var spritesDefault = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        if (spritesDefault != null)
        {
            return spritesDefault;
        }

        // Null makes Unity use the renderer's default sprite material and avoids accidentally assigning the water material.
        return null;
    }

    private static Tilemap FindNamedSceneTilemap(string tilemapObjectName)
    {
        return FindSceneObjects<Tilemap>()
            .Where(tilemap => tilemap.gameObject.name == tilemapObjectName)
            .OrderByDescending(tilemap => tilemap.gameObject.activeInHierarchy)
            .FirstOrDefault();
    }

    private static TileBase FindBottomTile()
    {
        foreach (var keyword in BottomTileKeywords)
        {
            var tile = AssetDatabase
                .FindAssets($"{keyword} t:TileBase", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<TileBase>)
                .Where(candidate => candidate != null)
                .OrderByDescending(candidate => candidate.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .FirstOrDefault(candidate => !candidate.name.Contains("Water", StringComparison.OrdinalIgnoreCase));

            if (tile != null)
            {
                return tile;
            }
        }

        return AssetDatabase
            .FindAssets("t:TileBase", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<TileBase>)
            .FirstOrDefault(candidate => candidate != null && !candidate.name.Contains("Water", StringComparison.OrdinalIgnoreCase));
    }

    private static int PadUnderwaterTiles(Tilemap waterTilemap, Tilemap underwaterTilemap, TileBase bottomTile)
    {
        Undo.RecordObject(underwaterTilemap, "Auto-pad underwater tiles");
        underwaterTilemap.ClearAllTiles();

        var filledCount = 0;
        var bounds = waterTilemap.cellBounds;

        foreach (var position in bounds.allPositionsWithin)
        {
            if (!waterTilemap.HasTile(position))
            {
                continue;
            }

            underwaterTilemap.SetTile(position, bottomTile);
            filledCount++;
        }

        return filledCount;
    }

    private static T[] FindSceneObjects<T>() where T : Object
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(obj => obj != null)
            .Where(obj => !EditorUtility.IsPersistent(obj))
            .ToArray();
    }
}
