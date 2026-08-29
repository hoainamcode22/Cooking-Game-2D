using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// ĐỒNG BỘ FONT TOÀN GAME (lệnh Sếp 2026-08-26): font mới Baloo2 (tròn, thân thiện, đủ tiếng Việt),
/// chữ TO hơn một chút và ĐẬM hơn.
///
/// Chạy 1 menu duy nhất cho TỪNG SCENE (SampleScene, SCN_Farm, SCN_Home):
///   Tools → Farm Game → Font → Đồng bộ font scene hiện tại (Baloo2 + to + đậm)
/// Lần đầu chạy sẽ tự: tạo TMP FontAsset dạng DYNAMIC (glyph tiếng Việt nạp tự động, không lo ô vuông)
/// + đặt làm font MẶC ĐỊNH toàn project (mọi text tạo bằng code từ nay tự dùng font này).
/// </summary>
public static class FontSyncTool
{
    private const string FontTtfPath   = "Assets/_Game/Fonts/Baloo2.ttf";
    private const string FontAssetPath = "Assets/_Game/Resources/Fonts/Baloo2 SDF.asset"; // 29-08: đã dọn về Resources khi đồng nhất font

    // ── Chỉnh 2 số này nếu Sếp muốn to/đậm hơn nữa ──
    private const float SizeMultiplier = 1.12f; // chữ to hơn 12%
    private const bool  AddBold        = true;  // in đậm toàn bộ

    [MenuItem("Tools/Farm Game/Font/Đồng bộ font scene hiện tại (Baloo2 + to + đậm)")]
    public static void SyncCurrentScene()
    {
        var fontAsset = EnsureFontAsset();
        if (fontAsset == null) return;

        SetProjectDefault(fontAsset);

        int changed = 0;
        foreach (var txt in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (txt == null) continue;

            // CHỐNG PHÌNH: text đã là Baloo2 (đã đồng bộ lần trước) → bỏ qua,
            // chạy menu bao nhiêu lần cũng không bị nhân cỡ chữ ×1.12 chồng lên nhau.
            if (txt.font == fontAsset) continue;

            Undo.RecordObject(txt, "Sync Font");

            txt.font = fontAsset;

            // TO hơn: text thường nhân fontSize; text auto-size nhân dải min-max
            if (txt.enableAutoSizing)
            {
                txt.fontSizeMin *= SizeMultiplier;
                txt.fontSizeMax *= SizeMultiplier;
            }
            else
            {
                txt.fontSize *= SizeMultiplier;
            }

            if (AddBold)
                txt.fontStyle |= FontStyles.Bold;

            EditorUtility.SetDirty(txt);
            changed++;
        }

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[FontSync] XONG ✔ Đổi {changed} text trong scene sang Baloo2, cỡ ×{SizeMultiplier}, " +
                  $"{(AddBold ? "in đậm" : "không đậm")}. NHỚ SAVE SCENE (Ctrl+S). " +
                  "Chạy lại menu này ở TỪNG scene còn lại (SCN_Farm, SCN_Home...).");
    }

    /// <summary>Tạo TMP FontAsset dynamic từ Baloo2.ttf nếu chưa có — tự nạp glyph tiếng Việt khi cần.</summary>
    private static TMP_FontAsset EnsureFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
        if (ttf == null)
        {
            Debug.LogError($"[FontSync] Không thấy font tại {FontTtfPath} — kiểm tra lại file Sếp tải.");
            return null;
        }

        var fa = TMP_FontAsset.CreateFontAsset(
            ttf, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);

        if (fa == null)
        {
            Debug.LogError("[FontSync] Tạo FontAsset thất bại.");
            return null;
        }

        fa.name = "Baloo2 SDF";
        AssetDatabase.CreateAsset(fa, FontAssetPath);

        if (fa.material != null)
        {
            fa.material.name = "Baloo2 SDF Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }
        if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
        {
            fa.atlasTextures[0].name = "Baloo2 SDF Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontAssetPath);
        Debug.Log($"[FontSync] Đã tạo {FontAssetPath} (Dynamic — tiếng Việt tự nạp, không ô vuông).");
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    }

    /// <summary>Đặt Baloo2 làm font mặc định TMP toàn project — text tạo runtime tự ăn theo.</summary>
    private static void SetProjectDefault(TMP_FontAsset fontAsset)
    {
        var settings = TMP_Settings.instance;
        if (settings == null)
        {
            Debug.LogWarning("[FontSync] Không thấy TMP Settings — bỏ qua bước đặt mặc định.");
            return;
        }

        var so = new SerializedObject(settings);
        var prop = so.FindProperty("m_defaultFontAsset");
        if (prop != null && prop.objectReferenceValue != fontAsset)
        {
            prop.objectReferenceValue = fontAsset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[FontSync] Baloo2 = font MẶC ĐỊNH toàn project.");
        }
    }
}
