using System;

namespace FarmGame.Fishing
{
    /// <summary>Lời mời vào phòng. Farm hiện hint "A đang mời bạn vào câu cá" + nút Tới.</summary>
    [Serializable]
    public class InviteInfo
    {
        public string fromId;
        public string fromName;
        public string roomId;
        public long unix;
    }
}
