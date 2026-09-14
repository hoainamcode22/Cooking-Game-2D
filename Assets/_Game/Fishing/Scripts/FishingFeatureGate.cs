using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// CÔNG TẮC BẬT / TẮT TOÀN BỘ HỆ THỐNG CÂU CÁ (Fishing Module):
    /// 
    /// Khi IsEnabled = false:
    ///  - Toàn bộ 26 script chạy ngầm (Offline Ticker, NetHub, RemotePlayers, Popups, VirtualJoystick...)
    ///    sẽ TẮT HOÀN TOÀN, không tự khởi tạo GameObject, không chạy Update(), không tốn CPU/RAM.
    ///  - Khi nào cần dùng lại, chỉ cần chuyển IsEnabled = true (hoặc dùng Menu Tools).
    /// </summary>
    public static class FishingFeatureGate
    {
        // 🔴 ĐẶT FALSE = TẮT HOÀN TOÀN HỆ THỐNG CÂU CÁ ĐỂ GAME NHẸ MƯỢT 100%
        public const bool IsEnabled = false;
    }
}
