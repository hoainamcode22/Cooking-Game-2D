# KẾ HOẠCH SẮP XẾP LẠI TUTORIAL
### 2026-09-04 · dựa trên kết quả quét của 3 nhóm (luồng tutorial · lớp UI · luồng lên cấp)

---

## PHẦN 1 — VÌ SAO XẤU: 3 lỗ hổng kiến trúc, không phải lỗi vặt

Sếp nói đúng: nhiều agent làm mà vẫn đè UI. Lý do là **cả 3 lỗi đều nằm ở chỗ không ai
sở hữu**, nên mỗi lần vá chỉ vá được đúng một ca, ca sau lại vỡ.

### ① Tutorial vẽ ĐÈ LÊN MỌI POPUP — theo thiết kế, không phải lỗi ngẫu nhiên

| Canvas | sortingOrder |
|---|---|
| `Canvas_HUD` | 100 |
| `Canvas_Popup` — chứa **13/17 popup** kể cả Lên Cấp | **300** |
| `Canvas_TouristBoatPopup` · `MillPopup_Root` | 400 |
| **`Tutorial_Canvas`** — dim, card thoại, tay chỉ, guide board | **999** |

`999 > 300` ⇒ **bất kỳ UI tutorial nào đang bật đều vẽ đè và nuốt click trước mọi popup.**
Đây là nguyên nhân trực tiếp của "đè UI", độc lập hoàn toàn với logic chờ.

Bằng chứng đội dev đã từng vật lộn với đúng chuyện này: `UiBlockerProbe.cs:12-14` liệt kê
sẵn `Tutorial_Canvas/Dim_Background`, `Tutorial_Canvas/Tutorial_GuideBoard`,
`Tutorial_Canvas/NPC_Dialog_Popup/NPC_Background` trong danh sách "8 nghi phạm chặn full-screen".

Cách duy nhất code hiện dùng để né: **tự tay `SetActive(false)` UI tutorial trước khi chờ** —
nhưng chỉ làm ở đúng 1 chỗ (mục ② dưới).

### ② Tutorial KHÔNG BIẾT popup nào đang mở — trừ đúng một ca hardcode

Quét toàn bộ `TutorialManager.cs` (1561 dòng): **không có** `IsAnyPopupOpen`, `Pause`,
`_paused`, `blockedBy`, `WaitForPopup`. Không có cơ chế tạm dừng nào.

Chỉ có duy nhất `WaitForLevelUpClaim()` (dòng 1375-1414), và nó **gắn cứng vào một tên bước**:

```
673  if (step.name == "L1L2_11_TransitionFlower")
680      yield return WaitForLevelUpClaim();
```

⇒ Lên cấp 3, 4, 5… ở bất kỳ bước nào khác: **tutorial cứ chạy tiếp trong khi popup đang bật.**

Trong khi đó dự án **đã có sẵn** `PopupManager.IsAnyPopupOpen()`
(`Managers/PopupManager.cs:69-92`) gom trạng thái ~15 popup — **TutorialManager chưa gọi lần nào.**

### ③ Ngay cả ca được xử lý cũng có RACE — đây chính là lỗi Sếp thấy

```
1379  bool expectLevelUp = PlayerProgressManager.Instance == null
1380                    || PlayerProgressManager.Instance.Level < 2;
1383  if (!expectLevelUp) yield break;      // BỎ QUA cả ẩn UI lẫn chờ
```

Điều kiện xét **Level cuối cùng**, không xét "popup đang mở hay chưa bấm Nhận".

Mà EXP **không cộng một lần** — `HarvestFeedbackSpawner.CoSpawnExp()` sinh nhiều "viên EXP bay",
**mỗi viên gọi `AddExp()` riêng khi chạm thanh EXP** (`HarvestFeedbackSpawner.cs:129`).
Nên Level chạm 2 lúc nào là tuỳ tốc độ viên bay.

**Nếu Level kịp lên 2 TRƯỚC khi coroutine chạy tới dòng 1379** → `expectLevelUp = false`
→ `yield break` ngay → **không ẩn UI tutorial, không chờ bấm Nhận** → tutorial chạy tiếp,
card thoại (order 999) đè lên popup Lên Cấp (order 300). **Đúng hiện tượng Sếp gặp.**

### Phụ: còn popup TỰ MỞ mà không ai chờ

`BoatAnnouncePopupUI` tự bật theo **đồng hồ** (`Start()` → `OnNextTripScheduled`, dòng 153 → 417),
không do người chơi bấm, và **không nằm trong** `PopupManager.IsAnyPopupOpen()`.
`LevelUpPopupUI` cũng **không nằm trong** danh sách đó (nó tự khoá input riêng).

---

## PHẦN 2 — KẾ HOẠCH: 4 giai đoạn, xong giai đoạn này mới sang giai đoạn kế

Mỗi giai đoạn có **cổng nghiệm thu** riêng. Không đạt cổng thì không đi tiếp.

---

### GIAI ĐOẠN 1 — CỔNG POPUP (dứt điểm chuyện đè UI)

**Mục tiêu:** tutorial tự nhường sân khấu cho **mọi** popup, không cần biết popup tên gì.

**Việc làm:**

| # | Việc | File |
|---|---|---|
| 1.1 | Thêm `LevelUpPopupUI` và `BoatAnnouncePopupUI` vào `IsAnyPopupOpen()` — hiện đang lọt lưới | `PopupManager.cs` |
| 1.2 | Viết `TutorialGate`: 1 hàm `CoPopupDangMo()` + coroutine `ChoPopupDongHet()` (ẩn UI tutorial → chờ popup đóng → hiện lại) | file mới `TutorialGate.cs` |
| 1.3 | Chèn cổng vào **đầu `PlayStep()`** (dòng 673) — mọi bước đều qua cổng, không riêng bước nào | `TutorialManager.cs` |
| 1.4 | Chèn cổng vào **`NotifyAction()`** (dòng 458): popup đang mở thì **đẩy action vào hàng đợi**, không advance | `TutorialManager.cs` |
| 1.5 | Xoá `expectLevelUp` (dòng 1379-1383) — đổi từ "đoán theo Level" sang "chờ theo trạng thái popup thật" | `TutorialManager.cs` |
| 1.6 | Hàng đợi action: nâng từ **1 phần tử** lên hàng đợi thật (hiện `_queuedAction` bị ghi đè khi có action thứ 2) | `TutorialManager.cs` |

**Cổng nghiệm thu G1** — chạy Play Mode, phải đạt cả 5:
1. Lên cấp 2 giữa lúc đang thu hoạch → UI tutorial **tự ẩn**, popup hiện rõ, bấm Nhận xong tutorial mới tiếp.
2. Lặp lại 5 lần liên tiếp, không lần nào đè.
3. Mở Kho / Shop / Cửa hàng giữa tutorial → card thoại tutorial **không** đè lên.
4. Popup thông báo tàu tự bật giữa tutorial → tutorial dừng chờ.
5. Console **không** còn dòng `Coroutine couldn't be started ... is inactive`.

---

### GIAI ĐOẠN 2 — TRẬT TỰ LỚP UI (một bảng, một nơi)

**Mục tiêu:** hết cảnh 8/10 Canvas trùng sortingOrder và 13 popup xếp lớp bằng vị trí tĩnh
trong Hierarchy.

**Việc làm:**

| # | Việc |
|---|---|
| 2.1 | Lập **bảng lớp chuẩn** (1 file hằng số duy nhất): World 0 · HUD 100 · Panel 200 · Popup 300 · **Tutorial 350** · Popup-trên-tutorial 400 · Chuyển cảnh 9999 |
| 2.2 | **Hạ `Tutorial_Canvas` từ 999 xuống 350** — tutorial nằm trên HUD nhưng **dưới** popup hệ thống. Đây là sửa cốt lõi: kể cả cổng ở G1 có lỗi, popup vẫn không bị đè |
| 2.3 | Gỡ trùng: 100 (`World`↔`Canvas_HUD`), 120 (`popup_Menu`↔`WarehousePopup`), 300 (`Canvas_Popup`↔`Popup_LevelUp_Township`), 400 (`TouristBoat`↔`Mill`) |
| 2.4 | `ShopManager.OpenShop()` ghi đè sortingOrder cha thành 150 và **không trả lại khi đóng** (`ShopManager.cs:118`) — sửa thành có trả lại |
| 2.5 | Canvas `World` đang **bị tắt** (`m_Enabled: 0`) mà vẫn để order 100 — xác minh có cố ý không |
| 2.6 | Viết tool Editor `KiemTraLopUI` — quét scene, báo mọi cặp trùng order |

**Cổng nghiệm thu G2:**
1. Tool `KiemTraLopUI` báo **0 cặp trùng**.
2. Mở lần lượt 10 popup, chụp màn hình từng cái — không cái nào bị cái khác che.
3. Mở 2 popup chồng nhau → cái mở sau luôn nằm trên.

---

### GIAI ĐOẠN 3 — CHUẨN HOÁ VÒNG ĐỜI MỘT BƯỚC

**Vấn đề hiện tại:** `PlayStep()` dài **374 dòng** (673→1047), chứa ~15 nhánh
`if (step.name == "...")` hardcode. Thêm bước mới là phải sửa vào giữa khối này —
đó là lý do mỗi lần vá lại sinh lỗi chỗ khác.

**Mục tiêu:** mọi bước đi qua **đúng 5 pha cố định**, khác biệt nằm ở **dữ liệu** chứ không ở code.

```
PHA 1  CỔNG      → chờ popup đóng hết, chờ camera dừng
PHA 2  DỌN       → tắt sạch guide/dim/tay chỉ của bước trước
PHA 3  DỰNG      → camera · dim · highlight · tay chỉ · card thoại
PHA 4  CHỜ       → đúng 1 điều kiện thoát, kèm watchdog
PHA 5  DỌN & ĐI  → tắt UI của bước này rồi mới sang bước kế
```

**Việc làm:**

| # | Việc |
|---|---|
| 3.1 | Mở rộng `TutorialStepData` thêm các trường mà hiện đang hardcode trong code: `subActionTruoc`, `doiCamera`, `anDimKhiCho`, `clipNpc`, `delayTruocKhiHien` |
| 3.2 | Chuyển từng nhánh hardcode sang dữ liệu — **mỗi lần 1 bước, test xong mới sang bước kế** |
| 3.3 | Rút `PlayStep()` xuống còn khung 5 pha, dưới 120 dòng |
| 3.4 | Bỏ 2 đường code song song (card V2 và hộp thoại cũ) — chỉ giữ V2 |

**Cổng nghiệm thu G3:**
1. Chạy trọn 31 bước từ đầu tới cuối, **không kẹt bước nào**.
2. `PlayStep()` không còn `if (step.name == ...)` nào.
3. So sánh video trước/sau: hành vi từng bước **giống hệt**.

---

### GIAI ĐOẠN 4 — BỘ NGHIỆM THU TỰ ĐỘNG (để lỗi vặt không quay lại)

Đây là câu trả lời cho ý Sếp: *"nhiều agent mà vẫn để lỗi vặt"*.
Lỗi vặt quay lại vì **không có ai kiểm tự động**, chỉ có mắt người.

| # | Việc |
|---|---|
| 4.1 | Tool `KiemTraTutorial`: quét `_steps` — đủ 31 bước, không ô NULL, không trùng, mọi `targetID` đều có nơi đăng ký |
| 4.2 | Tool `KiemTraLopUI` (từ G2) |
| 4.3 | Chế độ **tua nhanh**: phím tắt nhảy thẳng tới bước N để test 1 bước trong 10 giây thay vì chơi lại từ đầu |
| 4.4 | Log chuẩn `[Tutorial] ▶ Bước [i/n] · pha X · chờ Y` — đọc log là biết kẹt ở đâu, không phải đoán |
| 4.5 | Checklist nghiệm thu 31 bước, ảnh chụp từng bước |

**Cổng nghiệm thu G4:** cả 2 tool báo sạch, và test được 1 bước bất kỳ dưới 30 giây.

---

## PHẦN 3 — QUY TRÌNH LÀM VIỆC (Sếp yêu cầu "từng bước 1")

Từ vòng này trở đi, **mỗi giai đoạn** đi theo đúng 6 nhịp, không nhảy cóc:

```
1. QUÉT     → đội đọc code, báo cáo hiện trạng kèm SỐ DÒNG (không đoán)
2. BACKUP   → copy file sẽ sửa + ghi MD5 vào production/backup_*/
3. LÀM      → sửa, mỗi lần 1 việc trong bảng, không gộp
4. TỰ KIỂM  → tôi chạy tool kiểm + đọc lại diff trước khi báo Sếp
5. SẾP BẤM  → checklist ngắn, ghi rõ bấm gì, kỳ vọng thấy gì
6. CHỐT CỔNG→ đạt hết tiêu chí thì mới mở giai đoạn sau
```

**Ràng buộc tôi tự đặt cho mình từ vòng này:**
- Không đụng file nào chưa backup.
- Không sửa 2 giai đoạn cùng lúc.
- Mỗi lần báo cáo phải kèm **cách kiểm chứng**, không nói suông "đã sửa".
- Việc gì đo được thì đo (đếm pixel, đếm mảng, đọc log), không kết luận bằng mắt.

---

## PHẦN 4 — ĐỀ XUẤT THỨ TỰ

**Làm ngay G1 + G2** — hai cái này sửa dứt điểm chuyện đè UI và lỗi lên cấp Sếp vừa gặp.
Ước lượng: G1 khoảng 6 việc, G2 khoảng 6 việc, đều là sửa có kiểm soát, không viết lại logic lớn.

**G3 để sau** — đây là refactor `PlayStep()` 374 dòng, rủi ro cao nhất.
Chỉ nên làm khi G1/G2 đã ổn định và có bộ kiểm ở G4 đỡ lưng.

**G4 làm song song với G3** — tool kiểm phải có trước khi động vào `PlayStep()`.
