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
| 7b | **Tàu khách du lịch V2** (khách lên bờ, xếp hàng, đặt món, trả vàng+EXP, popup báo tàu, mua slot bến) | ✅ CODE XONG + QA SHIP — chờ Sếp chạy tool + kéo waypoint + playtest | 2026-08-29 |
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

### Hệ Tàu Khách Du Lịch V2 — 2026-08-29 (3 Dev song song + QA 2 vòng, verdict SHIP)
- Chuyển hệ boat từ CHU KỲ CỐ ĐỊNH (đậu 40p) sang HƯỚNG SỰ KIỆN: tàu cập bến → bắc ván gỗ → 3-6 khách
  du lịch (random 11 nhân vật NVGAME) xuống tàu → đi theo waypoint đường đất → xếp hàng trước cooking →
  bubble món mở LẦN LƯỢT hết khách (stagger 0.4s) → tap giao món (bất kỳ khách nào, không cần đúng thứ tự) →
  thưởng + mặt cười bay lên HUD → khách về tàu → khách cuối lên tàu thì tàu rời bến → chuyến kế 5p (1 bến) /
  10p so le (nhiều bến).
- Kinh tế (Sếp chốt): vàng = Σ giá nguyên liệu CHÍNH × 2 (loại gia vị), EXP = dish.rewardExp, món random trong
  38 DishData lọc theo unlockLevel. Kiên nhẫn 30p/khách chạy SONG SONG (UTC, offline vẫn trôi); hết giờ =
  MẶT TỨC GIẬN, bỏ về tàu, không trả tiền. Lưới an toàn maxDockMinutes=35 → tàu tự rời bến, hệ không bao giờ kẹt.
- 21 file C# / 11.161 dòng: Dev A lịch tàu V2 (BoatScheduleCore/BoatDockManager/Config/Controller +
  BoatShoreAdjustTool + vá TouristBoatDiagnosticTool) · Dev B khách du lịch (VisitorManager/Agent/Queue/Bubble/
  SmileyFX/Gangplank + NPCAnimationSetupTool + TouristVisitorSetupTool) · Dev C UI (popup báo tàu, popup mua slot,
  FX mở bến, rework UnlockFlow + BoatDockSlot, TouristBoatUIPopupSetupTool).
- Pipeline art: 11 sheet NVGAME (lưới 4x3 nền trắng) → 132 frame đã xóa phông + chuẩn hoá canvas + pivot
  bottom-center tại `Assets/NV_NPC/NVGAME/Processed/NV01..NV11/`.
- QA: compile 3 pass 0 error/0 warning · test console 119 PASS/0 FAIL · vòng 1 tìm 4 BLOCKING + 6 MAJOR + 11 minor
  (kẹt tàu vĩnh viễn, mất món trả 0 vàng, popup chết sau khi vào bếp, không tua nhanh test được) → vòng 2 đóng 21/21.
- Backup 9 file gốc: `production/backup_boat_2026-08-29/`. Báo cáo: `production/session-state/BOAT_V2_IMPLEMENTATION_REPORT.md`
  (mục 4 = ANH CẦN LÀM TRONG UNITY) · QA + checklist Play Mode 50 bước: `production/session-state/QA_REPORT_BOAT_V2.md` §7.8 ·
  Prompt đội vẽ 15 asset: `production/session-state/PROMPT_SPRITE_FORGE_BOAT_V2.md`.
- **BẢN CUỐI (cùng phiên): gộp thành MỘT NÚT** `Tools/Farm Game/Tourist Boat/★ SETUP TẤT CẢ (1 nút)` —
  tự điền 13 field config (maxDockMinutes=35), import 132 sprite + 88 clip + 11 prefab khách, dựng scene,
  GHI ĐÈ toạ độ waypoint + QueueAnchor đo thật từ scene (QueueAnchor (400,-2700); Dock1/2/3 mỗi bến 3 WP),
  dựng 2 popup, dịch 3 bến sát bờ +200Y, tự kiểm tra 5 nhóm rồi in 1 bảng tổng kết. Idempotent.
  Toạ độ nguồn (parse từ SCN_Farm.unity): Berth1(-531,-4285) Berth2(151,-4573) Berth3(948,-4839)
  BlindPoint(-9818,-7819) CookingGate(494,-2367); Grid_Iso45 iso cellSize(1,0.5) scale tích luỹ 300;
  đường đất = Tilemap_IsoDirt (332 ô).
- **BẢN VÁ 3 (2026-08-31) — Lead toàn quyền quyết 4 điểm lệch, đã thực thi + QA vòng 3:**
  1. **Công thức thưởng V2.1**: bỏ `Σ nguyên liệu ×2` (nó trả 50 vàng cho khoai tây chiên khi bán chợ được 95 —
     phục vụ khách LỖ hơn bán chợ). Nay dùng chính bảng 38 món đã cân bằng của Sếp:
     `vàng = round(sellPrice × diffMult × rarityBonus × touristGoldMultiplier)` (Easy 1.00 / Normal 1.15 / Hard 1.35;
     rarity = 1 + 0.05×nRare + 0.12×nEpic, kẹp 1.5) · `exp = round(rewardExp × expMult × touristExpMultiplier)`.
     Ví dụ: com_chien_bap_cai 62 vàng · bo_xao_tieu 362 · pho_bo_tai 540 · salad_dua_hau_bo_ap_chao 1193.
  2. **Nhịp tàu 3 mức**: 1 bến 5' · 2 bến 7' · 3 bến 10' (Sếp nói 10' là khi mở ĐỦ 3 slot). Field mới `gapTwoDockMinutes`.
  3. **Mặt cười**: bỏ hẳn nhánh bay về tâm màn hình; 3 nhánh `hudGoldTarget` wire cứng → dò tên → bay thẳng lên trời.
     Scale 0.4→1.5, fade từ t=0.45.
  4. **Fade-in 0.25s** khi khách xuống tàu (đối xứng fade-out), trên mọi SpriteRenderer con.
  Phụ: seed **SplitMix64** (QA đo: 3 bến trùng số khách 31.15%→6.26%, kỳ vọng 6.25%) · waypoint **tự bám
  `Tilemap_IsoDirt` bằng Dijkstra + Douglas-Peucker** (cost đất1/dock2/cát5/cỏ9, fallback 3 mốc thẳng) ·
  typewriter 0.02s/ký tự cho popup báo tàu.
- **QA vòng 3 bắt 2 lỗi Lead chưa lường, đã sửa:**
  · **B-6**: Dev A và Dev B ship TRÙNG file `TouristSmileyFlyFX.cs` (2 class 2 chủ) — copy A→B thì player build
    compile SẠCH nhưng chạy công thức thưởng CŨ, im lặng hoàn toàn. Đã **tách `TouristRewardCalculator` ra file riêng**.
  · **M-9 lạm phát EXP**: nấu đã cộng `rewardExp × hệ số` rồi phục vụ cộng THÊM ⇒ 2× EXP thiết kế, hết nội dung
    game trong 1.2-3.7 giờ. Đã thêm núm hãm **`touristExpMultiplier = 0.4`** (pho_bo_tai 68→27 EXP).
  · **M-8**: tool ★ ghi đè waypoint Sếp kéo tay dù HANDOFF nói không. Nay dùng **dấu vết băm FNV-1a toạ độ**
    trong EditorPrefs: chỉ đặt lại khi waypoint còn đúng chỗ tool sinh, đã kéo tay thì GIỮ NGUYÊN + log;
    muốn dựng lại phải tick ô `⚙ Ghi đè waypoint đã chỉnh tay`. QueueAnchor cũng được bảo vệ y hệt.
  · Giá bến 2: QA đề nghị tăng 2000→6000-8000 vì vàng dồi dào hơn. **Lead GIỮ 2000** (nằm trong GDD BOAT-001
    đã duyệt; cổ chai thật là cái bếp, phải nấu 4.5 món/5 phút mới đạt trần lý thuyết). Con số ghi trong HANDOFF
    để Sếp tự quyết sau khi chơi thật.
- Test: lịch tàu **127/127** · công thức thưởng **64/64** · compile 3 pass 0 error. Tool mới:
  `Xuất bảng thưởng khách (38 món)` → ghi `production/session-state/BANG_THUONG_KHACH_DU_LICH.md`.
- Sửa ngoài lề cùng phiên: **LockIcon** scale 750 sprite Knob (cái đĩa vàng che map) → tắt LockUI 3 bến đã mở +
  thu icon về 60 · **10 file hoa** 50/50/50 → **22/30/35** (hoa mới nhú đang to bằng lúc chín, rau thì 35→40→50).
  Backup: `production/backup_lockicon_2026-08-30/`, `production/backup_hoa_2026-08-30/`.
- CẦN SẾP: mở SCN_Farm → bấm nút ★ → Ctrl+S → Play test. Hướng dẫn: `production/HUONG_DAN_BAM_NUT_TAU_KHACH.md`.


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
