using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  KIỂM TRA TUTORIAL — CHỈ ĐỌC, KHÔNG GHI GÌ
/// ══════════════════════════════════════════════════════════════════════════
///
/// Danh sách `_steps` trên TutorialManager là một mảng kéo-thả trong Inspector.
/// Kéo nhầm, kéo thiếu, hoặc xoá asset là chuyện xảy ra thường xuyên, và hậu quả
/// chỉ lộ ra khi chơi lại tutorial từ đầu (mất ~10 phút mỗi lần thử).
/// Tool này soát toàn bộ danh sách trong 1 giây và IN BÁO CÁO — không sửa,
/// không ghi scene, không tự lưu. Muốn sửa thì sửa tay trong Inspector.
///
/// ⚠️ ĐÂY LÀ KIỂM TĨNH (static check).
/// Target của tutorial được đăng ký lúc CHẠY qua `TutorialManager.RegisterTarget()`
/// và `TutorialRuntimeTargetResolver`. Ở Edit Mode KHÔNG thể biết một targetID có
/// thật sự phân giải được hay không. Vì vậy mục "targetID lạ" chỉ đối chiếu với
/// DANH SÁCH ID KHẢ DĨ (id hằng trong resolver + id của mọi component TutorialTarget
/// có trong scene đang mở) và luôn xếp loại CẢNH BÁO, không bao giờ là LỖI.
/// </summary>
public static class TutorialAuditTool
{
    private const string THU_MUC_BUOC   = "Assets/Resources/TutorialSteps/L1_L2";
    private const string ASSET_DU_PHONG = "L1L2_04b_FirstHarvest";
    private const int    SO_BUOC_KY_VONG = 31;   // 21 bước L1L2 + 10 bước L2

    // Các id được resolver đăng ký bằng chuỗi hằng (đọc từ TutorialRuntimeTargetResolver.cs).
    private static readonly string[] ID_HANG_TRONG_RESOLVER =
    {
        "seed_rice", "seed_huong_duong",
        "tutorial_pen", "tutorial_pen_gem", "tutorial_feed", "tutorial_basket",
        "shop_corn", "shop_corn_plus", "shop_corn_buy", "shop_close",
    };

    [MenuItem("Tools/Farm/Tutorial/Kiem tra tutorial (chi bao cao)", false, 322)]
    private static void KiemTraTutorial()
    {
        var mgr = Object.FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
        if (mgr == null)
        {
            Debug.LogError("[TutorialAudit] Không tìm thấy TutorialManager trong scene đang mở. " +
                           "Hãy mở scene farm (vd Assets/_Game/Scenes/SCN_Farm.unity) rồi chạy lại.");
            return;
        }

        var so   = new SerializedObject(mgr);
        var mang = so.FindProperty("_steps");
        if (mang == null || !mang.isArray)
        {
            Debug.LogError("[TutorialAudit] Không đọc được field '_steps' trên TutorialManager " +
                           "(field đã đổi tên?). Không kiểm được gì thêm.");
            return;
        }

        int soLoi     = 0;
        int soCanhBao = 0;
        var sb        = new StringBuilder();

        sb.AppendLine("══════════ KIỂM TRA TUTORIAL (chỉ báo cáo, không sửa gì) ══════════");
        sb.AppendLine($"TutorialManager : {LayDuongDanScene(mgr)}");
        sb.AppendLine();

        // ── 1. Số lượng + liệt kê đầy đủ ────────────────────────────────────
        int soHienCo = mang.arraySize;
        sb.AppendLine("── 1. DANH SÁCH _steps ─────────────────────────────────────────");
        sb.AppendLine($"Số bước hiện có : {soHienCo}   ·   Kỳ vọng: {SO_BUOC_KY_VONG}" +
                      (soHienCo == SO_BUOC_KY_VONG ? "   (khớp)" : $"   (LỆCH {soHienCo - SO_BUOC_KY_VONG:+#;-#;0})"));
        sb.AppendLine();

        var buocTheoChiSo = new List<TutorialStepData>(soHienCo);
        for (int i = 0; i < soHienCo; i++)
        {
            var buoc = mang.GetArrayElementAtIndex(i).objectReferenceValue as TutorialStepData;
            buocTheoChiSo.Add(buoc);

            if (buoc == null)
            {
                sb.AppendLine($"  [{i:00}] <NULL>");
                continue;
            }

            string moTaTarget = string.IsNullOrEmpty(buoc.targetID) ? "-" : buoc.targetID;
            string moTaKeo    = string.IsNullOrEmpty(buoc.dragToTargetId) ? "" : $" · kéo→{buoc.dragToTargetId}";
            string moTaBang   = buoc.showGuideBoard ? " · [bảng hướng dẫn]" : "";
            sb.AppendLine($"  [{i:00}] {buoc.name,-28} target={moTaTarget,-20} wait={buoc.waitAction}{moTaKeo}{moTaBang}");
        }
        sb.AppendLine();

        // ── 2. Ô NULL ───────────────────────────────────────────────────────
        sb.AppendLine("── 2. Ô NULL ───────────────────────────────────────────────────");
        var chiSoNull = new List<int>();
        for (int i = 0; i < buocTheoChiSo.Count; i++)
            if (buocTheoChiSo[i] == null) chiSoNull.Add(i);

        if (chiSoNull.Count == 0)
        {
            sb.AppendLine("  Không có ô nào bị NULL.");
        }
        else
        {
            soLoi += chiSoNull.Count;
            foreach (int i in chiSoNull)
                sb.AppendLine($"  LỖI · chỉ số [{i:00}] để trống — tutorial sẽ đứng ở bước này.");
        }
        sb.AppendLine();

        // ── 3. Asset bị gán TRÙNG ───────────────────────────────────────────
        sb.AppendLine("── 3. ASSET GÁN TRÙNG ──────────────────────────────────────────");
        var viTriTheoAsset = new Dictionary<TutorialStepData, List<int>>();
        for (int i = 0; i < buocTheoChiSo.Count; i++)
        {
            var buoc = buocTheoChiSo[i];
            if (buoc == null) continue;
            if (!viTriTheoAsset.TryGetValue(buoc, out var ds))
            {
                ds = new List<int>();
                viTriTheoAsset[buoc] = ds;
            }
            ds.Add(i);
        }

        var nhomTrung = viTriTheoAsset.Where(kv => kv.Value.Count > 1).ToList();
        if (nhomTrung.Count == 0)
        {
            sb.AppendLine("  Không có asset nào bị gán 2 lần.");
        }
        else
        {
            soLoi += nhomTrung.Count;
            foreach (var kv in nhomTrung)
                sb.AppendLine($"  LỖI · '{kv.Key.name}' xuất hiện {kv.Value.Count} lần tại chỉ số " +
                              $"[{string.Join(", ", kv.Value.Select(x => x.ToString("00")))}].");
        }
        sb.AppendLine();

        // ── 4. Asset có trong thư mục nhưng KHÔNG nằm trong _steps ──────────
        sb.AppendLine("── 4. ASSET CÓ TRONG THƯ MỤC NHƯNG THIẾU TRONG _steps ──────────");
        sb.AppendLine($"  Thư mục quét: {THU_MUC_BUOC}");

        var daGan     = new HashSet<TutorialStepData>(viTriTheoAsset.Keys);
        var guidBuoc  = AssetDatabase.FindAssets("t:TutorialStepData", new[] { THU_MUC_BUOC });
        var thieuHan  = new List<string>();
        int soDuPhong = 0;

        foreach (string guid in guidBuoc.OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
        {
            string duongDan = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<TutorialStepData>(duongDan);
            if (asset == null || daGan.Contains(asset)) continue;

            if (asset.name == ASSET_DU_PHONG)
            {
                soDuPhong++;
                sb.AppendLine($"  (bỏ qua) '{asset.name}' — asset DỰ PHÒNG, chưa bao giờ nằm trong _steps. Không phải lỗi.");
                continue;
            }
            thieuHan.Add(asset.name);
        }

        if (thieuHan.Count == 0)
        {
            sb.AppendLine($"  Không thiếu asset nào (đã bỏ qua {soDuPhong} asset dự phòng).");
        }
        else
        {
            soLoi += thieuHan.Count;
            foreach (string ten in thieuHan)
                sb.AppendLine($"  LỖI · '{ten}' có file asset nhưng KHÔNG được gán vào _steps.");
        }
        sb.AppendLine();

        // ── 5/6/7. Soát từng bước ───────────────────────────────────────────
        var idKhaDi = GomIdKhaDi();

        sb.AppendLine("── 5. targetID KHÔNG KHỚP DANH SÁCH ID KHẢ DĨ (kiểm TĨNH) ──────");
        sb.AppendLine($"  Danh sách id khả dĩ gồm {idKhaDi.Count} id: id hằng trong TutorialRuntimeTargetResolver " +
                      "+ id sinh theo mẫu (tutorial_plot_01..08, tutorial_flower_01..06) " +
                      "+ targetID của mọi TutorialTarget trong scene đang mở.");
        sb.AppendLine("  Target thật được đăng ký LÚC CHẠY nên đây chỉ là CẢNH BÁO, không phải lỗi.");

        int soCanhBaoTarget = 0;
        for (int i = 0; i < buocTheoChiSo.Count; i++)
        {
            var buoc = buocTheoChiSo[i];
            if (buoc == null || string.IsNullOrWhiteSpace(buoc.targetID)) continue;
            if (idKhaDi.Contains(buoc.targetID)) continue;

            soCanhBaoTarget++;
            sb.AppendLine($"  CẢNH BÁO · [{i:00}] {buoc.name}: targetID '{buoc.targetID}' không nằm trong danh sách id khả dĩ.");
        }
        // Cả dragToTargetId cũng là một target — soát luôn cho đủ.
        for (int i = 0; i < buocTheoChiSo.Count; i++)
        {
            var buoc = buocTheoChiSo[i];
            if (buoc == null || string.IsNullOrWhiteSpace(buoc.dragToTargetId)) continue;
            if (idKhaDi.Contains(buoc.dragToTargetId)) continue;

            soCanhBaoTarget++;
            sb.AppendLine($"  CẢNH BÁO · [{i:00}] {buoc.name}: dragToTargetId '{buoc.dragToTargetId}' không nằm trong danh sách id khả dĩ.");
        }
        if (soCanhBaoTarget == 0) sb.AppendLine("  Mọi targetID / dragToTargetId đều khớp danh sách id khả dĩ.");
        soCanhBao += soCanhBaoTarget;
        sb.AppendLine();

        sb.AppendLine("── 6. KÉO TỪ HƯ KHÔNG (có dragToTargetId mà targetID rỗng) ─────");
        int soLoiKeo = 0;
        for (int i = 0; i < buocTheoChiSo.Count; i++)
        {
            var buoc = buocTheoChiSo[i];
            if (buoc == null) continue;
            if (string.IsNullOrWhiteSpace(buoc.dragToTargetId)) continue;
            if (!string.IsNullOrWhiteSpace(buoc.targetID)) continue;

            soLoiKeo++;
            sb.AppendLine($"  LỖI · [{i:00}] {buoc.name}: dragToTargetId='{buoc.dragToTargetId}' " +
                          "nhưng targetID rỗng — bàn tay không có điểm bắt đầu để kéo.");
        }
        if (soLoiKeo == 0) sb.AppendLine("  Không có bước nào kéo từ hư không.");
        soLoi += soLoiKeo;
        sb.AppendLine();

        sb.AppendLine("── 7. WaitForClick MÀ npcText RỖNG ─────────────────────────────");
        sb.AppendLine("  (Bỏ qua bước có showGuideBoard = true: nội dung nằm trên bảng hướng dẫn, không ở npcText.)");
        int soCanhBaoText = 0;
        for (int i = 0; i < buocTheoChiSo.Count; i++)
        {
            var buoc = buocTheoChiSo[i];
            if (buoc == null) continue;
            if (buoc.waitAction != TutorialWaitAction.WaitForClick) continue;
            if (!string.IsNullOrWhiteSpace(buoc.npcText)) continue;
            if (buoc.showGuideBoard) continue;

            soCanhBaoText++;
            sb.AppendLine($"  CẢNH BÁO · [{i:00}] {buoc.name}: chờ người chơi bấm nhưng npcText rỗng — không có gì để bấm.");
        }
        if (soCanhBaoText == 0) sb.AppendLine("  Không có bước nào chờ bấm mà bỏ trống lời thoại.");
        soCanhBao += soCanhBaoText;
        sb.AppendLine();

        // ── 8. Tổng kết ─────────────────────────────────────────────────────
        sb.AppendLine("── 8. TỔNG KẾT ─────────────────────────────────────────────────");
        if (soLoi == 0 && soCanhBao == 0) sb.AppendLine("  SẠCH");
        sb.AppendLine($"  LỖI: {soLoi} · CẢNH BÁO: {soCanhBao}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════════");

        if (soLoi > 0)      Debug.LogError(sb.ToString());
        else if (soCanhBao > 0) Debug.LogWarning(sb.ToString());
        else                Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Gom mọi id mà tutorial CÓ KHẢ NĂNG đăng ký lúc chạy. Không đảm bảo id nào
    /// cũng phân giải được — chỉ để phát hiện lỗi gõ sai tên.
    /// </summary>
    private static HashSet<string> GomIdKhaDi()
    {
        var tap = new HashSet<string>(ID_HANG_TRONG_RESOLVER);

        // Proxy sinh theo vòng lặp trong resolver: 8 ô lúa, 6 chậu hoa.
        for (int i = 1; i <= 8; i++) tap.Add($"tutorial_plot_{i:00}");
        for (int i = 1; i <= 6; i++) tap.Add($"tutorial_flower_{i:00}");

        // Target đặt sẵn trong scene (kể cả object đang tắt).
        var targets = Object.FindObjectsByType<TutorialTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in targets)
            if (t != null && !string.IsNullOrWhiteSpace(t.targetID)) tap.Add(t.targetID);

        return tap;
    }

    /// <summary>Đường dẫn trong Hierarchy, để biết đang soát TutorialManager nào.</summary>
    private static string LayDuongDanScene(Component c)
    {
        string duongDan = c.name;
        for (Transform t = c.transform.parent; t != null; t = t.parent)
            duongDan = t.name + "/" + duongDan;
        return duongDan;
    }
}
