using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ★ TOOL VÁ 2026-09-03 — sửa 2 bug dữ liệu làm "decor tàng hình" mà Sếp báo:
///
/// BUG B — 75 reference sprite CHẾT trong DecorGrowthConfig.asset:
///   DecorStageArtTool đã gán reference sprite TRƯỚC rồi mới đổi import setting +
///   reimport 75 PNG ⇒ Unity cấp internalID mới (meta: internalIDToNameTable
///   1609469681... ) trong khi config còn ghim fileID 21300000 kiểu cũ ⇒ mọi
///   stage sprite load về NULL ⇒ IsValid=false ⇒ TẤT CẢ decor rơi vào WorkerOnly:
///   có thợ đập búa nhưng không bao giờ hiện stage 1/2/hộp quà.
///   → Vá: nạp lại Sprite SỐNG từng file (import đã ổn định từ lâu) và gán lại
///     qua SerializedObject ⇒ serialize ra đúng internalID mới. Xong VERIFY bằng
///     cách reload tươi + in bảng FULL/WorkerOnly từng item — phải 15/15 FULL.
///
/// BUG A — 4 prefab decor mới (Decor_chaucaythu/chulun/giabanrau/binhtuoihoa)
///   được tạo với transform scale = 1 trong khi MỌI prefab decor cũ dùng scale 100
///   (đo thật: "Bù nhìn 1.prefab" m_LocalScale = 100). Sprite PPU 100 ⇒ item mới chỉ
///   cao ~4,9 world unit giữa map nhà cao 477 unit ⇒ vô hình.
///   → Vá: đặt root scale = (100,100,1) + gán lại sprite stage_3 sống (phòng nó
///     cũng dính reference chết). Collider là local-space nên tự đúng theo scale.
///
/// Backup đã nằm ở production/backup_round3_2026-09-03/ (Lead copy trước khi chạy).
/// </summary>
public static class DecorStageRepairTool
{
    private const string MenuFix   = "Tools/Farm Game/Decor 5 Stage/★ VÁ reference sprite + scale (2026-09-03)";
    private const string MenuCheck = "Tools/Farm Game/Decor 5 Stage/Kiểm tra sức khoẻ reference (chỉ đọc)";

    private const string ConfigPath = "Assets/_Game/Resources/DecorGrowthConfig.asset";
    private const string StageRoot  = "Assets/Art/Decor/Stages";

    /// <summary>itemID → slug thư mục art. PHẢI khớp bảng của DecorStageArtTool.</summary>
    private static readonly Dictionary<int, string> Slug = new Dictionary<int, string>
    {
        {1,"gieng"}, {2,"bunhin"}, {4,"chanhoa"}, {5,"coixaygio"}, {6,"cotden"},
        {9,"meovuive"}, {10,"rom"}, {11,"vonghoa"}, {13,"xehoa"}, {14,"dainuoc"},
        {15,"hoda"}, {16,"chaucaythu"}, {17,"chulun"}, {18,"giabanrau"}, {19,"binhtuoihoa"},
    };

    /// <summary>4 prefab mới cần scale 100 + sprite stage_3. slug → đường prefab.</summary>
    private static readonly Dictionary<string, string> PrefabMoi = new Dictionary<string, string>
    {
        {"chaucaythu",  "Assets/_Game/Farm/CÔNG TRÌNH/Decor_chaucaythu.prefab"},
        {"chulun",      "Assets/_Game/Farm/CÔNG TRÌNH/Decor_chulun.prefab"},
        {"giabanrau",   "Assets/_Game/Farm/CÔNG TRÌNH/Decor_giabanrau.prefab"},
        {"binhtuoihoa", "Assets/_Game/Farm/CÔNG TRÌNH/Decor_binhtuoihoa.prefab"},
    };

    private static readonly string[] FieldTheoStage =
        { "stage1Parts", "stage2HalfBuilt", "stage3Complete", "stage4GiftBox", "stage5BoxOpen" };

    [MenuItem(MenuFix, false, 30)]
    public static void Va()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Tool] DecorStageRepairTool — VÁ reference + scale");
        sb.AppendLine("──────────────────────────────────────────────────────────");
        int loi = 0;

        // ── BUG B: gán lại 75 sprite sống vào config ─────────────────────────
        var cfg = AssetDatabase.LoadAssetAtPath<DecorGrowthConfig>(ConfigPath);
        if (cfg == null)
        {
            Debug.LogError("[Tool] Không thấy " + ConfigPath + " — chạy '★ Nạp art 5 stage (APPLY)' trước.");
            return;
        }

        var so = new SerializedObject(cfg);
        SerializedProperty sets = so.FindProperty("stageSets");
        int gan = 0, thieuFile = 0;
        for (int i = 0; i < sets.arraySize; i++)
        {
            SerializedProperty e = sets.GetArrayElementAtIndex(i);
            int id = e.FindPropertyRelative("itemID").intValue;
            if (!Slug.TryGetValue(id, out string slug))
            {
                sb.AppendLine($"   ⚠ itemID {id} không có trong bảng slug — bỏ qua (kiểm tay).");
                continue;
            }
            for (int stg = 1; stg <= 5; stg++)
            {
                string path = $"{StageRoot}/{slug}/stage_{stg}.png";
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp == null)
                {
                    // ép reimport 1 nhịp rồi thử lại — phòng file chưa được import
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
                if (sp == null) { sb.AppendLine("   ❌ thiếu/không import được: " + path); thieuFile++; loi++; continue; }
                e.FindPropertyRelative(FieldTheoStage[stg - 1]).objectReferenceValue = sp;
                gan++;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cfg);
        sb.AppendLine($"✔ BUG B: gán lại {gan} sprite sống ({thieuFile} file thiếu).");

        // ── BUG A: 4 prefab mới — scale 100 + sprite stage_3 sống ───────────
        foreach (var kv in PrefabMoi)
        {
            string pf = kv.Value;
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(pf);
                Vector3 cu = root.transform.localScale;
                root.transform.localScale = new Vector3(100f, 100f, 1f);

                var sr = root.GetComponentInChildren<SpriteRenderer>(true);
                string p3 = $"{StageRoot}/{kv.Key}/stage_3.png";
                Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>(p3);
                string ghiChuSprite = "giữ nguyên";
                if (sr != null && s3 != null) { sr.sprite = s3; ghiChuSprite = "gán lại stage_3 sống"; }

                PrefabUtility.SaveAsPrefabAsset(root, pf);
                sb.AppendLine($"✔ BUG A: {System.IO.Path.GetFileName(pf)} scale {cu.x:0.##}→100 · sprite {ghiChuSprite}.");
            }
            catch (System.Exception ex)
            {
                sb.AppendLine("   ❌ " + pf + ": " + ex.Message); loi++;
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();

        // ── VERIFY: reload TƯƠI, in bảng chế độ từng item — lưới an toàn mới ──
        sb.AppendLine("──────────────────────────────────────────────────────────");
        sb.AppendLine(KiemTraNoiBo(out int fullOk, out int hong));
        sb.AppendLine("──────────────────────────────────────────────────────────");
        sb.AppendLine(loi == 0 && hong == 0
            ? $"[Tool] TỔNG KẾT: SẠCH — {fullOk} item FULL 5-STAGE. Vào Play mua thử 1 decor: phải thấy stage 1 + thợ (cao bằng shipper)."
            : $"[Tool] TỔNG KẾT: còn {loi + hong} vấn đề — đọc dòng ❌ ở trên rồi chạy lại (idempotent).");
        Debug.Log(sb.ToString());
    }

    [MenuItem(MenuCheck, false, 31)]
    public static void KiemTra()
    {
        Debug.Log("[Tool] KIỂM TRA SỨC KHOẺ REFERENCE (chỉ đọc)\n" + KiemTraNoiBo(out _, out _));
    }

    /// <summary>Reload config tươi, mô phỏng đúng logic runtime IsValid ⇒ in FULL/WorkerOnly.</summary>
    private static string KiemTraNoiBo(out int fullOk, out int hong)
    {
        fullOk = 0; hong = 0;
        var sb = new StringBuilder();
        var cfg = AssetDatabase.LoadAssetAtPath<DecorGrowthConfig>(ConfigPath);
        if (cfg == null) { hong = 1; return "❌ thiếu config " + ConfigPath; }

        sb.AppendLine($"{"itemID",-7}{"tên",-22}{"sprite sống",-12}chế độ runtime");
        foreach (var set in cfg.stageSets)
        {
            if (set == null) continue;
            int song = 0;
            for (int stg = 1; stg <= 5; stg++)
                if (set.SpriteForStage(stg) != null) song++;
            bool full = set.IsValid;
            if (full) fullOk++; else hong++;
            sb.AppendLine($"{set.itemID,-7}{set.displayName,-22}{song + "/5",-12}" +
                          (full ? "✅ FULL 5-STAGE" : "❌ WorkerOnly (reference chết/thiếu — đây chính là bug decor tàng hình)"));
        }
        // 4 prefab mới: scale phải 100
        foreach (var kv in PrefabMoi)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(kv.Value);
            if (go == null) { sb.AppendLine("❌ thiếu prefab " + kv.Value); hong++; continue; }
            float sx = go.transform.localScale.x;
            bool ok = sx > 99f;
            if (!ok) hong++;
            sb.AppendLine($"prefab {System.IO.Path.GetFileNameWithoutExtension(kv.Value),-22} scale={sx:0.##} " +
                          (ok ? "✅" : "❌ phải = 100 (prefab cũ đều 100; scale 1 ⇒ cao 4,9 unit = vô hình)"));
        }
        return sb.ToString();
    }
}
