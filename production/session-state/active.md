# ACTIVE SESSION - 2026-09-07 (VÒNG 12 — HỒ CÂU)

<!-- STATUS -->
Epic: Hồ Câu (SCN_Fishing) — scene câu cá có bạn bè, offline trước, Firebase sau
Feature: Vòng 12 - khung OFFLINE đầy đủ: 69 file .cs mới trong Assets/_Game/Fishing + 5 Editor Tool + 24 frame nhân vật
Task: Chờ Sếp compile → ★ SETUP TẤT CẢ → Check Fishing → menu 5 gắn SCN_Farm → Play test theo `production/BAO_CAO_VONG12_2026-09-07.md` mục 3
<!-- /STATUS -->

## Đã đóng vòng này
- Cắt 2 sheet hành khách → 24 PNG `Assets/_Game/Fishing/Art/Characters/{PlayerF,PlayerM}/` (nền trong suốt, cùng canvas, chân chạm đáy). Idle: down/up = frame 1, left/right = frame 2.
- Kiến trúc: module tự chứa `Assets/_Game/Fishing/`, namespace `FarmGame.Fishing`, world 1 unit (không ×150), Single load, tầng mạng `IRoomService/IChatService/IFriendService` + Local* offline có bot.
- 3 file cũ sửa cộng thêm: SaveAdapters (+3 khoá), SaveVersionGuard (+họ FISHING), PopupManager (+2 cờ AnyOpen). Backup `production/backup_vong12_2026-09-07/lead/`.
- Prompt đội vẽ: `production/PROMPT_SPRITE_FORGE_HO_CAU_2026-09-07.md` (37 file, thư mục `art-handoff/2026-09-07_HoCau/`).

## Quyết định Sếp đã chốt (07/09)
- Giỏ cá riêng + Quầy Cá riêng (không dùng kho farm, không đụng ShopManager).
- Online = Firebase toàn bộ; project `possible-jetty-436317-c2` (link đã gửi). Làm offline trước.
- Tab "Hồ câu" gắn HUD bằng tool, không sửa tay scene.

## Việc còn treo
- CẦN SẾP: duyệt số kinh tế 4 cần / 10 cá (đề xuất trong FishingDataSetupTool); chốt M5-1 audience trước khi bật chat online; cài Firebase Unity SDK + đăng nhập console khi làm vòng online; vị trí Tab_Fishing trên HUD chật.
- Chờ art: gói Hồ Câu 37 file.
- Chưa compile thật (Lead không có Unity). Lỗi đỏ → Sếp gửi nguyên văn.

## Vòng 11 (trước) còn treo — chưa động
- 3 hệ trùng khoá save (chuồng/máy xay/nhà dân), Pen_03/House_01 bị SetActive(false), plot khít lưới, tên asset Chậu Hoa3/4 đảo. Xem `BAO_CAO_VONG11_2026-09-06.md` mục 7.
