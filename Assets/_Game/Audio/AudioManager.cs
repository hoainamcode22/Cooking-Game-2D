using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource fxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip bgmMain;
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip ingredientPop;
    [SerializeField] private AudioClip cookStart;
    [SerializeField] private AudioClip successJingle;
    [SerializeField] private AudioClip coinReward;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.12f;
    [Range(0f, 1f)][SerializeField] private float uiVolume = 0.22f;
    [Range(0f, 1f)][SerializeField] private float fxVolume = 0.45f;

    [Header("Fast UI Click")]
    [SerializeField] private float uiClickCooldown = 0.05f;
    [SerializeField] private float ingredientCooldown = 0.04f;
    [SerializeField] private float uiCutoffTime = 0.08f;

    private float lastUIClickTime = -999f;
    private float lastIngredientTime = -999f;
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

        SetupSource(bgmSource, true, bgmVolume);
        SetupSource(uiSource, false, uiVolume);
        SetupSource(fxSource, false, fxVolume);
    }

    private void Start()
    {
        PlayMainBGM();
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
        AudioClip clipToPlay = coinReward != null ? coinReward : uiClick;
        PlayFX(clipToPlay, 0.75f, 1.05f, 1.12f);
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