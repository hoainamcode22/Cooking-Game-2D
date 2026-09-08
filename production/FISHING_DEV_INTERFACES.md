# HỢP ĐỒNG API GIỮA 4 DEV HỒ CÂU (Lead chốt — phải cài ĐÚNG tên/chữ ký, ai cần đổi thì ghi "YÊU CẦU LIÊN DEV", không tự đổi)

Tất cả trong `namespace FarmGame.Fishing`. Mỗi type một file, tên file = tên type. Đường dẫn gốc `Assets/_Game/Fishing/Scripts/`.

## Dev A — Player/ (chủ: Player, camera, joystick, người chơi khác, bootstrap scene)

```csharp
public class FishingPlayerController : MonoBehaviour {
    public static FishingPlayerController Local { get; private set; }
    public FacingDir Facing { get; }
    public bool IsMoving { get; }
    public Vector2 Position { get; }                 // transform.position
    public Transform HeadAnchor { get; }             // điểm trên đầu: bubble, line, name tag (con "HeadAnchor")
    public Transform HandAnchor { get; }             // điểm tay cầm cần (con "HandAnchor"), Dev B nối dây từ đây
    public void Initialize(FishingCharacterDef def, FishingConfig cfg);   // Bootstrap gọi ngay sau Instantiate prefab
    public void SetMovementLocked(bool locked);      // Dev B khoá khi đang câu (Casting..Result)
    public void SetFishingPhase(FishingPhase phase); // Dev B báo mỗi lần đổi pha → Animator bool IsFishing, dừng walk
    public event Action<FacingDir> OnFacingChanged;
}
public class VirtualJoystickUI : MonoBehaviour {     // UGUI, nằm trong Canvas_FishingHUD
    public static VirtualJoystickUI Instance { get; }
    public Vector2 Direction { get; }                 // đã trừ dead-zone, độ dài 0..1, (0,0) khi thả
    public bool IsActive { get; }
    public void BuildIfEmpty();                       // dựng nền + núm nếu chưa có con (tool + runtime), không huỷ con có sẵn
}
public class FishingCameraFollow : MonoBehaviour {   // trên Main Camera
    public Transform Target { get; set; }
    public void SnapToTarget();
}
public class FishingSceneBootstrap : MonoBehaviour { // trên FishingSceneRoot
    public static FishingSceneBootstrap Instance { get; }
    public FishingPlayerController LocalPlayer { get; }
    // Start(): spawn prefab theo FishingSession.SelectedCharacterId tại PlayerSpawnPoint (scale theo cfg.playerWorldHeight),
    // camera Target, DayNight.SetTimeOfDay(FishingSession.CarriedDayRatio nếu >= 0), AudioManager.StartWaterAmbience(),
    // RemotePlayersManager.Instance.JoinConfiguredRoom(). Nếu FishingDatabase null → LogError + nút về farm vẫn hoạt động.
}
public class RemotePlayersManager : MonoBehaviour {  // trên RemotePlayers
    public static RemotePlayersManager Instance { get; }
    public IReadOnlyList<RemotePlayerView> All { get; }
    public RemotePlayerView Find(string playerId);
    public event Action<RemotePlayerView> OnRemoteTapped;   // người chơi chạm vào người khác → Dev C mở PlayerInfoPopupUI
    public PlayerNetState LocalState { get; }               // state của mình đang đẩy lên (đọc-only cho UI)
    public void JoinConfiguredRoom();                       // dùng FishingSession.SelectedRoomId; gọi Room.ConfigureLocalSimulation trước
    public void SetLocalBubble(string text, float seconds); // Dev C gọi khi gửi chat → vào LocalState.bubbleText/bubbleUntilUnix
    public void SetPrivateMode(bool on);                    // đồng bộ vào LocalState.privateMode
}
public class RemotePlayerView : MonoBehaviour {
    public string PlayerId { get; }
    public PlayerNetState State { get; }              // bản copy mới nhất
    public Transform HeadAnchor { get; }
    // Tự gọi ChatBubbleUI.Show(HeadAnchor, text, seconds, isLocal:false) khi bubbleText đổi và (không privateMode hoặc là bạn) và trong bubbleVisibleDistance
}
// Nội bộ A: PlayerNameTag, FishingYSort, RelationshipLineView (LineRenderer từ Local.HeadAnchor tới HeadAnchor của bạn có relation != None, màu cfg.LineColorFor)
```

## Dev B — Fishing/ (chủ: logic câu, data runtime, giỏ, cần, FX, âm)

```csharp
public class FishingController : MonoBehaviour {     // trên object "FishingController" trong scene
    public static FishingController Local { get; private set; }
    public FishingPhase Phase { get; }
    public bool CanCast { get; }                      // Idle && HasUsableRod && gần FishingZone trong cfg.castRangeFromZone
    public string BlockReasonVi { get; }              // "Đến gần mép nước để quăng", "Chưa có cần câu, mua ở Quầy Cá", "Cần đã hỏng"
    public void Cast();                               // nếu !CanCast → OnBlocked(BlockReasonVi)
    public void Reel();                               // Waiting → thu không; Bite → xác suất bắt; ngoài ra bỏ qua
    public event Action<FishingPhase> OnPhaseChanged;
    public event Action<FishData, float> OnCatch;     // cá + kg
    public event Action OnEscape;                     // thu trễ / xui
    public event Action OnNothing;                    // thu sớm
    public event Action OnRodBroken;
    public event Action<string> OnBlocked;
    public float BiteWindowRemaining01 { get; }       // 1→0 trong pha Bite (HUD vẽ vòng)
    public void EnsureVisuals();                      // tạo con FishingBobber + FishingRodVisual nếu thiếu (tool + runtime)
}
public class FishBasket {                             // plain C#, KHÔNG MonoBehaviour, lưu PlayerPrefs FISH_BASKET_SAVE
    public static FishBasket Instance { get; }        // lazy load
    public int SlotCapacity { get; }  public int UsedSlots { get; }  public bool IsFull { get; }  public int TotalCount { get; }
    public IReadOnlyList<FishStack> Items { get; }    // FishStack { public string fishId; public int amount; } file riêng
    public bool CanAdd(string fishId);
    public bool TryAdd(string fishId, int amount, out string reasonVi);
    public int Count(string fishId);
    public bool Remove(string fishId, int amount);
    public void ClearAll();
    public event Action OnChanged;
    public void Save();
}
public class FishingGearState {                       // plain C#, lưu PlayerPrefs FISHING_GEAR_SAVE
    public static FishingGearState Instance { get; }
    public RodData EquippedRod { get; }               // null = chưa có
    public int EquippedDurabilityLeft { get; }
    public bool HasUsableRod { get; }
    public IReadOnlyList<OwnedRod> Owned { get; }     // OwnedRod { public string rodItemId; public int durabilityLeft; } file riêng
    public bool CanAfford(RodData rod);
    public bool TryBuy(RodData rod, out string reasonVi);   // trừ tiền qua FarmEconomyManager (gem nếu IsGemRod), thêm + trang bị
    public bool Equip(string rodItemId);
    public bool ConsumeCast();                        // -1 độ bền; trả false nếu vừa hỏng (và bỏ cần khỏi Owned)
    public event Action OnChanged;
    public void Save();
}
public class FishingZone : MonoBehaviour {            // Collider2D (Polygon/Box) isTrigger, trên FishingZones/Zone_xx
    public static IReadOnlyList<FishingZone> All { get; }
    public static bool TryGetNearest(Vector2 pos, out FishingZone zone, out Vector2 closestPoint, out float distance);
    public bool Contains(Vector2 worldPos);
    public Vector2 ClosestPoint(Vector2 worldPos);
    public Vector2 RandomPointInside();               // bot/phao
}
public static class FishingAudio {                    // AudioSource riêng, clip Resources/Audio/Fishing/*, fallback AudioManager có sẵn
    public static void PlayCast(); PlaySplash(); PlayBite(); PlayReel(); PlayCatch(); PlayFail();
}
// Nội bộ B: FishingStateMachine (POCO, Tick(dt), input Cast/Reel, random roll truyền vào), FishingCatchResolver (static: PickFish(db, rod, level, roll01)),
// FishingBobber, FishingRodVisual (LineRenderer HandAnchor→phao), FishingSplashFX, FishingSplashPool
```

## Dev C — UI/ (chủ: toàn bộ UGUI ở scene câu và ở farm)

```csharp
public class FishingHudUI : MonoBehaviour {          // trên Canvas_FishingHUD
    public static FishingHudUI Instance { get; }
    public void BuildIfEmpty();                       // dựng: avatar góc trái, VirtualJoystickUI (gọi BuildIfEmpty của nó) trái-dưới, nút QUĂNG + THU phải-dưới,
                                                      // cột tab phải: Giỏ cá · Bạn bè · Chat(icon) · Riêng tư · Về farm; các panel con ẩn
    public void ShowToast(string vi);
}
public class ChatBubbleUI : MonoBehaviour {          // bubble world-space trên đầu, tự huỷ/ẩn sau seconds, pool nhẹ
    public static void Show(Transform headAnchor, string text, float seconds, bool isLocal);
    public static void Hide(Transform headAnchor);
}
public class PlayerInfoPopupUI : MonoBehaviour {     // thông tin người khác: Kết bạn / Huỷ bạn · Kết nối (Bạn bè/Chị em/Hẹn hò) · Tặng cá / Tặng gem · Mời
    public static void Open(RemotePlayerView target);
    public static void Close();
}
public class FishingEntryPopupUI : MonoBehaviour {   // FARM, dưới Canvas_FishingPopup; bước 1 chọn nhân vật, bước 2 chọn phòng
    public static void Open();
    public static void OpenForInvite(InviteInfo invite);   // nhảy thẳng bước 2 với phòng đã chọn
    public bool IsOpen { get; }
    public void BuildIfEmpty();
}
public class FishCounterPopupUI : MonoBehaviour {    // FARM, dưới Canvas_FishingPopup; tab BÁN CÁ · tab CẦN CÂU
    public static void Open();
    public bool IsOpen { get; }
    public void BuildIfEmpty();
}
public class FishCounterWorldObject : MonoBehaviour { public int requiredLevel; }   // FARM, trên object FishCounter có Collider2D, mẫu StallWorldObject
public class FishingInviteHintUI : MonoBehaviour { public void BuildIfEmpty(); }    // FARM, dưới Canvas_FishingPopup, nghe Friends.OnInviteReceived, nút "Tới"
public class FishingHudTabButton : MonoBehaviour { }  // FARM, gắn trên Tab_Fishing (Button): ẩn nếu !FishingDatabase.IsEnabled || level < cfg.unlockLevel; onClick → FishingEntryPopupUI.Open()
public static class FishingUiKit { /* helper dựng UGUI: Panel, Button(text), Label, Card, Icon, ScrollList... dùng UIStandardSprites + SkinKit.FontVo */ }
// Nội bộ C: FishBasketPanelUI, FriendsPanelUI, FriendCardUI, ChatPanelUI (TMP_InputField → bàn phím mobile; gửi qua FishingNetHub.Chat.Send + RemotePlayersManager.SetLocalBubble + ChatBubbleUI.Show local), FishingResultToastUI
```

## Dev D — Editor/ + Net/ (chủ: tool dựng, data đề xuất, dịch vụ offline)

```csharp
public class LocalRoomService : IRoomService { public LocalRoomService(); }                 // 10 phòng × 10 slot giả, bot theo cfg.offlineBotCount
public class LocalChatService : IChatService { public LocalChatService(LocalRoomService room); }   // phát lại tin mình + bot đáp ngẫu nhiên
public class LocalFriendService : IFriendService { public LocalFriendService(LocalRoomService room); } // lưu PlayerPrefs "FISHING_FRIENDS_LOCAL" (khoá offline, không cần mirror), bot chấp nhận ngay
public class OfflineBotBrain { }                                                              // đi lại trong walkArea, tới fishingSpots câu, chat câu mẫu
// Tools (static class, [MenuItem("Tools/Farm Game/Hồ Câu/...")]):
//   FishingSceneSetupTool   ★ SETUP TẤT CẢ (1 nút): gọi Data → Anim → tạo/mở SCN_Fishing → dựng hierarchy → BuildIfEmpty các UI → Build Settings → SaveScene(SCN_Fishing) → báo cáo
//   FishingDataSetupTool    tạo/cập nhật FishingConfig, 10 FishData, 4 RodData, FishingDatabase (Resources/Fishing), characters
//   FishingPlayerAnimSetupTool  importer + 8 clip + controller + prefab Player_PlayerF/M; wire vào database.characters
//   FishingFarmHookSetupTool    chạy khi đang mở SCN_Farm: Tab_Fishing (+FishingHudTabButton), Canvas_FishingPopup (order 410) + FishingEntryPopupUI/FishCounterPopupUI/FishingInviteHintUI (BuildIfEmpty), object FishCounter (+Collider2D +FishCounterWorldObject); MarkSceneDirty, KHÔNG SaveScene
//   FishingCheckTool        Tools/Farm Game/Test/Check Fishing: PASS/FAIL (database, 12+12 sprite, prefab, scene trong build settings, id fish_ , tier 1..4 đủ, không class trùng file)
```

## Tên object trong scene (FishingIds): FishingSceneRoot · Grid_Fishing · PlayerSpawnPoint · FishingZones · CameraBounds · Canvas_FishingHUD · Canvas_FishingWorld · DayNight · RemotePlayers · FishingController · Main Camera · EventSystem
