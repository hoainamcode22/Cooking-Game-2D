# 🎨 PROMPT ĐỘI VẼ — HỒ CÂU **V3** (08/09/2026) · CÓ BẢNG STYLE ĐO THỰC

> Ra đề: Tech Lead · Duyệt: Sếp Edric · **THAY THẾ** bản 07/09 và V2 08/09.
> Phần 1 (STYLE) **đo trực tiếp bằng script từ art Sếp đã vẽ trong project**, không mô tả cảm tính.
> **Thư mục giao hàng:** `production/art-handoff/2026-09-07_HoCau/` — 4 gói con `D_LoiVao/` · `A_Trong_Nuoc/` · `B_Icon_HUD/` · `C_Khung_UI/`
> **Tổng 47 file PNG.** Ưu tiên: **D → A → B → C**.

---

# PHẦN 1 — STYLE CỦA GAME (bắt buộc đọc trước khi vẽ)

Project có **3 dòng nét vẽ** đang chạy song song. Vẽ sai dòng là asset chỏi ngay khi đặt cạnh nhau.

| Dòng | Dùng cho | Nhận dạng trong 3 giây | File mẫu |
|---|---|---|---|
| **A** — cartoon storybook | Công trình, quầy, vật thể, icon | Outline nâu đậm dày, vân gỗ vẽ tay, thảm cỏ bầu dục dưới chân | `Assetsgame/Buiding/icon_chuong_ga_v2.png` |
| **B** — painterly bán tả thực | Nguyên liệu, cây trồng, cá | Outline nâu mảnh hơn, chuyển màu mềm hơn | `Assetsgame/hatgiong/bapcai-removebg-preview.png` |
| **C** — vector iso phẳng | Tile map (Sếp tự vẽ) | Không outline, khối màu phẳng, góc 45° | `maptitle/Map45Iso/Sheet_IsoCliff45.png` |

## 1.1 DÒNG A — CÔNG TRÌNH & VẬT THỂ TRONG GAME ⭐ (dùng cho 95% gói này)

**File mẫu bắt buộc mở ra xem trước khi vẽ** (đây là art Sếp vẽ mới nhất, ngày 08/09):
- `Assets/Assetsgame/Buiding/icon_chuong_ga_v2.png` (512×512) ← **GOLDEN REFERENCE SỐ 1**
- `Assets/Assetsgame/Buiding/icon_chuong_bo_v2.png`, `icon_chuong_heo_v2.png`, `icon_chuong_bo_sua_v2.png`
- `Assets/Art/Decor/Stages/gieng/stage_3.png` (giếng nước, 480×488)

**Nét vẽ — mô tả đúng cái Sếp đã vẽ:**

Đây là **cartoon storybook vẽ tay**, kiểu truyện tranh thiếu nhi phương Tây, KHÔNG phải pixel-art, KHÔNG phải vector phẳng, KHÔNG phải 3D render.

| Đặc điểm | Số đo thực (từ `icon_chuong_ga_v2.png`) |
|---|---|
| **Outline ngoài** | Nâu sô-cô-la sẫm **`#331309`** (HSV: hue **14°**, sat **82%**, val **20%**). **TUYỆT ĐỐI KHÔNG ĐEN.** |
| **Bề dày outline ngoài** | **1,2% cạnh dài nhất** (ảnh 1024 px → viền 12 px; ảnh 512 px → viền 6 px). Đều tay, khép kín, bao trọn silhouette. |
| **Outline trong (chi tiết)** | Cùng màu nâu, mảnh hơn khoảng **một nửa** viền ngoài. Dùng để tách từng tấm ván, từng viên ngói, khung cửa, nan giỏ. |
| **Bảng màu gỗ** | Sẫm `#603018` · Trung `#784830` → `#906030` · Sáng `#A87830` → `#C09048` |
| **Mái ngói** | Đỏ đất/gạch nung, không đỏ tươi: `#8E3B2E` → `#B4574A`, mỗi viên ngói có viền riêng |
| **Cỏ nền** | Xanh olive `#6E8F3C` → `#9CBF57`, chấm hoa/lá vàng nhạt rải rác |
| **Độ bão hoà** | trung bình **66%**, p90 **98%** — màu tươi, không xỉn |
| **Độ sáng** | value trung bình **52%**, dải L từ **21 → 189** (tương phản mạnh, không bệt) |
| **Ám màu** | **R − B = +79** — toàn bảng màu ngả ấm/đỏ rõ rệt. Không dùng xám lạnh, không xanh ngả lam. |
| **Đổ bóng** | **Cel-shading mềm 2–3 tầng** (không airbrush mịn, không gradient dài): mảng sáng, mảng trung, mảng tối có ranh giới nhìn thấy nhưng bo mềm. |
| **Highlight** | Vệt sáng mảnh dọc theo mép trên của ván gỗ / mép ngói, hướng sáng **trên–trái**. |
| **Vân gỗ** | Vẽ tay bằng nét nâu mảnh cong nhẹ, 2–4 nét mỗi tấm ván. Đây là dấu hiệu nhận dạng của style — thiếu nó là mất chất. |
| **Chân đế** | Công trình đứng trên **thảm cỏ hình bầu dục** ôm sát chân, có viền nâu riêng, mép cỏ lởm chởm tự nhiên (xem chuồng gà). |
| **Chi tiết dễ thương** | Luôn thêm 1–3 vật nhỏ kể chuyện: giỏ trứng, xô gỗ, hàng rào ngắn, dây đèn, chậu hoa, con vật. Đây là điều làm art của game "có hồn". |
| **Góc nhìn** | 3/4 từ trên xuống (khoảng 30–40°), thấy được mặt trước + một mặt hông + một phần mái. |

### 1.1b Câu mô tả style dán thẳng vào prompt AI (dòng A)

> *Hand-drawn 2D cartoon game asset, children storybook illustration style, warm rustic farm village theme.
> Thick dark-brown outline (#331309), consistent weight around the whole silhouette, thinner brown lines for
> interior details. Soft cel-shading in 2–3 visible but soft-edged tone bands, light coming from upper-left.
> Warm wood palette (#603018 / #784830 / #906030 / #C09048) with hand-painted wood grain strokes on every plank.
> Terracotta red roof tiles (#8E3B2E → #B4574A), each tile outlined. Olive-green grass base (#6E8F3C → #9CBF57).
> Saturated but earthy colours, overall warm colour cast, strong light-to-dark contrast. Three-quarter top-down
> view (~35°). Cute, cosy, inviting. No text, no letters, no numbers, no logo anywhere. Transparent background,
> no drop shadow, no ground shadow baked in. Clean edges, no white halo.*

**Câu chống lỗi (negative prompt):** `text, letters, numbers, watermark, signage with writing, black outline, pixel art, flat vector, 3d render, photo, drop shadow, white background, white halo, cool grey tones, desaturated`

## 1.2 DÒNG B — NGUYÊN LIỆU / CÂY TRỒNG (dùng cho 10 con cá)

**File mẫu:** `Assets/Assetsgame/hatgiong/bapcai-removebg-preview.png` (409×610) · `cachualever3-removebg-preview.png` (503×496)

| Đặc điểm | Số đo thực |
|---|---|
| Outline | Nâu ấm `#442510` → `#654129`, dày **1,5–2,5%** cạnh dài. Hue outline **luôn ấm hơn** hue phần fill. |
| Rendering | Painterly bán tả thực hơn dòng A một chút: gradient mềm hơn, nhưng **vẫn có outline** và vẫn thấy rõ mảng sáng-tối. |
| Saturation | 49–65%, value 55% |
| Ánh sáng | Trên–trái (tâm vùng sáng lệch dx −0,03 / dy −0,23) |
| Ám màu | R − B = +44 |

> **Chốt cho gói này:** 10 con cá vẽ **lai giữa A và B** — dùng bảng màu và độ tươi của B, nhưng **giữ outline nâu đậm rõ của A** (`#3A1A0C`, dày ~1,5%), vì cá hiển thị ở kích thước icon nhỏ trong giỏ và trong toast, outline mảnh sẽ mất nét.

## 1.3 DÒNG C — TILE MAP (KHÔNG áp dụng cho gói này, chỉ để biết)

`Assets/maptitle/Map45Iso/Sheet_IsoCliff45.png`, `Sheet_IsoWater45.png`, `Sheet_Waterfall45.png` (Sếp vẽ 08/09): vector iso 45° phẳng, **không outline**, xanh lá tươi `#5DBB2E`, đá xám nâu `#6B6355`. Tile map do Sếp tự vẽ, đội vẽ không đụng.

## 1.4 LUẬT KỸ THUẬT — SAI LÀ TRẢ LẠI

1. ❌ **KHÔNG MỘT CHỮ NÀO** trên bất kỳ asset nào — không chữ, số, logo, nhãn. **Kể cả công trình**: chỗ nào có biển hiệu thì **vẽ biển TRỐNG**, game render chữ bằng TMP đè lên. (Lệnh Sếp, nhắc lại lần thứ ba.)
2. ❌ **Không nền, không bóng đổ tiếp đất bake vào ảnh.** Alpha trong suốt 100%. Không nền trắng, không nền magenta còn sót, **không viền trắng do remove.bg** (16 file cũ trong project đã dính lỗi này, đừng lặp lại).
3. ✅ Vật đứng đất: **chân chạm mép dưới canvas** (Dev đặt pivot Bottom-Center).
4. ✅ Giao **PNG rời từng file**, đúng tên, đúng thư mục. Không sprite-sheet, không `_v2`/`_final`/`@2x`, không file nguồn.
5. ✅ Không sửa `.cs` `.asset` `.prefab` `.unity` `.meta` — code và scene do Dev sở hữu.
6. ✅ Không import thẳng vào `Assets/` — chỉ thả vào `production/art-handoff/2026-09-07_HoCau/`.

---

# PHẦN 2 — DANH SÁCH 47 FILE CẦN VẼ

## GÓI D — LỐI VÀO + 2 TAB MỚI ⭐ ƯU TIÊN 1 · 5 file → 📁 `D_LoiVao/`

Đây là thứ đang chặn tiến độ: farm chưa có công trình nào để bấm vào hồ câu.

| # | File | Canvas | Vẽ gì | Dòng style |
|---|---|---|---|---|
| D1 | `building_ben_ho_cau.png` | **768 × 640** | **BẾN HỒ CÂU** đặt trên farm, bấm vào là vào scene câu. **Sàn gỗ nhô ra mặt nước** (ván nâu ấm có vân, cọc chống bên dưới), **chòi nhỏ mái ngói đỏ đất**, **3–4 cần câu dựng nghiêng** vào vách chòi, **thùng gỗ + xô** đựng cá, **phao đỏ-trắng** treo ở vách, **biển hiệu gỗ bầu dục TRỐNG** treo trước chòi. Chân cọc chạm mép dưới canvas. Thảm cỏ + mép nước xanh lam nhạt ôm chân công trình như chuồng gà. Kích thước cảm giác: to hơn chuồng gà ~1,8 lần. | **A** (bám sát `icon_chuong_ga_v2.png`) |
| D2 | `building_ben_ho_cau_glow.png` | 768 × 640 | **Trùng khít pixel với D1**, chỉ thêm viền sáng vàng ấm quanh mép ngoài (dày 6–8 px, mờ dần ra ngoài). Dùng khi công trình sẵn sàng bấm. | A |
| D3 | `icon_tab_fishing_hub.png` | **128 × 128** | Icon tab **HỒ CÂU** trên thanh HUD farm. Cần câu chéo 45° + phao đỏ-trắng + 2 gợn nước. Phải đồng bộ với 4 icon tab đang có (Shop/Kho/Chợ/Bếp): gọn, đọc được ở 64 px. | A (rút gọn chi tiết) |
| D4 | `icon_tab_shop_tool.png` | 128 × 128 | Icon tab **CÔNG CỤ** trong cửa hàng (tab thứ 4). Cần câu + rìu bắt chéo hình chữ X, cán gỗ nâu, lưỡi rìu xám bạc. | A |
| D5 | `tool_axe.png` | **256 × 256** | **RÌU** bán ở tab Công cụ. Cán gỗ nâu ấm có vân, lưỡi thép xám xanh có vệt sáng, đai đồng vàng `#D9A441` chỗ nối. Nghiêng 30°, lưỡi hướng lên trái. | A |

## GÓI A — CẦN · PHAO · CÁ · 15 file → 📁 `A_Trong_Nuoc/`

| # | File | Canvas | Vẽ gì |
|---|---|---|---|
| A1 | `rod_tier1_tre.png` | **64 × 240** | Cần **TRE**: thân tre vàng nhạt có mắt tre, **dựng đứng thẳng**, tay cầm ở đáy canvas, ngọn ở đỉnh. **KHÔNG vẽ dây câu** (game vẽ dây bằng code). |
| A2 | `rod_tier2_go.png` | 64 × 240 | Cần **GỖ**: gỗ nâu ấm có vân, quấn dây ở tay cầm, 1 khoen đồng vàng. |
| A3 | `rod_tier3_carbon.png` | 64 × 240 | Cần **CARBON**: đen xám ánh xanh rêu, 2–3 khoen đồng, tay cầm bọc cork be. |
| A4 | `rod_tier4_vang.png` | 64 × 240 | Cần **VÀNG** (mua bằng kim cương): thân đồng vàng `#D9A441` sáng, khoen burgundy `#8E1F3B`, ngọn gắn 1 viên đá nhỏ. Không vẽ tia lấp lánh (code lo). |
| A5 | `bobber.png` | **32 × 32** | Phao: nửa trên đỏ burgundy, nửa dưới trắng ngà, chấm vàng trên chóp, outline nâu. |
| A6 | 10 file cá (bảng dưới) | **128 × 128** | Cá nằm ngang nhìn nghiêng, **đầu bên trái**, thân chiếm ~75% chiều ngang. **Bố cục và tỉ lệ giống hệt nhau cả 10 con** để xếp cạnh nhau trong giỏ nhìn thành một bộ. |

| File | Loài | Màu | Độ hiếm (thể hiện bằng ánh viền, KHÔNG bằng chữ) |
|---|---|---|---|
| `fish_ro.png` | Cá rô | xanh rêu sẫm, vây vàng nhạt | Thường |
| `fish_diec.png` | Cá diếc | bạc xám, bụng trắng | Thường |
| `fish_chep.png` | Cá chép | vàng cam nhạt, vảy rõ, có râu | Thường |
| `fish_tre.png` | Cá trê | nâu đen bóng, râu dài, đầu bẹt | Không thường |
| `fish_loc.png` | Cá lóc | xanh đen, thân dài, vằn nhạt | Không thường |
| `fish_tram.png` | Cá trắm | xanh ô liu đậm, mình dày | Không thường |
| `fish_lang.png` | Cá lăng | vàng nâu, thân thon, râu | Hiếm — viền ánh bạc mảnh |
| `fish_tai_tuong.png` | Cá tai tượng | tròn dẹt, xám xanh, đuôi loe | Hiếm — viền ánh bạc |
| `fish_hoi.png` | Cá hồi | hồng cam, đốm đen | Sử thi — viền ánh vàng nhạt |
| `fish_koi_vang.png` | Cá koi vàng | vàng đồng `#D9A441`, đốm trắng đỏ | Sử thi — viền ánh vàng |

## GÓI B — 12 ICON TRONG SCENE CÂU · 12 file → 📁 `B_Icon_HUD/`

Tất cả **96 × 96**, dòng **A rút gọn**: outline nâu `#331309` dày ~3 px, 2–3 tông màu, không nền, không chữ. Phải đọc được ở 48 px.

| File | Vẽ gì | Chỗ dùng |
|---|---|---|
| `icon_tab_basket.png` | Giỏ mây có 2 đuôi cá ló ra | tab Giỏ cá |
| `icon_tab_friends.png` | 2 khuôn mặt tròn cạnh nhau | tab Bạn bè |
| `icon_tab_chat.png` | Bong bóng thoại 3 chấm | tab Chat |
| `icon_private_on.png` | Ổ khoá **đóng** | Riêng tư bật |
| `icon_private_off.png` | Ổ khoá **mở** | Riêng tư tắt |
| `icon_home_farm.png` | Nhà nông trại mái đỏ | nút Về farm |
| `icon_cast.png` | Cần câu vung + mũi tên cong tới | nút QUĂNG |
| `icon_reel.png` | Cuộn dây xoay + mũi tên cong ngược | nút THU |
| `icon_gift_fish.png` | Hộp quà + đuôi cá | Tặng cá |
| `icon_gift_gem.png` | Hộp quà + kim cương xanh | Tặng gem |
| `icon_invite.png` | Phong bì có dấu + | Mời bạn |
| `icon_zoom.png` | Kính lúp có dấu + | 2 nút zoom (game tự lật thành −) |

## GÓI C — KHUNG UI & THẾ GIỚI · 15 file → 📁 `C_Khung_UI/`

| # | File | Canvas | Vẽ gì | Ghi chú kỹ thuật |
|---|---|---|---|---|
| C1 | `bubble_chat_9slice.png` | **192 × 128** | Bong bóng thoại giấy kem viền nâu bo góc, **không đuôi** | 9-slice: viền 24 px, **ruột phẳng 1 màu** để kéo giãn |
| C2 | `bubble_chat_tail.png` | 48 × 32 | Đuôi bong bóng chĩa xuống, cùng viền C1 | |
| C3 | `card_friend_9slice.png` | **256 × 128** | Card bạn bè bo góc 20 px, gỗ nhạt viền nâu | 9-slice viền 28 px |
| C4 | `avatar_frame_round.png` | 160 × 160 | Khung tròn gỗ + dây thừng mảnh | Ruột trong suốt |
| C5 | `relation_badge_friend.png` | 64 × 64 | Huy hiệu tròn xanh `#4A8CFF`, 2 bàn tay | |
| C6 | `relation_badge_sibling.png` | 64 × 64 | Huy hiệu tròn vàng `#FFD933`, 2 trái tim nhỏ | |
| C7 | `relation_badge_dating.png` | 64 × 64 | Huy hiệu tròn đỏ `#FF404D`, 1 trái tim lớn | |
| C8 | `splash_ring.png` | 96 × 96 | Vòng nước loang trắng–xanh nhạt, viền mềm, alpha giảm dần ra ngoài | |
| C9 | `splash_drop.png` | 32 × 32 | 1 giọt nước bắn (giọt lệ nghiêng), trắng xanh trong | Code bắn 10 giọt/lần |
| C10 | `fish_counter_quay.png` | **512 × 384** | **QUẦY CÁ** trên farm (bán cá / mua cần): quầy gỗ nâu ấm, mái vải sọc burgundy–kem, mặt quầy có thau nước + 2–3 con cá, **biển hiệu TRỐNG** | dòng A, chân chạm đáy canvas |
| C11 | `cast_meter_frame.png` | **384 × 96** | **Khung thanh đo lực quăng**: card **bo tròn**, viền gỗ nâu, ruột lõm màu kem | 9-slice viền 28 px, ruột phẳng |
| C12 | `cast_meter_fill.png` | 320 × 48 | Dải chạy trong khung C11: trái xanh lá (gần) → giữa vàng (vừa) → phải **đỏ cam rực** (xa = HOÀN HẢO) | Gradient ngang, không outline |
| C13 | `cast_meter_marker.png` | 32 × 72 | Con trượt chỉ lực: mũi tên nhỏ hướng xuống, viền nâu ruột kem | |
| C14 | `perfect_burst.png` | 256 × 256 | Vụ nổ tia sáng vàng cho chữ "HOÀN HẢO!" — 16 tia dài ngắn không đều, tâm sáng trắng | **Không chữ** |
| C15 | `zone_marker_water.png` | 128 × 64 | Gợn sóng hình thoi mờ đánh dấu điểm câu | alpha ~60% |

---

# PHẦN 3 — NGHIỆM THU (Lead kiểm khi nhận)

1. Đủ **47 file**, đúng tên, đúng 4 thư mục, PNG alpha thật.
2. **Zero text** trên mọi file — kiểm kỹ `building_ben_ho_cau.png` và `fish_counter_quay.png`: biển hiệu phải trống.
3. Outline: nâu `#331309` ± sai số nhỏ, **không đen tuyệt đối** (`#000000`), dày đúng ~1,2% cạnh dài, khép kín.
4. Ám màu ấm: R − B phải **dương** trên toàn ảnh (không được ngả lạnh/xám xanh).
5. 4 cần cùng canvas 64×240, tay cầm chạm đáy, thân thẳng cùng trục.
6. 10 cá cùng bố cục (đầu trái, thân ~75% ngang), đặt cạnh nhau nhìn thành một bộ.
7. 9-slice (C1, C3, C11): ruột phẳng 1 màu, viền đúng độ dày ghi trên.
8. D1 và D2 trùng khít pixel.
9. Đặt file mới cạnh `icon_chuong_ga_v2.png` mà thấy **cùng một người vẽ** thì đạt.

Nhận đủ → Lead chạy tool import: cá vào `Data/Fish/*.asset`, cần vào `Data/Rods/*.asset` + `RodVisual`, phao vào `Bobber`, icon vào các `Tab_*`/`Btn_*`, building vào `FishingDock`, quầy vào `FishCounter`, khung đo vào `CastPowerMeterUI`.
