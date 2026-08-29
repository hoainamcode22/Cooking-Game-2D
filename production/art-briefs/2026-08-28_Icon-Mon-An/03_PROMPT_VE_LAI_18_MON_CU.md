# 🔁 PROMPT VẼ LẠI (TUỲ CHỌN) — 18 ICON MÓN ĂN CŨ

**Mục đích:** bộ 18 icon món ăn hiện tại đã dùng được, nhưng vẽ ở nhiều thời điểm khác nhau nên
**độ dày outline, cỡ vật đựng và độ bóng không đều nhau**. Khi 20 icon món mới (đợt 28-08) vào game,
mở sổ công thức sẽ thấy 38 icon cạnh nhau — lệch style sẽ lộ rõ.

**Việc này KHÔNG chặn tiến độ.** Ưu tiên vẽ 20 món mới trước
(`02_PROMPT_20_MON_MOI.md`). Chỉ làm đợt vẽ lại này khi đội vẽ có chỗ trống.

**Bắt buộc đọc trước:** `01_LUAT_DONG_BO_STYLE.md` (cùng thư mục).

---

## Nguyên tắc vẽ lại — GIỮ, KHÔNG ĐỔI

- ✅ **Giữ nguyên món và nguyên liệu**: `Bò xào tiêu` vẽ lại vẫn là bò xào tiêu, cùng loại vật đựng,
  cùng nguyên liệu nhìn thấy. Người chơi đã quen nhận món bằng mắt — đổi tạo hình là đổi nhận diện.
- ✅ **Giữ nguyên tên file** (tiếng Việt có dấu, y hệt), **ghi đè đúng file cũ** ở
  `Assets/Assetsgame/Món ăn/`. Giữ tên = GUID không đổi = bên dev **không phải nối lại data,
  không phải sửa scene**.
- 🔧 **Chỉ chuẩn hoá phần kỹ thuật**: độ dày outline, bảng màu ấm chuẩn, cỡ vật đựng so với khung,
  độ bóng highlight, khung 512×512, nền trong suốt sạch, bỏ mọi bóng đổ rời.

---

## 18 file cần vẽ lại (đúng tên, ghi đè tại chỗ)

| # | Tên file (.png) | Vật đựng chuẩn |
|---|---|---|
| 1 | `Khoai tây chiên.png` | Đĩa gốm |
| 2 | `Cơm chiên trứng.png` | Đĩa gốm |
| 3 | `Nước mía chanh.png` | Ly thuỷ tinh |
| 4 | `Trứng chiên cà chua.png` | Đĩa gốm |
| 5 | `Salad bắp cải chanh.png` | Đĩa gốm |
| 6 | `Bắp cải xào nấm.png` | Đĩa gốm |
| 7 | `Súp ngô nấm.png` | Bát gốm |
| 8 | `Salad nấm và rau.png` | Đĩa gốm |
| 9 | `Thịt heo luộc cuốn rau.png` | Đĩa gốm |
| 10 | `Canh khoai tây thịt heo.png` | Bát gốm |
| 11 | `Gà nướng lu.png` | Thớt gỗ |
| 12 | `Gà xào ớt.png` | Đĩa gốm |
| 13 | `Nấm xào thịt bò.png` | Đĩa gốm |
| 14 | `Sườn heo xào chua ngọt.png` | Đĩa gốm |
| 15 | `Trứng ốp la bò né.png` | Chảo gang nhỏ hoặc đĩa gốm |
| 16 | `Bò xào tiêu.png` | Đĩa gốm |
| 17 | `Bò hầm cà rốt.png` | Bát gốm |
| 18 | `Phở bò tái.png` | Bát gốm sâu |

**Hai file KHÔNG vẽ lại:** `Canh chua cá.png` và `Cá nướng tiêu.png` — hai món cá đã bị xoá khỏi
logic game (không còn công thức, không còn hồ cá). Cứ để nguyên file, đừng bỏ công vẽ.

---

## 🎨 LUẬT ART STUDIO — BẮT BUỘC MỌI PROMPT VẼ PHẢI ĐÍNH KÈM
> Ban hành 2026-08-26 theo lệnh Sếp. MỌI prompt gửi đội vẽ (GPT/agent-sprite-forge) PHẢI dán nguyên khối này.

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ
   trên BẤT KỲ asset nào. Text do game render bằng TMP. Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ
   dưới chân object. *(Icon món ăn: được phép bóng tiếp xúc rất nhẹ sát đáy bát/đĩa, trong silhouette.)*
3. ✅ **Meta Unity chuẩn**: spriteMode: 1 (Single) từng file · pivot Bottom-Center cho object đứng đất
   *(icon món ăn là UI phẳng → pivot Center, dev tự cài; đội vẽ KHÔNG đụng `.meta`)*.
4. ✅ **Frame animation**: mọi frame cùng hướng = CÙNG kích thước canvas; frame 01 = tư thế nghỉ;
   KHÔNG khói/hiệu ứng bake vào frame. *(Không áp dụng cho ảnh tĩnh; khói món nóng trong icon thì được.)*
5. ✅ **Style chuẩn**: burgundy #8E1F3B + đồng vàng #D9A441, gỗ nâu ấm, outline nâu đậm cartoon,
   dễ thương cho phụ nữ & trẻ em.
6. ✅ Giao đúng TÊN FILE + THƯ MỤC, không thêm file phụ (_single, @2x tự ý...).

---

## Giao file
- Bỏ vào `GIAO_FILE_TAI_DAY/`, **giữ y nguyên tên tiếng Việt có dấu** của file cũ
  (bản gốc để đối chiếu nằm ở `THAM_CHIEU_ICON_CU/`). Bên dev ghi đè đúng file cũ trong
  `Assets/Assetsgame/Món ăn/` — giữ tên = giữ GUID = không phải nối lại data.
- **Không cần kèm `.meta`** (giữ file `.meta` cũ = GUID cũ = không phải nối lại data).
- Giao được bao nhiêu thì gửi bấy nhiêu, không cần đủ 18 mới gửi.
