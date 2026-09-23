using TMPro;
using UnityEngine;

namespace KitchenUIv3
{
    /// <summary>
    /// Uon chu TMP theo cung tron (vong cung) de nam theo asset cong (banner cong, bang den).
    /// Cach lam: sau khi TMP dung mesh, dich tung ky tu theo duong parabol va xoay theo tiep tuyen.
    /// Chay lai khi chu doi (LocRuntimeInterceptor / SetText deu doi text.text) — khong ton CPU vi
    /// chi tinh khi text thay doi hoac tham so doi.
    ///   doCong  > 0 : cong len (giua cao hon 2 dau)   — tieu de bang den "TODAY'S SPECIAL"
    ///   doCong  < 0 : cong xuong                       — banner cuon giay
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    public class TMPCurvedText : MonoBehaviour
    {
        [Tooltip("Do cao vong cung (don vi canvas). Duong = cong len, am = cong xuong.")]
        public float doCong = 18f;
        [Tooltip("Xoay ky tu theo tiep tuyen cua cung.")]
        public bool xoayTheoCung = true;

        private TMP_Text _t;
        private string _chuCu;
        private float _congCu;
        private bool _xoayCu;
        private float _rongCu;

        private void OnEnable()
        {
            _t = GetComponent<TMP_Text>();
            _chuCu = null;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(KhiChuDoi);
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(KhiChuDoi);
        }

        private void KhiChuDoi(Object o)
        {
            if (o == _t) _chuCu = null;   // ep uon lai o LateUpdate
        }

        private void LateUpdate()
        {
            if (_t == null) _t = GetComponent<TMP_Text>();
            if (_t == null) return;
            float rong = _t.rectTransform.rect.width;
            if (_chuCu == _t.text && Mathf.Approximately(_congCu, doCong) && _xoayCu == xoayTheoCung && Mathf.Approximately(_rongCu, rong)) return;
            _chuCu = _t.text; _congCu = doCong; _xoayCu = xoayTheoCung; _rongCu = rong;
            Uon();
        }

        private void Uon()
        {
            _t.ForceMeshUpdate();
            var ti = _t.textInfo;
            int n = ti.characterCount;
            if (n == 0) return;

            // bien do theo be rong dong chu thuc te
            float xMin = float.MaxValue, xMax = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                var c = ti.characterInfo[i]; if (!c.isVisible) continue;
                xMin = Mathf.Min(xMin, c.bottomLeft.x); xMax = Mathf.Max(xMax, c.topRight.x);
            }
            if (xMax <= xMin) return;
            float w = xMax - xMin;

            for (int i = 0; i < n; i++)
            {
                var c = ti.characterInfo[i]; if (!c.isVisible) continue;
                int m = c.materialReferenceIndex, v = c.vertexIndex;
                var verts = ti.meshInfo[m].vertices;

                Vector3 mid = new Vector3((verts[v].x + verts[v + 2].x) * 0.5f, c.baseLine, 0f);
                float t = Mathf.Clamp01((mid.x - xMin) / w);            // 0..1 theo chieu ngang
                float u = 2f * t - 1f;                                   // -1..1
                float y = doCong * (1f - u * u);                         // parabol: giua = doCong, 2 dau = 0
                float dao = -2f * doCong * u / (w * 0.5f);               // dy/dx
                float goc = xoayTheoCung ? Mathf.Atan(dao) * Mathf.Rad2Deg : 0f;

                var xoay = Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.Euler(0f, 0f, goc), Vector3.one);
                for (int k = 0; k < 4; k++)
                    verts[v + k] = mid + xoay.MultiplyPoint3x4(verts[v + k] - mid);
            }
            _t.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
