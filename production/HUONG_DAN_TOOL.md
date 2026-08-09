# HƯỚNG DẪN SỬ DỤNG TOÀN BỘ TOOL

> Tất cả nằm ở menu **`Tools ▸ Farm ▸ ...`** trong Unity.
> Mọi tool đều có **bảng xem trước** và **Undo (Ctrl+Z)**. Không tool nào tự sửa khi vừa mở.

---

## ⚡ THỨ TỰ CHẠY (quan trọng — có tool phụ thuộc tool khác)

```
LẦN ĐẦU, CHẠY THEO ĐÚNG THỨ TỰ NÀY:

  ①  Suy Kích Thước Ô Công Trình     ← quyết định mỗi công trình chiếm mấy ô
  ②  Điền Thời Gian Xây              ← cần ① xong mới hợp lý
  ③  Điền Icon Unlock                ← độc lập, chạy lúc nào cũng được
  ④  Popup Lên Cấp (Township)        ← cần ③ xong thì popup mới có icon
  ⑤  Bảng Ô Art Xây Dựng             ← tuỳ chọn, làm khi có art
  ⑥  Setup Nhân Vật Đầu Bếp          ← độc lập hoàn toàn

  ⛔ Sửa Sorting Layer Chết          ← ĐANG KHOÁ, chưa chạy được
  🔬 Thử Nền Isometric               ← chỉ để xem thử, không ảnh hưởng game
```

---

## ① Suy Kích Thước Ô Công Trình

**Làm gì:** quyết định mỗi công trình chiếm bao nhiêu ô lưới (`gridSize`). Đây là nền của việc *không đặt đè lên nhau*.

**Cách dùng**
1. **Quét lại**
2. Lọc tab **BuildingData** → kiểm cột `Suy ra`. Số `7×5` cho chuồng, `4×4` cho nhà là hợp lý → để nguyên
3. Lọc tab **DecorData** → **sửa tay** những món cao lêu nghêu:

| Asset | Tool đề xuất | Nên gõ | Vì sao |
|---|---|---|---|
| Cột đèn | 2 × 6 | **2 × 2** | cột mảnh, phần cao là vươn lên trời |
| Bù nhìn | 4 × 6 | **2 × 2** | chỉ cắm 1 cọc xuống đất |
| Chân Hoa | 4 × 6 | **2 × 2** | |
| Ghế Hoa | 4 × 4 | **3 × 3** | chân đế nhỏ hơn lưng tựa |
| Khung Hoa | 12 × 8 | **8 × 3** | cổng hoa, đi xuyên qua được |

4. **Bỏ tick** `Home2` và `Home4` — hai asset này **thiếu `prefabToBuild`**, mua trong shop cũng không đặt được. Lỗi data có từ trước, bạn cần tự kéo prefab nhà vào 2 asset đó.
5. **ÁP DỤNG**

**Đọc cột Ghi chú**
- Chữ **XÁM** `pivot ở đáy (0,224) — đã tự bù` → **BÌNH THƯỜNG**. Pivot ở đáy là chuẩn đúng, hệ thống tự cộng bù.
- Chữ **ĐỎ** → lỗi thật, xử lý trước khi Apply.

**Quy tắc nhớ:** với đồ trang trí, **số thứ hai không nên lớn hơn số thứ nhất**.

---

## ② Điền Thời Gian Xây

**Làm gì:** đặt `buildTimeSeconds`. Để 0 = công trình hiện ngay, **không có giàn giáo**.

**Cách dùng**
1. Tick `Chỉ chọn dòng đang = 0`
2. **BỎ TICK HẾT DÒNG `Decor`** ← quan trọng
3. Chỉnh `Hệ số chia giá` nếu muốn (10 = giá 100 vàng → 10 giây)
4. Xem cột `HIỂN THỊ` và `RUSH ~` hợp lý chưa
5. **ÁP DỤNG**

**Vì sao bỏ Decor:** Township đặt đồ trang trí là hiện ngay. Bảng đang gợi ý Heo thần tài **5 phút**, Bù nhìn **1M40** — chờ 5 phút để cắm một con heo đất thì rất khó chịu.

**Data không nhất quán cần bạn xem lại:** `Chậu Hoa4` tính giá bằng **gem 100** trong khi Chậu Hoa 1–3 tính bằng **vàng 50/100/100** → công thức nhả ra 200s trong khi 3 cái kia chỉ 5–10s.

Có nút **"Đặt tất cả về 0"** để hoàn tác toàn bộ.

---

## ③ Điền Icon Unlock (Level Reward)

**Làm gì:** gán icon thật cho từng mục "vừa mở khoá" hiện trong popup lên cấp.

**Cách dùng**
1. **① Quét lại**
2. Xem dòng tổng: hiện đang **`64 mục — khớp 64 ✔ · KHÔNG khớp 0 ✘`** → tất cả đã tìm được icon
3. Duyệt qua vài mục xem icon có đúng nghĩa không
4. **② ÁP DỤNG vào 29 asset**

**Nút phụ**
- `In log ra Console` — xuất bảng đầy đủ để đọc kỹ
- `Đường dẫn XU/KIM CƯƠNG` — in ra đường dẫn 2 sprite tiền tệ mà HUD đang dùng
- `⚠ Xoá sạch unlockEntries (rollback)` — hoàn tác toàn bộ

**8 mục dùng icon tạm** (đúng nghĩa gần, cần art thật sau): phô mai (tạm dùng sữa — lệch nhất), bột gạo, 3 máy chế biến, bến tàu du lịch, nhà hàng ven biển, cây trồng chung.

---

## ④ Popup Lên Cấp (Township)

**Làm gì:** dựng cây Hierarchy popup lên cấp theo đúng bố cục video Township.

**Cách dùng**
1. **THOÁT PLAY MODE** trước (đang Play thì không lưu được scene)
2. Kiểm `Canvas đích` = **`Canvas_Popup`**. Tool tự chọn và **từ chối dựng** nếu bạn chọn canvas sai
3. **DỰNG POPUP** — tool tự lưu scene
4. **① Chẩn đoán** → xác nhận `Canvas cha: 'Canvas_Popup' (renderMode=ScreenSpaceOverlay)`
5. **Play** → **② Bật thử popup** → chờ ~2 giây → **③ Chụp ảnh + xuất báo cáo**

Ảnh và báo cáo xuất ra `Assets/_Debug_Capture/`.

**Thả art nhân vật vào:** `Layer_NhanVat_Sau` (bị băng rôn che chân) và `Layer_NhanVat_Truoc` (đè lên băng rôn, như con lợn trong video).

---

## ⑤ Bảng Ô Art Xây Dựng

**Làm gì:** 19 ô để bạn thả sprite vào cho hệ thống xây dựng. Để trống vẫn chạy — mọi mảnh hiện dưới dạng **khối màu nhận dạng**.

**Cách dùng**
1. Kit đã gán ✓ → bấm **"Gắn kit vào scene"** một lần rồi **Ctrl+S**
2. Bật **`Hiện nhãn tên ô`** → Play → mỗi khối màu hiện tên ô của nó
3. Có sprite thì kéo vào đúng dòng, khối màu tự biến mất
4. **TẮT `Hiện nhãn tên ô` trước khi build thật**

**Bảng màu**

| Màu | Mảnh |
|---|---|
| 🟫 Nâu đất | Thảm đất công trường |
| 🟧 Cam | Cọc giàn giáo |
| 🟨 Vàng | Thanh ngang |
| 🟩 Xanh mạ | Thanh chống chéo |
| 🩵 Xanh ngọc | Ván dựa |
| 🟦 Xanh dương | Công nhân |
| 🟪 Tím | Nền tên công trình |
| ⬛ Đen xám | Nền thanh đếm ngược |
| 🟩 Xanh đậm | Nền nút tăng tốc |
| ⬛ Đen | Nền thanh "MUA VỚI GIÁ" |
| 🤍 Trắng ngà | Hộp quà khánh thành |
| 🩷 Hồng | Ruy băng + hoa nơ |
| 🟥 Đỏ | Bóng bay |
| 🟨 Vàng mũ | Icon mũ bảo hộ |

`forcePlaceholderColors` = tô màu **cả khi đã có art**, tiện lúc căn vị trí.

**Công nhân:** nên gán `workerPrefab` (có Animator đập búa) thay vì sprite tĩnh.

---

## ⑥ Setup Nhân Vật Đầu Bếp

**Làm gì:** cắt sheet 30 frame thành 28 sprite, tạo 4 animation, Animator, và prefab.

**Cách dùng**
1. **1. Phân tích sheet** — xem bảng, cảnh báo frame lệch chân
2. **2. CẮT + TẠO TẤT CẢ**
3. **4. ĐẶT ĐẦU BẾP VÀO SCENE NGAY** ← nút này đặt luôn vào giữa khung nhìn

Hoặc tự kéo `Assets/NV_CHEF/Chef_NPC.prefab` từ cửa sổ **Project** vào Scene.

**Lưu ý:** nút 2 chỉ tạo *prefab asset* trong Project, **không** tự đặt vào Hierarchy. Nếu "không thấy nhân vật đâu" thì dùng nút 4.

Đầu bếp tự diễn vòng: Idle → Đảo → Xào → Hoàn thành → lặp. Không cần WASD.

**Về sorting:** đầu bếp hiện **luôn vẽ trên** công trình, do lỗi layer chết bên dưới. Với đầu bếp đứng nấu ở quầy hàng thì đó thường là điều bạn muốn.

---

## ⛔ Sửa Sorting Layer Chết — ĐANG KHOÁ

**Vấn đề nó định sửa:** 218 SpriteRenderer trỏ vào sorting layer `1669604809` **đã bị xoá** → rơi xuống dưới layer `Objects` → công trình dán đè lên nhau lộn xộn, nhân vật luôn nổi trên nhà.

**Vì sao khoá:** QA tìm ra 4 lỗ hổng khiến nó có thể làm hỏng nặng hơn.

1. Bỏ sót **10 `SortingGroup`** cũng dùng layer chết (`Tàu thủy 1`, `Taulua`…). `SortingGroup` **ghi đè** sorting của mọi con → sửa con là vô nghĩa
2. Layer chết còn nằm **trong chính prefab asset** (`House_01..05`, `Pen_01..04`, `Chauhoa_1..4`…) → sửa scene tạo prefab override hàng loạt, và kéo prefab mới vào map là **lại chết layer**
3. Bake mốc 500 sẽ **chôn Player NV_01** sau mọi công trình — Player dùng `YSortIso` mốc 0, lệch đúng 500 bậc
4. Bấm ÁP DỤNG hai lần thì **trừ toạ độ Y hai lần** → sort vỡ

**Nút Quét vẫn dùng được** để xem hiện trạng (chỉ đọc, không sửa).

Đây là **Việc 1** trong bản chẩn đoán — thứ đang làm game nhìn lộn xộn nhất. Khi nào muốn làm cho tử tế thì nói, tôi sửa 4 lỗ hổng này rồi mở khoá.

---

## 🔬 Thử Nền Isometric — chỉ để xem thử

Chuyển 5 thư mục nền (`Grass`, `Dirt`, `Soil`, `SoilWatered`, `Walkway`) sang ô thoi isometric, xuất ra `Assets/_Iso_Preview/`.

**Không ảnh hưởng game.** Asset gốc còn nguyên (đã xác minh `isReadable: 0`, `textureCompression: 0`). Thư mục preview 4,4 MB nằm chết, không scene nào tham chiếu.

Xoá bằng nút **"Xoá toàn bộ bản xem thử"** hoặc xoá thẳng thư mục.

**Không chuyển được:** `House`, `Fence`, `Elevation`, `Pinetrees`, `Warehouse` — ảnh gốc vẽ nhìn thẳng mặt, không có pixel mặt hông.

---

## 🛠 HAI CÔNG CỤ DEBUG (không ở menu)

### Camera Dev Panel
Gắn component `CameraDevPanel` vào **Main Camera**.

| Phím | Việc |
|---|---|
| **F1** | Zoom ra xem toàn bản đồ |
| **F2** | Bật/tắt Dev Mode (nới zoom 200–6000; người chơi vẫn khoá 400–1500) |
| **F3** | Ẩn/hiện panel |

Panel hiện ortho size, viewport, FPS, và **% chiều cao map đang thấy**. Tự huỷ ở bản release.

### Popup Capture Reporter
Bấm **F10** trong Play Mode → xuất ra `Assets/_Debug_Capture/`:
- `game_view.png` — ảnh Game view
- `popup_report.txt` — trạng thái runtime: từng cấp tổ tiên active/inactive, canvas cha + renderMode, `lossyScale`, alpha CanvasGroup, sprite nào null, số config

Tự huỷ ở bản release.

---

## ✅ SAU KHI CHẠY XONG ① VÀ ②

Play → mua một công trình → thử đặt. Đúng thì:

- Đặt chồng lên công trình khác → thảm **ĐỎ**, nút ✓ **XÁM không bấm được**
- Đặt chỗ trống → ra **giàn giáo màu cam/vàng** + đồng hồ đếm ngược + nút rush xanh, **không** hiện công trình thật ngay
- Xây xong → hiệu ứng ăn mừng rồi công trình thật hiện ra

Màu cam/vàng là placeholder của bộ ô art — đúng thiết kế.
