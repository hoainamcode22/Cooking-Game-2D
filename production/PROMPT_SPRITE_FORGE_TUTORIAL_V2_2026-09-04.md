# 🎨 PROMPT ĐỘI VẼ — GÓI TUTORIAL V2 (04/09/2026)

> Gửi cho: **agent-sprite-forge** (qua GPT của Sếp).
> Người ra đề: Tech Lead · Duyệt: Sếp Huy · Vòng 12.
> **Chỉ vẽ đúng 2 gói dưới. Không vẽ thêm gì ngoài danh sách.**

---

## ⛔ RANH GIỚI CÔNG VIỆC — ĐỌC TRƯỚC TIÊN (lệnh Sếp 04/09)

**Đội vẽ CHỈ VẼ. Không chèn bất kỳ logic nào.**

Cụ thể, đội vẽ **KHÔNG** làm những việc sau — đây là phần của đội code, chèn vào là hỏng khâu:

| ❌ Không làm | Vì sao |
|---|---|
| Không viết/sửa file `.cs`, `.json`, `.asset`, `.prefab`, `.unity`, `.meta` | Code và scene do đội Dev sở hữu; đội vẽ ghi vào là mất công cả hai bên |
| Không tự đặt animation/timing/fps vào file | fps do code quyết (`talkFps`, `waveFps`...), vẽ đủ 12 frame là xong |
| Không ghép sprite-sheet, không tự cắt ô | Giao **file PNG rời từng frame**. Code tự nạp theo tên |
| Không tự đổi tên file cho "gọn hơn" | Tên file là **hợp đồng** — code tìm đúng tên đó, đổi 1 ký tự là không tìm thấy |
| Không bake khói/lửa/ánh sáng/bóng vào frame | Code phun hiệu ứng lúc chạy để đồng bộ theo giờ trong ngày |
| Không thêm file phụ (`_v2`, `_final`, `@2x`, `_single`, ảnh preview...) | Thư mục thừa file làm tool nạp sai |
| Không import trực tiếp vào `Assets/` của Unity | Chỉ thả vào thư mục art-handoff. Lead có tool 1 nút để nạp vào đúng chỗ |

**Đội vẽ CHỈ cần làm đúng 3 việc:**
1. Vẽ đủ số file trong danh sách, đúng tên, đúng kích thước canvas.
2. Thả vào đúng thư mục art-handoff ghi ở cuối mỗi gói.
3. Nhắn Lead một câu: *"đã giao gói A"* / *"đã giao gói B"*.

Phần còn lại — nạp vào game, gắn vào nhân vật, chỉnh nhịp, nối hiệu ứng — **Lead đã dựng sẵn khung sườn và có tool 1 nút**, đội vẽ không phải đụng tới.

---

## 🔒 LUẬT ART STUDIO — BẮT BUỘC ĐỌC TRƯỚC KHI VẼ

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ
   trên BẤT KỲ asset nào. Text do game render bằng TMP. Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ
   dưới chân object — bóng do game tự vẽ để đồng nhất theo giờ trong ngày.
3. ✅ **Meta Unity chuẩn**: spriteMode Single từng file · pivot Bottom-Center cho object đứng đất.
4. ✅ **Frame animation**: mọi frame cùng hướng = **CÙNG kích thước canvas**, thân đứng yên cùng vị trí;
   frame 01 = tư thế nghỉ; **KHÔNG khói/hiệu ứng bake vào frame** (code phun runtime).
5. ✅ **Style chuẩn**: theo bộ Export_Train_UI_Package (burgundy `#8E1F3B` + đồng vàng `#D9A441`,
   gỗ nâu ấm, outline nâu đậm cartoon, dễ thương cho phụ nữ & trẻ em).
6. ✅ Giao đúng **TÊN FILE + THƯ MỤC** đặt trong prompt, không thêm file phụ (`_single`, `@2x` tự ý...).

---

# GÓI A — NPC HƯỚNG DẪN VIÊN TUTORIAL ⭐ ƯU TIÊN 1

## A.0 — Nhân vật này là ai

Một **cô gái nông dân trẻ, vui vẻ, thân thiện** — vai "chị hướng dẫn" dắt người chơi qua 31 bước
đầu game. Cô đứng **bên TRÁI khung hội thoại**, nói chuyện với người chơi suốt tutorial.

**Tham chiếu tông & tinh thần:** cô gái đội nón rơm ôm giỏ rau trong màn hình loading của game
(nền kem, viền nâu, mắt to, má hồng). **KHÔNG vẽ lại y hệt cô đó** — cô kia đã nhận vai *Shipper
giỏ hoa* trong game, trùng vai sẽ làm người chơi lẫn. Hãy vẽ **một nhân vật MỚI, cùng vũ trụ**:

- Nữ, khoảng 20 tuổi, **tóc nâu hạt dẻ buộc đuôi ngựa cao** (khác búi/xoã của shipper).
- **Nón rơm vành nhỏ hất ra sau lưng** (đeo dây cổ) — để lộ trọn khuôn mặt, vì mặt là thứ người chơi nhìn nhiều nhất.
- **Yếm nâu đất + áo sơ mi kẻ sọc đỏ burgundy `#8E1F3B` xắn tay**, khăn quàng cổ vàng đồng `#D9A441`.
- Mắt to tròn, má hồng, miệng cười hở răng. Outline nâu đậm cartoon.
- **KHÔNG cầm đồ vật gì trong tay** — hai tay phải rảnh để quơ/chỉ trỏ.

## A.1 — Khung hình & kỹ thuật (đọc kỹ, sai là phải vẽ lại cả bộ)

| Mục | Giá trị |
|---|---|
| Canvas mỗi frame | **512 × 640 px**, dọc. **CẢ 36 FRAME PHẢI Y HỆT KÍCH THƯỚC NÀY** |
| Nền | Trong suốt 100% (alpha 0), không bóng đổ |
| Khung hình nhân vật | **Nửa người — từ đỉnh đầu tới ngang hông**. Chừa lề trên 24px, lề dưới 0px (cắt ngang hông) |
| Vị trí thân | **Trục dọc thân người ĐỨNG YÊN TUYỆT ĐỐI ở giữa canvas trong cả 36 frame.** Chỉ tay/đầu/mắt/miệng động. Thân xê dịch = nhân vật "trượt" khi chạy animation |
| Hướng nhìn | Hơi chếch **3/4 sang PHẢI** (vì cô đứng bên trái, nói với card ở bên phải) |
| Số màu | Giữ bảng màu hạn chế, ăn tông game |

## A.2 — 3 clip × 12 frame = 36 file

### Clip 1 — `idle_talk` (dùng NHIỀU NHẤT, ~80% thời lượng tutorial)
Cô **đang nói chuyện**: miệng mấp máy, một tay khua nhẹ minh hoạ, thân thở lên xuống rất nhẹ.

- `talk_01` = **tư thế nghỉ chuẩn** (miệng khép hờ, tay xuôi tự nhiên) ← *frame gốc, các frame khác lệch từ đây*
- `talk_02` → `talk_06`: miệng mở dần rồi khép (chu kỳ nói 1), tay phải nâng lên ngang ngực khua nhẹ
- `talk_07` → `talk_12`: chu kỳ nói 2, tay hạ về, **frame 12 phải nối mượt ngược về frame 01** (loop kín)
- Biên độ tay: **nhỏ thôi** — đây là nói chuyện bình thường, không phải diễn kịch.

### Clip 2 — `wave` (chào ở bước đầu + ăn mừng khi xong 1 chặng)
Cô **vẫy tay chào hào hứng**, mặt rạng rỡ hơn clip 1.

- `wave_01` = tư thế nghỉ (giống `talk_01` để chuyển clip không giật)
- `wave_02` → `wave_04`: nâng tay phải lên cao quá đầu
- `wave_05` → `wave_09`: **vẫy qua lại 2 nhịp** (trái–phải–trái–phải), thân nghiêng nhẹ theo
- `wave_10` → `wave_12`: hạ tay về tư thế nghỉ, **frame 12 ≈ frame 01**

### Clip 3 — `point` (chỉ vào thứ người chơi phải bấm)
Cô **chỉ tay sang PHẢI và hơi xuống** (hướng vào chỗ cần thao tác), mặt nghiêm túc–khích lệ.

- `point_01` = tư thế nghỉ
- `point_02` → `point_05`: đưa tay phải ra chỉ, ngón trỏ duỗi thẳng
- `point_06` → `point_12`: **giữ nguyên tư thế chỉ**, chỉ nhấn nhá rất nhẹ (ngón trỏ nhích 2–3px, thân thở)
  — vì code sẽ **dừng loop ở đây** trong lúc chờ người chơi bấm. Frame 06→12 phải nhìn được khi lặp lâu mà không khó chịu.

### Blink — 1 file duy nhất
- `blink.png`: **CHÍNH XÁC là `talk_01`, chỉ khác đúng một thứ — hai mắt nhắm** (nét cong xuống hình chữ U ngược).
  Mọi thứ khác (tay, thân, tóc, nón) **không được xê dịch một pixel nào**. Code chèn frame này ngẫu nhiên 3–6 giây/lần đè lên mọi clip.

## A.3 — Tên file & nơi giao (SAI TÊN = CODE KHÔNG TÌM THẤY)

Giao vào: **`production/art-handoff/2026-09-04_TutorialV2/A_NPC_Guide/`**

```
guide_talk_01.png  … guide_talk_12.png      (12 file)
guide_wave_01.png  … guide_wave_12.png      (12 file)
guide_point_01.png … guide_point_12.png     (12 file)
guide_blink.png                             (1 file)
                                    ─────── TỔNG 37 FILE
```

Số **luôn 2 chữ số** (`01`, không phải `1`). Tất cả `.png` alpha.

---

# GÓI B — VFX TUTORIAL (ƯU TIÊN 2 — vẽ sau khi xong gói A)

Tất cả là **sprite rời để code tự bắn hạt**, KHÔNG phải video, KHÔNG phải sprite-sheet ghép.
Nền trong suốt, không bóng đổ, tông sáng vui.

Giao vào: **`production/art-handoff/2026-09-04_TutorialV2/B_VFX_Tutorial/`**

| # | Tên file | Canvas | Vẽ gì |
|---|---|---|---|
| B1 | `tut_glow_ring.png` | 256×256 | **Vòng sáng bo tròn** viền vàng đồng `#D9A441`, tâm rỗng hoàn toàn trong suốt, mép ngoài loe mờ dần. Code phóng to–thu nhỏ vòng này quanh nút cần bấm |
| B2 | `tut_arrow_down.png` | 128×160 | **Mũi tên trỏ xuống** mập mạp bo góc, thân vàng đồng, outline nâu đậm. Code cho nó nảy lên xuống trên đầu mục tiêu |
| B3 | `tut_sparkle_01.png` … `tut_sparkle_04.png` | 64×64 mỗi file | **4 ngôi sao lấp lánh 4 cánh**, 4 kích cỡ khác nhau (to → nhỏ), màu: 1 trắng, 1 vàng đồng, 1 hồng nhạt, 1 xanh mint. Code rải ngẫu nhiên khi hoàn thành 1 bước |
| B4 | `tut_burst_ray.png` | 256×256 | **Chùm tia toả tròn từ tâm** (kiểu tia nắng), 12 tia, vàng nhạt trong suốt dần ra ngoài. Code xoay + phóng khi qua bước |
| B5 | `tut_dust_puff_01.png` … `tut_dust_puff_03.png` | 96×96 mỗi file | **3 cụm khói bụi tròn mềm** (3 hình dạng khác nhau), trắng ngà hơi ngả nâu. Code bắn ở chân khi có thao tác trên đất |

**Tổng gói B: 10 file.**

---

# ❌ NHỮNG THỨ KHÔNG VẼ TRONG VÒNG NÀY

Để tránh phí công — Lead đã kiểm, các thứ này **đã có sẵn trong dự án**:

- ❌ **Khung/card hội thoại** — dùng `panel_paper_cream.png` có sẵn (9-slice border 24), Sếp đã chốt.
- ❌ **Bàn tay chỉ (tap hint)** — đã có `tutorial_hand.png`.
- ❌ **Confetti / pháo hoa / flash lên cấp** — đã có bộ Lana Studio + `confetti_01..06.png` giao đợt 31/08.
- ❌ **Nhân vật popup lên cấp** — `char_01..04` đã đủ, Sếp chốt giữ chế độ 1 hình master.
- ❌ **Thợ xây w02/w03** — đã giao 03/09, Lead đang nối dây.

---

# ✅ CHECKLIST TỰ KIỂM TRƯỚC KHI GIAO (đội vẽ tự soát)

- [ ] Mở chồng `guide_talk_01` và `guide_talk_07` lên nhau → **thân người trùng khít**, chỉ tay/miệng lệch?
- [ ] Mở chồng `guide_blink` và `guide_talk_01` → **chỉ có mắt khác**, không lệch gì khác?
- [ ] `guide_talk_12` nối về `guide_talk_01` có giật không?
- [ ] Đủ **37 file gói A**, **10 file gói B**, đúng tên, đúng 2 chữ số?
- [ ] Zoom 400% kiểm: có sót **chữ/số/logo** nào không? có **nền trắng hoặc bóng đổ** sót lại không?
- [ ] Tất cả file gói A đúng **512×640**? (mở Properties xem, đừng tin mắt)

Xong hết mới nhắn Lead: **"đã giao gói A"**.
