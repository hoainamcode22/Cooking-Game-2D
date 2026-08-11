# Lộ trình xây tiếp — toàn cảnh dự án

Rà toàn bộ: hệ ruộng · scene nấu ăn · nối kết giữa các hệ. Xếp theo mức chặn.

---

# PHẦN 0 — BỐN THỨ ĐANG HỎNG, PHẢI SỬA TRƯỚC KHI THÊM BẤT KỲ GÌ

## 🔴 1. Nấu ăn — CHỈ 4/20 MÓN NẤU ĐƯỢC

**Đây là lỗi lớn nhất dự án.** Game tên là Cooking Game mà chức năng nấu gần như không dùng được.

Panel nguyên liệu bên trái **chỉ có ĐÚNG 1 ô thẻ nguyên liệu và 1 ô thẻ gia vị** trong scene:
- `SampleScene.unity:8758` → `ingredientsContent` chỉ có **1 con** (prefab `Item_Ingredient_Beef`)
- `SampleScene.unity:8759` → `seasoningsContent` chỉ có **1 con** (`Item_Seasoning_FishSauce`)
- `CookingBoot.ApplyToCardGroup` (`:107-138`) **không `Instantiate` thẻ mới**, chỉ tái dùng con có sẵn

Chuyển 10 loại nguyên liệu vào bếp thì **chỉ 1 loại hiện lên**, người chơi không chọn được loại nào. Phần dư rơi vào `Debug.LogWarning("Không đủ slot…")`.

Luật chấm điểm: nguyên liệu tối đa **70 điểm**, gia vị tối đa **30**. Thiếu một nguyên liệu là tụt còn **35**. Ngưỡng đạt là **70**.

| Nấu được | Không nấu được |
|---|---|
| Khoai tây chiên · Gà nướng lu · Gà xào ớt · Bò xào tiêu | **16 món còn lại** |

Bốn món chạy được **đều chỉ cần đúng 1 nguyên liệu**. Đó là lý do duy nhất chúng qua được.

**Sửa:** nhân thẻ trong scene lên ≥8 ô nguyên liệu + ≥7 ô gia vị, HOẶC sửa `ApplyToCardGroup` để `Instantiate` theo số lượng thật.

## 🔴 2. Bảng đơn hàng chưa có trong scene

GUID `OrderBoardManager` = **0 kết quả** trong cả 3 scene. **Bạn chưa chạy tool.**

Hậu quả dây chuyền:
- **26 nhiệm vụ `DeliverOrder` đứng im vĩnh viễn** — gồm mission chính tuyến **cấp 2, 3, 5, 7, 8, 9** ⇒ **chuỗi nhiệm vụ chính tắc ngay từ cấp 2**
- Mất 1 nguồn vàng + 1 nguồn EXP
- `AnimalGuideController` poll 5 giây/lần mãi mãi tìm bảng không tồn tại

**Sửa:** `Tools ▸ Farm ▸ Bảng Đơn Hàng ▸ 2 · Dựng TẤT CẢ` → kéo bảng → Ctrl+S.

## 🔴 3. Chuồng trại phá vỡ toàn bộ kinh tế ruộng

| | Đầu vào | Đầu ra | Thời gian |
|---|---|---|---|
| **Chuồng gà** | 1 lúa (7 vàng) | 4 thịt gà + 4 trứng = **320 vàng** | **30 giây** |
| **Chuồng bò** | 1 lúa (7 vàng) | 4 thịt bò = **260 vàng** | **30 giây** |
| **Ruộng tiêu** (cây tốt nhất) | 190 vàng hạt | 220 vàng, lãi **+30** | **198 giây** |

**Chuồng lãi gấp ~70 lần ruộng trên mỗi giây.** EXP cũng gấp 9 lần (0.833/s so với 0.093/s).

Chuồng gà giá 100 vàng, mở ở **cấp 2**. Nghĩa là từ cấp 2, **toàn bộ hệ thống trồng trọt trở thành vô nghĩa về kinh tế**.

**Sửa:** `productAmount` 4 → 1, `feedDurationSeconds` 30 → 300-600, cần 2-3 đơn vị thức ăn thay vì 1.

## 🔴 4. Chín cặp ô đất TRÙNG `plotId` → mất dữ liệu cây trồng

`PlotController.SaveKey = $"PLOT_NORMAL_{plotId}"`. Trong scene, plotId **2, 3, 4, 5, 6, 7, 8, 26, 27** mỗi cái xuất hiện **hai lần**.

Chín cặp ô dùng chung một khoá PlayerPrefs → trồng ô này, thoát game vào lại thì **ô kia hiện cây, ô này mất**.

---

# PHẦN 1 — HỆ THỐNG RUỘNG

## Hiện trạng: ~45% hoàn chỉnh

| Hạng mục | % | Ghi chú |
|---|---|---|
| Kỹ thuật lõi (trồng/chờ/thu/save/offline) | **90%** | Rất chắc. Có kéo liềm thu hàng loạt, kéo hạt trồng hàng loạt |
| Dữ liệu 21 cây | 75% | Đủ trường, số liệu chưa cân |
| **Tương tác giữa lúc trồng và lúc thu** | **10%** | Chỉ có tăng tốc gem |
| **Mở rộng ruộng** | **20%** | Code đầy đủ nhưng **bị tắt hoàn toàn** |
| Kinh tế & cân bằng | 25% | Chuồng phá vỡ; 3 loại hoa bán lỗ |
| Đường cong tiến trình | 20% | Hết nội dung ở cấp 15/100 |
| Kho / giới hạn | 15% | Chỉ là chữ trên màn hình |

## Thời gian trồng — có 4 nghịch lý

`realTimeMultiplier = 0.3` nên thời gian thật = 30% con số trong asset.

| Cây | Cấp | Thời gian thật | Lãi/lượt |
|---|---|---|---|
| Lúa | 1 | 54s | +8 |
| Bắp cải | 1 | 90s | +15 |
| Ngô | 2 | 108s | +12 |
| Cà rốt | 3 | 120s | +14 |
| Cà chua | 3 | 144s | +15 |
| Khoai tây | 5 | 150s | +20 |
| Nấm | 6 | 180s | +20 |
| **Mía** | **7** | **126s** | +24 |
| **Chanh** | **8** | **234s** | +22 |
| **Ớt** | **9** | **162s** | +22 |
| **Tiêu** | **10** | **198s** | +30 |

**Nghịch lý:** Mía cấp 7 nhanh hơn Cà chua cấp 3 · Ớt cấp 9 nhanh hơn Nấm cấp 6 · Tiêu cấp 10 nhanh hơn Chanh cấp 8. **Chanh là lựa chọn tệ nhất tuyệt đối**: lâu nhất game mà lãi thua cả Mía trong nửa thời gian.

**10 loại hoa dùng chung đúng một thời gian 54 giây** — hoa cấp 1 và hoa cấp 10 chín cùng lúc. Và hoa lãi gấp 2-3 lần rau củ trên mỗi giây, nên người chơi tối ưu sẽ bỏ hẳn rau — trong khi rau lại là đầu vào bắt buộc cho chuồng và bếp.

**Ba loại hoa bán LỖ:** Cúc Trắng −14 · Cẩm Tú Cầu 0 (hoà) · Anh Thảo −2.

## Mở ô đất — hệ thống có nhưng bị tắt

`PlotController` có đủ `unlockedAtStart` · `requiredLevel` · `gemCost` · `requireAd`. Nhưng:
- Cả 26 ô trong scene đều `unlockedAtStart: 1`, `gemCost: 0`, `requiredLevel: 1`
- `FarmManager.unlockAllPlotsForLayout: 1` → gọi `UnlockAllPlotsNow()` rồi return
- `startUnlockedNormalCount: 4` **bị bỏ qua hoàn toàn**

⇒ `TryUnlockSelectedPlotByGem()`, `CanUnlockByLevel()`, `lockSprite` — **toàn bộ là code chết**.

Thay vào đó mở ô = **mua công trình "Đất" 50 vàng, không giới hạn, giá không tăng**.

## Vòng lặp trồng trọt — thiếu gì

| Tính năng | Có? |
|---|---|
| Tưới nước | ❌ — hàm `ApplyWaterBonus()` **đã viết sẵn** ở `PlotController.cs:713` nhưng **0 nơi gọi** |
| Bón phân | ❌ |
| Sâu bệnh / cỏ dại | ❌ |
| Cây héo nếu không thu | ❌ — vào trạng thái Ready là đứng vĩnh viễn |
| Tăng tốc gem | ⚠️ Có, nhưng **cứng 1 gem cho mọi cây** — 1 gem rút 234s của Chanh cũng như 5s cuối của Lúa |
| Thu hoạch hàng loạt | ✅ Kéo liềm |
| Trồng hàng loạt | ✅ Kéo hạt |
| Kho giới hạn sức chứa | ⚠️ **Chỉ là chữ** — `FarmInventoryManager.AddItem` không kiểm capacity một dòng nào |

## Đường cong cấp — hết nội dung ở cấp 15

Tổng EXP cấp 1 → 30 chỉ là **6.366**. Với tốc độ hiện tại: **30–60 phút là tới cấp 30**.

Cây cuối cùng mở ở **cấp 10**. Máy cuối cùng ở **cấp 15**. **Từ cấp 16 đến 100 không mở khoá thêm bất kỳ cây/chuồng/máy nào** — 94% thang cấp trống rỗng.

---

# PHẦN 2 — SCENE NẤU ĂN

## Cần thêm gì

### Ưu tiên 1 — mở khoá 16 món (không cần vẽ)

1. **Nhân ô thẻ** trong `SampleScene`: ≥8 ô nguyên liệu, ≥7 ô gia vị. Đây là việc quan trọng nhất.
2. **Thêm nguyên liệu `ca`** — hiện `ING_Fish.asset` được 2 món dùng nhưng **không có `InventoryItemData`, không cây, không chuồng, không dòng chợ**. Không định làm cá thì **xoá 2 món** `ca_nuong_tieu` + `canh_chua_ca`.
3. **Sửa `Dish_nuoc_mia_chanh`** — cả 2 nguyên liệu đều `kind = Seasoning` nên điểm nguyên liệu = 0, trần 30 điểm, **không bao giờ đạt 70**. Cần thêm một `IngredientData` có `kind = Ingredient` (ví dụ `ING_Sugarcane`).
4. **Thêm `Item_Milk`** vào `cookingInventoryItems` — sữa có nguồn (chuồng bò sữa) nhưng **không nằm trong danh sách nên không bao giờ vào bếp được**.

### Ưu tiên 2 — thưởng nấu ăn

`DishData` **không có trường thưởng nào**. `CookingChallengeManager` cộng cứng **20 EXP, 0 vàng** — bất kể món gì, độ khó gì, điểm bao nhiêu.

**Nghịch lý:** chợ bán sẵn cả 20 món. Nấu tốn nguyên liệu + chơi mini-game để được 20 EXP, còn mua thì chỉ tốn vàng. **Nấu đang thua mua về mọi mặt.**

Thêm `rewardExp` / `rewardGold` / `sellPrice` vào `DishData`, thưởng theo độ khó và điểm số.

### Ưu tiên 3 — dọn dữ liệu

- **Bộ `IngredientData` bị nhân đôi** ở 2 thư mục. `SEA_Pepper` bản trùng có `kind` **sai** (0 thay vì 1) — ai đổi tham chiếu sang bản đó là tiêu bị tính thành nguyên liệu thừa, điểm tụt từ 70 xuống 35.
- **2 GameObject `CookingGate`** trùng nhau trong `SCN_Farm`.
- 3 sản phẩm máy (`bot_gao`, `nuoc_mia_ep`, `pho_mai`) có `cookingData` rỗng → **không món nào dùng sản phẩm máy**.

### Cổng cấp 5 vẫn chưa khoá

`BuildingInteractable` gọi thẳng `OnClick_GoCooking()`, **không một dòng kiểm cấp**. Người chơi cấp 1 vào bếp được nhưng **món thấp nhất là cấp 5** ⇒ vào thấy màn hình chết, không thông báo gì.

---

# PHẦN 3 — NỐI KẾT GIỮA CÁC HỆ

## Năm chỗ đứt dây nghiêm trọng nhất

| # | Đứt ở đâu | Thiệt hại |
|---|---|---|
| **1** | Panel nguyên liệu chỉ 1 ô thẻ | 16/20 món không nấu nổi |
| **2** | `OrderBoardManager` không có trong scene | 26 mission đứng im, chuỗi chính tắc từ cấp 2 |
| **3** | Nấu ăn không sinh vàng, `DishData` không có trường thưởng | Vòng lặp kinh tế không khép |
| **4** | Nguyên liệu `ca` không tồn tại ở đâu cả | 2 món chết cứng |
| **5** | `dailyMissionDatabase = {fileID: 0}` trên **cả 2 popup** | **Tab nhiệm vụ hằng ngày trống rỗng** — mất vòng lặp giữ chân người chơi mỗi ngày |

## Hệ chết hoàn toàn

- **`QuestManager` + 4 UI** — không có trong scene nào. `CookingChallengeManager` gọi `QuestManager.Instance.OnItemCooked()` luôn bị bỏ qua. Bản thân `QuestManager.cs:185` còn ghi `// TODO: Give rewards`.
- **`PlayerWallet`** — ví mồ côi, không nối `FarmEconomyManager`.
- **`FarmManager.ConsumeSeed()` + `seedStocks`** — hệ hạt giống thứ hai, có dữ liệu trong scene nhưng **không bao giờ được gọi**.
- **`KitchenTransferManager.OnTransferredItemsChanged`** — bắn 5 chỗ, **0 người nghe**.

## Nguồn thu hiện tại

**Vàng lặp lại: chỉ còn Quầy Hàng.** Bảng đơn chưa dựng, nấu ăn không cho vàng.

**EXP lặp lại:** thu hoạch cây · nấu ăn (20 cứng) · tàu hoả (10/toa). Bảng đơn chết.

`PlayerStallManager` bán được hàng nhưng **không báo mission, không cộng EXP**.

---

# PHẦN 4 — THỨ TỰ NÊN LÀM

## Giai đoạn 1 — Chữa cái đang hỏng (không cần vẽ gì)

| # | Việc | Công sức |
|---|---|---|
| 1 | Chạy tool dựng bảng đơn hàng | 5 phút |
| 2 | **Nhân ô thẻ nguyên liệu trong scene bếp** | nửa buổi |
| 3 | Sửa 9 cặp `plotId` trùng | nửa buổi |
| 4 | Gán `dailyMissionDatabase` cho 2 popup | 5 phút |
| 5 | Cân lại chuồng trại (sản lượng, thời gian, thức ăn) | nửa buổi |
| 6 | Thêm trường thưởng vào `DishData` + cộng vàng khi nấu | nửa buổi |
| 7 | Khoá cổng bếp tới cấp 5 | 15 phút |
| 8 | Sửa `nuoc_mia_chanh`, thêm `Item_Milk`, xoá 2 món cá | 1 giờ |

Xong giai đoạn này: **20/20 món nấu được, 26 mission chạy lại, kinh tế không vỡ, không mất dữ liệu cây trồng.**

## Giai đoạn 2 — Cân bằng lại số

| # | Việc |
|---|---|
| 9 | Sửa 4 nghịch lý thời gian trồng · phân hoá 10 loại hoa |
| 10 | Sửa 3 loại hoa bán lỗ |
| 11 | Bật lại hệ khoá ô đất theo cấp, giá ô tăng luỹ tiến |
| 12 | Enforce sức chứa kho trong `FarmInventoryManager.AddItem` |
| 13 | Tăng tốc gem theo thời gian còn lại, không cứng 1 gem |
| 14 | Xem lại `realTimeMultiplier = 0.3` — hiện 1 giờ chơi lên 30 cấp |

## Giai đoạn 3 — Thêm chiều sâu

| # | Việc | Cần vẽ? |
|---|---|---|
| 15 | **Tưới nước** — hàm đã viết sẵn, chỉ cần nối + bình tưới | icon + hiệu ứng nước |
| 16 | Cây héo nếu bỏ quên quá lâu | sprite cây héo |
| 17 | Nội dung sau cấp 15 — cây mới, chuồng mới, khu đất mới | nhiều |
| 18 | Hệ thống nhiệm vụ/thành tựu/sự kiện (bản thiết kế đã có) | prefab UI |
| 19 | Chất lượng nông sản (thường/bạc/vàng) | icon 3 bậc |

## Giai đoạn 4 — Nội dung mới

| # | Việc |
|---|---|
| 20 | Hồ cá — mở 2 món cá đang chết |
| 21 | Cây Rau thơm — `herbs` hiện chỉ mua được ở chợ, chặn 4 món |
| 22 | 8–10 món ăn mới cho quãng cấp 15–30 |
| 23 | Multiplayer chợ (tầng dữ liệu đã viết sẵn theo hình dạng server) |

---

# TÓM TẮT MỘT DÒNG

Dự án có **hạ tầng kỹ thuật rất chắc** (save, offline, kéo thả, batch, placement, tutorial) nhưng **ba vòng lặp cốt lõi đều đang hỏng ở tầng dữ liệu**: nấu ăn không nấu được, đơn hàng chưa cắm vào scene, và chuồng trại làm ruộng thành vô nghĩa.

Sửa xong giai đoạn 1 là game **chơi được trọn vòng** lần đầu tiên.
