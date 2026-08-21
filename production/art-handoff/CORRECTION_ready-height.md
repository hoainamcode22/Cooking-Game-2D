# HIỆU CHỈNH SPEC — chiều cao cây ready (kiểm chứng bằng ghép ảnh thật)

> Ghi ngày 2026-08-19, SAU khi ghép art mới lên `tile_dirt.png` với đúng 12 CropPoint đo từ scene.
> File này **ghi đè** con số "ready ~225×480px" trong `GEOMETRY_AND_STAGES.md` §2.4.

## Vấn đề phát hiện

Spec cũ (ready = 480px cao) cho ra cây cao **148% chiều cao ô đất** (509 / 345 texel).
Nhân với `displayCount`, ô đất **biến mất hoàn toàn**:

| Cấu hình | Kết quả ghép thật |
|---|---|
| 12 cây @ 480px (spec cũ, đang là mặc định của lúa/ngô/cà chua/mía/chanh/ớt/tiêu) | Đất bị chôn 100%, chỉ còn viền |
| 6 cây @ 480px | Vẫn chôn ~85% đất — **giảm displayCount là CHƯA ĐỦ** |
| **6 cây @ 278px** | ✅ Thấy rõ mặt đất, từng cây đọc riêng, ra đúng cảm giác "trồng từ dưới đất lên" |

## Số đã hiệu chỉnh

| Stage | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Ratio (giữ nguyên) | 18% | 30% | 52% | 76% | 100% |
| **Canvas cao (px) — MỚI** | 50 | 84 | 145 | 212 | **278** |
| Canvas cao (px) — cũ, BỎ | 86 | 144 | 250 | 365 | 480 |

- Cây ready cao **278px** = 80% chiều cao ô đất (thay vì 148%).
- Bề ngang giữ nguyên tỉ lệ → ready ~130px thay vì ~225px.
- **`displayCount` cho cây cao: 6** (không phải 12). Cây thấp (bắp cải, cà rốt, khoai tây, nấm) giữ 6.

## Cách áp dụng cho 135 sprite đã sinh

Art đã sinh ở canvas 480px. Có 2 đường, **không cần vẽ lại**:

1. **Đổi `plantScale` trong CropData** từ 70 → **40.5** (= 70 × 278/480). Nhanh nhất, 0 file art thay đổi.
2. Hoặc chạy lại generator với `HGT` mới — `production/art-handoff/generated/generator/crops.py`,
   sửa 1 dòng `HGT=[80,132,229,334,440]` → `HGT=[50,84,145,212,278]` rồi re-render.

Khuyến nghị **cách 1** — art hiện tại có độ phân giải cao hơn cần thiết, thu nhỏ khi render sẽ nét hơn
là vẽ lại ở canvas nhỏ (đúng nguyên tắc oversampling; ở max zoom-in ortho 400 vẫn đủ pixel).

## Vẫn cần sếp chốt
Giảm cây từ 148% → 80% chiều cao ô đất **đổi cảm giác nhìn của cả farm**. Xem file
`PLOT_TEST.png` (3 panel so sánh) rồi quyết. Mình chưa sửa `CropData` nào.
