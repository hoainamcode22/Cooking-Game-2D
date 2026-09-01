#if UNITY_EDITOR
// ============================================================================
//  LevelRewardV2FillTool.cs  —  Đổ bảng quà lên cấp V3 (L2→L30) vào asset
//  Đích: Assets/_Game/Farm/Editor/LevelRewardV2FillTool.cs
//
//  Nguồn thiết kế: production/BANG_QUA_LEVELUP_V2_2026-09-01.md (bản V3 — lệnh Sếp 01/09:
//                  MỌI level tối thiểu 6 entry giftItems, không tính vàng/gem)
//  Nguồn ID      : catalog Lead dump từ MarketPriceTable.cs (chính chủ, có displayName +
//                  unlockLevel) — TOÀN BỘ id trong BANG dưới đây thuộc catalog đó và có
//                  unlockLevel ≤ level nhận. "trung"/"sua" KHÔNG dùng (id chuẩn: egg/milk).
//                  Đồ trang trí (hệ đặt-ngay, không có kho) KHÔNG đưa vào giftItems.
//
//  Schema đích  : LevelRewardConfig (Assets/_Game/Farm/Scripts/UI/LevelRewardConfig.cs)
//                 - giftGold : int
//                 - giftGems : int
//                 - giftItems: List<LevelRewardConfig.ItemGift> { itemId, displayName, icon, amount }
//                 (List không giới hạn phần tử → 6-7 entry/level không cần vá schema)
//
//  CÂN BẰNG (đã kiểm máy trước khi nhúng):
//  - Vàng + Gem giữ nguyên 100% bảng cũ 31/08 đã duyệt (chênh 0%).
//  - 29/29 level có 6-7 entry; giá trị vật phẩm ước theo mô hình giá market ≤ 35% giftGold
//    từng level (max 34,4% ở L4; tổng toàn bộ ≈14,1% tổng vàng) → không lạm phát.
//
//  AN TOÀN:
//  - Tool KHÔNG tạo asset mới (thiếu → log tên), KHÔNG đụng unlockEntries /
//    unlockDescriptions / hintText / levelReached, KHÔNG xoá entry lạ đang có trong asset
//    (entry ngoài bảng được giữ nguyên ở cuối list + báo cáo).
//  - Entry cũ trùng itemId/tên (so sánh bỏ dấu) được TÁI DÙNG để giữ icon thật; entry ghi
//    mới chưa có icon → icon = null + đánh dấu trong report (gán bằng tool icon / tay).
//  - Idempotent: APPLY lần 2 → "0 asset đổi".
//
//  Menu:
//    Tools/Farm Game/Level Rewards/Đổ quà V2 (DRY-RUN)  — chỉ in report, không ghi
//    Tools/Farm Game/Level Rewards/Đổ quà V2 (APPLY)    — ghi (Undo + SetDirty + SaveAssets)
// ============================================================================

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class LevelRewardV2FillTool
{
    const string DUONG_DAN_ASSET = "Assets/_Game/Farm/data/Lever Game/LevelReward_L{0}.asset";
    const int LEVEL_DAU = 2, LEVEL_CUOI = 30;

    // ─── Kiểu dữ liệu bảng nhúng ─────────────────────────────────────────────

    struct MucQua
    {
        public string id;
        public string ten;       // displayName tiếng Việt đúng theo catalog MarketPriceTable
        public int soLuong;
    }

    struct QuaLevel
    {
        public int level;
        public int vang;
        public int gem;
        public MucQua[] items;
    }

    static MucQua OK(string id, string ten, int n) =>
        new MucQua { id = id, ten = ten, soLuong = n };

    // ─── BẢNG DATA NHÚNG V3 — sinh + kiểm máy từ BANG_QUA_LEVELUP_V2_2026-09-01.md ─
    // Công thức mỗi level: 2-3 hạt (ưu tiên hạt VỪA MỞ + 1 hạt chủ lực cũ) + 1-2 nông sản/hoa
    // + 1 chăn nuôi hoặc gia vị + 1 vật liệu (từ L6, xoay vòng đá/gỗ/đinh/sơn/kính).
    // Level tròn 5/10/15/20/25/30 = "đậm" (gem cao kế thừa bảng cũ, L10/15/20/25/30 có 7 entry).

    static readonly QuaLevel[] BANG =
    {
        new QuaLevel{ level=2, vang=150, gem=2, items=new[]{ OK("seed_ngo","Hạt Ngô",3), OK("seed_rice","Hạt Lúa",2), OK("seed_bapcai","Hạt Bắp Cải",2), OK("ngo","Ngô",3), OK("egg","Trứng",1), OK("salt","Muối",2) }},
        new QuaLevel{ level=3, vang=200, gem=2, items=new[]{ OK("ca_rot","Hạt Cà Rốt",2), OK("seed_cachua","Hạt Cà Chua",2), OK("seed_rice","Hạt Lúa",2), OK("cachua","Cà Chua",3), OK("herbs","Rau Thơm",2), OK("egg","Trứng",1) }},
        new QuaLevel{ level=4, vang=250, gem=3, items=new[]{ OK("seed_hoa_hong","Hạt Hoa Hồng",2), OK("seed_hoa_oai_huong","Hạt Hoa Oải Hương",2), OK("hoa_hong","Hoa Hồng",3), OK("pork","Thịt Heo",1), OK("soysauce","Nước Tương",2), OK("salt","Muối",2) }},
        new QuaLevel{ level=5, vang=300, gem=3, items=new[]{ OK("khoai_tay","Hạt Khoai Tây",3), OK("seed_cachua","Hạt Cà Chua",2), OK("khoaitay","Khoai Tây",3), OK("cachua","Cà Chua",3), OK("egg","Trứng",2), OK("fishsauce","Nước Mắm",2) }},
        new QuaLevel{ level=6, vang=350, gem=3, items=new[]{ OK("seed_nam","Hạt Nấm",3), OK("khoai_tay","Hạt Khoai Tây",2), OK("mushroom","Nấm",3), OK("ngo","Ngô",3), OK("chicken_meat","Thịt Gà",1), OK("da","Đá",1) }},
        new QuaLevel{ level=7, vang=400, gem=4, items=new[]{ OK("seed_sugarcane","Hạt Mía",3), OK("seed_hoa_lan","Hạt Hoa Lan",2), OK("sugarcane","Mía",3), OK("mushroom","Nấm",3), OK("herbs","Rau Thơm",2), OK("dinh","Đinh",1) }},
        new QuaLevel{ level=8, vang=450, gem=4, items=new[]{ OK("seed_lemon","Hạt Chanh",3), OK("seed_sugarcane","Hạt Mía",2), OK("lemon","Chanh",3), OK("mushroom","Nấm",3), OK("beef","Thịt Bò",1), OK("son","Sơn",1) }},
        new QuaLevel{ level=9, vang=500, gem=5, items=new[]{ OK("seed_chili","Hạt Ớt",2), OK("seed_tulip","Hạt Tulip",2), OK("tulip","Tulip",3), OK("chili","Ớt",3), OK("egg","Trứng",2), OK("da","Đá",1) }},
        new QuaLevel{ level=10, vang=600, gem=8, items=new[]{ OK("seed_pepper","Hạt Tiêu",3), OK("seed_hoa_mau_don","Hạt Hoa Mẫu Đơn",2), OK("seed_hoa_cam_tu_cau","Hạt Hoa Cẩm Tú Cầu",2), OK("pepper","Tiêu",3), OK("hoa_hong","Hoa Hồng",3), OK("beef","Thịt Bò",1), OK("go","Gỗ",1) }},
        new QuaLevel{ level=11, vang=700, gem=5, items=new[]{ OK("seed_pumpkin","Hạt Bí Đỏ",3), OK("seed_pepper","Hạt Tiêu",2), OK("pumpkin","Bí Đỏ",3), OK("rice","Lúa",4), OK("chicken_meat","Thịt Gà",2), OK("dinh","Đinh",1) }},
        new QuaLevel{ level=12, vang=760, gem=5, items=new[]{ OK("seed_watermelon","Hạt Dưa Hấu",3), OK("seed_pumpkin","Hạt Bí Đỏ",2), OK("watermelon","Dưa Hấu",3), OK("mushroom","Nấm",4), OK("herbs","Rau Thơm",3), OK("son","Sơn",1) }},
        new QuaLevel{ level=13, vang=820, gem=5, items=new[]{ OK("seed_sugarcane","Hạt Mía",3), OK("seed_lemon","Hạt Chanh",2), OK("sugarcane","Mía",4), OK("lemon","Chanh",3), OK("milk","Sữa",1), OK("kinh","Kính",1) }},
        new QuaLevel{ level=14, vang=880, gem=5, items=new[]{ OK("seed_hoa_mau_don","Hạt Hoa Mẫu Đơn",2), OK("seed_hoa_anh_thao","Hạt Hoa Anh Thảo",2), OK("hoa_hong","Hoa Hồng",4), OK("cachua","Cà Chua",4), OK("milk","Sữa",1), OK("da","Đá",1) }},
        new QuaLevel{ level=15, vang=1000, gem=10, items=new[]{ OK("seed_nam","Hạt Nấm",3), OK("seed_pumpkin","Hạt Bí Đỏ",2), OK("mushroom","Nấm",5), OK("pumpkin","Bí Đỏ",3), OK("milk","Sữa",2), OK("fishsauce","Nước Mắm",2), OK("go","Gỗ",2) }},
        new QuaLevel{ level=16, vang=1100, gem=6, items=new[]{ OK("seed_cachua","Hạt Cà Chua",3), OK("ca_rot","Hạt Cà Rốt",3), OK("carot","Cà Rốt",4), OK("egg","Trứng",2), OK("salt","Muối",3), OK("dinh","Đinh",1) }},
        new QuaLevel{ level=17, vang=1200, gem=6, items=new[]{ OK("seed_chili","Hạt Ớt",3), OK("seed_lemon","Hạt Chanh",2), OK("chili","Ớt",4), OK("lemon","Chanh",4), OK("milk","Sữa",1), OK("son","Sơn",1) }},
        new QuaLevel{ level=18, vang=1300, gem=6, items=new[]{ OK("seed_pepper","Hạt Tiêu",3), OK("khoai_tay","Hạt Khoai Tây",3), OK("seed_watermelon","Hạt Dưa Hấu",2), OK("khoaitay","Khoai Tây",4), OK("beef","Thịt Bò",1), OK("kinh","Kính",2) }},
        new QuaLevel{ level=19, vang=1400, gem=6, items=new[]{ OK("seed_sugarcane","Hạt Mía",3), OK("seed_hoa_lan","Hạt Hoa Lan",2), OK("mushroom","Nấm",5), OK("sugarcane","Mía",4), OK("milk","Sữa",2), OK("da","Đá",2) }},
        new QuaLevel{ level=20, vang=1600, gem=15, items=new[]{ OK("seed_lemon","Hạt Chanh",3), OK("seed_hoa_cam_tu_cau","Hạt Hoa Cẩm Tú Cầu",2), OK("ca_rot","Hạt Cà Rốt",3), OK("carot","Cà Rốt",5), OK("lemon","Chanh",4), OK("beef","Thịt Bò",2), OK("go","Gỗ",2) }},
        new QuaLevel{ level=21, vang=1700, gem=7, items=new[]{ OK("seed_pepper","Hạt Tiêu",3), OK("seed_pumpkin","Hạt Bí Đỏ",2), OK("pumpkin","Bí Đỏ",4), OK("herbs","Rau Thơm",3), OK("milk","Sữa",2), OK("dinh","Đinh",2) }},
        new QuaLevel{ level=22, vang=1800, gem=7, items=new[]{ OK("seed_chili","Hạt Ớt",3), OK("seed_watermelon","Hạt Dưa Hấu",3), OK("watermelon","Dưa Hấu",4), OK("pepper","Tiêu",3), OK("chicken_meat","Thịt Gà",2), OK("son","Sơn",2) }},
        new QuaLevel{ level=23, vang=1950, gem=7, items=new[]{ OK("seed_sugarcane","Hạt Mía",3), OK("seed_ngo","Hạt Ngô",3), OK("ngo","Ngô",5), OK("sugarcane","Mía",4), OK("beef","Thịt Bò",2), OK("kinh","Kính",2) }},
        new QuaLevel{ level=24, vang=2050, gem=7, items=new[]{ OK("seed_lemon","Hạt Chanh",3), OK("seed_cachua","Hạt Cà Chua",3), OK("lemon","Chanh",4), OK("cachua","Cà Chua",5), OK("fishsauce","Nước Mắm",3), OK("da","Đá",2) }},
        new QuaLevel{ level=25, vang=2200, gem=15, items=new[]{ OK("seed_pepper","Hạt Tiêu",3), OK("khoai_tay","Hạt Khoai Tây",3), OK("pepper","Tiêu",4), OK("khoaitay","Khoai Tây",5), OK("milk","Sữa",2), OK("beef","Thịt Bò",2), OK("go","Gỗ",2) }},
        new QuaLevel{ level=26, vang=2300, gem=8, items=new[]{ OK("seed_pepper","Hạt Tiêu",3), OK("seed_pumpkin","Hạt Bí Đỏ",3), OK("pumpkin","Bí Đỏ",5), OK("rice","Lúa",5), OK("milk","Sữa",2), OK("dinh","Đinh",2) }},
        new QuaLevel{ level=27, vang=2400, gem=8, items=new[]{ OK("seed_chili","Hạt Ớt",3), OK("seed_hoa_anh_thao","Hạt Hoa Anh Thảo",2), OK("mushroom","Nấm",5), OK("chili","Ớt",4), OK("herbs","Rau Thơm",3), OK("son","Sơn",2) }},
        new QuaLevel{ level=28, vang=2480, gem=8, items=new[]{ OK("seed_sugarcane","Hạt Mía",3), OK("seed_watermelon","Hạt Dưa Hấu",3), OK("watermelon","Dưa Hấu",5), OK("sugarcane","Mía",5), OK("pork","Thịt Heo",2), OK("kinh","Kính",2) }},
        new QuaLevel{ level=29, vang=2550, gem=8, items=new[]{ OK("seed_pepper","Hạt Tiêu",3), OK("seed_cachua","Hạt Cà Chua",3), OK("cachua","Cà Chua",5), OK("pepper","Tiêu",4), OK("milk","Sữa",2), OK("da","Đá",2) }},
        new QuaLevel{ level=30, vang=2600, gem=30, items=new[]{ OK("seed_pepper","Hạt Tiêu",4), OK("khoai_tay","Hạt Khoai Tây",4), OK("watermelon","Dưa Hấu",5), OK("mushroom","Nấm",5), OK("beef","Thịt Bò",2), OK("milk","Sữa",2), OK("go","Gỗ",2) }},
    };

    // ─── Menu ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Farm Game/Level Rewards/Đổ quà V2 (DRY-RUN)")]
    public static void DryRun() => Chay(apDung: false);

    [MenuItem("Tools/Farm Game/Level Rewards/Đổ quà V2 (APPLY)")]
    public static void Apply() => Chay(apDung: true);

    // ─── Lõi ─────────────────────────────────────────────────────────────────

    static void Chay(bool apDung)
    {
        var bc = new StringBuilder();
        bc.AppendLine(apDung ? "===== ĐỔ QUÀ V3 — APPLY =====" : "===== ĐỔ QUÀ V3 — DRY-RUN (không ghi) =====");

        // Pass 1: load toàn bộ asset + gom icon theo itemId (tái dùng icon có sẵn, không tự dò path)
        var assets = new Dictionary<int, LevelRewardConfig>();
        var thieu = new List<string>();
        var iconTheoId = new Dictionary<string, Sprite>();

        for (int n = LEVEL_DAU; n <= LEVEL_CUOI; n++)
        {
            string path = string.Format(DUONG_DAN_ASSET, n);
            var cfg = AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(path);
            if (cfg == null) { thieu.Add(path); continue; }
            assets[n] = cfg;
            if (cfg.giftItems == null) continue;
            foreach (var g in cfg.giftItems)
                if (g != null && g.icon != null && !string.IsNullOrEmpty(g.itemId) && !iconTheoId.ContainsKey(g.itemId))
                    iconTheoId[g.itemId] = g.icon;
        }

        int soDoi = 0, soBoQua = 0, soThieuIcon = 0;

        foreach (var row in BANG)
        {
            if (!assets.TryGetValue(row.level, out var cfg)) continue; // đã nằm trong danh sách thiếu

            var ghiChu = new List<string>();
            var listMoi = XayListMoi(cfg, row, iconTheoId, ghiChu, ref soThieuIcon);

            bool doiVang = cfg.giftGold != row.vang;
            bool doiGem  = cfg.giftGems != row.gem;
            bool doiItem = !GiongNhau(cfg.giftItems, listMoi);

            if (!doiVang && !doiGem && !doiItem)
            {
                soBoQua++;
                bc.AppendLine($"L{row.level}: không đổi (đã khớp V3).");
                continue;
            }

            soDoi++;
            bc.AppendLine($"L{row.level}: SẼ ĐỔI" + (apDung ? " → ĐÃ GHI" : ""));
            if (doiVang) bc.AppendLine($"    • giftGold: {cfg.giftGold} → {row.vang}");
            if (doiGem)  bc.AppendLine($"    • giftGems: {cfg.giftGems} → {row.gem}");
            if (doiItem)
            {
                bc.AppendLine($"    • giftItems: {MoTa(cfg.giftItems)}");
                bc.AppendLine($"              → {MoTa(listMoi)}");
            }
            foreach (var g in ghiChu) bc.AppendLine("    • " + g);

            if (apDung)
            {
                Undo.RecordObject(cfg, "Đổ quà Level V3");
                cfg.giftGold = row.vang;
                cfg.giftGems = row.gem;
                cfg.giftItems = listMoi;
                EditorUtility.SetDirty(cfg);
            }
        }

        if (apDung && soDoi > 0) AssetDatabase.SaveAssets();

        bc.AppendLine("───────────────────────────────────────────");
        bc.AppendLine($"TỔNG: {soDoi} asset {(apDung ? "đã ghi" : "sẽ đổi")}, {soBoQua} asset không đổi (idempotent), {thieu.Count} asset THIẾU.");
        foreach (var t in thieu) bc.AppendLine($"    ⚠ THIẾU (không tạo mới): {t}");
        if (soThieuIcon > 0)
            bc.AppendLine($"⚠ {soThieuIcon} mục ghi mới chưa có icon (null) → chạy tool icon hoặc gán tay trong Inspector (chi tiết từng mục ở trên).");
        Debug.Log(bc.ToString());
    }

    /// <summary>Dựng list giftItems mục tiêu cho 1 level. Tái dùng entry cũ trùng id/tên để giữ icon thật.</summary>
    static List<LevelRewardConfig.ItemGift> XayListMoi(
        LevelRewardConfig cfg, QuaLevel row,
        Dictionary<string, Sprite> iconTheoId,
        List<string> ghiChu, ref int soThieuIcon)
    {
        var cu = cfg.giftItems ?? new List<LevelRewardConfig.ItemGift>();
        var daDung = new HashSet<LevelRewardConfig.ItemGift>();
        var moi = new List<LevelRewardConfig.ItemGift>();

        foreach (var muc in row.items)
        {
            var trung = TimTrung(cu, muc, daDung);

            var g = new LevelRewardConfig.ItemGift();
            if (trung != null)
            {
                daDung.Add(trung);
                // Entry cũ là bằng chứng sống — giữ icon thật; chuẩn hoá id/tên theo catalog V3.
                g.itemId      = muc.id;
                g.displayName = muc.ten;
                g.icon        = trung.icon;
            }
            else
            {
                g.itemId = muc.id;
                g.displayName = muc.ten;
                iconTheoId.TryGetValue(muc.id, out var sp);
                g.icon = sp;
                if (g.icon == null)
                {
                    soThieuIcon++;
                    ghiChu.Add($"icon null (gán sau): {muc.ten} [{muc.id}]");
                }
            }
            g.amount = Mathf.Max(1, muc.soLuong);
            moi.Add(g);
        }

        // Entry đang có trong asset mà KHÔNG nằm trong bảng V3 → GIỮ NGUYÊN ở cuối, không xoá.
        foreach (var c in cu)
        {
            if (c == null || daDung.Contains(c)) continue;
            moi.Add(c);
            ghiChu.Add($"GIỮ NGUYÊN (ngoài bảng V3): {c.displayName} [{c.itemId}] ×{c.amount}");
        }

        return moi;
    }

    /// <summary>Tìm entry cũ khớp mục V3: ưu tiên itemId, sau đó tên hiển thị bỏ dấu.</summary>
    static LevelRewardConfig.ItemGift TimTrung(
        List<LevelRewardConfig.ItemGift> cu, MucQua muc, HashSet<LevelRewardConfig.ItemGift> daDung)
    {
        if (cu == null) return null;
        if (!string.IsNullOrEmpty(muc.id))
            foreach (var c in cu)
                if (c != null && !daDung.Contains(c) && c.itemId == muc.id) return c;

        string tenChuan = BoDau(muc.ten);
        foreach (var c in cu)
            if (c != null && !daDung.Contains(c) && BoDau(c.displayName) == tenChuan) return c;
        return null;
    }

    static bool GiongNhau(List<LevelRewardConfig.ItemGift> a, List<LevelRewardConfig.ItemGift> b)
    {
        int na = a?.Count ?? 0, nb = b?.Count ?? 0;
        if (na != nb) return false;
        for (int i = 0; i < na; i++)
        {
            var x = a[i]; var y = b[i];
            if (x == null || y == null) { if (x != y) return false; continue; }
            if (x.itemId != y.itemId || x.displayName != y.displayName ||
                x.amount != y.amount || x.icon != y.icon) return false;
        }
        return true;
    }

    static string MoTa(List<LevelRewardConfig.ItemGift> ds)
    {
        if (ds == null || ds.Count == 0) return "(rỗng)";
        var sb = new StringBuilder();
        for (int i = 0; i < ds.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var g = ds[i];
            sb.Append(g == null ? "(null)" : $"{g.displayName}[{g.itemId}]×{g.amount}{(g.icon == null ? "(icon null)" : "")}");
        }
        return sb.ToString();
    }

    /// <summary>Chuẩn hoá tên: thường + bỏ dấu (đ→d) + gộp khoảng trắng — để so tên hiển thị.</summary>
    static string BoDau(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant().Replace('đ', 'd');
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (char c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        var parts = sb.ToString().Normalize(NormalizationForm.FormC)
                      .Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }
}
#endif
