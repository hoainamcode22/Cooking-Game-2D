using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Quản lý toàn bộ âm thanh và nhạc nền trong game.
/// Tự động sinh singleton, nạp các file âm thanh từ Resources/Audio và Assets/Audio Game.
/// Đảm bảo âm thanh 2D rõ nét, không bị tắt tiếng hay phụ thuộc khoảng cách camera.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        if (_instance == null)
        {
            var inst = Instance;
        }
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource fxSource;
    [SerializeField] private AudioSource waterAmbienceSource;

    [Header("Core Clips (Assets/Audio Game)")]
    [SerializeField] private AudioClip bgmMain;        // Morning_Garden_Waltz.mp3
    [SerializeField] private AudioClip uiClick;        // button.wav (tất cả nút)
    [SerializeField] private AudioClip expClip;        // exp.mp3 (kinh nghiệm)
    [SerializeField] private AudioClip plantingClip;   // gieohat.mp3 (gieo hạt & hoa)
    [SerializeField] private AudioClip harvestClip;    // thuhoach.mp3 (thu hoạch nông sản & hoa)
    [SerializeField] private AudioClip coinReward;     // vàng.wav (tiền vàng)
    [SerializeField] private AudioClip ingredientPop;
    [SerializeField] private AudioClip cookStart;
    [SerializeField] private AudioClip successJingle;
    [SerializeField] private AudioClip waterFlowClip;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.35f;     // Nhạc nền rõ ràng, êm dịu
    [Range(0f, 1f)][SerializeField] private float uiVolume = 0.70f;      // Tiếng nút bấm nảy giòn
    [Range(0f, 1f)][SerializeField] private float fxVolume = 0.85f;      // Tiếng gieo hạt, thu hoạch, vàng, exp
    [Range(0f, 1f)][SerializeField] private float waterVolume = 0.25f;

    [Header("Anti-Spam Cooldowns")]
    [SerializeField] private float uiClickCooldown = 0.05f;
    [SerializeField] private float expCooldown = 0.08f;
    [SerializeField] private float farmCooldown = 0.07f;
    [SerializeField] private float coinCooldown = 0.06f;

    private float lastUIClickTime = -999f;
    private float lastExpTime = -999f;
    private float lastFarmActionTime = -999f;
    private float lastCoinTime = -999f;
    private Coroutine duckRoutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        EnsureAudioListener();

        if (bgmSource == null) bgmSource = CreateChildSource("BGM_Source");
        if (uiSource == null) uiSource = CreateChildSource("UI_Source");
        if (fxSource == null) fxSource = CreateChildSource("FX_Source");
        if (waterAmbienceSource == null) waterAmbienceSource = CreateChildSource("Water_Ambience_Source");

        SetupSource(bgmSource, true, bgmVolume);
        SetupSource(uiSource, false, uiVolume);
        SetupSource(fxSource, false, fxVolume);
        SetupSource(waterAmbienceSource, true, 0f);

        LoadDefaultClipsIfMissing();
    }

    private void Start()
    {
        PlayMainBGM();
        StartWaterAmbience();
    }

    private void EnsureAudioListener()
    {
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cam.gameObject.AddComponent<AudioListener>();
            else
                gameObject.AddComponent<AudioListener>();
        }
    }

    private void Update()
    {
        // Tự động bắt sự kiện bấm cho TẤT CẢ nút bấm (UI Button / Toggle) trong game
        if (Input.GetMouseButtonDown(0) && EventSystem.current != null)
        {
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go == null) continue;

                var btn = go.GetComponentInParent<Button>();
                if (btn != null && btn.interactable && btn.isActiveAndEnabled)
                {
                    PlayUIClick();
                    break;
                }

                var toggle = go.GetComponentInParent<Toggle>();
                if (toggle != null && toggle.interactable && toggle.isActiveAndEnabled)
                {
                    PlayUIClick();
                    break;
                }
            }
        }

        UpdateWaterProximity();
    }

    private GameObject[] _cachedWaterObjects;
    private float _lastWaterSearchTime = -99f;

    /// <summary>
    /// Giảm nhỏ tiếng nước chảy khi ở xa.
    /// Chỉ khi Camera di chuyển hoặc zoom tới gần sông/suối/biển mới phát to dần lên.
    /// </summary>
    private void UpdateWaterProximity()
    {
        if (waterAmbienceSource == null || waterFlowClip == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        if (Time.unscaledTime - _lastWaterSearchTime > 3f || _cachedWaterObjects == null)
        {
            _lastWaterSearchTime = Time.unscaledTime;
            var list = new List<GameObject>();
            var all = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                string lname = all[i].name.ToLowerInvariant();
                if (lname.Contains("water") || lname.Contains("song") || lname.Contains("bien") || lname.Contains("river") || lname.Contains("ocean"))
                {
                    list.Add(all[i]);
                }
            }
            _cachedWaterObjects = list.ToArray();
        }

        float minDistance = float.MaxValue;
        Vector3 camPos = cam.transform.position;

        if (_cachedWaterObjects != null && _cachedWaterObjects.Length > 0)
        {
            for (int i = 0; i < _cachedWaterObjects.Length; i++)
            {
                if (_cachedWaterObjects[i] == null) continue;
                float d = Vector2.Distance(camPos, _cachedWaterObjects[i].transform.position);
                if (d < minDistance) minDistance = d;
            }
        }

        float targetVol = 0f;
        if (minDistance < 22f)
        {
            float proximity = Mathf.Clamp01(1f - (minDistance / 22f));
            float zoomFactor = cam.orthographic ? Mathf.Clamp01((14f - cam.orthographicSize) / 8f) : 0.8f;
            targetVol = waterVolume * proximity * Mathf.Max(0.2f, zoomFactor);
        }

        waterAmbienceSource.volume = Mathf.MoveTowards(waterAmbienceSource.volume, targetVol, Time.unscaledDeltaTime * 0.5f);
    }

    private void LoadDefaultClipsIfMissing()
    {
        // 1. Nạp từ Resources/Audio (ưu tiên cao, chạy được trên cả Build và Editor)
        if (bgmMain == null) bgmMain = Resources.Load<AudioClip>("Audio/Morning_Garden_Waltz");
        if (uiClick == null) uiClick = Resources.Load<AudioClip>("Audio/button");
        if (expClip == null) expClip = Resources.Load<AudioClip>("Audio/exp");
        if (plantingClip == null) plantingClip = Resources.Load<AudioClip>("Audio/gieohat");
        if (harvestClip == null) harvestClip = Resources.Load<AudioClip>("Audio/thuhoach");
        if (coinReward == null) coinReward = Resources.Load<AudioClip>("Audio/gold")
            ?? Resources.Load<AudioClip>("Audio/vang")
            ?? Resources.Load<AudioClip>("Audio/vàng");
        if (waterFlowClip == null) waterFlowClip = Resources.Load<AudioClip>("Audio/Ambience/water_flowing");

#if UNITY_EDITOR
        // 2. Fallback trực tiếp từ Assets/Audio Game
        if (bgmMain == null)
            bgmMain = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/Morning_Garden_Waltz.mp3");
        if (uiClick == null)
            uiClick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/button.wav");
        if (expClip == null)
            expClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/exp.mp3");
        if (plantingClip == null)
            plantingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/gieohat.mp3");
        if (harvestClip == null)
            harvestClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/thuhoach.mp3");
        if (coinReward == null)
            coinReward = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/gold.wav")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/vang.wav")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/vàng.wav");
        if (waterFlowClip == null)
            waterFlowClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Day_Night/Audio/Ambience/Water flowing.wav");
#endif
    }

    private AudioSource CreateChildSource(string sourceName)
    {
        GameObject obj = new GameObject(sourceName);
        obj.transform.SetParent(transform);
        return obj.AddComponent<AudioSource>();
    }

    private void SetupSource(AudioSource source, bool loop, float volume)
    {
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = 0f; // 2D âm thanh trực diện, không bị suy giảm theo toạ độ camera
    }

    /// <summary>🎵 Nhạc nền êm dịu, nhẹ nhàng (Morning_Garden_Waltz.mp3)</summary>
    public void PlayMainBGM()
    {
        if (bgmMain == null && bgmSource != null) LoadDefaultClipsIfMissing();
        if (bgmMain == null || bgmSource == null) return;

        if (bgmSource.clip == bgmMain && bgmSource.isPlaying)
            return;

        bgmSource.clip = bgmMain;
        bgmSource.volume = bgmVolume;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
    }

    public void StartWaterAmbience()
    {
        if (waterFlowClip == null && waterAmbienceSource != null) LoadDefaultClipsIfMissing();
        if (waterFlowClip == null || waterAmbienceSource == null) return;
        if (waterAmbienceSource.clip == waterFlowClip && waterAmbienceSource.isPlaying) return;

        waterAmbienceSource.clip = waterFlowClip;
        waterAmbienceSource.volume = 0f;
        waterAmbienceSource.spatialBlend = 0f;
        waterAmbienceSource.Play();
    }

    private void PlayFX(AudioClip clip, float volumeScale = 1f, float pitchMin = 0.98f, float pitchMax = 1.02f)
    {
        if (clip == null && fxSource != null) LoadDefaultClipsIfMissing();
        if (clip == null || fxSource == null) return;

        fxSource.pitch = Random.Range(pitchMin, pitchMax);
        fxSource.PlayOneShot(clip, fxVolume * volumeScale);
    }

    /// <summary>🔘 Tiếng bấm nút button (button.wav) — tự động áp dụng cho tất cả Button trong game</summary>
    public void PlayUIClick()
    {
        if (Time.unscaledTime - lastUIClickTime < uiClickCooldown)
            return;

        lastUIClickTime = Time.unscaledTime;
        if (uiClick == null) LoadDefaultClipsIfMissing();
        if (uiClick != null && uiSource != null)
        {
            uiSource.pitch = Random.Range(0.99f, 1.01f);
            uiSource.PlayOneShot(uiClick, uiVolume);
        }
    }

    public void PlayIngredientPop()
    {
        PlayUIClick();
    }

    /// <summary>🌱 Tiếng gieo hạt giống và hoa (gieohat.mp3)</summary>
    public void PlayPlanting()
    {
        if (Time.unscaledTime - lastFarmActionTime < farmCooldown) return;
        lastFarmActionTime = Time.unscaledTime;

        if (plantingClip == null) LoadDefaultClipsIfMissing();
        AudioClip clip = plantingClip != null ? plantingClip : uiClick;
        PlayFX(clip, 1f, 0.96f, 1.04f);
    }

    /// <summary>🌾 Tiếng kéo liềm thu hoạch nông sản & hoa (thuhoach.mp3)</summary>
    public void PlayHarvest()
    {
        if (Time.unscaledTime - lastFarmActionTime < farmCooldown) return;
        lastFarmActionTime = Time.unscaledTime;

        if (harvestClip == null) LoadDefaultClipsIfMissing();
        AudioClip clip = harvestClip != null ? harvestClip : uiClick;
        PlayFX(clip, 1f, 0.97f, 1.03f);
    }

    /// <summary>⭐ Tiếng nhận EXP kinh nghiệm (exp.mp3)</summary>
    public void PlayExp()
    {
        if (Time.unscaledTime - lastExpTime < expCooldown) return;
        lastExpTime = Time.unscaledTime;

        if (expClip == null) LoadDefaultClipsIfMissing();
        AudioClip clip = expClip != null ? expClip : uiClick;
        PlayFX(clip, 0.95f, 0.98f, 1.02f);
    }

    /// <summary>💰 Tiếng tiền vàng / coin (vàng.wav)</summary>
    public void PlayCoinReward()
    {
        if (Time.unscaledTime - lastCoinTime < coinCooldown) return;
        lastCoinTime = Time.unscaledTime;

        if (coinReward == null) LoadDefaultClipsIfMissing();
        AudioClip clip = coinReward != null ? coinReward : uiClick;
        PlayFX(clip, 0.9f, 0.98f, 1.02f);
    }

    public void PlayBuySell()
    {
        PlayCoinReward();
    }

    public void PlayCookStart()
    {
        DuckBGM(0.6f, 0.25f);
        PlayFX(cookStart != null ? cookStart : uiClick, 0.75f, 1f, 1.03f);
    }

    public void PlaySuccess()
    {
        DuckBGM(0.5f, 0.5f);
        PlayFX(successJingle != null ? successJingle : expClip, 0.8f, 1f, 1.02f);
    }

    private void DuckBGM(float multiplier, float duration)
    {
        if (duckRoutine != null)
            StopCoroutine(duckRoutine);

        duckRoutine = StartCoroutine(DuckBGMRoutine(multiplier, duration));
    }

    private IEnumerator DuckBGMRoutine(float multiplier, float duration)
    {
        if (bgmSource == null) yield break;

        float originalVolume = bgmVolume;
        bgmSource.volume = originalVolume * multiplier;

        yield return new WaitForSecondsRealtime(duration);

        if (bgmSource != null)
            bgmSource.volume = originalVolume;

        duckRoutine = null;
    }
}
