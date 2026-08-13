using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// TẠO FONT CHỮ CHO VỎ POPUP — mấu chốt "giống mock".
///
/// Mock của designer dùng Baloo 2 (chữ tròn mập); Unity đang chạy LiberationSans
/// (mảnh, lạnh) nên cùng màu cùng khung vẫn khác một trời một vực. Tool này:
///   1. Đọc MỌI file .ttf trong `Assets/_Game/Fonts/`
///   2. Tạo TMP FontAsset chế độ DYNAMIC (tự nạp glyph lúc chạy → dấu tiếng Việt
///      không cần bake trước)
///   3. File đầu tiên (theo tên A→Z) thành `Resources/Fonts/FontVo` — các vỏ tự
///      nhận; file còn lại + font mặc định cũ xếp vào hàng FALLBACK (thiếu glyph
///      nào thì rơi xuống lớp dưới, không bao giờ ra ô vuông □).
///
/// Chỉ tạo ASSET MỚI — không đổi font mặc định của project, không đụng text nào
/// ngoài các popup đã mặc vỏ.
/// </summary>
public static class TaoFontVoTool
{
    private const string ThuMucTtf = "Assets/_Game/Fonts";
    private const string ThuMucRa  = "Assets/_Game/Resources/Fonts";

    [MenuItem("Tools/Farm/Thay Áo Popup/0 · Tạo font chữ vỏ (đọc Assets/_Game/Fonts)", false, 0)]
    public static void Tao()
    {
        if (!Directory.Exists(ThuMucTtf) || Directory.GetFiles(ThuMucTtf, "*.ttf").Length == 0)
        {
            EditorUtility.DisplayDialog("Font vỏ",
                $"Chưa có file .ttf nào trong {ThuMucTtf}.\nBỏ font vào đó rồi chạy lại menu này.", "OK");
            return;
        }

        Directory.CreateDirectory(ThuMucRa);

        string[] cacTtf = Directory.GetFiles(ThuMucTtf, "*.ttf");
        System.Array.Sort(cacTtf);

        TMP_FontAsset chinh = null;
        var phu = new List<TMP_FontAsset>();

        foreach (string duongGoc in cacTtf)
        {
            string duong = duongGoc.Replace('\\', '/');
            var font = AssetDatabase.LoadAssetAtPath<Font>(duong);
            if (font == null) { Debug.LogWarning($"[FontVỏ] Không nạp được {duong}"); continue; }

            var fa = TMP_FontAsset.CreateFontAsset(font, 80, 8, GlyphRenderMode.SDFAA,
                                                   1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa == null) { Debug.LogWarning($"[FontVỏ] Tạo asset thất bại: {duong}"); continue; }

            string tenAsset = chinh == null
                ? "FontVo"
                : "FontVo_" + Path.GetFileNameWithoutExtension(duong);
            string duongRa = $"{ThuMucRa}/{tenAsset}.asset";

            AssetDatabase.DeleteAsset(duongRa);   // chạy lại menu = build lại sạch
            fa.name = tenAsset;
            AssetDatabase.CreateAsset(fa, duongRa);
            if (fa.material != null)
            {
                fa.material.name = tenAsset + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            if (fa.atlasTexture != null)
            {
                fa.atlasTexture.name = tenAsset + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            }

            if (chinh == null) chinh = fa; else phu.Add(fa);
        }

        if (chinh == null)
        {
            EditorUtility.DisplayDialog("Font vỏ", "Không tạo được font nào — xem Console.", "OK");
            return;
        }

        // Hàng fallback: các ttf phụ (vd bản subset tiếng Việt) → font mặc định cũ.
        if (chinh.fallbackFontAssetTable == null)
            chinh.fallbackFontAssetTable = new List<TMP_FontAsset>();
        chinh.fallbackFontAssetTable.Clear();
        foreach (var f in phu) chinh.fallbackFontAssetTable.Add(f);
        if (TMP_Settings.defaultFontAsset != null)
            chinh.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);

        EditorUtility.SetDirty(chinh);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FontVỏ] ✅ Tạo {1 + phu.Count} font asset. Chính: {ThuMucRa}/FontVo.asset " +
                  $"(fallback: {phu.Count} phụ + font mặc định). Play lại — các popup đã mặc vỏ " +
                  "tự đổi font, không cần chạy thêm gì.");
    }
}
