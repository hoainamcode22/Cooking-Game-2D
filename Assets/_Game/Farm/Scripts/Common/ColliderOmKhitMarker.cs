// ============================================================================
//  ColliderOmKhitMarker — ghi nho de TRA LAI khi dung tool "Collider om khit cong trinh" (2026-09-25)
//  Tool Editor them PolygonCollider2D om sat hinh cong trinh, TAT (khong xoa) cac BoxCollider2D cu,
//  noi lai moi tham chieu dang tro vao box cu sang polygon moi. Component nay giu danh sach do.
//  Luc chay KHONG lam gi (khong Update). Xoa tay component nay = mat duong tra lai tu dong.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class ColliderOmKhitMarker : MonoBehaviour
{
    public PolygonCollider2D poly;
    public List<BoxCollider2D> boxCu = new List<BoxCollider2D>();

    [System.Serializable]
    public class ThamChieu
    {
        public Object chu;              // component dang giu tham chieu
        public string duong;            // propertyPath
        public BoxCollider2D cu;        // box no tro toi truoc do
    }
    public List<ThamChieu> thamChieu = new List<ThamChieu>();
}
