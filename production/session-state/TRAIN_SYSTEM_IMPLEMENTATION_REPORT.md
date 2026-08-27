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

## 2026-08-26 — ĐẠI PHẪU: Kitchen UI chuyển sang HIERARCHY THẬT (bake-once + bind)
Lệnh Sếp: bỏ tool lưu vị trí; UI nằm hẳn trong Hierarchy để kéo chỉnh trực tiếp, Play giữ nguyên,
bảo trì không cần sửa code. Backup: KitchenSceneV2UI_v4.cs + KitchenV2SetupTool_v3.cs.

Kiến trúc mới (braces 211/211, parens 1457/1457; tool 43/43):
- Start: hierarchy có sẵn ("Order_Banner" tồn tại) → BindExistingHierarchy() — KHÔNG dựng lại;
  scene trống → RebuildNow() dựng khung lần đầu.
- BindExistingHierarchy(): nối ~60 refs theo path (banner/board detail+list/5 flavor rows/lò/khay/nút),
  gắn lại toàn bộ onClick (listener runtime không serialize được), gán lại sprite chấm tròn
  (sprite code-gen không lưu vào scene), DỌN nội dung động đã bake (thẻ/ô trống/danh sách món/chip)
  bằng DestroyImmediate rồi để BuildTrayCards/RebuildDishList sinh lại. Thiếu path nào → warning rõ tên.
- ĐÃ XÓA toàn bộ hệ layout-override (field + 3 method + 2 menu tool).
- Tool Setup: khung đã tồn tại → dialog "Giữ khung hiện tại (chỉ nạp data/skin)" [mặc định an toàn]
  vs "DỰNG LẠI từ đầu" (xóa chỉnh tay). Art ghi đè cùng file → Image trong scene tự nhận, không cần relink.
QUY TRÌNH SẾP TỪ NAY: kéo/chỉnh mọi thứ trực tiếp trong Hierarchy → Ctrl+S → Play là đúng y vậy.
Nội dung ĐỘNG (thẻ nguyên liệu, danh sách món, chip cần, ô trống mua slot) vẫn do logic sinh — đừng
chỉnh tay bên trong 2 Grid/Content/Need_Chips (sẽ bị dọn khi Play).

## 2026-08-27 — Bàn giao Đợt R7: mèo trắng đầu bếp đi lại (6 frame) + mèo vàng ngủ cuộn tròn
Backup ngay khi nhận: production/backup_train_2026-08-26/Kitchen_R7_cats_delivered/ (7 file png+meta).
QA:
- Kích thước KHÔNG đổi so với bản trước (walk 256x240 x6, sleeping 300x200) → không cần chỉnh
  RectTransform/anchor của Cat_Chef, Cat_Sleeping trong Hierarchy.
- Pixel-diff vs bản backup gần nhất (Kitchen_R6_dot2_redelivered): walk 30.3–32.0%, sleeping 46.7%
  khác biệt → vẽ MỚI thật, không phải hàng cũ dán nhãn lại.
- .meta: mtime KHÔNG đổi (26/8, trước giờ giao hàng) → đội vẽ không đụng vào, giữ đúng GUID.
  spriteMode: 1 (Single), textureType: 8 (Sprite) — đúng chuẩn.
- Alpha channel: 4 góc mỗi ảnh alpha=0 (nền trong suốt), không có nền/bóng bake theo ART_RULES_STUDIO.
- Không có chữ "z Z z" bake vào cat_sleeping.png — đúng yêu cầu (chữ bay là code KitchenZzzFloat).
- Xem trực tiếp: mèo trắng đội mũ đầu bếp + tạp dề hồng, mèo vàng sọc cam cuộn tròn ngủ — đúng mô tả,
  đúng phong cách nét vẽ mềm (viền nâu hữu cơ), khớp chuẩn nhân vật balaohangrong.png.
KẾT LUẬN: NHẬN HÀNG — cùng path/GUID cũ nên Unity tự re-import khi mở lại Editor, KHÔNG cần sửa code
hay chạy lại Setup tool. Hierarchy đã bind sẵn Cat_Chef (KitchenCatWalker.catChefWalk[]) và Cat_Sleeping
(Image.sprite) trỏ đúng GUID này từ trước.

## 2026-08-27 — Feedback vòng 2: chip vị banner + chữ tràn bảng đen + card khung mở ô + mèo đứng im
Backup trước khi sửa: production/backup_train_2026-08-26/KitchenSceneV2UI_v5.cs,
KitchenV2SetupTool_v4.cs. Braces 221/221, parens 1548/1548 (script chính); tool 43/43.

1. MÈO ĐỨNG IM — đã soi trực tiếp trong SampleScene.unity (không đoán mò):
   KitchenCatWalker trên Cat_Chef: m_Enabled=1, frames[] đủ 6 sprite hợp lệ (đúng GUID R7),
   minX=-280/maxX=260 khớp code, vị trí (-100,-84) khớp. Dữ liệu ĐÚNG 100% — không thấy bug.
   Nghi là do xem đúng lúc mèo đang "pause" (code có nghỉ ngẫu nhiên 1.2–3.2s mỗi lần quay đầu) hoặc
   ảnh chụp tĩnh. Sếp Play thử quan sát liên tục ~10s hoặc quay 1 clip ngắn gửi lại nếu vẫn thấy đứng yên.

2. Chip vị trên banner "ĐƠN CỦA KHÁCH": thay dòng chữ "Ngọt 2 · Cay 0..." bằng 5 chip nhỏ
   (chấm tròn màu + số), dùng ĐÚNG bảng màu đã có ở Bảng công thức (hồng/đỏ/xanh/dương/nâu) —
   tự sinh khi Play (BindExistingHierarchy tự dọn dòng chữ cũ + tạo 5 chip nếu chưa có).

3. Bảng đen "MÓN HÔM NAY" tràn chữ: bật TMP auto-size (9–15pt, tự co vừa khung) — hết tràn viền
   dù danh sách món dài ngắn khác nhau. Muốn bảng TO hơn nữa: Sếp kéo tay trực tiếp trong Hierarchy
   rồi Ctrl+S là xong (đúng tinh thần hierarchy sống — không cần report code nữa).

4. Nút "+ Mở N ô · giá vàng": trước giờ chỉ là ô màu phẳng — đã gắn skin.btnGreen (art nút xanh
   ĐÃ CÓ SẴN từ R3, chưa dùng tới) → giờ có khung bo góc/viền như các nút khác trong game, không cần
   art mới.

5. Icon vàng (+vàng): thêm field skin.iconGold + code tự lên khi có art (bảng đen góc phải + nút
   mở ô) — hiện CHƯA có art nên chưa hiện, không lỗi. Đã thêm PROMPT_SPRITE_FORGE_KITCHEN_R7_ICONS_DECOR.md
   (icon_gold, plaque_oven_state, deco_crate_stack, deco_firewood) — icon vàng tự lên hình ngay khi
   giao, không cần sửa code thêm lần nào nữa.

6. "Lò chưa nhóm" chưa có assets riêng + kho/lò còn trống: đưa vào prompt R7 trên (bảng gỗ treo lò
   9-slice + thùng gỗ cạnh kho + bó củi cạnh lò) — chờ đội vẽ, sẽ tự lên hình khi giao đúng tên file.

TẤT CẢ thay đổi trên đều theo đúng kiến trúc bind-not-rebuild: chip/icon là nội dung tự sinh mỗi lần
Play (không cần Ctrl+S), auto-size là thuộc tính component (không đụng vị trí Sếp đã chỉnh tay).

## 2026-08-27 — Bàn giao R7 icon/decor (4 file) + phát hiện & vá lỗ hổng kiến trúc Bind
Backup art: production/backup_train_2026-08-26/Kitchen_R7_icons_decor_delivered/. Backup script:
KitchenSceneV2UI_v6.cs (trước vá) → v7.cs (sau vá). Braces 227/227, parens 1597/1597.

QA 4 file giao (icon_gold 64x64, plaque_oven_state 300x64, deco_crate_stack 160x140,
deco_firewood 140x112): kích thước đúng spec, alpha trong suốt 100%, không text bake, soi mắt đạt
chuẩn ART_RULES. Lưu ý nhỏ: cả 4 file .meta ghi spriteMode: 2 (Multiple) thay vì 1 (Single) —
KHÔNG chặn gì vì LoadSpriteFixed trong Setup tool tự sửa về Single khi nạp qua tool, nhưng nhắc đội
vẽ lần sau xuất đúng Single luôn cho sạch.

PHÁT HIỆN: khi soi lại code, thấy phần wiring cho plaqueOvenState/decoCrateStack/decoFirewood
(đổi khung "Lò chưa nhóm" sang plaque gỗ + thêm Deco_Crates/Deco_Firewood) CHỈ có trong BuildStage()
(đường dựng-từ-đầu) — KHÔNG có trong BindExistingHierarchy() (đường Play thật đang dùng vì Hierarchy
đã có sẵn) → nếu để vậy thì 3 thứ này SẼ KHÔNG BAO GIỜ lên hình khi Play (trừ khi "DỰNG LẠI từ đầu"
— xóa hết chỉnh tay, không nên). Đã vá: thêm đúng 3 dòng self-heal vào BindExistingHierarchy (đổi
skin Oven_StateBar 1 lần duy nhất theo sprite-identity guard — không đụng nếu Sếp đã chỉnh tay sau
đó; tự tạo Deco_Crates/Deco_Firewood nếu Hierarchy chưa có). iconGold đã tự self-heal đúng từ trước
(EnsureChalkGoldIcon + BuildSlotShop chạy động mỗi lần).

CẦN 1 BƯỚC TRONG UNITY LẦN NÀY (khác mọi lần trước): 4 field mới (iconGold, plaqueOvenState,
decoCrateStack, decoFirewood) là field CHƯA TỪNG có giá trị — phải bấm 1 lần
Tools → Farm Game/Kitchen → Setup Kitchen UI v2 → chọn "Giữ khung hiện tại" (an toàn, không xóa gì)
để 4 sprite mới được nạp vào field, rồi Ctrl+S. Sau đó Play là tự lên hình hết, không cần bấm lại.

## 2026-08-27 — Soát UI Chợ + toàn bộ popup trong SCN_Farm.unity (chỉ phân tích, CHƯA sửa gì)
Cách làm: viết tool Python đọc thẳng SCN_Farm.unity (585,339 dòng) qua device_bash, dựng lại cây
GameObject/RectTransform, tính rect tuyệt đối (nền 1920x1080) cho từng node trong 49 popup tìm
thấy trong scene, rồi so khớp bằng tay (không tin heuristic đè-nhau tự động vì báo sai rất nhiều —
lớp trang trí layer chồng lên nhau là bình thường). Script + dữ liệu ở /tmp/farmui/ trên máy Sếp
(scratch, không thuộc project).

BUG XÁC NHẬN #1 — Chợ (Canvas_MarketPopup > Panel_Dim > Popup_Board): Chip_Timer đè lên Header_Banner
190x46px. Chip_Timer neo góc trên-phải (offset cố định -310,-96 / size 280x52), Header_Banner neo
giữa-trên (width cố định 620) — 2 khung này vốn định vẽ cho Popup_Board rộng 1920 (bản gốc/độc lập
vẫn đúng, có 60px hở), nhưng bản Chợ đang dùng Popup_Board đã bị thu hẹp còn 1420 rộng (viền tối
Panel_Dim quanh) → 2 khung bị kéo chồng nhau. Cần rộng tối thiểu ~1800px mới hết đè hoàn toàn với
offset hiện tại. CHƯA sửa — cần Sếp chọn hướng: (a) thu hẹp width Header_Banner riêng cho bản Chợ,
hay (b) đẩy Chip_Timer/Chip_Gold/Btn_Refresh vào trong theo tỉ lệ mới, hay (c) nới Popup_Board bản
Chợ về gần 1920 (bớt viền tối 2 bên).

BUG XÁC NHẬN #2 — Settings_Icon (nút bánh răng góc phải-trên, xuất hiện 3 lần ở 3 màn hình dùng
chung TopRight_Anchor): Image không gán sprite (has_sprite=False, alpha=1 tức đang HIỆN nhưng là
khung trống/màu phẳng) — không phải icon do code gán lúc chạy (icon cài đặt là cố định, không đổi),
nên đây là icon thật sự đang thiếu, không phải chờ runtime.

ĐÃ SOÁT KỸ (tay, đối chiếu tọa độ thật) — SẠCH: Shop (popup_Menu, 73 node), Kho (WarehousePopup,
72 node), thẻ + tab prefab Market (MarketListingCard_Prefab, MarketCategoryTab_Prefab). LevelUpPopup
(60 node) sạch, có 2 chỗ trông như đè (Badge_EXP_Star ló lên trên card, Ribbon_LevelUp_Shadow phủ
lên khung avatar) nhưng nhiều khả năng là chủ đích trang trí (huy hiệu ló mép, dải ruy băng vắt qua
avatar) — chưa đủ chắc để gọi là bug, cần Sếp xác nhận bằng mắt trong Editor.

KHUNG/NỀN CHƯA CÓ ICON (đúng câu Sếp hỏi) — 56 chỗ lọc còn ý nghĩa sau khi bỏ Viewport/Mask/line
trang trí mỏng/item trong suốt:
- Khung gỗ "board" của Kho + Shop (Board_Border, Board_Fill_Top, Board_Fill_Bottom, 4x Stud_Rim/Base)
  — TOÀN BỘ khung nền 2 popup này đang là màu phẳng, chưa có texture gỗ/viền/đinh ốc nào — đây là
  chỗ thiếu LỚN NHẤT, không phải icon nhỏ mà cả cái khung nền chính.
- Settings_Icon (nút cài đặt, 80x80, 3 chỗ) — xem BUG #2 trên.
- TopLeft_Anchor: Avatar_Frame (khung avatar 120x120), EXP_Background+EXP_Fill (thanh kinh nghiệm),
  Level_Star (sao cấp độ 80x80).
- TopRight_Anchor: Diamond_Background, Gold_Background (nền chip kim cương/vàng góc phải).
- popup_Menu/ShopItem_Template: Lock_Overlay (icon khóa khi item chưa mở, 296x335).
- Popup_AvatarProfile: Panel_LeftAvatar (khung avatar bên trái, 310x310).
- Sickle_Bottom_Tray: BG_Image (220x130).
60 chỗ khác (Img_Icon/Img_OutputIcon/Template_.../Reward lặp lại nhiều lần trong MillPopup_Root,
Popup_LevelUp_Township, Popup_Pages, v.v.) nhiều khả năng do code gán sprite lúc chạy (theo dữ liệu
món/thưởng), không phải lỗ hổng art — ưu tiên thấp hơn.

PHẠM VI: mới soát tay kỹ 4/49 popup (Chợ, Shop, Kho, LevelUp) + 2 prefab Market. 45 popup còn lại
mới chỉ qua bước quét hình học tự động (dễ báo sai) — CHƯA xác minh tay. Chưa sửa bất kỳ file/scene
nào trong lượt phân tích này.

## 2026-08-27 (tiếp) — Quét đè/dính toàn bộ 48 popup + wireframe + plan fix (chưa sửa gì)
Mở rộng scan hình học ra hết 48/49 popup (bỏ qua "Canvas_Popup" vì nó là container cha lồng lại toàn bộ
popup con đã quét riêng — trùng lặp). Bộ lọc: chỉ tính "đè thật" khi 2 khung chồng PARTIAL (15%–85% diện
tích khung nhỏ hơn) — chồng >85% (khung nền chứa trọn nút/icon bên trong) là layer bình thường, không tính.
Kết quả: 280 cặp bị máy nghi, sau khi soát tay từng nhóm còn lại thật sự đáng chú ý:

1. Chợ — Chip_Timer đè Header_Banner (đã báo lượt trước, giữ nguyên).
2. Settings_Icon mất icon (3 chỗ) — ĐÃ TÌM THẤY ART CÓ SẴN: "Assets/Assetsgame/Fantasy Wooden GUI  Free/
   PNG/setting.png" (500x500, textureType Sprite, alpha trong suốt, đã có sẵn trong project) — không cần
   xin art mới, chỉ cần wire vào field Image.sprite của 3 Settings_Icon (rồi Setup tool tự sửa spriteMode
   2→1 khi nạp qua LoadSpriteFixed).
3. popup_SKPhucLoi (Sự Kiện Phúc Lợi) — lỗi TEMPLATE dùng chung ở cả 6 nút (btn_HatGiongHiem_PL,
   btn_CuocKC_PL, btn_TThanNong_PL, btn_TuiVang_PL, btn_PhanBon_PL, btn_ThuocHaGa_PL): khối chữ mô tả
   (Text (TMP) #1) đè lên 58px phần trên icon phần thưởng bên dưới. Sửa 1 lần ở template là hết cả 6 nút.
4. popup_DiemDanh — TxT_NgayTrongTuan lấn 25px vào icon lịch (img_Lich) — mức độ nhẹ, cần xem mắt.
5. Mill (MillSlotUI.cs) — ĐÃ ĐỌC CODE, KHÔNG PHẢI BUG: Img_LockIcon hình học đè giữa Txt_Name, nhưng
   BindUnlockGem/BindLockedLevel (2 trạng thái duy nhất bật icon khoá) đều gọi DatCongThuc(null) nên tên
   món luôn RỖNG khi icon khoá hiện — không bao giờ đụng nhau thật. Loại khỏi danh sách bug.
6. Các nghi vấn còn lại (level badge đè góc avatar, icon tiền tệ đè text trong chip, NEW-ribbon đè icon,
   lock icon đè icon món trong Mill khi ô khoá, hand tutorial đè template, diorama Sky/Ground/Machine
   trong Mill...) đều là MẪU THIẾT KẾ CHUẨN của game mobile (badge góc, ribbon chéo, icon+text chung khung,
   scene lớp cảnh) — không tính là bug.

Đã dựng 1 trang HTML "wireframe" (vẽ lại đúng toạ độ RectTransform thật, scale 1:1, có mã màu + khung đỏ
cho chỗ nghi đè thật) cho 48 popup vì session này không có quyền điều khiển Unity Editor để chụp màn hình
Play mode thật — đã gửi cho Sếp qua chat (file production/session-state/farm_ui_qa_report.html).

PLAN FIX (đề xuất, CHƯA làm — chờ Sếp chọn):
- Chợ: 3 lựa chọn — (a) thu hẹp Header_Banner riêng bản Chợ (rủi ro: có thể làm méo hình ruy-băng tiêu đề
  nếu ảnh nền banner chiếm hết bề ngang 620px), (b) dời/thu nhỏ hàng Chip_Gold+Chip_Timer+Btn_Refresh để
  vừa khung 1420 (chật, tổng bề ngang 3 khung + tiêu đề đã là 1390/1420 — gần như không còn khe hở), (c)
  nới khung Popup_Board bản Chợ về gần 1900-1920 như bản gốc đang đúng (ít rủi ro nhất, chỉ đổi 1 field,
  nhưng sẽ làm viền tối 2 bên hẹp lại/mất — cần xác nhận đây có phải chủ đích thiết kế "popup nhỏ có viền"
  hay không). Em nghiêng về (c).
- Settings_Icon: wire sprite có sẵn (setting.png) vào 3 field Image, không cần art mới.
- popup_SKPhucLoi: cần Sếp xác nhận hướng — dời icon xuống hay chữ lên/thu ngắn — trước khi sửa vì ảnh
  hưởng bố cục cả 6 nút.
- popup_DiemDanh: dời TxT_NgayTrongTuan sang phải ~25-30px hoặc kiểm lại text alignment — ưu tiên thấp.

## 2026-08-27 (tiếp) — ĐÃ SỬA TRỰC TIẾP 4 bug xác nhận trong SCN_Farm.unity
Backup TRƯỚC khi sửa: production/backup_train_2026-08-27/SCN_Farm_before_popup_fixes.unity (bản đầy đủ
585,339 dòng, 16.6MB). Mọi edit làm bằng Python exact-match trên khối YAML riêng từng fileID (giống cách
làm với file .cs), verify SAU mỗi edit bằng cách đếm lại dòng file (585339 → vẫn 585339, không đổi) + đếm
số document YAML (6907 → vẫn 6907) + parse lại toàn bộ scene bằng tool riêng để tính lại toạ độ tuyệt đối,
xác nhận từng chỗ hết đè bằng số liệu thật (không đoán).

1. CHỢ — Popup_Board (fileID 289397642, nằm dưới Canvas_MarketPopup/Panel_Dim, CHỈ CÓ 1 bản duy nhất trong
   scene — sửa lại kết luận lượt trước: không có "bản độc lập đúng" nào để so sánh, số liệu bug vẫn đúng vì
   tính trực tiếp từ RectTransform gốc): m_SizeDelta.x 1420 → 1880 (nới ngang thêm 460px theo đúng ý Sếp
   "nới chiều ngang dài ra"). Ảnh nền Popup_Board dùng Image Type=Sliced (9-slice) nên nới rộng KHÔNG làm
   méo art — an toàn, không cần art mới. Header_Banner/Chip_Timer giờ cách nhau 40px (trước đè 190x46px).

2. Settings_Icon (3 chỗ, TopRight_Anchor fileID 267176880/859190069/1256992121): gán m_Sprite trỏ vào sprite
   có sẵn "setting_0" trong Assets/Assetsgame/Fantasy Wooden GUI Free/PNG/setting.png (guid
   a0b086fa73c90f649bc7b01178cdaf4a) — không xin art mới, dùng lại asset đã có trong project.

3. popup_SKPhucLoi — 6 nút (Hạt Giống Hiếm, Cuốc KC, Thần Nông, Túi Vàng, Phân Bón, Thuốc Hà Gà, fileID
   Text lần lượt 74604549/475634480/828319582/185021642/155857399/1058791182): thu chiều cao khối chữ mô tả
   từ 114.8px xuống 53.9px (m_SizeDelta.y -169.26→-230.17, m_AnchoredPosition.y -24→-54.45, giữ nguyên đáy,
   hạ đỉnh xuống) — hết đè lên icon phần thưởng, còn cách 3px. Cả 6 nút cùng 1 template giống hệt nhau nên
   sửa 1 công thức áp cho cả 6.

4. popup_DiemDanh — TxT_NgayTrongTuan (fileID 1241217200): dời phải + thu hẹp (apos.x 18.89→42.47,
   sizeDelta.x 201.52→188.79) — hết đè icon lịch (cách 5px) mà vẫn nằm gọn trong khung img_KhungLich
   (cách viền phải 5px), không tràn ra ngoài.

QUAN TRỌNG — KHÔNG đụng tới 44/49 popup còn lại: sau khi quét sâu lại (đối chiếu số liệu thật, không phải
đoán) toàn bộ các "nghi vấn" còn sót (badge cấp độ đè góc avatar, icon tiền tệ đè chữ trong chip, ruy-băng
NEW đè icon, icon khoá đè icon món khi ô khoá, tay hướng dẫn đè template, cảnh diorama Sky/Ground/Machine
trong Mill, 3 icon phần thưởng xếp chồng kiểu quạt trong RuongHiem...) đều là MẪU THIẾT KẾ CHUẨN của game
mobile — không phải bug. Không tự ý "giãn ra cho đẹp" trên các chỗ này vì: (1) không có bằng chứng cụ thể
là sai, (2) session này không có mắt nhìn Play mode thật để verify "đẹp hơn" hay chưa, tự đoán trên 44 popup
đang chạy đúng có nguy cơ tạo lỗi MỚI mà không ai phát hiện được cho tới khi Sếp mở Unity lên xem. Đã báo
lại Sếp xin hướng: Play-test 4 chỗ vừa sửa trước, nếu vẫn muốn nới thêm chỗ nào cụ thể thì báo, em sẽ làm
đúng chỗ đó có verify đàng hoàng thay vì sửa mù hàng loạt.

VERIFY CUỐI: parse lại toàn bộ file sau khi sửa — không phát sinh overlap MỚI ở cả 4 chỗ vừa sửa lẫn xung
quanh (Chip_Gold/Btn_Refresh/Rail_Categories/Panel_ListingArea trong Chợ vẫn nguyên vị trí, không bị ảnh
hưởng bởi việc nới Popup_Board vì chúng neo theo anchor riêng).


---

## [2026-08-27, tiếp] Đợt 2: 2 fix title-clearance + hội ý thiết kế + kết luận cho 44/49 popup còn lại

Theo lệnh Sếp "cứ làm xong hết 44 popup còn lại tôi review 1 lần / bạn cùng đội ngũ hội ý thiết kế sao ok
đẹp chỉnh chu nhất". Đã làm các việc sau:

### A. 2 fix mới đã áp dụng + verify (backup trước khi sửa, kiểm tra dòng/YAML doc không đổi sau sửa)

5. Popup_WarehouseUpgrade/Txt_UpgradeTitle (RT fileID 1727809479): m_AnchoredPosition.y 223→234 (nâng tiêu
   đề lên 11px) — khoảng cách tới Img_UpgradeWarehouse bên dưới từ <12px lên 20px.
6. WarehousePopup/Right_DetailPanel/Txt_DetailHeader (RT fileID 868596315): m_AnchoredPosition.y 320→333
   (nâng 13px) — khoảng cách tới Circle_Preview bên dưới lên 20.5px.

Cả 2 đều dùng chuẩn quét "khoảng hở tiêu đề < 12px = nghi vấn thật", tìm được đúng 2 trường hợp trong toàn
bộ 29 popup (đã loại trùng lặp do cách quét bắt theo tên, xem mục C). Verify: parse lại toàn bộ scene sau
sửa — số lượng GameObject/RectTransform/MonoBehaviour không đổi, tổng số cặp overlap không tăng (265, y hệt
trước 2 fix này).

### B. Hội ý thiết kế (Workflow đa-agent, đã xin phép Sếp 2 lần trước khi dùng)

Dùng 3 agent góc nhìn khác nhau (thoáng/generous, gọn/compact, hệ thống-8px) đề xuất bộ 4 số chuẩn, rồi 1
agent tổng hợp chốt bằng median (an toàn thống kê, không thiên vị phe nào):

- Khoảng cách tối thiểu 2 object anh em (sibling gap): **24px**
- Khoảng hở tiêu đề → nội dung (title clearance): **56px**
- Bề rộng tối thiểu cho popup lớn (chợ/kho/nhà máy/sự kiện): **75% bề ngang canvas**
- Lề trong tối thiểu (inner padding): **32px**

Đây là các mức SÀN cho các đợt chỉnh sửa/redesign SAU này, không phải lệnh áp máy móc ngay — vì 2 fix ở
mục A hiện chỉ đạt ~20px (dưới mức 56px khuyến nghị). Lý do CHƯA đẩy tiếp lên 56px: cả 2 popup không có
object "khung card nền" riêng để tính biên trên an toàn — đẩy tiêu đề lên thêm ~36px nữa có nguy cơ chạm
nút đóng (Btn_UpgradeClose, Btn_X ở Warehouse) hoặc vượt ra ngoài khung card thật mà dữ liệu scene không
thể hiện rõ (không có Play mode để nhìn kết quả). 56px là chuẩn khuyến nghị cho đợt polish có kiểm tra
bằng mắt (Play mode/Editor), không áp mù trên số liệu YAML.

### C. Vì sao KHÔNG sửa tiếp 55 cặp "khoảng cách sát nhau" tìm được (dù có chuẩn 24px của đội thiết kế)

Quét lại theo mọi cặp anh em có khoảng hở <8px trên 29 popup canonical (đã loại trùng do bug quét theo tên
— nhiều "popup" bị đếm trùng vì thực ra là con lồng trong 1 popup khác: Popup_Board, PopupRoot,
Popup_01..04_*, ShopItem_Template, TxT_TenPopup). Kết quả 55 cặp, đã xem trực tiếp dữ liệu từng cụm đại
diện: icon+chữ chung khung nhỏ (2-4px), răng bánh răng (1.8px), progress-bar sát nút (6px), icon phần
thưởng sát khung caption (2px) — TUYỆT ĐẠI ĐA SỐ là thiết kế cố ý, đúng như lưu ý loại trừ của chính đội
thiết kế ("cụm overlap chủ đích... coi là 1 compound object"). Không có Play mode để phân biệt "cần giãn"
với "vốn sát là đúng" theo từng cụm, nên quyết định KHÔNG sửa hàng loạt — làm mù trên 55 cụm có nguy cơ phá
vỡ nhiều chỗ đang ổn hơn là sửa được vài lỗi thật còn sót.

### D. Vì sao KHÔNG nới rộng thêm popup nào khác ngoài Chợ (dù chuẩn đội thiết kế là 0.75 tỉ lệ rộng)

Thử heuristic so bề rộng "popup root" với bounding box nội dung con — heuristic chọn sai "root" cho nhiều
popup (bắt trúng anchor nhỏ hoặc lớp overlay full-screen thay vì khung card thật), ra kết quả TIGHT âm vô
lý. Không tin tưởng đủ để dùng làm căn cứ sửa thêm — cần Sếp/đội xem trực tiếp Editor mới xác định popup
nào thực sự cần nới.

### E. Việc art còn lại — Kho + Shop chưa lên da khung gỗ

Không phải lỗi layout — RectTransform sửa không giải quyết được, cần texture mới. Đã viết prompt đầy đủ
(kèm luật ART_RULES_STUDIO) tại `production/session-state/qa-reports/ART_PROMPT_WoodBoard_Kho_Shop.md`,
gồm 2 file cần vẽ: WoodBoard_Frame.png (khung bảng gỗ 9-slice) + WoodBoard_Stud.png (đinh tán góc, thay 3
vòng tròn placeholder hiện tại).

### F. Báo cáo QA cập nhật

`production/session-state/qa-reports/farm_ui_qa_report_2026-08-27.html` — bản wireframe đầy đủ 48 popup,
6 thẻ lỗi đã đánh dấu "✅ ĐÃ SỬA", 1 thẻ "⚪ đã kiểm tra lại — false positive" (Mill), 1 thẻ "🔴 CÒN LẠI —
cần art" (Kho/Shop), thêm khối ghi chú giải thích quyết định không sửa hàng loạt 55 cặp + chuẩn số của đội
thiết kế.

TỔNG KẾT ĐỢT NÀY: 6/6 lỗi layout thật tìm được qua quét toàn bộ 48 popup đã sửa và verify xong. Phần còn
lại của "44 popup" theo cách đếm ban đầu của Sếp thực chất là ~23 popup KHÔNG có lỗi genuine sau khi quét
kỹ (49 tên quét được → 29 popup thật sau khi loại trùng do bug quét → 6 đã sửa → 23 sạch, không cần động
tới). 1 việc còn mở là art (mục E), không phải bug code/layout.


---

## [2026-08-27, tiếp 2] Gán art khung gỗ Kho + Shop (đội vẽ đã giao 2 file)

Đội vẽ giao `WoodBoard_Frame.png` (512×512, spriteMode Single, spriteBorder 64/64/64/64, guid
`9463ae1464ad4dc79019035afcd75785`) và `WoodBoard_Stud.png` (64×64, không border, guid
`963c6e2c679044bb92ed38ff3f348c3e`) vào `Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/`, kèm sẵn
`.meta` (lưu ý: đội vẽ có đụng `.meta` lần này — khác luật thường "art không đụng meta" — nhưng đây là 2
file HOÀN TOÀN MỚI, không phải sửa meta của asset cũ nào, và nội dung đúng chuẩn Unity TextureImporter
(guid hợp lệ, spriteMode/border khớp đúng yêu cầu trong prompt) nên không có rủi ro va GUID hay hỏng asset
cũ — đã kiểm tra không trùng guid với asset nào khác trước khi dùng).

Backup trước khi sửa: `production/backup_train_2026-08-27/SCN_Farm_before_woodboard_art.unity`.

ĐÃ GÁN (10 field sprite/type/color, đều verify từng cái):
- `WarehousePopup/Board_Border` (Image fileID 456646091) + `popup_Menu/Board_Border` (fileID 151284459):
  m_Sprite → WoodBoard_Frame (fileID 21300000 — hằng số Unity chuẩn cho sprite Single-mode, đã đối chiếu
  426 tham chiếu sprite khác trong scene dùng cùng fileID này để xác nhận, không đoán mò), m_Type 0→1
  (Sliced, đúng để 9-slice hoạt động), m_Color → trắng (1,1,1,1) để màu gốc của texture hiện đúng, không
  bị tint nâu đè lên.
- 8× `Stud_0..3_Rim` (4 góc × 2 popup): m_Sprite → WoodBoard_Stud (cùng cách xác nhận fileID), m_Color →
  trắng. Giữ m_Type = 0 (Simple) vì rivet không cần 9-slice.

ĐÃ TẮT (không xoá — đúng luật "che/tắt không xoá khi chưa duyệt"), vì các lớp màu phẳng cũ giờ đã được
thay thế hoàn toàn bởi art mới (Frame đã có sẵn nền vân gỗ bên trong, Stud đã có sẵn viền sậm + highlight
trong 1 ảnh — giữ lớp cũ active sẽ đè màu phẳng lên trên art mới, làm hỏng hình):
- `Board_Fill_Bottom` + `Board_Fill_Top` × 2 popup (4 object) — m_IsActive 1→0.
- `Stud_0..3_Base` + `Stud_0..3_Shine` × 2 popup (16 object) — m_IsActive 1→0.
- Đã kiểm tra cả 20 object này đều là node lá (không con), tắt không ảnh hưởng gì khác.

VERIFY: dòng file 585339, YAML doc 6907 — không đổi so với trước (không hỏng scene). Toàn bộ 10 field gán
sprite + 20 field tắt đều đã đọc lại xác nhận đúng giá trị mới.

CÒN LẠI CHO SẾP: mở Unity Editor 1 lần để engine tự re-import 2 file PNG mới (đọc guid từ .meta có sẵn,
sinh thumbnail) — sau đó Play/xem Kho + Shop sẽ thấy khung gỗ mới thay cho khối màu phẳng. Nếu 9-slice bị
kéo méo hoặc tỉ lệ chưa ưng mắt, báo lại để chỉnh border/kích thước.


---

## [2026-08-27, tiếp 3] Nối logic bếp mới (SampleScene) + gỡ minigame + nhún nút + bỏ vạch đỏ

Lệnh Sếp: chuyển hẳn logic cooking cũ sang UI mới, số lượng nguyên liệu phải hiện, click phải ăn,
minigame cũ + UI cũ bỏ, mọi nút/thẻ phải "nhún nhún" khi chạm, khung bo góc có bóng đổ, bỏ vạch đỏ
trên thanh vị. Đội trinh sát 3 agent quét song song (hierarchy scene / luồng logic / tài nguyên anim).

Backup: `production/backup_train_2026-08-27/cooking_rewire/` (SampleScene + 4 file .cs trước khi sửa).

### GỐC RỄ 2 LỖI SẾP BÁO (x0 + click chết) — CHUNG 1 NGUYÊN NHÂN
1. `KitchenTransferManager` (kho hàng đã gửi vào bếp, singleton DontDestroyOnLoad) CHỈ được đặt sẵn
   trong SCN_Farm. Play thẳng SampleScene → `Instance == null` → `KitchenSceneV2UI.RefreshCardQuantities`
   (dòng ~459) return sớm → mọi thẻ kẹt "x0". Và `CookingSelectionManager.TrySelect` (dòng 93-98) chặn
   im lặng mọi click khi quantity <= 0 → "click không được" chính là hệ quả của x0.
2. Phụ: id nông trại `chicken_meat`/`nam` KHÔNG trùng id bếp `chicken`/`mushroom` — UI v2 đối chiếu
   thẳng nên 2 mặt hàng này không bao giờ hiện, và khi nấu xong không trừ được kho (SetAfterCooking
   nhận id bếp, kho lưu id farm).

### ĐÃ SỬA (7 file, KHÔNG đổi bất kỳ id nào)
- MỚI `Scripts/KitchenV2/KitchenTransferBootstrap.cs`: RuntimeInitializeOnLoadMethod — tự tạo
  KitchenTransferManager nếu chưa có (Awake của nó tự nạp save PlayerPrefs). Vào từ Farm thì đã có
  sẵn → bootstrap bỏ qua.
- MỚI `Scripts/KitchenV2/KitchenIdMap.cs`: bảng dịch 2 chiều farmId↔kitchenId, nguồn sự thật DUY NHẤT
  là `CookingBoot.cookingInventoryItems` (đúng bảng luồng cũ dùng — 2 luồng không thể lệch). Id không
  có trong bảng → giữ nguyên (đa số id vốn trùng).
- `KitchenSceneV2UI.cs` — 4 chỗ: (a) RefreshCardQuantities dịch id qua KitchenIdMap + TRỪ số thẻ đang
  chọn (poll 0.15s không còn ghi đè con số TrySelect vừa trừ tạm); (b) SetFlavorRow: vạch đỏ Marker
  luôn SetActive(false) — Sếp bỏ vạch đỏ, fill bar + số cur/target là đủ; (c) chuỗi builder tiêu đề
  "VỊ KHÁCH MUỐN · vạch đỏ là mốc" → "VỊ KHÁCH MUỐN"; (d) BuildTrayCards gắn UIJuiceFeedback (tiếng
  pop nguyên liệu) + UnityEngine.UI.Shadow (bóng nâu mềm offset 0,-4) cho từng thẻ spawn lúc chạy.
- `CookingChallengeManager.cs`: OnClickCookSubmit → StartCoroutine(CookSubmitRoutine()) — NẤU THẲNG,
  bỏ StartRandomMiniGame; CanStartCooking bỏ điều kiện bắt buộc 2 minigame tồn tại. Thành/bại giờ do
  điểm hương vị quyết định (successScoreThreshold) — đúng "nấu ăn bình thường, minigame không cần tới".
- `CookingItemConsumer.cs`: dịch ngược id bếp → id farm (KitchenIdMap.ToFarm) trước khi SetAfterCooking
  → nấu xong TRỪ KHO ĐÚNG cả với gà/nấm.
- `Audio/UIJuiceFeedback.cs`: thêm IPointerEnter/Exit — hover phồng 1.06x (0.08s, unscaled), có cờ
  isPressed để hover không đá nhau với nhún bấm; thêm SetSound() công khai.
- MỚI `Scripts/UIJuiceAutoAttach.cs`: sweeper DontDestroyOnLoad quét mỗi 2s, tự gắn UIJuiceFeedback
  cho MỌI Button active (cả nút Instantiate lúc chạy — slot kho, thẻ shop). Chỉ quét Button, không
  đụng Slider/Scrollbar. Farm 125 nút + bếp 21 nút được phủ tự động, không sửa tay từng object.

### SỬA SCENE SampleScene.unity (45.287 dòng, 1.789 doc — không đổi sau sửa)
- TẮT (m_IsActive 0, KHÔNG xoá): Center_Panel, Left_Ingredients_Panel, Right_Panel, khungtittle,
  Background, Btn_farm — toàn bộ UI cooking CŨ biến mất khỏi màn hình. Canvas gốc + Audio giữ nguyên
  (AudioManager/_CookingFX còn dùng). ChecKSeletion (popup "chưa chọn gì") là con trực tiếp Canvas nên
  vẫn hoạt động.
- Sửa m_text bake của Txt_TasteTitle (GO 1568706926) → "VỊ KHÁCH MUỐN" (đúng kiểu escape Unity).

### VÌ SAO CHƯA XOÁ HẲN UI CŨ + MINIGAME (dù Sếp đã cho phép)
Các manager "não cũ" đang giữ tham chiếu serialize vào các panel cũ (hintsBoxUI, centerCookingPanelUI,
scoreResultBoxUI, 2 minigame UI...). Xoá vật lý TRƯỚC khi Play-test = nếu luồng mới còn sót lỗi thì
không còn đường lui trong cùng phiên. Trình tự an toàn: Sếp Play-test luồng mới (checklist bên dưới)
→ xác nhận OK → đợt sau xoá vật lý các subtree + manager minigame + script minigameCooking/ (đã lên
danh sách đầy đủ fileID từ đợt quét). Đã kiểm tra: ShowScoreResultPopupRoutine chỉ WaitForSeconds,
không chặn luồng khi box điểm nằm trong panel đã tắt; UI v2 tự hiện điểm trên lò ("XONG! Nđ").

### CHECKLIST PLAY-TEST CHO SẾP
1. Farm: gửi vài nguyên liệu vào bếp (popup Kho → gửi bếp), nhớ thử cả GÀ và NẤM.
2. Mở bếp: thẻ phải hiện đúng số lượng (kể cả gà/nấm), click thẻ → chọn được, số giảm 1.
3. Bấm NẤU MÓN → không còn minigame, lò cháy → điểm hiện trên lò → chạm Trình bày → vào kho.
4. Nấu xong quay lại Farm xem kho bếp có TRỪ đúng không (đặc biệt gà/nấm).
5. Thanh vị: không còn vạch đỏ; fill đổi màu khi đủ/vượt mốc.
6. Đưa trỏ/bấm nút bất kỳ (cả Farm lẫn bếp): nút phồng nhẹ khi hover, nhún khi bấm.


---

## [2026-08-27, tiếp 4] Khay bếp: chỉ hiện món đã gửi (Sếp chốt qua hỏi đáp) + xác minh luồng Gửi bếp

Sếp hỏi 2 điều từ Play-test: (1) "nguyên liệu tôi chuyển vào đâu?" (2) "chưa chuyển sao vẫn hiện item?".

XÁC MINH: nút Gửi bếp trong popup Kho (Btn_TransferKitchen, fileID 41404558, SCN_Farm) tồn tại, active,
đã nối OnTransferKitchenClicked → KitchenTransferManager.AddTransferredItem. Kho bếp hiện RỖNG thật —
x0 là số đúng (chưa có lần gửi thành công nào trong save hiện tại). Item vẫn hiện vì khay v2 vốn thiết kế
kiểu DANH MỤC đủ 21 nguyên liệu (x0 = chưa có).

SỬA THEO LỰA CHỌN SẾP (option "Chỉ hiện món đã gửi") — KitchenSceneV2UI.cs:
- RefreshCardQuantities: thẻ raw=0 và không đang chọn → SetActive(false) (GridLayout tự dồn ô);
  thẻ đang chọn luôn hiện kể cả vừa trừ về 0. Đếm thẻ hiện/tab.
- Thêm Txt_EmptyHint mỗi tab (tạo lazy, treo lên gốc Scroll để không bị Grid xếp ô): khay trống hiện
  "về nông trại mở KHO, chọn nguyên liệu rồi bấm GỬI BẾP nhé!".
- BuildTrayCards: bỏ tạo thẻ khoá (chưa mở khoá = chưa thể gửi; BuildLockedCard giữ lại phòng đổi ý).
Kiểm tra ngoặc/paren cân bằng OK. Backup .cs đã có sẵn từ đợt trước cùng ngày (cooking_rewire/).


---

## [2026-08-27, tiếp 5] Vá cổng GỬI BẾP — hạt giống + item id rỗng lọt qua làm "mất hàng vô hình"

Play-test Sếp lộ bug qua console: `Đã chuyển 1x '' `, `1x 'seed_nam'`, `3x ''` — cổng gửi cũ theo
DANH SÁCH CẤM (chỉ chặn công trình/hoa) nên HẠT GIỐNG và entry id rỗng trong save đều gửi được: kho
nông trại bị trừ, còn bếp không nhận diện → hàng "bốc hơi", khay bếp trống là ĐÚNG dữ liệu.

Sửa (backup WarehousePopupUI.cs + SCN_Farm vào cooking_rewire/):
1. IsTransferrableToKitchen → DANH SÁCH CHO PHÉP: chỉ item có InventoryItemData.cookingData mới gửi
   được (đúng điều kiện bếp nấu được). Nút Gửi bếp tự mờ với hạt giống/món ăn/đồ linh tinh.
2. OnTransferKitchenClicked: IsNullOrEmpty → IsNullOrWhiteSpace (chặn cả id ' ').
3. GetItemsForCategory: ẩn entry id rỗng khỏi danh sách Kho + LogWarning (entry hỏng vẫn nằm trong
   save, chỉ không hiển thị/bấm được nữa).
4. SCN_Farm: extraItemDatabase của WarehousePopupUI 36 → 50 entry (thêm 14 asset nấu ăn còn thiếu so
   với CookingBoot.cookingInventoryItems — whitelist đủ 20 món bếp). Line 585339→585353 (+14 đúng số
   dòng thêm), YAML doc 6907 không đổi.

Dữ liệu kẹt lại từ các lần gửi hỏng (CHƯA dọn, chờ Sếp quyết): 1x seed_nam nằm trong save bếp
(vô hình, vô hại); 4x item-id-rỗng đã bị trừ khỏi kho nông trại và mất (không truy được là món gì);
entry id rỗng vẫn trong save kho nông trại (đã ẩn khỏi UI).

Ghi chú console: warning "referenced script missing" (Wagon_1..4, Item_NguyenLieu), "2 event systems",
"2 audio listeners" là vấn đề CÓ TỪ TRƯỚC (prefab gãy script + nạp chồng 2 scene) — Item_NguyenLieu
sẽ hết khi xoá UI cũ đợt dọn; Wagon thuộc hệ tàu (việc riêng); 2 EventSystem/AudioListener xử lý khi
dọn scene bếp.


---

## [2026-08-27, tiếp 6] Sếp báo không gửi được Bắp Cải sau khi vá cổng — 2 nguyên nhân chồng nhau

1. SESSION PLAY CŨ: Sếp đang Play từ TRƯỚC lúc tôi thêm 14 asset vào extraItemDatabase — recompile
   giữa Play giữ nguyên dữ liệu scene CŨ trong RAM (36 entry, THIẾU Item_bapcai vì nó nằm trong nhóm
   14 mới thêm) → whitelist chặn 'bapcai'. Fix: chỉ cần STOP → PLAY lại để scene nạp 50 entry mới.
   (Đã xác minh: cropId bắp cải = 'bapcai', Item_bapcai.itemId = 'bapcai', khớp nhau — restart là chạy.)
2. LỖ HỔNG THẬT tìm thấy khi rà id (Sếp dặn "id rất quan trọng" — đúng): crop nấm thu hoạch ra id
   'nam' nhưng InventoryItemData là Item_Mushroom (itemId 'mushroom') — KHÔNG có asset nào itemId='nam'
   → whitelist sẽ chặn nấm vĩnh viễn nếu không xử lý. KHÔNG đổi id nào (luật); thêm bảng alias đã
   xác minh vào KitchenIdMap: FarmAliases { nam → mushroom } + NormalizeFarmId():
   - IsTransferrableToKitchen: normalize trước khi tra whitelist.
   - OnTransferKitchenClicked: LƯU ID CHUẨN vào kho bếp (AddTransferredItem(Normalize(id))) — bếp
     nhận diện được ngay, không cần dịch lại lúc hiển thị.
   - KitchenIdMap.ToKitchen: normalize alias trước khi tra bảng farm→kitchen.
   Bảng id đầy đủ đã rà từng asset: beef/egg/pork/chili/fishsauce/herbs/lemon/pepper/rice/salt/
   soysauce/cachua/khoaitay/bapcai/sugarcane/ngo/carot/milk trùng id kho; chicken_meat→chicken (đã có
   qua cookingData); nam→mushroom (alias mới).


---

## [2026-08-27, tiếp 7] "Không chuyển được" lần 2 — kiểm tra toàn diện: hệ thống ĐÚNG, item Sếp chọn là HẠT GIỐNG

Screenshot Sếp gửi: item đang chọn là "Hạt Cà Chua x5" (seed_cachua) — chính mô tả in-game ghi
"Không dùng làm nguyên liệu nấu ăn" (WarehousePopupUI.cs:734, nhánh mô tả hạt giống có sẵn từ trước)
→ nút mờ là ĐÚNG. Lưu ý UX: hạt giống hiển thị bằng ICON của nông sản (hạt cà chua mang icon quả
cà chua) nên rất dễ chọn nhầm — đây là lý do Sếp tưởng gửi bếp hỏng.

KIỂM TRA TOÀN DIỆN chuỗi id (tĩnh, đọc asset thật):
- SCN_Farm trên đĩa vẫn nguyên 50 entry extraItemDatabase + 585353 dòng (không bị editor ghi đè).
- 21 thẻ bếp (allIngredients của Kitchen_UI_v2) có id: bapcai/beef/cachua/carot/chicken/chili/egg/
  fishsauce/herbs/khoaitay/lemon/milk/mushroom/ngo/pepper/pork/rice/salt/soysauce/sugar/sugarcane.
- 20/20 cookingData id của InventoryItemData ĐỀU có thẻ bếp khớp — không lỗ hổng nào còn lại.
- Chuỗi bắp cải thật: kho 'bapcai' → whitelist (Item_bapcai, có cookingData) → store 'bapcai' →
  ToKitchen → thẻ 'bapcai'. THÔNG SUỐT (miễn là Play phiên MỚI sau khi scene nạp 50 entry).

SỬA THÊM (UX tự giải thích): RefreshDetailPanel — khi item không gửi được, nút đổi chữ thành
"KHÔNG PHẢI ĐỒ NẤU" (thay vì chỉ mờ đi); log 1 dòng mỗi lần chọn item: id + được/không.


---

## [2026-08-27, tiếp 8] Truy ra gốc: Editor giữ scene CŨ trong RAM — whitelist chuyển vào CODE

Log mới gắn phát huy ngay: `Chọn 'bapcai' → gửi bếp: KHÔNG` — id ĐÚNG (bắp cải thật) nhưng vẫn bị
chặn → chứng minh extraItemDatabase trong RAM Editor vẫn là bản 36 entry cũ: Unity KHÔNG tự nạp lại
SCN_Farm từ đĩa (file bị sửa ngoài trong lúc Editor đang mở scene). Restart Play là chưa đủ — phải
đóng/mở lại scene hoặc restart Unity thì 14 entry mới mới vào RAM.

FIX DỨT ĐIỂM (không phụ thuộc scene nữa): WarehousePopupUI thêm bảng tĩnh CookableIdsVerified
(20 id đã xác minh từng asset ngày 2026-08-27) làm FALLBACK — gate cho qua nếu (a) item có
cookingData trong database scene HOẶC (b) id nằm trong bảng xác minh. Luật Sếp chốt được mã hoá
đúng: nông sản ruộng + chăn nuôi (trứng/thịt/sữa/cá) + nguyên liệu mua chợ (nước mắm...) = ĐƯỢC;
hạt giống/vật liệu/công trình/món thành phẩm = KHÔNG. Ghi chú cá: dự án hiện KHÔNG còn item cá
(id 'ca' đã xoá từ trước, nằm trong DeadItemIds) — khi nào thêm lại cá chỉ cần gán cookingData là
nhánh (a) tự nhận, không cần sửa bảng.

CẢNH BÁO QUAN TRỌNG GHI LẠI CHO SẾP: Editor đang giữ SCN_Farm bản RAM cũ. Nếu Sếp Ctrl+S đè scene
từ trạng thái này, các sửa trên đĩa hôm nay (nới Chợ, icon Settings, khung gỗ Kho/Shop, 14 entry
database) sẽ bị GHI ĐÈ MẤT. Khuyến nghị: đóng scene/mở lại (hoặc restart Unity) TRƯỚC khi save gì.
Backup đầy đủ vẫn nằm ở production/backup_train_2026-08-27/ nếu lỡ tay.


---

## [2026-08-27, tiếp 9] TÌM RA BUG GỐC của mọi vụ "chuyển Nx ''" — reentrancy trong OnTransferKitchenClicked

Log phiên test Sếp cho mẫu quyết định: chuyển 1 cái → `Đã chuyển 1x 'rice'` OK (và Rice x11 HIỆN
trong khay bếp — pipeline mới chạy đúng!); bấm MAX → `Đã chuyển 11x ''` — id rỗng.

GỐC RỄ (bug CÓ TỪ TRƯỚC, không phải do các sửa đổi hôm nay): FarmInventoryManager.RemoveItem bắn
sự kiện kho-đổi → WarehousePopupUI.RefreshUI chạy NGAY GIỮA OnTransferKitchenClicked (stack log:
RefreshDetailPanel ← RefreshUI ← FarmInventoryManager.RemoveItem:205 ← OnTransferKitchenClicked:530).
Khi chuyển HẾT SẠCH một món (MAX), món biến khỏi danh sách → selectedItemId bị reset null TRƯỚC khi
AddTransferredItem đọc nó → AddTransferredItem(null) bỏ qua lặng lẽ → kho nông trại đã trừ, bếp không
nhận → "hàng bốc hơi". Gửi 1 cái không sao vì món còn dư, selection giữ nguyên.

ĐÍNH CHÍNH các chẩn đoán trước: các vụ `1x ''`/`3x ''` hôm trước KHÔNG phải "entry id rỗng trong
save kho" như tôi đoán — tất cả đều là bug reentrancy này khi người chơi chuyển hết sạch một món.
(Code ẩn entry id rỗng vẫn giữ — vô hại.)

FIX: chốt rawId + kitchenId (NormalizeFarmId) vào biến CỤC BỘ trước khi gọi RemoveItem; từ đó về sau
chỉ dùng biến cục bộ. Log gửi giờ in cả id kho lẫn id bếp.

ĐỀN BÙ: tạo editor tool 1-lần `Assets/_Game/Editor/DenBuNguyenLieuMat_2026_08_27.cs` — menu
Tools/Farm/"Đền bù nguyên liệu mất (27-08)": cộng thẳng vào PlayerPrefs KITCHEN_TRANSFER_SAVE đúng
format TransferSaveData (6 cachua, 5 bapcai, 9 rice, 11 ngo, 21 egg, 1 beef, 10 pork — đúng số bốc
hơi theo log). Có khoá DEN_BU_2026_08_27_DA_CHAY chống chạy 2 lần; yêu cầu chạy khi KHÔNG Play.
(4 đơn vị '' của phiên hôm trước không truy được là món gì — không đền được, đã báo Sếp.)


---

## [2026-08-27, tiếp 10] Xác nhận chặng VỀ của vòng lặp: món nấu xong → kho nông trại

Sếp hỏi vòng lặp farm → bếp → nấu → món bay vào kho (UI) → về farm thấy trong KHO đã trơn tru chưa.
KIỂM TRA CODE chặng về: CollectCookedDishToWarehouse (chạm bàn Trình bày) →
FarmInventoryManager.AddItem(dishId, 1) — cộng THẲNG vào kho nông trại (DontDestroyOnLoad, sống
xuyên scene). Kho đầy thì món GIỮ NGUYÊN trên dĩa + toast báo (fix TESTER-F8 có sẵn — không mất đồ).
KHO popup ở farm phân loại IsCookedDish → tab Món ăn → thấy ngay. Ô "VÀO KHO / Đã gửi N món" trong
bếp đúng là UI đếm (PlayerPrefs) như Sếp mô tả — không click được, chỉ hiển thị.

Sửa nhỏ thêm: null-guard FarmInventoryManager.Instance trong CollectCookedDishToWarehouse (Play
thẳng scene bếp không có kho farm → trước đây sẽ NullReferenceException gãy luồng; giờ giữ món trên
dĩa + LogWarning).

Ghi chú: nhân vật delivery bay món vào kho (deliveryCharacterMover) đang trỏ vào object 'delivery'
bị TẮT sẵn trong scene từ trước — animation bay không hiện nhưng logic cộng kho không ảnh hưởng
(code đã null-safe). Nếu Sếp muốn hiệu ứng "món bay vào kho" thì làm ở đợt polish sau trên UI v2.

TRẠNG THÁI VÒNG LẶP ĐẦY ĐỦ (sau các fix hôm nay): farm trồng/nuôi/mua → KHO → GỬI BẾP (whitelist
20 nguyên liệu, MAX an toàn) → bếp hiện thẻ đúng số → chọn → NẤU (không minigame) → điểm quyết định
→ Trình bày → vào kho nông trại → về farm mở KHO tab Món ăn thấy món. Món ăn KHÔNG gửi ngược lại
bếp được (đúng luật). Chưa có chiều "rút nguyên liệu từ bếp về farm" — Sếp cần thì báo, làm thêm nút.


---

## [2026-08-27, tiếp 11] HỆ 5 STAGE CÂY TRỒNG + LƯỚI ISO RẢI CÂY (phân tích video Township)

Sếp giao: phân tích video farm tham chiếu, tách nền 2 folder art mới (Hatgiong + Hoa), nâng 3 → 5 stage,
rải cây khít đều trên plot theo góc iso, sorting đúng.

### A. PHÂN TÍCH VIDEO (Township-style) — vì sao họ đặt cây "đẹp và đều"
Trích 13 frame + zoom 1 plot: cây KHÔNG rải ngẫu nhiên mà nằm trên LƯỚI ĐỀU theo 2 trục iso của ô đất
(không phải lưới thẳng đứng của màn hình). Hàng cây song song 2 cạnh hình thoi, mọi cây cùng cỡ, khoảng
cách bằng nhau; cây hàng sau vẽ TRƯỚC (che một phần bởi hàng trước) → cảm giác ruộng dày. Cây nhỏ (cà rốt/
cà chua/ngô) dùng NHIỀU cây nhỏ; cây to (bí đỏ) dùng ÍT cây nhưng to, khối phủ gần kín ô.
=> Công thức đã cài: hình thoi = ảnh của hình vuông (s,t)∈[-0.5,0.5]² qua
   P(s,t) = tâm + (s+t)·E_phải + (s−t)·E_trên   (E = vector tâm→đỉnh phải / đỉnh trên)
Lưới đều trong (s,t) ⇒ lưới đều theo 2 trục iso. Lấy 4 đỉnh từ AABB của sprite nền ⇒ tự đúng với mọi
kích cỡ plot, không hardcode toạ độ.

### B. TÁCH NỀN + CẮT STAGE (23 sheet → 115 sprite)
Script `~/farmart/cut.py`: nền magenta chroma-key. KHÔNG dùng key màu toàn ảnh (sẽ ăn mất hoa hồng/tím/
cẩm tú cầu/phong lan) mà FLOOD từ viền ảnh: chỉ vùng nền NỐI LIỀN với viền bị xoá, hoa màu gần magenta
nằm trong đường viền đen được giữ nguyên. Alpha feather + despill (F = (C − (1−a)·B)/a) nên cạnh sạch,
không quầng tím. Cắt stage bằng column-projection trên alpha → mỗi band = 1 stage, KHÔNG dính frame kế
bên (đúng yêu cầu Sếp); tự gộp/tách nếu số band ≠ 5. Cả 23 sheet đều ra đúng 5 band.
Kiểm tra bằng mắt: `qa-reports/REF_cut_QA_1.jpg`, `REF_cut_QA_2.jpg` (nền ô caro = vùng trong suốt).

Nhận dạng 23 sheet (tên file ChatGPT vô nghĩa → map theo hình):
nam, sugarcane, carot, rice, bapcai, ngo, cachua, watermelon, pumpkin, pepper, lemon, chili, khoaitay
+ 10 hoa: hoa_anh_thao, hoa_cuc_trang, hoa_cuc_van_tho, hoa_cam_tu_cau, huong_duong, hoa_mau_don,
hoa_oai_huong, hoa_lan, tulip, hoa_hong. Khớp 21/21 cropId đang có trong dự án (KHÔNG đổi id nào).

### C. XUẤT VÀO PROJECT
`Assets/Assetsgame/hatgiong/Stage5/<cropId>/<cropId>_s1..s5.png` (+ .meta), 115 file, 6.4 MB.
- Resize theo TRUNG VỊ từng nhóm (normal ×0.425 → s5 ≈256px, hoa ×0.637 → 256px, cây to ×0.738 → 320px)
  ⇒ vừa nhẹ texture, vừa GIỮ ĐÚNG tỉ lệ cao/thấp giữa các cây như đội vẽ vẽ (ngô cao hơn khoai tây...).
- .meta clone từ meta chuẩn của dự án, sửa: spriteMode 1 (Single), alignment 7 + pivot (0.5, 0)
  = Bottom-Center (gốc cây nằm đúng điểm trồng → sorting theo độ sâu mới đúng), PPU 100 (đúng quy ước
  dự án), filterMode 1 (Bilinear — sprite bị thu nhỏ ~6-12 lần nên Point sẽ rỗ). GUID = md5 theo tên,
  đã kiểm tra không trùng nhau.

### D. CODE (backup: production/backup_train_2026-08-27/crop5stage/)
1. `CropData.cs`: thêm `stageSprites[]` + `stageScales[]`; `HasStageSet`, `StageCount` (có bộ mới → 5,
   chưa có → 3), `StageFromProgress(p)` = chia đều n mốc, `GetScale(stage)`. GetSprite/GetStageSprite
   route qua bộ mới khi có. 3 field cũ (sprout/growing/readySprite + 3 scale) GIỮ NGUYÊN → cây/hoa nào
   chưa gán bộ mới chạy y như trước; PlotController/WarehousePopupUI/TutorialTool vẫn đọc readySprite
   làm icon fallback bình thường (đã quét toàn bộ .cs: 0 chỗ vỡ).
2. `PlotCropVisual.cs`:
   - stage: `crop.StageFromProgress(progress)` thay cho `progress>=1?2:(progress<0.5?0:1)`; isReady =
     stage cuối. Save KHÔNG cần migrate (save chỉ lưu thời điểm trồng, không lưu stage index).
   - `ApplyLattice(n)`: rải n CropPoint theo công thức iso ở mục A + **sorting theo độ sâu**
     (`sortingOrder = base + hạng theo y giảm dần`). TRƯỚC ĐÂY cả 12 cây dùng CHUNG sortingOrder 560 →
     không có thứ tự trước/sau, cây chồng nhau lộn xộn. Chỉ tính lại khi số cây đổi (không tốn mỗi frame).
   - `latticeInset` 0.86 (thụt vào mép ô) + `latticeDepthBias` -0.12 (đẩy khối cây xuống phủ giữa ô) —
     2 số này chỉnh trong Inspector là đổi được bố cục, không cần sửa code.
   - offsetY = 0 cho bộ mới (pivot Bottom-Center), cây cũ giữ offset như trước.
   - HIỆU NĂNG: `EnsureSetup()` trước đây gọi `AutoFindPoints()` (GetComponentsInChildren + cấp phát
     List) MỖI FRAME cho MỖI plot — nay chỉ quét khi chưa có điểm. Sếp đang thấy FPS 8-21, đây là 1
     nguồn rác GC rõ ràng đã dọn.
3. Dữ liệu: 21/21 CropData đã gán 5 stageSprites + stageScales + displayCount
   (cây thường & hoa = 12 cây/ô, nhóm TO = 6 cây/ô: bapcai, nam, pumpkin, watermelon).
   Scale dùng chung theo nhóm: normal 68.36 · hoa 64.45 · to 78.13 (⇒ stage5 cao ~140-250 đơn vị trên ô
   724×345 — tương đương cây cũ 222 nhưng dày hơn nhiều vì 12 cây).

### E. XEM TRƯỚC (mô phỏng ĐÚNG công thức trong code, không phải vẽ tay)
`qa-reports/REF_lattice_A2.jpg` (carot/ngo/rice/cachua/nam/pumpkin), `REF_lattice_B2.jpg`
(hoa_hong/tulip/bapcai/watermelon/sugarcane/lemon), `REF_lattice_growth2.jpg` (cà rốt stage 1→5).
Kết quả khớp đúng phong cách video tham chiếu: hạt rải đều trên đất → mầm → lá → gần chín → chín kín ô.

### F. CÒN LẠI / CẦN SẾP DUYỆT
- **pumpkin + watermelon**: art 5 stage ĐÃ cắt & xuất sẵn, nhưng dự án CHƯA có CropData/giá/cấp mở/thời
  gian lớn/hạt giống cho 2 cây này (cũng chưa có trong bảng giá chợ). Tạo cây mới = thêm id + thêm mục
  kinh tế ⇒ chờ Sếp chốt (giá hạt, giá bán, cấp mở, thời gian lớn, sản lượng) rồi em tạo, không tự ý.
- 21 sprite cũ (…lever1/2/3) vẫn nằm nguyên trong `Assetsgame/hatgiong/` — chưa xoá, để lùi được nếu
  cần. Sau khi Sếp Play-test OK có thể dọn.
- Nếu vào Editor thấy cây hơi tràn mép hoặc hở đất: chỉnh `latticeInset` (nhỏ hơn = thụt vào) và
  `latticeDepthBias` trên component PlotCropVisual của CropGroup — không cần sửa code.


---

## [2026-08-27, tiếp 12] Thêm BÍ ĐỎ + DƯA HẤU · xếp lại thứ tự hạt giống · tách hạt/hoa trong Shop

### A. 2 CÂY MỚI (số liệu tự suy theo ĐÚNG đường cong kinh tế của dự án)
Đường cong sẵn có: lãi/giây = (4×sellGold − goldPrice)/growSeconds tăng đều 0.160 (cấp 1) → 0.304
(Tiêu, cấp 10) và KHÔNG BAO GIỜ TỤT. 2 cây mới tiếp đúng đoạn cuối:

| asset | cropId | seedItemId | cấp | lớn | hạt(gold) | bán | exp | thu | lãi/giây |
|---|---|---|---|---|---|---|---|---|---|
| BiDo.asset   | pumpkin    | seed_pumpkin    | 11 | 620s | 145 | 84 | 62 | 4 | 0.308 |
| DuaHau.asset | watermelon | seed_watermelon | 12 | 700s | 158 | 95 | 70 | 4 | 0.317 |

Các tỉ lệ khác cũng khớp quy ước cũ: exp ≈ 0.73×sellGold · giá hạt/giá bán giảm dần (1.73 / 1.66 tiếp
sau Tiêu 1.76) · harvestAmount 4 · plantCost 1 · displayCount 6 (nhóm "quả to", 6 cây/ô như bắp cải/nấm).
stageSprites/stageScales = bộ 5 stage vừa cắt, scale 78.125. itemIcon/harvestIcon = stage 5,
plantSeedFxIcon = stage 1 (đúng convention: itemIcon của Cà Rốt cũng chính là sprite stage chín).
Cấp 11-12 hợp lệ: trần cấp = 100 (`PlayerProgressManager.CapToiDa`).

### B. ĐÃ NỐI VÀO 6 DANH SÁCH TRONG SCENE (SCN_Farm, +12 dòng, YAML doc 6907 không đổi)
ShopManager.seedList 21→23 · MarketManager.cropDatabase 21→23 · FarmManager.cropDatabase 11→13 ·
WarehousePopupUI.cropDatabase 21→23 · StallItemCatalog.cropDatabase 21→23 ·
SeedPopupController.cropDataList (Popup_seed) 11→13. Mỗi guid xuất hiện đúng 6 lần trong scene ✓.
(flowerCropDatabase KHÔNG thêm — 2 cây này là rau củ, không phải hoa.)

### C. CHỢ
- `MarketPriceTable.cs`: +2 dòng NongSan (pumpkin 84/lv11/w45 · watermelon 95/lv12/w45) và +2 dòng
  HatGiong (seed_pumpkin 80 · seed_watermelon 87 — đúng tỉ lệ 55% giá Shop như 21 hạt cũ).
- `MarketDatabase.asset`: đã CHÈN THẲNG 4 dòng (74→78) để chợ bán được NGAY, không phải chờ chạy tool.
  Vẫn nên chạy `Tools/Farm/Chợ/2` một lần cho đồng bộ ghi chú tự sinh; chạy lại cũng ra đúng 78 dòng
  vì bảng giá đã có 4 dòng mới.

### D. THỨ TỰ SHOP — "hạt giống ra hạt giống, hoa ra hoa"
`ShopManager.RenderItems` trước đây sắp DUY NHẤT theo `OrderBy(GetUnlockLevel)` ⇒ hạt rau và hạt hoa
XEN KẼ nhau (hướng dương cấp 1 nằm giữa lúa và bắp cải...). Nay sắp 4 tầng:
`GroupRank` (rau củ 0 → hoa 1 → công trình/trang trí 2) → `unlockLevel` → `RarityRank` (growSeconds:
cùng cấp thì cây lâu chín/hiếm hơn xếp sau) → tên. Kết quả tab HẠT GIỐNG:
13 hạt rau (rice → bapcai → ngo → carot → cachua → khoaitay → nam → sugarcane → lemon → chili →
pepper → **pumpkin** → **watermelon**), rồi 10 hạt hoa (huong_duong → ... → hoa_anh_thao).

### E. VỀ "XẾP LẠI THEO ĐỘ HIẾM MÓN ĂN" — PHÂN TÍCH + VÌ SAO CHƯA ĐỔI unlockLevel
Đã map toàn bộ 18 DishData → nguyên liệu → cây. Thước đo ĐÚNG cho "khi nào cần hạt này" là
món SỚM NHẤT/DỄ NHẤT mà cây phục vụ (không phải món khó nhất):

| cây | món sớm nhất cần nó | "nên mở cấp" | đang ở cấp | lệch |
|---|---|---|---|---|
| carot | Bò hầm cà rốt (cấp 8, KHÓ) — DUY NHẤT 1 món | ~8 | 3 | sớm 5 cấp |
| pepper | Gà nướng lu (cấp 7, VỪA) | ~7 | 10 | muộn 3 cấp |
| rice/bapcai/ngo/cachua | cấp 5-6 (dễ/vừa) | 5-6 | 1-3 | sớm (nhưng còn để BÁN nên hợp lý) |
| khoaitay, nam, sugarcane, lemon, chili | khớp | — | — | ✓ |

CHƯA tự đổi `unlockLevel` vì 2 lý do:
1. **Đổi cấp là đổi cả chuỗi**: giá hạt/giá bán/thời gian lớn/exp đều là hàm của cấp (đường cong lãi/
   giây), nên phải đổi kèm ở `MarketPriceTable` (2 chỗ/cây), `BasePriceBook`, và 10 asset
   `LevelReward_L*.asset` (trong đó L3 có ghi CHỮ "Cà chua và Cà rốt", L10 tặng seed_pepper) +
   `TutorialManager` (mảng HAT_O_DAT). Sai một mắt là lệch kinh tế mà không có lỗi biên dịch nào báo.
2. **Kết quả có thể phản trực giác**: theo đúng luật thì CÀ RỐT thành cây endgame (cấp ~8) còn TIÊU
   thành cây cấp 3 — ngược cảm nhận thông thường (tiêu là gia vị cao cấp, cà rốt là cây rẻ/sớm).
Đề xuất thay thế (rẻ hơn, không phá kinh tế): thêm 1 món DỄ dùng cà rốt (vd "Salad cà rốt" cấp 4) —
lúc đó cà rốt cấp 3 tự hợp lý; và chỉ hạ TIÊU 10 → 7 (một mình, ít cascade). Chờ Sếp chốt.

### F. SHOP ĐANG DÙNG UI MỚI — XÁC NHẬN
`Canvas_Popup/popup_Menu` (ShopManager) CHÍNH LÀ "CỬA HÀNG" và ĐANG là UI gỗ mới (Board_Border,
Board_Grain_1..6, Stud_*, Header_Banner, Tabs_Row 3 tab Seed/Building/Decor, ShopItem_Template) —
cùng bộ khung gỗ với WarehousePopup, và đã được gán texture gỗ mới sáng nay. UI shop CŨ duy nhất là
`Scripts/Mission/ShopSkin.cs` đã chết hẳn (OnEnable/Update return ngay, và `ShopNewUIBuilder` xoá nó).
Lưu ý: `Popup_seed`/`Popup_hoa` KHÔNG phải shop cũ — là popup chọn cây khi trồng, vẫn dùng.
CÒN LẠI: bảng tin CHỢ (`Popup_Board`) chưa lên da gỗ; hierarchy của nó do
`Farm/Editor/MarketBoardUIBuilder.cs` sinh nên skin tay sẽ bị mất nếu chạy lại tool — muốn skin thì
phải sửa trong builder. Chưa làm, chờ Sếp.

### G. TAB "HOA" RIÊNG — vì sao chưa làm
`contentParent` của shop dùng **GridLayoutGroup** nên KHÔNG chèn được dòng tiêu đề nhóm (nó sẽ chiếm
1 ô như một thẻ hàng). Muốn tách hẳn thì phải thêm tab thứ 4, mà `Tabs_Row` do
`ShopNewUIBuilder.cs` sinh ra ⇒ phải sửa builder rồi chạy lại tool (và bổ sung việc gán texture gỗ vào
builder, nếu không chạy lại tool sẽ mất da gỗ vừa gán). Hiện tại đã tách bằng THỨ TỰ (13 hạt rau liền
nhau rồi 10 hạt hoa liền nhau) — nếu Sếp muốn hẳn 1 tab HOA thì báo, em làm trong builder một lượt.

### H. CHECKLIST PLAY-TEST
1. Mở CỬA HÀNG → tab Hạt giống: 13 hạt rau xếp trước (kết thúc bằng Bí Đỏ, Dưa Hấu), sau đó 10 hạt hoa.
2. Bí Đỏ/Dưa Hấu hiện icon + ổ khoá "Mở ở cấp 11/12" (đúng, vì save hiện ~cấp 30 thì mua được luôn).
3. Trồng thử: popup chọn hạt phải có 13 mục; trồng ra 6 cây/ô, đủ 5 stage.
4. Chợ (Bảng tin chợ): thỉnh thoảng có "Bí Đỏ"/"Hạt Bí Đỏ" trong tab Nông sản/Hạt giống (weight 45,
   trần cấp = cấp người chơi + 2).
5. Thu hoạch → vào KHO tab Nông sản, có icon. Gửi bếp sẽ báo "KHÔNG PHẢI ĐỒ NẤU" — ĐÚNG, vì chưa có
   món nào dùng bí đỏ/dưa hấu (chưa tạo InventoryItemData/IngredientData cho 2 cây này).


---

## [2026-08-27, tiếp 13] Shop: HOA phải nằm SAU toàn bộ hạt rau (cùng 1 tab) — làm rõ + tách hàng

Sếp phản hồi: hoa vẫn ở cùng tab hạt giống, nhưng thứ tự phải là hết hạt rau rồi mới tới hoa; đang
thấy lộn xộn.

ĐÃ KIỂM TRA LẠI (không phải đoán):
- Code sắp thứ tự ĐÃ nằm trên đĩa (`ShopManager.cs:331-334` OrderBy GroupRank → unlockLevel →
  RarityRank → NameKey) và `GroupRank` trả 0 cho rau / 1 cho hoa. 21/21 asset có `cropCategory` đúng
  (11 rau = 0, 10 hoa = 1) nên phân nhóm chắc chắn chạy.
- GridLayoutGroup của `Content`: StartCorner 0 (trên-trái), StartAxis 0 (ngang), FixedColumnCount = 4
  ⇒ thứ tự nhìn thấy ĐÚNG BẰNG thứ tự phần tử, không có chuyện grid fill theo cột làm lộn.
⇒ Kết luận: bản Sếp đang chạy là bản TRƯỚC khi compile lại (Editor giữ DLL cũ). Cần Stop → chờ compile
→ Play lại mới thấy.

BỔ SUNG cho dễ thấy nhóm (không sửa scene): khi chuyển từ nhóm rau sang nhóm hoa, `RenderItems` chèn
ô RỖNG cho hết hàng đang dở (`PadRowWithSpacers`, số cột đọc từ chính GridLayoutGroup) ⇒ nhóm HOA luôn
bắt đầu ở HÀNG MỚI. Bố cục 4 cột sau khi sửa:
  hàng 1: lúa · bắp cải · ngô · cà rốt
  hàng 2: cà chua · khoai tây · nấm · mía
  hàng 3: chanh · ớt · tiêu · BÍ ĐỎ
  hàng 4: DƯA HẤU · [trống] · [trống] · [trống]
  hàng 5+: hướng dương · hoa hồng · oải hương · lan · cúc trắng · tulip · vạn thọ · mẫu đơn ·
           cẩm tú cầu · anh thảo
KHÔNG dùng dòng tiêu đề chữ vì ô grid cao 335px — một ô chỉ để chữ sẽ trông như lỗi layout.
