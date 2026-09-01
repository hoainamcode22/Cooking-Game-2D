# 🎨 PROMPT TRẢ HÀNG — MASCOT LEVEL-UP LẦN 2 (chỉ Đợt 3, icon & FX đã ĐẠT)
> Sếp dán nguyên văn cho GPT/agent-sprite-forge. Giao đè vào
> `production/art-handoff/2026-08-31_JuiceFX/3_LevelUp_Mascots/{id}/frame_01..12.png`.

## LÝ DO TRẢ HÀNG (hàng nhận 19:46 — kiểm bằng md5, chưa được vẽ lại)
1. Frame đang có NỀN TRÒN NAVY + VIỀN VÀNG bake cứng → SAI. Yêu cầu: **alpha trong suốt
   100%, KHÔNG khung, KHÔNG huy hiệu, KHÔNG nền** — khung tròn do popup game tự vẽ.
2. Đang là chân dung ĐẦU → SAI. Yêu cầu: **NỬA THÂN từ hông trở lên, THẤY RÕ 2 TAY**.
3. 12 frame gần như đứng yên → SAI. Yêu cầu chuỗi ĐỘNG TÁC RÕ RỆT giữa các frame:
   - frame_01: đứng cười, 2 tay xuôi (tư thế nghỉ)
   - frame_02-04: cúi lấy đà, gối chùng, tay kéo ra sau
   - frame_05-07: BẬT NHẢY lên cao, 2 TAY VUNG THẲNG LÊN TRỜI, miệng reo hò
   - frame_08-09: đỉnh nhảy — người nhô cao nhất khung, mắt nhắm cười toe
   - frame_10-12: tiếp đất nhún gối, tay hạ dần, về lại tư thế frame_01 (lặp mượt)
   KIỂM TRA TRƯỚC KHI GIAO: đặt frame_01 cạnh frame_07 phải thấy KHÁC HẲN
   (độ cao thân, vị trí tay). Nếu nhìn giống nhau = làm lại.

## GIỮ NGUYÊN CÁC YÊU CẦU CŨ
- 5 nhân vật đúng khuôn mặt/tóc/trang phục avatar tham chiếu trong `ref_avatars/`:
  cowboy, chef_female, flower_girl, boy, lumberjack.
- Canvas 256×256/frame, nhân vật ~85% chiều cao, HÔNG neo cùng toạ độ mọi frame
  (đầu và tay được phép vượt lên khi nhảy).
- Style cartoon outline nâu đậm theo bộ Export_Train_UI_Package. KHÔNG text,
  KHÔNG bóng đổ, KHÔNG lấp lánh bake vào frame (code phun runtime).
- Tên file y nguyên: `{id}/frame_01.png` … `frame_12.png`.
