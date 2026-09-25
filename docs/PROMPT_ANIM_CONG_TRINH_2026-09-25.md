# Prompt cho đội vẽ: công trình đứng yên, chỉ bộ phận chuyển động (2026-09-25)

## Vì sao hiện tại cả nhà bị rung
Sheet cũ (mayxaymia.png, maylamthucan.png) vẽ lại NGUYÊN căn nhà ở mỗi frame. Mỗi lần vẽ lại, mái, tường, bóng lệch nhau vài pixel, nên khi chạy animation cả công trình giật, trông như bị xê dịch.
Game trong video làm khác: nhà là 1 hình đứng yên, chỉ các vật nhỏ (thùng hàng trên băng chuyền, bánh răng, người làm) là lớp riêng chạy trên nền. Khói là hạt (particle) do engine tạo, không vẽ trong frame.

## Quy tắc bắt buộc cho đội vẽ
1. Vẽ căn nhà MỘT lần (file gốc PSD/Aseprite). Mọi lớp xuất ra từ CÙNG file gốc đó, không generate lại căn nhà.
2. Mọi file PNG của 1 công trình dùng CÙNG kích thước canvas và CÙNG gốc tọa độ. Xếp chồng các PNG lên nhau phải khớp từng pixel.
3. Nền (BASE): căn nhà hoàn chỉnh nhưng BỎ các bộ phận chuyển động. Chỗ bộ phận đó che, vẽ phần tường/nền phía sau cho kín.
4. Bộ phận chuyển động: mỗi frame CHỈ có bộ phận đó, còn lại trong suốt. Không vẽ lại tường, mái, bóng.
5. Không vẽ khói, không vẽ quầng sáng đèn vào bất kỳ frame nào (engine tự làm).
6. Loop liền: frame cuối nối mượt về frame đầu.
7. Góc nhìn isometric 2:1 giống nhà hiện tại, ánh sáng từ trên-trái, cùng bảng màu.
8. Xuất PNG nền trong suốt, không nén mất màu, viền sạch (không viền trắng/đen quanh vật).

## Kích thước giao
- Máy làm thức ăn (maylamthucan): canvas 600 x 448 px mỗi lớp (gấp đôi frame cũ 300x224).
- Máy xay mía (mayxaymia): canvas 300 x 224 px mỗi lớp (frame cũ ~130x104 quá nhỏ, vẽ lại to hơn).
- Các frame của 1 bộ phận ghép ngang thành 1 dải (strip), mỗi ô đúng bằng canvas.

## Danh sách file cần giao

### A. Máy làm thức ăn (feed mill)
| File | Nội dung | Số frame |
|---|---|---|
| feedmill_base.png | Nhà đầy đủ: mái, ống khói (miệng ống rỗng, không khói), phễu hạt trên mái (đầy hạt, đứng yên), đèn tắt sáng quầng, bệ đá. KHÔNG có bánh răng, KHÔNG có hạt chảy trên máng, KHÔNG có thùng | 1 |
| feedmill_gears.png | 2 bánh răng đồng trên tường, quay 1 vòng (bánh lớn chiều kim đồng hồ, bánh nhỏ ngược) | 8 |
| feedmill_chute.png | Dòng hạt chảy trên máng nghiêng, loop | 6 |
| feedmill_crate_empty.png | 1 thùng gỗ rỗng, đứng riêng, canvas 128x96 | 1 |
| feedmill_crate_full.png | Cùng thùng đó đầy hạt vàng, canvas 128x96 | 1 |
| feedmill_lamp_glow.png | Quầng sáng vàng mềm của đèn, chỉ quầng sáng | 1 |



### C. Khói ống khói (dùng chung mọi nhà)
| File | Nội dung |
|---|---|
| smoke_puff_01..03.png | 3 cụm khói tròn mềm, trắng xám, mép mờ dần trong suốt, 128x128, không có viền cứng |

## Prompt tiếng Anh (cho công cụ AI) 

### Base
"Isometric 2:1 cozy farm game building, hand-painted stylized mobile game art, warm wood and stone, soft top-left lighting, [feed mill with a grain hopper on the roof and a stone chimney / small wooden sugarcane press house with a sugarcane bundle and a wooden collecting tub]. Static building only: NO moving parts, NO gears, NO water wheel, NO flowing grain or liquid, NO smoke, NO light glow. Clean edges, transparent background, centered, full building visible, same camera angle as reference image."

### Bộ phận (dùng inpainting / vẽ trên CHÍNH ảnh base, không tạo ảnh mới)
"Only the [two brass gears on the wall / wooden water wheel / stream of grain on the chute / stream of green sugarcane juice], isolated on a fully transparent background, exact same position, scale, perspective and lighting as in the base image, animation frame N of 8, seamless loop, no building, no background, no shadow on ground."

### Khói
"Soft stylized smoke puff for a mobile farm game, round fluffy cloud, white to light grey, very soft edges fading to transparent, no outline, transparent background, 128x128."

## Lưu ý cho đội vẽ khi dùng AI
AI không giữ đúng vị trí giữa các lần generate. Cách an toàn: generate BASE 1 lần, rồi vẽ tay / inpaint các bộ phận lên chính file đó trong Photoshop, tách từng bộ phận ra layer riêng rồi xuất. Trước khi giao, bật tất cả layer chồng lên nhau và chạy thử: căn nhà phải đứng im tuyệt đối.

## Phía code (Tech Lead làm khi có hình)
- Prefab mỗi công trình: SpriteRenderer base + các SpriteRenderer con cho bộ phận (sorting order +1), Animator chỉ đổi sprite của bộ phận.
- Thùng hàng: code sinh thùng rỗng, trượt ra khỏi máy theo đường isometric, đổi thành thùng đầy, xếp chồng ở cửa (giống băng chuyền trong video).
- Đèn: quầng sáng nhấp nháy nhẹ bằng alpha.
- Khói: ParticleSystem nhẹ đặt ở miệng ống khói, 1 đến 2 hạt/giây, bay lên, trôi theo gió, to dần, mờ dần. Chỉ chạy khi máy đang làm việc.
