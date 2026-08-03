# PROMPT — THỢ A (Vòng 1 / Phase 0): Save trung tâm + Analytics (số liệu người chơi)

> Dán `_SHARED_CONTRACT.md` vào trước, rồi dán prompt này.

---

Bạn là **gameplay-programmer kiêm analytics-engineer** của studio (theo `Claude-Code-Game-Studios/.claude/agents/gameplay-programmer.md` và `analytics-engineer.md`). Làm việc theo `production/AUTONOMY.md`, vòng lặp **SCAN → IMPLEMENT → CHECK → REPORT**. Chỉ làm việc CỘNG THÊM an toàn; đụng STOP LIST thì dừng và ghi "CẦN BẠN".

**Phạm vi sở hữu:** chỉ script logic/data + ProjectSettings. **KHÔNG sửa file `.unity`.** Không đụng file UI của Thợ B.

## BƯỚC 0 — SCAN (bắt buộc trước khi code) ⚠️ theo LUẬT SCAN ở hợp đồng
> Game đã có nhiều hệ dựng sườn — **kiểm tra tồn tại trước, đừng dựng trùng.** Với vòng này (Save/Analytics/danh tính) mình đã xác nhận **CHƯA có** SaveManager tập trung, chưa có analytics → được viết mới. Nhưng vẫn phải xác nhận lại bằng grep và ghi mục "KIỂM KÊ TRƯỚC KHI LÀM" trong report.
1. `grep -ri "SaveData\|SaveManager\|persistentDataPath\|JsonUtility" Assets/_Game Assets/Scripts` — xác nhận chưa có save tập trung; ghi lại các chỗ đang tự lưu JSON riêng lẻ (`PlacementManager`, `FarmInventoryManager`, `KitchenTransferManager`…) để sau này gom vào ISaveable, KHÔNG viết đè.
2. `grep -ri "analytics\|GameAnalytics\|telemetry" Assets` — xác nhận chưa có analytics.
3. Đọc `production/SHIP_STEAM_DEMO_PLAN.md` (mục 2, 5).
4. Đọc để nắm API thật (KHÔNG sửa chữ ký): `FarmEconomyManager.cs`, `PlayerProgressManager.cs`, `FarmLevelManager.cs`.
5. Liệt kê mọi khoá PlayerPrefs gameplay đang dùng (grep `PlayerPrefs`). Đã biết trước: `FARM_ECONOMY_GOLD/GEMS`, `PLAYER_LEVEL/EXP`, `FARM_PLACED_BUILDINGS`, `FARM_INVENTORY_SAVE`, `KITCHEN_TRANSFER_SAVE`, `WAREHOUSE_LEVEL`, `PLAYER_PROFILE_*`, `MISSION_PROGRESS_V1`, `UNIFIED_TASK_DAILY_*`, `GUIDE_*`, `ANIMAL_GUIDE_*`, `STARTER_ITEMS_GIVEN`, `Codex.Tutorial*`, tiền tố `Mission_`, `PenStartTime_`.
6. Với A3 (gắn event): các điểm hook thưởng/mission ĐÃ CÓ (`MissionProgressTracker.ReportEvent`, `AttendanceManager`, `WelfareEventManager`, `PopupEwarManager`) — chỉ **thêm 1 dòng gọi analytics cạnh hook có sẵn**, KHÔNG sửa logic thưởng.

## NHIỆM VỤ (task nhỏ, làm lần lượt)

### A1 — SaveSystem trung tâm (file JSON)
- Tạo `Assets/_Game/Scripts/Core/GameSaveManager.cs` (namespace `Game.Core`), tự sinh bằng `[RuntimeInitializeOnLoadMethod]` (không cần đặt vào scene), `DontDestroyOnLoad`.
- Ghi/đọc `Application.persistentDataPath/save_slot0.json` bằng `JsonUtility`, có field `saveVersion` để migrate.
- Gom **gold, gems, level, exp** từ 2 manager có sẵn (đọc `Gold/Gems/Level/CurrentExp`; khôi phục bằng `SetCurrency` + `ForceSetLevelExp`).
- API công khai (đúng hợp đồng): `HasSave()`, `Save()`, `NewGame()`. Tự động gọi Load khi `SceneManager.sceneLoaded` là `SCN_Farm`.
- Auto-save: mỗi 30s (nếu dirty) + `OnApplicationPause(true)` + `OnApplicationQuit()`.
- **`NewGame()`**: xoá `save_slot0.json`; xoá state gameplay ở PlayerPrefs nhưng **BẢO TOÀN mọi khoá tiền tố `SET_`** (đọc & ghi lại); reset singleton còn sống (`ResetCurrency`, `ForceSetLevelExp(1,0)`).
- Tạo `Assets/_Game/Scripts/Core/ISaveable.cs`: interface `{ string SaveKey; string CaptureState(); void RestoreState(string); }` + cơ chế `Register/Unregister` trong GameSaveManager để hệ con (kho/ô đất/mission) cắm vào **sau này** (vòng này chỉ dựng khung, chưa cần cắm hết).
- **Acceptance:** chơi kiếm vàng/lên cấp → thoát → mở lại → `HasSave()` true, giá trị giữ nguyên; `save_slot0.json` tồn tại; New Game về đúng 400 vàng/15 gem/L1 mà **âm lượng/độ phân giải vẫn giữ**.

### A2 — Cài GameAnalytics SDK + AnalyticsManager  (⚠️ TOOL-FIRST)
- Cài **GameAnalytics Unity SDK** (miễn phí) qua Package Manager (Add package from git URL: `https://github.com/GameAnalytics/GA-SDK-UNITY.git#unity-package`, hoặc tarball `.tgz` — theo docs chính thức). (Bước cài package qua Package Manager là thao tác Unity không tránh được → ghi rõ 1 dòng vào "ANH CẦN LÀM TRONG UNITY".)
- Tạo `Assets/_Game/Scripts/Core/AnalyticsManager.cs` (namespace `Game.Core`) **bọc trong `#if GAMEANALYTICS`** (thêm define `GAMEANALYTICS` để compile được cả khi chưa cài). Wrapper tĩnh an toàn: `Init()`, `Design(eventId, float? value=null)`, `Progression(status, level)`, `Resource(source/sink, currency, amount, itemType, itemId)`. Tự sinh + `GameAnalytics.Initialize()` một lần đầu game.
- **TOOL bắt buộc:** `Assets/_Game/Editor/AnalyticsSetupTool.cs` → menu `Tools → Setup → GameAnalytics Object`: tự tạo GameObject `[GameAnalytics]` + gắn component GameAnalytics + AnalyticsManager vào scene khởi động, ping object. (Chỉ còn việc anh dán Game Key/Secret Key vào Inspector — bí mật nên không tự điền được; tool mở sẵn Inspector + log nhắc.)
- **Acceptance:** bấm tool → có object sẵn; game chạy in `[Analytics] init OK`; chưa có key → log cảnh báo, KHÔNG crash.

### A3 — Gắn funnel số liệu cơ bản (đo "gây nghiện")
Bắn event tại các điểm gameplay ĐÃ CÓ (chỉ thêm 1 dòng gọi wrapper, không đổi logic):
- `Progression(Start/Complete, "L{n}")` khi lên cấp (`PlayerProgressManager.OnLevelChanged`).
- `Design("tutorial:step_{i}")` tại mỗi bước tutorial (TutorialManager).
- `Design("plant"), ("harvest"), ("deliver_order"), ("cook")` tại các hook có sẵn.
- `Resource(Sink/Source, "gold", amount, ...)` khi tiêu/nhận vàng (trong `FarmEconomyManager.SpendGold/AddGold`).
- **Acceptance:** chơi 2 phút → Console in ≥6 loại event; (nếu có key thật) dashboard GameAnalytics nhận được sự kiện.

### A4 — Sửa danh tính project (Player Settings)  (⚠️ TOOL-FIRST)
- **TOOL bắt buộc:** `Assets/_Game/Editor/ProjectIdentitySetupTool.cs` → menu `Tools → Setup → Apply Project Identity`: dùng `PlayerSettings` API set `companyName`, `productName`, `bundleVersion = 0.1.0`, `fullscreenMode = FullScreenWindow`, mặc định 1920×1080. Tên game/studio để hằng số đầu file dễ sửa; nếu chưa có tên chính thức → ghi "CẦN BẠN" để anh chốt.
- **Acceptance:** bấm tool → Player Settings không còn `DefaultCompany`/`My project`.

## VERIFY (bắt buộc)
- Chạy Play Mode, xem Console 0 lỗi đỏ.
- Kiểm tra file save thật sự sinh ra (in đường dẫn `persistentDataPath`).
- Chạy `/code-review` (skill studio) trên các file mới.

## BÁO CÁO CUỐI (định dạng cố định)
```
## Thợ A — Vòng 1 report
- KIỂM KÊ TRƯỚC KHI LÀM: (hệ/khoá/JSON đã tồn tại liên quan + quyết định: gom vào ISaveable / viết mới)
- Đã làm: A1 …, A2 …, A3 …, A4 …
- File mới/sửa: …
- Log kiểm chứng: [Save] …, [Analytics] …
- ANH CẦN LÀM TRONG UNITY: (chỉ dạng "cài GA package qua Package Manager", "bấm Tools → Setup → GameAnalytics Object rồi dán key", "bấm Tools → Setup → Apply Project Identity")
- CẦN BẠN: (nếu có)
```
