using UnityEngine;
using UnityEngine.UI;

namespace KitchenUIv3
{
    /// <summary>
    /// Chay animation frame-by-frame tren mot UI Image (dong ho, noi, lua...).
    /// - Khong dung Animator/AnimationClip: chi doi sprite theo nhip, re va de keo tha.
    /// - Dung unscaledDeltaTime: popup pause game (timeScale = 0) van khong dung hinh
    ///   (bai hoc HarvestFlyItemFX 2026-09-22).
    /// - Khi Dung(): tra ve frame nghi (frameNghi) — vi du noi dong nap, dong ho chi 12h.
    /// </summary>
    [DisallowMultipleComponent]
    public class UISpriteFrameAnimator : MonoBehaviour
    {
        [Tooltip("Cac frame theo thu tu.")]
        public Sprite[] frames;
        [Tooltip("So frame moi giay.")]
        public float fps = 8f;
        [Tooltip("Tu chay ngay khi bat object.")]
        public bool chayKhiBat = false;
        [Tooltip("Frame hien khi dung (thuong la 0).")]
        public int frameNghi = 0;
        [Tooltip("Lap lai vo han. Tat = chay 1 vong roi dung.")]
        public bool lap = true;

        private Image _img;
        private float _t;
        private int _i;
        private bool _dangChay;

        public bool DangChay => _dangChay;

        private void Awake()
        {
            _img = GetComponent<Image>();
            HienFrame(frameNghi);
        }

        private void OnEnable()
        {
            if (chayKhiBat) Chay();
        }

        public void Chay()
        {
            if (frames == null || frames.Length == 0) return;
            _dangChay = true; _t = 0f; _i = 0;
            HienFrame(0);
        }

        public void Dung()
        {
            _dangChay = false;
            HienFrame(frameNghi);
        }

        private void Update()
        {
            if (!_dangChay || frames == null || frames.Length == 0 || fps <= 0f) return;
            _t += Time.unscaledDeltaTime;
            float buoc = 1f / fps;
            while (_t >= buoc)
            {
                _t -= buoc;
                _i++;
                if (_i >= frames.Length)
                {
                    if (lap) _i = 0;
                    else { _i = frames.Length - 1; _dangChay = false; break; }
                }
            }
            HienFrame(_i);
        }

        private void HienFrame(int i)
        {
            if (_img == null) _img = GetComponent<Image>();
            if (_img == null || frames == null || frames.Length == 0) return;
            i = Mathf.Clamp(i, 0, frames.Length - 1);
            if (frames[i] != null && !ReferenceEquals(_img.sprite, frames[i])) _img.sprite = frames[i];
        }
    }
}
