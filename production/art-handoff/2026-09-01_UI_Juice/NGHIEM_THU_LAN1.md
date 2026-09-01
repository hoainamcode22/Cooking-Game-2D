# 🔍 BIÊN BẢN NGHIỆM THU LẦN 1 — GÓI "UI JUICE V2" (2026-09-01)
> Người nghiệm thu: Tech Lead (QC bằng pixel thật trên 49 file, không tin bảng tự QC)
> Ảnh bằng chứng: `QC_EVIDENCE_chars.png` (lưới 4 nhân vật × 12 frame) trong cùng thư mục.

## KẾT LUẬN NHANH
| Hạng mục | Kết quả | Ghi chú |
|---|---|---|
| 🪙 `icon_gold.png` | ✅ **ĐẠT** | Dùng được ngay |
| 🎭 4 nhân vật × 12 frame | ❌ **TRẢ HÀNG** | Sai style + sai nhân dạng + frame không đồng nhất |
| 📋 Quy trình bàn giao | ⚠️ Vi phạm | Tự import thẳng vào `Assets/` — quy trình là giao vào `art-handoff/` rồi Lead gắn |

## 1. ICON VÀNG — ĐẠT ✅ (đo thật)
- 256×256, alpha 4 góc = 0 (nền sạch), bbox cân giữa, đồng xu 3/4 dày, emboss bông lúa mì, không text.
- Gradient vàng ấm + specular đúng hướng trên-trái. Cho phép dùng: giữ nguyên tại `Assets/Art/UI/Currency/icon_gold.png`.

## 2. 4 NHÂN VẬT — TRẢ HÀNG ❌ (4 lỗi, có số đo)
1. **SAI STYLE HOÀN TOÀN**: vẽ kiểu pastel/kawaii sticker màu bệt, KHÔNG phải hand-painted semi-realistic
   của `STYLE_CONTRACT.md`; không có outline nâu ấm #442510→#654129; không khớp 2 golden reference
   (bắp cải/cà chua) lẫn sprite NV trong game.
2. **SAI NHÂN DẠNG**: char_02 phải là ĐẦU BẾP NÓN CHEF TRẮNG (NV_CHEF) → giao 1 bé gái đội mũ lạ;
   char_04 phải là BÁC NÔNG DÂN GIÀ (NV05) → giao "nữ thám hiểm" (chính DONE.md tự khai).
   3/4 nhân vật là bé gái na ná nhau — người chơi không phân biệt được.
3. **FRAME KHÔNG ĐỒNG NHẤT — lỗi nặng nhất**: mỗi frame như được GEN LẠI TỪ ĐẦU: mũ đổi hình dạng,
   mặt đổi nét, char_01 vài frame mặt lem. Số đo: đáy thân trôi 451→488 (spec: cố định 470±4),
   tâm X trôi 226→285 (spec: 256±6). Phát 12fps sẽ bị "morph giật" chứ không phải nhún mượt.
4. **Frame trùng lặp**: f04 = f10 y hệt (md5 trùng) ở cả 4 nhân vật → thực chất chỉ có 11 frame.

## 3. XỬ LÝ
- Icon: GIỮ. Nhân vật: 48 file hiện tại TẠM GIỮ NGUYÊN trong `Assets/Art/UI/LevelUpV2/characters/`
  nhưng **Sếp KHÔNG chạy menu "Gắn art nhân vật V2"** cho tới khi đợt trả hàng đạt — popup vẫn chạy
  bằng sprite NV tạm, không ảnh hưởng game.
- Đội vẽ làm lại 4 nhân vật theo `PROMPT_SPRITE_FORGE_UI_JUICE_TRA_HANG_DOT1.md` (cùng thư mục production/),
  giao vào `production/art-handoff/2026-09-01_UI_Juice/characters/` (KHÔNG tự đụng vào Assets nữa).
- Đạt nghiệm thu lần 2 → Lead copy vào Assets, Sếp chạy menu gắn.
