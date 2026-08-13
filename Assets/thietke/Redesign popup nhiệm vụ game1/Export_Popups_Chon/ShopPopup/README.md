# Redesign Popup Cửa Hàng (ShopManager / ShopItemUI) — khung vật phẩm mẫu 3a

Mock tương tác: mở `ShopPopup_standalone.html` (1 file, chạy offline). Khung 1920×1080, popup 1300×880.
Đồng bộ ngôn ngữ với popup Nhiệm vụ (UnifiedTaskPopup_Redesign): ván gỗ + đinh góc, ribbon vàng đuôi đỏ, tab 3D, nút bấm lún.

## Cấu trúc (map vào code)
- **shopPanel** → ván gỗ 1300×880 (viền 8px `#4A2508`, gradient `#A9743C→#7C4E22`, thớ ván, 4 đinh góc),
  ribbon "CỬA HÀNG", nút đóng btnX, 3 tab: Hạt giống / Công trình / Trang trí (active nối liền giấy, inactive lún 14px).
- **searchBar** → ô lõm `#F3E2BB`, viền `#D9B478`, bo 18px + 2 chip số dư Vàng / Kim cương bên phải.
- **itemPrefab (KhungHatGiong)** → thẻ mẫu 3a, lưới 4 cột:
  - Khung gỗ ngoài `#C98F52→#A96F36`, bo 26px, đổ cạnh `0 6px 0 rgba(90,50,15,.35)`, padding 8px.
  - Lõi giấy kem bo 20px; **tên 2 dòng cố định 44px** (không tràn); **icon 84px trên ĐĨA TRÒN kem 112px**
    (radial `#FAF0D6→#F1DFB4`, inset shadow) — theo mẫu nền tròn bạn gửi.
  - Stepper − / số / + (cam / xanh, 36px, hàng cao 38px). Công trình & trang trí: "Mua 1 cái / lần".
  - **Nút giá = nút Mua** cao 54px: Vàng → xanh lá `#A5E05E→#57A51F` viền `#3F8A12`;
    Kim cương → xanh dương `#7CC9F0→#3486C2` viền `#2E6FA3`; không đủ tiền / khoá → xám `#B8AE95`.
  - Khoá cấp: card grayscale + overlay nâu `rgba(62,40,16,.5)` + ổ khoá + "Mở ở cấp X".
- **Toast** "Đã mua xN ..." xanh lá, đáy popup, tự ẩn 1.8s.
- Sắp xếp item theo unlockLevel tăng dần (giữ logic ShopManager). Tìm kiếm lọc theo tên.

## Type & assets
Font Baloo 2 (700/800). Title 54, tab 26, tên item 20, giá 25, stepper 23-24.
Assets từ source game: btnX, Icon_vang, kimcuong, hạt giống (hatgiong/*), khungchuong, chuongheo, chuongbo, coixoaygio, caythong, img_BaoThoc.

## Demo tweaks
`capNguoiChoi` (1–10) đổi cấp người chơi để xem trạng thái khoá.
