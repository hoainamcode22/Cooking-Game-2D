using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  M0-2 — SaveAdapters: mỗi hệ một adapter Capture/Restore, CHỈ gọi public API
//  thật của manager (đã đối chiếu từng tên hàm với source — xem SAVE_DESIGN.md).
//
//  Nguyên tắc:
//    • Mỗi adapter bọc try/catch riêng — một hệ hỏng không làm mất cả save.
//    • Hệ nào đã tự lưu PlayerPrefs → adapter chỉ CHỤP (snapshot đọc được) +
//      mirror thô khoá PlayerPrefs; KHÔNG có đường ghi thứ hai.
//    • Hệ TÀU: capture/restore thật, tự kích hoạt qua reflection khi
//      TrainManager.PATCH.md được duyệt (trước đó vẫn biên dịch và chạy bình thường).
// ═══════════════════════════════════════════════════════════════════════════
public static class SaveAdapters
{
    /// <summary>
    /// Bản save gần nhất còn trong bộ nhớ. SaveBootstrap nạp từ đĩa lúc khởi động
    /// (PrimeFromDisk); mỗi lần CaptureAll sẽ thay mới. Dùng để:
    ///   • giữ lại khoá mirror của hệ không có mặt trong scene hiện tại (đang ở scene
    ///     bếp thì không tìm thấy PlotController nào — không được vì thế mà rớt khoá),
    ///   • giữ snapshot tàu để áp lại khi TrainManager per-scene được dựng lại.
    /// </summary>
    public static FarmSaveData LastKnown { get; private set; }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        LastKnown = null;
        TrainAdapter.LiveCaptureEnabled = true;
    }
#endif

    /// <summary>SaveBootstrap gọi đúng một lần lúc khởi động với dữ liệu đọc từ đĩa (có thể null).</summary>
    public static void PrimeFromDisk(FarmSaveData fromDisk)
    {
        if (LastKnown == null && fromDisk != null)
            LastKnown = fromDisk;
    }

    /// <summary>Chụp toàn bộ. Không ném exception (từng mục tự nuốt và log [Save]).</summary>
    public static FarmSaveData CaptureAll()
    {
        var d = new FarmSaveData();

        Guard("economy",   () => EconomyAdapter.Capture(d));
        Guard("progress",  () => ProgressAdapter.Capture(d));
        Guard("warehouse", () => WarehouseAdapter.Capture(d));
        Guard("inventory", () => InventoryAdapter.Capture(d));
        Guard("plots",     () => PlotAdapter.Capture(d));
        Guard("pens",      () => PenAdapter.Capture(d));
        Guard("train",     () => TrainAdapter.Capture(d));
        Guard("tutorial",  () => TutorialAdapter.Capture(d));
        Guard("mirror",    () => PrefsMirrorAdapter.Capture(d));

        d.missionNote = "Mission tu luu PlayerPrefs: MISSION_PROGRESS_V1 + MISSION_CLAIMED_* " +
                        "+ ACHIEVEMENT_CLAIMED_* (da mirror trong prefsMirror, khong luu lai lan 2).";

        LastKnown = d;
        return d;
    }

    private static void Guard(string ten, Action capture)
    {
        try { capture(); }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] Capture mục '{ten}' lỗi: {e.Message} — bỏ qua mục này, save phần còn lại.");
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  KINH TẾ — FarmEconomyManager (đã tự lưu FARM_ECONOMY_GOLD/GEMS)
    // ═════════════════════════════════════════════════════════════════════
    public static class EconomyAdapter
    {
        public static void Capture(FarmSaveData d)
        {
            if (FarmEconomyManager.Instance != null)
            {
                d.gold = FarmEconomyManager.Instance.Gold;
                d.gems = FarmEconomyManager.Instance.Gems;
            }
            else
            {
                // Ngoài Play Mode / manager chưa dựng: đọc thẳng nguồn thật.
                d.gold = PlayerPrefs.GetInt("FARM_ECONOMY_GOLD", 0);
                d.gems = PlayerPrefs.GetInt("FARM_ECONOMY_GEMS", 0);
            }
        }
        // Restore: KHÔNG CÓ — hệ tự đọc PlayerPrefs ở Awake; phục hồi qua prefsMirror.
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CẤP / EXP — PlayerProgressManager (đã tự lưu PLAYER_LEVEL/PLAYER_EXP)
    // ═════════════════════════════════════════════════════════════════════
    public static class ProgressAdapter
    {
        public static void Capture(FarmSaveData d)
        {
            if (PlayerProgressManager.Instance != null)
            {
                d.level = PlayerProgressManager.Instance.Level;
                d.exp   = PlayerProgressManager.Instance.CurrentExp;
            }
            else
            {
                d.level = PlayerPrefs.GetInt("PLAYER_LEVEL", 1);
                d.exp   = PlayerPrefs.GetInt("PLAYER_EXP", 0);
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  KHO HẠT GIỐNG — WarehouseManager.Items (đã tự lưu FARM_WAREHOUSE)
    // ═════════════════════════════════════════════════════════════════════
    public static class WarehouseAdapter
    {
        public static void Capture(FarmSaveData d)
        {
            var wm = WarehouseManager.Instance;
            if (wm == null)
            {
                // Scene bếp / ngoài Play: giữ bản chụp cũ để save.json không "quên" kho.
                if (LastKnown != null) d.warehouseSeeds = LastKnown.warehouseSeeds;
                return;
            }

            foreach (WarehouseItemEntry it in wm.Items)
            {
                if (it == null || string.IsNullOrEmpty(it.itemId) || it.amount <= 0) continue;
                d.warehouseSeeds.Add(new SaveItemStack
                {
                    itemId = it.itemId, displayName = it.displayName, amount = it.amount
                });
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  KHO NÔNG SẢN — FarmInventoryManager (đã tự lưu FARM_INVENTORY_SAVE)
    // ═════════════════════════════════════════════════════════════════════
    public static class InventoryAdapter
    {
        public static void Capture(FarmSaveData d)
        {
            var inv = FarmInventoryManager.Instance;
            if (inv == null)
            {
                if (LastKnown != null) d.inventoryItems = LastKnown.inventoryItems;
                return;
            }

            foreach (KeyValuePair<string, int> kv in inv.GetOrderedItems())
                d.inventoryItems.Add(new SaveItemStack { itemId = kv.Key, amount = kv.Value });
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Ô ĐẤT — PlotController (mỗi ô tự lưu PLOT_NORMAL_{id}/PLOT_RARE_{id})
    //  Chỉ chụp phần public: id, unlocked, planted, ready, cropId.
    //  Thời gian trồng không public — nhưng nằm nguyên trong khoá PLOT_* (đã mirror).
    // ═════════════════════════════════════════════════════════════════════
    public static class PlotAdapter
    {
        public static void Capture(FarmSaveData d)
        {
            PlotController[] plots = UnityEngine.Object.FindObjectsByType<PlotController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (plots == null || plots.Length == 0)
            {
                if (LastKnown != null) d.plots = LastKnown.plots;   // scene bếp — giữ bản cũ
                return;
            }

            foreach (PlotController p in plots)
            {
                if (p == null) continue;
                d.plots.Add(new SavePlotSnapshot
                {
                    plotId     = p.PlotId,
                    isRare     = p.IsRarePlot,
                    isFlower   = p.Category == PlotCategory.Flower,
                    isUnlocked = p.IsUnlocked,
                    isPlanted  = p.IsPlanted,
                    isReady    = p.IsReady,
                    cropId     = p.CurrentCrop != null ? p.CurrentCrop.cropId : ""
                });
            }
        }

        /// <summary>Khoá PlayerPrefs của các ô đang có mặt trong scene — cho PrefsMirrorAdapter.</summary>
        public static IEnumerable<KeyValuePair<string, string>> LiveKeys()
        {
            PlotController[] plots = UnityEngine.Object.FindObjectsByType<PlotController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (plots == null) yield break;

            foreach (PlotController p in plots)
            {
                if (p == null) continue;
                // Định dạng khoá đối chiếu PlotController.KeyFor(): PLOT_RARE_{id} / PLOT_NORMAL_{id}
                string key = p.IsRarePlot ? $"PLOT_RARE_{p.PlotId}" : $"PLOT_NORMAL_{p.PlotId}";
                yield return new KeyValuePair<string, string>(key, "string");
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CHUỒNG — PenMiniPanelUI (đã tự lưu PenState_/PenFood_/PenStartTime_{penId})
    //  penId lấy từ PenMiniPanelConfig (ScriptableObject, field public) — không cần
    //  đụng PenMiniPanelUI (config của nó là private).
    // ═════════════════════════════════════════════════════════════════════
    public static class PenAdapter
    {
        /// <summary>pen_01..pen_04 theo PenMiniPanelConfig; chừa sẵn tới 06 cho chuồng mới.</summary>
        private static readonly string[] FallbackPenIds =
            { "pen_01", "pen_02", "pen_03", "pen_04", "pen_05", "pen_06" };

        public static List<string> CollectPenIds()
        {
            var ids = new List<string>();

            // Config nào đã được scene nạp thì Resources.FindObjectsOfTypeAll thấy hết.
            PenMiniPanelConfig[] configs = Resources.FindObjectsOfTypeAll<PenMiniPanelConfig>();
            if (configs != null)
                foreach (PenMiniPanelConfig c in configs)
                    if (c != null && !string.IsNullOrEmpty(c.penId) && !ids.Contains(c.penId))
                        ids.Add(c.penId);

            foreach (string id in FallbackPenIds)
                if (!ids.Contains(id)) ids.Add(id);

            return ids;
        }

        public static void Capture(FarmSaveData d)
        {
            foreach (string id in CollectPenIds())
            {
                if (!PlayerPrefs.HasKey("PenState_" + id)) continue;   // chuồng chưa từng lưu

                d.pens.Add(new SavePenSnapshot
                {
                    penId     = id,
                    state     = PlayerPrefs.GetInt("PenState_" + id, 0),
                    foodId    = PlayerPrefs.GetString("PenFood_" + id, ""),
                    startUnix = PlayerPrefs.GetString("PenStartTime_" + id, "0")
                });
            }

            if (d.pens.Count == 0 && LastKnown != null)
                d.pens = LastKnown.pens;
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TÀU — TrainManager. HỆ DUY NHẤT CHƯA CÓ SAVE.
    //
    //  Public API hiện tại chỉ đọc được State + SlotData; _tripIndex private và không
    //  có hàm phục hồi → cần patch additive (TrainManager.PATCH.md). Adapter dò 2 method
    //  CaptureTripSnapshot/RestoreTripSnapshot bằng reflection:
    //    • CHƯA patch: chụp phần đọc được (debug), restorable = false, log nhắc 1 lần.
    //    • ĐÃ patch:  tự kích hoạt capture/restore đầy đủ, không phải sửa file này.
    // ═════════════════════════════════════════════════════════════════════
    public static class TrainAdapter
    {
        private static bool _daNhacThieuPatch;

        /// <summary>
        /// FALSE = tàu trong scene CHƯA được áp lại snapshot cũ (đang init / đang chờ
        /// SaveBootstrap phục hồi) → capture phải GIỮ snapshot cũ thay vì chụp chuyến
        /// trắng vừa init. Không có cờ này, một lần auto-save rơi vào khoảng vài giây
        /// tàu đang chạy về ga sẽ ghi đè chuyến thật của người chơi bằng chuyến #0 rỗng.
        /// SaveBootstrap tắt/bật cờ quanh mỗi lần phục hồi.
        /// </summary>
        public static bool LiveCaptureEnabled = true;

        public static void Capture(FarmSaveData d)
        {
            var tm = TrainManager.Instance;
            if (tm == null || !LiveCaptureEnabled)
            {
                // Scene không có tàu (bếp) / tàu chưa được phục hồi — giữ snapshot cũ,
                // đừng xoá chuyến đang lưu.
                if (LastKnown != null && LastKnown.train != null) d.train = LastKnown.train;
                return;
            }

            d.train.hasData = tm.SlotData != null;
            d.train.state   = (int)tm.State;

            if (tm.SlotData != null)
            {
                foreach (TrainWagonSlotData s in tm.SlotData)
                {
                    if (s == null) continue;
                    d.train.slots.Add(new SaveTrainSlot
                    {
                        itemId         = s.itemId,
                        mode           = (int)s.mode,
                        currentAmount  = s.currentAmount,
                        requiredAmount = s.requiredAmount,
                        rewardAmount   = s.rewardAmount,
                        isCollected    = s.isCollected
                    });
                }
            }

            // Phần restorable — chỉ khi patch đã duyệt.
            MethodInfo mi = typeof(TrainManager).GetMethod(
                "CaptureTripSnapshot", BindingFlags.Public | BindingFlags.Instance);
            if (mi == null)
            {
                d.train.restorable  = false;
                d.train.snapshotJson = "";
                if (!_daNhacThieuPatch)
                {
                    _daNhacThieuPatch = true;
                    Debug.LogWarning("[Save] Train: TrainManager chưa có CaptureTripSnapshot — chỉ chụp " +
                                     "để đọc, KHÔNG phục hồi được. Duyệt TrainManager.PATCH.md để bật.");
                }
                return;
            }

            object snap = mi.Invoke(tm, null);
            d.train.snapshotJson = snap != null ? JsonUtility.ToJson(snap) : "";
            d.train.restorable   = !string.IsNullOrEmpty(d.train.snapshotJson);
        }

        /// <summary>
        /// Áp snapshot vào TrainManager đang sống. SaveBootstrap gọi sau khi tàu đã init
        /// xong (State == WaitingForLoad). Trả true nếu đã áp.
        /// </summary>
        public static bool TryRestore(SaveTrainSection train)
        {
            if (train == null || !train.restorable || string.IsNullOrEmpty(train.snapshotJson))
                return false;

            var tm = TrainManager.Instance;
            if (tm == null) return false;

            MethodInfo mi = typeof(TrainManager).GetMethod(
                "RestoreTripSnapshot", BindingFlags.Public | BindingFlags.Instance);
            if (mi == null) return false;   // save từ máy đã patch, code này chưa patch

            ParameterInfo[] ps = mi.GetParameters();
            if (ps.Length != 1) return false;

            object snap;
            try { snap = JsonUtility.FromJson(train.snapshotJson, ps[0].ParameterType); }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Train: snapshot hỏng ({e.Message}) — bỏ qua, tàu chạy chuyến mới.");
                return false;
            }
            if (snap == null) return false;

            mi.Invoke(tm, new[] { snap });
            return true;
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TUTORIAL — các cờ int (đã tự lưu, họ SAVE_VER_TUTORIAL)
    // ═════════════════════════════════════════════════════════════════════
    public static class TutorialAdapter
    {
        // Nguồn: TutorialManager.PrefKeyDone, TutorialPrePlant.PREF_KEY,
        // StarterInventorySetup.PREF_KEY, AnimalGuideController (4 khoá GUIDE_*).
        // Các const đó private → chép chuỗi, có SaveVersionGuard.AllFamilies làm chốt đối chiếu.
        public static readonly string[] Keys =
        {
            "TUTORIAL_MAIN_DONE", "TUTORIAL_PREPLANT_DONE", "STARTER_ITEMS_GIVEN",
            "ANIMAL_GUIDE_COOP_FEED_DONE", "GUIDE_DELIVER_DONE", "GUIDE_TRAIN_DONE",
            "GUIDE_COOKING_DONE"
        };

        public static void Capture(FarmSaveData d)
        {
            foreach (string k in Keys)
                if (PlayerPrefs.HasKey(k))
                    d.tutorialFlags.Add(new SaveKV { key = k, value = PlayerPrefs.GetInt(k, 0) });
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MIRROR THÔ — lớp phục hồi thật sự.
    //  Sao chép NGUYÊN VĂN giá trị các manager tự ghi vào PlayerPrefs. Vì là bản
    //  sao thô nên trung thực 100% (kể cả thời gian trồng của ô đất, blob JSON con
    //  của từng hệ với saveVersion riêng của nó).
    // ═════════════════════════════════════════════════════════════════════
    public static class PrefsMirrorAdapter
    {
        // ── Bảng khoá TĨNH: khoá nào của hệ nào — đối chiếu SAVE_DESIGN.md §1.1 ──
        // (một số const gốc là private nên phải chép chuỗi; chuỗi khoá save không
        //  đổi được nếu không muốn phá save đang có, nên chép là an toàn.)
        private static readonly string[] IntKeys =
        {
            "FARM_ECONOMY_GOLD", "FARM_ECONOMY_GEMS",                      // FarmEconomyManager
            "PLAYER_LEVEL", "PLAYER_EXP",                                  // PlayerProgressManager
            FarmInventoryManager.WarehouseLevelPrefsKey,                   // "WAREHOUSE_LEVEL" (public const)
            "PLAYER_PROFILE_AVATAR_INDEX", "PLAYER_PROFILE_WAREHOUSE_LEVEL",
            "PLAYER_PROFILE_ACHIEVEMENT_COUNT",                            // AvatarProfilePopupUI
            "MARKET_TIMER_SAVE_VERSION", "MARKET_TIMER_CYCLE_INDEX",
            "MARKET_REFRESH_PAID_COUNT",                                   // MarketRefreshTimer
            "UNIFIED_TASK_DAILY_STREAK",                                   // UnifiedTaskPopupUI
            "TouristBoat_IntroDone",                                       // BoatDockManager
        };

        private static readonly string[] StringKeys =
        {
            "FARM_WAREHOUSE",            // WarehouseManager (blob JSON, saveVersion trong blob)
            "FARM_INVENTORY_SAVE",       // FarmInventoryManager (blob)
            "FARM_PLACED_BUILDINGS",     // PlacementManager.BuildingsSaveKey (blob)
            "FARM_CONSTRUCTION_SITES",   // ConstructionManager.SaveKey (blob, v2)
            "FARM_DRAG_OBJECT_POS",      // ObjectDragHandler (blob)
            "FARM_PLAYER_STALL",         // PlayerStallManager (blob)
            "OrderBoard_Save",           // OrderBoardManager (blob)
            "KITCHEN_TRANSFER_SAVE",     // KitchenTransferManager (blob)
            "MISSION_PROGRESS_V1",       // MissionProgressTracker (blob)
            "PLAYER_PROFILE_NAME",       // AvatarProfilePopupUI
            "MARKET_TIMER_NEXT_UTC_TICKS", "MARKET_REFRESH_PAID_DATE",     // MarketRefreshTimer
            "UNIFIED_TASK_DAILY_LAST_SEEN", "UNIFIED_TASK_DAILY_CLAIMED_DATE",
        };

        public static void Capture(FarmSaveData d)
        {
            // (khoá → type) — dùng Dictionary để union không sinh trùng.
            var keys = new Dictionary<string, string>();

            foreach (string k in IntKeys)    keys[k] = "int";
            foreach (string k in StringKeys) keys[k] = "string";

            // Cờ tutorial (int).
            foreach (string k in TutorialAdapter.Keys) keys[k] = "int";

            // Dấu phiên bản mọi họ save — SaveVersionGuard.AllFamilies là public static (nguồn sự thật).
            foreach (string family in SaveVersionGuard.AllFamilies)
                keys[SaveVersionGuard.KeyFor(family)] = "int";

            // Ô đất trong scene hiện tại.
            foreach (KeyValuePair<string, string> kv in PlotAdapter.LiveKeys())
                keys[kv.Key] = kv.Value;

            // Chuồng.
            foreach (string id in PenAdapter.CollectPenIds())
            {
                keys["PenState_" + id]     = "int";
                keys["PenFood_" + id]      = "string";
                keys["PenStartTime_" + id] = "string";
            }

            // Bến thuyền du lịch — BoatDockManager.DockCount = 3; dò dư tới 10 cho an toàn,
            // HasKey chặn nên không sinh khoá rác.
            for (int i = 0; i < 10; i++)
            {
                keys[$"TouristBoat_Unlocked_{i}"]  = "int";
                keys[$"TouristBoat_AnchorUtc_{i}"] = "string";
            }

            // Cờ đã-nhận nhiệm vụ/thành tựu — id liệt kê qua MissionDatabase (public missions).
            // Định dạng khoá đối chiếu UnifiedTaskPopupUI/MissionHudButtonUI:
            //   MISSION_CLAIMED_{id} · MISSION_CLAIMED_DAILY_{yyyyMMdd}_{id} · ACHIEVEMENT_CLAIMED_{id}
            try
            {
                string today = DateTime.Now.ToString("yyyyMMdd");
                MissionDatabase[] dbs = Resources.FindObjectsOfTypeAll<MissionDatabase>();
                if (dbs != null)
                {
                    foreach (MissionDatabase db in dbs)
                    {
                        if (db == null || db.missions == null) continue;
                        foreach (MissionData m in db.missions)
                        {
                            if (m == null) continue;
                            string id = m.MissionId;
                            if (string.IsNullOrEmpty(id)) continue;
                            keys[$"MISSION_CLAIMED_{id}"]                 = "int";
                            keys[$"MISSION_CLAIMED_DAILY_{today}_{id}"]   = "int";
                            keys[$"ACHIEVEMENT_CLAIMED_{id}"]             = "int";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Mirror: liệt kê MissionDatabase lỗi ({e.Message}) — " +
                                 "cờ đã-nhận sẽ lấy từ mirror của lần lưu trước (nếu có).");
            }

            // UNION với mirror của lần lưu trước: đang ở scene bếp thì không thấy ô đất,
            // nhưng khoá PLOT_* vẫn phải theo save.json — lấy lại từ bản cũ.
            if (LastKnown != null && LastKnown.prefsMirror != null)
                foreach (SavePrefEntry old in LastKnown.prefsMirror)
                    if (old != null && !string.IsNullOrEmpty(old.key) && !keys.ContainsKey(old.key))
                        keys[old.key] = old.type;

            // Chốt: chỉ ghi khoá THẬT SỰ đang tồn tại trong PlayerPrefs.
            foreach (KeyValuePair<string, string> kv in keys)
            {
                if (!PlayerPrefs.HasKey(kv.Key)) continue;

                var e = new SavePrefEntry { key = kv.Key, type = kv.Value };
                switch (kv.Value)
                {
                    case "int":    e.i = PlayerPrefs.GetInt(kv.Key, 0);         break;
                    case "string": e.s = PlayerPrefs.GetString(kv.Key, "");     break;
                    case "float":  e.f = PlayerPrefs.GetFloat(kv.Key, 0f);      break;
                    default: continue;
                }
                d.prefsMirror.Add(e);
            }
        }
    }
}
