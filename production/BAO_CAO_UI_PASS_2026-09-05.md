# 📋 BÁO CÁO UI PASS — 3 TASK · 2026-09-05

> Tech Lead tổng hợp. Chi tiết từng gói: `production/session-state/WP_RESULTS_2026-09-05.md` · Scan gốc: `session-state/SCAN_UI_PASS_2026-09-05.md` · Plan đã duyệt: `PLAN_UI_PASS_2026-09-05.md`.
> **Backup toàn bộ file gốc:** `production/backup_ui_pass_2026-09-05/WP-*/` (+ `MD5SUMS.txt`). Hỏng → xoá file mới, copy ngược từ backup.
> **Chưa commit. Chưa compile trong Unity** (agent không mở được Editor) — bước 0 của CẦN BẠN là kiểm 0 lỗi đỏ.

---

## 1. Kết quả theo lỗi Sếp báo

| # | Lỗi Sếp báo | Nguyên nhân gốc | Đã sửa | File |
|---|---|---|---|---|
| T1-S1a | Khung text không giãn, chữ đè nút | Card cố định 820×230, autosize OFF | Card `VerticalLayoutGroup + ContentSizeFitter`, mọc lên trên; Body autosize 28–38; nút xuống `Row_Buttons` | `Editor/TutorialV2SetupTool.cs` |
| T1-S1b | NPC nhảy tới lui | **Art**: wave/point/blink nền magenta + khung khác talk; blink chớp 3–6s | **Tạm**: mọi clip dùng talk, tắt blink khác bộ, talkFps 6, bỏ trượt vào (3 cờ Inspector, tắt khi có art mới). **Gốc**: đơn đội vẽ gói A | `Scripts/Tutorial/TutorialNpcActor.cs` · `PROMPT_SPRITE_FORGE_UI_PASS_2026-09-05.md` |
| T1-S3 | Bảng 4 trang chưa thân thiện | `tut_board_frame` load nhưng quên gán; thẻ `Image_*` bị xoay ±15°; text rect cứng | Khung kem 900×620 + ribbon + dots thật; Instruction/label autosize; thẻ đổi tên `Card_*` + whitelist float chỉ `Icon_*` | `Editor/TutorialFourPopupSetupTool.cs` · `Scripts/Tutorial/TutorialGuideBoardUI.cs` |
| T1-S4 | 8 ô mà 10 hạt | `StarterInventorySetup.cs:79` hardcode 10 | Lúa 8, hướng dương 6 (const) + tool kiểm inspector override | `Scripts/Tutorial/StarterInventorySetup.cs` · `Editor/TutorialStepTextFixTool.cs` |
| T1-S5 | ĐÃ RÕ & nút gem không asset | sprite=0; `FindNamedSprite` fail | ĐÃ RÕ = `btn_big_green` 300×72 chữ 28; gem = `proc_btn_blue` + icon kim cương; liềm tìm sprite thật | `Editor/TutorialFourPopupSetupTool.cs` |
| T1-S6 | **MIỄN PHÍ lòi rìa** | Ô text 36×30 trong nút 88×60, wrap off | 84×56, NoWrap, autosize 12–22, Ellipsis (3 site builder + tool vá 7 instance trong scene) | `Editor/BuildingProcessUIBuilderTool.cs` · `Editor/GemCostTextFixTool.cs` |
| T1-KẸT | **Bàn tay đứng im** | (A) latch `_allRicePlantsNotified` không reset L1 + event tới sớm bị bỏ; (B) tay lấy cả ô KHOÁ, gate thì bỏ | 1 hàm `LayODatLua()/LayChauHoa()` dùng chung tay+gate; vào bước quét ô → reset latch + **kiểm gate ngay**; action tới sớm → xếp hàng; id lạ không pending; nút "Bỏ qua" 45s hiện được | `TutorialStepTriggerBridge.cs` · `TutorialManager.cs` · `TutorialRuntimeTargetResolver.cs` |
| T1-panel | Text hạt bị che, đè HUD | HLG `top −100`, tile 150 trong mask 159px; `txt_name` tắt & lệch ngoài tile; canvas order chưa sắp (Vòng 17) | Tool: panel 190→230, padding 0/8, tile 120×170, bật tên hạt; + Sếp APPLY Vòng 17 | `Editor/SeedPanelFixTool.cs` · `Scripts/UI/SeedPopupController.cs` |
| T1-1lần | Tutorial phải chạy 1 lần | Cờ `TUTORIAL_MAIN_DONE` đã đúng — lặp vì kẹt nên chưa tới `FinishTutorial` | Sửa kẹt (trên) + **lưu bước hiện tại** `TUTORIAL_STEP_INDEX` (thoát giữa chừng → resume); tool CHƠI LẠI xoá luôn `save.json`; F8 bảng nhảy bước tự sinh | `TutorialManager.cs` · `Editor/ChoiLaiTuDauTool.cs` · `TutorialDebugJump.cs` |
| T2-a | 4 nhân vật mất | Tool `LevelRewardIconAutoFixer` ghi null ×4; slot tự tắt không bật lại | Slot tự hồi; fallback tìm theo tên; **khoá 2 tool phá** bằng dialog; rewire tool xoá blink cũ | `CelebrationCharacterSlot.cs` · `LevelUpPopupUI.cs` · `LevelUpPopupRewireTool.cs` · `LevelRewardIconAutoFixer.cs` · `MasterTutorialBeautifier.cs` |
| T2-b | Chỉ 3 quà có hiệu ứng/text | 2 list → 2 component; gift không tạo ô tên; label neo sai | Gộp render qua `UnlockSlotUI`: mọi ô pop + bob + tên; tag MỚI (mở khoá) / ×N (quà) | `LevelUpPopupUI.cs` · `UnlockSlotUI.cs` · `LevelUpGiftSlotUI.cs` |
| T2-c | Pháo hoa mất | Bắn 1 lần 1.5s rồi Destroy | Loop 0.8–1.2s tới khi bấm "Bắt đầu nào" (≤3 burst đồng thời) | `LevelUpPopupUI.cs` |
| T2-d | Nút Bắt đầu nào phẳng | `MasterTutorialBeautifier` ép builtin | Rewire tool gán `btn_big_green` + phục hồi dim 0.65 | `LevelUpPopupRewireTool.cs` |
| T2-e | (phát sinh) popup chưa xem mất khi thoát | RAM only | `LEVELUP_SEEN_MAX` → Play lại hiện tiếp | `LevelUpPopupUI.cs` |
| T3-a | Avatar toàn khung màu code | `SkinKit.BoGoc` 100% | Khung gỗ/giấy/ribbon/card/slot/bar/nút = asset thật (bộ Train + shop_card + slot_normal + hud_avatar_base); code chỉ còn fallback | `Scripts/UI/AvatarProfilePopupUI.cs` · `DecorGrowth/DecorProgressPopupBridge.cs` |
| T3-b | Nút đóng đồng bộ toàn game | 4 sprite + 2 nút code ở 18 chỗ | **Registry** `UIStandardSprites` (1 nơi) + **tool** quét scene DRY/APPLY + 12 điểm code fallback đổi sang registry | `Scripts/UI/UIStandardSprites.cs` (MỚI) · `Editor/CloseButtonSyncTool.cs` (MỚI) · 15 file builder/popup |

**Thống kê:** 41 file sửa/mới (soát ngoặc `{}` cân 41/41; `()` lệch chỉ ở file đã lệch sẵn trong backup, delta bằng nhau) (6 file mới: `UIStandardSprites`, `CloseButtonSyncTool`, `GemCostTextFixTool`, `SeedPanelFixTool`, `TutorialStepTextFixTool`, + tool copy Resources trong CloseButtonSync). Không đổi chữ ký public. Không sửa tay `.unity/.prefab/.asset` — mọi thay đổi scene/prefab/asset đi qua **tool DRY-RUN → APPLY có Undo**.

## 2. Soát chéo của Lead
- Ngoặc `{}` cân 34/34 file; `()` lệch ở 3 file **có sẵn trước khi sửa** (biểu thức nhiều dòng), dòng mới cân.
- Không `using UnityEditor` lọt ra runtime (Editor folder hoặc `#if UNITY_EDITOR`).
- Không asmdef riêng → runtime scripts (Train*, SkinVi) thấy `UIStandardSprites`.
- ⚠ **Build thật**: registry rơi về fallback code cho tới khi chạy menu *Copy sprite chuan vao Resources* (Editor chạy bình thường).
- ⚠ `SeedPanelFixTool` phần **prefab** ghi thẳng (không Undo) — chạy DRY RUN đọc kỹ trước.
- ⚠ Popup Lên Cấp: `Text_MoKhoa`/`Text_Hint` do rewire tool tạo ở y=150/108 (ước lượng) — nếu chồng nút "Bắt đầu nào", kéo lại trong Inspector rồi Ctrl+S.

---

## 3. 🧑 CẦN BẠN — làm ĐÚNG THỨ TỰ trong Unity

**Bước 0 — Compile.** Mở Unity, chờ biên dịch. Nếu có lỗi đỏ → chụp Console gửi Lead (đừng sửa tay).

**Bước 1 — Vòng 17 (nếu chưa):** theo `CHECKLIST_VONG17_2026-09-04.md` 6 bước (sắp lớp UI + khôi phục 10 bước L2) → Ctrl+S.

**Bước 2 — Sprite chuẩn & nút đóng**
```
Tools ▸ Farm ▸ UI ▸ Dong bo nut dong - 1. Copy sprite chuan vao Resources
Tools ▸ Farm ▸ UI ▸ Dong bo nut dong - 2. DRY RUN            ← đọc Console
Tools ▸ Farm ▸ UI ▸ Dong bo nut dong - 3. APPLY
Tools ▸ Farm ▸ UI ▸ Dong bo nut kim cuong - DRY RUN → APPLY
```

**Bước 3 — Tutorial**
```
Tools ▸ Farm Game ▸ Tutorial V2 ▸ ★ Dựng card hội thoại V2 (1 nút)
Tools ▸ Farm Game ▸ Rebuild Tutorial 4 Popups
Tools ▸ Farm ▸ UI ▸ Sua chu MIEN PHI nut gem - DRY RUN → APPLY
Tools ▸ Farm ▸ UI ▸ Sua panel hat giong + hoa - DRY RUN → APPLY   (prefab ghi ngay)
Tools ▸ Farm ▸ Tutorial ▸ Sua text 6 o -> 8 o - DRY RUN → APPLY
Tools ▸ Farm ▸ Tutorial ▸ Kiem tra hat khoi dau (chi bao cao)     ← nếu ⚠: sửa list starterItems trong Inspector về lúa 8 / hướng dương 6
```

**Bước 4 — Popup Lên Cấp**
```
Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nối lại dây popup (DRY-RUN) → (APPLY)
```
⛔ KHÔNG bấm `★ Tự Động Sửa Icon & Gộp Quà…` và `Master Beautify Tutorial…` (đã có dialog cảnh báo).

**Bước 5 — Avatar**
```
Tools ▸ Farm UI ▸ Avatar ▸ Build Task 2 Popup (In Current Scene)
```

**Bước 6 — Ctrl+S.** Rồi `Tools ▸ Farm ▸ ⚠ CHƠI LẠI TỪ ĐẦU`.

**Bước 7 — Play test, F10 mỗi chỗ** (ảnh vào `Assets/_Debug_Capture/capture_*.png`):
1. Card chào: chữ dài `L1L2_02` nằm trong card, nút dưới chữ. NPC không nhảy (talk 6fps).
2. Bảng 4 trang: khung kem, ĐÃ RÕ xanh có sprite, nút gem có icon, trang 4 thẻ đứng yên.
3. Panel hạt: tên + số lượng hiện đủ, không đè HUD; lúa **x8**.
4. **Kéo hạt thật nhanh đủ 8 ô** → bước tự qua (Console `[Tutorial][Gate] … ĐÃ ĐẠT`).
5. Mini-panel cây: "MIỄN PHÍ" 1 dòng trong nút.
6. Lên cấp: 4 nhân vật, mọi ô quà có tên + hiệu ứng, pháo hoa bắn liên tục, nút "Bắt đầu nào" có sprite.
7. Thoát Play giữa tutorial → Play lại → Console `[Tutorial] Resume bước N`.
8. Mở avatar (nút góc trái trên): khung gỗ/card thật, nút đóng đỏ tròn giống Cài đặt. Mở Kho/Chợ/Shop/Mill/Tàu: nút đóng cùng kiểu.
9. **F8** → bảng nhảy bước hiện (không cần gắn component).

**Bước 8 — Gửi đội vẽ:** dán `production/PROMPT_SPRITE_FORGE_UI_PASS_2026-09-05.md` cho GPT (gói A NPC 37 file · gói B 4 minh hoạ). Về hàng → báo Lead nạp qua `★ Nạp art NPC + VFX`.

**Ngoài lề:** máy không còn Python chạy được (`Python311` chỉ còn Lib) → cài lại Python 3.11 + `pip install pillow` để Lead QC ảnh bằng số như trước (không gấp).
