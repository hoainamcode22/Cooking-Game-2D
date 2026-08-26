# PROMPT GỬI GPT (agent-sprite-forge) — CHỈNH THẲNG ĐẦU TÀU (round 4, touch-up nhỏ)
> ĐÍNH KÈM & TUÂN THỦ: `production/ART_RULES_STUDIO.md` (không text, không nền/bóng, meta Single, pivot đáy).

## VIỆC DUY NHẤT: chỉnh cho ĐẦU TÀU hết nghiêng — KHÔNG vẽ lại, KHÔNG đổi bất cứ thứ gì khác

File cần sửa (ghi đè đúng tên, đúng thư mục `Assets/Export_Train_UI_Package/Sprites/WorldTrain/`):
- `world_loco_upright_01.png` → `world_loco_upright_06.png` (ưu tiên chính — tàu Locomotive2)
- `world_loco_frontleft_01.png` → `world_loco_frontleft_06.png` (bản mirror của cùng đầu tàu —
  sửa xong bản upright thì mirror lại để 2 chiều khớp nhau, kẻo chiều về vẫn nghiêng)

## Lỗi hiện tại (đánh giá từ screenshot in-game 2026-08-26)
Đầu tàu bị NGHIÊNG/XIÊU so với đoàn toa: các trục dọc (ống khói, chóp đồng, vách cabin, đèn pha)
ngả lệch thay vì thẳng đứng; trục dọc THÂN tàu dốc hơn trục thân của các toa → ghép đoàn tàu
nhìn "gãy" ở khớp nối, đầu tàu như sắp đổ.

## Yêu cầu chỉnh (giữ nguyên 100% design, màu, chi tiết, hướng, kích thước canvas, pivot):
1. Mọi chi tiết THẲNG ĐỨNG ngoài đời phải THẲNG ĐỨNG trong ảnh: ống khói, 2 chóp đồng,
   chuông, vách cabin, đèn pha — dựng vuông góc mặt đất, không ngả.
2. Trục dọc thân tàu (đường nối tâm các bánh xe) phải CÙNG ĐỘ DỐC với trục thân toa
   `world_wagon_upright_01.png` — mở file toa đặt cạnh để so, 2 đường gầm phải song song từng pixel.
3. Bánh xe chạm cùng 1 đường ray thẳng (line đáy chung), không bánh cao bánh thấp.
4. Áp cùng độ chỉnh cho CẢ 6 frame (thân đứng yên cùng vị trí giữa các frame — chỉ bánh/nhún đổi),
   rồi mirror ra bộ frontleft. Frame 01 vẫn là tư thế nghỉ.

Kiểm tra trước khi giao: ghép thử [loco + 2 toa] thành 1 hàng trên đường chéo — không được thấy
đầu tàu "gãy cổ" so với toa.
