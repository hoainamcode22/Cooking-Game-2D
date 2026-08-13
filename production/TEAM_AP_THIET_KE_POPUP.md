# ÁP THIẾT KẾ "BẢNG GỖ NÔNG TRẠI · JUICY" VÀO POPUP — HỒ SƠ ĐỘI

Nguồn: `Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/`
(README.md + TaskPopup_standalone.html đã giải mã + 14 ảnh assets).

Nguyên tắc lần này — rút từ lần hỏng trước: **UI đổi, logic không đụng.** Mọi thay thế
bằng khớp chuỗi chính xác, khớp ≠ 1 lần là script DỪNG không ghi. Không còn vụ "tách hàm
bằng đếm ngoặc" nuốt nhầm hàm bên cạnh.

---

## 1 · PHÂN TÍCH BẢN THIẾT KẾ

### Khung (từ HTML giải mã, không phải nhìn ảnh)

| Thành phần | Đặc tả |
|---|---|
| Ván gỗ | 1300×850, bo 42, viền 8px `#4a2508`, gradient `#a9743c→#7c4e22`, thớ ngang mỗi 158px, 4 đinh sắt 22px |
| Ribbon | vùng 680×134 nhô trên đầu; tấm vàng `#ffd257→#f0a32f` viền 5px `#a35c14`; 2 đuôi đỏ `#d8641f→#a84812`; chữ 54px `#fffbe9` viền `#96540f`; đổi chữ theo tab: NHIỆM VỤ / ĐIỂM DANH / THÀNH TỰU |
| Tab | 3 tab ngang ×392×86; đang chọn `#fffbe9→#fdf0d3` nối liền giấy; thường `#e2a75f→#c48538` **lún 14px**; đĩa tròn trắng mờ 54 + icon 38; chấm đỏ khi có thưởng chưa nhận |
| Giấy | `#fdf3da→#fbeccb`, viền 4px `#6e4014`, vành trong `#f3ddb0`, bo chỉ ở đáy |
| Hàng | 1140×100, bo 22, cạnh dưới 5px; khung icon 76 bo 20 **nghiêng −3°**; cột chữ 300; thanh tiến độ 28 gloss; chip thưởng icon-trái-số-phải; nút 156×60 cạnh dưới 6px |
| Nút 4 trạng thái | Nhận `#a5e05e→#57a51f` + chấm đỏ · Đi làm `#ffd977→#f2a636` · Đã nhận `#ded4bd` · Khoá `#cfc7b4` → "Cấp X" |
| Hàng khoá/đã nhận | **mờ cả hàng** 0.55 / 0.68 |
| Chân mốc | banner vàng `#ffe2a0→#f5b94e`, chỉ may nét đứt inset 6px, túi vàng nhô −26px |
| Daily (vmDay) | 7 thẻ; band nâu `#c98a3f`, hôm nay cam `#e6913c` + glow `rgba(255,206,61,.35)` 5px + nút Nhận; đã qua: tick xanh **nhô góc** + chip "Đã nhận" `#61a832`; tương lai: **mờ 62%** + "Ngày mai"/"X ngày nữa" |
| Thành tựu | hàng như nhiệm vụ, **chỉ hiện mốc đang làm của mỗi chuỗi** — khớp cơ chế 7 chuỗi đã có |

### Assets — đối chiếu MD5 với source

14/14 ảnh trong thư mục thiết kế **trùng byte** với ảnh gốc trong `Assets/Assetsgame`
(bảng chi tiết ở `production/AP_THIET_KE_POPUP_NHIEM_VU.md` — vẫn đúng cho bản này,
md5 hai thư mục giống nhau). Popup trỏ ảnh gốc, không cần bản sao.

Bẫy tên: `cachua.png` = `cachualever3` (không phải lever2) · `bapcai.png` = `bapcai3` ·
`thitheo.png` = `iconthitheooo` (không phải `Thịt/thitheo`).

---

## 2 · CHIA VIỆC & KẾT QUẢ

### DEV-A — khung (6 miếng vá, mỗi miếng assert khớp đúng 1)

| Vá | Nội dung |
|---|---|
| A1 | 3 helper sprite: `BoGoc(r)` 9-slice · `DaiGradient()` ảnh 1×64 vẽ Simple · `PhuGradient()` phủ lớp trên. **Không lặp lỗi cũ**: gradient bo góc + Sliced từng làm ván gỗ trắng toát vì Sliced bóp toàn bộ chuyển sắc vào một hàng pixel |
| A2 | Ván gỗ 1300×850 + thớ + 4 đinh 3 lớp; giấy 5 lớp; 3 panel 1204×646; nút X 100px nhô góc (art `btnX` gán qua tool, chưa có art thì hình tròn đỏ + chữ X) |
| A3 | Ribbon vàng + 2 đuôi đỏ + chữ viền nâu, letter-spacing |
| A4-A6 | Tab ngang nổi/lún theo `TabTamY(selected)`; vùng bấm riêng alpha-0 (lớp gradient có pixel alpha thấp, Unity coi là "không trúng" — bật raycast trên nó là nửa dưới tab bấm hụt); chấm đỏ đổi nghĩa thành "có thưởng chưa nhận" qua `CoThuongChoNhan()`; tiêu đề ribbon đổi theo tab |

### DEV-B — nội dung (8 miếng vá)

| Vá | Nội dung |
|---|---|
| B1 | Panel trong giấy: nhãn trang y=283, vùng cuộn 1152×456 @y=33, nút ‹ › ra ±330 |
| B2 | `HangThuong` + 3 field (`nutNenDuoi`, `nutVien`, `doMo`) |
| B3 | `DungHangTrong`/`DungOThuong` bản thiết kế: hàng 1140, icon nghiêng −3°, gloss, chip 134 (đủ chỗ "x1000"), nút 3 lớp + chấm đỏ nhô góc |
| B4 | `NapHang`: mờ cả hàng bằng CanvasGroup — một giá trị, nền và chữ không thể lệch |
| B5 | `NapOThuong`: bỏ tự làm mờ icon (mờ 2 lần = 0.27, không đọc được) |
| B6 | `CapNhatNut`: 4 trạng thái từ `TaskPopupDesign.KieuNut` |
| B7 | `DungChanMoc` + `DungChipTinh`: banner 1140 @−255, chỉ may, túi nhô |
| B8 | Daily theo `vmDay`: 7 thẻ 152×300, band nâu/cam, glow hôm nay, tick nhô, chip trạng thái, mờ 62%, footer quà tuần |

### TESTER — 6 bài, tất cả ĐẠT

| Bài | Kết quả |
|---|---|
| Trùng định nghĩa (CS0102/CS0111) | 0 |
| Truy cập field không tồn tại (CS1061) | 0 |
| Lời gọi không có định nghĩa (CS0103) | 0 (2 cảnh báo là attribute `[Header]`, nhiễu regex) |
| Cân ngoặc | `{`212=212 · `(`1393=1393 |
| Chồng lấn — tính từ token thật trong `TaskPopupDesign.cs` | 0; khe nhỏ nhất 2px (cuộn↕nhãn), hàng: 18/18/10/10/114px |
| Daily xếp vừa | 7×152+6×14 = 1148 ≤ 1152 |

---

## 3 · 307 + 157 + 10 CÓ ĐỦ KHÔNG — CÓ, VÀ ĐÂY LÀ BẰNG CHỨNG

UI chỉ là **khuôn**. Số hàng do database quyết định:

```
_missionDatabase (307)  → LocTheoTrang(...)        → NapDanhSach → DungHangTrong/NapHang
_achievementDatabase(157)→ LocThanhTuuTheoChuoi(...) → NapDanhSach (7 chuỗi, đúng thiết kế
                                                       "chỉ hiện mốc đang làm")
Daily (10 asset)         → GetDailyRewards()        → BuildDailyCard ×7 + footer
```

Không có nhiệm vụ nào gõ cứng trong code — 6 nhiệm vụ trong HTML (`m1`,`m2`…) chỉ là dữ
liệu giả để xem trước, đã kiểm chứng không xuất hiện trong Unity.

Phân trang **giữ nguyên**: 6 mốc cấp 1–4 (28) · 5–9 (39) · 10–14 (47) · 15–19 (51) ·
20–24 (58) · 25+ (84), làm hết mốc tự sang trang. Tái dùng hàng giữ nguyên. Bấm Nhận vẫn
chỉ cập nhật một hàng (`CapNhatMotHang`) — không dựng lại, không lag trở lại.

---

## 4 · GIỐNG MẪU BAO NHIÊU PHẦN TRĂM

| Thành phần | Giống | Ghi chú |
|---|---|---|
| Bố cục & số đo | **100%** | chép từ CSS, kiểm bằng số học |
| Bảng màu | **100%** | token `TaskPopupDesign` = mã hex nguyên văn |
| Trạng thái nút/hàng/thẻ ngày | **100%** | đúng logic `vmRow`/`vmDay` |
| Gradient | **~95%** | 2 lớp thay `linear-gradient`; khác biệt nhìn được duy nhất: gradient dừng ở mép bo góc thay vì phủ tận mép |
| Đinh sắt / thớ ván / chỉ may | **~90%** | vẽ code: đinh 3 lớp tròn, chỉ may là vạch liền mảnh thay nét đứt thật |
| Ribbon 2 đuôi | **~85%** | đuôi là khối chữ nhật bo, chưa có khấc chéo `clip-path` — cần art thật |
| Font | **~60%** | thiết kế dùng Baloo 2; game đang font khác **thiếu dấu tiếng Việt** — việc riêng toàn dự án |
| Animation (pulse chấm đỏ, tab trượt) | **~70%** | lún/nổi tab là tức thời, chấm đỏ chưa pulse — thêm được sau, không chặn |

**Tổng thể: ~90%** phần dựng bằng code. Gắn 4 art thật (`boardFrame`, `ribbon`,
`tabButton`, `selectedTabButton`) qua các ô Sprite có sẵn thì code TỰ nhường — lên ~95%.
5% cuối là font.

---

## 5 · BẠN CẦN LÀM GÌ

1. Chờ Unity biên dịch — Console phải **0 lỗi đỏ**
2. **Play một lần** (popup tự dựng lúc chạy) → `Tools ▸ Farm ▸ Popup Nhiệm Vụ ▸ 2` gán 8 ảnh (vàng, kim cương, sao=EXP, rương, btnX, 3 icon tab) → `3` gán icon nhiệm vụ theo vật phẩm → **Ctrl+S**
3. Mở popup kiểm theo mục 6
4. Ổn rồi → `4 · Dọn thư mục Assets/thietke` (14 ảnh đã trùng byte với gốc)

## 6 · KIỂM MẮT

- [ ] Ván gỗ nâu có thớ ngang + 4 đinh góc; ribbon vàng 2 đuôi đỏ, chữ **NHIỆM VỤ** đổi theo tab
- [ ] 3 tab ngang: tab chọn nổi liền giấy, 2 tab kia lún; tab có thưởng chưa nhận mang chấm đỏ
- [ ] Trang "Cấp 1–4 · 28 nhiệm vụ", bấm ‹ › đổi mốc; **đủ 28 hàng cuộn được**
- [ ] Hàng: icon nghiêng nhẹ, thanh tiến độ có vệt sáng, chip "x50" không cắt chữ
- [ ] Nút: Nhận xanh chấm đỏ / Đi làm cam / Đã nhận xám mờ hàng / "Cấp X" xám mờ hơn
- [ ] Bấm Nhận: thưởng phóng to rồi bay về ví, ô HUD nảy, **chỉ hàng đó đổi**, không khựng
- [ ] Điểm danh: 7 thẻ, hôm nay glow + nút Nhận, ngày qua tick nhô góc, ngày tới mờ + "X ngày nữa"
- [ ] Thành tựu: 7 chuỗi + "N/157 mốc" trên nhãn
