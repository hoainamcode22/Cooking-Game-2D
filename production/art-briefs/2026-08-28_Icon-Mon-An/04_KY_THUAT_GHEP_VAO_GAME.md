# ⚙️ ICON NÀY SẼ ĐƯỢC GHÉP VÀO ĐÂU — giải thích kỹ thuật cho đội vẽ

> Mục đích của file này: để đội vẽ **hiểu mình đang vẽ cái gì, để ghép vào đâu**, chứ không phải
> vẽ mù theo mô tả. Hiểu rồi thì tự biết chỗ nào được phóng khoáng, chỗ nào không được sai một ly.
> Không cần biết lập trình để đọc file này.

---

## 1. Đường đi của một file PNG, từ tay đội vẽ tới màn hình người chơi

```
   Bạn vẽ                bên dev cài                bên dev gán                 game hiện
┌──────────────┐      ┌───────────────┐      ┌──────────────────────┐      ┌────────────────┐
│ Súp bí đỏ    │  →   │ Unity import  │  →   │ Dish_sup_bi_do_kem   │  →   │ thẻ món trong  │
│ kem sữa.png  │      │ thành Sprite  │      │   .dishSprite  ←     │      │ bếp, sổ công   │
│ 512×512      │      │ Single        │      │ Item_sup_bi_do_kem   │      │ thức, kho, chợ,│
│ nền trong    │      │ pivot Center  │      │   .icon        ←     │      │ đơn dân làng   │
└──────────────┘      └───────────────┘      └──────────────────────┘      └────────────────┘
```

**Một file PNG được gán vào ĐÚNG HAI chỗ** (bảng tra đầy đủ ở `05_BANG_GHEP_FILE_VAO_MON.csv`):

| Gán vào | Tên trường | Dùng ở màn nào |
|---|---|---|
| `Dish_<id>.asset` | `dishSprite` | Bếp: thẻ "khách muốn món này" · sổ công thức · danh sách chọn món |
| `Item_<id>.asset` | `icon` | Kho (tab **Món ăn**) · quầy chợ · đơn hàng dân làng |

Nghĩa là: **vẽ 1 file, dùng lại ở 5 chỗ**. Vì vậy icon không được "dính" bối cảnh của riêng một
màn nào — không nền bàn bếp, không nền quầy chợ, không khung viền. Nền phải trong suốt để game
đặt nó lên bất kỳ nền nào cũng vừa.

---

## 2. Icon hiện TO BAO NHIÊU trong game (số đo thật, lấy từ code)

| Chỗ hiện | Kích thước thật trên màn |
|---|---|
| Dòng chọn món trong sổ công thức | **38 × 38 px** |
| Thẻ đơn hàng của khách (bếp) | **48 × 48 px** |
| Ô chi tiết món đang nấu | **52 × 52 px** |
| Ô kho / ô quầy chợ | lớn hơn, nhưng vẫn dưới ~120 px |

**Đây là con số quan trọng nhất trong cả hồ sơ.** Bạn vẽ ở 512×512 để nét sạch và để sau này
game lên màn hình lớn vẫn dùng được, **nhưng người chơi chủ yếu nhìn nó ở cỡ móng tay**.

Hệ quả thực tế khi vẽ:
- Chi tiết nhỏ hơn ~1/12 chiều rộng khung sẽ **biến mất hoàn toàn** khi thu nhỏ. Vẽ 30 hạt tiêu
  li ti là công vẽ đổ sông — thà vẽ 6 hạt to rõ.
- **Silhouette (bóng đổ hình khối) quyết định tất cả.** Người chơi phân biệt "bát canh" với
  "đĩa xào" bằng dáng ngoài trước khi kịp nhìn màu. Bát thì phải rõ là bát (thành cao, miệng
  tròn), đĩa phải rõ là đĩa (dẹt, vành rộng), ly phải rõ là ly (cao, trong).
- **Màu nguyên liệu chính phải chiếm mảng lớn.** Ví dụ "Canh bí đỏ sườn non" ở 38px thì người
  chơi chỉ thấy được "một bát màu cam có mấy khối nâu" — thế là đủ, và đúng. Nếu bí đỏ bị vùi
  dưới nước canh thì ở 38px nó thành "một bát màu nâu", trùng với 4 món khác.
- Tương phản giữa thức ăn và vật đựng phải đủ mạnh. Súp kem trắng trong bát trắng = mất hình.

---

## 3. Bốn điều cấm — và điều gì hỏng nếu vi phạm

### ❌ Không chữ, không số, không logo
Game hiển thị tên món bằng **font chữ tiếng Việt render lúc chạy** (TextMeshPro), ngay cạnh icon.
Nếu chữ được vẽ chết vào ảnh thì:
- chữ trong ảnh và chữ của game **chồng lên nhau**;
- ở cỡ 38px chữ trong ảnh biến thành một vệt bẩn;
- đổi tên món hoặc dịch sang tiếng khác là phải vẽ lại toàn bộ ảnh.

### ❌ Không bóng đổ rời khỏi bát/đĩa
Game xếp icon vào lưới ô sát nhau (kho, chợ). Bóng đổ vẽ chết vào ảnh sẽ **tràn sang ô bên cạnh**
và chồng lên bóng của icon kế bên → nhìn như ảnh bị bẩn. Bóng trong game do code vẽ, để mọi icon
có cùng một hướng sáng.
→ **Được phép:** một vệt bóng tiếp xúc rất nhẹ **sát đáy** bát/đĩa, nằm gọn trong silhouette.

### ❌ Không nền (bàn, khăn, cỏ, hoa văn, khung viền)
Cùng một icon phải đặt được lên nền giấy của sổ công thức, nền gỗ của kho, và nền vải của quầy
chợ. Có nền là hỏng cả ba. Nền phải **trong suốt 100%**.
→ Nếu công cụ vẽ không xuất được alpha thì xuất nền **magenta #FF00FF đồng nhất**, bên dev tách.
  (Đã có kinh nghiệm: nền magenta tách sạch hơn nền trắng rất nhiều, vì không màu thức ăn nào
  trùng magenta.)

### ❌ Không tự đổi tên file
Tên file là **khoá tra cứu**. Bên dev có bảng `05_BANG_GHEP_FILE_VAO_MON.csv` map
`Súp bí đỏ kem sữa.png` → `Dish_sup_bi_do_kem.asset`. Đúng tên = gán tự động cả 20 món trong một
lần chạy. Sai tên (thiếu dấu, thêm `_v2`, thêm `@2x`, đổi hoa/thường) = phải mở từng file so bằng
mắt rồi gán tay, và rất dễ gán nhầm món này sang món kia.

**Riêng đợt vẽ lại 18 icon cũ** (`03_...`): giữ nguyên tên file cũ còn quan trọng hơn nữa. Unity
gắn mỗi file với một mã GUID nằm trong file `.meta` đi kèm. **Ghi đè đúng tên = GUID giữ nguyên =
mọi chỗ đang dùng icon đó tự cập nhật, không phải nối lại gì.** Đổi tên = GUID mới = mọi chỗ đang
dùng icon cũ thành **ô trống**, bên dev phải nối lại tay từng chỗ.
→ Và vì thế: **đội vẽ không đụng vào file `.meta`.** Không sửa, không xoá, không tạo mới. Chỉ giao
  file `.png`.

---

## 4. Vì sao lại là 512×512, pivot Center, PPU 100

- **512×512**: bộ icon cũ là 371×426 — đủ cho màn hình hiện tại nhưng bắt đầu mờ trên màn lớn.
  512 là bước nhảy an toàn, vẫn nhẹ file. Khung **vuông** để game co giãn không méo:
  code đặt icon vào ô vuông và bật `preserveAspect`, ảnh không vuông sẽ tự thu nhỏ lại và
  **trông bé hơn** các icon khác trong cùng lưới.
- **Vùng thức ăn chiếm ~380–430 px** trong khung 512: chừa lề để khi game bo góc ô hoặc thêm
  viền chọn thì không cắt vào món.
- **pivot Center**: icon món ăn là UI phẳng, game canh giữa ô. (Khác với cây cối ngoài nông trại
  dùng pivot Bottom-Center vì chúng "đứng trên đất".)
- **PPU 100**: chuẩn chung của dự án. Đội vẽ không cần cài — **bên dev tự cài khi import**.

---

## 5. Mỗi món có "công thức" riêng — vẽ đúng nguyên liệu là một phần của gameplay

Trong game, mỗi món cần một tập nguyên liệu cố định. Người chơi phải **nhìn icon để đoán mình cần
farm gì**. Cột "Nguyên liệu phải thấy" trong `05_BANG_GHEP_FILE_VAO_MON.csv` chính là tập đó.

Ví dụ đọc bảng:

| Tên file | dishId | Nguyên liệu phải thấy | Gia vị |
|---|---|---|---|
| `Canh bí đỏ sườn non.png` | `sup_bi_do_suon_non` | Bí Đỏ, Thịt Heo | Muối, Tiêu |

→ Trong icon **bắt buộc nhìn ra bí đỏ và thịt heo**. Gia vị (muối, tiêu) **không cần vẽ rõ** —
chỉ là lấm tấm cho đẹp, có cũng được không có cũng được. Đừng vẽ lọ muối, hũ tiêu bên cạnh bát.

Ngược lại: **đừng thêm nguyên liệu không có trong bảng.** Vẽ thêm cà rốt vào "Canh bí đỏ sườn non"
sẽ khiến người chơi đi farm cà rốt rồi nấu hoài không ra món.

---

## 6. Quy trình sau khi giao — để đội vẽ biết chuyện gì xảy ra tiếp

1. Đội vẽ bỏ PNG vào `GIAO_FILE_TAI_DAY/`.
2. Bên dev **soát tự động**: đúng tên chưa, đúng 512×512 chưa, nền có thật sự trong suốt chưa,
   mép có sót viền màu nền không, có bóng đổ tràn ra ngoài không.
3. File nào **rớt** thì dev báo lại **kèm ảnh chỗ sai**, không bắt vẽ lại cả bộ.
4. File nào **đạt** thì dev chuyển vào `Assets/Assetsgame/Món ăn/`, cài import setting, gán vào
   `dishSprite` + `icon`, chụp màn hình trong game gửi lại để đội vẽ thấy thành quả.

Vẽ được lô nào giao lô đó. Không cần đủ 20 mới gửi.
