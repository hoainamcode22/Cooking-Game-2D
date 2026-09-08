using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Phao câu: SpriteRenderer; chưa gán sprite tay thì vẽ thủ tục hình tròn đỏ/trắng 32px (cache static HideAndDontSave)
    /// — Sếp/đội vẽ gán bobberSprite thay sau. Bay parabola tới điểm nước (FlyTo), dập dềnh sin nhẹ khi nổi, giật mạnh khi cá cắn.
    /// Sorting: layer qua TouristSortingLayers.ResolveOrOverride(Visitor), order = order người chơi + 10.
    /// Con "Bobber" của FishingController (EnsureVisuals tạo).
    /// </summary>
    public class FishingBobber : MonoBehaviour
    {
        // Số thuần hình ảnh.
        private const float BobberWorldSize = 0.14f;   // đường kính phao (unit) khi dùng sprite thủ tục
        private const float ArcHeight = 0.35f;         // độ cao parabola khi bay
        private const float BobAmplitude = 0.02f;
        private const float BobFrequency = 2.2f;
        private const float BiteAmplitude = 0.06f;
        private const float BiteFrequency = 16f;
        private const int OrderAbovePlayer = 10;

        [Tooltip("Sprite phao vẽ tay; để trống = vẽ thủ tục tròn đỏ/trắng.")]
        [SerializeField] private Sprite bobberSprite;
        [Tooltip("Để trống = tự chọn layer theo TouristSortingLayers.Visitor.")]
        [SerializeField] private string sortingLayerName;

        private SpriteRenderer _sr;
        private Vector2 _from;
        private Vector2 _to;
        private float _duration;
        private float _t;
        private bool _flying;
        private bool _inWater;
        private bool _biting;
        private float _bobTime;

        /// <summary>Phao đã chạm nước (đang nổi).</summary>
        public bool IsInWater { get { return _inWater; } }
        /// <summary>Điểm nước đích (đúng cả khi đang bay).</summary>
        public Vector2 WaterPoint { get { return _to; } }
        /// <summary>Renderer để FX khác đọc sorting (splash +100).</summary>
        public SpriteRenderer Renderer { get { EnsureRenderer(); return _sr; } }

        private void Awake()
        {
            EnsureRenderer();
            Hide();
        }

        /// <summary>Bay parabola từ from tới to trong duration giây rồi tự chuyển sang nổi (IsInWater = true).</summary>
        public void FlyTo(Vector2 from, Vector2 to, float duration)
        {
            EnsureRenderer();
            ApplySorting();
            _from = from;
            _to = to;
            _duration = duration > 0.01f ? duration : 0.01f;
            _t = 0f;
            _flying = true;
            _inWater = false;
            _biting = false;
            _bobTime = 0f;
            transform.position = from;
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            _sr.enabled = true;
        }

        /// <summary>Đổi kiểu dập dềnh: biting = giật mạnh/nhanh.</summary>
        public void Bob(bool biting)
        {
            _biting = biting;
        }

        public void Hide()
        {
            _flying = false;
            _inWater = false;
            _biting = false;
            if (_sr != null) { _sr.enabled = false; }
            if (gameObject.activeSelf) { gameObject.SetActive(false); }
        }

        private void Update()
        {
            if (_flying)
            {
                _t += Time.deltaTime;
                float u = Mathf.Clamp01(_t / _duration);
                Vector2 p = Vector2.Lerp(_from, _to, u);
                p.y += Mathf.Sin(u * Mathf.PI) * ArcHeight;
                transform.position = p;
                if (u >= 1f)
                {
                    _flying = false;
                    _inWater = true;
                    _bobTime = 0f;
                    transform.position = _to;
                }
                return;
            }

            if (_inWater)
            {
                _bobTime += Time.deltaTime;
                float amp = _biting ? BiteAmplitude : BobAmplitude;
                float freq = _biting ? BiteFrequency : BobFrequency;
                float dy = Mathf.Sin(_bobTime * freq) * amp;
                // Cắn thì thêm rung ngang nhẹ cho "giật".
                float dx = _biting ? Mathf.Sin(_bobTime * freq * 1.7f) * amp * 0.5f : 0f;
                transform.position = _to + new Vector2(dx, dy);
            }
        }

        private void EnsureRenderer()
        {
            if (_sr != null) { return; }
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
        }

        private void ApplySorting()
        {
            if (_sr == null) { return; }
            _sr.sortingLayerName = TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Visitor);
            int baseOrder = 0;
            FishingPlayerController p = FishingPlayerController.Local;
            if (p != null)
            {
                SpriteRenderer psr = p.GetComponentInChildren<SpriteRenderer>();
                if (psr != null) { baseOrder = psr.sortingOrder; }
            }
            _sr.sortingOrder = baseOrder + OrderAbovePlayer;
        }

        // ── Sprite thủ tục cache static ──
        private static Sprite _sprBobber;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _sprBobber = null; }

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
    }
}
