# Prompt v3: Chuồng rộng (2 con) + Nhà hàng kiểu bếp có đầu bếp (SÁNG như quầy hàng)

Ảnh mẫu phong cách, bắt buộc dán vào công cụ vẽ (style reference, độ ảnh hưởng cao):
`Assets/Art/Buildings/FarmStand/_Opt/stand_base_opt.png` (quầy hàng).

## Luật chung

**Nền:**
- Nền trong suốt thật.
- Không nền trắng, không bóng xám lan ra ngoài chân nhà, không nền trắng lọt giữa các song rào.

**Canvas:**
- Canvas 1024x1024, công trình nằm giữa, cách mép khoảng 30px.
- Không chữ, không logo.

**Sprite sheet:**
- N frame xếp **1 hàng ngang**, các ô bằng nhau, nền trong suốt.
- Mọi frame vẽ vật **cùng vị trí, cùng cỡ**.
- Điểm treo, trục quay hoặc chân vật nằm **đúng cùng 1 pixel** ở mọi frame.
- Vẽ đúng tỉ lệ khi gắn lên nhà.
- Mỗi công trình kèm 1 ảnh `*_reference.png`: nhà + mọi lớp ghép sẵn đúng chỗ.

**Em làm bằng code, KHÔNG cần vẽ:** khói ống khói, hơi nước bay lên, ánh đèn và cửa sổ nhấp nháy, tia lửa, bụi.

---

## KHỐI PHONG CÁCH (dán đầu mọi prompt)

```
Match EXACTLY the art style of the attached reference image (a bright farm market stall from the
same game). Bright, sunny, glossy casual mobile farm game art, isometric 3/4 view, 2:1 diamond
footprint, building faces bottom-left and bottom-right, camera ~30 degrees from above.
Warm golden-orange wood with visible grain, clean dark-brown outlines, cheerful saturated colors,
soft cel shading with bright highlights, light from top-left.
PNG with fully transparent background, no grass base, no white background, no grey cast shadow
outside the footprint, no white gaps between fence rails, no text, no watermark.
```

---

## A. CHUỒNG RỘNG, CHỨA THOẢI MÁI 2 CON

### Vì sao phải vẽ khác bản 1
- **Sân chật:** nhà chuồng bản 1 chiếm gần nửa chân đế, nên sân còn lại hẹp, 2 con vật đứng là chạm nhau.
- **Máng nằm giữa sân:** con vật đi qua là đè lên máng.

### Luật bố cục (cả 3 chuồng)
- **Nhà chuồng:** nhỏ gọn, nằm ở **góc TRÊN (phía sau)** của hình thoi. Chiếm tối đa **25% diện tích chân đế** và rộng tối đa 35% bề ngang.
- **Sân:** chiếm **75% còn lại**. Mặt sân là đất phẳng **trống hoàn toàn ở giữa**, không đá, không máng, không vật gì.
- **Máng ăn, máng nước:** đặt **sát hàng rào**, ở cạnh trái hoặc phải, không lấn vào giữa.
- **Độ rộng sân:** vừa cho **2 con bò trưởng thành đứng cạnh nhau** mà hai bên vẫn còn chỗ đi. Nói cách khác, bề ngang sân ít nhất bằng 4 lần chiều dài thân con bò.
- **Hàng rào:** chạy **kín 4 cạnh** hình thoi, có 1 cổng ở cạnh trước. Song rào thấp để thấy rõ con vật bên trong.
- **Không vẽ con vật.** Con vật có sẵn trong game và tự đi lại.

### A1. Chuồng bò (cow_barn_base.png)
```
A wide open cow pen seen in isometric view. A SMALL cozy wooden cow barn with a bright red-orange
clay tile roof sits ONLY in the back (top) corner of the diamond footprint, taking at most 25% of
the area, big arched door open with golden hay inside (dark interior, no cow). The rest is a LARGE
flat empty dirt yard with a few straw bits, completely clear in the middle, wide enough for two
adult cows standing side by side with room to walk around. A long wooden hay feeder and a stone
water trough placed flat against the LEFT fence. Low warm golden wooden fence enclosing all four
sides of the diamond, with a small gate on the front edge. No animals. Static.
```

### A2. Chuồng gà (chicken_coop_base.png)
```
A wide open chicken yard seen in isometric view. A SMALL raised wooden chicken coop on short
stilts with a bright red-orange tile roof and a little ramp sits ONLY in the back (top) corner,
taking at most 25% of the area, with a straw nesting box. The rest is a LARGE flat empty dirt yard
with scattered grain, completely clear in the middle. A long grain trough and a clay water dish
placed against the RIGHT fence. Low warm golden wooden fence enclosing all four sides, small gate
on the front edge. No chickens. Static.
```

### A3. Chuồng heo (pig_pen_base.png)
```
A wide open pig pen seen in isometric view. A SMALL rustic wooden pig shed with a bright golden
straw thatched roof sits ONLY in the back (top) corner, taking at most 25% of the area, arched
opening with straw inside (dark interior, no pig). The rest is a LARGE flat dirt yard, completely
clear in the middle, with ONE shallow mud puddle near the right fence (flat, not in the center).
A wooden feeding trough with vegetables placed against the LEFT fence. Low warm golden wooden
fence enclosing all four sides, small gate on the front edge. No pigs. Static.
```

### Lớp chuyển động cho chuồng
- **weathervane_sheet.png** (mỗi chuồng 1 cái): 6 frame, ô 192x192.
  ```
  A small golden-brass rooster weathervane on a thin pole, sprite sheet 6 frames in ONE horizontal
  row, each cell 192x192, the rooster rotates around the pole, pole base at EXACTLY the same pixel
  (bottom center) every frame.
  ```
- **pig_mud_bubbles_sheet.png**: 6 frame, ô 256x128.
  ```
  A few brown mud bubbles forming and popping (puddle NOT included, only bubbles), 6 frames in ONE
  horizontal row, each cell 256x128, same positions every frame.
  ```
- **cow_trough_water_sheet.png**: 4 frame, ô 256x128.
  ```
  Water surface ripples inside a stone trough (trough NOT included), 4 frames in ONE horizontal
  row, each cell 256x128, same shape every frame, only shimmer lines move.
  ```

---

## B. NHÀ HÀNG KIỂU BẾP MỞ, CÓ CHỖ CHO ĐẦU BẾP

### Ý tưởng
- **Phía sau:** nhà hàng nhỏ.
- **Phía trước:** một **gian bếp mở có mái hiên**, giống màn nấu ăn (Cooking) trong game.
- **Chỗ đầu bếp:** có **khoảng đứng trống** phía sau quầy bếp. Đầu bếp NPC là sprite riêng và đứng vào đó. Em cho đầu bếp di chuyển qua lại giữa bếp lửa, nồi và thớt.
- **Animation:** nhiều thứ chuyển động (lửa bếp, nồi sôi, chảo xào, xiên nướng quay, lò nướng, bảng treo).

### B1. restaurant_base.png (1024x1024, chân 3x3 ô)
```
A cheerful countryside restaurant with an OPEN-AIR KITCHEN in front, isometric view. Back half:
a small cozy restaurant building, cream plaster walls with warm golden wooden beams, bright
red-orange clay tile roof, a stone chimney (no smoke), warm-lit windows (empty interior), flower
boxes. Front half: an open kitchen under a striped red-and-cream awning supported by wooden posts
(like the reference stall). Inside the open kitchen: a long wooden cooking counter along the FRONT
edge with a cutting board and vegetables; on the LEFT a brick stove with TWO EMPTY burners (no fire,
no pots); on the RIGHT a round stone pizza oven with a DARK EMPTY oven mouth (no fire); between
them a small brick grill pit with an EMPTY spit holder (no meat). Behind the counter an EMPTY flat
wooden floor area big enough for one chef to stand and walk left-right (no chef drawn).
Hanging copper pans and garlic under the awning, a hanging iron hook at the front-left post for
a sign (sign NOT drawn). Static, nothing moving.
```

### B2. Lớp chuyển động nhà hàng
Tất cả vẽ đúng tỉ lệ, đặt đúng chỗ trên nhà, 1 hàng ngang.

- **stove_fire_sheet.png**: 6 frame, ô 256x192. Lửa dưới 2 bếp.
  ```
  Two small bright orange-yellow gas-like flames for two stove burners, 6 frames in ONE horizontal
  row, each cell 256x192, flames flicker, flame bases at EXACTLY the same pixels every frame.
  ```
- **pot_boil_sheet.png**: 6 frame, ô 256x256. Nồi canh trên bếp.
  ```
  A copper soup pot with a lid, soup bubbling, the lid gently rattling up and down, 6 frames in ONE
  horizontal row, each cell 256x256, pot body at EXACTLY the same position every frame, only lid and
  bubbles move.
  ```
- **pan_fry_sheet.png**: 6 frame, ô 256x256. Chảo trên bếp thứ 2.
  ```
  A black frying pan with vegetables and a fried egg, food hopping slightly as it sizzles, 6 frames
  in ONE horizontal row, each cell 256x256, pan at EXACTLY the same position every frame, only food moves.
  ```
- **grill_spit_sheet.png**: 8 frame, ô 320x256. Xiên nướng quay trên lò than.
  ```
  A roast chicken on a metal spit rotating over glowing coals, 8 frames in ONE horizontal row, each
  cell 320x256, spit axis at EXACTLY the same pixels every frame, only the chicken rotates and the
  coals glow.
  ```
- **oven_fire_sheet.png**: 6 frame, ô 256x256. Lửa trong lò pizza.
  ```
  Warm orange flames and a pizza inside a dark stone oven mouth (oven NOT included), 6 frames in
  ONE horizontal row, each cell 256x256, flame base at EXACTLY the same pixels every frame.
  ```
- **sign_sheet.png**: 6 frame, ô 256x320. Bảng treo đung đưa (bảng trống, em gắn chữ bằng code).
  ```
  A blank round wooden signboard with a small fork-and-spoon emblem hanging from a hook by two short
  chains, swinging gently like a pendulum, 6 frames in ONE horizontal row, each cell 256x320, the hook
  point at EXACTLY the same pixel (top center) every frame.
  ```

### B3. Đầu bếp NPC (tùy chọn, nếu muốn đầu bếp mới hợp art này)
- **chef_work_sheet.png**: 8 frame, ô 384x384.
  ```
  A cute chubby chef with a tall white hat and white apron, facing 3/4 toward the viewer, working at
  a counter: frames 1-4 stirring a pot with a ladle, frames 5-8 chopping with a knife. 8 frames in ONE
  horizontal row, each cell 384x384, feet at EXACTLY the same pixel (bottom center) every frame,
  same scale. Same bright style as the reference.
  ```
- **chef_walk_sheet.png**: 6 frame, ô 384x384. Cùng đầu bếp đi ngang sang phải, chân nằm cùng 1 pixel ở mọi frame. Em tự lật hình để có chiều đi sang trái.

---

## Đặt file

- Chuồng: `Assets/Art/Buildings/AnimalPens_v3/`
- Nhà hàng: `Assets/Art/Buildings/Restaurant_v3/`

Có art rồi Sếp báo em. Em sẽ:

1. Kiểm tra nền trắng và độ lệch giữa các frame.
2. Làm tool lắp, có menu trả lại và backup, nằm trong **Edric Tools**.
3. Với chuồng: đo vùng sân thật trên hình để con vật chỉ đi trong sân, không đè lên nhà chuồng, không ra ngoài rào.
