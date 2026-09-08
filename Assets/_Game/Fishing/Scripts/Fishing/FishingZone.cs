using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Vùng nước câu được: Collider2D (Polygon/Box) isTrigger trên FishingZones/Zone_xx. Tự đăng ký vào danh sách static
    /// ở OnEnable/OnDisable để FishingController và bot hỏi "mép nước gần nhất ở đâu". Gizmo xanh nước để Sếp thấy vùng.
    /// </summary>
    // Không dùng [RequireComponent(typeof(Collider2D))]: Collider2D là abstract, Unity không tự thêm được → chỉ cảnh báo ở Awake.
    public class FishingZone : MonoBehaviour
    {
        private const int RandomPointTries = 16;   // số lần thử điểm ngẫu nhiên trong bounds trước khi lùi về tâm (chỉ là kỹ thuật, không phải số gameplay)

        private static readonly List<FishingZone> _all = new List<FishingZone>();

        public static IReadOnlyList<FishingZone> All { get { return _all; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _all.Clear(); }

        [Tooltip("Để trống = tự lấy Collider2D trên cùng object.")]
        [SerializeField] private Collider2D zoneCollider;

        private Collider2D Col
        {
            get
            {
                if (zoneCollider == null) { zoneCollider = GetComponent<Collider2D>(); }
                return zoneCollider;
            }
        }

        private void Awake()
        {
            // Vùng nước chỉ để hỏi hình học, không được chặn Rigidbody2D người chơi → ép isTrigger.
            Collider2D c = Col;
            if (c == null) { Debug.LogWarning(FishingIds.LogTag + " FishingZone '" + name + "' thiếu Collider2D (Polygon/Box) — vùng này không câu được."); return; }
            if (!c.isTrigger) { c.isTrigger = true; }
        }

        private void OnEnable()
        {
            if (!_all.Contains(this)) { _all.Add(this); }
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        /// <summary>Vùng gần pos nhất. Đứng trong vùng → distance 0, closestPoint = pos. Không có vùng nào → false.</summary>
        public static bool TryGetNearest(Vector2 pos, out FishingZone zone, out Vector2 closestPoint, out float distance)
        {
            zone = null;
            closestPoint = pos;
            distance = float.MaxValue;
            for (int i = 0; i < _all.Count; i++)
            {
                FishingZone z = _all[i];
                if (z == null || z.Col == null) { continue; }
                Vector2 cp;
                float d;
                if (z.Contains(pos)) { cp = pos; d = 0f; }
                else { cp = z.ClosestPoint(pos); d = Vector2.Distance(pos, cp); }
                if (d < distance) { distance = d; closestPoint = cp; zone = z; }
            }
            return zone != null;
        }

        public bool Contains(Vector2 worldPos)
        {
            Collider2D c = Col;
            return c != null && c.OverlapPoint(worldPos);
        }

        /// <summary>Điểm trên/trong collider gần worldPos nhất (Collider2D.ClosestPoint, Unity 6).</summary>
        public Vector2 ClosestPoint(Vector2 worldPos)
        {
            Collider2D c = Col;
            if (c == null) { return transform.position; }
            return c.ClosestPoint(worldPos);
        }

        /// <summary>Điểm ngẫu nhiên trong vùng (bot/phao): thử trong bounds rồi kiểm Contains; hết lượt → tâm bounds.</summary>
        public Vector2 RandomPointInside()
        {
            Collider2D c = Col;
            if (c == null) { return transform.position; }
            Bounds b = c.bounds;
            for (int i = 0; i < RandomPointTries; i++)
            {
                var p = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
                if (c.OverlapPoint(p)) { return p; }
            }
            return b.center;
        }

        private void OnDrawGizmos()
        {
            Collider2D c = zoneCollider != null ? zoneCollider : GetComponent<Collider2D>();
            if (c == null) { return; }
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            var poly = c as PolygonCollider2D;
            if (poly != null)
            {
                for (int p = 0; p < poly.pathCount; p++)
                {
                    Vector2[] path = poly.GetPath(p);
                    for (int i = 0; i < path.Length; i++)
                    {
                        Vector3 a = poly.transform.TransformPoint(path[i] + poly.offset);
                        Vector3 b2 = poly.transform.TransformPoint(path[(i + 1) % path.Length] + poly.offset);
                        Gizmos.DrawLine(a, b2);
                    }
                }
            }
            else
            {
                Bounds b = c.bounds;
                Gizmos.DrawWireCube(b.center, b.size);
            }
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.15f);
            Gizmos.DrawCube(c.bounds.center, c.bounds.size);
        }
    }
}
