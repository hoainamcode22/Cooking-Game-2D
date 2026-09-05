# CHECKLIST VÒNG 17 — Sắp xếp lại tutorial · 2026-09-04

Đã làm **song song 3 nhóm + Lead**, tổng **11 file**. Toàn bộ đã backup ở
`production/backup_round17_2026-09-04/` kèm MD5.

---

## SẾP BẤM THEO ĐÚNG THỨ TỰ NÀY

Mở `Assets/_Game/Scenes/SCN_Farm.unity`, đợi Unity biên dịch xong (không lỗi đỏ), rồi:

### Bước 1 — Xem trước, chưa sửa gì
```
Tools ▸ Farm ▸ UI       ▸ Kiem tra lop UI (chi bao cao)
Tools ▸ Farm ▸ Tutorial ▸ Kiem tra tutorial (chi bao cao)
```
Kỳ vọng: bảng liệt kê 10 Canvas + cảnh báo **4 nhóm trùng order**, và tutorial báo
**21/31 bước** (10 bước L2 chưa nối lại).

### Bước 2 — Nối lại 10 bước L2 (việc còn nợ từ vòng 16)
```
Tools ▸ Farm ▸ Tutorial ▸ Khoi phuc 10 buoc L2 - DRY RUN (chi bao cao)
Tools ▸ Farm ▸ Tutorial ▸ Khoi phuc 10 buoc L2 - APPLY (ghi vao scene)
```

### Bước 3 — Sắp xếp lại lớp UI
```
Tools ▸ Farm ▸ UI ▸ Sap xep lai lop UI - DRY RUN
Tools ▸ Farm ▸ UI ▸ Sap xep lai lop UI - APPLY
```
Tool tự kiểm và in dòng xác nhận `Tutorial_Canvas (250) < Canvas_Popup (300)`.

### Bước 4 — Nối lại 4 nhân vật popup Lên Cấp (việc vòng 16B)
```
Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nối lại dây popup (DRY-RUN)
Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nối lại dây popup (APPLY)
```

### Bước 5 — **Ctrl + S**
Không tool nào tự lưu scene. Chưa Ctrl+S là chưa ăn thua.

### Bước 6 — Chạy lại 2 tool kiểm ở Bước 1
Kỳ vọng: **0 cặp trùng order**, tutorial báo **31/31 bước, SẠCH**.

---

## BẢNG LỚP UI MỚI (`UILayers.cs` — một nơi duy nhất)

| Lớp | order | Canvas |
|---|---|---|
| World | 0 | `World` (đang tắt) — trả lại mức 100 cho HUD |
| HUD | 100 | `Canvas_HUD` |
| Panel | 200 | `popup_Menu` |
| Panel +10 | 210 | `WarehousePopup` |
| Panel +20 | 220 | `Canvas_MarketPopup` |
| Panel +30 | 230 | **Shop** (runtime, `ShopManager`) |
| **Tutorial** | **250** | **`Tutorial_Canvas` ← từ 999 xuống** |
| Popup | 300 | `Canvas_Popup` |
| Popup +10 | 310 | `Popup_LevelUp_Township` |
| PopupCaoCap | 400 | `Canvas_TouristBoatPopup` |
| PopupCaoCap +10 | 410 | `MillPopup_Root` |
| ChuyenCanh | 9999 | màn chuyển cảnh |

**Bất biến phải giữ:** `HUD < Panel < Tutorial < Popup`.
Tutorial **trên** HUD/Panel (highlight vẫn hiện) nhưng **dưới** popup hệ thống
(popup luôn cắt ngang được tutorial).

Shop = 230 chứ không phải 150 như cũ: sau khi nhóm Panel lên 200–220, số 150 sẽ khiến
**mở shop bị kho đè**. 230 giữ shop trên mọi panel nhưng vẫn dưới Tutorial 250 — nên
bước L2 (mua Ngô) lớp phủ hướng dẫn vẫn vẽ đè lên shop được, đúng như hành vi cũ.

---

## CÔNG CỤ TEST NHANH — đỡ phải chơi lại từ đầu

Vào Play Mode, bấm **F9** → hiện bảng nhỏ góc màn hình:
- Bước hiện tại `[i/n] tên bước`
- Ô nhập số + nút **Nhảy tới**
- Nút `<<` / `>>` lùi/tiến 1 bước
- Nút **In danh sách bước** ra Console

Test 1 bước mất ~10 giây thay vì chơi lại 20 phút.
(Bảng chỉ có trong Editor và development build, không lọt vào bản phát hành.)

---

## NGHIỆM THU G1 — 5 tiêu chí phải đạt

1. Lên cấp 2 giữa lúc đang thu hoạch → **UI tutorial tự ẩn**, popup hiện rõ, bấm Nhận xong tutorial mới tiếp.
2. Lặp 5 lần liên tiếp, **không lần nào đè**.
3. Mở Kho / Shop / Chợ giữa tutorial → card thoại tutorial **không** đè lên.
4. Popup thông báo tàu tự bật giữa tutorial → tutorial **dừng chờ**.
5. Console không còn `Coroutine couldn't be started ... is inactive`.

Trong Console sẽ thấy log mới của cổng:
```
[TutorialGate] Tạm dừng — popup 'LevelUp' đang mở.
[TutorialGate] Popup đã đóng — chạy tiếp sau 4.2s.
[Tutorial] Hoãn 'WaitForAllPlotsHarvested' — popup 'LevelUp' đang mở.
```

---

## 11 FILE ĐÃ ĐỤNG

| File | Việc |
|---|---|
| `Tutorial/TutorialGate.cs` **(mới)** | Cổng popup — `CoPopupDangMo` · `TenPopupDangMo` · `ChoPopupDongHet` |
| `Managers/PopupManager.cs` | Thêm `LevelUpPopupUI` + `BoatAnnouncePopupUI` (2 popup vốn lọt lưới) + `TenPopupDangMo()` |
| `TouristBoat/UI/BoatAnnouncePopupUI.cs` | Thêm `public static bool IsActive` (popup này tự bật theo đồng hồ, trước giờ không ai biết) |
| `Tutorial/TutorialManager.cs` | Cổng ở đầu `PlayStep` (áp cho **mọi** bước) · cổng trong `NotifyAction` · **xoá race `expectLevelUp`** · hàng đợi action 1→8 phần tử · 5 API cho công cụ test |
| `Scripts/UI/UILayers.cs` **(mới)** | Bảng lớp UI tập trung |
| `Editor/UILayerAuditTool.cs` **(mới)** | Quét Canvas, báo trùng order |
| `Editor/UILayerApplyTool.cs` **(mới)** | DRY-RUN / APPLY áp bảng lớp vào scene |
| `Editor/TutorialAuditTool.cs` **(mới)** | Quét `_steps`: NULL · trùng · thiếu · targetID lạ · kéo từ hư không |
| `Tutorial/TutorialDebugJump.cs` **(mới)** | Bảng F9 nhảy bước |
| `Shop/ShopManager.cs` | Trả lại sortingOrder khi đóng shop (trước giờ **không bao giờ trả**) + đổi 150 → 230 |
| `Editor/FixMarketLayerTool.cs` | Bỏ hardcode 122, lấy số từ `UILayers` (nếu không sẽ phá bảng mới) |

---

## NẾU SAI

`Ctrl + Z` trong Unity, hoặc khôi phục từ `production/backup_round17_2026-09-04/`
(có `SCN_Farm.unity`, `TutorialManager.cs`, `PopupManager.cs`, `ShopManager.cs`,
`FixMarketLayerTool.cs` + `_CHECKSUM.txt`).

---

## CHƯA LÀM (giai đoạn 3, để sau)

Refactor `PlayStep()` — hiện **374 dòng** với ~15 nhánh `if (step.name == ...)` hardcode.
Đây là phần rủi ro cao nhất, chỉ nên động vào khi G1/G2 đã chạy ổn định vài ngày và
đã có 2 tool kiểm ở trên đỡ lưng.
