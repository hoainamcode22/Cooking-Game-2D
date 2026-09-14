using UnityEngine;

namespace CookingGame.Optimization
{
    public static class WebGLTierScaler
    {
        public enum DeviceTier { Tier1_ConstrainedMobile = 1, Tier2_StandardMobile = 2, Tier3_Desktop = 3 }
        public static DeviceTier CurrentTier { get; private set; } = DeviceTier.Tier3_Desktop;
        public static bool IsMobileDevice { get; private set; }
        public static bool IsIOSDevice { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializePlatformPerformance()
        {
            DetectDeviceSpecs();
            ApplyTierSettings();
        }

        private static void DetectDeviceSpecs()
        {
            string os = SystemInfo.operatingSystem ?? "";
            IsIOSDevice = Application.platform == RuntimePlatform.IPhonePlayer || 
                          os.IndexOf("iOS", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                          os.IndexOf("iPhone", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                          os.IndexOf("iPad", System.StringComparison.OrdinalIgnoreCase) >= 0;

            IsMobileDevice = Application.isMobilePlatform || 
                             SystemInfo.deviceType == DeviceType.Handheld || 
                             IsIOSDevice;

            if (IsIOSDevice || (IsMobileDevice && SystemInfo.systemMemorySize <= 3000))
                CurrentTier = DeviceTier.Tier1_ConstrainedMobile;
            else if (IsMobileDevice)
                CurrentTier = DeviceTier.Tier2_StandardMobile;
            else
                CurrentTier = DeviceTier.Tier3_Desktop;
        }

        private static void ApplyTierSettings()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            if (CurrentTier == DeviceTier.Tier1_ConstrainedMobile)
            {
                QualitySettings.softParticles = false;
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.particleRaycastBudget = 8;
                QualitySettings.asyncUploadTimeSlice = 2;
                QualitySettings.asyncUploadBufferSize = 8;
            }
            else if (CurrentTier == DeviceTier.Tier2_StandardMobile)
            {
                QualitySettings.softParticles = false;
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.particleRaycastBudget = 16;
                QualitySettings.asyncUploadTimeSlice = 2;
                QualitySettings.asyncUploadBufferSize = 16;
            }
            else
            {
                QualitySettings.softParticles = true;
                QualitySettings.particleRaycastBudget = 64;
                QualitySettings.asyncUploadTimeSlice = 4;
                QualitySettings.asyncUploadBufferSize = 32;
            }
        }
    }
}