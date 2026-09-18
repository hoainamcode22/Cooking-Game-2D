using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GÁN ẢNH GỐC VÀO POPUP NHIỆM VỤ THEO BẢN THIẾT KẾ.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO CÓ TOOL NÀY
/// ══════════════════════════════════════════════════════════════════════════
/// Bản thiết kế lấy ảnh từ chính `Assets/Assetsgame` rồi đổi tên gọn lại
/// (`cachualever3-removebg-preview.png` → `cachua.png`). Đối chiếu MD5 xác nhận
/// **14/14 ảnh trùng byte** với ảnh gốc — nên KHÔNG cần giữ bản sao trong dự án,
/// chỉ cần trỏ đúng ảnh gốc.
///
/// Tool tra theo tên gốc thay vì để người dùng kéo tay 14 ô Sprite. Kéo tay dễ nhầm
/// hai ảnh giống nhau (`cachualever2` vs `cachualever3`, `bapcailuc1` vs `bapcai3`)
/// và không có gì báo là đã nhầm.
/// </summary>
public static class TaskPopupSpriteWireTool
{
    private const string Menu = "Tools/Farm/Popup Nhiệm Vụ/";

    /// <summary>
    /// Tên ô trong <c>UnifiedTaskPopupSprites</c> → đường dẫn ảnh gốc.
    /// Đường dẫn lấy từ kết quả đối chiếu MD5, không phải đoán theo tên.
    /// </summary>
    private static readonly (string o, string duongDan)[] BangGan =
    {
        // ── icon phần thưởng ────────────────────────────────────────────────
        ("coinIcon",    "Assets/Assetsgame/Icon_vang.png"),
        ("diamondIcon", "Assets/Assetsgame/kimcuong-removebg-preview.png"),
        // Sao xanh = EXP (Nhiệm vụ), Cúp vàng = Thành tựu
        ("expIcon",     "Assets/Assetsgame/iconsao-removebg-preview.png"),
        ("trophyIcon",  "Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_trophy_gold.png"),
        ("chestIcon",   "Assets/Assetsgame/Icon_Processed/NhiemVu/icon_chest_gold.png"),

        // ── nút đóng & ribbon & khung ─────────────────────────────────────────
        ("closeButton", UIStandardSprites.PathClose),
        ("ribbon",      "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites/shop_banner_ribbon.png"),

        // ── icon 3 tab ──────────────────────────────────────────────────────
        ("missionTabIcon",     "Assets/Assetsgame/Icon_Processed/NhiemVu/icon_tab_mission.png"),
        ("dailyTabIcon",       "Assets/Assetsgame/Icon_Processed/DangNhap/login_calendar.png"),
        ("achievementTabIcon", "Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_trophy_gold.png"),

        // ── icon danh mục nhiệm vụ ──────────────────────────────────────────
        ("iconHarvest", "Assets/Art/UI/Icons/Missions/mission_harvest.png"),
        ("iconPlant",   "Assets/Art/UI/Icons/Missions/mission_plant.png"),
        ("iconCook",    "Assets/Art/UI/Icons/Missions/mission_cook.png"),
        ("iconDeliver", "Assets/Assetsgame/PopupArt_Custom/icon_delivery_runner.png"),
        ("iconAnimal",  "Assets/Assetsgame/PopupArt_Custom/icon_feed_chicken.png"),
        ("iconShop",    "Assets/Art/UI/Icons/Missions/mission_shop.png"),
        ("iconProcess", "Assets/Assetsgame/PopupArt_Custom/icon_factory_process.png"),

        // ── huy hiệu thành tựu ──────────────────────────────────────────────
        ("badgeHarvest", "Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_harvest_wheat.png"),
        ("badgeChef",    "Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_chef_hat.png"),
        ("badgeAnimal",  "Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_ranch_horseshoe.png"),
        ("badgeCargo",   "Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_boat_cargo.png"),
        ("badgeCrown",   "Assets/Assetsgame/PopupArt_Custom/badge_star_ribbon.png"),
        ("badgeSprout",  "Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_sprout_shield.png"),
        ("badgeStar",    "Assets/Assetsgame/PopupArt_Custom/badge_star_ribbon.png"),
    };

    /// <summary>
    /// Icon nhiệm vụ: `MissionData.missionIcon`. Tra theo `targetItemId` để mỗi nhiệm vụ
    /// hiện đúng hình nông sản/món ăn của nó, thay vì cùng một icon cho cả 307 nhiệm vụ.
    /// </summary>
    private static readonly (string itemId, string duongDan)[] BangIconNhiemVu =
    {
        // Cây trồng & Hạt giống
        ("rice",               "Assets/Assetsgame/iconlua-removebg-preview.png"),
        ("lua",                "Assets/Assetsgame/iconlua-removebg-preview.png"),
        ("cachua",             "Assets/Assetsgame/cachualever3-removebg-preview.png"),
        ("bapcai",             "Assets/Assetsgame/bapcai3-removebg-preview.png"),
        ("carot",              "Assets/Assetsgame/hatgiong/carot-removebg-preview.png"),
        ("ca_rot",             "Assets/Assetsgame/hatgiong/carot-removebg-preview.png"),
        ("ngo",                "Assets/Assetsgame/hatgiong/bap-removebg-preview.png"),
        ("bap",                "Assets/Assetsgame/hatgiong/bap-removebg-preview.png"),
        ("corn",               "Assets/Assetsgame/hatgiong/bap-removebg-preview.png"),
        ("khoai_tay",          "Assets/Assetsgame/hatgiong/iconKhoaiTay.png"),
        ("khoaitay",           "Assets/Assetsgame/hatgiong/iconKhoaiTay.png"),
        ("bi_do",              "Assets/Assetsgame/hatgiong/bido-removebg-preview.png"),
        ("bido",               "Assets/Assetsgame/hatgiong/bido-removebg-preview.png"),
        ("dua_hau",            "Assets/Assetsgame/hatgiong/duahau-removebg-preview.png"),
        ("duahau",             "Assets/Assetsgame/hatgiong/duahau-removebg-preview.png"),
        ("mia",                "Assets/Assetsgame/hatgiong/mia-removebg-preview.png"),
        ("sugarcane",          "Assets/Assetsgame/hatgiong/mia-removebg-preview.png"),
        ("ot",                 "Assets/Assetsgame/hatgiong/ot-removebg-preview.png"),
        ("chili",              "Assets/Assetsgame/hatgiong/ot-removebg-preview.png"),
        ("nam",                "Assets/Assetsgame/hatgiong/namlever3-removebg-preview.png"),
        ("chanh",              "Assets/Assetsgame/hatgiong/chanh-removebg-preview.png"),
        ("caytieu",            "Assets/Assetsgame/hatgiong/tieulever3-removebg-preview.png"),
        ("pepper",             "Assets/Assetsgame/hatgiong/tieulever3-removebg-preview.png"),

        // Hoa
        ("hoa_hong",           "Assets/Assetsgame/Hoa/hoahong-removebg-preview.png"),
        ("hoa_lan",            "Assets/Assetsgame/Hoa/hoalan-removebg-preview.png"),
        ("huong_duong",        "Assets/Assetsgame/Hoa/hoahuongduong-removebg-preview.png"),
        ("tulip",              "Assets/Assetsgame/Hoa/tulip-removebg-preview.png"),
        ("hoaanhthao",         "Assets/Assetsgame/Hoa/hoaanhthao-removebg-preview.png"),
        ("hoacamtucau",        "Assets/Assetsgame/Hoa/hoacamtucau-removebg-preview.png"),
        ("hoacuctrang",        "Assets/Assetsgame/Hoa/hoacuctrang-removebg-preview.png"),
        ("hoacucvantho",       "Assets/Assetsgame/Hoa/hoacucvantho-removebg-preview.png"),
        ("hoamaudon",          "Assets/Assetsgame/Hoa/hoamaudon-removebg-preview.png"),
        ("hoaoaihuong",        "Assets/Assetsgame/Hoa/hoaoaihuong-removebg-preview.png"),

        // Vật nuôi & Sản phẩm chuồng
        ("pork",               "Assets/Assetsgame/iconthitheooo-removebg-preview.png"),
        ("egg",                "Assets/Assetsgame/conga-removebg-preview.png"),
        ("chicken_meat",       "Assets/Assetsgame/conga-removebg-preview.png"),
        ("beef",               "Assets/Assetsgame/Bò/Heo/iconbo-removebg-preview.png"),
        ("milk",               "Assets/Assetsgame/suamilk.png"),
        ("pig_pen",            "Assets/Assetsgame/Buiding/icon_chuong_heo_v2.png"),
        ("chuong_heo",         "Assets/Assetsgame/Buiding/icon_chuong_heo_v2.png"),
        ("chicken_pen",        "Assets/Assetsgame/Buiding/icon_chuong_ga_v2.png"),
        ("chuong_ga",          "Assets/Assetsgame/Buiding/icon_chuong_ga_v2.png"),

        // Món ăn & Nấu nướng
        ("ga_xao_ot",          "Assets/Assetsgame/PopupArt_Custom/icon_chili_chicken.png"),
        ("pho_beef",           "Assets/Art/UI/Icons/Missions/mission_cook.png"),
        ("trung_chien_ca_chua","Assets/Art/UI/Icons/Missions/mission_cook.png"),
        ("bo_ham_ca_rot",      "Assets/Art/UI/Icons/Missions/mission_cook.png"),
        ("com_chien_trung",    "Assets/Art/UI/Icons/Missions/mission_cook.png"),

        // Chế biến & Đơn hàng
        ("nuoc_mia_ep",        "Assets/Assetsgame/PopupArt_Custom/icon_factory_process.png"),
        ("pho_mai",            "Assets/Assetsgame/PopupArt_Custom/icon_factory_process.png"),
        ("bot_gao",            "Assets/Assetsgame/PopupArt_Custom/icon_factory_process.png"),
        ("order",              "Assets/Assetsgame/PopupArt_Custom/icon_delivery_runner.png"),
        ("delivery",           "Assets/Assetsgame/PopupArt_Custom/icon_delivery_runner.png"),
    };

    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(Menu + "1 · Kiểm tra ảnh gốc có đủ không", false, 1)]
    public static void KiemTraAnh()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ ẢNH GỐC CHO POPUP NHIỆM VỤ ═══\n");

        int thieu = 0;
        foreach (var (o, dd) in BangGan)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dd);
            if (s == null) thieu++;
            sb.AppendLine($"  {(s != null ? "có  " : "THIẾU")}  {o,-22} {dd}");
        }

        sb.AppendLine();
        foreach (var (id, dd) in BangIconNhiemVu)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dd);
            if (s == null) thieu++;
            sb.AppendLine($"  {(s != null ? "có  " : "THIẾU")}  icon:{id,-17} {dd}");
        }

        sb.AppendLine();
        sb.AppendLine(thieu == 0
            ? "  Đủ hết. Chạy mục 2 để gán."
            : $"  ⚠ Thiếu {thieu} ảnh. Ảnh thiếu sẽ để trống, code tự vẽ hình tạm thay thế.");

        // Ba mảnh art khung chưa có: chủ dự án sẽ vẽ sau.
        sb.AppendLine("\n  Chưa có (code tự dựng bằng gradient, vẽ xong thì gán vào):");
        sb.AppendLine("     boardFrame        ← ván gỗ nền 1300×850");
        sb.AppendLine("     ribbon            ← ribbon tiêu đề");
        sb.AppendLine("     tabButton         ← nền tab thường");
        sb.AppendLine("     selectedTabButton ← nền tab đang chọn");

        Debug.Log(sb.ToString());
    }

    [MenuItem(Menu + "2 · Gán ảnh vào popup trong scene", false, 2)]
    public static void GanAnh()
    {
        var popup = Object.FindFirstObjectByType<UnifiedTaskPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            EditorUtility.DisplayDialog("Popup nhiệm vụ",
                "Không thấy UnifiedTaskPopupUI trong scene đang mở.\n\n" +
                "Popup này tự dựng lúc chạy nên có thể chưa có object nào. " +
                "Bấm Play một lần cho nó sinh ra, hoặc gán tay trong prefab.", "OK");
            return;
        }

        var so = new SerializedObject(popup);
        SerializedProperty spr = so.FindProperty("sprites");
        if (spr == null)
        {
            Debug.LogError("[PopupNV] Không tìm thấy field 'sprites' — tên field đã đổi?");
            return;
        }

        int gan = 0;
        var sb = new StringBuilder();

        foreach (var (o, dd) in BangGan)
        {
            SerializedProperty p = spr.FindPropertyRelative(o);
            if (p == null) { sb.AppendLine($"  ⚠ không có ô '{o}'"); continue; }

            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dd);
            if (s == null) { sb.AppendLine($"  ⚠ không thấy ảnh {dd}"); continue; }

            if (p.objectReferenceValue == s) continue;
            p.objectReferenceValue = s;
            gan++;
            sb.AppendLine($"  {o,-22} ← {System.IO.Path.GetFileName(dd)}");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(popup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);

        Debug.Log($"[PopupNV] Đã gán {gan} ảnh.\n{sb}\n→ Ctrl+S để lưu scene.");
    }

    [MenuItem(Menu + "3 · Gán icon cho từng nhiệm vụ theo vật phẩm", false, 3)]
    public static void GanIconNhiemVu()
    {
        // Nạp sẵn sprite một lần, tránh LoadAssetAtPath trong vòng lặp.
        var kho = new Dictionary<string, Sprite>();
        foreach (var (id, dd) in BangIconNhiemVu)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dd);
            if (s != null) kho[id] = s;
        }

        var iconHarvest = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_harvest.png");
        var iconPlant   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_plant.png");
        var iconCook    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_cook.png");
        var iconDeliver = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/PopupArt_Custom/icon_delivery_runner.png");
        var iconAnimal  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/PopupArt_Custom/icon_feed_chicken.png");
        var iconShop    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_shop.png");
        var iconProcess = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/PopupArt_Custom/icon_factory_process.png");

        var badgeHarvest = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_harvest_wheat.png");
        var badgeChef    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_chef_hat.png");
        var badgeAnimal  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_ranch_horseshoe.png");
        var badgeCargo   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_boat_cargo.png");
        var badgeCrown   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_crown_ruby.png");
        var badgeTrophy  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Icon_Processed/ThanhTuu/achieve_trophy_gold.png");

        int gan = 0, boQua = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:MissionData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<MissionData>(path);
            if (m == null) continue;

            Sprite chosen = null;
            string id = (m.targetItemId ?? string.Empty).Trim().ToLowerInvariant();
            if (id.Length > 0 && kho.TryGetValue(id, out Sprite s))
            {
                chosen = s;
            }
            else if (path.Contains("Achievements") || m.name.StartsWith("Mission_a_"))
            {
                string mName = m.name.ToLowerInvariant();
                if (mName.Contains("cook") || mName.Contains("chef") || mName.Contains("beefdish"))
                    chosen = badgeChef;
                else if (mName.Contains("harvest") || mName.Contains("wheat"))
                    chosen = badgeHarvest;
                else if (mName.Contains("order") || mName.Contains("cargo") || mName.Contains("boat"))
                    chosen = badgeCargo;
                else if (mName.Contains("animal") || mName.Contains("pen") || mName.Contains("process"))
                    chosen = badgeAnimal;
                else if (mName.Contains("level"))
                    chosen = badgeCrown;
                else
                    chosen = badgeTrophy;
            }
            else
            {
                switch (m.eventType)
                {
                    case MissionEventType.HarvestItem: chosen = iconHarvest; break;
                    case MissionEventType.PlantCrop: chosen = iconPlant; break;
                    case MissionEventType.CookDish: chosen = iconCook; break;
                    case MissionEventType.DeliverOrder: chosen = iconDeliver; break;
                    case MissionEventType.FeedAnimal:
                    case MissionEventType.CollectAnimalProduct: chosen = iconAnimal; break;
                    case MissionEventType.BuyShopItem:
                    case MissionEventType.BuySeed:
                    case MissionEventType.SellAtStall: chosen = iconShop; break;
                    case MissionEventType.ReachLevel: chosen = badgeCrown; break;
                    case MissionEventType.LoadTrainCargo: chosen = badgeCargo; break;
                }
            }

            if (chosen != null)
            {
                if (m.missionIcon != chosen)
                {
                    Undo.RecordObject(m, "Gán icon nhiệm vụ");
                    m.missionIcon = chosen;
                    EditorUtility.SetDirty(m);
                    gan++;
                }
                else
                {
                    boQua++;
                }
            }
            else
            {
                boQua++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PopupNV] Đã gán/cập nhật icon cho {gan} nhiệm vụ, giữ nguyên {boQua}.");
    }

    [MenuItem(Menu + "4 · Dọn thư mục Assets/thietke", false, 20)]
    public static void DonThuMucThietKe()
    {
        const string tm = "Assets/thietke";
        if (!AssetDatabase.IsValidFolder(tm))
        {
            Debug.Log("[PopupNV] Không có thư mục Assets/thietke — đã dọn từ trước.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Dọn thư mục thiết kế",
                "Xoá HẲN Assets/thietke (html, js, md và 14 ảnh sao chép)?\n\n" +
                "14 ảnh trong đó đã được đối chiếu MD5 và TRÙNG BYTE với ảnh gốc\n" +
                "trong Assets/Assetsgame — popup đang trỏ vào ảnh gốc, không phải\n" +
                "bản sao này.\n\n" +
                "Chạy mục 2 và 3 TRƯỚC khi xoá.",
                "Xoá", "Huỷ"))
            return;

        if (AssetDatabase.DeleteAsset(tm))
            Debug.Log("[PopupNV] ✅ Đã xoá Assets/thietke. Dự án nhẹ đi ~3,7MB.");
        else
            Debug.LogError("[PopupNV] Xoá không được — có thể file đang mở trong Unity. " +
                           "Đóng tab Inspector đang xem file trong đó rồi thử lại.");
    }
}
