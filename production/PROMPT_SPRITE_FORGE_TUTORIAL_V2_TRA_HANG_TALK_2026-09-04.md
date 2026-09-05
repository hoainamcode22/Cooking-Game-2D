# 🔁 TRẢ HÀNG — VẼ LẠI 12 FRAME `guide_talk_*` (04/09/2026)

> Gửi: **agent-sprite-forge**. Người kiểm: Tech Lead. Vòng 12, đợt trả hàng 1.
> **Nghiệm thu bằng đo pixel, không bằng mắt thường.** Số liệu dưới đây đo trực tiếp từ file đã giao.

---

## ✅ ĐÃ NHẬN — 35/47 file ĐẠT, cảm ơn đội vẽ

| Gói | Kết quả |
|---|---|
| `guide_wave_01..12` | ✅ ĐẠT — nền trong sạch, khung hình thống nhất |
| `guide_point_01..12` | ✅ ĐẠT — đúng ý "giữ tư thế chỉ ở đoạn đuôi" |
| `guide_blink` | ✅ ĐẠT về kỹ thuật (xem ghi chú §3) |
| **Gói B — 10 file VFX** | ✅ **ĐẠT TOÀN BỘ**, kích thước đúng từng file, nền trong 71–93% |

Nhân vật vẽ rất đúng brief: tóc nâu đuôi ngựa, yếm nâu, sơ mi kẻ burgundy, khăn vàng đồng, mắt to má hồng. Giữ nguyên tạo hình này.

---

## ❌ TRẢ LẠI — 12 file `guide_talk_01..12`

Đã chuyển vào `A_NPC_Guide/_TRA_LAI_DOI_VE_talk_2026-09-04/` để khỏi lẫn với hàng đạt.
**2 lỗi, cả 2 đều chặn không dùng được.**

### LỖI 1 — Viền ô lưới bị cắt DÍNH VÀO ẢNH (alpha đặc, không phải mờ)

Đo trên `guide_talk_01.png`:

```
hàng y=179 : 450 px đặc kéo NGANG hết ảnh   ← đường kẻ ngang của ô lưới
hàng y=180 : 451 px đặc
hàng y=185 : 451 px đặc
hàng y=190 : 451 px đặc
hàng y=200 :  11 px  ← hết vệt, bắt đầu vùng bình thường

cột x=28 :   0 px đặc
cột x=30 : 459 px đặc kéo DỌC hết ảnh       ← đường kẻ dọc của ô lưới
cột x=32 : 460 px đặc
cột x=40 :  24 px  ← hết vệt
```

⇒ Có một **khung chữ nhật xám bao quanh nhân vật**, dày ~2–20px, **alpha ĐẶC** (không phải mờ nên không tự mất khi nén). Vào game sẽ hiện thành cái khung viền quanh cô hướng dẫn viên.

Nguyên nhân: `guide_talk_sheet_expanded.png` là **2400×2100, ô 600×700**. Cắt lưới 600×700 rồi ép về 512×640 ⇒ lưới lệch ⇒ dao cắt ăn vào đúng đường kẻ ô.
3 bộ `wave`/`point`/`blink` (giao lúc 07:42) **không có lỗi này** — làm theo cách khác và đúng.

### LỖI 2 — SAI KHUNG HÌNH, lệch hẳn so với 3 bộ kia

Đo vùng thân thật (alpha > 200) của frame nghỉ từng bộ:

| Bộ | TOP (y bắt đầu) | CAO | RỘNG | Nội dung |
|---|---:|---:|---:|---|
| `guide_wave_01`  | 52 | 588 | **308** | nửa người, **có hai tay** |
| `guide_point_01` | 50 | 590 | **297** | nửa người, **có hai tay** |
| `guide_blink`    | 42 | 598 | **312** | nửa người, có hai tay |
| **`guide_talk_01`** | **179** | **460** | **451** | ❌ **chỉ đầu + vai, KHÔNG CÓ TAY** |

Hai hệ quả:
1. **Nhân vật phóng to ~1.5× và nhảy vị trí** mỗi lần code chuyển clip Talk ↔ Point/Wave. Người chơi thấy cô ấy giật nảy liên tục — mà Talk là clip dùng **~80% thời lượng tutorial**.
2. Clip Talk theo brief là *"miệng mấp máy, một tay khua nhẹ minh hoạ"* — **frame không có tay thì không khua được gì.**

---

## 🎯 YÊU CẦU VẼ LẠI — làm đúng 1 việc này thôi

Vẽ lại `guide_talk_01..12`, **giữ nguyên tạo hình nhân vật**, chỉ sửa khung hình + cách xuất:

### Khung hình — BẮT BUỘC khớp bộ `wave`/`point`

| Chỉ tiêu | Giá trị bắt buộc | Cách tự kiểm |
|---|---|---|
| Canvas | **512 × 640 px** | Properties |
| Thân bắt đầu từ | **y ≈ 50** (±8px) | mở `guide_wave_01.png` chồng lên, đỉnh đầu phải trùng |
| Chiều cao thân | **≈ 590 px** (±15px) | như trên |
| Chiều rộng thân | **≈ 300 px** (±20px) — **KHÔNG phải 451** | như trên |
| Khung hình | **nửa người, thấy rõ HAI TAY tới ngang hông** | phải khua tay được |
| Tâm thân theo trục X | **≈ 245** (±6px) trong cả 12 frame | thân không được trượt ngang |

> **Cách chắc ăn nhất: lấy thẳng `guide_wave_01.png` làm nền, chỉ vẽ lại phần tay + miệng.**
> Như vậy thân, đầu, tóc, yếm, tỉ lệ trùng khít 100% với 3 bộ kia — không thể sai.

### Nội dung 12 frame (giữ nguyên brief cũ)

- `guide_talk_01` = **tư thế nghỉ**, miệng khép hờ, tay xuôi tự nhiên — phải **trùng khít `guide_wave_01`** trừ miệng
- `guide_talk_02..06`: miệng mở dần rồi khép (chu kỳ nói 1), tay phải nâng ngang ngực khua nhẹ
- `guide_talk_07..12`: chu kỳ nói 2, tay hạ về; **frame 12 nối mượt ngược về frame 01**
- Biên độ tay **NHỎ** — nói chuyện bình thường, không diễn kịch

### Cách xuất — ĐỪNG cắt từ spritesheet nữa

❌ **KHÔNG** dựng sheet rồi cắt lưới — đó chính là nguồn gốc Lỗi 1.
✅ **Xuất trực tiếp 12 file PNG rời**, mỗi file một canvas 512×640 riêng, nền alpha 0.
✅ Không giao kèm file sheet nguồn (`*_sheet_*.png`) — tool nạp đã tự lọc bỏ, giao thừa chỉ tốn công.

---

## ✅ CHECKLIST TỰ KIỂM TRƯỚC KHI GIAO LẠI

- [ ] Mở chồng `guide_talk_01` lên `guide_wave_01` → **đỉnh đầu, vai, hông phải trùng khít**
- [ ] Zoom 400% soi 4 mép ảnh: **không còn đường kẻ ngang/dọc nào** chạy hết chiều ảnh
- [ ] Mở chồng `guide_talk_01` và `guide_talk_07` → **thân trùng khít**, chỉ tay/miệng lệch
- [ ] Cả 12 file đúng **512×640** (xem Properties, đừng tin mắt)
- [ ] **Thấy rõ hai tay** trong mọi frame
- [ ] Nền alpha 0, không bóng đổ, không chữ/số/logo

Xong hết mới nhắn Lead: **"đã giao lại talk"**.

---

## ⏱️ TRONG LÚC CHỜ — game vẫn chạy, không ai phải đợi ai

Lead đã nạp **25 file đạt + 10 file gói B** vào game. Tutorial chạy được ngay hôm nay.

Clip Talk **tạm mượn 12 frame của bộ `wave`** (Sếp chốt). Lý do chọn wave chứ không để trống:
nó có chuyển động thật VÀ **cùng khung hình** với point/blink, nên chuyển clip không giật.

👉 Đội vẽ **giao lại 12 file `guide_talk_01..12`** vào đúng thư mục cũ rồi báo Lead.
Bấm lại nút nạp là art thật **tự động thay bộ mượn** — không phải sửa code, không phải nhớ thao tác nào.

> 12 file hỏng đợt 1 và spritesheet nguồn đã được **xoá** theo lệnh Sếp. Số đo lỗi giữ nguyên trong tài liệu này để đối chiếu.
