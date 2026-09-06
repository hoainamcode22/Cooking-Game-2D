# Project Memory Index

This directory holds persistent project memory files that survive session compaction.

## ⭐ QUY TRÌNH MỖI PHIÊN MỚI (user gõ "tiếp tục roadmap")

1. Đọc `production/ROADMAP_GAME_COMPLETE.md` → xác định sprint kế tiếp + trạng thái.
2. Đọc báo cáo mới nhất trong `production/session-state/` (IMPLEMENTATION_REPORT, MISSIONS_PROPOSAL…).
3. Làm sprint theo workflow: SCAN file liên quan → IMPLEMENT (file tool, không sed) → cập nhật Check tool → ghi nhật ký vào ROADMAP → trả lời kèm mục "ANH CẦN LÀM TRONG UNITY".
4. Luật: không commit/push · không xoá object scene/asset khi chưa duyệt · Console 0 đỏ · economy theo `L1_L10_ECONOMY_TABLE.md` đã duyệt · KHÔNG chạy tool cũ "Setup Village Orders L1-L6/Apply Phase 1 Data" (ghi đè kinh tế).

## Sự thật quan trọng (đừng scan lại từ đầu)

- EXP: `40+10n+n²` (n=level−1), max L30, dư EXP giữ lại. Starter: 400 vàng/15 gem (scene + script đồng bộ).
- ID đặc biệt: nấm thu hoạch = `mushroom` (order đã fix) · cà rốt seed = `ca_rot`, khoai tây seed = `khoai_tay` (KHÔNG có prefix seed_) · 2 món cá unlock 99 (chưa có hệ cá).
- Scene SCN_Farm có **24 HouseOrderController** (nhiều bản trùng tên) — VillageOrderManager gating L1=4→L9=8 nhà theo HouseId; dọn trùng lặp = Sprint 5 (cần duyệt).
- 13 missing script trong scene (chưa rõ vị trí — chạy `Demo L1-L10 → List Missing Scripts`).
- Tutorial 19 bước, mở màn thu hoạch ô chín sẵn (`TutorialPrePlant`, failsafe có sẵn); `FLOWER_PHASE_START_INDEX = 11`.
- Mission system có bug: tracker ghi theo itemId nhưng UI đọc theo missionName → progress không hiện (kế hoạch sửa trong `production/session-state/MISSIONS_L1_L10_PROPOSAL.md`).
- File tools (Read/Edit/Write) là nguồn chính xác; mount bash hay hiển thị file cũ/cụt — chỉ dùng bash để đọc, không sửa.

- Hệ Tàu Khách Du Lịch V2 (2026-08-29): tàu HƯỚNG SỰ KIỆN, không còn đậu 40p cố định — đậu tới khi khách cuối lên tàu
  (lưới an toàn `maxDockMinutes=35`, PHẢI > `patienceMinutes=30` nếu không đường "khách giận" thành code chết).
  Khách: 3-6/chuyến, random 11 prefab NVGAME, món random trong 38 DishData lọc theo unlockLevel, bubble mở LẦN LƯỢT
  hết khách (kiên nhẫn 30p SONG SONG), vàng = Σ giá nguyên liệu CHÍNH ×2 (loại gia vị), EXP = dish.rewardExp.
  Popup boat nằm ở canvas RIÊNG `Canvas_TouristBoatPopup` — ĐỪNG đưa lại vào `canvasPopupRoot` (vào bếp là chết coroutine).
  Sprite khách đã cắt sẵn: `Assets/NV_NPC/NVGAME/Processed/NV01..NV11/NVxx_{down|left|right|up}_{1..3}.png`.
  Mission event phục vụ khách đang TẮT có chủ đích (DeliverOrder là của Bảng Đơn Hàng, bật lên sẽ hoàn thành hộ).
  SETUP: chỉ cần 1 menu `Tools/Farm Game/Tourist Boat/★ SETUP TẤT CẢ (1 nút)` — làm hết mọi thứ, idempotent,
  KHÔNG tự save scene. Toạ độ scene đã đo (dùng lại khi cần): Berth1(-531,-4285) Berth2(151,-4573)
  Berth3(948,-4839) BlindPoint(-9818,-7819) CookingGate(494,-2367) QueueAnchor(400,-2700);
  Grid_Iso45 isometric cellSize(1,0.5) world scale 300 → world=((cx-cy)*0.5*300, (cx+cy)*0.25*300);
- KINH TẾ khách du lịch (V2.1, 2026-08-31): **38 file DishData trong `data/Farm_Cooking/` ĐÃ cân bằng sẵn rất kỹ**
  (difficulty 0/1/2 · unlockLevel 1-30 · sellPrice 62-884 · rewardExp 3-180 · rewardGold = đúng round(sellPrice×0.25)).
  ĐỪNG tự phát minh công thức — dùng lại sellPrice: `vàng = round(sellPrice × diffMult × rarityBonus × touristGoldMultiplier)`
  (Easy 1.00/Normal 1.15/Hard 1.35), `exp = round(rewardExp × expMult × touristExpMultiplier)` với
  **touristExpMultiplier = 0.4 là NÚM HÃM LẠM PHÁT** (nấu đã cộng EXP rồi, phục vụ cộng thêm ⇒ 2× nếu để 1.0,
  hết nội dung game trong 1.2-3.7 giờ). Nhịp tàu 3 mức: 1 bến 5' · 2 bến 7' · 3 bến 10'.
- BÀI HỌC GHÉP NỐI: 2 class không liên quan nằm chung 1 file mà 2 Dev cùng sửa ⇒ copy sai thứ tự thì
  **player build compile SẠCH nhưng chạy code cũ, im lặng hoàn toàn**. Luật: mỗi file một chủ.
- Tool ★ **KHÔNG ghi đè waypoint/QueueAnchor đã kéo tay** (dấu vết băm FNV-1a toạ độ trong EditorPrefs);
  muốn dựng lại phải tick `⚙ Ghi đè waypoint đã chỉnh tay`.
  đường đất=Tilemap_IsoDirt(332 ô), cát=IsoSand(868), cầu tàu=IsoDock(63). Cách parse scene 16MB:
  tách theo regex '--- !u!<cls> &<fid>', cls 1=GameObject(m_Name) cls 4=Transform(pos/father/scale),
  cls 1839735485=Tilemap(m_Tiles first.x/y), cls 156049354=Grid. Nếu waypoint bị kéo tay thì ĐỪNG bấm lại nút ★.

- ⚠️ SORTING LAYER MA (phát hiện 2026-09-06): `TagManager.asset` CHỈ có 5 layer: Bottom(1161173501) · Default(0) ·
  Objects(1471039481) · ObjectsFront(3561676937) · Foreground(1304480043). Nhưng ~20 file .cs gán `"CongTrinh"` và
  38 prefab mang ghost ID `1669604809` — layer này ĐÃ BỊ XOÁ. Layer `"Crop"` và `"FX"` cũng KHÔNG tồn tại.
  Cách vá đã duyệt: dùng `TouristSortingLayers.ResolveOrOverride(ten, TouristSortingLayers.Visitor)` (Visitor={"Objects","Default"}).
  ĐỪNG thêm layer vào cuối TagManager — sẽ đẩy 38 prefab lên trên cả Foreground.
- Rào chuồng: cả 4 chuồng (Pen_01..04) dùng CHUNG 1 file `chuongmoigiasuc.png` 500×500, 1 SpriteRenderer (BarnSprite,
  order 500) phủ cả 4 cạnh ⇒ KHÔNG thể che khuất con vật đúng nếu không tách art 2 lớp (sau/trước). Con vật kẹp order ≥512.
- `UIStandardSprites.Load()` AN TOÀN cho build: thử `Resources.Load` ở `Assets/Resources/UI/Standard/` trước rồi mới fallback
  `SettingsPopupUI.LoadSprite` (Editor-only). Popup mới PHẢI lấy sprite qua đây, CẤM `AssetDatabase.LoadAssetAtPath` trực tiếp.
- Art decor 5 stage: `Assets/Art/Decor/Stages/<slug>/stage_1..5.png`, 5 file cùng canvas & baseline. Thứ tự KHÔNG trực giác:
  stage_3 = THÀNH PHẨM, stage_4 = hộp quà, stage_5 = ăn mừng pháo hoa. Bảng map slug→itemID ở `DecorStageArtTool.cs`.
  Đủ 15/19 slug; thiếu banghieu(3) ghehoa(7) heothantai(8) vitvuive(12).
- `DecorProgressPopupBridge.Build()` là DEAD CODE (không ai gọi) — đừng dựa vào `_panel`/`_blocker` của nó.
  Popup xây dựng thật = `BuildingProcessPopupUI` (canvas riêng sortingOrder 32000).

- ⚠️ SỰ THẬT VÒNG 3 (2026-09-06) — đừng scan lại:
  · `Popup_Train_MasterStation` trong SCN_Farm là **CON của `Popup_LevelUp_Township`**, không phải con `Canvas_Popup`
    (`--- !u!1001 &4105157295546141520`, m_TransformParent fileID 1561892010, m_IsActive 0). Cha tắt ⇒ activeInHierarchy
    luôn false ⇒ popup không bao giờ hiện, mà activeSelf đã true nên click sau rơi vào nhánh toggle ClosePopup().
    Tàu hoả **KHÔNG có gate theo level** (grep sạch requiredLevel/unlockLevel/IsUnlocked = 0 kết quả).
  · `EnsurePopupsExist()` (TrainStationBuilding.cs:126-167) nằm trong `#if UNITY_EDITOR` ⇒ `Popup_train` và
    `Popup_item_Train` **KHÔNG tồn tại trong build thật**, chỉ Editor mới có.
  · `PenSupplyTrayV2.DangMoKhay` là cờ **TOÀN CỤC** (static singleton). Đừng dùng nó trong `PenMiniPanelUI.IsPanelOpen()`
    — chuồng này sẽ dập khay của chuồng kia trong cùng 1 frame. Dùng `DangMoKhayCho(pen)` (thêm 06/09 vòng 3).
    `PenMiniPanelUI._openedAtTime` từng là code chết (khai báo -99f, không ai gán) ⇒ chốt PanelKeepOpenSeconds vô dụng.
  · `remove_debug_logs.ps1` xoá Debug.Log để lại **`if` rỗng không thân**, nuốt luôn câu lệnh dưới nó
    (đã xảy ra ở PenClickDetector.cs:55). Viết mọi Debug.Log gọn trên 1 dòng để tránh.
  · Rào chuồng trong SCENE bị ghi đè `m_SortingLayerID: 0` (=Default) + `m_SortingOrder: 500`, KHÔNG còn ở layer ma nữa.
    Con vật giải ra `Objects` (value 2) > `Default` (value 1) ⇒ luôn vẽ trên rào. 4 chuồng đều `sortingOrderOffset: 50`
    ⇒ order thực 525-588. Bản vá FenceSortingOrderFloor=512 là đúng và đủ. "Đi xuyên rào trước" là giới hạn art 1 lớp.
  · Decor 4 slug `banghieu/ghehoa/heothantai/vitvuive` **THỰC SỰ chưa từng được vẽ** (đã quét 2.852 ảnh + git --all = 0).
    `DecorStageArtTool.BangMap()` chỉ có 15 entry và CỐ Ý bỏ 4 món này ⇒ art về mà không thêm entry thì tool bỏ qua IM LẶNG.
    `Bảng hiệu.asset` (GUID 78991ab7a7541d54a9dd699fefc8e29b) **chưa có trong ShopManager.decorList**.
    **Sếp chốt 06/09: món id 3 "Bảng Hiệu" vẽ thành KỆ GỖ 3 TẦNG ĐỰNG CHẬU CÂY** (khớp art đang chạy PuLbG-removebg-preview.png),
    KHÔNG vẽ bảng gỗ cắm cọc. Đã sửa vào PROMPT_SPRITE_FORGE_2026-09-06.md.
  · `PlacementManager.FixBuildingRenderSorting` (dòng 1161-1174) ép MỌI SpriteRenderer con về Max(order,500) — sẽ san phẳng
    thứ tự tứ chi khi art giao con vật nhiều bộ phận. Cần loại trừ nhánh có SortingGroup.

- ⚠️ SỰ THẬT VÒNG 6 (2026-09-06) — đừng scan lại:
  · Prefab `Popup_Train_MasterStation.prefab` có **5 component TrainStationMasterPopupUI** (4 cái đi lạc trên
    Wagon_1..4). Do `StationWagonSlotUI` NẰM CHUNG FILE với `TrainStationMasterPopupUI.cs` ⇒ Unity ghi 4 component
    toa về `fileID 11500000` (class chính). Mỗi bản đi lạc tự chạy `BuildOrFixHierarchy()` ⇒ đẻ popup ma.
    ĐÂY LÀ TÁI PHÁT CỦA "BÀI HỌC GHÉP NỐI: mỗi file một chủ". Phải tách `StationWagonSlotUI` ra file riêng.
  · Popup tàu **KHÔNG nằm trong SCN_Farm** (đếm GUID script = 0), nó do `EnsurePopupsExist()` sinh runtime
    làm con của `Canvas_Popup`. ⇒ Tiền đề vòng 3 ("popup nằm dưới Popup_LevelUp_Township đang tắt") là SAI,
    vòng lặp bật tổ tiên là no-op. ĐỪNG bảo Sếp kéo popup nữa.
  · `_devForceReplayTutorial` và `_devClearDoneFlagOnStart` trong SCN_Farm dòng 580-582 **được serialize vào
    scene**. Bật là mọi người chơi thật cũng bị dắt lại tutorial. Đã bọc `&& Application.isEditor`.
    Tutorial chỉ phủ cấp 1-2 (18 bước L1L2_* + 10 bước L2_*, cuối là `L2_10_HarvestPen`), không có L3_*/L4_*.
    Gate mới: cấp > 3 và chưa có cờ xong ⇒ tự đánh dấu xong, bỏ qua. Key: `TUTORIAL_MAIN_DONE`, `TUTORIAL_STEP_INDEX`.
  · **CẤM dùng `float` cho giây Unix.** Giây Unix ~1.79e9 nằm giữa 2^30 và 2^31, float32 chỉ 24 bit định trị
    ⇒ bước nhảy **128 giây**. Mọi đồng hồ ngắn hơn 128s sẽ sai hoặc kẹt 00:00. `PenMiniPanelUI.processStartUnix`
    từng là float ⇒ chuồng gà (45s) kẹt 00:00 từ lượt 2, 16/16 kịch bản. Dùng `long` +
    `DateTimeOffset.UtcNow.ToUnixTimeSeconds()` như `PlotController` và `ConstructionManager` đang làm.
    Kiểm chứng nhanh: nút gem hiện 15 = `RushCostFor(0)` ⇒ remaining thật sự = 0, không phải lỗi hiển thị.
  · Mọi `ToString()` số ghi ra PlayerPrefs PHẢI có `CultureInfo.InvariantCulture`. Máy tiếng Việt ghi
    `"1,7886528E+09"` rồi parse Invariant sẽ THẤT BẠI im lặng ⇒ mốc = 0 vĩnh viễn.
  · `UIStandardSprites.Close` = `btn_red_small.png` 256x96 là **thanh đỏ TRƠN KHÔNG có dấu X**. Dấu X ở 8 nút
    trong game là **object con TMP** đè lên (trắng, đậm, cỡ 26). Đừng tưởng sprite có sẵn X.
  · **ĐỪNG BẤM menu `Tools/Farm/UI/Dong bo nut dong - 3. APPLY`** (`CloseButtonSyncTool.cs:185`) — nó ép MỌI
    nút đóng trong scene về 64x64 + sprite btn_red_small, xoá sạch chỉnh tay của cả 8 nút cùng lúc.
  · 6 popup dựng UI bằng code kiểu "huỷ sạch con rồi dựng lại" nên nuốt chỉnh tay: `UnifiedTaskPopupUI`(đã vá),
    `AvatarProfilePopupUI`, `SettingsPopupUI`, `KitchenSceneV2UI`, `Mission/SkinVi`, và tool ở trên.
    Mẫu ĐÚNG để noi theo: `TrainLoadPopupUI.cs` dòng 11+23 (chỉ SerializeField Button rồi AddListener).
  · Dải `Dai_MoKhoa` của popup lên cấp cao 250, ô thưởng 190 ⇒ chỉ còn 30px cho chữ, có RectMask2D xén ngang.
    Bước ô = 206 (190 + spacing 16) nên bảng chữ phải hẹp hơn 206, bản cũ để 214 ⇒ hai nhãn đè nhau 8px.

- ⚠️ SỰ THẬT VÒNG 7 (2026-09-06) — QUAN TRỌNG, đừng lặp lại sai sót:
  · **CÁCH ĐẾM PREFAB TRONG SCENE:** muốn biết prefab có nằm trong scene hay không thì đếm **guid của
    PREFAB** qua `m_SourcePrefab: {fileID: 100100000, guid: <prefab guid>}`. **TUYỆT ĐỐI KHÔNG đếm guid
    của SCRIPT** — prefab instance KHÔNG ghi component ra file scene (kế thừa từ prefab), nên grep guid
    script luôn ra 0 = ÂM TÍNH GIẢ. Vòng 6 đã mắc bẫy này và kết luận sai ngược.
  · `Popup_Train_MasterStation` CÓ trong `SCN_Farm`: prefab guid `c4c6499270a0dd140b6ae1100658b2d6`,
    1 instance `PrefabInstance &4105157295546141520`, `m_TransformParent` = RectTransform của
    `Popup_LevelUp_Township` (fileID 1561892010), `m_IsActive: 0`. Bản vá bật lại tổ tiên là CẦN THIẾT.
  · **NGUYÊN NHÂN GỐC 5 popup ma (đã sửa vòng 7):** Unity chỉ sinh ĐÚNG MỘT script asset cho mỗi file .cs,
    ứng với class trùng tên file (fileID 11500000). Class MonoBehaviour thứ hai chung file thì KHÔNG có
    asset riêng, nên `AddComponent<ClassThuHai>()` bị Unity ghi thành fileID 11500000 = class chính,
    chỉ để lại dấu vết ở `m_EditorClassIdentifier`. Đây là cơ chế biến 4 toa tàu thành 4 popup.
    Tool gây ra: `Export_Train_UI_Package/Editor/TrainPackageBuildTool.cs:376`. Tool KHÔNG hỏng, ý đúng.
    ⇒ ĐÃ tách `StationWagonSlotUI` ra `Scripts/StationWagonSlotUI.cs` (vòng 7). ĐỪNG GỘP LẠI.
    ⇒ Bài học "mỗi file một chủ" áp cho cả MonoBehaviour, không chỉ để tránh 2 dev sửa đè nhau.
  · Đã xoá 4 component ma khỏi prefab (243 block → 239). Toa nay chỉ còn RectTransform + Button; script
    ô toa được gắn lại lúc chạy bởi `TrainStationMasterPopupUI.cs:420` (`GetComponent ?? AddComponent`)
    nên KHÔNG cần chạy lại TrainPackageBuildTool.
  · Quy trình mổ prefab an toàn (dùng lại khi cần): tách block theo regex `^--- !u!(\d+) &(\d+)`, đếm
    tham chiếu `fileID: <fid>` trong CHÍNH prefab và trong MỌI scene trước khi xoá, xoá dòng
    `- component: {fileID: X}` rồi mới xoá block, sau đó kiểm mọi `m_Component` còn lại đều trỏ tới
    block có thật. Có `production/unity_yaml_surgery.py` làm sẵn việc tương tự.

## Entries

- [Tutorial L1→L2 Phase](tutorial_l1l2.md) — EXP shortfall 10, tools created, manual steps remaining (LỖI THỜI một phần — xem ROADMAP Sprint 1b)
