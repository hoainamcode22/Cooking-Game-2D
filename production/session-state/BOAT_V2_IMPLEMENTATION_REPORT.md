# BÁO CÁO — HỆ TÀU KHÁCH DU LỊCH V2 (BOAT-002)

> Phiên 2026-08-29 · Lead + 3 Dev song song + QA 2 vòng · **Trạng thái: CODE XONG, đã copy vào project, chờ Sếp chạy tool + playtest**
> Backup toàn bộ file gốc: `production/backup_boat_2026-08-29/` — hỏng chỗ nào cứ copy ngược lại là về như cũ.

---

## 1. ĐÃ LÀM ĐƯỢC GÌ

Từ ý tưởng của Sếp, hệ tàu cũ (tàu chỉ chạy vào, đậu 40 phút cố định rồi đi — không có ai xuống) đã thành
một vòng lặp chơi thật sự:

**Tàu cập bến sát bờ → bắc ván gỗ xuống → 3–6 khách du lịch xuống tàu lần lượt → đi theo đường đất →
xếp hàng trước nhà hàng → bong bóng món ăn mở lần lượt trên đầu từng khách → Sếp nấu món, giao cho khách →
khách trả vàng + EXP, mặt cười bay lên HUD → khách về tàu → khách cuối lên tàu thì tàu rút ván, rời bến →
5 phút sau tàu tới lại (10 phút khi mở nhiều bến, so le nhau).**

Thay đổi nền tảng: tàu **không còn chạy theo đồng hồ cố định** mà chờ theo sự kiện — khách xong việc thì tàu mới đi.

### Các con số đã chốt với Sếp
| Hạng mục | Giá trị |
|---|---|
| Số khách mỗi chuyến | random **3–6** (11 nhân vật, không trùng nhau trong cùng chuyến) |
| Món khách gọi | random trong **38 món** thật của game, lọc theo level đã mở |
| Vàng khách trả | **Σ giá nguyên liệu chính × 2** (không tính muối/tiêu/nước mắm) |
| EXP | theo `rewardExp` của từng món |
| Kiên nhẫn khách | **30 phút**, chạy song song cho mọi khách |
| Hết kiên nhẫn | mặt **tức giận**, bỏ về tàu, không trả tiền |
| Tàu kế tiếp | **5 phút** (1 bến) · **10 phút** so le (nhiều bến) |
| Lưới an toàn | tàu đậu quá **35 phút** tự rời bến — game không bao giờ kẹt cứng |

> **Vì sao 35 mà không phải 30 như Sếp nói:** đồng hồ 30 phút của KHÁCH vẫn đúng nguyên ý Sếp. Còn 35 là mốc
> của TÀU, tính từ lúc chạm bến — khách phải đi bộ mất ~12 giây mới tới hàng rồi mới bắt đầu đếm 30 phút.
> Nếu để cả hai đều 30 thì tàu luôn bỏ đi *trước* khi khách kịp giận, Sếp sẽ không bao giờ nhìn thấy hiệu ứng
> mặt tức giận, và console sẽ liên tục cảnh báo. QA phát hiện ra chỗ này, để 35 là vừa khít.

---

## 2. HÀNG ĐÃ GIAO

**21 file C# (11.161 dòng)** đã copy sẵn vào project:

| Nhóm | File |
|---|---|
| Lịch tàu (Dev A) | `BoatScheduleCore` · `BoatDockManager` · `TouristBoatConfig` · `TouristBoatController` · `BoatShoreAdjustTool` · `TouristBoatDiagnosticTool` (vá lại cho khớp V2) |
| Khách du lịch (Dev B) | `TouristVisitorManager` · `TouristAgent` · `TouristQueue` · `TouristRequestBubble` · `TouristSmileyFlyFX` · `GangplankController` · `NPCAnimationSetupTool` · `TouristVisitorSetupTool` |
| Giao diện (Dev C) | `BoatAnnouncePopupUI` · `DockPurchasePopupUI` · `DockUnlockCelebrationFX` · `TouristBoatUnlockFlow` · `BoatDockSlot` · `TouristBoatUIPopupSetupTool` |
| Kiểm thử | `tests/unit/touristboat/BoatScheduleCoreTests.cs` |

**132 frame nhân vật** đã cắt sẵn tại `Assets/NV_NPC/NVGAME/Processed/NV01..NV11/`:
11 nhân vật × 4 hướng × 3 frame, **đã xóa phông trắng**, nền trong suốt hoàn toàn, chân chạm đáy canvas
(pivot Bottom-Center chuẩn), cao 256px, canvas đồng nhất từng nhân vật nên đi bộ không giật.
*(Ảnh gốc Sếp đưa là 11 sheet lưới 4×3 nền trắng — tôi bóc tách, khử nền bằng flood-fill từ viền, làm mềm
viền 1px chống răng cưa, chuẩn hoá canvas, và tạo hướng phải bằng cách lật gương hướng trái cho đối xứng tuyệt đối.)*

**3 Editor tool mới** (menu `Tools/Farm Game/Tourist Boat/`): Setup NPC Animations · Setup Tourist Visitors (Scene) ·
Setup Popups (UI) · Dịch bến sát bờ.

---

## 3. KIỂM THỬ

| Hạng mục | Kết quả |
|---|---|
| Biên dịch (3 pass: có Editor / giả lập bản build / gộp cả tool cũ) | **0 lỗi · 0 cảnh báo** |
| Test tự động lịch tàu (chạy thật bằng mono) | **119 PASS / 0 FAIL** |
| QA vòng 1 | 4 BLOCKING · 6 MAJOR · 11 minor |
| QA vòng 2 (sau khi Dev sửa) | **21/21 đóng hết** — verdict **SHIP** |

4 lỗi nghiêm trọng QA bắt được ở vòng 1, đều đã sửa tận gốc:
1. **Tàu kẹt vĩnh viễn** nếu thiếu 1 object trong scene → giờ có 4 lớp chống kẹt độc lập.
2. **Mất món của người chơi**: món bị trừ khỏi kho nhưng trả 0 vàng → giờ kiểm tra đủ điều kiện xong mới trừ kho, và có sàn tối thiểu 1 vàng.
3. **Popup báo tàu chết vĩnh viễn** sau lần đầu vào bếp → chuyển popup sang canvas riêng.
4. **Không tua nhanh test được** → đồng hồ khách giờ ăn theo `debugTimeScale` cùng nhịp với tàu.

---

## 4. ⚠️ ANH CẦN LÀM TRONG UNITY

Checklist đầy đủ 50 bước nằm ở `production/session-state/QA_REPORT_BOAT_V2.md` mục **7.8**. Rút gọn:

### Bắt buộc, làm đúng thứ tự
1. **Mở Unity, đợi compile** → phải **0 lỗi đỏ**. (Code đã copy sẵn, anh không phải copy gì.)
2. Mở `Assets/_Game/ScriptableObjects/TouristBoatConfig.asset` → điền **13 field mới**:
   `gapOneDockMinutes=5` · `gapMultiDockMinutes=10` · `minStaggerMinutes=3` · **`maxDockMinutes=35`** ·
   `visitorsMin=3` · `visitorsMax=6` · `patienceMinutes=30` · `rewardIngredientMultiplier=2` ·
   `disembarkInterval=0.8` · `visitorWalkSpeed=150` · `queueSpacing=120` · `bubbleScaleInTime=0.25` · `smileyFlyTime=1.2`
3. Chạy `Tools/Farm Game/Tourist Boat/**Setup NPC Animations**` → phải báo **11/11 nhân vật OK**
   (tự import 132 ảnh, tạo animation 4 hướng, tạo 11 prefab khách).
4. Mở scene `SCN_Farm` → chạy `**Setup Tourist Visitors (Scene)**` → rồi `**Setup Popups (UI)**`.
5. Chạy `**Dịch bến sát bờ**` → bấm *Tự suy hướng bờ* → *Áp dụng cho 3 bến* → nhìn scene chỉnh tay cho vừa mắt.
6. **Việc chỉ anh làm được (quan trọng nhất):** kéo `WP_01..WP_04` của 3 đường `TouristPath_Dock0X` **bám theo
   đường đất anh đã vẽ**, và kéo `QueueAnchor` ra **trước cửa nhà hàng cooking**. Tool đặt tạm, anh canh mắt mới đúng.
   Xong **Ctrl+S**.
7. Play test: lên Lv10 → xem tàu vào, khách xuống, xếp hàng, nấu món giao thử 1 khách.

### Tùy chỉnh nếu thấy chưa vừa mắt
- Khách đi nhanh/chậm quá → sửa `visitorWalkSpeed`. Hàng chờ thưa/dày quá → `queueSpacing`.
- Khách bị vật thể che → tăng sorting order trên prefab khách (mặc định đã để 5000, khá cao).
- Muốn test nhanh không chờ 5 phút → đặt `debugTimeScale = 60` (1 giây thực = 1 phút game), test xong trả về 1.

---

## 5. GỬI ĐỘI VẼ

Prompt đã viết sẵn (đã dán nguyên khối LUẬT ART STUDIO): `production/session-state/PROMPT_SPRITE_FORGE_BOAT_V2.md`
— **15 asset**, ưu tiên cao nhất là **4 frame tấm ván gỗ** (thiếu nó khách đi trên mặt nước) và **bong bóng + mặt cười/mặt tức giận**.

Game **chạy được ngay không cần chờ art**: tất cả chỗ thiếu ảnh đều có hình tạm vẽ bằng code
(bong bóng trắng, mặt cười vàng, mặt tức giận đỏ, ván gỗ giãn ngang). Art về chỉ việc kéo vào Inspector, không phải sửa code.

---

## 6. CẦN SẾP QUYẾT (không gấp)

1. **Nhiệm vụ (mission)**: hiện tôi để **TẮT** việc phục vụ khách du lịch tính vào nhiệm vụ. Lý do: loại sự kiện
   gần nhất là `DeliverOrder` vốn dành riêng cho Bảng Đơn Hàng — bật lên sẽ khiến khách du lịch "hoàn thành hộ"
   nhiệm vụ giao đơn của làng. Muốn tính thì nên thêm loại nhiệm vụ riêng, Sếp gật là làm.
2. **Sprint 5 tồn đọng** (24 nhà trùng lặp + 13 missing script trong scene) vẫn đang chờ Sếp duyệt danh sách xóa.
3. Số khách tối đa 18 người (3 bến × 6) khi zoom xa có thể hơi rối bong bóng — chơi thử rồi Sếp quyết có giảm không.
