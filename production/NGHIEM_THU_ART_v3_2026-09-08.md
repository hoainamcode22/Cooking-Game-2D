# NGHIỆM THU ART v3 — 08/09/2026
Đo độc lập, không lấy số đội vẽ khai. Mọi con số dưới đây em đo lại từ file `.png` thật.

## BẢNG TỔNG

| # | Hạng mục | Đội vẽ khai | Em đo được | Kết luận |
|---|---|---|---|---|
| 1 | Hàng 6 vách (ô 40–47) | vẽ lại 100% | vân đá thật, 947–2630 màu/ô, pixel đá 57–98% | **ĐẠT** ✅ |
| 2 | Độ sáng đá vách | ΔE 0.58 | **ΔE 0.68** (hàng 3–5) · 1.85 (toàn sheet) | **ĐẠT** ✅ |
| 3a | xor hình học 48 ô cát | **xor = 0** | **xor = 724 px, lệch 32/48 ô** | **KHAI SAI** — nhưng **chấp nhận được** ⚠️ |
| 3b | Tường hông cát 14 px | lệch 0 px | **trùng khít 100% với cỏ** | **ĐẠT TUYỆT ĐỐI** ✅ |
| 3c | Màu nước viền bờ | ΔE 0.73 | **ΔE 0.70** (`#0A7DA5`) | **ĐẠT** ✅ |
| — | *(không khai)* | — | **Nước + bọt vẽ xuyên giữa 11 ô cát mà map đang dùng** | ❌ **LỖI CHẶN** |

**Kết luận: 4 đạt, 1 khai sai nhưng bỏ qua được, 1 lỗi chặn phải sửa trước khi vào game.**

---

## ✅ ĐẠT — Việc 3b: tường hông. Đây là thứ chữa đúng gốc bệnh.

Profile alpha ô số 9, đo từng hàng pixel:

| | Hàng có pixel | Dải full-width | Đỉnh mặt đi | Đáy |
|---|---|---|---|---|
| CÁT **cũ** | 0..65 | 29..34 (6 px) | +75.0 | −77.3 |
| CÁT **v3** | **0..77** | **29..46 (18 px)** | **+75.0** | **−105.5** |
| CỎ (chuẩn) | **0..77** | **29..46 (18 px)** | **+75.0** | **−105.5** |

Trùng khít **từng hàng một** với cỏ. Em dựng lại vùng bãi biển bằng tile mới:
**bãi cát hết hẳn cảm giác nhô lên như cao nguyên**, giờ nằm phẳng với mặt biển và ngang bến tàu.
Xem `_qc_pier_v3_s.jpg` so với `_qc_pier.jpg`.

## ✅ ĐẠT — Việc 2 & Việc 1: vách núi

**Màu đá** (lọc riêng pixel xám sat < 0.30, bỏ cỏ và nước):
`#7A7264` L\*=48.38 so với đá gốc `#7B7364` L\*=48.75 → **ΔE 0.68**. Từ 10.01 về 0.68, đạt xa ngưỡng 2.0.

**Hàng 6** đã vẽ lại thật: ô 40 mép tràn có nước đổ, 41–42 vách hẻm có bụi nước dọc,
43 hồ chân thác hình bầu dục có bọt xoáy, 44–47 bốn bậc đá có nước trải mỏng.
Hết sạch xoắn ốc và sọc chéo. Xem `_qc_cliff_v3_full.jpg`.

⚠️ **Ghi chú nhỏ (không chặn):** nước *đứng* trong ô 40 và ô 43 là `#1788B0` / `#1687AF`,
**ΔE 3.6–4.0** so với biển — trong khi cả dự án đang dùng `#0A7DA5` (ΔE 0.70).
Chỗ hồ chân thác ráp vào tile `Suoi_Tilemap` sẽ thấy đường nối màu hơi lệch. Sửa 1 pass HSL là xong.

## ⚠️ KHAI SAI nhưng bỏ qua được — Việc 3a: xor không phải 0

Đội vẽ khai *"xor đúng bằng 0 pixel"*. Thực tế: **724 px lệch, 32/48 ô có lệch**.

Nhưng em kiểm tiếp thì thấy **không nguy hiểm**:
- **100% pixel lệch là "chỉ có ở CỎ, thiếu ở CÁT"** — cát là tập con của cỏ, **không có chỗ nào cát thò ra ngoài**.
- Lệch rải rác 7–42 px/ô ở mép chéo (pixel khử răng cưa), không thành mảng.
- Tổng 724 / 269.082 px đục = **0,269%**.

Không hở khe, không chồng lấn. **Chấp nhận, không bắt vẽ lại.** Nhưng ghi lại đây để lần sau
đội vẽ đừng khai số mà không đo.

---

## ❌ LỖI CHẶN — Nước và bọt sóng vẽ xuyên qua GIỮA ô cát đặc

### Hiện tượng
Đội vẽ thêm nước + bọt vào **16 ô** chứ không phải 1 hàng 8 ô như brief.
Trong 16 ô đó, **11 ô là ô map đang dùng để lát thân bãi biển**.

| Ô có nước | Số ô trên map dùng nó |
|---|---|
| **17** | **443 ô** ← ô cát đặc chính, chiếm **51%** cả bãi |
| 2 | 41 |
| 16 | 34 |
| 43 | 34 |
| 18 | 32 |
| 42 | 31 |
| 0 | 31 |
| 10 | 28 |
| 46 | 8 |
| 45 | 3 |
| 41 | 2 |
| **Tổng** | **687 / 862 ô = 79,7% bãi biển** |

### Đo vị trí vệt nước trong ô 17
549 px nước, nằm ở **hàng 29–60, cột 0–123** — tức là **một vệt xanh cắt ngang chính giữa
mặt đi**, không phải ở mép. **100% nằm trong mặt đi (hàng 0–63), 0 px nằm ở tường hông.**

### Hậu quả
**79,7% bãi biển sẽ có vệt nước xanh + bọt trắng chạy xuyên qua giữa cát**, thành một
lưới sọc chéo đều tăm tắp giữa bãi. Đã thấy rõ trong ảnh dựng lại `_qc_pier_v3_s.jpg`.

Ảnh đối chiếu CŨ / v3 từng ô: `_qc_sand_v3_LOI.jpg` — nhìn ô 17 là thấy ngay.

### Vì sao xảy ra
Brief v3 em viết *"1 hàng 8 ô cát ướt + bọt sóng"* nhưng **không chỉ rõ ô số mấy**.
Đội vẽ rải nước vào 16 ô nằm rải rác hàng 1, 2, 3 và 6 — trúng đúng ô 17 là ô lát nền chính.
Lỗi này một nửa là do brief của em chưa chốt số ô. Lần sau em sẽ ghi đích danh chỉ số ô.

---

## ⚠️ Ghi chú nhỏ thứ 2 — tường hông cát hơi nhạt

| | Mặt trên | Tường hông | Chênh L\* |
|---|---|---|---|
| CÁT v3 | `#EFD9B1` L\*=87.6 | `#D9C6A1` L\*=80.6 | **−7.0** |
| CỎ (chuẩn) | `#7AB716` L\*=68.1 | `#5E8E10` L\*=53.8 | **−14.3** |

Brief yêu cầu ~−15 L\*. Hiện chỉ −7, tức **độ tương phản vát cạnh bằng nửa của cỏ**.
Bề dày vẫn đọc được, nhưng nhạt hơn cỏ. Không chặn — sửa cùng lúc với lỗi chặn thì tiện.

---

## 📁 File & backup
Backup đầy đủ, khôi phục được bất cứ lúc nào:

| File | Nội dung |
|---|---|
| `backup_art_vong14/Sheet_IsoSand45_original.png` | cát trước khi có tường hông |
| `backup_art_vong14/Sheet_IsoCliff45_before_v3.png` | vách trước v3 |
| `backup_art_vong14/Sheet_IsoCliff45_v2.png` | vách v2 |

QC vòng này: `_qc_sand_v3_LOI.jpg` · `_qc_pier_v3_s.jpg` · `_qc_cliff_v3_full.jpg` ·
`_qc_cliff_v3_hang6.jpg` · `_qc_sand_vien.jpg`

*Chưa commit, chưa push, chưa sửa GitHub. Chưa sửa scene.*

---

## 🔧 2 hướng xử lý lỗi chặn — Sếp chọn

### Hướng A — Em tự gỡ vệt nước, giữ nguyên tường hông (~15 phút, đảo ngược được)
Ghép mặt đi của **ô cát cũ** (sạch, không nước) lên mặt đi của ô v3, **giữ nguyên tường hông
mới**. Chỉ đụng 11 ô map đang dùng. Kết quả: bãi cát đặc sạch sẽ + có bề dày.
Rủi ro thấp: thuần ghép ảnh, có backup, em kiểm bằng cách dựng lại bãi biển trước khi bàn giao.
Bộ ô cát ướt/bọt vẫn giữ ở các ô map không dùng để sau này Sếp cần thì có sẵn.

### Hướng B — Trả về đội vẽ (chậm hơn nhưng sạch hơn)
Brief v4 em đã viết sẵn: `BRIEF_ART_v4_2026-09-08.md`, ghi **đích danh chỉ số ô** để
không lặp lại lỗi này.

**Em đề xuất Hướng A** — vì phần khó (tường hông) đội vẽ đã làm đúng và đẹp, chỉ còn việc gỡ
vệt nước ra khỏi ô đặc, thuần cơ học. Xong rồi vẫn gửi brief v4 để họ vẽ bộ ô ướt tử tế.

---

# 🔧 ĐÃ THỰC HIỆN HƯỚNG A — 08/09/2026 (Sếp duyệt)

## Đã làm gì
Vá **11 ô** map đang dùng: `0 · 2 · 10 · 16 · 17 · 18 · 41 · 42 · 43 · 45 · 46`

- **Mặt đi**: lấy lại pixel từ `Sheet_IsoSand45_original.png`, **chỉ ở chỗ art cũ có alpha ≥ 200**
  (xem mục "lỗi em tự gây" bên dưới — đây là chỗ suýt hỏng).
- **Tường hông 14 px của v3**: giữ nguyên 100%.
- **Vệt xanh ăn xuống tường hông** (59–231 px/ô): thay bằng trung bình 10 pixel tường **sạch
  gần nhất cùng mặt trái/phải**, lặp tới khi hết — giữ nguyên vân và alpha.
- **Alpha tuyệt đối không đụng** → silhouette y nguyên bản đội vẽ.

## ⚠️ Một lỗi em tự gây ra và đã bắt được trước khi bàn giao
Lần vá đầu em chép RGB của art cũ ở **mọi** pixel có alpha > 16. Nhưng ở vùng mép, art cũ có
alpha rất thấp (17–199) và RGB ở đó là rác. Đè sang tile mới có alpha 255 → **sinh ra 329 pixel
đen** trên 6 ô (ô 16 nặng nhất: 174 px).

Em phát hiện khi nhìn ảnh QC thấy có vệt đen lạ, đo lại thì đúng là do mình.
**Đã khôi phục về backup v3 (md5 khớp) và vá lại** với luật chặt hơn: chỉ tin RGB cũ khi
**alpha cũ ≥ 200**; chỗ alpha cũ mờ thì giữ RGB của v3 rồi mới dọn màu xanh.

## Kiểm chứng sau khi vá lại (6 phép đo)

| # | Phép kiểm | Kết quả |
|---|---|---|
| 1 | Alpha có đổi không | **0 px** khác v3 ✅ |
| 2 | 37 ô không liên quan | **0/37 ô** bị đụng ✅ |
| 3 | Còn pixel xanh | **0 px** ✅ |
| 4 | Còn pixel đen mới sinh | **0 px** ✅ |
| 5 | Mặt đi khớp art cũ (vùng alpha cũ ≥ 200) | **0 px** lệch ✅ |
| 6 | Profile alpha 48 ô vs CỎ | lệch đúng 2 ô (23, 27), **lệch 1 hàng pixel** — |

Về mục 6: em đo cả trên **bản v3 gốc đội vẽ giao** thì cũng lệch **đúng 2 ô đó, đúng 1 px**.
Tức là **lỗi có sẵn, không phải do em vá**. Ô 23 và 27 map không dùng → bỏ qua.

Dựng lại bãi biển từ dữ liệu scene thật: **sạch vệt xanh, cát có bề dày, nằm phẳng với mặt biển
và ngang bến tàu.** Xem `_qc_pier_v3_FIXED_s.jpg`.

Viền bọt trắng quanh mép bãi vẫn còn — đó là **bọt vốn có trong art gốc** ở ô 0, 2, 16, 18.
Đúng, giữ lại.

## Còn giữ nguyên, không đụng
- 5 ô có nước mà map **không dùng**: `1 · 8 · 40 · 44 · 47`. Để nguyên làm bộ viền ướt tạm.
- **Tường hông cát vẫn hơi nhạt** (−7 L* thay vì −14.3 như cỏ) → Việc 2 brief v4.
- **Nước đứng ô 40 / 43 sheet vách** ΔE 3.6–4.0 → Việc 3 brief v4.
- **Mép tường hông hơi lởm chởm** ở các ô có viền cong (0, 2, 16, 18) — có sẵn trong v3,
  không phải do em. Ghi thêm vào brief v4 cho đội vẽ làm mượt.

## Backup — khôi phục 1 lệnh
| File | Là gì |
|---|---|
| `backup_art_vong14/Sheet_IsoSand45_v3_before_fix.png` | bản v3 đội vẽ giao (còn vệt xanh) |
| `backup_art_vong14/Sheet_IsoSand45_original.png` | bản gốc trước khi có tường hông |

Khôi phục: copy đè vào `Assets/maptitle/Map45Iso/Sheet_IsoSand45.png`.

*Chỉ sửa đúng 1 file `.png`. Không đụng `.meta`, không đụng scene, không đụng code.
Không commit, không push, không sửa GitHub.*
