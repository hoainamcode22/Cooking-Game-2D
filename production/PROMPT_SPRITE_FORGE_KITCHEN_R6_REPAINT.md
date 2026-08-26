# 🎨 PROMPT AGENT-SPRITE-FORGE — KITCHEN R6: REPAINT TOÀN BỘ SCENE BẾP (48 file)
> Lệnh Sếp: vẽ lại TOÀN BỘ asset màn bếp cho ĐÚNG CHẤT VẼ TAY của game farm — "thật hơn, đỡ AI hơn".
> Sếp đính kèm ảnh farm (nhà ga, nhà cửa, cây) làm CHUẨN CHẤT LIỆU — asset bếp phải đặt cạnh
> asset farm mà không lệch tông.
> ⛔ KHÔNG ĐỔI: bố cục, silhouette, tỉ lệ, góc nhìn, kích thước canvas, TÊN FILE (GHI ĐÈ đúng tên).
> ✅ CHỈ ĐỔI: cách tô, chất liệu, độ "tay người".
> 📁 `Assets/Export_Kitchen_UI_Package/Sprites/` — giao xong Sếp bấm tool là lên, không sửa code.

## 🖌️ CÔNG THỨC "ĐỠ AI" — áp cho MỌI file
1. Nguồn sáng ấm TRÊN-TRÁI thống nhất; tối thiểu 3 lớp: màu nền → bóng loang mềm → highlight mềm.
   Cấm gradient máy móc đều tăm tắp, cấm vệt highlight trắng cứng hình lưỡi liềm hoàn hảo.
2. BẤT ĐỐI XỨNG CÓ CHỦ Ý (dấu vết tay người): củ tỏi mỗi củ to nhỏ khác nhau, ván gỗ mỗi tấm
   lệch vân một chút, đường viền hơi run tự nhiên — không copy-paste đối xứng gương.
3. Texture chất liệu: gỗ có vân + mắt gỗ, đất nung loang màu + chấm rỗ, vải có nếp, kim loại có
   ánh phản chiếu cong, men gạch có loang — vẽ TIẾT CHẾ như asset farm, không nhiễu hạt.
4. Outline nâu sậm ẤM (không đen tuyền), độ dày biến thiên: đậm viền ngoài, mảnh chi tiết trong.
5. Bóng tiếp xúc mờ BÊN TRONG asset (chân bàn, đáy nồi sậm dần); KHÔNG drop-shadow ra ngoài.
6. Khóa bảng màu theo farm: nâu gỗ ấm / cam đất / vàng kem / xanh lá tươi / đỏ gạch trầm.
7. Luật cứng giữ nguyên: 0 text, alpha 100%, spriteMode Single, frame anim cùng canvas cùng vị trí thân.

## 📋 DANH SÁCH REPAINT — GIAO THEO 3 ĐỢT

### ĐỢT 1 — NỀN & CÔNG TRÌNH LỚN (định vũ trụ, làm trước — 10 file)
kitchen_wall_tile · kitchen_floor_diamond_tile (giữ tileable) · oven_body · oven_glow (nếu cần khớp lại)
· prep_table · plating_table · warehouse_hatch · chalkboard_menu · panel_board_wood (9-slice giữ vùng an toàn)
· kitchen_shelf_props

### ĐỢT 2 — PROP, DECOR & NHÂN VẬT (17 file)
sack_flour · cat_sleeping · plant_pot · cook_pot (để dành dùng sau) · deco_garlic_string ·
deco_onion_string · deco_herb_bunch · deco_string_lights · maneki_idle_01..04 (4 frame, giữ dáng) ·
cat_chef_walk_01..06 (giữ dáng đứng 2 chân + mũ + tạp dề, tô lông mềm 2 lớp, má hồng loang)
+ oven_fire_01..04 (lửa 4 frame: tô lõi vàng → cam → đỏ mềm, lưỡi lửa mỗi frame khác nhau tự nhiên)
  — LƯU Ý: 4 frame lửa GIỮ đúng canvas + vị trí cũ.

### ĐỢT 3 — UI SKIN (21 file)
panel_paper_cream · ribbon_header_orange · card_ingredient · card_locked · card_selected_glow ·
icon_lock · taste_bar_track · taste_bar_fill · taste_marker · chip_taste · tab_pill_on · tab_pill_off ·
btn_big_green · btn_big_gray · btn_red_small · btn_paper_small · btn_back_farm_sign · btn_back_to_farm
— UI vẫn phải sạch dễ đọc, nhưng thêm chất gỗ/giấy thật: viền gỗ có vân, giấy kem có gân nhẹ,
nút "mọng" giữ độ nổi khối nhưng highlight mềm tự nhiên hơn.

## GIAO HÀNG
Mỗi đợt xong báo Sếp → Sếp bấm `Tools → Farm Game → Kitchen → Setup Kitchen UI v2` + Ctrl+S là
toàn bộ lên hình tự động. Không đổi tên, không thêm file mới, không đổi kích thước.

---
## 🔄 CẬP NHẬT 2026-08-26 (lệnh Sếp sau khi xem đợt 1):
- ✅ ĐÃ NGHIỆM THU: oven_body + kitchen_shelf_props bản vẽ lại (đạt), cùng 8 file đợt 1 còn lại.
- ⛔ GỠ KHỎI DANH SÁCH REPAINT — GIỮ NGUYÊN BẢN CŨ, KHÔNG VẼ LẠI NỮA:
  `kitchen_wall_tile`, `kitchen_floor_diamond_tile` (Sếp giữ nền gỗ cũ — đã khôi phục bản gốc),
  `panel_board_wood`, `panel_paper_cream` (khung bảng công thức + khay item giữ nguyên),
  `maneki_idle_01..04` (mèo thần tài giữ nguyên), các nút/tab đã duyệt R3.
- ĐỢT 2 CÒN LẠI: sack_flour · cat_sleeping · plant_pot · cook_pot · deco_garlic_string ·
  deco_onion_string · deco_herb_bunch · deco_string_lights · cat_chef_walk_01..06 · oven_fire_01..04
- ĐỢT 3 CÒN LẠI: card_ingredient · card_locked · card_selected_glow · chip_taste ·
  taste_bar_track/fill/marker · icon_lock · ribbon_header_orange
- ⚠️ LỖI KỸ THUẬT PHẢI SỬA TỪ NAY: file .meta đợt 1 bị xuất THIẾU dòng `textureType: 8`
  → Unity không nhận Sprite, cả scene mất hình. TỪ NAY KHÔNG ĐƯỢC GHI ĐÈ FILE .META —
  chỉ giao file .png, để nguyên .meta của project.

---
## 🐱 BỔ SUNG ĐỢT 2 — CHUẨN NHÂN VẬT BẮT BUỘC (lệnh Sếp 2026-08-26)
Mọi nhân vật mèo phải vẽ ĐÚNG CHẤT bộ nhân vật có sẵn của game — chuẩn là file
`Assets/BAOLAOHANGRONG/balaohangrong.png` (bà lão hàng rong — Sếp đính kèm ảnh):
- Cel-shading MỀM 2 lớp (không gradient máy), má hồng tròn, mắt đơn giản biểu cảm
- Outline nâu sậm ấm, dày ngoài mảnh trong, nét hơi run tay
- Tỉ lệ chibi mũm mĩm đầu to, chi tiết vải có nếp gấp nhỏ (như tạp dề bà lão)
- Palette trầm ấm hòa với nhân vật hiện có, đứng cạnh bà lão KHÔNG lệch tông

Áp vào 2 việc:
1. `cat_sleeping.png` (300×200) — VẼ LẠI HẲN: mèo vàng mướp nằm cuộn ngủ theo đúng chất trên,
   thấy rõ tai, sọc lưng, đuôi quấn, mặt ngủ bình yên. KHÔNG vẽ chữ zZz (code đã làm animation zZz bay).
2. `cat_chef_walk_01..06` — vẽ lại theo cùng chất nhân vật (giữ dáng đứng 2 chân + mũ bếp + tạp dề hồng,
   canvas 256×240, 6 frame waddle như spec cũ).
