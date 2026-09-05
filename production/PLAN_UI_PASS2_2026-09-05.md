# PLAN — UI PASS 2 · 2026-09-05 (tối) · Tech Lead

> Nguồn: SCAN 3 agent (gameplay / ui / unity-ui) trên bản code 18:45 + 9 ảnh F10 17:15–18:33 trong `Assets/_Debug_Capture/`.
> Backup gốc: `production/backup_ui_pass2_2026-09-05/` (+ `MD5SUMS.txt`). Hỏng → xoá file mới, copy ngược.
> Luật: không commit · không sửa tay .unity/.prefab/.asset (đi qua tool DRY-RUN → APPLY) · chỉ cộng thêm hoặc sửa lỗi có bằng chứng.

## A. Nguyên nhân gốc đã xác nhận (file:dòng)

| # | Sếp báo | Nguyên nhân gốc | Mức |
|---|---|---|---|
| A1 | Sau thu hoạch không qua bước hoa, tay đứng im | **Deadlock**: `TutorialManager.NotifyAction:503-508` — action tới đúng lúc popup Lên Cấp đang mở (`PopupManager.IsAnyPopupOpen` gồm `LevelUpPopupUI.IsActive` :96) → cất vào hàng đợi, **nhưng không ai tiêu thụ** khi popup đóng (chỉ `PlayStep` đầu bước sau mới tiêu thụ, mà bước sau không bao giờ tới). Thu hoạch ô đầu → +EXP → lên cấp 2 → popup → `NotifyHarvest` bị hoãn vĩnh viễn ⇒ kẹt ở 09. Ảnh 171917 khớp 100%. | 🔴 |
| A2 | Bấm ĐÃ RÕ không thấy bàn tay ảo ảnh | (1) Phantom hand **không có sprite** (`TutorialPhantomDemoManager.cs:115-119` lấy `Image` trên `Hand_Click_Plot` — Image nằm ở con `Hand_Image`) → ô trắng; `_defaultHandSprite/_riceSeedSprite` không được đọc. (2) Gọi phantom ở bước 03 khi khay hạt chưa mở → chờ mãi rồi bị `StopDemo` ở 04 (`TutorialManager.cs:1041,1063`). (3) Bảng 4 trang bị tắt cứng `if (false && …)` :1107 + tool đặt `showGuideBoard=false` mọi step → `ConfirmGuidePopup` chết. (4) Layer phantom là con `Canvas_Popup` — dễ bị tắt kèm popup. (5) Không có trình tự "ảo ảnh → tay thật": 3 tay cùng lúc. | 🔴 |
| A3 | Popup Lên Cấp: chữ đè nút "Bắt đầu nào" | `LevelUpPopupRewireTool.cs:364-366` đặt Text_MoKhoa y=215 / Text_Hint y=165 theo giả định nút neo đáy y=78 — SAI, nút neo tâm (-462) ⇒ đỉnh nút = 164 → đè. Dải `Dai_MoKhoa` -272 cao 250 chỉ cách nút 6 px. Ảnh 171917 đo được chữ ở 149/109 (bản cũ) — hoàn toàn trong nút. Ngoài ra `UnlockSlotUI.cs:148-150` caption y = -99 dưới đáy ô → bị RectMask cắt ⇒ **tên ô chưa bao giờ hiện**. 2 lớp dim chồng (`V3_DimBackground` 0.62 + `Bg_NenToi` 0.65) ⇒ lần tới tối 87%. | 🔴 |
| A4 | UI đè: khay hạt đè 4 nút HUD dưới-trái | `FarmUIManager.cs:364-368` reset `Popup_seed` về (0,0) → x 113–1806, y 0–240; nút HUD y 22–180 ⇒ 4 ô đầu nằm trên 4 nút (khay 300 vẽ trên HUD 100, nút vẫn lộ qua nền mờ — ảnh 171836). Sếp chạm "Lúa" có thể trúng KHO. | 🟠 |
| A5 | NPC che nút BẢNG TIN CHỢ/NẤU ĂN | `TutorialV2SetupTool.cs:110-111` NPC anchor (0,0) pos (420,-40) 300×375 → x 420–720 trùng 2 nút; Tutorial_Canvas 250 > HUD 100. | 🟠 |
| A6 | Toast "đủ hàng… giao đơn" chen giữa tutorial | `AnimalGuideController.cs:212-238` poll 5s, không kiểm `TutorialManager.DangChayTutorial`; toast y=165 trùng dải khay hạt. | 🟠 |
| A7 | Tay thật bị khay hạt / mini-panel che | `Tutorial_Hands` ở Tutorial_Canvas 250 < Canvas_Popup 300 (khay, Popupprocess, khay liềm đều là con Canvas_Popup). | 🟡 |

## B. Changeset (3 Dev song song, không đụng chung file)

### Dev A — gameplay-programmer · Tutorial flow + Phantom (files: `TutorialManager.cs`, `TutorialPhantomDemoManager.cs`, `Editor/SetupTutorialL1L2Tool.cs`)
1. **Deadlock A1** [sửa lỗi rõ ràng]: khi `NotifyAction` hoãn action vì popup → khởi động coroutine `ChoPopupDongRoiTieuThu()` : chờ `!TutorialGate.CoPopupDangMo()` + 0.25s → nếu vẫn `WaitingAction` và hàng đợi chứa `_pendingWait` → `ConsumeQueuedAction()`. Thêm cả khi `DayVaoHangDoi` từ nhánh TypingText/Transitioning (vô hại).
2. **Phantom 4 bước** [cộng thêm]:
   - Sprite: hand = `HandPointerRT.GetComponentInChildren<Image>(true)`; fallback `_defaultHandSprite`; sickle = `FarmUIManager.Instance.SickleTrayRect` Image con; gem = icon trong `CropProcessPopupUI.SpeedUpButtonRect`; seed = icon crop (đã có).
   - Layer: parent = `Tutorial_Canvas` (tìm tên), nested Canvas `overrideSorting=true, sortingOrder=450`, `raycastTarget=false`, `SetActive(true)+SetAsLastSibling()` mỗi lần Play.
   - Trình tự: `Play*(…, Action onDone)` → trong lúc demo **tạm ẩn tay thật** (CanvasGroup alpha 0 trên root `Tutorial_Hands` / `_handPointer` / drag hint), demo xong → hiện lại + gọi `onDone`. Người chơi chạm màn hình → `StopDemo` (đã có FadeOutQuick). Lặp lại demo nếu người chơi đứng im 8 s (tuỳ chọn, cờ Inspector `_lapLaiSauGiay=8`).
   - Gọi ở: 05 (kéo hạt→plot_01), 06 (kéo hạt→ô trống kế tiếp, lấy từ `LayODatLua()`), 08/07 (chạm ô → nút gem của mini-panel, dùng `CropProcessPopupUI.SpeedUpButtonRect`, không quét Button theo tên), 09 (chạm ô chín → khay liềm hiện → kéo liềm plot_01→plot_02), 10 (kéo liềm qua các ô còn chín), 13/14/15/16/17 tương tự cho hoa. **Bỏ** gọi ở 03.
   - `AdvanceToNextStep` → `StopDemo()` luôn.
3. **Bảng 4 trang** [phục hồi]: bỏ `false &&` :1107; fallback theo tên `LaBuocBangHuongDan(step.name)` cho 03/06b/08b/09b (không cần sửa asset). Sau ĐÃ RÕ → bước kế tiếp chạy phantom.
4. Tool `SetupTutorialL1L2Tool.cs:551` gán `_defaultHandSprite` (`Assets/_Game/Farm/Art/UI/tutorial_hand.png`) + `_sickleSprite` khi AddComponent (dự phòng).
5. Resume: bảng rewind `{05→04, 13→12}` trong `StartTutorial` (khay hạt đóng khi resume).

### Dev B — ui-programmer · Popup Lên Cấp (files: `LevelUpPopupUI.cs`, `UnlockSlotUI.cs`, `Editor/LevelUpPopupTownshipTool.cs`, `Editor/LevelUpPopupRewireTool.cs`)
1. Runtime `BoTriVungDuoi()` cuối `PopulateUI` [cộng thêm]: `Dai_MoKhoa` y -215 · `hintText` (0,-385) 1100×66 autosize 20–28, 2 dòng, Ellipsis, màu kem #FFF5DC outline nâu · `Btn_TiepTuc` y -500 · `unlockDescText` tắt hẳn · tắt `V3_DimBackground` nếu có · `Nen_Dai` màu kem (255,243,214,235).
2. `hintText.text` = `cfg.hintText` nếu có, không thì "Lên cấp {N}! Quà mới đã vào kho của bạn." 
3. `UnlockSlotUI.cs:148-150` caption `anchoredPosition=(0,-2)`, height 26 → tên hiện dưới ô.
4. Tool đồng bộ: Township :385 → -215, :452 → -500, :267 order 310; Rewire :365 bỏ Text_MoKhoa, :366 → (0,182) anchor (0.5,0.5) 1100×66; thêm bước tắt `V3_DimBackground`.
5. Bỏ Instantiate `vfxSidePrefab` khi `useUIFireworks` (CPU vô ích).

### Dev C — unity-ui-specialist · UI đè (files MỚI `HudNavHider.cs`, `Editor/TutorialHandLayerTool.cs`; sửa `SeedPopupController.cs`, `AnimalGuideController.cs`, `TutorialDialogueCard.cs`, `Editor/TutorialV2SetupTool.cs`)
1. `HudNavHider` static ref-count: tìm `BottomLeft_Nav_Group` (con Canvas_HUD) theo tên, thêm CanvasGroup nếu thiếu; `An(object)`/`Hien(object)` → alpha 0.35→0 tuỳ mode, `blocksRaycasts=false`. Không cần wire scene.
2. `SeedPopupController.OnEnable/OnDisable` → `HudNavHider.An/Hien(this)` (khay hạt + khay hoa).
3. `TutorialDialogueCard.Show/Hide` → mờ HUD 0.35 trừ khi `TutorialManager.Instance.CurrentStepName` nằm trong nhóm bước có target ở HUD (L2_01_GotoShop…).
4. `AnimalGuideController`: 3 vòng poll `continue` khi `DangChayTutorial`; `DrainToastQueue` hoãn khi tutorial chạy; toast y 165 → 320.
5. `TutorialV2SetupTool`: NPC anchor/pivot (1,0) pos (-30,-158) (lật X nếu art nhìn phải) · Card anchor (1,0) pos (-350,150). Idempotent — Sếp chạy lại 1 nút.
6. `TutorialHandLayerTool` (DRY-RUN/APPLY, Undo): tạo `Tutorial_Canvas/Canvas_TutorialHand` (stretch, Canvas override 440, không GraphicRaycaster), dời `Tutorial_Hands`, `TutorialV2_Vfx`, proxy root của resolver vào; set `_tutorialCanvas` của `TutorialRuntimeTargetResolver` → canvas mới.

## C. Đội vẽ (prompt riêng, gói mới)
- Không cần asset mới cho vòng này: bàn tay, liềm, hạt, gem đều lấy từ asset đang chạy. Prompt đội vẽ vòng trước (`PROMPT_SPRITE_FORGE_UI_PASS_2026-09-05.md`: gói A NPC 37 file · gói B 4 minh hoạ) vẫn còn hiệu lực — gửi nếu chưa gửi.

## D. Rủi ro & rollback
- Dev A đụng `NotifyAction`/`PlayStep` (logic đang chạy) — đổi **cộng thêm coroutine**, không đổi điều kiện cũ; kiểm 3 kịch bản: popup mở trước action / sau action / không popup.
- Mọi thứ khác là cộng thêm hoặc đổi hằng số tool. Backup đầy đủ; commit về máy Sếp có mtime-guard (file bị sửa tay trong lúc Lead làm → từ chối ghi, báo lại).
