# 🎨 PROMPT ĐỘI VẼ — GÓI "HỒ CÂU" (07/09/2026)

> Người ra đề: Tech Lead · Duyệt: Sếp Edric · Bối cảnh: `production/PLAN_HO_CAU_2026-09-07.md` · Báo cáo code: `production/BAO_CAO_VONG12_2026-09-07.md`
> **Thư mục giao hàng:** `production/art-handoff/2026-09-07_HoCau/` (3 gói con A · B · C bên dưới)
> **Tổng: 3 gói · 37 file PNG.** Ưu tiên A (trong nước) → B (icon) → C (UI). Nhân vật người chơi **KHÔNG cần vẽ** (đã có 2 sheet hành khách, code đã cắt 24 frame).

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

**Chỉ cần 3 việc:** vẽ đủ & đúng tên → thả đúng thư mục → nhắn Lead *"đã giao gói X"*.

## 🔒 LUẬT ART STUDIO (bắt buộc — `production/ART_RULES_STUDIO.md`)

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, số, logo, label, biển hiệu chữ trên bất kỳ asset nào. Text do game render bằng TMP. Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ/magenta dưới chân object.
3. ✅ **Meta Unity chuẩn**: spriteMode Single từng file · pivot Bottom-Center cho object đứng đất (Dev đặt qua tool, đội vẽ chỉ cần chân object chạm mép dưới canvas).
4. ✅ **Frame animation**: mọi frame cùng hướng = CÙNG kích thước canvas, thân đứng yên cùng vị trí; frame 01 = tư thế nghỉ; KHÔNG khói/hiệu ứng bake vào frame.
5. ✅ **Style chuẩn**: theo bộ Export_Train_UI_Package (burgundy `#8E1F3B` + đồng vàng `#D9A441`, gỗ nâu ấm, outline nâu đậm cartoon (không đen), dễ thương cho phụ nữ & trẻ em). Nhân vật tham chiếu: 2 sheet hành khách (cô gái nón bucket xanh rêu + máy ảnh, cậu bé balo) — cần câu/phao/quầy phải hợp tông màu rêu-be-nâu ấm này. Tham chiếu chi tiết: `production/art-handoff/STYLE_CONTRACT.md`.
6. ✅ Giao đúng TÊN FILE + THƯ MỤC bên dưới.

---

# GÓI A — TRONG NƯỚC: CẦN · PHAO · CÁ ⭐ ƯU TIÊN 1 · 15 file

📁 `A_Trong_Nuoc/`

Đây là những thứ người chơi nhìn CẢ BUỔI khi ngồi câu. Tất cả vẽ ở góc nhìn iso 45° cùng nhân vật (nhìn từ trên chéo xuống).

| # | File | Canvas | Vẽ gì |
|---|---|---|---|
| A1 | `rod_tier1_tre.png` | **64 × 240** | Cần TRE: thân tre vàng nhạt có mắt tre, dựng ĐỨNG THẲNG, tay cầm ở đáy canvas (pivot đáy), đầu cần ở đỉnh. KHÔNG vẽ dây (code vẽ dây bằng LineRenderer từ đầu cần tới phao). |
| A2 | `rod_tier2_go.png` | 64 × 240 | Cần GỖ: gỗ nâu ấm, có quấn dây tay cầm, 1 vòng khoen đồng vàng `#D9A441`. |
| A3 | `rod_tier3_carbon.png` | 64 × 240 | Cần CARBON: đen xám ánh xanh rêu, 2-3 khoen đồng, tay cầm bọc cork be. |
| A4 | `rod_tier4_vang.png` | 64 × 240 | Cần VÀNG (mua bằng kim cương): thân đồng vàng `#D9A441` sáng, khoen burgundy `#8E1F3B`, đầu cần có 1 viên đá nhỏ (không lấp lánh bake). |
| A5 | `bobber.png` | **32 × 32** | Phao câu: nửa trên đỏ burgundy, nửa dưới trắng ngà, chấm vàng nhỏ trên chóp. Tròn, outline nâu. Đây là frame nghỉ. |
| A6 | `fish_ro.png` … `fish_koi_vang.png` | **128 × 128** ×10 | Icon 10 loài cá, nằm ngang nhìn nghiêng, đầu bên trái, kiểu cartoon dễ thương, bố cục đồng nhất (thân chiếm ~75% ngang canvas). Tên file đúng bảng dưới. |

**10 icon cá (A6), tên file = fishId:**

| File | Loài | Độ hiếm (gợi ý màu viền/ánh) |
|---|---|---|
| `fish_ro.png` | Cá rô: thân xanh rêu sẫm, vây vàng nhạt | Thường |
| `fish_diec.png` | Cá diếc: bạc xám, bụng trắng | Thường |
| `fish_chep.png` | Cá chép: vàng cam nhạt, vảy rõ, râu | Thường |
| `fish_tre.png` | Cá trê: nâu đen bóng, râu dài, đầu bẹt | Không thường |
| `fish_loc.png` | Cá lóc: xanh đen, thân dài, vằn nhạt | Không thường |
| `fish_tram.png` | Cá trắm: xanh ô liu đậm, to khoẻ | Không thường |
| `fish_lang.png` | Cá lăng: vàng nâu, thân thon dài, râu | Hiếm (ánh bạc) |
| `fish_tai_tuong.png` | Cá tai tượng: tròn dẹt, xám xanh, vây đuôi loe | Hiếm (ánh bạc) |
| `fish_hoi.png` | Cá hồi: hồng cam, đốm đen | Sử thi (ánh vàng nhẹ) |
| `fish_koi_vang.png` | Cá koi vàng: vàng đồng `#D9A441` đốm trắng đỏ | Sử thi (ánh vàng nhẹ) |

---

# GÓI B — ICON UI HUD HỒ CÂU · 12 file

📁 `B_Icon_HUD/`  Tất cả **96 × 96**, phong cách icon phẳng có outline nâu, tô 2-3 tông, đồng bộ với icon tab đang có trong game (Shop/Kho/Chợ/Bếp). Không nền, không chữ.

| # | File | Vẽ gì | Dùng ở |
|---|---|---|---|
| B1 | `icon_tab_fishing.png` | Cần câu + phao nhỏ chéo 45° | Tab HỒ CÂU trên HUD farm (`Tab_Fishing`) |
| B2 | `icon_tab_basket.png` | Giỏ mây có 2 đuôi cá ló ra | Tab giỏ cá (`Tab_Basket`) |
| B3 | `icon_tab_friends.png` | 2 khuôn mặt tròn cạnh nhau (nam/nữ) | Tab bạn bè (`Tab_Friends`) |
| B4 | `icon_tab_chat.png` | Bong bóng thoại có 3 chấm | Tab chat (`Tab_Chat`) |
| B5 | `icon_private_on.png` | Ổ khoá ĐÓNG | Nút riêng tư bật (`Tab_Private/Img_On`) |
| B6 | `icon_private_off.png` | Ổ khoá MỞ | Nút riêng tư tắt |
| B7 | `icon_home_farm.png` | Nhà nông trại nhỏ mái đỏ burgundy | Nút Về farm (`Btn_Home`) |
| B8 | `icon_cast.png` | Cần câu vung ra, mũi tên cong | Nút QUĂNG (`Btn_Cast`) — đặt trên nút BtnGreen3D có sẵn |
| B9 | `icon_reel.png` | Cuộn dây xoay, mũi tên cong ngược | Nút THU (`Btn_Reel`) — trên BtnYellow3D có sẵn |
| B10 | `icon_gift_fish.png` | Hộp quà + đuôi cá | Tặng cá (`Btn_GiftFish`) |
| B11 | `icon_gift_gem.png` | Hộp quà + kim cương xanh | Tặng gem (`Btn_GiftGem`) |
| B12 | `icon_invite.png` | Phong bì có dấu + | Nút Mời (`Btn_Invite`) |

---

# GÓI C — KHUNG UI & THẾ GIỚI FARM · 10 file

📁 `C_Khung_UI/`

| # | File | Canvas | Vẽ gì | Ghi chú kỹ thuật |
|---|---|---|---|---|
| C1 | `bubble_chat_9slice.png` | **192 × 128** | Bong bóng thoại giấy kem viền nâu, bo góc, KHÔNG đuôi | 9-slice: viền 24 px mỗi cạnh, ruột phẳng một màu để kéo giãn |
| C2 | `bubble_chat_tail.png` | 48 × 32 | Đuôi bong bóng chĩa xuống, cùng màu viền C1 | Đặt dưới giữa bubble |
| C3 | `card_friend_9slice.png` | **256 × 128** | Card bạn bè bo góc 20 px, gỗ nhạt viền nâu | 9-slice viền 28 px |
| C4 | `avatar_frame_round.png` | 160 × 160 | Khung tròn gỗ + dây thừng mỏng cho avatar | Ruột trong suốt (avatar lồng dưới) |
| C5 | `relation_badge_friend.png` | 64 × 64 | Huy hiệu tròn xanh dương `#4A8CFF` hình 2 bàn tay | Nhãn kết nối Bạn bè |
| C6 | `relation_badge_sibling.png` | 64 × 64 | Huy hiệu tròn vàng `#FFD933` hình 2 trái tim nhỏ | Nhãn Chị em |
| C7 | `relation_badge_dating.png` | 64 × 64 | Huy hiệu tròn đỏ `#FF404D` hình 1 trái tim lớn | Nhãn Hẹn hò |
| C8 | `splash_ring.png` | 96 × 96 | Vòng tròn nước loang trắng-xanh nhạt, viền mềm, alpha giảm ra ngoài | Thay hình vẽ code của splash |
| C9 | `fish_counter_quay.png` | **512 × 384** | QUẦY CÁ ngoài farm (iso 45°): quầy gỗ nâu ấm mái vải sọc burgundy-kem, mặt quầy có thau nước và 2-3 con cá, **biển hiệu trống** không chữ | pivot Bottom-Center, chân quầy chạm đáy canvas, khớp tỉ lệ nhà cửa farm hiện có (so với `Home1` ~341 px cao) |
| C10 | `zone_marker_water.png` | 128 × 64 | Gợn sóng nhỏ hình thoi mờ để đánh dấu điểm câu (tuỳ chọn) | Đặt trên mặt nước, alpha 60% |

---

## ✅ NGHIỆM THU (Lead kiểm khi nhận)

1. Đủ **37 file**, đúng tên, đúng thư mục A/B/C, PNG alpha thật (không nền trắng/magenta), không chữ.
2. Cần câu 4 file **cùng canvas 64×240**, tay cầm chạm đáy, thân đứng thẳng cùng trục.
3. 10 icon cá cùng bố cục (đầu trái, thân ~75% ngang), độ hiếm thể hiện bằng ánh viền không bằng chữ.
4. 9-slice (C1, C3): ruột phẳng 1 màu, viền đúng độ dày ghi trên.
5. Quầy cá (C9) khớp tông farm, biển trống.

Nhận đủ → Lead chạy tool gắn: cá vào `Data/Fish/*.asset`, cần vào `Data/Rods/*.asset` + `RodVisual.rodSprite`, phao vào `Bobber.bobberSprite`, icon vào các `Tab_*`/`Btn_*` đúng tên slot ghi ở cột "Dùng ở".
