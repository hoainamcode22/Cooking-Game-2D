# ĐỀ XUẤT HỆ THỐNG NHIỆM VỤ L1→L10 + NHIỆM VỤ NGÀY (BẢN KHẢO SÁT — CHỜ DUYỆT)

> Khảo sát code ngày 2026-06-12. Tài liệu này CHỈ LÀ ĐỀ XUẤT — chưa sửa bất kỳ file nào trong `Assets/` cho hệ mission.
> Tham chiếu chéo: `L1_L10_DESIGN_PLAN.md` (cột "Mission chính" trùng khớp danh sách bên dưới).

---

## 1. Hiện trạng — Schema `MissionData`

File: `Assets/_Game/Scripts/Mission/MissionData.cs` (ScriptableObject, menu `Game/Mission Data`)

| Field | Kiểu | Ghi chú |
|---|---|---|
| `missionIcon` | Sprite | icon nhiệm vụ |
| `missionName` | string | TÊN HIỂN THỊ, đồng thời bị dùng làm KEY tiến độ (xem mục 2 — đây là lỗi) |
| `targetAmount` | int | số lượng mục tiêu |
| `rewardIcon` | Sprite | icon thưởng |
| `rewardAmount` | int | số lượng thưởng |
| `rewardType` | enum `RewardType { Coin, Diamond }` | chỉ 2 loại |

**KHÔNG có:** `requiredLevel` / level gate, `missionId`, enum loại nhiệm vụ (Main/Daily), key sự kiện theo dõi (trackKey), không có field nào về thời hạn/daily.

Data hiện có: `MissionDatabase_Main.asset` (Data_Ewa) chứa **20 asset** dạng `Mission_<itemId>.asset` (rice, ngo, cachua, egg, beef, pork, hoa_hong, tulip, pho_beef, ga_xao_ot…). Ví dụ `Mission_rice.asset`: `missionName: "Thu thập 10 Lúa"`, `targetAmount: 10`, `rewardAmount: 150`, `rewardType: 0 (Coin)`. Tất cả hiển thị cùng lúc, không phân theo level.

## 2. Hiện trạng — Cách theo dõi tiến độ

File: `Assets/_Game/Scripts/Mission/MissionProgressTracker.cs` — singleton `DontDestroyOnLoad`, chỉ là `Dictionary<string,int>` trong RAM với `GetProgress/SetProgress/AddProgress`. **Không lưu** (mất khi tắt game), **không có event** khi tiến độ đổi.

**Toàn bộ gameplay chỉ có ĐÚNG 1 chỗ gọi tracker** (grep toàn project):

- `Assets/_Game/Farm/Scripts/Gameplay/PlotController.cs:584` — `MissionProgressTracker.Instance?.AddProgress(harvestItemId, amount);` khi thu hoạch.

**LỖI LỆCH KEY (quan trọng):** PlotController ghi tiến độ theo **itemId** (vd `"rice"`), nhưng UI đọc theo **`missionName`** (`PopupEwarManager.cs:134` → `GetProgress(item.Data.missionName)` = `"Thu thập 10 Lúa"`). Hai key không bao giờ trùng ⇒ **tiến độ thu hoạch hiện không bao giờ hiển thị lên UI nhiệm vụ**. Ngoài ra `PopupEwarManager.NotifyProgressChanged()` là code chết — không nơi nào gọi; UI chỉ refresh lúc mở popup.

Chưa có hook nào cho: trồng cây, cho thú ăn, thu trứng/sữa, giao đơn, mua hàng, nấu ăn, lên cấp.

## 3. Hiện trạng — Nhận thưởng trên UI

- `PopupEwarManager.cs` (popup "Ewa"): mở popup → `SpawnMissionItems()` instantiate `missionItemPrefab` cho TẤT CẢ mission trong database (chỉ 1 lần/phiên, cờ `_initialized`) → `RefreshAllProgress()` đọc tracker.
- `MissionItemUI.cs`: nút claim chỉ bấm được khi `currentAmount >= targetAmount` (text "Tiến hành"/"Nhận"). `OnClaimClicked()` → `PlayerWallet.AddCoin/AddDiamond(rewardAmount)` + `AvatarProfilePopupUI.AddAchievementCount()` → chuyển sang trạng thái `obj_BtnClaimed`.
- **`_claimed` chỉ nằm trong RAM** — không lưu, phiên sau nhận lại được (claim lặp vô hạn giữa các phiên).

## 4. Còn THIẾU gì để làm mission chính theo level L1→L10 + mission ngày

1. **Level gate:** không có `requiredLevel` trong MissionData; popup không lọc theo `FarmLevelManager.CurrentLevel` (manager này ĐÃ có sẵn: `Instance`, `CurrentLevel`, `event OnLevelChanged` — `Assets/_Game/Farm/Scripts/Managers/FarmLevelManager.cs`).
2. **Loại nhiệm vụ:** không có enum Main/Daily, không có database daily, không có cơ chế reset theo ngày.
3. **Key sự kiện chuẩn:** không có `trackKey`/enum sự kiện; đang lệch key giữa ghi (itemId) và đọc (missionName).
4. **Hook gameplay:** mới chỉ có thu hoạch. Thiếu: trồng (PlotController), cho ăn (`PenMiniPanelUI.TryFeed:147`), thu sản phẩm chuồng (`PenMiniPanelUI.cs:201` AddItem productItemId), giao đơn (`VillageOrderManager.DeliverOrder:226`), mua (`ShopItemUI.BuyItem:105`, `MarketManager.TryBuy:158`), nấu (`CookingChallengeManager.HandleCookingSuccess:327`), lên cấp (`FarmLevelManager.OnLevelChanged`).
5. **Lưu trữ:** tiến độ + trạng thái đã-nhận chưa persist (project đang dùng PlayerPrefs ở nhiều manager — theo cùng pattern).
6. **UI cập nhật realtime:** tracker không có event; `NotifyProgressChanged` chưa được nối.
7. **Tiến độ kiểu "đạt mốc"** (lên cấp X, tổng N đơn): cần dạng `SetProgress` thay vì cộng dồn — tracker hiện hỗ trợ được, chỉ thiếu chỗ gọi.

## 5. Kế hoạch triển khai THAY ĐỔI TỐI THIỂU (không phá UI hiện có)

Nguyên tắc: chỉ THÊM field/method có giá trị mặc định an toàn; giữ nguyên chữ ký `MissionItemUI.Setup/UpdateProgress`, giữ nguyên prefab item; asset cũ vẫn load bình thường (field mới nhận default).

| # | File | Thay đổi |
|---|---|---|
| 1 | `Assets/_Game/Scripts/Mission/MissionData.cs` | THÊM: `public string missionId;` (rỗng → fallback `name`), `public int requiredLevel = 1;`, `public MissionKind kind = MissionKind.Main;` (enum mới `Main, Daily`), `public MissionEventType eventType;` (enum mới: `HarvestItem, PlantItem, FeedAnimal, CollectProduct, DeliverOrder, DeliverOrderWithItem, DeliverComboOrder, BuyItem, CookDish, CookBeefDish, ReachLevel, TotalOrders`), `public string targetItemId;` (lọc theo item, rỗng = mọi item). Giữ nguyên 6 field cũ ⇒ asset cũ không vỡ. |
| 2 | `Assets/_Game/Scripts/Mission/MissionProgressTracker.cs` | THÊM (không xoá API cũ): `event Action<string,int> OnProgressChanged;` bắn trong `SetProgress/AddProgress`; persist PlayerPrefs (`mission_prog_<key>`, claimed: `mission_claimed_<id>`); API mới `ReportEvent(MissionEventType type, string itemId, int amount)` ghi vào key chuẩn `"<type>:<itemId>"` và `"<type>:any"`; tự subscribe `FarmLevelManager.OnLevelChanged` → `SetProgress("ReachLevel:any", level)` (KHÔNG cần sửa FarmLevelManager); daily reset: lưu `mission_daily_date` (yyyyMMdd), khác ngày → xoá key daily. Giữ `AddProgress(harvestItemId)` cũ hoạt động: chuyển tiếp vào `ReportEvent(HarvestItem, itemId, amount)`. |
| 3 | `Assets/_Game/Farm/Scripts/Gameplay/PlotController.cs` | Dòng 584 giữ nguyên hoặc đổi thành `ReportEvent(HarvestItem, harvestItemId, amount)`; thêm 1 dòng `ReportEvent(PlantItem, cropId, 1)` tại hàm trồng (nơi trừ hạt giống). |
| 4 | `Assets/_Game/Farm/Scripts/Animal/MiniPanel/PenMiniPanelUI.cs` | `TryFeed` thành công → `ReportEvent(FeedAnimal, foodItemId, 1)`; chỗ thu sản phẩm (dòng ~201) → `ReportEvent(CollectProduct, config.productItemId, amount)`. 2 dòng. |
| 5 | `Assets/_Game/Farm/Scripts/Village/VillageOrderManager.cs` | Cuối `DeliverOrder` (sau AddGold/AddExp) → `ReportEvent(DeliverOrder, "any", 1)`; nếu `order.HasSecondItem` → thêm `DeliverComboOrder`; bắn thêm `DeliverOrderWithItem` cho từng `itemId` trong đơn (phục vụ "giao đơn bò": targetItemId = `beef`/món bò). 3–4 dòng. |
| 6 | `Assets/_Game/Scripts/CookingChallengeManager.cs` | Trong `HandleCookingSuccess` → `ReportEvent(CookDish, dishId, 1)`; nếu dishId thuộc nhóm món bò (prefix/danh sách: `pho_beef`, `bo_*`…) → thêm `CookBeefDish`. 2 dòng. |
| 7 | `Assets/_Game/Farm/Scripts/Shop/ShopItemUI.cs` + `Assets/_Game/Farm/Scripts/Market/MarketManager.cs` | Sau mua thành công → `ReportEvent(BuyItem, itemId, qty)` (hạt giống `seed_*`, chuồng heo/bò theo id chuồng). Mỗi file 1 dòng. |
| 8 | `Assets/_Game/Scripts/Mission/PopupEwarManager.cs` | `SpawnMissionItems()`: lọc `data.requiredLevel <= FarmLevelManager.Instance.CurrentLevel` và tách 2 nhóm Main (level hiện tại trước) / Daily; bỏ cờ `_initialized` (hoặc respawn khi level đổi); subscribe `tracker.OnProgressChanged` → gọi `NotifyProgressChanged` sẵn có (hết code chết, UI realtime); đọc tiến độ bằng **key chuẩn của mission** (`missionId`/trackKey) thay vì `missionName` ⇒ SỬA LỖI LỆCH KEY. UI prefab không đổi. |
| 9 | `Assets/_Game/Scripts/Mission/MissionItemUI.cs` | `Setup()` đọc trạng thái claimed từ tracker (persist); `OnClaimClicked()` ghi claimed vào tracker. Chữ ký public giữ nguyên (`Setup`, `UpdateProgress`, `IsClaimed`, `Data`). |
| 10 | Data (Data_Ewa) | Tạo asset mới `Mission_L{n}_*.asset` theo bảng mục 6 + `MissionDatabase_Daily.asset`; 20 asset cũ giữ nguyên (requiredLevel mặc định 1 → có thể nâng dần sau, không vỡ). |

Không sửa: `FarmLevelManager.cs`, prefab popup/item, `MissionDatabase.cs` (database chỉ là List<MissionData>, dùng được ngay; thêm 1 field `dailyMissions` là tuỳ chọn).

## 6. DANH SÁCH MISSION CHÍNH L1→L10 (đề xuất data)

Phần thưởng theo `RewardType` hiện có (Coin/Diamond), số liệu khớp `L1_L10_ECONOMY_TABLE.md` ở mức gợi ý — chờ duyệt.

| Level | Mission | eventType | targetItemId | target | Thưởng đề xuất |
|---|---|---|---|---|---|
| L1 | Trồng 6 lúa | PlantItem | rice | 6 | 30 Coin |
| L1 | Thu hoạch 6 lúa | HarvestItem | rice | 6 | 40 Coin |
| L1 | Trồng 2 hoa | PlantItem | huong_duong (hoặc nhóm hoa) | 2 | 30 Coin |
| L1 | Lên cấp 2 | ReachLevel | any | 2 | 50 Coin + (1 Diamond tách riêng nếu cần) |
| L2 | Cho gà ăn | FeedAnimal | (thức ăn gà) | 1 | 30 Coin |
| L2 | Thu trứng | CollectProduct | egg | 1 | 40 Coin |
| L2 | Giao 1 đơn hàng | DeliverOrder | any | 1 | 60 Coin |
| L3 | Mua 1 hạt giống mới | BuyItem | seed_* (any seed) | 1 | 40 Coin |
| L3 | Hoàn thành 3 đơn | DeliverOrder | any | 3 | 100 Coin |
| L4 | Mua chuồng heo | BuyItem | (id chuồng heo) | 1 | 120 Coin |
| L4 | Thu hoạch 2 loại hoa | HarvestItem | nhóm hoa (đếm loại — cần biến thể "unique") | 2 | 80 Coin |
| L5 | Nấu món ăn đầu tiên | CookDish | any | 1 | 100 Coin + 1 Diamond |
| L5 | Giao 1 món ăn | DeliverOrderWithItem | (dish bất kỳ) | 1 | 120 Coin |
| L6 | Nấu 3 món ăn | CookDish | any | 3 | 150 Coin |
| L6 | Mua chuồng bò | BuyItem | (id chuồng bò) | 1 | 180 Coin |
| L7 | Giao 5 đơn hàng | DeliverOrder | any | 5 | 200 Coin |
| L7 | Thu hoạch 10 nông sản | HarvestItem | any | 10 | 150 Coin |
| L8 | Nấu 1 món bò | CookBeefDish | any | 1 | 200 Coin + 2 Diamond |
| L8 | Giao đơn có thịt bò/món bò | DeliverOrderWithItem | beef / món bò | 1 | 220 Coin |
| L9 | Giao 3 đơn combo (2 món) | DeliverComboOrder | any | 3 | 300 Coin |
| L10 | Đạt cấp 10 | ReachLevel | any | 10 | 500 Coin + 5 Diamond |
| L10 | Hoàn thành 20 đơn tổng | DeliverOrder (tổng tích luỹ) | any | 20 | 400 Coin |

Ghi chú kỹ thuật: "Thu 2 LOẠI hoa" (L4) cần đếm số loại khác nhau — cách tối thiểu: tracker ghi thêm key `HarvestItem:<flowerId>` từng loại, mission dạng `UniqueFlowerTypes` đếm số key hoa > 0 (1 hàm nhỏ trong tracker), hoặc đơn giản hoá thành "thu 2 hoa" nếu muốn tránh thêm code.

## 7. MISSION NGÀY (daily) — đề xuất

- Mở từ **L6** (khớp design plan §3). Mỗi ngày chọn **3 nhiệm vụ** từ pool, seed random theo ngày (yyyyMMdd) để mọi lần mở popup trong ngày giống nhau.
- Reset: so `mission_daily_date` trong PlayerPrefs lúc tracker Awake/khi mở popup; khác ngày → xoá tiến độ + claimed của nhóm Daily.
- Pool gợi ý (target nhỏ, thưởng 30–80 Coin hoặc 1 Diamond): Thu 10 lúa · Trồng 8 cây bất kỳ · Giao 2 đơn · Nấu 1 món · Cho thú ăn 3 lần · Thu 5 trứng · Mua 1 hạt giống · Thu 2 hoa.
- Data: `MissionDatabase_Daily.asset` riêng, `kind = Daily`, `requiredLevel = 6`.

## 8. Thứ tự làm + kiểm thử (ước lượng)

1. Schema + tracker (file 1, 2) — nửa buổi; chạy lại game cũ xác nhận popup Ewa vẫn mở bình thường (asset cũ default field mới).
2. Hooks (file 3–7) — mỗi hook 1–2 dòng; test từng sự kiện bằng log `[MissionTracker]`.
3. UI lọc theo level + realtime (file 8, 9) — kiểm tra: mở popup ở L1 chỉ thấy mission L1; lên cấp → mission mới xuất hiện; claim xong tắt game mở lại vẫn claimed.
4. Đổ data L1→L10 + daily (mục 6, 7) — chờ duyệt số liệu thưởng trước khi tạo asset.

> Rủi ro thấp: mọi thay đổi là cộng thêm; lỗi lệch key hiện tại khiến UI mission vốn chưa hiển thị tiến độ thật, nên sửa key không làm hỏng hành vi nào mà người chơi đang thấy.
