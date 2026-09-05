# 📋 BÁO CÁO UI PASS 3 — 2026-09-05 (tối muộn) · Tech Lead

> Vòng này trả lời 4 câu của Sếp: **(1)** vì sao logic cũ chạy tới chăn nuôi mà giờ đứng · **(2)** hai bàn tay · **(3)** tutorial "ông già" chưa ẩn · **(4)** đưa tutorial ra thành canvas chỉnh tay được.
> Backup: `production/backup_ui_pass3_2026-09-05/` (4 file .cs + **bản sao SCN_Farm.unity**) kèm `MD5SUMS.txt`.
> 7 file đã ghi về máy (5 sửa + 2 tool mới), MD5 khớp 100%. Chưa commit git. Chưa compile trong Unity.

---

## 1. Chẩn đoán — có bằng chứng, không đoán

### 🔴 (1) Vì sao mất phần chăn nuôi gia súc
Trong `SCN_Farm.unity`, danh sách `_steps` của `Tutorial_Manager` **chỉ còn 21 bước**. Toàn bộ **10 bước L2** — `L2_01_GotoShop → L2_02_UnlockCorn → L2_03_BuyCorn → L2_04_CloseShop → L2_05_PlantCorn → L2_06_AnimalIntro → L2_07_FocusPen → L2_08_FeedPen → L2_09_PenSpeedUp → L2_10_HarvestPen` — **không còn trong danh sách** (file asset vẫn nằm nguyên trong `Assets/Resources/TutorialSteps/L1_L2/`, chỉ là không được nối vào scene).
⇒ Tutorial hiện **kết thúc ở bước 18 (Lên cấp)** rồi dừng. Đây chính là "logic cũ chạy tới chăn nuôi, giờ không". Việc "Khôi phục 10 bước L2" của Vòng 17 (09-04) **chưa từng được bấm APPLY**.
**Đã làm:** tool mới `Khoi phuc DU 31 buoc` nạp lại đủ 31 bước theo đúng thứ tự kịch bản, ghi qua `SerializedObject` + Undo.

**Quyết định của Lead — bỏ `L1L2_04b_FirstHarvest`:** asset này nội dung là *"Chạm vào ô lúa chín để thu hoạch nào!"* (chờ `WaitForHarvest`) nhưng vị trí lại **ngay sau `04_FocusPlots`, tức TRƯỚC khi người chơi gieo hạt** → không ô nào chín → kẹt cứng tại bước 5. Bản git cũ (chạy được tới chăn nuôi) cũng đúng 31 bước không có nó. Muốn dùng lại: sửa nội dung asset rồi chèn sau `08b_GuideHarvest`.

### 🔴 (2) Hai bàn tay
Ba script cầm **ba object tay khác nhau**, không ai biết ai đang bật:
`TutorialManager._handPointer` → `Hand_Click_Plot` · `TutorialDragHintAnimator._hand` → `Hand_Drag_Seed` · `TutorialActionHandGuide._hand` → `Hand_Action_Plot_Diamond_Sickle`.
Đúng như ảnh Sếp chụp: 1 tay ở ô đất + 1 tay ở ô "Lúa". Thêm nữa, `Tutorial_Hands` đang **lồng 3 tầng** và có **3 bản `Hand_Drag_Seed`** (2 bản thừa không script nào dùng).
**Đã làm:** `TutorialHandBus` (trọng tài) — **chỉ 1 bàn tay tại 1 thời điểm**, và **không bao giờ 0 tay** ở bước cần tay. Tay ảo ảnh (phantom) cũng được tính vào trọng tài.

### 🔴 (3) "Tutorial ông già"
Là `NPC_Dialog_Popup` — **hệ hộp thoại CŨ** (trước card V2), vẫn `active = 1` trong scene, nội dung *"Cháu đến rồi à! Bắt tay vào vi…"*. Code còn **2 chỗ bật lại nó**: nhánh `else` khi không dùng card V2, và `SetTutorialUIVisible(true)` — nên nó nhảy ra ở đầu game.
**Đã làm:** code **khai tử vĩnh viễn** (Awake luôn tắt · không còn dòng `SetActive(true)` nào · mọi truy cập null-safe) + tool **xoá hẳn object khỏi scene** như Sếp duyệt.

### 🟠 (4) Chỗ "đứng im sau khi trồng lúa"
Chưa đủ bằng chứng để kết luận (không đọc được Console từ đây), nên tôi **gắn mắt cho vòng sau**: nhấn **F10** giờ in thêm mục `── TRẠNG THÁI TUTORIAL ──` vào `capture_*_report.txt`: bước hiện tại + waitAction · trạng thái + hàng đợi action · cổng popup (nghi phạm số 1) · **tay nào đang bật** (cảnh báo nếu >1) · số ô lúa/hoa trống-đang lớn-chín + tên ô còn việc · kho hạt · PlayerPrefs. Sếp bấm F10 **đúng lúc nó đứng**, tôi đọc là ra ngay.

---

## 2. Đã làm gì (7 file)

| File | Nội dung |
|---|---|
| `Scripts/Tutorial/TutorialHandBus.cs` **(MỚI)** | Trọng tài bàn tay: `LoaiTay {TayTinh, TayKeo, TayHanhDong, TayAoAnh}` · `Nhan/Nha/NhaTatCa` · reset khi tắt Domain Reload |
| `Scripts/Tutorial/TutorialManager.cs` | Trọng tài (`AnTayTinh`, `CapNhatLaiTayTinh`, `DonSachMoiBanTay`, chống dội `_dangDonTay`) · khai tử hộp thoại ông già · **13 thuộc tính công khai** cho F10 · **vá bước 04 không bị mất tay** |
| `Scripts/Tutorial/TutorialDragHintAnimator.cs` | Giành quyền `TayKeo` → tự tắt tay tĩnh; nhả quyền → gọi lại tay tĩnh |
| `Scripts/Tutorial/TutorialActionHandGuide.cs` | Giành/nhả quyền `TayHanhDong` qua **một cửa** `StartGuide/StopGuide`; vá rò quyền khi `_hand` null (nếu không sẽ mất tay vĩnh viễn) |
| `Scripts/Debug/PopupCaptureReporter.cs` | F10 in thêm mục TRẠNG THÁI TUTORIAL (bọc try/catch riêng — không bao giờ làm hỏng báo cáo cũ) |
| `Editor/TutorialStudioTool.cs` **(MỚI, 908 dòng)** | 5 menu: báo cáo cây · dọn cây + xoá ông già · **BẬT HẾT để chỉnh tay** · **TRẢ VỀ trạng thái chạy** · kiểm tra lại |
| `Editor/TutorialStepsFullRestoreTool.cs` **(MỚI)** | Khôi phục đủ **31 bước** (có lại phần chăn nuôi), DRY RUN → APPLY |

**An toàn đã cài trong tool dọn cây:** trước khi xoá bất kỳ object nào, tool quét **toàn scene** xem còn script nào trỏ tới không (kể cả phần tử mảng) — còn thì **không xoá**, chỉ log. Khi dời tay, đọc trước/gán lại `anchoredPosition/sizeDelta/scale`. Mọi thao tác gom **1 nhóm Undo** (Ctrl+Z một phát về như cũ). Không tự lưu scene.

---

## 3. 🧑 CẦN BẠN — bấm đúng thứ tự

**Bước 0 — Compile.** Mở Unity, chờ biên dịch xong. Lỗi đỏ → chụp Console gửi Lead.

**Bước 1 — Dọn cây + xoá ông già**
```
Tools ▸ Farm ▸ Tutorial Studio ▸ 1. Bao cao cay Tutorial (DRY RUN)     ← đọc Console, xem các dấu 🔴
Tools ▸ Farm ▸ Tutorial Studio ▸ 2. Don cay + Xoa hop thoai ong gia (APPLY)
Ctrl + S
```

**Bước 2 — Lấy lại phần chăn nuôi (31 bước)**
```
Tools ▸ Farm ▸ Tutorial ▸ Khoi phuc DU 31 buoc - DRY RUN    ← bảng 31 dòng, xem mục "SẼ THÊM"
Tools ▸ Farm ▸ Tutorial ▸ Khoi phuc DU 31 buoc - APPLY
Ctrl + S
```

**Bước 3 — Chơi lại & test**
```
Tools ▸ Farm ▸ ⚠ CHƠI LẠI TỪ ĐẦU   → Play
```
1. Đầu game: **không còn hộp thoại ông già**, chỉ còn card V2 (NPC cô gái góc phải).
2. Mọi bước: **chỉ thấy 1 bàn tay** — bước 04 chỉ ô đất · bước 05 tay kéo ở khay hạt · bước 07/08 tay ở nút kim cương · bước 09/10 tay liềm.
3. Chạy tiếp sau lên cấp 2 → phải sang **trồng hoa** → rồi **Shop mua ngô** → **chuồng gia súc** (phần chăn nuôi đã trở lại).
4. **Chỗ nào đứng im → bấm F10 ngay tại đó**, rồi báo tôi (tôi đọc `Assets/_Debug_Capture/capture_*_report.txt`, mục TRẠNG THÁI TUTORIAL).

**Bước 4 — Khi muốn tự chỉnh UI tutorial bằng mắt**
```
Tools ▸ Farm ▸ Tutorial Studio ▸ 3. BAT HET de chinh tay (EDIT MODE)   ← hiện hết, kéo-thả trong Scene view
   … chỉnh vị trí/kích thước card, NPC, bàn tay, bảng 4 trang …
Tools ▸ Farm ▸ Tutorial Studio ▸ 4. TRA VE trang thai chay (PLAY MODE) ← BẮT BUỘC bấm trước khi Play
Tools ▸ Farm ▸ Tutorial Studio ▸ 5. Kiem tra sau khi chinh (DRY RUN)
Ctrl + S
```
⚠ Bấm 3 xong mà quên bấm 4 rồi Ctrl+S là scene bị "bật hết" — nhớ 3 → chỉnh → 4 → 5.

---

## 4. Lưu ý & rủi ro còn lại
- Tool dọn cây có thể **từ chối xoá** nếu còn script trỏ tới object — khi đó Console in đúng tên script/field cần gỡ, gỡ xong bấm lại mục 2.
- Ảo ảnh (phantom) hiện ẩn tay thật bằng alpha chứ không tắt object ⇒ F10 có thể báo "nhiều hơn 1 bàn tay" khi ảo ảnh đang chạy — cảnh báo giả, không phải lỗi.
- Trong lúc làm, tôi phát hiện thao tác đồng bộ file của mình suýt làm **lùi bản `TutorialManager.cs`**; đã ghép lại thủ công và kiểm chứng: **11/11 hunk, 260/260 dòng** của cả hai nhánh có mặt, không mất dòng nào của bản V6 (deadlock, bảng ĐÃ RÕ, phantom, resume). Bản backup trước khi ghi vẫn giữ nguyên trong `backup_ui_pass3_2026-09-05/`.
- Vòng này **không cần asset mới** từ đội vẽ.
