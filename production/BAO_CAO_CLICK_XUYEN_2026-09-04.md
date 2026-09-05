# 🔍 RÀ SOÁT CLICK XUYÊN + MAP CỨNG — 2026-09-04

Backup: `production/backup_round11_2026-09-04/` (10 file + checksum)

## PHÁT HIỆN GỐC: Bếp load ADDITIVE

`FarmUIManager.cs:540` — `LoadScene(cookingSceneName, ..., LoadSceneMode.Additive)`
⇒ **scene Farm VẪN CHẠY NGẦM** khi người chơi ở Bếp. Mọi handler `OnMouseDown` của farm vẫn sống,
collider vẫn bắt click. Thêm nữa Bếp (`SampleScene`) có Camera riêng cũng tag `MainCamera`
⇒ khi farm camera bị tắt, `Camera.main` trỏ vào camera Bếp, nhưng **physics world dùng chung**
⇒ click trên món ăn quy đổi ra toạ độ world trúng collider con vật farm.

### Chuỗi chính xác gây bug Sếp báo
`LivestockAI.OnMouseDown()` (0 hàng rào) → `PenMiniPanelUI.OpenPanel()` → nhánh **Processing**
gọi thẳng `PenProcessPopupUI.Open()` (0 hàng rào) ⇒ popup chuồng bật đè lên màn hình Bếp.
*(Nhánh Idle/Ready có kiểm `isCookingMode`; riêng nhánh Processing — trạng thái phổ biến nhất khi
người chơi rảnh tay đi nấu ăn — bị bỏ sót.)*

### Cơ chế đã có nhưng KHÔNG ĐƯỢC DÙNG
`FarmUIManager` có sẵn 2 mảng `behavioursToDisableInCooking[]` và `popupObjectsToForceClose[]`,
code enable/disable đã viết đủ — nhưng trong `SCN_Farm.unity` (dòng ~453218) **cả hai đều RỖNG**.

---

## 🪤 CÁI BẪY LỚN — phải biết trước khi sửa

`FarmInputLock.BlockWorldInteraction` (chốt chặn dùng chung) có kiểm `EventSystem.IsPointerOverGameObject()`.
Nhưng `Main Camera.prefab:101` có **`Physics2DRaycaster` với `eventMask = Everything`** (`m_Bits: 4294967295`)
⇒ hàm đó trả **TRUE khi con trỏ nằm trên BẤT KỲ Collider2D nào** — tức **đúng lúc** người chơi bấm
vào chuồng/ruộng/nhà.

**Hệ quả:** 4 script vẫn được coi là "an toàn" (`PenClickDetector`, `MillBuildingClick`,
`OrderBoardWorldObject`, `StallWorldObject`) thực chất **tự chặn chính mình** — tương tác thật
đang đi vòng qua các đường `OnMouseDown` KHÔNG có rào. Nếu cứ thế gắn `BlockWorldInteraction`
vào 9 cửa còn lại thì **toàn bộ world-click sẽ chết**.

---

## ✅ ĐÃ SỬA

### 1. `FarmInputLock.cs` — sửa tận gốc (159 → 219 dòng)
| Sửa | Nội dung |
|---|---|
| Tên scene chết | `GetSceneByName("SCN_Cooking")` → **`"SampleScene"`**. Scene `SCN_Cooking` **không tồn tại** ⇒ nhánh dự phòng của `IsCookingMode` trước nay là **dead code** |
| Thêm `ConTroTrenUiThat()` | Chỉ tính "trên UI" khi hit đến từ **`GraphicRaycaster`**; bỏ qua hit của `Physics2DRaycaster` |
| `BlockWorldInteraction` | Dùng `ConTroTrenUiThat()` thay `IsPointerOverGameObject()` ⇒ **4 script cũ hoạt động đúng trở lại** |
| Thêm `BlockWorldClickBySceneOrPopup` | Cổng RIÊNG cho `OnMouseDown` — giống cái trên nhưng **không kiểm UI dưới con trỏ** (vì `OnMouseDown` chỉ nổ khi con trỏ đã ở trên collider của chính nó) |

### 2. Gắn rào 9 cửa (11 điểm chèn)
| File | Hàm | dòng |
|---|---|---|
| `LivestockAI.cs` | `OnMouseDown` | 378 |
| `PenProcessPopupUI.cs` | `Open` | 79 |
| `CharacterVoiceReaction.cs` | `OnMouseDown` | 26 |
| `HouseGrowthController.cs` | `CheckInputClick` | 185 |
| `DecorGrowthBootstrap.cs` | `Decor5Runtime.Update` | 579 |
| `DecorGrowthController.cs` | `CanAcceptClick` | 431 (`return false`) |
| `BoatDockSlot.cs` | `OnMouseDown` + `OnMouseUpAsButton` | 120, 148 |
| `TouristAgent.cs` | `OnMouseUpAsButton` | 489 |
| `TrainWagonSlot.cs` | `OnMouseDown` | 212 |

### 3. Map cứng đơ (vòng trước, nhắc lại)
- `CameraController.cs` — chỉ chặn kéo map khi hit từ `GraphicRaycaster`. Công tắc `Chi Chan Boi Ui That`.
- `AudioManager.cs` — giữ đúng 1 AudioListener, kiểm lại mỗi 0.5s.

---

## QA vòng 11
| Kiểm | Kết quả |
|---|---|
| tree-sitter, 12 file sửa trong 3h | **0 lỗi** |
| EOL | 10/10 giữ nguyên (CRLF/LF đúng từng file) |
| BOM | `TrainWagonSlot.cs` giữ nguyên BOM UTF-8 |
| Diff | thuần cộng thêm — 9 file chỉ +2..+4 dòng, không xoá dòng nào |
| Dùng đúng cổng | 11/11 điểm dùng `BlockWorldClickBySceneOrPopup`, **không** nơi nào dùng nhầm `BlockWorldInteraction` |
| Kiểu return | `CanAcceptClick` dùng `return false`, `Decor5Runtime` dùng `{ _pressed=false; return; }`, còn lại `return;` |

---

## 🧑 SẾP TEST — 6 mục

**A. Map cứng đơ**
1. Kéo map ở **vùng bến tàu** — phải mượt.
2. Bấm **ổ khoá bến** — popup mua bến vẫn mở được.
3. Bấm nút UI (Cửa hàng/Kho) rồi kéo — nút **vẫn phải chặn** kéo map.

**B. Click xuyên**
4. Vào **Bếp**, bấm lung tung vào món ăn / khoảng trống — **không** popup farm nào được bật lên.
5. Mở popup bất kỳ ở Farm (Kho, Chợ, Đơn hàng) rồi bấm xuyên qua nền popup — không được kích hoạt vật phía sau.
6. Bấm bình thường vào **chuồng, ruộng, nhà đang xây, bến tàu, toa tàu, khách du lịch** — tất cả **vẫn phải bấm được** (đây là phép thử chống hồi quy quan trọng nhất: nếu có cái nào bấm không được thì báo tên, tôi nới rào đúng chỗ đó).

---

## ⏳ CÒN LẠI (chưa làm, chờ Sếp)
- `behavioursToDisableInCooking[]` trong Inspector `FarmUIManager` vẫn rỗng — điền vào sẽ là lớp chặn thứ 2 ở tầng nguồn. Chưa làm vì đụng scene.
- Hiệu năng: tilemap Individual (~27.000 ô) · `Canvas_Popup` 697 CanvasRenderer. Chỉ đụng nếu còn giật.
