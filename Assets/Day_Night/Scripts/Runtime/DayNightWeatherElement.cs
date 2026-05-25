using UnityEngine;

namespace Day_Night
{
    [ExecuteAlways]
    [DefaultExecutionOrder(999)]
    public class DayNightWeatherElement : MonoBehaviour
    {
        public DayNightWeatherType WeatherType = DayNightWeatherType.Rain;
    }
}
