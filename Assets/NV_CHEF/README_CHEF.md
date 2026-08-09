# NV_CHEF — Nhân vật ĐẦU BẾP (đứng yên, tự diễn xào nấu)

Unity **6000.3.10f1**. Nhân vật **KHÔNG di chuyển**, không WASD, không Rigidbody — chỉ lặp **2 động tác** nấu.

> ### ⚠ BẢN RÚT GỌN — CHỈ CÒN 2 ĐỘNG TÁC: `Idle` và `Stir`
> `Flip` (xào lắc chảo) và `Finish` (hoàn thành/tắt lửa) **ĐÃ BỊ XOÁ HOÀN TOÀN**: clip, state,
> parameter, transition, sprite và toàn bộ code.
> **Sheet PNG vẫn còn 2 hàng art đó** (hàng 3 và 4) — tool cố ý **bỏ qua**, không cắt.
> Muốn bật lại: xem [mục 13](#13-thêm-lại-động-tác-nếu-đổi-ý).

---

## 1. Dùng như thế nào

**Cách nhanh nhất (đã dựng sẵn):**
> Kéo `Assets/NV_CHEF/Chef_NPC.prefab` vào map → bấm **Play** → đầu bếp tự diễn.

**Nếu cần dựng lại từ đầu (hoặc sau khi thay PNG):**
1. Menu **`Tools/Farm/Setup Nhân Vật Đầu Bếp`**
2. Bấm **`1. Phân tích sheet`** → đọc bảng, kiểm tra số hàng / số frame / cảnh báo lệch chân
   Bảng sẽ ghi **`4 hàng (DÙNG 2)`** và tô **xám** 2 hàng thừa kèm nhãn `⟨HÀNG THỪA — KHÔNG DÙNG⟩` — **đúng như thiết kế**.
3. Bấm **`2. CẮT + TẠO TẤT CẢ`** → làm hết: cắt sprite → 2 clip → Animator → prefab
4. Nếu muốn làm sạch hoàn toàn: **`3. Xoá và làm lại`** (xoá clip/controller/prefab + xoá sprite rect rồi dựng lại)

> **Sau khi rút xuống 2 động tác, bấm nút `2` (hoặc `3` cho chắc) để ghi lại `Chef.controller` + `Chef_NPC.prefab`.**
> Nút `3` là cách **chắc chắn** nhất để sprite rect `Chef_Flip_*` / `Chef_Finish_*` cũ biến mất khỏi `.meta` của PNG.
> ⚠ Cả hai nút đều **xoá rồi tạo lại** `Chef.controller` và `Chef_NPC.prefab` → **GUID mới**.
> `SCN_Farm.unity` đang có **1 instance đầu bếp** tại `x = 2098.3, y = -3064.8` (scale 200, `m_SortingOrder` override 3565).
> Sau khi bấm nút `2`/`3`, instance đó sẽ báo **Missing Prefab** → xoá nó, bấm nút **`4`** để đặt lại, rồi kéo về đúng chỗ cũ.
> (Đây là hành vi có từ trước, không phải do việc rút xuống 2 động tác.)

---

## 2. Cấu trúc đã tạo

```
Assets/NV_CHEF/
├─ preview_all-removebg-preview.png     ← sheet gốc (763 x 327, RGBA, nền trong suốt)
│                                          4 hàng art, CHỈ cắt 2 hàng đầu = 13 sprite căn theo chân
├─ Chef.controller                      ← Animator: 2 state, entry = Idle, chỉ dùng Trigger
├─ Chef_NPC.prefab                      ← KÉO CÁI NÀY VÀO MAP
├─ README_CHEF.md                       ← file này
├─ Animations/
│   ├─ Chef_Idle.anim      6 frame @10fps = 0.6s   LOOP
│   └─ Chef_Stir.anim      7 frame @10fps = 0.7s   LOOP
│      (Chef_Flip.anim và Chef_Finish.anim ĐÃ XOÁ cùng file .meta)
├─ Scripts/
│   ├─ ChefCookLoop.cs     ← vòng diễn tự động (runtime)
│   └─ ChefYSort.cs        ← Y-sort (runtime)
└─ Editor/
    ├─ ChefSetupTool.cs    ← tool menu Tools/Farm/...
    └─ ChefSheetAnalyzer.cs← bộ dò lưới frame từ alpha
```

`Chef_NPC` = `SpriteRenderer` + `Animator` + `ChefCookLoop` + `ChefYSort`.
**KHÔNG** có Rigidbody2D, **KHÔNG** có Collider, **KHÔNG** có script input — đây là NPC trang trí đứng yên.

---

## 3. Số liệu chốt (tool tự dò từ alpha, không hardcode)

| Hàng | Clip | Frame | Dải y (Unity, y↑) | Mốc đất (y) | Bước ngang | Cao thân |
|---|---|---|---|---|---|---|
| 1 | `Idle` chuẩn bị/bắc chảo | **6** | 251–318 | 251 | 96.0 px | 68 px |
| 2 | `Stir` đảo bằng sạn | **7** | 170–239 | 170 | 95.3 px | 70 px |
| ~~3~~ | ~~xào lắc, lửa bùng~~ **KHÔNG DÙNG** | ~~7~~ | 88–156 | 88 | 95.5 px | 69 px |
| ~~4~~ | ~~hoàn thành/tắt lửa~~ **KHÔNG DÙNG** | ~~8~~ | 6–75 | 6 | 95.4 px | 70 px |

> Quy đổi sang PIL/Photoshop (y=0 ở TRÊN): `yPIL = 326 - yUnity`.
> Ví dụ mốc đất Idle y=251 (Unity) = y=75 (PIL).

**Rect dùng chung cho MỌI sprite: `84 x 74` px** · đệm đáy `2` px · nửa chiều rộng `42` px
· Pivot **Bottom-Center** · PPU **100** · **13 sprite** (6 + 7) · **0 frame bị kẹp biên** · **0 frame bị cắt mất hình**

Tên sprite: `Chef_Idle_0..5`, `Chef_Stir_0..6` — chỉ 13 cái.

### 🔑 Vì sao vẫn PHÂN TÍCH cả 4 hàng dù chỉ DÙNG 2

`ChefSheetAnalyzer.Analyze()` **luôn dò hết 4 hàng**; giới hạn nằm ở `ChefSetupTool.SoHangDung()`
(= `min(số hàng dò được, AnimNames.Length)`) và được áp cho **cả 3 khâu**: cắt sprite, tạo clip, tạo state.

**Cố ý, không phải sót:** rect dùng chung `84 x 74` được tính từ **tầm với xa nhất của MỌI hàng**
(chảo vung, lửa bốc). Nếu chỉ phân tích 2 hàng thì rect **co lại** → sprite `Idle`/`Stir` đổi kích thước,
đổi vị trí nội dung trong rect, đổi cả **scale prefab**. Ta muốn `Idle`/`Stir` giữ **nguyên từng pixel**
như trước khi rút gọn, nên analyzer phải thấy cả sheet.

Hệ quả phụ: bảng phân tích hiện cảnh báo *"dò được 4 hàng nhưng danh sách tên có 2"* và analyzer đặt tên
tạm `Row2` / `Row3` cho 2 hàng thừa. **Đây là bình thường** — những tên đó chỉ tồn tại trong bộ nhớ,
**không** bao giờ trở thành sprite `Chef_Row2_*` hay clip `Chef_Row2.anim` (tool cắt vòng lặp trước đó).

### Import settings
| Thiết lập | Giá trị | Lý do |
|---|---|---|
| Texture Type | `Sprite (2D and UI)` | |
| Sprite Mode | `Multiple` | 13 frame được cắt trong 1 sheet |
| Pixels Per Unit | `100` | khớp chuẩn dự án |
| Compression | `None` | nền trong suốt + viền mềm, nén sẽ ra vệt bẩn quanh viền |
| Alpha Is Transparency | `true` | tránh viền tối khi lọc bilinear |
| Generate Mip Maps | `false` | sprite 2D không cần, mip làm mờ khi zoom |
| Mesh Type | `FullRect` | sprite nhỏ, Tight không lợi mà lại tạo mesh khác nhau mỗi frame |
| **Filter Mode** | **`Bilinear`** | xem mục 4 |

---

## 4. Vì sao Filter Mode = **Bilinear** (không phải Point)

Đã **mở ảnh xem** và **đo bằng code**, kết luận đây **KHÔNG phải pixel-art**:

- **29.8 %** pixel có hình mang **alpha trung gian** (viền khử răng cưa mềm) — pixel-art thật gần như chỉ có alpha 0 hoặc 255.
- **26 873 màu đục** khác nhau trên ảnh 763×327 — pixel-art thường vài chục tới vài trăm màu.
- Tô bóng chuyển sắc mượt, không có ô pixel vuông rõ (độ dài run màu trung bình ~3.4 px, không đều).

`Point` sẽ biến 30 % viền mềm đó thành **răng cưa cứng** và làm **sọc dải** vùng chuyển sắc — càng rõ vì prefab phóng scale **200** (ảnh bị kéo to hơn 1:1). `Bilinear` giữ viền mượt.

---

## 5. Cắt sprite: VÌ SAO căn theo CHÂN

**Không** cắt bounding-box khít từng frame → mỗi frame một rect khác nhau → pivot bottom-center rơi vào chỗ khác nhau → nhân vật **nhảy giật**.
**Không** cắt lưới đều → `763 / 8 = 95.375` không chia hết → lưới trôi dần, frame cuối bị cắt.

**Cách làm:** mọi frame **cùng một rect `84x74`**, đặt sao cho **điểm chân luôn ở đúng một chỗ trong rect**.

1. Dò dải hàng/cột từ alpha. Dải **vụn** (khói, đốm lửa bay rời) được **gộp** vào dải chính gần nhất — nếu không, đốm khói ở y(PIL) 87–89 sẽ bị đếm thành **hàng thứ 5** và làm sai toàn bộ mapping clip.
2. Mỗi frame: `footY` = dòng pixel thấp nhất có alpha; `feetCenterX` = tâm ngang của **5 dòng dưới cùng** (không lấy tâm cả thân — cánh tay/chảo/lửa vung sang một bên sẽ kéo tâm đi mỗi frame một chỗ).
3. **Mốc đất theo HÀNG** = *trung vị* `footY` của hàng. Frame nào chân cao hơn mốc (vd một frame nhấc chân 3 px) thì **giữ nguyên độ nhấc** đó → đúng chủ ý hoạ sĩ. Nếu căn từng frame theo đáy riêng thì cái nhấc 3 px biến thành **đất tụt 3 px** → giật 1 frame.
4. **Khớp lưới bền vững (Theil–Sen)** cho tâm chân: `anchor = intercept + pitch * index`. Frame lệch > `3 px` bị **ép về đường thẳng** (chắc chắn là lỗi đo do vật thể phụ chạm tầm chân); lệch nhỏ thì giữ số đo thật. Dùng Theil–Sen (trung vị hệ số góc từng cặp) chứ không dùng bình phương tối thiểu, vì least-squares bị chính điểm lỗi kéo lệch cả đường.
5. `rect.x = round(anchorX) - 42`, `rect.y = mốc đất - 2`, có **clamp trong biên ảnh**.

---

## 6. ⚠ Frame có nguy cơ rung — ĐỌC TRƯỚC KHI THAY ART

Trong **2 hàng đang dùng**, tool tự phát hiện và **đã sửa** 1 frame lệch tâm chân:

| Frame | Lệch so lưới | Nguyên nhân | Xử lý |
|---|---|---|---|
| **`Chef_Idle_0`** | **−8.0 px** ⚠ | **CÁN CHẢO thò ra ngang tầm chân** → 5 dòng dưới cùng rộng 49 px (các frame khác chỉ 22–24 px) nên tâm chân bị kéo lệch sang trái | ép về lưới → hết rung |

*(Hàng 3 và 4 cũng từng có 2 frame lệch −4.0 px / −3.1 px, nhưng 2 hàng đó không còn được cắt nên không còn liên quan.)*

Đây là **frame duy nhất vượt ngưỡng cảnh báo 5 px** trong sheet hiện tại: `Chef_Idle_0`. Nếu **không** ép về lưới thì frame đầu của Idle sẽ **nhảy ngang 8 px** mỗi vòng loop — rất dễ thấy.

**Rủi ro còn lại (rất nhỏ):** `rect.x` là số nguyên còn `anchorX` có thể là `.5` → sai số làm tròn **≤ 0.5 px**. Ở scale 200 và camera ortho 1200 thì tương đương **< 0.5 pixel màn hình** — mắt không thấy.
Muốn triệt tiêu hẳn: bật **`Pivot chính xác tuyệt đối`** trong tool (dùng pivot `Custom` đặt đúng lên chân, sai số 0 px) — đổi lại pivot không còn là Bottom-Center chuẩn.

**Hàng `Stir` bị "bẩn" đồng đều:** cả 7 frame đều có chảo hạ thấp nên `feetW = 43 px` ở mọi frame → tâm chân lệch **giống nhau** ở mọi frame → **không rung**, chỉ khiến cả hàng Stir dịch ngang ~2 px so với hàng Idle. Vô hại vì nhân vật đứng một chỗ.

---

## 7. Scale prefab = **200** — phép tính đầy đủ

**Quy ước dự án (đã kiểm chứng, không đoán):**
- `Assets/_Game/Farm/CÔNG TRÌNH/*.prefab` — `House_01`, `House_02`, `Đài nước`, `giếng_01`, `Bù nhìn`, `Bảng hiệu`, `Chậu hoa`… **tất cả root `m_LocalScale: 100`**
- Sprite công trình PPU **100** → ở scale 100 thì **1 pixel sprite = 1 world unit**
- **1 ô lưới = 100 world unit**
- Đối chiếu: `House_01` sprite `312 x 384 px` → **312 x 384 world unit** = ~3.1 x 3.8 ô. Camera ortho size **1200** → thấy 2400 world unit = 24 ô theo chiều dọc.

> ⚠ Lưu ý: brief ban đầu ghi "công trình dùng scale **150**". **Sai** — 150 là của bộ asset thiên nhiên bên thứ ba `maptitle/…/HappyHarvest_NatureDecor` (cỏ, đá, hàng rào), **không phải** công trình của game. Công trình dùng **100**. Con số dưới đây tính theo **100**.

**Phép tính:**

```
cao thân người      = 68 px          (lấy từ hàng Idle — hàng duy nhất KHÔNG có lửa/khói
                                      bốc lên làm phồng chiều cao)
→ ở scale 1         = 68 / 100 PPU        = 0.68 world unit
mục tiêu            = 1.35 ô x 100 unit/ô = 135 world unit
scale thô           = 135 / 0.68          = 198.5
làm tròn bội số 5   →                       200      (cho designer dễ nhớ / dễ chỉnh tay)

KIỂM TRA LẠI:
cao thật  = 0.68 x 200 = 136 world unit = 1.36 ô   ✅ nằm trong dải yêu cầu 1.2–1.5
cả rect   = 0.74 x 200 = 148 world unit = 1.48 ô   ✅ vẫn ≤ 1.5 (phần thừa là LỬA/KHÓI bốc lên)
bề rộng   = 0.84 x 200 = 168 world unit            (rect rộng vì chừa chỗ chảo vung; thân người chỉ ~0.6 ô)
so với nhà = 136 / 384 = 35 % chiều cao House_01   ✅ hợp lý cho người đứng cạnh nhà
```

Tool **tự tính lại** con số này mỗi lần chạy (`ChefSetupTool.TinhScale`), nên thay PNG khác → scale tự đổi theo, không cần sửa tay. Muốn nhân vật to/nhỏ hơn: đổi hằng `TargetCells` trong `ChefSetupTool.cs`.

---

## 8. Animation & Animator

### Clip
- `frameRate = 10`
- `Idle` **và** `Stir` đều **loop** (`AnimationClipSettings.loopTime = true`). Không còn clip nào không-loop vì `Finish` đã bị bỏ
- Bind qua `EditorCurveBinding` (`type = SpriteRenderer`, `path = ""`, `propertyName = "m_Sprite"`) + `AnimationUtility.SetObjectReferenceCurve`
- **n frame → n+1 keyframe.** Key thứ `n+1` lặp lại sprite cuối. **Vì sao:** `AnimationClip.length` = thời điểm key cuối. Nếu chỉ có `n` key tại `0..(n-1)/fps` thì `length = (n-1)/fps`, frame **cuối** chỉ tồn tại một khoảnh khắc rồi wrap về 0 → động tác **thiếu 1 frame** và loop giật. Thêm key cuối làm `length = n/fps`, mỗi frame hiện đủ `1/fps` giây.

### Animator `Chef.controller`
- **2 state**: `Idle`, `Stir` — **entry = `Idle`**
- **2 Trigger**: `ToIdle`, `ToStir` — **KHÔNG dùng Bool** (Bool phải nhớ tự tắt, quên là kẹt state; Trigger được transition tiêu thụ ngay)
- **2 transition**: `Idle → Stir` (`ToStir`) và `Stir → Idle` (`ToIdle`), cả hai chuyển tức thì (`hasExitTime = false`, `duration = 0`) — script đã chờ đủ số vòng clip rồi mới bắn trigger
- **Vì sao `Stir → Idle` dùng trigger, không dùng `hasExitTime`:** `Stir` là clip **LOOP** nên không bao giờ "hết" để tự thoát. Đặt `hasExitTime` cho nó sẽ khiến nó về `Idle` sau **đúng 1 vòng**, phá thẳng `soVongStir`. (Trước đây `Finish → Idle` dùng được `hasExitTime = 1.0` chỉ vì `Finish` là clip **không** loop.)
- Nguy cơ trigger `ToIdle` **đọng** lại được `ChefCookLoop.SetOnly()` dập trước mỗi lần set — xem mục 9

### API đã chọn để ghi sprite rect
**API 2D mới**: `SpriteDataProviderFactories` + `ISpriteEditorDataProvider` (**không** dùng `TextureImporter.spritesheet` đã deprecated).
Có tiền lệ chạy tốt trong chính dự án này: `Assets/NV_01/Editor/SetupPlayerNV01.cs` dùng đúng bộ API đó.
Kèm `ISpriteNameFileIdDataProvider` để **giữ ổn định fileID theo TÊN sprite** → cắt lại lần 2 **không** làm `.anim` / `.prefab` mất tham chiếu sprite.

---

## 9. `ChefCookLoop.cs` — vòng tự diễn

```
Idle (2–4 s ngẫu nhiên)  →  Stir (3 vòng)  →  về Idle  →  (lặp mãi)
```

Chờ theo **độ dài clip thật** (`AnimatorStateInfo.length`), **không hardcode giây** — thay PNG (số frame khác) hoặc đổi `frameRate` thì thời lượng tự đúng theo.

| Field Inspector | Mặc định | Ý nghĩa |
|---|---|---|
| `idleMinSeconds` / `idleMaxSeconds` | 2 / 4 | thời gian nghỉ ngẫu nhiên giữa 2 lượt nấu |
| `lamTronTheoVongClip` | ✔ | làm tròn thời gian nghỉ thành **số vòng nguyên** của clip Idle → Idle luôn kết thúc đúng frame cuối, không bị cắt giữa vòng |
| `soVongStir` | 3 | số lần lặp clip Stir (field số-vòng **duy nhất** còn lại) |
| `tuDongDien` | ✔ | tự diễn khi vào scene; tắt thì chờ code gọi `BatDauDien()` |
| `treKhoiDongNgauNhien` | 0 | **đặt > 0 khi rải nhiều đầu bếp** để họ lệch pha, không diễn trùng khớp như robot |
| `hetHanVaoState` | 2 s | quá hạn = Animator thiếu state/transition → log cảnh báo thay vì treo im |

API công khai: `BatDauDien()`, `DungDien()`.

**Bẫy Trigger đã xử lý:** `SetTrigger` mà không transition nào tiêu thụ thì trigger **đọng lại** và nổ sai lúc sau (ví dụ `ToIdle` đọng từ lượt trước sẽ giết clip `Stir` ngay frame đầu). Nên trước mỗi lần set, script **reset toàn bộ trigger khác** (`SetOnly`).

**Animator `cullingMode = AlwaysAnimate` — CỐ Ý.** `ChefCookLoop` chờ động tác bằng cách **đọc state hiện tại** của Animator. Nếu bật `CullCompletely`, lúc đầu bếp ra ngoài khung hình thì Animator **ngừng xử lý transition** → coroutine chờ mãi không thấy state mới → hết hạn + spam warning. Chi phí animate 1 sprite không đáng kể, đổi lấy hành vi luôn đúng.

---

## 10. ⚠⚠ SORTING — VẤN ĐỀ ĐANG CÓ CỦA DỰ ÁN

**Sorting layer hiện có** (`ProjectSettings/TagManager.asset`), theo thứ tự vẽ từ dưới lên:

| # | Tên | uniqueID |
|---|---|---|
| 0 | `Bottom` | 1161173501 |
| 1 | `Default` | 0 |
| 2 | **`Objects`** ← Chef dùng layer này | 1471039481 |
| 3 | `ObjectsFront` | 3561676937 |
| 4 | `Foreground` | 1304480043 |

### 🔴 218 sprite trỏ vào sorting layer ĐÃ BỊ XOÁ

Đã kiểm chứng lại trong `SCN_Farm.unity`: **218** `SpriteRenderer` có `m_SortingLayerID: 1669604809` — ID này **không còn tồn tại** trong `TagManager.asset`. Toàn bộ prefab trong `_Game/Farm/CÔNG TRÌNH/` cũng vậy.

ID chết → Unity coi như **layer index 0 (= `Bottom`)**, tức **nằm DƯỚI** layer `Objects`.

**Hệ quả thực tế:** cho tới khi 218 renderer đó chưa được trỏ lại, **đầu bếp sẽ LUÔN vẽ trên công trình**, bất kể `Order in Layer` — vì so sánh **LAYER thắng** so sánh **ORDER**.

**Cách sửa** (nằm **NGOÀI** thư mục `NV_CHEF` nên **tôi KHÔNG tự sửa**, đúng ràng buộc):
> Trỏ 218 `SpriteRenderer` đó về sorting layer **`Objects`**.
> Sau khi sửa, đầu bếp che/bị che **đúng ngay**, không cần đổi gì trong `NV_CHEF`.

**Tuyệt đối KHÔNG copy sorting từ prefab cũ** — sẽ dính lại ID rác. `ChefYSort.Awake()` luôn đặt layer **từ TÊN**, và `ChefSetupTool` cũng đặt `sortingLayerName` khi dựng prefab.

### `ChefYSort.cs` — công thức

```
sortingOrder = baseOrder - round(y * orderPerUnitY)
             = 500      - round(y * 1)
```

Vật ở **DƯỚI** (y nhỏ hơn) = gần camera hơn trong góc nhìn top-down → order **lớn hơn** → **vẽ đè lên**.
Dấu này **trùng** với `Assets/NV_01/Scripts/YSortIso.cs` (`order = -y * sortScale`) nên hai hệ tương thích.

| Field | Mặc định | Ghi chú |
|---|---|---|
| `sortingLayerName` | `Objects` | tự đặt ở `Awake` → chống layer rác |
| `baseOrder` | **500** | khớp `m_SortingOrder: 500` mà công trình đang dùng. Đặt **0** nếu muốn khớp nhân vật NV_01 (đang dùng `YSortIso`, order = `-y`) |
| `orderPerUnitY` | **1** | map này toạ độ cỡ **±2000** world unit → order ra ~`500 ± 2000`, **an toàn** trong giới hạn `±32767`. **ĐỪNG để 100** — order sẽ tràn và bị kẹp sai |
| `sortPoint` | trống | trống = dùng chính transform. Pivot sprite là Bottom-Center nên `transform.position.y` **chính là** chỗ chân đứng → sort đúng ngay, không cần offset |
| `luonCapNhat` | ✘ | vẫn tự cập nhật khi `y` đổi, chỉ bỏ qua khi `y` không đổi cho nhẹ |

### Vì sao viết `ChefYSort` riêng thay vì dùng `YSortIso.cs`

Đã đọc `Assets/NV_01/Scripts/YSortIso.cs`. Công thức **đúng** nhưng thiếu 3 thứ đầu bếp bắt buộc phải có:

1. **Thiếu `baseOrder`.** Công trình dùng `m_SortingOrder: 500` **cố định**. `YSortIso` cho `order = -y`, tức mốc so sánh là **0** → đầu bếp chỉ đè công trình khi `y < -500`, hoàn toàn không liên quan tới `y` của công trình. Có `baseOrder = 500` thì mốc giao nhau về đúng dải order của công trình.
2. **Thiếu điều khiển `sortingLayer`.** Prefab dễ bị để sai layer mà không ai biết — đúng cái bệnh dự án đang mắc.
3. **Lãng phí.** `YSortIso` tính lại mỗi `LateUpdate`. Đầu bếp **đứng yên vĩnh viễn**; Edric rải 20 con thì đó là 20 phép tính vô ích mỗi frame. `ChefYSort` chỉ tính lại khi `y` **đổi thật**, kèm chống tràn `±32767`.

### Giới hạn cần biết
Y-sort **đúng tuyệt đối** đòi **cả hai phía** dùng cùng công thức. Công trình hiện dùng order **cố định 500** (không theo y), nên không có công thức nào của riêng đầu bếp có thể đúng với **mọi** công trình. Sau khi 218 renderer được trỏ về `Objects`, nên cho công trình dùng **cùng công thức** `order = 500 - round(y)` (gắn `ChefYSort` hoặc `YSortIso` với `baseOrder = 500`) — lúc đó việc che/bị che sẽ đúng hoàn toàn.

`sortingOrder` được áp **lúc runtime** (không dùng `[ExecuteAlways]` để tránh làm bẩn diff scene khi kéo prefab).

---

## 11. Thay art (Edric đọc mục này)

Tool **tự dò lưới từ alpha**, **không** hardcode toạ độ nào → thay PNG khác vẫn chạy.

1. Thay file PNG (hoặc trỏ đường dẫn mới trong tool). Yêu cầu: nền **trong suốt**, mỗi hàng là **một động tác**, thứ tự hàng **trên → dưới** phải bắt đầu bằng `Idle`, `Stir` (hàng thứ 3 trở đi sẽ bị **bỏ qua** cho tới khi bạn thêm tên vào `AnimNames`).
2. Bấm **`1. Phân tích sheet`**. Kiểm tra:
   - **2 hàng đầu** có đúng số frame (tool tô **cam** nếu lệch số kỳ vọng 6/7). Hàng thứ 3+ tô **xám** kèm `⟨HÀNG THỪA — KHÔNG DÙNG⟩` — **bình thường**
   - dòng nào bị tô **cam/đỏ** → xem cột `lệch` (px). Frame lệch lớn mà **chưa** được ép về lưới sẽ **rung**
   - **`✖ CẮT MẤT HÌNH`** (đỏ) = rect không chứa hết nội dung → **tăng `Lề an toàn`** hoặc chừa viền cho sheet
3. Bấm **`2. CẮT + TẠO TẤT CẢ`**.
4. Số frame khác đi thì clip/scale **tự đổi theo**, không phải sửa code.

**Đổi số động tác / tên clip:** xem mục 13.

**Núm xoay trong tool khi art "khó":**

| Núm | Mặc định | Khi nào chỉnh |
|---|---|---|
| `Ngưỡng alpha` | 10 | art có viền glow rất mờ → **tăng**; art viền cứng → giảm |
| `Số dòng lấy tâm chân` | 5 | chân nhỏ/lớn khác → chỉnh cho vừa 2 bàn chân, **đừng** lấn ống quần |
| `Lề an toàn (px)` | 2 | bị `✖ CẮT MẤT HÌNH` → **tăng** |
| `Ép về lưới nếu lệch >` | 3 px | còn rung → **giảm** (ép mạnh tay hơn); art cố tình lắc người → **tăng** |
| `Cảnh báo nếu lệch >` | 5 px | chỉ ảnh hưởng màu cảnh báo, không đổi kết quả cắt |
| `Pivot chính xác tuyệt đối` | ✘ | muốn sai số **0 px** thay vì ≤ 0.5 px (đổi lại pivot thành `Custom`) |

---

## 12. Tóm tắt rủi ro còn lại

| Rủi ro | Mức | Chi tiết |
|---|---|---|
| **Đầu bếp vẽ đè lên mọi công trình** | 🔴 **CAO** | 218 renderer trỏ sorting layer đã xoá `1669604809` → về layer `Bottom`, nằm dưới `Objects`. **Không sửa được từ trong `NV_CHEF`.** Xem mục 10 |
| Y-sort chưa đúng tuyệt đối | 🟠 vừa | công trình dùng order **cố định 500**, không theo `y`. Cần cho công trình dùng cùng công thức — xem mục 10 |
| `Chef_Idle_0` rung 8 px | 🟢 **đã xử lý** | ép về lưới. Nếu thay art, **kiểm lại bảng phân tích** |
| Sai số làm tròn ≤ 0.5 px | 🟢 rất thấp | < 0.5 pixel màn hình. Bật `Pivot chính xác tuyệt đối` nếu muốn triệt tiêu |
| Sheet có **4 hàng** nhưng chỉ dùng **2** | 🟢 **cố ý** | tool bỏ qua hàng 3–4 ở cả 3 khâu (cắt / clip / state). Cảnh báo *"dò 4 hàng, có 2 tên"* là **bình thường** — xem mục 3 |
| Rect `Chef_Flip_*` / `Chef_Finish_*` còn sót trong `.meta` của PNG | 🟢 thấp | `SetSpriteRects` **thay thế** toàn bộ danh sách nên nút `2` là đủ. Tool tự đọc lại PNG sau khi cắt và **log cảnh báo** nếu còn sprite lạ → lúc đó bấm nút `3` |
| Nhiều đầu bếp diễn trùng khớp | 🟢 thấp | đặt `treKhoiDongNgauNhien > 0` |

---

## 13. Thêm lại động tác (nếu đổi ý)

Sheet PNG **vẫn còn nguyên 2 hàng art chưa dùng**: hàng 3 = *xào lắc chảo, lửa bùng* (7 frame),
hàng 4 = *hoàn thành / tắt lửa* (8 frame). Không có gì bị xoá khỏi ảnh — chỉ code thôi.

Muốn bật lại `Flip` (ví dụ), làm **đúng 3 chỗ**:

**1) `ChefSetupTool.cs`** — 3 mảng phải **cùng độ dài** và **cùng thứ tự hàng**:
```csharp
private static readonly string[] AnimNames     = { "Idle", "Stir", "Flip" };
private static readonly int[]    ExpectedFrames = { 6, 7, 7 };
private static readonly bool[]   LoopFlags      = { true, true, true };
```
Không cần sửa gì khác về số hàng — `SoHangDung()` tự nới ra theo `AnimNames.Length`.
Nếu động tác thêm vào **không loop** (kiểu `Finish`) thì đặt `LoopFlags` phần tử đó = `false`
**và** nhớ transition về `Idle` phải dùng `hasExitTime = true, exitTime = 1.0` (đừng dùng trigger — trigger đọng sẽ cắt clip ngay frame đầu).

**2) `ChefCookLoop.cs`** — thêm lại hằng + field + 1 dòng trong `VongDien()` + 1 dòng `ResetTrigger` trong `SetOnly()`:
```csharp
public const string StateFlip = "Flip";
public const string TrigFlip  = "ToFlip";
private static readonly int HashFlip = Animator.StringToHash(StateFlip);
[Min(1)] public int soVongFlip = 2;
// trong VongDien(), sau dòng Stir:
yield return DienState(TrigFlip, HashFlip, StateFlip, Mathf.Max(1, soVongFlip), -1f);
// trong SetOnly(): _animator.ResetTrigger(TrigFlip);
```
⚠ **Bắt buộc thêm `ResetTrigger`** cho trigger mới, nếu không nó sẽ **đọng** và cắt ngang động tác sau.

**3) `ChefSetupTool.TaoController()`** — thêm state, `AddParameter`, và transition (`Trig(idle, flip, ...)`, `Trig(flip, idle, ...)`…).

Rồi bấm **`3. Xoá và làm lại`**. Rect dùng chung vẫn là `84 x 74` (nó vốn đã được tính từ cả 4 hàng)
nên `Idle`/`Stir` **không đổi gì**.

> Mảng `ClipDaBo = { "Flip", "Finish" }` trong `ChefSetupTool.cs` chỉ dùng để **dọn rác** clip cũ.
> Nên gỡ tên vừa bật lại ra khỏi mảng này cho gọn — nhưng **không bắt buộc**: code đã tự bỏ qua
> mọi tên đang có trong `AnimNames`, nên không thể xoá oan clip đang dùng.
