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

        // Đang Play: manager là DontDestroyOnLoad nên còn sống → phải reset bộ nhớ luôn,
        // nếu không vàng/kim cương (và level) sẽ bị ghi đè lại giá trị cũ.
        if (Application.isPlaying)
        {
            if (FarmEconomyManager.Instance != null)
                FarmEconomyManager.Instance.ResetCurrency();
            if (PlayerProgressManager.Instance != null)
                PlayerProgressManager.Instance.ForceSetLevelExp(1, 0);
        }
    }
}
#endif
