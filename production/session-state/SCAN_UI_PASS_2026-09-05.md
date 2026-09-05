# SCAN — UI PASS 2026-09-05 (kết quả 4 agent quét, Lead tổng hợp)

> Nguồn sự thật cho PLAN. Mỗi mục có file:dòng. Chưa sửa gì ngoài `PopupCaptureReporter.cs` (timestamp).

---

## PHẦN 3 — AVATAR POPUP + NÚT ĐÓNG + KHO ASSET (agent 3 ✅)

### 3.1 Avatar popup = 100% vẽ bằng code, 0 asset khung
- `Assets/_Game/Scripts/UI/AvatarProfilePopupUI.cs` (991 dòng) — vừa logic vừa `CreateFreshHierarchy()`.
- Mọi khung/card/ribbon/nút = `SkinKit.BoGoc(r)` / `HinhTron()` (Texture2D sinh runtime, `SkinKit.cs:80-106`) + `Image.color`.
- Awake xoá `HoSoSkin` cũ (`:496-501`) ⇒ look hoàn toàn do code.
- Map node → dòng: Board_Wooden 624-627 · Ribbon_Header 631-634 · **Btn_Close 640-652 (HinhTron đỏ)** · Panel_Parchment 656-659 ·
  Avatar_Main_Frame 668-670 · Badge_Level 678-681 · Slot_{i}×8 717-739 · Box_FarmName 759-761 · Bar_Exp 771-785 ·
  Btn_SaveProfile 819-822 · **Card_* ×4 stat card 853-862**.
- Icon stat card load bằng `AssetDatabase` path cứng (`:800-810`), fallback `Resources/Icons/icon_warehouse|icon_achievement` **không tồn tại** ⇒ build thật 3/4 icon = null.
- Tool dựng: `Assets/_Game/Editor/JudgeAvatarHierarchyBuilder.cs` → `Tools/Farm UI/Avatar/Build Task 1 / Task 2 Popup`.
- HUD avatar (ngoài popup) dùng sprite thật: `Assetsgame/popup/ui_township_exact_bases/generated_sprites/hud_avatar_base.png` (120×120).

### 3.2 Nút đóng — chuẩn Sếp chọn = popup Cài đặt
- `SettingsPopupUI.cs:583,629-634`: sprite **`Assets/Export_Kitchen_UI_Package/Sprites/btn_red_small.png`** (256×96, 9-slice vào rect 64×64) + TMP "X" 26.
- Hiện có **4 sprite đóng khác nhau + 2 nút vẽ code**:
  | Nơi | Sprite |
  |---|---|
  | Settings | btn_red_small.png ✅ chuẩn |
  | AvatarProfilePopupUI 640-652 | ❌ HinhTron code |
  | UnifiedTaskPopupUI 595-608 (wire bởi TaskPopupSpriteWireTool:39) | Assetsgame/btnX.png (409×610) |
  | OrderBoardHierarchyBuilderTool 239 · StallHierarchyBuilderTool 264 | btnX.png ?? circle code |
  | ShopNewUIBuilder 180 · WarehouseNewUIBuilder 171 | btnX.png ?? ui_svg_perfect/btn_close.png (64×64) |
  | MarketBoardUIBuilder 201 · MillPopupBuilderTool 2366/3454 | ui_svg_perfect/btn_close.png ?? code |
  | TouristBoatUIPopupSetupTool 485-496 | ❌ Unity built-in Knob.psd tint đỏ |
  | Export_Train_UI_Package (Station/Load/Process/BuildTool) | btnX.png / ui_svg_perfect |
  | ShopSkin.cs:145 · SkinVi.NutDong | ❌ BoGoc code |
  | WarehousePopupUI/MarketPopupUI/MillPopupUI/ShopManager/DockPurchasePopupUI/PopupEwarManager | field serialized, sprite do tool/scene gán |
- Asset đóng khác chưa dùng: `Fantasy Wooden GUI  Free/PNG/Close Button.png` (94×91).
- **Không có class UI theme/sprite registry chung.** Gần nhất: `UnifiedTaskPopupSprites` SO + `TaskPopupSpriteWireTool` (bảng field→path); loader chung `SettingsPopupUI.LoadSprite(string)` (`:803-813`, static, AssetDatabase → Resources fallback).

### 3.3 Nút kim cương
- Icon gem: `Assetsgame/kimcuong-removebg-preview.png` (666×375 — 2 viên, ngang). **`Assetsgame/kimcuong.png` KHÔNG tồn tại** (3 loader gọi rồi fallback im lặng: BuildingProcessUIBuilderTool:30, PenProcessPopupUI:146, ShopNewUIBuilder:106).
- Nút gem có sprite: crop/pen/train process (`proc_btn_blue.png` 100×70, rect 88×60) · mill/shop (`shop_btn_buy_gem.png` 160×56) · train (`btn_blue_gem_3d.png`).
- ❌ **DecorProgressPopupBridge.cs:36-44,150-236**: mọi path `Resources/UI/*` → null (thư mục `Assets/Resources/UI` RỖNG) ⇒ nút gem + panel + track toàn màu phẳng.
- Tutorial không vẽ nút gem, chỉ trỏ vào `btn_PenGem` (TutorialRuntimeTargetResolver:163, TutorialManager:605,883).

### 3.4 "MIỄN PHÍ" lòi rìa (T1-S6) — NGUYÊN NHÂN CHÍNH XÁC
- `CropProcessPopupUI.cs:367-373` ghi `"MIỄN PHÍ"` vào `Txt_GemCost`.
- Ô text tạo ở `BuildingProcessUIBuilderTool.cs:148-152` (và 243-247 bản train): **36×30 px tại x=+18 trong nút 88×60**, font 22, `CreateText` (`:602-613`) **không set wrapping/overflow** ⇒ wrap 2 dòng tràn ra ngoài nút.
- Fix: widen box ≈ 84×56, `textWrappingMode=NoWrap`, `enableAutoSizing` (min 12 max 22) — hoặc đổi chữ ngắn ("FREE"/icon).

### 3.5 Kho asset UI tái dùng (đã kiểm đếm)
| Bộ | Path | Dùng cho |
|---|---|---|
| **Export_Train_UI_Package/Sprites** (49) | `popup_frame_wood` 128² · `popup_panel_paper` 96² · `ribbon_banner_gold` 128×48 · `timer_box_dark` · `btn_green_3d/yellow/disabled/blue_gem_3d` 96×48 · `progress_track_bar`+`fill_green` · `check_badge_green` · `icon_disc_large` | **Settings đang dùng** → chuẩn cho Avatar popup (map 1:1 với Board/Parchment/Ribbon/Bar_Exp/Badge_Check/Btn_Save) |
| **Export_Kitchen_UI_Package/Sprites** (54) | `btn_red_small` (đóng) · `btn_big_green/gray` · `card_ingredient` 140×170 · `panel_board_wood` 200² · `panel_paper_cream` · `ribbon_header_orange` · `icon_gold` | nút ĐÃ RÕ / Bắt đầu nào / card |
| **popup/ui_svg_perfect/generated_sprites** (20) | `panel_outer` 420×280 · `inner_panel` · `banner_header` · `btn_close` 64² · `btn_green` · **`slot_normal/selected/empty` 130²** · `circle_preview` · `badge_count` · `tab_*` · `progress_*` | Slot chọn avatar ×8 |
| **popup/ui_shop_svg** (12) | **`shop_card_outer` 160×210 + `shop_card_inner`** · `shop_banner_ribbon` 480×120 · `shop_currency_chip` · `shop_btn_buy_gem` | 4 stat card avatar |
| **popup/ui_building_svg** (4) | `proc_frame_bg` · `proc_track_bg` · `proc_fill_green` · `proc_btn_blue` | mini-panel cây/chuồng |
| **popup/ui_township_exact_bases** (9) | `hud_avatar_base` 120² · `hud_currency_base` · `hud_level_star` · `hud_callout_panel` · `hud_btn_plus` | khung avatar chính |
| Fantasy Wooden GUI Free (29) | `TextBTN_Big/Medium(+Pressed)` · `UI board L/M/S parchment/stone` · `WoodBoard_Frame` · `IRONY TITLE` · `Close Button` 94×91 · `khungvang/khungkimcuong/khungavata` | dự phòng |
| Art/UI/** (134) | TutorialV2 board(5)/npc(37)/vfx(10) · LevelUpV2 characters(58)+fireworks(7) · Settings flags · Icons(5) | **0 nút, 0 đóng, 0 gem, 0 card** |
| `Assets/Resources/UI` | **RỖNG** | nguyên nhân DecorProgress phẳng |

### 3.6 Đề xuất fix tối thiểu (agent 3)
1. `AvatarProfilePopupUI.cs:640-652` → `btn_red_small.png` Sliced 64×64 (qua `SettingsPopupUI.LoadSprite`).
2. `:624-627, 631-634, 656-659, 771-785, 819-822` → bộ Train frames (§3.5 hàng 1).
3. `:717-739, 853-862` → `slot_normal/selected` + `shop_card_outer/inner` + `check_badge_green`.
4. `BuildingProcessUIBuilderTool.cs:148` → widen Txt_GemCost + NoWrap + autosize.
5. `DecorProgressPopupBridge.cs:36-44` → trỏ path thật (hoặc field sprite).
6. **Tạo 1 registry `UIStandardSprites`** (SO hoặc static) để mọi popup lấy nút đóng/gem/nút xanh từ 1 chỗ — chấm dứt 6 biến thể.

---

## PHẦN 1 — TUTORIAL (agent 1 ✅)

### 1.1 Hai hệ hộp thoại khác nhau trên `Tutorial_Canvas`
**(a) Card hội thoại NPC** — `TutorialDialogueCard.cs`; tool `TutorialV2SetupTool.cs` (`Tools/Farm Game/Tutorial V2/★ Dựng card hội thoại V2`). Hierarchy `TutorialV2_Dialogue/{NPC_Guide, Card/{Body, Btn_Continue}}`.
- Card **cố định 820×230** (`TutorialV2SetupTool.cs:119-161`), Body 728×120, font 38, **autosize OFF, overflow=Overflow, không ContentSizeFitter** (scene :83618) ⇒ dòng 3 tràn ra ngoài & đè lên `Btn_Continue` (230×68 neo dưới-phải, padding đáy chỉ 76).
- `Btn_Continue` CÓ sprite `btn_paper_small.png` (:172-174) — OK.
**(b) Bảng hướng dẫn 4 trang** (ribbon + ĐÃ RÕ) — `TutorialGuideBoardUI.cs`; tool `TutorialFourPopupSetupTool.cs` (`Tools/Farm Game/Rebuild Tutorial 4 Popups`).
- 🔴 **`tut_board_frame.png` load rồi KHÔNG BAO GIỜ gán** (`:16,45`, border set :133) ⇒ trang chỉ có dim 0.45 + card nổi, không khung.
- Mọi text rect cố định, autosize OFF (`BuildText :389-405`): Title 440×48/25 · Instruction **660×40**/20 (2 dòng ≈52px tràn, đụng ribbon y=240 & card trên y=160) · Label 30px/17 · ĐÃ RÕ 260×62/26.
- 🔴 `AnimateIconsFloat :386-438` whitelist tên bắt đầu `Icon|Image|Diamond_|Badge` ⇒ trang 4 card `Image_Harvest_Drop`/`Image_Rice_Collected` (230×240) **bị xoay ±15° & nảy** kéo theo Label ⇒ chữ đè nhau.
- 🔴 `ConfirmButton` "ĐÃ RÕ": `m_Sprite=0`, màu (0.24,0.68,0.22) — phẳng (`:369-387`, scene :551115). Label = đúng size nút, 0 padding.
- 🔴 `Diamond_Button` trang 2: `FindNamedSprite("btn_RutNang_TGCay","Btn_gem","GemBox")` (`:52`) **thất bại** ⇒ ô xanh (0.2,0.75,1) 100×100 (scene :388515). Liềm trang 3 cùng pattern (`:53`).

### 1.2 NPC nhảy tới lui — do ART, không do code
- `TutorialNpcActor.cs:59-74`: talk 12fps, wave 14, point 12, forward 1→12 (không ping-pong), blink 0.12s mỗi 3-6s (:289-313). Clip theo bước: `ChonClipNpc` (`TutorialManager.cs:1111-1123`) Welcome/Celebration→Wave, WaitForClick→Talk, còn lại→Point.
- NPC_Guide rect 300×375, preserveAspect (scene :566524). 37 PNG đều **512×640** ⇒ canvas OK.
- 🔴 Bên trong canvas: `guide_talk_*` nền trong, cắt sát đầu-vai (đầu ~55%); **`guide_wave_*`, `guide_point_*`, `guide_blink` nền MAGENTA #FF00FF**, nửa thân, đầu nhỏ hơn, thấp hơn (point còn có dải xám trên).
  ⇒ (1) blink 0.12s mỗi 3-6s = **chớp ô magenta + đầu co/tụt rồi bật lại** (chính là "nhảy tới lui"); (2) đổi Talk↔Point mỗi bước = dịch chuyển/đổi scale; (3) `enterSlidePixels=90` chồng thêm.
- Bộ "CatDeu" 308×588 trong `art-handoff/2026-09-04_MASTER/D_NPC_CatDeu` **chưa import** (asset trong project md5 khác) — và nếu import sẽ **tệ hơn** (aspect 0.524 vs 0.8 của wave/point).
- Talk chỉ có **4 frame duy nhất** (01=12, 02=11, 03=04=09=10, 05=06=07=08) ⇒ giật do asset.

### 1.3 Luồng bước & KẸT
- Step assets: `Assets/Resources/TutorialSteps/L1_L2/` (32). `_steps` trong scene (`SCN_Farm.unity:577-597`) = **21 entry: thiếu `L1L2_04b_FirstHarvest` + 10 bước L2** (tool Vòng 17 "Khoi phuc 10 buoc L2" chưa APPLY — khớp report 14:15).
- `waitAction`: 2 WaitForPlant · 9 WaitForAllPlotsPlanted · 16 WaitForSeedPanel · … (TutorialStepData.cs:3-28).
- Bước 4 `L1L2_05_DragFirstRice` (WaitForPlant, bridge :127-132) → bước 5 `L1L2_06_PlantAllRice` (WaitForAllPlotsPlanted, gate `AllRiceFieldPlanted()` bridge :74-94 → :148-153).
- 🔴 **KẸT (A) — latch `_allRicePlantsNotified`** (bridge :43) set 1 lần, chỉ reset ở `L2_05_PlantCorn` (`TutorialManager.cs:845`), **không reset cho L1**. Nếu ô cuối được trồng khi còn ở bước 4/Transitioning → `NotifyAllPlotsPlanted` bị `NotifyAction :461-491` **bỏ im lặng** (pendingWait ≠ action, queue đòi step.waitAction == action :485-489) → vào bước 5 không còn ô trống, không event nữa, `PlayStep :724-730` không re-evaluate gate ⇒ **treo vĩnh viễn**.
- 🔴 **KẸT (B) — tập ô của TAY ≠ tập ô của GATE**: proxy `SetupRicePlotProxy` (resolver :342-364) lấy **mọi** Normal plot (không lọc IsUnlocked, :492-499), nearest-match 8 vị trí cứng `RICE_ORDER` :61-66; gate lọc `IsUnlocked` + tên không chứa chau/pot/hoa. Ô **khoá** có thể chiếm slot proxy → `IsPlotPending` = IsEmpty = true mãi → **tay chỉ ô đó vô hạn** (đúng ảnh 142417: ô trên cùng trống). >8 ô mở → gate đòi ô 9+ mà tay không chỉ.
- (C) `IsPlotPending :48-52` id lạ → `true` ⇒ pending mãi. (D) `WatchdogHetHat :1341-1377` chỉ nổ khi **mọi** loại hạt = 0 (starter cho cả bắp cải/cà chua/…) ⇒ không bao giờ cứu. (E) nút "Bỏ qua bước" 45s nằm trên card nhưng `SetupSmartGuide :1385` **ẩn card** ⇒ vô hình.
- Lệch số: bridge `FindNormalPlotsByName` cap **6** (:262); text step nói "6 ô"; sweep 8.

### 1.4 Hạt dư
- `StarterInventorySetup.cs:79` `seed_rice amount=10` (fallback list; **nếu `starterItems` trên component scene có dữ liệu thì inspector thắng** :87). 1 hạt/ô (`PlantDragController.cs:164-173`). 10−8 = 2 dư ✔. Hoa `seed_huong_duong=10` vs 6 chậu → 4 dư (:80). Top-up 1 lần theo `STARTER_ITEMS_GIVEN`.

### 1.5 Panel hạt / panel hoa (cấu trúc y hệt)
- `Popup_seed` (fileID 1116062372) & `Popup_hoa` (29644360): root 1693×190 neo đáy; Viewport `sizeDelta.y=-31.068` + **RectMask2D**; Content 210×295.94 với **HorizontalLayoutGroup `m_Top:-100`**, spacing 50, ChildControlHeight=1; `SeedPopupController.cs:126-131` ép `preferredHeight=150` (prefab chỉ 140).
- Toán: viewport 158.9px; inner rect bắt đầu 100px trên → tile 150px lệch lên ~50px ⇒ **đáy tile (chỗ `txt_soluong` neo bottom) bị mask cắt**.
- 🔴 Prefab `Assets/Assetsgame/hatgiong/Iteam_1.prefab`: `txt_name` **m_IsActive:0** tại (−146.3, −108.7) — ngoài tile 120×140 hoàn toàn. Không có dòng giá.
- Panel đè HUD dưới = do thứ tự Canvas (Tutorial 999 / HUD 100…) chưa sắp — Vòng 17 chưa APPLY.

### 1.6 Tool tutorial (menu) — xem bảng agent; lưu ý `TutorialHandFlowRebuildTool` từng auto-save scene mỗi recompile (đã tắt :6-13).

## PHẦN 2 — POPUP LÊN CẤP (agent 2 ✅)

### 2.1 Code
`Assets/_Game/Farm/Scripts/UI/LevelUpPopupUI.cs` (1678 dòng): `HandleLevelChanged`:225 · `PopulateUI`:340 · `ApplyUnlockSlots`:819 · `BuildMergedGiftCells`:661 · `LayoutMergedRewardArea`:704 · `ClaimAndClose`:983 · `StartV2Fx`:1061 · `SpawnUIFireworks`:1520 · `RunUIFireworks`:1548 · `VfxBurstLoop`:1378 · `StopVFX`:1392.
Phụ: `LevelUpGiftSlotUI.cs` (ô quà procedural) · `UnlockSlotUI.cs` (ô mở khoá, tag MỚI :81-93, PlayPop :137) · `CelebrationCharacterSlot.cs` (Play :117, PuppetLoop :205) · `LevelRewardConfig.cs` (SO).
Scene: `Canvas_Popup/Popup_LevelUp_Township/Root_HienThi/Content/{BangRon, NgoiSao, Hang_PhanThuong, Dai_MoKhoa, Btn_TiepTuc, V2_Celebration/V2_CharSlot_01..04}`.

### 2.2 Nhân vật biến mất — scene ĐANG wire đúng, runtime rỗng
- Scene `SCN_Farm.unity:443110-443114`: `celebrationSlots` = 4 ref hợp lệ; puppetMaster = char_01_f05 / char_03_f05 / **char_05_f05 / char_06_f05** (vòng 16B bỏ char_02 mũ hỏng, char_04 trùng char_03 — `LevelUpPopupRewireTool.cs:66-75`). Sprite tồn tại, meta đúng.
- **Thủ phạm #1:** `LevelRewardIconAutoFixer.cs` (menu `★ Tự Động Sửa Icon & Gộp Quà & Đồng Bộ Nhân Vật`, :26) tìm `PopupRoot/…/Content` (:110-112) **không tồn tại** → `content==null` → `:163-171` ghi `celebrationSlots=[null×4]` im lặng. Chính là hư hại 04/09 đã ghi ở `LevelUpPopupRewireTool.cs:14-16`.
- **Thủ phạm #2:** `CelebrationCharacterSlot.Play()` :125-130 tự `SetActive(false)` nếu thiếu master/image và **không bao giờ tự bật lại**.
- Bug phụ: rewire tool `:203` không xoá blink cũ → slot 3/4 chớp mặt char_03/char_04 (nhân vật khác).
- Tool ghép: `LevelUpPuppetArtTool` dùng `_master.png`; `LevelUpPopupRewireTool` dùng `_f05.png` & cấm master → **2 tool mâu thuẫn**; `LevelUpSlotLayoutFixTool` âm thầm gọi tool thứ nhất.
- Fix đúng: chạy `★ Nối lại dây popup (DRY-RUN→APPLY)` (`LevelUpPopupRewireTool.cs:33-34`) + code tự phục hồi slot bị tắt + KHÔNG chạy 2 tool phá (IconAutoFixer, MasterTutorialBeautifier).

### 2.3 Quà — "2 nguồn data" là THẬT (Sếp đúng)
- 1 asset/level (`Assets/_Game/Farm/data/Lever Game/`, 29 file) nhưng **2 list trong 1 asset, 2 component render**:
  - `unlockEntries` (2-3/level) → `UnlockSlotUI` → **có tag MỚI + pop + bob**.
  - `giftItems` (6-7/level) → `LevelUpGiftSlotUI.BuildProcedural` → **không tag, không pop, không tên**.
  - `unlockDescriptions` (legacy, gõ tay trùng).
- `PopulateUI:359` gọi ApplyUnlockSlots rồi :377-415 dựng gift rồi :438 gộp vào cùng dải ⇒ nhìn 1 hàng nhưng 2 hệ.
- Không có cap 3; `unlockSlots[]`=9; ảnh 142339 hiện 11 ô = 3 unlock + vàng + gem + 6 gift — data đủ.
- **Text trống ở ô quà = 2 bug `LevelUpGiftSlotUI.cs`:** (1) `BuildProcedural` không tạo `nameText` → tên không bao giờ hiện; (2) `CreateLabel` không set `anchorMin/Max` (mặc định 0,0) → nhãn số lượng neo góc dưới-trái pill cam → 8 pill cam trống.
- `ApplyUnlockSlots:863` truyền caption `""`; `unlockDescText` & `hintText` = `{fileID:0}` trong scene → dòng "Mở khoá: …" không hiện.
- Data: `BANG_QUA_LEVELUP_V2_2026-09-01.md:70` (24 level×6, 5 level đậm×7). Tool đổ: `LevelRewardV2FillTool.cs:108,111`.

### 2.4 Pháo hoa tắt sớm
- `RunUIFireworks:1548`: `kTotalDuration=1.5f` (:1594), spawn 1 lần (:1553), tự Destroy (:1629-1634). **Không gì gọi lại** ⇒ bắn 1.5s rồi hết.
- `VfxBurstLoop:1378` có loop 0.6s nhưng chỉ cho `vfxSidePrefab` (Lana Flash); nhánh confetti bị bỏ khi `useUIFireworks=1` (:1217-1241).
- Scene: `useUIFireworks:1`, `fireworksOnTopLayer:1`, 7 sprite `Art/UI/LevelUpV2/fireworks/` OK. Stop path `ClaimAndClose:993→StopVFX:1392` — chỗ hook loop sạch.

### 2.5 Nút "Bắt đầu nào" phẳng
- Scene `Btn_TiepTuc` Image: `m_Sprite = builtin UI/Skin/Background.psd`, color `#8CC63F`.
- **Thủ phạm:** `MasterTutorialBeautifier.cs` (`Tools/Farm/Master Beautify Tutorial & Mission UI`, :9) — :161-167 ép mọi Button trong canvas có "LevelUp" về builtin Background + `#8CC63F`; :169 set `Color.clear` cho Image tên chứa panel/background/bg ⇒ **`Bg_NenToi alpha=0`, popup không dim**.
- Đích đúng: `LevelUpPopupTownshipTool.cs:455` → `spr_btn_green` (placeholder code-gen). Asset đẹp sẵn: `Export_Kitchen_UI_Package/Sprites/btn_big_green.png` (đã được đề xuất 2 lần trong PROMPT ROUND13:97 & MASTER:160 — "có sẵn, Dev chưa gắn").

### 2.6 Tool liên quan (13 menu) — 2 tool PHÁ: `LevelRewardIconAutoFixer`, `MasterTutorialBeautifier`. Stray: `char_01_master.png.bak` có .meta.

## PHẦN 4 — PERSISTENCE / RESET TUTORIAL (agent 4 ✅)

### 4.1 Tutorial "đã xong" = 1 cờ PlayerPrefs duy nhất
- `TutorialManager.cs:168` `TUTORIAL_MAIN_DONE` · ghi **chỉ khi** `FinishTutorial()` (:1243→`MarkTutorialDone()` :1249) · đọc ở `Start()` :316 → `SkipTutorialEntirely()` (:1220, có nhả `blocksRaycasts`).
- ⇒ Người chơi hoàn thành tutorial **ĐÃ được bỏ qua ở lần Play sau** — đúng ý Sếp. Nếu thấy tutorial lặp lại ⇒ do (a) chưa bao giờ tới `FinishTutorial` (kẹt bước!) hoặc (b) tool reset xoá cờ.
- **Không lưu bước hiện tại** (`_currentIndex` :233 RAM only; grep `TUTORIAL_STEP` = 0 hit) ⇒ thoát giữa tutorial → lần sau chạy lại từ bước 0.
- 4 công tắc dev đều OFF trong scene (`SCN_Farm.unity:645-647`): `_devForceReplayTutorial`, `_devClearDoneFlagOnStart` (:206-211, :310-314), `TutorialPrePlant._forceResetFlagOnPlay` (:49-56), `StarterInventorySetup.forceResetOnPlay` (:30-47).
- 7 cờ họ TUTORIAL: `TUTORIAL_MAIN_DONE`, `TUTORIAL_PREPLANT_DONE` (TutorialPrePlant:27), `STARTER_ITEMS_GIVEN` (StarterInventorySetup:8), `ANIMAL_GUIDE_COOP_FEED_DONE`/`GUIDE_DELIVER_DONE`/`GUIDE_TRAIN_DONE`/`GUIDE_COOKING_DONE` (AnimalGuideController:53-80).

### 4.2 Tool reset dev
- Chính: `Tools/Farm/⚠ CHƠI LẠI TỪ ĐẦU (như người chơi mới)` — `ChoiLaiTuDauTool.cs:7,60-65` `PlayerPrefs.DeleteAll()` (khôn: nếu đang Play thì hẹn xoá sau EnteredEditMode :29-58). Xoá đủ tutorial+vàng+level+ô đất+kho.
- ❌ **GAP:** không gọi `SaveSystem.DeleteSave()` ⇒ `save.json` cũ còn; `SaveDebugTool.cs:29-34` auto-save lúc ExitingPlayMode (trước khi xoá) ⇒ lần Play sau `SaveBootstrap` nạp lại chuyến tàu cũ cho "người mới". PlayerPrefs an toàn chỉ nhờ `SaveBootstrap.AutoRestoreMissingPrefs=false` (:38).
- Reset **đầy đủ nhất** hiện có = in-game `SettingsPopupUI.OnResetProgressClicked` (:322-395): `SaveSystem.DeleteSave()` + `DeleteAll` + `ClearTutorialDoneFlag` + `SaveVersionGuard.ClearAll` + destroy managers + reload scene. → Tool editor nên gọi cùng logic.
- Phụ: `FarmResetTool.cs:8` Hard Reset · `Phase1TestTool.cs:233` (giữ tutorial) · `DemoL1L10Tool.cs:623` alias · `FarmSaveCleanupTool.cs:76` (không đụng tutorial).

### 4.3 SaveSystem
- `save.json` hợp nhất, `CurrentSaveVersion=1`, ghi atomic + `.bak` (`SaveSystem.cs:16-18,87-120`). Auto-save: dirty debounce 5s, chu kỳ 60s, pause/focus/quit (`SaveBootstrap.cs:245-277`).
- Tutorial flags chỉ là **bản chụp mirror** (`SaveData.cs:113`, `SaveAdapters.cs:376-390`), restore tắt cứng. **Không auto-save khi qua bước tutorial** (dirty chỉ từ currency/exp/level/inventory/warehouse/plot).

### 4.4 Popup Lên Cấp — "đã xem" KHÔNG lưu
- `LevelUpPopupUI.cs:144,185-186,226-236`: `_lastKnownLevel` + `Queue<int>` RAM only, 0 PlayerPrefs. Lên cấp mà chưa bấm "Bắt đầu nào" rồi thoát ⇒ popup mất vĩnh viễn. Pattern tốt để bắt chước: `BoatAnnouncePopupUI.cs:587-602` (`KeyDaBaoFormat`).

### 4.5 Bảng F9 nhảy bước
- `TutorialDebugJump.cs` (176 dòng) → `TutorialManager.DebugNhayToiBuoc` (:1496-1519) — không persist, không chạy lại logic gameplay.
- ❌ **Không nằm trong scene nào** (GUID không xuất hiện ở .unity/.prefab, không tool nào AddComponent) ⇒ bấm F9 hôm nay chỉ ra `PopupGateDebugF9` (tự bootstrap). Cần bootstrap `[RuntimeInitializeOnLoadMethod]` hoặc đổi phím.

### 4.6 Kết luận cho yêu cầu "tutorial 1 lần"
Cơ chế đã đúng về nguyên tắc; việc cần làm: (1) **sửa kẹt bước** để `FinishTutorial` chạy được; (2) lưu `TUTORIAL_STEP_INDEX` mỗi khi qua bước (resume, không về 0); (3) tool CHƠI LẠI gọi thêm `SaveSystem.DeleteSave()`; (4) bootstrap F9 jump panel để Sếp test nhanh.
