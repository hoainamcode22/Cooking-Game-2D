# BÁO CÁO VÒNG 6 - 06/09/2026

> 5 Dev chạy SONG SONG, mỗi Dev một nhóm file riêng. Backup: `production/backup_vong6_2026-09-06/` (9 file .bak).
> **Chưa commit. Chưa compile trong Unity. 0 file `.unity`/`.prefab`/`.asset`/`.meta` bị đụng.**
> Lead CHECK chéo: 10/10 file cân bằng `{}` `()` `[]`, `#if`/`#endif` khớp, md5 khớp báo cáo Dev,
> line-ending giữ nguyên, không còn `if` rỗng, `LevelUpPopupUI.IsActive` còn nguyên 7 lần như bản gốc.

---

## 1. TUTORIAL CHẠY LẠI Ở CẤP 9 (P0)

**KHÔNG PHẢI LỖI CODE. Là 2 ô tick dev đang bật trong scene.**

`SCN_Farm.unity` dòng 580-582, GameObject `Tutorial_Manager`:
```
_skipIntroInEditor: 0
_devForceReplayTutorial: 1     <-- ĐANG BẬT
_devClearDoneFlagOnStart: 1    <-- ĐANG BẬT
```
Ô thứ hai xoá cờ `TUTORIAL_MAIN_DONE` ngay khi vào scene, ô thứ nhất còn bỏ qua cờ đó thêm lần nữa.
Nên dù Sếp không bấm nút reset nào, mỗi lần Play là tutorial chạy lại từ bước 1.
(Vòng 4 đã bảo Sếp tick để test, Sếp quên bỏ tick. Đây là lỗi quy trình của đội, không phải của Sếp.)

Đã loại trừ giả thuyết hồi quy vòng 4: diff 233 dòng cho thấy vòng 4 **không** sửa `Start()`,
**không** sửa `IsTutorialDone`, và `ClearTutorialDoneFlag()` chỉ có 2 người gọi, cả 2 đều là nút reset.

### ⚠️ RỦI RO SHIP nghiêm trọng hơn cả bug
Hai ô tick này được **serialize vào scene**. Build ngay lúc này thì **mọi người chơi thật** cũng bị xoá
cờ và bị dắt lại tutorial từ bước 1 mỗi lần mở game.

### Đã sửa (`TutorialManager.cs`)
1. **Gate theo level**: chưa có cờ đã-xong + cả 2 ô dev đều tắt + cấp > `CapCuoiTutorialDay` (**= 3**)
   ⇒ tự đánh dấu đã xong và bỏ qua, log 1 dòng.
   Chọn 3 có căn cứ: 30 bước tutorial trong scene chỉ phủ cấp 1-2 (18 bước `L1L2_*` + 10 bước `L2_*`,
   bước cuối `L2_10_HarvestPen`), không có bước `L3_*`/`L4_*` nào. Chuồng kế tiếp (heo) mở cấp 4 và
   tutorial không dạy. Cắt ở 3 để không đập oan người đang dở bước `L2_08` mà vừa lên cấp 3.
2. **Gate ĐỘC LẬP với cờ dev**: Sếp cố ý tick để test thì gate không chen ngang.
3. **Hai ô dev chỉ còn hiệu lực trong Editor** (`&& Application.isEditor`). Trong build, nếu scene còn
   tick thì chỉ ghi 1 dòng LogWarning rồi bỏ qua. Đây là rào chặn rủi ro ship ở trên.

---

## 2. BA POPUP TÀU ĐÈ LÊN NHAU (P0)

**Thủ phạm nằm trong PREFAB, không phải code.**

`Assets/Export_Train_UI_Package/Prefabs/Popup_Train_MasterStation.prefab` chứa **5 component
`TrainStationMasterPopupUI`**, không phải 1:

| Component | Gắn trên | Đúng/Sai |
|---|---|---|
| `&8845219234218148582` | `Popup_Train_MasterStation` (gốc) | ĐÚNG |
| `&6699675954752299846` | `Wagon_1` | SAI |
| `&7746490990097687554` | `Wagon_2` | SAI |
| `&1511072026557396521` | `Wagon_3` | SAI |
| `&176758927644746265` | `Wagon_4` | SAI |

**Nguyên nhân gốc: `StationWagonSlotUI` nằm CHUNG FILE với `TrainStationMasterPopupUI.cs`.**
Trong Unity chỉ class trùng tên file mới mang `fileID 11500000`. Khi prefab được save, 4 component toa tàu
bị ghi trỏ về `fileID 11500000` tức trỏ nhầm về class chính. Bằng chứng: dump 4 khối đó ra thì field
serialize là bộ field của master (`canvasComponent, mainWoodFrame, txtTitle, wagonSlots[4]...`), không phải
bộ field của `StationWagonSlotUI`. Trường `m_EditorClassIdentifier` còn sót chữ `StationWagonSlotUI`.

> **Đây CHÍNH LÀ "BÀI HỌC GHÉP NỐI" đã ghi trong `memory/MEMORY.md`: "mỗi file một chủ".**
> Lần trước nó gây build chạy code cũ im lặng. Lần này nó đẻ ra 4 popup ma.

Cơ chế nổ: `BuildOrFixHierarchy()` tự dựng CẢ popup trên chính `gameObject` của nó. Mỗi component đi lạc
trên một toa lại dựng thêm nguyên một khung gỗ + đúng câu hint. Câu hint nằm ở dòng 533 của script và
xuất hiện **0 lần** trong prefab, tức do code sinh, mỗi bản sinh một lần. Khớp ảnh Sếp gửi.

Vì sao "bây giờ" mới thấy: trước vòng 1, `Awake()` luôn `SetActive(false)` nên cả 5 bản đều tự tắt
(đúng triệu chứng "popup không mở được"). Vòng 1 + vòng 3 làm bản gốc mở được, bản gốc mở thì
`BuildOrFixHierarchy()` bật cả 4 toa, kéo theo 4 bản đi lạc cùng dựng popup.

**Đính chính vòng 3:** tiền đề "popup nằm dưới `Popup_LevelUp_Township` đang tắt" là **SAI**.
Đếm GUID script trong `SCN_Farm.unity` ra **0 lần**: popup vốn KHÔNG nằm trong scene, nó được
`EnsurePopupsExist()` sinh runtime làm con trực tiếp của `Canvas_Popup` (đang bật). Vòng lặp bật tổ tiên
của vòng 3 là **no-op**, không phải thủ phạm nhưng cũng không giúp gì. **Sếp KHÔNG cần kéo popup nữa.**

### Đã sửa (3 file)
- `TrainStationMasterPopupUI.cs`: thêm `LaBanDiLac()` (component nào có tổ tiên cũng mang script này thì
  không phải popup thật) ⇒ `Awake()` tắt `enabled` rồi `Destroy(this)` trước khi chạm `Instance`.
  Thêm singleton thật. `OpenPopup()`/`ClosePopup()` gọi nhầm trên bản đi lạc thì **chuyển hướng** sang
  popup thật thay vì tự dựng thêm (chống tái phát "không mở được"). Log gộp 1 dòng, in thêm `soBanMasterPopupUI`.
- `TrainStationBuilding.cs` + `TrainProcessPopupUI.cs`: chuyển sang `LayPopupThat()`.

Vì sao cách này chắc: một popup tự dựng UI **không bao giờ được phép nằm lồng trong popup cùng loại**.
Kiểm theo quan hệ tổ tiên nên đúng bất kể prefab còn bao nhiêu component rác và bất kể thứ tự `Awake()`.

---

## 3. PROCESS CHUỒNG ĐỨNG 00:00 TỪ LƯỢT 2

**Nguyên nhân: sai số `float`. Đây là bug tinh vi nhất hôm nay.**

`processStartUnix` khai báo `private float`, gán `(float)GetUnixNow()`.
Giây Unix hiện nay ~1.788.652.800, nằm giữa 2^30 và 2^31. `float` chỉ có 24 bit định trị, nên **ở vùng số
này bước nhảy của float là 128 GIÂY**. Mốc bắt đầu bị làm tròn về bội số của 128.
Chuồng gà `feedDurationSeconds = 45` (`Config_Pen03_Ga.asset:35`), `realTimeMultiplier = 1` ⇒ 45 giây,
**nhỏ hơn hẳn bước nhảy 128**.

Hệ quả đo được bằng mô phỏng float32 thật:
- Đồng hồ 45 giây chạy thực tế **từ 0 tới 109 giây** tuỳ thời điểm bấm.
- Mỗi 128 giây đồng hồ tường có **20 giây "vùng chết"**: cho ăn trong đó thì `remaining = 0` ngay.
- Lượt 1 kết thúc tại `mốc_làm_tròn + 45`, mà `[+45, +64]` **chính là vùng chết** ⇒ thu hoạch rồi cho ăn
  lại (mất 2-8 giây) thì **lượt 2 LUÔN rơi vào vùng chết**. Thử 16 kịch bản: 16/16 lượt 2 kẹt `00:00`.

**Bằng chứng khoá chặt từ ảnh Sếp:** nút gem hiện **15**.
`RushCostFor(t) = ceil(15 + 0.82*sqrt(t))` ⇒ `RushCostFor(0) = 15`, `RushCostFor(45) = 21`.
Nút hiện 15 nghĩa là `GetRemainingSeconds()` trả về **đúng số 0**, không phải lỗi hiển thị.

**Lỗi thứ hai (gây "00 miết" vĩnh viễn sau khi tắt mở game):** dòng 777 ghi
`processStartUnix.ToString("R")` **không truyền culture**, dòng 795 đọc lại bằng `InvariantCulture`.
Máy cài tiếng Việt ghi ra `"1,7886528E+09"`, đọc lại **parse thất bại** ⇒ mốc = 0 vĩnh viễn.
Đây là chỗ **duy nhất trong cả dự án** ghi số ra PlayerPrefs mà thiếu `InvariantCulture`.

**Lỗi thứ ba:** `Start()` phát hiện hết giờ thì `SetState(Ready)` nhưng **quên `SaveState()`** ⇒ lần mở
game sau gặp lại đúng tình trạng cũ.

### Đã sửa (`PenMiniPanelUI.cs`)
Đổi sang **`long` giây Unix** qua `DateTimeOffset.UtcNow.ToUnixTimeSeconds()`. Không phải cách mới, mà là
đúng cách dự án đang dùng: `PlotController` (cây trồng) dòng 567-568 và `ConstructionManager` dòng 395-403
đều dùng `long`. Số nguyên `long` không có sai số làm tròn.
Thêm `BeginProcessing()` là lối vào DUY NHẤT của Processing, gán lại cả mốc đầu lẫn mốc cuối mỗi lượt.
`ProcessTimerCoroutine` đếm theo `GetRemainingSeconds()` thay vì cộng dồn `Time.deltaTime`
(deltaTime đứng khi `timeScale = 0` và vẽ lại từ 0 sau khi tải lại scene).
Ghi save có `InvariantCulture`, `ParseUnixSeconds()` đọc được cả 3 dạng đã từng ghi ra kể cả save hỏng.
**Giữ nguyên tên khoá và kiểu string** nên `SaveAdapters.cs` không phải sửa dòng nào.

Rào tự chữa: nạp save thấy `Processing` mà mốc đã trôi qua hoặc mốc = 0 ⇒ chuyển `Ready` **và ghi đè save**
(bản cũ quên ghi nên lần sau kẹt tiếp). Thêm `TickProcessTimeout` cho ca coroutine bị Unity giết khi
chuồng bị `SetActive(false)`. Mô phỏng lại 12 kịch bản x 6 lượt: **mọi lượt đều đúng 45 giây**.

---

## 4. NÚT ĐÓNG POPUP NHIỆM VỤ BỊ CODE GHI ĐÈ

File: `Assets/_Game/Scripts/Mission/UnifiedTaskPopupUI.cs`. Popup này **dựng 100% bằng code**.
- Dòng 493-497: vòng lặp **huỷ TOÀN BỘ con của root popup** mỗi lần mở ⇒ mọi chỉnh tay của Sếp bay sạch.
- Dòng 594-611: dựng lại nút bằng số cứng 100x100, ép sprite, và `chuX.SetActive(false)` **ẩn luôn chữ X**.

**Nguyên nhân hình ảnh Sếp thấy:** `UIStandardSprites.Close` trỏ vào `btn_red_small.png` 256x96, đó là
một **thanh nút đỏ bo góc TRƠN, KHÔNG có dấu X**. Code kéo nó thành ô vuông 100x100 rồi ẩn chữ X đi.
Ra đúng "khối đỏ bo góc to, méo, không có dấu X". Trong game, dấu X ở 8 nút khác là **một object con TMP**
đè lên, không nằm trong ảnh.

### Đã sửa
Cờ `giuNutCloseChinhTay` (mặc định bật) + ô kéo `btnCloseChinhTay`. Tìm nút chỉnh tay TRƯỚC khi dọn cây,
**chừa nguyên nhánh chứa nó**. Có nút chỉnh tay thì code **chỉ nối sự kiện click**, không đụng
`sizeDelta`/`anchoredPosition`/`localScale`/`sprite`/`color`. Đường dự phòng dựng nút thì theo chuẩn game
(64x64, sprite qua `UIStandardSprites`, **luôn hiện chữ X**).

### ⚠️ 5 popup KHÁC cũng bị cùng bệnh (chưa sửa, để Lead mở task riêng)
| File | Mức độ |
|---|---|
| `Editor/CloseButtonSyncTool.cs:185` | **NGUY HIỂM NHẤT.** Menu `Tools/Farm/UI/Dong bo nut dong - 3. APPLY` ép MỌI nút đóng trong scene về 64x64 + sprite `btn_red_small`. Chạy 1 lần là xoá sạch chỉnh tay của cả 8 nút. **ĐỪNG BẤM.** |
| `AvatarProfilePopupUI.cs:546,663` | Huỷ hết con rồi dựng lại |
| `SettingsPopupUI.cs:629` | Dựng cứng 64x64, không huỷ con nên nhẹ |
| `KitchenSceneV2UI.cs:154` | Huỷ sạch con rồi dựng lại |
| `Mission/SkinVi.cs:48` | Ghi đè sprite/type/color mọi nút tên chứa "close" |

Mẫu chuẩn để noi theo: `TrainLoadPopupUI.cs` dòng 11+23 (chỉ `[SerializeField] Button` rồi `AddListener`).
Đó chính là lý do popup Tàu Chở Hàng giữ được chỉnh tay.

---

## 5. TEXT PHẦN THƯỞNG POPUP LÊN CẤP

Đo thật, 3 lỗi chồng nhau:
1. **Tràn ngang:** bảng chữ rộng `190+24 = 214` nhưng bước ô chỉ `190+16 = 206` ⇒ hai nhãn cạnh nhau
   **đè lên nhau 8px**. Đúng cái "dính liền" trong ảnh.
2. **Chữ không đều:** autosize `12-18` + tối đa 1 dòng ⇒ nhãn ngắn ở 18, nhãn 30 ký tự bị ép xuống **sàn 12**
   rồi cắt "…". Chênh 6 điểm nên hàng chữ lỗ chỗ.
3. **Badge đè chữ:** tag `MỚI` 104x46 xoay 8°, đáy chạm `y = -8` trong khi bảng chữ bắt đầu `y = -2` ⇒ **đè 6px**.

Nhãn dài KHÔNG nằm trong code, nó nằm trong data (`LevelUpRewardDataSetupTool.cs:150-151` ghi vào
`LevelRewardConfig.unlockEntries[].label`).

### Đã sửa (`UnlockSlotUI.cs`, `LevelUpPopupUI.cs`)
Rút gọn nhãn bằng **LUẬT, không phải bảng tra**, nên các level sau tự gọn theo:

| Nhãn cũ | Nhãn mới | Badge |
|---|---|---|
| `Mở khóa hạt Ngô` (15 ký tự) | `Hạt Ngô` (7) | MỚI (đỏ) |
| `Chuồng gà đã mở bán trong Shop` (30) | `Chuồng gà` (9) | MỚI (đỏ) |
| `Nhà dân mới sẽ mở ở cấp 3` (25) | `Nhà dân` (7) | **`Cấp 3`** (xanh) |

Đã thử luật với toàn bộ data thật: `Chuồng heo`, `Chuồng bò`, `Máy Xay Bột`, `Máy Ép Mía`, `Bột gạo`,
`Nước mía`, `Phô mai`, `Khoai tây` đều gọn đúng. Chuỗi không khớp luật nào thì **trả nguyên bản**, không cắt bừa.
Sau khi gọn, nhãn dài nhất cả hàng là `Hạt Bắp Cải` (11) thuộc ô quà, tức 3 ô đầu nay NGẮN HƠN 7 ô sau.

Badge `Cấp 3`: mục "Nhà dân" **chưa mở** ở cấp này, đeo tag `MỚI` đỏ là sai nghĩa.
Khuôn chữ: rộng `190-8 = 182` (< bước ô 206, hở 24px), autosize **20-26** (khoảng hẹp, mọi nhãn cùng cỡ),
tối đa 2 dòng, canh TRÊN-giữa để nhãn 1 dòng và 2 dòng cùng cao độ.
Badge dời lên đỉnh ô, hết đè chữ.

**Lỗi thứ 4 Dev K phát hiện thêm:** dải `Dai_MoKhoa` cao 250, ô 190 nằm giữa ⇒ chỉ còn **30px** cho chữ,
mà bảng chữ cũ 26px vừa khít. Chỉ cần làm chữ to hơn một chút là **bị RectMask2D xén ngang**.
Đã thêm `MERGED_CAPTION_BAND = 56` vào tính chiều cao hàng nên cụm được canh giữa đúng chiều cao thật.

---

## 6. 🧑 SẾP LÀM (theo thứ tự)

**Bước 0.** Compile, Console 0 lỗi đỏ.

**Bước 1 (quan trọng nhất, sửa bug tutorial ngay).** `SCN_Farm` → GameObject **`Tutorial_Manager`** →
component `Tutorial Manager` → mục "Dev, chạy lại tutorial để test (B2)":
**bỏ tick CẢ HAI ô** `Dev Force Replay Tutorial` và `Dev Clear Done Flag On Start`. **Ctrl+S**.
Play: không được có NPC chào mừng nữa.
Khi nào muốn xem lại tutorial thì tick lại (nhớ bỏ tick sau), hoặc dùng nút "Chơi lại từ đầu" trong Cài đặt.

**Bước 2. Dọn prefab tàu (sửa tận gốc).**
Mở `Assets/Export_Train_UI_Package/Prefabs/Popup_Train_MasterStation.prefab` →
`Main_Frame > Inner_Scene > Train_Container` → chọn `Wagon_1`..`Wagon_4`.
Mỗi toa đang thừa một component **`Train Station Master Popup UI`**. **Xoá đúng 4 component đó.**
Giữ nguyên `RectTransform` và `Image`. **ĐỪNG xoá component trên object gốc `Popup_Train_MasterStation`.**
Bản vá code đã chặn được rồi, nhưng dọn prefab mới là sửa tận gốc.

**Bước 3. Test tàu.** Click ga tàu, Console lọc `[Train]`, xem dòng `soBanMasterPopupUI=`.
Kỳ vọng **= 1**. Popup phải hiện đúng **một** khung.

**Bước 4. Test chuồng (ca gây lỗi).** Console lọc `[Pen]`.
Cho ăn → `conLai` phải là **45** (không phải 0), gem hiện **21** rồi giảm dần (không phải 15 đứng im),
thanh xanh chạy từ trống sang đầy. Thu hoạch → **cho ăn lại NGAY** → `conLai` lại phải là 45. Làm tới lượt 3, 4.
Rồi: cho ăn, đợi ~15 giây, Stop Play, Play lại → `NAP_SAVE ... conLai=~30s`.
Rồi: cho ăn, Stop Play, đợi ngoài đời hơn 45 giây, Play lại → chuồng phải **sẵn sàng thu hoạch**.

**Bước 5. Tạo nút đóng Nhiệm vụ.** `Canvas_Popup > UnifiedTaskPopupRoot` → chuột phải → `UI > Button`,
đổi tên **`Btn_Close`**. Chỉnh sprite/size/vị trí theo đúng ý Sếp.
Gợi ý sprite có sẵn dấu X, ảnh vuông 64x64: `Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/btn_close.png`
Rồi chọn `UnifiedTaskPopupRoot` → Inspector `UnifiedTaskPopupUI` → mục "Nút đóng - tôn trọng chỉnh tay
trong scene" → **kéo `Btn_Close` vào ô `Btn Close Chinh Tay`** (chắc ăn hơn dựa vào tên). **Ctrl+S**.
Play: Console in `[NhiemVu] Giu nguyen nut dong chinh tay: Btn_Close`, nút giữ đúng size/vị trí/sprite Sếp chỉnh.
**⚠️ Sau đó ĐỪNG BAO GIỜ bấm menu `Tools/Farm/UI/Dong bo nut dong - 3. APPLY`** (xoá sạch chỉnh tay cả 8 nút).

**Bước 6. Test popup lên cấp.** Lên cấp, xem hàng thưởng: 3 nhãn đầu phải là `Hạt Ngô` / `Chuồng gà` /
`Nhà dân`, mọi nhãn cùng cỡ chữ, không nhãn nào chạm nhau, badge không đè chữ.
Nếu dải xuống 2 hàng và chữ dưới bị cắt: `Dai_MoKhoa` → `RectTransform.sizeDelta.y` từ **250** lên **≥ 520**.

---

## 7. HOÀN TÁC & md5

Backup `production/backup_vong6_2026-09-06/` (9 file `.bak`). Chép ngược `.bak` là về nguyên trạng.

| File | md5 mới |
|---|---|
| `TutorialManager.cs` | `862178526cc483588d065d7e69aa4f07` |
| `TrainStationMasterPopupUI.cs` | `ae04c4d3f502ee39841c88ace20f3a95` |
| `TrainStationBuilding.cs` | `d7c80d476585e29fe8cb0c6677516ce7` |
| `TrainProcessPopupUI.cs` (ExportTrainUIPackage) | `ca63bd6ec7f9b10636cfa46c8140cbe0` |
| `PenMiniPanelUI.cs` | `74ac9b7283523155255480c450647588` |
| `UnifiedTaskPopupUI.cs` | `857797f3081dec4ebbb69a6a8c515fca` |
| `UnlockSlotUI.cs` | `009a71946b9c4e6a87db0c37c53bb15c` |
| `LevelUpPopupUI.cs` | `74f8c9f66d81351be6168505f3f30d79` |

## 8. NỢ KỸ THUẬT MỚI GHI NHẬN
1. `StationWagonSlotUI` nằm chung file `TrainStationMasterPopupUI.cs` ⇒ tách ra file riêng, nếu không
   lần save prefab sau vẫn tái diễn lỗi 4 popup ma.
2. `EnsurePopupsExist()` trong `#if UNITY_EDITOR` + scene không chứa popup nào ⇒ **bản build thật không có
   popup tàu**. Hai hướng: đặt sẵn popup vào scene, hoặc chuyển prefab sang `Resources/` dùng `Resources.Load`.
3. `UIStandardSprites.PathClose` tên là "Close" nhưng thực ra là nút đỏ TRƠN không có X. Gây hiểu nhầm
   cho mọi popup dùng nó. Là registry dùng chung nên cần task riêng.
4. `PopupEwarManager.cs:45` gọi `btnClose.onClick.AddListener()` **không kiểm null** ⇒ NullReference nếu
   ô Inspector bỏ trống.
5. Chú thích `EffectiveFeedSeconds` nói có chia `premiumSpeedMultiplier` khi cho ăn cám nhưng **code không chia**.
   Chuồng gà `= 1` nên không lộ, chuồng khác có thể lệch thiết kế.
6. 5 popup khác cùng bệnh ghi đè nút đóng (bảng ở mục 4).
