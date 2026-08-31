# 🔊 AUDIT ÂM THANH TOÀN GAME — 2026-08-31

> Quét toàn bộ code phát âm thanh (`AudioManager`, `PlayOneShot`) + kho clip có sẵn.
> Kết quả: hệ nền tốt, nhưng **hầu hết tính năng mới từ tháng 8 đều đang CÂM**.

## ✅ ĐÃ CÓ ÂM THANH (không cần đụng)

| Chỗ | Cách phát |
|---|---|
| Nhạc nền + tiếng nước chảy | AudioManager tự chạy |
| TẤT CẢ nút bấm / toggle UI | AudioManager tự bắt click toàn cục (không cần gắn từng nút) |
| Gieo hạt, thu hoạch | PlotController |
| Cho thú ăn, thu sản phẩm chuồng | PenMiniPanelUI |
| Mọi giao dịch vàng (mua/bán/nhận thưởng) | FarmEconomyManager → PlayBuySell |
| Nhận EXP | PlayerProgressManager |
| Bắt đầu nấu + nấu thành công | CookingChallengeManager |
| Tiếng gà/heo/bò kêu ngẫu nhiên | LivestockAI |
| Ăn mừng mở bến tàu | DockUnlockCelebrationFX |

## ❌ CHƯA CÓ ÂM THANH (0 lệnh phát — xếp theo ưu tiên)

**Ưu tiên 1 — người chơi chạm mỗi ngày:**
1. **Tàu Khách Du Lịch** (cả hệ câm): tàu cập/rời bến, khách xuống tàu, bong bóng yêu cầu mở,
   phục vụ thành công (mặt cười bay), popup báo tàu, mua bến mới.
2. **Tàu Hỏa** (cả hệ câm): tàu đến/đi, chất hàng, nhận thưởng, tăng tốc bằng gem.
3. **Bảng Đơn Hàng**: giao đơn thành công (OrderDeliverFxUI có FX hình mà không tiếng), hủy/làm mới đơn.
4. **Popup Nhiệm vụ/Thành tựu/Điểm danh**: bấm "Nhận" chỉ có tiếng vàng gián tiếp — thiếu jingle nhận thưởng; điểm danh (AttendanceManager) câm hoàn toàn.
5. **Level-Up popup**: thiếu fanfare lên cấp (khoảnh khắc sướng nhất game!).

**Ưu tiên 2 — cảm giác "juicy":**
6. CoinFlyFX / GemFlyFX: vàng/gem bay chạm HUD không có tiếng "ting" kết thúc.
7. ~~2 minigame nấu ăn~~ — ĐÃ XOÁ HẲN 31/08 theo lệnh Sếp (xem DON_XOA_MINIGAME_COOKING_2026-08-31.md), bỏ mục này.
8. Máy chế biến (Building/Crop/PenProcessPopup): bắt đầu chế biến, chế biến xong — câm.
9. Đặt/di chuyển công trình (PlacementManager, EditMode): đặt xuống, xoay, hủy — câm.
10. Tutorial: chuyển bước, hoàn thành — câm.

**Ưu tiên 3 — không khí:**
11. Ambience ngày/đêm + mưa/sấm: clip CÓ SẴN trong `Assets/Day_Night/Audio/` nhưng chưa ai phát.

## 📦 CLIP MỒ CÔI CÓ SẴN — gắn được ngay, KHÔNG cần đặt mới

`Close Window.wav`, `Select item.wav`, `Deselect item.wav`, `Timer sound when the crops are ready.wav`,
`Buy _ Sell.wav`, `Picking up crop.wav`, `Planting crop.wav`, `Rain.wav`, `Thunder.wav`,
`Background ambience Day/Night.wav` — tất cả trong `Assets/Day_Night/Audio/` + `Assets/maptitle/.../Audio/`.

## 🎵 CLIP CẦN KIẾM/ĐẶT MỚI (chưa có trong project)

Còi tàu thủy · còi tàu hỏa + xình xịch · jingle nhận thưởng nhiệm vụ · fanfare level-up ·
tiếng "pop" bong bóng khách · "ting" vàng chạm HUD · tiếng đặt công trình (thịch) ·
tick-tock minigame. (Đây là AUDIO — không thuộc phạm vi agent-sprite-forge vẽ ảnh;
Sếp có thể lấy từ kho free như Kenney/freesound, hoặc ra lệnh đội Dev tái dùng clip có sẵn tạm.)

## Cách làm khi Sếp duyệt

Pattern đã có sẵn và rất rẻ: thêm method mới vào `AudioManager` (kiểu `PlayTrainArrive()`)
+ 1 dòng gọi tại đúng sự kiện — thuần cộng thêm, không đụng logic, mỗi hệ ~15 phút.
Đề xuất làm theo lô: Lô A (mục 1-5), Lô B (6-10), Lô C (ambience 11).
