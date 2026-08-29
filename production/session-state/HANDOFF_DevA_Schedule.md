# HANDOFF — Dev A (gameplay-programmer) · Tourist Boat V2 (BOAT-002)

> Phạm vi: schedule V2 event-driven — `BoatScheduleCore` · `BoatDockManager` · `TouristBoatConfig` · `TouristBoatController` · tool dịch bến sát bờ · bộ test console.
> KHÔNG đụng: `TouristBoatUnlockFlow.cs` (Dev C), `BoatDockSlot.cs`, `TouristBoatSetupTool.cs`.
> **Đợt 2 (Lead giao, có backup tại `production/backup_boat_2026-08-29/`):** thêm `TouristBoatDiagnosticTool.cs` vào phạm vi — đã vá cho khớp V2, xem mục 1.8.
> Trạng thái test: **119/119 PASS** (`mcs` + `mono`, xem mục 6).
> Cập nhật 2026-08-29 (sau QA review chéo): thêm **lưới an toàn chống kẹt tàu** (fix B-1, Sếp duyệt) — mục 1.7; **vá tool chẩn đoán cho V2** — mục 1.8. Compile sạch cả 6 file (4 runtime + 2 Editor).

---

## 1. Tóm tắt thay đổi từng file

### 1.1 `Assets/_Game/Farm/Scripts/TouristBoat/BoatScheduleCore.cs` (viết lại lõi, thuần C#)

- **Bỏ mô hình modulo chu kỳ cố định** làm nguồn sự thật. Thay bằng **máy trạng thái tường minh, persist được**:
  `WaitingNext(arrivalUtc) → Arriving(travel) → Docked (VÔ HẠN, chờ lệnh) → Departing(travel) → WaitingNext(...)`.
- `enum BoatState` giữ nguyên giá trị số của V1 và thêm alias `WaitingNext = Hidden` → code cũ (`BoatState.Hidden`) vẫn biên dịch & chạy đúng, code V2 dùng tên mới cho đúng nghĩa.
- Kiểu dữ liệu mới:
  - `DockScheduleState { State, AnchorUtcTicks, NextArrivalUtcTicks }` — đúng 3 số cần persist mỗi bến.
  - `DockResolveResult { State, JustDocked, Changed }`.
- Hàm chính:
  - `ResolveDock(state, nowUtc, travel)` — "tua" máy trạng thái tới hiện tại; tự nối chuỗi offline (Departing → WaitingNext → Docked) trong **một** lần gọi; `JustDocked` bật **đúng 1 lần** cho mỗi cú chạm bến (Docked là trạng thái hấp thụ → không thể ra lần hai).
  - `QueryPhase(state, nowUtc, travel)` → `BoatPhaseInfo` (state + progress 0-1) cho controller; thuần & idempotent.
  - `SelectGapSeconds(unlockedCount, gapOne, gapMulti)` — 5 phút / 10 phút.
  - `ScheduleNextArrival(departUtc, gap, travel, stagger, otherArrivals, n)` — arrival = rời bến + gap, rồi ép so le.
  - `ResolveStaggeredArrival(...)` — ép ≥ `minStagger` bằng cách **dời muộn**, không bao giờ kéo sớm.
  - `TryBeginDeparture(...)` — LỆNH duy nhất thoát pha Docked; trả `false` nếu không ở Docked (guard double-call).
  - `UpcomingArrivalUtcTicks`, `RoundedWaitMinutes`, `MakeFreshWaiting`, `IsScheduleImplausiblyFuture` (guard đồng hồ lùi V2).
- **Giữ nguyên** `EvaluateUnlock` + toàn bộ hàm V1 (`ComputePhase`, `ComputeCycleSeconds`, `ResolveStaggeredAnchor`, `BoatCycleSpec`…) trong khối `V1 LEGACY` — vì `TouristBoatDiagnosticTool.cs` (Editor, ngoài phạm vi tôi) còn gọi; xóa là gãy compile.
- Vẫn **không `using UnityEngine`**, không `DateTime.Now`, mọi mốc là ticks UTC truyền vào (inject được để test).

### 1.2 `Assets/_Game/Farm/Scripts/TouristBoat/BoatDockManager.cs` (viết lại phần điều phối)

**API CONTRACT V2 (đúng chữ ký đã pin, không đổi):**
```csharp
public event Action<int> OnBoatDocked;
public event Action<int> OnBoatDeparting;
public event Action<int, DateTime, int> OnNextTripScheduled;
public event Action<int> OnDockTimeoutForced;   // THÊM sau QA (fix B-1)
public bool IsDocked(int dockIndex);
public int UnlockedDockCount { get; }
public void ReportVisitorsAllAboard(int dockIndex);
public int BoatNumber(int dockIndex) => dockIndex + 1;
```

**API V1 giữ NGUYÊN 100%** (UnlockFlow/BoatDockSlot/Controller/tool đang gọi):
`Instance` · `DockCount` · `Config` · `OnDockUnlocked` · `OnBoatStateChanged` · `IsIntroDone` · `IsReady` ·
`IsDockUnlocked` · `MarkIntroDone` · `CanUnlockDock(out reason)` · `TryUnlockDock` · `UnlockDockFree` ·
`GetBoatState` · `GetDockedRemainingSeconds` · `GetDockBerth` · `TryGetPhaseInfo` · `GetBlindPoint` ·
`GetDockPathPoints` · `GetTravelSeconds` · `GetScheduleTravelSeconds`.
(`GetDockRequirement` nằm ở `TouristBoatConfig` — giữ nguyên.)

**API mới thêm (chỉ THÊM, cho Dev C / QA):**
- `bool TryGetNextArrivalUtc(int dockIndex, out DateTime arrivalUtc)` — dựng lại popup sau reload.
- `int GetMinutesToNextArrival(int dockIndex)` — số phút (thang thời gian game), -1 nếu không có.
- `#if UNITY_EDITOR`: `EditorForceDockNow(int)`, `EditorForceDepartNow(int)`, `EditorDescribeState(int)`.

**Lịch chuyến kế:** gap = `gapOneDockMinutes` (5) nếu `UnlockedDockCount == 1`, `gapMultiDockMinutes` (10) nếu ≥2; ép mọi cặp arrival cách nhau ≥ `minStaggerMinutes` (3) bằng cách dời muộn.

**Persist (PlayerPrefs):**
| Key | Ý nghĩa |
|---|---|
| `TouristBoat_Unlocked_{i}` | (V1, giữ nguyên) cờ mở bến |
| `TouristBoat_IntroDone` | (V1, giữ nguyên) intro đã chạy |
| `TouristBoat_AnchorUtc_{i}` | (V1) anchor chu kỳ cũ — V2 **không ghi**, chỉ để lại cho tương thích |
| `TouristBoat_V2_State_{i}` | **mới** — state (int) |
| `TouristBoat_V2_Anchor_{i}` | **mới** — mốc UTC ticks (string invariant) |
| `TouristBoat_V2_NextArrival_{i}` | **mới** — arrival chuyến kế khi Departing |
| `TouristBoat_ScheduleVersion` | **mới** — schema = 2 |

**Migrate V1 → V2:** bến đã mở mà chưa có key V2 → `WaitingNext(now + 30s)` (tàu vào ngay lần đầu) + log. Dữ liệu hỏng (anchor ≤ 0) → cũng đặt chuyến mới.

**Load resolve offline:** tua đúng chuỗi pha. Load vào **giữa pha Docked** thì **giữ Docked** — quản lý của tôi CHỜ `ReportVisitorsAllAboard`, không hỏi ngược Dev B; Dev B tự resolve khách rồi gọi (được phép gọi ngay frame đầu).

**Chống double-fire:**
- `OnBoatDocked` chỉ bắn khi `JustDocked` (transition xảy ra trong phiên này), và state được **persist TRƯỚC khi bắn** → reload không bắn lại.
- Chuyến đã Docked từ phiên trước (save = Docked) **KHÔNG** bắn lại (nếu bắn sẽ nhân đôi khách — GDD §8.6). Dev B khôi phục từ persistence riêng, hoặc hỏi `IsDocked(i)` sau boot.
- Event `OnBoatDocked` phát sinh trong lúc load được **hoãn ~2 frame** sau khi `IsReady = true` rồi mới bắn, để Dev B/C kịp subscribe trong `BootRoutine` (pattern "đợi IsReady" của V1). Chi tiết: `FlushPendingDockedEvents()`.
- `ReportVisitorsAllAboard` gọi trùng / gọi sai pha → bỏ qua êm + log, không lên lịch chồng chuyến.
- `OnNextTripScheduled` chỉ bắn 1 lần cho mỗi mốc arrival (`_announcedArrival`); Dev C vẫn nên persist theo `arrivalUtc` để không hiện lại sau reload, và **bỏ qua khi số phút < 1** (GDD §3.5).

**Đồng hồ lùi:** horizon = `gap + stagger×3 + travel×2 + 60s`; mốc UTC vọt quá horizon → reset `WaitingNext(now + 30s)` + `LogWarning` (giữ tinh thần luật V1 "lùi quá 1 gap thì reset"). Lùi nhẹ khi đang Arriving → lùi êm về WaitingNext, **giữ nguyên giờ hẹn**.

**debugTimeScale:** manager chia mọi duration (travel/gap/stagger/30s) cho scale trước khi đưa vào lõi — lõi chỉ biết giây thật. Số phút hiển thị cho popup vẫn quy đổi về **phút game** (scale 60: chờ 5 giây thực vẫn báo "5 phút").

### 1.3 `Assets/_Game/Farm/Scripts/TouristBoat/TouristBoatConfig.cs`

Thêm (GDD V2 §7), tất cả có `[Tooltip]` tiếng Việt:
`gapOneDockMinutes` · `gapMultiDockMinutes` · `minStaggerMinutes` · `visitorsMin` · `visitorsMax` · `patienceMinutes` · `rewardIngredientMultiplier` · `disembarkInterval` · `visitorWalkSpeed` · `queueSpacing` · `bubbleScaleInTime` · `smileyFlyTime`.
Property mới: `GapOneDockSeconds`, `GapMultiDockSeconds`, `MinStaggerSeconds`, `PatienceSeconds`.
`dockMinutes` / `hideMinutes` / `staggerMinutes` **giữ field** (serialize cũ + diagnostic tool) nhưng đánh dấu `[V1 — KHÔNG dùng ở V2]` trong Tooltip và comment `[V2 OBSOLETE]`. `OnValidate` kẹp giá trị mới (visitorsMax ≥ visitorsMin, các knob hiệu ứng có sàn > 0).

### 1.4 `Assets/_Game/Farm/Scripts/TouristBoat/TouristBoatController.cs` (sửa tối thiểu)

- Đọc state + progress từ manager như cũ (`TryGetPhaseInfo`), `case BoatState.Hidden` đổi tên thành `case BoatState.WaitingNext` (cùng giá trị).
- **Countdown world-space khi Docked**: không còn mốc thời gian → mặc định hiện nhãn tĩnh **"Đang đón khách..."**; bỏ tick `showDockedLabel` trong Inspector là **ẩn hẳn** chữ. Text ghi đúng 1 lần/lần đậu (không alloc mỗi frame). 2 field mới trên component: `showDockedLabel` (bool, mặc định bật), `dockedLabel` (string).
- Giữ nguyên tên child `"Countdown"` để scene/prefab cũ không phải sửa; giữ nguyên `berthOffset`, `EditorGetDockedPosition`, `EditorCaptureOffsetFrom`, `EditorBerthOffset` (tool menu 10/11 vẫn chạy).

### 1.5 `Assets/_Game/Farm/Editor/BoatShoreAdjustTool.cs` (MỚI)

Menu **`Tools/Farm Game/Tourist Boat/Dịch bến sát bờ`** → `EditorWindow` nhỏ:
- Field `Vector2 Offset (unit world)` + nút **ÁP DỤNG cho 3 bến** + nút **Hoàn tác lần dịch vừa rồi** (dịch ngược offset; Ctrl+Z cũng hoạt động vì mọi thay đổi qua `Undo.RecordObject`).
- Nút **"Tự suy hướng bờ"**: lấy vector `BlindPoint → Berth` (chuẩn hóa, trung bình 3 bến) × khoảng cách nhập vào (mặc định **150 unit**) → đó là hướng "từ biển tiến vào bờ".
  **Cơ sở suy hướng:** `TouristBoatSetupTool` đặt `BlindPoint` ở offset `(-2600, -1800)` so với bến ⇒ biển nằm phía **dưới-trái**, bờ nằm phía **trên-phải**; đi tiếp theo hướng BlindPoint→Berth là vào gần bờ hơn.
  Không suy được (thiếu `BlindPoint`/`Berth`, hoặc 2 điểm trùng nhau) → tool báo rõ và **để Sếp tự nhập Vector2** — đây là trường hợp duy nhất cần nhập tay.
- Tùy chọn **"Dịch cả WP cuối"** (mặc định bật, 1 WP) để đoạn cuối đường tàu không bị gãy khúc.
- Log ra Console: từng `Dock_XX/Berth` và từng `WP` dịch từ toạ độ nào tới toạ độ nào, bến nào bị bỏ qua vì thiếu object.

### 1.6 `tests/unit/touristboat/BoatScheduleCoreTests.cs` (bộ MỚI cho V2)

Bộ console tự chạy (không cần Unity), 119 assert, exit code 0/1 dùng được trong QA gate. Nhóm test:
A gap 5p/10p · A2 arrival = rời bến + gap (kể cả sàn kỹ thuật) · B so le 3p (xung đột trước/sau/biên/3 bến) ·
C1 resolve WaitingNext+Arriving · C2 Docked hấp thụ (offline 24h vẫn Docked, không bắn lại) · C3 Departing ·
C4 chuỗi offline nhiều pha · D đồng hồ lùi + reset · E `ReportVisitorsAllAboard` chuyển pha + vòng đời khép kín ·
F double-fire guard (gọi sai pha, gọi trùng, 10 frame liên tiếp chỉ 1 lần JustDocked) ·
**H lưới an toàn** (biên 29:59 vs đúng 30 phút, offline cả ngày, pha khác không dính, maxDock = 0/âm là tắt, ép rời vẫn giữ đúng gap, báo muộn idempotent, không kích lần hai, chuyến sau đếm lại từ 0) · G progress 0-1 + phút làm tròn + hồi quy `EvaluateUnlock`.

### 1.7 LƯỚI AN TOÀN CHỐNG KẸT TÀU (bổ sung sau QA — fix B-1, Sếp duyệt 2026-08-29)

Trả lời luôn cho câu hỏi mở số 1 ở mục 5: **có lưới an toàn**, mặc định 30 phút.

**Config:** `maxDockMinutes = 30`
Tooltip: *"Tàu đậu tối đa bao lâu rồi tự rời bến dù khách chưa xong — lưới an toàn chống kẹt."*
Để **0 = TẮT** lưới (tàu đậu vô hạn, event-driven thuần — chỉ dùng khi debug).
Cố ý để **2 field riêng** với `patienceMinutes` (cùng default 30) như Sếp chốt; đổi 1 số không kéo theo số kia.

**Lõi (`BoatScheduleCore`)** — 2 hàm thuần mới, không đổi chữ ký hàm cũ:
- `double DockedElapsedSeconds(state, nowUtc)` — giờ đậu tính bằng **UTC tuyệt đối** (offline vẫn đếm).
- `bool IsDockTimedOut(state, nowUtc, maxDockSeconds)` — chỉ đúng khi đang ở pha `Docked`; `maxDockSeconds ≤ 0` → luôn `false` (tắt lưới).
Việc chuyển pha vẫn đi qua đúng `TryBeginDeparture` như đường bình thường ⇒ **chuyến kế vẫn được lên lịch đủ gap + so le**, không mất lịch.

**Manager (`BoatDockManager`)** — hàm `UpdateDockTimeout(dockIndex, now)` chạy trong `Update` sau `ResolveDock`:

| Bước | Xảy ra khi | Hành động |
|---|---|---|
| 1. Cảnh báo | `Docked` và đã quá `maxDockMinutes` (đã chia `debugTimeScale` như mọi duration khác) | `LogWarning` + bắn **`OnDockTimeoutForced(dockIndex)`**, ghi mốc ân hạn. **Chưa** đổi pha. |
| 2. Ân hạn | trong **3 giây THỰC** kể từ bước 1 | Không làm gì — chờ Dev B đuổi khách còn lại về tàu. Dev B gọi `ReportVisitorsAllAboard` trong lúc này → đi **đường bình thường**, cờ tự dọn. |
| 3. Ép rời | hết 3 giây mà vẫn còn `Docked` | Manager **tự** `BeginDeparture(forcedByTimeout: true)` → Departing + lên lịch chuyến kế + `OnBoatDeparting` + `OnNextTripScheduled`. |

**Vì sao chọn cách này (đúng yêu cầu "chắc chắn không kẹt"):** bước 3 **không phụ thuộc** Dev B phản hồi — dù Dev B crash, không subscribe, hay quên gọi lại, tàu vẫn rời bến. Bước 2 chỉ là lịch sự với animation khách đi bộ.

**`ForcedDepartGraceSeconds = 3f` là giây THỰC, cố ý KHÔNG chia `debugTimeScale`** — khách đi bộ về tàu cần thời gian thật; nếu chia scale 60 thì cửa sổ chỉ còn 0.05s, vô nghĩa. Đây là hằng kỹ thuật trong code, không phải tuning knob.

**Idempotent:** `ReportVisitorsAllAboard` gọi **sau** khi đã bị ép rời → `return` **hoàn toàn êm** (không log, không error) nhờ cờ `_departForcedByTimeout`. Gọi trùng ở tình huống thường vẫn log `Debug.Log` mức thông tin như cũ.

**Không double-fire:** sau khi ép rời, state không còn `Docked` ⇒ `IsDockTimedOut` trả `false` ⇒ không kích lần hai. Cờ `_timeoutNoticed` / `_departForcedByTimeout` được dọn khi chuyến MỚI cập bến (`JustDocked`), nên chuyến sau đếm giờ đậu lại từ 0.

**Boot-safe:** `UpdateDockTimeout` cũng chờ qua `_readyFrame + 1` như `FlushPendingDockedEvents` → `OnDockTimeoutForced` không bắn trước khi Dev B kịp subscribe. Trường hợp load vào giữa pha `Docked` đã quá hạn từ lâu (tắt game 3 ngày): frame đầu hợp lệ bắn cảnh báo, 3 giây sau tàu rời bến — Dev B có đúng cửa sổ đó để resolve khách TimedOut.

**Việc cho Dev B:** nghe `OnDockTimeoutForced(dockIndex)` → cho mọi khách chưa xong đi thẳng về tàu (icon buồn, không thưởng) → gọi `ReportVisitorsAllAboard` nếu kịp. **Không kịp cũng không sao.**

### 1.8 `Assets/_Game/Farm/Editor/TouristBoatDiagnosticTool.cs` — vá cho V2 (đợt 2, Lead giao)

Giữ **nguyên tên 3 menu** (6/7/8) để Sếp không phải học lại. Nội dung sửa:

| Chỗ | V1 (cũ) | V2 (đã vá) |
|---|---|---|
| Menu **7. Test Ngay** | reflection vào field private `_anchorTicks` rồi tự tính `hide + travel + 1s` — V2 **không còn field đó** ⇒ fail êm, không làm gì | Gọi API chính thức **`mgr.EditorForceDockNow(0)`**; nếu tàu đã đậu thì báo "đang đậu sẵn". In luôn `EditorDescribeState(0)` sau khi ép, và nhắc `EditorForceDepartNow(0)` để xem tàu rời bến. **Không còn `using System.Reflection`.** Ép cập bến qua đường này bắn `OnBoatDocked` thật ⇒ Dev B spawn khách luôn, test được cả luồng khách. |
| Menu **8. Xóa Save Tàu** | chỉ xóa `TouristBoat_IntroDone`, `TouristBoat_Unlocked_{i}`, `TouristBoat_AnchorUtc_{i}` | Xóa thêm (tên key **đọc trực tiếp từ code Dev B/C**, không đoán): `TouristBoat_V2_State_{i}` · `TouristBoat_V2_Anchor_{i}` · `TouristBoat_V2_NextArrival_{i}` · `TouristBoat_ScheduleVersion` (Dev A) · **`TouristTrip_{i}`** (`TouristVisitorManager.KeyTripFormat` — Dev B) · **`TouristBoat_DaBaoChuyen_{i}`** (`BoatAnnouncePopupUI.KeyDaBaoFormat` — Dev C). Đếm và báo số key thực sự đã xóa. Lý do phải xóa đủ: xóa nửa vời để lại **khách mồ côi** của chuyến đã xóa, hoặc popup Dev C im vì tưởng đã báo rồi. |
| Hiển thị trạng thái | "còn X tới mốc kế" tính bằng modulo chu kỳ V1 (vô nghĩa ở V2) | Mô tả **state V2 thật**: pha hiện tại · WaitingNext còn bao lâu tới giờ cập bến · Arriving/Departing đi được bao nhiêu % path · Docked **đã đậu bao lâu / tối đa bao lâu (lưới an toàn)** · chuyến kế lúc mấy giờ · **chuyến vừa rồi có bị ép rời do quá giờ không**. Chạy được cả Edit Mode (đọc thẳng prefs V2) lẫn Play Mode (hỏi manager). |
| Mục `[2] Config` | in `dockMinutes` / `hideMinutes` | in số V2: gap 5/10 phút, so le 3 phút, `maxDockMinutes`, `patienceMinutes`; ghi rõ 3 field V1 không còn dùng. **Cảnh báo mới**: `maxDockMinutes < patienceMinutes` → báo lệch config ngay trong kết luận. |
| Mục `[4] Save` | in anchor V1 | in state V2 + mốc + chuyến kế của từng bến, kèm tình trạng save khách (Dev B) và cờ popup (Dev C) — nhìn 1 chỗ biết cả 3 hệ. |
| Kết luận `[9]` | "tàu đang trong pha Hidden (núp ở điểm mù)" | "tàu đang CHỜ CHUYẾN KẾ ở điểm mù" (đúng ngôn ngữ V2). |

**API Editor bổ sung trong `BoatDockManager`** (đều `#if UNITY_EDITOR`, không có trong build player):
`EditorForceDockNow(int)` · `EditorForceDepartNow(int)` · `EditorDescribeState(int)` (đã làm giàu: pha · mốc · giờ đậu đã trôi / giới hạn lưới an toàn · chuyến kế còn bao lâu · cờ bị ép rời) · `EditorIsDepartForcedByTimeout(int)` · `EditorDockedElapsedSeconds(int)` · `EditorMaxDockSeconds()`.

---

## 2. Field config mới + default CẦN SET trong asset

`Assets/_Game/ScriptableObjects/TouristBoatConfig.asset` hiện **chưa có** các field mới. Unity sẽ tự thêm với giá trị mặc định của C# khi mở asset — nhưng vì asset là YAML cũ, **số sẽ về 0 với vài field nếu Unity deserialize thiếu**, nên hãy kiểm tra & set tay theo bảng:

| Field | Default cần có | Ghi chú |
|---|---|---|
| `gapOneDockMinutes` | **5** | 1 bến mở |
| `gapMultiDockMinutes` | **10** | ≥2 bến mở |
| `minStaggerMinutes` | **3** | so le tối thiểu |
| `maxDockMinutes` | **30** | **MỚI sau QA** — lưới an toàn chống kẹt; 0 = tắt |
| `visitorsMin` | **3** | Dev B |
| `visitorsMax` | **6** | Dev B |
| `patienceMinutes` | **30** | Dev B |
| `rewardIngredientMultiplier` | **2** | Dev B |
| `disembarkInterval` | **0.8** | giây |
| `visitorWalkSpeed` | **150** | unit/giây — **cần Sếp/Dev B canh lại theo scale scene** (map toạ độ lớn) |
| `queueSpacing` | **120** | unit world — **REVIEW theo cỡ nhân vật NVGAME thật** |
| `bubbleScaleInTime` | **0.25** | giây |
| `smileyFlyTime` | **1.2** | giây |
| `dockMinutes` / `hideMinutes` / `staggerMinutes` | giữ nguyên 40 / 15 / 12 | **V2 không dùng** — đừng xóa (serialize cũ + diagnostic tool) |

Cách nhanh: mở asset trong Inspector, chuột phải component → **Reset** là **KHÔNG** nên (mất `introDialogue` đã chỉnh); hãy set tay 13 dòng trên.

---

## 3. Các bước Unity thủ công (Sếp / lead)

1. **Copy file** từ `deliver/devA/` đè lên dự án theo đúng đường dẫn tương đối (4 file script + 1 file Editor mới + 1 file test).
2. Mở Unity, đợi compile. Console phải **0 lỗi đỏ**.
3. Mở `Assets/_Game/ScriptableObjects/TouristBoatConfig.asset` → điền 13 field ở mục 2 → Ctrl+S.
4. Mở scene farm → menu **`Tools/Farm Game/Tourist Boat/Dịch bến sát bờ`** → bấm **Tự suy hướng bờ** (mặc định 150 unit) → **ÁP DỤNG cho 3 bến** → nhìn scene, chỉnh tay lần cuối cho tàu sát mép bờ (**bước REVIEW**) → Ctrl+S.
5. Chạy menu **`10. Canh Tau Vao O Dau`** để tàu snap về chỗ đậu mới trong Edit Mode.
6. (Tùy chọn) Trên component `Boat/TouristBoatController`: bỏ tick **Show Docked Label** nếu muốn ẩn hẳn chữ trên tàu khi đậu (Dev C có UI riêng thì nên tắt).
7. Test nhanh: đặt `debugTimeScale = 60` trong config → Play → lên Lv10 xem intro + tàu vào bến; gọi `EditorForceDepartNow` (hoặc để Dev B báo khách lên tàu) để xem chuyến kế được lên lịch đúng 5 phút.
8. **Save cũ:** không cần xóa PlayerPrefs — migrate tự động (tàu cập bến sau ~30 giây ở lần vào game đầu tiên). Muốn diễn lại intro từ đầu thì dùng menu **8. Xóa Save Tàu** (bản vá mới xóa đủ save của cả Dev A/B/C — xem mục 1.8).

---

## 4. Rủi ro còn lại

1. ~~`TouristBoatDiagnosticTool.cs` còn nghĩ theo V1~~ → **ĐÃ XỬ LÝ ở đợt 2** (mục 1.8): bỏ reflection, xóa save đủ 3 dev, hiển thị state V2 thật. Rủi ro còn lại rất nhỏ: **tên key của Dev B/C được hardcode trong tool** (`TouristTrip_{i}`, `TouristBoat_DaBaoChuyen_{i}`) — nếu 2 bạn đó đổi tên key thì menu 8 xóa thiếu trở lại. Đã ghi comment chỉ đúng hằng nguồn (`TouristVisitorManager.KeyTripFormat`, `BoatAnnouncePopupUI.KeyDaBaoFormat`) để dễ dò.
2. **Thứ tự subscribe event lúc boot.** `OnBoatDocked` phát sinh khi resolve-load được hoãn ~2 frame sau `IsReady`. Nếu `BootRoutine` của Dev B subscribe **muộn hơn 2 frame** (vd chờ manager khác) thì có thể lỡ event. **Khuyến nghị Dev B**: sau khi subscribe, luôn kiểm tra `IsDocked(i)` một lượt cho 3 bến để tự khôi phục — đây cũng là đường xử lý bắt buộc cho trường hợp save = Docked từ phiên trước.
3. **`_scheduleTravelSeconds` = max travel của 3 bến** (giữ quyết định m-3 của V1). Bến có path ngắn hơn sẽ thấy tàu trôi chậm hơn `boatSpeed` danh nghĩa. Nếu Sếp muốn mỗi bến chạy đúng tốc độ riêng thì đổi 1 dòng trong `FindSceneReferences` — nhưng khi đó tiến độ trên path và lịch vẫn khớp (V2 tính theo arrival tuyệt đối, không còn ràng buộc "3 bến chung chu kỳ" như V1).
4. **`visitorWalkSpeed` / `queueSpacing` mặc định là số phỏng đoán** theo scale map (~740 unit giữa 2 bến). Dev B phải canh lại bằng mắt trong scene — tôi ghi mặc định để hệ không chia 0, không phải giá trị thiết kế.
5. **Gangplank + `TouristQueueAnchor`** không thuộc phạm vi tôi — manager chỉ cung cấp `GetDockBerth(i)` + `OnBoatDocked`/`OnBoatDeparting` để Dev B gắn vào.
6. **`maxDockMinutes` và `patienceMinutes` là 2 field rời** (cùng default 30). Nếu ai đó chỉnh `patienceMinutes` lên 45 mà quên `maxDockMinutes` thì lưới an toàn sẽ cắt chuyến sớm hơn mốc kiên nhẫn — khách đang chờ sẽ bị đuổi về tàu. Quy ước: **`maxDockMinutes` ≥ `patienceMinutes`**, ghi luôn trong Tooltip.
7. **Popup "sau 0 phút"**: khi vừa mở bến, tàu vào ngay nên `OnNextTripScheduled` bắn với số phút 0. Dev C cần bỏ qua khi `< 1` (đúng GDD §3.5), tôi vẫn bắn để Dev C tự quyết.

---

## 5. Câu hỏi mở (cần lead/Sếp chốt)

1. ~~Có cần lưới an toàn "đậu quá X phút thì tự rời bến"?~~ → **CHỐT (Sếp duyệt 2026-08-29): CÓ**, `maxDockMinutes = 30`, xem mục 1.7.
   - ~~3 giây ân hạn có đủ không?~~ → **CHỐT (Lead 2026-08-29): GIỮ NGUYÊN 3 giây.** `ForcedDepartGraceSeconds = 3f` trong `BoatDockManager`, giây thực, không chia `debugTimeScale`. Muốn đổi sau này chỉ sửa 1 hằng số.
   - ~~Sàn `patienceMinutes >= 1` trong `OnValidate` có nên nới không?~~ → **CHỐT (Lead 2026-08-29): GIỮ NGUYÊN sàn 1 phút.** Không cho đặt 0 để tránh khách hết kiên nhẫn ngay lúc bubble vừa mở (Dev B sẽ resolve TimedOut trong cùng frame — nhìn như bug). Muốn test nhanh thì dùng `debugTimeScale`, đúng đường đã thiết kế.
2. **Số hiệu tàu vs số bến**: hiện `BoatNumber = dockIndex + 1` (Dock 1 → "Tàu số 01") đúng GDD. Nếu sau này muốn tên tàu riêng (không theo bến) thì cần bảng tên trong config.
3. **`minStaggerMinutes = 3` với gap 5 phút và 3 bến**: về lý thuyết 3 arrival cách nhau ≥3 phút trong chu kỳ ~10 phút là vừa đủ; nhưng nếu cả 3 bến rời bến gần nhau, luật dời-muộn có thể đẩy bến thứ 3 trễ thêm ~6 phút so với gap danh nghĩa. Chấp nhận được chứ, hay muốn kẹp trần "dời tối đa X phút"?
4. **Chữ trên tàu khi đậu**: mặc định tôi để `"Đang đón khách..."`. Nếu Dev C làm UI trạng thái chuyến riêng thì nên tắt (`showDockedLabel = false`) — Sếp chốt giúp để tránh 2 chỗ hiện cùng thông tin.

---

## 6. Chạy lại bộ test (QA)

```bash
cd <repo>
mcs -out:/tmp/boattests.exe \
    Assets/_Game/Farm/Scripts/TouristBoat/BoatScheduleCore.cs \
    tests/unit/touristboat/BoatScheduleCoreTests.cs
mono /tmp/boattests.exe ; echo "exit=$?"
```
Kết quả mong đợi: `TỔNG KẾT: 119 PASS · 0 FAIL`, `exit=0`.
(Đã chạy thật ở máy tôi với `mono-mcs`; ngoài ra **cả 6 file** — 4 runtime + `BoatShoreAdjustTool` + `TouristBoatDiagnosticTool` — được kiểm cú pháp bằng stub `UnityEngine`/`TMPro`/`UnityEditor`: **compile sạch, 0 error** khi bật `UNITY_EDITOR`.)
