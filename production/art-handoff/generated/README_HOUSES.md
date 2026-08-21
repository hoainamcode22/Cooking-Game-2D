# GENERATED HOUSE ART v2 — 5 nhà × 6 stage (30 sprite)

## 6 stage
1 móng+khung+dàn giáo · 2 bắt đầu xây (tường 1/3 + vật liệu) · 3 xây nửa (mái lợp một phần,
dàn giáo một bên) · **4 HOÀN CHỈNH** · 5 gói hộp quà · 6 hộp bung lộ nhà

## Stage 4 khớp bản gốc — THAY THẲNG ĐƯỢC, không đổi localScale
| Nhà | Sprite gốc (px) | Stage 4 mới (px) | Lệch |
|---|---|---|---|
| House_01 | 312 × 384 | 312 × 380 | 0% / −1.0% |
| House_02 | 287 × 424 | 290 × 419 | +1.0% / −1.2% |
| House_03 | 394 × 416 | 388 × 411 | −1.5% / −1.2% |
| House_04 | 278 × 406 | 282 × 402 | +1.4% / −1.0% |
| House_05 | 340 × 388 | 340 × 384 | 0% / −1.0% |

Sai số ≤1.5% cả 2 chiều → giữ nguyên `localScale 100`, footprint 4×4 cell vẫn đúng,
**15 instance trong SCN_Farm không vỡ**.

## QC — 5/5 PASS
- 0/30 sprite vượt ô 512px (dàn giáo + hộp quà rộng/cao hơn nhà nên đây là ràng buộc khó nhất)
- Viền trắng 0.00% trên cả 30 file
- Hue outline 23–27 (contract 15–46, không đen)
- Baseline chung y=470 mọi stage → **không giật khi đổi stage** (luật shared-crop-rect,
  `MayAnimSetupTool.cs:39-44`)

## Nhận diện 5 nhà (giữ đúng bản gốc)
| | Mái | Tường | Gói quà |
|---|---|---|---|
| House_01 | gambrel xanh ngọc `#3F8478` | ivory board-and-batten | san hô + vàng |
| House_02 | gable dốc navy `#2E3648` | clapboard xanh xám | hổ phách + đỏ |
| House_03 | gable rộng nâu socola `#6B4126` | lap siding hổ phách `#D9963C` | xanh rừng + vàng |
| House_04 | hip kem `#EEDCB6` | coral `#E0714F` | mận + kem |
| House_05 | gable xanh rêu `#7EA05C` | olive + khung gỗ cam nâu | đỏ thẫm + vàng |

## Stage 5–6 gần như KHÔNG cần code mới
`ConstructionCompleteFX.BuildGiftBox()` đã dựng hộp + ruy băng + nơ tự khớp footprint,
timeline 2.10s. Thay 3 slot `GiftBoxSide`/`Ribbon`/`Rosette` trong `ConstructionArtKit.asset`
là nâng cấp cho MỌI công trình, **0 dòng code**.

## Còn thiếu (cần code, chưa làm)
`ConstructionSiteVisuals.Build()` không nhận tham số tiến độ · `PlaceableItemData` không có field stage.
Muốn stage 1→3 tự đổi theo timer 30s phải thêm code — nằm trong DANH SÁCH DỪNG, chờ duyệt.
