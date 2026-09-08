# PLAN HỒ CÂU (SCN_Fishing) — VÒNG 12 · 07/09/2026

> Người lập: Tech Lead · Người duyệt: Sếp Edric (duyệt Vòng 1 ngày 07/09).
> Quyết định Sếp đã chốt: (1) **giỏ cá riêng + quầy cá riêng** ở farm, không dùng kho farm;
> (2) **Firebase toàn bộ** cho phần online (làm sau); (3) Vòng 1 = **khung OFFLINE chạy đúng logic trong Play Mode**,
> tab "Hồ câu" gắn vào HUD SCN_Farm **bằng Editor Tool**, không sửa tay scene.
> Nhân vật người chơi = 2 sheet hành khách (PlayerF cô gái máy ảnh, PlayerM cậu bé balo), đã cắt 24 frame.

---

## 0. KIỂM KÊ TRƯỚC KHI LÀM (SCAN 4 báo cáo, 556 file .cs)

| Cần gì | Có sẵn không | Dùng lại / Tránh |
|---|---|---|
| Đổi scene có màn chờ | `SceneTransitionManager.LoadScene(name, CloudWipe, Single)` (Core/SceneTransitionManager.cs:188), DDOL, tự tạo | Dùng. Farm → kitchen dùng Additive; **Hồ câu dùng Single** (scene có camera/đèn riêng, tránh 2 scene nặng cùng lúc trên mobile). Gọi `SaveSystem.Save("fishing-enter")` trước khi rời farm. |
| Manager sống qua scene | `FarmEconomyManager`, `PlayerProgressManager`, `FarmInventoryManager`, `AudioManager`, `SaveBootstrap` đều DDOL | Cộng vàng/EXP từ scene câu vẫn chạy. `AudioManager.PlayMainBGM()` tự bật mỗi lần load scene (AudioManager.cs:128) → scene câu gọi `StartWaterAmbience()`. |
| Tiền | `FarmEconomyManager.SpendGold/SpendGems/AddGold/AddGems` (Managers/FarmEconomyManager.cs:65-107) | **Không** gọi `RewardFlyFX.Fly()` sau `AddGold` (đã tự nghe event, gọi thêm = bay 2 lần). Dùng `RewardFlyFX.GoiYDiemXuatPhat(screenPos)` rồi `AddGold`. |
| EXP | `PlayerProgressManager.Instance.AddExp(int)`; công thức THẬT `40+10n+3n²/20` (Progression/PlayerProgressManager.cs:87), KHÔNG phải `n²` như MEMORY ghi | EXP câu cá = 0 mặc định (núm `expPerCatch`), EXP khi BÁN cá = round(tổng vàng/10) như quầy hàng (Stall/PlayerStallManager.cs:751). |
| Save | Mỗi hệ tự lưu PlayerPrefs (blob JSON + saveVersion trong blob) rồi `LuuGopPrefs.Hen()`; khoá phải khai vào `SaveAdapters.PrefsMirrorAdapter.StringKeys` (Save/SaveAdapters.cs:415) và họ vào `SaveVersionGuard.AllFamilies` (Managers/SaveVersionGuard.cs:91) | 2 sửa CỘNG THÊM nhỏ ở 2 file có sẵn (Lead làm, chỉ thêm dòng vào mảng). |
| Item id | `"ca"`, `"ca_nuong_tieu"`, `"canh_chua_ca"` nằm trong **danh sách xoá** của `KitchenTransferManager.DeadItemIds` và `MissionProgressTracker.DeadKeySubstrings` (EndsWith ":ca") | Mọi id cá dùng tiền tố **`fish_`** (vd `fish_ro`, `fish_chep`). Không dùng `ca_*`. |
| Shop | `ShopManager` có 3 tab cứng, `ShopItemUI.BuyItem` đổ đồ vào kho hạt giống (Shop/ShopItemUI.cs:190), tiền chọn theo `diamondPrice > 0` | **Không đụng ShopManager.** Quầy Cá riêng có 2 tab: Bán cá · Mua cần. |
| Object click ở farm | `StallWorldObject` (Stall/StallWorldObject.cs:17): Collider2D + OverlapPoint + chuỗi guard `FarmInputLock/EditMode/PopupManager` | Chép nguyên mẫu cho `FishCounterWorldObject`. |
| Popup | Mẫu ĐÚNG: `TrainLoadPopupUI.cs` (chỉ `[SerializeField]` + AddListener, không huỷ con). Canvas riêng mẫu `Canvas_TouristBoatPopup` order 400 | Popup mới trên farm nằm canvas riêng `Canvas_FishingPopup` order 410. Sprite qua `UIStandardSprites.Load()`, font qua `SkinKit.FontVo`, chữ qua `Loc.T()`. |
| HUD tab | `HudEditModeButtonSetupTool.cs` = mẫu tool thêm nút HUD idempotent, không tự save scene; nhóm tab ở `Canvas_HUD/BottomLeft_Nav_Group` (Tab_Shop/Warehouse/Market/Cooking) | Tool `FishingFarmHookSetupTool` thêm `Tab_Fishing` cùng kiểu, chép anchor/sprite từ `Tab_Cooking`. HUD đã sát mép (12/14 px) → tool chỉ THÊM, Sếp kéo vị trí. |
| Input | `TouchInput` (Core/TouchInput.cs) + `InputBridge`; `activeInputHandler = Both`; EventSystem dùng `StandaloneInputModule`. **Chưa có joystick ảo nào.** | Viết `VirtualJoystickUI` mới (UGUI IDragHandler). |
| Nhân vật 4 hướng | `NPCAnimationSetupTool.cs` (11 khách NVxx, idle = frame 2, walk 1-2-3-2-1 8fps, pivot BottomCenter, PPU 100, AnyState transitions, param `DirX/DirY/IsMoving`); `NV_01/PlayerMovement.cs` (Rigidbody2D, param `MoveX/MoveY`) | Tool mới `FishingPlayerAnimSetupTool` cho PlayerF/PlayerM. **Bảng idle theo hướng: down/up = frame 1, left/right = frame 2** (đo thật khi cắt). Param dùng `DirX/DirY/IsMoving` (theo khách du lịch). |
| Ngày/đêm | `Day_Night.DayNightCycleController` (5 Light2D, `Reset()` nạp preset, `SetTimeOfDay`, `CurrentDayRatio`), `DayNightWeatherSystem`, `DayNightRainOverlay`. Không singleton, không đồng bộ scene | Tool AddComponent + 5 Light2D. Đồng bộ giờ: `FishingSession` lưu `CurrentDayRatio` trước khi rời farm. |
| Nước / mưa toé | `RainSplashManager` (VFX/) có 3 lỗi (AssetDatabase runtime, không pool, mất frame cuối) | **Không chép.** Splash vẽ thủ tục kiểu `ConstructionCelebrationFX` (layer/order đọc từ đối tượng +100), pool kiểu `MillFxPool`. |
| Sorting layer | Chỉ có `Bottom/Default/Objects/ObjectsFront/Foreground`. `"FX"`, `"Crop"`, `"CongTrinh"` là layer ma | Mọi renderer qua `TouristSortingLayers.ResolveOrOverride(ten, TouristSortingLayers.Visitor)`. |
| Thứ tự Update | Không có `MonoManager.asset` → thứ tự Update KHÔNG xác định | Camera follow, dây câu, bubble đọc vị trí ở `LateUpdate()`. |
| Số giây | `float` cho Unix = bước nhảy 128s | `long` + `DateTimeOffset.UtcNow.ToUnixTimeSeconds()`; `ToString(CultureInfo.InvariantCulture)`. |
| Test | Không có tests/, không asmdef, UTF 1.6 có trong manifest | Logic thuần C# (state machine, resolver) viết dạng `static`/POCO không dính `Time/Random/PlayerPrefs` để test được sau; kèm menu `Tools/Farm Game/Test/Check Fishing`. Tạo asmdef test = CẦN BẠN (đụng compile toàn project). |
| Scene mới + Build Settings | **Chưa có tiền lệ** `EditorSceneManager.NewScene` / ghi `EditorBuildSettings.scenes` | Tool làm tường minh, idempotent (chỉ tạo nếu chưa có file, chỉ thêm nếu chưa có trong list). |

---

## 1. QUYẾT ĐỊNH KIẾN TRÚC (Lead chốt 07/09)

1. **Module tự chứa** ở `Assets/_Game/Fishing/` (Scripts · Editor · Art · Data · Prefabs · Animations). Scene ở `Assets/_Game/Scenes/SCN_Fishing.unity`. Namespace `FarmGame.Fishing`. Log tag `[Fishing]` (runtime) · `[FishingSetup]` (tool).
2. **Tỉ lệ world của scene câu = chuẩn Unity (1 ô iso = 1 × 0.5 unit, PPU 100)**, KHÔNG chép hệ ×150 của farm. Lý do: đèn Day_Night, mưa, nước, audio ambience đều viết cho tỉ lệ này; palette `Palette_Iso45` (PPU 128, thoi 128×64 = 1×0.5) dùng lại được để Sếp vẽ map. Chiều cao nhân vật do config (`playerWorldHeight`, mặc định 0.6 = tỉ lệ 170/300 của khách du lịch trên farm).
3. **Tầng mạng trừu tượng ngay từ vòng 1**: `IRoomService` · `IChatService` · `IFriendService` + DTO `PlayerNetState/RoomInfo/ChatMessage/FriendEntry/InviteInfo`. Vòng 1 cài `LocalRoomService/LocalChatService/LocalFriendService` (offline, có bot đi lại + câu + chat để test bubble/bạn bè/mời). Vòng online chỉ thêm `FirebaseRoomService...` và đổi `FishingNetHub`. UI **không** được gọi Firebase hay Local trực tiếp.
4. **DTO map thẳng sang Firebase RTDB**: `rooms/{roomId}/players/{uid}` = `PlayerNetState` (x, y, dir, moving, phase, bubbleText, bubbleUntilUnix, privateMode). `rooms/{roomId}/chat/{pushId}` = `ChatMessage`. Firestore: `users/{uid}/friends/{fid}` = `FriendEntry`.
5. **Vào scene câu = Single load** qua `SceneTransitionManager`, trước đó `SaveSystem.Save("fishing-enter")`. Về farm = `LoadScene("SCN_Farm", CloudWipe, Single)` (đường SCN_Home → SCN_Farm đang dùng y vậy).
6. **Đi vào lưu trữ**: `FishBasket` (giỏ cá, đếm theo LOẠI như kho farm, mặc định 20 ô) khoá `FISH_BASKET_SAVE`; `FishingGearState` (cần đang sở hữu + độ bền + cần đang cầm) khoá `FISHING_GEAR_SAVE`; nhân vật đã chọn `FISHING_PROFILE_SAVE`. Họ save `"FISHING"`.
7. **Kinh tế**: 4 cần (3 vàng + 1 gem) và 10 loài cá là **ĐỀ XUẤT** trong `FishingDataSetupTool`, Sếp duyệt/sửa số trên asset. Cá không được lời hơn bán nông sản ở chợ (đối chiếu `MarketPriceTable`). EXP câu = 0, EXP bán = vàng/10.
8. **Feature flag**: `FishingConfig.enabled` mặc định `false` trong code; tool tạo asset đặt `true`. Tắt = tab Hồ câu ẩn, quầy cá không mở, game y như cũ.
9. **Không sửa file cũ** ngoài 2 mảng khoá save (SaveAdapters, SaveVersionGuard) ; âm thanh câu cá nằm trong module (`FishingAudio.cs`, AudioSource riêng, fallback sang `AudioManager` có sẵn), KHÔNG sửa `AudioManager`. Không đụng `ShopManager`, `TownshipHUDController`, `FarmUIManager`.

---

## 2. LUỒNG NGƯỜI CHƠI (đúng mô tả Sếp)

```
FARM ── Tab "Hồ câu" (HUD) ──► FishingEntryPopupUI
                                 Bước 1: chọn PlayerF / PlayerM → OK
                                 Bước 2: danh sách phòng (10 phòng × 10 slot, hiện x/10) → Vào
                                 ▼  SaveSystem.Save · FishingSession.SelectedCharacter/Room
                             SCN_Fishing
                                 · Player spawn tại SpawnPoint, camera follow, joystick trái, nút QUĂNG/THU phải
                                 · Đi vào FishingZone (mép nước) → nút QUĂNG sáng
                                 · QUĂNG: cần vung, phao bay, splash "tủm" → Chờ (random theo cần) → CÁ CẮN (phao rung, rung máy)
                                 · THU trong cửa sổ cắn → bắt được: toast cá + vào giỏ · THU sớm/trễ → cá thoát/thu dây không
                                 · Cần trừ độ bền mỗi lần quăng, về 0 = "Cần đã hỏng", phải về farm mua
                                 · Tab Giỏ cá · Tab Bạn bè (card + Mời) · Icon chat → bàn phím → bubble trên đầu (ai gần thấy, trừ riêng tư)
                                 · Click người khác → PlayerInfoPopup: Kết bạn · Kết nối (Hẹn hò đỏ/Bạn bè xanh/Chị em vàng, vẽ line 2 đầu) · Tặng cá/gem
                                 · Nút Về farm
FARM ── Quầy Cá (object trong scene) ──► FishCounterPopupUI: tab BÁN CÁ (giỏ → vàng, EXP vàng/10) · tab CẦN CÂU (4 cần)
FARM ── FishingInviteHintUI: "A đang mời bạn vào câu cá" [Tới] → vào thẳng phòng của A
```

---

## 3. PHÂN CÔNG 4 DEV — MỖI FILE MỘT CHỦ

Lead viết trước (hợp đồng chung, KHÔNG Dev nào sửa): `Scripts/Core/*` (FishingIds, FacingDir, FishRarity, FishingPhase, RelationshipKind, FishData, RodData, FishingConfig, FishingCharacterDef, FishingDatabase, FishingSession) · `Scripts/Net/I*.cs + DTO` · `Scripts/Net/FishingNetHub.cs`.

| Dev | Domain | File sở hữu (Assets/_Game/Fishing/Scripts/…) |
|---|---|---|
| **A — Player/Camera/Input/Remote** | `Player/FishingPlayerController.cs` · `Player/VirtualJoystickUI.cs` · `Player/FishingCameraFollow.cs` · `Player/FishingYSort.cs` · `Player/RemotePlayerView.cs` · `Player/RemotePlayersManager.cs` · `Player/RelationshipLineView.cs` · `Player/PlayerNameTag.cs` |
| **B — Fishing logic/data/FX** | `Fishing/FishingStateMachine.cs` (POCO) · `Fishing/FishingCatchResolver.cs` (static) · `Fishing/FishingController.cs` · `Fishing/FishingZone.cs` · `Fishing/FishBasket.cs` · `Fishing/FishingGearState.cs` · `Fishing/FishingBobber.cs` · `Fishing/FishingRodVisual.cs` · `Fishing/FishingSplashFX.cs` · `Fishing/FishingSplashPool.cs` · `Fishing/FishingAudio.cs` |
| **C — UI (scene câu + farm)** | `UI/FishingHudUI.cs` · `UI/FishBasketPanelUI.cs` · `UI/FriendsPanelUI.cs` · `UI/FriendCardUI.cs` · `UI/ChatPanelUI.cs` · `UI/ChatBubbleUI.cs` · `UI/PlayerInfoPopupUI.cs` · `UI/FishingResultToastUI.cs` · `UI/FishingEntryPopupUI.cs` · `UI/FishCounterPopupUI.cs` · `UI/FishCounterWorldObject.cs` · `UI/FishingInviteHintUI.cs` · `UI/FishingUiKit.cs` (helper dựng UGUI bằng code: nút, card, text) |
| **D — Tools/Scene/Offline net** | `Editor/FishingSceneSetupTool.cs` (★ 1 nút) · `Editor/FishingPlayerAnimSetupTool.cs` · `Editor/FishingDataSetupTool.cs` · `Editor/FishingFarmHookSetupTool.cs` · `Editor/FishingCheckTool.cs` · `Net/LocalRoomService.cs` · `Net/LocalChatService.cs` · `Net/LocalFriendService.cs` · `Net/OfflineBotBrain.cs` |

Luật chung cho 4 Dev: đọc `production/PLAN_HO_CAU_2026-09-07.md` + toàn bộ `Scripts/Core`, `Scripts/Net/I*` trước · một top-level type / file, tên class = tên file · namespace `FarmGame.Fishing` · public API tiếng Anh, comment/log tiếng Việt · `Debug.Log` một dòng, `if` luôn có ngoặc · không `AssetDatabase` ngoài `#if UNITY_EDITOR` và không dùng nó để nạp sprite runtime · không `float` cho Unix · không đổi file của Dev khác, cần API thì ghi vào mục "YÊU CẦU LIÊN DEV" trong báo cáo · LF.

---

## 4. RỦI RO & PHÒNG NGỪA

| Rủi ro | Mức | Phòng ngừa |
|---|---|---|
| Không compile được ở đây (không có Unity) | Cao | Lead review tĩnh từng file; Sếp compile là bước 1 trong SẾP LÀM; mọi file mới đều trong `Assets/_Game/Fishing/`, xoá thư mục là game về như cũ. |
| Single load làm mất state farm chưa flush | Trung | `SaveSystem.Save("fishing-enter")` + PlayerPrefs sống trong tiến trình; `SCN_Home→SCN_Farm` đang là đường load Single chuẩn của game. |
| HUD chật, tab thứ 5 đè nút | Trung | Tool chỉ thêm nút, chép style Tab_Cooking, đặt cạnh phải, ghi rõ toạ độ vào báo cáo để Sếp kéo. Có thể ẩn tab bằng `FishingConfig.enabled`. |
| Ghost popup do 2 MonoBehaviour chung file | Cao (đã xảy ra vòng 6) | Luật 1 file 1 class, Lead grep `class .* : MonoBehaviour` đếm ≤1 mỗi file. |
| Tool ghi đè chỉnh tay của Sếp | Cao | Find-or-create, chỉ gán field đang trống, không `DestroyImmediate` object có sẵn, không tự save scene, Undo group. |
| Chat mở với trẻ em (chính sách store) | Cao (khi online) | Vòng offline chưa ảnh hưởng; ghi vào CẦN BẠN: chốt M5-1 trước khi bật Firebase chat. |

Backup: 2 file CŨ bị sửa (SaveAdapters.cs, SaveVersionGuard.cs) chép `.bak` vào `production/backup_vong12_2026-09-07/` trước khi ghi.
