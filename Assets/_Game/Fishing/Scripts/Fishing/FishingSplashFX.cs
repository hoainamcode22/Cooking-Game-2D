using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Splash "tủm" vẽ thủ tục: 6-8 giọt nước tròn trắng-xanh bay lên rồi rơi + 1 vòng tròn loang, sống 0.6s.
    /// Sprite nướng runtime cache static (HideAndDontSave) như ConstructionCelebrationFX. Sorting = layer/order truyền vào
    /// (pool lấy từ SpriteRenderer của phao +100). Không Destroy: hết đời tự SetActive(false) để FishingSplashPool tái dùng.
    /// </summary>
    public class FishingSplashFX : MonoBehaviour
    {
        // Số thuần hình ảnh (không phải gameplay) — giữ const để dễ chỉnh khi Sếp góp ý.
        private const int DropCount = 7;
        private const float LifeSeconds = 0.6f;
        private const float DropSize = 0.05f;
        private const float DropSpeedMin = 0.9f;
        private const float DropSpeedMax = 1.6f;
        private const float Gravity = -4.5f;
        private const float RingStartSize = 0.08f;
        private const float RingEndSize = 0.55f;

        private static readonly Color DropColor = new Color(0.85f, 0.95f, 1f, 1f);
        private static readonly Color RingColor = new Color(0.9f, 0.97f, 1f, 0.7f);

        private SpriteRenderer[] _drops;
        private Vector2[] _vel;
        private SpriteRenderer _ring;
        private Vector2 _origin;
        private float _scale = 1f;
        private float _t;
        private bool _playing;

        public bool IsPlaying { get { return _playing; } }

        /// <summary>Bắt đầu splash tại worldPos với sorting cho trước. Gọi lại khi đang chạy = chạy lại từ đầu.</summary>
        public void Play(Vector2 worldPos, string sortingLayerName, int sortingOrder, float scale)
        {
            EnsureChildren();
            _origin = worldPos;
            _scale = scale > 0.05f ? scale : 1f;
            _t = 0f;
            _playing = true;
            transform.position = worldPos;
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }

            for (int i = 0; i < _drops.Length; i++)
            {
                SpriteRenderer d = _drops[i];
                // Toả hình quạt lên trên, hơi lệch ngẫu nhiên — UnityEngine.Random được vì đây là MonoBehaviour hình ảnh.
                float ang = Mathf.Lerp(50f, 130f, (i + 0.5f) / _drops.Length) + Random.Range(-10f, 10f);
                float spd = Random.Range(DropSpeedMin, DropSpeedMax) * _scale;
                _vel[i] = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * spd;
                d.transform.position = worldPos;
                d.transform.localScale = Vector3.one * DropSize * _scale * Random.Range(0.7f, 1.2f);
                d.color = DropColor;
                d.sortingLayerName = sortingLayerName;
                d.sortingOrder = sortingOrder + 1;
                d.enabled = true;
            }
            _ring.transform.position = worldPos;
            _ring.transform.localScale = new Vector3(RingStartSize, RingStartSize * 0.5f, 1f) * _scale;   // dẹt theo phối cảnh iso
            _ring.color = RingColor;
            _ring.sortingLayerName = sortingLayerName;
            _ring.sortingOrder = sortingOrder;
            _ring.enabled = true;
        }

        public void Stop()
        {
            _playing = false;
            if (gameObject.activeSelf) { gameObject.SetActive(false); }
        }

        private void Update()
        {
            if (!_playing) { return; }
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / LifeSeconds);
            float alpha = 1f - u;

            for (int i = 0; i < _drops.Length; i++)
            {
                Vector2 p = _origin + _vel[i] * _t + new Vector2(0f, 0.5f * Gravity * _scale * _t * _t);
                _drops[i].transform.position = p;
                Color c = DropColor; c.a = alpha;
                _drops[i].color = c;
            }

            float size = Mathf.Lerp(RingStartSize, RingEndSize, Mathf.Sqrt(u)) * _scale;
            _ring.transform.localScale = new Vector3(size, size * 0.5f, 1f);
            Color rc = RingColor; rc.a = RingColor.a * alpha;
            _ring.color = rc;

            if (u >= 1f) { Stop(); }
        }

        private void OnDisable()
        {
            _playing = false;
        }

        private void EnsureChildren()
        {
            if (_drops != null && _drops.Length == DropCount && _ring != null) { return; }
            _drops = new SpriteRenderer[DropCount];
            _vel = new Vector2[DropCount];
            for (int i = 0; i < DropCount; i++) { _drops[i] = MakeChild("Drop_" + i, DropSprite()); }
            _ring = MakeChild("Ring", RingSprite());
        }

        private SpriteRenderer MakeChild(string name, Sprite sprite)
        {
            Transform existing = transform.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { sr = go.AddComponent<SpriteRenderer>(); }
            sr.sprite = sprite;
            sr.enabled = false;
            return sr;
        }

        // ── Sprite nướng runtime, cache static, tự sinh lại nếu texture bị huỷ (tắt Domain Reload) ──
        private static Sprite _sprDrop;
        private static Sprite _sprRing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _sprDrop = null; _sprRing = null; }

        private static bool Dead(Sprite s) { return s == null || s.texture == null; }

        /// <summary>Chấm tròn mềm — giọt nước.</summary>
        public static Sprite DropSprite()
        {
            if (!Dead(_sprDrop)) { return _sprDrop; }
            _sprDrop = Bake(32, 32, (x, y) =>
            {
                float dx = (x + 0.5f - 16f) / 14f;
                float dy = (y + 0.5f - 16f) / 14f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                return new Color(1f, 1f, 1f, Mathf.Clamp01((1f - r) * 5f));
            });
            return _sprDrop;
        }

        /// <summary>Vành tròn mảnh — vòng loang trên mặt nước.</summary>
        public static Sprite RingSprite()
        {
            if (!Dead(_sprRing)) { return _sprRing; }
            _sprRing = Bake(64, 64, (x, y) =>
            {
                float dx = (x + 0.5f - 32f) / 30f;
                float dy = (y + 0.5f - 32f) / 30f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.85f) / 0.12f);
                return new Color(1f, 1f, 1f, a);
            });
            return _sprRing;
        }

        /// <summary>Nướng texture theo hàm màu; pixelsPerUnit = chiều cao ⇒ sprite cao 1 unit, localScale = kích cỡ world.</summary>
        public static Sprite Bake(int w, int h, System.Func<int, int, Color> pixel)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) { cols[y * w + x] = pixel(x, y); }
            }
            tex.SetPixels(cols);
            tex.Apply(false, false);
            var spr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h, 0, SpriteMeshType.FullRect);
            spr.hideFlags = HideFlags.HideAndDontSave;
            return spr;
        }
    }
}
