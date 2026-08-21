# ART HANDOFF — INDEX

## Tài liệu
| File | Nội dung |
|---|---|
| `STYLE_CONTRACT.md` | Style pin vào 2 file thật trong project + palette đo từ 1.24M pixel |
| `GEOMETRY_AND_STAGES.md` | Hình học ô đất/chậu/nhà, mô hình 5 stage, changeset code 3→5, 10 bug |
| `CORRECTION_ready-height.md` | ⚠️ HIỆU CHỈNH: ready 480px → **278px**, displayCount 12 → 6 |

## Art đã sinh — `generated/` (135 sprite)
| Nhóm | Số | Đường dẫn |
|---|---|---|
| Cây trồng 11 loại × 5 stage | 55 | `generated/crops/<cropId>/<cropId>_s1..s5.png` |
| Hoa 10 loại × 5 stage | 50 | `generated/flowers/<id>/<id>_s1..s5.png` |
| Nhà 5 kiến trúc × 6 stage | 30 | `generated/houses/house_0N/house_0N_s1..s6.png` |

Xem nhanh: `generated/ALL_CROPS.png` · `ALL_FLOWERS.png` · `ALL_HOUSES.png` · `PLOT_TEST.png`

## Generator — `generated/generator/`
Python + SVG, không phụ thuộc image_gen. Sửa 1 mã HEX → re-render toàn bộ.
`pg2.py` (primitive) · `crops.py` `allcrops.py` `run_all.py` (cây) · `flowers.py` `run_flowers.py` (hoa)
· `houses.py` `run_houses.py` (nhà) · `export_sprites.py` `fexport.py` (xuất PNG trong suốt)

## QC — tất cả PASS
- 0/135 sprite vượt ô 512px
- Viền trắng **0.00%** trên cả 135 file (art cũ trong project lên tới 78%)
- Hue outline 23.3–27.1 (contract 15–46, không đen)
- Chiều cao tăng đơn điệu stage 1→5 trên cả 21 cây/hoa
- Nhà: 6 stage cùng baseline y=470 → không giật khi đổi stage

## Prompt pack cho image_gen (nếu muốn bản painterly)
`E:\agent-sprite-forge\sandbox\cooking-farm-2d\` — 30 file prompt + RUNBOOK + HANDOFF_MESSAGE
+ `scripts/verify_sheet.py`. Mở Codex, dán `HANDOFF_MESSAGE.md`.

## CHƯA gắn vào game
`CropData` chỉ có 3 field sprite. Cần changeset 3→5 stage (`GEOMETRY_AND_STAGES.md` §4) —
nằm trong DANH SÁCH DỪNG của `AUTONOMY.md`, chờ sếp duyệt. Chưa sửa dòng code nào.
