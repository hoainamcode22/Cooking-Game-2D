// ============================================================================
//  ChanDe — HINH THOI CHAN DE cua cong trinh (2026-09-24)
//  Nam o con "Chan_De" cua ô đất / chậu / nhà. Chi la KHUNG CANH trong Scene view (Gizmos):
//  ve dung vung N x M o luoi ma cong trinh chiem, dinh duoi = goc object (diem neo).
//  Sep keo / scale ANH (GroundSprite...) cho phan chan nam gon trong hinh thoi nay.
//  Luc Play khong ve gi, khong ton gi (khong Update).
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class ChanDe : MonoBehaviour
{
    [Tooltip("So o luoi cong trinh chiem (ngang x doc). Mot o = 300 x 150 world.")]
    public Vector2Int soO = Vector2Int.one;
    [SerializeField] private Color mau = new Color(0.2f, 1f, 0.3f, 0.95f);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform goc = transform.parent != null ? transform.parent : transform;
        int n = Mathf.Max(1, soO.x), m = Mathf.Max(1, soO.y);
        float w = IsoGrid.CellWidth, h = IsoGrid.CellHeight;
        Vector3 s = goc.position;                                   // dinh Nam = diem neo
        Vector3 e = s + new Vector3(n * w * 0.5f, n * h * 0.5f, 0f); // Dong
        Vector3 nn = e + new Vector3(-m * w * 0.5f, m * h * 0.5f, 0f); // Bac
        Vector3 wv = s + new Vector3(-m * w * 0.5f, m * h * 0.5f, 0f); // Tay
        Gizmos.color = mau;
        Gizmos.DrawLine(s, e); Gizmos.DrawLine(e, nn); Gizmos.DrawLine(nn, wv); Gizmos.DrawLine(wv, s);
        Gizmos.color = new Color(mau.r, mau.g, mau.b, 0.35f);
        Gizmos.DrawLine(s, nn); Gizmos.DrawLine(e, wv);
    }
#endif
}
