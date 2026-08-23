#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Farm.EditorTools.Mill;

public static class MillRebuildBatch
{
    [MenuItem("Tools/Farm/Popup May Xay/Rebuild All Now", false, -1)]
    public static void ExecuteRebuild()
    {
        Debug.Log("[MillRebuildBatch] Rebuilding Mill UI Popup with updated 2D assets & layout...");
        MillPopupBuilderTool.LamTatCa();
        Debug.Log("[MillRebuildBatch] Rebuild completed successfully!");
    }
}
#endif
