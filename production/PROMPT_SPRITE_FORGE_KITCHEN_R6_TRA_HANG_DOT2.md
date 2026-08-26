# ⚠️ TRẢ HÀNG R6 ĐỢT 2 — 11 FILE (kiểm pixel từng file so với bản cũ)
## BẰNG CHỨNG GIAO HÀNG CŨ (0,0% pixel khác bản R4 — không hề vẽ lại):
1–6. `cat_chef_walk_01..06` — 0,0% khác. Mô tả nói "chuẩn bà lão không lệch tông" nhưng là NGUYÊN bộ cũ.
7. `deco_garlic_string.png` — 0,0% khác.
8. `deco_onion_string.png` — 0,0% khác.
9. `deco_herb_bunch.png` — 0,0% khác.
10. `deco_string_lights.png` — 0,0% khác.
## CHƯA ĐẠT CHUẨN NHÂN VẬT:
11. `cat_sleeping.png` — chỉ sửa 8,9% (thêm mảng màu), vẫn kiểu vector bẹt; YÊU CẦU vẽ lại hẳn
    theo chuẩn `balaohangrong.png`: cel-shading mềm 2 lớp, outline ấm dày mảnh biến thiên, lông có khối.

## VẼ LẠI 11 FILE TRÊN THEO ĐÚNG CÔNG THỨC R6 + CHUẨN NHÂN VẬT BÀ LÃO (đã gửi 2 lần).

## ⛔ VI PHẠM LẶP LẠI LẦN 2 — GHI ĐÈ .META (36 file):
Đợt 2 lại ghi đè .meta thiếu `textureType: 8` làm hỏng import. LUẬT: CHỈ GIAO FILE .PNG.
Nếu tool xuất của đội tự sinh .meta → XÓA .meta trước khi giao.

## ⛔ VI PHẠM LỆNH "GIỮ NGUYÊN": maneki_idle_01..04 bị vẽ lại dù Sếp lệnh giữ nguyên.
Bản cũ đã được khôi phục. KHÔNG tự ý vẽ file ngoài danh sách được giao.

## ✅ ĐÃ NGHIỆM THU ĐỢT 2: oven_fire_01..04 (29% khác, đúng canvas), plant_pot (mới, đạt),
sack_flour (tạm nhận, sẽ xem trong game).
