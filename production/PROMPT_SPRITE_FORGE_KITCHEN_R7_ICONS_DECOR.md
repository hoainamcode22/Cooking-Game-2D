# PROMPT ĐỘI VẼ — KITCHEN R7: ICON VÀNG + BẢNG TRẠNG THÁI LÒ + DECOR KHO/LÒ
> Gửi kèm nguyên khối luật bên dưới cho mọi lượt vẽ.

## 🎨 LUẬT ART STUDIO — BẮT BUỘC ĐÍNH KÈM
1. ❌ TUYỆT ĐỐI KHÔNG TEXT: không chữ, không số, không logo trên BẤT KỲ asset nào.
2. ❌ KHÔNG NỀN, KHÔNG BÓNG ĐỔ: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ dưới chân object.
3. ✅ Meta Unity chuẩn: spriteMode: 1 (Single) từng file · pivot Bottom-Center cho object đứng đất.
4. ✅ Style chuẩn theo bộ Export_Kitchen_UI_Package hiện có: gỗ nâu ấm, giấy kem, outline nâu đậm cartoon, dễ thương.
5. ⚠️ TUYỆT ĐỐI KHÔNG GHI ĐÈ FILE .META — chỉ giao file .png, meta để Unity tự quản (từng vỡ UI 2 lần vì lỗi này).
6. ✅ Giao đúng TÊN FILE + THƯ MỤC bên dưới, không đổi tên, không thêm hậu tố.

Thư mục giao hàng: `Assets/Export_Kitchen_UI_Package/Sprites/`

---
## 1) icon_gold.png — icon vàng nhỏ (64×64)
Đồng xu vàng lấp lánh, phong cách ấm cùng bộ bếp (không dùng vàng lạnh kim loại công nghiệp).
Dùng để thay chữ "(+vàng)" trên bảng đen và trên nút mua thêm ô khay — code đã sẵn sàng, cứ giao
là tự lên hình, không cần báo lại/chỉnh code gì thêm.

## 2) plaque_oven_state.png — bảng trạng thái lò (300×64, 9-slice)
Hiện khung "Lò chưa nhóm" đang dùng tạm nền giấy kem chung, Sếp thấy trống/thiếu chất riêng.
Vẽ 1 tấm biển gỗ nhỏ kiểu treo trước lò (dạng thanh ngang, 2 đầu có thể có chốt/đinh gỗ trang trí),
nền trong để game phủ màu tiến trình (thanh cam) đè lên — viền + khung phải rõ để nhìn tách biệt
với nền bếp. 9-slice (viền không méo khi co giãn ngang).

## 3) deco_crate_stack.png — chồng thùng gỗ nhỏ (80×70)
Đặt cạnh khung "VÀO KHO" cho góc kho đỡ trống — 2-3 thùng gỗ xếp chồng, có thể hé vài củ/quả bên trong
(không text/nhãn trên thùng).

## 4) deco_firewood.png — bó củi nhỏ (70×56)
Đặt cạnh chân lò nướng — vài khúc củi xếp chéo, tông nâu ấm khớp palette bếp hiện tại.

---
## GIAO HÀNG
Xong đợt nào báo Sếp đợt đó kèm ảnh — bên mình backup + đối chiếu pixel-diff + soi theo
ART_RULES_STUDIO.md như mọi lần trước khi nhận. File cùng tên cũ (không có, đây toàn file MỚI)
nên không cần bấm lại Tools → Setup Kitchen UI v2 — code tự nhận theo tên file.
