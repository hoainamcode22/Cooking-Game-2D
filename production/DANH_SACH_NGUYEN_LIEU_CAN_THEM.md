# Danh sách nguyên liệu thiếu — cần thêm gì để nấu được đủ 20 món

Truy từng `IngredientData` của cả 20 món về nguồn thật trong farm (cây trồng · chuồng · máy · chợ).

**Kết quả: 8/20 món nấu được. 12 món kẹt vì 5 nguyên liệu không có nguồn.**

> **Điểm mừng: cả 5 nguyên liệu ĐỀU ĐÃ CÓ asset và icon sẵn.**
> Thiếu là thiếu *chỗ sản xuất ra chúng* trong farm, không phải thiếu art nguyên liệu.
> Bạn chỉ cần vẽ thêm cho **2 trong 5** thứ.

---

## Tóm tắt — bạn cần thêm gì

| # | Nguyên liệu | Asset có sẵn | Cần bạn vẽ | Công sức | Mở thêm |
|---|---|---|---|---|---|
| 1 | Thịt gà `chicken` | ✅ `ING_Chicken` | **Không cần gì** | 5 phút | 2 món |
| 2 | Nước tương `soysauce` | ✅ `SEA_SoySauce` | **Không cần gì** | 15 phút | 3 món |
| 3 | Đường `sugar` | ✅ `SEA_Sugar` | **Không cần gì** | 15 phút | 2 món |
| 4 | Rau thơm `herbs` | ✅ `ING_Herbs` | **4 sprite cây** | 1 buổi | 4 món |
| 5 | Cá `ca` | ✅ `ING_Fish` | **Hồ cá + cá** | Vài ngày | 2 món |

---

## 1. Thịt gà — chỉ là LỆCH TÊN, không thiếu gì

Chuồng gà **đã sản xuất thịt gà rồi**. Chỉ là hai bên gọi tên khác nhau:

```
Config_Pen03_Ga.asset  →  productItemId: chicken_meat
ING_Chicken.asset      →  id:            chicken
```

Món ăn đòi `chicken`, kho có `chicken_meat` → không khớp → báo thiếu nguyên liệu dù trong kho đang có.

**Việc cần làm:** đổi một trong hai cho khớp. **Bạn không phải vẽ gì cả.**

**Mở ra:** Gà xào ớt · Gà nướng lu mật mía *(món này còn cần đường nữa)*

---

## 2. Nước tương — thêm vào chợ

Chợ **đã bán muối và nước mắm** rồi (`salt`, `fishsauce`). Nước tương cùng loại gia vị, cùng cách bán, chỉ là chưa được thêm vào danh sách.

**Việc cần làm:** thêm `soysauce` vào `MarketDatabase.asset` theo đúng khuôn của `salt`. **Bạn không phải vẽ gì** — `SEA_SoySauce.asset` đã có icon.

**Mở ra:** Cơm chiên trứng · Bò xào tiêu · Nấm xào thịt bò

> Cơm chiên trứng là một trong 3 món tôi định cho ở cấp 5 — nó đang kẹt chỉ vì thiếu chai nước tương.

---

## 3. Đường — bán ở chợ

**Việc cần làm:** thêm `sugar` vào `MarketDatabase.asset`, cùng khuôn với muối / nước mắm / nước tương. **Bạn không phải vẽ gì** — `SEA_Sugar.asset` đã có icon.

> **Đã bỏ phương án máy ép mía** theo yêu cầu: mọi thứ liên quan nấu ăn gói gọn trong scene bếp, không thêm công trình chế biến nào ở farm. Bốn gia vị (muối · nước mắm · nước tương · đường) đều mua ở chợ — một đường duy nhất, dễ hiểu, không phải xây gì.

**Mở ra:** Nước mía chanh · Gà nướng lu mật mía

---

## 4. Rau thơm — CẦN BẠN VẼ

Không có cây trồng nào cho ra `herbs`. Đây là thứ chặn nhiều món nhất, gồm cả **Phở bò tái** mà tôi định để dành làm phần thưởng cấp 30.

### Bạn cần vẽ 4 sprite

Theo đúng cấu trúc `CropData` mà mọi cây khác đang dùng:

| Sprite | Dùng ở đâu | Gợi ý |
|---|---|---|
| **Gói hạt giống** | icon trong Shop và trong kho | giống các gói hạt khác, nhãn rau thơm |
| **Mầm** (`sproutSprite`) | vừa gieo xuống | 2-3 lá non nhỏ |
| **Đang lớn** (`growingSprite`) | giữa chừng | bụi lá cao hơn |
| **Chín** (`readySprite`) | thu hoạch được | bụi rau xanh um, có thể thêm chấm hoa nhỏ |

*Icon nguyên liệu khi vào bếp thì KHÔNG cần vẽ* — `ING_Herbs.asset` đã có sẵn, tôi dùng luôn cái đó làm `harvestIcon` bay về kho.

### Tôi làm phần còn lại

Tạo `CropData` mới (`cropId: herbs`, `seedItemId: seed_herbs`, `harvestItemId: herbs`), đặt `unlockLevel`, giá hạt, thời gian lớn, exp — rồi để trống 4 ô sprite **có nền màu** để bạn kéo art vào.

**Mở ra:** Phở bò tái · Salad nấm và rau · Salad bắp cải chanh · Thịt heo luộc cuốn rau

---

## 5. Cá — việc lớn nhất

Chưa có gì cả: không có hồ, không có hệ thống câu/nuôi, không có sản phẩm cá. `LevelReward_L16` đã ghi **"Hồ Cá đã mở — bắt đầu câu cá!"** nhưng đó là **lời hứa duy nhất trong toàn bộ bảng phần thưởng L2→L30 chưa được thực hiện**.

### Bạn cần vẽ

| Thứ | Ghi chú |
|---|---|
| **Hồ cá** | công trình đặt xuống map, có mặt nước |
| **Hoạt ảnh nước** | gợn sóng, cá quẫy — làm hồ có sức sống |
| **Trạng thái** | hồ trống ↔ có cá sẵn sàng thu |

*Icon con cá khi vào bếp thì KHÔNG cần vẽ* — `ING_Fish.asset` đã có.

### Cách làm gọn nhất

Làm hồ cá **giống hệt chuồng gia súc** đang có: thả mồi → chờ → thu cá. Dùng lại `PenMiniPanelUI` và `PenMiniPanelConfig` (thêm `Config_Pen05_HoCa`), không viết hệ thống mới. Như vậy chỉ tốn art, không tốn nhiều code.

**Mở ra:** Cá nướng tiêu · Canh chua cá

---

---

## Ba máy chế biến — kiểm lại thì chúng KHÔNG dính gì tới nấu ăn

Rà sản phẩm của cả ba máy xem có món nào dùng tới không:

| Máy | Sản phẩm | Có món nào dùng? |
|---|---|---|
| `may_01` Máy Xay Bột | `bot_gao` | **Không món nào** |
| `may_02` Máy Ép Mía | `nuoc_mia_ep` | **Không món nào** |
| `may_03` Máy Phô Mai | `pho_mai` | **Không món nào** — chỉ dùng cho đơn hàng nhà dân (`OrderItem_PhoMai`) |

Không có sản phẩm nào của ba máy là nguyên liệu của 20 món ăn. Vậy nên bỏ máy ép mía **không làm hỏng món nào cả**.

Mức độ hoàn thiện của ba máy hiện tại cũng rất mỏng: chỉ có `Config_*` , và **chỉ máy xay bột có mục bán trong Shop** (`Máy Xay Bột.asset`). Máy ép mía và máy phô mai chưa có mục shop nào.

### ✅ Quyết định: bỏ cả ba, thay bằng món ăn

| Cấp | Trước — `LevelReward` ghi | Sau — thay bằng |
|---|---|---|
| 11 | "Máy Xay Bột đã mở bán trong Shop" | **Salad bắp cải chanh** (Easy) |
| 13 | "Máy Ép Mía đã mở bán trong Shop" | **Trứng ốp la bò né** (Easy) |
| 15 | "Máy Phô Mai đã mở bán trong Shop" | **Bò xào tiêu** (Normal) |

Nhờ vậy **cấp 5 → 16 không hụt một cấp nào**, cấp nào cũng có món mới.

**Việc kéo theo khi xoá:** gỡ luôn `OrderItem_PhoMai.asset` khỏi pool đơn hàng nhà dân — không thì sẽ có đơn đòi phô mai mà chẳng còn máy nào làm ra được.

---

## Hai lỗi khác phát hiện lúc rà

### Mỗi nguyên liệu đang có 2 asset trùng nhau

Sáu cặp trùng, nằm ở hai thư mục:

```
_Game/Data/Data_cooking/          ING_Chicken · ING_Herbs · SEA_Salt · SEA_SoySauce · SEA_Sugar · SEA_FishSauce
_Game/ScriptableObjects/Ingredients/   ← y hệt, cùng id
```

Món ăn trỏ vào bộ nào thì bộ kia thành mồ côi. Nguy hiểm ở chỗ: sửa nhầm bộ mồ côi thì tưởng đã sửa mà game không đổi gì. **Nên gộp còn một bộ.**

### Chợ còn 30+ mục để trống

`MarketDatabase.asset` đầy placeholder chưa điền:

```
TODO_VEGETABLE_ID · TODO_MEAT_CHICKEN_ID · TODO_MEAT_COW_ID · TODO_MEAT_PIG_ID
TODO_EGG_ID · TODO_MSG_ID · TODO_FISH_SAUCE_ID · TODO_SALT_ID
TODO_FLOWER_SEED_ID_01..10 · TODO_DISH_ID_01..20
```

Chợ được thiết kế để bán những thứ này nhưng chưa ai điền id vào. Buồn cười là `TODO_FISH_SAUCE_ID` và `TODO_SALT_ID` bỏ trống trong khi `fishsauce`/`salt` đã được thêm ở chỗ khác trong cùng file.

---

## Ưu tiên: RAU THƠM trước, HỒ CÁ sau

Nếu tạm hoãn cả rau thơm lẫn cá thì **mất 6 món**, trong đó quãng cấp 8–11 hụt liền 3 cấp và mất luôn Phở bò tái làm món đỉnh cấp 30.

| Làm tới đâu | Số món | Quãng cấp | Hụt cấp nào |
|---|---|---|---|
| Việc 1+2+3 (không vẽ gì) | 14 | 5→27 | hụt 8, 10, 11, 19, 24, 30 |
| **+ Rau thơm** (4 sprite) | **18** | **5→30** | chỉ hụt 19 và 24 |
| + Hồ cá | 20 | 5→30 | không hụt cấp nào |

**Rau thơm đáng làm trước:** rẻ nhất trong hai việc cần vẽ (4 sprite cây) mà gỡ được 4 món, và lấp đúng ba cấp liền 8-10-11.

**Hồ cá để sau cũng được** — nó chỉ chặn 2 món ở cấp 19 và 24, và làm giống chuồng gia súc thì chủ yếu tốn art chứ ít tốn code.

---

## Tóm lại

Ba việc đầu **không cần bạn vẽ gì cả** — chỉ là sửa lệch tên và nối dây dữ liệu đã có sẵn. Làm xong là từ 8 lên 14 món.

Việc thứ tư cần **4 sprite cây rau thơm** — nên làm luôn, vì nó gỡ được 4 món kể cả Phở bò tái.

Việc thứ năm là **hồ cá**, để đợt sau cũng được.
