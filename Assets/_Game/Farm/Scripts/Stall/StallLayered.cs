// ============================================================================
//  QUAY HANG (Stall) - BAN MOI + NONG SAN NAY (2026-09-25)
//  Nha quay DUNG YEN (art FarmStand). Thinh thoang 1 mon nong san NAY len khoi dung sot cua no roi roi lai,
//  giong cho (MarketLayered). Icon lay tu CHINH game (CropData.harvestIcon: bi do, ca rot, bap cai,
//  ca chua, ngo, khoai tay) -> dong bo voi kho / popup.
//  Mon i nay o diem choNay[i] (dung sot cung loai tren art). Kich thuoc icon tu chuan hoa theo 'coMon'.
//  Tool: Tools > Farm Game > Quay Hang (Stall) > 1 lap / 2 tra lai.
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class StallLayered : MonoBehaviour
{
    [Header("Mon nay (tool tu gan tu CropData)")]
    public Transform produceRoot;
    public Sprite[] produceSprites;
    [Tooltip("Diem nay cua tung mon (don vi local cua quay), cung thu tu voi produceSprites.")]
    public Vector2[] choNay =
    {
        new Vector2(-0.737f, -0.266f), new Vector2(-0.425f, -0.444f), new Vector2(-0.090f, -0.600f),
        new Vector2(0.179f, -0.846f), new Vector2(0.535f, -0.667f), new Vector2(-1.117f, -0.980f),
    };
    [Tooltip("Nhip giua 2 lan nay (giay).")]
    public Vector2 nhipNay = new Vector2(1.4f, 3.0f);
    [Tooltip("Do cao nay (local).")]
    public float caoNay = 0.34f;
    public float thoiGianNay = 0.7f;
    [Tooltip("Canh dai nhat cua icon (local). 0.34 x scale 190 ~ 65 unit world.")]
    public float coMon = 0.34f;

    [HideInInspector] public Sprite spriteCu;

    private const int POOL = 3;
    private SpriteRenderer[] _mon;
    private float[] _tMon, _co;
    private Vector3[] _goc;
    private float _henNay;
    private int _orderGoc;

    private void OnEnable()
    {
        var sr = GetComponent<SpriteRenderer>();
        _orderGoc = sr != null ? sr.sortingOrder : 0;
        _henNay = Time.time + Random.Range(nhipNay.x, nhipNay.y);
    }

    private void Update()
    {
        if (produceRoot == null || produceSprites == null || produceSprites.Length == 0 || choNay == null || choNay.Length == 0) return;
        if (!WorldClearFX.TrongCamera(transform.position, 600f)) return;     // ngoai man hinh: nghi
        float dt = Time.deltaTime;
        if (_mon == null) TaoPool();

        if (Time.time >= _henNay)
        {
            _henNay = Time.time + Random.Range(nhipNay.x, nhipNay.y);
            for (int i = 0; i < POOL; i++)
            {
                if (_tMon[i] >= 0f) continue;
                int k = Random.Range(0, Mathf.Min(produceSprites.Length, choNay.Length));
                var sp = produceSprites[k];
                if (sp == null) break;
                _goc[i] = new Vector3(choNay[k].x, choNay[k].y, 0f);
                _mon[i].sprite = sp;
                float canh = Mathf.Max(0.0001f, Mathf.Max(sp.bounds.size.x, sp.bounds.size.y));
                _co[i] = coMon / canh;                                             // chuan hoa: icon to nho khac nhau van bang nhau
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
            float y = caoNay * 4f * k * (1f - k);                                  // parabol len roi xuong
            float s = _co[i] * (k < 0.12f ? Mathf.SmoothStep(0.4f, 1f, k / 0.12f) : 1f);
            float sx = s, sy = s;
            if (k > 0.85f) { float e = (k - 0.85f) / 0.15f; sx = s * (1f + 0.18f * e); sy = s * (1f - 0.22f * e); }   // bep nhe khi roi lai
            var tr = _mon[i].transform;
            tr.localPosition = _goc[i] + new Vector3(0f, y, 0f);
            tr.localScale = new Vector3(sx, sy, 1f);
            tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * 10f);
            var c = _mon[i].color; c.a = k > 0.9f ? Mathf.Lerp(1f, 0f, (k - 0.9f) / 0.1f) : 1f; _mon[i].color = c;
        }
    }

    private void TaoPool()
    {
        _mon = new SpriteRenderer[POOL]; _tMon = new float[POOL]; _goc = new Vector3[POOL]; _co = new float[POOL];
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
}
