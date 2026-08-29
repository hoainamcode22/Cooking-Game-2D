# GDD — Tourist Boat System (Bến Tàu Du Lịch)

> **Story:** BOAT-001 · **Mở khóa:** Level 10 · **Trạng thái:** Approved (Sếp duyệt 2026-08-13)
> **Nguồn quyết định:** hội thoại với chủ dự án + phân tích economy-designer (Phương án A)

---

## 1. Overview

Sau khi đạt Level 10, một hội thoại mở khóa **bến tàu du lịch** ở bãi biển. Tàu du lịch
xuất phát từ **điểm mù** ngoài khơi, chạy thẳng vào bến, **đậu 40 phút** (du khách tham quan
— V1 chỉ ambience, art nhân vật bổ sung phase 2), rồi lùi thẳng ra và quay về điểm mù
**núp 15 phút**, lặp lại vô hạn. Có 3 bến: bến 1 miễn phí (L10), bến 2 mở bằng vàng (L12),
bến 3 mở bằng gem (L14). Nhiều bến mở → tàu đến **so le 10–15 phút**, biển luôn sống động.

## 2. Player Fantasy

"Nông trại của mình nổi tiếng đến mức khách du lịch đi tàu đến thăm!" — cảm giác thành tựu,
thế giới sống động, luôn có gì đó chuyển động ngoài biển. Hợp 4 trụ cột: JUICY (tàu dập dềnh,
cập bến), BUILD LIÊN TỤC (teaser bến kế + đếm ngược tàu kế), DỄ THÂN THIỆN (tự động, không phạt),
PHẢN HỒI NGẮN (countdown rõ, tàu đến ngay khi mở bến).

## 3. Detailed Rules

### 3.1 Mở khóa
- Đạt **L10** (nghe `FarmLevelManager.OnLevelChanged`, kiểm tra `HasReached(10)`, kể cả nhảy cóc level):
  1. Hội thoại 4 câu trên guide board (skip được từng câu bằng tap).
  2. Camera zoom tới bến (tái dùng cơ chế TutorialCameraFocus/CameraController).
  3. `UnlockDockFree(0)` → tàu 1 xuất phát từ điểm mù vào bến ngay.
  4. Trả camera về người chơi. Hội thoại chỉ chạy **1 lần** (persist flag).
- **Bến 2**: nút mở khóa hiện teaser từ khi thấy bến; bấm được khi **L12**, giá **2.000 vàng**.
- **Bến 3**: yêu cầu **L14**, giá **25 gem**. Trừ tiền qua `FarmEconomyManager.SpendGold/SpendGems`
  (API tự từ chối nếu không đủ — UI phải hiện lý do: thiếu level / thiếu tiền).
- Mở bến thành công → tàu của bến đó xuất phát ngay (dopamine), trừ khi vi phạm luật so le (§3.3).

### 3.2 Vòng đời tàu (mỗi bến 1 tàu riêng)
`Hidden(15p tại điểm mù) → Arriving(chạy thẳng waypoint vào bến, mũi hướng bến) →
Docked(40p, hiện countdown) → Departing(LÙI thẳng ra khỏi bến rồi chạy về điểm mù) → Hidden(...)`
- Di chuyển theo waypoint per bến (điểm mù → … → berth). Departing đi ngược chuỗi waypoint
  (lùi — không quay đầu, đúng yêu cầu "đi thẳng và lùi").
- Visual: bob dập dềnh + flip theo hướng (tái dùng cách làm của FerryController).
- Ở trạng thái Hidden: tàu SetActive(false) tại điểm mù (kỹ thuật ẩn của TrainState).

### 3.3 Lịch so le (nhiều bến)
- Mốc thời gian tuyệt đối UTC. Mỗi bến có `anchorUtc` (thời điểm bắt đầu chu kỳ).
- Khi mở bến mới: nếu thời điểm cập bến dự kiến cách lần cập bến gần nhất của bến khác
  **< staggerMinutes (mặc định 12p)** → dời anchor để đủ khoảng cách. Không bao giờ 2 tàu
  vào cùng 1 bến (mỗi bến 1 tàu riêng).

### 3.4 Persist & offline catch-up
- Lưu PlayerPrefs (nhất quán codebase hiện tại; migrate SaveSystem khi M0-2 xong):
  `TouristBoat_Unlocked_{i}` (bool), `TouristBoat_AnchorUtc_{i}` (long ticks),
  `TouristBoat_IntroDone` (bool).
- Load: tính trạng thái hiện tại từ `(nowUtc - anchorUtc) % cycleDuration` — tắt game 3 tiếng
  mở lại tàu đúng pha, không tua tay từng giây.
- Đồng hồ máy bị chỉnh lùi: chỉ reset anchor = now khi `anchor − now > 1 chu kỳ` (rollback thật).
  Anchor tương lai ≤ 1 chu kỳ là HỢP LỆ (hệ quả của luật so le §3.3) — tàu chờ ở Hidden tới lượt,
  không reset. *(Cập nhật sau QA vòng 2 — fix B-1.)*

## 4. Formulas

```
travelTime       = pathLength / speed                    (giây, theo waypoint thực)
cycleDuration    = hideMinutes*60 + travelTime + dockMinutes*60 + travelTime
phase(t)         = (nowUtc - anchorUtc) trong [0, cycleDuration), lặp modulo
state(phase):    [0, hide)                        → Hidden
                 [hide, hide+travel)              → Arriving  (tiến độ = (phase-hide)/travel)
                 [hide+travel, hide+travel+dock)  → Docked    (countdown = mốc kế - phase)
                 còn lại                          → Departing (đi lùi)
Giá bến:  dock1 = free@L10 · dock2 = 2000 vàng@L12 · dock3 = 25 gem@L14   (config)
So le:    |arrivalUtc(bến mới) - arrivalUtc(gần nhất)| ≥ staggerMinutes → nếu vi phạm, anchor += phần thiếu
```

Mặc định (đều là tuning knobs): `dockMinutes=40, hideMinutes=15, staggerMinutes=12, speed=300 unit/s`.

## 5. Edge Cases

| # | Tình huống | Xử lý |
|---|-----------|-------|
| 1 | Lên nhiều level 1 lúc vượt qua 10 (nhận thưởng dồn) | `HasReached(10)` + flag → hội thoại chạy đúng 1 lần |
| 2 | Thoát game giữa lúc tàu đang Arriving/Departing | Trạng thái suy ra từ anchor+phase → vào lại đúng vị trí trên path |
| 3 | PlayerPrefs bị xóa nhưng level ≥ 10 | Session sau: intro flag mất → hội thoại chạy lại (chấp nhận, vô hại) |
| 4 | Đồng hồ máy chỉnh lùi | Lùi > 1 chu kỳ → anchor = now; lệch ≤ 1 chu kỳ (kể cả anchor tương lai do so le) → giữ nguyên, tàu chờ Hidden |
| 5 | Bấm mở bến khi thiếu vàng/level | Nút disable + tooltip lý do; SpendGold trả false → không mở |
| 6 | Mở bến trong lúc bến khác đang có tàu đậu | Vẫn mở; tàu mới tôn trọng luật so le §3.3 |
| 7 | Reload scene / đổi scene farm↔cooking | Manager re-init từ PlayerPrefs, idempotent |
| 8 | Chưa gắn sprite tàu (trước khi Sếp gắn art) | Tool sinh placeholder trắng đơn giản → game vẫn chạy, không NRE |

## 6. Dependencies

- `FarmLevelManager` (OnLevelChanged, HasReached) — chỉ đọc, không sửa.
- `FarmEconomyManager` (SpendGold, SpendGems, OnCurrencyChanged) — chỉ gọi API.
- `TutorialGuideBoardUI` / cơ chế camera focus — tái dùng cho intro (không sửa file gốc).
- `FerryController` — tham khảo pattern bob/flip (KHÔNG đụng vào ferry hiện có).
- PlayerPrefs (persist tạm) → sẽ migrate khi M0-2 SaveSystem hoàn thành.
- **KHÔNG sửa bất kỳ file code hiện có nào** — 100% additive (AUTONOMY.md §2).

## 7. Tuning Knobs (tất cả trong `TouristBoatConfig` ScriptableObject)

`dockMinutes, hideMinutes, staggerMinutes, boatSpeed, bobAmplitude, bobFrequency,
dock2Level, dock2GoldCost, dock3Level, dock3GemCost, unlockLevel (10), introDialogue[4],
debugTimeScale` (tua nhanh để test — chỉ hoạt động trong Editor/Development build).

## 8. Acceptance Criteria

1. L10 → hội thoại 4 câu → camera zoom → tàu 1 vào bến. Chạy đúng 1 lần, skip được.
2. Tàu đậu đúng `dockMinutes` (40p), có countdown nhìn thấy; rời bến bằng cách LÙI thẳng rồi về điểm mù; núp đúng `hideMinutes` (15p); lặp vô hạn.
3. Bến 2 chỉ mở được ở L12 với 2.000 vàng; bến 3 ở L14 với 25 gem; thiếu điều kiện → từ chối rõ ràng, không trừ tiền.
4. 2–3 bến mở → khoảng cách giữa 2 lần cập bến bất kỳ ≥ staggerMinutes.
5. Tắt game ở mọi trạng thái, mở lại sau X phút → tàu đúng pha (offline catch-up).
6. 0 lỗi đỏ console; không alloc mỗi frame trong Update (giữ 60fps máy tầm trung).
7. Editor Tool sinh đủ hierarchy + waypoints + config + placeholder; Sếp chỉ cần kéo sprite thật + tinh chỉnh waypoint bằng gizmo.
8. Test tự động (pure C# core) pass 100%: state machine, so le, offline catch-up, giá mở bến.
