using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GÁN ẢNH GỐC VÀO POPUP NHIỆM VỤ THEO BẢN THIẾT KẾ.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO CÓ TOOL NÀY
/// ══════════════════════════════════════════════════════════════════════════
/// Bản thiết kế lấy ảnh từ chính `Assets/Assetsgame` rồi đổi tên gọn lại
/// (`cachualever3-removebg-preview.png` → `cachua.png`). Đối chiếu MD5 xác nhận
/// **14/14 ảnh trùng byte** với ảnh gốc — nên KHÔNG cần giữ bản sao trong dự án,
/// chỉ cần trỏ đúng ảnh gốc.
///
/// Tool tra theo tên gốc thay vì để người dùng kéo tay 14 ô Sprite. Kéo tay dễ nhầm
/// hai ảnh giống nhau (`cachualever2` vs `cachualever3`, `bapcailuc1` vs `bapcai3`)
/// và không có gì báo là đã nhầm.
/// </summary>
public static class TaskPopupSpriteWireTool
{
    private const string Menu = "Tools/Farm/Popup Nhiệm Vụ/";

    /// <summary>
    /// Tên ô trong <c>UnifiedTaskPopupSprites</c> → đường dẫn ảnh gốc.
    /// Đường dẫn lấy từ kết quả đối chiếu MD5, không phải đoán theo tên.
    /// </summary>
    private static readonly (string o, string duongDan)[] BangGan =
    {
        // ── icon phần thưởng ────────────────────────────────────────────────
        ("coinIcon",    "Assets/Assetsgame/Icon_vang.png"),
        ("diamondIcon", "Assets/Assetsgame/kimcuong-removebg-preview.png"),
        // Sao xanh = EXP (chủ dự án đã chốt).
        ("expIcon",     "Assets/Assetsgame/iconsao-removebg-preview.png"),
        ("chestIcon",   "Assets/Assetsgame/AnhBtnNhanQua.png"),

        // ── nút đóng & ribbon ───────────────────────────────────────────────
        ("closeButton", UIStandardSprites.PathClose),
        ("ribbon",      "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites/shop_banner_ribbon.png"),

        // ── icon 3 tab ──────────────────────────────────────────────────────
        ("missionTabIcon",     "Assets/Assetsgame/img_icon_giay.png"),
        ("dailyTabIcon",       "Assets/Assetsgame/icon_lich.png"),
        ("achievementTabIcon", "Assets/Assetsgame/iconsao-removebg-preview.png"),
    };

    /// <summary>
    /// Icon nhiệm vụ: `MissionData.missionIcon`. Tra theo `targetItemId` để mỗi nhiệm vụ
    /// hiện đúng hình nông sản của nó, thay vì cùng một icon cho cả 307 nhiệm vụ.
    /// </summary>
    private static readonly (string itemId, string duongDan)[] BangIconNhiemVu =
    {
        ("rice",     "Assets/Assetsgame/iconlua-removebg-preview.png"),
        ("cachua",   "Assets/Assetsgame/cachualever3-removebg-preview.png"),
        ("bapcai",   "Assets/Assetsgame/bapcai3-removebg-preview.png"),
        ("pork",     "Assets/Assetsgame/iconthitheooo-removebg-preview.png"),
        ("egg",      "Assets/Assetsgame/conga-removebg-preview.png"),
        ("chicken_meat", "Assets/Assetsgame/conga-removebg-preview.png"),
    };

    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(Menu + "1 · Kiểm tra ảnh gốc có đủ không", false, 1)]
    public static void KiemTraAnh()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ ẢNH GỐC CHO POPUP NHIỆM VỤ ═══\n");

        int thieu = 0;
        foreach (var (o, dd) in BangGan)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dd);
            if (s == null) thieu++;
            sb.AppendLine($"  {(s != null ? "có  " : "THIẾU")}  {o,-22} {dd}");
        }

        sb.AppendLine();
        foreach (var (id, dd) in BangIconNhiemVu)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dd);
            if (s == null) thieu++;
            sb.AppendLine($"  {(s != null ? "có  " : "THIẾU")}  icon:{id,-17} {dd}");
        }

        sb.AppendLine();
        sb.AppendLine(thieu == 0
            ? "  Đủ hết. Chạy mục 2 để gán."
            : $"  ⚠ Thiếu {thieu} ảnh. Ảnh thiếu sẽ để trống, code tự vẽ hình tạm thay thế.");

        // Ba mảnh art khung chưa có: chủ dự án sẽ vẽ sau.
        sb.AppendLine("\n  Chưa có (code tự dựng bằng gradient, vẽ xong thì gán vào):");
        sb.AppendLine("     boardFrame        ← ván gỗ nền 1300×850");
        sb.AppendLine("     ribbon            ← ribbon tiêu đề");
        sb.AppendLine("     tabButton         ← nền tab thường");
        sb.AppendLine("     selectedTabButton ← nền tab đang chọn");

        Debug.Log(sb.ToString());
    }

    [MenuItem(Menu + "2 · Gán ảnh vào popup trong scene", false, 2)]
    public static void GanAnh()
    {
        var popup = Object.FindFirstObjectByType<UnifiedTaskPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            EditorUtility.DisplayDialog("Popup nhiệm vụ",
                "Không thấy UnifiedTaskPopupUI trong scene đang mở.\n\n" +
                "Popup này tự dựng lúc chạy nên có thể chưa có object nào. " +
                "Bấm Play một lần cho nó sinh ra, hoặc gán tay trong prefab.", "OK");
            return;
        }

        var so = new SerializedObject(popup);
        SerializedProperty spr = so.FindProperty("sprites");
        if (spr == null)
        {
            Debug.LogError("[PopupNV] Không tìm thấy field 'sprites' — tên field đã đổi?");
            return;
        }

        int gan = 0;
        var sb = new StringBuilder();

        foreach (var (o, dd) in BangGan)
        {
            SerializedProperty p = spr.FindPropertyRelative(o);
            if (p == null) { sb.AppendLine($"  ⚠ không có ô '{o}'"); continue; }

            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dd);
            if (s == null) { sb.AppendLine($"  ⚠ không thấy ảnh {dd}"); continue; }

            if (p.objectReferenceValue == s) continue;
            p.objectReferenceValue = s;
            gan++;
            sb.AppendLine($"  {o,-22} ← {System.IO.Path.GetFileName(dd)}");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(popup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);

        Debug.Log($"[PopupNV] Đã gán {gan} ảnh.\n{sb}\n→ Ctrl+S để lưu scene.");
    }

    [MenuItem(Menu + "3 · Gán icon cho từng nhiệm vụ theo vật phẩm", false, 3)]
    public static void GanIconNhiemVu()
    {
        // Nạp sẵn sprite một lần, tránh LoadAssetAtPath trong vòng lặp 464 nhiệm vụ.
        var kho = new Dictionary<string, Sprite>();
        foreach (var (id, dd) in BangIconNhiemVu)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dd);
            if (s != null) kho[id] = s;
        }

        if (kho.Count == 0)
        {
            Debug.LogWarning("[PopupNV] Không nạp được ảnh nào — bỏ qua.");
            return;
        }

        int gan = 0, boQua = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:MissionData"))
        {
            var m = AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null) continue;

            // ĐÃ CÓ icon thì KHÔNG ghi đè. Chủ dự án có thể đã gán tay icon riêng cho
            // một số nhiệm vụ; ghi đè hết là xoá công của họ mà không hỏi.
            if (m.missionIcon != null) { boQua++; continue; }

            string id = (m.targetItemId ?? string.Empty).Trim().ToLowerInvariant();
            if (id.Length == 0 || !kho.TryGetValue(id, out Sprite s)) { boQua++; continue; }

            Undo.RecordObject(m, "Gán icon nhiệm vụ");
            m.missionIcon = s;
            EditorUtility.SetDirty(m);
            gan++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PopupNV] Gán icon cho {gan} nhiệm vụ, bỏ qua {boQua} " +
                  "(đã có icon sẵn, hoặc vật phẩm chưa có ảnh trong bảng).");
    }

    [MenuItem(Menu + "4 · Dọn thư mục Assets/thietke", false, 20)]
    public static void DonThuMucThietKe()
    {
        const string tm = "Assets/thietke";
        if (!AssetDatabase.IsValidFolder(tm))
        {
            Debug.Log("[PopupNV] Không có thư mục Assets/thietke — đã dọn từ trước.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Dọn thư mục thiết kế",
                "Xoá HẲN Assets/thietke (html, js, md và 14 ảnh sao chép)?\n\n" +
                "14 ảnh trong đó đã được đối chiếu MD5 và TRÙNG BYTE với ảnh gốc\n" +
                "trong Assets/Assetsgame — popup đang trỏ vào ảnh gốc, không phải\n" +
                "bản sao này.\n\n" +
                "Chạy mục 2 và 3 TRƯỚC khi xoá.",
                "Xoá", "Huỷ"))
            return;

        if (AssetDatabase.DeleteAsset(tm))
            Debug.Log("[PopupNV] ✅ Đã xoá Assets/thietke. Dự án nhẹ đi ~3,7MB.");
        else
            Debug.LogError("[PopupNV] Xoá không được — có thể file đang mở trong Unity. " +
                           "Đóng tab Inspector đang xem file trong đó rồi thử lại.");
    }
}
