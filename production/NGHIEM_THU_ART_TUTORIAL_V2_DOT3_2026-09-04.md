# ✅ NGHIỆM THU ĐỢT 3 — `guide_talk_01..12` · **DUYỆT, ĐƯA VÀO GAME**

> Người kiểm: Tech Lead · Phương pháp: đo pixel + MD5 + quét màu + soi ở **đúng cỡ hiển thị trong game**.
> Kết luận: **ĐẠT — nạp vào game.** Còn 2 lỗi nhỏ, ghi nợ cho đợt polish art cuối, **không chặn tiến độ**.

---

## ✅ ĐẠT — đội vẽ sửa đúng cả 2 việc được giao

### 1. Miệng ĐÃ ĐỘNG THẬT
Khác biệt vùng miệng (crop quanh tâm 302,331) so với `talk_01`:

| Đợt 2 | Đợt 3 |
|---|---|
| **0 px** (miệng bất động hoàn toàn) | **1125 – 1195 px** (≈10.5% vùng miệng) |

### 2. Bốn khẩu hình KHÁC NHAU THẬT — kiểm chéo từng cặp

```
              N1(01,12)  N2(02,11)  N3(03,04,09,10)  N4(05,06,07,08)
N1                    0       1195             1133             1125
N2                 1195          0             1597             1503
N3                 1133       1597                0              944
N4                 1125       1503              944                0
```
Mọi cặp đều > 900 px ⇒ 4 khẩu hình phân biệt rõ (khép · hé · mở ngang thấy răng+lưỡi · mở tròn). Soi ảnh xác nhận.

### 3. Lông mày — sửa xong
Chênh lệch vùng lông mày giữa mọi frame và `talk_01`: **0 px**. Hết đốm tròn ở `talk_05`.

### 4. Khung hình — giữ nguyên hoàn hảo (quan trọng nhất)
Cả 12 file: **512×640**, bbox đặc bắt đầu **x = 93, y = 52** — khớp tuyệt đối `guide_wave_01`.
⇒ Chuyển clip Talk ↔ Wave ↔ Point **không giật, không đổi cỡ**. Không có file phụ.

### 5. Bốn nhóm tư thế đúng thiết kế
MD5 xác nhận: `{01,12}` · `{02,11}` · `{03,04,09,10}` · `{05,06,07,08}` — đúng như Lead chỉ định, không phải lỗi trùng file.

---

## ⚠️ HAI LỖI NHỎ — GHI NỢ, KHÔNG CHẶN

### Nợ 1 — Khẩu hình dán trên mảng màu lệch tông da

Quét ngang `talk_05` tại y = 320:
```
x 248→284 : RGB(250,183,150)   ← da mặt
x 290→314 : RGB(252,222,196)   ← NHẢY: sáng hơn +39 (G), +46 (B)
x 320→344 : RGB(251,188,152)   ← về lại da mặt
```
Ranh giới đổi màu chỉ trong 6 px, không hoà chuyển ⇒ một **mảng bầu dục sáng** quanh miệng, như miếng dán. Kèm theo: **nét môi gốc chưa xoá, vẫn ló ra bên phải** miệng mới, và miệng mới đặt hơi lệch trái so với trục mũi.

**Vì sao vẫn duyệt:** Lead đã dựng ảnh ở **đúng cỡ hiển thị trong game** (NPC 300×375, thu từ 640 → 375, tỉ lệ 0.586). Ở cỡ đó mảng vá **gần như vô hình**, 4 khẩu hình vẫn phân biệt rõ. Lỗi chỉ lộ khi phóng to 3×.

**Có cách vá nhanh:** mảng vá là màu phẳng đồng nhất `(252,222,196)`. Lead đã thử thay bằng màu da `(250,185,151)` trên `talk_05` — **661 px/file**, đường quét trở lại mượt (183→188→185→188→192), nét miệng đen/hồng giữ nguyên. Áp cho cả 12 file mất ~1 phút, có backup. Sếp muốn thì bảo một câu.

### Nợ 2 — Sợi tóc mái má phải vẫn nhấp nháy (yêu cầu đợt 3 chưa làm)

Chênh lệch vùng tóc má phải so với `talk_01`:
```
talk_01 :    0 px      ← KHÔNG có sợi tóc
talk_02 : 2031 px      ← CÓ sợi tóc
talk_03 :  896 px      ← CÓ
talk_05 :  967 px      ← CÓ
```
Chu kỳ chạy `1,2,3,3,4,4,4,4,3,3,2,1` ⇒ sợi tóc **biến mất 2/12 giây mỗi vòng lặp** → nhấp nháy nhẹ.
Sửa: cho sợi tóc xuất hiện ở **cả 12 frame**, hoặc bỏ hẳn ở cả 12. Gộp vào đợt polish art cuối.

---

## 📊 TIẾN TRIỂN QUA 3 ĐỢT

| Chỉ tiêu | Đợt 1 | Đợt 2 | Đợt 3 |
|---|:---:|:---:|:---:|
| Kích thước 512×640 | ✅ | ✅ | ✅ |
| Sạch viền ô lưới | ❌ 450px đặc | ✅ | ✅ |
| Khung hình khớp wave/point | ❌ lệch 1.5× | ✅ | ✅ |
| Có tay để khua | ❌ | ✅ | ✅ |
| Miệng động | ❌ | ❌ 0 px | ✅ ~1130 px |
| 4 khẩu hình phân biệt | ❌ | ❌ | ✅ mọi cặp >900px |
| Lông mày sạch | — | ❌ đốm tròn | ✅ 0 px lệch |
| Tóc mái không nhấp nháy | — | ❌ | ❌ (nợ) |
| Khẩu hình hoà tông da | — | — | ❌ (nợ) |

---

## 🔴 CẦN BẠN — nạp vào game

Mở `SCN_Farm.unity` → **Ctrl+R** (Console 0 lỗi đỏ) → rồi:

`Tools ▸ Farm Game ▸ Tutorial V2 ▸ ★ Nạp art NPC + VFX từ art-handoff (1 nút)` → **Ctrl+S**

Console phải ra `NPC: talk 12/12 · wave 12/12 · point 12/12` và **KHÔNG còn dòng `⚠ TẠM MƯỢN`**
— đó là dấu hiệu art thật đã thay bộ mượn `wave`.

Play thử: cô hướng dẫn viên đứng bên trái card, **khua tay + mấp máy miệng + chớp mắt**, chuyển clip không giật.
