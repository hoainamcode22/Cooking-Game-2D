#!/usr/bin/env python3
"""
make_iso_tileset.py — Sinh bộ tile ISOMETRIC 2:1 (128×64, kiểu Hay Day/Township)
từ texture seamless, theo ĐÚNG 48 rule config của RuleTile trong dự án.

Mỗi ô kim cương render trong canvas 128×80 (64 diamond + 16 skirt mép dày phía dưới).
Hình dáng blob sinh từ distance-field + noise TUẦN HOÀN theo tọa độ ô
=> hai tile cạnh nhau khớp biên từng pixel.

Kết quả: sheet 1024×480 (8×6 ô), xếp cùng vị trí lưới với sheet gốc
=> giữ nguyên internalID sprite => RuleTile clone chỉ cần đổi guid texture.
"""
import re, struct
import numpy as np
from PIL import Image, ImageFilter
from scipy.ndimage import distance_transform_edt

TILES = "/sessions/gracious-loving-rubin/mnt/Cooking-Game-2D/Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Tiles"

CELL_W, CELL_H, SKIRT = 128, 64, 16
CAN_W, CAN_H = 128, 80
SS = 2                      # supersample
FINE = 128                  # px fine-grid cho 1 cell (không gian vuông)

# ---------- parse rule + meta ----------
def parse_rules(asset_path):
    txt = open(asset_path, encoding='utf-8').read()
    rules = []
    for block in re.split(r"\n  - m_Id:", txt)[1:]:
        sid = int(re.search(r"m_Sprites:\s*\n\s*- \{fileID: (-?\d+)", block).group(1))
        hx = re.search(r"m_Neighbors: ([0-9a-fA-F]+)", block).group(1)
        vals = [struct.unpack('<i', bytes.fromhex(hx[i:i+8]))[0] for i in range(0, len(hx), 8)]
        pos = [(int(x), int(y)) for x, y in re.findall(r"- \{x: (-?\d+), y: (-?\d+)", block)]
        rules.append((sid, list(zip(pos, vals))))
    dflt = int(re.search(r"m_DefaultSprite: \{fileID: (-?\d+)", txt).group(1))
    return rules, dflt

def parse_meta_rects(meta_path):
    meta = open(meta_path, encoding='utf-8').read()
    out = {}
    for m in re.finditer(r"rect:\s*\n\s*serializedVersion: \d+\s*\n\s*x: (\d+)\s*\n\s*y: (\d+)\s*\n\s*width: (\d+)\s*\n\s*height: (\d+)[\s\S]*?internalID: (-?\d+)", meta):
        x, y, w, h, iid = map(int, m.groups())
        out[iid] = (x // 64, y // 64)   # (cột, hàng-từ-dưới)
    return out

# ---------- noise tuần hoàn theo tọa độ ô ----------
def _hash(ix, iy, seed):
    h = (ix.astype(np.int64) * 374761393 + iy.astype(np.int64) * 668265263 + seed * 1446648) & 0xffffffff
    h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff
    return ((h ^ (h >> 16)) & 0xffff) / 65535.0

def vnoise(u, v, seed):
    iu, iv = np.floor(u), np.floor(v)
    fu, fv = u - iu, v - iv
    fu = fu * fu * (3 - 2 * fu); fv = fv * fv * (3 - 2 * fv)
    iu = iu.astype(np.int64); iv = iv.astype(np.int64)
    n00 = _hash(iu, iv, seed); n10 = _hash(iu + 1, iv, seed)
    n01 = _hash(iu, iv + 1, seed); n11 = _hash(iu + 1, iv + 1, seed)
    return n00 * (1 - fu) * (1 - fv) + n10 * fu * (1 - fv) + n01 * (1 - fu) * fv + n11 * fu * fv

# ---------- field cho 1 config 3×3 ----------
def config_field(conds):
    """occupancy 3×3 (0=trống,1=đầy); don't-care coi như trống. Trả EDT (đv: cell)."""
    occ = np.zeros((3, 3), bool); occ[1, 1] = True
    for (dx, dy), v in conds:
        if v == 1: occ[1 + dy, 1 + dx] = True   # hàng = y, cột = x
    grid = np.zeros((3 * FINE, 3 * FINE), bool)
    for j in range(3):
        for i in range(3):
            if occ[j, i]:
                grid[j * FINE:(j + 1) * FINE, i * FINE:(i + 1) * FINE] = True
    return distance_transform_edt(grid) / FINE   # khoảng cách vào trong R

# ---------- render 1 tile iso ----------
def render_tile(conds, tex_top, ox, oy, inset, seed, thickness=8, shade=0.55, shadow=6):
    w, h = CAN_W * SS, CAN_H * SS
    px, py = np.meshgrid(np.arange(w) + 0.5, np.arange(h) + 0.5)
    X = px / SS - 64.0; Y = py / SS - 32.0            # tâm kim cương (64,32)
    u = 1.5 + (X / 64 - Y / 32) / 2                    # +x = lên-phải (khớp Unity iso)
    v = 1.5 + (-X / 64 - Y / 32) / 2
    field = config_field(conds)
    gi = np.clip((v * FINE).astype(int), 0, 3 * FINE - 1)
    gj = np.clip((u * FINE).astype(int), 0, 3 * FINE - 1)
    d = field[gi, gj]
    thr = inset + (vnoise(u * 7, v * 7, seed) - 0.5) * 0.11 + (vnoise(u * 19, v * 19, seed + 9) - 0.5) * 0.05
    m = d > thr
    # chỉ giữ trong kim cương trung tâm (nở 3px để 2 tile kề đè mí 1-2px, giấu seam)
    dia = (np.abs(X) / 64 + np.abs(Y) / 32) <= 1.0 + 3.0 / 64
    m &= dia

    top = np.zeros((h, w, 3), np.float32); alpha = np.zeros((h, w), np.float32)
    th_, tw_ = tex_top.shape[:2]
    ty = ((np.arange(h) // SS + oy) % th_); tx = ((np.arange(w) // SS + ox) % tw_)
    tex = tex_top[np.ix_(ty, tx)].astype(np.float32)

    if thickness > 0:  # mặt bên 3D phía dưới
        t = thickness * SS
        side = np.zeros_like(m); side[t:] = m[:-t]; side &= ~m
        top[side] = tex[side] * shade; alpha[side] = 1
        solid = m | side
    else:
        solid = m
    if shadow > 0:     # bóng mềm
        s = shadow * SS
        sh = np.zeros_like(solid); sh[s:] = solid[:-s]; sh &= ~solid
        shf = np.array(Image.fromarray((sh * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2.0)), np.float32) / 255 * 0.35
        alpha = np.maximum(alpha, shf)
    top[m] = tex[m]; alpha[m] = 1

    rgba = np.dstack([top, alpha[..., None] * 255]).astype(np.uint8)
    return np.array(Image.fromarray(rgba, "RGBA").resize((CAN_W, CAN_H), Image.LANCZOS))

# ---------- build 1 sheet ----------
def build_sheet(rule_asset, meta_path, tex_path, out_png, inset, seed, thickness, shadow):
    rules, dflt = parse_rules(rule_asset)
    pos = parse_meta_rects(meta_path)
    tex = np.array(Image.open(tex_path).convert("RGB"))
    cfg = {sid: conds for sid, conds in rules}
    all_same = [((dx, dy), 1) for dx in (-1, 0, 1) for dy in (-1, 0, 1) if (dx, dy) != (0, 0)]
    sheet = np.zeros((6 * CAN_H, 8 * CAN_W, 4), np.uint8)
    for k, (iid, (gx, gy)) in enumerate(pos.items()):
        conds = cfg.get(iid, all_same)
        ox, oy = (k * 53) % tex.shape[1], (k * 97) % tex.shape[0]
        tile = render_tile(conds, tex, ox, oy, inset, seed, thickness, 0.55, shadow)
        tp = (6 * CAN_H) - (gy * CAN_H) - CAN_H
        sheet[tp:tp + CAN_H, gx * CAN_W:(gx + 1) * CAN_W] = tile
    Image.fromarray(sheet, "RGBA").save(out_png)
    print("OK ->", out_png, sheet.shape)
    return rules, pos

if __name__ == "__main__":
    # CỎ: có mép dày + bóng (nổi trên nền đất)
    build_sheet(f"{TILES}/Grass/RuleTile_GrassDarker.asset",
                f"{TILES}/Grass/Sprites/Sprite_Tiles_Grass_darker_tiles.png.meta",
                "texture_grass_50.png", "Sheet_IsoGrass45.png",
                inset=0.16, seed=11, thickness=7, shadow=5)
    # ĐẤT: phẳng (vẽ đường đất lên trên cỏ)
    build_sheet(f"{TILES}/Dirt/RuleTile_Dirt.asset",
                f"{TILES}/Dirt/Sprites/Sprite_Tiles_Dirt.png.meta",
                "texture_dirt_50.png", "Sheet_IsoDirt45.png",
                inset=0.13, seed=23, thickness=0, shadow=0)
