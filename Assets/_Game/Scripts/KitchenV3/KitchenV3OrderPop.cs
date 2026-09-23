using UnityEngine;
using UnityEngine.UI;

namespace KitchenUIv3
{
    /// <summary>
    /// Khi mon tren the don doi (khach dau hang doi thay doi sau khi nau xong), icon mon "nhay"
    /// (scale 0 -> 1 co nay) va truot nhe tu phai sang. Chi theo doi sprite, khong dong logic.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class KitchenV3OrderPop : MonoBehaviour
    {
        public float thoiGian = 0.35f;
        public float truotX = 40f;
        [Tooltip("Cac Image/TMP khac cung nhay theo (vd Img_Avatar, Txt_Name).")]
        public RectTransform[] nhayTheo;

        private Image _img; private Sprite _cu; private float _t = 1f;
        private RectTransform _rt; private Vector2 _goc;
        private Vector2[] _gocTheo;

        private void Awake()
        {
            _img = GetComponent<Image>(); _rt = (RectTransform)transform; _goc = _rt.anchoredPosition; _cu = _img.sprite;
            _gocTheo = new Vector2[nhayTheo != null ? nhayTheo.Length : 0];
            for (int i = 0; i < _gocTheo.Length; i++) if (nhayTheo[i] != null) _gocTheo[i] = nhayTheo[i].anchoredPosition;
        }

        private void LateUpdate()
        {
            if (!ReferenceEquals(_img.sprite, _cu)) { _cu = _img.sprite; _t = 0f; }
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Mathf.Max(0.05f, thoiGian));
            float k = _t;
            float s = 1f + 0.35f * Mathf.Sin(k * Mathf.PI) * (1f - k);        // nay nhe
            float dx = (1f - Mathf.SmoothStep(0f, 1f, k)) * truotX;
            _rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Mathf.SmoothStep(0f, 1f, k)) * s;
            _rt.anchoredPosition = _goc + new Vector2(dx, 0f);
            for (int i = 0; i < _gocTheo.Length; i++)
                if (nhayTheo[i] != null) nhayTheo[i].anchoredPosition = _gocTheo[i] + new Vector2(dx, 0f);
            if (_t >= 1f) { _rt.localScale = Vector3.one; _rt.anchoredPosition = _goc; for (int i = 0; i < _gocTheo.Length; i++) if (nhayTheo[i] != null) nhayTheo[i].anchoredPosition = _gocTheo[i]; }
        }
    }
}
