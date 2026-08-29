# Project Memory Index

This directory holds persistent project memory files that survive session compaction.

## ⭐ QUY TRÌNH MỖI PHIÊN MỚI (user gõ "tiếp tục roadmap")

1. Đọc `production/ROADMAP_GAME_COMPLETE.md` → xác định sprint kế tiếp + trạng thái.
2. Đọc báo cáo mới nhất trong `production/session-state/` (IMPLEMENTATION_REPORT, MISSIONS_PROPOSAL…).
3. Làm sprint theo workflow: SCAN file liên quan → IMPLEMENT (file tool, không sed) → cập nhật Check tool → ghi nhật ký vào ROADMAP → trả lời kèm mục "ANH CẦN LÀM TRONG UNITY".
4. Luật: không commit/push · không xoá object scene/asset khi chưa duyệt · Console 0 đỏ · economy theo `L1_L10_ECONOMY_TABLE.md` đã duyệt · KHÔNG chạy tool cũ "Setup Village Orders L1-L6/Apply Phase 1 Data" (ghi đè kinh tế).

## Sự thật quan trọng (đừng scan lại từ đầu)

- EXP: `40+10n+n²` (n=level−1), max L30, dư EXP giữ lại. Starter: 400 vàng/15 gem (scene + script đồng bộ).
- ID đặc biệt: nấm thu hoạch = `mushroom` (order đã fix) · cà rốt seed = `ca_rot`, khoai tây seed = `khoai_tay` (KHÔNG có prefix seed_) · 2 món cá unlock 99 (chưa có hệ cá).
- Scene SCN_Farm có **24 HouseOrderController** (nhiều bản trùng tên) — VillageOrderManager gating L1=4→L9=8 nhà theo HouseId; dọn trùng lặp = Sprint 5 (cần duyệt).
- 13 missing script trong scene (chưa rõ vị trí — chạy `Demo L1-L10 → List Missing Scripts`).
- Tutorial 19 bước, mở màn thu hoạch ô chín sẵn (`TutorialPrePlant`, failsafe có sẵn); `FLOWER_PHASE_START_INDEX = 11`.
- Mission system có bug: tracker ghi theo itemId nhưng UI đọc theo missionName → progress không hiện (kế hoạch sửa trong `production/session-state/MISSIONS_L1_L10_PROPOSAL.md`).
- File tools (Read/Edit/Write) là nguồn chính xác; mount bash hay hiển thị file cũ/cụt — chỉ dùng bash để đọc, không sửa.

- Hệ Tàu Khách Du Lịch V2 (2026-08-29): tàu HƯỚNG SỰ KIỆN, không còn đậu 40p cố định — đậu tới khi khách cuối lên tàu
  (lưới an toàn `maxDockMinutes=35`, PHẢI > `patienceMinutes=30` nếu không đường "khách giận" thành code chết).
  Khách: 3-6/chuyến, random 11 prefab NVGAME, món random trong 38 DishData lọc theo unlockLevel, bubble mở LẦN LƯỢT
  hết khách (kiên nhẫn 30p SONG SONG), vàng = Σ giá nguyên liệu CHÍNH ×2 (loại gia vị), EXP = dish.rewardExp.
  Popup boat nằm ở canvas RIÊNG `Canvas_TouristBoatPopup` — ĐỪNG đưa lại vào `canvasPopupRoot` (vào bếp là chết coroutine).
  Sprite khách đã cắt sẵn: `Assets/NV_NPC/NVGAME/Processed/NV01..NV11/NVxx_{down|left|right|up}_{1..3}.png`.
  Mission event phục vụ khách đang TẮT có chủ đích (DeliverOrder là của Bảng Đơn Hàng, bật lên sẽ hoàn thành hộ).
  SETUP: chỉ cần 1 menu `Tools/Farm Game/Tourist Boat/★ SETUP TẤT CẢ (1 nút)` — làm hết mọi thứ, idempotent,
  KHÔNG tự save scene. Toạ độ scene đã đo (dùng lại khi cần): Berth1(-531,-4285) Berth2(151,-4573)
  Berth3(948,-4839) BlindPoint(-9818,-7819) CookingGate(494,-2367) QueueAnchor(400,-2700);
  Grid_Iso45 isometric cellSize(1,0.5) world scale 300 → world=((cx-cy)*0.5*300, (cx+cy)*0.25*300);
  đường đất=Tilemap_IsoDirt(332 ô), cát=IsoSand(868), cầu tàu=IsoDock(63). Cách parse scene 16MB:
  tách theo regex '--- !u!<cls> &<fid>', cls 1=GameObject(m_Name) cls 4=Transform(pos/father/scale),
  cls 1839735485=Tilemap(m_Tiles first.x/y), cls 156049354=Grid. Nếu waypoint bị kéo tay thì ĐỪNG bấm lại nút ★.

## Entries

- [Tutorial L1→L2 Phase](tutorial_l1l2.md) — EXP shortfall 10, tools created, manual steps remaining (LỖI THỜI một phần — xem ROADMAP Sprint 1b)
