// ═══════════════════════════════════════════════════════════════════════════
//  TOOL SỬA TEXT TUTORIAL "6 ô" → "8 ô" + KIỂM TRA HẠT KHỞI ĐẦU
//  WP-A2 — 2026-09-05. Editor-only (nằm trong thư mục Editor).
// ═══════════════════════════════════════════════════════════════════════════
//
//  Vì sao: scene thật có 8 ô đất (lúa) và 6 chậu hoa. Hai bước tutorial
//  L1L2_06_PlantAllRice và L1L2_10_HarvestAllRice vẫn nói "6 ô" ⇒ người chơi gieo
//  đủ 6 ô mà bước không qua. Luật AUTONOMY: không sửa tay .asset ⇒ đi qua tool có
//  DRY RUN (chỉ báo cáo) và APPLY (có Undo). Text về HOA (6 chậu) KHÔNG đụng.
//
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TutorialStepTextFixTool
{
    private const string ThuMuc     = "Assets/Resources/TutorialSteps/L1_L2";
    private const string TextCu     = "6 ô";
    private const string TextMoi    = "8 ô";
    private const string MenuGoc    = "Tools/Farm/Tutorial/";

    /// <summary>Chỉ đụng đúng 2 asset nói về LÚA (8 ô đất). Asset hoa (6 chậu) để nguyên.</summary>
    private static readonly HashSet<string> TenAssetCanSua = new HashSet<string>
    {
        "L1L2_06_PlantAllRice",
        "L1L2_10_HarvestAllRice",
    };

    [MenuItem(MenuGoc + "Sua text 6 o -> 8 o - DRY RUN (chi bao cao)", false, 300)]
    public static void DryRun() => ChayTool(apDung: false);

    [MenuItem(MenuGoc + "Sua text 6 o -> 8 o - APPLY", false, 301)]
    public static void Apply()
    {
        bool dongY = EditorUtility.DisplayDialog(
            "Sửa text tutorial 6 ô → 8 ô",
            $"Thay \"{TextCu}\" bằng \"{TextMoi}\" trong mọi chuỗi của:\n" +
            "  • L1L2_06_PlantAllRice\n  • L1L2_10_HarvestAllRice\n\n" +
            "Có Undo (Ctrl+Z). Asset hoa không bị đụng. Tiếp tục?",
            "Áp dụng", "Huỷ");
        if (!dongY) return;
        ChayTool(apDung: true);
    }

    private static void ChayTool(bool apDung)
    {
        var sb = new StringBuilder();
        sb.AppendLine(apDung
            ? "═══ [TutorialStepTextFix] APPLY — đã sửa ═══"
            : "═══ [TutorialStepTextFix] DRY RUN — chỉ báo cáo, chưa sửa gì ═══");

        string[] guids = AssetDatabase.FindAssets("t:TutorialStepData", new[] { ThuMuc });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[TutorialStepTextFix] Không tìm thấy TutorialStepData nào trong {ThuMuc}.");
            return;
        }

        int soAssetXet = 0, soChuoiDoi = 0;
        if (apDung) Undo.IncrementCurrentGroup();

        foreach (string guid in guids)
        {
            string duongDan = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<TutorialStepData>(duongDan);
            if (asset == null) continue;
            if (!TenAssetCanSua.Contains(asset.name)) continue;

            soAssetXet++;
            var so = new SerializedObject(asset);
            var thuocTinh = so.GetIterator();
            bool coDoi = false;

            // Duyệt MỌI thuộc tính chuỗi (kể cả trong struct/list lồng nhau) — không hard-code tên field.
            bool vaoCon = true;
            while (thuocTinh.NextVisible(vaoCon))
            {
                vaoCon = true;
                if (thuocTinh.propertyType != SerializedPropertyType.String) continue;

                string cu = thuocTinh.stringValue;
                if (string.IsNullOrEmpty(cu) || !cu.Contains(TextCu)) continue;

                string moi = cu.Replace(TextCu, TextMoi);
                soChuoiDoi++;
                coDoi = true;

                sb.AppendLine($"• {asset.name} › {thuocTinh.propertyPath}");
                sb.AppendLine($"    TRƯỚC: \"{cu}\"");
                sb.AppendLine($"    SAU  : \"{moi}\"");

                if (apDung) thuocTinh.stringValue = moi;
            }

            if (apDung && coDoi)
            {
                Undo.RecordObject(asset, "Sửa text tutorial 6 ô → 8 ô");
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            if (!coDoi) sb.AppendLine($"• {asset.name}: không có \"{TextCu}\" — bỏ qua.");
        }

        if (apDung && soChuoiDoi > 0)
        {
            AssetDatabase.SaveAssets();
            Undo.SetCurrentGroupName("Sửa text tutorial 6 ô → 8 ô");
        }

        sb.AppendLine($"── Asset xét: {soAssetXet}/{TenAssetCanSua.Count} · chuỗi {(apDung ? "đã đổi" : "sẽ đổi")}: {soChuoiDoi}");
        if (soAssetXet < TenAssetCanSua.Count)
            sb.AppendLine("⚠ Thiếu asset trong danh sách cần sửa — kiểm tra tên file trong " + ThuMuc);
        if (!apDung && soChuoiDoi > 0)
            sb.AppendLine("→ Ưng ý thì chạy menu '... - APPLY'.");

        Debug.Log(sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  KIỂM TRA HẠT KHỞI ĐẦU — chỉ báo cáo, không sửa gì
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tìm StarterInventorySetup trong scene đang mở, in danh sách starterItems ở Inspector
    /// và CẢNH BÁO nếu nó ghi đè mặc định trong code (lúa ≠ 8, hướng dương ≠ 6).
    /// Lưu ý: khi list Inspector KHÔNG rỗng thì code dùng list đó thay cho mặc định.
    /// </summary>
    [MenuItem(MenuGoc + "Kiem tra hat khoi dau (chi bao cao)", false, 310)]
    public static void KiemTraHatKhoiDau()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ [KiemTraHatKhoiDau] Chỉ báo cáo — không sửa gì ═══");
        sb.AppendLine($"Mặc định trong code: seed_rice = {StarterInventorySetup.SO_HAT_LUA_KHOI_DAU}, " +
                      $"seed_huong_duong = {StarterInventorySetup.SO_HAT_HUONG_DUONG_KHOI_DAU}");

        var cacSetup = Object.FindObjectsByType<StarterInventorySetup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (cacSetup == null || cacSetup.Length == 0)
        {
            sb.AppendLine("⚠ Không có StarterInventorySetup nào trong scene đang mở. Mở scene Farm rồi chạy lại.");
            Debug.LogWarning(sb.ToString());
            return;
        }

        bool coCanhBao = false;
        foreach (var setup in cacSetup)
        {
            var so   = new SerializedObject(setup);
            var list = so.FindProperty("starterItems");
            sb.AppendLine($"• {DuongDanHierarchy(setup.transform)} — starterItems: {(list == null ? "?" : list.arraySize.ToString())} mục");

            if (list == null || list.arraySize == 0)
            {
                sb.AppendLine("    (list rỗng ⇒ dùng mặc định trong code — OK)");
                continue;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                var muc    = list.GetArrayElementAtIndex(i);
                string id  = muc.FindPropertyRelative("itemId")?.stringValue ?? "";
                int amount = muc.FindPropertyRelative("amount")?.intValue ?? 0;
                string ghiChu = "";

                if (id == "seed_rice" && amount != StarterInventorySetup.SO_HAT_LUA_KHOI_DAU)
                { ghiChu = $"  ⚠ GHI ĐÈ mặc định (code = {StarterInventorySetup.SO_HAT_LUA_KHOI_DAU})"; coCanhBao = true; }
                else if (id == "seed_huong_duong" && amount != StarterInventorySetup.SO_HAT_HUONG_DUONG_KHOI_DAU)
                { ghiChu = $"  ⚠ GHI ĐÈ mặc định (code = {StarterInventorySetup.SO_HAT_HUONG_DUONG_KHOI_DAU})"; coCanhBao = true; }

                sb.AppendLine($"    [{i}] {id} × {amount}{ghiChu}");
            }
        }

        if (coCanhBao)
        {
            sb.AppendLine("→ Inspector đang ghi đè số hạt. Muốn dùng 8/6 thì sửa list trong Inspector (CẦN BẠN — tool không tự đụng scene).");
            Debug.LogWarning(sb.ToString());
        }
        else Debug.Log(sb.ToString());
    }

    private static string DuongDanHierarchy(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
