# 🎨 PROMPT AGENT-SPRITE-FORGE — KITCHEN POLISH R3 (nút + nồi + mèo đầu bếp + decor)
> Sếp gửi nguyên khối này cho đội vẽ. Giao file vào: `Assets/Export_Kitchen_UI_Package/Sprites/`
> Code + tool ĐÃ WIRE SẴN đúng tên file bên dưới — giao đúng tên là tự lên hình khi bấm tool.

## ⚠️ LUẬT ART STUDIO — ĐÍNH KÈM BẮT BUỘC
1. ❌ TUYỆT ĐỐI KHÔNG TEXT: không chữ, không số, không label trên bất kỳ asset nào.
   Nút "VỀ NÔNG TRẠI" tham chiếu CÓ CHỮ → vẽ BIỂN GỖ TRỐNG, chữ do game render TMP đè lên.
2. ❌ KHÔNG NỀN, KHÔNG BÓNG ĐỔ: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ.
3. ✅ Meta Unity: spriteMode: 1 (Single) từng file.
4. ✅ Frame animation: mọi frame CÙNG kích thước canvas, thân cùng vị trí; frame 01 = tư thế nghỉ.
5. ✅ Style: đồng bộ bộ Kitchen hiện tại (gỗ nâu ấm, kem #F5E8CF, outline nâu đậm cartoon,
   bo tròn dịu, dễ thương thân thiện — "ít giống AI", tay vẽ storybook như 2 ảnh tham chiếu Sếp gửi).
6. ✅ Giao đúng TÊN FILE, không thêm hậu tố tự ý.

## DANH SÁCH ASSET (13 file đơn + 6 frame mèo = 19 file)

### Nhóm 1 — NÚT (đang là màu bẹt, cần vẽ lại cho "mọng")
1. `btn_back_farm_sign.png` (512×280) — Biển gỗ treo 2 dây thừng từ mép trên (như ảnh tham chiếu
   "VỀ NÔNG TRẠI"): tấm gỗ nâu bo tròn viền đậm, icon chuồng/nhà nông trại đỏ nhỏ ở giữa-trên,
   2 nhánh lá xanh ôm 2 góc dưới. PHẦN GIỮA-DƯỚI ĐỂ TRỐNG cho chữ TMP. KHÔNG CHỮ.
2. `btn_big_green.png` (512×160, 9-slice được) — Nút xanh lá "mọng" kiểu nút NẤU! tham chiếu:
   thân gradient xanh tươi, viền xanh đậm, highlight bóng cong phía trên, đáy sậm tạo khối. KHÔNG CHỮ.
   ⚠️ GHI ĐÈ file cũ cùng tên (file cũ là hình chữ nhật bẹt).
3. `btn_big_gray.png` (512×160, cùng shape nút 2) — Bản "chưa sẵn sàng": xám-nâu ấm, cùng bevel/highlight.
   ⚠️ GHI ĐÈ file cũ.
4. `btn_red_small.png` (256×96, cùng ngôn ngữ bevel) — Nút đỏ nhỏ (Bỏ hết). ⚠️ GHI ĐÈ file cũ.
5. `btn_paper_small.png` (256×96) — Nút giấy kem nhỏ viền nâu, góc bo, bóng nhẹ TRONG thân nút
   (không bóng đổ ra ngoài) — dùng cho "Xem món khác".
6. `tab_pill_on.png` (256×96) — Tab gỗ sáng đang chọn: gỗ vàng ấm, viền nâu, đỉnh cong pill. ⚠️ GHI ĐÈ.
7. `tab_pill_off.png` (256×96) — Tab lặn: gỗ nâu sậm hơn, cùng shape. ⚠️ GHI ĐÈ.

### Nhóm 2 — NỒI + DECOR BẾP (cho giống khu bếp cooking ấm cúng như ảnh tham chiếu)
8. `cook_pot.png` (300×280, pivot Bottom-Center) — Nồi nấu súp đứng trên kiềng/bệ gạch nhỏ như ảnh
   tham chiếu bếp lò: nồi gang tròn nâu-đồng có 2 quai, nắp gỗ hé, muôi gỗ gác miệng nồi.
   KHÔNG vẽ khói/hơi (code phun runtime). KHÔNG lửa bake (lửa là prefab hạt riêng).
9. `deco_garlic_string.png` (128×220, pivot Top-Center) — Chùm tỏi trắng 5–6 củ bện dây treo.
10. `deco_onion_string.png` (128×220, pivot Top-Center) — Chùm hành tím/vàng bện dây treo.
11. `deco_herb_bunch.png` (128×200, pivot Top-Center) — Bó thảo mộc khô treo ngược (xanh olive + oải hương).
12. `deco_string_lights.png` (700×100, pivot Top-Center) — Dây đèn vàng ấm võng nhẹ 2 nhịp,
    bóng đèn tròn nhỏ phát sáng dịu (glow VẼ TRONG bóng đèn, không halo tràn nền).

### Nhóm 3 — MÈO ĐẦU BẾP ĐI DẠO (6 frame, canvas 256×240, pivot Bottom-Center)
Mèo tam thể trắng-cam như ảnh tham chiếu: MŨ ĐẦU BẾP TRẮNG + TẠP DỀ HỒNG, dáng chibi mập tròn.
NHÌN NGHIÊNG PHẢI (side-view), đi bộ 4 chân. Cùng canvas, thân cùng cao độ mọi frame:
13. `cat_chef_walk_01.png` — đứng nghỉ 4 chân chạm đất, đuôi cong (frame này cũng dùng làm idle)
14. `cat_chef_walk_02.png` — chân trước phải bước lên
15. `cat_chef_walk_03.png` — sải giữa, thân nhấp lên 4px
16. `cat_chef_walk_04.png` — chân đổi pha
17. `cat_chef_walk_05.png` — sải giữa pha ngược, thân nhấp lên 4px
18. `cat_chef_walk_06.png` — khép bước về gần tư thế 01
(Code tự lật trái/phải và cho mèo đi–dừng–quay đầu, chỉ cần vẽ hướng PHẢI.)

## SAU KHI GIAO
Báo Sếp → Sếp bấm `Tools → Farm Game → Kitchen → Setup Kitchen UI v2` là tự gán hết.
