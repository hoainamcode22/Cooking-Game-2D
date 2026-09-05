# 📤 ĐƠN ĐẶT ART — GỬI ĐỘI VẼ (agent-sprite-forge) — 2026-09-03

> **Sếp copy TOÀN BỘ file này dán cho GPT điều hành `agent-sprite-forge`.**
> Giao đúng TÊN FILE + THƯ MỤC ghi trong từng đơn → Lead sẽ tự cắt & gắn vào khung sườn bằng Editor Tool.
> Ảnh QC lỗi hiện tại (nền hồng cánh sen + kẻ lưới): `production/_qc5/qc_wh.jpg`, `qc_wc.jpg`, `qc_fg.jpg`

---

## 🎨 LUẬT ART STUDIO — BẮT BUỘC TUÂN THỦ (dán nguyên khối, không được lược)

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ trên BẤT KỲ asset nào. Text do game render bằng TMP. Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ dưới chân object — bóng do game tự vẽ để đồng nhất theo giờ trong ngày.
3. ✅ **Meta Unity chuẩn**: spriteMode Single từng file · pivot **Bottom-Center** cho object đứng đất.
4. ✅ **Frame animation**: mọi frame cùng hướng = **CÙNG kích thước canvas**, thân đứng yên cùng vị trí; frame 01 = tư thế nghỉ; **KHÔNG khói/bụi/tia sáng/hiệu ứng bake vào frame** (code phun runtime).
5. ✅ **Style chuẩn**: burgundy `#8E1F3B` + đồng vàng `#D9A441`, gỗ nâu ấm, outline nâu đậm cartoon, dễ thương cho phụ nữ & trẻ em.
6. ✅ Giao đúng **TÊN FILE + THƯ MỤC** được đặt trong đơn, không thêm file phụ (`_single`, `@2x` tự ý...).

### Bổ sung của Lead (đo từ art đang ship — `production/art-handoff/STYLE_CONTRACT.md`)
- Outline **nâu ấm sẫm, TUYỆT ĐỐI KHÔNG ĐEN**: `#442510` → `#654129` (hue 15–46). Hue outline luôn ấm/đỏ hơn hue phần fill. Dày **1.5–2.5%** cạnh dài nhất.
- Hand-painted **semi-realistic game-icon**, gradient airbrush mềm liên tục. **KHÔNG** cel-shading, **KHÔNG** dải màu phẳng, **KHÔNG** pixel-art, **KHÔNG** dither.
- Có specular bóng rời rạc (blob sáng mềm) + inner shadow nhẹ phía trong viền.
- 2 file reference BẮT BUỘC `view_image` trước mỗi lần gọi image_gen:
  `Assets/Assetsgame/hatgiong/bapcai-removebg-preview.png` · `Assets/Assetsgame/hatgiong/cachualever3-removebg-preview.png`

---

## 📐 LUẬT LƯỚI SPRITESHEET (MỚI — Lead ban hành 2026-09-03, áp dụng cho MỌI spritesheet từ nay)

Đợt trước 2/3 file bị lỗi vì không có luật này. Nghiệm thu bằng SỐ, không bằng cảm tính:

1. **Canvas phải CHIA HẾT cho lưới.** Ví dụ lưới 4 cột × 3 hàng ⇒ canvas `1200×900` (ô `300×300`), KHÔNG dùng `1200×896` (ô 298.67 → trôi dần).
2. **Mọi phần vẽ phải nằm TRỌN trong ô của nó.** Không một pixel nào được chạm/vượt đường biên ô — kể cả **cán búa, giỏ hoa, tà áo, tóc bay**. Chừa lề an toàn ≥ 8px mỗi cạnh.
3. **Mọi frame trong 1 sheet phải có BOUNDING BOX gần bằng nhau**: chênh lệch bề rộng ≤ 8px, bề cao ≤ 8px. (Đợt trước lệch tới **102–108px** ⇒ nhân vật giật ngang khi chạy animation.)
4. **Baseline chung**: đáy bàn chân của mọi frame phải nằm cùng một đường ngang (lệch ≤ 1px), tính từ đáy ô.
5. **Thân đứng yên**: tâm ngang của thân người không đổi giữa các frame (chỉ tay/chân/đạo cụ động).
6. **Alpha 0 tuyệt đối** ở nền: không pixel nào có alpha 1–32 (bóng ma mờ). Không mảnh rác rời khỏi thân ≥ 30px.
7. **KHÔNG bake khói/bụi/tia sáng/mồ hôi** vào bất kỳ frame nào.

---

## 🔴 ĐƠN 1 — VẼ LẠI `worker_hammer_spritesheet.png` (ưu tiên cao nhất)

**Lỗi đo được ở bản hiện tại** (`Assets/Art/Characters/Worker/worker_hammer_spritesheet.png`, 1200×896):
- Vùng nội dung theo cột = `(3,320) (328,620) (623,879) (926,1130)` nhưng biên ô lưới là `300/600/900` ⇒ **cán búa frame 1 tràn 20px sang ô frame 2, frame 2 tràn 20px sang ô frame 3**. Cắt lưới là dính búa của frame trước → đúng lỗi Sếp báo.
- 3 ô hàng cuối có **mảnh rác 104–288 px** nằm trong ô của frame khác.
- Bề rộng frame lệch **190→292 px (102px)**, bề cao lệch 35px ⇒ giật ngang.
- Hàng cuối có **khói/bụi trắng bake thẳng vào frame** ⇒ vi phạm luật #4.
- Canvas 896 không chia hết cho 3 hàng.

**Yêu cầu bản mới:**
- **Canvas `1200×900`**, lưới **4 cột × 3 hàng** = 12 frame, ô `300×300`.
- Thợ xây nam, mũ bảo hộ **vàng**, áo yếm/quần yếm **xanh dương đậm**, thắt lưng đồ nghề nâu — **giữ nguyên thiết kế nhân vật hiện tại**, chỉ sửa bố cục & làm sạch.
- Chu kỳ **đập búa 12 frame**: frame 01 = tư thế nghỉ (búa hạ, đứng thẳng) → nâng búa → vung lên đỉnh → giáng xuống → chạm đất → bật ngược lại nghỉ.
- **Cán búa phải nằm trọn trong ô** — nếu búa vung ngang quá dài thì **thu ngắn cán / xoay chéo lên**, đừng để chạm biên.
- **XOÁ SẠCH** mọi khói, bụi, tia lửa, đốm sáng ở hàng cuối.
- Baseline: đáy giày cách đáy ô đúng **20px** ở CẢ 12 frame.
- Thân người: tâm ngang cố định ở giữa ô (x = 150 trong ô).
- **Giao:** `Assets/Art/Characters/Worker/worker_hammer_spritesheet.png` (ghi đè)

---

## 🔴 ĐƠN 2 — VẼ LẠI `worker_celebrate_spritesheet.png`

**Lỗi đo được** (1200×896):
- **Ô hàng-cuối-cột-1 vẽ SAI NHÂN VẬT**: một anh **tóc đen, KHÔNG đội mũ bảo hộ**, mũ đang bay ra — khác hoàn toàn 11 frame còn lại. Phải vẽ lại frame này.
- 2 ô có mảnh rác rời, lớn nhất **1397 px**.
- Bề rộng frame lệch **133→241 px (108px)** ⇒ giật ngang.
- Nhiều frame có **khói/tia sáng bake vào** ⇒ vi phạm luật #4.

**Yêu cầu bản mới:**
- **Canvas `1200×900`**, lưới **4 cột × 3 hàng** = 12 frame, ô `300×300`.
- **CÙNG một nhân vật** với Đơn 1 (thợ xây mũ vàng, áo yếm xanh) ở **cả 12 frame** — mũ bảo hộ **luôn ở trên đầu**, không bay ra, không đổi màu tóc.
- Chu kỳ **ăn mừng 12 frame**: frame 01 = đứng nghỉ → giơ 2 tay → nhảy lên → xoay người vui → hạ xuống → về nghỉ.
- **XOÁ SẠCH** khói, bụi, tia chớp, đốm sáng.
- Baseline: ở frame chạm đất, đáy giày cách đáy ô đúng **20px**; frame nhảy lên thì thân nâng lên nhưng **tâm ngang giữ nguyên**.
- **Giao:** `Assets/Art/Characters/Worker/worker_celebrate_spritesheet.png` (ghi đè)

---

## 🟡 ĐƠN 3 — CHUẨN HOÁ `flowergirl_walk_spritesheet.png` (KHÔNG vẽ lại nhân vật)

**Tin tốt:** Lead đo bản hiện tại — **nền SẠCH HOÀN TOÀN**: 0 pixel alpha 1–32, 0 mảnh rác ≥30px, gutter giữa các ô sạch. Nhân vật vẽ đẹp, đúng style. **KHÔNG cần vẽ lại nhân vật.**

**Chỉ cần sửa 2 điểm kỹ thuật:**
1. Canvas hiện `848×1264`, lưới 3 cột × 4 hàng ⇒ ô `282.67×316` — **chiều ngang không chia hết**. Đổi canvas sang **`900×1264`** (ô `300×316`), giữ nguyên hình, chỉ căn lại vào lưới mới.
2. Bề rộng frame đang lệch **122→152 px (30px)**, bề cao lệch 8px. Căn lại cho **chênh lệch ≤ 8px** cả 2 chiều; baseline đáy giày cùng một đường (lệch ≤1px); tâm ngang thân cố định giữa ô.

- Bố cục giữ nguyên: hàng 1 = hướng xuống (down 1-3), hàng 2 = trái, hàng 3 = phải, hàng 4 = lên. Mỗi hàng 3 frame đi bộ.
- **Giao:** `Assets/Art/Characters/FlowerGirl/flowergirl_walk_spritesheet.png` (ghi đè)

---

## 🟡 ĐƠN 4 — 2 SPRITESHEET THỢ BÚA KHÁC (worker02, worker03) — task M7-10

Hiện game có 3 prefab thợ nhưng **chỉ dùng chung 1 bộ art**, chỉ khác lật ngang + co 6% ⇒ nhìn ra ngay là nhân bản.

Vẽ **2 người thợ MỚI**, cùng thế giới/cùng style/cùng khung xương với thợ ở Đơn 1-2, nhưng khác nhận dạng:
- **worker02**: mũ bảo hộ **cam**, áo yếm **nâu đất**, râu quai nón ngắn, dáng vạm vỡ hơn.
- **worker03**: mũ bảo hộ **trắng**, áo yếm **xanh lá rêu**, trẻ hơn, dáng gầy cao, khăn quàng cổ đỏ.

Mỗi người **2 sheet**, cùng quy cách Đơn 1-2 (canvas `1200×900`, lưới 4×3, ô 300×300, không khói bake, baseline 20px):
- `Assets/Art/Characters/Worker/worker02_hammer_spritesheet.png`
- `Assets/Art/Characters/Worker/worker02_celebrate_spritesheet.png`
- `Assets/Art/Characters/Worker/worker03_hammer_spritesheet.png`
- `Assets/Art/Characters/Worker/worker03_celebrate_spritesheet.png`

---

## 🟡 ĐƠN 5 — SỬA `meovuive` stage_2 — task M7-8

⚠ **Đính chính (Lead đã kiểm asset thật):** file asset tên `Mèo vui vẻ.asset` **nhưng field `itemName` bên trong là "Heo Vui Vẻ"** — và `itemName` mới là thứ người chơi thấy trong shop. Vậy **stage_3 vẽ HEO là ĐÚNG. Cái sai là stage_2 vẽ MÈO.**

- Vẽ lại **DUY NHẤT** `stage_2` thành **HEO**, cùng silhouette / cùng bệ đá / cùng baseline / cùng canvas với `stage_3`.
- **ĐỪNG đụng** stage_1, stage_3, stage_4, stage_5.
- **Giao:** `Assets/Art/Decor/Stages/meovuive/stage_2.png` (ghi đè)

---

## 🟡 ĐƠN 6 — 4 BỘ 5-STAGE CÒN THIẾU — task M7-9

4 vật trang trí này hiện chưa có bộ 5 stage nên vẫn giữ hành vi cũ (mua xong hiện luôn, không có tiến trình xây).

Mỗi slug cần đủ **5 file**: `stage_1.png` … `stage_5.png`, cùng canvas, cùng baseline, pivot Bottom-Center.
Ý nghĩa 5 stage (giống 15 bộ đã giao — xem `production/art-handoff/GEOMETRY_AND_STAGES.md`):
- `stage_1` = móng/khung đang dựng (sơ khai nhất)
- `stage_2` = đang xây dở (đã ra hình)
- `stage_3` = **thành phẩm hoàn thiện**
- `stage_4` = **hộp quà** đóng kín (che vật, cùng baseline)
- `stage_5` = khoảnh khắc mở hộp (nắp bung, vật lộ ra một phần)

| Slug (tên thư mục) | Vật |
|---|---|
| `banghieu` | Bảng hiệu gỗ — **BIỂN TRỐNG, KHÔNG CHỮ** (luật #1) |
| `ghehoa` | Ghế băng gỗ có giỏ hoa hai bên |
| `heothantai` | Heo thần tài (tượng heo vàng may mắn) |
| `vitvuive` | Vịt vui vẻ (tượng vịt trang trí) |

- **Giao:** `Assets/Art/Decor/Stages/<slug>/stage_1..5.png` (tạo thư mục mới)

---

## 🟢 ĐƠN 7 — 4 NHÂN VẬT POPUP LÊN CẤP (Sếp đã chốt: cắt từ NVGAME)

### Lead đã kiểm — TIN TỐT
`CelebrationCharacterSlot.cs` nhận `SetMaster(Sprite master, Sprite blink = null)` — **`blinkSprite` là TUỲ CHỌN**. Nghĩa là Lead có thể gắn thẳng sprite NVGAME có sẵn vào popup **NGAY, không cần đội vẽ**, hiệu ứng thở/đung đưa/nảy chạy đủ (chỉ thiếu chớp mắt).

**Vấn đề duy nhất là ĐỘ PHÂN GIẢI:**
| Slot | Sprite hiện tại | Sprite NVGAME |
|---|---|---|
| char_01, char_02 | **512×512** | 140–181 × 256–272 ⇒ phải phóng ~2× → **mờ nét** |
| char_03, char_04 | 304×304 / 305×305 | 256–272 ⇒ phóng ~12% → **chấp nhận được** |

⇒ **Lead sẽ gắn tạm bản NVGAME gốc để Sếp thấy ngay hôm nay.** Đơn dưới đây là bản VẼ LẠI để nét căng + có chớp mắt.

### 4 NHÂN VẬT ĐÃ CHỌN (Lead chọn theo độ đa dạng tuổi/giới/màu — ảnh tham chiếu: `production/_qc5/nvgame_all.jpg`)

| Slot | Nguồn | Mô tả nhân vật |
|---|---|---|
| char_01 | `Assets/NV_NPC/NVGAME/Processed/NV06/NV06_down_1.png` | **Ông lão nông dân** — tóc bạc, kính tròn, áo sơ mi kẻ xanh lá, yếm/tạp dề xanh rêu có túi đồ nghề, dang 2 tay thân thiện |
| char_02 | `.../NV08/NV08_down_1.png` | **Cô gái trẻ bím tóc** — mũ vành xanh lá, áo sọc ngang, yếm jean xanh, đeo máy ảnh trước ngực |
| char_03 | `.../NV10/NV10_down_1.png` | **Cậu bé da nâu** — tóc xoăn, kính bảo hộ đội trên đầu, áo thun vàng, quần short xanh |
| char_04 | `.../NV01/NV01_down_1.png` | **Chú đàn ông có râu** — mũ lưỡi trai kaki, áo gilê kaki nhiều túi, thắt lưng đồ nghề |

### YÊU CẦU VẼ

Với MỖI nhân vật, vẽ lại **đúng nhân vật đó** (giữ nguyên khuôn mặt, trang phục, màu sắc, phụ kiện — chỉ nâng độ phân giải & thêm tư thế ăn mừng), giao **2 file**:

| File | Nội dung |
|---|---|
| `char_0N_master.png` | **512×512**, toàn thân, tư thế **ĂN MỪNG** (giơ tay/vẫy tay/cười tươi) — đây là ảnh chính |
| `char_0N_blink.png` | **512×512**, **Y HỆT `_master` từng pixel, CHỈ khác đôi mắt NHẮM** (cung cong xuống). Dùng cho hiệu ứng chớp mắt |

**Bắt buộc:**
- `_blink` phải **trùng khít** `_master` — cùng canvas 512×512, thân/tay/chân/quần áo **không xê dịch 1 pixel nào**. Chỉ vùng mắt đổi. (Lệch là mặt sẽ giật khi chớp.)
- Toàn thân đứng, **pivot Bottom-Center**, chân chạm mép dưới cách đáy canvas **24px** ở cả 4 nhân vật (để 4 người đứng cùng một đường).
- Nền **alpha 0 tuyệt đối**, không bóng đổ, không viền sáng.
- Giữ đúng style gốc của NVGAME: outline nâu ấm, tô mềm, mặt tròn dễ thương.
- **KHÔNG CHỮ, KHÔNG SỐ** trên bất kỳ chi tiết nào (kể cả huy hiệu, nhãn áo).

**Giao vào:**
```
Assets/Art/UI/LevelUpV2/characters/char_01/char_01_master.png   (+ char_01_blink.png)
Assets/Art/UI/LevelUpV2/characters/char_02/char_02_master.png   (+ char_02_blink.png)
Assets/Art/UI/LevelUpV2/characters/char_03/char_03_master.png   (+ char_03_blink.png)
Assets/Art/UI/LevelUpV2/characters/char_04/char_04_master.png   (+ char_04_blink.png)
```
(Ghi đè file cũ. **KHÔNG** cần vẽ lại loạt `char_0N_f01..f12` — hệ đang chạy chế độ PUPPET 1 ảnh, loạt frame cũ không dùng tới.)

---

## ✅ NGHIỆM THU (Lead sẽ chạy script đo, không nghiệm thu bằng mắt)

Mỗi sheet phải qua đủ 7 mục của **LUẬT LƯỚI SPRITESHEET** ở trên. Lead chạy đo tự động và trả bảng số; sai mục nào trả lại mục đó.
