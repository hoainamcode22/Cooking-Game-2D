// ============================================================================
//  PenWalkArea — VUNG SAN THAT cua chuong cho con vat di (2026-09-25)
//  Da giac LOI (toa do local cua object co HappyHarvestAnimalVisualSpawner = goc chuong).
//  Tool "Edric Tools > Chuong (Pen) > 1" do tu hinh chuong v3: sau hang rao, TRUOC nha chuong va may an.
//  LivestockAI thay component nay thi dung no thay cho khung chu nhat cu -> con vat khong de len nha chuong,
//  khong ra khoi rao. Keo cac diem trong Inspector de chinh tay (Scene view hien khung xanh khi chon chuong).
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class PenWalkArea : MonoBehaviour
{
    [Tooltip("Cac dinh da giac LOI (local cua chuong), theo 1 chieu vong.")]
    public Vector2[] diem = new Vector2[0];

    public bool CoHieuLuc => diem != null && diem.Length >= 3;

    public bool Chua(Vector2 p)
    {
        if (!CoHieuLuc) return true;
        int n = diem.Length; float dau = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = diem[i], b = diem[(i + 1) % n];
            float c = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            if (Mathf.Abs(c) < 1e-6f) continue;
            if (dau == 0f) dau = Mathf.Sign(c);
            else if (Mathf.Sign(c) != dau) return false;
        }
        return true;
    }

    /// <summary>Diem ngau nhien trong da giac (lay mau trong hop bao, thu toi da 30 lan).</summary>
    public Vector2 DiemNgauNhien()
    {
        if (!CoHieuLuc) return Vector2.zero;
        Vector2 mn = diem[0], mx = diem[0];
        for (int i = 1; i < diem.Length; i++) { mn = Vector2.Min(mn, diem[i]); mx = Vector2.Max(mx, diem[i]); }
        for (int k = 0; k < 30; k++)
        {
            var p = new Vector2(Random.Range(mn.x, mx.x), Random.Range(mn.y, mx.y));
            if (Chua(p)) return p;
        }
        return TamDaGiac();
    }

    /// <summary>Keo diem ngoai vao canh gan nhat (diem trong thi giu nguyen).</summary>
    public Vector2 KepVao(Vector2 p)
    {
        if (!CoHieuLuc || Chua(p)) return p;
        Vector2 tot = p; float dMin = float.MaxValue;
        for (int i = 0; i < diem.Length; i++)
        {
            Vector2 a = diem[i], b = diem[(i + 1) % diem.Length];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            Vector2 q = a + ab * t;
            float d = (q - p).sqrMagnitude;
            if (d < dMin) { dMin = d; tot = q; }
        }
        // lui vao trong 1 chut cho chac nam trong
        return Vector2.Lerp(tot, TamDaGiac(), 0.04f);
    }

    public Vector2 TamDaGiac()
    {
        Vector2 s = Vector2.zero;
        for (int i = 0; i < diem.Length; i++) s += diem[i];
        return diem.Length > 0 ? s / diem.Length : Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (!CoHieuLuc) return;
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f);
        for (int i = 0; i < diem.Length; i++)
            Gizmos.DrawLine(transform.TransformPoint(diem[i]), transform.TransformPoint(diem[(i + 1) % diem.Length]));
    }
}
