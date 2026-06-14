# L1→L10 SCAN REPORT — Phase 1 (CHƯA SỬA CODE)

> Ngày: 2026-06-12 · Phương pháp: 5 agent scan song song toàn bộ source/scene/data + 1 agent research Township/Hay Day
> Trạng thái: **CHỜ DUYỆT** — chưa sửa bất kỳ file gameplay nào.

---

## A. TRẢ LỜI 20 CÂU HỎI SCAN

| # | Câu hỏi | Kết quả |
|---|---------|---------|
| 1 | Level/EXP manager | `Assets/_Game/Scripts/Progression/PlayerProgressManager.cs` (singleton, DontDestroyOnLoad, maxLevel=30) |
| 2 | Công thức L1→L30 | `RequiredExpForLevel(level)`: n = level−1 → **EXP = 40 + 10n + n²**. L1→2=40, L9→10=184, L29→30=≈820. EXP dư **có giữ lại** khi lên cấp ✓ |
| 3 | Vàng/kim cương | `Assets/_Game/Farm/Scripts/Managers/FarmEconomyManager.cs` — `startGold=1250`, `startGems=10`, lưu PlayerPrefs. ⚠ Có **debug override** (xem mục C) |
| 4 | Inventory/warehouse | `FarmInventoryManager.cs` + `WarehouseManager.cs` (Farm/Scripts/Managers). Key chuẩn hoá lowercase+trim, lưu PlayerPrefs, share farm↔cooking qua DontDestroyOnLoad ✓ |
| 5 | Crop data thật | `Assets/_Game/Farm/data/Hat_giong/` (11 cây) + `Assets/_Game/Farm/data/Hạt Hoa/` (10 hoa). Schema CropData: cropId, seedItemId, harvestItemId, tier, unlockLevel, growSeconds, sellGold, harvestAmount, plantCost, expReward, goldPrice, canBuyInSeedShop, canDropFromAds |
| 6 | ID thật | Xem bảng B.1 bên dưới. Quan trọng: lúa = `seed_rice`→`rice`, hoa hướng dương = `seed_huong_duong`→`huong_duong`, ngô `seed_ngo`→`ngo`, cà chua `seed_cachua`→`cachua`, bắp cải `seed_bapcai`→`bapcai`, trứng `egg`, thịt gà `chicken_meat`, heo `pork`, bò `beef`, sữa `milk` |
| 7 | Shop item data | `BaseItemData.cs` (Farm/Scripts/Shop) — CÓ field `unlockLevel`. Shop 3 tab (seed/building/decor). `ShopManager.cs`, `ShopItemUI.cs` |
| 8 | Công trình shop data | `Assets/_Game/Farm/CÔNG TRÌNH/` — 4 chuồng (Gà/Heo/Bò/Dairy) + 15+ decoration. Plot_01.prefab, Chauhoa_1-4.prefab |
| 9 | Village order data | `Assets/_Game/Farm/data/Village_data/` — 34 OrderItemDefinition. Manager: `Farm/Scripts/Village/VillageOrderManager.cs` (cooldown 60s, 1-2 item/đơn, 50% đơn 2 item, lọc theo level) |
| 10 | Số nhà dân order | **8 nhà** trong SCN_Farm (House_02,03,05,06,08,09,11,12). **KHÔNG có giới hạn/khoá** — cả 8 nhà order cùng lúc từ L1. Không có hệ mua/mở nhà |
| 11 | Order dùng item chưa unlock sớm | Theo unlockLevel order thì lọc đúng, NHƯNG: đơn nấm hỏng ID (mục C-1); 20 món ăn cùng bung ở L5 làm loãng pool; mía order L8 trong khi không có gate trồng |
| 12 | Cooking có bao nhiêu món | **20 món** — `Assets/_Game/Farm/data/Farm_Cooking/All_Data.asset` + Dish_*.asset. Nấu qua mini-game (Timing/Letter, ngưỡng 70 điểm) |
| 13 | Cooking mở level nào | Farm-side gate L5 qua `FarmLevelManager.HasReached(5)`. **Trong bếp KHÔNG gate** — vào được là thấy cả 20 món. Order món ăn: cả 20 món unlockLevel=5 |
| 14 | Mission system | CÓ cơ bản: `Assets/_Game/Scripts/Mission/` (MissionDatabase, MissionProgressTracker, MissionItemUI) + 21 Mission_*.asset trong `Farm/data/Data_Ewa/`. **Chưa có daily mission, chưa có mission theo level** |
| 15 | Achievement/Ewar | `PopupEwarManager.cs` — popup khung có, nội dung mỏng (~40%), nhận thưởng chạy ngầm khi đóng popup, không có VFX riêng |
| 16 | LevelUpPopupUI | **CÓ, hoàn chỉnh**: `Farm/Scripts/UI/LevelUpPopupUI.cs` + `LevelRewardConfig.cs` + `LevelUpGiftSlotUI.cs`. Có slot VFX (confetti + 2 bên). ⚠ Chỉ có asset **LevelReward_L2→L6**, **thiếu L7→L10** |
| 17 | Popup tutorial | **CÓ, hoàn chỉnh L1→L2**: TutorialManager (state machine, 21 loại wait-action), TutorialStepData, GuideBoard 4 bước, NPC dialog + typewriter, DragHintAnimator (hand pointer), CameraZoom/Focus, RuntimeTargetResolver (alias `seed_rice`/`seed_huong_duong` → ID thật) ✓ |
| 18 | Shop lock overlay | **CÓ**: `ShopLevelLockUI.cs` — nền tối 0.65, icon khoá, text "Mở ở cấp X", disable nút mua ✓. ⚠ Nhưng data crop toàn unlockLevel=1 nên không có gì để khoá (mục C-3) |
| 19 | VFX Lana Studio | 22 prefab tại `Assets/Lana Studio/Hyper Casual FX/Prefabs/`: Confetti_blast_multicolor, Confetti_directional_multicolor, Flash ×8 (magic_blue_pink…), Sparkle_ellow, Shine ×3, Area ×5, Dust, Water ×2. Tutorial tool đã wire 3 prefab confetti/flash |
| 20 | Missing/warning/error | SCN_Farm (1.324 object) & SampleScene (182): **0 missing script**. Warnings: debug override tiền tệ, ID lệch chuẩn, thiếu audio clip, thiếu coin-fly prefab (chi tiết mục C) |

### B.1 — Bảng ID & thông số thật (Hat_giong + Hạt Hoa)

Tất cả cây hiện: `sellGold=3`, `harvestAmount=4`, `plantCost=1`, `expReward=5` (trừ mía=8), `unlockLevel=1` (trừ khoai tây=0).

| Cây | cropId | seedItemId | harvestItemId | Giá shop | Grow (s) |
|-----|--------|-----------|---------------|----------|----------|
| Lúa | rice | seed_rice | rice | 20 | 180 |
| Bắp cải | bapcai | seed_bapcai | bapcai | 60 | 300 |
| Ngô | ngo | seed_ngo | ngo | 40 | 600 |
| Cà chua | cachua | seed_cachua | cachua | 80 | 900 |
| Cà rốt | carot | **ca_rot** ⚠ | carot | 50 | 400 |
| Khoai tây | khoaitay | **khoai_tay** ⚠ | khoaitay | 100 | 500 |
| Nấm | nam | seed_nam | **mushroom** ⚠ | 100 | 600 |
| Mía | sugarcane | seed_sugarcane | sugarcane | 150 | 420 |
| Chanh | lemon | seed_lemon | lemon | 130 | 780 |
| Ớt | chili | seed_chili | chili | 170 | 540 |
| Tiêu | pepper | seed_pepper | pepper | 190 | 660 |
| Hướng dương | huong_duong | seed_huong_duong | huong_duong | 50 | 180 |
| 9 hoa khác | hoa_lan/hoa_hong/tulip/hoa_cuc_trang/hoa_cuc_van_tho/hoa_mau_don/hoa_cam_tu_cau/hoa_anh_thao/hoa_oai_huong | seed_* (chuẩn) | trùng cropId | 60–140 | 180 (tất cả) |

Sản phẩm động vật (`Farm/data/Farm_dong_vat/`): egg, chicken_meat, pork, beef, milk + 20 item món ăn (itemId trùng dishId ✓).

---

## B. ĐÃ CÓ GÌ (nền tảng tốt)

- Tutorial L1→L2 **hoàn chỉnh 18 bước** + tool Setup/Check/Generate, alias seed về ID thật, camera zoom/focus, hand pointer, guide board. EXP khớp: 6 lúa ×5 + 2 hoa ×5 = 40 = đúng L2.
- LevelUpPopupUI hoàn chỉnh (quà, unlock list, nút, VFX slot) + tool setup; config L2–L6.
- Shop lock UI hoàn chỉnh; schema có unlockLevel.
- Village order system chạy được, bubble **có animation** (pop-in, float, bounce); order lọc theo level.
- Cooking 20 món, mini-game, món nấu xong **vào đúng warehouse farm** ✓; chuyển scene additive sạch, manager DontDestroyOnLoad ✓.
- Không popup nào tự mở khi Play ✓ (đã có DisableStartupPopupsTool).
- Mission khung + 21 asset mẫu; Avatar HUD + EXP circle; PF_ExpFly_World ✓; train system (gold/EXP source phụ).
- 27 menu tool editor, trong đó 16 tool farm-game (Setup Tutorial L1-L2, Setup Level Up Popup, Setup Shop Lock L3+, Setup Village Orders **L1-L6**, Force Level, Reset Save…).
- 0 missing script ở cả 2 scene chính.

## C. ĐANG LỖI GÌ (bug thật, có bằng chứng)

1. **[NGHIÊM TRỌNG] Đơn nấm không thể giao**: `Village_data/OrderItem_Mushroom.asset` yêu cầu `itemId: nam` (unlock L6) nhưng cây nấm `Hat_giong/nam.asset` thu hoạch ra `harvestItemId: mushroom`, kho lưu `Item_Mushroom (itemId: mushroom)`. `nam` ≠ `mushroom` → kẹt đơn vĩnh viễn ở L6+.
2. **[NGHIÊM TRỌNG] Debug override tiền tệ**: `FarmEconomyManager.Start()` có `#if UNITY_EDITOR → Gold=1000; Gems=1000; SaveCurrency();` → mọi lần Play trong Editor bị ép 1000/1000 và **ghi đè save**. Mọi playtest kinh tế đều sai. Cần xoá (chờ duyệt).
3. **Shop lock không có gì để khoá**: TẤT CẢ crop/hoa `unlockLevel=1` (khoai tây =0) → trái với thiết kế mở dần L1→L10; mâu thuẫn với order unlock (order ngô L2, cà chua L2… nhưng hạt mở từ L1).
4. **ID lệch chuẩn**: `ca_rot`, `khoai_tay` thiếu prefix `seed_`; khoai tây `unlockLevel=0`, `tier=0`.
5. **sellGold phẳng = 3 cho mọi cây** → bán chợ LỖ với mọi hạt (4×3=12 < giá hạt 20–190); chỉ order là có lời. Người chơi bán nhầm ở chợ sẽ kẹt tiền.
6. **Order mía trả 14 vàng/đơn vị** — thấp hơn cả lúa (15) dù hạt mía 150 vàng → lỗ nặng.
7. **Cooking không cho EXP** khi nấu (0 EXP) — chỉ order món mới có 10 EXP/món.
8. Audio thiếu file thật cho `ingredientPop`, `cookStart`, `coinReward` (đang fallback uiClick); **không có coin-fly prefab** (EXP-fly có).

## D. ĐANG THIẾU GÌ (so với chuẩn demo L1→L10)

1. LevelReward_L7→L10 (mới có L2–L6).
2. Hệ **giới hạn/mở khoá nhà dân** (cần: L1 chỉ 4 nhà active, mở dần — hiện HouseOrderController không có field level).
3. Gate 10 món ăn "dễ" trong bếp + lộ trình món L5→L10 (hiện 20 món bung cùng lúc, một số món cần **cá** — chưa có hệ nuôi/bắt cá → rủi ro nguyên liệu không farm được).
4. Mission theo level L1→L10, daily mission, achievement có nội dung + UI nhận thưởng rõ.
5. Animal tutorial L2→L4 (gà → heo → bò).
6. Tool tổng `Demo L1-L10`: Setup All / Check All / Simulate Economy / Reset Demo Save / Print Playtest Checklist. Tool Village Orders hiện chỉ tới L6.
7. Cân bằng kinh tế per-crop (sell/giá hạt/grow time/EXP) — hiện toàn placeholder phẳng.
8. Weather: ngày 120s, mưa 10–18s mỗi chu kỳ — cần proposal chỉnh (ngày dài hơn, mưa thưa hơn).

## E. RỦI RO GÌ

- Sửa ID (`nam`→`mushroom` hoặc ngược lại) đụng save cũ + Mission_nam + LevelReward_L6 + ING_Mushroom (cooking) → phải sửa **một đầu duy nhất** (đề xuất: sửa order requirement thành `mushroom`, giữ nguyên crop) và check toàn bộ tham chiếu.
- Thêm unlockLevel cho crop có thể phá tutorial nếu khoá nhầm `seed_rice`/`seed_huong_duong` (phải giữ L1).
- Giới hạn 4 nhà là feature mới đụng VillageOrderManager — làm sai có thể chết replenish loop.
- Xoá debug override tiền tệ làm thay đổi cảm giác test hiện tại của anh (Editor sẽ chạy số thật 1.250/10 hoặc số mới được duyệt).
- 50% đơn 2-item ở L1 có thể quá khó cho người mới — chỉnh tỉ lệ này là đụng GenerateOrder().

## F. FILE LIÊN QUAN (lối tắt)

| Hệ | File chính |
|----|-----------|
| Progression | `_Game/Scripts/Progression/PlayerProgressManager.cs` |
| Kinh tế | `_Game/Farm/Scripts/Managers/FarmEconomyManager.cs` |
| Kho | `_Game/Farm/Scripts/Managers/FarmInventoryManager.cs`, `WarehouseManager.cs` |
| Crop | `_Game/Farm/data/Hat_giong/*.asset`, `Hạt Hoa/*.asset` |
| Order | `_Game/Farm/Scripts/Village/VillageOrderManager.cs`, `HouseOrderController.cs`, `HouseOrderBubble(.Animator).cs`, `data/Village_data/*.asset` |
| Cooking | `_Game/Scripts/CookingChallengeManager.cs`, `data/Farm_Cooking/*.asset` |
| Tutorial | `_Game/Farm/Scripts/Tutorial/*` (9 file) + `Farm/Editor/SetupTutorialL1L2Tool.cs` |
| Level-up | `_Game/Farm/Scripts/UI/LevelUpPopupUI.cs`, `LevelRewardConfig.cs`, `data/Lever Game/LevelReward_L2..L6.asset` |
| Shop | `_Game/Farm/Scripts/Shop/*` |
| Mission | `_Game/Scripts/Mission/*`, `data/Data_Ewa/*` |
| VFX | `Assets/Lana Studio/Hyper Casual FX/Prefabs/*`, `Farm/Animations/PF_ExpFly_World.prefab` |
| Weather | `Assets/Day_Night/Scripts/Runtime/DayNightCycleController.cs` |

## G. ĐỀ XUẤT BUILD L1→L10 (thứ tự an toàn)

1. **Hotfix data được duyệt trước** (không code): fix `nam/mushroom`, xoá debug override, order mía 14→giá mới — mở đường cho mọi playtest.
2. Batch Economy data (crop sell/unlock/exp + order reward + starter) theo `L1_L10_ECONOMY_TABLE.md` sau khi duyệt.
3. Shop lock L1→L10 (chạy tool sẵn có + data mới).
4. Village: giới hạn 4 nhà active L1 + mở dần (feature mới, có proposal riêng).
5. LevelReward L7→L10 + nâng tool setup popup.
6. Cooking L5: gate 10 món dễ + EXP nấu ăn.
7. Animal tutorial L2→L4.
8. Mission L1→L10 + daily placeholder.
9. VFX/audio polish + weather tuning.
10. Tool tổng `Demo L1-L10` (Setup All / Check All / Simulate Economy) → Full playtest.

## H. CÁC PHẦN CẦN ANH DUYỆT

| # | Quyết định | Đề xuất của team |
|---|-----------|------------------|
| 1 | Fix bug `nam` vs `mushroom` theo hướng nào? | Sửa order requirement → `mushroom` (ít đụng chạm nhất) |
| 2 | Xoá debug override Gold/Gems=1000 trong Editor? | Xoá, thay bằng tool menu "Give Test Currency" |
| 3 | Bảng kinh tế mới (sell/unlock/order reward/starter) | Xem `L1_L10_ECONOMY_TABLE.md` — chờ duyệt từng bảng |
| 4 | Thiết kế vòng chơi + unlock L1→L10 | Xem `L1_L10_DESIGN_PLAN.md` |
| 5 | Giới hạn 4 nhà order ở L1, mở dần L3/5/7/9 | Đồng ý làm feature mới trong VillageOrderManager |
| 6 | 10 món cooking đầu (có món phải đổi nguyên liệu vì dính cá/bò sớm) | Danh sách trong DESIGN_PLAN mục 6 |
| 7 | Chuẩn hoá ID `ca_rot`/`khoai_tay` → `seed_carot`/`seed_khoai_tay`? | Làm, kèm migration check save cũ |
| 8 | Giảm tỉ lệ đơn 2-item ở L1–L2 (50% → 0–20%)? | L1: 0%, L2–3: 20%, L4+: 50% như cũ |
