# 📋 BÁO CÁO RÀ SOÁT 12 ĐIỂM SẾP BÁO — 2026-09-03

> Lead + 5 DEV scan song song (read-only, KHÔNG sửa file nào). Mọi kết luận có bằng chứng `file:dòng`.
> Trạng thái: ❌ CHƯA FIX · ✅ ĐÃ FIX (cần Play test xác nhận) · ❓ CHƯA KẾT LUẬN ĐƯỢC TỪ CODE · 🎨 CẦN ART

## BẢNG TỔNG

| # | Vấn đề | Trạng thái | Bằng chứng | Rủi ro sửa |
|---|---|---|---|---|
| 1 | EXP tàu lửa cộng ĐÔI | ❌ | `TrainManager.cs:372` + `:786` → `HarvestFeedbackSpawner.cs:128` | THẤP |
| 2 | Nhà/hộp quà mất trạng thái sau thoát game | ❌ | `PlacementManager.cs:1486-1488` thiếu gán `houseId` | THẤP (nhưng STOP LIST) |
| 3 | M7-6/M7-7: 4 DecorData id 16-19 + giá gem | ⏸ chờ Sếp | tool `★ BẬT TOÀN BỘ GÓI` đã sẵn | — |
| 4 | Pháo hoa popup Lên Cấp nằm SAU popup | ❌ | `LevelUpPopupUI.cs:1182-1198`, `:1319-1323`; `SCN_Farm.unity:390989` | THẤP (nếu làm FX bằng UI) |
| 5 | Ô quà trong khung trắng không đồng nhất | ✅ code đã gộp | `LevelUpGiftSlotUI.cs:19-72`; `SCN_Farm.unity:442853` | — |
| 6 | 4 nhân vật popup là art game khác | ❌ 🎨 | `LevelRewardIconAutoFixer.cs:99-152` | — |
| 7 | Spritesheet nhân vật còn dính frame khác | ❌ 🎨 (2/3 file bẩn) | đo bên dưới | — |
| 8 | Khách du lịch đè lên tàu | ❓ | `TouristSortingLayers.cs:35` vs `TouristAgent.cs:61` | — |
| 9 | Text trắng chìm trên nền be (popup avatar) | ❌ | `AvatarProfilePopupUI.cs:763` | THẤP |
| 10 | Nút X popup avatar không bấm được | ❌ | `AvatarProfilePopupUI.cs:429-433`, `:535-575` | THẤP |
| 11 | Map khó kéo hơn trước | ❓ không phải regression code | `Main Camera.prefab:162-177` không đổi | — |
| 12 | Fillbar EXP dính lại vào avatar | ❌ | `TownshipHUDBuilderTool.cs:132,178,208,229,499` | THẤP |

---

## CHI TIẾT

### 1. EXP tàu lửa cộng ĐÔI — ❌ CHƯA FIX
`TrainManager.cs:372` gọi `AddExp(expPerReward)` NGAY. Rồi `:773-787` (`SpawnExpFly`) — vì 4 ref FX trong Inspector đang RỖNG (`expFlyFXPrefab`, `expTargetTransform`) nên luôn rơi vào nhánh dự phòng `:786` gọi orb cũ, và orb chạm đích lại `AddExp` lần nữa (`HarvestFeedbackSpawner.cs:128`) ⇒ **2× EXP**.
Chuồng KHÔNG bị (chỉ cộng 1 lần qua orb). Ruộng đã fix bằng cờ `legacyExpOrbsEnabled=false` (`PlotController.cs:103,706-712`).

**Fix đề xuất (rủi ro THẤP):** thêm tham số optional `bool addExpOnArrival = true` vào `HarvestFeedbackSpawner.SpawnExpFly` (`:62`), bọc `AddExp` ở `:128`; đổi DUY NHẤT call site `TrainManager.cs:786` sang `addExpOnArrival: false`.
❌ KHÔNG xoá `AddExp` ở `:372` — nếu sau này ai gán đủ prefab FX thì nhánh chính `:777-783` không cộng EXP ⇒ mất EXP hoàn toàn.

### 2. Nhà đang xây / hộp quà mất trạng thái — ❌ CHƯA FIX
`HouseGrowthController.GetSaveKey()` (`:98-102`) băm key theo `houseId + toạ độ`. `houseId` chỉ được gán đúng trong `Initialize()` (`:240-256`) lúc ĐẶT MỚI.
`PlacementManager.LoadBuildings()` (`:1451-1524`) Instantiate lại nhà nhưng **KHÔNG hề gán lại `houseId`** ⇒ key tính ra khác ⇒ `PlayerPrefs.HasKey = false` ⇒ `Start()` rơi vào default `state = Completed` (`:147-151`).
Hệ MỚI `DecorGrowth` đã làm đúng: key = `itemID + slotIndex` (không băm toạ độ), và không có key thì `Destroy(this)` chứ không suy diễn Completed (`DecorGrowthController.cs:26,28,292-320`).

**Patch đề xuất — 4 dòng, thêm ngay sau `PlacementManager.cs:1487`:**
```csharp
var houseGrowth = obj.GetComponent<HouseGrowthController>();
if (houseGrowth != null) houseGrowth.houseId = itemData.name; // khớp currentItem.name ở :1277
```
KHÔNG gọi `Initialize()` (nó reset `state=Building` + ghi đè thời gian).
Nằm trong STOP LIST (đụng `LoadBuildings` — đường khởi động MỌI công trình) ⇒ chờ Sếp duyệt. Backup: `PlacementManager.cs`.

### 4. Pháo hoa popup Lên Cấp nằm SAU popup — ❌ CHƯA FIX (báo cáo vòng 2 ghi SAI)
Grep toàn bộ: `LevelUpPopupUI.cs` **KHÔNG hề gọi** `ConstructionCelebrationFX.Play()` (hàm đó chỉ dùng cho ăn mừng xây nhà, world-space).
Nguyên nhân gốc: `Canvas_Popup` là **Screen Space – Overlay** (`SCN_Farm.unity:390989`, `m_RenderMode: 0`). Unity vẽ Overlay Canvas **SAU CÙNG**, đè lên MỌI thứ Camera render. Pháo hoa là ParticleSystem do Camera vẽ ⇒ dù `LevelUpPopupUI.cs:1319-1323` đã ép `sortingLayerName="Foreground"`, `sortingOrder=5000` thì **vẫn vô nghĩa** — sortingOrder chỉ so được giữa các vật do Camera vẽ với nhau, không so được với Overlay Canvas. Sửa sai chỗ.

**Fix đề xuất (rủi ro THẤP — khuyến nghị):** dựng pháo hoa bằng **UI thuần** (Image + RectTransform, animate bằng code) làm con của popup, `SetAsLastSibling()`. Chỉ đụng `LevelUpPopupUI.SpawnVFX`.
Phương án 2 (rủi ro TRUNG BÌNH, KHÔNG khuyến nghị): đổi `Canvas_Popup` sang Screen Space – Camera — canvas này dùng chung cho nhiều popup khác, đổi là phải test lại hết.

### 5. Ô quà không đồng nhất — ✅ CODE ĐÃ GỘP, cần Sếp Play test lại
`LevelUpGiftSlotUI.BuildProcedural()` (`:19-72`) **đã dùng chính 2 sprite của ô NEW**: `spr_circle_fill` + `spr_ring_circle`, cùng size 190×190 (comment `:22,:50` ghi rõ "đồng bộ 100% với ô Mở Khóa NEW").
Scene xác nhận `giftItemSlotPrefab: {fileID: 0}` (không còn prefab vuông-be cũ) → luồng chạy qua `BuildMergedGiftCells()` (`LevelUpPopupUI.cs:626-644`).
⇒ **Ảnh Sếp chụp nhiều khả năng là build CŨ** (trước khi patch compile). Không thiếu sprite nào — `spr_circle_fill.png`, `spr_ring_circle.png` đã có tại `Assets/_Game/Farm/Art/UI_LevelUp/`.
**Việc cần làm:** mở lại popup trong Play Mode bản hiện tại để xác nhận bằng mắt.

### 6. 4 nhân vật popup là art game khác — ❌ CẦN ART
`LevelRewardIconAutoFixer.cs:99-152` (`[InitializeOnLoad]`, tự chạy khi mở Editor) gán 4 file:
`Assets/Art/UI/LevelUpV2/characters/char_01..04/char_0N_master.png` — nguồn gốc `production/art-handoff/2026-08-31_JuiceFX/3_LevelUp_Mascots/` (boy · chef_female · cowboy · flower_girl · lumberjack). Đây là bộ art RỜI, không thuộc hệ NPC nào của game ⇒ đúng như Sếp nói.
Hiệu ứng (thở, chớp mắt, đung đưa) nằm ở `CelebrationCharacterSlot.cs` — **TÁI DÙNG ĐƯỢC**, chỉ cần thay sprite `puppetMaster`/`blinkSprite`.

Pool nhân vật CÓ SẴN trong project để cắt ra dùng:
| Nguồn | Nội dung |
|---|---|
| `Assets/NV_NPC/NVGAME/Processed/NV01..NV11/` | 11 bộ NPC ĐÃ CẮT SẴN, 4 hướng × 3 frame — pool tốt nhất |
| `Assets/NV_CHEF/Chef_NPC.prefab` | NPC đầu bếp |
| `Assets/NV_01/Fei.psb`, `NV_02.psb`, `Player.prefab` | nhân vật người chơi chính |
| `Assets/Nhanvientaulua/` | nhân viên tàu lửa |
| `Assets/Art/Characters/FlowerGirl/` | cô gái giỏ hoa (shipper) — đã có, style cartoon outline nâu |

⚠ DEV-B **không tìm thấy** 2 nhân vật nào trên Bảng Đơn Hàng: `OrderBoardPopupUI.anhKhachHang[]` có 12 slot (heo/cún/mèo/thỏ/gấu/cừu/bò/vịt/gà/sóc/nai/chuột) nhưng **toàn bộ `anh: {fileID: 0}` — chưa gán ảnh nào**, đang hiện khối màu placeholder (`Canvas_OrderBoardPopup.prefab:3672-3696`). ⇒ **Cần Sếp chỉ rõ "2 nhân vật bên kia" là ai/ở màn nào.**

### 7. Spritesheet nhân vật — ❌ 2/3 FILE BẨN (Lead đo bằng số, không đoán)

| File | Kích thước | Lưới | Alpha bẩn | Kết luận |
|---|---|---|---|---|
| `FlowerGirl/flowergirl_walk_spritesheet.png` | 848×1264 | 3×4 | **0 px** alpha 1-32 · 0 mảnh rác ≥30px | ✅ **NỀN SẠCH HOÀN TOÀN** |
| `Worker/worker_hammer_spritesheet.png` | 1200×896 | 4×3 | 0 px mờ, nhưng **3 ô hàng cuối có mảnh rác 104-288 px lấn từ ô bên cạnh** | ❌ DÍNH FRAME KHÁC |
| `Worker/worker_celebrate_spritesheet.png` | 1200×896 | 4×3 | **2 ô có mảnh rác, lớn nhất 1397 px** | ❌ DÍNH + VẼ SAI |

**Chi tiết `worker_hammer`:** vùng có nội dung theo cột = `(3,320) (328,620) (623,879) (926,1130)`, nhưng biên ô lưới là `300 / 600 / 900`. ⇒ **cán búa frame 1 tràn 20px sang ô frame 2; frame 2 tràn 20px sang ô frame 3.** Cắt lưới là dính búa của frame trước.
**Chi tiết `worker_celebrate`:** ô hàng-cuối-cột-1 vẽ **NHÂN VẬT KHÁC** (tóc đen, KHÔNG đội mũ bảo hộ, mũ đang bay ra) — sai hoàn toàn so với 11 frame còn lại.
**Vi phạm LUẬT ART #4 (cả 2 file worker):** có **khói/bụi/tia sáng BAKE thẳng vào frame** (hàng cuối `hammer`, nhiều frame `celebrate`) — luật yêu cầu code phun runtime.
**Vi phạm LUẬT ART #4 (cả 3 file):** frame KHÔNG cùng kích thước — sprite rect trong `.meta`:
- `flowergirl`: rộng 122→152 px (**lệch 30px**), cao 264→272 (lệch 8px)
- `worker_hammer`: rộng 190→292 px (**lệch 102px**), cao 246→281 (lệch 35px)
- `worker_celebrate`: rộng 133→241 px (**lệch 108px**), cao 249→278 (lệch 29px)
Với pivot Bottom-Center, lệch bề rộng 100px ⇒ **nhân vật GIẬT NGANG rất rõ khi chạy animation.**
**Vi phạm phụ:** canvas không chia hết cho lưới — `848/3 = 282.67`, `896/3 = 298.67` ⇒ tool cắt lưới nguyên sẽ trôi dần qua từng ô.
Ảnh QC nền hồng cánh sen + kẻ lưới: `production/_qc5/qc_fg.jpg`, `qc_wh.jpg`, `qc_wc.jpg`.

### 8. Khách du lịch đè lên tàu — ❓ CODE HIỆN TẠI TRÔNG ĐÚNG, cần Sếp xác nhận lại
- Tàu: sorting CỐ ĐỊNH, layer `"ObjectsFront"`, order 650-660 (`TrainPathFollower.cs:19-20,170-182`).
- Khách: `SortingGroup` Y-sort động, base order 5000 (`TouristAgent.cs:67,815-828`), layer resolve ra `"Objects"` (`TouristSortingLayers.cs:35`).
- Thứ tự layer thật (`TagManager.asset:40-55`): `Bottom < Default < Objects < ObjectsFront < Foreground`.
⇒ Layer luôn thắng order ⇒ **tàu (ObjectsFront) phải luôn vẽ trên khách (Objects)**. 11 prefab `Tourist_NV01..11` đều để trống `sortingLayerName`, `SCN_Farm.unity` không có override nào.

⚠ **CÁI BẪY tìm được:** comment `TouristAgent.cs:35` và Tooltip Inspector `:61` ghi *"ĐỂ TRỐNG = tự chọn 'ObjectsFront' (khuyến nghị)"* — **NÓI DỐI**, code thật trả `"Objects"`. Nếu ai làm theo tooltip mà gõ tay `"ObjectsFront"` vào prefab thì khách (order 5000) sẽ đè tàu (order 650) ngay lập tức — đúng y triệu chứng Sếp thấy.
**Đề xuất:** (a) sửa comment+tooltip cho khớp thật (rủi ro 0); (b) lưới an toàn: kẹp trần `sortingOrder` của khách xuống dưới 650 trong `UpdateDynamicSorting()`.
**Cần Sếp:** quay 1 clip ngắn / chụp đúng lúc lỗi + cho biết là khách của TÀU LỬA hay TÀU KHÁCH DU LỊCH, để chốt.

### 9. Text trắng chìm trên nền be — ❌ CHƯA FIX
`AvatarProfilePopupUI.cs:763` — `CreateText(expBar, "Txt_ExpValue", ..., Color.white, ...)`. Track thanh EXP là màu be `#e8d0a4` (`TaskPopupDesign.cs:99`), nền popup `#fdf3da/#fbeccb` (`:78-79`). Khi EXP thấp (16/129 ≈ 12%) thì hầu hết chữ nằm trên nền be ⇒ chìm. Shadow `#5a320f` alpha 0.55 (`:764`) không đủ.
**Fix:** đổi `Color.white` → `#442510` (hoặc `#654129`) tại `:763`. Các `Color.white` khác (`:625,649,668,698,717,798,839`) nằm trên nút màu đậm — KHÔNG đụng.

### 10. Nút X popup avatar không bấm được — ❌ CHƯA FIX (khoanh vùng, cần soi Prefab)
Listener có gán đủ (`:429-433`), nhưng **không có log khi `btnClose == null`** ⇒ lỗi âm thầm.
Nghi vấn chính: popup có 2 đường dựng —
- `CreateFreshHierarchy()` (`:596-823`) dựng CHUẨN: mọi Image con `raycastTarget=false` (`:620,623,626`) + `closeRt.SetAsLastSibling()` (`:803`).
- `AutoWireNewHierarchy()` (`:535-575`) — chạy khi hierarchy đã tồn tại sẵn trong Scene/Prefab — chỉ **tìm theo tên rồi gán**, KHÔNG kiểm/khôi phục `raycastTarget`, `interactable`, và **KHÔNG gọi `SetAsLastSibling()`**.
⇒ Nếu trong Prefab `Btn_Close` bị tắt Raycast Target / `interactable=false` / bị object thêm sau đè lên / đổi tên → nút chết mà code không tự vá.
**Cần Sếp kiểm trong Unity:** `Popup_AvatarProfile > Board_Wooden > Btn_Close` → Image `Raycast Target` = ON · Button `Interactable` = true · không có object nào nằm SAU nó trong Hierarchy phủ lên vùng 68×68 tại (470, 290).
**Fix code đề xuất:** thêm `Debug.LogWarning` khi null + trong `AutoWireNewHierarchy` ép lại `interactable=true; image.raycastTarget=true; SetAsLastSibling();`.

### 11. Map khó kéo — ❓ KHÔNG PHẢI REGRESSION TỪ CODE
- `CameraController.cs` **KHÔNG dùng** helper `TouchInput` ⇒ giả thuyết "slop DPI 18→24 làm khó kéo" **BỊ BÁC BỎ**. `TouchInput.cs` (130 dòng) chỉ pass-through, không thêm threshold/delay nào.
- Giá trị THẬT đang chạy nằm trong `Assets/_Game/Farm/CÔNG TRÌNH/Main Camera.prefab:162-177`: `panSpeed=3`, `panSmoothTime=0.12`, `dragThreshold=15` — **không đổi từ 03/08/2026**.
- ⚠ Có thay đổi UNCOMMITTED trong `CameraController.cs` (`dragThreshold 40→8`, `panSmoothTime 0.12→0.08`) nhưng đó là **default field, CHƯA apply lên prefab ⇒ hiện không có tác dụng gì**. Code một đằng, prefab chạy một nẻo — nên dọn.
- 🔍 **Nghi phạm cần Sếp kiểm:** `SCN_Farm.unity:239049-239069` — `Main Camera` có component **`CameraDevPanel` với `showOnStart: 1`** (bật sẵn khi chạy game). Nếu panel dev này phủ UI có `raycastTarget` lên màn hình, `EventSystem.IsPointerOverGameObject()` trả `true` tại vùng đó ⇒ `CameraController` **bỏ qua hoàn toàn việc bắt đầu drag** (`:236-243`).
- Lưu ý: hệ pan này **vốn không có inertia/fling thật** (chỉ `SmoothDamp` đuổi theo) — cảm giác "mất quán tính" có thể là đặc tính sẵn có.
**Việc cần làm:** Sếp mở Main Camera trong Inspector xem panel dev có hiện không & to cỡ nào; mọi thông số pan đều đã là field Inspector nên Sếp chỉnh trực tiếp được.

### 12. Fillbar EXP dính lại vào avatar — ❌ CHƯA FIX, đã bắt đúng thủ phạm
Thủ phạm: menu **`Tools/Farm/HUD/9. [Tuỳ Chọn] Dựng Lại HUD Toàn Bộ Từ Đầu (Sẽ Reset Vị Trí)`** → `BuildHUD()` (`TownshipHUDBuilderTool.cs:132`).
Chuỗi gây lỗi: `BuildHUD()` → `CleanupLegacyHUDObjects()` (`:178`) → mảng `oldNames[]` (`:492-501`) liệt kê thẳng `"TopLeft_Township_HUD"` (`:499`) → `DestroyImmediate` (`:526-530`) xoá cả cụm Avatar + EXP bar (kèm mọi vị trí Sếp kéo tay) → dựng lại với toạ độ **hard-code**: `Avatar_Button` pos `(70,-72)` size 140×140 (`:208`), `EXP_Bar_Container` pos `(385,-60)` size 400×56 (`:229`) ⇒ khoảng hở cố định **~45px** bất kể Sếp đã giãn ra bao xa.
`TownshipHUDController.cs` (runtime) chỉ update `fillAmount`/text, **KHÔNG** đụng vị trí ⇒ loại trừ.
**Trước mắt:** chỉ dùng menu `1. Cập Nhật Logic & Nối Dây HUD (Giữ Nguyên Vị Trí Kéo Tay)` → `WireAndFixExistingHUD()` (`:15-16`) — AN TOÀN. **TUYỆT ĐỐI KHÔNG bấm menu `9`.**
**Fix tool đề xuất:** trong `BuildHUD()`, trước `CleanupLegacyHUDObjects` lưu lại `anchoredPosition` của `EXP_Bar_Container` + `Avatar_Button` nếu đã tồn tại, dựng xong áp lại (thay hằng số hard-code); thêm `EditorUtility.DisplayDialog` xác nhận.

---

## 🎨 DANH SÁCH ART CÒN THIẾU (gộp cả đơn cũ chưa giao)

| # | Asset | Task | Trạng thái |
|---|---|---|---|
| A1 | 4 nhân vật popup Lên Cấp — CẮT từ nhân vật có sẵn trong game | mới | ❌ chờ Sếp chọn nguồn |
| A2 | `worker_hammer_spritesheet.png` — vẽ lại (tràn ô + khói bake + lệch 102px) | M7-2 | ❌ |
| A3 | `worker_celebrate_spritesheet.png` — vẽ lại (frame 09 sai nhân vật + khói bake + lệch 108px) | M7-2 | ❌ |
| A4 | `flowergirl_walk_spritesheet.png` — nền SẠCH rồi, chỉ cần chuẩn hoá canvas/lệch 30px | M7-3 | ⚠ sửa nhẹ |
| A5 | 2 spritesheet thợ búa worker02/03 (áo/mũ/dáng khác) | M7-10 | ❌ chưa giao |
| A6 | `meovuive` stage_2 — vẽ MÈO nhưng shop hiện "Heo Vui Vẻ" ⇒ sửa stage_2 thành HEO | M7-8 | ❌ chưa giao |
| A7 | 4 bộ 5-stage: Bảng hiệu · Ghế Hoa · Heo thần tài · Vịt vui vẻ | M7-9 | ❌ chưa giao |
| — | Popup tiến độ decor (M7-11) | M7-11 | ✅ chỉ copy sprite sẵn có, KHÔNG cần vẽ |
| — | Khung tròn viền vàng ô quà (điểm 5) | — | ✅ đã có `spr_circle_fill` + `spr_ring_circle` |
| — | 15 slug × 5 stage = 75 PNG | M7-5 | ✅ ĐỦ (đã kiểm 15/15 thư mục đều có 5 file) |

Đơn art chi tiết: `production/PROMPT_SPRITE_FORGE_2026-09-03.md`

---

# ✅ PHẦN B — ĐÃ TRIỂN KHAI (Sếp duyệt 2026-09-03, cùng ngày)

Backup toàn bộ: `production/backup_round5_2026-09-03/` (8 file .cs + `_CHECKSUM.txt` md5).
Revert bất kỳ file nào = copy đè từ thư mục đó.

## 6 DEV — mỗi file một chủ, không ai đụng file của ai

| DEV | File | Việc đã làm | Dòng |
|---|---|---|---|
| DEV-1 | `HarvestFeedbackSpawner.cs` + `TrainManager.cs` | Thêm tham số optional `addExpOnArrival = true`; `TrainManager:787` gọi `false` ⇒ hết cộng đôi. Giữ nguyên `AddExp` ở `:372` | 270→271, 961→962 |
| DEV-2 | `AvatarProfilePopupUI.cs` | Chữ EXP trắng → `#442510`, shadow đổi sang highlight sáng; `BindButtons` thêm LogWarning khi nút null; `AutoWireNewHierarchy` tự-vá nút X (ép `interactable`/`raycastTarget`/`SetAsLastSibling`); `OpenPopup` ép `CanvasGroup.blocksRaycasts` | 965→991 |
| DEV-3 | `TouristAgent.cs` + `TouristSortingLayers.cs` | Sửa comment/Tooltip nói dối "ObjectsFront"; thêm lưới an toàn `clampBelowTrain` (chỉ kích hoạt nếu khách lỡ sang layer tàu ⇒ **hành vi hiện tại KHÔNG đổi**); log cảnh báo 1 lần | 836→880, 121→127 |
| DEV-4 | `LevelUpPopupUI.cs` | Pháo hoa dựng lại bằng **UI thuần** (Image con của popup + `SetAsLastSibling`), 24-40 hạt, trọng lực + xoay + fade 1.5s, `Time.unscaledDeltaTime`, `raycastTarget=false`. Công tắc `useUIFireworks` (bỏ tick = về ParticleSystem cũ) | 1398→1600 |
| DEV-5 | `TownshipHUDBuilderTool.cs` | `BuildHUD()` LƯU `anchoredPosition/sizeDelta/anchor/pivot` của MỌI RectTransform trước khi cleanup, dựng xong ÁP LẠI; hộp thoại 3 nút; menu 9 đổi tên thành "(GIỮ vị trí kéo tay)" | 636→710 |
| DEV-6 | `PlacementManager.cs` | `LoadBuildings()` gán lại `houseGrowth.houseId = itemData.name` sau Instantiate ⇒ key save khớp ⇒ giữ đúng trạng thái nhà/hộp quà | 2594→2603 |

## QA gác cổng (Lead tự chạy, không tin báo cáo suông)

| Kiểm | Kết quả |
|---|---|
| tree-sitter-c-sharp, 8/8 file | **0 lỗi cú pháp** |
| Cân bằng ngoặc `{}` | 8/8 file khớp |
| Diff vs backup | **chỉ cộng thêm**: xoá/sửa 0-5 dòng, thêm 6-203 dòng mỗi file |
| Kiểu xuống dòng (CRLF/LF) | Bắt được `LevelUpPopupUI.cs` bị đổi CRLF→LF ⇒ **đã vá về CRLF**. 8/8 giờ khớp bản gốc |
| `using` cần thiết | đủ (`System.Collections.Generic`, `UnityEngine.UI` …); `Random` không nhập nhằng |
| Mọi call site `SpawnExpFly` | 3 nơi — `PenMiniPanelUI:403` và `PlotController:707` dùng default `true` ⇒ **không đổi hành vi**; chỉ `TrainManager:787` = `false` |
| API sai | Bắt được `Resources.GetBuiltinResource<Sprite>("UISprite.psd")` ⇒ **đã vá** thành `"UI/Skin/UISprite.psd"` |

## 🧑 CẦN SẾP LÀM TRONG UNITY

1. **Build lại** (Ctrl+R) → xác nhận Console **0 lỗi đỏ**.
2. **Test EXP tàu**: thu 1 thưởng tàu → EXP phải cộng **đúng 1 lần**.
3. **Test popup Lên Cấp**: pháo hoa phải nổ **TRÊN mặt popup**. Muốn art thật thay khối màu: import `confetti_01..06.png` + `spark_star.png` từ `production/art-handoff/2026-08-31_JuiceFX/1_Celebrate_FX/` vào Assets rồi kéo vào field **`Firework Sprites`** trên Inspector `LevelUpPopupUI`. Muốn revert: bỏ tick **`Use UI Fireworks`**.
4. **Test popup avatar**: chữ EXP phải đọc rõ trên nền be; **bấm nút X**. Nếu Console hiện `[AvatarProfile] btnClose = null` ⇒ object trong prefab đang sai tên, phải đổi lại đúng `Btn_Close` trong `Popup_AvatarProfile > Board_Wooden`.
5. **Test nhà**: đặt nhà mới → thoát lúc đang xây → mở lại (phải giữ Building đúng thời gian) → để xong, thoát lúc **chưa mở hộp quà** → mở lại (phải giữ hộp quà, KHÔNG tự Completed) → mở hộp → thoát → mở lại (phải Completed).
6. **HUD**: từ nay bấm menu `9` chọn **"Tiếp tục (Giữ vị trí)"**; chỉ chọn "Reset Sạch" khi cố ý.
7. **Map khó kéo**: mở `Main Camera` trong Inspector, xem `CameraDevPanel` (`showOnStart = 1`) có che vùng kéo không → tắt thử rồi kéo lại. Thông số pan đều là field Inspector: `panSpeed=3`, `panSmoothTime=0.12`, `dragThreshold=15` — Sếp chỉnh trực tiếp được.
8. **Khách đè tàu**: quay 1 clip ngắn lúc lỗi + cho biết là **tàu lửa** hay **tàu khách du lịch** để chốt.

## 📌 Còn treo, chưa làm (chờ Sếp)
- **M7-6/M7-7**: 4 DecorData id 16-19 vào `ShopManager.decorList` + duyệt giá gem 150/200/250/150 (sửa scene + quyết định kinh tế).
- **M7-13** / bug gán chéo `DecorGrowthBootstrap.FindBestTarget()` (greedy nearest-match theo tên + toạ độ) — hệ khác, cần task riêng.
- **Điểm 5** (ô quà): code đã đúng, chờ Sếp Play test xác nhận bằng mắt.

---

# 🔍 PHẦN C — VÒNG 6: điểm 2 (object đè map) & điểm 3 (tàu THUỶ)

Backup: `production/backup_round6_2026-09-03/` (3 file + checksum).

## ĐIỂM 2 — TÌM RA OBJECT ĐANG ĐÈ CHẶN KÉO MAP ✅

`CameraController.cs:241` (và `:333` cho Input cũ): `if (EventSystem.current.IsPointerOverGameObject()) return;`
⇒ **BẤT KỲ** UI nào đang bật + `raycastTarget=1` dưới con trỏ đều chặn kéo map, không liên quan cờ C# nào.

Quét toàn bộ `SCN_Farm.unity` (1878 GameObject, 747 Image/RawImage/Button) — bảng object đang BẬT + ăn raycast + phủ rộng:

| Object | Kích thước | Tự đóng lúc chạy? | Nguy cơ |
|---|---|---|---|
| **`Canvas_MarketPopup/Panel_Dim`** | **3840×2160 full màn hình** | ⚠️ **CHỈ trong 2 giây đầu** sau load, và chỉ khi còn tick `closeBoardOnSceneStart` | 🔴 **THỦ PHẠM** |
| `Canvas_MarketPopup/Panel_Dim/Popup_Board` | 1880×840 | (con của trên) | 🔴 |
| `Canvas_Popup/popup_Menu/Panel_Dim` (Shop) | 3840×2160 | `ShopManager.Start()` — vô điều kiện | 🟢 an toàn |
| `Canvas_Popup/WarehousePopup/Panel_Dim` | 3840×2160 | `WarehousePopupUI.Start()` — vô điều kiện | 🟢 |
| `Canvas_Popup/MillPopup_Root/...` | ~1560×900 | `MillPopupUI.Awake()` — vô điều kiện | 🟢 |
| `Popup_LevelUp_Township/{Bg_NenToi, V2_TapCatcher}` | full-parent | `LevelUpPopupUI.Start()` — vô điều kiện | 🟢 |
| `Tutorial_System/...`, `Canvas_TouristBoatPopup/...Dim` | full-parent | — | 🟢 object CHA đang tắt |

**Nguyên nhân gốc:** file scene **vẫn đang LƯU popup Chợ ở trạng thái MỞ**. Bản vá 2026-09-02 chỉ là band-aid theo đồng hồ thực (`Time.timeSinceLevelLoad < 2f`). Máy load chậm hơn 2 giây ⇒ tấm dim full-screen **ở lại vĩnh viễn**, map chết cứng toàn màn hình.

**✅ ĐÃ SỬA — `MarketManager.cs` (621→647 dòng):** bỏ hẳn `Time.timeSinceLevelLoad`, thay bằng cờ static `s_daDongBangTinLanDau` + `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` reset mỗi lần vào Play. Lần `Start()` ĐẦU TIÊN sau load scene luôn đóng (máy chậm 10 giây cũng đúng); mọi lần sau (người chơi tự mở popup) không đóng ⇒ đúng cả 2 kịch bản, vẫn giữ chốt an toàn chống "LỖI 1". Có log `[Market] Đã đóng bảng tin chợ...`.

> 💡 Sếp có sẵn `Assets/_Game/Farm/Scripts/Debug/UiBlockerProbe.cs` — **F9** in object đang chặn dưới con trỏ, **F10** in mọi lớp phủ ≥80% màn hình. Dùng để xác nhận sau khi build lại.

**Sửa gốc triệt để (cần Sếp, vì đụng scene):** Hierarchy → `Canvas_MarketPopup > Panel_Dim` → bỏ tick Active → **Ctrl+S lưu scene**. Xong thì không còn phụ thuộc code đóng hộ nữa.

## ĐIỂM 3 — TÀU THUỶ (không phải tàu lửa)

Đội đã điều tra lại từ đầu. Bảng sorting THẬT:

| Đối tượng | Layer | Order | Đặt ở đâu |
|---|---|---|---|
| Khách du lịch | **Objects** | 5000 + (-Y×0.5) | `SortingGroup`, `TouristAgent.cs:41` |
| **Thân tàu thuỷ** (`Boat/Visual`) | **ObjectsFront** | 200 | ép cứng `TouristBoatController.cs:118-119` lúc `Start()` |
| Gangplank (ván lên tàu) | Objects | 900 | `GangplankController.cs:53` |
| Nhãn "Đang đón khách…" | ❌ **(thiếu) → Default** | 700 | `TouristBoatController.cs:549` |
| Floating text bến khoá | ❌ **(thiếu) → Default** | 200 | `BoatDockSlot.cs:353` |
| (tham chiếu) Tàu lửa | ObjectsFront | 650 | `TrainPathFollower.cs:19-20` |

Thứ tự layer: `Bottom < Default < Objects < ObjectsFront < Foreground`. **Unity so LAYER trước, order chỉ phá hoà trong cùng layer.**

**✅ ĐÃ SỬA 2 lỗi thật:** cả 2 nhãn `TextMeshPro` trên tàu chỉ set `sortingOrder`, **quên `sortingLayerName`** ⇒ rơi về layer `Default` (thấp hơn `Objects` của khách) ⇒ **khách LUÔN che chữ, 100% tái hiện**. Đã thêm `mr.sortingLayerName = "ObjectsFront";` ở cả 2 chỗ; `BoatDockSlot` nâng order 200→210 (200 đang BẰNG thân tàu ⇒ thứ tự vẽ không xác định, dễ nhấp nháy).

**❓ Phần THÂN tàu — chưa kết luận được, và Lead không đoán tiếp:**
Thân tàu ở `ObjectsFront` còn khách ở `Objects` ⇒ **về lý thuyết tàu PHẢI luôn vẽ trên khách**. Đã loại trừ thêm:
- 11 prefab `Tourist_NV01..11` đều để trống `sortingLayerName` — không ai gõ nhầm "ObjectsFront".
- Khách **không** bị `SetParent` vào tàu khi lên tàu (`TouristAgent.TickBoarding()` chỉ đi tới `GetBoardPosition` rồi fade + destroy) ⇒ loại trừ nested SortingGroup.
- Quét cả 10 `SortingGroup` trong `SCN_Farm.unity`: **KHÔNG có cái nào nằm trên `BoatSystem` / `Dock_01..03` / `Boat` / `Visual`** ⇒ loại trừ giả thuyết "SortingGroup cha vô hiệu hoá sorting của tàu".
- Cây tàu chỉ có **đúng 1 SpriteRenderer** (`Boat/Visual`), không có renderer phụ nào bị bỏ sót.
- Bản vá `clampBelowTrain` hôm nay **không giải quyết** bug tàu thuỷ (ngưỡng 650 vẫn cho khách vượt order 200 của tàu nếu cùng layer) — nhưng cũng không gây hại, giữ nguyên cho tàu lửa.

**✅ CÔNG CỤ ĐO ĐÃ VIẾT — `Assets/_Game/Farm/Scripts/Debug/SortingProbeF8.cs` (438 dòng, MỚI):**
Tự gắn vào scene lúc Play (không cần kéo component). Bấm **F8** in báo cáo 4 phần:
- **A** mọi tàu + toàn bộ Renderer con (layer · layerID · order · enabled · vị trí)
- **B** mọi khách + `SortingGroup` + Renderer con
- **C** **PHÁN XỬ** từng cặp khách–tàu cách nhau <15 đơn vị: tính đúng luật Unity (so layer value trước, rồi order), đánh dấu **❌ SAI** cho mọi cặp khách vẽ trên tàu, kèm cột `[nguồn]` chỉ rõ giá trị đến từ Renderer riêng hay từ `SortingGroup` nào
- **D** bảng sorting layer thật của project

Bọc `#if UNITY_EDITOR || DEVELOPMENT_BUILD` (không lọt bản release), toàn bộ try/catch, log 1 lần bằng StringBuilder. Phím F8 đã kiểm không trùng (F9/F10 do `UiBlockerProbe`/`PopupGateDebugF9` dùng).

## QA vòng 6
| Kiểm | Kết quả |
|---|---|
| tree-sitter 4/4 file (gồm file mới) | **0 lỗi cú pháp** |
| Cân bằng ngoặc | 4/4 khớp |
| Kiểu xuống dòng | 3/3 giữ nguyên (MarketManager CRLF · TouristBoatController LF · BoatDockSlot CRLF) |
| Diff | chỉ cộng thêm (xoá/sửa 1-6 dòng, thêm 6-32 dòng) |
| Trùng tên class / trùng phím | không |

## 🧑 SẾP LÀM 3 BƯỚC
1. Build lại → kéo map thử. Nếu vẫn kẹt, bấm **F9** (UiBlockerProbe) xem object nào đang chặn.
2. Vào Hierarchy tắt `Canvas_MarketPopup > Panel_Dim` rồi **Ctrl+S** — sửa gốc, hết phụ thuộc code.
3. Tái hiện cảnh khách đè tàu thuỷ → bấm **F8** → copy nguyên khối Console (nhất là **PHẦN C**) gửi lại. Có số đo là fix được ngay, không cần đoán thêm vòng nào.
