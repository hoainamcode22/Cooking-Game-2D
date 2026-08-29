# QA REPORT — Tourist Boat System V2 (BOAT-002) · Review chéo 3 gói Dev

> **⚠️ ĐỌC MỤC 7 TRƯỚC.** Mục 0–6 là **VÒNG 1** (2026-08-29, 15:0x) — giữ nguyên làm hồ sơ.
> Kết luận hiện hành và checklist Play Mode CUỐI CÙNG nằm ở **§7 — VÒNG 2 (regression)**.

> **Ngày:** 2026-08-29 · **QA Lead** · Gói review: `/home/user/work/deliver/devA|devB|devC`
> **Spec:** `/home/user/work/tourist-boat-system-v2.md` · **Source đối chiếu (read-only):** `/mnt/user-data/uploads/Cooking-Game-2D/`
> **Đã chạy thật:** biên dịch `mcs` 3 pass + chạy bộ test console của Dev A bằng `mono`.

---

## 0. VERDICT VÒNG 1 *(đã được thay thế — xem §7)*

# ⛔ FIX FIRST *(vòng 1)*

**Lý do:** Contract giữa 3 Dev khớp 100% và code biên dịch sạch (0 error / 0 warning) — phần "ghép nối" làm rất tốt. Nhưng có **4 lỗi hành vi chặn Acceptance Criteria §8**, trong đó 2 lỗi làm **hệ tự chết vĩnh viễn** trong phiên chơi (tàu kẹt không bao giờ rời bến · popup báo tàu ngừng hoạt động sau lần đầu vào bếp), 1 lỗi **mất món của người chơi mà không trả thưởng**, và 1 lỗi khiến **không thể nghiệm thu AC §8.4/§8.5 bằng `debugTimeScale`** như GDD yêu cầu.

Không có lỗi nào cần Sếp bỏ gói code — tất cả đều sửa được trong ngày, mỗi lỗi 1–20 dòng. **Đề nghị: trả về Dev sửa 4 mục BLOCKING + 3 mục MAJOR nặng nhất, rồi copy.**

---

## 1. BẢNG SỐ LIỆU

### 1.1 Compile-check thật (mcs 6.8.0 / mono, stub `UnityEngine`/`UnityEditor`/`TMPro` tự dựng)

| Pass | Nội dung | File .cs | Error | Warning |
|---|---|---:|---:|---:|
| **1** | 19 file của 3 Dev + 14 file source thật của project + stub · `-define:UNITY_EDITOR` | 36 | **0** | **0** |
| **2** | Giả lập **player build**: bỏ `UNITY_EDITOR`, bỏ mọi file trong `Editor/` | 27 | **0** | **0** |
| **3** | **Mô phỏng MERGE THẬT**: pass 1 + 2 Editor tool V1 còn lại trong project (`TouristBoatDiagnosticTool.cs`, `TouristBoatSetupTool.cs`) | 38 | **0** | **0** |

> Pass 3 là bằng chứng quan trọng nhất: **Dev A giữ đúng 100% API V1** — 2 tool cũ ngoài phạm vi Dev A vẫn biên dịch được sau khi `BoatScheduleCore`/`BoatDockManager` bị viết lại. Console Unity sẽ **không đỏ** khi copy.
>
> Script tái lập: `/home/user/work/qa/compile.sh`, `compile_player.sh`, `compile_full.sh` · stub: `/home/user/work/qa/stubs/`.

### 1.2 Bộ test đơn vị của Dev A

```
mcs -out:/tmp/boattests.exe BoatScheduleCore.cs BoatScheduleCoreTests.cs && mono /tmp/boattests.exe
→ TỔNG KẾT: 98 PASS · 0 FAIL      exit=0     ✅ đúng như HANDOFF devA mục 6
```

### 1.3 Kiểm khớp contract (mục 1 + 2 của brief)

| Hạng mục | Kết quả |
|---|---|
| Dev B/C gọi sang Dev A (17 thành viên: `OnBoatDocked` · `OnBoatDeparting` · `OnNextTripScheduled` · `IsDocked` · `UnlockedDockCount` · `ReportVisitorsAllAboard` · `Config` · `BoatNumber` · `GetDockBerth` · `IsDockUnlocked` · `IsIntroDone` · `IsReady` · `MarkIntroDone` · `CanUnlockDock` · `TryUnlockDock` · `UnlockDockFree` · `OnDockUnlocked`) | ✅ **khớp tuyệt đối**, đúng chữ ký |
| 12 field config V2 Dev B/C đọc (`visitorsMin/Max`, `patienceMinutes`, `rewardIngredientMultiplier`, `disembarkInterval`, `visitorWalkSpeed`, `queueSpacing`, `bubbleScaleInTime`, `smileyFlyTime`, `lockPanelWidth/Height`, `unlockLevel`, `GetDockRequirement`) | ✅ **tồn tại đủ** trong `TouristBoatConfig` V2 |
| Dev A gọi ngược sang B/C | ✅ **không có** (đúng thiết kế 1 chiều) |
| API ngoài (`FarmEconomyManager.AddGold/SpendGold/SpendGems/OnCurrencyChanged` · `FarmInventoryManager.HasItem/RemoveItem` · `FarmLevelManager.CurrentLevel/HasReached/OnLevelChanged` · `PlayerProgressManager.AddExp` · `BasePriceBook.TryGetBasePrice` · `MissionProgressTracker.ReportEvent` · `MissionEventType.DeliverOrder` · `FarmInputLock.*` · `FarmUIManager.ShowHint` · `AudioManager.Instance.PlayBuySell` · `BoatDockSlot` · `DishData.dishId/unlockLevel/rewardExp/sellPrice/requiredIngredients/dishSprite` · `IngredientData.id`) | ✅ **tồn tại đúng chữ ký** trong source gốc |
| Trùng `MenuItem` path giữa 3 tool mới + tool V1 | ✅ không trùng |
| Trùng tên class giữa 3 gói | ✅ không (compile chứng minh) |
| `dishId` dùng làm itemId kho | ✅ **đúng** — `CookingChallengeManager.cs:228` gọi `AddItem(cookedDishOnPlate.dishId, 1)` |

**⇒ Không có finding BLOCKING nào thuộc mục 1 (contract) hay mục 2 (API ngoài).** Toàn bộ finding dưới đây là **logic/hành vi**.

### 1.4 Tổng finding

| Mức | Số lượng |
|---|---:|
| 🔴 BLOCKING (B) | **4** |
| 🟠 MAJOR (M) | **6** |
| 🟡 minor (m) | **11** |
| **Tổng** | **21** |

---

## 2. FINDING — 🔴 BLOCKING

### B-1 · Thiếu `TouristQueue` trong scene ⇒ TÀU KẸT VĨNH VIỄN (không có lưới an toàn)

**File:** `devB/.../Visitors/TouristVisitorManager.cs:324` (và `:376`) · `devB/.../Visitors/TouristAgent.cs:316-330`

```csharp
// TouristVisitorManager.DisembarkRoutine
int slot = queue != null ? queue.Enqueue(agent) : i;   // ← queue null: slot = i
agent.AssignInitialSlot(slot, slot == 0);              // ← chỉ khách i==0 là "đầu hàng"
```
```csharp
// TouristAgent.TickWaitServe
if (_isFront) OpenBubbleIfNeeded();       // chỉ đầu hàng mới đặt PatienceEndUtcTicks
if (PatienceEndUtcTicks <= 0) return;     // ← khách 1..n: return mãi mãi
```

**Kịch bản gãy cụ thể:**
1. Sếp copy code, mở scene farm nhưng **chưa chạy** `Setup Tourist Visitors (Scene)` (hoặc đã chạy rồi sau đó xoá/đổi tên `QueueAnchor`, hoặc kéo nhầm nó thành con của object bị `SetActive(false)` ngoài tầm `FindFirstObjectByType(...Include)`).
2. `EnsureSceneRefs()` chỉ `Debug.LogWarning` rồi đi tiếp — **không dừng, không lưới an toàn**.
3. Tàu 01 cập bến, 4 khách xuống. Khách #0 mở bubble, hết 30 phút → về tàu. Khách #1,#2,#3 **không bao giờ được lên đầu hàng** (không có `TouristQueue.Remove()` để dồn hàng) ⇒ `PatienceEndUtcTicks` giữ nguyên 0 ⇒ **không bao giờ TimedOut, không bao giờ Board**.
4. `CheckAllAboard` không bao giờ đạt `DoneCount == total` ⇒ **`ReportVisitorsAllAboard` không bao giờ được gọi** ⇒ tàu `Docked` VÔ HẠN (Dev A cố ý không có luật tự rời bến — HANDOFF devA §5 câu hỏi 1).
5. **Hệ boat chết hoàn toàn**: không chuyến mới, không popup, xoá PlayerPrefs cũng vô ích vì lỗi tái diễn.

**Vi phạm:** AC §8.6 *"Tắt/mở game ở mọi pha: **không kẹt tàu**"*.

**Cách sửa đề xuất (Dev B, ~10 dòng):**
- Trong `EnsureSceneRefs()`: `queue == null` ⇒ `Debug.LogError` + **tự tạo runtime** một `TouristQueue` tại `transform.position` (khách chồng nhau nhưng hệ vẫn chạy), thay vì đi tiếp với `queue = null`.
- **Và** thêm lưới an toàn độc lập trong `TouristVisitorManager`: một `Coroutine` giám sát mỗi 30s — nếu `trip` tồn tại quá `patienceMinutes + 10 phút` mà `DoneCount < total` thì ép `timedOut[]` toàn bộ + `ReportVisitorsAllAboard`. (Đây cũng chính là câu hỏi mở #1 trong HANDOFF devA — **QA đề nghị Sếp chốt: CÓ, phải có lưới an toàn**, vì hiện tại một mình Dev B kẹt là cả hệ chết.)

---

### B-2 · `debugTimeScale` KHÔNG áp cho kiên nhẫn khách ⇒ 2 thang thời gian, không nghiệm thu được AC §8.4/§8.5

**File:** `devB/.../Visitors/TouristAgent.cs:416-421` · `devB/.../Visitors/TouristVisitorManager.cs` (toàn file) — `grep debugTimeScale` trong `devB/` + `devC/` = **0 kết quả**.

```csharp
// TouristAgent.OpenBubbleIfNeeded — KHÔNG chia debugTimeScale
float minutes = _config != null ? Mathf.Max(0.1f, _config.patienceMinutes) : 30f;
PatienceEndUtcTicks = DateTime.UtcNow.Ticks + (long)(minutes * 60.0 * TimeSpan.TicksPerSecond);
```

**Kịch bản gãy cụ thể:** Sếp làm đúng theo HANDOFF devA §3 bước 7 → đặt `debugTimeScale = 60` để test.
- Dev A chia MỌI duration cho 60 (`BoatDockManager.EffectiveGapSeconds/EffectiveTravelSeconds`): gap 5 phút → **5 giây thực**, travel 20s → 0,33s.
- Dev B **không chia**: khách vẫn chờ **30 PHÚT THỰC**.
- Kết quả: tàu cập bến sau 5 giây, rồi **đậu 30 phút thực** chờ khách. Trên màn hình trông y hệt B-1 ("tàu kẹt"), Sếp/QA không phân biệt được lỗi thật với lỗi cấu hình.
- **AC §8.5 (*"Khách chờ quá 30p — test debugTimeScale"*) là bất khả thi**: GDD ghi rõ dùng `debugTimeScale` để test ca này, nhưng knob đó không tác động tới patience.
- **AC §8.4 (*"1 bến: cập bến lại sau đúng 5p ±5s"*)** cũng không đo được trong 1 phiên test ngắn vì mỗi chu kỳ bị chặn 30 phút thực.

**Vi phạm:** GDD §7 (debugTimeScale là knob chung của hệ boat) + AC §8.4 + §8.5.

**Cách sửa đề xuất (Dev A + Dev B, 3 dòng):** Dev A mở thêm 1 API đọc-chỉ `public float EffectiveDebugTimeScale { get; }` (đã có sẵn hàm private `EffectiveTimeScale()`); Dev B chia `minutes` cho giá trị đó khi đặt `PatienceEndUtcTicks`, và `disembarkInterval` nữa cho đồng bộ. **Lưu ý:** phải chia lúc ĐẶT mốc (không chia lúc so sánh), để mốc UTC đã persist vẫn đúng sau khi tắt/mở game.

---

### B-3 · `RemoveItem` thành công nhưng thưởng = 0 / không cộng được ⇒ NGƯỜI CHƠI MẤT MÓN KHÔNG ĐƯỢC ĐỀN

**File:** `devB/.../Visitors/TouristVisitorManager.cs:561-576` · `devB/.../Visitors/TouristSmileyFlyFX.cs:187-229` (`TouristRewardCalculator.ComputeGold`)

```csharp
if (!kho.RemoveItem(dish.dishId, 1)) { ...; return; }   // ✅ đúng thứ tự: trừ trước

int vang = TouristRewardCalculator.ComputeGold(dish, mul, out fallback);
int exp  = TouristRewardCalculator.ComputeExp(dish);

if (vang > 0) FarmEconomyManager.Instance?.AddGold(vang);   // ← ①②
if (exp  > 0) PlayerProgressManager.Instance?.AddExp(exp);  // ← ①②
```

**Hai kịch bản gãy cụ thể — món đã bị TRỪ KHỎI KHO trước cả hai:**

① **`vang == 0` một cách hợp lệ.** `ComputeGold` rơi vào nhánh fallback (`requiredIngredients` rỗng, hoặc `ing.id` rỗng, hoặc `BasePriceBook.TryGetBasePrice` trả false) → `return Mathf.RoundToInt(dish.sellPrice * mul)`. **`DishData.sellPrice` mặc định = 0** (`DishData.cs:65`). Trong 38 asset món của dự án, chỉ cần MỘT asset chưa điền `sellPrice` và chưa khai `requiredIngredients` là: **món biến mất khỏi kho, khách cười, người chơi được 0 vàng.** Không có log lỗi nào nói "bạn vừa mất món" — chỉ có `LogWarning` về giá.

② **`FarmEconomyManager.Instance == null`** (vào thẳng scene farm khi test, hoặc manager bị destroy). `?.` nuốt lỗi êm ru → món mất, vàng không cộng, **không một dòng log**.

**Vi phạm:** AC §8.6 *"không mất thưởng"* + AC §8.2 *"giao món trừ đúng 1 món kho, **cộng đúng vàng + EXP**"*.

**Cách sửa đề xuất (Dev B, ~12 dòng):** đảo thành giao dịch có hoàn tác —
```csharp
int vang = ...; int exp = ...;
var eco = FarmEconomyManager.Instance;
if (eco == null || vang <= 0) {
    kho.AddItem(dish.dishId, 1);                     // HOÀN món về kho
    FarmUIManager.Instance?.ShowHint("Chưa nhận được thưởng — món đã trả lại kho.");
    Debug.LogError($"[TouristVisitor] Thưởng hỏng cho '{dish.dishId}' (vang={vang}, eco={eco != null}) — đã hoàn món.");
    return;                                          // KHÔNG MarkServed
}
eco.AddGold(vang);
```
Đồng thời **kẹp sàn thưởng**: `ComputeGold` khi fallback mà `sellPrice <= 0` thì trả `BasePriceBook.DefaultBasePrice * mul` (=20) + `LogWarning`, thay vì 0.
> Ghi chú: `AddItem` có thể bị từ chối khi kho đầy (`FarmInventoryManager.AddItem` F8) — nhưng ở đây ta vừa trừ đúng 1 slot của CHÍNH item đó nên slot vẫn còn, không đụng edge §5.3.

---

### B-4 · Popup "Tàu sắp cập bến" CHẾT VĨNH VIỄN sau lần đầu vào bếp

**File:** `devC/.../UI/BoatAnnouncePopupUI.cs:188-189, 198-229` · tương tác với `FarmUIManager.cs:471-472`

```csharp
// BoatAnnouncePopupUI.HandleNextTripScheduled
if (_drainRoutine == null)
    _drainRoutine = StartCoroutine(DrainRoutine());   // ← chỉ khởi động lại khi field == null
...
// DrainRoutine chỉ set _drainRoutine = null ở DÒNG CUỐI (:228) khi thoát bình thường
```
```csharp
// FarmUIManager.EnterCookingMode()  (source gốc, dòng 471-472)
if (canvasPopupRoot != null) canvasPopupRoot.SetActive(false);
```

**Kịch bản gãy cụ thể:**
1. `TouristBoatUIPopupSetupTool` **cố ý** đặt `TouristBoatPopups` dưới `FarmUIManager.canvasPopupRoot` (HANDOFF devC §3 mục 1: *"canvas đó bị EnterCookingMode() tắt khi vào bếp → popup boat tự ẩn"*).
2. Người chơi bấm "Vào bếp" → `canvasPopupRoot.SetActive(false)` → GameObject của `BoatAnnouncePopupUI` bị deactivate.
3. **Unity giết TẤT CẢ coroutine của MonoBehaviour khi GameObject bị SetActive(false), và KHÔNG chạy lại khi bật lại.** `DrainRoutine` chết giữa chừng ⇒ dòng `:228 _drainRoutine = null` **không bao giờ chạy**.
4. Người chơi ra khỏi bếp. `_drainRoutine` vẫn giữ tham chiếu Coroutine chết (≠ null) ⇒ mọi `OnNextTripScheduled` sau đó chỉ `_hangDoi.Add(...)` rồi **nằm đó mãi mãi**.
5. **Từ giây phút đó tới khi reload scene farm: không còn một popup báo tàu nào hiện ra.** Trong khi GDD §5 edge 6 yêu cầu ngược lại: *"popup thông báo **hoãn tới khi quay lại farm** (queue 1 thông báo mới nhất)"* — tức phải HOÃN rồi HIỆN, không phải MẤT LUÔN.
6. Nếu popup **đang mở** đúng lúc vào bếp thì tệ hơn: `HienPopupRoutine` (`while (_dangHien)`) và `MoAnimRoutine` cùng chết ⇒ `_dangHien` kẹt `true`, card đứng ở scale/alpha dở dang, `FarmInputLock.popupLockCount` lệch +1 (may mắn được `FarmInputLock.OnSceneLoaded → ResetAll()` gỡ hộ — **tình cờ, không phải thiết kế**, xem M-5).

**Cách sửa đề xuất (Dev C, ~8 dòng):**
- Thêm `private void OnEnable()` gọi lại `if (_drainRoutine == null && _hangDoi.Count > 0) _drainRoutine = StartCoroutine(DrainRoutine());`
- Thêm `private void OnDisable() { _drainRoutine = null; if (_dangHien) { FarmInputLock.RegisterPopupClose(); _dangHien = false; if (popupRoot != null) popupRoot.SetActive(false); } }` — reset sạch cờ để `OnEnable` khởi động lại đúng.
- **Sạch hơn nữa:** tách component `BoatAnnouncePopupUI` (nghe event + hàng đợi) ra một GameObject **NGOÀI** `canvasPopupRoot` (ví dụ trên `BoatSystem`), chỉ để phần `popupRoot` (visual) nằm trong canvas. Lúc đó điều kiện `SceneManager.GetSceneByName(cookingSceneName).isLoaded` ở `DuocPhepHien()` (`:243-244`) mới thật sự làm đúng việc "hoãn" của nó — hiện tại nó là code chết vì component đã bị tắt trước rồi.

---

## 3. FINDING — 🟠 MAJOR

### M-1 · Gangplank sai trạng thái khi load save đang `Docked` (race `IsReady`, không bao giờ re-sync)

**File:** `devB/.../Visitors/GangplankController.cs:53-69, 83-94`

```csharp
private void Start() {
    _dockIndex = ResolveDockIndex();
    TrySubscribe();                        // :62  — Instance đã có từ Awake ⇒ LUÔN thành công
    ApplyStateInstant(IsBoatDocked());     // :63  — nhưng IsDocked cần IsReady!
}
private void Update() { if (!_subscribed) TrySubscribe(); }   // :68 — _subscribed đã true ⇒ KHÔNG BAO GIỜ chạy lại
```

**Kịch bản gãy cụ thể:** Sếp tắt game lúc tàu đang đậu (khách còn trên bờ) → mở lại.
1. `BoatDockManager.Awake` đặt `Instance` (chắc chắn trước mọi `Start`), nhưng `IsReady` chỉ bật ở **`Start`** — mà **thứ tự `Start` giữa 2 MonoBehaviour là KHÔNG XÁC ĐỊNH**.
2. Nếu `GangplankController.Start` chạy trước `BoatDockManager.Start`: `IsDocked(i)` → `TryGetPhaseInfo` → `if (!IsReady ...) return false` → **false**.
3. `ApplyStateInstant(false)` → `spriteRenderer.enabled = false` + `localScale.x = 0` → **tấm gỗ biến mất**.
4. `_subscribed` đã `true` nên `Update` không thử lại; và Dev A **cố ý KHÔNG bắn lại `OnBoatDocked`** cho chuyến đã Docked từ phiên trước (chống nhân đôi khách — đúng thiết kế).
5. ⇒ Tàu đậu sát bờ **không có tấm gỗ**, khách của Dev B đi bộ trên mặt nước cho tới khi tàu rời bến.
6. Lỗi **nhấp nháy** (chạy lần được lần không) tuỳ thứ tự script → cực khó debug lúc Sếp test.

**Vi phạm:** AC §8.1 (*"gangplank bật"*) + §8.6 (*"tắt/mở game ở mọi pha"*) + GDD §3.7.

**Cách sửa đề xuất (Dev B, 4 dòng):** trong `Start` đợi `IsReady` như Dev B đã làm đúng ở `TouristVisitorManager.BootRoutine`, hoặc đơn giản nhất — sửa `Update` thành sync định kỳ thay vì chỉ subscribe:
```csharp
private void Update() {
    if (!_subscribed) { TrySubscribe(); return; }
    var mgr = BoatDockManager.Instance;
    if (mgr != null && mgr.IsReady && !_daSyncSauReady) { _daSyncSauReady = true; ApplyStateInstant(IsBoatDocked()); }
}
```

---

### M-2 · MỘT hàng chờ chung 3 bến + chỉ khách đầu hàng chạy đồng hồ ⇒ cận trên rời bến = 18 × 30 phút

**File:** `devB/.../Visitors/TouristQueue.cs` (toàn file — 1 instance dùng chung) · `devB/.../Visitors/TouristAgent.cs:319-330, 409-424`

**Kịch bản gãy cụ thể:** Sếp mở đủ 3 bến (Lv14). 3 tàu cập bến gần nhau (luật so le chỉ ép cách 3 phút), mỗi tàu 6 khách = **18 khách xếp CHUNG 1 hàng**.
- Đồng hồ kiên nhẫn chỉ chạy cho khách **slot 0** (`OpenBubbleIfNeeded` chỉ gọi khi `_isFront`). Khách slot 1..17 có `PatienceEndUtcTicks == 0` ⇒ `TickWaitServe` return ngay ⇒ **đứng chờ vô thời hạn, không tính giờ**.
- Người chơi AFK (đi ngủ / để game chạy nền): khách 0 hết giờ sau 30p → khách 1 lên đầu, bắt đầu ĐẾM MỚI 30p → … ⇒ **khách cuối lên tàu sau ~9 tiếng**.
- Trong suốt 9 tiếng đó **cả 3 bến đều `Docked`**, không chuyến mới, không popup.

**Vi phạm:** AC §8.4 (*"3 bến: các arrival cách nhau ≥3p, **chu kỳ ~10p/bến**"*) — với hàng chung, chu kỳ thực tế có thể là hàng giờ. GDD §3.1 cũng ghi cận trên là *"30p kiên nhẫn + thời gian đi bộ"*, tức GDD giả định kiên nhẫn chạy **song song**, không nối tiếp.

> Đây đồng thời là **câu hỏi mở #2 trong HANDOFF devB** ("1 hàng chung hay 3 hàng riêng?"). QA khuyến nghị Sếp chốt hướng **A**, vì hướng B (3 hàng riêng) vẫn để lại cận trên 6×30p = 3 tiếng/bến.

**Cách sửa đề xuất (chỉ báo cáo — đây là quyết định thiết kế, cần Sếp chốt):**
- **Hướng A (QA khuyến nghị):** đồng hồ kiên nhẫn bắt đầu **lúc khách đặt chân xuống bờ** (kết thúc `Disembark`), không phải lúc mở bubble. Bubble vẫn chỉ mở ở đầu hàng (giữ đúng §3.3 về mặt hình ảnh) nhưng cả chuyến hết kiên nhẫn **song song** ⇒ cận trên rời bến = 30p + đi bộ, đúng GDD §3.1, và AC §8.4 đạt được.
- **Hướng B:** mở bubble cho `N` khách đầu (N = 2–3) để tăng throughput.
- Cả hai hướng đều cần Sếp duyệt vì đụng cảm nhận gameplay.

---

### M-3 · Dồn hàng cướp mục tiêu của khách đang đi bộ ⇒ khách nhảy tới hàng rồi QUAY NGƯỢC ra đường

**File:** `devB/.../Visitors/TouristAgent.cs:218-231` (`OnQueueSlotChanged`)

```csharp
public void OnQueueSlotChanged(int slotIndex, Vector3 slotPos, bool isFront) {
    _slotIndex = slotIndex; _isFront = isFront;
    if (State == WalkingBack || Boarding || Done || Happy || Sad) return;   // ← KHÔNG loại Disembarking / WalkingPath
    SetTarget(slotPos);                                    // :228 — cướp target đang đi
    if (State == AgentState.WaitingServe) SetState(WalkingToSlot);          // state GIỮ NGUYÊN WalkingPath
}
```

**Kịch bản gãy cụ thể:**
1. Khách #3 đang ở pha `WalkingPath`, đi từ `WP_02` tới `WP_03` trên đường đất.
2. Khách #0 được phục vụ xong, rời hàng → `TouristQueue.Remove` báo slot mới cho khách #3 → `SetTarget(slotPos)`.
3. Khách #3 **bỏ ngang đường đất, đi thẳng (xuyên nhà/xuyên nước) tới slot hàng chờ**.
4. Tới nơi, `TickWalkPath` chạy tiếp: `_pathIndex++` → `AdvanceAlongPathOrQueue()` → target = `WP_03` ⇒ **khách quay đầu đi ngược ra đường**, rồi lại quay vào hàng.
5. Với 4–6 khách và hàng dồn liên tục, cả nhóm **đi tới đi lui loạn xạ** trước cửa nhà hàng.

**Vi phạm:** AC §8.1 (*"khách xuống đi **đúng đường đất**"*) + §8.3 (*"hàng tiến lên"*).

**Cách sửa đề xuất (Dev B, 2 dòng):** chỉ nhận slot mới nếu đã ở khu hàng chờ —
```csharp
if (State != AgentState.WaitingServe && State != AgentState.WalkingToSlot) return; // ghi nhận slot, chưa đổi target
SetTarget(slotPos);
SetState(AgentState.WalkingToSlot);
```
(khách đang `WalkingPath` vẫn cập nhật `_slotIndex`/`_isFront`, và khi tới cuối path `AdvanceAlongPathOrQueue()` sẽ tự đi đúng slot mới nhất — logic đã sẵn.)

---

### M-4 · Thưởng cộng cả GIA VỊ vào "Σ giá nguyên liệu" ⇒ lệch công thức GDD §3.4

**File:** `devB/.../Visitors/TouristSmileyFlyFX.cs:203-223` (`TouristRewardCalculator.ComputeGold`)

```csharp
var list = dish.requiredIngredients;
for (int i = 0; i < list.Count; i++) {
    IngredientData ing = list[i];        // :206 — KHÔNG lọc ing.kind
    ...
    tong += gia;
}
```

**Kịch bản gãy cụ thể:** GDD §3.4 ghi `goldReward(dish) = round( Σ FarmItemValue(**nguyên liệu chính** của món) × 2 )`. `IngredientData` có sẵn `public IngredientKind kind` với 2 giá trị `Ingredient` / `Seasoning` (`IngredientData.cs:13`) — tức dự án PHÂN BIỆT rõ. Một món như "Phở bò tái" khai 4 nguyên liệu + 3-4 gia vị (muối/nước mắm/tiêu) → thưởng bị **thổi lên 30–60%** so với con số Sếp đã cân bằng. Với 18 khách/chu kỳ 10 phút, lạm phát vàng tích luỹ nhanh.

**Vi phạm:** GDD §3.4 (công thức Sếp chốt) — đây là lệch **kinh tế**, không phải lệch code.

**Cách sửa đề xuất (Dev B, 1 dòng):**
```csharp
if (ing.kind == IngredientKind.Seasoning) continue;   // GDD §3.4: chỉ nguyên liệu CHÍNH
```
Đồng thời cần lưu ý: nếu bỏ hết mà `tong == 0` (món toàn gia vị) thì phải rơi về fallback, không trả 0 (xem B-3).

---

### M-5 · Input lock + popup treo nửa chừng khi bị `SetActive(false)` — chỉ được cứu NHỜ MAY

**File:** `devC/.../UI/BoatAnnouncePopupUI.cs:256-283, 333-355` · `devC/.../UI/DockPurchasePopupUI.cs:138-181, 343-366`

**Kịch bản gãy cụ thể:** (nửa còn lại của B-4)
- Cả 2 popup chỉ trả `FarmInputLock.RegisterPopupClose()` ở **dòng cuối** của `DongAnimRoutine`. Coroutine đó bị giết khi `canvasPopupRoot.SetActive(false)` (vào bếp) hoặc khi ai đó tắt popup bằng code khác.
- `OnDestroy` có xử lý (`if (_dangHien) RegisterPopupClose()`), nhưng **`OnDisable` thì không** — mà vào bếp là `OnDisable`, không phải `OnDestroy`.
- Hiện tại lỗi **không bùng phát** chỉ vì `FarmInputLock` có `SceneManager.sceneLoaded += OnSceneLoaded → ResetAll()` (`FarmInputLock.cs:34-37`) và scene bếp load additive nên `popupLockCount` bị reset về 0. **Đây là may mắn, không phải thiết kế** — đổi cách vào bếp (không load scene mới, chỉ bật/tắt canvas) là input khoá cứng ngay.
- Ngoài ra khi quay lại farm, card còn ở `localScale` / `dim.alpha` dở dang của tween bị cắt ⇒ popup hiện méo/mờ.

**Cách sửa đề xuất (Dev C, ~6 dòng/popup):** thêm `OnDisable()` đối xứng với `OnDestroy()` cho cả 2 popup — trả lock, hạ cờ `_dangHien`/`_dangMo`, `popupRoot.SetActive(false)`, reset `cardRect.localScale = Vector3.one` và `contentGroup.alpha = 1f`.

---

### M-6 · Dev C bắt tap bằng AABB `SpriteRenderer.bounds` + không kiểm `BlockMapPan` ⇒ kéo bản đồ cũng mở popup mua

**File:** `devC/.../TouristBoatUnlockFlow.cs:370-403` (`Update`) + `:582-606` (`TapDownThisFrame`)

```csharp
if (FarmInputLock.IsPopupOpen) return;          // :375 — có kiểm IsPopupOpen
// ...nhưng KHÔNG kiểm FarmInputLock.BlockMapPan / IsDraggingSeed / IsDraggingSickle
if (!TapDownThisFrame(out screenPos)) return;   // :379 — bắt ở MOUSE-DOWN, không phải click
```

**Kịch bản gãy cụ thể:**
1. Người chơi đặt ngón tay lên bảng khóa bến 2 rồi **kéo để pan bản đồ** → `wasPressedThisFrame` = true ngay tại điểm chạm ⇒ popup mua bến bật lên giữa lúc đang kéo, bản đồ đứng khựng.
2. Đang **kéo hạt giống / kéo liềm** ngang qua bảng khóa (`IsDraggingSeed` / `IsDraggingSickle` = true, nhưng `IsPopupOpen` = false) ⇒ popup mua bến chen ngang thao tác nông trại.
3. `sr.bounds` là **AABB world**, không phải hình sprite — vùng tap là hình chữ nhật bao quanh; Dev C đã ghi nhận (HANDOFF devC rủi ro #3) là chấp nhận được vì bảng không xoay, nhưng cộng với (1)(2) thì tần suất chạm nhầm khá cao.

**Cách sửa đề xuất (Dev C, 2 dòng):** đổi sang bắt ở **mouse-UP kèm kiểm tra không di chuyển quá ngưỡng** (giống `OnMouseUpAsButton` mà Dev B dùng cho khách), và thêm:
```csharp
if (FarmInputLock.BlockMapPan || FarmInputLock.IsDraggingSeed || FarmInputLock.IsDraggingSickle) return;
```
> **Cách sạch nhất vẫn là câu hỏi #2 trong HANDOFF devC**: cho Dev C sửa 1 dòng trong `BoatDockSlot.OnMouseDown` (gọi popup thay `TryUnlockDock`) và bỏ hẳn cơ chế "tắt collider + tự bắt tia". **QA ủng hộ phương án này** — nó xoá luôn M-6 và rủi ro "ai gọi `RefreshLockUI()` là đường mua thẳng V1 sống lại" (HANDOFF devC rủi ro #2).

---

## 4. FINDING — 🟡 minor

| Mã | File : dòng | Mô tả | Kịch bản / Hệ quả | Sửa đề xuất |
|---|---|---|---|---|
| **m-1** | `devA/.../TouristBoatConfig.cs:244` | `OnValidate` kẹp `patienceMinutes = Mathf.Max(1f, …)` | Mâu thuẫn trực tiếp với HANDOFF devB §3 bước 8 ("đặt `patienceMinutes` nhỏ, vd **0.5**"): Inspector tự nhảy về 1. Cộng với B-2, ca AC §8.5 nhanh nhất vẫn mất 1 phút thực/khách | Hạ sàn về `0.05f` (chỉ chống chia 0), hoặc sửa HANDOFF devB |
| **m-2** | `Cooking-Game-2D/.../Editor/TouristBoatDiagnosticTool.cs:355-365` (file CŨ, ngoài phạm vi 3 Dev) | Menu **8 "Xóa Save Tàu"** chỉ xoá `TouristBoat_Unlocked_*` / `_AnchorUtc_*` / `_IntroDone` | Sót `TouristBoat_V2_State/_Anchor/_NextArrival_*`, `TouristBoat_ScheduleVersion`, `TouristTrip_{dock}` (Dev B), `TouristBoat_DaBaoChuyen_{dock}` (Dev C) ⇒ QA "reset save" không sạch, kết quả test lẫn lộn giữa các lần. Menu **7 "Test Ngay"** dùng reflection `_anchorTicks` (`:440`) — field không còn ở V2 ⇒ fail êm (đúng như Dev A cảnh báo) | Lead bổ sung 6 key mới vào danh sách xoá; thay menu 7 bằng `EditorForceDockNow/EditorForceDepartNow` Dev A đã mở sẵn |
| **m-3** | `devA/.../BoatDockManager.cs:551, 669` ↔ `devC/.../BoatAnnouncePopupUI.cs:123-141` | `AnnounceNextTrip` có thể bắn ngay trong `Start` của Dev A (đường `ResetDockSchedule` khi migrate V1→V2 / đồng hồ lùi) | Dev C subscribe trong `Start` của mình — nếu `Start` của Dev A chạy trước thì event rơi vào hư không, và `_announcedArrival` đã đánh dấu nên `AnnounceNextTripIfPending` **không bắn lại** ⇒ mất popup "tàu vào sau 30 giây" ở lần đầu chơi save cũ (đúng lúc cần nhất) | Dev A: đưa `AnnounceNextTrip` phát sinh trong `Start` vào cùng cơ chế hoãn 2 frame như `_pendingDockedEvent` |
| **m-4** | `devB/.../TouristRequestBubble.cs:208-239` | `GetPlaceholder(Color tint)` **bỏ qua hoàn toàn tham số `tint`** — luôn trả circle TRẮNG cache chung | Chưa có art: bubble món, **mặt cười** và **mặt buồn** trông y hệt nhau (3 hình tròn trắng) ⇒ **không nghiệm thu được AC §8.5** ("icon buồn") và §8.2 ("mặt cười") bằng mắt | Cache 3 sprite theo màu (`Dictionary<Color,Sprite>`), hoặc đơn giản: đặt `sr.color = tint` ở nơi gọi |
| **m-5** | `devB/.../TouristSmileyFlyFX.cs:47-51` | `Spawn` log `LogWarning` **mỗi lần** thiếu sprite mặt cười | Trái cam kết HANDOFF devB §6 ("cảnh báo **đúng 1 lần**, không spam Console"). 18 khách/chu kỳ ⇒ Console ngập, che lỗi thật | Thêm `static bool _warned` |
| **m-6** | `devC/.../UI/DockPurchasePopupUI.cs:373-374` | `CultureInfo.GetCultureInfo("vi-VN")` | Ném `CultureNotFoundException` nếu Player Settings bật **Invariant Globalization** (hay gặp khi build IL2CPP mobile để giảm size). Rủi ro **kế thừa** — `BoatDockSlot.FormatVN` V1 đã làm y hệt, nên không phải lỗi mới | Dùng `NumberFormatInfo` tự dựng (`NumberGroupSeparator = "."`) — nên sửa CẢ 2 chỗ một lượt |
| **m-7** | `devC/.../UI/DockUnlockCelebrationFX.cs:280-283` | `_spriteSao` (static) **không đặt `hideFlags`**; texture nền cũng vậy | Rò rỉ nhẹ giữa các lần Play trong Editor (Dev B đã làm đúng ở `TouristRequestBubble.cs:221, 236` — nên đồng bộ) | `tex.hideFlags = _spriteSao.hideFlags = HideFlags.HideAndDontSave;` |
| **m-8** | `devB/.../TouristVisitorManager.cs:688-695` | `CheckAllAboard` xoá save + `_trips[dock] = null` **TRƯỚC** khi gọi `ReportVisitorsAllAboard` | Nếu Dev A từ chối lệnh (state ≠ `Docked` — xảy ra khi lịch vừa bị reset vì đồng hồ lùi), chuyến biến mất khỏi RAM lẫn đĩa nhưng tàu vẫn `Docked`; `OnBoatDocked` không bắn lại ⇒ **không còn đường phục hồi trong phiên** (phải reload scene) | Chỉ xoá save/`_trips` SAU khi `ReportVisitorsAllAboard` xác nhận thành công (Dev A nên đổi hàm này thành `bool`) |
| **m-9** | `devB/.../TouristAgent.cs:206, 313` | `FaceCardinal(Vector2.up)` cứng — "quay mặt về nhà hàng" | Giả định nhà hàng cooking luôn nằm phía **TRÊN** hàng chờ. `queueDirection` mặc định là chéo xuống-phải; nếu Sếp kéo `QueueAnchor` sao cho nhà hàng ở dưới/bên thì cả hàng quay lưng vào cửa | Suy hướng từ `queue.transform.position - transform.position`, hoặc thêm field `Vector2 huongNhinKhiCho` trên `TouristQueue` |
| **m-10** | `devB/.../GangplankController.cs:71-79` | `OnDestroy` unsubscribe theo `BoatDockManager.Instance` **hiện tại**, không theo instance đã subscribe | Nếu manager bị thay (reload scene, 2 `BoatSystem`), gỡ nhầm/không gỡ. Vô hại hôm nay (Dev B/C khác đã giữ ref `_manager` đúng cách) nhưng nên đồng bộ | Cache `private BoatDockManager _mgr;` lúc subscribe, unsubscribe theo `_mgr` |
| **m-11** | `devB/.../TouristVisitorSetupTool.cs:56` (prio 20) ↔ `devC/.../TouristBoatUIPopupSetupTool.cs:73` (prio 20) | 2 `MenuItem` khác tên nhưng **cùng priority 20** trong nhánh `Tools/Farm Game/Tourist Boat/` | Thứ tự hiển thị menu không xác định giữa các lần Unity reload domain — Sếp dễ bấm nhầm tool. Không gãy build | Lead đánh lại prio: A=12, B=20/21/22, C=30 |

---

## 5. ĐÃ TỰ SỬA

### ⚠️ KHÔNG sửa một dòng nào trong `/home/user/work/deliver/`.

Toàn bộ kiểm khớp contract (mục 1) và kiểm API ngoài (mục 2) **đều PASS** — không tìm thấy bất kỳ lỗi "sai tên API / typo nhỏ chắc chắn" nào để sửa theo quyền hạn mục 5 của brief. Ba gói code đi qua compile-check thật với **0 error, 0 warning** ở cả 3 pass. Mọi finding ở trên đều là **logic/thiết kế** ⇒ theo đúng luật, QA chỉ báo cáo.

**Thay đổi duy nhất đã thực hiện, và nó nằm NGOÀI thư mục giao:**

| # | File | Dòng | Sửa gì | Vì sao |
|---|---|---|---|---|
| 1 | `/home/user/work/qa/build/devC/.../Editor/TouristBoatUIPopupSetupTool.cs` (**bản sao chỉ để build**, không phải bản giao) | 531 | `batKy ??= f;` → `if (batKy == null) batKy = f;` | `??=` là cú pháp **C# 8**. `mcs 6.8` của môi trường QA chỉ tới **C# 7.2** nên không dịch được. **Unity của Sếp dùng C# 9 ⇒ dòng gốc chạy bình thường, KHÔNG cần sửa file giao.** Ghi ở đây cho minh bạch: đó là hạn chế của công cụ QA, không phải lỗi của Dev C. |

Hạ tầng QA đã dựng (tái sử dụng cho các story sau):
```
/home/user/work/qa/
├─ stubs/UnityStub.cs        · stub UnityEngine + TMPro + UI + InputSystem + Rendering
├─ stubs/UnityEditorStub.cs  · stub UnityEditor + UnityEditor.Animations
├─ stubs/ProjectStub.cs      · stub các type CÓ THẬT trong project nhưng thiếu trong source drop
│                              (LuuGopPrefs · AudioManager · TutorialManager · PopupManager ·
│                               CameraController · MarketPriceTable · SceneTransitionManager · …)
├─ compile.sh        · pass 1 — 3 gói Dev + source thật, có UNITY_EDITOR
├─ compile_player.sh · pass 2 — giả lập player build (bỏ Editor/)
└─ compile_full.sh   · pass 3 — mô phỏng MERGE THẬT (+ 2 Editor tool V1 của project)
```

---

## 6. CHECKLIST PLAY MODE *(bản vòng 1 — ĐÃ THAY THẾ bởi §7.8, giữ để đối chiếu)*

> Chạy **sau khi Dev sửa xong 4 mục BLOCKING**. Ô ❌ = ca hiện tại chắc chắn TRƯỢT, dùng để verify bản fix.

### 6.1 Chuẩn bị (làm 1 lần, theo đúng thứ tự)

- [ ] Copy `devA/` → project **TRƯỚC** (Dev B/C cần field config V2, không có là gãy compile).
- [ ] Copy `devB/`, `devC/`. Đợi Unity compile → **Console phải 0 lỗi đỏ**.
- [ ] Mở `TouristBoatConfig.asset` → điền tay **12 field mới** theo bảng HANDOFF devA §2 (Unity deserialize YAML cũ có thể để 0).
- [ ] Chạy `Tools/Farm Game/Tourist Boat/Setup NPC Animations` → dialog báo **11/11** nhân vật OK.
- [ ] Chạy `Tools/Farm Game/Tourist Boat/Setup Tourist Visitors (Scene)` trong `SCN_Farm`.
- [ ] Chạy `Tools/Farm Game/Tourist Boat/Setup Popups (UI)` → Ctrl+S.
- [ ] Chạy `Tools/Farm Game/Tourist Boat/Dịch bến sát bờ` → **Tự suy hướng bờ** → ÁP DỤNG 3 bến → **nhìn scene chỉnh tay** (REVIEW) → chạy menu `10. Canh Tau Vao O Dau`.
- [ ] **REVIEW bắt buộc:** kéo `WP_01..04` của `TouristPath_Dock01/02/03` bám đường đất; kéo `QueueAnchor` ra trước cửa nhà hàng cooking; canh `Gangplank` từng bến. Ctrl+S.
- [ ] Kiểm `TouristSystem/TouristVisitorManager` Inspector: `queue` ✅ · `touristPrefabs` = 11 ✅ · `dishDatabase` = **38** ✅ · `dockPathRoots[3]` ✅ · `gangplanks[3]` ✅. **Ô nào trống là dừng lại, chạy lại tool** (xem B-1).

### 6.2 Vòng lặp cơ bản — AC §8.1 → §8.3

| # | Việc làm | Phải thấy | AC |
|---|---|---|---|
| 1 | Lên Lv10 (hoặc `ForceSetLevelExp`) | Intro 4 câu chạy 1 lần → camera lia ra bến 1 → tàu 01 chạy vào **sát bờ** | §8.1 |
| 2 | Nhìn lúc tàu chạm bến | **Gangplank bắc xuống** (0,4s), 3–6 khách xuống **lần lượt cách 0,8s** | §8.1 |
| 3 | Theo dõi khách đi bộ | Đi **bám đường đất** theo WP, **không đi tắt, không quay đầu** | §8.1 · **M-3** |
| 4 | Khách tới nhà hàng | Xếp hàng **thẳng, đều `queueSpacing`**, **chỉ khách đầu hàng** mở bubble (scale-in mượt) | §8.1 · §8.2 |
| 5 | Xem món trong bubble | Món **luôn thuộc tập đã unlock** ở level hiện tại; các khách trong 1 chuyến **không trùng món** | §8.2 |
| 6 | Chưa nấu mà tap khách | Hint *"Chưa có \<tên món\> trong kho — vào bếp nấu nhé!"*, **kho không đổi** | §3.3 |
| 7 | Nấu món đó → đưa vào kho → tap khách | **Ghi số vàng/EXP trước và sau.** Kho **−1 đúng món đó**; vàng **+Σ giá nguyên liệu CHÍNH ×2** (không tính gia vị — **M-4**); EXP **+`dish.rewardExp`**; mặt cười bay lên HUD nhỏ→to→fade | §8.2 · **B-3 · M-4** |
| 8 | ❌ **Ca mất món:** tìm 1 `DishData` có `sellPrice = 0` **và** `requiredIngredients` rỗng, ép khách gọi món đó rồi giao | **PHẢI**: hoặc được thưởng > 0, hoặc món được **HOÀN LẠI KHO** + hint báo lỗi. **KHÔNG ĐƯỢC** mất món mà 0 vàng | **B-3** |
| 9 | Khách được phục vụ | Đi về tàu, **cả hàng dồn lên 1 slot**, bubble khách kế mở | §8.3 |
| 10 | Khách cuối lên tàu | **Gangplank rút** → tàu lùi rời bến (không đợi hết 40 phút như V1) | §8.3 |
| 11 | Ngay sau đó | Popup *"Tàu số 01 sẽ cập bến sau 5 phút!"* — khung gỗ bo góc, dim đen 60%, nút **"Đã rõ"**, hiện **đúng 1 lần** | §3.5 |

### 6.3 Lịch tàu — AC §8.4

| # | Việc làm | Phải thấy |
|---|---|---|
| 12 | 1 bến mở, bấm đồng hồ từ lúc tàu rời bến | Tàu cập bến lại sau **đúng 5 phút ±5 giây** |
| 13 | Mua bến 2 (Lv12 + 2.000 vàng) và bến 3 (Lv14 + 25 gem) | Tap bảng khóa → **popup mua** (không mua thẳng); đủ điều kiện → trừ tiền, popup đóng, **sao vàng nổ + SFX**, tàu xuất phát ngay |
| 14 | Thiếu tiền | Nút MUA **xám** + dòng đỏ *"Không đủ vàng"*; **nhận vàng lúc popup đang mở → nút tự sáng** (live) |
| 15 | 3 bến cùng chạy | Mọi cặp arrival **cách nhau ≥ 3 phút**; chu kỳ **~10 phút/bến** | ❌ **M-2** |
| 16 | ❌ Để 3 bến cùng đông khách (18 khách) rồi **AFK 15 phút** | Tàu vẫn phải **rời bến trong ~30–35 phút**, không phải hàng giờ | ❌ **M-2** |

### 6.4 Kiên nhẫn — AC §8.5

| # | Việc làm | Phải thấy |
|---|---|---|
| 17 | Đặt `debugTimeScale = 60`, Play | Lịch tàu chạy nhanh **VÀ** kiên nhẫn khách cũng nhanh theo (30 phút game ≈ 30 giây thực) | ❌ **B-2** |
| 18 | Để khách đầu hàng hết kiên nhẫn | Bubble đổi **icon BUỒN** (phân biệt rõ với mặt cười — ❌ **m-4** nếu chưa có art), giữ 2s, khách về tàu, **không cộng vàng/EXP** |
| 19 | Kiểm sau đó | Hàng dồn lên, khách kế mở bubble, tàu vẫn rời bến bình thường |

### 6.5 Tắt/mở game & scene bếp — AC §8.6 (ca dễ gãy nhất)

| # | Việc làm | Phải thấy |
|---|---|---|
| 20 | Tắt game lúc tàu **đang chạy vào** (Arriving) → mở lại | Tàu ở đúng vị trí theo giờ UTC, cập bến đúng lịch, **khách spawn ĐÚNG 1 LẦN** |
| 21 | Tắt game lúc tàu **đang Docked, khách đang xếp hàng** → mở lại | **Gangplank vẫn BẮC** (❌ **M-1** — thử **5 lần liên tiếp**, lỗi này nhấp nháy theo thứ tự script); khách được đặt **thẳng vào slot hàng chờ**; **KHÔNG nhân đôi khách** |
| 22 | Tắt game ở bước 21, **chờ hơn 30 phút thực** rồi mở lại | Khách quá hạn resolve **TimedOut ngay**, tàu rời bến, chuyến kế được lên lịch — **không kẹt** |
| 23 | Tắt game lúc tàu **đang rời bến** (Departing) → mở lại | Tua đúng: Departing → WaitingNext → (nếu đã quá giờ) Docked, **JustDocked bắn đúng 1 lần** |
| 24 | ❌ **Ca popup chết:** tàu rời bến (popup hiện) → bấm "Đã rõ" → **vào bếp** → quay ra farm → đợi chuyến kế rời bến | Popup báo tàu **PHẢI hiện lại**. Nếu im lặng vĩnh viễn ⇒ **B-4 chưa fix** |
| 25 | ❌ Đang mở popup báo tàu thì **vào bếp** ngay | Ra khỏi bếp: popup hiện **nguyên vẹn** (không méo scale, không mờ), bấm "Đã rõ" đóng bình thường, **input world không bị khoá cứng** | ❌ **B-4 · M-5** |
| 26 | Vào bếp lúc tàu đang đậu, nấu vài món rồi ra | Kiên nhẫn khách **vẫn trôi theo UTC** (đúng thiết kế); tàu có thể đã rời bến — **không được kẹt, không NRE** |
| 27 | Chỉnh đồng hồ máy **lùi 2 tiếng** khi đang chơi | Console `LogWarning` "Reset lịch bến … đồng hồ máy chỉnh lùi", tàu cập bến lại sau ~30 giây, **không kẹt** |

### 6.6 Chất lượng & hiệu năng — AC §8.7

- [ ] **Console 0 lỗi đỏ** trong toàn bộ phiên test (cảnh báo vàng về art placeholder là chấp nhận được).
- [ ] Ép 18 khách đồng thời (3 bến × 6) → **giữ 60fps** (Stats/Profiler). Lưu ý: hệ khách hiện **vẫn chạy full `Update` khi ở scene bếp** — cân nhắc thêm `TouristVisitorManager` vào `behavioursToDisableInCooking` của `FarmUIManager`.
- [ ] Khách **không bị decor che** (sorting `CongTrinh` + `baseSortingOrder 5000`, đã kẹp Y-sort ±8000).
- [ ] Chữ tiếng Việt trên 2 popup + teaser bảng khóa **có đủ dấu** (nếu mất dấu: đổi `Font Asset` sang font có dấu — HANDOFF devC §4 bước 4).
- [ ] Chạy `Tools/Farm Game/Tourist Boat/Xóa Tourist Visitors (Undo)` → không sót object rác; Ctrl+Z gỡ sạch được cả 3 tool.

---

## 6.7 TÓM TẮT CHO LEAD *(vòng 1 — đã xử lý xong, xem §7.2)*

| Ưu tiên | Mã | Ai sửa | Ước lượng |
|---|---|---|---|
| 1 | **B-3** mất món không đền | Dev B | 15 phút |
| 2 | **B-4** popup chết sau khi vào bếp | Dev C | 20 phút |
| 3 | **B-1** tàu kẹt khi thiếu `TouristQueue` (+ lưới an toàn timeout chuyến) | Dev B (+ Sếp chốt câu hỏi #1 HANDOFF devA) | 30 phút |
| 4 | **B-2** `debugTimeScale` cho patience | Dev A mở API + Dev B dùng | 15 phút |
| 5 | **M-1** gangplank sai khi load save Docked | Dev B | 10 phút |
| 6 | **M-3** khách quay đầu khi dồn hàng | Dev B | 5 phút |
| 7 | **M-4** thưởng cộng cả gia vị | Dev B | 1 dòng |
| 8 | **M-5** `OnDisable` cho 2 popup | Dev C | 10 phút |
| 9 | **M-2** hàng chờ / kiên nhẫn nối tiếp | **Cần Sếp chốt hướng trước** | — |
| 10 | **M-6** + toàn bộ `m-*` | gộp vào lượt sửa | — |

**3 câu hỏi cần Sếp/Lead chốt trước khi Dev sửa:**
1. **Lưới an toàn "đậu quá X giờ thì tự rời bến"** (HANDOFF devA câu hỏi #1) — QA đề nghị **CÓ**, đây là phần fix của B-1.
2. **Kiên nhẫn song song hay nối tiếp** (M-2 / HANDOFF devB câu hỏi #2) — QA đề nghị **bắt đầu đếm 30 phút từ lúc khách xuống bờ**, giữ nguyên "chỉ đầu hàng mở bubble".
3. **Cho Dev C sửa 1 dòng trong `BoatDockSlot.OnMouseDown`** (HANDOFF devC câu hỏi #2) — QA đề nghị **CHO**, xoá luôn M-6 và rủi ro collider bật lại.

---
---

# 7. VÒNG 2 — REGRESSION (2026-08-29, sau khi 3 Dev sửa)

> Phạm vi: verify **từng** finding vòng 1 bằng cách **đọc code thật** (diff từng file so với snapshot vòng 1 tại `qa/build/`), không tin lời khai trong HANDOFF; soi 3 vùng ghép nối mới; audit key của `TouristBoatDiagnosticTool`; chạy lại 3 pass compile + bộ test console.
>
> **2 quyết định của Sếp được dùng làm chuẩn nghiệm thu mới** (khác spec gốc §3.3):
> ① bubble mở lần lượt cho **mọi** khách (stagger 0.4s), kiên nhẫn 30 phút chạy **song song**, tap giao được **không cần đúng thứ tự hàng**;
> ② hết kiên nhẫn = **MẶT TỨC GIẬN (Angry)**, không phải mặt buồn.

---

## 7.0 VERDICT VÒNG 2

# ✅ SHIP — kèm 2 việc chặn trước khi bấm copy (≈5 phút, KHÔNG cần Dev sửa code)

Cả **4 BLOCKING** và **6 MAJOR** của vòng 1 đều **RESOLVED thật trong code**, không phải sửa hình thức: mỗi lỗi được vá đúng nguyên nhân gốc, và phần lớn còn được bọc thêm lưới an toàn. Compile **0 error / 0 warning** ở cả 3 pass, lần này chạy trên **file giao NGUYÊN BẢN** (vòng 1 phải patch tạm 1 dòng `??=`; Dev C đã bỏ dòng đó). Test console **119 PASS / 0 FAIL** (vòng 1: 98).

Hai việc chặn còn lại **không phải lỗi code của Dev**, mà là 1 con số config và 1 cái tên API mà QA **không có đủ dữ liệu để tự xác nhận**:

| | Việc | Ai làm | Thời gian |
|---|---|---|---|
| **B-5** | Xác nhận `TutorialManager` có **`public static bool IsTutorialDone`**. Sai tên ⇒ **console đỏ, không compile**; đúng tên nhưng sai NGHĨA ⇒ **không mua được bến 2/3, im lặng hoàn toàn** | Lead mở `TutorialManager.cs` | 30 giây |
| **M-7** | Đặt `maxDockMinutes = 35` (KHÔNG phải 30 như tooltip khuyên) | Sếp, lúc điền 13 field config | 5 giây |

Làm 2 việc đó thì **SHIP**. Nếu B-5 sai tên → chuyển **FIX FIRST** (Dev C sửa 1 dòng, 2 phút).

---

## 7.1 BẢNG SỐ LIỆU — VÒNG 1 vs VÒNG 2

| Chỉ số | Vòng 1 | Vòng 2 | |
|---|---:|---:|---|
| **Pass 1** — 3 Dev + source thật + stub, `-define:UNITY_EDITOR` | 0 err / 0 warn | **0 err / 0 warn** | 33 file |
| **Pass 2** — giả lập player build (bỏ `UNITY_EDITOR` + bỏ `Editor/`) | 0 err / 0 warn | **0 err / 0 warn** | 28 file |
| **Pass 3** — mô phỏng MERGE THẬT (+ tool V1 `TouristBoatSetupTool`) | 0 err / 0 warn | **0 err / 0 warn** | 34 file |
| Phải patch tạm file giao để compile? | có (1 dòng `??=`, C#8) | **KHÔNG** — chạy trên bản giao nguyên vẹn | ✅ |
| **Test console Dev A** (`mcs` + `mono`) | 98 PASS / 0 FAIL | **119 PASS / 0 FAIL** `exit=0` | +21 assert nhóm **H** (lưới an toàn) |
| File .cs của 3 Dev | 19 | **21** (+`TouristBoatDiagnosticTool` của A, +`BoatDockSlot` của C) | |
| Tổng dòng code 3 Dev | 9.190 | **11.161** (+21%) | |
| BLOCKING mở | 4 | **1** (B-5 — cần Lead xác nhận, không phải lỗi Dev) | |
| MAJOR mở | 6 | **1** (M-7 — 1 con số config) | |
| minor mở | 11 | **4** (đều là ghi nhận, không chặn) | |

Script tái lập: `qa/compile.sh` · `qa/compile_player.sh` · `qa/compile_full.sh` · `qa/srcset.sh` · stub ở `qa/stubs/`.
Bộ test: `mcs -out:/tmp/t.exe devA/.../BoatScheduleCore.cs devA/tests/unit/touristboat/BoatScheduleCoreTests.cs && mono /tmp/t.exe`

> **Lưu ý về pass 3:** `BoatDockSlot.cs` và `TouristBoatDiagnosticTool.cs` giờ do Dev C / Dev A ship, nên pass 3 **không** nạp bản gốc trong project nữa (tránh trùng class) — chỉ còn `TouristBoatSetupTool.cs` là tool V1 nguyên vẹn. Nó vẫn compile sạch ⇒ **API V1 vẫn được giữ trọn sau vòng 2**.

---

## 7.2 TRẠNG THÁI TỪNG FINDING VÒNG 1

### 🔴 BLOCKING

| Mã | Trạng thái | Bằng chứng trong code (đã đọc, không tin HANDOFF) |
|---|---|---|
| **B-1** tàu kẹt vĩnh viễn | ✅ **RESOLVED — 4 lớp** | ① `TouristVisitorManager.EnsureSceneRefs:1021` — `queue == null` giờ **tự dựng** `QueueAnchor(Auto-Fallback)` + `LogError`, không còn đi tiếp với null. ② **Nguyên nhân gốc bị xoá**: `TouristAgent.EnterWaitServe:383` gọi `TakeBubbleStaggerDelay` cho **mọi** khách ⇒ khách nào cũng có `PatienceEndUtcTicks`, không còn phụ thuộc `_isFront` (đây là điều kiện làm khách 1..n treo vĩnh viễn). ③ Dev A `BoatDockManager.UpdateDockTimeout:~250` — quá `maxDockMinutes` thì bắn `OnDockTimeoutForced` + tự `BeginDeparture(forced:true)` sau 3s. ④ Dev B `WatchdogRoutine:266` — mỗi 5s, chuyến sống quá `patience + 10 phút` thì `ForceEndTrip`. **Cắt bất kỳ 1 lớp nào hệ vẫn thoát kẹt.** |
| **B-2** `debugTimeScale` không áp cho khách | ✅ **RESOLVED** | `TouristVisitorManager.EffectiveTimeScale:163` (public) — công thức **giống hệt từng ký tự** Dev A. Đã chia scale ở: kiên nhẫn (`TouristAgent.OpenBubbleIfNeeded:496`, chia **lúc ĐẶT mốc** ⇒ mốc UTC persist vẫn đúng sau tắt/mở game — đúng khuyến nghị QA), `disembarkInterval` (`DisembarkRoutine:461`), nhịp bubble (`TakeBubbleStaggerDelay:685`), watchdog (`PatienceSecondsScaled:955`). Dev A cũng chia cho lưới an toàn (`EffectiveMaxDockSeconds:803`). |
| **B-3** mất món không đền | ✅ **RESOLVED — mạnh hơn đề xuất QA** | `DeliverTo:701` đảo đúng thứ tự: tính thưởng → kiểm `vang > 0 && eco != null && tien != null` → **mới** `RemoveItem` → cộng thưởng. Thiếu điều kiện thì **không đụng kho** (an toàn hơn cả cách "hoàn món" QA đề xuất — không cần `AddItem`, miễn nhiễm luôn edge kho đầy §5.3). Thêm: `TouristRewardCalculator.ComputeGold:256` có **sàn 1 vàng** và `GiaFallback:268` rơi tiếp xuống `BasePriceBook.DefaultBasePrice` khi `sellPrice = 0` ⇒ `vang <= 0` **không thể xảy ra** ⇒ không sinh soft-lock "không giao được món". |
| **B-4** popup chết sau khi vào bếp | ✅ **RESOLVED — sửa tận gốc** | Không chỉ vá `OnEnable/OnDisable` (đã có: `BoatAnnouncePopupUI:187/200`) mà **đổi hẳn kiến trúc**: tool dựng canvas RIÊNG `Canvas_TouristBoatPopup` ở gốc scene (`TouristBoatUIPopupSetupTool.TimHoacTaoCanvasRieng:470`) — **không** nằm dưới `FarmUIManager.canvasPopupRoot` nên `EnterCookingMode()` không giết coroutine nữa. Popup tự ẩn khi ở bếp bằng `Update` poll 0.5s (`:220`) + `DuocPhepHien` vẫn chặn `DangTrongSceneBep`. Hàng đợi **được giữ** ⇒ đúng GDD §5 edge 6 "HOÃN rồi hiện lại". Tool còn có `ChuyenPopupCuSangCanvasRieng` để di dời scene đã chạy bản tool cũ. |

### 🟠 MAJOR

| Mã | Trạng thái | Bằng chứng |
|---|---|---|
| **M-1** gangplank sai khi load save Docked | ✅ **RESOLVED** | `GangplankController.Update:80` — không còn "subscribe 1 lần rồi thôi": chờ `mgr.IsReady` mới **chốt** trạng thái (`_daChotSauReady`), sau đó **tự re-sync mỗi frame** khi `_extended != mgr.IsDocked(...)`. `IsBoatDocked:228` cũng thêm `mgr.IsReady`. Hết hẳn race thứ tự `Start`, và còn tự sửa nếu lỡ event vì bị `SetActive(false)`. |
| **M-2** hàng chờ nối tiếp ⇒ 18×30 phút | ✅ **RESOLVED theo quyết định ① của Sếp** | Kiên nhẫn giờ chạy **song song**: mọi khách vào `EnterWaitServe` → xin lượt bubble (stagger 0.4s) → đặt `PatienceEndUtcTicks` ngay. Cận trên rời bến = **30 phút + đi bộ**, đúng GDD §3.1, AC §8.4 đạt được. `TouristQueue` hạ xuống thuần vị trí (`isFront` chỉ còn để debug). `CanReceiveDish:141` bỏ điều kiện `_isFront` ⇒ tap khách nào cũng giao được. |
| **M-3** khách quay đầu ngược waypoint | ✅ **RESOLVED** | `OnQueueSlotChanged:257` — `if (State != WaitingServe && State != WalkingToSlot) return;` (chỉ ghi nhận slot, **không** đổi target khi đang `Disembarking`/`WalkingPath`). Đúng cách QA đề xuất. **Bonus ngoài yêu cầu**: `LanePoint:549` cho luồng khách về **lệch làn 26 unit** để 2 luồng không đi xuyên nhau. |
| **M-4** thưởng cộng cả gia vị | ✅ **RESOLVED** | `TouristRewardCalculator.ComputeGold:226` — `if (ing.kind == IngredientKind.Seasoning) continue;`. Có đếm `soNguyenLieuChinh`; món toàn gia vị rơi về fallback thay vì trả 0. |
| **M-5** input lock treo khi popup bị tắt | ✅ **RESOLVED** | Cả 2 popup có `OnDisable` → `TraTrangThaiVePhongThu()` (`BoatAnnouncePopupUI:200/213`, `DockPurchasePopupUI:~126/~140`): trả `FarmInputLock.RegisterPopupClose()`, hạ cờ, ẩn popup, **reset `cardRect.localScale` + `contentGroup.alpha` + dim** (tween bị cắt không còn để lại card méo/mờ). Không còn trông chờ `FarmInputLock.ResetAll` của `sceneLoaded`. |
| **M-6** tap bảng khóa ở mouse-down + AABB | ✅ **RESOLVED — theo phương án QA khuyến nghị** | Lead duyệt sửa `BoatDockSlot.cs`. Nay: `OnMouseDown:114` chỉ **ghi nhận** nhịp nhấn; `OnMouseUpAsButton:141` mới hành động, có **ngưỡng kéo 24px** + chặn `BlockMapPan / IsDraggingSeed / IsDraggingSickle / IsPopupOpen` ở **cả hai** đầu. Dùng collider thật (không còn AABB `sr.bounds`). `TouristBoatUnlockFlow` đã **xoá sạch** `VoHieuTapMuaTrucTiep()` + `Update()` bắn tia ⇒ **một đường tap duy nhất**, hết rủi ro "ai gọi `RefreshLockUI()` là đường mua thẳng sống lại". |

### 🟡 minor

| Mã | Trạng thái | Ghi chú |
|---|---|---|
| m-1 `patienceMinutes` sàn 1 phút | ⚪ **ĐÓNG theo quyết định ④ của Sếp** | Giữ sàn 1. Không còn là vấn đề vì B-2 đã fix: test nhanh giờ dùng `debugTimeScale`, không cần hạ `patienceMinutes`. |
| m-2 tool chẩn đoán xoá thiếu key | ✅ **RESOLVED — đã audit đối chiếu, xem §7.5** | 9/9 nhóm key khớp chính xác nguồn sự thật. |
| m-3 mất popup "vào sau 30 giây" | ✅ **RESOLVED** | `QuetChuyenChuaBao:150`. Xem phân tích tính đúng đắn ở §7.4-c. |
| m-4 placeholder 3 mặt giống hệt nhau | ✅ **RESOLVED** | `TouristRequestBubble.GetPlaceholderFace` + `enum FaceKind{Plain,Happy,Angry}` — vẽ procedural **tròn trắng / vàng cười / ĐỎ cau mày**. Nghiệm thu AC §8.2/§8.5 bằng mắt được ngay khi chưa có art. |
| m-5 spam warning thiếu sprite | ✅ **RESOLVED** | `TouristSmileyFlyFX._warnedNoSprite` — in đúng 1 lần/phiên. |
| m-6 `CultureInfo("vi-VN")` | ✅ **RESOLVED — cả 2 chỗ** | `NumberFormatInfo` tự dựng trong **`DockPurchasePopupUI`** *và* **`BoatDockSlot`** (sửa luôn nợ kỹ thuật V1). Không còn `CultureNotFoundException` khi bật Invariant Globalization. |
| m-7 `_spriteSao` thiếu `hideFlags` | ✅ **RESOLVED** | `DockUnlockCelebrationFX:259` (texture) + `:287` (sprite) đều `HideAndDontSave`. |
| m-8 xoá save trước khi Dev A nhận lệnh | ✅ **RESOLVED** | `TryFinishTrip:890` — báo trước, kiểm `mgr.IsDocked(dock)`; bị từ chối thì đặt `PendingReport` và **giữ chuyến** cho watchdog thử lại mỗi 5s. |
| m-9 `FaceCardinal(Vector2.up)` cứng | ✅ **RESOLVED** | Thay bằng `FaceTowardQueue()` — suy hướng từ vị trí hàng chờ. |
| m-10 gangplank unsubscribe sai instance | ✅ **RESOLVED** | `GangplankController._mgr` cache đúng instance đã gắn. |
| m-11 trùng menu priority 20 | ✅ **RESOLVED** | Nay duy nhất: A=12 · B=20/21/22 · C=30 · tool chẩn đoán=60/61/62. |

---

## 7.3 FINDING MỚI Ở VÒNG 2

### 🔴 B-5 · `TutorialManager.IsTutorialDone` — tham chiếu **static** không kiểm chứng được (rủi ro compile đỏ + chặn mua bến im lặng)

**File:** `devC/.../UI/BoatAnnouncePopupUI.cs:494`

```csharp
public static bool TutorialDangChay()
{
    return TutorialManager.Instance != null && !TutorialManager.IsTutorialDone;
}
```

**Vì sao QA không tự xác nhận được:** `TutorialManager.cs` **không có** trong bản source drop. Toàn bộ chứng cứ về nó trong drop chỉ là 3 dòng gọi *instance* method:
`FarmUIManager.cs:287 NotifySickleShown()` · `:348/:380 NotifySeedPanelOpened()`. **Không có dòng nào nhắc `IsTutorialDone`.** Dev C dẫn nguồn `MissionHudButtonUI.cs:131` — file đó cũng không có trong drop. Stub QA phải **giả định** member này tồn tại để compile chạy được ⇒ **pass 1/2/3 KHÔNG chứng minh dòng này đúng.**

**Vòng 1 chỗ này dùng reflection** (dò tên rồi cache, không thấy thì coi như tutorial không chạy) — **hỏng thì thoái hoá êm**. Vòng 2 đổi sang gọi thẳng static ⇒ **hỏng thì gãy cứng**. Đây là đánh đổi Dev C làm theo yêu cầu QA, nên QA phải chỉ ra cái giá của nó.

**2 kịch bản gãy:**

① **Sai TÊN / sai kiểu / là instance member chứ không static** ⇒ `error CS0117: 'TutorialManager' does not contain a definition for 'IsTutorialDone'` ⇒ **Unity console đỏ, toàn bộ assembly không compile, cả 3 gói Dev chết theo.** Đây là lỗi chặn merge nặng nhất có thể có.

② **Đúng tên nhưng SAI NGHĨA.** `!IsTutorialDone` = *"tutorial CHƯA HOÀN THÀNH"*, rộng hơn hẳn *"tutorial ĐANG chạy"* của vòng 1. Nếu tutorial của dự án kết thúc muộn hơn Lv10 (bến 1 mở ở Lv10, bến 2 ở Lv12, bến 3 ở Lv14):
- `BoatAnnouncePopupUI.DuocPhepHien():333` luôn `false` ⇒ **không popup báo tàu nào hiện ra**, tất cả nằm trong hàng đợi.
- `DockPurchasePopupUI.MoChoBen():201` `return` ngay ⇒ tap bảng khóa bến 2/3 **không làm gì cả**. Và vì `BoatDockSlot.OnMouseUpAsButton:161` thấy `_popupMua != null` nên nó `return` luôn, **không rơi xuống đường V1 dự phòng** ⇒ **không mua được bến, không một dòng log, không phản hồi nào trên màn hình.** Cực khó chẩn đoán.

**Việc phải làm trước khi copy (30 giây):** mở `TutorialManager.cs`, tìm `IsTutorialDone`.
- Có `public static bool IsTutorialDone` **và** nó chỉ `true` sau khi tutorial xong sớm (trước Lv10) → **OK, ship.**
- Khác đi → sửa **đúng 1 hàm** `TutorialDangChay()` cho khớp API thật. QA khuyến nghị đổi luôn về ngữ nghĩa hẹp *"tutorial đang chạy"*, và **thêm lưới an toàn** ở `MoChoBen`: nếu bị chặn vì tutorial thì `ShowHint("Xong hướng dẫn rồi mở bến nhé!")` thay vì im lặng.

---

### 🟠 M-7 · `maxDockMinutes = patienceMinutes = 30` ⇒ lưới an toàn LUÔN thắng đường tự nhiên (đường timeout thật thành code chết)

**File:** `devA/.../TouristBoatConfig.cs:51-53` (default + tooltip) · `devA/.../BoatDockManager.cs:~250 UpdateDockTimeout`

```csharp
[Tooltip("... Nên khớp patienceMinutes (30) vì khách hết kiên nhẫn là chuyến coi như xong. ...")]
public float maxDockMinutes = 30f;      // ← tooltip đang HƯỚNG Sếp vào đúng cái bẫy
```

**Chứng minh bằng mốc thời gian (debugTimeScale = 1, đúng bản release):**

| Mốc | Thời điểm |
|---|---|
| Tàu chạm bến | `T` — Dev A đặt `AnchorUtcTicks = T`, lưới an toàn đếm **từ đây** |
| Khách xuống hết | `T + disembarkInterval × N` = `T + 0.8×6` = `T+4.8s` |
| Khách cuối tới hàng chờ | `+ quãng đường / visitorWalkSpeed` ≈ `T+10s` |
| Bubble khách cuối mở (stagger 0.4s × 6) | ≈ `T+12s` ← **`PatienceEndUtcTicks` chỉ được đặt Ở ĐÂY** (`TouristAgent.OpenBubbleIfNeeded:496`) |
| Kiên nhẫn khách cuối hết | `T + 12s + 1800s` = **`T+1812s`** |
| **Lưới an toàn Dev A bắn** | `T + 1800s` = **`T+1800s`** ← **sớm hơn 12 giây** |

Vì mốc kiên nhẫn **luôn** bắt đầu sau lúc chạm bến (phải đi bộ + xếp hàng + tới lượt bubble), còn lưới an toàn đếm **từ** lúc chạm bến, nên `patienceEnd > maxDockDeadline` **với mọi khách, mọi chuyến, mọi cấu hình có `maxDock == patience`**.

**Hệ quả thực tế:**
1. **Đường timeout tự nhiên (`TickWaitServe` → `NotifyTimedOut` → `MarkTimedOut` → giữ mặt giận `angryHoldSeconds = 2s` → đi bộ về tàu) trở thành CODE CHẾT** trong mọi chuyến người chơi không phục vụ. Thay vào đó luôn chạy đường ép: `ForceLeaveAngry()` giữ mặt giận `forcedAngryHoldSeconds = **0.4s**` rồi bị `ForcedCleanupRoutine` **despawn cứng** sau 2.2s — khách biến mất giữa đường, không kịp đi về tàu.
   ⇒ **AC §8.5 ("icon giận 2s, khách về tàu") đang nghiệm thu nhầm biến thể 0.4s + despawn**, không phải hành vi thiết kế.
2. **Console có `LogWarning` "Tàu số 0X đậu quá 30 phút mà chưa có báo khách lên tàu" ở MỌI chuyến bỏ trống** — 3 bến thì cứ ~10 phút một cảnh báo. Lưới an toàn mất hết giá trị chẩn đoán vì nó kêu cả khi hệ đang chạy đúng (ngược tinh thần AC §8.7 "console sạch").
3. **Guard của chính Dev A không bắt được ca này**: tool chẩn đoán chỉ cảnh báo khi `maxDockMinutes < patienceMinutes`, mà `30 < 30` là `false`. Quy ước Dev A ghi ở HANDOFF §215 (`maxDockMinutes ≥ patienceMinutes`) **thiếu đúng số hạng** `đi bộ + stagger`.

**Cách sửa (chọn 1, không cần Dev sửa code):**
- **Nhanh nhất — Sếp làm lúc điền config:** đặt `maxDockMinutes = **35**` (dư 5 phút cho đi bộ + stagger + mọi map lớn). Đường tự nhiên thắng, lưới an toàn trở lại đúng vai "chỉ chạy khi có bug".
- **Sạch hơn — Dev A, 2 dòng:** đổi default thành `35f`, sửa tooltip thành *"phải LỚN HƠN `patienceMinutes` ít nhất 2–5 phút (khách còn phải đi bộ + chờ tới lượt bubble mới bắt đầu tính kiên nhẫn)"*, và đổi cảnh báo trong tool chẩn đoán thành `maxDockMinutes <= patienceMinutes`.

---

### 🟡 minor mới

| Mã | File : dòng | Mô tả | Hệ quả | Sửa đề xuất |
|---|---|---|---|---|
| **m-12** | `devB/.../TouristVisitorManager.cs:222` | `if (config == null) config = _mgr.Config;` — **không kiểm** `config == _mgr.Config` khi field đã có sẵn | Lead/tool kéo nhầm **asset TouristBoatConfig khác** vào `TouristVisitorManager` ⇒ Dev A và Dev B đọc `debugTimeScale`/`patienceMinutes` từ 2 asset khác nhau ⇒ khách và tàu chạy khác nhịp, lỗi cực khó nhìn ra. Hai công thức `EffectiveTimeScale` đã khớp tuyệt đối (xem §7.4-b) nên đây là đường lệch **duy nhất** còn lại | 2 dòng: `if (_mgr.Config != null && config != _mgr.Config) Debug.LogError("[TouristVisitor] config KHÁC asset của BoatDockManager — khách và tàu sẽ chạy khác nhịp!");` |
| **m-13** | `devC/.../TouristBoatUIPopupSetupTool.cs` (canvas order **400**) ↔ `FarmUIManager.cs:209-215` | `FarmUIManager.EnsureHintText()` (đường **dự phòng** khi `txtHint` chưa wire) chọn **root canvas có `sortingOrder` cao nhất** để đẻ `Txt_Hint_DuPhong` | Canvas mới của Dev C (400) nhiều khả năng cao hơn canvas HUD ⇒ hint dự phòng mọc trên canvas boat thay vì HUD. Vô hại về chức năng (canvas luôn active nên hint vẫn hiện, thậm chí còn không bị tắt khi vào bếp), nhưng là tác dụng phụ ngoài ý muốn — và `ShowHint` chính là kênh phản hồi của luồng giao món Dev B | Ghi nhận. Nếu Sếp thấy hint đặt sai chỗ: wire `txtHint` trong `FarmUIManager` (đường chính, không đụng code) |
| **m-14** | `devB/.../TouristAgent.cs:492` | `OpenBubbleIfNeeded` return ngay khi `_bubble == null` ⇒ `PatienceEndUtcTicks` **không bao giờ được đặt** | Prefab khách thiếu component `TouristRequestBubble` (Sếp xoá tay / prefab dựng thủ công) ⇒ khách đó chờ vô hạn, `IsWaitingBubble` luôn true nên tap chỉ ra hint *"Khách đang xem thực đơn…"* mãi. **Không còn kẹt tàu** (2 lưới an toàn của B-1 dọn sau 30 phút) nhưng chuyến đó hỏng và không có log nào chỉ ra nguyên nhân | 2 dòng trong `Setup()`: `if (_bubble == null) Debug.LogError($"[TouristVisitor] Prefab '{name}' thiếu TouristRequestBubble — khách này sẽ không gọi món được.");` |
| **m-15** | `devB/.../TouristVisitorManager.cs:364` (`ForcedCleanupRoutine`) + `:271` (`WatchdogRoutine`) | Dùng `WaitForSeconds` (**chịu `Time.timeScale`**) trong khi cửa sổ ân hạn của Dev A dùng `Time.realtimeSinceStartup` (**không chịu**) | Ai đó pause game bằng `Time.timeScale = 0` đúng lúc timeout: dọn dẹp của Dev B đứng im, Dev A vẫn ép rời bến sau 3 giây thực. **Đã có đường thoát đúng** — `HandleBoatDeparting` → `DestroyTrip`, và `ForcedCleanupRoutine` khi tỉnh lại thấy `_trips[dock] != trip` nên `yield break` (không despawn 2 lần, không xoá nhầm save). Chỉ là khách biến mất tức thì thay vì đi về tàu | Ghi nhận. Muốn chuẩn thang thời gian thì đổi 2 chỗ sang `WaitForSecondsRealtime` |

---

## 7.4 SOI 3 VÙNG GHÉP NỐI MỚI (3 Dev sửa song song, không thấy code của nhau)

### (a) Đường timeout — Dev A ân hạn 3s (thực) vs Dev B despawn 2.2s + watchdog 5s

**Kết luận: KHÔNG có race gây hại.** Đã truy 6 kịch bản:

| # | Kịch bản | Diễn biến | Kết quả |
|---|---|---|---|
| 1 | **Luồng chuẩn** | `T+maxDock`: Dev A `LogWarning` + `OnDockTimeoutForced` → Dev B `ForceEndTrip` (khách chưa phục vụ → Angry 0.4s → đi về tàu) → `T+2.2s`: `ForcedCleanupRoutine` despawn + `ClearTripSave` + `ReportVisitorsAllAboard` → Dev A còn ở `Docked` nên nhận lệnh **đường bình thường** (`forcedByTimeout:false`) → `T+3s` không bao giờ tới | ✅ Tàu rời bến sạch, `_timeoutNoticed` tự dọn ở `UpdateDockTimeout` khi thấy state ≠ Docked |
| 2 | **Dev B chậm/treo** (coroutine bị giết, `Time.timeScale=0`, exception) | `T+3s`: Dev A `BeginDeparture(forced:true)` → `OnBoatDeparting` → Dev B `HandleBoatDeparting` → `DestroyTrip` + `ClearTripSave` | ✅ Tàu **chắc chắn** rời bến, không phụ thuộc Dev B |
| 3 | **Cùng frame** (Dev B report đúng lúc Dev A ép) | Bất kể thứ tự: nếu Dev B trước → Dev A thấy `Docked`, nhận lệnh, `UpdateDockTimeout` sau đó thấy ≠ Docked → thoát. Nếu Dev A trước → Dev B `ReportVisitorsAllAboard` rơi vào guard `if (_departForcedByTimeout[dock]) return;` (`BoatDockManager:201`) — **bỏ qua HOÀN TOÀN ÊM, không log rác** | ✅ Không lên lịch chồng chuyến, không log nhiễu |
| 4 | **Đuổi khách 2 lần?** | `ForceEndTrip` có `if (trip.ForcedEnding) return;` (`:343`); watchdog có `if (trip.ForcedEnding) continue;` (`:278`); `ForcedCleanupRoutine` có `if (_trips[dock] != trip) yield break;` (`:368`); `TouristAgent.ForceLeaveAngry` bỏ qua khách đã `WalkingBack/Boarding/Done` | ✅ **Không** |
| 5 | **Xoá save sai lúc?** | Chỉ 3 chỗ `ClearTripSave`: sau khi despawn xong (`:380`), trong `HandleBoatDeparting` (tàu đã thật sự đi), và `TryFinishTrip` **sau khi** xác nhận `!mgr.IsDocked(dock)`. Không còn chỗ nào xoá trước khi Dev A nhận lệnh (đây chính là m-8) | ✅ **Không** |
| 6 | **Tàu rời mà khách còn trên bờ?** | Về mặt hình ảnh: có, ở kịch bản 2 khách bị `Destroy` giữa đường (không có animation lên tàu). Nhưng **không bao giờ còn khách "mồ côi" sống trong scene** — cả `DestroyTrip` lẫn `ForcedCleanupRoutine` đều `queue.Remove(a)` + `Destroy(a.gameObject)` | ⚠️ Chấp nhận được (đường lỗi), ghi vào checklist Play Mode bước 21 |

**Ở `debugTimeScale = 60`** (`maxDock` = 1800/60 = **30 giây thực**; ân hạn **3s thực không chia**; despawn Dev B **2.2s thực**; đi bộ **không** chia scale ≈ 5s):
2.2s < 3s nên **thứ tự vẫn đúng** ✅, nhưng khách chỉ có 2.2s để đi hết quãng đường ~5s ⇒ **luôn bị despawn giữa đường khi tua nhanh**. Đây là hệ quả tất yếu của việc "khách đi bộ trong không gian thật, lịch tàu tua nhanh" — **không phải bug**, nhưng Sếp cần biết trước để không báo nhầm lỗi. Đã ghi vào checklist bước 17.

### (b) `debugTimeScale` — Dev B tự tính lại thay vì đọc của Dev A

**Đã so từng ký tự — KHỚP TUYỆT ĐỐI, kể cả guard Editor/Dev build:**

| | Dev A `BoatDockManager` | Dev B `TouristVisitorManager` |
|---|---|---|
| Cờ cho phép | `_allowDebugTime = Application.isEditor \|\| Debug.isDebugBuild;` (đặt trong `Awake`) | `_allowDebugTime = Application.isEditor \|\| Debug.isDebugBuild;` (đặt trong `Awake`) |
| Công thức | `if (!_allowDebugTime \|\| config == null) return 1f; return Mathf.Max(0.01f, config.debugTimeScale);` | **giống hệt** |
| Phạm vi | `private float EffectiveTimeScale()` | `public float EffectiveTimeScale { get; }` |

`TouristAgent` đọc `_manager.EffectiveTimeScale` ⇒ **một nguồn duy nhất trong phạm vi Dev B**. Bản release (không phải Editor, không phải Dev build) cả hai đều trả `1f` ⇒ không lệch.

**Đường lệch duy nhất còn lại: 2 asset config khác nhau** → xem **m-12**.
**Rủi ro bảo trì (ghi nhận, không phải lỗi):** đây là *contract-by-copy* — sửa luật ở Dev A mà quên Dev B thì lệch âm thầm, compile vẫn sạch. Nếu sau này Dev A cho phép, nên đổi `EffectiveTimeScale()` của Dev A thành `public` và Dev B đọc thẳng.

### (c) Dev C: canvas riêng + sửa `BoatDockSlot.cs`

| Câu hỏi | Kết luận |
|---|---|
| Phá `TouristBoatSetupTool` (V1)? | **Không.** Tool đó chỉ dựng world-space (`grep Canvas` = 0 kết quả), không đụng canvas nào. Pass 3 compile sạch cùng nó. |
| **Serialize field của `BoatDockSlot` có đổi không?** (đây mới là chỗ chết người — đổi tên field là scene **mất reference âm thầm**, `SerializedObject.FindProperty` trả null và tool wire hụt **không báo lỗi**) | **KHÔNG ĐỔI MỘT CHỮ.** Đã `diff` danh sách field: `dockIndex` · `berth` · `pathRoot` · `blindPoint` · `lockRoot` · `teaserText` · `tapCollider` · `floatingTextRise` · `floatingTextSeconds` — **giống hệt bản gốc**, khớp đúng 6 tên mà `TouristBoatSetupTool.WireSlot` gọi. Scene đã dựng vẫn giữ nguyên reference. ✅ |
| Phá `FarmUIManager.HideAllPopups()`? | **Không.** Hàm đó chỉ duyệt mảng serialize `popupObjectsToForceClose`; canvas mới không nằm trong đó. **Còn an toàn hơn trước**: vòng 1 popup nằm dưới `canvasPopupRoot` nên "đóng tất cả popup" có thể `SetActive(false)` nó và **làm lệch `popupLockCount`**; nay không thể. Và kể cả Sếp tự thêm canvas mới vào mảng đó thì `OnDisable` cũng trả lock + giữ hàng đợi + `OnEnable` chạy lại. ✅ |
| Guard **m-1** (`dockIndex == 0 && !IsIntroDone`) còn nguyên **cả 2 đầu**? | **CÒN, ở cả hai.** `OnMouseDown:126` và **kiểm lại** ở `OnMouseUpAsButton:157` (*"guard m-1, kiểm lại lúc nhả"*) — đúng bài, vì trạng thái có thể đổi trong lúc giữ tay. ✅ |
| Có 2 đường tap chồng nhau không? | **Không.** `TouristBoatUnlockFlow` đã xoá `Update()` bắn tia + `VoHieuTapMuaTrucTiep()` + field `choPhepTapBangKhoa`/`_tapColliders`. Chỉ còn `BoatDockSlot` bắt tap. ✅ |
| Không có popup trong scene (quên chạy tool UI) thì sao? | `BoatDockSlot:165` **giữ nguyên đường V1** (`CanUnlockDock` → `TryUnlockDock` → floating text). Không ai mất đường mua bến vì quên chạy tool. ✅ |
| Popup vẫn ẩn đúng khi ở bếp? | Có — `Update` poll 0.5s tự đóng nếu scene bếp đang load (cả 2 popup), và `DuocPhepHien()` vẫn chặn `DangTrongSceneBep()`. ✅ |
| Còn `m-3` sau khi đổi kiến trúc? | **Đã kín cả 2 thứ tự `Start`** (phân tích dưới). ✅ |

**Vì sao `QuetChuyenChuaBao` đủ kín (không hiển nhiên, QA đã truy):** `BootRoutine` chỉ chờ `Instance != null` (có từ `Awake`) rồi quét ngay, mà `TryGetNextArrivalUtc` lại `return false` khi `!IsReady`. Thoạt nhìn là hụt. Nhưng hai cơ chế **bù trừ chính xác cho nhau**:
- `BoatDockManager.Start` chạy **trước** → announce đã bắn và bị lỡ, **nhưng** `IsReady == true` ⇒ **quét bắt được**.
- `BoatDockManager.Start` chạy **sau** → quét trả rỗng, **nhưng** subscription đã đăng ký xong ⇒ **event bắt được**.
- Mọi announce muộn hơn đều đi qua `Update` (sau cả 2 `Start`) ⇒ luôn bắt được.

⇒ Đúng ở cả hai thứ tự. *Khuyến nghị bảo trì:* thêm `|| !BoatDockManager.Instance.IsReady` vào điều kiện `while` cho ý đồ hiển thị rõ ràng — hiện tại tính đúng đắn phụ thuộc một lập luận ngầm mà người sửa sau dễ phá vỡ.

---

## 7.5 AUDIT KEY CỦA `TouristBoatDiagnosticTool` (mục 3 của brief)

Đối chiếu **từng chuỗi hằng** trong tool của Dev A với **nguồn sự thật trong code Dev B / Dev C**:

| # | Hằng trong tool (`TouristBoatDiagnosticTool.cs`) | Giá trị | Nguồn sự thật | Khớp |
|---|---|---|---|---|
| 1 | `KeyUnlockedFormat` :45 | `TouristBoat_Unlocked_{0}` | `BoatDockManager.cs:47` | ✅ |
| 2 | `KeyAnchorV1Format` :46 | `TouristBoat_AnchorUtc_{0}` | `BoatDockManager.cs:48` | ✅ |
| 3 | `KeyIntroDone` :47 | `TouristBoat_IntroDone` | `BoatDockManager.cs:49` | ✅ |
| 4 | `KeyStateFormat` :49 | `TouristBoat_V2_State_{0}` | `BoatDockManager.cs:51` | ✅ |
| 5 | `KeyStateAnchorFormat` :50 | `TouristBoat_V2_Anchor_{0}` | `BoatDockManager.cs:52` | ✅ |
| 6 | `KeyNextArrivalFormat` :51 | `TouristBoat_V2_NextArrival_{0}` | `BoatDockManager.cs:53` | ✅ |
| 7 | `KeySchemaVersion` :52 | `TouristBoat_ScheduleVersion` | `BoatDockManager.cs:54` | ✅ |
| 8 | `KeyTripFormat` :54 | `TouristTrip_{0}` | **`TouristVisitorManager.cs:112`** (Dev B) | ✅ |
| 9 | `KeyDaBaoChuyenFormat` :56 | `TouristBoat_DaBaoChuyen_{0}` | **`BoatAnnouncePopupUI.cs:79`** (Dev C) | ✅ |

**9/9 khớp chính xác từng ký tự** (kể cả `{0}`). Menu 8 xoá đủ: 2 key toàn cục + 7 key/bến × 3 bến. Menu 7 đã bỏ reflection `_anchorTicks`, chuyển sang `EditorForceDockNow` / `EditorDescribeState`; mục `[2] Config` in số V2 và **cảnh báo khi `maxDockMinutes < patienceMinutes`** (ngưỡng này thiếu số hạng đi-bộ — xem **M-7**).

⇒ **m-2 RESOLVED.** Đây là chỗ dễ sai âm thầm nhất của vòng này và Dev A đã làm đúng.

---

## 7.6 ACCEPTANCE CRITERIA §8 DƯỚI ÁNH SÁNG 2 QUYẾT ĐỊNH MỚI

| AC | Chuẩn nghiệm thu **sau** quyết định của Sếp | Trạng thái |
|---|---|---|
| **§8.1** tàu vào sát bờ, gangplank, 3–6 khách đi đúng đường đất, xếp hàng thẳng | Không đổi. M-1 (gangplank) + M-3 (đi đúng đường) đã fix; thêm lệch làn khi về | ✅ sẵn sàng test |
| **§8.2** *"bubble chỉ mở ở khách đầu hàng"* | ❗**SPEC ĐÃ ĐỔI (quyết định ①)** → chuẩn mới: **mọi khách đều mở bubble, lần lượt cách nhau 0,4s**, và **tap khách nào cũng giao được**. Phần còn lại (trừ đúng 1 món, vàng = Σ nguyên liệu **chính** ×2, EXP, mặt cười bay) giữ nguyên và đã fix B-3 + M-4 | ✅ chuẩn mới đã ghi vào bước 5–8 |
| **§8.3** khách được phục vụ về tàu, hàng tiến lên, *"bubble kế mở"* | ❗Vế *"bubble kế mở"* **không còn ý nghĩa** — mọi bubble đã mở từ đầu. Chuẩn mới: hàng **dồn lên**, khách cuối lên tàu → gangplank rút → tàu lùi | ✅ |
| **§8.4** 1 bến 5p ±5s · 3 bến ≥3p so le, chu kỳ ~10p | **Giờ mới nghiệm thu được** nhờ B-2 (tua nhanh đồng bộ) + M-2 (kiên nhẫn song song). ⚠️ Cần đặt `maxDockMinutes = 35` (**M-7**), nếu không mọi chuyến bỏ trống đều kết thúc bằng lưới an toàn kèm warning | ⚠️ phụ thuộc M-7 |
| **§8.5** *"icon buồn"* | ❗**ĐỔI (quyết định ②)** → **MẶT TỨC GIẬN (đỏ, cau mày)**. Placeholder procedural đã phân biệt được 3 mặt (m-4). ⚠️ Muốn thấy đúng **2 giây** giận + đi bộ về tàu thì **bắt buộc** `maxDockMinutes > patienceMinutes` (**M-7**) — nếu không sẽ luôn rơi vào biến thể ép 0,4s + despawn | ⚠️ phụ thuộc M-7 |
| **§8.6** tắt/mở game mọi pha: không kẹt tàu, không nhân đôi khách, không mất thưởng | 3 vế đều đã được vá đúng gốc (B-1 4 lớp · guard `_trips[dock] != null` + `IsDocked` scan · B-3). Thêm ca mới cần test: **lưới an toàn 30 phút** và **load save đang Docked quá hạn** | ✅ |
| **§8.7** console 0 lỗi đỏ · 60fps · ≤18 khách | 0 **lỗi đỏ** đạt được. ⚠️ **Warning** thì chưa sạch nếu để `maxDockMinutes = 30` (**M-7**). Hệ khách vẫn chạy full `Update` khi ở scene bếp — nên thêm `TouristVisitorManager` vào `behavioursToDisableInCooking` | ⚠️ phụ thuộc M-7 |

---

## 7.7 HAI VIỆC CHẶN — LÀM TRƯỚC KHI COPY

- [ ] **B-5 (30 giây):** mở `TutorialManager.cs` trong project thật, xác nhận có `public static bool IsTutorialDone` **và** nó thành `true` **trước Lv10**. Không đúng → báo Dev C sửa 1 hàm `BoatAnnouncePopupUI.TutorialDangChay()` (2 phút) rồi mới copy.
- [ ] **M-7 (5 giây):** khi điền config, đặt `maxDockMinutes = **35**` (không phải 30 như tooltip khuyên).

---

## 7.8 ✅ CHECKLIST PLAY MODE **CUỐI CÙNG** CHO SẾP

> Thay thế hoàn toàn §6. Đã gộp, bỏ bước thừa, đánh số liên tục. Cột **AC** = tiêu chí nghiệm thu bước đó chứng minh. Cột **Hồi quy** = finding vòng 1/2 mà bước đó kiểm chứng.

### GIAI ĐOẠN A — Cài đặt (làm 1 lần, đúng thứ tự)

| # | Việc | Phải thấy | Hồi quy |
|---|---|---|---|
| 1 | Copy `devA/` vào project **TRƯỚC** (B/C cần field config V2 mới compile) | — | |
| 2 | Copy `devB/`, `devC/`. **Ghi đè** `BoatDockSlot.cs` (Dev C, đã backup) và `TouristBoatDiagnosticTool.cs` (Dev A) | Unity compile xong: **0 lỗi đỏ** | **B-5** ← nếu đỏ ở `IsTutorialDone` thì DỪNG, xem §7.7 |
| 3 | Mở `TouristBoatConfig.asset` → điền **13** field theo HANDOFF devA §2 | `gapOneDockMinutes=5` · `gapMultiDockMinutes=10` · `minStaggerMinutes=3` · **`maxDockMinutes=35`** · `visitorsMin=3` · `visitorsMax=6` · `patienceMinutes=30` · `rewardIngredientMultiplier=2` · `disembarkInterval=0.8` · `visitorWalkSpeed=150` · `queueSpacing=120` · `bubbleScaleInTime=0.25` · `smileyFlyTime=1.2` | **M-7** |
| 4 | `Tools/Farm Game/Tourist Boat/Setup NPC Animations` | Dialog **11/11** nhân vật OK | |
| 5 | `Setup Tourist Visitors (Scene)` trong `SCN_Farm` | Dựng `TouristSystem` · `QueueAnchor` · 3 `TouristPath_Dock0X` · 3 `Gangplank` | |
| 6 | `Setup Popups (UI)` | Tạo canvas **`Canvas_TouristBoatPopup`** (riêng, ở gốc scene — **không** dưới `canvasPopupRoot`) | **B-4** |
| 7 | `Dịch bến sát bờ` → **Tự suy hướng bờ** → **ÁP DỤNG 3 bến** → chỉnh tay → `10. Canh Tau Vao O Dau` | Tàu đậu sát mép bờ | |
| 8 | **REVIEW bắt buộc:** kéo `WP_01..04` bám đường đất · kéo `QueueAnchor` ra trước cửa nhà hàng · canh 3 `Gangplank` · Ctrl+S | Gizmo vàng 6 slot nằm đúng chỗ khách đứng | |
| 9 | Kiểm Inspector `TouristVisitorManager` | `config` = **ĐÚNG asset mà `BoatDockManager` đang dùng** · `queue` ✅ · `touristPrefabs` = 11 · `dishDatabase` = **38** · `dockPathRoots[3]` · `gangplanks[3]` | **m-12** |
| 10 | Menu `6. Chẩn Đoán` | Không cảnh báo lệch config; mục `[2] Config` in đúng số V2 | **m-2** |

### GIAI ĐOẠN B — Vòng lặp cơ bản

| # | Việc | Phải thấy | AC | Hồi quy |
|---|---|---|---|---|
| 11 | Lên Lv10 | Intro 4 câu chạy **1 lần** → camera lia ra bến 1 → tàu 01 chạy vào sát bờ | §8.1 | |
| 12 | Lúc tàu chạm bến | **Gangplank bắc xuống** (~0,4s) rồi khách xuống **lần lượt cách 0,8s** | §8.1 | M-1 |
| 13 | Khách đi bộ | Bám **đúng đường đất** theo WP · **không đi tắt, không quay đầu ngược** · lượt về **lệch làn**, không đi xuyên lượt lên | §8.1 | **M-3** |
| 14 | Khách tới hàng chờ | Xếp hàng thẳng, đều `queueSpacing` · **TẤT CẢ khách đều mở bubble, lần lượt cách nhau ~0,4s** (không phải chỉ người đầu) | §8.2 *(chuẩn mới ①)* | **M-2** |
| 15 | Xem món | Món luôn thuộc tập đã unlock theo level · các khách trong 1 chuyến **không trùng món** | §8.2 | |
| 16 | Tap khách **ĐỨNG GIỮA HÀNG** (không phải người đầu) khi chưa nấu | Hint *"Chưa có \<tên món\> trong kho — vào bếp nấu nhé!"* · kho không đổi · **không** báo "chưa tới lượt" | §8.2 *(chuẩn mới ①)* | **M-2** |
| 17 | Nấu món đó → đưa vào kho → tap **đúng khách giữa hàng** đó. **Ghi số vàng/EXP trước và sau** | Kho **−1 đúng món** · vàng **+ Σ giá nguyên liệu CHÍNH ×2** (**không** tính muối/nước mắm/tiêu) · EXP **+`dish.rewardExp`** · mặt cười bay lên HUD nhỏ→to→fade | §8.2 | **B-3 · M-4** |
| 18 | **Ca mất món:** tìm 1 `DishData` có `sellPrice = 0` **và** `requiredIngredients` rỗng, ép khách gọi rồi giao | **Phải được thưởng > 0** (rơi về giá mặc định 10×2) · **tuyệt đối KHÔNG** trừ món mà 0 vàng | §8.6 | **B-3** |
| 19 | Khách được phục vụ | Đi về tàu · hàng **dồn lên** · khách cuối lên tàu → **gangplank rút** → tàu lùi rời bến | §8.3 | |
| 20 | Ngay sau đó | Popup *"Tàu số 01 sẽ cập bến sau 5 phút!"* — khung gỗ, dim 60%, nút **"Đã rõ"**, hiện **đúng 1 lần** | §3.5 | |

### GIAI ĐOẠN C — Lịch tàu & mua bến

| # | Việc | Phải thấy | AC | Hồi quy |
|---|---|---|---|---|
| 21 | 1 bến mở — bấm giờ từ lúc tàu rời bến | Cập bến lại sau **đúng 5 phút ±5s** | §8.4 | |
| 22 | **Kéo bản đồ** bằng cách đặt tay **lên bảng khóa** bến 2 rồi rê đi | **KHÔNG** mở popup mua (ngưỡng kéo 24px) | — | **M-6** |
| 23 | Đang kéo hạt giống / kéo liềm ngang qua bảng khóa | **KHÔNG** mở popup mua | — | **M-6** |
| 24 | **Chạm gọn** vào bảng khóa bến 2 (Lv12, đủ 2.000 vàng) | Mở **popup mua** (không mua thẳng) · số giá **"2.000"** đúng định dạng VN | §8.4 | **M-6 · m-6** |
| 25 | Bấm MUA | Trừ tiền · popup đóng · **sao vàng nổ + SFX** · tàu 02 xuất phát ngay | §8.4 | |
| 26 | Thiếu tiền → mở popup bến 3 (25 gem) | Nút MUA **xám** + dòng đỏ *"Không đủ gem"* · **nhận gem lúc popup đang mở → nút tự sáng** | §8.4 | |
| 27 | Mở đủ 3 bến, chạy vài chu kỳ | Mọi cặp arrival cách nhau **≥3 phút** · chu kỳ **~10 phút/bến** | §8.4 | **M-2** |
| 28 | **Ca §8.5 cũ:** để 3 bến cùng đông (18 khách) rồi **AFK** | Tàu rời bến trong **~30–35 phút** (KHÔNG phải hàng giờ) · **console không có warning "đậu quá … phút"** | §8.4 · §8.7 | **M-2 · M-7** |

### GIAI ĐOẠN D — Kiên nhẫn & lưới an toàn *(bật `debugTimeScale = 60`)*

| # | Việc | Phải thấy | AC | Hồi quy |
|---|---|---|---|---|
| 29 | Đặt `debugTimeScale = 60`, Play | Lịch tàu chạy nhanh **VÀ** khách cũng sốt ruột nhanh theo (30 phút game ≈ **30 giây thực**) — hai bên **cùng nhịp** | §8.4 · §8.5 | **B-2** |
| 30 | Để khách hết kiên nhẫn (không phục vụ ai) | Bubble đổi **MẶT TỨC GIẬN (đỏ, cau mày)** — phân biệt rõ với mặt cười vàng · giữ **~2 giây** · khách **đi bộ về tàu** · **không** cộng vàng/EXP | §8.5 *(chuẩn mới ②)* | **m-4 · M-7** |
| 31 | Sau đó | Hàng dồn lên · tàu vẫn rời bến bình thường · **console KHÔNG có** `LogWarning "đậu quá … phút"` | §8.5 · §8.7 | **M-7** ← nếu có warning là `maxDockMinutes` vẫn đang để 30 |
| 32 | **Ép lưới an toàn:** tạm đặt `maxDockMinutes = 1`, Play, để tàu đậu | `LogWarning` "đậu quá 1 phút…" → **3 giây sau tàu tự rời bến** dù khách còn trên bờ · khách chuyển giận rồi biến mất · **hệ KHÔNG kẹt**, chuyến kế lên lịch đúng gap | §8.6 | **B-1** |
| 33 | **Ép kẹt Dev B:** xoá `QueueAnchor` khỏi scene rồi Play | Console **LogError** "KHÔNG THẤY TouristQueue… đã tự dựng hàng chờ tạm" · khách đứng chồng nhau nhưng **vẫn hết kiên nhẫn, vẫn về tàu, tàu VẪN RỜI BẾN** | §8.6 | **B-1** |
| 34 | Trả `maxDockMinutes` về **35**, `debugTimeScale` về **1** | — | | |

### GIAI ĐOẠN E — Tắt/mở game & scene bếp *(ca dễ gãy nhất)*

| # | Việc | Phải thấy | AC | Hồi quy |
|---|---|---|---|---|
| 35 | Tắt game lúc tàu **đang chạy vào** (Arriving) → mở lại | Tàu ở đúng vị trí theo giờ UTC · cập bến đúng lịch · khách spawn **ĐÚNG 1 LẦN** | §8.6 | |
| 36 | Tắt lúc tàu **đang Docked, khách đang xếp hàng** → mở lại. **Thử 5 lần liên tiếp** | **Gangplank VẪN BẮC** cả 5 lần (lỗi cũ nhấp nháy theo thứ tự script) · khách đặt thẳng vào slot · **KHÔNG nhân đôi khách** | §8.6 | **M-1** |
| 37 | Lặp bước 36 nhưng **chờ >30 phút thực** rồi mới mở lại | Khách quá hạn resolve **TimedOut ngay** · tàu rời bến · lên lịch chuyến kế · **không kẹt** | §8.6 | **B-1** |
| 38 | Tắt lúc tàu **đang rời bến** (Departing) → mở lại | Tua đúng chuỗi Departing → WaitingNext → (nếu quá giờ) Docked · `JustDocked` bắn **đúng 1 lần** | §8.6 | |
| 39 | **Ca popup chết:** tàu rời bến (popup hiện) → bấm "Đã rõ" → **vào bếp** → ra farm → đợi chuyến kế rời bến | Popup báo tàu **PHẢI hiện lại**. Im lặng vĩnh viễn ⇒ B-4 chưa fix | §5 edge 6 | **B-4** |
| 40 | Đang mở popup báo tàu thì **vào bếp** ngay | Popup **tự đóng** khi vào bếp · ra farm: **input world bình thường** (tap được ruộng/khách) · popup không hiện méo/mờ | §5 edge 6 | **B-4 · M-5** |
| 41 | Đang mở **popup MUA bến** thì vào bếp | Tự đóng · ra farm tap lại bảng khóa mở được popup như thường · **không khoá cứng input** | — | **M-5** |
| 42 | Vào bếp lúc tàu đang đậu, nấu vài món rồi ra | Kiên nhẫn khách **vẫn trôi theo UTC** (đúng thiết kế) · tàu có thể đã rời bến · **không NRE, không kẹt** | §8.6 | |
| 43 | Chỉnh đồng hồ máy **lùi 2 tiếng** khi đang chơi | `LogWarning` "Reset lịch bến … đồng hồ máy chỉnh lùi" · tàu cập bến lại sau ~30 giây · **không kẹt** | §5 edge 2 | |
| 44 | Menu `8. Xóa Save Tàu` → Play lại | Dialog báo xoá **đủ 3 nhóm** (Dev A + Dev B + Dev C) · intro chạy lại từ đầu · **không còn khách mồ côi** của chuyến cũ | — | **m-2** |

### GIAI ĐOẠN F — Chất lượng & hiệu năng

| # | Việc | Phải thấy | AC |
|---|---|---|---|
| 45 | Rà Console toàn phiên | **0 lỗi đỏ.** Warning chỉ còn loại "art placeholder" — **không có** warning "đậu quá … phút" | §8.7 |
| 46 | Ép 18 khách đồng thời (3 bến × 6) | Giữ **60fps** (Stats/Profiler) | §8.7 |
| 47 | *(Tuỳ chọn, khuyến nghị)* Thêm `TouristVisitorManager` vào `behavioursToDisableInCooking` của `FarmUIManager` | Hệ khách không chạy `Update` khi ở scene bếp | §8.7 |
| 48 | Khách vs decor | Khách **không bị vật thể che** (sorting `CongTrinh`, order 5000, Y-sort kẹp ±8000) | §8.7 |
| 49 | Chữ tiếng Việt | 2 popup + teaser bảng khóa **đủ dấu**. Thiếu dấu → đổi `Font Asset` sang font có dấu | — |
| 50 | `Xóa Tourist Visitors (Undo)` + Ctrl+Z | Gỡ sạch, không sót object rác | — |

---

## 7.9 QA ĐÃ SỬA GÌ Ở VÒNG 2?

### ⚠️ KHÔNG sửa một dòng nào trong `/home/user/work/deliver/` — và **vòng này không cần patch gì cả**.

Vòng 1 phải thay tạm `??=` (C#8) trong bản sao build vì `mcs 6.8` chỉ tới C#7.2. **Dev C đã bỏ dòng đó**, và quét lại toàn bộ 21 file không còn cú pháp C#8+ nào (`??=`, `is not`, `using var`, `new()`, range) ⇒ **cả 3 pass compile vòng 2 chạy trên file giao NGUYÊN BẢN**, kết quả đáng tin hơn vòng 1.

Thay đổi duy nhất phía QA, đều nằm trong `qa/` (hạ tầng kiểm thử, không phải sản phẩm):

| # | File | Sửa gì | Vì sao |
|---|---|---|---|
| 1 | `qa/stubs/ProjectStub.cs` | Thêm `public static bool IsTutorialDone => true;` vào stub `TutorialManager` | Dev C đổi từ reflection sang gọi static. **Đã ghi chú thẳng trong stub rằng đây là GIẢ ĐỊNH và compile PASS không chứng minh nó đúng** — xem **B-5**. |
| 2 | `qa/stubs/UnityEditorStub.cs` | Bổ sung `SerializedProperty.NextVisible/type/name/propertyPath`, `SceneView.pivot/size` | Tool V1 `TouristBoatSetupTool` dùng, cần cho pass 3 |
| 3 | `qa/srcset.sh` (mới) + 3 script compile | Bỏ `BoatDockSlot.cs` và `TouristBoatDiagnosticTool.cs` **bản gốc project** ra khỏi danh sách nạp | 2 file này giờ do Dev C / Dev A ship — nạp cả 2 bản sẽ trùng class, làm sai kết quả |
