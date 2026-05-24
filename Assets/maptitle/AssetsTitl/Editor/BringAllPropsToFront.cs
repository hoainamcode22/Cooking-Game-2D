using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Dua tat ca prop/cong trinh dung SpriteRenderer len tren Tilemap trong Scene hien tai.
///
/// Nguyen tac an toan:
/// - Khong sua Tilemap hien tai.
/// - Khong xoa GameObject.
/// - Khong them/xoa/sua script logic gameplay.
/// - Chi sua thuoc tinh visual cua SpriteRenderer: Sorting Layer va Order in Layer.
/// </summary>
public static class BringAllPropsToFront
{
    private const int FrontSortingOrder = 500;

    [MenuItem("Tools/Map TopDown/Bring All Props To Front")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[BringAllPropsToFront] Khong tim thay Scene dang mo hop le.");
            return;
        }

        var roots = scene.GetRootGameObjects();

        // Chi doc thong tin TilemapRenderer de lay Sorting Layer lam moc, khong thay doi Tilemap.
        var tilemapRenderers = roots
            .SelectMany(root => root.GetComponentsInChildren<TilemapRenderer>(true))
            .Where(renderer => renderer != null)
            .ToArray();

        var targetSortingLayerId = GetHighestTilemapSortingLayerId(tilemapRenderers);
        var targetSortingLayerName = string.IsNullOrEmpty(SortingLayer.IDToName(targetSortingLayerId))
            ? "Current Sprite Layer"
            : SortingLayer.IDToName(targetSortingLayerId);

        var spriteRenderers = roots
            .SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
            .Where(IsIndependentSpriteRenderer)
            .ToArray();

        if (spriteRenderers.Length == 0)
        {
            Debug.LogWarning("[BringAllPropsToFront] Khong tim thay SpriteRenderer doc lap nao trong Scene.");
            return;
        }

        Undo.SetCurrentGroupName("Bring All Props To Front");
        var undoGroup = Undo.GetCurrentGroup();

        foreach (var spriteRenderer in spriteRenderers)
        {
            Undo.RecordObject(spriteRenderer, "Bring Prop To Front");

            // Neu Scene co TilemapRenderer, dua prop ve cung Sorting Layer cao nhat cua Tilemap.
            // Sau do day Order in Layer len cao de prop noi tren nen dat/co.
            if (tilemapRenderers.Length > 0)
            {
                spriteRenderer.sortingLayerID = targetSortingLayerId;
            }

            spriteRenderer.sortingOrder = FrontSortingOrder;
            EditorUtility.SetDirty(spriteRenderer);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            "[BringAllPropsToFront] Hoan tat. " +
            $"Da dua {spriteRenderers.Length} SpriteRenderer len truoc Tilemap. " +
            $"Sorting Layer: {targetSortingLayerName}, Order in Layer: {FrontSortingOrder}. " +
            "Tilemap va script logic khong bi thay doi.");
    }

    private static bool IsIndependentSpriteRenderer(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
        {
            return false;
        }

        // Bo qua moi SpriteRenderer neu no nam trong object Tilemap, phong truong hop co setup dac biet.
        if (spriteRenderer.GetComponentInParent<Tilemap>(true) != null)
        {
            return false;
        }

        if (spriteRenderer.GetComponentInParent<TilemapRenderer>(true) != null)
        {
            return false;
        }

        return true;
    }

    private static int GetHighestTilemapSortingLayerId(TilemapRenderer[] tilemapRenderers)
    {
        if (tilemapRenderers == null || tilemapRenderers.Length == 0)
        {
            return 0;
        }

        return tilemapRenderers
            .OrderByDescending(renderer => SortingLayer.GetLayerValueFromID(renderer.sortingLayerID))
            .ThenByDescending(renderer => renderer.sortingOrder)
            .First()
            .sortingLayerID;
    }
}
