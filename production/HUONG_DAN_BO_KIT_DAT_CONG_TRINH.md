# BỘ KIT ĐẶT CÔNG TRÌNH — HƯỚNG DẪN

Phân tích từ video tham chiếu (25,6s · 767 frame) + khảo sát 34 prefab công trình trong dự án.

---

## 1 · HỆ THỐNG HIỆN TẠI ĐANG HỎNG Ở ĐÂU

Khảo sát dữ liệu thật, không phải đoán:

| # | Lỗi | Bằng chứng |
|---|---|---|
| 1 | **Phần lớn công trình không nhấc lên được** | Điều kiện duy nhất là có `EditableBuilding`. Chỉ 8/34 prefab có: `House_01/02`, `Chauhoa_1/2`, `Pen_02`, `May_01..03` |
| 2 | **16 prefab trang trí không có collider nào** | Giếng, bù nhìn, cột đèn, xe hoa… → `OnMouseDown` không bao giờ chạy → đặt xuống là mất luôn |
| 3 | **Thảm nền gãy ở MỌI công trình** | `footprintVisual` trỏ tới fileID `8293749280720246623` trong `House_01.prefab` — object đó không tồn tại trong file. `SetFootprintActive()` là lệnh rỗng khắp nơi |
| 4 | **Vào Edit Mode không có phản hồi nào** | `gridOverlay`, `overlayImage`, `editModeLabel` của `EditModeManager` đều null trong `SCN_Farm` |
| 5 | **Chỉ 6 ô đất nhấc được trong scene** | `Plot_02..07`. Nhà dân, chuồng, chợ/kho đặt sẵn thì không |
| 6 | **Chợ / Cổng Bếp / Kho kéo được nhưng KHÔNG lưu** | `ObjectDragHandler` không ghi PlayerPrefs → Play lại là về chỗ cũ |
| 7 | 4 asset Chậu Hoa khai `gridSize 1×1` | Sai với hình thật, kích hoạt nhánh tự-đo + cảnh báo mỗi lần chạy |
| 8 | `Chậu Hoa3 → Chauhoa_4.prefab`, `Chậu Hoa4 → Chauhoa_3.prefab` | Hoán vị. Không lỗi chạy nhưng sửa giá sẽ nhầm chậu |

---

## 2 · BỘ KIT MỚI — CỐ Ý KHÁC VIDEO

Video tham chiếu dùng: **hình thoi tô đặc nửa trong suốt + 4 nêm tam giác đặc ở giữa cạnh**.

Bộ này giữ nguyên *chức năng* (báo vùng chiếm, báo hợp lệ/không) nhưng đổi hẳn *hình dạng* — phần được bảo hộ bản quyền:

| Thành phần | Video | Bộ này |
|---|---|---|
| Thảm nền | hình thoi **tô đặc** phẳng | hình thoi **bo góc, rỗng ruột**, sáng dần ra mép |
| Dấu góc | **4 nêm tam giác** đặc, ở **giữa cạnh** | **4 ngoặc chữ L** ôm **4 góc**, kiểu khung ngắm máy ảnh |
| Viền | đường liền | **nét đứt** chạy dọc cạnh |
| Bảng màu | xanh lá chanh | **xanh ngọc `#5FD9A8`** / san hô `#FF7A66` |
| Dấu "nhấc được" | không có | **chip 3 vạch** nổi trên nóc, nhấp nhô nhẹ |

Hình học thảm dùng `|x|^1.35 + |y|^1.35 = 1`. Số mũ 1 cho hình thoi nhọn (đúng hình video), 2 cho hình tròn — chọn 1.35 để ra hình thoi có góc bo, nhìn là nhận ra khác.

### Cấu trúc kit trên mỗi công trình

```
Công_trình
└── Kit_Nen                 ← tự dựng lúc chạy, không cần gán tay
      ├── Tham_Nen          thảm hình thoi bo góc
      ├── Vien_0 .. Vien_3  4 vạch nét đứt dọc 4 cạnh
      ├── Ngoac_0 .. Ngoac_3  4 ngoặc chữ L ôm 4 góc
      └── Chip_Keo          chip "nắm để kéo" trên nóc
```

---

## 3 · CHẠY TOOL

**`Tools ▸ Farm ▸ Bộ Kit Đặt Công Trình`**

| Mục | Việc |
|---|---|
| `1 · Kiểm tra — sẽ đổi những gì` | Chỉ đọc. In bảng 34 prefab: ô lưới, nhấc được chưa, có collider chưa, có kit chưa |
| `2 · Gắn kit + cho phép nhấc vào TẤT CẢ` | Thêm `EditableBuilding` + `BoxCollider2D` ôm vùng ô + `BuildingFootprintKit`, gỡ tham chiếu thảm gãy |
| `3 · Sửa gridSize và prefab bị lệch` | `gridSize 1×1` → đo lại từ hình vẽ; cảnh báo asset trỏ nhầm prefab |
| `4 · Báo cáo — công trình trong scene` | Liệt kê object trong scene: nhấc được không, thiếu gì |

**Thứ tự:** 1 → xem log → 3 → 2 → **Ctrl+S** → 4 → Play thử.

Chạy mục 2 trước mục 3 cũng được nhưng collider sẽ lấy `gridSize` sai của 4 chậu hoa.

Ctrl+Z hoàn tác được cả hai mục.

---

## 4 · VÌ SAO COLLIDER ÔM VÙNG Ô

Bạn đã chọn phương án này. Lý do nó đúng:

```
Cột đèn — ôm sát hình vẽ:        Cột đèn — ôm vùng ô (2×2):
      ┌┐                          ┌─────────────┐
      ││   collider ~40×180       │     ┌┐      │  collider 200×200
      ││                          │     ││      │
      ┴┴                          │     ┴┴      │
                                  └─────────────┘
  ngón tay ~90px → bấm trượt        bấm đâu cũng trúng
```

Quan trọng hơn: vùng bấm **trùng với tấm thảm** hiện bên dưới. Người chơi thấy thảm ở đâu là bấm được ở đó — không phải đoán.

Collider đặt `size = (N·100, M·100)`, `offset = (0, M·50)` vì vùng ô mọc **lên** từ chân công trình (quy ước "V8" của `PlacementManager`).

---

## 5 · CHỢ / CỔNG BẾP / KHO

Bạn chọn giữ `ObjectDragHandler` (kéo thẳng, mượt hơn) và chỉ thêm phần lưu. Đã làm:

- Khoá riêng `FARM_DRAG_OBJECT_POS`, có `saveVersion`
- Ghi ở `EndDrag()` khi vị trí hợp lệ
- Đọc ở `Start()`, rồi gọi `RefreshOccupancy()` để bảng ô không giữ chỗ cũ

**Không** nhét vào `FARM_PLACED_BUILDINGS` vì khoá đó là danh sách vật do `PlacementManager` *sinh ra* — `LoadBuildings()` đọc rồi `Instantiate` prefab. Ba công trình này dựng sẵn trong scene, nhét vào đó thì lần load sau sinh thêm bản sao chồng lên bản gốc.

Định danh bằng **tên object**. Đổi tên thì mất vị trí đã lưu, không hỏng gì khác.

> Lưu ý: người chơi vẫn thấy **hai kiểu tương tác** trong cùng Edit Mode — ba công trình này chạm là kéo đi ngay, còn lại phải giữ 0,3s rồi bấm ✓. Bạn đã biết và chấp nhận.

---

## 6 · GẮN ART CỦA BẠN VÀO

Mở `BuildingFootprintKit` trên bất kỳ công trình nào, có 4 ô Sprite:

| Ô | Thay bằng | Lưu ý |
|---|---|---|
| `Sprite Tham` | thảm nền | vẽ theo tỉ lệ vuông, code tự kéo giãn theo số ô |
| `Sprite Ngoac` | ngoặc góc | vẽ cho **góc trên-trái**, code tự xoay 3 góc kia |
| `Sprite Vach` | vạch nét đứt | vẽ nằm ngang, code tự xoay cho cạnh dọc |
| `Sprite Chip` | chip nắm kéo | vuông |

> ⚠ **Ảnh phải TRẮNG hoặc XÁM.** Màu do code nhuộm qua `SpriteRenderer.color` để đổi xanh↔đỏ theo trạng thái hợp lệ. Vẽ sẵn màu xanh vào ảnh thì lúc chuyển đỏ sẽ ra màu bùn.

Để trống ô nào thì ô đó dùng hình vẽ bằng code — vẫn chạy được, không lỗi.

Ba ô màu bên dưới (`Mau Tham`, `Mau Vien`, `Mau Chip`) chỉnh sắc độ mà không cần vẽ lại.

---

## 7 · KIỂM TRA SAU KHI CHẠY TOOL

Bấm Play rồi:

- [ ] Bấm nút Edit Mode (hoặc phím **E**) → mọi công trình hiện thảm + 4 ngoặc góc + chip trên nóc
- [ ] Thảm **trùng khít** vùng ô, không lệch xuống dưới chân hay lên trên nóc
- [ ] Giữ 0,3s trên **một công trình trang trí** (giếng, cột đèn) → nhấc lên được
- [ ] Nhấc lên → ghost hiện với nút ✓ ✕ ↻, kéo ra chỗ hợp lệ → xanh, chỗ chồng → đỏ
- [ ] Bấm ✓ → công trình đứng chỗ mới, thảm theo đúng chỗ mới
- [ ] Mua một công trình mới từ Shop → khung đặt trông **giống hệt** lúc nhấc công trình cũ
- [ ] Kéo Chợ sang chỗ khác → Stop → Play lại → **Chợ vẫn ở chỗ mới**
- [ ] Tắt Edit Mode → mọi thảm/ngoặc/chip biến mất sạch

Mục nào không đạt thì chạy `4 · Báo cáo` và gửi tôi log.

---

## 8 · CÒN LẠI

| Việc | Ghi chú |
|---|---|
| `EditModeManager.gridOverlay` vẫn null | Vào Edit Mode chưa có lớp phủ toàn màn hình. Kit trên từng công trình đã đủ báo hiệu, nhưng nếu muốn thêm nền mờ toàn map thì nói tôi |
| Object trong scene đã bị **Unpack Prefab** | Không tự nhận thay đổi từ prefab. Mục 4 sẽ chỉ ra cái nào |
| Sorting layer ID `1669604809` mồ côi | Không có trong TagManager, runtime rơi về `Objects`. Không gây lỗi nhưng nên dọn |
| `EditModeManager.cs` hỏng encoding | Phần lớn chú thích tiếng Việt đã thành mojibake. Sửa file này phải cẩn thận encoding |
