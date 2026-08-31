#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Tourist Boat/Xuất bảng thưởng khách (38 món)
///
/// Quét MỌI asset <see cref="DishData"/> trong project rồi ghi ra file markdown
/// <c>production/session-state/BANG_THUONG_KHACH_DU_LICH.md</c>: mỗi món 1 dòng gồm
/// difficulty · unlockLevel · sellPrice · vàng khách trả · EXP khách trả · so sánh với
/// công thức CŨ (Σ giá nguyên liệu × rewardIngredientMultiplier) · cờ fallback.
///
/// VÌ SAO CẦN TOOL NÀY: bảng thưởng phải khớp TỪNG con số trên asset thật. Người viết
/// code (tôi) không có 38 file DishData trong sandbox nên không được phép chép tay —
/// bịa số vào bảng cân bằng là lỗi nặng hơn thiếu bảng. Tool chạy trên máy Sếp, đọc
/// asset thật, gọi ĐÚNG <see cref="TouristRewardCalculator"/> mà game dùng ⇒ bảng luôn
/// đúng với build hiện tại, chạy lại được sau mỗi lần tuning.
///
/// Cột "vàng CŨ" chỉ để đối chiếu mức lạm phát/giảm phát. Nó gọi BasePriceBook nên cần
/// có provider giá đăng ký; ở Edit Mode thường CHƯA có ⇒ tool ghi rõ cảnh báo ở đầu file
/// thay vì im lặng đưa số sai.
/// </summary>
public static class TouristRewardTableExporter
{
    private const string MenuPath   = "Tools/Farm Game/Tourist Boat/Xuất bảng thưởng khách (38 món)";
    private const string OutputPath = "production/session-state/BANG_THUONG_KHACH_DU_LICH.md";

    [MenuItem(MenuPath, false, 63)]
    public static void Export()
    {
        TouristBoatConfig cfg = LayConfig();
        if (cfg == null)
        {
            EditorUtility.DisplayDialog("Xuất bảng thưởng",
                "Không tìm thấy TouristBoatConfig.asset tại Assets/_Game/ScriptableObjects/.\n\n" +
                "Chạy menu 1. Setup All để tạo config trước.", "OK");
            return;
        }

        List<DishData> mon = TaiTatCaMon();
        if (mon.Count == 0)
        {
            EditorUtility.DisplayDialog("Xuất bảng thưởng",
                "Không tìm thấy asset DishData nào trong project.", "OK");
            return;
        }

        // Sắp theo unlockLevel rồi sellPrice — đọc bảng thấy ngay đường cong kinh tế.
        mon.Sort((a, b) =>
        {
            int c = a.unlockLevel.CompareTo(b.unlockLevel);
            if (c != 0) return c;
            c = a.sellPrice.CompareTo(b.sellPrice);
            return c != 0 ? c : string.CompareOrdinal(a.dishId, b.dishId);
        });

        TouristRewardCalculator.EditorResetWarningCache();

        var sb = new StringBuilder();
        GhiPhanDau(sb, cfg, mon.Count);

        int soFallbackVang = 0, soFallbackExp = 0;
        long tongVangMoi = 0, tongVangCu = 0;

        for (int i = 0; i < mon.Count; i++)
        {
            DishData d = mon[i];
            bool fallback;
            int vangMoi = TouristRewardCalculator.ComputeGold(d, cfg, out fallback);
            int expMoi  = TouristRewardCalculator.ComputeExp(d, cfg);
            int vangCu  = VangCongThucCu(d, cfg);

            if (fallback) soFallbackVang++;
            if (d.rewardExp <= 0) soFallbackExp++;
            tongVangMoi += vangMoi;
            tongVangCu  += vangCu;

            string chenhLech = vangCu > 0
                ? string.Format("{0:+0;-0;0}%", (vangMoi - vangCu) * 100.0 / vangCu)
                : "n/a";
            string soSanhCho = d.sellPrice > 0
                ? string.Format("{0:+0;-0;0}%", (vangMoi - d.sellPrice) * 100.0 / d.sellPrice)
                : "n/a";

            sb.AppendLine($"| {i + 1} | `{d.dishId}` | {d.difficulty} | {d.unlockLevel} | {d.sellPrice} | " +
                          $"**{vangMoi}** | **{expMoi}** | {d.rewardExp} | {vangCu} | {chenhLech} | {soSanhCho} | " +
                          $"{DemNguyenLieu(d)} | {(fallback ? "FALLBACK" : "")} |");
        }

        GhiPhanCuoi(sb, mon.Count, soFallbackVang, soFallbackExp, tongVangMoi, tongVangCu);

        string duongDan = Path.Combine(ThuMucGocProject(), OutputPath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(duongDan));
            File.WriteAllText(duongDan, sb.ToString(), new UTF8Encoding(false));
        }
        catch (Exception e)
        {
            Debug.LogError("[TouristBoat] Không ghi được bảng thưởng: " + e.Message);
            EditorUtility.DisplayDialog("Xuất bảng thưởng", "Ghi file thất bại:\n" + e.Message, "OK");
            return;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[TouristBoat] Đã xuất bảng thưởng {mon.Count} món → {duongDan}");
        EditorUtility.DisplayDialog("Xuất bảng thưởng",
            $"Đã ghi bảng {mon.Count} món vào:\n{OutputPath}\n\n" +
            $"Món phải dùng fallback vàng: {soFallbackVang}\n" +
            $"Món chưa điền rewardExp: {soFallbackExp}\n\n" +
            "Mở file để xem chi tiết (đường dẫn đầy đủ in ở Console).", "OK");
    }

    // ─── Nội dung file ───────────────────────────────────────────────────

    private static void GhiPhanDau(StringBuilder sb, TouristBoatConfig cfg, int soMon)
    {
        sb.AppendLine("# Bảng thưởng khách du lịch — công thức V2.1");
        sb.AppendLine();
        sb.AppendLine($"> Tự sinh bởi `Tools/Farm Game/Tourist Boat/Xuất bảng thưởng khách` lúc " +
                      $"{DateTime.Now:yyyy-MM-dd HH:mm} · {soMon} món đọc từ asset DishData THẬT trong project.");
        sb.AppendLine("> Chạy lại menu này sau mỗi lần tuning để bảng khớp build hiện tại.");
        sb.AppendLine();
        sb.AppendLine("## Công thức");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("vàng = round( sellPrice × diffMult × rarityBonus × touristGoldMultiplier )   [sàn 1]");
        sb.AppendLine("exp  = round( rewardExp × expMult × touristExpMultiplier )                    [sàn 1]");
        sb.AppendLine($"  diffMult:  Easy {cfg.diffMultEasy:0.00} · Normal {cfg.diffMultNormal:0.00} · Hard {cfg.diffMultHard:0.00}");
        sb.AppendLine("  expMult:   Easy 1.00 · Normal 1.10 · Hard 1.25   (hằng trong code)");
        sb.AppendLine($"  touristExpMultiplier = {cfg.touristExpMultiplier:0.00}   [QA M-9] hãm lạm phát cấp độ");
        sb.AppendLine($"  rarityBonus = 1 + 0.05×(số nguyên liệu Rare) + 0.12×(số Epic), trần {cfg.rarityBonusCap:0.00}");
        sb.AppendLine($"  touristGoldMultiplier = {cfg.touristGoldMultiplier:0.00}");
        sb.AppendLine($"  FALLBACK khi sellPrice <= 0: Σ giá nguyên liệu chính × {cfg.rewardIngredientMultiplier}");
        sb.AppendLine("  FALLBACK khi rewardExp <= 0: round(8 + unlockLevel × 1.5)");
        sb.AppendLine("```");
        sb.AppendLine();

        if (!BasePriceBook.HasProvider)
        {
            sb.AppendLine("> **CẢNH BÁO cột \"vàng CŨ\":** BasePriceBook chưa có provider giá lúc xuất bảng " +
                          "(bình thường ở Edit Mode) nên cột này tính theo giá mặc định " +
                          $"{BasePriceBook.DefaultBasePrice}/nguyên liệu, KHÔNG phải giá thật. " +
                          "Muốn cột đó đúng: vào Play Mode rồi chạy lại menu xuất bảng.");
            sb.AppendLine();
        }

        sb.AppendLine("## Bảng");
        sb.AppendLine();
        sb.AppendLine("| # | dishId | difficulty | Lv | sellPrice | Vàng khách trả | EXP khách trả | rewardExp gốc | Vàng CŨ (Σ×hs) | Mới vs Cũ | Mới vs bán chợ | Nguyên liệu (chính/quý) | Ghi chú |");
        sb.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");
    }

    private static void GhiPhanCuoi(StringBuilder sb, int soMon, int fbVang, int fbExp,
                                    long tongMoi, long tongCu)
    {
        sb.AppendLine();
        sb.AppendLine("## Tổng kết");
        sb.AppendLine();
        sb.AppendLine($"- Số món: **{soMon}**");
        sb.AppendLine($"- Món phải dùng fallback vàng (chưa điền `sellPrice`): **{fbVang}**");
        sb.AppendLine($"- Món chưa điền `rewardExp`: **{fbExp}**");
        sb.AppendLine($"- Tổng vàng nếu phục vụ mỗi món 1 lần: **{tongMoi}** (công thức cũ: {tongCu})");
        if (tongCu > 0)
            sb.AppendLine($"- Thay đổi tổng thể: **{(tongMoi - tongCu) * 100.0 / tongCu:+0.0;-0.0;0}%**");
        sb.AppendLine();
        sb.AppendLine("### Đọc bảng thế nào");
        sb.AppendLine();
        sb.AppendLine("- **Mới vs bán chợ** là cột quan trọng nhất: phải luôn ≥ 0% — âm nghĩa là phục vụ khách " +
                      "du lịch LỖ hơn bán chợ, người chơi sẽ bỏ hệ boat (đây chính là lỗi của công thức cũ).");
        sb.AppendLine("- Món cùng tầm `sellPrice` mà `difficulty` cao hơn phải trả vàng cao hơn.");
        sb.AppendLine("- Dòng có `FALLBACK` = asset thiếu số, cần điền `sellPrice`/`requiredIngredients`.");
        sb.AppendLine("- Cột EXP đã nhân `touristExpMultiplier` (mặc định 0.40). Nấu xong trong bếp ĐÃ cộng EXP " +
                      "của món một lần rồi, nên đây chỉ là phần thưởng THÊM — đừng nâng knob quá 1.0 (QA M-9).");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    /// <summary>Vàng theo công thức CŨ (V2.0): Σ giá nguyên liệu chính × rewardIngredientMultiplier.</summary>
    private static int VangCongThucCu(DishData dish, TouristBoatConfig cfg)
    {
        if (dish == null) return 0;
        var list = dish.requiredIngredients;
        if (list == null || list.Count == 0) return 0;

        int mul  = Mathf.Max(1, cfg.rewardIngredientMultiplier);
        int tong = 0;
        for (int i = 0; i < list.Count; i++)
        {
            IngredientData ing = list[i];
            if (ing == null || string.IsNullOrEmpty(ing.id)) continue;
            if (ing.kind == IngredientKind.Seasoning) continue; // [QA M-4] gia vị không tính

            int gia;
            if (!BasePriceBook.TryGetBasePrice(ing.id, out gia))
                gia = BasePriceBook.DefaultBasePrice;
            tong += gia;
        }
        return tong * mul;
    }

    /// <summary>"3/1" = 3 nguyên liệu chính, 1 trong đó là Rare/Epic.</summary>
    private static string DemNguyenLieu(DishData dish)
    {
        var list = dish.requiredIngredients;
        if (list == null || list.Count == 0) return "0/0";

        int chinh = 0, quy = 0;
        for (int i = 0; i < list.Count; i++)
        {
            IngredientData ing = list[i];
            if (ing == null || ing.kind == IngredientKind.Seasoning) continue;
            chinh++;
            if (ing.tier != IngredientTier.Basic) quy++;
        }
        return chinh + "/" + quy;
    }

    private static List<DishData> TaiTatCaMon()
    {
        var ds = new List<DishData>();
        string[] guids = AssetDatabase.FindAssets("t:DishData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var d = AssetDatabase.LoadAssetAtPath<DishData>(path);
            if (d != null) ds.Add(d);
        }
        return ds;
    }

    private static TouristBoatConfig LayConfig()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<TouristBoatConfig>(
            "Assets/_Game/ScriptableObjects/TouristBoatConfig.asset");
        if (cfg != null) return cfg;

        string[] guids = AssetDatabase.FindAssets("t:TouristBoatConfig");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<TouristBoatConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    /// <summary>Thư mục gốc repo = cha của Assets/ (Application.dataPath bỏ đuôi "/Assets").</summary>
    private static string ThuMucGocProject()
    {
        string dataPath = Application.dataPath.Replace('\\', '/');
        int cut = dataPath.LastIndexOf("/Assets", StringComparison.Ordinal);
        return cut > 0 ? dataPath.Substring(0, cut) : dataPath;
    }
}
#endif
