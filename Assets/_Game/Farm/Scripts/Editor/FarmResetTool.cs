#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

public static class FarmResetTool
{
    [MenuItem("Tools/SCN Farm/Hard Reset Everything")]
    private static void HardResetEverything()
    {
        if (!EditorUtility.DisplayDialog(
                "Hard Reset Everything",
                "XÃ³a toÃ n bá»™ PlayerPrefs (Ã´ Ä‘áº¥t, nhÃ , tiá»n, kho)?\nHÃ nh Ä‘á»™ng nÃ y KHÃ”NG thá»ƒ hoÃ n tÃ¡c.",
                "XÃ³a háº¿t", "Há»§y"))
            return;

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
#endif
