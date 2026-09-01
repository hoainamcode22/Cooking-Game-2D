# 📋 TASKBOARD "UI JUICE" — CHỐT TOÀN CẢNH SAU 3 VIDEO (2026-09-01)
> Lead viết để Sếp nắm 1 trang: đã xong gì, còn gì, và 4 THẺ TASK viết sẵn để Sếp
> bàn giao bớt 1 thẻ cho bên khác phụ. Chi tiết kỹ thuật: `TEAM_UI_JUICE_V2_2026-09-01.md`.
> Backup toàn bộ: `production/backup_ui_fx_reward_2026-09-01/` — hỏng thì chép ngược lại.

---

## A. ĐÃ HOÀN THÀNH (code nằm trong project, chờ Sếp bấm menu Unity)

**Từ video 1 (Township — tiền bay về HUD):**
- ✅ Hệ `RewardFlyFX` thống nhất: vàng + kim cương + EXP cùng bay kiểu bung→khựng→bay cong so le→icon HUD nảy. Tự tắt CoinFlyFX/GemFlyFX cũ (revert được).
- ✅ `RewardIconLibrary` + tool "Đồng bộ icon vàng (DRY-RUN/APPLY)" → cả game dùng chung 1 icon vàng.
- ✅ Icon vàng MỚI của đội vẽ: ĐẠT nghiệm thu, đã nằm ở `Assets/Art/UI/Currency/icon_gold.png`.

**Từ video 2 (Township — ăn mừng xây xong):**
- ✅ Chẩn đoán đúng bệnh pháo hoa cũ (nằm sau công trình, nhỏ, thấp — có số dòng code).
- ✅ `ConstructionCelebrationFX` mới: khói chân → sao EXP bay lên → 4 đợt confetti TRƯỚC công trình → bóng bay lên trời; hook additive vào ConstructionManager (toggle tắt/bật được).

**Từ video 3 (Family Farm — popup Lên Cấp):**
- ✅ Popup V2: dải quà 5-6 món (bảng quà L2→L30 mới, giữ nguyên 100% vàng+gem đã duyệt), hết vòng tròn trắng rỗng, sparkle + tia sáng, chạm màn hình để đóng.
- ✅ Chạm ô quà → nhún mẩy + tooltip thông tin món quà (`GiftSlotBounceTooltip`).
- ✅ Art 4 nhân vật của đội vẽ: Sếp DUYỆT style; lỗi frame morph được giải quyết bằng cách bỏ lật frame, chuyển sang code diễn (mục B).

**Chỉ đạo mới nhất của Sếp (chuyển động như film, nhân vật "có hồn", khách du lịch hết "ảnh nhún nhún"):**
- ✅ `CelebrationCharacterSlot` V3.1 — chế độ PUPPET "thở": nhân vật đứng yên nhưng NGỰC PHỒNG/XẸP rất chậm (~3.2s/nhịp, ~2%), nghiêng đầu vi tế, THỈNH THOẢNG (4-7s ngẫu nhiên) mới nhún 1 cú chậm rãi 1.2s; hỗ trợ chớp mắt nếu có thêm 1 hình mắt nhắm. Xem GIF `demo_breathe_film.gif`.
- ✅ `NpcBreathingIdle` — bản world-space CÙNG ngôn ngữ chuyển động cho khách du lịch: chỉ thở khi ĐỨNG YÊN (đi lại vẫn dùng walk animation), mỗi khách lệch nhịp ngẫu nhiên. Tool gắn/gỡ vào 11 prefab `Tourist_NV01..11` bằng 1 nút (có menu hoàn tác).
- ✅ Tool `Gắn art PUPPET (1 hình master)` — dùng art char_01..04 Sếp đã duyệt, chỉ lấy hình đẹp nhất mỗi con.

## B. CẦN SẾP LÀM TRONG UNITY (1 lượt, theo thứ tự)
1. Đợi compile 0 lỗi đỏ.
2. `Tools/Farm Game/Reward FX/★ Setup Reward Fly FX` → Ctrl+S.
3. `Tools/Farm Game/Level Up Popup/★ Nâng cấp V2 (1 nút)` → rồi `Gắn art PUPPET (1 hình master — khuyến nghị)` → Ctrl+S. (KHÔNG cần chạy menu "Gắn art nhân vật V2" 12-frame nữa.)
4. `Tools/Farm Game/Level Rewards/Đổ quà V2 (DRY-RUN)` → duyệt → (APPLY).
5. `Tools/Farm Game/Tourist Boat/Thêm hiệu ứng THỞ cho khách (11 prefab)` → OK.
6. Play test: Debug Preview L2/L5 (popup: thở + thi thoảng nhún + chạm quà ra tooltip + chạm nền đóng) · nhận vàng/gem (bay về HUD) · xây 1 công trình (pháo hoa trước mặt) · đợi thuyền cập bến (khách đứng thở).

## C. 4 THẺ TASK — VIẾT SẴN ĐỂ BÀN GIAO (Sếp chọn 1 thẻ đưa bên khác phụ)

### 🎯 TASK-1 · Tinh chỉnh "độ có hồn" chuyển động + QA cảm giác (⭐ GỢI Ý BÀN GIAO — độc lập, không đụng code ai)
- **Việc:** vào Unity chỉnh tham số trên Inspector cho tới khi "đã mắt": popup — `CelebrationCharacterSlot` (breatheCycle 2.5-4s, breatheAmount 0.015-0.03, bounceEvery 4-7s, bounceDuration 1-1.5s); khách du lịch — `NpcBreathingIdle` trên 11 prefab Tourists (thở chậm hơn popup một chút, bounceSquash ≤0.05). Quay video trước/sau.
- **File liên quan:** `Assets/_Game/Farm/Scripts/UI/CelebrationCharacterSlot.cs`, `Assets/_Game/Farm/Scripts/FX/NpcBreathingIdle.cs` (chỉ chỉnh Inspector, KHÔNG sửa code).
- **Nghiệm thu:** video 30s popup + bến thuyền; Sếp xem thấy "nhân vật sống" là đạt; 0 lỗi đỏ Console.

### 🎯 TASK-2 · Chạy 6 bước Unity mục B + smoke test toàn bộ gói UI Juice
- **Việc:** bấm đúng thứ tự 6 bước, ghi lại report Console từng menu, Play test 4 kịch bản, báo lỗi (nếu có) kèm ảnh.
- **Nghiệm thu:** checklist B tick đủ, save scene, danh sách bug (nếu có) gửi Lead.

### 🎯 TASK-3 · Art bổ sung "chớp mắt" + chốt icon vàng toàn game (đội vẽ)
- **Việc:** vẽ 4 file `char_0N_blink.png` (y hệt hình master từng con, chỉ MẮT NHẮM — 512×512, cùng vị trí) giao vào `production/art-handoff/2026-09-01_UI_Juice/characters/char_0N/`; sau đó Sếp chạy lại menu Gắn art PUPPET là nhân vật biết chớp mắt. Kèm: chạy `Đồng bộ icon vàng (DRY-RUN→APPLY)` sau khi Sếp duyệt danh sách.
- **Nghiệm thu:** 4 file blink đúng spec (Lead QC pixel), nhân vật chớp mắt trong popup.

### 🎯 TASK-4 · Xác nhận itemId còn thiếu để mở khoá thêm quà (trứng/sữa/cám/vé…)
- **Việc:** xác nhận id thật trong game cho các món đang "chờ xác nhận id" trong `BANG_QUA_LEVELUP_V2_2026-09-01.md` (trứng: `trung` hay `egg`? sữa: `sua` hay `milk`? cám gà? vé?) → gửi Lead danh sách → Lead cập nhật tool Đổ quà V2 → chạy lại APPLY.
- **Nghiệm thu:** mỗi level đủ 5-6 món hiện icon thật, nhận quà cộng đúng vào kho.

> Lời khuyên của Lead: giao **TASK-1** cho bên khác là hợp nhất — thuần chỉnh Inspector + quay video, không sợ dẫm chân code; TASK-2 Sếp tự bấm nhanh nhất; TASK-3 là của đội vẽ; TASK-4 cần người hiểu data game.
