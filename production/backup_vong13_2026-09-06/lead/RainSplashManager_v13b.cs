using System.Collections.Generic;
using UnityEngine;
using Day_Night;

/// <summary>
/// ============================================================================
/// HAT MUA ROI XUONG DAT ROI TAN RA  (Task #35 — yeu cau cua Sep)
/// ============================================================================
/// Sep: "tim cai assets mua cua Happy Harvest cai hat mua roi xuong dat roi tan ra
///       ay, tang size len cho thay ro".
///
/// ART: Assets/Day_Night/VFX/Rain/RainSplashFlipbook.png — 384 x 256 px.
/// Vong 13 da MO ANH RA XEM va do bang alpha channel: day la LUOI 3 COT x 2 DONG,
/// moi khung 128 x 128 px, doc theo thu tu:
///     (0,0) vanh nuoc bat len   -> (0,1) vanh xoe rong  -> (0,2) vanh do xuong
///     (1,0) thanh vung nuoc     -> (1,1) vung nuoc mo    -> (1,2) tan het
/// Dung 6 khung = dung mot vong "roi xuong -> tan ra".
///
/// 🔴 BA LOI CUA BAN CU (vong 13 sua het, ghi lai de khong ai lam lai):
///
///  1. HIEU UNG NAY CHUA BAO GIO CHAY. Component khong duoc gan vao GameObject nao
///     trong SCN_Farm.unity (da dem guid script: 0 lan xuat hien ngoai file .meta
///     cua chinh no). Vong 13 cho no TU CAI DAT bang
///     [RuntimeInitializeOnLoadMethod] => khong can sua scene, khong can Sep keo tay,
///     va chay dung nhu nhau trong Editor lan ban build iPhone/iPad.
///
///  2. NEU CO GAN THI TRONG BAN BUILD CUNG VAN KHONG CHAY. Ban cu nap sprite bang
///     `AssetDatabase.LoadAssetAtPath` boc trong `#if UNITY_EDITOR`. AssetDatabase la
///     API CHI CO TRONG EDITOR; ra ban build thi khoi do bi cat, `splashSprites` rong,
///     `Update()` return ngay dong dau. Vong 13 doi sang `Resources.Load` (anh da duoc
///     copy sang Assets/_Game/Resources/VFX/) + `Sprite.Create` cat 6 khung ngay luc chay
///     => khong phu thuoc viec ai co cat sprite sheet trong Sprite Editor hay khong
///     (file .meta goc dang `spriteMode: 1`, tuc CHUA cat, nen ban cu chi lay duoc 1 sprite
///     va animation "tan ra" khong bao gio chay).
///
///  3. `SimpleSpriteAnimator` nam CHUNG FILE voi class nay. Unity chi cap fileID
///     11500000 cho class trung ten file => MonoBehaviour thu hai la mam mong cua dung
///     lop bug "5 popup tau de len nhau" (vong 7). Vong 13 tach ra file rieng.
///
/// TAT HIEU UNG: dat `TuCaiDat = false` (field static duoi day) hoac xoa GameObject
/// "RainSplash_Auto" luc chay. Khong co gi trong scene/prefab bi thay doi.
/// </summary>
public class RainSplashManager : MonoBehaviour
{
    // ── Cat sprite sheet ────────────────────────────────────────────────────
    private const string ResourcePath = "VFX/RainSplashFlipbook";
    private const int    Cols = 3;
    private const int    Rows = 2;
    private const float  PixelsPerUnit = 100f;

    /// <summary>Pivot cua moi khung. y = 0.28 vi vung nuoc nam o ~1/4 duoi khung
    /// (do that: vung nuoc o dong 2 nam trong y[64..105] cua khung 128) — de pivot o day
    /// thi hat mua "cham dat" dung cho minh sinh ra, khong bi treo len khong.</summary>
    private static readonly Vector2 FramePivot = new Vector2(0.5f, 0.28f);

    [Header("Mat do")]
    [Tooltip("So hat splash sinh ra moi giay khi dang mua.")]
    public int splashesPerSecond = 14;

    [Tooltip("Vung sinh quanh camera, WORLD unit. 1 o luoi = 300 x 150 nen 2600 x 1400 " +
             "phu kin man hinh o zoom moc (ortho 750).")]
    public Vector2 spawnArea = new Vector2(2600f, 1400f);

    [Header("Kich thuoc")]
    [Tooltip("He so phong to hat splash. Vong 13: 210.\n" +
             "VI SAO TO THE: khung 128 px / PPU 100 = 1.28 world unit. Voi scale 1.5 cu thi " +
             "hat splash chi 1.92 world, tuc 0.6 % be ngang MOT O LUOI (300) — nho hon mot " +
             "pixel tren man hinh, khong the nhin thay. 210 => 269 world ~ 0.9 be ngang o luoi, " +
             "dung co mot vung nuoc that.")]
    public float splashScale = 210f;

    [Tooltip("Fps cua animation 6 khung. 15 => mot hat song ~0.4 s.")]
    public float animSpeed = 15f;

    [Header("Sap lop")]
    [Tooltip("sortingOrder cua hat splash. Tham chieu da do trong du an: " +
             "tham dat/plot 500 · Canvas_HUD 100 · Canvas_Popup 300 · ghost 999. " +
             "560 = tren mat dat va tren o dat, duoi cong trinh cao va duoi moi popup.")]
    public int sortingOrder = 560;

    [Tooltip("Do duc cua hat splash.")]
    [Range(0f, 1f)] public float alpha = 0.85f;

    [Tooltip("Chi de danh cho Sep gan tay 6 sprite khac trong Inspector. De trong thi " +
             "tu cat tu RainSplashFlipbook.")]
    public Sprite[] splashSprites;

    // ── Tu cai dat, khong can sua scene ────────────────────────────────────
    /// <summary>Tat hieu ung: dat = false. Day la field MOI (scene chua serialize) nen
    /// gia tri trong code luon la gia tri that luc chay.</summary>
    public static bool TuCaiDat = true;

    private static RainSplashManager _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TuGanVaoScene()
    {
        if (!TuCaiDat || _instance != null) return;
        var go = new GameObject("RainSplash_Auto");
        Object.DontDestroyOnLoad(go);
        _instance = go.AddComponent<RainSplashManager>();
    }

    private DayNightWeatherSystem _weather;
    private float _timer;
    private float _timDoiThoiTiet;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        EnsureSprites();
    }

    private void EnsureSprites()
    {
        if (splashSprites != null && splashSprites.Length > 0) return;

        var tex = Resources.Load<Texture2D>(ResourcePath);
        if (tex == null)
        {
            // 1 dong, boc {} rieng: remove_debug_logs.ps1 xoa dong Debug.* ma de lai `if`.
            { Debug.LogWarning($"[RainSplash] Khong nap duoc '{ResourcePath}' tu Resources - hieu ung mua se khong chay. Kiem Assets/_Game/Resources/VFX/RainSplashFlipbook.png con khong."); }
            return;
        }

        int fw = tex.width  / Cols;
        int fh = tex.height / Rows;
        var list = new List<Sprite>(Cols * Rows);
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                // Rect cua Texture2D tinh tu DUOI len, con luoi khung doc tu TREN xuong
                // => phai dao dong lai, neu khong animation chay nguoc (tan ra truoc, bat len sau).
                var rect = new Rect(c * fw, (Rows - 1 - r) * fh, fw, fh);
                list.Add(Sprite.Create(tex, rect, FramePivot, PixelsPerUnit));
            }
        }
        splashSprites = list.ToArray();
    }

    private void Update()
    {
        if (_weather == null)
        {
            // Scene co the nap sau, tim lai moi 0.5 s chu khong tim moi frame.
            _timDoiThoiTiet -= Time.deltaTime;
            if (_timDoiThoiTiet > 0f) return;
            _timDoiThoiTiet = 0.5f;
            _weather = FindAnyObjectByType<DayNightWeatherSystem>();
            if (_weather == null) return;
        }

        bool dangMua = _weather.CurrentWeather == DayNightWeatherType.Rain
                    || _weather.CurrentWeather == DayNightWeatherType.Thunder;
        if (!dangMua) { _timer = 0f; return; }

        if (splashSprites == null || splashSprites.Length == 0) return;

        _timer += Time.deltaTime;
        float buoc = 1f / Mathf.Max(1, splashesPerSecond);
        int chan = 0;                       // chan vong lap: mot frame giat khong sinh 500 hat
        while (_timer >= buoc && chan++ < 8)
        {
            _timer -= buoc;
            SinhMotHat();
        }
        if (chan >= 8) _timer = 0f;
    }

    private void SinhMotHat()
    {
        Camera cam = Camera.main;
        Vector3 goc = cam != null ? cam.transform.position : transform.position;

        var pos = new Vector3(
            goc.x + Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f),
            goc.y + Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f),
            0f);

        var go = new GameObject("Splash");
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(splashScale, splashScale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        sr.color = new Color(1f, 1f, 1f, alpha);

        var anim = go.AddComponent<SimpleSpriteAnimator>();
        anim.sprites      = splashSprites;
        anim.fps          = animSpeed;
        anim.destroyOnEnd = true;
    }
}
