#!/usr/bin/env python3
"""
make_tilesheet_45.py — Ghép texture AI thành tile sheet 47-blob khớp RuleTile Happy Harvest.

Ý tưởng: KHÔNG nhờ AI vẽ cả sheet (AI không canh được lưới 64px).
Thay vào đó:
  1. Nhờ AI vẽ 1 texture SEAMLESS (lặp vô hạn) cho chất liệu (cỏ, đất, đường...).
  2. Script này cắt texture đó vào ĐÚNG silhouette của sheet gốc (mask_blob47.png),
     nên mọi tile nối với nhau hoàn hảo như bộ gốc — vì hình dáng viền không đổi.
  3. Tự thêm "độ dày 3D" (extrude xuống dưới + bóng) để có cảm giác nghiêng 45°
     kiểu Hay Day/Township.

Cách dùng:
  python make_tilesheet_45.py --top grass_new.png --base dirt_new.png --out sheet.png
  python make_tilesheet_45.py --top grass_new.png --transparent --out sheet.png
Tùy chọn:
  --mask mask_blob47.png   (mặc định: file cạnh script)
  --cell 64                kích thước 1 tile (px)
  --thickness 4            độ dày mép 3D phía dưới (px). 0 = tắt
  --side-shade 0.62        độ tối của mặt bên (0..1, nhỏ = tối hơn)
  --shadow 3               bóng đổ mềm dưới mép (px). 0 = tắt   (chỉ khi --transparent)
"""
import argparse, os
import numpy as np
from PIL import Image, ImageFilter


def tile_to(tex: Image.Image, w: int, h: int) -> np.ndarray:
    """Lặp (wrap) texture để phủ kín w×h — giữ tính seamless, không stretch."""
    t = np.array(tex.convert("RGB"), dtype=np.float32)
    th, tw = t.shape[:2]
    reps_y, reps_x = -(-h // th), -(-w // tw)
    return np.tile(t, (reps_y, reps_x, 1))[:h, :w]


def per_cell_shift_down(mask: np.ndarray, cell: int, t: int) -> np.ndarray:
    """Dịch mask xuống t px TRONG TỪNG Ô 64px (clamp tại biên ô).
    Nhờ clamp, tile nào có blob chạm biên ô sẽ không bị lem sang ô khác,
    và khi ghép trên map các mép vẫn liền mạch."""
    h, w = mask.shape
    out = mask.copy()
    for cy in range(0, h, cell):
        block = mask[cy:cy + cell]
        shifted = np.zeros_like(block)
        shifted[t:] = block[:-t] if t < block.shape[0] else 0
        # clamp: hàng đầu ô giữ nguyên giá trị gốc (blob chạm biên trên tiếp tục)
        shifted[:t] = block[:t]
        out[cy:cy + cell] = np.maximum(block, shifted)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--top", required=True, help="texture seamless cho bề mặt (cỏ...)")
    ap.add_argument("--base", help="texture seamless cho nền dưới (đất...)")
    ap.add_argument("--transparent", action="store_true", help="nền trong suốt (RGBA)")
    ap.add_argument("--mask", default=os.path.join(os.path.dirname(__file__), "mask_blob47.png"))
    ap.add_argument("--out", required=True)
    ap.add_argument("--cell", type=int, default=64)
    ap.add_argument("--thickness", type=int, default=4)
    ap.add_argument("--side-shade", type=float, default=0.62)
    ap.add_argument("--shadow", type=int, default=3)
    args = ap.parse_args()

    mask_img = Image.open(args.mask).convert("L")
    w, h = mask_img.size
    mask = (np.array(mask_img) > 127)

    top = tile_to(Image.open(args.top), w, h)

    if args.transparent:
        base = np.zeros((h, w, 3), np.float32)
        base_a = np.zeros((h, w), np.float32)
    elif args.base:
        base = tile_to(Image.open(args.base), w, h)
        base_a = np.ones((h, w), np.float32)
    else:
        raise SystemExit("Cần --base <texture> hoặc --transparent")

    out = base.copy()
    alpha = base_a.copy()

    # 1) Mặt bên 3D: vùng ngay DƯỚI mép blob = top texture tối đi (cảm giác độ dày 45°)
    if args.thickness > 0:
        grown = per_cell_shift_down(mask, args.cell, args.thickness)
        side = grown & ~mask
        out[side] = top[side] * args.side_shade
        alpha[side] = 1.0
    else:
        side = np.zeros_like(mask)

    # 2) Bóng mềm dưới mặt bên (chỉ chế độ transparent — bản đè lên tilemap nền)
    if args.transparent and args.shadow > 0:
        solid = mask | side
        sh = per_cell_shift_down(solid, args.cell, args.shadow) & ~solid
        sh_img = Image.fromarray((sh * 255).astype(np.uint8)).filter(
            ImageFilter.GaussianBlur(1.2))
        sh_a = np.array(sh_img, np.float32) / 255.0 * 0.35
        keep = alpha < sh_a
        out[keep] = 0
        alpha = np.maximum(alpha, sh_a)

    # 3) Bề mặt blob
    out[mask] = top[mask]
    alpha[mask] = 1.0

    if args.transparent:
        rgba = np.dstack([out, alpha[..., None] * 255]).astype(np.uint8)
        Image.fromarray(rgba, "RGBA").save(args.out)
    else:
        Image.fromarray(out.astype(np.uint8), "RGB").save(args.out)
    print("OK ->", args.out, f"({w}x{h}, cell {args.cell}px)")


if __name__ == "__main__":
    main()
