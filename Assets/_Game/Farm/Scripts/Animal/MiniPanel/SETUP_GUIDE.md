# Hướng dẫn Setup PenMiniPanel trong Unity Editor

## 1. Compile và verify

Sau khi Unity compile xong, kiểm tra Console không có error.
4 script mới: PenMiniPanelUI, PenClickDetector, PenDropTarget, PenBasketDragItem.

---

## 2. Tạo Prefab PF_PenMiniPanel

### Cấu trúc hierarchy:
```
PF_PenMiniPanel (World Space Canvas)
  ├── PanelBG          (Image — background cong)
  ├── Slot_Food1       (Image icon + TMP text số lượng góc)
  │     └── txtAmount  (TMP_Text "x2")
  ├── Slot_Food2       (Image icon + TMP text số lượng góc)
  │     └── txtAmount  (TMP_Text "x5")
  ├── Slot_Basket      (Image icon)
  │     └── GlowEffect (Image/ParticleSystem — bật khi Ready)
  └── ProgressOverlay  (active=false mặc định)
        ├── ProgressFill  (Image — fillAmount, fillMethod: Horizontal)
        └── txtTimer      (TMP_Text "1:23")
```

### Canvas settings:
- Render Mode: **World Space**
- Sort Order: **600** (cao hơn chuồng 500, animal 510+)
- Scale: điều chỉnh sao cho panel vừa mắt trong game world (thử ~0.005)

### Component trên PF_PenMiniPanel gốc:
- Thêm **BoxCollider2D** → size bao phủ toàn bộ panel (để detect click outside)
- Thêm script **PenMiniPanelUI** → gán:
  - `panelRoot` = chính GameObject này (hoặc PanelBG)
  - `slot1Root` = Slot_Food1
  - `slot1Icon` = Image icon của Slot_Food1
  - `slot1Amount` = txtAmount của Slot_Food1
  - (tương tự slot2)
  - `basketRoot` = Slot_Basket
  - `basketIcon` = Image icon của Slot_Basket
  - `basketActiveGlow` = GlowEffect
  - `progressOverlay` = ProgressOverlay
  - `progressFill` = ProgressFill
  - `progressLabel` = txtTimer
  - `panelCollider` = BoxCollider2D trên chính nó

---

## 3. Setup mỗi Prefab Chuồng (Pen_01 .. Pen_04)

### Trên GameObject chuồng, thêm:
1. **BoxCollider2D** — bao phủ vùng click của chuồng
2. Script **PenClickDetector** — gán:
   - `miniPanel` = instance PenMiniPanelUI (xem bước 4)
3. Script **PenDropTarget** — gán:
   - `miniPanel` = instance PenMiniPanelUI

### Đặt PenMiniPanel:
- Instantiate PF_PenMiniPanel làm **child** của GameObject chuồng
- Đặt localPosition = (1.5, 1.0, 0) hoặc điều chỉnh sao cho nổi cạnh chuồng
- Gán PenMiniPanelConfig đúng loại:
  - Pen_01 → Config_Pen01_BoThit
  - Pen_02 → Config_Pen02_Heo
  - Pen_03 → Config_Pen03_Ga
  - Pen_04 → Config_Pen04_BoSua

---

## 4. Gán DraggableFeedItem trên Slot_Food1 / Slot_Food2

- Thêm component **DraggableFeedItem** (bò/heo dùng đúng class tương ứng)
- Set `feedItemId`:
  - Slot_Food1 (bò thịt): `rice`
  - Slot_Food2 (bò thịt): `ngo`
  - Slot_Food1 (heo): `bapcai`
  - Slot_Food2 (heo): `carot`
  - ... (xem Config asset)
- `imgFeedIcon` = Image icon của slot
- `txtFeedAmount` = TMP text số lượng

---

## 5. Gán PenBasketDragItem trên Slot_Basket

- Thêm component **PenBasketDragItem**
- `basketImage` = Image icon của Slot_Basket

---

## 6. Gán Icon trong Config assets

Mở Project window → Farm/Data/PenConfig → từng Config_Pen0x.asset:
- `food1Icon`: kéo sprite Lúa (hoặc Bắp cải)
- `food2Icon`: kéo sprite Ngô (hoặc Cà rốt)
- `productIcon`: kéo sprite Thịt bò / Heo / Gà / Sữa
- `secondProductIcon`: (chỉ gà) kéo sprite Trứng
- `basketIcon`: kéo sprite Rổ

---

## 7. Thêm Item_Milk vào WarehousePopupUI

Trong Inspector của WarehousePopupUI (trên scene):
- Tìm field **Extra Item Database**
- Thêm `Item_Milk.asset` vào list

---

## 8. Rollback nếu cần

Để bật lại popup cũ:
- Mở CowPenPopupUI.cs / PigPenPopupUI.cs / ChickenPenPopupUI.cs
- Xóa 2 dòng `// [LEGACY...]` và `return;` trong hàm OpenPopup()
- Disable PenClickDetector trên prefab chuồng
