// ============================================================================
//  LandClearDebrisFX — hieu ung DON LO DAT (2026-09-25)
//  Gan tren LandClearingSite: quanh moi tho (goc lo) cu 0.5-0.9s bung bui + da vun + la + go bay tung,
//  giua lo thinh thoang boc khoi. Ngoai camera thi khong phat (WorldClearFX tu chan).
//  Tat: bo tick 'bat' tren component (object LandClearing_<id>) luc Play.
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class LandClearDebrisFX : MonoBehaviour
{
    public bool bat = true;
    [Tooltip("Nhip bung vun quanh moi tho (giay).")]
    public Vector2 nhipVun = new Vector2(0.5f, 0.9f);
    [Tooltip("Nhip boc khoi giua lo (giay).")]
    public Vector2 nhipKhoi = new Vector2(1.1f, 1.8f);

    private Vector3[] _diem = new Vector3[0];
    private Bounds _b;
    private float[] _hen = new float[0];
    private float _henKhoi;
    private float _k = 1f;
    private int _layer, _order;

    public void Init(Vector3[] diemTho, Bounds loDat)
    {
        _diem = diemTho ?? new Vector3[0];
        _b = loDat;
        _hen = new float[_diem.Length];
        for (int i = 0; i < _hen.Length; i++) _hen[i] = Time.time + Random.Range(0f, nhipVun.y);
        _henKhoi = Time.time + Random.Range(0.3f, 1f);
        _k = Mathf.Clamp(IsoGrid.CellWidth / 300f, 0.6f, 2f) * 1.1f;
        string ten = TouristSortingLayers.Resolve(TouristSortingLayers.Visitor);
        _layer = SortingLayer.NameToID(ten);
        _order = 30000;
    }

    private void Update()
    {
        if (!bat || _diem.Length == 0) return;
        float now = Time.time;
        for (int i = 0; i < _diem.Length; i++)
        {
            if (now < _hen[i]) continue;
            _hen[i] = now + Random.Range(nhipVun.x, nhipVun.y);
            Vector3 p = _diem[i];
            // lech vao phia tam lo (cho tho dang dap) + chut ngau nhien
            Vector3 vao = (_b.center - p); vao.z = 0f;
            p += vao.normalized * Random.Range(30f, 70f) * _k + new Vector3(Random.Range(-20f, 20f), Random.Range(-10f, 10f), 0f) * _k;
            WorldClearFX.DonDat(p, _k, _layer, _order);
        }
        if (now >= _henKhoi)
        {
            _henKhoi = now + Random.Range(nhipKhoi.x, nhipKhoi.y);
            Vector3 q = new Vector3(Random.Range(_b.min.x + _b.size.x * 0.25f, _b.max.x - _b.size.x * 0.25f),
                                    Random.Range(_b.min.y + _b.size.y * 0.25f, _b.max.y - _b.size.y * 0.25f), _b.center.z);
            WorldClearFX.Khoi(q, _k, _layer, _order - 1);
        }
    }
}
