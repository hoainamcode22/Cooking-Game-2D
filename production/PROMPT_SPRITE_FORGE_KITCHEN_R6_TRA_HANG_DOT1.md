# ⚠️ TRẢ HÀNG R6 ĐỢT 1 — 2 FILE GIAO LẠI BẢN CŨ (đã kiểm pixel, không phải vẽ lại)
> Kiểm định bằng so sánh pixel với bản giao trước:
> - `oven_body.png`: chỉ 1,7% pixel khác bản R5 → GẦN NHƯ KHÔNG ĐỔI (mô tả nói "loang màu đất,
>   mặt bàn đá" nhưng ảnh không có).
> - `kitchen_shelf_props.png`: chỉ 0,1% pixel khác bản R4 → KHÔNG VẼ LẠI.
> Yêu cầu vẽ lại THẬT 2 file này theo đúng công thức R6 (đính kèm lại ảnh farm chuẩn):

1. `oven_body.png` (512×512) — giữ nguyên silhouette hiện tại, NHƯNG tô lại:
   vòm đất nung phải có LOANG MÀU + vài vết rỗ nhẹ (không phải 1 gradient trơn),
   highlight mềm tan vào thân (bỏ vệt lưỡi liềm trắng cứng), đế gạch có MẠCH VỮA từng viên
   lệch nhau, củi có VÂN CẮT xoáy, viền nâu ấm dày mảnh biến thiên.
2. `kitchen_shelf_props.png` (512×256) — vẽ lại hẳn: 2 chảo phải có ÁNH KIM CONG nhiều lớp
   (đồng: cam→nâu đỏ + phản chiếu; gang: xám xanh sậm + viền sáng mảnh), chuỗi TỎI là các củ
   to nhỏ KHÁC NHAU có múi, chuỗi ỚT là trái ớt cong bóng (không phải cờ tam giác), rail gỗ có vân.

## GHI NHẬN ĐẠT (không cần làm lại): wall_tile, floor_tile, chalkboard, warehouse_hatch,
panel_board_wood, prep_table, plating_table, oven_glow.
Nhắc nhẹ cho ĐỢT 2: bàn + khung gỗ đợt sau nên đậm chất liệu hơn nữa (vân gỗ rõ như wall_tile đã làm tốt).
