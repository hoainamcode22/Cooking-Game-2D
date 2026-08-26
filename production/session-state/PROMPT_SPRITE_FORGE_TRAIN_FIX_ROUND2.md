# PROMPT GỬI GPT (agent-sprite-forge) — SỬA BỘ TÀU WORLD (round 2) + BỘ CHIỀU VỀ
> ĐÍNH KÈM & TUÂN THỦ: `production/ART_RULES_STUDIO.md` (luật mới: KHÔNG TEXT, KHÔNG NỀN/BÓNG trên mọi asset).

## VIỆC 1 — DẶM SẠCH 12 FILE ĐÃ GIAO (ghi đè đúng tên cũ, cùng thư mục WorldTrain/)
`world_loco_upright_01..06.png` + `world_wagon_upright_01..06.png`:
- XOÁ toàn bộ chữ nướng cứng: "FARM EXPRESS", "No.3" trên đầu tàu; "HARVEST TRANSPORT" trên toa
  → thay bằng biển gỗ/tấm ốp TRỐNG cùng màu.
- XOÁ 100% bóng đổ/nền trắng-xám dưới gầm tàu — alpha phải trong suốt tuyệt đối quanh silhouette.
- GIỮ NGUYÊN pose, kích thước canvas, vị trí thân, nhịp animation từng frame (chỉ dặm sạch, không vẽ lại).
- XÓA 2 file thừa `world_loco_upright_single.png`, `world_wagon_upright_single.png`.

## VIỆC 2 — VẼ BỘ CHIỀU VỀ (frontleft) — 12 file MỚI, cùng thư mục
Mô tả 2 chiều CHUẨN (theo 2 ảnh Sếp gửi 2026-08-26):
- CHIỀU ĐI (upright — đã có): đầu tàu quay PHẢI-LÊN, mũi tàu + đèn pha + ống khói nhìn về góc phải-trên,
  toa nối PHÍA SAU ở góc trái-dưới.
- CHIỀU VỀ (frontleft — cần vẽ): NGƯỢC HẲN LẠI — đầu tàu quay TRÁI-XUỐNG, mũi tàu + đèn pha + lưới gạt
  nhìn về góc trái-dưới, cabin ở phía sau, toa nối PHÍA SAU ở góc phải-trên. Cùng độ dốc trục ray
  với bộ upright (nhìn 2 bộ phải như 1 con tàu chạy 2 chiều trên CÙNG 1 đường ray chéo).
File: `world_loco_frontleft_01..06.png` (bánh quay + nhún, frame 01 nghỉ)
      `world_wagon_frontleft_01..06.png` (toa gỗ chở nông sản như bộ upright, nhún lệch pha)
Cùng đầu tàu burgundy/toa gỗ như upright — chỉ đổi hướng. Không text, không bóng, canvas đồng nhất,
meta Single + pivot Bottom-Center (lần trước làm đúng — giữ vậy).
