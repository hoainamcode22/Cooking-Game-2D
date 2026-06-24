# MISSIONS MASTER LIST — Danh sách nhiệm vụ hoàn chỉnh L1→L30

> **Đây là "list hoàn chỉnh" cho hệ nhiệm vụ (Ewar).** Gồm 3 phần:
> **A. Nhiệm vụ chính** (Main, theo level L1→L30) · **B. Nhiệm vụ ngày** (Daily, pool xoay vòng) ·
> **C. Thành tựu** (Achievement, dài hạn).
>
> Số liệu thưởng bám `L1_L10_ECONOMY_TABLE.md` (L1-L10) và tỉ lệ mở rộng cho L11-L30.
> `/autopilot` & tool `Setup Missions` đọc file này để sinh asset — **không tự bịa số**.
> Nền tảng kỹ thuật & sửa lỗi lệch key: xem `MISSIONS_L1_L10_PROPOSAL.md` (đã khảo sát code).

---

## 0. Schema dữ liệu (khớp MissionData sau khi nâng — Milestone M1-1)

| Field | Ý nghĩa |
|---|---|
| `missionId` | KEY duy nhất (vd `m_l4_buy_pig`). Rỗng → fallback tên asset |
| `missionName` | Tên hiển thị tiếng Việt |
| `requiredLevel` | Chỉ hiện khi player ≥ level này |
| `kind` | `Main` \| `Daily` |
| `eventType` | Loại sự kiện theo dõi (bảng dưới) |
| `targetItemId` | Lọc theo item; `any` = mọi item |
| `targetAmount` | Số lượng cần đạt |
| `rewardType` | `Coin` \| `Diamond` |
| `rewardAmount` | Số thưởng |

**eventType đang có (12):** `PlantItem, HarvestItem, FeedAnimal, CollectProduct, DeliverOrder,
DeliverOrderWithItem, DeliverComboOrder, BuyItem, CookDish, CookBeefDish, ReachLevel, TotalOrders`.

**eventType MỚI cần thêm khi tới milestone tương ứng** (đánh dấu ★ trong bảng):
`ProcessItem` (máy chế biến, L11+) · `UpgradeStorage` (nâng kho, L12+) · `CatchFish` (hồ cá, L16+) ·
`ServeBoat` (tàu du lịch, L23+) · `PlaceDecor` (trang trí, mọi level).

> Mission "thưởng kép" (vd 500 Coin + 5 Diamond): tạo **2 asset** cùng `missionId` prefix hoặc thêm
> field `secondRewardType/secondRewardAmount` (tuỳ chọn, M1-1). Bảng dưới ghi gộp cho dễ đọc.

---

## A. NHIỆM VỤ CHÍNH (Main) — L1 → L30

### Giai đoạn đầu L1–L10 (onboarding — khớp design plan)

| Lvl | missionId | Tên hiển thị | eventType | targetItemId | Target | Thưởng |
|----|-----------|--------------|-----------|--------------|--------|--------|
| 1 | m_l1_plant_rice | Trồng 6 cây lúa | PlantItem | rice | 6 | 30 Coin |
| 1 | m_l1_harvest_rice | Thu hoạch 6 lúa | HarvestItem | rice | 6 | 40 Coin |
| 1 | m_l1_plant_flower | Trồng 2 bông hoa | PlantItem | huong_duong | 2 | 30 Coin |
| 1 | m_l1_reach_l2 | Lên cấp 2 | ReachLevel | any | 2 | 50 Coin + 1 Diamond |
| 2 | m_l2_feed_chicken | Cho gà ăn 1 lần | FeedAnimal | any | 1 | 30 Coin |
| 2 | m_l2_collect_egg | Thu 1 quả trứng | CollectProduct | egg | 1 | 40 Coin |
| 2 | m_l2_deliver_1 | Giao 1 đơn hàng | DeliverOrder | any | 1 | 60 Coin |
| 3 | m_l3_buy_seed | Mua 1 loại hạt mới | BuyItem | seed | 1 | 40 Coin |
| 3 | m_l3_deliver_3 | Hoàn thành 3 đơn | DeliverOrder | any | 3 | 100 Coin |
| 3 | m_l3_harvest_veg | Thu hoạch 8 nông sản | HarvestItem | any | 8 | 70 Coin |
| 4 | m_l4_buy_pig | Mua chuồng heo | BuyItem | pen_pig | 1 | 120 Coin |
| 4 | m_l4_feed_pig | Cho heo ăn 2 lần | FeedAnimal | pen_pig | 2 | 80 Coin |
| 4 | m_l4_collect_pork | Thu 1 thịt heo | CollectProduct | pork | 1 | 90 Coin |
| 5 | m_l5_cook_first | Nấu món ăn đầu tiên | CookDish | any | 1 | 100 Coin + 1 Diamond |
| 5 | m_l5_deliver_dish | Giao 1 món ăn | DeliverOrderWithItem | dish | 1 | 120 Coin |
| 5 | m_l5_reach_l6 | Lên cấp 6 | ReachLevel | any | 6 | 120 Coin |
| 6 | m_l6_cook_3 | Nấu 3 món ăn | CookDish | any | 3 | 150 Coin |
| 6 | m_l6_buy_cow | Mua chuồng bò | BuyItem | pen_cow | 1 | 180 Coin |
| 6 | m_l6_collect_milk | Thu 3 sữa bò | CollectProduct | milk | 3 | 140 Coin |
| 7 | m_l7_deliver_5 | Giao 5 đơn hàng | DeliverOrder | any | 5 | 200 Coin |
| 7 | m_l7_harvest_10 | Thu hoạch 10 nông sản | HarvestItem | any | 10 | 150 Coin |
| 7 | m_l7_plant_sugarcane | Trồng 4 mía | PlantItem | sugarcane | 4 | 120 Coin |
| 8 | m_l8_cook_beef | Nấu 1 món bò | CookBeefDish | any | 1 | 200 Coin + 2 Diamond |
| 8 | m_l8_deliver_beef | Giao đơn có thịt bò | DeliverOrderWithItem | beef | 1 | 220 Coin |
| 8 | m_l8_combo_2 | Giao 2 đơn combo | DeliverComboOrder | any | 2 | 200 Coin |
| 9 | m_l9_combo_3 | Giao 3 đơn combo | DeliverComboOrder | any | 3 | 300 Coin |
| 9 | m_l9_cook_5 | Nấu 5 món ăn | CookDish | any | 5 | 250 Coin |
| 9 | m_l9_buy_decor | Đặt 1 trang trí | PlaceDecor ★ | any | 1 | 150 Coin |
| 10 | m_l10_reach_l10 | Đạt cấp 10 | ReachLevel | any | 10 | 500 Coin + 5 Diamond |
| 10 | m_l10_orders_20 | Hoàn thành 20 đơn (tổng) | TotalOrders | any | 20 | 400 Coin |
| 10 | m_l10_harvest_50 | Thu hoạch 50 nông sản (tổng) | HarvestItem | any | 50 | 350 Coin |

### Giai đoạn giữa L11–L20 (chế biến + hồ cá + mở rộng)

| Lvl | missionId | Tên hiển thị | eventType | targetItemId | Target | Thưởng |
|----|-----------|--------------|-----------|--------------|--------|--------|
| 11 | m_l11_buy_mill | Mua Máy Xay Bột | BuyItem | machine_mill | 1 | 400 Coin |
| 11 | m_l11_make_flour | Xay 4 bột gạo | ProcessItem ★ | flour | 4 | 300 Coin |
| 11 | m_l11_deliver_8 | Giao 8 đơn (tổng tích luỹ) | TotalOrders | any | 8 | 350 Coin |
| 12 | m_l12_upgrade_store | Nâng cấp kho 1 lần | UpgradeStorage ★ | any | 1 | 300 Coin + 2 Diamond |
| 12 | m_l12_cook_10 | Nấu 10 món (tổng) | CookDish | any | 10 | 400 Coin |
| 12 | m_l12_harvest_corn | Thu hoạch 20 ngô | HarvestItem | ngo | 20 | 320 Coin |
| 13 | m_l13_buy_press | Mua Máy Ép Mía | BuyItem | machine_press | 1 | 500 Coin |
| 13 | m_l13_make_juice | Ép 4 nước mía | ProcessItem ★ | sugar_juice | 4 | 350 Coin |
| 13 | m_l13_deliver_combo5 | Giao 5 đơn combo | DeliverComboOrder | any | 5 | 450 Coin |
| 14 | m_l14_process_8 | Chế biến 8 sản phẩm (tổng) | ProcessItem ★ | any | 8 | 400 Coin |
| 14 | m_l14_orders_40 | Hoàn thành 40 đơn (tổng) | TotalOrders | any | 40 | 500 Coin |
| 14 | m_l14_cook_beef3 | Nấu 3 món bò | CookBeefDish | any | 3 | 420 Coin |
| 15 | m_l15_buy_cheese | Mua Máy Phô Mai | BuyItem | machine_cheese | 1 | 700 Coin |
| 15 | m_l15_make_cheese | Làm 3 phô mai | ProcessItem ★ | cheese | 3 | 500 Coin + 3 Diamond |
| 15 | m_l15_reach_l15 | Đạt cấp 15 | ReachLevel | any | 15 | 600 Coin |
| 16 | m_l16_unlock_pond | Mở Hồ Cá | BuyItem | fish_pond | 1 | 700 Coin |
| 16 | m_l16_catch_fish | Câu 3 con cá | CatchFish ★ | any | 3 | 450 Coin |
| 16 | m_l16_cook_fish | Nấu 1 món cá | CookDish | fish_dish | 1 | 500 Coin |
| 17 | m_l17_catch_10 | Câu 10 con cá (tổng) | CatchFish ★ | any | 10 | 550 Coin |
| 17 | m_l17_deliver_fish | Giao 3 đơn có cá | DeliverOrderWithItem | fish | 3 | 600 Coin |
| 17 | m_l17_harvest_100 | Thu hoạch 100 nông sản (tổng) | HarvestItem | any | 100 | 500 Coin |
| 18 | m_l18_expand_land | Mở rộng đất 1 khu | BuyItem | land_expand | 1 | 800 Coin + 3 Diamond |
| 18 | m_l18_cook_15 | Nấu 15 món (tổng) | CookDish | any | 15 | 650 Coin |
| 18 | m_l18_orders_60 | Hoàn thành 60 đơn (tổng) | TotalOrders | any | 60 | 700 Coin |
| 19 | m_l19_process_20 | Chế biến 20 sản phẩm (tổng) | ProcessItem ★ | any | 20 | 700 Coin |
| 19 | m_l19_combo_10 | Giao 10 đơn combo (tổng) | DeliverComboOrder | any | 10 | 750 Coin |
| 19 | m_l19_decor_5 | Đặt 5 trang trí (tổng) | PlaceDecor ★ | any | 5 | 600 Coin |
| 20 | m_l20_reach_l20 | Đạt cấp 20 | ReachLevel | any | 20 | 1200 Coin + 8 Diamond |
| 20 | m_l20_orders_100 | Hoàn thành 100 đơn (tổng) | TotalOrders | any | 100 | 1000 Coin |
| 20 | m_l20_catch_30 | Câu 30 con cá (tổng) | CatchFish ★ | any | 30 | 800 Coin |

### Giai đoạn cuối L21–L30 (tàu du lịch + sự kiện + bậc thầy)

| Lvl | missionId | Tên hiển thị | eventType | targetItemId | Target | Thưởng |
|----|-----------|--------------|-----------|--------------|--------|--------|
| 21 | m_l21_cook_25 | Nấu 25 món (tổng) | CookDish | any | 25 | 850 Coin |
| 21 | m_l21_harvest_200 | Thu hoạch 200 nông sản (tổng) | HarvestItem | any | 200 | 800 Coin |
| 21 | m_l21_upgrade_store2 | Nâng kho lần 2 | UpgradeStorage ★ | any | 1 | 700 Coin + 3 Diamond |
| 22 | m_l22_process_40 | Chế biến 40 sản phẩm (tổng) | ProcessItem ★ | any | 40 | 900 Coin |
| 22 | m_l22_deliver_fish10 | Giao 10 đơn có cá (tổng) | DeliverOrderWithItem | fish | 10 | 850 Coin |
| 22 | m_l22_orders_150 | Hoàn thành 150 đơn (tổng) | TotalOrders | any | 150 | 1000 Coin |
| 23 | m_l23_unlock_boat | Mở Bến Tàu Du Lịch | BuyItem | tourist_boat | 1 | 1200 Coin |
| 23 | m_l23_serve_boat | Phục vụ 1 chuyến tàu | ServeBoat ★ | any | 1 | 800 Coin + 3 Diamond |
| 23 | m_l23_cook_beef5 | Nấu 5 món bò (tổng) | CookBeefDish | any | 5 | 850 Coin |
| 24 | m_l24_serve_3 | Phục vụ 3 chuyến tàu (tổng) | ServeBoat ★ | any | 3 | 1000 Coin |
| 24 | m_l24_combo_20 | Giao 20 đơn combo (tổng) | DeliverComboOrder | any | 20 | 950 Coin |
| 24 | m_l24_decor_10 | Đặt 10 trang trí (tổng) | PlaceDecor ★ | any | 10 | 800 Coin |
| 25 | m_l25_reach_l25 | Đạt cấp 25 | ReachLevel | any | 25 | 1500 Coin + 10 Diamond |
| 25 | m_l25_orders_250 | Hoàn thành 250 đơn (tổng) | TotalOrders | any | 250 | 1300 Coin |
| 25 | m_l25_serve_5 | Phục vụ 5 chuyến tàu (tổng) | ServeBoat ★ | any | 5 | 1100 Coin |
| 26 | m_l26_cook_40 | Nấu 40 món (tổng) | CookDish | any | 40 | 1100 Coin |
| 26 | m_l26_catch_60 | Câu 60 con cá (tổng) | CatchFish ★ | any | 60 | 1000 Coin |
| 26 | m_l26_process_70 | Chế biến 70 sản phẩm (tổng) | ProcessItem ★ | any | 70 | 1150 Coin |
| 27 | m_l27_harvest_350 | Thu hoạch 350 nông sản (tổng) | HarvestItem | any | 350 | 1100 Coin |
| 27 | m_l27_serve_10 | Phục vụ 10 chuyến tàu (tổng) | ServeBoat ★ | any | 10 | 1300 Coin + 4 Diamond |
| 27 | m_l27_orders_350 | Hoàn thành 350 đơn (tổng) | TotalOrders | any | 350 | 1400 Coin |
| 28 | m_l28_cook_55 | Nấu 55 món (tổng) | CookDish | any | 55 | 1300 Coin |
| 28 | m_l28_decor_20 | Đặt 20 trang trí (tổng) | PlaceDecor ★ | any | 20 | 1100 Coin |
| 28 | m_l28_process_100 | Chế biến 100 sản phẩm (tổng) | ProcessItem ★ | any | 100 | 1400 Coin |
| 29 | m_l29_orders_450 | Hoàn thành 450 đơn (tổng) | TotalOrders | any | 450 | 1600 Coin |
| 29 | m_l29_serve_15 | Phục vụ 15 chuyến tàu (tổng) | ServeBoat ★ | any | 15 | 1500 Coin + 5 Diamond |
| 29 | m_l29_combo_35 | Giao 35 đơn combo (tổng) | DeliverComboOrder | any | 35 | 1500 Coin |
| 30 | m_l30_reach_l30 | Đạt cấp 30 — Bậc thầy Nông trại | ReachLevel | any | 30 | 3000 Coin + 20 Diamond |
| 30 | m_l30_orders_500 | Hoàn thành 500 đơn (tổng) | TotalOrders | any | 500 | 2000 Coin |
| 30 | m_l30_cook_70 | Nấu 70 món (tổng) | CookDish | any | 70 | 1800 Coin |

> **Tổng nhiệm vụ chính:** ~91 mission (4 ở L1, 3/level từ L2→L30). Phần thưởng tăng dần khớp lạm phát
> kinh tế; kim cương rơi ở các mốc tròn (L1,5,8,10,12,15,18,20,21,23,25,27,29,30) để gem luôn nhỏ giọt.

---

## B. NHIỆM VỤ NGÀY (Daily) — mở từ L6

- Mỗi ngày random chọn **3** mục từ pool dưới (seed theo `yyyyMMdd` để mọi lần mở popup trong ngày giống nhau).
- Reset khi sang ngày mới (so `mission_daily_date`). `kind = Daily`, `requiredLevel = 6`.
- Target nhỏ, làm trong 1 phiên. Thưởng 40–90 Coin hoặc 1 Diamond.

| dailyId | Tên hiển thị | eventType | targetItemId | Target | Thưởng |
|---------|--------------|-----------|--------------|--------|--------|
| d_harvest | Thu hoạch 15 nông sản | HarvestItem | any | 15 | 60 Coin |
| d_plant | Trồng 10 cây bất kỳ | PlantItem | any | 10 | 50 Coin |
| d_deliver | Giao 3 đơn hàng | DeliverOrder | any | 3 | 80 Coin |
| d_cook | Nấu 2 món ăn | CookDish | any | 2 | 70 Coin |
| d_feed | Cho thú ăn 4 lần | FeedAnimal | any | 4 | 60 Coin |
| d_egg | Thu 6 quả trứng | CollectProduct | egg | 6 | 60 Coin |
| d_buy_seed | Mua 3 hạt giống | BuyItem | seed | 3 | 50 Coin |
| d_flower | Thu hoạch 4 bông hoa | HarvestItem | flower | 4 | 60 Coin |
| d_process | Chế biến 3 sản phẩm | ProcessItem ★ | any | 3 | 80 Coin |
| d_combo | Giao 1 đơn combo | DeliverComboOrder | any | 1 | 70 Coin |
| d_fish | Câu 5 con cá | CatchFish ★ | any | 5 | 80 Coin |
| d_gem | Hoàn thành mọi daily hôm nay | (tổng hợp) | — | 3/3 | 1 Diamond |

> `d_process` chỉ vào pool khi player có máy chế biến (L11+); `d_fish` khi có hồ cá (L16+).
> Có thể lọc pool theo level để daily không đòi thứ chưa mở.

---

## C. THÀNH TỰU (Achievement) — dài hạn, nhận 1 lần

Mốc lớn xuyên suốt game, nhận thưởng to. `kind = Main`, `requiredLevel = 1` (luôn hiện, tiến độ tích luỹ).

| achId | Tên hiển thị | eventType | Target | Thưởng |
|-------|--------------|-----------|--------|--------|
| a_harvest_100 | Nông dân tập sự — thu 100 nông sản | HarvestItem (any) | 100 | 200 Coin |
| a_harvest_500 | Nông dân lành nghề — thu 500 nông sản | HarvestItem (any) | 500 | 600 Coin + 3 Diamond |
| a_harvest_2000 | Nông dân huyền thoại — thu 2000 nông sản | HarvestItem (any) | 2000 | 2000 Coin + 10 Diamond |
| a_orders_50 | Người giao hàng — 50 đơn | TotalOrders | 50 | 300 Coin |
| a_orders_300 | Thương lái — 300 đơn | TotalOrders | 300 | 1000 Coin + 5 Diamond |
| a_cook_30 | Đầu bếp nhỏ — nấu 30 món | CookDish | 30 | 400 Coin |
| a_cook_150 | Bếp trưởng — nấu 150 món | CookDish | 150 | 1200 Coin + 5 Diamond |
| a_process_50 | Thợ chế biến — 50 sản phẩm | ProcessItem ★ | 50 | 500 Coin |
| a_fish_100 | Ngư dân — câu 100 cá | CatchFish ★ | 100 | 800 Coin + 3 Diamond |
| a_boat_25 | Chủ bến tàu — phục vụ 25 chuyến | ServeBoat ★ | 25 | 1500 Coin + 8 Diamond |
| a_decor_30 | Nhà trang trí — đặt 30 decor | PlaceDecor ★ | 30 | 700 Coin |
| a_level_10 | Khởi đầu vững — đạt L10 | ReachLevel | 10 | 300 Coin |
| a_level_20 | Vươn xa — đạt L20 | ReachLevel | 20 | 800 Coin + 5 Diamond |
| a_level_30 | Bậc thầy Nông trại — đạt L30 | ReachLevel | 30 | 3000 Coin + 20 Diamond |
| a_daily_7 | Chăm chỉ — hoàn thành daily 7 ngày | (streak) | 7 | 500 Coin + 3 Diamond |
| a_daily_30 | Trung thành — daily 30 ngày | (streak) | 30 | 2000 Coin + 15 Diamond |

---

## D. Ghi chú triển khai (cho autopilot / tools-programmer)

1. **Sửa lỗi nền trước** (M1-1..M1-4): nâng schema, tracker key chuẩn + persist + event, hook 8 điểm,
   UI lọc theo level. Chi tiết file/dòng: `MISSIONS_L1_L10_PROPOSAL.md` §5.
2. **eventType mới (★)** thêm vào enum đúng milestone phát sinh cơ chế (đừng thêm sớm gây code chết):
   `ProcessItem` (M3/L11), `UpgradeStorage` (M3-2), `CatchFish`/`ServeBoat`/`PlaceDecor` (M3-7/L16+).
   Trước khi cơ chế tồn tại, mission dùng nó để `requiredLevel` tương ứng → không lọt vào pool.
3. **Tool sinh asset:** mở rộng `Tools → Farm Game → Setup Missions` đọc bảng A/B/C, tạo asset
   `Mission_<missionId>.asset` + `MissionDatabase_Main`/`_Daily`/`_Achievement`, có report + undo.
4. **Thưởng kép Coin+Diamond:** tạo 2 asset hoặc dùng field `secondReward*` (M1-1). Đừng bỏ phần Diamond.
5. **Cân bằng:** tổng Coin từ mission L1→L30 ≈ vài chục nghìn — là *bonus* phụ trên nguồn chính (đơn hàng),
   không thay thế. Diamond nhỏ giọt ~120–150 viên cả hành trình (hợp F2P, không phá gem sink).
6. Sau khi sinh data: chạy `/balance-check` để xác nhận mission không tạo lạm phát vàng ngoài ý muốn.
