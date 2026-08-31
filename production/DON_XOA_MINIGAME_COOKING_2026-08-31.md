# 🧹 DỌN XOÁ MINIGAME COOKING — 2026-08-31

> Thực thi nốt quyết định Sếp 27/08: "GỠ minigame khỏi luồng nấu — bấm NẤU là nấu thẳng".
> Code luồng nấu đã ngắt từ 27/08, hôm nay xoá hẳn phần xác còn sót.

## Đã làm (tự động, có backup)

1. **Xoá 2 script** `LetterMiniGame.cs` + `CookingTimingMiniGameUI.cs`
   (cả folder `Assets/_Game/Scripts/minigameCooking/` — folder chỉ chứa đúng 2 file này).
2. **Gỡ tham chiếu cuối cùng** trong `KitchenSceneV2UI.RaiseLegacyOverlays()` (2 dòng) —
   giữ nguyên phần nâng sorting cho `CookingPopupController` (vẫn đang dùng).
3. Đã rà TOÀN BỘ code + prefab: không còn chỗ nào tham chiếu 2 class này → compile sẽ sạch.

Backup đầy đủ (muốn hồi thì copy ngược lại `Assets/_Game/Scripts/`):
`production/backup_xoa_minigame_2026-08-31/` (2 script + meta + KitchenSceneV2UI bản trước khi sửa).

## CẦN SẾP (2 phút, trong Unity)

Scene **SampleScene** còn 2 GameObject rỗng của minigame — sau khi Unity compile chúng sẽ hiện
cảnh báo "Missing Script" (vô hại nhưng rác). Sếp xoá tay giúp (agent không tự xoá object scene theo luật):

1. Mở `Assets/_Game/Scenes/SampleScene.unity`
2. Trong Hierarchy tìm và XOÁ 2 object: **`LetterMiniGameManager`** và **`CookingMiniGameManager`**
3. **Ctrl+S** lưu scene · Console 0 lỗi đỏ là xong.

## Ghi chú kỹ thuật

- `CookingChallengeManager.OnCookingMiniGameFinished()` giờ là method chết (không ai gọi, không
  tham chiếu class nào) — vô hại, để lại chờ đợt refactor bếp K2 dọn cùng thể, không đụng manager đang chạy.
- Bảng audit âm thanh hôm nay: bỏ mục 7 (âm thanh 2 minigame) — không còn tồn tại.
