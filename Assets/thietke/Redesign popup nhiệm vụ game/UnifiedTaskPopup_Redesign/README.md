# Redesign Popup Nhiệm Vụ (UnifiedTaskPopupUI) — bản chốt 2a "Bảng gỗ nông trại · juicy"

Mock tương tác: mở `TaskPopup_standalone.html` (chạy offline, 1 file) hoặc `TaskPopup.dc.html` trong tool design.
Khung thiết kế: 1920×1080 (CanvasScaler hiện tại của game), popup 1300×850.

## Cấu trúc (map vào UnifiedTaskPopupUI.cs)
- **Board_WoodFrame** → ván gỗ 1300×850, bo 42px, viền 8px `#4A2508`, gradient `#A9743C → #7C4E22`,
  thớ ván ngang mỗi 158px, 4 đinh sắt góc, bevel sáng trên / tối dưới.
- **Ribbon_Title** → plate vàng `#FFD257 → #F0A32F`, viền 5px `#A35C14`, 2 đuôi ribbon đỏ `#D8641F → #A84812`
  (clip-path notch). Chữ trắng `#FFFBE9` outline nâu `#96540F`. Đổi text theo tab: NHIỆM VỤ / ĐIỂM DANH / THÀNH TỰU.
- **Tabs** (ngang, trên giấy): active nối liền giấy (`#FFFBE9 → #FDF0D3`, margin-top 0),
  inactive lún xuống 14px (`#E2A75F → #C48538`), viền 4px `#6E4014`, icon trong đĩa tròn trắng mờ, chấm đỏ khi có thưởng.
- **PaperPanel** → giấy kem `#FDF3DA → #FBECCB`, viền 4px `#6E4014` + viền trong `#F3DDB0`, inner shadow trên.
- **Mission_Row** → nền `#FFFDF4 → #FDF6E3`, viền 3px `#ECD09C`, bo 22px, đổ cạnh `0 5px 0 rgba(190,140,70,.35)`.
  Cột: icon 76px (khung vàng gloss, xoay −3°) · tên + progress (300px) · chip thưởng · nút 156×60.
- **Progress bar** → track `#E8D0A4` inset shadow, fill xanh `#A9E470 → #68BD2B`, gloss trắng nửa trên, chữ trắng outline nâu.
- **Nút 4 trạng thái** (3D, cạnh dưới `0 6px 0 rgba(0,0,0,.28)`, bấm lún translateY(4px)):
  - Nhận: xanh `#A5E05E → #57A51F`, viền `#3F8A12`, chấm đỏ pulse
  - Đi làm: cam `#FFD977 → #F2A636`, viền `#C07818`, chữ `#7A4A10`
  - Đã nhận: xám be `#DED4BD`, chữ `#93876A`; hàng mờ 68%
  - Khoá (Cấp X): xám `#CFC7B4`; hàng mờ 55%, progress ghi "Mở ở cấp X"
- **MilestoneFooter** → banner vàng `#FFE2A0 → #F5B94E`, viền 4px `#C07D24` + chỉ may dashed bên trong,
  túi vàng 100px nhô lên trên, progress mốc + 2 chip thưởng.
- **Daily (Điểm danh 7 ngày)** → 7 thẻ, band ngày màu nâu `#C98A3F` (hôm nay cam `#E6913C` + glow vàng),
  hôm nay có nút Nhận pulse, ngày qua có tick `btnV.png`, ngày tới mờ 62% + "X ngày nữa". Footer quà tuần.
- **Achievement** → hàng như mission + chip mốc tím `#8A63D2` ("Mốc 3/15"), chỉ hiện mốc đang làm của mỗi chuỗi.

## Type & assets
- Font: Baloo 2 (700/800). Title 54px, tab 26px, tên nhiệm vụ 25px, nút 25px, chip 20px, progress 17px.
- Assets lấy từ source game (Assets/Assetsgame): btnX, btnV, AnhBtnNhanQua, Icon_vang, kimcuong,
  iconsao, iconlua, conga, cachua (cachualever3), bapcai (bapcai3), thitheo, iconmarrket, icongiay (img_icon_giay), iconlich (icon_lich).

## Hành vi giữ nguyên từ code hiện tại
Chia trang theo mốc cấp, row pooling, 4 trạng thái nút, chấm đỏ claimable, "Đi làm" đóng popup để ra farm.
