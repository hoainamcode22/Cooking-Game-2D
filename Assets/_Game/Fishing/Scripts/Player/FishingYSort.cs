using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Sắp thứ tự vẽ theo Y cho nhân vật (local + người chơi khác): đứng thấp hơn thì nổi lên trên.
    /// order = baseOrder + Clamp(RoundToInt(-y * factor), -clamp, clamp). World chuẩn (1 ô = 1 unit) nên factor 100 / clamp 8000.
    /// Đặt ở LateUpdate vì dự án không có MonoManager.asset (thứ tự Update không xác định) — vị trí đã chốt xong mới sort.
    /// </summary>
    public class FishingYSort : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private int baseOrder = 0;
        [SerializeField] private float factor = 100f;
        [SerializeField] private int clamp = 8000;

        public int BaseOrder { get { return baseOrder; } set { baseOrder = value; } }

        /// <summary>Gán renderer + order gốc từ code (tool/runtime). Renderer null thì tự tìm trong con.</summary>
        public void Configure(SpriteRenderer renderer, int newBaseOrder)
        {
            target = renderer;
            baseOrder = newBaseOrder;
        }

        private void Awake()
        {
            if (target == null) { target = GetComponentInChildren<SpriteRenderer>(); }
        }

        private void LateUpdate()
        {
            if (target == null) { return; }
            int dynamic = Mathf.Clamp(Mathf.RoundToInt(-transform.position.y * factor), -clamp, clamp);
            target.sortingOrder = baseOrder + dynamic;
        }
    }
}
