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
    [SerializeField] private AudioClip bgmCooking;     // bgm_cooking.mp3 (= "soft click.mp3") - BGM scene Bep
    [SerializeField] private AudioClip uiClick;        // button.wav (tất cả nút)
    [SerializeField] private AudioClip expClip;        // exp.mp3 (kinh nghiệm)
    [SerializeField] private AudioClip plantingClip;   // gieohat.mp3 (gieo hạt & hoa)
    [SerializeField] private AudioClip harvestClip;    // thuhoach.mp3 (thu hoạch nông sản & hoa)
    [SerializeField] private AudioClip coinReward;     // vàng.wav (tiền vàng)
    [SerializeField] private AudioClip ingredientPop;
    [SerializeField] private AudioClip cookStart;
    [SerializeField] private AudioClip successJingle;
    [SerializeField] private AudioClip waterFlowClip;

    [Header("Expanded Pro Clips")]
    [SerializeField] private AudioClip fanfareLevelUp; // fanfare_levelup.wav
    [SerializeField] private AudioClip coinTing;       // coin_ting.wav
    [SerializeField] private AudioClip gemSparkle;     // gem_sparkle.wav
    [SerializeField] private AudioClip bubblePop;      // bubble_pop.wav
    [SerializeField] private AudioClip trainWhistle;   // train_whistle.wav
    [SerializeField] private AudioClip boatHorn;       // boat_horn.wav
    [SerializeField] private AudioClip cookingSizzle;  // cooking_sizzle.wav
    [SerializeField] private AudioClip cookingChop;    // cooking_chop.wav
    [SerializeField] private AudioClip buildingPlace;  // building_place.wav
    [SerializeField] private AudioClip giftUnbox;      // gift_unbox.wav
    [SerializeField] private AudioClip buildingHammer;  // building_hammer.wav
    [SerializeField] private AudioClip touristChatter;  // tourist_chatter.wav
    [SerializeField] private AudioClip characterGreet;  // character_greet.wav

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.35f;     // Nhạc nền rõ ràng, êm dịu
    [Range(0f, 1f)][SerializeField] private float uiVolume = 0.70f;      // Tiếng nút bấm nảy giòn
    [Range(0f, 1f)][SerializeField] private float fxVolume = 0.45f;      // [10/09] 0.85 -> 0.45: Sep bao SFX on qua. Nhan them SfxGain (mac dinh 0.85) => ~0.38
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

        // [FIX 2026-09-06] Ap AM LUONG DA LUU ngay tu dau. Truoc day Awake dung gia tri mac
        // dinh trong Inspector nen thiet lap cua nguoi choi bi bo qua moi lan mo game.
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("SETTING_BGM_VOLUME", bgmVolume));
        SetupSource(bgmSource, true, PlayerPrefs.GetInt("SETTING_BGM_ENABLED", 1) == 1 ? bgmVolume : 0f);
        SetupSource(uiSource, false, 1f);   // giam am o buoc phat (SfxGain)
        SetupSource(fxSource, false, 1f);
        SetupSource(waterAmbienceSource, true, 0f);

        LoadDefaultClipsIfMissing();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        EnsureAudioListener();

        // [THEM 10/09] BGM RIENG CHO SCENE BEP.
        // "SampleScene" la ten THAT cua scene Bep (FarmUIManager.cookingSceneName = "SampleScene").
        // Scene Bep load kieu ADDITIVE nen sceneLoaded chi ban luc VAO; luc RA thi
        // FarmUIManager.ExitCookingMode() goi PlayFarmBGM() tra lai nhac Nong Trai.
        if (scene.name == "SampleScene")
            PlayCookingBGM();
        else
            PlayMainBGM();

        // [THEM 10/09] Nhet moi AudioSource ambience cua scene vua load vao so dang ky SFX,
        // de tieng nuoc / ambience theo thanh truot "Am thanh VFX".
        QuetVaDangKyAmbienceTrongScene(scene);
    }

    private void Start()
    {
        // [FIX 2026-09-04] Start chay SAU khi scene da len ⇒ luc nay Main Camera va
        // AudioListener cua no da ton tai, quet lai de don cai thua.
        EnsureAudioListener();

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.name == "SampleScene")
            PlayCookingBGM();
        else
            PlayMainBGM();

        StartWaterAmbience();
    }

    /// <summary>
    /// [FIX 2026-09-04 — Sếp báo "map cứng đơ, Console 999+ dòng"] Giữ ĐÚNG MỘT AudioListener.
    ///
    /// LỖI CŨ: AutoInit chạy ở BeforeSceneLoad ⇒ khi Awake gọi hàm này thì scene CHƯA load,
    /// FindFirstObjectByType trả null VÀ Camera.main cũng null ⇒ rơi vào nhánh cuối, tự gắn
    /// AudioListener lên chính AudioManager (DontDestroyOnLoad). Sau đó scene load, Main Camera
    /// mang sẵn AudioListener của nó ⇒ THÀNH 2 CÁI. Unity cảnh báo MỖI FRAME
    /// ("There are 2 audio listeners in the scene") ⇒ Console ngập 999+ dòng ⇒ FPS tụt còn ~10
    /// ⇒ kéo map giật cứng. Bản cũ có gọi lại hàm này lúc scene load, nhưng chỉ THÊM khi thiếu,
    /// KHÔNG BAO GIỜ tắt cái thừa ⇒ lỗi tồn tại vĩnh viễn.
    ///
    /// CÁCH CHỮA: quét mọi AudioListener đang bật, giữ lại đúng 1 (ưu tiên cái trên Camera.main
    /// để âm thanh theo đúng vị trí nghe), TẮT — không xoá — những cái còn lại. Tắt thì revert
    /// được và KHÔNG đụng vào prefab Main Camera của Sếp.
    /// </summary>
    private void EnsureAudioListener()
    {
        // Lay MOI AudioListener, ke ca cai dang bi tat (component disabled van duoc tra ve).
        AudioListener[] all = FindObjectsByType<AudioListener>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None);
        Camera main = Camera.main;

        // Dem nhung cai DANG THUC SU HOAT DONG (Unity chi canh bao voi loai nay).
        int soDangBat = 0;
        AudioListener dangBatDauTien = null;
        AudioListener trenMainCam     = null;
        AudioListener cuaChinhMinh    = null;

        for (int i = 0; i < all.Length; i++)
        {
            AudioListener l = all[i];
            if (l == null) continue;
            if (l.gameObject == gameObject) cuaChinhMinh = l;
            if (main != null && l.gameObject == main.gameObject) trenMainCam = l;
            if (l.enabled && l.gameObject.activeInHierarchy)
            {
                soDangBat++;
                if (dangBatDauTien == null) dangBatDauTien = l;
            }
        }

        // ── Truong hop 1: KHONG co cai nao hoat dong ⇒ bat len dung 1 cai ──────────
        // Xay ra khi vao Bep: FarmUIManager tat listener cua camera farm. Neu khong lo
        // thi Unity spam "There are no audio listeners in the scene" va game mat tieng.
        if (soDangBat == 0)
        {
            AudioListener chon = null;
            if (treNull(trenMainCam)) chon = trenMainCam;
            else if (main != null)    chon = main.gameObject.AddComponent<AudioListener>();
            else if (treNull(cuaChinhMinh)) chon = cuaChinhMinh;
            else                      chon = gameObject.AddComponent<AudioListener>();

            if (chon != null && chon.gameObject.activeInHierarchy) chon.enabled = true;
            return;
        }

        // ── Truong hop 2: co NHIEU HON 1 ⇒ giu dung 1, tat phan con lai ────────────
        if (soDangBat <= 1) return;

        AudioListener giuLai = (treNull(trenMainCam) && trenMainCam.enabled &&
                                trenMainCam.gameObject.activeInHierarchy)
                             ? trenMainCam : dangBatDauTien;
        if (giuLai == null) return;

        int daTat = 0;
        for (int i = 0; i < all.Length; i++)
        {
            AudioListener l = all[i];
            if (l == null || l == giuLai) continue;
            if (!l.enabled || !l.gameObject.activeInHierarchy) continue;
            l.enabled = false;
            daTat++;
        }

        if (daTat > 0)
            Debug.Log("[Audio] Da tat " + daTat + " AudioListener thua, giu lai 1 cai tren '" +
                      giuLai.gameObject.name + "'.");
    }

    private static bool treNull(AudioListener l) { return l != null; }

    // [FIX 2026-09-04] Kiem lai dinh ky: chuyen canh Farm <-> Bep bat/tat listener
    // ngoai tam kiem soat cua AudioManager, nen chi kiem luc sceneLoaded la KHONG DU.
    private float _nextListenerCheck;

    private void Update()
    {
        if (Time.unscaledTime >= _nextListenerCheck)
        {
            _nextListenerCheck = Time.unscaledTime + 0.5f;
            EnsureAudioListener();
        }

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

        // [FIX] Tiếng nước là SFX nền chạy vòng lặp, và hàm này GHI ĐÈ volume MỖI KHUNG HÌNH.
        // Không nhân SfxGain ở đây thì dù người chơi kéo thanh SFX về 0 (hay tắt hẳn SFX),
        // khung hình kế tiếp lại kéo volume lên theo khoảng cách camera — nước vẫn chảy ầm ầm.
        targetVol *= SfxGain;

        waterAmbienceSource.volume = Mathf.MoveTowards(waterAmbienceSource.volume, targetVol, Time.unscaledDeltaTime * 0.5f);
    }

    private void LoadDefaultClipsIfMissing()
    {
        // 1. Nạp từ Resources/Audio (ưu tiên cao, chạy được trên cả Build và Editor)
        if (bgmMain == null) bgmMain = Resources.Load<AudioClip>("Audio/Morning_Garden_Waltz");
        // [THEM 10/09] BGM scene Bep. File goc "Assets/_Game/Audio/soft click.mp3" KHONG nam
        // trong Resources nen Resources.Load khong bao gio thay - phai chep sang
        // Assets/Resources/Audio/bgm_cooking.mp3 (ten ASCII, khong dau cach).
        if (bgmCooking == null) bgmCooking = Resources.Load<AudioClip>("Audio/bgm_cooking");
        if (uiClick == null) uiClick = Resources.Load<AudioClip>("Audio/button");
        if (expClip == null) expClip = Resources.Load<AudioClip>("Audio/exp");
        if (plantingClip == null) plantingClip = Resources.Load<AudioClip>("Audio/gieohat");
        if (harvestClip == null) harvestClip = Resources.Load<AudioClip>("Audio/thuhoach");
        if (coinReward == null) coinReward = Resources.Load<AudioClip>("Audio/gold")
            ?? Resources.Load<AudioClip>("Audio/vang")
            ?? Resources.Load<AudioClip>("Audio/vàng");
        if (waterFlowClip == null) waterFlowClip = Resources.Load<AudioClip>("Audio/Ambience/water_flowing");

        if (fanfareLevelUp == null) fanfareLevelUp = Resources.Load<AudioClip>("Audio/fanfare_levelup");
        if (coinTing == null) coinTing = Resources.Load<AudioClip>("Audio/coin_ting");
        if (gemSparkle == null) gemSparkle = Resources.Load<AudioClip>("Audio/gem_sparkle");
        if (bubblePop == null) bubblePop = Resources.Load<AudioClip>("Audio/bubble_pop");
        if (trainWhistle == null) trainWhistle = Resources.Load<AudioClip>("Audio/train_whistle");
        if (boatHorn == null) boatHorn = Resources.Load<AudioClip>("Audio/boat_horn");
        if (cookingSizzle == null) cookingSizzle = Resources.Load<AudioClip>("Audio/cooking_sizzle");
        if (cookingChop == null) cookingChop = Resources.Load<AudioClip>("Audio/cooking_chop");
        if (buildingPlace == null) buildingPlace = Resources.Load<AudioClip>("Audio/building_place");
        if (giftUnbox == null) giftUnbox = Resources.Load<AudioClip>("Audio/gift_unbox");
        if (buildingHammer == null) buildingHammer = Resources.Load<AudioClip>("Audio/building_hammer");
        if (touristChatter == null) touristChatter = Resources.Load<AudioClip>("Audio/tourist_chatter");
        if (characterGreet == null) characterGreet = Resources.Load<AudioClip>("Audio/character_greet");

        // ── [THEM 2026-09-10] 8 FILE AM THANH MOI CUA SEP (Assets/ÂM THANH GAME) ──────────
        // Vi sao phai qua Resources: AutoInit() chay o BeforeSceneLoad va tu tao MOT
        // AudioManager moi, nen moi AudioManager dat san trong Scene deu tu huy o Awake
        // (guard _instance != this). Ket qua: MOI o [SerializeField] AudioClip keo tay
        // trong Inspector deu CHET luc chay. Duong song duy nhat la Resources.Load.
        // Ten canonical ASCII do Tools/Map45/24 chep ra Assets/Resources/Audio/.
        // CHI THEM fallback — moi fallback cu o tren van giu nguyen thu tu uu tien.
        if (buildingHammer == null) buildingHammer = Resources.Load<AudioClip>("Audio/sfx_builder_hammer");
        if (bubblePop == null)      bubblePop      = Resources.Load<AudioClip>("Audio/sfx_bubble_pop");
        if (buildingPlace == null)  buildingPlace  = Resources.Load<AudioClip>("Audio/sfx_building_place");
        if (gemSparkle == null)     gemSparkle     = Resources.Load<AudioClip>("Audio/sfx_gem");
        if (fanfareLevelUp == null) fanfareLevelUp = Resources.Load<AudioClip>("Audio/sfx_levelup");
        if (trainWhistle == null)   trainWhistle   = Resources.Load<AudioClip>("Audio/sfx_train_whistle");
        if (boatHorn == null)       boatHorn       = Resources.Load<AudioClip>("Audio/sfx_boat_horn");
        if (coinTing == null)       coinTing       = Resources.Load<AudioClip>("Audio/sfx_coin");
        // vang.mp3 cung dung duoc cho tieng vang chung neu gold.wav khong co.
        if (coinReward == null)     coinReward     = Resources.Load<AudioClip>("Audio/sfx_coin");

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

        if (fanfareLevelUp == null) fanfareLevelUp = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/fanfare_levelup.wav");
        if (coinTing == null) coinTing = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/coin_ting.wav");
        if (gemSparkle == null) gemSparkle = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/gem_sparkle.wav");
        if (bubblePop == null) bubblePop = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/bubble_pop.wav");
        if (trainWhistle == null) trainWhistle = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/train_whistle.wav");
        if (boatHorn == null) boatHorn = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/boat_horn.wav");
        if (cookingSizzle == null) cookingSizzle = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/cooking_sizzle.wav");
        if (cookingChop == null) cookingChop = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/cooking_chop.wav");
        if (buildingPlace == null) buildingPlace = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/building_place.wav");
        if (giftUnbox == null) giftUnbox = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/gift_unbox.wav");
        if (buildingHammer == null) buildingHammer = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/building_hammer.wav");
        if (touristChatter == null) touristChatter = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/tourist_chatter.wav");
        if (characterGreet == null) characterGreet = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Game/character_greet.wav");
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
        bgmSource.volume = IsBGMEnabled ? bgmVolume : 0f;   // [FIX 10/09] truoc day scene load lam nhac tu bat lai du da TAT
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
    }

    /// <summary>
    /// [THEM 10/09] Phat MOT ban nhac nen bat ky - dung chung cho Nong Trai va Bep.
    /// Giu nguyen quy uoc cua PlayMainBGM: dang phat dung bai roi thi khong cat ngang.
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = IsBGMEnabled ? bgmVolume : 0f;   // ton trong nut TAT NHAC
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
    }

    /// <summary>[THEM 10/09] Tra nhac nen ve bai cua Nong Trai (goi khi thoat scene Bep).</summary>
    public void PlayFarmBGM()
    {
        if (bgmMain == null) LoadDefaultClipsIfMissing();
        PlayBGM(bgmMain);
    }

    /// <summary>[THEM 10/09] Nhac nen rieng cho scene Bep - Resources/Audio/bgm_cooking.</summary>
    public void PlayCookingBGM()
    {
        if (bgmCooking == null) LoadDefaultClipsIfMissing();
        PlayBGM(bgmCooking != null ? bgmCooking : bgmMain);   // thieu file thi giu nhac Farm, khong im lang
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
        fxSource.PlayOneShot(clip, fxVolume * volumeScale * SfxGain);   // [FIX] nhan he so am luong
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
            uiSource.PlayOneShot(uiClick, uiVolume * SfxGain);           // [FIX] tieng bam nut cung phai theo thanh truot
        }
    }

    /// <summary>🔘 Alias cho PlayUIClick()</summary>
    public void PlayButton()
    {
        PlayUIClick();
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

    /// <summary>🎺 Nhạc Fanfare chúc mừng Lên Cấp rực rỡ (fanfare_levelup.wav)</summary>
    public void PlayLevelUpFanfare()
    {
        DuckBGM(0.6f, 1.5f);
        if (fanfareLevelUp == null) LoadDefaultClipsIfMissing();
        PlayFX(fanfareLevelUp != null ? fanfareLevelUp : successJingle, 1f, 1f, 1f);
    }

    /// <summary>🔔 Tiếng Vàng Ting leng keng khi chạm HUD (coin_ting.wav)</summary>
    public void PlayCoinTing()
    {
        if (Time.unscaledTime - lastCoinTime < coinCooldown) return;
        lastCoinTime = Time.unscaledTime;
        if (coinTing == null) LoadDefaultClipsIfMissing();
        PlayFX(coinTing != null ? coinTing : coinReward, 0.95f, 0.98f, 1.04f);
    }

    /// <summary>💎 Tiếng Kim Cương lấp lánh (gem_sparkle.wav)</summary>
    public void PlayGemSparkle()
    {
        if (gemSparkle == null) LoadDefaultClipsIfMissing();
        PlayFX(gemSparkle != null ? gemSparkle : expClip, 0.7f, 0.98f, 1.02f);
    }

    /// <summary>🫧 Tiếng Bong Bóng nổ / Pop (bubble_pop.wav)</summary>
    public void PlayBubblePop()
    {
        if (bubblePop == null) LoadDefaultClipsIfMissing();
        PlayFX(bubblePop != null ? bubblePop : uiClick, 0.9f, 0.95f, 1.05f);
    }

    /// <summary>🚂 Tiếng Còi Tàu Hỏa xình xịch (train_whistle.wav)</summary>
    public void PlayTrainWhistle()
    {
        DuckBGM(0.75f, 1.2f);
        if (trainWhistle == null) LoadDefaultClipsIfMissing();
        // [FIX 2026-09-11] Giảm volume còi tàu từ 0.45f xuống 0.18f để âm thanh êm dịu, không gây ồn chói tai
        PlayFX(trainWhistle != null ? trainWhistle : successJingle, 0.18f, 0.98f, 1.02f);
    }

    /// <summary>🚢 Tiếng Còi Tàu Thủy Du Lịch cập bến (boat_horn.wav)</summary>
    public void PlayBoatHorn()
    {
        DuckBGM(0.75f, 1.2f);
        if (boatHorn == null) LoadDefaultClipsIfMissing();
        PlayFX(boatHorn != null ? boatHorn : successJingle, 0.45f, 0.98f, 1.02f);
    }

    /// <summary>🍳 Tiếng Nấu Ăn xèo xèo (cooking_sizzle.wav)</summary>
    public void PlayCookingSizzle()
    {
        if (cookingSizzle == null) LoadDefaultClipsIfMissing();
        PlayFX(cookingSizzle != null ? cookingSizzle : cookStart, 0.85f, 0.97f, 1.03f);
    }

    /// <summary>🔪 Tiếng Băm Chặt / Thái rau củ trên thớt (cooking_chop.wav)</summary>
    public void PlayCookingChop()
    {
        if (cookingChop == null) LoadDefaultClipsIfMissing();
        PlayFX(cookingChop != null ? cookingChop : uiClick, 0.9f, 0.95f, 1.05f);
    }

    /// <summary>📦 Tiếng Đặt Công Trình / Đồ Trang Trí xuống đất (building_place.wav)</summary>
    public void PlayBuildingPlace()
    {
        if (buildingPlace == null) LoadDefaultClipsIfMissing();
        PlayFX(buildingPlace != null ? buildingPlace : uiClick, 0.95f, 0.95f, 1.05f);
    }

    /// <summary>🎁 Tiếng Bung Quà / Ăn mừng mở hộp (gift_unbox.wav)</summary>
    public void PlayGiftUnbox()
    {
        DuckBGM(0.4f, 1.2f);
        if (giftUnbox == null) LoadDefaultClipsIfMissing();
        PlayFX(giftUnbox != null ? giftUnbox : successJingle, 1f, 0.98f, 1.02f);
    }

    /// <summary>🔨 Tiếng Xây Dựng / Đập Búa đóng đinh (building_hammer.wav)</summary>
    public void PlayBuildingHammer()
    {
        if (buildingHammer == null) LoadDefaultClipsIfMissing();
        PlayFX(buildingHammer != null ? buildingHammer : uiClick, 0.55f, 0.96f, 1.04f);
    }

    /// <summary>👥 Tiếng Khách Du Lịch nói cười ríu rít khi xuống bến (tourist_chatter.wav)</summary>
    public void PlayTouristChatter()
    {
        DuckBGM(0.85f, 1.0f);
        if (touristChatter == null) LoadDefaultClipsIfMissing();
        PlayFX(touristChatter != null ? touristChatter : successJingle, 0.6f, 0.98f, 1.02f);
    }

    /// <summary>👋 Tiếng Nhân Vật chào & huýt sáo khi zoom tới / tương tác (character_greet.wav)</summary>
    public void PlayCharacterGreet()
    {
        if (characterGreet == null) LoadDefaultClipsIfMissing();
        PlayFX(characterGreet != null ? characterGreet : uiClick, 1f, 0.98f, 1.04f);
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

        // [FIX 10/09] Truoc day thieu dong nay: sau tieng coi tau/thuyen/fanfare dau tien,
        // BGM bi ket o 30-60% MAI MAI, lam moi SFX sau do nghe to len tuong ung.
        bgmSource.volume = IsBGMEnabled ? bgmVolume : 0f;

        duckRoutine = null;
    }

    // ─── Settings Controls (Music / SFX / Mute) ─────────────────────────────

    public float BGMVolume
    {
        get => PlayerPrefs.GetFloat("SETTING_BGM_VOLUME", bgmVolume);
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("SETTING_BGM_VOLUME", bgmVolume);
            if (bgmSource != null)
                bgmSource.volume = IsBGMEnabled ? bgmVolume : 0f;
        }
    }

    /// <summary>
    /// [FIX 2026-09-06] HE SO AM LUONG HIEU LUC cho MOI tieng dong (0..1).
    /// Moi noi phat tieng — ke ca AudioSource nam ngoai AudioManager (gia suc, tho xay) —
    /// deu phai nhan voi so nay thi keo thanh truot moi that su nho di.
    /// Tat SFX => 0.
    /// </summary>
    public static float SfxGain
    {
        get
        {
            if (PlayerPrefs.GetInt("SETTING_SFX_ENABLED", 1) != 1) return 0f;
            return Mathf.Clamp01(PlayerPrefs.GetFloat("SETTING_SFX_VOLUME", 0.85f));
        }
    }

    /// <summary>Ban khi nguoi choi keo thanh truot / bat tat SFX — cho AudioSource ngoai tu chinh lai.</summary>
    public static event System.Action<float> OnSfxGainChanged;

    public float SFXVolume
    {
        get => PlayerPrefs.GetFloat("SETTING_SFX_VOLUME", fxVolume);
        set
        {
            // [FIX 2026-09-06] Truoc day gan ca fxSource.volume VA nhan them fxVolume luc
            // PlayOneShot ⇒ am luong bi BINH PHUONG (0.5 nghe ra 0.25). Rieng uiSource con
            // te hon: uiVolume KHONG he theo thanh truot nen tieng bam nut khong bao gio nho di.
            // Nay: source.volume = 1, moi thu nhan SfxGain mot lan duy nhat luc phat.
            float v = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("SETTING_SFX_VOLUME", v);
            PlayerPrefs.Save();
            ApDungAmLuongSfx();
            OnSfxGainChanged?.Invoke(SfxGain);
        }
    }

    /// <summary>Dua 2 nguon SFX ve chuan: volume = 1, viec giam am do SfxGain lo luc phat.</summary>
    private void ApDungAmLuongSfx()
    {
        if (fxSource != null) fxSource.volume = 1f;
        if (uiSource != null) uiSource.volume = 1f;
        ApDungChoNguonNgoai();                    // [FIX] kéo theo cả các nguồn loop bên ngoài
    }

    // ════════════════════════════════════════════════════════════════════════
    // SỔ ĐĂNG KÝ NGUỒN SFX NGOÀI (AudioSource loop nằm ngoài AudioManager)
    // ════════════════════════════════════════════════════════════════════════
    //
    // VÌ SAO CẦN: SfxGain chỉ có tác dụng ở nơi NÀO GỌI PlayOneShot — tức là tiếng phát
    // một nhát. Còn AudioSource bật loop (gia súc, guồng nước, máy xay…) tự đặt volume
    // một lần rồi kêu mãi, kéo thanh trượt SFX chẳng ảnh hưởng gì tới nó.
    // Ai có nguồn loop kiểu đó thì DangKyNguonSfx(src, âmLượngGốc) một lần lúc bật;
    // từ đó mỗi lần người chơi chỉnh thanh trượt, volume được tính lại = gốc × SfxGain.

    // (file đã có `using System.Collections.Generic;` ở đầu — dùng thẳng List/Dictionary)
    private static readonly List<AudioSource>              _nguonSfxNgoai   = new List<AudioSource>();
    private static readonly Dictionary<AudioSource, float> _amLuongGocNgoai = new Dictionary<AudioSource, float>();

    /// <summary>Đăng ký một AudioSource loop để nó theo thanh trượt SFX. Gọi lại là cập nhật âm lượng gốc.</summary>
    public static void DangKyNguonSfx(AudioSource src, float amLuongGoc = 1f)
    {
        if (src == null) return;

        amLuongGoc = Mathf.Clamp01(amLuongGoc);
        if (!_amLuongGocNgoai.ContainsKey(src)) _nguonSfxNgoai.Add(src);
        _amLuongGocNgoai[src] = amLuongGoc;

        src.volume = amLuongGoc * SfxGain;
    }

    /// <summary>Bỏ đăng ký (gọi lúc OnDisable/OnDestroy của chủ nguồn). Không bắt buộc — huỷ rồi sẽ tự bị dọn.</summary>
    public static void HuyDangKyNguonSfx(AudioSource src)
    {
        if (src == null) return;
        _nguonSfxNgoai.Remove(src);
        _amLuongGocNgoai.Remove(src);
    }

    /// <summary>Tính lại volume cho mọi nguồn ngoài còn sống, đồng thời dọn nguồn đã bị huỷ.</summary>
    private static void ApDungChoNguonNgoai()
    {
        float gain = SfxGain;

        for (int i = _nguonSfxNgoai.Count - 1; i >= 0; i--)
        {
            AudioSource src = _nguonSfxNgoai[i];
            if (src == null)                       // GameObject đã bị huỷ → dọn khỏi sổ
            {
                _amLuongGocNgoai.Remove(src);      // dọn cả bảng âm lượng gốc, khỏi rò rỉ
                _nguonSfxNgoai.RemoveAt(i);
                continue;
            }

            float goc = _amLuongGocNgoai.TryGetValue(src, out float v) ? v : 1f;
            src.volume = goc * gain;
        }

        // Dọn nốt các khoá trỏ tới nguồn đã huỷ để Dictionary không phình mãi.
        if (_amLuongGocNgoai.Count > _nguonSfxNgoai.Count)
        {
            var chet = new List<AudioSource>();
            foreach (var kv in _amLuongGocNgoai)
                if (kv.Key == null) chet.Add(kv.Key);
            for (int i = 0; i < chet.Count; i++) _amLuongGocNgoai.Remove(chet[i]);
        }
    }

    // ============================================================================
    // [THEM 10/09] QUET AMBIENCE TRONG SCENE -> NHET VAO SO DANG KY SFX
    // ============================================================================
    // VI SAO: SCN_Farm co 4 AudioSource tieng nuoc (loop + playOnAwake, spatialBlend 0,
    // volume 0.35 / 0.35 / 0.25 / 0.25) nam trong scene va trong prefab
    // DayNightWeatherSetup. KHONG AI goi DangKyNguonSfx cho chung nen keo thanh truot
    // "Am thanh VFX" ve 0 thi nuoc VAN chay am am. Quet o sceneLoaded roi dang ky ho,
    // khoi phai sua file scene 17 MB bang tay.
    //
    // CACH NHAN DIEN: uu tien LUAT CAU TRUC (loop && playOnAwake) vi no khong the bo sot
    // du ai doi ten file clip; danh sach ten chi la luoi phu cho nguon loop khong
    // playOnAwake. Runtime KHONG doc duoc GUID (AssetDatabase la editor-only).
    //
    // CHONG RATCHET: chi lay volume hien tai lam "am luong goc" LAN DAU (khi
    // _amLuongGocNgoai chua co key). Load lai scene sinh AudioSource MOI (identity khac)
    // nen lay dung volume tac gia; nguon cu da huy thi ApDungChoNguonNgoai() tu don.
    //
    // KHONG DUNG TOI: 4 nguon cua chinh AudioManager, va 3 nguon ambience Ngay/Dem/Mua
    // do DayNightCycleController ghi volume MOI KHUNG HINH (chung tu nhan SfxGain roi).

    private static readonly string[] _tenClipAmbience =
    {
        "water flowing",
        "water_flowing",
        "rain",
        "thunder",
        "background ambience outside - day",
        "background ambience outside - night",
    };

    /// <summary>Quet moi AudioSource ambience cua scene vua load va dang ky vao so SfxGain.</summary>
    private void QuetVaDangKyAmbienceTrongScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        // Nhung nguon do DayNightCycleController tu quan (ghi volume moi frame) -> bo qua.
        var doDayNightQuanLy = new HashSet<AudioSource>();
        var dnc = FindObjectsByType<Day_Night.DayNightCycleController>(
                      FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < dnc.Length; i++)
        {
            if (dnc[i] == null) continue;
            if (dnc[i].DayAmbience   != null) doDayNightQuanLy.Add(dnc[i].DayAmbience);
            if (dnc[i].NightAmbience != null) doDayNightQuanLy.Add(dnc[i].NightAmbience);
            if (dnc[i].RainAmbience  != null) doDayNightQuanLy.Add(dnc[i].RainAmbience);
        }

        GameObject[] goc = scene.GetRootGameObjects();
        int soDangKy = 0;

        for (int r = 0; r < goc.Length; r++)
        {
            if (goc[r] == null) continue;

            AudioSource[] ds = goc[r].GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < ds.Length; i++)
            {
                AudioSource s = ds[i];
                if (s == null) continue;

                // 1. Khong dung 4 nguon cua chinh AudioManager.
                if (s == bgmSource || s == uiSource || s == fxSource || s == waterAmbienceSource) continue;
                if (s.transform.IsChildOf(transform)) continue;

                // 2. Khong dung nguon do DayNightCycleController ghi de moi frame.
                if (doDayNightQuanLy.Contains(s)) continue;

                // 3. DA dang ky roi -> TUYET DOI khong lay volume hien tai lam goc nua
                //    (neu lay se nhan SfxGain hai lan -> am luong tut dan ve 0).
                if (_amLuongGocNgoai.ContainsKey(s)) continue;

                if (!LaNguonAmbience(s)) continue;

                DangKyNguonSfx(s, s.volume);   // s.volume luc nay VAN la volume tac gia dat trong scene
                soDangKy++;
            }
        }

        ApDungChoNguonNgoai();                 // don nguon da huy + dong bo lai theo thanh truot

        if (soDangKy > 0)
            Debug.Log("[Audio] Da dang ky " + soDangKy + " nguon ambience cua scene '" +
                      scene.name + "' vao thanh truot SFX.");
    }

    /// <summary>Nguon ambience = loop tu chay nen, hoac ten clip nam trong danh sach ambience.</summary>
    private static bool LaNguonAmbience(AudioSource s)
    {
        if (s.loop && s.playOnAwake) return true;      // luat CAU TRUC - bat het 4 nguon nuoc

        AudioClip c = s.clip;
        if (c == null) return false;

        string ten = c.name.ToLowerInvariant();
        for (int i = 0; i < _tenClipAmbience.Length; i++)
            if (ten == _tenClipAmbience[i]) return true;

        return false;
    }

    public bool IsBGMEnabled
    {
        get => PlayerPrefs.GetInt("SETTING_BGM_ENABLED", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("SETTING_BGM_ENABLED", value ? 1 : 0);
            if (bgmSource != null)
                bgmSource.volume = value ? BGMVolume : 0f;
        }
    }

    public bool IsSFXEnabled
    {
        get => PlayerPrefs.GetInt("SETTING_SFX_ENABLED", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("SETTING_SFX_ENABLED", value ? 1 : 0);
            PlayerPrefs.Save();
            ApDungAmLuongSfx();                       // [FIX] volume = 1, SfxGain lo phan con lai
            OnSfxGainChanged?.Invoke(SfxGain);
        }
    }
}
