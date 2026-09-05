# PLAN — UI PASS 3 TASK · 2026-09-05 (chờ Sếp duyệt)

> Nguồn: `production/session-state/SCAN_UI_PASS_2026-09-05.md` (4 agent quét, mọi mục có file:dòng).
> Backup trước khi sửa: `production/backup_ui_pass_2026-09-05/` (đã tạo, có MD5).
> Ký hiệu: 🤖 Dev tự làm (code/tool additive) · 🤝 Dev làm, Sếp bấm tool/Ctrl+S · 🎨 cần đội vẽ · ⚠ đụng DANH SÁCH DỪNG → cần Sếp gật.

---

## 0. VIỆC SẾP BẤM NGAY (chưa cần code mới — Vòng 17 chưa APPLY)
Report 14:15 cho thấy `Tutorial_Canvas=999, Market=125, Stall=120`, `_steps`=21 ⇒ **tool Vòng 17 chưa chạy**. Một phần lỗi "panel hạt đè HUD" + "thiếu 10 bước L2" biến mất khi Sếp làm theo `production/CHECKLIST_VONG17_2026-09-04.md` (6 bước, có Ctrl+S).

---

## TASK 1 — TUTORIAL

| # | Lỗi Sếp báo | Nguyên nhân (file:dòng) | Cách sửa | Loại | Owner |
|---|---|---|---|---|---|
| 1.1 | Khung không giãn theo text, chữ đè | Card 820×230 cố định, Body autosize OFF, overflow (`TutorialV2SetupTool.cs:119-161`, scene :83618) | Thêm `ContentSizeFitter` (Vertical=Preferred) + `VerticalLayoutGroup` cho Card; Body autosize 30–38, min-height 230; nút Continue vào layout riêng dưới body. Sửa **trong tool dựng** rồi Sếp bấm lại `★ Dựng card hội thoại V2` | 🤝 | ui-programmer |
| 1.2 | NPC nhảy tới lui | Art: wave/point/blink nền **magenta** + khung hình khác talk; blink chớp mỗi 3-6s (`TutorialNpcActor.cs:289-313`); talk chỉ 4 frame duy nhất | **Bước 1 (Dev, ngay):** `TutorialNpcActor` thêm cờ `useTalkForAllClips=true` (mặc định) → mọi bước dùng talk; tắt blink khi blinkSprite ≠ cùng bộ; giảm `talkFps` 12→6 và bỏ `enterSlide`. **Bước 2 (đội vẽ):** vẽ lại wave(12)+point(12)+blink(1) **cùng khung hình/pivot với talk**, nền trong, 512×640; và vẽ **talk 12 frame thật** (khẩu hình chậm, thân đứng yên). Dev nạp về qua tool có sẵn | 🤖 + 🎨 | technical-artist / đội vẽ |
| 1.3 | Bảng 4 trang chưa thân thiện | `tut_board_frame.png` **chưa gán** (`TutorialFourPopupSetupTool.cs:16,45`); Instruction 660×40 tràn; `Image_*` card bị xoay ±15° (`TutorialGuideBoardUI.cs:400-401`) | Gán frame + ribbon có sẵn (`Art/UI/TutorialV2/board/`), Instruction autosize 16–20 & cao 60, whitelist float chỉ `Icon_*`; palette ấm (kem/gỗ), bo góc. Sếp bấm lại `Rebuild Tutorial 4 Popups` | 🤝 | ui-programmer |
| 1.4 | 8 ô mà 10 hạt lúa | `StarterInventorySetup.cs:79` `amount=10` (inspector `starterItems` có thể override :87) | Đổi 10→8 lúa, 10→6 hướng dương trong code + tool `Kiểm tra starter` báo nếu inspector khác. Text step "6 ô"→"8 ô" (`L1L2_06/10`) | 🤖 (+🤝 nếu inspector override) | gameplay-programmer |
| 1.5 | Text nhỏ, ĐÃ RÕ & nút gem không asset | ĐÃ RÕ sprite=0 (`:369-387`); gem `FindNamedSprite` fail → ô xanh (`:52,190`, scene :388515) | ĐÃ RÕ → `Export_Kitchen_UI_Package/Sprites/btn_big_green.png` Sliced, 300×72, font 28; gem → `ui_building_svg/proc_btn_blue.png` + `kimcuong-removebg-preview.png`; liềm → sprite thật. Font body 20→24 | 🤝 | ui-programmer |
| 1.6 | **MIỄN PHÍ lòi rìa** | `Txt_GemCost` 36×30 trong nút 88×60, wrap off, autosize off (`BuildingProcessUIBuilderTool.cs:148-152`; `CropProcessPopupUI.cs:367-373`) | Widen 84×56, `NoWrap` + autosize 12–22; nhân bản cho bản train (:243-247). Sếp bấm lại tool process popup (hoặc tool vá tại chỗ DRY-RUN/APPLY) | 🤝 | ui-programmer |
| 1.7 | **KẸT bàn tay** | (A) latch `_allRicePlantsNotified` không reset cho L1 + event bị drop khi lệch bước (`TutorialStepTriggerBridge.cs:43,148-153`; `TutorialManager.cs:461-491,724-730`). (B) tập ô TAY ≠ GATE: proxy lấy cả ô **khoá** (`TutorialRuntimeTargetResolver.cs:342-364,492-499`) | (A) `PlayStep` bước AllPlots: **re-evaluate gate ngay khi vào bước** → nếu đã đủ thì Notify luôn; reset latch mỗi lần vào bước; queue chấp nhận `WaitForAllPlotsPlanted` khi đang `WaitForPlant`. (B) proxy lọc `IsUnlocked` giống gate; đồng bộ 1 hàm `LayDanhSachODat()` dùng chung. (C) hiện nút "Bỏ qua" ngay cả khi card ẩn. Thêm log `[Tutorial][Gate]` | 🤖 (khoanh vùng trong Tutorial/, không đụng plot logic) | gameplay-programmer |
| 1.8 | Panel hạt/hoa chữ bị che | Viewport 158.9px mask; `HorizontalLayoutGroup m_Top=-100` + `preferredHeight=150` (`SeedPopupController.cs:126-131`); prefab `Iteam_1` `txt_name` tắt & lệch ngoài tile | Tool `Sửa panel hạt/hoa (DRY-RUN/APPLY)`: padding top 0, root cao 190→230, preferredHeight=140, bật `txt_name` đặt (0,−52); áp cho cả `Popup_seed` & `Popup_hoa` | 🤝 (⚠ sửa 2 object scene + 1 prefab qua tool, có undo) | ui-programmer |
| 1.9 | **Tutorial chỉ 1 lần** | Cờ `TUTORIAL_MAIN_DONE` đã đúng (`TutorialManager.cs:168,316`) — lặp là vì **kẹt nên chưa tới FinishTutorial**. Thiếu: lưu bước hiện tại; tool reset không xoá `save.json` | Lưu `TUTORIAL_STEP_INDEX` mỗi `AdvanceToNextStep` (resume); `ChoiLaiTuDauTool` gọi thêm `SaveSystem.DeleteSave()`; bootstrap bảng F9 nhảy bước | 🤖 | gameplay-programmer |

---

## TASK 2 — POPUP LÊN CẤP

| # | Lỗi | Nguyên nhân | Cách sửa | Loại | Owner |
|---|---|---|---|---|---|
| 2.1 | 4 nhân vật mất | Scene wire đúng; runtime null do tool `LevelRewardIconAutoFixer` ghi `celebrationSlots=[null×4]` (`:110-171`) & `CelebrationCharacterSlot.Play` tự tắt không bật lại (`:125-130`) | Sếp bấm `★ Nối lại dây popup DRY-RUN→APPLY`. Dev: `Play()` tự `SetActive(true)` khi có master; rewire tool xoá blink cũ (:203); **khoá 2 tool phá** (IconAutoFixer, MasterTutorialBeautifier) bằng dialog cảnh báo | 🤝 | ui-programmer |
| 2.2 | Chỉ 3 quà có hiệu ứng/text | **2 list, 2 component**: `unlockEntries`→`UnlockSlotUI` (tag+pop), `giftItems`→`LevelUpGiftSlotUI` (không tag, **không tạo nameText**, label neo sai) | Gộp render: mọi ô dùng `UnlockSlotUI` (pop + bob + caption tên); tag **MỚI** cho unlock, tag **×N** cho gift; giữ 2 list data (nghĩa khác nhau) nhưng 1 đường hiển thị; gán `unlockDescText` | 🤖 | ui-programmer |
| 2.3 | Pháo hoa mất | `RunUIFireworks` 1.5s spawn 1 lần rồi Destroy (`LevelUpPopupUI.cs:1548-1634`) | Coroutine loop: bắn lại mỗi 0.8–1.2s (random vị trí) cho tới `ClaimAndClose→StopVFX` (:993,1392) | 🤖 | technical-artist |
| 2.4 | Nút Bắt đầu nào phẳng | `MasterTutorialBeautifier` ép builtin Background + #8CC63F, dim `Bg_NenToi` alpha 0 | Gán `btn_big_green.png` Sliced + phục hồi dim 0.65 trong rewire tool; khoá tool phá | 🤝 | ui-programmer |
| 2.5 | (phát sinh) popup chưa xem mất khi thoát | `_lastKnownLevel`/Queue RAM only (`:144,226-236`) | Lưu `LEVELUP_SEEN_MAX` PlayerPrefs; khi Start nếu level > seen → enqueue lại | 🤖 | gameplay-programmer |

---

## TASK 3 — AVATAR + NÚT ĐÓNG ĐỒNG BỘ

| # | Việc | Cách | Loại | Owner |
|---|---|---|---|---|
| 3.1 | **Registry chuẩn UI** `UIStandardSprites` (static, 1 file mới): `Close=btn_red_small.png` · `Gem=proc_btn_blue+kimcuong` · `Green=btn_big_green` · `Frame=popup_frame_wood` · `Paper=popup_panel_paper` · `Ribbon=ribbon_banner_gold` · `Card=shop_card_outer/inner` · `Slot=slot_normal/selected` · `Check=check_badge_green` · `Bar=progress_track_bar/fill_green` | Class mới, loader qua `SettingsPopupUI.LoadSprite` | 🤖 | lead-programmer |
| 3.2 | **Tool "Đồng bộ nút đóng toàn game" (DRY-RUN/APPLY)** | Quét scene + prefab: mọi Image trên object tên `Btn_Close|BtnClose|btnClose|closeButton` → gán Close chuẩn, Sliced, 64×64, giữ "X" TMP; báo cáo danh sách trước khi ghi; có Hoàn tác | 🤝 ⚠ (ghi vào scene qua tool, Sếp Ctrl+S) | tools-programmer |
| 3.3 | Sửa 18 điểm code fallback về registry (Avatar :640, UnifiedTaskPopup, OrderBoard, Stall, Shop, Warehouse, Market, Mill, TouristBoat Knob, Train ×4, ShopSkin, SkinVi) | Chỉ đổi dòng load sprite → `UIStandardSprites.Close` (giữ chữ ký) | 🤖 | ui-programmer |
| 3.4 | **Avatar popup dựng bằng asset** | Sửa `AvatarProfilePopupUI.CreateFreshHierarchy`: Board→frame_wood, Parchment→panel_paper, Ribbon→ribbon_gold, Avatar frame→`hud_avatar_base`, 8 slot→`slot_normal/selected`, 4 stat card→`shop_card_outer/inner`, EXP→progress_track/fill, Save→btn_green_3d, Close→chuẩn; icon stat card sang Resources thật. Sếp bấm `Tools/Farm UI/Avatar/Build Task 2 Popup` | 🤝 | ui-programmer |
| 3.5 | `DecorProgressPopupBridge` phẳng | Trỏ registry thay `Resources/UI/*` rỗng | 🤖 | ui-programmer |

---

## 🎨 GỬI ĐỘI VẼ (1 prompt gộp — viết sau khi Sếp duyệt)
| Gói | File | Vì sao |
|---|---|---|
| A | NPC guide: `guide_wave_01..12`, `guide_point_01..12`, `guide_blink` — **512×640, nền trong, cùng khung hình/vị trí đầu với `guide_talk_01`**; + `guide_talk_01..12` vẽ lại 12 frame khẩu hình thật (chậm, thân đứng yên) | Nguyên nhân gốc NPC "nhảy" |
| B | 4 minh hoạ bảng hướng dẫn 512×512 (gieo hạt · tăng tốc gem · thu hoạch liềm · nhận thưởng) | Thay card "Ô ĐẤT/HẠT GIỐNG" chữ + icon rời |
| C | (tuỳ chọn) icon `ing_*` đã xong; **không cần** vẽ nút/close/gem — project đã có |

## 🧑 CẦN BẠN (dự kiến sau khi code xong)
1. Chạy Vòng 17 (6 bước) nếu chưa.
2. `★ Nối lại dây popup` DRY-RUN → APPLY.
3. Bấm lại 3 tool dựng: `★ Dựng card hội thoại V2` · `Rebuild Tutorial 4 Popups` · `Build Task 2 Popup` (avatar).
4. Tool mới: `Đồng bộ nút đóng` DRY-RUN → APPLY · `Sửa panel hạt/hoa` DRY-RUN → APPLY.
5. **Ctrl+S**, `⚠ CHƠI LẠI TỪ ĐẦU`, Play test L1 → Lên cấp → L2, F10 từng bước.

## Thứ tự thực thi đề xuất
1. **1.7 kẹt + 1.9 persist** (chặn tutorial) → 2. **2.1–2.4 popup lên cấp** → 3. **1.6 / 1.4 / 1.5 / 1.8** (nhanh, rõ) → 4. **3.1–3.5 registry + avatar + đóng** → 5. **1.1 / 1.3** card & bảng → 6. prompt đội vẽ.
Mỗi nhóm: backup → sửa → compile check → cập nhật backlog/active.md.

## Câu hỏi cần Sếp chốt (mơ hồ → không đoán)
Q1. Quà lên cấp: **mọi ô** đều có pop + tên + tag (MỚI cho mở khoá, ×N cho quà) — đúng ý Sếp?
Q2. NPC: tạm dùng **talk cho mọi bước** (fps 6, tắt blink) tới khi đội vẽ giao — OK?
Q3. Nút đóng chuẩn = `btn_red_small.png` (đỏ tròn Cài đặt) **cho cả popup Train/Boat/Mill** đang dùng kiểu khác — đồng bộ hết?
Q4. Panel hạt: nâng root 190→230 sẽ che thêm ~40px map phía trên HUD — chấp nhận, hay giữ 190 và thu icon 90→76?
