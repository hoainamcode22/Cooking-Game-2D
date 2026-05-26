using UnityEngine;

namespace Day_Night
{
    public class DayNightLegacyAmbienceBlender : MonoBehaviour
    {
        public AudioSource DayAmbienceSource;
        public AudioSource NightAmbienceSource;

        public void BlendToDay()
        {
            SetVolumes(1f, 0f);
        }

        public void BlendToNight()
        {
            SetVolumes(0f, 1f);
        }

        private void SetVolumes(float dayVolume, float nightVolume)
        {
            if (DayAmbienceSource != null) DayAmbienceSource.volume = dayVolume;
            if (NightAmbienceSource != null) NightAmbienceSource.volume = nightVolume;
        }
    }
}
