#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FarmResetTool
{
    [MenuItem("Tools/SCN Farm/Hard Reset Everything")]
    private static void HardResetEverything()
    {
        if (!EditorUtility.DisplayDialog(
                "Hard Reset Everything",
                "Xóa toàn bộ PlayerPrefs (ô đất, nhà, tiền, kho)?\nHành động này KHÔNG thể hoàn tác.",
                "Xóa hết", "Hủy"))
            return;

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[FarmResetTool] PlayerPrefs.DeleteAll() — toàn bộ dữ liệu đã xóa. Nhấn Play lại để test.");
    }
}
#endif
