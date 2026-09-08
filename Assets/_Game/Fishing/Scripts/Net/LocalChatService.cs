using System;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Chat OFFLINE. CHỦ FILE: Dev D. Tin của mình phát lại qua OnMessage ngay trong cùng frame (UI vẽ 1 đường duy nhất),
    /// rồi báo room (NotifyLocalChat) để 1 bot đáp. Tin bot (room.OnBotChat) cũng đổ vào OnMessage.
    /// Cắt theo cfg.chatMaxLength; unix là long.
    /// </summary>
    public class LocalChatService : IChatService
    {
        public event Action<ChatMessage> OnMessage;

        private readonly LocalRoomService _room;

        public LocalChatService(LocalRoomService room)
        {
            _room = room;
            if (_room != null) { _room.OnBotChat += HandleBotChat; }
        }

        public bool Send(string text)
        {
            if (_room == null || _room.CurrentRoomId == null) { return false; }
            if (string.IsNullOrEmpty(text)) { return false; }
            string clean = text.Trim();
            if (clean.Length == 0) { return false; }

            int max = Mathf.Max(1, FishingDatabase.ConfigOrDefault.chatMaxLength);
            if (clean.Length > max) { clean = clean.Substring(0, max); }

            var local = _room.LocalState;
            var msg = new ChatMessage
            {
                senderId = _room.LocalPlayerId,
                senderName = local != null && !string.IsNullOrEmpty(local.displayName) ? local.displayName : "Bạn",
                text = clean,
                unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            if (OnMessage != null) { OnMessage(msg); }
            _room.NotifyLocalChat(clean);
            return true;
        }

        private void HandleBotChat(ChatMessage msg)
        {
            if (msg == null) { return; }
            if (OnMessage != null) { OnMessage(msg); }
        }
    }
}
