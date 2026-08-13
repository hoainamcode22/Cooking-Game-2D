# TỐI ƯU HỆ THỐNG NHIỆM VỤ & THƯỞNG — HỒ SƠ ĐỘI

Nguồn: video `Đang ghi 2026-08-11 223056.mp4` (31,4s · 940 frame · 30fps) + 3 ảnh mẫu thiết kế.

---

## 0 · ĐO TỪ VIDEO TRƯỚC KHI SỬA BẤT CỨ THỨ GÌ

Tách 940 frame về mức xám 160×70, đếm tỉ lệ điểm ảnh đổi giữa hai frame liên tiếp.
Frame nào đổi dưới 0,4% coi là **đứng hình**.

```
Tổng thời gian đứng hình: 24,87s / 31,3s

Các đợt giật khi thao tác (giây 11,6 → 19,8):
   0,37s   giây 14,87
   0,27s   giây 13,90
   0,23s   giây 11,87
   0,20s   ×6 lần
   0,17s   ×2 lần
   0,13s   ×2 lần
```

12 lần khựng trong 8 giây — đúng đoạn người chơi bấm **Nhận** và cuộn danh sách.
Các đoạn đứng 4–7s còn lại là màn hình tĩnh lúc đọc, không tính.

**Trả lời câu hỏi "do nhiều dữ liệu mà không có cơ sở dữ liệu?":** Không.
307 dòng dữ liệu tra bằng `Dictionary` mất vài micro giây. Vấn đề nằm ở chỗ khác.

---

## 1 · BỐN NGUYÊN NHÂN

| # | Nguyên nhân | Con số |
|---|---|---|
| 1 | `ClaimMission` gọi `ShowTab()` → dựng lại cả danh sách | huỷ 5.833 + tạo 5.833 GameObject |
| 2 | `VerticalLayoutGroup` ôm 307 con, tính lại toàn bộ mỗi lần dirty | 307 con/lượt |
| 3 | `PlayerPrefs.Save()` chạy 5 lần cho một cú bấm Nhận | ~150ms chặn luồng chính |
| 4 | `IsMissionClaimed` đọc `PlayerPrefs.GetInt` 2 lần/nhiệm vụ | 614 lệnh gọi native/lượt dựng |

Chi tiết #1 — một hàng nhiệm vụ gồm 19 GameObject:

```
 1  Mission_Row          1  Txt_Title (TMP)
 1  IconFrame            4  ProgressBar (root+mask+fill+TMP)
 1  Img_Icon             9  RewardSlot ×3 (nền+icon+TMP)
                         2  Button (nền+TMP)
```

6 trong số đó là `TextMeshProUGUI` — mỗi cái khi sinh ra phải tra font atlas và dựng mesh riêng.

Chi tiết #3 — dấu vết một lần bấm Nhận:

```
PlayerPrefs.SetInt(claimed) + Save()   → flush đĩa
AddGold  → SaveCurrency()   + Save()   → flush đĩa
AddGems  → SaveCurrency()   + Save()   → flush đĩa
AddExp   → Save()                      → flush đĩa
AddAchievementCount → Save()           → flush đĩa
```

Trên Windows `PlayerPrefs.Save()` ghi vào registry rồi flush xuống đĩa, **đồng bộ**.
Toàn dự án có **45 chỗ** gọi nó trong code runtime — kể cả mỗi lần thu hoạch một ô lúa.

---

## 2 · PHÁT HIỆN NGOÀI DỰ KIẾN TRONG LÚC LÀM

### 2.1 · 157 thành tựu thật ra chỉ là 7 chuỗi

```
eventType  item              số bậc   các mốc
        0  (mọi thứ)             33   100 → 150 → 300 → 450 → 500 → 600 → …
        1  (mọi thứ)             32   40 → 50 → 80 → 120 → 160 → 200 → …
        2  (mọi thứ)             32   25 → 30 → 50 → 75 → 100 → 125 → …
        7  (mọi thứ)             29   2 → 3 → 4 → 5 → 6 → 7 → …
        0  rice                  15   100 → 200 → 300 → 400 → 500 → …
        2  bo_ham_ca_rot         15   15 → 30 → 45 → 60 → 75 → …
        4  (mọi thứ)              1   50
```

Đổ cả 157 bậc ra danh sách là bắt người chơi cuộn qua 33 dòng
"thu hoạch 100 / 150 / 300 / 450 …" xếp liền nhau — không biết nhìn dòng nào.
Ảnh mẫu 3 chỉ có 5 dòng, mỗi dòng một chuỗi. **Sửa: mỗi chuỗi một dòng, hiện đúng bậc đang làm.**

### 2.2 · Chia trang theo cấp không dùng được cho tab Thành tựu

Cả 157 mục đều `requiredLevel: 1`. Chia theo mốc cấp thì 157 dòng vẫn rơi hết vào một trang.
Nên tab Nhiệm vụ chia theo cấp (đúng yêu cầu), tab Thành tựu gộp theo chuỗi.

### 2.3 · Nút "Đi" không gắn hành động nào

`action.interactable = canClaim` nên nút "Đi" bật sáng xanh nhưng bấm không phản ứng.
Đã cho nó đóng popup — người chơi cần ra ruộng làm việc đó.

### 2.4 · Ô thưởng ở thanh "Phần thưởng mốc" tràn ra ngoài nền

Ô cuối đặt tại `x=460`, rộng 92 ⇒ mép phải 506, trong khi nửa chiều rộng thanh là 402.
Tràn 104px. Đã dời về `x=344`, rộng 86 ⇒ mép phải 387.

---

## 3 · CHIA VIỆC

### DEV-A — hiệu năng

| Việc | Trạng thái |
|---|---|
| Dựng `LuuGopPrefs` — gộp ghi đĩa, flush khi pause/thoát/rời Play | xong |
| Đổi 45 chỗ `PlayerPrefs.Save()` sang lưu gộp | xong |
| Cache cờ đã-nhận vào `Dictionary` | xong |
| Chia trang theo mốc cấp 1–4 · 5–9 · 10–14 · 15–19 · 20–24 · 25+ | xong |
| Tự nhảy sang mốc sau khi làm hết mốc hiện tại | xong |
| Tái dùng hàng — đổi trang không Instantiate/Destroy | xong |
| Bấm Nhận chỉ nạp lại đúng một hàng | xong |
| Gộp 157 thành tựu thành 7 chuỗi | xong |

### DEV-B — giao diện

| Việc | Trạng thái |
|---|---|
| Bố cục 4 cột cố định theo hằng số `X_*`, không đè nhau | xong |
| Icon ô thưởng xếp DỌC (icon trên, số dưới) — số 3 chữ số không bị icon lấn | xong |
| Bốn trạng thái nút: Khoá · Đi/Đang làm · Nhận · Đã nhận | xong |
| Chấm đỏ ở nút Nhận | xong |
| "Phần thưởng mốc" thành chân trang cố định, không nằm trong danh sách cuộn | xong |
| Thưởng bay phóng to 1,4 → **3,0** rồi mới thu về ví | xong |
| Ô vàng/kim cương/EXP trên HUD nảy khi thưởng chạm tới | xong |
| Nhịp phồng nhẹ ở hàng vừa nhận | xong |

### TESTER — kiểm chứng

| Việc | Trạng thái |
|---|---|
| Đếm frame đứng trong video gốc | xong |
| Cân ngoặc 7 file đã sửa | xong |
| Tính hình chữ nhật từng phần tử, dò chồng lấn bằng số học | xong |
| Rà hàm mồ côi sau khi thay code | xong |
| Đếm lại GameObject và số lần ghi đĩa | xong |

---

## 4 · BÁO CÁO HIỆU NĂNG

### 4.1 · Số GameObject phải dựng

| | Trước | Sau |
|---|---|---|
| Tab Nhiệm vụ | 307 hàng · **5.833 object** | trang nặng nhất 84 hàng · **1.596 object** |
| Trang đầu (người chơi mới, cấp 1–4) | 5.833 object | 28 hàng · **532 object** |
| Tab Thành tựu | 157 hàng · **2.983 object** | 7 chuỗi · **133 object** |
| Bấm **Nhận** | huỷ 5.833 + dựng 5.833 = **11.666 thao tác** | **0** — gán lại text/màu 1 hàng |
| Đổi trang | — | **0** — nạp lại nội dung hàng có sẵn |

Tab Thành tựu giảm **22 lần**. Trang đầu của tab Nhiệm vụ giảm **11 lần**.

### 4.2 · Ghi đĩa

| | Trước | Sau |
|---|---|---|
| Một lần bấm Nhận | 5 lần flush ≈ **150ms chặn luồng chính** | 5 lần đánh dấu (0ms), gộp 1 flush sau 2s |
| Thu hoạch một ô lúa | 1 flush ≈ 30ms | 0ms tại chỗ |
| Bán một món hàng | 1 flush ≈ 30ms | 0ms tại chỗ |

Không mất dữ liệu: flush ngay ở `OnApplicationPause`, `OnApplicationFocus(false)`,
`OnApplicationQuit`, và `OnDestroy` (rời Play Mode trong Editor).

### 4.3 · Đọc PlayerPrefs khi dựng danh sách

| Trước | Sau |
|---|---|
| 614 lệnh `GetInt` | 84 lần lượt đầu, sau đó 0 — tra `Dictionary` |

### 4.4 · Ước tính thời gian mở popup

Suy ra từ số object, chưa đo trên máy thật:

| | Trước | Sau |
|---|---|---|
| Mở popup lần đầu | ~5.800 object + 1.800 TMP | ~530 object + 170 TMP |
| Bấm Nhận | ~0,30s khựng (đo được từ video) | không dựng lại gì |

> **Con số dưới đây cần bạn xác nhận trên máy thật.** Tôi không chạy được Unity nên
> chỉ suy từ khối lượng công việc, không phải đo đồng hồ. Cách kiểm: xem mục 6.

---

## 5 · KIỂM TRA CHỒNG LẤN — TÍNH BẰNG SỐ, KHÔNG NHÌN MẮT

Đọc thẳng hằng số `X_*` trong file rồi tính lại các hình chữ nhật:

```
── HÀNG NHIỆM VỤ (ngang) ── nửa chiều rộng 402
   icon tròn        [-372,0 .. -300,0]   cách cột chữ      3,0px
   cột chữ          [-297,0 ..  -47,0]   cách ô thưởng 1  23,0px
   ô thưởng 1       [ -24,0 ..   68,0]   cách ô thưởng 2   6,0px
   ô thưởng 2       [  74,0 ..  166,0]   cách ô thưởng 3   6,0px
   ô thưởng 3       [ 172,0 ..  264,0]   cách nút          7,0px
   nút              [ 271,0 ..  393,0]   cách mép hàng     9,0px

── HÀNG NHIỆM VỤ (dọc) ── nửa chiều cao 48
   thanh tiến độ    [ -29,3 ..   -3,3]   cách tên          4,6px
   tên nhiệm vụ     [   1,2 ..   35,2]   cách mép trên    12,8px

── PANEL 860×540 (dọc) ── nửa chiều cao 270
   chân mốc         [-264,0 .. -160,0]   cách vùng cuộn   10,0px
   vùng cuộn        [-150,0 ..  186,0]   cách thanh trang  4,0px
   thanh trang      [ 190,0 ..  230,0]   cách tiêu đề      1,0px
   tiêu đề          [ 231,0 ..  269,0]   cách mép trên     1,0px

KẾT QUẢ: KHÔNG CÓ CHỒNG LẤN
```

Mọi mốc `x` nằm ở một chỗ duy nhất đầu file (`X_IconTron`, `X_CotChu`, `X_OThuong0`,
`B_OThuong`, `X_Nut`…). Bản cũ rải số toạ độ khắp `BuildMissionRow` nên chỉnh một chỗ
là lệch chỗ khác — đó là gốc của hiện tượng chữ đè lên ô thưởng.

---

## 6 · CÁCH BẠN TỰ ĐO LẠI

1. `Tools ▸ Farm ▸ ⚠ CHƠI LẠI TỪ ĐẦU` → Play
2. Mở popup Nhiệm vụ, bấm **Nhận** vài lần
3. Window ▸ Analysis ▸ **Profiler** → tab CPU → cột `GC Alloc` và `Time ms`

Cần thấy:

| Mốc | Kỳ vọng |
|---|---|
| Mở popup | không có đỉnh nhọn quá 1 khung hình |
| Bấm Nhận | `Instantiate`/`Destroy` gần bằng 0 |
| Bấm Nhận | không có `PlayerPrefs.Save` trong Profiler |
| Cuộn danh sách | không có `Canvas.BuildBatch` lặp mỗi frame |

Muốn xem lưu gộp tiết kiệm bao nhiêu: gọi `LuuGopPrefs.ThongKe()` trong Console.
Nó in `N lần yêu cầu lưu → M lần ghi đĩa thật`.

---

## 7 · CÒN LẠI, CHƯA LÀM

| Việc | Vì sao hoãn |
|---|---|
| Tab **Hằng ngày** chỉ có 7 thẻ — chưa đụng bố cục | Không gây lag; sửa sau khi bạn duyệt tab kia |
| Ảnh mẫu có huy hiệu đỏ trên tab bên trái | Cần biết quy tắc: hiện khi có gì chưa nhận? |
| Rương "Phần thưởng mốc" chưa bấm nhận được | Chưa có logic thưởng mốc, mới có thanh tiến độ |
| Font thiếu dấu tiếng Việt (`ầ ắ đ Đ`) | Ảnh hưởng 229/230 TMP toàn dự án, là việc riêng |
