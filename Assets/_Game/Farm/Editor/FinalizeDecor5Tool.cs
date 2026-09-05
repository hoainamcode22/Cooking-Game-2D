using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ★ NÚT CHỐT HẠ — gom 4 việc tay cuối cùng của gói "Nhân vật & Đồ trang trí 5 stage"
/// thành 1 cú bấm, theo lệnh trực tiếp của Sếp (2026-09-01: "fix sạch sẽ luôn tôi build 1 thể").
///
/// Việc sửa scene (thêm 4 item vào ShopManager.decorList) trước đây nằm trong DANH SÁCH DỪNG
/// của production/AUTONOMY.md §3.1 nên tool cũ cố ý không làm. Lệnh trực tiếp của Sếp
/// chính là phê duyệt — và tool này vẫn đi qua SerializedObject + Undo + hộp xác nhận,
/// không sửa file YAML bằng tay.
///
/// LÀM GÌ (đúng thứ tự):
///   1. Thêm 4 DecorData mới (itemID 16-19) vào ShopManager.decorList của scene đang mở
///      (bỏ qua cái đã có — idempotent, bấm 2 lần không nhân đôi).
///   2. Tick enabled = true trên 3 config: DecorGrowthConfig, BuilderWorkerConfig, ShipperConfig.
///   3. Tắt onlyDeliverToCompletedHouses (để test được ngay khi chưa nhà nào xây xong)
///      — NHỚ BẬT LẠI TRƯỚC KHI SHIP, tool in nhắc trong report.
///   4. Save scene (gồm cả Shipper_HomeAnchor đang dirty) + save asset.
///
/// Kèm nút TẮT KHẨN CẤP: enabled = false cả 3 config + save — 1 cú bấm là game về y như cũ.
/// </summary>
public static class FinalizeDecor5Tool
{
    private const string MenuRoot  = "Tools/Farm Game/";
    private const string MenuOn    = MenuRoot + "★ BẬT TOÀN BỘ GÓI Nhân vật + Decor 5 stage (1 nút cuối)";
    private const string MenuOff   = MenuRoot + "TẮT KHẨN CẤP toàn bộ gói (enabled = false cả 3)";

    private const string CfgDecor   = "Assets/_Game/Resources/DecorGrowthConfig.asset";
    private const string CfgWorker  = "Assets/_Game/Resources/BuilderWorkerConfig.asset";
    private const string CfgShipper = "Assets/_Game/Resources/ShipperConfig.asset";

    // 4 DecorData mới do DecorStageArtTool tạo — đường dẫn cố định, không đoán.
    private static readonly string[] NewItemPaths =
    {
        "Assets/_Game/Farm/CÔNG TRÌNH/Chau Cay Thu.asset",
        "Assets/_Game/Farm/CÔNG TRÌNH/Chu Lun.asset",
        "Assets/_Game/Farm/CÔNG TRÌNH/Gia Ban Rau.asset",
        "Assets/_Game/Farm/CÔNG TRÌNH/Binh Tuoi Hoa.asset",
    };

    [MenuItem(MenuOn, false, 0)]
    public static void BatToanBo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Đang Play Mode",
                "Thoát Play Mode trước rồi bấm lại — tool cần save scene.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("★ BẬT TOÀN BỘ GÓI",
            "Tool sẽ làm 4 việc:\n\n" +
            "1. Thêm 4 item decor mới vào ShopManager.decorList (scene đang mở)\n" +
            "2. Tick enabled = true trên 3 config (Decor / Worker / Shipper)\n" +
            "3. Tắt onlyDeliverToCompletedHouses (chỉ để test — nhớ bật lại trước khi ship)\n" +
            "4. SAVE scene + asset\n\n" +
            "Muốn tắt lại toàn bộ: menu \"TẮT KHẨN CẤP\" ngay cạnh.", "Làm luôn", "Thôi"))
            return;

        var sb = new StringBuilder();
        sb.AppendLine("[Tool] FinalizeDecor5Tool — BẬT TOÀN BỘ GÓI");
        sb.AppendLine("──────────────────────────────────────────────────────────────");

        int loi = 0;

        // ── 1) ShopManager.decorList ─────────────────────────────────────────
        ShopManager shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop == null)
        {
            sb.AppendLine("❌ Không tìm thấy ShopManager trong scene đang mở.");
            sb.AppendLine("   → Mở đúng scene Assets/_Game/Scenes/SCN_Farm.unity rồi bấm lại.");
            loi++;
        }
        else
        {
            var so = new SerializedObject(shop);
            SerializedProperty list = so.FindProperty("decorList");
            if (list == null || !list.isArray)
            {
                sb.AppendLine("❌ ShopManager không có field 'decorList' — cấu trúc đã đổi? Báo Lead.");
                loi++;
            }
            else
            {
                // gom itemID + reference đã có sẵn để idempotent
                var daCo = new HashSet<Object>();
                for (int i = 0; i < list.arraySize; i++)
                {
                    Object o = list.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (o != null) daCo.Add(o);
                }

                Undo.RecordObject(shop, "Thêm 4 decor mới vào decorList");
                int them = 0, trung = 0, thieu = 0;
                foreach (string path in NewItemPaths)
                {
                    var data = AssetDatabase.LoadAssetAtPath<BaseItemData>(path);
                    if (data == null)
                    {
                        sb.AppendLine("   ❌ thiếu asset: " + path +
                                      "  → chạy lại menu \"Tạo 4 DecorData item mới (APPLY)\".");
                        thieu++; continue;
                    }
                    if (daCo.Contains(data)) { trung++; continue; }
                    int idx = list.arraySize;
                    list.InsertArrayElementAtIndex(idx);
                    list.GetArrayElementAtIndex(idx).objectReferenceValue = data;
                    them++;
                    sb.AppendLine("   ✔ decorList += \"" + data.itemName + "\" (itemID=" + data.itemID + ")");
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(shop);
                EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
                sb.AppendLine("✔ decorList: thêm " + them + " · đã có sẵn " + trung +
                              " · thiếu asset " + thieu + " · tổng sau khi thêm: " + list.arraySize + " item.");
                if (thieu > 0) loi++;
            }
        }

        // ── 2) enabled = true ×3 (+ onlyDeliverToCompletedHouses = false) ────
        loi += DatCo(CfgDecor,   "enabled", true,  sb) ? 0 : 1;
        loi += DatCo(CfgWorker,  "enabled", true,  sb) ? 0 : 1;
        loi += DatCo(CfgShipper, "enabled", true,  sb) ? 0 : 1;
        if (DatCo(CfgShipper, "onlyDeliverToCompletedHouses", false, sb))
            sb.AppendLine("   ⚠ CHỈ ĐỂ TEST — trước khi ship, tick lại onlyDeliverToCompletedHouses " +
                          "(không nên giao hàng tới nhà đang là công trường).");

        // ── 3) SAVE tất cả ───────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        bool sceneSaved = EditorSceneManager.SaveOpenScenes();
        sb.AppendLine(sceneSaved
            ? "✔ ĐÃ SAVE scene (gồm cả Shipper_HomeAnchor còn dirty từ bước trước) + toàn bộ asset."
            : "❌ Save scene THẤT BẠI — tự bấm Ctrl+S và xem Console.");
        if (!sceneSaved) loi++;

        sb.AppendLine("──────────────────────────────────────────────────────────────");
        sb.AppendLine(loi == 0
            ? "[Tool] TỔNG KẾT: SẠCH — 0 lỗi. Bấm Play và test theo 7 kịch bản trong " +
              "production/TEAM_NHANVAT_DECOR5_2026-09-01.md PHẦN B7."
            : "[Tool] TỔNG KẾT: " + loi + " lỗi — đọc các dòng ❌ ở trên, xử xong bấm lại (an toàn, idempotent).");
        Debug.Log(sb.ToString());
    }

    [MenuItem(MenuOff, false, 1)]
    public static void TatKhanCap()
    {
        if (!EditorUtility.DisplayDialog("TẮT KHẨN CẤP",
            "Đặt enabled = false trên cả 3 config và save.\n" +
            "Game chạy lại Y NHƯ TRƯỚC KHI CÓ GÓI NÀY (code mới toàn bộ ngủ đông).\n" +
            "4 item mới vẫn nằm trong shop — chúng chỉ hiện ngay khi đặt, không qua 5 stage.",
            "Tắt", "Thôi"))
            return;

        var sb = new StringBuilder();
        sb.AppendLine("[Tool] FinalizeDecor5Tool — TẮT KHẨN CẤP");
        DatCo(CfgDecor,   "enabled", false, sb);
        DatCo(CfgWorker,  "enabled", false, sb);
        DatCo(CfgShipper, "enabled", false, sb);
        AssetDatabase.SaveAssets();
        sb.AppendLine("[Tool] Đã tắt + save. Muốn bật lại: menu ★ BẬT TOÀN BỘ GÓI.");
        Debug.Log(sb.ToString());
    }

    /// <summary>Đặt 1 field bool trên ScriptableObject qua SerializedObject. true = ok.</summary>
    private static bool DatCo(string assetPath, string field, bool value, StringBuilder sb)
    {
        var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (so == null)
        {
            sb.AppendLine("❌ thiếu config: " + assetPath + " → chạy tool SETUP tương ứng trước.");
            return false;
        }
        var ser = new SerializedObject(so);
        SerializedProperty p = ser.FindProperty(field);
        if (p == null || p.propertyType != SerializedPropertyType.Boolean)
        {
            sb.AppendLine("❌ " + System.IO.Path.GetFileNameWithoutExtension(assetPath) +
                          " không có field bool '" + field + "'.");
            return false;
        }
        bool cu = p.boolValue;
        p.boolValue = value;
        ser.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(so);
        sb.AppendLine("✔ " + System.IO.Path.GetFileNameWithoutExtension(assetPath) + "." + field +
                      ": " + cu + " → " + value + (cu == value ? " (đã đúng từ trước)" : ""));
        return true;
    }
}
