# BÁO CÁO ĐỐI CHIẾU DATA — Asset thật vs Design docs

Ngày xuất: 2026-08-19 · Nguồn asset: `Assets/_Game/Farm/data`, `Assets/_Game/Data/Data_cooking` · Docs: `production/*.md`, `production/session-state/*.md`

Tổng số điểm lệch/ghi nhận: **74** — Cao: 19 · Trung bình: 28 · Thấp: 25 · Đã khớp/đã sửa: 2

## Cây trồng vs ECONOMY_TABLE §3

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| seed_bapcai (Bắp Cải) | unlock L1, hạt 28, grow 70s, sell 10, EXP 7 | unlock L1, hạt 45, grow 300s, sell 15, EXP 6 | Thấp | giá hạt: asset 28 ≠ doc 45; grow s: asset 70 ≠ doc 300; sell/đv: asset 10 ≠ doc 15; EXP: asset 7 ≠ doc 6 |
| seed_cachua (Cà Chua) | unlock L3, hạt 52, grow 145s, sell 20, EXP 14 | unlock L3, hạt 65, grow 480s, sell 20, EXP 8 | Thấp | giá hạt: asset 52 ≠ doc 65; grow s: asset 145 ≠ doc 480; EXP: asset 14 ≠ doc 8 |
| ca_rot (Cà Rốt) | unlock L3, hạt 45, grow 120s, sell 17, EXP 12 | unlock L3, hạt 50, grow 400s, sell 16, EXP 7 | Thấp | giá hạt: asset 45 ≠ doc 50; grow s: asset 120 ≠ doc 400; sell/đv: asset 17 ≠ doc 16; EXP: asset 12 ≠ doc 7 |
| seed_rice (Lúa) | unlock L1, hạt 20, grow 50s, sell 7, EXP 5 | unlock L1, hạt 20, grow 180s, sell 7, EXP 5 | Thấp | grow s: asset 50 ≠ doc 180 |
| khoai_tay (Khoai Tây) | unlock L5, hạt 71, grow 220s, sell 30, EXP 22 | unlock L5, hạt 80, grow 500s, sell 25, EXP 9 | Thấp | giá hạt: asset 71 ≠ doc 80; grow s: asset 220 ≠ doc 500; sell/đv: asset 30 ≠ doc 25; EXP: asset 22 ≠ doc 9 |
| seed_sugarcane (Mía) | unlock L7, hạt 96, grow 340s, sell 46, EXP 34 | unlock L7, hạt 120, grow 420s, sell 36, EXP 10 | Thấp | giá hạt: asset 96 ≠ doc 120; grow s: asset 340 ≠ doc 420; sell/đv: asset 46 ≠ doc 36; EXP: asset 34 ≠ doc 10 |
| seed_ngo (Ngô) | unlock L2, hạt 35, grow 95s, sell 13, EXP 10 | unlock L2, hạt 40, grow 360s, sell 13, EXP 7 | Thấp | giá hạt: asset 35 ≠ doc 40; grow s: asset 95 ≠ doc 360; EXP: asset 10 ≠ doc 7 |
| seed_chili (Ớt) | unlock L9, hạt 127, grow 500s, sell 68, EXP 50 | unlock L9, hạt 170, grow 540s, sell 48, EXP 12 | Thấp | giá hạt: asset 127 ≠ doc 170; grow s: asset 500 ≠ doc 540; sell/đv: asset 68 ≠ doc 48; EXP: asset 50 ≠ doc 12 |
| seed_pepper (Tiêu) | unlock L10, hạt 134, grow 560s, sell 76, EXP 56 | unlock L10, hạt 190, grow 660s, sell 55, EXP 14 | Thấp | giá hạt: asset 134 ≠ doc 190; grow s: asset 560 ≠ doc 660; sell/đv: asset 76 ≠ doc 55; EXP: asset 56 ≠ doc 14 |
| seed_lemon (Chanh) | unlock L8, hạt 105, grow 380s, sell 52, EXP 38 | unlock L8, hạt 130, grow 780s, sell 38, EXP 12 | Thấp | giá hạt: asset 105 ≠ doc 130; grow s: asset 380 ≠ doc 780; sell/đv: asset 52 ≠ doc 38; EXP: asset 38 ≠ doc 12 |
| seed_nam (Nấm) | unlock L6, hạt 76, grow 250s, sell 34, EXP 25 | unlock L6, hạt 100, grow 600s, sell 30, EXP 10 | Thấp | giá hạt: asset 76 ≠ doc 100; grow s: asset 250 ≠ doc 600; sell/đv: asset 34 ≠ doc 30; EXP: asset 25 ≠ doc 10 |
| seed_hoa_hong (Hoa Hồng) | unlock L4, hạt 57, grow 170s, sell 23, EXP 17 | unlock L4, hạt 80, grow 180s, sell 24, EXP 5 | Thấp | giá hạt: asset 57 ≠ doc 80; grow s: asset 170 ≠ doc 180; sell/đv: asset 23 ≠ doc 24; EXP: asset 17 ≠ doc 5 |
| seed_hoa_oai_huong (Hoa Oải Hương) | unlock L4, hạt 67, grow 195s, sell 27, EXP 20 | unlock L4, hạt 100, grow 180s, sell 30, EXP 5 | Thấp | giá hạt: asset 67 ≠ doc 100; grow s: asset 195 ≠ doc 180; sell/đv: asset 27 ≠ doc 30; EXP: asset 20 ≠ doc 5 |
| seed_huong_duong (Hướng Dương) | unlock L1, hạt 23, grow 55s, sell 8, EXP 6 | unlock L1, hạt 35, grow 180s, sell 12, EXP 5 | Thấp | giá hạt: asset 23 ≠ doc 35; grow s: asset 55 ≠ doc 180; sell/đv: asset 8 ≠ doc 12; EXP: asset 6 ≠ doc 5 |

## Món ăn vs BANG_MON_AN_30

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| sup_ngo_nam (Súp ngô nấm) | unlockLevel 6 | mốc cấp 10 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| bap_cai_xao_nam (Bắp cải xào nấm) | unlockLevel 6 | mốc cấp 10 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| nuoc_mia_chanh (Nước mía chanh) | unlockLevel 8 | mốc cấp 10 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| salad_bap_cai_chanh (Salad bắp cải chanh) | unlockLevel 8 | mốc cấp 10 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| salad_nam_rau (Salad nấm và rau) | unlockLevel 7 | mốc cấp 15 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| thit_heo_luoc_cuon_rau (Thịt heo luộc cuốn rau) | unlockLevel 7 | mốc cấp 15 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| nam_xao_thit_bo (Nấm xào thịt bò) | unlockLevel 8 | mốc cấp 15 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| trung_op_la_bo_ne (Trứng ốp la bò né) | unlockLevel 8 | mốc cấp 15 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| canh_khoai_tay_thit_heo (Canh khoai tây thịt heo) | unlockLevel 6 | mốc cấp 20 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| bo_ham_ca_rot (Bò hầm cà rốt) | unlockLevel 8 | mốc cấp 20 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| ga_xao_ot (Gà xào ớt) | unlockLevel 9 | mốc cấp 20 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| bo_xao_tieu (Bò xào tiêu) | unlockLevel 10 | mốc cấp 25 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| pho_bo_tai (Phở bò tái) | unlockLevel 9 | mốc cấp 30 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| suon_heo_xao_chua_ngot (Sườn heo xào chua ngọt) | unlockLevel 9 | mốc cấp 30 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| ga_nuong_lu (Gà nướng lu mật mía) | unlockLevel 7 | mốc cấp 30 | Trung bình | Asset dồn toàn bộ món về L5–L10, chưa áp lịch 6 mốc (5/10/15/20/25/30) |
| Cơm chiên thịt gà | CHƯA CÓ asset DishData | thiết kế mốc cấp 5 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Súp bắp cải thịt heo | CHƯA CÓ asset DishData | thiết kế mốc cấp 5 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Sữa chua | CHƯA CÓ asset DishData | thiết kế mốc cấp 10 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Trà hoa lan mật ong | CHƯA CÓ asset DishData | thiết kế mốc cấp 15 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Bánh flan | CHƯA CÓ asset DishData | thiết kế mốc cấp 20 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Cơm chiên trứng vàng | CHƯA CÓ asset DishData | thiết kế mốc cấp 20 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Cá nướng tiêu | CHƯA CÓ asset DishData | thiết kế mốc cấp 25 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Canh chua cá | CHƯA CÓ asset DishData | thiết kế mốc cấp 25 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Kem hoa oải hương | CHƯA CÓ asset DishData | thiết kế mốc cấp 25 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Sữa chua sữa non mật ong | CHƯA CÓ asset DishData | thiết kế mốc cấp 25 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Nấm truffle xào thịt bò | CHƯA CÓ asset DishData | thiết kế mốc cấp 30 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| Cá quý nướng muối ớt | CHƯA CÓ asset DishData | thiết kế mốc cấp 30 | Cao | 12/30 món thiết kế chưa được tạo (gồm 2 món cá — hệ cá chưa tồn tại) |
| 2 món cá (Cá nướng tiêu, Canh chua cá) | KHÔNG tồn tại asset nào — cũng KHÔNG có món unlockLevel 99 trong data | Ghi chú đã biết: '2 món cá unlock level 99' | Trung bình | Kiểm tra toàn bộ Assets: không có unlockLevel: 99; món cá chưa được tạo. ING_Fish cũng không có trong Data_cooking |
| Món Hard trước cấp 20 | canh_khoai_tay_thit_heo Hard@L6, bo_ham_ca_rot Hard@L8, pho_bo_tai Hard@L9, suon_heo_xao_chua_ngot Hard@L9 | Thiết kế: không món Hard nào trước cấp 20 | Trung bình |  |

## Nguyên liệu / ID

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| chicken vs chicken_meat | Pen03 sản xuất 'chicken_meat'; ING_Chicken.id = 'chicken' (2 món gà yêu cầu 'chicken') | BANG_MON_AN_30 việc #2: phải đồng bộ 2 id này | Cao | Lệch id vẫn CHƯA sửa trong asset |
| sugar (Đường) trong Chợ | MarketDatabase KHÔNG có 'sugar' (soysauce đã có, L4) | BANG_MON_AN_30 việc #3: thêm soysauce + sugar vào Chợ | Cao | soysauce đã thêm ✓, sugar còn thiếu → SEA_Sugar không có nguồn mua |
| FlavorVector 21 nguyên liệu | ĐÃ ĐIỀN — không còn vector 0,0,0,0,0 (vd beef umami3/texture2) | BANG_MON_AN_30 cảnh báo 'cả 21 vector = 0' | OK-đã sửa | Việc #1 của doc đã hoàn thành sau khi doc viết |
| Field stars thiếu | cachua, milk không có field 'stars' trong asset | các nguyên liệu khác stars=3 | Thấp | ghi '?' trong Excel |
| ID đặc biệt (đã biết, xác nhận đúng) | nấm: seed 'seed_nam' → thu hoạch 'mushroom'; cà rốt seed = 'ca_rot', khoai tây seed = 'khoai_tay' (không prefix seed_) | quy ước chung seed_* | Thấp | Cần chú ý khi import Firebase: key không đồng nhất |
| 5 nguyên liệu hiếm (mat_ong, trung_vang, sua_non, truffle, ca_quy) | KHÔNG có IngredientData nào trong Data_cooking | BANG_MON_AN_30: cần tạo 5 IngredientData + RareDropRoller | Cao |  |
| Rau thơm (herbs) nguồn cung | Không có CropData nào cho ra 'herbs'; herbs chỉ mua ở Chợ (L3, giá 27) | DANH_SACH_NGUYEN_LIEU: cần cây rau thơm (4 sprite) | Trung bình | Hiện giải quyết tạm bằng bán ở chợ — 4 món dùng herbs vẫn nấu được |

## Chợ (Market)

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| milk unlock L13 | milk bán ở chợ mở L13 (cat 4) | PenConfig bò sữa/kinh tế demo chỉ tới L10 (đơn bò L8) | Thấp | L13 > phạm vi L1-L10; cần xác nhận chủ ý |
| Ghi chú trong MarketDatabase | setupNotes: 'SINH TỰ ĐỘNG — KHÔNG GÕ TAY, 74 dòng, bỏ vì thiếu icon: 0, bỏ thủ công: 3' | — | Thấp | 3 item bị tắt thủ công (không rõ item nào) |

## Quà lên cấp vs REWARDS_MASTER_LIST

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| Vàng + gem L2→L30 | Khớp 100% bảng doc (tổng 35.990 vàng + 208 gem) | ≈36.200 vàng + ~180 gem | OK-khớp | Số vàng/gem từng cấp trùng doc |
| Quà vật phẩm L10–L30 | Chỉ tặng hạt giống (seed_*), vd L10: 3× seed_pepper, L30: 5× seed_pepper | Doc hứa booster/pet/skin/decor/title (L10 booster_fertilizer + decor_arch_l10; L19 pet_cat; L30 pet_legendary + skin_legendary...) | Cao | Toàn bộ hệ booster/pet/skin/decor/title CHƯA có trong data |
| LevelReward_L1 / gà tặng L2 | Không có LevelReward_L1 (bắt đầu từ L2); L2 chỉ tặng 3× seed_ngo | L1 starter 400 vàng+15 gem+hạt; L2 tặng kèm pen_chicken | Trung bình | Starter pack & chuồng gà tặng chưa thấy trong LevelReward asset |

## Nhiệm vụ Main

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| Số lượng & phạm vi | 307 mission trong Main_L1_L10 (thực tế phủ L1→L30, 6–16 mission/cấp) | MISSIONS_MASTER_LIST: ~91 mission (3/cấp, L1 có 4) | Trung bình | Asset nhiều gấp ~3,4 lần doc; folder tên 'Main_L1_L10' nhưng chứa cả L11–L30 |
| Quy ước missionId | main_l{n}_... (vd main_l4_collect_pork_1) | m_l{n}_... (vd m_l4_collect_pork) | Thấp |  |
| Thưởng gem | Chỉ 6/307 mission thưởng gem (các mốc ReachLevel 2/6/10/20/25/30) | Doc: gem rơi ở L1,5,8,10,12,15,18,20,21,23,25,27,29,30 (mission thưởng kép) | Trung bình | MissionData chỉ có 1 rewardType — chưa có 'thưởng kép' như doc yêu cầu |
| targetItemId 'pho_beef' KHÔNG tồn tại | 12 mission trỏ 'pho_beef' (vd proc_c_15_2) | dish id thật là 'pho_bo_tai' | Cao | Các mission này không bao giờ hoàn thành được — tiến độ không khớp item nào |
| targetItemId dạng SỐ | 6 mission dùng id số [100, 106, 108, 120, 121, 122] | itemId dạng chuỗi (rice, egg...) | Cao | Id số không khớp itemId chuỗi nào trong catalog → mission chết |
| eventType mới (ProcessItem, CatchFish, ServeBoat, UpgradeStorage, PlaceDecor) | KHÔNG có trong enum MissionEventType (11 giá trị); mission máy chế biến dùng tạm CollectAnimalProduct | Doc yêu cầu thêm ★ eventType theo milestone | Trung bình |  |

## Nhiệm vụ Daily

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| 16 asset nhưng DB chỉ đăng ký 10 | 6 asset mồ côi không nằm trong MissionDatabase_Daily: Mission_daily_buy_seed, Mission_daily_collect_8, Mission_daily_cook_2, Mission_daily_deliver_3, Mission_daily_feed_2, Mission_daily_harvest_20 | Pool doc có 12 daily | Trung bình | Bộ daily_* cũ trùng chức năng bộ d_* mới |
| d_fish, d_gem | KHÔNG có asset | Doc pool §B có d_fish (câu 5 cá) và d_gem (xong 3/3 → 1 gem) | Trung bình |  |
| d_flower | targetItemId='huong_duong' — chỉ đếm hướng dương | Doc: 'Thu hoạch 4 bông hoa' (flower bất kỳ) | Thấp |  |
| d_combo / d_process | d_combo dùng DeliverOrder (không phân biệt combo); d_process dùng CollectAnimalProduct + requiredLevel 11 | Doc: DeliverComboOrder / ProcessItem | Trung bình | eventType chưa tồn tại nên gắn tạm |

## Thành tựu

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| Số lượng | 230 asset (11 nhóm; 5 nhóm 'a_proc_*' dạng chuỗi mốc 15–30 bậc; a_reach_level_2..100 = 99 asset); DB đăng ký 157 | Doc §C chỉ có 16 achievement | Trung bình | 73 asset mồ côi ngoài DB (70 a_reach_level + a_level_10/20/30) |
| a_reach_level_31..100 | Tồn tại 70 asset đạt cấp 31→100 nhưng maxLevel code = 30 (CapToiDa) | PlayerProgressManager.CapToiDa = 30 | Cao | 70 thành tựu không thể đạt; DB chỉ nhận tới a_reach_level_30 |
| a_daily_7 / a_daily_30 (streak) | KHÔNG có asset | Doc §C có 2 achievement streak | Thấp |  |
| a_fish_100, a_boat_25, a_decor_30, a_process_50 | a_process_1 tồn tại (không phải _50); fish/boat/decor không có | Doc §C | Thấp |  |

## Nhiệm vụ (chung)

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| 20 mission cũ ở gốc Data_Ewa | Schema cũ (không missionId/eventType), KHÔNG nằm trong DB nào — data chết | — | Thấp | Mission_bapcai, Mission_beef, ... Mission_pho_beef; cân nhắc xoá trước khi import Firebase |

## Level & EXP

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| Công thức EXP | Code: 40 + 10n + 3n²/20 (integer), maxLevel 30 — L2 cần 40, L10←142, L30←456 | ECONOMY_TABLE §1: 40 + 10n + n² (L10←184, L30←1061) | Trung bình | Code đã đổi sang curve nhẹ hơn sau khi doc viết; doc lỗi thời |

## Chuồng / Máy

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| Giá mua chuồng & mở khoá | PenMiniPanelConfig không chứa unlockLevel/giá mua (chỉ feed→product/duration/EXP) | ECONOMY_TABLE §4: gà tặng L2, heo 600 (L4), bò 1.500 (L6) | Thấp | Giá/level chuồng nằm ngoài các asset đã export — ghi '?' trong Excel |
| Thức ăn chuồng heo | pen_02 (heo) ăn bapcai + carot; pen_04 (bò sữa) ăn carot + khoaitay | — | Thấp | Ghi nhận để đối chiếu kinh tế thức ăn |

## Chính tả displayName

| Mục | Asset thật | Design doc | Mức độ | Ghi chú |
|---|---|---|---|---|
| Item kho món ăn (Farm_dong_vat) | 'Bò hầm cà rót' (sai dấu), 'Canh khoai tay thit heo' (thiếu dấu), 'Súp ngo nấm', 'Trứng óp la bò né', 'Salad nấm rau', 'Gà nướng lu' (thiếu 'mật mía') | DishData: 'Bò hầm cà rốt', 'Canh khoai tây thịt heo', 'Súp ngô nấm', 'Trứng ốp la bò né'... | Trung bình | displayName item kho lệch chính tả so với tên món trong DishData — hiện sai trong UI kho |

## Ghi chú phương pháp

- Số liệu 'Asset thật' parse trực tiếp từ Unity YAML (.asset); không sửa file gốc.
- ECONOMY_TABLE là bản NHÁP 'chờ duyệt' (2026-06-12) — lệch số có thể do đã re-balance sau doc; unlock level cây trồng khớp 100%.
- REWARDS_MASTER_LIST khớp hoàn toàn phần vàng/gem lên cấp L2–L30; chỉ lệch phần quà vật phẩm (booster/pet/skin chưa tồn tại).
- Field không parse được ghi '?' trong Excel (vd stars của cachua/milk, giá mua chuồng).