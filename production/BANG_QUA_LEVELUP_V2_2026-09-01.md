# 🎁 BẢNG QUÀ LEVEL-UP V3 — L2→L30, MỌI CẤP ≥6 VẬT PHẨM + VÀNG + GEM — 2026-09-01

> Thay thế bản preview `BANG_QUA_LEVELUP_L2_L30_2026-08-31.md` (V1) và bản V2/V2.1 cùng ngày.
> **Lệnh Sếp 01/09:** MỌI level phải có TỐI THIỂU 6 entry trong `giftItems` (không tính vàng/gem —
> popup render vàng + gem thành 2 ô riêng nên tổng dải quà ≥ 8 ô, đúng chất Family Farm).
> **Nguyên tắc sắt:** Vàng + Gem GIỮ NGUYÊN 100% bảng cũ đã duyệt (tổng 35.990 vàng + 208 gem, chênh 0%).
> Giá trị vật phẩm mỗi level ước theo giá market **≤ 35% giftGold** của level đó → không lạm phát.

## Nguồn ID — catalog chính chủ (Lead dump từ `MarketPriceTable.cs`, có displayName + unlockLevel)

Toàn bộ id trong bảng thuộc catalog này và **unlockLevel ≤ level nhận**:

| Nhóm | id (unlockLevel) |
|---|---|
| Hạt giống | seed_rice(1), seed_huong_duong(1), seed_bapcai(1), seed_ngo(2), ca_rot(3), seed_cachua(3), seed_hoa_hong(4), seed_hoa_oai_huong(4), khoai_tay(5), seed_nam(6), seed_hoa_lan(7), seed_hoa_cuc_trang(7), seed_sugarcane(7), seed_lemon(8), seed_tulip(9), seed_hoa_cuc_van_tho(9), seed_chili(9), seed_pepper(10), seed_hoa_mau_don(10), seed_hoa_cam_tu_cau(10), seed_hoa_anh_thao(10), seed_pumpkin(11), seed_watermelon(12) |
| Nông sản/Hoa | rice(1), bapcai(1), huong_duong(1), ngo(2), carot(3), cachua(3), hoa_hong(4), khoaitay(5), mushroom(6), sugarcane(7), lemon(8), tulip(9), chili(9), pepper(10), pumpkin(11), watermelon(12) |
| Chăn nuôi | egg(2), chicken_meat(2), pork(4), beef(8), milk(13) |
| Gia vị | salt(1), herbs(3), soysauce(4), fishsauce(4) |
| Vật liệu | da(6), go(6), dinh(7), son(8), kinh(8) |

> ⚠ `trung`/`sua` KHÔNG dùng — id chuẩn là `egg`/`milk`. Đồ trang trí (chậu hoa…) là hệ đặt-ngay
> không có kho → KHÔNG đưa vào `giftItems` (Lead đã báo Sếp riêng).

## Công thức mỗi level (6-7 entry)

- **2-3 hạt giống**: ưu tiên hạt VỪA MỞ ở level đó + 1 hạt chủ lực cũ (số lượng 2-4)
- **1-2 nông sản/hoa** theo theme (3-5)
- **1 chăn nuôi** (1-2) **hoặc gia vị** (2-3)
- **1 vật liệu xây dựng** từ L6, xoay vòng đá→gỗ→đinh→sơn→kính (1-2)
- Level tròn 5/10/15/20/25/30 = "đậm": gem cao kế thừa bảng cũ; L10/15/20/25/30 có **7 entry**.

## Bảng V3 (L2→L30) — khớp 1-1 với bảng nhúng trong `LevelRewardV2FillTool.cs`, đã kiểm máy

| Cấp | Vàng | Gem | Hạt giống | Nông sản/Hoa | Chăn nuôi | Gia vị | Vật liệu | Ghi chú unlock / theme | Giá trị item ước (% vàng) |
|---|---|---|---|---|---|---|---|---|---|
| 2 | 150 | 2 | `seed_ngo` ×3 + `seed_rice` ×2 + `seed_bapcai` ×2 | `ngo` ×3 | `egg` ×1 | `salt` ×2 | — | Mở chuồng gà + Ngô → hạt ngô vừa mở, trứng đầu tiên | ≈51 v (34%) |
| 3 | 200 | 2 | `ca_rot` ×2 + `seed_cachua` ×2 + `seed_rice` ×2 | `cachua` ×3 | `egg` ×1 | `herbs` ×2 | — | Mở Cà rốt + Cà chua + Rau thơm → hạt vừa mở | ≈60 v (30%) |
| 4 | 250 | 3 | `seed_hoa_hong` ×2 + `seed_hoa_oai_huong` ×2 | `hoa_hong` ×3 | `pork` ×1 | `soysauce` ×2 + `salt` ×2 | — | Mở Hoa hồng/Oải hương + chuồng heo + nước tương | ≈86 v (34%) |
| 5 | 300 | 3 | `khoai_tay` ×3 + `seed_cachua` ×2 | `khoaitay` ×3 + `cachua` ×3 | `egg` ×2 | `fishsauce` ×2 | — | 🌟 ĐẬM — MỞ BẾP + Khoai tây → nguyên liệu nấu món đầu tiên | ≈101 v (34%) |
| 6 | 350 | 3 | `seed_nam` ×3 + `khoai_tay` ×2 | `mushroom` ×3 + `ngo` ×3 | `chicken_meat` ×1 | — | `da` ×1 | Mở Nấm + chuồng bò + Đá/Gỗ → vật liệu đầu tiên | ≈97 v (28%) |
| 7 | 400 | 4 | `seed_sugarcane` ×3 + `seed_hoa_lan` ×2 | `sugarcane` ×3 + `mushroom` ×3 | — | `herbs` ×2 | `dinh` ×1 | Mở Mía + Hoa lan/Cúc trắng + Đinh | ≈120 v (30%) |
| 8 | 450 | 4 | `seed_lemon` ×3 + `seed_sugarcane` ×2 | `lemon` ×3 + `mushroom` ×3 | `beef` ×1 | — | `son` ×1 | Mở Chanh + thịt bò + Sơn/Kính | ≈151 v (34%) |
| 9 | 500 | 5 | `seed_chili` ×2 + `seed_tulip` ×2 | `tulip` ×3 + `chili` ×3 | `egg` ×2 | — | `da` ×1 | Mở Ớt + Tulip/Cúc vạn thọ, nhà #8 | ≈142 v (28%) |
| 10 | 600 | 8 | `seed_pepper` ×3 + `seed_hoa_mau_don` ×2 + `seed_hoa_cam_tu_cau` ×2 | `pepper` ×3 + `hoa_hong` ×3 | `beef` ×1 | — | `go` ×1 | 🌟 ĐẬM — Mở Tiêu + 3 hoa cao cấp, title "Nông dân thực thụ" (7 entry) | ≈181 v (30%) |
| 11 | 700 | 5 | `seed_pumpkin` ×3 + `seed_pepper` ×2 | `pumpkin` ×3 + `rice` ×4 | `chicken_meat` ×2 | — | `dinh` ×1 | Mở Bí đỏ + Máy Xay Bột → tặng lúa đem xay | ≈153 v (22%) |
| 12 | 760 | 5 | `seed_watermelon` ×3 + `seed_pumpkin` ×2 | `watermelon` ×3 + `mushroom` ×4 | — | `herbs` ×3 | `son` ×1 | Mở Dưa hấu + nâng cấp kho lần 1 | ≈182 v (24%) |
| 13 | 820 | 5 | `seed_sugarcane` ×3 + `seed_lemon` ×2 | `sugarcane` ×4 + `lemon` ×3 | `milk` ×1 | — | `kinh` ×1 | Mở Sữa + Máy Ép Mía → theme mía-chanh-sữa | ≈160 v (20%) |
| 14 | 880 | 5 | `seed_hoa_mau_don` ×2 + `seed_hoa_anh_thao` ×2 | `hoa_hong` ×4 + `cachua` ×4 | `milk` ×1 | — | `da` ×1 | Hoa cao cấp, đơn 2-item | ≈134 v (15%) |
| 15 | 1000 | 10 | `seed_nam` ×3 + `seed_pumpkin` ×2 | `mushroom` ×5 + `pumpkin` ×3 | `milk` ×2 | `fishsauce` ×2 | `go` ×2 | 🌟 ĐẬM — Máy Phô Mai + title "Đầu bếp khéo" (7 entry, sữa làm phô mai) | ≈230 v (23%) |
| 16 | 1100 | 6 | `seed_cachua` ×3 + `ca_rot` ×3 | `carot` ×4 | `egg` ×2 | `salt` ×3 | `dinh` ×1 | Mở Hồ Cá (cần câu/mồi → Idea lấp đầy) | ≈87 v (8%) |
| 17 | 1200 | 6 | `seed_chili` ×3 + `seed_lemon` ×2 | `chili` ×4 + `lemon` ×4 | `milk` ×1 | — | `son` ×1 | Món cá vào pool đơn → gia vị chua cay | ≈190 v (16%) |
| 18 | 1300 | 6 | `seed_pepper` ×3 + `khoai_tay` ×3 + `seed_watermelon` ×2 | `khoaitay` ×4 | `beef` ×1 | — | `kinh` ×2 | Mở đất khu 2 → nhiều hạt trồng kín đất mới | ≈170 v (13%) |
| 19 | 1400 | 6 | `seed_sugarcane` ×3 + `seed_hoa_lan` ×2 | `mushroom` ×5 + `sugarcane` ×4 | `milk` ×2 | — | `da` ×2 | Pet mèo ra mắt | ≈205 v (15%) |
| 20 | 1600 | 15 | `seed_lemon` ×3 + `seed_hoa_cam_tu_cau` ×2 + `ca_rot` ×3 | `carot` ×5 + `lemon` ×4 | `beef` ×2 | — | `go` ×2 | 🌟 ĐẬM — title "Chủ trại giỏi", pháo hoa lớn (7 entry) | ≈221 v (14%) |
| 21 | 1700 | 7 | `seed_pepper` ×3 + `seed_pumpkin` ×2 | `pumpkin` ×4 | `milk` ×2 | `herbs` ×3 | `dinh` ×2 | Nâng cấp kho lần 2 + slot sản xuất | ≈211 v (12%) |
| 22 | 1800 | 7 | `seed_chili` ×3 + `seed_watermelon` ×3 | `watermelon` ×4 + `pepper` ×3 | `chicken_meat` ×2 | — | `son` ×2 | Cây hiếm, đơn cá nhiều hơn | ≈234 v (13%) |
| 23 | 1950 | 7 | `seed_sugarcane` ×3 + `seed_ngo` ×3 | `ngo` ×5 + `sugarcane` ×4 | `beef` ×2 | — | `kinh` ×2 | Mở Bến Tàu Du Lịch (vé tàu → Idea lấp đầy) | ≈196 v (10%) |
| 24 | 2050 | 7 | `seed_lemon` ×3 + `seed_cachua` ×3 | `lemon` ×4 + `cachua` ×5 | — | `fishsauce` ×3 | `da` ×2 | Nhà hàng ven biển → nguyên liệu bếp | ≈161 v (8%) |
| 25 | 2200 | 15 | `seed_pepper` ×3 + `khoai_tay` ×3 | `pepper` ×4 + `khoaitay` ×5 | `milk` ×2 + `beef` ×2 | — | `go` ×2 | 🌟 ĐẬM — title "Ông/Bà chủ lớn" (7 entry) | ≈293 v (13%) |
| 26 | 2300 | 8 | `seed_pepper` ×3 + `seed_pumpkin` ×3 | `pumpkin` ×5 + `rice` ×5 | `milk` ×2 | — | `dinh` ×2 | Recipe cao cấp, decor mùa | ≈243 v (11%) |
| 27 | 2400 | 8 | `seed_chili` ×3 + `seed_hoa_anh_thao` ×2 | `mushroom` ×5 + `chili` ×4 | — | `herbs` ×3 | `son` ×2 | Đơn tàu khó (×3), decor premium | ≈198 v (8%) |
| 28 | 2480 | 8 | `seed_sugarcane` ×3 + `seed_watermelon` ×3 | `watermelon` ×5 + `sugarcane` ×5 | `pork` ×2 | — | `kinh` ×2 | Pet vịt + sự kiện mùa | ≈272 v (11%) |
| 29 | 2550 | 8 | `seed_pepper` ×3 + `seed_cachua` ×3 | `cachua` ×5 + `pepper` ×4 | `milk` ×2 | — | `da` ×2 | Full pool nội dung | ≈211 v (8%) |
| 30 | 2600 | 30 | `seed_pepper` ×4 + `khoai_tay` ×4 | `watermelon` ×5 + `mushroom` ×5 | `beef` ×2 + `milk` ×2 | — | `go` ×2 | 🌟 ĐẬM — "BẬC THẦY NÔNG TRẠI", gem max, pháo hoa khổng lồ (7 entry) | ≈344 v (13%) |

### Kết quả kiểm máy (script sinh bảng, chạy 01/09)

| Ràng buộc | Kết quả |
|---|---|
| Mọi level ≥6 entry giftItems | **29/29 PASS** (24 level = 6 entry, 5 level đậm = 7 entry) |
| Mọi id thuộc catalog MarketPriceTable | PASS (0 id ngoài pool, không dùng `trung`/`sua`, không decor) |
| unlockLevel ≤ level nhận | PASS toàn bộ (kể cả milk L13, watermelon L12, vật liệu từ L6) |
| Khung số lượng (hạt 2-4, nông sản 3-5, chăn nuôi 1-2, gia vị 2-3, vật liệu 1-2) | PASS |
| Giá trị item ≤ 35% giftGold từng level | PASS — max 34,4% (L4), min 7,9% (L16/L24) |
| Vàng + Gem so bảng cũ | 35.990 vàng + 208 gem = **chênh 0%** |
| **Tổng giá trị vật phẩm toàn dải** | ≈5.084 vàng = **14,1% tổng vàng thưởng** |

> **Mô hình giá ước:** container không có bảng giá market nên script dùng giá ước theo bậc
> unlockLevel (hạt 3-12v, nông sản 4-18v, trứng 6/thịt gà 12/heo 20/bò 32/sữa 28, gia vị 3-8,
> vật liệu 10-15). Sau khi APPLY, Sếp thay giá thật từ `MarketPriceTable.cs` vào script kiểm
> (hoặc chạy `/balance-check` / `Simulate Economy`) để chốt — trần thiết kế là 35%, hiện trung
> bình chỉ ~14% nên còn dư an toàn lớn kể cả khi giá thật cao hơn giá ước ~2 lần.

### Vì sao "nhiều quà" mà không lạm phát

1. Asset thật hiện chỉ có 1 giftItem/level → V3 đổ 6-7 item + vàng + gem = **dải ≥8 ô quà**
   kiểu Family Farm, trong khi faucet vàng/gem đứng yên 100%.
2. Vật phẩm toàn hàng TIÊU HAO rẻ (hạt trồng ngay, nguyên liệu nấu, gia vị, vật liệu xây) —
   quay lại vòng chơi thay vì tích trữ; tổng chỉ ~14% giá trị vàng thưởng.
3. Quà theo CHỦ ĐỀ thứ vừa mở: mở cây gì tặng hạt cây đó, mở chuồng tặng sản phẩm chuồng,
   mở máy tặng nguyên liệu cho máy, từ L6 thêm vật liệu xây dựng xoay vòng phục vụ công trình.
4. Level tròn giữ vai trò "đậm": gem đột biến (8/10/15/15/30) + 7 entry + title/pháo hoa.

---

## 💡 IDEA LẤP ĐẦY — loại quà MỚI cần hệ thống hỗ trợ (KHÔNG đưa vào data lần này)

> Backlog đề xuất cho tương lai, cần code hệ thống + id mới. KHÔNG có trong tool đổ V3.

| Ý tưởng | Level phù hợp | Cần hệ thống gì |
|---|---|---|
| 🚀 Booster tăng tốc cây trồng (`booster_fertilizer`) | L10+, mỗi level tròn ×1 | Hệ booster (REWARDS_MASTER_LIST đã quy hoạch, chưa có id trong catalog) |
| 🌧️ Booster tưới cả trại (`booster_rain`) / +giá bán (`booster_market`) | L13, L15, L21+ | nt |
| 🎡 Vé quay may mắn (lucky wheel ticket) | mỗi level ×1, level tròn ×3 | Hệ vòng quay + UI (id "vé" chưa có trong catalog) |
| 🚂 Vé tàu / vé bến tàu (ticket đơn tàu thưởng ×2) | L23+ | Hệ tàu du lịch đang làm |
| 🎣 Cần câu / mồi câu (`fishing_rod`, `bait`) | L16-L17 | Hồ cá (đã quy hoạch L16) |
| 🐱 Thức ăn pet | L19, L25, L28 | Hệ pet |
| 🏡 Skin/decor tặng qua "kho xây dựng" | level tròn | Decor hiện là hệ đặt-ngay không có kho → cần cơ chế nhận-rồi-đặt-sau |
| ⭐ XP boost 10 phút | L11+ | Hệ buff theo thời gian |
| 🎟️ Ticket sự kiện mùa | L28+ | Hệ sự kiện |
| 📦 "Hộp quà bí ẩn" (random 1 trong 3 nguyên liệu) | mọi level | Hệ loot box nhẹ (cân nhắc rating trẻ em) |
