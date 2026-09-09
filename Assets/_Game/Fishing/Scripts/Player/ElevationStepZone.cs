using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// VÙNG LÊN BẬC (cách nhẹ, không cần hệ elevation thật). Gắn trên 1 object có Collider2D isTrigger đặt ở LỐI LÊN của bậc núi:
    /// người chơi bước vào → thân nhân vật được nâng thêm <c>stepHeight</c> unit theo trục Y trong <c>duration</c> giây và
    /// cộng <c>sortingOrderBonus</c> để vẽ ĐÈ lên mặt bậc; bước ra → hạ về như cũ.
    /// Offset cộng THÊM vào vị trí ở LateUpdate (sau vật lý) nên không phá Rigidbody2D.MovePosition của FishingPlayerController.
    /// Sếp nhân bản 1 object vùng cho MỖI lối lên (mẫu: ElevSteps/Step_01 do tool tạo).
    /// </summary>
    // KHÔNG dùng [RequireComponent(typeof(Collider2D))]: Collider2D là lớp trừu tượng, Unity không tự thêm được → cảnh báo ở Awake thay thế.
    public class ElevationStepZone : MonoBehaviour
    {
        /// <summary>Tên object gốc chứa các vùng bậc (tool dựng sẵn 1 mẫu).</summary>
        public const string RootName = "ElevSteps";
        public const string SampleName = "Step_01";

        [Tooltip("Nâng thêm bao nhiêu unit khi vào vùng. Khớp offset Y của Tilemap_Elev* (0.25 = 1 bậc).")]
        [SerializeField] private float stepHeight = 0.25f;
        [Tooltip("Thời gian nâng/hạ (giây) cho mượt.")]
        [Min(0.01f)] [SerializeField] private float duration = 0.18f;
        [Tooltip("Cộng vào sorting order của nhân vật khi đang ở trên bậc (để vẽ trên mặt bậc).")]
        // [Lead vòng 14] PHẢI > stepHeight × FishingYSort.factor (0.25 × 100 = 25), nếu không lên bậc lại bị vẽ CHÌM
        // hơn lúc đứng dưới (YSort trừ order theo y). 30 = 25 + 5 dư an toàn.
        [SerializeField] private int sortingOrderBonus = 30;

        private FishingPlayerController _player;
        private FishingYSort _ySort;
        private SpriteRenderer _renderer;
        private float _applied;         // offset ĐÃ cộng vào transform người chơi
        private float _targetOffset;    // 0 = ở ngoài, stepHeight = ở trong
        private bool _bonusApplied;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) { col.isTrigger = true; }
        }

        private void OnValidate()
        {
            if (duration < 0.01f) { duration = 0.01f; }
        }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogWarning(FishingIds.LogTag + " ElevationStepZone '" + name + "' thiếu Collider2D — thêm BoxCollider2D (isTrigger) thì vùng lên bậc mới hoạt động.");
                return;
            }
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log(FishingIds.LogTag + " ElevationStepZone '" + name + "': Collider2D chưa isTrigger — đã tự bật (vùng lên bậc không được chặn người chơi).");
            }
        }

        private void OnDisable() { ReleasePlayer(); }

        private void OnDestroy() { ReleasePlayer(); }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsLocalPlayer(other)) { return; }
            _player = FishingPlayerController.Local;
            _ySort = _player.GetComponent<FishingYSort>();
            _renderer = _player.GetComponentInChildren<SpriteRenderer>();
            _targetOffset = stepHeight;
            ApplySortingBonus(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsLocalPlayer(other)) { return; }
            _targetOffset = 0f;
            // KHÔNG hạ sorting ngay: người chơi còn đang hạ dần, đổi order giữa chừng gây nháy thứ tự vẽ.
            // Hạ khi _applied thật sự về 0 (LateUpdate bên dưới).
        }

        /// <summary>Đọc/ghi vị trí người chơi ở LateUpdate: vật lý đã chạy xong, offset chỉ cộng phần CHÊNH nên không triệt chuyển động.</summary>
        private void LateUpdate()
        {
            if (_player == null) { return; }
            if (Mathf.Approximately(_applied, _targetOffset))
            {
                if (Mathf.Approximately(_targetOffset, 0f))
                {
                    ApplySortingBonus(false);
                    _player = null; _ySort = null; _renderer = null;
                }
                return;
            }
            float speed = stepHeight / Mathf.Max(0.01f, duration);
            float next = Mathf.MoveTowards(_applied, _targetOffset, speed * Time.deltaTime);
            float delta = next - _applied;
            _applied = next;
            _player.AddElevationOffset(delta);   // đẩy cả Rigidbody2D, xem FishingPlayerController.AddElevationOffset
        }

        private bool IsLocalPlayer(Collider2D other)
        {
            if (other == null) { return false; }
            var local = FishingPlayerController.Local;
            if (local == null) { return false; }
            var found = other.GetComponentInParent<FishingPlayerController>();
            return found != null && found == local;
        }

        private void ApplySortingBonus(bool on)
        {
            if (on == _bonusApplied) { return; }
            int sign = on ? 1 : -1;
            // FishingYSort ghi đè sortingOrder mỗi LateUpdate → phải cộng vào BaseOrder, không cộng thẳng renderer.
            if (_ySort != null) { _ySort.BaseOrder += sign * sortingOrderBonus; }
            else if (_renderer != null) { _renderer.sortingOrder += sign * sortingOrderBonus; }
            _bonusApplied = on;
        }

        /// <summary>Tắt/huỷ vùng khi người chơi còn đứng trong: trả lại độ cao và sorting order ngay để không kẹt lơ lửng.</summary>
        private void ReleasePlayer()
        {
            if (_player != null && !Mathf.Approximately(_applied, 0f))
            {
                _player.AddElevationOffset(-_applied);
            }
            _applied = 0f;
            _targetOffset = 0f;
            ApplySortingBonus(false);
            _player = null;
            _ySort = null;
            _renderer = null;
        }
    }
}
