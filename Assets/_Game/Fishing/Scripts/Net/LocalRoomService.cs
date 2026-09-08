using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Dịch vụ phòng OFFLINE (giả lập). CHỦ FILE: Dev D. Constructor KHÔNG tham số (FishingNetHub.EnsureOffline gọi).
    /// - Danh sách phòng room_01..room_N ("Phòng 1..N"), capacity cfg.roomCapacity; số người giả random ổn định theo seed,
    ///   phòng cuối luôn ĐẦY để test nút "Vào" bị tắt. Phòng mình đang ở = 1 + số bot.
    /// - JoinRoom: tạo cfg.offlineBotCount bot (PlayerNetState.isBot) → OnPlayerJoined từng bot, callback ok=true cùng frame.
    /// - Tick(dt): chạy OfflineBotBrain từng bot; đổi state → OnPlayerUpdated.
    /// Public thêm cho LocalChatService / LocalFriendService: Bots, LocalState, OnLocalChatSent, OnBotChat, NotifyLocalChat, SetBotBubble.
    /// </summary>
    public class LocalRoomService : IRoomService
    {
        private const int RoomListSeed = 20260907;
        private const string BotIdPrefix = "bot_";

        private static readonly string[] BotNames = { "Lan", "Minh", "Thảo", "Huy", "Ngọc", "Bảo", "Linh", "Khang", "Mai" };

        public bool IsConnected { get { return true; } }
        public string LocalPlayerId { get { return "local"; } }
        public string CurrentRoomId { get; private set; }

        public event Action OnRoomListChanged;
        public event Action<PlayerNetState> OnPlayerJoined;
        public event Action<PlayerNetState> OnPlayerUpdated;
        public event Action<string> OnPlayerLeft;

        /// <summary>Người chơi gọi chat (LocalChatService → NotifyLocalChat) → bắn cho ai muốn nghe (bot brain, log).</summary>
        public event Action<string> OnLocalChatSent;
        /// <summary>Bot vừa nói 1 câu (đã kèm bubble). LocalChatService nối vào OnMessage.</summary>
        public event Action<ChatMessage> OnBotChat;

        /// <summary>Danh sách bot trong phòng hiện tại (tham chiếu ổn định, đổi tại chỗ).</summary>
        public IReadOnlyList<PlayerNetState> Bots { get { return _bots; } }
        /// <summary>Bản state mới nhất của mình (PushLocalState). null khi chưa vào phòng.</summary>
        public PlayerNetState LocalState { get { return _local; } }

        private readonly List<PlayerNetState> _bots = new List<PlayerNetState>();
        private readonly List<OfflineBotBrain> _brains = new List<OfflineBotBrain>();
        private readonly Dictionary<string, OfflineBotBrain> _brainById = new Dictionary<string, OfflineBotBrain>();
        private PlayerNetState _local;

        private Rect _walkArea = new Rect(-4f, -3f, 8f, 6f);
        private readonly List<Vector2> _spots = new List<Vector2> { new Vector2(0f, -1f) };
        private System.Random _rng = new System.Random(RoomListSeed);

        public LocalRoomService()
        {
            Debug.Log(FishingIds.LogTag + " LocalRoomService (OFFLINE) sẵn sàng.");
        }

        private static FishingConfig Cfg { get { return FishingDatabase.ConfigOrDefault; } }

        // ─────────────────────────────────────────────────────────────────
        //  IRoomService
        // ─────────────────────────────────────────────────────────────────

        public void RequestRoomList(Action<List<RoomInfo>> onResult)
        {
            var cfg = Cfg;
            var list = new List<RoomInfo>(cfg.roomCount);
            var rnd = new System.Random(RoomListSeed);
            for (int i = 1; i <= cfg.roomCount; i++)
            {
                string id = RoomIdFor(i);
                var info = new RoomInfo
                {
                    roomId = id,
                    displayName = "Phòng " + i.ToString(CultureInfo.InvariantCulture),
                    capacity = cfg.roomCapacity,
                };
                if (id == CurrentRoomId) { info.playerCount = 1 + _bots.Count; }
                else if (i == cfg.roomCount && cfg.roomCount > 1) { info.playerCount = cfg.roomCapacity; }   // 1 phòng đầy để test nút tắt
                else { info.playerCount = rnd.Next(0, Mathf.Max(1, cfg.roomCapacity - 1)); }
                list.Add(info);
            }
            if (onResult != null) { onResult(list); }
        }

        public void JoinRoom(string roomId, PlayerNetState self, Action<bool, string> onResult)
        {
            if (string.IsNullOrEmpty(roomId)) { Fail(onResult, "Chưa chọn phòng"); return; }
            var cfg = Cfg;
            if (IsFullTestRoom(roomId, cfg)) { Fail(onResult, "Phòng đã đầy"); return; }
            if (CurrentRoomId != null) { LeaveRoom(); }

            CurrentRoomId = roomId;
            _local = self != null ? self : new PlayerNetState { displayName = "Bạn" };
            _local.playerId = LocalPlayerId;
            _local.isBot = false;
            _local.lastSeenUnix = NowUnix();

            // Seed theo phòng để cùng phòng → cùng bộ bot (tái lập được).
            _rng = new System.Random(RoomListSeed + StableHash(roomId));
            for (int i = 0; i < cfg.offlineBotCount; i++)
            {
                var bot = MakeBot(i, cfg);
                var brain = new OfflineBotBrain(bot, cfg, new System.Random(_rng.Next()), BotChatSink);
                brain.Configure(_walkArea, _spots);
                _bots.Add(bot);
                _brains.Add(brain);
                _brainById[bot.playerId] = brain;
            }

            Debug.Log(FishingIds.LogTag + " [OFFLINE] Vào " + roomId + " với " + _bots.Count.ToString(CultureInfo.InvariantCulture) + " bot.");
            if (onResult != null) { onResult(true, null); }
            for (int i = 0; i < _bots.Count; i++)
            {
                if (OnPlayerJoined != null) { OnPlayerJoined(_bots[i]); }
            }
            if (OnRoomListChanged != null) { OnRoomListChanged(); }
        }

        public void LeaveRoom()
        {
            if (CurrentRoomId == null) { return; }
            var ids = new List<string>(_bots.Count);
            for (int i = 0; i < _bots.Count; i++) { ids.Add(_bots[i].playerId); }
            _bots.Clear();
            _brains.Clear();
            _brainById.Clear();
            string old = CurrentRoomId;
            CurrentRoomId = null;
            _local = null;
            for (int i = 0; i < ids.Count; i++)
            {
                if (OnPlayerLeft != null) { OnPlayerLeft(ids[i]); }
            }
            Debug.Log(FishingIds.LogTag + " [OFFLINE] Rời " + old + ".");
            if (OnRoomListChanged != null) { OnRoomListChanged(); }
        }

        public void PushLocalState(PlayerNetState self)
        {
            if (self == null || CurrentRoomId == null) { return; }
            if (_local == null) { _local = self.Clone(); }
            else if (!ReferenceEquals(_local, self)) { _local.CopyFrom(self); }
            _local.playerId = LocalPlayerId;
            _local.lastSeenUnix = NowUnix();
        }

        public IReadOnlyList<PlayerNetState> GetRemotePlayers()
        {
            return _bots;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentRoomId == null || _brains.Count == 0) { return; }
            long now = NowUnix();
            for (int i = 0; i < _brains.Count; i++)
            {
                bool changed;
                try { changed = _brains[i].Tick(deltaTime, now); }
                catch (Exception e) { Debug.LogWarning(FishingIds.LogTag + " Bot brain lỗi (bỏ qua frame): " + e.Message); continue; }
                if (changed && OnPlayerUpdated != null) { OnPlayerUpdated(_bots[i]); }
            }
        }

        public void ConfigureLocalSimulation(Rect walkArea, IReadOnlyList<Vector2> fishingSpots)
        {
            if (walkArea.width > 0.01f && walkArea.height > 0.01f) { _walkArea = walkArea; }
            if (fishingSpots != null && fishingSpots.Count > 0)
            {
                _spots.Clear();
                for (int i = 0; i < fishingSpots.Count; i++) { _spots.Add(fishingSpots[i]); }
            }
            for (int i = 0; i < _brains.Count; i++) { _brains[i].Configure(_walkArea, _spots); }
            Debug.Log(FishingIds.LogTag + " [OFFLINE] walkArea=" + _walkArea + " spots=" + _spots.Count.ToString(CultureInfo.InvariantCulture));
        }

        // ─────────────────────────────────────────────────────────────────
        //  API thêm cho LocalChatService / LocalFriendService
        // ─────────────────────────────────────────────────────────────────

        /// <summary>LocalChatService gọi sau khi người chơi gửi tin: 1 bot random sẽ đáp sau 1-3s.</summary>
        public void NotifyLocalChat(string text)
        {
            if (OnLocalChatSent != null) { OnLocalChatSent(text); }
            if (_brains.Count == 0) { return; }
            _brains[_rng.Next(_brains.Count)].ScheduleReply();
        }

        /// <summary>Đặt bubble cho 1 bot (bạn bè "cảm ơn", "tới ngay"...). Không có bot đó thì bỏ qua.</summary>
        public void SetBotBubble(string botId, string text)
        {
            if (string.IsNullOrEmpty(botId)) { return; }
            OfflineBotBrain brain;
            if (!_brainById.TryGetValue(botId, out brain)) { return; }
            if (brain.SetBubble(text, NowUnix()) && OnPlayerUpdated != null) { OnPlayerUpdated(brain.State); }
        }

        /// <summary>Có bot này trong phòng hiện tại không.</summary>
        public bool HasBot(string playerId)
        {
            return !string.IsNullOrEmpty(playerId) && _brainById.ContainsKey(playerId);
        }

        /// <summary>Id bot ổn định theo chỉ số (bot_01…), để bạn bè lưu từ phiên trước vẫn "gặp lại".</summary>
        public static string BotIdFor(int index)
        {
            return BotIdPrefix + (index + 1).ToString("00", CultureInfo.InvariantCulture);
        }

        public static bool IsBotId(string playerId)
        {
            return !string.IsNullOrEmpty(playerId) && playerId.StartsWith(BotIdPrefix, StringComparison.Ordinal);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Nội bộ
        // ─────────────────────────────────────────────────────────────────

        private void BotChatSink(string botId, string text)
        {
            var msg = new ChatMessage
            {
                senderId = botId,
                senderName = FindBotName(botId),
                text = text,
                unix = NowUnix(),
            };
            if (OnBotChat != null) { OnBotChat(msg); }
        }

        private string FindBotName(string botId)
        {
            for (int i = 0; i < _bots.Count; i++)
            {
                if (_bots[i].playerId == botId) { return _bots[i].displayName; }
            }
            return "Bot";
        }

        private PlayerNetState MakeBot(int index, FishingConfig cfg)
        {
            Vector2 p = new Vector2(
                _walkArea.xMin + (float)_rng.NextDouble() * _walkArea.width,
                _walkArea.yMin + (float)_rng.NextDouble() * _walkArea.height);
            return new PlayerNetState
            {
                playerId = BotIdFor(index),
                displayName = BotNames[index % BotNames.Length],
                characterId = (index % 2 == 0) ? FishingIds.CharacterF : FishingIds.CharacterM,
                avatarIndex = _rng.Next(0, 6),
                level = _rng.Next(3, 16),
                x = p.x,
                y = p.y,
                dir = (int)FacingDir.Down,
                moving = false,
                phase = (int)FishingPhase.Idle,
                bubbleText = string.Empty,
                bubbleUntilUnix = 0L,
                privateMode = false,
                isBot = true,
                lastSeenUnix = NowUnix(),
            };
        }

        private static bool IsFullTestRoom(string roomId, FishingConfig cfg)
        {
            return cfg.roomCount > 1 && roomId == RoomIdFor(cfg.roomCount);
        }

        private static string RoomIdFor(int index1)
        {
            return "room_" + index1.ToString("00", CultureInfo.InvariantCulture);
        }

        private static void Fail(Action<bool, string> onResult, string errorVi)
        {
            Debug.LogWarning(FishingIds.LogTag + " [OFFLINE] JoinRoom thất bại: " + errorVi);
            if (onResult != null) { onResult(false, errorVi); }
        }

        private static long NowUnix()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>Hash ổn định giữa các lần chạy (string.GetHashCode có thể đổi theo runtime).</summary>
        private static int StableHash(string s)
        {
            int h = 17;
            for (int i = 0; i < s.Length; i++) { h = unchecked(h * 31 + s[i]); }
            return h;
        }
    }
}
