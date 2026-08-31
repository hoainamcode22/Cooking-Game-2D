# RÀ SOÁT INPUT CHO MOBILE — Cooking Game 2D

> Người rà: Dev C (ui-programmer) · Ngày: 2026-08-31
> Phát hành: **Android / iOS — người chơi dùng NGÓN TAY**. Chuột + bàn phím chỉ giữ để dev test trong Editor.
> Nguồn đối chiếu: project THẬT tại `E:\Game2\Cooking-Game-2D` (đọc trực tiếp, không phải bản snapshot).
> Cấu hình liên quan: `activeInputHandler: 2` (Both) ⇒ cả `Input` legacy lẫn Input System đều hoạt động.

---

## 0. KẾT LUẬN NGẮN

| Mức | Chỗ | Trạng thái |
|---|---|---|
| 🔴 P1 | Minigame canh thời gian — không dừng được thanh | **ĐÃ SỬA** |
| 🔴 P1 | Minigame bấm chữ — không chơi được | **ĐÃ SỬA** |
| 🔴 P1 | **Không vào được Edit Mode** (lỗi THẬT, khác với mô tả ban đầu) | **ĐÃ SỬA** (thêm nút) |
| ⚪ — | Xoay / xoá công trình khi đang đặt | **KHÔNG PHẢI LỖI** — nút ↻ ✕ ✓ 🗑 đã có và đã chạy bằng ngón tay |
| 🟠 P2 | Safe area (tai thỏ che HUD) | **ĐÃ SỬA** (component, Sếp gắn vào HUD) |
| 🟡 P3 | Lớp input chung + failsafe kéo hạt | **ĐÃ SỬA** |

---

## 1. BẢNG MỌI CHỖ ĐỌC INPUT (runtime, đã bỏ thư mục Editor)

### 1.1 Bàn phím — `Input.GetKey*` / `Keyboard.current`

| File : dòng | Phím | Chạy trên mobile? | Vì sao / xử lý |
|---|---|---|---|
| `minigameCooking/CookingTimingMiniGameUI.cs:241,248` | Space | ❌ **CHẶN GAMEPLAY** | Không có Button nào trong file ⇒ không dừng được thanh ⇒ **không nấu được món**. → **ĐÃ SỬA**: chạm bất kỳ đâu = Space |
| `minigameCooking/LetterMiniGame.cs:206-224` | A–Z | ❌ **CHẶN GAMEPLAY** | Không có Button nào ⇒ **không chơi được**. → **ĐÃ SỬA**: sinh hàng nút chữ |
| `Managers/EditModeManager.cs:56` | E | ❌ **CHẶN GAMEPLAY** | `ToggleEditMode()` là public "gắn vào Btn_EditMode" nhưng trong `SCN_Farm.unity`: `grep Btn_EditMode` = **0**, `grep ToggleEditMode` = **0** ⇒ **không có nút nào gọi** ⇒ trên điện thoại KHÔNG BAO GIỜ vào được Edit Mode. → **ĐÃ SỬA**: `MobileEditModeButton.cs` |
| `Managers/PlacementManager.cs:702` | Delete / Backspace | ⚪ không cần | Đã bọc `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`, và nút 🗑 là đường chính. Phím chỉ là tiện lợi cho dev |
| `Managers/PlacementManager.cs:710` | R | ⚪ không cần | Nút ↻ (`Btn_Rotate`) là đường chính, đã bind (xem §1.4) |
| `Kho/WarehousePopupUI.cs:174` · `Stall/StallPopupUI.cs:205` | Escape | ⚪ không cần | Chỉ là phím tắt đóng popup; nút X vẫn có. Android nút Back là chuyện khác — xem §4 mục 3 |
| `Camera/CameraController.cs:654` · `Camera/CameraDevPanel.cs:80` | phím dev | ⚪ không cần | Panel/dev shortcut |
| `Debug/PopupCaptureReporter.cs:60` (F10) · `Debug/PopupGateDebugF9.cs:34-35` (F9/F10) | debug | ⚪ không cần | Công cụ debug |

### 1.2 `Mouse.current` — **NULL trên điện thoại**

Tổng 59 lượt dùng. Đã soi từng file xem có đường Touchscreen song song hay không:

| File | Touchscreen | Mouse | Phán |
|---|:--:|:--:|---|
| `Camera/CameraController.cs` | ✔ (pinch 2 ngón + EnhancedTouch) | ✔ | ✅ OK — có nhánh touch riêng |
| `Managers/InputBridge.cs` | ✔ | ✔ | ✅ OK — chính là lớp bọc Touchscreen→Mouse |
| `Managers/FarmPlotInput.cs` | ✔ (5) | ✔ (3) | ✅ OK |
| `Cooking/KitchenClickOpen.cs` | ✔ | ✔ | ✅ OK |
| `Kho/WarehouseClickOpen.cs` | ✔ | ✔ | ✅ OK |
| `Market/MarketClickOpen.cs` | ✔ | ✔ | ✅ OK |
| `MillPopup/MillBuildingClick.cs` | ✔ (4) | ✔ (4) | ✅ OK |
| `OrderBoard/OrderBoardWorldObject.cs` | ✔ | ✔ | ✅ OK |
| `Animal/MiniPanel/PenClickDetector.cs` | ✔ | ✔ | ✅ OK |
| `Animal/MiniPanel/PenMiniPanelUI.cs` | ✔ (3) | ✔ (3) | ✅ OK |
| `Gameplay/PlantDragController.cs:117` | ✘ | ✔ | ❌ **failsafe chết trên mobile** → **ĐÃ SỬA** (dùng `TouchInput`) |
| `Debug/PopupGateDebugF9.cs:180` | — | ✔ | ⚪ debug |

### 1.3 `Input.mousePosition` — 29 lượt, **chạy được** nhờ `activeInputHandler = Both`

Unity mô phỏng chuột từ ngón tay khi bật Both, nên các chỗ này KHÔNG gãy. Không refactor rộng lúc này (rủi ro không cần thiết); danh sách để làm dần:

| File | Số lượt | Ghi chú |
|---|:--:|---|
| `Camera/CameraController.cs` | 8 | có nhánh touch riêng, mousePosition chỉ dùng ở nhánh chuột |
| `data/SickleController.cs` | 2 | kéo liềm — **ưu tiên chuyển sớm** (thao tác kéo, dễ lệch khi multi-touch) |
| `MillPopup/MillBuildingClick.cs` · `Managers/PlacementManager.cs` · `Gameplay/HouseGrowthController.cs` · `Gameplay/EditableBuilding.cs` | 2 mỗi file | `PlacementManager` dùng trong `IsMouseOverRect` (nút Ghost) — chạy đúng, xem §1.4 |
| `UI/CropProcessPopupUI.cs` · `UI/BuildingProcessPopupUI.cs` · `Train/TrainProcessPopupUI.cs` · `TouristBoat/BoatDockSlot.cs` · `Managers/WelfareEventManager.cs` · `Managers/AttendanceManager.cs` · `Animal/MiniPanel/*` (3 file) · `Audio/AudioManager.cs` | 1 mỗi file | đều là "vị trí để đặt popup/FX", không phải điều kiện chặn |
| `Gameplay/PlantDragController.cs` | 1 | **ĐÃ CHUYỂN** sang `TouchInput.PointerWorld` |

### 1.4 `OnMouseDown` / `OnMouseUpAsButton` — 33 lượt, **Unity tự map touch** ✅

Không phải sửa. Riêng `TouristBoat/BoatDockSlot.cs` đã được nâng lên chuẩn "chạm thật" trong lượt trước (bắt ở nhả tay + ngưỡng kéo 24px + bỏ qua khi đang kéo bản đồ/kéo hạt/kéo liềm).

### 1.5 EventSystem drag (kéo hạt giống)

`OnBeginDrag` / `OnEndDrag` của `SeedDragItem` → touch chạy tốt ✅. Lưới an toàn ở `PlantDragController` thì trước đây chết trên mobile — đã sửa (§3.3).

---

## 2. NÚT XOAY / XOÁ CÔNG TRÌNH — **KHÔNG PHẢI LỖI, ĐÃ CÓ SẴN**

Yêu cầu ban đầu là "thêm 2 nút UI nổi (Quay ↻ / Xoá ✕) vì mobile không quay/xoá được". Sau khi đọc `PlacementManager.cs` **thì thấy nút đã có đủ** — nên tôi **KHÔNG thêm nút mới**:

- `BindGhostButtons()` (dòng 2152) bind sẵn 4 nút trên Ghost theo tên: `Btn_Confirm` · `Btn_Cancel` · **`Btn_Rotate` → `RotateGhost()`** · `Btn_Delete`.
- `Update()` (dòng 686-693) còn có đường thứ hai: `Input.GetMouseButtonDown(0)` + `IsMouseOverRect(...)` cho từng nút — trên mobile `GetMouseButtonDown`/`mousePosition` được mô phỏng từ ngón tay (Both) nên **bấm bằng ngón tay là chạy**.
- Phím `R` / `Delete` chỉ là phím tắt cho dev (`Delete` còn bị bọc `#if UNITY_EDITOR || DEVELOPMENT_BUILD`).

**Và quan trọng hơn — thêm nút mới ở đây là NGUY HIỂM.** Chính file đó cảnh báo:

> "⚠ BẮT BUỘC phải liệt kê MỌI nút ở đây. Nút nào thiếu thì click sẽ rơi xuống nhánh 'ghost đi theo chuột' bên dưới → ghost NHẢY tới con trỏ, mang luôn cái nút chạy khỏi ngón tay → click không bao giờ hoàn tất, người dùng tưởng nút chết."

Thêm 2 nút nổi mà không khai báo rect của chúng vào danh sách trong `Update()` là **tái tạo đúng cái bug đó**. Vì vậy tôi giữ nguyên `PlacementManager.cs` (0 dòng thay đổi) và chỉ vá đúng chỗ THẬT SỰ thiếu: **cửa vào Edit Mode**.

---

## 3. VIỆC ĐÃ SỬA

### 3.1 `Assets/_Game/Scripts/minigameCooking/CookingTimingMiniGameUI.cs` (P1)
- **Chạm bất kỳ đâu = nhấn Space.** Vùng tap là Image trong suốt (alpha 0.004) + Button, **tự dựng lúc chạy** trên đúng Canvas của minigame, đặt lên trên cùng khi minigame bật, tắt khi kết thúc.
- Đường dự phòng: nếu không tìm được Canvas → đọc chạm toàn cục qua `TouchInput`, có **nhường lại nếu ngón đang ở trên một Button thật** (`EventSystem.RaycastAll`) để không cướp nút Close/Pause nếu sau này panel có thêm nút.
- **Arming 0.15s**: cú chạm vừa mở minigame (bấm "Nấu") không bị tính là cú chạm dừng thanh.
- Thêm chữ gợi ý **"Chạm để dừng!"** (TMP, tiếng Việt có dấu) — tự dựng nếu chưa gán ô chữ.
- Bàn phím Space **giữ nguyên** song song. Logic tính thành/bại + callback: **0 dòng thay đổi**.
- Cờ tắt: `choPhepChamDeDung`. API mới: `public void OnTapStop()` (wire tay vào nút nào cũng được).

### 3.2 `Assets/_Game/Scripts/minigameCooking/LetterMiniGame.cs` (P1)
- **Sinh hàng nút chữ** mỗi lượt: các chữ CÓ TRONG chuỗi (bỏ trùng, xáo trộn Fisher-Yates). Bấm nút → gọi đúng `CheckInput(char)` cũ ⇒ logic đúng/sai/kết thúc **0 dòng thay đổi**.
  - *Vì sao không hiện 26 chữ:* 26 nút trên màn hình điện thoại thì mỗi nút bé hơn đầu ngón tay. Muốn khó hơn thì tăng `soChuGiaThem` (mặc định **0** = giữ đúng độ khó bản cũ).
- **Cỡ nút theo pixel THẬT**: `coNutPixel = 90` (mức tối thiểu Apple/Google), quy đổi qua `Canvas.scaleFactor` nên đúng trên mọi mật độ điểm ảnh.
- **Tự co khi nhiều chữ**: `HorizontalLayoutGroup` + `ContentSizeFitter` + kẹp theo 84% bề rộng canvas ⇒ không tràn màn hình.
- Bàn phím A–Z **giữ nguyên**. Cờ tắt: `hienHangNutChu`.

### 3.3 `Assets/_Game/Farm/Scripts/Gameplay/PlantDragController.cs` (P3) — **đúng 2 chỗ**
- Dòng 117: `Mouse.current != null && ...wasReleasedThisFrame` → `TouchInput.TapUpThisFrame()`. Trước đây trên mobile `Mouse.current == null` nên **lưới an toàn chưa bao giờ chạy**; nay có cả `TouchPhase.Canceled` (hệ điều hành huỷ touch khi có cuộc gọi đến) — thiếu nhánh này là kéo hạt kẹt vĩnh viễn.
- `GetMouseWorld()`: `Input.mousePosition` → `TouchInput.PointerWorld(mainCam)`. Giữ nguyên tên hàm.
- (Diff đã kiểm: **chỉ 2 chỗ này** thay đổi, không có gì khác.)

### 3.4 `Assets/_Game/Farm/Scripts/Core/TouchInput.cs` (P3, MỚI)
API đúng như chốt: `TapDownThisFrame` · `TapUpThisFrame` · `IsHolding` · `PointerScreen` · `PointerWorld(Camera)` · `HasTouchscreen`.

> **⚠ Phát hiện khi làm:** dự án **ĐÃ CÓ** `Managers/InputBridge.cs` làm gần đúng việc này (Touchscreen → Mouse, kèm `IsPointerOverUI()` xử lý pointerId của touch rất đúng). Để **không có 2 nguồn sự thật**, `TouchInput` **gọi thẳng InputBridge** cho mọi đường Input System và chỉ thêm 3 thứ InputBridge thiếu:
> 1. tầng dự phòng `Input` legacy (InputBridge trả false/zero khi cả Touchscreen và Mouse đều null — hay gặp trong Device Simulator);
> 2. `TouchPhase.Canceled`;
> 3. `HasTouchscreen`.
>
> **Khuyến nghị lead:** code MỚI dùng `TouchInput`; code cũ đang gọi `InputBridge` KHÔNG cần sửa. Nếu sau này muốn gộp hẳn thì gộp vào 1 file — nhưng đừng làm cùng lúc với đợt phát hành này.

### 3.5 `Assets/_Game/Farm/Scripts/UI/SafeAreaFitter.cs` (P2, MỚI)
- Quy `Screen.safeArea` → `anchorMin/anchorMax` + zero offset trên chính RectTransform được gắn.
- **Có cờ từng cạnh**: `apCanhTren` (mặc định BẬT — HUD trên), `apCanhDuoi` (mặc định **TẮT** vì HUD dưới của dự án đã cách đáy), `apCanhTrai/Phai` (BẬT — máy nằm ngang).
- **KHÔNG PHÁ LAYOUT HIỆN TẠI:** khi `safeArea` trùng đúng toàn màn hình (Editor, PC, đa số Android) thì component **trả anchor về đúng giá trị GỐC** và không sửa gì. Anchor gốc được lưu 1 lần trong `Awake` nên chạy nhiều lần **không co dồn**.
- Chỉ tính lại khi safeArea / kích thước màn hình / orientation đổi (poll so 3 mốc, không tính mỗi frame).

### 3.6 `Assets/_Game/Farm/Scripts/UI/MobileEditModeButton.cs` (P1 thật, MỚI)
- Nút HUD bật/tắt Edit Mode, nhãn tự đổi **"Sửa" ⇄ "Xong"**, chỉ gọi API public đang có `EditModeManager.ToggleEditMode()` — **không đụng logic Edit Mode / placement**.
- Có `nutCoSan` để dùng nút thật trong scene; để trống thì tự dựng ở góc dưới-phải (cỡ theo pixel thật 110px).
- Cờ `chiHienTrenMobile` (mặc định TẮT = luôn hiện, để Sếp test bằng chuột).

---

## 4. CÒN TỒN (chưa sửa, cần Sếp/lead quyết)

1. **Nút Back của Android** — chưa có xử lý ở đâu (`Input.GetKeyDown(KeyCode.Escape)` chỉ có 2 chỗ, dùng cho phím Esc trên PC). Trên Android, bấm Back giữa lúc chơi sẽ **thoát app** thay vì đóng popup. Nên có 1 `AndroidBackHandler` gom: có popup → đóng popup; không có → hỏi "Thoát game?".
2. **29 lượt `Input.mousePosition`** còn lại — chạy được nhờ Both, chưa refactor. Ưu tiên chuyển sớm: `SickleController` (kéo liềm), `EditableBuilding`, `HouseGrowthController`.
3. **`activeInputHandler = Both` là điều kiện SỐNG CÒN** của bản build hiện tại. Nếu ai đổi Player Settings sang "Input System Package (New)" thì **29 chỗ `Input.mousePosition` + `GetMouseButton*` chết ngay**. Đề nghị ghi vào playbook: KHÔNG đổi mục này.
4. **Multi-touch**: `InputBridge`/`TouchInput` chỉ đọc `primaryTouch`. Đang ổn vì pinch-zoom do `CameraController` xử lý riêng bằng EnhancedTouch. Nhưng nếu sau này có thao tác 2 ngón khác thì cần mở rộng.
5. **Cỡ vùng chạm của các nút HUD hiện có** — chưa đo. Khuyến nghị: mọi nút ≥ 90×90 px thật; nút nào nhỏ hơn thì phóng `sizeDelta` (không cần sửa code).
6. **Bàn phím ảo** khi có ô nhập chữ (nếu sau này thêm) sẽ đẩy layout — chưa có xử lý.

---

## 5. CHECKLIST TEST TRÊN MÁY THẬT

### 5.1 Trước khi build
- [ ] Gắn `SafeAreaFitter` vào **gốc HUD trên** (avatar + thanh EXP + vàng + gem + nút cài đặt). Để `apCanhTren = true`, `apCanhDuoi = false`.
- [ ] Gắn `MobileEditModeButton` vào một object trong scene farm (hoặc kéo nút HUD có sẵn vào `nutCoSan`).
- [ ] Kiểm `ProjectSettings > Player > Active Input Handling = Both` (KHÔNG đổi).
- [ ] Android: `Minimum API Level` ≥ 23 · iOS: `Target minimum iOS Version` ≥ 12.
- [ ] Build **Development Build** cho lượt test đầu (để thấy log + nút 🗑 của Ghost).

### 5.2 Android (máy thật, ưu tiên 1 máy tai thỏ + 1 máy không tai thỏ)
- [ ] Nấu ăn: mở minigame canh thời gian → **chạm bất kỳ đâu** → thanh dừng, có chữ "Chạm để dừng!" hiện dưới panel.
- [ ] Chạm liên tiếp 2 lần rất nhanh → chỉ tính 1 lần dừng (không double-finish).
- [ ] Minigame bấm chữ: **hàng nút chữ hiện đủ**, không tràn màn hình, bấm đúng thứ tự → xanh; bấm sai → đỏ; hết giờ → kết thúc.
- [ ] Nút chữ đủ to để bấm bằng ngón cái (≥ 90px thật).
- [ ] Kéo hạt giống: kéo qua nhiều ô → trồng đủ; **nhấc tay giữa lúc kéo → thoát chế độ kéo ngay** (không kẹt icon hạt bám tay).
- [ ] Trong lúc kéo hạt, **kéo notification bar xuống rồi quay lại** → không kẹt kéo (test nhánh `TouchPhase.Canceled`).
- [ ] Nút **"Sửa"** hiện ở góc dưới-phải → bấm → vào Edit Mode (lưới hiện) → chạm công trình → hàng nút **✓ ✕ ↻** hiện → bấm ↻ **xoay được**, bấm ✓ đặt xong.
- [ ] Mua công trình mới từ shop → ghost hiện → ngón kéo ghost đi, thả tay, bấm ↻ / ✓ — **nút không chạy khỏi ngón tay**.
- [ ] Pinch 2 ngón zoom + 1 ngón pan bản đồ → mượt, không xung đột với tap mở nhà/kho/chợ.
- [ ] Tap chuồng / bếp / kho / chợ / cối / bảng đơn → popup mở đúng.
- [ ] HUD trên **không bị lỗ camera che** (máy có khuyết).
- [ ] Bấm nút **Back** → ghi nhận hành vi hiện tại (dự kiến: thoát app — xem §4 mục 1).

### 5.3 iOS (ưu tiên iPhone có tai thỏ / Dynamic Island + 1 máy có nút Home)
- [ ] Lặp lại toàn bộ mục 5.2.
- [ ] **Tai thỏ / Dynamic Island không che** avatar, vàng, gem, nút cài đặt (đây là mục chính của `SafeAreaFitter`).
- [ ] **Thanh gesture dưới** không đè nút HUD dưới; nếu đè → bật `apCanhDuoi = true` trên HUD dưới.
- [ ] Xoay máy (nếu game cho xoay) → HUD tính lại vùng an toàn, không lệch.
- [ ] Máy có nút Home (không tai thỏ) → layout **giữ nguyên như bản cũ** (safeArea = toàn màn hình → component không sửa gì).

### 5.4 Hồi quy trên Editor (Sếp test bằng chuột/bàn phím)
- [ ] Space vẫn dừng được minigame canh thời gian.
- [ ] Phím A–Z vẫn chơi được minigame chữ.
- [ ] Phím E vẫn toggle Edit Mode; phím R vẫn xoay ghost.
- [ ] Kéo hạt bằng chuột: nhả chuột → thoát chế độ kéo.
- [ ] Console **0 lỗi đỏ**.

---

## 6. COMPILE-CHECK

3 pass bằng `mcs` + stub Unity (mở rộng thêm `Screen.safeArea`, `ScreenOrientation`, `Input.GetTouch/TouchPhase`, `Canvas.scaleFactor`, `KeyCode`, `LayoutElement`, `ContentSizeFitter.FitMode`, `RaycastResult`, `PointerEventData(EventSystem)`):

| Pass | Nội dung | Kết quả |
|---|---|---|
| 1 | Editor: toàn bộ file giao + `InputBridge` thật + deps thật, `-define:UNITY_EDITOR` | **0 error / 0 warning** |
| 2 | Giả lập player build: bỏ `UNITY_EDITOR`, bỏ mọi file `Editor/` | **0 error / 0 warning** |
| 3 | Merge: pass 1 + tool `TouristBoatSetupTool` V1 của project | **0 error / 0 warning** |

`PlantDragController.cs` không stub nổi hết phụ thuộc (`Physics2D`, `FarmManager`, `WarehouseManager`, `CropData` đầy đủ) nên được kiểm bằng **diff so với bản gốc**: đúng 2 chỗ thay đổi như mô tả ở §3.3, không có thay đổi nào khác.
