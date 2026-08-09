# BOX LÀM VIỆC — Chợ: Bảng Tin Chợ (T1) + Quầy Hàng (T2)

> File này là **kênh trao đổi chung** của DEV-A, DEV-B và TESTER.
> Mỗi người ghi vào mục của mình. Đọc mục của người kia trước khi làm phần giao nhau.

**Dự án:** `E:\Game2\Cooking-Game-2D` · Unity 6000.3.10f1 · không `.asmdef`
**Bộ skill:** `E:\Game2\Cooking-Game-2D\.claude\skills\` (74 skill)

---

## 0. YÊU CẦU CỦA CHỦ DỰ ÁN

1. **Chợ gộp cả hai nguồn**: luôn luôn có hàng bán (NPC bán mọi vật phẩm trong farm), **và** hàng người chơi đăng bán cũng hiện lên cùng bảng. Phòng khi chưa có multiplayer, chợ vẫn đầy hàng.
2. **KHÔNG có đồng tiền thứ ba.** Bỏ ý tưởng cỏ bốn lá. **Chỉ dùng VÀNG.**
3. **Thay hết dữ liệu trống bằng vật phẩm thật** trong farm. Giá và số lượng phải chăng để người chơi vào thường xuyên.
4. **T1**: cải tạo `Canvas_MarketPopup` đang có thành **Bảng Tin Chợ** giống video (dải lọc danh mục, thẻ có người bán, đếm ngược + làm mới).
5. **T2**: dựng **object mới + hierarchy Quầy Hàng** giống video 1.
6. **Chỉ dựng NỀN có màu** — chủ dự án tự gắn art sau. Bố cục giống video, **trang trí khác đi một chút để tránh đạo ý tưởng**.

---

## 1. 🔴 BA LỖI PHẢI SỬA TRƯỚC KHI THÊM TÍNH NĂNG

Rà soát phát hiện. **Không được bỏ qua** — thêm tính năng lên nền hỏng sẽ hỏng tiếp.

### LỖI 1 — Popup mở rồi tự đóng ngay
`MarketManager` và `MarketPopupUI` **cùng trỏ `popupRoot` vào `Panel_Background`**.
`Panel_Background` để `active=0` trong scene → `MarketPopupUI.Start()` chỉ chạy khi popup vừa được bật, và dòng `MarketPopupUI.cs:14` gọi `popupRoot.SetActive(false)` → **đóng ngay cái popup vừa mở**.

**Sửa:** gộp hai script làm một (giữ `MarketManager`, bỏ `MarketPopupUI`), hoặc bỏ hẳn dòng `SetActive(false)` trong `Start()`.

### LỖI 2 — Mua chùa
`MarketManager.cs:293` — `CanSpendGold` **trả `true` khi `FarmEconomyManager.Instance == null`**. Không có manager là mua không mất tiền.

**Sửa:** không có manager thì trả `false`.

### LỖI 3 — Hạt giống mua ở chợ vào SAI KHO
`MarketManager.cs:174` luôn đổ vào `FarmInventoryManager`. Nhưng quy ước dự án:

| Kho | Chứa gì |
|---|---|
| `WarehouseManager` | **CHỈ HẠT GIỐNG** (`seed_*`) |
| `FarmInventoryManager` | nông sản, sản phẩm chuồng/máy, món ăn, gia vị |

Mua hạt ở chợ xong **không trồng được** vì nó nằm nhầm kho.

**Sửa:** thêm nhánh phân loại trong `TryBuy` **TRƯỚC KHI** điền dữ liệu hạt giống vào chợ.
⚠️ Cạm bẫy: `ca_rot` và `khoai_tay` là hạt giống nhưng **không có tiền tố `seed_`**. Đừng phân loại bằng `StartsWith("seed")` — phải tra bảng `CropData.seedItemId`.

---

## 2. HIỆN TRẠNG — ĐÃ CÓ GÌ

| Thứ | Ở đâu | Ghi chú |
|---|---|---|
| `MarketManager.cs` | `Farm\Scripts\Market\` | 462 dòng, singleton, timer, mua |
| `MarketShopItemUI.cs` | cùng thư mục | thẻ hàng, dùng prefab thuần |
| `MarketPopupUI.cs` | cùng thư mục | **trùng chức năng — nên bỏ** |
| `MarketClickOpen.cs` | cùng thư mục | raycast mở chợ, chạy mỗi frame |
| `MarketDatabase_SO.cs` | cùng thư mục | `MarketItemDef` chỉ 4 trường |
| Prefab thẻ | `_Game\Prefab\ui\Market\ShopItem_Prefab.prefab` | dùng `Text` legacy, 170×210 |
| Object trên map | `World/Buildings/Market` | 3 BoxCollider2D |

### Hierarchy hiện tại của `Canvas_MarketPopup` (13 object, là GameObject GỐC của scene)

```
Canvas_MarketPopup                       Canvas, CanvasScaler, GraphicRaycaster, CanvasGroup, UIRaycastBlocker
└─ Panel_Background              [OFF]   Image          ← root popup
   └─ Popup_Main                         Image, MarketManager, MarketPopupUI
      ├─ Header_Bar                      Image
      │  ├─ BtnClose ▸ txt_X             Button, TMP
      │  ├─ Text_Timer                   Text (legacy)
      │  ├─ Timer_Background ▸ FillBar_Timer
      │  ├─ Button_RefreshFree ▸ Text
      │  └─ Button_RefreshGem ▸ Text
      └─ Scroll_View                     ScrollRect (ngang, dọc=0)
         └─ Viewport ▸ Content           GridLayoutGroup 200×180, 2 hàng
```

**Thiếu:** `ContentSizeFitter` trên `Content` · `MarketManager.buttonClose` chưa gán · không có tiêu đề popup · không hiện số vàng.

---

## 3. DỮ LIỆU — TÌNH TRẠNG THẢM HOẠ

`MarketDatabase.asset` có 48 dòng:

- **38 dòng là `TODO_*`** (79%) → chợ hiện `TODO_DISH_ID_07`, icon trắng, mua vào thành item rác
- **10 dòng còn lại bị trùng**: `rice`×2, `fishsauce`×6, `salt`×2 → thực chất chỉ **3 vật phẩm**

### 🔴 Khoảng trống lớn nhất: KHÔNG CÓ GIÁ BÁN

**Chỉ `CropData.sellGold` tồn tại.** Món ăn, sản phẩm chuồng, sản phẩm máy, gia vị, vật liệu — **không thứ nào có giá bán**.

Không có giá thì không làm được cả chợ lẫn quầy hàng. **Đây là việc đầu tiên phải giải quyết.**

### Vật phẩm có sẵn để điền vào chợ

**Nông sản (có sẵn `sellGold`)** — rice 7 · ngo 13 · bapcai 15 · carot 16 · cachua 20 · khoaitay 25 · mushroom 30 · sugarcane 36 · lemon 38 · chili 48 · pepper 55

**Hoa (có sẵn `sellGold`)** — huong_duong 12 · hoa_hong 24 · hoa_oai_huong 30 · hoa_cuc_trang 24 · hoa_lan 22 · tulip 20 · hoa_cuc_van_tho 26 · hoa_anh_thao 32 · hoa_cam_tu_cau 30 · hoa_mau_don 28

**Hạt giống (21 loại, giá = `goldPrice`)** — `seed_rice` 20 … `seed_pepper` 190. ⚠️ `ca_rot`, `khoai_tay` không có tiền tố `seed_`.

**Sản phẩm chuồng (CHƯA CÓ GIÁ)** — beef · pork · chicken_meat · egg · milk

**Sản phẩm máy (CHƯA CÓ GIÁ)** — bot_gao · nuoc_mia_ep · pho_mai

**Món ăn (20 món, CHƯA CÓ GIÁ)** — xem `Farm\data\Farm_Cooking\`

**Gia vị (CHƯA CÓ GIÁ)** — fishsauce · salt · soysauce · sugar · herbs

---

## 4. PHÂN CÔNG

### DEV-A — Nền tảng dữ liệu + T1 Bảng Tin Chợ

Skill gợi ý: `content-audit` → `balance-check` → `quick-design` → `dev-story` → `team-ui` → `code-review`

| # | Việc | Xong khi |
|---|---|---|
| A0 | **Sửa LỖI 1, 2, 3 ở mục 1** | popup mở không tự đóng · không mua chùa được · hạt vào đúng kho |
| A1 | Thêm giá bán cho mọi vật phẩm chưa có (mục 3) — thêm field vào `InventoryItemData`/`DishData` hoặc bảng giá riêng, DEV-A tự chọn rồi **ghi vào mục 7 để DEV-B dùng chung** | mọi itemId tra được giá |
| A2 | Mở rộng `MarketItemDef`: thêm `Category`, `UnlockLevel`, `Weight`, `MinQuantity/MaxQuantity` giữ nguyên | có đủ trường để lọc |
| A3 | **Điền lại toàn bộ `MarketDatabase.asset`** bằng vật phẩm thật, bỏ trùng lặp, bỏ `TODO_*`. Giá mua ở chợ nên cao hơn giá bán ~40-60% để có chỗ cho quầy hàng | không còn `TODO_` nào |
| A4 | **Hệ thống người bán NPC**: `MarketSellerData` (tên Việt, avatar, cấp) — khoảng 40-60 tên, giữ ổn định giữa các phiên | thẻ nào cũng có người bán |
| A5 | **`IMarketProvider`** — `LocalMarketProvider` trộn hàng NPC + hàng người chơi từ quầy (DEV-B cung cấp). Viết theo hình dạng server để sau nối multiplayer không phải sửa UI | bảng tin lấy hàng qua provider |
| A6 | Làm mới **bằng VÀNG** (bỏ gem): thêm `goldRefreshCost`, giá luỹ tiến trong ngày. `RefreshNowFree` chỉ cho phép khi hết giờ | không còn refresh chùa |
| A7 | Timer **chạy nền + lưu**: `DateTimeOffset.UtcNow` + PlayerPrefs, đóng game mở lại vẫn đúng | thoát game vào lại timer đúng |
| A8 | **UI Bảng Tin Chợ** trên `Canvas_MarketPopup`: dải lọc danh mục dọc · thẻ 2 tầng (hàng ở trên, người bán ở dưới) · đếm ngược + nút làm mới vàng · hiện thẻ **so le** · trạng thái rỗng "CHƯA CÓ VẬT PHẨM NÀO ĐƯỢC ĐĂNG BÁN" · `ContentSizeFitter` · đổi `Text` legacy sang TMP | giống video, nền có màu |

### DEV-B — T2 Quầy Hàng

Skill gợi ý: `quick-design` → `create-stories` → `dev-story` → `team-ui` → `ux-review` → `code-review`

| # | Việc | Xong khi |
|---|---|---|
| B1 | `PlayerListing` (itemId, qty, giá, thời điểm đăng, hạn, trạng thái) + lưu/khôi phục PlayerPrefs có `saveVersion` | thoát game vào lại còn hàng |
| B2 | **Object Quầy Hàng ngoài map** + collider + script mở popup. Bày hàng đang bán lên mặt quầy để nhìn từ ngoài biết đang bán gì | bấm vào mở được popup |
| B3 | **Popup Quầy Hàng** — lưới ô **4 trạng thái**: trống dùng được (`+` "Bán vật phẩm") · đang bán (icon + SL + giá) · khoá mở được (🔒 "Thêm ô" + giá **VÀNG**) · chưa tới lượt (ô trơn) | 4 trạng thái phân biệt rõ |
| B4 | Panel chọn vật phẩm **trượt đè lên lưới** (không phải popup mới): 3 cột — tab danh mục · lưới vật phẩm có badge số lượng · khu thiết lập | giữ được ngữ cảnh |
| B5 | Bộ chỉnh **số lượng** và **giá**: nút `−`/`+`, **nút `−` chuyển XÁM khi chạm giới hạn**. Giá gợi ý tự tính theo số lượng, người chơi tinh chỉnh được | phản hồi trạng thái đúng |
| B6 | Vật phẩm **hết số lượng thì gỡ khỏi lưới chọn** | lưới chỉ hiện thứ chọn được |
| B7 | Nút **gạt loa** (bật/tắt quảng cáo), trả bằng **VÀNG** | gạt được, trừ vàng |
| B8 | Đặt lên quầy → trừ kho · Huỷ → hoàn kho · Hết hạn → hoàn kho **không mất hàng** | không mất hàng trong mọi đường |
| B9 | **NPC tự mua** hàng trên quầy sau một khoảng thời gian ngẫu nhiên → cộng vàng. Không có bước này thì quầy hàng vô nghĩa khi chưa có multiplayer | đăng bán xong có tiền về |
| B10 | Xuất danh sách hàng đang bán cho DEV-A gộp vào bảng tin (mục A5) | hàng của mình hiện ở bảng tin |

### Giao diện chung giữa hai dev

DEV-A cần từ DEV-B: hàm lấy danh sách `PlayerListing` đang Active.
DEV-B cần từ DEV-A: bảng giá gốc (A1) để tính giá gợi ý.

**Chốt tên hàm ở mục 7 trước khi ai đó code.**

---

## 5. QUY TẮC CHUNG

- **Chỉ dựng nền có màu.** Mọi chỗ chờ art để `Image` màu phẳng + tên rõ ràng, đừng để trống trắng.
- **Bố cục giống video, trang trí khác đi.** Đừng sao chép y hệt hình dáng/màu — đổi bo góc, đổi bảng màu, đổi hoạ tiết viền.
- **Không dựng UI bằng `new GameObject()` trong runtime.** Dùng prefab + Editor tool sinh hierarchy. Đây là bài học từ `UnifiedTaskPopupUI` 1433 dòng không ai sửa nổi.
- **TextMeshPro**, không dùng `Text` legacy.
- Mọi save phải có `saveVersion`.
- Comment bằng tiếng Việt, giải thích **VÌ SAO** chứ không phải *cái gì*.
- **Không được để lỗi biên dịch.** Tự kiểm ngoặc, `#if/#endif`, using trước khi báo xong.

---

## 6. NHẬT KÝ TRAO ĐỔI

*(DEV-A, DEV-B ghi vào đây)*

### DEV-A
> **Xong A0 → A8 (2026-08-09).** Đã đọc phần DEV-B ghi ở mục 7 và **theo hợp đồng của DEV-B**
> (`BasePriceBook` / `PlayerStallManager`) thay vì áp cái của mình — hai bên đã khớp, không phải sửa gì thêm.

#### ⚠️ VIỆC ĐẦU TIÊN PHẢI LÀM KHI MỞ UNITY

Mở `SCN_Farm.unity` rồi bấm **`Tools ▸ Farm ▸ Chợ ▸ 0 · CHẠY TẤT CẢ`** → **Ctrl+S**.

Một nút chạy đủ 4 bước theo đúng thứ tự (sprite → dữ liệu → hierarchy → nguồn icon).
**Chưa chạy thì `Canvas_MarketPopup` vẫn là hierarchy cũ và bảng tin ra trắng** —
scene có 591.000 dòng YAML nên tôi không sửa tay, dựng bằng tool là đường an toàn duy nhất.

#### A0 · Ba lỗi nghiêm trọng — ĐÃ SỬA

| Lỗi | Sửa ở đâu | Cách sửa |
|---|---|---|
| 1 · Popup tự đóng | `MarketPopupUI.cs` | Bỏ hẳn `popupRoot.SetActive(false)` trong `Start()`. Giữ lại class (không xoá file) vì `PopupManager`, `MarketClickOpen`, `DisableStartupPopupsTool`, `DemoL1L10Tool` đều tham chiếu kiểu này — xoá là scene mất component và 2 tool Editor không biên dịch. Giờ nó chỉ uỷ quyền sang `MarketManager`. |
| 2 · Mua chùa | `MarketManager.CanSpendGold` | `FarmEconomyManager.Instance == null` giờ trả **false**. Thêm: `TryBuyListing` trừ vàng **trước** khi cộng kho và kiểm tra kho đích tồn tại **trước** khi trừ vàng. |
| 3 · Hạt vào sai kho | `MarketManager.GiveItemToCorrectStorage` | Phân loại bằng `MarketPriceTable.IsSeed()` (tra danh mục), hạt → `WarehouseManager.AddItem(id, tên, icon, sl)`, còn lại → `FarmInventoryManager`. **Không dùng `StartsWith("seed")`** vì `ca_rot` và `khoai_tay` không có tiền tố đó. |

#### A1–A8 · File đã tạo / sửa

**Runtime** — `Assets\_Game\Farm\Scripts\Market\`

| File | Việc | Nội dung |
|---|---|---|
| `MarketPriceTable.cs` | **A1** | **74 vật phẩm có giá gốc** + 5 món tắt thủ công. Nông sản/hoa lấy đúng `CropData.sellGold`; hạt giống = 55% `goldPrice`; chăn nuôi / chế biến / món ăn / gia vị / vật liệu là số mới. Hệ số: chợ ×1.5, gợi ý bán ×1.3, biên chỉnh tay ×0.5–×2.0. |
| `MarketCategory.cs` | A2 | Enum 9 danh mục + tên/màu/viết tắt. Dải lọc hiện 8 tab (bỏ `CheBien`, xem lý do trong file). |
| `MarketDatabase_SO.cs` | A2 | `MarketItemDef` thêm `Category`, `UnlockLevel`, `Weight`. Bỏ hàm sinh `TODO_*`. |
| `MarketSellerDirectory.cs` | **A4** | **56 người bán**: 50 tên Việt + 6 tài khoản `guest.13xxxxxxx`. Mảng cứng ⇒ `npc_00`…`npc_55` ổn định vĩnh viễn giữa các phiên, không cần lưu. Avatar tạm là 10 màu + chữ cái đầu. |
| `MarketListing.cs` | A5 | Kiểu hàng rao bán theo hình dạng server. Thời gian bằng **UTC ticks**. |
| `IMarketProvider.cs` | A5 | Interface + `MarketPlayerListingBridge` (DEV-B đã cắm vào). |
| `LocalMarketProvider.cs` | A5 | Trộn hàng NPC + hàng người chơi. Bốc bằng `System.Random(cycleSeed)` để cùng chu kỳ ra cùng bảng. Chặn trùng > 2 lần/món, rải người bán, giá ±25%. Xếp: có loa → hàng người chơi → giảm sâu nhất. |
| `MarketRefreshTimer.cs` | **A6+A7** | Mốc hết hạn lưu `DateTimeOffset.UtcNow.UtcTicks` vào PlayerPrefs, có `saveVersion` + nhánh `MigrateFromLegacy`. Bù nhiều chu kỳ một lúc khi tắt game lâu. Làm mới **bằng VÀNG**, luỹ tiến 150 → 300 → 450 (trần 900), reset theo ngày UTC. Miễn phí **chỉ khi** hết giờ. |
| `MarketManager.cs` | A0+glue | Còn dữ liệu/kinh tế/đóng-mở. Bỏ sạch gem, bỏ `LoadData`/`TryBuy`/`RefreshNowWithGems`. |
| `MarketBoardPalette.cs` | A8 | Bảng màu + kích thước dùng chung Editor↔runtime. |
| `MarketBoardUI.cs` | **A8** | Vẽ lưới, lọc danh mục, đồng hồ, nút làm mới, trạng thái rỗng, toast, **hiện thẻ so le**. Tái dùng thẻ thay vì Destroy/Instantiate. |
| `MarketListingCardUI.cs` | A8 | Thẻ 2 tầng (hàng trên · người bán dưới) + nhãn "HỜI"/"CỦA BẠN" + lớp phủ ĐÃ BÁN. |
| `MarketCategoryTabUI.cs` | A8 | Tab lọc; tab đang chọn đổi nền vàng + phóng 1.12× + hiện tên đầy đủ. |
| `MarketStallBridgeAdapter.cs` | phối hợp | Cắm `MarketPriceTable` vào `BasePriceBook` của DEV-B qua `[RuntimeInitializeOnLoadMethod]`. |

**Editor** — `Assets\_Game\Farm\Editor\`

| File | Nội dung |
|---|---|
| `MarketBoardSpriteFactory.cs` | Sinh 6 sprite thủ tục (bo góc 30/20/12, viên thuốc, tròn, **dải chấm bi**) ra `Farm/Art/UI_MarketBoard/`. Toàn màu trắng + alpha, tint bằng `Image.color`. |
| `MarketDatabaseGeneratorTool.cs` | **A3** — sinh lại `MarketDatabase.asset` từ bảng giá; **tự bỏ vật phẩm chưa có icon** (quét `t:InventoryItemData` + `t:CropData`). Kèm menu "4 · Kiểm tra dữ liệu chợ". |
| `MarketBoardUIBuilder.cs` | **A8** — dựng hierarchy trong scene + sinh 2 prefab, nối dây bằng `SerializedObject`. |

**Dữ liệu** — `Assets\_Game\Farm\data\Market\MarketDatabase.asset`: **74 dòng thật, 0 dòng `TODO_`, 0 trùng lặp** (trước: 38 `TODO_` + 10 dòng trùng = thực chất 3 vật phẩm).

**Đã XOÁ:** `MarketShopItemUI.cs` và `_Game/Prefab/ui/Market/ShopItem_Prefab.prefab` — thẻ cũ dùng `Text` legacy, không còn ai tham chiếu (đã kiểm bằng GUID trong mọi `.unity`/`.prefab`).

#### Quyết định thiết kế

1. **Bảng giá là static trong code, không phải ScriptableObject.** Giá phải tra được từ mọi nơi kể cả khi chưa có manager nào trong scene; và cân bằng bằng cách đọc một file dễ hơn soi 60 asset nằm ở 5 thư mục.
2. **Chợ bán rẻ hơn Shop.** Hạt giống ở chợ ≈ 82% giá Shop → có lý do ghé chợ. Món ăn là hàng đắt nhất (mua một đĩa phở rẻ hơn tự gom nguyên liệu).
3. **Vật phẩm chưa có icon bị chặn từ khâu sinh dữ liệu**, không phải chặn ở UI — chắc chắn hơn. Hiện bị chặn: `bot_gao`, `nuoc_mia_ep`, `pho_mai` (asset để icon = None) và `canh_chua_ca`, `ca_nuong_tieu` (`unlockLevel 99`, farm chưa có cá).
4. **Đồng hồ chạy nền thật.** Không dùng coroutine + `Time.deltaTime` như bản cũ (coroutine chết khi popup đóng). Thoát game 3 tiếng vào lại vẫn đúng chu kỳ.
5. **Người bán là mảng cứng**, không random mỗi phiên — "Ngọc Hằng hôm nay bán lúa, mai vẫn là Ngọc Hằng cấp 24".
6. **Trang trí khác video có chủ đích.** Video: nền cam đất · mái hiên **sọc** xanh-trắng · thẻ khung vé **góc khuyết** · icon danh mục treo **dây thừng**. Bản này: nền **xanh mòng két** · dải **chấm bi** tím-kem · thẻ **bo góc tròn đều** · tab dạng **viên thuốc** trên thanh ray dọc. Bố cục giữ theo video vì bố cục đó tốt.

#### Chỗ còn CHỜ ART (mọi chỗ đều là `Image` màu phẳng, tên có hậu tố `_ChoArt`)

| Object | Ở đâu | Thay bằng gì |
|---|---|---|
| `Icon_Gold_ChoArt` | chip ví · nút làm mới · thẻ hàng | icon đồng vàng |
| `Image_Accent_ChoArt` | prefab tab danh mục | 8 icon danh mục |
| `Image_SellerAvatar_ChoArt` | prefab thẻ hàng | 10 avatar NPC (đang là 10 màu + chữ cái đầu) |
| `Deco_RibbonTop` + `Deco_RibbonDots` | đỉnh popup | hoạ tiết dải trang trí |
| 6 sprite trong `Farm/Art/UI_MarketBoard/` | nền panel/thẻ/viên thuốc | đè PNG cùng tên là xong, không phải sửa code |
| icon 3 sản phẩm máy | `Item_BotGao` · `Item_NuocMiaEp` · `Item_PhoMai` | gán icon rồi bật `MarketEnabled = true` và chạy lại menu 2 |

#### Rủi ro còn lại

- **Chưa chạy `Tools/Farm/Chợ/0` thì bảng tin chưa có gì.** Đây là việc bắt buộc, không phải tuỳ chọn.
- Dữ liệu `MarketManager` cũ trong scene (`textTimer`, `buttonRefreshGem`, `shopItemPrefab`…) sẽ bị Unity bỏ khi nạp — bình thường, tool nối dây lại hết.
- Đồng hồ dựa vào giờ máy nên vặn ngược đồng hồ hệ thống vẫn ăn gian được. Chỉ server mới chặn nổi; game đơn nên chấp nhận.
- Chưa test trong Unity (môi trường không có Editor). Tôi đã tự soát cân bằng ngoặc, `#if/#endif`, `using`, và mọi ký hiệu gọi chéo. TESTER chạy mục 8 giúp.

### DEV-B
> **2026-08-09 — T2 Quầy Hàng: XONG B1→B10.** Biên dịch sạch (đã đối chiếu
> `Library/ScriptAssemblies`, toàn bộ 12 kiểu mới có mặt trong Assembly-CSharp /
> Assembly-CSharp-Editor).

**File đã tạo — runtime** (`Assets\_Game\Farm\Scripts\Stall\`, Assembly-CSharp):

| File | Việc |
|---|---|
| `PlayerListing.cs` | B1 · mô hình listing hình dạng server + `StallSourceStore`, `ListingStatus` |
| `PlayerStallManager.cs` | B1/B8/B9/B10 · bộ não: ô quầy, save có `saveVersion`, trừ/hoàn kho, NPC mua, cầu nối chợ |
| `BasePriceBook.cs` | lớp bọc bảng giá — ưu tiên `MarketPriceTable` của DEV-A |
| `StallItemCatalog.cs` | sổ tra itemId → icon / tên / danh mục / kho nguồn |
| `StallPopupUI.cs` | B3/B4/B5/B6/B7 · popup + panel trượt đè + bộ chỉnh + nút gạt loa |
| `StallSlotUI.cs` | B3 · một ô, 4 trạng thái |
| `StallPickItemCellUI.cs` | B4 · ô trong lưới chọn, badge số lượng góc dưới phải |
| `StallCategoryTabUI.cs` | B4 · tab danh mục |
| `StallWorldObject.cs` | B2 · quầy ngoài map, bấm mở popup |
| `StallCounterDisplay.cs` | B2 · bày hàng lên mặt quầy |

**File đã tạo — Editor** (`Assets\_Game\Farm\Editor\`):

| File | Việc |
|---|---|
| `StallHierarchyBuilderTool.cs` | menu **`Tools ▸ Farm ▸ Quầy Hàng`** — sinh toàn bộ hierarchy + prefab |
| `StallSpriteFactory.cs` | sinh 10 sprite thủ tục (không cần art ngoài) |

**KHÔNG sửa file nào của DEV-A.** Không đụng `MarketManager`, `PopupManager`,
`FarmInputLock` — tránh xung đột.

#### Quyết định thiết kế (và vì sao)

1. **Bỏ contract tạm của mình, dùng `MarketPriceTable` + `MarketPlayerListingBridge` của DEV-A.**
   DEV-A ship trước; hai hợp đồng song song sẽ cho hai con số giá khác nhau trên hai màn hình.
2. **Kho nguồn được GHI VÀO TỪNG LISTING lúc đăng bán** (`sourceStoreRaw`), không đoán lại
   lúc hoàn hàng. Lúc hoàn thì kho đã về 0 nên mọi phép đoán "kho nào đang giữ món này"
   đều sai → hàng lạc kho = mất hàng.
3. **Không mất hàng trong MỌI đường (B8).** Hoàn kho thất bại (manager chưa tồn tại) thì
   đặt cờ `refundPending` và thử lại mỗi nhịp + mỗi phiên sau, không bao giờ bỏ qua.
   Bán mà chưa cộng được vàng thì **giữ nguyên Active**, không đánh dấu Sold.
   `TrimFinished()` không bao giờ xoá dòng còn nợ hoàn hàng.
4. **Mốc thời gian là UTC ticks tuyệt đối**, kể cả mốc NPC sẽ mua (`npcBuyAtUtcTicks`
   quay số ngay lúc đăng bán). Nhờ vậy offline vẫn đúng, không cần chạy nền.
   Thứ tự xét: **NPC mua trước, hết hạn sau** — ngược lại thì người chơi tắt app sẽ mất
   khoản tiền lẽ ra đã kiếm được.
5. **Giá ảnh hưởng tốc độ bán** (B9): rẻ → nhanh, đắt → chậm, bật loa → ×0.45.
   Không có liên hệ này thì người chơi luôn kéo giá kịch trần và bộ chỉnh giá thành trang trí.
6. **Tiền loa trừ TRƯỚC khi trừ kho**, và nếu kho từ chối thì hoàn lại tiền loa ngay.
   Làm ngược lại sẽ đẻ thêm một đường hoàn hàng nữa.
7. **Không đụng `PopupManager`** (danh sách cứng, DEV-A cũng đang ở đó) — dùng
   `FarmInputLock.RegisterPopupOpen/Close` + `SetPopupRaycastBlock`.
8. **`popupRoot.SetActive(false)` đặt ở `Awake`, KHÔNG ở `Start`** — đây đúng là LỖI 1
   của chợ cũ (Start chạy lại khi popup vừa bật → tự đóng).
9. **Trang trí KHÁC video** (yêu cầu mục 0.6): bảng màu **mận/tím + nhấn ngọc lam**
   (video: cam đất) · góc **VÁT (bát giác)** (video: bo tròn) · mái hiên **RĂNG SÒ
   viền vàng** (video: sọc xanh-trắng). Bố cục thì giữ đúng video vì đó là công năng.
10. **Chỉ dùng VÀNG.** Mở ô quầy và bật loa đều trừ vàng. Không có đồng tiền thứ ba.

#### Cách chạy (cho TESTER)

1. Mở `SCN_Farm`.
2. Menu **`Tools ▸ Farm ▸ Quầy Hàng`** → bấm **“2 · Dựng TẤT CẢ”**.
3. Sinh ra 3 object gốc: `StallSystem` · `Canvas_StallPopup` · `Stall_WorldObject`.
4. Kéo `Stall_WorldObject` (đang ở toạ độ 0,0) tới chỗ muốn đặt trên bản đồ.
5. Bấm Play → bấm vào quầy → popup mở.

#### Chỗ CHỜ ART (đều là màu phẳng, tên có tiền tố dễ tìm)

Tìm theo tiền tố **`IMG_Art…`** (UI) và **`SPR_Art…`** (ngoài map):

| Tên | Là gì |
|---|---|
| `IMG_ArtPanelBackground` · `IMG_ArtPickerBackground` | nền popup |
| `IMG_ArtValance` | mái hiên răng sò |
| `IMG_ArtSlotBackground` · `IMG_ArtCellBackground` | nền ô |
| `IMG_ArtPlusIcon` · `IMG_ArtLockIcon` · `IMG_ArtSpeakerIcon` · `IMG_ArtCoin` · `IMG_ArtGoldIcon` | icon |
| `IMG_ArtCategoryIcon` (×5) | icon 5 tab danh mục |
| `IMG_ArtPlayerAvatar` | avatar góc dưới trái |
| `SPR_ArtStallBody` · `SPR_ArtStallValance` · `SPR_ArtEmptySign` | quầy ngoài map |

Sprite thủ tục nằm ở `Assets\_Game\Farm\Art\UI_Stall\` — thay file cùng tên là xong,
hoặc kéo art mới vào các `Image`/`SpriteRenderer` trên.

#### Còn treo / rủi ro

- **Chưa chạy được Unity để bấm nút tool** (không có Editor trong môi trường này) —
  hierarchy chỉ được sinh khi TESTER chạy menu ở bước 2. Code đã biên dịch sạch.
- **`Stall_WorldObject` đặt tạm ở (0,0)** — cần kéo tới vị trí hợp lý trên bản đồ.
- **Cấp mở ô** đang là `3/5/8/12/16/21/27` và **giá mở ô** `500…24000` vàng —
  số cân bằng tạm, chỉnh trong Inspector của `StallSystem`.
- **Nếu `MarketPriceTable` thiếu itemId nào** thì quầy rơi về bảng dự phòng trong
  `BasePriceBook` (giá có thể lệch bảng tin). DEV-A bổ sung dòng là hết.

### TESTER
> **Rà soát tĩnh toàn bộ 31 file mới/sửa (8.823 dòng) + `MarketDatabase.asset` + `SCN_Farm.unity` — 2026-08-09.**
> Môi trường không có Unity Editor và không có `mcs`/`csc`/`dotnet` ⇒ **không chạy được biên dịch thật**.
> Thay vào đó đã kiểm bằng máy: cân bằng ngoặc, `#if/#endif`, `using`, trùng tên kiểu toàn dự án,
> đối chiếu CHỮ KÝ từng hàm gọi chéo với khai báo gốc, và tra GUID trong 1.327 file `.unity`/`.prefab`/`.asset`.
>
> **KẾT LUẬN: KHÔNG có lỗi chặn biên dịch. KHÔNG sửa file code nào.**
> Nhưng **CHƯA bàn giao được** — còn 1 việc bắt buộc (chạy 2 Editor tool) và 5 lỗi cần sửa.

#### 1 · BIÊN DỊCH — ĐẠT

| Kiểm | Kết quả |
|---|---|
| Cân bằng `{}` `()` `[]` (bỏ chuỗi/comment) | 31/31 file cân bằng |
| `#if` ↔ `#endif` | khớp; 5 file Editor đều bọc `#if UNITY_EDITOR` |
| `using` đủ cho mọi type | đủ. `PlayerStallManager` có cả `using System;` lẫn `using UnityEngine;` nhưng viết `UnityEngine.Random.Range` — tránh đúng CS0104. `LocalMarketProvider` viết `System.Random`. Không còn `Random.` trần nào. |
| **Trùng tên kiểu (CS0101)** | **Không có.** 314 kiểu cấp cao nhất trong `Assets/`, 0 trùng. 4 tên trùng duy nhất (`CameraController`, `DayEvent`, `LightFrame`, `FpsCounterAnchorPositions`) đều là kiểu CŨ nằm trong namespace khác — không dính hai dev. |
| Type Editor bị runtime tham chiếu | **Không.** `Scripts/Market/` + `Scripts/Stall/` không hề nhắc `UnityEditor`/`*SpriteFactory`/`*Builder*` (trừ 1 dòng comment). Chiều ngược lại (Editor gọi `EditorSetVisualSources` / `EditorReplaceItems` / `EditorSetDatabases` / `EditorSetCategory`) đều nằm trong `#if UNITY_EDITOR` của file runtime — hợp lệ. |
| Ký hiệu ngoài (đã tra tận khai báo) | `FarmEconomyManager.Gold/SpendGold/AddGold/OnCurrencyChanged(int,int)` · `WarehouseManager.Items/GetAmount/RemoveItem/AddItem(id,tên,icon,sl)` · `WarehouseItemEntry.itemId/amount` · `FarmInventoryManager.GetOrderedItems/GetAmount/RemoveItem/AddItem` · `PlayerProgressManager.Level` · `FarmLevelManager.CurrentLevel` · `FarmInputLock.*` · `EditModeManager.IsEditMode` · `PopupManager.IsAnyPopupOpen()` · `MissionProgressTracker.ReportEvent(MissionEventType,string,int)` + `BuySeed`/`BuyShopItem` · `CropData.itemName/itemIcon/harvestIcon/goldPrice/sellGold/cropCategory/seedItemId/harvestItemId/cropId/itemID` · `InventoryItemData.itemId/displayName/icon` — **tất cả tồn tại, đúng chữ ký**. |
| TMP API mới (`textWrappingMode`, `TextWrappingModes`, `TextOverflowModes`) | có thật trong `com.unity.ugui@bb329a87fcdc/Runtime/TMP/TMP_Text.cs:744` |
| `.meta` | 31/31 file có `.meta`; GUID trong scene khớp GUID `.meta` |

#### 2 · TÍCH HỢP DEV-A ↔ DEV-B — ĐẠT, khớp từng chữ

| Điểm chạm | Khai báo (DEV-A) | Chỗ gọi (DEV-B) | Khớp |
|---|---|---|---|
| `IsSeed` | `MarketPriceTable.cs:153` `public static bool IsSeed(string)` | `PlayerStallManager.cs:454` | ✅ |
| `GetMinPlayerUnitPrice` | `MarketPriceTable.cs:116` | `PlayerStallManager.cs:419` | ✅ tên chính xác |
| `GetMaxPlayerUnitPrice` | `MarketPriceTable.cs:123` | `PlayerStallManager.cs:429` | ✅ tên chính xác |
| `GetSuggestedUnitPrice` · `GetBasePrice` · `Has` · `TryGet` | `MarketPriceTable.cs:99/86/83/78` | `PlayerStallManager.cs:410`, `BasePriceBook.cs:94`, `MarketStallBridgeAdapter.cs:32` | ✅ |
| `MarketPlayerListingBridge` | **`Scripts/Market/IMarketProvider.cs:76`** (không phải file riêng) | — | có đủ `GetActiveListings` (`Func<List<MarketListing>>`), `OnPlayerListingSold` (`Func<string,bool>`), `NotifyChanged()`, `Clear()`, `FetchActiveListings()` |
| Kiểu delegate | `Func<List<MarketListing>>` / `Func<string,bool>` | `BuildMarketListings()` → `List<MarketListing>`; `HandleSoldFromMarketBoard(string)` → `bool` | ✅ |
| `CreatePlayerListing` | `MarketListing.cs:79` — **đúng 9 tham số**: `(string listingId, string itemId, int quantity, int pricePerUnit, long createdUtcTicks, long expiresUtcTicks, bool hasLoa, string playerName, int playerLevel)` | `PlayerStallManager.cs:192` truyền `(string, string, int, int, long, long, bool, string, int)` | ✅ đúng thứ tự, đúng kiểu |

**Đăng ký trùng (double-subscribe): KHÔNG có.**
`MarketStallBridgeAdapter` **chỉ** gọi `BasePriceBook.Register()` (chiều A→B) và có comment nói rõ cố ý không đụng chiều B→A.
`PlayerStallManager.RegisterMarketBridge()` **chỉ** gán 2 delegate của cầu nối (chiều B→A). Hai bên không giẫm chân.
`MarketManager.Awake` đăng ký `OnPlayerListingsChanged += HandleProviderChanged` và `OnDestroy` gỡ đúng nó — cân đối.
`PlayerStallManager.OnDestroy` chỉ gỡ 2 delegate của mình, không gọi `Bridge.Clear()` — đúng như đã hẹn, không cắt event của DEV-A.

Thêm một điểm **an toàn ngoài dự kiến**: `MarketManager.TryBuyListing` chặn hàng của chính mình bằng `MarketBuyResult.OwnListing` **trước** mọi bước tiền/kho, nên đường `Bridge.OnPlayerListingSold` thực tế không bao giờ chạy từ bảng tin ⇒ **không có đường in tiền** (không thể vừa cộng vàng người bán vừa không trừ ai).

#### 3 · THAM CHIẾU CHẾT — 🟡 CÒN 1 CHỖ

`MarketShopItemUI.cs` và `ShopItem_Prefab.prefab` (kèm `.meta`) đã xoá sạch; thư mục `Assets/_Game/Prefab/ui/Market/` rỗng; chuỗi `MarketShopItemUI` và `ShopItem` không còn trong bất kỳ scene nào.
Đã dựng lại bảng GUID từ 3.651 file `.meta` rồi quét 1.327 file `.unity`/`.prefab`/`.asset`: **mọi `m_Script` trong cây `Canvas_MarketPopup` đều phân giải được** (`MarketPopupUI.cs`, `MarketManager.cs`, `UIRaycastBlocker.cs` + component UI dựng sẵn của Unity) ⇒ **KHÔNG có Missing Script**.

**Nhưng còn sót đúng 1 dòng** — xem mục CẦN SỬA #1. Câu "không còn ai tham chiếu (đã kiểm bằng GUID)" của DEV-A **chưa đúng**.

#### 4 · DỮ LIỆU — ĐẠT TUYỆT ĐỐI

`Assets/_Game/Farm/data/Market/MarketDatabase.asset`:

| Kiểm | Kết quả |
|---|---|
| Số dòng | **74** (đúng như DEV-A báo) |
| Dòng `TODO_` | **0** |
| Trùng `ItemID` | **0** |
| `BuyPrice ≤ 0` / `MaxQuantity < MinQuantity` / `Weight ≤ 0` | 0 / 0 / 0 |
| **ItemID tra được về vật phẩm THẬT** | **74/74.** Quét 458 file `.asset` trong `_Game`, đối chiếu `CropData.harvestItemId/seedItemId/cropId/itemID`, `InventoryItemData.itemId`, `DishData.dishId`, `PenMiniPanelConfig.productItemId`, `IngredientData.id`. **Không có itemId nào không tra được.** |
| Phân loại hạt giống | Dự án có đúng **21** `CropData.seedItemId`. DB có đúng **21** dòng `Category: 2 (HatGiong)`. Hai tập **trùng khít 100%** — không dòng nào gán nhầm, không thiếu dòng nào. `ca_rot` và `khoai_tay` đều nằm đúng nhóm HatGiong. |
| Phân bố | NôngSản 11 · HạtGiống 21 · Hoa 10 · ChănNuôi 5 · MónĂn 18 · GiaVị 4 · VậtLiệu 5. CheBien = 0 (3 món tắt vì chưa có icon), 2 món cá bị loại — khớp đúng lời DEV-A. |

#### 5 · CHỈ DÙNG VÀNG — ĐẠT

Grep `gem|diamond|kim cương|cỏ bốn lá` trên toàn bộ 31 file: **0 lần gọi `SpendGems`/`AddGems`/`Gems`, 0 `gemRefreshCost`, 0 `diamondPrice`.**
Chỉ còn 3 dòng comment ("không gem") và 1 tên tham số bắt buộc `OnCurrencyChanged(int gold, int gems)` — chữ ký của event có sẵn, không phải luồng tiền.
Mọi khoản chi đều qua `FarmEconomyManager.SpendGold`: làm mới chợ (`MarketManager.RefreshNowWithGold`), mở ô quầy (`TryUnlockSlot`), bật loa (`TryPostListing`). ✅
*(Lưu ý: `gemRefreshCost: 1` và object `Button_RefreshGem` VẪN CÒN trong `SCN_Farm.unity` — xem CẦN SỬA #1.)*

#### 6 · BA LỖI CŨ — ĐÃ SỬA CẢ BA

| Lỗi | Kiểm chứng |
|---|---|
| **1 · Popup tự đóng** | `MarketPopupUI.cs:29-37` — `Start()` **không còn** `popupRoot.SetActive(false)`, chỉ nối `btnClose`. Class giữ lại nên `PopupManager:12`, `MarketClickOpen:9`, `DisableStartupPopupsTool:36,132`, `DemoL1L10Tool:292` không đứt. Tên field `popupRoot` giữ nguyên nên `DisableStartupPopupsTool` đọc `SerializedProperty("popupRoot")` vẫn chạy. ✅ |
| **2 · Mua chùa** | `MarketManager.cs:345-346` — `CanSpendGold` trả **`false`** khi `FarmEconomyManager.Instance == null`. `SpendGold` (cs:356) cũng trả `false`. Thứ tự trong `TryBuyListing` đúng: kiểm đủ tiền → kiểm KHO ĐÍCH tồn tại (cs:290-293) → trừ tiền (cs:295) → cộng kho. Không mất vàng mà không có hàng. `CanSpendGold` trả true khi `amount<=0`, nhưng `TotalPrice` luôn ≥1 (cả `Quantity` lẫn `PricePerUnit` đều `Mathf.Max(1,…)`) ⇒ không lách được. ✅ |
| **3 · Hạt sai kho** | `MarketManager.cs:289` `bool isSeed = MarketPriceTable.IsSeed(...)` → `GiveItemToCorrectStorage` (cs:320) đẩy hạt vào `WarehouseManager.AddItem(id, tên, icon, sl)`, còn lại vào `FarmInventoryManager`. **Không có `StartsWith("seed")` ở bất kỳ đâu.** `IsSeed` tra `Category == HatGiong`, mà `ca_rot`/`khoai_tay` đã được xác nhận nằm đúng nhóm đó (mục 4) ⇒ mua về vào đúng `WarehouseManager`, trồng được. Quầy hàng (`PlayerStallManager.GetSourceStore:454`) dùng **chính hàm đó** ⇒ chợ và quầy không thể lệch kho. ✅ |

#### 7 · CHỐNG MẤT HÀNG (B8) — ĐẠT

Rà 4 đường ra khỏi quầy trong `PlayerStallManager`:

| Đường | Cơ chế | Kết luận |
|---|---|---|
| Đặt lên quầy | `TryPostListing:617` trừ kho **trước** khi tạo listing. Tiền loa trừ **trước** kho; kho từ chối thì hoàn tiền loa ngay (`cs:620-621`). | không sinh listing ma, không giữ tiền oan |
| Huỷ | `TryCancelListing:676` hoàn về `l.SourceStore` — **kho nguồn ghi vào listing lúc đăng bán**, không đoán lại. Hoàn thất bại → `refundPending = true` | không mất |
| Hết hạn | `TickStall:784` cùng cơ chế, cùng `refundPending` | không mất |
| NPC mua | `SellListing:731` — `FarmEconomyManager` null thì **trả false và GIỮ NGUYÊN `Active`**, không đánh dấu Sold | không mất tiền |
| Dọn save | `TrimFinished:863` `if (l.refundPending) continue;` — **không bao giờ xoá dòng còn nợ hàng** | không mất |
| Trả bù | `TickStall:795` thử lại `refundPending` mỗi nhịp + mỗi phiên | không mất |

**Hoàn về ĐÚNG kho: ✅.** `TryGiveBackToStore:534` chọn theo `sourceStoreRaw` lưu trong listing. Đã kiểm chéo: trong dự án **`WarehouseManager` thật sự chỉ chứa hạt giống** — `AddHarvest` đã `[Obsolete]` và không nơi nào gọi; 4 chỗ gọi `WarehouseManager.AddItem` là Shop, Chợ, quà lên cấp (`LevelReward_L*.asset` toàn `seed_*`) và hạt khởi đầu (`seed_rice`, `seed_huong_duong`). ⇒ `GetSellableItems` không bao giờ đẩy nông sản vào nhánh `SeedWarehouse` ⇒ không có đường lạc kho.

#### 8 · SAVE — ĐẠT

**6 key mới, không key nào đụng key đang dùng** (đã grep chuỗi literal toàn dự án, mỗi key chỉ xuất hiện 1 lần tại nơi khai báo):

| Key | Chủ | `saveVersion` |
|---|---|---|
| `MARKET_TIMER_SAVE_VERSION` | `MarketRefreshTimer:26` | **chính nó là version** (`CurrentSaveVersion = 1`) |
| `MARKET_TIMER_NEXT_UTC_TICKS` | `:27` | có, qua key trên |
| `MARKET_TIMER_CYCLE_INDEX` | `:28` | có |
| `MARKET_REFRESH_PAID_COUNT` | `:29` | có |
| `MARKET_REFRESH_PAID_DATE` | `:30` | có |
| `FARM_PLAYER_STALL` | `PlayerStallManager:118` | có, `saveVersion` **trong JSON** + `CurrentSaveVersion = 1` |

Cả hai đều có nhánh chuyển đổi (`MigrateFromLegacy` / comment `if (data.saveVersion < 2)`), và `PlayerStallManager` có chốt `_khongDuocGhi` — save của bản game MỚI HƠN thì **không đọc và không ghi đè**. Đúng chuẩn `WarehouseManager`.
`PLAYER_PROFILE_NAME` / `PLAYER_PROFILE_AVATAR_INDEX` là key CÓ SẴN và chỉ được **đọc**, không ghi. ✅

#### 9 · EDITOR TOOL

| | `Tools ▸ Farm ▸ Chợ` | `Tools ▸ Farm ▸ Quầy Hàng` |
|---|---|---|
| Chạy 2 lần có nhân đôi object? | **Không** — xoá cây cũ rồi dựng lại, và **giữ nguyên component `MarketManager`** (không tạo mới) nên `PopupManager`/`MarketClickOpen` không đứt tham chiếu | **Không** khi object đang bật; **CÓ** khi object bị tắt — xem CẦN SỬA #2 |
| Xoá nhầm object? | Xoá **toàn bộ con của `Canvas_MarketPopup`** (có ghi rõ trong comment) | Xoá cả `Canvas_StallPopup` + `Stall_WorldObject` rồi dựng lại |
| Ghi đè dữ liệu người dùng? | Ghi đè `MarketListingCard_Prefab` + `MarketCategoryTab_Prefab` mỗi lần chạy | **Mất vị trí `Stall_WorldObject`** mỗi lần chạy — xem CẦN SỬA #2 |
| Có `Undo`? | `RegisterFullObjectHierarchyUndo` + `RecordObject` | `RegisterCreatedObjectUndo` + `DestroyObjectImmediate` |
| Chặn Play Mode? | Có (`RunEverything`/`BuildAll`) | **Không** |

---

## 🔴 CHẶN BÀN GIAO (1)

**B1 · Chưa chạy 2 Editor tool ⇒ hiện tại chợ ra TRẮNG và quầy hàng KHÔNG TỒN TẠI.**

Đã đọc trực tiếp `SCN_Farm.unity`:

* `Canvas_MarketPopup` vẫn là **hierarchy CŨ 13 object** — còn nguyên `Panel_Background`, `Button_RefreshGem`, `Text_Timer` (Text legacy). Chưa có `Panel_Dim`, `Rail_Categories`, `Content_Listings`, `MarketBoardUI`.
* `Canvas_StallPopup` · `StallSystem` · `Stall_WorldObject`: **0 kết quả trong toàn scene.**

⇒ Bấm vào chợ sẽ mở một popup không có `MarketBoardUI` (bảng trắng), và không có cách nào mở quầy hàng.

**Việc phải làm trước khi bàn giao** (theo đúng thứ tự):
1. Mở `SCN_Farm.unity`
2. `Tools ▸ Farm ▸ Chợ ▸ 0 · CHẠY TẤT CẢ`
3. `Tools ▸ Farm ▸ Quầy Hàng` → `2 · Dựng TẤT CẢ`
4. Kéo `Stall_WorldObject` từ (0,0) tới vị trí trên bản đồ
5. **Ctrl+S**

---

## 🟠 CẦN SỬA (5)

**S1 · Tham chiếu chết tới prefab đã xoá còn nằm trong scene.**

`Assets\_Game\Scenes\SCN_Farm.unity:70714-70720` (MonoBehaviour `&289397645` = `MarketManager` trên `Popup_Main`):

```yaml
  gemRefreshCost: 1
  shopItemPrefab: {fileID: 3151731457757288887, guid: 7741950547983b84894a528e92a3e10e, type: 3}
  buttonRefreshGem: {fileID: 130118009}
```

GUID `7741950547983b84894a528e92a3e10e` **không còn `.meta` nào mang nó** — đó chính là `ShopItem_Prefab.prefab` DEV-A đã xoá. Đây là **lần duy nhất** trong toàn dự án (đã quét hết `_Game`).

Mức độ: **không gây Missing Script, không gây lỗi lúc chạy** — Unity bỏ qua field không còn tồn tại trên class khi nạp. Nhưng nó chứng minh câu "đã kiểm bằng GUID trong mọi `.unity`/`.prefab`" của DEV-A là **sai**, và dòng `gemRefreshCost` vẫn nằm đó trái với yêu cầu "chỉ dùng vàng".

**Sửa:** chạy `Tools ▸ Farm ▸ Chợ ▸ 0` rồi **Ctrl+S**. Unity ghi lại scene và ba dòng trên biến mất cùng object `Button_RefreshGem`. Không cần sửa code.

---

**S2 · Tool quầy hàng làm MẤT vị trí `Stall_WorldObject`, và nhân đôi object nếu object đang tắt.**

`Assets\_Game\Farm\Editor\StallHierarchyBuilderTool.cs:871-876`

```csharp
GameObject old = GameObject.Find(WorldObjectName);
if (old != null) Undo.DestroyObjectImmediate(old);

var root = new GameObject(WorldObjectName);
Undo.RegisterCreatedObjectUndo(root, "Tạo quầy hàng ngoài map");
root.transform.position = new Vector3(0f, 0f, 0f);
```

Hai vấn đề:
1. Chủ dự án kéo quầy tới chỗ đẹp trên bản đồ → chạy lại tool → **quầy nhảy về (0,0)**. Bước 4 trong hướng dẫn của DEV-B phải làm lại mỗi lần.
2. `GameObject.Find` **chỉ thấy object đang BẬT**. Tắt `Stall_WorldObject` rồi chạy lại tool ⇒ `old == null` ⇒ sinh **object thứ hai** trùng tên. Cùng lỗi ở `BuildPopup:169` và `BuildSystem:120`.

**Sửa tối thiểu** (giữ vị trí + thấy cả object đang tắt):

```csharp
// Tìm cả object đang TẮT, và GIỮ vị trí cũ khi dựng lại
GameObject old = null;
foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
    if (r.name == WorldObjectName) { old = r; break; }

Vector3 viTriCu = old != null ? old.transform.position : Vector3.zero;
if (old != null) Undo.DestroyObjectImmediate(old);

var root = new GameObject(WorldObjectName);
Undo.RegisterCreatedObjectUndo(root, "Tạo quầy hàng ngoài map");
root.transform.position = viTriCu;
```

---

**S3 · Quầy hàng đứng ngoài `PopupManager` ⇒ bẫy mất khoá input đang chờ sẵn.**

`Assets\_Game\Farm\Scripts\Managers\PopupManager.cs:54-62`

```csharp
if (!anyOpen
    && FarmInputLock.IsPopupOpen
    && !FarmInputLock.IsSeedPopupOpen
    && !FarmInputLock.IsMarketPopupOpen
    && !FarmInputLock.IsDraggingSeed
    && !FarmInputLock.IsDraggingSickle)
{
    FarmInputLock.ResetAll();
}
```

`StallPopupUI.OpenPopup:240` gọi `FarmInputLock.RegisterPopupOpen()` ⇒ `IsPopupOpen == true`. Nhưng `IsAnyPopupOpen()` (cs:72-88) **không biết `StallPopupUI`** ⇒ `anyOpen == false` ⇒ nhánh "tự chữa" trên **xoá sạch khoá input mỗi frame trong lúc popup quầy đang mở**: bản đồ pan/zoom xuyên popup, và bấm trúng collider Chợ/Kho/Bếp nằm dưới sẽ mở popup khác chồng lên.

**Hiện tại chưa nổ** vì trong `SCN_Farm.unity` `blockingOverlay: {fileID: 0}` (null) nên `LateUpdate` thoát ở dòng 47 trước khi tới nhánh đó. **Nhưng ngày nào ai đó gán `blockingOverlay` là lỗi bật ngay** — và đây là field đang được để trống chờ gán.

**Sửa tối thiểu 2 dòng, không đụng cấu trúc `PopupManager`** (thêm cờ static, không cần kéo Inspector):

`StallPopupUI.cs` — thêm cờ:
```csharp
/// <summary>Cho PopupManager biết quầy hàng đang mở mà không cần kéo tham chiếu Inspector.</summary>
public static bool AnyOpen { get; private set; }
```
gán `AnyOpen = true;` cuối `OpenPopup()`, `AnyOpen = false;` cuối `ClosePopup()` và trong `OnDisable()`.

`PopupManager.IsAnyPopupOpen()` — thêm 1 vế vào cuối:
```csharp
|| CropProcessPopupUI.AnyOpen
|| StallPopupUI.AnyOpen;     // ← thêm
```

**Kèm theo:** `MarketClickOpen.cs:125` mới chỉ chặn 2 canvas, thiếu canvas quầy hàng —
```csharp
if (parentCanvas != null && (parentCanvas.name == "Canvas_Popup" || parentCanvas.name == "Canvas_MarketPopup"))
```
sửa thành
```csharp
if (parentCanvas != null && (parentCanvas.name == "Canvas_Popup" || parentCanvas.name == "Canvas_MarketPopup" || parentCanvas.name == "Canvas_StallPopup"))
```
(`StallWorldObject.cs:30` đã liệt kê đủ cả ba canvas — chỉ phía chợ thiếu.)

---

**S4 · Nút `−` của quầy hàng sẽ ra Ô VUÔNG RỖNG.**

`Assets\_Game\Farm\Editor\StallHierarchyBuilderTool.cs:559`

```csharp
minus = MakeStepButton(row, "Btn_Minus", "−", new Vector2(0f, 0.5f), new Vector2(48f, -8f));
```

`"−"` là **U+2212 MINUS SIGN**, không phải dấu trừ ASCII. Font mặc định của dự án là `LiberationSans SDF` — **Static, đúng 250 ký tự**, đã kiểm: **không có U+2212**, và `m_fallbackFontAssets: []`. Kết quả: nút giảm hiện `▯`.

Đây đúng là cái bẫy DEV-A đã né cho nút X (`MarketBoardUIBuilder.cs:240-242`: *"Dùng chữ 'X' thường chứ KHÔNG dùng ✕ (U+2715): font mặc định của dự án là LiberationSans, ký tự đó không có trong bộ"*) — DEV-B lại vấp đúng chỗ đó.

**Sửa:** đổi `"−"` → `"-"` (hyphen ASCII, U+002D). Một ký tự.

---

**S5 · Hiệu ứng "ĐÃ BÁN" trên thẻ không bao giờ hiện.**

`Assets\_Game\Farm\Scripts\Market\MarketBoardUI.cs:294-302`

```csharp
MarketBuyResult result = manager.TryBuyListing(listingId);
switch (result)
{
    case MarketBuyResult.Success:
        MarkCardSold(listingId);   // ← luôn không tìm thấy
```

`manager.TryBuyListing` → `provider.MarkListingSold` → `OnListingsChanged` → `HandleProviderChanged` → `OnMarketChanged` → `HandleMarketChanged` → **`Redraw(true)` đã chạy xong** trước khi hàm trả về. Lúc `MarkCardSold` chạy thì listing đã bị lọc khỏi danh sách và các thẻ đã đổi `listingId` ⇒ vòng lặp `MarketBoardUI.cs:326-333` không khớp id nào ⇒ **no-op**. Comment ở cs:299 ("thẻ mờ đi tại chỗ trước") mô tả hành vi không tồn tại.

Không gây lỗi, nhưng mất phản hồi thị giác khi mua. **Sửa:** gọi `MarkCardSold(listingId)` **trước** `manager.TryBuyListing(...)` không được (chưa biết kết quả) ⇒ cách gọn nhất là bỏ `MarkCardSold` và để `Redraw` lo, hoặc hoãn `Redraw` một frame. Ưu tiên thấp.

---

## 🔵 GÓP Ý (6)

**G1 · Font không có dấu tiếng Việt — ảnh hưởng TOÀN BỘ chữ của cả hai popup.**
`TMP Settings.m_defaultFontAsset` = `LiberationSans SDF`, `m_AtlasPopulationMode: 0` (**Static**), 250 ký tự, `m_fallbackFontAssets: []`. Đã kiểm từng codepoint: **không có** `ầ`(U+1EA7), `ắ`(U+1EAF), `đ`(U+0111), `Đ`(U+0110). Cả hai builder tạo `TextMeshProUGUI` mà **không gán font** ⇒ dùng font mặc định này ⇒ "QUẦY HÀNG", "Bán vật phẩm", "CHƯA CÓ VẬT PHẨM NÀO ĐƯỢC ĐĂNG BÁN", tên người bán, tên món… đều thiếu dấu.
Đây là **tình trạng CÓ TỪ TRƯỚC** (229/230 TMP component trong `SCN_Farm` dùng đúng font này), **không phải lỗi của hai dev**. Nhưng nó chặn thẳng ô "Mọi thẻ đều có icon và **tên thật**" ở mục 8. Sửa gọn: import 1 font Việt (vd Be Vietnam Pro/Nunito), tạo Font Asset, đặt làm mặc định trong TMP Settings — hoặc tối thiểu chuyển `LiberationSans SDF` sang **Dynamic** và thêm fallback.

**G2 · `PopupManager.marketPopup: {fileID: 0}`** — `MarketPopupUI` chưa từng được kéo vào `PopupManager` trong scene. Có sẵn từ trước, và hiện vô hại vì `MarketManager` tự đặt `FarmInputLock.IsMarketPopupOpen`. Nhân lúc mở scene chạy tool thì kéo luôn.

**G3 · `LocalMarketProvider.GetListings` cấp phát mỗi lần vẽ.** `MarketPlayerListingBridge.FetchActiveListings()` → `BuildMarketListings()` tạo **một `List` mới + N `MarketListing` mới mỗi lần gọi**, mà `GetListings` được gọi mỗi lần `Redraw`. Với vài chục món thì không sao; nếu sau này quầy nhiều hàng thì nên cache theo dấu thời gian `OnStallChanged`.

**G4 · `TickStall` bỏ qua nhánh hết hạn khi bán thất bại.** `PlayerStallManager.cs:774-778`: `if (npcMua) { if (SellListing(l)) changed = true; continue; }` — `SellListing` trả false (thiếu `FarmEconomyManager`) vẫn `continue`, nên listing đó không được xét hết hạn ở nhịp này. Chỉ xảy ra khi thiếu manager ví (không xảy ra ở `SCN_Farm`), và **không mất hàng** — chỉ treo. Đổi `continue` thành `if (SellListing(l)) { changed = true; continue; }` là hết.

**G5 · `MarketStallBridgeAdapter` không bao giờ `Unregister`.** Provider là `static readonly` không giữ `MonoBehaviour` nào nên không rò bộ nhớ. Không cần sửa, ghi lại để sau này ai đọc khỏi lo.

**G6 · Tool chợ ghi đè 2 prefab thẻ/tab mỗi lần chạy** (`MarketBoardUIBuilder.cs:572, 627` — `PrefabUtility.SaveAsPrefabAsset`). Sau khi chủ dự án gắn art vào `MarketListingCard_Prefab`, chạy lại menu `3` hoặc `0` sẽ **xoá sạch art đó**. Nên ghi cảnh báo này vào hộp thoại của tool. Tool quầy hàng cũng vậy với `PF_StallSlot` / `PF_StallPickCell` / `Canvas_StallPopup.prefab`.

---

### KẾT LUẬN

**Chất lượng code: cao.** Không lỗi biên dịch, không trùng kiểu, không rò type Editor sang runtime, hợp đồng A↔B khớp từng chữ ký, dữ liệu 74/74 tra được về vật phẩm thật, chống mất hàng kín cả 6 đường, save có version và có chốt chống ghi đè.

**Chưa bàn giao được** vì **B1** (chưa chạy tool ⇒ scene chưa có gì) và 5 mục **CẦN SỬA** — trong đó **S3** và **S4** cần sửa code (tổng cộng ~8 dòng), **S1** tự hết khi chạy tool + Ctrl+S, **S2** nên sửa để tool dùng lại được nhiều lần, **S5** ưu tiên thấp.

**TESTER không sửa file nào** — không tìm thấy lỗi chặn biên dịch, đúng theo phạm vi được giao.

---

## 7. CHỐT GIAO DIỆN CHUNG

*(Hai dev thống nhất tên class/hàm ở đây TRƯỚC KHI code)*

| Bên cung cấp | Tên | Chữ ký | Chốt chưa |
|---|---|---|---|
| DEV-A | bảng giá gốc | `MarketPriceTable` (static) — DEV-B gọi thẳng | ✅ ĐÃ NỐI XONG |
| DEV-B | danh sách hàng người chơi | `MarketPlayerListingBridge.GetActiveListings` — DEV-B đã gán | ✅ ĐÃ NỐI XONG |

> **Cập nhật 2026-08-09 (DEV-B).** DEV-A đã ship `MarketPriceTable` và
> `MarketPlayerListingBridge` trước khi tôi tới chỗ nối. **Tôi bỏ contract tạm của mình
> và dùng đúng contract của DEV-A** — một hợp đồng tốt hơn hai hợp đồng cạnh tranh.
> Cả hai chiều đã được nối và đang biên dịch sạch.

---

### 7.1 DEV-B → dùng bảng giá của DEV-A (chiều A ⟶ B) ✅

DEV-B gọi thẳng `MarketPriceTable` (static, không cần manager, không cần đăng ký):

| DEV-B gọi | Dùng cho |
|---|---|
| `MarketPriceTable.GetSuggestedUnitPrice(itemId)` | giá gợi ý ở bộ chỉnh giá (B5) |
| `MarketPriceTable.GetMinPlayerUnitPrice(itemId)` | chặn dưới → nút `−` chuyển XÁM |
| `MarketPriceTable.GetMaxPlayerUnitPrice(itemId)` | chặn trên → nút `+` chuyển XÁM |
| `MarketPriceTable.GetBasePrice(itemId)` | giá gốc, qua `BasePriceBook` |
| `MarketPriceTable.Has(itemId)` / `.IsSeed(itemId)` | **chọn ĐÚNG kho** khi trừ/hoàn hàng (B8) |

`MarketPriceTable.IsSeed` là chỗ duy nhất phân loại đúng `ca_rot` và `khoai_tay` —
hai hạt giống không có tiền tố `seed_`. Quầy hàng dùng nó làm nguồn sự thật để hàng
hoàn về không lạc kho (đúng cạm bẫy của LỖI 3).

**`BasePriceBook.cs` (DEV-B sở hữu) giữ lại làm lớp bọc chịu lỗi**, thứ tự tra:
`provider cắm ngoài` → **`MarketPriceTable` (chính thức)** → `StallItemCatalog` (asset thật)
→ bảng dự phòng cứng → `10`.
⇒ Nếu DEV-A thêm/bớt dòng trong bảng giá, quầy hàng tự theo, không sửa dòng nào.
Bậc dự phòng chỉ chạy cho itemId mà bảng của DEV-A chưa có.

---

### 7.2 DEV-A → nhận hàng người chơi từ DEV-B (chiều B ⟶ A) ✅

**DEV-A KHÔNG PHẢI LÀM GÌ THÊM.** DEV-B đã gán hai delegate trong
`PlayerStallManager.Awake()` đúng như hướng dẫn ở `IMarketProvider.cs`:

```csharp
MarketPlayerListingBridge.GetActiveListings   = BuildMarketListings;      // → List<MarketListing>
MarketPlayerListingBridge.OnPlayerListingSold = HandleSoldFromMarketBoard; // (listingId) => bool
```

- Chuyển đổi qua `MarketListing.CreatePlayerListing(...)`, giữ nguyên `listingId`,
  `createdUtcTicks`, `expiresUtcTicks`, `hasLoa` → thẻ ở bảng tin hiện đúng dữ liệu thật.
- `MarketPlayerListingBridge.NotifyChanged()` được gọi **mỗi lần quầy đổi**
  (đăng bán · huỷ · hết hạn · NPC mua · mở ô).
- Đăng ký ở **Awake**, không phải Start — bảng tin mở sớm vẫn thấy hàng.
- `OnDestroy` chỉ gỡ ĐÚNG hai delegate của DEV-B, **cố tình không gọi `Bridge.Clear()`**
  vì hàm đó xoá luôn event `OnPlayerListingsChanged` mà UI của DEV-A đang nghe.

**API trực tiếp của DEV-B** (dùng nếu cần, không bắt buộc):

```csharp
PlayerStallManager.Instance.GetActiveListings()          // IReadOnlyList<PlayerListing>
PlayerStallManager.Instance.GetActiveListingsWithLoa()   // hàng có loa
PlayerStallManager.Instance.TryBuyListing(id, out err)   // chốt bán + cộng vàng
PlayerStallManager.Instance.OnStallChanged               // event Action
PlayerStallManager.Instance.OnListingSold                // event Action<PlayerListing,int>
```

⚠️ `PlayerStallManager.Instance` có thể null ở scene chưa đặt quầy (vd `SCN_Home`) —
luôn kiểm tra null. Đi qua `MarketPlayerListingBridge` thì đã an toàn sẵn.

---

### 7.3 Ràng buộc DEV-A cần biết (ảnh hưởng bảng tin)

| Điều | Giá trị | Vì sao |
|---|---|---|
| Không có đồng tiền thứ ba | Loa và mở ô quầy đều trừ **VÀNG** (`FarmEconomyManager.SpendGold`) | Chủ dự án chốt ở mục 0.2 |
| Canvas quầy hàng | `Canvas_StallPopup`, reference resolution **1920×1080** | Khớp `Canvas_MarketPopup` đang có |
| Khoá input | Quầy hàng dùng `FarmInputLock.RegisterPopupOpen/Close` | Không đụng `PopupManager` để tránh xung đột với DEV-A |
| Kho | Nông sản/món/chăn nuôi ⇒ `FarmInventoryManager`; hạt giống ⇒ `WarehouseManager` | Kho nguồn được **ghi vào từng listing** lúc đăng bán nên hoàn hàng luôn về đúng chỗ |

---

### 7.4 DEV-A xác nhận (2026-08-09) ✅

Đã đọc 7.1–7.3 của DEV-B và **theo đúng hợp đồng đó**, không áp thêm cái nào của mình.

| Điểm chạm | Trạng thái |
|---|---|
| `MarketPriceTable` — DEV-B gọi thẳng | ✅ Đã ship, 74 vật phẩm có giá. Mọi hàm ở 7.1 đều tồn tại đúng chữ ký. |
| `MarketPriceTable.IsSeed()` | ✅ Là chỗ DUY NHẤT phân loại hạt giống. `MarketManager.GiveItemToCorrectStorage` cũng dùng chính nó ⇒ chợ và quầy chọn kho **giống hệt nhau**, không thể lệch. |
| `MarketPlayerListingBridge` | ✅ `LocalMarketProvider.GetListings()` gọi `FetchActiveListings()` mỗi lần vẽ. `MarketManager` nghe `OnPlayerListingsChanged` ⇒ quầy đổi là bảng tin vẽ lại ngay. |
| `MarketListing.CreatePlayerListing` | ✅ Giữ nguyên chữ ký DEV-B đang gọi. |
| Hàng người chơi trên bảng tin | ✅ Xếp **trước** hàng NPC (sau hàng có loa), gắn nhãn xanh **"CỦA BẠN"**, **không cho tự mua lại** (`MarketBuyResult.OwnListing`). |

**Một file DEV-A thêm cho chiều A ⟶ B:** `Farm/Scripts/Market/MarketStallBridgeAdapter.cs`
— cắm `MarketPriceTable` vào `BasePriceBook.Register()` qua `[RuntimeInitializeOnLoadMethod]`
(không phải MonoBehaviour: `MarketManager` nằm trên popup tắt sẵn nên `Awake` của nó chỉ chạy
lúc mở chợ, quá muộn cho quầy hàng). Đây chỉ là **lớp bọc ngoài** — `BasePriceBook` đã gọi
thẳng `MarketPriceTable` ở bậc 2 nên bỏ file này đi chợ và quầy vẫn ra cùng một giá.

**Hai bên KHÔNG đụng file của nhau.** DEV-A chỉ sửa trong `Scripts/Market/`, `Editor/Market*`,
`data/Market/`; không chạm `Scripts/Stall/`, `PopupManager`, `FarmInputLock`.

---

## 8. BÀN GIAO

TESTER kiểm trước khi bàn giao.

**Ký hiệu:** `[x]` = TESTER đã xác minh · `[~]` = code/dữ liệu đúng nhưng **phải chạy 2 Editor tool + Play mới xác nhận được** · `[ ]` = chưa đạt.

> ⚠️ Môi trường của TESTER **không có Unity Editor** ⇒ mọi ô đều kiểm bằng **rà soát tĩnh**
> (đọc code, đối chiếu chữ ký, phân tích YAML scene/asset, tra GUID). Ô `[~]` là ô mà logic đã
> đúng nhưng kết quả cuối phụ thuộc bước dựng hierarchy — xem **CHẶN BÀN GIAO · B1** ở mục 6.

- [x] **Biên dịch sạch, không lỗi** — 31 file / 8.823 dòng: ngoặc cân bằng, `#if↔#endif` khớp, `using` đủ, **0 trùng tên kiểu** (CS0101) trên 314 kiểu toàn dự án, **0 type Editor bị runtime tham chiếu**, mọi ký hiệu ngoài (`FarmEconomyManager`/`WarehouseManager`/`FarmInventoryManager`/`MissionProgressTracker`/`CropData`/TMP…) đều tồn tại đúng chữ ký. Không tìm thấy lỗi chặn biên dịch nào ⇒ **không sửa file nào**.
- [x] **Mở chợ không tự đóng (LỖI 1)** — `MarketPopupUI.cs:29-37`, `Start()` không còn `popupRoot.SetActive(false)`; class giữ lại nên 5 nơi tham chiếu không đứt, tên field `popupRoot` giữ nguyên cho `DisableStartupPopupsTool`.
- [x] **Hết vàng thì không mua được (LỖI 2)** — `MarketManager.cs:345` trả `false` khi thiếu ví; thứ tự *kiểm tiền → kiểm kho đích → trừ tiền → cộng kho* đúng; `TotalPrice` luôn ≥ 1 nên không lách qua nhánh `amount<=0`.
- [x] **Mua hạt giống ở chợ → vào `WarehouseManager`, trồng được (LỖI 3)** — phân loại bằng `MarketPriceTable.IsSeed` (tra danh mục), **không có `StartsWith("seed")` ở bất kỳ đâu**. Đã đối chiếu: 21/21 `CropData.seedItemId` của dự án trùng khít 21 dòng `HatGiong` trong asset, gồm cả `ca_rot` và `khoai_tay`. Quầy hàng dùng chính hàm đó ⇒ hai bên không thể lệch kho.
- [x] **Không còn chữ `TODO_` nào hiện ra** — `MarketDatabase.asset`: **74 dòng, 0 `TODO_`, 0 trùng ItemID**. Thêm một lớp chặn nữa ở `LocalMarketProvider.cs:78`.
- [~] **Mọi thẻ đều có icon và tên thật, không có icon trắng** — **74/74 ItemID tra được về vật phẩm THẬT** (đối chiếu 458 asset: `CropData`, `InventoryItemData`, `DishData`, `PenMiniPanelConfig`, `IngredientData`); tool đã lọc sẵn 5 món thiếu icon. ⚠️ Nhưng **tên sẽ THIẾU DẤU tiếng Việt** vì font mặc định `LiberationSans SDF` là Static/250 ký tự, không có `ầ ắ đ Đ`, fallback rỗng — **lỗi có từ trước, không phải của hai dev** (xem GÓP Ý · G1).
- [~] **Lọc danh mục chạy đúng** — `LocalMarketProvider.TryAdd` lọc theo `listing.Category` → `MarketPriceTable.GetCategory`; 8 tab trong `FilterOrder` phủ đúng 7 danh mục có dữ liệu (CheBien = 0 dòng, đã cố ý bỏ tab). Cần bấm thử sau khi dựng UI.
- [x] **Làm mới trừ vàng, không trừ gem** — grep toàn bộ 31 file: **0 `SpendGems`/`AddGems`/`gemRefreshCost`/`Diamond`**. Chi phí đi qua `FarmEconomyManager.SpendGold`, luỹ tiến 150→300→450 (trần 900), miễn phí **chỉ khi** hết giờ. ⚠️ `gemRefreshCost: 1` + object `Button_RefreshGem` **vẫn còn trong `SCN_Farm.unity`** cho tới khi chạy tool + Ctrl+S (CẦN SỬA · S1).
- [~] **Timer đúng sau khi thoát và mở lại game** — mốc lưu bằng `DateTimeOffset.UtcNow.UtcTicks` vào PlayerPrefs có `saveVersion`, `Tick()` bù **nhiều chu kỳ một lúc** (trần 10.000) và chặn mốc "quá xa tương lai". Logic đúng; cần một lần thoát/vào để chốt.
- [x] **Đăng bán ở quầy → trừ kho · huỷ → hoàn kho · hết hạn → hoàn kho** — rà đủ 6 đường: trừ kho **trước** khi tạo listing; hoàn về `SourceStore` **ghi trong từng listing** (không đoán lại); hoàn thất bại → `refundPending` và thử lại mỗi nhịp + mỗi phiên; `TrimFinished` **không bao giờ xoá dòng còn nợ hàng**; bán mà chưa cộng được vàng thì **giữ nguyên `Active`**. Đã kiểm chéo: `WarehouseManager` trong dự án thật sự **chỉ chứa hạt giống** (`AddHarvest` đã `[Obsolete]`, quà lên cấp và hạt khởi đầu đều là `seed_*`) ⇒ **không có đường lạc kho**.
- [~] **Hàng đăng bán hiện ở bảng tin chợ** — hợp đồng khớp **từng chữ ký**: `MarketPlayerListingBridge.GetActiveListings`/`OnPlayerListingSold` đúng kiểu `Func<List<MarketListing>>` và `Func<string,bool>`; `CreatePlayerListing` **đúng 9 tham số, đúng thứ tự, đúng kiểu**; **không có double-subscribe** giữa `MarketStallBridgeAdapter` và `PlayerStallManager`. Hàng người chơi được xếp trước hàng NPC, gắn nhãn "CỦA BẠN", chặn tự mua lại.
- [~] **NPC mua hàng → có vàng về** — `npcBuyAtUtcTicks` quay số ngay lúc đăng bán (mốc UTC tuyệt đối ⇒ offline vẫn đúng), xét **NPC mua trước, hết hạn sau**. Không có đường in tiền: bảng tin chặn hàng của chính mình bằng `MarketBuyResult.OwnListing` **trước** mọi bước tiền/kho.
- [ ] **Cả hai popup dựng bằng prefab, sửa được trong Editor** — code đúng (không `new GameObject()` lúc runtime, chỉ `Instantiate` prefab), **nhưng scene hiện tại CHƯA CÓ**: `Canvas_MarketPopup` vẫn là hierarchy cũ 13 object, và `Canvas_StallPopup`/`StallSystem`/`Stall_WorldObject` **không tồn tại** (0 kết quả trong `SCN_Farm.unity`).

---

### 🚦 KẾT LUẬN BÀN GIAO — **CHƯA ĐẠT** (còn 1 việc bắt buộc + 5 mục cần sửa)

**Việc bắt buộc trước khi bàn giao:**

1. Mở `SCN_Farm.unity`
2. `Tools ▸ Farm ▸ Chợ ▸ 0 · CHẠY TẤT CẢ`
3. `Tools ▸ Farm ▸ Quầy Hàng` → `2 · Dựng TẤT CẢ`
4. Kéo `Stall_WorldObject` từ (0,0) tới vị trí trên bản đồ
5. **Ctrl+S** → xong bước này thì **S1 tự hết** và 5 ô `[~]` chuyển sang kiểm được bằng Play

**Cần sửa code (~8 dòng, chi tiết ở mục 6):** **S3** (quầy hàng đứng ngoài `PopupManager` ⇒ bẫy mất khoá input) · **S4** (nút `−` dùng U+2212 ⇒ ra ô vuông rỗng) · **S2** (tool làm mất vị trí quầy) · **S5** (ưu tiên thấp).

**Không có lỗi chặn biên dịch ⇒ TESTER không sửa file nào.**
