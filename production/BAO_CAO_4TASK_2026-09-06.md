# BÁO CÁO — 4 TASK SẾP GIAO (06/09/2026)

> Đội: 1 agent SCAN → Lead verify → 3 Dev song song (không đụng file của nhau) → Lead CHECK chéo.
> Backup: `production/backup_4task_2026-09-06/` · **Chưa commit. Chưa compile trong Unity.**

## 0. NGUYÊN NHÂN GỐC
Commit `51353436` "push update chi tiết chức năng 06/09/2026" (06/09 00:33) gây **Task 1 + Task 2**.
Task 4 là bug hệ thống cũ (sorting layer `"CongTrinh"` đã bị xoá khỏi project). Task 3 là thiếu art.

---
## 1. TASK 1 — Tàu hoả ✅ ĐÃ SỬA
**Bug:** `TrainStationMasterPopupUI.Awake()` có `gameObject.SetActive(false)`. Popup tắt sẵn trong scene ⇒ Unity chưa chạy `Awake()`. Click ga tàu → `SetActive(true)` → `Awake()` chạy lần đầu ngay đó → tự tắt lại ⇒ `OnEnable()` (nơi `RefreshUI()`) không kịp chạy.
**Sửa:** thêm cờ `_openRequested`, bật TRƯỚC `SetActive(true)` trong `OpenPopup()`; `Awake()` chỉ tự tắt khi cờ chưa bật. Không đổi chữ ký public nào.
**Không phải khoá level** — hệ tàu không có unlock level; object `gataulua` trong scene đang bật.

---
## 2. TASK 2 — Process xây dựng ✅ ĐÃ SỬA (trừ nhà dân — xem §6)
**3 bug chồng nhau:**
1. `DecorProgressPopupBridge.IsOpen` dựa vào `_panel`, mà `_panel` chỉ gán trong `Build()` — **`Build()` không ai gọi** (grep 0 call-site) ⇒ `IsOpen` vĩnh viễn `false` ⇒ 4 lệnh `Close()` ở `DecorGrowthController` **không bao giờ chạy** ⇒ popup nằm lì, chặn kéo map.
2. `BuildingProcessPopupUI.LoadDesignAssets()` thiếu `FindObjectsInactive.Include` ⇒ mượn sprite luôn null. **Phát hiện thêm:** 2 field `_frameBgSpr`/`_trackBgSpr` (khung + máng) chưa từng được gán lẫn áp — đây mới là nguyên nhân chính "mất nền, card bo góc, khung".
3. Popup vô hình với `FarmInputLock` + `PopupManager.IsAnyPopupOpen()`.

**Đã sửa (3 file):**
- `DecorProgressPopupBridge.cs` — `IsOpen` đọc trạng thái THẬT từ `BuildingProcessPopupUI.Instance.IsOpen`. `Build()` giữ lại, đánh dấu DEAD CODE.
- `BuildingProcessPopupUI.cs` — `FindObjectsByType(FindObjectsInactive.Include)`; mượn thêm `_frameBgSpr`/`_trackBgSpr` và **áp** chúng; fallback `UIStandardSprites` (**đã verify dùng `Resources.Load` trước → build Android chạy được**, không dính bẫy `AssetDatabase` Editor-only); đăng ký `FarmInputLock.RegisterPopupOpen/Close` có cờ chống double; **`raycastTarget = false`** cho nền/khung/thanh/text, chỉ nút kim cương ăn click.
- `PopupManager.cs` — `IsAnyPopupOpen()` + `TenPopupDangMo()` biết tới popup này.

**Kiến trúc theo ý Sếp** (UI giống hệt ruộng, logic tách riêng): KHÔNG gộp class. Ruộng + nhà dân dùng `CropProcessPopupUI`; decor + chuồng-lúc-xây dùng `BuildingProcessPopupUI` nhưng nay **lấy đúng bộ sprite chuẩn** ⇒ nhìn giống hệt.

---
## 3. TASK 3 — Decor thiếu stage 🎨 CHỜ ĐỘI VẼ
Đối chiếu 19 DecorData ↔ art ↔ `DecorGrowthConfig`: **15/19 đủ 5 stage, 4 thiếu hoàn toàn**.

| itemID | Tên | slug cần | Trạng thái |
|---|---|---|---|
| 3 | Bảng Hiệu | `banghieu` | 0/5 — GUID còn chưa có trong shop |
| 7 | Ghế Hoa | `ghehoa` | 0/5 |
| 8 | Heo Thần Tài | `heothantai` | 0/5 |
| 12 | Vịt Vui Vẻ | `vitvuive` | 0/5 |

Đã rà cả `Assets/Art/Decor/Stages/` (15 slug) và **12 thư mục `art-handoff/`** (31/08→05/09): **0 kết quả** cho 4 slug này. **Art chưa từng được vẽ** — không phải bị thất lạc.
→ Đơn hàng: `production/PROMPT_SPRITE_FORGE_2026-09-06.md` gói A (20 file).
→ Khi art về: thêm 4 entry vào bảng map `DecorStageArtTool.cs` + chạy tool 1 lần là xong.

**Đính chính backlog:** M7-6 nói "id 16-19 chưa kéo vào `ShopManager.decorList`" là **thông tin cũ/sai** — đã grep scene, 4 GUID đó có trong `decorList` rồi → đóng được M7-6.

---
## 4. TASK 4 — Gia súc bị chuồng đè ✅ ĐÃ VÁ TẠM + CẦN ART
**Bug hệ thống:** Sorting Layer `"CongTrinh"` **không tồn tại** trong `TagManager.asset` (chỉ có `Bottom · Default · Objects · ObjectsFront · Foreground`). Nhưng **~20 file .cs** vẫn gán layer này, và **38 prefab** mang ghost ID `1669604809`. Layer này từng có thật, đã bị xoá.
Hệ khách du lịch đã bị đúng bug này và được vá 29/08 bằng `TouristSortingLayers` — **gia súc chưa bao giờ được áp bản vá đó**.

**Đã sửa (2 file, dùng lại bản vá Sếp đã duyệt):**
- `LivestockAI.cs` — bỏ `"CongTrinh"`, dùng `TouristSortingLayers.ResolveOrOverride(..., Visitor)` → resolve ra layer thật `"Objects"`; thêm `FenceSortingOrderFloor = 512` (rào đang ở order 500) làm sàn kẹp.
- `HappyHarvestAnimalVisualSpawner.cs` — bỏ 2 dòng gán `"CongTrinh"`, để `LivestockAI` tự quản.
→ 2 lớp bảo hiểm (layer thật + sàn order) ⇒ **con vật không còn bị chôn dưới rào.**

**KHÔNG đụng `TagManager.asset`** — thêm layer vào cuối danh sách sẽ khiến 38 prefab (nhà, giếng, decor, bù nhìn…) nhảy lên vẽ TRÊN CẢ `Foreground`, rủi ro vượt xa phạm vi 1 bug. Đề xuất task riêng có regression đầy đủ.

**Giới hạn còn lại (không code nào cứu được):** cả 4 chuồng dùng **1 file `chuongmoigiasuc.png` (500×500), 1 SpriteRenderer** phủ cả 4 cạnh ⇒ không thể vừa cho rào-sau ở sau con vật vừa rào-trước ở trước nó. **Bắt buộc tách art 2 lớp** → gói B trong đơn hàng art.

**FYI Lead — cùng bug, ngoài phạm vi hôm nay:** layer `"Crop"` (`PlotCropVisual.cs:22`, `HarvestSlashFX.cs:35,55`) và `"FX"` (`PlantDragController.cs:277`) **cũng không tồn tại**.

---
## 5. 🧑 CẦN SẾP LÀM TRONG UNITY (đúng thứ tự)

**Bước 0 — Compile.** Mở Unity, chờ biên dịch. Lỗi đỏ → chụp Console gửi Lead, **đừng sửa tay**.

**Bước 1 — Test tàu hoả.** Play → click ga tàu **lần đầu tiên** → popup phải hiện ngay (không phải bấm 2 lần).
Nếu vẫn không hiện: object `Popup_Train_MasterStation` trong scene đang bị đặt **nhầm cha** (nằm dưới `Popup_LevelUp_Township` thay vì `Canvas_Popup`) → kéo về đúng cha. *Sửa scene = DANH SÁCH DỪNG, Lead không tự đụng.*

**Bước 2 — Test process xây dựng.** Mua 1 **decor** + 1 **chuồng** → click lúc đang xây:
- Phải thấy: khung/card bo góc + thanh xanh lá + nút xanh dương có icon kim cương + title.
- Đóng popup rồi **kéo map** → phải kéo được. Nếu Console vẫn `[UiProbe] 🔴 KÉO MAP BỊ CHẶN` → bấm **F9** đọc tên GameObject thủ phạm, gửi Lead.
- Bấm nút kim cương → vẫn tăng tốc được.

**Bước 3 — Test NHÀ DÂN** (chỗ Lead chưa chắc, xem §6). Click nhà đang xây → **báo Lead thấy gì / Console log gì**.

**Bước 4 — Test gia súc.** Vào 4 chuồng, để con vật đi hết phạm vi → không còn bị chôn dưới rào.
Console **không nên** có warning `[TouristVisitor]` cho gia súc. Nếu CÓ → báo ngay (nghĩa là layer `"Objects"` cũng đã bị xoá).

**Bước 5 — Gửi đội vẽ:** `production/PROMPT_SPRITE_FORGE_2026-09-06.md` (gói A 20 file + gói B 2 file). Về hàng thả vào `production/art-handoff/2026-09-06_Decor4_Rao2Lop/`, báo Lead nạp.

---
## 6. ⚠️ CHƯA CHẮC — CẦN SẾP XÁC NHẬN
**Bug "click nhà dân không hiện gì" chưa được sửa trực tiếp.** Nhà dân đi đường riêng: `HouseGrowthController` → `CropProcessPopupUI.OpenForHouse()`. Dev B đã đọc kỹ cả 2 hàm, **logic trông đúng trên code tĩnh**, không tìm ra lỗi rõ ràng. `CropProcessPopupUI.cs` còn bị CONTRACT §0.4 cấm sửa.

**Giả thuyết của Lead: bug này sẽ TỰ HẾT sau fix hôm nay** — vì thủ phạm nghi ngờ chính là tấm chắn raycast kẹt của `BuildingProcessPopupUI` (nay đã đóng đúng + đã bỏ `raycastTarget`) làm `HouseGrowthController` bị chặn ở `FarmInputLock.BlockWorldClickBySceneOrPopup` trước khi kịp mở popup.

→ **Sếp test Bước 3 rồi báo lại.** Nếu vẫn hỏng, Lead có sẵn phương án B: chuyển 3 call-site trong `HouseGrowthController.cs` sang `BuildingProcessPopupUI` (file này đã có sẵn overload `Open(HouseGrowthController)` và đã được vá đủ) — cần Sếp duyệt vì đụng file ngoài phạm vi.

---
## 7. Thống kê & hoàn tác
- **6 file code đã sửa** · 0 file `.unity`/`.prefab`/`.asset` bị đụng · **chưa commit**.
- Backup: `production/backup_4task_2026-09-06/*.bak` (8 file). 2 file của Dev C khôi phục bằng `git checkout <path>`.
- Lead CHECK chéo đã verify: mọi API được gọi đều tồn tại thật (`TouristSortingLayers.ResolveOrOverride`, `FarmInputLock.RegisterPopupOpen/Close`, `UIStandardSprites.*`); 5 file sprite cần thiết **có thật** trong `Assets/Resources/UI/Standard/`; md5 khớp báo cáo của cả 3 Dev.
- Sự cố đã xử lý: `PopupManager.cs` dùng CRLF, lần ghi đầu bị đổi thành LF toàn file → Dev B tự phát hiện, phục hồi từ `.bak`, ghi lại ở byte-mode. Diff cuối chỉ còn 2 hunk đúng dự kiến.

---
# PHỤ LỤC — VÒNG 2 (sau log test của Sếp)

## Sếp báo đúng: vòng 1 của Lead làm TỆ ĐI. Nguyên nhân:
Lead cho thêm `FarmInputLock.RegisterPopupOpen()` vào `BuildingProcessPopupUI.Open()` và đưa popup này
vào `PopupManager.IsAnyPopupOpen()`. Nhưng **`FarmInputLock.BlockMapPan` VÀ `BlockWorldClickBySceneOrPopup`
đều gate trên 2 thứ đó** ⇒ hễ popup tiến độ "đang mở" là **khoá TOÀN BỘ kéo map + mọi click world**
(kể cả chuồng, nhà, ruộng). Popup này neo ở world, không che màn hình → **không hề cần khoá**.
→ **ĐÃ ROLLBACK** cả 2 chỗ. Giữ lại `ReleasePopupInputBlock()` trong `Close()` làm lưới an toàn gỡ khoá kẹt.

## Công cụ chẩn đoán ĐANG NÓI DỐI — đã sửa
`UiBlockerProbe` in `Prefab_Bush`, `House_01(Clone)`, `Đài nước(Clone)`, `Decor_binhtuoihoa(Clone)`
là "thủ phạm chặn map". **SAI.** Probe dùng `es.RaycastAll()` thô, mà Main Camera có
`Physics2DRaycaster` eventMask = Everything (`Main Camera.prefab:101`) ⇒ **mọi Collider2D world đều lọt vào**.
Trong khi `CameraController.ConTroDangTrenUI()` và `FarmInputLock.ConTroTrenUiThat()` **đều lọc
chỉ lấy hit từ `GraphicRaycaster`** ⇒ bụi cây/ngôi nhà **không hề chặn map**. Probe đổ oan.
→ Đã vá probe: tách nhãn `[UI THAT]` / `[world - KHONG chan map]`, và khi không có UI thật thì
**in thẳng trạng thái khoá** (`BlockMapPan`, `popupLockCount`, `TenPopupDangMo()`, các cờ) — từ nay
Sếp bấm F9 sẽ ra thủ phạm thật thay vì danh sách bụi cây.

## Thủ phạm THẬT của "click chuồng/decor không hiện process"
`DecorGrowthController.OnMouseUpAsButton()` dòng 473 dùng `EventSystem.current.IsPointerOverGameObject()` **thô**.
Vì `Physics2DRaycaster` bật Everything, hàm này trả `true` **ngay khi con trỏ nằm trên collider CỦA CHÍNH VẬT ĐÓ**
⇒ decor/chuồng **tự chặn click của chính mình**, `HandleClick()` không bao giờ chạy.
Project đã ghi rõ cái bẫy này trong `FarmInputLock.cs:52-63` (fix 04/09) nhưng file này **chưa từng được áp bản vá**.
SCAN vòng 1 đã nêu, Dev B bỏ qua vì ngoài phạm vi file — **lỗi điều phối của Lead**.
→ Đã đổi sang `FarmInputLock.BlockWorldClickBySceneOrPopup` (cổng đúng, không kiểm UI dưới con trỏ).

## Lead tự bắt lỗi của mình khi verify
Lần gỡ dòng ở `PopupManager.cs` làm dấu `;` rơi vào cuối một dòng comment ⇒ **file sẽ không compile**.
Bước kiểm cân bằng ngoặc/`;` bắt được trước khi bàn giao, đã sửa. 4/4 file cân bằng `{}` `()`.

## File vòng 2 (4 file)
| File | md5 mới |
|---|---|
| `BuildingProcessPopupUI.cs` | `35b1b00c4fc033640e2eb16effa7295c` |
| `PopupManager.cs` | `71935d8aac408d3aa44b371221f70fbe` |
| `DecorGrowthController.cs` | `c515ca91f3bce5dd84ae7461ffc75d03` |
| `Debug/UiBlockerProbe.cs` | `cf30791a4dffa1914d524fb2ad270f4f` |

## Sếp test lại — 3 việc
1. **Compile** → 0 lỗi đỏ.
2. **Kéo map ở chỗ trống + chỗ có bụi cây/nhà** → phải kéo được cả 2. Nếu vẫn kẹt, bấm **F9** rồi gửi Lead
   khối log mới (nay sẽ in thẳng cờ nào đang khoá, không còn đổ oan bụi cây).
3. **Click chuồng đang xây + decor đang xây** → phải hiện process. Nếu decor OK mà **nhà dân vẫn không hiện**
   thì nhà dân đi đường `HouseGrowthController` → `CropProcessPopupUI` (chưa đụng) — báo Lead để xử riêng.
