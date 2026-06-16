# 🐔 PHASE 2 — Shop Reorg + Tutorial Chăn Nuôi (L2) + Sorting

> **Bộ não chung:** đọc `TEAM_BRIEF_TASKBOARD.md` trước (tầm nhìn phụ nữ+trẻ em, 4 trụ cột:
> juicy/nhiều animation, build liên tục, dễ-thân thiện, phản hồi ngắn). File này là phần việc
> Phase 2, chia cho các agent theo skill. Chủ sở hữu: `producer`.
> **Engine:** Unity 6.3 LTS · C# · URP. **Gate:** Phase 2 chỉ mở sau khi tutorial L1 test OK.

## Hệ thống hiện có (để khỏi viết lại từ đầu)
- Shop: `Assets/_Game/Farm/Scripts/Shop/ShopManager.cs` — 3 list theo tab: `seedList` (0 Hạt giống),
  `buildingList` (1 Công trình), `decorList` (2 Trang trí); `ShowTab(int)` đổ UI theo thứ tự list.
  Item là `BaseItemData` có `unlockLevel`.
- Khoá item: `Assets/_Game/Farm/Scripts/Shop/ShopLevelLockUI.cs` — `Refresh(BaseItemData)` bật/tắt
  `lockOverlayRoot` theo `unlockLevel` vs level người chơi.
- Chuồng: `Assets/_Game/Farm/Scripts/Animal/MiniPanel/PenMiniPanelUI.cs` — state `Idle→Processing→Ready`;
  `OpenPanel()`, `TryFeed(foodItemId, vfxPos)` (→ Processing), `TryHarvest(vfxPos)` (→ product meat +
  secondProduct egg + `SpawnExpFly(expReward)`); `progressOverlay`, `config.feedDurationSeconds`.
  Phụ trợ: `PenClickDetector`, `PenDropTarget`, `DraggableFeedItem`, `PenBasketDragItem`.
- Tutorial (ĐÃ có sẵn, tái dùng): `TutorialManager` + `TutorialActionHandGuide.GuideSweepPlots(ids, needReady)`
  (tay quét ô CÒN VIỆC, bám theo user), `TutorialRuntimeTargetResolver` (proxy + `EnableAreaMask(kind, dim)`),
  `UnmaskRaycastFilter.SetScreenRect` (nền xám), `TutorialCameraFocus.CinematicFocus` (zoom 1-chủ).
  Notify hooks hiện có: `NotifyBuyItem`, `NotifyAllPlotsPlanted`, `NotifyOpenCropProcess`, `NotifySpeedUp`,
  `NotifyHarvest`, `NotifySeedPanelOpened`… (xem `TutorialManager`).

---

## EPIC A — Sắp xếp lại Shop + khoá Trang trí
| ID | Task | Owner | Hỗ trợ | Prio | Status |
|----|------|-------|--------|------|--------|
| A1 | Sắp xếp item mỗi tab theo `unlockLevel` tăng dần (mở trước hiện trước); cùng level giữ thứ tự gốc (stable) | `gameplay-programmer` | `unity-ui-specialist` | P1 | ✅ DONE (ShopManager.OnSearchTextChanged + OrderBy) |
| A2 | Tab **Trang trí** thêm `ShopLevelLockUI` + `lockOverlayRoot` (ảnh khoá) cho từng item, gọi `Refresh()` khi đổ UI | `unity-ui-specialist` | `ui-programmer` | P1 | ✅ NỀN xong — chạy menu *Setup Shop Lock Overlay*. Bạn gắn: sprite khoá + set `unlockLevel>1` cho asset trang trí |
| A3 | Xác nhận `unlockLevel` của mọi item 3 tab hợp lý L1→L30 (đã audit kinh tế, chỉ rà gating shop) | `economy-designer` | `game-designer` | P2 | TODO |
| A4 | Overlay "Mở ở cấp X" rõ ràng, nút bị khoá không bấm được | `ui-programmer` | `ux-designer` | P2 | TODO |

**Cách làm A1 (gợi ý):** trong `ShopManager.ShowTab()`, trước khi đổ UI, sort 1 bản copy của list theo
`unlockLevel` (đọc qua reflection như `ShopLevelLockUI` đang làm, hoặc thêm getter `int UnlockLevel` vào
`BaseItemData`). Dùng `OrderBy` ổn định, KHÔNG sửa list gốc serialized.
**Acceptance:** mở từng tab → item cấp thấp nằm trên; item khoá có ảnh khoá + chữ "Mở ở cấp X" ở cả 3 tab.

---

## EPIC B — Tutorial Chăn Nuôi (Level 2) — nối tiếp sau lúa & hoa
Tái dùng hệ thống guide thông minh đã có (mask + tay bám theo user + camera cinematic).
**Cần thêm Notify hooks mới** (gameplay-programmer): `WaitForOpenShop`, `WaitForBuyCornx8`,
`WaitForCloseShop`, `WaitForOpenPen`, `WaitForFeed`, `WaitForPenSpeedUp`, `WaitForPenHarvest`
(map tương tự các WaitFor… hiện có; bắn từ `ShopManager`/`PenMiniPanelUI`).

### Kịch bản (đã chuẩn hoá từ mô tả của bạn) — tạo các step asset L2_xx:
| Step | Nội dung | Hand / Mask / Camera | Chờ (waitAction) |
|------|----------|----------------------|------------------|
| B1 | Sau khi xong lúa+hoa → tay chỉ **Btn_Home** (ngôi nhà) | tay pulse ở Btn_Home | user click Btn_Home |
| B2 | Tay di tới **Btn_Store** | tay di chuyển Btn_Home→Btn_Store | user click Btn_Store (shop mở) |
| B3 | Shop mở → **popup thông báo** "Mở khoá hạt Ngô/Bắp!" | — | user đóng popup |
| B4 | Tay chỉ **item Ngô**, **nền xám bao quanh item Ngô**; hướng dẫn bấm **＋** đủ **8 hạt** | tay pulse ở nút ＋, mask quanh item ngô | đủ 8 ngô trong giỏ |
| B5 | Tay chỉ nút **Mua** | tay pulse nút Mua | user mua (NotifyBuyItem) |
| B6 | Tay chỉ **Btn_Close** | tay pulse Btn_Close | user đóng shop |
| B7 | Tay chỉ **8 ô đất** trồng ngô (tái dùng `GuideSweepPlots` plant) | mask 8 ô + tay quét ô trống | trồng đủ 8 ô (NotifyAllPlotsPlanted) |
| B8 | Hội thoại ngắn: *"Bạn đã làm tốt lắm!"* → *"Trồng xong rồi, giờ tới chăn nuôi gia súc nhé…"* | — | auto / click |
| B9 | **Zoom tới Pen_03**, tay chỉ **chính giữa chuồng** | `CinematicFocus(pen_03)` + tay pulse giữa pen | user click pen → mở panel thức ăn |
| B10 | Popup giống "bước 1 ruộng" → **kéo thức ăn (lúa) vào chuồng** | tay drag-guide (giống đất/hoa) | `TryFeed` thành công → Processing |
| B11 | User bấm **"Đã rõ"** → process chạy | — | — |
| B12 | Step 2: click chuồng mở process → tay chỉ **nút kim cương** hoàn tất | tay pulse nút gem | speed-up (NotifyPenSpeedUp) |
| B13 | Step 3: popup → **cầm rổ kéo vào chuồng thu hoạch**; tay **bám theo user** kéo (giống đất/hoa) | tay drag-guide bám theo | `TryHarvest` → meat/egg/EXP |

| ID | Task | Owner | Hỗ trợ | Prio | Status |
|----|------|-------|--------|------|--------|
| B-script | Viết kịch bản + thoại (B8) hợp đối tượng trẻ em | `game-designer` | `writer` | P1 | TODO |
| B-steps | Tạo step asset L2_01..L2_13 (giống thư mục `Resources/TutorialSteps/L1_L2`) | `gameplay-programmer` | `game-designer` | P1 | TODO |
| B-hooks | Thêm Notify hooks mới + bắn từ ShopManager/PenMiniPanelUI | `gameplay-programmer` | `lead-programmer` | P1 | TODO |
| B-manager (B1–B7) | Nhánh L2_01/03/04/05 + hooks shop + resolver item Ngô + generator `SetupTutorialL2Tool` | `gameplay-programmer` | `unity-ui-specialist` | P1 | ✅ DONE — chạy menu *Tools/Farm Game/Setup Tutorial L2 (Shop + Corn)* rồi Play |
| B-manager (B8–B13) | Gem speed-up (TrySpeedUpGem) + FocusOnPen + hooks + nhánh L2_07–10 + resolver pen/feed/basket + generator 10 step | `gameplay-programmer` | `unity-ui-specialist` | P1 | ✅ DONE (code). Cần Editor: chạy generator + thêm nút `btn_PenGem` OnClick→TrySpeedUpGem + tên chuồng `Pen_03` |
| B-pen-proxy | Proxy cho Btn_Home/Btn_Store/item Ngô/Pen_03/nút gem/rổ (cho tay chỉ + mask) | `unity-ui-specialist` | `gameplay-programmer` | P1 | TODO |
| B-vfx | Hiệu ứng juicy khi cho ăn / thu hoạch (rắc, lấp lánh) | `technical-artist` | — | P2 | TODO |

**Acceptance EPIC B:** chạy liền mạch lúa→hoa→shop→mua ngô→trồng→chuồng gà; tay luôn đúng vị trí &
bám theo user; mỗi bước chờ user làm xong mới qua (giống logic L1 đã chốt).

---

## EPIC C — Sorting (thứ tự hiển thị trong/quanh chuồng)
| ID | Task | Owner | Hỗ trợ | Prio | Status |
|----|------|-------|--------|------|--------|
| C1 | **Process/overlay tiến trình** đặt sorting **THẤP HƠN** các vật phẩm trong chuồng | `ui-programmer` | `technical-artist` | P1 | ✅ NỀN xong (PenMiniPanelUI: SetAsFirstSibling + `processOverlayCanvas`/`processSortingOrder`). Bạn gán Canvas ref nếu cần ép sorting |
| C2 | **Thịt / Trứng / EXP** sorting **CAO NHẤT** (nổi trên cùng), spawn **kéo lên cao**, KHÔNG nằm sát panel thức ăn | `technical-artist` | `ui-programmer` | P1 | 🔶 PHẦN spawn-up XONG (PenMiniPanelUI.harvestSpawnUpOffset); phần sortingOrder render còn TODO |

**Cách làm (gợi ý):** trong `PenMiniPanelUI`/`HarvestFeedbackSpawner`, đặt `sortingOrder`:
process < vật phẩm chuồng < (thịt/trứng/EXP). Điểm spawn của meat/egg/EXP dịch lên trên (offset Y dương)
để tách khỏi panel thức ăn.
**Acceptance:** khi Processing, thanh process nằm dưới con vật/vật phẩm; khi thu hoạch, thịt/trứng/EXP
bay nổi trên cùng, ở phía trên, không đè panel thức ăn.

---

## PROMPT SẴN CHO AGENT (copy-paste)

### ▶ gameplay-programmer (B-steps, B-hooks, B-manager, A1)
```
Đọc TEAM_BRIEF_TASKBOARD.md + PHASE2_SHOP_ANIMAL_TUTORIAL.md. Tái dùng hệ thống tutorial đã có
(GuideSweepPlots smart, EnableAreaMask, CinematicFocus). Làm EPIC B: thêm Notify hooks mới, tạo step
asset L2_01..L2_13 theo bảng kịch bản, thêm nhánh xử lý trong TutorialManager. Và A1: sort shop theo
unlockLevel trong ShopManager.ShowTab (copy list, OrderBy ổn định, không sửa list gốc). KHÔNG commit.
```

### ▶ unity-ui-specialist (A2, B-pen-proxy)
```
Đọc 2 file brief. A2: thêm ShopLevelLockUI + ảnh khoá cho item tab Trang trí, gọi Refresh() khi đổ UI.
B-pen-proxy: tạo proxy world/UI cho Btn_Home, Btn_Store, item Ngô, Pen_03, nút gem, rổ để tay chỉ + mask.
```

### ▶ technical-artist + ui-programmer (EPIC C, B-vfx)
```
Đọc brief. EPIC C: đặt sortingOrder process < vật phẩm chuồng < thịt/trứng/EXP; dịch spawn meat/egg/EXP
lên cao tách khỏi panel thức ăn. B-vfx: hiệu ứng juicy khi cho ăn/thu hoạch.
```

### ▶ game-designer + writer (B-script, A3)
```
Đọc brief. Viết thoại B8 dễ thương cho trẻ em ("Bạn đã làm tốt lắm!" → "…giờ tới chăn nuôi nhé").
Rà unlockLevel item shop hợp lý L1→L30.
```

---

## ĐỀ XUẤT THỨ TỰ TRIỂN KHAI (giảm rủi ro)
1. **EPIC A + C** trước — nhỏ, độc lập, test nhanh (sort shop, khoá trang trí, sorting chuồng).
2. **EPIC B** sau — lớn nhất; làm theo từng cụm: (B1–B6 shop) → (B7 trồng ngô) → (B8 thoại) → (B9–B13 chuồng).
