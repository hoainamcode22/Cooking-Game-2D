using System;

namespace FarmGame.Fishing
{
    /// <summary>Thông tin 1 phòng để vẽ danh sách ở bước 2. roomId dạng "room_01".."room_10".</summary>
    [Serializable]
    public class RoomInfo
    {
        public string roomId;
        public string displayName;
        public int capacity;
        public int playerCount;
        public bool IsFull => playerCount >= capacity;
    }
}
