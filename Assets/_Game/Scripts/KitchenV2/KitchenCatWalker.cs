using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenUIv2
{
    /// <summary>Mèo đầu bếp đi qua đi lại trên sàn bếp: đi → dừng nghỉ → quay đầu đi tiếp. Frame do agent-sprite-forge vẽ.</summary>
    public class KitchenCatWalker : MonoBehaviour
    {
        public Sprite[] frames;
        public float speed = 85f;
        public float minX = -280f, maxX = 260f;
        public float frameTime = 0.14f;
        public Vector2 pauseRange = new Vector2(1.2f, 3.2f);

        private Image _img;
        private RectTransform _rt;
        private float _dir = 1f;
        private float _pauseT;
        private float _frameT;
        private int _frame;

        private void Awake()
        {
            _img = GetComponent<Image>();
            _rt = (RectTransform)transform;
        }

        private void Start()
        {
            if (_rt != null)
            {
                var pos = _rt.anchoredPosition;
                if (pos.x < minX) pos.x = minX;
                if (pos.x > maxX) pos.x = maxX;
                _rt.anchoredPosition = pos;
            }
        }

        private void Update()
        {
            if (_img == null || _rt == null || frames == null || frames.Length == 0) return;

            if (_pauseT > 0f)
            {
                _pauseT -= Time.unscaledDeltaTime;
                if (frames[0] != null) _img.sprite = frames[0]; // đứng yên = frame đầu
                return;
            }

            var pos = _rt.anchoredPosition;
            pos.x += _dir * speed * Time.unscaledDeltaTime;
            if (pos.x >= maxX)
            {
                pos.x = maxX;
                _dir = -1f;
                _pauseT = Random.Range(pauseRange.x, pauseRange.y);
            }
            else if (pos.x <= minX)
            {
                pos.x = minX;
                _dir = 1f;
                _pauseT = Random.Range(pauseRange.x, pauseRange.y);
            }
            _rt.anchoredPosition = pos;

            // Lật mặt theo hướng đi (frame gốc vẽ hướng PHẢI)
            var sc = _rt.localScale;
            sc.x = Mathf.Abs(sc.x) * (_dir >= 0f ? 1f : -1f);
            _rt.localScale = sc;

            _frameT += Time.unscaledDeltaTime;
            if (_frameT >= frameTime)
            {
                _frameT = 0f;
                _frame = (_frame + 1) % frames.Length;
                if (frames[_frame] != null) _img.sprite = frames[_frame];
            }
        }
    }
}
