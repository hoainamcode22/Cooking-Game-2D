using System;
using System.IO;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  M0-2 — SaveSystem: ghi/đọc save.json (atomic, có version, có bak).
//
//  KHÔNG đụng file code đang có. Nguồn sự thật runtime của các hệ đã tự lưu
//  vẫn là PlayerPrefs (xem SAVE_DESIGN.md); file này chỉ:
//    • gom bản chụp hợp nhất + mirror thô PlayerPrefs vào save.json,
//    • giữ phần lưu THẬT của hệ Tàu,
//    • phục hồi khoá PlayerPrefs bị thiếu (chỉ khi được gọi tường minh).
// ═══════════════════════════════════════════════════════════════════════════
public static class SaveSystem
{
    /// <summary>Tăng khi đổi cấu trúc FarmSaveData, rồi viết nhánh trong MigrateFrom.</summary>
    public const int CurrentSaveVersion = 1;

    public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static string TmpPath  => SavePath + ".tmp";
    public static string BakPath  => SavePath + ".bak";

    /// <summary>
    /// Save trên đĩa MỚI HƠN code (người dùng hạ cấp bản game) → đọc phần hiểu được
    /// nhưng CẤM ghi đè file — cùng triết lý WarehouseManager._khongDuocGhi.
    /// </summary>
    private static bool _khongDuocGhi;

#if UNITY_EDITOR
    // Enter Play Mode Options (không reload domain): static sống qua các lần Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _khongDuocGhi = false; }
#endif

    // ═════════════════════════════════════════════════════════════════════
    //  GHI
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Chụp toàn bộ state (qua SaveAdapters) và ghi save.json atomic.
    /// An toàn gọi từ bất kỳ đâu, bất kỳ lúc nào trong Play Mode; ngoài Play Mode
    /// vẫn chạy nhưng chỉ chụp được phần đọc từ PlayerPrefs.
    /// </summary>
    /// <param name="reason">Chuỗi ghi vào log để truy vết ("auto", "quit", "editor-exit-play"…).</param>
    /// <returns>true nếu đã ghi file thành công.</returns>
    public static bool Save(string reason = "manual")
    {
        if (_khongDuocGhi)
        {
            Debug.LogWarning("[Save] Bỏ qua ghi: save.json trên đĩa thuộc bản game MỚI HƠN " +
                             "(xem log lúc Load). Không ghi đè để không phá save của người chơi.");
            return false;
        }

        FarmSaveData data;
        try
        {
            data = SaveAdapters.CaptureAll();
        }
        catch (Exception e)
        {
            // CaptureAll đã try/catch từng mục; vào đây là lỗi khung sườn — vẫn không được ném tiếp
            // vì Save() chạy trong OnApplicationQuit/Update.
            Debug.LogError($"[Save] CaptureAll lỗi không lường trước: {e}");
            return false;
        }

        data.saveVersion = CurrentSaveVersion;
        data.savedAtUtc  = DateTime.UtcNow.ToString("o");

        string json;
        try { json = JsonUtility.ToJson(data, true); }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Serialize lỗi: {e.Message}");
            return false;
        }

        if (!WriteAtomic(json))
            return false;

        Debug.Log($"[Save] Đã ghi save.json (v{CurrentSaveVersion}, lý do: {reason}, " +
                  $"{data.prefsMirror.Count} khoá mirror, tàu: " +
                  $"{(data.train.restorable ? "có snapshot" : data.train.hasData ? "chỉ đọc (chưa patch)" : "không")}).");
        return true;
    }

    /// <summary>Ghi tmp → File.Replace giữ .bak. Không ném exception ra ngoài.</summary>
    private static bool WriteAtomic(string json)
    {
        try
        {
            File.WriteAllText(TmpPath, json);

            if (File.Exists(SavePath))
            {
                try
                {
                    File.Replace(TmpPath, SavePath, BakPath);
                }
                catch (Exception e)
                {
                    // File.Replace có thể hỏng trên vài cấu hình (khác volume, bak bị khoá…)
                    // → đường lùi thủ công, vẫn giữ được bak.
                    Debug.LogWarning($"[Save] File.Replace lỗi ({e.Message}) — dùng đường lùi Copy.");
                    File.Copy(SavePath, BakPath, true);
                    File.Copy(TmpPath, SavePath, true);
                    File.Delete(TmpPath);
                }
            }
            else
            {
                File.Move(TmpPath, SavePath);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Ghi file thất bại: {e.Message} (path: {SavePath})");
            try { if (File.Exists(TmpPath)) File.Delete(TmpPath); } catch { /* dọn rác best-effort */ }
            return false;
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  ĐỌC
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Có save.json (hoặc bak) trên đĩa không.</summary>
    public static bool HasSaveFile() => File.Exists(SavePath) || File.Exists(BakPath);

    /// <summary>
    /// Đọc save.json; hỏng thì tự thử save.json.bak; kiểm version + gọi MigrateFrom.
    /// Trả về null nếu không có file hoặc cả hai bản đều hỏng (log rõ, không ném).
    /// </summary>
    public static FarmSaveData Load()
    {
        FarmSaveData data = TryReadFile(SavePath, "save.json");

        if (data == null && File.Exists(BakPath))
        {
            Debug.LogWarning("[Save] save.json thiếu/hỏng — thử bản dự phòng save.json.bak.");
            data = TryReadFile(BakPath, "save.json.bak");
        }

        if (data == null)
            return null;

        if (data.saveVersion > CurrentSaveVersion)
        {
            _khongDuocGhi = true;
            Debug.LogWarning($"[Save] save.json v{data.saveVersion} MỚI HƠN code v{CurrentSaveVersion} " +
                             "(hạ cấp bản game?). Đọc phần hiểu được, CẤM ghi đè file trong phiên này.");
        }
        else if (data.saveVersion < CurrentSaveVersion)
        {
            Debug.Log($"[Save] Chuyển save.json v{data.saveVersion} → v{CurrentSaveVersion}.");
            MigrateFrom(data.saveVersion, data);
            data.saveVersion = CurrentSaveVersion;
        }

        return data;
    }

    private static FarmSaveData TryReadFile(string path, string label)
    {
        try
        {
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json)) return null;

            FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(json);
            if (data == null) return null;

            // JsonUtility để null các List khi field vắng mặt trong JSON đời cũ — chuẩn hoá
            // ngay tại cổng vào để mọi nơi dùng không phải null-check từng list.
            if (data.warehouseSeeds == null) data.warehouseSeeds = new System.Collections.Generic.List<SaveItemStack>();
            if (data.inventoryItems == null) data.inventoryItems = new System.Collections.Generic.List<SaveItemStack>();
            if (data.plots == null)          data.plots          = new System.Collections.Generic.List<SavePlotSnapshot>();
            if (data.pens == null)           data.pens           = new System.Collections.Generic.List<SavePenSnapshot>();
            if (data.tutorialFlags == null)  data.tutorialFlags  = new System.Collections.Generic.List<SaveKV>();
            if (data.prefsMirror == null)    data.prefsMirror    = new System.Collections.Generic.List<SavePrefEntry>();
            if (data.train == null)          data.train          = new SaveTrainSection();
            if (data.train.slots == null)    data.train.slots    = new System.Collections.Generic.List<SaveTrainSlot>();

            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Không đọc được {label}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Nhánh chuyển đổi save.json đời cũ. Gọi khi data.saveVersion &lt; CurrentSaveVersion.
    /// Viết theo chuỗi tăng dần để save nhảy nhiều phiên bản vẫn đi đủ từng bước:
    ///
    ///     if (oldVersion &lt; 2) { /* v1 → v2: … */ }
    ///     if (oldVersion &lt; 3) { /* v2 → v3: … */ }
    ///
    /// Hiện CurrentSaveVersion = 1 nên chưa có nhánh nào.
    /// </summary>
    private static void MigrateFrom(int oldVersion, FarmSaveData data)
    {
        // (chưa có gì để chuyển — v1 là phiên bản đầu tiên)
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PHỤC HỒI & XOÁ
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ghi lại các khoá PlayerPrefs có trong prefsMirror mà máy này CHƯA có.
    /// KHÔNG BAO GIỜ đè khoá đang tồn tại — PlayerPrefs luôn thắng khi có mặt.
    /// Mặc định chỉ được gọi từ menu Editor (Tools/Farm Game/Save/Load Now) hoặc khi
    /// SaveBootstrap.AutoRestoreMissingPrefs được bật có chủ đích. Lý do: tool
    /// "CHƠI LẠI TỪ ĐẦU" xoá PlayerPrefs — tự bơm lại là phá reset (SAVE_DESIGN.md §3.3).
    /// </summary>
    /// <returns>Số khoá đã ghi lại.</returns>
    public static int RestoreMissingPrefs(FarmSaveData data)
    {
        if (data == null || data.prefsMirror == null) return 0;

        int restored = 0;
        foreach (SavePrefEntry e in data.prefsMirror)
        {
            try
            {
                if (e == null || string.IsNullOrEmpty(e.key)) continue;
                if (PlayerPrefs.HasKey(e.key)) continue;   // không đè — PlayerPrefs thắng

                switch (e.type)
                {
                    case "int":    PlayerPrefs.SetInt(e.key, e.i);      restored++; break;
                    case "string": PlayerPrefs.SetString(e.key, e.s ?? ""); restored++; break;
                    case "float":  PlayerPrefs.SetFloat(e.key, e.f);    restored++; break;
                    default:
                        Debug.LogWarning($"[Save] Mirror có type lạ '{e.type}' cho khoá '{e.key}' — bỏ qua.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Save] Phục hồi khoá '{e?.key}' lỗi: {ex.Message} — bỏ qua khoá này.");
            }
        }

        if (restored > 0)
        {
            PlayerPrefs.Save();
            Debug.Log($"[Save] Đã phục hồi {restored} khoá PlayerPrefs bị thiếu từ save.json " +
                      "(các manager đọc PlayerPrefs ở Awake — cần vào lại Play Mode để thấy đủ).");
        }
        else
        {
            Debug.Log("[Save] Không có khoá PlayerPrefs nào thiếu — không phục hồi gì.");
        }
        return restored;
    }

    /// <summary>
    /// Xoá save.json (+ .bak, .tmp). KHÔNG đụng PlayerPrefs — muốn chơi lại từ đầu
    /// dùng FarmResetTool như cũ, rồi gọi thêm hàm này để backup cũ không còn.
    /// </summary>
    public static void DeleteSave()
    {
        int deleted = 0;
        foreach (string p in new[] { SavePath, BakPath, TmpPath })
        {
            try { if (File.Exists(p)) { File.Delete(p); deleted++; } }
            catch (Exception e) { Debug.LogWarning($"[Save] Không xoá được {p}: {e.Message}"); }
        }
        Debug.Log($"[Save] Đã xoá {deleted} file save ({SavePath}[.bak/.tmp]). PlayerPrefs KHÔNG bị đụng.");
    }
}
