# NGHIỆM THU BỘ ART SUỐI / VÁCH NÚI / THÁC — 08/09/2026
**Tech Lead** · Em không tin lời khai, em đo bằng script. Dưới đây là số đo thật.

Ảnh kèm: `_qc_water45.jpg` · `_qc_cliff45.jpg` · `_qc_waterfall45.jpg` · `_qc_ghep_thac.jpg`
Backup art gốc trước khi em sửa màu: `production/backup_art_vong14/`

---

## A. BẢNG ĐIỂM

| hạng mục | kết quả | ghi chú |
|---|---|---|
| Quy cách file (kích thước, chia ô, PPU, pivot) | ✅ **ĐẠT 100 %** | đo từng file, khớp brief từng con số |
| **Hình học hình thoi** | ✅ **ĐẠT — 0 pixel sai** | xem mục B, đây là hạng mục quan trọng nhất |
| Màu đá vách | ✅ **ĐẠT** ΔE 0,6 | không thấy khác |
| Cỏ trên vách | ✅ **ĐẠT** ΔE 1,2 vs cỏ nền | rất khó thấy |
| **Màu nước** | ❌ **KHÔNG ĐẠT** ΔE 6,7 vs biển | **em đã tự sửa** — mục C |
| Bố cục 48 miếng nước | ⚠️ **ĐẠT MỘT PHẦN** | hàng 4 sai nội dung — mục D |
| **Bộ vách núi** | ❌ **KHÔNG DÙNG ĐƯỢC làm Rule Tile** | mục E — đây là vấn đề nặng nhất |
| `RuleTile_IsoCliff45.asset` | ❌ **KHÔNG GIAO** | chỉ có RuleTile nước |
| Thác 8 khung | ⚠️ **DÙNG TẠM ĐƯỢC** | mép cắt vuông — mục F |
| File `.meta` kèm sẵn | ✅ **ĐẠT** | SpriteMode Multiple, grid đúng, không phải cắt tay |

---

## B. ✅ HÌNH HỌC — ĐẠT TUYỆT ĐỐI (khen thật)

Đây là hạng mục dễ hỏng nhất và họ làm đúng. Em chạy so khớp **mặt nạ alpha từng pixel**
giữa miếng đặc nhất của `Sheet_IsoWater45` và của `Sheet_IsoGrass45` (bộ đang chạy trong game):

```
GRASS  miếng đặc nhất  #7  vùng đục 5358 px  bbox 128 × 72
WATER  miếng đặc nhất  #7  vùng đục 5358 px  bbox 128 × 72
số pixel KHÁC NHAU  =  0
```

Cắt ngang mặt nạ thấy đúng hình thoi chuẩn: bề rộng theo hàng
`8 → 40 → 72 → 104 → 128 → 128 → 96 → 64 → 32 → 0`. Ghép vào nền game sẽ không hở khe.

Quy cách cũng đúng hết: Water `1024×480 / ô 128×80 / PPU 128 / pivot (0.5, 0.6)` ·
Cliff `1024×672 / ô 128×112 / PPU 128 / pivot (0.5, 0.714)` · Waterfall `512×1024 / 8 khung 128×512`.
`RuleTile_IsoWater45.asset` có đủ **48 luật**.

---

## C. ❌ MÀU NƯỚC LỆCH BIỂN — ĐÃ SỬA XONG

Đội vẽ khai *"nền nước gốc #0D81A9 lấy trực tiếp từ Texture_WaterBase45"*. **Pixel nói khác.**

| | màu trội thật | so với biển |
|---|---|---|
| Biển của game (`Texture_WaterBase45.png`) | `#0B7EA7` | — |
| Brief em đặt | `#0D81A9` | ΔE 1,0 ✓ |
| **Nước họ giao** | **`#168FB8`** | **ΔE 6,7 ❌** |

Thang ΔE: `<1` mắt không thấy · `2–3,5` thấy khi để cạnh nhau · **`>5` thấy rõ**.
Nghĩa là chỗ suối đổ ra biển sẽ có **một đường ranh giới sáng hơn** — đúng thứ Sếp dặn phải tránh.

**Em đã sửa, không cần trả art về vẽ lại:** viết script chỉnh màu **chỉ trên pixel NƯỚC XANH**
(lọc theo sắc 175°–215° và độ bão hoà > 0,25), hạ độ sáng ×0,907 và nâng bão hoà ×1,06 —
đúng tỉ lệ đo được giữa `#168FB8` và `#0B7EA7`. Bọt trắng và cỏ bờ **không đụng tới**.

| file | trước | sau | ΔE với biển |
|---|---|---|---|
| `Sheet_IsoWater45.png` | `#168FB8` | **`#0B7EA6`** | **0,9** ✅ |
| `Sheet_Waterfall45.png` | `#168FB8` | **`#0B7EA6`** | **0,9** ✅ |

Kiểm lại sau khi sửa: **mặt nạ alpha vẫn khác 0 pixel** so với cỏ ⇒ hình học không suy suyển.
Bọt trắng sau khi sửa vẫn là `#D4EDF6` (vẫn trắng). Bản gốc nằm ở `production/backup_art_vong14/`.

---

## D. ⚠️ HÀNG 4 CỦA SHEET NƯỚC SAI NỘI DUNG

Brief hàng 4 = *"chỗ cạn thấy đá cuội đáy, bãi sỏi ướt, nước loang ra cỏ"*.
Giao về (miếng 24–31): **mảng xám trắng kẻ sọc chéo**, không có viên cuội nào, không phải nước cạn.
Nhìn như miếng nháp chưa vẽ. 8/48 miếng này hiện **không dùng được**.

Miếng 38–39 cũng lạ: hoa văn chấm bi tròn đều, không rõ để làm gì.

*Không chặn tiến độ* — 40 miếng còn lại đủ để vẽ suối. Nhưng nên yêu cầu vẽ lại 8 miếng này.

---

## E. ❌ BỘ VÁCH NÚI — ĐÂY LÀ VẤN ĐỀ NẶNG NHẤT

Brief yêu cầu 3 nhóm rõ rệt để dựng được Rule Tile và núi CAO:
- hàng 1–2: 4 cạnh thẳng + 4 góc lồi + 4 góc lõm
- hàng 3–4: **mặt vách đứng xếp chồng được** (để núi cao tuỳ ý)
- hàng 6: **rãnh spillway cho nước chảy qua**

Thực tế giao về: **40/48 miếng gần như y hệt nhau** — cùng một khối vuông mặt cỏ viền đá,
chỉ khác nhau vài pixel. Em nhìn miếng 8, 9, 10, 11 gần như không phân biệt được.

Hậu quả cụ thể:
1. **Không dựng được Rule Tile** — Rule Tile cần bộ cạnh/góc KHÁC NHAU rõ ràng để chọn theo hàng
   xóm. 40 miếng giống nhau thì luật nào cũng ra cùng một hình.
2. **Không dựng được núi cao** — thiếu hẳn miếng "mặt vách đứng" xếp chồng. Bộ này chỉ làm được
   bậc thềm cao 1 tầng, không ra được vách như trong video Sếp gửi.
3. **Hàng 6 (spillway) hỏng**: miếng 41–42 là tảng trắng/xám bè, miếng 43 là hình bầu dục kẻ sọc.
   Nhìn như lỗi xuất file, không phải rãnh nước.

Và **thiếu hẳn `RuleTile_IsoCliff45.asset`** — họ chỉ giao RuleTile cho nước.

---

## F. ⚠️ THÁC 8 KHUNG — DÙNG TẠM ĐƯỢC, CÓ MỘT LỖI RÕ

Được: 8 khung, vệt sáng chạy lệch pha thật, bọt trắng chân thác có, màu nay đã khớp biển.
Lỗi: **hai mép trái/phải của cột nước bị cắt vuông góc, alpha cứng**. Ghép thử
(`_qc_ghep_thac.jpg`) thấy rõ nó là **một thanh chữ nhật màu xanh dán lên vách**, không toè ra,
không có bụi nước ở mép, đỉnh cột cũng cắt ngang chứ không tan vào mép tràn.

Sửa được bằng 1 trong 2 cách, không cần vẽ lại toàn bộ:
- **(nhanh)** yêu cầu đội vẽ làm mềm alpha 6–10 px hai mép + thêm bụi nước mờ, giữ nguyên 8 khung;
- **(miễn phí)** dùng `VFX_CliffWater_Front.vfx` **đã có sẵn** trong project cho phần mép và bụi
  nước, chỉ dùng sheet này làm thân thác phía sau.

---

## G. 🎯 KẾT LUẬN & VIỆC CẦN LÀM

**Nhận:** `Sheet_IsoWater45.png` (đã sửa màu) + `RuleTile_IsoWater45.asset` + `Sheet_Waterfall45.png`
(đã sửa màu) — đủ dựng suối chảy ra biển ngay hôm nay.

**Trả về đội vẽ, 3 mục:**
1. **Vách núi vẽ lại phần lớn** — cần 4 cạnh + 4 góc lồi + 4 góc lõm KHÁC NHAU RÕ RỆT, và
   **8 miếng mặt vách đứng xếp chồng được**. Kèm `RuleTile_IsoCliff45.asset`.
2. **Hàng 4 sheet nước (miếng 24–31)** vẽ lại: nước cạn thấy cuội đáy, bãi sỏi ướt.
3. **Thác**: làm mềm alpha hai mép + bụi nước, giữ 8 khung.

**Về màu — nói lại với đội vẽ để lần sau không lặp:** đừng lấy màu bằng mắt từ ảnh tham chiếu.
Mở thẳng `Texture_WaterBase45.png`, hút màu bằng eyedropper, và **kiểm bằng ΔE ≤ 2** trước khi giao.
ΔE 6,7 là mắt thường thấy được ngay khi hai mảng nằm cạnh nhau.

---

## H. 🛠️ CÁCH SETUP CON THÁC (làm được ngay với art hiện có)

### Bước 1 — Import (Unity tự nhận, `.meta` đã kèm)
Mở Unity, chờ import. Kiểm nhanh 1 file: Sprite Editor của `Sheet_IsoWater45` phải hiện
**48 ô 128×80**, PPU 128, pivot Custom (0.5, 0.6). Nếu đúng thì bỏ qua bước cắt tay.

### Bước 2 — Tilemap cho suối
- `Water_Tilemap` **đã có sẵn** trong scene và **đang là BIỂN** (material `a5814331…`,
  sortingOrder **−8**, 4 tile biển). **Đừng vẽ đè lên nó.**
- Tạo tilemap MỚI: chuột phải `Grid_Iso45` ▸ 2D Object ▸ Tilemap ▸ Isometric.
  Đặt tên **`Suoi_Tilemap`**, `TilemapRenderer.sortingOrder = **−7**` (trên biển, dưới cỏ).
- ⚠️ Bắt buộc là **con của `Grid_Iso45`** thì mới khớp ô 300 × 150 world.
- Kéo `RuleTile_IsoWater45.asset` vào palette rồi vẽ dòng suối.

### Bước 3 — Cao nguyên
Với bộ vách hiện tại chỉ dựng được **thềm 1 tầng**. Tạo tilemap `VachNui_Tilemap`, order **+1**,
vẽ khối cao nguyên bằng các miếng hàng 1–2. Chờ bộ vách mới rồi mới dựng núi cao.

### Bước 4 — Thác
1. Tạo GameObject rỗng `ThacNuoc` tại mép cao nguyên.
2. Thêm `SpriteRenderer` + `SimpleSpriteAnimator` (**đã có sẵn**, em tách ra ở vòng 13e):
   `sprites` = 8 khung của `Sheet_Waterfall45`, `fps = 12`, **`destroyOnEnd = false`** (để lặp).
3. `sortingOrder`: trên vách, **dưới** mọi popup. Gợi ý **520**.
4. Muốn thác dài hơn: nhân bản, xếp chồng dọc, **lệch pha** bằng cách đặt `fps` chênh nhau
   một chút (12 và 11,5) — nếu cùng pha sẽ thấy rõ chỗ nối.

### Bước 5 — Bọt chân thác + tiếng nước
- Kéo `Day_Night/VFX/Water/VFX_CliffWater_Front.vfx` vào chân thác cho bụi nước.
- `AudioSource` 3D + `Day_Night/Audio/Ambience/Water flowing.wav`, bán kính ~1200 world (4 ô).

### Bước 6 — Chặn đặt công trình xuống nước (⚠️ đừng quên)
Không làm là người chơi đặt nhà giữa suối. Thêm ô của `Suoi_Tilemap` vào `occupiedCells` của
`PlacementManager` lúc khởi động — em làm được, Sếp gật là em code.

### Bước 7 — Thứ tự lớp (dưới → trên)
`Underwater_Tilemap (−9)` → `Water_Tilemap / biển (−8)` → **`Suoi_Tilemap (−7)`** →
`Tilemap_IsoGrass` → `VachNui_Tilemap (+1)` → công trình → `ThacNuoc (520)` → popup.
