using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup Shop Locks
///
/// Thêm ShopLevelLockUI component và lock overlay UI vào prefab shop item.
/// Cần có ShopManager trong scene để tìm itemPrefab.
///
/// Sau khi chạy:
///   - Prefab shop item sẽ có ShopLevelLockUI component
///   - Có overlay tối + icon ổ khóa + text "Mở ở cấp X"
///   - ShopItemUI.Setup() sẽ tự gọi Refresh() để hiện/ẩn lock
/// </summary>
public static class ShopLockSetupTool
{
    private const string MENU_ADD    = "Tools/Farm Game/Setup Shop Locks/Add Lock UI to Item Prefab";
    private const string MENU_REPORT = "Tools/Farm Game/Setup Shop Locks/Report Current unlockLevel";

    [MenuItem(MENU_ADD)]
    public static void AddLockUIToItemPrefab()
    {
        // Tìm ShopManager để lấy itemPrefab
        var shopManager = Object.FindFirstObjectByType<ShopManager>();
        if (shopManager == null)
        {
            EditorUtility.DisplayDialog("Shop Lock Setup",
                "Không tìm thấy ShopManager trong scene!\n\n" +
                "Hãy mở scene SCN_Farm (hoặc scene chứa ShopManager) rồi chạy lại.",
                "OK");
            return;
        }

        var itemPrefabSO = new SerializedObject(shopManager);
        var itemPrefabProp = itemPrefabSO.FindProperty("itemPrefab");
        var itemPrefab = itemPrefabProp?.objectReferenceValue as GameObject;

        if (itemPrefab == null)
        {
            EditorUtility.DisplayDialog("Shop Lock Setup",
                "ShopManager.itemPrefab chưa được gán!\n\nKéo prefab shop item vào field 'Item Prefab' của ShopManager.",
                "OK");
            return;
        }

        // Mở prefab để edit
        string prefabPath = AssetDatabase.GetAssetPath(itemPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Shop Lock Setup",
                "Không tìm thấy đường dẫn prefab trên disk.\n" +
                "Hãy kéo prefab từ Project window vào ShopManager.itemPrefab.",
                "OK");
            return;
        }

        // Edit prefab
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // Kiểm tra đã có ShopLevelLockUI chưa
            var existingLock = prefabRoot.GetComponent<ShopLevelLockUI>();
            if (existingLock != null)
            {
                EditorUtility.DisplayDialog("Shop Lock Setup",
                    "ShopLevelLockUI đã tồn tại trong prefab này!\n\nKhông cần thêm lại.",
                    "OK");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            // Tạo lock overlay child
            var overlayGo = new GameObject("LockOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(prefabRoot.transform, false);

            var overlayRT = overlayGo.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.65f);

            // Lock icon (optional — user có thể kéo sprite sau)
            var lockIconGo = new GameObject("LockIcon", typeof(RectTransform));
            lockIconGo.transform.SetParent(overlayGo.transform, false);
            var lockIconRT = lockIconGo.GetComponent<RectTransform>();
            lockIconRT.anchorMin = new Vector2(0.5f, 0.55f);
            lockIconRT.anchorMax = new Vector2(0.5f, 0.55f);
            lockIconRT.sizeDelta = new Vector2(36, 36);
            lockIconRT.anchoredPosition = Vector2.zero;
            var lockIconImg = lockIconGo.AddComponent<Image>();
            lockIconImg.color = Color.white;
            lockIconImg.raycastTarget = false;

            // Lock text "Mở ở cấp X"
            var lockTextGo = new GameObject("LockLevelText", typeof(RectTransform));
            lockTextGo.transform.SetParent(overlayGo.transform, false);
            var lockTextRT = lockTextGo.GetComponent<RectTransform>();
            lockTextRT.anchorMin = new Vector2(0f, 0.1f);
            lockTextRT.anchorMax = new Vector2(1f, 0.45f);
            lockTextRT.offsetMin = new Vector2(4, 0);
            lockTextRT.offsetMax = new Vector2(-4, 0);
            var lockTxt = lockTextGo.AddComponent<TextMeshProUGUI>();
            lockTxt.text      = "Mở ở cấp 5";
            lockTxt.fontSize  = 18;
            lockTxt.fontStyle = FontStyles.Bold;
            lockTxt.alignment = TextAlignmentOptions.Center;
            lockTxt.color     = Color.white;
            lockTxt.raycastTarget = false;

            // Thêm ShopLevelLockUI và wire references
            var lockUI = prefabRoot.AddComponent<ShopLevelLockUI>();
            var lockSO = new SerializedObject(lockUI);
            lockSO.FindProperty("lockOverlayRoot").objectReferenceValue = overlayGo;
            lockSO.FindProperty("lockIcon").objectReferenceValue        = lockIconImg;
            lockSO.FindProperty("lockLevelText").objectReferenceValue   = lockTxt;
            lockSO.ApplyModifiedProperties();

            // Ẩn overlay mặc định
            overlayGo.SetActive(false);

            // Lưu prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"[ShopLockSetupTool] ✅ Đã thêm ShopLevelLockUI vào prefab: {prefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        EditorUtility.DisplayDialog("Shop Lock Setup",
            "✅ Thêm Lock UI thành công!\n\n" +
            "Tiếp theo:\n" +
            "1. Kéo sprite ổ khóa vào LockIcon.sprite trong prefab (tùy chọn)\n" +
            "2. Mở scene và chạy Tools/Farm Game/Setup Village Orders L1-L6\n" +
            "   để set unlockLevel cho từng item",
            "OK");
    }

    [MenuItem(MENU_REPORT)]
    public static void ReportUnlockLevels()
    {
        var shopManager = Object.FindFirstObjectByType<ShopManager>();
        if (shopManager == null)
        {
            Debug.Log("[ShopLockSetupTool] ShopManager không tìm thấy trong scene.");
            return;
        }

        var so = new SerializedObject(shopManager);
        var allLists = new[] { "seedList", "buildingList", "decorList" };
        var listNames = new[] { "Seeds", "Buildings", "Decor" };

        Debug.Log("═══════════════════════════════════════");
        Debug.Log("[ShopLockSetupTool] SHOP ITEM UNLOCK LEVELS:");
        Debug.Log("═══════════════════════════════════════");

        for (int i = 0; i < allLists.Length; i++)
        {
            var listProp = so.FindProperty(allLists[i]);
            if (listProp == null) continue;
            Debug.Log($"── Tab: {listNames[i]} ({listProp.arraySize} items) ──");

            for (int j = 0; j < listProp.arraySize; j++)
            {
                var elem = listProp.GetArrayElementAtIndex(j);
                var data = elem.objectReferenceValue as BaseItemData;
                if (data == null) continue;
                Debug.Log($"  [{j}] {data.itemName,-25} unlockLevel={GetUnlockLevel(data)}  goldPrice={data.goldPrice}");
            }
        }

        Debug.Log("═══════════════════════════════════════");
        Debug.Log("[ShopLockSetupTool] Xem Console để đọc report.");
    }

    private static int GetUnlockLevel(BaseItemData item)
    {
        if (item == null) return 1;
        var field = item.GetType().GetField("unlockLevel",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(int))
            return Mathf.Max(1, (int)field.GetValue(item));
        return 1;
    }

    [MenuItem(MENU_ADD, true)]
    [MenuItem(MENU_REPORT, true)]
    private static bool Validate() => !EditorApplication.isPlaying;
}
