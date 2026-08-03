# PROMPT — THỢ B (VÒNG LOOP): Thưởng · Sự kiện · Điểm danh · Giữ chân người chơi

> Đọc `_SHARED_CONTRACT.md` trước. Tuân thủ SCAN-first + TOOL-first.

---

Bạn là **ui-programmer kiêm systems-designer & live-ops-designer** (theo `Claude-Code-Game-Studios/.claude/agents/`). Mục tiêu vòng này: **hoàn thiện & nối UI** cho hệ thưởng/sự kiện đã dựng sườn, thêm các **hook giữ chân** kiểu Hay Day/Township, đổ data từ master list. Làm theo `production/AUTONOMY.md`.

**Phạm vi sở hữu:** UI thưởng/sự kiện/HUD giữ chân. Giao **TOOL + code + data** (KHÔNG commit sửa tay `.unity`). Không đụng tutorial/economy của Thợ A.

## ⚠️ QUAN TRỌNG — KHÔNG LÀM LẠI CÁI ĐÃ CÓ
Game đã có **rất nhiều** hệ thưởng dựng sườn nhưng **UI/asset/data chưa xong**. Nhiệm vụ chủ yếu là **HOÀN THIỆN + NỐI + ĐỔ DATA**, KHÔNG viết lại logic.

## BƯỚC 0 — SCAN (bắt buộc, ghi "KIỂM KÊ TRƯỚC KHI LÀM")
Đọc kỹ:
- Data thưởng/nhiệm vụ đầy đủ: `production/REWARDS_MASTER_LIST.md` (có mục §4 Đăng nhập 7 ngày), `production/MISSIONS_MASTER_LIST.md` (A. Main, B. Daily, C. Achievement), `production/CURSOR_PROMPT_WIRE_REWARDS_MISSIONS.md`.
- Hệ ĐÃ CÓ (xác định trạng thái từng cái): `AttendanceManager.cs` (điểm danh — khung popup), `WelfareEventManager.cs` (sự kiện — khung popup), `LevelUpPopupUI` + `LevelRewardConfig` + `LevelUpGiftSlotUI`, `MissionDatabase` + `MissionProgressTracker` + `UnifiedTaskPopupUI` + `PopupEwarManager`.
- Tool ĐÃ CÓ (chạy/mở rộng, đừng viết trùng): `LevelUpRewardDataSetupTool`, `LevelUpPopupSetupTool`, `MissionSetupTool`, `SetupUnifiedTaskPopupTool`, `MissionHudButtonSetupTool`.
- Popup dùng chung: `PopupManager.cs` (tái dùng).
→ Liệt kê: cái nào chạy được, cái nào khung rỗng, cái nào thiếu data/UI/art.

## NHIỆM VỤ (ưu tiên HOÀN THIỆN cái đã có)

### B1 — Điểm danh 7 ngày (daily login streak)  ⚠️ TOOL-FIRST
- Hoàn thiện `AttendanceManager` (khung đã có): logic chuỗi ngày (seed theo `yyyyMMdd`, sang ngày mới +1, bỏ ngày mất chuỗi), cộng thưởng thật theo `REWARDS_MASTER_LIST §4`.
- Viết `Tools → Setup → Daily Login 7 Days`: dựng popup 7 ô quà + nút Nhận, gán refs, đổ data 7 ngày, **slot art cho icon quà**. Idempotent.
- **Acceptance:** mở game mỗi ngày nhận quà, chuỗi tăng, cộng vàng/gem thật, lưu qua phiên.

### B2 — Sự kiện (WelfareEventManager)  ⚠️ TOOL-FIRST
- Hoàn thiện `WelfareEventManager`: 1–2 sự kiện đơn giản, có thời hạn (vd "Tuần thu hoạch x2", "Quà chào mừng"). Popup + đếm ngược + nhận thưởng.
- `Tools → Setup → Welfare Event`: dựng popup + đổ data + slot art banner. Idempotent.
- **Acceptance:** sự kiện hiện, còn hạn thì nhận được, hết hạn thì ẩn; thưởng cộng thật.

### B3 — Nhiệm vụ ngày (Daily) + reset theo ngày  ⚠️ TOOL-FIRST
- Hoàn thiện tab Daily trong `UnifiedTaskPopupUI` (backlog M1-6): 3 daily/ngày từ pool `MISSIONS_MASTER_LIST §B`, reset sang ngày mới, claim persist.
- Mở rộng `MissionSetupTool` (hoặc tool mới) đổ data daily. Idempotent.
- **Acceptance:** mỗi ngày 3 daily, hoàn thành nhận thưởng, sang ngày mới reset.

### B4 — Popup lên cấp L2→L10 (pháo hoa)  ⚠️ TOOL-FIRST
- Chạy/kiểm `LevelUpRewardDataSetupTool` + `LevelUpPopupSetupTool` để popup lên cấp L2–L10 hiện đủ quà + "mở khoá" (theo `REWARDS_MASTER_LIST §1`) + hiệu ứng pháo hoa/confetti. Nối `LevelUpGiftSlotUI`. Slot art cho icon quà/tính năng.
- **Acceptance:** lên mỗi cấp L2–L10 bung popup, nhận quà 1 lần, có juice.

### B5 — Hook giữ chân P0 (retention)  ⚠️ TOOL-FIRST
- **Badge "✓ đã chín" + đếm ngược** rõ trên cây/chuồng (nhìn từ camera xa vẫn thấy). 
- **Teaser "Cấp tới mở X"** + thanh EXP trên HUD (luôn hiện mục tiêu kế; ẩn khi max).
- Viết tool dựng 2 thứ này + slot art (badge, icon).
- **Acceptance:** cây chín có badge + đếm ngược; HUD luôn cho thấy mục tiêu cấp kế.

## VERIFY
- Play Mode 0 lỗi đỏ. Test nhận thưởng của cả 5 hệ, lưu qua phiên.
- `/code-review` file mới.

## DANH SÁCH ART CẦN ANH VẼ (tool để slot sẵn; thả PNG vào rồi chạy lại tool)
- Icon quà điểm danh 7 ngày (7 ô), banner sự kiện, icon badge "đã chín", icon quà/tính năng cho popup lên cấp, icon HUD teaser.
> Gợi ý: tool tự gán sprite theo tên file trong thư mục quy ước (vd `Assets/_Game/Art/Rewards/day1.png`, `event_harvest.png`).

## BÁO CÁO CUỐI
```
## Thợ B — VÒNG LOOP report
- KIỂM KÊ TRƯỚC KHI LÀM: (mỗi hệ thưởng đã có → trạng thái → hoàn thiện/nối/đổ data)
- Đã làm: B1 …, B2 …, B3 …, B4 …, B5 …
- File mới/sửa (tool + data + UI): …
- ANH CẦN LÀM TRONG UNITY: (bấm Tools → Setup → Daily Login 7 Days, …)
- DANH SÁCH ART CẦN ANH VẼ: …
- CẦN BẠN: …
```
