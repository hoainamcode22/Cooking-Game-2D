# 🔧 FIX MẤT TÊN NHIỆM VỤ / THÀNH TỰU — 2026-08-31

## Kết luận điều tra (vì sao vá 30/08 rồi mà VẪN mất tên)

- Data KHÔNG mất: toàn bộ 200+ asset thành tựu + daily trong `Assets/_Game/Farm/data/Data_Ewa/`
  vẫn còn nguyên `missionName` (đã soi từng file). Database wire trong scene cũng đủ 3 bảng.
- Font Baloo2 KHÔNG hỏng: asset dynamic, nguồn .ttf còn, hỗ trợ đủ tiếng Việt.
- Thủ phạm thật: **chiều CAO khung chữ**. Baloo2 cao dòng = 1.602 × cỡ chữ
  (LiberationSans cũ chỉ ~1.16). Ô tên nhiệm vụ cỡ chữ 25 cần 40px, khung cũ chỉ 34px.
  TMP ở chế độ Ellipsis **và cả Truncate** đều cắt theo CẢ chiều dọc: không lọt nổi
  1 dòng → trả về **0 ký tự** → mất trắng. Bản vá 30/08 (Ellipsis→Truncate) chỉ chữa
  hướng ngang nên tên vẫn biến mất.
- Vì sao CHỈ mất mỗi dòng tên: mọi chữ khác trong popup dựng qua `CreateText`
  (mặc định Overflow — tràn vẫn vẽ), riêng `Txt_Title` là dòng duy nhất bị đặt
  Ellipsis/Truncate. Tab Nhiệm vụ + Thành tựu dựng chung 1 hàm → mất tên cả hai.

## Đã sửa (1 dòng, khoanh vùng)

File: `Assets/_Game/Scripts/Mission/UnifiedTaskPopupUI.cs` — hàm `DungHangTrong`
- Khung `Txt_Title` cao **34 → 44px** (đủ chứa 1 dòng Baloo2 cỡ 25).
- Tâm chữ giữ nguyên y=21, KHÔNG chạm thanh tiến độ (y=−18) hay mép hàng — đã tính hình học cả
  hàng nhiệm vụ (cao 100) lẫn hàng thành tựu (cao 92).
- Giữ nguyên Truncate: tên dài quá 480px vẫn bị cắt gọn phần đuôi (không tràn đè ô thưởng).

Backup file gốc: `production/backup_ten_nhiemvu_2026-08-31/UnifiedTaskPopupUI.cs`
(hỏng thì copy đè lại là về như cũ).

## CẦN SẾP (1 phút)

1. Mở Unity, đợi compile — 0 lỗi đỏ.
2. Play → mở popup Nhiệm vụ → kiểm tra CẢ 3 tab: **Nhiệm vụ / Thành tựu / Điểm danh** —
   tên phải hiện lại (tên quá dài sẽ bị cắt đuôi, đó là chủ đích).
3. Nếu Sếp test bằng bản BUILD (.exe): phải **build lại** mới ăn fix — bản build cũ chứa code cũ.

## Theo dõi QA (chưa sửa — chưa có báo lỗi, không đụng)

Cùng họ lỗi "Ellipsis/Truncate + khung thấp + font cao dòng" còn tiềm ẩn ở các UI bake sẵn:
- `OrderBoardHierarchyBuilderTool.cs` (Bảng đơn hàng), `MarketBoardUIBuilder.cs` (Chợ),
  `StallHierarchyBuilderTool.cs` (Quầy hàng), `LevelUpGiftSlotUI.cs` (ô quà level-up,
  hiện dùng font mặc định nên chưa dính — sẽ dính nếu sau này áp Baloo2).
- Luật kiểm nhanh cho mọi label 1 dòng dùng Baloo2: **cao khung ≥ 1.65 × cỡ chữ**.
