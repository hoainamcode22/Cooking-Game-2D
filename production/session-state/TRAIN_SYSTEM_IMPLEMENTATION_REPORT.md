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
