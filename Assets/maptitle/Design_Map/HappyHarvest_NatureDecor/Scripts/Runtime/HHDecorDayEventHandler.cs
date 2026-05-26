using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(999)]
public class HHDecorDayEventHandler : MonoBehaviour
{
    [System.Serializable]
    public class DayEvent
    {
        public float StartTime = 0f;
        public float EndTime = 1f;
        public UnityEvent OnEvents;
        public UnityEvent OffEvent;

        public bool IsInRange(float t)
        {
            return t >= StartTime && t <= EndTime;
        }
    }

    public DayEvent[] Events;

    private bool[] activeStates;

    private void OnEnable()
    {
        activeStates = new bool[Events == null ? 0 : Events.Length];
        UpdateEvents(force: true);
    }

    private void Update()
    {
        UpdateEvents(force: false);
    }

    private void UpdateEvents(bool force)
    {
        if (Events == null || Events.Length == 0)
            return;

        float dayRatio = GetDayRatio();

        for (int i = 0; i < Events.Length; i++)
        {
            bool isActive = Events[i].IsInRange(dayRatio);
            if (!force && activeStates[i] == isActive)
                continue;

            activeStates[i] = isActive;
            if (isActive)
                Events[i].OnEvents?.Invoke();
            else
                Events[i].OffEvent?.Invoke();
        }
    }

    private static float GetDayRatio()
    {
        Day_Night.DayNightCycleController controller = Object.FindFirstObjectByType<Day_Night.DayNightCycleController>();
        return controller == null ? 0.5f : controller.CurrentDayRatio;
    }
}
