# L1→L10 IMPLEMENTATION REPORT — Session 2026-06-12

> Trạng thái: Batch 1–4, 6, 8 ĐÃ IMPLEMENT + verify từng giá trị. Batch 5 (animal tutorial) & Batch 7 (mission) ở dạng proposal chờ duyệt.
> KHÔNG commit, KHÔNG push. Mọi giá trị data đã đối chiếu khớp 100% bảng kinh tế được duyệt.

## 1. Đã sửa/tạo file nào

**Code (7 file):**

| File | Thay đổi |
|------|----------|
| `Farm/Scripts/Managers/FarmEconomyManager.cs` | XOÁ debug override `Gold/Gems=1000` trong Editor; starter 1250/10 → **400 vàng / 15 gem** |
| `Farm/Scripts/Village/VillageOrderManager.cs` | MỚI: giới hạn nhà nhận đơn theo level (L1=4 → L9=8, chỉnh được trong Inspector); tỉ lệ đơn 2-item theo level (L1 0% / L2-3 20% / L4-6 35% / L7+ 50%); tối đa 1 món nấu mỗi đơn; class `HouseUnlockStep` |
| `Farm/Scripts/Data/PlaceableItemData.cs` | Thêm field `unlockLevel` (ShopLevelLockUI tự đọc qua reflection → công trình khoá được theo level) |
| `_Game/Scripts/Dish/DishData.cs` | Thêm field `unlockLevel` (mặc định 5) |
| `_Game/Scripts/Dish/DishCardUI.cs` | Thêm `SetLocked(bool)` — món khoá xám + không bấm được |
| `_Game/Scripts/Dish/DishBookUI.cs` | Lọc món theo `PlayerProgressManager.Level` (null = test bếp độc lập → mở hết) |
| `_Game/Scripts/CookingChallengeManager.cs` | `HandleCookingSuccess`: **+8 EXP** mỗi lần nấu thành công |

**Editor tools (2 file):**

| File | Thay đổi |
|------|----------|
| `Farm/Editor/LevelUpRewardDataSetupTool.cs` | Nâng L2-L6 → **L2-L10**, bảng reward khớp econ đã duyệt, idempotent |
| `Farm/Editor/DemoL1L10Tool.cs` | **MỚI** — menu `Tools → Farm Game → Demo L1-L10`: Check All (8 nhóm PASS/FAIL) / Setup All / Reset Demo Save / Print Playtest Checklist |

**Data (78 file .asset):** 21 crop/hoa (sell/unlock/grow/exp) · 33 village order (reward/unlock/fix nấm) · 20 món ăn (+unlockLevel) · 4 chuồng (giá+unlock, sửa tên Gà/Heo bị tráo) · LevelReward L2-L6 cập nhật + **L7-L10 tạo mới** (kèm .meta GUID mới) · prefab `DayNightWeatherSetup` (weather) · `SCN_Farm.unity` (chỉ 2 dòng startGold/startGems).

**Report:** `MISSIONS_L1_L10_PROPOSAL.md` (mới), file này.

## 2. Level 1→10 unlock những gì

| Level | Cây/Hoa | Công trình/Hệ thống | Nhà order |
|---|---|---|---|
| 1 | Lúa, Hướng dương, Bắp cải | Tutorial | 4 |
| 2 | Ngô | Chuồng gà (100g) | 4 |
| 3 | Cà chua, Cà rốt | Đơn trứng/thịt gà | 5 |
| 4 | Hoa hồng, Oải hương | Chuồng heo (600g) | 5 |
| 5 | Khoai tây | **BẾP** + 3 món đầu | 6 |
| 6 | Nấm | Chuồng bò (1.500g) + 3 món | 6 |
| 7 | Mía, Hoa lan, Cúc trắng | 2 món gà/heo | 7 |
| 8 | Chanh | Chuồng bò sữa (2.000g), đơn bò + 5 món | 7 |
| 9 | Ớt, Tulip, Cúc vạn thọ | 3 món cay/phở | 8 |
| 10 | Tiêu, 3 hoa còn lại | Món bò xào tiêu | 8 |

## 3. EXP curve (giữ code cũ: 40+10n+n²)
40 / 51 / 64 / 79 / 96 / 115 / 136 / 159 / 184 — tổng 924 EXP tới L10. Nguồn: thu hoạch 5–14/ô, đơn 3–10/đv, **nấu +8/món (mới)**, train 10/slot.

## 4. Starter: **400 vàng + 15 gem** (scene + script đồng bộ; quà level cộng thêm 3.200 vàng + 34 gem qua L2-L10).

## 5. Giá hạt (mua → thu 4 đv): lúa 20→28 · hướng dương 35→48 · bắp cải 45→60 · ngô 40→52 · cà chua 65→80 · cà rốt 50→64 · hoa hồng 80→96 · oải hương 100→120 · khoai 80→100 · nấm 100→120 · mía 120→144 · chanh 130→152 · ớt 170→192 · tiêu 190→220. **Bán chợ giờ luôn lời** (sửa lỗi sellGold=3 phẳng).

## 6. Giá công trình: gà 100 (L2) · heo 600 (L4) · bò 1.500 (L6) · bò sữa 2.000 (L8). Feed: 1 nông sản → 4 sản phẩm + 10 EXP (giữ nguyên config Pen).

## 7. Order reward: tỉ lệ ~2,1× giá bán (L1) giảm dần ~1,6× (L10). Fix nặng: đơn mía 14→60, đơn nấm sửa ID `nam`→`mushroom` (hết kẹt vĩnh viễn). Đơn món cá unlock 99 (loại khỏi demo).

## 8. Cooking L5: 3 món L5 (cơm chiên trứng, trứng chiên cà chua, khoai tây chiên) → 3 món L6 → 3 món L7 → 5 món L8 → 3 món L9 → 1 món L10, **tier theo công thức nguyên liệu thật** (đã dump recipe từng món). Món khoá hiện xám trong sách nấu. Nấu xong vào kho farm (luồng cũ, giữ nguyên).

## 9. Mission/Achievement: **CHƯA IMPLEMENT** — phát hiện bug tracker ghi theo itemId nhưng UI đọc theo missionName (progress không bao giờ hiện). Kế hoạch sửa tối thiểu + danh sách mission L1-L10 + daily đã viết tại `MISSIONS_L1_L10_PROPOSAL.md` — **chờ anh duyệt** vì phải đổi schema MissionData + móc 5 file gameplay.

## 10. Shop lock: data crop đã có unlock thật (trước đây toàn L1 — không có gì để khoá); công trình đã khoá được nhờ field mới. Overlay "Mở ở cấp X" là hệ sẵn có, tự ăn theo data.

## 11. Popup level-up: config đủ L2→L10 (asset đã tạo). ⚠ Cần chạy 1 lần trong Unity: `Tools → Farm Game → Setup Level Up Popup → Setup Reward Data (L2-L10)` để gán 9 config + icon vào popup trong scene, rồi lưu scene.

## 12. VFX Lana: dùng như tutorial/popup tool đã wire: `Confetti_blast_multicolor`, `Confetti_directional_multicolor`, `Flash_magic_blue_pink`. Chưa đụng thêm.

## 13. Playtest: tôi KHÔNG chạy được Unity từ môi trường này — đã verify 100% giá trị data bằng script đối chiếu + tạo tool `Check All` để anh bấm 1 phát ra PASS/FAIL 8 nhóm. Cần anh chơi thử theo checklist (mục 16).

## 14. Asset cần anh xử lý thêm: icon cho quà LevelReward L7-L10 (tool tự scan, thiếu thì kéo tay trong Inspector); icon riêng cho Chuồng Bò Sữa (đang dùng tạm icon Chuồng Bò); 3 file âm thanh còn thiếu (coin, plant, cooking-start); prefab coin-fly (chưa có).

## 15. Warning còn lại: tool cũ `Setup Village Orders L1-L6/Apply Phase 1 Data` chứa số liệu CŨ — **đừng chạy**, sẽ ghi đè kinh tế mới (Check All sẽ phát hiện nếu lỡ chạy). ID `ca_rot`/`khoai_tay` thiếu prefix `seed_` — chưa đổi (đụng save + seed panel, cần duyệt riêng). Decor đang bán bằng kim cương 20-400 (khác design gold 50-300) — để nguyên chờ anh quyết.

## 16. Console: cần anh mở Unity → đợi compile → check Console. Các sửa đổi đều additive/minimal, rủi ro compile thấp. Sau đó: **(1)** chạy `Demo L1-L10 → Check All` → phải 0 FAIL (2 mục có thể WARN nếu chưa mở SCN_Farm); **(2)** chạy `Setup Reward Data (L2-L10)` + lưu scene; **(3)** `Reset Demo Save` → Play → chơi theo `Print Playtest Checklist` (18 tiêu chí).

## 17. Việc nên làm sau L10 / phiên tới
1. Duyệt + implement mission system theo proposal (bug tracker phải sửa trước khi demo có nhiệm vụ).
2. Animal tutorial L2 (gà) bằng mini-sequence TutorialManager sẵn có.
3. Đơn sữa (milk chưa có OrderItem — chuồng bò sữa L8 đang thiếu đầu ra).
4. Chuẩn hoá ID ca_rot/khoai_tay + migration.
5. Rain chance per day (hiện mưa vẫn 1 lần/chu kỳ ngày + 1 lần/đêm, chỉ ngắn đi — muốn "có ngày không mưa" cần thêm ~3 dòng ở `ScheduleRainEvents()`).
6. Tourist boat design doc (Phase 14 — chưa làm).
