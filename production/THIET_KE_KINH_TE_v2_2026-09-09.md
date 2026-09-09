# THIẾT KẾ KINH TẾ v2 — CẤP 1→50 (VÒNG 15, 09/09/2026)
Trạng thái: **ĐÃ ÁP DỤNG 09/09 (Sếp duyệt: đường cong vừa · shop OK, bỏ 3 máy · L31–50 em quyết).** Xem mục I. Mọi con số dưới đây em đo từ file thật của game
(23 cây, 38 món, 29 file quà, 42 mặt hàng shop, code EXP/đơn hàng/tàu). Sếp duyệt bảng là em
áp dụng một lượt: sửa code đường cong + trần cấp, viết lại 29 file quà, tạo mới 20 file L31–50,
đổi `unlockLevel` trong shop.

Hai việc đã sửa ngay trong vòng này (không cần duyệt vì là lỗi):
- 🔴 **Khung đen xám bay lên HUD** — đã sửa (mục F).
- ✅ Rà lại hiệu ứng bay: vàng/KC/EXP **phủ toàn game** (mục F), chỉ còn 1 lỗ: **vật phẩm** từ popup lên cấp và bảng đơn chưa bay vào Kho.

---

## A. CHẨN ĐOÁN — vì sao "lên cấp quá nhanh, tiền thì lúc thiếu lúc thừa"

### A1. EXP: đường cong quá ngắn, tốc độ kiếm quá đều
| Đo được | Giá trị |
|---|---|
| Công thức hiện tại | `40 + 10n + 3n²/20` (`PlayerProgressManager.cs:83`), trần **30** (`CapToiDa`, dòng 30) |
| Tổng EXP để đi hết L1→L30 | **6.822** |
| EXP/phút mỗi ô đất | **≈ 6,0 cho MỌI loại cây** — từ Lúa (L1) đến Dưa hấu (L12) đều 6,0. Không có lý do gì để trồng cây đắt |
| Tiền/phút mỗi ô | 9,6 (Lúa) → 19,0 (Dưa hấu) — tăng đúng, nhưng EXP thì không |
| Với 9 ô đất | ≈ 54 EXP/phút ⇒ **hết 30 cấp sau ~2 giờ trồng liên tục** |
| Mô phỏng chơi 45 phút/ngày | **L10: 0,4 ngày · L20: 1,2 ngày · L30: 2,2 ngày** |

Chưa kể đơn hàng (`EXP = vàng/8`, sàn 3), nấu ăn (3→180 EXP/món), tàu (10/lượt), khách du lịch, quầy bán.

### A2. Vàng: đầu game khan, giữa game ngập
| Đo được | Giá trị |
|---|---|
| Tổng vàng tặng khi lên cấp L2→30 | **35.990** |
| Tổng giá **toàn bộ** 42 mặt hàng shop | **26.480** |
| ⇒ | **Quà lên cấp một mình đã mua sạch shop và còn dư 9.500.** Đây là lạm phát. |
| L1–3, Lúa | lời 8 vàng / 50 giây / ô → mua Nhà Dân 1 (100) mất 12 phút với 1 ô. Cảm giác "khó kiếm" của Sếp là ở đoạn này |
| Shop mở khoá | **34/42 mặt hàng mở ngay L1–2**, chỉ 3 máy khoá ở L17/21/24 ⇒ không có gì để mong chờ |
| Chữ "mở khoá" trong popup lên cấp | **Nói sai**: L6 báo "Chuồng bò đã mở" nhưng shop khoá L8; L11 báo "Máy Xay Bột" nhưng shop khoá L17; L13 "Máy Ép Mía" vs L21; L15 "Máy Phô Mai" vs L24 |

### A3. Kim cương: có vào, không có ra
| Nguồn | 208 KC từ lên cấp + nhiệm vụ + tutorial |
|---|---|
| Chỗ tiêu | **đúng 1 mặt hàng** (Chậu Đá Quý 100 KC) + tăng tốc chờ |
| ⇒ | KC dồn không có mục đích. Người chơi không cần tiết kiệm cũng không cần chi. |

### A4. Quà lên cấp: lặp và vô hồn
- 29 cấp toàn hạt giống + nông sản. `seed_pepper` xuất hiện **8 lần**, `seed_sugarcane` 6 lần.
- **Sau L12 không còn cây mới** (Dưa hấu là cuối) ⇒ L13–30 quà chỉ là hạt cũ lặp lại.
- Không có vật liệu xây, không có món ăn, không có decor — tức không dùng đến shop / cooking.

---

## B. ĐƯỜNG CONG EXP MỚI — `30·L + 7·L²` (giữ L1→L2 = 40 cho tutorial)

| Lv | Cần (cũ) | Cần (mới) | Gấp | Tích luỹ mới |
|---|---|---|---|---|
| 1 | 40 | **40** | 1,0 | 40 |
| 2 | 50 | 88 | 1,8 | 128 |
| 5 | 82 | 325 | 4,0 | 838 |
| 10 | 142 | 1.000 | 7,0 | 4.348 |
| 15 | 209 | 2.025 | 9,7 | 12.283 |
| 20 | 284 | 3.400 | 12,0 | 26.393 |
| 25 | 366 | 5.125 | 14,0 | 48.428 |
| 30 | 456 | 7.200 | 15,8 | **80.138** |
| 35 | — | 9.625 | — | 123.273 |
| 40 | — | 12.400 | — | 179.583 |
| 45 | — | 15.525 | — | 250.818 |
| 50 | — | 19.000 | — | **338.728** |

**Mô phỏng 45 phút/ngày** (6 ô → 9 → 12 → 16 → 20 ô theo cấp):

| Mốc | Hiện tại | Đề xuất |
|---|---|---|
| L10 | 0,4 ngày | **1,8 ngày** |
| L20 | 1,2 ngày | **9 ngày** |
| L30 | 2,2 ngày | **23 ngày** |
| L40 | — | **44 ngày** |
| L50 | — | **77 ngày** (~2,5 tháng) |

Đây là nhịp chuẩn của farm game mobile: tuần đầu lên nhanh để gắn người chơi, tháng 1 tới L30,
tháng 2–3 mới chạm trần.

**Kèm 1 chỉnh nhỏ về nguồn EXP:** cho EXP/phút tăng nhẹ theo bậc cây (6,0 → 8,5 ở cây L10+),
để cây đắt có lý do tồn tại. Chỉ sửa `expReward` trong 10 asset cây cấp 7–12, không đụng code.

### Tương thích save (quan trọng)
`CurrentExp` là **EXP dư của cấp hiện tại**. Mốc mới ≥ mốc cũ ở mọi cấp, nên số dư cũ luôn
< mốc mới ⇒ **không ai bị lên vài cấp một lúc, không ai bị tụt cấp**. Vẫn bump `SaveVersion 1→2`
và kẹp `CurrentExp ≤ Req−1` một lần cho chắc. Người đang L30 giữ nguyên L30, tiếp tục lên 31.

---

## C. VÀNG & KIM CƯƠNG — biến quà lên cấp từ "thu nhập" thành "chào mừng"

| | Hiện tại (L2–30) | Đề xuất L2–30 | Đề xuất L2–50 |
|---|---|---|---|
| Vàng tặng | 35.990 | **22.410** (−38%) | 89.260 |
| KC tặng | 208 | **138** | 290 |
| Tổng giá shop | 26.480 | **≈ 60.000** (gating lại + 8 món cao cấp mới) | **≈ 210.000** (thêm nội dung L31–50) |

Nguyên tắc: vàng chính phải đến từ **chơi** (trồng, đơn, nấu), quà lên cấp chỉ đủ để **thử ngay
thứ vừa mở khoá**. Kim cương: mỗi cấp 2, mốc 5 cấp = 10, mốc 10 cấp = 12/20/30/40/60.

### Thêm chỗ tiêu kim cương (bắt buộc, không thì KC vô nghĩa)
| Mặt hàng KC mới | Giá | Mở ở |
|---|---|---|
| Đài phun nước | 80 | L12 |
| Cổng vườn hoa | 120 | L18 |
| Đèn pha lê | 150 | L24 |
| Tượng đá mini | 180 | L32 |
| Cây anh đào | 250 | L40 |
| Bộ ghế đá sân vườn | 300 | L48 |
| **Tổng sink KC** | **1.080** | > 290 KC tặng ⇒ KC luôn khan, phải chọn |

⚠ 6 món này **cần art** — em ghi vào brief đội vẽ nếu Sếp duyệt.

---

## D. SHOP — dựng lại cái thang mở khoá (và làm chữ trong popup nói thật)

| Mặt hàng | Hiện tại | Đề xuất | Lý do |
|---|---|---|---|
| Nhà Dân 1 · Đất Trồng · Chậu Đất · Bụi cỏ/hoa nền | L1 | L1 | Bộ khởi đầu |
| Chuồng Gà | L2 | L2 | Khớp quà L2 |
| Nhà Dân 2 | L1 | **L3** | Khớp chữ "Thêm 1 nhà dân" ở L3 |
| Chuồng Bò Sữa | L2 | **L4** | Khớp chữ L4 |
| Hoa Trắng/Đỏ/Vàng, Đá nhỏ/vừa, Khúc gỗ | L1 | **L4** | Decor bậc 1 sau khi ổn định |
| Chuồng Heo | L4 | **L6** | Khớp chữ L6 |
| Nhà Dân 3 | L1 | **L8** | 750 vàng + vật liệu — cần có tàu rồi |
| Chuồng Bò | L8 | L8 | Khớp |
| Đèn Tường, Thùng Gỗ, Đá Lớn, Bảng chỉ đường, Hoa Sen | L1–2 | **L9** | Decor bậc 2 |
| Máy Xay Bột | L17 | **L11** | Khớp chữ L11 |
| Nhà Dân 4 · Cột Đèn · Chậu Cây Cảnh · Đèn Lồng | L1–2 | **L12** | Bậc vật liệu đinh |
| Máy Ép Mía | L21 | **L13** | Khớp chữ L13 |
| Máy Phô Mai | L24 | **L15** | Khớp chữ L15 |
| Nhà Dân 5 · Cây Thông · Bụi Cây Lớn | L1–3 | **L16** | Bậc vật liệu kính |
| **8 decor cao cấp mới** (giá 3.000–12.000) | — | **L20 / 25 / 30 / 35 / 40 / 45 / 50** | Sink vàng dài hạn ⚠ cần art |

---

## E. BẢNG QUÀ LÊN CẤP L2→L50 — khuôn cố định 6 ô, mỗi ô một nguồn

Popup hiện tối đa 6 ô. Khuôn: **Ô1 Vàng · Ô2 Kim cương · Ô3 Hạt giống của cây VỪA mở (farm) ·
Ô4 Vật liệu tàu (gỗ/đá/đinh/kính) · Ô5 Món/nguyên liệu của công thức vừa mở (cooking) ·
Ô6 Decor thật từ shop (chỉ ở mốc 5 cấp)**. Người chơi nhìn quà là biết vừa mở gì.

| Lv | EXP để lên cấp kế | Vàng | KC | Ô3 · Hạt giống (farm) | Ô4 · Vật liệu (tàu) | Ô5 · Nấu ăn (cooking) | Ô6 · Decor (shop) |
|---|---|---|---|---|---|---|---|
| 2 | 88 | 100 | 2 | hạt Ngô ×4 | gỗ 3 · đá 2 | Súp ngô ×1 (món mới) | — |
| 3 | 153 | 135 | 2 | hạt Cà rốt · Cà chua ×4 | gỗ 4 · đá 2 | Salad bắp cải ×1 (món mới) | — |
| 4 | 232 | 170 | 2 | hạt Hoa hồng · Oải hương ×4 | gỗ 4 · đá 3 | Gà xào bắp ×1 (món mới) | — |
| 5 | 325 | 205 | 10 | hạt Khoai tây ×4 | gỗ 4 · đá 3 | Khoai tây chiên · Bánh ngô ×1 (món mới) | Hoa Trắng |
| 6 | 432 | 240 | 2 | hạt Nấm ×4 | gỗ 5 · đá 3 | Canh khoai tây · Bắp cải xào ×1 (món mới) | — |
| 7 | 553 | 275 | 2 | hạt Hoa lan · Cúc trắng · Mía ×4 | gỗ 5 · đá 3 | Gà nướng · Thịt heo luộc ×1 (món mới) | — |
| 8 | 688 | 310 | 2 | hạt Chanh ×4 | gỗ 5 · đá 4 | Bò hầm cà rốt · Trứng ốp la ×1 (món mới) | — |
| 9 | 837 | 345 | 2 | hạt Tulip · Cúc vạn thọ · Ớt ×4 | gỗ 6 · đá 4 | Phở bò · Sườn heo ×1 (món mới) | — |
| 10 | 1,000 | 380 | 12 | hạt Tiêu · Mẫu đơn · Cẩm tú cầu · Anh thảo ×4 | gỗ 6 · đá 4 | Bò xào tiêu ×1 (món mới) | Đèn Tường |
| 11 | 1,177 | 400 | 2 | hạt Bí đỏ ×4 | gỗ 7 · đá 4 · đinh 2 | Bánh bí đỏ ×1 (món mới) | — |
| 12 | 1,368 | 455 | 2 | hạt Dưa hấu ×4 | gỗ 8 · đá 5 · đinh 3 | Nước ép dưa ×1 (món mới) | — |
| 13 | 1,573 | 510 | 2 | hạt cây cao nhất đang có ×3 | gỗ 9 · đá 5 · đinh 3 | Dưa hấu trộn ×1 (món mới) | — |
| 14 | 1,792 | 565 | 2 | hạt cây cao nhất đang có ×3 | gỗ 10 · đá 6 · đinh 4 | Súp bí đỏ ×1 (món mới) | — |
| 15 | 2,025 | 620 | 10 | hạt ⚠ CÂY MỚI #1 (cần art) ×4 | gỗ 11 · đá 6 · đinh 4 | Dưa hấu dầm ×1 (món mới) | Thùng Gỗ |
| 16 | 2,272 | 675 | 2 | hạt cây cao nhất đang có ×3 | gỗ 12 · đá 7 · đinh 5 | nguyên liệu nấu ×3 | — |
| 17 | 2,533 | 730 | 2 | hạt cây cao nhất đang có ×3 | gỗ 13 · đá 7 · đinh 5 | Canh bí đỏ ×1 (món mới) | — |
| 18 | 2,808 | 785 | 2 | hạt cây cao nhất đang có ×3 | gỗ 14 · đá 8 · đinh 6 | nguyên liệu nấu ×3 | — |
| 19 | 3,097 | 840 | 2 | hạt cây cao nhất đang có ×3 | gỗ 15 · đá 8 · đinh 6 | nguyên liệu nấu ×3 | — |
| 20 | 3,400 | 895 | 20 | hạt ⚠ CÂY MỚI #2 ×4 | gỗ 16 · đá 9 · đinh 7 | Gà hầm nấm ×1 (món mới) | Đèn Lồng |
| 21 | 3,717 | 950 | 2 | hạt cây cao nhất đang có ×3 | gỗ 17 · đá 9 · đinh 7 · kính 2 | nguyên liệu nấu ×3 | — |
| 22 | 4,048 | 1,045 | 2 | hạt cây cao nhất đang có ×3 | gỗ 18 · đá 10 · đinh 8 · kính 3 | Chè ngô sữa ×1 (món mới) | — |
| 23 | 4,393 | 1,140 | 2 | hạt cây cao nhất đang có ×3 | gỗ 19 · đá 10 · đinh 8 · kính 3 | nguyên liệu nấu ×3 | — |
| 24 | 4,752 | 1,235 | 2 | hạt cây cao nhất đang có ×3 | gỗ 20 · đá 11 · đinh 9 · kính 4 | nguyên liệu nấu ×3 | — |
| 25 | 5,125 | 1,330 | 10 | hạt ⚠ CÂY MỚI #3 ×4 | gỗ 21 · đá 11 · đinh 9 · kính 4 | nguyên liệu nấu ×3 | Chậu Cây Cảnh |
| 26 | 5,512 | 1,425 | 2 | hạt cây cao nhất đang có ×3 | gỗ 22 · đá 12 · đinh 10 · kính 5 | Bò hầm bí ×1 (món mới) | — |
| 27 | 5,913 | 1,520 | 2 | hạt cây cao nhất đang có ×3 | gỗ 23 · đá 12 · đinh 10 · kính 5 | nguyên liệu nấu ×3 | — |
| 28 | 6,328 | 1,615 | 2 | hạt cây cao nhất đang có ×3 | gỗ 24 · đá 13 · đinh 11 · kính 6 | nguyên liệu nấu ×3 | — |
| 29 | 6,757 | 1,710 | 2 | hạt cây cao nhất đang có ×3 | gỗ 25 · đá 13 · đinh 11 · kính 6 | nguyên liệu nấu ×3 | — |
| 30 | 7,200 | 1,805 | 30 | hạt ⚠ CÂY MỚI #4 ×4 | gỗ 26 · đá 14 · đinh 12 · kính 7 | Salad dưa hấu ×1 (món mới) | Cây Thông |
| 31 | 7,657 | 1,900 | 2 | hạt cây cao nhất đang có ×3 | gỗ 28 · đá 15 · đinh 13 · kính 8 | nguyên liệu nấu ×3 | — |
| 32 | 8,128 | 2,040 | 2 | hạt cây cao nhất đang có ×3 | gỗ 30 · đá 16 · đinh 14 · kính 9 | nguyên liệu nấu ×3 | — |
| 33 | 8,613 | 2,180 | 2 | hạt cây cao nhất đang có ×3 | gỗ 32 · đá 17 · đinh 15 · kính 10 | nguyên liệu nấu ×3 | — |
| 34 | 9,112 | 2,320 | 2 | hạt cây cao nhất đang có ×3 | gỗ 34 · đá 18 · đinh 16 · kính 11 | nguyên liệu nấu ×3 | — |
| 35 | 9,625 | 2,460 | 10 | hạt ⚠ CÂY MỚI #5 ×4 | gỗ 36 · đá 19 · đinh 17 · kính 12 | nguyên liệu nấu ×3 | ⚠ Decor cao cấp #1 |
| 36 | 10,152 | 2,600 | 2 | hạt cây cao nhất đang có ×3 | gỗ 38 · đá 20 · đinh 18 · kính 13 | nguyên liệu nấu ×3 | — |
| 37 | 10,693 | 2,740 | 2 | hạt cây cao nhất đang có ×3 | gỗ 40 · đá 21 · đinh 19 · kính 14 | nguyên liệu nấu ×3 | — |
| 38 | 11,248 | 2,880 | 2 | hạt cây cao nhất đang có ×3 | gỗ 42 · đá 22 · đinh 20 · kính 15 | nguyên liệu nấu ×3 | — |
| 39 | 11,817 | 3,020 | 2 | hạt cây cao nhất đang có ×3 | gỗ 44 · đá 23 · đinh 21 · kính 16 | nguyên liệu nấu ×3 | — |
| 40 | 12,400 | 3,160 | 40 | hạt ⚠ CÂY MỚI #6 ×4 | gỗ 46 · đá 24 · đinh 22 · kính 17 | nguyên liệu nấu ×3 | ⚠ Decor cao cấp #2 |
| 41 | 12,997 | 3,300 | 2 | hạt cây cao nhất đang có ×3 | gỗ 48 · đá 25 · đinh 23 · kính 18 | nguyên liệu nấu ×3 | — |
| 42 | 13,608 | 3,490 | 2 | hạt cây cao nhất đang có ×3 | gỗ 50 · đá 26 · đinh 24 · kính 19 | nguyên liệu nấu ×3 | — |
| 43 | 14,233 | 3,680 | 2 | hạt cây cao nhất đang có ×3 | gỗ 52 · đá 27 · đinh 25 · kính 20 | nguyên liệu nấu ×3 | — |
| 44 | 14,872 | 3,870 | 2 | hạt cây cao nhất đang có ×3 | gỗ 54 · đá 28 · đinh 26 · kính 21 | nguyên liệu nấu ×3 | — |
| 45 | 15,525 | 4,060 | 10 | hạt ⚠ CÂY MỚI #7 ×4 | gỗ 56 · đá 29 · đinh 27 · kính 22 | nguyên liệu nấu ×3 | ⚠ Decor cao cấp #3 |
| 46 | 16,192 | 4,250 | 2 | hạt cây cao nhất đang có ×3 | gỗ 58 · đá 30 · đinh 28 · kính 23 | nguyên liệu nấu ×3 | — |
| 47 | 16,873 | 4,440 | 2 | hạt cây cao nhất đang có ×3 | gỗ 60 · đá 31 · đinh 29 · kính 24 | nguyên liệu nấu ×3 | — |
| 48 | 17,568 | 4,630 | 2 | hạt cây cao nhất đang có ×3 | gỗ 62 · đá 32 · đinh 30 · kính 25 | nguyên liệu nấu ×3 | — |
| 49 | 18,277 | 4,820 | 2 | hạt cây cao nhất đang có ×3 | gỗ 64 · đá 33 · đinh 31 · kính 26 | nguyên liệu nấu ×3 | — |
| 50 | 19,000 | 5,010 | 60 | hạt ⚠ CÂY MỚI #8 ×4 | gỗ 66 · đá 34 · đinh 32 · kính 27 | nguyên liệu nấu ×3 | ⚠ Decor huyền thoại |

Ghi chú:
- ⚠ **Sau L12 game hết cây mới.** Bảng đề xuất **8 cây mới** ở L15/20/25/30/35/40/45/50 — cần art.
  Chưa có thì ô3 tạm là "hạt cây cao nhất ×3".
- ⚠ **Sau L30 game hết món mới.** Cần 6–8 món mới cho L32–50 (nguyên liệu từ cây mới + chuồng).
- Vật liệu tặng tăng dần đúng nhịp shop yêu cầu (L≤10 chỉ gỗ/đá, L11–20 thêm đinh, L21+ thêm kính).
- Decor ô6 lấy **đúng prefab shop** nên người chơi được "nếm" decor miễn phí → muốn mua thêm.

### Nội dung mở khoá L31–50 (đề xuất, cần duyệt — không có nội dung thì 20 cấp trống)
| Lv | Mở khoá |
|---|---|
| 32 | Nâng cấp chuồng cấp 2 (+50% sản lượng) |
| 34 | Cây mới #5 · Món mới |
| 35 | Khu đất 3 · Decor cao cấp #1 |
| 38 | Máy mới (Máy Sấy / Lò Nướng) |
| 40 | Cây anh đào (KC) · Decor cao cấp #2 |
| 42 | Nhà hàng ven biển cấp 2 |
| 45 | Khu đất 4 · Decor cao cấp #3 |
| 48 | Bộ ghế đá (KC) |
| 50 | Huy hiệu Nông trại Vàng · Decor huyền thoại · Khung avatar |

---

## F. HIỆU ỨNG BAY — đã rà xong

### F1. 🔴 "Khung nền đen xám bay cùng vàng" — ĐÃ SỬA
Không phải sprite vàng (đã đo: 42% trong suốt, 0 pixel tối). Gốc rễ ở `RewardFlyFX.ResolveSprite()`:
- `RewardIconLibrary.asset` chỉ có `goldSprite`; **`gemSprite` và `expSprite` = null**.
- Null thì rơi xuống nhánh dự phòng: `target.GetComponentInChildren<Image>()` — mà `targetGem` là
  `Diamond_Container`, Image **đầu tiên** là của **chính container** với sprite `hud_currency_base.png`
  = **cái nền pill đen xám của HUD**.
- ⇒ Mỗi lần nhận kim cương, game bay **bản sao cái pill đen** lên HUD. EXP tương tự (badge dùng
  sprite built-in → bay ô vuông trắng).

Đã sửa 2 lớp: (1) gán `gemSprite` = `kimcuong-removebg-preview.png` trong library; (2) nhánh dự
phòng giờ **chỉ mượn Image của con, không lấy chính container**, ưu tiên tên có "Icon", loại mọi
sprite tên `base/bg/pill/capsule/frame/placeholder`. EXP không tìm được icon thật thì rơi về ngôi
sao vẽ tay sẵn có — không bao giờ bay ô trắng nữa.

### F2. Rà phủ sóng
Vàng/KC/EXP được bắn từ **chính manager** (`FarmEconomyManager.AddGold/AddGems` → `OnGoldAddedFx`,
`PlayerProgressManager.AddExp` → `OnExpAddedFx`) và `RewardFlyFX` nghe toàn cục. Đã grep: **không
có đường nào cộng tiền bỏ qua manager** ⇒ mọi popup hiện có và tương lai đều tự có hiệu ứng, xuất
phát từ đúng điểm chạm, bay về đúng icon HUD, có lớp overlay riêng nên popup không che.

| Loại | Bay về HUD | Ghi chú |
|---|---|---|
| Vàng | ✅ mọi nơi | |
| Kim cương | ✅ mọi nơi | vừa sửa sprite |
| EXP | ✅ mọi nơi | vừa sửa sprite |
| Vật phẩm thu hoạch → Kho | ✅ | `HarvestFlyItemFX` + `WarehousePulseFX` |
| Vật phẩm tàu → Kho | ✅ | `TrainManager` có FX |
| **Vật phẩm quà lên cấp → Kho** | ❌ | `LevelUpPopupUI.cs:1290` gọi `WarehouseManager.AddItem` thẳng, không FX |
| **Vật phẩm đơn hàng → Kho** | ❌ | `OrderBoardManager` AddItem, không FX |

Đề xuất bịt 2 lỗ này bằng **cùng một khuôn**: `WarehouseManager.AddItem` bắn `OnItemAddedFx(sprite, số lượng)`,
`RewardFlyFX` thêm `RewardKind.Item` bay về nút **KHO** ở thanh dưới. Khoảng 60 dòng, em làm khi Sếp gật
(chưa làm vì `RewardFlyFX` 800 dòng CRLF đang chạy tốt, sửa mù dễ vỡ thứ đang đúng).

---

## G. HÀNH VI NGƯỜI CHƠI → THIẾT KẾ KINH TẾ

Sếp hỏi theo nhóm: người xài nhiều / tiết kiệm / trẻ em / phụ nữ trung niên / dưới–trên 18.
Em nói thẳng một điều trước: **tuổi và giới không dự đoán được cách tiêu tiền trong game** — dữ
liệu ngành farm game cho thấy cùng một nhóm tuổi có cả người tích trữ lẫn người tiêu sạch. Thứ dự
đoán được là **nhịp chơi và phản xạ khi có tiền**. Nên em thiết kế theo 4 kiểu hành vi, và ánh xạ
các nhóm Sếp nêu vào đó:

| Kiểu hành vi | Nhận ra qua | Nhóm Sếp nêu thường rơi vào | Game phải làm gì |
|---|---|---|---|
| **Tiêu ngay** (thấy là mua, tăng tốc bằng KC) | Vàng luôn gần 0, KC tụt nhanh | "người dùng tiền nhiều", trẻ em | **Không chặn, cho họ cạn** — nhưng cạn *mềm*: thứ tiếp theo đắt hơn thấy rõ, và **chờ luôn là lựa chọn miễn phí**. Không bao giờ có tường "hết tiền = không chơi được" |
| **Tích trữ** (chỉ mua khi lời) | Vàng tăng đều, ít tăng tốc | "người tiết kiệm", người chơi lâu năm | Phải **sống được**: thu nhập cơ bản luôn dương kể cả khi kẻ tiêu ngay đã cạn. Thưởng tiết kiệm bằng **cơ hội có hạn** (món hôm nay ×1.3 đã có, thêm "giảm giá cuối tuần" cho decor) để để dành có ý nghĩa |
| **Thư giãn** (phiên ngắn, mê trang trí, ghét thất bại) | 5–15 phút/lần, mua decor nhiều hơn công trình | phụ nữ trung niên, trẻ em | **Không có vòng lặp phạt**, quà nhỏ đều tay, decor là mục tiêu, bước tiếp theo luôn hiện rõ. Đây là nhóm ô6-decor trong bảng quà phục vụ |
| **Tối ưu** (phiên dài, tìm vàng/phút cao nhất) | Chỉ trồng cây lời nhất, phá cap sớm | người chơi trên 18 nhiều thời gian | **Đường cong EXP và sink cuối game phải chịu được họ** — chính vì nhóm này mà cần L31–50 và 6 decor KC. Cho họ EXP/phút tăng theo bậc cây để tối ưu có việc làm |

### Riêng người chơi dưới 18
Không phải khác về cách chơi mà khác về **trách nhiệm**: không tạo áp lực tiền thật, không đếm ngược
gây hoảng, giá hiện rõ trước khi trừ, không có cơ chế "mất" khiến trẻ khó chịu. Bản kinh tế này
**không có trừ tiền ẩn, không có phạt**, chỉ có chờ hoặc trả KC — đã đúng hướng.

### Vì sao bản này "tự nhiên, không khó chịu"
1. **Tuần đầu vẫn lên nhanh** (L10 ≈ 2 ngày) — kẻ tiêu ngay không nản, người thư giãn thấy tiến bộ.
2. **Không ai cạn hoàn toàn**: ô đất và cây L1 không bao giờ khoá, lúa 50 giây luôn ra tiền.
3. **Luôn có thứ để để dành**: shop gating + decor cao cấp + KC decor ⇒ người tích trữ có mục tiêu.
4. **Quà lên cấp = thử ngay**: hạt của cây vừa mở + vật liệu đúng cho công trình vừa mở ⇒ không
   phải đọc chữ "đã mở khoá" rồi tự đi tìm.
5. **Trần 50 ở tháng 2,5** — đủ xa cho người tối ưu, đủ gần để người thư giãn tin là có ngày tới.

---

## H. VIỆC EM SẼ LÀM KHI SẾP GẬT (một lượt, có backup)
| # | Việc | Loại | Rủi ro |
|---|---|---|---|
| 1 | `PlayerProgressManager`: công thức mới, `CapToiDa 30→50`, `SaveVersion 1→2`, kẹp EXP dư | CODE | thấp — đã chứng minh không lên/tụt cấp |
| 2 | Viết lại 29 file `LevelReward_L2..30` theo bảng E | ASSET | thấp — có backup |
| 3 | Tạo 20 file `LevelReward_L31..50` | ASSET | thấp |
| 4 | Đổi `unlockLevel` 25 mặt hàng shop theo bảng D | ASSET | thấp |
| 5 | Nâng `expReward` 10 cây cấp 7–12 (6,0 → 8,5 EXP/phút) | ASSET | thấp |
| 6 | Bịt 2 lỗ FX vật phẩm → Kho | CODE | trung bình |
| 7 | Brief đội vẽ: 8 cây mới, 6–8 món mới, 8 decor cao cấp, 6 decor KC | ART | — |

**Cần Sếp trả lời 3 câu:**
1. Duyệt đường cong `30L + 7L²` (L30 ≈ 23 ngày, L50 ≈ 77 ngày) hay muốn nhanh/chậm hơn?
2. Duyệt bảng shop gating (mục D)?
3. Nội dung L31–50 (mục E cuối) — duyệt để em viết brief art, hay Sếp muốn nội dung khác?

## BACKUP vòng này
`production/backup_scene_vong14/RewardFlyFX.cs`, `RewardIconLibrary.asset`

---

## I. NHẬT KÝ ÁP DỤNG — 09/09/2026

| # | Việc | Kết quả |
|---|---|---|
| 1 | `PlayerProgressManager`: `Req = 30L + 7L²` (L1 = 40), `CapToiDa 30→50`, `SaveVersion 1→2`, kẹp EXP dư một lần sau Load | ✅ |
| 2 | Viết lại **29 file** `LevelReward_L2..30` theo khuôn 6 ô | ✅ 49/49 YAML hợp lệ |
| 3 | Tạo **20 file** `LevelReward_L31..50` (+ .meta guid mới) | ✅ |
| 4 | Shop gating: đổi `unlockLevel` **23 mặt hàng** theo bảng D. **Bỏ 3 máy** (Sếp đã xoá khỏi shop) — không tặng, không mở khoá, không nhắc trong popup | ✅ |
| 5 | Nâng `expReward` **13 cây cấp 7–12**: 6,0 → 6,8…8,5 EXP/phút theo bậc | ✅ |
| 6 | Vật phẩm quà lên cấp **bay về nút KHO**: `WarehouseManager.OnItemAddedFx` + `RewardFlyFX.SpawnItemFromScreen` (đường riêng, không đụng switch cũ) | ✅ |
| 7 | Vật phẩm từ **bảng đơn hàng** vẫn chưa bay (đi thẳng `FarmInventoryManager.AddItem`, không có icon) | ⬜ nhỏ, làm sau |

Tổng quà thực tế L2–30: **22.410 vàng · 138 KC** (cũ 35.990 · 208).

### Nội dung L31–50 đã chốt trong file quà (mở khoá chỉ là CHỮ + quà; hệ thống thật cần build sau)
| Lv | Mở khoá | Cần gì để thành thật |
|---|---|---|
| 32 | Chuồng cấp 2 (+50% sản lượng) | code nâng cấp chuồng |
| 35 · 45 | Khu đất 3 · 4 | `LandRegionData` mới |
| 34 · 43 | Cây mới #5 · #7 | art + `CropData` |
| 38 | Lò Nướng | art + máy |
| 40 · 48 | Cây anh đào · Bộ ghế đá (KC) | art decor KC |
| 42 | Nhà hàng ven biển cấp 2 | code |
| 47 | Tàu 2 toa | code |
| 50 | Huy hiệu Nông trại Vàng · Khung avatar | UI |

⚠ Từ L31 trở đi **chưa có nội dung thật** — người chơi lên cấp vẫn nhận vàng/KC/vật liệu/hạt, nhưng
chữ "mở khoá" là lời hứa. Phải build dần theo bảng trên trước khi người chơi đầu tiên chạm L31
(theo mô phỏng: ~ngày 25).

### Brief art v6 (gửi đội vẽ) — `production/BRIEF_ART_v6_2026-09-09.md`

### Backup: `production/backup_kinhte_vong15/` (29 file quà cũ · 42 asset shop · 23 cây · PlayerProgressManager · WarehouseManager)

### Bổ sung 09/09 — ĐẤT MỞ RỘNG (làm luôn, không treo)
Hệ đất đang chạy là `Lot_00..20` (21 lô, `Land_x_y` là data chết). Lô đã trải tới L62 nhưng giá **×1,6 mỗi lô**:
Lô 16 (L50) = 2.305.840 vàng, Lô 20 = 15 triệu — thu nhập ~8k/giờ không bao giờ tới.
Đã trải lại **20 lô đều trên L5→L50** (bước 2–3 cấp), giá **×1,337** từ 2.000 → 500.000.
Tổng sink đất L5–50 ≈ **1,98 triệu vàng** — đây là sink dài hạn chính, thay cho decor cao cấp chưa có art.
Lô 17–20 giờ mở ở L43/45/47/50 thay vì L53–62 (ngoài trần).

Còn đúng 4 mục L31–50 cần CODE mới (không phải nối dây): chuồng cấp 2 (+50%) · nhà hàng cấp 2 · tàu 2 toa · Lò Nướng (kèm art).
Backup: `backup_kinhte_vong15/land/`.

### Kiểm tra lại theo câu hỏi của Sếp (09/09, muộn)
- **Lên được cấp 50?** Có. `CapToiDa = 50` trong code; scene lưu `maxLevel: 100` nhưng `Awake()` kẹp về 50. Đã tính lại: không ai bị lên/tụt cấp khi cập nhật.
- **Popup quà đủ 49 cấp?** Lúc đầu **chưa** — popup đọc `levelRewardConfigs` serialize trong scene, đang có đúng 29 entry (L2–30). 20 file mới chưa nằm trong list ⇒ L31–50 sẽ không hiện quà. **Đã nối thêm 20 entry vào scene** (+1.440 byte, chỉ chèn vào list). Kiểm lại: 49/49 có trong scene.
- Chữ "mở khoá" L31–50 đã đổi sang **chỉ nói thứ có thật**: lô đất mở theo cấp + mốc quà KC. Bỏ hết chuồng cấp 2 / nhà hàng cấp 2 / tàu 2 toa / Lò Nướng theo ý Sếp.
- Bỏ 6 nhãn "Mở khóa decor" ở L5/10/15/20/25/30 vì lệch với shop gating (shop là nguồn sự thật).
- Backup scene trước khi nối: `backup_kinhte_vong15/SCN_Farm_before_levelrewards.unity`.
