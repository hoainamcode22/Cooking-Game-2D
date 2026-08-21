using System;
using System.Collections.Generic;

// ═══════════════════════════════════════════════════════════════════════════
//  M0-2 — DTO của save.json (JsonUtility-compatible: chỉ field public + List,
//  không Dictionary, không property, không Sprite).
//
//  Class gốc đặt tên `FarmSaveData` (file vẫn là SaveData.cs theo spec) vì
//  `WarehouseManager` đã có nested class private tên `SaveData` — về C# thì
//  không đụng nhau, nhưng trùng tên là mời gọi đọc nhầm.
//
//  LƯU Ý PHÂN VAI (xem SAVE_DESIGN.md §2):
//    • `prefsMirror` là LỚP PHỤC HỒI thật sự — bản sao thô các khoá PlayerPrefs
//      mà các manager tự ghi. Phục hồi = ghi lại nguyên văn → trung thực 100%.
//    • Các field còn lại (gold/level/warehouseSeeds/plots/pens/tutorialFlags…)
//      là BẢN CHỤP ĐỌC ĐƯỢC để debug/soi save — nguồn sự thật vẫn là PlayerPrefs.
//    • `train` là phần lưu THẬT duy nhất không tồn tại ở PlayerPrefs.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Một khoá PlayerPrefs được sao chép nguyên văn. type = "int" | "string" | "float".</summary>
[Serializable]
public class SavePrefEntry
{
    public string key;
    public string type;
    public int    i;
    public string s;
    public float  f;
}

/// <summary>Một dòng vật phẩm (kho hạt giống hoặc kho nông sản).</summary>
[Serializable]
public class SaveItemStack
{
    public string itemId;
    public string displayName;   // kho nông sản không có tên hiển thị → để rỗng
    public int    amount;
}

/// <summary>
/// Bản chụp MỘT ô đất qua public API của <c>PlotController</c>.
/// Thời gian trồng (start/finish unix) KHÔNG public — dữ liệu đầy đủ của ô nằm ở
/// prefsMirror (khoá PLOT_NORMAL_{id} / PLOT_RARE_{id}); ô tự Load lại ở Start().
/// </summary>
[Serializable]
public class SavePlotSnapshot
{
    public int    plotId;
    public bool   isRare;
    public bool   isFlower;
    public bool   isUnlocked;
    public bool   isPlanted;
    public bool   isReady;
    public string cropId;
}

/// <summary>Bản chụp một chuồng (đọc từ khoá PenState_/PenFood_/PenStartTime_{penId}).</summary>
[Serializable]
public class SavePenSnapshot
{
    public string penId;
    public int    state;       // PenMiniPanelUI.PenState: 0 Idle · 1 Processing · 2 Ready
    public string foodId;
    public string startUnix;   // chuỗi vì code gốc lưu bằng SetString(double.ToString("R"))
}

/// <summary>Một toa tàu — bản đọc được của TrainWagonSlotData (không icon).</summary>
[Serializable]
public class SaveTrainSlot
{
    public string itemId;
    public int    mode;          // TrainWagonSlotMode
    public int    currentAmount;
    public int    requiredAmount;
    public int    rewardAmount;
    public bool   isCollected;
}

/// <summary>
/// Phần TÀU — hệ duy nhất chưa có save ở PlayerPrefs.
/// `snapshotJson` là JSON của <c>TrainManager.TrainTripSnapshot</c> (chỉ tồn tại SAU khi
/// TrainManager.PATCH.md được duyệt) — giữ dạng chuỗi để code này biên dịch được cả
/// TRƯỚC và SAU patch. `restorable` = đã chụp được qua API patch (có tripIndex) hay chưa.
/// </summary>
[Serializable]
public class SaveTrainSection
{
    public bool hasData;
    public bool restorable;
    public int  state;                       // TrainState lúc chụp
    public string snapshotJson;              // TrainTripSnapshot (sau patch), "" nếu chưa patch
    public List<SaveTrainSlot> slots = new List<SaveTrainSlot>();
}

/// <summary>Cặp khoá-số cho các cờ int (tutorial…).</summary>
[Serializable]
public class SaveKV
{
    public string key;
    public int    value;
}

/// <summary>Gốc của save.json. Xem bảng schema trong SAVE_DESIGN.md.</summary>
[Serializable]
public class FarmSaveData
{
    /// <summary>Tăng khi đổi cấu trúc file này, rồi viết nhánh trong SaveSystem.MigrateFrom.</summary>
    public int    saveVersion;

    /// <summary>DateTime.UtcNow.ToString("o") tại thời điểm ghi.</summary>
    public string savedAtUtc = "";

    // ── Bản chụp đọc-được (nguồn thật: PlayerPrefs — phục hồi qua prefsMirror) ──
    public int gold;
    public int gems;
    public int level;
    public int exp;

    public List<SaveItemStack>    warehouseSeeds = new List<SaveItemStack>();
    public List<SaveItemStack>    inventoryItems = new List<SaveItemStack>();
    public List<SavePlotSnapshot> plots          = new List<SavePlotSnapshot>();
    public List<SavePenSnapshot>  pens           = new List<SavePenSnapshot>();
    public List<SaveKV>           tutorialFlags  = new List<SaveKV>();

    /// <summary>
    /// THAM CHIẾU (không lưu lại lần 2): tiến độ nhiệm vụ do MissionProgressTracker tự lưu
    /// ở khoá MISSION_PROGRESS_V1; cờ đã-nhận ở MISSION_CLAIMED_* / ACHIEVEMENT_CLAIMED_*.
    /// Tất cả đã nằm trong prefsMirror.
    /// </summary>
    public string missionNote = "";

    // ── Phần lưu THẬT ──
    public SaveTrainSection train = new SaveTrainSection();

    /// <summary>Bản sao thô toàn bộ khoá PlayerPrefs của game (lớp phục hồi).</summary>
    public List<SavePrefEntry> prefsMirror = new List<SavePrefEntry>();
}
