# L1→L10 ECONOMY TABLE (BẢN NHÁP — CHỜ DUYỆT, CHƯA GHI DATA)

> Team C (Economy). Ngày: 2026-06-12. Mọi số "đề xuất" chỉ ghi vào data sau khi anh duyệt.
> ⚠ Điều kiện tiên quyết: xoá debug override `Gold=1000/Gems=1000` trong FarmEconomyManager.Start() (Editor) — nếu không mọi playtest kinh tế đều vô nghĩa.

---

## 1. EXP curve (GIỮ NGUYÊN code hiện tại — đã hợp lý)

Công thức: `EXP(level→level+1) = 40 + 10n + n²`, n = level−1. EXP dư giữ lại ✓.

| Lên cấp | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | … 15 | 20 | 30 |
|---------|---|---|---|---|---|---|---|---|----|------|----|----|
| EXP cần | 40 | 51 | 64 | 79 | 96 | 115 | 136 | 159 | 184 | ~319 | ~514 | ~1.061 |
| Cộng dồn từ L1 | 40 | 91 | 155 | 234 | 330 | 445 | 581 | 740 | 924 | — | — | — |

So chuẩn thị trường: Hay Day/Township L1–5 rẻ hơn nữa (7–130) nhưng curve này khớp tutorial hiện có (40 = đúng 6 lúa + 2 hoa) → giữ, bù bằng nguồn EXP dồi dào.

**Nguồn EXP:** thu hoạch 5–14/ô (bảng §3) · đơn hàng 3–12 EXP/đơn vị · nấu ăn +8/món (MỚI — hiện 0, cần duyệt) · train 10/slot · mission.

## 2. Tiền tệ khởi đầu (CHỜ DUYỆT)

| | Hiện tại | Đề xuất | Lý do |
|---|---------|---------|-------|
| Vàng | 1.250 (Editor bị ép 1000) | **400** | 1.250 mua được 60 hạt lúa → phá cảm giác khan hiếm. 400 = đủ 1 chuồng nhỏ sai lầm vẫn sống nhờ van lúa. Township start 500 |
| Gem | 10 (Editor bị ép 1000) | **15** | Chuẩn thị trường ~30 nhưng có quà +2~8/cấp → tổng 49 đến L10, đủ học speed-up nhiều lần |
| Hạt tặng sẵn (starter pack) | không rõ | 6 seed_rice + 2 seed_huong_duong | Tutorial không được phép bắt mua hạt |

## 3. Bảng cây trồng — hiện tại vs đề xuất

Hiện tại: MỌI cây `sell=3/đv` (12/ô) → bán chợ lỗ với mọi hạt (hạt 20–190); order mía 14/đv < lúa 15/đv; mọi cây unlock L1. Đề xuất (1 ô = 1 hạt → 4 đơn vị):

| Cây | Unlock | Giá hạt (cũ→mới) | Grow s (cũ→mới) | Sell/đv (cũ→mới) | Thu/ô | Lãi/ô | Order/đv (cũ→mới) | EXP/ô (cũ→mới) |
|------|--------|------------------|------------------|-------------------|-------|-------|---------------------|------------------|
| Lúa | 1 | 20 | 180 | 3→**7** | 28 | +8 | 15 (giữ) | 5 |
| Hướng dương | 1 | 50→**35** | 180 | 3→**12** | 48 | +13 | 15→**20** | 5 |
| Bắp cải | 1 | 60→**45** | 300 | 3→**15** | 60 | +15 | 18→**26** | 5→**6** |
| Ngô | **2** | 40 | 600→**360** | 3→**13** | 52 | +12 | 16→**22** | 5→**7** |
| Cà chua | **3** | 80→**65** | 900→**480** | 3→**20** | 80 | +15 | 20→**34** | 5→**8** |
| Cà rốt | **3** | 50 | 400 | 3→**16** | 64 | +14 | (mới) **28** | 5→**7** |
| Hoa hồng | **4** | 80 | 180 | 3→**24** | 96 | +16 | 18→**40** | 5 |
| Oải hương | **4** | 140→**100** | 180 | 3→**30** | 120 | +20 | 18→**48** | 5 |
| Khoai tây | **5** | 100→**80** | 500 | 3→**25** | 100 | +20 | (mới) **42** | 5→**9** |
| Nấm | **6** | 100 | 600 | 3→**30** | 120 | +20 | 22→**50** | 5→**10** |
| Mía | **7** | 150→**120** | 420 | 3→**36** | 144 | +24 | 14→**60** | 8→**10** |
| Chanh | **8** | 130 | 780 | 3→**38** | 152 | +22 | (mới) **62** | 5→**12** |
| Ớt | **9** | 170 | 540 | 3→**48** | 192 | +22 | (mới) **78** | 5→**12** |
| Tiêu | **10** | 190 | 660 | 3→**55** | 220 | +30 | (mới) **88** | 5→**14** |
| 9 hoa khác | rải L4–L10 | 60–140 | 180 | **18–32** | — | — | 18–20→**30–52** | 5 |

Quy luật: tỉ lệ Order/Sell giảm dần 2,1× (L1) → 1,6× (L10) — đơn luôn lời hơn bán chợ; bán chợ vẫn dương (van thanh khoản, sửa lỗi "bán là lỗ" hiện tại). Lúa giữ nguyên giá hạt 20 + chu kỳ 180s = van chống kẹt kiểu Hay Day wheat.

## 4. Động vật & công trình (CHỜ DUYỆT — giá feed cần dump PenMiniPanelConfig ở Batch 5)

| Hạng mục | Unlock | Giá | Sản phẩm — Sell/đv | Order/đv (cũ→mới) |
|----------|--------|-----|---------------------|---------------------|
| Chuồng gà | L2 | **TẶNG** (LevelReward_L2) | Trứng 14 · Thịt gà 22 | 18→**24** · 22→**36** |
| Chuồng heo | L4 | **600** | Thịt heo 34 | 25→**55** |
| Chuồng bò | L6 | **1.500** (không bắt buộc mua ngay; cần cho đơn bò L8) | Thịt bò 50 · Sữa 28 | 30→**80** · (mới) **45** |
| Nhà dân #5–#8 | L3/5/7/9 | Mở FREE theo level (demo) | — | — |
| Decor | L4+ | 50–300 | — | không bắt buộc |

## 5. Món ăn (order reward — khớp DESIGN_PLAN §6)

10 món demo: 115–160 vàng + 10–12 EXP/món (giữ vùng giá hiện tại 100–170 ✓ — nguyên liệu thô ~30–90 → biên lời ~1,8–2,5× kèm công mini-game). Món bò L8+: 155–170. Món cá: LOẠI khỏi order demo. Nấu thành công: +8 EXP (mới).

## 6. Quà level-up (tạo LevelReward_L7→L10 mới; L2–L6 ghi đè theo bảng)

| Lên cấp | Vàng | Gem | Vật phẩm | Ghi chú |
|---------|------|-----|----------|---------|
| 2 | 150 | 2 | 3 seed_ngo + chuồng gà | animal tutorial |
| 3 | 200 | 2 | 3 seed_cachua | mở nhà #5 |
| 4 | 250 | 3 | 2 seed_hoa_hong | chuồng heo bán |
| 5 | 300 | 3 | 5 seed_khoai_tay | MỞ BẾP, mở nhà #6 |
| 6 | 350 | 3 | 3 seed_nam | chuồng bò bán, daily mission |
| 7 | 400 | 4 | 3 seed_mia | mở nhà #7 |
| 8 | 450 | 4 | 3 seed_chanh | đơn bò |
| 9 | 500 | 5 | 2 seed_chili | mở nhà #8 |
| 10 | 600 | 8 | danh hiệu + pháo hoa lớn | tổng kết demo |

Tổng phát: **3.200 vàng + 34 gem** qua 9 lần lên cấp.

## 7. Nguồn ↔ Hố tiền tệ L1→L10

| | Nguồn (ước tổng L1→L10) | Hố (ước tổng) |
|---|--------------------------|----------------|
| **Vàng** | Đơn hàng ~3.500–5.000 · Quà level 3.200 · Bán chợ ~500–1.000 · Mission ~500 · Train ~300 | Hạt giống ~1.800–2.600 · Chuồng heo 600 · Chuồng bò 1.500 · Decor 0–500 |
| **Gem** | Start 15 + quà 34 = **49** | Speed-up 1–3/lần (~10–35 tuỳ kiểu chơi) · (sau demo: skip order, đổi nguyên liệu) |
| **Ads** | CHƯA LÀM — placeholder: field `canDropFromAds` đã có sẵn trong CropData | — |

Cân đối: tổng nguồn ~8.000–10.000 vs hố bắt buộc ~4.000–4.700 → dư ~50% cho sai lầm/decor. Không có điểm nào bắt buộc chi > số dư tích luỹ tối thiểu (kiểm chứng §8).

## 8. Mô phỏng người chơi BÌNH THƯỜNG (baseline)

| Level | Vàng đầu cấp | Gem | Hoạt động chính | Chi | Thu (đơn+bán+quà) | Cuối cấp | Thời gian | EXP tích luỹ |
|-------|--------------|-----|------------------|-----|---------------------|----------|-----------|---------------|
| 1 | 400 | 15 | Tutorial 6 lúa + 2 hoa (hạt tặng) | 0 | +0 | 400 | ~6 ph | 40 |
| 2 | 400→550* | 17 | Gà ăn→trứng; 2 vòng lúa; 2 đơn | −160 | +330 | 720 | ~9 ph | 91 |
| 3 | 720→920* | 19 | +cà chua/cà rốt; đơn trứng | −250 | +420 | 1.090 | ~10 ph | 155 |
| 4 | 1.090→1.340* | 22 | **MUA CHUỒNG HEO 600**; hoa hồng | −820 | +480 | 1.000 | ~12 ph | 234 |
| 5 | 1.000→1.300* | 25 | MỞ BẾP; nấu 2 món; đơn món 130 | −300 | +650 | 1.650 | ~12 ph | 330 |
| 6 | 1.650→2.000* | 28 | **MUA CHUỒNG BÒ 1.500**; nấm | −1.750 | +700 | 950 | ~13 ph | 445 |
| 7 | 950→1.350* | 32 | Mía; món heo; đơn 2-item | −350 | +800 | 1.800 | ~14 ph | 581 |
| 8 | 1.800→2.250* | 36 | Chanh; đơn bò + món bò | −400 | +950 | 2.800 | ~15 ph | 740 |
| 9 | 2.800→3.300* | 41 | Ớt/tulip; đơn combo | −450 | +1.100 | 3.950 | ~15 ph | 924 → **L10** |

\* sau quà level-up. Tổng: **~96 phút**, không điểm nào < 950 vàng sau L4. Điểm bóp tiền chủ đích: L4 (heo) và L6 (bò) — đúng nhịp "tiết kiệm để mua" của thể loại, có thể trì hoãn bò mà không kẹt đơn (đơn bò chỉ từ L8).

## 9. Mô phỏng 3 kiểu người chơi

| Kiểu | Hành vi | Min balance (tại đâu) | Gem còn ở L10 | Thời gian → L10 | Kết luận |
|------|---------|------------------------|----------------|------------------|----------|
| **Tiết kiệm** | Không decor, hoãn bò tới L8, chỉ farm lúa/bắp cải + đơn | ~700 (L4) | ~45 (gần như không dùng) | ~105 ph | An toàn tuyệt đối, hơi chậm — chấp nhận |
| **Phung phí** | Mua mọi chuồng ngay khi mở + decor ~400 | **~120 (L6)** — vẫn dương nhờ van lúa 20đ/hạt | ~30 | ~100 ph | Căng đúng chỗ thiết kế, không kẹt; cần test thật |
| **Gem rush** | Speed-up mọi cây trồng chính + chuồng | ~600 (L6) | **~5–8 (cạn ở L8–9)** | **~70 ph** | Nhanh hơn ~27%, hết gem trước L10 → demo cho thấy giá trị gem ✓ |

Điều kiện không-kẹt được bảo đảm bởi: (a) lúa 20đ luôn mua được + lời dương; (b) đơn lúa luôn trong pool mọi level; (c) đơn chỉ yêu cầu item đã unlock; (d) công trình đắt không chặn đường EXP (chỉ chặn nhánh sản phẩm tương ứng).

## 10. Hạng mục chờ duyệt (tick từng dòng)

- [ ] Starter 400 vàng / 15 gem / tặng 6+2 hạt
- [ ] Xoá debug override Editor (Gold/Gems=1000)
- [ ] Bảng cây §3 (sell mới, unlock mới, grow ngô 360s + cà chua 480s, order reward mới)
- [ ] Fix order mía 14 → 60/đv; fix ID nấm (order `nam` → `mushroom`)
- [ ] Giá chuồng: gà tặng L2 · heo 600 (L4) · bò 1.500 (L6)
- [ ] Quà level-up §6 (tạo LevelReward_L7–L10)
- [ ] Nấu ăn +8 EXP/món
- [ ] EXP curve giữ nguyên công thức code

> Sau khi duyệt: Team D ghi data bằng tool batch (không sửa tay từng asset), Team G chạy `Simulate Economy` đối chiếu bảng §8 trước khi vào scene.
