# PROMPT — THỢ B (Vòng 1 / Phase 0): Main Menu + Settings + Pause + One-click Build

> Dán `_SHARED_CONTRACT.md` vào trước, rồi dán prompt này.

---

Bạn là **ui-programmer kiêm unity-ui-specialist** (kèm vai devops cho build tool), theo `Claude-Code-Game-Studios/.claude/agents/ui-programmer.md`, `unity-ui-specialist.md`, `devops-engineer.md`. Làm theo `production/AUTONOMY.md`, vòng lặp **SCAN → IMPLEMENT → CHECK → REPORT**. Chỉ CỘNG THÊM an toàn; đụng STOP LIST thì dừng, ghi "CẦN BẠN".

**Phạm vi sở hữu vòng này:** UI script, prefab UI, `AudioManager.cs`, Editor build tool, và **scene `SCN_Home.unity`** (chỉ mình bạn sửa scene này). KHÔNG sửa file logic/manager của Thợ A. KHÔNG sửa `SCN_Farm.unity` (chỉ đọc để tham chiếu).

## BƯỚC 0 — SCAN ⚠️ theo LUẬT SCAN ở hợp đồng
> **Kiểm tra tồn tại trước, đừng dựng trùng.** Nhiều popup/menu đã có sẵn dạng khung — tái dùng thay vì viết mới. Ghi mục "KIỂM KÊ TRƯỚC KHI LÀM" trong report.
1. Mở YAML `Assets/_Game/Scenes/SCN_Home.unity` xem hierarchy hiện có (`Canvas_Home`, `Btn_GoGame`, `HomeSceneUI`, `Background`).
2. Đọc **home menu ĐÃ CÓ**: `Assets/Scripts/Core/HomeSceneUI.cs`, `Assets/_Game/Scripts/Mission/HomeMenuManager.cs`, `Assets/_Game/Scripts/UI/HomeMenuController.cs` — nếu đã có logic điều hướng/menu thì **mở rộng nó**, đừng viết `MainMenuController` chồng chéo vô ích.
3. Đọc `SceneLoader.cs` (dùng `SceneLoader.Instance.LoadScene("SCN_Farm")`).
4. Đọc **`PopupManager.cs`** — nếu hợp, dựng Settings/Pause **tái dùng hệ popup này** thay vì tạo canvas rời.
5. Xem các popup thưởng ĐÃ CÓ để học pattern & style (đừng đụng chức năng của chúng): `LevelUpPopupUI`, `UnifiedTaskPopupUI`, `AttendanceManager`, `WelfareEventManager`.
6. Đọc `Assets/_Game/Audio/AudioManager.cs` (sẽ thêm setter âm lượng).
7. Đọc hợp đồng: gọi `GameSaveManager.Instance.HasSave()/NewGame()` (Thợ A xây); dùng đúng khoá `SET_*`.

## NHIỆM VỤ (task nhỏ)

> ⚠️ TOOL-FIRST (xem hợp đồng): với mỗi phần dưới đây, viết (1) script runtime + (2) **Editor Tool tự dựng** để anh chỉ bấm 1 nút. KHÔNG bắt anh kéo-thả tay.

### B1 — Main Menu trong SCN_Home
- Tạo `Assets/_Game/Scripts/Core/MainMenuController.cs` (namespace `Game.Core`).
- Logic: "Tiếp tục" hiện khi `HasSave()==true` (ẩn nếu chưa) → `SceneLoader.Instance.LoadScene("SCN_Farm")`. "Chơi mới" → nếu đã có save thì hiện panel xác nhận → `GameSaveManager.Instance.NewGame()` rồi vào game. "Thoát" → `Application.Quit()` (Editor: `EditorApplication.isPlaying=false`).
- **TOOL bắt buộc:** `Assets/_Game/Editor/MainMenuSetupTool.cs` → menu `Tools → Setup → Main Menu (SCN_Home)`: tự tạo/tìm Canvas, dựng 4 nút **Tiếp tục / Chơi mới / Cài đặt / Thoát** (nhân bản style `Btn_GoGame`), gắn `MainMenuController`, **gán sẵn tham chiếu 4 nút + panel xác nhận**, ping object. Idempotent.
- **Acceptance:** bấm tool 1 lần → menu hiện đủ nút, chạy được; chạy tool lần 2 không nhân đôi.

### B2 — Settings (âm lượng + màn hình)
- Thêm vào `AudioManager.cs` (cộng thêm): `SetMusicVolume(float)`, `SetSfxVolume(float)` áp hệ số lên `bgmSource`/`uiSource`/`fxSource` (giữ tỉ lệ mix gốc).
- Tạo `SettingsManager.cs` (tự sinh, `DontDestroyOnLoad`): master (`AudioListener.volume`), music, sfx, fullscreen, độ phân giải, chất lượng. Lưu PlayerPrefs **đúng khoá `SET_*`**. Áp dụng khi khởi động.
- Tạo `SettingsPanelUI.cs` (bind slider/toggle → manager).
- **TOOL bắt buộc:** `Assets/_Game/Editor/SettingsPanelSetupTool.cs` → menu `Tools → Setup → Settings Panel`: tự dựng panel (3 slider Master/Music/SFX + toggle Fullscreen + tuỳ chọn 2 TMP_Dropdown), gắn `SettingsPanelUI`, **gán sẵn mọi tham chiếu**, đặt vào `SCN_Home` (và prefab để tái dùng ở pause). Idempotent.
- **Acceptance:** bấm tool → panel ra đủ, kéo slider nghe âm lượng đổi ngay; thoát/mở lại giữ cài đặt; khoá `SET_*` có trong PlayerPrefs.

### B3 — Pause menu (ESC) cho gameplay
- Tạo `PauseMenuController.cs` (namespace `Game.Core`): nhấn **ESC** → `Time.timeScale=0`, overlay (Tiếp tục / Cài đặt / Về Menu / Thoát). "Về Menu" gọi `GameSaveManager.Instance.Save()` rồi load `SCN_Home`. **Tự dựng overlay bằng code lúc chạy** (Canvas sortingOrder cao + `TextMeshProUGUI`) để chạy ngay KHÔNG cần setup tay — đây đã đúng tinh thần tool-first (zero-setup). Tự sinh qua một bootstrap `[RuntimeInitializeOnLoadMethod]`.
- Không pause ở `SCN_Home`. `Time.timeScale` luôn về 1 khi rời pause (chống kẹt).
- **Acceptance:** vào SCN_Farm nhấn ESC ra menu; Về Menu/Thoát/Tiếp tục chạy; không kẹt timeScale; không cần thao tác setup nào.

### B4 — One-click Build tool (build liên tục)
- Tạo `Assets/_Game/Editor/BuildTool.cs`: menu `Tools → Build → Windows Demo (x86_64)` → build ra `Builds/Windows/`, scene đúng thứ tự (`SCN_Home`, `SCN_Farm`, `SampleScene`), tự đặt tên theo thời gian. In đường dẫn.
- **Acceptance:** bấm menu → ra `.exe` chạy được; log đường dẫn.

## VERIFY (bắt buộc)
- Play Mode Console 0 lỗi đỏ.
- Test vòng đời: Menu → New Game → chơi → ESC → Về Menu → Continue giữ tiến trình (phối hợp với save của Thợ A).
- Chạy `/code-review` trên file mới.

## BÁO CÁO CUỐI
```
## Thợ B — Vòng 1 report
- KIỂM KÊ TRƯỚC KHI LÀM: (menu/popup đã tồn tại liên quan + quyết định: mở rộng / tái dùng PopupManager / viết mới)
- Đã làm: B1 …, B2 …, B3 …, B4 …
- File mới/sửa: … (gồm SCN_Home.unity)
- Log kiểm chứng: [Menu] …, [Settings] …
- ANH CẦN LÀM TRONG UNITY: (chỉ dạng "bấm menu Tools → Setup → Main Menu", "Tools → Setup → Settings Panel", "Tools → Build → Windows Demo")
- CẦN BẠN: (nếu có)
```
