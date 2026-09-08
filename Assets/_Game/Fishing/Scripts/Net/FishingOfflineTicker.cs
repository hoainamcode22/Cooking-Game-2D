using System;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// MonoBehaviour ẩn, DontDestroyOnLoad, chỉ để Update() gọi tick cho dịch vụ OFFLINE khi đang ở FARM
    /// (LocalFriendService.TickFarmSide giả lập lời mời). CHỦ FILE: Dev D (được Lead cho phép thêm file này trong Net/).
    /// Trong scene câu, RemotePlayersManager đã gọi Room.Tick nên ticker này KHÔNG tick room — chỉ phát OnTick cho ai đăng ký.
    /// Không dùng [InitializeOnLoad]; chỉ tạo khi Application.isPlaying.
    /// </summary>
    public class FishingOfflineTicker : MonoBehaviour
    {
        private const string ObjectName = "FishingOfflineTicker";

        /// <summary>Đăng ký nhận deltaTime mỗi frame.</summary>
        public static event Action<float> OnTick;

        private static FishingOfflineTicker _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            OnTick = null;
        }

        /// <summary>Tạo object ẩn nếu chưa có. Trả false khi không ở Play Mode (Editor tool không được tạo).</summary>
        public static bool Ensure()
        {
            if (_instance != null) { return true; }
            if (!Application.isPlaying) { return false; }
            var go = new GameObject(ObjectName);
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FishingOfflineTicker>();
            Debug.Log(FishingIds.LogTag + " FishingOfflineTicker đã tạo (DontDestroyOnLoad, ẩn).");
            return true;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void Update()
        {
            if (OnTick != null) { OnTick(Time.unscaledDeltaTime); }
        }

        private void OnDestroy()
        {
            if (_instance == this) { _instance = null; }
        }
    }
}
