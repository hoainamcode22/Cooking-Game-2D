# 📤 ĐƠN ĐẶT ART ĐỢT 2 — 2026-09-03 (sau nghiệm thu đợt 1)

> Sếp copy TOÀN BỘ file này dán cho GPT điều hành `agent-sprite-forge`.
> **Nghiệm thu đợt 1: đội vẽ đã sửa được lỗi NẶNG NHẤT** (tràn biên ô) — 7/7 file sạch. Cảm ơn đội.
> Đợt 2 chỉ còn 3 việc, trong đó **ĐƠN 8 đang CHẶN tích hợp**, ưu tiên làm trước.

---

## ✅ KẾT QUẢ NGHIỆM THU ĐỢT 1 (Lead đo bằng script, không đánh giá bằng mắt)

| Tiêu chí | Kết quả |
|---|---|
| Không tràn biên ô (lỗi chính đợt trước) | ✅ **7/7 ĐẠT** — cắt lưới không còn dính cán búa frame bên cạnh |
| Gutter an toàn ≥16px | ✅ 7/7 ĐẠT (thực đo 37–107px) |
| Canvas chia hết cho lưới | ✅ 6/7 — **trừ `worker_celebrate` (xem ĐƠN 8)** |
| Alpha nền sạch | ✅ 4/7 hoàn hảo 0px; 3 file còn 94–9058px alpha 1–32 = **viền khử răng cưa bình thường, CHẤP NHẬN** |
| `flowergirl` 900×1264 lưới 3×4 | ✅ ĐẠT |
| `char_01_master` + `char_01_blink` 512×512 | ✅ ĐẠT |
| Baseline chung | ❌ lệch 4–24px — **xem ĐƠN 9** |

> 📌 Lead tự đính chính: tiêu chí "bbox mọi frame lệch ≤8px" trong luật cũ là **SAI/quá cứng** đối với animation có đạo cụ vung ra (búa, tay giơ). Vì tool nay cắt **trọn ô lưới 300×300** (không cắt sát viền), bbox lệch **KHÔNG gây giật**. Bỏ tiêu chí đó. **Thay bằng 2 tiêu chí đúng: baseline chung + tâm ngang thân cố định.**

---

## 🔴 ĐƠN 8 — `worker_celebrate_spritesheet.png` CHƯA ĐƯỢC LÀM (đang CHẶN tích hợp)

Báo cáo bàn giao ghi file này đã chuẩn 1200×900, nhưng **file thực tế trên đĩa vẫn là 1200×896** và **ngày sửa vẫn là 12:17** (5 file worker kia đều 14:46). ⇒ Đội vẽ **quên file này**, chỉ cập nhật báo cáo.

**Hậu quả:** `896 ÷ 3 hàng = 298.67px` — không chia hết. Tool cắt của Lead nay **chặn cứng và bỏ qua sheet này**, nên **Worker 01 sẽ KHÔNG có animation ăn mừng** cho tới khi có bản mới.

**Yêu cầu:** vẽ lại đúng spec như 5 file kia —
- Canvas **1200×900**, lưới **4 cột × 3 hàng**, ô **300×300**
- CÙNG nhân vật Worker 01 ở cả 12 frame (mũ bảo hộ **vàng** luôn trên đầu, yếm **xanh dương**) — bản cũ có 1 frame vẽ nhầm người **tóc đen không đội mũ**, phải bỏ
- **XOÁ SẠCH** khói/bụi/tia sáng bake vào frame
- Chu kỳ ăn mừng 12 frame: nghỉ → giơ tay → nhảy → xoay vui → hạ → về nghỉ
- Áp **LUẬT BASELINE** ở ĐƠN 9 bên dưới
- **Giao:** `Assets/Art/Characters/Worker/worker_celebrate_spritesheet.png` (ghi đè)

---

## 🟡 ĐƠN 9 — CĂN BASELINE CHUNG cho 6 sheet worker + flowergirl

Đây là lỗi làm nhân vật **nhấp nhô lên xuống** khi chạy animation. Số đo hiện tại (khoảng cách từ **đáy bàn chân** tới **đáy ô**):

| Sheet | Baseline đo được | Yêu cầu |
|---|---|---|
| `worker_hammer` | 6..16 px (**lệch 10**) | mọi frame = **20px**, lệch ≤1px |
| `worker_celebrate` | 2..26 px (**lệch 24**) | (làm cùng ĐƠN 8) |
| `worker02_hammer` | 6..17 px (**lệch 11**) | 20px |
| `worker02_celebrate` | 6..10 px (lệch 4) | 20px |
| `worker03_hammer` | 6..17 px (**lệch 11**) | 20px |
| `worker03_celebrate` | 6..13 px (lệch 7) | 20px |
| `flowergirl_walk` | 2..10 px (lệch 8) | 20px |

**LUẬT BASELINE (bổ sung vào LUẬT LƯỚI SPRITESHEET):**
1. **Đáy bàn chân** của nhân vật cách **đáy ô** đúng **20px** ở MỌI frame, lệch tối đa **1px**.
2. **Ngoại lệ hợp lệ:** frame nhân vật đang **nhảy lên khỏi mặt đất** (celebrate) — lúc đó thân nâng lên là ĐÚNG. Nhưng frame nào **chân chạm đất** thì phải đúng 20px.
3. **Tâm ngang của THÂN NGƯỜI** (không tính búa/tay vươn) phải nằm giữa ô (x = 150 trong ô 300px), lệch ≤3px ở mọi frame.
4. Không cần vẽ lại nhân vật — **chỉ cần dịch chuyển nội dung trong ô** cho khớp baseline. Giữ nguyên nét vẽ.

**Giao:** ghi đè chính 7 file cũ, không đổi tên, không thêm file phụ.

---

## 🟢 ĐƠN 10 — BỘ DẤU TÍCH V (Sếp yêu cầu)

Lead đã kiểm kê toàn game: dấu tích V đang được vẽ bằng **4 cách khác nhau** ⇒ không đồng bộ.

| Nơi dùng | Hiện đang vẽ bằng | Kích thước |
|---|---|---|
| Chọn avatar (popup Hồ Sơ) | ký tự text "V" trong TMP | badge 22×22 |
| Quà điểm danh "đã nhận" | 2 hình chữ nhật xoay ghép lại bằng code | 44–50px |
| Nút xác nhận đặt công trình + FX xây xong | vẽ thủ tục bằng code (SDF) | 26–62px |
| Đơn hàng đã giao | ✅ sprite thật `UI_OrderBoard/ob_check.png` (128×128) — **ĐẸP, giữ nguyên** |
| Chọn ngôn ngữ (Cài đặt) | `Export_Train_UI_Package/check_badge_green.png` — file chỉ 281 byte, nét kém |

**Yêu cầu: vẽ 2 file**, nền alpha 0, **hình V màu TRẮNG tinh** (code sẽ tự tô màu theo ngữ cảnh — vẽ trắng là dùng lại được cho mọi nền):

1. **`check_thin.png`** — canvas **128×128**, dấu V nét **mảnh–vừa**, bo tròn 2 đầu nét, chiếm ~62% khung. Dùng cho badge nhỏ (22–62px).
2. **`check_bold.png`** — canvas **128×128**, dấu V nét **dày**, đầu nét bo tròn, chiếm ~70% khung, có **viền ngoài trắng dày 6px** kiểu sticker để nổi trên nền màu. Dùng cho huy hiệu to (44–50px).

Yêu cầu chung: nét V phải **cân, góc gãy dứt khoát**, đọc rõ khi thu xuống 22px. Không đổ bóng, không gradient, không nền tròn (nền tròn màu do code vẽ).

**Giao:** `Assets/_Game/Farm/Art/UI_Common/check_thin.png` · `Assets/_Game/Farm/Art/UI_Common/check_bold.png` (tạo thư mục mới)

---

## ⏳ CÒN NỢ TỪ ĐỢT 1 (chưa giao, nhắc lại)
- `char_02_master.png` + `char_02_blink.png` (512×512) — cô gái trẻ bím tóc, từ `NV_NPC/NVGAME/Processed/NV08`
- `char_03_master.png` + `char_03_blink.png` (512×512) — cậu bé da nâu áo vàng, từ `NV10`
- `char_04_master.png` + `char_04_blink.png` (512×512) — chú đàn ông có râu, từ `NV01`
  (char_01 từ NV06 đã giao ✅)
- `meovuive/stage_2.png` — vẽ lại thành HEO (ĐƠN 5 đợt 1)
- 4 bộ 5-stage: `banghieu`, `ghehoa`, `heothantai`, `vitvuive` (ĐƠN 6 đợt 1)

## ✅ NGHIỆM THU
Lead chạy script đo tự động: canvas chia hết · không tràn biên · gutter ≥16px · **baseline 20px lệch ≤1px** · tâm thân lệch ≤3px · alpha nền sạch · không bake FX. Sai mục nào trả lại đúng mục đó kèm số đo.
