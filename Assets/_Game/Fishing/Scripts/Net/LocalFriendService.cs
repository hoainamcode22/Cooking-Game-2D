using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Bạn bè OFFLINE. CHỦ FILE: Dev D. Lưu PlayerPrefs "FISHING_FRIENDS_LOCAL" (blob JSON {saveVersion, friends}) + LuuGopPrefs.Hen().
    /// Bot chấp nhận kết bạn ngay. Mời = log + true. Tặng quà trừ gem qua FarmEconomyManager / cá qua FishBasket (Dev B).
    /// Giả lập lời mời: nếu cfg.offlineSimulateInviteAfterSeconds > 0 và đang ở farm thì sau đó giây bắn 1 InviteInfo
    /// từ bạn đầu tiên trong Friends (tick qua FishingOfflineTicker). PrivateMode lưu PlayerPrefs int.
    /// </summary>
    public class LocalFriendService : IFriendService
    {
        private const string PrefsKey = "FISHING_FRIENDS_LOCAL";
        private const string PrefsPrivateKey = "FISHING_PRIVATE_MODE_LOCAL";
        private const int BlobVersion = 1;

        /// <summary>DTO cho JsonUtility (chỉ field public + List).</summary>
        [Serializable]
        private class FriendsBlob
        {
            public int saveVersion = BlobVersion;
            public List<FriendEntry> friends = new List<FriendEntry>();
        }

        public IReadOnlyList<FriendEntry> Friends { get { return _friends; } }
        public event Action OnFriendsChanged;
        public event Action<InviteInfo> OnInviteReceived;
        // OFFLINE không ai tặng lại mình → event thuộc interface, không bao giờ bắn (tắt cảnh báo CS0067).
#pragma warning disable 67
        public event Action<string, GiftKind, string, int> OnGiftReceived;
#pragma warning restore 67

        public bool PrivateMode
        {
            get { return _privateMode; }
            set
            {
                if (_privateMode == value) { return; }
                _privateMode = value;
                PlayerPrefs.SetInt(PrefsPrivateKey, value ? 1 : 0);
                LuuGopPrefs.Hen();
            }
        }

        private readonly LocalRoomService _room;
        private readonly List<FriendEntry> _friends = new List<FriendEntry>();
        private bool _privateMode;

        // Giả lập lời mời khi ở farm
        private float _farmTimer;
        private bool _inviteFiredThisVisit;
        private bool _tickerHooked;

        public LocalFriendService(LocalRoomService room)
        {
            _room = room;
            Load();
            _privateMode = PlayerPrefs.GetInt(PrefsPrivateKey, 0) == 1;
            if (_room != null)
            {
                _room.OnPlayerJoined += HandleBotJoined;
                _room.OnPlayerLeft += HandleBotLeft;
            }
            HookTickerIfNeeded();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Tra cứu
        // ─────────────────────────────────────────────────────────────────

        public bool IsFriend(string playerId)
        {
            return Find(playerId) != null;
        }

        public FriendEntry Find(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) { return null; }
            for (int i = 0; i < _friends.Count; i++)
            {
                if (_friends[i] != null && _friends[i].playerId == playerId) { return _friends[i]; }
            }
            return null;
        }

        public RelationshipKind GetRelationship(string playerId)
        {
            var f = Find(playerId);
            return f != null ? f.relation : RelationshipKind.None;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Kết bạn / kết nối
        // ─────────────────────────────────────────────────────────────────

        public void SendFriendRequest(PlayerNetState target)
        {
            if (target == null || string.IsNullOrEmpty(target.playerId)) { return; }
            if (_room != null && target.playerId == _room.LocalPlayerId) { return; }
            if (IsFriend(target.playerId))
            {
                Debug.Log(FishingIds.LogTag + " Đã là bạn: " + target.displayName);
                return;
            }
            // OFFLINE: bot chấp nhận ngay.
            _friends.Add(new FriendEntry
            {
                playerId = target.playerId,
                displayName = target.displayName,
                avatarIndex = target.avatarIndex,
                level = target.level,
                online = true,
                roomId = _room != null ? _room.CurrentRoomId : null,
                relation = RelationshipKind.Friend,
                privateMode = target.privateMode,
            });
            Save();
            Debug.Log(FishingIds.LogTag + " [OFFLINE] " + target.displayName + " đã chấp nhận kết bạn.");
            if (_room != null) { _room.SetBotBubble(target.playerId, "Rất vui được làm bạn! 🤝"); }
            if (OnFriendsChanged != null) { OnFriendsChanged(); }
        }

        public void RemoveFriend(string playerId)
        {
            var f = Find(playerId);
            if (f == null) { return; }
            _friends.Remove(f);
            Save();
            Debug.Log(FishingIds.LogTag + " Huỷ bạn: " + f.displayName);
            if (OnFriendsChanged != null) { OnFriendsChanged(); }
        }

        public bool SetRelationship(string playerId, RelationshipKind kind)
        {
            var f = Find(playerId);
            if (f == null) { return false; }
            if (f.relation == kind) { return true; }
            f.relation = kind;
            Save();
            Debug.Log(FishingIds.LogTag + " Kết nối với " + f.displayName + " = " + kind);
            if (_room != null) { _room.SetBotBubble(playerId, kind == RelationshipKind.Dating ? "Ngại quá 😳" : "Tuyệt vời! 💛"); }
            if (OnFriendsChanged != null) { OnFriendsChanged(); }
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Mời / tặng
        // ─────────────────────────────────────────────────────────────────

        public bool InviteToRoom(string playerId, string roomId)
        {
            var f = Find(playerId);
            if (f == null) { Debug.LogWarning(FishingIds.LogTag + " Mời thất bại: chưa là bạn với " + playerId); return false; }
            if (string.IsNullOrEmpty(roomId)) { roomId = _room != null ? _room.CurrentRoomId : null; }
            Debug.Log(FishingIds.LogTag + " [OFFLINE] Đã mời " + f.displayName + " vào " + (roomId ?? "(chưa có phòng)") + " (giả lập, không có phản hồi thật).");
            if (_room != null && _room.HasBot(playerId)) { _room.SetBotBubble(playerId, "Tới ngay! 🎣"); }
            return true;
        }

        public bool SendGift(string playerId, GiftKind kind, string itemId, int amount, out string errorVi)
        {
            errorVi = null;
            if (string.IsNullOrEmpty(playerId)) { errorVi = "Chưa chọn người nhận"; return false; }
            if (amount <= 0) { errorVi = "Số lượng không hợp lệ"; return false; }

            var cfg = FishingDatabase.ConfigOrDefault;
            switch (kind)
            {
                case GiftKind.Gems:
                    {
                        if (amount < cfg.giftGemMin || amount > cfg.giftGemMax)
                        {
                            errorVi = "Chỉ tặng từ " + cfg.giftGemMin.ToString(CultureInfo.InvariantCulture) + " đến " + cfg.giftGemMax.ToString(CultureInfo.InvariantCulture) + " kim cương";
                            return false;
                        }
                        var eco = FarmEconomyManager.Instance;
                        if (eco == null) { errorVi = "Chưa có hệ tiền tệ"; return false; }
                        if (!eco.SpendGems(amount)) { errorVi = "Không đủ kim cương"; return false; }
                        break;
                    }
                case GiftKind.Fish:
                    {
                        if (string.IsNullOrEmpty(itemId)) { errorVi = "Chưa chọn cá"; return false; }
                        var basket = FishBasket.Instance;
                        if (basket == null) { errorVi = "Chưa có giỏ cá"; return false; }
                        if (!basket.Remove(itemId, amount)) { errorVi = "Không đủ cá"; return false; }
                        break;
                    }
                default:
                    errorVi = "Loại quà không hỗ trợ";
                    return false;
            }

            var audio = AudioManager.Instance;
            if (audio != null) { audio.PlayGemSparkle(); }
            string name = Find(playerId) != null ? Find(playerId).displayName : playerId;
            Debug.Log(FishingIds.LogTag + " [OFFLINE] Tặng " + name + ": " + kind + " " + (itemId ?? string.Empty) + " x" + amount.ToString(CultureInfo.InvariantCulture));
            if (_room != null) { _room.SetBotBubble(playerId, "Cảm ơn bạn nhiều 💖"); }
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Giả lập lời mời khi ở farm (FishingOfflineTicker)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Gọi mỗi frame khi ở farm. Public để test/Editor gọi tay.</summary>
        public void TickFarmSide(float dt)
        {
            var cfg = FishingDatabase.ConfigOrDefault;
            if (cfg.offlineSimulateInviteAfterSeconds <= 0f) { return; }
            if (FishingSession.IsInFishingScene)
            {
                _farmTimer = 0f;
                _inviteFiredThisVisit = false;
                return;
            }
            if (_inviteFiredThisVisit || _friends.Count == 0) { return; }
            _farmTimer += dt;
            if (_farmTimer < cfg.offlineSimulateInviteAfterSeconds) { return; }

            _inviteFiredThisVisit = true;
            var f = _friends[0];
            var invite = new InviteInfo
            {
                fromId = f.playerId,
                fromName = f.displayName,
                roomId = !string.IsNullOrEmpty(f.roomId) ? f.roomId : "room_01",
                unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            Debug.Log(FishingIds.LogTag + " [OFFLINE] Giả lập lời mời từ " + f.displayName + " vào " + invite.roomId);
            if (OnInviteReceived != null) { OnInviteReceived(invite); }
        }

        private void HookTickerIfNeeded()
        {
            if (_tickerHooked) { return; }
            if (FishingDatabase.ConfigOrDefault.offlineSimulateInviteAfterSeconds <= 0f) { return; }
            if (!FishingOfflineTicker.Ensure()) { return; }
            FishingOfflineTicker.OnTick += TickFarmSide;
            _tickerHooked = true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Đồng bộ online/roomId của bạn là bot
        // ─────────────────────────────────────────────────────────────────

        private void HandleBotJoined(PlayerNetState bot)
        {
            if (bot == null) { return; }
            var f = Find(bot.playerId);
            if (f == null) { return; }
            f.online = true;
            f.roomId = _room != null ? _room.CurrentRoomId : null;
            f.level = bot.level;
            f.displayName = bot.displayName;
            if (OnFriendsChanged != null) { OnFriendsChanged(); }
        }

        private void HandleBotLeft(string playerId)
        {
            var f = Find(playerId);
            if (f == null) { return; }
            // Bot "vẫn ở lại phòng" sau khi mình rời → giữ online + roomId để nút "Tới" ở farm còn bật.
            f.online = true;
            if (OnFriendsChanged != null) { OnFriendsChanged(); }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Save/Load
        // ─────────────────────────────────────────────────────────────────

        private void Load()
        {
            _friends.Clear();
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) { return; }
            try
            {
                var blob = JsonUtility.FromJson<FriendsBlob>(json);
                if (blob == null || blob.friends == null) { return; }
                if (blob.saveVersion != BlobVersion) { Debug.Log(FishingIds.LogTag + " FISHING_FRIENDS_LOCAL version " + blob.saveVersion.ToString(CultureInfo.InvariantCulture) + " → " + BlobVersion.ToString(CultureInfo.InvariantCulture) + " (không cần migrate)."); }
                for (int i = 0; i < blob.friends.Count; i++)
                {
                    var f = blob.friends[i];
                    if (f != null && !string.IsNullOrEmpty(f.playerId) && Find(f.playerId) == null) { _friends.Add(f); }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(FishingIds.LogTag + " Không đọc được FISHING_FRIENDS_LOCAL, bỏ qua: " + e.Message);
            }
        }

        private void Save()
        {
            var blob = new FriendsBlob { saveVersion = BlobVersion };
            blob.friends.AddRange(_friends);
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(blob));
            LuuGopPrefs.Hen();
        }
    }
}
