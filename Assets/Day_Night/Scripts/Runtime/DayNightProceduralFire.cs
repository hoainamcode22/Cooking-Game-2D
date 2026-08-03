using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

namespace Day_Night
{
    /// <summary>
    /// Ngọn lửa vẽ bằng SpriteRenderer (render chắc chắn trong URP 2D — y như cỏ/cây).
    /// Tự sinh sprite ngọn lửa (gradient cam→vàng), material UNLIT (luôn sáng, không bị đèn làm tối),
    /// kích thước theo đơn vị world. KHÔNG tạo object trong OnValidate (Unity cấm).
    /// </summary>
    [ExecuteAlways]
    public class DayNightProceduralFire : MonoBehaviour
    {
        [Min(0.05f)] public float Width = 50f;
        [Min(0.05f)] public float Height = 75f;
        [Range(0.1f, 4f)] public float FlickerSpeed = 2.2f;
        [Range(0f, 1f)] public float FlickerAmount = 0.18f;
        public Texture2D FlameTexture;                 // (tùy chọn — không bắt buộc)
        public string SortingLayerName = "Foreground";
        public int SortingOrder = 1000;                // PHẢI cao hơn decor map (logpile=700)

        private const string ChildName = "FireSprite";

        private Transform _child;
        private SpriteRenderer _sr;
        private VisualEffect _vfx;
        private static Sprite _flame;
        private static Material _unlitMat;
        private static readonly PropertyInfo VfxSortingLayerNameProperty = typeof(VisualEffect).GetProperty("sortingLayerName");
        private static readonly PropertyInfo VfxSortingOrderProperty = typeof(VisualEffect).GetProperty("sortingOrder");
        private float _baseX, _baseY;

        private void OnEnable() => Build();   // OnEnable ĐƯỢC PHÉP tạo object (khác OnValidate)

        private void Update()
        {
            if (_sr == null) Build();
            Apply();
            if (_vfx != null && _vfx.enabled) _vfx.Play();
            Flicker();
        }

        private void Build()
        {
            _vfx = GetComponent<VisualEffect>();

            // Tắt MeshRenderer rác cũ (URP 2D không vẽ được, có Font Material).
            var oldMr = GetComponent<MeshRenderer>();
            if (oldMr != null) oldMr.enabled = false;

            if (_child == null)
            {
                Transform found = transform.Find(ChildName);
                _child = found != null ? found : new GameObject(ChildName).transform;
                _child.SetParent(transform, false);
                _child.localPosition = Vector3.zero;
                _child.localRotation = Quaternion.identity;
            }

            _sr = _child.GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = _child.gameObject.AddComponent<SpriteRenderer>();

            if (_flame == null) _flame = BuildFlameSprite();
            _sr.sprite = _flame;
            _sr.color = Color.white;

            // UNLIT → lửa luôn sáng rực, không bị đèn 2D làm tối.
            if (_unlitMat == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                if (sh != null) _unlitMat = new Material(sh) { hideFlags = HideFlags.DontSave };
            }
            if (_unlitMat != null) _sr.sharedMaterial = _unlitMat;
        }

        private void Apply()
        {
            if (_sr == null) return;
            _sr.sortingLayerName = SortingLayerName;
            _sr.sortingOrder = SortingOrder;
            ApplyVfxSorting();

            Vector3 pls = transform.lossyScale;
            _baseX = Width  / Mathf.Max(Mathf.Abs(pls.x), 1e-4f);
            _baseY = Height / Mathf.Max(Mathf.Abs(pls.y), 1e-4f);
        }

        private void ApplyVfxSorting()
        {
            if (_vfx == null) return;

            // VFX Graph serializes renderer sorting separately from the fallback sprite,
            // so keep the particle sparks on the same layer/order as the flame sprite.
            SetVisualEffectProperty(VfxSortingLayerNameProperty, SortingLayerName);
            SetVisualEffectProperty(VfxSortingOrderProperty, SortingOrder);
        }

        private void SetVisualEffectProperty(PropertyInfo property, object value)
        {
            if (property == null || !property.CanWrite) return;

            property.SetValue(_vfx, value);
        }

        private void Flicker()
        {
            if (_child == null) return;
            float t = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float wx = 1f + Mathf.Sin(t * FlickerSpeed * 6.2f)        * FlickerAmount * 0.45f;
            float wy = 1f + Mathf.Sin(t * FlickerSpeed * 8.1f + 1.3f) * FlickerAmount;
            _child.localScale = new Vector3(_baseX * wx, _baseY * wy, 1f);

            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = Mathf.Clamp01(0.9f + Mathf.Sin(t * FlickerSpeed * 5f) * 0.1f);
                _sr.color = c;
            }
        }

        // Texture ngọn lửa hình giọt: phình dưới, nhọn trên, gradient đỏ-cam→vàng, lõi sáng, viền mềm.
        private static Sprite BuildFlameSprite()
        {
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

            float cx = (S - 1) * 0.5f;
            for (int y = 0; y < S; y++)
            {
                float ny = y / (float)(S - 1);
                float prof = Mathf.Sin(Mathf.Pow(ny, 0.62f) * Mathf.PI);
                float halfW = prof * 0.44f * S;

                Color baseCol;
                if (ny < 0.35f)
                    baseCol = Color.Lerp(new Color(1f, 0.22f, 0.03f), new Color(1f, 0.5f, 0.05f), ny / 0.35f);
                else if (ny < 0.72f)
                    baseCol = Color.Lerp(new Color(1f, 0.5f, 0.05f), new Color(1f, 0.82f, 0.2f), (ny - 0.35f) / 0.37f);
                else
                    baseCol = Color.Lerp(new Color(1f, 0.82f, 0.2f), new Color(1f, 0.97f, 0.62f), (ny - 0.72f) / 0.28f);

                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float alpha = halfW <= 0.5f ? 0f : Mathf.Clamp01((halfW - dx) / (S * 0.06f));
                    float core = Mathf.Clamp01((halfW * 0.5f - dx) / (S * 0.1f)) * Mathf.Clamp01((0.55f - ny) / 0.45f);
                    Color px = Color.Lerp(baseCol, new Color(1f, 0.98f, 0.82f), core * 0.7f);
                    px.a = alpha;
                    tex.SetPixel(x, y, px);
                }
            }
            tex.Apply();

            var sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0f), S);
            sp.name = "ProceduralFlame";
            sp.hideFlags = HideFlags.DontSave;
            return sp;
        }
    }
}
