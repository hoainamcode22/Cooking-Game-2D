#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dựng NỀN khoá cho item shop (mọi tab, gồm Trang trí): thêm ShopLevelLockUI + tạo sẵn
/// child "LockOverlay" (Image tối) + "LockLevelText" (TMP "Mở ở cấp X") và wire tham chiếu.
///
/// Menu: Tools/Farm Game/Setup Shop Lock Overlay
///
/// Sau khi chạy, VIỆC CỦA BẠN (gắn assets):
///   • Gắn sprite ổ khoá vào Image của 'LockOverlay' (hiện để trống màu tối).
///   • Set unlockLevel > 1 cho các asset Trang trí cần khoá.
/// Code ẩn/hiện overlay đã có sẵn (ShopItemUI.Setup → ShopLevelLockUI.Refresh).
/// Tool idempotent — chạy lại không tạo trùng.
/// </summary>
public static class SetupShopLockTool
{
    private const string MENU = "Tools/Farm Game/Setup Shop Lock Overlay";

    [MenuItem(MENU)]
    public static void Setup()
    {
        int done = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path  = AssetDatabase.GUIDToAssetPath(guid);
            var    asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;
            if (asset.GetComponentInChildren<ShopItemUI>(true) == null) continue;

            if (ScaffoldLockOverlay(path)) done++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Shop Lock Overlay",
            $"Đã dựng nền khoá cho {done} prefab item shop.\n\n" +
            "VIỆC CỦA BẠN (gắn assets):\n" +
            "• Gắn sprite ổ khoá vào Image 'LockOverlay'.\n" +
            "• Set unlockLevel > 1 cho asset Trang trí cần khoá.\n\n" +
            "Overlay sẽ tự hiện khi item chưa đủ cấp.",
            "OK");
    }

    [MenuItem(MENU, true)]
    private static bool Validate() => !EditorApplication.isPlaying;

    private static bool ScaffoldLockOverlay(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var shopItem = root.GetComponentInChildren<ShopItemUI>(true);
            if (shopItem == null) return false;
            GameObject host = shopItem.gameObject;

            // Idempotent: đã có overlay → chỉ wire lại cho chắc
            Transform existing = host.transform.Find("LockOverlay");
            if (existing != null)
            {
                WireLock(host, existing.gameObject, existing.GetComponentInChildren<TextMeshProUGUI>(true));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }

            // 1) LockOverlay — Image tối, sprite TRỐNG (bạn gắn sau), phủ kín item
            var overlayGO = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(host.transform, false);
            var orT = (RectTransform)overlayGO.transform;
            orT.anchorMin = Vector2.zero; orT.anchorMax = Vector2.one;
            orT.offsetMin = Vector2.zero; orT.offsetMax = Vector2.zero;
            var orImg = overlayGO.GetComponent<Image>();
            orImg.color = new Color(0f, 0f, 0f, 0.65f);
            orImg.raycastTarget = true;          // chặn click khi đang khoá

            // 2) LockLevelText — TMP "Mở ở cấp X"
            var txtGO = new GameObject("LockLevelText", typeof(RectTransform));
            txtGO.transform.SetParent(overlayGO.transform, false);
            var txT = (RectTransform)txtGO.transform;
            txT.anchorMin = Vector2.zero; txT.anchorMax = Vector2.one;
            txT.offsetMin = Vector2.zero; txT.offsetMax = Vector2.zero;
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Mở ở cấp X";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize  = 28;
            tmp.color     = Color.white;

            // 3) ShopLevelLockUI + wire
            WireLock(host, overlayGO, tmp);

            overlayGO.SetActive(false);          // mặc định ẩn; Refresh() bật khi khoá

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[ShopLock] Dựng nền khoá: {prefabPath}");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void WireLock(GameObject host, GameObject overlay, TextMeshProUGUI tmp)
    {
        var lockUI = host.GetComponent<ShopLevelLockUI>();
        if (lockUI == null) lockUI = host.AddComponent<ShopLevelLockUI>();

        var so = new SerializedObject(lockUI);
        var lor = so.FindProperty("lockOverlayRoot");
        if (lor != null) lor.objectReferenceValue = overlay;
        var lt = so.FindProperty("lockLevelText");
        if (lt != null && tmp != null) lt.objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
