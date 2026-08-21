# AUTOPILOT BACKLOG — Kế hoạch build A→Z (single source of truth)

> **Đây là "danh sách việc tổng" mà `/autopilot` đọc để tự làm từ A đến Z.**
> Mỗi phiên, chỉ cần gõ **`/autopilot`** (hoặc "tiếp tục roadmap") — đội agent đọc file này +
> `memory/MEMORY.md` + báo cáo phiên gần nhất trong `production/session-state/`, chọn task chưa
> bị chặn kế tiếp, làm theo `production/AUTONOMY.md`, rồi cập nhật cột **Status** ngay tại đây.
>
> Nguồn thiết kế: `Cooking_Farm_2D_Studio_Playbook.pdf` (cẩm nang), `ROADMAP_GAME_COMPLETE.md`,
> `L1_L10_DESIGN_PLAN.md`, `L1_L10_ECONOMY_TABLE.md`, `MISSIONS_MASTER_LIST.md`.
>
> **Luật vàng (xem AUTONOMY.md):** chỉ làm việc CỘNG THÊM an toàn không cần hỏi; DỪNG &
> hỏi khi đụng: xoá/sửa logic lớn, sửa `.unity/.prefab/.asset` quan trọng, commit/push, hoặc
> quyết định thiết kế còn mơ hồ. Mọi việc cần con người gom vào mục **"CẦN BẠN"** ở cuối báo cáo.

---

## Quy ước

- **Status:** `TODO` · `DOING` · `BLOCKED` (chờ bạn/được liệt kê ở CẦN BẠN) · `REVIEW` (xong code, chờ bạn test Unity) · `DONE`
- **Loại:** 🤖 = agent tự làm được hết (code/data/tool/doc) · 🧑 = cần bạn trong Unity/art/quyết định · 🤝 = agent làm phần lớn, bạn finish 1 bước
- **Owner:** agent chính (xem `.claude/agents/`). **Dep:** task phải xong trước.
- Mỗi task ≤ 1–3 ngày. Task to hơn → producer chẻ nhỏ trước khi làm.

---

## MILESTONE 0 — Nền móng kỹ thuật (làm TRƯỚC mọi nội dung mới)

| ID | Task | Loại | Owner | Dep | Acceptance | Status |
|----|------|------|-------|-----|------------|--------|
| M0-1 | **Xoá debug override** `Gold/Gems=1000` trong Editor (FarmEconomyManager) | 🤖 | gameplay-programmer | — | Play Mode bắt đầu đúng 400/15; grep không còn override | TODO |
| M0-2 | **SaveSystem JSON**: gom mọi state vào `SaveData`, ghi/đọc `persistentDataPath/save.json`, có `saveVersion` để migrate, auto-save khi đổi state + pause/quit — SCAN phát hiện 20+ hệ ĐÃ tự lưu PlayerPrefs; gói mới = save.json hợp nhất/backup + lưu hệ TÀU còn thiếu + SaveDebugTool. Code tại `Scripts/Save/`, docs `production/savesystem/` | 🤝 | lead-programmer | — | Tắt/mở game giữ nguyên vàng/level/inventory/ô đất/chuồng/mission; có log `[Save]`; TrainManager.PATCH chờ duyệt | REVIEW |
| M0-2b | **Điều tra bug "mất vật phẩm khi thoát Play Mode"** user báo — nghi: debug override Gold/Gems (M0-1), StarterInventorySetup, tool reset, hoặc hệ chưa flush khi ExitPlayMode. Cần log phiên test của user để chẩn đoán | 🤝 | qa-lead | M0-2 | Tái hiện được bug + xác định hệ gây mất; có fix hoặc task fix riêng | TODO |
| M0-3 | Chuyển các manager đang dùng PlayerPrefs cho DỮ LIỆU LỚN sang SaveSystem (giữ PlayerPrefs cho cài đặt nhỏ) | 🤖 | gameplay-programmer | M0-2 | Không còn state quan trọng nằm rải rác ở PlayerPrefs; smoke test pass | TODO |
| M0-4 | **Backup an toàn**: xác nhận Git remote (GitHub private) tồn tại; nếu chưa, hướng dẫn bạn tạo | 🤝 | devops-engineer | — | `git remote -v` có remote; bạn đã push ít nhất 1 lần | TODO |
| M0-5 | `/smoke-check` + `/regression-suite` chạy được, 0 lỗi đỏ console | 🤖 | qa-lead | M0-2 | Báo cáo smoke PASS | TODO |

---

## MILESTONE 1 — Hệ nhiệm vụ hoàn chỉnh (bạn yêu cầu) + retention hooks P0

| ID | Task | Loại | Owner | Dep | Acceptance | Status |
|----|------|------|-------|-----|------------|--------|
| M1-1 | **Nâng schema MissionData** theo `MISSIONS_MASTER_LIST.md`: thêm `missionId, requiredLevel, kind(Main/Daily), eventType, targetItemId` (giữ 6 field cũ → asset cũ không vỡ) | 🤖 | gameplay-programmer | — | Asset cũ load bình thường; field mới có default an toàn | TODO |
| M1-2 | **Tracker chuẩn key + persist + event**: `ReportEvent(type,itemId,amount)`, key `"<type>:<itemId>"`/`":any"`, lưu save, `OnProgressChanged`; sửa LỖI lệch key (ghi itemId vs đọc missionName) | 🤖 | gameplay-programmer | M1-1, M0-2 | Tiến độ hiển thị đúng realtime; claim persist qua phiên | TODO |
| M1-3 | **Hook gameplay đủ 8 điểm**: plant, harvest, feed, collectProduct, deliverOrder(+combo+withItem), buy, cook(+beef), reachLevel | 🤖 | gameplay-programmer | M1-2 | Mỗi sự kiện bắn log `[MissionTracker]`; mọi mission L1-L10 chạy được | TODO |
| M1-4 | **UI lọc theo level + realtime + claim persist** (PopupEwar, MissionItemUI) | 🤖 | ui-programmer | M1-2 | Mở popup ở L1 chỉ thấy mission L1; lên cấp hiện mission mới; claim 1 lần | TODO |
| M1-5 | **Đổ data mission chính L1→L30** qua tool (mở rộng `Setup Missions`) theo `MISSIONS_MASTER_LIST.md` | 🤝 | tools-programmer | M1-1 | Tool tạo đủ asset L1-L30; bạn chạy menu trong Unity 1 lần | REVIEW |
| M1-6 | **Daily mission + reset theo ngày** (pool đầy đủ, seed theo yyyyMMdd, mở từ L6) | 🤖 | gameplay-programmer | M1-2 | Mỗi ngày 3 daily; sang ngày mới reset; thưởng cộng thật | TODO |
| M1-7 | **Achievement / Ewar dài hạn** nối với tracker (thu 100/500, giao 50, nấu 30, đạt L10/20/30…) | 🤖 | gameplay-programmer | M1-2 | Achievement nhận thưởng được, lưu vĩnh viễn | TODO |
| M1-8 | **Teaser unlock kế tiếp** (HUD: "Cấp tới mở X" + thanh EXP) — retention P0 | 🤖 | ui-programmer | — | Luôn hiện mục tiêu kế tiếp; ẩn khi max | TODO |
| M1-9 | **Daily login wheel / streak 7 ngày** — retention P0 | 🤝 | gameplay-programmer | M0-2 | Quà mỗi ngày, chuỗi tăng dần, cộng vàng/gem thật | TODO |
| M1-10 | **Thông báo "đã chín/đã xong"** + đếm ngược rõ trên cây/chuồng — retention P0 | 🤖 | ui-programmer | — | Có badge "✓ chín" + countdown; rõ ở camera xa | TODO |
| M1-11 | **Fix 12 mission chết trỏ `pho_beef`** (dish thật `pho_bo_tai`; L4/L6/L8 đổi món đúng level) — tool `FixPhoBeefMissions.cs` + vá `MissionSetupTool.cs`, gói tại `production/missionfix/` | 🤝 | gameplay-programmer | — | Chạy menu Fix Pho Beef Missions (APPLY) → 12/12 sửa, 0 lỗi Console; generator đã vá chống tái sinh bug | REVIEW |
| M1-12 | Sửa 2 mission nấu quá sớm: `proc_c_2_3` (L2 đòi món L9), `proc_c_3_2` (L3 đòi món L5) — Bếp mở L5; generator không xét unlock level của dish | 🤖 | gameplay-programmer | M1-11 | Mission CookDish chỉ xuất hiện từ L5; target là dish đã mở tại level đó | TODO |
| M1-13 | Rà 70 achievement `a_reach_level_31..100` (cap L30) + 79 mission/daily mồ côi ngoài MissionDatabase — đề xuất dọn/park, chờ duyệt | 🤖 | game-designer | — | Có proposal danh sách xoá/giữ; bạn duyệt trước khi đụng asset | TODO |

---

## MILESTONE 2 — Tutorial L3→L10 (mở khoá nhịp chơi)

| ID | Task | Loại | Owner | Dep | Acceptance | Status |
|----|------|------|-------|-----|------------|--------|
| M2-1 | **Tutorial L4 chăn nuôi (heo)** — 9 step asset L4_01..09 theo Playbook §4.2 + hook `WaitForBuyPen` + đếm khẩu phần | 🤝 | gameplay-programmer | M1-3 | Luồng L4 liền mạch: mua chuồng→đặt→cho ăn 2 phần→gem→thu thịt; bạn chạy generator + test | TODO |
| M2-2 | **Template chăn nuôi tái dùng** rồi sinh L6 (bò) + L8 (bò sữa) theo bảng Playbook §4.3 | 🤝 | gameplay-programmer | M2-1 | L6/L8 chạy được, chỉ khác tham số | TODO |
| M2-3 | **Tutorial L5 mở Bếp**: hint sang scene cooking, nấu món đầu (com_chien_trung) | 🤝 | gameplay-programmer | M2-1 | Popup "Bếp đã mở!"; guide nấu món đầu; +EXP | TODO |
| M2-4 | Hint mềm L3/L7/L9 (mua hạt mới, mở nhà dân, đơn combo) qua AnimalGuideController/toast | 🤖 | gameplay-programmer | M1-3 | Toast đúng level, 1 lần/loại, không spam | TODO |
| M2-5 | **+8 EXP khi nấu thành công** (hiện 0) | 🤖 | economy-designer | — | Nấu xong cộng EXP; cân bằng vẫn đúng curve | TODO |

---

## MILESTONE 3 — Kinh tế & nội dung L11→L30

| ID | Task | Loại | Owner | Dep | Acceptance | Status |
|----|------|------|-------|-----|------------|--------|
| M3-1 | **File mô hình kinh tế (Excel/CSV)** L1-L30: cột lời/giờ, lời/ô, nguồn–hố; chạy `Simulate Economy` | 🤖 | economy-designer | — | Đổi 1 số thấy ngay ảnh hưởng; mô phỏng 3 kiểu người chơi không kẹt | TODO |
| M3-2 | **Nâng cấp kho/silo** (dung lượng tăng dần, tốn vàng+vật liệu) — hố vàng + mục tiêu | 🤝 | gameplay-programmer | M0-2 | Mua nâng cấp tăng slot kho; UI rõ | TODO |
| M3-3 | **Mở rộng đất** (vén sương mù, mở ô 9–30) | 🧑 | level-designer | — | Khu mới mở bằng vàng+vật liệu; cần bạn đặt vùng trong scene | BLOCKED |
| M3-4 | **Almanac / Sổ sưu tập** (cây, món, con vật, decor) — retention | 🤝 | ui-programmer | M1-2 | Lật mở từng ô; đếm % hoàn thành; thưởng khi đủ bộ | TODO |
| M3-5 | **Bonus thu hoạch biến thiên** (10–15% ra quà thêm + hiệu ứng "may mắn!") | 🤖 | gameplay-programmer | — | Tần suất đúng; có VFX/âm thanh | TODO |
| M3-6 | Cân bằng & data nội dung **L11–L20** (máy chế biến đã có; thêm cây/đơn/giá) | 🤖 | economy-designer | M3-1 | Simulate L11-20 không kẹt | TODO |
| M3-7 | Cân bằng & data nội dung **L21–L30** (hồ cá/món cá, tourist boat, sự kiện mùa) — hoặc cắt sang update sau launch | 🧑 | game-designer | M3-6 | Có design doc trước khi code; bạn duyệt scope | BLOCKED |

---

## MILESTONE 4 — Nhân vật, art & "juice" (linh hồn game)

| ID | Task | Loại | Owner | Dep | Acceptance | Status |
|----|------|------|-------|-----|------------|--------|
| M4-1 | **ART_BIBLE.md** (bảng màu HEX, luật bo tròn/pastel, mood board refs) | 🤖 | art-director | — | File hoàn chỉnh, dùng làm chuẩn mọi asset | TODO |
| M4-2 | **Brief đặt Mascot + 4 NPC** (mô tả tính cách, pose, biểu cảm, kích thước) để bạn thuê/đặt artist | 🤖 | art-director | M4-1 | Brief đủ để gửi Fiverr/artist; có icon app + ảnh store | TODO |
| M4-3 | **Tích hợp art mascot/NPC** vào tutorial, popup, bong bóng đơn | 🧑 | unity-ui-specialist | M4-2 | Cần bạn cung cấp PNG/sprite → agent wire vào | BLOCKED |
| M4-4 | **Pass JUICE**: tween nút nảy + popup pop + số đếm + squash/stretch + screen shake nhẹ trên mọi hành động cốt lõi | 🤖 | ui-programmer | — | Mỗi hành động có ≥1 animation; giữ 60fps | TODO |
| M4-5 | **VFX**: đất tơi, nước, lấp lánh khi lớn, confetti lên cấp (tái dùng Lana Studio) | 🤖 | technical-artist | — | Particle hợp tông, không tụt fps máy yếu | TODO |
| M4-6 | **Audio thật**: coin/plant/cook/level-up/bubble/harvest (mua/royalty-free) + wire AudioManager | 🧑 | audio-director | — | Cần bạn cấp/duyệt file mp3 → agent wire | BLOCKED |

---

## MILESTONE 4.5 — Xã hội online kiểu Hay Day (⏸ HOÃN — quyết định 2026-08-19, kích hoạt tiền-soft-launch)

> Plan chi tiết: `production/PLAN_XA_HOI_ONLINE.md` · Thiết kế: `production/firebase/FIREBASE_DATABASE_DESIGN.md`
> Catalog import sẵn sàng: `production/firebase/catalog_json_firebase.zip`. Toàn bộ 0 đồng (Spark) tới khi cần Functions.

| ID | Task | Loại | Owner | Dep | Acceptance | Status |
|----|------|------|-------|-----|------------|--------|
| MS-A | Setup Firebase console (project, Auth Anonymous, Firestore asia-southeast1, RTDB, dán Rules, import catalog) — Claude lái Chrome ~45' | 🤝 | technical-director | M6-2 gần kề | Console đủ 4 dịch vụ, rules deploy, catalog 20 file lên đủ | BLOCKED (hoãn chủ động) |
| MS-B | Nền client: Firebase SDK, đăng nhập ẩn danh, username duy nhất, FarmSnapshotBuilder (tái dùng SaveAdapters) đẩy `farmSnapshots/{uid}` | 🤝 | network-programmer | MS-A, M0-2 PASS | 2 account thấy snapshot của nhau | BLOCKED |
| MS-C | Thăm làng read-only + chợ async MVP (claim model) + tab hàng xóm (user thật + seed accounts + NPC) + leaderboard | 🤝 | gameplay-programmer | MS-B | Thăm được làng thật, mua được ở quầy người khác | BLOCKED |
| MS-D | Trước soft launch: Blaze + 4 Cloud Functions chống hack + App Check + budget alert $5 | 🧑 | security-engineer | MS-C | Giao dịch chợ 100% qua server | BLOCKED |

---

## MILESTONE 5 — Kiếm tiền (mobile F2P) + bản Steam premium

| ID | Task | Loại | Owner | Dep | Acceptance | Status |
|----|------|------|-------|-----|------------|--------|
| M5-1 | **Quyết định mô hình audience** (mixed + age gate khuyến nghị) + viết **Privacy Policy** | 🧑 | producer | — | Bạn chốt; có Privacy Policy để lên store | BLOCKED |
| M5-2 | **Store IAP**: starter pack, gói gem, season pass, remove-ads (chỉ bản mobile) | 🤝 | gameplay-programmer | M0-2, M5-1 | Mua được (sandbox); cộng vật phẩm thật | TODO |
| M5-3 | **Ad SDK Families-certified** (rewarded only), age gate, `tagForChildDirectedTreatment` | 🧑 | gameplay-programmer | M5-1 | Cần bạn cấp tài khoản AdMob; tuân thủ COPPA/Families | BLOCKED |
| M5-4 | **Tách build flag** bản Steam (không IAP, không ads, cân bằng "kiếm bằng chơi" làm chuẩn) | 🤖 | technical-director | M5-2 | 1 codebase, 2 cấu hình; bản Steam ẩn store | TODO |
| M5-5 | Dòng hàng cosmetic: decor/skin/pet/booster theo Playbook §6.1 | 🤝 | systems-designer | M3-4 | Mua bằng vàng/gem; không pay-to-win | TODO |

---

## MILESTONE 6 — Đánh bóng, QA & phát hành (Mobile trước → Steam)

| ID | Task | Loại | Owner | Dep | Acceptance | Status |
|----|------|------|-------|-----|------------|--------|
| M6-1 | **Full playtest L1→L30** + cân bằng cuối; sửa chỗ rớt tutorial (<40% qua = ưu tiên 1) | 🧑 | qa-lead | M2-*, M3-* | Chơi liền mạch không kẹt; báo cáo `/playtest-report` | BLOCKED |
| M6-2 | `/release-checklist`: age rating, store assets (icon, screenshot, trailer), mô tả, ASO | 🤝 | release-manager | M4-2 | Checklist xanh hết; assets sẵn sàng | TODO |
| M6-3 | **Build Android (AAB, IL2CPP, ARM64)** + **Google Play closed test (12 tester/14 ngày)** | 🧑 | devops-engineer | M6-2 | Bạn tạo Play Console ($25), gom 12 tester; build lên track test | BLOCKED |
| M6-4 | **Soft launch** 1–2 thị trường → đo D1/D7, tỉ lệ qua tutorial → sửa → mở rộng | 🧑 | live-ops-designer | M6-3 | Có số liệu retention; ≥2 vòng sửa | BLOCKED |
| M6-5 | **Steam page sớm** (trailer + screenshot + wishlist) — chạy SONG SONG từ Milestone 4 | 🧑 | community-manager | M4-2 | Page live, nút wishlist bật | BLOCKED |
| M6-6 | **Steam Demo + Next Fest** + định giá $5.99–9.99 | 🧑 | release-manager | M6-5 | Demo chơi mượt; đăng ký Next Fest | BLOCKED |

---

## Cách autopilot dùng file này (tóm tắt)
1. Đọc file này từ trên xuống, bỏ qua `DONE`/`BLOCKED`.
2. Chọn task `TODO` đầu tiên mà mọi **Dep** đã `DONE` và **Loại = 🤖** (hoặc 🤝 phần làm được).
3. Giao cho **Owner**, làm theo `AUTONOMY.md`, dùng tool/skill phù hợp.
4. Chạy check (`/smoke-check`, `/code-review`…), cập nhật **Status**.
5. Lặp tới khi: hết task 🤖, hoặc gặp 🧑/🤝-cần-bạn → ghi vào **CẦN BẠN** rồi báo cáo & dừng.

> Khi thêm việc mới phát sinh, agent ghi thẳng vào milestone phù hợp (không tạo file rời).
