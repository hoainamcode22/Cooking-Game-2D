# 📤 ĐƠN ĐẶT ART — GỬI ĐỘI VẼ (agent-sprite-forge) — 2026-09-01

> **Sếp copy TOÀN BỘ file này dán cho GPT điều hành `agent-sprite-forge`.**
> Lead đã dựng xong khung sườn code cho hệ 5 stage; đây là 3 đơn art còn thiếu để gói hoàn chỉnh.
> Giao xong vào đúng folder ghi trong từng đơn → Lead sẽ tự gắn vào khung sườn bằng Editor Tool.

---

## 🎨 LUẬT ART STUDIO — BẮT BUỘC TUÂN THỦ (dán nguyên khối, không được lược)

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ trên BẤT KỲ asset nào. Text do game render bằng TMP. Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ dưới chân object — bóng do game tự vẽ để đồng nhất theo giờ trong ngày.
3. ✅ **Meta Unity chuẩn**: spriteMode Single từng file · pivot **Bottom-Center** cho object đứng đất.
4. ✅ **Frame animation**: mọi frame cùng hướng = **CÙNG kích thước canvas**, thân đứng yên cùng vị trí; frame 01 = tư thế nghỉ; KHÔNG khói/hiệu ứng bake vào frame (code phun runtime).
5. ✅ **Style chuẩn**: theo bộ Export_Train_UI_Package (burgundy `#8E1F3B` + đồng vàng `#D9A441`, gỗ nâu ấm, outline nâu đậm cartoon, dễ thương cho phụ nữ & trẻ em).
6. ✅ Giao đúng **TÊN FILE + THƯ MỤC** được đặt trong đơn, không thêm file phụ (`_single`, `@2x` tự ý...).

### Bổ sung của Lead cho đợt này (đã đo từ art đang ship, `production/art-handoff/STYLE_CONTRACT.md`)
- Outline **nâu ấm sẫm, TUYỆT ĐỐI KHÔNG ĐEN**: `#442510` → `#654129` (hue 15–46). Hue outline luôn ấm/đỏ hơn hue phần fill. Dày **1.5–2.5%** cạnh dài nhất.
- Hand-painted **semi-realistic game-icon**, gradient airbrush mềm liên tục. **KHÔNG** cel-shading, **KHÔNG** dải màu phẳng, **KHÔNG** pixel-art, **KHÔNG** dither.
- Có specular bóng rời rạc (blob sáng mềm) + inner shadow nhẹ phía trong viền.
- 2 file reference BẮT BUỘC `view_image` trước mỗi lần gọi image_gen:
  `Assets/Assetsgame/hatgiong/bapcai-removebg-preview.png` · `Assets/Assetsgame/hatgiong/cachualever3-removebg-preview.png`

---

## 🔴 ĐƠN 1 — SỬA LỖI "HEO VUI VẺ" (ưu tiên cao nhất)

> ⚠ **LEAD ĐÍNH CHÍNH (2026-09-01, sau khi kiểm asset thật):** bản đơn trước tôi ghi ngược.
> File asset tên là `Mèo vui vẻ.asset` **nhưng field `itemName` bên trong là "Heo Vui Vẻ"** —
> và `itemName` mới là thứ người chơi thấy trong shop. Vậy **stage 3 vẽ HEO là ĐÚNG**.
> Cái sai là **stage 2 vẽ MÈO**. Đừng vẽ lại stage 3.

**Lỗi:** bộ ảnh `ChatGPT Image 22_51_22 1 thg 9, 2026.png` (itemID 9, shop hiện **"Heo Vui Vẻ"**) tự mâu thuẫn giữa 2 ô:
- stage 2 = **MÈO trắng** ngồi trên bệ đá ❌
- stage 3 = **HEO hồng** đội vòng hoa cúc ✅ (đúng, giữ nguyên)

**Cần vẽ lại: 1 ô duy nhất — `stage_2.png`.**
- Nội dung: **CON HEO** của stage 3 nhưng ở trạng thái ĐANG XÂY DỞ — heo hồng ngồi trên bệ đá vuông, **CHƯA có vòng hoa** (vòng hoa cúc trắng nằm rời bên cạnh, dưới đất, đúng như stage 1 đang có), bệ đá chưa hoàn thiện (còn thô, chưa có hoa nhỏ quanh chân).
- **PHẢI cùng silhouette + cùng chiều cao + cùng baseline với stage 3** để game đổi stage không giật hình.
- Canvas **512 × 512**, vật chạm đáy canvas (pivot Bottom-Center), nền alpha 0.

**Giao vào:** `production/art-handoff/2026-09-02_Decor5Stage_Fix/heovuive/stage_2.png`

## 🟠 ĐƠN 2 — 4 BỘ 5-STAGE CÒN THIẾU

4 item này đã có trong shop nhưng **chưa có art 5 stage** nên đang giữ hành vi cũ (đặt xuống là hiện ngay, không có cảm giác xây dựng):

| itemID | Tên trong shop | Gợi ý nội dung |
|---|---|---|
| 3 | **Bảng hiệu** | biển gỗ treo trên 2 cột — ⚠ **BIỂN TRỐNG, KHÔNG CHỮ** (luật §1) |
| 7 | **Ghế Hoa** | băng ghế gỗ dài có giàn hoa leo phía sau |
| 8 | **Heo thần tài** | tượng heo vàng may mắn trên bệ đá (khác hẳn "Mèo vui vẻ") |
| 12 | **Vịt vui vẻ** | tượng/đàn vịt con trên bệ, cạnh vũng nước nhỏ |

**Mỗi item cần 5 ô, mỗi ô 512×512, alpha 0, pivot Bottom-Center, CÙNG canvas & CÙNG baseline cho cả 5:**
| ô | Nội dung |
|---|---|
| `stage_1.png` | **vật liệu rời** — các bộ phận chưa lắp + dụng cụ (búa, đinh, cưa, xô sơn) nằm dưới đất |
| `stage_2.png` | **đang xây nửa vời** — khung/thân đã dựng, chưa có phần trang trí, có thể kèm giàn giáo gỗ nhỏ |
| `stage_3.png` | **HOÀN THIỆN** — đây là hình hiện vĩnh viễn trong game, đẹp nhất, đầy đủ hoa/chi tiết |
| `stage_4.png` | **HỘP QUÀ ĐÓNG** — hộp quà vuông có nơ, tông màu riêng cho từng item (đừng 4 hộp giống nhau) |
| `stage_5.png` | **HỘP BUNG** — nắp hộp bay lên + vật hoàn thiện nhô ra khỏi hộp + confetti/tia sáng bung quanh |

⚠ **Quan trọng — nhìn 5 bộ đã làm để bám đúng nhịp tăng tiến:** `Assets/Art/Decor/Stages/gieng/` (giếng), `bunhin/` (bù nhìn), `coixaygio/` (cối xay gió), `xehoa/` (xe hoa), `rom/` (rơm). Chúng đã được cắt + xoá phông + căn baseline chuẩn, dùng làm mẫu.

⚠ **Có thể giao dạng 1 sheet 1536×1024** (grid 3 cột × 2 hàng, ô 512×512: hàng trên = stage 1/2/3, hàng dưới = stage 4/5 + 1 ô trống) **HOẶC 5 file PNG rời**. Lead xử lý được cả hai. **Nếu giao dạng sheet thì KHÔNG vẽ đường kẻ chia ô** (đợt trước có kẻ, Lead phải dò và cắt thủ công).

**Giao vào:** `production/art-handoff/2026-09-02_Decor5Stage_Fix/<slug>/` với slug lần lượt: `banghieu` · `ghehoa` · `heothantai` · `vitvuive`

---

## ⚪ ĐƠN 3 — 3 THỢ BÚA KHÁC NHAU + CHỚP MẮT (không gấp)

Hiện code đã chạy với **1 spritesheet thợ búa** duy nhất, 3 prefab chỉ khác nhau bằng cách **lật ngang + co nhỏ 6%** → mắt tinh sẽ thấy là 1 người lặp 3 lần.

**Cần: 2 spritesheet thợ nữa** (thợ #2 và thợ #3), khác nhau về **màu áo + kiểu mũ + dáng người**:
- Thợ #2: áo yếm **nâu đất**, mũ bảo hộ **trắng**, người **cao gầy**
- Thợ #3: áo yếm **xanh lá**, mũ bảo hộ **cam**, người **thấp đậm**, có râu

Cho mỗi thợ, cần **2 sheet**, đúng spec của sheet gốc đang dùng:
| Sheet | Kích thước | Grid | Nội dung |
|---|---|---|---|
| `worker0N_hammer_spritesheet.png` | **1200 × 896** | 4 cột × 3 hàng, ô 300 × 298.667 | 12 frame = 1 chu kỳ đập búa liên tục (0→11 rồi loop). **Frame 8/9/10 = búa chạm đất** (game bắn bụi + tiếng ở đúng 3 frame này) |
| `worker0N_celebrate_spritesheet.png` | **1200 × 896** | 4 cột × 3 hàng | 12 frame nhảy ăn mừng. **Frame 0 = đứng thẳng bình thường** (game dùng làm pose ĐỨNG IM lúc chờ mở hộp quà) |

⚠ Nhìn sheet gốc để bám: `Assets/Art/Characters/Worker/worker_hammer_spritesheet.png` + `worker_celebrate_spritesheet.png`.
⚠ Luật §4: mọi frame **CÙNG kích thước ô**, thân đứng yên cùng vị trí, không bake khói/bụi vào frame.

**Kèm (rất nhỏ, làm cùng lúc):** 4 file `char_0N_blink.png` còn nợ từ gói UI Juice — y hệt hình master từng nhân vật, **chỉ MẮT NHẮM**, 512×512 cùng vị trí. Giao vào `production/art-handoff/2026-09-01_UI_Juice/characters/char_0N/`.

**Giao vào:** `production/art-handoff/2026-09-02_Worker_Variants/`

---

## ✅ NGHIỆM THU — LEAD SẼ KIỂM BẰNG SỐ, KHÔNG KIỂM CẢM TÍNH

Mọi file giao về sẽ bị đo tự động, đạt hết mới nhận:
| Tiêu chí | Ngưỡng |
|---|---|
| Viền trắng ở rìa vật (pixel RGB ≥ 243) | **< 1.0%** (5 bộ đã làm đạt 0.06%) |
| Alpha trong suốt | phải **có thật** (đợt trước 15/15 file alpha = 0.00% → Lead phải tự xoá phông) |
| 5 stage cùng slug | **CÙNG kích thước canvas** (lệch là giật hình khi đổi stage) |
| Baseline (đáy vật) giữa 5 stage | **lệch ≤ 2px** |
| Outline hue | **15–46** (nâu ấm), không đen |
| Text trong ảnh | **0** |
| Bóng đổ bake vào ảnh | **0** |
| Chiều cao stage 1 → 3 | **tăng đơn điệu** (đang xây thì phải cao dần lên) |

Đạt → Lead gắn vào game bằng `Tools/Farm Game/Decor 5 Stage/★ Nạp art 5 stage (APPLY)`.
Không đạt → Lead gửi lại đúng file nào sai, sai chỉ số nào.

---

## 🟠 ĐƠN 4 — VẼ LẠI 2 NHÂN VẬT POPUP LÊN CẤP (bổ sung 2026-09-02, theo lệnh Sếp)

Popup LÊN CẤP có 4 nhân vật chibi ở 4 góc, nhưng **2 bên đang lệch style** ("đầu rồng đuôi rắn"):
- Bên TRÁI (style A — CHUẨN, giữ nguyên): `Assets/Art/UI/LevelUpV2/characters/char_01/char_01_master.png` (bé trai nông dân áo caro đỏ) + `char_02` (bé gái đầu bếp mũ trắng-đỏ) — viền dày, màu bão hòa, mắt to có highlight trắng, má hồng đậm.
- Bên PHẢI (style B — pastel mềm, LỆCH, cần vẽ lại): `char_03` (bé gái du lịch mũ tai bèo + máy ảnh) + `char_04` (bé gái thám hiểm mũ cối) — viền mảnh, màu nhạt, mắt chấm.

**Cần vẽ: 4 file, vẽ lại char_03 + char_04 THEO STYLE BÊN TRÁI (style A):**
| File | Nội dung |
|---|---|
| `char_03_master.png` | bé gái du lịch (giữ nhân dạng: mũ tai bèo + máy ảnh) — render đúng chất char_01: viền nâu đậm 6-8px @512, màu bão hòa, mắt to highlight trắng |
| `char_03_blink.png` | Y HỆT master, chỉ MẮT NHẮM (cùng pose, cùng vị trí từng pixel) |
| `char_04_master.png` | bé gái thám hiểm (mũ cối) — cùng chất trên |
| `char_04_blink.png` | Y HỆT master, chỉ MẮT NHẮM |

Spec: **512×512**, nền alpha 0, bố cục bán thân (đầu+vai) chiếm ~80% khung như char_01, hiển thị thực tế 230px. Tuân toàn bộ LUẬT ART STUDIO ở đầu file.
**Giao vào:** ghi đè đúng 4 đường dẫn `Assets/Art/UI/LevelUpV2/characters/char_0N/` — slot đã trỏ sẵn, không cần bấm gì thêm trong Unity.

---

## ⚪ ĐƠN 5 — BỘ VỆT CHUYỂN ĐỘNG CHO XE CỘ (bổ sung 2026-09-03, không gấp)

Bối cảnh: code đã tự làm **afterimage/bóng ma** cho mọi nhân vật + xe cộ (bóng mờ của chính sprite, mờ dần). Với NHÂN VẬT thế là đẹp. Với XE CỘ, art vẽ tay đẹp hơn nhiều — cần 3 bộ sau, mỗi frame **KHÔNG bake vật thể** (chỉ vệt hiệu ứng rời, game ghép sau đuôi xe lúc runtime):

| Bộ | Nội dung | Spec |
|---|---|---|
| `wake_boat_01..06.png` | **Vệt nước rẽ sóng** sau đuôi tàu thủy: bọt trắng-xanh hình chữ V mở dần rồi tan | 6 frame, canvas **512×256**, alpha 0, vệt chạy từ trái (đuôi tàu) sang phải, frame 6 tan gần hết |
| `steam_train_01..06.png` | **Cụm khói hơi nước** phụt sau ống khói tàu lửa khi chạy: cuộn tròn trắng-xám ấm, tan dần | 6 frame, canvas **256×256**, alpha 0, cuộn nở dần + bay nhẹ lên-trái |
| `speedline_01..04.png` | **Vệt gió tốc độ** chung (dùng cho phà + shipper chạy nhanh): 3-4 nét cong mảnh màu trắng mờ | 4 frame, canvas **256×128**, alpha 0, nét thon dần về đuôi |

Luật chung: tuân trọn **LUẬT ART STUDIO** đầu file (không text, không nền, không bóng đổ bake). Mọi frame cùng bộ = cùng canvas, cùng gốc toạ độ. Màu bám palette: bọt nước lấy xanh của `hoda/stage_3` (hồ đá), khói trắng-xám ấm không lẫn tím.
**Giao vào:** `production/art-handoff/2026-09-03_Motion_FX/<tên bộ>/`
Nghiệm thu như mọi đơn: alpha thật, viền sạch <1%, canvas đồng nhất. Giao xong Lead viết 1 emitter nhỏ gắn vào đuôi tàu/ống khói (code 30 phút, không cần Sếp chờ).
