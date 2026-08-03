# PLAN — SỬA POPUP LÊN CẤP KHÔNG HIỆN

> Trạng thái: **ĐÃ TÌM RA NGUYÊN NHÂN GỐC** (không phải phỏng đoán — có dẫn chứng số dòng).
> Quy trình: chẩn đoán → sửa → **chụp ảnh Game view ra file → Claude đọc ảnh xác minh** → lặp tới khi popup hiện.

---

## PHẦN 1 — NGUYÊN NHÂN GỐC

### 🔴 Popup bị dựng vào SAI CANVAS — nằm trong bong bóng đơn hàng của một căn nhà

Tool tự chọn canvas bằng dòng này:

```csharp
if (c.name.ToLower().Contains("popup")) { _targetCanvas = c; break; }
```

`House_02.prefab` có một GameObject tên **`OrderPopup2`** — bong bóng hiện đơn hàng trên đầu nhà. Tên nó **chứa chữ "popup"** → tool bốc trúng nó.

Chuỗi bằng chứng:

| Bằng chứng | Vị trí |
|---|---|
| Popup mới có `m_Father` trỏ tới RectTransform `698583181` | `SCN_Farm.unity:112630` |
| `698583181` là instance của `House_02.prefab`, nằm trong `House_10 (1)` | `SCN_Farm.unity:236752` |
| GameObject đó tên `OrderPopup2`, `localScale = 0.005` | `House_02.prefab:93, 108` |
| Cha nó `OrderAnchor (3)` có `localScale = 3` | `House_02.prefab:236-238` |
| Canvas của nó là **`m_RenderMode: 2` = World Space** | `House_02.prefab:173-194` |
| `HouseOrderBubble.Awake()` gọi `gameObject.SetActive(false)` | `HouseOrderBubble.cs:18-21` |
| Scene có ~50 căn nhà loại này, `FindObjectsSortMode.None` không bảo đảm thứ tự | — |

**Hậu quả cộng dồn:**

1. Popup bị **co nhỏ ~67 lần** (0.005 × 3 = 0.015).
2. Nằm ở **toạ độ world của căn nhà** (khoảng -3106, -2696) — ngoài khung hình.
3. `OrderPopup2` **không có CanvasScaler**, `sizeDelta` chỉ 100×100.
4. **Bị `Awake()` tắt ngay frame đầu** → `activeInHierarchy = false` vĩnh viễn.

### 🔴 Hệ quả 1 — Coroutine không chạy được

`ShowNextPopup()` gọi `popupRoot.SetActive(true)` (dòng 167) — `activeSelf` thành true nhưng **`activeInHierarchy` vẫn false** vì tổ tiên đang tắt. Nên:

- `SpawnVFX()` dòng 428 `StartCoroutine(...)` → **lỗi** "Coroutine couldn't be started because the game object is inactive!"
- `StartCoroutine(AnimateIn())` dòng 171 → **lỗi tương tự**

### 🔴 Hệ quả 2 — Cờ `_isShowing` kẹt vĩnh viễn (lý do bấm lần 2 im re)

```
dòng 161:  _isShowing = true      ← đặt trước, KHÔNG có try/finally
dòng 162:  IsActive   = true      ← static, kẹt luôn
```

`_isShowing` chỉ về `false` khi user bấm nút "Nhận Quà" — mà nút đang vô hình. Nên:

- Lần bấm ② **đầu tiên**: `ShowNextPopup()` chạy, sinh lỗi coroutine.
- Lần bấm ② **thứ 2+**: `HandleLevelChanged` dòng 143 thấy `_isShowing == true` → **chỉ Enqueue, không gọi ShowNextPopup** → Console chỉ còn đúng 1 dòng log của tool. **Khớp chính xác hiện tượng bạn thấy.**

`IsActive` static kẹt true → **tutorial bị chặn vĩnh viễn** vì nó chờ popup "nhường sân khấu".

### 🔴 Hệ quả 3 — Input lock kẹt

Dòng 169 `AcquireInputLock()` **đã chạy thành công** → `popupLockCount = 1` → `FarmInputLock.BlockMapPan = true` mãi mãi. **Dấu hiệu kiểm chứng: sau khi bấm ②, bạn sẽ không kéo/pan map được nữa.**

### 🔴 Lỗi riêng — `levelRewardConfigs` bị RỖNG

Scene ghi `levelRewardConfigs: []` tại `SCN_Farm.unity:112667`. Lệnh `CopyFromSerializedProperty` chép config từ popup cũ **đã thất bại âm thầm**. Kể cả sửa được canvas, popup sẽ hiện mà **không có vàng/ngọc/quà nào**.

Project có 29–31 asset ở `Assets/_Game/Farm/data/Lever Game/LevelReward_L*.asset`.

### ✅ Các giả thuyết đã LOẠI TRỪ (để không mất thời gian)

| Giả thuyết | Kết luận |
|---|---|
| Canvas sorting bị che | **Loại** — nested Canvas có `overrideSorting=true` được so sánh **toàn cục**, order 300 vẫn trên HUD 100 |
| CanvasGroup alpha kẹt 0 | **Loại** — scene ghi `m_Alpha: 1`, `fadeInDuration: 0.25` |
| `fadeInDuration = 0` gây chia cho 0 | **Loại** — C# cho ra `Infinity`, `Clamp01` → 1, thoát vòng lặp an toàn |
| `PopulateUI` ném exception | **Loại** — nhánh `cfg == null` null-check đủ 100% (dòng 242-252) |
| Tìm thấy popup CŨ thay vì mới | **Loại** — scene chỉ còn **1** component `LevelUpPopupUI` |
| `AcquireInputLock`/`SpawnVFX` ném exception | **Loại** — mọi tham chiếu đều non-null |

---

## PHẦN 2 — KẾ HOẠCH SỬA (8 việc)

| # | Việc | File | Mức |
|---|---|---|---|
| F1 | Bộ chọn Canvas thông minh: **loại World Space, loại canvas lồng, loại canvas trong prefab nhà**, ưu tiên `Canvas_Popup` | `LevelUpPopupTownshipTool.cs` | 🔴 Cao |
| F2 | Chốt cứng trong `Build()`: canvas không hợp lệ → hiện hộp thoại, **từ chối dựng** | `LevelUpPopupTownshipTool.cs` | 🔴 Cao |
| F3 | Tự nạp 29–31 `LevelRewardConfig` từ project (thay `CopyFromSerializedProperty` đã fail) | `LevelUpPopupTownshipTool.cs` | 🔴 Cao |
| F4 | `ShowNextPopup`: phát hiện `activeInHierarchy == false` → **log rõ đường dẫn + canvas + scale**, KHÔNG để kẹt cờ | `LevelUpPopupUI.cs` | 🔴 Cao |
| F5 | `TestShowPopup`: reset `_isShowing` + cảnh báo nếu object đang tắt | `LevelUpPopupTownshipTool.cs` | 🟠 TB |
| F6 | `Diagnose`: in **Canvas cha, renderMode, đường dẫn hierarchy, lossyScale** | `LevelUpPopupTownshipTool.cs` | 🟠 TB |
| F7 | **Tool chụp Game view ra PNG + xuất báo cáo runtime ra file** → Claude đọc để xác minh thật | file mới | 🔴 Cao |
| F8 | `GrantRewards`: thêm null-check `cfg.giftItems` (NRE tiềm ẩn khi bấm Nhận Quà) | `LevelUpPopupUI.cs` | 🟠 TB |

---

## PHẦN 3 — VÒNG LẶP XÁC MINH

Claude **không chụp được màn hình Unity trực tiếp**. Cách giải quyết:

```
F7 tạo tool  →  bạn bấm 1 nút trong Unity
                     ↓
   Ghi ra Assets/_Debug_Capture/
     • game_view.png      ← ảnh chụp Game view
     • popup_report.txt   ← trạng thái runtime đầy đủ
                     ↓
   Claude ĐỌC 2 file này  →  thấy đúng cái bạn thấy
                     ↓
   Chưa hiện?  →  sửa tiếp  →  lặp lại
   Hiện rồi?   →  xong
```

`popup_report.txt` sẽ ghi:

- Đường dẫn hierarchy đầy đủ của popup
- `activeSelf` / `activeInHierarchy` từng cấp — chỉ ra **chính xác cấp nào đang tắt**
- Canvas cha: tên, `renderMode`, `sortingOrder`, có `CanvasScaler` không
- `lossyScale` (phải ≈ 1,1,1)
- `CanvasGroup.alpha`
- Kích thước rect của từng thành phần
- Sprite nào null
- Số `LevelRewardConfig`
- `_isShowing`, `IsActive`, `FarmInputLock` counter

---

## PHẦN 4 — BẠN LÀM GÌ (sau khi Claude sửa xong)

1. **Thoát Play Mode**
2. `Tools ▸ Farm ▸ Popup Lên Cấp (Township)` → **DỰNG POPUP** (giờ sẽ từ chối nếu canvas sai)
3. Bấm **① Chẩn đoán** → xác nhận Canvas cha là `Canvas_Popup (Overlay)`
4. **Play** → bấm **② Bật thử popup**
5. Bấm nút **③ Chụp ảnh + xuất báo cáo**
6. Nhắn Claude: *"đã chụp"* → Claude đọc file và xác minh

---

## PHẦN 5 — TIÊU CHÍ HOÀN THÀNH

- [ ] `popup_report.txt`: Canvas cha = `Canvas_Popup`, `renderMode = ScreenSpaceOverlay`
- [ ] `popup_report.txt`: mọi cấp tổ tiên `activeInHierarchy = true`
- [ ] `popup_report.txt`: `lossyScale ≈ (1, 1, 1)`
- [ ] `popup_report.txt`: `CanvasGroup.alpha = 1`
- [ ] `popup_report.txt`: số `LevelRewardConfig` ≥ 29
- [ ] Console: **không có** lỗi "Coroutine couldn't be started"
- [ ] `game_view.png`: **Claude nhìn thấy băng rôn xanh + ngôi sao + nút xanh trên ảnh**
- [ ] Sau khi bấm nút xanh: popup đóng, map pan lại được (input lock đã nhả)
