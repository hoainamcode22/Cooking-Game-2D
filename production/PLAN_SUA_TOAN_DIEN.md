# KẾ HOẠCH SỬA TOÀN DIỆN — chờ chủ dự án duyệt

---

# PHẦN 1 — TẠI SAO KHÔNG NẤU ĐƯỢC MÓN?

## Không phải do dữ liệu. Dữ liệu đúng hết.

Tôi truy cả chuỗi từ shop tới nồi:

```
Mua hạt ở Shop        →  WarehouseManager           ✅ chạy đúng
Trồng xuống ruộng     →  trừ hạt khỏi kho           ✅ chạy đúng
Thu hoạch             →  FarmInventoryManager       ✅ chạy đúng
Thu sản phẩm chuồng   →  FarmInventoryManager       ✅ chạy đúng
Nấu xong nhận món     →  FarmInventoryManager       ✅ chạy đúng
Bấm "gửi vào bếp"     →  KitchenTransferManager     ✅ chạy đúng
Vào scene bếp, nạp thẻ →  ApplyToCardGroup()        🔴 ĐỨT Ở ĐÂY
```

## Chỗ đứt: bếp chỉ có ĐÚNG MỘT Ô THẺ

Kiểm trực tiếp trong `SampleScene.unity`:

| Container | Số ô thẻ thật |
|---|---|
| `Content_Ingredients` | **1 con** (`Item_Ingredient_Beef`) |
| `Content_Seasonings` | **1 con** (`Item_Seasoning_FishSauce`) |

Cả scene chỉ có **3 PrefabInstance**, trong đó đúng 1 thẻ nguyên liệu + 1 thẻ gia vị.

Và `CookingBoot.ApplyToCardGroup` (`:107-138`) **chỉ tái dùng con có sẵn, không bao giờ `Instantiate` thêm**:

```csharp
foreach (Transform child in contentRoot)      // đếm con CÓ SẴN
    cards.Add(card);

for (int i = 0; i < cards.Count; i++)         // lặp theo SỐ Ô, không theo SỐ HÀNG
    ...
if (items.Count > cards.Count)
    Debug.LogWarning("Không đủ slot…");        // phần dư bị bỏ im lặng
```

## Hậu quả

Người chơi gửi 10 loại nguyên liệu vào bếp → **chỉ 1 loại hiện lên màn hình**, và họ **không chọn được loại nào** vì đó là phần tử đầu tiên trong Dictionary.

Luật chấm điểm: nguyên liệu **70 điểm** · gia vị **30 điểm** · **thiếu một nguyên liệu là tụt còn 35** · ngưỡng đạt **70**.

⇒ Món cần 2 nguyên liệu trở lên **không bao giờ đạt nổi 70**.

| Nấu được (4 món) | Vì sao |
|---|---|
| Khoai tây chiên · Gà nướng lu · Gà xào ớt · Bò xào tiêu | **đều chỉ cần đúng 1 nguyên liệu** |

16 món còn lại chết vì thiếu ô thẻ. **Đây là lỗi giao diện, không phải lỗi dữ liệu.**

## Ba món chết vì lý do khác

| Món | Lý do |
|---|---|
| `nuoc_mia_chanh` | Cả 2 nguyên liệu đều `kind = Seasoning` → điểm nguyên liệu = 0 → trần 30 điểm |
| `ca_nuong_tieu` · `canh_chua_ca` | Nguyên liệu `ca` **không tồn tại ở đâu cả** — không `InventoryItemData`, không cây, không chuồng, không dòng chợ |

## Còn một món có nguồn mà không vào bếp được

`milk` — chuồng bò sữa sản xuất ra, vào kho đúng, nhưng `Item_Milk.asset` **không nằm trong danh sách `cookingInventoryItems`** nên không bao giờ chuyển vào bếp được.

---

# PHẦN 2 — SÁU NHÓM VIỆC

## Nhóm A — Mở khoá nấu ăn (không cần vẽ gì)

| # | Việc | Cách làm |
|---|---|---|
| A1 | **Sửa `ApplyToCardGroup` để `Instantiate` theo số hàng thật** | Thêm prefab thẻ vào `LeftPanelRefs`, sinh thiếu thì tạo, thừa thì tắt. Sửa ở code chứ không nhân tay trong scene — nhân tay thì lần sau lại thiếu |
| A2 | Sửa `Dish_nuoc_mia_chanh` | Thêm một `IngredientData` `kind = Ingredient` (mía) |
| A3 | Thêm `Item_Milk` vào `cookingInventoryItems` | Sữa mới vào bếp được |
| A4 | **Xoá 2 món cá** `ca_nuong_tieu`, `canh_chua_ca` | Chưa có hệ hồ cá thì giữ làm gì. Xoá sạch cả `ING_Fish`, dòng chợ, dòng bảng giá |
| A5 | Thưởng nấu ăn | Thêm `rewardExp`/`rewardGold` vào `DishData`, thưởng theo độ khó × điểm số. Hiện **mọi món cứng 20 EXP, 0 vàng** |
| A6 | Khoá cổng bếp tới cấp 5 | Hiện cấp 1 vào được nhưng món thấp nhất cấp 5 → màn hình chết |
| A7 | Dọn `IngredientData` trùng ở 2 thư mục | `SEA_Pepper` bản trùng có `kind` **sai** — ai đổi tham chiếu là điểm tụt từ 70 xuống 35 |
| A8 | Xoá 1 trong 2 `CookingGate` trùng | |

**Xong nhóm A: 18/18 món nấu được** (20 trừ 2 món cá đã xoá).

## Nhóm B — Lưu tiến trình như người chơi thật

Yêu cầu: đã qua rồi thì không nhắc lại, **đặc biệt là tutorial**.

| # | Việc |
|---|---|
| B1 | **Cờ "đã xong tutorial"** lưu vào PlayerPrefs. Hiện `TutorialManager.Start()` chạy lại từ bước 0 **mỗi lần Play** |
| B2 | Kèm công tắc dev để bật lại khi cần test |
| B3 | Rà mọi popup một-lần (lên cấp, mở khoá, giới thiệu) xem có lưu cờ chưa |
| B4 | Kiểm toàn bộ save: `saveVersion` + nhánh migrate cho mọi khoá |

## Nhóm C — Xoá các tính năng không có

Theo yêu cầu, **xoá hẳn** chứ không để code chết gây hiểu nhầm:

| # | Xoá gì |
|---|---|
| C1 | `PlotController.ApplyWaterBonus()` — viết sẵn, 0 nơi gọi |
| C2 | Mọi dấu vết bón phân / sâu bệnh / cỏ dại (grep ra 0 kết quả, chỉ cần xác nhận) |
| C3 | `CropData.canDropFromAds` — bật cho 11 cây nhưng **không có hệ thống ads nào** |
| C4 | `canAppearInRareMarket` — 21/21 asset đều = 0 |
| C5 | `CropData.tier` — dead field, giá trị lộn xộn |
| C6 | `FarmManager.ConsumeSeed()` + `seedStocks` + `seedStockMap` — hệ hạt giống thứ hai, có dữ liệu trong scene mà **không bao giờ được gọi** |
| C7 | `PlayerWallet` + `MissionItemUI` — ví mồ côi, thưởng rơi vào hư không |
| C8 | `QuestManager` + `QuestHUDController` + `QuestItemUI` + `QuestPopupController` + `AchievementItemUI` — chết hoàn toàn, `QuestManager.cs:185` còn ghi `// TODO: Give rewards` |
| C9 | `CookingStackSlotUI` · `CookingScoreCalculator.IsSameIngredient()` · `rareBonus`/`techniqueBonus` luôn = 0 |
| C10 | `KitchenTransferManager.OnTransferredItemsChanged` — bắn 5 chỗ, **0 người nghe** |
| C11 | `CookingSelectionManager` mồ côi trên prefab thẻ + 6 field không ai đọc |
| C12 | `CropData_Wheat.asset` — asset rác không có `harvestItemId` |
| C13 | `SEA_Milk` (không món nào dùng) · 3 `InventoryItemData` máy chế biến có `cookingData` rỗng |

## Nhóm D — Sắp lại thời gian từ bé đến lớn

### D1 · Cây trồng — hiện có 4 nghịch lý

| Cây | Cấp | Hiện tại | **Đề xuất** |
|---|---|---|---|
| Lúa | 1 | 54s | **50s** |
| Hướng dương | 1 | 54s | **55s** |
| Bắp cải | 1 | 90s | **70s** |
| Ngô | 2 | 108s | **95s** |
| Cà rốt | 3 | 120s | **120s** |
| Cà chua | 3 | 144s | **145s** |
| Hoa hồng | 4 | 54s | **170s** |
| Oải hương | 4 | 54s | **195s** |
| Khoai tây | 5 | 150s | **220s** |
| Nấm | 6 | 180s | **250s** |
| Hoa lan | 7 | 54s | **280s** |
| Cúc trắng | 7 | 54s | **310s** |
| **Mía** | 7 | **126s** ⚠ | **340s** |
| **Chanh** | 8 | **234s** | **380s** |
| Tulip | 9 | 54s | **420s** |
| Cúc vạn thọ | 9 | 54s | **460s** |
| **Ớt** | 9 | **162s** ⚠ | **500s** |
| **Tiêu** | 10 | **198s** ⚠ | **560s** |
| Mẫu đơn | 10 | 54s | **600s** |
| Cẩm tú cầu | 10 | 54s | **650s** |
| Anh thảo | 10 | 54s | **700s** |

Nguyên tắc: **cấp mở càng cao thì càng lâu, không có ngoại lệ.** Và 10 loại hoa **không còn dùng chung một con số** — hiện hoa cấp 1 và cấp 10 chín cùng lúc.

Giá bán phải tăng theo thời gian, nếu không cây cấp cao thành vô nghĩa. **Ba loại hoa đang bán lỗ** (Cúc trắng −14, Cẩm tú cầu 0, Anh thảo −2) sẽ được sửa cùng lúc.

### D2 · Chuồng và máy

| Chuồng | Cấp | Hiện tại | **Đề xuất** |
|---|---|---|---|
| Chuồng Gà | 2 | 30s · 1 thức ăn · 4+4 sản phẩm | **90s · 2 thức ăn · 1+1** |
| Chuồng Heo | 4 | 30s · 1 · 4 | **150s · 2 · 1** |
| Chuồng Bò | 6 | 30s · 1 · 4 | **240s · 3 · 1** |
| Chuồng Bò Sữa | 8 | 30s · 1 · 4 | **300s · 3 · 2** |
| Máy Xay Bột | 11 | 60s · 2 | **360s · 2** |
| Máy Ép Mía | 13 | 90s · 2 | **420s · 2** |
| Máy Phô Mai | 15 | 120s · 2 | **480s · 2** |

⚠️ `feedDurationSeconds` hiện **không nhân `realTimeMultiplier`** trong khi cây trồng thì có → hai hệ đo thời gian khác nhau. Phải thống nhất.

## Nhóm E — Chuồng đang bị gì

| Vấn đề | Chi tiết |
|---|---|
| **Kinh tế vỡ** | 1 lúa (7 vàng) → chuồng gà ra **320 vàng trong 30 giây**. Ruộng tiêu tốt nhất lãi 30 vàng trong 198 giây. **Chuồng lãi gấp ~70 lần mỗi giây**, EXP gấp 9 lần. Từ cấp 2 trồng trọt thành vô nghĩa |
| **Sữa không vào bếp được** | Sản xuất đúng, vào kho đúng, nhưng `Item_Milk` không nằm trong `cookingInventoryItems` |
| **3/4 chuồng cùng công thức** | Bò thịt, gà, bò sữa đều ăn rice/ngo. Bắp cải và cà rốt chỉ dùng cho chuồng heo |
| Không báo mission khi thu | ✅ Thật ra CÓ (`PenMiniPanelUI.cs:201,245,249`) |
| Chuyển vào bếp | Sản phẩm chuồng **đã vào đúng `FarmInventoryManager`**, cùng kho mà `WarehousePopupUI` đọc → chuyển vào bếp được, chỉ thiếu `Item_Milk` |

**Việc:** cân lại số theo bảng D2 · thêm `Item_Milk` · đa dạng công thức thức ăn · thống nhất đơn vị thời gian.

## Nhóm F — Nối lại các dây đứt

| # | Đứt gì | Sửa |
|---|---|---|
| F1 | **9 cặp ô đất trùng `plotId`** (2,3,4,5,6,7,8,26,27) dùng chung khoá lưu | Cấp lại id duy nhất. **Đây là lỗi mất dữ liệu, ưu tiên cao** |
| F2 | `dailyMissionDatabase = {fileID: 0}` ở **cả 2 popup** → tab nhiệm vụ hằng ngày **trống rỗng** | Gán asset đã có sẵn |
| F3 | `MissionProgressTracker` không có instance → hook `OnLevelChanged` không cài | Thêm object vào scene |
| F4 | `PlayerStallManager` bán được hàng nhưng **không báo mission, không cộng EXP** | Nối hook |
| F5 | `TrainManager` thiếu 4 ref FX · chỉ 3 slot thưởng cho 4 toa · không báo mission · `TrainInventoryAdapter` **vứt icon** đi | Gán ref, thêm slot, nối hook, giữ icon |
| F6 | 3 nút trong `SCN_Farm` gọi `SetActive` với target rỗng → bấm không làm gì | Gán hoặc xoá nút |
| F7 | `UnifiedTaskPopupUI` có **14 sprite ref rỗng** → popup dựng bằng khối màu trơn | Gán art hoặc sinh sprite thủ tục |
| F8 | Sức chứa kho **chỉ là chữ** — `FarmInventoryManager.AddItem` không kiểm một dòng nào | Enforce, hoặc bỏ hẳn UI sức chứa cho khỏi lừa người chơi |
| F9 | Tăng tốc gem **cứng 1 gem** cho mọi cây | Theo thời gian còn lại |
| F10 | Hệ khoá ô đất theo cấp/gem **bị `unlockAllPlotsForLayout: 1` tắt hoàn toàn** | Quyết định: bật lại hoặc xoá code chết |

---

# PHẦN 3 — CÁCH LÀM VIỆC

**2 dev + 1 tester, trao đổi qua file `TEAM_SUA_TOAN_DIEN.md`.**

| Ai | Làm gì |
|---|---|
| **DEV-A** | Nhóm A (nấu ăn) + Nhóm B (lưu tiến trình) + Nhóm C (xoá code chết) |
| **DEV-B** | Nhóm D (thời gian) + Nhóm E (chuồng) + Nhóm F (nối dây) |
| **TESTER** | Kiểm biên dịch → **chơi thử từ cấp 1 tới cấp 30** → tìm chức năng chết → đề xuất sửa → hội thoại với 2 dev trong file → fix tới khi sạch |

Vòng lặp: dev làm → tester tìm lỗi → ghi vào file → dev sửa → tester kiểm lại. Lặp tới khi không còn lỗi.

---

# PHẦN 4 — CẦN BẠN DUYỆT

1. **Bảng thời gian cây trồng D1** — 50s tới 700s, cấp cao thì lâu hơn. Có quá lâu ở cấp 10 không?
2. **Bảng chuồng D2** — giảm sản lượng từ 4 xuống 1, tăng thời gian 30s lên 90–300s. Đây là thay đổi lớn nhất về cân bằng.
3. **Xoá 2 món cá** — hay giữ lại chờ làm hồ cá sau?
4. **Sức chứa kho (F8)** — enforce thật, hay bỏ hẳn UI đó?
5. **Hệ khoá ô đất (F10)** — bật lại theo cấp, hay xoá code chết và giữ cách mua công trình?
6. **`realTimeMultiplier = 0.3`** — giữ để test cho nhanh, hay trả về 1.0? Hiện một giờ chơi lên 30 cấp.

Duyệt xong tôi giao việc ngay.
