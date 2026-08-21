# SAVE_DESIGN — SaveSystem JSON local (M0-2)

> **Phát hiện quan trọng nhất của đợt scan:** mô tả backlog ("game KHÔNG lưu gì cả") **đã lỗi thời**.
> Codebase hiện tại ĐÃ có một lớp save PlayerPrefs trưởng thành: 8 hệ tự lưu blob JSON có
> `saveVersion` riêng, hơn 20 khoá ghi thẳng số/chuỗi được `SaveVersionGuard` đóng dấu phiên bản,
> và `LuuGopPrefs` lo debounce + flush ở mọi lối thoát (pause/focus/quit/thoát Play Mode).
> Vì luật studio là **không sửa file đang có** và **không lưu trùng một thứ ở hai nơi**, SaveSystem
> JSON mới được thiết kế để **bổ khuyết** chứ không thay thế:
>
> 1. **Lưu hệ duy nhất chưa được lưu: TÀU (TrainManager)** — cần 1 patch nhỏ (xem `TrainManager.PATCH.md`).
> 2. **`save.json` = bản chụp hợp nhất + bản sao dự phòng (mirror)** của toàn bộ khoá PlayerPrefs,
>    ghi file atomic, có `saveVersion` + hook migrate, log `[Save]` — đúng acceptance M0-2.
> 3. **KHÔNG hệ nào bị lưu 2 nơi theo kiểu "2 nguồn sự thật"**: PlayerPrefs vẫn là nguồn sự thật
>    runtime; `save.json` chỉ được dùng để phục hồi khi khoá PlayerPrefs **không tồn tại**
>    (mất registry/đổi máy) và phải kích hoạt **thủ công** qua menu Editor (mặc định KHÔNG tự
>    phục hồi — để không phá tool "CHƠI LẠI TỪ ĐẦU" đang xoá PlayerPrefs).

---

## 1. Kết quả scan từng hệ giữ state runtime

Ký hiệu: ✅ = đã tự lưu PlayerPrefs (để nguyên) · ❌ = chưa lưu · ⬜ = transient, không cần lưu.

### 1.1. Hệ ĐÃ tự lưu PlayerPrefs (KHÔNG lưu lại lần 2 — chỉ mirror thô vào save.json)

| Hệ | Class (file) | State | Khoá PlayerPrefs | Version | Public API đọc |
|---|---|---|---|---|---|
| Tiền tệ | `FarmEconomyManager` (Managers/) | Gold, Gems | `FARM_ECONOMY_GOLD`, `FARM_ECONOMY_GEMS` | `SAVE_VER_FARM_ECONOMY` = 1 | `Instance.Gold/.Gems`, event `OnCurrencyChanged` |
| Cấp / EXP | `PlayerProgressManager` (Scripts/Progression/) | Level, CurrentExp | `PLAYER_LEVEL`, `PLAYER_EXP` | `SAVE_VER_PLAYER_PROGRESS` = 1 | `Instance.Level/.CurrentExp`, event `OnExpChanged`, `OnLevelChanged` |
| Kho HẠT GIỐNG | `WarehouseManager` (Kho/) | items (id, tên, số lượng) | `FARM_WAREHOUSE` (blob JSON) | trong blob, v1 | `Instance.Items` (IReadOnlyList), `OnWarehouseChanged` |
| Kho NÔNG SẢN | `FarmInventoryManager` (Managers/) | items + thứ tự | `FARM_INVENTORY_SAVE` (blob JSON) | trong blob, v1 | `Instance.GetOrderedItems()`, `OnInventoryChanged` |
| Cấp kho | `WarehousePopupUI` (Kho/) | warehouseLevel | `WAREHOUSE_LEVEL` (int) | `SAVE_VER_WAREHOUSE_LEVEL` | `FarmInventoryManager.WarehouseLevelPrefsKey` (public const) |
| Hồ sơ người chơi | `AvatarProfilePopupUI` (Scripts/UI/) | tên, avatar, đếm thành tựu | `PLAYER_PROFILE_NAME/_AVATAR_INDEX/_WAREHOUSE_LEVEL/_ACHIEVEMENT_COUNT` | `SAVE_VER_PLAYER_PROFILE` = 1 | qua PlayerPrefs |
| Ô ĐẤT (từng ô) | `PlotController` (Gameplay/) | state, cropId, startUnixTime, finishUnixTime | `PLOT_NORMAL_{id}` / `PLOT_RARE_{id}` (blob JSON mỗi ô) | trong blob, v1 (+ bảng LegacyPlotIdMap v0→v1) | `PlotId`, `IsRarePlot`, `Category`, `IsUnlocked/IsPlanted/IsGrowing/IsReady/IsEmpty`, `CurrentCrop` — **thời gian trồng KHÔNG public**, nhưng không cần: ô tự Load ở `Start()` |
| Nhà/đồ đã đặt (mua ở shop) | `PlacementManager` (Managers/) | placedBuildings | `FARM_PLACED_BUILDINGS` (blob) — `BuildingsSaveKey` public const | trong blob, v1 | tự phục hồi lúc load scene |
| Công trường đang xây | `ConstructionManager` (Gameplay/) | sites + tiến độ | `FARM_CONSTRUCTION_SITES` (blob) — `SaveKey` public const | trong blob, **v2** | tự phục hồi |
| Vị trí object kéo-thả | `ObjectDragHandler` (Gameplay/) | vị trí từng object | `FARM_DRAG_OBJECT_POS` (blob) | trong blob, v1 | tự phục hồi |
| CHUỒNG (4 chuồng) | `PenMiniPanelUI` + `PenMiniPanelConfig` (Animal/MiniPanel/) | state (Idle/Processing/Ready), thức ăn, mốc thời gian | `PenState_{penId}` (int), `PenFood_{penId}` (string), `PenStartTime_{penId}` (string) — penId = `pen_01..pen_04` (public field trên `PenMiniPanelConfig`) | `SAVE_VER_PEN_STATE` = 1 | tự Load ở `Start()` |
| Quầy hàng người chơi | `PlayerStallManager` (Stall/) | listings đang bán | `FARM_PLAYER_STALL` (blob) | trong blob, v1 | tự phục hồi |
| Chợ — đồng hồ làm mới | `MarketRefreshTimer` (Market/) | mốc refresh, cycleIndex, số lần trả phí | `MARKET_TIMER_SAVE_VERSION/_NEXT_UTC_TICKS/_CYCLE_INDEX`, `MARKET_REFRESH_PAID_COUNT/_PAID_DATE` | `MARKET_TIMER_SAVE_VERSION` = 1 | tự phục hồi |
| Bảng đơn hàng | `OrderBoardManager` (OrderBoard/) | các đơn + hạn | `OrderBoard_Save` (blob) | trong blob, v1 | tự phục hồi |
| Chuyển nguyên liệu sang bếp | `KitchenTransferManager` (data/Farm_Cooking/) | hàng đã chuyển | `KITCHEN_TRANSFER_SAVE` (blob) | trong blob, v1 | tự phục hồi |
| Tiến độ NHIỆM VỤ | `MissionProgressTracker` (Scripts/Mission/) | tiến độ chính + daily | `MISSION_PROGRESS_V1` (blob) | trong blob, v1 | static `ReportEvent/GetProgressFor`, event `OnProgressChanged` — **đã tự lưu, save.json CHỈ THAM CHIẾU (mirror thô), không lưu lại** |
| Cờ ĐÃ NHẬN nhiệm vụ | `UnifiedTaskPopupUI` / `MissionHudButtonUI` | claimed flags | `MISSION_CLAIMED_{id}`, `MISSION_CLAIMED_DAILY_{yyyyMMdd}_{id}`, `ACHIEVEMENT_CLAIMED_{id}`, `UNIFIED_TASK_DAILY_LAST_SEEN/_STREAK/_CLAIMED_DATE` | `SAVE_VER_MISSION` = 1 | id liệt kê được qua `MissionDatabase.missions` (public) → mirror được |
| Cờ TUTORIAL | `TutorialManager`, `TutorialPrePlant`, `StarterInventorySetup`, `AnimalGuideController` | 7 cờ int | `TUTORIAL_MAIN_DONE`, `TUTORIAL_PREPLANT_DONE`, `STARTER_ITEMS_GIVEN`, `ANIMAL_GUIDE_COOP_FEED_DONE`, `GUIDE_DELIVER_DONE`, `GUIDE_TRAIN_DONE`, `GUIDE_COOKING_DONE` | `SAVE_VER_TUTORIAL` = 1 | `TutorialManager.IsTutorialDone` (static) |
| Thuyền du lịch | `BoatDockManager` (TouristBoat/) | bến mở, mốc neo, cờ intro | `TouristBoat_Unlocked_{0..2}`, `TouristBoat_AnchorUtc_{0..2}`, `TouristBoat_IntroDone` (`DockCount` = 3, public const) | (chưa có dấu version — hệ này chưa vào `AllFamilies`) | tự phục hồi |
| Dấu phiên bản các họ save | `SaveVersionGuard` (Managers/) | version từng họ | `SAVE_VER_{family}` — danh sách họ ở `SaveVersionGuard.AllFamilies` (public static) | — | đọc/mirror được |

### 1.2. Hệ CHƯA lưu → save.json phụ trách

| Hệ | Class | State mất khi thoát | Chiến lược |
|---|---|---|---|
| ❌ **TÀU** | `TrainManager` (Train/) | `State` (6 trạng thái), `SlotData[]` (hàng đã nạp dở / thưởng chưa thu), `_tripIndex` (private), `_pendingRewards` (private) | **Capture/restore vào `save.json`**. Public API chỉ đọc được `State` + `SlotData` — KHÔNG đủ: `_tripIndex` private, không có hàm set state → **cần patch bổ sung 2 method + 1 DTO** (additive, xem `TrainManager.PATCH.md`). Adapter dùng **reflection** để tự kích hoạt khi patch được duyệt; chưa patch thì chỉ chụp phần đọc được (debug) và log rõ. Hậu quả nếu không patch: nạp 3/4 toa rồi thoát = mất hàng đã trừ kho; thưởng chưa thu = mất chuyến. |

### 1.3. Hệ transient — cố ý KHÔNG lưu (ghi rõ để khỏi tranh cãi về sau)

| Hệ | Vì sao không lưu |
|---|---|
| ⬜ `FarmLevelManager` | Chỉ là gương của `PlayerProgressManager.Level` (được `SetLevel()` đẩy sang mỗi lần đổi). |
| ⬜ `FeedMillController`, `RotatingGear` | Thuần visual (bánh răng quay). |
| ⬜ `AttendanceManager`, `WelfareEventManager`, `PopupManager`, `ShopManager`, `FarmUIManager` | Thuần UI/popup, không giữ tiến độ. |
| ⬜ `FarmManager` | Chỉ giữ selection + crop database; ô đất tự lưu. |
| ⬜ `CookingChallengeManager` + minigame nấu | Phiên nấu dở là gameplay tính-điểm-theo-lượt; nguyên liệu trừ và thưởng cộng đều qua các kho ĐÃ lưu. Thoát giữa lượt nấu = bỏ lượt (chấp nhận, giống mọi minigame). |
| ⬜ `LocalMarketProvider` (hàng NPC ở chợ) | Cố ý tái sinh **deterministic** từ `cycleSeed` (RNG có hạt) — `MarketRefreshTimer` đã lưu seed/chu kỳ. *Hạn chế đã biết:* cờ "đã bán" của thẻ NPC trong MỘT chu kỳ không lưu → thoát vào lại trong cùng chu kỳ có thể mua lại cùng thẻ. Không phải mất dữ liệu; muốn vá thì là task riêng của hệ chợ. |
| ⬜ `LivestockAI`, `FerryController`, `TrainStationBuilding` | Con vật đi lại / thuyền chạy cảnh / click ga — thuần trình diễn. |

---

## 2. Schema `save.json` (class `FarmSaveData` trong `SaveData.cs`)

> Đặt tên class `FarmSaveData` (file vẫn là `SaveData.cs` như spec) vì `WarehouseManager` đã có
> nested class private tên `SaveData` — hợp lệ về C# nhưng trùng tên gây nhầm khi đọc code.
> JsonUtility-compatible: chỉ List + field public, không Dictionary, không property.

| Field | Kiểu | Nguồn (capture qua) | Ghi chú |
|---|---|---|---|
| `saveVersion` | int | `SaveSystem.CurrentSaveVersion` = 1 | tăng khi đổi schema, viết nhánh trong `SaveSystem.MigrateFrom` |
| `savedAtUtc` | string | `DateTime.UtcNow` ("o") | |
| `gold`, `gems` | int | `FarmEconomyManager.Instance.Gold/Gems` (fallback PlayerPrefs) | **snapshot để đọc/debug** — nguồn thật là 2 khoá PlayerPrefs, phục hồi qua `prefsMirror` |
| `level`, `exp` | int | `PlayerProgressManager.Instance.Level/CurrentExp` | snapshot, như trên |
| `warehouseSeeds` | List\<SaveItemStack\> | `WarehouseManager.Instance.Items` | snapshot |
| `inventoryItems` | List\<SaveItemStack\> | `FarmInventoryManager.Instance.GetOrderedItems()` | snapshot |
| `plots` | List\<SavePlotSnapshot\> | `FindObjectsByType<PlotController>` → public getters | snapshot (id, unlocked, planted, ready, cropId). Thời gian trồng không public → dữ liệu đầy đủ nằm ở mirror khoá `PLOT_*` |
| `pens` | List\<SavePenSnapshot\> | khoá `PenState_/PenFood_/PenStartTime_` theo penId từ `PenMiniPanelConfig` (Resources.FindObjectsOfTypeAll) | snapshot + đây cũng chính là dữ liệu phục hồi |
| `train` | SaveTrainSection | `TrainManager.Instance.State/SlotData` + (sau patch) `CaptureTripSnapshot()` qua reflection | **phần lưu THẬT duy nhất không có ở PlayerPrefs**. `snapshotJson` chỉ có sau patch (`restorable=true`) |
| `tutorialFlags` | List\<SaveKV\> | 7 khoá tutorial (chỉ ghi khoá đang tồn tại) | snapshot |
| `missionNote` | string | — | ghi rõ: mission TỰ lưu ở `MISSION_PROGRESS_V1` + `MISSION_CLAIMED_*`; save.json chỉ mirror thô |
| `prefsMirror` | List\<SavePrefEntry\> {key, type, i, s, f} | **bản sao THÔ mọi khoá PlayerPrefs của game** (bảng khoá tĩnh + khám phá động: plot qua instance, pen qua config, mission-claimed qua `MissionDatabase`, dock 0..9, `SAVE_VER_*` qua `AllFamilies`; union với mirror của lần lưu trước để không rớt khoá khi đang ở scene bếp) | **lớp phục hồi thật sự**: copy nguyên văn giá trị các manager tự ghi → phục hồi 100% trung thực, không cần patch hệ nào |

## 3. Luồng hoạt động

### 3.1. Ghi (capture) — `SaveSystem.Save()`
- **Khi state đổi (debounce 5 s):** `SaveBootstrap` nghe `FarmEconomyManager.OnCurrencyChanged`,
  `PlayerProgressManager.OnExpChanged/OnLevelChanged`, `WarehouseManager.OnWarehouseChanged`,
  `FarmInventoryManager.OnInventoryChanged`, `MissionProgressTracker.OnProgressChanged` (static),
  `FarmManager.OnPlotPlantedEvent/OnPlotHarvestedEvent` (static) → `MarkDirty()`. Mốc hẹn đặt tại
  lần đánh dấu ĐẦU TIÊN (không dời mỗi lần — tránh bẫy debounce vô hạn, học từ `LuuGopPrefs`).
- **Định kỳ 60 s:** quét các hệ không có event (ô đất, chuồng, tàu).
- **Mọi lối thoát:** `OnApplicationPause(true)` / `OnApplicationFocus(false)` / `OnApplicationQuit`.
- **Thoát Play Mode trong Editor:** hook `EditorApplication.playModeStateChanged → ExitingPlayMode`
  trong `SaveDebugTool` (object còn sống, capture được).
- Mỗi mục capture bọc try/catch riêng — 1 mục hỏng chỉ mất mục đó, không mất cả save. Log `[Save]`.

### 3.2. Ghi file ATOMIC
```
ghi save.json.tmp  →  File.Replace(tmp, save.json, save.json.bak)
(lần đầu chưa có save.json thì File.Move; File.Replace lỗi thì fallback Copy+Delete)
```
Đường dẫn: `Application.persistentDataPath/save.json` (+ `.bak`, `.tmp`).
Đọc: `save.json` hỏng/parse fail → tự thử `save.json.bak` (log warning `[Save]`).

### 3.3. Phục hồi (restore)
- **Tàu (tự động, mỗi phiên):** `SaveBootstrap` đợi `TrainManager.Instance` sẵn sàng
  (coroutine, đợi qua `InitAfterFrame` về `WaitingForLoad`) → gọi `RestoreTripSnapshot` qua
  reflection (chỉ khi patch đã duyệt). Snapshot tàu trong bộ nhớ được làm tươi ở mỗi lần capture
  → chuyển scene bếp ↔ farm cũng không mất chuyến tàu.
- **Các hệ PlayerPrefs (thủ công, mặc định TẮT):** `SaveSystem.RestoreMissingPrefs(data)` chỉ ghi
  khoá **chưa tồn tại** — không bao giờ đè giá trị đang có. Kích hoạt qua menu
  `Tools/Farm Game/Save/Load Now`. **Vì sao không tự động:** tool `FarmResetTool`
  ("CHƠI LẠI TỪ ĐẦU") xoá PlayerPrefs.DeleteAll — nếu bootstrap tự bơm lại từ save.json thì
  reset vĩnh viễn không reset được. Muốn bật tự phục hồi (ship bản build cho người chơi thật)
  thì đổi `SaveBootstrap.AutoRestoreMissingPrefs = true` và nhớ sửa flow reset xoá cả save.json.

### 3.4. Version & migrate
- `save.json` mang `saveVersion` (hiện = 1). `SaveSystem.Load()`:
  - version **cũ hơn** code → gọi `MigrateFrom(oldVersion, data)` (chuỗi if tăng dần, hiện rỗng).
  - version **mới hơn** code (hạ cấp bản game) → đọc tiếp phần hiểu được, **cấm ghi đè** file
    (`_khongDuocGhi` — cùng triết lý `WarehouseManager`).
- Từng hệ PlayerPrefs vẫn giữ version riêng của nó (blob/`SAVE_VER_*`) — không đụng.

## 4. Vòng đời object
- `SaveBootstrap` tự spawn bằng `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` +
  `DontDestroyOnLoad` — **không cần kéo thả gì vào scene**, sống qua cả scene bếp.
- Reset static bằng `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` để không chết khi
  bật "Enter Play Mode Options" (không reload domain) — học từ `LuuGopPrefs`.
- `SceneManager.sceneLoaded` → hook lại event của manager per-scene (Warehouse, FarmManager,
  Train là per-scene; Economy/Progress/Inventory là DontDestroyOnLoad).

## 5. Những gì CỐ Ý không làm (để không phá luật studio)
- Không sửa bất kỳ file nào đang có. Patch duy nhất đề xuất: `TrainManager.PATCH.md` (additive).
- Không đụng scene/prefab.
- Không chuyển hệ nào từ PlayerPrefs sang save.json (đó là refactor lớn, ngoài scope M0-2 và
  vi phạm "không viết lại logic").
- Không tự phục hồi PlayerPrefs từ save.json khi thiếu khoá (lý do ở §3.3).
