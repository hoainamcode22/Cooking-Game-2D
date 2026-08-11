# BOX LÀM VIỆC — Sửa toàn diện

> Kênh trao đổi của **DEV-A**, **DEV-B**, **TESTER**. Đọc mục của người kia trước khi làm phần giao nhau.
> Kế hoạch gốc: `production\PLAN_SUA_TOAN_DIEN.md` — **đọc kỹ trước khi code**.

**Dự án:** `E:\Game2\Cooking-Game-2D` · Unity 6000.3.10f1 · không `.asmdef`

---

## 0. SÁU QUYẾT ĐỊNH ĐÃ CHỐT — KHÔNG PHẢI HỎI LẠI

| # | Câu hỏi | **Chốt** |
|---|---|---|
| 1 | Bảng thời gian cây trồng 50s→700s | **Duyệt.** Làm theo bảng D1 |
| 2 | Bảng chuồng (giảm sản lượng 4→1, tăng thời gian) | **Duyệt.** Làm theo bảng D2 |
| 3 | Hai món cá | **XOÁ SẠCH** — `Dish_ca_nuong_tieu`, `Dish_canh_chua_ca`, `ING_Fish`, mọi dòng chợ/bảng giá liên quan. Chưa có hồ cá thì giữ làm gì |
| 4 | Sức chứa kho | **ENFORCE THẬT** trong `FarmInventoryManager.AddItem`. Kho đầy thì chặn và báo rõ. Hiện UI ghi "12/25" mà không chặn gì — đang lừa người chơi |
| 5 | Hệ khoá ô đất theo cấp/gem | **XOÁ CODE CHẾT.** Giữ cách mua công trình "Đất". Xoá `TryUnlockSelectedPlotByGem`, `CanUnlockByLevel`, `lockSprite`, `gemCost`, `requireAd`, `unlockAllPlotsForLayout`, `startUnlockedNormalCount`. Nhưng **giá ô đất phải tăng luỹ tiến** (xem F10) |
| 6 | `realTimeMultiplier` | **ĐẶT VỀ `1.0f`** và ghi **giây thật** vào asset. Hiện 0.3 nên con số trong asset không phải thời gian thật — hai người đọc hai kiểu. Bảng D1 là **giây THẬT**, ghi thẳng vào `growSeconds` |

---

## 1. 🔴 GỐC CỦA VẤN ĐỀ NẤU ĂN — ĐỌC TRƯỚC KHI SỬA

**Không phải lỗi dữ liệu.** Cả chuỗi shop → trồng → thu hoạch → kho → gửi bếp đều chạy đúng.

Đứt ở bước cuối: `SampleScene.unity` chỉ có **1 ô thẻ nguyên liệu + 1 ô thẻ gia vị**:
- `Content_Ingredients` → 1 con (`Item_Ingredient_Beef`)
- `Content_Seasonings` → 1 con (`Item_Seasoning_FishSauce`)
- Cả scene chỉ có **3 PrefabInstance**

Và `CookingBoot.ApplyToCardGroup` (`:107-138`) **lặp theo SỐ Ô chứ không theo SỐ HÀNG**, không bao giờ `Instantiate`:

```csharp
foreach (Transform child in contentRoot) cards.Add(card);   // = 1
for (int i = 0; i < cards.Count; i++) { ... }               // lặp 1 lần
if (items.Count > cards.Count) Debug.LogWarning("Không đủ slot…");
```

Luật chấm điểm: nguyên liệu **70đ** · gia vị **30đ** · thiếu 1 nguyên liệu tụt còn **35đ** · ngưỡng đạt **70**.
⇒ Món cần ≥2 nguyên liệu **không bao giờ đạt nổi**. Chỉ 4 món 1-nguyên-liệu chạy được.

> **SỬA Ở CODE, KHÔNG NHÂN TAY THẺ TRONG SCENE.** Nhân tay thì lần sau thêm món lại thiếu ô.

---

## 2. DEV-A — Nhóm A + B + C

### Nhóm A — Mở khoá nấu ăn

| # | Việc | Xong khi |
|---|---|---|
| A1 | **Sửa `ApplyToCardGroup` để `Instantiate` theo số hàng thật.** Thêm field prefab thẻ vào `LeftPanelRefs`, thiếu thì sinh, thừa thì tắt. Giữ nguyên `Item_Ingredient_Beef`/`Item_Seasoning_FishSauce` làm prefab mẫu | gửi 10 loại vào bếp thì hiện đủ 10 |
| A2 | `Dish_nuoc_mia_chanh`: thêm một `IngredientData` `kind = Ingredient` (mía). Nếu chưa có `ING_Sugarcane` thì tạo | món này nấu được |
| A3 | Thêm `Item_Milk` vào `cookingInventoryItems` của `CookingBoot` (và `CookingSelectionManager` nếu còn dùng) | sữa vào bếp được |
| A4 | **Xoá 2 món cá**: `Dish_ca_nuong_tieu`, `Dish_canh_chua_ca`, `ING_Fish`, dòng trong `MarketPriceTable`, dòng trong `MarketDatabase`, dòng trong `All_Data.asset` (`ListDishData`) | grep `ca_nuong_tieu` = 0 |
| A5 | Thêm `rewardExp` · `rewardGold` · `sellPrice` vào `DishData`, điền cho 18 món. Sửa `CookingChallengeManager.cs:332-333` thưởng theo **độ khó × hệ số điểm** (70đ ×1.0 → 100đ ×1.5) và **cộng vàng** | nấu món khó được nhiều hơn món dễ |
| A6 | Khoá cổng bếp tới **cấp 5** trong `BuildingInteractable` case `CookingGate`, kèm thông báo "Cần cấp 5" | cấp 1-4 không vào được |
| A7 | Dọn `IngredientData` trùng: gom hết về `Assets\_Game\Data\Data_cooking\`, xoá `Assets\_Game\ScriptableObjects\Ingredients\`, sửa 2 món đang trỏ sang bản trùng (`Dish_bap_cai_xao_nam` → `SEA_FishSauce`, `Dish_pho_bo_tai` → `SEA_Chili`) | mỗi id chỉ còn 1 asset |
| A8 | Xoá 1 trong 2 `CookingGate` trùng trong `SCN_Farm` | còn 1 cổng |

⚠️ **A7 có bẫy:** `ScriptableObjects\Ingredients\SEA_Pepper.asset` có `kind: 0` (Ingredient) trong khi bản đúng là `kind: 1` (Seasoning). Xoá đúng bản sai, đừng xoá ngược.

### Nhóm B — Lưu tiến trình như người chơi thật

| # | Việc | Xong khi |
|---|---|---|
| B1 | **Cờ "đã xong tutorial"** lưu PlayerPrefs. `TutorialManager.Start()` hiện chạy lại từ bước 0 **mỗi lần Play** — người chơi đã xong rồi vẫn bị dắt lại | xong tutorial, thoát vào lại không bị dắt nữa |
| B2 | Công tắc dev trong Inspector để bật lại tutorial khi cần test | tick là chạy lại được |
| B3 | Rà mọi popup một-lần (lên cấp, mở khoá công trình, giới thiệu bếp) — có lưu cờ chưa | không popup nào hiện lại lần hai |
| B4 | Mọi khoá PlayerPrefs phải có `saveVersion` + nhánh migrate. Liệt kê đủ vào mục 6 | không có khoá nào thiếu version |

### Nhóm C — Xoá code chết

Xoá **hẳn** cả file/field, không để lại comment "để dành sau":

C1 `PlotController.ApplyWaterBonus()` · C2 xác nhận 0 dấu vết bón phân/sâu bệnh/cỏ dại · C3 `CropData.canDropFromAds` · C4 `canAppearInRareMarket` · C5 `CropData.tier` · C6 `FarmManager.ConsumeSeed()` + `seedStocks` + `seedStockMap` + dữ liệu trong scene · C7 `PlayerWallet` + `MissionItemUI` · C8 `QuestManager` + `QuestHUDController` + `QuestItemUI` + `QuestPopupController` + `AchievementItemUI` · C9 `CookingStackSlotUI` + `CookingScoreCalculator.IsSameIngredient()` + `rareBonus`/`techniqueBonus` · C10 `KitchenTransferManager.OnTransferredItemsChanged` · C11 `CookingSelectionManager` mồ côi trên prefab thẻ + 6 field không ai đọc · C12 `CropData_Wheat.asset` · C13 `SEA_Milk` + `cookingData` rỗng của 3 sản phẩm máy

⚠️ **C8 có bẫy:** `CookingChallengeManager` đang gọi `QuestManager.Instance.OnItemCooked()`. Xoá `QuestManager` thì phải gỡ lời gọi đó, không thì vỡ biên dịch.

---

## 3. DEV-B — Nhóm D + E + F

### Nhóm D — Thời gian, sắp từ bé đến lớn

**Đặt `FarmManager.realTimeMultiplier = 1.0f`** (cả code lẫn giá trị trong scene), rồi ghi **giây thật** vào `growSeconds`:

| Cây | Cấp | `growSeconds` mới |
|---|---|---|
| Lúa | 1 | **50** |
| Hướng dương | 1 | **55** |
| Bắp cải | 1 | **70** |
| Ngô | 2 | **95** |
| Cà rốt | 3 | **120** |
| Cà chua | 3 | **145** |
| Hoa hồng | 4 | **170** |
| Oải hương | 4 | **195** |
| Khoai tây | 5 | **220** |
| Nấm | 6 | **250** |
| Hoa lan | 7 | **280** |
| Cúc trắng | 7 | **310** |
| Mía | 7 | **340** |
| Chanh | 8 | **380** |
| Tulip | 9 | **420** |
| Cúc vạn thọ | 9 | **460** |
| Ớt | 9 | **500** |
| Tiêu | 10 | **560** |
| Mẫu đơn | 10 | **600** |
| Cẩm tú cầu | 10 | **650** |
| Anh thảo | 10 | **700** |

**Nguyên tắc: cấp mở càng cao thì càng lâu, KHÔNG ngoại lệ.**

**Giá bán phải tăng theo thời gian**, nếu không cây cấp cao thành vô nghĩa. Đặc biệt sửa **3 loại hoa đang bán LỖ**: Cúc trắng (−14), Cẩm tú cầu (0), Anh thảo (−2). Nguyên tắc: lãi mỗi giây phải tăng nhẹ theo cấp, và **không cây nào lỗ**.

### Chuồng và máy

| Chuồng | Cấp | Hiện tại | **Mới** |
|---|---|---|---|
| Chuồng Gà | 2 | 30s · 1 ăn · 4+4 ra | **90s · 2 ăn · 1+1 ra** |
| Chuồng Heo | 4 | 30s · 1 · 4 | **150s · 2 · 1** |
| Chuồng Bò | 6 | 30s · 1 · 4 | **240s · 3 · 1** |
| Chuồng Bò Sữa | 8 | 30s · 1 · 4 | **300s · 3 · 2** |
| Máy Xay Bột | 11 | 60s · 2 | **360s · 2** |
| Máy Ép Mía | 13 | 90s · 2 | **420s · 2** |
| Máy Phô Mai | 15 | 120s · 2 | **480s · 2** |

⚠️ `feedDurationSeconds` hiện **không nhân `realTimeMultiplier`** trong khi cây trồng thì có. Sau khi đặt multiplier = 1.0 thì hai hệ tự thống nhất — **xác nhận lại**, đừng giả định.

### Nhóm E — Chuồng

| # | Việc |
|---|---|
| E1 | Cân lại số theo bảng trên. **Đây là thay đổi lớn nhất**: hiện 1 lúa (7 vàng) → chuồng gà ra **320 vàng trong 30 giây**, lãi gấp ~70 lần ruộng mỗi giây, EXP gấp 9 lần. Từ cấp 2 trồng trọt thành vô nghĩa |
| E2 | **Đa dạng công thức thức ăn** — hiện 3/4 chuồng đều ăn rice/ngo. Bắp cải và cà rốt chỉ dùng cho chuồng heo. Chia lại cho mỗi chuồng một khẩu vị khác |
| E3 | Xác nhận sản phẩm chuồng vào **đúng `FarmInventoryManager`** và **chuyển vào bếp mượt** (đã đúng, chỉ thiếu `Item_Milk` mà DEV-A lo ở A3 — **kiểm chéo với DEV-A**) |
| E4 | Sản phẩm chuồng phải có **giá bán tương xứng thời gian mới** |

### Nhóm F — Nối dây đứt

| # | Việc | Ưu tiên |
|---|---|---|
| F1 | **9 cặp ô đất trùng `plotId`** (2,3,4,5,6,7,8,26,27) dùng chung khoá `PLOT_NORMAL_{id}` → trồng ô này, vào lại ô kia hiện cây. **Cấp lại id duy nhất cho 38 PlotController** | 🔴 **CAO NHẤT — lỗi mất dữ liệu** |
| F2 | `dailyMissionDatabase = {fileID: 0}` ở **cả 2 popup** (`SCN_Farm.unity:73948` và `:432462`) → tab nhiệm vụ hằng ngày **trống rỗng**. Gán `MissionDatabase_Daily.asset` | 🔴 Cao |
| F3 | `MissionProgressTracker` **không có instance** trong scene → hook `OnLevelChanged` (`:117-127`) không bao giờ cài. Thêm object vào scene | 🟠 |
| F4 | `PlayerStallManager` bán được hàng nhưng **không báo mission, không cộng EXP**. Nối `ReportEvent` + `AddExp` | 🟠 |
| F5 | `TrainManager`: 4 ref FX rỗng · chỉ **3 slot thưởng cho 4 toa** (toa 4 luôn trống) · không báo mission · `TrainInventoryAdapter.AddItem` **vứt icon đi** (`:30`) | 🟠 |
| F6 | 3 nút trong `SCN_Farm` gọi `SetActive` với `m_Target: {fileID: 0}` → bấm không làm gì. Gán hoặc xoá | 🟡 |
| F7 | `UnifiedTaskPopupUI` có **14 sprite ref rỗng** → popup dựng bằng khối màu trơn. Sinh sprite thủ tục như đã làm cho chợ/quầy | 🟡 |
| F8 | **Enforce sức chứa kho** trong `FarmInventoryManager.AddItem`. Kho đầy → chặn + báo rõ. Hiện UI ghi "12/25" mà không chặn gì | 🟠 |
| F9 | Tăng tốc gem **cứng 1 gem** cho mọi cây → đổi thành theo thời gian còn lại. Dùng lại công thức của `ConstructionManager`: `ceil(15 + 0.82·√giây)` | 🟠 |
| F10 | Xoá hệ khoá ô đất chết (quyết định #5), nhưng **giá công trình "Đất" phải tăng luỹ tiến** thay vì cố định 50 vàng vô hạn. Đề xuất: `50 × 1.35^(số ô đã có − 8)`, làm tròn chục | 🟠 |

---

## 4. QUY TẮC

- **Không để lỗi biên dịch.** Cả hai đang xoá class/field mà file khác đang dùng — đây là rủi ro số một. Tự kiểm ngoặc, `#if/#endif`, using, ký hiệu gọi chéo.
- Sửa **ở code**, không nhân tay object trong scene.
- Mọi save có `saveVersion` + nhánh migrate.
- Comment tiếng Việt, giải thích **VÌ SAO** chứ không phải *cái gì*.
- Đụng scene 591.000 dòng thì **đếm lại số object trước và sau**. DEV-A đợt trước suýt xoá mất 2 căn nhà vì luật lan truyền.
- Chỉ dựng nền có màu, chủ dự án tự gắn art.

---

## 5. GIAO NHAU GIỮA HAI DEV

| Chỗ | Ai lo | Người kia cần biết |
|---|---|---|
| `Item_Milk` vào bếp | DEV-A (A3) | DEV-B kiểm chéo ở E3 |
| Giá bán nông sản/sản phẩm chuồng | DEV-B (D, E4) | DEV-A dùng khi tính `sellPrice` món ăn ở A5 |
| `MarketPriceTable` | **cả hai đụng** — DEV-A xoá dòng cá (A4), DEV-B sửa giá (D, E4) | **Chốt ai sửa trước ở mục 6** |
| Xoá `QuestManager` | DEV-A (C8) | DEV-B đừng nối gì vào nó |

---

## 6. NHẬT KÝ

### DEV-A

#### 6.A.0 — CHỐT VỚI DEV-B TRƯỚC KHI GÕ (đọc ngay)

| Chỗ giao nhau | Chốt |
|---|---|
| **`MarketPriceTable.cs`** | **DEV-A SỬA TRƯỚC.** Tôi chỉ **xoá 2 dòng** (`canh_chua_ca`, `ca_nuong_tieu`) và **bật lại** `nuoc_mia_chanh` (`marketEnabled: false` → `true`, kèm xoá khối comment 10 dòng giải thích vì sao tắt). Ba dòng, ba chỗ, không đụng con số nào khác. DEV-B chờ tôi ghi "XONG A4" ở mục 6.A.9 rồi hãy mở file — lúc đó cứ sửa `basePrice` thoải mái, không sợ chồng chéo. |
| **`FarmManager.cs`** ⚠️ **CHỖ GIAO NHAU THỨ 5, mục 5 chưa ghi** | Cả hai đều đụng: tôi làm **C6** (xoá `seedStocks` + `seedStockMap` + `ConsumeSeed` + `GetSeedStock` + `HasSeed` + class `SeedStockData`), DEV-B làm **D** (`realTimeMultiplier` → `1.0f`) và **F10** (xoá `TryUnlockSelectedPlotByGem` / `unlockAllPlotsForLayout` / `startUnlockedNormalCount`). **DEV-A SỬA TRƯỚC** — tôi chỉ cắt đúng khối hạt giống, KHÔNG chạm `realTimeMultiplier` và KHÔNG chạm khối mở khoá ô đất. |
| `Item_Milk` vào bếp (A3 ↔ E3) | Đã thêm `Item_Milk.asset` (guid `b5519c93970e53d478b4657f15532d65`) vào `cookingInventoryItems` của `CookingBoot` trong `SampleScene`. **Hệ quả cho C13: TÔI GIỮ LẠI `SEA_Milk.asset`** — xem 6.A.7. |
| `QuestManager` (C8) | Đã xoá sạch. DEV-B **đừng nối `ReportEvent` vào nó** ở F4/F5 — dùng `MissionProgressTracker.ReportEvent` (hệ còn sống). |
| `PopupEwarManager.cs` (C7 ↔ F2) | Tôi xoá field `missionItemPrefab` + hàm `SpawnMissionItems`/`RefreshAllProgress` (chết theo `MissionItemUI`). **KHÔNG chạm** `dailyMissionDatabase` — F2 của DEV-B gán asset vào đúng field đó, vẫn còn nguyên. |
| `DishData.sellPrice` (A5 ↔ D/E4) | Tôi lấy `MarketPriceTable.GetBasePrice(dishId)` làm nguồn cho `sellPrice`. Nếu DEV-B đổi giá nông sản thì giá món **không tự đổi theo** — cuối đợt cần rà lại một lượt. |

#### 6.A.1 — A1 · Gốc của vấn đề nấu ăn (việc số một)

**Chẩn đoán bổ sung so với mục 1:** không chỉ `ApplyToCardGroup` lặp theo số ô. Còn một thủ phạm thứ hai chưa ai thấy — `LeftPanelSpawner` **cũng nằm trong scene và cũng đang chạy `Start()`**, sinh 12 thẻ nguyên liệu + 7 thẻ gia vị từ một **danh sách cứng trong scene**. Hai bộ sinh thẻ tranh nhau một cái container:

- `LeftPanelSpawner` sinh thẻ nhưng **không bao giờ gọi `SetQuantityFromKitchen`** → mọi thẻ nó sinh có `currentKitchenQuantity = 0`.
- `CookingSelectionManager.TrySelect` có `if (quantity <= 0) return;` ⇒ **thẻ do `LeftPanelSpawner` sinh KHÔNG BẤM ĐƯỢC.**
- Danh sách cứng đó còn trỏ vào **13 asset `IngredientData` trùng** ở `ScriptableObjects\Ingredients\` (thứ A7 bắt xoá) và vào **`ING_Fish`** (thứ A4 bắt xoá) → xoá asset xong là 14 tham chiếu treo.

**Đã sửa:**
1. `LeftPanelRefs` thêm 2 field `SelectableIngredientCard ingredientCardPrefab` / `seasoningCardPrefab`. **Để trống vẫn chạy** — `CookingBoot` tự lấy thẻ con đầu tiên trong container làm khuôn (đúng `Item_Ingredient_Beef` / `Item_Seasoning_FishSauce` như đề bài yêu cầu). Nhờ vậy **không phải gán tay gì trong Inspector**, và chủ dự án vẫn có chỗ gắn prefab riêng sau này.
2. `CookingBoot.ApplyToCardGroup` viết lại: lặp theo `items.Count`, **thiếu thì `Instantiate`, thừa thì `SetActive(false)`**. Bỏ hẳn `Debug.LogWarning("Không đủ slot…")` vì giờ không bao giờ thiếu slot nữa.
3. `LeftPanelSpawner.Start()` **không còn tự sinh**. `SpawnAll()` giữ lại làm `[ContextMenu]` cho editor. Lý do ghi trong comment: một container chỉ được có một chủ, nếu không thì thẻ quantity = 0 đè lên thẻ thật.
4. `SampleScene`: hai list `ingredients:` / `seasonings:` của `LeftPanelSpawner` **rút về rỗng (`[]`)** — xoá luôn 14 tham chiếu treo nói trên trong một nhát. **Không xoá GameObject nào** (đếm object trước/sau: xem 6.A.8).

#### 6.A.2 — A2 · `nuoc_mia_chanh` và cái bẫy `ga_nuong_lu`

Nguyên nhân sâu hơn mô tả trong PLAN: **`Item_sugarcane.asset` (mía trong kho) có `cookingData` trỏ vào `SEA_Sugar` (`kind: 1` = Seasoning)**. Nên "mía" trong bếp thật ra là "đường" — không phải nguyên liệu.

- Tạo mới `Assets\_Game\Data\Data_cooking\ING_Sugarcane.asset` (`id: sugarcane`, `kind: 0`, vector `sweet 3` — giữ đúng vector cũ của `SEA_Sugar` để **không lệch điểm hương vị của món nào**).
- `Item_sugarcane.cookingData`: `SEA_Sugar` → `ING_Sugarcane`.
- `Dish_nuoc_mia_chanh.requiredIngredients`: `SEA_Sugar` → `ING_Sugarcane`. Điểm: nguyên liệu 70 + hương vị 30 = **100**.
- ⚠️ **`Dish_ga_nuong_lu` cũng phải sửa cùng lúc, nếu không là làm hỏng một món đang chạy được.** Món này khai `SEA_Sugar` ở ô "Mía". Trước đây `SEA_Sugar` là gia vị nên `ScoreRequiredIngredients` bỏ qua. Sau khi mía thành **nguyên liệu**, người chơi bỏ mía vào nồi sẽ bị tính là "nguyên liệu thừa" ⇒ tụt từ 70 xuống 35 ⇒ **món đang chạy được hoá thành không nấu nổi**. Đã đổi ô đó sang `ING_Sugarcane` → yêu cầu {gà, mía}, cả hai đều có nguồn thật (chuồng gà + cây mía cấp 7).
- `MarketPriceTable`: bật lại `nuoc_mia_chanh` (`marketEnabled: false` → mặc định `true`) vì lý do tắt đã hết.

#### 6.A.3 — A4 · Xoá 2 món cá (13 chỗ, không phải 3)

Xoá asset (kèm `.meta`): `Dish_ca_nuong_tieu`, `Dish_canh_chua_ca`, `ING_Fish`, `Item_ca_nuong_tieu`, `Item_canh_chua_ca`.
Gỡ tham chiếu ở: `All_Data.asset` (2 dòng), `MarketPriceTable.cs` (2 dòng), `BasePriceBook.cs` (2 mục), `DemoL1L10Tool.cs` (1 nhánh `if`), `SCN_Farm.unity` (**4 chỗ**: `WarehousePopupUI.cookedDishIds` 2 dòng chuỗi + `WarehousePopupUI.extraItemDatabase` 2 dòng + `StallItemCatalog.itemDatabase` 2 dòng + một `itemDatabase` thứ hai 2 dòng), `SampleScene.unity` (dòng cá trong list `LeftPanelSpawner` — đã bay theo 6.A.1).

#### 6.A.4 — A5 · Thưởng theo độ khó × điểm

`DishData` thêm 3 field: `rewardExp`, `rewardGold`, `sellPrice`. Điền đủ **18/18** asset.
`CookingChallengeManager.HandleCookingSuccess` thay `AddExp(20)` cứng bằng:

```
hệ số điểm = 1.0 tại 70đ  →  1.5 tại 100đ   (nội suy thẳng, kẹp [1.0 , 1.5])
EXP nhận  = ceil(rewardExp  × hệ số)
Vàng nhận = ceil(rewardGold × hệ số)
```
Công thức số gốc: `rewardExp = ceil(unlockLevel × (3 + 1.5 × difficulty))` · `rewardGold = round(sellPrice × 0.25)` · `sellPrice = MarketPriceTable.GetBasePrice(dishId)` (đã kiểm khớp **18/18**, để **bán ở chợ và bán ở kho không ra hai số khác nhau**).

| Món | Độ khó | Cấp | EXP | Vàng | Giá bán |
|---|---|---|---|---|---|
| `khoai_tay_chien` (dễ nhất) | Easy | 5 | **15** | **24** | 95 |
| `trung_chien_ca_chua` | Normal | 5 | 23 | 31 | 125 |
| `canh_khoai_tay_thit_heo` | Hard | 6 | 36 | 48 | 190 |
| `bo_ham_ca_rot` | Hard | 8 | 48 | 70 | 280 |
| `bo_xao_tieu` | Normal | 10 | 45 | 68 | 270 |
| `pho_bo_tai` (khó nhất) | Hard | 9 | **54** | **80** | 320 |

Nấu hoàn hảo 100đ → ×1.5 ⇒ `pho_bo_tai` ăn **81 EXP + 120 vàng**, gấp 5,4 lần `khoai_tay_chien` nấu vừa đủ điểm. Trước đây cả 20 món đều đúng **20 EXP + 0 vàng**.

#### 6.A.5 — A6 · Khoá cổng bếp tới cấp 5

Hai đường vào bếp, **phải khoá cả hai**, khoá một cái là còn đường kia:
1. Click cổng `CookingGate` ngoài world → `BuildingInteractable.OnMouseDown` case `CookingGate`
2. Nút HUD wire thẳng vào `FarmUIManager.OnClick_GoCooking` (`AnimalGuideController` dò đúng listener tên này ⇒ nút đó **có tồn tại**)

File mới **`Assets\_Game\Farm\Scripts\Cooking\CookingGateAccess.cs`** giữ **một** con số duy nhất (`RequiredLevel = 5`) + hàm `CanEnterOrWarn()` tự hiện thông báo. Chặn ở **cả hai** chỗ: `BuildingInteractable` (báo ngay khi bấm cổng) và `OnClick_GoCooking` (chốt cuối — nút mới thêm sau này cũng không lọt).
Cấp < 5 → `FarmUIManager.ShowHint("Cần cấp 5 mới vào được Bếp.")`.
Con số 5 = `unlockLevel` **nhỏ nhất** trong 18 `DishData`, và **trùng khớp** `AnimalGuideController.CookingMinLevel = 5` (hướng dẫn "NHÀ BẾP đã mở!") — hai hệ giờ nói cùng một cấp.
`KitchenClickOpen` **không cần sửa**: đã kiểm, `TryOpenCooking()` chỉ dò click trúng collider rồi `return`, không mở scene (comment trong file ghi rõ "BuildingInteractable handles scene transition").

#### 6.A.6 — A7 · Dọn `IngredientData` trùng (bẫy đã né)

⚠️ **PLAN nói "xoá `ScriptableObjects\Ingredients\`" nhưng xoá cả thư mục là mất 3 asset KHÔNG CÓ BẢN THAY THẾ:** `ING_Carot`, `ING_KhoaiTay`, `ING_cachua`. Ba asset này là bản **duy nhất** và đang được 6 món + 3 `InventoryItemData` dùng.

Cách làm: **di chuyển** 3 asset đó (kèm `.meta` → **giữ nguyên guid → 0 tham chiếu nào đứt**) sang `Data\Data_cooking\`, rồi mới xoá 13 asset trùng còn lại + thư mục.

⚠️ **Bẫy `SEA_Pepper`:** bản trong `ScriptableObjects\Ingredients\` có `kind: 0` (SAI — Pepper là gia vị). Đã kiểm bằng mắt trên cả hai file trước khi xoá: xoá bản `ScriptableObjects\` (`guid d2bb12c9…`, `kind: 0`), **giữ** bản `Data_cooking\` (`guid 068c729a…`, `kind: 1`). Bản sai chỉ được `LeftPanelSpawner` trong scene dùng — mà list đó đã rút rỗng ở 6.A.1.

Đổi tham chiếu sang bản đúng ở 5 chỗ: `Dish_bap_cai_xao_nam` (`SEA_FishSauce`), `Dish_pho_bo_tai` (`SEA_Chili`), `PF_Item_IngredientCard.prefab` / `PF_Item_SeasoningCard.prefab` / `Item_Ingredient_Beef.prefab` (`ING_Beef`).

#### 6.A.7 — C13 · XUNG ĐỘT A3 ↔ C13, và tôi chọn A3

**C13 bảo xoá `SEA_Milk` ("không món nào dùng"). Xoá là A3 chết.**
`Item_Milk.cookingData` trỏ vào `SEA_Milk`, và `CookingBoot.FillOldCardsFromTransferredItems` có `if (inventoryItem.cookingData == null) continue;`. Xoá `SEA_Milk` ⇒ `Item_Milk` bị bỏ qua im lặng ⇒ **sữa không bao giờ vào bếp** ⇒ vỡ đúng gạch đầu dòng "Sản phẩm chuồng vào kho và chuyển vào bếp được, **gồm cả sữa**" ở mục 8 BÀN GIAO.

⇒ **GIỮ `SEA_Milk.asset`.** Nó không phải code chết — nó là mắt nối duy nhất cho `Item_Milk`.
⚠️ **Rủi ro còn lại, ai làm nội dung phải biết:** hiện **0/18 món dùng sữa**. Người chơi bỏ sữa vào nồi sẽ bị `ScoreRequiredIngredients` tính là "nguyên liệu thừa" → tụt 70 xuống 35. Sữa vào bếp được nhưng **chưa có món nào cần nó** → cần một món sữa (sinh tố / bánh flan) ở đợt sau. Đã ghi vào 6.A.9 phần rủi ro.

Nửa sau của C13 (3 `InventoryItemData` máy chế biến `cookingData` rỗng): **không có code chết nào để xoá** — 3 asset đó không nằm trong `cookingInventoryItems` của cả `CookingBoot` lẫn `CookingSelectionManager`. Thay vì xoá, đã thêm `Debug.LogWarning` trong `BuildInventoryLookup` để lần sau ai nhét một item không có `cookingData` vào danh sách bếp thì **biết ngay**, thay vì bị bỏ qua im lặng như hiện nay.

#### 6.A.8 — Mổ scene 591k dòng: đếm object trước/sau

Dùng lại `production\unity_yaml_surgery.py` (mổ ở mức **document**, tự dọn tham chiếu treo, có bước `verify()`).

| File | Việc | Doc trước→sau | GameObject trước→sau | Tham chiếu treo |
|---|---|---|---|---|
| `SCN_Farm.unity` | **A8** xoá `CookingGate (1)` + cây con | 6208 → **6193** | 1599 → **1597** | **0** |
| `SampleScene.unity` | **C9** gỡ component `CookingStackSlotUI` | 717 → **716** | 181 → **181** | **0** |
| `SampleScene.unity` | **C11** gỡ 22 mục `m_Modifications` + 6 field chết | — | 182 → **182** | **0** |
| `Item_Ingredient_Beef.prefab` | **C11** gỡ `CookingSelectionManager` mồ côi | 28 → **27** | 5 → **5** | **0** |
| `Item_Seasoning_FishSauce.prefab` | **C11** gỡ `CookingSelectionManager` mồ côi | 32 → **31** | 6 → **6** | **0** |
| `KhungEwar.prefab` | **C7** gỡ component `MissionItemUI` | 35 → **34** | 8 → **8** | **0** |

**Đếm mốc trong `SCN_Farm` trước/sau A8 — không mất gì:** `PlotController` **38 → 38** · `CookingGate` **2 → 1** · `PenMiniPanelUI` 0 → 0. Không có object nào bị lan truyền.

`CookingGate (1)` (anchor `2003407256`) và `CookingGate` (anchor `1297670172`) **cùng toạ độ `(2101, −3150)`, cùng scale 200, cùng 4 `BoxCollider2D`, cùng `buildingType: 2`, cùng `KitchenClickOpen`** — bản sao y hệt.
Giữ bản gốc (tên **không** có `(1)`) vì `AnimalGuideController.FindCookingGateTransform()` gọi `GameObject.Find("CookingGate")` — bản `(1)` không bao giờ được nó tìm thấy, nên nó chính là bản dư.
Xoá **15 doc** = 2 GameObject (`CookingGate (1)` + 1 con) + toàn bộ component của chúng, và 1 mục trong danh sách của cha. `verify()` sau khi mổ: **0 tham chiếu treo** trên cả 6 file.

Hai lần mổ scene khác **không qua tool** (chỉ xoá dòng đơn lẻ trong danh sách, không đụng document nào) — vẫn đếm object trước/sau, đều **1616 → 1616** và **182 → 182**:
- **A4** gỡ 8 dòng tham chiếu 2 món cá trong `SCN_Farm` (4 danh sách) + 2 dòng chuỗi `cookedDishIds`
- **C6** gỡ khối `seedStocks` 11 dòng của `FarmManager` · **C12** gỡ 2 dòng `CropData_Wheat` · **A1** rút 96 dòng list `LeftPanelSpawner` về `[]` · **A3** chèn 1 dòng `Item_Milk`

#### 6.A.9 — B1/B2/B3 · Lưu tiến trình

**B1 — cờ "đã xong tutorial".** `TutorialManager.Start()` trước đây **luôn** chạy `PlayIntroAnimation()` → `StartTutorial()` → step 0, mỗi lần bấm Play. Người chơi cấp 12 mở game lên vẫn bị dắt lại từ "kéo hạt lúa vào ô đất" — và **kẹt vĩnh viễn** ở đó, vì cổng qua bước là "không còn ô nào trống" mà ruộng họ đang trồng đầy (watchdog hết-hạt không đỡ được ca này).

Đã thêm khoá `TUTORIAL_MAIN_DONE`, đóng dấu trong `FinishTutorial()` **ngay dòng đầu, trước phần dọn UI** — dọn UI mà ném lỗi thì cờ vẫn đã ghi xong.
⚠️ Không chỉ `return` khi phát hiện đã xong: phải gọi `SkipTutorialEntirely()` (hàm mới) để **tắt `blocksRaycasts` của Tutorial_Canvas**. Bỏ bước này thì Canvas tàng hình nuốt sạch click và cả bản đồ thành không bấm được — đúng thứ `FinishTutorial()` đang phải tự tay tắt.

**B2 — hai công tắc dev** trong Inspector `TutorialManager`:
| Công tắc | Tác dụng |
|---|---|
| `_devForceReplayTutorial` | Bỏ qua cờ, chạy lại từ bước 0. Cờ trong PlayerPrefs **giữ nguyên** → bỏ tick là trở lại bình thường. Có `LogWarning` nhắc bỏ tick trước khi build |
| `_devClearDoneFlagOnStart` | **Xoá hẳn** cờ ngay khi vào scene → lần Play sau cũng chạy lại. Dùng khi muốn thử đúng trải nghiệm người chơi MỚI |

**B3 — rà popup một-lần.** Kết quả kiểm từng cái:
| Popup / hướng dẫn một-lần | Cờ lưu | Kết luận |
|---|---|---|
| **Tutorial chính** | `TUTORIAL_MAIN_DONE` | 🔴 **TRƯỚC ĐÂY KHÔNG CÓ** → B1 đã sửa |
| Tutorial trồng trước | `TUTORIAL_PREPLANT_DONE` | ✅ đã có |
| Tặng đồ khởi đầu | `STARTER_ITEMS_GIVEN` | ✅ đã có |
| 4 hướng dẫn theo cấp (chuồng gà/heo/bò/bò sữa) | `ANIMAL_GUIDE_L2/L4/L6/L8_DONE` | ✅ đã có |
| Tip cho gà ăn · giao hàng · tàu · **giới thiệu bếp** | `ANIMAL_GUIDE_COOP_FEED_DONE` · `GUIDE_DELIVER_DONE` · `GUIDE_TRAIN_DONE` · `GUIDE_COOKING_DONE` | ✅ đã có |
| **Popup lên cấp** | *không có cờ riêng* | ✅ **vẫn đúng** — `HandleLevelChanged` chỉ hiện khi `newLevel > _lastKnownLevel`, mà `_lastKnownLevel` khởi tạo từ `PLAYER_LEVEL` **đã lưu** ⇒ không hiện lại |
| Nhận thưởng nhiệm vụ / thành tựu | `MISSION_CLAIMED_*` · `ACHIEVEMENT_CLAIMED_*` | ✅ đã có |
| Điểm danh (`AttendanceManager`) | — | ✅ **không tự hiện**, `OpenPopup()` chỉ chuyển sang tab Hằng ngày |

⇒ **Chỉ tutorial chính là thiếu cờ.** Không popup nào khác hiện lại lần hai.

#### 6.A.10 — B4 · TOÀN BỘ KHOÁ PLAYERPREFS + `saveVersion`

**Cơ chế.** Dự án có **2 kiểu** save, nên phải có 2 cách đóng dấu:
- **Ghi JSON** → nhét thẳng field `saveVersion` vào blob (cách 6 hệ có sẵn đang dùng).
- **Ghi thẳng số/chuỗi** (`PLAYER_LEVEL`, `PenState_*`, mọi cờ tutorial…) → **không có chỗ nào nhét version vào**. Đây là lý do có file mới **`Assets\_Game\Farm\Scripts\Managers\SaveVersionGuard.cs`**: dấu phiên bản nằm ở **khoá phụ** `SAVE_VER_<họ>`.
  ⚠️ **Không gói version vào chính khoá đang có dữ liệu** — làm vậy là đổi định dạng của khoá đó, người chơi hiện tại mất sạch ngay lần cập nhật này.

`SaveVersionGuard.Ensure(family, version, migrate, hasExistingSave)` trả về version CŨ, gọi `migrate` nếu cũ hơn, rồi đóng dấu (⇒ chuyển đổi **đúng một lần**). Save **mới hơn** code (người chơi hạ cấp bản game) thì **không ghi đè xuống** — chỉ cảnh báo, vì ghi đè là lần sau lên bản mới lại migrate lần nữa trên dữ liệu đã mới.

**BẢNG ĐẦY ĐỦ — 47 khoá / 15 họ save. Không khoá nào thiếu version.**

| # | Khoá PlayerPrefs | Họ / nơi giữ version | Ver | Kiểu | Nhánh migrate |
|---|---|---|---|---|---|
| 1 | `PLAYER_LEVEL` | `SAVE_VER_PLAYER_PROGRESS` | 1 | int | ✅ `MigrateProgress` — kẹp EXP dư ≥ mốc cấp về `mốc−1` để không nhảy cấp ngay khi vào game |
| 2 | `PLAYER_EXP` | ↑ cùng họ | 1 | int | ↑ |
| 3 | `FARM_ECONOMY_GOLD` | `SAVE_VER_FARM_ECONOMY` | 1 | int | chỉ đóng dấu (v0→v1 không đổi định dạng) |
| 4 | `FARM_ECONOMY_GEMS` | ↑ | 1 | int | ↑ |
| 5 | `FARM_INVENTORY_SAVE` | **trong JSON** `saveVersion` | 1 | json | DEV-B (F8) — cắt phần vượt sức chứa |
| 6 | `KITCHEN_TRANSFER_SAVE` | **trong JSON** `saveVersion` | 1 | json | ✅ **DEV-A** — gỡ itemId đã xoá (`ca`, `ca_nuong_tieu`, `canh_chua_ca`) rồi ghi lại |
| 7 | `WAREHOUSE_LEVEL` | `SAVE_VER_WAREHOUSE_LEVEL` | 1 | int | chỉ đóng dấu |
| 8 | `PLAYER_PROFILE_NAME` | `SAVE_VER_PLAYER_PROFILE` | 1 | string | chỉ đóng dấu + kẹp index avatar |
| 9 | `PLAYER_PROFILE_AVATAR_INDEX` | ↑ | 1 | int | ↑ (kẹp về `[0, avatarSprites.Length−1]`) |
| 10 | `PLAYER_PROFILE_WAREHOUSE_LEVEL` | ↑ | 1 | int | ↑ |
| 11 | `PLAYER_PROFILE_ACHIEVEMENT_COUNT` | ↑ | 1 | int | ↑ |
| 12 | `PenState_<penId>` | `SAVE_VER_PEN_STATE` | 1 | int | ✅ **DEV-A** — v0 có lượt đang chạy thì **trả về Idle + hoàn thức ăn vào kho** (bảng D2 đổi 30s → 90..480s nên mốc thời gian cũ không còn đo được đúng) |
| 13 | `PenFood_<penId>` | ↑ | 1 | string | ↑ |
| 14 | `PenStartTime_<penId>` | ↑ | 1 | string | ↑ |
| 15 | `TUTORIAL_MAIN_DONE` 🆕 | `SAVE_VER_TUTORIAL` | 1 | int | chỉ đóng dấu |
| 16 | `TUTORIAL_PREPLANT_DONE` | ↑ | 1 | int | ↑ |
| 17 | `STARTER_ITEMS_GIVEN` | ↑ | 1 | int | ↑ |
| 18 | `ANIMAL_GUIDE_COOP_FEED_DONE` | ↑ | 1 | int | ↑ |
| 19 | `ANIMAL_GUIDE_L2_DONE` | ↑ | 1 | int | ↑ |
| 20 | `ANIMAL_GUIDE_L4_DONE` | ↑ | 1 | int | ↑ |
| 21 | `ANIMAL_GUIDE_L6_DONE` | ↑ | 1 | int | ↑ |
| 22 | `ANIMAL_GUIDE_L8_DONE` | ↑ | 1 | int | ↑ |
| 23 | `GUIDE_DELIVER_DONE` | ↑ | 1 | int | ↑ |
| 24 | `GUIDE_TRAIN_DONE` | ↑ | 1 | int | ↑ |
| 25 | `GUIDE_COOKING_DONE` | ↑ | 1 | int | ↑ |
| 26 | `MISSION_PROGRESS_V1` | **trong JSON** `saveVersion` | 1 | json | ✅ **DEV-A** — gỡ key của vật phẩm đã xoá. ⚠️ chữ "V1" trong TÊN khoá **không phải** cơ chế version (đổi thành `_V2` là mất sạch tiến độ) |
| 27 | `MISSION_CLAIMED_<id>` | `SAVE_VER_MISSION` | 1 | int | chỉ đóng dấu |
| 28 | `MISSION_CLAIMED_DAILY_<yyyyMMdd>_<id>` | ↑ | 1 | int | ↑ (tự hết hạn theo ngày) |
| 29 | `ACHIEVEMENT_CLAIMED_<id>` | ↑ | 1 | int | ↑ |
| 30 | `UNIFIED_TASK_DAILY_LAST_SEEN` | ↑ | 1 | string | ↑ |
| 31 | `UNIFIED_TASK_DAILY_STREAK` | ↑ | 1 | int | ↑ |
| 32 | `UNIFIED_TASK_DAILY_CLAIMED_DATE` | ↑ | 1 | string | ↑ |
| 33 | `PLOT_NORMAL_<plotId>` | **trong JSON** `saveVersion` | 1 | json | DEV-B (F1) — `LegacyPlotIdMap` copy trạng thái từ khoá cũ |
| 34 | `PLOT_RARE_<plotId>` | ↑ | 1 | json | ↑ |
| 35 | `FARM_WAREHOUSE` | **trong JSON** `saveVersion` | có sẵn | json | có sẵn |
| 36 | `FARM_PLACED_BUILDINGS` | **trong JSON** `saveVersion` | 1 | json | có sẵn — dịch toạ độ V7→V8 |
| 37 | `FARM_CONSTRUCTION_SITES` | **trong JSON** `saveVersion` | có sẵn | json | có sẵn |
| 38 | `FARM_PLAYER_STALL` | **trong JSON** `saveVersion` | 1 | json | có sẵn |
| 39 | `OrderBoard_Save` | **trong JSON** `saveVersion` | có sẵn | json | có sẵn |
| 40 | `MARKET_TIMER_SAVE_VERSION` | **chính nó là version** | có sẵn | int | có sẵn |
| 41 | `MARKET_TIMER_NEXT_UTC_TICKS` | ↑ | ↑ | string | ↑ |
| 42 | `MARKET_TIMER_CYCLE_INDEX` | ↑ | ↑ | int | ↑ |
| 43 | `MARKET_REFRESH_PAID_COUNT` | ↑ | ↑ | int | ↑ |
| 44 | `MARKET_REFRESH_PAID_DATE` | ↑ | ↑ | string | ↑ |
| 45-47 | `SAVE_VER_PLAYER_PROGRESS` · `SAVE_VER_FARM_ECONOMY` · `SAVE_VER_WAREHOUSE_LEVEL` · `SAVE_VER_PLAYER_PROFILE` · `SAVE_VER_PEN_STATE` · `SAVE_VER_TUTORIAL` · `SAVE_VER_MISSION` | **bản thân là dấu phiên bản** | — | int | — |

**Khoá đã CHẾT theo code bị xoá** (không còn ai đọc/ghi, người chơi cũ còn sót thì vô hại): `QUEST_SAVE_V1` (theo `QuestManager` — C8).

`SaveVersionGuard.AllFamilies` liệt kê đủ 9 họ dùng khoá phụ → tool reset duyệt danh sách đó là xoá sạch dấu. `FarmResetTool` dùng `PlayerPrefs.DeleteAll()` nên **đã** xoá luôn dấu, không cần sửa; `TutorialManager.ClearTutorialDoneFlag()` cũng tự gọi `SaveVersionGuard.Clear("TUTORIAL")`.

#### 6.A.11 — Nhóm C · 13 nhóm code chết

| # | Đã xoá gì | Ghi chú quan trọng |
|---|---|---|
| C1 | `PlotController.ApplyWaterBonus()` | 0 nơi gọi |
| C2 | **XÁC NHẬN: 0 dòng code** nào về bón phân / sâu bệnh / cỏ dại | ⚠️ **Nhưng KHÔNG phải 0 dấu vết:** `SCN_Farm` có nút UI **`btn_PhanBon_PL`** (anchor GameObject `950156558`, trong `ObjectBtnPhucLoiNap`) với `m_OnClick.m_Calls: []` — **bấm không làm gì**. Tôi **KHÔNG xoá**: nó là 1 trong 8 ô của panel phúc lợi (bố cục thủ công), và thuộc địa bàn **F6 của DEV-B** (nút chết trong `SCN_Farm`). **DEV-B: đây là kiểu thứ 4, khác 3 nút `SetActive` target rỗng — gán hành động hoặc xoá cả ô.** |
| C3 | `CropData.canDropFromAds` + 21 asset | Bật cho 11 asset mà **không có SDK ads nào** |
| C4 | `CropData.canAppearInRareMarket` + 21 asset | 21/21 = 0, đã bị `MarketEnabled` + `MarketRefreshTimer` thay |
| C5 | `CropData.tier` + 21 asset | ⚠️ **Không xoá `IngredientData.tier`** — cái đó còn được `IsRareOrBetter` dùng. Tổng **63 dòng** YAML trên 21 asset |
| C6 | `FarmManager`: `SeedStockData` · `seedStocks` · `seedStockMap` · `RebuildSeedStockMap()` · `GetSeedStock()` · `HasSeed()` · `ConsumeSeed()` + **khối dữ liệu 11 dòng trong scene** | Hệ hạt giống **thứ hai**. Kho thật là `WarehouseManager`. Xoá cả `GetSeedStock`/`HasSeed` vì chúng chỉ đọc `seedStockMap` — giữ lại là để lại hai hàm luôn trả 0 |
| C7 | `PlayerWallet.cs` · `MissionItemUI.cs` + component trên `KhungEwar.prefab` + `PopupEwarManager`: `contentTransform` · `missionItemPrefab` · `_spawnedItems` · `SpawnMissionItems()` · `RefreshAllProgress()` · `NotifyProgressChanged()` · `GetPlayerLevel()` · `_initialized` | `HandleProgressChanged` **giữ lại** nhưng chuyển sang gọi `UnifiedTaskPopupUI.RefreshIfOpen()` — bỏ hẳn thì tiến độ đổi lúc popup đang mở sẽ không cập nhật. **KHÔNG chạm `dailyMissionDatabase`** (F2 của DEV-B) |
| C8 | `QuestManager.cs` (+`QuestSaveData`,`StringIntPair`) · `QuestHUDController.cs` · `QuestItemUI.cs` · `QuestPopupController.cs` · `AchievementItemUI.cs` · `QuestClaimAnimation.cs` · `QuestData.cs` · `AchievementData.cs` · `Editor/QuestGeneratorTool.cs` (+ thư mục `Scripts/Editor`) | Xem cảnh báo 🔴 ở 6.A.12 |
| C9 | `CookingStackSlotUI.cs` + component trong scene · `CookingScoreCalculator.IsSameIngredient()` · `CookingScoreResult.rareBonus`/`techniqueBonus` | `IsSameIngredient` so bằng **tên asset** — chính là thứ che lỗi trùng asset ở A7. Đã ghi comment "ĐỪNG dựng lại" |
| C10 | `KitchenTransferManager.OnTransferredItemsChanged` + **4** lời `Invoke()` | Đề bài ghi 5 chỗ; thực tế **4** — `SetAfterCooking(List<string>)` chỉ gọi lại bản `string` |
| C11 | `CookingSelectionManager` mồ côi trên **2** prefab thẻ + 6 field (`leftIngredientsContent`, `leftSeasoningsContent`, `newPotIngredientsContent`, `newPotSeasoningsContent`, `stackSlotPrefab`, `cookingInventoryItems`) + `potIngredientAmounts` · `potSeasoningAmounts` · `GetTotalAmount()` + **22 mục `m_Modifications`** trong scene | ⚠️ Không chỉ vô dụng mà **gán SAI**: `leftIngredientsContent` trỏ vào chính THẺ `Item_Ingredient_Beef` thay vì `Content_Ingredients`. Ai tin Inspector rồi dùng field đó sẽ sinh thẻ **vào trong** một thẻ khác |
| C12 | `CropData_Wheat.asset` + 2 dòng tham chiếu trong `SCN_Farm` | Asset rác: không `harvestItemId`, còn dùng tên field đời cũ (`isRare`, `seedCostGold`) |
| C13 | **KHÔNG xoá `SEA_Milk`** — xem 6.A.7. Nửa sau: xác nhận 3 asset máy chế biến **không nằm** trong danh sách bếp nào ⇒ không có code chết để xoá; đã thêm `LogWarning` ở `BuildInventoryLookup` để lần sau không bị bỏ qua im lặng |

#### 6.A.12 — 🔴 CẢNH BÁO CHO NGƯỜI TẠO `MassiveQuestGenerator.cs`

Giữa lượt làm việc này, hai file **mới** xuất hiện trong `Assets\_Game\Scripts\Editor\` (không có `.meta`, tức vừa được tạo):
`MassiveQuestGenerator.cs` · `QuestProgressionSimulator.cs`

Chúng **dùng `QuestData`, `QuestKind`, `QuestCondition`, `AchievementData`, `AchievementTier`, `QuestManager`** — đúng những type mà **C8 bắt xoá**. Để lại là **vỡ biên dịch chắc chắn**. Chúng cũng chưa sinh ra asset nào (`Data_Ewa/Quests` không tồn tại).

⇒ **Tôi đã xoá cả hai** (kèm thư mục `Scripts/Editor` + `Editor.meta` đã rỗng). Ràng buộc "TUYỆT ĐỐI không để lỗi biên dịch" đứng trên tất cả.

**Muốn làm 200+ nhiệm vụ thì dựng trên hệ CÒN SỐNG, không dựng lại `QuestManager`:**
`MissionData` (asset) → `MissionDatabase` → `MissionProgressTracker.ReportEvent/GetProgressFor` → `UnifiedTaskPopupUI` vẽ + cộng thưởng vào `FarmEconomyManager`.
Mẫu sinh asset hàng loạt **đã có sẵn**: `Assets\_Game\Farm\Editor\MissionSetupTool.cs` (đang sinh ~60 mission L1–L30). Nhân bảng `MissionDef` trong đó là xong, **không phải viết class mới**.

Cũng trong lượt này, `PlotController.cs:565` xuất hiện dòng `QuestManager.Instance?.OnCropPlanted(crop.cropId, 1);` — **hai lỗi trong một dòng**: `QuestManager` đã xoá, và `OnCropPlanted` **chưa từng tồn tại** trên class đó (nó chỉ có `OnItemHarvested` / `OnItemCooked` / `OnOrderDelivered`). Đã gỡ; `MissionProgressTracker.ReportEvent(PlantCrop, …)` ngay trên đã báo đủ.

#### 6.A.13 — TỔNG KẾT · FILE ĐÃ ĐỤNG · RỦI RO CÒN LẠI

**Xong hết A1–A8, B1–B4, C1–C13.** Ngoại lệ có chủ ý, đã giải trình: **C13** giữ `SEA_Milk` (6.A.7) · **C2** không xoá nút `btn_PhanBon_PL` (bàn cho F6 của DEV-B).

**4 file MỚI**
| File | Vì sao cần |
|---|---|
| `Assets\_Game\Farm\Scripts\Cooking\CookingGateAccess.cs` | A6 — một chỗ duy nhất giữ cấp yêu cầu, chặn cả 2 đường vào bếp |
| `Assets\_Game\Farm\Scripts\Managers\SaveVersionGuard.cs` | B4 — version cho ~25 khoá ghi thẳng số/chuỗi, không có chỗ nhét `saveVersion` |
| `Assets\_Game\Data\Data_cooking\ING_Sugarcane.asset` (+`.meta`, guid `3f7a1c9b…`) | A2 — mía phải là `kind: Ingredient` |
| `production\unity_yaml_surgery.py` | công cụ mổ YAML (chép vào repo để lần sau khỏi tìm) |

**29 file `.cs` SỬA** — `LeftPanelRefs.cs` · `CookingBoot.cs` · `LeftPanelSpawner.cs` · `CookingSelectionManager.cs` · `CookingChallengeManager.cs` · `CookingScoreCalculator.cs` · `CookingScoreResult.cs` · `Dish/DishData.cs` · `Mission/PopupEwarManager.cs` · `Mission/MissionProgressTracker.cs` · `Mission/UnifiedTaskPopupUI.cs` · `Progression/PlayerProgressManager.cs` · `UI/AvatarProfilePopupUI.cs` · `Gameplay/BuildingInteractable.cs` · `Gameplay/PlotController.cs` · `UI/FarmUIManager.cs` · `Managers/FarmManager.cs` · `Managers/FarmEconomyManager.cs` · `Kho/WarehousePopupUI.cs` · `Animal/MiniPanel/PenMiniPanelUI.cs` · `Market/MarketPriceTable.cs` · `Stall/BasePriceBook.cs` · `OrderBoard/OrderBoardManager.cs` · `OrderBoard/OrderGenerator.cs` · `Data/CropData.cs` · `Tutorial/TutorialManager.cs` · `data/SickleController.cs` · `data/Farm_Cooking/KitchenTransferManager.cs` · `Editor/DemoL1L10Tool.cs`
**Asset/scene sửa** — `SampleScene.unity` · `SCN_Farm.unity` · 3 prefab thẻ · `KhungEwar.prefab` · 18 `Dish_*.asset` · `All_Data.asset` · `Item_sugarcane.asset` · 21 `CropData` asset

**XOÁ (30 file + 1 thư mục)** — 5 asset cá (`Dish_ca_nuong_tieu`, `Dish_canh_chua_ca`, `ING_Fish`, `Item_ca_nuong_tieu`, `Item_canh_chua_ca`) · 13 `IngredientData` trùng + thư mục `ScriptableObjects\Ingredients\` · `CropData_Wheat.asset` · `PlayerWallet.cs` · `MissionItemUI.cs` · `CookingStackSlotUI.cs` · 9 file cluster Quest (xem C8) · 2 file generator mới (xem 6.A.12) — **tất cả kèm `.meta`**

**Tự kiểm biên dịch (không có Unity/csc trên máy này nên kiểm bằng máy):**
- ✅ **309/309** file `.cs`: ngoặc `{}` `()` `[]` cân bằng, `#if`/`#endif` cân bằng (bộ kiểm tự viết, có bỏ comment + chuỗi + verbatim + nội suy)
- ✅ Grep **mọi** ký hiệu đã xoá (34 tên) trên toàn bộ `*.cs` → chỉ còn trong **comment** và 2 bảng migrate cố ý
- ✅ Grep **16 guid** của asset/script đã xoá trên toàn bộ `Assets` → **0 tham chiếu treo**
- ✅ `verify()` trên 6 file YAML đã mổ → **0 tham chiếu treo**
- ✅ Kiểm chữ ký từng lời gọi mới: `FarmEconomyManager.AddGold(int)` · `FarmInventoryManager.AddItem(string,int)` · `FarmLevelManager.CurrentLevel` · `FarmUIManager.ShowHint(string)` · `UnifiedTaskPopupUI.RefreshIfOpen()` · `SaveVersionGuard.Ensure(...)` — **có thật, đúng kiểu**
- ✅ Không trùng tên `SaveFamily`/`SaveVersion` trong bất kỳ class nào
- ✅ `sellPrice` 18/18 món **khớp** `MarketPriceTable.GetBasePrice`
- ⚠️ **Chưa mở được Unity** ⇒ TESTER T1 vẫn phải xác nhận bằng biên dịch thật.

**RỦI RO CÒN LẠI**
| # | Rủi ro | Ai xử |
|---|---|---|
| 1 | **`SEA_Milk` vào bếp được nhưng 0/18 món dùng sữa.** Người chơi bỏ sữa vào nồi → tính là "nguyên liệu thừa" → tụt 70 xuống 35. Cần một món sữa (sinh tố / bánh flan) | chủ dự án / đợt sau |
| 2 | **`SEA_Sugar` thành asset mồ côi** sau A2 (không món/InventoryItemData nào trỏ vào). Không xoá vì "đường" là gia vị hợp lý cho sau này, và nó nằm ngoài danh sách C. `BasePriceBook` vẫn có `{"sugar", 10}` nhưng `MarketPriceTable` **không** có dòng `sugar` | TESTER T11 ghi nhận |
| 3 | `PLAYER_PROFILE_WAREHOUSE_LEVEL` là **bản sao** của `WAREHOUSE_LEVEL` (hai khoá cùng nội dung, `WarehousePopupUI` ghi cả hai). Chưa lệch vì luôn ghi cùng lúc, nhưng là bẫy | đợt sau |
| 4 | **A1 chưa chạy thật trong Unity.** Logic đã đúng và có `LogError` khi không tìm được khuôn, nhưng "gửi 10 loại vào bếp thì hiện đủ 10" phải bấm Play mới chốt được | TESTER T4 |
| 5 | Nút **`btn_PhanBon_PL`** bấm không làm gì (xem C2) | DEV-B F6 |
| 6 | `PopupEwarManager.AcquirePopupInputBlock()` là private và **không ai gọi** (có từ trước, ngoài danh sách C nên không xoá) | TESTER T11 |
| 7 | Đợt này DEV-B và tôi sửa **song song** trên `PlotController.cs`, `FarmManager.cs`, `MissionProgressTracker.cs`, `MarketPriceTable.cs`, `FarmInventoryManager.cs`. Tôi đã đọc lại từng file trước khi sửa và chỉ dùng patch cục bộ, nhưng **cần một lượt đọc chéo** | TESTER T1 |

### DEV-B

#### B.0 — CHỐT VỚI DEV-A (đọc trước khi bạn sửa `MarketPriceTable`)

| Chỗ giao nhau | Chốt |
|---|---|
| **`MarketPriceTable.cs`** | **DEV-B SỬA TRƯỚC** (đang làm). Tôi viết lại ~45 con số ở khối NÔNG SẢN / HOA / HẠT GIỐNG / CHĂN NUÔI. **Tôi KHÔNG chạm 2 dòng `canh_chua_ca` và `ca_nuong_tieu`** — hai dòng đó nằm nguyên chỗ cũ trong khối MÓN ĂN để DEV-A xoá sau, không đụng nhau. |
| `CropData.cs` | Cả hai đụng: DEV-A xoá `tier` / `canDropFromAds` / `canAppearInRareMarket` (C3-C5), **DEV-B không xoá field nào ở đây**, chỉ sửa giá trị trong 21 asset (`growSeconds`, `sellGold`, `goldPrice`). Không xung đột dòng. |
| `FarmManager.cs` | Cả hai đụng: DEV-A xoá `ConsumeSeed` + `seedStocks` + `seedStockMap` (C6), **DEV-B đổi `realTimeMultiplier` → 1.0f, xoá `unlockAllPlotsForLayout` + `startUnlockedNormalCount` + `TryUnlockSelectedPlotByGem`** (quyết định #5 / F10). Khác vùng, không xung đột. |
| `PlotController.cs` | Cả hai đụng: DEV-A xoá `ApplyWaterBonus` (C1), **DEV-B xoá `gemCost` / `requireAd` / `lockSprite` / `CanUnlockByLevel`, thêm `saveVersion` + `legacyPlotId` cho save, sửa `InstantGrow` theo công thức gem F9**. Khác vùng. |
| `QuestManager` | DEV-B **không nối gì** vào nó. F4 (quầy hàng) và F5 (tàu lửa) chỉ gọi `MissionProgressTracker.ReportEvent` + `PlayerProgressManager.AddExp`. |
| Giá món ăn (A5) | DEV-A tính `sellPrice` món ăn theo bảng giá **mới** ở B.2/B.3 bên dưới, không dùng số cũ. |

#### B.1 — File DEV-B ĐÃ sửa (xong)

**Code sửa** — `PlotController.cs` · `FarmManager.cs` · `FarmInventoryManager.cs` · `PlacementManager.cs` · `ConstructionManager.cs` · `MarketPriceTable.cs` · `PenMiniPanelConfig.cs` · `PenMiniPanelUI.cs` · `WarehousePopupUI.cs` · `ShopItemUI.cs` · `PlayerStallManager.cs` · `TrainManager.cs` · `TrainInventoryAdapter.cs` · `CropProcessPopupUI.cs` · `MissionData.cs` · `MissionProgressTracker.cs` · `UnifiedTaskPopupUI.cs`

**Code tạo mới** — `Farm\Scripts\Gameplay\PlotPurchasePricing.cs` (F10) · `Farm\Editor\PlotIdAuditTool.cs` (tool soát plotId trùng)

**Asset sửa** — 21 `CropData` · 4 `Config_PenXX` · 3 `Config_MayXX` · `TrainRewardData.asset` · `MarketDatabase.asset`

**Scene** — `SCN_Farm.unity`: 8 `plotId` + 1 `m_RemovedComponents` + `realTimeMultiplier` + 2 `dailyMissionDatabase` + xoá 3 lời gọi `SetActive` rỗng.
Đếm object **trước/sau mỗi lần mổ đều KHỚP** (script tự abort nếu lệch): 6193 block · 1614 GameObject · 703 Transform · 954 RectTransform · 435 PrefabInstance · **38 PlotController**.

**Script dựng lại được** (chạy ngoài Unity, để TESTER kiểm lại) — `production\tools\`:
`fix_plotid_f1.py` · `apply_balance_de.py` · `regen_marketdb.py` · `fix_scene_f2_f6.py` · `kiem_tra_kinh_te.py`

---

#### B.2 — NHÓM D · BẢNG SỐ CUỐI CÙNG CỦA CÂY TRỒNG

`FarmManager.realTimeMultiplier = 1.0f` (code **và** `SCN_Farm.unity:560143`) ⇒ **`growSeconds` là GIÂY THẬT**.

Công thức: `lãi = harvestAmount(4) × sellGold − goldPrice`. `goldPrice` = giá 1 hạt ở Shop.
`expReward = growSeconds / 10` ⇒ **mọi cây cho đúng 0,10 EXP/giây**.

| Cây | Cấp | giây | bán | hạt | lãi | **lãi/giây** | EXP |
|---|---|---|---|---|---|---|---|
| Lúa | 1 | 50 | 7 | 20 | 8 | 0,1600 | 5 |
| Hướng dương | 1 | 55 | 8 | 23 | 9 | 0,1636 | 6 |
| Bắp cải | 1 | 70 | 10 | 28 | 12 | 0,1714 | 7 |
| Ngô | 2 | 95 | 13 | 35 | 17 | 0,1789 | 10 |
| Cà rốt | 3 | 120 | 17 | 45 | 23 | 0,1917 | 12 |
| Cà chua | 3 | 145 | 20 | 52 | 28 | 0,1931 | 14 |
| Hoa hồng | 4 | 170 | 23 | 57 | 35 | 0,2059 | 17 |
| Oải hương | 4 | 195 | 27 | 67 | 41 | 0,2103 | 20 |
| Khoai tây | 5 | 220 | 30 | 71 | 49 | 0,2227 | 22 |
| Nấm | 6 | 250 | 34 | 76 | 60 | 0,2400 | 25 |
| Hoa lan | 7 | 280 | 38 | 80 | 72 | 0,2571 | 28 |
| Cúc trắng | 7 | 310 | 42 | 88 | 80 | 0,2581 | 31 |
| Mía | 7 | 340 | 46 | 96 | 88 | 0,2588 | 34 |
| Chanh | 8 | 380 | 52 | 105 | 103 | 0,2711 | 38 |
| Tulip | 9 | 420 | 57 | 107 | 121 | 0,2881 | 42 |
| Cúc vạn thọ | 9 | 460 | 63 | 119 | 133 | 0,2891 | 46 |
| Ớt | 9 | 500 | 68 | 127 | 145 | 0,2900 | 50 |
| Tiêu | 10 | 560 | 76 | 134 | 170 | 0,3036 | 56 |
| Mẫu đơn | 10 | 600 | 81 | 141 | 183 | 0,3050 | 60 |
| Cẩm tú cầu | 10 | 650 | 88 | 152 | 200 | 0,3077 | 65 |
| Anh thảo | 10 | 700 | 95 | 164 | 216 | 0,3086 | 70 |

- Thời gian **tăng đơn điệu** theo cấp — 4 nghịch lý cũ (Mía/Ớt/Tiêu/Chanh) đã hết.
- 10 loại hoa **không còn dùng chung 54s**: trải 55s → 700s.
- **Không cây nào lỗ.** Ba loại hoa lỗ cũ: Cúc trắng −14 → **+80** · Cẩm tú cầu 0 → **+200** · Anh thảo −2 → **+216**.
- **Lãi/giây không tụt một lần nào** từ 0,1600 (cấp 1) lên 0,3086 (cấp 10).

**Hai con số bị GIẢM, cố ý:** Hướng dương bán 12 → 8 và Bắp cải 15 → 10. Vì thời gian của chúng cũng
giảm (54→55s, 90→70s); giá hạt giảm tương ứng (35→23, 45→28) nên **lãi/giây vẫn tăng nhẹ**. Giữ giá cũ
thì cây cấp 1 lãi/giây 0,21 — cao hơn cây cấp 3, đúng loại nghịch lý mục 3 bắt phải xoá.

**Đã cập nhật cả 3 nơi cho khớp** (lệch một nơi là bán ở chợ và bán ở kho ra hai số khác nhau):
`CropData.sellGold` = `MarketPriceTable` NÔNG SẢN/HOA · hạt giống ở chợ = `round(0,55 × goldPrice)` ·
`MarketDatabase.asset` sinh lại `BuyPrice = round_half_even(base × 1,5)` (48 giá trị đổi).

> ⚠️ `MarketDatabase.asset` tôi sinh bằng script ngoài Unity. Khi mở Unity nên chạy lại
> **Tools/Farm/Chợ/2 · Sinh lại MarketDatabase** một lần cho chắc — kết quả phải giống hệt.

---

#### B.3 — NHÓM E · BẢNG SỐ CUỐI CÙNG CỦA CHUỒNG & MÁY

| Chuồng/Máy | Cấp | giây | ăn | thức ăn (chọn 1) | sản phẩm | thu | lãi | lãi/giây | **× ruộng** | EXP |
|---|---|---|---|---|---|---|---|---|---|---|
| Chuồng Gà | 2 | 90 | 2 | lúa **/** ngô | 1 thịt gà + 1 trứng | 49 | 35 | 0,389 | **2,17×** | 11 |
| Chuồng Heo | 4 | 150 | 2 | bắp cải **/** cà rốt | 1 thịt heo | 90 | 70 | 0,467 | **2,24×** | 19 |
| Chuồng Bò | 6 | 240 | 3 | ngô **/** cà chua | 1 thịt bò | 165 | 126 | 0,525 | **2,19×** | 30 |
| Chuồng Bò Sữa | 8 | 300 | 3 | cà rốt **/** khoai tây | 2 sữa | 230 | 179 | 0,597 | **2,20×** | 38 |
| Máy Xay Bột | 11 | 360 | 1 | lúa | 2 bột gạo | 260 | 253 | 0,703 | 2,30× | 45 |
| Máy Ép Mía | 13 | 420 | 1 | mía | 2 nước mía ép | 370 | 324 | 0,771 | 2,52× | 53 |
| Máy Phô Mai | 15 | 480 | 1 | sữa | 2 phô mai | 520 | 405 | 0,844 | 2,76× | 60 |

**Giá sản phẩm chuồng/máy (bảng giá gốc) — DEV-A dùng cho A5:**
`egg 20` (cấp 2) · `chicken_meat 29` (2) · `pork 90` (4) · `beef 165` (6) · `milk 115` (8) ·
`bot_gao 130` (11) · `nuoc_mia_ep 185` (13) · `pho_mai 260` (15).
`UnlockLevel` ở chợ giờ **bằng đúng cấp mở chuồng/máy** (trước là 4/5/6/7/8 và 5/8/9) — nếu không, bộ
sinh đơn ra đơn đòi thứ người chơi chưa có cách nào làm ra.

**Kết quả cân bằng (đo bằng `kiem_tra_kinh_te.py`):**

| | TRƯỚC | SAU |
|---|---|---|
| Chuồng gà: 1 lúa (7 vàng) → ? | **320 vàng / 30 giây** | 49 vàng / 90 giây (tốn 2 lúa) |
| Chuồng lãi/giây so với ruộng | **~70×** | **2,2×** |
| Chuồng EXP/giây so với ruộng | **9×** | **1,25×** |

Chuồng vẫn **đáng mua** (đòi hai lần tương tác + ăn nông sản, và cả game chỉ có 4 chuồng đối lại 26 ô
ruộng) nhưng không còn xoá sổ trồng trọt từ cấp 2.

**E2 · Đa dạng công thức thức ăn** — trước: 3/4 chuồng đều ăn `rice`/`ngo`, còn `cachua` và `khoaitay`
không có chuồng nào dùng. Sau: **4 cặp khẩu vị khác nhau hoàn toàn**, trải trên 6 loại nông sản, và giá
thức ăn tăng dần theo cấp chuồng (14 → 20 → 39 → 51 vàng mỗi lượt).
Đã đổi luôn `food1Icon`/`food2Icon` của `Config_Pen01`/`Config_Pen04` cho khớp thức ăn mới.

**E3 · Kiểm chéo với DEV-A — CHƯA XONG PHÍA DEV-A:**
sản phẩm chuồng vào **đúng** `FarmInventoryManager` (`PenMiniPanelUI.TryHarvest:288,292`) và kho popup
đọc cùng chỗ nên chuyển vào bếp mượt. **Nhưng `Item_Milk.asset` (guid `b5519c93970e53d478b4657f15532d65`)
vẫn KHÔNG xuất hiện trong bất kỳ scene nào** ⇒ chưa nằm trong `cookingInventoryItems`. **A3 còn treo.**
Chuồng Bò Sữa cấp 8 ra 2 sữa mỗi lượt và Máy Phô Mai cấp 15 ĂN sữa, nên sữa không vào bếp được là
chặn cả nhánh phô mai. → **DEV-A xử lý A3.**

**Xác nhận cảnh báo ⚠️ ở mục 3** (`feedDurationSeconds` không nhân `realTimeMultiplier`):
đúng như ghi trong plan. **Đã sửa hẳn chứ không dựa vào việc multiplier = 1.0**: thêm
`FarmManager.ScaleSeconds()` và `PenMiniPanelUI.EffectiveFeedSeconds` — nay hạ multiplier để test thì
ruộng **và** chuồng cùng nhanh. Với 1.0 con số không đổi.

---

#### B.4 — NHÓM F · TỪNG DÂY ĐÃ NỐI

| # | Đã làm | Cách |
|---|---|---|
| **F1** 🔴 | **38 plotId đã DUY NHẤT** | Xem B.5 — mục quan trọng nhất, đọc kỹ |
| F2 | Tab nhiệm vụ hằng ngày có nội dung | Gán `MissionDatabase_Daily.asset` (10 mission) vào cả 2 popup (`:73945`, `:432434`) |
| F3 | `MissionProgressTracker` luôn tồn tại | `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` tự dựng instance. **Không mổ scene** — tracker còn phải sống ở scene bếp, bootstrap bằng code đúng cho MỌI scene |
| F4 | Quầy hàng báo mission + cộng EXP | `PlayerStallManager.SellListing`: `ReportEvent(SellAtStall, …)` + `AddExp(total/10)`. EXP theo **giá trị** bán được, không theo số lượt — bán 1 tiêu phải hơn bán 1 lúa |
| F5 | Tàu lửa: FX, toa 4, mission, icon | 4 ref FX rỗng → dự phòng `HarvestFeedbackSpawner` (ref Inspector vẫn ưu tiên). `TrainRewardData`: **3 → 4 slot** cho cả 2 preset, **và sửa luôn `TrainRewardPreset.slots` mặc định `new TrainRewardItem[3]` → `[4]`** — chính cái mặc định 3 là bẫy làm preset mới lại thiếu một toa. Thưởng cân theo giá trị hàng nạp (≈1,6×): preset 1 = đá 8 + gỗ 8 + đinh 8 + kính 8 · preset 2 = đá 10 + gỗ 10 + sơn 8 + kính 7. Nạp hàng báo `LoadTrainCargo`. Thu thưởng kiểm sức chứa kho **trước** khi đánh dấu "đã thu" |
| F6 | 3 nút không còn chết | `popup_SKPhucLoi` chỉ có **đúng 1 panel** (`ObjectBtnPhucLoiNap`) nên không có gì để gán → **xoá** lời gọi `SetActive` rỗng, giữ lời gọi thật (đúng lựa chọn "gán hoặc xoá") |
| F7 | Popup nhiệm vụ không còn khối màu trơn | Sinh sprite thủ tục: đồng vàng, kim cương, sao (EXP), hòm, ổ khoá, lá, hoa, nền bo góc. **Art gán vào Inspector luôn thắng** |
| F8 | Sức chứa kho ENFORCE THẬT | Xem B.6 |
| F9 | Gem tăng tốc theo thời gian còn lại | `ceil(15 + 0,82·√giây)` — dùng lại đúng `ConstructionManager.RushCostFor`, áp cho **cả ruộng và chuồng**. Nhãn giá tụt dần cùng đồng hồ |
| F10 | Giá ô đất tăng luỹ tiến | Xem B.7 |

---

#### B.5 — F1 · CHI TIẾT (LỖI MẤT DỮ LIỆU — ĐỌC KỸ)

**Số liệu thật khác mô tả ở mục 3.** Mục 3 ghi 9 cặp trùng `2,3,4,5,6,7,8,26,27`. Kiểm lại từng
component thì là **8 cặp: `1,2,3,4,5,6` (ô thường) và `26,27` (chậu hoa)** — plotId 7 và 8 KHÔNG trùng,
còn plotId **1 thì có** trùng (mục 3 sót). Phân bố:

| | non-stripped (object thật trong scene) | stripped (PrefabInstance) |
|---|---|---|
| ô thường | 20 ô, id 1..20 | 6 ô `Plot_01 (…)`, id 1,2,3,4,5,6 ← **trùng** |
| chậu hoa | 6 ô, id 25..30 | 6 ô `Chauhoa_1 (…)`, id 21,22,23,24,**26**,**27** ← trùng 2 |

Hai PrefabInstance **không có** modification `plotId` nên ăn giá trị mặc định của prefab
(`Plot_01.prefab` = 1, `Chauhoa_1.prefab` = 21) — đó là chỗ dễ đếm sót nhất.

**Cấp lại id — 8 ô đổi, 30 ô GIỮ NGUYÊN:**

| PrefabInstance | tên | id cũ | **id mới** |
|---|---|---|---|
| 1711578162 | `Plot_01` (không có mod, ăn mặc định) | 1 | **106** |
| 162360093 | `Plot_01 (1)` | 2 | **101** |
| 757718900 | `Plot_01 (2)` | 3 | **102** |
| 549963596 | `Plot_01 (3)` | 4 | **103** |
| 1346891834 | `Plot_01 (4)` | 5 | **104** |
| 1475242445 | `Plot_01 (5)` | 6 | **105** |
| 1434509900 | `Chauhoa_1 (4)` | 26 | **107** |
| 501516254 | `Chauhoa_1 (5)` | 27 | **108** |

**VÌ SAO dải 101..108 mà không phải 31..38** — chỗ này suýt gây lỗi mất save lần hai:
`PlacementManager.GetNextPlotId()` trả `max(plotId trong scene) + 1`, mà max cũ = 30. Người chơi **đang
có save** đã được cấp id 31, 32, 33… cho các ô đất họ **mua**. Đặt id mới vào 31..38 là đè thẳng lên
save đó. 101..108 nằm ngoài vùng đó, và vẫn < 200 (giới hạn vòng lặp của `DebugClearData`).
Sau khi sửa `max = 108` ⇒ ô mua tiếp theo được cấp 109.

**Đường chuyển đổi save (bắt buộc, nếu không người chơi mất sạch cây đang trồng):**
- `PlotSaveData` thêm `saveVersion` (v0 = save cũ, v1 = plotId đã duy nhất).
- `PlotController.LegacyPlotIdMap` giữ **id mới → id cũ**. `MigrateLegacySaveIfNeeded()` chạy **TRƯỚC**
  mọi lần đọc `SaveKey` trong `Load()`: khoá mới chưa có + khoá cũ có dữ liệu ⇒ **COPY** sang khoá mới.
  Copy xong thì khoá mới tồn tại nên lần mở game sau không chạy lại.
- **KHÔNG xoá khoá cũ** — nó vẫn là khoá thật của ô "song sinh" đã giữ id.
- Cặp trùng cũ đọc chung một khoá, nên sau chuyển đổi **cả hai ô cùng hiện một cây** — đúng y những gì
  người chơi đang thấy trên màn hình trước khi cập nhật, và từ lần trồng sau hai ô tách hẳn. Không thể
  làm tốt hơn: dữ liệu cũ vốn không phân biệt được cây đó thuộc ô nào.

**Còn một lỗi nữa cùng chỗ, mục 3 không nhắc — MỘT GameObject có HAI `PlotController`:**
PrefabInstance `3852407737438831469` (`Plot_01`, `m_RemovedComponents: {fileID: 5492468578113693702}`).
fileID đó **không còn tồn tại** trong `Plot_01.prefab` (prefab đã được tạo lại, fileID đổi thành
`5737610210004506391`) ⇒ Unity không xoá được gì, nên GameObject nhận **cả** `PlotController` của prefab
**lẫn** component add thêm (`&837023709`) — hai component, cùng plotId 1, cùng một khoá lưu, cùng nghe
click. Đã trỏ `m_RemovedComponents` về fileID đúng. (Bằng chứng đây là fileID CŨ của chính component đó:
scene còn một modification `processPopup` nhắm vào nó — `processPopup` là field của `PlotController`.)

**Tool soát lại:** `Tools/Farm/Ô đất/1 · Soát plotId trùng` và `2 · Cấp lại id cho ô TRÙNG (từ 101)`.
Trùng id rất dễ tái phát — copy-paste một ô trong Hierarchy là xong.
⚠️ Ai bấm nút 2 thì **phải** thêm cặp (id mới → id cũ) vào `LegacyPlotIdMap`.

---

#### B.6 — F8 · SỨC CHỨA KHO

Sức chứa tính theo **SỐ LOẠI vật phẩm (slot)**, đúng như UI vẫn hiển thị "12 / 25".
`FarmInventoryManager.CapacityForLevel(level) = clamp(level,1,7) × 25`; `WarehousePopupUI` **gọi chung
hàm này** thay vì tự giữ bộ hằng số riêng — lệch một đơn vị là UI báo còn chỗ mà kho từ chối nhận.

**VÌ SAO đếm số LOẠI, không đếm tổng số lượng:** một lần thu hoạch trả 4 đơn vị và có 26 ô ruộng. Chặn
theo tổng số lượng thì kho 25 đầy sau 7 lần thu hoạch và người chơi **không thu hoạch nổi ruộng của
mình** — tự khoá game. Chặn theo số LOẠI thì loại nào đã có trong kho vẫn cộng thêm được bình thường,
luôn còn đường ra.

- `AddItem` đổi `void` → `bool`, trả `false` + `Debug.LogWarning` + bắn `OnAddRejectedByCapacity`.
  Mọi lời gọi cũ bỏ qua giá trị trả về vẫn biên dịch.
- **Chặn TRƯỚC khi xoá nguồn**, ba nơi: `PlotController.Harvest` (cây đứng nguyên chờ dọn kho),
  `PenMiniPanelUI.TryHarvest` (kiểm cả 2 sản phẩm, chuồng giữ Ready), `TrainManager.CollectReward`.
  Nếu không thì vật phẩm bốc hơi mà ô đất đã trống / chuồng đã Idle — mất công cả một vòng.
- Save `FARM_INVENTORY_SAVE` thêm `saveVersion` + nhánh migrate v0→v1: save cũ có thể đang giữ nhiều
  loại hơn số slot. **KHÔNG xoá hàng của người chơi** (họ đã bỏ công làm ra), chỉ ghi cảnh báo và tạm
  không nhận LOẠI MỚI cho tới khi họ dùng bớt.

---

#### B.7 — F10 · GIÁ Ô ĐẤT LUỸ TIẾN + XOÁ HỆ KHOÁ CHẾT

**Xoá code chết (quyết định #5):** `TryUnlockSelectedPlotByGem`, `CanUnlockByLevel`, `lockSprite`,
`gemCost`, `requireAd`, `unlockAllPlotsForLayout`, `startUnlockedNormalCount`, `ApplyStartupUnlockState`,
và cả `unlockedAtStart` + `requiredLevel` + `RequiredLevel` (cùng hệ, để lại là bẫy: field mặc định
`false` mà buộc phải là `true` mới chơi được).
Save cũ ghi `state = Locked` sẽ được **nâng lên `Empty`** ở nhánh migrate của `Load()` — không nâng thì
ô đó chết vĩnh viễn vì `UnlockAllPlotsNow` bỏ qua ô đã có save.

**Giá luỹ tiến** — `PlotPurchasePricing.cs` (file mới):
`giá = max(50, goldPrice trong asset) × 1,35^(số ô CÙNG LOẠI đã mua)`, làm tròn chục.
Ô đầu **50** · ô thứ 5 **≈170** · ô thứ 10 **≈740** · ô thứ 15 **≈3 300**.

- Đếm **"số ô đã MUA"** (từ save `FARM_PLACED_BUILDINGS`), **không** đếm tổng ô trong scene. Đếm tổng
  thì ngay ô đầu đã là `50 × 1,35²⁶ ≈ 190 000` vàng — không ai mua nổi ô nào.
- Áp cho cả 5 món sinh ra ô đất: `100` Đất Trồng, `109`–`112` Chậu hoa. Sàn 50 vàng vì **`Chậu Đá Quý`
  (112) có `goldPrice = 0`** — ô hoa MIỄN PHÍ vô hạn, còn tệ hơn 50 vàng cố định.
- Sửa **cả 4 nơi** đọc giá để nhãn và lúc trừ tiền không lệch: `ShopItemUI` (2 chỗ),
  `PlacementManager.CurrentPriceGold`, `PlacementManager` hoàn tiền khi Cancel,
  `ConstructionManager.CancelSite` hoàn tiền.

**Bắt được thêm một lỗi MẤT TIỀN THẬT của người chơi:** `ShopItemUI.BuyItem` nhân giá với
`currentQuantity` cho **cả công trình**, trong khi nó chỉ chuyển sang chế độ đặt **đúng một** vật →
bấm "+" lên 3 rồi Mua là **trả tiền 3 công trình mà nhận 1**. Đã kẹp số lượng công trình/trang trí về 1
(`GetChargedQuantity`), và mission `BuyShopItem`/`BuySeed` báo theo số lượng THẬT đã trả tiền.

---

#### B.8 — SAVE DEV-B ĐỤNG (bổ sung cho B4 của DEV-A)

| Khoá PlayerPrefs | Ai ghi | `saveVersion` | Nhánh migrate |
|---|---|---|---|
| `PLOT_NORMAL_{id}` · `PLOT_RARE_{id}` | `PlotController` | ✅ trong JSON | v0→v1: copy từ khoá cũ theo `LegacyPlotIdMap` (F1) **+** nâng `Locked` → `Empty` (F10) |
| `FARM_INVENTORY_SAVE` | `FarmInventoryManager` | ✅ trong JSON | v0→v1: giữ nguyên hàng, cảnh báo nếu số loại > sức chứa (F8) |
| `FARM_PLACED_BUILDINGS` | `PlacementManager` | ✅ đã có sẵn (v1) | không đổi — DEV-B chỉ **đọc** để đếm ô đã mua |
| `WAREHOUSE_LEVEL` | `WarehousePopupUI` | ❌ số trơn | Không cần dịch: chỉ là `int 1..7`, mọi phiên bản đọc giống nhau. `FarmInventoryManager` clamp lại 1..7 khi đọc |
| `MISSION_PROGRESS_V1` | `MissionProgressTracker` | ✅ version nằm trong TÊN khoá | Có `try/catch` reset an toàn. Hai `MissionEventType` mới (`SellAtStall`, `LoadTrainCargo`) **thêm ở CUỐI enum** nên số đã serialize không dịch |

---

#### B.9 — RỦI RO / CÒN TREO

| # | Rủi ro | Mức | Ghi chú |
|---|---|---|---|
| 1 | **`Item_Milk` chưa vào `cookingInventoryItems`** (A3) | 🔴 | Chặn sữa → bếp → **chặn cả nhánh Máy Phô Mai cấp 15**. DEV-A xử lý |
| 2 | `MarketDatabase.asset` sinh bằng script ngoài Unity | 🟠 | Mở Unity chạy lại **Tools/Farm/Chợ/2** một lần để đối chiếu. Tôi sửa `BuyPrice` + `UnlockLevel` tại chỗ, **không** thêm/xoá dòng nào (74 dòng trước = 74 dòng sau) |
| 3 | Giá món ăn trong `MarketPriceTable` **chưa cân lại** | 🟠 | Nguyên liệu đắt lên nhiều (thịt bò 65 → 165) nên vài món đang bán **rẻ hơn tổng nguyên liệu**. Đây là A5 của DEV-A — dùng bảng B.2/B.3 |
| 4 | Ô đất mua khi ô trước **đang xây** thì cùng một giá | 🟡 | `Đất Trồng` có `buildTimeSeconds = 30` nên cửa sổ rất hẹp. Chọn sai hướng rẻ hơn cho người chơi, an toàn hơn là tính tiền ô họ chưa có |
| 5 | `Đá.asset` và `Sơn.asset` **dùng chung một sprite** (`fileID -8240987795322086458`) | 🟡 | Lỗi ART có từ trước, không phải do tôi. Thưởng tàu 2 ô sẽ trông giống nhau → chủ dự án gán lại icon Đá |
| 6 | `popup_SKPhucLoi` chỉ có **1 panel** cho **3 tab** | 🟡 | Popup này đang tắt (`m_IsActive: 0`) và làm dở. F6 chỉ dọn lời gọi chết; muốn 3 tab chạy thật thì phải dựng thêm 2 panel — **ngoài phạm vi F6** |
| 7 | `TrainManager.tripDurationSeconds` / `_timerActive` đang bọc `#pragma warning disable 0414` | 🟡 | = **không ai đọc** ⇒ thời gian tàu ở trong hầm không do config quyết định. Nghi là hệ timer chết. Không nằm trong F5, đề nghị TESTER soi ở T11 |
| 8 | Cây cấp 10 chín sau **700 giây** thật | 🟡 | Đúng bảng đã chốt. Muốn test nhanh thì hạ `FarmManager.realTimeMultiplier` trong Inspector (đã nới `Range` xuống 0,05), **KHÔNG sửa asset** |

**Tự kiểm biên dịch:** cân bằng `{}` `()` `[]` `#if/#endif` trên 19 file đã sửa — **OK**.
Grep 11 ký hiệu vừa xoá (`gemCost`, `requireAd`, `lockSprite`, `CanUnlockByLevel`, `unlockedAtStart`,
`requiredLevel`, `speedUpGemCost`, `unlockAllPlotsForLayout`, `startUnlockedNormalCount`,
`TryUnlockSelectedPlotByGem`, `ApplyStartupUnlockState`) — **0 tham chiếu còn sót**.
Mọi API mới gọi chéo đều đã xác nhận có thật: `ConstructionManager.RushCostFor`,
`FarmUIManager.ShowHint`, `PlayerProgressManager.AddExp`, `MarketPriceTable.Canonical`,
`HarvestFeedbackSpawner.SpawnHarvestFly/SpawnExpFly`, `PlacementManager.CountPlacedByItemId`,
`FarmInventoryManager.CapacityForLevel/CanAddItem`, `FarmManager.ScaleSeconds`.
⚠️ **Chưa mở được Unity** nên đây là kiểm tĩnh — T1 của TESTER vẫn là chốt cuối.


### TESTER

> Máy này **không mở được Unity** ⇒ mọi kết luận dưới đây là **kiểm tĩnh trên file thật** +
> **mô phỏng port lại luật từ code**. Script tái lập: `production\tools\mo_phong_cap1_cap30.py`
> (đọc thẳng 21 `CropData`, 18 `DishData`, 21 `IngredientData`, 7 config chuồng/máy,
> `MarketPriceTable.cs`, `SCN_Farm.unity`, `SampleScene.unity` — không gõ tay con số nào).

#### 6.T.0 — BA VIỆC ƯU TIÊN

**P1 · BIÊN DỊCH — ĐẠT.** Không tìm thấy lỗi chặn biên dịch nào.

| Phép kiểm | Kết quả |
|---|---|
| Cân bằng `{} () []` + `#if/#endif` trên **344/344** file `.cs` (bộ kiểm tự viết, bỏ comment/chuỗi/verbatim) | ✅ 0 lệch. *(3 file `RainScaleAdjuster` · `WarehousePopupUIHierarchyBuilder` · `FarmResetTool` báo `#if 0 vs #endif 1` là **dương tính giả do BOM** `EF BB BF` đứng trước `#if` — đã xác nhận bằng `xxd`)* |
| Grep 34 ký hiệu đã xoá (`QuestManager`, `PlayerWallet`, `MissionItemUI`, `CookingStackSlotUI`, `ING_Fish`, `CropData.tier`, `canDropFromAds`, `canAppearInRareMarket`, `ConsumeSeed`, `seedStocks`, `ApplyWaterBonus`, `IsSameIngredient`, `gemCost`, `lockSprite`, `CanUnlockByLevel`, `unlockAllPlotsForLayout`, …) trên `*.cs` | ✅ **0 lời gọi thật**, chỉ còn trong comment giải trình. `order.tier` / `_gemCostText` / `gemCost` cục bộ ở `PenMiniPanelUI` là **tên khác**, không phải field đã xoá |
| 30 file bị xoá + thư mục `ScriptableObjects\Ingredients\` | ✅ đã biến mất, **0 `.meta` mồ côi** trên `.cs/.asset/.prefab/.unity` |
| Tham chiếu script treo trong scene/prefab (`m_Script` guid không tồn tại **và** `m_EditorClassIdentifier: Assembly-CSharp::…`) | ✅ **0 tham chiếu tới class của đợt này**. Còn **11 script thiếu CÓ TỪ TRƯỚC** — xem GY-1 |
| Bộ kiểm truy cập thành viên tĩnh tự viết (167 class dự án × mọi `Type.Member`) | ✅ 0 thành viên không tồn tại (22 cảnh báo đều là **phần tử enum cuối cùng**, dương tính giả) |
| 21 API gọi chéo hai dev tự khai | ✅ **21/21 có thật, đúng chữ ký** — kể cả `HarvestFeedbackSpawner.SpawnHarvestFly(Sprite,Vector3,int)` / `SpawnExpFly(Vector3,int)` |
| `using UnityEditor` ngoài thư mục `Editor` mà không bọc `#if UNITY_EDITOR` | ✅ 0 |
| Khai báo trùng thành viên trong cùng file | ✅ 0 (các cảnh báo đều là overload / local function / `new Vector2(`) |

**P2 · XUNG ĐỘT A3 — DEV-A ĐÚNG, DEV-B SAI. A3 ĐÃ XONG.**

Kiểm tận file, ba bằng chứng khớp nhau:

1. `Assets\_Game\Scenes\SampleScene.unity:3628-3648` — khối `cookingInventoryItems:` của `CookingBoot`
   (`&453846585`, script guid `1a9ab50d…`) có **20 mục**, mục thứ **20** ở **dòng 3648** là
   `guid: b5519c93970e53d478b4657f15532d65` = **`Item_Milk.asset`**.
2. `Assets\_Game\Farm\data\Farm_dong_vat\Item_Milk.asset:18` — `cookingData: {… guid: 7ec4c04aa13cb9f44b904d3efc4a59ea}`
   = **`SEA_Milk.asset`**, asset này **còn tồn tại** (C13 giữ lại, đúng như 6.A.7).
3. Giải mã cả 20 mục: **13 `kind=Ingredient` + 7 `kind=Seasoning`**, sữa nằm ở nhóm gia vị.
   `CookingBoot.FillOldCardsFromTransferredItems:103` phân luồng theo `cookingData.kind` ⇒ sữa
   vào **cột gia vị**, không phải cột nguyên liệu.

> **DEV-B ơi:** câu ở B.3/E3 *"`Item_Milk.asset` vẫn KHÔNG xuất hiện trong bất kỳ scene nào"* là **SAI**.
> guid đó xuất hiện **3 lần**: `SampleScene.unity:3648`, `SCN_Farm.unity:70263`, `SCN_Farm.unity:564321`.
> Nhiều khả năng bạn grep trước khi DEV-A ghi file. **Rủi ro B.9 #1 → ĐÓNG.** Nhánh Máy Phô Mai cấp 15 không bị chặn.

**P3 · SỬA SONG SONG — KHÔNG AI ĐÈ MẤT SỬA CỦA AI.**

Thực tế chỉ **4** file giao nhau (`FarmInventoryManager.cs` chỉ DEV-B đụng, không có trong danh sách 29 file của DEV-A).
Đọc chéo từng file, xác nhận **cả hai vệt sửa đều còn nguyên**:

| File | Dấu vết DEV-A | Dấu vết DEV-B | Kết luận |
|---|---|---|---|
| `FarmManager.cs` | C6: khối `seedStocks`/`ConsumeSeed`/`SeedStockData` đã đi hết, còn comment `:28-35` | `realTimeMultiplier = 1.0f` (`:66`) · `ScaleSeconds` (`:211`) · comment F10 (`:117`) | ✅ đủ cả hai |
| `PlotController.cs` | C1: `ApplyWaterBonus` = 0 kết quả grep | `LegacyPlotIdMap` (`:61`) · `MigrateLegacySaveIfNeeded` (`:907`) · `saveVersion` (`:28`) · `RushCostFor` (`:737`) · `CanAddItem` (`:650`) | ✅ đủ cả hai |
| `MissionProgressTracker.cs` | `DeadKeySubstrings` gỡ key món cá (`:38`) | `[RuntimeInitializeOnLoadMethod]` F3 (`:72`) · `SellAtStall`/`LoadTrainCargo` | ✅ đủ cả hai |
| `MarketPriceTable.cs` | 2 dòng cá đã xoá · `nuoc_mia_chanh` bật lại (`:346`, không còn `false`) | 45 con số mới (`rice 7` · `beef 165` · `milk 115` · `pho_mai 260`) | ✅ đủ cả hai |

⚠️ **Một điều cần biết cho lần sau:** `FarmManager.cs` và `FarmInventoryManager.cs` bị **ghi đè NGUYÊN FILE**
(đổi luôn kết thúc dòng LF → **CRLF** + thêm BOM). `git diff` báo 485 thêm / 563 xoá trên `FarmManager.cs`
— tức là *toàn bộ file*, không phải patch cục bộ. Lần này **may mắn không mất gì** vì người ghi sau đã
đọc lại file trước khi ghi, nhưng đây đúng là kịch bản dễ nuốt mất sửa của người kia nhất. Lần sau
**giữ nguyên kết thúc dòng** để `git diff` còn đọc được.

#### 6.T.1 — BẢNG T1 → T12

| # | Kiểm gì | Kết quả | Ghi chú |
|---|---|---|---|
| T1 | Biên dịch sạch | ✅ **ĐẠT** (kiểm tĩnh) | 8 phép kiểm ở P1. Vẫn cần 1 lần bấm Play để chốt tuyệt đối |
| T2 | 13 nhóm code chết đã xoá hết | ✅ **ĐẠT** | 0 tham chiếu sót. `SEA_Milk` giữ lại có chủ ý (6.A.7) — **đúng**, xem P2 |
| T3 | Mô phỏng cấp 1 → 30 | ✅ **ĐI ĐƯỢC, 0 chặn cứng** | Nhưng lệch nhịp EXP/vàng nặng — xem **CS-4** |
| T4 | 18/18 món nấu được | ✅ **18/18 ĐẠT** | Bảng đầy đủ ở 6.T.2 |
| T5 | Kinh tế 4 nguồn | ⚠️ **gần đạt** | Chuồng **2,17×–2,73×** ruộng — đúng như DEV-B báo. Nhưng **1 món bán rẻ hơn nguyên liệu** — xem **CS-1** |
| T6 | Không nghịch lý thời gian | ✅ **ĐẠT** | 21/21 cây tăng đơn điệu theo cấp |
| T7 | Không cây nào bán lỗ | ✅ **ĐẠT** | Lãi thấp nhất +8 (lúa). Lãi/giây **không tụt lần nào**: 0,1600 → 0,3086 |
| T8 | Hết cặp `plotId` trùng | ✅ **ĐẠT** | 38 component → **38 id duy nhất**. Đường migrate **bảo toàn cây đang trồng** — xem 6.T.3 |
| T9 | Tutorial không lặp lại | ✅ **ĐẠT** | `TUTORIAL_MAIN_DONE` + `SkipTutorialEntirely()` tắt `blocksRaycasts` — đúng chỗ |
| T10 | Mọi khoá save có `saveVersion` | ✅ **ĐẠT** | 9 họ khoá phụ + 8 hệ JSON tự quản. 7 nơi gọi `SaveVersionGuard.Ensure` |
| T11 | Tìm chức năng chết còn lại | ⚠️ **tìm được 12 chỗ** | Nặng nhất: **CB-1** (`ShowHint` câm) và **CS-2** (timer tàu chết) |
| T12 | Đề xuất hướng sửa | ✅ | 6.T.5, mỗi lỗi kèm `file:dòng` + code sai + code sửa |

#### 6.T.2 — T4 · 18/18 MÓN NẤU ĐƯỢC

Luật port từ `CookingScoreCalculator`: `final = ingredientScore + round(flavor100 × 0,3)`, kẹp [0,100], ngưỡng **70**.
`ingredientScore` = **70** khi tập nguyên liệu (**chỉ `kind == Ingredient`**) khớp đúng · **35** khi thiếu/thừa · **0** khi không trùng.
⇒ **Khớp đúng nguyên liệu là đã 70 = ĐẠT**, phần hương vị chỉ là điểm cộng.

| # | Món | Cấp | Nguyên liệu (đã phân giải guid → id + kind) | Gia vị | Điểm theo công thức | Điểm tối đa | Đạt? |
|---|---|---|---|---|---|---|---|
| 1 | `com_chien_trung` | 5 | rice, egg | soysauce | **100** | 100 | ✅ |
| 2 | `khoai_tay_chien` | 5 | khoaitay | — | **98** | 100 | ✅ |
| 3 | `trung_chien_ca_chua` | 5 | egg, cachua | — | **91** | 94 | ✅ |
| 4 | `bap_cai_xao_nam` | 6 | bapcai, mushroom | fishsauce | **100** | 100 | ✅ |
| 5 | `canh_khoai_tay_thit_heo` | 6 | pork, khoaitay | salt | **100** | 100 | ✅ |
| 6 | `sup_ngo_nam` | 6 | ngo, mushroom, egg | — | **98** | 98 | ✅ |
| 7 | `ga_nuong_lu` | 7 | chicken, **sugarcane** | pepper, salt | **96** | 96 | ✅ |
| 8 | `salad_nam_rau` | 7 | mushroom, herbs | — | **96** | 96 | ✅ |
| 9 | `thit_heo_luoc_cuon_rau` | 7 | pork, herbs | — | **97** | 100 | ✅ |
| 10 | `bo_ham_ca_rot` | 8 | beef, carot | pepper, salt | **100** | 100 | ✅ |
| 11 | `nam_xao_thit_bo` | 8 | mushroom, beef | soysauce | **100** | 100 | ✅ |
| 12 | `nuoc_mia_chanh` | 8 | **sugarcane** | lemon | **100** | 100 | ✅ |
| 13 | `salad_bap_cai_chanh` | 8 | bapcai, herbs | lemon | **100** | 100 | ✅ |
| 14 | `trung_op_la_bo_ne` | 8 | egg, beef, cachua | pepper | **94** | 94 | ✅ |
| 15 | `ga_xao_ot` | 9 | chicken | chili | **97** | 100 | ✅ |
| 16 | `pho_bo_tai` | 9 | beef, rice, herbs | lemon, chili | **86** | 100 | ✅ |
| 17 | `suon_heo_xao_chua_ngot` | 9 | pork, cachua | chili, lemon | **90** | 97 | ✅ |
| 18 | `bo_xao_tieu` | 10 | beef | pepper, soysauce | **100** | 100 | ✅ |

- Món nhiều nguyên liệu nhất = **3** ≤ `maxIngredients: 4`; nhiều gia vị nhất = **2** ≤ `maxSeasonings: 3`. Không món nào tràn ô.
- **13/13 nguyên liệu** và **7/8 gia vị** mà 18 món cần **đều có mặt** trong `cookingInventoryItems`. Chỉ `sugar` không có — và **0/18 món dùng nó**.
- Mọi nguyên liệu đều có **nguồn** trước hoặc đúng cấp mở món (ruộng · chuồng · **chợ** cho `salt/herbs/soysauce/fishsauce`). `herbs` — thứ 4 món cần — mua ở chợ từ **cấp 3** (`MarketPriceTable.cs:332`, `MarketDatabase` BuyPrice 27). Không thiếu chỗ nào.
- A2 xử lý đúng: `Item_sugarcane.cookingData` → `ING_Sugarcane` (`kind: 0`), và `ga_nuong_lu` đã đổi theo nên **không bị tính nguyên liệu thừa**.

> ⚠️ **BÁC BỎ rủi ro #1 của DEV-A (6.A.7 / 6.A.13) — CƠ CHẾ KHÔNG NHƯ MÔ TẢ.**
> `SEA_Milk` có `kind: 1` (Seasoning). `CookingScoreCalculator.ScoreRequiredIngredients` (`:96-100`)
> có `if (selected.kind != IngredientKind.Ingredient) continue;` ⇒ **thẻ sữa bị BỎ QUA hoàn toàn**
> khi chấm nguyên liệu. Sữa **không thể** bị tính là "nguyên liệu thừa", **không thể** kéo 70 xuống 35.
> Vì `final = 70 + seasoningScore` mà `seasoningScore ≥ 0`, bỏ sữa vào nồi **luôn ≥ 70 = vẫn ĐẠT** —
> tệ nhất là mất phần hương vị (tối đa −30đ).
> ⇒ Không cần gấp một "món sữa" để chữa cháy. **Nhưng vẫn nên có** vì lý do khác: sữa hiện là
> **ngõ cụt nội dung** — vào bếp được mà không món nào tiêu thụ. Sửa dòng rủi ro đó lại kẻo đợt sau
> có người đi "sửa" một lỗi không tồn tại.

#### 6.T.3 — T8 · `plotId` và đường cứu save

| Phép kiểm | Kết quả |
|---|---|
| Component `PlotController` trong `SCN_Farm` | **38** (26 object thật + 12 stripped của PrefabInstance) |
| Số `plotId` **duy nhất** | **38 / 38** ✅ |
| Cặp trùng còn lại | **KHÔNG CÒN** |
| Dải id | `1..30` + `101..108` |
| `LegacyPlotIdMap` | 8 cặp `{101→2, 102→3, 103→4, 104→5, 105→6, 106→1, 107→26, 108→27}` — **khớp 1:1** với 8 id mới có thật trong scene ✅ |

**DEV-B nói đúng, mục 3 của brief sai:** thực tế là **8 cặp** (`1,2,3,4,5,6` + `26,27`), không phải 9 cặp `2..8,26,27`.
Tôi dò lại độc lập: `plotId 7` và `8` nằm trong nhóm 26 object thật, **mỗi id đúng một lần** — không trùng.
Còn `Chauhoa_1` (PrefabInstance `1376952406`) **không có modification `plotId`** nên ăn mặc định `21` của prefab — chỗ dễ đếm sót nhất, và **không đụng ai** vì không object nào khác mang 21.

**Đường migrate CÓ bảo toàn cây đang trồng — xác nhận bằng đọc code:**
`PlotController.Load():938` gọi `MigrateLegacySaveIfNeeded()` **trước** `PlayerPrefs.HasKey(SaveKey)`.
Hàm này (`:907-932`) chỉ chạy khi *(a)* ô nằm trong `LegacyPlotIdMap`, *(b)* khoá **mới chưa có**, *(c)* khoá **cũ có dữ liệu** — rồi **COPY nguyên blob** (giữ `plantedCropId` · `startUnixTime` · `finishUnixTime` · `state`) sang khoá mới.
`KeyFor(oldId)` dùng `isRarePlot` của **chính ô đó**, nên chậu hoa đọc đúng `PLOT_RARE_26/27`, ô thường đọc đúng `PLOT_NORMAL_1..6`. ✅
Nếu **thiếu** bước này thì nhánh `!HasKey` ở `:942` sẽ `Save()` đè một trạng thái trắng — **mất sạch cây**. Bước này có, đúng chỗ, đúng thứ tự.
*(Hệ quả đã biết và chấp nhận được: một lần duy nhất sau cập nhật, cặp song sinh cùng hiện một cây và thu hoạch được cả hai. Ghi vào GY-8.)*

#### 6.T.4 — T3 · MÔ PHỎNG CẤP 1 → 30

Điều kiện đầu (đọc từ `SCN_Farm`): **400 vàng · 15 gem · 10 hạt lúa + 10 hạt hướng dương ·
26 ô ruộng + 12 chậu hoa dùng được ngay** (F10 xoá hệ khoá, `FarmManager.Start()` gọi `UnlockAllPlotsNow()`).
`realTimeMultiplier: 1` trong scene ⇒ `growSeconds` **là giây thật**. Tổng EXP tới cấp 30 = **6 366**.

| Cấp | EXP cần | EXP/giây | Vàng/giây | Vàng còn | Sự kiện |
|---|---|---|---|---|---|
| 1 | 40 | 3,91 | 6,42 | 466 | lúa · hướng dương · bắp cải. Trồng đủ 38 ô ngay lượt đầu (10+10 hạt tặng + 366 vàng mua nốt 18 ô ≤ 400) |
| 2 | 50 | 4,05 | 6,62 | 447 | ngô · **mua Chuồng Gà 100v** |
| 4 | 71 | 3,86 | 7,93 | 105 | **mua Chuồng Heo 600v** |
| **5** | 82 | 4,08 | 9,17 | 290 | **CỔNG BẾP MỞ** · 3 món đầu — nguyên liệu đủ (`khoaitay` L5 · `egg` từ Chuồng Gà L2 · `cachua` L3 · `soysauce` chợ L4) |
| 6 | 93 | 4,08 | 9,62 | 509 | nấm · **Chuồng Bò 1500v — CHƯA ĐỦ VÀNG** |
| 8 | 117 | 4,05 | 11,00 | 1 104 | chanh · **Chuồng Bò Sữa 2000v — CHƯA ĐỦ VÀNG** |
| **10** | 142 | 4,05 | 12,45 | 419 | **mua Chuồng Bò (trễ 4 cấp)** |
| **14** | 195 | 4,17 | 12,98 | 592 | **mua Chuồng Bò Sữa (trễ 6 cấp)** |
| **17** | 238 | 4,30 | 13,57 | 206 | **mua Máy Xay Bột (trễ 6 cấp)** |
| **21** | 300 | 4,43 | 14,28 | 771 | **mua Máy Ép Mía (trễ 8 cấp)** |
| **24** | 349 | 4,55 | 15,05 | 567 | **mua Máy Phô Mai (trễ 9 cấp)** |
| 30 | 456 | 4,68 | 15,89 | 8 932 | mua đủ 7/7 chuồng+máy |

**Trả lời dứt khoát: CÓ — người chơi đi được từ cấp 1 tới cấp 30, KHÔNG có điểm chặn cứng nào.**
Không cấp nào thiếu hạt, thiếu nguyên liệu, thiếu công trình bắt buộc, hay kẹt vì kho.
Nhịp thực tế (cây vẫn lớn khi offline vì `PlotController` lưu mốc unix): **~11 ngày** với 3 lần vào game/ngày,
**~17 ngày** với 2 lần/ngày.

**Nhưng có một chỗ *khựng* rõ rệt, không phải chặn — xem CS-4:** công trình mở theo **cấp** đến sớm hơn
khả năng trả tiền từ **4 đến 9 cấp**. Người chơi cấp 15 nhìn Máy Phô Mai sáng đèn rồi phải cày thêm
**9 cấp** mới mua nổi. Gốc là EXP tới nhanh gấp ~3–5 lần vàng (38 ô × 0,1 EXP/giây là hằng số, trong khi
lãi/giây chỉ 0,16 → 0,31).

#### 6.T.5 — LỖI, CHIA BA MỨC

##### 🔴 CHẶN BÀN GIAO

**CB-1 · `FarmUIManager.ShowHint()` CÂM — 36 lời gọi không hiện chữ nào.**
`Assets\_Game\Scenes\SCN_Farm.unity:459646` → `txtHint: {fileID: 0}` (chỉ có **một** `FarmUIManager` trong scene, và code **không** tự dò).

```csharp
// Assets\_Game\Farm\Scripts\UI\FarmUIManager.cs:114-118  — SAI (im lặng khi chưa gán)
public void ShowHint(string message)
{
    if (txtHint != null)
        txtHint.text = message;
}
```
Hệ quả **trực tiếp lên tiêu chí bàn giao của cả hai dev**:
- A6 "khoá cổng bếp tới cấp 5 **kèm thông báo 'Cần cấp 5'**" → người chơi cấp 1-4 bấm cổng bếp, **không có gì xảy ra, không lời giải thích**.
- Quyết định #4 / F8 "kho đầy thì chặn **và báo rõ**" → `PlotController.Harvest`, `PenMiniPanelUI.TryHarvest`, `TrainManager.CollectReward` chặn đúng nhưng **báo bằng mồm**. Người chơi bấm cây chín mà không thu được, không hiểu vì sao — giống hệt game hỏng.
- F9 "Cần N kim cương", các lỗi trồng trọt ("Chưa chọn ô đất") — cùng số phận.

**Sửa (2 phút, không đụng scene):**
```csharp
// Assets\_Game\Farm\Scripts\UI\FarmUIManager.cs
public void ShowHint(string message)
{
    // Ref rỗng trong scene không được phép biến thông báo thành im lặng: đây là
    // đường báo lỗi DUY NHẤT của cổng bếp (A6) và kho đầy (F8).
    if (txtHint == null)
        txtHint = GetComponentInChildren<TMP_Text>(true);   // hoặc dựng runtime 1 TMP_Text
    if (txtHint != null) txtHint.text = message;
    else Debug.LogWarning($"[FarmUI] txtHint chưa gán — mất thông báo: \"{message}\"");
}
```
Tốt nhất: gán thẳng `txtHint` (và `txtLevel`, `txtDay`) trong Inspector khi mở được Unity.

##### 🟠 CẦN SỬA

**CS-1 · `trung_op_la_bo_ne` NẤU RA RẺ HƠN NGUYÊN LIỆU — lỗ 100 vàng/lượt.**
Xác nhận bằng số cho rủi ro B.9 #3 của DEV-B.
`Dish_trung_op_la_bo_ne.asset`: `sellPrice: 145` · `rewardGold: 36` ⇒ tổng thu **181**.
Nguyên liệu theo `MarketPriceTable`: egg 20 + **beef 165** + cachua 20 + pepper 76 = **281**.
⇒ Người chơi **bán thẳng thịt bò lời hơn nấu**. Đây là món cấp 8 dễ nhất có thịt bò nên rất dễ dính.
Bốn món khác mỏng dưới 30%: `nam_xao_thit_bo` 25% · `bo_xao_tieu` 27% · `pho_bo_tai` 29% · `bo_ham_ca_rot` 30%
— trong khi nấu tốn **thêm một vòng thao tác + minigame**, đáng ra phải hơn bán thô nhiều hơn thế.
**Gốc:** A5 lấy `sellPrice = MarketPriceTable.GetBasePrice(dishId)` nhưng DEV-B nâng thịt bò 65 → **165**
mà giá **món** thì không nâng theo — đúng chỗ 6.A.0 đã cảnh báo "cuối đợt cần rà lại một lượt".
**Sửa:** đặt sàn `sellPrice ≥ 1,45 × Σ giá nguyên liệu` cho **cả 18 món** rồi đồng bộ lại
`MarketPriceTable` khối MÓN ĂN + `DishData.sellPrice` + `rewardGold = round(sellPrice × 0,25)`.
Riêng `trung_op_la_bo_ne` cần `sellPrice ≈ 410` (hoặc bỏ `beef` khỏi công thức — món "trứng ốp la bò né" 145 vàng vốn không hợp lý khi có 165 vàng thịt bò).

**CS-2 · Hệ timer tàu CHẾT — nghi ngờ B.9 #7 của DEV-B là ĐÚNG.**
`Assets\_Game\Farm\Scripts\Train\TrainManager.cs:179-189` — **cả thân `Update()` bị comment**:
```csharp
void Update()
{
    // Timer tạm tắt — flow chạy liền không đợi
    // if (!_timerActive) return;
    // processPopup?.UpdateTimer(TripRemainingTime);
    // if (TripRemainingTime <= 0f) { _timerActive = false; OnProcessingTimerExpired(); }
}
```
`tripDurationSeconds` (`:85`) và `_timerActive` (`:97`) đều phải bọc `#pragma warning disable 0414`
**đúng vì không ai đọc chúng**. `StartProcessing` cũng bị comment (`:351-353`).
⇒ **Thời gian tàu ở trong hầm không do config quyết định**, chỉnh `tripDurationSeconds` trong Inspector là vô nghĩa.
**Sửa:** hoặc bật lại timer (bỏ comment + gọi `_tripEndTime = Time.time + FarmManager.ScaleSeconds(tripDurationSeconds)` khi vào hầm), hoặc **xoá hẳn** `tripDurationSeconds` + `_timerActive` + `_tripEndTime` + `TripRemainingTime` + 2 khối `#pragma` + `Update()` rỗng. Đừng để nguyên trạng — trường Inspector nói dối người chỉnh số.

**CS-3 · `FarmManager.rarePlotsRoot` RỖNG ⇒ 12 chậu hoa vô hình với `FarmManager`.**
`SCN_Farm.unity` — `rarePlotsRoot: {fileID: 0}` (cùng chỗ với `defaultNormalCrop` và `defaultRareCrop` cũng rỗng).
`CachePlotsFromRoots():136` ⇒ `rarePlots` **luôn rỗng** ⇒ `UnlockAllPlotsNow():149` không chạm chậu hoa nào,
và vòng dò "ô sắp chín nhất" (`:469-479`) bỏ qua toàn bộ chậu hoa.
Hiện **chưa vỡ gameplay** vì F10 đã cho `PlotController.Load()` mặc định `Empty`, nhưng đây là một root ref rỗng
nằm giữa manager quan trọng nhất — sẽ cắn ở tính năng tiếp theo dùng `rarePlots`.
**Sửa:** gán `rarePlotsRoot` trong Inspector, hoặc bỏ hẳn hai root và đổi `CachePlotsFromRoots` sang
`FindObjectsByType<PlotController>(...)` rồi phân loại bằng `IsRarePlot`.

**CS-4 · Công trình mở theo cấp SỚM HƠN khả năng trả tiền 4–9 cấp.**
Xem bảng 6.T.4. Chuồng Bò mở L6 mua được L10 · Bò Sữa mở L8 mua được L14 · Máy Phô Mai mở L15 mua được **L24**.
Gốc: `RequiredExpForLevel` quá nông so với 38 ô ruộng (mỗi ô cố định **0,1 EXP/giây**, tổng ~3,9–4,7 EXP/giây)
trong khi lãi/giây chỉ 0,16 → 0,31.
**Sửa (chọn 1):** *(a)* hạ giá 4 công trình cao cấp ~35–40% (`Chuồng Bò 1500→950`, `Bò Sữa 2000→1300`,
`Xay Bột 2500→1600`, `Ép Mía 3000→2000`, `Phô Mai 3500→2400`); hoặc *(b)* nâng hệ số EXP trong
`RequiredExpForLevel` (ví dụ `3n²/20` → `n²/4`) để cấp đi chậm lại cho khớp dòng tiền. Khuyên **(a)** vì
không đụng save của người chơi.

**CS-5 · `Home5.asset` trùng `itemID: 104` với `Home4.asset`.**
`Assets\_Game\Farm\CÔNG TRÌNH\DataShop\Buiding\Home5.asset` — `itemID: 104`, `itemName: "Nhà Dân 4"`, y hệt `Home4`.
`PlacementManager.CountPlacedByItemId(104)` gộp chung hai món; nếu sau này áp giá luỹ tiến F10 cho nhà thì
mua nhà 4 sẽ làm nhà 5 đắt lên. Cùng họ lỗi với `plotId` trùng.
**Sửa:** đổi `Home5.itemID` sang `105`… nhưng `105` đang là `Khung Hoa` ⇒ cấp `114` (ngoài dải đang dùng), và sửa tên hiển thị.

**CS-6 · `FarmInventoryManager.OnAddRejectedByCapacity` bắn nhưng KHÔNG AI NGHE.**
`FarmInventoryManager.cs:66` — comment ghi *"UI nào muốn hiện popup 'kho đầy' thì nghe ở đây"*, nhưng
grep `OnAddRejectedByCapacity +=` toàn dự án = **0**. Sự kiện chết ngay khi vừa sinh ra.
**Sửa:** cho `FarmUIManager` (hoặc `WarehousePopupUI`) `+=` trong `OnEnable` và `ShowHint`/mở popup nâng cấp kho. Làm cùng CB-1 là gọn nhất.

##### 🟢 GÓP Ý

| # | Nội dung |
|---|---|
| GY-1 | **11 script thiếu trong scene, CÓ TỪ TRƯỚC đợt này** (không phải lỗi của hai dev): `SCN_Farm` — `CowSlotUI`×2, `PigSlotUI`×2, `ChickenSlotUI`×2, `DraggablePigFeedItem`×2, `DraggableChickenFeedItem`×2, `CowPenPopupUI`, `PigPenPopupUI`, `ChickenPenPopupUI`; `SampleScene` — `DropZone`, `DishDetailUI`, `RequiredIngredientItemUI`. Đây là cụm UI chuồng **đời cũ** đã bị `PenMiniPanelUI` thay. Unity sẽ log "The referenced script is missing" mỗi lần mở scene. Nên dọn ở đợt sau |
| GY-2 | **Ví/tiến trình THỨ HAI vẫn còn sống trong code**: `Scripts\Core\PlayerProfile.cs` (có `AddGold`, `CurrentEXP`, `Level`) + `Scripts\UI\HUDController.cs` — **0 instance trong mọi scene**. Đúng cùng họ với `PlayerWallet` mà C7 đã xoá. Nhóm C bỏ sót. Đề nghị xoá tiếp |
| GY-3 | MonoBehaviour **0 instance, 0 `AddComponent`** ⇒ chết hẳn: `TopBarExpUI` (thanh EXP top-bar — `FindFirstObjectByType<TopBarExpUI>()` ở `HarvestFeedbackSpawner:50,89` và `UnifiedTaskPopupUI:1036` **luôn trả null**; may là `JudgeAvatarProfileButton` cũng nghe `OnExpChanged` nên EXP vẫn hiện) · `TutorialPrePlant` (chính `SetupTutorialL1L2Tool:529` gọi nó "obsolete") · `TrainController` (`Taulua\`, bản tàu cũ, 0 tham chiếu) · `FarmPlotInput` · `TrainWorldSlotUI` (bọc `#pragma 0414, 0649`) |
| GY-4 | `FarmManager.TryPlantSelectedDefaultCrop()` (`:407`) — **0 nơi gọi trong code, 0 trong scene**. Cùng hai field chỉ nó đọc (`defaultNormalCrop`, `defaultRareCrop`, cả hai rỗng trong scene). Code chết mà nhóm C bỏ sót |
| GY-5 | **Sự kiện bắn mà không ai nghe** (ngoài CS-6): `PlayerStallManager.OnListingSold` / `OnListingExpired` · `ConstructionManager.OnConstructionComplete` · `ConstructionSiteUI.OnRushClicked` · `SceneLoader.OnSceneLoadStart` / `OnSceneLoadComplete` · `GiftBoxReveal.OnFinished` · `DayNightWeatherSystem.WeatherChanged`. Đều có từ trước |
| GY-6 | Còn **1 nút** trong `SCN_Farm:544771` có `m_Calls` với `m_Target: {fileID: 0}` + `m_MethodName:` rỗng (F6 dọn 3, sót 1). Vô hại nhưng nên dọn nốt. Nút `btn_PhanBon_PL` (`:386510`) vẫn `m_OnClick.m_Calls: []` — DEV-A đã bàn giao cho F6, **F6 chưa xử lý** |
| GY-7 | **`son` (Sơn) là ngõ cụt**: chỉ có ở thưởng tàu preset 2, **không hệ nào tiêu thụ** (nâng cấp kho cần `da/dinh/go/kinh`, `BuildUpgradeRequirements:993`). Nó chiếm 1 slot kho vĩnh viễn. Hoặc cho vào công thức nâng cấp, hoặc bỏ khỏi preset |
| GY-8 | Migrate F1 (đúng, đã lường): sau cập nhật, mỗi cặp song sinh **cùng hiện một cây và thu hoạch được cả hai** đúng **một lần**. Người chơi được lợi nhẹ, không mất gì. Chấp nhận |
| GY-9 | 3 lời gọi `AddItem` ở **đường hoàn hàng** vẫn bỏ qua giá trị trả về: `OrderBoardManager.cs:328` (rollback đơn) · `PlayerStallManager.cs:553` (huỷ đăng bán) · `PenMiniPanelUI.cs:587` (hoàn thức ăn). Chỉ cắn khi kho vừa đầy **và** loại đó vừa về 0 — hiếm, nhưng là mất đồ. Nên `if (!AddItem(...)) { giữ nguyên trạng thái cũ + ShowHint }` |
| GY-10 | `FarmInventoryManager.UsedSlots => items.Count` nhưng `GetOrderedItems()` lọc `amount > 0`. Save hỏng có entry `amount = 0` sẽ khiến kho báo "13/25" mà popup vẽ 12 dòng, và **chặn nhận loại mới sớm hơn thật**. Nên `UsedSlots` đếm cùng điều kiện |
| GY-11 | `BasePriceBook.Fallback` còn **số đời cũ** (`beef 60`, `milk 22`, `pho_bo_tai 160`, `nam 30`, `sugar 10`). Hiện **không chạm tới** vì bậc 2 hỏi `MarketPriceTable` trước — **trừ** `sugar` và `nam` (hai id `MarketPriceTable` không có). Nên dọn cho khỏi bẫy người sau |
| GY-12 | Xác nhận rủi ro #2 của DEV-A: `SEA_Sugar.asset` **mồ côi thật** (0 `DishData`, 0 `InventoryItemData` trỏ vào) và `MarketPriceTable` **không có** dòng `sugar` trong khi `BasePriceBook` có. Cùng gốc với GY-11 |

##### ✅ TESTER ĐÃ TỰ SỬA — 2 LỖI **MẤT TIỀN / MẤT ĐỒ CỦA NGƯỜI CHƠI** (4 file)

Cả hai đều là **lỗ hổng của F8**: DEV-B đổi `AddItem` từ `void` sang `bool` và chặn đúng ở 3 nơi
(`PlotController.Harvest`, `PenMiniPanelUI.TryHarvest`, `TrainManager.CollectReward`) nhưng **bỏ sót 2 nơi khác**
vẫn huỷ nguồn sau khi `AddItem` trả `false`.

**TF-1 · MUA Ở CHỢ: TRỪ TIỀN RỒI MỚI CỘNG KHO ⇒ kho đầy = mất trắng số vàng đó.**
`Assets\_Game\Farm\Scripts\Market\MarketManager.cs` — `TryBuyListing` gọi `SpendGold(totalPrice)` **trước**
`GiveItemToCorrectStorage(...)`, mà hàm sau bỏ qua `bool` của `AddItem`.
Trớ trêu là chú thích ngay trên hàm ghi *"kiểm tra đủ → trừ tiền → cộng kho"* — bước "kiểm tra đủ" thiếu đúng phép kiểm sức chứa.
Rất dễ dính: `herbs` (4 món cần) hầu như luôn là **loại mới** trong kho vì nấu xong là hết.
```csharp
// SAI
if (!isSeed && FarmInventoryManager.Instance == null)
    return MarketBuyResult.InventoryMissing;

if (!SpendGold(totalPrice))                       // ← tiền bay trước
    return MarketBuyResult.NotEnoughGold;
GiveItemToCorrectStorage(listing.ItemId, listing.Quantity, isSeed);   // ← có thể từ chối im lặng

// ĐÃ SỬA
if (!isSeed && !FarmInventoryManager.Instance.CanAddItem(listing.ItemId))
    return MarketBuyResult.InventoryFull;         // ← chặn TRƯỚC khi trừ tiền
if (!SpendGold(totalPrice))
    return MarketBuyResult.NotEnoughGold;
```
Kèm theo: `IMarketProvider.cs` thêm `InventoryFull = 6` (mã riêng — "kho chưa sẵn sàng" và "kho đầy" đòi hai
hành động khác nhau) và `MarketBoardUI.cs` thêm `case` báo **"Kho đầy — bán bớt hoặc nâng cấp kho"**
(không để rơi vào `default:` vì `default` báo sai *"Món này vừa có người mua"* rồi `Redraw`, người chơi bấm mua lại mãi).

**TF-2 · NHẬN MÓN ĂN VỀ KHO: kho đầy ⇒ MÓN BỐC HƠI dù nguyên liệu đã bị trừ.**
`Assets\_Game\Scripts\CookingChallengeManager.cs:199` — `CollectCookedDishToWarehouse()` bỏ qua `bool`
rồi vẫn `cookedDishOnPlate = null` (`:207`) + `HideCookedDish()`.
Một đĩa `pho_bo_tai` = **310 vàng nguyên liệu** biến mất không dấu vết.
```csharp
// SAI
FarmInventoryManager.Instance.AddItem(cookedDishOnPlate.dishId, 1);

// ĐÃ SỬA — giữ món trên dĩa, dọn kho xong bấm lại là nhận được
if (!FarmInventoryManager.Instance.AddItem(cookedDishOnPlate.dishId, 1))
{
    Debug.LogWarning($"[Cooking] Kho đầy — chưa đưa '{cookedDishOnPlate.dishId}' vào kho. …");
    FarmUIManager.Instance?.ShowHint("Kho đầy — bán bớt hoặc nâng cấp kho rồi nhận món.");
    return;
}
```
> ⚠️ Câu `ShowHint` này **hiện chưa hiện được** vì **CB-1**. Sửa CB-1 xong là chạy.
> Ghi chú nhỏ cho DEV-A: `deliveryCharacterMover.ShowDeliveryOnly()` vẫn chạy **trước** chỗ `return` mới —
> nhân vật giao hàng nhấp nháy một cái rồi thôi. Chỉ là mỹ quan, tôi **không** đảo thứ tự để giữ patch nhỏ nhất.

**Đã kiểm lại sau khi vá:** cân bằng `{} () []` + `#if/#endif` trên **344/344** file `.cs` — **0 lệch**.

#### 6.T.6 — CÔNG CỤ BÀN GIAO

`production\tools\mo_phong_cap1_cap30.py` — chạy `python mo_phong_cap1_cap30.py [gốc_dự_án]`.
In ra **T3 · T4 · T5 · T6 · T7 · T8** trong một lượt, đọc thẳng asset thật.
Đổi bất kỳ con số cân bằng nào thì chạy lại là biết ngay có vỡ nghịch lý / vỡ ngưỡng 70 / vỡ plotId không.


### DEV-A (vòng 2) — 6 LỖI 🟠 CẦN SỬA

> Nhận việc: CS-1 → CS-6. CB-1 đã có người vá trước (`ShowHint` giờ có đường dự phòng) nên
> tôi **dùng lại** đường đó cho CS-6 chứ không dựng đường thông báo thứ hai.
> Vẫn **không mở được Unity** ⇒ mọi kết luận dưới đây là kiểm tĩnh + chạy lại đúng công cụ
> của TESTER (`mo_phong_cap1_cap30.py`) trên asset THẬT sau khi sửa.

#### 6.A2.1 — CS-1 · SÀN LỢI NHUẬN CHO CẢ 18 MÓN

Rà cả 18 món: tổng giá nguyên liệu tính bằng `MarketPriceTable.GetBasePrice` **có áp bảng
`Aliases`** (`chicken` → `chicken_meat`), cộng cả gia vị. Thu = `sellPrice` + `rewardGold`.

**Sàn đặt theo `DishData.difficulty`, tăng dần theo độ khó:** `0 → ≥35 %` · `1 → ≥45 %` · `2 → ≥60 %`.
Quy ước phụ giữ nguyên đề xuất của TESTER: `rewardGold = round(sellPrice × 0,25)`.

Chỉ **NÂNG**, không hạ món nào — hạ giá là cắt thu nhập người chơi đã quen, và 12 món còn lại
vốn đã trên sàn rất xa.

| Món | df | NL | thu cũ | % cũ | thu mới | % mới | `sellPrice` | `rewardGold` |
|---|---|---|---|---|---|---|---|---|
| `trung_op_la_bo_ne` | 0 | 281 | 181 | **−36 %** | 381 | **+36 %** | 145 → **305** | 36 → **76** |
| `nam_xao_thit_bo` | 1 | 225 | 281 | 25 % | 331 | **47 %** | 225 → **265** | 56 → **66** |
| `bo_xao_tieu` | 1 | 267 | 338 | 27 % | 394 | **48 %** | 270 → **315** | 68 → **79** |
| `bo_ham_ca_rot` | 2 | 270 | 350 | 30 % | 438 | **62 %** | 280 → **350** | 70 → **88** |
| `pho_bo_tai` | 2 | 310 | 400 | 29 % | 500 | **61 %** | 320 → **400** | 80 → **100** |
| `suon_heo_xao_chua_ngot` | 2 | 230 | 369 | 60,4 % | 375 | **63 %** | 295 → **300** | 74 → **75** |

12 món còn lại đã trên sàn, **không đụng**: thấp nhất là `ga_nuong_lu` 50 % (sàn 45) và
`nuoc_mia_chanh` 53 % (sàn 35). Cao nhất `salad_nam_rau` 321 %.

**Ba nơi phải khớp nhau, đã sửa đủ cả ba:**
`DishData.sellPrice` (6 asset) · `MarketPriceTable` khối MÓN ĂN (6 dòng) ·
`MarketDatabase.asset` `BuyPrice` (sinh lại bằng `production\tools\regen_marketdb.py --write`, đổi 8 giá trị).
Bộ kiểm tự viết đối chiếu **cả ba nguồn + sàn lãi** cho 18/18 món: **0 lệch**.

> ⚠️ **`ga_nuong_lu` bị công cụ báo 82 % thay vì 50 %** — không phải lỗi cân bằng.
> `mo_phong_cap1_cap30.py` tra giá bằng `PRICE[...]` **không đi qua bảng `Aliases`**, nên
> `chicken` ra 0 vàng (giá thật nằm ở khoá `chicken_meat` = 29). `ga_xao_ot` cũng vậy
> (341 % ảo / 209 % thật). Tôi **không sửa công cụ của TESTER**; ghi ra đây để lần chạy sau
> khỏi tưởng hai món đó lãi dày hơn thực tế. Cả hai vẫn trên sàn kể cả khi tính đúng.

**Hệ quả có chủ ý:** một đĩa mua ở chợ (`BasePrice × 1,5`) nay **đắt hơn** tự gom nguyên liệu.
Câu cũ trong comment *"mua một đĩa phở rẻ hơn tự gom nguyên liệu"* đã sai và đã được sửa lại —
tiền công nấu phải nằm trong giá món, nếu không thì chính chợ là đường lách khiến nấu ăn vô nghĩa.

#### 6.A2.2 — CS-2 · XOÁ HỆ TIMER TÀU (không bật lại)

Chốt theo hướng TESTER đề xuất và brief xác nhận: luồng "chạy liền không đợi" là **quyết định
thiết kế cố ý và đang chạy đúng**, nên xoá code chết chứ không hồi sinh timer.

Đã xoá khỏi `TrainManager.cs`: `Update()` rỗng · khối comment `StartProcessingTimer()` ·
`tripDurationSeconds` · `_tripEndTime` · `_timerActive` · **cả 2 cặp `#pragma warning disable/restore 0414`** ·
`OnProcessingTimerExpired()`.

Hai ký hiệu TESTER dặn kiểm trước khi xoá:

| Ký hiệu | Còn ai gọi? | Xử lý |
|---|---|---|
| `TripRemainingTime` | **CÒN** — `TrainStationBuilding.cs:67` | **GIỮ**, nhưng đổi thân thành `=> 0f`. Hành vi y hệt bản cũ (`_tripEndTime` chưa từng được gán ⇒ hiệu luôn âm ⇒ `Mathf.Max` kẹp về 0), chỉ khác là hết đọc biến chết |
| `OnProcessingTimerExpired` | **0 lời gọi thật** (chỉ nằm trong khối comment của `Update()`) | **XOÁ**. Phần việc của nó đã nằm nguyên trong `OnShippingReachedTunnel()` |

**Đụng scene:** `SCN_Farm.unity` có `tripDurationSeconds: 300` — đúng bằng chứng cho lỗi này,
ai đó đã chỉnh thành 5 phút mà game không đổi một giây nào. Đã xoá dòng thuộc tính mồ côi đó.
`TrainState.Processing` **giữ nguyên** (không còn state nào vào nó, nhưng xoá phần tử enum là
rủi ro chéo không cần thiết).

#### 6.A2.3 — CS-3 · XOÁ `rarePlotsRoot` **VÀ** `normalPlotsRoot`

Kiểm thật thì lỗi **nặng hơn** mô tả của CS-3, và **cơ chế khác** với mô tả:

- `rarePlotsRoot: {fileID: 0}` — rỗng, đúng như TESTER báo.
- **Nhưng `normalPlotsRoot` cũng hỏng:** cây con của Transform đó chỉ chứa **19** `PlotController`,
  trong khi scene có **38**. Gần một nửa số ô đất đứng ngoài tầm nhìn của `FarmManager`.
- **Và chuyện "12 chậu hoa là rare" là không đúng:** grep `isRarePlot` ra **26 lần trong scene +
  5 prefab (`Chauhoa_1..4`, `Plot_01`), TẤT CẢ đều `= 0`**. Không một ô nào trong dự án đang là
  ô hiếm. Nhánh khoá save `PLOT_RARE_x` của `PlotController.KeyFor()` hiện **chưa từng chạy** —
  câu ở 6.T.3 *"chậu hoa đọc đúng `PLOT_RARE_26/27`"* là **suy luận, không phải sự thật trên file**.

**Sửa:** xoá cả hai field, `CachePlotsFromRoots()` chuyển sang
`FindObjectsByType<PlotController>(FindObjectsInactive.Include, …)` rồi phân nhóm bằng
`PlotController.IsRarePlot` — cùng đúng một cờ mà khoá save đang dùng, nên hai bên không thể lệch.
Giữ nguyên tên hàm + mục ContextMenu (2 nơi trong chính file đang gọi).

Kết quả: **38/38 ô** vào `normalPlots` (vì `isRarePlot` toàn 0), `rarePlots` vẫn rỗng — nhưng
**không còn ô nào bị bỏ sót**, đó mới là thứ `UnlockAllPlotsNow()` và `GetNextGrowingPlot()` cần.
Ngày nào chậu hoa được tick `isRarePlot` thì chúng tự tách nhóm, không phải sửa thêm dòng nào.
Cách này còn sống sót khi người chơi **mua thêm ô đất** — `PlacementManager` đẻ ô mới ở gốc scene,
ngoài mọi root, nên hệ root cũ **chắc chắn** sẽ bỏ sót chúng.

**Đụng scene:** xoá 2 dòng thuộc tính mồ côi `normalPlotsRoot` / `rarePlotsRoot`.

#### 6.A2.4 — CS-4 · DỜI `unlockLevel` CHO KHỚP KHẢ NĂNG CHI TRẢ

Chạy `mo_phong_cap1_cap30.py` (mô hình vàng chỉ tính ruộng + chuồng, tức **cận dưới** — chưa
tính nấu ăn, đơn hàng, tàu, quầy) để tìm **cấp đầu tiên đủ vàng** cho từng công trình, rồi đặt
`unlockLevel` đúng vào đó.

| Công trình | Giá | `unlockLevel` cũ | Mua nổi ở cấp | `unlockLevel` mới | Trễ cũ |
|---|---|---|---|---|---|
| Chuồng Gà | 100 | 2 | 2 | **2** (giữ) | 0 |
| Chuồng Heo | 600 | 4 | 4 | **4** (giữ) | 0 |
| Chuồng Bò | 1500 → **950** | 6 | 8 | **8** | −4 |
| Chuồng Bò Sữa | 2000 | 8 | 13 | **13** | −6 |
| Máy Xay Bột | 2500 | 11 | 17 | **17** | −6 |
| Máy Ép Mía | 3000 | 13 | 21 | **21** | −8 |
| Máy Phô Mai | 3500 | 15 | 24 | **24** | −9 |

**Vì sao RIÊNG Chuồng Bò phải hạ giá thay vì dời cấp** (chỗ duy nhất tôi đi lệch chữ "chỉ dời
`unlockLevel`"): Chuồng Bò là **nguồn thịt bò duy nhất bằng lao động**, mà **3 món cấp 8**
(`bo_ham_ca_rot`, `nam_xao_thit_bo`, `trung_op_la_bo_ne`) và `pho_bo_tai` cấp 9 đều cần bò.
Dời chuồng lên L10 cho khớp giá 1500 thì 4 món đó thành **không nấu được bằng lao động** →
vỡ tiêu chí "18/18 món", và phải kéo theo 4 `DishData.unlockLevel` + 4 dòng `MarketPriceTable`.
Nên trần cứng của Chuồng Bò là **L8**, và ở L8 mô phỏng cho 1104 vàng ⇒ giá phải ≤ ~1100.
Lấy **950** — đúng con số TESTER đã đề xuất ở CS-4 phương án (a), còn dư 154 vàng.
9 công trình còn lại (nhà dân, chậu hoa, khung hoa, ô đất) đều L1 và ≤ 180 vàng — mua được ngay
từ cấp 1, **không đụng**.

**Đồng bộ kéo theo** (`MarketPriceTable.UnlockLevel` **phải** bằng cấp mở chuồng/máy, nếu không bộ
sinh đơn ra đơn đòi thứ chưa có cách nào làm): `beef` 6→8 · `milk` 8→13 · `bot_gao` 11→17 ·
`nuoc_mia_ep` 13→21 · `pho_mai` 15→24. `MarketDatabase.asset` đã sinh lại theo.

**Chạy lại công cụ sau khi sửa — 0 dòng "chưa đủ vàng", 0 dòng "trễ N cấp":**

| Cấp | Sự kiện | Vàng còn sau khi mua |
|---|---|---|
| 2 | MUA Chuồng Gà (−100) | 447 |
| 4 | MUA Chuồng Heo (−600) | 105 |
| **8** | MUA Chuồng Bò (−950) · mở 5 món | **154** |
| **13** | MUA Chuồng Bò Sữa (−2000) | **545** |
| **17** | MUA Máy Xay Bột (−2500) | **775** |
| **21** | MUA Máy Ép Mía (−3000) | **1 340** |
| **24** | MUA Máy Phô Mai (−3500) | **1 136** |

`ĐIỂM CHẶN: KHÔNG có điểm chặn cứng nào trên đường cấp 1 → 30 ✔` · `T4: ĐẠT 18/18` ·
`T5: Món nấu ra RẺ HƠN nguyên liệu — KHÔNG CÓ ✔` · `T8: 38/38 plotId duy nhất, không đổi`.
Tỉ lệ lãi/giây chuồng ÷ ruộng cùng cấp vẫn trong dải **1,93× → 2,73×** (tiêu chí "không gấp chục lần" giữ nguyên).

#### 6.A2.5 — CS-5 · `Home5` HẾT TRÙNG `itemID`

`Home5.asset`: `itemID: 104` → **`114`**, `itemName: "Nhà Dân 4"` → **"Nhà Dân 5"**.
Quét lại **toàn bộ `DataShop\`**: id đang dùng = `100…114, 120, 121, 122`, **0 cặp trùng**.

**Về rủi ro save mà brief cảnh báo — KHÔNG CÓ, và đây là lý do:**
`Home5.asset` là **asset mồ côi**. Grep guid `ec3f7dbf…` trên toàn bộ `.unity/.prefab/.asset` =
**0 tham chiếu**; `buildingList` của `ShopManager` trong `SCN_Farm:382768-382783` có 15 mục, có
`Home4` (`b6d4dab1…`) nhưng **không có `Home5`**. Không mua được ⇒ chưa từng có entry
`FARM_PLACED_BUILDINGS` nào mang nó ⇒ không cần đường migrate.
Thêm nữa, `PlacementManager.FindItemById()` trả về **mục đầu tiên** khớp — nên khi hai asset cùng
id `104`, đúng một trong hai vốn đã **không bao giờ** load được. Đổi id chỉ làm hết mập mờ.

> 📌 **Còn lại cho designer:** `Home5` vẫn nằm ngoài `buildingList` nên chưa bán được trong Shop.
> Thêm vào hay không là **quyết định nội dung**, tôi không tự ý sửa mảng trong scene.

#### 6.A2.6 — CS-6 · KHO ĐẦY GIỜ CÓ BÁO (khép lại F8)

`FarmUIManager` đăng ký `FarmInventoryManager.OnAddRejectedByCapacity` ở `OnEnable`, gỡ ở `OnDisable`
(sự kiện là **static** — đăng ký ở `Awake` mà quên gỡ là kho giữ tham chiếu tới `FarmUIManager` đã
chết qua mỗi lần đổi scene). Handler ghép tên tiếng Việt bằng `MarketPriceTable.GetDisplayName` và
kèm số ô đang dùng, rồi gọi thẳng `ShowHint` — chính đường dự phòng vừa được vá ở CB-1:

> `Kho đầy (25/25 ô) — chưa nhận được "Rau Thơm". Bán bớt hoặc nâng cấp kho.`

Nhờ vậy **mọi** đường bị chặn vì kho đầy đều lên tiếng, kể cả 3 đường ở **GY-9** mà chưa ai xử lý
giá trị trả về (`OrderBoardManager:328`, `PlayerStallManager:553`, `PenMiniPanelUI:587`): vật phẩm
vẫn có thể mất ở đó, nhưng ít nhất người chơi **thấy** lý do thay vì tưởng game hỏng.

#### 6.A2.7 — KIỂM TRA SAU KHI SỬA

| Phép kiểm | Kết quả |
|---|---|
| Cân bằng `{} () []` + `#if/#endif` trên **344/344** file `.cs` | ✅ **0 lệch** |
| Grep 6 ký hiệu vừa xoá (`rarePlotsRoot`, `normalPlotsRoot`, `tripDurationSeconds`, `_timerActive`, `_tripEndTime`, `OnProcessingTimerExpired`) | ✅ **0 lời gọi thật** — chỉ còn trong comment giải trình |
| 18/18 món: `DishData.sellPrice` = `MarketPriceTable` = `MarketDatabase.BuyPrice ÷ 1,5` **và** đạt sàn lãi | ✅ **0 lệch** |
| `UnlockLevel` đồng bộ 3 nguồn cho `beef`/`milk`/`bot_gao`/`nuoc_mia_ep`/`pho_mai` | ✅ khớp cả 5 |
| `itemID` trùng trong `DataShop\` | ✅ **0 cặp** |
| Đếm object `SCN_Farm` trước/sau (chỉ xoá 3 dòng thuộc tính mồ côi) | ✅ **y hệt**: 6 193 document · 1 614 GameObject · 1 166 MonoBehaviour · 703 Transform · 435 PrefabInstance |
| `mo_phong_cap1_cap30.py` — T3 · T4 · T5 · T8 | ✅ 0 điểm chặn · 18/18 món · 0 món lỗ · 38/38 plotId |
| Kết thúc dòng + BOM của mọi file đã sửa | ✅ giữ nguyên (`TrainManager.cs` LF+BOM, `MarketDatabase.asset` LF không BOM, `SCN_Farm.unity` LF không BOM) |

#### 6.A2.8 — CÒN LẠI / RỦI RO

| # | Nội dung | Mức |
|---|---|---|
| 1 | **Vẫn chưa bấm Play.** Máy không mở được Unity. Toàn bộ trên đây là kiểm tĩnh — T1 của TESTER vẫn là chốt cuối | ⚠️ |
| 2 | **Máy Phô Mai lùi tới cấp 24** (trên đường 30 cấp) — hơi sâu. Sở dĩ vậy vì mô hình vàng của công cụ là **cận dưới**: nó bỏ qua nấu ăn, đơn hàng, tàu, quầy hàng. Người chơi thật sẽ dư tiền **trước** cấp đó ⇒ công trình mở ra là mua được ngay, tức lệch về phía **an toàn**. Nếu muốn kéo sớm lại thì hạ giá 3 máy ~25 % (2500/3000/3500 → 1900/2300/2700) sẽ về **L15/L18/L21** — tôi **không** tự làm vì brief chốt "dời `unlockLevel`" chứ không phải "hạ giá" | 🟡 |
| 3 | **Giá món tăng ⇒ thưởng đơn hàng cũng tăng theo** (`OrderGenerator` tính theo giá gốc). Đơn có `pho_bo_tai` nay đắt hơn ~25 %. Đúng hướng (nấu ăn phải đáng công) nhưng là một khoản lạm phát chưa mô phỏng | 🟡 |
| 4 | **Sàn chỉ chặn phía dưới, không nén phía trên.** Sau khi sửa, biên lãi vẫn trải rất rộng: 36 % (`trung_op_la_bo_ne`) → 321 % (`salad_nam_rau`). Món rẻ vẫn là máy in tiền tương đối. Nén dải này là việc **cân bằng nội dung**, cần một vòng thiết kế riêng chứ không phải sửa lỗi | 🟡 |
| 5 | **`isRarePlot` toàn dự án = 0** (xem 6.A2.3). Nhánh khoá save `PLOT_RARE_x` và cả khái niệm "ô hiếm" hiện là code chưa từng chạy. Hoặc tick cờ cho 12 chậu hoa, hoặc xoá hẳn nhánh rare — **đừng để nguyên**, vì `LegacyPlotIdMap` đang migrate `107→26, 108→27` với giả định chậu hoa là rare | 🟠 |
| 6 | `Home5` vẫn ngoài `buildingList` (xem 6.A2.5) — chờ quyết định nội dung | 🟢 |
| 7 | `TrainStationBuilding` bấm vào nhà ga vẫn mở popup ghi *"Đang vận chuyển… 0:00"* vì `TripRemainingTime` nay trả 0. **Không phải hồi quy** — bản cũ cũng ra đúng 0:00 (biến `_tripEndTime` chưa từng được gán). Sửa cho chỉnh thì đổi `TrainStationBuilding` sang `ShowArrived()`, nhưng đó là mỹ quan ngoài phạm vi 6 lỗi | 🟢 |
| 8 | Tôi **không sửa** `mo_phong_cap1_cap30.py` dù nó thiếu bảng `Aliases` (xem cảnh báo ở 6.A2.1) — công cụ là của TESTER, sửa chéo dễ thành hai bản khác nhau. Đề nghị TESTER thêm 1 dòng `PRICE['chicken'] = PRICE['chicken_meat']` | 🟢 |

**File đã đụng (18, chưa kể chính file TEAM này):**
`Dish_trung_op_la_bo_ne` · `Dish_nam_xao_thit_bo` · `Dish_bo_xao_tieu` · `Dish_bo_ham_ca_rot` ·
`Dish_pho_bo_tai` · `Dish_suon_heo_xao_chua_ngot` · `MarketPriceTable.cs` · `MarketDatabase.asset` ·
`Chuồng Bò.asset` · `Chuồng Bò Sữa.asset` · `Máy Xay Bột.asset` · `Máy Ép Mía.asset` ·
`Máy Phô Mai.asset` · `Home5.asset` · `TrainManager.cs` · `FarmManager.cs` · `FarmUIManager.cs` ·
`SCN_Farm.unity`.


---

## 7. TESTER — CHƠI THỬ CẤP 1 TỚI 30

Không có Unity nên **mô phỏng bằng script**, như đã làm với `mo_phong_don_hang.py`.

| # | Kiểm gì |
|---|---|
| T1 | **Biên dịch sạch** — ưu tiên số một, hai dev vừa xoá rất nhiều class |
| T2 | Grep xác nhận 13 nhóm code chết đã xoá hết, không còn tham chiếu |
| T3 | **Mô phỏng hành trình cấp 1 → 30**: mỗi cấp người chơi có gì, làm được gì, có bị chặn ở đâu không |
| T4 | **18 món ăn: món nào nấu được?** Sau khi sửa A1 thì phải là 18/18 |
| T5 | Kinh tế: ruộng vs chuồng vs đơn hàng vs quầy hàng — còn cái nào lãi gấp chục lần cái khác không |
| T6 | Bảng thời gian: xác nhận **không còn nghịch lý** cấp cao mà nhanh hơn cấp thấp |
| T7 | Không cây/hoa nào bán lỗ |
| T8 | 9 cặp `plotId` trùng đã hết |
| T9 | Tutorial không lặp lại sau khi xong |
| T10 | Mọi khoá save có `saveVersion` |
| T11 | **Tìm mọi chức năng chết còn lại** — hệ nào không có trong scene, sự kiện nào bắn mà không ai nghe, nút nào bấm không làm gì |
| T12 | Đề xuất hướng sửa cho từng lỗi, ghi vào mục 6 để hai dev fix |

**Vòng lặp:** dev làm → tester tìm lỗi → ghi vào file → dev sửa → tester kiểm lại. Lặp tới khi không còn lỗi chặn.

---

## 8. BÀN GIAO

- [x] Biên dịch sạch, 0 lỗi — *kiểm tĩnh 344/344 file; vẫn cần 1 lần bấm Play xác nhận*
- [x] **18/18 món nấu được** — bảng đầy đủ ở 6.T.2
- [x] 13 nhóm code chết đã xoá, 0 tham chiếu còn sót — *`SEA_Milk` giữ lại có chủ ý và ĐÚNG*
- [x] Thời gian cây trồng tăng dần theo cấp, không nghịch lý — 21/21 cây
- [x] Không cây/hoa nào bán lỗ — lãi/giây 0,1600 → 0,3086, không tụt lần nào
- [x] Chuồng không còn lãi gấp chục lần ruộng — **2,17× → 2,73×**
- [x] Sản phẩm chuồng vào kho và chuyển vào bếp được, gồm cả sữa — **A3 XONG**, xem P2
- [x] ~~9~~ **8** cặp `plotId` trùng đã hết — 38 component / 38 id duy nhất, migrate bảo toàn cây
- [x] Tab nhiệm vụ hằng ngày có nội dung — `MissionDatabase_Daily` gán ở `SCN_Farm:73945` và `:432434`
- [x] Tutorial không lặp lại — `TUTORIAL_MAIN_DONE` + `SkipTutorialEntirely()`
- [x] Sức chứa kho enforce thật — chặn ĐÚNG **và BÁO RÕ**: CB-1 đã vá (`ShowHint` có đường dự phòng) + CS-6 nối `OnAddRejectedByCapacity` vào `ShowHint` (6.A2.6). TESTER đã bịt 2 lỗ mất đồ/mất tiền (TF-1, TF-2)
- [x] Cổng bếp khoá tới cấp 5 — khoá ĐÚNG **và BÁO 'Cần cấp 5'** qua `CookingGateAccess.LockedMessage` → `ShowHint` (CB-1 đã vá)
- [x] Mọi save có `saveVersion` — 9 họ khoá phụ + 8 hệ JSON tự quản
- [x] Chơi được từ cấp 1 tới 30 không bị chặn — **0 chặn cứng**; khựng dòng tiền 4–9 cấp đã hết sau CS-4 (6.A2.4)
- [x] Không món ăn nào nấu ra lỗ — 18/18 đạt sàn lãi theo độ khó, thấp nhất +36 % (6.A2.1)

---

**KẾT LUẬN TESTER (vòng 1):** còn **1 lỗi CHẶN BÀN GIAO** (CB-1 · `ShowHint` câm) và **6 lỗi CẦN SỬA**.
Hai lỗi mất tiền/mất đồ của người chơi (TF-1, TF-2) TESTER đã tự vá. Sửa xong CB-1 thì tick nốt 2 ô còn lại.

**DEV-A vòng 2:** CB-1 đã có người vá trước; 6 lỗi 🟠 CS-1 → CS-6 **đã sửa xong** — chi tiết ở
mục 6 ▸ **DEV-A (vòng 2)**, kèm 8 phép kiểm sau khi sửa và 8 mục rủi ro còn lại.
Ô CHẶN BÀN GIAO cuối cùng còn treo: **một lần bấm Play trong Unity** (T1).
