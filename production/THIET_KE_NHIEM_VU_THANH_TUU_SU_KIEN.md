# Thiết kế lại: Nhiệm vụ · Thành tựu · Sự kiện · Phần thưởng

Bản thiết kế đầy đủ, soạn lại từ đầu đến cuối. **Chưa code gì — cần bạn duyệt trước.**

Ngày soạn: 06/08/2026 · Dự án: `Cooking-Game-2D` · Unity 6000.3.10f1

---

## 0. Bốn quyết định bạn đã chốt

| Hạng mục | Chọn |
|---|---|
| Popup nhiệm vụ | Dựng lại bằng prefab, tách logic khỏi UI |
| 218 asset cũ | Dọn sạch, soạn lại bảng mới |
| Nút HUD | Gộp cả hai — bong bóng hiện nhiệm vụ đang làm, đổi sang trạng thái mừng khi vừa xong |
| Thưởng nấu ăn | Theo độ khó + điểm mini-game |

---

## 1. Hiện trạng — cái gì đang có thật

Khảo sát toàn bộ source trước khi thiết kế. Không phải "chưa có gì", mà là **có nhiều nhưng lệch nhau**.

### Đang chạy tốt, GIỮ NGUYÊN

| Thứ | Ở đâu | Vì sao giữ |
|---|---|---|
| `MissionProgressTracker` | `Scripts\Mission\MissionProgressTracker.cs` | Khoá `"{EventType}:{itemId}"` + wildcard `":*"`, lưu JSON, có event `OnProgressChanged`. Kiến trúc chắc. |
| **7 điểm hook gameplay** | `PlotController:534,603` · `VillageOrderManager:286` · `CookingChallengeManager:337` · `PenMiniPanelUI:201,245,249` · `ShopItemUI:127` · `MarketManager:179` | **Tài sản giá trị nhất.** Gameplay đã báo tiến độ về đúng chỗ. Không phải cắm lại. |
| `PlayerProgressManager` | `Scripts\Progression\` | Nguồn cấp độ/EXP duy nhất. Cấm đụng. |
| `FarmEconomyManager.AddGold/AddGems` | `Farm\Scripts\Managers\` | Cửa duy nhất trao tiền, có sẵn hiệu ứng xu bay. |
| `VillageOrderManager` + 18 `Order_item_*.asset` | `Farm\Scripts\Village\` | Hệ đơn hàng nhà dân chạy tốt, độc lập. |
| Hierarchy `MissionHudButton` | `Canvas_HUD` trong `SCN_Farm` | Layout đã anchor đúng, chỉ thay script điều khiển. |

### Hỏng hoặc chết, BỎ

| Thứ | Vấn đề |
|---|---|
| `UnifiedTaskPopupUI.cs` (1433 dòng) | Dựng UI 100% bằng `new GameObject()`, hardcode ~200 toạ độ/màu. Đổi tab = xoá sạch dựng lại. Không sửa được trong Editor. |
| `MissionItemUI.cs` + `PlayerWallet.cs` | Code chết. `PlayerWallet` là **ví mồ côi** — không lưu, không hiện HUD → thưởng claim qua đó **mất trắng**. |
| `PopupEwarManager.cs` | Chỉ còn là hộp giữ 3 ref database. |
| 20 asset `Mission_*.asset` ở gốc `Data_Ewa\` | Định dạng cũ, thiếu `eventType`/`requiredLevel`/`missionId` → **đếm sai, không bao giờ hoàn thành**. |
| 99 asset `Mission_a_reach_level_N` | Spam. Thay bằng 1 thành tựu nhiều bậc. |
| 2 bộ daily trùng (`Mission_d_*` và `Mission_daily_*`) | Chọn 1 bộ. |
| `FarmLevelManager` | Gương phản chiếu không lưu, mọi chỗ phải viết `?? fallback`. |
| `MissionProgressTracker` phần MonoBehaviour | **Không có trong bất kỳ scene nào** → `Instance` luôn null, `TryInstallLevelHook()` không bao giờ chạy. |

### Trống hoàn toàn

- **Sự kiện**: `WelfareEventManager` chỉ là vỏ popup bật/tắt. Không data, không thời gian, không thưởng. `LevelReward_L28` ghi "Sự kiện mùa (placeholder)" — chỉ là chữ.
- **Danh hiệu**: L15/20/25/30 ghi "Danh hiệu BẬC THẦY..." nhưng **không có code cấp hay lưu danh hiệu nào**.
- **Thưởng nấu ăn**: `DishData` không có trường exp/gold. Mọi món đều đúng 20 EXP, **0 vàng**. `CookingScoreResult.goldReward` khai báo nhưng không ai gán. Nút `Btn_ClaimRewardBG` gọi `OnClickClaimReward` — **method không tồn tại**.
- **Cổng cấp 5**: `CookingGate` active từ cấp 1, không có script khoá theo cấp.

---

## 2. Kiến trúc mới

### 2.1 Bốn loại dữ liệu, bốn ScriptableObject riêng

Hiện tại thành tựu là `MissionData` tái dụng — đó là lý do phải sinh 99 asset cho một thứ đáng lẽ là một chuỗi bậc. Tách ra:

```
QuestData          — nhiệm vụ (tutorial / chính / hằng ngày)
AchievementData    — thành tựu NHIỀU BẬC (1 asset = 1 chuỗi)
GameEventData      — sự kiện có thời hạn
DishData (+ mở rộng) — thêm expReward / goldReward
```

#### `QuestData`

```
questId          string   — khoá lưu, không đổi sau khi phát hành
questName        string
description      string
icon             Sprite
kind             enum { Tutorial, Main, Daily }

conditions       List<QuestCondition>   ← NHIỀU điều kiện, không chỉ một
  · eventType      MissionEventType
  · targetItemId   string ("" = mọi item)
  · targetAmount   int

requiredLevel    int
requiredQuestId  string   ← khoá chuỗi: chỉ mở khi nhiệm vụ trước xong

rewardGold       int
rewardGems       int
rewardExp        int      ← ✱ HIỆN ĐANG THIẾU, phải thêm
rewardItems      List<ItemReward>

autoClaim        bool     — tutorial tự nhận, không bắt bấm
```

> **`rewardExp` là thứ bắt buộc phải thêm.** Hiện `MissionData` không có trường EXP nên `UnifiedTaskPopupUI` phải **bịa số trong code** (`:93-96` mission 10 exp, thành tựu 20 exp, ReachLevel `level×15`). Không cân bằng được gì khi con số nằm trong code.

#### `AchievementData` — nhiều bậc trong một asset

```
achievementId    string
name             string
icon             Sprite
eventType        MissionEventType
targetItemId     string

tiers            List<Tier>
  · threshold      int
  · rewardGold     int
  · rewardGems     int
  · rewardExp      int
  · title          string   ← danh hiệu, "" nếu bậc này không cho
```

Ví dụ: 3 asset `a_harvest_100/500/2000` gộp thành **một** asset "Nông dân cần mẫn" với 3 bậc. 99 asset `reach_level` gộp thành **một** asset "Thăng tiến" với các bậc 5/10/15/20/25/30.

**Từ 110 asset xuống còn 12.**

#### `GameEventData` — xây từ số 0

```
eventId          string
eventName        string
banner           Sprite
mode             enum { FixedDate, DaysFromFirstLogin }
startUtc/endUtc  string (ISO)  — khi mode = FixedDate
durationDays     int           — khi mode = DaysFromFirstLogin

tasks            List<QuestCondition + điểm thưởng>
milestones       List<{ điểm cần, thưởng }>
```

Đợt này tôi **dựng khung + 1 sự kiện mẫu**, chưa làm nội dung 4 mùa — nói rõ để bạn không kỳ vọng nhầm.

---

### 2.2 Chuỗi nhiệm vụ Cấp 1 → Cấp 5 bám tutorial

Đây là phần bạn yêu cầu rõ nhất: *"lever 1 → lever 5 xoay quanh tutorial, sau khi làm xong là hoàn thành nhiệm vụ"*.

**Cách nối:** thêm `MissionEventType.TutorialStep`, `targetItemId` = tên bước tutorial. Một script cầu nối `TutorialQuestBridge` nghe `TutorialManager` sang bước và báo về tracker. Tutorial không cần biết gì về nhiệm vụ, nhiệm vụ không cần biết gì về tutorial — hai bên chỉ gặp nhau ở cái cầu.

Mốc EXP thật (công thức `PlayerProgressManager.cs:51`): **L2 = 40 · L3 = 50 · L4 = 60 · L5 = 71** (cộng dồn 221 EXP để tới cấp 5).

| # | questId | Tên | Bám bước tutorial | Cấp | Thưởng |
|---|---|---|---|---|---|
| 1 | `q_t01_plant_rice` | Trồng lúa kín 8 ô | `L1L2_06_PlantAllRice` | 1 | 60 vàng · 15 exp |
| 2 | `q_t02_harvest_rice` | Thu hoạch hết lúa | `L1L2_10_HarvestAllRice` | 1 | 80 vàng · 20 exp |
| 3 | `q_t03_plant_flower` | Trồng hoa 6 chậu | `L1L2_14_PlantAllFlowers` | 2 | 90 vàng · 20 exp |
| 4 | `q_t04_harvest_flower` | Thu hoạch hết hoa | `L1L2_17_HarvestAllFlowers` | 2 | 100 vàng · 25 exp |
| 5 | `q_t05_buy_corn` | Mua hạt ngô ở Shop | `L2_03_BuyCorn` | 2 | 60 vàng · 15 exp |
| 6 | `q_t06_feed_pen` | Cho gà ăn | `L2_08_FeedPen` | 2 | 80 vàng · 20 exp |
| 7 | `q_t07_harvest_pen` | Thu trứng từ chuồng | `L2_10_HarvestPen` | 3 | 100 vàng · 25 exp |
| 8 | `q_t08_deliver` | Giao 3 đơn cho nhà dân | `DeliverOrder × 3` | 3 | 150 vàng · 35 exp |
| 9 | `q_t09_buy_pen` | Mua thêm 1 chuồng | `BuyItem: pen_*` | 4 | 200 vàng · 40 exp |
| 10 | `q_t10_train` | Chở hàng lên tàu 1 lần | `TrainLoad × 1` | 4 | 200 vàng · 45 exp |
| 11 | `q_t11_first_cook` | **Nấu món đầu tiên** | `CookDish × 1` | 5 | 300 vàng · 3 gem · 60 exp |

Tổng: **1.420 vàng · 320 EXP**. Cộng với thu hoạch/đơn hàng thường thì người chơi tới cấp 5 vừa đủ, không phải cày.

Nhiệm vụ 1→7 đặt `autoClaim = true` — tutorial xong là tự nhận, **không bắt người chơi bấm** trong lúc còn đang được dắt tay. Từ nhiệm vụ 8 trở đi mới bắt bấm nhận, đúng lúc nút HUD hiện ra.

---

### 2.2b Tutorial kết thúc ở cấp mấy — và sau đó thả tay

**Câu trả lời: cấp 3.** Tính theo EXP thật:

| Việc trong tutorial | EXP |
|---|---|
| Thu hoạch ô lúa chín sẵn (TutorialPrePlant) | 5 |
| 8 ô lúa × 5 | 40 |
| 6 chậu hoa × 5 | 30 |
| 8 ô ngô × 5 | 40 |
| Thu trứng chuồng gà | 10 |
| **Tổng** | **≈ 125** |

Mốc cộng dồn: L2 = 40 · **L3 = 90** · L4 = 150 · L5 = 221.
125 EXP → dừng ở **cấp 3**, được khoảng nửa đường lên cấp 4. Đúng như bạn đoán.

**Từ cấp 3 trở đi: THẢ TAY.** Không còn lớp tối, không còn bàn tay chỉ, không ép thao tác. Chỉ còn:

- **Bảng nhiệm vụ** — hiện ra đúng lúc này (mục 2.4), người chơi *muốn* thì mở, không mở cũng không sao
- Đơn hàng nhà dân tự chạy như thường

Nhiệm vụ 8→11 trong bảng trên vì thế **không phải hướng dẫn** — chúng là mục tiêu tự chọn có thưởng, giống Township. Ai muốn tự mò cũng được.

**Ngoại lệ duy nhất: khoảnh khắc mở bếp ở cấp 5** (mục 2.7). Đó là cả một hệ thống mới nên đáng được chỉ một lần — nhưng chỉ chỉ *vào công trình*, vào trong bếp thì thả hẳn.

Từ cấp 3 lên cấp 5 cần thêm ~96 EXP tự chơi. Bằng khoảng 20 lượt thu hoạch hoặc vài đơn hàng nhà dân.

---

### 2.3 Cấp 5 trở đi — vòng lặp nấu ăn

*"qua lever 5 đổ đi là sẽ mở khóa các món ăn, nấu được món nào thì sẽ được nhận exp vàng, thành tựu của nhiệm vụ đó"*

**Ba việc phải làm:**

**(a) Khoá bếp tới cấp 5.** `CookingGate` hiện active từ cấp 1. Thêm component `LevelGatedObject` (requiredLevel = 5): dưới cấp 5 thì hiện ổ khoá + chữ "Mở ở cấp 5", bấm vào báo còn thiếu bao nhiêu.

**(b) Thêm thưởng vào `DishData`.** Công thức, tính sẵn vào asset để bạn chỉnh tay được:

```
vàng gốc = 20 + unlockLevel×6 + (Easy 0 · Normal 15 · Hard 35)
exp gốc  = 10 + unlockLevel×2 + (Easy 0 · Normal  5 · Hard 12)
hệ số điểm = 1 + (điểm − 70) / 60     → 70 điểm ×1.0 · 100 điểm ×1.5
```

| Món | Cấp | Khó | Vàng | EXP | Nấu 100 điểm |
|---|---|---|---|---|---|
| khoai_tay_chien | 5 | Easy | 50 | 20 | 75 vàng · 30 exp |
| com_chien_trung | 5 | Easy | 50 | 20 | 75 · 30 |
| trung_chien_ca_chua | 5 | Normal | 65 | 25 | 97 · 37 |
| sup_ngo_nam | 6 | Normal | 71 | 27 | 106 · 40 |
| canh_khoai_tay_thit_heo | 6 | Hard | 91 | 34 | 136 · 51 |
| ga_nuong_lu | 7 | Normal | 77 | 29 | 115 · 43 |
| bo_ham_ca_rot | 8 | Hard | 103 | 38 | 154 · 57 |
| pho_bo_tai | 9 | Hard | 109 | 40 | 163 · 60 |
| bo_xao_tieu | 10 | Normal | 95 | 35 | 142 · 52 |

Cấp 5 cần 82 EXP để lên cấp 6 → khoảng **3-4 món dễ**. Không lê thê.

**(c) Mỗi món một nhiệm vụ + góp vào thành tựu.** Nấu lần đầu mỗi món = 1 nhiệm vụ `q_cook_{dishId}` (thưởng gấp đôi món đó, một lần). Nấu lần thứ n góp vào thành tựu "Đầu bếp" (bậc 10/50/200 món).

**Hai lỗi phải sửa luôn:**
- `KitchenTransferManager.SetAfterCooking` trừ **cứng 1 đơn vị** mỗi nguyên liệu, không theo lượng thực dùng (`:182`).
- `CookingChallengeManager.cs:199` gọi `FarmInventoryManager.Instance.AddItem` **không null-check** → NullReference nếu vào bếp thẳng từ Home.

---

### 2.4 Nút HUD — thiết kế 3 trạng thái

Bạn muốn: hiện tên + icon nhiệm vụ vừa xong, có bong bóng mũi tên báo "đến nhận quà", và **ẩn đi trong lúc còn tutorial cho gia súc ăn**.

Giữ nguyên hierarchy `MissionHudButton` đã có, viết lại script điều khiển thành máy trạng thái:

#### Trạng thái A — ẨN

Không hiện gì. Cả nút tròn lẫn bong bóng đều tắt.

**Điều kiện thoát:** người chơi xong bước `L2_10_HarvestPen` (bước cuối của chuỗi cho gia súc ăn) **hoặc** đạt cấp 3 — cái nào đến trước. Lưu cờ `QUEST_HUD_UNLOCKED` để không bao giờ ẩn lại.

> Lấy mốc "hoặc cấp 3" làm lưới an toàn: nếu người chơi bỏ qua tutorial hoặc tutorial lỗi, nút vẫn phải hiện ra chứ không mất luôn.

#### Trạng thái B — BÌNH THƯỜNG

Bong bóng hiện nhiệm vụ **đang làm**: icon, tên, thanh tiến độ `3/8`, nút "Đến".
Nút tròn hiện icon nhiệm vụ + tiến độ nhỏ. Không có chấm đỏ.

#### Trạng thái C — VỪA HOÀN THÀNH

- Bong bóng đổi màu (xanh lá), tiêu đề `Nhiệm Vụ Mới` → **`Hoàn Thành!`**
- Hiện **tên + icon nhiệm vụ vừa xong**
- Nút đổi chữ "Đến" → **"Nhận quà"**
- **Chấm đỏ** trên nút tròn + nảy nhẹ (scale 1.0 ⇄ 1.08, chu kỳ 0.6s)
- Mũi tên nhỏ chỉ vào nút tròn
- Bấm bất kỳ đâu trên bong bóng → mở popup ở đúng tab, cuộn tới đúng nhiệm vụ

Trạng thái C giữ cho tới khi người chơi nhận quà. Nếu xong nhiều nhiệm vụ cùng lúc thì xếp hàng, hiện cái sớm nhất, badge ghi số còn lại.

**Bỏ `Update()` poll mỗi 0.3s** — đã có `OnProgressChanged` và `OnLevelChanged`, polling là thừa.

---

### 2.6 Mở món ăn từ cấp 5 tới cấp 30

**Đếm được: đúng 20 món.** Hiện tất cả đang dồn cục vào cấp 5–10 (18 món) và 2 món cá bị khoá cứng ở `unlockLevel: 99` → không bao giờ mở.

Trước khi xếp phải biết **món nào nấu được sớm nhất** — không được mở món mà người chơi chưa có nguyên liệu. Đối chiếu mốc cấp của cây trồng và chuồng trại:

> Lúa/bắp cải/rau L1 · trứng gà L2 · ngô L2 · cà chua+cà rốt L3 · thịt heo L4 · khoai tây L5 · nấm+thịt bò L6 · mía L7 · chanh L8 · ớt L9 · tiêu L10 · **cá L16 (Hồ Cá)**

Cột **"sớm nhất"** = cấp mà nguyên liệu cuối cùng của món đó có mặt. Cột **"mở ở"** là đề xuất của tôi, luôn ≥ "sớm nhất".

#### Nguyên tắc số một: KHÔNG có món Hard nào trước cấp 17

`difficulty` không phải chỉ là nhãn — nó điều khiển thẳng **độ khó mini-game**
(`CookingTimingMiniGameUI.ApplyDifficulty:121` và `LetterMiniGame.ApplyDifficulty:234` đổi cửa sổ bấm / tốc độ chữ theo Easy·Normal·Hard).

Nên món Hard đúng là chỗ làm người chơi nản và bỏ. Cả 6 món Hard dồn hết về sau cấp 17.

Xếp thêm theo **số nguyên liệu** — món 1 nguyên liệu tiếp cận dễ hơn hẳn món 5 nguyên liệu, vì nó không đòi cả một chuỗi cung ứng ở farm.

> 📌 **BẢNG MÓN ĂN ĐÃ ĐƯỢC CHỐT LẠI — xem `BANG_MON_AN_30.md`.**
> Phương án cuối: **30 món, mở theo 6 mốc 5 cấp** (cấp 5 · 10 · 15 · 20 · 25 · 30), mỗi mốc 5 món.
> Dùng hết 20 món đã có art, chỉ cần vẽ 10 sprite mới. Phần dưới đây (bảng mở từng cấp)
> giữ lại để tham khảo lý do thiết kế, nhưng **số liệu lấy theo `BANG_MON_AN_30.md`**.

#### Vì sao phải THÊM món chứ không chỉ xếp lại

Rà cả 29 sản phẩm farm xem cái nào được dùng thì lòi ra một mảng lớn bị treo:

| Sản phẩm farm | Có món nào dùng? |
|---|---|
| **10 loại hoa** | **không món nào** — 6 loại còn không dùng vào việc gì cả |
| **Sữa** (Chuồng Bò Sữa, cấp 8) | **không món nào** |
| **Mía** (cấp 7) | không món nào dùng trực tiếp |

Tutorial cấp 2 bỏ công dạy người chơi trồng hoa 6 chậu, rồi hoa không đi đâu. Nuôi bò sữa xong không biết làm gì với sữa.

Và danh sách 20 món hiện tại nghiêng hẳn về **món mặn, nhiều thịt** — Sườn heo xào chua ngọt, Bò hầm cà rốt, Thịt heo luộc cuốn rau. Với đối tượng phụ nữ và trẻ em thì nhánh đang bỏ không (hoa và sữa) lại chính là thứ hợp gu nhất: trà hoa, siro, sữa chua, bánh flan, kem.

#### 8 món mới — cần bạn vẽ 8 sprite

Chỉ dùng nguyên liệu đang bị treo. Tất cả đều Easy hoặc Normal.

| Món mới | Nguyên liệu | Khó | Gỡ treo cho |
|---|---|---|---|
| Sữa chua | Sữa · Đường | Easy | sữa |
| Chè bắp sữa | Ngô · Sữa · Đường | Easy | sữa |
| Sinh tố cà rốt sữa | Cà rốt · Sữa · Đường | Easy | sữa |
| Bánh flan | Sữa · Trứng · Đường | Normal | sữa |
| Trà hoa cúc | Hoa cúc trắng · Đường | Easy | **hoa** |
| Siro hoa hồng | Hoa hồng · Chanh · Đường | Easy | **hoa** |
| Trà hoa lan | Hoa lan · Chanh · Đường | Easy | **hoa** |
| Kem hoa oải hương | Sữa · Hoa oải hương · Đường | Normal | **hoa** + sữa |

> Bốn loại hoa chọn làm món đều là hoa ăn/uống được ngoài đời (hồng, oải hương, cúc, lan). **Cố ý không đưa cẩm tú cầu vào món ăn** — ngoài đời nó độc, game cho trẻ em thì không nên dạy sai.

#### Giai đoạn LÀM QUEN — cấp 5 → 19 · 17 món · **không món Hard nào**

| Cấp | Món | Khó | Số NL | Nguyên liệu |
|---|---|---|---|---|
| **5** | Khoai tây chiên | Easy | **1** | Khoai tây |
| **5** | Cơm chiên trứng | Easy | 3 | Lúa · Trứng · Nước tương |
| **5** | Trứng chiên cà chua | Normal | 2 | Trứng · Cà chua |
| 6 | Súp ngô nấm | Normal | 3 | Ngô · Nấm · Trứng |
| 7 | Bắp cải xào nấm | Normal | 3 | Bắp cải · Nấm · Nước mắm |
| 8 | Salad nấm và rau | Normal | 2 | Nấm · Rau thơm |
| 9 | ✨ Sữa chua | Easy | 2 | Sữa · Đường |
| 10 | Nước mía chanh | Easy | 2 | Đường · Chanh |
| **11** | Thịt heo luộc cuốn rau | Normal | 2 | Thịt heo · Rau thơm |
| 12 | ✨ Trà hoa cúc | Easy | 2 | Hoa cúc trắng · Đường |
| **13** | Salad bắp cải chanh | Easy | 3 | Bắp cải · Chanh · Rau thơm |
| 14 | ✨ Chè bắp sữa | Easy | 3 | Ngô · Sữa · Đường |
| **15** | Gà xào ớt | Normal | 2 | Thịt gà · Ớt |
| 16 | ✨ Siro hoa hồng | Easy | 3 | Hoa hồng · Chanh · Đường |
| 17 | ✨ Bánh flan | Normal | 3 | Sữa · Trứng · Đường |
| 18 | Nấm xào thịt bò | Normal | 3 | Nấm · Thịt bò · Nước tương |
| 19 | Trứng ốp la bò né | Easy | 4 | Trứng · Thịt bò · Tiêu · Cà chua |

#### Giai đoạn THỬ THÁCH — cấp 20 → 30 · 11 món · Hard xen kẽ

| Cấp | Món | Khó | Số NL | Nguyên liệu |
|---|---|---|---|---|
| **20** | Canh khoai tây thịt heo | **Hard** | 3 | Khoai tây · Thịt heo · Muối |
| 21 | ✨ Sinh tố cà rốt sữa | Easy | 3 | Cà rốt · Sữa · Đường |
| 22 | Bò hầm cà rốt | **Hard** | 4 | Thịt bò · Cà rốt · Tiêu · Muối |
| 23 | ✨ Trà hoa lan | Easy | 3 | Hoa lan · Chanh · Đường |
| 24 | Cá nướng tiêu | **Hard** | 3 | Cá · Tiêu · Muối |
| 25 | ✨ Kem hoa oải hương | Normal | 3 | Sữa · Hoa oải hương · Đường |
| 26 | Canh chua cá | **Hard** | 4 | Cá · Cà chua · Chanh · Nước mắm |
| 27 | Bò xào tiêu | Normal | 3 | Thịt bò · Tiêu · Nước tương |
| 28 | Sườn heo xào chua ngọt | **Hard** | 4 | Thịt heo · Ớt · Chanh · Cà chua |
| 29 | Gà nướng lu mật mía | Normal | 4 | Thịt gà · Tiêu · Muối · Đường |
| **30** | **Phở bò tái** | **Hard** | **5** | Lúa · Thịt bò · Rau thơm · Chanh · Ớt |

#### Vì sao xếp như vậy

**Cấp nào từ 5 tới 30 cũng có món mới. Không hụt một cấp nào.** 26 cấp, 28 món (cấp 5 được 3 món). Đây là mức dày nhất có thể — đúng tinh thần "build liên tục".

**Cấp 5 cho 3 món, không phải 1.** Mở bếp mà chỉ một món thì không có gì để *chọn*, mất hẳn cảm giác sách công thức. Món đầu tiên là **Khoai tây chiên, đúng 1 nguyên liệu** — nấu được ngay, không phải chuẩn bị gì.

**Không món Hard nào trước cấp 20.** Suốt 15 cấp đầu người chơi chỉ gặp mini-game Easy/Normal.

**Món Hard đầu tiên là món dễ nhất trong nhóm Hard** — Canh khoai tây thịt heo, 3 nguyên liệu quen thuộc từ cấp 5. Bậc thang chứ không quăng thẳng.

**Từ cấp 20, Hard xen kẽ Easy/Normal** — 20 Hard · 21 Easy · 22 Hard · 23 Easy · 24 Hard · 25 Normal… Cấp nào căng thì cấp sau được nghỉ. Không dồn 6 món Hard liên tiếp.

**Cá vào cấp 24 tuy Hồ Cá mở từ cấp 16** — quãng giữa không phí: `LevelReward_L17` đã ghi "Món cá vào pool đơn hàng", nên cá có chỗ tiêu thụ ngay từ cấp 17 qua đơn hàng nhà dân, còn món cá thì để dành cho giai đoạn thử thách.

**Phở bò tái ở cấp 30.** Món khó nhất, 5 nguyên liệu, món Việt biểu tượng — phần thưởng đỉnh, đi kèm danh hiệu "BẬC THẦY NÔNG TRẠI" mà `LevelReward_L30` đã hứa.

#### Tổng kết

| Quãng | Số món | Easy | Normal | Hard |
|---|---|---|---|---|
| Cấp 5–19 | 17 | 8 | 9 | **0** |
| Cấp 20–30 | 11 | 3 | 2 | 6 |
| **Tổng** | **28** | **11** | **11** | **6** |

Trước: 5 Easy · 9 Normal · 6 Hard trên 20 món (30% Hard).
Sau: 11 · 11 · 6 trên 28 món (21% Hard), và toàn bộ Hard nằm sau cấp 20.

#### Còn 4 loại hoa vẫn chưa có chỗ dùng

Cúc vạn thọ · Anh thảo · Cẩm tú cầu · Mẫu đơn — không nên ép vào món ăn (cẩm tú cầu độc thật ngoài đời). Đề xuất cho chúng vào **đơn hàng nhà dân** dạng bó hoa, cùng kiểu hoa hồng và tulip đang có. Việc nhỏ, chỉ thêm asset `Order_item_*`.

---

### 2.6c Bỏ ba máy chế biến

Rà lại thì **không có sản phẩm nào của ba máy được món ăn dùng tới**:

| Máy | Sản phẩm | Món nào dùng? |
|---|---|---|
| `may_01` Máy Xay Bột | `bot_gao` | không món nào |
| `may_02` Máy Ép Mía | `nuoc_mia_ep` | không món nào |
| `may_03` Máy Phô Mai | `pho_mai` | không món nào — chỉ 1 đơn hàng nhà dân |

**Quyết định: bỏ cả ba.** Mọi thứ liên quan nấu ăn gói gọn trong scene bếp; farm chỉ lo trồng trọt và chăn nuôi. Bốn gia vị (muối · nước mắm · nước tương · đường) mua ở chợ.

**Việc kéo theo:**
- Xoá 3 `Config_May*.asset` + `Máy Xay Bột.asset` trong shop + `Item_PhoMai.asset`
- Gỡ `OrderItem_PhoMai.asset` khỏi pool đơn hàng nhà dân (nếu không sẽ có đơn đòi món không ai làm ra được)
- Sửa `unlockDescriptions` của `LevelReward_L11 / L13 / L15` — bỏ dòng "Máy … đã mở bán trong Shop", thay bằng món ăn mới ở bảng trên

---

### 2.6b ⛔ CHẶN: 12/20 món thiếu nguyên liệu KHÔNG TỒN TẠI trong farm

Bảng 2.6 ở trên mới chỉ kiểm *hạt giống đã mở bán chưa*. Kiểm sâu hơn — truy từng `IngredientData` của cả 20 món về nguồn thật trong farm (cây trồng / chuồng / máy / chợ) — thì phát hiện **5 nguyên liệu không có nguồn nào cả**:

| Nguyên liệu | id | Loại | Chặn mấy món | Vì sao thiếu |
|---|---|---|---|---|
| **Rau thơm** | `herbs` | nguyên liệu | **4** | Không có cây trồng nào cho ra `herbs` |
| **Nước tương** | `soysauce` | gia vị | **3** | Chợ có `salt` và `fishsauce` nhưng thiếu cái này |
| **Cá** | `ca` | nguyên liệu | **2** | Hồ Cá chưa tồn tại — mới chỉ là chữ ở `LevelReward_L16` |
| **Thịt gà** | `chicken` | nguyên liệu | **2** | Chuồng gà **CÓ** sản xuất, nhưng tên là `chicken_meat` — **lệch id** |
| **Đường** | `sugar` | gia vị | **2** | Mía trồng được, máy ép mía có, nhưng không cho ra `sugar` |

#### 8 món nấu được ngay hôm nay

Khoai tây chiên · Trứng chiên cà chua · Súp ngô nấm · Bắp cải xào nấm · Canh khoai tây thịt heo · Bò hầm cà rốt · Trứng ốp la bò né · Sườn heo xào chua ngọt

#### 12 món đang kẹt

| Món | Thiếu |
|---|---|
| Cơm chiên trứng | nước tương |
| Bò xào tiêu · Nấm xào thịt bò | nước tương |
| Nước mía chanh | đường |
| Gà xào ớt | thịt gà *(lệch id)* |
| Gà nướng lu mật mía | thịt gà *(lệch id)* + đường |
| Salad nấm và rau · Thịt heo luộc cuốn rau · Salad bắp cải chanh · Phở bò tái | rau thơm |
| Cá nướng tiêu · Canh chua cá | cá |

> **Cơm chiên trứng** là món tôi định cho ở cấp 5 — nó cũng đang kẹt vì nước tương.

#### Bốn việc phải làm, xếp theo công sức

**1. Sửa lệch id `chicken` — 5 phút, mở 2 món.**
`Config_Pen03_Ga` sản xuất `chicken_meat`, món ăn đòi `chicken`. Đổi một chỗ cho khớp. Không cần vẽ gì. Đây thuần tuý là lỗi, không phải thiếu nội dung.

**2. Thêm nước tương + đường — nửa buổi, mở 5 món.**
Chợ đã bán `salt` và `fishsauce` rồi, thêm `soysauce` theo đúng khuôn.
Riêng **đường** nên làm khác: mía trồng được từ cấp 7, máy ép mía `may_02` đã có sẵn và đang cho ra `nuoc_mia_ep`. Cho nó ra thêm `sugar` là đúng vòng lặp nông trại — trồng mía, ép ra đường, nấu ăn. Hay hơn nhiều so với mua ngoài chợ.

**3. Thêm cây Rau thơm — cần bạn vẽ, mở 4 món.**
Ảnh hưởng nhiều nhất, gồm cả **Phở bò tái** mà tôi định để dành cấp 30. Cần một `CropData` mới + bộ sprite các giai đoạn lớn. Tôi dựng data và nền, bạn vẽ art.

**4. Hồ Cá — việc lớn nhất, mở 2 món.**
Chưa có gì cả: không object, không hệ thống câu/nuôi, không sản phẩm. `LevelReward_L16` đã hứa "Hồ Cá đã mở" nhưng đó là lời hứa duy nhất trong toàn bộ bảng phần thưởng chưa thực hiện.

#### Nguyên tắc bắt buộc từ giờ

> **Không mở món trước khi chuỗi nguyên liệu của nó chạy được.**
> Mở món mà người chơi không nấu nổi thì tệ hơn là chưa mở — nó biến phần thưởng lên cấp thành lời hứa suông.

Bảng 2.6 ở trên đã xếp theo phương án **làm đủ cả 4 việc + thêm 8 món mới** → 28 món.

Nếu tạm hoãn rau thơm và cá thì mất 6 món (Salad nấm và rau L8 · Thịt heo luộc cuốn rau L11 · Salad bắp cải chanh L13 · Cá nướng tiêu L24 · Canh chua cá L26 · **Phở bò tái L30**) — hụt cấp 8, 11, 13 và mất món đỉnh cuối game. Nên ưu tiên **rau thơm trước** vì nó rẻ (4 sprite cây) mà gỡ được 4 món.

---

> **Cần bạn vẽ:** 8 sprite món mới · 4 sprite cây rau thơm · hồ cá. Tôi dựng data và nền trước, để trống ô sprite có nền màu để bạn kéo art vào.

---

### 2.7 Khoảnh khắc mở bếp ở cấp 5

Đây là ngoại lệ duy nhất sau khi thả tay. Chỉ chạy **một lần**, có cờ `COOKING_INTRO_DONE`.

**Bước 1 — Popup lên cấp 5.** Dải ô tròn "vừa mở khoá" hiện 4 ô:

```
[🏠 NHÀ BẾP ĐÃ MỞ]  [🍟 Khoai tây chiên]  [🍚 Cơm chiên trứng]  [🍳 Trứng chiên cà chua]
```

Ô nhà bếp to hơn, có viền sáng — nó là cái chính, ba món là hệ quả.

**Bước 2 — Bấm "Bắt đầu nào" xong, chuỗi tự chạy:**

| Nhịp | Việc | Thời lượng |
|---|---|---|
| 1 | Camera lia mượt tới `CookingGate` (ease-in-out) | 1.2s |
| 2 | Nền tối nhẹ (alpha 0.45), khoét lỗ tròn quanh công trình | 0.3s |
| 3 | Công trình nảy nhẹ + viền sáng nhấp nháy | lặp |
| 4 | Bàn tay hiện, gõ vào giữa công trình | lặp |
| 5 | Dòng chữ dưới công trình: *"Bếp đã mở! Bấm vào để nấu ăn"* | giữ |

**Bước 3 — Người chơi bấm vào bếp** → tắt nền tối, tắt tay, ghi cờ, vào scene nấu ăn.

**Bước 4 — Trong bếp: KHÔNG hướng dẫn gì.** Sách công thức mở sẵn, ba món sáng, phần còn lại xám có ghi "Mở ở cấp N". Người chơi tự chọn, tự nấu.

**Chống kẹt:** nếu người chơi không bấm trong 15 giây thì tắt nền tối và tay, trả camera về, vẫn ghi cờ. Công trình giữ viền sáng cho tới lần nấu đầu tiên. Không bao giờ được để nền tối khoá màn hình vĩnh viễn — đây đúng là lỗi vừa gặp ở tutorial trồng trọt.

**Dùng lại đồ đã có:** `TutorialCameraFocus` (lia camera), `UnmaskRaycastFilter` (khoét lỗ), `TutorialActionHandGuide.GuidePoint` (bàn tay). Không viết mới, chỉ gọi lại ngoài luồng tutorial.

**Kèm theo:** thêm `LevelGatedObject` vào `CookingGate` — dưới cấp 5 hiện ổ khoá + "Mở ở cấp 5", bấm vào báo còn thiếu bao nhiêu. Hiện công trình này **bật từ cấp 1**, ai cũng vào bếp được.

---

### 2.5 Gộp save

Hiện có **5 loại khoá rời rạc** cho cùng một hệ thống, không có version, 3 file định nghĩa trùng nhau:

```
MISSION_PROGRESS_V1
MISSION_CLAIMED_{id}
MISSION_CLAIMED_DAILY_{yyyyMMdd}_{id}
ACHIEVEMENT_CLAIMED_{id}
UNIFIED_TASK_DAILY_LAST_SEEN / _STREAK / _CLAIMED_DATE
```

Gộp thành **một** khoá `QUEST_SAVE_V1` chứa `saveVersion` + tiến độ + đã-nhận + daily + thành tựu + sự kiện, kèm bước chuyển đổi đọc dữ liệu cũ để **người chơi hiện tại không mất tiến trình**.

Khoá mới không đụng bất kỳ prefix nào đang dùng (`PLAYER_`, `FARM_`, `PLOT_`, `Pen`, `GUIDE_`, `TUTORIAL_`, `STARTER_`, `WAREHOUSE_`, `KITCHEN_`).

---

## 3. Chia việc

Theo cách làm quen thuộc: 2 dev + 1 tester, trao đổi qua file MD trong `production\`.

### DEV-A — Dữ liệu và logic (không đụng UI)

| # | Việc | Kết quả |
|---|---|---|
| A1 | 4 ScriptableObject mới + `MissionEventType.TutorialStep` | `QuestData.cs`, `AchievementData.cs`, `GameEventData.cs`, mở rộng `DishData` |
| A2 | `QuestManager` — nạp bảng, xét điều kiện, phát thưởng, lưu | thay `UnifiedTaskPopupUI` phần logic |
| A3 | Gộp save + chuyển đổi từ 5 khoá cũ | `QUEST_SAVE_V1` |
| A4 | `TutorialQuestBridge` nối 11 nhiệm vụ tutorial | L1→L5 chạy thông |
| A5 | Editor tool sinh lại bảng nhiệm vụ/thành tựu | dọn 218 asset → ~45 |
| A6 | Thưởng nấu ăn + sửa 2 lỗi mục 2.3 | vòng lặp cấp 5+ chạy |
| A7 | Sửa `unlockLevel` 20 món theo bảng 2.6 + `LevelGatedObject` cho `CookingGate` | mở món đúng nhịp, bếp khoá tới cấp 5 |
| A8 | Thêm ô "mở khoá món ăn" vào 20 asset `LevelReward_L*` tương ứng | popup lên cấp khoe được món mới |
| A9 | Sửa 5 nguyên liệu thiếu: lệch id `chicken`, thêm `soysauce`+`sugar` vào chợ, cây Rau thơm, Hồ Cá | 20 món nấu được thật |
| A10 | Bỏ 3 máy chế biến + gỡ `OrderItem_PhoMai` + sửa mô tả `LevelReward_L11/13/15` | farm chỉ còn trồng trọt & chăn nuôi |
| A11 | Gộp 6 cặp `IngredientData` trùng ở 2 thư mục | hết chuyện sửa nhầm bộ mồ côi |
| A12 | Tạo 8 `DishData` mới (sữa & hoa) + 8 `IngredientData` còn thiếu nếu có | 28 món, cấp nào cũng có món mới |
| A13 | Thêm đơn hàng bó hoa cho 4 loại hoa còn treo | mọi sản phẩm farm đều có chỗ dùng |

### DEV-B — Giao diện (dựng nền có màu, bạn gắn art sau)

| # | Việc | Kết quả |
|---|---|---|
| B1 | Prefab popup 4 tab (Nhiệm vụ · Hằng ngày · Thành tựu · Sự kiện) | `QuestPopup.prefab` |
| B2 | Prefab dòng nhiệm vụ + dòng thành tựu nhiều bậc | `QuestRow.prefab`, `AchievementRow.prefab` |
| B3 | Viết lại `MissionHudButtonUI` thành 3 trạng thái mục 2.4 | `QuestHudButtonUI.cs` |
| B4 | Hiệu ứng nhận quà: xu bay, sao, chấm đỏ, nảy | dùng lại `CoinFlyFX` có sẵn |
| B5 | Editor tool dựng lại hierarchy popup | bấm một nút ra đủ khung |
| B6 | Chuỗi mở bếp cấp 5 mục 2.7 (camera → nền tối → tay → chữ), kèm chống kẹt 15s | `CookingUnlockIntro.cs` |
| B7 | Ổ khoá + chữ "Mở ở cấp N" trên thẻ món trong sách công thức | `DishCardUI` bổ sung |

### TESTER

| # | Việc |
|---|---|
| T1 | Chạy thông cấp 1→5 bằng tutorial, chụp từng bước, xác nhận 11 nhiệm vụ nhận đúng |
| T2 | Nấu 5 món, đối chiếu vàng/exp với bảng mục 2.3 |
| T3 | Thử người chơi có save cũ: tiến trình nhiệm vụ cũ phải chuyển đổi được, không mất |
| T4 | Thử nút HUD: ẩn đúng lúc, hiện đúng lúc, 3 trạng thái đúng |
| T5 | Kiểm biên dịch + rà chéo mọi con số thưởng |

---

## 4. Những gì tôi KHÔNG làm trong đợt này

Nói trước để không hiểu nhầm phạm vi:

- **Nội dung sự kiện 4 mùa** — chỉ dựng khung + 1 sự kiện mẫu.
- **Art** — mọi thứ dựng nền có màu để bạn biết chỗ gắn.
- **Cân bằng kinh tế toàn game** — chỉ cân phần nhiệm vụ và nấu ăn. Bảng `LevelRewardConfig` L2→L30 (38.700 vàng, 210 gem) giữ nguyên.
- **Danh hiệu hiển thị trên hồ sơ** — thành tựu sẽ *cấp* danh hiệu, nhưng chỗ khoe danh hiệu trên avatar là việc khác.

---

## 5. Rủi ro

| Rủi ro | Cách chặn |
|---|---|
| Xoá 218 asset làm mất dữ liệu cân bằng đã soạn | Xuất toàn bộ ra CSV trước khi xoá, tool sinh lại đọc từ CSV đó |
| Người chơi hiện tại mất tiến trình nhiệm vụ | Bước chuyển đổi đọc cả 5 khoá cũ, T3 kiểm riêng việc này |
| `TutorialQuestBridge` bám tên bước — đổi tên bước là đứt | Bảng ánh xạ nằm trong một asset, sai thì log cảnh báo rõ chứ không im lặng |
| Popup mới thiếu chức năng so với bản cũ | Liệt kê chức năng bản cũ thành danh sách đối chiếu trước khi xoá |
| Bỏ `FarmLevelManager` làm vỡ chỗ khác | Có 6+ chỗ đang gọi `?? FarmLevelManager` — sửa hết rồi mới xoá, hoặc giữ lại như vỏ rỗng |

---

## 6. Cần bạn duyệt

1. Chuỗi 11 nhiệm vụ cấp 1→5 ở mục 2.2 — đúng thứ tự bạn muốn chưa?
2. Bảng thưởng nấu ăn ở mục 2.3 — con số có hợp lý không?
3. Mốc hiện nút HUD: xong `L2_10_HarvestPen` **hoặc** đạt cấp 3 — đúng ý "tầm cấp 2-3 lúc kết thúc tutorial" chưa?
4. **Bảng 28 món cấp 5→30 ở mục 2.6** — cấp nào cũng có món mới, không Hard trước cấp 20, Hard xen kẽ từ cấp 20. Đặc biệt xem **8 món mới** (sữa & hoa) có đúng gu không.
5. **Chuỗi mở bếp ở mục 2.7** — camera lia + tay chỉ + viền sáng, rồi thả hẳn khi vào bếp. Đúng ý chưa?
6. Phạm vi mục 4 — có gì bạn muốn thêm vào đợt này không?

Duyệt xong tôi giao việc cho hai dev và bắt đầu.
