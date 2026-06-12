using UnityEngine;
using UnityEngine.Events;

namespace Day_Night
{
    [ExecuteAlways]
    [DefaultExecutionOrder(999)]
    public class DayNightDayEventHandler : MonoBehaviour
    {
        [System.Serializable]
        public class DayEvent
        {
            public float StartTime = 0f;
            public float EndTime = 1f;
            public UnityEvent OnEvents;
            public UnityEvent OffEvent;

            public bool IsInRange(float time)
            {
                if (StartTime <= EndTime)
                {
                    return time >= StartTime && time <= EndTime;
                }

                return time >= StartTime || time <= EndTime;
            }
        }

        public DayEvent[] Events;

        private DayNightCycleController controller;
        private bool[] eventStates;

        private void OnEnable()
        {
            controller = FindFirstObjectByType<DayNightCycleController>();
            eventStates = new bool[Events == null ? 0 : Events.Length];

            for (int i = 0; i < eventStates.Length; i++)
            {
                eventStates[i] = !Events[i].IsInRange(GetTime());
            }

            UpdateEvents(true);
        }

        private void Update()
        {
            UpdateEvents(false);
        }

        public void RefreshNow()
        {
            if (Events == null)
            {
                return;
            }

            if (eventStates == null || eventStates.Length != Events.Length)
            {
                eventStates = new bool[Events.Length];
            }

            UpdateEvents(true);
        }

        private float GetTime()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<DayNightCycleController>();
            }

            return controller != null ? controller.CurrentDayRatio : 0.5f;
        }

        private void UpdateEvents(bool force)
        {
            if (Events == null)
            {
                return;
            }

            if (eventStates == null || eventStates.Length != Events.Length)
            {
                eventStates = new bool[Events.Length];
                force = true;
            }

            float time = GetTime();
            for (int i = 0; i < Events.Length; i++)
            {
                bool inRange = Events[i].IsInRange(time);
                if (!force && eventStates[i] == inRange)
                {
                    continue;
                }

                eventStates[i] = inRange;

                if (inRange)
                {
                    Events[i].OnEvents?.Invoke();
                }
                else
                {
                    Events[i].OffEvent?.Invoke();
                }
            }
        }
    }
}
