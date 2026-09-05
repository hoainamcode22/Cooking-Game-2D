# ACTIVE SESSION — 2026-09-05 (tối)

<!-- STATUS -->
Epic: UI Polish Pass 2 (sửa 4 lỗi Sếp báo sau test Pass 1)
Feature: Tutorial deadlock + Phantom demo 4 bước · Popup Lên Cấp vùng đáy · UI đè (HUD/NPC/toast/lớp tay)
Task: ✅ IMPLEMENT + REVIEW xong, 13 file đã ghi về máy (MD5 khớp) → chờ Sếp làm CẦN BẠN (production/BAO_CAO_UI_PASS2_2026-09-05.md §3) + F10 test 9 điểm. Plan: production/PLAN_UI_PASS2_2026-09-05.md · Backup: production/backup_ui_pass2_2026-09-05/
<!-- /STATUS -->

## Quyết định vòng này
- Kẹt sau thu hoạch = deadlock hàng đợi action khi popup Lên Cấp mở → thêm coroutine tiêu thụ khi popup đóng (không đổi điều kiện cũ).
- Phantom demo: ẩn tay thật khi demo → demo xong hiện; lặp 8s ≤3 lần; gọi ở 05/06/07-08/09/10/13/14/17; bỏ 03. Bảng ĐÃ RÕ giữ lại (03/06b/08b/09b, theo tên bước).
- Popup Lên Cấp vùng đáy: dải −215 · 1 dòng gợi ý −385 · nút −500 · tên ô hiện · bỏ dòng "Mở khoá" · 1 lớp dim.
- HUD 4 nút: ẩn khi khay hạt/hoa mở, mờ 0.35 khi card thoại mở (trừ L2_01/L2_02). NPC sang góc dưới-phải.
- Lớp tay riêng `Canvas_TutorialHand` 440 (tool DRY-RUN/APPLY). Dim giữ 250.
- Không cần asset mới vòng này.

## Việc còn treo (vòng sau)
- Phantom cho 15/16 (hoa: tăng tốc / thu hoạch chậu đầu) chưa gắn.
- MINOR: cổng popup ở bước 08 khi mini-panel còn mở; card "Bỏ qua" 45s có thể đè bảng.
- Kiểm hướng mặt NPC góc phải (NPC_LAT_X).
- Prompt đội vẽ Pass 1 (NPC 37 file + 4 minh hoạ) — chờ hàng về để nạp.

## Files đã ghi (13)
TutorialManager.cs · TutorialPhantomDemoManager.cs · Editor/SetupTutorialL1L2Tool.cs · LevelUpPopupUI.cs · UnlockSlotUI.cs · Editor/LevelUpPopupTownshipTool.cs · Editor/LevelUpPopupRewireTool.cs · SeedPopupController.cs · AnimalGuideController.cs · TutorialDialogueCard.cs · Editor/TutorialV2SetupTool.cs · UI/HudNavHider.cs (MỚI) · Editor/TutorialHandLayerTool.cs (MỚI)
