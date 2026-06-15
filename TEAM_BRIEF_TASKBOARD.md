# 🧠 BỘ NÃO CHUNG & BẢNG TASK — Farm Game (Level 1 → Polish → Live)

> **Mục đích:** Đây là "bộ não chung" (single source of truth) cho **toàn bộ 49 agent** trong
> studio (`.claude/agents/`). MỌI agent đọc **Phần 1 (Bộ não chung)** trước khi làm bất cứ việc gì,
> rồi nhận task của mình ở **Phần 3 (Bảng task)** + prompt sẵn ở **Phần 4**.
>
> **Chủ sở hữu file này:** `producer` (cập nhật trạng thái task, sprint). Mọi quyết định lớn ghi lại tại đây.
> **Engine:** Unity 6.3 LTS (6000.3.10f1) · C# · URP · New Input System · Addressables.

---

## PHẦN 1 — BỘ NÃO CHUNG (mọi agent phải đọc)

### 1.1 Tầm nhìn game
Game nông trại + nấu ăn 2D casual. **Đối tượng chính: phụ nữ và trẻ em.** Ưu tiên cảm giác
dễ thương, ấm áp, thân thiện, dễ chơi, KHÔNG áp lực, KHÔNG bạo lực.

### 1.2 Bốn trụ cột thiết kế (Design Pillars) — mọi quyết định phải bám theo
1. **JUICY — nhiều hiệu ứng/animation & phản hồi.** Mỗi hành động (trồng, tưới, lớn, thu hoạch,
   nhận xu/EXP, lên cấp, bấm nút) đều phải có animation + âm thanh + particle khiến người chơi "đã mắt".
2. **BUILD LIÊN TỤC — luôn có thứ để xây/trồng/mở khoá.** Vòng lặp ngắn, luôn có mục tiêu kế tiếp,
   không để người chơi "đứng hình" không biết làm gì.
3. **DỄ & THÂN THIỆN — accessible cho trẻ em & người không chơi game.** Nút to, luồng đơn giản,
   chữ rõ, hướng dẫn bằng bàn tay/animation thay vì chữ nhiều.
4. **VÒNG PHẢN HỒI NGẮN (dopamine).** Thưởng sớm, thưởng thường xuyên, hiệu ứng ăn mừng rõ ràng.

### 1.3 Quy tắc làm việc chung (Collaboration Protocol — BẮT BUỘC)
- Theo `CLAUDE.md`: **Question → Options → Decision → Draft → Approval**.
- Hỏi "May I write this to [filepath]?" **trước khi** Write/Edit. Thay đổi nhiều file cần duyệt cả changeset.
- **Không commit** nếu chưa được yêu cầu.
- Mỗi task phải hoàn thành được trong **1–3 ngày**; lớn hơn → `producer` chẻ nhỏ trước.
- Spec mơ hồ → **hỏi**, không đoán (đoán sai đắt hơn 1 câu hỏi).
- Không tự sửa file ngoài domain của mình nếu chưa được lead giao.

### 1.4 Cách cả đội "chung 1 bộ não"
- `producer` điều phối toàn bộ board này; leads (`game-designer`, `lead-programmer`, `art-director`,
  `audio-director`, `qa-lead`, `release-manager`) delegate xuống specialist theo `agent-coordination-map.md`.
- Mọi agent: đọc file này → đọc agent-file của mình (`.claude/agents/<tên>.md`) → đọc docs liên quan
  (`.claude/docs/coding-standards.md`, `technical-preferences.md`, `coordination-rules.md`).
- Kết quả/quyết định/handoff **ghi ngược lại** vào board (mục Status + Notes của task).
- Dùng skills sẵn có trong `.claude/skills/` (vd `/sprint-plan`, `/bug-report`, `/code-review`,
  `/qa-plan`, `/regression-suite`, `/smoke-check`, `/perf-profile`, `/art-bible`, `/ux-review`,
  `/balance-check`, `/release-checklist`, `/localize`, `/playtest-report`).

### 1.5 Trạng thái hiện tại (đã làm xong)
- ✅ **Tutorial L1** đã có: intro mây đẹp (không zoom), camera 1-chủ (cinematic), nền xám bao các ô.
- ✅ **Ô đất: tay quét 8 ô plot_01→plot_08 đúng thứ tự** user gửi (xếp bằng nearest-match theo pos),
  nền xám + camera bao đủ 8 ô.
- ✅ **Chậu hoa: tay quét 6 chậu hoa_01→hoa_06 đúng thứ tự** + zoom/nền xám bao đủ 6 chậu (làm giống ô đất).
- ✅ **Bỏ lỗi 2 bàn tay** (bước L1L2_07): tắt tay tĩnh khi action-guide chạy.
- ✅ **Tay thông minh theo tiến độ**: tay chỉ quét ô CÒN VIỆC (trồng → ô trống; thu hoạch → ô chín),
  user làm xong ô nào tay tự bỏ qua ô đó.
- ✅ **Gate đúng tiến độ (chậm hơn)**: "đã trồng/thu hoạch hết" tính theo SỐ Ô THẬT đã mở khoá
  (8 ô đất / 6 chậu) thay vì cứng 6 → không còn hiện bước kế khi user chưa làm xong. Nền xám vùng
  giờ chỉ là hiệu ứng (không chặn click bảng hạt/liềm).
- ✅ **Sửa lệch vị trí tay/mask**: anchor theo **tâm collider/renderer** (không dùng transform.position
  vì gốc transform nằm dưới đáy tile ~+87 world). Đã chống giá trị `_riceZoom` cũ (3.5) bằng `SanitizeZoom`.
- ✅ **Audit công thức L1→30**: XP, thưởng lên cấp (đủ L2–L30), kinh tế cây, nhiệm vụ — **không bug**.
- File code liên quan: `Assets/_Game/Farm/Scripts/Camera/CameraController.cs`,
  `.../Tutorial/TutorialManager.cs`, `TutorialCameraFocus.cs`, `TutorialRuntimeTargetResolver.cs`,
  `TutorialActionHandGuide.cs`, `UnmaskRaycastFilter.cs`, `TutorialStepTriggerBridge.cs`.

### 1.6 Dữ liệu tham chiếu — vị trí 8 ô đất (world position của transform)
| Ô | X | Y | Ô | X | Y |
|---|---|---|---|---|---|
| plot_01 | 2098.474 | -810.379 | plot_05 | 2344.329 | -1165.193 |
| plot_02 | 1877.763 | -933.307 | plot_06 | 2562.245 | -1050.647 |
| plot_03 | 2109.649 | -1056.234 | plot_07 | 2579.571 | -1284.817 |
| plot_04 | 2333.154 | -938.895 | plot_08 | 2789.774 | -1165.786 |

> ⚠️ **Lưu ý vàng:** đây là `transform.position` (nằm ở ĐÁY tile). Tâm tile nhìn thấy = `Collider2D.bounds.center`
> (≈ +87 world theo Y do Box Collider Offset Y≈173.6 × scale 0.5). **Mọi tính toán tay/mask/camera phải dùng
> tâm collider, KHÔNG dùng transform.position.** Tutorial L1 dùng 6 ô đầu (plot_01..plot_06).

---

## PHẦN 2 — SƠ ĐỒ ĐỘI & QUY TRÌNH

Phân cấp & delegate theo `.claude/docs/agent-coordination-map.md`:
- **Directors:** `creative-director` (tầm nhìn/đối tượng), `technical-director` (kiến trúc/perf/build), `producer` (điều phối).
- **Leads:** `game-designer`, `lead-programmer`, `art-director`, `audio-director`, `qa-lead`, `release-manager`, `localization-lead`.
- **Specialists Unity:** `unity-specialist` + `unity-ui-specialist`, `unity-shader-specialist`, `unity-addressables-specialist`, `unity-dots-specialist`.
- **Programmers:** `gameplay-programmer`, `ui-programmer`, `engine-programmer`, `tools-programmer`.
- **Design/Content:** `systems-designer`, `level-designer`, `economy-designer`, `ux-designer`, `writer`, `world-builder`.
- **Art/Audio:** `technical-artist`, `sound-designer`.
- **Quality/Ops:** `qa-tester`, `performance-analyst`, `devops-engineer`, `analytics-engineer`, `accessibility-specialist`, `live-ops-designer`, `community-manager`, `security-engineer`.

**Quy trình chuẩn dùng cho board này:** Bug Fix (Pattern 2), New Feature (Pattern 1), Balance (Pattern 3), Sprint Cycle (Pattern 5), Release (Pattern 7) — xem coordination-map.

---

## PHẦN 3 — BẢNG TASK (theo EPIC)

Ký hiệu Priority: **P0** = chặn/khẩn, **P1** = cao, **P2** = trung bình.
Status: `TODO` · `DOING` · `BLOCKED` · `DONE`.

### EPIC A — Hoàn thiện Tutorial Level 1
| ID | Task | Owner (chính) | Hỗ trợ | Prio | Status |
|----|------|---------------|--------|------|--------|
| A1 | Camera 1-chủ (CinematicFocus/EndCinematic) | `lead-programmer`→`gameplay-programmer` | `unity-specialist` | P1 | ✅ DONE |
| A2 | Intro mây đẹp, bỏ zoom | `gameplay-programmer` | `technical-artist`, `ux-designer` | P1 | ✅ DONE |
| A3 | Nền xám bao 6 ô + tay quét plot_01..06 | `ui-programmer`/`unity-ui-specialist` | `gameplay-programmer` | P1 | ✅ DONE |
| A4 | **Sửa lệch vị trí tay/mask** (dùng tâm collider) | `gameplay-programmer` | `unity-ui-specialist` | P0 | ✅ DONE |
| A5 | QA chạy hết luồng tutorial L1 (intro→trồng→thu hoạch) | `qa-tester` | `qa-lead` | P0 | TODO |
| A6 | UX review: nhịp độ, độ rõ của tay, chữ ngắn gọn cho trẻ em | `ux-designer` | `game-designer` | P1 | TODO |
| A7 | Set `_riceZoom`/`_flowerZoom` đúng trong Inspector (≈460) + tinh chỉnh padding mask | `unity-specialist` | `gameplay-programmer` | P1 | TODO |

**Acceptance A5–A7:** tay luôn nằm đúng trên tile khi camera lia; nền xám ôm khít 6 ô; không giật; trẻ em làm theo được không cần đọc nhiều.

### EPIC B — Pass "Juice" (nhiều animation hơn) ⭐ trọng tâm theo yêu cầu
| ID | Task | Owner (chính) | Hỗ trợ | Prio | Status |
|----|------|---------------|--------|------|--------|
| B1 | Spec danh sách hiệu ứng cần có (trồng, tưới, lớn, thu hoạch, xu/EXP bay, lên cấp, bấm nút, mua bán) | `art-director` | `game-designer`, `creative-director` | P1 | TODO |
| B2 | VFX particle + shader (đất tơi, nước, lấp lánh, confetti lên cấp) | `technical-artist` | `unity-shader-specialist` | P1 | 🔶 DOING |
| B2a | Popup lên cấp: pháo hoa to + nhiều + bùm liên tục tới khi Nhận Quà; Lana03 to 2 bên | `technical-artist` | `ui-programmer` | P1 | ✅ DONE |
| B3 | UI feedback animation (nút nảy, popup pop, reward bay vào ví) | `ui-programmer` | `unity-ui-specialist` | P1 | TODO |
| B4 | Âm thanh đi kèm từng hiệu ứng (cute, mềm) | `audio-director` | `sound-designer` | P1 | TODO |
| B5 | Animation sprite nhân vật/cây/con vật (idle, harvest pop, bounce) | `art-director` | `technical-artist` | P2 | TODO |
| B6 | Budget hiệu năng VFX cho máy yếu (trẻ em hay dùng máy phổ thông) | `performance-analyst` | `technical-director` | P1 | TODO |

**Acceptance EPIC B:** mỗi hành động cốt lõi có ≥1 animation + âm thanh; giữ ≥60fps trên máy tầm trung; phong cách dễ thương, không chói/giật.

### EPIC C — Build liên tục (CI/CD)
| ID | Task | Owner (chính) | Hỗ trợ | Prio | Status |
|----|------|---------------|--------|------|--------|
| C1 | Pipeline auto-build (mỗi commit + nightly) | `devops-engineer` | `technical-director`, `release-manager` | P1 | TODO |
| C2 | Versioning + cadence build (mốc build thường xuyên) | `release-manager` | `producer` | P2 | TODO |
| C3 | Smoke test tự động mỗi build (`/smoke-check`) | `qa-lead` | `qa-tester` | P1 | TODO |

**Acceptance EPIC C:** mỗi commit ra được build chơi được; smoke test pass tự động; có changelog.

### EPIC D — Hợp đối tượng (phụ nữ + trẻ em)
| ID | Task | Owner (chính) | Hỗ trợ | Prio | Status |
|----|------|---------------|--------|------|--------|
| D1 | Art direction dễ thương/ấm (bo tròn, màu pastel) — `/art-bible` | `art-director` | `creative-director` | P1 | TODO |
| D2 | Accessibility: nút to, chữ scale được, colorblind-safe, luồng đơn giản | `accessibility-specialist` | `ux-designer`, `ui-programmer` | P1 | TODO |
| D3 | Localize tiếng Việt + giọng văn thân thiện | `localization-lead` | `writer` | P2 | TODO |
| D4 | Tinh chỉnh nhịp onboarding/kinh tế cho casual (đã audit L1–30) | `economy-designer` | `game-designer` | P2 | TODO |

### EPIC E — Level 2 (KHOÁ tới khi user test L1 OK)
| ID | Task | Owner (chính) | Hỗ trợ | Prio | Status |
|----|------|---------------|--------|------|--------|
| E1 | Chốt thiết kế L2 | `game-designer` | `level-designer` | P1 | 🔒 BLOCKED (chờ L1 pass) |
| E2 | Implement L2 | `gameplay-programmer` | `unity-specialist` | P1 | 🔒 BLOCKED |
| E3 | QA L2 | `qa-tester` | `qa-lead` | P1 | 🔒 BLOCKED |

> **GATE của user:** "Làm xong L1, tôi test OK thì mới tiếp tục L2." → EPIC E chỉ mở sau khi A5 pass.

---

## PHẦN 4 — PROMPT SẴN CHO TỪNG AGENT (copy-paste để giao việc)

> Mỗi prompt đã gắn sẵn "bộ não chung". Dán prompt + bảo agent đọc file này trước.

### ▶ producer
```
Bạn là producer. Đọc TEAM_BRIEF_TASKBOARD.md (bộ não chung). Lập sprint từ bảng task:
- Sprint 1: A5, A6, A7 (đóng Tutorial L1) + B1 (spec juice) + C1 (CI).
- Giữ EPIC E khoá tới khi A5 pass.
Dùng /sprint-plan. Cập nhật Status từng task trong file. Báo rủi ro & phụ thuộc.
```

### ▶ qa-tester  (A5) + qa-lead
```
Đọc bộ não chung. Viết & chạy test cho Tutorial L1 (Pattern 2/QA): intro → bấm ĐÃ RÕ →
camera lia 6 ô → nền xám ôm khít 6 ô → tay quét plot_01..06 → trồng đủ 6 → thu hoạch.
Kiểm tra đặc biệt: tay có ĐÚNG trên tile khi camera lia không (bug cũ: lệch xuống ~87 world).
Dùng /qa-plan, /bug-report. Ghi kết quả vào board (A5).
```

### ▶ unity-specialist / unity-ui-specialist  (A7 + hỗ trợ A3/A4)
```
Đọc bộ não chung + mục 1.6 (vị trí ô, lưu ý tâm collider). Trong scene SCN_Farm:
- Đặt _riceZoom/_flowerZoom (TutorialCameraFocus) ≈ 460; tinh chỉnh _areaScreenPadPct/_areaScreenPadPx
  (TutorialRuntimeTargetResolver) để nền xám ôm khít 6 ô.
- Xác nhận hand/mask anchor theo Collider2D.bounds.center (đã có PlotVisualCenter).
Không sửa logic ngoài UI/camera tutorial nếu chưa được lead-programmer giao.
```

### ▶ art-director  (B1, B5, D1)
```
Đọc bộ não chung (4 trụ cột, đối tượng phụ nữ+trẻ em). Dùng /art-bible:
- Liệt kê danh sách animation/VFX cốt lõi (B1) theo từng hành động.
- Định hướng style dễ thương, pastel, bo tròn (D1).
Giao technical-artist & ui-programmer phần implement. Output: spec + art bible vào docs.
```

### ▶ technical-artist + unity-shader-specialist  (B2, B6)
```
Đọc bộ não chung + spec B1. Làm VFX: đất tơi khi trồng, giọt nước khi tưới, lấp lánh khi cây lớn,
xu/EXP bay vào ví, confetti khi lên cấp. Giữ budget perf cho máy phổ thông (phối hợp performance-analyst, B6).
Dùng VFX Graph/Shader Graph URP. Theo /perf-profile để kiểm tra fps.
```

### ▶ ui-programmer  (B3)
```
Đọc bộ não chung. Thêm animation feedback UI: nút nảy khi bấm, popup pop-in/out, phần thưởng bay
vào ví xu/EXP, số nhảy (count-up). Nhẹ, không chặn input. Phối unity-ui-specialist.
```

### ▶ audio-director + sound-designer  (B4)
```
Đọc bộ não chung. Tạo danh sách SFX cute/mềm gắn với từng animation EPIC B (trồng, tưới, thu hoạch,
xu, lên cấp, bấm nút). Mix dễ chịu cho trẻ em. Output: audio event list.
```

### ▶ devops-engineer + release-manager + technical-director  (EPIC C)
```
Đọc bộ não chung. Dựng CI/CD Unity: build tự động mỗi commit + nightly, smoke test (/smoke-check),
versioning + changelog (/changelog, /release-checklist). Mục tiêu: luôn có build chơi được liên tục.
```

### ▶ accessibility-specialist + ux-designer  (A6, D2)
```
Đọc bộ não chung. Review tutorial & UI cho trẻ em/phụ nữ: nút to, chữ ít & rõ, scale chữ, colorblind-safe,
luồng tối giản. Dùng /ux-review. Đề xuất chỉnh sửa cụ thể, giao ui-programmer implement.
```

### ▶ game-designer + economy-designer  (D4, hỗ trợ B1/E1)
```
Đọc bộ não chung. Đảm bảo "build liên tục": luôn có mục tiêu kế tiếp ở L1→L2. Tinh chỉnh nhịp
onboarding & kinh tế cho casual (đã audit L1–30 sạch). Dùng /balance-check. Chuẩn bị thiết kế L2 (mở khi A5 pass).
```

---

## PHẦN 5 — DEFINITION OF DONE (chung)
- Chạy được trên máy tầm trung ≥60fps; không lỗi console đỏ.
- Theo 4 trụ cột (juicy, build liên tục, dễ-thân thiện, phản hồi ngắn).
- Có test/QA pass (qa-lead ký) + code review (lead-programmer ký) cho code.
- Cập nhật Status + Notes vào board này. Không commit nếu user chưa duyệt.

---

## PHẦN 6 — CÁCH KÍCH HOẠT "BỘ NÃO CHUNG" TỰ ĐỘNG (tuỳ chọn)
Để mọi agent tự nạp brief này, thêm 1 dòng vào `CLAUDE.md` (cần user duyệt):
```
## Team Brief & Task Board
@TEAM_BRIEF_TASKBOARD.md
```
Hoặc chuyển file vào `.claude/docs/` và tham chiếu từ `CLAUDE.md`.
