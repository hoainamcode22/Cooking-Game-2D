using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Camera orthographic bám theo Target (SmoothDamp ở LateUpdate — vị trí người chơi đã chốt), kẹp trong BoxCollider2D
    /// "CameraBounds" (trừ nửa khung nhìn để mép map không lộ). Không có CameraBounds thì không kẹp.
    /// </summary>
    public class FishingCameraFollow : MonoBehaviour
    {
        private const float UnityDefaultOrthoSize = 5f;

        [SerializeField] private Transform target;
        [Tooltip("Tên object chứa BoxCollider2D giới hạn camera. Mặc định FishingIds.CameraBoundsName.")]
        [SerializeField] private string boundsObjectName = FishingIds.CameraBoundsName;

        public Transform Target { get { return target; } set { target = value; } }

        private Camera _cam;
        private BoxCollider2D _bounds;
        private bool _boundsSearched;
        private Vector3 _velocity;
        private FishingConfig _cfg;
        private bool _initialized;

        private void Start() { EnsureInit(); }

        private void EnsureInit()
        {
            if (_initialized) { return; }
            _initialized = true;
            _cfg = FishingDatabase.ConfigOrDefault;
            _cam = GetComponent<Camera>();
            if (_cam == null) { _cam = Camera.main; }
            if (_cam != null)
            {
                _cam.orthographic = true;
                // Chỉ đặt size khi còn giá trị mặc định 5 của Unity — tôn trọng nếu Sếp đã chỉnh tay.
                if (Mathf.Approximately(_cam.orthographicSize, UnityDefaultOrthoSize)) { _cam.orthographicSize = _cfg.cameraOrthoSize; }
                // Iso: renderer cùng order thì y cao vẽ trước (xa hơn).
                _cam.transparencySortMode = TransparencySortMode.CustomAxis;
                _cam.transparencySortAxis = Vector3.up;
            }
            else
            {
                Debug.LogWarning(FishingIds.LogTag + " FishingCameraFollow không tìm thấy Camera.");
            }
        }

        private void FindBoundsOnce()
        {
            if (_boundsSearched) { return; }
            _boundsSearched = true;
            string n = string.IsNullOrEmpty(boundsObjectName) ? FishingIds.CameraBoundsName : boundsObjectName;
            var go = GameObject.Find(n);
            if (go != null) { _bounds = go.GetComponent<BoxCollider2D>(); }
            if (_bounds == null) { Debug.Log(FishingIds.LogTag + " Không có '" + n + "' (BoxCollider2D) — camera không kẹp biên."); }
        }

        private void LateUpdate()
        {
            if (target == null) { return; }
            EnsureInit();
            FindBoundsOnce();
            Vector3 desired = ClampToBounds(target.position);
            desired.z = transform.position.z;
            float smooth = _cfg != null ? _cfg.cameraSmoothTime : 0.12f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smooth);
        }

        /// <summary>Nhảy thẳng tới Target (lúc spawn) để không thấy camera trôi từ gốc toạ độ.</summary>
        public void SnapToTarget()
        {
            if (target == null) { return; }
            EnsureInit();
            FindBoundsOnce();
            Vector3 p = ClampToBounds(target.position);
            p.z = transform.position.z;
            transform.position = p;
            _velocity = Vector3.zero;
        }

        private Vector3 ClampToBounds(Vector3 p)
        {
            if (_bounds == null || _cam == null) { return p; }
            Bounds b = _bounds.bounds;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;

            // Bounds hẹp hơn khung nhìn thì đứng giữa bounds theo trục đó, không nhảy qua lại.
            p.x = b.size.x <= halfW * 2f ? b.center.x : Mathf.Clamp(p.x, b.min.x + halfW, b.max.x - halfW);
            p.y = b.size.y <= halfH * 2f ? b.center.y : Mathf.Clamp(p.y, b.min.y + halfH, b.max.y - halfH);
            return p;
        }
    }
}
