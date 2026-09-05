# 🔍 NGHIỆM THU ĐỢT 2 — `guide_talk_01..12` (04/09/2026)

> Người kiểm: Tech Lead · Phương pháp: **đo pixel + MD5 + soi ảnh**, không nghiệm thu bằng lời khai.
> Kết luận: **ĐẠT phần khung hình (lỗi nặng nhất đợt 1 đã sửa xong) — CHƯA ĐẠT phần nội dung animation.**
> Quyết định: **VẪN NẠP VÀO GAME NGAY** (tốt hơn hẳn bộ mượn), yêu cầu đợt 3 sửa 1 việc nhỏ.

---

## ✅ ĐÃ SỬA ĐÚNG — ghi nhận công đội vẽ

Hai lỗi chặn của đợt 1 đều **hết sạch**:

| Chỉ tiêu | Đợt 1 | Đợt 2 | |
|---|---|---|---|
| Viền ô lưới dính vào ảnh | 450px đặc kéo ngang y=179; 459px đặc kéo dọc x=30 | **bbox mờ = bbox đặc ở cả 12 file ⇒ 0 vệt** | ✅ |
| Khung hình (top / cao / rộng) | 179 / 460 / 451 — cận cảnh, **không có tay** | **52 / 588 / 308** — nửa người, có tay | ✅ |
| Mép trái thân | trôi | **x = 93 cố định TUYỆT ĐỐI ở cả 12 frame** | ✅ |
| Trùng khít bộ `wave`/`point` | không | `talk_01` bbox = `wave_01` bbox = **(93, 52, 401, 640)** | ✅ |
| Kích thước canvas | 512×640 | 512×640 cả 12 | ✅ |
| File phụ | có spritesheet 4.2MB | **không có file nào ngoài hợp đồng** | ✅ |

> Ghi chú: bảng của đội vẽ khai "tâm thân CenterX" chạy 246 → 291 (lệch 45px), nhìn qua tưởng thân trượt.
> Lead đã kiểm: đó là **tâm của bbox**, bị tay giơ ra kéo lệch. **Mép trái thân x=93 đứng yên tuyệt đối** ⇒ thân KHÔNG trượt. Đội vẽ làm đúng, chỉ đo nhầm chỉ số.

**⇒ Chuyển clip Talk ↔ Wave ↔ Point sẽ KHÔNG giật, KHÔNG đổi cỡ.** Đây là mục tiêu quan trọng nhất của đợt trả hàng và đã đạt.

---

## ❌ CHƯA ĐẠT — 12 file chỉ có 4 hình thật, và miệng không động

### Bằng chứng 1 — MD5 trùng nhau

```
a483b2098924 : frame 01, 12   ← hai file GIỐNG HỆT nhau từng byte
1a1ffbb502fd : frame 02, 11   ← hai file GIỐNG HỆT nhau từng byte
```

### Bằng chứng 2 — khác biệt giữa 2 frame liên tiếp

```
01 → 02 :  5.94%
02 → 03 :  7.92%
03 → 04 :  0.07%   ← đứng yên
04 → 05 :  1.94%
05 → 06 :  0.10%   ← đứng yên
06 → 07 :  0.11%   ← đứng yên
07 → 08 :  0.07%   ← đứng yên
08 → 09 :  1.94%
09 → 10 :  0.08%   ← đứng yên
10 → 11 :  7.87%
11 → 12 :  5.94%
12 → 01 :  0.00%   ← trùng khít
```

Gom lại: **12 frame = 4 nhóm**
`{01,12}` · `{02,11}` · `{03,04,09,10}` · `{05,06,07,08}`

### Bằng chứng 3 — thay đổi nằm ở TAY, không phải MIỆNG

Profile khác biệt theo hàng (`talk_01` vs `talk_05`):

```
y  40-189 :      0 px khác          ← đỉnh đầu → trán: bất động
y 190-339 :    321-517 px khác      ← vùng MẶT: gần như không đổi
y 390-589 :  3099-3729 px khác      ← vùng TAY: toàn bộ chuyển động dồn ở đây
```

Soi ảnh 4 tư thế cạnh nhau xác nhận: **cùng một nụ cười khép môi ở cả 4**, không mở miệng, không có "miệng tròn oh".

### Bảng báo cáo không khớp file

Đội vẽ khai `talk_03` = *"miệng mở phát âm"*, `talk_04` = *"cười tươi"*, `talk_05` = *"miệng tròn oh"*, `talk_08` = *"biểu cảm sinh động"*.
Thực tế `03` và `04` giống nhau **99.93%**, còn `05,06,07,08` là **cùng một hình**. Lần sau xin đội vẽ đối chiếu file trước khi điền bảng.

### Lỗi nhỏ khác
- Sợi tóc mái rủ xuống má phải **xuất hiện ở frame 02-05 nhưng không có ở frame 01** ⇒ lúc loop sẽ thấy nhấp nháy nhẹ.
- Lông mày trái ở `talk_05` vẽ thành một đốm tròn, trông hơi lỗi (ở cỡ hiển thị trong game thì khó thấy).

---

## 🚀 QUYẾT ĐỊNH CỦA LEAD — vẫn nạp vào game

Lý do: 4 tư thế tay này **tốt hơn hẳn** bộ `wave` đang mượn tạm (wave là vẫy tay chào, sai ngữ cảnh "đang giảng giải").
Thứ tự 4 nhóm tạo thành một chu kỳ **tay giơ lên → giữ → hạ về**, ở 12fps là ~1 giây/chu kỳ — nhìn hoàn toàn chấp nhận được.
Và quan trọng nhất: **khung hình đã chuẩn nên không giật khi đổi clip**.

Giữ `talkFps = 12`. Không cần sửa code — tool nạp thấy đủ 12 file thật sẽ **tự ghi đè bộ mượn wave**.

---

## 📋 YÊU CẦU ĐỢT 3 — chỉ 1 việc nhỏ, không vẽ lại từ đầu

**Chỉ cần vẽ lại phần MIỆNG.** Giữ nguyên 100% thân, tay, tóc, khung hình của đợt 2 — phần đó đã đúng.

Trên đúng 4 tư thế đã có, đổi khẩu hình thành 4 kiểu khác nhau rồi xuất lại 12 file theo thứ tự cũ:

| Nhóm hiện có | Frame | Khẩu hình cần vẽ |
|---|---|---|
| `{01,12}` | 01, 12 | **Miệng khép**, cười nhẹ (tư thế nghỉ — giữ nguyên như hiện tại) |
| `{02,11}` | 02, 11 | **Hé mở nhỏ** — âm "m / b" |
| `{03,04,09,10}` | 03,04,09,10 | **Mở vừa, môi ngang** — âm "a / e" |
| `{05,06,07,08}` | 05,06,07,08 | **Mở tròn** — âm "o / u" |

Thêm 2 sửa vặt (nếu tiện):
- Cho **sợi tóc mái rủ má phải xuất hiện ở CẢ 12 frame** (hoặc bỏ hẳn ở cả 12) để hết nhấp nháy khi loop.
- Sửa lông mày trái của `talk_05` (đang là đốm tròn).

Đợt 3 giao lại vào đúng thư mục cũ → Sếp bấm lại nút nạp → tự thay. Trong lúc chờ, game đã có bộ đợt 2 chạy.

---

## 🔴 CẦN BẠN

Mở `SCN_Farm.unity`, Ctrl+R sạch, rồi:
`Tools ▸ Farm Game ▸ Tutorial V2 ▸ ★ Nạp art NPC + VFX từ art-handoff (1 nút)` → **Ctrl+S**

Console phải ra `NPC: talk 12/12 · wave 12/12 · point 12/12` và **KHÔNG còn dòng `⚠ TẠM MƯỢN`** — đó là dấu hiệu art thật đã thay bộ mượn.
