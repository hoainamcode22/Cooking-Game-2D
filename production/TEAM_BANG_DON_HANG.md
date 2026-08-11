# BOX LÀM VIỆC — Bảng Đơn Hàng (thay hệ nhà dân cũ)

> Kênh trao đổi chung của DEV-A, DEV-B, TESTER.
> Đọc mục của người kia trước khi làm phần giao nhau.

**Dự án:** `E:\Game2\Cooking-Game-2D` · Unity 6000.3.10f1 · không `.asmdef`
**Tham chiếu thiết kế:** `production\PHAN_TICH_BANG_DON_HANG_CU.md` — mô tả từng nút, từng trạng thái, từng nhịp trong video. **Đọc kỹ trước khi code.**

---

## 0. YÊU CẦU CHỦ DỰ ÁN

1. **Một object công trình** — chủ dự án tự vẽ bảng, ta chỉ dựng nền + logic
2. **Con cú giao hàng** — chủ dự án tự làm anim, **KHÔNG cần đụng**
3. **Xoá hoàn toàn hệ cũ**: bong bóng trên nhà, popup cũ, luồng cũ, dữ liệu cũ
4. **Dựng UI + flow + button + animation GIỐNG VIDEO 100%**
5. **Gộp Farm + Cooking**: đơn hàng gồm nông sản, món ăn, nguyên liệu farm được
6. **Nội dung thật nhiều** — người chơi build/farm liên tục, giao xong đơn này có đơn khác
7. Đơn **khó dần theo cấp**

---

## 1. 🔴 HIỆN TRẠNG — HỆ CŨ GẦN NHƯ ĐÃ CHẾT

Đọc kỹ, đây là lý do phải làm lại chứ không phải vá.

**Lỗi chí mạng:** `VillageOrderManager.RegisterHouse` (dòng 308) gán `HouseId = houses.Count`. 12 nhà người chơi nhìn thấy nằm ở **scene root**, tự đăng ký nên nhận id **12–23**. Nhưng `IsHouseActiveForOrders` (dòng 363) yêu cầu `HouseId < maxHouses` mà `GetMaxActiveHousesForLevel` trả tối đa **8** → **12 nhà đó không bao giờ nhận đơn**.

Chỉ 12 placeholder dưới `World/Buildings/Home` (id 0–11) mới đủ điều kiện — mà chúng **đang TẮT hết**. Nên bong bóng không hiện.

**Lỗi khác:**
- `houses[3]` và `houses[9]` **NULL** trong scene
- UI hiện `rewardExp` chưa nhân 2, nhưng khi giao lại cộng `rewardExp * 2` → **người chơi thấy sai số**
- Trừ item2 fail thì item1 **đã bị trừ rồi**, kho lệch, chỉ log lỗi
- 8 PrefabInstance `OrderPopup2` còn override `houseTransform` — **field không còn tồn tại**, Unity ném warning mỗi lần load scene
- `Order_item_salad_nam_rau.asset` điền nhầm `itemId = salad_bap_cai_chanh` → món `salad_nam_rau` **không bao giờ ra đơn**

---

## 2. 🔴 XOÁ LÀ VỠ — PHẢI SỬA KÈM

Xoá 4 class `VillageOrderManager` / `HouseOrderController` / `HouseOrderBubble` / `HouseOrderPopupUI` sẽ làm **không biên dịch được toàn project**. Phải sửa đồng thời:

| File | Dòng | Sửa gì |
|---|---|---|
| `Managers\PopupManager.cs` | 15, 78 | trỏ sang popup mới |
| `Managers\EditModeManager.cs` | 104, 107 | API mới (ẩn/hiện phiếu khi vào Edit Mode) |
| `Managers\PlacementManager.cs` | 1223, 1339, 1448, 1527 | bỏ `HouseOrderController` |
| `Tutorial\AnimalGuideController.cs` | 221, 226, 228, 233, 251 | trỏ sang bảng đơn mới |
| `Editor\DemoL1L10Tool.cs` | 119, 136, 147, 230, 388–415 | bỏ `OrderItemDefinition` |
| `Editor\Phase1TestTool.cs` | 125–128, 180–198 | bỏ tham chiếu |
| `Editor\MissionSetupTool.cs` | 259–262 | **đổi nguồn icon** sang `MarketPriceTable` |
| `Editor\VillageOrdersL1L6SetupTool.cs` | cả file | xoá luôn |

### ⚠️ BA ĐIỀU TUYỆT ĐỐI KHÔNG ĐƯỢC LÀM

**1. KHÔNG xoá 5 prefab `House_01..05.prefab`.** Chúng có **24 instance** trong scene. Xoá file là 24 object thành "Missing Prefab", vỡ cả khu nhà dân. **Chỉ gỡ component** `HouseOrderController`, `HouseOrderBubble`, `OrderAnchor*`, `OrderPopup2` bên trong; giữ nguyên thân nhà, SpriteRenderer, Collider, `EditableBuilding`.

**2. KHÔNG đụng enum `MissionEventType`** (`Scripts\Mission\MissionData.cs:12`). **26 asset mission** đã serialize `eventType: 1`. Đổi thứ tự là mọi mission dịch sai loại.

**3. BẮT BUỘC gọi lại hook nhiệm vụ.** Hệ cũ phát ở `VillageOrderManager.cs:286, 288`:
```csharp
MissionProgressTracker.ReportEvent(MissionEventType.DeliverOrder, item1.itemId, 1);
MissionProgressTracker.ReportEvent(MissionEventType.DeliverOrder, item2.itemId, 1, includeTypeWide: false);
```
Thiếu là **26 nhiệm vụ treo vĩnh viễn**, gồm `main_l2_deliver_1` ở ngay cấp 2 → người chơi kẹt tiến trình.

Nhớ gọi thêm `TutorialManager.Instance?.NotifyDelivery()` — bước tutorial `6_DeliverOrder` hiện đang treo vì không ai gọi.

---

## 3. KINH TẾ — KHÔNG ĐƯỢC LÀM MẤT VÒI VÀNG

Đơn hàng nhà dân là **1 trong 2 nguồn vàng lặp lại duy nhất** (cùng Quầy Hàng). Ước tính **50–70% thu nhập vàng lặp lại**, tính cả 26 mission thưởng Coin thì lên **70–80%**.

Hệ mới phải giữ mức thu nhập tương đương hoặc hơn. Công thức cũ để tham chiếu:
- `rewardGold = Σ(số lượng × goldPerUnit)`
- `rewardExp = Σ(số lượng × expPerUnit)`, **khi giao nhân đôi**
- 1 đơn nông sản ≈ 45–350 vàng / 18–96 EXP · 1 đơn món ăn ≈ 110–340 vàng / 20–40 EXP

---

## 4. DỮ LIỆU — DÙNG `MarketPriceTable`, BỎ 37 ASSET CŨ

`Scripts\Market\MarketPriceTable.cs` là `static class`, tự build ở static ctor, **không cần manager trong scene**. Đã phủ **~85 item**: nông sản, hoa, hạt giống, chăn nuôi, chế biến, gia vị, món ăn, vật liệu.

| Hàm | Dùng để |
|---|---|
| `AllItems` | duyệt toàn bộ item → **tự sinh pool đơn, thay hẳn 37 file `.asset`** |
| `GetBasePrice(itemId)` | giá gốc 1 đơn vị → **base tính thưởng** |
| `GetUnlockLevel(itemId)` | gating theo cấp |
| `GetDisplayName` · `GetCategory` | hiển thị |
| `MarketItemInfo.MarketEnabled` | **lọc tự động 5 item không dùng được** |

**5 item `MarketEnabled = false`**: `bot_gao`, `nuoc_mia_ep`, `pho_mai` (không có icon → ra ô trắng) và `ca_nuong_tieu`, `canh_chua_ca` (chưa có nguyên liệu cá). Lọc theo cờ này là tự tránh.

⚠️ **Bảng thiếu 2 dòng, DEV-A bổ sung:** `salad_nam_rau` (món ăn có thật, `unlockLevel 7`) và `chicken` (id trong `IngredientData` là `chicken` nhưng kho lưu `chicken_meat` — thống nhất một tên).

⚠️ **Không đưa vào đơn:** `salt`, `herbs`, `soysauce`, `fishsauce`, `sugar` — **không có nguồn sản xuất trong farm**, chỉ mua ở chợ. Bắt giao là chặn người chơi.

### 11 nông sản/hoa đủ icon nhưng hệ cũ bỏ phí
`carot` · `khoaitay` · `lemon` · `chili` · `pepper` · `hoa_lan` · `hoa_cuc_trang` · `hoa_cuc_van_tho` · `hoa_mau_don` · `hoa_cam_tu_cau` · `hoa_anh_thao`

---

## 5. NỘI DUNG ĐƠN HÀNG — 300+ ĐƠN, NHƯNG KHÔNG PHẢI 300 ASSET

> **Quyết định:** KHÔNG soạn tay 300 file `.asset`. Dự án đã bị đúng cái bẫy này với 218 asset nhiệm vụ — không ai sửa nổi, một nửa hỏng.
>
> Thay bằng: **bộ mẫu đơn + kho tên + bộ sinh**. Kết quả là **hơn 300 đơn khác nhau thật sự**, mà chỉ vài file dữ liệu.

### 5.1 Bốn bậc độ khó theo cấp

| Bậc | Cấp | Số món/đơn | Số lượng mỗi món | Nguồn món |
|---|---|---|---|---|
| **1 — Tập sự** | 1–5 | 1 | 2–5 | nông sản cơ bản |
| **2 — Quen tay** | 6–12 | 1–2 | 2–8 | + chăn nuôi, hoa, **món ăn dễ** |
| **3 — Lành nghề** | 13–20 | 2–3 | 3–10 | + món ăn Normal, hoa hiếm |
| **4 — Bậc thầy** | 21–30 | 3–4 | 4–12 | + món ăn Hard, mọi thứ |

Xác suất số món trong đơn theo bậc, không phải cứng: bậc 2 = 60% một món / 40% hai món; bậc 3 = 20/50/30; bậc 4 = 10/30/40/20.

### 5.2 Kho tên đơn — soạn 300+ tên

Tên đơn là thứ làm mỗi đơn **cảm giác khác nhau**. Chia theo **chủ đề**, mỗi chủ đề khớp với loại hàng trong đơn:

| Chủ đề | Khớp khi đơn có | Ví dụ tên (soạn 25–40 tên mỗi chủ đề) |
|---|---|---|
| Bữa cơm gia đình | nông sản cơ bản | "Bữa cơm nhà bác Heo" · "Cơm chiều nhà Gấu" · "Mâm cơm ngày mùa" |
| Tiệc mừng | nhiều món, giá trị cao | "Tiệc mừng nhà mới" · "Liên hoan cuối mùa" · "Tiệc thôi nôi bé Cún" |
| Quán ăn | món ăn nấu | "Đơn quán Cô Ba" · "Bếp nhà hàng Bốn Mùa" |
| Bó hoa | hoa | "Bó hoa tặng mẹ" · "Hoa cưới nhà Thỏ" · "Giỏ hoa ngày lễ" |
| Chợ phiên | hỗn hợp số lượng lớn | "Hàng chợ phiên" · "Gánh hàng rong" |
| Trang trại bạn | chăn nuôi | "Trại gà nhà Vịt cần hàng" |
| Đơn gấp | thưởng cao, khó | "Đơn gấp — trả hậu" · "Khách quý đặt riêng" |

**Cách ghép:** bộ sinh chọn chủ đề theo thành phần đơn, rồi bốc ngẫu nhiên một tên chưa dùng gần đây. 7 chủ đề × ~40 tên = **280 tên**, cộng biến thể theo khách hàng ⇒ dư 300.

### 5.3 Công thức thưởng

```
vàng gốc  = Σ( số lượng × MarketPriceTable.GetBasePrice(itemId) )
hệ số bậc = 1.0 · 1.15 · 1.3 · 1.5     (bậc 1→4)
hệ số món ăn = 1.4   (đơn có món nấu — thưởng công nấu)
vàng thưởng = vàng gốc × hệ số bậc × hệ số món ăn × ngẫu nhiên(0.9–1.15)

exp thưởng = vàng thưởng / 8   (làm tròn, tối thiểu 3)
```

Cho đơn giá trị cao hơn tổng giá bán thẳng ở Quầy Hàng — **phải có lãi thì người chơi mới giao đơn thay vì bán**.

### 5.4 Đơn luôn có, không bao giờ trống

- Bảng có **9 ô**. Luôn giữ **đủ 9 đơn**.
- Giao xong một đơn → **đơn mới sinh ngay** (không chờ), lấp vào cuối lưới.
- Bỏ đơn bằng thùng rác → cũng sinh ngay đơn mới.
- **Không có đồng hồ đếm ngược** — video không có, và đơn hàng phải là nguồn thu đều.
- Đảm bảo **luôn có ít nhất 2 đơn người chơi giao được ngay** với kho hiện tại; nếu bộ sinh ra toàn đơn không làm nổi thì ép sinh lại vài đơn dễ. Không có luật này thì bảng dễ kẹt cứng.

---

## 6. PHÂN CÔNG

### DEV-A — Xoá hệ cũ + dữ liệu + logic

Skill: `content-audit` → `balance-check` → `dev-story` → `code-review`

| # | Việc | Xong khi |
|---|---|---|
| A1 | **Xoá sạch hệ cũ** theo mục 2, sửa hết 8 file bị vỡ | project biên dịch sạch |
| A2 | Gỡ component đơn hàng khỏi 5 prefab `House_*` (**KHÔNG xoá file prefab**) | 24 instance còn nguyên |
| A3 | Dọn 8 `OrderAnchor` + 8 `OrderPopup2` + override `houseTransform` rác trong scene | hết warning khi load scene |
| A4 | Bổ sung `salad_nam_rau` + thống nhất `chicken`/`chicken_meat` vào `MarketPriceTable` | không sót món |
| A5 | `OrderData` + `OrderGenerator`: 4 bậc, xác suất số món, lọc `MarketEnabled`, loại 5 gia vị không farm được | sinh đơn đúng bậc |
| A6 | **Kho tên đơn 300+** theo 7 chủ đề mục 5.2 | mỗi đơn một tên hợp cảnh |
| A7 | Công thức thưởng mục 5.3 | có lãi hơn bán thẳng |
| A8 | `OrderBoardManager`: giữ đủ 9 đơn, sinh ngay khi giao/bỏ, bảo đảm ≥2 đơn giao được | bảng không bao giờ kẹt |
| A9 | **Lưu qua phiên** — PlayerPrefs có `saveVersion` | thoát game vào lại đơn còn nguyên |
| A10 | Giao đơn: trừ kho **nguyên tử** (thiếu một món thì không trừ món nào), cộng vàng/EXP, gọi `ReportEvent(DeliverOrder,...)` ×2 dòng + `NotifyDelivery()` | 26 mission chạy lại |

### DEV-B — Object bảng + UI popup

Skill: `quick-design` → `team-ui` → `ux-review` → `code-review`

| # | Việc | Xong khi |
|---|---|---|
| B1 | **Object bảng ngoài map**: 1 SpriteRenderer trên gốc cho chủ dự án gắn art, + collider, + **sorting chép từ công trình có sẵn** (xem `StallHierarchyBuilderTool.ApDungSortingTheoCongTrinhCoSan`) | gắn ảnh vào là thành công trình |
| B2 | **Phiếu ghim trên mặt bảng** phản chiếu trạng thái: xanh = giao được, trắng = chưa đủ | nhìn từ ngoài biết có đơn không |
| B3 | Popup: title pill + icon, X đỏ **lồi ra ngoài mép** | khớp video |
| B4 | **Lưới phiếu 3×3**, 4 trạng thái: trắng ngà · **xanh + dấu tích** · **viền sáng vàng** khi chọn · ô viền nét đứt | khớp video |
| B5 | Phiếu **chỉ hiện phần thưởng** (⭐exp + 🪙vàng), KHÔNG hiện yêu cầu | đúng ý đồ video |
| B6 | Cột phải: avatar khách · ô thưởng · gạch nét đứt · **lưới yêu cầu 3×2** | khớp video |
| B7 | Ô yêu cầu hiện **`có/cần`** (vd `6/2` = có 6 cần 2), dấu tích xanh khi đủ | KHÔNG hiện `2/2` |
| B8 | **Nút thùng rác đỏ** (bỏ đơn) + **nút xanh "GIAO HÀNG"** | khớp video |
| B9 | **Ba hiệu ứng khi giao, chạy CÙNG LÚC**: khói trắng tại chỗ phiếu · sao+vàng bay lên kèm "+N" · lưới dồn lấp chỗ | khớp video |
| B10 | Nối `PopupManager` + `FarmInputLock` (mẫu ở `HouseOrderPopupUI.cs:353-369`) | không kẹt input |

### Giao diện chung
DEV-B cần từ DEV-A: danh sách đơn, trạng thái đủ/thiếu từng món, hàm giao đơn, hàm bỏ đơn.
**Chốt tên hàm ở mục 8 trước khi code.**

---

## 7. QUY TẮC

- **Chỉ dựng nền có màu** — chủ dự án tự gắn art. Chỗ chờ art đặt tên `IMG_Art*` / `SPR_Art*`.
- **Bố cục và flow giống video 100%**, nhưng **trang trí khác đi**: đổi bảng màu, bo góc, hoạ tiết. Tránh đạo ý tưởng.
- **KHÔNG dựng UI bằng `new GameObject()` lúc runtime.** Prefab + Editor tool. Bài học `UnifiedTaskPopupUI` 1433 dòng.
- TextMeshPro, không `Text` legacy.
- Mọi save có `saveVersion` + nhánh migrate.
- Comment tiếng Việt, giải thích **VÌ SAO**.
- **Không để lỗi biên dịch.** Tự kiểm ngoặc, `#if/#endif`, using, ký hiệu gọi chéo.

---

## 8. CHỐT GIAO DIỆN CHUNG

*(hai dev thống nhất TRƯỚC KHI code)*

### 8.1 · DEV-B ĐỀ XUẤT — 4 hàm cần từ DEV-A  ✅ *(DEV-B đã dựng sẵn, DEV-A chỉ việc kế thừa)*

**File hợp đồng:** `Assets\_Game\Farm\Scripts\OrderBoard\OrderBoardContract.cs` — **DEV-B đã tạo**.
DEV-A **KHÔNG tự định nghĩa lại** 3 type dưới đây, chỉ viết
`public class OrderBoardManager : OrderBoardManagerBase` rồi override 4 hàm.

> **VÌ SAO làm kiểu abstract base thay vì "hẹn miệng tên hàm":** hai dev code song song.
> Nếu DEV-B gọi thẳng `OrderBoardManager.Instance.XXX()` mà DEV-A chưa kịp tạo file thì
> **cả project không biên dịch được** và DEV-A cũng đứng luôn. Với base class do DEV-B
> sở hữu: UI biên dịch được ngay hôm nay, `Instance` trả `null` thì popup chỉ hiện lưới
> rỗng chứ không vỡ. Khi DEV-A ráp xong là chạy, không phải sửa một dòng UI nào.

| # | Bên cung cấp | Chữ ký | Ý nghĩa | Chốt |
|---|---|---|---|---|
| 1 | DEV-A | `IReadOnlyList<OrderBoardOrderView> GetOrders()` | 9 đơn đang treo, thứ tự = thứ tự ô trên lưới 3×3 | ✅ |
| 2 | DEV-A | `int GetOwnedAmount(string itemId)` | số lượng **đang có** trong kho → vế trái của `có/cần` (B7) | ✅ |
| 3 | DEV-A | `bool TryDeliverOrder(string orderId, out string failReason)` | trừ kho nguyên tử + cộng thưởng + bắn mission hook | ✅ |
| 4 | DEV-A | `bool DiscardOrder(string orderId)` | bỏ đơn (nút thùng rác) + sinh đơn mới | ✅ |

**Kèm theo (đã có sẵn ở base, DEV-A chỉ gọi):**

| Thành viên | Dùng để |
|---|---|
| `static OrderBoardManagerBase Instance` | base tự gán trong `Awake`/`OnDestroy`, DEV-A không phải viết singleton |
| `event Action OnBoardChanged` | DEV-A gọi `RaiseBoardChanged()` sau khi sinh/giao/bỏ đơn → UI + phiếu ngoài map tự vẽ lại |
| `const int SlotCount = 9` | số ô lưới, dùng chung |
| `const int MaxRequirementSlots = 6` | lưới yêu cầu 3×2 bên cột phải |

**Hai type dữ liệu (DEV-B đã định nghĩa, DEV-A đổ dữ liệu vào):**

```csharp
OrderBoardOrderView   : orderId, title, customerAvatarId, rewardGold, rewardExp,
                        List<OrderBoardRequirementView> requirements, CanDeliver
OrderBoardRequirementView : itemId, displayName, needAmount, ownedAmount, IsEnough
```

> `ownedAmount` do UI tự nạp lại mỗi lần vẽ bằng `GetOwnedAmount(itemId)` — DEV-A **không
> cần** cập nhật field này khi kho đổi. Lý do: kho thay đổi liên tục (thu hoạch, nấu ăn),
> nếu bắt DEV-A đồng bộ thì chỉ cần quên một chỗ là con số `có/cần` nói dối.

### 8.2 · DEV-B cung cấp ngược cho DEV-A

| Tên | Chữ ký | Dùng để |
|---|---|---|
| `OrderBoardPopupUI.AnyOpen` | `static bool` | DEV-A thêm `\|\| OrderBoardPopupUI.AnyOpen` vào `PopupManager.IsAnyPopupOpen()` thay chỗ `HouseOrderPopupUI` cũ *(DEV-B đã thêm sẵn dòng này)* |
| `OrderBoardPopupUI.OpenPopup()` / `.ClosePopup()` | `void` | `AnimalGuideController` trỏ tutorial sang bảng mới |
| `OrderBoardWorldObject.SetOrderMarksVisible(bool)` | `void` | `EditModeManager` ẩn phiếu ghim khi vào Edit Mode |

### 8.3 · DEV-A XÁC NHẬN — ĐỒNG Ý HỢP ĐỒNG CỦA DEV-B ✅

DEV-A **nhận nguyên** `OrderBoardContract.cs`, **không định nghĩa lại** `OrderBoardOrderView` /
`OrderBoardRequirementView` / `OrderBoardManagerBase`. Lớp hiện thực:

```csharp
public class OrderBoardManager : OrderBoardManagerBase   // Farm\Scripts\OrderBoard\OrderBoardManager.cs
{
    public override IReadOnlyList<OrderBoardOrderView> GetOrders();                 // ✅ luôn đúng 9 phần tử
    public override int  GetOwnedAmount(string itemId);                             // ✅ số THẬT, không cắt về need
    public override bool TryDeliverOrder(string orderId, out string failReason);    // ✅ trừ kho nguyên tử
    public override bool DiscardOrder(string orderId);                              // ✅ bỏ xong sinh ngay đơn mới
}
```

**Bốn điều DEV-B cần biết, chốt cứng:**

1. **`GetOrders()` luôn trả đúng 9 phần tử, KHÔNG BAO GIỜ có `null`.** Ô trống chỉ tồn tại trong
   một khoảnh khắc bên trong hàm giao/bỏ; ra tới UI là đã lấp xong. Vẽ ô viền nét đứt chỉ cần
   cho trường hợp `Instance == null` (chưa vào scene) — lúc đó danh sách rỗng.
   *VÌ SAO:* để UI không phải rắc `if (order == null)` khắp 9 ô rồi quên một chỗ và ném NRE.
2. **`rewardExp` là con số CUỐI CÙNG.** Hệ cũ hiện `rewardExp` rồi khi giao lại cộng `rewardExp*2`
   → người chơi thấy sai số. Hệ mới không nhân thêm ở bất cứ đâu.
3. **`customerAvatarId`** nằm trong tập cố định 12 mã: `heo` `cun` `meo` `tho` `gau` `cuu` `bo`
   `vit` `ga` `soc` `nai` `chuot`. Đặt tên sprite theo mã này là map được ngay.
   Chưa có art thì `OrderBoardIconResolver.TintFromId(customerAvatarId)` cho mỗi khách một màu riêng.
4. **`requirements.Count` tối đa 4** (bậc 4 — Bậc thầy), luôn ≤ 6 ô của lưới 3×2. UI không cần lo tràn.

**Sự kiện:** DEV-A gọi `RaiseBoardChanged()` sau **mọi** thay đổi. ~~Thêm `OnOrderDelivered`~~ —
**ĐÃ RÚT theo yêu cầu của DEV-B ở 8.4, lý do của DEV-B đúng.** Sự kiện đó nằm trên lớp con
`OrderBoardManager` nên buộc UI phải viết thẳng tên lớp — phá đúng ranh giới mà
`OrderBoardContract.cs` sinh ra để giữ. Trong code có comment ghi lại quyết định này.

> **Đổi lại, DEV-A cam kết một điều quan trọng cho B9:** khi giao/bỏ, ô bị **xoá khỏi danh
> sách** (`List.RemoveAt`) và đơn mới rơi xuống **CUỐI lưới** — đúng mục 5.4 *"lấp vào cuối
> lưới"*. Nhờ vậy 8 phiếu phía sau tự trượt lên một bậc và hiệu ứng ③ "dồn lưới" của DEV-B
> có cái để chạy. Nếu DEV-A lấp lại đúng ô cũ thì nhìn như phiếu bị "thay ruột" và hiệu ứng
> dồn lưới thành vô nghĩa.

**Hai chỗ DEV-A ĐỘNG VÀO FILE CHUNG — DEV-B đọc kỹ để khỏi đè nhau:**

| File | DEV-A làm gì | DEV-B cần làm gì |
|---|---|---|
| `Managers\PopupManager.cs` | **gỡ** field `houseOrderPopup` + dòng `HouseOrderPopupUI.IsOpen` (class đã xoá). Dòng `\|\| OrderBoardPopupUI.AnyOpen` của DEV-B ở cuối hàm **giữ nguyên, không đụng** | ✅ đã xong |
| `Managers\EditModeManager.cs` | **gỡ** `HideBubbles/RestoreBubbles` + danh sách `_hiddenBubbles`. Thay bằng `SetOrderBoardMarksVisible(bool)`: đóng `OrderBoardPopupUI` đang mở rồi gọi `OrderBoardWorldObject.SetOrderMarksVisible()` | ✅ đã xong. DEV-A vẫn gọi hộ **thêm một lớp** vì `OrderBoardWorldObject` chỉ tự ẩn *phiếu ghim* chứ không đóng *popup* — mà popup mở lúc kéo thả công trình là kẹt click |

### 8.4 · DEV-B TRẢ LỜI 8.3 — CHỐT XONG, KHÔNG CÒN GÌ CHỜ NHAU

| Điểm của DEV-A | DEV-B |
|---|---|
| `GetOrders()` luôn 9 phần tử, không null | ✅ Nhận. UI **vẫn giữ nhánh `null`** — không phải nghi ngờ DEV-A, mà vì lúc `Instance == null` (mở scene chưa có manager, hoặc Prefab Mode) thì danh sách rỗng và 9 ô phải vẽ ra được thứ gì đó thay vì ném NRE. Nhánh đó cũng chính là trạng thái 4 "ô viền nét đứt" của B4 |
| `rewardExp` là số cuối cùng | ✅ UI **không nhân, không chia** ở bất cứ đâu. Số trên phiếu, số trong ô thưởng và số bay lên `+N` đều đọc thẳng cùng một field |
| 12 mã `customerAvatarId` | ✅ Đang tô màu tạm bằng `TintFromId`. Khi có art: gắn sprite vào `IMG_ArtCustomerAvatar` trong `Canvas_OrderBoardPopup ▸ Col_Detail ▸ Detail_Content` |
| `requirements.Count` ≤ 4 | ✅ Lưới 3×2 dư 2 ô, 2 ô thừa tự về trạng thái viền nét đứt |

**Về `OnOrderDelivered(int, OrderBoardOrderView)` — DEV-B XIN KHÔNG DÙNG, DEV-A khỏi làm.**

Lý do: popup **đã tự biết** ô nào vừa giao. Nó ghi lại chỉ số ô + `rewardExp` + `rewardGold`
**TRƯỚC** khi gọi `TryDeliverOrder`, rồi bật cờ `_animating` để bỏ qua `OnBoardChanged` phát ra
giữa chừng. Ba hiệu ứng B9 bung đúng chỗ mà không cần thêm kênh nào.

Thêm sự kiện static đó lại **có hại**: nó nằm trên `OrderBoardManager` (lớp con của DEV-A) nên UI
phải tham chiếu thẳng vào tên lớp đó — đúng thứ mà cả `OrderBoardContract.cs` sinh ra để tránh.
Ngày DEV-A đổi tên lớp hoặc tách hai manager là UI vỡ theo. Giữ ranh giới ở 4 hàm + 1 sự kiện.

**Hai chỗ file chung — DEV-B đã làm xong phần của mình:**

| File | Trạng thái |
|---|---|
| `Managers\PopupManager.cs` | ✅ DEV-B **đã thêm** `\|\| OrderBoardPopupUI.AnyOpen` ở **cuối** `IsAnyPopupOpen()`, kèm comment. Cố ý đặt cuối, cách xa khối `[SerializeField]` mà DEV-A đang dọn → hai người không đè nhau |
| `Managers\EditModeManager.cs` | ✅ DEV-A **không phải gọi gì**. `OrderBoardWorldObject.OnEnable` tự `+= EditModeManager.OnEditModeChanged`, và còn đọc `EditModeManager.IsEditMode` một lần lúc bật để bắt kịp trường hợp object được bật GIỮA lúc đang ở Edit Mode |

---

## 9. NHẬT KÝ

### DEV-A

**Xong A1 → A10. 4 file mới, 11 file sửa, 47 file + 1 thư mục script bị xoá.**

| # | Việc | Trạng thái | Nằm ở đâu |
|---|---|---|---|
| A1 | Xoá 4 class hệ cũ + sửa 8 file bị vỡ | ✅ | xem bảng "đã xoá" và "đã sửa" bên dưới |
| A2 | Gỡ component đơn hàng khỏi 5 prefab `House_*` — **KHÔNG xoá file prefab** | ✅ | mỗi prefab: 18/26 doc YAML bị gỡ, còn đúng 1 GameObject gốc |
| A3 | Dọn 8 `OrderAnchor` + 8 `OrderPopup2` + override `houseTransform` | ✅ | `SCN_Farm.unity`: 109/6317 doc bị gỡ |
| A4 | `salad_nam_rau` + thống nhất `chicken`/`chicken_meat` | ✅ | `MarketPriceTable.Aliases` + `Canonical()` |
| A5 | `OrderData` + `OrderGenerator` — 4 bậc, xác suất số món, lọc `MarketEnabled` | ✅ | `OrderData.cs` · `OrderGenerator.cs` |
| A6 | Kho tên đơn 300+ | ✅ **315 tên** (7 chủ đề × 45) + 12 mã khách | `OrderNameBank.cs` |
| A7 | Công thức thưởng, phải có lãi hơn bán ở Quầy Hàng | ✅ **0/28.000 đơn mô phỏng bị lỗ** | `OrderGenerator.ComputeReward` |
| A8 | Giữ đủ 9 đơn, sinh ngay khi giao/bỏ, ≥2 đơn giao được | ✅ | `OrderBoardManager.RefillAndBalance` |
| A9 | Lưu qua phiên, PlayerPrefs có `saveVersion` | ✅ | `OrderBoardManager.SaveBoard/LoadBoard/Migrate` |
| A10 | Trừ kho nguyên tử + hook nhiệm vụ + tutorial | ✅ | `OrderBoardManager.TryDeliverOrder` |

#### File MỚI (4)

`Assets\_Game\Farm\Scripts\OrderBoard\` — cùng thư mục với DEV-B, **không trùng tên type nào**:
`OrderData.cs` · `OrderNameBank.cs` · `OrderGenerator.cs` · `OrderBoardManager.cs`

#### File SỬA (11)

| File | Sửa gì |
|---|---|
| `Market\MarketPriceTable.cs` | thêm bảng `Aliases` (`chicken`→`chicken_meat`), `Canonical()`, `ItemAliases`. `salad_nam_rau` **vốn đã có sẵn** ở dòng 277 — mục 4 file TEAM ghi thiếu, đã kiểm lại |
| `Managers\PopupManager.cs` | gỡ field `houseOrderPopup` + dòng `HouseOrderPopupUI.IsOpen` |
| `Managers\EditModeManager.cs` | `HideBubbles/RestoreBubbles` → `SetOrderBoardMarksVisible()`; gỡ hẳn danh sách `_hiddenBubbles` |
| `Managers\PlacementManager.cs` | gỡ 3 khối `HouseOrderController.Initialize()`; viết lại `DisablePlaceholderInScene` |
| `Tutorial\AnimalGuideController.cs` | `PollForDeliverableOrder` hỏi thẳng `OrderBoardManager.HasAnyDeliverableOrder()`; tay chỉ vào bảng |
| `Editor\DemoL1L10Tool.cs` | `CheckOrders` kiểm pool sinh từ `MarketPriceTable`; `CheckSceneManagers` đếm `OrderBoardManager`; gỡ khối đồng bộ `availableItems` và bảng `ExpectedRawOrders` |
| `Editor\Phase1TestTool.cs` | `Print Village Orders Status` → `Print Order Board Status` (in cả `có/cần` từng món); mục 5/6 đổi sang bảng giá + `OrderBoardManager` |
| `Editor\MissionSetupTool.cs` | nguồn icon `OrderItemDefinition` → `InventoryItemData`, thêm bước 6 gán icon cho **bí danh** |
| `Editor\ShopLockSetupTool.cs` | bỏ hướng dẫn chạy tool đã xoá |
| `Editor\TutorialStepsL1GeneratorTool.cs` | comment (không đụng logic) |
| `Scripts\Mission\MissionData.cs` | **CHỈ SỬA COMMENT** ở dòng 12. Thứ tự enum `MissionEventType` **giữ nguyên tuyệt đối** — `DeliverOrder` vẫn là giá trị 1, 26 asset mission không bị ảnh hưởng |

#### ĐÃ XOÁ

- `Farm\Scripts\Village\` (cả thư mục — 10 file `.cs`): `VillageOrderManager`, `HouseOrderController`,
  `HouseOrderBubble`, `HouseOrderBubblePool`, `HouseOrderBubbleAnimator`, `HouseOrderPopupUI`,
  `HouseOrderRuntime`, `OrderCategory`, `OrderItemDefinition`, `OrderState`
- `Farm\data\Village_data\` — **37 asset** `OrderItemDefinition`
- `Farm\Editor\VillageOrdersL1L6SetupTool.cs`
- `Farm\Frefab_home\OrderPopup2.prefab` — prefab popup cũ
- Trong `SCN_Farm.unity`: cây `OrderPopup` (popup cũ, 12 con), object `VillageOrderManager`,
  8 cây `OrderAnchor*`, 8 PrefabInstance `OrderPopup2`, 10 component `HouseOrderController`

#### 🔴 KIỂM CHỨNG BA ĐIỀU CẤM

| Điều cấm | Bằng chứng |
|---|---|
| **KHÔNG xoá 5 prefab `House_*`** | 5 file còn nguyên. Đếm lại trong scene: **16 PrefabInstance + 8 GameObject = đúng 24 object nhà**, y hệt trước khi sửa. Đã kiểm 0 tham chiếu treo |
| **KHÔNG đụng enum `MissionEventType`** | chỉ sửa chữ sau dấu `//`. `DeliverOrder` vẫn ở vị trí thứ 2 (giá trị 1) |
| **KHÔNG quên hook nhiệm vụ** | `TryDeliverOrder` bước 4 gọi `ReportEvent(DeliverOrder, …)` — dòng đầu `includeTypeWide` mặc định (đếm SỐ ĐƠN), các dòng sau `false`. Kèm `TutorialManager.Instance?.NotifyDelivery()` |

**Suýt phạm điều cấm số 1 — ghi lại để không ai lặp lại:** bước dọn scene ban đầu có luật
"một doc chết thì cả PrefabInstance chứa nó chết theo". Vì 5 prefab nhà vừa bị gỡ
`HouseOrderController`, bản sao *stripped* của component đó vẫn nằm trong scene → luật lan truyền
**xoá luôn 2 căn nhà**. Phát hiện bằng bước đếm lại 16 → 14, khôi phục từ bản lưu và siết luật:
chỉ lan truyền từ **transform gốc** chết, không lan từ component chết.

#### Quyết định thiết kế (ảnh hưởng DEV-B / TESTER)

1. **`chicken` là BÍ DANH, không phải dòng thứ hai trong bảng giá.** Công thức nấu ăn dùng
   `chicken` (`ING_Chicken.asset`), kho dùng `chicken_meat` (`Item_ChickenMeat.asset`). Sửa bên
   nào cũng hỏng: sửa `chicken` là mọi công thức gà chết, sửa `chicken_meat` là kho đã lưu của
   người chơi chết. Nên quy về một tên **ngay tại cửa tra cứu** (`MarketPriceTable.Canonical`).
   Thêm một dòng `Add("chicken", …)` sẽ khiến bộ sinh ra đơn đòi `chicken` — mà kho không bao
   giờ có khoá đó, đơn vĩnh viễn không giao được.

2. **Sàn lợi nhuận +10% so với Quầy Hàng.** Mục 5.3 cho hệ số bậc 1 = ×1.0, nhưng bán thẳng ở
   quầy được ×1.3 (`SuggestedSellMultiplier`) — chạy đúng công thức trần thì **suốt cấp 1–5 giao
   đơn là LỖ**, người chơi tính ra sẽ bỏ hẳn bảng đơn và ta mất một trong hai vòi vàng lặp lại.
   Đã thêm sàn `vàng gốc × 1.3 × 1.10`. Mô phỏng 4.000 đơn/cấp ở 7 mốc cấp: **0 đơn nào lỗ**.

3. **Món nấu có khoảng số lượng RIÊNG** (bậc 2: 1–2 · bậc 3: 1–3 · bậc 4: 2–4). Khoảng "2–8"
   của mục 5.1 viết cho nông sản. Thử số thật với khoảng chung: 6 × Cơm Chiên Trứng ⇒ **~1060
   vàng cho một đơn ở cấp 6**, trong khi mục 3 ghi đơn món ăn cũ chỉ 110–340. Vừa quá sức người
   chơi vừa thổi bay cân bằng. Với khoảng riêng: 2 đĩa ⇒ ~354 vàng / 44 EXP — nhỉnh hơn mức cũ
   đúng như yêu cầu "tương đương hoặc hơn".

4. **Số lượng neo theo GIÁ TRỊ món** (rẻ ⇒ nhiều, đắt ⇒ ít). Không có luật này thì bậc 4 sẽ đẻ
   ra đơn "12 Phở Bò Tái" — người chơi bấm thùng rác ngay và ô bảng đó coi như phí.

5. **Bậc 1 (cấp 1–5) CHỈ có nông sản.** Người chơi cấp 1–5 chưa chắc đã mua nổi chuồng hay mở
   bếp; đơn đòi trứng lúc chưa có gà là đơn chết ngay từ lúc sinh ra.

6. **Đơn mới rơi xuống CUỐI lưới** (`RemoveAt` chứ không gán `null`) — xem hộp ở mục 8.3.

7. **`DisablePlaceholderInScene` quét theo `Transform`, không theo component đánh dấu.** Ứng viên
   đầu tiên là `EditableBuilding`, nhưng **kiểm thật thì chỉ House_01 và House_02 có component
   đó**; House_03/04/05 không có. Neo vào nó là ba placeholder kia không bao giờ bị tắt và người
   chơi thấy hai căn nhà chồng lên nhau.

#### Rủi ro còn lại

- **EXP bậc 1 thấp hơn hệ cũ mỗi đơn** (9–13 so với 18–96). Nhưng bảng giữ **9 đơn cùng lúc,
  không cooldown**, còn hệ cũ chỉ 4 nhà kèm cooldown 60 giây — **tổng thông lượng cao hơn hẳn**.
  Ước tính dọn hết một bảng ở cấp 1 ⇒ ~81 EXP, trong khi lên cấp 2 chỉ cần 40. TESTER xác nhận
  lại nhịp lên cấp thực tế.
- **EXP bậc 3–4 rất cao** (trung bình 120 và 249 mỗi đơn, cấp 20 cần 284 EXP). Đây là hệ quả
  trực tiếp của công thức `exp = vàng/8` ở mục 5.3 — **đã làm đúng đặc tả**, không tự ý đổi.
  Nếu chủ dự án thấy lên cấp quá nhanh ở cấp 13+: sửa hằng số `ExpPerGold` trong `OrderGenerator`.
- **Chưa mở được Unity để bấm Play.** Đã kiểm bằng công cụ thay thế: cân bằng ngoặc/chuỗi trên
  **262 file `.cs`** (0 lỗi), quét 0 tham chiếu còn sót tới ký hiệu đã xoá, và kiểm **0 tham
  chiếu treo** trong scene + 5 prefab sau khi mổ. Vẫn cần TESTER mở Unity xác nhận Console sạch.
- **`OrderBoardManager` chưa có mặt trong scene.** DEV-A không tự thêm object vào `SCN_Farm` —
  đó là phần B1 của DEV-B (tool `Tools ▸ Farm ▸ Bảng Đơn Hàng ▸ 2 · Dựng TẤT CẢ`). **Nhớ gắn
  component `OrderBoardManager` vào object bảng** rồi gọi `RegisterBoardAnchor` (hoặc kéo tay
  vào ô `boardWorldAnchor`) — thiếu thì bảng rỗng và tay hướng dẫn chỉ vào gốc toạ độ.
- **Save cũ của người chơi không có gì để migrate** (hệ cũ không lưu đơn). Nhánh `Migrate()` đã
  có sẵn từ ngày đầu, hiện trả `null` = dựng bảng mới.
- **`QuestManager.OnOrderDelivered` nay được gọi** (trước giờ chưa ai gọi). Hiện chưa có asset
  `QuestData` nào nên không đổi gì; khi soạn quest thì nhớ để trống `targetItemId` cho điều kiện
  đếm số đơn.

### DEV-B

**Xong B1 → B10. 8 file mới, 1 file sửa. Biên dịch độc lập với DEV-A.**

| # | Việc | Trạng thái | Nằm ở đâu |
|---|---|---|---|
| B1 | Bảng ngoài map: 1 `SpriteRenderer` trên gốc + `BoxCollider2D` + sorting chép từ công trình có sẵn | ✅ | `OrderBoardWorldObject.cs` · tool dựng `OrderBoard_WorldObject` |
| B2 | 5 phiếu ghim phản chiếu trạng thái (xanh = giao được, trắng ngà = chưa đủ) | ✅ | `OrderBoardWorldObject.RefreshMarks()` |
| B3 | Title pill + icon kẹp giấy, nút X đỏ **lồi ra ngoài mép** panel | ✅ | tool · `TitlePill` + `BtnClose` |
| B4 | Lưới phiếu 3×3, 4 trạng thái | ✅ | `OrderTicketUI.cs` · `PF_OrderTicket.prefab` |
| B5 | Phiếu **chỉ hiện phần thưởng** | ✅ | `OrderTicketUI` — có chú thích cấm thêm dòng yêu cầu |
| B6 | Cột phải: avatar · ô thưởng · gạch nét đứt · lưới yêu cầu 3×2 | ✅ | tool · `Col_Detail` |
| B7 | Ô yêu cầu `có/cần` (`6/2`), dấu tích xanh khi đủ | ✅ | `OrderRequireCellUI.cs` |
| B8 | Nút thùng rác đỏ + nút xanh dương "GIAO HÀNG" | ✅ | tool · `Btn_Discard` + `Btn_Deliver` |
| B9 | Ba hiệu ứng **chạy cùng lúc** | ✅ | `OrderDeliverFxUI.cs` (khói + sao/vàng) · `OrderBoardPopupUI.ReflowRoutine` (dồn lưới) |
| B10 | `PopupManager` + `FarmInputLock` | ✅ | `PopupManager.IsAnyPopupOpen()` · `Acquire/ReleasePopupInputBlock` |

**Cách dùng:** mở scene `SCN_Farm` → menu `Tools ▸ Farm ▸ Bảng Đơn Hàng` → nút
**"2 · Dựng TẤT CẢ"**. Tool sinh sprite, popup, prefab và bảng ngoài map. Kéo
`OrderBoard_WorldObject` tới chỗ muốn đặt trên bản đồ.

**Bốn quyết định đáng ghi lại:**

1. **Hợp đồng là abstract class, không phải lời hẹn miệng** (`OrderBoardContract.cs`). Hai dev
   code song song trên project không `.asmdef`; gọi thẳng `OrderBoardManager.Instance` khi file
   đó chưa tồn tại là **cả project không biên dịch được**, DEV-A cũng đứng luôn. Với base class:
   UI chạy được ngay, `Instance == null` thì popup hiện lưới rỗng.
2. **Lưới 3×3 TẮT `GridLayoutGroup` sau lần dựng đầu.** Toạ độ 9 ô vẫn do layout group tính
   (chỉnh `cellSize`/`spacing` trong Inspector là lưới tự đổi), nhưng phải tắt thì hiệu ứng
   "dồn lưới" mới trượt được — layout group bật thì mỗi khung hình nó lại kéo phiếu về ô cũ và
   người chơi không bao giờ thấy hiệu ứng.
3. **B9 chạy trong MỘT vòng lặp duy nhất**, không nối đuôi. Nối đuôi ba đoạn 0.3s thành 0.9s là
   đánh thuế lên thao tác lặp nhiều nhất của cả hệ thống. Tổng hiện tại ~0.72s, cả ba kết thúc
   cùng lúc.
4. **Trang trí cố ý khác video** (mục 7): bảng màu **xanh rêu + kem + hổ phách** (video: cam đất;
   quầy hàng: mận/ngọc lam) · góc **bo tròn + đinh tán** (video: bo tròn trơn) · **viền kép**
   thay viền nét đứt trên biển tên · phiếu **mép xé giấy + góc gập** thay mép răng cưa · đinh
   ghim **hổ phách** thay đinh đỏ. Bố cục và flow giữ nguyên 100% vì đó là công năng.

**Chờ art của chủ dự án** — tìm theo tiền tố `IMG_Art` / `SPR_Art`:

| Chỗ | Object | Đang là |
|---|---|---|
| Thân bảng ngoài map | `OrderBoard_WorldObject` (SpriteRenderer trên gốc) | khối xanh rêu |
| Phiếu ghim ngoài map | `OrderMarks ▸ SPR_ArtOrderMark_0..4` | tờ giấy vẽ thủ tục |
| Tờ phiếu trong popup | `PF_OrderTicket ▸ IMG_ArtTicketPaper` | giấy mép xé vẽ thủ tục |
| Đinh ghim | `PF_OrderTicket ▸ IMG_ArtPin` | đinh vẽ thủ tục |
| Avatar khách (12 con) | `Col_Detail ▸ Detail_Content ▸ IMG_ArtCustomerAvatar` | tròn, tô màu theo mã khách |
| Icon vật phẩm | `PF_OrderRequireCell ▸ IMG_ArtItemIcon` | tự lấy từ `StallItemCatalog`; thiếu thì tô màu theo id |
| Icon biển tên | `TitlePill ▸ IMG_ArtTitleIcon` | kẹp giấy (video là mặt cú) |
| Nền panel · nút · sao · vàng · tích · thùng rác · khói | `IMG_Art*` + `ob_*.png` | sinh thủ tục, dùng được luôn |

**Rủi ro / điểm cần chủ dự án quyết:**

- **Nút thùng rác không có hỏi lại.** Đúng video, nhưng lỡ tay là mất một đơn đang gom dở.
  Nếu muốn an toàn: thêm bước xác nhận cho đơn đã đủ **≥50%** hàng. Chưa làm vì lệch video.
- **Bảng ngoài map chỉ ghim 5 phiếu cho 9 đơn.** Ưu tiên hiện các đơn **giao được trước** để
  không bao giờ có cảnh "3 đơn giao được mà ngoài map toàn giấy trắng". Đổi số phiếu: sửa
  vòng lặp trong `BuildWorldObject`.
- **Số vàng/EXP trên HUD nhảy lên (bước ⑤ trong flow video)** thuộc `FarmEconomyManager` phía
  DEV-A — DEV-B chỉ bắn nhãn `+N` bay lên trong popup.
- **Sorting của bảng chép từ `Stall_WorldObject` / `Market` / `CookingGate`.** Nếu chạy tool trên
  scene không có cả ba thì tool **log warning** và bảng nằm ở Default order 0 (chìm dưới đất).
  Không gán id cứng vì layer id `1669604809` không có trong TagManager.

### TESTER

**Kết luận: BÀN GIAO ĐƯỢC SAU KHI SỬA 1 MỤC CHẶN + 2 MỤC CẦN SỬA.**
Không tìm thấy lỗi chặn biên dịch, không mất dữ liệu, **24 căn nhà còn nguyên vẹn**.
TESTER **không sửa file nào** — mọi phát hiện đều nằm ngoài hai nhóm được phép sửa
(chặn biên dịch / mất dữ liệu). Bản vá cụ thể ghi ngay dưới từng mục.

Cách kiểm: dựng lại bản `HEAD` của git (`3ea718d`) rồi **so từng doc YAML** giữa scene/prefab
cũ và mới — nhờ vậy phân biệt được lỗi do đợt này gây ra với lỗi đã có từ trước.

---

#### 🔴 CHẶN BÀN GIAO — 1 mục

**C1 · Tool `Dựng TẤT CẢ` KHÔNG gắn `OrderBoardManager` vào scene ⇒ bảng chết, 26 nhiệm vụ vẫn treo.**

`OrderBoardHierarchyBuilderTool.BuildWorldObject()` tạo `OrderBoard_WorldObject` và gắn
`OrderBoardWorldObject`, nhưng **không hề gắn `OrderBoardManager`**. Không có nó thì
`OrderBoardManagerBase.Instance == null` ⇒ `GetOrders()` rỗng ⇒ popup hiện 9 ô nét đứt,
không đơn nào sinh ra, `TryDeliverOrder` không bao giờ chạy ⇒ **`ReportEvent(DeliverOrder…)`
không bao giờ bắn ⇒ đúng 26 nhiệm vụ vẫn treo như trước khi làm lại**, gồm `main_l2_deliver_1`.
DEV-A có ghi ở "Rủi ro còn lại" là phải gắn tay, nhưng đây là bước duy nhất giữa "xong" và
"không chạy gì cả" — để cho người dùng nhớ hộ là chắc chắn quên.

`Assets\_Game\Farm\Editor\OrderBoardHierarchyBuilderTool.cs:780`

```csharp
// SAI (thiếu hẳn manager)
OrderBoardWorldObject world = root.AddComponent<OrderBoardWorldObject>();

// SỬA
OrderBoardWorldObject world = root.AddComponent<OrderBoardWorldObject>();

// Manager phải có mặt trong scene, nếu không Instance = null và cả bảng rỗng.
// Gắn thẳng lên object bảng: một object chết là cả hai chết cùng nhau, không có
// cảnh bảng hiện ra mà không có dữ liệu.
if (Object.FindFirstObjectByType<OrderBoardManager>(FindObjectsInactive.Include) == null)
{
    OrderBoardManager mgr = root.AddComponent<OrderBoardManager>();
    mgr.RegisterBoardAnchor(root.transform);   // tay hướng dẫn chỉ đúng vào bảng
}
```

---

#### 🟠 CẦN SỬA — 2 mục

**S1 · Mổ prefab `House_04` để lại 8 tham chiếu treo trong scene + 42 object rác.**

Đây là **hậu quả trực tiếp của A2**, và là chỗ duy nhất trong toàn bộ đợt mổ bị sót.
Khi gỡ cây `OrderPopup2` khỏi `House_04.prefab`, hai doc bị xoá là
`&6831084008357971525` (RectTransform của `OrderPopup2`) và `&1795167608238108149` (Canvas).
Nhưng trong scene, PrefabInstance `House_04 (2)` (`&722524515`) vẫn còn trỏ vào chúng:

| Chỗ | `SCN_Farm.unity` | Nội dung |
|---|---|---|
| 1 | dòng **136137–136141** | `--- !u!224 &492135843 stripped` → `m_CorrespondingSourceObject: {fileID: 6831084008357971525, guid: d191c47…}` — **node nguồn không còn tồn tại** |
| 2 | dòng **262671–262674** | `- target: {fileID: 1795167608238108149, …}` `propertyPath: m_AdditionalShaderChannelsFlag` — override trỏ vào Canvas đã xoá |
| 3 | dòng **262741–262759** | `m_AddedGameObjects:` — **6 mục** đều `targetCorrespondingSourceObject: {fileID: 6831084008357971525}` |

Đối chiếu OLD↔NEW: trước đợt này scene có **101** tham chiếu treo vào 5 prefab nhà (đã có sẵn
từ trước, không phải lỗi của ai trong đợt này), sau đợt này là **109** — **8 cái mới đúng bằng
2 + 1 + 6 ở trên**. Không có tham chiếu treo mới nào ở 4 prefab nhà còn lại.

6 object bị mồ côi là **3 bản sao HUD nháp** (`TopLeft_Anchor` + `TopRight_Anchor`, mỗi cặp
chứa `Avatar_Frame` · `EXP_Background/EXP_Fill` · `Level_Star/Text_Level` · `Settings_Icon` ·
`Diamond_Background` · `Gold_Background`) — tổng **42 GameObject + 156 doc YAML**.

**KHÔNG PHẢI MẤT DỮ LIỆU.** Đã truy ngược: HUD thật của game là
`Canvas_HUD ▸ SafeArea ▸ TOPBAR ▸ LeftTopBar(GoldBox/GemBox) / RightTopBar(JudgeAvatar)`
với tên field khác hẳn (`Txt_GoldValue`, `Txt_GemValue`, `Txt_EXP`). **0 script nào tham chiếu
tới 42 object mồ côi kia** — chúng là rác do một tool dựng HUD chạy 3 lần rồi thả nhầm vào
trong `House_04 (2)`, có từ TRƯỚC đợt này (bản `HEAD` chưa có, tức sinh ra giữa `HEAD` và
lúc DEV-A bắt đầu). Đợt này chỉ biến chúng từ "rác còn nối được" thành "rác treo lơ lửng".

**Hậu quả nếu để nguyên:** Unity log cảnh báo prefab instance mỗi lần load `SCN_Farm`, và lần
lưu scene kế tiếp sẽ tự vứt 156 doc đó — tức là **checkbox "Load scene không còn warning" ở
mục 10 không đạt**.

**Cách sửa (an toàn nhất, không cần đụng YAML):** mở `SCN_Farm` trong Unity → Hierarchy tìm
`House_04 (2)` → xoá 3 cặp `TopLeft_Anchor` / `TopRight_Anchor` nằm trong nó → Save.
Unity tự dọn luôn cả doc stripped và override treo. Nếu mổ bằng script thì phải xoá **đủ cả 3
chỗ trong bảng trên**, xoá thiếu một chỗ là vẫn còn cảnh báo.

**S2 · `BuildPopup()` không thấy canvas đang TẮT ⇒ chạy tool lần 2 sinh popup thứ hai.**

`BuildWorldObject()` đã xử lý đúng chuyện này (có hẳn comment ở dòng 719–726, quét
`FindObjectsByType<Transform>` kèm `FindObjectsInactive.Include`), nhưng `BuildPopup()` thì
không — nó dùng `GameObject.Find`, mà hàm này **bỏ qua object đang tắt**. Người dùng tắt
`Canvas_OrderBoardPopup` cho đỡ vướng Scene view rồi chạy lại tool là có **hai canvas chồng
nhau**; cờ `OrderBoardPopupUI.AnyOpen` là `static` nên hai popup dùng chung một cờ ⇒ đóng cái
này thì `PopupManager.IsAnyPopupOpen()` vẫn tưởng còn mở ⇒ **kẹt input bản đồ**.

`Assets\_Game\Farm\Editor\OrderBoardHierarchyBuilderTool.cs:134`

```csharp
// SAI — GameObject.Find bỏ qua object đang tắt
GameObject old = GameObject.Find(CanvasName);
if (old != null) Undo.DestroyObjectImmediate(old);

// SỬA — quét cả object tắt, y hệt cách BuildWorldObject đang làm ở dòng 721-726
GameObject old = null;
foreach (var t in Object.FindObjectsByType<Transform>(
             FindObjectsInactive.Include, FindObjectsSortMode.None))
{
    if (t != null && t.parent == null && t.name == CanvasName) { old = t.gameObject; break; }
}
if (old != null) Undo.DestroyObjectImmediate(old);
```

---

#### ✅ ĐẠT — 10 mục đã kiểm

**1 · BIÊN DỊCH — sạch.**
- 🔴 **Nghi ngờ của DEV-B về `PopupManager.cs` là KHÔNG CÓ THẬT.** Đã đọc cả file: field
  `houseOrderPopup` đã gỡ (còn comment ở dòng 14–16), **không còn dòng `HouseOrderPopupUI.IsOpen`**,
  dòng `|| OrderBoardPopupUI.AnyOpen` của DEV-B nằm nguyên ở dòng 92. Toàn project **không có
  một dòng `using Village;` nào**. Hai dev không đè lên nhau.
- Quét **351 file `.cs`**: cân bằng `{}` `()` `[]` — 0 lỗi; `#if`/`#endif` khớp hết (3 file báo
  lệch là do BOM `﻿` đứng trước `#if`, không phải lỗi thật).
- **0 trùng tên kiểu ở phạm vi global.** 9 cặp "trùng" mà công cụ báo (`SaveEntry`, `Wiring`,
  `Row`, `Entry`…) đều là **class lồng `private`** bên trong lớp khác nên không đụng nhau —
  gồm cả `OrderBoardManager.SaveEntry` (dòng 510) vs `WarehouseManager.SaveEntry` (dòng 67).
- **0 kiểu trong `Editor/` bị runtime tham chiếu**; `Scripts\OrderBoard\` không có
  `using UnityEditor` nào.
- Đã đối chiếu **từng lời gọi chéo** trong code mới với khai báo thật:
  `EditModeManager.OnEditModeChanged` (dòng 24) · `EditModeManager.IsEditMode` (21) ·
  `FarmEconomyManager.AddGold` (81) · `PlayerProgressManager.Level` (16) / `AddExp` (56) ·
  `QuestManager.OnOrderDelivered` (116) · `TutorialManager.NotifyDelivery` (338) ·
  `StallItemCatalog.Instance` (30) / `GetIcon` (215) · `FarmLevelManager.CurrentLevel` (9) ·
  `MarketPriceTable.Has/Canonical/ItemAliases/AllItems/MarketBuyMultiplier` — **có đủ**.
- `AnimalGuideController.cs:224` viết `OrderBoardManager.Instance as OrderBoardManager` — đọc
  thành viên `static` qua tên lớp con là hợp lệ trong C#, **không lỗi**.

**2 · THAM CHIẾU CHẾT — 0.**
Dùng bộ bóc chú thích tự viết (hiểu `//`, `/* */`, chuỗi thường và chuỗi `@""`) rồi mới grep
10 ký hiệu. Kết quả: **0 lần xuất hiện trong CODE**. 20 lần còn lại đều nằm sau `//` — là
chú thích cố ý ghi lại lịch sử, giữ được.

**3 · 🔴 24 CĂN NHÀ — CÒN ĐỦ, KHÔNG MẤT CĂN NÀO.**
So trực tiếp với bản `HEAD`:

| | PrefabInstance | GameObject | Tổng |
|---|---|---|---|
| Trước (HEAD) | 16 | 8 | **24** |
| Sau | 16 | 8 | **24** |

Tên 8 GameObject **giống hệt** trước/sau (`House_02 03 05 06 08 09 11 12`). 16 PrefabInstance
phân bổ: House_01 ×3 · House_02 ×3 · House_03 ×4 · House_04 ×4 · House_05 ×2.
**435 tham chiếu `m_SourcePrefab` trong scene — 0 Missing Prefab, 0 `{fileID: 0}`.**
Toàn scene **0 tham chiếu `{fileID: N}` nội bộ bị treo** (cả bản cũ lẫn bản mới) — cây
hierarchy nguyên vẹn, phép mổ 109 doc không cắt trúng thứ gì đang được trỏ tới.
Luật siết "chỉ lan truyền từ transform gốc chết" mà DEV-A ghi trong nhật ký **đã có tác dụng**.

**4 · 5 PREFAB NHÀ — còn đủ, thân nhà nguyên vẹn.**
Cả 5 file còn nguyên. Mỗi prefab sau khi gỡ: GameObject gốc + Transform + **SpriteRenderer** +
**BoxCollider2D** (House_02/04 có 2 cái) + CanvasRenderer + **`EditableBuilding`** + 1
PrefabInstance con `Light_Windows`. **0 `HouseOrderController` / `HouseOrderBubble` /
`OrderAnchor` / `OrderPopup2` còn sót. 0 `m_Script` trỏ GUID không tồn tại. 0 `m_Script: {fileID: 0}`.**

**5 · SCENE SẠCH.**
`OrderAnchor` 8→**0** · `OrderPopup2` 8→**0** · `houseTransform` 8→**0** · `VillageOrderManager` 2→**0**.
GUID `m_Script` mồ côi: **0 cái mới sinh ra**. Trong 28 GUID không nằm trong `Assets`, **19 cái
là uGUI/TMP trong `PackageCache`** (`TextMeshProUGUI`, `Image`, `Button`, `ScrollRect`,
`GridLayoutGroup`…), **1 cái là `Volume` của URP** — đúng như dặn, không báo nhầm. 8 cái còn
lại là **mồ côi CÓ SẴN TỪ TRƯỚC** trên các object chuồng trại (`CowSlot_02`, `PigSlot_01`,
`ChickenPenPopup`, `FeedItem_01`…), có mặt y nguyên trong bản `HEAD`, **không liên quan đợt này**.

**6 · 🔴 HOOK NHIỆM VỤ — CÓ, VÀ ĐÚNG NGỮ NGHĨA.**
`OrderBoardManager.cs:266-272`:
```csharp
if (order.lines.Count > 0 && order.lines[0] != null)
    MissionProgressTracker.ReportEvent(MissionEventType.DeliverOrder, order.lines[0].itemId, 1);

for (int i = 1; i < order.lines.Count; i++)
    if (order.lines[i] != null)
        MissionProgressTracker.ReportEvent(MissionEventType.DeliverOrder, order.lines[i].itemId, 1,
                                           includeTypeWide: false);
```
Dòng đầu để mặc định (`includeTypeWide = true` — xác nhận ở `MissionProgressTracker.cs:49`),
các dòng sau `false`. Hệ cũ đúng 2 dòng vì đơn cũ tối đa 2 món; đơn mới tối đa 4 món nên vòng
lặp là cách viết đúng — **vẫn chỉ 1 lần cộng khoá `DeliverOrder:*`**, không thổi số đơn.
`TutorialManager.Instance?.NotifyDelivery()` có ở dòng **283**.
`QuestManager.Instance?.OnOrderDelivered()` ở dòng **279**, gọi đúng 1 lần/đơn.
**Enum `MissionEventType` KHÔNG bị đụng:** `HarvestItem` = 0, **`DeliverOrder` = 1**, thứ tự 9
thành viên giữ nguyên — 26 asset mission `eventType: 1` an toàn.

**7 · TRỪ KHO NGUYÊN TỬ — ĐÚNG, có cả đường lui.**
`OrderBoardManager.cs:196-244`. Bước 1 duyệt **toàn bộ** `order.lines` bằng `GetAmount` và
`return false` trước khi chạm vào kho — thiếu một món thì **không món nào bị trừ**. Bước 2 trừ
thật, ghi lại từng dòng đã trừ vào `removed`; `RemoveItem` trả `false` giữa chừng thì `break`
rồi **`AddItem` hoàn lại đủ** những gì đã lấy, trả `failReason` cho UI. Đường thất bại giữa
chừng đã được bọc kín. Khoá kho thống nhất: mọi `OrderLine.itemId` đều đi qua
`MarketPriceTable.Canonical` ở cả 3 cửa vào (`Generate` dòng 305, `GenerateDeliverable` 374,
`FromSave` 682), và `FarmInventoryManager.NormalizeKey` cũng hạ chữ thường ⇒ không lệch khoá.

**8 · HỢP ĐỒNG A↔B — khớp 4/4.**
`public class OrderBoardManager : OrderBoardManagerBase` (dòng 24). Bốn `override` đúng chữ ký
tuyệt đối: `GetOrders()` (156) · `GetOwnedAmount(string)` (165) · `TryDeliverOrder(string, out string)`
(176) · `DiscardOrder(string)` (306). DEV-A **không định nghĩa lại** 3 type của DEV-B.
UI gọi đúng tên ở `OrderBoardPopupUI.cs:437/466/588`.
**Không có double-subscribe:** cả `OrderBoardPopupUI.Subscribe()` (157) lẫn
`OrderBoardWorldObject.Subscribe()` (118) đều chốt bằng `if (board == null || _subscribedTo == board) return;`
rồi `Unsubscribe()` trước khi `+=`, nên dù `OnEnable`+`Start`+`OpenPopup`/`Update` gọi nhiều
lần vẫn chỉ đăng ký một lần. `RaiseBoardChanged()` được gọi sau **mọi** thay đổi (dòng 120,
297, 319).

**9 · LƯU — có `saveVersion`, không đụng key nào.**
Key: **`"OrderBoard_Save"`** (`OrderBoardManager.cs:37`), `CurrentSaveVersion = 1`.
`SaveRoot.saveVersion` là **trường đầu tiên** (526–529), có nhánh `Migrate()` (640) xử lý cả
trường hợp save **mới hơn** bản đang chạy. Đã liệt kê toàn bộ key PlayerPrefs của project
(`FARM_INVENTORY_SAVE`, `FARM_WAREHOUSE`, `FARM_PLACED_BUILDINGS`, `FARM_PLAYER_STALL`,
`FARM_CONSTRUCTION_SITES`, `MISSION_PROGRESS_V1`, `QUEST_SAVE_V1`, `WAREHOUSE_LEVEL`,
`KITCHEN_TRANSFER_SAVE`, `STARTER_ITEMS_GIVEN`, `TUTORIAL_PREPLANT_DONE`) — **không trùng cái nào**.
Chốt `if (!_ready) return;` ở đầu `SaveBoard()` (541) chặn đúng cái bẫy ghi đè save bằng bảng rỗng.

**10 · EDITOR TOOL.**
`BuildWorldObject()` **idempotent** (quét cả object tắt, dòng 721–726) và **giữ nguyên vị trí
người dùng đã kéo** (`viTriCu`, dòng 730/735) — đúng yêu cầu. Tool **chỉ xoá đúng 2 object nó
tự tạo** (`Canvas_OrderBoardPopup`, `OrderBoard_WorldObject`), **không đụng gì khác trong scene**.
Hai lỗi còn lại đã ghi ở **C1** và **S2**.

---

#### 💡 GÓP Ý — không chặn, ghi lại để khỏi quên

1. **`houseOrderPopup: {fileID: 0}` còn sót ở `SCN_Farm.unity` dòng 408215.** Field đã bị gỡ
   khỏi `PopupManager.cs` nên Unity lặng lẽ bỏ qua và tự dọn ở lần lưu scene kế tiếp. Không
   phải lỗi, chỉ là dòng rác duy nhất còn lại của hệ cũ.
2. **Comment ở `PlacementManager.cs:1517-1519` ghi sai sự thật.** Nó viết *"kiểm thật thì chỉ
   House_01 và House_02 có `EditableBuilding`; House_03/04/05 KHÔNG có"* — nhưng **cả 5 prefab
   đều có `EditableBuilding`**, cả trước lẫn sau đợt mổ (đã kiểm GUID
   `8e2b7de78d39b8e47b1c9d6ddc2360f7` trong bản `HEAD` của cả 5 file: đủ 5/5). Code hiện tại
   quét theo `Transform` nên **vẫn chạy đúng**, chỉ có lý do ghi trong comment là sai — cần sửa
   chữ kẻo lần sau có người dựa vào đó mà quyết định.
3. **Ẩn phiếu lúc Edit Mode bị làm hai lần.** `OrderBoardWorldObject.OnEnable` tự
   `+= EditModeManager.OnEditModeChanged`, mà `EditModeManager.SetOrderBoardMarksVisible()` lại
   gọi thêm `b.SetOrderMarksVisible()` một lượt nữa. Vô hại (`SetActive` idempotent, và lớp
   ngoài còn lo việc đóng popup mà lớp trong không làm) — đúng như 8.3/8.4 đã chốt. Chỉ ghi lại
   để sau này ai đọc khỏi tưởng thừa rồi gỡ nhầm cái đang gánh việc đóng popup.
4. **Tên key save lệch quy ước.** Cả project dùng `UPPER_SNAKE` (`FARM_*`, `MISSION_*`), riêng
   bảng đơn dùng `OrderBoard_Save`. Không va chạm gì, nhưng đổi thành `ORDERBOARD_SAVE` ngay bây
   giờ thì rẻ; đợi có người chơi rồi mới đổi là phải viết thêm nhánh migrate.
5. **`BuildWorldObject` giữ `position` nhưng không giữ `rotation`/`localScale`.** Người dùng
   xoay hoặc phóng to bảng rồi chạy lại tool là mất. Thêm 2 dòng lưu/khôi phục ở dòng 730/735
   là xong.
6. **11 file mới chưa có `.meta`** (`Scripts\OrderBoard\*.cs`, 2 file `Editor\OrderBoard*.cs`, và
   cả thư mục `OrderBoard`). Bình thường — Unity sinh ra lúc import lần đầu. Chỉ lưu ý: **mở
   Unity cho nó import xong rồi hãy commit**, đừng commit trước rồi để máy khác sinh GUID khác.
7. **Chưa chạy được Play.** TESTER kiểm bằng đối chiếu YAML + đọc code, **chưa mở Unity**. Bốn
   mục ở mục 10 dưới đây bắt buộc phải xác nhận bằng mắt sau khi sửa xong C1.

---

## 10. BÀN GIAO — TESTER KIỂM

**Trạng thái: 8/13 ĐẠT · 1 KHÔNG ĐẠT · 4 CHỜ CHẠY PLAY.**
Ký hiệu: `[x]` đạt · `[!]` không đạt, có bản vá ở mục 9 ▸ TESTER · `[ ]` chờ mở Unity.

- [x] Biên dịch sạch, 0 lỗi — *kiểm tĩnh 351 file: ngoặc, `#if/#endif`, trùng tên kiểu, lời gọi chéo. `PopupManager.cs` KHÔNG vỡ (nghi ngờ của DEV-B không có thật)*
- [x] Không còn tham chiếu `VillageOrderManager` / `HouseOrderBubble` / `HouseOrderPopupUI` / `OrderItemDefinition` — *0 lần trong code, 20 lần còn lại đều trong chú thích*
- [x] 24 instance nhà trong scene KHÔNG bị Missing Prefab — *16 PrefabInstance + 8 GameObject, khớp từng tên với bản `HEAD`; 435 `m_SourcePrefab` đều giải được*
- [!] Load scene không còn warning `houseTransform` — *`houseTransform` đã sạch (8→0), NHƯNG phép mổ `House_04.prefab` để lại **8 tham chiếu treo + 42 object mồ côi** trong `House_04 (2)` ⇒ vẫn còn cảnh báo. Xem **S1***
- [x] `MissionProgressTracker.ReportEvent(DeliverOrder, ...)` được gọi khi giao — 26 mission chạy — *`OrderBoardManager.cs:266-272`, `includeTypeWide` đúng ngữ nghĩa; enum `DeliverOrder` vẫn = 1*
- [x] `TutorialManager.NotifyDelivery()` được gọi — *`OrderBoardManager.cs:283`*
- [ ] Bảng luôn đủ 9 đơn, giao xong có đơn mới ngay — *code đúng (`RefillAndBalance`), nhưng **C1** làm manager không có mặt trong scene ⇒ phải sửa C1 rồi mới kiểm được*
- [ ] Luôn có ≥2 đơn giao được với kho hiện tại — *chặn bởi C1*
- [x] Trừ kho nguyên tử — thiếu một món thì không trừ món nào — *`OrderBoardManager.cs:196-244`, có cả đường hoàn lại khi hỏng giữa chừng*
- [ ] Thoát game vào lại đơn còn nguyên — *chặn bởi C1 (key `OrderBoard_Save` + `saveVersion` đã đúng, chỉ chờ chạy thật)*
- [x] Ô yêu cầu hiện `có/cần` chứ không phải `đủ/cần` — *`OrderRequireCellUI.cs:82` in `ownedAmount + "/" + needAmount`, và `GetOwnedAmount` trả số THẬT không cắt về `need`*
- [ ] Ba hiệu ứng khi giao chạy cùng lúc — *chặn bởi C1*
- [ ] Gắn ảnh vào object bảng là thành công trình, sorting đúng — *phải chạy tool mới kiểm được; nhớ tool sẽ log warning nếu scene thiếu cả `Stall_WorldObject`/`Market`/`CookingGate`*

**Việc phải làm trước khi bàn giao:** sửa **C1** (2 dòng, tool) → sửa **S2** (6 dòng, tool) →
dọn **S1** trong Unity (xoá 3 cặp anchor trong `House_04 (2)`) → chạy
`Tools ▸ Farm ▸ Bảng Đơn Hàng ▸ 2 · Dựng TẤT CẢ` → bấm Play và tick 4 ô còn lại.
