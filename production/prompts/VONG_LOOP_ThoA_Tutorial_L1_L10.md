# PROMPT — THỢ A (VÒNG LOOP): Vòng chơi liền mạch L1→L10 + Tutorial dắt tay L3→L10

> Đọc `_SHARED_CONTRACT.md` trước. Tuân thủ SCAN-first + TOOL-first.

---

Bạn là **gameplay-programmer kiêm game-designer & economy-designer** (theo `Claude-Code-Game-Studios/.claude/agents/`). Mục tiêu vòng này: người chơi **chơi liền mạch từ Cấp 1 đến Cấp 10 KHÔNG kẹt tiền / KHÔNG kẹt đơn**, và có **tutorial dắt tay** ở các cấp mở tính năng mới (L3–L10). Làm theo `production/AUTONOMY.md`, vòng lặp SCAN → IMPLEMENT → CHECK → REPORT.

**Phạm vi sở hữu:** logic tutorial/gameplay + data kinh tế/đơn hàng. Giao **TOOL + code + data** (KHÔNG commit sửa tay `.unity`). Không đụng hệ thưởng/popup của Thợ B.

## BƯỚC 0 — SCAN (bắt buộc, ghi "KIỂM KÊ TRƯỚC KHI LÀM")
Đọc kỹ (đã có sẵn — ĐỪNG làm lại từ đầu):
- Thiết kế: `production/session-state/L1_L10_DESIGN_PLAN.md`, `L1_L10_ECONOMY_TABLE.md`, `L1_L10_IMPLEMENTATION_REPORT.md`, `L1_L10_SCAN_REPORT.md`, và `production/RESEARCH_TUTORIAL_HAYDAY_TOWNSHIP.md`.
- Hệ tutorial data-driven: `Assets/_Game/Farm/Scripts/Tutorial/TutorialManager.cs` (`List<TutorialStepData> _steps`), `TutorialStepData.cs`, `TutorialGuideBoardUI`, `TutorialActionHandGuide`, `TutorialCameraFocus/Zoom`, `AnimalGuideController`.
- Tool tutorial ĐÃ CÓ (học pattern + mở rộng, đừng viết trùng): `SetupTutorialL1L2Tool`, `SetupTutorialL2Tool`, `TutorialStepsL1GeneratorTool`, `TutorialFourPopupSetupTool`, `TutorialHandFlowRebuildTool`, `CheckTutorialL1L2SetupTool`.
- Đơn hàng/kinh tế: `VillageOrdersL1L6SetupTool`, `VillageOrderManager`, `FarmEconomyManager`, `MissionSetupTool`.
→ Liệt kê: cấp nào ĐÃ có tutorial (L1–L2), cấp nào CHƯA (L3–L10); đơn hàng đã tới cấp mấy (L6?) → cần mở tới L10.

## NHIỆM VỤ

### A1 — Tutorial L3→L10 (dắt tay, kiểu Hay Day)  ⚠️ TOOL-FIRST
- Dựa **template L1–L2 đã có**, viết `Assets/_Game/Farm/Editor/SetupTutorialL3L10Tool.cs` → `Tools → Setup → Tutorial L3-L10`: sinh các `TutorialStepData` asset cho từng mốc mở tính năng, bám `L1_L10_DESIGN_PLAN.md`:
  - L3: mua hạt mới / mở ô đất
  - L4: chăn nuôi (mua chuồng → đặt → cho ăn → thu sản phẩm) — nối `AnimalGuideController`
  - L5: mở Bếp, nấu món đầu tiên
  - L6: mở nhà dân / đơn combo
  - L7–L10: hint mềm cho tính năng theo design plan (máy chế biến, tàu…)
- Text tiếng Việt có dấu, ngắn; ảnh minh hoạ/portrait để **slot trống** (art anh vẽ sau — xem DANH SÁCH ART). Có failsafe: thiếu ảnh vẫn chạy.
- Tool idempotent, tự gán step vào `TutorialManager._steps`, log rõ.
- **Acceptance:** bấm tool → chơi thử L3→L10 thấy tutorial hiện đúng lúc, không kẹt bước, không spam.

### A2 — Vòng đơn hàng tới L10  ⚠️ TOOL-FIRST
- Mở rộng `VillageOrdersL1L6SetupTool` (hoặc tool mới `SetupVillageOrdersL1L10`) để đơn nhà dân + item đơn phủ tới L10 theo economy table. Đảm bảo mọi nông sản/sản phẩm L1–L10 đều có đường tiêu thụ (bán chợ hoặc lên đơn).
- **Acceptance:** ở mỗi cấp L1–L10 luôn có đơn/nguồn thu để kiếm tiền tiến cấp.

### A3 — Cân bằng kinh tế L1–L10 (không kẹt tiền)  (economy-designer)
- Đọc `L1_L10_ECONOMY_TABLE.md`. Kiểm tra/điều chỉnh giá mua-bán, thời gian, thưởng trong **data asset** sao cho mô phỏng 3 kiểu người chơi (chăm / vừa / lười) đều **không kẹt tiền, không kẹt chờ**.
- Nếu có tool `Simulate Economy`/balance-check thì chạy; nếu chưa, viết tool nhỏ in bảng lời/giờ theo cấp.
- **Acceptance:** báo cáo bảng mô phỏng L1→L10, chỉ ra nơi từng kẹt và đã sửa.

### A4 — +EXP khi nấu (nếu đang 0) & vá điểm kẹt
- Nếu nấu món chưa cộng EXP → thêm (backlog M2-5), giữ đúng đường cong.
- Chơi thử toàn tuyến L1→L10, ghi & vá mọi chỗ kẹt logic (đơn không giao được, tính năng không mở, tutorial rớt).
- **Acceptance:** chơi liền mạch L1→L10 một lượt không kẹt.

## VERIFY
- Play Mode 0 lỗi đỏ. Chạy `CheckTutorialL1L2SetupTool` + kiểm L3–L10.
- Chạy `/playtest-report` (skill studio) cho tuyến L1→L10.
- `/code-review` file mới.

## DANH SÁCH ART CẦN ANH VẼ (tool để slot sẵn, anh thả PNG vào rồi chạy lại tool)
- Ảnh minh hoạ guide board L3–L10 (mỗi bước 1 ảnh: mua chuồng, cho ăn, mở bếp, nấu, mở nhà dân…), PNG nền trong ~512px.
- Portrait mascot dẫn tutorial (nếu chưa có).
> Gợi ý: tool tự gán sprite theo tên file trong thư mục quy ước (vd `Assets/_Game/Art/Tutorial/guide_L4_feed.png`).

## BÁO CÁO CUỐI
```
## Thợ A — VÒNG LOOP report
- KIỂM KÊ TRƯỚC KHI LÀM: (tutorial/đơn/economy đã có tới đâu → quyết định)
- Đã làm: A1 …, A2 …, A3 …, A4 …
- File mới/sửa (tool + data + logic): …
- Bảng mô phỏng kinh tế L1→L10: …
- ANH CẦN LÀM TRONG UNITY: (bấm Tools → Setup → Tutorial L3-L10, …)
- DANH SÁCH ART CẦN ANH VẼ: …
- CẦN BẠN: …
```
