using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource fxSource;
    [SerializeField] private AudioSource waterAmbienceSource;

    [Header("Core Clips")]
    [SerializeField] private AudioClip bgmMain;
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip ingredientPop;
    [SerializeField] private AudioClip cookStart;
    [SerializeField] private AudioClip successJingle;
    [SerializeField] private AudioClip coinReward;

    [Header("Farm & Economy Clips")]
    [SerializeField] private AudioClip plantingClip;
    [SerializeField] private AudioClip harvestClip;
    [SerializeField] private AudioClip buySellClip;
    [SerializeField] private AudioClip waterFlowClip;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.12f;
    [Range(0f, 1f)][SerializeField] private float uiVolume = 0.22f;
    [Range(0f, 1f)][SerializeField] private float fxVolume = 0.45f;
    [Range(0f, 1f)][SerializeField] private float waterVolume = 0.25f;

    [Header("Fast UI Click")]
    [SerializeField] private float uiClickCooldown = 0.05f;
    [SerializeField] private float ingredientCooldown = 0.04f;
    [SerializeField] private float farmCooldown = 0.06f;
    [SerializeField] private float uiCutoffTime = 0.08f;

    private float lastUIClickTime = -999f;
    private float lastIngredientTime = -999f;
    private float lastFarmActionTime = -999f;
    private Coroutine duckRoutine;
    private Coroutine uiStopRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null) bgmSource = CreateChildSource("BGM_Source");
        if (uiSource == null) uiSource = CreateChildSource("UI_Source");
        if (fxSource == null) fxSource = CreateChildSource("FX_Source");
        if (waterAmbienceSource == null) waterAmbienceSource = CreateChildSource("Water_Ambience_Source");

        SetupSource(bgmSource, true, bgmVolume);
        SetupSource(uiSource, false, uiVolume);
        SetupSource(fxSource, false, fxVolume);
        SetupSource(waterAmbienceSource, true, waterVolume);

        LoadDefaultClipsIfMissing();
    }

    private void Start()
    {
        PlayMainBGM();
        StartWaterAmbience();
    }

    private void LoadDefaultClipsIfMissing()
    {
#if UNITY_EDITOR
        if (plantingClip == null)
            plantingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Audio/Planting/Planting crop.wav");

        if (harvestClip == null)
            harvestClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Audio/Planting/Picking up crop.wav");

        if (buySellClip == null)
            buySellClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Day_Night/Audio/UI/Buy _ Sell.wav");

        if (waterFlowClip == null)
            waterFlowClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assetsgame/Bò/HappyHarvest_Copy/Audio/Ambience/Water flowing.wav");
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
    }

    public void PlayMainBGM()
    {
        if (bgmMain == null || bgmSource == null) return;

        if (bgmSource.clip == bgmMain && bgmSource.isPlaying)
            return;

        bgmSource.clip = bgmMain;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StartWaterAmbience()
    {
        if (waterFlowClip == null || waterAmbienceSource == null) return;
        if (waterAmbienceSource.clip == waterFlowClip && waterAmbienceSource.isPlaying) return;

        waterAmbienceSource.clip = waterFlowClip;
        waterAmbienceSource.volume = waterVolume;
        waterAmbienceSource.Play();
    }

    private void PlayUIInterrupt(AudioClip clip, float volumeScale = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (clip == null || uiSource == null) return;

        uiSource.Stop();
        uiSource.clip = clip;
        uiSource.volume = uiVolume * volumeScale;
        uiSource.pitch = Random.Range(pitchMin, pitchMax);
        uiSource.Play();

        if (uiStopRoutine != null)
            StopCoroutine(uiStopRoutine);

        uiStopRoutine = StartCoroutine(StopUIAfter(uiCutoffTime));
    }

    private IEnumerator StopUIAfter(float time)
    {
        yield return new WaitForSecondsRealtime(time);
        if (uiSource != null && uiSource.isPlaying)
            uiSource.Stop();
    }

    private void PlayFX(AudioClip clip, float volumeScale = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (clip == null || fxSource == null) return;

        fxSource.pitch = Random.Range(pitchMin, pitchMax);
        fxSource.PlayOneShot(clip, fxVolume * volumeScale);
    }

    public void PlayUIClick()
    {
        if (Time.unscaledTime - lastUIClickTime < uiClickCooldown)
            return;

        lastUIClickTime = Time.unscaledTime;
        PlayUIInterrupt(uiClick, 0.75f, 1.05f, 1.12f);
    }

    public void PlayIngredientPop()
    {
        if (Time.unscaledTime - lastIngredientTime < ingredientCooldown)
            return;

        lastIngredientTime = Time.unscaledTime;
        AudioClip clipToPlay = ingredientPop != null ? ingredientPop : uiClick;
        PlayUIInterrupt(clipToPlay, 0.7f, 1.08f, 1.16f);
    }

    /// <summary>🌱 Tiếng gieo hạt giống / cho vật nuôi ăn</summary>
    public void PlayPlanting()
    {
        if (Time.unscaledTime - lastFarmActionTime < farmCooldown) return;
        lastFarmActionTime = Time.unscaledTime;

        AudioClip clip = plantingClip != null ? plantingClip : ingredientPop;
        PlayFX(clip, 0.85f, 0.95f, 1.08f);
    }

    /// <summary>🧺 Tiếng nhổ / thu hoạch nông sản & sản phẩm chuồng trại</summary>
    public void PlayHarvest()
    {
        if (Time.unscaledTime - lastFarmActionTime < farmCooldown) return;
        lastFarmActionTime = Time.unscaledTime;

        AudioClip clip = harvestClip != null ? harvestClip : successJingle;
        PlayFX(clip, 0.9f, 0.95f, 1.05f);
    }

    /// <summary>💰 Tiếng tiền vàng / mua bán trong Market & Shop</summary>
    public void PlayBuySell()
    {
        AudioClip clip = buySellClip != null ? buySellClip : coinReward;
        PlayFX(clip, 0.85f, 0.98f, 1.06f);
    }

    public void PlayCookStart()
    {
        DuckBGM(0.55f, 0.25f);
        PlayFX(cookStart, 0.75f, 1f, 1.03f);
    }

    public void PlaySuccess()
    {
        DuckBGM(0.45f, 0.5f);
        PlayFX(successJingle, 0.8f, 1f, 1.02f);
    }

    public void PlayCoinReward()
    {
        PlayBuySell();
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