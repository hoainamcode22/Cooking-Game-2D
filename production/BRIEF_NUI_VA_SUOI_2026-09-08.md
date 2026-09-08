# BRIEF — NÚI & SUỐI CHẢY (theo video tham khảo Sếp gửi)
**08/09/2026 · Tech Lead** · Kèm ảnh: `production/_qc_iso45_art.jpg`, `production/_qc_elevation_tiles.jpg`

---

## PHẦN 1 — PHÂN TÍCH VIDEO (đã tách 19 khung hình, đo màu từng vùng)

Video 528×368, 9,7 giây, camera **pan dọc từ trên xuống**. Cảnh gồm **6 lớp xếp chồng**, đây là
điểm mấu chốt: **nó KHÔNG phải một bức tranh, mà là nhiều lớp tile + prop + hiệu ứng**.

| # | lớp | mô tả | ghi chú kỹ thuật |
|---|---|---|---|
| 1 | **Nền cỏ** | cỏ xanh tươi, sắc độ đều, có đốm hoa nhỏ rải rác | tilemap phẳng |
| 2 | **Khối núi / vách đá** | cao nguyên đá màu **nâu-oliu ấm**, mặt trên phẳng có cỏ phủ mép, mặt bên là vách dựng đứng nhiều tầng | **tileset cao độ** (elevation), không phải 1 ảnh |
| 3 | **Hồ trên đỉnh** | vũng nước nhỏ nằm trên mặt phẳng đá, chính là **nguồn** của thác | tile nước + viền đá |
| 4 | **Thác nước** | rơi qua **2–3 bậc**. Mỗi bậc: mép tràn hẹp → thân thác thẳng đứng → **hồ tiếp nước có bọt trắng** | animation / VFX, KHÔNG phải ảnh tĩnh |
| 5 | **Suối dưới chân núi** | dòng chảy **rộng dần**, uốn chéo theo trục iso, giữa dòng sẫm, sát bờ nhạt hơn | tilemap nước có bờ |
| 6 | **Chi tiết rìa** | đá nhỏ rải mép nước, bụi cỏ, cây thông bám sườn núi, hoa trắng nhỏ | prop rời rạc |

**Bốn thứ làm nó "đẹp" mà nếu thiếu sẽ trông rẻ tiền:**

1. **Thác chia BẬC, không đổ một mạch.** Mỗi bậc có một hồ tiếp nước + đám bọt trắng. Chính chỗ
   bọt trắng đó tạo cảm giác "nước đang rơi mạnh". Thác thẳng một đường từ trên xuống = trông như
   dán decal.
2. **Nước có 3 tầng độ sâu.** Sát bờ nhạt (thấy đáy) → giữa dòng đậm → chỗ dưới chân thác đậm nhất.
   Đo được từ video: nhạt `#5BC3E6` · vừa `#4FA0C8` · sâu `#035987`.
3. **Bờ nước không bao giờ là đường thẳng.** Luôn có viền đá lởm chởm + cỏ nhoè rủ xuống mép.
4. **Thân thác có 3–4 vệt sáng dọc chạy KHÔNG cùng tốc độ.** Đây là thứ tạo chuyển động; nếu cả
   khối nước trôi cùng nhau thì mắt đọc ra "ảnh đang trượt", không ra "nước đang chảy".

**Màu đo được từ video** (để đối chiếu, KHÔNG phải màu sẽ dùng — xem Phần 5):
thân thác `#5BC3E6` → `#88D8FD` → `#A4EBFF` · bọt `#EDFAFF` · sông sâu `#035987` ·
đá vách `#AAA376` / `#948958` / `#635B35` · cỏ `#77C918`.

---

## PHẦN 2 — 🟢 TIN TỐT: PROJECT ĐÃ CÓ SẴN QUÁ NỬA

Em lục kho trước khi viết brief. Trong `Assets/maptitle/Map45Iso/` (bộ tile ISO-45 chuẩn của game)
và `Assets/Day_Night/` đã có:

| đã có | đường dẫn | dùng được ngay? |
|---|---|---|
| **Texture nước của game** | `Map45Iso/Texture_WaterBase45.png` (130×130, PPU 64, tileable) | ✅ Đây CHÍNH LÀ màu nước chuẩn phải bám theo |
| **Prop núi + thác** | `Map45Iso/Prop_NuiThac.png` (367×380, PPU 90) | ⚠️ Là ảnh tròn kiểu icon, **không ghép tile được** — chỉ dùng làm THAM CHIẾU PHONG CÁCH |
| **Prop hầm núi** | `Map45Iso/Prop_TunnelNui.png` | ⚠️ như trên |
| **8 bộ rule-tile ISO-45** | `Sheet_IsoGrass45 / Dirt / DirtPatch / Dock / Fence / Rock / Sand / Stone` + 8 `RuleTile_Iso*45.asset` | ✅ **MẪU CHUẨN** cho sheet mới |
| **Tileset núi/vách** (kiểu top-down) | `Design_Map/HappyHarvest_NatureDecor/Tiles/Elevation/Sprite_Tiles_Elevation.png` 384×704, PPU 64, **30 miếng + 30 Tile asset** | ⚠️ Đẹp nhưng là **lưới VUÔNG**, không phải iso-45 ⇒ không ghép với nền game |
| **VFX thác nước** | `Day_Night/VFX/Water/VFX_CliffWater_Front.vfx` · `VFX_CliffWater_Side.vfx` · `ShaderGraph_CliffWater.shadergraph` | ✅ Dùng được, chỉ cần đặt vào scene |
| **VFX mặt nước gợn** | `VFX_WaterLines.vfx` · `VFX_WaterLinesStorm.vfx` — **đã có 2 object trong SCN_Farm rồi** | ✅ |
| **Shader nước có sóng** | `Day_Night/ShaderGraphs/ShaderGraph_Water.shadergraph` + `SubGraph_WaveSubGraph` | ✅ |
| **Tiếng nước chảy** | `Day_Night/Audio/Ambience/Water flowing.wav` | ✅ |
| **Tilemap nước** | `Water_Tilemap` + `Underwater_Tilemap` **đã có sẵn trong SCN_Farm** | ✅ chỉ thiếu tile để vẽ lên |

**Kết luận: Sếp KHÔNG cần đặt vẽ lại từ đầu.** Thiếu đúng **2 bộ sheet** (Phần 3).

---

## PHẦN 3 — THIẾU ĐÚNG 2 THỨ

### ❌ Thiếu 1 — `Sheet_IsoWater45.png` (nước + bờ, để vẽ suối bằng tilemap)
`Tiles/Water/` hiện chỉ có mỗi `Sprite_WaterIcon.png` — **không có tile nước nào**. Nên
`Water_Tilemap` trong scene đang rỗng, không vẽ suối được.

### ❌ Thiếu 2 — `Sheet_IsoCliff45.png` (vách núi theo lưới ISO-45)
Bộ Elevation của Happy Harvest là **lưới vuông**, đặt lên nền iso-45 sẽ lệch góc thấy ngay.

*(Thác thì KHÔNG cần vẽ — đã có `VFX_CliffWater`. Nếu Sếp muốn tránh VFX Graph trên mobile thì
đặt vẽ thêm bộ 3 ở cuối Phần 4.)*

---

## PHẦN 4 — 📋 PROMPT GIAO ĐỘI VẼ (copy nguyên khối này gửi đi)

> ### YÊU CẦU CHUNG — BẮT BUỘC ĐỌC TRƯỚC
> Game nông trại 2D **isometric 45°**, phong cách **casual hand-painted, màu tươi, viền mềm,
> KHÔNG pixel-art, KHÔNG cel-shading viền đen**. Đích: mobile iPhone/iPad.
> Toàn bộ art mới phải **ghép khít 100 %** với bộ tile đang chạy trong game.
>
> **HÌNH HỌC — SAI MỘT LY LÀ VỨT:**
> - Ô lưới là hình thoi **128 × 64 px** (tỉ lệ **đúng 2 : 1**).
> - File sheet: **1024 × 480 px**, chia lưới **8 cột × 6 hàng**, mỗi ô **128 × 80 px**
>   (hình thoi 128×64 nằm trong ô 128×80, chừa 16 px phía trên cho phần nhô cao).
> - **Pixels Per Unit = 128**, **pivot = (0.5, 0.6)** cho MỌI miếng.
> - Nền trong suốt (PNG-32). Không viền trắng, không nửa alpha lem ở mép thoi.
> - ⚠️ **Mẫu chuẩn tuyệt đối: `Sheet_IsoGrass45.png` (gửi kèm).** Mở file đó ra, đặt sheet mới
>   chồng lên, 4 đỉnh hình thoi phải trùng KHÍT từng pixel. Đây là tiêu chí nghiệm thu số 1.
>
> ---
> ### FILE 1 — `Sheet_IsoWater45.png` (1024 × 480, 48 miếng)
> Bộ tile **nước suối / hồ** để vẽ dòng chảy bằng tilemap. Bố cục 8 cột × 6 hàng:
>
> - **Hàng 1 (8 miếng) — MẶT NƯỚC GIỮA DÒNG.** 8 biến thể mặt nước sâu, gợn nhẹ khác nhau để
>   rải ngẫu nhiên không bị lặp mắt. Đây là miếng dùng nhiều nhất.
> - **Hàng 2 (8 miếng) — BỜ THẲNG.** 4 hướng bờ (Bắc / Đông / Nam / Tây theo trục thoi), mỗi
>   hướng 2 biến thể. Mép bờ: dải nước nhạt (thấy đáy) → viền cát/sỏi mảnh → cỏ nhoè rủ xuống.
> - **Hàng 3 (8 miếng) — GÓC.** 4 góc lồi + 4 góc lõm.
> - **Hàng 4 (8 miếng) — CHỖ CẠN & CHUYỂN TIẾP.** Nước cạn thấy đá cuội đáy, bãi sỏi ướt, chỗ
>   nước loang ra cỏ.
> - **Hàng 5 (8 miếng) — CHI TIẾT TRONG DÒNG.** Đá nhô giữa dòng có bọt trắng vòng quanh (3 cỡ),
>   khúc gỗ chìm, lá súng, gợn xoáy nhẹ, 2 miếng bọt trắng chảy xiết.
> - **Hàng 6 (8 miếng) — ĐẦU & CUỐI DÒNG.** 2 miếng "nguồn" (nước ứa ra từ khe đá), 2 miếng
>   "cửa suối" loe rộng, 4 miếng nối suối hẹp ↔ suối rộng.
>
> **Bảng màu nước — DÙNG ĐÚNG 6 MÃ NÀY, không tự chọn:**
> | vai trò | mã | ghi chú |
> |---|---|---|
> | sâu nhất (chân thác, giữa dòng) | `#065A7E` | |
> | **nền chuẩn** | `#0D81A9` | **lấy từ `Texture_WaterBase45.png` của game — màu gốc** |
> | giữa | `#1E9AC4` | |
> | cạn / sát bờ | `#46B7D8` | |
> | vệt sáng gợn | `#74CBE2` | trần sáng của texture gốc |
> | bọt trắng | `#E8F7FC` | |
>
> Cỏ mép bờ dùng `#7AB814` (sáng) / `#41640A` (bóng đổ) — trích từ `Sheet_IsoGrass45.png`.
> Sỏi/cát mép dùng `#A2A289` / `#8A8470`.
>
> ---
> ### FILE 2 — `Sheet_IsoCliff45.png` (1024 × 672, 48 miếng)
> Bộ **vách núi / cao độ** theo đúng lưới iso-45. Mặt trên là cỏ, mặt bên là đá dựng.
> *(Cao 672 vì hàng vách đứng cần thêm chiều cao — vẫn 8 cột, ô 128 × 112.)*
>
> - **Hàng 1–2 (16 miếng) — MÉP CAO NGUYÊN.** 4 cạnh thẳng + 4 góc lồi + 4 góc lõm + 4 biến thể,
>   mặt trên phủ cỏ, mép có cỏ rủ lởm chởm xuống vách.
> - **Hàng 3–4 (16 miếng) — MẶT VÁCH ĐÁ.** Vách dựng đứng nhiều thớ ngang, nứt dọc, có thể xếp
>   chồng nhiều tầng để núi cao tuỳ ý. Cần 4 biến thể để tường dài không lặp.
> - **Hàng 5 (8 miếng) — CHÂN VÁCH.** Chỗ đá gặp mặt đất: đá vụn, cỏ mọc chen, bóng đổ mềm.
> - **Hàng 6 (8 miếng) — MÉP THÁC.** ⭐ Quan trọng nhất: **rãnh cắt vào vách để nước chảy qua**.
>   Cần: 1 miếng mép tràn trên đỉnh (nước bắt đầu đổ), 2 miếng rãnh vách hai bên thác,
>   1 miếng hõm chân thác (chỗ hứng nước), 4 biến thể bậc trung gian.
>
> **Bảng màu đá — trích từ `Sheet_IsoRock45.png` của game:**
> mặt sáng `#9C927F` · mặt giữa `#887F6E` · nền `#7B7364` · bóng `#5A5449` · khe sâu `#453F36`.
> Cỏ trên đỉnh `#86C81C`, bóng cỏ `#41640A`.
>
> ---
> ### FILE 3 (TUỲ CHỌN) — `Sheet_Waterfall45.png` — thác dạng ảnh động
> *Chỉ đặt nếu muốn tránh VFX Graph trên máy yếu.* **512 × 1024, 8 khung animation**, mỗi khung
> 128 × 512 — một dải thác đổ dọc, ghép nối được đầu-đuôi để kéo dài tuỳ ý.
> Yêu cầu: **3–4 vệt sáng dọc trôi lệch pha nhau** (đây là thứ tạo cảm giác chảy), đỉnh thác mờ
> dần vào mép tràn, chân thác toè ra thành bọt trắng. 12 fps, lặp liền mạch không giật.
> Kèm 4 khung `Splash_Base` 256 × 128 cho đám bọt dưới chân.
>
> ---
> ### NGHIỆM THU — thiếu 1 mục là trả lại
> 1. Chồng lên `Sheet_IsoGrass45.png`: 4 đỉnh hình thoi **trùng khít từng pixel**.
> 2. Đúng 1024 px chiều ngang, ô 128 px, PPU 128, pivot (0.5, 0.6), PNG-32 nền trong.
> 3. Chỉ dùng các mã màu trong bảng (sai số ±3 mỗi kênh).
> 4. Ghép thử 3×3 miếng cùng loại: **không thấy đường nối, không thấy viền sáng**.
> 5. Đặt cạnh `Prop_NuiThac.png` và `Sheet_IsoGrass45.png`: cùng một game, không lệch phong cách.
> 6. Giao kèm file gốc phân lớp (PSD/Procreate) để sau còn sửa.

---

## PHẦN 5 — 🎨 BẢNG MÀU CHỐT (đã đối chiếu video ↔ màu game)

Màu trong video hơi sáng và ngả cyan hơn nước của game một chút. Em **giữ tông của game làm gốc**
(để suối mới không lạc lõng với hồ/biển đang có) và **mượn CẤU TRÚC 6 tầng của video**:

| tầng | video tham khảo | **CHỐT DÙNG** | lấy từ đâu |
|---|---|---|---|
| sâu nhất | `#035987` | **`#065A7E`** | tối hơn nền game, cùng sắc 195° |
| nền | — | **`#0D81A9`** | ⭐ `Texture_WaterBase45.png` — màu nước gốc của game |
| giữa | `#4FA0C8` | **`#1E9AC4`** | nội suy |
| cạn | `#5BC3E6` | **`#46B7D8`** | nội suy |
| gợn sáng | `#A4EBFF` | **`#74CBE2`** | trần sáng của texture gốc |
| bọt | `#EDFAFF` | **`#E8F7FC`** | |

Cả 6 mã cùng nằm trên sắc **~195°** nên chuyển tầng mượt, không bị "vá màu".

---

## PHẦN 6 — RÁP VÀO UNITY (sau khi có art)

1. **Cắt sheet**: Sprite Editor → Grid By Cell Size 128 × 80, Pivot **Custom (0.5, 0.6)**, PPU 128.
   Làm y hệt `Sheet_IsoGrass45.png` — mở file đó xem cấu hình rồi copy.
2. **Tạo Rule Tile**: nhân bản `RuleTile_IsoGrass45.asset` → đổi tên `RuleTile_IsoWater45`, thay
   48 sprite. Nhân bản để giữ nguyên bộ luật ghép cạnh đã chỉnh sẵn — **đừng tạo mới từ đầu**.
3. **Vẽ suối**: vẽ lên `Water_Tilemap` **đã có sẵn** trong scene. Nhớ nó phải là con của
   `Grid_Iso45` thì mới khớp ô 300 × 150 world.
4. **Thứ tự lớp** (dưới → trên): `Underwater_Tilemap` → `Water_Tilemap` → `Tilemap_IsoGrass`
   → vách núi → prop → thác.
5. **Thác**: kéo `VFX_CliffWater_Front.vfx` vào chỗ mép tràn, `VFX_CliffWater_Side.vfx` cho thác
   nhìn nghiêng. ⚠️ Đặt sorting **trên** vách đá nhưng **dưới** mọi popup.
6. **Mặt nước động**: gán `ShaderGraph_Water` cho material của `Water_Tilemap` — đã có sóng sẵn.
7. **Âm thanh**: `Water flowing.wav` + AudioSource 3D, bán kính ~1200 world (4 ô), quanh chân thác.
8. **Chặn đặt công trình lên nước**: thêm ô nước vào `occupiedCells` của `PlacementManager`, nếu
   không người chơi đặt nhà xuống giữa suối được.

⚠️ **Cảnh báo hiệu năng:** VFX Graph khá nặng trên iPhone đời cũ. Nếu test thấy tụt fps thì
chuyển sang **FILE 3** (thác dạng sprite animation) — nhẹ hơn nhiều, và trên màn điện thoại
gần như không phân biệt được.
