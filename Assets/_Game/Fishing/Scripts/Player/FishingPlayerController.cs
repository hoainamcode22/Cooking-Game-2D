using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Nhân vật người chơi ở hồ câu: đi 4 hướng (joystick ảo hoặc WASD để test Editor), Rigidbody2D MovePosition,
    /// đẩy DirX/DirY/IsMoving/IsFishing sang Animator, tạo HeadAnchor/HandAnchor cho bubble/line/dây câu.
    /// Nếu nằm dưới RemotePlayerView (prefab dùng chung cho người khác) thì tự tắt để không giành Local.
    /// </summary>
    public class FishingPlayerController : MonoBehaviour
    {
        public const string HeadAnchorName = "HeadAnchor";
        public const string HandAnchorName = "HandAnchor";

        [Tooltip("Để trống = tự lấy Animator ở chính object/đời con.")]
        [SerializeField] private Animator animator;
        [Tooltip("Để trống = tự lấy SpriteRenderer ở chính object/đời con.")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Sorting layer mong muốn; rỗng hoặc không tồn tại thì rơi về TouristSortingLayers.Visitor.")]
        [SerializeField] private string sortingLayerName = "";
        [SerializeField] private int baseSortingOrder = 0;

        public static FishingPlayerController Local { get; private set; }
        public FacingDir Facing { get; private set; } = FacingDir.Down;
        public bool IsMoving { get; private set; }
        public Vector2 Position { get { return transform.position; } }
        public Transform HeadAnchor { get; private set; }
        public Transform HandAnchor { get; private set; }
        public event Action<FacingDir> OnFacingChanged;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _footCollider;
        private bool _ownCollider;          // collider do code tạo → được phép chỉnh size theo scale
        private FishingConfig _cfg;
        private Vector2 _move;              // hướng đi frame này (đã khoá 4 hướng, độ dài 0/1)
        private bool _locked;
        private FishingPhase _phase = FishingPhase.Idle;
        private bool _isPuppet;

        // Animator: chỉ Set param nào controller thật sự có (tránh warning ngập Console khi clip IsFishing chưa làm).
        private readonly HashSet<int> _animParams = new HashSet<int>();
        private bool _animParamsCached;
        private bool _animWarned;
        private static readonly int HashDirX = Animator.StringToHash(FishingIds.AnimParamDirX);
        private static readonly int HashDirY = Animator.StringToHash(FishingIds.AnimParamDirY);
        private static readonly int HashIsMoving = Animator.StringToHash(FishingIds.AnimParamIsMoving);
        private static readonly int HashIsFishing = Animator.StringToHash(FishingIds.AnimParamIsFishing);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Local = null; }

        private void Awake()
        {
            // Prefab nhân vật dùng chung cho người chơi khác → bản đó là "con rối", không điều khiển, không giành Local.
            _isPuppet = GetComponentInParent<RemotePlayerView>() != null;
            if (_isPuppet) { enabled = false; return; }

            Local = this;
            _cfg = FishingDatabase.ConfigOrDefault;
            if (animator == null) { animator = GetComponentInChildren<Animator>(); }
            if (spriteRenderer == null) { spriteRenderer = GetComponentInChildren<SpriteRenderer>(); }

            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) { _rb = gameObject.AddComponent<Rigidbody2D>(); }
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _footCollider = GetComponent<CapsuleCollider2D>();
            if (_footCollider == null)
            {
                _footCollider = gameObject.AddComponent<CapsuleCollider2D>();
                _footCollider.direction = CapsuleDirection2D.Horizontal;
                _ownCollider = true;
            }
        }

        private void OnDestroy()
        {
            if (Local == this) { Local = null; }
        }

        /// <summary>Bootstrap gọi ngay sau Instantiate: scale theo chiều cao config, tạo anchor, áp sorting layer.</summary>
        public void Initialize(FishingCharacterDef def, FishingConfig cfg)
        {
            if (_isPuppet) { return; }
            if (cfg != null) { _cfg = cfg; }
            if (_cfg == null) { _cfg = FishingDatabase.ConfigOrDefault; }
            if (animator == null) { animator = GetComponentInChildren<Animator>(); }
            if (spriteRenderer == null) { spriteRenderer = GetComponentInChildren<SpriteRenderer>(); }

            float worldHeight = Mathf.Max(0.1f, _cfg.playerWorldHeight);
            ApplyScale(worldHeight);
            float s = Mathf.Max(0.0001f, transform.lossyScale.y);

            // Anchor con: HeadAnchor = đỉnh đầu + 0.05 (bubble/line/tên), HandAnchor ≈ 55% chiều cao (Dev B nối dây câu).
            HeadAnchor = EnsureChild(HeadAnchorName, new Vector3(0f, (worldHeight + 0.05f) / s, 0f));
            HandAnchor = EnsureChild(HandAnchorName, new Vector3(0f, worldHeight * 0.55f / s, 0f));

            // Collider nhỏ ở chân (pivot bottom-center nên chân ở local y≈0). Chỉ chỉnh collider do code tạo.
            if (_footCollider != null && _ownCollider)
            {
                _footCollider.size = new Vector2(worldHeight * 0.45f / s, worldHeight * 0.22f / s);
                _footCollider.offset = new Vector2(0f, worldHeight * 0.11f / s);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Visitor);
                var ySort = GetComponent<FishingYSort>();
                if (ySort == null) { ySort = gameObject.AddComponent<FishingYSort>(); }
                ySort.Configure(spriteRenderer, baseSortingOrder);
            }
            else
            {
                Debug.LogWarning(FishingIds.LogTag + " Player '" + name + "' không có SpriteRenderer — chạy Tools/Farm Game/Hồ Câu/★ SETUP để tạo prefab nhân vật.");
            }

            PushFacingToAnimator();
            SetAnimBool(HashIsMoving, false);
            SetAnimBool(HashIsFishing, false);
        }

        /// <summary>Dev B khoá khi đang câu (Casting..Result): dừng ngay, không nhận input.</summary>
        public void SetMovementLocked(bool locked)
        {
            _locked = locked;
            if (!locked) { return; }
            _move = Vector2.zero;
            IsMoving = false;
            if (_rb != null) { _rb.linearVelocity = Vector2.zero; }
            SetAnimBool(HashIsMoving, false);
        }

        /// <summary>Dev B báo mỗi lần đổi pha → Animator bool IsFishing (chưa có clip thì chỉ dừng walk).</summary>
        public void SetFishingPhase(FishingPhase phase)
        {
            _phase = phase;
            bool fishing = phase != FishingPhase.Idle;
            SetAnimBool(HashIsFishing, fishing);
            if (fishing)
            {
                _move = Vector2.zero;
                IsMoving = false;
                SetAnimBool(HashIsMoving, false);
            }
        }

        private void Update()
        {
            if (_locked || _phase != FishingPhase.Idle)
            {
                _move = Vector2.zero;
                IsMoving = false;
                return;
            }

            Vector2 raw = ReadInput();

            // KHOÁ 4 HƯỚNG: trục mạnh thắng, bỏ trục còn lại → không đi chéo (khớp sprite 4 hướng).
            if (Mathf.Abs(raw.x) >= Mathf.Abs(raw.y)) { raw.y = 0f; } else { raw.x = 0f; }

            bool moving = raw.sqrMagnitude > 0.0001f;
            // Độ dài luôn 0/1: tốc độ cố định cfg.moveSpeed (Mathf.Sign(0) trả 1 nên phải rẽ nhánh theo trục còn lại).
            _move = Vector2.zero;
            if (moving) { _move = raw.x != 0f ? new Vector2(Mathf.Sign(raw.x), 0f) : new Vector2(0f, Mathf.Sign(raw.y)); }
            IsMoving = moving;

            if (moving)
            {
                FacingDir f = _move.x > 0f ? FacingDir.Right : _move.x < 0f ? FacingDir.Left : _move.y > 0f ? FacingDir.Up : FacingDir.Down;
                if (f != Facing)
                {
                    Facing = f;
                    PushFacingToAnimator();
                    OnFacingChanged?.Invoke(Facing);
                }
            }
            SetAnimBool(HashIsMoving, moving);
        }

        private void FixedUpdate()
        {
            if (_rb == null) { return; }
            if (_move.sqrMagnitude < 0.0001f)
            {
                // Đứng yên: triệt vận tốc để không bị trôi sau va chạm.
                if (_rb.linearVelocity.sqrMagnitude > 0f) { _rb.linearVelocity = Vector2.zero; }
                return;
            }
            float speed = _cfg != null ? _cfg.moveSpeed : 1.6f;
            _rb.MovePosition(_rb.position + _move * speed * Time.fixedDeltaTime);
        }

        /// <summary>Joystick ảo khi đang chạm; không thì WASD/mũi tên (legacy Input, activeInputHandler = Both) để test Editor.</summary>
        private Vector2 ReadInput()
        {
            var js = VirtualJoystickUI.Instance;
            if (js != null && js.IsActive) { return js.Direction; }

            float x = 0f, y = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { x -= 1f; }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { x += 1f; }
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { y += 1f; }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { y -= 1f; }
            return new Vector2(x, y);
        }

        // ── Scale / anchor ──

        private void ApplyScale(float worldHeight)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) { return; }
            float spriteLocalH = spriteRenderer.sprite.bounds.size.y;
            if (spriteLocalH <= 0.0001f) { return; }
            // SpriteRenderer có thể nằm ở con có scale riêng → bù tỉ lệ con so với root.
            float childRel = transform.lossyScale.y > 0.0001f ? spriteRenderer.transform.lossyScale.y / transform.lossyScale.y : 1f;
            float parentScale = transform.parent != null && transform.parent.lossyScale.y > 0.0001f ? transform.parent.lossyScale.y : 1f;
            float s = worldHeight / (spriteLocalH * Mathf.Max(0.0001f, childRel) * parentScale);
            transform.localScale = new Vector3(s, s, 1f);
        }

        private Transform EnsureChild(string childName, Vector3 localPos)
        {
            Transform t = transform.Find(childName);
            if (t == null)
            {
                var go = new GameObject(childName);
                t = go.transform;
                t.SetParent(transform, false);
                t.localPosition = localPos;
            }
            return t;
        }

        // ── Animator an toàn ──

        private bool AnimatorReady()
        {
            if (animator != null && animator.runtimeAnimatorController != null && animator.isInitialized)
            {
                if (!_animParamsCached)
                {
                    _animParamsCached = true;
                    var ps = animator.parameters;
                    for (int i = 0; i < ps.Length; i++) { _animParams.Add(ps[i].nameHash); }
                }
                return true;
            }
            if (!_animWarned)
            {
                _animWarned = true;
                Debug.LogWarning(FishingIds.LogTag + " Player '" + name + "' chưa có Animator/Controller hợp lệ — vẫn đi lại nhưng không có animation (chạy Tools/Farm Game/Hồ Câu/★ SETUP).");
            }
            return false;
        }

        private void SetAnimBool(int hash, bool value)
        {
            if (!AnimatorReady() || !_animParams.Contains(hash)) { return; }
            animator.SetBool(hash, value);
        }

        private void SetAnimFloat(int hash, float value)
        {
            if (!AnimatorReady() || !_animParams.Contains(hash)) { return; }
            animator.SetFloat(hash, value);
        }

        private void PushFacingToAnimator()
        {
            float x = 0f, y = 0f;
            switch (Facing)
            {
                case FacingDir.Left: x = -1f; break;
                case FacingDir.Right: x = 1f; break;
                case FacingDir.Up: y = 1f; break;
                default: y = -1f; break;
            }
            SetAnimFloat(HashDirX, x);
            SetAnimFloat(HashDirY, y);
        }
    }
}
