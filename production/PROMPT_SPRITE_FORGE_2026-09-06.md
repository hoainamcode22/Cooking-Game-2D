# 🎨 PROMPT ĐỘI VẼ — 06/09/2026

> Người ra đề: Tech Lead · Duyệt: Sếp Huy
> **Thư mục giao hàng:** `production/art-handoff/2026-09-06_Decor4_Rao2Lop/`
> **Tổng: 2 gói · 22 file PNG.** Gói A ưu tiên 1.

---

## ⛔ RANH GIỚI CÔNG VIỆC
**Đội vẽ CHỈ VẼ. Không chèn logic.**

| ❌ Không làm | Vì sao |
|---|---|
| Sửa `.cs` `.asset` `.prefab` `.unity` `.meta` | Code & scene do Dev sở hữu |
| Ghép sprite-sheet / tự cắt ô | Giao **PNG rời từng file** |
| Đổi tên file hoặc tên thư mục | Tên là **HỢP ĐỒNG** — code tìm đúng tên đó |
| Đổi kích thước canvas giữa các stage | Lệch 1px = vật "nhảy" khi đổi stage |
| Thêm file phụ (`_v2` `_final` `@2x`) | Tool nạp tự lọc, giao thừa là phí công |
| Import thẳng vào `Assets/` | Chỉ thả vào `art-handoff` |

## 🔒 LUẬT ART STUDIO (bắt buộc — `production/ART_RULES_STUDIO.md`)
1. ❌ **KHÔNG TEXT** — không chữ, số, logo, label. Có biển → vẽ **BIỂN TRỐNG**.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ** — alpha trong suốt 100%. **Tuyệt đối không nền magenta `#FF00FF`**.
3. ✅ spriteMode Single từng file · pivot **Bottom-Center** cho vật đứng đất.
4. ✅ Outline **nâu ấm sẫm, KHÔNG ĐEN** — dải hợp lệ `#442510` → `#654129` (hue 15–46), dày 1.5–2.5% cạnh dài.
5. ✅ Hướng sáng **trên–trái**. Style dễ thương cho phụ nữ & trẻ em.
6. ✅ Chi tiết palette: `production/art-handoff/STYLE_CONTRACT.md`.

---

> ## ⚠️ CẢNH BÁO TRÙNG HÌNH (thêm 06/09 vòng 4, BẮT BUỘC ĐỌC)
> Trong game **ĐÃ CÓ SẴN** một con heo hồng đội vòng hoa ngồi trên bệ đá: đó là món
> **id 9 "Heo Vui Vẻ"** (slug `meovuive`, đã đủ 5 stage).
> Món **id 8 "Heo Thần Tài"** trong gói A dưới đây **cũng là heo**, nên PHẢI vẽ KHÁC HẲN
> để người chơi không nhầm hai món. Hướng phân biệt bắt buộc:
> `meovuive` = heo hồng dễ thương, vòng hoa cúc trắng, bệ đá xám vuông, tông pastel.
> `heothantai` = heo THẦN TÀI phong cách may mắn: heo vàng ánh kim hoặc hồng đậm, đeo
> vòng cổ đồng tiền vàng, ngồi trên bệ tròn đỏ burgundy `#8E1F3B` viền đồng `#D9A441`,
> có thể thêm thỏi vàng nhỏ dưới chân. KHÔNG dùng vòng hoa cúc trắng, KHÔNG dùng bệ đá xám.
> Ảnh gốc để đối chiếu tránh vẽ trùng:
> `Assets/Art/Decor/Stages/meovuive/stage_3.png` (đây là món ĐÃ CÓ, đừng vẽ giống nó).

# GÓI A — 4 BỘ DECOR 5 STAGE ⭐ ƯU TIÊN 1 · 20 file

## Chuyện đã xảy ra
15/19 món decor có đủ 5 stage xây dựng (mua → thấy công trình lớn dần → pháo hoa → hoàn thiện).
**4 món này KHÔNG có art stage nào** ⇒ người chơi bấm mua là vật **hiện ra nguyên hình ngay lập tức**,
không có cảm giác xây dựng — hỏng trải nghiệm so với 15 món kia.

## Cần vẽ

| Thư mục (slug) | itemID | Tên hiển thị | File cần |
|---|---|---|---|
| `banghieu/` | 3 | Bảng Hiệu | `stage_1.png` … `stage_5.png` |
| `ghehoa/` | 7 | Ghế Hoa | `stage_1.png` … `stage_5.png` |
| `heothantai/` | 8 | Heo Thần Tài | `stage_1.png` … `stage_5.png` |
| `vitvuive/` | 12 | Vịt Vui Vẻ | `stage_1.png` … `stage_5.png` |

Đường dẫn giao: `production/art-handoff/2026-09-06_Decor4_Rao2Lop/<slug>/stage_N.png`

## Ý nghĩa 5 stage (BẮT BUỘC đúng thứ tự — code đọc theo số)

| Stage | Nội dung | Ghi chú |
|---|---|---|
| **stage_1** | Móng / khung gỗ giàn giáo mới dựng, chưa thành hình | thấp nhất |
| **stage_2** | Đang xây dở — đã nhận ra hình dáng nhưng còn giàn giáo | |
| **stage_3** | ✅ **THÀNH PHẨM HOÀN THIỆN** — đây là hình người chơi thấy mãi về sau | **quan trọng nhất** |
| **stage_4** | **Hộp quà** che kín vật (nơ + giấy gói) — khoảnh khắc trước khi mở | hộp quà, KHÔNG phải vật |
| **stage_5** | Vật hoàn thiện + **pháo hoa/tia sáng ăn mừng** quanh nó | nền vẫn trong suốt |

⚠️ Thứ tự này **không trực giác** (3 = xong, 4 = hộp quà, 5 = ăn mừng) nhưng là hợp đồng với code — làm đúng y vậy.

## LUẬT KÍCH THƯỚC (quan trọng nhất gói này)
- **5 file trong CÙNG 1 slug phải CÙNG kích thước canvas, không lệch 1px.**
- Kích thước gợi ý: **480 × 480 px** (tham chiếu bộ đang chạy: `binhtuoihoa` 473×475, `gieng` 480×488, `xehoa` 480×446).
- **Chân vật phải nằm CÙNG một baseline (cùng toạ độ Y) ở cả 5 stage** — nếu lệch, vật sẽ giật nảy khi chuyển stage.
- Chiều cao vật **tăng dần** stage_1 → stage_3.

## Mô tả 4 món
- **Bảng Hiệu (`banghieu`)** — ⚠️ **ĐÍNH CHÍNH 06/09 (Sếp Huy chốt): vẽ KỆ GỖ 3 TẦNG ĐỰNG CHẬU CÂY, KHÔNG PHẢI BẢNG HIỆU.**
  Tên món trong game là "Bảng Hiệu" nhưng art đang chạy thật là kệ gỗ 3 tầng có chậu cây và hoa
  (`Assets/Assetsgame/bocaycoitrangtri/Assettrangtri/PuLbG-removebg-preview.png`, 409x610).
  **stage_3 (thành phẩm) PHẢI vẽ khớp với ảnh đó** để người chơi đã mua không thấy món đồ biến hình.
  Mở file gốc đó ra xem trước khi vẽ. 4 stage còn lại kể câu chuyện dựng cái kệ đó lên.
  Vẫn giữ luật: không chữ, không số, nếu có mặt phẳng giống biển thì để TRỐNG.
- **Ghế Hoa (`ghehoa`)** — ghế băng gỗ dài ngoài vườn, có chậu/giỏ hoa đặt cạnh hoặc leo trên lưng ghế.
- **Heo Thần Tài (`heothantai`)** — tượng heo tròn mũm mĩm màu hồng/vàng kiểu tượng sân vườn (vật trang trí bằng sứ, KHÔNG phải heo thật trong chuồng).
- **Vịt Vui Vẻ (`vitvuive`)** — tượng vịt vàng dễ thương kiểu sân vườn, có thể kèm bồn nước nhỏ.

> 📌 Tham chiếu style: mở `Assets/Art/Decor/Stages/binhtuoihoa/stage_1..5.png` và `gieng/stage_1..5.png`
> — 4 bộ mới phải nhìn như **cùng một người vẽ** với 2 bộ đó.

---

# GÓI B — TÁCH RÀO CHUỒNG 2 LỚP · 2 file

## Chuyện đã xảy ra
Con gia súc trong chuồng **bị hàng rào vẽ đè lên người**, trông như bị chôn dưới rào.
Dev đã vá tạm bằng code (giờ con vật luôn nổi trên rào) nhưng **không thể sửa triệt để**:
cả 4 chuồng dùng **1 file ảnh duy nhất** phủ cả 4 cạnh ⇒ Unity chỉ so sánh được nguyên khối,
không thể vừa cho rào-sau ở sau con vật vừa cho rào-trước ở trước nó.
**Bắt buộc tách art thành 2 lớp** thì mới đúng.

## File nguồn
`Assets/Assetsgame/Nhà/chuongmoigiasuc.png` — **500 × 500 px**

## Cần giao 2 file

| File | Nội dung |
|---|---|
| `chuongmoigiasuc_sau.png` | **CHỈ** phần rào phía SAU (2 cạnh trên-trái + trên-phải của hình thoi isometric — phần xa camera) |
| `chuongmoigiasuc_truoc.png` | **CHỈ** phần rào phía TRƯỚC (2 cạnh dưới-trái + dưới-phải — phần gần camera) |

Đường dẫn giao: `production/art-handoff/2026-09-06_Decor4_Rao2Lop/rao_2_lop/`

## LUẬT BẮT BUỘC
1. **Cả 2 file đúng 500 × 500 px** — y hệt file gốc. Pivot giữ nguyên.
2. **KHÔNG vẽ lại, KHÔNG chỉnh màu.** Chỉ **tách** pixel của file gốc ra 2 lớp.
3. Phần không thuộc lớp của mình để **alpha = 0 hoàn toàn** (trong suốt), KHÔNG tô nền, KHÔNG cắt bằng hình chữ nhật.
4. **Chồng 2 file lên nhau phải ra lại đúng 100% file gốc** — không thừa, không thiếu 1 pixel nào.
5. Đường cắt chạy qua **2 góc trái và phải của hình thoi** (điểm rộng nhất) — cọc rào nào bị đường cắt đi qua thì cho nguyên cọc đó vào lớp **TRƯỚC**.
6. Xuất PNG RGBA, không nén mất alpha.

## Nghiệm thu bằng số (Lead sẽ kiểm tự động)
- 2 file đều đúng 500×500
- `sau + truoc` chồng lên = khớp gốc, sai lệch pixel = 0
- Viền trắng ≤ 0.1%
- Không có pixel `#FF00FF`

---

## ✅ Chỉ cần 3 việc
1. Vẽ đủ & đúng tên · 2. Thả đúng thư mục · 3. Nhắn Lead *"đã giao gói A"* / *"đã giao gói B"*.
