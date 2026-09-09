# 🎨 PROMPT ĐỘI VẼ — HỒ CÂU **BẢN V2** (08/09/2026) — THAY THẾ bản 07/09

> Người ra đề: Tech Lead · Duyệt: Sếp Edric · **Bản này THAY THẾ `PROMPT_SPRITE_FORGE_HO_CAU_2026-09-07.md`** (bản cũ chưa giao file nào, thư mục vẫn rỗng).
> **Thư mục giao hàng:** `production/art-handoff/2026-09-07_HoCau/` (giữ nguyên đường dẫn cũ, 4 gói con A · B · C · D).
> **Tổng: 4 gói · 47 file PNG.** Thứ tự ưu tiên: **D (lối vào + tab) → A (trong nước) → B (icon HUD) → C (khung UI)**.
> Nhân vật người chơi KHÔNG cần vẽ (2 sheet hành khách đã cắt sẵn 24 frame).

---

## ⛔ RANH GIỚI CÔNG VIỆC (lệnh Sếp)

**Đội vẽ CHỈ VẼ. Không chèn logic.**

| ❌ Không làm | Vì sao |
|---|---|
| Sửa `.cs` `.asset` `.prefab` `.unity` `.meta` | Code & scene do Dev sở hữu |
| Ghép sprite-sheet, tự cắt ô | Giao **PNG rời từng file** |
| Đổi tên file | Tên là **hợp đồng** — Dev/Sếp kéo đúng tên đó vào slot |
| Bake bóng/ánh sáng/nước động vào ảnh | Code làm runtime (day/night, sóng, splash) |
| Thêm file phụ (`_v2`, `_final`, `@2x`, sheet nguồn) | Thừa là phí công |
| Import thẳng vào `Assets/` | Chỉ thả vào art-handoff |

## 🔒 LUẬT ART STUDIO (bắt buộc — `production/ART_RULES_STUDIO.md`)

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT** — không chữ, số, logo, label, biển hiệu chữ trên **bất kỳ** asset nào, **kể cả công trình**. Sếp nhấn mạnh lại 08/09: *"các assets không được có 1 dòng text nào dính vào công trình"*. Chỗ nào thiết kế có biển → **vẽ biển TRỐNG**.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ** — alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ/magenta.
3. ✅ **Meta Unity**: spriteMode Single từng file · vật đứng đất thì **chân chạm mép dưới canvas** (Dev đặt pivot Bottom-Center).
4. ✅ **Frame animation**: cùng hướng = cùng kích thước canvas, thân đứng yên, frame 01 = tư thế nghỉ.
5. ✅ **Style**: burgundy `#8E1F3B` + đồng vàng `#D9A441`, gỗ nâu ấm, outline nâu đậm cartoon (không đen), dễ thương cho phụ nữ & trẻ em. Tham chiếu nhân vật: 2 sheet hành khách (cô gái nón bucket xanh rêu + máy ảnh, cậu bé balo). Chi tiết: `production/art-handoff/STYLE_CONTRACT.md`.
6. ✅ Giao **đúng tên file + đúng thư mục** bên dưới.

---

# GÓI D — LỐI VÀO HỒ CÂU + 2 TAB MỚI ⭐ ƯU TIÊN 1 · 5 file

📁 `D_LoiVao/`

Đây là thứ chặn tiến độ: farm hiện chưa có công trình nào để bấm vào hồ câu, 2 tab mới đang xài icon tạm.

| # | File | Canvas | Vẽ gì |
|---|---|---|---|
| D1 | `building_ben_ho_cau.png` | **768 × 640** | **BẾN HỒ CÂU** — công trình chính đặt trên farm, bấm vào là vào scene câu. Góc nhìn iso 45° giống nhà/chuồng farm hiện có. Nội dung: **sàn gỗ nhô ra mặt nước** (ván gỗ nâu ấm, có cọc chống), một **chòi nhỏ mái vải sọc burgundy-kem**, vài **cần câu dựng nghiêng** vào vách chòi, 1 **thùng gỗ + xô** đựng cá, 1 **phao đỏ trắng** treo. **BIỂN HIỆU TRỐNG** hình bầu dục gỗ treo trước chòi (KHÔNG CHỮ — game render chữ bằng TMP đè lên). Chân cọc chạm mép dưới canvas. Tỉ lệ: cao tương đương `Home1` của farm (~341 px art) nhân 1.8 vì đây là công trình điểm nhấn. |
| D2 | `building_ben_ho_cau_glow.png` | 768 × 640 | **Cùng canvas, cùng vị trí y hệt D1**, nhưng vẽ thêm viền sáng vàng nhạt quanh mép (dày 6-8 px, mờ dần) — dùng khi công trình sẵn sàng cho người chơi bấm. Không đổi hình chính. |
| D3 | `icon_tab_fishing_hub.png` | **128 × 128** | Icon tab **HỒ CÂU** trên thanh HUD farm (nằm cạnh phải tab Bếp). Phong cách **y hệt** 4 icon tab đang có (Shop/Kho/Chợ/Bếp): icon phẳng, outline nâu, 2-3 tông, vừa khung tròn/vuông bo. Nội dung: **cần câu chéo 45° + phao đỏ trắng + 2 gợn nước**. |
| D4 | `icon_tab_shop_tool.png` | 128 × 128 | Icon tab **CÔNG CỤ** trong cửa hàng (tab thứ 4, cạnh phải tab Trang trí). Cùng style 3 tab shop hiện có. Nội dung: **cần câu + rìu bắt chéo** hình chữ X, cán gỗ nâu, lưỡi rìu xám bạc. |
| D5 | `tool_axe.png` | **256 × 256** | **RÌU** bán ở tab Công cụ (chưa có chức năng, Sếp mua trước dùng sau). Rìu tay cầm gỗ nâu ấm, lưỡi thép xám xanh có ánh sáng, đai đồng vàng `#D9A441` chỗ nối. Đặt nghiêng 30°, đầu lưỡi hướng lên trái. |

---

# GÓI A — TRONG NƯỚC: CẦN · PHAO · CÁ · ƯU TIÊN 2 · 15 file

📁 `A_Trong_Nuoc/`  Góc nhìn iso 45° cùng nhân vật.

| # | File | Canvas | Vẽ gì |
|---|---|---|---|
| A1 | `rod_tier1_tre.png` | **64 × 240** | Cần **TRE**: thân tre vàng nhạt có mắt tre, dựng **ĐỨNG THẲNG**, tay cầm ở đáy canvas, đầu cần ở đỉnh. **KHÔNG vẽ dây** (code vẽ dây bằng LineRenderer). |
| A2 | `rod_tier2_go.png` | 64 × 240 | Cần **GỖ**: gỗ nâu ấm, quấn dây ở tay cầm, 1 khoen đồng vàng. |
| A3 | `rod_tier3_carbon.png` | 64 × 240 | Cần **CARBON**: đen xám ánh xanh rêu, 2-3 khoen đồng, tay cầm bọc cork be. |
| A4 | `rod_tier4_vang.png` | 64 × 240 | Cần **VÀNG** (mua bằng kim cương): thân đồng vàng `#D9A441` sáng, khoen burgundy, đầu cần gắn 1 viên đá nhỏ (không lấp lánh bake sẵn). |
| A5 | `bobber.png` | **32 × 32** | Phao câu: nửa trên đỏ burgundy, nửa dưới trắng ngà, chấm vàng trên chóp, outline nâu. |
| A6 | 10 file cá (bảng dưới) | **128 × 128** mỗi file | Icon cá nằm ngang nhìn nghiêng, **đầu bên trái**, cartoon dễ thương, thân chiếm ~75% chiều ngang canvas (bố cục đồng nhất cả 10 con). |

**10 icon cá — tên file = fishId, KHÔNG đổi:**

| File | Loài | Gợi ý màu | Độ hiếm |
|---|---|---|---|
| `fish_ro.png` | Cá rô | xanh rêu sẫm, vây vàng nhạt | Thường |
| `fish_diec.png` | Cá diếc | bạc xám, bụng trắng | Thường |
| `fish_chep.png` | Cá chép | vàng cam nhạt, vảy rõ, có râu | Thường |
| `fish_tre.png` | Cá trê | nâu đen bóng, râu dài, đầu bẹt | Không thường |
| `fish_loc.png` | Cá lóc | xanh đen, thân dài, vằn nhạt | Không thường |
| `fish_tram.png` | Cá trắm | xanh ô liu đậm, mình dày | Không thường |
| `fish_lang.png` | Cá lăng | vàng nâu, thân thon, râu | Hiếm (ánh bạc quanh viền) |
| `fish_tai_tuong.png` | Cá tai tượng | tròn dẹt, xám xanh, đuôi loe | Hiếm (ánh bạc) |
| `fish_hoi.png` | Cá hồi | hồng cam, đốm đen | Sử thi (ánh vàng nhẹ) |
| `fish_koi_vang.png` | Cá koi vàng | vàng đồng `#D9A441`, đốm trắng đỏ | Sử thi (ánh vàng nhẹ) |

---

# GÓI B — 12 ICON GIAO DIỆN TRONG SCENE CÂU · 12 file

📁 `B_Icon_HUD/`  Tất cả **96 × 96**, icon phẳng outline nâu, 2-3 tông, không nền, không chữ. Đây chính là "8 icon còn thiếu" Sếp nói, tôi liệt kê đủ 12 để bộ HUD đồng bộ.

| # | File | Vẽ gì | Slot trong game |
|---|---|---|---|
| B1 | `icon_tab_basket.png` | Giỏ mây có 2 đuôi cá ló ra | `Tab_Basket` (Giỏ) |
| B2 | `icon_tab_friends.png` | 2 khuôn mặt tròn cạnh nhau (nam/nữ) | `Tab_Friends` (Bạn bè) |
| B3 | `icon_tab_chat.png` | Bong bóng thoại có 3 chấm | `Tab_Chat` |
| B4 | `icon_private_on.png` | Ổ khoá **ĐÓNG** | `Tab_Private` bật (riêng tư) |
| B5 | `icon_private_off.png` | Ổ khoá **MỞ** | `Tab_Private` tắt |
| B6 | `icon_home_farm.png` | Nhà nông trại nhỏ mái đỏ burgundy | `Btn_Home` (Về farm) |
| B7 | `icon_cast.png` | Cần câu vung ra + mũi tên cong tới | `Btn_Cast` (QUĂNG) — đặt đè lên nút xanh |
| B8 | `icon_reel.png` | Cuộn dây xoay + mũi tên cong ngược | `Btn_Reel` (THU) — đè lên nút vàng |
| B9 | `icon_gift_fish.png` | Hộp quà + đuôi cá | `Btn_GiftFish` |
| B10 | `icon_gift_gem.png` | Hộp quà + kim cương xanh | `Btn_GiftGem` |
| B11 | `icon_invite.png` | Phong bì có dấu + | `Btn_Invite` (mời bạn) |
| B12 | `icon_zoom.png` | Kính lúp có dấu + ở giữa | 2 nút zoom (game tự lật thành −) |

---

# GÓI C — KHUNG UI & THẾ GIỚI · 15 file

📁 `C_Khung_UI/`

| # | File | Canvas | Vẽ gì | Ghi chú kỹ thuật |
|---|---|---|---|---|
| C1 | `bubble_chat_9slice.png` | **192 × 128** | Bong bóng thoại giấy kem viền nâu bo góc, **không đuôi** | 9-slice: viền 24 px mỗi cạnh, ruột phẳng 1 màu |
| C2 | `bubble_chat_tail.png` | 48 × 32 | Đuôi bong bóng chĩa xuống, cùng màu viền C1 | |
| C3 | `card_friend_9slice.png` | **256 × 128** | Card bạn bè bo góc 20 px, gỗ nhạt viền nâu | 9-slice viền 28 px |
| C4 | `avatar_frame_round.png` | 160 × 160 | Khung tròn gỗ + dây thừng mỏng | Ruột trong suốt |
| C5 | `relation_badge_friend.png` | 64 × 64 | Huy hiệu tròn xanh `#4A8CFF`, 2 bàn tay | Nhãn Bạn bè |
| C6 | `relation_badge_sibling.png` | 64 × 64 | Huy hiệu tròn vàng `#FFD933`, 2 trái tim nhỏ | Nhãn Chị em |
| C7 | `relation_badge_dating.png` | 64 × 64 | Huy hiệu tròn đỏ `#FF404D`, 1 trái tim lớn | Nhãn Hẹn hò |
| C8 | `splash_ring.png` | 96 × 96 | Vòng nước loang trắng-xanh nhạt, viền mềm, alpha giảm dần ra ngoài | |
| C9 | `splash_drop.png` | 32 × 32 | 1 giọt nước bắn (hình giọt lệ nghiêng), trắng xanh trong | Code bắn 10 giọt/lần |
| C10 | `fish_counter_quay.png` | **512 × 384** | **QUẦY CÁ** trên farm (bán cá / mua cần): quầy gỗ nâu ấm, mái vải sọc burgundy-kem, mặt quầy có thau nước + 2-3 con cá, **biển hiệu TRỐNG** | pivot chân chạm đáy canvas |
| C11 | `cast_meter_frame.png` | **384 × 96** | **Khung thanh đo lực quăng** (Sếp yêu cầu 08/09): card **bo tròn** viền gỗ, ruột lõm màu kem, chia ngầm 3 vùng | 9-slice viền 28 px, ruột phẳng |
| C12 | `cast_meter_fill.png` | 320 × 48 | Dải màu chạy trong khung C11: trái xanh lá (gần) → giữa vàng (vừa) → phải **đỏ cam rực** (xa/HOÀN HẢO) | Gradient ngang, không outline |
| C13 | `cast_meter_marker.png` | 32 × 72 | Con trượt chỉ lực: mũi tên nhỏ hướng xuống, viền nâu ruột kem | |
| C14 | `perfect_burst.png` | 256 × 256 | Vụ nổ tia sáng vàng cho chữ "HOÀN HẢO!" (16 tia không đều, tâm sáng) | Không chữ |
| C15 | `zone_marker_water.png` | 128 × 64 | Gợn sóng nhỏ hình thoi mờ đánh dấu điểm câu | alpha ~60% |

---

## ✅ NGHIỆM THU (Lead kiểm khi nhận)

1. Đủ **47 file**, đúng tên, đúng thư mục A/B/C/D, PNG alpha thật (không nền trắng/magenta).
2. **Zero text** trên mọi file, đặc biệt `building_ben_ho_cau.png` và `fish_counter_quay.png` — biển hiệu phải trống.
3. 4 cần cùng canvas 64×240, tay cầm chạm đáy, thân đứng thẳng cùng trục.
4. 10 icon cá cùng bố cục (đầu trái, thân ~75% ngang); độ hiếm thể hiện bằng ánh viền, không bằng chữ.
5. 9-slice (C1, C3, C11): ruột phẳng 1 màu, viền đúng độ dày ghi trên.
6. D1 và D2 **trùng khít pixel** (chỉ khác viền sáng).

Nhận đủ → Lead chạy tool gắn: cá vào `Data/Fish/*.asset`, cần vào `Data/Rods/*.asset` + `RodVisual.rodSprite`, phao vào `Bobber`, icon vào các `Tab_*`/`Btn_*`, building vào `FishingDock`, quầy vào `FishCounter`, khung đo vào `CastPowerMeterUI`.
