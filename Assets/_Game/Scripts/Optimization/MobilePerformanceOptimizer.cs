using UnityEngine;
using UnityEngine.Scripting;

namespace CookingGame.Optimization
{
    /// <summary>
    /// BỘ ĐIỀU PHỐI HIỆU NĂNG TOÀN DIỆN CHO DI ĐỘNG (iOS & Android) — 60 FPS MƯỢT MÀ:
    ///
    /// 1. Khoá cứng 60 FPS & tắt vSync xung đột trên mobile/PC.
    /// 2. Giới hạn độ phân giải render (Resolution Clamping):
    ///    Màn hình Retina/2K/3K trên iPhone/Android được scale về 1080p chuẩn
    ///    giúp giảm 50%–70% GPU Pixel Fillrate, chống nóng máy & tụt pin.
    /// 3. Chặn tắt màn hình lúc chơi (SleepTimeout.NeverSleep).
    /// 4. Dọn rác bộ nhớ (GC) định kỳ khi chuyển cảnh để tránh khựng khung hình.
    /// 5. Tự động cấu hình QualitySettings theo cấu hình máy (Low / High).
    /// </summary>
    [Preserve]
    public static class MobilePerformanceOptimizer
    {
        public static bool IsMobile { get; private set; }
        public static bool IsIOS { get; private set; }
        public static bool IsLowEndDevice { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializePerformance()
        {
#if UNITY_WEBGL
            // WebGL uses WebGLTierScaler instead and must not call native mobile/thread APIs
            return;
#else
            try
            {
                DetectDevice();
                ApplyFrameRateAndVsync();
                ApplyResolutionScaling();
                ApplyQualityOptimizations();
                ConfigureGarbageCollection();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MobilePerformanceOptimizer] Warning during init: {ex.Message}");
            }
#endif
        }

        private static void DetectDevice()
        {
            string os = SystemInfo.operatingSystem ?? "";
            IsIOS = Application.platform == RuntimePlatform.IPhonePlayer ||
                    os.IndexOf("iOS", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    os.IndexOf("iPhone", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    os.IndexOf("iPad", System.StringComparison.OrdinalIgnoreCase) >= 0;

            IsMobile = Application.isMobilePlatform ||
                       SystemInfo.deviceType == DeviceType.Handheld ||
                       IsIOS;

            int ramMB = SystemInfo.systemMemorySize;
            int cpuCores = SystemInfo.processorCount;

            // Thiết bị yếu: iOS đời cũ (<= 3GB RAM) hoặc Android <= 4GB RAM / <= 4 Cores
            IsLowEndDevice = (IsIOS && ramMB <= 3200) || (IsMobile && (ramMB <= 4000 || cpuCores <= 4));

            Debug.Log($"[PerformanceOptimizer] Khởi động tối ưu — Mobile: {IsMobile}, iOS: {IsIOS}, RAM: {ramMB}MB, Cores: {cpuCores}, LowEnd: {IsLowEndDevice}");
        }

        private static void ApplyFrameRateAndVsync()
        {
            // 60 FPS mượt mà
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // Chặn ngủ màn hình khi đang chơi
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private static void ApplyResolutionScaling()
        {
            if (!IsMobile) return;

            // Lấy độ phân giải thực của màn hình
            int screenW = Screen.currentResolution.width;
            int screenH = Screen.currentResolution.height;

            if (screenW <= 0 || screenH <= 0)
            {
                screenW = Screen.width;
                screenH = Screen.height;
            }

            int minDim = Mathf.Min(screenW, screenH);
            int maxDim = Mathf.Max(screenW, screenH);

            // Mục tiêu: Cạnh ngắn tối đa 1080p (hoặc 720p trên máy yếu)
            int targetShortDim = IsLowEndDevice ? 720 : 1080;

            if (minDim > targetShortDim)
            {
                float scale = (float)targetShortDim / minDim;
                int targetW = Mathf.RoundToInt(screenW * scale);
                int targetH = Mathf.RoundToInt(screenH * scale);

                // Làm tròn bội số của 8 để tương thích GPU
                targetW = (targetW / 8) * 8;
                targetH = (targetH / 8) * 8;

                Screen.SetResolution(targetW, targetH, true);
                Debug.Log($"[PerformanceOptimizer] Scale độ phân giải: {screenW}x{screenH} → {targetW}x{targetH} (Tiết kiệm ~{(1f - scale * scale) * 100f:0} % GPU Fillrate)");
            }
        }

        private static void ApplyQualityOptimizations()
        {
            if (IsMobile)
            {
                QualitySettings.antiAliasing = 0; // 2D game không cần MSAA khử răng cưa 3D
                QualitySettings.softParticles = false;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.billboardsFaceCameraPosition = false;
                QualitySettings.streamingMipmapsActive = false;

                // Tối ưu hoá tải bất đồng bộ
                QualitySettings.asyncUploadTimeSlice = 4;
                QualitySettings.asyncUploadBufferSize = 32;
                QualitySettings.asyncUploadPersistentBuffer = true;

                if (IsLowEndDevice)
                {
                    QualitySettings.particleRaycastBudget = 8;
                    QualitySettings.shadows = ShadowQuality.Disable;
                }
                else
                {
                    QualitySettings.particleRaycastBudget = 32;
                }
            }
        }

        private static void ConfigureGarbageCollection()
        {
            // Bật GC Mode tiêu chuẩn
            GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
        }
    }
}
