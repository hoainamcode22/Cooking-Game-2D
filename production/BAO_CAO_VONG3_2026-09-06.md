# BÁO CÁO VÒNG 3 - 06/09/2026

> Đội: 4 Dev chạy SONG SONG, mỗi Dev một nhóm file riêng (luật "mỗi file một chủ").
> Backup: `production/backup_vong3_2026-09-06/` (8 file .bak).
> **Chưa commit. Chưa compile trong Unity.**
> Lead đã CHECK chéo: 7/7 file cân bằng `{}` `()`, md5 khớp báo cáo Dev, line-ending giữ nguyên, không còn `if` rỗng.

---

## TÓM TẮT 1 DÒNG MỖI TASK

| Task | Trạng thái | Ai làm |
|---|---|---|
| 1. Tàu hoả không mở popup | Tìm ra nguyên nhân THẬT + đã vá code. Còn 1 bước Sếp phải sửa scene | Dev A |
| 2a. Process xây dựng | ✅ Sếp đã xác nhận OK, đóng task | vòng 2 |
| 2b. Click chuồng không mở panel thóc (hồi quy mới) | ✅ Tìm ra nguyên nhân THẬT + đã vá, không cần đụng scene | Dev B |
| 3. Decor thiếu stage | ❌ Art THỰC SỰ chưa có. Đã dò 2.852 file ảnh + toàn bộ lịch sử git | Dev C |
| 4. Gia súc bị rào đè | Bản vá cũ đúng về số học. Vá thêm 1 lỗ. Phần "đi xuyên" bắt buộc chờ art 2 lớp | Dev D |

---

## 1. TASK 1 - TÀU HOẢ

**Trả lời câu hỏi của Sếp: KHÔNG CÓ GATE THEO CẤP.** Tàu mở từ cấp 1, mọi lúc.
Grep sạch `requiredLevel` / `unlockLevel` / `IsUnlocked` / `MinLevel` trên cả 3 thư mục tàu: 0 kết quả.
`TrainManager.cs` không chứa một chữ `Level` nào. Object `gataulua` còn mang `PermanentBuilding` (công trình cố định, không bao giờ ẩn).

**Nguyên nhân THẬT (bằng chứng cứng trong scene):**
`Popup_Train_MasterStation` KHÔNG nằm dưới `Canvas_Popup`. Nó là **con của `Popup_LevelUp_Township`** (popup lên cấp).
Trích `SCN_Farm.unity`, khối `--- !u!1001 &4105157295546141520`: `m_TransformParent: {fileID: 1561892010}` = RectTransform của `Popup_LevelUp_Township` (id 1561892009), kèm `m_IsActive: 0`.

Vì sao chết: `OpenPopup()` chỉ `SetActive(true)` cho CHÍNH NÓ. Cha đang tắt ⇒ `activeInHierarchy` vẫn false ⇒ không vẽ gì, `Awake()`/`OnEnable()`/`RefreshUI()` đều không chạy. Nhưng `activeSelf` đã thành true ⇒ **click lần sau rơi vào nhánh toggle `ClosePopup()`** ⇒ popup vĩnh viễn không hiện. Khớp 100% triệu chứng.

Bản vá `_openRequested` của vòng 1 CÒN NGUYÊN (dòng 51-64, 66, 116-119) và đã biên dịch (DLL 09:52 mới hơn source 08:34, log không có `error CS`). Nó đúng nhưng không đủ, vì bug thật nằm ở cấu trúc scene.

**Đã sửa (2 file):**
- `TrainStationMasterPopupUI.cs` - `OpenPopup()` thêm vòng lặp bật lại tổ tiên đang tắt, dừng ở Canvas gần nhất. Dùng lại đúng cơ chế đã có sẵn ở `TrainLoadPopupUI.OpenForWagon()` (dòng 119-127) mà file này thiếu.
- `TrainStationBuilding.cs` - chỉ thêm log `[Train]`, không đụng logic.

**Lead đã kiểm rủi ro của bản vá này:** bật `Popup_LevelUp_Township` lên có làm popup lên cấp bung ra không? KHÔNG.
`LevelUpPopupUI.IsActive` chỉ được gán `true` trong `ShowNextPopup()` (dòng 336), không gán ở `OnEnable`. `Start()` dòng 210 tắt con `Root_HienThi` ngay khi object được bật lần đầu. Nên **không lặp lại thảm hoạ khoá map của vòng 1**.

**Phát hiện phụ nghiêm trọng:** 2 prefab `Popup_train` và `Popup_item_Train` KHÔNG có trong `SCN_Farm`, chỉ được tạo runtime bởi `EnsurePopupsExist()` - mà toàn bộ khối đó nằm trong `#if UNITY_EDITOR` (`TrainStationBuilding.cs:126-167`). **Trong bản build thật 2 popup này không tồn tại.** Editor chạy được, Android sẽ câm.

---

## 2. TASK 2b - CLICK CHUỒNG KHÔNG MỞ PANEL THÓC (hồi quy vòng 2)

**Giả thuyết ban đầu của Lead SAI, Dev B đã bác bằng bằng chứng.**
`DecorGrowthController` vô can: `CanAcceptClick()` dòng 433 `if (_state != Building && _state != ReadyToReveal) return false;` ⇒ chuồng xây xong ở state `Completed` thì thoát ngay. Thêm nữa `RestoreFromSave()` dòng 307-314 tự `Destroy(this)` khi không có save key.

**Nguyên nhân THẬT:** vòng sửa 06/09 đã đổi `PenMiniPanelUI.IsPanelOpen()` thành cờ **TOÀN CỤC**:
```
CŨ:  IsPanelOpen() => panelRoot != null && panelRoot.activeSelf
MỚI: IsPanelOpen() => PenSupplyTrayV2.DangMoKhay || (panelRoot != null && panelRoot.activeSelf)
```
`PenSupplyTrayV2.DangMoKhay` (dòng 56) là **static singleton** = "có BẤT KỲ khay nào đang mở", không phải "khay của chuồng NÀY".

Chuỗi chết người, gọn trong 1 frame:
1. Bấm chuồng A → `OpenPanel()` → `PenSupplyTrayV2.Show()` → `_hienThi = true`
2. Cùng frame đó, `Update()` của chuồng B chạy (`PenMiniPanelUI.cs:147`): `IsPanelOpen()` nay trả `true`
3. Chốt giữ 1.5s ở dòng 153 vô dụng vì `_openedAtTime` khai báo `= -99f` (dòng 77) và **không bao giờ được gán ở đâu cả**
4. `IsPointerOverPanel()` đo vào RectTransform của chuồng B, con trỏ ở xa ⇒ false
5. → `ClosePanel()` → `PenSupplyTrayV2.HideIfShowing()` ⇒ **dập tắt khay vừa mở của chuồng A**

**Vì sao khớp chính xác lời Sếp:** `PenSupplyTrayV2.TryShow()` dòng 143 `if (pen.CurrentState == Processing) return false;` ⇒ nhánh Processing không đụng khay V2 ⇒ không bị phá ⇒ "process ok đã đúng như mong đợi". Còn hai nhánh `Idle` (panel thóc) và `Ready` (khay rổ) đều đi qua `TryShow` ⇒ **cả hai chết** ⇒ "chưa mở painel thóc này kia đâu hết".

**Đã sửa (3 file):**
- `PenSupplyTrayV2.cs` - THÊM `DangMoKhayCho(pen)` (dòng 65-68) trả theo từng chuồng. Giữ nguyên `DangMoKhay` cũ vì `TutorialManager.cs:3181` đang dùng đúng nghĩa toàn cục ở đó. Thuần cộng thêm.
- `PenMiniPanelUI.cs` - dòng 253 `IsPanelOpen()` dùng `DangMoKhayCho(this)`, **giữ nguyên chữ ký public** nên scene/prefab không cần đụng. Dòng 154 thêm chốt chặn `if (panelRoot == null || !panelRoot.activeSelf) return;` (toàn bộ khối dưới là logic của panel CŨ, khay V2 tự lo qua `OnDimPressed()`). Dòng 274 hồi sinh `_openedAtTime`.
- `PenClickDetector.cs` - gỡ `if` rỗng ở dòng 55-59 (do `remove_debug_logs.ps1` xoá Debug.Log để lại thân rỗng, nuốt luôn câu `if` bên dưới). Đảo thứ tự: kiểm trúng collider TRƯỚC rồi mới kiểm `FarmInputLock`. Thêm log `[PenClick]` in rõ **cờ nào đang chặn**.

**Lead đã verify** `PenMiniPanelUI.Update()` không làm gì khác ngoài "chạm ra ngoài thì đóng" (timer chạy bằng coroutine riêng), nên chốt chặn dòng 154 không cắt mất chức năng nào.

**Lợi ích kèm theo:** `TutorialRuntimeTargetResolver.cs:139` trước đây với cờ toàn cục luôn trả về chuồng ĐẦU TIÊN trong danh sách chứ không phải chuồng đang mở ⇒ tay chỉ tutorial trỏ nhầm chuồng. Nay đã đúng.

---

## 3. TASK 3 - DECOR THIẾU STAGE: ART THỰC SỰ CHƯA CÓ

Dev C quét **2.852 file ảnh** toàn dự án + toàn bộ lịch sử git + ổ ngoài `E:\agent-sprite-forge`. Kết luận từng slug:

| slug | itemID | Kết luận |
|---|---|---|
| `banghieu` | 3 | THỰC SỰ CHƯA CÓ (0/5) |
| `ghehoa` | 7 | THỰC SỰ CHƯA CÓ (0/5) |
| `heothantai` | 8 | THỰC SỰ CHƯA CÓ (0/5) |
| `vitvuive` | 12 | THỰC SỰ CHƯA CÓ (0/5) |

Không có sprite sheet chưa cắt. Không có file đặt sai tên. Không có file lạc thư mục.

**Cái Sếp nhớ "đã giao đủ assets" chính là 4 ảnh THÀNH PHẨM 1 stage này** - chúng CÓ THẬT và đang chạy trong game (đó là lý do 4 món vẫn mua và đặt ra world được, chỉ là hiện nguyên hình ngay, không có cảm giác xây):

| slug | File thật đang dùng | Kích thước |
|---|---|---|
| banghieu | `Assets/Assetsgame/bocaycoitrangtri/Assettrangtri/PuLbG-removebg-preview.png` | 409x610 |
| ghehoa | `.../dqOJj-removebg-preview.png` | 409x610 |
| heothantai | `.../wkPGx-removebg-preview.png` | 409x610 |
| vitvuive | `.../fM7aK-removebg-preview.png` | 409x610 |

**Thiếu là 16 file còn lại**: stage_1, stage_2, stage_4, stage_5 cho mỗi món.

**Bằng chứng phủ định mạnh nhất:** 2 file QC contact sheet do chính đội làm ngày 01/09 (`production/_to_delete_scan_tmp/qc_all_decor.jpg` và `decor_sheet.jpg`) chụp toàn bộ bộ decor 5 stage - **chỉ có đúng 15 slug**, không có id 3, 7, 8, 12. Và `git log --all --diff-filter=A` grep 4 slug = **0 kết quả**, chưa từng có file nào mang tên này được commit.

**Ba lần đặt đơn đều chưa có hàng về:**
- `PROMPT_SPRITE_FORGE_DECOR_FIX_2026-09-01.md` (thư mục giao `2026-09-02_Decor5Stage_Fix/` **không tồn tại**)
- `PROMPT_SPRITE_FORGE_2026-09-03.md` dòng 133-136
- `PROMPT_SPRITE_FORGE_2026-09-06.md` gói A - thư mục `production/art-handoff/2026-09-06_Decor4_Rao2Lop/` có đủ 5 thư mục con nhưng **RỖNG 0 byte**, chỉ có 1 file `_DAT_FILE_VAO_DAY.md`

### 3 phát hiện phụ Sếp cần biết

**a) Tool đang CỐ Ý loại 4 món này.** `DecorStageArtTool.cs` hàm `BangMap()` chỉ có **15 entry**, không có 4 slug trên. Dòng DRY-RUN ghi rõ "KHÔNG có art 5 stage (CỐ Ý bỏ)". ⇒ **Chỉ thả file vào thư mục là tool bỏ qua im lặng.** Phải thêm 4 entry vào `BangMap()` trước.

**b) Món id 3 tên "Bảng Hiệu" nhưng art hiện tại là KỆ CÂY.** `PuLbG-removebg-preview.png` là kệ gỗ 3 tầng đựng chậu cây và hoa. Trong khi đơn hàng 06/09 mô tả `banghieu` là "bảng gỗ nông trại cắm 2 cọc, bảng phải trống không chữ". Nếu đội vẽ làm theo đơn thì stage_3 sẽ khác hẳn hình người chơi đang thấy ⇒ **món đồ sẽ biến hình**. CẦN SẾP CHỐT.

**c) Đính chính báo cáo hôm qua:** hôm qua ghi "4 GUID đó có trong decorList rồi". SAI một phần. `decorList` trong scene có 18 entry, **thiếu đúng `Bảng hiệu` (GUID 78991ab7...)**. Ghế Hoa, Heo thần tài, Vịt vui vẻ thì có. Cả 4 đều KHÔNG có trong `DecorGrowthConfig` (15 stageSet).

---

## 4. TASK 4 - GIA SÚC BỊ RÀO ĐÈ

**Bản vá vòng 1 CÒN NGUYÊN và ĐÚNG:** `LivestockAI.cs` dòng 71 `FenceSortingOrderFloor = 512`, dòng 99-101 `ResolveOrOverride(..., Visitor)` + kẹp order. `UpdateDynamicSorting()` dòng 230-239 chạy **mỗi frame** trong `Update()`, nên bất cứ script nào ghi đè ở `Awake/Start/OnEnable` đều bị giành lại ngay frame sau.

**Phát hiện quyết định:** trong `SCN_Farm.unity`, cả 4 chuồng bị ghi đè ở CẤP SCENE:
`propertyPath: m_SortingLayerID → value: 0` (= layer `Default`) và `m_SortingOrder → value: 500`.
Nghĩa là rào KHÔNG còn nằm ở layer ma 1669604809 nữa, mà ở `Default`.
Thứ tự layer thật: `Bottom`(0) · `Default`(1) · `Objects`(2) · `ObjectsFront`(3) · `Foreground`(4).
Con vật giải ra `Objects`(2) > `Default`(1) ⇒ **con vật vẽ trên rào bất kể order**.
Số học order: cả 4 chuồng `sortingOrderOffset: 50` ⇒ base 650, `y ∈ [1.25 ; 2.5]` ⇒ order thực **525 đến 588**, luôn trên 512 và trên 500, và y-sorting giữa các con vẫn hoạt động (không bị bão hoà).

**⇒ Bản vá đúng và đủ để con vật NỔI TRÊN rào.**

**Đã sửa (2 file):**
- `LivestockAI.cs` - thêm log `[Livestock]` in ở 2 mốc (frame-1 và sau-1s, mốc 2 để bắt script ghi đè muộn). In layer name + id + value + order của **cả con vật lẫn BarnSprite**, rồi kết luận thẳng `CON VAT VE TREN RAO` hoặc `RAO DE LEN CON VAT (SAI)`. Có cờ `logSortingDiagnostics` trên Inspector để tắt.
- `HappyHarvestAnimalVisualSpawner.cs` dòng 80-86 - vá lỗ kẹp order duy nhất còn sót: `sg.sortingOrder = 600 + offset + index*5` không kẹp sàn, nếu `sortingOrderOffset` bị gõ âm thì con vật chìm dưới rào đúng 1 frame đầu. Nay dùng `Mathf.Max(..., FenceSortingOrderFloor)`.

### RANH GIỚI: code cứu được tới đâu
Ba triệu chứng Sếp báo **không thể cùng đúng với 1 lớp art**:
- "nằm dưới chuồng" + "rào đè lên người" = cùng một lỗi, **code sửa được, ĐÃ SỬA**.
- "đi xuyên" = **mặt trái bắt buộc**, không phải bug còn sót. `BarnSprite` là MỘT SpriteRenderer, một file `chuongmoigiasuc.png` phủ cả 4 cạnh, ở MỘT order 500. Kéo con vật lên trên để khỏi bị chôn thì nó đồng thời nổi trên cạnh rào TRƯỚC. Kéo xuống thì quay lại bị chôn. **Không có con số nào cứu được cả hai.** Bắt buộc tách art 2 lớp (rào-sau ~490 / rào-trước ~600), con vật nằm giữa.

---

## 5. 🧑 CẦN SẾP LÀM (đúng thứ tự)

**Bước 0 - Compile.** Mở Unity, chờ biên dịch. Lỗi đỏ thì chụp Console gửi Lead, ĐỪNG sửa tay.

**Bước 1 - Sửa cha của popup tàu (quan trọng nhất, Lead không được tự làm vì đụng scene):**
1. Mở `Assets/_Game/Scenes/SCN_Farm.unity`
2. Hierarchy: `Canvas_Popup` → `Popup_LevelUp_Township` → **`Popup_Train_MasterStation`**
3. **Kéo `Popup_Train_MasterStation` ra, thả thẳng vào `Canvas_Popup`** (thành anh em cùng cấp với `Popup_LevelUp_Township`)
4. Giữ nguyên trạng thái TẮT (ô tick cạnh tên để trống)
5. Ctrl+S lưu scene

**Bước 2 - Bổ sung 2 popup thiếu (để bản BUILD chạy được, hiện chỉ Editor có):**
Kéo vào làm con trực tiếp của `Canvas_Popup` rồi TẮT cả hai:
- `Assets/Export_Train_UI_Package/Prefabs/Popup_train.prefab` → đặt tên object `Popup_train`
- `Assets/Export_Train_UI_Package/Prefabs/Popup_item_Train.prefab` → đặt tên object `Popup_item_Train`

**Bước 3 - Test tàu.** Play, lọc Console chữ `[Train]`, click ga tàu. Bảng đọc log:

| Console in | Nghĩa |
|---|---|
| (không dòng nào) | Click không chạm collider, báo Lead |
| `bi chan tai cong ... Popup dang mo = 'X'` | Popup X đang kẹt mở, thủ phạm là X |
| `TrainState=Processing` | Tàu đang chạy nên mở popup đồng hồ, KHÔNG phải popup toa hàng |
| `masterPopup=NULL` | Popup không có trong scene, làm lại Bước 1 |
| `Popup master DANG MO san -> toggle` | Popup bị bật activeSelf từ trước mà không hiện, làm lại Bước 1 |
| `To tien '...' dang TAT - da bat lai` | Xác nhận đúng nguyên nhân, bản vá vừa cứu |
| `OpenPopup xong ... activeInHierarchy=True` | Đã mở đúng. Vẫn không thấy thì là sorting, báo Lead |

**Bước 4 - Test chuồng.** Lọc Console chữ `[PenClick]`, bật Clear on Play.
- Click chuồng **Idle (đang đói)** → phải hiện khay thóc VÀ **ở nguyên đó** (trước đây bị dập ngay).
- Đóng khay, click sang **chuồng khác** ngay → phải mở được chỉ với 1 cú click.
- Chuồng **Processing** → popup tiến độ + nút gem (không được hồi quy).
- Chuồng **Ready** → khay cái rổ.
- Nếu vẫn không mở: gửi Lead nguyên dòng `[PenClick] ... BI CHAN ...`, dòng đó in rõ cờ nào đang chặn.

**Bước 5 - Test gia súc.** Lọc Console `[Livestock]`. Mỗi con in 2 dòng.
- Cuối dòng ghi `CON VAT VE TREN RAO` ⇒ phần code xong.
- Ghi `RAO DE LEN CON VAT (SAI)` ⇒ copy nguyên dòng gửi Lead.
- Nhìn mắt: còn bị **chôn dưới rào** không? Nếu hết chôn mà chỉ còn **đè lên vạch rào trước** thì đúng như dự đoán, code hết phần việc, chuyển sang đội vẽ.
- **Báo Lead: "đi xuyên" là đè lên vạch rào trước (giới hạn art) hay đi lọt ra ngoài khuôn chuồng (chỉnh `walkBoundsMin/Max` được)?** Hai cái khác hẳn nhau về hướng xử lý.
- Test xong bỏ tick `Log Sorting Diagnostics` cho Console đỡ ồn.

**Bước 6 - Gửi đội vẽ.** `production/PROMPT_SPRITE_FORGE_2026-09-06.md` (gói A 20 file decor + gói B 2 file rào 2 lớp). Về hàng thả vào `production/art-handoff/2026-09-06_Decor4_Rao2Lop/<slug>/`.
Khi art về, Lead sẽ thêm 4 entry vào `BangMap()` của `DecorStageArtTool.cs` rồi Sếp chạy menu `Tools/Farm Game/Decor 5 Stage/★ Nạp art 5 stage`.

**Bước 7 - Kéo tay `Bảng hiệu.asset` vào `ShopManager.decorList`** trong `SCN_Farm` (GUID `78991ab7a7541d54a9dd699fefc8e29b`). Đây là món duy nhất còn thiếu trong shop.

---

## 6. ⚠️ CẦN SẾP QUYẾT (Lead không tự quyết được)

**Món id 3 "Bảng Hiệu" nên vẽ thành cái gì?**
Art đang chạy trong game là **kệ gỗ 3 tầng đựng chậu cây**. Tên hiển thị là "Bảng Hiệu". Đơn hàng đang mô tả là "bảng gỗ cắm 2 cọc".
- Chọn A: sửa đơn hàng thành **kệ cây** ⇒ giữ nguyên hình người chơi đang thấy, an toàn nhất.
- Chọn B: giữ đơn là **bảng hiệu** ⇒ món đồ sẽ đổi hình, ai đã mua sẽ thấy nó biến thành thứ khác.

---

## 7. THỐNG KÊ & HOÀN TÁC

- **7 file code đã sửa** (Dev A 2, Dev B 3, Dev D 2). `DecorGrowthController.cs` KHÔNG sửa (md5 không đổi) vì Dev B chứng minh nó vô can.
- 0 file `.unity` / `.prefab` / `.asset` / `.meta` bị đụng. 0 lệnh git ghi dữ liệu. Chưa commit.
- Backup: `production/backup_vong3_2026-09-06/` (8 file `.bak`). Muốn quay đầu: chép ngược `.bak` về đúng chỗ.

| File | md5 mới |
|---|---|
| `PenClickDetector.cs` | `b815e04fabb6ca42d9b38e136ed3a153` |
| `PenMiniPanelUI.cs` | `54da12500bb3a97c12d1d638897dd8b7` |
| `PenSupplyTrayV2.cs` | `0ab836346f4a0e8fba828841955b44c5` |
| `LivestockAI.cs` | `a974cf2f7dbf3612831d96eccdd921db` |
| `HappyHarvestAnimalVisualSpawner.cs` | `4fdef722d2c5231a4d1d536799f11ad0` |
| `TrainStationMasterPopupUI.cs` | `28436a1e772f391edb8316579ace3687` |
| `TrainStationBuilding.cs` | `f2991bff6c35a964d249f19b202688f1` |

## 8. NỢ KỸ THUẬT GHI NHẬN (task riêng, chưa làm)

1. `EnsurePopupsExist()` nằm trong `#if UNITY_EDITOR` ⇒ 2 popup tàu không tồn tại trong build thật.
2. `PlacementManager.cs:1161-1174` `FixBuildingRenderSorting` ép MỌI SpriteRenderer con về `Max(order,500)`. Hiện vô hại (con bò 1 sprite), nhưng khi art giao con vật nhiều bộ phận sẽ san phẳng thứ tự tứ chi. Đề xuất thêm `if (sr.GetComponentInParent<SortingGroup>() != null) continue;`.
3. Layer ma `"Crop"` (`PlotCropVisual.cs:22`, `HarvestSlashFX.cs:35,55`) và `"FX"` (`PlantDragController.cs:277`) vẫn không tồn tại.
4. 38 prefab còn mang ghost sorting ID `1669604809`.
5. `ProjectSettings.asset:888` `activeInputHandler: 2` (Both) ⇒ `OnMouseDown` và `Update` cùng bắn 1 frame ở nhiều nơi, hiện chỉ được cứu bởi `popupLockCount`.

---

# PHỤ LỤC VÒNG 4 - 06/09 (sau ảnh test của Sếp)

## A. XÁC NHẬN ĐÓNG 2 BUG (bằng ảnh Sếp gửi)
- Khay chuồng gà ĐÃ mở lại được (ảnh 2 thấy khay 2 ô: rổ + bao thóc) ⇒ **Task 2b ĐÓNG**.
- Gà đang vẽ TRÊN rào, không còn bị chôn ⇒ **Task 4 phần sorting ĐÓNG**. Còn lại chỉ là art rào 2 lớp.

## B. THƯ MỤC `Assets/Assetsgame/Buiding trang trí` - ĐÃ SOI TẬN NƠI
Dev C vòng 3 bỏ sót thư mục này (tên viết sai chính tả "Buiding" + có dấu tiếng Việt, tên file
là `ChatGPT Image 22_46_24 1 thg 9, 2026.png` nên không khớp bất kỳ từ khoá slug nào).

**Nội dung: 15 file PNG 1536x1024, mỗi file là 1 SHEET 5 STAGE chưa cắt** (lưới 3x2, ô 6 bỏ trống).
Lead đã dựng contact sheet và xem tận mắt: `production/_qc_buiding_trangtri.jpg`.

**KẾT LUẬN: 15 sheet này ĐÚNG BẰNG 15 slug đã cắt. Không có gì mới để đưa vào.**
Đối chiếu 1-1 (`production/_qc_stage3_dacat.jpg`): bunhin · coixaygio · chaucaythu · chanhoa · cotden ·
vonghoa · chulun · hoda · xehoa · giabanrau · dainuoc · gieng · meovuive · binhtuoihoa · rom.

**Trả lời câu hỏi của Sếp:**
- "còn cái nào chưa đưa vào?" → **KHÔNG còn cái nào.** 15/15 sheet đều đã được cắt và nạp đủ 5/5 stage.
- "hay là bỏ vào đủ rồi, các vật trang trí stage kia đang dư thừa?" → **KHÔNG dư thừa.** 15 file trong
  `Buiding trang trí` là **FILE GỐC** để cắt, còn `Assets/Art/Decor/Stages/<slug>/stage_N.png` là **thành phẩm
  đã cắt** mà game thật sự đọc. Hai bộ phục vụ hai mục đích khác nhau, giữ cả hai.
- "xoá UI cũ để thay thế các stage này?" → **KHÔNG cần và KHÔNG nên.** Hệ stage đã chạy đúng cho 15 món.
  Vấn đề duy nhất là 4 món CÒN LẠI chưa có art, không phải hệ cũ sai.

**Vẫn thiếu đúng 4 món (20 file), y như kết luận vòng 3:** `banghieu`(3) · `ghehoa`(7) · `heothantai`(8) · `vitvuive`(12).

## C. PHÁT HIỆN MỚI: DATA MÓN id 8 VÀ id 9 BỊ RỐI
| | id 8 | id 9 |
|---|---|---|
| File asset | `Heo thần tài.asset` | `Mèo vui vẻ.asset` |
| `itemName` trong data | "Heo Thần Tài" | **"Heo Vui Vẻ"** (không phải Mèo) |
| Icon đang trỏ | heo hồng vòng hoa nằm trên cỏ (`wkPGx`) | **3 chậu cây hình thú mèo/gấu/thỏ** (`s2Uvz`) |
| Slug 5 stage | CHƯA CÓ | `meovuive` = **heo hồng vòng hoa ngồi bệ đá**, đủ 5/5 |

Ba điểm lệch:
1. Tên FILE là "Mèo vui vẻ" nhưng tên HIỂN THỊ trong game là "Heo Vui Vẻ". Slug `meovuive` vẽ heo ⇒ **stage art khớp với TÊN HIỂN THỊ, chỉ lệch tên file**. Không phải bug người chơi thấy.
2. **Icon của id 9 SAI THẬT**: đang là 3 chậu cây hình thú, trùng hệt art của `chaucaythu` (id 16).
   Người chơi mở shop sẽ thấy id9 và id16 giống nhau, mua về lại ra con heo.
3. id 8 "Heo Thần Tài" cũng là heo ⇒ khi đội vẽ làm gói A phải vẽ KHÁC hẳn `meovuive`, nếu không hai
   món trong shop nhìn như một. **Đã thêm khối cảnh báo phân biệt vào `PROMPT_SPRITE_FORGE_2026-09-06.md`.**

→ Việc sửa icon id 9 đụng file `.asset` (DANH SÁCH DỪNG), Lead không tự làm. Xem mục CẦN SẾP.

## D. TUTORIAL CHĂN NUÔI - Dev E đã sửa 2 lỗi
**Lỗi 1: badge bao thóc = 0 nên tutorial dạy cho ăn mà không có gì để cho ăn.** Hai nguyên nhân chồng nhau:
- `StarterInventorySetup` có mặc định `cam_ga` x5 nhưng ô `starterItems` trong scene đã điền tay chỉ 2 dòng
  (`seed_rice`, `seed_huong_duong`) ⇒ nhánh mặc định không bao giờ chạy.
- `TutorialManager.LayChuongTutorial()` so `gameObject.name` với `{Pen_03, Pen_03(Clone)}`, nhưng trong
  `Pen_03.prefab` script `PenMiniPanelUI` nằm ở object CON tên `PF_PenMiniPanel` ⇒ hàm **luôn trả null**
  ⇒ hàm cấp thóc có sẵn từ trước **chưa từng chạy một lần nào**.
Đã sửa: dò tên theo cả chuỗi cha + dự phòng so `Config.penId == "pen_03"`; nâng mục tiêu lên **3 bao**;
chống cộng trùng bằng cờ `TUTORIAL_PEN_FEED_GIVEN`; chỉ bù phần thiếu (đang có 2 thì chỉ cộng 1).

**Lỗi 2: bàn tay hướng dẫn nằm dưới khay.** Đo thật: `Canvas_TutorialHand` order **440**, khay V2 order **800**.
Đã nâng tay lên `order khay + 50` = **850**, đọc số thật từ `PenSupplyTrayV2.OrderKhay` chứ không gõ số ma.
Chọn 850 vì: phải trên khay 800 và trên bảng tiến trình chuồng 500; phải **dưới 999** vì món đồ người chơi
đang kéo dùng Canvas 999 (món phải bám ngón tay, vẽ trên tay); dưới 9999 để màn chuyển cảnh không hở.
Lớp ảo ảnh Phantom bỏ số cứng 450, nay bám theo tay = 860. Kết thúc tutorial tự trả về 440.
Tay không chặn click: mọi `Hand_Image` đã `m_RaycastTarget: 0`, ảo ảnh có `blocksRaycasts = false`.

**Rào an toàn Dev E tự đặt:** nếu tay nằm thẳng trên Canvas gốc `Tutorial_Canvas` thì KHÔNG nâng, chỉ
LogWarning, vì nâng canvas gốc sẽ kéo `Dim_Background` lên trên `Canvas_Popup` và nuốt sạch click.

**Rủi ro cần Sếp test kỹ:** sửa `LayChuongTutorial()` làm SỐNG LẠI các nhánh L2_07..L2_10 trước giờ chết
(tự mở khay hộ, tự bù kim cương, tự bỏ qua bước khi đã cho ăn). Đúng ý code gốc nhưng chưa từng chạy thật.

File vòng 4 (3 file, backup `production/backup_vong4_tutorial_2026-09-06/`):
| File | md5 mới |
|---|---|
| `TutorialManager.cs` | `0cccf8c4c957852c9c8dc29980638d2d` |
| `PenSupplyTrayV2.cs` | `23e1ffa95cabff77b764c975f19879e6` |
| `TutorialPhantomDemoManager.cs` | `68c0d6f6dea04aea099279e7e05b24f2` |

## E. 🧑 CẦN SẾP LÀM THÊM (ngoài các bước ở mục 5 phía trên)

**Kéo popup tàu về đúng cha (hướng dẫn chi tiết):**
1. Thoát Play Mode (bấm nút ▶ cho tắt). **Sửa scene lúc đang Play sẽ mất hết khi thoát.**
2. Tab **Hierarchy** (cột trái), tìm ô Search gõ: `Popup_Train_MasterStation`
3. Bấm vào kết quả, rồi **xoá chữ trong ô Search** để cây thư mục hiện lại (object vẫn được chọn, sáng xanh).
4. Nhìn lên trên nó: sẽ thấy nó đang thụt vào trong `Popup_LevelUp_Township`.
5. **Giữ chuột trái kéo `Popup_Train_MasterStation`, thả vào đúng chữ `Canvas_Popup`.**
   Mẹo: kéo tới khi thấy `Canvas_Popup` được bôi sáng cả dòng (không phải chỉ hiện vạch ngang mỏng
   giữa 2 dòng). Vạch ngang = thả thành anh em; bôi sáng cả dòng = thả vào bên trong. Sếp cần **bôi sáng cả dòng**.
6. Thả ra. Giờ `Popup_Train_MasterStation` phải thụt vào thẳng dưới `Canvas_Popup`, ngang hàng với `Popup_LevelUp_Township`.
7. Ô tick vuông cạnh tên nó phải **để TRỐNG** (tắt). Nếu Unity tự bật thì bỏ tick đi.
8. **Ctrl+S** lưu scene. Tiêu đề cửa sổ Unity phải hết dấu `*`.
9. Nếu Unity hỏi "Prefab instance..." thì chọn **Continue** hoặc **Apply to nothing**, đừng chọn Revert.

**Sửa icon món id 9 (tuỳ Sếp, không gấp):**
Chọn `Assets/_Game/Farm/CÔNG TRÌNH/Mèo vui vẻ.asset`, ô `Item Icon` đang là 3 chậu cây hình thú
(trùng món id 16). Kéo thay bằng `Assets/Art/Decor/Stages/meovuive/stage_3.png` cho khớp thứ người chơi
thật sự nhận được. Cân nhắc đổi luôn tên file asset thành `Heo vui vẻ.asset` cho khỏi rối về sau.

**File QC Lead dựng để Sếp tự đối chiếu (mở bằng ảnh, không cần Unity):**
- `production/_qc_buiding_trangtri.jpg` - 15 sheet gốc chưa cắt
- `production/_qc_stage3_dacat.jpg` - 15 slug đã cắt, kèm số stage mỗi slug
- `production/_qc_doichieu_heo.jpg` - đối chiếu icon id8 / id9 / stage heo / 3 món còn thiếu

---

# PHỤ LỤC VÒNG 5 - 06/09: ẨN 4 MÓN DECOR THIẾU ART

Sếp duyệt: *"nếu không có trong source thì xoá hoặc ẩn đi"*. Lead chọn **ẨN, KHÔNG XOÁ**, lý do ở mục C.

## A. XÁC MINH LẦN CUỐI (Dev F đo lại độc lập, khớp 100%)
| Hạng mục | Đo được |
|---|---|
| `DecorGrowthConfig.enabled` | `1` (bật) |
| Số `stageSet` | **15**, id 1,2,4,5,6,9,10,11,13,14,15,16,17,18,19 |
| Sprite null trong config | **0** (15 bộ đều đủ 5/5) |
| `Assets/Art/Decor/Stages/*/` | 15 thư mục, mỗi cái đúng 5 PNG |
| `DecorStageArtTool.BangMap()` | 15 entry, slug trùng khít |
| File `DecorData` trong `CÔNG TRÌNH/` | **19**, itemID chạy liền 1..19 |
| `ShopManager.decorList` | 18 entry |

**Thiếu art đúng 4 món: id 3, 7, 8, 12.** Không có món thứ 20 nào bị bỏ sót.

**Chi tiết mới:** `Bảng hiệu` (id 3) **vốn đã không nằm trong `decorList`** từ trước, nên runtime chỉ ẩn
thêm **3 món**. Tab Trang trí đi từ 18 ô xuống **15 ô**.

## B. TẠI SAO KHÔNG XOÁ (phát hiện quan trọng, suýt nữa thì hỏng)
`decorList` **không chỉ** là danh sách shop. Nó còn là **bảng tra cứu khi khôi phục world**:
- `PlacementManager.cs:1622` `FindItemById()` duyệt `ShopManager.Instance.decorList`
- `PlacementManager.cs:1643` `FindItemByPrefabName()` cũng vậy
- `ConstructionManager.cs:886` `FindItemById()` cũng vậy

⇒ **Xoá phần tử khỏi `decorList` thì mọi món người chơi ĐÃ MUA VÀ ĐẶT sẽ tra không ra data và BIẾN MẤT
khỏi sân.** Đây chính là lý do bộ lọc chỉ được phép chặn ở tầng vẽ giao diện, tuyệt đối không đụng danh sách.

## C. BỘ LỌC ĐÃ CÀI (1 file duy nhất: `ShopManager.cs`)
Chèn `if (BiAnViThieuArt(item)) continue;` vào `RenderItems()` — vòng lặp DUY NHẤT sinh ô món hàng.

Điều kiện ẩn (hàm `BiAnViThieuArt`), soi gương đúng nhánh decor của `DecorGrowthConfig.ShouldApply()`:
chỉ xét `DecorData` · đọc config động qua `DecorGrowthBootstrap.Config` · `FindSet(id)` trả null hoặc
`!IsValid` thì ẩn.

**KHÔNG hard-code một id nào.** Sếp nạp art rồi chạy `DecorStageArtTool`, `stageSet` mới xuất hiện là món
**tự hiện lại**, không cần ai sửa code lần nữa.

Ba cửa an toàn: `enabled == false` ⇒ không ẩn gì · `applyToDecor == false` ⇒ không ẩn gì ·
món trong `excludedItemIDs` (Đất, Chậu Hoa 1-4, vốn đặt thẳng là ĐÚNG thiết kế) ⇒ không ẩn.

**Công tắc:** `anMonThieuArt` (mặc định tick) trên Inspector của `ShopManager`, mục
`[Decor5] An mon decor thieu art`. Bỏ tick là hiện lại đủ 18 ô ngay.

**Log khởi động:** `[Shop] An 3 mon decor thieu art 5 stage: Ghế Hoa, Vịt Vui Vẻ, Heo Thần Tài`
(viết gọn 1 dòng và bọc trong `{}` riêng để `remove_debug_logs.ps1` không nuốt câu lệnh dưới).

**Lead đã verify:** `decorList` xuất hiện 7 lần trong file, **toàn bộ là ĐỌC**. Grep
`Add|Remove|Clear|RemoveAll|Insert|gán mới` = 0 kết quả (dòng 50 là khai báo khởi tạo, không phải ghi đè).
Ngoặc cân bằng `{}` 49/49, `()` 220/220, `[]` 12/12. Không có `if` rỗng. LF thuần + BOM giữ nguyên như gốc.

## D. 🧑 SẾP TEST (không phải sửa scene/asset gì cả)
1. Chờ compile, Console 0 lỗi đỏ.
2. Play. Console phải có đúng 1 dòng `[Shop] An 3 mon decor thieu art 5 stage: ...`
3. Shop → tab Trang trí: đếm còn **15 ô** (trước 18), không có ô trống hở.
4. **Test quan trọng nhất:** bỏ tick `An Mon Thieu Art` → Play → mua và đặt `Ghế Hoa` ra sân → thoát Play.
   Tick lại → Play lại. **Ghế Hoa phải còn nguyên ngoài sân**, chỉ là không mua thêm được trong shop.
5. Test công tắc: bỏ tick → tab Trang trí quay lại đủ 18 ô.
6. Test cửa an toàn: `DecorGrowthConfig.asset` bỏ tick `enabled` → Play → shop hiện lại đủ 18 ô, không log.
   **Nhớ tick lại sau khi test.**

## E. HOÀN TÁC
Bỏ tick `anMonThieuArt` là xong (không cần build lại). Muốn về sạch: chép
`production/backup_vong5_andecor_2026-09-06/ShopManager.cs.bak` đè lại.

| File | md5 trước | md5 sau |
|---|---|---|
| `ShopManager.cs` | `baba853c85ac55e4697036e4fdb05359` | `86cccad2de368d8a2547c939f26bc8a1` |
