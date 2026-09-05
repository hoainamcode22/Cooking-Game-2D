# ✅ BIÊN BẢN NGHIỆM THU ART ĐỢT 2 — 2026-09-03

Lead đo bằng script, không nghiệm thu bằng mắt thường. Backup bản đội vẽ giao: `production/backup_round8_2026-09-03/`

## KẾT QUẢ KỸ THUẬT — 7/7 ĐẠT ✅

| Sheet | Canvas | Chia hết | Tràn biên ô | Baseline chạm đất | Tâm thân |
|---|---|---|---|---|---|
| worker_hammer | 1200×900 | ✅ 300×300 | ✅ không | ✅ 20px (12/12 frame) | ✅ lệch 1px |
| worker_celebrate | 1200×900 | ✅ 300×300 | ✅ không | ✅ 20px | ✅ lệch 1px |
| worker02_hammer | 1200×900 | ✅ 300×300 | ✅ không | ✅ 20px (12/12) | ✅ lệch 1px |
| worker02_celebrate | 1200×900 | ✅ 300×300 | ✅ không | ✅ 20px | ✅ lệch 1px |
| worker03_hammer | 1200×900 | ✅ 300×300 | ✅ không | ✅ 20px (12/12) | ✅ lệch 1px |
| worker03_celebrate | 1200×900 | ✅ 300×300 | ✅ không | ✅ 20px | ✅ lệch 1px |
| flowergirl_walk | 900×1264 | ✅ 300×316 | ✅ không | ✅ 20px | (lệch do đổi hướng + giỏ hoa — hợp lệ) |

**Đội vẽ làm rất tốt đợt này.** 3 sheet hammer đạt baseline **tuyệt đối 20px ở cả 12/12 frame, lệch = 0**.

## 2 VIỆC LEAD TỰ SỬA (không bắt đội vẽ chạy lại vòng nữa)

1. **`worker_celebrate` frame 3** (hàng 0, cột 2): baseline 14px — nhân vật **lún 6px** xuống dưới đường chuẩn. Lead dịch nội dung ô lên 6px (khoảng trống phía trên 8px, đủ chỗ, không cắt đỉnh đầu).
2. **`flowergirl_walk` cả 12 frame**: đội vẽ **chưa áp luật baseline** cho sheet này — baseline rải 2..10px, lệch 8px ⇒ cô gái sẽ nhấp nhô khi đi. Lead chuẩn hoá toàn bộ 12 frame về đúng 20px (khoảng trống phía trên 41–49px, thừa chỗ).

Cả 2 việc chỉ **dịch pixel trong ô**, không vẽ lại, không đụng nét.

## ⚠️ 1 ĐIỂM NỘI DUNG CHƯA XỬ LÝ — CẦN SẾP QUYẾT

**`worker_celebrate` frame 9** (hàng 2, cột 0) vẫn vẽ **nhân vật tóc đen KHÔNG đội mũ, mũ bay ra ngoài** — đúng chỗ Lead đã nêu ở ĐƠN 8 đợt 1.

11 frame còn lại đều đội mũ vàng. Frame 10-11-12 ngay sau đó **mũ lại trở về trên đầu** ⇒ khi chạy animation sẽ thấy mũ **nhấp nháy biến mất 1 frame rồi hiện lại**.

Hai hướng, Sếp chọn:
- **(A) Sửa cho nhất quán** — vẽ lại frame 9 có đội mũ như 11 frame kia. Đơn giản, an toàn.
- **(B) Giữ ý tưởng "tung mũ ăn mừng"** — nhưng phải vẽ lại **frame 10, 11, 12 cũng không đội mũ** và có mũ đang rơi xuống, để thành một chuỗi liền mạch. Sinh động hơn nhưng tốn thêm 3 frame.

Ngoài ra còn **vệt khói/bụi bake vào frame** ở 1-2 ô (vi phạm LUẬT ART #4 — hiệu ứng phải do code phun runtime). Mức độ nhẹ, không chặn tích hợp.

## KẾT LUẬN
**CHO PHÉP TÍCH HỢP.** Điểm frame 9 là lỗi thị giác nhỏ, không chặn — sửa ở đợt sau.
