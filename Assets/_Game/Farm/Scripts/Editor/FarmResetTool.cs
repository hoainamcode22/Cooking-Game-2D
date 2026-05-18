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
        Debug.Log("[FarmResetTool] PlayerPrefs.DeleteAll() — toàn bộ dữ liệu đã xóa. Nhấn Play lại để test.");
    }
}

// ── VFX Prefab Builder ────────────────────────────────────────────────────────
// Tạo sẵn 3 hierarchy VFX trong scene: SeedRain, SeedCostText, HarvestAmountText.
// Sau khi bấm menu: kéo object vào Project → Prefabs rồi gán vào FarmCropVFXSpawner.

public static class FarmVFXPrefabBuilder
{
    [MenuItem("FarmTools/Create Farming VFX Hierarchy")]
    public static void CreateFromFarmTools() => BuildAll();

    [MenuItem("Tools/Farm/Create Farming VFX Hierarchy")]
    public static void CreateFromTools() => BuildAll();

    private static void BuildAll()
    {
        CreateSeedRainVFX();
        CreateSeedCostTextVFX();
        CreateHarvestAmountTextVFX();
        Debug.Log("[FarmVFXPrefabBuilder] 3 VFX hierarchy tạo xong — kéo vào Project để tạo prefab.");
    }

    // ── PF_SeedRain_World ─────────────────────────────────────────────────────

    private static void CreateSeedRainVFX()
    {
        const string ROOT_NAME = "PF_SeedRain_World";
        if (FindAndSelect(ROOT_NAME)) return;

        GameObject root = new GameObject(ROOT_NAME);
        root.AddComponent<SeedRainVFX>();

        // IconTemplate: SpriteRenderer child dùng làm mẫu clone
        GameObject tpl = new GameObject("IconTemplate");
        tpl.transform.SetParent(root.transform, false);
        SpriteRenderer sr = tpl.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "FX";
        sr.sortingOrder     = 200;
        tpl.SetActive(false); // ẩn template, code sẽ clone và bật lại

        Selection.activeGameObject = root;
        Debug.Log($"[FarmVFXPrefabBuilder] '{ROOT_NAME}' tạo xong — gán Sprite vào IconTemplate rồi kéo thành prefab.");
    }

    // ── PF_SeedCostText_World ─────────────────────────────────────────────────

    private static void CreateSeedCostTextVFX()
    {
        const string ROOT_NAME = "PF_SeedCostText_World";
        if (FindAndSelect(ROOT_NAME)) return;

        GameObject root = new GameObject(ROOT_NAME);
        root.AddComponent<SeedCostTextVFX>();

        GameObject tpl = new GameObject("TextTemplate");
        tpl.transform.SetParent(root.transform, false);
        TextMeshPro tmp = tpl.AddComponent<TextMeshPro>();
        tmp.text             = "-1";
        tmp.fontSize         = 5f;
        tmp.color            = new Color(1f, 0.35f, 0.15f, 1f);
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.sortingLayerID   = SortingLayer.NameToID("FX");
        tmp.sortingOrder     = 210;
        tpl.SetActive(false);

        Selection.activeGameObject = root;
        Debug.Log($"[FarmVFXPrefabBuilder] '{ROOT_NAME}' tạo xong — kéo thành prefab.");
    }

    // ── PF_HarvestAmountText_World ────────────────────────────────────────────

    private static void CreateHarvestAmountTextVFX()
    {
        const string ROOT_NAME = "PF_HarvestAmountText_World";
        if (FindAndSelect(ROOT_NAME)) return;

        GameObject root = new GameObject(ROOT_NAME);
        root.AddComponent<HarvestAmountTextVFX>();

        GameObject tpl = new GameObject("TextTemplate");
        tpl.transform.SetParent(root.transform, false);
        TextMeshPro tmp = tpl.AddComponent<TextMeshPro>();
        tmp.text             = "+4";
        tmp.fontSize         = 6f;
        tmp.color            = new Color(0.2f, 0.95f, 0.3f, 1f);
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.sortingLayerID   = SortingLayer.NameToID("FX");
        tmp.sortingOrder     = 210;
        tpl.SetActive(false);

        Selection.activeGameObject = root;
        Debug.Log($"[FarmVFXPrefabBuilder] '{ROOT_NAME}' tạo xong — kéo thành prefab.");
    }

    // Nếu object tên ROOT_NAME đã tồn tại trong scene → select và báo skip
    private static bool FindAndSelect(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.LogWarning($"[FarmVFXPrefabBuilder] '{name}' đã tồn tại trong scene — chọn lại object cũ.");
            return true;
        }
        return false;
    }
}
#endif
