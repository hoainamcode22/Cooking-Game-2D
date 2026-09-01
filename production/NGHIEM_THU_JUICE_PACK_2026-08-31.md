# ✅❌ BIÊN BẢN NGHIỆM THU JUICE PACK — vòng 2, 2026-08-31 20:10

## ✅ ĐẠT — Đợt 2 Icon tiền tệ (bản vẽ lại 20:07) + Đợt 1 FX (20:08)
- 3 icon 256×256 RGBA: nền trong suốt chuẩn (alpha 4 góc = 0), xu hết cắt mép,
  có rãnh sao, không text — ĐÚNG luật art. Gem giác cắt rõ. Đã nằm đúng
  `Assets/Resources/UI/Currency/` (code đã trỏ đường này).
- 7 file FX confetti/spark: size đúng 64/96, alpha chuẩn. Nằm đúng `Resources/FX/Celebrate/`.
- `coin_stack_v2` dính 1 đốm rác 161px phía trên — đội code đã dọn bằng thuật toán
  connected-components và ghi đè cả handoff + Resources (20:2x). KHÔNG cần đội vẽ làm lại.

## ❌ KHÔNG ĐẠT — Đợt 3 Mascot (60 frame)
Bằng chứng máy: md5 toàn bộ frame kiểm tra = TRÙNG bản 19:46 (vòng 1) — báo cáo
"đã vẽ lại, xóa phông" là chưa đúng với hàng thực trong folder. Lỗi vòng 1 vẫn nguyên:
1. **Có NỀN tròn navy + viền vàng bake cứng** — vi phạm Luật Art #2 (không nền).
   Khung huy hiệu là việc của popup, không bake vào frame.
2. **Chỉ là chân dung đầu** — spec yêu cầu NỬA THÂN từ hông, thấy tay.
3. **12 frame gần như tĩnh** (frame_01 vs frame_07 chỉ lệch vành khung) — không có
   động tác lấy đà → bật nhảy → vung tay → tiếp đất như spec.

→ Prompt trả hàng: `PROMPT_SPRITE_FORGE_MASCOT_TRA_HANG_LAN2.md`.
Trong lúc chờ: popup Level-Up vẫn chạy bình thường với bộ frame hiện tại
(hiển thị như huy hiệu chân dung nhún nhảy — không vỡ, chỉ chưa "rầm rộ").


---
## ✅ CẬP NHẬT vòng 3 (20:22 → 20:4x): MASCOT ĐẠT — TOÀN BỘ JUICE PACK HOÀN TẤT
- 60/60 frame là hàng MỚI thật (md5 khác hẳn vòng 1-2), 256×256, alpha 4 góc = 0.
- Động tác đo máy: 24-33% pixel thay đổi giữa frame nghỉ và frame nhảy (cả 5 nhân vật) —
  chuỗi đứng → chùng gối lấy đà → bật nhảy vung 2 tay reo hò → tiếp đất, đúng spec.
- Soi mắt: nhân vật khớp avatar gốc, style outline đậm, không nền không text.
- Đội code hậu kỳ: 12/60 frame dính MẢNH NHÂN VẬT TRÀN TỪ MÉP CANVAS (cắt sheet lệch —
  boy 01-04, cowboy 03/08/09/10/12, lumberjack 05/06/10). Đã dọn bằng thuật toán
  giữ-1-khối-chính, soi lại từng ảnh, ghi đè cả Resources lẫn art-handoff.
- Kết luận: cả 3 đợt hàng NGHIỆM THU ĐẠT. Chờ Sếp mở Unity Play test.
