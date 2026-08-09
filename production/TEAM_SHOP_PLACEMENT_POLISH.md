# ĐỘI LÀM VIỆC — CĂN HÀNG, THANH XÁC NHẬN & ANIMATION (chuẩn Township)

> Kênh giao tiếp chung. Đọc mục người kia trước khi code.
> Nguồn tham chiếu: `PHAN_TICH_TOWNSHIP_ANIMATION.md` (bóc 77 frame video Township).

---

## 0. HIỆN TRẠNG — DỮ LIỆU ĐÃ ĐÚNG, NHƯNG VẪN LỆCH

### Dữ liệu Edric đã điền xong (kiểm chứng trực tiếp trong `.asset`)
```
Home1  4×4   buildTime 30      Chuồng Bò Sữa  7×5   0
Home3  4×5   buildTime 30      Chuồng Bò      7×5   0
Home5  4×4   buildTime 30      Chuồng Gà      7×5   0
Home2  1×1   ← THIẾU PREFAB    Chuồng Heo     7×5   0
Home4  1×1   ← THIẾU PREFAB    Máy ×3         7×5   240
Khung Hoa 8×3                  Đất            4×2   30
Chậu Hoa 1-4  1×1
```

### 🔴 NGUYÊN NHÂN "MÉO MÉO KHÔNG ĐỀU" — ĐÃ TÍNH RA

**KHÔNG phải do gridSize sai.** Là do hệ thống snap **TÂM vùng ô**, nhưng mắt người lại so **CHÂN công trình**.

Độ lệch pivot của từng nhà khác nhau: `Home1 = (0,192)` · `Home3 = (0,208)` · `Home5 = (0,194)`.

Công thức hiện tại: `anchor = footprintCenter − pivotOffset`

```
Home1 (4×4):  center = ox·100          →  anchor = ox·100 − 192
Home3 (4×5):  center = ox·100 + 250    →  anchor = ox·100 + 42
Home5 (4×4):  center = ox·100          →  anchor = ox·100 − 194
```

Chân nhà (= anchor, vì pivot ở đáy) rơi vào `−192`, `+42`, `−194` so với mốc lưới.
→ **Home1 và Home3 đứng cạnh nhau lệch nhau ~34 unit (≈18 pixel màn hình).**
→ Home1 và Home5 lệch **2 unit** (gần như trùng, nên 2 cái này trông đều).

Đúng khớp ảnh Edric gửi: mấy nhà mái nâu thẳng hàng với nhau, nhưng nhà mái xanh thì nhô ra.

### ✅ CÁCH SỬA (đã kiểm chứng bằng phép tính)

**Neo công trình vào MÉP DƯỚI của vùng ô, không phải tâm vùng ô.**

```
anchor.y = rect.yMin · CELL        (mép dưới vùng ô)
anchor.x = tâm ngang vùng ô
```

Kiểm chứng:
```
Home1 (4×4):  rect.yMin = oy   →  anchor.y = oy·100   ✔ bội số 100
Home3 (4×5):  rect.yMin = oy   →  anchor.y = oy·100   ✔ bội số 100
Home5 (4×4):  rect.yMin = oy   →  anchor.y = oy·100   ✔ bội số 100
```
**Mọi công trình có chân trên cùng một lưới → thẳng hàng tuyệt đối.**

Sprite vươn lên cao hơn vùng ô là **bình thường** — mái nhà nhô ra ngoài footprint, Township cũng vậy.

### 🔴 KHÁC BIỆT THANH XÁC NHẬN so với Township

| | Township | Game Edric hiện tại |
|---|---|---|
| Khung chứa | **1 thanh nền tối bo góc** | 3 ô vuông rời, không nền |
| Giá tiền | **`KAUFEN FÜR 🪙 30` ở hàng trên** | KHÔNG CÓ |
| Thứ tự nút | **✕ đỏ · ↻ xanh dương · ✓ xanh lá** | ✓ xanh · ✕ đỏ · ↻ cam |
| Hình nút | tròn | vuông |
| Khi di chuyển vật có sẵn | chữ đổi thành **`KOSTENLOS PLATZIEREN`** (Đặt miễn phí) | không phân biệt |

### 🔴 DẤU GÓC — hiện chỉ có 2, Township có 4

Ảnh Edric: **2 mũi tên đỏ** ở hai bên trái/phải.
Township: **4 chevron xanh** ôm 4 góc vùng ô. Đỏ chỉ khi không đặt được.

---

## 1. PHÂN CÔNG

| Vai | Skill / agent | Sở hữu file |
|---|---|---|
| **DEV-1** — Lưới & Đặt | `map-systems`, `gameplay-programmer` | `PlacementManager.cs`, `PlaceableItemData.cs`, `ObjectDragHandler.cs`, `.asset` trong `CÔNG TRÌNH/` |
| **DEV-2** — UI & Animation | `team-ui`, `ux-design`, `unity-ui-specialist` | `PlacementGhostVisualController.cs`, prefab `Placement_Ghost`, file MỚI cho animation/icon nổi |
| **TESTER** | `qa-tester` | không sửa code |

---

## 2. VIỆC CỦA DEV-1

### V1 — 🔴 NEO VÀO MÉP DƯỚI VÙNG Ô (quan trọng nhất)
Đổi công thức đặt: `anchor = rect bottom-center` thay vì `rect center − pivotOffset`.
- Sửa ở mọi chỗ: ghost đi theo chuột, `ConfirmPlacement`, `RegisterCompletedBuilding`, `LoadBuildings`, `StartEditBuilding`
- **Tương thích ngược — ✅ EDRIC ĐÃ CHỐT: VIẾT HÀM CHUYỂN ĐỔI SAVE.**
  Thêm `saveVersion` vào `BuildingsSave`. Save cũ (không có key `saveVersion`, coi như v0) thì
  lần đầu load phải dịch toạ độ về hệ mới, rồi ghi lại với `saveVersion = 1`.
  Phép dịch: toạ độ cũ là `anchor = center − pivotOffset`, mới là `anchor = rectBottomCenter`.
  → `anchorMới = anchorCũ + pivotOffset − (M·CELL/2)` với M = chiều sâu ô.
  **Phải test:** mở save cũ → công trình đứng đúng chỗ như trước khi sửa, không nhảy.
- Giữ nguyên: vùng ô vẫn khớp lưới, kiểm tra chồng lấn không đổi

### V2 — THỐNG NHẤT CHIỀU SÂU THEO NHÓM
Cùng một nhóm công trình nên cùng chiều sâu, để người chơi xếp thành hàng đẹp:
- **5 nhà dân → tất cả `4×4`** (hiện Home3 là 4×5, lệch nhóm)
- 4 chuồng đã đồng bộ `7×5` ✔
- 3 máy đã đồng bộ `7×5` ✔

**✅ EDRIC ĐÃ CHỐT: đồng bộ hết 5 nhà thành `4×4`.**
Sửa `Home3` từ `4×5` → `4×4` (sửa thẳng file `.asset`, có `Undo.RecordObject`).
`Home2`/`Home4` để `4×4` luôn cho nhất quán, dù chưa có prefab.
Mái nhô ra ngoài footprint là bình thường — Township cũng vậy.

### V3 — `Home2` / `Home4` THIẾU PREFAB
Hai asset này `prefabToBuild = null` → mua trong shop cũng không đặt được, và gridSize kẹt `1×1` gây lệch.
Chọn một trong hai:
- **(a)** Ẩn khỏi shop cho tới khi có prefab (an toàn nhất) — thêm cờ `chuaSanSang` hoặc để `unlockLevel = 999`
- **(b)** Trỏ tạm sang prefab của Home1

Ghi rõ đã chọn cái nào vào nhật ký. **Đề xuất (a)** — đừng để người chơi mua thứ không đặt được.

### V4 — CHỮ "ĐẶT MIỄN PHÍ" KHI DI CHUYỂN
Khi `currentlyEditingBuilding != null` (di chuyển vật đã đặt):
- Thanh xác nhận hiện **`ĐẶT MIỄN PHÍ`**, KHÔNG hiện số tiền
- Khi đặt mới: **`MUA VỚI GIÁ 🪙 <giá>`**

Cung cấp cho DEV-2 một property đọc được:
```csharp
public bool IsFreeMove => currentlyEditingBuilding != null;
public int  CurrentPriceGold { get; }
public int  CurrentPriceGem  { get; }
```

### V5 — TẮT CẢNH BÁO LẶP
Console đang spam `'Home3' còn gridSize = 1×1` dù asset đã là 4×5 — cảnh báo cũ còn đọng.
Kiểm `WarnGridSizeMissing`: chỉ cảnh báo khi `gridSize` **thật sự** là 1×1 VÀ prefab đo ra lớn hơn 1 ô. Home2/Home4 không có prefab thì đừng cảnh báo (không đo được, cảnh báo vô nghĩa).

---

## 3. VIỆC CỦA DEV-2

### V6 — 🔴 THANH XÁC NHẬN GIỐNG TOWNSHIP
Dựng lại bằng code (không cần sửa prefab tay):
```
┌──────────────────────────────┐
│   MUA VỚI GIÁ  🪙 30         │   ← hàng chữ, nền tối bo góc
│    (✕)      (↻)      (✓)     │   ← 3 nút TRÒN, đúng thứ tự này
└──────────────────────────────┘
```
- Một **nền tối bo góc** bọc cả cụm (dùng ô art `PriceBarBg` đã có)
- Thứ tự **✕ → ↻ → ✓** (huỷ · xoay · xác nhận). Township đặt xác nhận ở PHẢI — thuận tay phải, và tránh bấm nhầm huỷ
- Nút **tròn**, không vuông
- Màu: ✕ đỏ · ↻ **xanh dương** (hiện đang cam) · ✓ xanh lá
- ✓ **xám khi không đặt được** (cái này đã chạy đúng ✔)
- Đọc `PlacementManager.IsFreeMove` → đổi chữ

### V7 — 4 DẤU GÓC CHEVRON
Hiện chỉ 2 mũi tên hai bên. Township có **4 chevron ôm 4 góc vùng ô**.
- Xanh khi đặt được, **đỏ** khi chồng lấn
- Nhấp nháy nhẹ (scale 1.0 ↔ 1.08, chu kỳ ~1s)
- Đặt đúng 4 góc của `rect`, không phải 4 góc sprite

### V8 — BỘ ANIMATION TÁI SỬ DỤNG (theo §4 file phân tích)
File mới, dùng chung cho toàn game:

| Component | Thông số (đã đo từ video) |
|---|---|
| `FloatingIconBob` | `y ±6px`, chu kỳ `1.2s`, ease sin · `scale 1.0↔1.06` lệch pha |
| `RisingBalloon` | `y +250px / 2.5s` ease-out · `x` sin biên độ `15px` · alpha tắt ở 30% cuối · scale nhỏ dần |
| `FloatingNumber` | `y +90px / 1.2s` ease-out · **`scale 0 → 1.25 → 1.0` ease-out-back** ← cái overshoot này là toàn bộ vị "nảy" |
| `GiftBoxReveal` | `scale 0→1.15→1.0` (0.4s back) · giữ 1.2s · `scale→1.3 + alpha→0` (0.5s) |
| `GentleSway` | `rotation.z = sin(time + offsetRiêng) · 2°`, pivot gốc cây |

Dự án **không có DOTween** → viết bằng coroutine, giống các file hiện có.

### V9 — ICON NỔI TRÊN ĐẦU CÔNG TRÌNH
Đây là khác biệt lớn nhất giữa màn hình game Edric và Township. Township lúc nào cũng có **5–8 icon đang bob**.

| Icon | Khi nào hiện |
|---|---|
| 🟡 Mũ bảo hộ | đang xây (đã có ô art `HardHatDone`) |
| 🥛 Sản phẩm | sản phẩm xong, chạm để thu |
| ⭐ Ngôi sao | thưởng XP chờ |
| **Chữ "Z"** | máy **đứng không**, thiếu nguyên liệu |
| 🔴 Số đỏ | có việc cần làm |

- Dựng bằng code, có ô art để Edric thay sprite sau (mở rộng `ConstructionArtKit`)
- Dùng `FloatingIconBob` ở V8
- Sorting layer đặt trên công trình để không bị che

---

## 4. HỢP ĐỒNG API (chốt trước khi code)

DEV-1 cung cấp:
```csharp
public bool IsFreeMove       { get; }   // true = di chuyển vật có sẵn (miễn phí)
public int  CurrentPriceGold { get; }
public int  CurrentPriceGem  { get; }
public RectInt CurrentRect   { get; }   // vùng ô hiện tại, để DEV-2 đặt 4 chevron
public bool IsCurrentValid   { get; }   // để DEV-2 đổi màu xanh/đỏ
```

**Trạng thái chốt:** ✅ **ĐÃ CHỐT VÀ ĐÃ CÓ TRONG CODE** — DEV-1, `PlacementManager.cs`
(khối `HỢP ĐỒNG API §4`, ngay dưới `CurrentRotationSteps`). Tên/chữ ký khớp 100 % bảng trên.

Ghi chú dùng cho DEV-2:
- `CurrentPriceGold` / `CurrentPriceGem` **tự trả 0 khi `IsFreeMove`** → chỉ cần
  `if (IsFreeMove) → "ĐẶT MIỄN PHÍ"`, ngược lại hiện đồng nào > 0.
- `CurrentRect` có `width == 0` khi KHÔNG có Ghost nào → dùng làm điều kiện ẩn 4 chevron.
- 4 góc world của rect: `PlacementManager.CellCornerToWorld(rect.xMin, rect.yMin)`,
  `(rect.xMax, rect.yMin)`, `(rect.xMin, rect.yMax)`, `(rect.xMax, rect.yMax)`.
  Tâm rect: `PlacementManager.RectCenterWorld(rect)`. Neo (mép dưới): `RectAnchorWorld(rect)`.
- `CurrentRect` được cập nhật MỖI FRAME trong `Update()` và mỗi lần xoay (`SetupFootprint`).
- Có thể **xoá `ConstructionBridge.GetGhostItem()`** và thay bằng `CurrentPriceGold/Gem`.
  Phần `ReserveCells` của `ConstructionBridge` thì DEV-1 vẫn giữ nguyên tên
  `reservedRects` + `RebuildOccupiedCells` nên reflection của bạn KHÔNG vỡ.

---

## 5. NHẬT KÝ DEV-1

> Vòng V8 — hoàn tất V1…V6. Mọi file đều biên dịch được; KHÔNG sửa file nào của DEV-2.

### 5.1 — CÔNG THỨC NEO MỚI (V1)

```
N = số ô ngang · M = số ô dọc (đã tính xoay) · CELL = 100

SnapAnchor(world, size):                     ← thay cho SnapCenter trong TOÀN BỘ luồng đặt
    ox = Floor( world.x/CELL − N/2 + 0.5 )       // căn giữa NGANG
    oy = Floor( world.y/CELL       + 0.5 )       // hàng ô chứa CHÂN công trình
    anchor = ( (ox + N/2)·CELL , oy·CELL )        ← mép dưới + giữa ngang vùng ô

RectFromAnchor(anchor, size) = RectInt(ox, oy, N, M)      ← vùng ô mọc LÊN từ chân
RectAnchorWorld(rect)        = ( (xMin + N/2)·CELL , yMin·CELL )
AnchorToFootprintCenter      = anchor + (0, M·CELL/2)     ← KHÔNG còn pivotOffset
FootprintCenterToAnchor      = center − (0, M·CELL/2)
```

`SnapAnchor` **idempotent**: `SnapAnchor(SnapAnchor(p)) == SnapAnchor(p)` → kéo một công
trình ra rồi thả lại không dịch nửa ô, và DEV-2 snap lại lần hai cũng không lệch.

**Nguyên nhân thật của "méo méo không đều" — hoá ra là HAI lỗi cộng dồn:**

1. **Nửa ô do chiều sâu LẺ.** V7 snap TÂM ô, mốc tâm = `(oy + M/2)·CELL`.
   M chẵn → chân trùng đường kẻ; M lẻ → chân rơi vào GIỮA ô.
   → Home1/Home5 (4 ô) chân ở `700`, Home3 (5 ô) chân ở `750` → **lệch đúng 50 unit**.
   Đây mới là con số thật (§0 ước 34 unit — cùng triệu chứng, khác chi tiết).
2. **Phép bù pivot của V7 bị LỆCH ĐƠN VỊ 100 lần.**
   `TryMeasurePrefabVisualBounds` lấy `size` bằng `× lossyScale` (world) nhưng lấy `center`
   bằng `InverseTransformPoint` (local, tức **chia** cho scale root = 100).
   → `PivotOffsetOf(Home1)` trả **1.92** chứ không phải 192 → "bù pivot" gần như không làm gì.
   Hậu quả: Ghost tính rect ở một chỗ, `RefreshOccupancy` (đo bounds thật) ra chỗ khác
   → vừa chặn oan đất trống, vừa cho đặt đè lên nhau. **Đã sửa** (dùng world unit cả hai).

V8 xoá cả hai: chân = bội số nguyên của CELL, không phụ thuộc pivot, không phụ thuộc M
chẵn/lẻ. Mái nhô ra ngoài footprint là bình thường (Township cũng vậy).

### 5.2 — GIỮ HAY BỎ `AnchorToFootprintCenter` / `FootprintCenterToAnchor`?

**GIỮ — giữ nguyên TÊN và CHỮ KÝ, chỉ đổi RUỘT.** Lý do:
`ConstructionManager.SpawnSite()` (dòng ~600) và `ConstructionSite.Initialize()` (dòng 75)
đang gọi hai hàm này. Xoá là **vỡ biên dịch của DEV-2**. Đổi ruột thì DEV-2 tự động hưởng
hệ toạ độ mới mà không phải sửa một dòng nào, và cặp hàm vẫn khứ-hồi chính xác tuyệt đối.
`GetFootprintRect(center, size)` cũng **giữ nguyên** vì `ConstructionBridge.ReserveCells`
(dòng 90) truyền vào `ConstructionSite.CenterWorld` — đầu vào thật sự là TÂM.
Các private member mà `ConstructionBridge` reflection vào (`currentItem`, `reservedRects`,
`RebuildOccupiedCells`) đều **không đổi tên**.

`PivotOffsetOf` giữ public nhưng giờ **chỉ còn một người dùng: hàm chuyển đổi save v0→v1**.

### 5.3 — CHUYỂN ĐỔI SAVE (V3) + KIỂM CHỨNG BẰNG SỐ

`BuildingsSave` thêm `public int saveVersion;` — save cũ không có key này nên JsonUtility
để 0 ⇒ tự động nhận là **v0** (đúng thủ thuật đã dùng cho field `rot`).
`LoadBuildings()` dịch từng entry rồi `SaveBuildings()` đóng dấu `saveVersion = 1`
⇒ **chỉ dịch một lần**, không bao giờ dịch hai lần.

```
neoMới = SnapAnchor( neoCũ + pivotOffset − (0, M·CELL/2) , size )
```
Ý nghĩa thực tế: vì pivot ở đáy nên `pivotOffset.y ≈ M·CELL/2`, hai số triệt tiêu nhau
⇒ phép dịch = "giữ nguyên CHÂN nhà, kéo về đường kẻ lưới gần nhất".

**Kiểm chứng — lấy đúng đầu ra của V7 (`neo = SnapCenter = (oy + M/2)·CELL`), oy = 5:**

| Nhà | M cũ | pivot.y | neo cũ | + pivot − M·CELL/2 | snap | dịch |
|---|---|---|---|---|---|---|
| Home1 4×4 | 4 | 192 | `(5+2)·100 = 700` | `700+192−200 = 692` | **700** | **0** |
| Home5 4×4 | 4 | 194 | `(5+2)·100 = 700` | `700+194−200 = 694` | **700** | **0** |
| Home3 4×5 | 5 | 208 | `(5+2.5)·100 = 750` | `750+208−200 = 758` | **800** | **+50** |

- TRƯỚC: chân `700 / 700 / 750` → Home3 lệch **nửa ô** (đúng "nhà mái xanh nhô ra").
- SAU: chân `700 / 700 / 800` → **tất cả là bội số của 100 → thẳng hàng tuyệt đối**.
- 4 nhà 4×4 **không dịch một pixel**. Chỉ Home3 dịch 50 unit (≈26 px) vì `gridSize` của nó
  cũng đổi 4×5 → 4×4 — một lần duy nhất, và đó chính là cái giá để nó vào hàng.

### 🔴 5.4 — VIỆC CẦN DEV-2 LÀM (save công trường đang xây)

`FARM_CONSTRUCTION_SITES` (file của DEV-2) cũng lưu neo theo **hệ V7**. Sau vòng này,
công trường khôi phục từ save CŨ sẽ dựng giàn giáo cao hơn đúng `M·CELL/2` (≈2 ô), vì
`AnchorToFootprintCenter` giờ cộng `M·CELL/2` vào một con số vốn đã là tâm.
**DEV-2 chọn một trong hai:**
- (a) thêm `saveVersion` cho `SiteEntry` và dịch y hệt công thức §5.3, hoặc
- (b) đơn giản `PlayerPrefs.DeleteKey("FARM_CONSTRUCTION_SITES")` một lần khi thấy save cũ
  (công trường đang xây là dữ liệu ngắn hạn, mất không đau — nhưng phải hoàn tiền).

DEV-1 **không tự sửa** vì mọi file tên chứa `Construction` thuộc quyền DEV-2 (§9.1).

### 5.5 — `Home2` / `Home4`: TÌM RA NGUYÊN NHÂN THẬT, KHÔNG PHẢI "THIẾU PREFAB"

Prefab **CÓ ĐỦ CẢ 5 CÁI**. `.asset` lưu tham chiếu bằng cặp `(fileID, guid)` và
**hai guid bị HOÁN CHỖ cho nhau**, nên guid trỏ sang prefab khác trong khi fileID lại của
prefab đúng → Unity giải ra `null`, Inspector chỉ hiện "None" y như chưa ai gán:

| | fileID (đúng) | guid CŨ (sai) | guid MỚI (đã sửa) |
|---|---|---|---|
| Home2 | `8741544369598970430` = root `House_02` | `d191c47…` = *House_04* | `4b0123a…` = **House_02** ✔ |
| Home4 | `1582242620454337050` = root `House_04` | `4b0123a…` = *House_02* | `d191c47…` = **House_04** ✔ |

Bằng chứng khoá chặt: `itemIcon` của Home2 dùng đúng sprite của `House_02`
(`7108f0f1…`) và của Home4 dùng sprite `House_04` (`a37cc19f…`).

**Đã chọn (a) — ẩn khỏi shop bằng `unlockLevel = 999`**, theo quyết định Edric.
Vì sao cách này ít xâm lấn nhất:
- **Không phải sửa một dòng code nào.** `ShopLevelLockUI.Refresh()` tự phủ lớp khoá và
  `interactable = false` cả `btnBuy/btnPlus/btnMinus`; `ShopManager` còn `OrderBy(unlockLevel)`
  nên hai nhà này rơi xuống cuối danh sách.
- Thêm cờ mới (`chuaSanSang`) thì **vô dụng**: người đọc cờ phải là `ShopManager`/`ShopItemUI`,
  hai file KHÔNG thuộc quyền DEV-1 (§1) → không sửa được → cờ chết.
- Lọc `prefabToBuild == null` trong `ShopManager` cũng phải sửa file người khác.

> **Đảo lại chỉ mất 1 giây:** guid đã sửa nên prefab giờ giải được. Bấm
> `Tools ▸ Farm ▸ Đồng Bộ Nhóm 5 Nhà Dân` → nút *"Mở lại shop cho nhà ĐÃ có prefab"*,
> hoặc sửa tay `unlockLevel: 999 → 1`. Edric quyết.

### 5.6 — ⚠️ MỘT LỖI DỮ LIỆU CHƯA SỬA (cần Edric quyết)

`Home4` và `Home5` **trùng `itemID = 104`** (và trùng luôn `itemName` "Nhà Dân 4").
`FindItemById("104")` trả về cái nào đứng trước trong `ShopManager.buildingList` → save
có thể load Home5 thành Home4. DEV-1 **cố tình không tự sửa**: đổi `itemID` sẽ làm mọi
save đang có entry `"104"` mất công trình. Đề nghị: đổi `Home5.itemID → 105` kèm một bước
dịch save `"104" → "105"`, hoặc chấp nhận vì Home4 đang bị ẩn khỏi shop.

### 5.7 — FILE / VÙNG ĐÃ SỬA

**`Assets/_Game/Farm/Scripts/Managers/PlacementManager.cs`**
| Vùng | Việc |
|---|---|
| doc đầu file | viết lại quy ước toạ độ V8 + 2 nguyên nhân thật |
| `SnapAnchor` · `RectFromAnchor` · `RectAnchorWorld` · `SnapAnchorFor` | **MỚI** — lưới neo |
| `SnapCenter` · `SnapCenterFor` · `GetFootprintRect` | giữ nguyên ruột, dán cảnh báo "không dùng trong luồng đặt" |
| `GridSizeOf` | cảnh báo dùng chung HashSet với `WarnGridSizeMissing` |
| `PivotOffsetOf` | giữ, doc lại: chỉ còn phục vụ migration |
| `CurrentRectOf` · `HalfDepthWorld` | **MỚI**; xoá `CurrentPivotOffset()`, `CurrentFootprintCenter()` |
| `AnchorToFootprintCenter` / `FootprintCenterToAnchor` | đổi ruột sang `± M·CELL/2` |
| `TryMeasurePrefabVisualBounds` | **sửa lệch đơn vị 100 lần** của `center` |
| khối API §4 + `CurrentSaveVersion` | **MỚI** — 5 property cho DEV-2 |
| `BuildingsSave` | thêm `saveVersion` |
| `Update()` | `CurrentRectOf` + gán `currentRect` |
| `StartEditBuilding` · `RotateGhost` · `GetSnappedMousePos` | `SnapCenter` → `SnapAnchor` |
| `CacheCloneLocalCenter` | xoá `fallbackPivotOffset` (code chết) |
| `SetupFootprint` | thảm + khung lấy từ `currentRect` |
| `ConfirmPlacement` | `placedRect = RectFromAnchor(pos,size)` cho `reservedRects` |
| `RegisterCompletedBuilding` | snap lại neo (giữ z của DEV-2) trước khi ghi save |
| `SaveBuildings` / `LoadBuildings` / `MigrateAnchorV0ToV1` | **V3 — chuyển đổi save** |
| `RefreshOccupancy` · `ComputeRectFor` | object do ta đặt → `RectFromAnchor`; vật scene → vẫn đo bounds |
| `Cleanup` | reset `currentRect` / `isValidPos` |
| `WarnGridSizeMissing` | **V6** — 3 điều kiện, hết cảnh báo sai |

**`Assets/_Game/Farm/Scripts/Gameplay/ObjectDragHandler.cs`**
`_pivotOffset` → `_pivotOffsetX` + `_footOffsetY` (đo đáy hộp bao) · `SnapToGrid` snap CHÂN
qua `PlacementManager.SnapAnchor` · `IsValidPlacement`/`UpdatePlacementIndicator` dùng
`RectOf(pos)` + `RectCenterWorld`. Vật pivot ở giữa (decor, ô đất) vẫn đúng nhờ `_footOffsetY`.

**`.asset` trong `CÔNG TRÌNH/DataShop/Buiding/`** (sửa YAML trực tiếp)
| | gridSize | unlockLevel | prefab |
|---|---|---|---|
| Home1 | 4×4 (không đổi) | 1 | ✔ |
| Home2 | 1×1 → **4×4** | 1 → **999** | guid **đã sửa** → House_02 |
| Home3 | 4×5 → **4×4** | 1 | ✔ |
| Home4 | 1×1 → **4×4** | 1 → **999** | guid **đã sửa** → House_04 |
| Home5 | 4×4 (không đổi) | 1 | ✔ |

**`Assets/_Game/Farm/Editor/HouseGroupNormalizeTool.cs` — MỚI**
Menu `Tools ▸ Farm ▸ Đồng Bộ Nhóm 5 Nhà Dân (4×4)`.
Dùng `Undo.RecordObject` (gọi TRƯỚC khi đổi giá trị) + `EditorUtility.SetDirty` +
`AssetDatabase.SaveAssets()`. Idempotent.

> **Vì sao sửa YAML *và* viết tool:** sửa YAML để dữ liệu đúng NGAY, không phải chờ ai bấm
> menu. Viết tool vì (1) nếu Unity đang mở và đang giữ asset trong bộ nhớ thì lần Save
> Project kế tiếp có thể ghi đè YAML — bấm tool là khôi phục; (2) tool bắt được lỗi
> `prefabToBuild` giải ra null (lỗi guid ở §5.5 gần như không thể thấy bằng mắt trong
> Inspector); (3) `Undo.RecordObject` cho Edric Ctrl+Z được, sửa YAML thì không.
> Tool KHÔNG tự sửa guid — đó là việc phải soi từng cặp fileID/guid, DEV-1 đã sửa tay.

### 5.8 — ĐÃ KIỂM: KHÔNG PHÁ THỨ ĐANG CHẠY

| Thứ | Còn đúng? | Vì sao |
|---|---|---|
| Chồng lấn theo ô | ✔ | `occupiedCells`/`IsAreaFree` không đổi; rect giờ **thống nhất** một công thức cho cả Ghost và occupancy (trước đây hai đường ra hai rect khác nhau) |
| Xoay + lưu rotation | ✔ | `rotationSteps`, `RotationOf`, field `rot` không đổi; `SnapAnchor` chỉ đổi mốc trục Y nên xoay không làm nhà "tụt" |
| Timer xây / rush | ✔ | không đụng file `Construction*`; hợp đồng `TryStartConstruction(data, pos, rot, plotId)` giữ nguyên |
| Hoàn tiền | ✔ | `Cleanup(refund)` không đổi |
| Edit mode + Delete | ✔ | khớp entry theo `originalEditPosition` vẫn đúng vì object được spawn tại toạ độ save (đã dịch) |
| Biên bản đồ | ✔ | `IsRectInsideMap` nhận rect, không nhận toạ độ |

---

## 6. NHẬT KÝ DEV-2

> Vòng V6…V10 — hoàn tất cả 5 việc. KHÔNG sửa `PlacementManager.cs`, `PlaceableItemData.cs`,
> `ObjectDragHandler.cs`. Mọi thứ đọc qua HỢP ĐỒNG API §4 mà DEV-1 đã chốt.

### 6.1 — FILE MỚI / FILE ĐÃ SỬA

**MỚI — `Assets/_Game/Farm/Scripts/FX/`** (7 file)

| File | Nội dung |
|---|---|
| `FxEase.cs` | Easing dùng chung + `SetAlpha`/`CollectFaders` + `StablePhase01` |
| `FloatingIconBob.cs` | Nhịp nhấp nhô của icon nổi |
| `RisingBalloon.cs` | Bóng bay khánh thành |
| `FloatingNumber.cs` | Số thưởng "+10" bay lên |
| `GiftBoxReveal.cs` | Hộp quà nở ra rồi tan |
| `GentleSway.cs` | Lay nhẹ quanh GỐC (có bù pivot) |
| `BuildingStatusIcon.cs` | V9 — khung trắng + icon trạng thái nổi trên đầu công trình |

**ĐÃ SỬA**

| File | Việc |
|---|---|
| `Managers/PlacementGhostVisualController.cs` | V6 thanh xác nhận + V7 4 chevron; xoá `EnsurePriceBar`/`MakePriceLabel` cũ; thêm `Update()` |
| `Gameplay/ConstructionArtKit.cs` | +4 ô art, +4 màu nhận dạng, +4 case trong 3 hàm tra |
| `Gameplay/ConstructionSpriteFactory.cs` | +`CrossMark` `RotateArrow` `Star` `LetterZ` `MilkBottle` + helper `SdTriangle`/`Cross2` |
| `Gameplay/ConstructionManager.cs` | V10 — `SaveVersion` 1→2 + xoá key save cũ |
| `Gameplay/ConstructionSite.cs` | Gắn icon mũ bảo hộ suốt thời gian xây, tắt cùng giàn giáo |
| `Editor/ConstructionArtKitWindow.cs` | Nối dây 4 ô mới (`FieldOf`/`DescOf`), sửa ghi chú 19→23 ô |

**KHÔNG sửa prefab `Placement_Ghost`** — toàn bộ V6 dựng bằng code runtime. Prefab là YAML
dùng chung với DEV-1, sửa tay dễ đụng độ merge và Edric không phải mở prefab chỉnh gì.

### 6.2 — V6 THANH XÁC NHẬN: BỐN QUYẾT ĐỊNH KỸ THUẬT

1. **Nền tối làm CON của `Button_Row`, ở sibling index 0.**
   `AnimateGhostActionBar` scale Button_Row lúc bật (0.45 → 1.08 → 1). Là CON thì nền pop
   CÙNG hàng nút; là em ruột thì nút nảy mà nền đứng yên. Index 0 ⇒ UGUI vẽ trước ⇒ nằm dưới
   3 nút, khỏi đụng `sortingOrder` của DEV-1. Bắt buộc `LayoutElement.ignoreLayout = true`,
   nếu không HorizontalLayoutGroup coi nền là "nút thứ 4".
2. **Thứ tự nút bằng `SetSiblingIndex`** (nền 0, ✕ 1, ↻ 2, ✓ 3) — không sửa prefab.
3. **Glyph ✕ ↻ ✓ là SPRITE, không phải ký tự.** Prefab có 3 node `Label` chứa Unicode nhưng
   cả 3 đang `m_IsActive: 0`; bật lên là đánh cược font TMP mặc định có đủ 3 glyph.
4. **Hàng giá xếp THỦ CÔNG, bỏ `HorizontalLayoutGroup` + `ContentSizeFitter`.**
   Fitter ghi `sizeDelta` ở pha `SetLayoutHorizontal`, group cha đọc `sizeDelta` ở pha
   `CalculateLayoutInputHorizontal` — hai pha khác nhau ⇒ nền luôn chậm 1 frame so với chữ,
   đổi "ĐẶT MIỄN PHÍ" ↔ "MUA VỚI GIÁ 30" là thấy nền giật.

Ba thứ **giữ nguyên, không đụng**: `Button.interactable` (DEV-1 gán mỗi frame),
`Button.colors`, `targetGraphic`. ColorTint NHÂN vào `Image.color` nên ✓ vẫn tự xám. Bù thêm:
`RefreshConfirmBar` làm mờ glyph theo `interactable` — Unity chỉ tô lại targetGraphic, không
tô graphic con, để nguyên thì ✓ disable = đĩa mờ 50 % + tick trắng chói, đọc ra "UI lỗi".

### 6.3 — V7 CHEVRON: LẤY GÓC BẰNG HÀM CỦA DEV-1

4 chevron nằm trong node mới `Rect_Chevrons` (con của Ghost, `localScale = 1/lossyScale` để
BÊN TRONG nó 1 đơn vị = 1 world unit). Góc lấy bằng `PlacementManager.CellCornerToWorld` —
**không tự nhân CELL**, vì DEV-1 vừa đổi hệ neo sang mép dưới vùng ô (§5.1) và tự tính lại là
mời lỗi "lệch nửa ô" quay về. 4 nêm cũ (`_corners`, suy từ bounds SPRITE) đã bị TẮT
(`useRectChevrons = true`).

Sprite chevron có **pivot đúng tại góc**, hai cánh vươn theo +X/+Y ⇒ chỉ cần xoay `i·90°` là
ôm cả 4 góc, không phải 4 công thức offset song song. Có lấy mẫu bội 3×3 khử răng cưa.
`CurrentRect.width == 0` ⇒ ẩn hết (đúng quy ước §4).

### 6.4 — THÔNG SỐ ANIMATION THỰC DÙNG

| Component | Thông số đã cài |
|---|---|
| `FloatingIconBob` | `y ±6px` · chu kỳ `1.2s` sin · `scale 1.0↔1.06` **lệch pha 0.25 vòng** · lệch pha riêng từng icon |
| `RisingBalloon` | `y +250px / 2.5s` ease-out-cubic · `x` sin `±15px` (1.5 nhịp) · alpha tắt từ mốc `0.70` · `scale → 0.72` theo ĐỘ CAO |
| `FloatingNumber` | `y +90px / 1.2s` ease-out-cubic · `scale 0 → 1.25 → 1.0` ease-out-back · alpha tắt từ mốc `0.60` |
| `GiftBoxReveal` | `scale 0→1.15→1.0` (0.4s back) · giữ `1.2s` · `scale→1.3` + `alpha→0` (0.5s) |
| `GentleSway` | `rotation.z = sin(t/3s + phaRiêng)·2°`, có **bù pivot** để gốc cây đứng yên |
| 4 chevron (V7) | `scale 1.0 ↔ 1.08`, chu kỳ `1s` |

**🔴 OVERSHOOT 1.25 LÀ SỐ CHÍNH XÁC, KHÔNG PHẢI 1.70158 QUEN TAY.**
Ease-out-back `f(t) = 1 + c3(t−1)³ + c1(t−1)²` có độ vượt `o(c1) = 4c1³/(27(c1+1)²)`.
`c1 = 1.70158` chỉ cho đỉnh **~1.10** — dùng nó rồi bảo "đã có overshoot" là mất hơn nửa cú
nảy. Giải `o(c1) = 0.25` được nghiệm **chính xác `c1 = 3`** (kiểm tay: `f(0)=1−4+3=0`,
`f(0.5)=1−0.5+0.75=1.25`, `f(1)=1`). `FxEase.BackConstantFor()` đảo hàm bằng Newton cho các
đỉnh khác (1.15 → `c1 ≈ 2.164`).

**⚠ MỘT SUY LUẬN KHÁC TÀI LIỆU, CỐ Ý GHI RA:** §4.3 xếp `scale` cùng khối 1.2s với `y` nhưng
không ghi thời gian riêng cho cú pop. Để pop kéo đủ 1.2s thì đỉnh 1.25 rơi vào GIỮA quãng bay
⇒ đọc ra "số phình lên khi bay", không phải "số nảy ra rồi bay". Đã tách
`scaleDuration = 0.45s`; muốn y hệt tài liệu thì đặt nó = 1.2 trong Inspector.

**Quy đổi đơn vị:** thông số đo bằng "px" của video 826×576, còn world này 1 ô = 100 unit.
Mỗi component có ô `pixelToUnit` (Canvas UI để `1`, sprite world để `~2.5`) — giữ nguyên con
số đo được để người sau còn đối chiếu tài liệu, thay vì thấy một số 15 không rõ từ đâu.

**Đã grep `CoinFlyFX` trước khi viết:** nó dùng `AnimationCurve` của Inspector + `k*(2-k)`
(ease-out-quad), KHÔNG có ease-out-back, và bám chặt vào HUD Canvas + `OnGoldAddedFx`. Không
tái dùng được cho số bay trên đầu công trình, nhưng **cũng không sửa nó** — đang chạy đúng.
`FxEase` gom easing về một chỗ cho code MỚI; `CoinFlyFX` / `EnvironmentSway` /
`PlacementManager.BackOut` để nguyên.

### 6.5 — Ô ART MỚI (4 ô, tổng 19 → 23)

| Ô | Field | Màu nhận dạng | Hiện khi |
|---|---|---|---|
| `IconFrameBg` | `iconFrameBg` | TRẮNG XANH NHẠT | khung bọc mọi icon trạng thái |
| `IconStar` | `iconStar` | VÀNG CAM SAO | thưởng XP chờ |
| `IconZzz` | `iconZzz` | XÁM XANH BUỒN NGỦ | máy đứng không |
| `IconProductReady` | `iconProductReady` | XANH SỮA | sản phẩm xong |

- Enum `Slot` thêm ở **CUỐI** — chèn giữa là mọi ô đã gán trong `.asset` bị trượt sang ô khác.
- **Ngoại lệ có chủ ý:** `C_IconFrame` gần như TRẮNG, phá quy ước "mỗi ô một màu lạ". Khung
  icon Township *là* màu trắng; tô tím/cam thì cả cụm đọc sai ngay cả khi vị trí đã đúng. Bù
  lại pha một chút xanh rất nhạt để trong Inspector còn phân biệt được với `C_Clock`.
- Chưa có sprite → vẽ thủ tục: `Star()` (gập nan quạt 1/5 + SDF cạnh), `LetterZ()` (3 đoạn),
  `MilkBottle()` (thân + cổ + nắp xám), `CrossMark()`, `RotateArrow()` (cung HỞ 18°…96° + mũi
  nhọn — hở mới đọc ra "quay", khép lại là chữ O).

### 6.6 — V9 ICON NỔI: GẮN Ở ĐÂU

`BuildingStatusIcon` là component **độc lập, tái dùng được**: `AttachTo(go, status, height, kit)`
hoặc gắn tay trong Inspector rồi chọn `Status` (đổi lúc Play cũng cập nhật ngay).
4 trạng thái: `Building` (mũ bảo hộ) · `ProductReady` (sản phẩm) · `RewardWaiting` (sao) ·
`Idle` (chữ **Z**). `productSprite` riêng của công trình THẮNG ô art chung — mỗi xưởng ra một
loại hàng, dùng chung một bình sữa là mất hết thông tin.

Sorting: `ConstructionManager.TopSortingLayerName`, order **31000** — trên công trình và trên
UI công trường (30000), dưới nhãn debug ô art (32000).

**Đã tự gắn vào `ConstructionSite`** (mũ bảo hộ suốt thời gian xây, Township giữ icon này cả
lượt xây chứ không chớp một cái). Độ cao `worldH*0.5 + 416`: canvas UI công trường nằm ở
`worldH*0.5 + 26` cao 300 với pivot mép dưới ⇒ chiếm tới `+326`; thấp hơn 416 là icon đè lên
nền tên công trình. `HideConstructionVisuals()` tắt icon cùng giàn giáo — để nó bob tiếp lúc
hộp quà đang mở thì người chơi thấy "vẫn đang xây" trong khi đã xây xong.

**CHƯA gắn vào công trình đã xây xong** — 3 trạng thái kia cần hệ sản xuất / thưởng XP quyết
định *khi nào* bật, đó là việc của gameplay, không phải của UI. Component đã sẵn sàng, chỉ cần
một dòng `AttachTo`.

### 6.7 — V10 SAVE CÔNG TRƯỜNG: XOÁ KEY MỘT LẦN

Theo §5.4 phương án (b), **Edric đã chốt**. `ConstructionManager.SaveVersion` **1 → 2**;
`LoadSites()` thấy `saveVersion < 2` thì `PlayerPrefs.DeleteKey("FARM_CONSTRUCTION_SITES")`
+ `Save()` + `Debug.LogWarning` giải thích rõ (số công trường bị bỏ, lý do đổi hệ neo, và
nhấn mạnh đây là hành vi CÓ CHỦ Ý xảy ra ĐÚNG MỘT LẦN mỗi máy).

Hai chi tiết dễ sai đã xử lý:
- `SitesSave.saveVersion` **đổi mặc định từ `SaveVersion` về `0`**. `JsonUtility.FromJson`
  không bảo đảm chạy field initializer, nên để mặc định = 2 thì save đời rất cũ (chưa có key
  `saveVersion`) có nguy cơ bị đọc thành "đã mới nhất" và không được dọn. `SaveSites()` luôn
  gán tường minh khi ghi nên đổi mặc định không ảnh hưởng gì.
- **KHÔNG hoàn tiền.** `SiteEntry` không lưu giá đã trả; muốn hoàn phải tra ngược
  `PlaceableItemData` mà `Home4`/`Home5` đang **trùng `itemID = 104`** (§5.6) ⇒ có khả năng
  hoàn sai món. Thà mất vài công trường test còn hơn tự cấp tiền sai cho người chơi.
- Công trình **ĐÃ XÂY XONG** (`FARM_PLACED_BUILDINGS`) không bị ảnh hưởng — DEV-1 có
  `MigrateAnchorV0ToV1` riêng cho key đó.

### 6.8 — TESTER CẦN KIỂM (ngoài §8)

| # | Kiểm | Đạt khi |
|---|---|---|
| 1 | Mua 1 nhà → xem thanh xác nhận | 1 nền tối bo góc bọc CẢ cụm · chữ `MUA VỚI GIÁ` + icon xu + đúng số ở HÀNG TRÊN · 3 nút TRÒN thứ tự **✕ đỏ · ↻ xanh dương · ✓ xanh lá** |
| 2 | Bật Edit Mode, nhấc 1 công trình đã đặt | chữ đổi thành **`ĐẶT MIỄN PHÍ`**, KHÔNG có icon tiền và KHÔNG có số |
| 3 | Kéo ghost lên chỗ chồng lấn | 4 chevron đổi ĐỎ · nút ✓ xám **và dấu tick cũng mờ theo** (không phải tick trắng chói) |
| 4 | Đếm số chevron | đúng **4**, ôm 4 góc **vùng ô** (thảm xanh), KHÔNG ôm 4 góc sprite. Mái nhà nhô ra ngoài chevron là ĐÚNG |
| 5 | Đứng yên nhìn chevron | nhấp nháy rất nhẹ (~1s/nhịp), không giật, không trôi |
| 6 | Bấm ↻ vài lần rồi nhìn chevron | chevron nhảy theo vùng ô mới, vẫn ôm đúng 4 góc |
| 7 | Đặt công trình cần thời gian xây | icon **mũ bảo hộ trong khung trắng** nổi TRÊN nền tên công trình, nhấp nhô đều, KHÔNG đè lên đồng hồ / nút rush |
| 8 | Chờ xây xong | icon mũ bảo hộ TẮT đúng lúc giàn giáo tắt, không bob tiếp trong lúc hộp quà mở |
| 9 | Mở game lần đầu sau bản này, có công trường đang xây từ bản cũ | Console có 1 dòng `🔴 XOÁ SAVE CÔNG TRƯỜNG CŨ` ghi rõ số lượng; **công trình đã xây xong KHÔNG mất** |
| 10 | Tắt game rồi mở lại lần THỨ HAI | KHÔNG còn dòng cảnh báo đó nữa (chỉ chạy một lần) |
| 11 | Mua vật giá 0 (nếu có) | hiện `ĐẶT MIỄN PHÍ`, không hiện icon tiền trống |
| 12 | Bấm ✕ / ✓ / ↻ | cả 3 vẫn ăn click; nền tối KHÔNG chặn tia chuột |
| 13 | Console suốt lượt đặt | không có exception, không spam warning |

**Chưa test được vì không có Unity trên máy DEV-2** — cả 13 mục trên đều cần chạy thật.
Ưu tiên xem mục **3, 4, 7, 9** trước: đó là 4 chỗ có nhiều giả định nhất về thứ tự khởi tạo
và về cách Unity tô màu nút disable.

---

## 7. BÁO CÁO TESTER
### Vòng 1 — ⬜ chờ dev

---

## 8. TIÊU CHÍ ĐẠT

- [ ] Đặt 3 nhà khác loại cạnh nhau → **chân thẳng hàng tuyệt đối**, không lệch pixel nào
- [ ] Thanh xác nhận: nền tối bo góc + giá ở trên + 3 nút tròn thứ tự ✕ ↻ ✓
- [ ] Di chuyển vật có sẵn → hiện **"ĐẶT MIỄN PHÍ"**, không hiện tiền
- [ ] Đặt mới → hiện **"MUA VỚI GIÁ 🪙 X"** đúng số
- [ ] **4** chevron ôm 4 góc vùng ô, xanh↔đỏ theo trạng thái
- [ ] Nút ✓ xám khi chồng lấn (đã đạt ✔)
- [ ] Icon nổi bob mượt trên đầu công trình đang xây
- [ ] `Home2`/`Home4` không mua được trong shop (hoặc đã có prefab)
- [ ] Console không còn spam cảnh báo gridSize
- [ ] Save cũ mở lên không mất công trình

---

## 9. QUY TẮC

1. Chỉ sửa file thuộc phạm vi mình (§1). Cần file người khác → ghi yêu cầu vào §4.
2. **KHÔNG phá** những thứ đang chạy đúng: kiểm tra chồng lấn theo ô, xoay, timer xây, rush, save/load, hoàn tiền.
3. Animation dựng bằng **coroutine** (không có DOTween trong dự án).
4. UI dựng bằng **code**, có ô art để thay sprite sau.
5. Code phải biên dịch được. Unity 6000.3.10f1.
6. Ghi chú tiếng Việt giải thích VÌ SAO.
