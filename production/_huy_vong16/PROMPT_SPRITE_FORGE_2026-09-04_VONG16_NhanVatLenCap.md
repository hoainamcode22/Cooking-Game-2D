# PROMPT ĐỘI VẼ — VÒNG 16 · 2026-09-04
## Popup LÊN CẤP — bổ sung 1 nhân vật bán thân + vá 2 frame lạc bộ

> **ĐỌC TRƯỚC KHI VẼ**
> Chỉ VẼ ẢNH. **Không** chèn logic, không kèm script, không tạo prefab, không
> đổi tên file, không thêm file phụ (spritesheet gộp, file PSD, file preview…).
> Chỉ giao đúng số file trong hợp đồng đặt tên dưới đây.

**Tổng cộng: 15 file PNG.**

---

## 0. Ảnh tham chiếu bắt buộc

```
production/prompt-art/REF_BoBanThan_ChuanStyle.jpg
```

Trong đó là **3 bộ đã đạt chuẩn** đang chạy trong game (char_01 · char_03 · char_04),
mỗi bộ 11 frame + 1 blink. **Bám sát 100% nét vẽ, tỉ lệ đầu/thân, độ dày viền,
bảng màu và cách đổ bóng của 3 bộ này.** Đây là chuẩn duy nhất — không tham chiếu
bộ art nào khác trong project.

**Không được vẽ theo** 2 bộ sau (đang lẫn trong thư mục, là bộ CŨ đã bỏ):
- avatar trong **khung tròn viền vàng nền xanh navy**
- nhân vật **toàn thân** đứng giơ tay

---

## 1. Quy cách kỹ thuật (áp dụng cho CẢ 15 file)

| Mục | Giá trị |
|---|---|
| Định dạng | PNG-32, RGBA, **nền trong suốt hoàn toàn** |
| Canvas | **512 × 512 px** — đúng số này, không co giãn |
| Vùng nhân vật | nằm gọn trong `(95, 25)` → `(417, 488)`, tức **≈ 322 × 463 px** |
| Căn chỉnh | căn **giữa theo chiều ngang**; đáy thân chạm quanh `y = 488` |
| Khung hình | **BÁN THÂN**: đầu + vai + ngực, **cắt ngang ngực** — không vẽ hông, không vẽ chân, không vẽ tay giơ lên |
| Viền | viền nét đậm cùng độ dày với 3 bộ tham chiếu |
| Nền | **trong suốt** — không nền tròn, không khung, không bóng đổ xuống nền |

**Sạch khung — bắt buộc:**
- Mỗi file chỉ được có **đúng 1 mảng pixel liền nhau**. Không được sót mảnh vụn,
  đốm mờ, hay mảnh của frame khác dính vào. (Bên tôi có tool đếm mảng để nghiệm thu —
  dư 1 đốm 2 pixel cũng bị trả lại.)
- Không viền trắng/xám còn sót quanh mép sau khi xoá phông.

---

## 2. GÓI A — Nhân vật MỚI `char_05`: **CHÀNG NÔNG DÂN TRẺ**

**13 file.**

### Mô tả nhân vật
- Nam, trẻ (khoảng 20–25 tuổi), vẻ mặt tươi tắn, thân thiện.
- **Mũ rơm** vành mềm màu vàng rơm nhạt (khác hẳn mũ pith ô-liu của char_03/04
  và mũ lưỡi trai be của char_01 — để nhìn phát biết ngay là người thứ 4).
- Tóc nâu sáng, ló ra dưới vành mũ.
- **Áo sơ mi** vải thô màu xanh dương nhạt, **tay xắn tới khuỷu**.
- **Yếm/quần yếm** nâu đất, có 1 dây đeo vai và 1 túi ngực.
- Khăn rằn nhỏ vắt hờ ở cổ (màu đỏ gạch nhạt) — cho ăn nhịp với khăn đỏ của
  cô đầu bếp trong cùng thế giới game.
- Tông màu tổng thể **ấm** (rơm · nâu đất · xanh dương nhạt), độ bão hoà tương
  đương 3 bộ tham chiếu, không rực hơn.

### Danh sách file
```
char_05_f02.png   char_05_f03.png   char_05_f04.png   char_05_f05.png
char_05_f06.png   char_05_f07.png   char_05_f08.png   char_05_f09.png
char_05_f10.png   char_05_f11.png   char_05_f12.png
char_05_blink.png
char_05_f01.png
```

> Đánh số **bắt đầu từ f02** cho khớp với 3 bộ đang chạy. `f01` vẫn giao, vẽ
> giống hệt `f02` (cùng độ cao) — dùng làm khung nghỉ.

### Chuyển động 12 frame — chu kỳ NHÚN NẢY, lặp vô tận
Nhân vật **nhún lên xuống theo trục dọc**, thân giữ nguyên tư thế, chỉ đổi độ cao
và bóp/giãn nhẹ (squash & stretch). Không đổi tư thế tay, không xoay người.

| Frame | Độ cao (0 = thấp nhất, 10 = cao nhất) | Ghi chú |
|---|---|---|
| f01 · f02 | 0 | đáy — hơi bẹt xuống (squash nhẹ) |
| f03 | 0 | vẫn ở đáy, bắt đầu bật |
| f04 | 2 | đang lên |
| f05 | 6 | |
| f06 | **10** | **đỉnh** — hơi kéo dài lên (stretch nhẹ) |
| f07 | 9 | |
| f08 | 7 | |
| f09 | 5 | |
| f10 | 2 | bằng đúng f04 |
| f11 | 4 | nảy phụ nhỏ |
| f12 | 3 | về đáy |

Biên độ nhún: đỉnh cao hơn đáy khoảng **110 px** (đối chiếu char_03: đáy ở
`y≈136`, đỉnh ở `y≈25`). Frame `f04` và `f10` phải **giống hệt nhau từng pixel**.

### `char_05_blink.png`
Cùng tư thế và độ cao với `f02`, **chỉ khác duy nhất ở đôi mắt: nhắm lại**
(một nét cong mảnh). Mọi thứ còn lại không được xê dịch 1 pixel.

---

## 3. GÓI B — Vá 2 frame LẠC BỘ của `char_01`

**2 file.**

`char_01` (**ông thám hiểm râu, mũ lưỡi trai be, áo khaki nhiều túi**) ở frame
`f02`–`f12` đã đúng chuẩn và đang chạy tốt. Nhưng 2 file dưới đây lại là **cậu bé
áo caro đỏ của bộ khác** lọt vào — chạy popup là nhân vật đổi mặt giữa chừng.

Vẽ lại 2 file này **đúng là ông thám hiểm râu** trong `char_01_f02.png`:

```
char_01_f01.png     ← vẽ giống hệt char_01_f02.png (cùng tư thế, cùng độ cao)
char_01_blink.png   ← giống char_01_f02.png, CHỈ khác: hai mắt nhắm lại
```

---

## 4. Nơi giao file

```
production/art-handoff/2026-09-04_VONG16/A_Char05_NongDan/     ← 13 file char_05
production/art-handoff/2026-09-04_VONG16/B_Char01_VaFrame/     ← 2 file char_01
```

Giao đúng thư mục, đúng tên file. Bên tôi có tool tự nạp vào game theo tên —
sai 1 ký tự là tool bỏ qua file đó.

---

## 5. Bên tôi nghiệm thu bằng gì (để đội vẽ biết trước)

1. **Kích thước**: đúng `512 × 512` cả 15 file.
2. **Đếm mảng liền nhau** của kênh alpha: phải đúng **1 mảng/file**.
3. **bbox** nội dung nằm trong `(95,25)–(417,488)`.
4. **f04 vs f10**: phải trùng khít (MD5 giống nhau).
5. **blink vs f02**: chỉ được khác ở vùng mắt, phần còn lại lệch < 0.5% pixel.
6. Xem ở **cỡ hiển thị thật trong popup** — không kết luận bằng ảnh phóng to.
