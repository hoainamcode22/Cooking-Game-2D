# Prompt vẽ 3 công trình nhiều lớp (giống Máy Xay / Chợ)

Cách dùng: dán **KHỐI CHUNG** lên đầu, rồi dán prompt của từng file. Mỗi file vẽ riêng 1 lần.

## Luật bắt buộc cho mọi sprite sheet

Lần trước lá cờ chợ bị lệch vị trí giữa các frame, nên lần này bắt buộc:

- Mỗi sheet có N frame xếp **1 hàng ngang**, các ô bằng nhau, nền trong suốt.
- Mọi frame vẽ vật **cùng vị trí, cùng cỡ**. Chỉ phần chuyển động thay đổi.
- Điểm treo hoặc trục quay nằm **đúng cùng 1 pixel** ở mọi frame.
- Vẽ đúng tỉ lệ như khi gắn lên nhà (không phóng to riêng).
- Kèm 1 ảnh `*_reference.png`: nhà + mọi lớp ghép sẵn, để em đo vị trí gắn.
- Không chữ, hoặc chỉ chữ tiếng Anh. Không nền trắng. Không bóng đổ lan ra ngoài chân nhà.

Khói, ánh đèn nhấp nháy, bụi, lá bay em làm bằng code. Họa sĩ chỉ cần vẽ **1 sprite tĩnh** cho mỗi thứ đó.

---

## KHỐI CHUNG (dán đầu mọi prompt)

```
Cute casual mobile farm game art, Hay Day / Township style. Isometric 3/4 view, 2:1 diamond
footprint, building faces bottom-left and bottom-right, camera ~30 degrees from above.
Soft cel shading, clean dark-brown outlines (3-4 px), warm sunny palette, light from top-left.
PNG, fully transparent background, no ground tile, no grass base, no cast shadow outside the
footprint, no text, no logo, no watermark. Centered on canvas.
```

---

## 1. Chuồng gia súc (Livestock Barn), chân 2x2 ô

**barn_base.png** (2048x2048)
```
A cozy red wooden livestock barn with white trim and a grey shingled gambrel roof, a small hay
loft opening under the roof peak (dark empty interior), a big double front door opening
(dark empty interior, no animal inside), a wooden water trough and two hay bales beside the door,
a short wooden fence corner on the right. Static, nothing moving.
```

**barn_weathervane_sheet.png**: 6 frame, ô 256x256
```
A small metal rooster weathervane on a thin pole, sprite sheet 6 frames in ONE horizontal row,
each cell 256x256, the rooster slowly rotates around the vertical pole: front, 3/4 right, side,
back, side left, 3/4 left. The pole base is at EXACTLY the same pixel (bottom center) in every
frame, same scale, only the rooster rotation changes.
```

**barn_cow_peek_sheet.png**: 4 frame, ô 384x384
```
A cute black-and-white cow head peeking out of a dark barn door opening, sprite sheet 4 frames
in ONE horizontal row, each cell 384x384: 1 neutral, 2 ears flick, 3 eyes blink, 4 head tilts
slightly. Neck bottom edge at EXACTLY the same position in every frame, same scale.
```

**barn_hay_loft_sheet.png**: 4 frame, ô 256x256
```
A tuft of golden hay sticking out of a barn loft opening, gently swaying in the wind, sprite
sheet 4 frames in ONE horizontal row, each cell 256x256, hay root fixed at the same pixel,
only the tips sway.
```

**barn_trough_water_sheet.png**: 4 frame, ô 256x128
```
Top surface of water inside a wooden trough, light-blue with white shimmer lines moving,
sprite sheet 4 frames in ONE horizontal row, each cell 256x128, exact same shape and position
every frame, only the shimmer lines move.
```

---

## 2. Quầy hàng (Market Stall), chân 1x2 ô

**stall_base.png** (1536x1536)
```
A small wooden roadside market stall with a sloped counter full of produce crates (tomatoes,
corn, cabbages, carrots), a wooden frame with an EMPTY top rail where an awning will hang,
a small hanging hook on the right post (nothing hanging). Static.
```

**stall_awning_sheet.png**: 4 frame, ô 768x384
```
A red-and-cream striped scalloped fabric awning, sprite sheet 4 frames in ONE horizontal row,
each cell 768x384, the scalloped bottom edge ripples gently in the wind. The TOP edge (attached
to the rail) is at EXACTLY the same pixels in every frame; only the lower fabric waves.
```

**stall_sign_sheet.png**: 6 frame, ô 256x320
```
A small blank wooden price board hanging from two short ropes on one hook, sprite sheet
6 frames in ONE horizontal row, each cell 256x320, swinging gently left-right like a pendulum.
The hook point is at EXACTLY the same pixel (top center) in every frame, same scale.
```

**stall_pinwheel_sheet.png**: 6 frame, ô 192x192
```
A colorful paper pinwheel on a short stick, sprite sheet 6 frames in ONE horizontal row,
each cell 192x192, blades rotate 60 degrees per frame, the center axle at EXACTLY the same
pixel every frame.
```

**stall_lantern.png** (256x256): 1 đèn lồng giấy tĩnh. Em làm quầng sáng nhấp nháy bằng code.

---

## 3. Nhà hàng (Restaurant), chân 3x3 ô

**restaurant_base.png** (2048x2048)
```
A charming two-story countryside bistro restaurant, cream plaster walls with wooden beams,
red tiled roof, a brick chimney (no smoke), a big front window with a DARK EMPTY interior,
a green striped awning over the entrance, a blank wooden signboard above the door, two small
outdoor tables with checkered cloth (empty tabletops), flower boxes under the windows. Static.
```

**restaurant_window_chef_sheet.png**: 6 frame, ô 512x384
```
Warm-lit kitchen interior seen through a window (window frame NOT included): a cute chef with
a white hat tossing a pan, sprite sheet 6 frames in ONE horizontal row, each cell 512x384,
background kitchen identical in every frame, only the chef's arm and pan move, flame flicker
under the pan.
```

**restaurant_sign_sheet.png**: 6 frame, ô 384x256
```
A blank round wooden restaurant sign with a small fork-and-spoon emblem, hanging from an iron
bracket, sprite sheet 6 frames in ONE horizontal row, each cell 384x256, swinging gently.
The bracket hinge at EXACTLY the same pixel every frame.
```

**restaurant_dish_steam_sheet.png**: 4 frame, ô 256x256
```
A steaming bowl of soup on a plate (for an outdoor table), sprite sheet 4 frames in ONE
horizontal row, each cell 256x256, bowl identical every frame, only 2-3 soft white steam
curls rise and change shape.
```

**restaurant_door_sheet.png**: 4 frame, ô 384x512
```
A wooden restaurant front door with a small round window, sprite sheet 4 frames in ONE
horizontal row, each cell 384x512: closed, 1/3 open, 2/3 open, open (warm light inside).
The hinge side at EXACTLY the same pixels every frame.
```

**restaurant_smoke_puff.png** (256x256): 1 cụm khói mềm màu xám trắng, tĩnh. Em cho khói bay bằng code.

---

## Đặt file

Đặt vào `Assets/Art/Buildings/Barn/`, `Assets/Art/Buildings/Stall/`, `Assets/Art/Buildings/Restaurant/`, đúng tên file như trên.

Có art rồi Sếp báo em làm tool lắp và nén ảnh giống chợ / máy xay. Mỗi tool có menu **lắp mới** và **trả lại cũ**.
