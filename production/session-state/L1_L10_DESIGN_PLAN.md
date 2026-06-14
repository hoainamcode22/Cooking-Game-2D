# L1→L10 DESIGN PLAN (BẢN NHÁP — CHỜ DUYỆT)

> Team B (Progression Design) + tham chiếu nghiên cứu Township/Hay Day. Ngày: 2026-06-12.
> Nguyên tắc: không phá tutorial L1→L2 đang chạy tốt; mọi thay đổi data chờ duyệt.

---

## 1. Bài học từ Township & Hay Day (đã research, nguồn ở cuối file)

**Tutorial:** Cả 2 game mở đầu bằng **thu hoạch lúa chín sẵn** (10 giây đầu = phần thưởng, không phải chờ đợi), mỗi bước 1 cử chỉ kéo-thả duy nhất + spotlight, mascot dẫn chuyện (bù nhìn Mr. Wicker / Ernie), **tự do hoàn toàn sau 3–5 phút**. Township dạy luôn nút speed-up bằng gem ngay trong tutorial (lần đầu miễn phí).

**EXP:** L1→5 cực rẻ (Hay Day: 27/7/14/30/50 — phiên đầu lên 4–6 cấp), nhảy vọt ×4 ở L6 (220), sau đó +25–30%/cấp. Cả 2 game đến L10 cần ~960 EXP/cấp.

**Kinh tế:** Lúa = van chống kẹt tiền (Township: trồng **0 đồng**; Hay Day: 1 hạt → thu 2). Cây mới = timer ×2, lời ×2 nhưng lời/phút giảm — lúa luôn là vòng farm chủ lực. Tặng đúng **30 tiền premium** lúc đầu + nhỏ giọt 1 viên/cấp. Đơn hàng = nguồn EXP chính (đánh đổi coin↔XP rõ ràng), bán chợ = nguồn coin chính. Đơn khó được phép huỷ (chờ 6–30 phút).

**Mỗi level 1–10 đều mở thứ mới**, xoay vòng thể loại: cây → máy → con vật → hệ thống (orders L4, achievement L6, market L7, vòng quay L8, event L9, social L10). Phụ nữ/trẻ em: cảm giác **sở hữu & trang trí** đặt ngay trong tutorial (sơn nhà, đặt tên nông trại).

**Nhịp timer phiên đầu:** 2/5/5/20 phút đan xen — luôn có việc sau mỗi 1–3 phút ("Starbucks test").

## 2. Nguyên tắc chốt cho demo

1. Lúa (180s, hạt 20 vàng) giữ vai trò "wheat" — không bao giờ khoá, lời nhẹ, van chống kẹt.
2. Mỗi level mở đúng 2–3 thứ + 1 quà; popup level-up show hết.
3. Đơn hàng L1 chỉ 1 item, dễ; độ phức tạp tăng theo level (xem §7).
4. Không yêu cầu thứ chưa farm được (đặc biệt món có **cá** — chưa có hệ cá).
5. Tutorial bắt buộc kết thúc ≤ 7 phút (lên L2); từ đó chỉ hint theo ngữ cảnh.
6. Gem: đủ để học speed-up trong tutorial (lần đầu free hoặc 1 gem).

## 3. BẢNG VÒNG CHƠI L1→L10 (bảng chính — CHỜ DUYỆT)

EXP giữ công thức code hiện tại: `40 + 10n + n²` (n = level−1).

| Level | EXP cần (→cấp sau) | Unlock | Quà level-up | Tutorial / Hint | Order pool (cộng dồn) | Shop unlock | Mission chính |
|-------|--------------------|--------|--------------|-----------------|------------------------|-------------|----------------|
| 1 | 40 | Lúa, Hướng dương, Bắp cải; 6 ô đất + 2 chậu hoa; 4 nhà order | — (start: 400 vàng, 15 gem — chờ duyệt) | Tutorial 18 bước hiện có: NPC chào → guide board 4 bước → kéo `seed_rice` vào 6 ô → speed-up (free lần đầu) → thu hoạch → EXP bay → kéo `seed_huong_duong` vào 2 chậu → thu → lên L2 | Lúa ×1/×3/×5; Bắp cải ×3 (chỉ 1-item) | seed_rice, seed_huong_duong, seed_bapcai | Trồng 6 lúa · Thu 6 lúa · Trồng 2 hoa · Lên L2 |
| 2 | 51 | Ngô; **Chuồng gà (tặng)**; trứng | 150 vàng + 2 gem + 3 seed_ngo | Animal tutorial: camera tới chuồng gà → cho gà ăn → hint speed-up → thu trứng → giao 1 đơn | + Ngô ×2-4 | seed_ngo | Cho gà ăn · Thu trứng · Giao 1 đơn |
| 3 | 64 | Cà chua, Cà rốt; **nhà order #5**; đơn trứng/thịt gà | 200 vàng + 2 gem + 3 seed_cachua | Hint: vào shop mua hạt mới; giới thiệu bảng mission | + Cà chua, Cà rốt, Trứng, Thịt gà | seed_cachua, seed_carot | Mua 1 hạt giống mới · Hoàn thành 3 đơn |
| 4 | 79 | Hoa hồng, Oải hương; **Chuồng heo (mua được)** | 250 vàng + 3 gem + 2 seed_hoa_hong | Hint shop: mua chuồng heo → cho heo ăn | + Hoa HD, Hoa hồng, Oải hương | seed_hoa_hong, seed_hoa_oai_huong, Chuồng heo | Mua chuồng heo · Thu hoa 2 loại |
| 5 | 96 | **BẾP (cooking)** + 10 món dễ; Khoai tây; **nhà #6** | 300 vàng + 3 gem + 5 khoai tây giống + popup "Bếp đã mở!" | Hint sang scene bếp, nấu món đầu (cơm chiên trứng) | + Thịt heo; + món nấu dễ (tối đa 1 món/đơn) | seed_khoai_tay, vật phẩm bếp | Nấu món đầu tiên · Giao 1 món ăn |
| 6 | 115 | Nấm; **Chuồng bò (mua được)**; daily mission bật | 350 vàng + 3 gem + 3 seed_nam | Hint chuồng bò + daily mission | + Nấm (SAU KHI FIX ID), + sữa | seed_nam, Chuồng bò | Nấu 3 món · Mua chuồng bò |
| 7 | 136 | Mía; **nhà #7**; hoa lan + cúc trắng | 400 vàng + 4 gem + 3 seed_mia | — | + Mía, + 2-item phổ biến hơn | seed_mia, seed_hoa_lan, seed_hoa_cuc_trang | Giao 5 đơn · Thu 10 nông sản |
| 8 | 159 | Chanh; đơn thịt bò; món bò (4 món) | 450 vàng + 4 gem + 3 seed_chanh | — | + Chanh, Thịt bò, món bò | seed_chanh | Nấu 1 món bò · Giao đơn thịt bò |
| 9 | 184 | Ớt; Tulip + cúc vạn thọ; **nhà #8 (đủ 8)** | 500 vàng + 5 gem + 2 seed_chili | — | + Ớt, Tulip; đơn combo farm+animal+cook | seed_chili, seed_tulip, seed_hoa_cuc_van_tho | Giao 3 đơn combo |
| 10 | 184→(L11: 211) | Tiêu; hoa còn lại; achievement "Nông dân thực thụ" | 600 vàng + 8 gem + danh hiệu + pháo hoa lớn | Popup "Hành trình mới đang chờ!" | + Tiêu; pool đầy đủ trừ món cá | seed_pepper, 4 hoa còn lại, decor | Đạt L10 · Hoàn thành 20 đơn tổng |

Tổng EXP L1→L10 = **924**. Nguồn EXP: thu hoạch 5/ô (mía 8), đơn 3–10/đơn vị, nấu +8/món (đề xuất mới), train 10/slot. Ước tính 75–100 phút chơi liên tục → đạt L10 (hợp demo).

## 4. Tutorial L1→L2 — trạng thái & việc còn lại

Hệ hiện có ĐẦY ĐỦ (TutorialManager + 18 step + alias ID thật + camera + hand pointer + guide board + log `[SeedIdScan]`/`[TutorialTargetRegistry]`). Việc còn lại chỉ là **kiểm/chỉnh, không xây mới**:

- Chạy `Tools → Farm Game → Test → Check Tutorial L1-L2 Setup` xác nhận PASS sau khi đổ data kinh tế mới.
- Xác nhận speed-up trong tutorial: free lần đầu hoặc cấp đủ gem (econ table §Gem).
- Thêm tool `Debug → Reset Tutorial L1-L2` (chưa có — sẽ làm ở Batch 1).
- Camera sau tutorial trả về default size 750 (CameraController) — test không giật.

## 5. Animal tutorial L2→L4 (mới)

- **L2 — Gà:** chuồng gà TẶNG qua LevelReward_L2 (không bắt mua). Flow: camera focus chuồng → bubble "Cho gà ăn nào!" → kéo thức ăn → chờ/speed-up → thu trứng → hint giao đơn trứng. Dùng lại TutorialManager dạng mini-sequence 4 step.
- **L3 — Heo (hint mềm):** mission "Mua chuồng heo" + hand pointer vào shop khi mở shop lần đầu ở L4 (chuồng heo bán ở L4).
- **L4 — Bò:** tương tự heo, mở bán L6 (dời từ L4 để giãn chi tiêu — xem econ). Nếu data animal heo/bò chưa đủ (cần kiểm PenMiniPanelConfig từng chuồng ở Batch 5) → báo cáo trước, không tự chế.

## 6. Cooking L5 — 10 món đầu (CHỜ DUYỆT)

Tiêu chí: nguyên liệu 100% farm được ≤ L6, không cá, không bò (bò để L8).

| # | Món (dishId) | Nguyên liệu chính | Mở | Order reward đề xuất |
|---|--------------|-------------------|----|----------------------|
| 1 | com_chien_trung | lúa + trứng | L5 | 130 vàng / 10 EXP (giữ) |
| 2 | trung_chien_ca_chua | trứng + cà chua | L5 | 120 / 10 |
| 3 | khoai_tay_chien | khoai tây | L5 | 115 / 10 |
| 4 | salad_bap_cai_chanh | bắp cải (+chanh → **đổi thành cà chua** vì chanh L8) | L5 | 120 / 10 |
| 5 | bap_cai_xao_nam | bắp cải + nấm | L6 | 125 / 10 |
| 6 | sup_ngo_nam | ngô + nấm | L6 | 135 / 10 |
| 7 | ga_nuong_lu | thịt gà | L6 | 160 / 12 |
| 8 | ga_xao_ot | thịt gà + ớt (**ớt L9 → đổi cà chua**, hoặc dời món L9) | L7 | 140 / 12 |
| 9 | canh_khoai_tay_thit_heo | khoai tây + thịt heo | L7 | 135 / 12 |
| 10 | suon_heo_xao_chua_ngot | thịt heo + cà chua | L7 | 150 / 12 |

Món L8+: pho_bo_tai, bo_xao_tieu, bo_ham_ca_rot, nam_xao_thit_bo, trung_op_la_bo_ne, thit_heo_luoc_cuon_rau, nuoc_mia_chanh (mía L7+chanh L8). **Loại khỏi order demo:** canh_chua_ca, ca_nuong_tieu (cần cá — chưa có hệ cá). Ở Batch 6 phải dump recipe thật từng Dish_*.asset để chốt — bảng trên dựa trên tên món, cần verify nguyên liệu chính xác.

Bổ sung: nấu thành công +8 EXP (hiện 0) — cần duyệt.

## 7. Village orders L1→L10

- **Nhà:** L1 active 4 nhà (đề xuất: House_02, 03, 05, 06 — gần spawn camera; chốt khi vào scene). Mở thêm: L3→#5, L5→#6, L7→#7, L9→#8. Cần feature mới: `activeFromLevel` trên HouseOrderController HOẶC filter trong VillageOrderManager (đề xuất cách 2 — ít đụng scene).
- **Tỉ lệ đơn 2-item:** L1: 0% · L2–L3: 20% · L4–L6: 35% · L7+: 50% (hiện cứng 50%).
- **Pool:** dùng unlockLevel sẵn có của OrderItemDefinition, chỉnh: sugarcane 8→7, beef 8 (giữ), tulip 9 (giữ), fix nấm; món ăn tách 3 đợt L5/L6-7/L8+ thay vì 20 món cùng L5. Mỗi đơn tối đa 1 món nấu.
- **Reward:** theo bảng econ (payout ≥ 1.8× giá trị bán lẻ ở L1–5, ≥ 1.5× ở L6–10).
- Đơn món cá: gỡ khỏi pool demo.

## 8. Mission / Daily / Achievement

- Mission chính theo level: cột "Mission chính" ở §3 (4 mission L1, 3/cấp sau đó). Data: mở rộng MissionDatabase hiện có, thêm field `requiredLevel`.
- Daily: bật L6, 3 nhiệm vụ/ngày (giao X đơn, thu Y nông sản, nấu Z món) — placeholder reward 50–100 vàng, làm UI tab trong popup_Ewar.
- Achievement dài hạn: thu 100/500 nông sản, giao 50 đơn, nấu 30 món, đạt L10 — 5 cái cho demo.

## 9. Nhìn xa L11→L30 (định hướng, chưa làm)

| Mốc | Nội dung (theo nhịp Township/Hay Day) |
|-----|----------------------------------------|
| L11–L13 | Daily spin / quà ngày nâng cấp, cây mới, nâng kho |
| L14–L16 | Máy chế biến nông sản (làm bột, nước ép) — tầng production thứ 2 |
| L17–L19 | Mở rộng đất + plot 21–30, hệ cá (mở khoá món cá), boat/bến nhỏ |
| L20–L24 | Tourist boat (đã có future design riêng), nhà hàng ven biển |
| L25–L30 | Event mùa vụ, trang trí nâng cao, social/leaderboard placeholder |

## 10. Tool cần build (map với Phase 16 master prompt)

| Menu | Trạng thái |
|------|-----------|
| Tools → Farm Game → Demo L1-L10 → Setup All | MỚI — gọi chuỗi: tutorial, level-up popup (L2–L10), shop locks, village orders, missions, VFX slots, startup popups |
| … → Check All | MỚI — PASS/FAIL từng hệ + đếm missing script + console error |
| … → Simulate Economy | MỚI — chạy mô phỏng 3 kiểu người chơi, in bảng |
| … → Reset Demo Save | Nâng từ `Phase1TestTool ⚠ Reset Player Save` |
| … → Print Playtest Checklist | MỚI |
| Setup Tutorial L1-L2 / Check / Generate | ĐÃ CÓ ✓ (thêm Reset Tutorial) |
| Setup Level Up Popup (+ Reward L2-L6) | ĐÃ CÓ — nâng lên L2–L10 |
| Setup Shop Locks | ĐÃ CÓ (L3+) — nâng L1–L10 theo data mới |
| Setup Village Orders | ĐÃ CÓ (L1–L6) — nâng L1–L10 + house gating |
| Setup Missions L1-L10 / Check | MỚI |

## 11. Câu hỏi chờ anh chốt

1. Bảng §3: duyệt nguyên bảng hay chỉnh cột nào (đặc biệt vị trí chuồng bò L6, khoai tây L5)?
2. 10 món L5–L7 ở §6 + nguyên tắc "mỗi đơn tối đa 1 món nấu" — OK?
3. 4 nhà active đầu game chọn theo vị trí gần camera spawn — để team tự chọn khi vào scene hay anh chỉ định?
4. Nấu ăn +8 EXP/món — OK?
5. Tutorial speed-up: free lần đầu (đề xuất) hay trừ 1 gem?

---
*Nguồn research: Hay Day Wiki (Experience Levels, Truck, Diamond, Crops), Township Wiki (Level up, Xp, Crops, Helicopter, Cash), Deconstructor of Fun — Behind the Success of Hay Day, Game Developer — Monetization analysis of Hay Day, AppGamer/BlueStacks Township guides.*
