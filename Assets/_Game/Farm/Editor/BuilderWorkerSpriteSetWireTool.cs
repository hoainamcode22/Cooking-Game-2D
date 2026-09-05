using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// NỐI 3 BỘ SPRITE RIÊNG CHO 3 THỢ XÂY — 1 nút, thay cho việc kéo tay 72 ô Inspector.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO CÓ TOOL NÀY (2026-09-04, vòng 12):
///   Art w02/w03 đội vẽ đã giao và ĐÃ slice xong từ 03/09 (6 spritesheet × 12 frame),
///   nhưng `BuilderWorkerConfig.workerSpriteSets` vẫn còn 3 ô RỖNG ⇒ cả 3 thợ rơi vào
///   nhánh legacy `hammerFrames`/`celebrateFrames` dùng chung ⇒ Sếp thấy "1 thợ copy ra
///   3 người giống hệt nhau". Đây thuần tuý là việc NỐI DÂY, không phải thiếu art.
///
/// AN TOÀN (theo production/AUTONOMY.md — đổi data phải qua tool có verify + undo + report):
///   • DRY-RUN in báo cáo đầy đủ, KHÔNG ghi gì vào asset.
///   • APPLY dùng Undo.RecordObject ⇒ Ctrl+Z một phát là về nguyên trạng.
///   • KHÔNG đụng tới `hammerFrames`/`celebrateFrames` legacy (vẫn giữ làm lưới an toàn).
///   • Thiếu sheet hoặc thiếu frame ⇒ BỎ QUA thợ đó, ghi cảnh báo, các thợ khác vẫn nối.
///     Ô nào để rỗng thì thợ đó tự lùi legacy — không crash, không mất thợ.
///
/// [Worker]
/// </summary>
public static class BuilderWorkerSpriteSetWireTool
{
    private const string MENU_DRY   = "Tools/Farm Game/Worker/★ Nối 3 bộ sprite riêng cho 3 thợ (DRY-RUN)";
    private const string MENU_APPLY = "Tools/Farm Game/Worker/★ Nối 3 bộ sprite riêng cho 3 thợ (APPLY)";
    private const string MENU_CHECK = "Tools/Farm Game/Worker/Kiểm tra 3 thợ có khác nhau chưa (chỉ đọc)";

    private const string CONFIG_PATH = "Assets/_Game/Resources/BuilderWorkerConfig.asset";
    private const string ART_ROOT    = "Assets/Art/Characters/Worker";
    private const int    FRAME_COUNT = 12;

    /// <summary>Mô tả 1 thợ: tên nhận dạng + 2 spritesheet nguồn.</summary>
    private struct ThoSpec
    {
        public string TenNhanDang;
        public string SheetHammer;
        public string SheetCelebrate;
    }

    private static readonly ThoSpec[] DanhSachTho =
    {
        new ThoSpec {
            TenNhanDang    = "Worker 01 — mũ vàng, yếm xanh",
            SheetHammer    = ART_ROOT + "/worker_hammer_spritesheet.png",
            SheetCelebrate = ART_ROOT + "/worker_celebrate_spritesheet.png",
        },
        new ThoSpec {
            TenNhanDang    = "Worker 02 — mũ cam, râu quai nón",
            SheetHammer    = ART_ROOT + "/worker02_hammer_spritesheet.png",
            SheetCelebrate = ART_ROOT + "/worker02_celebrate_spritesheet.png",
        },
        new ThoSpec {
            TenNhanDang    = "Worker 03 — mũ trắng, khăn đỏ",
            SheetHammer    = ART_ROOT + "/worker03_hammer_spritesheet.png",
            SheetCelebrate = ART_ROOT + "/worker03_celebrate_spritesheet.png",
        },
    };

    [MenuItem(MENU_DRY, false, 40)]
    private static void DryRun() { Chay(false); }

    [MenuItem(MENU_APPLY, false, 41)]
    private static void Apply() { Chay(true); }

    // ─────────────────────────────────────────────────────────────────────────
    private static void Chay(bool ghiThat)
    {
        var cfg = AssetDatabase.LoadAssetAtPath<BuilderWorkerConfig>(CONFIG_PATH);
        if (cfg == null)
        {
            Debug.LogError($"[WorkerWire] KHÔNG tìm thấy config tại {CONFIG_PATH}. Dừng, không đụng gì.");
            return;
        }

        var bc  = new StringBuilder();
        var che = ghiThat ? "APPLY" : "DRY-RUN";
        bc.AppendLine($"╔══ [WorkerWire] {che} — nối 3 bộ sprite riêng cho 3 thợ ══");
        bc.AppendLine($"║ Config: {CONFIG_PATH}");

        // Gom kết quả trước, chỉ ghi khi TẤT CẢ đã đọc xong — tránh ghi nửa vời.
        var ketQua   = new List<(int idx, ThoSpec spec, Sprite[] hammer, Sprite[] celeb)>();
        int soLoi    = 0;
        int soBoQua  = 0;

        for (int i = 0; i < DanhSachTho.Length; i++)
        {
            var spec = DanhSachTho[i];
            bc.AppendLine($"║");
            bc.AppendLine($"║ ── Thợ [{i}] {spec.TenNhanDang}");

            Sprite[] hammer = DocFrames(spec.SheetHammer,    bc, ref soLoi);
            Sprite[] celeb  = DocFrames(spec.SheetCelebrate, bc, ref soLoi);

            if (hammer == null || celeb == null)
            {
                bc.AppendLine($"║    ⏭  BỎ QUA thợ [{i}] — thiếu frame. Thợ này sẽ tự lùi về bộ legacy dùng chung.");
                soBoQua++;
                continue;
            }

            bc.AppendLine($"║    ✔ búa      : {hammer.Length} frame ({hammer[0].name} … {hammer[hammer.Length - 1].name})");
            bc.AppendLine($"║    ✔ ăn mừng  : {celeb.Length} frame ({celeb[0].name} … {celeb[celeb.Length - 1].name})");
            ketQua.Add((i, spec, hammer, celeb));
        }

        // ── Cảnh báo trùng lặp: 2 thợ mà cùng sprite đầu tiên ⇒ vẫn sẽ giống nhau.
        var nhomTrung = ketQua.GroupBy(k => k.hammer[0].GetInstanceID()).Where(g => g.Count() > 1).ToList();
        foreach (var g in nhomTrung)
        {
            soLoi++;
            bc.AppendLine($"║ ⚠ TRÙNG: thợ [{string.Join(",", g.Select(x => x.idx))}] dùng CHUNG frame búa đầu " +
                          $"'{g.First().hammer[0].name}' ⇒ nhìn vẫn giống nhau. Kiểm lại đường dẫn sheet.");
        }

        bc.AppendLine("║");
        bc.AppendLine($"║ TỔNG: {ketQua.Count} thợ nối được · {soBoQua} bỏ qua · {soLoi} cảnh báo/lỗi");

        if (!ghiThat)
        {
            bc.AppendLine("║ ⓘ DRY-RUN — CHƯA ghi gì vào asset. Sạch rồi thì chạy bản (APPLY).");
            bc.AppendLine("╚════════════════════════════════════════════════════════════");
            Debug.Log(bc.ToString());
            return;
        }

        if (ketQua.Count == 0)
        {
            bc.AppendLine("║ ✋ Không có thợ nào nối được ⇒ KHÔNG ghi gì, giữ nguyên asset.");
            bc.AppendLine("╚════════════════════════════════════════════════════════════");
            Debug.LogWarning(bc.ToString());
            return;
        }

        // ── GHI THẬT (có Undo) ───────────────────────────────────────────────
        Undo.RecordObject(cfg, "Nối 3 bộ sprite riêng cho 3 thợ xây");

        // Array.Resize GIỮ LẠI phần tử cũ. Cấp phát mảng mới sẽ XOÁ SẠCH bộ nào Sếp đã
        // nối tay trước đó (QA bắt 04/09) — Ctrl+Z cứu được nhưng đừng để phải cứu.
        if (cfg.workerSpriteSets == null)
            cfg.workerSpriteSets = new BuilderWorkerConfig.WorkerSpriteSet[DanhSachTho.Length];
        else if (cfg.workerSpriteSets.Length < DanhSachTho.Length)
            System.Array.Resize(ref cfg.workerSpriteSets, DanhSachTho.Length);

        foreach (var (idx, spec, hammer, celeb) in ketQua)
        {
            if (cfg.workerSpriteSets[idx] == null)
                cfg.workerSpriteSets[idx] = new BuilderWorkerConfig.WorkerSpriteSet();

            var bo = cfg.workerSpriteSets[idx];
            bo.tenThoNhanDang = spec.TenNhanDang;
            bo.hammerFrames   = hammer;
            bo.celebrateFrames = celeb;
        }

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();

        bc.AppendLine("║ ✅ ĐÃ GHI vào asset. Lỡ tay thì Ctrl+Z (Undo) là về nguyên trạng.");
        bc.AppendLine("║ ⓘ Bộ legacy hammerFrames/celebrateFrames GIỮ NGUYÊN — không đụng tới.");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");
        Debug.Log(bc.ToString());

        KiemTra();
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Đọc đúng <see cref="FRAME_COUNT"/> sprite con từ 1 spritesheet, sắp theo tên.
    /// Tên do tool slice sinh ra có số 2 chữ số (…_01..12) nên sắp theo Ordinal là đúng thứ tự.
    /// Trả null nếu thiếu file / chưa slice / không đủ frame — bên gọi sẽ bỏ qua thợ đó.
    /// </summary>
    private static Sprite[] DocFrames(string sheetPath, StringBuilder bc, ref int soLoi)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        if (all == null || all.Length == 0)
        {
            soLoi++;
            bc.AppendLine($"║    ✖ KHÔNG đọc được: {sheetPath}");
            return null;
        }

        var sprites = all.OfType<Sprite>()
                         .OrderBy(s => s.name, System.StringComparer.Ordinal)
                         .ToArray();

        if (sprites.Length < FRAME_COUNT)
        {
            soLoi++;
            bc.AppendLine($"║    ✖ {System.IO.Path.GetFileName(sheetPath)} mới có {sprites.Length}/{FRAME_COUNT} sprite con " +
                          $"⇒ chưa slice xong. Chạy 'Characters ▸ ★ Slice 3 spritesheet nhân vật (APPLY)' trước.");
            return null;
        }

        if (sprites.Length > FRAME_COUNT)
            bc.AppendLine($"║    ⚠ {System.IO.Path.GetFileName(sheetPath)} có {sprites.Length} sprite con, lấy {FRAME_COUNT} cái đầu.");

        return sprites.Take(FRAME_COUNT).ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(MENU_CHECK, false, 42)]
    private static void KiemTra()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<BuilderWorkerConfig>(CONFIG_PATH);
        if (cfg == null) { Debug.LogError($"[WorkerWire] Không thấy {CONFIG_PATH}"); return; }

        var bc = new StringBuilder();
        bc.AppendLine("╔══ [WorkerWire] KIỂM TRA — 3 thợ đã khác nhau chưa? ══");
        bc.AppendLine($"║ enabled (công tắc tổng) = {cfg.enabled}" + (cfg.enabled ? "" : "  ⚠ ĐANG TẮT — hệ thợ sẽ không chạy!"));

        var dauTien = new List<string>();
        for (int i = 0; i < DanhSachTho.Length; i++)
        {
            Sprite[] h = cfg.GetHammerFrames(i);
            Sprite[] c = cfg.GetCelebrateFrames(i);
            bool rieng = cfg.workerSpriteSets != null
                      && i < cfg.workerSpriteSets.Length
                      && cfg.workerSpriteSets[i] != null
                      && cfg.workerSpriteSets[i].hammerFrames != null
                      && cfg.workerSpriteSets[i].hammerFrames.Length > 0;

            string ten = (h != null && h.Length > 0 && h[0] != null) ? h[0].name : "(null)";
            dauTien.Add(ten);
            bc.AppendLine($"║ Thợ [{i}] : {(rieng ? "BỘ RIÊNG ✔" : "lùi LEGACY ⚠")} · búa {(h?.Length ?? 0)}f · mừng {(c?.Length ?? 0)}f · frame đầu = {ten}");
        }

        bool khacNhau = dauTien.Distinct().Count() == DanhSachTho.Length;
        bc.AppendLine("║");
        bc.AppendLine(khacNhau
            ? "║ ✅ 3 thợ dùng 3 bộ sprite KHÁC NHAU — vào Play sẽ thấy 3 người khác nhau."
            : "║ ❌ Vẫn còn thợ dùng chung sprite ⇒ trông giống nhau. Chạy bản (APPLY) đi.");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");

        if (khacNhau) Debug.Log(bc.ToString()); else Debug.LogWarning(bc.ToString());
    }
}
