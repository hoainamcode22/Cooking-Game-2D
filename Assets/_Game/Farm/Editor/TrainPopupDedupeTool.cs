using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// DỌN POPUP TÀU BỊ NHÂN BẢN — sửa lỗi "3 popup đè nhau, 2 nút X thừa".
/// ══════════════════════════════════════════════════════════════════════════════
///
/// NGUYÊN NHÂN GỐC (đo ngày 04/09, KHÔNG phải do Sếp kéo prefab):
///   `TrainStationBuilding.FindPopupCanvas()` duyệt FindObjectsByType&lt;Canvas&gt;() — hàm này
///   KHÔNG bảo đảm thứ tự — rồi nhận BỪA canvas đầu tiên có chữ "Popup"/"UI" trong tên.
///   Nó thường vớ phải **Canvas_StallPopup** (quầy hàng) thay vì **Canvas_Popup**.
///   `EnsurePopupsExist()` Instantiate popup vào đó, không ai tắt, rồi các tool
///   [InitializeOnLoad] tự lưu scene ⇒ bản thừa nằm vĩnh viễn trong file scene.
///
///   Có từ commit 01/09 chứ không phải hôm nay. Hàm FindPopupCanvas đã được vá ở vòng 13.
///   Tool này dọn nốt các bản đã lỡ sinh ra.
///
/// GIỮ LẠI bản nằm dưới `Canvas_Popup` (đúng chỗ). XOÁ các bản trùng ở canvas khác.
/// Có DRY-RUN, có Undo (Ctrl+Z), KHÔNG tự lưu scene.
///
/// [Train]
/// </summary>
public static class TrainPopupDedupeTool
{
    private const string MENU_DRY   = "Tools/Farm Game/Train/★ Dọn popup tàu bị nhân bản (DRY-RUN)";
    private const string MENU_APPLY = "Tools/Farm Game/Train/★ Dọn popup tàu bị nhân bản (APPLY)";

    private static readonly string[] TEN_POPUP =
    {
        "Popup_train",
        "Popup_item_Train",
        "Popup_Train_MasterStation",
    };

    private const string CANVAS_DUNG = "Canvas_Popup";

    [MenuItem(MENU_DRY, false, 80)]
    private static void DryRun() { Chay(false); }

    [MenuItem(MENU_APPLY, false, 81)]
    private static void Apply() { Chay(true); }

    private static void Chay(bool ghiThat)
    {
        var bc = new StringBuilder();
        bc.AppendLine($"╔══ [TrainDedupe] {(ghiThat ? "APPLY" : "DRY-RUN")} — dọn popup tàu nhân bản ══");

        // Quét MỌI Transform trong scene, kể cả object đang tắt.
        var tatCa = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var canXoa = new List<GameObject>();
        int soGiu = 0;

        foreach (string ten in TEN_POPUP)
        {
            var ds = tatCa.Where(t => t != null && LaTenPopup(t.name, ten))
                          .Select(t => t.gameObject)
                          .Distinct()
                          .ToList();

            bc.AppendLine("║");
            bc.AppendLine($"║ ── {ten}: tìm thấy {ds.Count} bản");

            if (ds.Count == 0)
            {
                bc.AppendLine("║    · không có bản nào — bỏ qua");
                continue;
            }

            // Ưu tiên giữ bản nằm dưới Canvas_Popup; không có thì giữ bản đầu tiên.
            GameObject giu = ds.FirstOrDefault(g => DuongDan(g.transform).Contains(CANVAS_DUNG)) ?? ds[0];
            soGiu++;

            foreach (var g in ds)
            {
                string dd = DuongDan(g.transform);
                if (g == giu)
                {
                    bc.AppendLine($"║    ✔ GIỮ  {dd}  (active={g.activeSelf})");
                }
                else
                {
                    bc.AppendLine($"║    ✖ XOÁ  {dd}  (active={g.activeSelf})");
                    canXoa.Add(g);
                }
            }
        }

        bc.AppendLine("║");
        bc.AppendLine($"║ TỔNG: giữ {soGiu} bản đúng · xoá {canXoa.Count} bản thừa");

        if (canXoa.Count == 0)
        {
            bc.AppendLine("║ ✅ Scene đã sạch, không có bản thừa nào.");
            bc.AppendLine("╚════════════════════════════════════════════════════════════");
            Debug.Log(bc.ToString());
            return;
        }

        if (!ghiThat)
        {
            bc.AppendLine("║ ⓘ DRY-RUN — CHƯA xoá gì. Đọc kỹ danh sách XOÁ ở trên, đúng rồi thì chạy (APPLY).");
            bc.AppendLine("║ ⚠ Sau khi APPLY nhớ kiểm lại reference trên TrainManager (loadPopup / processPopup)");
            bc.AppendLine("║   — nếu nó đang trỏ vào bản bị xoá thì phải kéo lại bản còn giữ.");
            bc.AppendLine("╚════════════════════════════════════════════════════════════");
            Debug.Log(bc.ToString());
            return;
        }

        var scene = canXoa[0].scene;
        foreach (var g in canXoa)
            Undo.DestroyObjectImmediate(g);   // Undo.* ⇒ Ctrl+Z lấy lại được

        EditorSceneManager.MarkSceneDirty(scene);

        bc.AppendLine($"║ ✅ ĐÃ XOÁ {canXoa.Count} object thừa.");
        bc.AppendLine("║ 🔴 BẤM Ctrl+S ĐỂ LƯU SCENE (tool cố ý không tự lưu).");
        bc.AppendLine("║ ⓘ Lỡ tay: Ctrl+Z rồi ĐỪNG lưu.");
        bc.AppendLine("║ ⚠ Kiểm lại Inspector TrainManager: loadPopup / processPopup còn trỏ đúng không.");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");
        Debug.Log(bc.ToString());
    }

    /// <summary>Khớp cả tên gốc lẫn bản Unity tự đánh số ("Popup_train (1)").</summary>
    private static bool LaTenPopup(string tenObject, string tenGoc)
    {
        if (tenObject == tenGoc) return true;
        return tenObject.StartsWith(tenGoc + " (") && tenObject.EndsWith(")");
    }

    private static string DuongDan(Transform t)
    {
        var sb = new StringBuilder(t.name);
        while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
        return sb.ToString();
    }
}
