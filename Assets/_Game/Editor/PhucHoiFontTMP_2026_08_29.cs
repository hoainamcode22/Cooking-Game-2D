#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PHỤC HỒI + NƯỚNG SẴN FONT TIẾNG VIỆT — 2026-08-29 (bản sửa sai của chính tôi).
///
/// CHUYỆN ĐÃ XẢY RA: công cụ trước gọi `ClearFontAssetData(true)`. Tham số `true` là
/// "setAtlasSizeToZero" — nó KHÔNG chỉ xoá glyph mà còn thu ảnh atlas về 0×0. Font từ
/// 11,6 MB còn 6,5 KB, bảng glyph rỗng, atlas rỗng ⇒ lúc chạy TMP không nhét nổi chữ nào
/// vào tấm ảnh 0×0 nên báo "character not found" với CẢ chữ ASCII thường như 'z'.
/// Đó là lý do chữ trong game rụng lung tung: "NÔNG T I", "Cơm chiên tr ng", "L  chưa nhóm".
///
/// CÁCH CHỮA (khác hẳn lần trước — không còn phụ thuộc việc nướng chữ lúc chạy):
///   1. `ClearFontAssetData(false)` → dựng LẠI ảnh atlas đúng 2048×2048 (false = giữ kích thước).
///   2. `TryAddCharacters(...)` → NƯỚNG SẴN ~300 ký tự (ASCII + toàn bộ tiếng Việt + dấu câu)
///      vào atlas ngay tại Editor. Sức chứa 2048² ở cỡ 64px là ~676 ô — dư gấp đôi.
///   3. Tắt `m_ClearDynamicDataOnBuild` — nếu để bật, Unity sẽ XOÁ SẠCH đống vừa nướng khi
///      build, và bản .exe lại thiếu chữ y như cũ. Đây là cái bẫy phải nhớ.
///   4. Vẫn để Atlas Population = Dynamic + Multi Atlas TẮT: chữ lạ ngoài dự kiến vẫn nướng
///      thêm được vào chỗ trống, mà hết chỗ thì chỉ thiếu chữ chứ không văng lỗi.
///
/// Chạy lại bao nhiêu lần cũng được. Bản gốc 11,6 MB nằm ở
/// production/backup_train_2026-08-29/font/Baloo2 SDF.asset
/// </summary>
public static class PhucHoiFontTMP_2026_08_29
{
    private const string FontPath = "Assets/_Game/Resources/Fonts/Baloo2 SDF.asset";

    /// <summary>
    /// 4096 chứ không phải 2048. LÝ DO ĐO ĐƯỢC (29-08, lần chạy đầu chỉ vào được 101/338 chữ):
    /// font này vẽ glyph ở pointSize 289 + padding 28 ⇒ mỗi chữ chiếm ô ~142×181 px.
    /// Atlas 2048² chỉ chứa nổi ~101 ô là ĐẦY 90% ⇒ toàn bộ chữ tiếng Việt bị loại,
    /// nên trong game chữ rụng dấu và TMP cứ thử-nướng-lại mỗi khung hình.
    /// 4096² cho ~616 ô — thừa cho ~338 chữ, vẫn còn chỗ trống cho chữ lạ phát sinh.
    /// Giá phải trả: atlas Alpha8 4096² = 16 MB. Muốn nhẹ hơn thì phải dựng lại font ở
    /// pointSize ~64 bằng Font Asset Creator (đổi cả face metrics) — việc khác, làm sau.
    /// </summary>
    private const int ATLAS = 4096;

    /// <summary>Bộ ký tự cần nướng. Dải Unicode, không gõ tay để khỏi sót.</summary>
    private static string BuildCharset()
    {
        var sb = new StringBuilder();
        void Range(int a, int b) { for (int c = a; c <= b; c++) sb.Append((char)c); }

        Range(0x0020, 0x007E);   // ASCII in được: chữ, số, dấu câu

        // ── Latin-1: CHỈ lấy 45 ký tự game thật sự dùng, KHÔNG lấy cả dải 0xA0–0xFF ──
        // Lý do (đo 29-08): atlas 4096 đầy 95% mà vẫn rớt ỹ Ỷ Ỹ Ỵ. Quét toàn dự án
        // (code + scene + prefab + tên món/vật phẩm) thấy game chỉ đụng 45/96 ký tự dải này.
        // Bỏ 51 ký tự chết (¢£¤¥¦ª¬µ¶¹¼½¾ØÞß…) là dư chỗ cho trọn bộ tiếng Việt.
        sb.Append("¡§©®°±²·º»ÀÁÂÃÆÈÉÊÌÍÒÓÔÕ×ÙÚÜÝàáâãèéêìíòóôõ÷ùúý");

        sb.Append("ĂăĐđĨĩŨũƠơƯư");   // 12 ký tự Việt nằm rải rác, lấy đích danh

        Range(0x1EA0, 0x1EF9);   // TOÀN dải tiếng Việt mở rộng — 90 ký tự, không cắt cái nào

        // Dấu câu game thật sự dùng (đã lọc: ★ ✅ 🌾 🔒… Baloo2 không có, nướng cũng trượt)
        sb.Append("–—‘’“”•…‹›₫");

        return sb.ToString();
    }

    [MenuItem("Tools/Tối ưu/Font: 3. PHỤC HỒI + NƯỚNG SẴN TIẾNG VIỆT (chạy cái này)", false, 62)]
    private static void PhucHoi()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không thấy font ở:\n" + FontPath, "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Phục hồi font tiếng Việt",
                "Sẽ dựng lại atlas 4096×4096 và NƯỚNG SẴN toàn bộ ký tự tiếng Việt vào font.\n\n" +
                "Sau bước này chữ trong game không còn phụ thuộc việc nướng lúc chạy —\n" +
                "hết cảnh rụng chữ, và bản build cũng đủ chữ.\n\n" +
                "Mất khoảng 10–30 giây.",
                "Làm đi", "Huỷ"))
            return;

        try
        {
            EditorUtility.DisplayProgressBar("Phục hồi font", "Dựng lại atlas 2048×2048...", 0.2f);

            var so = new SerializedObject(font);
            so.FindProperty("m_AtlasWidth").intValue  = ATLAS;
            so.FindProperty("m_AtlasHeight").intValue = ATLAS;
            // creationSettings phải khớp, nếu không TMP vẫn nướng theo số cũ.
            var cs = so.FindProperty("m_CreationSettings");
            if (cs != null)
            {
                var cw = cs.FindPropertyRelative("atlasWidth");
                var ch = cs.FindPropertyRelative("atlasHeight");
                if (cw != null) cw.intValue = ATLAS;
                if (ch != null) ch.intValue = ATLAS;
            }
            so.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = false;
            // BẮT BUỘC: bật cờ này thì Unity xoá sạch glyph vừa nướng lúc build ⇒ .exe thiếu chữ.
            so.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            // false = GIỮ kích thước atlas (2048), chỉ dọn glyph cũ.
            // (Lần trước truyền true — chính chỗ này đã thu atlas về 0×0.)
            font.ClearFontAssetData(false);

            EditorUtility.DisplayProgressBar("Phục hồi font", "Đang nướng ký tự tiếng Việt...", 0.6f);

            string charset = BuildCharset();
            string missing;
            font.TryAddCharacters(charset, out missing);

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Kiểm ngay tại chỗ: thiếu chữ Việt là báo ĐỎ, không để lọt xuống build rồi mới biết.
            var thieuViet = new List<char>();
            const string VN_THUONG = "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ";
            // Lần trước chỉ kiểm chữ THƯỜNG nên báo "thiếu 1" trong khi thật ra thiếu 4 (ỹ Ỷ Ỹ Ỵ).
            foreach (char c in VN_THUONG + VN_THUONG.ToUpperInvariant())
                if (!font.HasCharacter(c)) thieuViet.Add(c);

            int baked = font.characterTable != null ? font.characterTable.Count : 0;
            int glyphs = font.glyphTable != null ? font.glyphTable.Count : 0;

            var sb = new StringBuilder();
            sb.AppendLine("[Font] PHỤC HỒI XONG");
            sb.AppendLine("   yêu cầu nướng : " + charset.Length + " ký tự");
            sb.AppendLine("   đã vào font   : " + baked + " ký tự / " + glyphs + " glyph");
            sb.AppendLine("   atlas         : " + ATLAS + "x" + ATLAS + " · multiAtlas OFF · clearOnBuild OFF");
            if (!string.IsNullOrEmpty(missing))
            {
                sb.AppendLine("   KHÔNG có trong Baloo2.ttf (bỏ qua được, phần lớn là ký hiệu lạ):");
                sb.AppendLine("   " + missing);
            }
            else sb.AppendLine("   không thiếu ký tự nào.");

            if (thieuViet.Count > 0)
            {
                sb.AppendLine("   ❌ VẪN THIẾU " + thieuViet.Count + " CHỮ VIỆT: " + new string(thieuViet.ToArray()));
                sb.AppendLine("      ⇒ atlas vẫn chật. Báo lại để nới tiếp hoặc hạ pointSize.");
                Debug.LogError(sb.ToString());
            }
            else Debug.Log(sb.ToString());

            EditorUtility.DisplayDialog(thieuViet.Count == 0 ? "Xong — ĐỦ CHỮ" : "CHƯA ĐỦ CHỮ",
                "Đã nướng " + baked + "/" + charset.Length + " ký tự vào font.\n\n" +
                (thieuViet.Count == 0
                    ? "Đã kiểm: có ĐỦ toàn bộ chữ tiếng Việt.\nGiờ bấm Play, rồi BUILD LẠI."
                    : "VẪN THIẾU " + thieuViet.Count + " chữ Việt — xem Console (dòng đỏ).") ,
                "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
#endif
