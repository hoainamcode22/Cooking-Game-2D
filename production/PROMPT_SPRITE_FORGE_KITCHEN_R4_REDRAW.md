# 🎨 PROMPT AGENT-SPRITE-FORGE — KITCHEN R4: VẼ LẠI 14 FILE BỊ LỆCH STYLE
> ❌ ĐỢT R3 BỊ TRẢ LẠI (11/18 file): các file dưới đây vẽ kiểu VECTOR BẸT, hình học đơn giản
> (tỏi = vòng tròn trắng trơn, thảo mộc = tam giác xanh, mèo 4 chân thô) — KHÔNG GIỐNG game.
> ✅ Sếp sẽ đính kèm 2 ẢNH THAM CHIẾU (bếp lò ấm cúng + bé mèo đầu bếp). PHẢI BÁM SÁT STYLE ẢNH:
> vẽ tay storybook, đổ bóng mềm có khối, màu ấm trầm, outline nâu đậm dày sạch —
> TUYỆT ĐỐI KHÔNG vẽ kiểu flat-vector/icon hình học.
> 📁 GHI ĐÈ đúng tên file cũ tại `Assets/Export_Kitchen_UI_Package/Sprites/` — tool tự gán lại.

## ⚠️ LUẬT ART STUDIO (đính kèm bắt buộc)
1. ❌ KHÔNG TEXT trên asset. 2. ❌ KHÔNG nền, KHÔNG bóng đổ ra ngoài — alpha trong suốt 100%.
3. ✅ spriteMode: 1 (Single). 4. ✅ Frame cùng canvas, thân cùng vị trí, frame 01 = nghỉ.
5. ✅ Không bake khói/lửa/hiệu ứng vào asset.

## NHÓM A — NỒI & DECOR (5 file, vẽ lại theo ảnh bếp lò tham chiếu)
1. `cook_pot.png` (300×280, pivot Bottom-Center) — NỒI NẤU SÚP như trong ảnh mẫu bếp:
   nồi gang bụng tròn màu đồng-nâu có ánh kim ấm, 2 quai, nắp gỗ hé lệch, muôi gỗ gác miệng,
   đặt trên bệ gạch đỏ-nâu thấp kiểu lò trong ảnh. Đổ bóng mềm trong thân nồi, highlight cong.
   KHÔNG khói/lửa bake.
2. `deco_garlic_string.png` (128×220, pivot Top-Center) — CHÙM TỎI BỆN DÂY y như trong ảnh mẫu:
   5–6 củ tỏi trắng-kem CÓ KHỐI (múi tỏi, gốc tím nhạt, lá bện nâu vàng), treo dây thừng.
   Không phải vòng tròn trơn!
3. `deco_onion_string.png` (128×220, pivot Top-Center) — CHÙM HÀNH BỆN như ảnh mẫu:
   củ hành nâu-cam bóng có vân dọc, bện thành chuỗi, dây thừng treo.
4. `deco_herb_bunch.png` (128×200, pivot Top-Center) — BÓ THẢO MỘC KHÔ TREO NGƯỢC như ảnh mẫu:
   lá xanh olive rủ xuống từng nhánh mềm mại + chút hoa oải hương tím, buộc dây twine.
   Không phải tam giác!
5. `deco_string_lights.png` (700×100, pivot Top-Center) — DÂY ĐÈN như ảnh mẫu: dây võng tự nhiên,
   bóng đèn tròn vàng ấm PHÁT SÁNG có lõi sáng + viền glow dịu bên trong bóng, đui đen nhỏ.

## NHÓM B — BÉ MÈO ĐẦU BẾP (6 frame, 256×240, pivot Bottom-Center) — VẼ LẠI HOÀN TOÀN
❌ Bản cũ: mèo 4 chân nhìn nghiêng, tạp dề hình vuông — SAI.
✅ Vẽ ĐÚNG BÉ MÈO TRONG ẢNH THAM CHIẾU SỐ 2: mèo tam thể (trắng + mảng cam + mảng nâu đen)
   ĐỨNG THẲNG 2 CHÂN dáng chibi mập tròn, đầu to tròn, má hồng, mắt cười tít ^^,
   MŨ ĐẦU BẾP TRẮNG phồng, TẠP DỀ HỒNG có viền bèo + túi nhỏ, nhìn CHÍNH DIỆN hơi nghiêng 3/4 PHẢI.
   Chu kỳ đi lạch bạch (waddle) đứng thẳng:
   - `cat_chef_walk_01.png` — đứng nghỉ, 2 tay thu trước tạp dề, đuôi cong
   - `cat_chef_walk_02.png` — chân phải bước, thân nghiêng nhẹ trái, đuôi vẫy
   - `cat_chef_walk_03.png` — giữa bước, thân nhấp lên 5px
   - `cat_chef_walk_04.png` — chân trái bước, thân nghiêng nhẹ phải
   - `cat_chef_walk_05.png` — giữa bước pha ngược, thân nhấp lên 5px
   - `cat_chef_walk_06.png` — khép về gần tư thế 01
   (Code tự lật hướng trái/phải — chỉ vẽ 1 hướng.)

## NHÓM C — 3 PROP CŨ NHÌN KHÔNG RA HÌNH (Sếp hỏi "2 cục ở dưới là gì") — VẼ LẠI
12. `kitchen_shelf_props.png` (512×256, pivot Top-Center) — GIÀN TREO 2 NỒI như mockup + style asset
    farm của game: thanh rail gỗ ngang có dây treo, 1 NỒI ĐỒNG to + 1 NỒI GANG nhỏ treo móc,
    có khối ánh kim ấm, thêm chuỗi ớt đỏ nhỏ + chuỗi tỏi trắng treo cạnh. KHÔNG vẽ vòng tròn bẹt!
13. `sack_flour.png` (256×280, pivot Bottom-Center) — BAO BỘT MÌ vải kem thắt dây thừng, miệng bao
    hé lộ bột trắng, có nếp vải + khối bóng mềm — nhìn phải RA bao bột (bản cũ nhìn như cục u).
14. `cat_sleeping.png` (300×200, pivot Bottom-Center) — MÈO CAM NẰM CUỘN NGỦ thấy rõ TAI + ĐUÔI
    quấn quanh thân + mặt nhắm zZz, cùng style bé mèo đầu bếp — nhìn phải RA con mèo.

## GIỮ NGUYÊN (không vẽ lại): biển Về nông trại, 4 nút + 2 tab (Sếp đã duyệt đợt R3).
## SAU KHI GIAO: báo Sếp bấm `Tools → Farm Game → Kitchen → Setup Kitchen UI v2`.
