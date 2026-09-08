# FACTS CHO 4 DEV HỒ CÂU — API có sẵn phải dùng (đường dẫn tương đối /root/work/proj)

Đọc file nguồn khi cần ký hiệu chính xác. KHÔNG sửa file nào ngoài thư mục mình sở hữu.

## Hợp đồng chung (Lead viết, đọc hết trước khi code)
- `Assets/_Game/Fishing/Scripts/Core/*.cs` — FishingIds, FacingDir, FishRarity, FishingPhase, RelationshipKind, FishData, RodData, FishingCharacterDef, FishingConfig, FishingDatabase, FishingSession.
- `Assets/_Game/Fishing/Scripts/Net/*.cs` — PlayerNetState, RoomInfo, ChatMessage, FriendEntry, InviteInfo, GiftKind, IRoomService, IChatService, IFriendService, FishingNetHub.
- Kế hoạch: `production/PLAN_HO_CAU_2026-09-07.md`.

## Scene / load
- `SceneTransitionManager.Instance.LoadScene(string, SceneTransitionManager.TransitionType.CloudWipe, LoadSceneMode)` — `Assets/_Game/Farm/Scripts/Core/SceneTransitionManager.cs:188`. Đã bọc trong `FishingSession.EnterFishingScene()/ReturnToFarm()`.
- `SaveSystem.Save(string reason)` — `Assets/_Game/Farm/Scripts/Save/SaveSystem.cs:46`.
- Singleton tự tạo kiểu dự án: `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` để reset static (Enter Play Mode Options giữ static). Xem `Assets/_Game/Farm/Scripts/Save/SaveBootstrap.cs:46-58`.
- `AudioManager.Instance` (`Assets/_Game/Audio/AudioManager.cs`): `PlayUIClick() PlayCoinReward() PlayBuySell() PlaySuccess() PlayBubblePop() PlayGemSparkle() PlayCoinTing() PlayHarvest() StartWaterAmbience() PlayMainBGM()`. Không có "play by key". KHÔNG sửa file này.

## Tiền / EXP / save
- `FarmEconomyManager.Instance` (`Assets/_Game/Farm/Scripts/Managers/FarmEconomyManager.cs`): `int Gold`, `int Gems`, `bool SpendGold(int)`, `bool SpendGems(int)`, `void AddGold(int)`, `void AddGems(int)`, `event Action<int,int> OnCurrencyChanged`. Đã tự bắn FX bay; **không** gọi `RewardFlyFX.Fly()` thêm. Muốn FX bay từ vị trí quầy: `RewardFlyFX.GoiYDiemXuatPhat(Vector2 screenPos, float thoiHanGiay = 0.5f)` rồi `AddGold`.
- `PlayerProgressManager.Instance` (`Assets/_Game/Scripts/Progression/PlayerProgressManager.cs`): `int Level`, `void AddExp(int)`, `event Action<int> OnLevelChanged`. Mọi Instance đều có thể null ở scene lạ → null-check.
- PlayerPrefs blob JSON: mẫu `Assets/_Game/Farm/Scripts/Managers/FarmInventoryManager.cs:238-249` (`JsonUtility.ToJson(dto)`, `PlayerPrefs.SetString(key, json)`, `LuuGopPrefs.Hen()`). `LuuGopPrefs.Hen()` = `Assets/_Game/Farm/Scripts/Managers/LuuGopPrefs.cs:61` (gộp PlayerPrefs.Save 2s). Version: `SaveVersionGuard.Ensure(string family, int currentVersion, Action<int,int> migrate = null, bool hasExistingSave = true)` — họ `FishingIds.SaveFamily` đã được Lead thêm vào `AllFamilies`; 3 khoá đã thêm vào `SaveAdapters.StringKeys`.
- Số: `long` Unix qua `DateTimeOffset.UtcNow.ToUnixTimeSeconds()`; mọi `ToString/Parse` số dùng `CultureInfo.InvariantCulture`. JsonUtility chỉ serialize field public + List (không Dictionary).

## Input / UI
- `TouchInput` (static, `Assets/_Game/Farm/Scripts/Core/TouchInput.cs`): `bool HasTouchscreen`, `bool TapDownThisFrame()`, `bool TapUpThisFrame()`, `bool IsHolding()`, `Vector2 PointerScreen()`, `Vector2 PointerWorld(Camera cam = null)`. `InputBridge.IsPointerOverUI()` (`Managers/InputBridge.cs:83`). `activeInputHandler = Both`; EventSystem dùng `StandaloneInputModule`. Bàn phím test trong Editor: `Input.GetKey(KeyCode.W)` v.v. chấp nhận (legacy vẫn bật).
- Click object world ở farm: chép `Assets/_Game/Farm/Scripts/Stall/StallWorldObject.cs` (Collider2D.OverlapPoint, chuỗi guard `FarmInputLock.BlockWorldInteraction`, `EditModeManager.IsEditMode`, `PopupManager.Instance.IsAnyPopupOpen()`, `FarmInputLock.BlockMapPan`, `requiredLevel`). Popup farm đăng ký `FarmInputLock.RegisterPopupOpen()/RegisterPopupClose()` PHẢI cân (mở 1 = đóng 1), xem `BuildingProcessPopupUI.cs:31` cờ `_inputLockHeld`.
- Sprite UI: `UIStandardSprites.Load(path)` + accessor (`Assets/_Game/Scripts/UI/UIStandardSprites.cs`): `Close`(thanh đỏ TRƠN, X là TMP con), `BtnGreen BtnGray BtnPaper BtnGem IconGem IconGold FrameWood PanelPaper Ribbon RowDark BtnGreen3D BtnYellow3D BarTrack BarFill CheckBadge CardOuter CardInner SlotNormal SlotSelected AvatarBase ObCheck ObTrash`; `CloseSize=(64,64)`, `CloseGlyphSize=26f`. Kiểm `sprite.border != Vector4.zero` trước khi dùng `Image.Type.Sliced`. **Cấm** `AssetDatabase` để nạp sprite runtime.
- Font: `SkinKit.FontVo` (`Assets/_Game/Scripts/Mission/SkinKit.cs:37`) → TMP_FontAsset Baloo2 (line height 1.6× size, rộng hơn Liberation). Chữ: `Loc.T("Tiếng Việt")` (`Assets/_Game/Scripts/Localization/LocalizationManager.cs:171`).
- Mẫu popup ĐÚNG: `Assets/Export_Train_UI_Package/Scripts/TrainLoadPopupUI.cs` (chỉ SerializeField + AddListener, `GetComponent ?? AddComponent`, trùng thì SetActive(false) không Destroy). Popup dùng canvas riêng: `Assets/_Game/Farm/Editor/TouristBoatUIPopupSetupTool.cs:569-597`.
- Canvas order đang dùng ở farm: HUD 100 · Canvas_Popup 300 · Canvas_TouristBoatPopup 400 · Train 420-425 · TutorialHand 440. Hồ câu dùng **410** (`FishingIds.FarmPopupCanvasOrder`).
- HUD farm: `Canvas_HUD/BottomLeft_Nav_Group/{Tab_Shop,Tab_Warehouse,Tab_Market,Tab_Cooking}`; mẫu tool thêm nút idempotent: `Assets/_Game/Farm/Editor/HudEditModeButtonSetupTool.cs` (không tự save scene, không ghi đè vị trí đã kéo). `UIJuiceAutoAttach` tự gắn hiệu ứng nảy 1.2× cho mọi Button → chừa lề.
- `JuicyPulseFX.Play(Transform target, float punchScale = 1.22f, float duration = 0.26f)` (`Assets/_Game/Farm/Scripts/UI/JuicyPulseFX.cs:48`).

## Nhân vật / render
- Mẫu tool animation: `Assets/_Game/Farm/Editor/NPCAnimationSetupTool.cs` (importer Single + BottomCenter + PPU 100 dòng 366-405; clip ObjectReferenceKeyframe 416-441; controller AnyState 529-611; prefab 622-691 với scale theo `TouristWorldHeight`). Bẫy `SetTextureSettings` ghi đè spriteMode: `Assets/_Game/Farm/Editor/DecorStageArtTool.cs:589-599`.
- Frame nhân vật: `Assets/_Game/Fishing/Art/Characters/{PlayerF|PlayerM}/{Char}_{down|left|right|up}_{1..3}.png`. **Idle: down/up = frame 1; left/right = frame 2.** Walk = vòng 4 khoá `[bướcA, idle, bướcB, idle]` 8 fps (bước = 2 frame còn lại).
- Hướng từ vận tốc: `TouristAgent.FaceCardinal` (`Assets/_Game/Farm/Scripts/TouristBoat/Visitors/TouristAgent.cs:803-813`) — trục mạnh hơn thắng, không flipX. Animator param `DirX/DirY` float, `IsMoving` bool (+ `IsFishing` bool cho hồ câu, chưa có clip thì chỉ dừng walk).
- Sorting: chỉ có layer `Bottom/Default/Objects/ObjectsFront/Foreground`. Dùng `TouristSortingLayers.ResolveOrOverride(string userValue, string[] priority)` với `TouristSortingLayers.Visitor` (`Assets/_Game/Farm/Scripts/TouristBoat/Visitors/TouristSortingLayers.cs:109`). Y-sort: `order = base + Mathf.Clamp(RoundToInt(-y * factor), -clamp, clamp)` (world chuẩn: factor 100, clamp 8000).
- Rigidbody2D người chơi: `Assets/NV_01/Editor/SetupPlayerNV01.cs:270-281` (gravity 0, FreezeRotation, Interpolate, CapsuleCollider2D nhỏ ở chân).
- Camera: không có follow sẵn. Mẫu bù zoom canvas world: `PlacementGhostVisualController.cs:1396-1400` (scene câu ortho cố định nên không cần).

## Ngày/đêm / thời tiết / FX
- `Day_Night.DayNightCycleController` (`Assets/Day_Night/Scripts/Runtime/DayNightCycleController.cs`): public `DayDurationInSeconds=300`, `StartingTime=0.5`, `RunInPlayMode`, `float CurrentDayRatio`, `SetTimeOfDay(float)`, `ResetToHappyHarvestDefaults()`; 5 field Light2D `DayLight NightLight AmbientLight SunRimLight MoonRimLight` + `LightsRoot`. `[ExecuteAlways]`, `Reset()` nạp preset. `DayNightWeatherSystem` (`SetRain/SetSun/ToggleRain`, `CurrentWeather`), `DayNightRainOverlay` (mesh thủ tục, tự follow camera, `SortingLayerName="Foreground"` order 260), `DayNightWeatherElement`. Light2D: `UnityEngine.Rendering.Universal.Light2D` (URP 17).
- FX vẽ thủ tục + tự huỷ, layer/order đọc từ đối tượng +100: `Assets/_Game/Farm/Scripts/FX/ConstructionCelebrationFX.cs` (`static Play(Transform, int)`, texture cache HideAndDontSave). Pool: `Assets/_Game/Farm/Scripts/MillPopup/MillFxPool.cs` (fake-null: so `== null` tường minh).
- KHÔNG chép `Assets/_Game/Farm/Scripts/VFX/RainSplashManager.cs` (AssetDatabase runtime, không pool, mất frame cuối).

## Tool Editor
- Mẫu ★ 1 nút: `Assets/_Game/Farm/Editor/TouristBoatOneClickSetup.cs` (dialog xác nhận, Undo group, progress bar, báo cáo 2 tầng, ping object, MarkSceneDirty KHÔNG SaveScene, find-or-create, chỉ gán field trống). Tạo asset: `MissionSetupTool.CreateOrUpdate` (`Assets/_Game/Farm/Editor/MissionSetupTool.cs:483-512`) + `EnsureFolder` (:527). DRY-RUN/APPLY: `DecorStageArtTool.cs:38-42`. Check menu: `Tools/Farm Game/Test/...`.
- Chưa có tiền lệ tạo scene mới / ghi `EditorBuildSettings.scenes` → làm tường minh, idempotent.
- Bẫy reimport bất đồng bộ: sau `SaveAndReimport()` trong `StartAssetEditing/StopAssetEditing`, `LoadAllAssetsAtPath` chưa thấy sub-asset (MayAnimSetupTool.cs:322).
- Ghi field private: `new SerializedObject(comp).FindProperty("ten").objectReferenceValue = x; ApplyModifiedProperties()`.
- Menu gốc dùng `Tools/Farm Game/Hồ Câu/...`.

## Luật code (bắt buộc)
1 top-level type / 1 file, tên class = tên file · namespace `FarmGame.Fishing` · `Debug.Log` 1 dòng, `if` luôn có `{}` · public API tiếng Anh, comment/log tiếng Việt, tag `[Fishing]`/`[FishingSetup]` · không `float` Unix · LF · không `AssetDatabase` ngoài `#if UNITY_EDITOR` · Update đọc trạng thái người khác → `LateUpdate` · mọi `Instance` có thể null.
