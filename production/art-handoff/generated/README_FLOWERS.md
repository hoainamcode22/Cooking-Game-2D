# GENERATED FLOWER ART — Cooking-Game-2D (đợt 2: 10 hoa × 5 stage)

Sinh bằng code vector theo `production/art-handoff/STYLE_CONTRACT.md`. Không dùng image_gen.

## Stage (hoa khác cây trồng ở stage 4)
1 vừa gieo · 2 mầm · 3 cây non (chỉ lá, chưa có cành) · **4 nụ** · 5 nở rộ

## QC — 10/10 PASS
- Chiều cao tăng đơn điệu stage 1→5 trên cả 10 loài ✓
- 0 sprite vượt ô 512px ✓
- Viền trắng 0.00% trên toàn bộ 50 file ✓
- Hue outline 24.1–27.1 (contract 15–46, không đen) ✓

## Quan trọng: hoa trồng trong CHẬU, không trồng ở ô đất
- Chậu `chauhoa_6` = 101×100px, đúng 1 cell. Điểm trồng (−3, +18) so với tâm chậu.
- Sprite **không vẽ chậu** — chậu là sprite riêng đã có sẵn.
- Đáy PNG = baseline → cắm đúng mặt đất trong chậu.
- `displayCount: 1` — mỗi chậu 1 khóm.

## Màu lạnh
Xanh lam / tím / lavender CHỈ dùng cho oải hương, hoa lan, cẩm tú cầu — đúng contract,
để 3 loài mở khoá muộn (L4/L7/L10) đọc ra "cao cấp".
