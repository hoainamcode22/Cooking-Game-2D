#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WarehousePopupUIHierarchyBuilder
{
    [MenuItem("Tools/Farm/Warehouse/Build Warehouse Upgrade UI")]
    public static void BuildWarehouseUpgradeUI()
    {
        WarehousePopupUI warehousePopup = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
        if (warehousePopup == null)
        {
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(warehousePopup.gameObject, "Build Warehouse Upgrade UI");
        warehousePopup.BuildWarehouseExtensionHierarchyForEditor();

        EditorUtility.SetDirty(warehousePopup);
        EditorSceneManager.MarkSceneDirty(warehousePopup.gameObject.scene);
    }
}
#endif
