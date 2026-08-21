# GENERATED CROP ART — Cooking-Game-2D (đợt 1: 11 cây × 5 stage)

Sinh bằng code vector (SVG → Chromium), theo `production/art-handoff/STYLE_CONTRACT.md`.
KHÔNG dùng image_gen. Ngày 2026-08-19.

## Nội dung
- `crops/<cropId>/<cropId>_s1..s5.png` — 55 sprite, nền trong suốt
- `crops/<crop>_5stage.png` — strip xem trước 5 stage
- `crops/manifest.json` — kích thước + pivot + PPU từng sprite
- `generator/` — mã nguồn sinh art (sửa 1 HEX → re-render toàn bộ)

## Quy cách sprite (đã kiểm, 11/11 PASS)
- **Đáy PNG = baseline y=460** → import pivot `(0.5, 0)` là gốc cây chạm đúng CropPoint.
- Cây căn giữa ngang: tâm ảnh = tâm thân cây.
- Không sprite nào vượt ô 512px (0 clipped).
- Viền trắng: **0.00%** trên cả 55 file.
- Hue outline: 24.4–26.0 (contract yêu cầu 15–46, không đen). ✓
- Saturation trung bình: 52.8–60.4%.

## Import Unity
```
spritePixelsToUnits : 100
spriteMode          : 1 (Single)
alignment           : 7 (Custom)
pivot               : {x: 0.5, y: 0}
alphaIsTransparency : 1
filterMode          : 1 (Bilinear)
```

## CHƯA gắn được vào game
`CropData` hiện chỉ có 3 field sprite (sprout/growing/ready). Cần changeset 3→5 stage ở
`production/art-handoff/GEOMETRY_AND_STAGES.md` §4 — nằm trong DANH SÁCH DỪNG, chờ duyệt.

Tạm thời map 3 stage: s1→sprout, s3→growing, s5→ready.

## Tỉ lệ chiều cao thực đo (% so với stage 5)
| cây | s1 | s2 | s3 | s4 | s5 |
|---|---|---|---|---|---|
| rice | 19 | 37 | 54 | 74 | 100 |
| bapcai | 26 | 48 | 57 | 78 | 100 |
| ngo | 19 | 38 | 57 | 69 | 100 |
| cachua | 18 | 35 | 57 | 80 | 100 |
| carot | 24 | 47 | 57 | 78 | 100 |
| khoaitay | 18 | 35 | 57 | 80 | 100 |
| nam | 20 | 35 | 55 | 77 | 100 |
| mia | 18 | 34 | 59 | 84 | 100 |
| chanh | 18 | 35 | 58 | 80 | 100 |
| ot | 18 | 36 | 56 | 79 | 100 |
| tieu | 18 | 35 | 63 | 85 | 100 |
