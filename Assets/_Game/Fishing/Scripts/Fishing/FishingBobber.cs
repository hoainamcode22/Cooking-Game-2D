using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Phao câu: SpriteRenderer; chưa gán sprite tay thì vẽ thủ tục hình tròn đỏ/trắng 32px (cache static HideAndDontSave)
    /// — Sếp/đội vẽ gán bobberSprite thay sau. Con "Sinker" = cục chì đen nhỏ (ellipse 0.05) treo dưới phao (luôn vẽ code).
    /// Bay parabola tới điểm nước (FlyTo); CHẠM NƯỚC (hết FlyTo) → tự phát splash to (FishingSplashPool.Play) + FishingAudio.PlaySplash,
    /// lún xuống 0.06 rồi nổi lên dập dềnh; cá cắn (Bob(true)) → giật xuống 0.08 nhanh + 1 ring nhỏ mỗi 0.4 s.
    /// Sorting: layer qua TouristSortingLayers.ResolveOrOverride(Visitor), order = order người chơi + 10 (chì = +9).
    /// Con "Bobber" của FishingController (EnsureVisuals tạo).
    /// </summary>
    public class FishingBobber : MonoBehaviour
    {
        // Số thuần hình ảnh.
        private const float BobberWorldSize = 0.14f;   // đường kính phao (unit) khi dùng sprite thủ tục
        private const float ArcHeight = 0.35f;         // độ cao parabola khi bay (nhân arcMultiplier)
        private const float BobAmplitude = 0.02f;
        private const float BobFrequency = 2.2f;
        private const float BiteAmplitude = 0.06f;
        private const float BiteFrequency = 16f;
        private const float SinkDepth = 0.06f;         // lún khi vừa chạm nước
        private const float SinkDuration = 0.35f;
        private const float BiteJerkDepth = 0.08f;     // giật xuống khi cá cắn
        private const float BiteJerkDown = 0.08f;      // giây đi xuống
        private const float BiteRingInterval = 0.4f;
        private const float BiteRingScale = 0.6f;
        private const float SinkerWorldHeight = 0.05f;
        private const float SinkerGap = 0.035f;        // khoảng hở giữa đáy phao và tâm chì
        private const float SinkerAlphaInWater = 0.7f; // chì chìm dưới nước → hơi mờ
        private const int OrderAbovePlayer = 10;
        private const string SinkerChildName = "Sinker";

        [Tooltip("Sprite phao vẽ tay; để trống = vẽ thủ tục tròn đỏ/trắng.")]
        [SerializeField] private Sprite bobberSprite;
        [Tooltip("Để trống = tự chọn layer theo TouristSortingLayers.Visitor.")]
        [SerializeField] private string sortingLayerName;

        private SpriteRenderer _sr;
        private SpriteRenderer _sinker;
        private Vector2 _from;
        private Vector2 _to;
        private float _duration;
        private float _arcMul = 1f;
        private float _splashScale = 1f;
        private float _t;
        private bool _flying;
        private bool _inWater;
        private bool _biting;
        private float _bobTime;
        private float _landTime;
        private float _biteTime;
        private float _biteRingTimer;

        /// <summary>Phao đã chạm nước (đang nổi).</summary>
        public bool IsInWater { get { return _inWater; } }
        /// <summary>Điểm nước đích (đúng cả khi đang bay).</summary>
        public Vector2 WaterPoint { get { return _to; } }
        /// <summary>Renderer để FX khác đọc sorting (splash +1).</summary>
        public SpriteRenderer Renderer { get { EnsureRenderer(); return _sr; } }

        private void Awake()
        {
            EnsureRenderer();
            Hide();
        }

        /// <summary>Bay parabola từ from tới to trong duration giây rồi tự chuyển sang nổi (IsInWater = true) + splash chuẩn.</summary>
        public void FlyTo(Vector2 from, Vector2 to, float duration)
        {
            FlyTo(from, to, duration, 1f, 1f);
        }

        /// <summary>
        /// Như FlyTo nhưng chỉnh đỉnh parabola (arcMultiplier, quăng hoàn hảo 1.4) và cỡ splash lúc chạm nước (splashScale, hoàn hảo 1.3).
        /// </summary>
        public void FlyTo(Vector2 from, Vector2 to, float duration, float arcMultiplier, float splashScale)
        {
            EnsureRenderer();
            ApplySorting();
            _from = from;
            _to = to;
            _duration = duration > 0.01f ? duration : 0.01f;
            _arcMul = arcMultiplier > 0.1f ? arcMultiplier : 1f;
            _splashScale = splashScale > 0.05f ? splashScale : 1f;
            _t = 0f;
            _flying = true;
            _inWater = false;
            _biting = false;
            _bobTime = 0f;
            _landTime = 0f;
            transform.position = from;
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            _sr.enabled = true;
            if (_sinker != null) { _sinker.enabled = true; SetSinkerAlpha(1f); }
        }

        /// <summary>Đổi kiểu dập dềnh: biting = giật xuống mạnh rồi rung nhanh + ring nhỏ lặp; false = dập dềnh thường.</summary>
        public void Bob(bool biting)
        {
            if (biting && !_biting)
            {
                _biteTime = 0f;
                _biteRingTimer = 0f;   // ring đầu tiên phát ngay
            }
            _biting = biting;
        }

        public void Hide()
        {
            _flying = false;
            _inWater = false;
            _biting = false;
            if (_sr != null) { _sr.enabled = false; }
            if (_sinker != null) { _sinker.enabled = false; }
            if (gameObject.activeSelf) { gameObject.SetActive(false); }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_flying)
            {
                _t += dt;
                float u = Mathf.Clamp01(_t / _duration);
                Vector2 p = Vector2.Lerp(_from, _to, u);
                p.y += Mathf.Sin(u * Mathf.PI) * ArcHeight * _arcMul;
                transform.position = p;
                if (u >= 1f) { Land(); }
                return;
            }

            if (!_inWater) { return; }

            _bobTime += dt;
            _landTime += dt;

            // Lún khi vừa chạm nước: nửa sóng sin đi xuống rồi nổi lên trong SinkDuration; dập dềnh vào dần sau đó.
            float sink = _landTime < SinkDuration ? -SinkDepth * Mathf.Sin(Mathf.PI * _landTime / SinkDuration) : 0f;
            float bobIn = Mathf.Clamp01((_landTime - SinkDuration * 0.6f) / 0.3f);

            float dx = 0f;
            float dy = sink;
            if (_biting)
            {
                _biteTime += dt;
                // Giật xuống nhanh rồi hồi theo mũ.
                float jerk = _biteTime < BiteJerkDown
                    ? -BiteJerkDepth * (_biteTime / BiteJerkDown)
                    : -BiteJerkDepth * Mathf.Exp(-(_biteTime - BiteJerkDown) * 10f);
                dy += jerk + Mathf.Sin(_bobTime * BiteFrequency) * BiteAmplitude;
                dx = Mathf.Sin(_bobTime * BiteFrequency * 1.7f) * BiteAmplitude * 0.5f;

                _biteRingTimer -= dt;
                if (_biteRingTimer <= 0f)
                {
                    _biteRingTimer = BiteRingInterval;
                    FishingSplashPool.Shared.PlayRing(_to, _sr, BiteRingScale);
                }
            }
            else
            {
                dy += Mathf.Sin(_bobTime * BobFrequency) * BobAmplitude * bobIn;
            }
            transform.position = _to + new Vector2(dx, dy);
        }

        /// <summary>Hết FlyTo: đặt phao tại điểm nước, splash to + tiếng "tủm", bắt đầu tween lún.</summary>
        private void Land()
        {
            _flying = false;
            _inWater = true;
            _bobTime = 0f;
            _landTime = 0f;
            transform.position = _to;
            FishingSplashPool.Shared.Play(_to, _sr, _splashScale);
            FishingAudio.PlaySplash();
            SetSinkerAlpha(SinkerAlphaInWater);
        }

        private void EnsureRenderer()
        {
            if (_sr != null && _sinker != null) { return; }
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) { _sr = gameObject.AddComponent<SpriteRenderer>(); }
            if (bobberSprite != null)
            {
                _sr.sprite = bobberSprite;
                transform.localScale = Vector3.one;
            }
            else
            {
                _sr.sprite = ProceduralSprite();
                // Sprite thủ tục cao 1 unit ⇒ scale = đường kính mong muốn.
                transform.localScale = Vector3.one * BobberWorldSize;
            }
            _sr.color = Color.white;
            EnsureSinker();
        }

        /// <summary>Con "Sinker": chì đen treo dưới đáy phao, cỡ world SinkerWorldHeight bất kể scale/sprite phao.</summary>
        private void EnsureSinker()
        {
            Transform st = transform.Find(SinkerChildName);
            GameObject go = st != null ? st.gameObject : new GameObject(SinkerChildName);
            go.transform.SetParent(transform, false);
            _sinker = go.GetComponent<SpriteRenderer>();
            if (_sinker == null) { _sinker = go.AddComponent<SpriteRenderer>(); }
            _sinker.sprite = SinkerSprite();
            _sinker.color = Color.white;

            float ps = Mathf.Max(0.0001f, transform.localScale.y);
            float halfHeightWorld = _sr.sprite != null ? _sr.sprite.bounds.extents.y * ps : BobberWorldSize * 0.5f;
            go.transform.localScale = Vector3.one * (SinkerWorldHeight / ps);
            go.transform.localPosition = new Vector3(0f, -(halfHeightWorld + SinkerGap) / ps, 0f);
            _sinker.enabled = _sr.enabled;
        }

        private void SetSinkerAlpha(float a)
        {
            if (_sinker == null) { return; }
            Color c = _sinker.color; c.a = a;
            _sinker.color = c;
        }

        private void ApplySorting()
        {
            if (_sr == null) { return; }
            string layer = TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Visitor);
            _sr.sortingLayerName = layer;
            int baseOrder = 0;
            FishingPlayerController p = FishingPlayerController.Local;
            if (p != null)
            {
                SpriteRenderer psr = p.GetComponentInChildren<SpriteRenderer>();
                if (psr != null) { baseOrder = psr.sortingOrder; }
            }
            _sr.sortingOrder = baseOrder + OrderAbovePlayer;
            if (_sinker != null)
            {
                _sinker.sortingLayerName = layer;
                _sinker.sortingOrder = baseOrder + OrderAbovePlayer - 1;   // dưới phao một bậc để phao che khi chồng
            }
        }

        // ── Sprite thủ tục cache static ──
        private static Sprite _sprBobber;
        private static Sprite _sprSinker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _sprBobber = null; _sprSinker = null; }

        /// <summary>Tròn 32px: nửa trên đỏ, nửa dưới trắng, viền tối mảnh, highlight nhỏ. Tự sinh lại nếu texture bị huỷ.</summary>
        public static Sprite ProceduralSprite()
        {
            if (_sprBobber != null && _sprBobber.texture != null) { return _sprBobber; }
            _sprBobber = FishingSplashFX.Bake(32, 32, (x, y) =>
            {
                float dx = (x + 0.5f - 16f) / 15f;
                float dy = (y + 0.5f - 16f) / 15f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1f) { return Color.clear; }
                float edge = Mathf.Clamp01((1f - r) * 8f);                    // khử răng cưa mép
                Color body = dy > 0f ? new Color(0.9f, 0.2f, 0.18f) : new Color(0.97f, 0.97f, 0.95f);
                if (r > 0.86f) { body = Color.Lerp(body, new Color(0.25f, 0.1f, 0.1f), 0.75f); }   // viền tối
                float hx = (dx + 0.35f) / 0.25f, hy = (dy - 0.4f) / 0.25f;
                if (hx * hx + hy * hy < 1f) { body = Color.Lerp(body, Color.white, 0.6f); }        // highlight
                body.a = edge;
                return body;
            });
            return _sprBobber;
        }

        /// <summary>Chì: ellipse đứng 20x32 px đen xám, highlight mờ góc trên trái. Cao 1 unit (Bake) ⇒ scale = chiều cao world.</summary>
        public static Sprite SinkerSprite()
        {
            if (_sprSinker != null && _sprSinker.texture != null) { return _sprSinker; }
            _sprSinker = FishingSplashFX.Bake(20, 32, (x, y) =>
            {
                float dx = (x + 0.5f - 10f) / 9f;
                float dy = (y + 0.5f - 16f) / 15f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1f) { return Color.clear; }
                float edge = Mathf.Clamp01((1f - r) * 6f);
                // Tối dần ra mép cho có khối; highlight nhỏ phía trên-trái.
                float shade = Mathf.Lerp(0.3f, 0.1f, r);
                Color body = new Color(shade, shade, shade + 0.02f);
                float hx = (dx + 0.35f) / 0.3f, hy = (dy - 0.35f) / 0.35f;
                if (hx * hx + hy * hy < 1f) { body = Color.Lerp(body, new Color(0.6f, 0.6f, 0.62f), 0.5f); }
                body.a = edge;
                return body;
            });
            return _sprSinker;
        }
    }
}
