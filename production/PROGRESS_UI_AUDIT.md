# AUDIT PROGRESS UI — Ruộng vs Chuồng vs Máy vs Công trường

*Ngày: 2026-08-20 · Nguồn: đọc trực tiếp code + prefab YAML trong `/home/claude/work/gamedata/Assets/_Game/Farm/` · Đơn vị quy đổi: **1 ô lưới (CELL) = 100 world-unit (wu)** — theo `PlacementManager.cs` dòng 16: "CELL = 100 world unit, ORIGIN = (0,0). Đây là NGUỒN SỰ THẬT DUY NHẤT".*

---

## 1. Giải phẫu mẫu chuẩn — Plot_01 (ô ruộng)

### 1.1. Điều bất ngờ số 1: prefab Plot_01 KHÔNG chứa thanh progress nào

Trong `CÔNG TRÌNH/Plot_01.prefab`, toàn bộ tham chiếu UI tiến trình của `PlotController` là **fileID 0** (dòng 157–163 của prefab):

```yaml
readyIcon:    {fileID: 0}
timerRoot:    {fileID: 0}
timerText:    {fileID: 0}
progressFill: {fileID: 0}
progressRoot: {fileID: 0}
progressFullWidth: 1
progressLeftToRight: 1
```

Cây con của prefab chỉ có: `GroundSprite`, `CropGroup` (12 CropPoint), `HarvestSpawnPoint`, `ExpSpawnPoint`, `Crop VFX Root`, và 3 prefab VFX lồng (`PF_SeedRain_World`, `PF_SeedCostText_World`, `PF_HarvestAmountText_World`). **Không có node nào tên `ProgressRoot`/`Fill`/`TimerRoot`.**

→ Thanh progress "đẹp" mà người chơi thấy trên ruộng **nằm ở SCENE (`SCN_Farm`), là node được thêm tay vào từng instance** (scene không có trong bộ export nên không đọc được kích thước/sprite thật của nó). Bằng chứng code: `PlotController.ForceRebindChildren()` tự đi tìm con theo TÊN mỗi lần Awake/OnValidate:

```csharp
// PlotController.cs dòng 339–343
t = transform.Find("ProgressRoot");
if (t != null) progressRoot = t.gameObject;
t = transform.Find("ProgressRoot/Fill");
if (t != null) progressFill = t;
```

### 1.2. Cách PlotController điều khiển bar (cơ chế "scale-X")

`RefreshVisual()` (PlotController.cs dòng 811–853):

- Chỉ hiện khi **Growing**, tự tắt khi Ready/Empty: `progressRoot.SetActive(state == PlotState.Growing)` (dòng 824–825). Đây là bar **always-on** — không cần bấm vào ruộng.
- Fill chạy bằng **localScale.x + dịch tâm** (dòng 827–841):

```csharp
float p = Mathf.Clamp01(progress);
Vector3 scale = progressFill.localScale;
scale.x = p;
progressFill.localScale = scale;
if (progressLeftToRight) {
    Vector3 pos = progressFill.localPosition;
    pos.x = -(progressFullWidth * (1f - p)) * 0.5f;   // progressFullWidth = 1 (serialized)
    progressFill.localPosition = pos;
}
```

- Nguồn tiến độ: `GetGrowProgress01()` (dòng 618–632) — tính từ mốc Unix `startUnixTime/finishUnixTime`, Ready = 1.
- Ngoài ra còn `ShowProgressBar(bool)` (dòng 856–860) cho hệ khác bật/tắt.

### 1.3. Popup "Painel" của ruộng (bấm vào ruộng đang lớn)

`CropProcessPopupUI.cs` — script gắn lên GameObject **Painel trong scene** ("con trực tiếp của ô đất — World Space Canvas", comment dòng 8–10; prefab của nó cũng không có trong export). Cơ chế:

- Tự bind theo tên `Track_Bar/Progress_Fill`, `Track_Bar/Txt_TimeRemaining`, `Btn_SpeedUp`… (dòng 198–241).
- **Ép fill về Image.Type.Filled / Horizontal / Left** (dòng 243–248) và chạy bằng `fillAmount` (dòng 279): `progressFill.fillAmount = currentPlot.GetGrowProgress01();`
- Tự bám vị trí `plot.position + (0, 0.7, 0)` mỗi frame (dòng 114), hỗ trợ cả 3 render mode canvas.

### 1.4. Con số thật của Plot_01 (khung tham chiếu)

| Thông số | Giá trị serialized | Quy đổi world-unit |
|---|---|---|
| Root `Plot_01` localScale | **0.5** | — |
| `GroundSprite` localScale | 100 | sprite 7.24×3.45 → **362 × 172.5 wu** |
| Sprite nền đất | guid `c1d278193612f0d4a95620fb6c62090d` = `Assets/maptitle/tile_dirt.png` | |
| Footprint (`BuildingFootprintKit.soO`) | 4×2 | **400 × 200 wu** (khớp BoxCollider 800×400 × scale 0.5) |
| SortingLayer/Order | GroundSprite: layerID 1669604809, order 500; cây: layer "CongTrinh", order 560 | |
| `progressFullWidth` | 1 (đơn vị local của plot) | ≈ 0.5 wu nếu Fill là con trực tiếp — con số thật phụ thuộc scale của node scene |

> **Trung thực:** kích thước/sprite của khung + fill bar ruộng KHÔNG đọc được từ export (nó là node scene). Điều chắc chắn từ code: bar ruộng always-on, ẩn hiện theo state, fill scale-X trái→phải, còn popup của ruộng dùng `Image.fillAmount`.

---

## 2. Bảng so sánh 8 đối tượng

Trước tiên, giải phẫu **PF_PenMiniPanel.prefab** (dùng chung cho cả 4 chuồng + 3 máy):

```
PF_PenMiniPanel  (RectTransform 400×150, localScale 0.01, Canvas WORLD-SPACE, sortingOrder 600, layerID 1669604809)
 └─ PanelContent (panelRoot — anchors STRETCH (0,0)-(1,1), sizeDelta +270×+136, mặc định tắt)
     ├─ Background   (Image sliced, sprite proc_frame_bg — STRETCH (0,0)-(1,1), sizeDelta 0)
     ├─ Slot_Food1   (96×110, x=-60, sprite proc_track_bg) · Slot_Food2 (96×110, x=+60) · Slot_Basket (100×110)
     └─ ProgressOverlay (khung bar — Image sliced proc_frame_bg, anchors STRETCH (0,0)-(1,1), sizeDelta +360×+84, mặc định tắt)
         ├─ ProgressFill (Image FILLED/Horizontal/Left, fillAmount=0, sprite proc_fill_green, màu (0.25,0.82,0.35,0.9),
         │                anchors (0.05,0.15)-(0.95,0.42) + sizeDelta +222×+30 → CŨNG stretch)
         ├─ TimerText   (TMP fontSize 20 CỐ ĐỊNH, anchors (0.1,0.48)-(0.9,0.92) + sizeDelta +210×+30)
         └─ Txt_PenName (TMP fontSize 22, anchor CENTER cố định 230×28 tại (-48,+52) — KHÔNG stretch)
```

Sprite tra từ `Temp/claude_export/sprite_map_all.tsv`:
- khung/nền: `bb4dc0e2693052041a593d75feee86f5` = `Assets/Assetsgame/popup/ui_building_svg/generated_sprites/proc_frame_bg.png`
- fill xanh: `7a3272bd67021a44eb3f847add03aea1` = `.../proc_fill_green.png`
- ô slot: `0f1c2058e02572e49b8081fa161ef681` = `.../proc_track_bg.png`

**Chuỗi cộng dồn kích thước (mấu chốt của "khung to khung nhỏ"):** vì `PanelContent` và `ProgressOverlay` đều neo stretch với sizeDelta DƯƠNG, kích thước khung = `root + (270,136) + (360,84)`. Hệ số quy đổi: panel scale 0.01 × pen-root scale 100 = **1 px UI = 1 wu**.

| Đối tượng | Có progress world-space? | Cấu trúc node | Kích thước THẬT (wu, đã quy đổi) | Sprite khung | Code điều khiển | **LỆCH GÌ so với Plot** |
|---|---|---|---|---|---|---|
| **Plot_01** (ruộng 4×2 = 400×200 wu, root scale 0.5) | ✅ always-on khi Growing (node ở SCENE, không có trong prefab) + popup Painel khi bấm | `ProgressRoot/Fill` (Transform thường) — bind theo tên | *Không đọc được từ export — node scene* | *không đọc được* (popup dùng quy ước tên `Track_Bar/Progress_Fill`, cùng "họ" sprite proc_*) | `PlotController.RefreshVisual` 827–841 (scale-X); popup `CropProcessPopupUI` 279 (fillAmount) | **CHUẨN so sánh** |
| **Pen_01 — bò** (7×5 = 700×500 wu, root scale **100**) | ⚠️ CHỈ khi bấm mở panel; đang nuôi mà không mở panel thì **không có bar nào** | nested `PF_PenMiniPanel`, root override **400×150** @ anchoredPos (−3.41, +0.28) → tâm panel lệch **(−341, +28) wu** | Khung ProgressOverlay = 400+270+360 = **1030 × 370 wu** (10.3×3.7 ô!); Fill = 0.9×1030+222 = **1149 × 130 wu** — fill TRÀN khỏi khung ~60 wu mỗi bên | `proc_frame_bg` (sliced) | `PenMiniPanelUI` — `fillAmount` trong coroutine dòng 325 và `RefreshUI` dòng 405–406 | Khác cơ chế (fillAmount vs scale-X), khác điều kiện hiện (bấm mới thấy vs always-on), bar rộng hơn cả chuồng (1030 > 700), lệch trái 341 wu |
| **Pen_02 — heo** | như Pen_01 | như trên nhưng root bị kéo thành **771.07 × 486.71** (override `m_SizeDelta` trong Pen_02.prefab) @ (−3.52, +0.04) | Khung = 771+270+360 = **1401 × 707 wu** (14×7 ô — TO HƠN CẢ CHUỒNG 700×500); Fill = **1483 × 221 wu** | `proc_frame_bg` | như trên | **Thủ phạm chính "khung to"**: khung ×1.36 ngang, ×1.91 dọc so với 3 chuồng kia; chữ TimerText vẫn fontSize 20 → khung to chữ bé |
| **Pen_03 — gà** | như Pen_01 | root 400×150 @ (−3.06, +0.02) → (−306, +2) wu | Khung **1030 × 370 wu**, Fill 1149×130 | `proc_frame_bg` | như trên | như Pen_01, nhưng offset panel khác Pen_01 tới 35 wu ngang / 26 wu dọc — bar mỗi chuồng đậu một chỗ |
| **Pen_04 — bò sữa** | như Pen_01 | root 400×150 @ (−2.91, +0.10) → (−291, +10) wu | Khung **1030 × 370 wu**, Fill 1149×130 | `proc_frame_bg` | như trên | như Pen_01, offset lại khác tiếp |
| **May_01 — xay bột** (prefab là **bản sao gần nguyên xi của Pen_04**: vẫn BarnSprite `chuongmoigiasuc.png`, vẫn `HappyHarvestAnimalVisualSpawner`, vẫn PenClickDetector/PenDropTarget; chỉ đổi config `Config_May01_XayBot` rice/60s, ẩn Slot_Food2) | ⚠️ chỉ khi bấm | nested `PF_PenMiniPanel`, root 400×150 @ **(0, −2)** → tâm panel (0, **−200) wu** — nằm DƯỚI tâm nhà | Khung **1030 × 370 wu**, Fill 1149×130 | `proc_frame_bg` | `PenMiniPanelUI` (nguyên hệ chuồng) | Bar máy đậu THẤP hơn bar chuồng ~230–540 wu theo trục Y; cùng mọi lệch khác của chuồng |
| **May_02 — ép mía** | như May_01 (config sugarcane/90s) | root 400×150 @ (0, −2) | như May_01 | `proc_frame_bg` | như trên | như May_01 |
| **May_03 — phô mai** | như May_01 (config milk/120s) | root 400×150 @ (0, −2) | như May_01 | `proc_frame_bg` | như trên | như May_01 |
| **PF_PenMiniPanel** (asset gốc) | — | xem giải phẫu trên | thiết kế "gốc" 360×84 khung + 222×30 fill, nhưng anchors stretch làm con số này **không bao giờ là kích thước thật** | `proc_frame_bg`/`proc_fill_green` | `PenMiniPanelUI` | Lỗi cấu trúc: stretch-anchor + sizeDelta dương cộng dồn 2 tầng |

### Đối tượng thứ 9 (ngoài đề nhưng liên quan trực tiếp): CÔNG TRƯỜNG (ConstructionSite)

- **Không có fill bar nào** dù `ConstructionSite.Progress01()` (dòng 117–118) đã tính sẵn tiến độ — UI chỉ hiện **đồng hồ chữ** `52Sek`/`1M59Sek`.
- UI dựng **100% bằng code** lúc runtime (`ConstructionSiteUI.Build/Construct`, dòng 55–203): canvas world-space `max(470, bề ngang nhà) × 300 wu`, mép dưới đặt ở `worldH/2 + 26` wu trên nóc (hằng `GapAboveRoof = 26`); tấm tên ≥320×76 @y=226; **thanh thời gian 252×70 wu** @y=140 (icon đồng hồ 48×48, chữ fontSize 40); nút rush 196×80 @y=46. Canvas scale 1 → 1px = 1wu (comment dòng 13–15 xác nhận quy ước).
- Sprite: **vẽ thủ tục lúc runtime** (`ConstructionSpriteFactory.Panel/GreenButton/ClockIcon/CoinIcon`) hoặc ô art `ConstructionArtKit` — bộ sprite THỨ BA, không chung với proc_* của chuồng, không chung với bar ruộng.
- Sorting: layer trên cùng + order **30000** (ConstructionSite.cs dòng 89–90) vs panel chuồng order 600 vs sprite ruộng 500–560.
- `FeedMillController.cs` (máy thức ăn gia súc): chỉ quay bánh răng + bật VFX — **không có progress UI nào**, ghi nhận "thiếu".

---

## 3. Chẩn đoán gốc rễ — vì sao "khung to khung nhỏ"

1. **Không tồn tại một prefab/component progress-bar dùng chung.** Ba hệ, ba đời kiến trúc: ruộng = node scene vẽ tay + `PlotController` scale-X; chuồng/máy = `PF_PenMiniPanel` + `Image.fillAmount`; công trường = canvas dựng bằng code, không có fill. Mỗi hệ một bộ sprite (bar ruộng: không rõ; chuồng: `proc_frame_bg`/`proc_fill_green`; công trường: texture thủ tục), một quy ước sorting (500–560 / 600 / 30000), một điều kiện hiển thị (always-on / bấm mới hiện / always-on-nhưng-chỉ-chữ).

2. **PF_PenMiniPanel bị "stretch-anchor cộng dồn".** `PanelContent` (+270,+136) và `ProgressOverlay` (+360,+84) đều neo (0,0)-(1,1) với sizeDelta dương → kích thước khung thật = `sizeDelta root + 630 × +220`. Con số 360×84 trong prefab trông như "kích thước bar" nhưng thực tế chỉ là phần cộng thêm. Hệ quả: **chỉnh root là khung phình theo cấp số cộng ở mọi tầng**, và `ProgressFill` (0.9×W+222) luôn TRÀN khỏi khung ~12% bề ngang.

3. **Pen_02 bị kéo root 400×150 → 771×487 ngay trong prefab** (override `m_SizeDelta` của rect `1435085710780094715`). Nghi vấn động cơ: `PenMiniPanelUI.IsPointerOverPanel` (dòng 465–471) chỉ hit-test bằng **rect root** — root 400×150 NHỎ hơn khung nhìn thấy 1030×370, nên bấm vào mép khung là panel tự đóng; kéo root to là cách "chữa" triệu chứng đó, và tạo ra chuồng heo khung khổng lồ 1401×707 wu.

4. **Offset đặt tay mỗi prefab một giá trị:** bò (−341,+28), heo (−352,+4), gà (−306,+2), bò sữa (−291,+10), cả 3 máy (0,−200). Không có quy tắc "đỉnh sprite + khoảng thở" như công trường (`GapAboveRoof=26`).

5. **Ba quy ước đơn vị/scale chồng nhau:** Plot root scale **0.5**, Pen/May root scale **100** (panel con 0.01 để bù), UI công trường scale **1**. Ai copy giá trị kích thước giữa hai hệ mà không nhân/chia đúng hệ số là lệch ngay 2–200 lần.

6. **Chữ không đi cùng khung:** TimerText/Txt_PenName fontSize cố định 20/22, Txt_PenName lại neo CENTER cố định trong khi khung stretch → heo: khung ×1.9 nhưng chữ giữ nguyên; tên chuồng đứng lệch góc.

7. **Máy chế biến là bản copy prefab chuồng** (vẫn barn sprite, spawner con vật, 2 slot thức ăn — chỉ ẩn slot 2) → mọi bệnh của chuồng lây nguyên sang máy, cộng thêm offset (0,−200) tự chế.

---

## 4. Đề xuất chuẩn hoá (chưa code) — `PF_WorldProgressBar`

### 4.1. Spec 1 component + 1 prefab dùng chung

**Prefab `PF_WorldProgressBar`** (world-space, 1 px = 1 wu — cùng quy ước ConstructionSiteUI/Placement_Ghost):

```
PF_WorldProgressBar (RectTransform 360×84, localScale 0.01 khi cha scale 100 / 2.0 khi cha scale 0.5 — component tự bù, xem 4.2)
 · Canvas WorldSpace, sortingLayer = layer UI nổi hiện có, sortingOrder 600
 · KHÔNG GraphicRaycaster (bar chỉ hiển thị — không ăn click, hết bệnh hit-test)
 ├─ Frame (Image sliced proc_frame_bg, anchors CENTER cố định 360×84 — TUYỆT ĐỐI không stretch)
 ├─ Fill  (Image FILLED/Horizontal/Left, proc_fill_green, màu (0.25,0.82,0.35,0.9), anchors CENTER cố định 324×36, y=−16)
 └─ TimerText (TMP autosize min 20 max 40, rect 300×34, y=+18, outline đen 0.2)
```

**Component `WorldProgressBar.cs`** — API tối thiểu:
- `SetProgress01(float p)` → `fill.fillAmount = p` (MỘT cơ chế duy nhất toàn game; khớp sẵn popup ruộng + panel chuồng).
- `SetTimeText(string)` / `SetVisible(bool)`.
- `AttachAbove(Transform host, float worldTopY)` → đặt `localPosition.y = worldTopY + 26` (dùng lại hằng `GapAboveRoof = 26` của ConstructionSiteUI cho toàn game).
- Tự chuẩn hoá scale: `transform.localScale = 0.01 / lossyScaleCủaCha` để bar luôn ra đúng **360×84 wu** bất kể cha scale 0.5 (plot) hay 100 (pen/máy) — đây là chỗ giết chết vĩnh viễn lỗi "3 quy ước đơn vị".

**Con số chuẩn (lấy hệ ruộng làm mốc):**

| Thông số | Giá trị | Căn cứ |
|---|---|---|
| Khung | **360 × 84 wu** (3.6 × 0.84 ô) | đúng cặp số thiết kế gốc trong PF_PenMiniPanel trước khi bị stretch phá; ≈ 90% bề ngang ruộng 400 wu — tỉ lệ cân đối trên đối tượng nhỏ nhất |
| Fill | 324 × 36 wu (inset 10%/…) | 0.9 × khung, KHÔNG cộng sizeDelta |
| Sprite | `proc_frame_bg` + `proc_fill_green` (guid ở mục 2) | bộ sprite đã có sẵn, cùng "họ" với popup ruộng (`Track_Bar/Progress_Fill`) |
| Offset dọc | đỉnh sprite building + **26 wu** | đồng bộ hằng GapAboveRoof của công trường |
| Offset ngang | **0** (giữa building) | xoá sạch các offset tay −341/−291/(0,−200) |
| Điều kiện hiện | luôn hiện khi đang chạy tiến trình (Growing/Processing/Building), ẩn khi xong | theo hành vi ruộng (PlotController 824–825) |

**Quy tắc scale theo cỡ building:** KHÔNG scale bar theo footprint. Building 4×2 (ruộng) đến 7×5 (chuồng/máy) dùng chung 360×84 wu — bar là UI đọc thông tin, to theo nhà chỉ tổ mỗi nhà một cỡ (chính là bệnh hiện tại). Ngoại lệ duy nhất: đối tượng 1×1 (nếu sau này có, vd chậu hoa) dùng biến thể ×0.75 = 270×63 wu, quy định bằng 1 enum trên component, không bằng tay.

> Lưu ý trung thực: kích thước bar ruộng THẬT đang nằm trong `SCN_Farm` (không có trong export). Trước khi chốt cặp 360×84, mở scene đo node `ProgressRoot` của một Plot instance; nếu số thật khác thì thay cặp số chuẩn bằng số đo được — cấu trúc spec không đổi.

### 4.2. Bảng việc per-prefab (🤖 = code/sửa YAML làm được · 🧑 = phải vào Unity Editor)

| # | Việc | Ai | Ghi chú |
|---|---|---|---|
| 1 | Tạo `WorldProgressBar.cs` + `PF_WorldProgressBar.prefab` | 🤖 viết script; 🧑 tạo prefab (kéo sprite, font TMP) — hoặc 🤖 sinh YAML prefab rồi 🧑 mở Editor xác nhận | prefab mới, guid mới |
| 2 | `PF_PenMiniPanel`: sửa anchors `ProgressOverlay` → CENTER cố định 360×84; `ProgressFill` → CENTER 324×36; hoặc thay hẳn overlay bằng PF_WorldProgressBar | 🤖 sửa YAML được (đổi m_AnchorMin/Max/m_SizeDelta); 🧑 mở Editor kiểm tra 7 prefab nhận đúng | 1 sửa ăn cho cả 4 chuồng + 3 máy |
| 3 | `Pen_02`: xoá override `m_SizeDelta 771×487` → trả về 400×150 | 🤖 sửa YAML (xoá 2 dòng override) | PHẢI làm cùng #5, nếu không tái phát lý do người ta kéo to |
| 4 | Chuẩn hoá offset panel: pens về (0, +y_theo_nóc), máy bỏ (0,−200) | 🤖 sửa YAML từng pen/máy; 🧑 nghiệm thu vị trí trong scene | 8 giá trị anchoredPosition |
| 5 | `PenMiniPanelUI.IsPointerOverPanel`: hit-test theo rect PanelContent/Overlay thật thay vì rect root | 🤖 | sửa dòng 465–471 |
| 6 | `PenMiniPanelUI`: thêm bar always-on khi Processing mà panel đóng (spawn PF_WorldProgressBar) | 🤖 | hiện tại đóng panel là mất dấu tiến trình |
| 7 | `PlotController`: refactor nhánh progressFill scale-X (827–841) sang gọi `WorldProgressBar.SetProgress01` (giữ fallback Find("ProgressRoot") cho save/scene cũ) | 🤖 | |
| 8 | Plot instances trong `SCN_Farm`: thay node ProgressRoot tay bằng PF_WorldProgressBar | 🧑 (scene không sửa mù bằng text được vì chưa có trong export) | đo số thật trước, xem 4.1 |
| 9 | `ConstructionSiteUI`: thêm Fill bar (đã có sẵn `Progress01()`), dùng đúng sprite proc_* thay texture thủ tục | 🤖 (UI này dựng 100% code) | đổi timer plate 252×70 → khung 360×84 chuẩn |
| 10 | Máy chế biến: tách prefab khỏi "xác" chuồng (bỏ AnimalVisualSpawner/barn sprite nếu art máy đã có) | 🧑 quyết định art + kéo; 🤖 dọn component | ngoài phạm vi bar nhưng nên làm cùng đợt |

### 4.3. Ước lượng rủi ro

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Sửa YAML prefab tay làm hỏng fileID/nested-prefab | **Trung bình** | chỉ đụng block `m_Modifications` (thêm/xoá override — cấu trúc phẳng); diff trước sau; mở Editor reimport kiểm tra |
| Scene `SCN_Farm` có override riêng đè lên prefab (không nhìn thấy trong export) | **Cao** | bước 🧑 bắt buộc: audit instance trong scene trước khi merge; con số Pen_02 771×487 chính là tiền lệ override "chui" |
| Pen_02 thu nhỏ lại → vùng click panel hụt → panel tự đóng khi bấm | **Cao nếu bỏ qua #5** | gộp #3 + #5 chung một PR |
| Đổi cơ chế fill của Plot (scale-X → fillAmount) làm lệch pixel bar cũ | Thấp | giữ node cũ 1 bản build, so ảnh chụp trước/sau |
| Save/PlayerPrefs | **Không ảnh hưởng** | toàn bộ thay đổi là view-layer; state nằm ở PlayerPrefs (PLOT_*, PenState_*) không đụng |
| TMP font/material instance (outline) khác nhau giữa 3 hệ | Thấp | PF_WorldProgressBar dùng 1 font asset (8f586378b4e1... đang dùng trong panel chuồng) |

---

## 5. Danh sách "THIẾU progress UI" (không đoán — có dẫn chứng)

| Đối tượng | Tình trạng | Dẫn chứng |
|---|---|---|
| Chuồng/máy khi panel ĐÓNG | **Thiếu hẳn** bar luôn-hiện; đang nuôi mà đóng panel thì không còn chỉ báo nào cho tới bubble Ready | `PenMiniPanelUI`: `progressOverlay` là con của `panelRoot` (PanelContent), `ClosePanel()` tắt panelRoot (dòng 166–171); bubble chỉ hiện khi Ready (`UpdateReadyBubble`, 639–644) |
| Công trường | Có đồng hồ chữ, **thiếu fill bar** dù đã tính `Progress01()` | `ConstructionSite.cs` 117–118 (hàm không ai gọi để vẽ); `ConstructionSiteUI` chỉ có `_timeText` |
| Máy thức ăn gia súc (FeedMill) | **Không có progress UI nào** | `FeedMillController.cs` chỉ quay bánh răng + VFX (toàn file, 89 dòng) |
| Plot_01.prefab | ProgressRoot/TimerRoot/ReadyIcon **không có trong prefab** — gắn ở scene, bind runtime | prefab dòng 157–163 (fileID 0); `PlotController.ForceRebindChildren` dòng 321–343 |
| Popup Painel của ruộng | Prefab không có trong export — script scene-object | `CropProcessPopupUI.cs` dòng 8–10 |
