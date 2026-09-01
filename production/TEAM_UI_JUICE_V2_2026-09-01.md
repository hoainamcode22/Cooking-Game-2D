# TEAM UI JUICE V2 — Đồng bộ tiền tệ bay + Pháo hoa xây xong + Popup Lên Cấp V2
> Ngày: 2026-09-01 · Lead điều phối 4 sub-team (FX Dev, UI Dev, Game Designer, VFX Dev)
> Backup trước khi làm: `production/backup_ui_fx_reward_2026-09-01/` (13 script + 29 asset Lever Game)
> Nếu hỏng: copy ngược file từ backup đè lại là về nguyên trạng. KHÔNG commit/push git.

---

## 1. PHÂN TÍCH 3 VIDEO THAM KHẢO (Lead xem frame-by-frame bằng ffmpeg)

### Video 1 — Township: tiền tệ bay về HUD
- Chạm bubble vàng trên công trình → chùm ~6-10 xu **bung radial** ra khỏi điểm chạm (~0.2s, có overshoot),
  khựng nhẹ, rồi **từng xu bay so le** (stagger ~0.05s) theo đường **cong bezier** về icon vàng HUD,
  thu nhỏ dần; icon HUD **nảy** mỗi lần 1 xu chạm; số đếm tăng dần (1748→1768→1798…).
- Cùng một hệ cho MỌI tiền tệ: sao EXP ("+3"), tiền mặt ("+25 von Ernie"), vàng ("+14") — đồng nhất 100%.

### Video 2 — Township: ăn mừng xây xong công trình
- Công trình bọc "vải trắng" + chùm bóng bay trên nóc + bubble "!".
- Chạm → sao EXP "+13" bay lên; **bóng bay TÁCH RA bay CAO dần lên trời**; lớp bọc mở lộ công trình;
  **nhiều đợt confetti/pháo sáng nổ SO LE ở PHÍA TRƯỚC và QUANH nửa trên công trình** (luôn rõ, không bao giờ bị che);
  kèm cụm khói poof ở chân. Tổng ~2.5-3s.

### Video 3 — Family Farm: popup Lên Cấp
- Nền dim tối. Sao vàng to + số level; **mascot nhún nhảy sprite-sheet mượt** trong huy hiệu tròn;
  ruy băng "LEVEL UP"; **sparkle 4 cánh nhấp nháy LIÊN TỤC** quanh badge; tia sáng xoay sau sao.
- Dải quà **6 item** (vàng 100, cá, cà rốt, bơ, bánh, vé ×3) có tag NEW + tooltip khi chạm từng món.
- Nút "Mở quà" pulse; chạm → đóng, quà về HUD.

## 2. CHẨN ĐOÁN PHÁO HOA HIỆN TẠI VÌ SAO XẤU (chi tiết: `production/session-state/CHAN_DOAN_PHAOHOA_2026-09-01.md`)
1. **Nằm SAU công trình**: prefab `LevelUp_Confetti_Lana02` được mượn qua reflection (`ConstructionManager.cs:600-612`)
   nhưng KHÔNG ghi đè sorting của `ParticleSystemRenderer` → hạt giữ layer `Default/0`, vẽ dưới layer `Objects`.
2. **Nhỏ**: `completeVfxScale=40` nhân transform scale — ParticleSystem bỏ qua (scalingMode Local) giữa map 1 ô = 100 unit.
3. **Thấp**: spawn tại tâm-CHÂN công trình, nhà dựng xong đè lên điểm nổ.
4. **Chỉ 1 cụm, không so le** + dây reflection dễ đứt.

## 3. BÀN GIAO CỦA 4 ĐỘI (12 file code + 2 doc)

### Đội A — FX Dev: hệ "RewardFlyFX" thống nhất vàng/gem/exp
| File | Đích | Loại |
|---|---|---|
| RewardFlyFX.cs | `Assets/_Game/Farm/Scripts/UI/` | MỚI — 1 hệ bay cho Gold/Gem/Exp đúng chuẩn Township (burst→khựng→bezier so le→HUD nảy), unscaled time, null-safe |
| RewardIconLibrary.cs | `Assets/_Game/Farm/Scripts/UI/` | MỚI — ScriptableObject icon dùng chung TOÀN GAME (Resources/RewardIconLibrary) |
| PlayerProgressManager.cs | `Assets/_Game/Scripts/Progression/` | CẬP NHẬT (chỉ +9 dòng `[V2 ADD]`: event `OnExpAddedFx` — Lead đã diff xác nhận) |
| RewardFxSetupTool.cs | `Assets/_Game/Farm/Editor/` | MỚI — menu ★ Setup + "Đồng bộ icon vàng (DRY-RUN/APPLY)" quét scene+prefab đổi icon vàng cũ → icon mới |
- RewardFlyFX mặc định **tắt (không xoá)** CoinFlyFX/GemFlyFX cũ lúc runtime (quét cả Awake + Start vì GemFlyFX có Bootstrap AfterSceneLoad). Revert: bỏ tick `disableLegacyFx`.
- Icon mới của đội vẽ sẽ vào `Assets/Art/UI/Currency/icon_gold.png` → chạy lại menu Setup là tự gán.

### Đội B — UI Dev: Popup Lên Cấp V2
| File | Đích | Loại |
|---|---|---|
| CelebrationCharacterSlot.cs | `Assets/_Game/Farm/Scripts/UI/` | MỚI — loop sprite-sheet 12fps + nhún nhảy + squash, frames rỗng tự ẩn |
| LevelUpSparkleFX.cs | `Assets/_Game/Farm/Scripts/UI/` | MỚI — tia sáng xoay 2 lớp + sparkle 4 cánh pool 12 + glow pulse, 100% vẽ runtime không cần art |
| LevelUpTapToClose.cs | `Assets/_Game/Farm/Scripts/UI/` | MỚI — chạm bất kỳ đâu đóng popup (delay 0.8s chống tắt oan), vẫn nhận đủ quà |
| LevelUpPopupUI.cs | `Assets/_Game/Farm/Scripts/UI/` | CẬP NHẬT (additive `[V2 ADD]`; 13 dòng gốc chỉnh đúng 2 chỗ: dải quà co giãn 5-6 item + **ẨN slot unlock không icon** — hết nạn vòng tròn trắng rỗng) |
| LevelUpPopupV2SetupTool.cs | `Assets/_Game/Farm/Editor/` | MỚI — menu ★ Nâng cấp V2 (dựng 4 slot nhân vật 2 trái 2 phải, gán tạm sprite NV01/03/05/07 chạy được NGAY) + menu "Gắn art nhân vật V2" khi art về |

### Đội C — Game Designer: bảng quà L2→L30 (5-6 món/level)
| File | Đích | Loại |
|---|---|---|
| BANG_QUA_LEVELUP_V2_2026-09-01.md | `production/` | Bảng 29 level × 6 mục; vàng+gem GIỮ NGUYÊN 100% bảng cũ đã duyệt (35.990 vàng + 208 gem, chênh 0% — không lạm phát); phần "đầy" bằng vật phẩm tiêu hao theo chủ đề unlock từng level |
| LevelRewardV2FillTool.cs | `Assets/_Game/Farm/Editor/` | MỚI — menu Đổ quà V2 (DRY-RUN/APPLY), 28/29 level đủ ≥5 mục từ pool 11 id ĐÃ XÁC MINH trong project (`seed_ngo, seed_cachua, seed_hoa_hong, seed_nam, seed_sugarcane, seed_lemon, seed_chili, seed_pepper, khoai_tay, ca_rot, mushroom`); id chưa xác minh (trứng/sữa/gạo/cám/vé) tool BỎ QUA + in mục CẦN XÁC NHẬN |

### Đội D — VFX Dev: ăn mừng xây xong V2
| File | Đích | Loại |
|---|---|---|
| ConstructionCelebrationFX.cs | `Assets/_Game/Farm/Scripts/FX/` | MỚI — `Play(building, exp)`: poof khói chân → sao EXP + "+N" bay lên từ ĐỈNH → 4 đợt confetti SO LE (0.2/0.65/1.1/1.5s, 12-18 mảnh/đợt, palette game) render **TRƯỚC công trình** (sortingOrder = max của công trình +100) → 3-5 bóng bay (tái dùng RisingBalloon) bay cao ~550 unit; tự huỷ 3.5s |
| ConstructionManager.cs | `Assets/_Game/Farm/Scripts/Gameplay/` | CẬP NHẬT (+50 dòng, 0 dòng xoá — Lead diff xác nhận): toggle `useCelebrationV2=true`, hiệu ứng cũ bọc early-return chống double |

## 4. CẦN BẠN — LÀM 1 LƯỢT TRONG UNITY (theo đúng thứ tự)
1. Mở Unity, đợi compile → **Console phải 0 lỗi đỏ** (12 file code đã được đặt đúng chỗ).
2. Mở scene farm chính (SCN_Farm) → menu **`Tools/Farm Game/Reward FX/★ Setup Reward Fly FX (1 nút)`** → đọc report → **Ctrl+S**.
3. Menu **`Tools/Farm Game/Level Up Popup/★ Nâng cấp V2 (1 nút)`** → đọc report → **Ctrl+S**.
4. Menu **`Tools/Farm Game/Level Rewards/Đổ quà V2 (DRY-RUN)`** → duyệt danh sách Console → chạy **(APPLY)**.
5. Play test: nhận vàng/gem/exp (bán chợ, thu hoạch) → chùm icon bung + bay về HUD + icon nảy; log "Đã tắt CoinFlyFX/GemFlyFX cũ" phải xuất hiện.
6. Xây 1 công trình có buildTime > 0 → xem pháo hoa V2 (trước công trình, cao, 4 đợt + bóng bay). Test thêm 2 nhà sát nhau.
7. Dùng `Tools/Farm Game/... Debug Preview L2/L5` (có sẵn trong LevelUpPopupUI) xem popup V2: 4 nhân vật tạm nhún nhảy, sparkle, chạm màn hình đóng được, KHÔNG còn vòng tròn trắng rỗng.
8. Gửi file `production/PROMPT_SPRITE_FORGE_UI_JUICE_2026-09-01.md` cho GPT đội vẽ.
9. KHI ART VỀ (`production/art-handoff/2026-09-01_UI_Juice/`): báo Lead 1 câu — Lead copy vào `Assets/Art/UI/...` rồi Sếp chạy 2 menu: `★ Setup Reward Fly FX` (gán icon vàng mới) + `Gắn art nhân vật V2` (thay 4 nhân vật 12 frame) + `Đồng bộ icon vàng (DRY-RUN → APPLY)` để cả game dùng chung icon vàng mới.
10. Xác nhận id còn thiếu cho quà (trứng? sữa? cám gà?): mở `Assets/_Game/Farm/Editor/LevelUpRewardDataSetupTool.cs` hoặc hỏi Lead tra tiếp — sau đó Lead mở khoá thêm mục quà trong bảng V2.

## 5. RỦI RO ĐÃ TÍNH
- FX nhân đôi gem: đã chặn (quét tắt legacy 2 lần Awake+Start).
- Thu hoạch có 2 lớp exp FX (orb world cũ + sao UI mới) — nếu Sếp thấy thừa, bước sau tắt orb (cần duyệt riêng, KHÔNG tự làm).
- "Đồng bộ icon vàng APPLY" với prefab KHÔNG Undo được → luôn DRY-RUN duyệt trước; tool từ chối chạy khi chưa gán icon mới.
- Mọi thay đổi scene do tool tạo — KHÔNG tự save; Sếp Ctrl+S mới ăn.
- Hỏng bất kỳ đâu: lấy file gốc trong `production/backup_ui_fx_reward_2026-09-01/` đè lại.
