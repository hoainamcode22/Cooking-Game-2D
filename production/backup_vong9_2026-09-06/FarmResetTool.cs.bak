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
                "Xóa toàn bộ PlayerPrefs (ô đất, nhà, tiền, kho)?\nHành động này KHÔNG thể hoàn tác.",
                "Xóa hết", "Hủy"))
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

            // Kho hạt giống và kho nông sản cũng phải dọn TRONG BỘ NHỚ. DeleteAll() chỉ xoá
            // trên đĩa; hàng vẫn nằm trong list của manager và lần AddItem/RemoveItem kế tiếp
            // sẽ ghi lại y nguyên — đúng cái bẫy mà comment ở trên đã mô tả cho vàng/level.
            if (WarehouseManager.Instance != null)
                WarehouseManager.Instance.XoaSaveVaLamTrongKho();
            if (FarmInventoryManager.Instance != null)
                FarmInventoryManager.Instance.ClearAll();
        }
    }
}
#endif
