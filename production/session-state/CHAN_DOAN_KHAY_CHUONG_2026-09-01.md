# CHẨN ĐOÁN — Hệ UI chuồng gia súc hiện tại & điểm nối Khay V2

> Mọi số dòng dẫn theo bản gốc trong `Assets/_Game/Farm/Scripts/...` (bản upload).

## 1. Sơ đồ flow HIỆN TẠI (ai mở gì, callback nào chạy)

```
[Người chơi bấm chuồng]
        │
PenClickDetector.Update → TryOpenPanel (PenClickDetector.cs:42,79)
   • chặn khi EditMode / PopupManager.IsAnyPopupOpen / IsDraggingSeed (:81-94)
   • OverlapPoint collider chuồng (:101-105)
   • Processing → PenProcessPopupUI.Open (:107-120)
   • IsPanelOpen? Close : miniPanel.OpenPanel() (:122-128)
        │
PenMiniPanelUI.OpenPanel (PenMiniPanelUI.cs:216-252)   ← ★ NGÃ BA TRẠNG THÁI — ĐIỂM NỐI V2
   ├─ Processing → PenProcessPopupUI.Open(this)            (:223-236)  — GIỮ NGUYÊN, V2 không đụng
   ├─ Ready      → FarmUIManager.ShowPenBasketTray(this)   (:239-243)  ← V2 chặn tại đây
   └─ Idle       → FarmUIManager.ShowLivestockFeedPopup(this)
                   + TutorialManager.NotifyOpenPen()       (:245-251)  ← V2 chặn tại đây
```

### Nhánh CHO ĂN (Idle) — 2 UI rời rạc, mảnh 1
```
FarmUIManager.ShowLivestockFeedPopup(pen)        (FarmUIManager.cs:407-449)
   • HideAllPopups (:411) · tìm/tự tạo LivestockFeedPopupController (:414-428)
   • controller.Open(pen) (:440) · FarmInputLock.IsSeedPopupOpen = true (:447)
        │
LivestockFeedPopupController                     (LivestockFeedPopupController.cs)
   • OnEnable: IsSeedPopupOpen=true + RegisterPopupOpen (:25-26)
               nghe FarmInventoryManager.OnInventoryChanged (:30-31)
               nghe WarehouseManager.OnWarehouseChanged (:36-37)      ← nguồn refresh badge
   • Open → PopulateFeedItems (:99-152): tạo item cho food1/food2/premiumFood,
     mỗi item = LivestockFeedDragItem.Setup(itemId, icon, name, pen) (:132,140,148)
   • Update: chạm ngoài popup → FarmUIManager.HideLivestockFeedPopup (:53-62)
        │
LivestockFeedDragItem  ← ★ HANDLER SỞ HỮU SPRITE BAO THỨC ĂN + SỐ LƯỢNG
   • RefreshStock: stock = FarmInventoryManager.Instance.GetAmount(_foodItemId)
     (LivestockFeedDragItem.cs:56-58)                                  ← ★ NGUỒN SỐ BADGE
   • OnBeginDrag: hết đồ → ShowHint "Chưa có ..." (:77-82);
     IsDraggingSeed=true (:85); FarmUIManager.ShowFloatingDragIcon(_foodSprite) (:89-92)
   • OnEndDrag → TryDropOnPen(screenPos) (:114)
        └→ _targetPen.TryFeed(_foodItemId, worldPos) (:139)  ← ★ CALLBACK CHO ĂN
           fallback quét Physics2D.OverlapPointAll → pen.TryFeed (:148-168)
           trượt hết → FarmUIManager.HideLivestockFeedPopup (:172)
        │
PenMiniPanelUI.TryFeed (PenMiniPanelUI.cs:294-341)
   • RemoveItem + MissionProgressTracker.FeedAnimal (:309-310)
   • SetState(Processing) + SaveState (:316-317) + coroutine đếm giờ (:320)
   • ClosePanel() (:324) rồi tự mở PenProcessPopupUI (:325-332)
   • TutorialManager.NotifyFeed (:339)
```

### Nhánh THU HOẠCH (Ready) — 2 UI rời rạc, mảnh 2
```
FarmUIManager.ShowPenBasketTray(pen)             (FarmUIManager.cs:461-501)
   • HideAllPopups (:465) · tìm/tự tạo PenBasketTrayController (:469-481)
   • controller.Open(pen) (:493) — KHÔNG giữ khoá input, KHÔNG set IsSeedPopupOpen
        │
PenBasketTrayController  ← ★ CONTROLLER SỞ HỮU SPRITE RỖ (basketIcon, Awake tự tìm :18-19)
   • OnBeginDrag: ghost canvas overlay 9999 + IsDraggingSeed=true (:39-67)
   • OnEndDrag → TryHarvestPen(screenPos) (:75-90)
        └→ _targetPen.TryHarvest(worldPos) (:110)  ← ★ CALLBACK THU HOẠCH
           fallback OverlapPointAll → pen/PenDropTarget → TryHarvest (:117-136)
           mọi nhánh kết thúc bằng Close() (:111,133,138) — kể cả thả trượt
        │
PenMiniPanelUI.TryHarvest (PenMiniPanelUI.cs:344-398)
   • check kho CanAddItem (:348-360) · SpawnHarvestFX + AddItem + Mission (:366-390)
   • SetState(Idle) + SaveState (:393-394) + TutorialManager.NotifyPenHarvest (:396)

Nhánh song song (KHÔNG đụng): PenBasketDragItem (world-space panel, PenBasketDragItem.cs)
   • OnEndDrag → FindDropTarget → PenDropTarget.ReceiveBasketDrop (:79-83)
   • PenDropTarget.ReceiveBasketDrop → miniPanel.TryHarvest (PenDropTarget.cs:29-34)
   • Thả trượt: KHÔNG close gì, item tự về chỗ (rectTransform reset :77) ← hành vi
     "bay về khay" mà V2 cần → V2 nhúng CHÍNH class này cho ô rỗ.
```

### Đóng panel
```
PenMiniPanelUI.ClosePanel (:263-270)
   • ReleasePopupInputBlock · HideLivestockFeedPopup · HidePenBasketTray
   • SuppressWorldClickForCurrentFrame (:269)                ← V2 thêm 1 dòng HideIfShowing
FarmUIManager.HideLivestockFeedPopup (:451-458): IsSeedPopupOpen=false, IsDraggingSeed=false
FarmUIManager.HidePenBasketTray (:503-507)
```

### Sprite/dữ liệu nằm ở đâu
| Thứ | Nguồn |
|---|---|
| Sprite rỗ | `PenMiniPanelConfig.basketIcon` (PenMiniPanelConfig.cs:59); PenBasketTrayController đọc qua Image `basketIcon` |
| Sprite bao thức ăn | `config.food1Icon` ?: `premiumFoodIcon` (PenMiniPanelUI.cs:547); fallback `Resources.Load("Icons/{id}")` (LivestockFeedPopupController.cs:266-271) |
| Item id thức ăn | `config.food1ItemId` ?: `premiumFoodItemId` (PenMiniPanelUI.cs:546, :275) |
| SỐ LƯỢNG badge | `FarmInventoryManager.Instance.GetAmount(itemId)` (LivestockFeedDragItem.cs:58) — refresh khi `OnInventoryChanged` / `OnWarehouseChanged` (LivestockFeedPopupController.cs:30-37) |
| Khoá input chế độ cho ăn | `IsSeedPopupOpen=true` (FarmUIManager.cs:447) + `RegisterPopupOpen` (LivestockFeedPopupController.cs:26,88) |

## 2. ĐIỂM NỐI V2 (đúng 1 file cũ bị sửa, additive)

```
PenMiniPanelUI.OpenPanel
   Ready:  if (useSupplyTrayV2 && PenSupplyTrayV2.TryShow(this)) return;   // [V2 ADD]
   Idle :  if (useSupplyTrayV2 && PenSupplyTrayV2.TryShow(this)) { NotifyOpenPen; return; } // [V2 ADD]
PenMiniPanelUI.ClosePanel
   PenSupplyTrayV2.HideIfShowing();                                        // [V2 ADD]
```
- `TryShow` trả **false** (thiếu Camera, Processing, host không bật nổi) → chạy tiếp
  đúng dòng cũ ngay bên dưới → **không bao giờ mất UI**.
- Khay V2 nhúng **nguyên vẹn** 2 handler cũ:
  - Ô thức ăn = `AddComponent<LivestockFeedDragItem>` + `Setup(...)` — đúng cách
    LivestockFeedPopupController vẫn làm (:197-198). Badge = chính `txtStock` của
    handler cũ (bind reflection) → số do `RefreshStock` cũ ghi, cùng nguồn `GetAmount`.
  - Ô rỗ = `AddComponent<PenBasketDragItem>` — ghost + `PenDropTarget.ReceiveBasketDrop`
    → `TryHarvest` nguyên bản; thả trượt item tự về khay (không close như tray cũ).
- Khoá input mô phỏng đúng từng chế độ: Idle giữ `IsSeedPopupOpen` + `RegisterPopupOpen`
  (parity feed popup), Ready không giữ gì (parity basket tray cũ).
```
