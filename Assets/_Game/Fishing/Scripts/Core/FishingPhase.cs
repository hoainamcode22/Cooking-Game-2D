namespace FarmGame.Fishing
{
    /// <summary>
    /// Pha của một lượt câu. Dùng chung cho FishingStateMachine (Dev B), HUD (Dev C) và PlayerNetState (đồng bộ mạng).
    /// Serialize int, chỉ được THÊM vào cuối.
    /// </summary>
    public enum FishingPhase
    {
        Idle = 0,      // đứng, chưa quăng
        Casting = 1,   // đang vung cần (animation), phao đang bay
        Waiting = 2,   // phao nổi, chờ cá
        Bite = 3,      // cá cắn, cửa sổ THU
        Reeling = 4,   // đang thu dây
        Result = 5     // hiện kết quả (bắt được / thoát / không có gì) rồi về Idle
    }
}
