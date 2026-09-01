# 🔁 ĐƠN ĐẶT HÀNG ĐỢT 2 — ĐỒNG BỘ STYLE 4 NHÂN VẬT POPUP LÊN CẤP (đơn NHỎ, chỉ 2-6 file)
> Từ: Tech Lead · 2026-09-01 · Sếp duyệt bộ 4 nhân vật hiện tại về Ý TƯỞNG nhưng chê: **4 con không cùng style với nhau**.
> TIN VUI: game đã chuyển sang chế độ PUPPET (code tự diễn thở/nhún trên 1 hình) → **KHÔNG cần vẽ 12 frame nữa. Mỗi nhân vật chỉ cần ĐÚNG 1 HÌNH MASTER.**

## CHẨN ĐOÁN STYLE (nhìn sheet hiện tại)
- char_03 (cô gái mũ bucket + máy ảnh) và char_04 (cô gái áo vest safari): **CHUẨN** — trùng style với avatar HUD trong game (nét mềm, má hồng, tỉ lệ chibi đầu to, tông kaki/be). → GIỮ NGUYÊN, không vẽ lại.
- char_01 (nông dân nam): mặt render kiểu bán-thực (semi-real), lệch hẳn 2 cô gái → VẼ LẠI theo style char_03/04.
- char_02 (bé gái mũ trắng lạ): style kawaii phẳng + cái mũ không rõ là gì → VẼ LẠI theo style char_03/04.

## VIỆC CẦN LÀM (chỉ 2 file bắt buộc)
1. `char_01_master.png` — **Nông dân nam trẻ** (giữ ý tưởng cũ: mũ thám hiểm be, áo khaki) nhưng VẼ ĐÚNG STYLE của char_03/char_04: cùng kiểu mặt tròn má hồng, cùng nét outline, cùng tông màu kaki/be/nâu, cùng độ mềm của tô màu. Đặt char_03_f01.png bên cạnh khi vẽ để so — 2 con phải nhìn như CÙNG MỘT HỌA SĨ vẽ.
2. `char_02_master.png` — **Đầu bếp** (nón chef trắng cao truyền thống + tạp dề, khăn cổ đỏ) — cũng đúng style char_03/04. Nhân vật phải nhìn phát biết ngay là đầu bếp.

**Spec mỗi file:** 512×512 PNG alpha 100%, mascot bán thân KHÔNG CHÂN, đáy thân bo cong chạm y=470±4, tâm X=256±6, mặt cười ăn mừng nhìn thẳng, không text/không nền/không bóng đổ/không FX bake.

## TÙY CHỌN THÊM (nếu tiện — làm nhân vật chớp mắt):
- `char_01_blink.png` … `char_04_blink.png`: copy y nguyên master từng con (char_03/04 dùng chính f01 hiện có làm master), CHỈ sửa mắt thành nhắm cười, mọi pixel khác giữ nguyên vị trí.

## THAM CHIẾU (view ảnh trước khi vẽ)
- Style đích: `E:\Game2\Cooking-Game-2D\Assets\Art\UI\LevelUpV2\characters\char_03\char_03_f01.png` và `...\char_04\char_04_f01.png`
- Avatar HUD của game (đồng bộ tổng thể): ảnh avatar góc trái HUD trong scene farm.

## BÀN GIAO
- Giao vào: `E:\Game2\Cooking-Game-2D\production\art-handoff\2026-09-01_UI_Juice\characters\char_0N\char_0N_master.png` (+ `_blink.png` nếu có).
- KHÔNG đụng `Assets\`. Có file là Lead QC pixel → copy vào game → Sếp bấm lại menu `★ HOÀN THIỆN NHÂN VẬT` là nhận art mới ngay (tool ưu tiên `_master.png` hơn `_f01.png`).
- Tự QC trước khi nộp: đáy y=470±4, tâm X=256±6, alpha 4 góc =0, đặt cạnh char_03 nhìn cùng style.
