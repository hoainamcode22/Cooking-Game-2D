#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.Tilemaps;

/// <summary>
/// Bo cong cu setup ve map 45 do (kieu Hay Day/Township).
/// Buoc 1: Tools > Map45 > 1. Tao Palette_45
/// Buoc 2: Tools > Map45 > 2. Tao Grid_45 vao Scene (roi Save Scene)
/// Sau do: Window > 2D > Tile Palette > chon Palette_45 > quet co / quet dat.
/// </summary>
public static class Map45SetupTool
{
    const string Root = "Assets/maptitle/Map45/";

    [MenuItem("Tools/Map45/1. Tao Palette_45")]
    public static void CreatePalette()
    {
        var grass = AssetDatabase.LoadAssetAtPath<TileBase>(Root + "RuleTile_Grass45.asset");
        var dirt  = AssetDatabase.LoadAssetAtPath<TileBase>(Root + "RuleTile_Dirt45.asset");
        if (grass == null || dirt == null)
        {
            EditorUtility.DisplayDialog("Map45", "Khong tim thay RuleTile_Grass45 / RuleTile_Dirt45 trong " + Root, "OK");
            return;
        }

        var go = new GameObject("Palette_45", typeof(Grid));
        var layer = new GameObject("Layer1", typeof(Tilemap), typeof(TilemapRenderer));
        layer.transform.SetParent(go.transform);
        var tm = layer.GetComponent<Tilemap>();
        tm.SetTile(new Vector3Int(0, 0, 0), grass); // o 1: co
        tm.SetTile(new Vector3Int(1, 0, 0), dirt);  // o 2: dat
        layer.GetComponent<TilemapRenderer>().enabled = false;

        string path = Root + "Palette_45.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        // GridPalette sub-asset de Tile Palette window nhan dien
        var palette = ScriptableObject.CreateInstance<GridPalette>();
        palette.name = "Palette Settings";
        palette.cellSizing = GridPalette.CellSizing.Automatic;
        AssetDatabase.AddObjectToAsset(palette, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path);

        EditorUtility.DisplayDialog("Map45",
            "Da tao " + path + "\n\nMo Window > 2D > Tile Palette, chon Palette_45:\n- O trai: co (RuleTile_Grass45)\n- O phai: dat (RuleTile_Dirt45)", "OK");
    }

    [MenuItem("Tools/Map45/2. Tao Grid_45 vao Scene")]
    public static void CreateGridInScene()
    {
        var grid = new GameObject("Grid_45", typeof(Grid));

        // Nen co trai toan map (nhu Township) - SpriteRenderer tiled
        var ground = new GameObject("GroundBase45", typeof(SpriteRenderer));
        ground.transform.SetParent(grid.transform);
        var sr = ground.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Texture_GrassBase45.png");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(60f, 40f);   // chinh lai theo kich thuoc map
        sr.sortingOrder = -10;
        ground.transform.position = Vector3.zero;

        MakeTilemapLayer(grid, "Tilemap_Dirt45", 1);   // ve duong dat len nen co
        MakeTilemapLayer(grid, "Tilemap_Grass45", 2);  // ve dao co dam noi len tren

        // Y-sorting cho decor 3/4 sau nay
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = Vector3.up;
        }

        Selection.activeGameObject = grid;
        EditorUtility.DisplayDialog("Map45",
            "Da tao Grid_45 trong scene (nho Save Scene - Ctrl+S).\n\nCach ve:\n1. Mo Tile Palette, chon Palette_45\n2. Chon o CO -> click chon Tilemap_Grass45 -> quet\n3. Chon o DAT -> chon Tilemap_Dirt45 -> quet\nVien tu noi lien, khong lo line.", "OK");
    }

    static void MakeTilemapLayer(GameObject parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
    }

    // ================== BO ISOMETRIC (kieu Township/Hay Day) ==================
    const string IsoRoot = "Assets/maptitle/Map45Iso/";

    [MenuItem("Tools/Map45/3. Tao Palette_Iso45")]
    public static void CreateIsoPalette()
    {
        var grass = AssetDatabase.LoadAssetAtPath<TileBase>(IsoRoot + "RuleTile_IsoGrass45.asset");
        var dirt  = AssetDatabase.LoadAssetAtPath<TileBase>(IsoRoot + "RuleTile_IsoDirt45.asset");
        var rock  = AssetDatabase.LoadAssetAtPath<TileBase>(IsoRoot + "RuleTile_IsoRock45.asset");
        var stone = AssetDatabase.LoadAssetAtPath<TileBase>(IsoRoot + "RuleTile_IsoStone45.asset");
        var patch = AssetDatabase.LoadAssetAtPath<TileBase>(IsoRoot + "RuleTile_IsoDirtPatch45.asset");
        var sand  = AssetDatabase.LoadAssetAtPath<TileBase>(IsoRoot + "RuleTile_IsoSand45.asset");
        var dock  = AssetDatabase.LoadAssetAtPath<TileBase>(IsoRoot + "RuleTile_IsoDock45.asset");
        var fence = AssetDatabase.LoadAssetAtPath<TileBase>(IsoRoot + "RuleTile_IsoFence45.asset");
        if (grass == null || dirt == null)
        {
            EditorUtility.DisplayDialog("Map45", "Khong tim thay RuleTile_IsoGrass45 / RuleTile_IsoDirt45 trong " + IsoRoot, "OK");
            return;
        }
        // xoa palette cu neu co (de chay lai menu nay la palette tu cap nhat)
        if (AssetDatabase.LoadAssetAtPath<Object>(IsoRoot + "Palette_Iso45.prefab") != null)
            AssetDatabase.DeleteAsset(IsoRoot + "Palette_Iso45.prefab");

        var go = new GameObject("Palette_Iso45", typeof(Grid));
        var g = go.GetComponent<Grid>();
        g.cellLayout = GridLayout.CellLayout.Isometric;
        g.cellSize = new Vector3(1f, 0.5f, 1f);

        var layer = new GameObject("Layer1", typeof(Tilemap), typeof(TilemapRenderer));
        layer.transform.SetParent(go.transform);
        var tm = layer.GetComponent<Tilemap>();
        tm.SetTile(new Vector3Int(0, 0, 0), grass); // o 1: co
        tm.SetTile(new Vector3Int(1, 0, 0), dirt);  // o 2: dat
        if (rock != null) tm.SetTile(new Vector3Int(2, 0, 0), rock);   // o 3: nui
        if (stone != null) tm.SetTile(new Vector3Int(3, 0, 0), stone); // o 4: duong da
        if (patch != null) tm.SetTile(new Vector3Int(4, 0, 0), patch); // o 5: vung dat vien co
        if (sand != null) tm.SetTile(new Vector3Int(5, 0, 0), sand);   // o 6: bai cat/bien
        if (dock != null) tm.SetTile(new Vector3Int(6, 0, 0), dock);   // o 7: ben tau go
        if (fence != null) tm.SetTile(new Vector3Int(7, 0, 0), fence); // o 8: hang rao go
        layer.GetComponent<TilemapRenderer>().enabled = false;

        string path = IsoRoot + "Palette_Iso45.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        var palette = ScriptableObject.CreateInstance<GridPalette>();
        palette.name = "Palette Settings";
        palette.cellSizing = GridPalette.CellSizing.Manual;
        AssetDatabase.AddObjectToAsset(palette, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path);

        EditorUtility.DisplayDialog("Map45",
            "Da tao " + path + "\n\nMo Window > 2D > Tile Palette, chon Palette_Iso45:\n- O trai: co iso\n- O phai: dat iso", "OK");
    }

    [MenuItem("Tools/Map45/4. Tao Grid_Iso45 vao Scene")]
    public static void CreateIsoGridInScene()
    {
        var grid = new GameObject("Grid_Iso45", typeof(Grid));
        var g = grid.GetComponent<Grid>();
        g.cellLayout = GridLayout.CellLayout.Isometric;
        g.cellSize = new Vector3(1f, 0.5f, 1f);

        // Nen dat trai toan map (tile co iso ve len tren)
        var ground = new GameObject("GroundBase_Dirt", typeof(SpriteRenderer));
        ground.transform.SetParent(grid.transform);
        var sr = ground.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Texture_DirtBase45.png");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(80f, 50f);
        sr.sortingOrder = -10;

        MakeIsoLayer(grid, "Tilemap_IsoGrass", 1);  // ve co (co mep day + bong)
        MakeIsoLayer(grid, "Tilemap_IsoDirt", 2);   // ve duong dat len tren co
        MakeIsoLayer(grid, "Tilemap_IsoRock", 5);   // ve nui/vach da tren cung

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = Vector3.up;
        }

        Selection.activeGameObject = grid;
        EditorUtility.DisplayDialog("Map45",
            "Da tao Grid_Iso45 (nho Save Scene).\n\nCach ve:\n1. Tile Palette > Palette_Iso45\n2. O CO -> chon Tilemap_IsoGrass -> quet dao co\n3. O DAT -> chon Tilemap_IsoDirt -> quet duong dat\nTile kim cuong 2:1, map se xeo cheo nhu Township.", "OK");
    }

    static void MakeIsoLayer(GameObject parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        var r = go.GetComponent<TilemapRenderer>();
        r.sortingOrder = order;
        r.mode = TilemapRenderer.Mode.Individual; // sort tung o theo Y cho iso
    }

    // ================== THUYEN DU LICH ==================

    [MenuItem("Tools/Map45/5. Tao Thuyen Du Lich")]
    public static void CreateTouristFerry()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IsoRoot + "Boats/Boat_Ferry_AI.png");

        var root = new GameObject("TouristFerry");
        var ferry = root.AddComponent<FerryController>();

        var vis = new GameObject("Visual", typeof(SpriteRenderer));
        vis.transform.SetParent(root.transform);
        vis.transform.localPosition = Vector3.zero;
        var sr = vis.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 30; // tren mat nuoc
        ferry.visual = sr;

        // 4 waypoint mau: BenA -> bien -> bien -> BenB (keo tha lai tren map)
        string[] names = { "WP_BenA", "WP_Bien1", "WP_Bien2", "WP_BenB" };
        Vector3[] pos = { new Vector3(-5, 0), new Vector3(-2, -1.2f), new Vector3(2, -1.2f), new Vector3(5, 0) };
        ferry.waypoints = new Transform[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var wp = new GameObject(names[i]);
            wp.transform.SetParent(root.transform);
            wp.transform.position = pos[i];
            ferry.waypoints[i] = wp.transform;
        }

        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Map45",
            "Da tao TouristFerry (nho Save Scene).\n\n" +
            "1. Keo WP_BenA / WP_BenB vao 2 ben tau tren map, WP_Bien* ra giua bien\n" +
            "2. Chinh speed / dockWaitTime tren FerryController\n" +
            "3. Bam Play: thuyen chay A->B, dau ben don/tra khach, dap denh tren nuoc\n" +
            "4. Thay sprite trong Visual khi co art dep hon (Gemini)", "OK");
    }
}
#endif
