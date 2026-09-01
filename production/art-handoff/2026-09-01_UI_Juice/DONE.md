# 🎨 BÁO CÁO BÀN GIAO ART — GÓI "UI JUICE V2" (BẢN V2 MASCOT KHÔNG CHÂN)
> Người thực hiện: Đội vẽ / Tech Lead Studio Cooking-Farm-2D
> Nơi nhận: Tech Lead & Đội Dev Unity
> Thư mục bàn giao: production/art-handoff/2026-09-01_UI_Juice/
> Thư mục trong Game: Assets/Art/UI/

---

## 1. DANH MỤC FILE ĐÃ BÀN GIAO & IMPORT (49 FILE)

### 🪙 Hạng mục A — Icon Tiền Tệ Duy Nhất
- currency/icon_gold.png — 256×256 px, PNG Alpha 100%, đồng xu vàng dày 3/4 viền nâu ấm #654129, dập nổi bông lúa mì, không text. *(Đã loại bỏ hoàn toàn icon EXP / Kim cương theo chỉ đạo)*.

### 🎭 Hạng mục B — 4 Nhân Vật Ăn Mừng V2 (Mascot Bán Thân Không Chân - 12 frames/con)
- characters/char_01/ (12 frames): char_01_f01.png → char_01_f12.png — Nông dân nam (mũ thám hiểm - NV01)
- characters/char_02/ (12 frames): char_02_f01.png → char_02_f12.png — Đầu bếp (nón chef trắng, khăn xanh - NV_CHEF)
- characters/char_03/ (12 frames): char_03_f01.png → char_03_f12.png — Thôn nữ (mũ bucket sage green - NV03)
- characters/char_04/ (12 frames): char_04_f01.png → char_04_f12.png — Nữ thám hiểm (mũ safari bọ - NV05)

---

## 2. BẢNG TỰ QC (KIỂM TRA CHẤT LƯỢNG)
- [x] **Chuẩn Mascot Bán Thân**: Chỉ đầu to + ngực/vai, TUYỆT ĐỐI KHÔNG CHÂN, đáy thân cắt cong tròn để lọt gọn trong huy hiệu tròn.
- [x] **Baseline & Pivot**: Đáy thân cố định y≈470 trên mọi frame, căn giữa X=256.
- [x] **Animation Nhún Tại Chỗ**: 12 frames @ 12fps (f01 nghỉ -> f02-04 squash lún -> f05-07 stretch vươn -> f08-09 đỉnh lắc đầu -> f10-12 hồi phục mẩy mẩy).
- [x] **100% Alpha trong suốt**: Đã khử sạch viền và nền, không bóng đổ đen, không confetti nổ sẵn.
- [x] **Đã import trực tiếp**: Toàn bộ sprite đã có sẵn trong Assets/Art/UI/Currency/ và Assets/Art/UI/LevelUpV2/characters/.
