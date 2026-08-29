# HANDOFF — Dev C (ui-programmer) · Tourist Boat V2 (BOAT-002 §3.5 + §3.6)

Ngày: 2026-08-29 · Phạm vi: popup báo tàu · popup mua slot bến · hiệu ứng mở slot · rework bảng khóa V1 · Editor tool dựng UI.
**Bản 2 — đã sửa theo QA (`/home/user/work/qa/QA_REPORT_BOAT_V2.md`): B-4 · M-5 · M-6 · m-6 · m-7 · m-11 + bỏ reflection TutorialManager.** Xem §9.

---

## 1. File đã giao (5 file, đúng đường dẫn tương đối dự án)

| # | Đường dẫn | Loại | Nội dung |
|---|---|---|---|
| 1 | `Assets/_Game/Farm/Scripts/TouristBoat/UI/BoatAnnouncePopupUI.cs` | MỚI | Popup "Tàu số 0X sắp cập bến!" — dim 60% + card gỗ, scale-pop ease-out-back 0.25s + text fade, nút "Đã rõ" |
| 2 | `Assets/_Game/Farm/Scripts/TouristBoat/UI/DockPurchasePopupUI.cs` | MỚI | Popup mua slot bến — level + icon/giá vàng-gem, nút MUA disable + lý do đỏ, cập nhật live |
| 3 | `Assets/_Game/Farm/Scripts/TouristBoat/UI/DockUnlockCelebrationFX.cs` | MỚI | FX world-space mở slot — bảng khóa thu + 8-12 sao vàng + SFX `AudioManager.PlayBuySell()` |
| 4 | `Assets/_Game/Farm/Scripts/TouristBoat/TouristBoatUnlockFlow.cs` | SỬA (bản đầy đủ) | Giữ nguyên intro L10; bảng khóa dùng Sprite asset; tap bảng khóa → mở popup mua (không mua thẳng nữa) |
| 5 | `Assets/_Game/Farm/Editor/TouristBoatUIPopupSetupTool.cs` | MỚI | Menu `Tools/Farm Game/Tourist Boat/Setup Popups (UI)` — dựng + wire 2 popup, idempotent |
| 6 | `Assets/_Game/Farm/Scripts/TouristBoat/BoatDockSlot.cs` | **SỬA (bản 2 — lead duyệt sau QA M-6)** | Tap bảng khóa: bắt ở NHẢ chuột + ngưỡng kéo + guard `BlockMapPan`/kéo hạt/kéo liềm → mở popup mua; không có popup thì giữ nguyên đường V1 |

Ngoài 6 file trên không đụng gì. `BoatDockManager.cs`, `TouristBoatConfig.cs`, `FarmUIManager.cs`, `FarmInputLock.cs` giữ nguyên 100%.

**Compile-check (mcs, stub Unity của QA)** — 3 pass đều **0 error / 0 warning**: (1) devA + devC + source thật, `-define:UNITY_EDITOR`; (2) giả lập player build (bỏ `UNITY_EDITOR`, bỏ `Editor/`); (3) merge thật (thêm `TouristBoatSetupTool` + `TouristBoatDiagnosticTool` V1).

---

## 2. Khớp API contract

**Dev A (đã giao, không đổi chữ ký):**
- `event Action<int, DateTime, int> OnNextTripScheduled` — popup báo tàu subscribe đúng chữ ký này (dockIndex, arrivalUtc, phút chờ).
- `int BoatNumber(int dockIndex)` — dùng cho tiêu đề "Tàu số 0X" (fallback `dockIndex + 1` nếu manager null).
- `TouristBoatConfig Config { get; }` — popup mua đọc `GetDockRequirement(dockIndex)` cho level/giá; UnlockFlow đọc `lockPanelWidth/Height` cho cỡ bảng khóa. **Không hardcode số nào.**
- Unlock giữ API V1: `CanUnlockDock(int, out string)` · `TryUnlockDock(int)` · `UnlockDockFree(int)` · `IsDockUnlocked` · `IsIntroDone` · `MarkIntroDone` · `IsReady` · `GetDockBerth` · `OnDockUnlocked`. Không đổi gì.
- 12 field config mới của V2: **chỉ đọc, không sửa** (thực tế UI chỉ cần `lockPanel*`, `unlockLevel`, `dock2/3Level`, `dock2GoldCost`, `dock3GemCost`).

**Trừ tiền:** hoàn toàn qua `BoatDockManager.TryUnlockDock` → `FarmEconomyManager.SpendGold/SpendGems`. Popup KHÔNG tự trừ tiền, không tự set unlocked.

---

## 3. Tool tự làm gì (chạy 1 lần là xong phần wiring)

Menu `Tools/Farm Game/Tourist Boat/Setup Popups (UI)`:

1. Dùng **canvas RIÊNG** `Canvas_TouristBoatPopup` ở gốc scene (Overlay, `sortingOrder` 400, ScaleWithScreenSize 1920×1080); có sẵn thì tái dùng.
   **[QA B-4] Đây là thay đổi so với bản 1** — bản 1 đặt popup dưới `FarmUIManager.canvasPopupRoot`, mà `EnterCookingMode()` `SetActive(false)` canvas đó ⇒ Unity giết coroutine ⇒ hàng đợi thông báo chết vĩnh viễn. Nay component luôn sống để nghe event; việc "ở bếp thì không hiện" do chính popup lo (`DangTrongSceneBep()`), đúng nghĩa **HOÃN rồi HIỆN LẠI** của GDD §5 edge 6.
   Tool tự **chuyển** `TouristBoatPopups` của scene đã chạy bản 1 sang canvas riêng (giữ nguyên con + ref đã wire).
2. Dựng hierarchy đầy đủ (idempotent — chạy lại không nhân bản, không đè ref/sprite đã chỉnh tay):
   ```
   Canvas_Popup
   └─ TouristBoatPopups                (ACTIVE)
      ├─ BoatAnnouncePopup             (ACTIVE — component phải sống để nghe event)
      │  └─ Root                       (INACTIVE)
      │     ├─ Dim   (Image đen a=0.6, raycastTarget)
      │     └─ Card  (Image khung gỗ, 1100×620)
      │        └─ Content (CanvasGroup) → Title · Body · Btn_DaRo/Label
      └─ DockPurchasePopup             (ACTIVE)
         └─ Root                       (INACTIVE)
            ├─ Dim · Card (980×680)
            └─ Content → Title · LevelReq · CostRow(CostIcon+CostText) · Reason · Btn_Mua/Label · Btn_Close/Label
   ```
3. Wire **toàn bộ** SerializeField của 2 component (đã đối chiếu đúng tên field).
4. Wire ngược sang `TouristBoatUnlockFlow` trong scene: `purchasePopup` + `lockBoardSprite` (khung gỗ placeholder).
5. Tìm font TMP có thật trong project (ưu tiên font trong `Assets/`, không phải font mặc định TMP).
6. Tìm sprite khung gỗ theo tên: `khunggo` → `khung_go` → `WoodBoard_Frame` → `khung` → `wood/frame/board/panel`.
7. Tìm icon vàng/gem: **ưu tiên lấy đúng icon HUD** (Image anh em của `txtGold`/`txtGem` trong `FarmUIManager`), fallback dò tên asset.
8. Đặt 2 `Root` inactive, ping object, log từng thứ ra Console, và dialog cuối liệt kê **art đang dùng tạm**.
9. Mọi object tạo mới đều `Undo.RegisterCreatedObjectUndo` → Ctrl+Z gỡ sạch. Menu priority = **30** (QA m-11: A=12, B=20-22, C=30 — thứ tự menu cố định).

---

## 4. Việc Sếp làm trong Unity (5 bước)

1. Mở scene farm (`SCN_Farm`) → chạy `Tools/Farm Game/Tourist Boat/Setup Popups (UI)` → **Ctrl+S lưu scene** (tool chỉ sửa scene, không tạo prefab).
2. Đọc dialog cuối + Console: mục "ART ĐANG DÙNG TẠM" liệt kê chính xác sprite/font nào là placeholder.
3. Khi art khung gỗ xong: kéo sprite vào `Source Image` của **Card ở CẢ 2 popup** + field `Lock Board Sprite` của `BoatSystem/TouristBoatUnlockFlow`. Không cần sửa code.
4. Kiểm font tiếng Việt: bật tạm `Root` của popup báo tàu, nhìn dòng "Tàu số 01 sắp cập bến!" — thiếu dấu thì đổi `Font Asset` của các TMP sang font có dấu.
5. Play test:
   - Lv12 + ≥2.000 vàng → tap bảng khóa bến 2 → popup mua → MUA → popup đóng, sao vàng nổ, tàu xuất phát.
   - Thiếu tiền → nút MUA xám + dòng đỏ "Không đủ vàng"; nhận vàng trong lúc popup mở → nút tự sáng (live).
   - Tàu rời bến → popup "Tàu số 0X sẽ cập bến sau X phút!" hiện đúng 1 lần (5 phút khi 1 bến, 10 phút khi ≥2 bến — số lấy thẳng từ lịch của Dev A).
   - **Hồi quy QA B-4:** vào bếp → ra → đợi tàu rời bến: popup **vẫn hiện**. Đang mở popup mà vào bếp: popup tự đóng, ra bếp không bị khoá input, card không méo/mờ.

---

## 5. Sprite / font đang là PLACEHOLDER (chờ art)

| Thứ | Hiện tại | Thay thế thế nào |
|---|---|---|
| Khung gỗ card 2 popup | Sprite tìm được trong project theo tên; không có thì `UI/Skin/UISprite.psd` built-in (hộp xám bo góc) | Kéo art vào `Source Image` của Card (2 popup) |
| Bảng khóa bến world-space | Cùng sprite khung gỗ ở trên, tool wire vào `lockBoardSprite` | Đổi field `Lock Board Sprite` trên `TouristBoatUnlockFlow` |
| Icon ổ khóa | Placeholder tròn V1 do `TouristBoatSetupTool` sinh | Gán `Lock Icon Sprite` (tuỳ chọn) |
| Icon vàng / gem | Icon HUD thật nếu dò được; không thì `UI/Skin/Knob.psd` tròn | Kéo vào `Gold Icon Sprite` / `Gem Icon Sprite` |
| Sao vàng của FX | **Sprite procedural vẽ bằng code** (sao 4 cánh 32×32, cache static) — cố ý không cần asset | Không cần thay; muốn art riêng thì báo, tôi thêm field Sprite |
| Font TMP | Font TMP đầu tiên trong `Assets/` (không phải font mặc định TMP nếu có) | Đổi Font Asset trên các TMP |
| Nút | `UI/Skin/UISprite.psd` 9-slice + tint màu | Kéo art nút vào Image của Btn_* |

---

## 6. Quyết định thiết kế đáng chú ý

- **Không mua thẳng nữa (bản 2 — QA M-6):** sửa thẳng trong `BoatDockSlot`. `OnMouseDown` chỉ *ghi nhận* nhịp nhấn; hành động ở `OnMouseUpAsButton` với ngưỡng kéo 24px và guard `BlockMapPan / IsDraggingSeed / IsDraggingSickle / IsPopupOpen` — chạm tay rồi kéo bản đồ không còn mở popup nhầm. Có popup trong scene → `MoChoBen(dockIndex)`; **không có popup → giữ nguyên đường V1** (`TryUnlockDock` + floating text) để không ai mất đường mua bến. Guard m-1 (`dockIndex == 0 && !IsIntroDone`) giữ nguyên, kiểm cả lúc nhấn lẫn lúc nhả.
  Cơ chế tạm của bản 1 (tắt collider + tự bắn tia AABB trong `UnlockFlow.Update`) đã **xoá sạch** — hết luôn rủi ro "ai gọi `RefreshLockUI()` là đường mua thẳng sống lại".
- **FX không đánh nhau:** khi bến có `BoatDockSlot` (nó tự chạy punch+thu bảng trong `UnlockFxRoutine`), `HandleDockUnlocked` truyền `bangKhoaRoot = null` → FX chỉ bắn sao + SFX. Chỉ scene dựng tay (không có slot) thì FX mới tự thu bảng.
- **Chống báo trùng:** 1 PlayerPrefs key/bến (`TouristBoat_DaBaoChuyen_{dock}`, value = arrival ticks) — không phình prefs theo số chuyến. Đánh dấu "đã báo" NGAY LÚC HIỆN, không đợi bấm nút (crash giữa chừng cũng không báo lại).
- **Hàng đợi popup báo tàu:** mở 3 bến cùng lúc → 3 event → hiện lần lượt, không chồng. Chờ tới lượt mà chuyến còn < 1 phút → bỏ (đánh dấu đã báo) đúng luật §3.5.
- **Điều kiện hoãn popup:** tutorial đang chạy · `FarmInputLock.IsPopupOpen` / `PopupManager.IsAnyPopupOpen()` · scene bếp đang load. Poll 0.25s bằng `WaitForSecondsRealtime`.
- **Tutorial:** dùng API thật, **không còn reflection** — `TutorialManager.Instance != null && !TutorialManager.IsTutorialDone` (quy ước chung, giống `MissionHudButtonUI.cs:131`). Một hàm duy nhất `BoatAnnouncePopupUI.TutorialDangChay()`, popup mua và UnlockFlow đều gọi nó.
- **Số phút trong popup là số THẬT của lịch:** ưu tiên hỏi lại `BoatDockManager.GetMinutesToNextArrival(dock)` (đúng thang `debugTimeScale` của Dev A) khi `TryGetNextArrivalUtc` vẫn trả đúng chuyến đang chờ báo; chỉ khi lịch đã đổi mới dùng số phút kèm event. Phép trừ UTC thô của bản 1 đã bỏ.
- **Tự dựng lại hàng đợi lúc vào game (phòng QA m-3):** sau khi subscribe, quét cả 3 bến bằng `TryGetNextArrivalUtc` → chuyến nào chưa ghi "đã báo" thì enqueue. Không phụ thuộc việc event của Dev A có bắn kịp trước lúc mình subscribe hay không.
- **Sống sót qua `SetActive(false)` (QA B-4 + M-5):** cả 2 popup có `OnDisable` trả `FarmInputLock`, hạ cờ, ẩn `popupRoot`, reset `localScale`/`alpha`; `OnEnable` của popup báo tàu tự chạy lại vòng rút nếu hàng đợi còn hàng. Hàng đợi **không bị xoá** khi tắt — đúng luật "hoãn rồi hiện lại". Vào bếp lúc popup đang mở → popup tự đóng (poll 0.5s), không đè lên scene bếp, không khoá input.
- **Input lock:** `RegisterPopupOpen/RegisterPopupClose` + `SetPopupRaycastBlock` — đúng pattern; `RegisterPopupClose` tự `SuppressWorldClickForCurrentFrame()` nên tap "Đã rõ" không lọt xuống world. `OnDestroy` trả lock nếu popup đang mở (không lệch `popupLockCount`).
- **Tween:** coroutine ease tự viết (`EaseOutBack` cho pop, sin nửa chu kỳ cho punch) chạy `Time.unscaledDeltaTime` — không dùng tween library ngoài, đúng codebase.
- **Định dạng số (QA m-6):** bỏ `CultureInfo.GetCultureInfo("vi-VN")` ở **cả** `DockPurchasePopupUI` lẫn `BoatDockSlot` — tự dựng `NumberFormatInfo` (`NumberGroupSeparator = "."`) để build bật Invariant Globalization (IL2CPP mobile) không ném `CultureNotFoundException`.
- **Sprite procedural (QA m-7):** texture + sprite sao đặt `HideFlags.HideAndDontSave` — không để rác qua mỗi lần Play trong Editor.
- **Text tiếng Việt CÓ DẤU, không emoji** (tiếp tục quyết định lead sau QA V1: font TMP dự án có thể thiếu glyph emoji).

---

## 7. Rủi ro / cần lead chốt

1. ~~TutorialManager reflection~~ → **ĐÃ XONG**, dùng API thật.
2. ~~Tắt collider tap~~ → **ĐÃ XONG**, sửa thẳng `BoatDockSlot` (lead duyệt).
3. ~~Bắt tap bằng AABB~~ → **ĐÃ XONG**, dùng collider sẵn có của bến qua `OnMouseUpAsButton`. *Còn lại:* vùng tap vẫn là `BoxCollider2D` của Dock (tool V1 sinh, offset 0,170 · size 360×180) — **có thể lệch so với bảng khóa mới** nếu Sếp đổi `lockPanelWidth/Height`. Cần thì tôi thêm 1 menu canh collider theo cỡ bảng.
4. **Không tìm được sprite khung gỗ trong drop source** (`find` cho `khunggo`/`khung`/`WoodBoard` ra 0 kết quả — drop chỉ có script + NVGAME sheet). Tool sẽ dò trong project THẬT của Sếp; nếu vẫn không có, Card dùng hộp xám built-in — vẫn chạy, chỉ xấu.
5. **Không có TMP font asset nào trong drop** → chưa xác nhận được font tiếng Việt. Nếu project chưa Import TMP Essentials, chữ có dấu có thể mất dấu. Tool đã log cảnh báo + hướng dẫn.
6. **Bến tốn CẢ vàng lẫn gem** (config tương lai): popup hiện chỉ hiển thị 1 loại (ưu tiên vàng). Hiện `GetDockRequirement` không sinh case này nên chưa phải xử lý — nếu V3 có thì báo tôi thêm hàng giá thứ 2.
7. **Tên scene bếp là dữ liệu, không phải hằng số:** cả 2 popup dùng field `cookingSceneName` (default `"SampleScene"`, copy từ `FarmUIManager`). **Nếu Sếp đổi tên scene bếp thì phải sửa ở 3 chỗ** (FarmUIManager + 2 popup) — muốn gọn thì lead mở 1 property public trên `FarmUIManager` để tôi đọc chung.
8. **Popup boat nằm trên canvas riêng** (order 400) nên **không** được `FarmUIManager.ForceCloseAllPopups()` / `HideAllPopups()` quét như popup khác; popup tự lo đóng khi vào bếp/bị tắt. Nếu lead muốn nó chịu quản lý chung, thêm nó vào `popupObjectsToForceClose` — nhưng **đừng** đưa lại vào `canvasPopupRoot` (tái phát B-4).

## 8. Câu hỏi cho lead

- ~~Tên property TutorialManager~~ · ~~cho sửa `BoatDockSlot`~~ → đã có câu trả lời, đã làm.
- Vùng tap của bảng khóa (`BoxCollider2D` trên Dock) có cần tool canh tự động theo `lockPanelWidth/Height` không? (rủi ro #3)
- Tên chính xác của sprite khung gỗ + icon vàng/gem trong project thật, để tôi pin cứng vào tool thay vì dò theo từ khóa?
- Màu vàng HUD chính xác: tôi đang dùng `#FFD34D` theo GDD §3.6 — nếu HUD dùng mã khác, cho tôi mã để đồng bộ.

---

## 9. Nhật ký sửa theo QA (bản 2)

| Mã | Mức | Đã sửa thế nào | File |
|---|---|---|---|
| **B-4** | 🔴 | Tool đổi sang **canvas riêng** `Canvas_TouristBoatPopup` (không bị `EnterCookingMode` tắt) + tự di dời popup của scene đã chạy bản 1. Popup thêm `OnEnable` (chạy lại vòng rút nếu hàng đợi còn hàng) và `OnDisable` (xoá `_drainRoutine` chết, trả lock, reset visual, **giữ hàng đợi**). Vào bếp lúc đang mở → tự đóng (poll 0.5s) | `TouristBoatUIPopupSetupTool.cs` · `BoatAnnouncePopupUI.cs` |
| **M-5** | 🟠 | `OnDisable` đối xứng `OnDestroy` cho **cả 2 popup**: `FarmInputLock.RegisterPopupClose()`, hạ cờ, ẩn `popupRoot`, `cardRect.localScale = 1`, `contentGroup.alpha = 1`, `dim.alpha` về chuẩn. Không còn dựa vào `FarmInputLock.ResetAll()` của `sceneLoaded` | `BoatAnnouncePopupUI.cs` · `DockPurchasePopupUI.cs` |
| **M-6** | 🟠 | Sửa thẳng `BoatDockSlot` (lead duyệt): `OnMouseDown` chỉ ghi nhận nhịp nhấn + guard `BlockMapPan`/`IsDraggingSeed`/`IsDraggingSickle`/`IsPopupOpen`; hành động ở `OnMouseUpAsButton` với ngưỡng kéo 24px; có popup → `MoChoBen`, không có → **fallback V1 nguyên vẹn**; guard m-1 giữ và kiểm 2 lần. Xoá sạch cơ chế tắt-collider + bắn-tia AABB trong `UnlockFlow` | `BoatDockSlot.cs` · `TouristBoatUnlockFlow.cs` |
| **Tutorial** | — | Bỏ toàn bộ reflection → `TutorialManager.Instance != null && !TutorialManager.IsTutorialDone` trong `TutorialDangChay()` (1 hàm dùng chung) | `BoatAnnouncePopupUI.cs` |
| **Số phút** | — | Lấy từ `GetMinutesToNextArrival` (đúng thang `debugTimeScale`), fallback số phút của event; bỏ phép trừ UTC thô. Thêm quét lịch lúc vào game (phòng **m-3**) | `BoatAnnouncePopupUI.cs` |
| **m-6** | 🟡 | Bỏ `CultureInfo.GetCultureInfo("vi-VN")` ở **cả 2 chỗ** → `NumberFormatInfo` tự dựng | `DockPurchasePopupUI.cs` · `BoatDockSlot.cs` |
| **m-7** | 🟡 | `HideFlags.HideAndDontSave` cho texture + sprite sao procedural | `DockUnlockCelebrationFX.cs` |
| **m-11** | 🟡 | Menu priority 20 → **30** | `TouristBoatUIPopupSetupTool.cs` |

**Không thuộc Dev C (để lead điều phối):** B-1 · B-2 · B-3 · M-1 · M-2 · M-3 · M-4 (Dev B) · m-1 · m-2 · m-3 (Dev A / tool V1 / lead).

**Còn tồn phía Dev C:** vùng tap bảng khóa vẫn là `BoxCollider2D` cỡ cố định của tool V1 (rủi ro §7.3); tên scene bếp lặp ở 3 nơi (§7.7); art khung gỗ + icon vàng/gem vẫn placeholder (§5).
