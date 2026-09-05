# 📋 BÁO CÁO UI PASS 2 — 2026-09-05 (tối) · Tech Lead

> Vòng này sửa 4 lỗi Sếp báo sau khi test bản UI Pass 1 (ảnh F10 17:15–18:33): **kẹt sau thu hoạch không qua trồng hoa**, **bấm ĐÃ RÕ không thấy tay ảo ảnh**, **chữ đè nút "Bắt đầu nào"**, **UI đè nhau** (khay hạt/NPC/toast/tay thật).
> Plan đã trình: `production/PLAN_UI_PASS2_2026-09-05.md` · Backup gốc: `production/backup_ui_pass2_2026-09-05/` (12 file + `MD5SUMS.txt`) — hỏng thì xoá file mới, copy ngược.
> 13 file đã ghi về máy (11 sửa + 2 mới), MD5 khớp 100%. **Chưa commit. Chưa compile trong Unity** — Bước 0 của CẦN BẠN.
> Đội: 3 agent SCAN → Lead PLAN → 3 Dev song song → 2 Lead-review chéo (đã vá 2 lỗi rò trạng thái trước khi ghi).

---

## 1. Kết quả theo lỗi Sếp báo

| # | Sếp báo | Nguyên nhân gốc (file:dòng) | Đã sửa | File |
|---|---|---|---|---|
| **K1** | Thu hoạch xong không qua trồng hoa, tay đứng im | **Deadlock**: `TutorialManager.NotifyAction` — tín hiệu "đã thu hoạch" tới đúng lúc popup **Lên Cấp** đang mở → bị cất vào hàng đợi chờ popup đóng, nhưng **không ai lấy ra** khi popup đóng (chỉ đầu bước sau mới tiêu thụ — mà bước sau không bao giờ tới). Ảnh 171917 khớp 100%. Cùng lớp lỗi: bước 07 mở mini-panel cây cũng bị hoãn vì chính mini-panel được tính là popup. | Coroutine `ChoPopupDongRoiTieuThu()`: popup đóng → +0.25s → tiêu thụ hàng đợi. `LaPopupCuaChinhBuoc()`: mini-panel cây là popup *của chính bước* → không hoãn. Bước 11 không chờ 12s nếu popup Lên Cấp vừa đóng. | `TutorialManager.cs` |
| **K2** | Bấm ĐÃ RÕ không thấy bàn tay ảo ảnh | (1) Tay ảo **không có sprite** (lấy `Image` sai object → ô trắng). (2) Gọi demo ở bước 03 khi khay hạt chưa mở → chờ mãi rồi bị huỷ. (3) Bảng 4 trang bị tắt cứng `if (false && …)` + tool đặt `showGuideBoard=false`. (4) Layer ảo ảnh là con `Canvas_Popup` — dễ bị tắt kèm. (5) 3 tay cùng lúc, không có trình tự. | `TutorialPhantomDemoManager` viết lại (859 dòng): sprite tay/liềm/hạt/gem lấy từ asset đang chạy · layer dưới `Tutorial_Canvas` order 450 · **ẩn tay thật khi demo → demo xong hiện lại** · lặp lại sau 8s nếu đứng im (≤3 lần) · chạm màn hình → tắt ngay · thu hoạch demo 2 pha (chạm ô chín → liềm quét ô1→ô2) · gem demo 2 pha (chạm ô → chạm nút gem thật). Gọi ở **05, 06, 07/08, 09, 10, 13, 14, 17**; bỏ ở 03. Bảng ĐÃ RÕ phục hồi cho 03/06b/08b/09b (theo tên bước, không cần sửa asset). Resume ở 05/13 → lùi về 04/12. | `TutorialPhantomDemoManager.cs` · `TutorialManager.cs` · `Editor/SetupTutorialL1L2Tool.cs` |
| **K3** | Chữ đè nút "Bắt đầu nào" | `LevelUpPopupRewireTool.cs:364-366` đặt 2 dòng chữ theo giả định nút neo đáy (sai — nút neo tâm) ⇒ đè đúng nút. Dải quà cách nút 6 px. **Tên dưới ô quà chưa bao giờ hiện** (`UnlockSlotUI.cs:148` neo lệch −99 px, bị mask cắt). 2 lớp dim chồng (`V3_DimBackground` + `Bg_NenToi`) ⇒ tối 87%. Dải trắng chói alpha 1. | Runtime `BoTriVungDuoi()` (idempotent, chạy cuối `PopulateUI`): dải quà y **−215** · **1 dòng gợi ý** kem #FFF5DC (1100×66, autosize 20–28, ≤2 dòng) y −385, nằm *trước* nút · nút y **−500** · bỏ dòng "Mở khoá: …" (trùng tên ô) · tắt `V3_DimBackground` · dải đổi kem (255,243,214,235). Tên ô: neo (0,−2), 1 dòng autosize 12–18. Tool Township/Rewire đồng bộ cùng số. Bỏ Lana Flash ParticleSystem vô ích khi dùng pháo hoa UI. | `LevelUpPopupUI.cs` · `UnlockSlotUI.cs` · `Editor/LevelUpPopupTownshipTool.cs` · `Editor/LevelUpPopupRewireTool.cs` |
| **K4a** | Khay hạt đè 4 nút HUD dưới-trái | `FarmUIManager:364` reset khay về (0,0) → x 113–1806, y 0–240; nút HUD y 22–180 ⇒ 4 ô hạt đầu nằm trên 4 nút, có thể bấm nhầm KHO khi chọn Lúa. | **`HudNavHider`** (mới, static ref-count): khay hạt/khay hoa mở → 4 nút alpha 0 + không nhận tap; đóng → hiện lại. Không cần wire scene. | `HudNavHider.cs` (MỚI) · `SeedPopupController.cs` |
| **K4b** | NPC che nút BẢNG TIN CHỢ / NẤU ĂN | `TutorialV2SetupTool:110` NPC x 420–720 trùng 2 nút; Tutorial_Canvas 250 > HUD 100. | NPC sang **góc dưới-PHẢI** (neo (1,0), (−30,−158)), card neo phải (−350,150). Card mở → HUD mờ 0.35 + không nhận tap (trừ bước L2_01/L2_02 cần bấm Shop). | `Editor/TutorialV2SetupTool.cs` · `TutorialDialogueCard.cs` |
| **K4c** | Toast "đủ hàng… giao đơn" chen giữa tutorial | `AnimalGuideController` poll 5s không kiểm tutorial; toast y=165 trùng khay hạt. | 4 vòng poll + hàng đợi toast **nhường** khi `DangChayTutorial` (hoãn, không mất tip); toast lên y=320. | `AnimalGuideController.cs` |
| **K4d** | Tay thật bị khay hạt / mini-panel che | `Tutorial_Hands` ở canvas 250 < `Canvas_Popup` 300. | Tool **`Lop tay tutorial (440)`** DRY-RUN/APPLY: tạo `Canvas_TutorialHand` (override 440, không raycaster) dời `Tutorial_Hands` + `TutorialV2_Vfx` vào, trỏ resolver `_tutorialCanvas` sang. `Dim_Background` giữ 250 (không nuốt tap). | `Editor/TutorialHandLayerTool.cs` (MỚI) |

## 2. Soát chéo của Lead (2 reviewer độc lập)
- Compile: mọi member gọi tới đều grep có thật (đúng tên/quyền truy cập/chữ ký); ngoặc `{}` `()` cân 13/13 file; không `UnityEditor` lọt runtime; overload mới không mơ hồ; không `yield` trong try/catch; không CS1628.
- Đã vá trước khi ghi: (1) `TutorialPhantomDemoManager.OnDisable` rò trạng thái → gọi `StopDemo()`; (2) `FinishTutorial/SkipTutorialEntirely` không đóng card ⇒ HUD kẹt mờ vĩnh viễn → thêm `AnHopThoai()` + `StopDemo()` ở 2 chỗ và lưới `LateUpdate` trong `TutorialDialogueCard`.
- Line-ending giữ đúng file gốc (CRLF/LF), không BOM.
- ⚠ MINOR chưa đụng (hành vi cũ): bước 08 nếu mini-panel còn mở thì cổng popup vẫn ẩn UI tutorial tới khi panel đóng (không kẹt); sau 45s ở bước bảng, card "Bỏ qua bước" vẫn có thể hiện đè bảng.
- ⚠ Hướng mặt NPC khi sang góc phải chưa xem được ảnh: nếu cô bé quay lưng lại card → đổi `NPC_LAT_X = true` trong `TutorialV2SetupTool.cs` rồi bấm lại nút Dựng.

## 3. 🧑 CẦN BẠN — làm ĐÚNG THỨ TỰ trong Unity

**Bước 0 — Compile.** Mở Unity, chờ biên dịch. Lỗi đỏ → chụp Console gửi Lead (đừng sửa tay).

**Bước 1 — Nếu chưa làm CẦN BẠN của Pass 1** (`BAO_CAO_UI_PASS_2026-09-05.md` §3: Vòng 17, đồng bộ nút đóng, panel hạt, avatar) → làm trước.

**Bước 2 — Tutorial**
```
Tools ▸ Farm Game ▸ Tutorial V2 ▸ ★ Dựng card hội thoại V2 (1 nút)      ← NPC sang góc phải
Tools ▸ Farm ▸ Tutorial ▸ Lop tay tutorial (440) - DRY RUN                 ← đọc Console: 2 object dời + resolver
Tools ▸ Farm ▸ Tutorial ▸ Lop tay tutorial (440) - APPLY
```
(Không cần chạy lại `Setup Tutorial L1L2` — bảng ĐÃ RÕ phục hồi bằng code theo tên bước. Nếu Sếp CÓ chạy lại tool đó thì bấm lại APPLY của "Lop tay tutorial".)

**Bước 3 — Popup Lên Cấp**
```
Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nối lại dây popup (DRY-RUN) → (APPLY)   ← ④c/④d: dải −215, nút −500, Text_Hint, tắt V3_Dim
```
⛔ Vẫn KHÔNG bấm `★ Tự Động Sửa Icon & Gộp Quà…` và `Master Beautify Tutorial…`.

**Bước 4 — Ctrl+S.** Rồi `Tools ▸ Farm ▸ ⚠ CHƠI LẠI TỪ ĐẦU`.

**Bước 5 — Play test, F10 mỗi chỗ** (ảnh vào `Assets/_Debug_Capture/capture_*.png`, Lead sẽ vào xem):
1. Card chào: NPC + card ở góc dưới-PHẢI, không đè 4 nút HUD; 4 nút mờ khi card mở. NPC nhìn về phía card? (không → `NPC_LAT_X = true`).
2. Bước 03 bảng trang 1 → **ĐÃ RÕ** → tay chỉ ô đất → chạm ô → khay hạt mở (**4 nút HUD biến mất**) → **ảo ảnh tay+hạt kéo từ khay vào ô 1** (mờ ~0.75, ~1.7s), tay thật ẩn trong lúc demo rồi hiện; đứng im 8s → demo lặp; chạm → demo tắt ngay.
3. Bước 06: demo kéo vào **ô trống kế tiếp**; trồng đủ 8 ô → qua bước (Console `ĐÃ ĐẠT`).
4. Bước 07/08: chạm ô → mini-panel → **qua 08 ngay** (Console không có `Hoãn 'WaitForOpenCropProcess'`), demo chạm nút kim cương thật; "MIỄN PHÍ" 1 dòng trong nút.
5. Bước 09: bảng trang 3 → ĐÃ RÕ → demo chạm ô chín → liềm quét ô1→ô2 → Sếp quẹt thật. **Nếu popup Lên Cấp bật lúc này → bấm "Bắt đầu nào" → Console `[Tutorial][Gate] Popup đóng → tiêu thụ hàng đợi` → tutorial chạy tiếp** (đây là chỗ kẹt cũ).
6. Bước 10 → 11 → **trồng hoa**: camera sang chậu hoa, tay chỉ chậu, khay hoa mở (HUD ẩn), demo kéo hạt hướng dương.
7. Popup Lên Cấp 2: dải quà kem cách nút rõ, **1 dòng gợi ý** giữa dải và nút, nút "Bắt đầu nào" thấp hơn, **mỗi ô có tên** dưới ô, nền tối 1 lớp (~65%), pháo hoa lặp tới khi bấm.
8. Trong tutorial không có toast "đủ hàng…"; sau tutorial toast hiện ở y=320 (trên khay).
9. Thoát Play giữa bước 05 → Play lại → Console `lùi về bước 04`, tay chỉ ô đất.

**Bước 6 — Gửi đội vẽ:** vòng này **không cần asset mới** (tay/liềm/hạt/gem lấy từ asset đang chạy). Prompt vòng trước `production/PROMPT_SPRITE_FORGE_UI_PASS_2026-09-05.md` (gói A NPC 37 file — hết "nhảy tới lui" · gói B 4 minh hoạ bảng) vẫn còn hiệu lực — nếu chưa gửi GPT thì gửi; về hàng báo Lead nạp.

## 4. Thống kê
- 11 file sửa + 2 file mới (`HudNavHider.cs`, `TutorialHandLayerTool.cs`). Không đổi chữ ký public có sẵn (chỉ thêm overload/public static mới: `TutorialManager.TimIdOConViec`, `TutorialPhantomDemoManager.Play*(…, Action onDone)`). Không sửa tay `.unity/.prefab/.asset`.
- Menu tool mới: `Tools/Farm/Tutorial/Lop tay tutorial (440) - DRY RUN` · `... - APPLY`.
