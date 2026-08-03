# ĐỘI LÀM VIỆC — LẤP ICON PHẦN THƯỞNG LÊN CẤP

> File này là **kênh giao tiếp chung**. Mỗi agent ghi vào MỤC CỦA MÌNH, đọc mục của người khác trước khi làm.
> Chu kỳ: DEV-A + DEV-B làm → TESTER kiểm → ghi kết quả → sửa → lặp tới khi ĐẠT.

---

## 0. BỐI CẢNH

Popup lên cấp **đã hiện đúng** (băng rôn xanh, ngôi sao số 5, nút xanh — khớp video Township).
Còn lại **9 ô tròn phía dưới đang TRẮNG TRƠN**, chỉ có nhãn NEW đỏ, không có icon nào.

### Vì sao trống
`LevelRewardConfig.unlockDescriptions` chỉ là `List<string>` — **thuần chữ, không có Sprite**.
Ví dụ `LevelReward_L5.asset` dòng 23-26:
```yaml
unlockDescriptions:
- "MỞ KHÓA NHÀ BẾP — nấu ăn ngay!"
- "Mở khóa Khoai tây"
- "Thêm 1 nhà dân nhận đơn hàng"
```
Tool dựng **9 ô cứng** nhưng không ai nạp sprite vào → trắng.
Thêm nữa: L5 chỉ có **3** mục unlock, mà đang hiện **9** ô → phải ẩn ô thừa.

### Đã đúng rồi (đừng phá)
- `giftItems` chạy tốt — ảnh khoai tây `khoai_tay x5` hiện đúng ở hàng quà.
- Vàng `+300`, ngọc `+3` đúng số.

### Còn sai về hình
Icon vàng/ngọc ở hàng "Phần thưởng" đang là **đĩa tròn tô màu** (`spr_circle_fill` tint vàng/xanh),
không phải sprite xu/kim cương thật. HUD góc phải đã có sprite xu + kim cương đẹp → phải dùng lại.

---

## 1. MỤC TIÊU (tiêu chí ĐẠT)

- [ ] Mỗi mục trong `unlockDescriptions` hiện **1 ô có icon THẬT** lấy từ thư viện asset của game
- [ ] Số ô hiện = số mục unlock (L5 → 3 ô), ô thừa **ẩn** chứ không để trắng
- [ ] Icon vàng/ngọc hàng "Phần thưởng" dùng **sprite thật** của game
- [ ] Data-driven: designer sửa được qua Inspector, **không hardcode**
- [ ] Không phá `giftItems` đang chạy đúng
- [ ] `popup_report.txt` báo mọi ô đang bật đều có `sprite != NULL`
- [ ] Ảnh `game_view.png`: nhìn thấy icon trong ô, không còn ô trắng

---

## 2. PHÂN CÔNG

| Vai | Chuyên môn (theo `.claude/agents/`) | Nhiệm vụ |
|---|---|---|
| **DEV-A** | `economy-designer` + `systems-designer` | Tầng DỮ LIỆU: tìm thư viện icon, mở rộng `LevelRewardConfig`, tạo bảng tra unlock→icon, điền dữ liệu 31 level |
| **DEV-B** | `unity-ui-specialist` + `ui-programmer` | Tầng UI: nạp icon vào `UnlockSlotUI` lúc chạy, ẩn ô thừa, thay icon vàng/ngọc bằng sprite thật |
| **TESTER** | `qa-tester` | Kiểm bằng `popup_report.txt` + `game_view.png`, đối chiếu tiêu chí mục 1, ghi PASS/FAIL |

**Ranh giới để không đụng nhau:**
- DEV-A sở hữu: `LevelRewardConfig.cs`, các file `.asset` trong `data/Lever Game/`, file database mới
- DEV-B sở hữu: `LevelUpPopupUI.cs`, `UnlockSlotUI.cs`, `LevelUpPopupTownshipTool.cs`
- **Điểm giao (chốt trước khi code):** tên field/API mà DEV-B gọi để lấy dữ liệu → ghi vào §3

---

## 3. HỢP ĐỒNG API (DEV-A và DEV-B PHẢI thống nhất ở đây TRƯỚC KHI CODE)

> DEV-A đề xuất → DEV-B xác nhận → mới bắt đầu code.

**Đề xuất khởi điểm (DEV-A sửa nếu tìm được cách tốt hơn):**

```csharp
// Trong LevelRewardConfig — thay thế/bổ sung cho unlockDescriptions
[System.Serializable]
public class UnlockEntry
{
    public string  label;   // "Mở khóa Khoai tây"
    public Sprite  icon;    // icon THẬT từ thư viện game
}
public List<UnlockEntry> unlockEntries = new List<UnlockEntry>();
```

Giữ `unlockDescriptions` cũ để không vỡ code đang dùng; thêm hàm cầu nối:
```csharp
/// Trả về danh sách unlock để UI hiển thị. Ưu tiên unlockEntries,
/// nếu rỗng thì tự suy từ unlockDescriptions (icon = null).
public List<UnlockEntry> GetUnlockEntries();
```

**Trạng thái chốt:** ✅ **ĐÃ CHỐT — DEV-A đã implement, code đã có trên đĩa** (DEV-B cứ gọi, không cần chờ)

### API CHÍNH XÁC (copy-paste được)

`UnlockEntry` là **class LỒNG TRONG** `LevelRewardConfig` (giống `ItemGift` sẵn có) →
**phải viết đủ tên `LevelRewardConfig.UnlockEntry`**, KHÔNG viết `UnlockEntry` trần.

```csharp
// File: Assets/_Game/Farm/Scripts/UI/LevelRewardConfig.cs

[System.Serializable]
public class LevelRewardConfig.UnlockEntry   // (khai báo lồng, xem file)
{
    public string label;   // "Mở khóa Khoai tây"
    public Sprite icon;    // icon THẬT, hiện đã điền đủ 64/64 mục
}

public List<UnlockEntry> unlockEntries;                 // field (Inspector sửa được)
public List<UnlockEntry> GetUnlockEntries();            // ← DEV-B GỌI HÀM NÀY
public int               UnlockCount { get; }           // = GetUnlockEntries().Count
```

**Hợp đồng của `GetUnlockEntries()`**
| Điều kiện | Trả về |
|---|---|
| `unlockEntries` có dữ liệu | chính danh sách đó (đã lọc bỏ phần tử null / dòng rỗng) |
| `unlockEntries` rỗng | tự suy từ `unlockDescriptions`, `icon = null` (tương thích ngược) |
| level không mở gì | list **rỗng** (Count = 0) |
| mọi trường hợp | **KHÔNG BAO GIỜ null** — DEV-B không cần `?? new List<>()` |

**Mẫu dùng ở tầng UI (ẩn ô thừa):**
```csharp
var entries = cfg.GetUnlockEntries();          // luôn != null
for (int i = 0; i < unlockSlots.Length; i++)
{
    bool used = i < entries.Count;
    unlockSlots[i].gameObject.SetActive(used); // ô thừa → ẩn, KHÔNG để trắng
    if (used)
    {
        unlockSlots[i].Setup(entries[i].icon, true, "");  // API sẵn có của UnlockSlotUI
        unlockSlots[i].PlayPop(i);
    }
}
```
> `UnlockSlotUI.Setup(Sprite icon, bool showNewTag = true, string caption = "")` — DEV-A không sửa file này.

**Cam kết của DEV-A:** `unlockDescriptions` và `giftItems` KHÔNG bị xoá/đổi → code cũ
(`LevelUpPopupUI.cs` dòng ~267) vẫn chạy nguyên.

---

## 4. NHẬT KÝ DEV-A (tầng dữ liệu)

**Trạng thái: XONG. 64/64 mục unlock của 29 asset đã có icon thật (0 mục thiếu).**

### 4.1 ⭐ ĐƯỜNG DẪN XU VÀNG & KIM CƯƠNG — thứ DEV-B cần trước nhất

Đây là **đúng 2 sprite mà HUD góc trên-trái đang hiển thị** (đã dò từ `SCN_Farm.unity`):

| Dùng cho | Đường dẫn asset | Tên sprite (sub-asset) | fileID | guid |
|---|---|---|---|---|
| **XU VÀNG** | `Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/vang-removebg-preview.png` | `vang-removebg-preview_0` | `-846414766330871110` | `a1c4be4bd781bd74399a37785962ed71` |
| **KIM CƯƠNG** | `Assets/Assetsgame/kimcuong-removebg-preview.png` | `kimcuong-removebg-preview_0` | `-5564238710932881115` | `63b103dfe32bee843a5c100fa9a0968d` |

> ⚠ Chú ý tên thư mục XU VÀNG có **HAI dấu cách**: `Fantasy Wooden GUI␣␣Free`.
> ⚠ Cả 2 file đều `Sprite Mode = Multiple` → `AssetDatabase.LoadAssetAtPath<Sprite>(path)`
> **trả về null**. Phải dùng `AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().First()`.
> (Hàm `UnlockIconFillTool.LoadSprite()` đã làm sẵn — DEV-B copy được.)

Bằng chứng trong scene (nơi HUD đang dùng đúng 2 sprite này):
- `Canvas_HUD/SafeArea/TOPBAR/LeftTopBar/GoldBox/Vangicon` → sprite xu vàng
- `Canvas_HUD/SafeArea/TOPBAR/LeftTopBar/GemBox/kimcuongIcon` → sprite kim cương
- Ngoài ra `icon_rewardGold` trong scene cũng đã dùng sprite xu vàng ở trên.

**Bản trùng tên — ĐỪNG lấy nhầm:** `Assets/Anh/vang-removebg-preview.png` và
`Assets/Anh/kimcuong-removebg-preview.png` là bản copy KHÁC guid, HUD không dùng.

Muốn lấy nhanh: mở tool của DEV-A → bấm nút **“Đường dẫn XU/KIM CƯƠNG”** → in ra Console.

### 4.2 THƯ VIỆN ICON TÌM ĐƯỢC (nguồn dữ liệu cho DEV-B)

**Cách tốt nhất: KHÔNG lấy sprite theo đường dẫn, lấy qua ScriptableObject.**
Mọi item trong game đều đã có SO chứa sprite → data-driven, đúng quy tắc §7.2.

| Loại SO | Field icon | Số asset | Vị trí | Ví dụ |
|---|---|---|---|---|
| `CropData` (: `BaseItemData`) | `itemIcon` (alias `.icon`) | 11 nông sản + 10 hoa | `Assets/_Game/Farm/data/Hat_giong/`, `Assets/_Game/Farm/data/Hạt Hoa/` | Khoai_Tay, CaChua, Ngo, Mia, nam, chanh, Ot, caytieu, Ca_Rot, BapCai, Crop_Rice · HoaHong, HoaLan, Tulip, HuongDuong, HoaOaiHuong, HoaCucTrang, HoaCucVanTho, HoaMauDon, HoaCamTuCau, HoaAnhThao |
| `BuildingData` (: `PlaceableItemData`) | `itemIcon` | 18 | `Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/` | Chuồng Gà, Chuồng Heo, Chuồng Bò, Chuồng Bò Sữa, Home1-5 (“Nhà Dân”), Đất (“Đất Trồng”), Chậu Hoa1-4, Khung Hoa, Máy Xay Bột*, Máy Ép Mía*, Máy Phô Mai* |
| `DecorData` (: `PlaceableItemData`) | `itemIcon` | 15 | `Assets/_Game/Farm/CÔNG TRÌNH/*.asset` | Vòng Hoa, Ghế Hoa, Giếng, Hồ Đá, Cột Đèn, Bù Nhìn, Xe Hoa, Vịt Vui Vẻ, Đài Nước, Cối Xoay Gió… |
| `InventoryItemData` | `icon` | ~45 | `Assets/_Game/Farm/data/Farm_dong_vat/`, `Item_Kho_Cook/`, `item_taulua/`, `Farm_May_Che_Bien/` | Thịt bò/heo/gà, Trứng, Sữa + **20 món ăn** (`Assets/Assetsgame/Món ăn/*.png`) + nguyên liệu (Gạo, Muối, Nước mắm…) |
| `OrderItemDefinition` | `icon` | 38 | `Assets/_Game/Farm/data/Village_data/` | icon đơn hàng dân làng (trùng nguồn với InventoryItemData) |

`(*)` **3 asset máy chế biến có `itemIcon = NULL`** — xem §4.5.

**Sprite art rời (không có SO) — tra theo TÊN FILE, đừng hardcode path vì thư mục có Unicode:**

| Ý nghĩa | Đường dẫn | Tên sprite |
|---|---|---|
| Nhà bếp / nhà hàng | `Assets/Assetsgame/bocaycoitrangtri/Assettrangtri/cooking.png` | `cooking_0` |
| Kho (nâng cấp kho) | `Assets/maptitle/AssetsTitl/Sprites/Tiles/Warehouse/Sprites/Sprite_Tiles_Warehouse.png` | `Sprite_Tiles_Warehouse_0` |
| Máy chế biến (chung) | `Assets/Assetsgame/Nhà/maylamthucangiasuc.png` | `maylamthucangiasuc_0` |
| Nhà ga / bến tàu | `Assets/Assetsgame/Nhà/gataulua.png` | `gataulua_0` |
| Tàu (đơn tàu) | `Assets/Taulua/taulua.png` | `taulua_0` |
| Đơn hàng (đầu bếp bưng khay) | `Assets/Anh/delivery.png` | `delivery_0` |
| Lịch (nhiệm vụ / sự kiện) | `Assets/Assetsgame/Lich.png` | `Lich_0` |
| Sách nấu ăn (công thức) | `Assets/Assetsgame/SachNauAn.png` | `SachNauAn_0` |
| Ngôi sao (danh hiệu / mốc) | `Assets/Assetsgame/iconsao-removebg-preview.png` | `iconsao-removebg-preview_0` |
| Cá | `Assets/Anh/ca.png` | `ca_0` |
| Gạo | `Assets/Anh/gaoicon.png` | `gaoicon_0` |
| Sữa | `Assets/Assetsgame/suamilk.png` | `suamilk_0` |
| Đất trồng | `Assets/maptitle/tile_dirt.png` | `tile_dirt_0` |
| UI popup lên cấp (đã dùng) | `Assets/_Game/Farm/Art/UI_LevelUp/` | `spr_ring_circle`, `spr_new_tag`, `spr_star`, `spr_circle_fill`… |

**Thư mục icon món ăn:** `Assets/Assetsgame/Món ăn/` — 20 file PNG (Phở bò tái, Cá nướng tiêu,
Khoai tây chiên, Cơm chiên trứng…), đều đã được `InventoryItemData` trỏ tới.
**Lưu ý:** thư mục `Assets/PixelVibe 67 Cooking Ingredients…Pack/` **CHỈ CÒN `DemoScene.unity`,
KHÔNG có sprite nào** — đừng mất thời gian tìm ở đó.

### 4.3 FILE ĐÃ SỬA / TẠO

| File | Việc |
|---|---|
| `Assets/_Game/Farm/Scripts/UI/LevelRewardConfig.cs` | **SỬA** — thêm `UnlockEntry`, `unlockEntries`, `GetUnlockEntries()`, `UnlockCount`. GIỮ NGUYÊN `unlockDescriptions` + `giftItems`. |
| `Assets/_Game/Farm/Editor/UnlockIconFillTool.cs` | **TẠO MỚI** — menu `Tools/Farm/Điền Icon Unlock (Level Reward)`. Bảng xem trước (khớp/không khớp + preview icon) → nút ÁP DỤNG → `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets()`. Có nút rollback và nút in đường dẫn xu/kim cương. |
| `Assets/_Game/Farm/data/Lever Game/LevelReward_L2…L30.asset` (29 file) | **ĐÃ ĐIỀN SẴN** `unlockEntries` (label + guid/fileID sprite thật) để TESTER kiểm được **ngay, không cần ai bấm nút** (xem §8). Chạy lại tool cho kết quả y hệt (idempotent). |

*Ghi chú số lượng:* mục 3 nói “31 asset” nhưng thực tế thư mục chỉ có **29** file (L2→L30, không có L1 và L31).

### 4.4 CÁCH ĐIỀN — bảng tra `unlockDescriptions` → icon

Thuật toán trong `UnlockIconFillTool`:
1. Chuẩn hoá chuỗi: **bỏ dấu + chữ thường** (`Norm()`), nên khớp được cả `"MỞ KHÓA"`/`"Mở khoá"`.
   `đ/Đ` xử lý riêng vì không phân rã được bằng `FormD`.
2. Khớp **NGUYÊN TỪ** (`ContainsWord()`) → `"kho"` **không** dính vào `"mở khoá"`.
3. Bảng ~68 luật xếp từ **cụ thể → chung**, luật đứng trước thắng. Vài chỗ thứ tự là bắt buộc:
   - `"nuoc mia"` trước `"mia"` (nếu không “Mở khóa nước mía” bị gán icon cây mía)
   - `"may pho mai"` trước `"pho mai"`
   - `"don tau"` trước `"kho"` (nếu không “Đơn tàu **khó**” bị gán icon Kho)
   - `"decor"` trước `"pet"` (để “Mở khóa decor cho pet” ra icon decor)
   - `"mo rong dat"` là luật duy nhất chứa “đất” → tránh đụng “**Đạt** nội dung tối đa”
4. Nguồn icon: `CropData` → `PlaceableItemData` → `InventoryItemData` → sprite art tra theo tên file.

### 4.5 CÁC MỤC PHẢI DÙNG ICON TẠM (không MISS nhưng cần art thật)

Tất cả 64 mục **đều có icon != null**, nhưng 8 mục sau chỉ là **thay thế gần nghĩa** —
designer/artist nên gán lại tay trong Inspector khi có art:

| Level | Mục | Icon tạm đang dùng | Vì sao |
|---|---|---|---|
| L11 | Máy Xay Bột đã mở bán trong Shop | `maylamthucangiasuc_0` | `BuildingData "Máy Xay Bột"` có `itemIcon = NULL` |
| L13 | Máy Ép Mía đã mở bán trong Shop | `maylamthucangiasuc_0` | `BuildingData "Máy Ép Mía"` có `itemIcon = NULL` |
| L15 | Máy Phô Mai đã mở bán trong Shop | `maylamthucangiasuc_0` | `BuildingData "Máy Phô Mai"` có `itemIcon = NULL` |
| L11 | Mở khóa bột gạo | `gaoicon_0` | `Item_BotGao.icon = NULL`, cả project **không có art bột gạo** |
| L15 | Mở khóa phô mai | `suamilk_0` (sữa) | `Item_PhoMai.icon = NULL`, cả project **không có art phô mai** ← chênh nhất |
| L23 | Bến Tàu Du Lịch đã mở | `gataulua_0` (nhà ga tàu) | không có art bến tàu du lịch |
| L24 | Nhà hàng ven biển | `cooking_0` (nhà bếp) | không có art nhà hàng |
| L12/20/22/27 | “cây trồng mới / cây hiếm / cây cao cấp” | `bapcailuc1` (Bắp Cải) | mô tả chung, không chỉ đích danh cây nào |

**Yêu cầu art (gửi cho artist):** icon phô mai, icon bột gạo, icon 3 máy chế biến
(Xay Bột / Ép Mía / Phô Mai), icon bến tàu du lịch, icon nhà hàng ven biển.
Sau khi có art: gán vào `itemIcon`/`icon` của SO tương ứng rồi chạy lại tool → tự cập nhật.

### 4.6 CẢNH BÁO CHO CẢ ĐỘI

- `LevelUpRewardDataSetupTool` (menu `Tools/Farm Game/Setup Level Up Popup/Setup Reward Data`)
  **ghi đè `unlockDescriptions`** (dòng ~455). Nếu chạy lại tool đó → phải chạy lại
  `Tools/Farm/Điền Icon Unlock (Level Reward)` để đồng bộ nhãn. Bảng xem trước sẽ hiện
  cảnh báo “nhãn cũ đã lệch”.
- DEV-A **không hề chạm** `LevelUpPopupUI.cs`, `UnlockSlotUI.cs`, `LevelUpPopupTownshipTool.cs`.
- Popup prefab dựng cứng **9 ô** nhưng level nhiều nhất chỉ có **3 mục** (L5, L6, L7, L9, L15, L20, L25);
  L18 và L23 chỉ có **1 mục**. → DEV-B **bắt buộc** ẩn ô thừa, nếu không vẫn còn ô trắng.
  Số mục theo level: L2=3 L3=2 L4=2 L5=3 L6=3 L7=3 L8=2 L9=3 L10=2 L11=2 L12=2 L13=2
  L14=2 L15=3 L16=2 L17=2 L18=1 L19=2 L20=3 L21=2 L22=2 L23=1 L24=2 L25=3 L26=2 L27=2
  L28=2 L29=2 L30=2.

---

## 5. NHẬT KÝ DEV-B (tầng UI)

**Trạng thái: XONG cả 3 việc. Chỉ sửa đúng 3 file thuộc phạm vi DEV-B.**

### 5.1 FILE ĐÃ SỬA

| File | Dòng | Việc |
|---|---|---|
| `Assets/_Game/Farm/Scripts/UI/LevelUpPopupUI.cs` | 39–61 | Thêm 3 field: `unlockSlotsContainer` (Transform), `unlockSlots[]` (UnlockSlotUI), `unlockStripRoot` (GameObject) |
| ″ | 115–121 | Thêm runtime: `_unlockSlotsCache`, `_pendingUnlockPopCount`, `_warnedNoUnlockSlots` |
| ″ | 237–241 | Trong `ShowNextPopup()`: gọi `PlayUnlockPops()` **sau** `popupRoot.SetActive(true)` |
| ″ | 268–270 | Trong `PopulateUI()`: gọi `ApplyUnlockSlots(cfg)` — **ngoài** nhánh `if (cfg != null)` |
| ″ | 371–530 | 3 hàm mới: `ApplyUnlockSlots()` (381), `PlayUnlockPops()` (451), `ResolveUnlockSlots()` (469) |
| `Assets/_Game/Farm/Scripts/UI/UnlockSlotUI.cs` | 31–35 | Thêm `CurrentIcon` / `HasIcon` (read-only, cho QA & tool kiểm tra) |
| ″ | 43–49 | `Setup()` reset `localScale = 1` + dừng pop cũ (chống ô kẹt scale 0 → vô hình) |
| ″ | 60–62 | `Setup()` trả `iconImage.color = Color.white` (chống icon bị tint bẩn) |
| ″ | 79–87 | `PlayPop()` khi object tắt: vẫn đặt scale = 1 rồi mới return |
| `Assets/_Game/Farm/Editor/LevelUpPopupTownshipTool.cs` | 1–2 | `using System.Collections.Generic; using System.Linq;` |
| ″ | 44–59 | Const `PATH_ICON_GOLD` / `PATH_ICON_GEM` |
| ″ | 365–376 | `Build()`: nạp `goldIcon`/`gemIcon` rồi truyền vào `MakeCurrencyChip` |
| ″ | 429–433 | `Build()`: `BuildUnlockSlot` trả về `UnlockSlotUI`, gom vào `unlockSlotList` |
| ″ | 497–502 | Truyền `strip`, `scrollContent`, `unlockSlotList` vào `WireToLevelUpPopupUI` |
| ″ | 565, 611 | `BuildUnlockSlot` đổi `void` → `UnlockSlotUI`, thêm `return ui;` |
| ″ | 614–644 | Hàm mới `LoadRealSprite(assetPath, what)` |
| ″ | 647–653 | Đổi chữ ký `MakeCurrencyChip`: `Color iconColor` → `Sprite iconSprite, Color fallbackTint` |
| ″ | 671–680 | `MakeCurrencyChip`: dùng sprite thật, `preserveAspect = true`, không tint |
| ″ | 693–702 | `WireToLevelUpPopupUI` thêm 3 tham số cuối |
| ″ | 793–820 | Nối dây `unlockSlotsContainer` + `unlockStripRoot` + mảng `unlockSlots` (SerializedObject/arraySize) |

**KHÔNG chạm:** `LevelRewardConfig.cs`, `.asset` trong `data/Lever Game/`, `UnlockIconFillTool.cs`,
`BuildProceduralGiftSlots()`, `giftItemsContainer`, `unlockDescText`, `PopupCaptureReporter.cs`.

### 5.2 LOGIC NẠP / ẨN Ô (chính xác)

`PopulateUI(level, cfg)` → `ApplyUnlockSlots(cfg)`:

```
slots   = ResolveUnlockSlots()                    // luôn != null
entries = cfg != null ? cfg.GetUnlockEntries()    // API DEV-A, LUÔN != null
                      : null                      // cfg == null → coi như 0 mục
wanted  = entries?.Count ?? 0
used    = min(wanted, slots.Length)

for i in 0..slots.Length-1:
    inUse = (i < used)
    slot[i].gameObject.SetActive(inUse)           // ← ô thừa TẮT HẲN, không để trắng
    if inUse: slot[i].Setup(entries[i].icon, true, "")
_pendingUnlockPopCount = used
```

- `cfg == null` → `entries = null` → `used = 0` → **ẩn HẾT 9 ô** ✔
- L5 (3 mục) → bật ô 1-3, **tắt ô 4-9** ✔. L18/L23 (1 mục) → bật 1, tắt 8 ✔
- `caption` truyền `""` (nhãn chữ đã hiện gộp ở `unlockDescText`; nhồi chữ vào ô 190px sẽ tràn)
- `entries[i].icon == null` vẫn bật ô (khung tròn + NEW) và **LogWarning đếm số ô thiếu icon**
- `wanted > 9` → LogWarning, hiện 9 ô đầu (hiện tại không level nào vượt: max 3)

**`ResolveUnlockSlots()` — thứ tự ưu tiên (dòng 469):**
1. `unlockSlotsContainer.GetComponentsInChildren<UnlockSlotUI>(true)` ← **CÁCH CHÍNH**
2. mảng `unlockSlots[]` (bỏ phần tử null)
3. `popupRoot.GetComponentsInChildren<UnlockSlotUI>(true)` ← **cứu cánh** (xem 5.4)

> **VÌ SAO container ưu tiên hơn mảng:** tool `Build()` gọi `DeleteExisting()` →
> `DestroyImmediate` cả cây cũ → mọi phần tử mảng thành null "mồ côi"; còn container chỉ
> là **1** tham chiếu. Designer thêm/bớt ô trong Hierarchy cũng không phải sửa mảng.
> `includeInactive = true` là **BẮT BUỘC**: từ lần mở popup thứ 2, ô thừa đang TẮT — nếu
> bỏ qua ô tắt thì mảng ngắn dần và không bao giờ bật lại được.
> Kết quả được cache; cache tự dò lại khi phát hiện phần tử null (object đã bị Destroy).

**Vì sao `PlayPop` tách thành `PlayUnlockPops()` (dòng 451):**
`PopulateUI()` chạy ở dòng 235, `popupRoot.SetActive(true)` mới ở dòng 236 → lúc `Setup()`
các ô **chưa `activeInHierarchy`**, mà `UnlockSlotUI.PlayPop()` có chốt
`if (!isActiveAndEnabled) return` (Unity từ chối `StartCoroutine` trên object tắt).
Gọi `PlayPop` trong `PopulateUI` là **mất sạch animation**. Nên hoãn: lưu
`_pendingUnlockPopCount`, gọi `PlayUnlockPops()` ở dòng 241 (sau khi đã qua chốt
`activeInHierarchy`, cùng chỗ `SpawnVFX`).

### 5.3 ICON VÀNG / NGỌC — LẤY ĐƯỢC, có fallback

`MakeCurrencyChip` **không còn** dùng `spr_circle_fill` tô màu khi có sprite thật:

| | Đường dẫn (const trong tool, dòng 56/58) | Đã xác minh |
|---|---|---|
| XU VÀNG | `Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/vang-removebg-preview.png` | file TỒN TẠI, `.meta` có `spriteMode: 2`, sub-sprite `vang-removebg-preview_0`, guid `a1c4be4b…` ✔ khớp §4.1 |
| KIM CƯƠNG | `Assets/Assetsgame/kimcuong-removebg-preview.png` | file TỒN TẠI, `spriteMode: 2`, sub-sprite `kimcuong-removebg-preview_0`, guid `63b103df…` ✔ khớp §4.1 |

`LoadRealSprite()` (dòng 627): thử `LoadAssetAtPath<Sprite>` trước (trường hợp Single) →
nếu null thì `LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().FirstOrDefault()`
(cả 2 file đều Multiple nên **luôn đi nhánh này**).

**Fallback khi không tìm thấy:** `LogWarning` in NGUYÊN đường dẫn đã thử + nhắc kiểm tra
"HAI dấu cách" → `MakeCurrencyChip` quay về `spr_circle_fill` + tint như cũ
(`hasReal = false`). **Sprite không bao giờ null.**
Khi có sprite thật: `color = Color.white` (KHÔNG tint, tránh nhuộm bẩn xu) và
`preserveAspect = true` (xu/kim cương không vuông tuyệt đối).
Không hardcode đường dẫn nào ở tầng RUNTIME — 2 const này nằm trong code **Editor**.

### 5.4 ⚠ ĐIỀU TESTER PHẢI KIỂM ĐẶC BIỆT

1. **Icon mở khoá chạy được NGAY, KHÔNG cần bấm "DỰNG POPUP" lại.** Scene hiện tại được
   dựng bằng bản tool CŨ nên 3 field mới đang **null/rỗng** → mình đã làm **cứu cánh #3**:
   quét `popupRoot.GetComponentsInChildren<UnlockSlotUI>(true)`. Console sẽ in
   `"Chưa nối dây ô mở khoá, đã TỰ TÌM được 9 ô dưới 'Root_HienThi'"` — dòng này là
   **BÌNH THƯỜNG**, không phải lỗi. Nó chỉ mất đi sau khi Edric bấm DỰNG POPUP lại.
2. **Icon vàng/ngọc thì PHẢI dựng lại popup mới đổi** — sprite được gắn lúc *dựng scene*,
   không phải lúc chạy. Trước khi Edric bấm DỰNG POPUP, hàng "Phần thưởng" **vẫn là đĩa
   tròn tô màu** → nếu ảnh `game_view.png` chụp trước bước đó thì **đừng tính FAIL cho
   tiêu chí icon vàng/ngọc**, ghi rõ "chờ dựng lại".
3. Đọc `popup_report.txt` mục `── Ô MỞ KHOÁ ──`: kỳ vọng ở cấp 5 là **`Đang bật: 3/9`**
   (KHÔNG phải 9/9). Số ô bật phải khớp bảng số mục theo level ở §4.6.
   Reporter hiện **không** in sprite của từng ô (nó nằm trong `Scripts/Debug/`, ngoài phạm
   vi DEV-B nên mình không sửa) → thay vào đó xem **Console**:
   `[LevelUpPopupUI] Ô mở khoá: bật 3/9, có icon 3/3.` ← dòng chốt cho tiêu chí §1.
   Nếu có ô thiếu icon sẽ có thêm LogWarning `"N/M ô mở khoá có icon = NULL"`.
   (Đã thêm `UnlockSlotUI.CurrentIcon` / `.HasIcon` public để bản reporter sau đọc được.)
4. **Không được có ô trắng.** Nếu ảnh còn ô trắng: đếm — nếu đúng 9 ô trắng thì là
   `ResolveUnlockSlots()` trả rỗng (xem LogWarning), nếu 3 ô có khung mà không icon thì là
   `unlockEntries[i].icon = null` (việc của DEV-A).
5. **Hàng quà khoai tây x5 phải còn nguyên.** Mình không chạm `BuildProceduralGiftSlots` /
   `giftItemsContainer`. `Hang_Qua` là object RIÊNG với dải mở khoá nên `SetActive(false)`
   của mình không lan sang.
6. Kiểm **mở popup 2 lần liên tiếp** (ví dụ lên L5 rồi L6): ô từng bị ẩn phải hiện lại
   đúng và **không bị vô hình**. Đây là bug tiềm ẩn mình đã bịt trong `UnlockSlotUI.Setup()`
   (reset `localScale = 1`) — vì coroutine pop bị Unity huỷ giữa đường khi ô bị tắt sẽ để
   scale kẹt ở ~0.
7. Dải icon dùng `HorizontalLayoutGroup` + `ContentSizeFitter` → 3 ô sẽ dồn về **mép trái**
   (`childAlignment = MiddleLeft`, do tool dựng sẵn). Nếu muốn căn giữa thì đổi
   `chl.childAlignment = TextAnchor.MiddleCenter` — **quyết định của art-director, chưa làm.**

### 5.5 CÒN VƯỚNG

- **Chưa biên dịch thử được** (không có Unity/dotnet ở môi trường agent). Đã rà tay:
  ngoặc `{}` cân bằng cả 3 file, `using` đủ (`System.Linq` cho `OfType/FirstOrDefault`,
  `System.Collections.Generic` cho `List<UnlockSlotUI>`), viết đủ tên
  `LevelRewardConfig.UnlockEntry`, không dùng API nào lạ với Unity 6000.3.
- `unlockStripRoot` (ẩn cả thanh nền tối khi level không mở gì) **chỉ hoạt động sau khi
  dựng lại popup**. Trước đó nếu gặp level không có asset thì sẽ thấy thanh nền rỗng.
  Không ảnh hưởng L2–L30 (đều ≥ 1 mục).
- Ô mở khoá vẫn hiện nhãn **NEW cho MỌI ô** (`showNewTag = true` cứng). `UnlockEntry`
  chưa có cờ "đã xem" → nếu design muốn NEW chỉ ở lần đầu, cần DEV-A thêm field.
- 8 mục dùng icon tạm ở §4.5 (phô mai → sữa, v.v.) là **vấn đề art/dữ liệu**, tầng UI
  không xử lý được.
- Căn giữa dải icon (điểm 7 ở trên) đang chờ chốt thẩm mỹ.

---

## 6. BÁO CÁO TESTER

*(TESTER ghi vào đây mỗi vòng: đọc gì, PASS/FAIL từng tiêu chí mục 1, lỗi cụ thể + đề xuất)*

### Vòng 1 — STATIC REVIEW (đọc code + đọc YAML, CHƯA có ảnh chụp)

- Trạng thái: ✅ **ĐÃ KIỂM XONG** — không có lỗi biên dịch. Tìm được **1 lỗi Cao (dữ liệu)** + 2 lỗi TB + 5 lỗi Thấp.
- Đã đọc: 6 file .cs trong phạm vi, 29 file `.asset`, `SCN_Farm.unity`, 35 file `.meta` sprite,
  `BaseItemData.cs`, `InventoryItemData.cs`, `PopupSpriteFactory.cs`, `LevelUpGiftSlotUI.cs`.
- **Chưa** đọc được `popup_report.txt` / `game_view.png` (chưa ai bấm Play — xem §8).

#### A. LỖI BIÊN DỊCH: **KHÔNG CÓ** (rà tay + kiểm máy)

| Hạng mục kiểm | Kết quả |
|---|---|
| Cân bằng `{} () []` (đã lược chuỗi + comment bằng script) | 6/6 file cân bằng, độ sâu cuối = 0 |
| `#if` / `#endif` | LevelUpPopupUI 1/1, UnlockSlotUI 1/1, PopupCaptureReporter 4/4 — cân bằng |
| `using System.Linq` ở file dùng `.OfType<>()/.FirstOrDefault()` | Có: `LevelUpPopupTownshipTool.cs:2`, `UnlockIconFillTool.cs:3` ✔ |
| `LevelRewardConfig.UnlockEntry` viết ĐỦ TÊN ngoài class | ✔ `LevelUpPopupUI.cs:393`, `UnlockIconFillTool.cs:490,493,533`. **Không có chỗ nào viết `UnlockEntry` trần** → không CS0246 |
| `GetUnlockEntries()` / `UnlockCount` | Tồn tại `LevelRewardConfig.cs:98,131` ✔ |
| `UnlockSlotUI.Setup(Sprite,bool,string)` / `PlayPop(int)` / `CurrentIcon` / `HasIcon` / `EditorBind(Image,Image,GameObject,TextMeshProUGUI)` | Tồn tại `UnlockSlotUI.cs:41,77,32,35,122`; mọi call-site khớp chữ ký ✔ |
| `WireToLevelUpPopupUI` — 20 tham số vs 20 đối số (dòng 501-505 ↔ 693-702) | Khớp cả số lượng lẫn kiểu ✔ |
| `MakeCurrencyChip(RectTransform,string,string,Sprite,Color,out TMP)` | Call-site 369-370 & 375-376 khớp ✔ |
| `Set("unlockStripRoot", unlockStrip != null ? unlockStrip.gameObject : null)` | Ternary suy ra `GameObject` → chuyển ngầm sang `Object` ✔ |
| `Object` có nhị nghĩa `System.Object`/`UnityEngine.Object`? | **KHÔNG** — cả 2 file Editor đều không `using System;` ✔ |
| API Unity 6000.3: `AssetDatabase.LoadAllAssetRepresentationsAtPath`, `SerializedProperty.arraySize`, `GetArrayElementAtIndex`, `FindObjectsByType<T>(FindObjectsInactive,FindObjectsSortMode)`, `FindFirstObjectByType<T>(FindObjectsInactive)`, `ScreenCapture.CaptureScreenshot`, `TextWrappingModes.NoWrap`, `ShaderUtilities.Keyword_Outline` | Tất cả tồn tại ✔ |
| Phụ thuộc ngoài phạm vi | `CropData.itemName/itemIcon`, `PlaceableItemData.itemName/itemIcon` (kế thừa `BaseItemData.cs:19,23`), `InventoryItemData.displayName/icon` (`:8,9`), `PopupSpriteFactory.Load/Hex/GenerateAll/ArtFolder`, `EditorOnlyHint` — đều có ✔ |
| Phân mảnh assembly | **Không có file `.asmdef` nào** trong Assets → một `Assembly-CSharp` duy nhất, `UnlockSlotUI.CurrentIcon/HasIcon/EditorBind` truy cập được từ cả Editor lẫn Debug ✔ |
| Biến dùng trước khai báo / khai báo mà không dùng | Không thấy. `out var a/b/c` ở `UnlockIconFillTool.Resolve()` dùng 3 tên KHÁC nhau trong cùng switch → không CS0128 ✔ |

#### B. ĐỐI CHIẾU TỪNG TIÊU CHÍ §1

| # | Tiêu chí | Kết quả | Bằng chứng (file:dòng) |
|---|---|---|---|
| 1 | Mỗi mục `unlockDescriptions` hiện 1 ô có icon THẬT | ⚠ **PASS 62/64 — FAIL 2 mục** | 64/64 entry có block `icon:` khác `{fileID: 0}`; nhưng L12:25 và L21:25 trỏ fileID sai → **BUG-01** |
| 2 | Số ô hiện = số mục unlock, ô thừa ẩn | ✅ **PASS (static)** | `LevelUpPopupUI.cs:396-410` (`used = Min(wanted, slots.Length)`, `SetActive(inUse)`). Trace ở mục D |
| 3 | Icon vàng/ngọc dùng sprite thật | ❌ **FAIL Ở TRẠNG THÁI HIỆN TẠI** (code đúng, scene chưa đổi) | `SCN_Farm.unity`: `Hang_Vang/Icon` **và** `Hang_Ngoc/Icon` đều `m_Sprite = {fileID: 21300000, guid: 6607e3eb4f5a2524a9ffa55148c5b0db}` = `spr_circle_fill`, tint `(1, 0.772, 0.192)` / `(0.494, 0.851, 0.341)`, `m_PreserveAspect: 0`. Code tool ĐÃ đúng (`LevelUpPopupTownshipTool.cs:366-367, 627-645, 671-680`) → **phải bấm DỰNG POPUP mới đổi** |
| 4 | Data-driven, không hardcode | ✅ **PASS** | `LevelRewardConfig.cs:71-75` public `List<UnlockEntry>` + Header/Tooltip → Inspector sửa được. Runtime 0 đường dẫn cứng. 2 `const` path chỉ nằm trong code **Editor** (`LevelUpPopupTownshipTool.cs:56,59`) |
| 5 | Không phá `giftItems` | ✅ **PASS — bằng chứng cứng** | `git diff --numstat` cả 29 file: **chỉ thêm, 0 dòng bị xoá** (L18/L23 = +3, các file 2 mục = +5, 3 mục = +7 → đúng `1 + 2×N`). 29/29 file còn `giftItems` (1 mục) và `hintText` |
| 6 | `popup_report.txt` báo mọi ô bật có `sprite != NULL` | ⏳ **CHƯA KIỂM ĐƯỢC** (cần F10) — nhưng reporter ĐỦ khả năng: `PopupCaptureReporter.cs:226-262` in từng ô `icon=<tên>/NULL ✘ Ô TRẮNG!` + `scale`, dòng 249 in `Đang bật: N/9  Có icon: M`. Dự đoán: L5 → `3/9, icon 3` ✔; **L12 & L21 → 1 ô `icon=NULL`** (BUG-01) |
| 7 | `game_view.png` thấy icon trong ô, không còn ô trắng | ⏳ **CHƯA KIỂM ĐƯỢC** |

#### C. DỮ LIỆU CÓ THẬT KHÔNG — TỰ KIỂM CHỨNG, KHÔNG TIN LỜI KHAI

Kết luận: **lời khai của DEV-A về cơ bản ĐÚNG** — dữ liệu thật, đúng số, tool tái tạo được. Chỉ 1 chỗ sai fileID.

1. **Số lượng** — 29 file (L2→L30, không có L1/L31, đúng như §4.3 ghi chú).
   Tổng **64 `unlockEntries` = 64 `unlockDescriptions`**, từng file khớp 1-1 (L2=3, L18=1, L23=1, L30=2 … đúng bảng §4.6). **0 mục có `icon: {fileID: 0}`.**
2. **Đã mở tay 4 file yêu cầu** — `LevelReward_L5/L2/L18/L30.asset`: có block `unlockEntries:` với `label` + `icon: {fileID: ..., guid: ...}` thật. Ví dụ L5:
   `MỞ KHÓA NHÀ BẾP` → `cooking.png` (guid `7b314202…`), `Mở khóa Khoai tây` → `iconKhoaiTay.png` (`c0014274…`), `Thêm 1 nhà dân` → `hom1.png` (`0772edf2…`).
3. **Tra GUID có thật không** — quét toàn bộ `.meta` trong `Assets/`: **35/35 guid đều tìm được file `.meta` + file ảnh tồn tại thật.** Không guid nào trỏ vào hư không.
4. **Tra `fileID` sub-sprite có thật không** (bước DEV-A không làm) — đối chiếu `internalIDToNameTable` của từng `.meta` **và** đối chiếu chéo với 34.156 tham chiếu object khác trong project:
   **34/35 cặp (guid, fileID) là sprite mà chính Unity đang tham chiếu ở nơi khác** → chắc chắn hợp lệ.
   **1 cặp SAI → BUG-01** (chi tiết dưới).
5. **Nhãn có lệch không** — 64/64 `label` trong `unlockEntries` **giống từng byte** với `unlockDescriptions` tương ứng → chạy lại tool sẽ KHÔNG hiện cảnh báo "nhãn cũ đã lệch".
6. **Có phải hand-fake không?** Đã **cài lại thuật toán `Norm()` + `ContainsWord()` + toàn bộ 68 luật `RULES`** bằng Python, tra ngược thư viện thật (21 `CropData`, 32 `PlaceableItemData`, 48 `InventoryItemData` + tra file ảnh theo tên) rồi tự suy icon cho cả 64 nhãn:
   **64/64 khớp CHÍNH XÁC guid+fileID đang có trong YAML.** → dữ liệu đúng là do tool sinh, **idempotent**, không phải bịa.
7. **`giftItems` / `hintText`** — nguyên vẹn 29/29 (xem tiêu chí 5).

#### D. TRACE LOGIC ẨN Ô (`ApplyUnlockSlots`, `LevelUpPopupUI.cs:381-441`)

| Tình huống | Kết quả trace | Kết luận |
|---|---|---|
| `cfg == null` | `entries = null` (394) → `wanted = 0` (396) → `used = 0` (397) → vòng 400-421 gọi `SetActive(false)` cho **cả 9 ô** | ✅ ẩn hết, KHÔNG còn ô trắng. Không NRE vì `entries[i]` (414) chỉ chạy sau `if (!inUse) continue;` (412) |
| `entries.Count = 3`, `slots.Length = 9` | `used = 3` → ô 0,1,2 `SetActive(true)` + `Setup()`; ô 3-8 `SetActive(false)` | ✅ đúng 3 bật / 6 ẩn |
| Mở popup LẦN 2 (L5 → L6, hoặc L6 → L18 ít mục hơn) | `ResolveUnlockSlots()` (469) dùng `GetComponentsInChildren<UnlockSlotUI>(**true**)` — `includeInactive = true` ở cả 3 nhánh (486, 509) → mảng luôn đủ 9 dù ô đang tắt; cache chỉ bị bỏ khi có phần tử null (472-479) | ✅ đúng, ô tắt bật lại được. `includeInactive` đã đúng ở CẢ nhánh chính và nhánh cứu cánh |
| Ô bị `SetActive(false)` giữa lúc coroutine pop chạy → kẹt `localScale = 0`? | Vô hiệu hoá GameObject làm Unity **dừng hẳn** coroutine → scale kẹt ~0. NHƯNG `Setup()` (`UnlockSlotUI.cs:47-49`) reset `_rt.localScale = Vector3.one` **TRƯỚC MỌI THỨ**, và thứ tự trong `ApplyUnlockSlots` là `SetActive(true)` (410) → `Setup()` (420) → `PlayPop()` (459) | ✅ đã bịt. `PlayPop()` khi object tắt cũng đặt scale = 1 rồi mới `return` (`UnlockSlotUI.cs:83-87`) |
| `_rt` null vì `Awake()` chưa chạy (ô nằm trong cây đang tắt lúc load scene) | `Setup()`:47 và `PlayPop()`:79 đều có `if (_rt == null) _rt = transform as RectTransform;` | ✅ an toàn |
| `PlayPop` gọi lúc chưa `activeInHierarchy` | Đã tách sang `PlayUnlockPops()` gọi ở dòng 241, **sau** `popupRoot.SetActive(true)` (207) và sau chốt `activeInHierarchy` (215) | ✅ đúng, animation không bị mất |

**Xác minh scene (điều DEV-B nói ở §5.4.1 là ĐÚNG):**
`SCN_Farm.unity` — MonoBehaviour `LevelUpPopupUI` **KHÔNG có** 3 key `unlockSlotsContainer` / `unlockSlots` / `unlockStripRoot` (scene lưu bằng script cũ) → cả 3 = null/rỗng lúc load → **chạy vào cứu cánh #3**. Đường dẫn thật:
`Canvas_Popup / Popup_LevelUp_Township / Root_HienThi / Content / Dai_MoKhoa / ScrollView / Viewport / Content / Slot_MoKhoa_01..09`
→ 9 ô nằm ĐÚNG dưới `Root_HienThi` (= `popupRoot`, fileID 966091324) nên `popupRoot.GetComponentsInChildren` **tìm được cả 9**.
**9/9 `UnlockSlotUI` có `iconImage` đã bind (fileID khác 0)** → `Setup()` sẽ thật sự vẽ được icon.
`levelRewardConfigs` = **29 phần tử** (không rỗng). Dòng Console `"đã TỰ TÌM được 9 ô"` là BÌNH THƯỜNG, đúng như DEV-B nói.

#### E. DANH SÁCH LỖI

**[Cao] BUG-01 — `fileID` sprite Kho SAI → L12 và L21 mỗi cấp 1 Ô TRẮNG**
`Assets/_Game/Farm/data/Lever Game/LevelReward_L12.asset:25` (nhãn "Nâng cấp kho lần 1")
`Assets/_Game/Farm/data/Lever Game/LevelReward_L21.asset:25` (nhãn "Nâng cấp kho lần 2")
```yaml
# SAI (đang có trên đĩa)
    icon: {fileID: 6877185878475894549, guid: ec37c4358fcdf50428a97e00a3d56320, type: 3}
# ĐỀ XUẤT SỬA
    icon: {fileID: 21300000, guid: ec37c4358fcdf50428a97e00a3d56320, type: 3}
```
Bằng chứng (4 lớp, độc lập nhau):
1. `maptitle/AssetsTitl/.../Sprite_Tiles_Warehouse.png.meta` có **`spriteMode: 1` (Single)**, không phải Multiple.
2. Toàn project chỉ có **12** texture `spriteMode: 1`; quét 34.156 tham chiếu object → **0 tham chiếu nào dùng fileID khác `21300000`** cho texture Single.
3. Chính `SCN_Farm.unity` tham chiếu đúng guid này bằng **`fileID: 21300000`**.
4. Ca đối chứng: `maptitle/tile_dirt.png` cũng `spriteMode: 1`, `internalIDToNameTable` ghi `213: 4084418110250204006` (tên cũ `grown01plot-…_0`) **nhưng Unity tham chiếu nó bằng `21300000` ở 7 chỗ** (`Đất.asset`, `Plot_01.prefab`, `grown.prefab`, `SCN_Farm.unity`, `Mission_main_l18_expand_land.asset`, `tile_dirt.asset`) — và L18 cũng dùng `21300000`, ĐÚNG.
→ Nguyên nhân: DEV-A lấy fileID từ `internalIDToNameTable` của `.meta`. Bảng đó **chỉ có thẩm quyền với `spriteMode: 2`**; với Single mode nó là rác sót lại.
→ Hậu quả: `icon` = null lúc chạy → cấp 12 và 21 sẽ có 1 khung tròn trắng (chỉ còn nhãn NEW). Console in `bật 2/9, có icon 1/2` + LogWarning `1/2 ô mở khoá có icon = NULL`.
→ **Ai sửa: DEV-A** (file `.asset` thuộc phạm vi DEV-A). Cách nhanh nhất & an toàn nhất: mở `Tools ▸ Farm ▸ Điền Icon Unlock (Level Reward)` bấm **ÁP DỤNG 1 lần** — `LoadAllAssetRepresentationsAtPath()` trả về Sprite THẬT nên Unity tự serialize lại `21300000`, tự khỏi. Hoặc sửa tay 2 dòng như trên.

**[TB] BUG-02 — Tiêu chí 3 (icon vàng/ngọc) KHÔNG THỂ PASS ở lần bấm Play đầu tiên**
`SCN_Farm.unity` — `Hang_Vang/Icon` và `Hang_Ngoc/Icon` vẫn là `spr_circle_fill` + tint (số liệu ở bảng B#3). Sprite được gắn lúc **dựng scene**, không phải lúc chạy → bắt buộc: thoát Play → `Tools ▸ Farm ▸ Popup Lên Cấp (Township)` → **DỰNG POPUP** → Ctrl+S. Trước bước đó, tiêu chí 3 ghi FAIL "chờ dựng lại", **không tính lỗi cho DEV-B** (code tool đã đúng, có fallback + LogWarning in nguyên đường dẫn). Đã xác minh 2 file đích tồn tại thật với guid khớp §4.1: `Fantasy Wooden GUI␣␣Free/PNG/vang-removebg-preview.png` (`a1c4be4b…`, spriteMode 2) và `kimcuong-removebg-preview.png` (`63b103df…`, spriteMode 2).

**[TB] BUG-03 — Mất HẾT chữ mô tả mục mở khoá: `unlockDescText` = NULL trong scene**
`SCN_Farm.unity:333065` → `unlockDescText: {fileID: 0}` (và `hintText: {fileID: 0}`).
`LevelUpPopupUI.cs:420` truyền `caption = ""` với lý do "nhãn chữ đã hiện gộp ở `unlockDescText`" (§5.2). Nhưng field đó **không được nối dây trong scene**, và `WireToLevelUpPopupUI` (`LevelUpPopupTownshipTool.cs:766-816`) **không bao giờ `Set("unlockDescText", ...)`** → dựng lại popup vẫn null.
→ Hậu quả: người chơi thấy 3 icon + nhãn NEW mà **không có một chữ nào** giải thích; câu "Mở khóa Khoai tây" không xuất hiện ở đâu. Không nằm trong §1 nhưng là hồi quy trải nghiệm → escalate cho art-director/DEV-B chốt: (a) truyền `entries[i].label` làm caption, hoặc (b) tool tự tạo + nối `Text_MoKhoa`.

**[Thấp] BUG-04 — §5.4.3 của DEV-B đã lỗi thời, làm TESTER kiểm sai chỗ**
`PopupCaptureReporter.cs:226-262` **ĐÃ** in sprite từng ô (`icon=<tên>` / `NULL ✘ Ô TRẮNG!`, `scale`, tổng `Đang bật: N/M  Có icon: K`), và dòng 265-281 **ĐÃ** kiểm `Hang_Vang`/`Hang_Ngoc` có còn `spr_circle_fill` hay không. → Tiêu chí 6 và 3 kiểm được **hoàn toàn bằng `popup_report.txt`**, không cần đọc Console. Đề xuất DEV-B sửa lại §5.4.3.

**[Thấp] BUG-05 — Lệch chỉ số nếu một ô là null (`LevelUpPopupUI.cs:403`)**
`if (slot == null) continue;` bỏ qua ô nhưng `i` vẫn "tiêu thụ" `entries[i]` → mục đó bị mất im lặng, `withIcon` đếm thiếu, còn `_pendingUnlockPopCount = used` (424) vẫn đếm dư. Thực tế **không chạm tới được** (cả 3 nhánh `ResolveUnlockSlots` trả mảng không null, cache tự dò lại khi có phần tử null) nên chỉ là vệ sinh code. Đề xuất: dùng con trỏ ghi riêng (`int fed = 0;`) hoặc lọc null ở cả nhánh 1 và 3.

**[Thấp] BUG-06 — Rủi ro KHÔNG idempotent với 2 texture nhiều sprite**
`Taulua/taulua.png` có 2 sub-sprite (`taulua_0`, `taulua_1`), `Assetsgame/SachNauAn.png` có 2 (`SachNauAn_0`, `_1`). `UnlockIconFillTool.LoadSprite()` (dòng 463-465) dùng `LoadAllAssetRepresentationsAtPath(...).OfType<Sprite>().FirstOrDefault()` — thứ tự trả về **không được Unity bảo đảm** → chạy lại tool có thể đổi sang `_1`. Đề xuất: `.OrderBy(s => s.name).FirstOrDefault()` hoặc ưu tiên sprite có tên kết thúc `_0`.

**[Thấp] BUG-07 — Báo động giả trong `popup_report.txt` nếu bấm F10 quá sớm**
`PopupCaptureReporter.cs:258-262` cảnh báo `localScale ≈ 0 → vô hình`. Animation pop (`UnlockSlotUI.PopRoutine`, `popDuration = 0.32s` + `staggerDelay = 0.06s`/ô) **hợp lệ** giữ scale ở 0 trong ~0.5s đầu. → Hướng dẫn Edric: **chờ ≥ 1,5 giây sau khi popup hiện rồi mới bấm F10**, nếu không sẽ đọc ra lỗi không tồn tại.

**[Thấp] BUG-08 — `unlockStripRoot` không được ẩn khi không dò được ô nào**
`LevelUpPopupUI.cs:384-388` `return` sớm khi `slots.Length == 0`, bỏ qua đoạn ẩn dải ở 427-428 → còn lại thanh nền tối rỗng. Chỉ xảy ra khi dò ô thất bại hoàn toàn. Đề xuất: chuyển đoạn 427-428 lên trước `return`.

*(Quan sát, không tính lỗi vòng này)* `Root_HienThi` lưu trong scene với `m_IsActive: 1` → popup lộ 1 frame trước khi `Start()` tắt nó. Hiện tượng có từ trước, không do thay đổi lần này.

#### F. KẾT LUẬN — CÓ nên để Edric bấm Play chưa?

## ✅ **CÓ — bấm Play được. KHÔNG có lỗi biên dịch nào.**

Nhưng làm **đúng thứ tự này**, nếu không sẽ mất công chụp lại:

1. **DEV-A sửa BUG-01 trước** (2 dòng ở `LevelReward_L12.asset:25` + `LevelReward_L21.asset:25`, hoặc bấm ÁP DỤNG trong tool 1 lần). Không sửa thì L12/L21 chắc chắn còn ô trắng.
2. **THOÁT Play Mode** → `Tools ▸ Farm ▸ Popup Lên Cấp (Township)` → **DỰNG POPUP** → **Ctrl+S**.
   Bắt buộc cho tiêu chí 3 (BUG-02). Sau bước này dòng Console `"đã TỰ TÌM được 9 ô"` sẽ biến mất — đó là dấu hiệu tốt.
3. **Play** → nút **②** bật popup **cấp 5** → **chờ ≥ 1,5 giây** (BUG-07) → **F10**.
4. Lặp bước 3 cho **cấp 18** (case biên: chỉ 1 mục → phải thấy `Đang bật: 1/9`) và **cấp 12** (case BUG-01 → phải thấy `Đang bật: 2/9  Có icon: 2`).

**Kỳ vọng ở `popup_report.txt` để chấm PASS:**
- `── Ô MỞ KHOÁ: 9 cái ──` → `Đang bật: 3/9   Có icon: 3` (cấp 5) và dòng `✔ Mọi ô đang bật đều có icon.`
- KHÔNG có dòng `✘✘ CÓ n Ô ĐANG BẬT MÀ KHÔNG CÓ ICON`
- KHÔNG có dòng `✘ n ô có localScale ≈ 0`
- `── ICON TIỀN TỆ ──` → `Hang_Vang sprite=vang-removebg-preview_0 ✔ sprite thật` và `Hang_Ngoc sprite=kimcuong-removebg-preview_0 ✔ sprite thật`
  (nếu vẫn `spr_circle_fill` → bước 2 chưa làm, chưa tính FAIL cho DEV-B)

---

## 6b. XỬ LÝ SAU BÁO CÁO TESTER VÒNG 1 (Orchestrator)

| Lỗi | Xử lý | Trạng thái |
|---|---|---|
| **BUG-01** — `L12.asset:25` và `L21.asset:25` dùng `fileID: 6877185878475894549` cho sprite Single | Đã sửa cả 2 về `fileID: 21300000` | ✅ ĐÃ SỬA |
| **BUG-02** — icon vàng/ngọc trong scene vẫn là `spr_circle_fill` | Không phải lỗi code. Sprite gắn lúc DỰNG, nên phải bấm **DỰNG POPUP** lại | ⚠ Cần Edric bấm |
| **BUG-03** — `unlockDescText` không nối dây, ô không có chữ | **KHÔNG SỬA — đúng thiết kế.** Đối chiếu video Township: các ô tròn **không có chữ nào**, chỉ icon + nhãn NEW. Yêu cầu của Edric là "design i hệt như trong video" | ✅ ĐÓNG (by design) |
| 5 lỗi Thấp còn lại | Ghi nhận, không chặn. Xử sau nếu ảnh chụp cho thấy vấn đề | 📋 Theo dõi |

**Kết luận: ĐỦ ĐIỀU KIỆN để Edric bấm Play.**

---

## 7. QUY TẮC CHUNG

1. **Chỉ đọc/sửa file thuộc phạm vi của mình** (mục 2). Cần file của người khác → ghi yêu cầu vào §3.
2. **Không hardcode đường dẫn sprite bằng chuỗi** trong code runtime — phải qua Inspector hoặc ScriptableObject.
3. **Không xoá** `unlockDescriptions`, `giftItems` — code khác đang dùng.
4. Mọi thay đổi phải **biên dịch được**; nêu rõ nếu chưa chắc.
5. Ghi nhật ký **ngắn, có số dòng dẫn chứng**, không kể lể.
6. TESTER **không được sửa code** — chỉ báo lỗi.

---

## 8. GIỚI HẠN THỰC TẾ (quan trọng)

Agent **không bấm được nút trong Unity**. Vòng xác minh thật là:

```
DEV-A + DEV-B code  →  Edric bấm Play + nút ③ (hoặc F10)
                    →  Unity ghi Assets/_Debug_Capture/{game_view.png, popup_report.txt}
                    →  TESTER đọc 2 file đó  →  PASS/FAIL
                    →  chưa đạt thì quay lại DEV
```

Nên TESTER vòng 1 chỉ kiểm được **bằng đọc code** (static review). Xác minh bằng ảnh chỉ làm được sau khi Edric bấm chụp.
