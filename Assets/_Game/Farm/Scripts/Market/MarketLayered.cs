// ============================================================================
//  CHO (Market) - BAN NHIEU LOP (2026-09-25)
//  Nha cho DUNG YEN. Chi cac lop nho chuyen dong:
//    - MK_Flag   : day co duoi ca bay (frame chon trong flagOrder, qua lai muot)
//    - MK_Sign   : bang gia treo dung dua (6 frame, qua lai)
//    - MK_Lantern: 2 den long (tinh) + MK_GlowL / MK_GlowR quang sang nhap nhay nhe
//    - MK_Produce: thinh thoang 1 mon (tao, cam, ca rot...) NAY len khoi ke roi roi lai
//  Moi lop nam trong Hierarchy -> Sep keo / doi so trong Inspector tuy y.
//  Edit mode hien dung nhu luc Play (frame dau, den sang vua).
// ============================================================================
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class MarketLayered : MonoBehaviour
{
    [Header("Lop (tool tu gan)")]
    public SpriteRenderer flag;
    public SpriteRenderer sign;
    public SpriteRenderer glowL;
    public SpriteRenderer glowR;
    public Transform produceRoot;

    [Header("Frame")]
    public Sprite[] flagFrames;
    public Sprite[] signFrames;
    public Sprite[] produceSprites;
    [Tooltip("Frame co duoc dung, chay qua lai. Bo art hien tai frame 2-5 lech vi tri nen chi dung 0,1.")]
    public int[] flagOrder = { 0, 1 };
    public float flagFps = 4f;
    public float signFps = 5f;

    [Header("Den")]
    [Range(0f, 1f)] public float denMin = 0.5f;
    [Range(0f, 1f)] public float denMax = 0.9f;
    public float denChuKy = 2.6f;

    [Header("Mon hang nay (don vi local cua cho)")]
    public Vector2[] choNay =
    {
        new Vector2(1.17f, -0.70f), new Vector2(0.83f, -0.92f), new Vector2(0.53f, -0.39f),
        new Vector2(-0.28f, -0.50f), new Vector2(-0.89f, -0.20f), new Vector2(1.50f, -0.25f),
    };
    public Vector2 nhipNay = new Vector2(1.6f, 3.4f);
    public float caoNay = 0.38f;
    public float thoiGianNay = 0.7f;
    public float coMon = 0.75f;

    [HideInInspector] public Sprite spriteCu;

    // ── runtime ──
    private float _tFlag, _tSign, _henNay;
    private const int POOL = 3;
    private SpriteRenderer[] _mon;
    private float[] _tMon;
    private Vector3[] _goc;
    private int _orderGoc;

    private void OnEnable()
    {
        HienTinh();
        var sr = GetComponent<SpriteRenderer>();
        _orderGoc = sr != null ? sr.sortingOrder : 0;
        _henNay = Time.time + Random.Range(nhipNay.x, nhipNay.y);
    }

    private void HienTinh()
    {
        if (flag != null && flagFrames != null && flagFrames.Length > 0) flag.sprite = flagFrames[Mathf.Clamp(flagOrder != null && flagOrder.Length > 0 ? flagOrder[0] : 0, 0, flagFrames.Length - 1)];
        if (sign != null && signFrames != null && signFrames.Length > 0) sign.sprite = signFrames[0];
        DatAlpha(glowL, (denMin + denMax) * 0.5f);
        DatAlpha(glowR, (denMin + denMax) * 0.5f);
    }

    private void Update()
    {
        if (!Application.isPlaying) { HienTinh(); return; }
        float dt = Time.deltaTime;

        // Co: chay qua lai theo flagOrder
        if (flag != null && flagFrames != null && flagFrames.Length > 0 && flagOrder != null && flagOrder.Length > 0)
        {
            _tFlag += dt * flagFps;
            int k = QuaLai((int)_tFlag, flagOrder.Length);
            flag.sprite = flagFrames[Mathf.Clamp(flagOrder[k], 0, flagFrames.Length - 1)];
        }
        // Bang gia: 0..5..0
        if (sign != null && signFrames != null && signFrames.Length > 0)
        {
            _tSign += dt * signFps;
            sign.sprite = signFrames[QuaLai((int)_tSign, signFrames.Length)];
        }
        // Den
        float t = Time.time * Mathf.PI * 2f / Mathf.Max(0.2f, denChuKy);
        DatAlpha(glowL, Mathf.Lerp(denMin, denMax, 0.5f + 0.5f * Mathf.Sin(t)));
        DatAlpha(glowR, Mathf.Lerp(denMin, denMax, 0.5f + 0.5f * Mathf.Sin(t + 1.9f)));

        CapNhatMon(dt);
    }

    /// <summary>0,1,..,n-1,n-2,..,1,0,... (khong lap lai frame o 2 dau)</summary>
    private static int QuaLai(int i, int n)
    {
        if (n <= 1) return 0;
        int chuKy = n * 2 - 2;
        int m = i % chuKy;
        return m < n ? m : chuKy - m;
    }

    private void CapNhatMon(float dt)
    {
        if (produceRoot == null || produceSprites == null || produceSprites.Length == 0 || choNay == null || choNay.Length == 0) return;
        if (_mon == null)
        {
            _mon = new SpriteRenderer[POOL]; _tMon = new float[POOL]; _goc = new Vector3[POOL];
            var goc = GetComponent<SpriteRenderer>();
            for (int i = 0; i < POOL; i++)
            {
                var go = new GameObject("Mon");
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(produceRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (goc != null) { sr.sharedMaterial = goc.sharedMaterial; sr.sortingLayerID = goc.sortingLayerID; }
                sr.sortingOrder = _orderGoc + 5;
                sr.enabled = false;
                _mon[i] = sr; _tMon[i] = -1f;
            }
        }

        if (Time.time >= _henNay)
        {
            _henNay = Time.time + Random.Range(nhipNay.x, nhipNay.y);
            for (int i = 0; i < POOL; i++)
            {
                if (_tMon[i] >= 0f) continue;
                Vector2 p = choNay[Random.Range(0, choNay.Length)];
                _goc[i] = new Vector3(p.x, p.y, 0f);
                _mon[i].sprite = produceSprites[Random.Range(0, produceSprites.Length)];
                _mon[i].enabled = true;
                _tMon[i] = 0f;
                break;
            }
        }

        float T = Mathf.Max(0.2f, thoiGianNay);
        for (int i = 0; i < POOL; i++)
        {
            if (_tMon[i] < 0f) continue;
            _tMon[i] += dt;
            float k = _tMon[i] / T;
            if (k >= 1f) { _mon[i].enabled = false; _tMon[i] = -1f; continue; }
            float y = caoNay * 4f * k * (1f - k);                               // cung parabol len roi xuong
            float s = coMon * (k < 0.12f ? Mathf.SmoothStep(0.4f, 1f, k / 0.12f) : 1f);
            float sx = s, sy = s;
            if (k > 0.85f) { float e = (k - 0.85f) / 0.15f; sx = s * (1f + 0.18f * e); sy = s * (1f - 0.22f * e); }   // bep nhe khi roi lai
            var tr = _mon[i].transform;
            tr.localPosition = _goc[i] + new Vector3(0f, y, 0f);
            tr.localScale = new Vector3(sx, sy, 1f);
            var c = _mon[i].color; c.a = k > 0.9f ? Mathf.Lerp(1f, 0f, (k - 0.9f) / 0.1f) : 1f; _mon[i].color = c;
        }
    }

    private static void DatAlpha(SpriteRenderer r, float a)
    {
        if (r == null) return;
        var c = r.color; c.a = a; r.color = c;
    }
}
