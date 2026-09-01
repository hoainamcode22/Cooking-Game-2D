# 🎨 PROMPT ĐỘI VẼ — GÓI "UI JUICE V2" (icon vàng + 4 nhân vật ăn mừng popup lên cấp)
> Từ: Tech Lead studio Cooking-Farm-2D · Ngày 2026-09-01 · **BẢN V2 (đã sửa theo chỉ đạo Sếp: bỏ exp star, nhân vật KHÔNG CHÂN)**
> Người nhận: GPT điều hành `agent-sprite-forge` (E:\agent-sprite-forge)
> Code phía game ĐÃ XONG và đang chạy bằng sprite tạm — chỉ chờ đúng các file dưới đây để thay vào.

---

## 0. ĐỌC TRƯỚC KHI VẼ (bắt buộc, theo thứ tự)
1. `E:\Game2\Cooking-Game-2D\production\ART_RULES_STUDIO.md` — 6 luật sắt (tóm ở mục 4 dưới).
2. `E:\Game2\Cooking-Game-2D\production\art-handoff\STYLE_CONTRACT.md` — style + palette đo thật.
3. **View 2 GOLDEN REFERENCES** (phải mở ảnh cho vào context, không truyền path suông):
   - `E:\Game2\Cooking-Game-2D\Assets\Assetsgame\hatgiong\bapcai-removebg-preview.png`
   - `E:\Game2\Cooking-Game-2D\Assets\Assetsgame\hatgiong\cachualever3-removebg-preview.png`
4. **View 2 ảnh REFERENCE ĐỘNG TÁC** Sếp chọn (mascot Family Farm — chỉ tham khảo TƯ THẾ/BỐ CỤC, KHÔNG copy nhân vật):
   - `E:\Game2\Cooking-Game-2D\production\art-handoff\2026-09-01_UI_Juice\REF\ref_mascot_badge.png`
   - `E:\Game2\Cooking-Game-2D\production\art-handoff\2026-09-01_UI_Juice\REF\ref_popup_full.png`
5. View reference nhân vật CỦA GAME (để giữ ĐÚNG nhân dạng):
   - Nông dân chính: `E:\Game2\Cooking-Game-2D\Assets\NV_NPC\NVGAME\Processed\NV01\NV01_down_1.png`
   - Đầu bếp: 1 sprite bất kỳ trong `E:\Game2\Cooking-Game-2D\Assets\NV_CHEF\`
   - Thôn nữ: `...\Processed\NV03\NV03_down_1.png` · Bác nông dân: `...\Processed\NV05\NV05_down_1.png`

## 1. HẠNG MỤC A — ICON ĐỒNG VÀNG (hạng mục icon DUY NHẤT của gói này)
- **File:** `currency/icon_gold.png` · **Canvas 256×256**, object chiếm ~88% canvas, căn giữa, pivot Center.
- Đồng xu vàng DÀY, mặt trước nghiêng nhẹ 3/4 (thấy độ dày mép dưới-phải), kiểu casual Township nhưng theo style contract của game:
  - Fill vàng ấm gradient `#A2993D → #D9A441 → #F7EB89` (highlight phía trên-trái), specular blob mềm rời rạc.
  - Outline nâu ấm `#654129`, dày ~4-5px, khép kín; inner shadow nhẹ trong viền; vành xu nổi (rim) tông sẫm hơn mặt.
  - Mặt xu **emboss chìm hình BÔNG LÚA MÌ** (hoa văn chìm cùng tông — KHÔNG phải chữ/số/ký hiệu $).
- ❌ **KHÔNG vẽ icon kim cương. KHÔNG vẽ icon EXP/sao.** Game dùng icon sẵn có — đừng nộp thêm bất kỳ icon nào khác ngoài `icon_gold.png`.

## 2. HẠNG MỤC B — 4 NHÂN VẬT ĂN MỪNG (12 frame/con, CHỈ ĐẦU + THÂN, KHÔNG CHÂN)
Popup Lên Cấp V2 có các KHUNG TRÒN chứa nhân vật nhún nhảy ăn mừng — đúng kiểu mascot trong 2 ảnh REF (mục 0.4): nhân vật "mọc" trong huy hiệu tròn, chỉ thấy ĐẦU + THÂN TRÊN, biểu cảm cười lớn hết cỡ. Code phát 12 frame @ 12fps, loop vô hạn, và mask tròn sẵn — art chỉ cần vẽ nhân vật rời.

**4 nhân vật — giữ ĐÚNG nhân dạng/trang phục theo reference mục 0.5:**
| ID | Nhân vật | Reference |
|---|---|---|
| char_01 | Nông dân chính (nam, mũ thám hiểm) | NV01 |
| char_02 | Đầu bếp (nón chef trắng) | NV_CHEF |
| char_03 | Thôn nữ | NV03 |
| char_04 | Bác nông dân già | NV05 |

**Spec từng frame (áp dụng cho CẢ 4 con):**
- Canvas **512×512**, PNG alpha. Bố cục: **ĐẦU TO chiếm ~55-60% chiều cao + THÂN TRÊN (vai/ngực, tay ngắn được phép), TUYỆT ĐỐI KHÔNG VẼ CHÂN, không hông** — đáy thân cắt mềm (đường cong tròn) như mascot REF, để lọt gọn trong khung tròn.
- Nhìn THẲNG camera (hướng down), phẳng kiểu popup, biểu cảm ĂN MỪNG: mắt nhắm cười / miệng cười mở lớn.
- **12 frame CÙNG kích thước canvas, tâm X cố định, ĐÁY THÂN cố định y≈470** trên mọi frame (nhún bằng squash/stretch thân + đầu, KHÔNG rời khỏi đáy — vì không có chân nên không có động tác bật nhảy rời đất).
- Nhịp "nhún nhảy tại chỗ" 12 frame (loop mượt f12→f01):
  - f01: tư thế NGHỈ (thẳng, cười) — bắt buộc là frame nghỉ.
  - f02–f04: thân LÚN xuống (squash: thân bè ra, đầu cúi nhẹ, má phồng).
  - f05–f07: thân VƯƠN cao (stretch: thân thon lại, đầu ngửa, miệng cười to nhất, tay/vai nhấc lên nếu có tay).
  - f08–f09: đỉnh vươn, đầu lắc nhẹ sang một bên (tilt ~5-8°).
  - f10–f12: lún về lại, dư chấn "mẩy mẩy" nhỏ dần rồi khớp về f01.
- ❌ KHÔNG bake hiệu ứng vào frame (không confetti, sao, khói, bóng đổ, motion blur) — code phun FX runtime.

## 3. BÀN GIAO — ĐÚNG TÊN FILE, ĐÚNG THƯ MỤC, KHÔNG FILE THỪA
Giao vào: `E:\Game2\Cooking-Game-2D\production\art-handoff\2026-09-01_UI_Juice\`
```
2026-09-01_UI_Juice/
├── REF/                           (Lead đặt sẵn 2 ảnh reference — CHỈ ĐỌC, không sửa)
├── currency/
│   └── icon_gold.png              (bắt buộc — file icon DUY NHẤT)
├── characters/
│   ├── char_01/char_01_f01.png … char_01_f12.png
│   ├── char_02/char_02_f01.png … char_02_f12.png
│   ├── char_03/char_03_f01.png … char_03_f12.png
│   └── char_04/char_04_f01.png … char_04_f12.png
└── DONE.md   (liệt kê đủ file + tự QC: kích thước đúng, alpha sạch 100%, 0% viền trắng, đáy thân khớp y≈470 mọi frame)
```
Sau khi có DONE.md, Tech Lead sẽ lấy về gắn vào khung sườn (`Assets/Art/UI/Currency/` và `Assets/Art/UI/LevelUpV2/characters/`) — đội vẽ KHÔNG cần đụng vào Assets.

## 4. 6 LUẬT SẮT (dán từ ART_RULES_STUDIO.md — vi phạm là trả hàng)
1. ❌ TUYỆT ĐỐI KHÔNG TEXT: không chữ, số, logo, label trên bất kỳ asset nào.
2. ❌ KHÔNG NỀN, KHÔNG BÓNG ĐỔ: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ.
3. ✅ Meta Unity chuẩn: mỗi file 1 sprite (Single); icon tiền tệ pivot Center; nhân vật gói này pivot Center (đứng trong khung tròn, KHÔNG đứng đất).
4. ✅ Frame animation: mọi frame CÙNG kích thước canvas, thân cùng vị trí; frame 01 = nghỉ; KHÔNG bake hiệu ứng.
5. ✅ Style chuẩn: theo STYLE_CONTRACT (outline nâu ấm #442510→#654129 KHÔNG ĐEN, hand-painted semi-realistic, không cel-shading/pixel-art), palette Export_Train_UI_Package (burgundy #8E1F3B + đồng vàng #D9A441).
6. ✅ Giao đúng TÊN FILE + THƯ MỤC ở mục 3, không thêm file phụ (_single, @2x…).
