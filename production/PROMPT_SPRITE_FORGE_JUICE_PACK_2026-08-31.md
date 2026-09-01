# 🎨 PROMPT AGENT-SPRITE-FORGE — JUICE PACK (3 đợt hàng, 2026-08-31)
> Sếp dán NGUYÊN VĂN file này cho GPT điều hành agent-sprite-forge.
> Bàn giao vào: `production/art-handoff/2026-08-31_JuiceFX/` theo đúng thư mục con + TÊN FILE bên dưới.
> Sau khi giao, đội code sẽ tự import vào Resources — KHÔNG tự đặt file vào Assets/.

## 🎨 LUẬT ART STUDIO — BẮT BUỘC (dán kèm, không được bỏ)
1. ❌ TUYỆT ĐỐI KHÔNG TEXT: không chữ, không số, không logo trên BẤT KỲ asset nào. Text do game render TMP.
2. ❌ KHÔNG NỀN, KHÔNG BÓNG ĐỔ: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ.
3. ✅ Meta Unity chuẩn: spriteMode Single từng file; pivot CENTER cho toàn bộ pack này (là FX/icon/UI).
4. ✅ Frame animation: mọi frame CÙNG kích thước canvas, nhân vật đứng cùng vị trí; frame 01 = tư thế nghỉ;
   KHÔNG bake hiệu ứng lấp lánh/khói vào frame (code phun runtime).
5. ✅ Style chuẩn bộ Export_Train_UI_Package: cartoon dễ thương, outline nâu đậm, màu ấm bão hoà,
   burgundy #8E1F3B + đồng vàng #D9A441 làm điểm nhấn, thân thiện phụ nữ & trẻ em.
6. ✅ Giao đúng TÊN FILE + THƯ MỤC, không thêm file phụ (_single, @2x tự ý…).

---

## 📦 ĐỢT 1 — FX PHÁO HOA/CONFETTI (thư mục `1_Celebrate_FX/`, 7 file)
Tham chiếu: hiệu ứng chạm công trình mới xây kiểu Township — mảnh TO, RÕ, màu tươi.
| File | Size | Mô tả |
|---|---|---|
| confetti_01.png | 64×64 | Mảnh giấy chữ nhật cong nhẹ, ĐỎ tươi, outline mảnh |
| confetti_02.png | 64×64 | Mảnh giấy vuông xoắn, VÀNG #D9A441 |
| confetti_03.png | 64×64 | Dải ruy băng ngắn uốn sóng, XANH DA TRỜI |
| confetti_04.png | 64×64 | Mảnh tròn, XANH LÁ tươi |
| confetti_05.png | 64×64 | Ngôi sao 5 cánh mập, TÍM |
| confetti_06.png | 64×64 | Trái tim nhỏ mập, HỒNG |
| spark_star.png | 96×96 | Tia pháo hoa 4 cánh nhọn trắng-vàng, lõi sáng rực, rìa mềm (vẫn nét cartoon, KHÔNG blur ảnh thật) |

## 📦 ĐỢT 2 — BỘ ICON TIỀN TỆ THỐNG NHẤT (thư mục `2_Currency_Icons/`, 3 file)
Game đang có 6-7 icon vàng khác nhau (ob_coin, Icon_vang, icon_gold, stall_icon_coin…) — Sếp lệnh
GOM VỀ MỘT BỘ DUY NHẤT. Đây là icon sẽ xuất hiện ở MỌI NƠI: HUD, popup, hiệu ứng bay.
| File | Size | Mô tả |
|---|---|---|
| icon_gold_v2.png | 256×256 | Đồng xu vàng dày mặt nghiêng nhẹ 3/4, viền kép, mặt xu TRƠN BÓNG có 1 vệt highlight cong + 1 rãnh khắc hình ngôi sao nhỏ ở tâm (KHÔNG chữ, KHÔNG số, KHÔNG ký hiệu $ hay C), tông vàng #FFC93C→#D9A441, outline nâu đậm — nhìn "ngon, mập, muốn chạm" như xu Township/Hay Day |
| icon_gem_v2.png | 256×256 | Kim cương xanh ngọc mài giác đứng, các mặt giác rõ ràng sáng-tối, 1 tia lấp lánh nhỏ góc trên-trái, outline xanh đậm |
| coin_stack_v2.png | 256×256 | Chồng 3 xu icon_gold_v2 xếp lệch + 1 xu dựng nghiêng tựa vào — dùng cho ô thưởng lớn |

## 📦 ĐỢT 3 — 5 MASCOT LEVEL-UP, MỖI CON 12 FRAME (thư mục `3_LevelUp_Mascots/`, 60 file)
Tham chiếu video: mascot cười toe trong huy hiệu Level-Up, ăn mừng RẦM RỘ.
NHÂN VẬT PHẢI KHỚP 5 avatar CÓ SẴN của game (ảnh gốc đội code gửi kèm thư mục
`ref_avatars/` — lấy từ `Assets/Resources/Avatars/`): avatar_cowboy, avatar_chef_female,
avatar_flower_girl, avatar_boy, avatar_lumberjack. Vẽ THÊM PHẦN THÂN TRÊN (nửa thân, từ hông trở lên)
đúng khuôn mặt/màu tóc/trang phục avatar gốc.

- Mỗi nhân vật: **12 frame** ăn mừng liên hoàn để lặp mượt: 01 đứng cười (nghỉ) → 02-04 lấy đà
  cúi nhẹ → 05-07 BẬT nhảy lên hai tay vung cao → 08-09 đỉnh nhảy mắt nhắm cười toe →
  10-12 tiếp đất nhún rồi về tư thế 01. Cảm xúc: reo hò, phấn khích, miệng cười to.
- Canvas 256×256/frame, nhân vật chiếm ~85% cao, chân khung ở cùng toạ độ mọi frame.
- Tên file: `{id}/frame_01.png` … `{id}/frame_12.png` với id ∈
  {cowboy, chef_female, flower_girl, boy, lumberjack} (tạo 5 thư mục con đúng tên id).

## THỨ TỰ ƯU TIÊN GIAO: Đợt 2 (icon tiền — chặn task T2) → Đợt 1 → Đợt 3 (nhiều nhất, giao cuốn chiếu từng nhân vật được).
