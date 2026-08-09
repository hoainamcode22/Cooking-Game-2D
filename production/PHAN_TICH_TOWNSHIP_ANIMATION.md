# PHÂN TÍCH TOWNSHIP — CÁCH HỌ LÀM GAME & LÀM HIỆU ỨNG

> Nguồn: video 38,7 giây · 826×576 · 30fps · bóc 77 frame
> Đối chiếu với hiện trạng `Cooking-Game-2D`

---

## PHẦN 1 — HAI CHẾ ĐỘ ĐẶT, HAI CÂU CHỮ KHÁC NHAU

Đây là chi tiết dễ bỏ sót nhất nhưng ảnh hưởng lớn tới cảm nhận.

| Chế độ | Chữ trên thanh | Khi nào |
|---|---|---|
| **Di chuyển công trình có sẵn** | **`KOSTENLOS PLATZIEREN`** = "Đặt MIỄN PHÍ" | Vào edit mode, nhấc công trình đã đặt |
| **Mua từ shop** | **`KAUFEN FÜR 🪙 10`** = "MUA VỚI GIÁ 10" | Đặt công trình mới |

Cùng một thanh UI, cùng 3 nút `✕ ↻ ✓`, **chỉ đổi dòng chữ**. Người chơi biết ngay lần này có mất tiền hay không.

**Game bạn hiện chưa phân biệt.** `PlacementManager` có 2 nhánh `StartPlacingNewObject` và `StartEditBuilding` nhưng thanh giá chỉ hiện một kiểu. Sửa rẻ: khi `currentlyEditingBuilding != null` thì đổi chữ thành "ĐẶT MIỄN PHÍ", ẩn số tiền.

---

## PHẦN 2 — VÒNG ĐỜI CÔNG TRÌNH, ĐỦ 4 GIAI ĐOẠN

Bóc được trọn vẹn trong khoảng frame 36 → 60 (~12 giây thực).

### A. Ghost đặt (f_036, f_040)
- Công trình hiện **đầy đủ hình, hơi trong suốt**, KHÔNG phải khối xám
- Thảm xanh + **4 dấu góc chevron xanh** ôm chân
- Thanh `KAUFEN FÜR 🪙 30` + 3 nút

### B. Công trường đang xây (f_040 → f_070)
- Công trình thật **BIẾN MẤT**, thay bằng **khung gỗ giàn giáo màu nâu vàng**
- Bên trong khung có **3–4 công nhân áo xanh** đứng làm
- **Khói bụi trắng** phun lên từ chân công trường
- Nổi trên đầu: **icon mũ bảo hộ vàng** trong khung trắng bo góc
- Icon này **tồn tại suốt thời gian xây** — đó là ngôn ngữ "chỗ này đang thi công"

### C. Khánh thành (f_057 — khoảnh khắc quan trọng nhất)
Đây là thứ làm nên cảm giác "đã tay":

1. Công trình hiện ra **bọc trong hộp quà trắng**, có **hoa nơ ruy băng hồng/xanh** gắn các mặt
2. **Bóng bay đỏ và xanh** bay lên, có trôi ngang nhẹ
3. **Icon người + dấu cộng** bay lên
4. **1 giây sau (f_059)**: hộp mở dần, số **"+10"** bay lên kèm icon người và icon mũ bảo hộ

### D. Hoạt động (f_050, f_070)
- Icon sản phẩm nổi trên đầu: **bình sữa** ở xưởng sữa, **ngôi sao** ở nhà thưởng
- Đồng hồ sản xuất: **`9M47SEK`** dạng chữ, nền tối bo góc, có icon xu bên trái

---

## PHẦN 3 — NGÔN NGỮ ICON NỔI (giá trị nhất để học)

Township không dùng chữ để nói trạng thái. Họ dùng **một bộ icon nhất quán**, ai cũng đoán được kể cả trẻ chưa đọc chữ:

| Icon | Nghĩa |
|---|---|
| 🟡 **Mũ bảo hộ vàng** | Đang thi công |
| 🥛 **Bình sữa / ổ bánh / phô mai** | Sản phẩm đã xong, chạm để thu |
| ⭐ **Ngôi sao vàng** | Thưởng XP đang chờ |
| 👥 **Người + dấu cộng** | Dân số vừa tăng |
| 🎀 **Nơ quà** | Có quà chờ nhận |
| 💰 **Bảng xu trên đất trống** | Ô đất đang bán, mua mở rộng |
| **Chữ "Z"** | Công trình **đứng không**, thiếu nguyên liệu |
| 🔴 **Số đỏ trên nút** | Có việc cần làm |

**Chữ "Z" là chi tiết tinh tế nhất.** Nhà máy hết nguyên liệu thì "ngủ" — người chơi thấy Z là biết phải nạp hàng. Không cần một dòng chữ nào.

Bộ icon này chính là cách họ giữ người chơi nhỏ tuổi: **màn hình luôn có 5–8 icon nổi**, mỗi cái là một lời mời chạm.

---

## PHẦN 4 — HỌ LÀM HIỆU ỨNG BẰNG GÌ (kỹ thuật)

Quan sát kỹ thì **KHÔNG có skeletal animation** ở lớp công trình. Toàn bộ là **sprite + tween transform**. Đây là tin tốt cho bạn — làm được hết bằng code, không cần Spine.

### 4.1 Icon nổi trên đầu
```
Vòng lặp vô hạn:
  position.y  : bob lên xuống ±6px, chu kỳ ~1.2s, ease sin
  scale       : 1.0 ↔ 1.06, lệch pha với bob
```
Chỉ 2 tween. Rẻ, mà làm màn hình "sống".

### 4.2 Bóng bay khánh thành
```
Spawn 4–6 quả, màu ngẫu nhiên (đỏ/vàng/xanh)
  position.y  : +250px trong 2.5s, ease-out
  position.x  : dao động sin biên độ 15px  ← quan trọng, không có thì trông như thang máy
  alpha       : 1 → 0 ở 30% cuối
  scale       : nhỏ dần theo độ cao (giả phối cảnh)
```

### 4.3 Số thưởng bay lên
```
  position.y  : +90px trong 1.2s, ease-out
  scale       : 0 → 1.25 → 1.0  (overshoot, ease-out-back)
  alpha       : giữ 1 trong 60%, rồi tắt
```
Cái `overshoot 1.25` là thứ tạo cảm giác "nảy". Bỏ nó đi là mất hết vị.

### 4.4 Hộp quà khánh thành
```
  scale : 0 → 1.15 → 1.0   (ease-out-back, 0.4s)
  giữ 1.2s
  scale : 1.0 → 1.3 + alpha 1 → 0  (0.5s)  ← hộp "nở ra rồi tan"
```

### 4.5 Khói bụi công trường
`ParticleSystem` cấu hình rất nhẹ:
```
emission ~4 hạt/giây · lifetime 1.5s
vận tốc lên 30px/s · scale 0.6 → 1.4 theo lifetime
alpha 0.5 → 0 · màu trắng ngà
```
Ít hạt, chậm, mờ. Không phải khói dày.

### 4.6 Cây cối lay
Nhìn frame liền nhau thấy tán cây **nghiêng qua lại rất nhẹ** (~2°), lệch pha mỗi cây. Làm bằng `rotation.z = sin(time + offsetRiêng) * 2°`, pivot ở gốc cây. Một script 5 dòng gắn lên mọi cây.

### 4.7 Mặt nước
Nước có gợn — hoặc sprite animation vài frame, hoặc UV scroll. Không phân biệt được ở độ phân giải này.

---

## PHẦN 5 — THIẾT KẾ CHẶN ĐƯỜNG (f_031)

Đây là bài học về UX, không phải animation.

Người chơi mở shop nhà ở, bấm mua → popup:

```
      NICHT GENUG LEUTE          ("Không đủ dân")
   ERHÖHE DIE EINWOHNERGRENZE    ("Nâng giới hạn dân số")
        [hình toà thị chính]
          [ ANZEIGEN ]           ("Xem ngay")
```

**Ba điều họ làm đúng:**

1. **Không chỉ nói "không được"** — nói rõ *thiếu cái gì*
2. **Có hình** công trình cần xây, không chỉ chữ
3. **Có nút dẫn đi luôn** — bấm `ANZEIGEN` là camera lia tới chỗ cần làm

Người chơi **không bao giờ bị bỏ lại trong trạng thái bế tắc**. Mọi lần bị chặn đều biến thành một lời chỉ đường.

Trong shop cũng thấy điều tương tự: `Berghütte 2/3` (đã có 2, tối đa 3), `Farmhaus — STADTSTUFE 10 BENÖTIGT` (cần cấp 10). Giới hạn hiện **ngay trên thẻ hàng**, không đợi bấm mới báo.

---

## PHẦN 6 — ĐỐI CHIẾU VỚI GAME CỦA BẠN

### Đã có, khớp đúng
| Township | Game bạn |
|---|---|
| Ghost + thảm + 4 chevron | ✅ `PlacementGhostVisualController` |
| Thanh 3 nút ✕ ↻ ✓ + giá | ✅ vừa làm xong |
| Giàn giáo + công nhân + khói | ✅ `ConstructionSiteVisuals` |
| Icon mũ bảo hộ khi đang xây | ✅ ô `HardHatDone` trong art kit |
| Hộp quà + ruy băng + bóng bay | ✅ `ConstructionCompleteFX` |
| Đồng hồ đếm ngược nổi | ✅ `ConstructionSiteUI` |
| Nút tăng tốc | ✅ có, công thức khớp video |

### Chưa có — xếp theo mức đáng làm

| # | Thiếu | Chi phí | Vì sao đáng |
|---|---|---|---|
| 1 | **Đổi chữ "ĐẶT MIỄN PHÍ" khi di chuyển** | ~10 dòng | Rẻ nhất, hiệu quả rõ nhất |
| 2 | **Icon nổi "sản phẩm đã xong"** (bình sữa/ổ bánh) | 1 component | Đây là thứ khiến màn hình "sống". Bạn đã có sản phẩm nhưng chưa có icon mời chạm |
| 3 | **Chữ "Z" khi máy đứng không** | 1 sprite + 1 điều kiện | Chi tiết nhỏ, cảm giác chuyên nghiệp lớn |
| 4 | **Popup chặn đường có nút dẫn đi** | vừa | Chống bế tắc — quan trọng với trẻ em |
| 5 | **Giới hạn hiện sẵn trên thẻ shop** (`2/3`, `Cần cấp 10`) | vừa | Người chơi không mất công bấm rồi mới biết |
| 6 | **Cây lay nhẹ** | 5 dòng | Bạn có `EnvironmentSway` trên 34 object — kiểm xem có chạy không |
| 7 | **Số thưởng bay lên có overshoot** | nhỏ | Bạn có `CoinFlyFX`, kiểm xem có `ease-out-back` chưa |

---

## PHẦN 7 — BA KẾT LUẬN

**1. Không cần Spine.** Toàn bộ hiệu ứng trong video làm được bằng sprite + tween transform + ParticleSystem nhẹ. Kết luận này khớp với khuyến nghị ban đầu: học Unity 2D Animation là đủ.

**2. Sức mạnh nằm ở SỐ LƯỢNG icon nổi, không phải độ phức tạp từng cái.** Mỗi hiệu ứng chỉ 2–3 tween. Nhưng màn hình lúc nào cũng có 5–8 icon đang bob. Đó là cái làm game "đắt tiền".

**3. Mọi lần chặn đường đều kèm lối ra.** Không có popup nào chỉ nói "không được". Luôn có hình + nút dẫn đi. Đây là điều quan trọng nhất với đối tượng phụ nữ và trẻ em.
