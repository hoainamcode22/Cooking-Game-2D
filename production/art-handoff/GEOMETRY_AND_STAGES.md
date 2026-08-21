# GEOMETRY & 5-STAGE MODEL — Cooking-Game-2D
> Mọi số dưới đây đo trực tiếp từ project. Nguồn ghi trong ngoặc.

## 1. Số đo nền tảng

| Đại lượng | Giá trị | Nguồn |
|---|---|---|
| PPU | **100** | `.png.meta → spritePixelsToUnits` (26/28 file) |
| Pivot cây/hoa/nhà | **(0.5, 0) bottom-centre** | 28 file meta |
| Ô đất `tile_dirt.png` | rect **700 × 345 px**, iso 2:1 | `maptitle/tile_dirt.png.meta` |
| Ô đất — world (scene) | **462.3 × 233.4 unit** (scale 66.04) | `SCN_Farm.unity` |
| Ô đất — world (prefab) | 350.0 × 172.5 unit (scale 100 × root 0.5) | `Plot_01.prefab` |
| Điểm trồng / ô | **12** (`CropPoint_1..12`) trên diamond ≈ **79%** mặt đất | `SCN_Farm.unity` |
| Chậu hoa `chauhoa_6` | **101 × 100 px** = đúng 1 cell (100 unit) | `bocaycoitrangtri/chauhoa.png.meta` |
| Chậu — điểm trồng | **(−3, +18)** so với tâm chậu (mặt đất trong chậu đo được +24) | `Chauhoa_1.prefab` |
| Grid đặt công trình | `PlacementManager.CELL = 100f` | `PlacementManager.cs:88` |
| Camera | ortho, default **950** (400–1500), ref 1920×1080 | `Main Camera.prefab:167-172` |

**Mật độ texel:** 1 px art ready-stage = 0.70 unit (scale 70) ≈ mật độ của đất (0.66 u/px).
→ **Đây là luật ngầm của project: art cây ở stage chín phải cùng mật độ texel với đất, sai lệch < 6%.**

## 2. Mô hình 5 STAGE (mới — thay cho 3 stage hardcode)

### 2.1 Ngữ nghĩa từng stage

| # | Cây trồng | Hoa |
|---|---|---|
| 1 | Vừa gieo — đất mới xới, hạt lấp ló, chưa nhú mầm | Vừa gieo — mặt đất chậu mới xới, hạt lấp ló |
| 2 | Mầm — 2 lá mầm nhỏ, cọng non | Mầm — 2 lá mầm nhú khỏi đất chậu |
| 3 | Cây non — lá thật, thân rõ, chưa có quả | Cây non — cụm lá xanh, chưa có nụ |
| 4 | Trưởng thành — cây đầy, ra hoa / quả non chưa chín | Nụ — nụ khép, hé màu cánh |
| 5 | **Chín — sẵn thu hoạch**, quả/củ rõ, màu no | **Nở rộ** — hoa bung hết cánh |

### 2.2 Luật hình học (điểm mấu chốt: "trồng từ dưới đất lên")

> **MỘT scale duy nhất cho cả 5 stage. Chiều cao lớn dần nằm TRONG art, không nằm ở scale.**

Lý do: pivot đã là bottom-centre nên đáy sprite **chính là** gốc cây. Nếu đổi scale theo stage như code cũ,
`PlotCropVisual.cs:150` cộng thêm `offsetY = (scale.y − sproutScale.y) * 0.3` và **nhấc cây khỏi mặt đất**
(lúa chín bị nhấc 6.0 unit, bắp cải 6.9, cà rốt 4.5). Giữ scale cố định → offsetY = 0 → cây luôn cắm xuống đất.

**Ratio chiều cao chuẩn (so với stage 5 = 100%):**

| Stage | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Chiều cao hiển thị | **18%** | 30% | 52% | 76% | **100%** |

### 2.3 Canvas sinh ảnh

Sinh **1 sheet / 1 cây**, grid **3 cột × 2 hàng**, ô **512 × 512 px** → sheet **1536 × 1024 px**.

- Ô 1→5 (đọc trái→phải, trên→dưới) = stage 1→5. **Ô 6 để trống, nền magenta thuần.**
- **Đường đất chung (BASELINE) ở y = 460 px trong MỌI ô** — gốc cây của cả 5 stage chạm đúng dòng này.
- Bề ngang tối đa 300 px, căn giữa ngang ô (x = 256).
- Chiều cao thân cây theo ratio §2.2 áp lên trần 440 px: **80 / 132 / 229 / 334 / 440 px**.

### 2.4 Kích thước xuất cuối (sau khi tách + trim + align feet)

| | Stage 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| **Cây trồng** (scale 70) | ~225×86 | ~225×144 | ~225×250 | ~225×365 | **~225×480** |
| **Hoa** (scale 50, trong chậu) | ~230×50 | ~230×83 | ~230×143 | ~230×209 | **~230×275** |

Hoa ready = 115% bề ngang chậu, 136% chiều cao chậu — đúng tỉ lệ hoa hồng hiện tại.

## 3. Nhà — 5 kiến trúc × 6 stage

| Prefab | Sprite hiện có | rect px | world unit | footprint |
|---|---|---|---|---|
| House_01 | `Assetsgame/Nhà/hom1.png` | 312 × 384 | 312 × 384 | 4 × 4 cell |
| House_02 | `home2.png` | 287 × 424 | 287 × 424 | 4 × 4 (⚠ **cao 4.24 cell, đã tràn**) |
| House_03 | `home3.png` | 394 × 416 | 394 × 416 | 4 × 4 |
| House_04 | `home4.png` | 278 × 406 | 278 × 406 | 4 × 4 |
| House_05 | `home5.png` | 340 × 388 | 340 × 388 | 4 × 4 |

### 3.1 Sáu stage

| # | Nội dung |
|---|---|
| 1 | Dàn giáo + móng bê tông + khung sườn gỗ trần, chưa có tường mái |
| 2 | Bắt đầu xây — tường lên ~1/3, dàn giáo còn nguyên, có vật liệu xếp quanh |
| 3 | Xây được nửa — tường gần đủ, mái mới lợp một phần, dàn giáo còn một bên |
| 4 | **Hoàn chỉnh** — nhà xong, tháo hết dàn giáo (đây là silhouette của sprite hiện tại) |
| 5 | Gói kín trong hộp quà — hộp + ruy băng + nơ bao trọn ngôi nhà |
| 6 | Hộp bung — nắp/cạnh hộp tách ra, lộ ~60% ngôi nhà bên trong |

### 3.2 Luật CỨNG cho nhà

> **Cả 6 stage của MỘT ngôi nhà phải dùng CHUNG một crop rect** = union của mọi stage, KHÔNG tight-bbox
> từng frame. Bài học đã ghi trong `MayAnimSetupTool.cs:39-44`: bbox riêng từng frame làm công trình
> **nhảy 12 px** giữa các frame. Rect chung khoá nó lại.

- Sinh 1 sheet / 1 nhà: grid **3 × 2**, ô **512 × 512** → **1536 × 1024**. 6 ô = 6 stage.
- Baseline chung **y = 470 px** mọi ô (chân nhà chạm đúng dòng này).
- Silhouette stage 4 **phải khớp sprite hiện tại** để không vỡ 15 instance đang đặt trong scene.
- Dàn giáo (stage 1–3) và hộp quà (stage 5–6) **được phép rộng/cao hơn** nhà — chính vì vậy mới cần rect chung.
- **KHÔNG nới House_02 thêm nữa** — nó đã tràn footprint 4 cell theo chiều cao.

## 4. Việc cần làm trong CODE (chưa làm — cần sếp duyệt riêng)

Đổi 3 → 5 stage **đụng DANH SÁCH DỪNG** của `AUTONOMY.md` (sửa logic lõi + đổi chữ ký public).
Chưa động vào. Đề xuất changeset:

1. `CropData.cs:35-37` — thay 3 field rời bằng `public Sprite[] growthStages = new Sprite[5];`
   Giữ 3 field cũ `[Obsolete]` + migration đọc sang array → 21 asset cũ không vỡ.
2. `CropData.cs:121 GetSprite(int)` — index vào array, clamp.
3. `PlotCropVisual.cs:126` — ngưỡng 3 mức → 5 mức: `stage = clamp(floor(progress01 * 5), 0, 4)`.
4. `PlotCropVisual.cs:150` — **xoá `offsetY`** (đặt = 0). Đây cũng là fix bug cây bị nhấc khỏi đất.
5. `CropData.cs:40-42` — gộp 3 scale thành **1 scale duy nhất** `plantScale`.
6. `CropData.cs:95 GetStageSprite(float)` — dead code, 0 caller → xoá.
7. Editor tool `Tools/Farm Game/Migrate Crop Stages 3→5` có report + undo, tự map cũ→mới
   (sprout→[0],[1] · growing→[2],[3] · ready→[4]) để game chạy được ngay trước khi có art mới.

## 5. Bug đã phát hiện trong lúc scan (báo cáo, chưa sửa)

| # | Bug | Vị trí |
|---|---|---|
| B1 | `Plot_01.prefab`: **7/11 CropPoint nằm NGOÀI** diamond đất → ô đất người chơi MUA có cây mọc lơ lửng | `CÔNG TRÌNH/Plot_01.prefab` |
| B2 | Prefab plot nhỏ hơn plot trong scene **24%** (350×172.5 vs 462.3×233.4) | như trên |
| B3 | 3 sorting layer **không tồn tại**: `Crop`, `CongTrinh`, `FX` + layer ID chết `1669604809` (195 SpriteRenderer dùng). Nếu `Crop` rơi về Default order 2 → cây bị vẽ **DƯỚI** tilemap `Dat_Nen` (11) và `Co_Grass` (12) trên 18/19 ô | `TagManager.asset` vs `PlotCropVisual.cs:22-23` |
| B4 | `Plot_01.prefab` thiếu `CropPoint_1` (chỉ có `_2`..`_12` = 11 điểm) trong khi `displayCount` = 12 | `Plot_01.prefab` |
| B5 | 6 chậu trong scene có CropPoint lệch **21.7 unit** so với prefab | `SCN_Farm.unity` |
| B6 | PPU sai 200 (thay vì 100) trên `cachualever3` + `namlever1` → 2 sprite này nhỏ bằng nửa | 2 file `.meta` |
| B7 | `HoaOaiHuong.asset` map stage lộn: sprout=`_3`, growing=`_0`, ready=`_1`, bỏ `_2` | `Hạt Hoa/HoaOaiHuong.asset` |
| B8 | `BapCai` scale outlier 10/30/33 (peers 50/60/70) | `Hat_giong/BapCai.asset` |
| B9 | `Ot.asset` stage 2 trỏ file vô danh `lever2-removebg-preview.png` — không rõ thuộc cây nào | `Assetsgame/hatgiong/` |
| B10 | Nhà: cả 5 prefab dùng sorting layer chết `1669604809`; `m_Size` baked đã lỗi thời (DrawMode Simple nên vô hại) | `House_0*.prefab` |
