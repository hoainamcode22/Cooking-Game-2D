#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dọn các object UI bị BỎ RƠI ở GỐC SCENE.
///
/// Nguồn gốc: BuildingProcessUIBuilderTool trước đây gọi Transform.SetParent() vào transform
/// nằm trong PREFAB ASSET. Unity cấm việc này, nên object vừa new GameObject() không gắn được
/// vào prefab mà rơi thẳng ra gốc scene đang mở. Mỗi lần chạy tool đó lại thêm một loạt object rác.
/// Lỗi gốc đã sửa (dùng PrefabUtility.LoadPrefabContents); tool này dọn phần rác đã sinh ra.
///
/// An toàn: CHỈ xoá object thoả ĐỦ 3 điều kiện, và luôn hỏi + liệt kê trước khi xoá:
///   1. nằm ở GỐC scene (không có cha),
///   2. có RectTransform nhưng KHÔNG có Canvas nào ở trên  -> UI mồ côi, không thể hiển thị,
///   3. tên nằm trong danh sách tên mà CreateRect() của tool kia sinh ra.
/// Ba điều kiện cùng lúc thì gần như không thể trùng với object thật của designer.
/// </summary>
public static class DonRacUIGocSceneTool
{
    private const string Menu = "Tools/Farm/Don Rac UI Mo Coi O Goc Scene";

    /// <summary>Tên do CreateRect() trong BuildingProcessUIBuilderTool sinh ra.</summary>
    private static readonly HashSet<string> TenRac = new HashSet<string>
    {
        "Txt_PenName", "Txt_CropName", "Track_Bar", "Progress_Fill",
        "Txt_TimeRemaining", "Btn_SpeedUp", "Icon_Diamond", "Txt_GemCost",
    };

    [MenuItem(Menu, false, 300)]
    public static void Don()
    {
        var nghiRac = new List<GameObject>();

        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null) continue;
            if (go.transform.parent != null) continue;                    // 1. phải ở gốc scene
            if (go.GetComponent<RectTransform>() == null) continue;       // 2a. phải là object UI
            if (go.GetComponentInParent<Canvas>() != null) continue;      // 2b. không nằm dưới Canvas nào
            if (!TenRac.Contains(go.name)) continue;                      // 3. tên khớp danh sách
            nghiRac.Add(go);
        }

        if (nghiRac.Count == 0)
        {
            EditorUtility.DisplayDialog("Dọn rác UI",
                "Không tìm thấy object UI mồ côi nào ở gốc scene.\nScene sạch.", "OK");
            return;
        }

        var ds = new StringBuilder();
        for (int i = 0; i < nghiRac.Count; i++)
            ds.AppendLine($"{i + 1}. {nghiRac[i].name}   (con: {nghiRac[i].transform.childCount})");

        bool ok = EditorUtility.DisplayDialog("Dọn rác UI mồ côi",
            $"Tìm thấy {nghiRac.Count} object UI mồ côi ở GỐC scene:\n\n{ds}\n"
            + "Chúng có RectTransform nhưng không nằm dưới Canvas nào nên KHÔNG THỂ hiển thị — "
            + "là rác do lỗi SetParent vào prefab asset.\n\nXoá chúng?\n(Ctrl+Z hoàn tác được.)",
            "Xoá", "Hủy");
        if (!ok) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Don rac UI mo coi");

        var log = new StringBuilder();
        int dem = 0;
        foreach (var go in nghiRac)
        {
            log.AppendLine($"- xoá '{go.name}' (con: {go.transform.childCount})");
            Undo.DestroyObjectImmediate(go);
            dem++;
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        Debug.Log($"[DonRac] Đã xoá {dem} object UI mồ côi ở gốc scene:\n{log}");
        EditorUtility.DisplayDialog("Dọn rác UI",
            $"Đã xoá {dem} object.\n\nCtrl+S để lưu scene.\n(Ctrl+Z nếu muốn hoàn tác.)", "OK");
    }
}
#endif
