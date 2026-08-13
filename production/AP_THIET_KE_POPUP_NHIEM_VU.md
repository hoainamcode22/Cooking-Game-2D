# ÁP THIẾT KẾ POPUP NHIỆM VỤ VÀO UNITY

Nguồn: `Assets/thietke/anh/UnifiedTaskPopup_Redesign/` — bản chốt **2a "Bảng gỗ nông trại · juicy"**.

---

## 1 · ẢNH: KHÔNG CẦN GIỮ BẢN SAO

Đối chiếu MD5 từng file giữa thư mục thiết kế và `Assets/Assetsgame`:

| Ảnh trong thiết kế | Ảnh gốc trong source | Kết quả |
|---|---|---|
| `btnX.png` | `btnX.png` | trùng byte |
| `btnV.png` | `btnV.png` | trùng byte |
| `AnhBtnNhanQua.png` | `AnhBtnNhanQua.png` | trùng byte |
| `Icon_vang.png` | `Icon_vang.png` | trùng byte |
| `iconmarrket.png` | `iconmarrket.png` | trùng byte |
| `icongiay.png` | `img_icon_giay.png` | trùng byte |
| `iconlich.png` | `icon_lich.png` | trùng byte |
| `kimcuong.png` | `kimcuong-removebg-preview.png` | trùng byte |
| `iconsao.png` | `iconsao-removebg-preview.png` | trùng byte |
| `iconlua.png` | `iconlua-removebg-preview.png` | trùng byte |
| `conga.png` | `conga-removebg-preview.png` | trùng byte |
| `cachua.png` | `cachualever3-removebg-preview.png` | trùng byte |
| `bapcai.png` | `bapcai3-removebg-preview.png` | trùng byte |
| `thitheo.png` | `iconthitheooo-removebg-preview.png` | trùng byte |

**14/14 trùng.** Popup trỏ vào ảnh gốc, thư mục `Assets/thietke` xoá được hoàn toàn — dự án nhẹ đi ~3,7MB.

> Lưu ý một cái bẫy: `cachua.png` khớp `cachualever3`, KHÔNG phải `cachualever2`.
> `bapcai.png` khớp `bapcai3`, không phải `bapcailuc1/2`. Kéo tay rất dễ nhầm, nên tool tra theo đường dẫn đã xác minh.

---

## 2 · SỐ ĐO: RÚT TỪ CSS, KHÔNG ƯỚC LƯỢNG

File mới `Assets/_Game/Scripts/Mission/TaskPopupDesign.cs` giữ **toàn bộ** màu và số đo,
chép nguyên từ thuộc tính `style` inline trong `TaskPopup_standalone.html`.

Trước đây `UnifiedTaskPopupUI.cs` rải hàng trăm `new Color32(...)` thẳng vào từng lời gọi.
Sửa một sắc độ phải mò khắp 2.600 dòng — và sửa sót một chỗ thì hai thành phần cạnh nhau
lệch màu. Giờ **269 chỗ** trong code tham chiếu về `TaskPopupDesign`.

Khung thiết kế **1920×1080** trùng `CanvasScaler.referenceResolution` của game, nên số pixel
trong CSS dùng trực tiếp, không quy đổi.

### Bố cục — đã kiểm bằng số học, không nhìn mắt

```
BẢNG GỖ 1300×850 (dọc, biên ±425)
   giấy            [-383,0 ..  263,0]
   tab đang chọn   [ 263,0 ..  349,0]   ← nối liền giấy, khớp 0,0px

TRONG GIẤY (dọc, biên ±383)
   chân mốc        [-361,0 .. -269,0]
   vùng cuộn       [-255,0 ..  241,0]   ← cách chân mốc 14,0px

HÀNG NHIỆM VỤ 1140×100 (ngang, biên ±570)
   khung icon      [-552,0 .. -476,0]   cách cột chữ     18,0px
   cột chữ         [-458,0 .. -158,0]   cách ô thưởng 1  18,0px
   ô thưởng 1      [-140,0 ..  -28,0]   cách ô thưởng 2  10,0px
   ô thưởng 2      [ -18,0 ..   94,0]   cách nút        302,0px
   nút             [ 396,0 ..  552,0]   cách mép hàng     9,0px

TAB (ngang, biên ±602)   3 tab × 392px, khe 14px
```

Khoảng 302px giữa ô thưởng và nút là **đúng thiết kế** — CSS đặt `flex:1` cho vùng chip nên
nó giãn ra, chip dồn về bên trái.

---

## 3 · ĐÃ ĐỔI NHỮNG GÌ

### Khung bảng

| Thành phần | Trước | Sau (theo thiết kế) |
|---|---|---|
| Ván gỗ | 1180×720, một màu `#88491D` | **1300×850**, gradient `#a9743c → #8a5a2e → #7c4e22`, bo 42, viền 8px `#4a2508` |
| Thớ ván | không có | vạch ngang mỗi **158px**, dày 5px |
| Đinh sắt | không có | **4 góc**, 22×22, 3 lớp cho hiệu ứng lồi |
| Ribbon | 520×105 đỏ | tấm vàng `#ffd257 → #f0a32f` + **2 đuôi đỏ**, chữ 54px viền nâu |
| Tab | **DỌC** ở ray trái, 155×142 | **NGANG** trên đầu, 3 tab × 392×86, chọn thì nổi, thường thì lún 14px |
| Giấy | 890×595 | **1204×646**, viền 4px + vành trong 3px `#f3ddb0` |
| Ray gỗ trái + NPC | có | **bỏ** — thiết kế không có, và nó ăn 220px chiều ngang |

### Hàng nhiệm vụ

| Thành phần | Trước | Sau |
|---|---|---|
| Kích thước | 805×96 | **1140×100**, bo 22, cạnh dưới dày 5px |
| Khung icon | tròn 72 | **vuông bo 20, 76×76, nghiêng −3°**, gradient vàng |
| Cột chữ | 250 | **300** |
| Thanh tiến độ | cao 26, một màu | cao **28**, gradient `#a9e470 → #68bd2b`, **gloss trắng nửa trên**, chữ viền nâu |
| Ô thưởng | 92×58, icon TRÊN số DƯỚI | **112×52, icon TRÁI số PHẢI** (ô rộng hơn nên xếp ngang vừa) |
| Nút | 122×56 | **156×60**, bo 18, cạnh dưới dày 6px |
| Nhãn nút | "Đi" | **"Đi làm"** |
| Hàng khoá / đã nhận | đổi màu từng phần | **làm mờ cả hàng** — 0,55 và 0,68 |

Làm mờ bằng `CanvasGroup` thay vì đổi màu từng thứ: một giá trị điều khiển toàn bộ nên
không thể lệch giữa nền và chữ.

### Bốn trạng thái nút — lấy nguyên mã màu

```
Nhận     #a5e05e → #57a51f   viền #3f8a12   chữ #ffffff   + chấm đỏ
Đi làm   #ffd977 → #f2a636   viền #c07818   chữ #7a4a10
Đã nhận  #ded4bd             viền #c9bd9f   chữ #93876a
Khoá     #cfc7b4             viền #b8ae95   chữ #8d8266   → nhãn "Cấp X"
```

### Chấm đỏ trên tab — đổi nghĩa

Bản cũ dùng chấm đỏ để báo "tab đang chọn" (trùng chức năng với việc tab đổi màu).
Thiết kế dùng nó để báo **"tab này có thứ chưa nhận"**, và ẩn đi khi đang xem tab đó.

---

## 4 · GRADIENT: uGUI KHÔNG CÓ, PHẢI DỰNG BẰNG 2 LỚP

Thiết kế dùng `linear-gradient(180deg, A, B)` ở gần như mọi thành phần. uGUI `Image`
không hỗ trợ. Cách làm:

```
lớp dưới  = màu B, sprite bo góc alpha đặc
lớp trên  = màu A, sprite bo góc alpha GIẢM DẦN từ trên xuống
```

Hai sprite đó sinh bằng code (`BoGoc(r)` và `BoGocGradient(r)`), cache theo bán kính nên
mỗi bán kính chỉ vẽ một lần cho cả phiên.

Cách khác là lấy màu trung bình rồi tô phẳng — nhưng làm vậy popup bẹt hẳn, mất hết chiều
sâu, và đó chính là thứ phân biệt bản thiết kế với bản cũ.

---

## 5 · CHẠY TOOL

**`Tools ▸ Farm ▸ Popup Nhiệm Vụ`**

| Mục | Việc |
|---|---|
| `1 · Kiểm tra ảnh gốc có đủ không` | Chỉ đọc. In bảng 14 ảnh + 4 mảnh art còn thiếu |
| `2 · Gán ảnh vào popup trong scene` | Gán 8 ô Sprite: vàng, kim cương, sao(EXP), rương, nút X, 3 icon tab |
| `3 · Gán icon cho từng nhiệm vụ theo vật phẩm` | Nhiệm vụ về lúa → icon lúa, cà chua → icon cà chua… **Không ghi đè icon đã có** |
| `4 · Dọn thư mục Assets/thietke` | Xoá html, js, md và 14 ảnh sao chép |

**Thứ tự:** 1 → 2 → 3 → Play thử → nếu ổn thì 4.

> Mục 2 cần popup tồn tại trong scene. Popup này **tự dựng lúc chạy** nên có thể chưa có
> object nào — bấm Play một lần cho nó sinh ra rồi chạy tool.

---

## 6 · CÒN 4 MẢNH ART CHƯA CÓ

Code đang tự dựng bằng gradient, chạy được ngay. Vẽ xong thì gán vào là đẹp hơn:

| Ô Sprite | Mảnh | Ghi chú |
|---|---|---|
| `boardFrame` | ván gỗ nền 1300×850 | 9-slice, **xuất rỗng** không có nội dung |
| `ribbon` | ribbon tiêu đề 680×134 | gồm cả 2 đuôi đỏ |
| `tabButton` | nền tab thường 392×86 | bo góc chỉ ở đỉnh |
| `selectedTabButton` | nền tab đang chọn | sáng hơn, nối liền giấy |

Gán vào rồi code **tự bỏ** phần gradient tương ứng — không phải sửa gì.

---

## 7 · KHÔNG ĐỘNG TỚI

Theo yêu cầu, chỉ đổi phần hiển thị. Đã kiểm lại còn nguyên:

| Giữ nguyên | Số chỗ gọi |
|---|---|
| `ClaimMission` / `ClaimAchievement` — nhận thưởng | 3 / 3 |
| `GrantRewards` — cộng vàng/gem/EXP | 4 |
| `MissionProgressTracker` — tiến độ | 6 |
| `MocCap` — chia trang theo mốc cấp | 9 |
| `LocTheoTrang` / `LocThanhTuuTheoChuoi` | 2 / 2 |
| `NapDanhSach` — tái dùng hàng | 3 |
| `GhiCoDaNhan` — cờ đã nhận | 3 |

**Không xoá nhiệm vụ nào.** 307 nhiệm vụ + 157 thành tựu + 10 nhiệm vụ ngày còn nguyên
trong database.

---

## 8 · KIỂM TRA SAU KHI CHẠY

- [ ] Mở popup → ván gỗ có thớ ngang và 4 đinh góc
- [ ] Ribbon vàng cam, chữ **NHIỆM VỤ** trắng viền nâu, 2 đuôi đỏ hai bên
- [ ] 3 tab ngang trên đầu; tab đang chọn **nổi lên nối liền giấy**, hai tab kia lún xuống
- [ ] Đổi tab → chữ ribbon đổi thành **ĐIỂM DANH** / **THÀNH TỰU**
- [ ] Hàng nhiệm vụ: khung icon **nghiêng nhẹ**, thanh tiến độ có **vệt sáng nửa trên**
- [ ] Ô thưởng: icon bên trái, số bên phải, không chữ nào bị cắt
- [ ] Nút "Nhận" xanh **có chấm đỏ**; "Đi làm" cam; "Đã nhận" xám be; khoá ghi **"Cấp X"**
- [ ] Hàng khoá và hàng đã nhận **mờ hẳn** so với hàng đang làm
- [ ] Chân trang: túi vàng **nhô lên** mép thanh, có chỉ may nét đứt
- [ ] Tab nào có thưởng chưa nhận thì **có chấm đỏ**, tab đang xem thì không

Mục nào lệch thì chụp gửi tôi kèm số đo bạn thấy sai.

---

## 9 · VIỆC RIÊNG CHƯA LÀM

Font trong dự án **thiếu dấu tiếng Việt** (`ầ ắ đ Đ`) — ảnh hưởng 229/230 thành phần chữ
toàn game, không riêng popup này. Chữ trong Unity sẽ chưa hiện giống bản thiết kế cho tới
khi thay font. Thiết kế dùng **Baloo 2** (700/800); muốn giống hẳn thì nhập font đó và tạo
lại atlas TMP có đủ bộ dấu.
