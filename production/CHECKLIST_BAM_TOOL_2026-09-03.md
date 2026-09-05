# 🎮 CHECKLIST BẤM TOOL — gộp 2 vòng làm việc 03/09/2026

> Tên menu lấy trực tiếp từ code, không viết theo trí nhớ. **Làm đúng thứ tự** — các bước sau phụ thuộc bước trước.
> Quy tắc chung: tool nào có DRY-RUN thì **chạy DRY-RUN xem báo cáo trước**, thấy sạch mới APPLY.

---

## BƯỚC 0 — Trước khi bắt đầu
| # | Việc | Ở đâu |
|---|---|---|
| 0.1 | **Build lại** (Ctrl+R) → Console phải **0 lỗi đỏ** | Unity |
| 0.2 | Nếu còn lỗi đỏ → dừng, gửi Lead. Đừng chạy tool khi chưa compile được | — |

---

## BƯỚC 1 — CẮT SPRITE NHÂN VẬT MỚI  ⭐ làm đầu tiên
7 spritesheet (3 thợ × 2 + cô gái giỏ hoa) → 84 sprite con.

| # | Menu | Ghi chú |
|---|---|---|
| 1.1 | `Tools ▸ Farm Game ▸ Characters ▸ ★ Slice 3 spritesheet nhân vật (DRY-RUN)` | Xem báo cáo: canvas · chia hết · tràn biên · baseline. Phải sạch hết |
| 1.2 | `Tools ▸ Farm Game ▸ Characters ▸ ★ Slice 3 spritesheet nhân vật (APPLY)` | Cắt thật |
| 1.3 | `Tools ▸ Farm Game ▸ Characters ▸ Kiểm tra sprite con đã slice` | Xác minh đủ 84 sprite |

> Tên sprite sinh ra: `hammer_01..12`, `celebrate_01..12` (thợ 01) · `w02_hammer_01..12`, `w02_celebrate_01..12` · `w03_hammer_01..12`, `w03_celebrate_01..12` · `fg_down/left/right/up_1..3`

---

## BƯỚC 2 — GÁN 3 BỘ SPRITE CHO 3 THỢ  (Inspector, không phải menu)
Mở `Assets/Resources/BuilderWorkerConfig.asset` → mục **"3 BỘ SPRITE RIÊNG THEO TỪNG THỢ"** (`workerSpriteSets`):

| Ô | Thợ | hammerFrames | celebrateFrames |
|---|---|---|---|
| `[0]` | Worker 01 — mũ vàng, yếm xanh | `hammer_01..12` | `celebrate_01..12` |
| `[1]` | Worker 02 — mũ cam, râu quai nón | `w02_hammer_01..12` | `w02_celebrate_01..12` |
| `[2]` | Worker 03 — mũ trắng, khăn đỏ | `w03_hammer_01..12` | `w03_celebrate_01..12` |

> Ô nào để trống thì thợ đó tự lùi về bộ cũ dùng chung — không crash, điền dần từng thợ cũng được.

---

## BƯỚC 3 — DỰNG NHÂN VẬT VÀO SCENE
| # | Menu | Phụ thuộc |
|---|---|---|
| 3.1 | `Tools ▸ Farm Game ▸ Worker ▸ ★ SETUP thợ búa (1 nút)` | cần Bước 1 + 2 |
| 3.2 | `Tools ▸ Farm Game ▸ Shipper ▸ Tạo Shipper_HomeAnchor trong scene` | bấm **riêng**, chỉ 1 lần |
| 3.3 | `Tools ▸ Farm Game ▸ Shipper ▸ ★ SETUP cô gái giỏ hoa (1 nút)` | cần 3.2 |
| 3.4 | `Tools ▸ Farm Game ▸ Shipper ▸ Kiểm tra sẵn sàng` | phải xanh hết |

> Lỡ tay: `Tools ▸ Farm Game ▸ Worker ▸ Hoàn tác setup thợ búa`

---

## BƯỚC 4 — ĐỒ TRANG TRÍ 5 STAGE
| # | Menu |
|---|---|
| 4.1 | `Tools ▸ Farm Game ▸ Decor 5 Stage ▸ ★ Nạp art 5 stage (DRY-RUN)` |
| 4.2 | `Tools ▸ Farm Game ▸ Decor 5 Stage ▸ ★ Nạp art 5 stage (APPLY)` |
| 4.3 | `Tools ▸ Farm Game ▸ Decor 5 Stage ▸ Tạo 4 DecorData item mới (DRY-RUN)` |
| 4.4 | `Tools ▸ Farm Game ▸ Decor 5 Stage ▸ Tạo 4 DecorData item mới (APPLY)` |
| 4.5 | `Tools ▸ Farm Game ▸ Decor 5 Stage ▸ Kiểm tra sức khoẻ reference (chỉ đọc)` |

---

## BƯỚC 5 — BẬT CẢ GÓI  ⚠️ làm SAU CÙNG
| # | Menu | Nó làm gì |
|---|---|---|
| 5.1 | `Tools ▸ Farm Game ▸ ★ BẬT TOÀN BỘ GÓI Nhân vật + Decor 5 stage (1 nút cuối)` | ① thêm 4 DecorData id 16-19 vào `ShopManager.decorList` ② tick `enabled=true` cả 3 config ③ **lưu scene** |

> 🚨 **NÚT TẮT KHẨN CẤP** — game lỗi thì bấm ngay, đưa về đúng như trước khi có gói:
> `Tools ▸ Farm Game ▸ TẮT KHẨN CẤP toàn bộ gói (enabled = false cả 3)`

---

## BƯỚC 6 — POPUP LÊN CẤP
| # | Việc | Ở đâu |
|---|---|---|
| 6.1 | `Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ HOÀN THIỆN NHÂN VẬT (vị trí + art, 1 nút)` | mới có `char_01` (ông lão NV06); char_02/03/04 đội vẽ chưa giao |
| 6.2 | *(tuỳ chọn)* Import `confetti_01..06.png` + `spark_star.png` từ `production/art-handoff/2026-08-31_JuiceFX/1_Celebrate_FX/` vào Assets → kéo vào field **`Firework Sprites`** trên Inspector `LevelUpPopupUI` | pháo hoa đang chạy khối màu tạm |
| 6.3 | Muốn quay lại pháo hoa cũ: **bỏ tick `Use UI Fireworks`** | Inspector |

---

## BƯỚC 7 — SỬA MAP KHÓ KÉO (sửa gốc, cần Sếp vì đụng scene)
| # | Việc |
|---|---|
| 7.1 | Hierarchy → `Canvas_MarketPopup ▸ Panel_Dim` → **bỏ tick Active** → **Ctrl+S lưu scene** |
| 7.2 | Kiểm `Main Camera` → component `CameraDevPanel` → nếu `showOnStart` đang bật mà không cần thì tắt |

> Bản trên máy Sếp đã tắt `Panel_Dim` rồi nhưng **chưa commit** — lưu scene để khỏi mất.

---

## BƯỚC 8 — HUD (chỉ khi cần)
| # | Menu | An toàn? |
|---|---|---|
| 8.1 | `Tools ▸ Farm ▸ HUD ▸ 1. Cập Nhật Logic & Nối Dây HUD (Giữ Nguyên Vị Trí Kéo Tay)` | ✅ luôn an toàn |
| 8.2 | `Tools ▸ Farm ▸ HUD ▸ 9. [Tuỳ Chọn] Dựng Lại HUD Toàn Bộ (GIỮ vị trí kéo tay)` | ✅ nay đã an toàn — hộp thoại hiện ra chọn **"Tiếp tục (Giữ vị trí)"**. Chỉ chọn "Reset Sạch" khi cố ý |

---

## BƯỚC 9 — 3 PHÍM CHẨN ĐOÁN (bấm trong Play Mode)
| Phím | Công cụ | Dùng khi |
|---|---|---|
| **F8** | `SortingProbeF8` | Khách du lịch đè lên tàu thuỷ → bấm đúng lúc thấy lỗi, copy **PHẦN C** trong Console gửi Lead |
| **F9** | `UiBlockerProbe` | Map kẹt không kéo được → in ra object đang chặn dưới con trỏ |
| **F10** | `UiBlockerProbe` | In mọi lớp phủ đang bật ≥80% màn hình |

---

## ❌ TUYỆT ĐỐI KHÔNG BẤM
| Menu | Lý do |
|---|---|
| `Setup Village Orders L1-L6` / `Apply Phase 1 Data` | **ghi đè bảng kinh tế đã duyệt** (ghi trong `memory/MEMORY.md`) |
| `Tools ▸ Farm Game ▸ Popups ▸ Dựng Popup Cài Đặt` / `Bake Cài Đặt & Hồ Sơ Vào SCN_Farm` | popup Cài đặt đang có **2 phiên cùng sửa** — chờ Sếp chốt trước |

---

## KIỂM THỬ SAU KHI XONG
1. **EXP tàu lửa**: thu 1 thưởng → EXP cộng **đúng 1 lần** (trước bị cộng đôi)
2. **Popup Lên Cấp**: pháo hoa nổ **TRÊN mặt popup**
3. **Popup avatar**: chữ đọc rõ trên nền be · **bấm được nút X**
4. **Nhà đang xây**: thoát game lúc chưa mở hộp quà → mở lại phải **giữ nguyên**, không tự Completed
5. **3 thợ xây**: phải khác nhau thật (mũ vàng / cam+râu / trắng+khăn đỏ), không còn lật ngang nhân bản
6. **Kéo map**: mượt như trước

---

# 📌 BỔ SUNG 03/09 chiều — map cứng & popup tàu

## A. Popup tàu hoả — ĐÃ KHÔI PHỤC (Lead làm xong)
- `Assets/Export_Train_UI_Package/Prefabs/Popup_Train_MasterStation.prefab` đã trả về **bản 09:33** (trước khi hỏng).
- Bản hỏng 12:05 **không bị xoá**, lưu tại `production/backup_round8_2026-09-03/Popup_Train_MasterStation_BAN_HONG_1205.prefab` — cần đối chiếu sau thì lấy ra.
- `.meta` không đụng ⇒ guid `c4c6499270a0dd140b6ae1100658b2d6` giữ nguyên, scene không đứt tham chiếu.
- Đã kiểm: **27/27 field serialize** của `TrainStationMasterPopupUI.cs` đều có trong prefab khôi phục ⇒ không mismatch dù script sửa lúc 12:52.
- ⚠️ Sếp cần chỉnh lại **vị trí con tàu trên đường ray** (phần Sếp làm lúc 12:05 đã mất). Lần này chỉnh xong **chụp lại Inspector trước khi Apply prefab** để có mốc so.

## B. Camera bounds — Sếp tự canh trong Play Mode
1. Play → chọn `Main Camera` trong Hierarchy → component `CameraController` → mục **Bounds (minX, maxX, minY, maxY)**.
2. Hiện: `X -5000 | Y 5000 | Z -5000 | W 5000`. **Z chính là minY** — cái đang chặn không cho xuống bến tàu.
3. Kéo `Z` từ `-5000` xuống dần (thử `-5600`), vừa sửa vừa kéo map xuống bến xem đã thoải mái chưa.
4. Nếu muốn tới rìa phải: tăng `Y` (maxX) từ `5000` lên `~6000`.
5. ⚠️ **Đừng giảm `X` (minX)** — `BlindPoint` ở X = −9818 là chỗ đậu tàu ngoài màn hình, nới ra là người chơi kéo thấy tàu "tàng hình".
6. Ưng số nào rồi: thoát Play, mở prefab `Assets/_Game/Farm/CÔNG TRÌNH/Main Camera.prefab` nhập lại đúng số đó (giá trị sửa trong Play Mode KHÔNG tự lưu).

## C. FPS 10 — cần Sếp đo giúp 30 giây
Kéo map cứng đơ là do FPS 10, không phải UI đè. Hai phép thử để biết nghẽn ở đâu:

| Thử | Nếu FPS nhảy lên | Kết luận |
|---|---|---|
| Bấm zoom preset **300** (zoom vào gần) | có | nghẽn ở **render** — zoom xa nhất (Ortho 1500, viewport 5333×3000) đang vẽ gần cả bản đồ |
| **Đóng popup tàu** | có | nghẽn ở **`Canvas_Popup`** — 697 CanvasRenderer trên 1 canvas, đổi 1 chi tiết là dựng lại cả 697 |

Chính xác hơn: `Window ▸ Analysis ▸ Profiler` → tab CPU → xem cột nào cao nhất (Rendering / Scripts / UI).
Báo lại Lead con số là sửa trúng ngay.

---

# 🔴 04/09 — TÌM RA GỐC RỄ "MAP CỨNG ĐƠ" (đã sửa)

## Thủ phạm: 2 AudioListener → Unity cảnh báo MỖI FRAME → FPS 10

Console của Sếp: **999+ dòng cùng một cảnh báo, cùng một giây** —
*"There are 2 audio listeners in the scene."* Unity in cảnh báo này **mỗi frame**; mỗi dòng log
kèm stack trace tốn vài mili-giây ⇒ FPS rơi xuống ~10 ⇒ kéo map chỉ nhận ~10 mẫu chuột/giây ⇒
**cảm giác cứng đơ, lâu lâu nhích một chút**.

### Cơ chế (đọc từ code, không đoán)
1. `AudioManager.AutoInit()` chạy ở `RuntimeInitializeLoadType.**BeforeSceneLoad**` — TRƯỚC khi scene load.
2. `Awake()` → `DontDestroyOnLoad` → gọi `EnsureAudioListener()`.
3. Lúc đó scene CHƯA có gì: `FindFirstObjectByType<AudioListener>()` = null **và** `Camera.main` = null
   ⇒ rơi vào nhánh cuối `gameObject.AddComponent<AudioListener>()` — **AudioManager tự gắn listener cho chính nó**.
4. Scene load xong → `Main Camera.prefab` mang sẵn AudioListener của nó (dòng 180) ⇒ **THÀNH 2 CÁI**.
5. `HandleSceneLoaded` có gọi lại `EnsureAudioListener()` nhưng hàm cũ **chỉ THÊM khi thiếu, không bao giờ TẮT cái thừa** ⇒ lỗi tồn tại vĩnh viễn.

Khớp mọi triệu chứng Sếp mô tả: *"ban đầu tôi đâu chỉnh gì phần này"* (đúng — do gói âm thanh thêm hôm 03/09),
*"đột nhiên build một hồi nó ra khoản này"*, và vùng bến tàu nặng nhất vì chỗ đó camera zoom xa,
tilemap chồng dày nên vốn đã tốn — cộng thêm log mỗi frame là đứng hẳn.

### Đã sửa — `Assets/_Game/Audio/AudioManager.cs`
- `EnsureAudioListener()` nay quét **mọi** AudioListener đang bật, giữ đúng **1** (ưu tiên cái trên `Camera.main`), **TẮT** những cái thừa (tắt chứ không xoá ⇒ revert được, KHÔNG đụng prefab Main Camera).
- Gọi thêm ở `Start()` — chạy sau khi scene đã lên nên chắc chắn dọn được.
- QA: tree-sitter **0 lỗi** · ngoặc 71/71 · EOL giữ nguyên LF · diff chỉ cộng thêm (xoá 7 / thêm 56).
- Backup: `production/backup_round9_2026-09-04/AudioManager.cs`

### Sếp test
1. Bấm **Clear** trong Console → Play lại.
2. Cảnh báo "2 audio listeners" phải **biến mất hoàn toàn**, thay bằng 1 dòng `[Audio] Da tat 1 AudioListener thua...`
3. Kéo map ở vùng bến tàu — phải mượt lại.

> 💡 Mẹo Console: gõ vào ô tìm kiếm góc trên phải để lọc, ví dụ `[UiProbe]` hoặc `BlockMapPan` —
> trước đó Sếp bấm F9 không thấy gì là vì log bị chôn dưới 999+ dòng cảnh báo audio.

### Nếu sau khi sửa vẫn còn cứng (ít khả năng)
Lúc đó mới tới lượt 2 việc còn treo — không làm trước:
- `Tools ▸ Farm Game ▸ Hiệu năng ▸ ★ Tilemap: Individual → Chunk` (~27.000 ô đang sort riêng lẻ)
- Tách `Canvas_Popup` (697 CanvasRenderer chung 1 canvas)

---

# ✅ 04/09 — CHỐT HẠ "MAP CỨNG ĐƠ Ở VÙNG BẾN TÀU"

Log Sếp gửi đã chỉ đích danh. **Có 2 lỗi RIÊNG BIỆT chồng lên nhau**, sửa cả hai:

## LỖI 1 — Kéo map bị chặn ở bến tàu (đây mới là cái Sếp hỏi từ đầu)

**Bằng chứng đo tại chỗ:**
```
[UiProbe] ⛔ KÉO MAP BỊ CHẶN — 1. BoatSystem/Dock_02/LockUI
          layer=Default · nút bấm thật=KHÔNG ⇒ nghi lớp phủ mồ côi
F9: BlockMapPan=False · IsAnyPopupOpen=False · UI đè #1: BoatSystem/Dock_02/LockUI
```
⇒ KHÔNG phải khoá input, KHÔNG phải popup, KHÔNG phải FPS.

**Chuỗi nhân quả:**
1. `Main Camera.prefab` dòng 101 có component **`Physics2DRaycaster`**.
2. Nó khiến `EventSystem.IsPointerOverGameObject()` trả **TRUE khi con trỏ nằm trên BẤT KỲ `Collider2D` nào trong thế giới** — không riêng UI.
3. `BoatSystem/Dock_0X/LockUI` có **`BoxCollider2D` 180×90** phủ kín vùng bến (dùng để bắt tap mở khoá).
4. `CameraController.cs:241` / `:333`: `if (IsPointerOverGameObject()) return;` ⇒ **không bắt đầu kéo được**.

Vì thế map mượt ở mọi nơi khác, chết đúng vùng bến tàu — và "tự nhiên hỏng" vì hệ bến tàu mới thêm cuối tháng 8, trước đó vùng đó không có collider.

**ĐÃ SỬA — `CameraController.cs`:** thêm `ConTroDangTrenUI()` — chỉ chặn kéo map khi hit đến từ **`GraphicRaycaster`** (UI thật trên Canvas). Hit từ `Physics2DRaycaster` (va chạm world) **bỏ qua** — chúng vốn có đường xử lý riêng (`OnMouseDown` của LockUI, `ObjectDragHandler`, `PlacementManager`).
- Công tắc Inspector **`Chi Chan Boi Ui That`** — bỏ tick là về hành vi cũ.
- Nút bấm UI thật (ví dụ `Kitchen_UI_v2/Btn_BackFarm` trong log) **vẫn chặn kéo map như cũ** — đúng.
- Sửa luôn `World/Buildings/CookingGate` (cũng chặn nhầm, có trong log).

## LỖI 2 — AudioListener (bản vá hôm qua chưa trọn)

Log mới cho thấy 2 trạng thái sai xen kẽ:
- Vào Bếp → `There are no audio listeners` ×50 — vì `FarmUIManager:594` tắt listener camera farm, mà bản vá hôm qua đã tắt listener dự phòng của AudioManager ⇒ **còn 0 cái**.
- Về Farm → `There are 2 audio listeners` trở lại.

**ĐÃ SỬA — `AudioManager.cs`:** `EnsureAudioListener()` nay làm việc trên listener **đang BẬT**:
- 0 cái đang bật ⇒ bật/ tạo đúng 1 (ưu tiên `Camera.main`)
- >1 cái đang bật ⇒ tắt bớt, giữ đúng 1
- Kiểm lại **mỗi 0.5 giây** trong `Update()` — vì chuyển cảnh Farm ↔ Bếp bật/tắt listener ngoài tầm kiểm soát của AudioManager, chỉ kiểm lúc `sceneLoaded` là **không đủ**.

## QA
| | AudioManager.cs | CameraController.cs |
|---|---|---|
| tree-sitter | 0 lỗi | 0 lỗi |
| ngoặc | 71/71 | 82/82 |
| EOL | LF (giữ nguyên) | CRLF (giữ nguyên) |
Backup: `production/backup_round10_2026-09-04/`

## Sếp test
1. Clear Console → Play.
2. Kéo map ở **vùng bến tàu** — phải mượt.
3. Bấm vào **ổ khoá bến** — popup mua bến vẫn phải mở được (chứng tỏ không phá `OnMouseDown`).
4. Bấm nút UI (Cửa hàng / Kho) rồi kéo — nút vẫn phải chặn kéo map như cũ.
5. Vào Bếp rồi ra Farm vài lần — Console **không còn** cảnh báo audio nào.
