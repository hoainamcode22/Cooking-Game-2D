# 🚀 HƯỚNG DẪN BẤM NÚT — HỆ TÀU KHÁCH DU LỊCH

> Code đã copy sẵn vào project rồi. Anh **không phải copy gì, không phải điền số gì, không phải kéo waypoint**.
> Toàn bộ việc setup gói trong **1 nút bấm**.

---

## ✅ LÀM ĐÚNG 4 BƯỚC NÀY

### Bước 1 — Mở Unity, đợi compile xong
Mở project, đợi thanh tiến trình dưới góc phải chạy hết.
**Console phải 0 lỗi đỏ.** Nếu có lỗi đỏ thì dừng lại, chụp màn hình gửi tôi.

### Bước 2 — Mở scene `SCN_Farm`
`Assets/_Game/Scenes/SCN_Farm.unity` — bấm đúp để mở.
**Quan trọng:** phải mở đúng scene này trước khi bấm nút, vì tool dựng object vào scene đang mở.

### Bước 3 — Bấm nút duy nhất
```
Tools ▸ Farm Game ▸ Tourist Boat ▸ ★ SETUP TẤT CẢ (1 nút)
```
Nút này nằm **trên cùng** trong menu Tourist Boat (có dấu ★ cho dễ thấy).

Bấm xong ngồi đợi khoảng **1–3 phút** (nó phải import 132 ảnh nhân vật và tạo 88 animation).
Sẽ thấy thanh tiến trình chạy qua 8 bước. **Đừng bấm gì trong lúc đó.**

Cuối cùng hiện **một bảng tổng kết duy nhất**. Đọc bảng đó:
- Dòng nào có ✔ là xong.
- Dòng nào có ✖ đỏ là còn thiếu, bảng sẽ ghi luôn cách khắc phục — anh gửi tôi dòng đó.

### Bước 4 — Bấm **Ctrl + S** để lưu scene
Tool cố ý **không tự lưu** để anh xem kết quả trước. Ưng thì Ctrl+S, không ưng thì Ctrl+Z hoàn tác sạch.

---

## 🎮 XONG. GIỜ CHƠI THỬ

Bấm **Play**, chơi tới **Level 10** (hoặc dùng công cụ dev để nhảy thẳng lên Lv10).

Anh sẽ thấy: hội thoại mở bến → tàu 01 chạy vào sát bờ → **tấm ván gỗ bắc xuống** → 3–6 khách du lịch
xuống tàu lần lượt → đi theo đường đất → **xếp hàng trước nhà hàng** → bong bóng món ăn mở lần lượt trên
đầu từng khách.

Nấu món khách đang đòi, đưa vào kho, rồi **bấm vào khách đó** → khách trả vàng + EXP, **mặt cười bay lên
góc HUD**, khách quay về tàu. Khách cuối lên tàu → ván gỗ rút → tàu rời bến → **5 phút sau tàu tới lại**.

Bảng khóa ở bến 2 và 3: bấm vào → hiện popup mua slot → đủ tiền bấm MUA → pháo hoa sao vàng, tàu chạy vào ngay.

---

## ⏱️ MẸO TEST NHANH (không phải ngồi đợi 5 phút)

Mở `Assets/_Game/ScriptableObjects/TouristBoatConfig.asset`, tìm dòng **`Debug Time Scale`**, đổi thành **`60`**.
Lúc này 1 giây thực = 1 phút trong game: tàu 5 phút thành 5 giây, khách giận sau 30 giây thay vì 30 phút.
**Test xong nhớ trả về `1`** trước khi build.

*(Chỉ có tác dụng trong Unity Editor, bản game thật luôn chạy tốc độ bình thường — không sợ quên.)*

---

## 🔍 NẾU CÓ TRỤC TRẶC

Bấm: `Tools ▸ Farm Game ▸ Tourist Boat ▸ 6. Chẩn Đoán`
Nó in ra Console toàn bộ tình trạng: bến nào mở, tàu đang ở pha nào, còn bao lâu tới chuyến sau,
config có sai chỗ nào không. Copy nguyên đoạn đó gửi tôi là tôi biết bệnh.

Muốn chơi lại từ đầu: `Tools ▸ Farm Game ▸ Tourist Boat ▸ 8. Xóa Save Tàu` (xóa sạch cả tàu, cả khách, cả cờ thông báo).

---

## 📌 NHỮNG GÌ TOOL ĐÃ TỰ LÀM HỘ ANH

| # | Việc | Chi tiết |
|---|---|---|
| 1 | Điền 13 thông số | gồm cả `maxDockMinutes = 35` (số này phải lớn hơn 30 phút kiên nhẫn của khách, tool tự canh) |
| 2 | Cắt + import 132 ảnh nhân vật | pivot dưới chân, 88 animation đi bộ 4 hướng, 11 prefab khách |
| 3 | Dựng hệ thống vào scene | TouristSystem, hàng chờ, 3 đường đi, 3 tấm ván gỗ |
| 4 | **Đặt sẵn đường đi + chỗ xếp hàng** | tôi đã đo toạ độ thật từ scene của anh: hàng chờ đặt tại (400, −2700) ngay trước cửa nhà hàng, mỗi bến 3 điểm đường bám khu đất, né 2 căn nhà bên trái |
| 5 | Dựng 2 popup | popup báo tàu sắp cập bến + popup mua slot bến |
| 6 | Dịch 3 bến sát bờ | dời lên 200 đơn vị về phía đất liền |
| 7 | Tự kiểm tra | 5 nhóm hạng mục, thiếu gì báo đỏ ngay |

**Chỉ còn 1 việc duy nhất trong tương lai:** khi đội vẽ giao art (tấm ván gỗ, bong bóng, mặt cười, mặt tức giận,
khung gỗ popup), anh kéo ảnh vào ô tương ứng trong Inspector. Hiện game đang chạy bằng hình tạm vẽ bằng code
nên **không chờ art vẫn chơi được đầy đủ**.

---

## ⚠️ MỘT LƯU Ý DUY NHẤT

Nếu sau khi chạy tool anh **tự tay kéo lại** waypoint hay chỗ xếp hàng cho đẹp hơn, thì **đừng bấm nút ★ lần nữa**
— nó sẽ ghi đè về toạ độ mặc định. Muốn đổi vĩnh viễn thì báo tôi toạ độ mới, tôi cập nhật vào tool.

(Riêng việc dịch bến sát bờ thì an toàn: tool nhận biết đã dịch rồi nên bấm lại 5 lần cũng không cộng dồn.)
