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

- ⚠️ SỰ THẬT VÒNG 10 (2026-09-06) — HỆ LƯỚI & PLACE, đừng scan lại:
  · **Ô LƯỚI THẬT LÀ 300 x 150 world**, KHÔNG phải 150 x 75. `Grid_Iso45` scale 150 nhưng **9 tilemap mặt đất
    con đều localScale 2** (GroundBase_Dirt, Tilemap_IsoGrass/IsoDirt/IsoRock/IsoStone/IsoDirtPatch/IsoSand/
    IsoDock/IsoFence) ⇒ 150×2 = 300. Xác minh nguồn 2: `Sheet_IsoGrass45.png` PPU 128, thoi 128x64.
    Ghi chú cũ "scale 300" đúng (ô NHÌN THẤY); 150 là ô CODE dùng. `IsoGrid` đã sửa, tự đo hệ số từ scene.
  · 🚫 **TUYỆT ĐỐI KHÔNG đổi scale `Grid_Iso45`** — 9 tilemap mặt đất là CON của nó, đổi là phình cả map.
    Lead đã chặn đề xuất 150→350 ở vòng 10.
  · **`gridSize` trong PlaceableItemData từng lưu ĐƠN VỊ ART, không phải số ô.** Home1 = 341x342 = 116.622 ô.
    Đã điền lại 32/37 asset: nhà + 19 decor = **1x1**, 4 chuồng + 3 máy = **2x2**, Chậu Hoa + Đất giữ 1x1.
    Mốc neo: `Đất` (tile_dirt.png) art 350x172.5 = đúng 1 viên thoi, gridSize 1x1.
  · **NGUỒN SINH SỐ RÁC (đã bịt cả 3):** `BuildingFootprintKit.cs:334` `Ceil(bounds/CELL)` với CELL=100 —
    House_01 collider 341x342 ÷ 100 = đúng gridSize rác, VÒNG LẶP TỰ NUÔI. Cộng `Editor/BuildingGridSizeTool`
    (menu bấm vào là đầu độc lại cả 37 asset) và `FX/ConstructionCelebrationFX.cs:87` (CHÉP hằng 100f nên
    `[Obsolete]` không bao giờ bắt được).
  · `PlacementManager.CELL = 100f` là lưới VUÔNG của **save v0/v1**. **ĐỪNG xoá, ĐỪNG đổi thành 300** —
    `MigrateAnchorV0ToV1` (dòng ~1609) cần đúng số 100 vì nó nằm sẵn trong save trên đĩa. Đã tách
    `LegacyCellV0V1 = 100f` cho chỗ đó, `CELL` giữ `[Obsolete]` để bắt chỗ còn sót.
  · **BẪY LOCAL vs WORLD:** `IsoPlacementPreview` từng `InverseTransformPoint` tâm ô ra LOCAL rồi cộng nửa
    chiều rộng đang là WORLD ⇒ mỗi ô vẽ TO GẤP 150 LẦN (vì nó là con Grid_Iso45 scale 150).
    Luật: dựng đủ 4 đỉnh trong WORLD rồi mới `InverseTransformPoint` TỪNG đỉnh. `IsoGridOverlay` làm đúng cách này.
  · `ConstructionArtKit.priceBarBg` từng trỏ vào **`btn_CloseRanking.png`** (nút Close bảng xếp hạng!)
    1179x211 border 0, Sliced border 0 = kéo giãn phẳng ⇒ góc bo bake trong art méo thành góc cứng.
    Đã đặt về None. Bài học: kiểm `spriteBorder` trước khi dùng `Image.Type.Sliced`.
  · `enforceLandBounds` đã TẮT (vòng 10, Sếp yêu cầu): hệ mua đất chưa có biển giá (`signPrefab` null) nên
    người chơi không có cách nào mua, mà nông trại lại nằm trong `region_2_0` đang khoá.
  · **HUD: hai cụm cha đã sát rìa 12 px và 14 px, không kéo ra thêm được** — muốn "đừng ra giữa" thì phải thu
    mép TRONG. Thủ phạm đè nhau là `JuicyPulseFX` phóng 1.20-1.25 từ 5 nơi. 4:3 (iPad, targetDevice 2)
    từng đè 8.1 px. `RewardFlyFX:604` phóng Icon_Gold (con, đã scale 1.2) thêm 1.25 = NHÂN CHỒNG.
  · Canvas world-space (card place) phải **bù zoom** theo `orthographicSize / 750`, camera ortho 400-1500,
    không bù thì zoom hết ra chữ teo một nửa (font 38 còn 13.7 px).
  · `btn_close.png` KHÔNG có trong `Resources/UI/Standard/` và không trong `UIStandardSprites.AllPaths`
    ⇒ **build thật trả null**. `UIStandardSprites.CardOuter/CardInner` thì CÓ (border 30 và 28), dùng được.
  · 3 tool PHÁ HOẠI, ĐỪNG BẤM: `Editor/CloseButtonSyncTool` · `Editor/TownshipHUDBuilderTool` ·
    `Editor/BuildingGridSizeTool` (đã vá công thức nhưng bản đo vẫn đo cả viền trong suốt).

## Entries

- [Tutorial L1→L2 Phase](tutorial_l1l2.md) — EXP shortfall 10, tools created, manual steps remaining (LỖI THỜI một phần — xem ROADMAP Sprint 1b)

- ⚠️ SỰ THẬT VÒNG 12 (2026-09-07) — HỒ CÂU (SCN_Fishing), đừng scan lại:
  · Module tự chứa `Assets/_Game/Fishing/` (69 .cs, namespace FarmGame.Fishing), scene `Assets/_Game/Scenes/SCN_Fishing.unity` do tool ★
    `Tools/Farm Game/Hồ Câu/★ SETUP TẤT CẢ (1 nút)` tạo và LƯU (scene tool sở hữu). Gắn vào SCN_Farm = menu 5 riêng, KHÔNG tự save.
  · World scene câu = **1 unit chuẩn** (1 ô iso 1×0.5, PPU 100, nhân vật cao 0.6), KHÔNG chép hệ ×150 farm. Farm → câu = Single load qua
    SceneTransitionManager, `SaveSystem.Save("fishing-enter")` trước. Về farm = LoadScene("SCN_Farm") như từ SCN_Home.
  · Kế hoạch `production/PLAN_HO_CAU_2026-09-07.md`, hợp đồng API `production/FISHING_DEV_INTERFACES.md`, facts `production/FISHING_DEV_FACTS.md`.
  · Id cá PHẢI tiền tố `fish_` ("ca" nằm trong DeadItemIds của KitchenTransferManager và DeadKeySubstrings của MissionProgressTracker).
  · `PopupManager.IsAnyPopupOpen()` đã cộng `FishingEntryPopupUI.AnyOpen || FishCounterPopupUI.AnyOpen` — BẮT BUỘC vì PopupManager.LateUpdate
    gọi `FarmInputLock.ResetAll()` mỗi frame khi không thấy popup nào mở (popup mới không đăng ký = click xuyên popup). Popup Hồ Câu nào
    thêm sau cũng phải cộng vào đây.
  · Save: khoá `FISH_BASKET_SAVE`, `FISHING_GEAR_SAVE`, `FISHING_PROFILE_SAVE` đã vào `SaveAdapters.StringKeys`; họ `FISHING` vào
    `SaveVersionGuard.AllFamilies`. `FISHING_FRIENDS_LOCAL` là khoá offline tạm, không mirror.
  · Nhân vật PlayerF/PlayerM: 24 PNG `Assets/_Game/Fishing/Art/Characters/{Char}/{Char}_{down|left|right|up}_{1..3}.png`.
    **Idle down/up = frame 1, left/right = frame 2** (đo thật). Animator param `DirX/DirY/IsMoving/IsFishing` (KHÔNG MoveX/MoveY).
    Prefab dùng chung local/remote: `FishingPlayerController` tự tắt khi là con của `RemotePlayerView`.
  · Tầng mạng: UI chỉ gọi `FishingNetHub.Room/Chat/Friends` (interface). Offline = Local* (bot theo `cfg.offlineBotCount`).
    Online Firebase: project `possible-jetty-436317-c2`, DTO `PlayerNetState` map thẳng `rooms/{roomId}/players/{uid}`.
  · Cần hết độ bền: lần quăng cuối VẪN câu được, báo "Cần đã hỏng" khi về Idle. `TryBuy` không gọi PlayBuySell (SpendGold đã kêu).
  · Backup 3 file cũ vòng 12: `production/backup_vong12_2026-09-07/lead/*.bak`. Xoá `Assets/_Game/Fishing` + hoàn 3 file = về nguyên trạng.

## VÒNG 13 (07/09/2026) — HÌNH HỌC Ô LƯỚI, ANCHOR ART, COLLIDER
- **LỚP LỖI "SỐ ART LỌT VÀO Ô LOCAL" ĐÃ NỔ LẦN THỨ 4-5.** Vòng 13 tìm thêm **23 prefab** có
  `BoxCollider2D` từ **10.600 → 151.400 world** (`khungtrongchauhoa_0` = 505×1010 ô) và `May_01..03`
  vẫn còn **41.300** (vòng 12 chỉ sửa 4 chuồng). Đã nắn hết. **Quy tắc: mọi số trong `m_Size`/`m_Offset`/
  `soO` của prefab công trình mà > 50 đều là số rác** — root scale 100 nên local 413 ⇒ 41.300 world.
- **`BuildingFootprintKit` dòng 220 dùng `soO` KHÔNG KẸP TRẦN.** Trần `TranSoO = 24` CHỈ áp khi `soO`
  để trống (0,0). `soO` điền sai số to là thảm nền vẽ phủ hàng trăm ô. Đừng tin vào trần đó.
- **`m_Size` của SpriteRenderer là CACHE, thường ĐÃ CŨ.** Muốn biết cỡ art thật phải đọc
  `spriteSheet.sprites[].rect` trong `.png.meta` và khớp `internalID` với `m_Sprite: {fileID: ...}`
  của prefab. `fileID 21300000` = sprite đơn (dùng cả texture); số lớn = sub-sprite trong sheet.
  Ví dụ Pen_03 cache `28.58 x 14.72` (⇒ 4287 world, báo động giả) nhưng sub-sprite thật là
  `4.13 x 2.98` ⇒ **619,5 x 447 world** — chuồng KHÔNG hề to.
- **QUY ƯỚC PIVOT CỦA DỰ ÁN = ĐÁY SPRITE** (`PlacementManager.cs:284,408`;
  `DecorGrowthController.cs:650` cũng đã giả định vậy). Nhà + chuồng + máy tuân thủ.
  **19 decor + Chauhoa_1..4 + Khung Hoa đang pivot GIỮA ⇒ art bị vẽ tụt nửa chiều cao của nó**
  (Rơm 202, Vòng hoa 301, Khung Hoa 378 world; 1 ô sâu chỉ 150). Vòng 13 đã sửa 75 meta stage
  (`spritePivot → (0.5, 0)`) + 16 sprite bake (`alignment 0 → 6`). **Chauhoa_1..4 CHƯA sửa** vì
  sprite dùng chung 23+2 chỗ và phải dịch cả cụm hoa. Hoàn tác: `Tools/Farm Game/Hoan tac neo art decor`
  hoặc copy lại `production/backup_vong13_2026-09-06/lead/meta/`.
- **`RectFromWorldBounds` PHÌNH 1 Ô THÀNH 3x3.** Chiếu 4 góc hộp bao VUÔNG sang không gian ISO:
  hộp bao của 1 ô kim cương (300x150) chạm 9 ô ⇒ mọi vật đi qua nhánh cuối `ComputeRectFor`
  chặn cả 8 ô quanh nó. Đã đổi nhánh cuối sang `RectFromAnchor(transform.position, SoO)`.
  **Đừng dùng `RectFromWorldBounds` để suy footprint nữa.**
- **PLOT ĐẤT:** `tile_dirt.png` 724x345 px PPU 100, scale tích luỹ 50 ⇒ 362 x 172,5 world, to hơn ô
  (300x150) 20,7 %; pivot GIỮA + localPosition 0 ⇒ tụt 75. Đã sửa `GroundSprite` scale
  `82.8729 / 86.9565` + localPosition `(0,150)` (root 0.5 ⇒ world +75) + collider `(0,150) 600x300`.
  Kiểm chéo: 12 `CropPoint` là lưới 3 cột x 4 hàng, tâm (−0,45 · 82,1) ≈ tâm ô (0 · 75).
  **`CropPoint_1` là RectTransform, đọc `m_AnchoredPosition (−113, 467.7)` KHÔNG phải `m_LocalPosition (0,0)`** — đừng "sửa" nó.
- **KHE HỞ GIỮA CÁC Ô LƯỚI** = `IsoPlacementPreview.cellInset = 0.06` (thu mỗi ô 6 % ⇒ hở 18 world).
  Field public đã serialize trong scene ⇒ phải thêm field MỚI: `epKhitVienO = true`.
- **`Placement_Ghost.prefab` KHÔNG serialize field nào của `PlacementGhostVisualController`**
  (block `1010101010101010101` chỉ có `m_Script`) ⇒ sửa mặc định trong code là ăn ngay.
  Vòng 13: nút ✓ 148→126 (90,7 px), ✗/🗑 128→106, card 392x244 → **312x204**. **SÀN nút ✓ = 126**
  (px = 0,72 × S; 88 px đầu ngón tay ⇒ S ≥ 122,3). Không hạ tiếp.
- **`AssetDatabase` trong code RUNTIME = hiệu ứng không bao giờ chạy trên bản build.**
  `RainSplashManager` cũ nạp sprite bằng `AssetDatabase` bọc `#if UNITY_EDITOR` ⇒ build iPhone rỗng.
  Đã đổi sang `Resources.Load` + `Sprite.Create`. `RainSplashFlipbook.png` = **lưới 3x2, 6 khung 128x128**
  (đã mở ảnh ra đo bằng alpha channel), pivot dùng `(0.5, 0.28)` = chỗ vũng nước.
  Component tự cài bằng `[RuntimeInitializeOnLoadMethod]` — không cần sửa scene. Tắt: `RainSplashManager.TuCaiDat = false`.
- **NGÀY ĐÊM:** `Assets/Day_Night/Prefabs/DayNightWeatherSetup.prefab` → `AmbientLightIntensityCurve`.
  Đêm/ngày cũ 0,38/1,45 = 26 %. Vòng 13: 4 key đêm → 0.95 / 1.02 / 1.03 / 0.95 (= 65,5 %).
  **Phải nâng CẢ 4 key**, nâng riêng 2 đầu là nửa đêm sáng hơn rạng đông. `ThunderLightMultiplier`
  0.85 → 0.88 + khoá cứng `MinWeatherLightMultiplier = 0.88f` trong `DayNightCycleController`.
- **ID ĐỊNH DANH — trạng thái thật:** plot ✅ (`plotId` + `FARM_NEXT_PLOT_ID`); nhà/công trình ❌
  (`BuildingEntry` chỉ có `itemId,x,y,plotId,rot`); chuồng ❌ (`PenState_{penId}`, penId từ
  ScriptableObject ⇒ **mua chuồng thứ 2 cùng loại là dùng chung save**); máy ❌ (`MILL_S{i}_*` TOÀN CỤC);
  nhà đang xây ⚠️ (`HouseSave_{houseId}_{x}_{y}` nhúng toạ độ ⇒ kéo nhà đang xây là xây xong miễn phí).

## VÒNG 13b (08/09/2026) — SỬA LỖI CỦA CHÍNH VÒNG 13 + BỘ DECOR THIÊN NHIÊN
- 🔴 **BẢNG `SpriteAlignment` CỦA UNITY — GHI RA ĐÂY VÌ ĐÃ NHỚ SAI MỘT LẦN VÀ SUÝT PHÁ ART:**
  `0=Center · 1=TopLeft · 2=TopCenter · 3=TopRight · 4=LeftCenter · 5=RightCenter ·
   6=BottomLeft · 7=BottomCenter · 8=BottomRight · 9=Custom`.
  **7 = BottomCenter, KHÔNG phải Custom. 6 = BottomLeft, KHÔNG phải BottomCenter.**
  Kiểm chứng bằng chính dữ liệu dự án: `Sprite_Street_lamp` align=9 kèm `spritePivot (0.740, 0.042)`
  (giá trị custom thật ⇒ 9=Custom); `Sprite_RockMedium` align=7 kèm `spritePivot (0.5, 0)` (khớp
  BottomCenter). Khi `alignment != 9`, Unity **BỎ QUA** `spritePivot` — sửa `spritePivot` mà không
  đổi `alignment` là VÔ TÁC DỤNG.
- Hệ quả đã sửa ở vòng 13b: (1) `tile_dirt` align=7 nên art plot VỐN ĐÃ neo ở chân ô — phép dịch
  `+150` local của vòng 13 là SAI, đã trả `GroundSprite.localPosition` về `(0,0,0)`; chỉ giữ đổi
  scale `82.8729 / 86.9565` (⇒ đúng 300×150 world = 1 ô). (2) 16 meta decor bị đặt nhầm
  `alignment: 6` (BottomLeft) → đã sửa thành **7**. (3) 75 meta `Assets/Art/Decor/Stages/*` vốn đã
  `alignment: 7` ⇒ art stage decor **CHƯA BAO GIỜ lệch**; phép sửa `spritePivot` ở vòng 13 là no-op.
- Pivot đúng của các nhóm: nhà (stage_4) **7**; `BarnSprite` chuồng+máy (`chuongmoigiasuc.png`,
  413×298 px, scale 1.5×100=150 ⇒ **619,5 × 447 world**, y ∈ [0,447]) **7** ⇒ collider vòng 13
  `(0, 2.235)` cỡ `6.195 × 4.47` là ĐÚNG; `tile_dirt` **7**; 19 decor gốc **0 (Center)** — đây mới
  là nhóm thật sự lệch; `Chauhoa` GroundSprite **0** — CHƯA sửa (sprite dùng chung 25 nơi + phải
  dịch cả cụm chậu + hoa).
- **BỘ DECOR THIÊN NHIÊN MỚI (25 món, itemID 200–224)** ở
  `Assets/_Game/Farm/CÔNG TRÌNH/DecorThienNhien/` — sinh bằng script từ art Happy Harvest
  (`Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Art/...`) + sheet
  `Assets/maptitle/decor/Gemini_...preview.png` (PPU 16, 33 miếng). Cấu trúc mỗi prefab:
  root scale 100 giữ BoxCollider2D + EditableBuilding + BuildingFootprintKit(soO 1×1),
  **SpriteRenderer nằm trên con tên `Visual`** — nhờ vậy chỉnh neo bằng transform, KHÔNG phải
  đụng pivot của art dùng chung. Công thức đặt con: `scale = k/100`,
  `localPosition = ((pivotX − 0.5) · W / 100, pivotY · H / 100)` với `k = W_target / w_unit`,
  trần chiều cao 420 world. Đã thêm 25 guid vào `ShopManager.decorList` trong scene (19 → 44 mục)
  và 25 id vào `DecorGrowthConfig.excludedItemIDs` (**bắt buộc**, nếu không `BiAnViThieuArt` ẩn hết
  vì decor mới không có bộ 5 stage).
- `Assets/maptitle/decor/` + `Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Art/Environment/`
  là KHO ART TRANG TRÍ CHƯA DÙNG của dự án: Barrel · Bush · Flowers(đỏ/trắng/vàng) · Grass ×2 ·
  Lamps(street/lantern/house) · Log · Pinetree · Rocks(big/med/small) · Signs · WaterLilly ·
  Interior/Fireplace(logpile) · Interior/Plant. `bocaycoi.png` = 16 CÂY (thông, sồi, tre, liễu,
  táo, anh đào, dừa…), scene mới dùng 6.
- ⚠️ `device_stage_files` KHÔNG stage được file sâu quá 7 cấp thư mục. Art Happy Harvest sâu 8–10
  cấp ⇒ phải render QC **ngay trên máy Sếp** (PIL 12.3 có sẵn trong device VM) rồi stage ảnh kết quả.

## VÒNG 13c (08/09/2026) — CỠ DECOR + MẬT ĐỘ MƯA
- **MƯA:** `splashScale` 210 → **68** (269 → 87 world = 0,29 ô), `splashesPerSecond` 14 → **48**,
  thêm `daoDongCo (0.72–1.34)` cho mỗi hạt một cỡ. **Bài học riêng:** `spawnArea` số CỐ ĐỊNH
  2600×1400 chỉ đúng ở ortho 750; Sếp test ở ortho 1500 (khung nhìn 6333×3000) nên mưa chỉ rơi
  giữa màn. Đã thêm `tuTinhVungSinh = true` → đo `2·ortho × aspect` mỗi lần sinh.
  **Quy tắc chung: mọi vùng/khoảng tính bằng world trong VFX phải suy từ `cam.orthographicSize`,
  đừng bao giờ hard-code cho một mức zoom.**
- **CỠ DECOR — mốc đo lấy TỪ MAP CỦA SẾP** (đo trên ảnh play mode: dev overlay ghi
  `Viewport 6333x3000` trên màn 1562 px ⇒ 4,05 world/px): decor có sẵn trong world là bụi cây
  ~223 world, cây thông ~304, đèn lồng ~121, hoa nhỏ ~100. **Dải đúng cho decor = 29 %–63 % bề
  ngang một ô (86–190 world). Nhà là 171 % — decor phải NHỎ HƠN công trình.**
- 24 decor mới: bề rộng × 0,72, trần cao 420 → 300 ⇒ 86–173 world. Sửa bằng cách patch
  `Visual.localScale/localPosition` + collider, **giữ nguyên guid** nên không phá tham chiếu scene.
- 19 decor CŨ (trước đó 327–490 rộng = 109–163 % ô, cao tới 602): thu nhỏ bằng **`m_LocalScale`
  của ROOT** (100 → 38–57). Đúng vì: `Instantiate(prefab,pos,rot)` KHÔNG ghi đè scale;
  `BoxCollider2D.m_Size/m_Offset` là đơn vị LOCAL nên tự co theo, hộp bấm không lệch; `soO` là số
  ô nên không đổi; `DecorGrowthController` chụp `_initialScale = transform.localScale` ở dòng 184
  rồi mọi pop/bob nhân tương đối vào đó nên KHÔNG đè mất giá trị mới.
- Đã xoá theo yêu cầu: món **201 Lò Sưởi Đá** (4 file + guid trong decorList + id trong
  excludedItemIDs) ⇒ còn **24 món: id 200 + 202–224**. Xoá 2 ảnh QC sheet_33 và bocaycoi_16cay,
  giữ duy nhất `production/_qc_vong13_decor_moi.jpg`.
- ⚠️ `SCN_Farm.unity` bị Unity ghi lại giữa vòng 13b và 13c (591.486 → 646.443 dòng) khi Sếp mở
  Play Mode. Bản chèn decorList vẫn còn. **Luôn nhắc Sếp đóng Unity trước khi sửa file trên đĩa.**
- Xoá file trên máy Sếp cần `device_request_delete_permission` một lần cho gốc project
  (`E:\Game2\Cooking-Game-2D`); sau đó `rm` chạy được cả session.

## VÒNG 13d (08/09/2026) — NEO TÂM Ô · LÀM ĐẦY KHUNG · CARD KHÔNG ĐÈ CÔNG TRÌNH
- **NEO:** trước 13d mọi thứ neo ở ĐỈNH NAM ô (mũi trước hình thoi) — đúng cho nhà/chuồng, nhưng
  decor nhỏ trông như treo hẫng. 24 decor mới nay neo vào **TÂM Ô (0, 75)**, chia 2 kiểu theo
  tỉ lệ art `cao/rộng`: **< 0,62 = DẸT → TÂM art = tâm ô**; còn lại **ĐỨNG → CHÂN art = tâm ô**.
- **19 decor CŨ KHÔNG neo tâm được nếu chưa sửa `DecorGrowthController`** — SR nằm trên ROOT, mà
  `transform.position` của root chính là điểm neo `PlacementManager` ghi + `RefreshOccupancy` đọc.
  Ba đường đều vướng `EnsureCollider()` (dòng ~653) đang cứng `offset = (0, size.y*0.5f)`:
  (1) pivot.y âm — sạch cho 5 món ngoài growth, nhưng 14 món còn lại bị đổi sprite sang
  `Assets/Art/Decor/Stages/` lúc chạy ⇒ phải sửa thêm 75 file; (2) bọc con `Visual` —
  `ResolveRenderer()` có `GetComponentInChildren` (dòng 688) nên growth vẫn thấy, NHƯNG
  `EnsureCollider` lấy `sprite.bounds.size` không nhân scale con ⇒ collider sai tỉ lệ;
  (3) `Sprite.Create` pivot mới lúc chạy. **Bản đúng nhất = (2) + vá `EnsureCollider` nhân lossyScale.**
- **CỠ DECOR (chốt lại lần cuối):** bề rộng art = **93 % bề ngang ô (280/300)**, trần cao **300**
  (2 chiều sâu ô), VÀ trần **3,4 × số pixel art** — trần cuối này bắt buộc: art gốc bé (Hoa Đỏ 43 px,
  Bụi Cỏ Nhỏ 45 px) kéo lên 280 world thì vỡ hình và lố so với cây thông 734 px. Kết quả 43 món:
  **85–280 world (28–93 % ô), cao ≤ 300**.
- **CARD ✓/✗/🗑 ĐÈ CÔNG TRÌNH — nguyên nhân là khối "TRẦN TRƯỜN LÊN" trong `NeoCardVaoWorld()`,
  KHÔNG phải do card to.** Khi đáy card tụt dưới `leAnToanDuoiPx = 170`, code cũ kéo card lên tới
  TÂM vùng ô; nhánh đó nổ ở cả nửa dưới màn hình nên card gần như luôn bò lên che chân công trình.
  Sửa bằng field mới `khongDeCardChePhuCongTrinh = true` → **3 bậc: DƯỚI → SANG CẠNH → (chỉ khi
  card rộng hơn viewport) TRƯỜN LÊN**. Bậc sang cạnh phải **tắt phép kẹp ngang**, nếu không kẹp
  kéo card về đúng chỗ vừa tránh. Card 312×204 → **296×200**; nút ✓ giữ 126 (= 90,7 px, sàn 88 px).

## VÒNG 13e (08/09/2026) — 19 DECOR CŨ NEO TÂM Ô · BONG BÓNG CHUỒNG
- **Bọc SpriteRenderer của 19 decor cũ vào con `Visual`** (root giữ Transform + BoxCollider2D +
  EditableBuilding + BuildingFootprintKit). Con: `localScale (1,1,1)`, `localPosition (0, 75/k, 0)`
  ⇒ chân art = TÂM Ô. Collider: `offset (0, 75/k + h_unit/2)`, `size (w_unit, h_unit)`.
  Prefab 177 → 209 dòng. **43/43 decor giờ đồng bộ neo tâm ô.**
- **BẮT BUỘC vá kèm `DecorGrowthController.EnsureCollider()`**: bản cũ cắm chết 2 giả định
  (SR cùng GameObject với BoxCollider2D; art bắt đầu tại gốc) nên bọc con làm vỡ cả hai.
  Bản mới: `wb = _sr.bounds` (world) → `size = wb.size / |box.transform.lossyScale|`,
  `offset = box.transform.InverseTransformPoint(wb.center)`. Đúng cho mọi cấu hình SR/pivot/scale.
  **Mẫu này dùng lại được cho bất kỳ chỗ nào tự tính collider từ sprite.**
- **BONG BÓNG SẢN PHẨM CHUỒNG nằm thấp:** `readyBubbleLocalPos.y` **đã serialize = 320 trong cả 4
  prefab chuồng** (khối PrefabInstance, propertyPath) ⇒ mặc định code vô hiệu. Thân chuồng phủ
  y ∈ [0, 447] world nên 320 = lọt giữa thân. Bản cũ có tự đo đỉnh nhưng quy về anchoredPosition
  bằng phép chia tay `(tamWorld - transform.position.y)/donVi` — sai khi cha không nằm ở gốc chuồng.
  **Sửa: đặt thẳng bằng `RectTransform.position` (world).** Field MỚI: `datBongBongTheoWorld=true`,
  `bongBongCaoThemWorld=80`, `bongBongCaoDuPhongWorld=640`. Thêm `LayGocChuong()` đi ngược cây cha
  tìm `BuildingFootprintKit`/`PenClickDetector` thay vì tin `transform.parent`.
- **QUY TẮC RÚT RA (lặp lại lần thứ 4 trong dự án): trước khi sửa giá trị mặc định của một
  `[SerializeField]`, LUÔN grep `propertyPath: <tên field>` trong prefab/scene trước.** Nếu đã bị
  serialize thì đổi code KHÔNG ăn — phải thêm FIELD MỚI hoặc sửa thẳng giá trị serialize.

## GHI CHÚ ĐỌC LOG (08/09/2026)
- `[IsoGrid] He so o mat dat do tu scene = 2 (8 tilemap con) -> o placement = 300 x 150 world`
  là **LOG THÔNG TIN, KHÔNG PHẢI LỖI** — và 300×150 là con số ĐÚNG. `Grid_Iso45` có 12 con,
  trong đó **9 con scale 2**: `GroundBase_Dirt` (SpriteRenderer) + **8 Tilemap_Iso\*** (Dirt,
  DirtPatch, Dock, Fence, Grass, Rock, Sand, Stone). IsoGrid chỉ đếm **TilemapRenderer** nên ra 8.
  (Sửa lại ghi chú cũ "9 ground tilemap": đúng là **8 tilemap + 1 sprite nền**.)
- `MissingComponentException: There is no 'VisualEffect' attached to "P_VFX_Moths 1 (2)"` là
  **lỗi CHỈ CÓ TRONG EDITOR, sinh từ package `com.unity.visualeffectgraph`**, không phải code dự án.
  Stack toàn bộ nằm trong `AdvancedVisualEffectEditor.AutoAttachToSelection` → `VFXViewWindow.AttachTo`.
  `P_VFX_Moths 1` (13 thể hiện trong scene, prefab guid 357f9b0391…) là **ParticleSystem**
  (class 198/199/210), KHÔNG có component VisualEffect. Hai object VFX Graph THẬT duy nhất trong
  scene là `VFX_WaterLines` và `VFX_WaterLinesStorm` (class 73398921 = VFXRenderer).
  **Cách hết: đóng cửa sổ Window ▸ Visual Effects ▸ Visual Effect Graph.** Không ảnh hưởng bản build.
- ⚠️ `Tilemap_IsoFence` renderer đã **BẬT LẠI** (`m_Enabled: 1`) — vòng 11 từng tắt theo yêu cầu Sếp.
  Nhiều khả năng mất khi Unity ghi lại scene. Hỏi Sếp trước khi tắt lại.
