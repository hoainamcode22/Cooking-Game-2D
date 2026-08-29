# Prompt vẽ lại — 2 sheet BÍ ĐỎ + DƯA HẤU (bỏ vũng đất bake sẵn)

**Bối cảnh:** 2 sheet 5-stage của **Bí Đỏ** và **Dưa Hấu** đã dùng được trong game, nhưng phần vẽ có
một lỗi kỹ thuật: dưới gốc cây và sau lá, máy vẽ đã **vẽ sẵn một vũng ĐẤT ĐỎ-NÂU + bóng tiếp đất**.

Vì trong game mỗi ô ruộng xếp 6 cây chồng nhau, vũng đất đó lộ ra thành **vệt hồng-đỏ** trên nền đất
nâu của ô ruộng — trông như tách nền chưa sạch. Bên dev đã gọt bằng thuật toán nhưng gọt sâu hơn nữa
là bắt đầu ăn vào lá, nên cần bản vẽ lại cho sạch từ gốc.

**Chỉ cần sửa đúng 1 điểm — mọi thứ khác GIỮ NGUYÊN:**

- ❌ BỎ hoàn toàn vũng đất / mảng đất / bóng tiếp đất dưới gốc và phía sau lá.
- ✅ Cây "mọc trên không" — nền magenta chạm trực tiếp tới mép lá, mép quả, mép dây leo.
- ✅ Giữ NGUYÊN: bố cục, dáng cây, số lá, màu sắc, nét outline, tỉ lệ 5 stage như bản hiện tại.
  (Bản hiện tại đã được duyệt về mặt tạo hình — chỉ vướng vũng đất.)

**2 file cần vẽ lại (đúng tên, đúng thư mục cũ):**

| File | Nội dung | 5 stage |
|---|---|---|
| `Bi_Do_5stage.png` | Bí đỏ | hạt → mầm → dây lá → ra hoa + quả non → quả chín to |
| `Dua_Hau_5stage.png` | Dưa hấu | hạt → mầm → dây lá → hoa + quả non → quả chín to |

**Quy cách kỹ thuật (giống 21 sheet trước, đã chạy tốt):**
- 5 stage xếp thành **một hàng ngang**, trái → phải theo thứ tự lớn dần, **chân cây thẳng hàng nhau**.
- Nền **magenta đồng nhất** (#FF00FF hoặc gần), **KHÔNG đổ bóng, KHÔNG vũng đất, KHÔNG nền cỏ/đất**.
- Các stage cách nhau một khoảng trống rõ ràng (nền magenta liền mạch), **không stage nào chạm/đè
  stage bên cạnh** — bên dev cắt tự động theo khoảng trống này.
- Kích thước tương đương các sheet cũ (~1536×1024 hoặc 1672×941), 1 file 1 loại cây.

---

## 🎨 LUẬT ART STUDIO — BẮT BUỘC MỌI PROMPT VẼ PHẢI ĐÍNH KÈM
> Ban hành 2026-08-26 theo lệnh Sếp. MỌI prompt gửi đội vẽ (GPT/agent-sprite-forge) PHẢI dán nguyên khối này.

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ
   trên BẤT KỲ asset nào (thân tàu, toa, nhà, thùng, bảng...). Text do game render bằng TMP.
   Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ
   dưới chân object — bóng do game tự vẽ để đồng nhất theo giờ trong ngày.
   *(← chính là điểm cần sửa ở 2 sheet này)*
3. ✅ **Meta Unity chuẩn**: spriteMode: 1 (Single) từng file · pivot Bottom-Center cho object đứng đất.
4. ✅ **Frame animation**: mọi frame cùng hướng = CÙNG kích thước canvas, thân đứng yên cùng vị trí;
   frame 01 = tư thế nghỉ; KHÔNG khói/hiệu ứng bake vào frame (code phun runtime).
5. ✅ **Style chuẩn**: theo bộ Export_Train_UI_Package (burgundy #8E1F3B + đồng vàng #D9A441,
   gỗ nâu ấm, outline nâu đậm cartoon, dễ thương cho phụ nữ & trẻ em).
6. ✅ Giao đúng TÊN FILE + THƯ MỤC được đặt trong prompt, không thêm file phụ (_single, @2x tự ý...).

---

## Giao file
- Đặt vào `Assets/Assetsgame/hatgiong/Hatgiong/` (cùng chỗ 13 sheet cũ), PNG, **không cần kèm `.meta`**.
- Nhận file xong bên dev chạy lại script cắt là ra 5 sprite/cây, tự vào đúng chỗ cũ — **không cần
  nối lại data, không sửa scene** (guid giữ nguyên).
