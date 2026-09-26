// ============================================================================
//  NHA HANG (CookingGate) - BAN MOI v3 NHIEU LOP (2026-09-25)
//  Nha dung yen (Restaurant_v3). Cac lop chuyen dong (moi lop 1 object con trong Hierarchy, keo chinh tay):
//    RS_Stove1 / RS_Stove2 : lua 2 bep gach (6 frame, lech pha nhau)
//    RS_Oven               : lua + pizza trong lo da (6 frame)
//    RS_Sign               : bang thia-nia dung dua qua lai (6 frame ping-pong)
//    Khoi ong khoi         : bang code (WorldClearFX.Khoi), diem khoi = RS_SmokePoint
//  Edit mode hien frame dau (giong Play). Ngoai man hinh: khong doi frame, khong nha khoi.
// ============================================================================
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class RestaurantLayered : MonoBehaviour
{
    [Header("Lop (tool tu gan)")]
    public SpriteRenderer stove1, stove2, oven, sign;
    public Transform smokePoint;

    [Header("Frame")]
    public Sprite[] stoveFrames = new Sprite[0];
    public Sprite[] ovenFrames = new Sprite[0];
    public Sprite[] signFrames = new Sprite[0];
    public float stoveFps = 10f, ovenFps = 7f, signFps = 5f;

    [Header("Khoi ong khoi")]
    public bool coKhoi = true;
    [Tooltip("[2026-09-25 v2] Sep: khoi hoi it -> day hon. Nhip giua 2 dot khoi (giay).")]
    public Vector2 nhipKhoiV2 = new Vector2(0.28f, 0.5f);
    [Tooltip("So cum khoi moi dot.")]
    [Range(1, 4)] public int soCumKhoi = 2;
    public float coKhoiV2 = 0.95f;

    [Header("Khoi DUNG CHUNG may xay (2026-09-26)")]
    [Tooltip("Bat: vao Play tu nhan ban khoi FM_Smoke cua may xay thuc an (ParticleSystem) dat o RS_SmokePoint -> khoi giong het may xay. " +
             "Khong co may xay trong scene -> dung khoi cu (WorldClearFX).")]
    public bool khoiMayXay = true;
    [Tooltip("Keo tay 1 ParticleSystem vao day neu muon. De trong = tu nhan ban tu may xay.")]
    public ParticleSystem khoiPS;
    [Tooltip("So cum khoi / giay (may xay: 1.8 khi dang xay, 0.45 khi ranh).")]
    public float khoiMoiGiay = 1.8f;
    [Tooltip("Co khoi so voi may xay (1 = bang).")]
    public float khoiTiLe = 1.15f;
    private int _lanTimKhoi;
    private float _henTimKhoi;

    [HideInInspector] public Sprite spriteCu;

    [Header("Dau bep (tool 3 tu gan) - de tra lai")]
    public SpriteRenderer quayTruoc;                 // RS_CounterFront: quay go + cot truoc, ve DE LEN dau bep
    [HideInInspector] public Transform chef;
    [HideInInspector] public Transform chefChaCu;
    [HideInInspector] public Vector3 chefPosCu, chefScaleCu;
    [HideInInspector] public int chefOrderCu, chefLayerCu;
    [HideInInspector] public bool chefYSortCu;
    [HideInInspector] public string chefYSortLayerCu = "";

    private float _t, _henKhoi;
    private SpriteRenderer _goc;

    private void OnEnable()
    {
        _goc = GetComponent<SpriteRenderer>();
        _t = Random.Range(0f, 5f);
        HienFrameDau();
    }

    private void HienFrameDau()
    {
        Dat(stove1, stoveFrames, 0); Dat(stove2, stoveFrames, 3);
        Dat(oven, ovenFrames, 0); Dat(sign, signFrames, 0);
    }

    private static void Dat(SpriteRenderer r, Sprite[] f, int i)
    {
        if (r == null || f == null || f.Length == 0) return;
        var sp = f[((i % f.Length) + f.Length) % f.Length];
        if (sp != null && r.sprite != sp) r.sprite = sp;
    }

    // Nhan ban khoi may xay (1 lan, co thu lai vai lan neu may xay sinh sau nha hang)
    private void TimKhoiMayXay()
    {
        _lanTimKhoi++;
        _henTimKhoi = Time.time + 1.5f;
        if (smokePoint == null) { _lanTimKhoi = 99; return; }
        var fm = FindFirstObjectByType<FeedMillLayered>(FindObjectsInactive.Include);
        if (fm == null || fm.smoke == null) return;
        var src = fm.smoke.transform;
        khoiPS = Instantiate(fm.smoke, smokePoint);
        khoiPS.name = "RS_Smoke";
        var t = khoiPS.transform;
        t.localPosition = Vector3.zero;
        t.rotation = src.rotation;
        Vector3 a = src.lossyScale, b = smokePoint.lossyScale;
        t.localScale = new Vector3(Chia(a.x, b.x), Chia(a.y, b.y), Chia(a.z, b.z)) * Mathf.Max(0.1f, khoiTiLe);
        var r = khoiPS.GetComponent<ParticleSystemRenderer>();
        if (r != null && _goc != null) { r.sortingLayerID = _goc.sortingLayerID; r.sortingOrder = _goc.sortingOrder + 10; }
        var em = khoiPS.emission;
        em.rateOverTime = Mathf.Max(0.1f, khoiMoiGiay);
        khoiPS.Play();
    }

    private static float Chia(float a, float b) => Mathf.Abs(b) > 1e-5f ? a / b : a;

    private static int QuaLai(int i, int n)
    {
        if (n <= 1) return 0;
        int ck = n * 2 - 2, m = i % ck;
        return m < n ? m : ck - m;
    }

    private void Update()
    {
        if (!Application.isPlaying) { HienFrameDau(); return; }
        if (_goc != null && !_goc.isVisible) return;
        _t += Time.deltaTime;
        int s = (int)(_t * stoveFps);
        Dat(stove1, stoveFrames, s);
        Dat(stove2, stoveFrames, s + 3);
        Dat(oven, ovenFrames, (int)(_t * ovenFps));
        if (signFrames != null && signFrames.Length > 0) Dat(sign, signFrames, QuaLai((int)(_t * signFps), signFrames.Length));

        if (khoiMayXay && khoiPS == null && _lanTimKhoi < 8 && Time.time >= _henTimKhoi) TimKhoiMayXay();

        if (coKhoi && smokePoint != null && khoiPS == null && Time.time >= _henKhoi)
        {
            _henKhoi = Time.time + Random.Range(nhipKhoiV2.x, nhipKhoiV2.y);
            int layer = _goc != null ? _goc.sortingLayerID : 0, order = _goc != null ? _goc.sortingOrder + 10 : 10;
            for (int i = 0; i < soCumKhoi; i++)
                WorldClearFX.Khoi(smokePoint.position + new Vector3(Random.Range(-8f, 8f), i * 6f, 0f), coKhoiV2 * Random.Range(0.85f, 1.15f), layer, order);
        }
    }
}
