# KẾT QUẢ CÁC GÓI DEV — UI PASS 2026-09-05 (Lead gom từ báo cáo agent)

> Backup gốc mọi file: `production/backup_ui_pass_2026-09-05/WP-*/` (+MD5SUMS.txt). Chưa commit. Chưa compile trong Unity (Sếp kiểm).

## ✅ D2a — `Assets/_Game/Farm/Editor/CloseButtonSyncTool.cs` (MỚI, 444 dòng)
Menu `Tools/Farm/UI/`:
1. `Dong bo nut dong - 1. Copy sprite chuan vao Resources` → copy `UIStandardSprites.AllPaths` vào `Assets/Resources/UI/Standard/` (Sprite/Single + giữ 9-slice border).
2. `Dong bo nut dong - 2. DRY RUN` → liệt kê mọi nút đóng trong scene (path, sprite, size, có chữ X?).
3. `Dong bo nut dong - 3. APPLY` → gán `btn_red_small` Sliced trắng, size 64×64 nếu rect 48–120, tạo `Txt_X` nếu thiếu; Undo; MarkSceneDirty (không tự lưu).
4–5. `Dong bo nut kim cuong - DRY RUN / APPLY` → nền `proc_btn_blue` + icon `kimcuong`.
Lead đã soát API (TextureImporter, Undo, EditorSceneManager, TMPro) — hợp lệ; `#if UNITY_EDITOR` bao toàn file.

## ✅ C1 — `Assets/_Game/Farm/Editor/TutorialV2SetupTool.cs` (dòng 126–219)
Card: `LayoutElement{min 230}` + `VerticalLayoutGroup` + `ContentSizeFitter` vertical → mọc lên trên, đáy giữ y=150. Body autosize 28–38, `LayoutElement{min 96, flexible 1}`. `Row_Buttons` mới chứa `Btn_Continue` (230×68, sprite paper giữ). Idempotent (dời nút cũ vào row, không nhân đôi). `TutorialDialogueCard.cs` không cần sửa (chỉ animate anchoredPosition/scale/alpha).
CẦN BẠN: chạy `★ Dựng card hội thoại V2 (1 nút)` → Ctrl+S → test `L1L2_02_ReadyQuestion`.

## ✅ A2 — NPC tạm · hạt 8/6 · reset đủ · F8 · tool text
- `Scripts/Tutorial/TutorialNpcActor.cs`: 3 cờ TẠM THỜI (mặc định true): `_dungTalkChoMoiClip` (Wave/Point → talk), `_tatBlinkKhiKhacBo` (bỏ blink khi rect khác/đang talk-all), `_apDungThamSoChamV3` (Awake ép talkFps=6, enterSlide=0). Public API giữ. Tắt 3 cờ khi có art gói A.
- `Scripts/Tutorial/StarterInventorySetup.cs`: const `SO_HAT_LUA_KHOI_DAU=8`, `SO_HAT_HUONG_DUONG_KHOI_DAU=6` (10→8, 10→6).
- `Editor/ChoiLaiTuDauTool.cs`: `XoaThat()` gọi `SaveSystem.DeleteSave()` (SaveSystem.cs:268) trước `DeleteAll`.
- `Scripts/Tutorial/TutorialDebugJump.cs`: F9→**F8**; tự sinh `~TutorialDebugJump` (HideInHierarchy, DDOL) khi Play — không cần gắn vào scene.
- MỚI `Editor/TutorialStepTextFixTool.cs`: `Tools/Farm/Tutorial/Sua text 6 o -> 8 o - DRY RUN | APPLY` (2 asset lúa có "6 ô" xác nhận) · `Kiem tra hat khoi dau (chi bao cao)` (cảnh báo nếu inspector `starterItems` ≠ 8/6).
CẦN BẠN: DRY→APPLY text; `Kiem tra hat khoi dau` → nếu ⚠ sửa list Inspector; Play kiểm NPC 6fps + F8.

## ✅ D1 — Avatar popup bằng asset + DecorProgress bridge
- `Assets/_Game/Scripts/UI/AvatarProfilePopupUI.cs`: agent 1 làm dở (gọi `SetFrameOrFallback`/`LogSpriteFallbackOnce` chưa định nghĩa → KHÔNG compile); agent 2 đã bổ sung 2 helper, wire `avatarSlotBgImages`, sửa `CreateStatCard` (CardOuter/CardInner/SlotNormal, `valueNodeName`), icon qua `UIStandardSprites.Load` (bỏ `#if UNITY_EDITOR AssetDatabase`), sửa luôn bug cũ `AutoWireNewHierarchy` tìm `Txt_*Val` nhưng card tạo `Txt_Value`.
- Mapping: Board=FrameWood · Ribbon=Ribbon · Btn_Close=Close(64², "X") · Parchment=PanelPaper · AvatarFrame=AvatarBase · Slot_i=SlotNormal/Selected · Badge_Check=CheckBadge · Box_FarmName=RowDark(+PanelPaper lót) · Bar_Exp=BarTrack/BarFill · Card_*=CardOuter/Inner · Icon_Frame=SlotNormal · Btn_Save=BtnGreen3D. Badge_Level/Badge_Edit/Box_AvatarChoices vẫn vẽ code (ngoài mapping).
- `DecorGrowth/DecorProgressPopupBridge.cs`: PanelPaper / BarTrack / BarFill (giữ anchorMax.x QA R4) / BtnGem / IconGem, fallback màu giữ.
- Sanity ngoặc OK. ⚠ **Player build**: mọi sprite registry null cho tới khi chạy `Dong bo nut dong - 1. Copy sprite chuan vao Resources` (chỉ `Icons/icon_cooking_building` có sẵn trong Resources) — rơi về fallback code, không crash.
CẦN BẠN: menu Copy sprite (D2a) → `Tools/Farm UI/Avatar/Build Task 2 Popup…` → Play mở avatar → F10.

## ✅ C3 — "MIỄN PHÍ" vừa nút gem + panel hạt/hoa
- `Editor/BuildingProcessUIBuilderTool.cs`: 3 site (cây :141-151, tàu :238-248, reskin chuồng :545-575) → icon x=−26, `Txt_GemCost` 84×56 x=+6, NoWrap, autosize 12–22, Ellipsis. `CreateText` thêm param tuỳ chọn (caller cũ không đổi).
- MỚI `Editor/GemCostTextFixTool.cs`: `Tools/Farm/UI/Sua chu MIEN PHI nut gem - DRY RUN | APPLY` (quét `Txt_GemCost` mọi scene, cả inactive; Undo; prefab-override).
- MỚI `Editor/SeedPanelFixTool.cs`: `Tools/Farm/UI/Sua panel hat giong + hoa - DRY RUN | APPLY` — `Popup_seed`/`Popup_hoa` y 190→230, HLG top −100→0 bottom 8, `itemPreferredHeight`→170; prefab `Iteam_1` 120×170, Icon y=−8, `txt_name` bật (y=34, 18 auto 12–18), `txt_soluong` y=4 (24 auto 14–24). ⚠ Phần prefab ghi thẳng `SaveAsPrefabAsset` (không Undo) → chạy DRY RUN trước.
- `Scripts/UI/SeedPopupController.cs`: default 150→170 + cảnh báo Start nếu < 170.
CẦN BẠN: GemCost DRY→APPLY · SeedPanel DRY→APPLY · Ctrl+S · F10 panel hạt + panel hoa + mini-panel cây.

## ✅ C2 — Bảng hướng dẫn 4 trang thân thiện
- `Editor/TutorialFourPopupSetupTool.cs`: `Board_Frame` (TutFrame Sliced 900×620, kem #FFF6E5) mỗi trang; Ribbon TutRibbon; `BuildText(minAuto,maxAuto)`; Title 20–26; Instruction 720×72 auto 16–22 y=176; nhãn thẻ 12–17; ĐÃ RÕ = BtnGreen Sliced 300×72 nhãn 28; `Diamond_Button` = BtnGem 120×84 + `Icon_Gem`; liềm `FindSickleSpriteAsset()`; dots TutDotOn/Off; thẻ trang 4 đổi tên `Card_*`; idempotent (`CreateUI` tái dùng, `AddOrGet`, `Pick`).
- `Scripts/Tutorial/TutorialGuideBoardUI.cs`: whitelist float bỏ `Image` → thẻ không xoay.
- Lưu ý: `Icon_Gem` nhấp nhô cùng nút cha (chủ ý); nếu rung đôi → đổi tên `Gem_Icon`.
CẦN BẠN: `Tools/Farm Game/Rebuild Tutorial 4 Popups` → Ctrl+S → step `L1L2_03_GuideBoard` F10 4 trang.

## ✅ D2b — 12 điểm fallback nút đóng → `UIStandardSprites.Close` (15 file backup)
UnifiedTaskPopupUI ~598 · TaskPopupSpriteWireTool 39 · OrderBoardHierarchyBuilderTool · StallHierarchyBuilderTool 264 · ShopNewUIBuilder 90,183 · WarehouseNewUIBuilder 88,174 (Simple→Sliced) · MarketBoardUIBuilder 204 · MillPopupBuilderTool 2259,2369,3477 (`MillSkin.closeChuan`, giữ Glyph_X) · TouristBoatUIPopupSetupTool 485-495 (bỏ Knob.psd) · Train Station/Load/Process PopupUI · TrainPackageBuildTool ×3 · SkinVi.NutDong 51-60 (sprite thay MacAoNut, giữ tạo chữ X) · ShopSkin 147 → gọi `SkinVi.NutDong`.
Pattern: `UIStandardSprites.Close ?? cũ`, Sliced + white. Không đổi chữ ký public. Không asmdef → runtime scripts gọi được.

## ✅ A1 — Fix KẸT bàn tay + lưu bước tutorial
- `Scripts/Tutorial/TutorialStepTriggerBridge.cs`: `static LayODatLua()` (Normal+IsUnlocked, loại chau/pot/hoa, sort PlotId) & `LayChauHoa()` = nguồn duy nhất cho gate + tay; bỏ cap 6; `ResetAllTracking()`; `KiemTraLaiGate(TutorialWaitAction)` (đánh giá gate 9-12 theo ruộng thật, đạt → Notify, log `[Tutorial][Gate]`).
- `TutorialManager.cs`: `TUTORIAL_STEP_INDEX` lưu ở `AdvanceToNextStep`, resume ở `StartTutorial` (log `Resume bước N`), xoá ở `MarkTutorialDone`/`ClearTutorialDoneFlag`. `PlayStep` 5 nhánh quét ô → `ThuQuaGateNgay(step)` (purge queue cùng loại → Reset → KiemTraLaiGate; đạt thì `yield break`). `NotifyAction`: action quét ô tới sớm mà bước kế cần → xếp hàng. `SetupSmartGuide` sinh id theo số ô thật (`TaoIdQuetO`). Watchdog 45s → `DamBaoNutBoQuaNhinThay()` (card hiện lại kèm nút).
- `TutorialRuntimeTargetResolver.cs`: `IsPlotPending` id lạ → false + warn 1 lần; proxy dùng `LayODatLua/LayChauHoa`, bỏ cap 8/6, xoá `FindPlotsByCategory`.
TEST: kéo hạt thật nhanh đủ 8 ô → tự qua (Console `[Gate] … ĐÃ ĐẠT`/`Xếp hàng`); thoát giữa tutorial → Play lại `Resume bước N`; đứng 45s → thấy "Bỏ qua bước này"; L2_05 ngô không tự qua oan.

## ✅ B — Popup Lên Cấp (2 agent nối tiếp)
- B1 `CelebrationCharacterSlot.Play()` tự `SetActive(true)` khi có master; `StartV2Fx` fallback tìm `V2_CharSlot_0N` + warning.
- B2 `LevelUpPopupRewireTool.cs`: xoá blink cũ khi `dungBlink=false`; mục ④: `Bg_NenToi` alpha 0.65, `Btn_TiepTuc`=`UIStandardSprites.BtnGreen` Sliced, nối/tạo `Text_MoKhoa`/`Text_Hint` (Undo). DRY-RUN báo đủ.
- B3 `LevelRewardIconAutoFixer` + `MasterTutorialBeautifier`: dialog cảnh báo (mặc định Huỷ); IconAutoFixer thêm `Root_HienThi/Content`, null → bỏ ghi.
- B4 `LevelUpPopupUI.cs`: `_mergedCells: List<UnlockSlotUI>`; `UnlockSlotUI.CreateRuntimeCell()` (sao viền/tag từ ô mẫu, fallback SlotNormal); mọi ô pop+bob+caption; tag MỚI đỏ / ×N cam (quà) / +N cam (vàng, gem); `ResolveUnlockSlots` lọc `IsRuntimeCell`; `LayoutMergedRewardArea` co qua `SetBaseScale`. `LevelUpGiftSlotUI`: label neo giữa + `nameText`.
- B5 `VongLapPhaoHoa`: bùm ngay, rồi 0.8–1.2s, ≤3 đồng thời; `StopVFX` (ClaimAndClose/OnDisable/OnDestroy) dừng + huỷ root.
- B6 `LEVELUP_SEEN_MAX`: thiếu key → = cấp hiện tại; `Level>seen` → enqueue hiện lại sau 1s; ghi khi Claim.
- Rủi ro: vị trí `Text_MoKhoa`(y150)/`Text_Hint`(y108) ước lượng, có thể chồng nút → kéo tay; `GiftSlotBounceTooltip` vs `SetBaseScale` (có sẵn); reset L1 không xoá `LEVELUP_SEEN_MAX` (chỉ ảnh hưởng hiện-lại).
CẦN BẠN: `★ Nối lại dây popup` DRY-RUN → APPLY → Ctrl+S → test lên cấp.

## 🏁 9/9 gói xong · 34+ file · xem `production/BAO_CAO_UI_PASS_2026-09-05.md`
