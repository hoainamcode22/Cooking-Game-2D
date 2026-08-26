# 📦 GÓI TÀI NGUYÊN GIAO DIỆN TÀU HOẢ (TRAIN UI PACKAGE)

Gói tài nguyên được đóng gói độc lập tại thư mục `Assets/Export_Train_UI_Package/` theo đúng chuẩn Unity uGUI (9-Slice Sprite, PNG sắc nét, hỗ trợ co giãn đa kích thước màn hình).

---

## 📂 Danh Sách Tài Nguyên Đã Xuất (`Assets/Export_Train_UI_Package/Sprites/`)

### 1. Khung Viền & Bảng Giấy (9-Slice)
- **`popup_frame_wood.png`**: Khung gỗ nâu sẫm, 4 góc bo tròn gắn 4 đinh đồng nổi 3D (`Border: 36, 36, 36, 36`).
- **`popup_panel_paper.png`**: Tấm giấy da màu kem ấm áp viền chỉ nâu (`Border: 20, 20, 20, 20`).
- **`ribbon_banner_gold.png`**: Dải ruy-băng vàng cam đuôi én 3D cho tiêu đề (`Border: 28, 14, 28, 14`).

### 2. Bộ Nút Bấm 3D (9-Slice)
- **`btn_green_3d.png`**: Nút xanh lá cây tươi nổi 3D (*"THÊM HÀNG"*, *"RA GA NHẬN HÀNG"*).
- **`btn_yellow_3d.png`**: Nút vàng cam nổi 3D (*"NẠP TẤT CẢ"*).
- **`btn_blue_gem_3d.png`**: Nút xanh ngọc nổi 3D (*"TĂNG TỐC · 12 💎"*).
- **`btn_disabled_3d.png`**: Nút xám nhạt nổi 3D (*"ĐÃ ĐỦ HÀNG"*).

### 3. Thanh Tiến Độ, Đĩa Icon & Bong Bóng
- **`progress_track_bar.png`**: Rãnh trượt tiến độ chìm màu gỗ (`Border: 14, 10, 14, 10`).
- **`progress_fill_green.png`**: Thanh lấp đầy tiến độ màu xanh lá tươi.
- **`bubble_cargo_req.png`**: Bong bóng giấy kem viền nâu nhấp nhô trên toa tàu.
- **`icon_disc_large.png`**: Đĩa tròn vàng kem viền đồng để đặt icon nông sản to.
- **`check_badge_green.png`**: Huy hiệu tích xanh khi toa đã nạp đủ hàng.
- **`timer_box_dark.png`**: Khung hộp đồng hồ đếm ngược bo góc sẫm màu.

### 4. Con Tàu Ngang Chuẩn Art Game & Hiệu Ứng Khói
- **`flat_locomotive_horizontal.png`**: Đầu tàu hơi nước đỏ rượu vang (burgundy), viền kim loại đồng/vàng bóng, bánh xe nan hoa, đèn pha vàng, ống khói nhả mây trắng cực đẹp.
- **`flat_wagon_horizontal.png`**: Toa hàng gỗ vân nâu ấm, nẹp góc kim loại đinh tán, bánh xe nan hoa đồng, khoang rỗng mở để chất hàng/bong bóng.
- **`steam_smoke_cloud.png` / `train_smoke_puff.png`**: Đám mây khói hơi nước dạng hạt để làm hiệu ứng ống khói tàu chạy.
- **`station_full_scene_bg.png`**: Tranh nền ga tàu toàn cảnh (Bầu trời, đồi cỏ, đường ray kim loại).
- **`mini_train_track_bg.png`**: Khung đường ray mini cho popup đếm ngược.
- **`train_popup_mini_horizontal.png`**: Đoàn tàu mini nằm trên đường ray nhỏ.

---

## 🛠️ Hướng Dẫn Sử Dụng Trong Unity (Chỉ Cần Kéo Thả)

1. Khi tạo GameObject `Image` trong Canvas, kéo file sprite tương ứng vào ô **Source Image**.
2. Chọn **`Image Type: Sliced`** cho các khung viền gỗ, giấy kem, nút bấm và ruy-băng để viền không bị mờ hay méo khi đổi kích thước.
3. Chỉnh `TextMeshPro - Text (UI)`:
   - Màu chữ tiêu đề: `#FFFFFF` có đổ bóng viền nâu `#5B3417`.
   - Màu chữ thân: `#5B3417` (Nâu đậm phong cách gỗ).
   - Màu số lượng đồng hồ: `#FFD257` (Vàng sáng).
