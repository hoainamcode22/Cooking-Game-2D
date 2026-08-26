# BÁO CÁO — Hợp nhất hệ thống Tàu Hỏa (2026-08-26)

## Bối cảnh
Đội sprite-forge đã giao 22 sprite PNG + 3 prefab + 5 script UI (`Assets/Export_Train_UI_Package/`).
Audit phát hiện UI chạy bằng DATA GIẢ (TrainItemDatabase hardcode), tách rời TrainManager:
tăng tốc không trừ gem, timer chỉ chạy khi popup mở & mất khi thoát game, thưởng 450 vàng + 8 gem
chưa duyệt kinh tế, không input-lock, sprite load kiểu Editor-only (trắng UI khi build).

## Quyết định đã duyệt (Sếp, 2026-08-26)
1. Chuyến tàu 10–15 phút (mặc định 600s, chỉnh trong Inspector `tripDurationSeconds`), chạy nền + offline.
2. Tăng tốc = kim cương theo `ConstructionManager.RushCostFor(remaining)` — đồng nhất hệ xây dựng.
3. Thưởng = vật liệu (TrainRewardData asset: đá/gỗ/đinh/sơn/kính) + `goldBonusPerTrip` = 80 vàng/chuyến.
4. Duyệt sửa cả TrainManager lẫn 5 script package. Backup: `production/backup_train_2026-08-26/`.

## Kiến trúc sau refactor — 1 NGUỒN SỰ THẬT
- `TrainManager` (global): state machine 6 state + SlotData từ TrainCargoData/TrainRewardData asset,
  timer unix-time (state Processing khôi phục lại), persistence PlayerPrefs `train_trip_state_v1`
  (sống sót thoát game, mọi nhánh restore đều tự thoát kẹt), `TrySpeedUp()` trừ gem thật,
  `TryLoadAllToSlot()`, gold bonus chốt chuyến, event `OnStateChanged`, `LastSentCargo` cho popup timer.
- 3 popup package = VIEW THUẦN đọc TrainManager, có FarmInputLock đủ cặp, icon = Sprite thật từ asset.
- `TrainSpriteLoader.Assign()` — build-safe: không load được path thì GIỮ sprite prefab, không gán null.
- `TrainStationBuilding` routing theo state: WaitingForLoad→popup nạp · Processing→popup timer · Reward→popup thu.
- Thu thưởng qua `CollectReward` cũ: check kho đầy (CanAddItem) + EXP + FX bay + mission event giữ nguyên.
- Click toa world → mở popup nạp MỚI (fallback popup cũ nếu thiếu prefab). Hint "Tàu đã về ga" khi tàu về.

## File sửa (6) — backup đủ tại production/backup_train_2026-08-26/
TrainManager.cs · TrainStationBuilding.cs · TrainStationMasterPopupUI.cs · TrainLoadPopupUI.cs
· TrainProcessPopupUI.cs · TrainSpriteLoader.cs

## QA (agent review độc lập): PASS-WITH-FIXES → đã vá hết
- M1 deadlock restore (thoát game lúc tàu vào hầm / đủ hàng chưa khởi hành / thu hết chưa rời ga) — VÁ (M1a/b/c + m1r).
- m2 popup timer không tự đóng khi sang chuyến mới — VÁ. m4 click toa lúc tàu đang vào ga im lặng — VÁ (hint).
- m6 OpenForWagon bật nhầm cả cây parent — VÁ (dừng ở Canvas). m7 prefab master ĐÃ có sẵn trong SCN_Farm ✔.
- Còn lại (minor, chưa vá — rủi ro thấp): singleton không clear khi destroy; dead-code nhánh toggle ở TrainStationBuilding;
  TrainDataModel.TrainItemDatabase chỉ còn là định nghĩa mồ côi (có thể xoá ở sprint dọn scene).

## ANH CẦN LÀM TRONG UNITY (bắt buộc trước khi test)
1. Mở Unity cho compile 6 file — Console phải 0 lỗi đỏ.
2. Chọn TrainManager trong Hierarchy → Inspector kiểm 2 field mới:
   `Trip Duration Seconds` = 600 (10 phút; muốn 15 phút đặt 900) · `Gold Bonus Per Trip` = 80.
3. Play Mode test flow: click ga (gataulua) → popup TÀU CHỞ HÀNG → nạp từng toa (THÊM HÀNG / NẠP TẤT CẢ)
   → đủ hàng tàu trượt phải + popup ĐANG VẬN CHUYỂN đếm ngược → thử TĂNG TỐC (phải TRỪ kim cương đúng giá trên nút)
   → tàu về có hint → click ga → NHẬN THƯỞNG → thu 4 toa (kho gần đầy phải báo "Kho đầy") → +80 vàng → chuyến mới.
4. Test persistence: đang vận chuyển → thoát Play → Play lại → timer phải tiếp tục đúng thời gian còn lại.
5. Nếu popup nào lệch layout/sprite: chạy lại tool build package (Tools → nếu có TrainPackageBuildTool) hoặc báo team.

---

## BỔ SUNG 2026-08-26 (chiều) — Wire 3 asset world sprite-forge bàn giao

### Đội vẽ đã giao (verify OK): 3 PNG RGBA nền trong tại Assets/Export_Train_UI_Package/Sprites/
world_bubble_train_arrived.png · station_building_world.png · icon_speedup_wing.png (đều 1024x1024).
⚠ Lỗi kỹ thuật đội vẽ: cả 3 .meta để spriteMode=2 (Multiple) thay vì 1 (Single) → load sprite fail.
Tool setup bên dưới TỰ SỬA importer, không cần sửa tay.

### Code đã wire (backup *_v2 trong production/backup_train_2026-08-26/)
1. `TrainStationBuilding.cs`: bubble "Tàu đã về" world-space trên nóc ga — tự tạo runtime
   (SpriteRenderer + TrainArrivedBubbleBob nhấp nhô), hiện khi RewardArriving/RewardReadyToCollect,
   tự ẩn các state khác, cùng sorting layer với ga, chuẩn hoá bề rộng ~1.4 unit.
   Field mới `arrivedBubbleSprite` (gán qua tool, chạy được trong build) + `bubbleHeight` (mặc định 2.2).
2. `TrainProcessPopupUI.cs` (package): icon cánh vàng tự tạo child Icon_Wing trong nút TĂNG TỐC,
   build không load được thì ẩn (không ô trắng).
3. File MỚI `Assets/Export_Train_UI_Package/Editor/TrainWorldAssetSetupTool.cs` — 2 menu:
   - `Tools/Farm Game/Train/Setup Train World Assets`: ép importer 3 PNG về Sprite/Single +
     alphaIsTransparency, gán bubble vào TrainStationBuilding, mark scene dirty, log report.
   - `Tools/Farm Game/Train/Apply Station World Sprite (tuỳ chọn)`: thay hình ga ngoài map bằng
     station_building_world.png — có Undo, KHÔNG tự chạy (chờ Sếp duyệt hình).

### ANH CẦN LÀM TRONG UNITY (thêm vào checklist cũ)
6. Chạy menu `Tools → Farm Game → Train → Setup Train World Assets` → xem Console log ✔ → **Save scene (Ctrl+S)**.
7. (Tuỳ chọn) Ngắm thử hình ga mới: chạy menu `Apply Station World Sprite` — không ưng thì Ctrl+Z.
8. Play test thêm: nạp đủ hàng → tăng tốc cho tàu về → thấy bubble tàu đỏ nhấp nhô trên nóc ga →
   thu hết thưởng → bubble tự biến mất. Nút TĂNG TỐC giờ có icon cánh vàng bên trái chữ.

---

## BỔ SUNG 2026-08-26 (tối) — Chuẩn bị sẵn hệ animation tàu world (chờ đội vẽ giao frame)

### Prompt đội vẽ (đã cập nhật theo lệnh Sếp: vẽ ĐỦ frame)
`production/session-state/PROMPT_SPRITE_FORGE_WORLD_TRAIN_REDESIGN.md` — 24 file:
6 frame × 2 hướng (frontleft/upright) × (đầu tàu + toa), đánh số _01→_06, frame 01 = tư thế nghỉ,
KHÔNG vẽ khói (khói code phun), pivot bottom-center, spriteMode Single, style = tàu đỏ burgundy popup.
Giao vào: `Assets/Export_Train_UI_Package/Sprites/WorldTrain/`

### Code ĐÃ VIẾT SẴN (compile được ngay, chưa cần asset)
1. File MỚI `Assets/_Game/Farm/Scripts/Train/TrainWorldVisual.cs`:
   - Frame animation theo chuyển động thật (đọc delta vị trí — không đụng TrainPathFollower):
     chạy = lăn bánh 10fps, đứng ga = frame nghỉ. Tự chọn bộ frame theo hướng (delta.x).
   - Khói `TrainWorldSmokePuff`: phun từ miệng ống khói (tự tính từ bounds sprite, chỉnh được),
     bốc lên + nở to + lượn gió + tan, scale theo cỡ tàu; nhịp 0.3s khi chạy / 1.4s khi đậu ga.
2. Tool mở rộng `TrainWorldAssetSetupTool.cs` — menu MỚI:
   `Tools → Farm Game → Train → Setup World Train Frames (sau khi đội vẽ giao)`:
   ép importer cả folder WorldTrain (Single + pivot bottom-center), load frame theo tên,
   gắn TrainWorldVisual vào Locomotive/Locomotive2 + 8 Wagon của CẢ 2 tàu, TẮT Animator sheet cũ,
   gán khói train_smoke_puff.png, đặt frame nghỉ, log report + nhắc save scene. Backup tool v1 đã lưu.

### Khi đội vẽ báo xong, Sếp chỉ cần (2 bước)
1. Kiểm tra file nằm đúng `Assets/Export_Train_UI_Package/Sprites/WorldTrain/` (24 PNG).
2. Unity: chạy menu `Setup World Train Frames` → Ctrl+S → Play xem tàu lăn bánh + khói bốc từ ống khói.

---

## BỔ SUNG 2026-08-26 (tối, sau test của Sếp) — Polish hàng hóa trên toa world

Feedback test: icon hàng (thịt...) trên toa tàu giao quá to, lòi khỏi toa; tàu chạy chất hàng nhìn xấu.

### Đã sửa (backup: TrainWagonSlot.cs + TrainManager_v2.cs trong backup_train_2026-08-26/)
1. `TrainManager.CheckAllLoaded()`: tàu giao khởi hành là **chạy sạch** — ẩn toàn bộ icon hàng trên toa
   (HideAllShippingSlots). Hàng đã gửi vẫn xem được trong popup "Đang vận chuyển" (chips) như cũ.
   Tàu THƯỞNG vẫn chở kính/gạch/đá về như trước (Sếp khen size đó chuẩn).
2. `TrainWagonSlot.FitIconToWagon()` (mới): mọi icon hàng/thưởng hiển thị trên toa world được
   **chuẩn hoá về cùng 1 cỡ** = 45% bề rộng vùng toa (đo bằng BoxCollider2D nên đúng với mọi scale scene,
   reset scale gốc trước khi đo nên refresh nhiều lần không phình dần). Field `iconFitRatio` chỉnh được
   trong Inspector từng WorldSlot (0 = trả về hành vi cũ).

### ANH CẦN LÀM TRONG UNITY
- Chỉ cần compile (0 đỏ) + Play test lại: nạp hàng → icon nhỏ gọn nằm trên toa; đủ hàng →
  tàu lăn bánh TRỐNG TRƠN; tàu về vẫn thấy kính/gạch/đá nhỏ gọn. Muốn icon to/nhỏ hơn:
  chọn WorldSlot_01..04 → chỉnh Icon Fit Ratio (0.45 mặc định).

---

## BỔ SUNG 2026-08-26 (đêm) — Popup-only + FX nhận hàng + wire 12 frame tàu mới

### Quyết định Sếp: tàu world CHỈ CHẠY LÀM CẢNH — nạp & nhận 100% trong popup
1. `TrainManager`: cờ mới `showWagonIconsInWorld = false` (mặc định TẮT) — 4 method refresh slot world
   đều Hide, icon/bubble/collider trên toa ngoài world biến mất hoàn toàn ở mọi state.
   Muốn quay lại kiểu cũ: tick cờ này trên TrainManager trong Inspector.
2. `CollectReward(int, bool spawnWorldFx)` — popup gọi bản (idx, false), FX world không phun thừa sau lớp dim.
   Chữ ký cũ CollectReward(int) giữ nguyên (delegate true) — không vỡ chỗ gọi nào.

### FX nhận thưởng mới trong popup (juicy — pillar #1)
`TrainStationMasterPopupUI.SpawnCollectFlyFX`: bấm toa → 2-4 icon vật phẩm "BÙM" nảy vọt lên
phóng to, rồi bay theo đường cong Bezier vào ĐÚNG icon Kho trên HUD (đọc qua
`HarvestFeedbackSpawner.WarehouseTarget` — accessor mới thêm), thu nhỏ + tan khi chạm kho.
Thưởng càng nhiều bay càng nhiều icon, so le 0.09s.

### Assets đội vẽ giao (verify PASS)
`Sprites/WorldTrain/`: 12 frame (loco 6 + wagon 6, hướng upright) + 2 file `_single` phụ.
Meta lần này CHUẨN (Single, pivot đáy). Art khớp style burgundy popup, không khói, không ray. 
Chưa có hướng frontleft → `TrainWorldVisual` thêm `flipXWhenFallback` (mặc định bật): chạy hướng
ngược thì lật gương bộ upright — đợt sau đội vẽ giao frontleft thì tự hết lật.
Tool `Setup World Train Frames` đã lọc chỉ nhận frame `_NN` (bỏ `_single`).
Ghi chú nhỏ: art có chữ nướng cứng "FARM EXPRESS / HARVEST TRANSPORT" (đã dặn không chữ) — chấp nhận được vì là chữ trang trí.

### Backup mới: TrainManager_v3, TrainStationMasterPopupUI_v2, HarvestFeedbackSpawner (gốc), TrainWorldAssetSetupTool_v1

### ANH CẦN LÀM TRONG UNITY (chốt phiên)
1. Compile 0 đỏ → chạy menu `Tools → Farm Game → Train → Setup World Train Frames (sau khi đội vẽ giao)`
   → đọc log ✔ (2 đầu tàu + 8 toa gắn, Animator cũ tắt) → **Ctrl+S save scene**.
2. Play test: toa ngoài world KHÔNG còn icon hàng ở mọi giai đoạn · tàu mới màu burgundy lăn bánh
   6-frame + khói bốc từ ống khói · click ga → popup nạp → đủ hàng tàu chạy sạch → tàu về →
   popup NHẬN THƯỞNG → bấm toa thấy icon BÙM nảy lên bay vào icon Kho trên HUD.
3. Nếu hướng lật gương nhìn kỳ ở chặng nào, báo team đặt đội vẽ thêm bộ frontleft (prompt có sẵn).

---

## SPRINT MỚI 2026-08-26 — HUD Gain FX (phân tích 2 video Township của Sếp)

### Phân tích video
V1 (ga tàu): nhận thưởng → item pop trên toa, thanh kho (icon nhà + fill "29/50") hiện góc trên, item bay vào.
V2 (thu hoạch): lúa bay ra + tag EXP xanh bay về thanh level; thanh kho (nhà đỏ + "21/50→27→32") trượt hiện
mép trên, đếm tăng dần theo món bay vào, xong tự ẩn. Vàng/gem bay về counter HUD.

### Đã có sẵn (không làm lại): vàng bay (CoinFlyFX) · EXP bay (HarvestFeedbackSpawner.SpawnExpFly)
· item bay về icon kho khi thu hoạch/chuồng (SpawnHarvestFly).

### Đã DUYỆT (Sếp): thanh kho hiện SLOT THEO LOẠI (UsedSlots/SlotCapacity) + hiệu ứng "+N" — phương án A.

### Code mới/sửa (backup: FarmEconomyManager.cs, FarmInventoryManager.cs trong backup_train_2026-08-26/)
1. `FarmEconomyManager`: event `OnGemAddedFx` (mirror vàng) bắn trong AddGems.
2. `FarmInventoryManager`: event `OnItemAddedFx(itemId, amount)` bắn trong AddItem — MỌI nguồn nhận đồ
   (thu hoạch/chuồng/chợ/tàu/nhiệm vụ) đều kích hoạt thanh kho, không phải hook từng chỗ.
3. FILE MỚI `GemFlyFX.cs`: kim cương bung + bay về icon gem HUD (tự tìm icon theo tên nếu chưa gán,
   fallback vẽ gem xanh runtime). Bootstrap tự gắn cạnh CoinFlyFX nếu quên chạy tool.
4. FILE MỚI `WarehouseGainToastUI.cs`: pill [icon nhà kho | fill bar | 25/30] trượt hiện mép trên
   (ease-out + nảy overshoot kiểu Township), fill mượt, "+N" bay lên, pulse mỗi lần nhận,
   tự ẩn sau 2.5s; kho đầy → flash đỏ + "KHO ĐẦY!". Mượn font TMP tiếng Việt từ HUD (khỏi ô vuông).
   Bootstrap tự sinh nếu scene chưa setup (bản fallback màu phẳng).
5. FILE MỚI `FarmFxSetupTool.cs` (Editor) — menu `Tools → Farm Game → FX → Setup HUD Gain FX`:
   gắn GemFlyFX + tạo WarehouseGainToast trong HUD canvas, gán icon kho thật từ HUD +
   sprite bar gỗ/kem từ train package (serialize vào scene = đẹp cả trong build). 

### Assets: KHÔNG THIẾU — tái dùng icon kho HUD + progress bar train package. (Tuỳ chọn sau này:
đặt sprite-forge vẽ skin pill riêng cho thanh kho nếu Sếp muốn khác biệt.)

### ANH CẦN LÀM TRONG UNITY
1. Compile 0 đỏ → chạy `Tools → Farm Game → FX → Setup HUD Gain FX` → Ctrl+S.
2. Play test: thu hoạch lúa → thanh kho trượt ra, "+N" bay, fill nhích, 2.5s tự ẩn ·
   mua đồ ở chợ → thanh kho cũng hiện · nhận gem (thưởng level/nhiệm vụ) → gem bay về icon kim cương ·
   vàng & EXP bay như cũ · để kho đầy loại mới → pill flash đỏ "KHO ĐẦY!".
3. Chỉnh vị trí/kích thước pill: chọn WarehouseGainToast → Inspector (Anchored Pos, Panel Size).

---

## BỔ SUNG 2026-08-26 (tối muộn) — Hướng tàu theo ga + tẩy bóng trắng bằng code

### 1. Hướng tàu đứng yên (feedback: "tàu phải xoay về hướng ga")
- `TrainWorldVisual` thêm `initialDir` (0=trái-xuống, 1=phải-lên) — tàu đứng yên quay đúng hướng
  hành trình của nó thay vì mặc định phải-lên.
- Tool `Setup World Train Frames` tự nhận diện: con thuộc `TrainVisualRoot` (tàu THƯỞNG, chạy hầm→ga)
  → initialDir=0 + LẬT GƯƠNG preview ngay trong Scene view; `TrainVisualRoot2` (tàu GIAO, ga→hầm)
  → initialDir=1 giữ nguyên. Khi đội vẽ giao bộ frontleft xịn → tự thay, hết lật gương.

### 2. Bóng trắng dưới gầm tàu — ĐÃ TẨY BẰNG CODE (không chờ đội vẽ)
- Bóng bake trong PNG là XÁM ĐẶC alpha=255 (~28.000 px/frame) nên không xoá bằng ngưỡng alpha được.
- Dùng connected-component (scipy): tách blob xám nhạt bão-hoà-thấp LỚN + DẸT nằm sát đáy silhouette
  → chỉ trúng ellipse bóng, không đụng khung thép xám của toa hay highlight thân tàu.
- Đã xử lý cả 12 frame TỪ BẢN GỐC (backup: production/backup_train_2026-08-26/WorldTrain_original/),
  verify bằng mắt trên nền magenta: sạch 100%. Unity sẽ tự reimport (guid/meta không đổi).
- Chữ "FARM EXPRESS/No.3/HARVEST TRANSPORT" VẪN CÒN — phải chờ đội vẽ round 2 dặm (đã đặt hàng).

### ANH CẦN LÀM
- Quay lại Unity (tự reimport ảnh sạch) → chạy lại `Setup World Train Frames` → thấy tàu thưởng
  quay mặt về ga ngay trong Scene → Ctrl+S → Play kiểm tra 2 tàu chạy 2 chiều đúng hướng.

---

## CHỐT PHIÊN 2026-08-26 — Sorting cố định + QA tổng PASS

1. **Art round 3 nghiệm thu ĐẠT**: 24 frame, 2 chiều style đồng bộ (frontleft = mirror hoàn hảo),
   sạch chữ, sạch bóng (soi pixel xác nhận), meta chuẩn, không file thừa.
2. **Sorting cố định theo lệnh Sếp**: đầu tàu 660 → Wagon_01 659 → ... → Wagon_04 656, cả 2 tàu.
   TrainWorldVisual bỏ y-sort động, giữ order cố định (tự chống ConfigureTrainSorting ghi đè).
   Tool gán + preview ngay trong Scene view.
3. **QA tổng lần cuối (agent độc lập): PASS-WITH-FIXES → đã vá hết**: fake-null `??` trong
   WarehouseGainToastUI (3 chỗ), dọn FX_CollectFly sót khi đóng popup, camera-aware UI conversion.
   Minor còn lại (không chặn): bubble ga chưa chia lossyScale; mojibake comment cũ TrainWagonSlot;
   bootstrap chỉ chạy scene đầu (đã có tool gán sẵn nên vô hại).

## ANH CẦN LÀM — CHECKLIST NGHIỆM THU CUỐI (Play test toàn flow)
1. Compile 0 đỏ → chạy `Setup World Train Frames` (nạp frontleft thật + sorting 660→656) → Ctrl+S.
2. (Nếu chưa) chạy `Setup HUD Gain FX` → Ctrl+S.
3. Play toàn flow tàu: click ga → nạp từng toa (icon nhỏ gọn) → NẠP TẤT CẢ → đủ hàng: popup diễn tàu đi
   + tàu world chạy SẠCH vào hầm KHÔNG chồng đè → popup timer đếm 10:00 → TĂNG TỐC trừ gem đúng giá
   → tàu về NGƯỢC CHIỀU (frontleft) + bubble trên ga + hint → click ga → NHẬN THƯỞNG → icon BÙM bay
   vào kho + thanh kho hiện + +80 vàng → tàu rời ga → chuyến mới. Thoát Play giữa chừng → vào lại:
   timer/trạng thái giữ nguyên.
4. Play FX: thu hoạch → thanh kho + "+N" · mua chợ → thanh kho · nhận gem → gem bay về icon.

---

## FIX CUỐI 2026-08-26 — Play mode toa tách khỏi ray (dùng đúng layout Sếp xếp tay)

Nguyên nhân: SnapToPosition() runtime vứt vị trí xếp tay, trải toa lại bằng spacing cứng
(locomotiveSpacing/carriageSpacing tune cho art cũ) dọc vector waypoint (lệch góc với ray)
→ toa giãn sai + trôi chéo khỏi ray.

Fix (`TrainPathFollower.cs`, backup đã lưu): `CaptureAuthoredLayout()` chụp offset từng toa so với
trainRoot ngay FRAME ĐẦU (trước snap đầu tiên) → runtime dùng đúng số đo đó:
- Đậu ga: đặt toa đúng NGUYÊN offset tay xếp (cùng hướng; khác hướng thì dùng khoảng cách tay xếp).
- Đang chạy: toa bám path với đúng khoảng cách tay xếp (GetSpacing thay công thức cứng).
- Path seed dọc hướng trải thật của đoàn toa. Toggle `useAuthoredSpacing` (mặc định BẬT) trên
  2 GO ShippingTrainPath/TrainPath — tắt là về hành vi cũ.

ANH CẦN LÀM: compile 0 đỏ → Play — đoàn tàu lúc đậu PHẢI giống hệt lúc Edit, chạy không tách toa.
Không cần chạy tool gì thêm.

---

## SPRINT K1 — KITCHEN UI v2 (2026-08-26, đã duyệt plan: giữ minigame + Món Hôm Nay full logic)

### Code mới (backup CookingChallengeManager đã lưu)
1. `CookingChallengeManager` (edit ADDITIVE): 4 event tĩnh OnCookStarted/OnDishCooked/OnDishFailed/
   OnDishCollected + 3 property đọc (CurrentDish/CookedDishOnPlate/IsCooking) + hook
   DailySpecialManager.ApplyGoldBonus (null-safe — chưa setup thì hành vi y cũ). Không đổi chữ ký nào.
2. `KitchenV2/DailySpecialManager.cs` (MỚI): 3 món/ngày seed theo ngày, chỉ chọn món đã mở theo level,
   bonus VÀNG x1.5 khi nấu đạt (⚠ CON SỐ CHỜ SẾP DUYỆT — chỉnh Inspector `goldBonusMultiplier`,
   đặt 1 = tắt). Không nhân EXP.
3. `KitchenV2/KitchenSceneV2UI.cs` (MỚI, 967 dòng): toàn bộ màn bếp theo mockup — TopBar bếp trưởng+EXP+vàng ·
   Đơn của khách · Bảng công thức (detail: chip nguyên liệu, 5 thanh vị có vạch đỏ mốc + fill đổi màu
   đúng/thiếu/lố, thưởng, ĐIỂM DỰ KIẾN realtime qua CookingScoreCalculator.Evaluate) · Sổ công thức
   (tab lọc Dễ/Vừa/Khó, khoá theo level) · Stage (bảng đen Món Hôm Nay, lò 3 trạng thái theo event,
   bàn sơ chế toast, bàn trình bày = nút cất kho, hộp VÀO KHO đếm đã gửi) · Khay 2 tab thẻ nguyên liệu
   (TÁI DÙNG SelectableIngredientCard + RegisterAllLeftCards — SelectionManager 0 sửa) · nút hành động
   3 trạng thái. Minigame + popup cũ GIỮ NGUYÊN, tự nâng sorting nổi lên trên UI mới. UI cũ KHÔNG xoá.
4. `Editor/KitchenV2SetupTool.cs` (MỚI): menu Tools → Farm Game → Kitchen → Setup Kitchen UI v2.

### Prompt asset K2 cho GPT/sprite-forge: `PROMPT_SPRITE_FORGE_KITCHEN_ASSETS.md` (~20 file, kèm ART_RULES).

### ANH CẦN LÀM (nghiệm thu K1 — skin tạm màu phẳng, K2 mới đẹp)
1. Compile 0 đỏ → mở SampleScene → menu `Setup Kitchen UI v2` → log ✔ → Ctrl+S.
2. Play: chọn món trong sổ (tab lọc) → chạm thẻ chọn nguyên liệu/gia vị (thanh vị nhích + điểm dự kiến
   đổi) → NẤU! → minigame như cũ → lò "ĐANG CHÁY" → xong → chạm bàn trình bày → VÀO KHO +1, vàng/EXP
   cộng, topbar nhảy → nấu món trong bảng MÓN HÔM NAY xem hint "+vàng thưởng thêm".
3. Duyệt số kinh tế Món Hôm Nay: x1.5 vàng OK không? (chỉnh trên component DailySpecialManager).
4. Đưa PROMPT_SPRITE_FORGE_KITCHEN_ASSETS.md cho GPT — hàng về báo "1" để làm K2 (skin + animation).

---

## SPRINT K2 — SKIN + ANIMATION BẾP (2026-08-26 đêm) — QA PASS

### Assets nghiệm thu: 36/36 file Export_Kitchen_UI_Package ĐẠT (alpha sạch, 0 text, meta Single).
Nhận xét art: style flat-vector đơn giản, sạch, đồng bộ — ĐƠN GIẢN HƠN mockup (ít shading/depth).
Dùng tốt; nếu Sếp muốn "sang" hơn → round polish sau (thêm shading + outline dày như bộ train).

### Code K2 (backup đủ trong backup_train_2026-08-26/)
1. `KitchenSkin` (36 sprite serialize qua tool = đẹp cả trong build) — field trống tự fallback màu phẳng K1.
2. Áp skin toàn UI: tường/sàn tile, khung gỗ + giấy kem 9-slice, ribbon cam, thẻ nguyên liệu + viền chọn
   phát sáng, thanh vị track/fill/vạch đỏ sprite, nút NẤU xanh/xám sprite, tab pill on/off, Bỏ hết đỏ,
   lò đất + bàn sơ chế/trình bày/VÀO KHO/bảng đen art thật, decor (kệ chảo, chậu cây, bao bột, mèo ngủ).
3. Animation: Mèo Thần Tài vẫy 4f loop · lửa lò 4f + glow pulse khi nấu · KHÓI bốc từ lò · thanh %
   nướng chạy khi lò bận (reset khi cất/hỏng) · card punch scale khi chọn/bỏ chọn.
4. Thẻ KHOÁ theo cấp: `IngredientData.unlockLevel` (additive, mặc định 1) — tool tự đặt SEA_Milk = 14
   → thẻ Sữa khoá xám + ổ khoá + "Cấp 14" đúng mockup.
5. Tool Setup Kitchen UI v2 nâng cấp: tự gán 36 sprite skin + set Milk. QA agent: PASS (5 nit minor → đã vá 3).

### ANH CẦN LÀM (nghiệm thu K2)
1. Compile 0 đỏ → mở SampleScene → chạy LẠI `Tools → Farm Game → Kitchen → Setup Kitchen UI v2` → Ctrl+S.
2. Play: mèo vẫy tay · chọn thẻ thấy viền xanh + nảy · thẻ Sữa khoá Cấp 14 · NẤU → minigame → lò cháy
   lửa nhấp nháy + khói + % chạy · cất kho → lò nghỉ. So màn hình với mockup — chấm % giống.
3. Ưng art flat hiện tại hay đặt đội vẽ polish thêm depth? (không chặn gì — chạy tốt rồi mới polish).

## 2026-08-26 — Kitchen polish (chữ dễ đọc, nới khung) — HOÀN TẤT CODE
File sửa: Assets/_Game/Scripts/KitchenV2/KitchenSceneV2UI.cs (backup: production/backup_train_2026-08-26/KitchenSceneV2UI_v2.cs)

P1 (3 edits): banner đơn khách 460×104, tiêu đề trắng đậm 19pt nằm TRONG ruy băng (270×42); header "BẢNG CÔNG THỨC" trắng đậm 18pt nằm trong ruy băng 258×42.

P2 (17 edits) + P2B (2 edits):
- Bảng đen to hơn (236×128), chữ phấn 15pt sáng hơn
- Thanh trạng thái lò 214×34, chữ 16pt nâu sậm
- "Bàn sơ chế" & "Trình bày": chuyển vào PILL giấy kem đặt DƯỚI bàn (chữ 15pt nâu đậm, bold) — hết bị sprite nhỏ nuốt chữ; chữ Trình bày auto-size 9–15 (chuỗi dài "CHẠM ĐỂ CẤT VÀO KHO!" không tràn)
- Hộp VÀO KHO: 190×96, sprite phủ kín khung (preserveAspect=false) → 2 dòng chữ luôn nằm trên nền sprite; "VÀO KHO" 17pt vàng sáng bold, "Đã gửi N món" 14pt kem
- Tab Nguyên liệu/Gia vị: tab đang mở chữ nâu sậm bold, tab kia kem sáng
- Nút hành động 264×80, sub-text "chạm khay bên dưới" nâu sậm 13pt (hết bị trắng mờ trên nền xám)
- Chip nguyên liệu trên banner: 72×68, icon 38, tên auto-size 8–11 (hết tràn khung)

Verify: 22 edits tổng, count==1 mỗi anchor, braces 149/149, parens 1059/1059 cân.
ANH CẦN LÀM: Tools → Farm Game → Kitchen → Setup Kitchen UI v2, rồi Ctrl+S. Console phải 0 đỏ.

## 2026-08-26 — Kitchen R3: nút Về nông trại + nồi + mèo đi dạo + lửa prefab + decor — CODE XONG, CHỜ ART
Backup mới: KitchenSceneV2UI_v3.cs, KitchenV2SetupTool_v2.cs (cùng thư mục backup).

Code (KitchenSceneV2UI.cs — 8 edits, braces 163/163, parens 1163/1163):
- Banner ĐƠN CỦA KHÁCH hạ từ y-8 → y-42 (ruy băng hết bị mép màn hình cắt)
- Nút VỀ NÔNG TRẠI góc trái trên (170×92): gọi CookingSceneUI.BackToFarm() CŨ (logic scene giữ nguyên);
  skin.btnBackFarm — tạm dùng btn_back_to_farm.png cũ, chờ biển gỗ treo dây mới
- Skin mới: btnBackFarm, btnPaperSmall (Xem món khác), cookPot, decorGarlic/Onion/Herbs/Lights, catChefWalk[]
- Decor treo tường 2 cụm (tỏi/hành/thảo mộc) + 2 đoạn dây đèn + nồi nấu cạnh khay (600,-300)
- KitchenCatWalker (class mới cùng file): mèo đầu bếp đi qua lại sàn phải (x 500–900), đi→dừng 1.2–3.2s→quay đầu,
  lật localScale.x theo hướng, 6 frame walk; chỉ dựng khi có frame art
- Lửa lò prefab: TrySpawnFirePrefab() — có ovenFirePrefab thì chuyển canvas sang ScreenSpaceCamera,
  spawn Area_fire_red (Lana Studio) vào Oven_Mouth, sortingOrder = canvas+1, tắt lửa frame cũ;
  bỏ trống prefab = fallback lửa frame như cũ. fireScale chỉnh được trên inspector.

Tool (KitchenV2SetupTool.cs — 2 edits, braces 41/41): SOpt/SArrOpt — asset CHƯA giao chỉ log "(chờ art)",
không tính lỗi; tự gán prefab Area_fire_red vào ovenFirePrefab.

Prompt đội vẽ: production/PROMPT_SPRITE_FORGE_KITCHEN_R3.md (19 file: 7 nút/tab, 5 nồi+decor, 6 frame mèo + luật NO TEXT).

## 2026-08-26 — Kitchen R3: ART VỀ ĐỦ 18 FILE ✔ + nới tiêu đề/bảng, hạ khung
Verify art: 18/18 file đúng kích thước spec 100%, meta spriteMode Single, không text, nền trong suốt,
6 frame mèo cùng canvas 256×240. Snapshot: production/backup_train_2026-08-26/Kitchen_R3_delivered/ (18 file).

Layout (6 edits, braces 163/163):
- Banner ĐƠN CỦA KHÁCH hạ tiếp -42 → -70 (Sếp: còn cao quá)
- Ruy băng ĐƠN CỦA KHÁCH 270×42 → 330×46; BẢNG CÔNG THỨC 258×42 → 310×46 (title hết chật)
- Bảng MÓN HÔM NAY 236×128 @(222,-122) → 300×140 @(240,-180) — dài ra, tụt xuống dưới banner, text nằm gọn trong bảng
- Kệ treo xoong -118 → -185, mèo thần tài x -170 → -230 (tránh bị banner hạ xuống đè lên)

ANH CẦN LÀM: Setup Kitchen UI v2 + Ctrl+S → Play xem: nút mọng mới, biển Về nông trại, nồi, dây đèn,
tỏi/hành treo tường, mèo đầu bếp đi dạo sàn phải, lửa prefab trong lò.

## 2026-08-26 — HOTFIX: UI cũ nổi lên trong Play — NGUYÊN NHÂN lửa prefab
Bug: TrySpawnFirePrefab đổi canvas mới sang ScreenSpaceCamera → canvas UI CŨ (Overlay) luôn được
Unity vẽ ĐÈ lên canvas camera → toàn bộ UI cũ hiện lên trên. UI mới không mất, chỉ bị đè.
Fix: thêm cờ useCameraCanvasForFire (mặc định FALSE, tooltip cảnh báo); TrySpawnFirePrefab return sớm
khi cờ tắt; tool GỠ prefab khỏi ovenFirePrefab + ép cờ false. Lửa quay về frame 4 khung như cũ.
Lửa prefab sẽ bật lại ở K3 sau khi xóa hẳn UI cũ (gán prefab + tick cờ).
ANH CẦN LÀM: chạy lại Setup Kitchen UI v2 + Ctrl+S → Play: UI mới phủ lại như trước.

## 2026-08-26 — Kitchen R4: trả lại 11 file art lệch style + dời vùng mèo đi dạo
Sếp chê R3: nồi/tỏi/hành/thảo mộc/đèn vẽ vector bẹt "như game khác", mèo sai dáng (cần chibi đứng 2 chân
như ảnh mẫu). Nút + biển Về nông trại + tab ĐƯỢC DUYỆT, giữ nguyên.
- Code (2 edits): vùng mèo đi dạo dời từ sàn phải (500..900, y-350) → dải sàn DƯỚI 2 bàn trưng bày
  (x -280..260, y -120) — hết vướng nút CHỌN NGUYÊN LIỆU.
- Prompt vẽ lại: production/PROMPT_SPRITE_FORGE_KITCHEN_R4_REDRAW.md (11 file ghi đè cùng tên:
  cook_pot, 4 decor, 6 frame mèo chibi đứng thẳng waddle). Sếp cần đính kèm 2 ảnh tham chiếu khi gửi.
- Bản R3 cũ vẫn còn snapshot ở backup Kitchen_R3_delivered/ nếu cần đối chiếu.

## 2026-08-26 — Kitchen R4b: hạ decor xuống sàn + kệ mèo thần tài + thêm 3 file vẽ lại
Sếp soi: "2 cục" dưới lò = sack_flour + cat_sleeping (art xấu, nhìn không ra) đang LƠ LỬNG trên tường;
mèo thần tài không có kệ; muốn 2 nồi treo (shelf_props) vẽ như asset farm.
- Code (4 edits, braces 163/163): Plant_L hạ y 60→-172 + thêm Plant_L2 (-232,-172) [mockup 2 chậu];
  Sack_Flour (−190,20)→(−150,−176); Cat_Sleeping (−120,14)→(−60,−184) — tất cả ngồi trên đường viền sàn;
  kệ gỗ panelBoard (126×20) + bảng tên panelPaper "Mèo Thần Tài" dưới mèo thần tài như mockup.
- Prompt R4 nâng 11→14 file: thêm kitchen_shelf_props (giàn treo 2 nồi đồng/gang + chuỗi ớt/tỏi),
  sack_flour, cat_sleeping vẽ lại.

## 2026-08-26 — Kitchen R4: NGHIỆM THU 14 FILE VẼ LẠI ✔
- Kích thước 14/14 đúng spec 100%; ghi đè đúng tên cũ (meta Single giữ nguyên); nền trong suốt, không text.
- Visual: nồi có khối + ánh kim + bệ gạch ✔; giàn treo 2 chảo đồng/gang + chuỗi ớt/tỏi ✔; bao bột nhìn ra
  bao bột ✔; mèo ngủ cuộn tròn thấy tai/đuôi ✔; mèo đầu bếp ĐỨNG 2 CHÂN mũ trắng + tạp dề hồng viền bèo ✔.
- Kiểm tra động: 6 frame mèo lệch nhau 5k–13.6k pixel, centroid nhấp nhô 113→120 → waddle chạy đúng.
- Snapshot: backup_train_2026-08-26/Kitchen_R4_delivered/ (14 file).
- Nhận xét thật: style đã có khối/gradient tốt hơn nhiều, vẫn hơi "vector sạch" hơn ảnh mẫu painterly,
  nhưng ở cỡ hiển thị in-game (48–120px) nhìn ổn — chờ Sếp duyệt trên màn hình thật.
ANH CẦN LÀM: Setup Kitchen UI v2 + Ctrl+S → Play xem tổng thể; mèo đi dưới 2 bàn + kệ/bảng tên mèo thần tài
+ decor ngồi trên sàn đã vào từ đợt code trước.

## 2026-08-26 — Kitchen R5: feedback vòng Play — 14 edits code + prompt vẽ lò
Làm rõ hiểu nhầm: "cái nồi như mẫu" Sếp muốn = CÁI LÒ TO trong mockup. Nồi súp nhỏ cạnh nút → XÓA.
Code (14 edits, braces 180/180, parens cân):
- Xóa Deco_CookPot (nồi nhỏ cạnh nút CHỌN NGUYÊN LIỆU)
- Chữ MÓN HÔM NAY hết tràn viền: inset 22/16 (trước 10/6)
- Bảng công thức hạ -118 → -152 (tránh chạm nhầm nút Về nông trại)
- Lò phóng to 230×190 → 280×240 (mouth 140×100, body 260×225, glow/fire to theo) — chờ art R5
- Chấm màu tròn trước tên vị (Ngọt hồng/Cay đỏ/Chua xanh/Đậm dương/Kết cấu nâu) — sprite tròn
  GetDotSprite() vẽ bằng code, không cần asset; label dời x30
- Khay: MakeGrid bọc ScrollRect (Viewport+RectMask2D+ContentSizeFitter, kéo tay/chuột + lăn chuột,
  Clamped, sensitivity 24) — CẢ nguyên liệu & gia vị; ShowTrayTab toggle cụm scroll
- Hệ mua slot: mặc định 7 "Ô trống"/tab (PlayerPrefs kitchen_extra_slots_v2_*), nút xanh dương
  "+ Mở 7 ô · N vàng" — SpendGold qua FarmEconomyManager cũ, giá leo thang baseCost×(gói+1),
  serialized slotPackSize=7 / slotPackBaseCostGold=500 (⚠ CHỜ SẾP DUYỆT GIÁ); toast báo đủ/thiếu vàng
Prompt: production/PROMPT_SPRITE_FORGE_KITCHEN_R5_OVEN.md (oven_body 512×512 + oven_glow theo mockup).

## 2026-08-26 — Kitchen R5 lò NGHIỆM THU ✔ + chốt hướng art theo Sếp (giữ góc, chỉ repaint)
- Sếp bác phương án isometric full (A+B+C) — GIỮ góc nhìn + layout + assets hiện tại,
  chỉ nâng nét vẽ cho cùng chất farm. Không đổi code.
- oven_body 512×512 + oven_glow 300×220: đúng spec, đúng mockup (vòm+ống khói+miệng rỗng+bệ 2 hộc củi),
  backup Kitchen_R5_oven_delivered/. Đạt kết cấu; chất liệu sẽ nâng ở đợt repaint.
- Prompt mới: production/PROMPT_SPRITE_FORGE_KITCHEN_R6_REPAINT.md — công thức chất liệu 6 điều
  (3 lớp shading, texture, outline ấm biến thiên, bóng trong, palette farm) + 3 đợt file ưu tiên,
  GHI ĐÈ cùng tên, không cần sửa code/tool.

## 2026-08-26 — R6 mở rộng theo lệnh Sếp: REPAINT TOÀN BỘ SCENE (48 file, 3 đợt)
Sếp làm rõ: vẽ lại cả scene bếp cho nét thật hơn, đỡ AI — giữ nguyên mọi thứ khác.
Prompt R6 viết lại: công thức "đỡ AI" 7 điều (thêm luật bất đối xứng có chủ ý, cấm gradient máy móc)
+ đủ 48 file chia 3 đợt (10 nền/công trình → 17 prop/nhân vật/lửa → 21 UI skin). Ghi đè cùng tên,
0 thay đổi code. File: production/PROMPT_SPRITE_FORGE_KITCHEN_R6_REPAINT.md

## 2026-08-26 — R6 đợt 1: nghiệm thu 8/10, TRẢ 2 file giao bản cũ
- Kiểm pixel vs backup: oven_body chỉ 1,7% khác R5, kitchen_shelf_props 0,1% khác R4 → giao lại hàng cũ,
  TRẢ (production/PROMPT_SPRITE_FORGE_KITCHEN_R6_TRA_HANG_DOT1.md).
- ĐẠT: wall_tile (vân gỗ ✔), floor_tile (bevel+ron ✔), chalkboard (vệt phấn, canvas 400×320),
  warehouse_hatch (thiết kế mới vuông 380×380, góc đinh tán + biển trống), prep/plating table (512×300,
  có thớt/dao/đĩa viền xanh — tạm đạt, dặn đợt 2 đậm chất liệu hơn), panel_board_wood, oven_glow.
- Code fit canvas mới (2 edits, braces 180/180): VÀO KHO rect 190×96 → 160×152 @(-250,108)
  (art vuông, tránh bóp méo); bảng đen 300×140 → 280×175 (art 1.25:1).
- Snapshot: Kitchen_R6_dot1_delivered/ (10 file).
ANH CẦN LÀM: gửi prompt trả hàng cho đội vẽ; Setup Kitchen UI v2 + Ctrl+S xem 8 file mới trên scene.

## 2026-08-26 — Sự cố mất hình + trả nền gỗ + nghiệm thu 2 file vẽ lại
- NGUYÊN NHÂN scene trắng: đội vẽ ghi đè .meta bản rút gọn THIẾU `textureType: 8` → Unity import
  thành Texture thường, mất Sprite sub-asset → mọi tham chiếu null (tường trắng, lò khối nâu, kho/bàn mất).
  Tool LoadSpriteFixed tự sửa khi chạy lại. Đã ghi luật mới vào prompt: đội vẽ KHÔNG ĐƯỢC ghi đè .meta.
- Theo lệnh Sếp: KHÔI PHỤC nền cũ từ git local (git show HEAD: → ghi đè, không commit/push):
  kitchen_wall_tile, kitchen_floor_diamond_tile, panel_board_wood (+ meta gốc). Gỡ wall/floor/panel/
  paper/maneki/nút R3 khỏi danh sách repaint (giữ nguyên vĩnh viễn theo ý Sếp).
- Lưu ý: .git/index.lock bị kẹt do mount cấm xóa → đã rename thành .git/index.lock.stale_20260826
  (vô hại; Sếp có thể xóa tay).
- Nghiệm thu 2 file vẽ lại: oven_body khác 24,5% (loang đất nung + gạch mạch vữa + củi vân năm ✔),
  shelf_props khác 19,8% (chảo đồng nhiều lớp + tỏi bất đối xứng + ớt cong ✔) — ĐẠT.
  Backup: Kitchen_R6_trahang_redelivered/.
ANH CẦN LÀM: mở SCENE BẾP (SampleScene) → Setup Kitchen UI v2 + Ctrl+S (tool tự sửa toàn bộ meta hỏng
và gắn lại sprite) → Play kiểm tra: nền gỗ cũ trở lại, lò + giàn treo bản đẹp mới.

## 2026-08-26 — R7: căn giữa chữ bảng + zZz mèo ngủ + chuẩn nhân vật cho đợt 2
- Code (3 edits, braces 183/183): _txtChalk TopLeft → Left (khối chữ nằm giữa bảng theo chiều dọc);
  mèo ngủ có "z Z z" bay lên lơ lửng (KitchenZzzFloat — sin ngang + trôi lên + fade + scale, cycle 2.4s,
  thuần code không cần art).
- Prompt R6 bổ sung: chuẩn nhân vật = balaohangrong.png (cel-shading mềm, outline ấm biến thiên,
  chibi mũm mĩm); cat_sleeping vẽ lại hẳn (không bake zZz), cat_chef_walk 6 frame theo cùng chất.
ANH CẦN LÀM: Setup Kitchen UI v2 + Ctrl+S → Play xem chữ bảng + zZz; gửi prompt bổ sung + ảnh bà lão
cho đội vẽ trước khi họ chạy đợt 2.

## 2026-08-26 — Hệ "chỉnh tay giữ qua Play" cho Kitchen UI
Sếp muốn kéo object trong edit mode và Play vẫn giữ. Cơ chế (4 edits, braces 191/191 + tool 45/45):
- LayoutOverride[] serialized trên Kitchen_UI_v2 (path=tên khối cấp 1, pos/size/scale) — lưu theo scene
- ApplyLayoutOverrides() chạy CUỐI EnsureBuilt → chỉnh tay LUÔN thắng code, cả edit/Play/rebuild/tool
- CaptureLayoutOverrides(): chụp hiện trạng → rebuild chuẩn code → so lệch >0.5px → CHỈ lưu khối đã kéo
  → áp lại ngay (thông minh: không đóng băng khối chưa đụng, code sau này sửa layout vẫn ăn)
- Menu mới: Tools/Farm Game/Kitchen/「Lưu vị trí chỉnh tay」 và 「Xóa toàn bộ vị trí chỉnh tay」(có Undo)
- Giới hạn: chỉ khối CẤP 1 (Order_Banner, Chalkboard, Oven, Prep_Table, Warehouse_Box, decor...) —
  đủ cho nhu cầu kéo bố cục; chỉnh sâu bên trong khối vẫn cần code.
QUY TRÌNH SẾP: kéo/resize khối trong edit mode → menu Lưu vị trí chỉnh tay → Ctrl+S → Play giữ nguyên.

## 2026-08-26 — R6 đợt 2: nghiệm thu 6/22 file, TRẢ 11, khôi phục maneki
Kiểm pixel từng file:
- ĐẠT: oven_fire_01..04 (~29% khác, giữ đúng canvas 256), plant_pot (32,9% — chậu mới có nụ hồng, đạt),
  sack_flour (11% — tạm nhận, xem trong game).
- TRẢ 11 file: cat_chef_walk×6 + deco_garlic/onion/herb/lights = 0,0% khác R4 (giao hàng cũ lần 2);
  cat_sleeping chỉ sửa 8,9% chưa đạt chuẩn bà lão. Phiếu: PROMPT_SPRITE_FORGE_KITCHEN_R6_TRA_HANG_DOT2.md
- VI PHẠM: (1) lại ghi đè 36 .meta thiếu textureType:8 — tool auto-repair khi chạy, luật CHỈ GIAO PNG
  ghi vào phiếu; (2) maneki_idle×4 bị vẽ lại TRÁI LỆNH giữ nguyên → đã khôi phục bản cũ từ git
  (bản mới còn ở backup Kitchen_R6_dot2_delivered — Sếp thích thì áp lại 1 phút).
- .git/index.lock kẹt lần 2 → rename .stale_2 (mount cấm xóa; vô hại).
ANH CẦN LÀM: Setup Kitchen UI v2 + Ctrl+S (tool tự sửa 36 meta hỏng) → Play xem lửa lò mới + chậu cây;
gửi phiếu trả hàng đợt 2 cho đội vẽ.

## 2026-08-26 — R6 đợt 2 vẽ lại: NGHIỆM THU 9/11 ✔, trả nốt 2 file lần 3
- ĐẠT (pixel-diff thật + hình đúng chuẩn): cat_sleeping 34,3% khác (mềm, tai/đuôi rõ, má hồng),
  cat_chef_walk×6 ~15% (mũ 3 múi + tạp dề viền bèo, giữ dáng/canvas, frame vẫn lệch nhau đúng nhịp),
  deco_garlic 26,9% (củ to nhỏ + gốc tím), deco_onion 26,9% (nâu-cam/tím bóng).
- TRẢ LẦN 3: deco_herb_bunch + deco_string_lights = 0,0% khác R4 (lần 3 giao bản cũ).
  Phiếu: PROMPT_SPRITE_FORGE_KITCHEN_R6_TRA_HANG_DOT2_LAN2.md (chuẩn đối chiếu = tỏi/hành vừa duyệt).
- Backup: Kitchen_R6_dot2_redelivered/ (11 file).
ANH CẦN LÀM: Setup Kitchen UI v2 + Ctrl+S → Play ngắm mèo bếp mới đi lạch bạch + mèo ngủ + tỏi hành;
gửi phiếu trả 2 file cho đội vẽ.

## 2026-08-26 — R6 đợt 2 CHỐT SỔ ✔ + nâng mèo bếp
- 2 file cuối vẽ thật: deco_herb_bunch 54,2% khác (bó lá + oải hương — hơi rậm, chờ Sếp duyệt in-game),
  deco_string_lights 19,9% (bóng có lõi sáng + vệt thủy tinh + đui lệch góc — ĐẠT). Đợt 2 xong 11/11.
- Mèo đầu bếp nâng y -120 → -84 để thấy nguyên thân, không bị khay che chân.
Còn lại R6: đợt 3 UI skin (thẻ/thanh vị/ruy băng/chip/khóa) — chờ lệnh Sếp.
