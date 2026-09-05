using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// NẠP ART GÓI TỔNG (A · B · C · E) — 1 nút.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// Gói D (miệng NPC) do `TutorialV2SetupTool ▸ ★ Nạp art NPC + VFX` lo — tool này KHÔNG đụng.
///
/// VIỆC TOOL LÀM:
///   ① Copy PNG từ `production/art-handoff/2026-09-04_MASTER/` vào đúng thư mục trong Assets
///   ② Set import chuẩn Sprite (Single, alpha, không mipmap)
///   ③ **SET 9-SLICE BORDER** cho 3 file gói E — đây là thứ KHÔNG thể để Sếp kéo tay:
///      sai border một chút là khung co giãn ra méo viền, mà nhìn mắt thường không phát hiện được.
///
/// Chạy lại nhiều lần vẫn ra một kết quả. Không tự lưu scene (luật studio).
///
/// [MasterArt]
/// </summary>
public static class MasterArtImportTool
{
    private const string MENU = "Tools/Farm Game/★ Nạp art gói TỔNG A·B·C·E (1 nút)";
    private const string GOC   = "production/art-handoff/2026-09-04_MASTER";

    /// <summary>Một file cần nạp: nguồn → đích, kèm border 9-slice nếu có.</summary>
    private struct Mon
    {
        public string Goi, TenFile, ThuMucNguon, ThuMucDich;
        public Vector4 Border;          // (left, bottom, right, top) — Vector4.zero = không 9-slice
    }

    private static readonly Mon[] DANH_SACH =
    {
        // ── GÓI A — cờ ngôn ngữ ──────────────────────────────────────────────
        M("A", "flag_vn.png", "A_Co_NgonNgu", "Assets/Art/UI/Settings"),
        M("A", "flag_en.png", "A_Co_NgonNgu", "Assets/Art/UI/Settings"),

        // ── GÓI B — icon gia vị ──────────────────────────────────────────────
        M("B", "ing_rau.png",         "B_Icon_GiaVi", "Assets/Art/UI/Ingredients"),
        M("B", "ing_nuoc_mam.png",    "B_Icon_GiaVi", "Assets/Art/UI/Ingredients"),
        M("B", "ing_nuoc_tuong.png",  "B_Icon_GiaVi", "Assets/Art/UI/Ingredients"),

        // ── GÓI C — 2 nhân vật lên cấp (vào ĐÚNG thư mục char_0N cũ để tool
        //    ★ Nối lại dây popup tìm thấy mà không phải sửa gì) ────────────────
        M("C", "char_03_master.png", "C_Char_LenCap", "Assets/Art/UI/LevelUpV2/characters/char_03"),
        M("C", "char_04_master.png", "C_Char_LenCap", "Assets/Art/UI/LevelUpV2/characters/char_04"),

        // ── GÓI E — khung Guide Board (3 file 9-SLICE + 2 chấm thường) ───────
        M("E", "tut_board_frame.png",        "E_Tutorial_GuideBoard", "Assets/Art/UI/TutorialV2/board", new Vector4(72, 72, 72, 72)),
        M("E", "tut_board_ribbon.png",       "E_Tutorial_GuideBoard", "Assets/Art/UI/TutorialV2/board", new Vector4(60,  0, 60,  0)),
        M("E", "tut_slot_illustration.png",  "E_Tutorial_GuideBoard", "Assets/Art/UI/TutorialV2/board", new Vector4(40, 40, 40, 40)),
        M("E", "tut_step_dot_on.png",        "E_Tutorial_GuideBoard", "Assets/Art/UI/TutorialV2/board"),
        M("E", "tut_step_dot_off.png",       "E_Tutorial_GuideBoard", "Assets/Art/UI/TutorialV2/board"),
    };

    private static Mon M(string goi, string ten, string nguon, string dich, Vector4 border = default)
        => new Mon { Goi = goi, TenFile = ten, ThuMucNguon = nguon, ThuMucDich = dich, Border = border };

    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem(MENU, false, 5)]
    private static void Chay()
    {
        var bc = new StringBuilder();
        bc.AppendLine("╔══ [MasterArt] NẠP GÓI TỔNG A · B · C · E ══");

        string gocDuAn = Directory.GetParent(Application.dataPath).FullName;
        var daCopy = new List<Mon>();
        int thieu = 0;

        string goiHienTai = "";
        foreach (var m in DANH_SACH)
        {
            if (m.Goi != goiHienTai)
            {
                goiHienTai = m.Goi;
                bc.AppendLine("║");
                bc.AppendLine($"║ ── GÓI {m.Goi} → {m.ThuMucDich}");
            }

            string nguon = Path.Combine(gocDuAn, GOC, m.ThuMucNguon, m.TenFile);
            if (!File.Exists(nguon))
            {
                thieu++;
                bc.AppendLine($"║    ⏭ {m.TenFile} — CHƯA CÓ trong art-handoff, bỏ qua");
                continue;
            }

            if (!AssetDatabase.IsValidFolder(m.ThuMucDich)) TaoThuMuc(m.ThuMucDich);

            try
            {
                File.Copy(nguon, Path.Combine(gocDuAn, m.ThuMucDich, m.TenFile), true);
                daCopy.Add(m);
                bc.AppendLine($"║    ✔ {m.TenFile}");
            }
            catch (System.Exception e)
            {
                thieu++;
                bc.AppendLine($"║    ✖ {m.TenFile}: {e.Message}");
            }
        }

        if (daCopy.Count == 0)
        {
            bc.AppendLine("║");
            bc.AppendLine("║ ✋ Không copy được file nào — kiểm lại thư mục art-handoff.");
            bc.AppendLine("╚════════════════════════════════════════════════════════════");
            Debug.LogWarning(bc.ToString());
            return;
        }

        AssetDatabase.Refresh();

        // ── Set import + border ─────────────────────────────────────────────
        bc.AppendLine("║");
        bc.AppendLine("║ ── Chỉnh import + 9-slice border");
        int soBorder = 0;

        foreach (var m in daCopy)
        {
            string path = m.ThuMucDich + "/" + m.TenFile;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { bc.AppendLine($"║    ⚠ {m.TenFile}: chưa import xong, bấm lại nút này"); continue; }

            bool doi = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; doi = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; doi = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; doi = true; }
            if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; doi = true; }
            if (imp.maxTextureSize < 1024) { imp.maxTextureSize = 1024; doi = true; }

            if (m.Border != Vector4.zero && imp.spriteBorder != m.Border)
            {
                imp.spriteBorder = m.Border;
                doi = true;
                soBorder++;
                bc.AppendLine($"║    ✔ {m.TenFile} · border {m.Border.x},{m.Border.y},{m.Border.z},{m.Border.w} (9-slice)");
            }

            if (doi) imp.SaveAndReimport();
        }

        AssetDatabase.Refresh();

        bc.AppendLine("║");
        bc.AppendLine($"║ TỔNG: copy {daCopy.Count} file · set border {soBorder} file · thiếu {thieu}");
        bc.AppendLine("║");
        bc.AppendLine("║ ⓘ VIỆC TIẾP THEO (tool khác lo, KHÔNG phải nút này):");
        bc.AppendLine("║   • Gói C  → chạy 'Level Up Popup ▸ ★ Nối lại dây popup' để gắn char_03/04 vào slot");
        bc.AppendLine("║   • Gói D  → chạy 'Tutorial V2 ▸ ★ Nạp art NPC + VFX' để lấy 12 frame miệng mới");
        bc.AppendLine("║   • Gói A/E → art đã vào Assets, phần wire vào UI Lead làm ở vòng sau");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");
        Debug.Log(bc.ToString());
    }

    private static void TaoThuMuc(string duongDan)
    {
        var phan = duongDan.Split('/');
        string dang = phan[0];
        for (int i = 1; i < phan.Length; i++)
        {
            string tiep = dang + "/" + phan[i];
            if (!AssetDatabase.IsValidFolder(tiep)) AssetDatabase.CreateFolder(dang, phan[i]);
            dang = tiep;
        }
    }
}
