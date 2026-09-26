# Prompt vẽ công trình mới (bản 2, SÁNG như quầy hàng)

## Cập nhật 25/09: Sếp chốt SÁNG như quầy hàng

Bỏ hướng "tối như chợ cũ". Chợ và máy xay đã được làm sáng lên cho khớp quầy hàng.
Nhà hàng và 3 chuồng bản 1 **đã sáng đúng như quầy hàng**, không bắt buộc vẽ lại.
Chỉ dùng prompt này khi muốn vẽ công trình MỚI. Ảnh mẫu phong cách: `stand_base_opt.png` (quầy hàng).

## Bắt buộc

- **Dán kèm ảnh mẫu** quầy hàng `Assets/Art/Buildings/FarmStand/_Opt/stand_base_opt.png` vào công cụ vẽ (style reference, độ ảnh hưởng cao).
- Vẽ đúng 1 công trình mỗi lần. Nền **trong suốt thật**: không nền trắng, không bóng đổ xám lan ra ngoài chân nhà, không nền trắng lọt giữa các song rào.
- Canvas 1024x1024, công trình nằm giữa, cách mép khoảng 40px.
- Không chữ, không logo.

## Luật sprite sheet (giữ như bản 1)

- N frame xếp **1 hàng ngang**, các ô bằng nhau, nền trong suốt.
- Mọi frame vẽ vật **cùng vị trí, cùng cỡ**, điểm treo hoặc trục quay **đúng cùng 1 pixel**.
- Vẽ đúng tỉ lệ khi gắn lên nhà. Kèm 1 ảnh `*_reference.png` ghép sẵn nhà + các lớp.
- **Khói ống khói, ánh đèn nhấp nháy: KHÔNG cần vẽ**, em làm bằng code.

---

## KHỐI PHONG CÁCH (dán đầu mọi prompt)

```
Match EXACTLY the art style of the attached reference image (a bright farm market stall from the
same game). Bright, sunny, glossy casual mobile farm game art, isometric 3/4 view, 2:1 diamond
footprint, building faces bottom-left and bottom-right.
Warm golden-orange wood with visible grain, clean dark-brown outlines, cheerful saturated colors,
soft cel shading with bright highlights, light from top-left, crisp and clean like the reference.
PNG with fully transparent background, no ground grass, no white background, no grey cast shadow
outside the footprint, no white gaps between fence rails, no text, no watermark.
```

---

## 1. Nhà hàng (Restaurant), chân 3x3 ô

**restaurant_base.png**
```
A cozy two-story village restaurant bright and cheerful like the reference:
grey cobblestone foundation, cream plaster walls framed by warm golden wooden beams, bright
red-orange clay tile roof with thick overhanging edges, a stone chimney (no smoke), a big front window
with a warm-lit EMPTY interior, a wooden door, a stone pizza oven built into the right side wall
with a DARK EMPTY oven mouth (no fire), an outdoor wooden counter with two cooking pots (no steam),
a hanging iron lantern like the reference, flower boxes under the windows. Static.
```

**restaurant_oven_fire_sheet.png**: 6 frame, ô 256x256
```
Warm orange-yellow fire flames inside a dark stone oven mouth, sprite sheet 6 frames in ONE
horizontal row, each cell 256x256, flames flicker and change shape, the flame base is at EXACTLY
the same pixels in every frame, same scale. Style of the attached reference.
```

**restaurant_pot_steam_sheet.png**: 4 frame, ô 256x256
```
2-3 soft white steam curls rising from a cooking pot (pot NOT included, only steam),
sprite sheet 4 frames in ONE horizontal row, each cell 256x256, steam base at EXACTLY the same
pixel every frame, curls rise and change shape.
```

---

## 2. Chuồng bò (Cow Barn), chân 3x3 ô

**cow_barn_base.png**
```
A sturdy cow barn in the style of the reference: warm golden wooden frame, vertical
wooden plank walls, bright red-orange clay tile roof with thick edges, a big arched double door
open with hay inside (dark interior, no cow), grey stone foundation. In front: a fenced yard
(thick dark wooden fence posts and rails, NO white gaps between rails, the ground between rails
must be dirt or transparent), a wooden hay feeder and a stone water trough. The yard takes the
front 2/3 of the diamond footprint, the barn sits at the back corner. Static.
```

**cow_barn_weathervane_sheet.png**: 6 frame, ô 256x256
```
A small golden-brass rooster weathervane on a thin pole, sprite sheet 6 frames in ONE horizontal
row, each cell 256x256, the rooster rotates around the pole, pole base at EXACTLY the same pixel
(bottom center) every frame.
```

---

## 3. Chuồng gà (Chicken Coop), chân 2x2 ô

**chicken_coop_base.png**
```
A raised wooden chicken coop on warm golden wooden stilts in the style of the reference,
warm plank walls, bright red-orange tile roof, a small ramp with slats, nesting box on the side
with straw, a long wooden grain trough and a clay water dish. Fenced yard with thick dark wooden
posts and rails (NO white gaps between rails). Coop at the back corner, yard in front. Static.
```

**chicken_coop_weathervane_sheet.png**: 6 frame, ô 256x256 (giống chuồng bò, con gà nhỏ hơn)

---

## 4. Chuồng heo (Pig Pen), chân 3x3 ô

**pig_pen_base.png**
```
A rustic pig shed in the style of the reference: thick dark wooden frame, grey weathered plank
walls, a thick straw thatched roof in muted golden-brown, arched opening with straw inside
(dark interior, no pig), grey stone foundation. Fenced yard with warm golden wooden posts and rails
(NO white gaps), a muddy puddle in the right corner, a wooden feeding trough with vegetables.
Shed at the back corner, yard in front. Static.
```

**pig_pen_mud_bubbles_sheet.png**: 6 frame, ô 256x128
```
A few brown mud bubbles forming and popping on a flat mud puddle surface (puddle NOT included,
only bubbles), sprite sheet 6 frames in ONE horizontal row, each cell 256x128, same positions
every frame, only bubbles grow and pop.
```

---

## Đặt file

- Nhà hàng: `Assets/Art/Buildings/Restaurant_v2/`
- 3 chuồng: `Assets/Art/Buildings/AnimalPens_v2/`

Để thư mục v2 riêng thì bản cũ vẫn còn, không bị ghi đè.

Có art rồi Sếp báo em. Em kiểm tra nền trắng và độ lệch giữa các frame, xong làm tool lắp: menu lắp mới + menu trả lại, có backup.
