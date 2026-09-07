# TEAM PLAN — LƯỚI ISO THỐNG NHẤT & HỆ MỞ RỘNG ĐẤT

> Trạng thái: **Giai đoạn 0 đã xong** (lõi + tool đã có trong repo, chưa đụng `PlacementManager`).
> Tài liệu liên quan: `production/TEAM_PLACEMENT_CONSTRUCTION.md` (§4 Toán lưới — sẽ bị thay bởi tài liệu này).

---

## 1. TẠI SAO CÔNG TRÌNH KHÔNG ĐẶT SÁT NHAU ĐƯỢC

Đã quét toàn bộ hệ thống. Có **2 nguyên nhân gốc**, cộng dồn lại thành lỗi bạn thấy.

### Nguyên nhân A — hai hệ lưới đá nhau

| Hệ | Hình ô | Kích thước | Gốc |
|---|---|---|---|
| Nền bản đồ `Grid_Iso45` | **kim cương** (Isometric) | cellSize (1, 0.5) × scale 150 = **150 × 75 world** | (0,0) |
| `PlacementManager.CELL` | **vuông** | **100 × 100 world** | (0,0) |

Hai lưới này không có bội số chung theo cả 2 trục, lại khác hình dạng ô. Hệ quả: **không có vị trí nào** để công trình vừa khít ô nền vừa khít công trình bên cạnh. Đây là lý do map nhìn "lệch lệch" dù code snap chạy đúng.

> Ghi chú lịch sử: comment trong `PlacementManager.cs` (dòng 80–86) nói *"3 tilemap nền lệch nhau nên KHÔNG tồn tại lưới nền thống nhất"* — điều đó **đúng ở thời điểm đó**, nhưng nay đã có `Grid_Iso45` là lưới nền duy nhất và chuẩn.

### Nguyên nhân B — `gridSize` làm tròn LÊN

`BuildingGridSizeTool` tính `gridSize = Ceil(bounds / CELL)`:

| Công trình | Art (world) | gridSize | Ô chiếm (world) | **Hở** |
|---|---|---|---|---|
| House_01 | 312 × 384 | 4 × 4 | 400 × 400 | **88 × 16** |
| Pen_01 (chuồng) | 694 × 446 | 7 × 5 | 700 × 500 | 6 × **54** |
| Cột đèn | 159 × 563 | 2 × 6 | 200 × 600 | **41** × 37 |

Hở gần 1 ô ⇒ hai nhà đặt "sát nhau" theo lưới vẫn cách nhau ~90 world trên màn hình. Đây chính là khoảng cách giữa các `Plot_01` bạn chỉ ra.

### Nguyên nhân phụ C — art top-down trên nền iso

Art công trình hiện tại (Happy Harvest) có **chân hình chữ nhật**, còn ô nền là **hình thoi**. Sau khi sửa A + B, công trình sẽ xếp đều và dính nhau, nhưng muốn khít hoàn hảo về hình học như Township thì chân công trình phải vẽ lại theo hình thoi. → Việc của DEV-4 (art), làm dần, không chặn tiến độ.

---

## 2. KIẾN TRÚC ĐÍCH

```
                       ┌──────────────────────────────┐
                       │   IsoGrid  (static, mới)     │  ← NGUỒN SỰ THẬT DUY NHẤT
                       │  WorldToCell / CellToWorld   │     bám Grid_Iso45 (150×75)
                       │  SnapAnchor / RectFromAnchor │
                       └──────────────┬───────────────┘
              ┌───────────────┬───────┴────────┬──────────────────┐
     PlacementManager   ObjectDragHandler  ConstructionManager  LandExpansionManager
     (ghost, occupancy)  (kéo tay)          (đang xây)           (khu đất, hàng rào)
```

**Quy ước toạ độ mới** (giữ tinh thần V8 cũ để ít phá vỡ nhất):

- `ANCHOR` = `transform.position` của công trình = **đỉnh Nam (thấp nhất)** của vùng ô.
- `CENTER` = tâm hình học vùng ô — dùng cho thảm nền, giàn giáo, VFX.
- `RectInt(ox, oy, N, M)` = vùng ô trên lưới iso.
- Bất biến bắt buộc: `Snap(Snap(p)) == Snap(p)` (đã chứng minh trong `IsoGrid.SnapCenter`).

---

## 3. ĐÃ CÓ SẴN TRONG REPO (giai đoạn 0 — xong)

| File | Nội dung |
|---|---|
| `Assets/_Game/Farm/Scripts/Grid/IsoGrid.cs` | Toán lưới iso dùng chung. Tự tìm `Grid_Iso45`, fallback 150×75. API: `WorldToCell`, `CellCenterToWorld`, `SnapCenter`, `SnapAnchor`, `RectFromAnchor`, `RectCenterWorld`, `FootprintWorldSize`, `CellCorners`, `RotateSize`. |
| `Assets/_Game/Farm/Scripts/Land/LandRegionData.cs` | ScriptableObject 1 khu đất: `cellRects` (toạ độ ô iso), `goldPrice`, `unlockLevel`, `requiredRegionIds`, `unlockedByDefault`. Có `ContainsCell`, `BorderCells`, `CenterWorld`. |
| `Assets/_Game/Farm/Scripts/Land/LandExpansionManager.cs` | Quản lý mua/mở đất. `IsCellUnlocked`, `IsRectUnlocked`, `CanBuy(out reason)`, `TryBuy`, event `OnRegionUnlocked`. Lưu `PlayerPrefs["FARM_UNLOCKED_REGIONS"]`. Tự vẽ overlay khoá + hàng rào lên tilemap. Có Gizmo xem khu ngay trong Scene. |
| `Assets/_Game/Farm/Scripts/Land/LandRegionSign.cs` | Biển "CẤP X MỞ / giá tiền" giữa khu chưa mua, bấm → `onRequestBuy` (nối popup) hoặc mua thẳng. |
| `Assets/_Game/Farm/Editor/IsoLandRegionTool.cs` | `Tools/Farm/Khu Dat` — sinh lưới khu đất (giá & cấp tăng dần từ tâm ra), dựng `LandExpansion` + `Tilemap_LockedOverlay` vào scene. |
| `Assets/_Game/Farm/Editor/IsoGridSizeTool.cs` | `Tools/Farm/Suy Kich Thuoc O theo LUOI ISO` — đo lại footprint theo ô iso, **làm tròn gần nhất** thay vì Ceil, có bảng xem trước + sửa tay từng dòng. |

---

## 4. CHIA VIỆC

### DEV-1 — LÕI LƯỚI (ưu tiên cao nhất, chặn mọi việc khác)
**Mục tiêu:** `PlacementManager` snap theo `IsoGrid`.

Sửa `Assets/_Game/Farm/Scripts/Managers/PlacementManager.cs`, giữ nguyên tên hàm public để không vỡ chỗ gọi:

```csharp
// dòng ~88  — GIỮ hằng cho code cũ tham chiếu, nhưng KHÔNG dùng để snap nữa
[System.Obsolete("Dùng IsoGrid. CELL chỉ còn cho migrate save v1→v2.")]
public const float CELL = 100f;

// dòng ~133 — chuyển tiếp sang IsoGrid
public static Vector3 SnapAnchor(Vector3 world, Vector2Int size)
    => IsoGrid.SnapAnchor(world, size);

// dòng ~151
public static RectInt RectFromAnchor(Vector3 anchorWorld, Vector2Int size)
    => IsoGrid.RectFromAnchor(anchorWorld, size);

// dòng ~94 / 99 / 105
public static Vector2Int WorldToCell(Vector3 w)      => IsoGrid.WorldToCell(w);
public static Vector3 CellCenterToWorld(Vector2Int c) => IsoGrid.CellCenterToWorld(c);

// dòng ~321 — nửa chiều sâu theo lưới iso
private static float HalfDepthWorld(PlaceableItemData d, int rot)
    => IsoGrid.HalfDepth(GridSizeOf(d, rot));
```

Việc kèm theo:
1. `RectFromWorldBounds` (dòng ~1931) — đổi sang `IsoGrid.WorldToCell` cho 4 góc bounds.
2. `TryGetMapBounds` / `IsRectInsideMap` (dòng ~2009, ~2074) — **thêm điều kiện đất**:
   ```csharp
   if (LandExpansionManager.Instance != null &&
       !LandExpansionManager.Instance.IsRectUnlocked(rect)) return false;
   ```
3. **Save v1 → v2**: viết `MigrateV1ToV2()` — đọc anchor cũ (lưới vuông 100), đổi sang ô iso gần nhất bằng `IsoGrid.SnapAnchor`, tăng `CurrentSaveVersion = 2`. Chạy đúng 1 lần, có log.
4. `ObjectDragHandler.SnapToGrid` (dòng ~394) đã gọi `PlacementManager.SnapAnchor` → tự động đúng, chỉ cần test lại 7 prefab Pen/May.

**Nghiệm thu:** đặt 2 nhà cạnh nhau → không còn khe hở; kéo ra thả lại → không dịch nửa ô; save/load giữ nguyên vị trí.

---

### DEV-2 — FOOTPRINT & COLLIDER
**Mục tiêu:** mọi công trình có `gridSize` bám sát art.

1. Chạy `Tools/Farm/Suy Kich Thuoc O theo LUOI ISO` → duyệt bảng → Apply.
   Bảng đề xuất khởi điểm (ô iso 150×75, art hiện tại):
   | Công trình | gridSize cũ (vuông 100) | **gridSize mới (iso)** |
   |---|---|---|
   | Home1–Home5 | 4×4 | **2×2** |
   | Chuồng (Pen_01…04) | 7×5 | **5×4** |
   | Máy chế biến | 7×5 | **5×4** |
   | Khung Hoa | 8×3 | **4×3** |
   | Đất (Plot_01) | 4×2 | **3×2** |
   | Chậu hoa | 1×1 | **1×1** |
   | Cột đèn / Bù nhìn | 2×2 | **1×1** |
2. Chạy lại `Tools/Farm/Bộ Kit Đặt Công Trình` để collider + `BuildingFootprintKit.soO` khớp size mới.
3. Kiểm tra `HouseGroupNormalizeTool` vẫn ép Home1–5 cùng size.

**Nghiệm thu:** thảm nền footprint ôm đúng chân art, không thừa quá 10% mỗi cạnh.

---

### DEV-3 — HỆ MỞ RỘNG ĐẤT
**Mục tiêu:** người chơi bấm khu đất trống → mua → map rộng ra.

1. `Tools/Farm/Khu Dat` → chọn 3×3 khu, mỗi khu 8×8 ô → **Sinh asset** → **Dựng vào Scene**.
2. Vẽ 1 tile "cỏ dại / lớp tối" cho khu chưa mua (dùng `Sheet_IsoGrass45` giảm sáng 45%, hoặc tile cỏ khô) → gán vào `lockedOverlayTile`.
3. Dựng prefab biển báo: SpriteRenderer (bảng gỗ) + BoxCollider2D + TextMesh + `LandRegionSign` → gán vào `signPrefab`.
4. Nối popup xác nhận mua: `onRequestBuy` → mở popup có sẵn của dự án → nút Đồng ý gọi `LandExpansionManager.Instance.TryBuy(region)`.
5. Nối `OnRegionUnlocked` → VFX + `PlacementManager.RefreshOccupancy()` + refresh camera bounds.
6. Thêm nút reset trong `ChoiLaiTuDauTool`: `PlayerPrefs.DeleteKey("FARM_UNLOCKED_REGIONS")`.

**Nghiệm thu:** khu chưa mua có hàng rào bao quanh + biển "CẤP X MỞ", không đặt được công trình vào; mua xong hàng rào biến mất, đặt được ngay, thoát game vào lại vẫn nhớ.

**Tư vấn tile hay prefab** (câu bạn hỏi): **dùng tile cho hàng rào** — `RuleTile_IsoFence45` tự nối, tự bẻ góc, đổi hình dạng khu chỉ cần sửa `cellRects` chứ không phải dựng lại prefab. Prefab chỉ dùng cho **biển báo** và **decor cổng vào** (những thứ chỉ có 1 cái mỗi khu).

---

### DEV-4 — ART & EDIT MODE UX
1. **Lưới overlay khi vào Edit Mode**: hiện `EditModeManager.gridOverlay` = null trong scene → vào Edit Mode không thấy lưới nào. Dựng overlay vẽ ô kim cương bằng `IsoGrid.CellCorners` (shader đơn giản hoặc tilemap mờ).
2. **Snap preview**: khi kéo, tô sáng đúng các ô sẽ chiếm (xanh = trống, đỏ = vướng, vàng = ngoài đất chưa mua).
3. **Art chân hình thoi**: vẽ lại chân các công trình chính (nhà, chuồng, máy) theo đáy hình thoi để khít tuyệt đối — làm dần từng nhóm.
4. Bật lại nút xoay nếu cần (`choPhepXoayCongTrinh`) sau khi lưới ổn định.

---

## 5. THỨ TỰ TRIỂN KHAI

```
Tuần 1  DEV-1 lõi lưới  ──▶ DEV-2 footprint  ──▶ chơi thử, chỉnh số
                         └─▶ DEV-3 khu đất (chạy song song, chỉ cần IsoGrid)
Tuần 2  DEV-4 overlay + preview  ──▶ art chân hình thoi (dài hạn)
```

**Rủi ro & phòng ngừa**

| Rủi ro | Phòng ngừa |
|---|---|
| Save cũ dịch chỗ toàn map | Viết `MigrateV1ToV2` + **backup `SCN_Farm.unity` trước khi chạy lần đầu** |
| Grid_Iso45 bị đổi scale sau này | `IsoGrid` đọc động từ Grid, không hardcode; nếu xoá Grid thì fallback 150×75 |
| Công trình cũ nằm ngoài khu đất | `enforceLandBounds` mặc định vẫn cho phép ô không thuộc khu nào (tương thích map cũ) |
| Trộn lẫn 2 hệ toạ độ khi đang chuyển | Cấm gọi `PlacementManager.CELL` trong code mới; đã đánh `[Obsolete]` |

---

## 6. CHECKLIST NGHIỆM THU CUỐI

- [ ] Đặt 4 nhà thành hàng: 4 chân nhà nằm trên cùng đường kẻ, không hở
- [ ] Đặt chuồng cạnh nhà: viền chạm viền, không chồng
- [ ] Kéo công trình ra rồi thả lại đúng chỗ cũ: không dịch
- [ ] Thoát game → vào lại: mọi công trình đúng vị trí
- [ ] Khu chưa mua: không đặt được, có hàng rào + biển báo
- [ ] Mua khu: trừ tiền đúng, hàng rào biến mất, đặt được ngay
- [ ] Vào Edit Mode: thấy lưới kim cương, ô chiếm tô đúng màu
