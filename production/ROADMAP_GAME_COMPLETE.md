# ROADMAP — HOÀN THIỆN GAME NÔNG TRẠI (L1→L30)

> Quy trình tự động: mỗi phiên mới, chỉ cần gõ **"tiếp tục roadmap"** — đội agent sẽ đọc file này + `memory/MEMORY.md` + báo cáo phiên gần nhất trong `production/session-state/`, tự nhận sprint kế tiếp, làm việc theo workflow SCAN → IMPLEMENT → CHECK TOOL → REPORT, cập nhật trạng thái vào đây.
> Luật cố định: không commit/push · không xoá logic-scene khi chưa duyệt · mọi data đổi qua tool/batch có verify · Console 0 error đỏ · sau mỗi sprint phải có mục "ANH CẦN LÀM TRONG UNITY".

## Trạng thái tổng

| Sprint | Nội dung | Trạng thái | Phiên |
|--------|----------|------------|-------|
| 0 | Scan + 3 báo cáo + duyệt kinh tế | ✅ XONG | 2026-06-12 |
| 1 | Kinh tế L1-L10 + shop/village gating + cooking gate + hotfix 2 bug + tool Demo L1-L10 | ✅ XONG (đã verify in-game: 400/15, đơn 26g, 4 nhà) | 2026-06-12 |
| 1b | Tutorial Feel Pack (Hay Day opening) + LevelReward L2-L30 | ✅ CODE XONG — **chờ anh chạy tool + playtest** | 2026-06-12 |
| 2 | Mission system (sửa bug tracker key + mission L1-L10 + daily L6) | ✅ CODE XONG — chờ anh chạy `Setup Missions L1-L10` + test | 2026-06-12 |
| 3 | Animal guide L2-L8 (toast gà→heo→bò→bò sữa) + đơn sữa | ✅ CODE XONG — Setup All tự gắn | 2026-06-12 |
| 4 | VFX: coin-fly về ví + fix DDOL warnings | ✅ CODE XONG (còn: audio thật, ảnh từ anh) | 2026-06-12 |
| 5 | Dọn scene: 24 nhà trùng lặp, 13 missing script, tool rác (Phase 13) | ⬜ cần duyệt danh sách xoá | |
| 6 | Content L11-L15: 3 máy chế biến (xay bột L11, ép mía L13, phô mai L15) | ✅ CODE XONG — chạy tool Setup Production Machines (còn: mở rộng plot, daily spin) | 2026-06-12 |
| 7 | Content L16-L22: hồ cá (mở 2 món cá), pet/trang trí nâng cao, event đơn giản | ⬜ | |
| 8 | Content L23-L30: tourist boat (Phase 14), nhà hàng ven biển, sự kiện mùa | ⬜ design doc trước | |
| 9 | Full playtest L1-L30 + cân bằng + build EXE/APK | ⬜ | |

## Việc anh hỗ trợ (ảnh/asset — làm dần, không gấp)

1. NPC portrait (mascot dẫn tutorial — kiểu bù nhìn/cô nông dân, PNG nền trong, ~512px)
2. 4 ảnh minh hoạ guide board (gieo hạt → tăng tốc → thu hoạch → nhận thưởng)
3. Icon Chuồng Bò Sữa (đang dùng tạm icon Chuồng Bò)
4. 3 file âm thanh: coin.mp3, plant.mp3, cook_start.mp3 (+ tuỳ chọn: harvest, bubble-pop)
5. Sprite coin bay (hoặc để team dùng icon vàng hiện có)
6. Font TMP hỗ trợ tiếng Việt đầy đủ nếu sau khi chạy tutorial mới thấy ô vuông □ (báo lại team)

## Định nghĩa "game hoàn chỉnh" (điều kiện đóng roadmap)

Chơi liền mạch L1→L30 không kẹt tiền/kẹt đơn · tutorial L1 chuẩn Hay Day + animal tutorial L2-L4 · popup level-up đủ L2-L30 có pháo hoa · mission chính + daily chạy và nhận thưởng được · cooking 18 món mở dần (20 khi có hồ cá) · shop khoá/mở đúng từng level · 8 nhà mở dần + tourist boat hoạt động · VFX coin/EXP fly + audio đủ · Console 0 đỏ · build chạy ngoài Editor.

## Nhật ký sprint (agent tự ghi thêm mỗi phiên)

### Hệ Tàu Hỏa — 2026-08-26 (hợp nhất logic + UI package, QA pass)
- Hợp nhất 2 hệ (TrainManager cũ vs Export_Train_UI_Package của sprite-forge) về 1 nguồn sự thật:
  popup = view thuần đọc TrainManager; bỏ toàn bộ data giả TrainItemDatabase + SendMessage hack.
- Khôi phục state Processing: timer unix 10-15 phút (Inspector `tripDurationSeconds`=600), chạy nền + offline,
  persistence PlayerPrefs `train_trip_state_v1`, mọi nhánh restore tự thoát kẹt (đã vá deadlock M1 theo QA).
- TĂNG TỐC trừ kim cương thật theo RushCostFor (trước đây skip MIỄN PHÍ). Thưởng = vật liệu asset + 80 vàng/chuyến
  (bỏ 450 vàng + 8 gem chưa duyệt). Thu thưởng check kho đầy + EXP + FX + mission event như cũ.
- Input lock đủ cho 3 popup mới; sprite build-safe (TrainSpriteLoader.Assign — hết trắng UI khi build).
- Báo cáo đầy đủ + việc cần anh: `production/session-state/TRAIN_SYSTEM_IMPLEMENTATION_REPORT.md`.
  Prompt assets world bổ sung cho GPT/sprite-forge: `production/session-state/PROMPT_SPRITE_FORGE_TRAIN_ASSETS.md`.
  Backup 6 file gốc: `production/backup_train_2026-08-26/`.

### Sprint 3+4+6 — 2026-06-12 (phiên 4, 3 agent song song)
- **Animal guide** (`AnimalGuideController.cs` — KHÔNG đụng TutorialManager): toast hướng dẫn theo level (L2 gà → L4 heo → L6 bò → L8 bò sữa), toast "cho gà ăn" khi đặt chuồng đầu tiên, chống spam khi nhảy cấp. UI toast tự tạo runtime, không cần prefab.
- **OrderItem_Milk** (L8, 45g/đv) + SetupAll giờ **tự đồng bộ TẤT CẢ OrderItemDefinition** vào VillageOrderManager.availableItems — order item mới chỉ cần tạo asset + chạy Setup All.
- **CoinFlyFX**: vàng bay về icon Vangicon trên HUD mỗi lần nhận vàng (event `OnGoldAddedFx`), tự wire qua Setup All, fallback an toàn nếu thiếu icon/sprite. Fix 4 warning DontDestroyOnLoad (SetParent null).
- **3 Máy chế biến** (tái dùng hệ chuồng): Máy Xay Bột 2500g/L11 (lúa→2 bột gạo 60s), Máy Ép Mía 3000g/L13 (mía→2 nước mía 90s), Máy Phô Mai 3500g/L15 (sữa→2 phô mai 120s — chuỗi 3 bước!). Tool `Setup Production Machines L11-L15` tự clone prefab Pen_04 → May_01..03, swap config, đăng ký shop + kho. 3 OrderItem mới (70/95/130g). Icon sản phẩm chờ anh vẽ.
- File mới: AnimalGuideController.cs, CoinFlyFX.cs, ProductionMachineSetupTool.cs + 14 asset data máy + OrderItem_Milk + 3 OrderItem máy.

### Sprint 2 + Preview tooling — 2026-06-12 (phiên 3)
- **Mission system hoàn chỉnh**: fix bug tracker (key itemId vs missionName), `MissionEventType` 9 loại, `ReportEvent` + persistence PlayerPrefs + `OnProgressChanged` realtime, hook 6 điểm gameplay (harvest/plant/deliver/cook/feed/collect/buy), PopupEwar lọc theo level, claim lưu vĩnh viễn, daily tự reset theo ngày (data sẵn, UI tab = TODO).
- Tool mới: `Tools → Farm Game → Setup Missions L1-L10` (tạo 23 mission chính L1-L10 + 6 daily + MissionDatabase_Daily; 20 mission mẫu cũ bị thay khỏi database chính) + `Test → Check Missions L1-L10`.
- **Preview tooling** (trả lời "không thấy khung đâu"): `Demo L1-L10 → Preview → Bật khung Level-Up Popup / Bật khung Guide Board / Tắt hết Preview` — bật khung ngay trong Editor để gắn ảnh, tự ping + select trong Hierarchy, log rõ đường dẫn gắn 4 ảnh minh hoạ + NPC portrait. Setup All giờ tự ping khung popup sau khi chạy.
- File sửa: MissionData.cs, MissionProgressTracker.cs, PopupEwarManager.cs, MissionItemUI.cs, PlotController.cs, VillageOrderManager.cs (hook delivery), CookingChallengeManager.cs, PenMiniPanelUI.cs, ShopItemUI.cs, MarketManager.cs, DemoL1L10Tool.cs + MissionSetupTool.cs (mới).

### Sprint 1b — 2026-06-12
- Tutorial 19 bước kiểu Hay Day: chèn `L1L2_04b_FirstHarvest` (1 ô lúa chín sẵn — `TutorialPrePlant.cs`, có failsafe tự bỏ qua nếu không tạo được ô chín), text có dấu + ngắn + typing 0.02, camera zoom 5→3.5, `FLOWER_PHASE_START_INDEX` 10→11, EXP 5+30+10=45≥40.
- `LevelUpRewardDataSetupTool` sinh bảng L11-L30 tự động (vàng 700→2600, gem theo band, quà hạt xoay vòng, teaser tính năng tương lai). Menu mới: **Setup Reward Data (L2-L30)**.
- `DemoL1L10Tool`: Setup All gọi đúng menu mới + check info L11-L30.
- File sửa: SetupTutorialL1L2Tool.cs, TutorialManager.cs, TutorialPrePlant.cs, LevelUpRewardDataSetupTool.cs, DemoL1L10Tool.cs.
