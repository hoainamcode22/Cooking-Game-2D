# CHECKLIST VÒNG 16 — 2026-09-04

## Nguyên nhân gốc: tutorial KHÔNG kẹt, mà bị CỤT 10 bước

Log Play Mode của Sếp nói rõ:

```
[Tutorial] StartTutorial — total steps: 21
[Tutorial] Step [20/20] L1L2_18_LevelUpCelebration
[Tutorial] Tutorial FINISHED — restoring camera and closing UI.
```

Đối chiếu bản Git `c05e3ebb` (01/09/2026): `_steps` có **31** phần tử.
Bản đang làm việc: **21**. → 10 bước Level-2 đã rơi khỏi danh sách.

| | Bản Git c05e3ebb | Bản hiện tại |
|---|---|---|
| Số bước trong `_steps` | 31 | 21 |
| Bước cuối | `L2_10_HarvestPen` | `L1L2_18_LevelUpCelebration` |

10 asset bị mất khỏi list (file vẫn còn nguyên trong `Assets/Resources/TutorialSteps/L1_L2/`):

```
L2_01_GotoShop      L2_06_AnimalIntro
L2_02_UnlockCorn    L2_07_FocusPen
L2_03_BuyCorn       L2_08_FeedPen
L2_04_CloseShop     L2_09_PenSpeedUp
L2_05_PlantCorn     L2_10_HarvestPen
```

> Lưu ý: `L1L2_04b_FirstHarvest.asset` **chưa bao giờ** nằm trong `_steps` ở bất kỳ
> commit nào → là asset dự phòng, tool KHÔNG đụng vào.

Đã kiểm chứng: 8/10 bước L2 có handler riêng trong `TutorialManager.cs`
(`L2_01, L2_03, L2_04, L2_05, L2_07, L2_08, L2_09, L2_10`); 2 bước còn lại
(`L2_02`, `L2_06`) chỉ là câu thoại NPC. → Code chạy được đủ 31 bước.

---

## VIỆC SẾP CẦN BẤM (theo thứ tự)

### 1. Mở scene
Mở `Assets/_Game/Scenes/SCN_Farm.unity`

### 2. Xem trước (không ghi gì)
`Tools ▸ Farm ▸ Tutorial ▸ Khoi phuc 10 buoc L2 - DRY RUN (chi bao cao)`

Kỳ vọng trong Console:
```
_steps hien tai : 21 phan tu
Se them moi     : 10
    + L2_01_GotoShop ... + L2_10_HarvestPen
KET LUAN: sau khi APPLY, _steps se co 31 phan tu.
```

### 3. Ghi thật
`Tools ▸ Farm ▸ Tutorial ▸ Khoi phuc 10 buoc L2 - APPLY (ghi vao scene)`

### 4. **Ctrl + S** — bắt buộc
Tool KHÔNG tự lưu scene. Chưa Ctrl+S là chưa ăn thua.

> Ghi chú: lần trước Sếp bấm tool dựng card Tutorial V2 nhưng **chưa Ctrl+S**,
> nên NPC trong scene vẫn ở toạ độ cũ `(24, 210)` trong khi tool đã đặt
> `(420, -40)`. Lần Ctrl+S này sẽ lưu luôn cả hai thay đổi.

### 5. Nếu sai
- `Ctrl + Z` (tool có `Undo.RecordObject`), hoặc
- Khôi phục `production/backup_round16_2026-09-04/SCN_Farm.unity.bak`

---

## 3 lỗi runtime đã vá sẵn trong code (không cần bấm gì)

| Lỗi trong log | File đã sửa | Cách sửa |
|---|---|---|
| `Coroutine couldn't be started because the game object 'Tutorial_GuideBoard' is inactive!` × 3 | `TutorialGuideBoardUI.cs` → `Hide()` | Nếu object đã tắt thì đóng thẳng, không chạy animation (cùng khuôn guard đã dùng cho `SettingsPopupUI.ClosePopup()`) |
| `Setting the duration while system is still playing is not supported` × nhiều | `HarvestSlashFX.cs` → `Spawn()` | `AddComponent<ParticleSystem>()` tự Play ngay → `Stop()` trước, cấu hình xong mới `Play()` |
| `Hand pointer target 'seed_rice' chua dang ky` → `Hand pointer target: NONE` (bước 4 & 15) | `TutorialManager.cs` | Hạt giống chỉ đăng ký khi khay hạt mở ra; trước đây resolve đúng 1 lần rồi bỏ → nay chờ tối đa 12s rồi gắn lại vòng sáng highlight |

---

## Backup vòng 16

`production/backup_round16_2026-09-04/`

```
SCN_Farm.unity.bak            0729ff069655250caca1a4979386b3b8
TutorialManager.cs.bak        469dccf4deb42babe4b90f8160a93004
TutorialGuideBoardUI.cs.bak   7e53bae10702d59c4a9488049c3e4f17
HarvestSlashFX.cs.bak         3aeebbe7b79149cc9ce63611b7dfbaec
```

---

## Còn tồn (chưa làm, chờ Sếp quyết)

1. `[LevelUpPopupUI] Ô mở khoá: hiện 3/9` — dải item lên cấp thiếu icon 6/9 ô,
   chưa có animation + badge NEW.
2. ~~Gói art D — nhận hay vẽ lại?~~ **XONG** — Sếp chốt chỉ cắt, không vẽ lại.
   Xem `production/NGHIEM_THU_CAT_KHUNG_2026-09-04_VONG16.md`.
   → Sau bước Ctrl+S ở trên, bấm thêm 2 nút nạp art:
   - `Tools ▸ Farm ▸ Tutorial V2 ▸ (nút 2) Nạp art` — lấy 12 frame NPC bản 308×588
   - `MasterArtImportTool` — lấy icon gia vị bản 232×232 + gói A/C/E
3. 5 bộ mascot × 12 frame ở `2026-08-31_JuiceFX/3_LevelUp_Mascots/` có phải nhân vật
   Sếp muốn thay cho 4 NPC popup lên cấp không?
4. Chưa chạy: `MasterArtImportTool`, `TrainPopupDedupeTool`, `LevelUpPopupRewireTool`.
