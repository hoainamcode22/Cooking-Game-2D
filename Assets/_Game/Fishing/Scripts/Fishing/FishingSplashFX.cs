using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Splash "tủm" vẽ thủ tục, tham số hoá theo 3 phần: (a) 1-3 vòng ring giãn dần lệch pha 0.08 s, (b) tới 10 giọt nước
    /// bắn vòm parabola ra ngoài rồi rơi + mờ dần, (c) tới 5 đốm bọt trắng nổi trên mặt nước 0.6 s.
    /// Sprite nướng runtime cache static (HideAndDontSave) như ConstructionCelebrationFX. Sorting = layer/order truyền vào
    /// (pool lấy từ SpriteRenderer của phao +1). Không Destroy: hết đời tự SetActive(false) để FishingSplashPool tái dùng.
    /// </summary>
    public class FishingSplashFX : MonoBehaviour
    {
        // Số thuần hình ảnh (không phải gameplay) — giữ const để dễ chỉnh khi Sếp góp ý.
        public const int MaxRings = 3;
        public const int MaxDrops = 10;
        public const int MaxFoam = 5;

        private const float RingLifeSeconds = 0.6f;
        private const float RingStagger = 0.08f;
        private const float DropLifeSeconds = 0.65f;
        private const float FoamLifeSeconds = 0.6f;
        private const float DropSize = 0.045f;
        private const float DropSpeedMin = 0.8f;
        private const float DropSpeedMax = 1.5f;
        private const float Gravity = -4.5f;
        private const float RingStartSize = 0.08f;
        private const float RingEndSize = 0.55f;
        private const float FoamSize = 0.035f;
        private const float FoamDrift = 0.12f;

        private static readonly Color DropColor = new Color(0.85f, 0.95f, 1f, 1f);
        private static readonly Color RingColor = new Color(0.9f, 0.97f, 1f, 0.7f);
        private static readonly Color FoamColor = new Color(1f, 1f, 1f, 0.85f);

        private SpriteRenderer[] _drops;
        private Vector2[] _dropVel;
        private SpriteRenderer[] _rings;
        private SpriteRenderer[] _foam;
        private Vector2[] _foamDir;
        private Vector2 _origin;
        private float _scale = 1f;
        private float _t;
        private float _life;
        private int _ringCount;
        private int _dropCount;
        private int _foamCount;
        private bool _playing;

        public bool IsPlaying { get { return _playing; } }

        /// <summary>Splash to (rơi nước): 3 ring + 9 giọt + bọt. Gọi lại khi đang chạy = chạy lại từ đầu.</summary>
        public void Play(Vector2 worldPos, string sortingLayerName, int sortingOrder, float scale)
        {
            Play(worldPos, sortingLayerName, sortingOrder, scale, 3, 9, 5);
        }

        /// <summary>
        /// Splash tuỳ biến: ringCount (0..3) vòng loang lệch pha, dropCount (0..10) giọt bắn vòm, foamCount (0..5) đốm bọt nổi.
        /// scale nhân kích cỡ + tốc độ giọt (quăng hoàn hảo ×1.3).
        /// </summary>
        public void Play(Vector2 worldPos, string sortingLayerName, int sortingOrder, float scale, int ringCount, int dropCount, int foamCount)
        {
            EnsureChildren();
            _origin = worldPos;
            _scale = scale > 0.05f ? scale : 1f;
            _ringCount = Mathf.Clamp(ringCount, 0, MaxRings);
            _dropCount = Mathf.Clamp(dropCount, 0, MaxDrops);
            _foamCount = Mathf.Clamp(foamCount, 0, MaxFoam);
            _t = 0f;
            float ringLife = _ringCount > 0 ? RingLifeSeconds + RingStagger * (_ringCount - 1) : 0f;
            _life = Mathf.Max(ringLife, Mathf.Max(_dropCount > 0 ? DropLifeSeconds : 0f, _foamCount > 0 ? FoamLifeSeconds : 0f));
            if (_life <= 0f) { Stop(); return; }
            _playing = true;
            transform.position = worldPos;
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }

            for (int i = 0; i < _drops.Length; i++)
            {
                SpriteRenderer d = _drops[i];
                bool on = i < _dropCount;
                d.enabled = on;
                if (!on) { continue; }
                // Toả vòm rộng ra hai bên (35°..145°), hơi lệch ngẫu nhiên — UnityEngine.Random được vì đây là MonoBehaviour hình ảnh.
                float ang = Mathf.Lerp(35f, 145f, (i + 0.5f) / _dropCount) + Random.Range(-8f, 8f);
                float spd = Random.Range(DropSpeedMin, DropSpeedMax) * _scale;
                _dropVel[i] = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * spd;
                d.transform.position = worldPos;
                d.transform.localScale = Vector3.one * DropSize * _scale * Random.Range(0.7f, 1.2f);
                d.color = DropColor;
                d.sortingLayerName = sortingLayerName;
                d.sortingOrder = sortingOrder + 1;
            }

            for (int i = 0; i < _rings.Length; i++)
            {
                SpriteRenderer r = _rings[i];
                bool on = i < _ringCount;
                r.enabled = on;
                if (!on) { continue; }
                r.transform.position = worldPos;
                r.transform.localScale = new Vector3(RingStartSize, RingStartSize * 0.5f, 1f) * _scale;   // dẹt theo phối cảnh iso
                Color c = RingColor; c.a = 0f;   // ring lệch pha chưa tới lượt thì trong suốt
                r.color = c;
                r.sortingLayerName = sortingLayerName;
                r.sortingOrder = sortingOrder;
            }

            for (int i = 0; i < _foam.Length; i++)
            {
                SpriteRenderer f = _foam[i];
                bool on = i < _foamCount;
                f.enabled = on;
                if (!on) { continue; }
                float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                _foamDir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.5f) * Random.Range(0.4f, 1f);
                f.transform.position = worldPos + _foamDir[i] * 0.03f * _scale;
                f.transform.localScale = new Vector3(FoamSize, FoamSize * 0.6f, 1f) * _scale * Random.Range(0.7f, 1.3f);
                f.color = FoamColor;
                f.sortingLayerName = sortingLayerName;
                f.sortingOrder = sortingOrder + 1;
            }
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

            // (b) Giọt: parabola + mờ dần; rơi qua mặt nước (dưới gốc) thì tắt sớm cho khỏi "xuyên nước".
            float du = Mathf.Clamp01(_t / DropLifeSeconds);
            for (int i = 0; i < _dropCount; i++)
            {
                Vector2 p = _origin + _dropVel[i] * _t + new Vector2(0f, 0.5f * Gravity * _scale * _t * _t);
                bool below = p.y < _origin.y - 0.03f * _scale;
                _drops[i].transform.position = p;
                Color c = DropColor; c.a = below ? 0f : 1f - du * du;
                _drops[i].color = c;
            }

            // (a) Ring: mỗi vòng bắt đầu trễ RingStagger × i, giãn theo sqrt (nhanh lúc đầu) và mờ dần.
            for (int i = 0; i < _ringCount; i++)
            {
                float lt = _t - RingStagger * i;
                Color rc = RingColor;
                if (lt <= 0f) { rc.a = 0f; _rings[i].color = rc; continue; }
                float u = Mathf.Clamp01(lt / RingLifeSeconds);
                float size = Mathf.Lerp(RingStartSize, RingEndSize, Mathf.Sqrt(u)) * _scale * (1f - 0.15f * i);
                _rings[i].transform.localScale = new Vector3(size, size * 0.5f, 1f);
                rc.a = RingColor.a * (1f - u);
                _rings[i].color = rc;
            }

            // (c) Bọt: trôi chậm ra ngoài, nhấp nháy nhẹ rồi tan.
            float fu = Mathf.Clamp01(_t / FoamLifeSeconds);
            for (int i = 0; i < _foamCount; i++)
            {
                _foam[i].transform.position = _origin + _foamDir[i] * (0.03f + FoamDrift * fu) * _scale;
                Color fc = FoamColor;
                fc.a = FoamColor.a * (1f - fu) * (0.75f + 0.25f * Mathf.Sin(_t * 18f + i));
                _foam[i].color = fc;
            }

            if (_t >= _life) { Stop(); }
        }

        private void OnDisable()
        {
            _playing = false;
        }

        private void EnsureChildren()
        {
            if (_drops != null && _drops.Length == MaxDrops && _rings != null && _rings.Length == MaxRings && _foam != null && _foam.Length == MaxFoam) { return; }
            _drops = new SpriteRenderer[MaxDrops];
            _dropVel = new Vector2[MaxDrops];
            for (int i = 0; i < MaxDrops; i++) { _drops[i] = MakeChild("Drop_" + i, DropSprite()); }
            _rings = new SpriteRenderer[MaxRings];
            for (int i = 0; i < MaxRings; i++) { _rings[i] = MakeChild("Ring_" + i, RingSprite()); }
            _foam = new SpriteRenderer[MaxFoam];
            _foamDir = new Vector2[MaxFoam];
            for (int i = 0; i < MaxFoam; i++) { _foam[i] = MakeChild("Foam_" + i, DropSprite()); }
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

        /// <summary>Chấm tròn mềm — giọt nước / bọt.</summary>
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
