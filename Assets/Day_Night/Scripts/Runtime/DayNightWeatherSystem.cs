using System;
using System.Collections.Generic;
using UnityEngine;

namespace Day_Night
{
    public class DayNightWeatherSystem : MonoBehaviour
    {
        public Transform SearchRoot;
        public DayNightWeatherType StartingWeather = DayNightWeatherType.Sun;

        public DayNightWeatherType CurrentWeather { get; private set; }
        public event Action<DayNightWeatherType> WeatherChanged;

        private readonly List<DayNightWeatherElement> elements = new List<DayNightWeatherElement>();

        private void Awake()
        {
            CurrentWeather = StartingWeather;
        }

        private void Start()
        {
            RefreshElements();
            ChangeWeather(StartingWeather);
        }

        private void OnValidate()
        {
            CurrentWeather = StartingWeather;
            RefreshElements();
            ApplyWeather();
        }

        public void ChangeWeather(DayNightWeatherType newWeather)
        {
            CurrentWeather = newWeather;
            RefreshElements();
            ApplyWeather();
            WeatherChanged?.Invoke(CurrentWeather);
        }

        public void SetSun()
        {
            ChangeWeather(DayNightWeatherType.Sun);
        }

        public void SetRain()
        {
            ChangeWeather(DayNightWeatherType.Rain);
        }

        public void SetThunder()
        {
            ChangeWeather(DayNightWeatherType.Thunder);
        }

        public void ToggleRain()
        {
            ChangeWeather(CurrentWeather == DayNightWeatherType.Rain ? DayNightWeatherType.Sun : DayNightWeatherType.Rain);
        }

        public void RefreshElements()
        {
            elements.Clear();

            if (SearchRoot != null)
            {
                SearchRoot.GetComponentsInChildren(true, elements);
                return;
            }

            elements.AddRange(FindObjectsByType<DayNightWeatherElement>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        private void ApplyWeather()
        {
            for (int i = 0; i < elements.Count; i++)
            {
                DayNightWeatherElement element = elements[i];
                if (element == null)
                {
                    continue;
                }

                bool active = (element.WeatherType & CurrentWeather) != 0;
                if (element.gameObject.activeSelf != active)
                {
                    element.gameObject.SetActive(active);
                }
            }
        }
    }
}
