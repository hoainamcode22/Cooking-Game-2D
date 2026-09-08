using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Hình đại diện của MỘT người chơi khác trong phòng (con của RemotePlayers). Nội suy vị trí mượt về state,
    /// đẩy Animator, y-sort, collider để chạm, name tag, bubble chat. Prefab nhân vật (dùng chung với local) là con "Visual";
    /// FishingPlayerController trong prefab tự tắt khi thấy cha là RemotePlayerView.
    /// </summary>
    public class RemotePlayerView : MonoBehaviour
    {
        private const float SnapDistance = 10f;     // lệch quá xa (bot dịch chuyển/reset) thì nhảy thẳng thay vì đi xuyên map
        private const float SpeedMultiplier = 1.2f; // đuổi nhanh hơn tốc độ thật để không bị tụt lại

        public string PlayerId { get; private set; }
        public PlayerNetState State { get; private set; }
        public Transform HeadAnchor { get; private set; }
        /// <summary>Collider (trigger) để RemotePlayersManager kiểm OverlapPoint khi chạm.</summary>
        public Collider2D TapCollider { get; private set; }

        private FishingConfig _cfg;
        private Transform _visual;
        private SpriteRenderer _sr;
        private Animator _anim;
        private PlayerNameTag _tag;
        private Vector2 _target;
        private bool _hasFirstState;
        private string _lastBubbleText;
        private long _lastBubbleUntil;
        private string _lastName;
        private int _lastLevel = -1;

        private readonly HashSet<int> _animParams = new HashSet<int>();
        private bool _animParamsCached;
        private static readonly int HashDirX = Animator.StringToHash(FishingIds.AnimParamDirX);
        private static readonly int HashDirY = Animator.StringToHash(FishingIds.AnimParamDirY);
        private static readonly int HashIsMoving = Animator.StringToHash(FishingIds.AnimParamIsMoving);
        private static readonly int HashIsFishing = Animator.StringToHash(FishingIds.AnimParamIsFishing);

        /// <summary>
        /// RemotePlayersManager gọi 1 lần sau khi tạo. visual = bản Instantiate prefab nhân vật (đã là con của object này) hoặc null.
        /// </summary>
        public void Setup(PlayerNetState first, FishingConfig cfg, GameObject visual)
        {
            _cfg = cfg != null ? cfg : FishingDatabase.ConfigOrDefault;
            PlayerId = first != null ? first.playerId : name;
            State = first != null ? first.Clone() : new PlayerNetState { playerId = PlayerId };

            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.AddComponent<SpriteRenderer>();   // trống: vẫn có vị trí/name tag để test net khi chưa có prefab
            }
            _visual = visual.transform;
            _visual.SetParent(transform, false);
            _visual.localPosition = Vector3.zero;
            _visual.gameObject.name = "Visual";

            _sr = _visual.GetComponentInChildren<SpriteRenderer>();
            _anim = _visual.GetComponentInChildren<Animator>();
            DisableLocalOnlyParts(visual);

            float h = Mathf.Max(0.1f, _cfg.playerWorldHeight);
            float w = ApplyScale(h);

            HeadAnchor = EnsureChild(FishingPlayerController.HeadAnchorName, new Vector3(0f, h + 0.05f, 0f));

            // Collider chạm nằm trên object này (không có Rigidbody2D → static, OverlapPoint hoạt động), trigger để không đẩy người chơi.
            var box = GetComponent<BoxCollider2D>();
            if (box == null) { box = gameObject.AddComponent<BoxCollider2D>(); }
            box.isTrigger = true;
            box.size = new Vector2(Mathf.Max(0.3f, w), h);
            box.offset = new Vector2(0f, h * 0.5f);
            TapCollider = box;

            if (_sr != null)
            {
                _sr.sortingLayerName = TouristSortingLayers.Resolve(TouristSortingLayers.Visitor);
                var ySort = GetComponent<FishingYSort>();
                if (ySort == null) { ySort = gameObject.AddComponent<FishingYSort>(); }
                ySort.Configure(_sr, 0);
            }

            _tag = PlayerNameTag.Create(transform, HeadAnchor, State.displayName, State.level);
            _lastName = State.displayName; _lastLevel = State.level;

            _target = new Vector2(State.x, State.y);
            transform.position = new Vector3(State.x, State.y, 0f);
            _hasFirstState = true;
            PushAnimator(false);
            CheckBubble();
        }

        /// <summary>Nhận state mới từ dịch vụ phòng (bản copy, không giữ tham chiếu).</summary>
        public void ApplyState(PlayerNetState s)
        {
            if (s == null) { return; }
            State.CopyFrom(s);
            _target = new Vector2(s.x, s.y);
            if (!_hasFirstState)
            {
                transform.position = new Vector3(s.x, s.y, 0f);   // lần đầu: teleport
                _hasFirstState = true;
            }
            if (_tag != null && (s.displayName != _lastName || s.level != _lastLevel))
            {
                _lastName = s.displayName; _lastLevel = s.level;
                _tag.SetInfo(s.displayName, s.level);
            }
            CheckBubble();
        }

        private void Update()
        {
            if (!_hasFirstState) { return; }
            Vector2 pos = transform.position;
            float dist = Vector2.Distance(pos, _target);
            bool movedThisFrame = false;
            if (dist > SnapDistance)
            {
                transform.position = new Vector3(_target.x, _target.y, 0f);
            }
            else if (dist > 0.0005f)
            {
                float speed = _cfg.moveSpeed * SpeedMultiplier;
                Vector2 np = Vector2.MoveTowards(pos, _target, speed * Time.deltaTime);
                transform.position = new Vector3(np.x, np.y, 0f);
                movedThisFrame = true;
            }
            PushAnimator(movedThisFrame);
        }

        // ── Bubble ──

        private void CheckBubble()
        {
            if (string.IsNullOrEmpty(State.bubbleText))
            {
                _lastBubbleText = null; _lastBubbleUntil = 0;
                return;
            }
            // Cùng text nhưng bubbleUntilUnix mới = gửi lại câu cũ → vẫn phải hiện lại.
            if (State.bubbleText == _lastBubbleText && State.bubbleUntilUnix == _lastBubbleUntil) { return; }
            _lastBubbleText = State.bubbleText; _lastBubbleUntil = State.bubbleUntilUnix;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long left = State.bubbleUntilUnix - now;
            if (left <= 0) { return; }

            var friends = FishingNetHub.Friends;
            bool isFriend = friends != null && friends.IsFriend(PlayerId);
            if (State.privateMode && !isFriend) { return; }

            var local = FishingPlayerController.Local;
            if (local == null || HeadAnchor == null) { return; }
            // Người lạ ở xa thì không thấy (theo tooltip FishingConfig.bubbleVisibleDistance: bạn bè thấy bất kể khoảng cách).
            float dist = Vector2.Distance(local.Position, transform.position);
            if (dist > _cfg.bubbleVisibleDistance && !isFriend) { return; }

            ChatBubbleUI.Show(HeadAnchor, State.bubbleText, (float)left, false);
        }

        // ── Dựng hình ──

        /// <summary>Scale Visual sao cho chiều cao sprite = h. Trả về chiều rộng world của sprite (để làm collider).</summary>
        private float ApplyScale(float h)
        {
            if (_sr == null || _sr.sprite == null) { return h * 0.6f; }
            Vector2 size = _sr.sprite.bounds.size;
            if (size.y <= 0.0001f) { return h * 0.6f; }
            float childRel = _visual.lossyScale.y > 0.0001f ? _sr.transform.lossyScale.y / _visual.lossyScale.y : 1f;
            float s = h / (size.y * Mathf.Max(0.0001f, childRel));
            _visual.localScale = new Vector3(s, s, 1f);
            return size.x * s * childRel;
        }

        /// <summary>Prefab dùng chung với local: tắt điều khiển + vật lý để con rối không nhận input, không đẩy ai.</summary>
        private static void DisableLocalOnlyParts(GameObject visual)
        {
            var ctrls = visual.GetComponentsInChildren<FishingPlayerController>(true);
            for (int i = 0; i < ctrls.Length; i++) { ctrls[i].enabled = false; }
            var bodies = visual.GetComponentsInChildren<Rigidbody2D>(true);
            for (int i = 0; i < bodies.Length; i++) { bodies[i].simulated = false; }
            var cols = visual.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols.Length; i++) { cols[i].enabled = false; }
            var sorts = visual.GetComponentsInChildren<FishingYSort>(true);
            for (int i = 0; i < sorts.Length; i++) { sorts[i].enabled = false; }   // y-sort do object cha đảm nhiệm
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
            if (_anim == null || _anim.runtimeAnimatorController == null || !_anim.isInitialized) { return false; }
            if (!_animParamsCached)
            {
                _animParamsCached = true;
                var ps = _anim.parameters;
                for (int i = 0; i < ps.Length; i++) { _animParams.Add(ps[i].nameHash); }
            }
            return true;
        }

        private void PushAnimator(bool movedThisFrame)
        {
            if (!AnimatorReady()) { return; }
            float x = 0f, y = 0f;
            switch ((FacingDir)State.dir)
            {
                case FacingDir.Left: x = -1f; break;
                case FacingDir.Right: x = 1f; break;
                case FacingDir.Up: y = 1f; break;
                default: y = -1f; break;
            }
            if (_animParams.Contains(HashDirX)) { _anim.SetFloat(HashDirX, x); }
            if (_animParams.Contains(HashDirY)) { _anim.SetFloat(HashDirY, y); }
            if (_animParams.Contains(HashIsMoving)) { _anim.SetBool(HashIsMoving, State.moving || movedThisFrame); }
            if (_animParams.Contains(HashIsFishing)) { _anim.SetBool(HashIsFishing, State.phase != (int)FishingPhase.Idle); }
        }
    }
}
