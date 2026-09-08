using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Trạng thái phiên đi câu, sống qua scene (static). CHỦ FILE: Lead.
    /// Farm gọi EnterFishingScene() sau khi chọn nhân vật + phòng; scene câu gọi ReturnToFarm().
    /// Nhân vật đã chọn được nhớ vào PlayerPrefs (FISHING_PROFILE_SAVE) để lần sau mặc định.
    /// </summary>
    public static class FishingSession
    {
        public static string SelectedCharacterId { get; private set; } = FishingIds.CharacterF;
        public static string SelectedRoomId { get; private set; }
        /// <summary>Phòng do lời mời trỏ tới; FishingEntryPopupUI đọc để nhảy thẳng vào phòng bạn.</summary>
        public static string PendingInviteRoomId { get; set; }
        /// <summary>Giờ trong ngày (0..1) đọc từ Day_Night của farm trước khi rời, để scene câu nối tiếp. -1 = không biết.</summary>
        public static float CarriedDayRatio { get; private set; } = -1f;
        public static bool IsInFishingScene => SceneManager.GetActiveScene().name == FishingIds.FishingSceneName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Enter Play Mode Options giữ static giữa các lần Play ⇒ phải reset tay.
            SelectedRoomId = null; PendingInviteRoomId = null; CarriedDayRatio = -1f;
            SelectedCharacterId = PlayerPrefs.GetString(FishingIds.PrefsProfile, FishingIds.CharacterF);
            if (SelectedCharacterId != FishingIds.CharacterF && SelectedCharacterId != FishingIds.CharacterM) { SelectedCharacterId = FishingIds.CharacterF; }
        }

        public static void SelectCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) { return; }
            SelectedCharacterId = characterId;
            PlayerPrefs.SetString(FishingIds.PrefsProfile, characterId);
            LuuGopPrefs.Hen();
        }

        public static void SelectRoom(string roomId) { SelectedRoomId = roomId; }

        /// <summary>
        /// Rời farm sang scene câu: lưu save.json, nhớ giờ ngày/đêm, load Single qua SceneTransitionManager.
        /// Trả false nếu hệ tắt hoặc chưa chọn phòng.
        /// </summary>
        public static bool EnterFishingScene()
        {
            if (!FishingDatabase.IsEnabled) { Debug.LogWarning(FishingIds.LogTag + " EnterFishingScene bị chặn: FishingDatabase chưa bật."); return false; }
            if (string.IsNullOrEmpty(SelectedRoomId)) { Debug.LogWarning(FishingIds.LogTag + " EnterFishingScene: chưa chọn phòng."); return false; }
            var dayNight = Object.FindFirstObjectByType<Day_Night.DayNightCycleController>();
            CarriedDayRatio = dayNight != null ? dayNight.CurrentDayRatio : -1f;
            try { SaveSystem.Save("fishing-enter"); } catch (System.Exception e) { Debug.LogWarning(FishingIds.LogTag + " SaveSystem.Save lỗi (vẫn đi tiếp): " + e.Message); }
            Debug.Log(FishingIds.LogTag + " Vào hồ câu: nhân vật=" + SelectedCharacterId + " phòng=" + SelectedRoomId + " giờ=" + CarriedDayRatio.ToString("0.00", CultureInfo.InvariantCulture));
            LoadScene(FishingIds.FishingSceneName);
            return true;
        }

        /// <summary>Về farm (Single). Scene câu tự nhớ giờ để lần sau vào farm không cần đồng bộ ngược (farm có đồng hồ riêng).</summary>
        public static void ReturnToFarm()
        {
            var dayNight = Object.FindFirstObjectByType<Day_Night.DayNightCycleController>();
            CarriedDayRatio = dayNight != null ? dayNight.CurrentDayRatio : -1f;
            try { SaveSystem.Save("fishing-exit"); } catch (System.Exception e) { Debug.LogWarning(FishingIds.LogTag + " SaveSystem.Save lỗi (vẫn đi tiếp): " + e.Message); }
            Debug.Log(FishingIds.LogTag + " Về farm.");
            LoadScene(FishingIds.FarmSceneName);
        }

        private static void LoadScene(string sceneName)
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadScene(sceneName, SceneTransitionManager.TransitionType.CloudWipe, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning(FishingIds.LogTag + " Không có SceneTransitionManager, load thẳng.");
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }
    }
}
