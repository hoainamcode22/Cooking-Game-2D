// =============================================================================
//  LevelUpCharImportTool  —  VÒNG 16
//  ---------------------------------------------------------------------------
//  Nạp art nhân vật popup LÊN CẤP từ thư mục bàn giao của đội vẽ vào Assets,
//  và ép import setting chuẩn Sprite UI.
//
//  Gói đặt hàng vòng 16 (xem production/PROMPT_SPRITE_FORGE_2026-09-04_VONG16_...):
//    A_Char05_NongDan/  → char_05  (nhân vật MỚI, 13 file)
//    B_Char01_VaFrame/  → char_01  (vá 2 file f01 + blink bị lạc bộ)
//
//  AN TOÀN:
//    · Gói B GHI ĐÈ 2 file đang có ⇒ tool tự backup sang production/ trước khi ghi.
//    · Chỉ nhận đúng tên file trong hợp đồng đặt tên — file lạ bị bỏ qua, có báo.
//    · KHÔNG [InitializeOnLoad], KHÔNG delayCall ⇒ chỉ chạy khi bấm menu.
// =============================================================================

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class LevelUpCharImportTool
{
    private const string CHAR_ROOT = "Assets/Art/UI/LevelUpV2/characters";
    private const string HANDOFF   = "production/art-handoff/2026-09-04_VONG16";

    private static readonly (string goi, string charId)[] GOI =
    {
        ("A_Char05_NongDan", "char_05"),
        ("B_Char01_VaFrame", "char_01"),
    };

    // Hợp đồng đặt tên: char_XX_f01..f12 và char_XX_blink
    private static readonly Regex TEN_HOP_LE = new Regex(@"^char_\d{2}_(f(0[1-9]|1[0-2])|blink)\.png$");

    [MenuItem("Tools/Farm Game/Level Up Popup/Nap art nhan vat VONG 16 - DRY RUN", false, 62)]
    public static void DryRun() { Chay(false); }

    [MenuItem("Tools/Farm Game/Level Up Popup/Nap art nhan vat VONG 16 - APPLY", false, 63)]
    public static void Apply() { Chay(true); }

    private static void Chay(bool ghiThat)
    {
        string gocDuAn = Directory.GetParent(Application.dataPath).FullName;
        string backupDir = Path.Combine(gocDuAn, "production", "backup_round16_2026-09-04", "char_ghi_de_tu_dong");

        var bc = new StringBuilder();
        bc.AppendLine("╔══════════ NẠP ART NHÂN VẬT LÊN CẤP — VÒNG 16 ══════════");
        bc.AppendLine($"║ Chế độ: {(ghiThat ? "APPLY (ghi thật)" : "DRY-RUN (không ghi gì)")}");

        int tongChep = 0, tongBoQua = 0, tongDe = 0;
        var canImport = new System.Collections.Generic.List<string>();

        foreach (var (goi, charId) in GOI)
        {
            string nguon = Path.Combine(gocDuAn, HANDOFF.Replace('/', Path.DirectorySeparatorChar), goi);
            bc.AppendLine("║");
            bc.AppendLine($"║ ── Gói {goi}  →  {CHAR_ROOT}/{charId}/");

            if (!Directory.Exists(nguon))
            {
                bc.AppendLine("║    · Chưa có thư mục nguồn — đội vẽ chưa giao, bỏ qua.");
                continue;
            }

            var tatCa = Directory.GetFiles(nguon, "*.png", SearchOption.TopDirectoryOnly);
            var hopLe = tatCa.Where(f =>
            {
                string ten = Path.GetFileName(f);
                return TEN_HOP_LE.IsMatch(ten) && ten.StartsWith(charId + "_", StringComparison.Ordinal);
            }).ToArray();

            foreach (var f in tatCa.Except(hopLe))
            {
                tongBoQua++;
                bc.AppendLine($"║    ⏭ BỎ QUA (sai hợp đồng đặt tên): {Path.GetFileName(f)}");
            }
            if (hopLe.Length == 0) { bc.AppendLine("║    · Không có file hợp lệ."); continue; }

            string dichTuyetDoi = Path.Combine(gocDuAn, CHAR_ROOT.Replace('/', Path.DirectorySeparatorChar), charId);
            if (ghiThat) Directory.CreateDirectory(dichTuyetDoi);

            foreach (var f in hopLe.OrderBy(x => x, StringComparer.Ordinal))
            {
                string ten  = Path.GetFileName(f);
                string dich = Path.Combine(dichTuyetDoi, ten);
                bool deLen  = File.Exists(dich);

                if (deLen)
                {
                    tongDe++;
                    if (ghiThat)
                    {
                        Directory.CreateDirectory(backupDir);
                        File.Copy(dich, Path.Combine(backupDir, ten), true);
                    }
                }

                if (ghiThat) File.Copy(f, dich, true);
                canImport.Add($"{CHAR_ROOT}/{charId}/{ten}");
                tongChep++;
                bc.AppendLine($"║    {(deLen ? "↻ ĐÈ (đã backup)" : "+ mới        ")} {ten}");
            }
        }

        bc.AppendLine("║");
        bc.AppendLine($"║ Tổng: chép {tongChep} · ghi đè {tongDe} · bỏ qua {tongBoQua}");

        if (!ghiThat)
        {
            bc.AppendLine("║ → Chạy lại bằng menu ... APPLY để ghi thật.");
            bc.AppendLine("╚════════════════════════════════════════════════════════");
            Debug.Log(bc.ToString());
            return;
        }

        AssetDatabase.Refresh();

        int soEp = 0;
        foreach (var duongDan in canImport)
            if (EpChuanSpriteUI(duongDan)) soEp++;

        AssetDatabase.SaveAssets();

        bc.AppendLine($"║ Đã ép chuẩn import Sprite UI cho {soEp} file.");
        bc.AppendLine("║ Bước tiếp: chạy 'Nối lại dây popup (DRY-RUN)' rồi (APPLY), sau đó Ctrl+S.");
        bc.AppendLine($"║ Backup file bị đè: production/backup_round16_2026-09-04/char_ghi_de_tu_dong/");
        bc.AppendLine("╚════════════════════════════════════════════════════════");
        Debug.Log(bc.ToString());
    }

    /// <summary>Ép PNG về đúng chuẩn Sprite UI: Single, pivot giữa, không nén mờ.</summary>
    private static bool EpChuanSpriteUI(string duongDan)
    {
        var ti = AssetImporter.GetAtPath(duongDan) as TextureImporter;
        if (ti == null) return false;

        bool doi = false;
        if (ti.textureType     != TextureImporterType.Sprite)       { ti.textureType     = TextureImporterType.Sprite;       doi = true; }
        if (ti.spriteImportMode != SpriteImportMode.Single)         { ti.spriteImportMode = SpriteImportMode.Single;         doi = true; }
        if (ti.alphaIsTransparency != true)                         { ti.alphaIsTransparency = true;                         doi = true; }
        if (ti.mipmapEnabled   != false)                            { ti.mipmapEnabled   = false;                            doi = true; }
        if (ti.filterMode      != FilterMode.Bilinear)              { ti.filterMode      = FilterMode.Bilinear;              doi = true; }
        if (ti.maxTextureSize  < 1024)                              { ti.maxTextureSize  = 1024;                             doi = true; }
        if (ti.textureCompression != TextureImporterCompression.Uncompressed)
        {                                                             ti.textureCompression = TextureImporterCompression.Uncompressed; doi = true; }

        if (!doi) return false;
        ti.SaveAndReimport();
        return true;
    }
}
