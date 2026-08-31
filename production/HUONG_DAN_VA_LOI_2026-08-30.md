# 🔧 BẢN VÁ — 3 VẤN ĐỀ SẾP BÁO

> Code vá đã copy sẵn vào project. Backup thêm lần nữa trước khi vá:
> `production/backup_boat_2026-08-30/` (scene + config + prefab khách) và `production/backup_ui_2026-08-30/` (2 file mission).

---

## 📌 KẾT LUẬN ĐIỀU TRA (quan trọng nhất)

Tôi đã soi scene, prefab, dấu thời gian từng file. Kết quả:

**Tool ★ của sếp đã chạy THÀNH CÔNG hoàn toàn.** Bằng chứng: config điền đủ 13 thông số, 132 ảnh nhân vật
đã import, 11 prefab khách đã tạo, hệ thống đã dựng vào scene, chỗ xếp hàng đúng vị trí (400, −2700),
3 bến đã dịch sát bờ +200. Sếp tưởng chưa bấm, thực ra bấm rồi.

**Popup tàu hỏa trắng và mất chữ nhiệm vụ KHÔNG phải do hệ tàu khách gây ra.** Dấu thời gian nói rõ:

| Thời đim | Việc |
|---|---|
| 29/08 **16:43** | 2 file `SkinKit.cs` + `UnifiedTaskPopupUI.cs` bị sửa (đổi font sang Baloo2) ← **gây mất chữ nhiệm vụ** |
| 29/08 **16:50–16:57** | Gói tàu khách mới được copy vào — **đến sau 7–14 phút** |
| 25/08 | Gói Train được tạo với file ảnh lỗi ← **gây popup tàu hỏa trắng** |

---

## ✅ LỖI 1 — Mất chữ tên nhiệm vụ **(ĐÃ VÁ XONG, sếp không phải làm gì)**

**Nguyên nhân:** dòng tên nhiệm vụ là ô chữ DUY NHẤT trong popup đặt chế độ `Ellipsis` (cắt bằng dấu ba chấm).
Khi font đổi sang Baloo2 (chữ tròn, rộng hơn font cũ), tên nhiệm vụ dài tràn khỏi khung 480px → TMP rơi vào
nhánh Ellipsis và **trả về 0 ký tự**, tức mất trắng. Các chữ khác trong popup dùng chế độ mặc định nên vẫn hiện.
Không phải lỗi thiếu dấu tiếng Việt — font Baloo2 có đủ dấu.

**Đã sửa:** đổi `Ellipsis` → `Truncate` (cắt phần thừa nhưng luôn hiện phần đầu, không bao giờ mất trắng).
Sửa đúng 1 dòng trong `UnifiedTaskPopupUI.cs`.

> Ô icon nhiệm vụ trắng thì **không phải lỗi** — toàn bộ nhiệm vụ đều để trống icon, code cố ý ẩn ảnh.

---

## 🔨 LỖI 2 — Popup "TÀU CHỞ HÀNG" trắng đặc **(cần sếp bấm 1 nút)**

**Nguyên nhân:** 21 file PNG trong `Assets/Export_Train_UI_Package/Sprites/` có file `.meta` được viết bằng
script chứ không do Unity sinh: mang mã GUID giả và **thiếu dòng khai báo loại ảnh**. Unity vì thế import chúng
thành **Texture thường chứ không phải Sprite** → prefab popup không gán được ảnh nào → Unity vẽ ra hình chữ nhật
trắng. Mấy icon toa còn thấy được là nhờ đường nạp khác.

**Sếp bấm:**
```
Tools ▸ Farm Game ▸ Train ▸ 🔧 Sửa import sprite Train (trắng UI)
```
Tool sẽ đặt lại loại ảnh cho cả 21 file, khôi phục viền 9-slice, rồi liệt kê ra ô nào còn thiếu ảnh.
Nó **không đụng vào file `.meta`** (đụng vào là mất hết liên kết cũ) mà đi qua đúng đường Unity cho phép.

Báo cáo ghi tại `production/session-state/TRAIN_SPRITE_FIX_REPORT.txt` — sếp gửi tôi file đó nếu còn trắng.

---

## 🔨 LỖI 3 — Chưa thấy nhân vật khách **(đã vá code, sếp chạy lại tool ★)**

Tìm ra 2 lỗi thật trong gói của tôi:

**a) Sai lớp hiển thị.** Đội Dev giả định project có lớp tên `CongTrinh`, nhưng project sếp thực tế chỉ có
5 lớp: `Bottom`, `Default`, `Objects`, `ObjectsFront`, `Foreground`. Tên sai → Unity **âm thầm** đẩy nhân vật
về lớp `Default` thấp nhất → **bị cỏ, cây, nhà che khuất**. Đã sửa: khách nằm ở `ObjectsFront`, tấm ván ở
`Objects`, bong bóng ở `Foreground`; tên lớp không tồn tại thì báo cảnh báo rõ chứ không im lặng nữa.

**b) Tấm ván gỗ bé bằng hạt cát.** Tool gán ảnh khung gỗ 512px = **5 đơn vị** trong bản đồ mà 3 bến cách nhau
740 đơn vị và nhân vật cao 170 đơn vị. Nhìn bằng mắt thường không thấy. Đã sửa thành **dài 420 × dày 90**,
và dịch cho hai đầu ván chạm đúng mạn tàu với bờ. Kèm theo, đội phát hiện ván dài ra làm điểm đường đi đầu tiên
nằm *phía sau* đầu ván khiến khách đi giật lùi — đã xử lý luôn.

**Sếp bấm lại:** `Tools ▸ Farm Game ▸ Tourist Boat ▸ ★ SETUP TẤT CẢ (1 nút)` rồi **Ctrl+S**.
(Chạy lại an toàn, không nhân đôi thứ gì.)

---

## 🔍 LỖI 4 — "Vật thể quá to che hết map" **(cần sếp giúp tôi 1 lần)**

Cái này tôi **chưa dám kết luận** vì chưa thấy được màn hình sếp. Tôi đã loại trừ được: 2 popup mới đều đang tắt
đúng (không phải chúng che), tool không hề đụng vào ảnh nào của project ngoài 132 ảnh nhân vật, và vật thể
to nhất trong scene là `"Công trình đèn"` (đã có sẵn từ trước, không có hình nên không vẽ ra gì).

Có khả năng vùng vàng ở ảnh là **bãi cát của map** (map có sẵn 868 ô cát ở vùng biển) chứ không phải lỗi —
nhưng tôi cần dữ liệu thật để chắc.

**Sếp bấm giúp:**
```
Tools ▸ Farm Game ▸ Tourist Boat ▸ 🔍 Chẩn đoán & Xuất báo cáo
```
Bấm **2 lần**: một lần ở chế độ thường, một lần trong lúc **đang Play** (lúc thấy vùng vàng đó).
Tool ghi ra `production/session-state/DIAG_REPORT.txt` — nó liệt kê **mọi vật thể có kích thước trên 3000 đơn vị**
(sắp từ to đến nhỏ, thủ phạm sẽ nằm ngay dòng đầu), mọi ô ảnh đang trắng vì thiếu hình, mọi khung canvas,
và tình trạng từng khách. Sếp **không cần gửi file cho tôi** — tôi đọc thẳng được trong máy sếp.

Nếu tiện, sếp chụp thêm 1 ảnh lúc vùng vàng che map kèm **cửa sổ Hierarchy mở rộng bên trái** thì tôi khoanh
vùng nhanh hơn nhiều.

---

## THỨ TỰ LÀM CHO GỌN

1. Mở Unity, đợi compile — **0 lỗi đỏ**
2. `🔧 Sửa import sprite Train (trắng UI)` → xem popup tàu hỏa hết trắng chưa
3. Mở `SCN_Farm` → `★ SETUP TẤT CẢ (1 nút)` → **Ctrl+S**
4. `🔍 Chẩn đoán & Xuất báo cáo` (bấm 1 lần thường + 1 lần trong Play)
5. Play → lên Lv10 → xem khách đã hiện chưa
6. Mở popup Nhiệm vụ → xem tên nhiệm vụ đã hiện chữ chưa

Xong bước 4 là tôi có đủ dữ liệu để xử nốt vụ "vật thể che map".
