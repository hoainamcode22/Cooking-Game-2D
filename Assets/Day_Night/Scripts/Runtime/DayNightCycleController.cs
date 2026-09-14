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
        [Min(1f)] public float DayDurationInSeconds = 300f;
        [Range(0f, 1f)] public float StartingTime = 0.5f;
        public bool RunInPlayMode = true;
        public bool PreviewInEditMode = true;

        [Header("Nhip Ngay/Dem (Task: ngay dai, dem nhanh)")]
        [Tooltip("Bat/tat toan bo lop chinh nhip. Tat = quay ve dung hanh vi goc (moi pha chay deu nhau).")]
        public bool UsePhaseDurationScaling = true;

        [Tooltip("He so KEO DAI ban ngay. 1 = nhu cu, 1.8 = ban ngay lau gap 1.8 lan.")]
        [Range(0.25f, 4f)] public float DayPhaseDurationScale = 1.8f;

        [Tooltip("He so RUT NGAN ban dem. 1 = nhu cu, 0.5 = ban dem troi qua nhanh gap doi.")]
        [Range(0.1f, 2f)] public float NightPhaseDurationScale = 0.5f;

        [Tooltip("He so cho hai doan chuyen tiep binh minh va hoang hon. 1 = giu nguyen do dai nhu cu.")]
        [Range(0.1f, 3f)] public float TwilightPhaseDurationScale = 1f;

        [Tooltip("Khi dang MUA (hoac giong), pha hien tai keo dai them bao nhieu phan tram. 0.25 = lau them 25%.")]
        [Range(0f, 1f)] public float RainPhaseExtension = 0.25f;

        [Tooltip("Bat: mot vong ngay/dem VAN dai dung DayDurationInSeconds, chi doi ty le ngay/dem ben trong. Tat: ngay dai ra lam ca vong dai them.")]
        public bool KeepTotalDayLength = true;

        [Header("Nhip Ngay/Dem - Moc chia pha (khop voi gradient anh sang)")]
        [Tooltip("Moc ket thuc dem sang binh minh.")]
        [Range(0f, 1f)] public float DawnStartRatio = 0.05f;
        [Tooltip("Moc binh minh chuyen thanh ban ngay day du.")]
        [Range(0f, 1f)] public float DayStartRatio = 0.10f;
        [Tooltip("Moc ban ngay bat dau chuyen sang hoang hon.")]
        [Range(0f, 1f)] public float DuskStartRatio = 0.85f;
        [Tooltip("Moc hoang hon chuyen han sang ban dem.")]
        [Range(0f, 1f)] public float NightStartRatio = 0.90f;

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
        [Range(0f, 1f)] public float DayRainStartMin = 0.42f;
        [Range(0f, 1f)] public float DayRainStartMax = 0.52f;
        [Range(0f, 1f)] public float NightRainStartMin = 0.82f;
        [Range(0f, 1f)] public float NightRainStartMax = 0.88f;
        [Min(1f)] public float MinRainDurationSeconds = 25f;
        [Min(1f)] public float MaxRainDurationSeconds = 50f;
        [Range(0f, 1f)] public float ThunderChance = 0.08f;
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

        // Toc do troi thoi gian hien tai (1 = nhu ban goc). Chi de debug/hien thi.
        public float CurrentTimeSpeedMultiplier { get { return GetPhaseSpeedMultiplier(currentDayRatio); } }

        private float currentDayRatio;
        private float previousDayRatio;
        private ScheduledRain dayRain;
        private ScheduledRain nightRain;
        private bool automaticWeatherInitialized;
        // Task #35: chan khong cho lop mua/giong lam toi qua muc so voi lop ngay/dem.
        // Toi da giam 12% do sang (>= 0.88x), khong nhan don them voi curve ngay/dem.
        private const float MinWeatherLightMultiplier = 0.88f;
        // Chan so lan chia nho mot frame khi thoi gian nhay qua nhieu moc pha cung luc.
        private const int MaxPhaseSubSteps = 8;
        private const float PhaseBoundaryEpsilon = 0.00001f;

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
                EnsureRainAmbienceAudio();
            }
        }

        private void EnsureRainAmbienceAudio()
        {
            if (RainAmbience == null)
            {
                var sources = GetComponentsInChildren<AudioSource>(true);
                foreach (var s in sources)
                {
                    if (s != null && (s.name.Contains("Rain") || (s.clip != null && s.clip.name.Contains("Rain"))))
                    {
                        RainAmbience = s;
                        break;
                    }
                }

                if (RainAmbience == null)
                {
                    var go = new GameObject("RainAmbienceAudio");
                    go.transform.SetParent(transform, false);
                    RainAmbience = go.AddComponent<AudioSource>();
                    RainAmbience.loop = true;
                    RainAmbience.spatialBlend = 0f;
                    RainAmbience.playOnAwake = false;
#if UNITY_EDITOR
                    RainAmbience.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Day_Night/Audio/Ambience/Rain.wav");
#endif
                }
            }
        }

        private void OnValidate()
        {
            EnsurePresetIsCurrent();
            currentDayRatio = StartingTime;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    UpdateSystem(currentDayRatio, true);
            };
#else
            UpdateSystem(currentDayRatio, true);
#endif
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                previousDayRatio = currentDayRatio;

                if (RunInPlayMode)
                {
                    // Time.deltaTime da nhan Time.timeScale: game pause (timeScale = 0) thi dong ho dung han.
                    currentDayRatio = AdvanceDayRatio(currentDayRatio, Time.deltaTime);
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
            DayDurationInSeconds = 300f;
            UsePhaseDurationScaling = true;
            DayPhaseDurationScale = 1.8f;
            NightPhaseDurationScale = 0.5f;
            TwilightPhaseDurationScale = 1f;
            RainPhaseExtension = 0.25f;
            KeepTotalDayLength = true;
            DawnStartRatio = 0.05f;
            DayStartRatio = 0.10f;
            DuskStartRatio = 0.85f;
            NightStartRatio = 0.90f;
            StartingTime = 0.5f;
            UseAutomaticWeather = true;
            DayRainStartMin = 0.42f;
            DayRainStartMax = 0.52f;
            NightRainStartMin = 0.82f;
            NightRainStartMax = 0.88f;
            MinRainDurationSeconds = 2f;
            MaxRainDurationSeconds = 4f;
            ThunderChance = 0.02f;

            DayLightGradient = CreateGradient(
                new Color(0f, 0f, 0f, 1f), 0.05f,
                new Color(0.7600f, 0.3300f, 0.1400f, 1f), 0.08f,
                new Color(1.0000f, 0.9000f, 0.6200f, 1f), 0.10f,
                new Color(1.0000f, 1.0000f, 1.0000f, 1f), 0.5000f,
                new Color(1.0000f, 0.8600f, 0.5200f, 1f), 0.75f,
                new Color(0.9500f, 0.2600f, 0.1300f, 1f), 0.85f,
                new Color(0f, 0f, 0f, 1f), 0.90f);

            NightLightGradient = CreateGradient(
                Color.black, 0.85f,
                new Color(0.1216f, 0.0824f, 0.2941f, 1f), 0.90f,
                new Color(0.1213f, 0.0791f, 0.2925f, 1f), 0.99f,
                Color.black, 1.0f);

            AmbientLightGradient = CreateGradient(
                new Color(0.1500f, 0.2100f, 0.8000f, 1f), 0.05f,
                new Color(0.9400f, 0.7800f, 0.5600f, 1f), 0.10f,
                new Color(1.0000f, 1.0000f, 1.0000f, 1f), 0.50f,
                new Color(1.0000f, 0.9000f, 0.7200f, 1f), 0.75f,
                new Color(0.1500f, 0.2100f, 0.8000f, 1f), 0.90f);

            SunRimLightGradient = CreateGradient(
                Color.black, 0.05f,
                new Color(1f, 0f, 0.6243f, 1f), 0.08f,
                new Color(0.7642f, 0.2668f, 0.0553f, 1f), 0.10f,
                new Color(0.3868f, 0.2510f, 0.0973f, 1f), 0.50f,
                new Color(0.9906f, 0.6245f, 0.2399f, 1f), 0.75f,
                new Color(0.5660f, 0.0249f, 0.0911f, 1f), 0.85f,
                Color.black, 0.90f);

            MoonRimLightGradient = CreateGradient(
                Color.black, 0.85f,
                new Color(0.1020f, 0.1569f, 1f, 1f), 0.90f,
                new Color(0.1020f, 0.1569f, 1f, 1f), 0.99f,
                Color.black, 1.0f);

            DayLightIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.05f, 0f),
                new Keyframe(0.1f, 0.85f),
                new Keyframe(0.50f, 0.25f),
                new Keyframe(0.85f, 0.75f),
                new Keyframe(0.9f, 0f),
                new Keyframe(1f, 0f));

            NightLightIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.85f, 0f),
                new Keyframe(0.9f, 1.25f),
                new Keyframe(0.95f, 1.35f),
                new Keyframe(0.99f, 1.25f),
                new Keyframe(1f, 0f));

            AmbientLightIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.1f, 1.15f),
                new Keyframe(0.50f, 1.45f),
                new Keyframe(0.85f, 1.25f),
                new Keyframe(0.9f, 0.55f),
                new Keyframe(1f, 0.55f));

            SunRimIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.05f, 0f),
                new Keyframe(0.1f, 0.85f),
                new Keyframe(0.50f, 0.2f),
                new Keyframe(0.85f, 0.75f),
                new Keyframe(0.9f, 0f),
                new Keyframe(1f, 0f));

            MoonRimIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.85f, 0f),
                new Keyframe(0.9f, 1.05f),
                new Keyframe(0.99f, 1.15f),
                new Keyframe(1f, 0f));

            DayAudioCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.05f, 0f),
                new Keyframe(0.1f, 1f),
                new Keyframe(0.85f, 1f),
                new Keyframe(0.9f, 0f),
                new Keyframe(1f, 0f));

            NightAudioCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.85f, 0f),
                new Keyframe(0.9f, 1f),
                new Keyframe(0.99f, 1f),
                new Keyframe(1f, 0f));
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

        // Tra ve he so TOC DO troi thoi gian cho pha dang chay.
        // Ban chat: doi do dai pha thanh toc do (speed = 1 / duration scale),
        // nen chi thay doi thoi gian troi NHANH hay CHAM, khong dung vao mau/anh sang.
        // ApplyLighting van doc cung mot ratio 0..1 nhu cu.
        private float GetPhaseSpeedMultiplier(float ratio)
        {
            if (!UsePhaseDurationScaling)
            {
                return 1f;
            }

            float durationScale = GetPhaseDurationScale(ratio);

            if (KeepTotalDayLength)
            {
                // Chia cho do dai trung binh => tong mot vong van bang DayDurationInSeconds,
                // chi ty le ngay/dem ben trong vong la thay doi.
                float average = GetAveragePhaseDurationScale();
                if (IsUsableNumber(average) && average > 0.0001f)
                {
                    durationScale /= average;
                }
            }

            if (IsRainingNow())
            {
                durationScale *= 1f + Mathf.Max(0f, SanitizeNumber(RainPhaseExtension, 0f));
            }

            if (!IsUsableNumber(durationScale))
            {
                return 1f;
            }

            durationScale = Mathf.Clamp(durationScale, 0.05f, 20f);
            return 1f / durationScale;
        }

        // Do dai trung binh (co trong so theo be rong tung pha) cua mot vong.
        private float GetAveragePhaseDurationScale()
        {
            float dawn = Mathf.Clamp01(SanitizeNumber(DawnStartRatio, 0.05f));
            float day = Mathf.Clamp01(SanitizeNumber(DayStartRatio, 0.10f));
            float dusk = Mathf.Clamp01(SanitizeNumber(DuskStartRatio, 0.85f));
            float night = Mathf.Clamp01(SanitizeNumber(NightStartRatio, 0.90f));

            if (!(dawn <= day && day <= dusk && dusk <= night))
            {
                return 1f;
            }

            float nightSpan = dawn + (1f - night);
            float twilightSpan = (day - dawn) + (night - dusk);
            float daySpan = dusk - day;

            float dayScale = Mathf.Max(0.05f, SanitizeNumber(DayPhaseDurationScale, 1f));
            float nightScale = Mathf.Max(0.05f, SanitizeNumber(NightPhaseDurationScale, 1f));
            float twilightScale = Mathf.Max(0.05f, SanitizeNumber(TwilightPhaseDurationScale, 1f));

            return nightSpan * nightScale + twilightSpan * twilightScale + daySpan * dayScale;
        }

        // Cong don thoi gian theo tung doan pha: neu mot frame dai vuot qua moc chuyen pha
        // (hoac vuot qua 1.0), phan con lai duoc tinh bang toc do cua pha moi, khong bi sai nhip.
        private float AdvanceDayRatio(float ratio, float deltaSeconds)
        {
            if (!IsUsableNumber(ratio))
            {
                ratio = Mathf.Clamp01(SanitizeNumber(StartingTime, 0.5f));
            }

            if (!IsUsableNumber(deltaSeconds) || deltaSeconds <= 0f)
            {
                return Mathf.Repeat(ratio, 1f);
            }

            float duration = Mathf.Max(1f, SanitizeNumber(DayDurationInSeconds, 300f));
            float remaining = deltaSeconds;
            float speed = 1f;

            for (int i = 0; i < MaxPhaseSubSteps && remaining > 0f; i++)
            {
                speed = GetPhaseSpeedMultiplier(ratio);
                if (!IsUsableNumber(speed) || speed <= 0f)
                {
                    speed = 1f;
                }

                float boundary = GetNextPhaseBoundary(ratio);
                float distance = boundary - ratio;
                if (distance <= 0f)
                {
                    break;
                }

                float secondsToBoundary = distance * duration / speed;
                if (!IsUsableNumber(secondsToBoundary) || secondsToBoundary > remaining)
                {
                    break;
                }

                ratio = Mathf.Repeat(boundary + PhaseBoundaryEpsilon, 1f);
                remaining -= secondsToBoundary;
            }

            if (remaining > 0f)
            {
                ratio = Mathf.Repeat(ratio + remaining * speed / duration, 1f);
            }

            return IsUsableNumber(ratio) ? Mathf.Repeat(ratio, 1f) : 0f;
        }

        // Moc chuyen pha ke tiep tinh tu ratio (co the > 1 khi sap sang ngay moi).
        private float GetNextPhaseBoundary(float ratio)
        {
            float best = 1f;

            best = PickCloserBoundary(ratio, best, Mathf.Clamp01(SanitizeNumber(DawnStartRatio, 0.05f)));
            best = PickCloserBoundary(ratio, best, Mathf.Clamp01(SanitizeNumber(DayStartRatio, 0.10f)));
            best = PickCloserBoundary(ratio, best, Mathf.Clamp01(SanitizeNumber(DuskStartRatio, 0.85f)));
            best = PickCloserBoundary(ratio, best, Mathf.Clamp01(SanitizeNumber(NightStartRatio, 0.90f)));

            return best;
        }

        private static float PickCloserBoundary(float ratio, float best, float candidate)
        {
            return candidate > ratio && candidate < best ? candidate : best;
        }

        private static bool IsUsableNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float SanitizeNumber(float value, float fallback)
        {
            return IsUsableNumber(value) ? value : fallback;
        }

        // Phan loai pha theo cac moc, dung dung thu tu dem -> binh minh -> ngay -> hoang hon -> dem.
        private float GetPhaseDurationScale(float ratio)
        {
            float dawn = Mathf.Clamp01(SanitizeNumber(DawnStartRatio, 0.05f));
            float day = Mathf.Clamp01(SanitizeNumber(DayStartRatio, 0.10f));
            float dusk = Mathf.Clamp01(SanitizeNumber(DuskStartRatio, 0.85f));
            float night = Mathf.Clamp01(SanitizeNumber(NightStartRatio, 0.90f));

            float dayScale = Mathf.Max(0.05f, SanitizeNumber(DayPhaseDurationScale, 1f));
            float nightScale = Mathf.Max(0.05f, SanitizeNumber(NightPhaseDurationScale, 1f));
            float twilightScale = Mathf.Max(0.05f, SanitizeNumber(TwilightPhaseDurationScale, 1f));

            // Moc bi dao lon trong Inspector: bo qua lop chinh nhip cho an toan.
            if (!(dawn <= day && day <= dusk && dusk <= night))
            {
                return 1f;
            }

            if (ratio < dawn || ratio >= night)
            {
                return nightScale;
            }

            if (ratio < day || ratio >= dusk)
            {
                return twilightScale;
            }

            return dayScale;
        }

        private bool IsRainingNow()
        {
            if (WeatherSystem == null)
            {
                return false;
            }

            return WeatherSystem.CurrentWeather == DayNightWeatherType.Rain ||
                   WeatherSystem.CurrentWeather == DayNightWeatherType.Thunder;
        }

        private float GetWeatherLightMultiplier()
        {
            if (WeatherSystem == null)
            {
                return 1f;
            }

            if (WeatherSystem.CurrentWeather == DayNightWeatherType.Thunder)
            {
                return Mathf.Max(MinWeatherLightMultiplier, ThunderLightMultiplier);
            }

            if (WeatherSystem.CurrentWeather == DayNightWeatherType.Rain)
            {
                return Mathf.Max(MinWeatherLightMultiplier, RainLightMultiplier);
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

            // [FIX 10/09] Nhan he so am luong SFX. Truoc day ham nay ghi thang volume moi
            // khung hinh nen ambience Ngay / Dem / MUA khong he theo thanh truot "Am thanh
            // VFX" - keo ve 0 van nghe ro, va tat SFX cung khong tat duoc.
            float mucDich = target * global::AudioManager.SfxGain;

            source.volume = instant
                ? mucDich
                : Mathf.MoveTowards(source.volume, mucDich, AudioFadeSpeed * Time.deltaTime);
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
