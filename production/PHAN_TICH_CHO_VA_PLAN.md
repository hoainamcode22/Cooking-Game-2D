# Phân tích 2 video chợ + Plan cải tiến

Tách 83 frame (video 1: 50 frame @1.5fps · video 2: 33 frame @3fps), soi từng nút và từng trạng thái.

---

# PHẦN 1 — PHÂN TÍCH

## 1.1 Hai hệ thống TÁCH BIỆT, không phải một

| | **QUẦY HÀNG** (video 1) | **BẢNG TIN CHỢ** (video 2) |
|---|---|---|
| Vai trò | **Tôi BÁN** | **Tôi MUA của người khác** |
| Object ngoài map | quầy nhỏ có mái hiên, bày hàng lên trên | bảng tin riêng |
| Nội dung | kho của tôi → đặt lên quầy | thẻ hàng của mọi người chơi |
| Tiền tệ đặc biệt | 🍀 bật loa | 🍀 làm mới |

Đúng như bạn hình dung: **hai object, hai popup riêng.**

---

## 1.2 QUẦY HÀNG — mổ xẻ

### Object ngoài map
Quầy nhỏ khung vàng, **mái hiên sọc xanh-trắng**, mặt quầy bày các thẻ hàng nhỏ. Nhìn vào là biết đang bán gì mà **không cần mở popup** — chi tiết quan trọng, nó làm cái quầy "sống".

### Popup — khung chung
- **Mái hiên sọc xanh-trắng** vắt ngang đỉnh panel, title pill "QUẦY HÀNG" + icon đè lên mái
- Nút X đỏ tròn nằm **lồi ra ngoài mép** panel, không nằm trong
- Nền panel cam đất, các ô lõm màu đậm hơn

### Lưới ô quầy — **4 trạng thái**

| Trạng thái | Hiển thị |
|---|---|
| **Trống, dùng được** | dấu `+` to + chữ "Bán vật phẩm" |
| **Đang bán** | icon vật phẩm + số lượng + 🪙 giá |
| **Khoá, mở được** | 🔒 + "Thêm quầy" + nút xanh dương `💎 6` |
| **Chưa tới lượt** | ô cam đậm trơn, không chữ |

Bốn trạng thái này là **cốt lõi của cảm giác tiến trình** — người chơi luôn thấy ô kế tiếp đang chờ mở, và biết chính xác giá bao nhiêu gem.

### Cạnh trái popup
Dải tab dọc 3 icon: **kho nông sản · nhà kho · quầy**. Đây là bộ lọc nguồn vật phẩm.

### Góc dưới trái
Avatar người chơi + **badge cấp 50** + tên "John".

---

### Luồng đặt hàng lên quầy — 4 nhịp

**Nhịp 1.** Bấm ô `+ Bán vật phẩm`

**Nhịp 2.** Panel chọn vật phẩm **trượt đè lên** lưới ô (không phải popup mới) — giữ ngữ cảnh, không mất phương hướng.

**Nhịp 3.** Bố cục 3 cột:
- **Trái**: tab danh mục
- **Giữa**: lưới vật phẩm — icon, tên phía trên, **số lượng ở góc dưới phải**
- **Phải**: khu thiết lập, ban đầu trống

**Nhịp 4.** Chọn vật phẩm → cột phải hiện:

```
        [icon vật phẩm to]
   [−]  10          [+]      ← SỐ LƯỢNG
   [−]  🪙 10       [+]      ← GIÁ BÁN
   [ 🍀 0   TẮT LOA ]         ← nút xanh dương
   [    Đặt lên quầy    ]     ← nút xanh lá
```

**Bấm "Đặt lên quầy"** → hàng vào ô, trừ khỏi kho.

### Bốn chi tiết tinh mà dễ bỏ sót

**1. Nút `−` đổi màu theo trạng thái.** Xanh lá khi bấm được, **xám khi đã chạm giới hạn**. Người chơi biết ngay là hết đường giảm, không phải bấm thử.

**2. Số lượng và giá liên động.** Đổi số lượng thì giá gợi ý đổi theo — game tự tính giúp, người chơi chỉ tinh chỉnh.

**3. Vật phẩm bán hết thì biến mất khỏi lưới.** Frame 16 còn "Đậu Tương · Mía · Bắp", frame 21 chỉ còn "Đậu Tương · Bắp" — Mía đã bán hết nên bị gỡ. Lưới luôn chỉ hiện thứ thật sự chọn được.

**4. "TẮT LOA" là nút GẠT, không phải nút mua.** Chữ "TẮT" nghĩa là loa **đang bật**, bấm để tắt. Loa = quảng cáo mặt hàng lên bảng tin cho người khác thấy trước, trả bằng 🍀.

---

## 1.3 BẢNG TIN CHỢ — mổ xẻ

### Khung chung
Cùng bộ khung với Quầy hàng — **mái hiên sọc xanh-trắng, title pill**. Hai popup nhìn là biết cùng một họ.

### Cạnh trái — dải lọc danh mục
Các icon **treo trên dây thừng**, xếp dọc. Đọc được: nông sản (lúa+cà chua) · trái cây (táo+nho) · sữa & trứng · **đồ chế biến (mứt+bánh mì)** · hải sản · vải-len · khoáng sản.

Icon **đang chọn**: nền vàng sáng, **to hơn hẳn**, viền phát sáng. Tương phản rất mạnh với các icon còn lại — không bao giờ nhầm đang ở mục nào.

### Trên phải — làm mới
```
[ Làm mới sau: 1m 42s ]   [ 🍀10  Làm mới ]
```
Đồng hồ đếm ngược miễn phí, **hoặc trả 10 🍀 để làm mới ngay**. Đây là vòng lặp giữ chân: hàng tốt thì hiếm, muốn săn phải chờ hoặc trả tiền.

### Thẻ hàng — cấu trúc 2 tầng

```
┌─ khung vé, góc khuyết ────┐
│ ┌── panel lõm ─────────┐  │
│ │  [icon]   Tên món    │  │
│ │    4      🪙  64     │  │   ← số lượng dưới icon, giá bên phải
│ └──────────────────────┘  │
│  (avatar)  Tên người bán  │
└───────────────────────────┘
```

**Tên người bán là linh hồn của bảng tin.** John · Ngọc Hằng · Tạ Trân · Hiệp Trần Thị · Hạnh Bùi thị · Mơ Mơ · Thai Tran · Tăng Thu Hậu · Hoài Thương · Suhào Bany · Hằng Nguyễn · guest.1379345808. Có cả tên thật lẫn tên guest tự sinh — nhìn là tin ngay đây là chợ có người thật.

Lưới **4 cột × 3 hàng = 12 thẻ**/trang.

### Hiệu ứng
Thẻ hiện ra **lần lượt so le** (stagger), không bụp ra cùng lúc. Frame 17–22 bắt được rõ: hàng 1 hiện xong mới tới hàng 2, mỗi thẻ mờ→rõ + phóng nhẹ. Rẻ mà làm cả bảng tin sống hẳn.

### Trạng thái rỗng
`CHƯA CÓ VẬT PHẨM NÀO ĐƯỢC ĐĂNG BÁN` — chữ nhạt, căn giữa vùng trống.

---

## 1.4 🍀 — đồng tiền thứ ba

Không phải vàng, không phải kim cương. **Cỏ bốn lá.** Dùng cho:
- Làm mới bảng tin ngay: **10 🍀**
- Bật loa quảng cáo mặt hàng

Tách riêng khỏi gem là có chủ đích: gem để mua công trình/tăng tốc (giá trị cao), cỏ để thao tác chợ (giá trị thấp, dùng thường xuyên). Trộn chung sẽ làm người chơi tiếc gem mà không dám dùng chợ.

---

## 1.5 Chợ hiện tại của bạn — hiện trạng

`MarketManager.cs` + `MarketDatabase.asset`:

| | Có | Thiếu |
|---|---|---|
| Mua | ✅ 10 slot random, làm mới 300s hoặc 1 gem | |
| **Bán** | ❌ | **không có gì** |
| Người bán | ❌ | không có khái niệm |
| Lọc danh mục | ❌ | |
| Đồng tiền chợ | ❌ | đang dùng gem |

**Và đang hỏng ngay trên màn hình:** ảnh bạn gửi cho thấy `TODO_FLOWER_SEED`, `TODO_MEAT_CHICKE`. `MarketDatabase.asset` có **hơn 30 mục placeholder chưa điền id**:

```
TODO_VEGETABLE_ID · TODO_MEAT_CHICKEN_ID · TODO_MEAT_COW_ID · TODO_MEAT_PIG_ID
TODO_EGG_ID · TODO_MSG_ID · TODO_FISH_SAUCE_ID · TODO_SALT_ID
TODO_FLOWER_SEED_ID_01..10 · TODO_DISH_ID_01..20
```

Buồn cười là `TODO_FISH_SAUCE_ID` và `TODO_SALT_ID` bỏ trống trong khi `fishsauce`/`salt` đã được thêm ở chỗ khác **trong cùng file**.

---

# PHẦN 2 — PLAN

## 2.1 Quyết định kiến trúc quan trọng nhất

> **Viết tầng dữ liệu theo hình dạng SERVER ngay từ bây giờ, dù giờ chưa có multiplayer.**

Một mặt hàng rao bán =

```
Listing {
  listingId    string
  sellerId     string      ← "local" khi là mình, id thật khi có server
  sellerName   string
  sellerAvatar int
  itemId       string
  quantity     int
  pricePerUnit int
  createdUtc   long
  expiresUtc   long
  status       enum { Active, Sold, Expired, Cancelled }
  hasLoa       bool
}
```

Giai đoạn 1 (bây giờ): nguồn dữ liệu là **`LocalMarketProvider`** — sinh hàng giả từ NPC có tên Việt.
Giai đoạn 2 (sau này): thay bằng `ServerMarketProvider`, **UI và luồng không phải sửa một dòng nào.**

Đây là điểm mấu chốt. Làm sai chỗ này thì sau này phải viết lại toàn bộ.

```
IMarketProvider
├── GetListings(category, page)
├── PostListing(itemId, qty, price, hasLoa)
├── CancelListing(listingId)
├── BuyListing(listingId)
└── GetMyListings()
```

## 2.2 Chợ giả nhưng phải "có người"

Giai đoạn 1 sinh người bán giả — nhưng **không được cẩu thả**, vì tên người bán chính là thứ làm bảng tin đáng tin:

- **Bộ tên Việt** (~60 tên: Ngọc Hằng, Tạ Trân, Hiệp Trần Thị, Mơ Mơ, Hoài Thương…) + vài tên `guest.13xxxxxxxx` cho giống thật
- Mỗi NPC có avatar + cấp độ riêng, **giữ ổn định giữa các phiên** (Ngọc Hằng hôm nay bán lúa, mai vẫn là Ngọc Hằng)
- Giá dao động ±25% quanh giá gốc → có hàng hời để săn
- Hàng theo cấp người chơi: cấp thấp thì bảng tin toàn nông sản cơ bản

## 2.3 Đồng tiền chợ 🍀

Thêm **Cỏ Bốn Lá** làm đồng tiền thứ ba:
- Làm mới bảng tin ngay: 10 🍀
- Bật loa cho một mặt hàng: 5 🍀
- Nguồn nhận: thu hoạch rơi ngẫu nhiên, nhiệm vụ hằng ngày, lên cấp

Nếu bạn thấy thừa thì dùng gem cũng được — nhưng tôi khuyên tách riêng, vì lý do ở mục 1.4.

## 2.4 Chia giai đoạn

### Giai đoạn 1 — Sửa cái đang hỏng (nửa ngày, không cần art)

Chợ đang hiện `TODO_FLOWER_SEED` ngay trên màn hình người chơi.

- Điền hết 30+ mục `TODO_*` trong `MarketDatabase.asset` bằng id thật
- Bỏ mục nào chưa có vật phẩm tương ứng
- Thêm `soysauce`, `sugar` (đã cần cho món ăn)

### Giai đoạn 2 — QUẦY HÀNG

**Object ngoài map**: quầy có mái hiên, **bày hàng đang bán lên mặt quầy** — mở khoá ở một cấp nhất định.

**Popup**, dựng theo đúng video:
- Khung mái hiên + title pill, X đỏ lồi ra ngoài mép
- Lưới ô 5×2, **4 trạng thái** như mục 1.2
- Ô khoá mở bằng gem, giá tăng dần (6 → 12 → 25 → 50)
- Panel chọn vật phẩm **trượt đè**, 3 cột
- Bộ đếm số lượng + giá, **nút `−` xám khi chạm giới hạn**
- Giá gợi ý tự tính theo số lượng, người chơi tinh chỉnh
- Vật phẩm hết thì gỡ khỏi lưới
- Nút gạt loa
- Hàng có **hạn** — hết hạn thì trả về kho, không mất

### Giai đoạn 3 — BẢNG TIN CHỢ

**Object ngoài map** riêng: bảng tin.

**Popup**:
- Cùng khung mái hiên
- **Dải lọc danh mục dọc treo dây thừng**, icon đang chọn to + sáng vàng
- Đồng hồ đếm ngược + nút làm mới trả 🍀
- Lưới thẻ 4×3, thẻ 2 tầng: hàng ở trên, **người bán ở dưới**
- **Hiện thẻ so le** — chi tiết rẻ nhất mà hiệu quả nhất
- Trạng thái rỗng có chữ
- Bấm thẻ → xác nhận mua → hàng vào kho, thẻ mờ đi rồi biến mất

### Giai đoạn 4 — Nối multiplayer (sau này)

Thay `LocalMarketProvider` bằng `ServerMarketProvider`. UI không đổi.

## 2.5 Việc cần bạn vẽ

| Thứ | Ghi chú |
|---|---|
| Object Quầy hàng | quầy + mái hiên, có chỗ bày 4-6 thẻ hàng nhỏ |
| Object Bảng tin chợ | bảng gỗ đứng |
| Mái hiên sọc | dùng chung cả 2 popup |
| Icon danh mục | 6-8 icon lọc |
| Icon 🍀 Cỏ Bốn Lá | nếu làm đồng tiền riêng |
| Avatar NPC | 8-10 cái, ghép ngẫu nhiên với tên |

Tôi dựng nền có màu trước, bạn gắn art sau như mọi lần.

## 2.6 Thứ tự tôi đề nghị

**Giai đoạn 1 trước tiên** — chợ đang hiện `TODO_FLOWER_SEED` cho người chơi thấy, đó là lỗi nhìn thấy được ngay, sửa nửa ngày.

Rồi **giai đoạn 3 (Bảng tin chợ) trước giai đoạn 2 (Quầy hàng)**. Lý do: bảng tin cho người chơi **thứ để làm ngay** (săn hàng hời), còn quầy hàng chỉ có ý nghĩa khi đã có người mua — mà người mua thật thì phải chờ multiplayer. Làm quầy trước sẽ ra một tính năng bán hàng cho... không ai cả.

---

## 2.7 Cần bạn chốt

1. **Đồng tiền chợ** — thêm 🍀 Cỏ Bốn Lá riêng, hay dùng luôn gem?
2. **Thứ tự** — Bảng tin trước rồi Quầy hàng, hay ngược lại như bạn định?
3. **Hàng bán ở quầy khi chưa có multiplayer** — để NPC tự mua sau một thời gian (có tiền về, quầy có ý nghĩa ngay), hay để đó chờ multiplayer thật?
4. **Số ô quầy** — bắt đầu mấy ô, tối đa mấy ô, giá gem mở từng ô?
