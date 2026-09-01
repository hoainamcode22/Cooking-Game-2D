# REPORT — PEN SUPPLY TRAY V2 (khay vật phẩm chuồng kiểu Hay Day)

## 1. Kiến trúc & thiết kế

```
PenMiniPanelUI.OpenPanel ──(toggle useSupplyTrayV2, mặc định BẬT)──► PenSupplyTrayV2.TryShow(pen)
                         └─(toggle TẮT / TryShow false)────────────► 2 UI cũ, nguyên trạng 100%

PenSupplyTrayV2_Host
 └─ PenSupplyTrayV2_Canvas  (ScreenSpaceOverlay, sortingOrder 800 — dưới ghost kéo 999/9999)
     ├─ Dim_Spotlight       RawImage full-screen, texture 192×N sinh runtime:
     │                      alpha 0 trong lỗ radial mềm quanh chuồng → 0.45 ở ngoài;
     │                      + PenSupplyTrayV2DimCatcher (chạm ra ngoài = đóng)
     └─ TrayRoot            panel bo góc 9-slice SDF sinh runtime, #2A2A30 a0.85
         ├─ Border          viền sáng nhẹ (sprite vành 3px, cùng 9-slice)
         ├─ Slot_Basket     [Ô 1] nền bo góc + Icon rỗ (config.basketIcon ?: productIcon)
         │                  + PenBasketDragItem  ← HANDLER CŨ NHÚNG NGUYÊN VẸN
         └─ Slot_Feed       [Ô 2] nền bo góc + Icon thức ăn + Badge góc dưới-phải
                            + LivestockFeedDragItem ← HANDLER CŨ NHÚNG NGUYÊN VẸN
```

**Không đổi process — cách bảo đảm:**
- Ô thức ăn: `AddComponent<LivestockFeedDragItem>` + `Setup(feedId, icon, tên, pen)` — đúng
  nghi thức `LivestockFeedPopupController.GetOrCreateItem` (:197-198) vẫn làm. Toàn bộ
  OnBeginDrag (check kho → hint hết đồ → FloatingDragIcon) / OnEndDrag → `TryDropOnPen`
  → `PenMiniPanelUI.TryFeed` chạy nguyên bản, không viết lại dòng nào.
- Ô rỗ: `AddComponent<PenBasketDragItem>` — ghost canvas riêng + `FindDropTarget` +
  `PenDropTarget.ReceiveBasketDrop` → `TryHarvest` nguyên bản. Chọn class này (thay vì
  PenBasketTrayController) vì khi thả trượt nó KHÔNG tự Close — item "bay về khay",
  đúng UX video ref.
- Badge: bind `txtStock` (field private của LivestockFeedDragItem) vào TMP_Text badge
  bằng reflection → **chính `RefreshStock` cũ ghi số**, nguồn duy nhất
  `FarmInventoryManager.Instance.GetAmount(itemId)`; refresh theo đúng 2 event cũ
  (`OnInventoryChanged` + `OnWarehouseChanged`). Reflection hụt (field đổi tên sau này)
  → fallback tự ghi từ CÙNG nguồn, không bao giờ lệch số.
- Khoá input parity từng chế độ: Idle giữ `IsSeedPopupOpen=true` + `RegisterPopupOpen`
  (y hệt ShowLivestockFeedPopup + controller.OnEnable); Ready không giữ khoá nào
  (y hệt PenBasketTrayController). Hide trả khoá đúng như HideLivestockFeedPopup.
- Đóng khay khi trạng thái chuồng nhảy (`CurrentState != stateLúcMở`) — khớp nhịp cũ:
  TryFeed tự ClosePanel + mở PenProcessPopupUI; TryHarvest xong tray cũ cũng Close.

**Trạng thái 2 ô theo ngữ cảnh:**
| Trạng thái chuồng | Ô rỗ | Ô thức ăn |
|---|---|---|
| Idle (đói) | mờ 0.4, khoá raycast | sáng; kho = 0 thì mờ 0.45 nhưng VẪN kéo được → hint cũ "Chưa có..." |
| Ready (có sản phẩm) | sáng, kéo được | mờ 0.4, khoá raycast (TryFeed lúc Ready vốn trả false) |
| Processing | KHÔNG mở khay — TryShow trả false, flow PenProcessPopupUI cũ chạy | |

## 2. Tham số chỉnh được (Inspector trên host `PenSupplyTrayV2_Host`)

| Tham số | Mặc định | Ý nghĩa |
|---|---|---|
| cellHeightRatio / cellMinPx / cellMaxPx | 0.13 / 110 / 190 | cạnh ô = clamp(Screen.height×ratio, min, max) px |
| iconFillRatio | 0.82 | icon chiếm % cạnh ô |
| panelColor | #2A2A30 a0.85 | màu than mờ của panel |
| borderColor | trắng a0.30 | viền sáng nhẹ |
| slotColor | #3C3C46 a0.92 | nền từng ô |
| dimAlpha | 0.45 | độ tối nền game |
| spotlightRadiusScale | 2.3 | bán kính vùng sáng = hệ số × cạnh ô |
| spotlightInnerRatio | 0.45 | trong bán kính×tỉ lệ này sáng hoàn toàn (lỗ mềm) |
| popDuration / popStartScale / popOvershoot | 0.18 / 0.8 / 0.10 | pop-in ease-out-back (FxEase, coroutine thuần) |
| sortingOrder | 800 | canvas khay — dưới ghost kéo rỗ (999) & ghost tray cũ (9999) |

## 3. CẦN SẾP LÀM TRONG UNITY

1. **Copy 4 file code** theo MANIFEST.md (PenMiniPanelUI.cs GHI ĐÈ, 3 file còn lại là MỚI).
2. Mở scene nông trại → menu **Tools → Farm Game → Chuồng → ★ Setup Khay V2 (1 nút)**.
   Tool tạo host + bật toggle mọi chuồng, idempotent, có Undo, KHÔNG auto-save —
   ưng thì tự Ctrl+S.
3. **Chạy thử 8 kịch bản** ở mục 4 (đặc biệt: kéo thức ăn/rỗ vào chuồng, badge khi xay
   cám xong, tutorial bước L2_08_FeedPen).
4. Kiểm tra **FloatingDragIcon**: nếu icon kéo thức ăn bị chìm dưới khay/dim → hạ
   `sortingOrder` của host xuống dưới canvas chứa FloatingDragIcon (khay đã tự nhún
   alpha khi kéo nên thường không thấy vấn đề, nhưng nên xác nhận bằng mắt).
5. Nếu tutorial có bước trỏ tay vào popup thức ăn cũ (highlight theo RectTransform của
   LivestockFeedPopup): trong lúc tutorial chạy có thể tạm TẮT toggle
   `useSupplyTrayV2` trên chuồng tutorial — tắt là về nguyên trạng 100%.
6. Ghi chú thiết kế: khay V2 hiển thị MỘT loại thức ăn (đúng luật slot1 world-panel cũ:
   `food1ItemId` ?: `premiumFoodItemId`). Chuồng nào cần chọn giữa nông sản thô và túi
   cám trong cùng khay → báo tôi mở rộng ô 2 thành 2 ô con (vẫn nhúng handler cũ).

## 4. SANDBOX TEST — VÒNG 1: mô phỏng 8 kịch bản (đọc lại toàn bộ code sau khi viết)

| # | Kịch bản | Đường chạy mô phỏng | Kết quả |
|---|---|---|---|
| 1 | Mở khay khi chuồng đói | OpenPanel Idle → TryShow → ô thức ăn sáng, ô rỗ mờ+khoá; giữ IsSeedPopupOpen+RegisterPopupOpen (parity feed popup); hint cũ; NotifyOpenPen giữ nguyên thứ tự | ✔ |
| 2 | Chuồng có sản phẩm | OpenPanel Ready → ô rỗ sáng, ô thức ăn mờ+khoá; không giữ khoá (parity tray cũ); kéo rỗ → ReceiveBasketDrop → TryHarvest → state Idle → Update phát hiện → Hide (khớp Close cũ) | ✔ |
| 3 | Kho hết thức ăn | Setup→RefreshStock cũ ghi badge "0" đỏ; ô mờ 0.45 nhưng vẫn nhận drag → OnBeginDrag cũ chặn + hint "Chưa có ... Máy Xay" | ✔ |
| 4 | Kéo thả trúng chuồng | OnEndDrag cũ → TryDropOnPen → TryFeed → RemoveItem/Mission/VFX/Save y nguyên → TryFeed gọi ClosePanel → [V2 ADD] HideIfShowing đóng khay ĐỒNG BỘ trước khi PenProcessPopupUI mở | ✔ |
| 5 | Thả trượt ra ngoài | Thức ăn: handler cũ gọi HideLivestockFeedPopup (hạ cờ seed) → khay V2 còn mở, Update dựng lại cờ, item về khay. Rỗ: PenBasketDragItem không Close, reset anchoredPos → về khay | ✔ |
| 6 | Mở khay chuồng khác khi đang mở | Detector không bị chặn → OpenPanel(B) → TryShow → Show(B): trả khoá A, rebuild slot theo B, re-pop; guard 0.08s chống dim-press cùng frame nuốt khay mới. Nếu PopupManager chặn (như từng chặn feed popup cũ) → hành vi = cũ | ✔ |
| 7 | Đóng bằng chạm ra ngoài | DimCatcher.OnPointerDown → bỏ qua khi đang kéo / <0.08s sau mở → Hide + trả khoá. Không Suppress click → chạm thẳng chuồng khác vẫn mở tiếp (giống feed popup cũ) | ✔ |
| 8 | Xoay màn hình / đổi resolution | Update thấy Screen.width/height đổi (và không đang kéo) → re-Show trọn gói: cell = clamp(h×0.13,110,190) tính lại, slot dựng lại (handler mới cache đúng vị trí), dim regen đúng aspect | ✔ |

## 5. VÒNG 2: soát compile-risk từng dòng

- **API Unity 6.3**: chỉ dùng API đã có mặt trong codebase (`FindFirstObjectByType` +
  `FindObjectsInactive.Include`, `FindObjectsByType`, TMP, RawImage, SetPixels32,
  Sprite.Create + border 9-slice — cùng pattern `GetRoundSprite` của PenMiniPanelUI).
- **Null-guard mọi tham chiếu ngoài**: Camera.main, FarmUIManager.Instance,
  FarmInventoryManager.Instance, WarehouseManager.Instance, MarketPriceTable (try/catch),
  _pen, _trayRoot, _dim, font (null thì TMP dùng font mặc định).
- **Không đổi chữ ký public cũ**: PenMiniPanelUI chỉ THÊM 1 field private + các dòng guard;
  không sửa/xoá method nào. Handler cũ không bị sửa file — field private bind qua
  reflection có fallback (badge tự ghi từ cùng nguồn; ghost rỗ thiếu sprite chỉ là
  cosmetic, chức năng thả vẫn chạy).
- **4 lỗi tự bắt được và đã sửa trong vòng này**:
  1. Guard trong `LamMoiBadge` chặn nhầm chính lượt gọi trong `Show()` (trước khi
     `_hienThi` bật) → đổi guard sang `_pen == null`.
  2. Host bị đặt inactive trong scene → `StartCoroutine` sẽ ném exception → thêm
     tự-kích-hoạt, không được thì `TryShow` trả false (rơi về UI cũ, không crash).
  3. `_dangKeoTruoc` dính trạng thái cũ khi re-show → reset trong Show.
  4. (Tool) chuỗi nội suy lồng `\"` trong hole — hợp lệ C# nhưng khó đọc/soát → tách
     biến `dongHost`.
- **Cân bằng ngoặc bằng python** (bóc string/comment rồi đếm): cả 3 file
  `{} () []` đều khớp — PenSupplyTrayV2.cs 71/71 · 345/345 · 31/31;
  PenMiniPanelUI.cs 105/105 · 495/495 · 40/40; SetupTool 6/6 · 23/23 · 2/2.

## 6. VÒNG 3: diff bản FULL vs bản gốc staged

```
PenMiniPanelUI.cs:  dòng XOÁ logic = 0 · dòng THÊM = 24
  (đã khôi phục CRLF dòng cuối cho khớp bản gốc — diff thuần additive)
  + 15 dòng: khối comment + [Header] + [Tooltip] + field useSupplyTrayV2 (+1 dòng trống)
  +  2 dòng: nhánh Ready (comment + if TryShow return)
  +  6 dòng: nhánh Idle (comment + if TryShow { NotifyOpenPen; return; })
  +  1 dòng: ClosePanel → PenSupplyTrayV2.HideIfShowing()
  Mọi dòng thêm đều nằm trong khối đánh dấu // [V2 ADD]; tắt toggle = bỏ qua toàn bộ.
```

## 7. Tóm tắt

- **Nối vào**: duy nhất `PenMiniPanelUI.OpenPanel` (2 nhánh Idle/Ready) + 1 dòng
  `ClosePanel` — toggle `useSupplyTrayV2` tắt là về nguyên trạng 100%.
- **File**: 4 file code (1 sửa additive, 3 mới) + 3 tài liệu.
- **Kết quả test**: Vòng 1 — 8/8 kịch bản pass trên mô phỏng; Vòng 2 — 4 lỗi tự bắt
  và sửa, ngoặc cân, không đổi chữ ký public; Vòng 3 — diff 0 xoá / 24 thêm.
- **Chưa thể kiểm trong sandbox** (cần Unity thật): thứ tự render FloatingDragIcon vs
  canvas 800 (mục 3.4), tutorial highlight popup cũ (mục 3.5), và việc PopupManager
  có chặn click chuồng B khi khay đang mở hay không (hành vi nào cũng = parity cũ).
