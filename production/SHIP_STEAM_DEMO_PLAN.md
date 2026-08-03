# GAME THIẾU GÌ & LỘ TRÌNH LÊN STEAM — Bản demo chạy thật (24–31/7/2026)

> Người soát: 1 dev · Engine: Unity 6.3 (6000.3.10f1) · Thể loại: Cooking/Farm 2D (kiểu Hay Day / Township)
> Kèm theo tài liệu này là **8 script mới mình đã viết sẵn** trong `Assets/_Game/Scripts/Core/` (xem Mục 5).

---

## 0. Sự thật cần biết trước (đọc 30 giây)

**Không thể "phát hành công khai" trên Steam trong tháng 7.** Steam bắt buộc **chờ 30 ngày** sau khi trả phí **$100 (Steam Direct)** rồi mới được release, cộng thêm Valve review build + store page. Vậy nên mục tiêu đúng đắn cho tuần này là:

> ✅ **Một bản build `.exe` chạy thật, ổn định, có save/menu/settings, chơi mượt L1→L5** — tức "demo/vertical slice" đủ chất để (a) tự tin là game *vận hành được ngoài Editor*, và (b) dùng làm demo cho **Steam Next Fest** + trang wishlist sau này.

Cái *chặn* bạn khỏi có bản đó không phải là thiếu nội dung — mà là thiếu **khung vận hành cơ bản** (save gom về 1 mối, main menu, settings, pause, thoát game, danh tính project). Đó chính là phần mình đã code hôm nay.

---

## 1. Game bạn ĐANG CÓ gì (mạnh hơn bạn nghĩ)

Đối chiếu với vòng lặp cốt lõi của Hay Day / Township, phần "chơi" của bạn đã khá đầy:

- **Trồng – thu hoạch**: hệ ô đất (`PlotController`, ~25 ô), cây/hạt giống, RuleTiles, hiệu ứng thu hoạch.
- **Chăn nuôi**: gà → heo → bò → bò sữa (`AnimalGuideController`, chuồng/pen, đếm khẩu phần).
- **Chế biến / nấu ăn**: scene bếp riêng, hệ nguyên liệu + món (`CookingChallengeManager`, DishData, 18 món mở dần), máy xay bột / ép mía / phô mai (L11–L15).
- **Đơn hàng & giao vận**: đơn nhà dân (`VillageOrderManager`, `HouseOrderController`), tàu chở hàng (`TrainManager` — giống train của Township), chợ (`MarketManager`), kho (`WarehouseManager`).
- **Tiến trình**: level/EXP (`PlayerProgressManager`, đường cong tới L100), tiền/gem (`FarmEconomyManager`), phần thưởng lên cấp L2–L30.
- **Nhiệm vụ**: mission chính L1–L30 + daily (`MissionProgressTracker`, 9 loại event).
- **Tutorial**: L1–L2 kiểu Hay Day 19 bước.
- **Không khí**: ngày/đêm (`Day_Night`), VFX (Lana Studio), AudioManager có sẵn khung.

Nói cách khác: **"phần thịt" (gameplay) đã có**. Cái thiếu là **"bộ khung"** để nó thành một sản phẩm chạy độc lập, và **độ đánh bóng**.

---

## 2. THIẾU GÌ — nhóm A: CHẶN SHIP (bắt buộc để .exe chạy thật)

Đây là những thứ **mọi game Steam PC đều phải có**, mà code hiện tại đang thiếu. Cột "Trạng thái" cho biết mình đã xử lý tới đâu hôm nay:

| # | Thiếu | Vì sao chặn | Trạng thái |
|---|-------|-------------|------------|
| A1 | **Save gom về 1 mối** — state đang rải rác ở 21 chỗ PlayerPrefs (registry Windows) | Dễ hỏng/mất dữ liệu; Steam Cloud **không** đồng bộ được registry → phải là **file** | ✅ **Đã code** `GameSaveManager` (file JSON + versioning + Steam Cloud hook) |
| A2 | **Main Menu thật** — `SCN_Home` chỉ có 1 nút "Vào game" | Không có New Game / Continue / Settings / Quit | ✅ **Đã code** `MainMenuController` (bạn gắn nút) |
| A3 | **Settings** — không có chỉnh âm lượng / độ phân giải / fullscreen / chất lượng | Người chơi PC bắt buộc cần | ✅ **Đã code** `SettingsManager` + `SettingsPanelUI` |
| A4 | **Pause menu** — không pause/thoát được giữa chừng | Chuẩn tối thiểu PC | ✅ **Đã code** `PauseMenuController` (ESC, tự chạy) |
| A5 | **Thoát game** — không có `Application.Quit` ở đâu cả | Không đóng được game | ✅ **Đã code** (trong Pause + Main Menu) |
| A6 | **Danh tính project** — `companyName = DefaultCompany`, `productName = My project`, appId trống | Không thể build tên đàng hoàng | 🧑 **Bạn sửa** (Mục 7 — 5 phút) |
| A7 | **Steam SDK** — chưa có Steamworks | Không lên được Steam / không có cloud, achievement | ✅ **Đã code** wrapper `SteamManager` (bạn cài SDK khi cần — Mục 8) |
| A8 | **Build chạy ngoài Editor** — chưa xác nhận build .exe không lỗi | Đây LÀ mục tiêu | 🧑 **Bạn build & test** (Mục 10) |

---

## 3. THIẾU GÌ — nhóm B: nội dung & độ hoàn thiện (được phép để SAU demo)

Những cái này quan trọng cho **bản full**, nhưng **KHÔNG cần cho demo tháng 7**. Đừng ôm hết trong 1 tuần:

- Tutorial L3→L10 liền mạch (hiện mới chuẩn tới L2). — *Demo chỉ cần mượt tới L5.*
- Cân bằng kinh tế L11→L30 + mở rộng đất, nâng cấp kho/silo.
- Retention: daily login/streak, thông báo "đã chín", teaser mở khoá kế tiếp, almanac/sổ sưu tập.
- "Juice": tween nút, pop popup, số đếm, screen-shake, confetti — làm game "đã tay".
- Audio thật (mp3 coin/plant/cook/level-up) — hiện AudioManager chờ file.
- Art: mascot + NPC + icon store.
- Kiếm tiền (IAP/ads) — **bản Steam premium KHÔNG cần** (bỏ hẳn cho gọn; chỉ mobile mới cần).
- Nội dung L23–L30 (hồ cá, tourist boat, sự kiện mùa) — để update sau launch.

> Quy tắc vàng cho 1 dev: **demo hẹp mà bóng** ăn đứt **game rộng mà rời rạc**.

---

## 4. Vòng lặp Hay Day / Township so với game bạn

| Trụ cột Township/Hay Day | Game bạn | Nhận xét |
|--------------------------|----------|----------|
| Trồng → thu → bán/nấu | ✅ Có | Vòng lặp lõi ổn |
| Đơn hàng (nhà dân) | ✅ `VillageOrderManager` | Ổn |
| Tàu chở hàng | ✅ `TrainManager` | Giống Township |
| Chế biến nhiều tầng | ✅ Máy xay/ép/phô mai | Tốt |
| Level mở khoá dần | ✅ L1–L30 data | Tốt |
| Nhiệm vụ + daily | ✅ Có | Tốt |
| **Lưu game chắc chắn** | ⚠️ Rải rác → ✅ đã gom | **Vừa sửa** |
| **Menu/Settings/Pause** | ❌ → ✅ đã thêm | **Vừa thêm** |
| Bạn bè / xã hội / chợ toàn cầu | ❌ Chưa | Bỏ qua cho demo |
| Sự kiện định kỳ (live-ops) | ❌ Chưa | Sau launch |
| Đánh bóng (juice/audio/art) | ⚠️ Khung | Việc chính sau demo |

---

## 5. VIỆC MÌNH ĐÃ CODE HÔM NAY (8 file + 1 sửa)

Tất cả nằm trong `Assets/_Game/Scripts/Core/` (trừ AudioManager). **Chỉ CỘNG THÊM, không xoá logic cũ** → an toàn. Không dùng namespace của bạn (đặt trong `Game.Core`).

1. **`ISaveable.cs`** — interface để hệ con (kho, ô đất, mission…) tự cắm vào save trung tâm sau này.
2. **`GameSaveManager.cs`** — save JSON tập trung ở `persistentDataPath/save_slot0.json`: có `saveVersion` (migrate), auto-save mỗi 30s + khi pause/quit, `NewGame()`/`Continue()`/`HasSave()`, **tự nạp khi vào `SCN_Farm`**, hook Steam Cloud. *Tự sinh, không cần kéo vào scene.*
3. **`SettingsManager.cs`** — âm lượng (master/music/sfx), độ phân giải, fullscreen, chất lượng; lưu ở PlayerPrefs tiền tố `SET_` (không bị xoá khi New Game). *Tự sinh.*
4. **`SettingsPanelUI.cs`** — cắm vào panel Settings bạn dựng; kéo thả slider/toggle/dropdown (ô nào trống thì bỏ qua).
5. **`PauseMenuController.cs`** — **ESC để pause**; TỰ DỰNG overlay bằng code (Tiếp tục / Cài đặt / Về Menu / Thoát) nên **chạy được ngay không cần dựng UI**. *Tự sinh qua GameBootstrap.*
6. **`MainMenuController.cs`** — gắn vào `SCN_Home`; kéo 4 nút vào là xong (tự ẩn "Tiếp tục" khi chưa có save; có panel xác nhận New Game).
7. **`SteamManager.cs`** — wrapper Steamworks.NET, **bọc trong `#if STEAMWORKS_NET`** nên build vẫn chạy khi chưa cài SDK. Có Init, achievement, Steam Cloud read/write.
8. **`GameBootstrap.cs`** — tự sinh PauseMenu để ESC chạy ở mọi scene gameplay.
9. **`AudioManager.cs`** *(sửa nhẹ)* — thêm `SetMusicVolume` / `SetSfxVolume` để slider âm lượng hoạt động.

### Cách gắn vào Unity (làm 1 lần, ~30–45 phút)

**Bước 0 — mở project & kiểm lỗi.** Mở Unity, để nó import (tự sinh `.meta`). Mở **Console** → phải **0 lỗi đỏ**. (Nếu có lỗi, gửi mình log.)

**Bước 1 — Save/Settings/Pause: KHÔNG cần làm gì.** Ba thứ này tự chạy (auto-init). ESC trong `SCN_Farm` sẽ mở pause menu ngay.

**Bước 2 — Main Menu (trong `SCN_Home`):**
1. Tạo GameObject rỗng tên `MainMenu`, Add Component → `MainMenuController`.
2. Tạo 4 Button (có thể nhân bản `Btn_GoGame` sẵn có cho đồng bộ style): **Tiếp tục, Chơi mới, Cài đặt, Thoát**.
3. Kéo 4 button vào 4 ô tương ứng trong `MainMenuController`. Xong. *(Hoặc gắn OnClick → `Continue` / `NewGame` / `OpenSettings` / `QuitGame`.)*

**Bước 3 — Panel Settings (dựng 1 lần):**
1. Trong `SCN_Home` (và/hoặc `SCN_Farm`), tạo 1 Panel tối giản: 3 **Slider** (Master/Music/SFX), 1 **Toggle** (Fullscreen). *(Muốn xịn thì thêm 2 TMP_Dropdown Resolution/Quality.)*
2. Add Component → `SettingsPanelUI`, kéo các slider/toggle vào ô tương ứng.
3. Kéo Panel này vào ô `settingsPanel` của `MainMenuController`. Pause menu sẽ **tự tìm** `SettingsPanelUI` trong scene khi bấm "Cài đặt".

**Bước 4 — kiểm thử vòng đời save:** Play → chơi kiếm ít vàng/lên cấp → **Thoát qua Pause** → Play lại → bấm **Tiếp tục** → phải giữ nguyên vàng/level. Vào persistentDataPath xem có `save_slot0.json`.

---

## 6. VIỆC BẠN PHẢI TỰ LÀM TRONG UNITY (mình không code thay được)

- Dựng UI (nút, panel, slider) như Mục 5 — kéo thả trong Editor.
- Sửa danh tính project (Mục 7).
- Cấu hình & bấm Build (Mục 10).
- Cài Steamworks.NET nếu muốn bật Steam (Mục 8).
- Cung cấp file: **icon app** (.png 512+), **audio thật** (mp3), ảnh store.

---

## 7. Sửa danh tính project (5 phút — bắt buộc trước khi build)

**Edit → Project Settings → Player:**

| Trường | Hiện tại | Đổi thành |
|--------|----------|-----------|
| Company Name | `DefaultCompany` | Tên studio của bạn (vd `Astronex`) |
| Product Name | `My project` | Tên game thật (vd `Tiny Kitchen Farm`) |
| Version | `1.61` | `0.1.0` (demo) |
| Default Is Full Screen / Mode | Windowed (3) | **Fullscreen Window** (khuyến nghị PC) |
| Default Screen Width/Height | 1280×720 | 1920×1080 |
| Icon | — | Kéo icon .png vào **Default Icon** |
| Api Compatibility / Backend | Mono (dev) | **IL2CPP** cho bản release |

> Lưu ý: `bundleVersion` cũ là 1.61 — nên hạ về `0.x` cho đúng giai đoạn demo/EA.

---

## 8. Tích hợp Steam (làm khi đã có bản chạy tốt)

**Sự thật quan trọng (2026):**
- Phí **Steam Direct = $100/game**, được hoàn lại sau khi game đạt **$1,000 doanh thu**.
- **Bắt buộc chờ 30 ngày** sau khi trả phí + xác minh danh tính/thuế **trước khi** được release. → *Vì vậy dù demo xong tháng 7, public release sớm nhất cũng phải cuối tháng 8.*
- Steam ăn **30%** doanh thu (cải thiện sau mốc $10M/$50M).
- Valve **review build + store page** (kiểm kỹ thuật/tuân thủ, không chấm hay/dở).

**Các bước kỹ thuật (khi sẵn sàng):**
1. Cài **Steamworks.NET** (qua Package Manager Git URL hoặc `.unitypackage` từ github.com/rlabrecque/Steamworks.NET).
2. Project Settings → Player → **Scripting Define Symbols** thêm `STEAMWORKS_NET` → phần Steam trong `SteamManager` tự bật.
3. Tạo file `steam_appid.txt` ở thư mục gốc project chứa **App ID** của bạn (test dùng `480` = Spacewar).
4. Đặt 1 GameObject `[SteamManager]` ở `SCN_Home`, điền App ID vào Inspector.
5. Cloud & achievement **tự chạy** qua hook mình đã cắm trong `GameSaveManager` (`TryCloudWrite/Read`) và `SteamManager.UnlockAchievement("API_NAME")`.

**Song song (không cần code):** trả phí, tạo **Steam page sớm** (capsule art + screenshot + trailer) để **bật nút Wishlist** càng sớm càng tốt — đây là thứ quyết định lượt mua ngày launch.

---

## 9. "Thứ tự làm game" đúng cho 1 dev (bạn hỏi mục này)

Bạn đang ở tình huống điển hình của solo dev: gameplay nhảy vọt, khung + đánh bóng bị bỏ lại. Thứ tự chuẩn để "về đích":

1. **Ổn định nền (khung)** ← *bạn đang ở đây, và phần lớn mình vừa code xong*: save, menu, settings, pause, build, danh tính.
2. **Vertical slice**: chọn 1 lát chơi ngắn (L1→L5) và làm nó **mượt + bóng 100%**. Đây là "demo".
3. **Playtest thật** với 3–5 người lạ, ghi chỗ họ kẹt/bỏ cuộc → sửa.
4. **Đánh bóng** (juice + audio + art) cho lát đó.
5. **Steam page + wishlist** (song song từ bước 2).
6. **Mở rộng nội dung** (L6→L30) *sau khi* lát đầu đã hoàn hảo.
7. **Cân bằng kinh tế** bằng bảng mô phỏng.
8. **QA + build final + gửi Valve**.

Nguyên tắc: **luôn giữ game ở trạng thái build-được**. Mỗi tính năng mới → build thử → chơi ngoài Editor. Đừng để dồn tới cuối mới build lần đầu (rất dễ vỡ).

---

## 10. LỘ TRÌNH NGÀY-THEO-NGÀY (24 → 31/7) — mục tiêu: demo .exe chạy thật

| Ngày | Việc | Kết quả cần đạt |
|------|------|-----------------|
| **T6 24/7** | Mở project, xác nhận Console 0 lỗi với 8 script mới. Sửa danh tính project (Mục 7). | Build settings & Player đã đúng tên/icon/độ phân giải |
| **T7 25/7** | Dựng Main Menu (4 nút) + gắn `MainMenuController`. Test Continue/New Game/Quit. | Vào game từ menu, thoát được |
| **CN 26/7** | Dựng panel Settings (3 slider + fullscreen) + `SettingsPanelUI`. Test ESC pause + đổi âm lượng/độ phân giải. | Settings + Pause chạy thật |
| **T2 27/7** | **Build Windows lần đầu** (IL2CPP, x86_64). Chạy .exe ngoài Editor. Sửa mọi lỗi build/khi chạy. | Có `.exe` mở được, không crash |
| **T3 28/7** | Test vòng đời save trên bản .exe: chơi → thoát → mở lại → Continue giữ nguyên. Sửa scene flow nếu kẹt. | Save/Load chắc chắn trên build |
| **T4 29/7** | Chốt "vertical slice" L1→L5: bịt mọi chỗ kẹt tiền/kẹt đơn/lỗi tutorial trong lát này. | Chơi liền mạch L1→L5 |
| **T5 30/7** | Đánh bóng nhanh lát demo: audio cơ bản, vài tween nút, popup lên cấp. | Lát demo "đã tay" |
| **T6 31/7** | Playtest 2–3 người, ghi lỗi. Build bản demo cuối `0.1.0`. (Tuỳ chọn) bắt đầu tạo Steam page. | **Demo .exe hoàn chỉnh để chia sẻ** |

> Nếu chỉ có ít giờ mỗi ngày: **ưu tiên tuyệt đối 24–28/7** (khung + build được). Đó là ranh giới giữa "một đống script" và "một game chạy".

---

## 11. Checklist ship (dán ra, tick dần)

**Chặn ship (phải xong cho demo):**
- [ ] Console 0 lỗi đỏ sau khi thêm 8 script
- [ ] Company/Product name, version, icon đã đặt
- [ ] Main Menu: New Game + Continue + Settings + Quit chạy
- [ ] Settings: âm lượng + fullscreen + độ phân giải áp dụng được
- [ ] ESC pause + Về Menu + Thoát game chạy
- [ ] `save_slot0.json` sinh ra; Continue giữ đúng tiến trình
- [ ] Build Windows IL2CPP x86_64 mở được ngoài Editor, không crash
- [ ] Chơi liền mạch L1→L5 không kẹt

**Trước khi lên Steam (sau demo):**
- [ ] Trả phí Steam Direct $100 + xác minh danh tính/thuế (bắt đầu sớm vì chờ 30 ngày)
- [ ] Bật `STEAMWORKS_NET` + `steam_appid.txt` + App ID → Steam init OK
- [ ] Steam Cloud lưu/nạp được; ≥3 achievement chạy
- [ ] Steam page: capsule art, 4–6 screenshot, trailer ngắn, mô tả → bật Wishlist
- [ ] Vượt Valve review (build + store page)
- [ ] (Khuyến nghị) đăng ký **Steam Next Fest** với bản demo

---

### Nguồn (Steam publishing)
- Steamworks — Steam Direct Fee: https://partner.steamgames.com/doc/gettingstarted/appfee
- How to Publish Your Game on Steam in 2026: https://www.thegamemarketer.com/insight-posts/how-to-publish-your-game-on-steam-guide
- Valve $100 Steam Direct fee: https://www.gamedeveloper.com/business/valve-will-charge-devs-100-to-publish-games-through-steam-direct
