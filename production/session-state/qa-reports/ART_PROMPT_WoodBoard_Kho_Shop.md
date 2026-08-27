# Prompt vẽ — Khung gỗ nền popup Kho (Warehouse) + Shop (popup_Menu)

**Bối cảnh:** 2 popup này (`WarehousePopup` và `popup_Menu`, cùng dùng chung 1 bộ layout) hiện đang
dùng các Image màu phẳng làm placeholder cho khung nền — chưa có texture nào. Đây KHÔNG phải lỗi
RectTransform/layout, mà là thiếu asset — cần đội vẽ cấp texture mới thì mới gán được.

Kích thước & vị trí hiện tại (canvas tham chiếu 1920×1080, dùng để đội vẽ canh tỉ lệ, không cần vẽ đúng pixel):
- Khung viền ngoài (`Board_Border`): 1516 × 896 px
- Vùng nền trong (`Board_Fill_Top` / `Board_Fill_Bottom`, xếp chồng 2 lớp): 1500 × 880 px (viền dày ~8px mỗi cạnh)
- 4 con tán/đinh ốc góc (`Stud_0..3`): mỗi cái hiện dựng từ 3 vòng tròn màu phẳng chồng nhau
  (Rim ngoài 30×30, Base giữa 26×26, Shine highlight 13×13) — đội vẽ chỉ cần vẽ GỘP thành 1 sprite duy nhất.

---

## 📦 2 FILE CẦN VẼ

### 1) `WoodBoard_Frame.png`
- Khung bảng gỗ nguyên khối (viền + nền gộp làm 1 texture), sẵn sàng để Unity dùng **9-slice (Sliced)**
  khi co giãn theo từng popup (popup Kho/Shop có thể rộng hẹp khác nhau).
- Canvas gợi ý: 512×512 hoặc 768×768, border insets đều 4 cạnh khoảng 64px (tỉ lệ ~12.5% mỗi cạnh) để
  9-slice không méo khi kéo giãn lớn.
- Gỗ nâu ấm, có vân gỗ nhẹ, có thể có vài mối ghép ván dọc/ngang cho có chiều sâu, không quá chi tiết
  rối mắt vì đây là NỀN chứa nội dung UI khác đè lên trên.

### 2) `WoodBoard_Stud.png`
- 1 con tán/đinh ốc góc kim loại (kiểu đồng vàng hoặc sắt cũ), thay thế 3 vòng tròn placeholder hiện tại.
- Hình vuông canvas, vẽ tán nằm giữa, có bóng đổ nhẹ NGAY TRONG sprite để có độ nổi khối (khác với luật
  "không bóng đổ" ở mục 2 luật studio bên dưới — luật đó áp dụng cho object đứng trong scene/nhân vật, còn
  chi tiết kim loại nhỏ như đinh tán thì bóng khối nhẹ ngay trong texture là bình thường cho phong cách UI
  này; nếu đội vẽ không chắc, cứ vẽ PHẲNG không bóng để an toàn, code sẽ tự thêm bóng UI nếu cần).
- 1 file dùng lại (rotate/mirror) cho cả 4 góc — không cần vẽ 4 bản riêng.

**Style tham chiếu để đồng bộ:** bộ asset gỗ đang dùng sẵn trong dự án tại
`Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/` (ví dụ `Khungvien_02.png`, các file `UI board ... .png`)
— màu tông nâu gỗ ấm + điểm nhấn đồng vàng, khớp bảng màu chuẩn ở luật #5 bên dưới.

---

## 🎨 LUẬT ART STUDIO — BẮT BUỘC MỌI PROMPT VẼ PHẢI ĐÍNH KÈM
> Ban hành 2026-08-26 theo lệnh Sếp. MỌI prompt gửi đội vẽ (GPT/agent-sprite-forge) PHẢI dán nguyên khối này.

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ
   trên BẤT KỲ asset nào (thân tàu, toa, nhà, thùng, bảng...). Text do game render bằng TMP.
   Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ
   dưới chân object — bóng do game tự vẽ để đồng nhất theo giờ trong ngày.
3. ✅ **Meta Unity chuẩn**: spriteMode: 1 (Single) từng file · pivot Bottom-Center cho object đứng đất
   (riêng 2 file khung/tán trong prompt này là UI phẳng nên pivot Center là hợp lý, không phải Bottom-Center).
4. ✅ **Frame animation**: mọi frame cùng hướng = CÙNG kích thước canvas, thân đứng yên cùng vị trí;
   frame 01 = tư thế nghỉ; KHÔNG khói/hiệu ứng bake vào frame (code phun runtime). *(không áp dụng cho
   2 file tĩnh trong prompt này, giữ lại nguyên văn luật vì đây là khối bắt buộc đính kèm)*
5. ✅ **Style chuẩn**: theo bộ Export_Train_UI_Package (burgundy #8E1F3B + đồng vàng #D9A441,
   gỗ nâu ấm, outline nâu đậm cartoon, dễ thương cho phụ nữ & trẻ em).
6. ✅ Giao đúng TÊN FILE + THƯ MỤC được đặt trong prompt, không thêm file phụ (_single, @2x tự ý...).

---

## Giao file
- Tên file: đúng như trên (`WoodBoard_Frame.png`, `WoodBoard_Stud.png`).
- Định dạng: PNG, nền trong suốt (alpha), không nén mất chi tiết viền.
- Thư mục đề xuất: `Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/` (cùng chỗ các asset gỗ khác đang
  dùng, để tiện gán) — nếu đội vẽ muốn để thư mục khác cũng được, mình sẽ tự gán lại field khi nhận file,
  KHÔNG cần đội vẽ đụng vào file `.meta` hay chỉnh scene.
- Sau khi nhận file, mình sẽ gán vào đúng 2 popup (Warehouse + Shop) và báo lại kết quả riêng.
