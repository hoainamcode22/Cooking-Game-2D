# 🎨 LUẬT ART STUDIO — BẮT BUỘC MỌI PROMPT VẼ PHẢI ĐÍNH KÈM
> Ban hành 2026-08-26 theo lệnh Sếp. MỌI prompt gửi đội vẽ (GPT/agent-sprite-forge) PHẢI dán nguyên khối này.

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ
   trên BẤT KỲ asset nào (thân tàu, toa, nhà, thùng, bảng...). Text do game render bằng TMP.
   Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ
   dưới chân object — bóng do game tự vẽ để đồng nhất theo giờ trong ngày.
3. ✅ **Meta Unity chuẩn**: spriteMode: 1 (Single) từng file · pivot Bottom-Center cho object đứng đất.
4. ✅ **Frame animation**: mọi frame cùng hướng = CÙNG kích thước canvas, thân đứng yên cùng vị trí;
   frame 01 = tư thế nghỉ; KHÔNG khói/hiệu ứng bake vào frame (code phun runtime).
5. ✅ **Style chuẩn**: theo bộ Export_Train_UI_Package (burgundy #8E1F3B + đồng vàng #D9A441,
   gỗ nâu ấm, outline nâu đậm cartoon, dễ thương cho phụ nữ & trẻ em).
6. ✅ Giao đúng TÊN FILE + THƯ MỤC được đặt trong prompt, không thêm file phụ (_single, @2x tự ý...).
