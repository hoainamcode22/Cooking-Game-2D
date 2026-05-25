using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Chuyen Scene hien tai tu Isometric Diamond sang 2D Top-Down Rectangular Grid.
/// Tool chi tac dong cac object dang nam trong Scene dang mo:
/// - Grid
/// - Main Camera / Camera dau tien trong Scene
/// - TilemapRenderer con cua Grid
/// - Graphics transparency sorting settings
/// </summary>
public static class TopDownRectGridConverter
{
    [MenuItem("Tools/Map TopDown/Convert Active Scene To Rect Grid")]
    public static void ConvertActiveSceneToRectGrid()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            Debug.LogError("[TopDownRectGridConverter] Khong tim thay Scene dang mo hop le.");
            return;
        }

        Undo.SetCurrentGroupName("Convert Scene To TopDown Rect Grid");
        var undoGroup = Undo.GetCurrentGroup();

        var convertedGridCount = ConvertAllSceneGrids(activeScene);
        var convertedCamera = ConfigureMainCamera(activeScene);
        ConfigureTopDownTransparencySorting();
        var convertedRendererCount = ConfigureTilemapRenderersUnderGrids(activeScene);

        EditorSceneManager.MarkSceneDirty(activeScene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            "[TopDownRectGridConverter] Hoan tat chuyen Scene sang Top-Down Rect Grid. " +
            $"Grid: {convertedGridCount}, Camera: {(convertedCamera ? "OK" : "Khong tim thay")}, " +
            $"TilemapRenderer: {convertedRendererCount}.");
    }

    private static int ConvertAllSceneGrids(Scene scene)
    {
        var count = 0;
        var grids = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Grid>(true))
            .ToArray();

        foreach (var grid in grids)
        {
            Undo.RecordObject(grid, "Convert Grid To Rectangle");

            // Dua luoi ve Top-Down 2D chuan: o vuong, khong nghieng isometric.
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            grid.cellSize = new Vector3(1f, 1f, 0f);
            grid.cellGap = Vector3.zero;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

            EditorUtility.SetDirty(grid);
            count++;
        }

        if (count == 0)
        {
            Debug.LogWarning("[TopDownRectGridConverter] Khong tim thay component Grid trong Scene hien tai.");
        }

        return count;
    }

    private static bool ConfigureMainCamera(Scene scene)
    {
        var camera = Camera.main;
        if (camera == null || camera.gameObject.scene != scene)
        {
            camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault();
        }

        if (camera == null)
        {
            Debug.LogWarning("[TopDownRectGridConverter] Khong tim thay Camera trong Scene hien tai.");
            return false;
        }

        Undo.RecordObject(camera, "Configure TopDown Camera");
        Undo.RecordObject(camera.transform, "Reset Camera Rotation");

        // Camera 2D Top-Down: Orthographic va nhin thang vao mat phang XY.
        camera.orthographic = true;
        camera.transform.rotation = Quaternion.identity;

        // Giu nguyen X/Y hien tai, dam bao camera nam truoc mat phang tilemap theo setup 2D mac dinh.
        var position = camera.transform.position;
        if (position.z >= 0f)
        {
            position.z = -10f;
            camera.transform.position = position;
        }

        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(camera.transform);
        return true;
    }

    private static void ConfigureTopDownTransparencySorting()
    {
        // Y-Sorting Top-Down: object co Y thap hon duoc ve o phia truoc.
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);
    }

    private static int ConfigureTilemapRenderersUnderGrids(Scene scene)
    {
        var count = 0;
        var grids = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Grid>(true))
            .ToArray();

        foreach (var grid in grids)
        {
            var renderers = grid.GetComponentsInChildren<TilemapRenderer>(true);
            foreach (var renderer in renderers)
            {
                Undo.RecordObject(renderer, "Configure Tilemap Renderer");

                // Individual de Unity tinh Y-Sorting theo tung tile/sprite thay vi gom chunk.
                renderer.mode = TilemapRenderer.Mode.Individual;

                EditorUtility.SetDirty(renderer);
                count++;
            }
        }

        if (count == 0)
        {
            Debug.LogWarning("[TopDownRectGridConverter] Khong tim thay TilemapRenderer nam duoi cac Grid.");
        }

        return count;
    }
}
