#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DEV TOOL — Quản lý các công trình/ô đất ĐÃ ĐẶT (mua trong play mode).
///
/// Vấn đề: vật được spawn lúc runtime từ save (PlayerPrefs "FARM_PLACED_BUILDINGS"),
/// KHÔNG nằm sẵn trên Hierarchy → stop play là biến mất nhưng Play lại hiện lại,
/// nên không xóa được bằng cách xóa GameObject thường.
///
/// Tool này đọc thẳng save → liệt kê từng vật → cho XÓA LẺ từng cái hoặc XÓA HẾT,
/// ngay tại Editor (KHÔNG cần vào Play). Lần Play sau sẽ không spawn lại vật đã xóa.
///
/// Menu: Tools/Farm Game/Dev/Placed Objects Manager
///
/// Ngoài ra, trong game (khi đang Edit Mode 1 vật): bấm phím Delete/Backspace,
/// hoặc nút Btn_Delete trên Ghost → PlacementManager.DeleteEditingBuilding().
/// </summary>
public class PlacedObjectsManagerTool : EditorWindow
{
    // PHẢI khớp PlacementManager.BuildingsSaveKey
    private const string SaveKey = "FARM_PLACED_BUILDINGS";

    // `rot` BẮT BUỘC phải có, dù tool này không dùng tới.
    // JsonUtility bỏ qua field lạ khi ĐỌC nhưng KHÔNG giữ lại khi GHI —
    // thiếu nó thì xoá lẻ 1 công trình sẽ làm MẤT HƯỚNG XOAY của tất cả công trình còn lại.
    [Serializable] private class Entry { public string itemId; public float x, y; public int plotId; public int rot; }

    // 🔴 `saveVersion` BẮT BUỘC PHẢI CÓ.
    // PlacementManager coi save THIẾU key này là **v0** và sẽ CHẠY LẠI phép chuyển đổi
    // toạ độ (MigrateAnchorV0ToV1) → mọi công trình DỊCH CHỖ LẦN THỨ HAI.
    // Tool này chỉ đọc-rồi-ghi-lại, nên thiếu 1 field là phá hỏng cả map.
    [Serializable] private class Save  { public int saveVersion; public List<Entry> list = new List<Entry>(); }

    private Save        _data;
    private Vector2     _scroll;

    [MenuItem("Tools/Farm Game/Dev/Placed Objects Manager")]
    public static void Open()
    {
        var w = GetWindow<PlacedObjectsManagerTool>("Placed Objects");
        w.minSize = new Vector2(420, 300);
        w.Reload();
    }

    private void OnEnable() => Reload();

    private void Reload()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        _data = string.IsNullOrEmpty(json) ? new Save() : JsonUtility.FromJson<Save>(json);
        if (_data == null) _data = new Save();
        if (_data.list == null) _data.list = new List<Entry>();
    }

    private void Persist()
    {
        // Save đang có vật nhưng saveVersion = 0 nghĩa là ta vừa đọc một save v0 CHƯA
        // được runtime dịch (hoặc JsonUtility không thấy key). Ghi lại nguyên 0 sẽ khiến
        // PlacementManager dịch toạ độ LẦN NỮA → cả map dịch chỗ.
        // Runtime luôn dịch + ghi lại v1 ngay lần Play đầu, nên tới lúc tool này chạy thì
        // giá trị đúng phải là CurrentSaveVersion.
        if (_data.saveVersion == 0 && _data.list.Count > 0)
        {
            _data.saveVersion = PlacementManager.CurrentSaveVersion;
            Debug.LogWarning("[PlacedObjects] save thiếu saveVersion — đã đặt thành " +
                             $"{PlacementManager.CurrentSaveVersion} để tránh dịch toạ độ lần hai.");
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_data));
        PlayerPrefs.Save();
        Debug.Log($"[PlacedObjects] Đã lưu save — còn {_data.list.Count} vật. " +
                  "Lần Play sau sẽ áp dụng.");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Công trình / Ô đất đã đặt (từ save)", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "⚠ TOOL CŨ — nên dùng bản mới:  Tools ▸ Farm ▸ Dọn Dẹp Dữ Liệu Đã Lưu\n\n" +
            "Bản mới hơn ở chỗ: hiện TÊN THẬT (\"Chậu Hoa1\") thay vì id dạng số, bao cả " +
            "công trường đang xây, dò được dữ liệu ô đất mồ côi, có tìm kiếm và chọn nhiều.",
            MessageType.Warning);

        if (GUILayout.Button("Mở tool mới", GUILayout.Height(22)))
        {
            FarmSaveCleanupTool.Open();
            Close();
            // ExitGUI thay vì `return`: Close() giữa OnGUI làm số control vẽ ra ở frame
            // này khác frame trước → IMGUI ném "Getting control ... in a group with only
            // N controls". ExitGUI cắt sạch vòng vẽ, không sinh lỗi đó.
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Vật mua trong play mode được spawn từ save, không nằm trên Hierarchy.\n" +
            "Bấm [Xóa] để gỡ vật đó khỏi save → lần Play sau sẽ KHÔNG hiện lại nữa.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("↻ Tải lại", GUILayout.Height(24))) Reload();
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("🗑 XÓA TẤT CẢ", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Xóa tất cả?",
                    $"Xóa toàn bộ {_data.list.Count} vật đã đặt khỏi save?\nKhông thể hoàn tác.",
                    "Xóa hết", "Hủy"))
                {
                    PlayerPrefs.DeleteKey(SaveKey);
                    PlayerPrefs.Save();
                    _data = new Save();
                    Debug.Log("[PlacedObjects] Đã xóa TẤT CẢ vật đã đặt khỏi save.");
                }
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(4);

        if (_data.list.Count == 0)
        {
            EditorGUILayout.HelpBox("Save trống — chưa có vật nào được đặt.", MessageType.None);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        int removeIndex = -1;
        for (int i = 0; i < _data.list.Count; i++)
        {
            var e = _data.list[i];
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"#{i + 1}  {(string.IsNullOrEmpty(e.itemId) ? "(no id)" : e.itemId)}",
                    GUILayout.Width(160));
                EditorGUILayout.LabelField($"({e.x:0}, {e.y:0})  plot:{e.plotId}");
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Xóa", GUILayout.Width(60)))
                    removeIndex = i;
                GUI.backgroundColor = Color.white;
            }
        }
        EditorGUILayout.EndScrollView();

        if (removeIndex >= 0)
        {
            var e = _data.list[removeIndex];
            _data.list.RemoveAt(removeIndex);
            Persist();
            Debug.Log($"[PlacedObjects] Đã xóa vật '{e.itemId}' tại ({e.x:0},{e.y:0}).");
            Repaint();
        }
    }

    [MenuItem("Tools/Farm Game/Dev/Placed Objects Manager", true)]
    private static bool Validate() => !EditorApplication.isPlaying;
}
#endif
