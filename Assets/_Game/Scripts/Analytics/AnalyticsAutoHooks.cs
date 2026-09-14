using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TU DONG ban cac event quan trong nhat MA KHONG SUA MOT DONG NAO cua gameplay.
///
/// Cach hoat dong: [RuntimeInitializeOnLoadMethod] dung mot GameObject an
/// (DontDestroyOnLoad) ngay truoc khi scene dau tien load, roi DANG KY nghe cac
/// event tinh/instance DA CO SAN trong du an. Khong file gameplay nao phai doi.
///
/// EVENT TU BAN (da doi chieu voi ma nguon that trong du an):
///   game_start            — luc boot: nen tang, do phan giai, ngon ngu, phien ban
///   scene_view            — SceneManager.sceneLoaded
///   level_up              — PlayerProgressManager.OnLevelChanged (event instance, nen phai buoc muon)
///   level_up_popup_closed — LevelUpPopupUI.OnAllClosed
///   region_unlocked       — LandExpansionManager.OnRegionUnlocked
///   crop_planted          — FarmManager.OnPlotPlantedEvent   (co han toc do)
///   crop_harvested        — FarmManager.OnPlotHarvestedEvent (co han toc do)
///   language_changed      — LocalizationManager.OnChanged + SettingsPopupUI.OnLanguageChanged
///   session_heartbeat     — moi 60 giay, toi da 60 lan (cach duy nhat do do dai phien tren web)
///   session_end           — thoat game / roi tab / an nen
///   session_resume        — quay lai tab
///
/// KHONG ban rieng tung event cho: cong EXP (OnExpAddedFx) va hang cho khach du lich
/// (TouristVisitorManager.OnQueueOrderChanged) — hai cai nay ban qua nhieu lan se
/// dot quota GA4. Chung duoc CONG DON va gui kem trong session_heartbeat / session_end.
/// </summary>
[DisallowMultipleComponent]
public class AnalyticsAutoHooks : MonoBehaviour
{
    // =========================================================================
    //  Bootstrap
    // =========================================================================

    private const string HostObjectName = "[GameAnalytics]";

    private static AnalyticsAutoHooks _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        GameAnalytics.Init();

        var go = new GameObject(HostObjectName);
        go.hideFlags = HideFlags.HideInHierarchy; // khong lam ban Hierarchy cua Sep
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<AnalyticsAutoHooks>();
    }

    // =========================================================================
    //  Trang thai
    // =========================================================================

    private float _sessionStartTime;
    private bool  _sessionEndSent;
    private int   _lastLevel = -1;
    private int   _expAccum;
    private int   _plantCount;
    private int   _harvestCount;
    private int   _queueChangeCount;
    private int   _sceneCount;
    private int   _heartbeatCount;
    private string _lastScene = "";
    private string _lastLanguage = "";

    /// <summary>Han toc do: ten event -> thoi diem som nhat duoc ban lai (giay, realtime).</summary>
    private readonly Dictionary<string, float> _nextAllowed = new Dictionary<string, float>();

    private const float RateLimitSeconds  = 20f; // event lap lai nhieu (trong/thu hoach)
    private const float HeartbeatSeconds  = 60f;
    private const int   HeartbeatMaxCount = 60;  // toi da 1 gio ping, du de chung minh phien dai

    private bool _hookedProgress;

    // =========================================================================
    //  Vong doi
    // =========================================================================

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        _sessionStartTime = Time.realtimeSinceStartup;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

        LevelUpPopupUI.OnAllClosed       += HandleLevelUpPopupClosed;
        LandExpansionManager.OnRegionUnlocked += HandleRegionUnlocked;
        FarmManager.OnPlotPlantedEvent   += HandlePlotPlanted;
        FarmManager.OnPlotHarvestedEvent += HandlePlotHarvested;
        TouristVisitorManager.OnQueueOrderChanged += HandleQueueChanged;
        PlayerProgressManager.OnExpAddedFx += HandleExpAdded;
        LocalizationManager.OnChanged    += HandleLanguageChanged;
        SettingsPopupUI.OnLanguageChanged += HandleLanguageChanged;

        Application.wantsToQuit += HandleWantsToQuit;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        LevelUpPopupUI.OnAllClosed       -= HandleLevelUpPopupClosed;
        LandExpansionManager.OnRegionUnlocked -= HandleRegionUnlocked;
        FarmManager.OnPlotPlantedEvent   -= HandlePlotPlanted;
        FarmManager.OnPlotHarvestedEvent -= HandlePlotHarvested;
        TouristVisitorManager.OnQueueOrderChanged -= HandleQueueChanged;
        PlayerProgressManager.OnExpAddedFx -= HandleExpAdded;
        LocalizationManager.OnChanged    -= HandleLanguageChanged;
        SettingsPopupUI.OnLanguageChanged -= HandleLanguageChanged;

        Application.wantsToQuit -= HandleWantsToQuit;

        UnhookProgress();
    }

    private void Start()
    {
        SendGameStart();
        StartCoroutine(HeartbeatLoop());
        StartCoroutine(BindProgressManagerWhenReady());
    }

    // =========================================================================
    //  game_start
    // =========================================================================

    private void SendGameStart()
    {
        _lastLanguage = SafeLanguage();

        GameAnalytics.Log("game_start",
            ("platform",      Application.platform.ToString()),
            ("screen_w",      Screen.width),
            ("screen_h",      Screen.height),
            ("dpi",           Mathf.RoundToInt(Screen.dpi)),
            ("language",      _lastLanguage),
            ("sys_language",  Application.systemLanguage.ToString()),
            ("app_version",   Application.version),
            ("unity_version", Application.unityVersion),
            ("first_open",    GameAnalytics.IsFirstOpen),
            ("js_ready",      GameAnalytics.JsReady));

        GameAnalytics.SetUserProperty("game_platform", Application.platform.ToString());
        GameAnalytics.SetUserProperty("game_language", _lastLanguage);
    }

    private static string SafeLanguage()
    {
        // LocalizationManager la static class, luon co san; van bao try/catch
        // vi no doc PlayerPrefs khi truy cap lan dau.
        try { return LocalizationManager.Current ?? "vi"; }
        catch (Exception) { return "vi"; }
    }

    // =========================================================================
    //  scene_view
    // =========================================================================

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _sceneCount++;
        _lastScene = scene.name;

        GameAnalytics.Log("scene_view",
            ("scene_name", scene.name),
            ("load_mode",  mode.ToString()),
            ("scene_seq",  _sceneCount),
            ("t_sec",      Mathf.RoundToInt(SessionSeconds())));
    }

    // =========================================================================
    //  level_up — PlayerProgressManager.OnLevelChanged la event INSTANCE, ma
    //  Instance chua chac ton tai o frame dau => phai cho roi moi buoc vao.
    // =========================================================================

    private IEnumerator BindProgressManagerWhenReady()
    {
        // Cho toi da ~30 giay. Neu scene dau khong co PlayerProgressManager
        // (vd man hinh Home) thi vong lap van tiep tuc sang scene sau.
        float deadline = Time.realtimeSinceStartup + 30f;

        while (!_hookedProgress)
        {
            var pm = PlayerProgressManager.Instance;
            if (pm != null)
            {
                pm.OnLevelChanged += HandleLevelChanged;
                _hookedProgress = true;

                _lastLevel = pm.Level;
                GameAnalytics.SetUserProperty("player_level", _lastLevel);
                break;
            }

            if (Time.realtimeSinceStartup > deadline)
            {
                // Reset han cho va thu tiep — scene gameplay co the load rat muon.
                deadline = Time.realtimeSinceStartup + 30f;
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    private void UnhookProgress()
    {
        if (!_hookedProgress) return;
        var pm = PlayerProgressManager.Instance;
        if (pm != null) pm.OnLevelChanged -= HandleLevelChanged;
        _hookedProgress = false;
    }

    private void HandleLevelChanged(int newLevel)
    {
        int from = _lastLevel;
        _lastLevel = newLevel;

        // Lan dau buoc day (RaiseAll luc Start) khong phai "len cap" that.
        if (from < 0 || newLevel <= from) return;

        GameAnalytics.Log("level_up",
            ("level",      newLevel),
            ("from_level", from),
            ("t_sec",      Mathf.RoundToInt(SessionSeconds())),
            ("scene",      _lastScene));

        GameAnalytics.SetUserProperty("player_level", newLevel);
    }

    private void HandleLevelUpPopupClosed()
    {
        GameAnalytics.Log("level_up_popup_closed",
            ("level", _lastLevel),
            ("t_sec", Mathf.RoundToInt(SessionSeconds())));
    }

    // =========================================================================
    //  Mo rong dat
    // =========================================================================

    private void HandleRegionUnlocked(LandRegionData region)
    {
        if (region == null) return;

        GameAnalytics.Log("region_unlocked",
            ("region_id",    region.regionId),
            ("unlock_level", region.unlockLevel),
            ("gold_price",   region.goldPrice),
            ("gem_price",    region.gemPrice),
            ("cells",        region.CellCount),
            ("t_sec",        Mathf.RoundToInt(SessionSeconds())));
    }

    // =========================================================================
    //  Nong trai — co han toc do vi nguoi choi trong/thu hoach lien tuc
    // =========================================================================

    private void HandlePlotPlanted(PlotController plot)
    {
        _plantCount++;
        if (!PassRateLimit("crop_planted")) return;
        GameAnalytics.Log("crop_planted", ("total_this_session", _plantCount));
    }

    private void HandlePlotHarvested(PlotController plot)
    {
        _harvestCount++;
        if (!PassRateLimit("crop_harvested")) return;
        GameAnalytics.Log("crop_harvested", ("total_this_session", _harvestCount));
    }

    private void HandleQueueChanged()
    {
        _queueChangeCount++; // chi dem, khong ban — qua on
    }

    private void HandleExpAdded(int amount)
    {
        if (amount > 0) _expAccum += amount; // cong don, gui kem heartbeat / session_end
    }

    // =========================================================================
    //  Ngon ngu
    // =========================================================================

    private void HandleLanguageChanged(string lang)
    {
        if (string.IsNullOrEmpty(lang)) return;
        if (lang == _lastLanguage) return; // hai nguon cung ban -> chong trung

        string from = _lastLanguage;
        _lastLanguage = lang;

        GameAnalytics.Log("language_changed", ("to", lang), ("from", from));
        GameAnalytics.SetUserProperty("game_language", lang);
    }

    // =========================================================================
    //  Nhip tim + ket phien
    // =========================================================================

    private IEnumerator HeartbeatLoop()
    {
        var wait = new WaitForSecondsRealtime(HeartbeatSeconds);

        while (_heartbeatCount < HeartbeatMaxCount)
        {
            yield return wait;
            if (_sessionEndSent) continue;

            _heartbeatCount++;
            GameAnalytics.Log("session_heartbeat",
                ("minutes",   _heartbeatCount),
                ("level",     _lastLevel),
                ("scene",     _lastScene),
                ("exp_total", _expAccum),
                ("planted",   _plantCount),
                ("harvested", _harvestCount));
        }
    }

    private float SessionSeconds() => Mathf.Max(0f, Time.realtimeSinceStartup - _sessionStartTime);

    private void SendSessionEnd(string reason)
    {
        if (_sessionEndSent) return;
        _sessionEndSent = true;

        GameAnalytics.Log("session_end",
            ("reason",        reason),
            ("duration_sec",  Mathf.RoundToInt(SessionSeconds())),
            ("last_level",    _lastLevel),
            ("last_scene",    _lastScene),
            ("scenes_seen",   _sceneCount),
            ("exp_total",     _expAccum),
            ("planted",       _plantCount),
            ("harvested",     _harvestCount),
            ("queue_changes", _queueChangeCount),
            ("events_sent",   GameAnalytics.EventCount));
    }

    private bool HandleWantsToQuit()
    {
        SendSessionEnd("quit");
        return true; // KHONG chan thoat
    }

    private void OnApplicationPause(bool paused)
    {
        // Tren WebGL day la luc nguoi choi doi tab / khoa may — coi nhu ket phien.
        if (paused) SendSessionEnd("pause");
        else ResumeSession("unpause");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SendSessionEnd("blur");
        else ResumeSession("focus");
    }

    private void ResumeSession(string reason)
    {
        if (!_sessionEndSent) return;

        _sessionEndSent = false;
        GameAnalytics.Log("session_resume",
            ("reason", reason),
            ("t_sec",  Mathf.RoundToInt(SessionSeconds())));
    }

    private void OnApplicationQuit()
    {
        SendSessionEnd("app_quit");
    }

    // =========================================================================
    //  Tien ich
    // =========================================================================

    private bool PassRateLimit(string eventName)
    {
        float now = Time.realtimeSinceStartup;
        if (_nextAllowed.TryGetValue(eventName, out float allowedAt) && now < allowedAt)
            return false;

        _nextAllowed[eventName] = now + RateLimitSeconds;
        return true;
    }
}
