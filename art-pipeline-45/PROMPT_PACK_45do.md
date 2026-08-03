# Bộ Prompt + Pipeline: Asset nghiêng 45° kiểu Hay Day / Township

Dựa trên khảo sát dự án của bạn:

| Thông số hiện tại | Giá trị |
|---|---|
| Sheet tile | 512×384 px, lưới 8×6 → tile **64×64 px** |
| Pixels Per Unit | **64** |
| Autotile | **RuleTile** (2D Tilemap Extras), ~47 rule blob |
| Grid trong scene | `Grid_Map_45` — Rectangular, cell 1×1 |
| Palette | `_Palettes/Palette_Main.prefab` |

## 1. Nguyên tắc quan trọng (đọc trước)

**Hay Day / Township KHÔNG dùng tile nghiêng thật.** Nền đất vẫn là tile phẳng
nhìn từ trên xuống. Cảm giác "nghiêng 45°" đến từ 3 thứ:

1. **Nhà / cây / decor vẽ góc 3/4** (thấy mặt trước + mái) và **đổ bóng** nhất quán.
2. **Mép tile có "độ dày"** — viền tối phía dưới như đất nhô lên vài cm.
3. **Y-sorting**: vật ở dưới màn hình che vật ở trên.

→ Bạn **giữ nguyên** Grid Rectangular, RuleTile, Palette hiện tại. Chỉ thay art.

**Không nhờ AI vẽ nguyên sheet tile** — AI không canh được lưới 64px và các rule,
tile sẽ không khớp nhau. Thay vào đó dùng pipeline 2 bước:

```
AI vẽ TEXTURE SEAMLESS (lặp vô hạn)  →  make_tilesheet_45.py cắt vào silhouette
                                         của sheet gốc (mask_blob47.png)
                                      →  sheet mới khớp RuleTile 100%, nối liền từng pixel
```

Vì silhouette lấy đúng từ sheet gốc nên các tile nối nhau y như bộ Happy Harvest,
chỉ khác chất liệu + có mép dày 3D.

## 2. Style guide chung (chèn vào MỌI prompt)

Khớp với ảnh reference của bạn:

```
Casual mobile farm game art style like Hay Day and Township, hand-painted,
soft painterly cartoon, vibrant warm saturated colors, clean smooth shapes,
no outlines, sunlight from top-right, soft shadows falling to bottom-left,
high detail, crisp edges, no photo textures, no noise grain
```

**Bóng đổ luôn về HƯỚNG DƯỚI-TRÁI** trên mọi asset — sai hướng 1 asset là lệch cả map.

## 3. Nhóm A — Texture nền seamless (cỏ, đất, đường, nước)

AI chỉ cần vẽ texture vuông lặp được. Script lo phần tile.

### Midjourney (tốt nhất cho texture — có `--tile`)

```
top-down seamless grass texture for a casual mobile farm game, hand-painted
cartoon style like Hay Day, fresh vibrant green with subtle tufts and tiny
flowers, soft painterly, even lighting, no shadows, no outlines
--tile --ar 1:1 --v 6 --stylize 200
```

Đổi chất liệu bằng cách thay dòng đầu:
- Đất: `top-down seamless dirt soil texture, warm brown with small pebbles and subtle cracks`
- Đường lát đá: `top-down seamless cobblestone path texture, rounded beige stones with sandy gaps`
- Nước: `top-down seamless water texture, bright blue with soft cartoon ripples and light caustics`
- Đất trồng: `top-down seamless tilled farm soil texture, dark rich brown with neat furrow rows`

### ChatGPT / GPT-Image

```
Create a SEAMLESS TILEABLE square texture (must repeat perfectly on all 4 edges
with zero visible seams). Top-down view of [grass / dirt / cobblestone / water]
for a casual mobile farm game like Hay Day. Hand-painted cartoon style, vibrant
warm colors, soft painterly, even flat lighting, no shadows, no outlines,
no vignette. The pattern must have uniform density — no large focal elements.
Output 1024x1024.
```

### Gemini (Imagen)

```
Seamless repeating texture, top-down [grass] for a mobile farm game, Hay Day
art style, hand-painted cartoon, vibrant green, even lighting, no shadow,
edges must tile perfectly, 1024x1024
```

**Kiểm tra seamless**: mở ảnh trong trình sửa ảnh, dịch (offset/wrap) 50% ngang + dọc
— nếu thấy vệt nối thì bảo AI "fix the visible seam" hoặc render lại.

### Chạy script

```bash
# Bản đè lên tilemap nền (khuyên dùng — grass đè lên dirt tilemap):
python make_tilesheet_45.py --top grass_ai.png --transparent --out Sprite_Tiles_Grass_45.png

# Hoặc bản nền đục như bộ gốc:
python make_tilesheet_45.py --top grass_ai.png --base dirt_ai.png --out Sprite_Tiles_Grass_45.png

# Chỉnh độ dày 3D và bóng: --thickness 4 --side-shade 0.62 --shadow 3
```

Sheet khác (Dirt, Walkway...): trích mask từ sheet gốc tương ứng bằng
`extract_mask.py --src <sheet gốc> --out mask_dirt.png` rồi truyền `--mask mask_dirt.png`.

## 4. Nhóm B — Nhà / công trình góc 3/4 (đây mới là thứ tạo cảm giác 45°)

Mỗi công trình là **1 sprite riêng** (không phải tile), nền trong suốt, bóng vẽ sẵn.

### Midjourney

```
isometric 3/4 view of a cozy farm restaurant building for a casual mobile game,
Hay Day and Township style, red tiled roof, cream walls, striped awning, wooden
sign, flower pots, hand-painted cartoon, vibrant warm colors, soft shadow cast
to the bottom-left, isolated on plain white background, full building visible,
centered --ar 1:1 --v 6 --stylize 250 --no scene, ground, grass, sky
```

### ChatGPT / GPT-Image (kiểm soát tốt nhất cho sprite)

```
Game sprite, transparent background PNG. A [farm restaurant / barn / windmill /
market stall] viewed from a 45-degree high angle (3/4 top-down like Hay Day):
front facade AND roof both visible. Hand-painted casual cartoon, vibrant warm
colors, no outlines. Bake a soft drop shadow on the ground to the BOTTOM-LEFT
of the building. Nothing else in the image — no ground, no grass, no sky.
The bottom edge of the building must sit flat so it can be placed on a tilemap.
```

### Gemini

```
Single game asset on transparent background: [building] in 3/4 top-down view,
45 degree angle, Hay Day style, hand-painted cartoon, front and roof visible,
soft shadow to bottom-left, no environment
```

Thay `[...]`: nhà hàng, kho (warehouse), nhà dân, hàng rào, giếng nước, bến tàu,
cột đèn, biển gỗ, xe hàng... Cây cối dùng cùng khung prompt:

```
... A round leafy tree, 3/4 top-down view, lush green cartoon foliage with
painted highlights, brown trunk visible at bottom, soft shadow to bottom-left ...
```

## 5. Nhóm C — Vật trang trí nhỏ lấp đất trống

```
Sprite sheet of small farm decorations viewed from 3/4 top-down angle, Hay Day
style: rocks, bushes, flowers, tree stumps, wooden crates, hay bales, each with
soft shadow to bottom-left, transparent background, evenly spaced grid
```

Cắt tay từng sprite sau khi sinh (AI xếp lưới không đều — đừng tin kích thước ô).

## 6. Import vào Unity (khớp cấu hình dự án của bạn)

### Sheet tile mới
1. Texture Type: **Sprite (2D and UI)** · Sprite Mode: **Multiple** · PPU: **64**
   · Filter: Bilinear · Compression: None (hoặc High Quality).
2. Sprite Editor → Slice → **Grid By Cell Size 64×64** → Apply.
3. **Cách nhanh nhất**: đặt tên file y hệt và **ghi đè PNG gốc** (giữ nguyên `.meta`)
   → RuleTile tự nhận sprite mới, không phải nối lại 47 rule.
   Muốn giữ bộ cũ: duplicate `RuleTile_GrassDarker.asset`, mở Inspector,
   kéo sprite mới vào từng rule theo đúng thứ tự cũ (layout ô giống hệt nên
   vị trí nào thay vị trí đó).
4. Nếu dùng bản `--transparent`: xếp layer tilemap
   `Underwater → Water → Dirt/Base → Grass → Walkway` (Order in Layer tăng dần).

### Sprite nhà / decor 3/4
1. Pivot: **Bottom** (chân công trình).
2. SpriteRenderer, Sorting Layer riêng (vd `Objects`) nằm trên các tilemap.
3. Bật **Y-sorting**: Project Settings → Graphics → Camera Settings →
   Transparency Sort Mode = **Custom Axis**, Axis = **(0, 1, 0)**.
   (URP: chỉnh trong URP Asset / Camera → Renderer.)
4. Đặt kích thước theo footprint: 1 ô = 64px. Nhà 3×3 ô → sprite rộng ~192px
   (phần mái/bóng được tràn ra ngoài footprint).
5. Bóng đã bake trong sprite — không cần shadow runtime.

### Scene
Giữ nguyên `Grid_Map_45` (Rectangular, cell 1×1). Không đổi sang Isometric.

## 7. Checklist chất lượng

- [ ] Texture nền: dịch wrap 50% không thấy vệt nối
- [ ] Mọi asset: nắng từ trên-phải, bóng về dưới-trái
- [ ] Mép tile có viền dày tối phía dưới (script tự làm)
- [ ] Nhà: thấy đồng thời mặt trước + mái, chân nhà phẳng
- [ ] Cùng bảng màu ấm bão hòa giữa các asset (giữ 1 ảnh reference khi prompt:
      up kèm ảnh screenshot mẫu của bạn và ghi "match this art style exactly")

## 8. File trong bộ này

| File | Công dụng |
|---|---|
| `make_tilesheet_45.py` | Ghép texture AI → sheet 47-blob khớp RuleTile, thêm mép 3D |
| `extract_mask.py` | Trích mask từ các sheet gốc khác (Dirt, Walkway...) |
| `mask_blob47.png` | Mask trích sẵn từ sheet Grass darker của bạn |
| `demo_sheet_opaque.png` | Demo sheet nền đục (texture demo tự sinh) |
| `demo_sheet_transparent.png` | Demo sheet nền trong suốt |
| `demo_texture_grass.png` / `demo_texture_dirt.png` | Texture demo để test script |
