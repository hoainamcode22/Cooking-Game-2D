using UnityEngine;
using UnityEngine.Events;

namespace Day_Night
{
    public class DayNightLegacyDayEventHandler : MonoBehaviour
    {
        [System.Serializable]
        public class DayEvent
        {
            public float StartTime;
            public float EndTime = 1f;
            public UnityEvent OnEvents;
            public UnityEvent OffEvent;
        }

        public DayEvent[] Events;
    }
}
