using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

/// <summary>
/// Tool setup Tilemap Isometric 45 do cho project.
/// Nguyen tac an toan:
/// - Chi doc anh tu Assets/HappyHarvest/Art.
/// - Chi ghi asset sinh ra vao Assets/maptitle/AssetsTitl.
/// - Khong dung vao script, prefab, logic game san co.
/// </summary>
public sealed class Iso45TilemapOneClickSetup : EditorWindow
{
    private const string SourceRoot = "Assets/HappyHarvest/Art";
    private const string DestRoot = "Assets/maptitle/AssetsTitl";
    private const string SpritesRoot = DestRoot + "/Sprites";
    private const string TilesRoot = DestRoot + "/Tiles";
    private const string RuleTilesRoot = DestRoot + "/RuleTiles";

    private static readonly string[] ImageExtensions =
    {
        ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".bmp"
    };

    private static readonly string[] SheetKeywords =
    {
        "sheet", "spritesheet", "sprite_sheet", "atlas", "tileset", "tile_set", "tilemap", "tile_map"
    };

    private static readonly string[] RuleTileKeywords =
    {
        "ground", "dirt", "soil", "grass", "road", "path", "floor", "terrain", "dat", "nen", "co", "duong"
    };

    [SerializeField] private int pixelsPerUnit = 32;
    [SerializeField] private Vector2 isoPivot = new Vector2(0.5f, 0.25f);
    [SerializeField] private bool cleanupGeneratedFolders = true;
    [SerializeField] private bool autoSliceSpriteSheets = true;
    [SerializeField] private bool createRuleTilesIfPackageExists = true;

    [MenuItem("Tools/Map 45 Isometric/One Click Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<Iso45TilemapOneClickSetup>("Iso 45 Tilemap");
        window.minSize = new Vector2(460f, 260f);
        window.Show();
    }

    [MenuItem("Tools/Map 45 Isometric/Run Full Setup Now")]
    public static void RunFromMenu()
    {
        var window = CreateInstance<Iso45TilemapOneClickSetup>();
        window.RunFullSetup();
        DestroyImmediate(window);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Setup Tilemap Isometric 45", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tool chi doc tu Assets/HappyHarvest/Art va chi tao asset trong Assets/maptitle/AssetsTitl. " +
            "Graphics Settings se duoc set theo yeu cau Y-Sorting.",
            MessageType.Info);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            pixelsPerUnit = Mathf.Max(1, EditorGUILayout.IntField("Pixels Per Unit", pixelsPerUnit));
            isoPivot = EditorGUILayout.Vector2Field("Pivot Isometric", isoPivot);
            cleanupGeneratedFolders = EditorGUILayout.ToggleLeft("Xoa lai folder Sprites/Tiles/RuleTiles da sinh", cleanupGeneratedFolders);
            autoSliceSpriteSheets = EditorGUILayout.ToggleLeft("Tu nhan dien va slice spritesheet dang grid", autoSliceSpriteSheets);
            createRuleTilesIfPackageExists = EditorGUILayout.ToggleLeft("Tao RuleTile neu package 2D Tilemap Extras co san", createRuleTilesIfPackageExists);
        }

        GUILayout.Space(8f);

        if (GUILayout.Button("ONE CLICK: Copy + Process + Generate Map Hierarchy", GUILayout.Height(42f)))
        {
            RunFullSetup();
        }

        if (GUILayout.Button("Generate Map Hierarchy Only", GUILayout.Height(28f)))
        {
            GenerateMapHierarchy();
        }
    }

    private void RunFullSetup()
    {
        try
        {
            ValidateRootPaths();
            ListOldToolScriptsForManualReview();

            if (cleanupGeneratedFolders)
            {
                DeleteGeneratedFolder(SpritesRoot);
                DeleteGeneratedFolder(TilesRoot);
                DeleteGeneratedFolder(RuleTilesRoot);
            }

            EnsureAssetFolder(SpritesRoot);
            EnsureAssetFolder(TilesRoot);

            var copiedSpritePaths = CopyImagesToSpritesFolder();
            AssetDatabase.Refresh();

            var processedSpritePaths = ProcessCopiedTextures(copiedSpritePaths);
            var createdTiles = CreateTileAssets(processedSpritePaths);

            if (createRuleTilesIfPackageExists)
            {
                TryCreateBasicRuleTiles(processedSpritePaths);
            }

            GenerateMapHierarchy();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Iso45TilemapOneClickSetup] Hoan tat. " +
                $"Da copy {copiedSpritePaths.Count} anh, tao/cap nhat {createdTiles} Tile asset.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[Iso45TilemapOneClickSetup] Loi setup: " + ex);
        }
    }

    private static void ValidateRootPaths()
    {
        var sourceAbs = AssetPathToAbsolute(SourceRoot);
        var destAbs = AssetPathToAbsolute(DestRoot);
        var assetsAbs = NormalizeFullPath(Application.dataPath);

        if (!Directory.Exists(sourceAbs))
        {
            throw new DirectoryNotFoundException("Khong tim thay thu muc nguon: " + sourceAbs);
        }

        if (!Directory.Exists(destAbs))
        {
            Directory.CreateDirectory(destAbs);
        }

        // Khoa vung lam viec: moi duong dan deu phai nam trong Assets cua project hien tai.
        AssertInsideDirectory(sourceAbs, assetsAbs, "SourceRoot");
        AssertInsideDirectory(destAbs, assetsAbs, "DestRoot");
    }

    private static void ListOldToolScriptsForManualReview()
    {
        // Khong tu xoa script de tranh xoa nham. Chi liet ke file .cs trong folder dich de ban tu quyet dinh.
        var destAbs = AssetPathToAbsolute(DestRoot);
        if (!Directory.Exists(destAbs))
        {
            return;
        }

        var scripts = Directory.GetFiles(destAbs, "*.cs", SearchOption.AllDirectories)
            .Select(AbsoluteToAssetPath)
            .Where(path => !string.Equals(path, DestRoot + "/Editor/Iso45TilemapOneClickSetup.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (scripts.Length == 0)
        {
            return;
        }

        Debug.LogWarning(
            "[Iso45TilemapOneClickSetup] Phat hien script .cs trong folder dich. Tool KHONG tu xoa de dam bao an toan:\n" +
            string.Join("\n", scripts));
    }

    private static void DeleteGeneratedFolder(string assetFolder)
    {
        AssertAssetPathInside(assetFolder, DestRoot);
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            AssetDatabase.DeleteAsset(assetFolder);
        }
    }

    private static List<string> CopyImagesToSpritesFolder()
    {
        var sourceAbs = AssetPathToAbsolute(SourceRoot);
        var spritesAbs = AssetPathToAbsolute(SpritesRoot);
        AssertInsideDirectory(sourceAbs, AssetPathToAbsolute(SourceRoot), "sourceAbs");
        AssertInsideDirectory(spritesAbs, AssetPathToAbsolute(DestRoot), "spritesAbs");

        Directory.CreateDirectory(spritesAbs);

        var copiedAssetPaths = new List<string>();
        var imageFiles = Directory.GetFiles(sourceAbs, "*.*", SearchOption.AllDirectories)
            .Where(IsSupportedImageFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var sourceFile in imageFiles)
        {
            var relativePath = MakeRelativePath(sourceAbs, sourceFile);
            var destinationFile = NormalizeFullPath(Path.Combine(spritesAbs, relativePath));
            AssertInsideDirectory(destinationFile, spritesAbs, "destinationFile");

            var destinationDirectory = Path.GetDirectoryName(destinationFile);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourceFile, destinationFile, true);
            copiedAssetPaths.Add(AbsoluteToAssetPath(destinationFile));
        }

        return copiedAssetPaths;
    }

    private List<string> ProcessCopiedTextures(List<string> copiedSpritePaths)
    {
        var processed = new List<string>();

        foreach (var assetPath in copiedSpritePaths)
        {
            AssertAssetPathInside(assetPath, SpritesRoot);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            var dimensions = GetTextureDimensions(assetPath);
            var slice = autoSliceSpriteSheets ? DetectGridSlice(assetPath, dimensions.x, dimensions.y) : GridSlice.None;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;

            if (slice.IsValid)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritesheet = BuildSpriteSheetMeta(assetPath, dimensions.x, dimensions.y, slice.CellWidth, slice.CellHeight, isoPivot);
            }
            else
            {
                importer.spriteImportMode = SpriteImportMode.Single;

                // Unity moi yeu cau dung TextureImporterSettings de dat Custom Pivot cho Sprite Single.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = isoPivot;
                importer.SetTextureSettings(settings);
            }

            importer.SaveAndReimport();
            processed.Add(assetPath);
        }

        return processed;
    }

    private int CreateTileAssets(List<string> processedSpritePaths)
    {
        var count = 0;

        foreach (var texturePath in processedSpritePaths)
        {
            AssertAssetPathInside(texturePath, SpritesRoot);

            var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (sprites.Length == 0)
            {
                var single = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                if (single != null)
                {
                    sprites = new[] { single };
                }
            }

            var relativeTexturePath = texturePath.Substring(SpritesRoot.Length).TrimStart('/');
            var relativeFolder = Path.GetDirectoryName(relativeTexturePath)?.Replace('\\', '/') ?? string.Empty;
            var tileFolder = string.IsNullOrEmpty(relativeFolder) ? TilesRoot : TilesRoot + "/" + relativeFolder;
            EnsureAssetFolder(tileFolder);

            foreach (var sprite in sprites)
            {
                var tileName = SanitizeFileName(sprite.name);
                var tilePath = AssetDatabase.GenerateUniqueAssetPath(tileFolder + "/" + tileName + ".asset");
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = tileName;
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;

                AssetDatabase.CreateAsset(tile, tilePath);
                count++;
            }
        }

        return count;
    }

    private static void TryCreateBasicRuleTiles(List<string> processedSpritePaths)
    {
        var ruleTileType = FindRuleTileType();
        if (ruleTileType == null)
        {
            Debug.Log("[Iso45TilemapOneClickSetup] Khong tim thay RuleTile package. Bo qua buoc RuleTile bonus.");
            return;
        }

        EnsureAssetFolder(RuleTilesRoot);

        var terrainSprites = new List<Sprite>();
        foreach (var texturePath in processedSpritePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(texturePath).ToLowerInvariant();
            if (!RuleTileKeywords.Any(fileName.Contains))
            {
                continue;
            }

            terrainSprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>());
        }

        foreach (var sprite in terrainSprites.Take(24))
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(RuleTilesRoot + "/" + SanitizeFileName(sprite.name) + "_RuleTile.asset");
            var ruleTile = ScriptableObject.CreateInstance(ruleTileType);
            ruleTile.name = SanitizeFileName(sprite.name) + "_RuleTile";

            // RuleTile package khong phai built-in, nen set bang SerializedObject de tranh phu thuoc compile-time.
            var serialized = new SerializedObject(ruleTile);
            var defaultSprite = serialized.FindProperty("m_DefaultSprite");
            if (defaultSprite != null)
            {
                defaultSprite.objectReferenceValue = sprite;
            }

            var colliderType = serialized.FindProperty("m_DefaultColliderType");
            if (colliderType != null)
            {
                colliderType.enumValueIndex = 0;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(ruleTile, assetPath);
        }
    }

    private static void GenerateMapHierarchy()
    {
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, -0.26f);

        var root = GameObject.Find("Grid_Map_45");
        if (root == null)
        {
            root = new GameObject("Grid_Map_45");
            Undo.RegisterCreatedObjectUndo(root, "Create Grid_Map_45");
        }

        var grid = root.GetComponent<Grid>();
        if (grid == null)
        {
            grid = Undo.AddComponent<Grid>(root);
        }

        grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
        grid.cellSize = new Vector3(1f, 0.5f, 1f);
        grid.cellGap = Vector3.zero;

        CreateOrConfigureLayer(root.transform, "Dat_Nen", 0);
        CreateOrConfigureLayer(root.transform, "Co_Grass", 1);
        CreateOrConfigureLayer(root.transform, "VatThe_Props", 2);

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        Debug.Log("[Iso45TilemapOneClickSetup] Da tao/cap nhat Grid_Map_45 va Y-Sorting settings.");
    }

    private static void CreateOrConfigureLayer(Transform root, string layerName, int sortingOrder)
    {
        var layer = root.Find(layerName);
        GameObject layerObject;

        if (layer == null)
        {
            layerObject = new GameObject(layerName);
            Undo.RegisterCreatedObjectUndo(layerObject, "Create " + layerName);
            layerObject.transform.SetParent(root, false);
        }
        else
        {
            layerObject = layer.gameObject;
        }

        if (layerObject.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(layerObject);
        }

        var renderer = layerObject.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(layerObject);
        }

        renderer.mode = TilemapRenderer.Mode.Individual;
        renderer.sortingOrder = sortingOrder;
        renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
        EditorUtility.SetDirty(layerObject);
    }

    private static Vector2Int GetTextureDimensions(string assetPath)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            return Vector2Int.zero;
        }

        return new Vector2Int(texture.width, texture.height);
    }

    private static GridSlice DetectGridSlice(string assetPath, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return GridSlice.None;
        }

        var fileName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        var nameLooksLikeSheet = SheetKeywords.Any(fileName.Contains);
        if (!nameLooksLikeSheet)
        {
            return GridSlice.None;
        }

        // Uu tien tile isometric 2:1, sau do moi den tile vuong pho bien.
        var candidates = new[]
        {
            new GridSlice(64, 32),
            new GridSlice(128, 64),
            new GridSlice(32, 16),
            new GridSlice(32, 32),
            new GridSlice(64, 64),
            new GridSlice(128, 128),
            new GridSlice(16, 16)
        };

        foreach (var candidate in candidates)
        {
            if (width % candidate.CellWidth != 0 || height % candidate.CellHeight != 0)
            {
                continue;
            }

            var spriteCount = (width / candidate.CellWidth) * (height / candidate.CellHeight);
            if (spriteCount > 1 && spriteCount <= 512)
            {
                return candidate;
            }
        }

        return GridSlice.None;
    }

    private static SpriteMetaData[] BuildSpriteSheetMeta(
        string assetPath,
        int textureWidth,
        int textureHeight,
        int cellWidth,
        int cellHeight,
        Vector2 pivot)
    {
        var result = new List<SpriteMetaData>();
        var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(assetPath));
        var row = 0;

        // Unity dung toa do rect goc duoi-trai. Vong lap nay dat ten theo hang tren-xuong-duoi cho de doc.
        for (var y = textureHeight - cellHeight; y >= 0; y -= cellHeight)
        {
            var column = 0;
            for (var x = 0; x <= textureWidth - cellWidth; x += cellWidth)
            {
                result.Add(new SpriteMetaData
                {
                    name = $"{baseName}_{row:00}_{column:00}",
                    rect = new Rect(x, y, cellWidth, cellHeight),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = pivot
                });
                column++;
            }
            row++;
        }

        return result.ToArray();
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

    private static bool IsSupportedImageFile(string path)
    {
        var extension = Path.GetExtension(path);
        return ImageExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        AssertAssetPathInside(assetFolder, DestRoot);

        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        var parts = assetFolder.Split('/');
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

    private static string AssetPathToAbsolute(string assetPath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return NormalizeFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string AbsoluteToAssetPath(string absolutePath)
    {
        var fullPath = NormalizeFullPath(absolutePath);
        var assetsPath = NormalizeFullPath(Application.dataPath);

        AssertInsideDirectory(fullPath, assetsPath, "absolutePath");

        if (string.Equals(fullPath, assetsPath, StringComparison.OrdinalIgnoreCase))
        {
            return "Assets";
        }

        var relative = fullPath.Substring(assetsPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return "Assets/" + relative.Replace('\\', '/');
    }

    private static string MakeRelativePath(string root, string path)
    {
        var rootUri = new Uri(AppendDirectorySeparator(NormalizeFullPath(root)));
        var pathUri = new Uri(NormalizeFullPath(path));
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }

    private static string NormalizeFullPath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void AssertAssetPathInside(string assetPath, string allowedRoot)
    {
        var absolutePath = AssetPathToAbsolute(assetPath);
        var absoluteRoot = AssetPathToAbsolute(allowedRoot);
        AssertInsideDirectory(absolutePath, absoluteRoot, assetPath);
    }

    private static void AssertInsideDirectory(string path, string allowedRoot, string label)
    {
        var normalizedPath = NormalizeFullPath(path);
        var normalizedRoot = NormalizeFullPath(allowedRoot);
        var comparison = StringComparison.OrdinalIgnoreCase;

        if (string.Equals(normalizedPath, normalizedRoot, comparison))
        {
            return;
        }

        var rootWithSeparator = AppendDirectorySeparator(normalizedRoot);
        if (!normalizedPath.StartsWith(rootWithSeparator, comparison))
        {
            throw new InvalidOperationException($"{label} nam ngoai vung duoc phep: {normalizedPath}");
        }
    }

    private static string SanitizeFileName(string rawName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safe = new string(rawName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Tile" : safe;
    }

    private struct GridSlice
    {
        public static readonly GridSlice None = new GridSlice(0, 0);

        public readonly int CellWidth;
        public readonly int CellHeight;

        public GridSlice(int cellWidth, int cellHeight)
        {
            CellWidth = cellWidth;
            CellHeight = cellHeight;
        }

        public bool IsValid => CellWidth > 0 && CellHeight > 0;
    }
}
