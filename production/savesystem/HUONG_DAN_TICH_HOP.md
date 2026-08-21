# HƯỚNG DẪN TÍCH HỢP — SaveSystem JSON local (M0-2)

## 0. Đọc trước 30 giây

Scan cho thấy backlog đã lỗi thời: **game ĐÃ lưu gần hết qua PlayerPrefs** (chi tiết:
`SAVE_DESIGN.md` §1). Gói này vì thế:

1. **KHÔNG sửa một file code nào đang có.** Chỉ THÊM 5 file mới.
2. Lưu hệ duy nhất đang mất dữ liệu: **TÀU** (cần duyệt `TrainManager.PATCH.md` — 1 patch,
   thuần additive).
3. `save.json` = bản chụp hợp nhất + backup nguyên văn mọi khoá PlayerPrefs (atomic,
   có `saveVersion`, log `[Save]`), kèm bộ menu debug trong Editor.

## 1. Copy file vào đâu

| File trong gói | Copy tới |
|---|---|
| `SaveData.cs` | `Assets/_Game/Farm/Scripts/Save/SaveData.cs` |
| `SaveSystem.cs` | `Assets/_Game/Farm/Scripts/Save/SaveSystem.cs` |
| `SaveAdapters.cs` | `Assets/_Game/Farm/Scripts/Save/SaveAdapters.cs` |
| `SaveBootstrap.cs` | `Assets/_Game/Farm/Scripts/Save/SaveBootstrap.cs` |
| `Editor/SaveDebugTool.cs` | `Assets/_Game/Farm/Scripts/Save/Editor/SaveDebugTool.cs` (**bắt buộc trong thư mục `Editor/`**) |

KHÔNG copy: `compile_check/` (harness kiểm chứng, chạy bằng mono ngoài Unity),
`SAVE_DESIGN.md`, `TrainManager.PATCH.md`, file .md này.

**Không cần kéo thả gì vào scene** — `SaveBootstrap` tự spawn bằng
`[RuntimeInitializeOnLoadMethod]` (DontDestroyOnLoad, sống qua cả scene bếp).

## 2. Duyệt patch tàu (tùy chọn nhưng nên làm — đây là hệ đang MẤT dữ liệu thật)

Mở `TrainManager.PATCH.md`, chèn khối code vào cuối class `TrainManager`
(sau method `GetRewardPreset`, trước dấu `}` đóng class — hiện là sau dòng 679).
Chưa duyệt thì mọi thứ khác vẫn chạy; tàu chỉ được "chụp để đọc" và console nhắc
đúng một lần bằng log `[Save]`.

## 3. Test 5 phút

1. **Play** → chờ console:
   `[Save] Chưa có save.json — sẽ tạo ở lần lưu đầu tiên.` (lần đầu) hoặc
   `[Save] Đã nạp save.json (v1, lưu lúc … UTC).`
2. Trồng 1 ô lúa, thu hoạch 1 ô chín, cho chuồng ăn, cộng vàng (bán gì đó).
   Sau ≤ 5 giây thấy: `[Save] Đã ghi save.json (v1, lý do: auto, … khoá mirror, …)`.
3. (Nếu đã duyệt patch tàu) nạp 1–2 toa tàu rồi **thoát Play** → console:
   `[Save] Auto-save khi thoát Play Mode (Editor hook).`
4. **Play lại** → kiểm: vàng/level giữ nguyên, kho còn hàng, ô đất đúng trạng thái,
   chuồng đúng tiến độ *(các hệ này do PlayerPrefs sẵn có — xác nhận không bị gói mới
   phá)*; tàu về ga với đúng số hàng đã nạp + log
   `[Save] Train: phục hồi chuyến #N ở pha nạp hàng.` *(cái này là giá trị MỚI của gói)*.
5. `Tools/Farm Game/Save/Show Save JSON (log)` → soi nội dung save;
   `Open Save Folder` → thấy `save.json` (+ `.bak` từ lần ghi thứ hai).

### Test phục hồi backup (kịch bản mất PlayerPrefs / đổi máy)
1. Đảm bảo đã có save.json (bước trên).
2. Chạy tool reset PlayerPrefs của dự án (FarmResetTool) — mô phỏng mất registry.
3. `Tools/Farm Game/Save/Load Now` → xác nhận → log `[Save] Đã phục hồi N khoá…`.
4. Play → tiến trình quay lại đúng như trước khi reset.
   (Đây cũng là lý do gói **không tự** phục hồi lúc boot: để tool reset còn reset được.
   Muốn auto cho bản build người chơi: `SaveBootstrap.AutoRestoreMissingPrefs = true`
   và sửa flow reset gọi thêm `SaveSystem.DeleteSave()`.)

## 4. Menu Editor

`Tools/Farm Game/Save/`
- **Open Save Folder** — mở thư mục `persistentDataPath`.
- **Show Save JSON (log)** — in save.json ra console (cắt 12k ký tự đầu nếu quá dài).
- **Save Now** — ghi ngay (ngoài Play Mode chỉ capture được từ PlayerPrefs — đủ cho mirror).
- **Load Now** — (có confirm) phục hồi khoá PlayerPrefs bị thiếu + áp snapshot tàu nếu đang Play.
- **Delete Save (save.json)** — (có confirm) xoá save.json/.bak/.tmp; KHÔNG đụng PlayerPrefs.
- Hook tự động: thoát Play Mode → auto-save (`ExitingPlayMode`, object còn sống).

## 5. Phạm vi phủ — trung thực

| Nhóm | Trạng thái |
|---|---|
| Vàng/gem, level/exp, kho hạt, kho nông sản, ô đất, nhà đã đặt, công trường, chuồng, quầy hàng, bảng đơn, chợ (timer), bếp-transfer, tutorial, mission (tiến độ + đã nhận), thuyền du lịch, hồ sơ | ✅ Tự lưu PlayerPrefs từ trước (không đụng); gói này thêm backup nguyên văn vào save.json + phục hồi khi mất khoá |
| **Tàu (TrainManager)** | ✅ Lưu/phục hồi qua save.json **SAU khi duyệt `TrainManager.PATCH.md`** (1 patch, additive). Chưa duyệt: chưa cover — trạng thái y như hiện tại (mất khi thoát) |
| Hàng NPC ở chợ trong 1 chu kỳ (cờ "đã bán" từng thẻ) | ⚠️ CHƯA cover — hàng tái sinh đúng theo seed đã lưu, nhưng thẻ đã mua có thể mua lại sau restart trong cùng chu kỳ. Không mất dữ liệu; nếu muốn vá là patch riêng của hệ chợ (ngoài scope M0-2) |
| Phiên nấu ăn dở, minigame, con vật đi lại, FX | ⬜ Transient — cố ý không lưu (SAVE_DESIGN.md §1.3) |
| Chuyển scene farm ↔ bếp giữa phiên | ✅ Snapshot tàu trong RAM được áp lại khi TrainManager per-scene dựng lại (sau patch) — trước gói này tàu cũng reset khi đổi scene |

## 6. Kết quả compile-sanity (bắt buộc của Bước 3 — ĐÃ CHẠY THẬT)

Harness: `compile_check/run.sh` — mcs (Mono 6.8) + stub UnityEngine tối thiểu +
**21 file manager THẬT** copy nguyên văn từ Assets (`compile_check/real/`):
FarmEconomyManager, PlayerProgressManager, FarmLevelManager, FarmInventoryManager,
WarehouseManager, FarmManager, SaveVersionGuard, LuuGopPrefs, MissionProgressTracker,
MissionData, MissionDatabase, PenMiniPanelConfig, PlotController, CropData, BaseItemData,
TrainManager, TrainState, TrainCargoData, TrainRewardData, TrainWagonSlot, TrainInventoryAdapter.

| Pass | Nội dung | Kết quả |
|---|---|---|
| A | 4 file save mới + 21 file thật (TrainManager GỐC, chưa patch) | ✅ 0 lỗi |
| B | Như A nhưng TrainManager ĐÃ chèn đúng khối trong `TrainManager.PATCH.md` (trích tự động từ file .md để không lệch) | ✅ 0 lỗi |
| C | Thêm `Editor/SaveDebugTool.cs` + define `UNITY_EDITOR` + stub UnityEditor | ✅ 0 lỗi |
| D | Smoke test chạy thật (mono): capture 13 khoá gieo sẵn → DeleteAll → RestoreMissingPrefs đủ 13, không đè khoá đang có; Save() 2 lần → atomic, có .bak, không sót .tmp; DeleteSave dọn sạch | ✅ 18/18 PASS |
| E | Hợp đồng reflection với TrainManager đã patch: tìm đúng 2 method, đúng chữ ký, `JsonUtility.FromJson(string, Type)` khởi tạo được `TrainTripSnapshot` | ✅ 5/5 PASS |

Giới hạn của kiểm chứng (nói thẳng):
- Stub hoá (không compile thật) các class thuần UI/VFX ngoài phạm vi save:
  PlacementManager/ConstructionManager (chỉ đối chiếu 2 const khoá save, đã grep đúng giá trị
  trong source thật), FarmUIManager, popup/FX… — chữ ký stub đối chiếu từng call-site bằng grep.
- `JsonUtility` trong smoke test là stub (serialize thật là của Unity — schema đã theo đúng
  luật JsonUtility: class [Serializable], field public, List, không Dictionary/Sprite).
- Chưa chạy trong Unity Editor thật — bước test 5 phút ở §3 là để chốt điều đó.
