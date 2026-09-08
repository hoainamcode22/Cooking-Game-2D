using System;

namespace FarmGame.Fishing
{
    /// <summary>Chat trong phòng hiện tại. Tin của chính mình cũng phát lại qua OnMessage để UI vẽ 1 đường duy nhất.</summary>
    public interface IChatService
    {
        event Action<ChatMessage> OnMessage;
        /// <summary>Gửi tin (đã cắt theo chatMaxLength). Trả false nếu chưa vào phòng / rỗng.</summary>
        bool Send(string text);
    }
}
