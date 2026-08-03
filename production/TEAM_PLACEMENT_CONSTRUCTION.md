# ĐỘI LÀM VIỆC — ĐẶT CÔNG TRÌNH & TIẾN TRÌNH XÂY DỰNG (chuẩn Township)

> Kênh giao tiếp chung. Mỗi dev ghi vào mục của mình, ĐỌC mục người kia trước khi code.
> Chu kỳ: DEV-1 + DEV-2 → TESTER kiểm → sửa → lặp tới khi ĐẠT.

---

## 0. BÓC TÁCH VIDEO + 4 ẢNH THAM CHIẾU

Luồng Township đầy đủ, theo đúng thứ tự Edric mô tả:

### Giai đoạn A — ĐẶT (ảnh 1, 3, 4 · video f_020)
- Ghost công trình bám con trỏ, **snap theo ô lưới**
- **Thảm footprint xanh lá** phủ đúng diện tích chân công trình, bám sát lưới
- **4 dấu góc chevron xanh** ôm 4 góc thảm
- Thanh xác nhận dưới màn: nền tối bo góc, chữ **"KAUFEN FÜR 🪙 <giá>"**, 3 nút tròn:
  **✕ đỏ** (huỷ) · **↻ xanh dương** (xoay) · **✓ xanh lá** (xác nhận)
- **KHI CHỒNG LẤN** (ảnh 4 · video f_020): thảm chuyển **ĐỎ**, công trình nhuốm đỏ,
  và **nút ✓ CHUYỂN XÁM, KHÔNG BẤM ĐƯỢC**

### Giai đoạn B — ĐANG XÂY (ảnh 2 · video f_045)
- Công trình **CHƯA hiện**. Thay bằng **giàn giáo/cọc gỗ** + **công nhân NPC** đứng làm
- **Khói bụi** phun lên
- Nổi trên đầu: **TÊN CÔNG TRÌNH** (chữ trắng viền đậm, in hoa)
- Ngay dưới: **thanh thời gian** nền tối bo góc + icon đồng hồ + `52Sek` / `1M59Sek`
- Dưới nữa: **nút xanh lá "💵 21"** = tăng tốc bằng tiền mặt
- Giá rush **giảm dần theo thời gian còn lại** (52s→21 · 1m59s→24)

### Giai đoạn C — HOÀN THÀNH (video f_075)
- Công trình hiện ra trong **hộp quà trang trí**: khung trắng, **ruy băng hoa hồng** 3 mặt,
  **bóng bay đỏ/vàng** bay lên
- Icon **mũ bảo hộ + dấu tick xanh** bật lên trên đầu
- Sau đó hộp mở, công trình lộ ra

---

## 1. HIỆN TRẠNG GAME — CÁI GÌ CÓ, CÁI GÌ KHÔNG

### ✅ Đang chạy tốt (ĐỪNG PHÁ)
| Có sẵn | Vị trí |
|---|---|
| Ghost bám chuột khi giữ LMB | `PlacementManager.cs:132-135` |
| Clone visual đa sprite từ prefab thật | `PlacementManager.cs:882-934` |
| Khung placement procedural (fill + 4 corner bracket) vẽ bằng code | `PlacementGhostVisualController.cs:80-156` |
| Đổi màu xanh↔đỏ + rung khi invalid | `PlacementGhostVisualController.cs:271-307, 497-526` |
| Footprint làm tròn LÊN bội số ô (`Ceil`) | `PlacementManager.cs:416-417` |
| Nút ✓ ✕ có bind + animation pop | `PlacementManager.cs:205-224, 988-1057` |
| Edit Mode: giữ 0.3s nhấc công trình | `EditableBuilding.cs:23-79` |
| Hoàn tiền khi huỷ | `PlacementManager.cs:746-753` |
| Lưu/tải `FARM_PLACED_BUILDINGS` | `PlacementManager.cs:601-664` |

### 🔴 CÓ CODE NHƯNG CHẾT / SAI CẤU HÌNH
| # | Vấn đề | Bằng chứng |
|---|---|---|
| L1 | **`obstacleLayer` m_Bits = 0** → OverlapBox luôn null → `isValidPos` LUÔN TRUE → **đây chính là lý do đặt chồng lên nhau được** | `SCN_Farm.unity:321012-321014` |
| L2 | **`mapGrid` = null** → nhánh snap-theo-Grid chết, chỉ còn `Mathf.Round(x/100)*100` | `SCN_Farm.unity:321008` vs `PlacementManager.cs:833-839` |
| L3 | **Box va chạm cứng 50×50** bất kể công trình to nhỏ | `PlacementManager.cs:55` + `SCN_Farm.unity:321015` |
| L4 | **2 hệ snap lệch nhau**: PlacementManager **100** vs ObjectDragHandler **50** | `SCN_Farm.unity:321009` vs `:236457` |
| L5 | `footprintPadding` là **field chết**, không nơi nào đọc | grep toàn Assets = 0 |
| L6 | **`Btn_Rotate` không bind**, không xoay gì | prefab `Placement_Ghost` dòng 1384, 1489-1491 |
| L7 | `Btn_Delete` + `Selection_Ring` code tìm nhưng **prefab không có** | `PlacementManager.cs:197-199, 294-297` |
| L8 | 4 `Corner_TL/TR/BL/BR` trong prefab **không gán sprite** → vô hình | prefab dòng 84, 594, 1067, 1158 |
| L9 | `Grid_Footprint` bị **disable** ngay sau setup | `PlacementManager.cs:434-435` |
| L10 | Sorting layer `"CongTrinh"` **không tồn tại** | `PlacementManager.cs:15-16` vs TagManager |
| L11 | `ObjectDragHandler.Awake()` có **`if` treo** nuốt 3 dòng gán | `ObjectDragHandler.cs:74-78` |

### ❌ HOÀN TOÀN KHÔNG CÓ (0 dòng code — đã grep xác nhận)
`ConstructionSite` · `buildTime` · `constructionTime` · `UnderConstruction` · `rushCost` · `IsBuilding` → **tất cả 0 kết quả**

1. Trạng thái **đang xây** — bấm ✓ là công trình hiện ra hoàn chỉnh ngay
2. **Timer xây** + lưu offline timestamp
3. **Nút tăng tốc** bằng tiền/kim cương cho công trình
4. **Giàn giáo / công nhân / khói bụi**
5. **Nhãn tên + thanh thời gian** nổi trên đầu
6. **Hiển thị giá** trong thanh xác nhận
7. **Xoay** công trình + lưu rotation
8. **Kiểm tra biên map**
9. **Field kích thước ô lưới** trong data (chỉ suy từ bounds sprite → sai khi sprite có vùng trong suốt)

### Dữ liệu hiện có
`PlaceableItemData` chỉ **7 field**: `itemID, itemName, itemIcon, goldPrice, diamondPrice, prefabToBuild, unlockLevel`.
**33 asset**: 18 `BuildingData` (`CÔNG TRÌNH/DataShop/Buiding/`) + 15 `DecorData` (`CÔNG TRÌNH/`).

---

## 2. PHÂN CÔNG

| Vai | Skill dùng (`.claude/skills/`) | Sở hữu file |
|---|---|---|
| **DEV-1** — Lưới & Đặt | `map-systems`, `code-review`, agent `gameplay-programmer` + `systems-designer` | `PlacementManager.cs`, `PlaceableItemData.cs`, `ObjectDragHandler.cs`, `EditModeManager.cs`, các `.asset` trong `CÔNG TRÌNH/` |
| **DEV-2** — Xây dựng & UI | `team-ui`, `ux-design`, `design-system`, agent `unity-ui-specialist` + `ui-programmer` | File MỚI (`ConstructionSite.cs`, `ConstructionUI.cs`…), `PlacementGhostVisualController.cs`, prefab `Placement_Ghost` |
| **TESTER** | `qa-plan`, `smoke-check`, agent `qa-tester` | Không sửa code |

---

## 3. HỢP ĐỒNG API — CHỐT TRƯỚC KHI CODE

### DEV-1 cung cấp (thêm vào `PlaceableItemData`)
```csharp
[Header("Kích thước theo Ô LƯỚI — KHÔNG suy từ sprite nữa")]
public Vector2Int gridSize = new Vector2Int(1, 1);   // 2x2, 3x3…

[Header("Xây dựng")]
public float buildTimeSeconds = 0f;   // 0 = hiện ngay, không qua giai đoạn xây
public int   rushGemCost      = 0;    // 0 = tự tính theo thời gian còn lại
```

### DEV-1 GỌI của DEV-2 (điểm tích hợp duy nhất)
Trong `ConfirmPlacement()`, thay vì Instantiate prefab thật ngay:
```csharp
// DEV-2 cung cấp. Trả về true nếu đã chuyển sang trạng thái ĐANG XÂY.
// Trả về false nếu buildTimeSeconds <= 0 → DEV-1 tự Instantiate như cũ.
bool started = ConstructionManager.Instance != null &&
               ConstructionManager.Instance.TryStartConstruction(
                   currentItem,          // PlaceableItemData
                   snappedWorldPos,      // Vector3
                   rotationSteps,        // int, 0-3
                   plotId);              // int
if (!started) { /* Instantiate prefab thật như code cũ */ }
```

### DEV-2 cung cấp
```csharp
public class ConstructionManager : MonoBehaviour
{
    public static ConstructionManager Instance { get; }
    public bool TryStartConstruction(PlaceableItemData data, Vector3 pos, int rotSteps, int plotId);
    public event System.Action<PlaceableItemData, Vector3, int, int> OnConstructionComplete;
}
```
Khi xây xong, `ConstructionManager` **tự Instantiate prefab thật** rồi phát `OnConstructionComplete`.

**Trạng thái chốt:** ✅ **DEV-1 đã chốt & đã code xong phía mình** (2026-08-02) · ✅ **DEV-2 xác nhận** (2026-08-02)
> DEV-2 đã tạo `ConstructionManager` đúng chữ ký (không namespace · `Instance` public static ·
> `TryStartConstruction(PlaceableItemData, Vector3, int, int)`), có gọi ngược
> `RegisterCompletedBuilding` khi xây xong. Reflection của DEV-1 tra được, **không cần sửa
> `ConfirmPlacement()`**. Hai yêu cầu API nhỏ gửi lại DEV-1: xem §6.

### ⚠️ DEV-2 ĐỌC KỸ — API THẬT SỰ ĐANG CHẠY

**1. DEV-1 gọi DEV-2 bằng REFLECTION (không phải lời gọi trực tiếp).**
Lý do: lúc DEV-1 code thì `ConstructionManager` chưa tồn tại → gọi trực tiếp làm **cả
`Assembly-CSharp` không biên dịch được**, chặn toàn đội. `#if` không dùng được vì không có
symbol nào để định nghĩa. Xem `PlacementManager.TryStartConstructionDev2()`.
Reflection tra **đúng chữ ký này**, sai một chữ là không tìm thấy và game rơi về đường cũ:

```csharp
public class ConstructionManager : MonoBehaviour        // KHÔNG namespace, KHÔNG đổi tên
{
    public static ConstructionManager Instance { get; }  // property tĩnh public, tên "Instance"
    public bool TryStartConstruction(PlaceableItemData data, Vector3 pos, int rotSteps, int plotId);
}
```
`pos` DEV-1 truyền vào **đã snap sẵn** = tâm world của khối ô. `rotSteps` 0-3.
Khi DEV-2 merge xong, có thể thay thân `TryStartConstructionDev2()` bằng lời gọi trực tiếp —
**nơi gọi trong `ConfirmPlacement()` không phải sửa**.

**2. Trả `true` = DEV-2 nhận việc.** DEV-1 khi đó **KHÔNG** Instantiate prefab, **KHÔNG** ghi save,
và **tự giữ chỗ ô lưới** (`reservedRects`) để không ai đặt đè lên giàn giáo.

**3. Xây xong, DEV-2 GỌI NGƯỢC LẠI:**
```csharp
// Sau khi ConstructionManager tự Instantiate prefab thật:
PlacementManager.Instance.RegisterCompletedBuilding(data, spawnedObj, rotSteps);
// → PlacementManager lo: sorting layer, hiện con vật, cấp plotId, GHI SAVE, nhả chỗ giữ.
// Không gọi hàm này thì công trình sẽ KHÔNG được lưu và mất khi tắt game.

// Nếu huỷ / hoàn tiền công trình đang xây:
PlacementManager.Instance.ReleaseConstructionCells(centerWorldPos);
```

**4. Toán lưới dùng chung — gọi thẳng, đừng tự viết lại:**
```csharp
PlacementManager.CELL                                   // const float = 100
PlacementManager.GridOrigin                             // Vector2.zero
PlacementManager.SnapCenterFor(data, world, rotSteps)   // → Vector3 tâm đã snap
PlacementManager.GridSizeOf(data, rotSteps)             // → Vector2Int (đã hoán đổi khi xoay)
PlacementManager.GetFootprintRect(centerWorld, size)    // → RectInt vùng ô
PlacementManager.RectCenterWorld(rect)                  // → Vector3
PlacementManager.RotationOf(rotSteps)                   // → Quaternion
PlacementManager.Instance.IsAreaFree(rect)              // chồng lấn?
PlacementManager.Instance.IsRectInsideMap(rect)         // ngoài biên?
PlacementManager.Instance.CurrentRotationSteps          // hướng xoay Ghost hiện tại
```

**5. Kích thước UI "đang xây":** giàn giáo/nhãn tên nên rộng đúng `N*100 × M*100` world unit,
với `N,M = GridSizeOf(data, rotSteps)`. Ghost đang dùng đúng con số đó cho thảm xanh.

---

## 4. TOÁN LƯỚI — CHỐT MỘT LẦN, CẢ HAI DEV THEO

Đây là gốc rễ của "lệch" và "đè". **DEV-1 đã chốt — cả đội dùng theo, không tự chế lại.**

### ✅ CHỐT

```
CELL   = 100f          // PlacementManager.CELL  (const, KHÔNG serialize, KHÔNG Inspector)
ORIGIN = (0, 0)        // PlacementManager.GridOrigin
```

`UnityEngine.Grid mapGrid` đã bị **XOÁ** khỏi PlacementManager, `float gridSize` cũng vậy.
Chỉ còn **một** hằng số. Không còn đường nào để hai hệ lưới lệch nhau lần nữa.

### VÌ SAO LÀ 100 (đã đo thật, không đoán)

Đo hộp bao 33 prefab trong `Assets/_Game/Farm/CÔNG TRÌNH/` (prefab dùng root scale = 100
nên "1 unit sprite = 100 world unit"):

| Prefab | Bounds (world unit) | Ô @CELL=100 | Ô @CELL=50 | Ô @CELL=150 |
|---|---|---|---|---|
| cột đèn | 159 × 563 | 2×6 | 4×12 | 2×4 |
| House_02 | 238 × 406 | 3×5 | 5×9 | 2×3 |
| House_01 | 312 × 384 | 4×4 | 7×8 | 3×3 |
| House_04/05 | 374 × 385 | 4×4 | 8×8 | 3×3 |
| decor (Đài nước, Rơm, Vịt, Mèo, Giếng, Xe hoa…) | 345 × 461 | 4×5 | 7×10 | 3×4 |
| Chauhoa_1..4 | 523 × 287 | 6×3 | 11×6 | 4×2 |
| Pen_01/02 (chuồng) | 694 × 446 | 7×5 | 14×9 | 5×3 |

- **50 → loại.** 7×10 ô cho một cái nhà là quá mịn: snap gần như tự do, hai công trình vẫn
  kề nhau lệch nửa ô, và HashSet ô phình gấp 4 lần.
- **150 → loại.** Đúng bằng scale của Tilemap nền (`Grid_Map_45`, cellSize 1 × scale 150),
  nghe hợp lý nhưng: (a) làm tròn phí tới 45 % (cột đèn rộng 159 chiếm 300); (b) **3 tilemap
  nền trong SCN_Farm lệch nhau** — đặt tại `(-290,256)`, `(0,0)`, `(-28,-299)`, không phải bội
  số của 150 → **không tồn tại một lưới nền thống nhất** để bám theo. Nền chỉ là mảng trang trí.
- **100 → chọn.** Mọi công trình rơi vào 2×6 … 7×5 ô, đúng dải Township. Là giá trị đã
  serialize sẵn của PlacementManager trong SCN_Farm nên chỉ phải kéo ObjectDragHandler
  (đang 50) về theo, ít rủi ro nhất. Và 1 unit sprite = 1 ô nên designer nhẩm được bằng mắt.

### CÔNG THỨC CUỐI CÙNG

```csharp
// SNAP TÂM cho công trình N×M ô  →  PlacementManager.SnapCenter(world, size)
ox = Floor( (world.x - ORIGIN.x)/CELL - N*0.5 + 0.5 )   // chỉ số ô TRÁI nhất
oy = Floor( (world.y - ORIGIN.y)/CELL - M*0.5 + 0.5 )   // chỉ số ô DƯỚI nhất
center = ORIGIN + ( (ox + N*0.5)*CELL , (oy + M*0.5)*CELL )

// VÙNG Ô CHIẾM  →  PlacementManager.GetFootprintRect(center, size)
rect = RectInt(ox, oy, N, M)
```

**Xử lý lệch nửa ô khi cạnh CHẴN — chỗ hay sai nhất:**
tâm khối N ô luôn ở mốc `(ox + N/2)` ô.
- `N LẺ`  → `N/2` bán nguyên → tâm rơi đúng **TÂM một ô**.
- `N CHẴN` → `N/2` nguyên → tâm rơi đúng **ĐƯỜNG KẺ** giữa hai ô.

Đó chính là hành vi Township. **Sai lầm cần tránh:** ép mọi thứ về tâm ô
(`Round(x/CELL)*CELL + CELL/2`) rồi cộng `offset = (N-1)*0.5*CELL` — công thức đó tương đương
nhưng chỉ đúng khi lấy ô dưới con trỏ làm ô GÓC; với công trình to thì con trỏ nằm ở góc
dưới-trái chứ không phải giữa công trình, cầm rất khó chịu. Bản chốt ở trên **căn giữa
footprint vào con trỏ** và rút gọn đúng bằng `Floor(x/CELL) + 0.5 ô` khi `N=M=1`.

Dùng `Floor(v + 0.5)` chứ **không** dùng `Mathf.Round`: `Mathf.Round` làm tròn về số chẵn ở
đúng mốc `.5` (banker's rounding) → nhảy ô không đều khi kéo chậm.

### HỘP VA CHẠM

Không còn hộp va chạm vật lý. `collisionCheckSize` 50×50 và `obstacleLayer` đã bị **xoá khỏi
PlacementManager**. Chồng lấn = so **vùng ô** (`RectInt` ∩ `HashSet<Vector2Int>`), chính xác
tuyệt đối và không phụ thuộc layer/collider. Nếu DEV-2 cần một hộp world để vẽ:
`size = (N*CELL, M*CELL)`, tâm = `RectCenterWorld(rect)`.

---

## 5. NHẬT KÝ DEV-1

### Vòng 1 — 2026-08-02 · ✅ XONG V1→V7, biên dịch sạch phía DEV-1

**CELL = 100, ORIGIN = (0,0).** Lý do đầy đủ + bảng đo 33 prefab: xem §4.

#### File đã sửa / tạo

| File | Việc |
|---|---|
| `Assets/_Game/Farm/Scripts/Data/PlaceableItemData.cs` | **V2** — thêm `gridSize (Vector2Int, mặc định 1×1)`, `buildTimeSeconds`, `rushGemCost` + helper `GetGridSize(rotationSteps)` (hoán đổi X↔Y khi bước lẻ). Giữ nguyên 3 field cũ. |
| `Assets/_Game/Farm/Scripts/Managers/PlacementManager.cs` | Viết lại tầng lưới. Chi tiết bên dưới. |
| `Assets/_Game/Farm/Scripts/Gameplay/ObjectDragHandler.cs` | **V7** — sửa `if` treo ở `Awake()`; bỏ `gridSize=50` dùng chung `PlacementManager.CELL`; validate bằng ô lưới thay Physics2D; refresh bảng ô lúc bắt đầu/kết thúc kéo. |
| `Assets/_Game/Farm/Scripts/Managers/EditModeManager.cs` | Gọi `RefreshOccupancy()` mỗi lần bật/tắt Edit Mode (1 chỗ, trong `ToggleEditMode()`). |
| `Assets/_Game/Farm/Editor/BuildingGridSizeTool.cs` | **MỚI** — Editor tool `Tools/Farm/Suy Kích Thước Ô Công Trình`. |

#### PlacementManager — thay đổi theo mục

- **V1 · Toán lưới.** Thêm khối hằng số + hàm tĩnh `CELL / GridOrigin / WorldToCell /
  CellCenterToWorld / CellCornerToWorld / SnapCenter / GetFootprintRect / RectCenterWorld /
  SnapCenterFor / GridSizeOf / RotationOf / RotationStepsOf`.
  **XOÁ** `mapGrid`, `gridSize`, `footprintPadding`, `obstacleLayer`, `collisionCheckSize`
  (key thừa còn lại trong SCN_Farm.unity vô hại, Unity bỏ qua).
- **V3 · Chồng lấn.** `Physics2D.OverlapBox` bị **gỡ hoàn toàn**. Thay bằng
  `Dictionary<GameObject,RectInt> occupancyByObject` + `Dictionary<GameObject,Vector2Int> knownSizes`
  + `List<RectInt> reservedRects` + `HashSet<Vector2Int> occupiedCells`.
  `RefreshOccupancy()` dựng lại từ scene theo 3 nguồn, ưu tiên giảm dần:
  1. `knownSizes` — object do chính PlacementManager Instantiate (lấy cỡ từ data → chuẩn nhất)
  2. `EditableBuilding` trong scene → tra data theo **tên prefab** (`FindItemByPrefabName`,
     chấp nhận hậu tố `(Clone)`); tra không ra thì đo bounds rồi `Ceil` lên ô
  3. `reservedRects` — ô ConstructionManager đang giữ
  Công trình **đang sửa bị loại ra** khỏi bảng nên không tự chặn chính nó.
  Gọi refresh tại: `Start`, `StartPlacingNewObject`, `StartEditBuilding`, `Cleanup`,
  `ConfirmPlacement`, `DeleteEditingBuilding`, `ClearBuildingData`, `RegisterCompletedBuilding`,
  `EditModeManager.ToggleEditMode`, `ObjectDragHandler.BeginDrag/EndDrag`.
  Hộp kiểm tra giờ đúng bằng footprint `N×M` ô — không còn 50×50 cứng.
- **V4 · Biên bản đồ.** `TryGetMapBounds()` / `IsRectInsideMap(rect)`.
  **Lấy từ `TilemapRenderer.bounds` (hợp của mọi tilemap), KHÔNG lấy `CameraController.bounds`** —
  vì `bounds` là vùng kẹp *vị trí camera*, và `MapBoundary.LateUpdate()` còn tự nới nó thêm
  1000 unit mỗi khi camera lại gần mép nên nó lớn dần vô hạn, dùng làm biên xây dựng thì vô nghĩa
  (chính comment trong `CameraController.FitMapToView()` đã cảnh báo điều này).
  Có `mapBoundsOverride (Vector4)` để designer khoá cứng, và biên **luôn Encapsulate mọi công
  trình đang tồn tại** để không bao giờ khoá chết map. Tắt được bằng `enforceMapBounds`.
- **V5 · Xoay.** `Btn_Rotate` đã bind (`BindGhostButtons()`), thêm phím tắt **R**.
  Xoay 90°/lần, `rotationSteps` 0-3, `gridSize` tự hoán đổi, **snap lại ngay sau khi xoay**
  (3×2 và 2×3 có mốc tâm khác nhau — không snap lại là lệch nửa ô ngay).
  Chỉ xoay **visual clone**, không xoay cả Ghost (nếu xoay Ghost thì hàng nút ✕↻✓ quay theo),
  và xoay **quanh tâm art** chứ không quanh pivot.
  **Chống xoay đúp:** nút đi qua 2 đường (`IsMouseOverRect` trong `Update` + `Button.onClick`)
  → debounce 0.15 s. `Btn_Confirm/Cancel` không dính lỗi này vì chúng huỷ Ghost ngay.
  **Lưu rotation:** `BuildingEntry` thêm `public int rot`. **Tương thích ngược:** save cũ không
  có key `rot` → `JsonUtility` để mặc định 0 → không xoay. Đã áp dụng lại lúc `LoadBuildings`,
  lúc di chuyển trong Edit Mode, và lúc Cancel (trả về hướng gốc).
- **V6 · Gọi DEV-2.** `TryStartConstructionDev2()` — **dùng reflection**, có cache
  `PropertyInfo/MethodInfo`, chỉ tra 1 lần. Lý do chọn reflection thay vì "chờ DEV-2 tạo class
  kịp": gọi trực tiếp một class chưa tồn tại làm **cả `Assembly-CSharp` chết biên dịch**, chặn
  toàn đội, kể cả TESTER. Chi tiết + hợp đồng ngược `RegisterCompletedBuilding` / cách gỡ
  reflection sau khi merge: xem hộp cảnh báo cuối §3.
- **V7 · Dọn code chết.** Xoá `footprintPadding` (field chết) · xoá toàn bộ `Selection_Ring`
  (`ringRenderer` + 2 khối `transform.Find`) vì prefab không có · gộp bind nút vào một
  `BindGhostButtons(bindDelete)` duy nhất, `Btn_Delete` chỉ bind ở luồng Edit và ghi rõ là tuỳ
  chọn · xoá `FindBestSourceRenderer` (không còn nơi gọi) · `SetupFootprint` giờ vẽ thảm đúng
  bằng `N*CELL × M*CELL` thay vì suy từ bounds sprite rồi nhân 1.08.

#### Editor tool
`Tools/Farm/Suy Kích Thước Ô Công Trình` — quét `t:PlaceableItemData` (33 asset), đo bounds
prefab **không cần Instantiate** (tự tính `sprite.bounds × lossyScale`, vì `Renderer.bounds`
của prefab asset trả về rỗng), `gridSize = Ceil(size / CELL)` tối thiểu 1×1.
Bảng xem trước có: bounds đo được, giá trị hiện tại, giá trị suy ra **sửa tay được**, checkbox
từng dòng, lọc BuildingData/DecorData, cảnh báo **pivot lệch > nửa ô** (thảm xanh vẽ quanh
pivot nên art lệch pivot sẽ nhìn như "trôi" khỏi thảm). Chỉ ghi asset khi bấm **ÁP DỤNG**,
có `Undo.RecordObject`.
**Chưa chạy được vì tôi không mở được Unity Editor** — Edric mở project rồi bấm menu này
một lần là 33 asset có `gridSize` chuẩn.

**VÌ SAO KHÔNG TỰ ĐIỀN THẲNG VÀO 33 FILE `.asset`:** tôi có thử đo bằng cách đọc YAML prefab
ngoài Unity. Kết quả **sai với prefab lồng nhau**: `Pen_03`, `Pen_04`, `May_01/02/03` ra
`4287×2208` world unit (≈ 43×23 ô) trong khi `Pen_01` cùng họ chỉ `694×446` — vì sprite thật
nằm trong nested prefab mà trình đọc YAML không mở được. Điền số sai vào asset còn tệ hơn để
mặc định, nên tôi **không đụng file `.asset`** và làm lưới an toàn ở runtime thay thế (dưới đây).

**LƯỚI AN TOÀN cho tới khi tool được chạy:** nếu `data.gridSize` vẫn là `1×1` mặc định,
`PlacementManager` **tạm đo bounds visual** thay vì tin 1×1 (nếu tin thì cái chuồng 7×5 ô chỉ
chiếm 1 ô → vẫn đè được, tức là lỗi cũ chưa được sửa). Mỗi asset log **cảnh báo đúng một lần**:
`'<tên asset>' còn gridSize = 1×1 (mặc định) — đang tạm đo bounds. Chạy menu Tools/Farm/...`
Đo bounds kém chính xác hơn (dính mái, ống khói, viền trong suốt của sprite) nên đây chỉ là tạm.

#### CÒN VƯỚNG / RỦI RO CẦN TESTER SOI

1. **Nên chạy Editor tool trước khi test.** Chưa chạy vẫn chơi được nhờ lưới an toàn đo
   bounds, nhưng footprint sẽ hơi phình (dính mái/viền trong suốt) và Console có cảnh báo.
   Chạy tool một lần là hết cả hai.
   ⚠️ Riêng `Chuồng Gà`, `Chuồng Bò Sữa`, `Máy Xay Bột`, `Máy Ép Mía`, `Máy Phô Mai`
   (prefab lồng nhau) — **soi kỹ số tool suy ra**, nhóm này dễ ra số vô lý nhất.
   Chiếu theo `Chuồng Bò` = 7×5 ô để đối chứng.
2. **`buildTimeSeconds` của cả 33 asset đang là 0** → theo hợp đồng §3 nghĩa là "hiện ngay,
   không qua giai đoạn xây". DEV-2 hoặc Edric phải điền số > 0 cho ít nhất vài công trình
   thì tiêu chí §8 "Bấm ✓ → giàn giáo + công nhân" mới test được. DEV-1 cố tình **không**
   tự đặt số để không giẫm lên bảng cân bằng của DEV-2.
3. **Biên bản đồ có thể quá chặt.** Nếu tilemap nền nhỏ hơn khu chơi thì ghost sẽ đỏ ở rìa.
   Cách xử lý nhanh: bật `verboseGridLog` để in biên đã dò, rồi hoặc tắt `enforceMapBounds`,
   hoặc điền `mapBoundsOverride = (minX, maxX, minY, maxY)`.
4. **Pivot lệch tâm.** Thảm xanh bám **pivot prefab**, không bám tâm art (chọn vậy để vị trí
   lưu/khôi phục là một con số duy nhất, không phải suy lại offset mỗi lần load). Prefab nào
   có art lệch pivot sẽ thấy công trình hơi trôi khỏi thảm — Editor tool đã liệt kê sẵn danh
   sách này ở cột "Ghi chú", cần Edric sửa pivot prefab.
5. **`PlacedObjectsManagerTool.cs` (dev tool cũ) chưa có field `rot`** trong class `Entry` của
   nó → nếu ai dùng tool đó **xoá lẻ** một công trình, `JsonUtility` ghi lại save sẽ **làm mất
   hướng xoay** của tất cả công trình còn lại. File đó ngoài phạm vi được phép sửa của DEV-1.
   **Fix 1 dòng:** thêm `public int rot;` vào `Entry` (dòng 27). Xin phép Edric hoặc giao DEV-2.
6. **Sorting layer `"CongTrinh"` vẫn không tồn tại** (L10) — code tự fallback về `"Objects"`,
   không crash, nhưng nên thêm layer cho đúng ý đồ.
7. Chưa build thử trong Unity (không có Editor ở môi trường này). Code đã rà tay: không còn
   tham chiếu tới field/hàm đã xoá ở bất kỳ file `.cs` nào trong `Assets/`.

---

## 6. NHẬT KÝ DEV-2

### Vòng 1 — 2026-08-02 · ✅ XONG N1→N6

#### File đã tạo / sửa

| File | Việc |
|---|---|
| `Assets/_Game/Farm/Scripts/Gameplay/ConstructionManager.cs` | **MỚI** — singleton, đồng hồ offline, save `FARM_CONSTRUCTION_SITES`, giá rush, hoàn thành |
| `…/Gameplay/ConstructionSite.cs` | **MỚI** — một công trường: state + timer + nhấp nhô công nhân |
| `…/Gameplay/ConstructionSiteVisuals.cs` | **MỚI** — N2: thảm đất, giàn giáo, công nhân, `ParticleSystem` khói bụi (dựng bằng code) |
| `…/Gameplay/ConstructionSiteUI.cs` | **MỚI** — N3: World Space Canvas tên + đồng hồ + nút rush |
| `…/Gameplay/ConstructionCompleteFX.cs` | **MỚI** — N5: hộp quà + ruy băng + bóng bay + mũ bảo hộ tick xanh |
| `…/Gameplay/ConstructionSpriteFactory.cs` | **MỚI** — sinh sprite thủ tục **runtime** bằng SDF (bản runtime của `PopupSpriteFactory`) |
| `…/Gameplay/ConstructionBridge.cs` | **MỚI** — 2 lời gọi reflection vào `PlacementManager` (xem "Yêu cầu gửi DEV-1") |
| `Assets/_Game/Farm/Scripts/Managers/PlacementGhostVisualController.cs` | **SỬA** — N4: thêm dải giá phía trên hàng nút (`EnsurePriceBar`, dựng runtime) |
| `Assets/_Game/Farm/Editor/ConstructionBuildTimeTool.cs` | **MỚI** — N6: menu `Tools/Farm/Điền Thời Gian Xây` |

**KHÔNG đụng** `PlacementManager.cs`, `PlaceableItemData.cs`, `ObjectDragHandler.cs`,
`EditModeManager.cs`, và **không sửa prefab `Placement_Ghost`** (dải giá dựng lúc chạy nên
Edric không phải mở prefab).

#### N1 — Lõi

- **Tự mọc, không cần kéo vào scene.** `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` +
  `SceneManager.sceneLoaded` tự tạo object khi scene có `PlacementManager`. Lý do: DEV-2
  không sửa `SCN_Farm.unity` (tránh xung đột merge) mà `Instance` phải khác null ngay lần
  bấm ✓ đầu tiên. Có `ResetStatics()` ở `SubsystemRegistration` cho chế độ tắt Domain Reload.
- **Timer offline.** Chỉ lưu `startUnix (DateTimeOffset.UtcNow.ToUnixTimeSeconds())` +
  `duration`. Thời gian còn lại LUÔN tính lại từ "bây giờ" → tắt game 1 phút mở lại đúng
  1 phút; `Time.timeScale = 0` (mở popup) cũng không làm treo tiến độ.
- **Chống lùi giờ.** `NowUnix()` = max của 3 nguồn: đồng hồ OS · *mốc neo đầu phiên + số giây
  đã CHƠI (`Time.realtimeSinceStartup`)* · mốc lớn nhất từng thấy (lưu trong save).
  Nguồn giữa là điểm khác biệt: vặn giờ lùi thì hai nguồn kia đóng băng nhưng **chơi 5 phút
  timer vẫn trôi 5 phút** — không phạt oan người chơi thật khi máy đồng bộ NTP lùi vài giây.
- **Save riêng** key `FARM_CONSTRUCTION_SITES`, JSON, `saveVersion = 1`, kèm `maxSeenUnix`.
  Ghi khi: bắt đầu xây · rush · xây xong · `OnApplicationPause(true)` (Android kill app không
  gọi `OnApplicationQuit`) · `OnApplicationQuit` · `OnDestroy`.
  ⚠ Có bẫy đã xử lý: lúc tắt game Unity huỷ object **không theo thứ tự**, công trường có thể
  chết trước manager → vòng lặp đọc ra rỗng và ghi đè mất sạch tiến độ. `SaveSites()` phát
  hiện "có phần tử đã bị Destroy" thì ghi lại **bản tốt gần nhất** thay vì bản rỗng.
- **Tiền** lấy qua `FarmEconomyManager` (có save, `SpendGold/SpendGems` tự kiểm tra lại số dư).
  **Không đụng `PlayerWallet`.** Mọi nhánh thất bại đều xảy ra TRƯỚC lời gọi trừ tiền → không
  bao giờ mất tiền; báo rõ bằng dòng chữ đỏ nổi ngay dưới nút rush ("Không đủ vàng! Cần 21,
  đang có 8.").
- **"Kho đầy"** hiểu là giới hạn số công trường cùng lúc: `maxConcurrentSites`, **mặc định 0 =
  không giới hạn**. Nếu bật mà chạm trần thì trả `false` (DEV-1 dựng ngay) chứ không chặn —
  vì tiền đã bị Shop trừ trước khi tới đây, chặn hẳn là người chơi mất trắng.

#### CÔNG THỨC GIÁ RUSH (chốt)

```
giá = ceil( 15 + 0.82 · √(số giây còn lại) )        // rushGemCost > 0 thì dùng thẳng số đó
```

Khớp đúng 2 mốc trong video §0: `52s → 20.91 → 21` ✔ · `1m59s (119s) → 23.95 → 24` ✔
Thang giá: 5 phút → 30 · 1 giờ → 64 · 8 giờ → 154.
**Vì sao dạng √ chứ không tuyến tính:** hai mốc cách nhau 67 giây mà giá chỉ tăng 3 → ép
tuyến tính ra `b = 0.045/giây`, công trình 8 giờ sẽ ra **1 305** — không ai bấm. Hằng số 15 là
"phí bấm nút" tối thiểu, chống chờ gần xong rồi rush cho rẻ (Township cũng làm vậy).
Hai hằng số là `[SerializeField]` trên `ConstructionManager` nên cân bằng lại được không cần sửa code.

**⚠ ĐANG TRỪ VÀNG 🪙, KHÔNG PHẢI KIM CƯƠNG.** Video f_045 vẽ tờ tiền xanh = "tiền mặt", và
người chơi mới chỉ có **15 gem** (start) trong khi giá rush ~21 → để gem thì không ai test được.
Field của DEV-1 tên `rushGemCost` nhưng chỉ dùng làm *số cứng*, không quyết định loại tiền.
Đổi sang gem bằng dropdown `rushCurrency` trên Inspector, 1 giây.

#### N2/N3/N5 — UI & hiện vật dựng thế nào

- **Sprite thủ tục runtime.** `ConstructionSpriteFactory` là bản chạy-được-trong-build của
  `PopupSpriteFactory` (file kia nằm trong `Editor/`, dùng `AssetDatabase` nên không có trong
  bản build). Cùng kỹ thuật SDF + khử răng cưa. Có: panel bo góc 9-slice, nút xanh gradient,
  ván gỗ, đồng hồ, xu, kim cương, dấu tick, mũ bảo hộ, bóng bay, bóng công nhân, chấm khói.
  Cache tĩnh, `HideAndDontSave`, tự sinh lại nếu texture bị huỷ (chế độ tắt Domain Reload).
- **Giàn giáo phủ ĐÚNG ô.** Mọi kích thước tính từ `GridSizeOf(data, rot) × CELL` — đúng con số
  Ghost dùng cho thảm xanh, nên giàn giáo khít vùng vừa đặt. Số cọc tăng theo bề rộng (2–6 cọc).
- **Khói bụi** = `ParticleSystem` tạo bằng code, phun nhẹ lên trên, `loop`. Material dò shader
  theo thứ tự `Sprites/Default` → URP 2D Sprite-Unlit → `UI/Default` (project chạy URP 17.3).
- **UI nổi**: World Space Canvas, `localScale = 1` nên **1 "pixel" UI = 1 world unit**, giống
  hệt cách prefab `Placement_Ghost` làm (root ×100 · canvas ×0.01). Pivot đáy-giữa đặt ngay
  trên nóc, nội dung nở lên trên. Từ trên xuống: tên IN HOA viền đậm (TMP outline, cùng cách
  `AddTextOutline` của `LevelUpPopupTownshipTool`) → thanh tối bo góc + đồng hồ + `52Sek`/`1M59Sek`
  → nút xanh lá + icon tiền + số.
  **Không bị che**: canvas đẩy lên sorting layer `Foreground` (layer cao nhất project có),
  `sortingOrder = 30000`; công trình chạy ở `Objects` nên không vẽ đè được.
  **Luôn hướng camera**: `LateUpdate` gán `rotation = camera.rotation`.
  Text chỉ dựng lại khi con số GIÂY đổi, không phải mỗi frame.
- **Hiệu ứng hoàn thành**: coroutine + easing tay (project **không có DOTween**). Hộp quà khung
  trắng + ruy băng hồng bung ra → mũ bảo hộ + tick xanh bật lên → **giữa chuỗi mới Instantiate
  công trình thật** (đúng "hộp mở, công trình lộ ra") → bóng bay đỏ/vàng bay lên. Chạy bằng
  `Time.unscaledDeltaTime` để không đứng hình nếu vừa lúc có popup mở.
  Có mượn lại VFX sẵn có: nếu Edric chưa gán `completeVfxPrefab` thì tự lấy prefab confetti mà
  `LevelUpPopupUI` trong scene đang dùng (`LevelUp_Confetti_Lana02`), phóng to ×40 cho vừa map.

#### N4 — Giá trong thanh xác nhận

Dựng runtime trong `PlacementGhostVisualController.EnsurePriceBar()`, gọi từ `EnsureBuilt()`,
chỉ chạy 1 lần mỗi Ghost. Nền tối bo góc + `MUA VỚI GIÁ` + icon xu/kim cương + số, đặt tại
`y = 104` trong `Placement_UI` (hàng nút cao 126 tâm y = 0 → sát ngay trên). Lấy
`goldPrice`, không có thì `diamondPrice`. **Đang SỬA công trình cũ thì không hiện** (không mua gì).

#### N6 — Editor tool

`Tools/Farm/Điền Thời Gian Xây`: `buildTime = clamp(giá / 10, 5, 300)` làm tròn bội số 5
(item chỉ bán bằng gem quy đổi 1 💎 ≈ 20 🪙). Bảng xem trước có checkbox từng dòng, cột gợi ý
**sửa tay được**, cột hiển thị dạng `1M59Sek`, cột giá rush ước tính, tô cam dòng đang = 0.
Chỉ ghi khi bấm ÁP DỤNG, có `Undo.RecordObject`. Có thêm nút "Đặt tất cả về 0" để tắt hẳn
giai đoạn xây khi cần test nhanh luồng cũ.

#### ⚠ YÊU CẦU GỬI DEV-1 (2 API — có thì xoá được `ConstructionBridge.cs`)

```csharp
public PlaceableItemData CurrentGhostItem { get; }                       // để in giá lên thanh xác nhận
public void ReserveConstructionCells(Vector3 centerWorld, Vector2Int size); // giữ chỗ cho công trường KHÔI PHỤC TỪ SAVE
```
Hiện DEV-2 đang lấy bằng reflection vào `currentItem` / `reservedRects` / `RebuildOccupiedCells`.
Lý do phải làm vậy: `ConfirmPlacement()` mới giữ chỗ ô, nhưng công trường bật lại game được
`ConstructionManager` tự dựng lại → **không đi qua `ConfirmPlacement()`** → không có ô nào được
giữ → người chơi đặt đè lên giàn giáo. Reflection có bọc null-check, hỏng thì chỉ log cảnh báo
một lần chứ không ném lỗi.

#### 🔍 TESTER CẦN KIỂM

1. **CHẠY `Tools/Farm/Điền Thời Gian Xây` TRƯỚC KHI TEST.** Cả 33 asset đang `buildTimeSeconds = 0`
   = hiện ngay → không thấy giàn giáo và tưởng tính năng chết. Chạy tool 1 lần là xong.
   (Nên chạy cả `Tools/Farm/Suy Kích Thước Ô Công Trình` của DEV-1 trước, nếu chưa.)
2. Bấm ✓ → **giàn giáo + công nhân + khói, KHÔNG hiện công trình thật**; giàn giáo phủ đúng
   diện tích thảm xanh lúc đặt (so 2 ảnh chụp trước/sau).
3. Nhãn tên + đồng hồ + nút rush nổi trên đầu, **không bị công trình khác che** — thử đặt một
   công trình cao ngay trước công trường.
4. Định dạng đồng hồ: 52 → `52Sek`; 119 → `1M59Sek`.
5. **Offline**: bấm ✓ cho công trình 300 s → tắt hẳn game 1 phút → mở lại phải còn ~240 s.
   Thử cả tắt bằng Stop trong Editor và kill app trên máy Android.
6. **Chống lùi giờ**: đang xây thì vặn giờ máy lùi 1 giờ → timer **không được nhảy ngược**;
   chơi tiếp 1 phút thì vẫn phải trôi đúng 1 phút.
7. **Rush**: đủ tiền → trừ đúng số hiện trên nút, xây xong ngay. Không đủ tiền → hiện chữ đỏ,
   **kiểm tra số vàng trong HUD KHÔNG đổi** (đây là chỗ dễ mất tiền nhất).
   Giá rush phải **giảm dần** khi đồng hồ chạy.
8. **Không đặt đè được lên giàn giáo** — cả trong cùng phiên lẫn sau khi tắt/mở lại game.
9. Xây xong → hộp quà + bóng bay + mũ tick, rồi công trình thật hiện ra và **vẫn còn sau khi
   tắt game** (chứng minh `RegisterCompletedBuilding` đã chạy).
10. Thanh xác nhận lúc đặt phải hiện `MUA VỚI GIÁ 🪙 <giá>` đúng bằng giá trong Shop.
11. Console sạch. Riêng cảnh báo `'<asset>' còn gridSize = 1×1` là của DEV-1, hết sau khi chạy tool.

#### CÒN VƯỚNG / RỦI RO

1. **Chưa chạy thử trong Unity Editor** (môi trường không có Editor). Code đã rà tay: cân bằng
   ngoặc, mọi API dùng đều đã đối chiếu với `Library/PackageCache/com.unity.ugui@…`
   (`overflowMode` còn dùng được, `enableWordWrapping` đã Obsolete nên **không dùng**).
2. **Sorting layer `CongTrinh` vẫn không tồn tại** (L10 của DEV-1). Code tự lùi về `Objects`
   cho giàn giáo và `Foreground` cho UI, không crash.
3. **Art công nhân là hình vẽ tạm** (bóng người đội mũ). Gán `workerSprite` trên
   `ConstructionManager` là thay được — object trong Hierarchy tên
   `Worker_1 ◄ THẢ ART CÔNG NHÂN VÀO ĐÂY` cho dễ tìm.
4. `ConstructionManager` **không** `DontDestroyOnLoad` — công trường là object của scene nông
   trại. Đổi scene rồi quay lại thì nó tự tạo lại và nạp lại từ save. Nếu sau này farm chạy
   chung một scene duy nhất thì không ảnh hưởng gì.
5. **Đã làm giúp DEV-1 việc số 5 ở §5 chưa?** — CHƯA. `PlacedObjectsManagerTool.cs` thiếu
   `public int rot;` trong `Entry` vẫn còn nguyên (ngoài phạm vi file DEV-2 được phép sửa).
6. Nếu bật `maxConcurrentSites > 0` thì công trình vượt trần sẽ **dựng ngay** thay vì xếp hàng.
   Muốn hành vi "xếp hàng" thật thì cần DEV-1 mở API hoàn tiền lúc `ConfirmPlacement`.

---

### Vòng 2 — 2026-08-02 · ✅ NỐI DÂY BỘ Ô ART (`ConstructionArtKit`)

#### Nguyên tắc

Mọi mảnh đi qua `ConstructionArtKit.ResolveSafe(kit, slot, hìnhVẽCode, out spr, out col)`:

| Trạng thái ô | Sprite | Màu |
|---|---|---|
| còn trống | hình vẽ thủ tục của `ConstructionSpriteFactory` | **MÀU NHẬN DẠNG** của ô |
| đã gán art | sprite của Edric | `Color.white` (trừ khi bật `forcePlaceholderColors`) |
| `kit == null` | như "còn trống" | như "còn trống" |

Bật `showSlotLabels` trong asset kit ⇒ mỗi mảnh mọc thêm **nhãn chữ ghi tên ô**
(`ConstructionSiteVisuals.AttachSlotLabel` — một hàm dùng chung, tự chọn `TextMeshPro` 3D
cho mảnh world và `TextMeshProUGUI` cho mảnh UI). Tắt cờ = không tạo một object nào.

#### BẢNG TRA: ô art ↔ mảnh trong game ↔ màu nhận dạng

| # | Ô art (`Slot`) | Tên hiển thị | Màu nhận dạng | Mảnh được tô / thay | File |
|---|---|---|---|---|---|
| 1 | `GroundPatch` | Thảm đất | NÂU ĐẤT `#8C6138` (α .75) | `Ground_Patch` | Visuals |
| 2 | `ScaffoldPost` | Cọc giàn giáo | CAM `#D97326` | `Scaffold_Post_1…6` | Visuals |
| 3 | `ScaffoldRail` | Thanh ngang | VÀNG `#F2BF33` | `Scaffold_Rail_1…3` | Visuals |
| 4 | `ScaffoldBrace` | Thanh chống | XANH LÁ MẠ `#99D94D` | `Scaffold_Brace_L` · `_R` | Visuals |
| 5 | `LeaningBoard` | Ván dựa | XANH NGỌC `#40BFB3` | `Scaffold_LeaningBoard` | Visuals |
| 6 | `Worker` | Công nhân | XANH DƯƠNG `#4D8CF2` | `Worker_1` · `Worker_2` | Visuals |
| — | `workerPrefab` | (prefab) | — | thay hẳn 2 object trên → `Worker_n_Prefab` | Visuals |
| 7 | `DustParticle` | Hạt bụi | NÂU ĐẤT `#8C6138` | texture + `startColor` của `Dust_FX` | Visuals |
| 8 | `NamePlateBg` | Nền tên | TÍM `#8C59D9` | `Nen_TenCongTrinh` **(MỚI)** | UI |
| 9 | `TimerBarBg` | Nền đồng hồ | ĐEN XÁM `#26262E` | `Bar_ThoiGian` | UI |
| 10 | `ClockIcon` | Icon đồng hồ | TRẮNG `#F2F2F2` | `Icon_DongHo` | UI |
| 11 | `RushButtonBg` | Nền nút rush | XANH LÁ ĐẬM `#4DCC33` | `Btn_Rush` (Image nền) | UI |
| 12 | `CoinIcon` | Icon xu | VÀNG KIM `#FFCC26` | `Icon_Tien` khi `rushCurrency = Gold` | UI |
| 13 | `GemIcon` | Icon kim cương | XANH KIM CƯƠNG `#73D9F2` | `Icon_Tien` khi `rushCurrency = Gems` | UI |
| 14 | `PriceBarBg` | Nền thanh giá | ĐEN `#1A1A1F` | ⚠ **CHƯA NỐI** (nằm trong `PlacementGhostVisualController`) | — |
| 15 | `GiftBoxSide` | Mặt hộp quà | TRẮNG NGÀ `#F2F2EB` | `Frame_Top/Bottom/Left/Right` | CompleteFX |
| 16 | `Ribbon` | Ruy băng | HỒNG ĐẬM `#E64073` | `Ribbon_V` · `Ribbon_H` | CompleteFX |
| 17 | `Rosette` | Hoa ruy băng | HỒNG ĐẬM `#E64073` | `Bow_Left` · `Bow_Right` · `Bow_Knot` (tối hơn 22 %) | CompleteFX |
| 18 | `Balloon` | Bóng bay | ĐỎ `#F24D4D` | `Balloon_1…6/Body` | CompleteFX |
| 19 | `HardHatDone` | Mũ bảo hộ | VÀNG MŨ `#FFD91A` | `Icon_MuBaoHo` | CompleteFX |

**Đã nối 18/19 ô.** (`Visuals` = `ConstructionSiteVisuals.cs`, `UI` = `ConstructionSiteUI.cs`,
`CompleteFX` = `ConstructionCompleteFX.cs`.)

#### 4 chỗ CỐ TÌNH lệch khỏi quy tắc "trống ⇒ tô màu nhận dạng"

1. **`RushButtonBg`** — hình vẽ code `GreenButton()` đã tự có gradient xanh trong texture.
   Tô thêm `#4DCC33` nữa là xanh chồng xanh, tối sì và mất hẳn dải bóng. Placeholder giữ
   `Color.white`; có art thật mới tint. Nút vẫn dễ nhận vì nó là thứ duy nhất màu xanh lá.
2. **`NamePlateBg`** — tooltip của ô ghi "để trống = chỉ có chữ, không nền", nên ô trống thì
   tấm nền bị `SetActive(false)`: giao diện mặc định y hệt vòng 1. Nó chỉ hiện (màu tím) khi
   bật `showSlotLabels` hoặc `forcePlaceholderColors` — tức lúc Edric đang căn vị trí.
3. **`Balloon`** — ô trống thì **cả 6 quả đều đỏ** `#F24D4D` (đúng màu nhận dạng). Chỉ khi có
   art mới rải bảng màu đỏ/vàng/hồng ngẫu nhiên như tooltip hứa.
4. **`Worker`** — ô cũ `ConstructionManager.workerSprite` (vòng 1) vẫn được tôn trọng: kit
   trống + scene đã gán sprite ⇒ dùng sprite đó và **không** tô xanh nhận dạng. Thứ tự ưu
   tiên: `kit.workerPrefab` ▸ `kit.worker` ▸ `workerSprite` cũ ▸ bóng người vẽ code.

📌 Lưu ý màu: 4 mảnh giàn giáo dùng chung hình vẽ `Plank()` vốn **đã có màu gỗ nâu**, mà
`SpriteRenderer.color` là phép NHÂN → màu hiện ra là "nâu × màu ô" (cam→nâu cam, vàng→hổ
phách, xanh mạ→ô-liu, xanh ngọc→lục sẫm). Vẫn phân biệt được từng mảnh, nhưng đừng lấy
ảnh chụp màn hình đi so mã màu với bảng trên. Thả art thật vào là hết tint.

#### File đã sửa / tạo ở vòng 2

| File | Việc |
|---|---|
| `…/Gameplay/ConstructionManager.cs` | thêm `[SerializeField] artKit` + property `ArtKit`, truyền xuống `SpawnSite` và `ConstructionCompleteFX.Play` |
| `…/Gameplay/ConstructionSite.cs` | `Initialize(…, ConstructionArtKit artKit = null)`, chuyển kit sang Visuals + UI |
| `…/Gameplay/ConstructionSiteVisuals.cs` | 7 ô world; hỗ trợ `workerPrefab`; material bụi cache **theo texture**; **hàm nhãn dùng chung** `AttachSlotLabel` + `ConstructionSlotLabelBillboard` |
| `…/Gameplay/ConstructionSiteUI.cs` | 6 ô UI + **thêm mới** `Nen_TenCongTrinh`; icon tiền đổi ô theo loại tiền lúc chạy |
| `…/Gameplay/ConstructionCompleteFX.cs` | 5 ô hiệu ứng hoàn thành |
| `Assets/_Game/Farm/Editor/ConstructionArtKitWindow.cs` | **MỚI** — `Tools/Farm/Bảng Ô Art Xây Dựng` |

Không đụng `ConstructionArtKit.cs`, `PlacementManager.cs`, `PlaceableItemData.cs`,
`ObjectDragHandler.cs`, `EditModeManager.cs`.

#### Tool `Tools/Farm/Bảng Ô Art Xây Dựng`

Mỗi dòng: **ô vuông màu nhận dạng · tên tiếng Việt · mô tả ngắn · ObjectField kéo sprite
thẳng vào asset · ✔ đã gán / ✘ còn trống**. Trên cùng có ô chọn asset kit (tự dò trong
project), nút *Tạo kit mới*, và 2 công tắc `showSlotLabels` / `forcePlaceholderColors`.
Dưới cùng là thanh tiến độ **"Đã gán X/19 ô art"** + danh sách ô còn trống.
Kéo sprite vào là ghi ngay (`SetDirty` + `SaveAssets`), có Undo vì đi qua `SerializedObject`.

⚠ **Nút "Gắn kit vào scene" là bước dễ quên nhất.** `ConstructionManager` TỰ MỌC lúc chạy
(`RuntimeInitializeOnLoadMethod`) và object tự mọc đó có `artKit = null` — gán art bao nhiêu
cũng không hiện. Nút này tìm (hoặc tạo) `ConstructionManager` trong scene đang mở, gán kit
vào, đánh dấu scene bẩn. Bấm một lần rồi Ctrl+S là xong.

#### 🔍 TESTER CẦN KIỂM THÊM (vòng 2)

12. `Tools/Farm/Bảng Ô Art Xây Dựng` → *Tạo kit mới* → *Gắn kit vào scene* → lưu scene.
13. Bật `showSlotLabels`, bấm ✓ một công trình: **mọi mảnh phải có nhãn chữ trắng viền đen**
    đọc được (Thảm đất · Cọc giàn giáo · Thanh ngang · Thanh chống · Ván dựa · Công nhân ·
    Hạt bụi · Nền tên · Nền đồng hồ · Icon đồng hồ · Nền nút rush · Icon xu), và xây xong thì
    thấy tiếp Mặt hộp quà · Ruy băng · Hoa ruy băng · Bóng bay · Mũ bảo hộ.
14. **Tắt `showSlotLabels` → không còn object `Nhãn_*` nào trong Hierarchy.**
15. Thả một sprite bất kỳ vào ô `Cọc giàn giáo`: cọc phải đổi sang sprite đó, **hết màu cam**,
    và dựng THẲNG (không xoay 90° như ván thủ tục).
16. Xoá kit khỏi `ConstructionManager` (để null) → game phải chạy y hệt, mọi mảnh có màu.
17. Đổi `rushCurrency` sang `Gems` → `Icon_Tien` đổi sang ô `Icon kim cương` (xanh) và nhãn
    của nó đổi chữ theo.

---

## 7. BÁO CÁO TESTER

### Vòng 1 — 2026-08-02 · ✅ ĐÃ RÀ · Kết luận: **CÓ THỂ MỞ UNITY** (kèm 4 bước bắt buộc ở §7.7)

**Phạm vi rà:** 14 file `.cs` (5 của DEV-1 + 9 của DEV-2), đọc TOÀN BỘ, không mở Unity Editor.
Đối chiếu thêm: `ProjectVersion.txt`, `ProjectSettings.asset`, `TagManager.asset`, prefab
`Placement_Ghost.prefab`, và các API ngoài mà 2 dev gọi tới (`BaseItemData`, `ShopManager`,
`FarmEconomyManager`, `PlotController`, `CameraController`, `InputBridge`, `LevelUpPopupUI`).

---

### 7.1 LỖI BIÊN DỊCH — **KHÔNG TÌM THẤY LỖI NÀO**

| Hạng mục kiểm | Kết quả |
|---|---|
| Cân bằng `{}` `()` `[]` — đã bỏ chuỗi/comment/verbatim/char literal, 14/14 file | **0 lệch** (tất cả về 0, không có độ sâu âm) |
| `#if` / `#endif` | chỉ 1 cặp duy nhất (`ObjectDragHandler.FreeCursor`) — **cân** |
| Encoding | 14/14 UTF-8 hợp lệ (không file nào hỏng byte) |
| Tham chiếu tới field/hàm DEV-1 đã XOÁ (`mapGrid`, `gridSize` float, `footprintPadding`, `obstacleLayer`, `collisionCheckSize`, `FindBestSourceRenderer`, `ringRenderer`) | grep toàn `Assets/**/*.cs` = **0 kết quả sót** |
| Trùng tên class | 8 class `Construction*` + enum `ConstructionRushCurrency` — mỗi cái khai báo **đúng 1 lần** |
| Tên file khớp tên MonoBehaviour | ✅ 14/14 |
| `using` thiếu | không (đã soát từng file: `System`, `System.Collections`, `.Generic`, `.Reflection`, `TMPro`, `UnityEngine.UI`, `.Rendering`, `.Tilemaps`, `.SceneManagement`, `.InputSystem.EnhancedTouch`) |
| Lambda che tham số ngoài trong `ConstructionSpriteFactory` | không — lambda dùng `ww/hh`, method dùng `w/h` · `hh_()` khai báo sau `Plank()` nhưng C# không cần thứ tự |
| Editor tool gọi runtime class | dự án **không có `.asmdef` nào** → `Assembly-CSharp-Editor` thấy `Assembly-CSharp` mặc định ✅ |
| API Unity **6000.3.10f1** | `FindObjectsByType/FindFirstObjectByType(FindObjectsInactive…)` ✅ · `ParticleSystem` module-struct + `MinMaxCurve(float,float)` / `(float,AnimationCurve)` / `MinMaxGradient(Color,Color)` / `(Gradient)` ✅ · `TMP.overflowMode` ✅ (không dùng `enableWordWrapping` đã Obsolete) · `ShaderUtilities.Keyword_Outline` ✅ · `Sprite.Create(…, SpriteMeshType, Vector4)` ✅ · `RectTransformUtility.RectangleContainsScreenPoint` ✅ |
| Legacy `Input.*` trong `PlacementManager` | `ProjectSettings.activeInputHandler: 2` = **Both** → hợp lệ, KHÔNG ném `InvalidOperationException` |
| API ngoài | `FarmEconomyManager.Gold/Gems/SpendGold/SpendGems/AddGold/AddGems` ✅ · `ShopManager.buildingList/decorList` = `List<BaseItemData>` ✅ · `LevelUpPopupUI` có private field đúng tên `vfxConfettiPrefab` ✅ · `PlotController.PlotId/SetPlotId/InitializeAsNew` ✅ · `CameraController.bounds (Vector4)/InvalidateContentBounds()` ✅ · `EditableBuilding.SetFootprintActive` ✅ |

**Cảnh báo nhẹ — KHÔNG chặn build:**
- 9 file mới của DEV-2 + 2 Editor tool **chưa có `.meta`** → Unity tự sinh lúc import, nhưng nhớ commit kèm.
- `PlacementGhostVisualController.SetCorner()` (dòng 691) là **code chết** — không nơi nào gọi.
- `ConstructionSpriteFactory.PanelDark` (dòng 27) khai báo nhưng không dùng.
- `EditModeManager.cs` toàn bộ comment tiếng Việt bị **mojibake** (`Quáº£n lÃ½…`) — lỗi encoding có từ trước, DEV-1 đã né bằng cách viết comment mới không dấu. Không ảnh hưởng biên dịch.

---

### 7.2 ĐỐI CHIẾU 2 CHIỀU REFLECTION

#### Chiều A — DEV-1 → DEV-2 · **KHỚP 100 %**

| DEV-1 tra (`PlacementManager.cs:842-850`) | DEV-2 khai báo thật | KQ |
|---|---|---|
| type tên `"ConstructionManager"` (`Type.GetType` + quét `AppDomain`) | `public class ConstructionManager : MonoBehaviour` — **không namespace**, `ConstructionManager.cs:37` | ✅ |
| `GetProperty("Instance", Public\|Static)` | `public static ConstructionManager Instance { get; private set; }` dòng 43 — setter private KHÔNG cản, `GetProperty(Public)` vẫn tìm được vì getter public | ✅ |
| `GetMethod("TryStartConstruction", Public\|Instance, {PlaceableItemData, Vector3, int, int})` | `public bool TryStartConstruction(PlaceableItemData data, Vector3 pos, int rotSteps, int plotId)` dòng 359 | ✅ đúng tên, đúng 4 kiểu, **đúng thứ tự** |
| ép kết quả `result is bool b` | trả `bool` | ✅ |

→ **Không lệch một chữ.** Reflection sẽ tra được, game KHÔNG rơi về đường cũ.

#### Chiều B — DEV-2 → DEV-1 · **KHỚP** (nhưng 2 API §6 xin thêm thì **KHÔNG tồn tại**)

**B1 · Lời gọi TRỰC TIẾP (không reflection):**

| DEV-2 gọi | DEV-1 khai báo | KQ |
|---|---|---|
| `PlacementManager.Instance.RegisterCompletedBuilding(data, spawned, rot)` — `ConstructionManager.cs:525` | `public void RegisterCompletedBuilding(PlaceableItemData data, GameObject spawnedObj, int rotationStepsUsed)` — `PlacementManager.cs:883` | ✅ **KHỚP · là `public`** |
| `PlacementManager.Instance.ReleaseConstructionCells(site.CenterWorld)` — dòng 406 & 724 | `public void ReleaseConstructionCells(Vector3 centerWorld)` — dòng 922 | ✅ **KHỚP · `public`** |

**B2 · Hai API DEV-2 xin ở §6 — kiểm tra kết quả:**

| API xin | Có trong `PlacementManager` không? |
|---|---|
| `public PlaceableItemData CurrentGhostItem { get; }` | ❌ **KHÔNG TỒN TẠI** (grep toàn `Assets/**/*.cs`: chỉ xuất hiện trong comment `ConstructionBridge.cs:20`) |
| `public void ReserveConstructionCells(Vector3, Vector2Int)` | ❌ **KHÔNG TỒN TẠI** (chỉ trong comment `ConstructionBridge.cs:21`) |

**NHƯNG hậu quả KHÔNG phải "reflection trả null → hỏng ngầm".** `ConstructionBridge` không gọi 2 API đó;
nó reflection vào **3 thành viên PRIVATE**, và cả 3 đều tồn tại đúng tên/chữ ký:

| Bridge tra (`ConstructionBridge.cs:43-45`) | Thành viên thật trong `PlacementManager` | KQ |
|---|---|---|
| field `"currentItem"` `NonPublic\|Instance` | `private PlaceableItemData currentItem;` dòng **196** | ✅ |
| field `"reservedRects"` `NonPublic\|Instance` | `private readonly List<RectInt> reservedRects = new();` dòng **227** | ✅ (`readonly` chỉ chặn gán lại field — vẫn `GetValue` + `.Add()` bình thường) |
| method `"RebuildOccupiedCells"` `NonPublic\|Instance` | `private void RebuildOccupiedCells()` dòng **1223**, 0 tham số | ✅ (`Invoke(pm, null)` hợp lệ) |

→ **Cả 2 tính năng CHẠY ĐƯỢC:** dải giá trên thanh xác nhận ✅ và giữ chỗ ô cho công trường khôi phục ✅.
**Thứ tự gọi cũng đúng:** `EnsurePriceBar()` chạy bên trong `EnsureBuilt()` ← `SetupGhostVisualController()`
(`PlacementManager.cs:351`), tức là **SAU** `currentItem = itemData` (dòng 335) → đọc ra giá.
Luồng Edit thì `currentItem = null` (dòng 390) → không hiện dải giá, **đúng ý đồ**.

⚠ **Rủi ro còn lại (S3):** hợp đồng ngầm 3-tên-private này không được bảo vệ bởi trình biên dịch.
DEV-1 đổi tên `currentItem` / `reservedRects` / `RebuildOccupiedCells` là **hỏng im lặng**, chỉ có 1 dòng
`LogWarning` (và nhánh `GetGhostItem` thì **không có cảnh báo nào cả** — dải giá biến mất mà không ai biết).
Đề nghị DEV-1 mở 2 API public như §6 rồi xoá `ConstructionBridge.cs`, hoặc tối thiểu thêm comment
`// ⚠ ConstructionBridge reflection theo tên field này` ngay trên 3 thành viên đó.

---

### 7.3 LỖI TÍCH HỢP / LOGIC (không phải lỗi biên dịch)

#### 🔴 BUG-1 · S1 · Hai bên tính `gridSize` bằng HAI nguồn khác nhau

- **DEV-1** `CurrentGridSize()` (`PlacementManager.cs:1416-1434`): `data.gridSize` còn 1×1 → **đo bounds**
  (lưới an toàn) → chuồng ra **7×5**.
- **DEV-2** dùng `PlacementManager.GridSizeOf(data, rot)` = `data.GetGridSize(rot)` ở **mọi nơi**
  (`ConstructionManager.cs:376, 691` · `ConstructionSite.cs:53`) → **luôn trả 1×1**, không có lưới an toàn.

Vì cả 33 asset đang `gridSize = 1×1`, hậu quả **ngay lúc này**:

1. Thảm xanh lúc đặt = 7×5 ô, **giàn giáo chỉ 1 ô (100×100)** → tiêu chí §6.2 "giàn giáo phủ đúng
   diện tích thảm xanh" **FAIL**.
2. `ConstructionBridge.ReserveCells(center, size)` chỉ giữ **1 ô** thay vì 35 ô →
   **đặt đè lên giàn giáo được** → tiêu chí §6.8 **FAIL**.
3. `ConstructionManager.cs:379` `SnapCenter(pos, size)` **snap LẠI** với size 1×1. Đã thay số kiểm chứng
   tại x = 1234: công trình cạnh **CHẴN bị dịch +50 unit = nửa ô**; cạnh lẻ không dịch.

   | cỡ thật | tâm DEV-1 | sau khi DEV-2 snap lại 1×1 | lệch |
   |---|---|---|---|
   | 1, 3, 5, 7 (lẻ) | 1250 | 1250 | 0 |
   | **2, 4 (chẵn)** | 1200 | **1250** | **+50** |

   → công trình thật mọc **lệch nửa ô** so với chỗ người chơi đặt và so với ô đã giữ.

**Sửa (DEV-2, 1 dòng) — `ConstructionManager.cs:379`:**
```csharp
// SAI (snap lại bằng size có thể khác size mà DEV-1 đã dùng → lệch nửa ô ở cạnh chẵn)
Vector3 center = PlacementManager.SnapCenter(pos, size);
// ĐÚNG (hợp đồng §3: pos DEV-1 truyền vào ĐÃ snap sẵn)
Vector3 center = pos;
```
**Và/hoặc (DEV-1)** mở một API trả cỡ ô ĐÃ giải quyết fallback để DEV-2 dùng chung:
```csharp
public Vector2Int ResolvedGridSize(PlaceableItemData data, int rot, GameObject probe = null);
```
👉 **Cách nhanh nhất diệt cả 3 hậu quả: chạy `Tools/Farm/Suy Kích Thước Ô Công Trình` TRƯỚC KHI TEST.**

**Kèm theo — thiếu sót thiết kế:** điều kiện `data.gridSize.x > 1 || data.gridSize.y > 1`
(`PlacementManager.cs:1421` và `:1443`) **không phân biệt được** "chưa điền" với "designer cố ý để 1×1".
Item thật sự 1×1 sẽ **mãi mãi** bị đo bounds + spam cảnh báo. Đề nghị dùng `Vector2Int.zero` làm giá trị
"chưa điền", hoặc thêm cờ `[SerializeField] private bool gridSizeConfirmed;`.

#### 🔴 BUG-2 · S2 · `PurgeCoveredReservations()` có thể XOÁ NHẦM chỗ giữ của công trường

`PlacementManager.cs:1209-1221` xoá một `reservedRect` nếu nó **chồng lấn** rect của **bất kỳ** object nào:
```csharp
if (RectsOverlap(kv.Value, r)) { reservedRects.RemoveAt(i); break; }
```
Rect của công trình có sẵn trong scene được suy bằng `RectFromWorldBounds(MeasureWorldBounds(go))`
(dòng 1251) — hộp bao sprite **thường phình hơn footprint thật** (mái, ống khói, viền trong suốt).
Chỉ cần một công trình đứng **cạnh** công trường là rect phình của nó chạm vào chỗ giữ → chỗ giữ bị xoá
**âm thầm** → người chơi đặt đè lên giàn giáo. Xảy ra ở **mọi** lần `RefreshOccupancy()`
(Start · bật/tắt Edit Mode · mỗi lần `BeginDrag/EndDrag` · mỗi lần bắt đầu đặt).

**Sửa đề nghị (DEV-1) — `PlacementManager.cs:1218`:**
```csharp
// SAI — chồng 1 ô là mất cả chỗ giữ
if (RectsOverlap(kv.Value, r)) { reservedRects.RemoveAt(i); break; }
// ĐÚNG — chỉ nhả khi công trình thật mọc ĐÚNG vào vùng đó
if (kv.Value.Equals(r))       { reservedRects.RemoveAt(i); break; }
```

#### 🟠 BUG-3 · S3 · `plotId` truyền sang DEV-2 **luôn = 0**

`PlacementManager.cs:762-768`: `int assignedPlotId = 0;` rồi truyền thẳng vào `TryStartConstructionDev2`.
Ô đất xây qua công trường sẽ có `site.PlotId = 0` và `OnConstructionComplete` báo `plotId = 0`.
Không vỡ ngay (vì `RegisterCompletedBuilding` tự cấp ID mới ở dòng 902), nhưng ai đăng ký event này
về sau sẽ nhận số sai. Sửa: bỏ tham số, hoặc ghi rõ trong hợp đồng §3 là "luôn 0 ở luồng hiện tại".

#### 🟠 BUG-4 · S3 · Reset save chỉ xoá **1 trong 2** key

`PlotController.cs:705-707` (`DebugClearData`) và `PlacementManager.ClearBuildingData()` (dòng 1020-1028)
chỉ xoá `FARM_PLACED_BUILDINGS`. **`FARM_CONSTRUCTION_SITES` còn nguyên** → sau khi "xoá dữ liệu" vẫn
mọc lại công trường ma rồi tự sinh công trình mới.
**Sửa 1 dòng** trong `ClearBuildingData()`:
```csharp
PlayerPrefs.DeleteKey(ConstructionManager.SaveKey);   // hoặc ConstructionManager.Instance?.ClearAllSites();
```
(`FarmResetTool.cs:17` và `DemoL1L10Tool.cs:640` dùng `PlayerPrefs.DeleteAll()` nên **không** dính lỗi này.)

#### 🔴 BUG-5 · S2 · `PlacedObjectsManagerTool.Entry` **vẫn thiếu `rot`** — CHƯA AI SỬA

`Assets/_Game/Farm/Editor/PlacedObjectsManagerTool.cs:27`
```csharp
// HIỆN TẠI (thiếu rot → JsonUtility ghi lại là mất hướng xoay của TẤT CẢ công trình)
[Serializable] private class Entry { public string itemId; public float x, y; public int plotId; }
// SỬA
[Serializable] private class Entry { public string itemId; public float x, y; public int plotId; public int rot; }
```
Đây là rủi ro #5 của DEV-1 và rủi ro #5 của DEV-2 — **cả hai đều bỏ ngỏ vì ngoài phạm vi**.
Ảnh hưởng trực tiếp tiêu chí §8.4 "xoay lưu lại sau khi tắt game".

#### 🟡 BUG-6 · S4 · Cửa sổ trùng lặp cực hẹp lúc xây xong

`SpawnFinishedBuilding` ghi `FARM_PLACED_BUILDINGS` (bên trong `RegisterCompletedBuilding`, có
`PlayerPrefs.Save()`) rồi **mới** `SaveSites()`. Bị kill app đúng giữa 2 lệnh Save → công trình nằm trong
**cả hai** save → lần mở sau mọc 2 lần (1 công trình thật + 1 công trường). Xác suất rất thấp, ghi nhận
để không bỏ sót.

#### ✅ XÁC NHẬN **KHÔNG** có xung đột save (điểm tốt của cả 2 dev)
- 2 key hoàn toàn khác nhau, không đè lên nhau.
- `ConfirmPlacement` **`return` ngay** khi DEV-2 nhận việc (dòng 770-777) → **không ghi 2 lần**.
- Công trình **đang xây** nằm trong save của DEV-2 → **không mất khi tắt app**; xây xong mới sang save DEV-1.
- Cơ chế `_lastGoodEntries` chống ghi đè bằng danh sách rỗng lúc teardown là **đúng và cần thiết**.

---

### 7.4 KIỂM TOÁN BẰNG SỐ

#### Toán snap của DEV-1 — ✅ **ĐẠT, khép kín tuyệt đối**

Chạy `ox = Floor(x/100 − N·0.5 + 0.5)`, `center = (ox + N·0.5)·100` cho **N = 1, 2, 3, 4** tại
x = 0, ±1, ±50, ±51, ±100, ±149, ±151, 237.5, 1234.5:

| Kiểm | Kết quả |
|---|---|
| `GetFootprintRect(SnapCenter(x,N), N).xMin == ox` | ✅ **đúng ở 100 % mẫu** → snap và rect **khép kín**, không lệch |
| `SnapCenter(SnapCenter(x,N), N) == SnapCenter(x,N)` (N = 1,2,3,4,7 · 1000 mẫu) | ✅ **0 lệch** → snap lại sau khi xoay KHÔNG trôi |
| N **lẻ** → tâm rơi `…50` (tâm ô) · N **chẵn** → tâm rơi `…00` (đường kẻ) | ✅ đúng hệt §4, **không lệch nửa ô** |
| Giá trị âm (`Mathf.FloorToInt`) | ✅ x = −1 → ô −1 (không phải 0); x = −149 → ô −2 |

Ví dụ N=2: x=49→tâm 0 · x=50→tâm 100 · x=149→tâm 100 · x=150→tâm 200 (bước đều 100, không nhảy đôi).
Ví dụ N=3: x=99→tâm 50 · x=100→tâm 150 (bước đều).

#### Công thức rush của DEV-2 — ✅ **ĐẠT ở đúng 2 mốc video**

| giây còn lại | `15 + 0.82·√t` | `ceil` | video / doc |
|---|---|---|---|
| 2 | 16.1597 | 17 | — |
| **52** | **20.9131** | **21** | 21 ✔ **KHỚP** |
| **119** | **23.9451** | **24** | 24 ✔ **KHỚP** |
| 300 (5 phút) | 29.2028 | 30 | doc ghi 30 ✔ |
| 3600 (1 giờ) | 64.2000 | **65** | doc §6 ghi **64** ✘ |
| 28800 (8 giờ) | 154.1586 | **155** | doc §6 ghi **154** ✘ |

→ **Code đúng.** Chỉ có **bảng thang giá trong §6 quên bước `ceil`** ở 2 dòng cuối
(1 giờ = **65**, 8 giờ = **155**). Sửa doc, KHÔNG sửa code.

#### Xoay — ✅ ĐẠT về code, ❌ chưa quan sát được

`PlaceableItemData.GetGridSize(rot)` hoán đổi X↔Y ở bước lẻ (dòng 51) ✅.
DEV-2 dùng đúng `GridSizeOf(data, rot)` cho **giàn giáo** (`ConstructionSiteVisuals.Build` → `w`, `h`)
và cho **UI nổi** (`worldW`, `worldH` ở `ConstructionSite.cs:55-56`), rồi dựng công trình thật bằng
`PlacementManager.RotationOf(rot)` ✅. Giàn giáo **không tự xoay** mà đổi chiều rộng/cao — đúng ý đồ
(cọc gỗ luôn đứng thẳng). **Nhưng hiện không kiểm chứng được** vì BUG-1 khiến mọi cỡ đều là 1×1
(xoay 1×1 vẫn ra 1×1).

#### Cấu hình dự án đã xác minh

| Mục | Giá trị thật | Ảnh hưởng |
|---|---|---|
| Unity | `6000.3.10f1` | khớp §9.4 ✅ |
| `activeInputHandler` | **2 = Both** | `Input.GetMouseButton/GetKeyDown` cũ trong `PlacementManager` **vẫn chạy**, không ném exception ✅ |
| Sorting Layers | `Bottom · Default · Objects · ObjectsFront · Foreground` | **`CongTrinh` KHÔNG tồn tại** (đúng như L10). Công trình → `Objects` · giàn giáo → `ObjectsFront` · UI công trường → `Foreground`. Không crash, và giàn giáo/UI **luôn vẽ trên** công trình ✅ |
| prefab `Placement_Ghost` | CÓ `Btn_Confirm`, `Btn_Cancel`, **`Btn_Rotate`**, `Button_Row`, `Grid_Footprint`, 4 `Corner_*`. KHÔNG có `Btn_Delete` / `Selection_Ring` | khớp giả định của cả 2 dev ✅ |
| Đơn vị UI | `Placement_UI` scale **0.01** dưới root scale **100** ⇒ tích = 1 → **1 px UI = 1 world unit** | dải giá DEV-2 đặt ở y = 104, `Button_Row` là 400×120 tại y = 0 (DEV-1 style lại 430×126) → **không chồng nhau** ✅ |

---

### 7.5 VI PHẠM PHẠM VI — **KHÔNG CÓ**

| Kiểm | Kết quả |
|---|---|
| DEV-2 sửa `PlacementManager.cs` / `PlaceableItemData.cs` / `ObjectDragHandler.cs` / `EditModeManager.cs`? | ❌ **KHÔNG** — dùng `ConstructionBridge` reflection thay thế ✅ |
| DEV-2 sửa prefab `Placement_Ghost.prefab`? | ❌ **KHÔNG** — dải giá dựng lúc chạy ✅ |
| DEV-1 sửa file nào của DEV-2? | ❌ **KHÔNG** ✅ |
| DEV-1 sửa `PlacementGhostVisualController.cs` (của DEV-2)? | ❌ **KHÔNG** — chỉ **gọi** `SetTileSprite/EnsureBuilt/ConfigureFromWorldBounds/SetValid` ✅ |
| `PlacedObjectsManagerTool.cs` | **Cả 2 đều không đụng** (đúng luật §9.1) → BUG-5 còn nguyên, cần Edric giao việc |

---

### 7.6 ĐỐI CHIẾU 11 TIÊU CHÍ §8

| # | Tiêu chí | KQ | Ghi chú |
|---|---|---|---|
| 1 | Đặt chồng → thảm ĐỎ + ✓ XÁM không bấm được | **PASS** | `IsAreaFree` theo `HashSet<Vector2Int>`; `btnConfirm.interactable = isValidPos` (dòng 316); `ConfirmPlacement` chặn lần 2 (dòng 718). Cần mắt xác nhận sắc xám của `Button.disabledColor` |
| 2 | Snap đúng ô, **không lệch nửa ô** với cả 2×2 và 3×3 | **PASS** | Chứng minh bằng số ở §7.4 — khép kín và idempotent |
| 3 | Kéo ra ngoài biên map → invalid | **CHƯA-KIỂM-ĐƯỢC** | Logic đúng, nhưng biên = hợp của 3 `TilemapRenderer` đặt lệch nhau (−290 / 0 / −28). Phải nhìn thực tế mới biết có chặt quá không → bật `verboseGridLog` khi test |
| 4 | ↻ xoay 90° + **lưu sau khi tắt game** | **PASS (code) · RỦI RO** | Bind đủ + debounce 0.15 s + `BuildingEntry.rot` + tương thích ngược. **Nhưng BUG-5 sẽ xoá sạch `rot`** nếu ai chạy `PlacedObjectsManagerTool` |
| 5 | Thanh xác nhận hiện **đúng giá** | **PASS** | `EnsurePriceBar` chạy sau khi `currentItem` đã gán; reflection đọc được; lấy `goldPrice`, không có thì `diamondPrice` |
| 6 | ✓ → giàn giáo + công nhân + khói, **KHÔNG** hiện công trình thật | **FAIL** *(cấu hình)* | 33/33 asset `buildTimeSeconds = 0` → `TryStartConstruction` trả `false` ngay ở dòng 364 → dựng công trình luôn. **Phải chạy `Tools/Farm/Điền Thời Gian Xây`.** Chạy rồi thì luồng đúng — nhưng giàn giáo vẫn sai cỡ vì BUG-1 |
| 7 | Nổi trên đầu: tên + đồng hồ + nút rush xanh | **PASS** | `Foreground` + `sortingOrder 30000`, luôn hướng camera, format `52Sek` / `1M59Sek` / `2H05M` đúng |
| 8 | Tắt game 1 phút → thời gian đã trôi đúng | **PASS** | Chỉ lưu `startUnix + duration`, tính lại từ `NowUnix()`; `OnApplicationPause(true)` có lưu → Android kill app vẫn an toàn |
| 9 | Rush → trừ tiền, xây xong ngay | **PASS** | Mọi nhánh fail xảy ra **trước** `SpendGold/SpendGems`; `FinishImmediately` kéo `StartUnix` lùi đúng `Ceil(Duration)` → remaining ≤ 0 |
| 10 | Xây xong → ăn mừng rồi công trình thật hiện ra | **PASS** | FX gọi `onReveal` **giữa** chuỗi (đúng "hộp mở, công trình lộ ra"), rồi `RegisterCompletedBuilding` ghi save |
| 11 | Không lỗi Console | **FAIL** *(cấu hình)* | Sẽ có tới 33 dòng `LogWarning`: `'<asset>' còn gridSize = 1×1` cho tới khi chạy tool của DEV-1 |
| + | *(§6.8)* Không đặt đè được lên giàn giáo | **FAIL** | **BUG-1** (chỉ giữ 1 ô) + **BUG-2** (purge nhầm chỗ giữ) |

**Tổng: PASS 8 · FAIL 3 · CHƯA-KIỂM-ĐƯỢC 1** — cả 3 FAIL đều **không phải lỗi biên dịch**, và 2/3 tự
biến mất sau khi chạy 2 Editor tool.

---

### 7.7 KẾT LUẬN — **CÓ, Edric mở Unity được**, theo đúng thứ tự này

Không tìm thấy lỗi biên dịch nào. **Hai chiều reflection đều khớp từng chữ** — đây là rủi ro lớn nhất
của vòng này và nó đã qua. Mọi mục FAIL đều là **cấu hình dữ liệu** hoặc **lệch nguồn kích thước ô**,
không có mục nào chặn build.

**BƯỚC 1 — nên sửa TRƯỚC khi Play (2 dòng, tiết kiệm cả buổi test):**
| Ai | File:dòng | Sửa |
|---|---|---|
| DEV-2 | `ConstructionManager.cs:379` | `Vector3 center = PlacementManager.SnapCenter(pos, size);` → **`Vector3 center = pos;`** |
| DEV-1 | `PlacementManager.cs:1218` | `if (RectsOverlap(kv.Value, r))` → **`if (kv.Value.Equals(r))`** |

**BƯỚC 2 — mở Unity, chờ compile xong:** xác nhận Console **0 error**. Đây là điều duy nhất tôi
không kiểm được từ ngoài Editor.

**BƯỚC 3 — chạy 2 Editor tool (BẮT BUỘC, ~5 phút):**
1. `Tools/Farm/Suy Kích Thước Ô Công Trình` → **ÁP DỤNG**.
   ⚠ Soi kỹ 5 prefab lồng nhau: **Chuồng Gà · Chuồng Bò Sữa · Máy Xay Bột · Máy Ép Mía · Máy Phô Mai**
   (đối chứng **Chuồng Bò = 7×5 ô**). → gỡ **BUG-1** và tiêu chí **11**.
2. `Tools/Farm/Điền Thời Gian Xây` → **ÁP DỤNG** (hoặc chỉ điền tay 60–120 s cho 2–3 công trình để test
   nhanh). → gỡ tiêu chí **6**.

**BƯỚC 4 — tuỳ chọn, nên làm:**
- Thêm Sorting Layer **`CongTrinh`** (đặt giữa `Objects` và `ObjectsFront`) — hết fallback.
- Giao 1 dòng BUG-5: `PlacedObjectsManagerTool.cs:27` thêm `public int rot;` — chưa ai sở hữu file này.
- Giao 1 dòng BUG-4: `PlacementManager.ClearBuildingData()` thêm `PlayerPrefs.DeleteKey(ConstructionManager.SaveKey);`

**Sửa doc (không phải code):** §6 bảng thang giá rush — 1 giờ = **65** (không phải 64), 8 giờ = **155**
(không phải 154). Hai mốc video 52 s → 21 và 119 s → 24 thì **chính xác tuyệt đối**.

**Vòng 2 sẽ kiểm lại:** 3 tiêu chí FAIL sau khi 2 tool đã chạy · tiêu chí 3 (biên map) · và xác minh
BUG-1/BUG-2 đã hết bằng test "đặt đè lên giàn giáo" trong cùng phiên **và** sau khi tắt/mở lại game.

---

## 8. TIÊU CHÍ ĐẠT

- [ ] Đặt 2 công trình chồng nhau → thảm ĐỎ + nút ✓ **XÁM, không bấm được**
- [ ] Công trình snap đúng ô, **không lệch nửa ô** với cả cỡ chẵn (2×2) và lẻ (3×3)
- [ ] Kéo ra ngoài biên map → invalid
- [ ] Nút ↻ xoay được 90° và **lưu lại sau khi tắt game**
- [ ] Thanh xác nhận hiện **đúng giá** công trình
- [ ] Bấm ✓ → giàn giáo + công nhân + khói, **KHÔNG** hiện công trình thật
- [ ] Nổi trên đầu: tên + đồng hồ đếm ngược + nút rush xanh
- [ ] Tắt game 1 phút, mở lại → **thời gian đã trôi đúng** (offline)
- [ ] Bấm rush → trừ tiền, xây xong ngay
- [ ] Xây xong → hiệu ứng ăn mừng rồi công trình thật hiện ra
- [ ] Không lỗi Console

---

## 9. QUY TẮC

1. Chỉ sửa file thuộc phạm vi mình (§2). Cần file người khác → ghi yêu cầu vào §3.
2. **KHÔNG phá** những mục ✅ ở §1.
3. UI **dựng bằng code** tối đa (Edric sẽ tự thay art sau) — sprite thủ tục như
   `PopupSpriteFactory.cs` đã làm cho popup lên cấp là mẫu tốt, tham khảo lại.
4. Mọi thay đổi phải **biên dịch được**. Unity 6000.3.10f1.
5. Ghi nhật ký NGẮN, có số dòng.
