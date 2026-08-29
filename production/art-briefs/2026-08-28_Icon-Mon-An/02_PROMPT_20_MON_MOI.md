# 🍲 PROMPT VẼ — 20 ICON MÓN ĂN MỚI (đợt 28-08-2026)

**Bối cảnh:** logic của 20 món này đã code xong và đã vào game (công thức nấu, sổ công thức,
đơn hàng dân làng, chợ, kho). Hiện mỗi món đang dùng **tạm icon của nguyên liệu chính**
(ví dụ "Súp bí đỏ kem sữa" đang hiện quả bí đỏ) — nên cần 20 icon thật để thay.

**Bắt buộc đọc trước khi vẽ:** `01_LUAT_DONG_BO_STYLE.md` (cùng thư mục này) — sổ tay đồng bộ
style icon món ăn. Mọi quy tắc góc nhìn / vật đựng / nét / màu / nền / quy cách file nằm ở đó,
prompt này chỉ mô tả **nội dung từng món**.

**Nhắc lại 3 điểm dễ sai nhất:**
- Icon món ăn = **hình phẳng chúc xuống 40–50°**, KHÔNG vẽ isometric 45° như cây cối ngoài nông trại.
- Nền **trong suốt**, **không bóng đổ rời**, **không chữ**.
- Tên file **đúng tiếng Việt có dấu** như bảng dưới, bỏ vào `GIAO_FILE_TAI_DAY/`.
- Muốn hiểu icon này ghép vào đâu, hiện to bao nhiêu px, vì sao cấm chữ/nền/bóng đổ:
  đọc `04_KY_THUAT_GHEP_VAO_GAME.md`. Bảng tra file → món: `05_BANG_GHEP_FILE_VAO_MON.csv`.

---

## 📋 BẢNG 20 MÓN

Cột "Nguyên liệu" là thứ **phải nhìn thấy được** trong icon — người chơi nhận món bằng mắt,
không đọc chữ. Cột "Vật đựng" theo mục 2 của sổ tay.

| # | Tên file (.png) | Vật đựng | Nguyên liệu phải thấy | Ghi chú tạo hình |
|---|---|---|---|---|
| 1 | `Cơm chiên bắp cải.png` | Đĩa gốm | Cơm rang vàng nhạt, bắp cải thái sợi xanh nhạt | Cơm đánh tơi, vài sợi bắp cải nổi trên mặt, 1–2 sợi khói mảnh |
| 2 | `Súp ngô trứng.png` | Bát gốm | Hạt ngô vàng, dải trứng vàng nhạt trong nước súp sánh | Nước súp hơi sánh vàng nhạt, hạt ngô rõ từng hạt |
| 3 | `Salad bắp cải cà rốt.png` | Đĩa gốm | Bắp cải sợi trắng-xanh, cà rốt sợi cam | Trộn tơi, cao lên giữa đĩa, mặt hơi bóng nước trộn |
| 4 | `Gà xào bắp cải.png` | Đĩa gốm | Miếng thịt gà nâu vàng, lá bắp cải xanh | Xào áp chảo, mặt bóng mỡ nhẹ |
| 5 | `Bánh ngô chiên giòn.png` | Đĩa nhỏ | 3 chiếc bánh ngô tròn dẹt vàng ruộm, thấy hạt ngô nhú lên mặt bánh | Rìa bánh giòn sậm hơn giữa bánh |
| 6 | `Canh cà chua cà rốt.png` | Bát gốm | Miếng cà chua đỏ, khoanh cà rốt cam, lá rau thơm xanh | Nước canh đỏ-cam trong, rau thơm rắc trên |
| 7 | `Khoai tây xào cà rốt.png` | Đĩa gốm | Khoai tây thanh vàng nhạt, cà rốt thanh cam | Xào se mặt, có chút nước tương nâu bóng |
| 8 | `Canh nấm cà chua.png` | Bát gốm | Nấm nâu nhạt cắt đôi, cà chua đỏ, rau thơm | Nước canh nâu-đỏ nhạt, khói mảnh |
| 9 | `Bánh ngô nướng mật mía.png` | Lá chuối | Bánh ngô nướng vàng sậm, phết mật mía nâu bóng | Vết cháy xém nhẹ, mật mía chảy 1 giọt |
| 10 | `Bánh khoai tây nấm chiên.png` | Đĩa nhỏ | 2–3 viên bánh khoai tây chiên vàng, lát nấm lộ ra mép | Vỏ ngoài ráp giòn, cắt hé 1 viên thấy nhân nấm |
| 11 | `Bánh bí đỏ hấp mía.png` | Lá chuối | 2 miếng bánh bí đỏ hấp màu cam-vàng, mặt bóng mượt | Kết cấu mềm mịn, không giòn; vài sợi mía kẹo bên cạnh |
| 12 | `Nước ép dưa hấu chanh.png` | Ly thuỷ tinh | Nước ép hồng-đỏ, 1 lát chanh vắt mép ly, ống hút | Ly trong, thấy sắc chuyển từ đỏ xuống hồng nhạt |
| 13 | `Dưa hấu trộn muối ớt.png` | Đĩa gốm | Khối dưa hấu đỏ vuông, rắc muối ớt đỏ cam, lá rau thơm | Khối dưa mọng nước, vỏ xanh còn giữ 1 mép |
| 14 | `Súp bí đỏ kem sữa.png` | Bát gốm | Súp bí đỏ cam đặc, xoáy kem sữa trắng trên mặt | Xoáy kem hình vòng, mặt súp mịn không hạt |
| 15 | `Dưa hấu dầm sữa đá.png` | Ly thuỷ tinh | Miếng dưa hấu đỏ dầm, sữa trắng, viên đá | Ba lớp phân biệt rõ: đỏ – trắng – đá trong |
| 16 | `Canh bí đỏ sườn non.png` | Bát gốm | Khối bí đỏ cam, miếng sườn non (thịt nạc hồng, không lộ xương thô) | Nước canh vàng-cam, hạt tiêu rắc lấm tấm |
| 17 | `Gà hầm nấm cà rốt.png` | Bát gốm sâu | Miếng gà nâu vàng, nấm nâu, khoanh cà rốt cam | Nước hầm nâu sánh, khói mảnh, mặt bóng |
| 18 | `Chè ngô sữa kem.png` | Ly thuỷ tinh thấp | Hạt ngô vàng trong chè trắng-vàng sánh, chút kem trên mặt | Sánh sệt, hạt ngô lơ lửng, đầu ly có kem xoáy |
| 19 | `Bò hầm bí đỏ sốt kem.png` | Bát gốm sâu | Khối thịt bò nâu sậm, bí đỏ cam, khoai tây vàng nhạt, sốt kem ngà | Sốt kem sánh bọc quanh, món "sang" nhất bộ — thêm hạt tiêu, khói mảnh |
| 20 | `Salad dưa hấu bò áp chảo.png` | Đĩa gốm rộng | Lát bò áp chảo hồng-nâu xếp lên khối dưa hấu đỏ, rau thơm, sốt chanh ớt | Món khó nhất — bố cục xếp tầng đẹp, sốt rưới thành đường mảnh |

---

## 🎯 GỢI Ý THỨ TỰ GIAO (để dev thay dần, không phải chờ đủ 20)

- **Lô 1 (ưu tiên cao — cấp 1–8, người chơi mới gặp ngay):** #1 #2 #3 #4 #5 #6 #7 #8
- **Lô 2 (cấp 9–15):** #9 #10 #11 #12 #13 #14 #15
- **Lô 3 (cấp 17–30, món trung + khó):** #16 #17 #18 #19 #20

Giao lô nào xong bên dev gán lô đó, không cần đợi cả bộ.

---

## 🎨 LUẬT ART STUDIO — BẮT BUỘC MỌI PROMPT VẼ PHẢI ĐÍNH KÈM
> Ban hành 2026-08-26 theo lệnh Sếp. MỌI prompt gửi đội vẽ (GPT/agent-sprite-forge) PHẢI dán nguyên khối này.

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ
   trên BẤT KỲ asset nào (thân tàu, toa, nhà, thùng, bảng...). Text do game render bằng TMP.
   Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ
   dưới chân object — bóng do game tự vẽ để đồng nhất theo giờ trong ngày.
   *(Với icon món ăn: được phép có bóng tiếp xúc rất nhẹ SÁT ĐÁY bát/đĩa, nằm trong silhouette
   của vật đựng — xem mục "Nền & bóng" của sổ tay. Ngoài ra không bóng.)*
3. ✅ **Meta Unity chuẩn**: spriteMode: 1 (Single) từng file · pivot Bottom-Center cho object đứng đất
   *(riêng icon món ăn là UI phẳng nên pivot Center — bên dev tự cài, đội vẽ không đụng `.meta`)*.
4. ✅ **Frame animation**: mọi frame cùng hướng = CÙNG kích thước canvas, thân đứng yên cùng vị trí;
   frame 01 = tư thế nghỉ; KHÔNG khói/hiệu ứng bake vào frame (code phun runtime).
   *(Không áp dụng cho 20 file tĩnh trong prompt này — giữ nguyên văn vì đây là khối bắt buộc đính kèm.
   Riêng khói của món nóng thì ĐƯỢC vẽ vào icon, vì icon món ăn là ảnh tĩnh không có runtime effect.)*
5. ✅ **Style chuẩn**: theo bộ Export_Train_UI_Package (burgundy #8E1F3B + đồng vàng #D9A441,
   gỗ nâu ấm, outline nâu đậm cartoon, dễ thương cho phụ nữ & trẻ em).
6. ✅ Giao đúng TÊN FILE + THƯ MỤC được đặt trong prompt, không thêm file phụ (_single, @2x tự ý...).

---

## Giao file
- 20 file PNG, tên **đúng như cột "Tên file"** ở bảng trên (tiếng Việt có dấu).
- Thư mục: **`GIAO_FILE_TAI_DAY/`** ngay trong hồ sơ này — đọc `DOC_TRUOC_KHI_GIAO.md` trong đó
  để tự soát 8 điểm. Bên dev soát xong mới chuyển vào `Assets/Assetsgame/Món ăn/`.
- **Không cần kèm `.meta`** — nhận file xong bên dev gán vào `DishData.dishSprite` +
  `InventoryItemData.icon` của đúng 20 món, thay icon tạm, rồi báo lại kết quả.
