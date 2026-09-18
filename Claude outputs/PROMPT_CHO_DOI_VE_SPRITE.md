# BRIEF BÀN GIAO CHO ĐỘI VẼ SPRITE

> **Cách dùng:** Sếp copy toàn bộ phần dưới dấu gạch ngang và dán thẳng vào GPT để nó điều khiển `agent-sprite-forge`. Phần code đã xong, giờ tới phần art. Mọi thông số dưới đây lấy trực tiếp từ codebase thật, không phải ước lượng.

---

# BRIEF VẼ ASSET - GAME NÔNG TRẠI 2D ISOMETRIC

## 1. Bối cảnh phong cách dự án

Đây là một game nông trại 2D làm bằng **Unity, isometric kim cương góc 45 độ**.

**Phong cách:** cozy, ấm áp, dễ thương. Tham chiếu gần nhất là **Hay Day** và **Township**. Màu tươi nhưng không chói, viền mềm, ánh sáng ấm, bóng đổ nhẹ.

**Đối tượng người chơi:** phụ nữ và trẻ em. Vì vậy:

- Tuyệt đối **không bạo lực**, không máu, không vũ khí, không hình ảnh đáng sợ.
- Mọi sinh vật vẽ theo hướng **tròn trịa, mắt to, biểu cảm vui**.
- Tránh chi tiết rối. Nhìn ở mức zoom xa vẫn phải đọc ra được vật đó là gì.

**Bốn trụ cột thiết kế của dự án - art phải phục vụ cả bốn:**

1. **JUICY** - mọi hành động đều có animation, âm thanh và hạt hiệu ứng. Art cần chừa chỗ cho hiệu ứng: đừng vẽ kín khung, chừa khoảng thở quanh vật để particle và ánh sáng có chỗ nở ra.
2. **Luôn có thứ để xây** - mọi vật thể đều có trạng thái đang xây, đã xong, được thưởng. Xem yêu cầu 5 stage ở mục 3.3.
3. **Nút to, thân thiện** - silhouette phải rõ, đường nét dày, không có chi tiết mảnh dưới 3px.
4. **Vòng phản hồi ngắn** - người chơi bấm là thấy kết quả ngay. Art phải khác biệt rõ giữa các trạng thái để mắt bắt được thay đổi trong nửa giây.

---

## 2. Ràng buộc kỹ thuật BẮT BUỘC

Những con số này lấy từ code đang chạy. Sai một trong số này là asset không dùng được, phải vẽ lại.

### 2.1. Pivot

**Pivot phải là đáy giữa (bottom-center), tức `SpriteAlignment = 7`.**

Lý do: game sắp xếp thứ tự vẽ theo toạ độ đáy của vật. Pivot ở giữa thì vật sẽ chìm nửa dưới đất hoặc bay lơ lửng, và thứ tự che khuất sẽ sai.

### 2.2. Ô đặt vật

- Một ô đặt trong thế giới game là **300 x 150 đơn vị world** (đúng hình thoi isometric 2:1).
- **Chiều rộng art của decor nên bằng 93% một ô: 280 trên 300.**
- **Chiều cao tối đa: 300.** Cao hơn sẽ đè lên vật ở ô phía sau.

### 2.3. Định dạng file

- **PNG, nền trong suốt hoàn toàn.** Không nền trắng, không nền checker, không viền trắng còn sót (chú ý khử viền halo khi xuất).
- Không đặt nhiều vật trong một file trừ khi brief nói rõ là sprite sheet.

### 2.4. PPU - quan trọng, đọc kỹ

Dự án hiện đang có **sáu giá trị PPU khác nhau** cùng tồn tại: **64, 90, 100, 128, 132, 256**. Đây là nợ kỹ thuật, và là một trong những nguyên nhân làm ảnh bị mờ (sprite bị vẽ ở tỉ lệ không đúng kích thước gốc).

Vì vậy:

- **Mỗi asset giao về BẮT BUỘC ghi rõ PPU dự kiến của nó**, ghi trong tên file hoặc trong file kèm theo.
- **Từ nay chuẩn hoá về một giá trị.** Khuyến nghị:
  - **Decor, nhân vật, UI, biển báo: PPU = 100** (đây đang là đa số).
  - **Tile và prop isometric thuộc nhóm `maptitle/Map45Iso`: PPU = 128** (đa số ở đó).
- Không tự ý đổi PPU của asset đã có. Chỉ áp cho asset mới.

### 2.5. Cấu trúc thư mục giao hàng

**File nằm sâu quá 7 cấp thư mục không chuyển được.** Giữ thư mục giao hàng nông. Ví dụ hợp lệ:

```
Assets/Art/Decor/Stages/banghieu/stage_1.png
```

(7 cấp, đã là mức tối đa. Đừng thêm cấp nào nữa.)

---

## 3. Danh sách asset cần vẽ ngay

### 3.1. Biển mua đất (land purchase sign)

Dùng cho các lô đất mới ở phía Bắc.

**Mô tả hình:** cột biển gỗ, nâu ấm, kiểu biển chỉ đường trong làng quê. Mặt biển là một tấm ván phẳng. Chân cột cắm đất, có thể có cỏ nhỏ ở chân.

**Yêu cầu riêng:**

- **Phải chừa sẵn một vùng bảng trống, phẳng, không hoa văn**, vì game sẽ vẽ chữ giá tiền đè lên đó lúc chạy. Vùng trống này chiếm khoảng 60% chiều cao mặt biển, nằm ở giữa.
- **Phải đọc được khi zoom xa.** Silhouette rõ, không chi tiết vụn.
- Cần **hai trạng thái**:
  - **Khoá** - gỗ xám lạnh hơn, độ bão hoà thấp, có thể thêm ổ khoá nhỏ.
  - **Mua được / nổi bật** - gỗ nâu ấm no màu, có viền sáng nhẹ hoặc ánh vàng quanh mép để mắt bắt ngay.

**Tham chiếu bắt buộc:** biển hiện có nằm ở
`Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Art/Environment/Signs/Sign Right/`.
**Art mới phải khớp silhouette của nó** để người chơi đọc ra là cùng một họ đồ vật, không phải một thứ lạ.

**Kích thước mục tiêu:** **280 x 300 px**, PPU 100. Pivot đáy giữa.
Giao 2 file: `sign_land_locked.png`, `sign_land_available.png`.

---

### 3.2. Hàng rào viền lô đất Bắc

Để các lô đất mới phía Bắc có viền nhìn thấy được, khớp với `Tilemap_IsoFence` đang có.

**Mô tả hình:** hàng rào gỗ thấp, kiểu nông trại, nâu ấm, cùng tông với hàng rào hiện có trong game.

**Yêu cầu riêng:**

- **Isometric 45 độ**, khớp lưới 300 x 150.
- **Tileable (nối liền không thấy mối) theo 4 hướng**: Bắc, Nam, Đông, Tây.
- **Cộng thêm 4 mảnh góc**: Đông Bắc, Tây Bắc, Đông Nam, Tây Nam.
- Tổng **8 file**. Mối nối phải khít tuyệt đối: mép trái của mảnh này phải trùng pixel với mép phải của mảnh kia.

**Kích thước mục tiêu:** mỗi mảnh **300 x 200 px** (rộng đúng một ô, cao thêm để chứa phần cọc nhô lên), PPU 128 để khớp nhóm tile isometric. Pivot đáy giữa.

Tên file:
`fence_north_n.png`, `fence_north_s.png`, `fence_north_e.png`, `fence_north_w.png`,
`fence_north_ne.png`, `fence_north_nw.png`, `fence_north_se.png`, `fence_north_sw.png`

---

### 3.3. Bốn món decor còn thiếu

Bốn món này đã được đặc tả trong dự án nhưng **chưa ai vẽ**. Code đã sẵn sàng, chỉ thiếu ảnh.

| Slug | Tên | Mô tả hình |
|---|---|---|
| `banghieu` | **Kệ chậu cây 3 tầng** | **CHÚ Ý: Sếp đã quyết đây là KỆ GỖ 3 TẦNG ĐỰNG CHẬU CÂY, KHÔNG PHẢI biển hiệu.** Kệ gỗ nâu ấm, ba tầng, mỗi tầng có 2-3 chậu đất nung với cây xanh và hoa nhỏ. Dáng chắc, hơi thô mộc, ấm cúng. |
| `ghehoa` | **Ghế hoa** | Ghế băng gỗ nhỏ, có giàn hoa leo hoặc chậu hoa hai bên. Màu gỗ nhạt, hoa hồng/tím/vàng. Cảm giác ngồi nghỉ thư giãn. |
| `heothantai` | **Heo thần tài** | Tượng heo đất tròn trịa, màu hồng phấn hoặc vàng kim, đội mũ hoặc đeo dây đồng tiền, mặt cười hiền. Không kitsch quá, giữ tinh thần cozy. |
| `vitvuive` | **Vịt vui vẻ** | Tượng/mô hình vịt vàng tròn, mắt to, mỏ cam, đang cười. Có thể đứng trên một bệ gỗ nhỏ. |

#### Bộ 5 stage - bắt buộc cho mỗi món

Mỗi món phải giao **5 file trên CÙNG MỘT canvas và CÙNG MỘT baseline**, để game đổi sprite mà vật không nhảy vị trí.

**Thứ tự stage của dự án này KHÔNG trực giác. Đọc kỹ:**

| File | Nội dung |
|---|---|
| `stage_1.png` | Bắt đầu xây - đống vật liệu, nền móng, mới có chút gì đó |
| `stage_2.png` | Đang xây dở - đã ra hình nhưng chưa xong, có giàn giáo hoặc thiếu phần trên |
| **`stage_3.png`** | **THÀNH PHẨM HOÀN CHỈNH** - đây là hình vật ở trạng thái bình thường, người chơi nhìn thấy lâu nhất |
| **`stage_4.png`** | **HỘP QUÀ** - một hộp quà có nơ, KHÔNG phải cái vật kia. Đây là trạng thái chờ người chơi bấm nhận thưởng |
| **`stage_5.png`** | **PHÁO HOA ĂN MỪNG** - hiệu ứng pháo hoa/kim tuyến bung ra. Chỉ hiện khoảng 0.35 giây |

Nhắc lại vì hay bị vẽ sai: **stage_3 là thành phẩm, không phải stage_5.** stage_4 và stage_5 là quà và pháo hoa, không phải hai giai đoạn hoàn thiện hơn của vật.

**Kích thước mục tiêu:** canvas **512 x 512 px** cho cả 5 stage (đây là cell size của `DecorStageSet`), vật chính chiếm khoảng **280 px chiều rộng**, cao không quá **300 px**, đặt sát đáy canvas. PPU 100. Pivot đáy giữa.

**Đường giao hàng:**

```
Assets/Art/Decor/Stages/banghieu/stage_1.png ... stage_5.png
Assets/Art/Decor/Stages/ghehoa/stage_1.png ... stage_5.png
Assets/Art/Decor/Stages/heothantai/stage_1.png ... stage_5.png
Assets/Art/Decor/Stages/vitvuive/stage_1.png ... stage_5.png
```

---

### 3.4. Hàng rào chuồng 2 lớp

**Vấn đề hiện tại:** chuồng đang dùng **một file duy nhất `chuongmoigiasuc.png` kích thước 500 x 500**, dùng chung cho mọi chuồng. Nó là **một lớp phẳng**, nên con vật không thể bị hàng rào che đúng cách - con vật đi ra phía trước hàng rào thì vẫn bị hàng rào vẽ đè lên, hoặc ngược lại. Hiện code đang phải **ép sorting order của con vật lên >= 512** (`LivestockAI.FenceSortingOrderFloor = 512`) để chữa cháy, nghĩa là con vật **luôn** nằm trên hàng rào, kể cả khi nó đứng sau chuồng. Nhìn sai hoàn toàn.

**Cần vẽ:** tách chuồng thành **hai file PNG riêng**, để con vật render **ở giữa** hai lớp.

| File | Nội dung |
|---|---|
| `pen_fence_back.png` | **Lớp SAU** - phần hàng rào ở phía xa camera (cạnh Bắc và hai cạnh bên phía trên). Con vật sẽ vẽ ĐÈ LÊN lớp này. |
| `pen_fence_front.png` | **Lớp TRƯỚC** - phần hàng rào ở phía gần camera (cạnh Nam và hai cạnh bên phía dưới). Lớp này vẽ ĐÈ LÊN con vật. |

**Yêu cầu riêng:**

- Hai file phải **cùng canvas, cùng baseline**. Chồng hai file lên nhau phải ra đúng cái chuồng hoàn chỉnh, không lệch một pixel.
- Không được trùng lặp phần nào giữa hai lớp (nếu trùng, chỗ trùng sẽ đậm hơn do alpha cộng dồn).
- Vẫn giữ phong cách gỗ nâu ấm, khớp với hàng rào ở mục 3.2.

**Kích thước mục tiêu:** **500 x 500 px** mỗi lớp (giữ đúng kích thước file cũ để không phải chỉnh lại prefab). PPU 100. Pivot đáy giữa.

---

## 4. Quy trình giao hàng

### 4.1. Thư mục

| Asset | Thư mục |
|---|---|
| Biển mua đất | `Assets/Art/UI/Signs/` |
| Hàng rào lô Bắc | `Assets/Art/Environment/Fence/` |
| 4 món decor (5 stage) | `Assets/Art/Decor/Stages/<slug>/` |
| Hàng rào chuồng 2 lớp | `Assets/Art/Environment/Pen/` |

Nhắc lại: **không quá 7 cấp thư mục**, file sâu hơn không chuyển được.

### 4.2. Quy ước đặt tên

- Chữ thường, không dấu, không khoảng trắng, phân cách bằng dấu gạch dưới.
- Decor theo stage: đúng `stage_1.png` đến `stage_5.png`, không đổi tên.
- Slug thư mục decor phải **đúng chính xác** `banghieu`, `ghehoa`, `heothantai`, `vitvuive`.
- Mỗi lô giao kèm một file `README.txt` ngắn ghi **PPU của từng ảnh** và kích thước px thật.

### 4.3. Sau khi giao

1. Đội vẽ giao file về đúng thư mục trên.
2. **Sếp báo cho đội code.**
3. Đội code sẽ: gắn ảnh vào prefab, đặt pivot đáy giữa bằng `DecorPivotAnchorTool`, và **đăng ký slug vào bảng ánh xạ slug → itemID trong `DecorStageArtTool.cs`**.

> ⚠ **Điểm dễ mất công nhất:** art giao về mà **không có entry trong bảng slug → itemID thì bị bỏ qua trong im lặng** - không lỗi, không cảnh báo, chỉ đơn giản là không có gì hiện ra trong game. Vì vậy **mỗi slug decor mới đều phải được đăng ký itemID**. Không có bước này thì coi như chưa giao.

---

## 5. Lưu ý về chữ trong ảnh

**Tiếng Anh hiện là ngôn ngữ chính của game** (bảng dịch 1677 key, mặc định English).

Vì vậy, mọi asset có chữ nung sẵn trong ảnh phải theo một trong hai cách:

1. **Không có chữ - ưu tiên cách này.** Game tự vẽ chữ đè lên bằng TextMeshPro, nên art chỉ cần chừa sẵn vùng bảng trống (xem mục 3.1). Cách này dịch được vô hạn ngôn ngữ mà không phải vẽ lại.
2. **Nếu bắt buộc phải có chữ trong ảnh**, giao **cả hai bản: tiếng Anh và tiếng Việt**, cùng kích thước, cùng pivot, đặt tên hậu tố `_en` và `_vi`.

Bản tiếng Anh thường dài hơn tiếng Việt, nên nếu chọn cách 2 thì phải thiết kế khung theo bản tiếng Anh trước rồi mới rút về tiếng Việt, không làm ngược lại.
