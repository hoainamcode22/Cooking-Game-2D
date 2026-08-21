# STYLE CONTRACT — Cooking-Game-2D Farm Art
> Nguồn: đo trực tiếp từ sprite đã ship trong project (không mô tả cảm tính).
> Mọi prompt sinh ảnh PHẢI tuân file này. Ngày lập: 2026-08-19.

## 0. Hai file tham chiếu chuẩn (GOLDEN REFERENCES)

Style đích **đã tồn tại trong project**. Không mô tả bằng lời — dùng đúng 2 file này làm reference ảnh:

| Vai trò | Đường dẫn | Kích thước | Outline đo được |
|---|---|---|---|
| REF_A — bắp cải | `Assets/Assetsgame/hatgiong/bapcai-removebg-preview.png` | 409 × 610 | `#654129`, ~9 px (≈1.5% cạnh dài) |
| REF_B — cà chua | `Assets/Assetsgame/hatgiong/cachualever3-removebg-preview.png` | 503 × 496 | `#442510`, ~4 px (≈0.8% cạnh dài) |

**LUẬT BẮT BUỘC khi chạy skill:** trước mỗi lần gọi `image_gen`, phải `view_image` 2 file trên để chúng
hiện trong context. `generate2dsprite/SKILL.md` quy định rõ: reference phải là ảnh **visible**, KHÔNG được
truyền đường dẫn dạng chuỗi rồi coi đó là reference.

## 1. Rendering

- Hand-painted **semi-realistic game-icon**. Gradient airbrush mềm, liên tục.
- **KHÔNG** cel-shading, **KHÔNG** dải màu phẳng, **KHÔNG** dither, **KHÔNG** pixel-art.
  (Đo: p90 |dL/dy| trong thân = 6.8–51, không có plateau; 4.298–54.112 màu duy nhất mỗi sprite.)
- Có **specular bóng rời rạc** (blob sáng mềm), không chỉ shading khuếch tán.
- Có **inner shadow nhẹ** ngay phía trong đường viền.

## 2. Outline — luật quan trọng nhất

- **Nâu ấm sẫm, TUYỆT ĐỐI KHÔNG đen.** Khoảng hợp lệ: `#442510` → `#654129` (hue 15–46).
- Hue của outline **luôn ấm/đỏ hơn** hue của phần fill. (VD bắp cải: outline hue 24 vs fill hue 82.)
- Bề dày: **1.5–2.5% cạnh dài nhất** của vật thể. Vật nhỏ → mỏng, vật lớn → dày.
- Outline **bao trọn** silhouette, khép kín, không đứt đoạn.
- Không dùng outline trắng kiểu sticker (đó là ngôn ngữ của layer UI, `Farm/Art/UI_LevelUp/spr_star.png` — giữ tách biệt).

## 3. Bảng màu (đo thực từ 1.24 triệu pixel thân)

| Vai trò | Sẫm | Trung | Sáng |
|---|---|---|---|
| Xanh lá — thân/lá cây trồng | `#364824` | `#728848` | `#B5CC73` |
| Xanh lá — lá hoa (sạch hơn) | `#2F5225` | `#5C8440` | `#94BB5C` |
| Nâu — đất/gỗ/thân | `#513524` | `#7C563A` | `#D69D62` |
| Đỏ — quả | `#64120C` | `#A32F25` | `#D35D46` |
| Vàng/gold — lúa, ngô, chanh | `#2F2C0F` | `#A2993D` | `#F7EB89` |
| Hồng/đỏ hoa | `#913E4C` | `#DC8899` | `#FDC4D2` |
| Lạnh (CHỈ dùng cho hoa) | `#453F7A` | `#736BB0` | `#ADA3E2` |
| Tím lan/cẩm tú cầu | `#713464` | `#B183B0` | `#EDCAF2` |
| Tối nhất toàn cục | `#2B0F09` | | |
| Sáng nhất toàn cục | | | `#FDFDEF` |

- Xanh lá của game là **olive/vàng-lục (hue 80–107)**, KHÔNG phải xanh ngả lam.
- Nâu nằm trong dải hẹp hue 22–31.
- Toàn bộ palette **ám ấm** — R−B luôn dương (+56 đến +95).

## 4. Ánh sáng & độ bão hoà

| Chỉ số | Giá trị đích | Ghi chú |
|---|---|---|
| Hướng sáng | **trên–trái** | tâm vùng sáng nhất lệch dx ≈ −0.06, dy ≈ −0.15 của bbox |
| Saturation trung bình | **65.5%** | p90 = 92.7% |
| Value trung bình | **61.2%** | |
| Dải sáng L(p95)−L(p5) | **148** | tương phản cao, không bệt |

Art cây hiện tại đang **thấp hơn ~13 điểm saturation** so với đích — art mới phải rực hơn art cũ.

## 5. Nền & rìa

- Nền **magenta đặc `#FF00FF`** khi sinh raw (bắt buộc để chroma-key). Không gradient, không bóng đổ nền.
- Xuất cuối: **RGBA trong suốt hoàn toàn**, `alphaIsTransparency: 1`.
- **KHÔNG bóng đổ tiếp đất baked vào sprite** — không sprite nào hiện có làm vậy.
- ⚠️ **Rìa phải sạch.** 16 file hiện tại dính viền trắng do remove.bg (tệ nhất `ngoscale1` 78%, `khoaitaylever3` 68%,
  `carotlever3` 62%, `tile_dirt` 44%). Art mới **không được** tái tạo lỗi này.

## 6. Import Unity (khớp asset đang chạy)

```
spritePixelsToUnits : 100
spriteMode          : 2 (Multiple)
alignment           : 7 (Custom)
pivot               : {x: 0.5, y: 0}   ← ĐÁY sprite CHÍNH LÀ gốc cây
alphaIsTransparency : 1
filterMode          : 1 (Bilinear)     ← SỬA: hiện đang 0 (Point), sai cho art painterly
```

## 7. Art hiện có sẽ chỏi khi đặt cạnh art mới (ưu tiên xử lý)

1. 30+ sprite growth stage trong `Assetsgame/hatgiong/` — botanical, KHÔNG outline, nhạt màu.
   *Cà chua đang lệch ngay trong chính nó: stage 3 có outline, stage 1/2/4 thì không.*
2. Cả 10 sheet hoa trong `Assetsgame/Hoa/` — không outline, sáng từ trên thay vì trên-trái.
3. `Assetsgame/conga-removebg-preview.png`, `conheo-removebg-preview.png` — chibi cartoon, sai hẳn register.
4. Ảnh `.jpg` thật trong `Assets/Anh/` (chili, egg, rice, salt, sugar, peppercorns, soy_sauce, fish_sauce, Tofu, rau).
5. `Assets/Anh/farm.png`, `nongdan.png`, `ngoinhacoooking.png` — cartoon truyện tranh.
6. `Assets/maptitle/tile_grass.png` — gradient phẳng cạnh đất painterly có texture.

> Ghi chú: `Assets/Anh/` gần như là **bản sao byte-identical** của `Assets/Assetsgame/` — mọi pass restyle
> phải xử lý 1 lần rồi đồng bộ, tránh làm 2 lần.
