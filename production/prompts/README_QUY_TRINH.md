# QUY TRÌNH LÀM VIỆC — Folder prompts cho 2 thợ Cursor

**Vị trí:** `Cooking-Game-2D/production/prompts/`
Đây là "hộp thư" cố định. Mỗi vòng, nhà phát hành (mình) bỏ prompt mới vào đây; 2 thợ vào đọc và làm.

## Mỗi phiên, mỗi thợ đọc theo thứ tự:
1. `_SHARED_CONTRACT.md` — luật chung: SCAN kỹ (đừng làm lại cái đã có), TOOL-FIRST (mọi setup = 1 nút bấm), phân chia file chống xung đột, API contract.
2. File prompt của MÌNH trong vòng hiện tại (xem "VÒNG ĐANG CHẠY" bên dưới).
3. Làm theo prompt → nộp báo cáo đúng mẫu ở cuối prompt.

## Quy tắc bất biến (nhắc lại):
- **SCAN trước:** grep + đọc, phân loại "đã có / thiếu UI / thiếu data / chưa có" → ưu tiên **hoàn thiện & nối dây** hơn viết mới.
- **TOOL-FIRST:** không làm tay trong Unity. Viết Editor Tool (`Tools → …`) tự dựng, idempotent, có log, tự ping. Anh chỉ bấm 1 nút.
- **Art:** thợ chỉ để **slot trống** trong tool; art do anh vẽ rồi thả vào (theo "DANH SÁCH ART" cuối mỗi prompt). Tool nên tự gán sprite theo tên file nếu có.
- **Không đụng file .unity bằng tay để commit** — giao TOOL + code + data; anh chạy tool để dựng vào scene (nhờ vậy 2 thợ song song không vỡ merge scene).
- Console 0 lỗi đỏ; build được; báo cáo có "KIỂM KÊ TRƯỚC KHI LÀM", "ANH CẦN LÀM TRONG UNITY" (chỉ dạng bấm tool), "CẦN BẠN".

## VÒNG ĐANG CHẠY: "VÒNG LOOP" — Vòng chơi L1→L10 + Hệ giữ chân
- **Thợ A** → `VONG_LOOP_ThoA_Tutorial_L1_L10.md` (vòng chơi liền mạch + tutorial dắt tay L3–L10)
- **Thợ B** → `VONG_LOOP_ThoB_Rewards_Events_Retention.md` (thưởng · sự kiện · điểm danh · giữ chân)

## Các vòng khác (đã soạn sẵn, chạy sau khi anh yêu cầu):
- `ROUND1_ThoA_Save_Analytics.md` + `ROUND1_ThoB_Menu_Settings_Build.md` — nền vận hành (save/menu/settings/build/analytics). Nên chạy ngay sau VÒNG LOOP.
