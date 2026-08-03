# HỢP ĐỒNG CHUNG — Vòng 1 (Phase 0) — 2 thợ đọc TRƯỚC khi làm

> Mục đích: để 2 thợ làm SONG SONG mà không đụng file/không phá code của nhau.
> Dán khối này vào đầu mỗi phiên của cả 2 thợ.

## 1. Bối cảnh kỹ thuật
- Engine: **Unity 6.3 (6000.3.10f1)**, project tại thư mục gốc `Cooking-Game-2D/`.
- Mọi script mới đặt namespace **`Game.Core`**. Không tạo asmdef (tất cả biên dịch vào Assembly-CSharp).
- Scene chính: `Assets/_Game/Scenes/SCN_Home.unity` (menu), `SCN_Farm.unity` (gameplay), `SampleScene.unity` (bếp, load additive).
- Quy trình studio bắt buộc: đọc & tuân theo `Claude-Code-Game-Studios/.claude/docs/coding-standards.md`, `.../coordination-rules.md`, và `production/AUTONOMY.md`.
- Vòng lặp làm việc: **SCAN → IMPLEMENT → CHECK (Console 0 lỗi đỏ) → REPORT**.
- Kết thúc mỗi task: **build/compile sạch**, và ghi mục **"ANH CẦN LÀM TRONG UNITY"** + **"CẦN BẠN"** (nếu chạm STOP LIST của AUTONOMY).

## 1b. LUẬT SCAN — KHÔNG ĐƯỢC LÀM LẠI CÁI ĐÃ CÓ ⚠️ (đọc kỹ)

Game này **đã có RẤT NHIỀU hệ được dựng sườn** (đặc biệt là thưởng/sự kiện), nhưng phần lớn **UI/asset chưa hoàn thiện hoặc chưa wire**. Vì vậy **TRƯỚC KHI tạo bất kỳ script/hệ mới nào**, bắt buộc:

1. `grep` tên hệ đó trong `Assets/_Game` + `Assets/Scripts`, và đọc các file liên quan.
2. Đọc doc: `production/REWARDS_MASTER_LIST.md`, `production/MISSIONS_MASTER_LIST.md`, `production/CURSOR_PROMPT_WIRE_REWARDS_MISSIONS.md`, `production/ROADMAP_GAME_COMPLETE.md`.
3. Phân loại đúng tình trạng rồi mới làm:
   - **Đã có logic nhưng UI/asset chưa xong** → nhiệm vụ là **HOÀN THIỆN + WIRE UI/asset**, KHÔNG viết lại logic.
   - **Đã có nhưng thiếu data** → đổ data qua tool/batch có verify.
   - **Chưa có gì tương đương** → mới được viết mới.
4. Trong báo cáo phải có mục **"KIỂM KÊ TRƯỚC KHI LÀM"**: liệt kê cái đã tồn tại liên quan task + trạng thái + quyết định (hoàn thiện / wire / viết mới).

**Kho hệ ĐÃ TỒN TẠI (kiểm tra trước, đừng dựng trùng):**
- Điểm danh: `AttendanceManager.cs` (khung popup — UI/asset chưa xong)
- Sự kiện phúc lợi: `WelfareEventManager.cs` (khung popup — UI/asset chưa xong)
- Thưởng lên cấp L2–L30: `LevelUpRewardDataSetupTool`, `LevelRewardConfig`, `LevelUpPopupUI`, `LevelUpGiftSlotUI`
- Mission + Daily + Achievement/Ewar: `MissionDatabase`, `MissionProgressTracker`, `UnifiedTaskPopupUI`, `PopupEwarManager`, `MissionSetupTool`
- Thưởng tàu: `TrainRewardData`, `TrainRewardSlot`
- Popup dùng chung: `PopupManager.cs` (tái dùng cho popup mới, đừng tự dựng canvas rời nếu đã hợp)
- Home menu: `HomeMenuManager.cs`, `HomeMenuController.cs`, `HomeSceneUI.cs`

> Nguyên tắc: **"Hoàn thiện & nối dây" ưu tiên hơn "viết mới".** Chạm tay vào là để làm cho cái đã có CHẠY ĐƯỢC, không phải thay thế nó.

## 1c. QUY TẮC TOOL-FIRST ⚠️ (bắt buộc — anh chỉ bấm 1 nút)

**Mọi thao tác setup trong Unity PHẢI đóng gói thành Editor Tool tự động.** Anh KHÔNG làm tay từng bước — chỉ bấm menu `Tools → …` là nó dựng/gán/đổ data ra hết.

Áp dụng cho: tạo GameObject, dựng UI (canvas/panel/nút/slider), gán tham chiếu (button → controller, slider → manager), wire event, đổ data/asset, đổi Player Settings, đăng ký vào shop/kho…

Yêu cầu chất lượng của mỗi tool:
1. **Idempotent** — chạy lại KHÔNG nhân đôi/không hỏng. Tìm object/asset cũ theo tên rồi cập nhật, thay vì tạo trùng.
2. **Log rõ** — in ra đã tạo gì, gán gì, ở đâu.
3. **Tự ping + Select** object vừa tạo trong Hierarchy/Project (giống các SetupTool có sẵn).
4. **An toàn** — không xoá scene/prefab/asset khi chưa được phép (theo AUTONOMY STOP LIST).
5. Đặt trong `Assets/_Game/Editor/` hoặc `Assets/_Game/Farm/Editor/`, dùng `[MenuItem("Tools/…")]`.

**Học pattern từ tool đã có (đọc trước khi viết):** `DemoL1L10Tool.cs`, `MissionSetupTool.cs`, `ProductionMachineSetupTool.cs`, `LevelUpPopupSetupTool.cs`, `SetupUnifiedTaskPopupTool.cs`.

→ Hệ quả cho báo cáo: mục **"ANH CẦN LÀM TRONG UNITY"** giờ chỉ được phép là dạng **"bấm menu Tools → X"** (một hai nút bấm), KHÔNG phải danh sách thao tác kéo-thả dài.

## 2. Phân chia quyền sở hữu (chống xung đột)
- **Thợ A** = Systems & Data. Chỉ sửa: script logic/manager/data (`Assets/_Game/Scripts/Core/` cho file MỚI của A, `Assets/_Game/Farm/Scripts/**` logic, ProjectSettings). **KHÔNG sửa file `.unity` nào trong vòng này.**
- **Thợ B** = UI/UX & Build. Sở hữu **`SCN_Home.unity`** vòng này. Sửa: script UI, prefab UI, `AudioManager.cs`, Editor build tool.
- Nếu cần đụng file của thợ kia → ghi vào "CẦN BẠN", KHÔNG tự sửa.

## 3. Giao diện chung giữa 2 thợ (API contract)
**SaveSystem (Thợ A xây — Thợ B chỉ GỌI, không sửa):**
```
GameSaveManager.Instance.HasSave()  // bool
GameSaveManager.Instance.Save()
GameSaveManager.Instance.NewGame()
// Tự động nạp save khi vào SCN_Farm (Thợ B không cần gọi Load)
```

**Khoá Settings trong PlayerPrefs (Thợ B xây — prefix "SET_"):**
```
SET_MASTER_VOL, SET_MUSIC_VOL, SET_SFX_VOL   (float 0..1)
SET_FULLSCREEN                                (int 0/1)
SET_QUALITY                                   (int)
SET_RES_W, SET_RES_H                          (int)
```
→ **Thợ A: hàm NewGame() PHẢI bảo toàn mọi khoá bắt đầu bằng "SET_"** (đọc & ghi lại sau khi xoá).

**Manager tiền/level đã có sẵn (cả 2 dùng, KHÔNG sửa chữ ký):**
```
FarmEconomyManager.Instance  → Gold, Gems, SetCurrency(int,int), ResetCurrency()
PlayerProgressManager.Instance → Level, CurrentExp, ForceSetLevelExp(int,int)
```

## 4. Định nghĩa "xong" chung
- Console 0 lỗi đỏ. Compile sạch.
- Có log rõ ràng cho mỗi hệ (`[Save]`, `[Analytics]`, `[Menu]`…).
- Báo cáo cuối: việc đã làm · file thay đổi · "ANH CẦN LÀM TRONG UNITY" · "CẦN BẠN".
