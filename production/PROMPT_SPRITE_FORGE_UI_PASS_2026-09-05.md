# 🎨 PROMPT ĐỘI VẼ — UI PASS TUTORIAL (05/09/2026)

> Người ra đề: Tech Lead · Duyệt: Sếp Huy · Bối cảnh: `production/PLAN_UI_PASS_2026-09-05.md` (đã duyệt).
> **Thư mục giao hàng:** `production/art-handoff/2026-09-05_UI_PASS/`
> **Tổng: 2 gói · 41 file.** Ưu tiên A trước B. Chỉ 2 gói này — nút/close/gem/card/khung **KHÔNG cần vẽ** (project đã có, Dev đang gắn).

---

## ⛔ RANH GIỚI CÔNG VIỆC (lệnh Sếp)

**Đội vẽ CHỈ VẼ. Không chèn logic.**

| ❌ Không làm | Vì sao |
|---|---|
| Sửa `.cs` `.asset` `.prefab` `.unity` `.meta` | Code & scene do Dev sở hữu |
| Tự đặt fps / timing | fps do code quyết (talk hiện 6 fps) |
| Ghép sprite-sheet, tự cắt ô | Giao **PNG rời từng frame** |
| Đổi tên file | Tên là **hợp đồng** — code tìm đúng tên đó |
| Bake khói/bóng/ánh sáng vào frame | Code phun runtime |
| Thêm file phụ (`_v2`, `_final`, `@2x`, sheet nguồn) | Tool nạp tự lọc, giao thừa là phí công |
| Import thẳng vào `Assets/` | Chỉ thả vào art-handoff |

**Chỉ cần 3 việc:** vẽ đủ & đúng tên → thả đúng thư mục → nhắn Lead *"đã giao gói X"*.

## 🔒 LUẬT ART STUDIO (bắt buộc — `production/ART_RULES_STUDIO.md`)

1. ❌ **KHÔNG TEXT** — không chữ, số, logo, label trên bất kỳ asset nào.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ** — alpha trong suốt 100%. **Tuyệt đối không nền magenta `#FF00FF`** (đợt trước wave/point/blink giao nền magenta ⇒ nhân vật "nhảy" trong game).
3. ✅ spriteMode Single từng file · pivot Bottom-Center cho vật đứng đất.
4. ✅ Frame animation: **cùng kích thước canvas**, thân đứng yên tuyệt đối, frame 01 = tư thế nghỉ.
5. ✅ Style: burgundy `#8E1F3B` + đồng vàng `#D9A441`, gỗ nâu ấm, outline nâu đậm cartoon (không đen), dễ thương cho phụ nữ & trẻ em. Tham chiếu chi tiết: `production/art-handoff/STYLE_CONTRACT.md`.
6. ✅ Giao đúng TÊN FILE + THƯ MỤC bên dưới.

---

# GÓI A — NPC CÔ GÁI HƯỚNG DẪN: 4 CLIP CÙNG KHUNG HÌNH ⭐ ƯU TIÊN 1 · 37 file

## Chuyện đã xảy ra
Trong game, cô gái hướng dẫn **nhảy tới nhảy lui, chớp ô magenta** mỗi 3–6 giây. Lead đã mổ ra nguyên nhân — **không phải code**:

| Clip hiện có | Nền | Khung hình |
|---|---|---|
| `guide_talk_01..12` | trong suốt ✅ | cắt sát đầu-vai, đầu chiếm ~55% chiều cao |
| `guide_wave_01..12` | **magenta ❌** | nửa thân, đầu nhỏ hơn, đứng thấp hơn |
| `guide_point_01..12` | **magenta ❌** + dải xám trên | nửa thân, tay chỉ |
| `guide_blink.png` | **magenta ❌** | nửa thân — khớp wave, KHÔNG khớp talk |

Code đổi clip theo bước (Talk ↔ Point ↔ Wave) và chớp mắt bằng `guide_blink` ⇒ mỗi lần đổi là đầu co/giãn, nhảy vị trí, lộ ô magenta. Ngoài ra `guide_talk_01..12` thực chất chỉ có **4 hình khác nhau** (01=12, 02=11, 03=04=09=10, 05=06=07=08) ⇒ miệng giật cục.

## Yêu cầu — MỘT KHUNG HÌNH DUY NHẤT cho cả 4 clip

📁 `A_NPC_Guide/`

**Chuẩn khung hình = `guide_talk_01.png` hiện tại.** Mở file này TRƯỚC KHI VẼ:
`Assets/Art/UI/TutorialV2/npc/guide_talk_01.png` (bản sạch cùng nội dung: `production/art-handoff/2026-09-04_MASTER/D_NPC_Mieng_SuaLai/guide_talk_01.png`).
Đo bằng tool của mình: **vị trí tâm đầu, đỉnh tóc, vai trái/phải, mép dưới thân** — rồi giữ **y nguyên các mốc đó** trên toàn bộ 37 file dưới. Lệch ≤ 2 px.

| # | File | Số file | Canvas | Vẽ gì |
|---|---|---|---|---|
| A1 | `guide_talk_01.png` … `guide_talk_12.png` | 12 | **512 × 640** | **Vẽ lại đủ 12 khẩu hình KHÁC NHAU** (miệng đóng → hé → mở vừa → mở rộng → về), thân/đầu/tóc/mắt **đứng yên tuyệt đối**, chỉ miệng (và cằm rất nhẹ) thay đổi. Nét mặt hiền, đang giải thích cho người mới. Frame 01 = miệng đóng, nghỉ |
| A2 | `guide_wave_01.png` … `guide_wave_12.png` | 12 | 512 × 640 | Cùng khung hình A1. Tay phải giơ vẫy chào nhẹ (biên độ nhỏ, không che mặt). Miệng cười. Frame 01 = tay bắt đầu giơ |
| A3 | `guide_point_01.png` … `guide_point_12.png` | 12 | 512 × 640 | Cùng khung hình A1. Tay phải đưa ra chỉ về phía **trước-phải** (hướng vào nội dung game). Frame 01–06 đưa tay ra, 07–12 giữ & nhấn nhẹ (code lặp 06→12) |
| A4 | `guide_blink.png` | 1 | 512 × 640 | **Đúng frame `guide_talk_01` nhưng mắt nhắm.** Không đổi gì khác |

⚠ Kiểm tra trước khi giao (đội vẽ tự QC):
- 37 file cùng 512×640, alpha thật, **0 pixel magenta**.
- Ghép 37 frame thành GIF nháp xem: đầu **không** nhúc nhích, chỉ miệng/tay động.
- Không bóng dưới chân, không viền trắng.

---

# GÓI B — 4 MINH HOẠ BẢNG HƯỚNG DẪN ⭐ ƯU TIÊN 2 · 4 file

Bảng hướng dẫn 4 trang (Bước 1 Trồng lúa → Bước 2 Tăng tốc → Bước 3 Thu hoạch → Bước 4 Nhận thưởng) hiện dùng **card chữ + icon rời** rất khô ("Ô ĐẤT", "HẠT GIỐNG LÚA"). Sếp muốn UI có hồn cho phụ nữ & trẻ em: mỗi trang **1 tranh minh hoạ** kể việc cần làm, không cần chữ.

📁 `B_Tutorial_Illustrations/`

| # | File | Canvas | Vẽ gì (không chữ) |
|---|---|---|---|
| B1 | `tut_illu_plant.png` | **512 × 512** | Bàn tay cầm túi hạt lúa **kéo** thả vào ô đất nâu (mũi tên cong mềm màu vàng đồng), vài hạt rơi, ô đất kiểu isometric như trong game |
| B2 | `tut_illu_speedup.png` | 512 × 512 | Cây lúa non trong ô đất + **viên kim cương xanh** lớn phía trên có tia lấp lánh, mũi tên cong từ kim cương xuống cây; cây bên phải đã cao vàng óng (trước/sau) |
| B3 | `tut_illu_harvest.png` | 512 × 512 | **Cái liềm** vàng đồng lướt qua ô lúa chín, vài bó lúa bay lên, tia sáng nhỏ |
| B4 | `tut_illu_reward.png` | 512 × 512 | Giỏ/rổ lúa đầy + đồng vàng bay lên + ngôi sao EXP xanh lam nhỏ (như HUD), cảm giác ăn mừng nhẹ |

Quy tắc chung gói B: vật thể chiếm ~80% khung, căn giữa, **cùng góc nhìn chếch 3/4, cùng nguồn sáng trên-trái, cùng độ dày outline** cho cả 4 — vì 4 tranh nằm cạnh nhau trong 4 trang liên tiếp. Tham chiếu palette: `Assets/Art/UI/TutorialV2/board/tut_slot_illustration.png` và bộ icon `Assets/Assetsgame/hatgiong/SHOP/icons/`.

---

## ✅ CHECKLIST BÀN GIAO
```
production/art-handoff/2026-09-05_UI_PASS/
├─ A_NPC_Guide/            37 png (talk 12 · wave 12 · point 12 · blink 1) — 512×640
└─ B_Tutorial_Illustrations/ 4 png — 512×512
```
Xong gói nào nhắn Lead **"đã giao gói A/B"**. Lead QC bằng số (canvas, alpha, magenta=0, lệch mốc ≤2px) rồi nạp vào game qua tool `Tools/Farm Game/Tutorial V2/★ Nạp art NPC + VFX` — đội vẽ không phải làm gì thêm.
