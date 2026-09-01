#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [V3.2] MENU: Tools/Farm Game/Tourist Boat/
///   • "Thêm hiệu ứng THỞ cho khách (11 prefab)" — gắn (hoặc LÀM MỚI) NpcBreathingIdle
///     trên các prefab Tourist_NV01..11 VÀ đổ sẵn frame idle thật của từng NPC
///     (down_1..3 = đổi chân trụ, left_1/right_1 = ngoái nhìn) từ
///     Assets/NV_NPC/NVGAME/Processed/NVxx/ → khách đứng chờ CỬA QUẬY như người thật,
///     hết cảnh "tấm ảnh phập phồng". Prefab đã có component vẫn được ĐỔ LẠI FRAME
///     (idempotent — chạy lại sau khi update code là ăn bản mới).
///   • "GỠ hiệu ứng THỞ khỏi khách (hoàn tác)" — remove component khỏi 11 prefab.
///
/// Ghi vào prefab qua PrefabUtility, có hộp thoại xác nhận; KHÔNG đụng scene/manager.
/// </summary>
public static class TouristBreathingSetupTool
{
    private const string MENU_ADD    = "Tools/Farm Game/Tourist Boat/Thêm hiệu ứng THỞ cho khách (11 prefab)";
    private const string MENU_REMOVE = "Tools/Farm Game/Tourist Boat/GỠ hiệu ứng THỞ khỏi khách (hoàn tác)";
    private const string PREFAB_DIR  = "Assets/_Game/Farm/Prefabs/Tourists";
    private const string SPRITE_ROOT = "Assets/NV_NPC/NVGAME/Processed";

    [MenuItem(MENU_ADD)]
    public static void AddBreathing()
    {
        if (!EditorUtility.DisplayDialog("Idle sống động cho khách (V3.2)",
            "Gắn/làm mới NpcBreathingIdle + đổ frame idle thật (đổi chân, ngoái nhìn)\n" +
            "vào các prefab Tourist_NV trong:\n" + PREFAB_DIR +
            "\n\nPrefab sẽ được GHI LẠI (có menu GỠ để hoàn tác). Tiếp tục?", "Làm", "Thôi"))
            return;
        Run(add: true);
    }

    [MenuItem(MENU_REMOVE)]
    public static void RemoveBreathing()
    {
        if (!EditorUtility.DisplayDialog("Gỡ hiệu ứng THỞ",
            "Gỡ NpcBreathingIdle khỏi các prefab Tourist_NV (hoàn tác về như cũ)?", "Gỡ", "Thôi"))
            return;
        Run(add: false);
    }

    private static void Run(bool add)
    {
        var report = new StringBuilder();
        report.AppendLine(add ? "═══════ GẮN/LÀM MỚI NpcBreathingIdle V3.2 (kèm frame idle) ═══════"
                              : "═══════ GỠ NpcBreathingIdle khỏi prefab khách ═══════");

        string[] guids = AssetDatabase.FindAssets("t:Prefab Tourist_NV", new[] { PREFAB_DIR });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[TouristBreathing] Khong tim thay prefab Tourist_NV* trong {PREFAB_DIR}.");
            EditorUtility.DisplayDialog("Idle khách", "Khong tim thay prefab Tourist_NV* — xem Console.", "OK");
            return;
        }

        int changed = 0, skipped = 0, failed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                var comp = root.GetComponent<NpcBreathingIdle>();

                if (!add)
                {
                    if (comp == null) { skipped++; report.AppendLine($"  = {path} (không có, bỏ qua)"); }
                    else
                    {
                        Object.DestroyImmediate(comp, true);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changed++; report.AppendLine($"  ✓ {path} (đã gỡ)");
                    }
                    continue;
                }

                if (comp == null) comp = root.AddComponent<NpcBreathingIdle>();

                // "Tourist_NV07" → "NV07" → đổ frame idle thật
                string nvId = root.name.Replace("Tourist_", "");
                int soFrame = DoFrames(comp, nvId, report);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed++;
                report.AppendLine($"  ✓ {path} — {soFrame} frame idle ({nvId})");
            }
            catch (System.Exception e)
            {
                failed++; report.AppendLine($"  ✗ {path}: {e.Message}");
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        report.AppendLine($"─── Kết quả: {(add ? "gắn/làm mới" : "gỡ")} {changed}, bỏ qua {skipped}, lỗi {failed} / {guids.Length} prefab. ───");
        if (add) report.AppendLine("Play Mode → đợi thuyền cập bến: khách đứng chờ sẽ đổi chân trụ, ngoái nhìn quanh, thi thoảng nhún vai.");
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Idle khách",
            $"{(add ? "Gắn/làm mới" : "Gỡ")} {changed}, bỏ qua {skipped}, lỗi {failed} / {guids.Length} prefab.\nXem report trong Console.", "OK");
    }

    /// <summary>Đổ down_1..3 + left_1 + right_1 của NVxx vào component. Trả về số frame gán được.</summary>
    private static int DoFrames(NpcBreathingIdle comp, string nvId, StringBuilder report)
    {
        var so = new SerializedObject(comp);
        int ok = 0;

        var downProp = so.FindProperty("downFrames");
        if (downProp != null)
        {
            downProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                Sprite s = LoadSprite($"{SPRITE_ROOT}/{nvId}/{nvId}_down_{i + 1}.png");
                downProp.GetArrayElementAtIndex(i).objectReferenceValue = s;
                if (s != null) ok++;
            }
        }

        var leftProp  = so.FindProperty("lookLeftFrame");
        var rightProp = so.FindProperty("lookRightFrame");
        Sprite l = LoadSprite($"{SPRITE_ROOT}/{nvId}/{nvId}_left_1.png");
        Sprite r = LoadSprite($"{SPRITE_ROOT}/{nvId}/{nvId}_right_1.png");
        if (leftProp  != null) { leftProp.objectReferenceValue  = l; if (l != null) ok++; }
        if (rightProp != null) { rightProp.objectReferenceValue = r; if (r != null) ok++; }

        so.ApplyModifiedPropertiesWithoutUndo();
        if (ok == 0)
            report.AppendLine($"    [WARN] {nvId}: không tìm thấy sprite nào trong {SPRITE_ROOT}/{nvId}/ — rơi về chế độ thở thuần.");
        return ok;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
#endif
