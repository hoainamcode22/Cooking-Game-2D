using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [Decor5] ScriptableObject cấu hình DUY NHẤT của hệ 5 stage.
/// Đặt tại `Assets/_Game/Resources/DecorGrowthConfig.asset` (DEV-D tạo bằng Editor tool)
/// để <see cref="DecorGrowthBootstrap"/> lazy-load qua Resources.Load.
///
/// FEATURE FLAG (CONTRACT §9): <see cref="enabled"/> mặc định **false**.
/// Chưa có asset, hoặc asset có enabled == false ⇒ mọi hook return ngay ở dòng đầu,
/// game chạy y hệt như trước khi thêm gói này. Đây là điều kiện để 1 dòng cộng thêm
/// vào PlacementManager được coi là "cộng thêm có default an toàn" (AUTONOMY §2).
///
/// KHÔNG sửa file .asset nào của giá cả — giá giữ nguyên 100%, ở đây chỉ có THỜI GIAN XÂY.
///
/// ── HAI CHẾ ĐỘ (QUYẾT ĐỊNH LEAD #1) ─────────────────────────────────────────
///   • FULL 5-STAGE : item CÓ bộ art hợp lệ (15 decor) → đổi sprite qua 5 stage + hộp quà.
///   • WORKER-ONLY  : chuồng / máy KHÔNG có art 5 stage → giữ nguyên sprite, chỉ có
///                    timer + thợ búa + popup tiến độ, hết giờ ăn mừng rồi xong ngay.
/// </summary>
[CreateAssetMenu(fileName = "DecorGrowthConfig", menuName = "Farm/Decor Growth Config")]
public class DecorGrowthConfig : ScriptableObject
{
    /// <summary>
    /// Một mốc trong bảng "giá vàng → giây xây" của chuồng.
    /// Là NESTED struct trong chính DecorGrowthConfig để file này vẫn chỉ có MỘT
    /// top-level type (CONTRACT §7: 1 class / 1 file, không ngoại lệ).
    /// </summary>
    [System.Serializable]
    public struct GoldToSeconds
    {
        public int gold;
        public int seconds;
    }

    // ── Feature flag ──────────────────────────────────────────────────────────
    [Header("FEATURE FLAG — mặc định TẮT (CONTRACT §9)")]
    [Tooltip("false = toàn bộ hệ 5 stage bị vô hiệu hoá, game chạy như cũ. Sếp bật tay khi art đã đủ.")]
    public bool enabled = false;

    [Header("Bật cho loại nào")]
    [Tooltip("Đồ trang trí (DecorData). BẮT BUỘC phải có bộ art 5 stage hợp lệ mới được bật.")]
    public bool applyToDecor = true;

    [Tooltip("Chuồng gia súc (prefab có PenClickDetector / PenDropTarget / PenMiniPanelUI).\n" +
             "Chuồng KHÔNG có art 5 stage → chạy WORKER-ONLY: giữ nguyên sprite, chỉ thêm timer + thợ + popup.")]
    public bool applyToPens = true;

    [Tooltip("Nhà village. LƯU Ý: nhà KHÔNG nhận DecorGrowthController (đã có HouseGrowthController riêng) —\n" +
             "cờ này chỉ cho phép DEV-B gắn THỢ BÚA lên nhà qua HouseWorkerBridge. Xem HouseWorkersAllowed.")]
    public bool applyToHouses = true;

    [Tooltip("Máy chế biến & công trình còn lại. Cũng chạy WORKER-ONLY vì không có art 5 stage.")]
    public bool applyToMachines = true;

    // ── Bộ art ────────────────────────────────────────────────────────────────
    [Header("Bộ art 5 stage theo itemID (chỉ 15 decor có)")]
    public List<DecorStageSet> stageSets = new List<DecorStageSet>();

    // ── Loại trừ tường minh ───────────────────────────────────────────────────
    [Header("LOẠI TRỪ TƯỜNG MINH — đừng xoá, đây là cửa an toàn")]
    [Tooltip("itemID TUYỆT ĐỐI không được gắn hệ xây.\n" +
             "100 = Đất (có PlotController — hệ xây sẽ che sprite ô đất).\n" +
             "109..112 = Chậu Hoa 1..4 (BuildingData trang trí nhỏ, không có art 5 stage, không cần thợ).")]
    public List<int> excludedItemIDs = new List<int> { 100, 109, 110, 111, 112 };

    // ── Nhịp stage & FX ───────────────────────────────────────────────────────
    [Header("Ngưỡng stage")]
    [Range(0.05f, 0.95f)]
    [Tooltip("Progress đổi từ stage 1 (vật liệu rời) sang stage 2 (xây nửa vời). CONTRACT §6 chốt 0.5.")]
    public float stage2Threshold = 0.5f;

    [Header("Hộp quà thở (stage 4)")]
    public float giftBoxBobAmplitude = 0.04f;
    public float giftBoxBobSpeed = 3.5f;

    [Header("Hộp bung (stage 5)")]
    [Tooltip("Thời gian pop scale khi hộp bung nắp, chạy bằng Time.unscaledDeltaTime. WORKER-ONLY bỏ qua bước này.")]
    public float boxOpenDuration = 0.35f;
    public float boxOpenPopScale = 0.25f;

    [Header("Ăn mừng")]
    [Tooltip("PHẢI khớp ConstructionCelebrationFX.TotalLife = 3.5f. Thợ búa ăn mừng tới HẾT khoảng này mới biến mất (CONTRACT §6).")]
    public float celebrationSeconds = 3.5f;

    [Tooltip("EXP thưởng truyền vào ConstructionCelebrationFX.Play. 0 = không cộng EXP (mặc định an toàn, tránh lạm phát level).")]
    public int celebrationExpReward = 0;

    // ── Công thức thời gian xây (CONTRACT §8) ─────────────────────────────────
    [Header("Thời gian xây DECOR — CONTRACT §8")]
    [Tooltip("Decor: buildSeconds = Clamp(Round(diamondPrice * hệ số này), min, max).")]
    public float decorGemToSeconds = 0.6f;

    public int minBuildSeconds = 20;
    public int maxBuildSeconds = 240;

    [Header("Thời gian xây CHUỒNG — BẢNG MỐC (QUYẾT ĐỊNH LEAD #2)")]
    [Tooltip("CONTRACT §8 tự mâu thuẫn giữa bảng mốc và công thức; Lead chốt BẢNG THẮNG.\n" +
             "Giá nằm giữa 2 mốc → nội suy tuyến tính. Dưới mốc đầu → giây của mốc đầu.\n" +
             "Trên mốc cuối → giây của mốc cuối. Bảng RỖNG → rơi về công thức cũ (penGoldToSeconds).")]
    public List<GoldToSeconds> penBuildTable = new List<GoldToSeconds>
    {
        new GoldToSeconds { gold = 100,  seconds = 45  },
        new GoldToSeconds { gold = 600,  seconds = 90  },
        new GoldToSeconds { gold = 950,  seconds = 120 },
        new GoldToSeconds { gold = 2000, seconds = 180 }
    };

    [Tooltip("CHỈ dùng khi penBuildTable rỗng: Clamp(Round(30 + goldPrice * hệ số này), 45, 180).")]
    public float penGoldToSeconds = 0.075f;

    // Cận dưới / cận trên riêng của nhánh chuồng — chốt cứng theo CONTRACT §8 (45..180),
    // KHÔNG lấy min/maxBuildSeconds để designer chỉnh decor không vô tình đổi luôn chuồng.
    private const int PenMinSeconds = 45;
    private const int PenMaxSeconds = 180;

    /// <summary>
    /// Tên component nhận diện CHUỒNG. Dùng CHUỖI vì DEV-A không được reference class
    /// của DEV khác / của hệ chuồng (CONTRACT §0.1) — so sánh qua Type.Name lúc chạy.
    /// </summary>
    private static readonly string[] PenComponentNames =
    {
        "PenClickDetector",
        "PenDropTarget",
        "PenMiniPanelUI"
    };

    /// <summary>Tên component nhận diện Ô ĐẤT — cùng lý do dùng chuỗi như trên.</summary>
    private static readonly string[] PlotComponentNames =
    {
        "PlotController"
    };

    // Bản sao đã sắp xếp của penBuildTable (tránh sort + alloc mỗi lần tra).
    private List<GoldToSeconds> _sortedPenTable;
    private int _sortedPenTableCount = -1;

    /// <summary>Tra bộ art theo itemID. Không có → null (item chạy WORKER-ONLY hoặc bị bỏ qua).</summary>
    // ───────────────────────────────────────────────────────────────────────
    // [FIX CS1503 — Lead 2026-09-01] BaseItemData.itemID là **string**, không phải int
    // (CONTRACT §3 ghi sai). Mọi API nội bộ của hệ Decor5 dùng int (save key, bảng
    // stageSet, danh sách loại trừ). 2 hàm dưới là CẦU DUY NHẤT giữa 2 kiểu —
    // đừng gọi int.TryParse rải rác nữa.
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>Đổi itemID dạng chuỗi sang int. Chuỗi không phải số ⇒ băm FNV-1a
    /// **tất định** (KHÔNG dùng string.GetHashCode: .NET Core randomize mỗi lần chạy
    /// ⇒ save key sẽ đổi sau mỗi lần mở game). Dải băm ≥ 1.000.000 nên không thể
    /// đụng itemID thật (hiện 1..122).</summary>
    public static int ParseItemId(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID)) return 0;
        string t = itemID.Trim();
        int v;
        if (int.TryParse(t, out v)) return v;

        uint h = 2166136261u;
        for (int i = 0; i < t.Length; i++) { h ^= t[i]; h *= 16777619u; }
        return 1000000 + (int)(h % 1000000u);
    }

    /// <summary>Lấy itemID dạng int từ data. data null ⇒ 0.</summary>
    public static int ItemIdOf(PlaceableItemData data)
        => data == null ? 0 : ParseItemId(data.itemID);

    public DecorStageSet FindSet(int itemID)
    {
        if (stageSets == null) return null;
        for (int i = 0; i < stageSets.Count; i++)
        {
            DecorStageSet s = stageSets[i];
            if (s != null && s.itemID == itemID) return s;
        }
        return null;
    }

    /// <summary>Item nằm trong danh sách loại trừ tường minh.</summary>
    public bool IsExcludedItem(int itemID)
    {
        if (excludedItemIDs == null) return false;
        for (int i = 0; i < excludedItemIDs.Count; i++)
        {
            if (excludedItemIDs[i] == itemID) return true;
        }
        return false;
    }

    /// <summary>
    /// Thời gian xây (giây) cho một item — CONTRACT §8, thứ tự ưu tiên KHÔNG được đổi:
    ///   1. stageSet.buildSecondsOverride > 0   → dùng nó (designer chốt tay)
    ///   2. data.buildTimeSeconds     > 0       → GIỮ NGUYÊN (nhà 60/180/360/600/900, máy 240)
    ///   3. data is DecorData                   → Clamp(Round(diamondPrice * decorGemToSeconds), min, max)
    ///                                            diamondPrice &lt;= 0 → lấy goldPrice * 0.05f
    ///   4. còn lại (chuồng / công trình gold)  → BẢNG MỐC penBuildTable, nội suy tuyến tính
    ///   5. không xác định (data null)          → 0 = KHÔNG bật hệ xây cho món này
    /// Trả 0 nghĩa là "bỏ qua", KHÔNG phải "xây tức thời" — đây là CỬA AN TOÀN, đừng bỏ.
    /// </summary>
    public float ResolveBuildSeconds(PlaceableItemData data)
    {
        if (data == null) return 0f;

        DecorStageSet set = FindSet(ItemIdOf(data));
        if (set != null && set.buildSecondsOverride > 0f) return set.buildSecondsOverride;

        // Nhà & máy: asset đã có thời gian xây do Sếp chốt — TUYỆT ĐỐI không đổi.
        if (data.buildTimeSeconds > 0f) return data.buildTimeSeconds;

        int lo = Mathf.Min(minBuildSeconds, maxBuildSeconds);
        int hi = Mathf.Max(minBuildSeconds, maxBuildSeconds);

        if (data is DecorData)
        {
            float raw = data.diamondPrice > 0
                ? data.diamondPrice * decorGemToSeconds
                : data.goldPrice * 0.05f;
            return Mathf.Clamp(Mathf.Round(raw), lo, hi);
        }

        return ResolvePenSeconds(data.goldPrice);
    }

    /// <summary>
    /// Giây xây của chuồng theo BẢNG MỐC (QUYẾT ĐỊNH LEAD #2), nội suy tuyến tính giữa
    /// 2 mốc gần nhất. Bảng rỗng ⇒ rơi về công thức cũ. Kết quả luôn Clamp [45, 180].
    /// </summary>
    public float ResolvePenSeconds(int goldPrice)
    {
        List<GoldToSeconds> tbl = GetSortedPenTable();

        if (tbl == null || tbl.Count == 0)
            return Mathf.Clamp(Mathf.Round(30f + goldPrice * penGoldToSeconds), PenMinSeconds, PenMaxSeconds);

        if (goldPrice <= tbl[0].gold)
            return Mathf.Clamp(tbl[0].seconds, PenMinSeconds, PenMaxSeconds);

        int last = tbl.Count - 1;
        if (goldPrice >= tbl[last].gold)
            return Mathf.Clamp(tbl[last].seconds, PenMinSeconds, PenMaxSeconds);

        for (int i = 0; i < last; i++)
        {
            GoldToSeconds a = tbl[i];
            GoldToSeconds b = tbl[i + 1];
            if (goldPrice < a.gold || goldPrice > b.gold) continue;

            int span = b.gold - a.gold;
            float t = span <= 0 ? 0f : (float)(goldPrice - a.gold) / span;
            return Mathf.Clamp(Mathf.Round(Mathf.Lerp(a.seconds, b.seconds, t)), PenMinSeconds, PenMaxSeconds);
        }

        return Mathf.Clamp(Mathf.Round(30f + goldPrice * penGoldToSeconds), PenMinSeconds, PenMaxSeconds);
    }

    private List<GoldToSeconds> GetSortedPenTable()
    {
        if (penBuildTable == null) return null;
        if (_sortedPenTable != null && _sortedPenTableCount == penBuildTable.Count) return _sortedPenTable;

        _sortedPenTable = new List<GoldToSeconds>(penBuildTable);
        _sortedPenTable.Sort((x, y) => x.gold.CompareTo(y.gold));   // designer nhập lộn xộn vẫn đúng
        _sortedPenTableCount = penBuildTable.Count;
        return _sortedPenTable;
    }

    /// <summary>
    /// Có gắn <see cref="DecorGrowthController"/> lên object vừa đặt hay không.
    ///
    /// ⚠ NHÀ: prefab nhà đã có <c>HouseGrowthController</c> — nếu gắn thêm controller mới thì
    ///   HAI hệ cùng ghi SpriteRenderer + collider mỗi frame và cùng đọc PlayerPrefs khác key
    ///   ⇒ nhà nhấp nháy / kẹt trạng thái. Vì vậy hàm này LUÔN trả false cho nhà, bất kể
    ///   <see cref="applyToHouses"/>. Nhà vẫn được gắn THỢ BÚA — việc đó do DEV-B làm qua
    ///   HouseWorkerBridge và chỉ kiểm <see cref="HouseWorkersAllowed"/>.
    ///
    /// ⚠ Ô ĐẤT (PlotController) và các itemID trong <see cref="excludedItemIDs"/> luôn bị loại.
    ///
    /// DECOR cần bộ art hợp lệ mới bật (không có art thì đổi sprite ra null = vật biến mất).
    /// CHUỒNG / MÁY không cần art — chúng chạy WORKER-ONLY (QUYẾT ĐỊNH LEAD #1).
    /// </summary>
    public bool ShouldApply(PlaceableItemData data, GameObject spawned)
    {
        if (!enabled) return false;
        if (data == null || spawned == null) return false;

        if (IsExcludedItem(ItemIdOf(data))) return false;
        if (IsHouseObject(spawned)) return false;
        if (IsPlotObject(spawned)) return false;

        if (IsPenObject(spawned)) return applyToPens;

        if (data is DecorData)
        {
            if (!applyToDecor) return false;
            DecorStageSet s = FindSet(ItemIdOf(data));
            return s != null && s.IsValid;      // decor BẮT BUỘC có art 5 stage
        }

        return applyToMachines;
    }

    /// <summary>
    /// Item này sẽ chạy WORKER-ONLY (không đổi sprite) hay FULL 5-STAGE.
    /// DEV-B / DEV-D đọc để biết có cần art hay không.
    /// </summary>
    public bool IsWorkerOnlyFor(int itemID)
    {
        DecorStageSet s = FindSet(itemID);
        return s == null || !s.IsValid;
    }

    /// <summary>DEV-B đọc cờ này để biết có được gắn thợ búa lên nhà village không.</summary>
    public bool HouseWorkersAllowed => enabled && applyToHouses;

    /// <summary>Object đã có hệ xây nhà cũ (HouseGrowthController) ở bất kỳ cấp nào.</summary>
    public bool IsHouseObject(GameObject go)
    {
        if (go == null) return false;
        return go.GetComponentInChildren<HouseGrowthController>(true) != null;
    }

    /// <summary>
    /// Nhận diện CHUỒNG bằng TÊN component (chuỗi), không reference class.
    /// GetComponentsInChildren&lt;Component&gt; có thể trả PHẦN TỬ NULL khi prefab còn
    /// MonoBehaviour bị missing script ⇒ bắt buộc null-check từng phần tử.
    /// </summary>
    public bool IsPenObject(GameObject go) => HasComponentNamed(go, PenComponentNames);

    /// <summary>Nhận diện Ô ĐẤT (PlotController) — hệ xây sẽ che sprite ô đất nên phải loại.</summary>
    public bool IsPlotObject(GameObject go) => HasComponentNamed(go, PlotComponentNames);

    private static bool HasComponentNamed(GameObject go, string[] names)
    {
        if (go == null || names == null) return false;

        Component[] comps = go.GetComponentsInChildren<Component>(true);
        if (comps == null) return false;

        for (int i = 0; i < comps.Length; i++)
        {
            Component c = comps[i];
            if (c == null) continue;                 // missing script → element null
            string n = c.GetType().Name;
            for (int j = 0; j < names.Length; j++)
            {
                if (n == names[j]) return true;
            }
        }
        return false;
    }
}
