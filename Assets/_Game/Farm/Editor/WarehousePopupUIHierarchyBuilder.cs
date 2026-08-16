#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WarehousePopupUIHierarchyBuilder
{
    [MenuItem("Tools/Farm/Warehouse/Build Warehouse Upgrade UI")]
    public static void BuildWarehouseUpgradeUI()
    {
        WarehouseNewUIBuilder.BuildWarehouseUI();
    }
}
#endif
