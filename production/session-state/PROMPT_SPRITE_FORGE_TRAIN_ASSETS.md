# PROMPT GỬI GPT (điều hành agent-sprite-forge) — Assets bổ sung cho hệ Tàu Hỏa
> Logic đã hợp nhất xong ngày 2026-08-26. Các asset UI popup ĐÃ ĐỦ (22 sprite trong
> Assets/Export_Train_UI_Package/Sprites — không vẽ lại). Chỉ cần bổ sung các asset WORLD sau,
> cùng style với package đã giao (nâu gỗ ấm #5B3417, kem #F5E9D0, cam vàng ribbon, viền 3D mềm, dễ thương cho phụ nữ & trẻ em):

1. `world_bubble_train_arrived.png` (~256x256, nền trong suốt)
   Bong bóng thoại world-space nhấp nhô trên nóc ga: hình đầu tàu đỏ mini + dấu chấm than vàng,
   đuôi bong bóng chỉ xuống. Dùng báo "tàu đã về" ngoài map. Cùng họa tiết với bubble_cargo_req.png.

2. `station_building_world.png` (~512x512, nền trong suốt, flat 2D nhìn ngang như các công trình world hiện có)
   Nhà ga hàng: tường kem, mái đỏ tam giác, biển "GA HÀNG" ribbon cam, cửa sổ xanh to, cửa chính nâu —
   giống hệt kiểu ga trong popup (station_building_flat.png) nhưng tỉ lệ/độ chi tiết khớp công trình world
   (tham chiếu các building Assets/Assetsgame/popup/ui_township_exact_bases).

3. (Tuỳ chọn, ưu tiên thấp) `icon_speedup_wing.png` (~128x128) — icon cánh/tia tốc độ nhỏ đặt cạnh chữ
   TĂNG TỐC trên nút xanh ngọc, style bóng 3D cùng bộ nút btn_blue_gem_3d.png.

Yêu cầu kỹ thuật: PNG trong suốt, không chữ nướng cứng vào ảnh (text để game render TMP),
xuất thêm bản @2x nếu pipeline cho phép. KHÔNG sửa file code nào trong Assets/_Game và
Assets/Export_Train_UI_Package/Scripts — logic đã khoá version.
