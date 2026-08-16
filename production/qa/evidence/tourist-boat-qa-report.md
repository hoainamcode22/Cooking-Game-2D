# QA Report — Tourist Boat System (BOAT-001)

> **QA:** qa-tester · **Vòng 1:** 2026-08-13 · **Vòng 2 (regression sau fix):** 2026-08-13
> **Spec:** `design/gdd/tourist-boat-system.md` (Approved 2026-08-13)
> **Phạm vi:** 7 file code Tourist Boat + đối chiếu 4 file API gốc (FarmLevelManager, FarmEconomyManager, LuuGopPrefs, CameraController)
>
> **VERDICT CUỐI (vòng 2): ✔ SHIP** *(code-side — kèm checklist Play Mode §6 trước khi release build)*
> Vòng 1: ✗ FIX FIRST (1 BLOCKING + 1 MAJOR) → cả 6 finding đã được dev fix, QA verify từng fix bằng test chạy thật + compile-check + đọc code. **Vòng 2: 94 PASS / 0 FAIL.**

---

## 0. Tóm tắt số liệu

| Hạng mục | Vòng 1 | Vòng 2 (regression) |
|---|---|---|
| Unit test tự động (mono, chạy thật) | 80 PASS / **1 FAIL** (81) | **94 PASS / 0 FAIL** (94 — thêm 13 test regression E + sửa 2 test C10 mô phỏng guard mới) |
| Compile-check 7 file + 4 file API thật | 0 error / 0 warning | **0 error / 0 warning** (verify merge: Dev B gọi `IsReady` khớp bản mới Dev A) |
| BLOCKING | 1 (B-1) | **0 — B-1 RESOLVED** |
| MAJOR | 1 (M-1) | **0 — M-1 RESOLVED** |
| MINOR | 3 (m-1, m-2, m-3) | **0 — cả 3 RESOLVED** |
| NOTE còn mở | 9 | **7** (n-1, n-2 RESOLVED; thêm n-10 mới — doc-only) |

Harness: `tests/unit/touristboat/BoatScheduleCoreTests.cs`
Log vòng 1: `production/qa/evidence/tourist-boat-test-output.log` · Log vòng 2: `tourist-boat-test-output-round2.log`
```
mcs Assets/_Game/Farm/Scripts/TouristBoat/BoatScheduleCore.cs \
    tests/unit/touristboat/BoatScheduleCoreTests.cs -out:boat_tests.exe && mono boat_tests.exe
```
Config test = default GDD §4: hide 900s · dock 2400s · stagger 720s · travel giả định 60s · cycle 3420s.

---

## 1. VÒNG 2 — Trạng thái từng finding

### BLOCKING

**B-1 · Guard đồng hồ lùi phá luật so le khi anchor bị đẩy vào tương lai — ✅ RESOLVED**
- **Fix của dev:** hàm mới `BoatScheduleCore.IsClockRolledBack(now, anchor, cycleSeconds)` (`BoatScheduleCore.cs:258-261`) — chỉ coi là đồng hồ lùi khi `anchor − now > 1 chu kỳ`. Áp dụng đủ **3 chỗ**: `Update()` (`BoatDockManager.cs:156`), `LoadFromPrefs()` (`:474`), và `TryGetPhaseInfo()` truyền anchor NGUYÊN VẸN không `SanitizeAnchor` (`:330-334`). Grep xác nhận manager không còn chỗ nào dùng `IsAnchorInFuture`/`SanitizeAnchor`.
- **QA verify bằng test chạy thật:**
  - `C10g/C10h` (kịch bản vòng 1 từng FAIL): mở bến 2 khi tàu bến 1 cập sau 8p → anchor tương lai +240s được **giữ nguyên**, gap thực = **720s** (vòng 1: 480s). PASS.
  - `E1a-f` (biên theo yêu cầu lead): anchor = now + **đúng 1 cycle** → KHÔNG rollback; **+1 cycle +1s** (và +1 tick) → LÀ rollback; anchor quá khứ → không; cycle âm → kẹp 0, không exception. PASS cả 6.
  - `E2a`: quét 6 mốc "bến 1 sắp cập trong <12p" (1/3/5/8/10/11.9 phút) → gap luôn ≥ 720s, guard không reset oan mốc nào. PASS.
  - `E2b`: quét ~200 tổ hợp worst-case với 2 bến khác → anchor sau so le **không bao giờ** vượt now quá 1 cycle → guard mới không bao giờ đụng anchor hợp lệ. PASS (xác nhận dung sai 1 cycle là đủ về mặt toán).
  - `E4a/b`: đồng hồ lùi THẬT (anchor vượt 2 cycle) vẫn bị phát hiện; trước khi reset ComputePhase vẫn Hidden an toàn. PASS.

### MAJOR

**M-1 · UnlockFlow không đợi manager load prefs — ✅ RESOLVED**
- **Fix của dev:** `public bool IsReady` (`BoatDockManager.cs:94`, bật tại `:127` SAU `LoadFromPrefs`); `BootRoutine` đợi `Instance != null && Instance.IsReady` (`TouristBoatUnlockFlow.cs:84-97`, có timeout 8s + warning rõ khi thiếu config). `IsReady` cũng gate `Update` (:140), `CanUnlockDock` (:202), `UnlockDockFree` (:273), `TryGetPhaseInfo` (:318).
- **QA verify:** đọc code — intro không thể đọc `IsIntroDone` giả-false trước load; nhánh "UnlockDockFree no-op im lặng rồi MarkIntroDone" không còn đường vào (BootRoutine chặn từ đầu). Verify hành vi cuối trong Play Mode (§6).

### MINOR

**m-1 · Race tap bến 1 trước/trong intro — ✅ RESOLVED** — guard `if (dockIndex == 0 && !mgr.IsIntroDone) return;` tại `BoatDockSlot.cs:109`. Đúng đề xuất QA.

**m-2 · Alloc mỗi frame khi path suy biến — ✅ RESOLVED** — `EnsurePathBuffers(count)` (`TouristBoatController.cs:268-275`) tái dùng buffer, chỉ alloc khi kích thước đổi; nhánh suy biến GIỮ buffer, retry 0-alloc (`:259-262`). Đọc lại toàn bộ Update path: không còn alloc steady-state nào; countdown vẫn 1 string/giây.

**m-3 · So le bào mòn do travel lệch giữa bến — ✅ RESOLVED (quyết định lead)** — `_scheduleTravelSeconds = max(travel 3 bến)` (`BoatDockManager.cs:54, 544-545`), dùng đồng nhất ở `Update` (:143-144), `TryGetPhaseInfo` (:333), `RefreshDockState` (:428), `UnlockInternal` (:394, :405) → 3 chu kỳ bằng nhau tuyệt đối.
- **QA verify:** test `E3a-c` — gap so le tại chu kỳ 0 / 100 / 1000 y hệt nhau (720s, sai số 0) → **so le giữ vĩnh viễn**, AC §8.4 giờ đúng cả dài hạn. Toàn bộ test cũ A–D không vỡ (đều dùng travel đồng nhất). Trade-off hình ảnh (tàu path ngắn trôi chậm hơn boatSpeed danh nghĩa) đã ghi rõ trong comment — xác nhận bằng mắt ở Play Mode.
- Lưu ý nhỏ (không chặn): `GetTravelSeconds(dockIndex)` giờ chỉ mang nghĩa tham khảo/debug — XML doc đã ghi rõ, API contract không đổi.

### NOTE — đã đóng

**n-1 · Emoji trong text runtime — ✅ RESOLVED** — scan Unicode toàn bộ string literal 7 file: **0 emoji/glyph đặc biệt trong text runtime**. `💎` → "Kim Cương" (`BoatDockSlot.cs:171`); `▸` bỏ khỏi hint (`TouristBoatUnlockFlow.cs:304`); 🎉🍳 bỏ khỏi dialogue. Còn lại CHỈ trong Editor-only: `✅`/`•`/`├─` trong `EditorUtility.DisplayDialog` của tool (render bằng font OS, không vào build người chơi) và `→` trong `[Tooltip]` (Inspector-only) — chấp nhận.

**n-2 · Hai bộ introDialogue default — ✅ RESOLVED** — single source tại `TouristBoatConfig.cs:69-75` (bản không emoji); tool không còn mảng `IntroDialogue`, `LoadOrCreateConfig`/`ApplyGddDefaults` không đụng `introDialogue` (verify grep + đọc `TouristBoatSetupTool.cs:255-300`).

### NOTE — còn mở (không chặn ship)

| # | Nội dung | Vị trí | Hướng xử lý |
|---|---|---|---|
| n-3 | Hidden chỉ tắt child `Visual`, không SetActive(false) cả tàu — lệch câu chữ GDD §3.2, tương đương về hình ảnh | `TouristBoatController.cs:107-114` | Chấp nhận / sửa GDD |
| n-4 | `staggerMinutes > cycle/2` vô nghiệm → trả anchor không thỏa, không warning; OnValidate không chặn | `BoatScheduleCore.cs:368-382`, `TouristBoatConfig.cs` | Default an toàn; cân nhắc clamp trong OnValidate |
| n-5 | `debugTimeScale > 1`: so le tính theo thời gian thực (comment đã ghi nhận, release scale 1) | `BoatDockManager.cs:398-402` | Chấp nhận (debug-only) |
| n-6 | Tool wire field qua `FindProperty(...)` không null-check (NRE Editor nếu đổi tên field) — hiện khớp 100% | `TouristBoatSetupTool.cs:195-201` | Nice-to-have |
| n-7 | `CinematicFocus` kẹp target vào bounds camera — BoatSystem đặt ngoài bounds thì intro zoom không tới bến | `CameraController.cs:536` | **CẦN BẠN — Unity** khi layout thật |
| n-9 | `textWrappingMode`/`TextWrappingModes` là API TMP mới (uGUI 2.x/Unity 6) — đúng version project, xác nhận lần import đầu | `BoatDockSlot.cs:234`, Tool | **CẦN BẠN — Unity** |
| n-10 **(mới, doc-only)** | Fix B-1 làm hành vi lệch câu chữ GDD §3.4 ("anchor > now: reset = now"): giờ đồng hồ lùi ≤ 1 chu kỳ (~57p) KHÔNG reset — tàu chờ Hidden tối đa ~1 chu kỳ rồi tự đúng pha. Hành vi này ĐÚNG hơn spec (spec cũ chính là nguồn bug B-1) | GDD §3.4 | Đề nghị cập nhật 1 câu trong GDD |

n-8 (camera restore nhanh hơn lúc lia đi, trả input ngay) — giữ nguyên, đã chấp nhận từ vòng 1 (giống TutorialCameraFocus).

---

## 2. VÒNG 2 — Kết quả test regression mới (section E + C10 sửa)

| Test | Nội dung | Expected | Actual | KQ |
|---|---|---|---|---|
| C10g | Guard mới: anchor tương lai ≤ 1 cycle được giữ nguyên (không reset) | giữ nguyên | giữ nguyên | **PASS** |
| C10h | Kịch bản B-1 vòng 1 (bến 1 cập sau 8p): gap thực sau guard mới | ≥ 720s | **720s** (vòng 1: 480s) | **PASS** |
| E1a | anchor = now + đúng 1 cycle → không rollback (biên chạm) | False | False | **PASS** |
| E1b | anchor = now + 1 cycle + 1s → rollback | True | True | **PASS** |
| E1c | anchor = now + 1 cycle + 1 tick → rollback (biên chặt) | True | True | **PASS** |
| E1d | anchor quá khứ → không rollback | False | False | **PASS** |
| E1e | anchor tương lai 240s (so le đẩy) → không rollback | False | False | **PASS** |
| E1f | cycleSeconds âm → kẹp 0, không exception | True | True | **PASS** |
| E2a | 6 mốc "bến 1 cập trong <12p": gap luôn ≥ 720s, không reset oan | all ≥ stagger | all ≥ stagger | **PASS** |
| E2b | ~200 tổ hợp worst-case 2 bến khác: anchor ≤ now + 1 cycle | never flagged | never flagged | **PASS** |
| E3a-c | m-3 chu kỳ đồng nhất: gap tại chu kỳ 0/100/1000 y hệt (720s) | không trôi | không trôi | **PASS** |
| E4a-b | Đồng hồ lùi thật (+2 cycle): phát hiện đúng, ComputePhase an toàn | đúng | đúng | **PASS** |

Toàn bộ test vòng 1 (A/B/C/D — 79 test còn lại) chạy lại nguyên trạng: **PASS 100%** → fix không phá hành vi cũ (bao gồm: hành trình L1→L30, biên vòng đời ±1s, offline catch-up, timeScale, C12 mở 3 bến, determinism).

---

## 3. Kết quả vòng 1 (lịch sử — đầy đủ trong log vòng 1)

<details>
<summary>Vòng 1: 80 PASS / 1 FAIL — bảng chi tiết</summary>

- **A. Hành trình L1→L30** (A1–A5): 10/10 PASS — 27 tổ hợp L1-L9 deny level; L10 bến 1 free; ranh giới 1.999/2.000 vàng, 24/25 gem; L15→L30 ổn định 48 tổ hợp.
- **B. Vòng đời & offline** (B6–B9): 37/37 PASS — biên ±1s tại 900/960/3360/3420s; tiến độ tuyến tính; offline 3d+17p + 5 mốc tính tay; đồng hồ lùi an toàn; scale=60 khớp scale=1.
- **C. So le** (C10–C12): 12/13 — **C10h FAIL** (bug B-1: gap 480s < 720s sau guard cũ); C12 mở 3 bến gap ≥ 720s; không treo khi vô nghiệm.
- **D. Edge §5** (D1–D5): 14/14 PASS — nhảy cóc L9→L11; persist round-trip bit-một-bit; 1000 mốc deterministic; travel=0 an toàn; Next/NearestArrival đúng mốc.

Chi tiết lỗi vòng 1 (nguyên văn phân tích B-1/M-1/m-1/m-2/m-3 + 9 NOTE): xem git history của file này hoặc log vòng 1.
</details>

Đối chiếu chữ ký API (không đổi ở vòng 2, compile lại vẫn 0 error): `CinematicFocus(Vector3, float, bool)` / `EndCinematic` / `CurrentPosition` / `CurrentSize` · `SpendGold/SpendGems(int)→bool` / `AddGold` / `Gold/Gems` · `HasReached(int)` / `OnLevelChanged` / `CurrentLevel` · `LuuGopPrefs.Hen()/LuuNgay()` — **tất cả KHỚP**. Hierarchy tool ↔ tên runtime khớp 100% (BoatSystem/BlindPoint/Dock_XX/Berth/Path/WP_XX/Boat/Visual).

---

## 4. Mapping Acceptance Criteria GDD §8 → verdict (vòng 2)

| AC | Nội dung | Vòng 1 | **Vòng 2** | Căn cứ |
|---|---|---|---|---|
| 1 | L10 → hội thoại 4 câu → camera zoom → tàu 1 vào bến; đúng 1 lần, skip được | CẦN BẠN (rủi ro M-1, m-1) | **PASS (logic)** — M-1/m-1 resolved; xác nhận hình ảnh Play Mode | IsReady wait + guard dock 0; flow §3.1 đủ 4 bước |
| 2 | Đậu 40p có countdown; rời bằng LÙI; núp 15p; lặp vô hạn | PASS (logic) | **PASS (logic)** | B6 (94-test run); Departing ngược path không flip |
| 3 | Bến 2 = L12+2.000 vàng; bến 3 = L14+25 gem; thiếu → từ chối, không trừ tiền | PASS | **PASS** | A2–A4; SpendGold/SpendGems thật |
| 4 | 2–3 bến mở → mọi khoảng cách cập bến ≥ staggerMinutes | **FAIL (B-1)** | **PASS** — cả tức thời (C10/C12/E2) lẫn **vĩnh viễn** (E3, nhờ m-3 chu kỳ đồng nhất) | C10g-h, E1–E3 |
| 5 | Tắt game mọi trạng thái, mở lại đúng pha | PASS (logic) | **PASS (logic)** | B7/B8/D2 + guard load mới E1/E4 |
| 6 | 0 lỗi đỏ console; không alloc mỗi frame | CẦN BẠN (m-2) | **PASS (static)** — m-2 resolved, mọi Update path 0-alloc steady-state; xác nhận profiler Play Mode | EnsurePathBuffers |
| 7 | Tool sinh đủ hierarchy + waypoint + config + placeholder | CẦN BẠN — Unity | **CẦN BẠN — Unity** (tên khớp 100% static; chỉ chạy được trong Editor) | §2.2 vòng 1 |
| 8 | Test tự động pure C# pass 100% | 80/81 | **94/94 PASS = 100%** | log round2 |

---

## 5. Verdict cuối

> ## ✔ SHIP (code-side)
>
> - Cả 6 finding vòng 1 (1 BLOCKING, 1 MAJOR, 3 MINOR, 2 NOTE lớn) đã fix và **được verify độc lập**: 94/94 test chạy thật PASS, compile-check 0 lỗi (merge Dev A × Dev B khớp — `IsReady` gọi đúng chữ ký), đọc code xác nhận đủ 6/6.
> - Fix B-1 dùng dung sai 1 chu kỳ được chứng minh an toàn về toán (E2b: so le không bao giờ đẩy anchor quá `now + cycle − hide`), fix m-3 làm AC-4 đúng **vĩnh viễn** thay vì chỉ lúc mở bến.
> - Còn mở: 7 NOTE (không chặn ship; n-10 chỉ là cập nhật 1 câu GDD §3.4).

## 6. Checklist Play Mode trước release build (CẦN BẠN — Unity)

1. AC-1 end-to-end: lên L10 (cả nhảy cóc L9→L11) → intro 4 câu (skip từng câu) → camera zoom → tàu vào → restore; vào lại game xác nhận không replay.
2. AC-6: Profiler — 0 GC alloc/frame trong lúc 3 tàu chạy + 1 tàu Docked hiện countdown; 0 lỗi đỏ console (kể cả khi cố tình chỉnh đồng hồ máy lùi 30p và lùi 2 tiếng).
3. AC-7: chạy tool trên scene thật, kéo BoatSystem ra bãi biển, xác nhận gizmo + camera intro tới đúng bến (n-7 bounds).
4. m-3 visual: tàu bến path ngắn trôi chậm hơn — xác nhận Sếp chấp nhận cảm giác "thong thả".
5. n-9: xác nhận `textWrappingMode` compile trên version TMP của project.

*QA không sửa code dev — chỉ cập nhật harness (`tests/unit/touristboat/`), stub (`/home/claude/work/stubs/`) và report này.*
