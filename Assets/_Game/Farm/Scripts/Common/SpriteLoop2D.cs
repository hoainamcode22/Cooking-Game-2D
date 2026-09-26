// ============================================================================
//  SpriteLoop2D — doi sprite theo nhip cho 1 SpriteRenderer (chong chong ga, co kho, nuoc mang...) (2026-09-25)
//  Edit mode hien frame dau (giong Play). Ngoai man hinh khong doi sprite (isVisible).
//
//  [2026-09-26] Che do "phat khi con vat di qua" (bun ban tung o chuong heo):
//   - An khi nghi. Moi 0.25s xem con vat (LivestockAI) trong chuong: con nao VUA buoc vao vung elip
//     (tamVung / banKinhVung, toa do local cua object nay) -> hien len, chay 1 luot frame roi an.
//   - Nghi giua 2 lan phat: thoiGianNghi giay. Ngoai man hinh: khong kiem tra, khong ton CPU.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop2D : MonoBehaviour
{
    public Sprite[] frames = new Sprite[0];
    [Min(0.1f)] public float fps = 1.2f;
    [Tooltip("Bat dau o frame ngau nhien -> nhieu chuong khong quay giong het nhau.")]
    public bool batDauNgauNhien = true;

    [Header("Phat khi con vat di qua (bun ban tung...)")]
    [Tooltip("Bat: binh thuong AN, chi chay 1 luot khi con vat buoc vao vung elip ben duoi.")]
    public bool chiPhatKhiConVatDiQua = false;
    [Tooltip("Tam vung (local cua object nay).")]
    public Vector2 tamVung;
    [Tooltip("Ban kinh ngang / doc cua vung elip (local).")]
    public Vector2 banKinhVung = new Vector2(0.4f, 0.2f);
    [Min(0f)] public float thoiGianNghi = 1.5f;

    private SpriteRenderer _sr;
    private float _t;

    // che do phat
    private bool _dangPhat;
    private float _henKiem, _henNghi, _henLamMoi;
    private Transform _chuong;
    private readonly List<Transform> _conVat = new List<Transform>(8);
    private readonly HashSet<Transform> _dangTrong = new HashSet<Transform>();

    private void OnEnable()
    {
        _sr = GetComponent<SpriteRenderer>();
        _t = batDauNgauNhien ? Random.Range(0f, 10f) : 0f;
        if (_sr != null && frames != null && frames.Length > 0 && frames[0] != null && _sr.sprite == null) _sr.sprite = frames[0];
        if (chiPhatKhiConVatDiQua && Application.isPlaying && _sr != null) { _sr.enabled = false; _dangPhat = false; }
    }

    private void Update()
    {
        if (_sr == null || frames == null || frames.Length == 0) return;
        if (chiPhatKhiConVatDiQua) { CapNhatPhat(); return; }
        if (!_sr.isVisible) return;
        _t += Time.deltaTime;
        var sp = frames[(int)(_t * fps) % frames.Length];
        if (sp != null && _sr.sprite != sp) _sr.sprite = sp;
    }

    private void CapNhatPhat()
    {
        if (_dangPhat)
        {
            _t += Time.deltaTime;
            int i = (int)(_t * fps);
            if (i >= frames.Length) { _dangPhat = false; _sr.enabled = false; _henNghi = Time.time + thoiGianNghi; return; }
            if (frames[i] != null && _sr.sprite != frames[i]) _sr.sprite = frames[i];
            return;
        }
        if (Time.time < _henKiem) return;
        _henKiem = Time.time + 0.25f;
        if (!DangTrongManHinh()) return;

        if (_chuong == null) _chuong = transform.parent != null && transform.parent.parent != null ? transform.parent.parent : transform.parent;
        if (_chuong == null) return;
        if (Time.time >= _henLamMoi)
        {
            _henLamMoi = Time.time + 2f;
            _conVat.Clear();
            foreach (var ai in _chuong.GetComponentsInChildren<Assetsgame.Animals.LivestockAI>(false))
                if (ai != null) _conVat.Add(ai.transform);
        }

        float rx = Mathf.Max(0.01f, banKinhVung.x), ry = Mathf.Max(0.01f, banKinhVung.y);
        bool coConMoiVao = false;
        for (int k = 0; k < _conVat.Count; k++)
        {
            var cv = _conVat[k];
            if (cv == null) continue;
            Vector3 l = transform.InverseTransformPoint(cv.position);
            float dx = (l.x - tamVung.x) / rx, dy = (l.y - tamVung.y) / ry;
            bool trong = dx * dx + dy * dy <= 1f;
            if (trong) { if (_dangTrong.Add(cv)) coConMoiVao = true; }
            else _dangTrong.Remove(cv);
        }
        if (coConMoiVao && Time.time >= _henNghi)
        {
            _dangPhat = true; _t = 0f;
            if (frames[0] != null) _sr.sprite = frames[0];
            _sr.enabled = true;
        }
    }

    // Renderer dang an nen isVisible luon false -> tu kiem tra theo camera chinh.
    private bool DangTrongManHinh()
    {
        var cam = Camera.main;
        if (cam == null) return true;
        Vector3 v = cam.WorldToScreenPoint(transform.TransformPoint(tamVung));
        float w = Screen.width, h = Screen.height;
        return v.z > 0f && v.x > -0.2f * w && v.x < 1.2f * w && v.y > -0.2f * h && v.y < 1.2f * h;
    }
}
