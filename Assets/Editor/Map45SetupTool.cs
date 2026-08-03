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
}
#endif
