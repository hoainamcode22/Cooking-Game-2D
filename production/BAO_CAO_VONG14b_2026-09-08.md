# BÁO CÁO VÒNG 14b — 08/09/2026
Xử lý 4 việc Sếp nêu: gỡ núi giữ suối · tách thác + bọt · lỗi grid khi đặt công trình · camera bị chặn.

---

## 1. NÚI XẤU → GỠ, GIỮ DÒNG SUỐI

Tool đã viết lại. Menu cũ `Tools ▸ Farm ▸ Nui va Thac` **đổi thành** `Tools ▸ Farm ▸ Thac Nuoc`:

| Menu | Việc |
|---|---|
| **1. Tao prefab Thac + Bot** | Sinh 2 prefab để Sếp tự kéo thả, tự dựng thác theo ý |
| **2. Xoa phan NUI (giu dong suoi)** | Gỡ 4 tầng tường đá + đỉnh + suối-trên-đỉnh + thác cũ, **giữ nguyên dòng suối dưới đất**, đồng thời gỡ luôn 9 ô "hồ đá" 3×3 trôi trên cỏ mà Sếp chê |
| **3. Xoa SACH tat ca** | Gỡ hết, kể cả suối |

Phần dựng núi đã **bỏ hẳn khỏi code**, không còn cách nào lỡ tay dựng lại.

## 2. THÁC + BỌT — 2 PREFAB RỜI

| Prefab | Nội dung |
|---|---|
| `Assets/_Game/Farm/Prefabs/VFX/P_ThacNuoc.prefab` | 8 khung `Sheet_Waterfall45`, 12 fps, **lặp vô hạn**, sortingOrder 100 |
| `Assets/_Game/Farm/Prefabs/VFX/P_BotChanThac.prefab` | 8 khung `Sheet_BotChanThac`, 10 fps, lặp, sortingOrder 101 (vẽ đè lên chân thác) |

Kéo thả vào scene, chỉnh Scale và vị trí tuỳ ý. Muốn thác dài hơn thì kéo cao Scale Y hoặc
xếp chồng nhiều cái. Bọt để **10 fps lệch nhịp với thác 12 fps** cho đỡ lộ chu kỳ lặp.

### Sheet bọt là em tự cắt ra, không phải đội vẽ giao
Trong bộ art **không có** sprite bọt đứng riêng. Ba thứ gần nhất đều không dùng được:
- Ô 43 sheet vách (hồ chân thác) — có sẵn **đá và cỏ bao quanh**, đứng một mình thì lòi ra
  mảng đá hình lục giác. Chính là thứ Sếp thấy xấu.
- Ô 38–39 sheet nước — là ghềnh, không phải bọt.
- Bọt đẹp nhất nằm ở **chân sprite thác**, nhưng dính liền với cột nước.

Nên em cắt 96 hàng dưới cùng của cả 8 khung thác ra thành sheet riêng:
`Assets/maptitle/Map45Iso/Sheet_BotChanThac.png` — 512×192, 8 khung 128×96, PPU 128,
pivot (0.5, 0.28). Kèm `.meta` đầy đủ 8 sub-sprite + `nameFileIdTable`.

**Đây là bọt cắt lại, không phải bọt vẽ riêng.** Nó rộng đúng bằng cột nước (128 px) nên hợp
với thác hẹp; muốn hồ bọt loe rộng như hồ thật thì phải nhờ đội vẽ — em đã ghi vào brief v5.

## 3. TILE NƯỚC — **CHƯA THAY**, nói thẳng

| Thứ | Đang dùng art nào |
|---|---|
| Dòng suối em vừa vẽ | ✅ `Sheet_IsoWater45` (art mới, ΔE 0.70) |
| Con thác | ✅ `Sheet_Waterfall45` (art mới) |
| **BIỂN** (`Water_Tilemap`, chỗ tàu bè) | ❌ **vẫn là art cũ** `Sprite_WaterIcon.png` 64×64, ô vuông 100×100 trên `Grid_Map_45` |

Biển **chưa đụng tới**. Đây đúng là "Hướng 3" em nêu ở báo cáo vòng 14: vẽ lại 8.949 ô biển
bằng `Sheet_IsoWater45` để biển – bãi cát – suối – thác về chung một lưới iso 300×150.
Việc này Sếp nói sẽ tự vẽ tile nên em chưa tự làm.

---

## 4. LỖI GRID KHI ĐẶT CÔNG TRÌNH — ĐÃ SỬA

### Triệu chứng Sếp báo
Không đặt sát nhau được · khung lưới hiện quá nhiều ô · ô đất lệch khỏi tâm khung.

### Gốc rễ: **cả 3 là MỘT lỗi**
Log `[Place] o=(6,-14) co=3x4` là chìa khoá. Một ô đất chỉ chiếm 1×1, nhưng hệ thống tính ra 12 ô.

Chuỗi nhân quả:
1. Unity đặt tên bản sao trong scene là `Plot_01 (1)` … `Plot_01 (7)` (7/9 ô đất).
2. `FindItemByPrefabName` (dòng 1772) chỉ bóc `"(Clone)"`, **không bóc `" (n)"`** →
   `"Plot_01 (1)"` không khớp `"Plot_01"` → trả `null`.
3. `data == null` → `CurrentGridSize()` rơi xuống `fallbackGridSize`.
4. `fallbackGridSize` được gán ở dòng 1110 bằng `RectFromWorldBounds(bounds ghost clone)` —
   **đúng cái hàm chiếu 4 góc AABB vuông sang iso** đã bị gỡ khỏi `ComputeRectFor` vòng trước,
   nhưng vẫn còn sống ở đường ghost. Ghost còn bị nhân **×1.03** (dòng 2643) nên độ trải
   thành ±1.03 → ra 3×3 / **3×4** / 4×3 tuỳ phần lẻ vị trí.

Từ rect 12 ô đó suy ra cả 3 triệu chứng:
- Preview đọc thẳng `pm.CurrentRect` → tô 12 ô ⇒ **thừa ô**.
- Rect đè lên ô hàng xóm ⇒ **CHONG_LAN, không đặt sát nhau được**.
- `RectFromAnchor` đẩy tâm lên `HalfDepth`: 1×1 = 75 world, 3×4 = 262.5 world
  ⇒ khung bị đẩy cao hơn ô đất đúng **187.5 world** ⇒ **lệch tâm**.

Đã kiểm: `Plot_01.prefab` có `soO: {x:1, y:1}` **đúng**; scene **không có override** nào.
Số 3×4 sinh ra lúc chạy, không nằm trong file nào.

### Đã sửa — 2 chỗ, file `PlacementManager.cs`
**A.** Trong `CurrentGridSize()`, chèn nhánh đọc thẳng `BuildingFootprintKit.SoO` của vật đang
sửa, **trước khi** rơi xuống `fallbackGridSize`. Đây đúng nguồn sự thật mà `ComputeRectFor`
đã dùng nên hai bên khớp nhau.

**B.** Thêm hàm `BocHauToSoThuTu()` bóc hậu tố `" (n)"`, gọi trong `FindItemByPrefabName`.
Không dùng Regex để khỏi thêm `using` vào file.

### Cách Sếp tự kiểm
Nhấc bất kỳ ô đất nào (kể cả `Plot_01 (1)`…`(7)`), console phải in **`co=1x1`**.
Còn thấy `co=3x4` là chưa ăn.

### Về ý "2 khung lưới, đặt plot vào ô kế bên"
Sau khi sửa, mỗi ô đất chỉ chiếm đúng 1 ô lưới nên **đặt sát nhau được ngay**. Nếu Sếp còn muốn
preview hiện thêm 1 vành ô trống xung quanh để ngắm chỗ đặt tiếp, đó là tính năng mới trong
`IsoPlacementPreview` — nói em làm, khoảng 20 dòng.

---

## 5. CAMERA KHÔNG KÉO XUỐNG BẾN CẢNG — ĐÃ SỬA

### Gốc rễ
Không phải hằng số cứng, không phải Collider. `Main Camera.prefab` để `bounds ±5000` (thừa sức),
nhưng `LandExpansionManager.UpdateCameraBounds()` **ghi đè lúc chạy**: nó **thay thế** giới hạn
bằng hộp bao các ô đất **đã mở**.

Chỉ khu `Land_1_1` (ô 0..7) mở sẵn, 8 khu còn lại khoá tới level 5. Hộp đó + padding 600 ra:

| | Giá trị |
|---|---|
| Bounds đang chạy | X[−1650 … 1650] Y[**−600** … 1650] → hiển thị 3300 × 2250 ✓ khớp bảng debug |
| Tâm camera xuống thấp nhất | y = **−600** |
| Mép dưới thấy được (ortho 1068) | y = −1668 |
| Zoom xa nhất (ortho 1500) | y = −2100 |
| Cầu tàu `Tilemap_IsoDock` | y **−4875 … −3525**, tâm **−4200** |
| **Thiếu** | zoom hết cỡ vẫn cách mép cầu tàu **1425 unit** |

Nên đúng là **không có cách nào xuống tới**, không phải Sếp kéo sai.

### Đã sửa — `LandExpansionManager.cs`
Đổi từ **thay thế** sang **hợp (union)** với một sàn cố định:
```
SAN_MIN_Y = -5100   SAN_MIN_X = -2000   SAN_MAX_X = 2400
```
Mở thêm đất vẫn nới rộng bình thường, **không mất tính năng**.

Dùng `const` chứ không thêm `[SerializeField]` mới — field mới sẽ serialize thành `0` trong
scene có sẵn, đúng cái bẫy đã dính nhiều lần trong dự án này.

**Bounds sau khi sửa:** X[−2000 … 2400] Y[−5100 … 1650] → bảng debug sẽ hiện **4400 × 6750**.
Camera căn giữa được cầu tàu ở y −4200. ✓

---

## BACKUP
| File | Backup |
|---|---|
| `PlacementManager.cs` | `production/backup_scene_vong14/PlacementManager_before_fix.cs` |
| `LandExpansionManager.cs` | `production/backup_scene_vong14/LandExpansionManager_before_fix.cs` |
| `SCN_Farm.unity` | `production/backup_scene_vong14/SCN_Farm_before_thac.unity` |
| `Sheet_IsoSand45.png` | `production/backup_art_vong14/` (2 mốc) |

Đã kiểm cân bằng ngoặc trên cả 2 file `.cs`: phần em thêm lệch **0**. Line ending giữ nguyên LF.

*Không commit, không push, không sửa GitHub. Chưa đụng scene — 2 fix đều nằm trong code.*

## CHƯA LÀM (chờ Sếp)
1. Chặn đặt công trình xuống nước — cần sửa `PlacementManager`, chờ Sếp gật.
2. Vẽ lại biển thành tilemap iso (Hướng 3) — Sếp nói sẽ tự vẽ tile.
3. Preview hiện thêm vành ô trống xung quanh — tính năng mới, nói là em làm.
