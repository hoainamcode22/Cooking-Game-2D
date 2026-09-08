namespace FarmGame.Fishing
{
    /// <summary>Kết cục của một lượt câu, đọc ở pha Result (FishingStateMachine.LastOutcome). Chỉ THÊM vào cuối.</summary>
    public enum FishingOutcome
    {
        None = 0,      // chưa có kết quả (đang giữa lượt hoặc bị huỷ)
        Caught = 1,    // bắt được cá
        Escaped = 2,   // cá thoát: THU trễ (hết cửa sổ) hoặc xui khi roll
        Nothing = 3    // THU sớm khi chưa có cá
    }
}
