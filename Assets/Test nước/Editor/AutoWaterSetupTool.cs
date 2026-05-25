using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class AutoWaterSetupTool
{
    private const string SourceRoot = "Assets/HappyHarvest";
    private const string DestinationRoot = "Assets/Test nước";
    private const string ImportRoot = DestinationRoot + "/ImportedWater";
    private const string OldWaterObjectName = "Water_Surface_System";
    private const string WaterTilemapName = "Water_Tilemap";
    private const string WaterMaterialName = "Material_Water";

    private const int SourceWaterSortingOrder = -100;
    private static readonly Color SourceWaterTilemapColor = new Color(0.51029426f, 0.76887006f, 0.9716981f, 1f);

    private static readonly string[] ExplicitSourceAssets =
    {
        // Source scene layer: Grid/Water
        SourceRoot + "/Art/Tiles/Water/Tile_Water.asset",
        SourceRoot + "/Art/Tiles/Water/Sprite_WaterIcon.png",
        SourceRoot + "/Materials/Material_Water.mat",
        SourceRoot + "/ShaderGraphs/ShaderGraph_Water.shadergraph",
        SourceRoot + "/VFX/Water/Texture_04.png",
        SourceRoot + "/VFX/Water/WaterLines.png",
        SourceRoot + "/VFX/Water/WaterLines2.png",

        // Source scene layer: Grid/UnderwaterTiles. These are the cliff/underwater edge tiles used around ponds.
        SourceRoot + "/Art/Tiles/Elevation/Sprites/Sprite_Tiles_Elevation.png",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/Sprite_Tiles_elevation_2.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/Sprite_Tiles_elevation_3.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/tiles_elevation_15.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/tiles_elevation_16.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/tiles_elevation_17.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/tiles_elevation_18.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/tiles_elevation_19.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/tiles_elevation_21.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/tiles_elevation_31.asset",
        SourceRoot + "/Art/Tiles/Elevation/Tiles/tiles_elevation_32.asset",

        // Source scene layer: Grid/WateredTileLayer. Copied for completeness, not used by the new water layer.
        SourceRoot + "/Art/Tiles/SoilWatered/Tiles/RuleTIle_Soil_Watered.asset",
        SourceRoot + "/Art/Tiles/SoilWatered/Sprite/Sprite_Tiles_Soil_Watered.png",
        SourceRoot + "/Art/Tiles/SoilWatered/Sprite/Sprite_Tiles_Soil_Watered_mask.png",
        SourceRoot + "/Art/Tiles/SoilWatered/Sprite/Sprite_Tiles_Soil_Watered_normal.png",
    };

    private static readonly HashSet<string> CopyableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".asset",
        ".mat",
        ".shadergraph",
        ".shadersubgraph",
        ".png",
        ".tga",
        ".jpg",
        ".jpeg",
        ".vfx",
        ".prefab",
        ".rendertexture",
        ".cubemap",
        ".spriteatlas",
        ".spriteatlasv2"
    };

    private static readonly HashSet<string> TextReferenceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".asset",
        ".mat",
        ".shadergraph",
        ".shadersubgraph",
        ".vfx",
        ".prefab",
        ".spriteatlas",
        ".spriteatlasv2"
    };

    private static readonly string[] ExcludedFragments =
    {
        "/watercan/",
        "watercan",
        "watering",
        "character_water",
        "/audio/",
        "/readme/",
        "/scripts/"
    };

    [MenuItem("Tools/Map TopDown/Setup Water Tilemap System")]
    public static void SetupWaterTilemapSystem()
    {
        if (!AssetDatabase.IsValidFolder(SourceRoot))
        {
            Debug.LogError($"AutoWaterSetupTool: Source folder not found: {SourceRoot}");
            return;
        }

        EnsureFolder(DestinationRoot);
        EnsureFolder(ImportRoot);

        Dictionary<string, string> copiedAssets;

        try
        {
            AssetDatabase.StartAssetEditing();
            copiedAssets = CopyWaterAssets();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();
        RemapCopiedGuidReferences(copiedAssets);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var ruleTilePath = EnsureWaterRuleTile(copiedAssets.Values);
        var importedPaths = copiedAssets.Values
            .Concat(string.IsNullOrEmpty(ruleTilePath) ? Array.Empty<string>() : new[] { ruleTilePath })
            .ToArray();

        DeleteOldSquareWaterObject();

        var grid = FindTargetGrid();
        if (grid == null)
        {
            Debug.LogError("AutoWaterSetupTool: No Grid found. Open your map scene or create Grid_Map_45 first.");
            return;
        }

        var material = FindImportedMaterial(importedPaths);
        var waterTilemap = CreateWaterTilemap(grid, material);
        var paletteTile = FindPreferredPaletteTile(importedPaths);

        Selection.activeObject = paletteTile != null ? paletteTile : waterTilemap.gameObject;
        EditorGUIUtility.PingObject(Selection.activeObject);
        EditorSceneManager.MarkSceneDirty(waterTilemap.gameObject.scene);

        Debug.Log(
            "AutoWaterSetupTool: Imported HappyHarvest water assets and created a fresh water tilemap. " +
            $"Grid='{grid.name}', Tilemap='{waterTilemap.name}', " +
            $"PaletteTile='{(paletteTile != null ? paletteTile.name : "none")}', " +
            $"Material='{(material != null ? material.name : "default")}', CopiedAssets={copiedAssets.Count}.");
    }

    [MenuItem("Tools/Map TopDown/Import & Setup Water")]
    public static void ImportAndSetupWater()
    {
        SetupWaterTilemapSystem();
    }

    private static Dictionary<string, string> CopyWaterAssets()
    {
        var sourceAssets = CollectSourceAssets();
        var copied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourcePath in sourceAssets.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var destinationPath = ToDestinationPath(sourcePath);
            EnsureFolder(GetAssetDirectory(destinationPath));

            if (AssetDatabase.LoadAssetAtPath<Object>(destinationPath) != null)
            {
                AssetDatabase.DeleteAsset(destinationPath);
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            {
                Debug.LogWarning($"AutoWaterSetupTool: Could not copy {sourcePath} -> {destinationPath}");
                continue;
            }

            copied[sourcePath] = destinationPath;
        }

        return copied;
    }

    private static HashSet<string> CollectSourceAssets()
    {
        var directMatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in ExplicitSourceAssets)
        {
            AddIfExists(directMatches, path);
        }

        foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { SourceRoot + "/Art/Tiles/Water" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsCopyCandidate(path))
            {
                directMatches.Add(path);
            }
        }

        foreach (var guid in AssetDatabase.FindAssets("Water t:TileBase", new[] { SourceRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsCopyCandidate(path))
            {
                directMatches.Add(path);
            }
        }

        var withDependencies = new HashSet<string>(directMatches, StringComparer.OrdinalIgnoreCase);
        var dependencies = AssetDatabase.GetDependencies(directMatches.ToArray(), true);

        foreach (var dependency in dependencies)
        {
            if (IsCopyCandidate(dependency) && IsUnderFolder(dependency, SourceRoot))
            {
                withDependencies.Add(dependency);
            }
        }

        return withDependencies;
    }

    private static bool IsCopyCandidate(string path)
    {
        return !string.IsNullOrEmpty(path)
            && !AssetDatabase.IsValidFolder(path)
            && CopyableExtensions.Contains(Path.GetExtension(path))
            && !IsExcluded(path);
    }

    private static void DeleteOldSquareWaterObject()
    {
        var oldWaterObject = FindSceneObjects<GameObject>()
            .Where(gameObject => gameObject.name == OldWaterObjectName)
            .OrderByDescending(gameObject => gameObject.activeInHierarchy)
            .FirstOrDefault();

        if (oldWaterObject != null)
        {
            Object.DestroyImmediate(oldWaterObject);
        }
    }

    private static Grid FindTargetGrid()
    {
        var grids = FindSceneObjects<Grid>();
        return grids.FirstOrDefault(grid => grid.name.Equals("Grid_Map_45", StringComparison.OrdinalIgnoreCase))
            ?? grids.FirstOrDefault(grid => grid.name.Equals("Grid", StringComparison.OrdinalIgnoreCase))
            ?? grids.FirstOrDefault(grid => grid.gameObject.activeInHierarchy)
            ?? grids.FirstOrDefault();
    }

    private static Tilemap CreateWaterTilemap(Grid grid, Material material)
    {
        var waterObject = new GameObject(WaterTilemapName);
        Undo.RegisterCreatedObjectUndo(waterObject, "Create HappyHarvest water tilemap");
        waterObject.transform.SetParent(grid.transform, false);
        waterObject.transform.localPosition = Vector3.zero;
        waterObject.transform.localRotation = Quaternion.identity;
        waterObject.transform.localScale = Vector3.one;
        waterObject.layer = grid.gameObject.layer;

        var tilemap = Undo.AddComponent<Tilemap>(waterObject);
        tilemap.color = SourceWaterTilemapColor;
        tilemap.animationFrameRate = 1f;
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        tilemap.orientation = Tilemap.Orientation.XY;

        var renderer = Undo.AddComponent<TilemapRenderer>(waterObject);
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = SourceWaterSortingOrder;
        renderer.mode = TilemapRenderer.Mode.SRPBatch;
        renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
        renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Auto;

        EditorUtility.SetDirty(waterObject);
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);

        return tilemap;
    }

    private static Material FindImportedMaterial(IEnumerable<string> importedPaths)
    {
        return importedPaths
            .Where(path => string.Equals(Path.GetExtension(path), ".mat", StringComparison.OrdinalIgnoreCase))
            .Select(AssetDatabase.LoadAssetAtPath<Material>)
            .Where(material => material != null)
            .OrderByDescending(material => material.name.Equals(WaterMaterialName, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(material => material.name.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault();
    }

    private static TileBase FindPreferredPaletteTile(IEnumerable<string> importedPaths)
    {
        return importedPaths
            .Where(path => string.Equals(Path.GetExtension(path), ".asset", StringComparison.OrdinalIgnoreCase))
            .Select(AssetDatabase.LoadAssetAtPath<TileBase>)
            .Where(tile => tile != null)
            .OrderByDescending(tile => tile.name.Equals("Water_RuleTile", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(tile => tile.name.Equals("Tile_Water", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(tile => tile.name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault();
    }

    private static string EnsureWaterRuleTile(IEnumerable<string> importedPaths)
    {
        var importedPathArray = importedPaths.ToArray();
        var existingRuleTilePath = importedPathArray.FirstOrDefault(path =>
            string.Equals(Path.GetExtension(path), ".asset", StringComparison.OrdinalIgnoreCase)
            && Path.GetFileNameWithoutExtension(path).IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0
            && Path.GetFileNameWithoutExtension(path).IndexOf("RuleTile", StringComparison.OrdinalIgnoreCase) >= 0
            && AssetDatabase.LoadAssetAtPath<TileBase>(path) != null);

        if (!string.IsNullOrEmpty(existingRuleTilePath))
        {
            return existingRuleTilePath;
        }

        var ruleTileType = FindRuleTileType();
        if (ruleTileType == null)
        {
            Debug.LogWarning("AutoWaterSetupTool: RuleTile type was not found. Use imported Tile_Water directly in the Tile Palette.");
            return null;
        }

        var sprite = FindWaterSprite(importedPathArray);
        if (sprite == null)
        {
            Debug.LogWarning("AutoWaterSetupTool: Could not find Sprite_WaterIcon for Water_RuleTile.");
            return null;
        }

        var ruleTilePath = ImportRoot + "/Art/Tiles/Water/Water_RuleTile.asset";
        EnsureFolder(GetAssetDirectory(ruleTilePath));

        var existingRuleTile = AssetDatabase.LoadAssetAtPath<TileBase>(ruleTilePath);
        if (existingRuleTile != null)
        {
            ConfigureRuleTile(existingRuleTile, sprite);
            return ruleTilePath;
        }

        var ruleTile = ScriptableObject.CreateInstance(ruleTileType);
        ruleTile.name = "Water_RuleTile";
        ConfigureRuleTile(ruleTile, sprite);
        AssetDatabase.CreateAsset(ruleTile, ruleTilePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ruleTilePath, ImportAssetOptions.ForceUpdate);

        return ruleTilePath;
    }

    private static Type FindRuleTileType()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("UnityEngine.RuleTile") ?? assembly.GetType("UnityEngine.Tilemaps.RuleTile");
            if (type != null && typeof(ScriptableObject).IsAssignableFrom(type))
            {
                return type;
            }
        }

        return null;
    }

    private static Sprite FindWaterSprite(IEnumerable<string> importedPaths)
    {
        foreach (var path in importedPaths)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null && tile.sprite != null)
            {
                return tile.sprite;
            }
        }

        return importedPaths
            .Where(IsTexturePath)
            .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
            .OfType<Sprite>()
            .OrderByDescending(sprite => sprite.name.Equals("Sprite_WaterIcon", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(sprite => sprite.name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault();
    }

    private static void ConfigureRuleTile(Object ruleTile, Sprite sprite)
    {
        var serializedRuleTile = new SerializedObject(ruleTile);

        var defaultSprite = serializedRuleTile.FindProperty("m_DefaultSprite");
        if (defaultSprite != null)
        {
            defaultSprite.objectReferenceValue = sprite;
        }

        var colliderType = serializedRuleTile.FindProperty("m_DefaultColliderType");
        if (colliderType != null)
        {
            colliderType.enumValueIndex = 0;
        }

        serializedRuleTile.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ruleTile);
    }

    private static void RemapCopiedGuidReferences(Dictionary<string, string> copiedAssets)
    {
        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in copiedAssets)
        {
            var oldGuid = AssetDatabase.AssetPathToGUID(pair.Key);
            var newGuid = AssetDatabase.AssetPathToGUID(pair.Value);

            if (!string.IsNullOrEmpty(oldGuid) && !string.IsNullOrEmpty(newGuid) && oldGuid != newGuid)
            {
                guidMap[oldGuid] = newGuid;
            }
        }

        foreach (var newPath in copiedAssets.Values)
        {
            if (!TextReferenceExtensions.Contains(Path.GetExtension(newPath)))
            {
                continue;
            }

            var absolutePath = ToAbsolutePath(newPath);
            if (!File.Exists(absolutePath))
            {
                continue;
            }

            try
            {
                var text = File.ReadAllText(absolutePath);
                var remapped = text;

                foreach (var pair in guidMap)
                {
                    remapped = remapped.Replace(pair.Key, pair.Value);
                }

                if (remapped != text)
                {
                    File.WriteAllText(absolutePath, remapped);
                    AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"AutoWaterSetupTool: Skipped GUID remap for {newPath}. {exception.Message}");
            }
        }
    }

    private static string ToDestinationPath(string sourcePath)
    {
        var normalizedSourcePath = sourcePath.Replace('\\', '/');
        var relativePath = normalizedSourcePath.Substring(SourceRoot.Length).TrimStart('/');
        return ImportRoot + "/" + relativePath;
    }

    private static bool IsUnderFolder(string path, string folder)
    {
        var normalizedPath = path.Replace('\\', '/');
        var normalizedFolder = folder.TrimEnd('/').Replace('\\', '/');
        return normalizedPath.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTexturePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tga", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExcluded(string path)
    {
        var lowerPath = path.Replace('\\', '/').ToLowerInvariant();
        return ExcludedFragments.Any(lowerPath.Contains);
    }

    private static void AddIfExists(HashSet<string> paths, string path)
    {
        if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
        {
            paths.Add(path);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        folderPath = folderPath.Replace('\\', '/').TrimEnd('/');

        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parts = folderPath.Split('/');
        var current = parts[0];

        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string GetAssetDirectory(string assetPath)
    {
        return Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? DestinationRoot;
    }

    private static string ToAbsolutePath(string assetPath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return Path.Combine(projectRoot ?? string.Empty, assetPath);
    }

    private static T[] FindSceneObjects<T>() where T : Object
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(obj => obj != null)
            .Where(obj => !EditorUtility.IsPersistent(obj))
            .ToArray();
    }
}
