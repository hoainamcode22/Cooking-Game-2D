using UnityEngine;

namespace Day_Night
{
    public class DayNightThunderAudio : MonoBehaviour
    {
        public AudioSource ThunderSource;
        public Vector2 DelayRange = new Vector2(7f, 18f);
        [Range(0f, 1f)] public float Volume = 0.9f;

        private float nextThunderTime;

        private void OnEnable()
        {
            ScheduleNextThunder();
        }

        private void Update()
        {
            if (!Application.isPlaying || ThunderSource == null || ThunderSource.clip == null)
            {
                return;
            }

            if (Time.time < nextThunderTime)
            {
                return;
            }

            ThunderSource.PlayOneShot(ThunderSource.clip, Volume);
            ScheduleNextThunder();
        }

        private void ScheduleNextThunder()
        {
            float min = Mathf.Min(DelayRange.x, DelayRange.y);
            float max = Mathf.Max(DelayRange.x, DelayRange.y);
            nextThunderTime = Time.time + Random.Range(min, max);
        }
    }
}
