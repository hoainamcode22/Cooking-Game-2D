# PROMPT DÁN VÀO CURSOR — Đổ list Nhiệm vụ / Thành tựu / Lên cấp vào popup (qua tool)

> Copy khối dưới dán vào Cursor. Làm tuần tự, chạy tool trong Unity sau mỗi task, verify 0 lỗi đỏ.
> Dữ liệu nguồn: `production/MISSIONS_MASTER_LIST.md` + `production/REWARDS_MASTER_LIST.md`.

---

Bạn là lập trình viên Unity senior trên project này. Mục tiêu: đổ ĐẦY ĐỦ list nhiệm vụ / thành tựu /
phần thưởng lên cấp vào các popup hiện có, BẰNG CÁCH mở rộng các Editor Tool sẵn có (KHÔNG sửa tay
file `.asset`/`.unity`). Mọi data sinh qua tool, idempotent, có verify.

## ĐỌC TRƯỚC (Phase 0)
`Assets/_Game/Scripts/Mission/MissionData.cs` (schema: missionId/requiredLevel/eventType/targetItemId/isDaily — ĐÃ có),
`MissionDatabase.cs`, `PopupEwarManager.cs`, `Assets/_Game/Scripts/Mission/UnifiedTaskPopupUI.cs`,
`Assets/_Game/Farm/Editor/MissionSetupTool.cs` (mảng MAIN[]/DAILY[], CreateOrUpdate, LoadOrCreateDatabase),
`Assets/_Game/Farm/Editor/LevelUpRewardDataSetupTool.cs` (REWARD_TABLE, tự gán vào LevelUpPopupUI),
`Assets/_Game/Farm/Scripts/UI/LevelRewardConfig.cs`, và 2 file data nguồn ở `production/`.

## LUẬT
1. CHỈ cộng thêm/cập nhật mảng dữ liệu trong tool + thêm field/method mới. KHÔNG sửa tay `.asset`/`.unity`,
   KHÔNG đổi chữ ký public đang dùng, KHÔNG xoá logic cũ. Giữ tool idempotent (chạy lại không tạo trùng).
2. Sprite/icon: tái dùng cơ chế lookup icon sẵn có trong tool; thiếu thì để null (tôi gán sau).
3. Biên dịch 0 lỗi. KHÔNG commit. Cuối mỗi task in: file đã đụng + menu cần tôi chạy trong Unity.

## ⚠ GIỚI HẠN eventType (BẮT BUỘC tuân theo — đừng bịa enum)
Enum `MissionEventType` chỉ có 9 loại: `HarvestItem, DeliverOrder, CookDish, FeedAnimal,
CollectAnimalProduct, BuyShopItem, BuySeed, ReachLevel, PlantCrop`. Map list như sau:
- "Trồng X" → `PlantCrop`; "Thu hoạch X" → `HarvestItem`; "Cho ăn" → `FeedAnimal`;
  "Thu sản phẩm chuồng" → `CollectAnimalProduct`; "Mua chuồng/đồ" → `BuyShopItem` (chuồng heo id `108`,
  chuồng bò id `106` — verify id chuồng bò sữa/máy trong data); "Mua hạt" → `BuySeed`;
  "Nấu món / nấu món bò" → `CookDish` (món bò: set `targetItemId` = dishId như `bo_ham_ca_rot`);
  "Đạt cấp X" → `ReachLevel`.
- "Giao đơn / đơn combo / đơn có thịt bò/cá / tổng N đơn" → tất cả dùng `DeliverOrder`
  (tracking hiện đếm mọi đơn; với "đơn có thịt bò" set `targetItemId="beef"` nếu tracker hỗ trợ lọc item, nếu không thì để rỗng).
- "Chế biến (bột/phô mai/nước mía)" → máy chế biến tái dùng hệ chuồng ⇒ dùng `CollectAnimalProduct`
  với `targetItemId` = sản phẩm máy (vd `flour`, `cheese`). XÁC NHẬN trong code máy có bắn event này; nếu KHÔNG, để các mission này lại và báo tôi.
- **"Câu cá / phục vụ tàu / nâng kho / đặt trang trí"**: CHƯA có eventType & tính năng → **KHÔNG tạo** các
  mission này lúc này (hoặc tạo nhưng đánh dấu `requiredLevel` cao + ghi chú "chưa track"). Liệt kê chúng vào
  cuối báo cáo để tôi quyết định thêm eventType + hook sau.

## TASK 1 — Nhiệm vụ chính L1→L30
Trong `MissionSetupTool.cs`, mở rộng mảng `MAIN[]` từ L1-L10 hiện tại lên **đủ L1→L30** theo
`MISSIONS_MASTER_LIST.md §A` (đã có sẵn missionId, tên, target, thưởng), map eventType theo mục ⚠ ở trên.
Giữ nguyên format `new MissionDef(...)`. Bỏ qua mission không track được (mục ⚠). Sửa hàm Check:
`requiredLevel` cho phép **1–30** (hiện đang 1–10). Chạy menu **Tools/Farm Game/Setup Missions L1-L10**
(đổi tên menu thành "Setup Missions L1-L30" nếu muốn) → `MissionDatabase_Main` được popup tự đọc, lọc theo level.

## TASK 2 — Nhiệm vụ ngày (Daily)
Mở rộng mảng `DAILY[]` theo `MISSIONS_MASTER_LIST.md §B` (pool đầy đủ, chỉ loại sự kiện track được).
`isDaily=true`, `requiredLevel=6`. Chạy tool → `MissionDatabase_Daily`.

## TASK 3 — Thành tựu (Achievement) + nối vào tab "Thành tựu"
1. Thêm mảng `ACHIEVEMENTS[]` trong `MissionSetupTool.cs` theo `MISSIONS_MASTER_LIST.md §C` (chỉ loại track được:
   thu/giao/nấu/đạt cấp/chế biến/login). `isDaily=false`, `requiredLevel=1`, eventType map như mục ⚠.
2. Tạo `MissionDatabase_Achievement.asset` (giống cách tạo Main/Daily) chứa các achievement.
3. Trong `UnifiedTaskPopupUI.cs`: thêm `[SerializeField] private MissionDatabase achievementDatabase;`
   + resolve giống mission/daily; **đổ dữ liệu vào Panel_Achievement** (mirror cách build panel Mission:
   spawn item theo từng MissionData, hiện tiến độ + nút nhận). Giữ chữ ký public cũ.
4. Tool tự gán `achievementDatabase` vào component `UnifiedTaskPopupUI` trong scene (giống cách
   `LevelUpRewardDataSetupTool` tự gán configs vào `LevelUpPopupUI`). Chạy tool.

## TASK 4 — Phần thưởng lên cấp L1→L30 (gần như đã xong)
`LevelUpRewardDataSetupTool` đã sinh LevelRewardConfig L2-L30 + tự gán vào `LevelUpPopupUI`. Chỉ cần
**cập nhật `REWARD_TABLE`** (vàng/gem/giftItems/unlockDescriptions) cho khớp `REWARDS_MASTER_LIST.md §1`
(quà vật phẩm + mô tả mở khoá từng cấp). Giữ format `G(...)` ItemGift. Chạy menu
**Tools/Farm Game/Setup Level Up Popup/Setup Reward Data (L2-L30)**.

## VERIFY (Phase cuối)
- Biên dịch 0 lỗi. Chạy **Tools/Farm Game/Test/Check Missions** (sửa cap 30) → PASS.
- Vào Play: mở popup nhiệm vụ ở cấp 1 (chỉ thấy mission L1), lên cấp thấy mission mới; tab Hằng ngày hiện 3 việc;
  tab Thành tựu hiện list + nhận thưởng được; popup lên cấp L2/L5/... hiện đúng quà.
- In báo cáo: file đụng + menu đã chạy + **danh sách mission CHƯA track được** (câu cá/tàu/…) để tôi quyết định bước sau.

Bắt đầu Phase 0, sau mỗi task dừng tóm tắt ngắn rồi chạy tool/verify trước khi sang task kế.
