using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Trên FishingSceneRoot. Start(): spawn nhân vật theo FishingSession.SelectedCharacterId tại PlayerSpawnPoint,
    /// gắn camera, nối giờ ngày/đêm từ farm, bật tiếng nước, vào phòng. Database thiếu vẫn chạy được (nhân vật trống + LogError).
    /// </summary>
    public class FishingSceneBootstrap : MonoBehaviour
    {
        private const string FallbackRoomId = "room_01";

        public static FishingSceneBootstrap Instance { get; private set; }
        public FishingPlayerController LocalPlayer { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }

        private void Start()
        {
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;

            // Sếp bấm Play thẳng SCN_Fishing trong Editor → chưa qua popup chọn phòng → dùng phòng mặc định để test nhanh.
            if (string.IsNullOrEmpty(FishingSession.SelectedRoomId))
            {
                FishingSession.SelectRoom(FallbackRoomId);
                Debug.LogWarning(FishingIds.LogTag + " Chưa chọn phòng (Play thẳng scene câu?) — dùng tạm " + FallbackRoomId + ".");
            }

            Vector3 spawnPos = Vector3.zero;
            var spawn = GameObject.Find(FishingIds.SpawnPointName);
            if (spawn != null) { spawnPos = spawn.transform.position; }
            else { Debug.LogWarning(FishingIds.LogTag + " Không có '" + FishingIds.SpawnPointName + "' — spawn tại (0,0)."); }
            spawnPos.z = 0f;

            LocalPlayer = SpawnLocalPlayer(spawnPos, cfg);

            // Đèn lồng theo người chơi (Light2D Point) — bật/tắt và số đo lấy từ FishingConfig.
            if (cfg != null && cfg.playerLanternEnabled && LocalPlayer != null)
            {
                PlayerLanternLight.Attach(LocalPlayer.transform, cfg);
            }

            var camFollow = FindFirstObjectByType<FishingCameraFollow>();
            if (camFollow != null && LocalPlayer != null)
            {
                camFollow.Target = LocalPlayer.transform;
                camFollow.SnapToTarget();
            }
            else if (camFollow == null) { Debug.LogWarning(FishingIds.LogTag + " Main Camera chưa có FishingCameraFollow — camera đứng yên."); }

            // Nối giờ từ farm để trời ở hồ câu giống lúc rời đi. -1 = không biết → giữ StartingTime của scene.
            // Controller nay nằm trong prefab DayNightWeatherSetup (lấy từ farm) — vẫn tìm theo type.
            if (FishingSession.CarriedDayRatio >= 0f)
            {
                var dayNight = FindFirstObjectByType<Day_Night.DayNightCycleController>();
                if (dayNight != null) { dayNight.SetTimeOfDay(FishingSession.CarriedDayRatio); }
            }

            // AudioManager DDOL tự PlayMainBGM mỗi lần load scene; chỉ cần thêm tiếng nước.
            if (AudioManager.Instance != null) { AudioManager.Instance.StartWaterAmbience(); }

            var remote = RemotePlayersManager.Instance;
            if (remote != null) { remote.JoinConfiguredRoom(); }
            else { Debug.LogWarning(FishingIds.LogTag + " Không có RemotePlayersManager trong scene — chơi một mình."); }
        }

        private FishingPlayerController SpawnLocalPlayer(Vector3 pos, FishingConfig cfg)
        {
            FishingDatabase db = FishingDatabase.Instance;
            FishingCharacterDef def = db != null ? db.FindCharacter(FishingSession.SelectedCharacterId) : null;
            GameObject prefab = def != null ? def.prefab : null;

            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, Quaternion.identity);
                go.name = "Player_Local";
            }
            else
            {
                // Không có database/prefab: vẫn dựng một nhân vật trống để scene chạy, camera/joystick/net test được.
                go = new GameObject("Player_Local_Fallback");
                go.transform.position = pos;
                go.AddComponent<SpriteRenderer>();
                Debug.LogError(FishingIds.LogTag + " Thiếu FishingDatabase hoặc prefab nhân vật '" + FishingSession.SelectedCharacterId + "' — chạy Tools/Farm Game/Hồ Câu/★ SETUP TẤT CẢ rồi Play lại.");
            }

            var ctrl = go.GetComponent<FishingPlayerController>();
            if (ctrl == null) { ctrl = go.AddComponent<FishingPlayerController>(); }
            ctrl.Initialize(def, cfg);
            return ctrl;
        }
    }
}
