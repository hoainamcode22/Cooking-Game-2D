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

## Entries

- [Tutorial L1→L2 Phase](tutorial_l1l2.md) — EXP shortfall 10, tools created, manual steps remaining (LỖI THỜI một phần — xem ROADMAP Sprint 1b)
