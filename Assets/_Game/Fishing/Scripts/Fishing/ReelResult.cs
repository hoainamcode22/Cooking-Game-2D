namespace FarmGame.Fishing
{
    /// <summary>Kết quả tức thời khi bấm THU (FishingStateMachine.TryReel). Chỉ THÊM vào cuối.</summary>
    public enum ReelResult
    {
        Ignored = 0,      // không ở pha nhận THU (Idle/Casting/Reeling/Result) → bỏ qua
        Nothing = 1,      // THU khi đang Waiting → thu dây không có gì
        BiteAttempt = 2   // THU đúng cửa sổ Bite → vào Reeling, kết quả do SetReelOutcome quyết định
    }
}
