# MÁY XAY THỨC ĂN — CHUYỂN SANG KÉO-THẢ + HIỆU ỨNG BAO THÀNH PHẨM

**Ngày:** 21/08/2026 · **Phiên:** Tech Lead + 6 sub-agent · **Trạng thái:** `REVIEW` (code xong, chờ 1 lượt trong Unity Editor)

Công trình liên quan: `MayThucAn_Anim` (prefab `Assets/Assetsgame/Nhà/BUIDING_ANIM/MayThucAn_Anim.prefab`, instance trong `SCN_Farm`).
Popup: `MillPopup_Root` dưới `Canvas_Popup` (canvas lồng, `overrideSorting`, `sortingOrder = 400`).

---

## 0. VIỆC CẦN BẠN LÀM (làm một lượt là xong)

| # | Việc | Ở đâu | Ghi chú |
|---|------|-------|---------|
| 1 | Mở Unity, đợi compile xong, xem Console có lỗi đỏ nào không | — | 6 script mới + 3 script sửa. 0 lỗi đỏ là điều kiện tiên quyết. |
| 2 | Chạy `Tools/Farm/Popup May Xay/1. Dung Popup (Scene + Prefab)` | menu Unity | Tool tự dựng lại hierarchy: thêm `Drop_Highlight` cho 5 slot, thêm `Bag_Glow`, gắn + wire 3 component hiệu ứng, gắn `MillRecipeDragSource` vào prefab card. |
| 3 | Chạy `Tools/Farm/Popup May Xay/3. Kiem Tra (bao cao)` | menu Unity | Đọc báo cáo. Mọi field phải là `ĐÃ WIRE`. Nếu có `CHƯA WIRE` thì báo lại, đừng wire tay. |
| 4 | Vào Play Mode, bấm vào máy xay, **kéo** một card công thức sang **thả** vào slot trống | Play Mode | Xem mục 4 bên dưới để biết chính xác phải thấy gì. |
| 5 | Kiểm cảm giác kéo trên **chuột** và trên **màn hình cảm ứng / Device Simulator** | Play Mode | Đây là thứ duy nhất không mô phỏng được — xem mục 5 (ngưỡng trục kéo). |
| 6 | Nếu ưng, tự commit | git | Đội **không** commit gì (AUTONOMY §3.4). Bản gốc 4 file đã backup ở `production/backup-mill-21-08/`. |

**Chưa làm và cố ý không làm:**

- Không sửa `.unity` / `.prefab` bằng tay — mọi thay đổi hierarchy đi qua Editor Tool (AUTONOMY §3.1).
- Không commit, không push (AUTONOMY §3.4).
- Không thêm tab. Xem mục 1.

---

## 1. VỀ 3 TAB TRONG VIDEO — KHÔNG CÓ GÌ PHẢI XOÁ

Yêu cầu ban đầu: *"xóa tab máy làm mía và làm nước mắm"*.

Kiểm tra thực tế trong Unity: **popup không có tab nào cả, chưa từng có.**

- `MillPopupUI.cs` dòng 19–23 ghi rõ: *"Video có 3 tab … Chủ dự án xác nhận đó là LỖI THIẾT KẾ — mỗi máy một popup riêng … KHÔNG có field tab, KHÔNG có code tab chết. Đừng 'khôi phục theo video'."*
- `MillPopupBuilderTool.cs` không dựng node `Tab_*` nào; hai sprite `tab_active.png` / `tab_inactive.png` tồn tại trong `ui_mill_assets/generated_sprites/` nhưng **không được dùng**.
- `MillPopup_Root.prefab` có 0 node tên `Tab*`.
- `MillConfig` không có field category/tab; cả 4 công thức đều thuộc nhóm thức ăn gia súc.

Video và 4 ảnh gửi kèm là **bản mockup HTML** (`Assets/Assetsgame/popup/ui_mill_assets/full_mill_ui.html` và bản tương tác dẫn xuất từ nó), không phải build Unity — nhận ra qua nền xanh `#2D4329`, dải chú thích *"1 Máy xay thức ăn gia súc · bánh răng lớn quay xuôi · bánh nhỏ ngược chiều, nhanh hơn 40%"* ở góc trên, và 3 tab.

**Sếp đã chốt: giữ nguyên không tab.** Nếu sau này làm máy xay mía / máy nước mắm thì mỗi máy một popup riêng, không nhồi tab.

---

## 2. LUỒNG MỚI

### Trước

```
chọn card  →  bấm nút xanh "XAY NGAY"  →  máy tự chọn slot trống đầu tiên  →  xay
xong  →  bấm THU  →  cộng kho (không có hiệu ứng gì)
```

### Sau

```
KÉO card ra khỏi danh sách   →  cả hàng slot nhận được sáng viền xanh
THẢ vào một slot trống       →  trừ nguyên liệu, slot bắt đầu đếm ngược
                             →  một loạt hạt nguyên liệu bay từ bong bóng vào phễu máy
                             →  thân máy nhún một nhịp
xay xong                     →  bao ở vòng tròn đầu ra NẢY một nhịp + loé sáng
                             →  vòng sáng thở nhè nhẹ suốt lúc còn hàng chưa thu
bấm THU                      →  cộng kho, 3 icon sản phẩm bung ra rồi BAY về nút KHO ở HUD
```

Chuỗi gọi (file:hàm):

| Bước | Nơi phát | Nơi xử |
|---|---|---|
| Nhấc bao | `MillRecipeDragSource.OnBeginDrag` | `MillPopupUI.BatDauKeo` → chọn card + `SangVienSlot(true)` |
| Bóng chạy theo tay | `MillRecipeDragSource.OnDrag` | `MillDragSession.Theo` |
| Thả | `MillSlotUI.OnDrop` | `MillPopupUI.ThaVaoSlot(idx, r)` ← **chỗ chặn thật** |
| Bắt đầu xay | — | `MillPopupUI.BatDauXay(idx, r)` → `TruNguyenLieu` → ghi `SlotState` → `LuuTrangThai` → `MillIntakeFX.Chay` |
| Nhả tay | `MillRecipeDragSource.OnEndDrag` | `MillPopupUI.KetThucKeo(aiNhan)` → tắt viền, toast nếu thả trượt |
| Mẻ xong | `MillPopupUI.Update` (đối chiếu `soChoThu` với frame trước) | `MillOutputBagFX.PhatRoi()` một lần + `DatSanSang(true)` |
| Thu | `MillSlotUI.btnCollect` | `MillPopupUI.BamThu` → `CongSanPham` → `MillCollectFlyFX.Bay` |

**Nút "XAY NGAY" đã bỏ.** Node `Btn_Main` giữ nguyên tên/vị trí/sprite nhưng thành **bảng gợi ý** không bấm được, chữ đổi theo tình huống:

| Tình huống | Chữ trên bảng | Màu nền |
|---|---|---|
| Sẵn sàng | `KÉO VÀO SLOT ĐỂ XAY` | xanh `#82C94F` |
| Chưa chọn công thức | `CHỌN MỘT CÔNG THỨC` | be `#D9CDB9` |
| Không đủ nguyên liệu | `THIẾU NGUYÊN LIỆU` | be |
| 5 slot đều bận | `HẾT SLOT TRỐNG` | be |

Vì sao tắt `btnMain.enabled` chứ không đặt `interactable = false`: `interactable = false` kích hoạt Disabled tint của `Button` (tool đặt disabled alpha = 0.55) ⇒ bảng gợi ý bị mờ như đang lỗi, và màu do `CapNhatNutLon` tô sẽ bị `Button` tô đè. Tắt hẳn component thì màu giữ nguyên và click cũng không vào.

---

## 3. FILE ĐÃ THAY ĐỔI

### Mới — `Assets/_Game/Farm/Scripts/MillPopup/`

| File | Việc |
|---|---|
| `MillDragSession.cs` | Phiên kéo static: đang cầm công thức nào, ngón tay nào giữ, và cái bóng bao chạy theo con trỏ (tái dùng, không Instantiate mỗi lần). |
| `MillRecipeDragSource.cs` | Gắn trên card. **Phân xử trục kéo**: kéo dọc = cuộn danh sách, kéo ngang/chéo = nhấc bao. Xem mục 5. |
| `MillRectUtil.cs` | 2 phép biến đổi RectTransform mà cả 3 file FX đều cần — `TamWorld` và `DoiPivotVeGiua`. Xem mục 6, đây là chỗ suýt sinh 3 bug im lặng. |
| `MillIntakeFX.cs` | Hạt nguyên liệu bay vào phễu (pool, không cấp phát) + nhún thân máy. |
| `MillOutputBagFX.cs` | Bao nảy một nhịp khi vừa xong (sự kiện) + vòng sáng thở khi còn hàng (trạng thái). Hai thứ tách riêng có lý do — xem doc trong file. |
| `MillCollectFlyFX.cs` | Icon bay từ slot về nút `Tab_Warehouse` ở `Canvas_HUD`. UI thuần, không dùng `HarvestFeedbackSpawner` — xem mục 6. |

### Sửa

| File | Sửa gì | Có phá tương thích? |
|---|---|---|
| `MillPopupUI.cs` | Bỏ `BamXayNgay`; thêm `BatDauKeo` / `KetThucKeo` / `ThaVaoSlot` / `SlotNhanDuoc` / `BatDauXay` / `SangVienSlot`; `Update` phát nhịp bao; `Close` dọn phiên kéo + icon bay; 3 field FX mới. | Không. `Instance` / `AnyOpen` / `IsOpen` / `Open` / `Close` / `HienToast` giữ nguyên chữ ký — `PopupManager`, `PopupGateDebugF9`, `MillBuildingClick` không phải sửa. |
| `MillSlotUI.cs` | Hiện thực `IDropHandler` + `IPointerEnter/ExitHandler`; thêm `OnDropRecipe` / `CoTheNhanTha` / `dropHighlight` / `SetDropHighlight`; `Awake` bật `imgBg.raycastTarget`. | Không. 5 `MillSlotMode` và mọi `Bind*` giữ nguyên. |
| `MillRecipeCardUI.cs` | Thêm `IconSprite`; **`blocksRaycasts` giờ luôn `true`** (trước là `= unlocked`). | Không. Xem mục 7 — đây là sửa một bug có thật. |
| `MillPopupBuilderTool.cs` | +281 dòng, −6. Thêm 2 sprite sinh tự động, node `Drop_Highlight` × 5, node `Bag_Glow`, gắn + wire 3 FX + `MillRecipeDragSource`, đổi chữ mặc định `Btn_Main`, mở rộng `MillAudit`. | Không đổi tên node nào, không đổi tên field nào, không đổi hằng layout nào ngoài `BagGlowSize`. |

Backup 4 file gốc: `production/backup-mill-21-08/*.bak`.

---

## 4. NGHIỆM THU (mục 0 việc #4) — phải thấy đúng những thứ này

1. **Kéo card "Cám cho gà" sang ngang** → bóng bao 64px chạy theo con trỏ, **nằm trên** cả cửa sổ popup (không bị viewport danh sách cắt).
2. Ngay lúc nhấc → **cả hàng slot đã mở và đang trống** hiện viền xanh mờ (alpha 0.45). Đưa con trỏ lên một slot → viền slot đó **đậm lên** (alpha 1).
3. **Thả vào slot trống** → toast `Đã cho Cám cho gà vào slot 1`; slot chuyển sang đếm ngược `1p59`; nguyên liệu trong kho **giảm đúng** 3 lúa + 2 ngô.
4. Cùng lúc đó → ~6 hạt lúa bay vòng cung từ bong bóng `x3` bên trái vào phễu máy, **thân máy nhún** một nhịp (bẹp dọc, bè ngang). Hạt **không nhô ra ngoài** khung máy.
5. **Thả vào slot đang xay** → toast `Slot #2 đang có hàng`, không trừ nguyên liệu. **Thả ra chỗ trống** → toast `Thả bao vào một SLOT TRỐNG để máy xay`.
6. **Kéo dọc** trong danh sách công thức → danh sách **cuộn**, KHÔNG nhấc bao. Kéo dọc khi đặt tay lên card khoá ("Cám cho bò sữa", mở ở cấp 14) → vẫn cuộn được.
7. **Xay xong** → bao ở vòng tròn đầu ra nảy lên ~26px rồi rơi lại **đúng chỗ cũ** (không lệch), loé sáng, sau đó vòng sáng thở chu kỳ ~1.15s. Vệt sáng **không tràn** ra nền kem của panel.
8. **Bấm THU** → 3 icon sản phẩm bung ra từ **giữa thẻ slot** (không phải từ góc), bay vòng cung về nút **KHO** góc dưới-trái HUD, nhỏ dần rồi mờ. Kho tăng đúng 1 Cám gà.
9. **Bấm THU khi túi nông sản đầy** → toast `Túi nông sản đã đầy…`, slot **vẫn giữ hàng**, **không có** icon nào bay (không được để thấy hàng bay vào kho mà kho không tăng).
10. **Đóng popup ngay sau khi bấm THU** → icon đang bay **biến mất hết**, không còn cái nào lơ lửng trên mặt đồng.
11. **Đóng popup giữa lúc đang kéo** (bấm X bằng tay kia) → bóng bao biến mất, không sót trên màn hình.
12. Mở lại popup → mọi slot vẽ đúng trạng thái; danh sách công thức **vẫn cuộn được** (kiểm cờ `m_Dragging` của ScrollRect không bị treo).

---

## 5. THỨ DUY NHẤT CẦN SẾP CHỈNH BẰNG CẢM GIÁC

`MillRecipeDragSource.nguongTruc` — mặc định `1.0` (chia đôi 45°).

- Kéo mà `|Δy| > |Δx| × nguongTruc` ⇒ hiểu là **cuộn danh sách**.
- Còn lại ⇒ **nhấc bao**.

Khu slot nằm **bên phải** danh sách nên "kéo ngang" là cử chỉ tự nhiên để mang bao qua đó. Nếu Sếp thử thấy:

- **khó lấy bao ra** (hay bị cuộn oan) → tăng lên `1.4`–`1.8`;
- **hay nhấc bao oan lúc muốn cuộn** → giảm xuống `0.6`–`0.8`.

Sửa trong Inspector của prefab `Assets/_Game/Farm/Prefabs/Mill/MillRecipeCard.prefab`. Chốt xong nói lại để đội đưa vào `MillConfig` cho hết hardcode.

Ghi chú: danh sách **ngắn hơn viewport** (không có gì để cuộn) thì phép phân xử bị bỏ hẳn — mọi hướng kéo đều là nhấc bao. Hiện có 4 công thức, `Content` cao hơn viewport 379px nên phép phân xử **đang có hiệu lực**.

---

## 6. BA CẠM BẪY ĐÃ CHẶN (ghi lại để lần sau khỏi mắc)

### 6.1 Pivot ở góc — 3 bug im lặng cùng một gốc

`MillPopupBuilderTool` neo node bằng `TL/TR/BL/BR`, và các helper đó đặt **pivot vào đúng góc** đó. Hậu quả:

- `rt.position` trả về vị trí của **pivot**, không phải tâm ⇒ icon THU sẽ bung ra từ **mép trên-trái** thẻ slot, lệch ~(59, +90); hạt nguyên liệu sẽ bắn từ mép bong bóng.
- `localScale` phóng/co quanh **pivot** ⇒ phồng `Output_Bubble` (pivot góc dưới-phải) lên 1.18 làm bao **lao chéo lên trái 14px**, nhìn như bị kéo đi chứ không phải thở. Nhún thân máy cũng vậy.
- `ScreenPointToLocalPointInRectangle` trả toạ độ tính từ **pivot của cha**, còn `anchoredPosition` tính từ **điểm neo** ⇒ neo hạt ở (0.5, 0.5) trong `AnimationBox` (pivot góc trên-trái) làm mọi hạt lệch **nửa khung** (314, −125).

Cả ba đi qua `MillRectUtil`: `TamWorld()`, `DoiPivotVeGiua()`, `DatNeoTheoPivotCha()`. Đọc doc đầu file đó trước khi viết hiệu ứng UI mới trong dự án này.

### 6.2 Không dùng lại `HarvestFeedbackSpawner`

`HarvestFeedbackSpawner.SpawnHarvestFly` bay trong **không gian world** (prefab `PF_HarvestFlyItem_World_Clean`, đích là `FX_Target_Warehouse` — cái nhà kho trên đồng). Popup máy xay là UI phủ kín màn hình kèm lớp `Dim` đen 55% ⇒ icon world bay **sau** lớp dim, người chơi không thấy gì. Vì vậy `MillCollectFlyFX` là UI thuần.

Thứ tự canvas trong `SCN_Farm`: `Canvas_HUD` 100 · `Canvas_Popup` 150 · `MillPopup_Root` 400. `CoinFlyFX` gắn xu vào `Canvas_HUD` — bắt chước y hệt thì icon bay **dưới** popup. Icon được gắn vào canvas của popup (400) và toạ độ nút KHO được quy đổi bằng cặp `WorldToScreenPoint` → `ScreenPointToLocalPointInRectangle`; cả hai canvas đều ScreenSpaceOverlay nên camera = `null` và phép quy đổi chính xác bất kể CanvasScaler hai bên khác nhau.

### 6.3 `MillCollectFlyFX` không thể trông vào `OnDisable`

Component nằm trên node **gốc** của popup (node mang `Canvas`), còn `Close()` chỉ tắt node con `PopupRoot`. Node gốc vẫn active ⇒ `OnDisable` **không chạy** ⇒ icon tiếp tục bay lơ lửng trên mặt đồng sau khi popup đã đóng (icon lại gắn thẳng vào canvas nên cũng không bị tắt theo). Vì vậy `MillPopupUI.Close()` gọi tường minh `fxBayVeKho.DonSach()`.

---

## 7. BUG PHÁT HIỆN THÊM (đã sửa) — card khoá làm chết cuộn danh sách

`MillRecipeCardUI.Bind` trước đây đặt `_canvasGroup.blocksRaycasts = unlocked`, với ý định *"cho click xuyên xuống ScrollRect để vẫn kéo cuộn được"*.

Nhưng **không có gì đỡ ở dưới**: `Viewport` chỉ có `RectMask2D` (không `Image`), `RecipeList` và `InnerPanel` đều `raycastTarget = false` ⇒ raycast xuyên thẳng tới `Window`, và `Window` không phải `ScrollRect`. Kết quả thật: đặt ngón tay lên card khoá thì **danh sách không cuộn được chút nào**.

Nay luôn để `true` và để `MillRecipeDragSource` lo: card khoá không nhấc được bao (tự kiểm `IsUnlocked`) nhưng vẫn **forward cú kéo cho ScrollRect** ⇒ cuộn được. Click vào card khoá vẫn vô hại (`btnSelect.interactable = false` + hàng rào `!_unlocked` trong `BamChon`).

---

## 8. VIỆC CÒN NỢ (không chặn nghiệm thu)

| # | Việc | Vì sao chưa làm |
|---|---|---|
| 1 | 4 `MillRecipeData` vẫn dùng **chung một sprite placeholder** (`icon` guid `cb6c7b1a…`); `CoTronBo` / `CamBoSua` chưa có `animalBadgeIcon` | Cần Sếp kéo art thật vào Inspector — agent không gán được asset nhị phân (AUTONOMY §4). Bóng kéo, hạt bay, icon bay đều **lấy icon từ đây** nên gán art thật sẽ nâng chất lượng cả 3 hiệu ứng cùng lúc. |
| 2 | `CoTronBo` có 1 entry `ingredients` **rỗng itemId** (amount 4); `CamBoSua` có 2 entry rỗng (6, 4) | Là lỗi data từ trước. `DuNguyenLieu` bỏ qua entry rỗng nên vô hại, nhưng chip nguyên liệu trên card thiếu 1–2 ô so với thiết kế. Cần Sếp chốt nguyên liệu thật rồi đội đổ bằng tool. |
| 3 | `nguongTruc` còn hardcode trong prefab card | Chờ Sếp chốt con số ở mục 5. |
| 4 | `MillBuildingClick` chỉ tồn tại như **added component trên scene instance**, không có trong prefab `MayThucAn_Anim` | Instance khác của prefab (hoặc instantiate lại) sẽ **không bấm được**. `MayXayMia_Anim` hiện **không có** handler nào. Sửa = thêm vào prefab, tức là sửa `.prefab` ⇒ nằm trong DANH SÁCH DỪNG, cần Sếp duyệt riêng. |
| 5 | `PopupManager.IsAnyPopupOpen()` **chưa** biết tới `MillPopupUI.AnyOpen` | `MillPopupUI.cs` dòng 58–66 đã nói cần thêm một dòng `\|\| MillPopupUI.AnyOpen`. Là sửa file có sẵn ngoài phạm vi task này. Hệ quả hiện tại: mở popup máy xay xong click ra ngoài vẫn có thể chạm xuống world. |
| 6 | 2 `HarvestFeedbackSpawner` trùng trong `SCN_Farm` (dòng 93522 và 391163); `Awake` `Destroy(gameObject)` cái thua ⇒ phụ thuộc thứ tự load | Không liên quan máy xay nhưng là bom hẹn giờ. Sửa scene ⇒ DANH SÁCH DỪNG. |
| 7 | `HarvestFeedbackSpawner.cs:56–65` có `if` treo (di sản `remove_debug_logs.ps1`) làm guard `harvestFlyPrefab == null` bị bỏ qua | Cùng lý do trên — sửa là additive, chờ Sếp cho phép. |

---

## 9. THAM SỐ TINH CHỈNH NHANH

| Muốn gì | Sửa ở đâu |
|---|---|
| Bao sáng mạnh/yếu hơn | `MillOutputBagFX.alphaGlowMax` (0.8) · `scaleGlowMax` (1.18, **giữ ≤ 1.2**) |
| Bao nảy cao/thấp hơn | `MillOutputBagFX.caoNhay` (26) · `scaleDinh` (1.18) |
| Nhiều/ít hạt nguyên liệu | `MillIntakeFX.soHat` (6) · `doVong` (30, **giữ ≤ 32**) |
| Máy nhún mạnh/nhẹ | `MillIntakeFX.bienNhun` (0.07) |
| Icon bay nhanh/chậm | `MillCollectFlyFX.thoiGianBay` (0.62) · `soIcon` (3) |
| Bóng kéo to/nhỏ | `MillRecipeDragSource.kichCoBong` (64) |
| Viền slot đậm/nhạt | `MillSlotUI.alphaVienSanSang` (0.45) · `alphaVienHover` (1.0) |
| Kích cỡ vệt sáng | `MillDesign.BagGlowSize` (128) — **đừng nâng**, đọc comment tại node `Bag_Glow` |
