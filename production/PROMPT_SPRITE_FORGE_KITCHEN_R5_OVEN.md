# 🎨 PROMPT AGENT-SPRITE-FORGE — KITCHEN R5: VẼ LẠI CÁI LÒ NƯỚNG TO (1 file chính + 1 phụ)
> Đây là "cái lò to bự phía sau" trong mockup Kitchen Cook Flow — Sếp sẽ đính kèm ảnh mockup.
> Bản hiện tại nhỏ, cam bẹt. Vẽ lại ĐÚNG như lò trong mockup + ảnh bếp lò tham chiếu.
> 📁 GHI ĐÈ vào `Assets/Export_Kitchen_UI_Package/Sprites/`

## ⚠️ LUẬT ART STUDIO
1. ❌ KHÔNG TEXT. 2. ❌ KHÔNG nền/bóng đổ ngoài — alpha 100%. 3. ✅ spriteMode: 1.
4. ❌ KHÔNG bake lửa/khói vào thân lò (lửa là 4 frame riêng đã có).

## FILE
1. `oven_body.png` (512×512, pivot Bottom-Center) — LÒ NƯỚNG GẠCH LỚN theo mockup:
   - Vòm mái tròn bo dịu màu nâu đất gradient (sáng trên, sậm dưới), có khối mềm storybook
   - MIỆNG LÒ vòm cung to màu nâu sô-cô-la sậm (để trống — code đặt lửa/glow vào đây)
   - Ống khói vuông ngắn trên đỉnh
   - Chân đế quầy gạch/gỗ ngang có 2 rãnh nhỏ như mockup
   - Tỉ lệ: vòm chiếm ~65% cao, đế ~35%; miệng lò chiếm ~40% giữa vòm, tâm hơi thấp
2. `oven_glow.png` (300×220) — quầng sáng cam ấm hình vòm khớp miệng lò mới, mép mềm alpha
   (thay bản cũ cho khớp size mới).

## SAU KHI GIAO: báo Sếp bấm Setup Kitchen UI v2 — code đã phóng khung lò lên 280×240 chờ sẵn.
