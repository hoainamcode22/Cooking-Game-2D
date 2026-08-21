#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  M0-2 — Tool Editor cho SaveSystem. ĐẶT TRONG THƯ MỤC Editor/ (bắt buộc —
//  tham chiếu UnityEditor, để ngoài là build device vỡ).
//
//  Menu: Tools/Farm Game/Save/…
//  Kèm hook EditorApplication.playModeStateChanged: auto-save NGAY TRƯỚC khi
//  thoát Play Mode — đây là lối thoát duy nhất SaveBootstrap không tự bắt được
//  một cách đáng tin (OnApplicationQuit trong Editor không phải lúc nào cũng nổ).
// ═══════════════════════════════════════════════════════════════════════════
public static class SaveDebugTool
{
    private const string Menu = "Tools/Farm Game/Save/";

    // ── Hook thoát Play Mode ────────────────────────────────────────────────
    [InitializeOnLoadMethod]
    private static void InstallPlayModeHook()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
    }

    private static void HandlePlayModeChanged(PlayModeStateChange change)
    {
        // ExitingPlayMode: mọi object còn SỐNG → capture được qua manager Instance.
        if (change != PlayModeStateChange.ExitingPlayMode) return;

        SaveSystem.Save("editor-exit-play");
        Debug.Log("[Save] Auto-save khi thoát Play Mode (Editor hook).");
    }

    // ── Menu ────────────────────────────────────────────────────────────────

    [MenuItem(Menu + "Open Save Folder", false, 1)]
    private static void OpenSaveFolder()
    {
        string dir = Application.persistentDataPath;
        if (File.Exists(SaveSystem.SavePath))
            EditorUtility.RevealInFinder(SaveSystem.SavePath);
        else
            EditorUtility.RevealInFinder(dir);
        Debug.Log($"[Save] Thư mục save: {dir}");
    }

    [MenuItem(Menu + "Show Save JSON (log)", false, 2)]
    private static void ShowSaveJson()
    {
        if (!File.Exists(SaveSystem.SavePath))
        {
            Debug.LogWarning($"[Save] Chưa có file {SaveSystem.SavePath}. Bấm Save Now trước.");
            return;
        }

        string json = File.ReadAllText(SaveSystem.SavePath);
        const int maxLog = 12000;   // console Unity cắt log quá dài — cắt chủ động cho gọn
        if (json.Length > maxLog)
            Debug.Log($"[Save] save.json ({json.Length} ký tự, hiện {maxLog} đầu):\n" +
                      json.Substring(0, maxLog) + "\n… (mở file để xem đủ)");
        else
            Debug.Log($"[Save] save.json ({json.Length} ký tự):\n" + json);
    }

    [MenuItem(Menu + "Save Now", false, 3)]
    private static void SaveNow()
    {
        if (!EditorApplication.isPlaying)
            Debug.Log("[Save] Đang NGOÀI Play Mode — capture sẽ chỉ đọc được từ PlayerPrefs " +
                      "(manager Instance chưa dựng); vẫn đủ cho mirror.");
        SaveSystem.Save("editor-menu");
    }

    [MenuItem(Menu + "Load Now (phục hồi khoá PlayerPrefs thiếu)", false, 4)]
    private static void LoadNow()
    {
        FarmSaveData data = SaveSystem.Load();
        if (data == null)
        {
            Debug.LogWarning("[Save] Không có save.json (hoặc cả .bak) để phục hồi.");
            return;
        }

        bool dongY = EditorUtility.DisplayDialog(
            "Load Now — phục hồi từ save.json",
            $"Save v{data.saveVersion}, lưu lúc {data.savedAtUtc} UTC, " +
            $"{data.prefsMirror.Count} khoá mirror.\n\n" +
            "Chỉ ghi lại các khoá PlayerPrefs BỊ THIẾU — khoá đang tồn tại luôn được giữ nguyên.\n" +
            "Manager đọc PlayerPrefs ở Awake nên cần vào lại Play Mode để thấy đủ hiệu lực.",
            "Phục hồi", "Thôi");
        if (!dongY) return;

        int restored = SaveSystem.RestoreMissingPrefs(data);

        // Đang Play thì thử áp luôn snapshot tàu (hệ duy nhất restore trực tiếp được).
        if (EditorApplication.isPlaying && data.train != null && data.train.restorable)
        {
            bool ok = SaveAdapters.TrainAdapter.TryRestore(data.train);
            Debug.Log(ok ? "[Save] Đã áp snapshot tàu vào phiên đang chạy."
                         : "[Save] Chưa áp được snapshot tàu (tàu chưa init xong hoặc chưa duyệt patch).");
        }

        Debug.Log($"[Save] Load Now xong — phục hồi {restored} khoá.");
    }

    [MenuItem(Menu + "Delete Save (save.json)", false, 20)]
    private static void DeleteSave()
    {
        bool dongY = EditorUtility.DisplayDialog(
            "Delete Save",
            "Xoá save.json + save.json.bak + save.json.tmp?\n\n" +
            "PlayerPrefs KHÔNG bị đụng — muốn chơi lại từ đầu hẳn thì chạy thêm " +
            "tool reset PlayerPrefs (FarmResetTool) như trước giờ.",
            "Xoá file save", "Thôi");
        if (!dongY) return;

        SaveSystem.DeleteSave();
    }
}
#endif
