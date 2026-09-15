#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UNITY EDITOR TOOL: Batch Convert Text to English (Scene & Prefabs)
/// ══════════════════════════════════════════════════════════════════════════════
/// Tự động quét toàn bộ TMP_Text trong scene hiện tại (và toàn bộ Prefab nếu muốn),
/// tra cứu bảng từ điển LocStringTable.EN (tra khớp chính xác và tra không phân biệt hoa thường)
/// để thay thế các chuỗi tiếng Việt thành tiếng Anh chuẩn quốc tế.
///
/// Tính năng:
///   • Hỗ trợ Undo.RecordObject đầy đủ (hoàn tác Ctrl+Z an toàn).
///   • Tra cứu chính xác và tra cứu Case-Insensitive, bảo toàn định dạng IN HOA.
///   • Tự động xử lý khoảng trắng / xuống dòng đầu cuối chuỗi.
///   • Bỏ qua các ô nhập liệu (TMP_InputField) và các text gắn thẻ [NoLoc].
///   • Log chi tiết từng text đã thay đổi kèm đường dẫn Hierarchy vào Console.
///   • Tự động đánh dấu Dirty cho Scene hoặc Prefab để người dùng lưu lại (Ctrl+S).
/// </summary>
public static class BatchTranslateTextToEnglishTool
{
    private static Dictionary<string, string> _bangKhongPhanBietHoaThuong;

    private static void EnsureDictionary()
    {
        if (_bangKhongPhanBietHoaThuong == null)
        {
            _bangKhongPhanBietHoaThuong = new Dictionary<string, string>(
                LocStringTable.EN.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in LocStringTable.EN)
            {
                if (!_bangKhongPhanBietHoaThuong.ContainsKey(kvp.Key))
                {
                    _bangKhongPhanBietHoaThuong[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    [MenuItem("Tools/Farm Game/Localization/Batch Convert Text In Scene To English", false, 100)]
    public static void BatchConvertActiveScene()
    {
        EnsureDictionary();

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không có Scene nào đang được mở!", "OK");
            return;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        List<TMP_Text> allTexts = new List<TMP_Text>();
        foreach (var root in rootObjects)
        {
            allTexts.AddRange(root.GetComponentsInChildren<TMP_Text>(true));
        }

        int count = 0;
        int skipped = 0;
        StringBuilder logReport = new StringBuilder();
        logReport.AppendLine($"═════════ [BATCH CONVERT TEXT TO ENGLISH - SCENE '{activeScene.name}'] ═════════");

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Batch Convert Text In Scene To English");
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < allTexts.Count; i++)
        {
            TMP_Text tmp = allTexts[i];
            if (tmp == null) continue;

            if (LaChuKhongDuocDich(tmp))
            {
                skipped++;
                continue;
            }

            string original = tmp.text;
            if (TryTranslate(original, out string translated) && original != translated)
            {
                Undo.RecordObject(tmp, "Batch Convert Text To English");
                tmp.text = translated;
                EditorUtility.SetDirty(tmp);

                string path = GetHierarchyPath(tmp.transform);
                logReport.AppendLine($"  ✓ [{path}]");
                logReport.AppendLine($"    • VN: \"{original.Replace("\n", "\\n")}\"");
                logReport.AppendLine($"    • EN: \"{translated.Replace("\n", "\\n")}\"");
                count++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            logReport.AppendLine($"══════════════════════════════════════════════════════════════════════");
            logReport.AppendLine($"Tổng kết: Đã chuyển đổi {count} TMP_Text sang tiếng Anh. Bỏ qua {skipped} text hệ thống.");
            Debug.Log(logReport.ToString());

            EditorUtility.DisplayDialog(
                "Chuyển đổi Text sang Tiếng Anh hoàn tất",
                $"Đã chuyển đổi thành công {count} TMP_Text trong Scene '{activeScene.name}' sang tiếng Anh!\n\n" +
                $"Bỏ qua: {skipped} text (ô nhập liệu / NoLoc).\n" +
                $"Chi tiết danh sách vui lòng xem tại Unity Console.\n\n" +
                $"Nhấn Ctrl+S để lưu Scene.",
                "OK");
        }
        else
        {
            Debug.Log($"[LocTool] Scene '{activeScene.name}' không có TMP_Text nào cần chuyển đổi (toàn bộ đã là tiếng Anh hoặc chưa có trong từ điển).");
            EditorUtility.DisplayDialog(
                "Thông báo",
                $"Không có TMP_Text nào cần chuyển đổi trong Scene '{activeScene.name}'!\n" +
                $"(Các text đã là tiếng Anh hoặc không nằm trong từ điển LocStringTable.EN).",
                "OK");
        }
    }

    [MenuItem("Tools/Farm Game/Localization/Batch Convert Text In All Prefabs To English", false, 101)]
    public static void BatchConvertAllPrefabs()
    {
        EnsureDictionary();

        if (!EditorUtility.DisplayDialog(
            "Xác nhận chuyển đổi Prefabs",
            "Tool sẽ quét toàn bộ Prefabs trong thư mục Assets/_Game và chuyển đổi các TMP_Text sang tiếng Anh.\n\nBạn có muốn tiếp tục?",
            "Tiếp tục", "Huỷ"))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game" });
        int totalModifiedPrefabs = 0;
        int totalConvertedTexts = 0;
        StringBuilder logReport = new StringBuilder();
        logReport.AppendLine("═════════ [BATCH CONVERT TEXT TO ENGLISH - PREFABS] ═════════");

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Batch Convert Prefabs", $"Đang kiểm tra: {path}", (float)i / guids.Length);

                GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);
                if (prefabContents == null) continue;

                TMP_Text[] texts = prefabContents.GetComponentsInChildren<TMP_Text>(true);
                int prefabConvertedCount = 0;

                foreach (var tmp in texts)
                {
                    if (tmp == null || LaChuKhongDuocDich(tmp)) continue;

                    string original = tmp.text;
                    if (TryTranslate(original, out string translated) && original != translated)
                    {
                        tmp.text = translated;
                        prefabConvertedCount++;
                        logReport.AppendLine($"  ✓ [{path} :: {GetHierarchyPath(tmp.transform)}]");
                        logReport.AppendLine($"    • VN: \"{original.Replace("\n", "\\n")}\"");
                        logReport.AppendLine($"    • EN: \"{translated.Replace("\n", "\\n")}\"");
                    }
                }

                if (prefabConvertedCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
                    totalModifiedPrefabs++;
                    totalConvertedTexts += prefabConvertedCount;
                }

                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        logReport.AppendLine($"══════════════════════════════════════════════════════════════════════");
        logReport.AppendLine($"Tổng kết Prefab: Đã chuyển đổi {totalConvertedTexts} TMP_Text trong {totalModifiedPrefabs} Prefabs.");
        Debug.Log(logReport.ToString());

        EditorUtility.DisplayDialog(
            "Hoàn tất chuyển đổi Prefabs",
            $"Đã chuyển đổi thành công {totalConvertedTexts} TMP_Text trong {totalModifiedPrefabs} Prefabs!\nChi tiết xem tại Unity Console.",
            "OK");
    }

    /// <summary>
    /// Tra cứu chuỗi tiếng Anh tương ứng từ từ điển LocStringTable.EN.
    /// Tra khớp chính xác -> Tra khớp không phân biệt hoa thường -> Tra khớp chuỗi đã tỉa khoảng trắng.
    /// </summary>
    public static bool TryTranslate(string originalText, out string translatedText)
    {
        translatedText = originalText;
        if (string.IsNullOrWhiteSpace(originalText)) return false;

        // 1. Tra cứu chính xác
        if (LocStringTable.EN.TryGetValue(originalText, out string en) && !string.IsNullOrEmpty(en))
        {
            translatedText = en;
            return true;
        }

        // 2. Tra cứu chuỗi đã cắt khoảng trắng đầu/cuối
        string trimmed = originalText.Trim();
        if (LocStringTable.EN.TryGetValue(trimmed, out en) && !string.IsNullOrEmpty(en))
        {
            translatedText = originalText.Replace(trimmed, en);
            return true;
        }

        // 3. Tra cứu không phân biệt hoa thường
        EnsureDictionary();
        if (_bangKhongPhanBietHoaThuong.TryGetValue(originalText, out en) && !string.IsNullOrEmpty(en))
        {
            translatedText = KhopKieuChu(originalText, en);
            return true;
        }

        // 4. Tra cứu không phân biệt hoa thường cho chuỗi trimmed
        if (_bangKhongPhanBietHoaThuong.TryGetValue(trimmed, out en) && !string.IsNullOrEmpty(en))
        {
            string matched = KhopKieuChu(trimmed, en);
            translatedText = originalText.Replace(trimmed, matched);
            return true;
        }

        return false;
    }

    private static string KhopKieuChu(string goc, string en)
    {
        bool coChuCai = false;
        for (int i = 0; i < goc.Length; i++)
        {
            if (char.IsLower(goc[i])) return en;
            if (char.IsUpper(goc[i])) coChuCai = true;
        }
        return coChuCai ? en.ToUpperInvariant() : en;
    }

    private static bool LaChuKhongDuocDich(TMP_Text t)
    {
        if (t.GetComponentInParent<TMP_InputField>() != null) return true;
        Transform tr = t.transform;
        while (tr != null)
        {
            if (tr.name.Contains("[NoLoc]") || tr.name.StartsWith("~Loc")) return true;
            tr = tr.parent;
        }
        return false;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
