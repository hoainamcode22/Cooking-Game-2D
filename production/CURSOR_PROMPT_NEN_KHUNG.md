# PROMPT DÁN VÀO CURSOR — Dựng nền & khung (không cần asset)

> Copy toàn bộ khối dưới đây dán vào Cursor (Agent/Composer). Nó chỉ dựng CODE + KHUNG;
> bạn tự thêm ảnh/sprite/âm thanh sau qua Inspector. Làm tuần tự Phase 0→4.

---

Bạn là lập trình viên Unity senior làm việc trên project game nông trại 2D có sẵn tại repo này
(Unity 6.3 LTS, C#, URP). Nhiệm vụ: DỰNG NỀN & KHUNG cho 3 hệ thống bên dưới. Tôi sẽ tự thêm
ảnh/sprite/âm thanh sau — bạn KHÔNG cần asset để hoàn thành phần code.

## ĐỌC TRƯỚC KHI LÀM (Phase 0 — chưa sửa gì)
Đọc và tóm tắt lại cho tôi 5 dòng hiểu biết, rồi mới bắt đầu:
- `CLAUDE.md`, `production/AUTONOMY.md`, `production/AUTOPILOT_BACKLOG.md` (Milestone 0 & 1)
- `production/MISSIONS_MASTER_LIST.md`, `production/session-state/MISSIONS_L1_L10_PROPOSAL.md`
- `PHASE2_SHOP_ANIMAL_TUTORIAL.md` (mẫu tutorial chăn nuôi gà L2 — LÀM GIỐNG cách này cho L4)
- Hệ tutorial sẵn có: `Assets/_Game/Farm/Scripts/Tutorial/TutorialManager.cs`,
  `TutorialStepData.cs`, `TutorialActionHandGuide.cs`, `TutorialRuntimeTargetResolver.cs`,
  `TutorialCameraFocus.cs`, `UnmaskRaycastFilter.cs`,
  `Assets/_Game/Farm/Editor/SetupTutorialL2Tool.cs`, thư mục `Assets/Resources/TutorialSteps/L1_L2/`.
- Script khác: `FarmEconomyManager.cs`, `FarmLevelManager.cs`, `Assets/_Game/Scripts/Mission/MissionData.cs`,
  `MissionProgressTracker.cs`, `PopupEwarManager.cs`, `MissionItemUI.cs`, `PlotController.cs`,
  `Assets/_Game/Farm/Scripts/Animal/MiniPanel/PenMiniPanelUI.cs`,
  `Assets/_Game/Farm/Scripts/Shop/ShopManager.cs`, `VillageOrderManager.cs`, `CookingChallengeManager.cs`.

## LUẬT BẮT BUỘC (an toàn)
1. CHỈ làm việc CỘNG THÊM: tạo class/field/method mới có default an toàn. KHÔNG đổi chữ ký public
   đang được UI/scene dùng. KHÔNG xoá hay viết lại logic cũ (ô đất, thu hoạch, order, cooking, level).
2. KHÔNG sửa tay file `.unity` / `.prefab` / `.asset`. Nếu cần tạo data → viết **Unity Editor Tool**
   (menu `Tools/Farm Game/...`) để tôi bấm chạy; tool phải có log report + hỗ trợ Undo.
3. Mọi field tham chiếu Sprite/AudioClip/asset → để `[SerializeField]` + comment `// TODO: gán trong Inspector`.
   ĐỪNG hardcode đường dẫn asset. Tôi sẽ tự gán.
4. Giữ phong cách code hiện tại: pattern singleton `Instance` + `DontDestroyOnLoad`, dùng ScriptableObject
   cho data, event `Action<>`. Mã phải BIÊN DỊCH 0 LỖI.
5. KHÔNG commit/push. Cuối mỗi phase in danh sách file đã tạo/sửa + việc tôi cần làm trong Unity.

## PHASE 1 — Hoàn thiện Tutorial (L3→L5, trọng tâm L4 chăn nuôi heo)
Tái dùng hệ tutorial ĐÃ CÓ (TutorialManager + step asset + tay/mask/camera). LÀM GIỐNG cách tutorial
chăn nuôi gà L2 trong `PHASE2_SHOP_ANIMAL_TUTORIAL.md`. CHỈ dựng KHUNG + logic + step-data; ảnh NPC/
minh hoạ tôi tự gán (để field Sprite trống + `// TODO: gán trong Inspector`). Text tiếng Việt cho trẻ em
được phép điền sẵn trong step-data.
- **Hook mới (cộng thêm):** `WaitForBuyPen(string penId)` bắn từ `ShopManager`/`PlacementManager` khi
  mua + đặt chuồng; đếm khẩu phần thức ăn trong `PenMiniPanelUI` (`foodCount`, bắn `WaitForFeed` mỗi lần
  cho ăn). Tái dùng `TrySpeedUpGem`, `TryHarvest`, `CinematicFocus`, `GuideSweepPlots`, `EnableAreaMask` sẵn có.
- **Nhánh L4 trong `TutorialManager`** (mirror nhánh L2 đã có).
- **Editor Tool `Tools/Farm Game/Setup Tutorial L4 (Pig)`**: clone `SetupTutorialL2Tool`, sinh step asset
  `L4_01..L4_09` trong `Assets/Resources/TutorialSteps/L4/` theo bảng dưới (report + Undo):

  | Step | Nội dung (thoại) | Camera/Tay/Mask | Chờ |
  |---|---|---|---|
  | L4_01 | Popup lên cấp 4: "Chuồng heo đã mở!" | pháo hoa popup | bấm Nhận quà |
  | L4_02 | "Vào Shop mua chuồng heo nhé!" | tay Btn_Home→Btn_Store | WaitForOpenShop |
  | L4_03 | Chỉ item Chuồng heo (600 vàng) | mask quanh item, tay nút Mua | WaitForBuyPen("pen_pig") |
  | L4_04 | "Đặt chuồng vào chỗ trống" | tay drag đặt | đặt xong (PlacementManager) |
  | L4_05 | Zoom chuồng, "Chạm để cho heo ăn" | CinematicFocus(pen) | click chuồng mở panel |
  | L4_06 | "Kéo bắp cải cho heo ăn" (1/2) | tay drag-guide | TryFeed lần 1 |
  | L4_07 | "Cho ăn thêm 1 phần nữa" (2/2) | tay drag-guide | TryFeed lần 2 → Processing |
  | L4_08 | "Chờ, hoặc bấm 💎 cho xong ngay" | tay nút gem | hết giờ HOẶC TrySpeedUpGem |
  | L4_09 | "Cầm rổ thu thịt heo!" + teaser "Cấp 6 mở chuồng bò" | tay drag thu hoạch | TryHarvest |

- **Template tái dùng:** gói bước chăn nuôi thành generator nhận tham số (`animal, foodItemId, portions,
  productItemId, penId`) để sinh **L6 (bò)** + **L8 (bò sữa)** chỉ bằng đổi tham số.
- **L5 (skeleton):** sequence "Bếp đã mở!" — popup + hint sang scene cooking + guide nấu món đầu
  (reuse hook cooking). Ảnh để trống.
- Acceptance: chạy tool L4 → vào Play ở cấp 4 → luồng liền mạch: mua→đặt→cho ăn 2 phần→gem→thu thịt→teaser bò;
  0 lỗi đỏ; L6/L8 sinh được bằng đổi tham số.

## PHASE 2 — Hệ Nhiệm vụ hoàn chỉnh (theo MISSIONS_MASTER_LIST.md)
Theo đúng kế hoạch "thay đổi tối thiểu" trong `MISSIONS_L1_L10_PROPOSAL.md` §5:
- `MissionData.cs`: THÊM field `string missionId; int requiredLevel = 1; MissionKind kind = Main;`
  `MissionEventType eventType; string targetItemId;` + 2 enum mới (`MissionKind{Main,Daily}`,
  `MissionEventType{PlantItem,HarvestItem,FeedAnimal,CollectProduct,DeliverOrder,DeliverOrderWithItem,
  DeliverComboOrder,BuyItem,CookDish,CookBeefDish,ReachLevel,TotalOrders,ProcessItem,UpgradeStorage,
  CatchFish,ServeBoat,PlaceDecor}`). Tùy chọn: `secondRewardType/secondRewardAmount` cho thưởng kép.
  GIỮ 6 field cũ → asset cũ không vỡ.
- `MissionProgressTracker.cs`: THÊM `event Action<string,int> OnProgressChanged` (bắn trong Set/AddProgress);
  PERSIST qua PlayerPrefs (`mission_prog_<key>`, `mission_claimed_<id>`) — có thể chuyển sang SaveSystem sau; API mới
  `ReportEvent(MissionEventType type, string itemId, int amount)` ghi key chuẩn `"<type>:<itemId>"` +
  `"<type>:any"`; tự subscribe `FarmLevelManager.OnLevelChanged` → `SetProgress("ReachLevel:any", level)`;
  daily reset theo `yyyyMMdd`. GIỮ `AddProgress` cũ (chuyển tiếp vào ReportEvent HarvestItem).
- Hook 8 điểm gameplay (mỗi chỗ 1–3 dòng, SỬA LỖI lệch key hiện tại): `PlotController` (plant+harvest),
  `PenMiniPanelUI` (feed + collectProduct), `VillageOrderManager` (deliver + combo + withItem + TotalOrders),
  `CookingChallengeManager` (cook + cookBeef), `ShopItemUI`/`MarketManager` (buy).
- `PopupEwarManager` + `MissionItemUI`: lọc hiển thị theo `requiredLevel <= FarmLevelManager.CurrentLevel`,
  tách nhóm Main/Daily/Achievement, đọc tiến độ bằng KEY CHUẨN (không dùng missionName), subscribe
  `OnProgressChanged` để cập nhật realtime, claim đọc/ghi persist. GIỮ chữ ký public + prefab cũ.
- Editor Tool `Tools/Farm Game/Setup Missions (L1-L30)`: đọc bảng A/B/C trong `MISSIONS_MASTER_LIST.md`,
  sinh asset `Mission_<missionId>.asset` + database Main/Daily/Achievement, report + Undo.
  (Icon mission để trống — tôi gán sau.)
- Acceptance: mở popup ở L1 chỉ thấy mission L1; lên cấp hiện mission mới; tiến độ chạy realtime; claim
  1 lần và giữ qua phiên; daily reset theo ngày.

## PHASE 3 — 3 móc giữ chân (khung UI, ảnh để trống)
- **Teaser unlock kế tiếp**: component HUD `NextUnlockTeaserUI` hiện "Cấp tới mở: <tên> 🔒" + thanh EXP
  tới cấp sau (đọc LevelReward/unlockLevel sẵn có). Ẩn khi max. Field icon/sprite để trống.
- **Daily login wheel / streak 7 ngày**: `DailyRewardManager` (lưu qua PlayerPrefs: ngày cuối nhận + streak)
  + khung popup `DailyRewardPopupUI` (7 ô quà). Cộng vàng/gem thật qua FarmEconomyManager. Sprite ô để trống.
- **Badge "đã chín/đã xong" + đếm ngược**: component `ReadyBadgeUI` gắn được lên cây/chuồng, đọc timer sẵn có,
  hiện "✓" khi xong + countdown khi đang chạy. Prefab/sprite badge để trống cho tôi gán.
- Acceptance: 3 thứ chạy bằng placeholder (text/box) không cần ảnh; tôi thay ảnh sau mà không phải sửa code.

## PHASE 4 — Kiểm tra & báo cáo
- Bảo đảm project biên dịch 0 lỗi; nếu có test/smoke script thì chạy.
- In báo cáo cuối: (1) file đã tạo/sửa theo từng phase; (2) **"CẦN BẠN TRONG UNITY"** — liệt kê chính xác:
  menu tool nào bấm, sprite/âm thanh/prefab nào cần gán, object nào cần đặt tên/đúng vị trí; (3) việc còn
  lại chưa làm được vì thiếu asset/quyết định.

Bắt đầu từ Phase 0. Sau mỗi phase, dừng lại tóm tắt ngắn rồi sang phase tiếp theo.
