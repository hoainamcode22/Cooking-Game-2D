# 🔁 PROMPT TRẢ HÀNG ĐỢT 1 — VẼ LẠI 4 NHÂN VẬT ĂN MỪNG (gói UI Juice V2)
> Từ: Tech Lead · 2026-09-01 · Nghiệm thu lần 1: **icon vàng ĐẠT (không vẽ lại)** — **4 nhân vật TRẢ HÀNG**.
> Biên bản lỗi + ảnh bằng chứng: `production/art-handoff/2026-09-01_UI_Juice/NGHIEM_THU_LAN1.md` + `QC_EVIDENCE_chars.png` — MỞ XEM TRƯỚC KHI LÀM.
> Spec gốc vẫn hiệu lực: `production/PROMPT_SPRITE_FORGE_UI_JUICE_2026-09-01.md` (mục 0, 2, 4). Dưới đây CHỈ là phần SỬA.

---

## VÌ SAO BỊ TRẢ (4 lỗi đo được — không tranh luận)
1. Sai style: pastel/kawaii sticker, màu bệt, không outline nâu ấm → PHẢI theo `STYLE_CONTRACT.md` (hand-painted semi-realistic, outline #442510→#654129, KHÔNG đen, KHÔNG trắng).
2. Sai nhân dạng: char_02 không phải đầu bếp, char_04 không phải bác nông dân già; 3/4 nhân vật na ná nhau.
3. Frame không đồng nhất: mỗi frame bị gen lại từ đầu — mũ/mặt đổi hình dạng, đáy thân trôi 451→488, tâm X trôi 226→285. ĐÂY LÀ LỖI NẶNG NHẤT.
4. f04 trùng y hệt f10 (chỉ có 11 frame thật).

## YÊU CẦU SỬA — LÀM ĐÚNG THEO QUY TRÌNH NÀY, TỪNG NHÂN VẬT MỘT
**Bước 1 — CHỐT MASTER (frame nghỉ f01) trước, đủ 4 con, style khớp game:**
- View lại cho vào context: 2 golden ref (bắp cải, cà chua) + sprite gốc từng nhân vật:
  - char_01 `NV01_down_1.png` — nông dân NAM, mũ thám hiểm be, da nâu, áo khaki túi hộp.
  - char_02 sprite trong `Assets\NV_CHEF\` — ĐẦU BẾP đội NÓN CHEF TRẮNG cao, tạp dề. Nhân dạng phải nhìn phát biết ngay là đầu bếp.
  - char_03 `NV03_down_1.png` — thôn nữ. GIỮ ĐÚNG trang phục/mũ của NV03 trong game.
  - char_04 `NV05_down_1.png` — BÁC NÔNG DÂN GIÀ (nam, lớn tuổi, có râu/nếp nhăn). KHÔNG được đổi thành nhân vật khác.
- Mỗi master: 512×512, đầu to ~55-60% + thân trên, KHÔNG chân, đáy thân bo cong, **đáy chạm đúng y=470 (±4px), tâm X=256 (±6px)**, biểu cảm cười ăn mừng, outline nâu ấm, tô hand-painted có khối.
- 4 master phải KHÁC NHAU rõ rệt (nam trẻ / đầu bếp / thôn nữ / ông già).

**Bước 2 — SINH 11 FRAME CÒN LẠI TỪ CHÍNH MASTER ĐÓ (img2img/edit từ f01, hoặc vẽ sheet lưới 3×4 trong 1 ảnh rồi cắt — cấm gen rời từng frame từ text):**
- CÙNG một nhân vật, CÙNG mũ, CÙNG màu, CÙNG nét mặt cơ bản — chỉ đổi TƯ THẾ theo nhịp:
  f01 nghỉ → f02-f04 lún (squash bè ra, đầu cúi) → f05-f07 vươn (thon cao, đầu ngửa, cười to nhất) → f08-f09 đỉnh + đầu nghiêng 5-8° → f10-f12 hồi về, dư chấn nhỏ dần, khớp loop về f01.
- 12 frame PHẢI KHÁC NHAU từng frame (cấm copy f04 thành f10).
- Khống chế hình học MỌI frame: đáy thân y=470±4 · tâm X=256±6 · silhouette mũ không đổi kiểu dáng.

**Bước 3 — TỰ QC BẰNG SỐ trước khi nộp (script python, ghi kết quả vào DONE_DOT1.md):**
- bbox bottom mỗi frame ∈ [466, 474]; |centerX − 256| ≤ 6; 12 md5 khác nhau; canvas 512×512; alpha 4 góc = 0.
- Ghép thử GIF 12fps xem có bị morph mũ/mặt không — mắt thường thấy "nhấp nháy đổi hình" là chưa đạt.

## BÀN GIAO ĐỢT TRẢ HÀNG
- Giao vào: `E:\Game2\Cooking-Game-2D\production\art-handoff\2026-09-01_UI_Juice\characters\char_0N\char_0N_f01..f12.png` (ghi đè bản cũ trong art-handoff).
- **TUYỆT ĐỐI KHÔNG tự copy vào `Assets\` nữa** — vi phạm quy trình lần 2 là Lead báo Sếp. Lead sẽ QC rồi tự gắn.
- Kèm `DONE_DOT1.md`: bảng số QC bước 3 cho đủ 48 file.
- KHÔNG động vào `currency/icon_gold.png` (đã đạt).
