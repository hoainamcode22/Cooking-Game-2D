using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Hình cần câu + dây, vẽ bằng code:
    /// · Thân cần = LineRenderer riêng (_rodLine, 8 điểm) từ HandAnchor vươn lên chéo về bên quăng (Left/Right → nghiêng bên đó;
    ///   Down → lệch phải; Up → lệch trái), dài rodLength, thon dần 0.045 → 0.012 (widthCurve), gradient gỗ nâu ấm, 20% đầu (tay cầm) đậm hơn.
    /// · Cuộn dây = sprite tròn đồng vàng #D9A441, đường kính 0.07, treo dưới cần tại 18% chiều dài.
    /// · Đầu cần = RodTip (public) — dây câu bắt đầu từ đó, không từ tay.
    /// · Uốn theo pha (SetPhase): Casting whip ngả trước 25° rồi hồi · Waiting thẳng, rung ±1° · Bite đầu cần cúi về phao 20° + rung · Reeling uốn nhẹ.
    /// · Dây RodTip → phao = bezier 8 điểm, võng 0.12 khi Waiting, căng khi Bite/Reeling (LineRenderer trắng ngà width 0.01).
    /// · Sếp gán rodSprite → dùng SpriteRenderer xoay theo góc cần thay cho LineRenderer thân (dây vẫn LineRenderer, cuộn ẩn).
    /// Sorting: layer ResolveOrOverride("Objects", Visitor), order = order người chơi + 5 (cuộn/dây +6).
    /// Cập nhật ở LateUpdate vì thứ tự Update không xác định (người chơi/phao dịch trong Update). Ẩn khi Idle.
    /// Con "RodVisual" của FishingController (EnsureVisuals tạo).
    /// </summary>
    public class FishingRodVisual : MonoBehaviour
    {
        // Số thuần hình ảnh.
        private const int RodPoints = 8;
        private const int LinePoints = 8;
        private const float LineWidth = 0.01f;
        private const float RodWidthBase = 0.045f;
        private const float RodWidthTip = 0.012f;
        private const float HandleFraction = 0.2f;
        private const float ReelFraction = 0.18f;
        private const float ReelDiameter = 0.07f;
        private const float ReelHang = 0.03f;          // cuộn treo dưới thân cần
        private const float CastWhipDeg = 25f;
        private const float CastWhipSeconds = 0.5f;
        private const float WaitWobbleDeg = 1f;
        private const float BiteBendDeg = 20f;
        private const float BiteShakeDeg = 2.5f;
        private const float ReelBendDeg = 10f;
        private const float LineSagWaiting = 0.12f;
        private const float LineSagTaut = 0.015f;
        private const float LineSagFlying = 0.04f;
        private const int OrderAbovePlayer = 5;
        private const string DefaultLayer = "Objects";

        private static readonly Color LineColor = new Color(1f, 0.97f, 0.88f, 0.95f);      // trắng ngà
        private static readonly Color WoodHandle = new Color(0.38f, 0.24f, 0.13f, 1f);     // tay cầm đậm
        private static readonly Color WoodBase = new Color(0.55f, 0.36f, 0.2f, 1f);        // gỗ nâu ấm
        private static readonly Color WoodTip = new Color(0.74f, 0.53f, 0.32f, 1f);        // đầu cần sáng hơn
        private static readonly Color ReelColor = new Color(0.851f, 0.643f, 0.255f, 1f);   // #D9A441 đồng vàng

        [Tooltip("Sprite cần vẽ tay (pivot nên ở gốc cần, dựng thẳng đứng); trống = thân cần LineRenderer thủ tục.")]
        [SerializeField] private Sprite rodSprite;
        [Tooltip("Chiều dài cần (unit) — nhân vật cao ~0.6 nên cần ~0.55.")]
        [SerializeField] private float rodLength = 0.55f;
        [Tooltip("Để trống = 'Objects' (fallback theo TouristSortingLayers.Visitor).")]
        [SerializeField] private string sortingLayerName;

        private LineRenderer _line;
        private LineRenderer _rodLine;
        private SpriteRenderer _rodSr;
        private Transform _rodTf;
        private SpriteRenderer _reelSr;
        private FishingBobber _bobber;
        private FishingPhase _phase = FishingPhase.Idle;
        private float _phaseTime;
        private float _sag = LineSagWaiting;
        private bool _visible;
        private SpriteRenderer _playerSr;
        private FishingPlayerController _playerOfSr;
        private readonly Vector3[] _rodPts = new Vector3[RodPoints];
        private readonly Vector3[] _linePts = new Vector3[LinePoints];

        /// <summary>Đầu cần (world) sau khi uốn — dây câu bắt đầu từ đây. Cập nhật mỗi LateUpdate khi hiện.</summary>
        public Vector3 RodTip { get; private set; }

        private void Awake()
        {
            Build();
            SetVisible(false);
        }

        /// <summary>Controller gọi để dây nối tới phao.</summary>
        public void Bind(FishingBobber bobber) { _bobber = bobber; }

        /// <summary>Controller báo pha mới để cần đổi kiểu uốn (whip / thẳng / cúi / thu). Đổi pha → đồng hồ pha về 0.</summary>
        public void SetPhase(FishingPhase phase)
        {
            if (phase == _phase) { return; }
            _phase = phase;
            _phaseTime = 0f;
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            Build();
            bool useSprite = rodSprite != null;
            _rodSr.enabled = visible && useSprite;
            _rodLine.enabled = visible && !useSprite;
            _reelSr.enabled = visible && !useSprite;
            _line.enabled = visible;
            if (!visible) { _line.positionCount = 0; }
        }

        private void LateUpdate()
        {
            if (!_visible) { return; }
            FishingPlayerController p = FishingPlayerController.Local;
            if (p == null) { return; }
            _phaseTime += Time.deltaTime;

            Vector3 hand = p.HandAnchor != null ? p.HandAnchor.position : (Vector3)p.Position;
            Vector2 dir = RodDirection(p.Facing);
            Vector2 forward = ForwardVector(p.Facing);
            bool hasBobber = _bobber != null && _bobber.gameObject.activeInHierarchy;
            Vector3 bobberPos = hasBobber ? _bobber.transform.position : hand + (Vector3)(dir * rodLength);

            // Hướng uốn: về phía phao (đang câu) hoặc về bên quăng (Casting chưa có phao ổn định).
            Vector2 straightTip = (Vector2)hand + dir * rodLength;
            Vector2 toward = hasBobber && _phase != FishingPhase.Casting ? (Vector2)bobberPos - straightTip : forward;
            float bendSign = Mathf.Sign(dir.x * toward.y - dir.y * toward.x);   // Mathf.Sign(0) = 1 → không bao giờ 0

            float tilt, bend;
            PoseForPhase(out tilt, out bend);
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + tilt * bendSign;
            float bendDeg = bend * bendSign;

            BuildRodCurve(hand, baseAngle, bendDeg);
            RodTip = _rodPts[RodPoints - 1];
            ApplySorting(p);

            if (rodSprite != null)
            {
                _rodTf.position = hand;
                // Sprite cứng: xoay theo góc gốc + nửa độ uốn để đầu cần xấp xỉ RodTip.
                _rodTf.rotation = Quaternion.Euler(0f, 0f, baseAngle + bendDeg * 0.5f - 90f);   // sprite dựng đứng ⇒ trừ 90°
                Vector2 tipDir = new Vector2(Mathf.Cos((baseAngle + bendDeg * 0.5f) * Mathf.Deg2Rad), Mathf.Sin((baseAngle + bendDeg * 0.5f) * Mathf.Deg2Rad));
                RodTip = hand + (Vector3)(tipDir * rodLength);
            }
            else
            {
                _rodLine.SetPositions(_rodPts);
                PlaceReel(dir);
            }

            UpdateLine(hasBobber, bobberPos);
        }

        /// <summary>Góc nghiêng cả cần (tilt) và độ cúi đầu cần (bend), độ, theo pha + thời gian trong pha. Dấu dương = về hướng uốn.</summary>
        private void PoseForPhase(out float tilt, out float bend)
        {
            tilt = 0f;
            bend = 0f;
            switch (_phase)
            {
                case FishingPhase.Casting:
                {
                    // Whip: ngả ra trước tới 25° trong nửa đầu CastWhipSeconds rồi hồi, sau đó rung tắt dần.
                    if (_phaseTime < CastWhipSeconds)
                    {
                        float u = _phaseTime / CastWhipSeconds;
                        bend = CastWhipDeg * Mathf.Sin(Mathf.PI * u);
                        tilt = CastWhipDeg * 0.35f * Mathf.Sin(Mathf.PI * u);
                    }
                    else
                    {
                        float t = _phaseTime - CastWhipSeconds;
                        bend = 6f * Mathf.Sin(t * 18f) * Mathf.Exp(-t * 4f);
                    }
                    break;
                }
                case FishingPhase.Waiting:
                    tilt = Mathf.Sin(_phaseTime * 2.4f) * WaitWobbleDeg;
                    break;
                case FishingPhase.Bite:
                    bend = BiteBendDeg * Mathf.Clamp01(_phaseTime / 0.12f) + Mathf.Sin(_phaseTime * 40f) * BiteShakeDeg;
                    tilt = Mathf.Sin(_phaseTime * 31f) * BiteShakeDeg * 0.5f;
                    break;
                case FishingPhase.Reeling:
                    bend = ReelBendDeg + Mathf.Sin(_phaseTime * 14f) * 1.5f;
                    break;
                default:
                    // Result/Idle: về thẳng.
                    break;
            }
        }

        /// <summary>
        /// Điểm thân cần: đi từ tay theo từng đoạn rodLength/(N-1), góc mỗi đoạn = baseAngle + bendDeg × t² (gốc cứng, đầu mềm)
        /// ⇒ 3 điểm cuối cúi nhiều nhất, tương đương bezier hướng phao mà không cần điểm điều khiển.
        /// </summary>
        private void BuildRodCurve(Vector3 hand, float baseAngleDeg, float bendDeg)
        {
            float seg = rodLength / (RodPoints - 1);
            Vector3 pos = hand;
            _rodPts[0] = pos;
            for (int i = 1; i < RodPoints; i++)
            {
                float t = i / (float)(RodPoints - 1);
                float a = (baseAngleDeg + bendDeg * t * t) * Mathf.Deg2Rad;
                pos += new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * seg;
                _rodPts[i] = pos;
            }
        }

        /// <summary>Cuộn dây treo dưới thân cần ở ReelFraction chiều dài (nội suy trên đường cong).</summary>
        private void PlaceReel(Vector2 dir)
        {
            float f = ReelFraction * (RodPoints - 1);
            int i0 = Mathf.Clamp(Mathf.FloorToInt(f), 0, RodPoints - 2);
            Vector3 on = Vector3.Lerp(_rodPts[i0], _rodPts[i0 + 1], f - i0);
            // Vuông góc với cần, chọn phía hướng xuống (cuộn treo dưới).
            Vector2 perp = new Vector2(-dir.y, dir.x);
            if (perp.y > 0f) { perp = -perp; }
            _reelSr.transform.position = on + (Vector3)(perp * ReelHang);
        }

        /// <summary>Dây RodTip → phao: bezier bậc 2, điểm điều khiển = trung điểm hạ xuống sag (võng khi chờ, căng khi cắn/thu).</summary>
        private void UpdateLine(bool hasBobber, Vector3 bobberPos)
        {
            if (!hasBobber)
            {
                if (_line.positionCount != 0) { _line.positionCount = 0; }
                return;
            }

            float targetSag;
            switch (_phase)
            {
                case FishingPhase.Waiting: targetSag = LineSagWaiting; break;
                case FishingPhase.Bite:
                case FishingPhase.Reeling: targetSag = LineSagTaut; break;
                default: targetSag = LineSagFlying; break;
            }
            // Đổi độ võng mượt (căng nhanh hơn chùng).
            float rate = targetSag < _sag ? 14f : 5f;
            _sag = Mathf.Lerp(_sag, targetSag, 1f - Mathf.Exp(-rate * Time.deltaTime));

            Vector3 a = RodTip;
            Vector3 c = bobberPos;
            Vector3 b = (a + c) * 0.5f + Vector3.down * _sag;
            for (int i = 0; i < LinePoints; i++)
            {
                float t = i / (float)(LinePoints - 1);
                float mt = 1f - t;
                _linePts[i] = mt * mt * a + 2f * mt * t * b + t * t * c;
            }
            if (_line.positionCount != LinePoints) { _line.positionCount = LinePoints; }
            _line.SetPositions(_linePts);
        }

        /// <summary>Hướng thân cần: vươn lên chéo về bên quăng. Left/Right → nghiêng bên đó; Down → lệch phải; Up → lệch trái.</summary>
        private static Vector2 RodDirection(FacingDir facing)
        {
            switch (facing)
            {
                case FacingDir.Left: return new Vector2(-0.72f, 0.7f).normalized;
                case FacingDir.Right: return new Vector2(0.72f, 0.7f).normalized;
                case FacingDir.Up: return new Vector2(-0.5f, 0.87f).normalized;
                default: return new Vector2(0.5f, 0.87f).normalized;
            }
        }

        /// <summary>Bên quăng (ngang) theo hướng nhìn — dùng làm hướng whip khi chưa có phao.</summary>
        private static Vector2 ForwardVector(FacingDir facing)
        {
            switch (facing)
            {
                case FacingDir.Left: return Vector2.left;
                case FacingDir.Right: return Vector2.right;
                case FacingDir.Up: return new Vector2(-1f, 0.3f);
                default: return new Vector2(1f, -0.3f);
            }
        }

        private void Build()
        {
            if (_rodSr != null && _line != null && _rodLine != null && _reelSr != null) { return; }

            // Sprite cần (chỉ dùng khi Sếp gán rodSprite).
            Transform rodT = transform.Find("Rod");
            GameObject rodGo = rodT != null ? rodT.gameObject : new GameObject("Rod");
            rodGo.transform.SetParent(transform, false);
            _rodTf = rodGo.transform;
            _rodSr = rodGo.GetComponent<SpriteRenderer>();
            if (_rodSr == null) { _rodSr = rodGo.AddComponent<SpriteRenderer>(); }
            _rodSr.sprite = rodSprite;
            _rodSr.color = Color.white;
            if (rodSprite != null)
            {
                // Scale để chiều cao sprite = rodLength, bất kể PPU của sprite gán tay.
                float h = rodSprite.bounds.size.y > 0.0001f ? rodSprite.bounds.size.y : 1f;
                _rodTf.localScale = Vector3.one * (rodLength / h);
            }
            _rodSr.enabled = false;

            // Thân cần LineRenderer.
            Transform bodyT = transform.Find("RodBody");
            GameObject bodyGo = bodyT != null ? bodyT.gameObject : new GameObject("RodBody");
            bodyGo.transform.SetParent(transform, false);
            _rodLine = bodyGo.GetComponent<LineRenderer>();
            if (_rodLine == null) { _rodLine = bodyGo.AddComponent<LineRenderer>(); }
            SetupLine(_rodLine);
            _rodLine.widthMultiplier = 1f;
            _rodLine.widthCurve = AnimationCurve.Linear(0f, RodWidthBase, 1f, RodWidthTip);   // thon đều gốc → đầu
            _rodLine.colorGradient = RodGradient();
            _rodLine.numCapVertices = 4;
            _rodLine.numCornerVertices = 4;
            _rodLine.positionCount = RodPoints;
            _rodLine.enabled = false;

            // Cuộn dây.
            Transform reelT = transform.Find("Reel");
            GameObject reelGo = reelT != null ? reelT.gameObject : new GameObject("Reel");
            reelGo.transform.SetParent(transform, false);
            _reelSr = reelGo.GetComponent<SpriteRenderer>();
            if (_reelSr == null) { _reelSr = reelGo.AddComponent<SpriteRenderer>(); }
            _reelSr.sprite = ReelSprite();
            _reelSr.color = Color.white;
            reelGo.transform.localScale = Vector3.one * ReelDiameter;   // sprite cao 1 unit ⇒ scale = đường kính
            _reelSr.enabled = false;

            // Dây câu.
            _line = GetComponent<LineRenderer>();
            if (_line == null) { _line = gameObject.AddComponent<LineRenderer>(); }
            SetupLine(_line);
            _line.startWidth = LineWidth;
            _line.endWidth = LineWidth;
            _line.startColor = LineColor;
            _line.endColor = LineColor;
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _line.positionCount = 0;
        }

        private static void SetupLine(LineRenderer lr)
        {
            lr.useWorldSpace = true;
            lr.textureMode = LineTextureMode.Stretch;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            Material mat = LineMaterial();
            if (mat != null) { lr.sharedMaterial = mat; }   // null → giữ material mặc định của LineRenderer (vẫn vẽ được)
        }

        /// <summary>Gradient gỗ: 0..20% tay cầm đậm (đổi màu gắt tại 20%), rồi nâu ấm → đầu cần sáng.</summary>
        private static Gradient RodGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(WoodHandle, 0f),
                    new GradientColorKey(WoodHandle, HandleFraction - 0.005f),
                    new GradientColorKey(WoodBase, HandleFraction + 0.005f),
                    new GradientColorKey(WoodTip, 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private void ApplySorting(FishingPlayerController p)
        {
            string layer = TouristSortingLayers.ResolveOrOverride(string.IsNullOrEmpty(sortingLayerName) ? DefaultLayer : sortingLayerName, TouristSortingLayers.Visitor);
            int baseOrder = 0;
            if (p != null)
            {
                // Cache SpriteRenderer người chơi (FishingYSort đổi order mỗi frame nên vẫn đọc order mỗi lần).
                if (_playerSr == null || _playerOfSr != p) { _playerSr = p.GetComponentInChildren<SpriteRenderer>(); _playerOfSr = p; }
                if (_playerSr != null) { baseOrder = _playerSr.sortingOrder; }
            }
            _rodSr.sortingLayerName = layer;
            _rodSr.sortingOrder = baseOrder + OrderAbovePlayer;
            _rodLine.sortingLayerName = layer;
            _rodLine.sortingOrder = baseOrder + OrderAbovePlayer;
            _reelSr.sortingLayerName = layer;
            _reelSr.sortingOrder = baseOrder + OrderAbovePlayer + 1;
            _line.sortingLayerName = layer;
            _line.sortingOrder = baseOrder + OrderAbovePlayer + 1;
        }

        // ── Tài nguyên cache static ──
        private static Material _lineMat;
        private static Sprite _sprReel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _lineMat = null; _sprReel = null; }

        private static Material LineMaterial()
        {
            if (_lineMat != null) { return _lineMat; }
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) { sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"); }
            if (sh == null) { return null; }   // không có shader nào → bên gọi dùng material mặc định
            _lineMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            return _lineMat;
        }

        /// <summary>Cuộn dây: đĩa tròn đồng vàng #D9A441, vành tối, lỗ trục nhỏ giữa, highlight. Cao 1 unit ⇒ scale = đường kính.</summary>
        private static Sprite ReelSprite()
        {
            if (_sprReel != null && _sprReel.texture != null) { return _sprReel; }
            _sprReel = FishingSplashFX.Bake(32, 32, (x, y) =>
            {
                float dx = (x + 0.5f - 16f) / 15f;
                float dy = (y + 0.5f - 16f) / 15f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1f) { return Color.clear; }
                float edge = Mathf.Clamp01((1f - r) * 8f);
                Color body = ReelColor;
                if (r > 0.82f) { body = Color.Lerp(body, new Color(0.35f, 0.24f, 0.08f), 0.7f); }   // vành tối
                if (r < 0.18f) { body = Color.Lerp(body, new Color(0.3f, 0.2f, 0.08f), 0.8f); }     // lỗ trục
                float hx = (dx + 0.35f) / 0.3f, hy = (dy - 0.35f) / 0.3f;
                if (hx * hx + hy * hy < 1f) { body = Color.Lerp(body, Color.white, 0.45f); }        // highlight
                body.a = edge;
                return body;
            });
            return _sprReel;
        }
    }
}
