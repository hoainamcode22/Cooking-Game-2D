using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Trên object RemotePlayers. Cầu nối duy nhất giữa scene và IRoomService: vào phòng, đẩy state của mình theo netPushRate,
    /// Tick dịch vụ mỗi frame, tạo/cập nhật/huỷ RemotePlayerView theo sự kiện phòng, phát hiện chạm vào người khác.
    /// Chỉ nói chuyện qua FishingNetHub (không biết Local hay Firebase).
    /// </summary>
    public class RemotePlayersManager : MonoBehaviour
    {
        private const string FallbackRoomId = "room_01";
        private const string OfflinePlayerId = "local";
        private const string DefaultDisplayName = "Nông dân";
        private const string PrefsProfileName = "PLAYER_PROFILE_NAME";          // khoá của AvatarProfilePopupUI
        private const string PrefsProfileAvatar = "PLAYER_PROFILE_AVATAR_INDEX";
        private const float FallbackWalkHalfSize = 6f;

        public static RemotePlayersManager Instance { get; private set; }
        public IReadOnlyList<RemotePlayerView> All { get { return _views; } }
        public event Action<RemotePlayerView> OnRemoteTapped;
        public PlayerNetState LocalState { get; private set; }

        private readonly List<RemotePlayerView> _views = new List<RemotePlayerView>();
        private readonly Dictionary<string, RemotePlayerView> _byId = new Dictionary<string, RemotePlayerView>();
        private FishingConfig _cfg;
        private IRoomService _room;      // giữ đúng instance đã đăng ký để huỷ đăng ký
        private bool _joined;
        private float _pushClock;
        private Camera _cam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            Instance = this;
            _cfg = FishingDatabase.ConfigOrDefault;
            LocalState = new PlayerNetState { playerId = OfflinePlayerId, displayName = DefaultDisplayName, level = 1 };
        }

        private void OnEnable()
        {
            _room = FishingNetHub.Room;
            if (_room == null) { return; }
            _room.OnPlayerJoined += HandleJoined;
            _room.OnPlayerUpdated += HandleUpdated;
            _room.OnPlayerLeft += HandleLeft;
        }

        private void OnDisable()
        {
            if (_room == null) { return; }
            _room.OnPlayerJoined -= HandleJoined;
            _room.OnPlayerUpdated -= HandleUpdated;
            _room.OnPlayerLeft -= HandleLeft;
        }

        private void OnDestroy()
        {
            if (_room != null && _joined) { _room.LeaveRoom(); }
            _joined = false;
            if (Instance == this) { Instance = null; }
        }

        public RemotePlayerView Find(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) { return null; }
            RemotePlayerView v;
            return _byId.TryGetValue(playerId, out v) ? v : null;
        }

        /// <summary>Vào phòng FishingSession.SelectedRoomId: báo vùng đi lại + điểm câu cho bot trước, rồi JoinRoom.</summary>
        public void JoinConfiguredRoom()
        {
            if (_room == null) { _room = FishingNetHub.Room; }
            if (_room == null) { Debug.LogWarning(FishingIds.LogTag + " Không có IRoomService — bỏ qua vào phòng."); return; }
            if (_joined) { Debug.Log(FishingIds.LogTag + " Đã ở trong phòng " + _room.CurrentRoomId + ", bỏ qua JoinConfiguredRoom lần 2."); return; }

            string roomId = FishingSession.SelectedRoomId;
            if (string.IsNullOrEmpty(roomId))
            {
                roomId = FallbackRoomId;
                Debug.LogWarning(FishingIds.LogTag + " SelectedRoomId rỗng — dùng tạm " + roomId + ".");
            }

            BuildLocalStateIdentity();
            RefreshLocalStateDynamic();

            _room.ConfigureLocalSimulation(ComputeWalkArea(), CollectFishingSpots());
            _room.JoinRoom(roomId, LocalState.Clone(), HandleJoinResult);
        }

        /// <summary>Dev C gọi khi gửi chat: bubble hiện trên đầu mình cho người khác thấy.</summary>
        public void SetLocalBubble(string text, float seconds)
        {
            LocalState.bubbleText = text ?? string.Empty;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            LocalState.bubbleUntilUnix = string.IsNullOrEmpty(text) ? 0 : now + Mathf.Max(1, Mathf.CeilToInt(seconds));
            PushNow();
        }

        /// <summary>Chế độ riêng tư: người lạ không thấy bubble của mình. Đồng bộ cả sang IFriendService.</summary>
        public void SetPrivateMode(bool on)
        {
            LocalState.privateMode = on;
            var friends = FishingNetHub.Friends;
            if (friends != null && friends.PrivateMode != on) { friends.PrivateMode = on; }
            PushNow();
        }

        private void Update()
        {
            if (_room == null) { return; }
            _room.Tick(Time.deltaTime);

            if (_joined)
            {
                // Đồng hồ tích luỹ: đẩy đúng netPushRate lần/giây bất kể FPS.
                float rate = _cfg != null ? Mathf.Max(1f, _cfg.netPushRate) : 5f;
                _pushClock += Time.deltaTime;
                if (_pushClock >= 1f / rate)
                {
                    _pushClock = 0f;
                    PushNow();
                }
            }

            DetectTap();
        }

        // ── Local state ──

        private void BuildLocalStateIdentity()
        {
            string id = _room != null ? _room.LocalPlayerId : null;
            LocalState.playerId = string.IsNullOrEmpty(id) ? OfflinePlayerId : id;
            string n = PlayerPrefs.GetString(PrefsProfileName, DefaultDisplayName);
            LocalState.displayName = string.IsNullOrEmpty(n) ? DefaultDisplayName : n;
            LocalState.avatarIndex = PlayerPrefs.GetInt(PrefsProfileAvatar, 0);
            LocalState.level = PlayerProgressManager.Instance != null ? Mathf.Max(1, PlayerProgressManager.Instance.Level) : 1;
            LocalState.characterId = FishingSession.SelectedCharacterId;
            LocalState.isBot = false;
            var friends = FishingNetHub.Friends;
            if (friends != null) { LocalState.privateMode = friends.PrivateMode; }
        }

        private void RefreshLocalStateDynamic()
        {
            var local = FishingPlayerController.Local;
            if (local != null)
            {
                Vector2 p = local.Position;
                LocalState.x = p.x; LocalState.y = p.y;
                LocalState.dir = (int)local.Facing;
                LocalState.moving = local.IsMoving;
            }
            var fishing = FishingController.Local;
            LocalState.phase = (int)(fishing != null ? fishing.Phase : FishingPhase.Idle);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            LocalState.lastSeenUnix = now;
            // Bubble hết hạn thì xoá để người vào sau không thấy câu cũ.
            if (!string.IsNullOrEmpty(LocalState.bubbleText) && LocalState.bubbleUntilUnix <= now) { LocalState.bubbleText = string.Empty; LocalState.bubbleUntilUnix = 0; }
        }

        private void PushNow()
        {
            if (_room == null || !_joined) { return; }
            RefreshLocalStateDynamic();
            _room.PushLocalState(LocalState.Clone());
        }

        // ── Cấu hình mô phỏng offline ──

        private Rect ComputeWalkArea()
        {
            var go = GameObject.Find(FishingIds.CameraBoundsName);
            var box = go != null ? go.GetComponent<BoxCollider2D>() : null;
            if (box != null)
            {
                Bounds b = box.bounds;
                return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            }
            Vector2 c = Vector2.zero;
            if (FishingPlayerController.Local != null) { c = FishingPlayerController.Local.Position; }
            else
            {
                var spawn = GameObject.Find(FishingIds.SpawnPointName);
                if (spawn != null) { c = spawn.transform.position; }
            }
            return new Rect(c.x - FallbackWalkHalfSize, c.y - FallbackWalkHalfSize, FallbackWalkHalfSize * 2f, FallbackWalkHalfSize * 2f);
        }

        private static List<Vector2> CollectFishingSpots()
        {
            var spots = new List<Vector2>();
            IReadOnlyList<FishingZone> zones = FishingZone.All;
            if (zones == null) { return spots; }
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] == null) { continue; }
                spots.Add(zones[i].RandomPointInside());   // 2 điểm/zone để bot không đứng chồng nhau
                spots.Add(zones[i].RandomPointInside());
            }
            return spots;
        }

        // ── Sự kiện phòng ──

        private void HandleJoinResult(bool ok, string errorVi)
        {
            if (!ok)
            {
                Debug.LogWarning(FishingIds.LogTag + " Vào phòng thất bại: " + errorVi);
                return;
            }
            _joined = true;
            _pushClock = 0f;
            // Người đã ở trong phòng trước mình: dịch vụ có thể không bắn OnPlayerJoined → tự đồng bộ snapshot (idempotent).
            var snapshot = _room.GetRemotePlayers();
            if (snapshot != null)
            {
                for (int i = 0; i < snapshot.Count; i++) { HandleJoined(snapshot[i]); }
            }
            Debug.Log(FishingIds.LogTag + " Đã vào phòng " + _room.CurrentRoomId + " với " + _views.Count + " người khác.");
        }

        private void HandleJoined(PlayerNetState s)
        {
            if (s == null || string.IsNullOrEmpty(s.playerId)) { return; }
            if (s.playerId == LocalState.playerId) { return; }
            var existing = Find(s.playerId);
            if (existing != null) { existing.ApplyState(s); return; }
            CreateView(s);
        }

        private void HandleUpdated(PlayerNetState s)
        {
            if (s == null || string.IsNullOrEmpty(s.playerId) || s.playerId == LocalState.playerId) { return; }
            var v = Find(s.playerId);
            if (v == null) { CreateView(s); return; }
            v.ApplyState(s);
        }

        private void HandleLeft(string playerId)
        {
            var v = Find(playerId);
            if (v == null) { return; }
            _byId.Remove(playerId);
            _views.Remove(v);
            Destroy(v.gameObject);
        }

        private void CreateView(PlayerNetState s)
        {
            // RemotePlayerView phải có TRƯỚC khi Instantiate prefab: FishingPlayerController trong prefab kiểm cha để tự tắt.
            var holder = new GameObject("Remote_" + s.playerId);
            holder.transform.SetParent(transform, false);
            holder.transform.position = new Vector3(s.x, s.y, 0f);
            var view = holder.AddComponent<RemotePlayerView>();

            GameObject visual = null;
            var db = FishingDatabase.Instance;
            var def = db != null ? db.FindCharacter(s.characterId) : null;
            if (def != null && def.prefab != null) { visual = Instantiate(def.prefab, holder.transform); }

            view.Setup(s, _cfg, visual);
            _views.Add(view);
            _byId[s.playerId] = view;
        }

        // ── Chạm vào người khác ──

        private void DetectTap()
        {
            if (_views.Count == 0) { return; }
            if (!TouchInput.TapDownThisFrame() || InputBridge.IsPointerOverUI()) { return; }
            if (_cam == null) { _cam = Camera.main; }
            Vector2 w = TouchInput.PointerWorld(_cam);

            RemotePlayerView hit = null;
            float best = float.MaxValue;
            for (int i = 0; i < _views.Count; i++)
            {
                var v = _views[i];
                if (v == null || v.TapCollider == null || !v.TapCollider.OverlapPoint(w)) { continue; }
                // Nhiều người chồng nhau → ưu tiên người đứng thấp hơn (đang vẽ phía trước).
                float y = v.transform.position.y;
                if (y < best) { best = y; hit = v; }
            }
            if (hit == null) { return; }
            OnRemoteTapped?.Invoke(hit);
        }
    }
}
