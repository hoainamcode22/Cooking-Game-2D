#!/usr/bin/env python3
"""
extract_mask.py — Trích silhouette mask từ 1 sheet Happy Harvest gốc (Dirt, Walkway...).
Dùng Otsu threshold trên độ sáng; nếu mask bị ngược thì thêm --invert.

  python extract_mask.py --src Sprite_Tiles_Dirt_tiles.png --out mask_dirt.png [--invert]

Kiểm tra file mask: vùng TRẮNG phải là hình blob (phần tile vẽ), ĐEN là nền.
"""
import argparse
import numpy as np
from PIL import Image

ap = argparse.ArgumentParser()
ap.add_argument("--src", required=True)
ap.add_argument("--out", required=True)
ap.add_argument("--invert", action="store_true")
args = ap.parse_args()

g = np.array(Image.open(args.src).convert("L")).astype(np.float64)
# Otsu
hist, _ = np.histogram(g, bins=256, range=(0, 256))
p = hist / hist.sum(); omega = np.cumsum(p); mu = np.cumsum(p * np.arange(256))
sigma = (mu[-1] * omega - mu) ** 2 / (omega * (1 - omega) + 1e-12)
t = np.argmax(sigma)
mask = g > t
if args.invert:
    mask = ~mask
Image.fromarray((mask * 255).astype(np.uint8)).save(args.out)
print(f"OK -> {args.out} (threshold={t}, blob={mask.mean()*100:.1f}%)")
