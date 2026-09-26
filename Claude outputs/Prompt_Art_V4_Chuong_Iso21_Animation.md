# Prompt v4: Vẽ lại 3 chuồng đúng góc iso 2:1 + thêm animation cho chuồng

## Vì sao phải vẽ lại

Em đo trên art v3:
- **Chuồng bò:** đáy hàng rào nghiêng **35.6°**.
- **Chuồng heo:** nghiêng **37°**.
- **Lưới đất và đường trong game:** nghiêng **26.57°** (iso 2:1).

Art dốc hơn lưới khoảng 10°, nên đặt cạnh đường đất nhìn bị xéo. Prompt v3 ghi "camera ~30 degrees from above" nên công cụ vẽ tự chọn góc. Bản này ép góc bằng **số đo cụ thể + ảnh khung**.

## Gửi kèm cho công cụ vẽ (2 ảnh)

1. **Ảnh mẫu phong cách** (style reference, độ ảnh hưởng cao): `Assets/Art/Buildings/FarmStand/_Opt/stand_base_opt.png` (quầy hàng).
2. **Ảnh khung bố cục** (layout / structure reference): `pen_iso_guide_1024.png` (file đi kèm prompt này).
   - **Hình thoi đỏ:** đường hàng rào, cũng là mặt đất.
   - **Ô xanh:** chỗ đặt nhà chuồng, ở góc sau.
   - **Vạch xanh lá:** cổng.

## Luật chung (giữ như v3)

**Nền:**
- Nền trong suốt thật.
- Không nền trắng, không bóng xám lan ra ngoài, không nền trắng lọt giữa các song rào.

**Canvas:**
- Canvas 1024x1024.
- Mặt đất khớp đúng hình thoi trong ảnh khung: trái (42,700), trên (512,465), phải (982,700), dưới (512,935).
- Không chữ, không logo.

**Sprite sheet:**
- N frame xếp **1 hàng ngang**, các ô bằng nhau, nền trong suốt.
- Mọi frame vẽ vật **cùng vị trí, cùng cỡ**.
- Điểm gốc (chân, trục, điểm treo) nằm **đúng cùng 1 pixel** ở mọi frame.

**Tham chiếu:**
- Mỗi chuồng kèm 1 ảnh `*_reference.png`: chuồng + mọi lớp animation ghép sẵn đúng chỗ, để em đo vị trí gắn.
- **Không vẽ con vật.** Con vật có sẵn trong game và tự đi lại.

**Em làm bằng code, KHÔNG cần vẽ:** bụi bay, rơm bay theo gió, ruồi bay, trái tim, bong bóng chữ, khói.

---

## KHỐI PHONG CÁCH + GÓC NHÌN (dán đầu mọi prompt)

```
Match EXACTLY the art style of the FIRST attached image (a bright farm market stall from the same
game). Bright, sunny, glossy casual mobile farm game art, warm golden-orange wood with visible grain,
clean dark-brown outlines, cheerful saturated colors, soft cel shading, light from top-left.

STRICT ISOMETRIC GEOMETRY (most important): true 2:1 game isometric (dimetric) projection.
The ground footprint is a diamond exactly TWICE as wide as it is tall. Every ground-aligned edge
(fence bottoms, fence rails, wall bases, roof eaves, trough edges, gate) slopes EXACTLY 1 pixel up
for every 2 pixels across (26.57 degrees). Vertical edges (posts, wall corners) stay perfectly
vertical. NOT 30 degrees, NOT 35 degrees, NOT a steep top-down view.
Use the SECOND attached image as the exact layout: the fence follows the red diamond
(left 42,700 / top 512,465 / right 982,700 / bottom 512,935 on a 1024x1024 canvas), the barn sits
inside the blue back-corner zone and may rise up to y=130, the gate is at the green mark.
Do NOT draw the guide lines, labels or colors of the guide image.

PNG with fully transparent background, no grass base, no white background, no grey cast shadow
outside the footprint, no white gaps between fence rails, no text, no watermark.
```

---

## A. 3 CHUỒNG TĨNH (giữ bố cục v3, chỉ sửa góc)

### Luật bố cục
- **Nhà chuồng:** nhỏ, chỉ nằm trong ô xanh (góc sau, tối đa 25% diện tích).
- **Sân:** 75% còn lại là đất phẳng, **trống hoàn toàn ở giữa**.
- **Máng:** đặt sát hàng rào trái hoặc phải.
- **Hàng rào:** thấp, chạy kín 4 cạnh, 1 cổng ở cạnh trước bên trái.
- **Để trống sẵn** những chỗ sẽ gắn animation (máng cỏ, vũng bùn, ổ trứng...). Vẽ tĩnh, không chuyển động.
- **Chuồng bò:** **KHÔNG** vẽ chong chóng / con gà trên mái. Chuồng heo cũng vậy.

### A1. cow_barn_base.png
```
A wide open cow pen, strict 2:1 isometric as specified. A SMALL cozy wooden cow barn with a bright
red-orange clay tile roof ONLY in the back (top) corner zone, big arched door open with golden hay
inside (dark interior, no cow). NO weathervane, nothing on the roof ridge. The rest is a LARGE flat
empty dirt yard with a few straw bits, clear in the middle. A long wooden hay feeder (EMPTY, no hay)
and a stone water trough (EMPTY, dry) placed flat against the LEFT fence. Low golden wooden fence on
all four sides, small gate at the front-left edge. No animals. Static.
```

### A2. pig_pen_base.png
```
A wide open pig pen, strict 2:1 isometric as specified. A SMALL rustic wooden pig shed with a golden
straw thatched roof ONLY in the back (top) corner zone, arched opening with straw inside (dark, no
pig). NO weathervane. The rest is a LARGE flat dirt yard, clear in the middle, with ONE big shallow
flat mud puddle near the RIGHT fence (flat on the ground, follows the 2:1 ground plane). A wooden
feeding trough with vegetables against the LEFT fence. Low golden wooden fence on all four sides,
small gate at the front-left edge. No pigs. Static.
```

### A3. chicken_coop_base.png
```
A wide open chicken yard, strict 2:1 isometric as specified. A SMALL raised wooden chicken coop on
short stilts with a bright red-orange tile roof and a little ramp ONLY in the back (top) corner
zone, with an open straw nesting box on its side (nest EMPTY, no eggs), a small square flap door on
the coop front (door drawn OPEN, dark hole). The rest is a LARGE flat dirt yard with scattered
grain, clear in the middle. A long grain trough and a clay water dish against the RIGHT fence.
Low golden wooden fence on all four sides, small gate at the front-left edge. No chickens. Static.
```

### A4. (tùy chọn) dairy_barn_base.png: Chuồng Bò Sữa
Hiện chuồng Bò Sữa dùng chung hình với chuồng bò. Muốn khác thì vẽ thêm bản này.
```
Same layout and geometry as the cow pen, but the barn is white-and-black painted wood (like a
dairy cow pattern) with a red roof, two shiny metal milk cans standing by the barn door, an EMPTY
hay feeder and an EMPTY stone trough against the LEFT fence. No weathervane. No animals. Static.
```

---

## B. ANIMATION CHO CHUỒNG (sheet 1 hàng ngang, vẽ cùng góc 2:1)

### Chuồng heo
- **pig_mud_splash_sheet.png**: 8 frame, ô 320x224. Bùn bắn tung lên từ vũng rồi rơi xuống. Em cho phát ngẫu nhiên khi heo đi qua vũng bùn.
  ```
  Brown mud clumps and droplets splashing UP out of a puddle and falling back down, like a pig just
  flopped into the mud (puddle itself NOT included, no pig), 8 frames in ONE horizontal row, each
  cell 320x224, the splash origin at EXACTLY the same pixel (bottom center) every frame, frame 1 and
  frame 8 almost empty so it can play once.
  ```
- **pig_mud_bubbles_sheet.png**: 6 frame, ô 256x128. Bóng bùn nổi lên rồi vỡ, chạy lặp liên tục. Có rồi thì bỏ qua.
  ```
  A few brown mud bubbles forming and popping (puddle NOT included), 6 frames in ONE horizontal row,
  each cell 256x128, same positions every frame, loops seamlessly.
  ```
- **pig_trough_food_sheet.png**: 4 frame, ô 256x160. Rau củ trong máng lắc lư nhẹ, như heo vừa ăn.
  ```
  Vegetables (carrots, cabbage leaves, apples) heaped inside a wooden trough, gently wobbling and
  shifting (trough NOT included, only the food), 4 frames in ONE horizontal row, each cell 256x160,
  same pile position every frame, loops seamlessly, strict 2:1 isometric.
  ```

### Chuồng bò (dùng cho cả Bò Sữa)
- **cow_hay_sheet.png**: 6 frame, ô 320x180. Cỏ khô trong máng lay động theo gió.
  ```
  A heap of golden hay lying inside a long feeder (feeder NOT included, only the hay), a few straws
  swaying gently in the breeze, 6 frames in ONE horizontal row, each cell 320x180, hay pile at
  EXACTLY the same position every frame, loops seamlessly, strict 2:1 isometric.
  ```
- **cow_trough_water_sheet.png**: 4 frame, ô 256x128. Mặt nước trong máng gợn sóng lấp lánh.
  ```
  Clear blue water surface inside a stone trough (trough NOT included), ripples and shimmer lines
  moving, 4 frames in ONE horizontal row, each cell 256x128, same outline every frame, loops
  seamlessly, the water surface is a flat 2:1 isometric shape.
  ```
- **cow_barn_door_sheet.png**: 6 frame, ô 256x320. Cánh cửa chuồng khép mở nhẹ theo gió.
  ```
  One wooden barn door leaf with a white X brace, swinging very slightly back and forth on its
  hinges in the wind, 6 frames in ONE horizontal row, each cell 256x320, the hinge edge at EXACTLY
  the same pixels every frame, loops seamlessly (ping-pong), strict 2:1 isometric.
  ```

### Chuồng gà
- **chicken_nest_egg_sheet.png**: 6 frame, ô 192x160. Trứng trong ổ rơm lắc nhẹ.
  ```
  Two white eggs in a small straw nest (nest box NOT included, only straw and eggs), the eggs gently
  wobbling, 6 frames in ONE horizontal row, each cell 192x160, same position every frame, loops.
  ```
- **chicken_flap_door_sheet.png**: 6 frame, ô 160x160. Cửa lật nhỏ của chuồng gà đung đưa.
  ```
  A small square wooden flap door hinged at the top, swinging gently like a gate a chicken just
  walked through, 6 frames in ONE horizontal row, each cell 160x160, the top hinge at EXACTLY the
  same pixels every frame, loops (ping-pong), strict 2:1 isometric.
  ```
- **Chong chóng gà:** chỉ chuồng gà giữ, dùng lại sheet cũ `weathervane_sheet.png`, không cần vẽ lại.

---

## Đặt file

- Thư mục: `Assets/Art/Buildings/AnimalPens_v4/`
- Tên file giữ như trên.
- Mỗi chuồng 1 ảnh `*_reference.png`.

Có art rồi Sếp báo em, em tự lắp luôn:

1. **Đo góc hàng rào trên hình.** Cho phép 26.57° ± 1.5°. Lệch hơn thì em báo Sếp kèm số đo, chưa lắp.
2. **Kiểm tra nền và frame:** nền trắng, độ lệch giữa các frame.
3. **Lắp vào game:**
   - Làm tool trong **Edric Tools > Chuong (Pen)**, có menu trả lại và backup.
   - Thay hình 4 chuồng ở ngoài map và trong shop.
   - Đo lại vùng sân để con vật đi đúng trong rào.
4. **Gắn animation:**
   - Bùn bắn phát khi heo đi qua vũng bùn.
   - Các lớp khác chạy lặp liên tục.
   - Ngoài màn hình thì đứng yên, không tốn CPU trên mobile.
