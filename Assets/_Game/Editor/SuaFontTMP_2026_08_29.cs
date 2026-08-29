#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SỬA LỖI FONT TMP — 2026-08-29.
///
/// TRIỆU CHỨNG: build xong, vào bếp (SampleScene) là Console/Player đổ hàng loạt
/// `IndexOutOfRangeException` tại `TMP_MaterialManager.GetFallbackMaterial(...)`, bắn từ
/// `ScrollRect.LateUpdate` — đúng cái danh sách 38 món trong sổ công thức. Trong bản build
/// lỗi này lặp mỗi frame, UI bếp không vẽ nổi → treo/văng game.
///
/// GỐC (đã đo): atlas font 1024×1024 (sampling 64, padding 6) chứa được ~169 glyph, trong khi
/// toàn bộ text tiếng Việt của game cần ~298 glyph (riêng ký tự có dấu đã ~207). 20 tên món mới
/// đẩy qua ngưỡng → TMP mở atlas thứ 2 (Multi Atlas) → mảng vật liệu vỡ (bug TMP đã biết).
///
/// CÁCH SỬA (đúng thao tác bấm tay trong Inspector, gói vào 1 nút vì Sếp không tìm thấy font):
///   · Atlas Resolution 1024 → 2048 (chứa ~676 glyph, dư gấp đôi nhu cầu).
///   · TẮT Multi Atlas Textures (hết chỗ thì thiếu chữ — hỏng nhẹ, không văng lỗi).
///   · Clear Dynamic Data (xoá glyph đã nướng, để runtime tự nướng lại vào atlas 2048).
///
/// Áp cho CẢ HAI font của game: FontVo (UI dùng, 234 tham chiếu) + Baloo2 SDF (mặc định TMP).
/// Backup nguyên bản: production/backup_train_2026-08-29/font/
/// </summary>
public static class SuaFontTMP_2026_08_29
{
    private static readonly string[] TargetPaths =
    {
        // 29-08: đã đồng nhất — cả game chỉ còn MỘT font (FontVo đã nghỉ hưu).
        "Assets/_Game/Resources/Fonts/Baloo2 SDF.asset",
    };

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Tối ưu/Font: 1. Liệt kê mọi font TMP (CHỈ ĐỌC)", false, 60)]
    private static void LietKe()
    {
        var sb = new StringBuilder("[Font TMP] Toàn bộ font asset trong dự án:\n");
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f == null) continue;
            var so = new SerializedObject(f);
            sb.AppendLine(string.Format(
                "  {0}\n     atlas {1}x{2} · sampling {3} · multiAtlas {4} · glyph đã nướng {5}",
                path,
                so.FindProperty("m_AtlasWidth").intValue,
                so.FindProperty("m_AtlasHeight").intValue,
                f.faceInfo.pointSize,
                so.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue,
                f.glyphTable != null ? f.glyphTable.Count : 0));
        }
        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Font TMP", "Danh sách đầy đủ đã in ra Console.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ❌ ĐÃ KHOÁ 29-08-2026: nút này gọi ClearFontAssetData(TRUE) — tham số true thu ảnh
    // atlas về 0×0, làm font rỗng hoàn toàn và chữ trong game rụng hàng loạt.
    // Dùng "Font: 3. PHỤC HỒI + NƯỚNG SẴN TIẾNG VIỆT" thay thế.
    // [MenuItem("Tools/Tối ưu/Font: 2. SỬA LỖI VĂNG BẾP — nới atlas 2048 (2 font game)", false, 61)]
    private static void Sua()
    {
        if (!EditorUtility.DisplayDialog("Sửa font",
                "Sẽ sửa font duy nhất của game: Baloo2 SDF.asset (Resources/Fonts)\n\n" +
                "Atlas 1024 → 2048, TẮT Multi Atlas, Clear Dynamic Data.\n" +
                "Chữ trong game KHÔNG đổi kiểu dáng — chỉ nới chỗ chứa.\n\n" +
                "Backup đã có ở production/backup_train_2026-08-29/font/.",
                "Sửa đi", "Huỷ"))
            return;

        var sb = new StringBuilder("[Font TMP] Kết quả sửa:\n");
        int done = 0;
        foreach (string path in TargetPaths)
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f == null) { sb.AppendLine("  BỎ QUA (không thấy): " + path); continue; }

            var so = new SerializedObject(f);
            so.FindProperty("m_AtlasWidth").intValue  = 2048;
            so.FindProperty("m_AtlasHeight").intValue = 2048;
            so.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            // = nút "Clear Dynamic Data" trong Inspector: xoá glyph đã nướng + đưa atlas về
            // trạng thái rỗng để runtime nướng lại theo kích thước mới.
            f.ClearFontAssetData(true);

            EditorUtility.SetDirty(f);
            sb.AppendLine("  ĐÃ SỬA: " + path + " → 2048x2048, multiAtlas OFF, dynamic data cleared");
            done++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Xong",
            done + "/1 font đã sửa.\n\nBấm Play thử vào bếp — 20 lỗi IndexOutOfRange phải sạch.\n" +
            "Sau đó BUILD LẠI thì bản .exe mới hết văng.", "OK");
    }
}
#endif
