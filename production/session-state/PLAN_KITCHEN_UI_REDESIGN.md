# PLAN — CHUYỂN UI SCENE NẤU ĂN SANG THIẾT KẾ "KITCHEN COOK FLOW" (2026-08-26)
> Trạng thái: CHỜ SẾP DUYỆT. Chưa code. Nguyên tắc: GIỮ NGUYÊN 100% logic/data — chỉ thay lớp View.

## 1. PHÂN TÍCH MOCKUP (video 31s, HTML/CSS 1332x892)
Layout 6 khối:
- **TopBar** (trái-trên): pill Bếp trưởng + level + thanh EXP · pill vàng.
- **OrderBanner** (giữa-trên): ribbon "ĐƠN CỦA KHÁCH" + card món + chip 5 vị yêu cầu.
- **RecipeBoard** (panel trái, 2 chế độ):
  · DETAIL "BẢNG CÔNG THỨC": icon+tên món, chip độ khó+cấp, "CẦN NHỮNG THỨ NÀY" (chip nguyên liệu),
    5 thanh vị Ngọt/Cay/Chua/Đậm/Kết cấu (fill xanh theo lựa chọn, VẠCH ĐỎ = mốc), footer +vàng/+EXP/giá bán
    + "Điểm dự kiến" cập nhật realtime.
  · LIST "SỔ CÔNG THỨC": tab lọc Tất cả/Dễ/Vừa/Khó + hàng món scroll (icon, tên, cấp, vàng, chevron).
- **KitchenStage** (giữa): tường bếp + đồ treo + bảng "MÓN HÔM NAY" + Mèo Thần Tài (vẫy tay) + chậu cây/
  bao tải/mèo ngủ · Bàn sơ chế (toast "Sơ chế x món") · Bàn trình bày ("Chạm để cất vào kho") ·
  hộp VÀO KHO (đếm "Đã gửi N món") · LÒ ĐẤT 3 trạng thái: chưa nhóm → đang cháy (than đỏ + khói + % nướng) → đã nghỉ.
- **IngredientTray** (dưới): 2 tab "Nguyên liệu x/4" & "Gia vị x/3" + nút Bỏ hết + grid card bo góc
  (icon + tên + badge xN, chọn = viền sáng) + ô khoá theo cấp (Cấp 14 Sữa 🔒) + ô trống.
- **ActionButton** (phải-dưới) 3 state: CHỌN NGUYÊN LIỆU (xám) → NẤU! (xanh + đếm n nguyên liệu-m gia vị) → ĐANG NƯỚNG x% (progress).
Flow video: chọn món từ sổ → chọn nguyên liệu/gia vị (thanh vị nhích, điểm dự kiến đổi 61đ→94đ) → NẤU →
lò cháy % → sơ chế → chạm bàn trình bày cất kho → +xu +EXP → đơn mới.

## 2. HIỆN TRẠNG (SampleScene + code)
- Scene nấu = `Assets/_Game/Scenes/SampleScene.unity`. UI cũ kiểu sách lật trang
  (img_SachMo, btn_Left/Right/BackPage, man_delivery, warehouse, 2 cột flavor labels/values...).
- **Logic ĐÃ CÓ ĐỦ và khớp mockup 1:1** (đây là điểm vàng — không phải viết gameplay mới):
  · `DishData`: targetFlavor(FlavorVector) + requiredIngredients + difficulty + unlockLevel + rewardExp/Gold/sellPrice
    = đúng mọi field RecipeBoard cần. `ListDishData` = SỔ CÔNG THỨC. 13 ING_ + 8 SEA_ asset = đúng tray 2 tab.
  · `FlavorVector{sweet,spicy,sour,umami,texture}` = 5 thanh vị.
  · `CookingSelectionManager`: maxIngredients=4/maxSeasonings=3 (ĐÚNG mockup "tối đa 4/3"), TrySelect/TryDeselect/ResetSelection.
  · `CookingScoreCalculator` = "Điểm dự kiến". `CookingChallengeManager`: SetCurrentDish → OnClickCookSubmit →
    minigame (Timing/Letter) → CollectCookedDishToWarehouse (= chạm bàn trình bày) → thưởng scale theo điểm.
  · Đã hook sẵn: MissionProgressTracker (nấu món), FarmEconomy (vàng), PlayerProgress (EXP), kho món ăn.

## 3. KIẾN TRÚC CHUYỂN ĐỔI — "View mới, não cũ" (đúng bài đã làm với tàu hỏa)
- Tạo `Canvas_Kitchen_v2` MỚI trong SampleScene + bộ script view thuần namespace `KitchenUIv2`:
  `KitchenTopBarUI, KitchenOrderBannerUI, KitchenRecipeBoardUI, KitchenStageUI, KitchenTrayUI, KitchenActionButtonUI`.
- 1 adapter duy nhất `KitchenUIAdapter` nối view ↔ managers hiện có (không sửa manager trừ khi thiếu event
  — thiếu thì THÊM event, không đổi chữ ký cũ).
- UI cũ: **SetActive(false)**, KHÔNG xóa (đúng luật AUTONOMY — rollback 1 click). Xóa thật để Sprint dọn scene sau.
- Builder tool Editor (như train): `Tools → Farm Game → Kitchen → Build Kitchen UI v2` — dựng hierarchy +
  gán sprite serialize (build-safe), chạy lại idempotent.
- FX tái dùng: khói lò = train_smoke_puff · fly vào kho = HarvestFly/WarehouseGainToast có sẵn · font TMP mượn HUD.

## 4. ASSETS — đặt sprite-forge (style game, BỚT AI-look, tuân ART_RULES_STUDIO.md)
ĐÃ CÓ KHÔNG VẼ LẠI: icon 21 nguyên liệu/gia vị (asset ING_/SEA_ + PixelVibe pack) · khung gỗ/giấy/ribbon/nút
(train package dùng tạm Sprint K1) · khói. CẦN VẼ MỚI (~20 file):
1. `kitchen_bg_wall.png` + `kitchen_bg_floor_diamond.png` (nền tường kem + sàn caro nâu, tileable)
2. Lò đất 3 state: `oven_idle / oven_fire_01..04 (than+lửa loop) / oven_done` + `oven_progress_pill`
3. `prep_table.png` (bàn + dao) · `plating_table.png` (bàn + đĩa trắng) · `warehouse_chest.png` (hộp VÀO KHO)
4. Mèo Thần Tài `maneki_idle_01..04` (vẫy tay loop, không text) + `maneki_sign.png` (biển gỗ trống)
5. `menu_chalkboard.png` (bảng đen TRỐNG — chữ món TMP render) · decor: `hang_pans.png, skewers.png,
   plant_pot.png, sack.png, cat_sleep.png`
6. UI: `card_ingredient_frame.png` (bo góc + slot badge) · `card_locked.png` + `icon_lock.png` ·
   `taste_bar_track/fill.png` + `taste_marker_red.png` · `btn_cook_green/gray.png` · `tab_pill_on/off.png` ·
   `chip_difficulty_easy/mid/hard.png` · `order_card_frame.png`
Tất cả: không text, không bóng, Single, pivot phù hợp, palette đồng bộ train package (kem/nâu gỗ/cam).

## 5. SPRINT & THỨ TỰ
- **K1 — Khung + nối não (code trước, skin tạm)**: build layout đủ 6 khối bằng asset tạm, adapter bind toàn bộ
  flow end-to-end (chọn món→chọn đồ→thanh vị+điểm dự kiến→nấu→cất kho→thưởng). Nghiệm thu: chơi trọn 1 món
  bằng UI mới. ~1 phiên.
- **K2 — Skin + animation**: assets sprite-forge về → tool gán · mèo vẫy · lò cháy + khói + % · card bounce ·
  toast sơ chế · fly vào kho. ~1 phiên.
- **K3 — QA + chuyển giao**: QA agent review + Sếp playtest checklist · tắt hẳn UI cũ · cập nhật ROADMAP.

## 6. CẦN SẾP QUYẾT (trước khi code K1)
Q1. MINIGAME timing/gõ chữ hiện có — mockup KHÔNG có (chỉ % lò nướng). Giữ minigame (xen giữa NẤU→lò) hay bỏ,
    thay bằng chờ % lò như mockup?
Q2. "MÓN HÔM NAY" trên bảng đen — hiện chưa có logic. Làm data thật (3 món/ngày thưởng x2?) hay chỉ trang trí (đợt sau làm logic)?
