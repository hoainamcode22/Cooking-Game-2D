using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenUIv2
{
    /// <summary>Chữ zZz bay lên lơ lửng + mèo ngủ thở phập phồng và quẫy đuôi nhấp nhô nhẹ.</summary>
    public class KitchenZzzFloat : MonoBehaviour
    {
        public TMP_Text txt;
        public float cycle = 2.4f;
        private float _t;
        private RectTransform _catRt;

        private void Awake()
        {
            _catRt = (RectTransform)transform;
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;

            // Thân mèo thở phập phồng + quẫy đuôi nhấp nhô theo nhịp ngủ
            if (_catRt != null)
            {
                float breath = Mathf.Sin(_t * (6.283f / cycle));
                float scaleY = 1f + 0.045f * breath;
                float rotZ = 1.2f * Mathf.Sin(_t * 3.5f);
                _catRt.localScale = new Vector3(1f, scaleY, 1f);
                _catRt.localEulerAngles = new Vector3(0f, 0f, rotZ);
            }

            if (txt == null) return;
            float p = (_t % cycle) / cycle;
            var rt = txt.rectTransform;
            rt.anchoredPosition = new Vector2(12f + 7f * Mathf.Sin(p * 6.283f), 6f + 36f * p);
            var c = txt.color;
            c.a = p < 0.15f ? p / 0.15f : 1f - (p - 0.15f) / 0.85f;
            txt.color = c;
            float sc = 0.7f + 0.45f * p;
            rt.localScale = new Vector3(sc, sc, 1f);
        }
    }
}
