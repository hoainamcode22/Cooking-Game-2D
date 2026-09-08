using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Hình cần câu + dây: SpriteRenderer cần (rodSprite; trống → thanh nâu 4x60 px thủ tục) neo ở FishingPlayerController.Local.HandAnchor,
    /// xoay theo hướng nhìn (Down: chĩa xuống-phải, Up: lên-phải, Left/Right: theo hướng, hơi hếch lên);
    /// LineRenderer (Sprites/Default, width 0.01, trắng ngà) HandAnchor → đỉnh cần → phao.
    /// Cập nhật ở LateUpdate vì thứ tự Update không xác định (người chơi/phao dịch trong Update). Ẩn khi Idle.
    /// Con "RodVisual" của FishingController (EnsureVisuals tạo).
    /// </summary>
    public class FishingRodVisual : MonoBehaviour
    {
        private const float LineWidth = 0.01f;
        private const int OrderAbovePlayer = 10;
        private static readonly Color LineColor = new Color(1f, 0.97f, 0.88f, 0.95f);   // trắng ngà
        private static readonly Color RodColor = new Color(0.45f, 0.28f, 0.14f, 1f);    // nâu gỗ

        [Tooltip("Sprite cần vẽ tay (pivot nên ở gốc cần, dựng thẳng đứng); trống = thanh nâu thủ tục.")]
        [SerializeField] private Sprite rodSprite;
        [Tooltip("Chiều dài cần (unit) — nhân vật cao ~0.6 nên cần ~0.5.")]
        [SerializeField] private float rodLength = 0.5f;
        [Tooltip("Để trống = tự chọn layer theo TouristSortingLayers.Visitor.")]
        [SerializeField] private string sortingLayerName;

        private LineRenderer _line;
        private SpriteRenderer _rodSr;
        private Transform _rodTf;
        private FishingBobber _bobber;
        private bool _visible;
        private readonly Vector3[] _points = new Vector3[3];

        private void Awake()
        {
            Build();
            SetVisible(false);
        }

        /// <summary>Controller gọi để dây nối tới phao.</summary>
        public void Bind(FishingBobber bobber) { _bobber = bobber; }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            Build();
            _rodSr.enabled = visible;
            _line.enabled = visible;
        }

        private void LateUpdate()
        {
            if (!_visible) { return; }
            FishingPlayerController p = FishingPlayerController.Local;
            if (p == null) { return; }

            Vector3 hand = p.HandAnchor != null ? p.HandAnchor.position : (Vector3)p.Position;
            Vector2 dir = RodDirection(p.Facing);
            Vector3 tip = hand + (Vector3)(dir * rodLength);

            _rodTf.position = hand;
            _rodTf.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);   // sprite dựng đứng ⇒ trừ 90°
            ApplySorting();

            _points[0] = hand;
            _points[1] = tip;
            bool hasBobber = _bobber != null && _bobber.gameObject.activeInHierarchy;
            _points[2] = hasBobber ? _bobber.transform.position : tip;
            int count = hasBobber ? 3 : 2;
            if (_line.positionCount != count) { _line.positionCount = count; }
            for (int i = 0; i < count; i++) { _line.SetPosition(i, _points[i]); }
        }

        /// <summary>Hướng thân cần theo hướng nhìn: người nhìn xuống → cần chĩa xuống-phải; lên → lên-phải; trái/phải → hếch nhẹ.</summary>
        private static Vector2 RodDirection(FacingDir facing)
        {
            switch (facing)
            {
                case FacingDir.Down: return new Vector2(0.7f, -0.7f);
                case FacingDir.Up: return new Vector2(0.7f, 0.7f);
                case FacingDir.Left: return new Vector2(-0.9f, 0.45f).normalized;
                default: return new Vector2(0.9f, 0.45f).normalized;
            }
        }

        private void Build()
        {
            if (_rodSr != null && _line != null) { return; }

            Transform rodT = transform.Find("Rod");
            GameObject rodGo = rodT != null ? rodT.gameObject : new GameObject("Rod");
            rodGo.transform.SetParent(transform, false);
            _rodTf = rodGo.transform;
            _rodSr = rodGo.GetComponent<SpriteRenderer>();
            if (_rodSr == null) { _rodSr = rodGo.AddComponent<SpriteRenderer>(); }
            Sprite s = rodSprite != null ? rodSprite : RodProceduralSprite();
            _rodSr.sprite = s;
            _rodSr.color = rodSprite != null ? Color.white : RodColor;
            // Scale để chiều cao sprite = rodLength, bất kể PPU của sprite gán tay.
            float h = s != null && s.bounds.size.y > 0.0001f ? s.bounds.size.y : 1f;
            _rodTf.localScale = Vector3.one * (rodLength / h);

            _line = GetComponent<LineRenderer>();
            if (_line == null) { _line = gameObject.AddComponent<LineRenderer>(); }
            _line.useWorldSpace = true;
            _line.startWidth = LineWidth;
            _line.endWidth = LineWidth;
            _line.startColor = LineColor;
            _line.endColor = LineColor;
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _line.textureMode = LineTextureMode.Stretch;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            Material mat = LineMaterial();
            if (mat != null) { _line.sharedMaterial = mat; }   // null → giữ material mặc định của LineRenderer (vẫn vẽ được)
            _line.positionCount = 2;
        }

        private void ApplySorting()
        {
            string layer = TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Visitor);
            int baseOrder = 0;
            FishingPlayerController p = FishingPlayerController.Local;
            if (p != null)
            {
                SpriteRenderer psr = p.GetComponentInChildren<SpriteRenderer>();
                if (psr != null) { baseOrder = psr.sortingOrder; }
            }
            _rodSr.sortingLayerName = layer;
            _rodSr.sortingOrder = baseOrder + OrderAbovePlayer;
            _line.sortingLayerName = layer;
            _line.sortingOrder = baseOrder + OrderAbovePlayer + 1;
        }

        // ── Tài nguyên cache static ──
        private static Material _lineMat;
        private static Sprite _sprRod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _lineMat = null; _sprRod = null; }

        private static Material LineMaterial()
        {
            if (_lineMat != null) { return _lineMat; }
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) { sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"); }
            if (sh == null) { return null; }   // không có shader nào → bên gọi dùng material mặc định
            _lineMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            return _lineMat;
        }

        /// <summary>Thanh 4x60 px trắng (tint nâu qua color), pivot đáy giữa, cao 1 unit.</summary>
        private static Sprite RodProceduralSprite()
        {
            if (_sprRod != null && _sprRod.texture != null) { return _sprRod; }
            const int w = 4, h = 60;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Cột giữa đặc, 2 cột mép mờ; đầu cần (trên) sáng hơn gốc một chút.
                    float a = (x == 0 || x == w - 1) ? 0.6f : 1f;
                    float v = Mathf.Lerp(0.9f, 1f, y / (float)h);
                    cols[y * w + x] = new Color(v, v, v, a);
                }
            }
            tex.SetPixels(cols);
            tex.Apply(false, false);
            _sprRod = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h, 0, SpriteMeshType.FullRect);
            _sprRod.hideFlags = HideFlags.HideAndDontSave;
            return _sprRod;
        }
    }
}
