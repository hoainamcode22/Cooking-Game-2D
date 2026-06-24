# REWARDS MASTER LIST — Phần thưởng L1→L30 (lên cấp · nhiệm vụ · thành tựu · đăng nhập 7 ngày)

> Bảng phần thưởng đầy đủ để bạn TỰ THÊM vào game (LevelReward asset, Mission asset, Daily login).
> Số liệu lên cấp L1–L10 khớp `L1_L10_ECONOMY_TABLE.md §6`; L11–L30 mở rộng theo cùng nhịp lạm phát
> (vàng 700→2600/cấp như `LevelUpRewardDataSetupTool` đang sinh). Phần nhiệm vụ/thành tựu lấy từ
> `MISSIONS_MASTER_LIST.md`. Đăng nhập 7 ngày là thiết kế MỚI (Mục 4).
>
> ⚠ Quy ước: "Quà" = vật phẩm cộng thẳng vào kho/ví khi bấm "Nhận quà" ở popup lên cấp. "Mở khoá" =
> thứ xuất hiện trong shop/scene (KHÔNG cộng vào kho, chỉ cho phép mua/dùng). Item ID là gợi ý theo
> cách đặt tên hiện có của bạn — đổi cho khớp asset thật.

---

## 0. Bảng item ID tham chiếu (đổi cho khớp data thật của bạn)

- **Hạt:** `seed_rice, seed_huong_duong, seed_bapcai, seed_ngo, seed_cachua, seed_carot, seed_hoa_hong,
  seed_hoa_oai_huong, seed_khoai_tay, seed_nam, seed_mia, seed_chanh, seed_chili, seed_pepper,
  seed_tulip, seed_hoa_lan, seed_hoa_cuc_trang, seed_hoa_cuc_van_tho`
- **Chuồng:** `pen_chicken, pen_pig, pen_cow, pen_dairy`
- **Máy:** `machine_mill` (xay bột), `machine_press` (ép mía), `machine_cheese` (phô mai)
- **Khu mới:** `fish_pond` (hồ cá), `tourist_boat` (bến tàu)
- **Booster:** `booster_fertilizer` (phân bón x2 lớn nhanh), `booster_rain` (tưới cả trại), `booster_market` (+giá bán)
- **Trang trí/skin/pet:** `decor_*`, `skin_*`, `pet_cat / pet_dog / pet_duck`
- **Danh hiệu:** `title_*`

---

## 1. ⭐ PHẦN THƯỞNG LÊN CẤP — chi tiết L1 → L30

> Cột "Quà" cộng thẳng khi nhận. Cột "Mở khoá" là nội dung mới (shop/scene). Popup lên cấp show cả hai.

### L1 → L10 (khớp economy table — nền onboarding)

| Cấp | Vàng | Gem | Quà vật phẩm | MỞ KHOÁ tại cấp này | Đặc biệt |
|----|------|-----|--------------|----------------------|----------|
| 1 (start) | 400 | 15 | 6× `seed_rice` + 2× `seed_huong_duong` (starter) | Lúa, Hướng dương, Bắp cải; 6 ô đất + 2 chậu hoa; 4 nhà order | Bắt đầu game |
| 2 | 150 | 2 | 3× `seed_ngo` + **`pen_chicken` (tặng)** | Ngô; chuồng gà; trứng | Mở animal tutorial |
| 3 | 200 | 2 | 3× `seed_cachua` | Cà chua, Cà rốt; **nhà order #5** | — |
| 4 | 250 | 3 | 2× `seed_hoa_hong` | Hoa hồng, Oải hương; **`pen_pig` bán ở shop** | Tutorial L4 chăn nuôi heo |
| 5 | 300 | 3 | 5× `seed_khoai_tay` | **MỞ BẾP (cooking)** + 10 món dễ; Khoai tây; **nhà #6** | Popup "Bếp đã mở!" |
| 6 | 350 | 3 | 3× `seed_nam` | Nấm; **`pen_cow` bán**; **daily mission bật** | — |
| 7 | 400 | 4 | 3× `seed_mia` | Mía; **nhà #7**; hoa lan + cúc trắng | — |
| 8 | 450 | 4 | 3× `seed_chanh` | Chanh; đơn thịt bò; 4 món bò; **`pen_dairy` bán** | — |
| 9 | 500 | 5 | 2× `seed_chili` | Ớt; Tulip + cúc vạn thọ; **nhà #8 (đủ 8)** | Đơn combo |
| 10 | 600 | 8 | 1× `booster_fertilizer` + 1× `decor_arch_l10` | Tiêu; hoa còn lại; trang trí cơ bản | 🏅 `title_farmer` "Nông dân thực thụ" + pháo hoa lớn |

**Cộng L1→L10:** ~3.200 vàng + 34 gem (không tính start). Khớp econ table.

### L11 → L20 (chế biến + hồ cá + mở rộng)

| Cấp | Vàng | Gem | Quà vật phẩm | MỞ KHOÁ tại cấp này | Đặc biệt |
|----|------|-----|--------------|----------------------|----------|
| 11 | 700 | 5 | 1× `booster_fertilizer` + 3× `seed_mia` | **`machine_mill` (Máy Xay Bột) bán**; bột gạo | — |
| 12 | 760 | 5 | 1× `decor_fence_set` + 200 hạt vàng vặt | **Nâng cấp kho lần 1**; cây mới | — |
| 13 | 820 | 5 | 1× `booster_market` + 3× `seed_chanh` | **`machine_press` (Ép Mía) bán**; nước mía | — |
| 14 | 880 | 5 | 2× `seed_*` (hoa cao cấp) | Đơn 2-item nhiều hơn; hoa mới | — |
| 15 | 1.000 | 10 | 1× `skin_house_blue` + 1× `booster_rain` | **`machine_cheese` (Phô Mai) bán**; phô mai | 🏅 `title_chef` "Đầu bếp khéo" |
| 16 | 1.100 | 6 | 1× `fishing_rod` + 5× `bait` | **`fish_pond` (Hồ Cá) mở**; câu cá | Mở món cá |
| 17 | 1.200 | 6 | 2× món cá recipe unlock | Món cá vào pool đơn; cá mới | — |
| 18 | 1.300 | 6 | 1× `booster_fertilizer` + 1× `decor_pond_small` | **Mở rộng đất khu 2** (ô 9–14) | — |
| 19 | 1.400 | 6 | 1× **`pet_cat`** (pet đầu tiên) | Pet đi quanh trại (nhặt xu rơi); decor pet | Pet ra mắt |
| 20 | 1.600 | 15 | 1× `skin_barn_gold` + 2× `booster_*` | Cây/đơn cao cấp; trang trí cấp 2 | 🏅 `title_master` "Chủ trại giỏi" + pháo hoa lớn |

### L21 → L30 (tàu du lịch + sự kiện + bậc thầy)

| Cấp | Vàng | Gem | Quà vật phẩm | MỞ KHOÁ tại cấp này | Đặc biệt |
|----|------|-----|--------------|----------------------|----------|
| 21 | 1.700 | 7 | 1× `booster_market` + 3× `seed_*` | **Nâng cấp kho lần 2**; slot sản xuất thêm | — |
| 22 | 1.800 | 7 | 1× `decor_lantern_set` | Đơn cá nhiều hơn; cây hiếm | — |
| 23 | 1.950 | 7 | 1× `booster_fertilizer` ×2 | **`tourist_boat` (Bến Tàu Du Lịch) mở** | Hệ tàu khách ra mắt |
| 24 | 2.050 | 7 | 1× `decor_seaside_set` | Nhà hàng ven biển; đơn tàu cao cấp | — |
| 25 | 2.200 | 15 | 1× **`pet_dog`** + 1× `skin_*` hiếm | Trang trí cấp 3; combo 3-item | 🏅 `title_tycoon` "Ông/Bà chủ lớn" |
| 26 | 2.300 | 8 | 2× `booster_*` + 3× `seed_pepper` | Recipe cao cấp; decor mùa | — |
| 27 | 2.400 | 8 | 1× `decor_garden_premium` | Đơn tàu khó (payout x3); cây cao cấp | — |
| 28 | 2.480 | 8 | 1× **`pet_duck`** + 2× `booster_*` | Sự kiện mùa (placeholder); decor hiếm | — |
| 29 | 2.550 | 8 | 1× `skin_*` premium + 3× `seed_*` | Toàn bộ pool nội dung; trang trí top | — |
| 30 | 2.600 | 30 | 1× `pet_legendary` + 1× `skin_legendary` + 1× `decor_trophy` | (max nội dung demo) | 🏅 `title_grandmaster` "BẬC THẦY NÔNG TRẠI" + pháo hoa khổng lồ |

**Cộng L11→L30 (gợi ý):** ~33.000 vàng + ~146 gem (gem dồn ở mốc tròn 15/20/25/30). Là faucet vàng chính của
mid/late game, cân với hố vàng tăng dần (máy, mở đất, nâng kho, tàu) để không vỡ kinh tế.

---

## 2. PHẦN THƯỞNG NHIỆM VỤ (tóm tắt — chi tiết ở MISSIONS_MASTER_LIST.md)

Nhiệm vụ là **bonus phụ** trên nguồn chính (đơn hàng), KHÔNG thay thế. ~91 nhiệm vụ chính L1→L30,
mỗi cấp 3 nhiệm vụ (L1 có 4). Mức thưởng tăng dần theo cấp:

| Band cấp | Thưởng/nhiệm vụ (vàng) | Gem (ở mốc tròn) |
|----------|------------------------|-------------------|
| L1–L5 | 30–120 | 1 (L1, L5) |
| L6–L10 | 140–500 | 2 (L8), 5 (L10) |
| L11–L15 | 300–700 | 2–3 (L12, L15) |
| L16–L20 | 450–1.000 | 3 (L18), 8 (L20) |
| L21–L25 | 800–1.300 | 3 (L21,23), 10 (L25) |
| L26–L30 | 1.100–2.000 | 4 (L27), 20 (L30) |

> Danh sách đầy đủ từng nhiệm vụ (missionId, eventType, target, thưởng) ở `MISSIONS_MASTER_LIST.md` §A.
> **Daily mission** (mở từ L6): 3 việc/ngày, 40–90 vàng/việc, +1 gem nếu xong cả 3 — §B của file đó.

## 3. PHẦN THƯỞNG THÀNH TỰU (liệt kê đầy đủ — nhận 1 lần, dài hạn)

| Thành tựu | Điều kiện | Thưởng |
|-----------|-----------|--------|
| Nông dân tập sự | Thu 100 nông sản | 200 vàng |
| Nông dân lành nghề | Thu 500 nông sản | 600 vàng + 3 gem |
| Nông dân huyền thoại | Thu 2.000 nông sản | 2.000 vàng + 10 gem |
| Người giao hàng | 50 đơn | 300 vàng |
| Thương lái | 300 đơn | 1.000 vàng + 5 gem |
| Đầu bếp nhỏ | Nấu 30 món | 400 vàng |
| Bếp trưởng | Nấu 150 món | 1.200 vàng + 5 gem |
| Thợ chế biến | Chế biến 50 sản phẩm | 500 vàng |
| Ngư dân | Câu 100 cá | 800 vàng + 3 gem |
| Chủ bến tàu | Phục vụ 25 chuyến tàu | 1.500 vàng + 8 gem |
| Nhà trang trí | Đặt 30 decor | 700 vàng |
| Khởi đầu vững | Đạt L10 | 300 vàng |
| Vươn xa | Đạt L20 | 800 vàng + 5 gem |
| Bậc thầy Nông trại | Đạt L30 | 3.000 vàng + 20 gem |
| Chăm chỉ | Đăng nhập (daily) 7 ngày | 500 vàng + 3 gem |
| Trung thành | Đăng nhập 30 ngày | 2.000 vàng + 15 gem |

**Tổng thành tựu:** ~16 cái · ~15.800 vàng + ~77 gem nếu hoàn thành hết (rải cả vòng đời game).

## 4. ⭐ ĐĂNG NHẬP 7 NGÀY (Daily Login Streak) — thiết kế mới

Chu kỳ **7 ngày lặp lại**. Mỗi ngày mở app + bấm nhận 1 lần. Ngày 7 là phần thưởng to (tạo lý do
"ráng đủ tuần"). Vàng **nhân hệ số theo band cấp** để luôn đáng nhận ở mọi giai đoạn.

| Ngày | Phần thưởng cơ bản (×hệ số vàng) | Ghi chú |
|------|----------------------------------|---------|
| 1 | 50 vàng + 2× `seed_rice` | chào ngày mới |
| 2 | 80 vàng | — |
| 3 | **1 gem** | mốc giữa tuần |
| 4 | 100 vàng + 1× `booster_fertilizer` | booster nhẹ |
| 5 | 3× hạt (theo cấp hiện tại) | quà nông sản |
| 6 | 150 vàng | — |
| 7 | **3 gem + 200 vàng + 1× `decor_*` / `skin_*` nhỏ** 🎁 | phần thưởng tuần (to + pháo hoa nhỏ) |

**Hệ số vàng theo band:** L1–10 ×1 · L11–20 ×2 · L21–30 ×3.
> Ví dụ Ngày 7 ở L15: 200×2 = 400 vàng + 3 gem + 1 decor. Ở L25: 200×3 = 600 vàng + 3 gem + 1 skin.

**Quy tắc streak (khuyến nghị cho đối tượng trẻ em — "tha thứ"):**
- Nên dùng kiểu **"không phạt"**: lỡ 1 ngày thì KHÔNG mất chuỗi, chỉ là ngày đó không nhận (tiếp tục ở
  ngày kế khi quay lại). Tránh "reset về ngày 1" gây ức chế cho trẻ/phụ huynh.
- (Tuỳ chọn nâng cao sau launch) "Lịch 28 ngày" theo tháng + 1 quà cực to cuối tháng (pet/skin hiếm).
- Liên kết với thành tựu "Chăm chỉ (7 ngày)" và "Trung thành (30 ngày)" ở Mục 3.

**Tổng phát từ login/tuần (ở band ×1):** ~580 vàng + 4 gem + hạt/booster/decor mỗi tuần.

## 5. Tổng nguồn phát (faucet) & cảnh báo cân bằng

| Nguồn | Vàng (ước cả game L1→L30) | Gem (ước) |
|-------|----------------------------|-----------|
| Lên cấp (Mục 1) | ~36.000 | ~180 |
| Nhiệm vụ chính (Mục 2) | ~vài chục nghìn (bonus) | ~80 |
| Thành tựu (Mục 3) | ~15.800 | ~77 |
| Đăng nhập 7 ngày (Mục 4) | đều đặn/tuần | ~4/tuần |
| Daily mission (từ L6) | nhỏ/đều | ~1/ngày khi xong cả 3 |

> ⚠ **Cân bằng:** đây là FAUCET (vàng/gem chảy vào). Phải cân với HỐ (hạt giống, mua chuồng/máy, nâng kho,
> mở đất, tàu, trang trí) — xem `L1_L10_ECONOMY_TABLE.md` + file mô hình kinh tế (backlog M3-1). Sau khi
> nhập số, chạy `/balance-check` / `Simulate Economy` để chắc không lạm phát ngoài ý muốn. Gem tổng
> (~340 cả vòng đời) đủ nhỏ giọt để gem vẫn "quý" và có lý do mua/xem quảng cáo (hợp F2P).

## 6. Ghi chú triển khai (để bạn/agent tự thêm data)

1. **Lên cấp:** ghi vào `LevelReward` asset L1–L30 (bạn đã có `LevelUpRewardDataSetupTool` sinh L11–L30 —
   chỉ cần cập nhật bảng Mục 1 vào tool/data). Mỗi reward gồm: vàng, gem, list (itemId, qty), title (nếu có).
2. **Nhiệm vụ + thành tựu:** dùng `MISSIONS_MASTER_LIST.md` + tool `Setup Missions (L1-L30)` (backlog M1-5).
3. **Đăng nhập 7 ngày:** khớp `DailyRewardManager` + `DailyRewardPopupUI` (backlog M1-9 / Phase 3 prompt Cursor).
   Lưu `lastClaimDay` + `streakIndex` qua PlayerPrefs/SaveSystem; áp hệ số band theo `FarmLevelManager.CurrentLevel`.
4. **Item ID:** đối chiếu Mục 0 với asset thật; thiếu item (pet/skin/decor/booster/fishing_rod) thì tạo
   placeholder data trước, ảnh gắn sau (đúng cách bạn đang làm — dựng nền, tự thêm asset).
5. Các con số là **đề xuất cân bằng** — chỉnh thoải mái; giữ nguyên tắc: faucet ~ hơi nhiều hơn hố, gem nhỏ giọt.

