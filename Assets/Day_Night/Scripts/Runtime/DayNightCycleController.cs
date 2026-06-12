using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Day_Night
{
    [ExecuteAlways]
    [DefaultExecutionOrder(10)]
    public class DayNightCycleController : MonoBehaviour
    {
        private const int CurrentPresetVersion = 3;

        [SerializeField, HideInInspector] private int presetVersion;

        [Header("Time")]
        [Min(1f)] public float DayDurationInSeconds = 120f;
        [Range(0f, 1f)] public float StartingTime = 0.5f;
        public bool RunInPlayMode = true;
        public bool PreviewInEditMode = true;

        [Header("Lights")]
        public Transform LightsRoot;
        public Light2D DayLight;
        public Gradient DayLightGradient;
        public AnimationCurve DayLightIntensityCurve;
        public Light2D NightLight;
        public Gradient NightLightGradient;
        public AnimationCurve NightLightIntensityCurve;
        public Light2D AmbientLight;
        public Gradient AmbientLightGradient;
        public AnimationCurve AmbientLightIntensityCurve;
        public Light2D SunRimLight;
        public Gradient SunRimLightGradient;
        public AnimationCurve SunRimIntensityCurve;
        public Light2D MoonRimLight;
        public Gradient MoonRimLightGradient;
        public AnimationCurve MoonRimIntensityCurve;

        [Header("Weather")]
        public DayNightWeatherSystem WeatherSystem;
        public bool UseAutomaticWeather = true;
        [Range(0f, 1f)] public float DayRainStartMin = 0.36f;
        [Range(0f, 1f)] public float DayRainStartMax = 0.56f;
        [Range(0f, 1f)] public float NightRainStartMin = 0.78f;
        [Range(0f, 1f)] public float NightRainStartMax = 0.92f;
        [Min(1f)] public float MinRainDurationSeconds = 10f;
        [Min(1f)] public float MaxRainDurationSeconds = 18f;
        [Range(0f, 1f)] public float ThunderChance = 0.12f;
        [Range(0.4f, 1.5f)] public float RainLightMultiplier = 1f;
        [Range(0.25f, 1.5f)] public float ThunderLightMultiplier = 0.85f;

        [Header("Audio")]
        public AudioSource DayAmbience;
        public AudioSource NightAmbience;
        public AudioSource RainAmbience;
        [Range(0f, 1f)] public float DayAmbienceVolume = 0.55f;
        [Range(0f, 1f)] public float NightAmbienceVolume = 0.55f;
        [Range(0f, 1f)] public float RainAmbienceVolume = 0.8f;
        public float AudioFadeSpeed = 2.5f;
        public AnimationCurve DayAudioCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve NightAudioCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public float CurrentDayRatio { get { return currentDayRatio; } }

        private float currentDayRatio;
        private float previousDayRatio;
        private ScheduledRain dayRain;
        private ScheduledRain nightRain;
        private bool automaticWeatherInitialized;

        private struct ScheduledRain
        {
            public float Start;
            public float End;
            public bool IsActive;
            public bool IsFinished;
            public DayNightWeatherType Weather;
        }

        private void Reset()
        {
            ResetToHappyHarvestDefaults();
        }

        private void OnEnable()
        {
            EnsurePresetIsCurrent();
            currentDayRatio = StartingTime;
            previousDayRatio = currentDayRatio;
            automaticWeatherInitialized = false;
            UpdateSystem(currentDayRatio, true);
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                InitializeAutomaticWeather();
            }
        }

        private void OnValidate()
        {
            EnsurePresetIsCurrent();
            currentDayRatio = StartingTime;
            UpdateSystem(currentDayRatio, true);
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                previousDayRatio = currentDayRatio;

                if (RunInPlayMode)
                {
                    currentDayRatio = Mathf.Repeat(currentDayRatio + Time.deltaTime / Mathf.Max(1f, DayDurationInSeconds), 1f);
                }

                UpdateAutomaticWeather();
                UpdateSystem(currentDayRatio, false);
                return;
            }

            if (PreviewInEditMode)
            {
                UpdateSystem(StartingTime, true);
            }
        }

        public void SetTimeOfDay(float normalizedTime)
        {
            currentDayRatio = Mathf.Repeat(normalizedTime, 1f);
            StartingTime = currentDayRatio;
            previousDayRatio = currentDayRatio;
            automaticWeatherInitialized = false;
            UpdateSystem(currentDayRatio, true);
        }

        public void SetWeather(DayNightWeatherType weather)
        {
            if (WeatherSystem != null)
            {
                WeatherSystem.ChangeWeather(weather);
            }
        }

        public void ResetToHappyHarvestDefaults()
        {
            presetVersion = CurrentPresetVersion;
            DayDurationInSeconds = 120f;
            StartingTime = 0.5f;
            UseAutomaticWeather = true;
            DayRainStartMin = 0.36f;
            DayRainStartMax = 0.56f;
            NightRainStartMin = 0.78f;
            NightRainStartMax = 0.92f;
            MinRainDurationSeconds = 10f;
            MaxRainDurationSeconds = 18f;
            ThunderChance = 0.12f;

            DayLightGradient = CreateGradient(
                new Color(0f, 0f, 0f, 1f), 0.1900f,
                new Color(0.7600f, 0.3300f, 0.1400f, 1f), 0.2800f,
                new Color(1.0000f, 0.9000f, 0.6200f, 1f), 0.3500f,
                new Color(1.0000f, 1.0000f, 1.0000f, 1f), 0.5000f,
                new Color(1.0000f, 0.8600f, 0.5200f, 1f), 0.6800f,
                new Color(0.9500f, 0.2600f, 0.1300f, 1f), 0.7900f,
                new Color(0f, 0f, 0f, 1f), 0.8559f);

            NightLightGradient = CreateGradient(
                new Color(0.1216f, 0.0824f, 0.2941f, 1f), 0.1706f,
                Color.black, 0.2441f,
                Color.black, 0.7675f,
                new Color(0.1213f, 0.0791f, 0.2925f, 1f), 0.8559f);

            AmbientLightGradient = CreateGradient(
                new Color(0.1500f, 0.2100f, 0.8000f, 1f), 0.1200f,
                new Color(0.9400f, 0.7800f, 0.5600f, 1f), 0.3000f,
                new Color(1.0000f, 1.0000f, 1.0000f, 1f), 0.4400f,
                new Color(1.0000f, 1.0000f, 1.0000f, 1f), 0.6400f,
                new Color(1.0000f, 0.9000f, 0.7200f, 1f), 0.7800f,
                new Color(0.1500f, 0.2100f, 0.8000f, 1f), 0.8800f);

            SunRimLightGradient = CreateGradient(
                Color.black, 0.1529f,
                new Color(1f, 0f, 0.6243f, 1f), 0.2147f,
                new Color(0.7642f, 0.2668f, 0.0553f, 1f), 0.3029f,
                new Color(0.3868f, 0.2510f, 0.0973f, 1f), 0.5058f,
                new Color(0.9906f, 0.6245f, 0.2399f, 1f), 0.7265f,
                new Color(0.5660f, 0.0249f, 0.0911f, 1f), 0.8089f,
                Color.black, 0.8559f);

            MoonRimLightGradient = CreateGradient(
                new Color(0.1020f, 0.1569f, 1f, 1f), 0.0971f,
                Color.black, 0.1676f,
                Color.black, 0.7824f,
                new Color(0.1020f, 0.1569f, 1f, 1f), 0.8973f);

            DayLightIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.22f, 0f),
                new Keyframe(0.32f, 0.85f),
                new Keyframe(0.50f, 0.25f),
                new Keyframe(0.68f, 0.75f),
                new Keyframe(0.82f, 0f),
                new Keyframe(1f, 0f));

            NightLightIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 1.35f),
                new Keyframe(0.22f, 1.2f),
                new Keyframe(0.32f, 0f),
                new Keyframe(0.75f, 0f),
                new Keyframe(0.88f, 1.25f),
                new Keyframe(1f, 1.35f));

            AmbientLightIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0.38f),
                new Keyframe(0.24f, 0.55f),
                new Keyframe(0.34f, 1.15f),
                new Keyframe(0.50f, 1.45f),
                new Keyframe(0.68f, 1.25f),
                new Keyframe(0.82f, 0.58f),
                new Keyframe(1f, 0.38f));

            SunRimIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.24f, 0f),
                new Keyframe(0.34f, 0.85f),
                new Keyframe(0.50f, 0.2f),
                new Keyframe(0.72f, 0.75f),
                new Keyframe(0.84f, 0f),
                new Keyframe(1f, 0f));

            MoonRimIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 1.15f),
                new Keyframe(0.20f, 1.05f),
                new Keyframe(0.32f, 0f),
                new Keyframe(0.78f, 0f),
                new Keyframe(0.90f, 1.05f),
                new Keyframe(1f, 1.15f));

            DayAudioCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.24f, 0f),
                new Keyframe(0.30f, 1f),
                new Keyframe(0.72f, 1f),
                new Keyframe(0.82f, 0f),
                new Keyframe(1f, 0f));

            NightAudioCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.24f, 1f),
                new Keyframe(0.32f, 0f),
                new Keyframe(0.72f, 0f),
                new Keyframe(0.84f, 1f),
                new Keyframe(1f, 1f));
        }

        private void UpdateSystem(float ratio, bool instantAudio)
        {
            ApplyLighting(ratio);
            ApplyAudio(ratio, instantAudio);

            if (instantAudio)
            {
                RefreshDayEventHandlers();
            }
        }

        private void InitializeAutomaticWeather()
        {
            automaticWeatherInitialized = true;
            previousDayRatio = currentDayRatio;
            ScheduleRainEvents();

            if (UseAutomaticWeather && WeatherSystem != null)
            {
                WeatherSystem.ChangeWeather(DayNightWeatherType.Sun);
            }
        }

        private void UpdateAutomaticWeather()
        {
            if (!UseAutomaticWeather || WeatherSystem == null)
            {
                return;
            }

            if (!automaticWeatherInitialized)
            {
                InitializeAutomaticWeather();
            }

            if (currentDayRatio < previousDayRatio)
            {
                ScheduleRainEvents();
            }

            UpdateScheduledRain(ref dayRain);
            UpdateScheduledRain(ref nightRain);
        }

        private void ScheduleRainEvents()
        {
            dayRain = CreateScheduledRain(DayRainStartMin, DayRainStartMax);
            nightRain = CreateScheduledRain(NightRainStartMin, NightRainStartMax);
        }

        private ScheduledRain CreateScheduledRain(float startMin, float startMax)
        {
            float min = Mathf.Clamp01(Mathf.Min(startMin, startMax));
            float max = Mathf.Clamp01(Mathf.Max(startMin, startMax));
            float start = Random.Range(min, max);
            float maxDuration = Mathf.Max(MinRainDurationSeconds, MaxRainDurationSeconds);
            float duration = Random.Range(MinRainDurationSeconds, maxDuration);
            float durationRatio = duration / Mathf.Max(1f, DayDurationInSeconds);
            float end = Mathf.Min(start + durationRatio, 0.98f);

            if (end <= start)
            {
                end = Mathf.Min(start + 0.01f, 1f);
            }

            return new ScheduledRain
            {
                Start = start,
                End = end,
                Weather = Random.value <= ThunderChance ? DayNightWeatherType.Thunder : DayNightWeatherType.Rain
            };
        }

        private void UpdateScheduledRain(ref ScheduledRain rain)
        {
            if (rain.IsFinished)
            {
                return;
            }

            if (!rain.IsActive && CrossedTime(rain.Start))
            {
                rain.IsActive = true;
                WeatherSystem.ChangeWeather(rain.Weather);
                return;
            }

            if (rain.IsActive && CrossedTime(rain.End))
            {
                rain.IsActive = false;
                rain.IsFinished = true;
                WeatherSystem.ChangeWeather(DayNightWeatherType.Sun);
            }
        }

        private bool CrossedTime(float target)
        {
            if (Mathf.Approximately(previousDayRatio, currentDayRatio))
            {
                return false;
            }

            if (previousDayRatio < currentDayRatio)
            {
                return target > previousDayRatio && target <= currentDayRatio;
            }

            return target > previousDayRatio || target <= currentDayRatio;
        }

        private void ApplyLighting(float ratio)
        {
            float weatherMultiplier = GetWeatherLightMultiplier();

            if (DayLight != null)
            {
                DayLight.color = DayLightGradient.Evaluate(ratio);
                DayLight.intensity = EvaluateCurve(DayLightIntensityCurve, ratio, 1.57f) * weatherMultiplier;
            }

            if (NightLight != null)
            {
                NightLight.color = NightLightGradient.Evaluate(ratio);
                NightLight.intensity = EvaluateCurve(NightLightIntensityCurve, ratio, 1.57f) * weatherMultiplier;
            }

            if (AmbientLight != null)
            {
                AmbientLight.color = AmbientLightGradient.Evaluate(ratio);
                AmbientLight.intensity = EvaluateCurve(AmbientLightIntensityCurve, ratio, 0.8f) * weatherMultiplier;
            }

            if (SunRimLight != null)
            {
                SunRimLight.color = SunRimLightGradient.Evaluate(ratio);
                SunRimLight.intensity = EvaluateCurve(SunRimIntensityCurve, ratio, 1.57f) * weatherMultiplier;
            }

            if (MoonRimLight != null)
            {
                MoonRimLight.color = MoonRimLightGradient.Evaluate(ratio);
                MoonRimLight.intensity = EvaluateCurve(MoonRimIntensityCurve, ratio, 1.57f) * weatherMultiplier;
            }

            if (LightsRoot != null)
            {
                LightsRoot.localRotation = Quaternion.Euler(0f, 0f, 360f * ratio);
            }
        }

        private float GetWeatherLightMultiplier()
        {
            if (WeatherSystem == null)
            {
                return 1f;
            }

            if (WeatherSystem.CurrentWeather == DayNightWeatherType.Thunder)
            {
                return ThunderLightMultiplier;
            }

            if (WeatherSystem.CurrentWeather == DayNightWeatherType.Rain)
            {
                return RainLightMultiplier;
            }

            return 1f;
        }

        private void ApplyAudio(float ratio, bool instant)
        {
            float dayTarget = DayAudioCurve.Evaluate(ratio) * DayAmbienceVolume;
            float nightTarget = NightAudioCurve.Evaluate(ratio) * NightAmbienceVolume;
            bool raining = WeatherSystem != null &&
                (WeatherSystem.CurrentWeather == DayNightWeatherType.Rain ||
                 WeatherSystem.CurrentWeather == DayNightWeatherType.Thunder);
            float rainTarget = raining ? RainAmbienceVolume : 0f;

            SetSourceVolume(DayAmbience, dayTarget, instant);
            SetSourceVolume(NightAmbience, nightTarget, instant);
            SetSourceVolume(RainAmbience, rainTarget, instant);
        }

        private void SetSourceVolume(AudioSource source, float target, bool instant)
        {
            if (source == null)
            {
                return;
            }

            source.loop = true;

            if (Application.isPlaying && source.isActiveAndEnabled && !source.isPlaying)
            {
                source.Play();
            }

            source.volume = instant
                ? target
                : Mathf.MoveTowards(source.volume, target, AudioFadeSpeed * Time.deltaTime);
        }

        private void RefreshDayEventHandlers()
        {
            DayNightDayEventHandler[] handlers = FindObjectsByType<DayNightDayEventHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < handlers.Length; i++)
            {
                handlers[i].RefreshNow();
            }
        }

        private static Gradient CreateGradient(params object[] values)
        {
            int keyCount = values.Length / 2;
            GradientColorKey[] colorKeys = new GradientColorKey[keyCount];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keyCount];

            for (int i = 0; i < keyCount; i++)
            {
                Color color = (Color)values[i * 2];
                float time = (float)values[i * 2 + 1];
                colorKeys[i] = new GradientColorKey(color, time);
                alphaKeys[i] = new GradientAlphaKey(color.a, time);
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static float EvaluateCurve(AnimationCurve curve, float time, float fallback)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(time) : fallback;
        }

        private void EnsurePresetIsCurrent()
        {
            if (presetVersion >= CurrentPresetVersion &&
                DayLightIntensityCurve != null &&
                DayLightIntensityCurve.length > 0)
            {
                return;
            }

            ResetToHappyHarvestDefaults();
        }
    }
}
