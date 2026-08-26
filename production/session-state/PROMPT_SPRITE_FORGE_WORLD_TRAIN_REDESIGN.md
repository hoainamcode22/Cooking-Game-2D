# PROMPT GỬI GPT (agent-sprite-forge) — VẼ LẠI TÀU WORLD ĐỒNG BỘ VỚI TÀU POPUP

## Bối cảnh & mục tiêu
Tàu ngoài world map đang dùng 3 bộ ảnh tạm KHÔNG đồng nhất (2 đầu tàu 2 màu khác nhau, toa là xe goòng mỏ,
style vẽ mềm khác hẳn popup). Cần vẽ lại TOÀN BỘ tàu world theo ĐÚNG NGÔN NGỮ THIẾT KẾ của tàu trong popup
để người chơi hiểu ngay: "con tàu ngoài map = con tàu trong popup".

## NGUỒN STYLE BẮT BUỘC THAM CHIẾU (mở xem trước khi vẽ)
- `Assets/Export_Train_UI_Package/Sprites/flat_locomotive_horizontal.png` — đầu tàu chuẩn: thân ĐỎ BURGUNDY
  (#8E1F3B vùng sáng → #6E1830 vùng tối), viền/ống/chuông ĐỒNG VÀNG (#D9A441/#F2C063), cabin mái cong,
  ống khói loe, đèn pha tròn vàng, bánh nan hoa đồng, outline nâu đậm dày kiểu cartoon, đổ khối 3D mềm.
- `Assets/Export_Train_UI_Package/Sprites/flat_wagon_horizontal.png` — toa chuẩn: thùng GỖ NÂU ẤM vân ngang,
  nẹp góc kim loại xám đinh tán, khoang MỞ RỖNG nhìn thấy lòng toa (để game đặt bubble hàng hóa lơ lửng trên toa),
  2 cụm bánh nan hoa đồng.

## GÓC NHÌN & KÍCH THƯỚC BẮT BUỘC THAM CHIẾU (để gắn vào scene là khớp, KHÔNG đổi)
Đường ray world chạy CHÉO kiểu isometric → vẽ góc 3/4 GIỐNG HỆT góc của 3 file hiện tại (mở xem để khớp góc):
- `Assets/Taulua/taulua.png` (đầu tàu nhìn chéo TRÁI-XUỐNG, khung đơn ~677x369 chia 12 frame)
- `Assets/Taulua/đầu tàu mới/tauchovaypham.png` (chéo, ~863x289 chia 8 frame)
- `Assets/Taulua/toatau.png` (toa chéo, ~677x369 chia 12 frame)
Tỉ lệ thân/bánh, footprint đáy phải gần bằng sprite cũ (game giữ nguyên scale & spacing trong scene).

## DANH SÁCH FILE BÀN GIAO (PNG nền trong RGBA, KHÔNG chữ)
Xuất vào: `Assets/Export_Train_UI_Package/Sprites/WorldTrain/`

VẼ ĐỦ FRAME ANIMATION — mỗi hướng 6 FRAME RỜI đánh số _01→_06 (mỗi file 1 frame, KHÔNG gộp sheet):
1. `world_loco_frontleft_01.png` → `world_loco_frontleft_06.png`
   Đầu tàu burgundy góc chéo trái-xuống. Animation loop: bánh nan hoa QUAY (mỗi frame xoay ~15°)
   + thân nhún nhẹ lên xuống 2-3px + piston/thanh truyền chuyển động nếu thấy được.
2. `world_loco_upright_01.png` → `world_loco_upright_06.png`
   Cùng đầu tàu, góc chéo phải-lên, cùng nhịp animation.
3. `world_wagon_frontleft_01.png` → `world_wagon_frontleft_06.png`
   Toa gỗ khoang mở, chéo trái-xuống. Animation: bánh quay + thùng nhún lệch pha nhẹ so với đầu tàu.
4. `world_wagon_upright_01.png` → `world_wagon_upright_06.png`
   Cùng toa, góc chéo phải-lên.

QUY TẮC CỨNG (đọc kỹ — lần trước dính lỗi meta):
- CHỈ 1 kiểu đầu tàu dùng chung cho cả tàu giao lẫn tàu nhận (chấm dứt 2 tàu 2 màu).
- ❌ TUYỆT ĐỐI KHÔNG VẼ KHÓI vào bất kỳ frame nào — khói do code phun runtime bằng
  `train_smoke_puff.png` có sẵn, bốc từ miệng ống khói. Vẽ miệng ống khói RÕ, hở, hướng thẳng đứng.
- Mọi frame CÙNG HƯỚNG phải CÙNG KÍCH THƯỚC canvas và thân tàu đứng yên cùng vị trí
  (chỉ bánh/độ nhún đổi giữa frame) — lệch canvas là animation bị giật.
- Frame 01 = tư thế nghỉ (dùng khi tàu đứng ở ga).
- Meta Unity: spriteMode: 1 (Single) cho TỪNG file. Pivot TÂM ĐÁY (bottom-center) để bánh chạm ray.
- Kèm bản @2x nếu pipeline cho phép.

## Việc team Dev (Claude) sẽ tự làm sau khi nhận hàng — GPT KHÔNG đụng vào
Tool gắn sprite vào Locomotive/Locomotive2/Wagon_01-04 (2 tàu), tắt Animator frame cũ,
khói particle + nhún nhẹ toa khi chạy, chọn sprite theo hướng ray. KHÔNG sửa file .cs, .unity, .prefab nào.
