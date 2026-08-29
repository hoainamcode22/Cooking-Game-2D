using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// TOOL SỬA TRIỆT ĐỂ LỖI MẤT DẤU TIẾNG VIỆT TRONG TOÀN BỘ GAME (Bếp, Nông Trại, Menu, Sổ Công Thức):
/// Nguyên nhân: FontAsset 'Baloo2 SDF' bị nướng (bake) ở dạng Static thiếu ký tự tiếng Việt.
/// Tool này:
///  1. Tạo lại FontAsset 'Baloo2 SDF' và 'FontVo' ở chế độ DYNAMIC 100% (tự động nạp mọi ký tự tiếng Việt lúc chạy).
///  2. Thêm Fallback font an toàn (LiberationSans SDF).
///  3. Gán đè font vào TMP_Settings làm font mặc định cho toàn bộ dự án.
///  4. Quét và đồng bộ toàn bộ TextMeshProUGUI trong scene đang mở (SampleScene, SCN_Farm, SCN_Home).
/// </summary>
public static class FixVietnameseFontTool
{
    private const string FontTtfPath     = "Assets/_Game/Fonts/Baloo2.ttf";
    private const string FontAssetFolder = "Assets/_Game/Resources/Fonts";
    private const string Baloo2AssetPath = "Assets/_Game/Resources/Fonts/Baloo2 SDF.asset";
    private const string FontVoAssetPath = "Assets/_Game/Resources/Fonts/FontVo.asset";

    [MenuItem("Tools/Sửa Lỗi Text Tiếng Việt (Tạo lại Font Dynamic Baloo2)", false, -100)]
    public static void FixAllVietnameseFonts()
    {
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
        if (ttf == null)
        {
            // Tìm bất kỳ font .ttf nào trong Assets/_Game/Fonts
            string[] files = Directory.GetFiles("Assets/_Game/Fonts", "*.ttf");
            if (files.Length > 0)
                ttf = AssetDatabase.LoadAssetAtPath<Font>(files[0].Replace('\\', '/'));
        }

        if (ttf == null)
        {
            EditorUtility.DisplayDialog("Lỗi Font", $"Không tìm thấy file font .ttf trong Assets/_Game/Fonts/", "OK");
            return;
        }

        Directory.CreateDirectory(FontAssetFolder);

        // 1. Tạo FontAsset Dynamic Baloo2 SDF
        TMP_FontAsset dynamicBaloo = CreateDynamicFontAsset(ttf, "Baloo2 SDF", Baloo2AssetPath);

        // 2. Tạo FontAsset Dynamic FontVo (đồng bộ)
        TMP_FontAsset dynamicFontVo = CreateDynamicFontAsset(ttf, "FontVo", FontVoAssetPath);

        // 3. Fallback an toàn
        var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (liberation != null)
        {
            if (dynamicBaloo.fallbackFontAssetTable == null) dynamicBaloo.fallbackFontAssetTable = new List<TMP_FontAsset>();
            dynamicBaloo.fallbackFontAssetTable.Clear();
            dynamicBaloo.fallbackFontAssetTable.Add(liberation);

            if (dynamicFontVo.fallbackFontAssetTable == null) dynamicFontVo.fallbackFontAssetTable = new List<TMP_FontAsset>();
            dynamicFontVo.fallbackFontAssetTable.Clear();
            dynamicFontVo.fallbackFontAssetTable.Add(liberation);
        }

        EditorUtility.SetDirty(dynamicBaloo);
        EditorUtility.SetDirty(dynamicFontVo);

        // 4. Đặt làm font mặc định toàn dự án
        var settings = TMP_Settings.instance;
        if (settings != null)
        {
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = dynamicBaloo;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }

        // 5. Đồng bộ tất cả TextMeshPro trong Scene hiện tại
        int count = 0;
        foreach (var txt in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (txt == null) continue;
            Undo.RecordObject(txt, "Fix Vietnamese Font");
            txt.font = dynamicBaloo;
            EditorUtility.SetDirty(txt);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"[FixFont] ✅ ĐÃ SỬA XONG TRIỆT ĐỂ LỖI TIẾNG VIỆT: Đã tạo Font DYNAMIC cho 'Baloo2 SDF' và 'FontVo', cập nhật {count} Text objects trong scene. Sếp chỉ cần Save Scene (Ctrl + S) và bấm Play!");
        EditorUtility.DisplayDialog("Thành Công!", $"Đã chuyển font 'Baloo2 SDF' và 'FontVo' sang chế độ DYNAMIC hoàn toàn.\nĐã đồng bộ {count} text trong scene hiện tại.\n\nNhớ bấm Ctrl + S để lưu scene nhé Sếp!", "OK");
    }

    private static TMP_FontAsset CreateDynamicFontAsset(Font ttf, string assetName, string savePath)
    {
        AssetDatabase.DeleteAsset(savePath);

        var fa = TMP_FontAsset.CreateFontAsset(
            ttf, 72, 6, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);

        fa.name = assetName;
        AssetDatabase.CreateAsset(fa, savePath);

        if (fa.material != null)
        {
            fa.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }

        if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
        {
            fa.atlasTextures[0].name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        }

        return fa;
    }
}
