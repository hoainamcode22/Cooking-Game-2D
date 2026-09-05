# 🎨 PROMPT TỔNG — GỬI ĐỘI VẼ MỘT LẦN (04/09/2026)

> Gộp **toàn bộ** art còn thiếu: gói Round13 chưa vẽ + gói Tutorial V3 mới.
> Người ra đề: Tech Lead · Duyệt: Sếp Huy · **File này THAY THẾ `PROMPT_SPRITE_FORGE_ROUND13_2026-09-04.md`** (đừng làm theo file cũ nữa).
> Thư mục giao hàng: `production/art-handoff/2026-09-04_MASTER/`

**Tổng: 5 gói · 24 file.** Ưu tiên theo thứ tự A → E.

---

## ⛔ RANH GIỚI CÔNG VIỆC (lệnh Sếp — nhắc lại lần cuối)

**Đội vẽ CHỈ VẼ. Không chèn logic.**

| ❌ Không làm | Vì sao |
|---|---|
| Sửa `.cs` `.asset` `.prefab` `.unity` `.meta` | Code & scene do Dev sở hữu |
| Tự đặt fps / timing vào file | fps do code quyết |
| Ghép sprite-sheet, tự cắt ô | Giao **PNG rời từng frame**. Cắt lưới là nguồn gốc lỗi đợt 1 |
| Đổi tên file "cho gọn" | Tên là **hợp đồng** — code tìm đúng tên đó |
| Bake khói/bóng/ánh sáng vào frame | Code phun runtime |
| Thêm file phụ (`_v2`, `_final`, `@2x`, sheet nguồn) | Tool nạp tự lọc bỏ, giao thừa là phí công |
| Import thẳng vào `Assets/` | Chỉ thả vào art-handoff |

**Chỉ cần 3 việc:** vẽ đủ & đúng tên → thả đúng thư mục → nhắn Lead *"đã giao gói X"*.

## 🔒 LUẬT ART STUDIO

1. ❌ **KHÔNG TEXT** (ngoại lệ duy nhất: hoạ tiết trên lá cờ Anh — đó là hoạ tiết cờ, không phải label).
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ** — alpha trong suốt 100%.
3. ✅ spriteMode Single từng file · pivot Bottom-Center cho vật đứng đất.
4. ✅ Frame animation: **cùng kích thước canvas**, thân đứng yên tuyệt đối, frame 01 = tư thế nghỉ.
5. ✅ Style: burgundy `#8E1F3B` + đồng vàng `#D9A441`, gỗ nâu ấm, outline nâu đậm cartoon.

---

# GÓI A — CỜ NGÔN NGỮ ⭐ ƯU TIÊN 1 · 2 file

Hai lá cờ trong màn Cài đặt **đang vẽ bằng code** (`SettingsPopupUI.cs:606-632`) — hình chữ nhật đỏ + sao, và hình xanh + chữ "EN". Rất thô.

📁 `A_Co_NgonNgu/`

| # | Tên file | Canvas | Vẽ gì |
|---|---|---|---|
| A1 | `flag_vn.png` | **96 × 64** | Cờ Việt Nam — nền đỏ `#DA251D`, sao vàng 5 cánh `#FFFF00` giữa. Bo góc ~6px, viền ngoài nâu đậm 2px. Nếp gấp vải rất nhẹ (gradient mờ) |
| A2 | `flag_en.png` | **96 × 64** | Cờ Anh (Union Jack) — nền `#012169`, chữ thập đỏ `#C8102E` viền trắng, dải chéo trắng-đỏ. **Cùng** bo góc, **cùng** độ dày viền, **cùng** độ nếp gấp như A1 |

⚠️ Hai lá nằm cạnh nhau trong một hàng — lệch kích thước/bo góc là lộ ngay.

---

# GÓI B — ICON GIA VỊ & RAU ⭐ ƯU TIÊN 2 · 3 file

Hiện nằm rải rác ở `Assets/Anh/` dạng ảnh AI tách nền, **phong cách lệch hẳn** bộ chuẩn.

### 📌 CHUẨN PHẢI BÁM
**`Assets/Assetsgame/hatgiong/SHOP/icons/`** (26 file: `seed_cabbage`, `seed_carrot`, `seed_chili`, `seed_corn`, `seed_mushroom`…). **Mở vài file đó xem TRƯỚC KHI VẼ** — phải cùng: độ dày outline, độ bão hoà, góc nhìn chếch 3/4, độ bo khối, nguồn sáng trên-trái.

📁 `B_Icon_GiaVi/`

| # | Tên file | Canvas | Vẽ gì |
|---|---|---|---|
| B1 | `ing_rau.png` | **256 × 256** | Bó rau xanh — vài cọng lá xanh mướt buộc lại, lá có gân, `#4CAF50` → `#8BC34A`, outline nâu đậm |
| B2 | `ing_nuoc_mam.png` | **256 × 256** | Chai nước mắm — chai thuỷ tinh **thấp mập**, nước nâu hổ phách `#8B4513`, nắp đỏ burgundy, **nhãn TRỐNG**, có ánh sáng phản chiếu thân chai |
| B3 | `ing_nuoc_tuong.png` | **256 × 256** | Chai nước tương — chai **cao thon** (khác rõ B2 để không lẫn), nước nâu đen `#2B1810`, nắp vàng đồng `#D9A441`, **nhãn TRỐNG**, cùng kiểu ánh sáng B2 |

⚠️ Vật thể chiếm ~80% khung, căn giữa, lề đều ~25px — để xếp cạnh 26 icon cũ không cái nào to nhỏ lệch.

---

# GÓI C — 2 NHÂN VẬT POPUP LÊN CẤP ⭐ ƯU TIÊN 3 · 2 file

Lead đo được:
```
char_01_master.png : 512 × 512   ✅
char_02_master.png : 512 × 512   ✅
char_03_master.png : 304 × 304   ❌ nhỏ hơn 40%
char_04_master.png : 305 × 305   ❌ nhỏ hơn 40%, kích thước LẺ
```
⇒ 2 nhân vật bên phải **mờ và nhỏ hơn hẳn** 2 bên trái.

📁 `C_Char_LenCap/`

| # | Tên file | Yêu cầu |
|---|---|---|
| C1 | `char_03_master.png` | **512 × 512**, **VẼ LẠI ở độ phân giải gốc** — KHÔNG phóng to ảnh 304px (phóng to = mờ nhoè, không sửa được gì) |
| C2 | `char_04_master.png` | **512 × 512**, cùng yêu cầu |

Giữ nguyên tạo hình, chỉ vẽ lại cho nét. Căn giữa, lề trên ~20px, chân chạm mép dưới — khớp `char_01`/`char_02`.

---

# GÓI D — SỬA MIỆNG NPC TUTORIAL ⭐ ƯU TIÊN 4 · 12 file

## Chuyện đã xảy ra — Lead nhận trách nhiệm

Ở nghiệm thu đợt 3, Lead phát hiện khẩu hình được **dán trên một mảng bầu dục lệch tông da**:
```
Quét ngang talk_05 tại y=320:
  x 248→284 : RGB(250,183,150)   ← da mặt
  x 290→314 : RGB(252,222,196)   ← NHẢY, sáng hơn +39(G) +46(B)
  x 320→344 : RGB(251,188,152)   ← về lại da mặt
```
Lead đã đánh giá *"ở cỡ hiển thị thật gần như vô hình, không đáng bắt vẽ lại"* và cho qua.
**Đánh giá đó SAI.** Sếp chơi thử và thấy ngay: *"vài frame miệng dính layout frame khác, dính khá nhiều, biến dạng khuôn mặt NPC luôn."* Xin lỗi đội vẽ vì phải quay lại.

## Việc cần làm — vẽ lại 12 frame `guide_talk_*`

📁 `D_NPC_Mieng_SuaLai/`

**GIỮ NGUYÊN 100%**: thân, tay, tóc, yếm, khăn, khung hình. Phần đó đã ĐÚNG và đã được nghiệm thu.
**CHỈ SỬA**: cách vẽ miệng.

| Sai lầm đợt 3 | Cách làm đúng |
|---|---|
| Vẽ khẩu hình lên một lớp riêng rồi **dán đè** lên mặt → mảng oval lệch màu | **Vẽ miệng TRỰC TIẾP lên khuôn mặt**, xoá sạch miệng cũ trước, hoà màu vào da xung quanh |
| Nét môi gốc **chưa xoá**, còn ló ra bên phải miệng mới → nhìn như 2 miệng | Xoá hẳn miệng cũ. Zoom 400% kiểm không còn vệt môi thừa nào |
| Miệng mới đặt **lệch trái** so với trục mũi | Căn miệng thẳng trục dọc với sống mũi |

### 4 khẩu hình trên 4 nhóm tư thế (giữ nguyên nhóm)

| Nhóm | Frame | Khẩu hình |
|---|---|---|
| N1 | `01`, `12` | **Miệng khép**, cười nhẹ (tư thế nghỉ) |
| N2 | `02`, `11` | **Hé mở nhỏ** — âm "m / b" |
| N3 | `03, 04, 09, 10` | **Mở vừa, môi ngang** — âm "a / e", thấy răng + lưỡi |
| N4 | `05, 06, 07, 08` | **Mở tròn** — âm "o / u" |

### Sửa thêm 1 lỗi còn nợ từ đợt 3
**Sợi tóc mái rủ má phải** hiện chỉ có ở frame 02–05, không có ở frame 01 → chu kỳ chạy
`1,2,3,3,4,4,4,4,3,3,2,1` làm nó **biến mất 2/12 giây mỗi vòng ⇒ nhấp nháy**.
→ Cho sợi tóc xuất hiện ở **cả 12 frame**, hoặc bỏ hẳn ở cả 12.

Giao: `guide_talk_01.png` … `guide_talk_12.png` (**512 × 640**, alpha 0).

---

# GÓI E — POPUP HƯỚNG DẪN TUTORIAL V3 ⭐ ƯU TIÊN 5 · 5 file

Popup hướng dẫn hiện tại (tấm giấy nâu 4 trang) **layout vỡ**: tấm nền vàng đè lên dòng chữ, và bị code xoay nghiêng. Lead đã vá phần code; giờ cần **art khung đẹp** để thay hẳn.

📁 `E_Tutorial_GuideBoard/`

| # | Tên file | Canvas | spriteBorder | Vẽ gì |
|---|---|---|---|---|
| E1 | `tut_board_frame.png` | **512 × 512** | **9-slice, border {72,72,72,72}** | **Khung bảng hướng dẫn** — giấy da kem `#F5E9D0`, viền gỗ nâu ấm dày ~40px, 4 góc bọc đồng vàng `#D9A441`. Ruột giữa **trơn hoàn toàn** (phần này sẽ bị co giãn). Đây là khung chính, phải đẹp nhất gói |
| E2 | `tut_board_ribbon.png` | **420 × 120** | **{60,0,60,0}** | **Dải ruy-băng tiêu đề** vắt ngang đỉnh khung — burgundy `#8E1F3B`, 2 đuôi cờ đuôi nheo 2 bên, viền vàng đồng mảnh. Giữa **để trống** cho TMP ghi chữ lên |
| E3 | `tut_slot_illustration.png` | **512 × 384** | **{40,40,40,40}** | **Ô đặt ảnh minh hoạ** — nền vàng kem nhạt `#F7E9C4` bo góc 24px, viền nâu mảnh 3px, hơi lõm vào (inner shadow rất nhẹ). Đây là thứ thay tấm nền vàng đang vỡ |
| E4 | `tut_step_dot_on.png` | **48 × 48** | — | **Chấm chỉ trang ĐANG XEM** — tròn đầy màu vàng đồng `#D9A441`, viền nâu đậm 3px, có highlight nhỏ góc trên-trái |
| E5 | `tut_step_dot_off.png` | **48 × 48** | — | **Chấm trang CHƯA XEM** — tròn rỗng, nền kem nhạt, cùng viền nâu 3px, **cùng đường kính ngoài E4** |

⚠️ **E1–E3 BẮT BUỘC là 9-slice**: vẽ sao cho khi kéo giãn phần ruột, viền và góc **không méo**. Kiểm bằng cách tưởng tượng cắt ảnh theo lưới border đã ghi — 4 góc phải là hoạ tiết trọn vẹn, 4 cạnh phải lặp được liền mạch.

---

# ❌ KHÔNG VẼ — Lead đã kiểm, ĐÃ CÓ SẴN

| Sếp/đội từng nêu | Thực tế |
|---|---|
| Nút **"Bắt đầu nào"** thiếu asset | ✅ có `Export_Kitchen_UI_Package/Sprites/btn_big_green.png` (border 48) |
| Nút **Tất cả / Dễ / Vừa / Khó** | ✅ có `tab_pill_on.png` + `tab_pill_off.png` (border 24) — Lead đã nối code vòng 13 |
| Khung **card hội thoại** tutorial | ✅ dùng `panel_paper_cream.png` (border 24) |
| Badge **NEW** | ✅ sinh sẵn ở `PopupSpriteFactory.cs:353` |
| **Bàn tay chỉ** tap-hint | ✅ có `tutorial_hand.png` |
| **Confetti / pháo hoa** | ✅ bộ Lana Studio + `confetti_01..06.png` |
| **VFX tutorial** (glow ring, mũi tên, sparkle, burst, dust) | ✅ gói B vòng 12 đã giao đủ 10 file, đã vào game |
| Animation dải item, khung Hồ sơ | Là **code**, không phải art |

---

# ✅ CHECKLIST TỰ KIỂM TRƯỚC KHI GIAO

**Gói A** — 2 lá cùng 96×64, cùng bo góc, cùng viền? Đặt cạnh nhau xem lệch không?
**Gói B** — mở cạnh 3–4 file `hatgiong/SHOP/icons/`: outline, bão hoà, góc nhìn **cùng một bộ**? Hai chai phân biệt được từ dáng? Nhãn trống?
**Gói C** — đúng 512×512, **vẽ lại** chứ không phóng to? (zoom 400% kiểm độ nét)
**Gói D** — zoom 400%: quanh miệng **KHÔNG còn mảng màu khác tông**? **KHÔNG còn** nét môi cũ ló ra? Miệng thẳng trục mũi? Chồng `talk_01` lên `talk_07` → thân trùng khít? Sợi tóc má phải có đủ ở **cả 12** frame?
**Gói E** — E1/E2/E3 cắt theo lưới border: 4 góc trọn vẹn, 4 cạnh lặp liền mạch? E4/E5 **cùng đường kính ngoài**?
**Tất cả** — nền alpha 0, không bóng đổ, không chữ/số/logo? Đúng **24 file**, đúng tên, đúng thư mục, **không file phụ**?

Xong hết mới nhắn Lead: **"đã giao gói A/B/C/D/E"** (báo theo từng gói, đừng chờ xong hết mới báo).
