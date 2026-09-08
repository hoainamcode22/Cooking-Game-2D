namespace FarmGame.Fishing
{
    /// <summary>
    /// Hằng số dùng chung toàn module Hồ Câu. CHỦ FILE: Lead. Dev không sửa, cần thêm thì ghi "YÊU CẦU LIÊN DEV".
    /// Mọi id cá dùng tiền tố "fish_" — id "ca" đang nằm trong danh sách xoá của
    /// KitchenTransferManager.DeadItemIds và MissionProgressTracker.DeadKeySubstrings.
    /// </summary>
    public static class FishingIds
    {
        // ── Scene ──
        public const string FishingSceneName = "SCN_Fishing";
        public const string FarmSceneName = "SCN_Farm";
        public const string FishingScenePath = "Assets/_Game/Scenes/SCN_Fishing.unity";

        // ── Thư mục asset (tool dùng) ──
        public const string ModuleRoot = "Assets/_Game/Fishing";
        public const string ArtCharactersRoot = ModuleRoot + "/Art/Characters";
        public const string AnimationsRoot = ModuleRoot + "/Animations";
        public const string PrefabsRoot = ModuleRoot + "/Prefabs";
        public const string DataRoot = ModuleRoot + "/Data";
        /// <summary>Database nạp runtime qua Resources.Load(DatabaseResourcePath).</summary>
        public const string ResourcesFolder = "Assets/_Game/Resources/Fishing";
        public const string DatabaseResourcePath = "Fishing/FishingDatabase";

        // ── Nhân vật ──
        public const string CharacterF = "PlayerF";
        public const string CharacterM = "PlayerM";
        public const string PrefabPrefix = "Player_";

        // ── Animator params (theo khách du lịch NPCAnimationSetupTool, KHÔNG dùng MoveX/MoveY) ──
        public const string AnimParamDirX = "DirX";
        public const string AnimParamDirY = "DirY";
        public const string AnimParamIsMoving = "IsMoving";
        public const string AnimParamIsFishing = "IsFishing";

        // ── Save ──
        public const string SaveFamily = "FISHING";
        public const int SaveVersion = 1;
        public const string PrefsBasket = "FISH_BASKET_SAVE";
        public const string PrefsGear = "FISHING_GEAR_SAVE";
        public const string PrefsProfile = "FISHING_PROFILE_SAVE";

        // ── Tên object trong scene (tool tạo, code Find theo tên) ──
        public const string SceneRootName = "FishingSceneRoot";
        public const string GridName = "Grid_Fishing";
        public const string SpawnPointName = "PlayerSpawnPoint";
        public const string FishingZonesRoot = "FishingZones";
        public const string CameraBoundsName = "CameraBounds";
        public const string HudCanvasName = "Canvas_FishingHUD";
        public const string WorldCanvasName = "Canvas_FishingWorld";
        public const string DayNightRootName = "DayNight";
        public const string RemotePlayersRoot = "RemotePlayers";

        // ── Farm side ──
        public const string FarmPopupCanvasName = "Canvas_FishingPopup";
        public const string FarmHudTabName = "Tab_Fishing";
        public const string FarmCounterName = "FishCounter";
        public const int FarmPopupCanvasOrder = 410;

        // ── Log ──
        public const string LogTag = "[Fishing]";
        public const string SetupLogTag = "[FishingSetup]";
    }
}
