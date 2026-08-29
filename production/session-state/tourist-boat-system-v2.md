# GDD — Tourist Boat System V2: Khách Du Lịch Lên Bờ (BOAT-002)

> **Story:** BOAT-002 · **Kế thừa:** BOAT-001 (Approved 2026-08-13, QA 94/94) · **Trạng thái:** Sếp ra lệnh trực tiếp 2026-08-29 (chat), các con số đã chốt qua Q&A
> **Thay đổi lớn:** vòng đời tàu chuyển từ CHU KỲ THỜI GIAN CỐ ĐỊNH (đậu 40p) sang HƯỚNG SỰ KIỆN (tàu chờ khách được phục vụ xong mới rời bến).
> **Backup trước khi sửa:** `production/backup_boat_2026-08-29/`

---

## 1. Overview

Tàu du lịch cập bến (sát bờ hơn V1) → bắc **tấm gỗ (gangplank)** nối vào bờ → **3–6 khách du lịch** (random từ 11 nhân vật NVGAME) lần lượt xuống tàu, đi theo đường đất tới **xếp hàng trước nhà hàng cooking** → khách đứng đầu mở **bubble món ăn** (random trong các món đã mở theo level, database DishData 38 món) → người chơi nấu món trong cooking, có món trong kho thì tap khách để giao → khách trả **vàng = giá nguyên liệu × 2** + **EXP của món**, bubble biến thành **mặt cười bay lên HUD** (nhỏ → to dần, fade) → khách quay về tàu → khi TẤT CẢ khách đã lên tàu, tàu rời bến → **5 phút sau** tàu kế cập bến (1 bến) / **10 phút so le** (nhiều bến). Popup thông báo "Tàu số 0X sẽ cập bến sau X phút" với khung card bo góc + nút "Đã rõ". Khách chờ tối đa **30 phút**, hết giờ buồn bã về tàu không trả tiền.

## 2. Player Fantasy

"Nhà hàng của mình nổi tiếng tới mức khách đi tàu tới ăn!" — vòng lặp nấu-phục-vụ-nhận-thưởng nhìn thấy được bằng mắt: khách thật đi lại, xếp hàng, cười, trả tiền. Trụ cột: JUICY (mặt cười bay, coin fly, gangplank bắc xuống), PHẢN HỒI NGẮN (popup báo trước, countdown), DỄ THÂN THIỆN (không phạt nặng — khách hết kiên nhẫn chỉ buồn bã rời đi).

## 3. Detailed Rules

### 3.1 Vòng đời chuyến tàu (mỗi bến 1 tàu, event-driven)

```
WaitingNext(gap phút, UTC anchor) → Arriving(travel theo path) → Docked:
  Unloading(gangplank hạ, khách xuống lần lượt 0.8s/khách)
  Serving(khách xếp hàng, bubble mở dần; đồng hồ kiên nhẫn 30p/khách chạy theo UTC)
  Boarding(khách được phục vụ/hết kiên nhẫn đi về tàu)
→ khi khách cuối lên tàu: gangplank rút, Departing(lùi theo path) → WaitingNext(...)
```

- **KHÔNG còn dockMinutes 40p cố định.** Tàu đậu tới khi khách cuối lên tàu (cận trên thực tế = 30p kiên nhẫn + thời gian đi bộ).
- Số hiệu tàu = số bến: Dock 1 → "Tàu số 01", Dock 2 → "02", Dock 3 → "03".
- Bến chưa mở slot → tàu của bến đó không chạy (giữ nguyên luật unlock V1: bến 1 free L10 · bến 2 2.000 vàng L12 · bến 3 25 gem L14).

### 3.2 Lịch tàu kế tiếp (chốt với Sếp 2026-08-29)

- `gapOneDock = 5 phút` — chỉ 1 bến mở: tàu 01 rời bến, đúng 5p sau tàu 01 cập bến lại.
- `gapMultiDock = 10 phút` — ≥2 bến mở: mỗi bến, tàu kế của bến đó = departure của bến đó + 10p; manager kiểm tra 2 arrival bất kỳ cách nhau ≥ `minStaggerMinutes` (mặc định 3p), vi phạm thì dời arrival muộn hơn.
- Chưa mở slot 2 thì tàu 01 vẫn quay lại đều đặn (per-dock độc lập nên tự đúng).
- Anchor lưu UTC (PlayerPrefs), chống chỉnh đồng hồ lùi như V1.

### 3.3 Khách du lịch

- Số khách/chuyến: **random 3–6** (config `visitorsMin/visitorsMax`).
- Nhân vật: random KHÔNG lặp trong 1 chuyến từ roster 11 prefab NVGAME (chuyến sau random lại).
- Di chuyển: đi bộ theo waypoint path riêng từng bến (`TouristPath_Dock0X`, node đặt dọc đường đất Sếp đã vẽ — tool sinh node mặc định, Sếp kéo chỉnh 1 lần trong scene, đánh dấu REVIEW).
- Hàng chờ: `TouristQueueAnchor` trước nhà hàng cooking + `queueSpacing`; khách tới slot trống nhỏ nhất, khi khách trước rời đi cả hàng tiến lên 1 slot.
- Bubble: chỉ khách ĐỨNG ĐẦU hàng mở bubble (scale-in mượt), hiện sprite món. Món = random từ DishData có `unlockLevel ≤ level hiện tại` (database 38 món thực tế trong `Assets/_Game/Farm/data/Farm_Cooking/` — bảng BANG_MON_AN_30 là doc cũ, số thật là 38, lọc theo level nên đầu game chỉ ra món đã mở). Khách trong 1 chuyến không trùng món nếu đủ món để chọn.
- **Giao món:** tap khách/bubble → nếu `FarmInventoryManager.HasItem(dishId)` → `RemoveItem` → thưởng + hiệu ứng; nếu chưa có món → hint "Chưa có <tên món> trong kho — vào bếp nấu nhé!". (Nấu xong bấm "đưa vào kho" như luồng cooking hiện tại — không đổi luồng bếp.)
- **Kiên nhẫn:** 30 phút/khách tính bằng UTC tuyệt đối từ lúc bubble mở (offline vẫn chạy). Hết giờ: bubble chuyển icon buồn 2s → khách về tàu, không thưởng.

### 3.4 Thưởng (công thức Sếp chốt)

```
goldReward(dish) = round( Σ FarmItemValue(nguyên liệu chính của món) × 2 )
expReward(dish)  = dish.rewardExp   (data có sẵn trong DishData)
```
- `FarmItemValue` = giá bán farm của nguyên liệu (nguồn giá đang dùng để tính "310 vàng nguyên liệu" trong CookingChallengeManager — Dev tra đúng util đó, KHÔNG bịa bảng giá mới).
- Fallback an toàn: nguyên liệu nào không tra được giá → dùng `dish.sellPrice` thay cho cả món (log warning để tuning sau).
- Trả thưởng qua `FarmEconomyManager.AddGold` (tự bắn `OnGoldAddedFx` → CoinFlyFX có sẵn) + `PlayerProgressManager.AddExp`.
- Bắn mission event loại phù hợp trong `MissionEventType` hiện có (Dev kiểm tra enum, có loại deliver/cook thì dùng, KHÔNG thêm enum mới nếu không cần).

### 3.5 Popup "Tàu sắp cập bến"

- Trigger: đúng lúc chuyến kế được lên lịch (tàu trước rời bến / vừa mở bến / vào game thấy chuyến kế trong tương lai ≥1p) — mỗi chuyến chỉ báo 1 lần (persist key theo arrivalUtc).
- Nội dung: khung card bo góc gỗ (tái dùng `khunggo/WoodBoard_Frame` trong source) + dim nền đen 60% + text:
  **"Tàu số 01 sẽ cập bến sau 5 phút!"** / dòng phụ: *"Chuẩn bị nguyên liệu, nấu món ngon tiếp đãi khách nhé!"*
  + nút **"Đã rõ"** đóng popup. Text hiện kiểu typewriter nhẹ hoặc pop-scale (Dev C chọn, đồng bộ popup khác của game).
- Tôn trọng `FarmInputLock` và không đè tutorial.

### 3.6 Mua slot bến (rework UI V1)

- Tap bảng khóa bến (asset thay cho màu nền — art request) → mở **tab popup mua slot**: khung card bo góc, icon vàng/gem + số giá (TMP màu vàng), nút MUA.
- Đủ level + đủ tiền → trừ tiền, đóng popup, **hiệu ứng mở slot**: bảng khóa vỡ/fade, tàu xuất phát ngay, confetti/scale-pop nhẹ tại bến, SFX mua có sẵn.
- Thiếu level/tiền → nút disable + dòng lý do (giữ luật V1: API SpendGold/SpendGems tự từ chối).

### 3.7 Tàu sát bờ + gangplank

- Berth dời gần bờ hơn: thêm menu tool `Tourist Boat → Dịch bến sát bờ (+offset)` chỉnh Berth transform theo offset config — chạy xong Sếp nhìn scene chỉnh tay lần cuối (REVIEW).
- Gangplank: object con của Dock, sprite frame-animation "bắc tấm gỗ" (art request 4 frame), bật khi tàu Docked, đảo ngược khi rút. Chưa có art → placeholder 1 sprite gỗ kéo dài (vẫn chạy được logic).

## 4. Formulas

```
nextArrivalUtc(dock) = departureUtc(dock) + gap(dock)        // gap = 5p (1 bến) / 10p (≥2 bến)
arrival hợp lệ khi   |arrival(i) − arrival(j)| ≥ minStagger  // vi phạm → arrival(i) += đủ hiệu
visitors(trip)       = randInt[visitorsMin, visitorsMax]      // 3–6, seed = arrivalUtc (offline tái lập đúng)
goldReward           = Σ FarmItemValue(ingredients) × 2
patienceEndUtc       = bubbleOpenUtc + 30p
tàu rời bến khi       mọi khách ∈ {Served, TimedOut} và đã Board xong
```

## 5. Edge Cases

1. **Tắt game giữa chuyến:** trạng thái chuyến + danh sách khách (charIdx, dishId, served, patienceEndUtc, queueIdx) lưu PlayerPrefs JSON `TouristTrip_{dock}`. Load: khách đang đi bộ → đặt thẳng vào vị trí đích của state đó; patience đã hết trong lúc offline → resolve TimedOut ngay; mọi khách xong → tàu resolve đã rời + tính nextArrival.
2. **Chỉnh đồng hồ lùi > 1 gap:** reset anchor = now (luật V1 giữ nguyên).
3. **Kho đầy lúc giao món:** giao món CHỈ remove item + add thưởng (không add item) → không đụng edge kho đầy.
4. **Level thấp chưa mở món nào (không thể xảy ra vì boat mở L10, cooking mở trước đó):** guard vẫn có — không có món hợp lệ → khách yêu cầu món unlockLevel thấp nhất.
5. **2 khách trùng nhân vật:** không xảy ra trong 1 chuyến (roster 11 ≥ 6 max); 2 chuyến khác bến cùng lúc CÓ THỂ trùng nhau — chấp nhận (khác bến).
6. **Người chơi đứng trong scene bếp khi tàu tới:** hệ khách chỉ chạy ở scene farm; popup thông báo hoãn tới khi quay lại farm (queue 1 thông báo mới nhất).
7. **Tutorial đang chạy:** popup + tàu không tương tác tới khi tutorial xong (check TutorialManager active như V1 UnlockFlow).

## 6. Dependencies

FarmEconomyManager (AddGold/Spend) · PlayerProgressManager (AddExp) · FarmInventoryManager (Has/RemoveItem) · DishData 38 asset + util giá nguyên liệu của CookingChallenge · MissionProgressTracker.ReportEvent · FarmInputLock · CoinFlyFX (event có sẵn) · BoatDockManager/BoatScheduleCore V1 (sửa) · TouristBoatSetupTool (mở rộng) · 11 sheet NVGAME (pipeline cắt + xóa phông) · khung gỗ UI có sẵn.

## 7. Tuning Knobs (TouristBoatConfig — field mới, default)

`gapOneDockMinutes=5 · gapMultiDockMinutes=10 · minStaggerMinutes=3 · visitorsMin=3 · visitorsMax=6 · patienceMinutes=30 · rewardIngredientMultiplier=2 · disembarkInterval=0.8s · walkSpeed (unit/s, chỉnh theo scale scene) · queueSpacing · bubbleScaleInTime=0.25s · smileyFlyTime=1.2s`
(dockMinutes V1 giữ trong config nhưng không dùng ở V2 — đánh dấu Obsolete comment.)

## 8. Acceptance Criteria

1. Mở bến 1: popup báo tàu 01, tàu vào sát bờ, gangplank bật, 3–6 khách xuống đi đúng đường đất, xếp hàng thẳng trước cooking.
2. Bubble chỉ mở ở khách đầu hàng, món luôn thuộc tập đã unlock; giao món trừ đúng 1 món kho, cộng đúng vàng (=Σ nguyên liệu ×2) + EXP món, mặt cười bay lên HUD nhỏ→to→fade.
3. Khách được phục vụ đi về tàu, hàng tiến lên, bubble kế mở; khách cuối lên tàu → gangplank rút → tàu lùi rời bến.
4. 1 bến: tàu lại cập bến sau đúng 5p ±5s. 3 bến: các arrival cách nhau ≥3p, chu kỳ ~10p/bến.
5. Khách chờ quá 30p (test debugTimeScale): icon buồn, về tàu, không cộng tiền.
6. Tắt/mở game ở mọi pha: không kẹt tàu, không nhân đôi khách, không mất thưởng.
7. Console 0 lỗi đỏ · 60fps giữ nguyên (khách dùng SpriteRenderer + Animator thường, ≤18 khách đồng thời).

---

## Phân công (Studio)

| Ai | Việc |
|---|---|
| Dev A (gameplay-programmer) | Schedule V2 event-driven: BoatScheduleCore/BoatDockManager/Config/Controller |
| Dev B (gameplay/ai-programmer) | TouristVisitorManager + TouristAgent + Queue + Bubble + Smiley + persistence |
| Dev C (ui-programmer) | Popup báo tàu + popup mua slot + hiệu ứng mở slot (rework UnlockFlow) |
| Dev D (tools-programmer, gộp vào B) | NPCAnimationSetupTool + TouristVisitorSetupTool + tool dịch bến sát bờ |
| Art (GPT/sprite-forge) | Gangplank 4 frame · bảng khóa slot · mặt cười/mặt buồn bubble (prompt riêng) |
| Lead (tôi) | Pipeline cắt 11 sheet NVGAME + xóa phông trắng + API contract + merge + QA gate |
